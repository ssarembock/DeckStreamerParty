using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeckParty.EditorTools
{
    /// <summary>
    /// One-click project setup: imports TextMeshPro essentials, generates placeholder art, data assets, the network
    /// prefab, UI prefabs and both scenes, then configures build settings. Runs automatically (with a prompt)
    /// the first time the project is opened, and is available from the Deck Party menu.
    /// </summary>
    public static class DeckPartySetup
    {
        const string TmpPackage = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";
        const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        static Action s_AfterTmp;
        static double s_TmpStarted;

        // ------------------------------------------------------------------ menu

        [MenuItem("Deck Party/Setup/Build Everything (keeps existing scenes)", priority = 0)]
        public static void BuildMissing() => BuildEverything(false);

        [MenuItem("Deck Party/Setup/Rebuild Scenes and UI Prefabs (overwrites)", priority = 1)]
        public static void RebuildAll()
        {
            if (!EditorUtility.DisplayDialog("Rebuild Deck Party scenes?",
                    "This regenerates MainMenu.unity, Game.unity and the UI prefabs, discarding any edits you made to them " +
                    "(the old prefabs are backed up to Prefabs/Backup first).\n\n" +
                    "Your art, animations, stream setups, UI Skin, audio, FX and database assets are kept.", "Rebuild", "Cancel"))
                return;
            BuildEverything(true);
        }

        [MenuItem("Deck Party/Setup/Regenerate Placeholder Art", priority = 2)]
        public static void RegeneratePlaceholders()
        {
            PlaceholderArt.GenerateAll(true);
            Debug.Log("[DeckParty] Placeholder art regenerated.");
        }

        [MenuItem("Deck Party/Open Main Menu Scene", priority = 20)]
        public static void OpenMainMenu() => OpenScene(DeckPartyPaths.MainMenuScene);

        [MenuItem("Deck Party/Open Game Scene", priority = 21)]
        public static void OpenGame() => OpenScene(DeckPartyPaths.GameScene);

        [MenuItem("Deck Party/Select Game Database", priority = 40)]
        public static void SelectDatabase() => Select(DeckPartyPaths.Database);

        [MenuItem("Deck Party/Select UI Skin", priority = 41)]
        public static void SelectSkin() => Select(DeckPartyPaths.Skin);

        [MenuItem("Deck Party/Select Audio Library", priority = 42)]
        public static void SelectAudio() => Select(DeckPartyPaths.AudioLibrary);

        [MenuItem("Deck Party/Open Instructions", priority = 60)]
        public static void OpenInstructions() => OpenDoc("INSTRUCTIONS.md");

        [MenuItem("Deck Party/Open V1 Instructions (sprites and backgrounds)", priority = 61)]
        public static void OpenV1Instructions() => OpenDoc("V1Instructions.md");

        [MenuItem("Deck Party/Open V2 Instructions (Audience Capture update)", priority = 62)]
        public static void OpenV2Instructions() => OpenDoc("V2Instructions.md");

        [MenuItem("Deck Party/Open UI Guide", priority = 63)]
        public static void OpenUIGuide() => OpenDoc("UIGuide.md");

        [MenuItem("Deck Party/Open New UI Guidelines (website look)", priority = 64)]
        public static void OpenNewUIGuidelines() => OpenDoc("NewUIGuidelines.md");

        static void OpenDoc(string fileName)
        {
            var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", fileName));
            if (File.Exists(path)) EditorUtility.OpenWithDefaultApp(path);
            else Debug.LogWarning($"[DeckParty] {fileName} not found next to the Assets folder.");
        }

        static void OpenScene(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[DeckParty] {path} doesn't exist yet. Run Deck Party > Setup > Build Everything.");
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(path);
        }

        static void Select(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (asset == null)
            {
                Debug.LogWarning($"[DeckParty] {path} not found. Run Deck Party > Setup > Build Everything.");
                return;
            }
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        // ------------------------------------------------------------------ build

        public static bool ScenesExist => File.Exists(DeckPartyPaths.MainMenuScene) && File.Exists(DeckPartyPaths.GameScene);

        public static void BuildEverything(bool overwrite)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Deck Party", "Exit Play Mode before running setup.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureTmpEssentials(() => RunBuild(overwrite));
        }

        static void RunBuild(bool overwrite)
        {
            try
            {
                EditorUtility.DisplayProgressBar("Deck Party", "Creating folders and placeholder art...", 0.1f);
                DeckPartyAssets.EnsureFolders();
                PlaceholderArt.GenerateAll(false);
                EditorUI.LoadArt();

                EditorUtility.DisplayProgressBar("Deck Party", "Creating data assets...", 0.2f);
                DeckPartyAssets.EnsureDataAssets();

                EditorUtility.DisplayProgressBar("Deck Party", "Setting up languages, fonts and string tables...", 0.25f);
                LocalizationSetup.EnsureAll();
                EditorLoc.Reset();

                EditorUtility.DisplayProgressBar("Deck Party", "Setting up character animation...", 0.3f);
                CharacterAnimationTools.EnsureAll();

                EditorUtility.DisplayProgressBar("Deck Party", "Generating Feel feedbacks and shader materials...", 0.33f);
                FxSetup.Ensure(false);
                // Fonts may have changed: builders pick up the UI Skin's body font for new text.
                EditorUI.LoadArt();

                EditorUtility.DisplayProgressBar("Deck Party", "Creating the network session prefab...", 0.35f);
                DeckPartyAssets.EnsureNetworkPrefab();

                EditorUtility.DisplayProgressBar("Deck Party", "Building UI prefabs...", 0.45f);
                bool rebuildPrefabs = overwrite || !File.Exists(DeckPartyPaths.GuestCardPrefab);
                DeckPartyAssets.EnsureUIPrefabs(rebuildPrefabs);

                if (overwrite || !File.Exists(DeckPartyPaths.MainMenuScene))
                {
                    EditorUtility.DisplayProgressBar("Deck Party", "Building the MainMenu scene...", 0.6f);
                    MainMenuSceneBuilder.Build();
                }

                if (overwrite || !File.Exists(DeckPartyPaths.GameScene))
                {
                    EditorUtility.DisplayProgressBar("Deck Party", "Building the Game scene...", 0.8f);
                    GameSceneBuilder.Build();
                }

                EditorUtility.DisplayProgressBar("Deck Party", "Configuring build settings...", 0.95f);
                ConfigureProject();
                AssetDatabase.SaveAssets();
                EditorUtility.ClearProgressBar();

                EditorSceneManager.OpenScene(DeckPartyPaths.MainMenuScene);
                Debug.Log("[DeckParty] Setup complete. Open Assets/DeckParty/Scenes/MainMenu.unity and press Play. See INSTRUCTIONS.md for art and multiplayer.");
                EditorUtility.DisplayDialog("Audience Capture is ready",
                    "Scenes, prefabs, placeholder art, languages, FX and data assets were generated.\n\n" +
                    "Press Play in the MainMenu scene to try it. UIGuide.md (Deck Party > Open UI Guide) explains where to change everything.",
                    "Let's go live");
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogException(e);
                EditorUtility.DisplayDialog("Deck Party setup failed", e.Message + "\n\nSee the Console for details. You can re-run Deck Party > Setup > Build Everything.", "OK");
            }
        }

        /// <summary>
        /// Makes MainMenu (index 0, the startup scene in builds) and Game the first two enabled build scenes.
        /// Any other scenes already in the list are kept after them.
        /// </summary>
        public static void EnsureBuildScenes()
        {
            var required = new[] { DeckPartyPaths.MainMenuScene, DeckPartyPaths.GameScene };
            var current = EditorBuildSettings.scenes;
            var wanted = required.Select(path => new EditorBuildSettingsScene(path, true))
                .Concat(current.Where(scene => !required.Contains(scene.path)))
                .ToArray();

            bool same = current.Length == wanted.Length;
            for (int i = 0; same && i < wanted.Length; i++)
                same = current[i].path == wanted[i].path && current[i].enabled == wanted[i].enabled;
            if (same) return;

            EditorBuildSettings.scenes = wanted;
            Debug.Log("[DeckParty] Added MainMenu and Game to the build scene list (File > Build Profiles > Scene List).");
        }

        static void ConfigureProject()
        {
            EnsureBuildScenes();

            PlayerSettings.productName = "Deck Party";
            // Multiplayer hosts must keep simulating while their window is in the background.
            PlayerSettings.runInBackground = true;
            PlayerSettings.visibleInBackground = true;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.forceSingleInstance = false;
        }

        // ------------------------------------------------------------------ TextMeshPro essentials

        static bool TmpReady => AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath) != null || AssetDatabase.FindAssets("t:TMP_Settings").Length > 0;

        static void EnsureTmpEssentials(Action then)
        {
            if (TmpReady)
            {
                then();
                return;
            }

            Debug.Log("[DeckParty] Importing TextMeshPro Essential Resources...");
            s_AfterTmp = then;
            s_TmpStarted = EditorApplication.timeSinceStartup;
            AssetDatabase.importPackageCompleted += OnTmpImported;
            AssetDatabase.importPackageFailed += OnTmpFailed;
            AssetDatabase.importPackageCancelled += OnTmpCancelled;
            EditorApplication.update += PollTmp;
            AssetDatabase.ImportPackage(TmpPackage, false);
        }

        static void OnTmpImported(string packageName) => FinishTmp(true);
        static void OnTmpFailed(string packageName, string error)
        {
            Debug.LogError($"[DeckParty] TMP Essentials import failed: {error}");
            FinishTmp(false);
        }
        static void OnTmpCancelled(string packageName) => FinishTmp(false);

        static void PollTmp()
        {
            if (TmpReady) FinishTmp(true);
            else if (EditorApplication.timeSinceStartup - s_TmpStarted > 90) FinishTmp(false);
        }

        static void FinishTmp(bool success)
        {
            AssetDatabase.importPackageCompleted -= OnTmpImported;
            AssetDatabase.importPackageFailed -= OnTmpFailed;
            AssetDatabase.importPackageCancelled -= OnTmpCancelled;
            EditorApplication.update -= PollTmp;

            var then = s_AfterTmp;
            s_AfterTmp = null;
            if (then == null) return;

            if (!success)
            {
                EditorUtility.DisplayDialog("Deck Party", "TextMeshPro Essential Resources couldn't be imported automatically.\n\n" +
                    "Use Window > TextMeshPro > Import TMP Essential Resources, then run Deck Party > Setup > Build Everything.", "OK");
                return;
            }

            AssetDatabase.Refresh();
            // Give TMP a moment to load its new settings before building UI.
            EditorApplication.delayCall += () => then();
        }
    }

    /// <summary>
    /// Safety net when pressing Play: if setup never finished, stop and offer to run it; otherwise make sure both
    /// scenes are in the build list so scene loads can't fail.
    /// </summary>
    [InitializeOnLoad]
    static class DeckPartyPlayModeGuard
    {
        static DeckPartyPlayModeGuard()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingEditMode) return;
            // Multiplayer Play Mode clones share the main project's settings; leave them alone.
            if (Application.dataPath.Replace('\\', '/').Contains("/Library/VP/")) return;

            if (!DeckPartySetup.ScenesExist)
            {
                EditorApplication.isPlaying = false;
                if (EditorUtility.DisplayDialog("Deck Party setup isn't finished",
                        "The MainMenu and/or Game scene hasn't been generated yet, so the game can't run.\n\nBuild the missing pieces now?",
                        "Build now", "Cancel"))
                {
                    EditorApplication.delayCall += () => DeckPartySetup.BuildEverything(false);
                }
                return;
            }

            DeckPartySetup.EnsureBuildScenes();
        }
    }

    /// <summary>Offers to run setup the first time the project is opened without generated scenes.</summary>
    [InitializeOnLoad]
    static class DeckPartyAutoSetup
    {
        const string SessionKey = "DeckParty.AutoSetupOffered";

        static DeckPartyAutoSetup()
        {
            EditorApplication.delayCall += TryOffer;
        }

        static void TryOffer()
        {
            if (Application.isBatchMode || SessionState.GetBool(SessionKey, false)) return;

            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryOffer;
                return;
            }

            // Multiplayer Play Mode clones share the main project's assets; never build from a clone.
            if (Application.dataPath.Replace('\\', '/').Contains("/Library/VP/")) return;
            if (DeckPartySetup.ScenesExist) return;

            SessionState.SetBool(SessionKey, true);
            if (EditorUtility.DisplayDialog("Welcome to Deck Party",
                    "Deck Party needs to generate its scenes, UI prefabs, placeholder art and data assets (and import TextMeshPro essentials).\n\n" +
                    "This takes a few seconds. You can also run it later from Deck Party > Setup > Build Everything.",
                    "Build now", "Later"))
            {
                DeckPartySetup.BuildEverything(false);
            }
        }
    }
}
