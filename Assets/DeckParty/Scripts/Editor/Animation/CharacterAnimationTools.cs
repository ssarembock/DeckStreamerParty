using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DeckParty.Core;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace DeckParty.EditorTools
{
    /// <summary>
    /// Animated characters: the portrait stage (render texture + camera on the "CardPortraits" layer), template
    /// controllers with every state the game plays, override controllers per character, and a validator.
    /// </summary>
    internal static class CharacterAnimationTools
    {
        public const string Folder = "Assets/DeckParty/Animation";
        public const string CharacterTemplatePath = Folder + "/AC_CharacterTemplate.controller";
        public const string StreamerTemplatePath = Folder + "/AC_StreamerTemplate.controller";
        public const string StagePrefabPath = "Assets/DeckParty/Prefabs/PortraitStage.prefab";
        public const string UnlitMaterialPath = Folder + "/PortraitUnlit.mat";

        static readonly CharacterState[] s_CharacterStates =
        {
            CharacterState.Idle, CharacterState.IdlePreActive, CharacterState.Active, CharacterState.IdlePostActive, CharacterState.Score,
            CharacterState.Enter, CharacterState.Exit, CharacterState.Hit, CharacterState.Celebrate,
        };

        static readonly CharacterState[] s_StreamerStates =
            { CharacterState.Idle, CharacterState.Select, CharacterState.Anxious, CharacterState.Celebrate };

        // ------------------------------------------------------------------ menu

        [MenuItem("Deck Party/Art/Create Character Animator Template", priority = 200)]
        public static void CreateTemplateMenu()
        {
            var controller = EnsureTemplate(CharacterTemplatePath, s_CharacterStates, true);
            Selection.activeObject = controller;
            EditorGUIUtility.PingObject(controller);
        }

        [MenuItem("Deck Party/Art/Create Streamer Animator Template", priority = 201)]
        public static void CreateStreamerTemplateMenu()
        {
            var controller = EnsureTemplate(StreamerTemplatePath, s_StreamerStates, false);
            Selection.activeObject = controller;
            EditorGUIUtility.PingObject(controller);
        }

        [MenuItem("Deck Party/Art/Validate Character Animations", priority = 202)]
        public static void ValidateMenu()
        {
            var report = Validate(out int problems);
            Debug.Log("[DeckParty] Character animation report:\n" + report);
            EditorUtility.DisplayDialog("Character animations", problems == 0 ? "No problems found." : $"{problems} problem(s) found. Details are in the Console.", "OK");
        }

        // ------------------------------------------------------------------ setup (called by Build Everything)

        public static void EnsureAll()
        {
            EnsureFolder(Folder);
            EnsureTemplate(CharacterTemplatePath, s_CharacterStates, true);
            EnsureTemplate(StreamerTemplatePath, s_StreamerStates, false);
            EnsureLayer(PortraitStage.LayerName, 31);
            var unlit = EnsureUnlitMaterial();
            var stage = EnsureStagePrefab(unlit);

            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(DeckPartyPaths.Database);
            if (db != null && db.portraitStage == null)
            {
                db.portraitStage = stage;
                EditorUtility.SetDirty(db);
            }
        }

        /// <summary>A controller whose states are exactly the names the game plays; one-shot states flow back to idle.</summary>
        public static AnimatorController EnsureTemplate(string path, CharacterState[] states, bool character)
        {
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (existing != null) return existing;
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));

            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            var machine = controller.layers[0].stateMachine;
            var created = new Dictionary<CharacterState, AnimatorState>();
            for (int i = 0; i < states.Length; i++)
            {
                var state = machine.AddState(CharacterAnimDriver.StateName(states[i]), new Vector3(300 + (i % 3) * 260, 60 + (i / 3) * 90, 0));
                created[states[i]] = state;
            }
            machine.defaultState = created[CharacterState.Idle];

            // The game also returns one-shots to idle itself, so these transitions are a convenience for previewing.
            void Back(CharacterState from, CharacterState to)
            {
                if (!created.ContainsKey(from) || !created.ContainsKey(to)) return;
                var t = created[from].AddTransition(created[to]);
                t.hasExitTime = true;
                t.exitTime = 1f;
                t.duration = 0.1f;
            }
            if (character)
            {
                Back(CharacterState.Active, CharacterState.IdlePostActive);
                Back(CharacterState.Score, CharacterState.Idle);
                Back(CharacterState.Enter, CharacterState.Idle);
                Back(CharacterState.Hit, CharacterState.Idle);
            }
            else
            {
                Back(CharacterState.Select, CharacterState.Idle);
            }
            AssetDatabase.SaveAssets();
            return controller;
        }

        /// <summary>Creates (or returns) an override controller for one viewer, ready for its clips, and assigns it.</summary>
        public static AnimatorOverrideController CreateOverride(GuestArt art)
        {
            var template = EnsureTemplate(CharacterTemplatePath, s_CharacterStates, true);
            string folder = Folder + "/Characters";
            EnsureFolder(folder);
            string path = $"{folder}/{art.type}_Override.overrideController";
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
            if (existing == null)
            {
                existing = new AnimatorOverrideController(template) { name = art.type + "_Override" };
                AssetDatabase.CreateAsset(existing, path);
            }
            art.animatorController = existing;
            AssetDatabase.SaveAssets();
            return existing;
        }

        // ------------------------------------------------------------------ portrait stage

        static Material EnsureUnlitMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterialPath);
            if (material != null) return material;
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) return null;
            material = new Material(shader) { name = "PortraitUnlit" };
            AssetDatabase.CreateAsset(material, UnlitMaterialPath);
            return material;
        }

        static GameObject EnsureStagePrefab(Material unlit)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(StagePrefabPath);
            if (existing != null) return existing;
            var go = new GameObject("PortraitStage");
            var stage = go.AddComponent<PortraitStage>();
            stage.unlitMaterial = unlit;
            var cameraGo = new GameObject("StageCamera");
            cameraGo.transform.SetParent(go.transform, false);
            var camera = cameraGo.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, StagePrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        /// <summary>Names a free user layer (prefers <paramref name="preferredIndex"/>) so the stage camera only sees characters.</summary>
        static void EnsureLayer(string name, int preferredIndex)
        {
            if (LayerMask.NameToLayer(name) >= 0) return;
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            int index = string.IsNullOrEmpty(layers.GetArrayElementAtIndex(preferredIndex).stringValue) ? preferredIndex : -1;
            for (int i = 31; index < 0 && i >= 8; i--)
                if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)) index = i;
            if (index < 0)
            {
                Debug.LogWarning("[DeckParty] No free layer for CardPortraits; the portrait stage uses layer 31.");
                return;
            }
            layers.GetArrayElementAtIndex(index).stringValue = name;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------ validation

        /// <summary>The states the game will look for on this viewer.</summary>
        public static CharacterState[] RequiredStates(GuestType type) =>
            GuestCatalog.Get(type).HasAbility
                ? new[] { CharacterState.IdlePreActive, CharacterState.Active, CharacterState.IdlePostActive, CharacterState.Score }
                : new[] { CharacterState.Idle, CharacterState.Score };

        public static RuntimeAnimatorController ControllerFor(GuestArt art)
        {
            if (art.animatorController != null) return art.animatorController;
            var animator = art.characterPrefab != null ? art.characterPrefab.GetComponentInChildren<Animator>(true) : null;
            return animator != null ? animator.runtimeAnimatorController : null;
        }

        /// <summary>State names present on the controller (an override controller reports its template's states).</summary>
        public static HashSet<string> StateNames(RuntimeAnimatorController controller)
        {
            var names = new HashSet<string>();
            if (controller is AnimatorOverrideController overrideController) controller = overrideController.runtimeAnimatorController;
            if (controller is AnimatorController animatorController && animatorController.layers.Length > 0)
                foreach (var child in animatorController.layers[0].stateMachine.states) names.Add(child.state.name);
            return names;
        }

        /// <summary>Missing required states, as display names. Idle_Pre/Post_Active fall back to Idle, so Idle alone satisfies them.</summary>
        public static List<string> MissingStates(GuestArt art)
        {
            var missing = new List<string>();
            var controller = ControllerFor(art);
            if (controller == null) return missing;
            var names = StateNames(controller);
            foreach (var state in RequiredStates(art.type))
            {
                string name = CharacterAnimDriver.StateName(state);
                bool idleFallback = (state == CharacterState.IdlePreActive || state == CharacterState.IdlePostActive) && names.Contains("Idle");
                if (!names.Contains(name) && !idleFallback) missing.Add(name);
            }
            return missing;
        }

        public static string Validate(out int problems)
        {
            problems = 0;
            var sb = new StringBuilder();
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(DeckPartyPaths.Database);
            if (db == null) return "Game Database not found.";

            foreach (var art in db.guests)
            {
                if (art == null || !art.IsAnimated) continue;
                var issues = new List<string>();
                var controller = ControllerFor(art);
                if (controller == null) issues.Add("no Animator Controller (assign one, or give the prefab an Animator with a controller)");
                else
                {
                    var missing = MissingStates(art);
                    if (missing.Count > 0) issues.Add("missing states: " + string.Join(", ", missing) + " (missing Active/Score just get a Feel punch)");

                    // Clips must animate paths that exist on the prefab (rigged PSBs animate bones like "bone_1/bone_2").
                    if (art.characterPrefab != null)
                    {
                        var badPaths = new HashSet<string>();
                        foreach (var clip in controller.animationClips.Distinct())
                        {
                            if (clip == null) continue;
                            foreach (var binding in AnimationUtility.GetCurveBindings(clip).Concat(AnimationUtility.GetObjectReferenceCurveBindings(clip)))
                                if (!string.IsNullOrEmpty(binding.path) && art.characterPrefab.transform.Find(binding.path) == null) badPaths.Add(binding.path);
                        }
                        if (badPaths.Count > 0) issues.Add("clips animate paths the prefab doesn't have: " + string.Join(", ", badPaths.Take(6)) + (badPaths.Count > 6 ? "..." : string.Empty));
                    }
                    else if (controller.animationClips.Any(c => c != null && AnimationUtility.GetCurveBindings(c).Any(b => b.type != typeof(SpriteRenderer))))
                    {
                        issues.Add("controller-only characters can only animate the sprite (sprite-swap clips on a root SpriteRenderer)");
                    }
                }

                if (issues.Count == 0) sb.AppendLine($"OK   {art.name}");
                else
                {
                    problems += issues.Count;
                    sb.AppendLine($"FIX  {art.name}: " + string.Join("; ", issues));
                }
            }
            if (sb.Length == 0) sb.AppendLine("No animated characters assigned yet.");
            return sb.ToString();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
