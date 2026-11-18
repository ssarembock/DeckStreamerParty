using System.Collections.Generic;
using DeckParty.Platforms;
using UnityEditor;
using UnityEngine;

namespace DeckParty.EditorTools
{
    /// <summary>
    /// The Online Platform settings (Project Settings > Deck Party > Online Platform, or the asset's Inspector): which
    /// platform this project builds for, what's installed for it, and each platform's options.
    /// </summary>
    [CustomEditor(typeof(OnlinePlatformSettings))]
    internal sealed class OnlinePlatformSettingsEditor : Editor
    {
        static readonly OnlinePlatformKind[] s_Kinds =
        {
            OnlinePlatformKind.Default, OnlinePlatformKind.Steam, OnlinePlatformKind.Epic, OnlinePlatformKind.AppleGameCenter,
            OnlinePlatformKind.GooglePlayGames, OnlinePlatformKind.Xbox, OnlinePlatformKind.PlayStation, OnlinePlatformKind.NintendoSwitch,
        };

        static bool s_ShowSteam = true;
        static bool s_ShowEpic = true;
        static bool s_ShowPlaceholders;

        GUIStyle m_Wrap;

        public override void OnInspectorGUI()
        {
            m_Wrap ??= new GUIStyle(EditorStyles.wordWrappedLabel) { richText = true };
            var settings = (OnlinePlatformSettings)target;
            serializedObject.Update();

            EditorGUILayout.LabelField("Online Platform", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Which service runs multiplayer in builds of this project. <b>Default</b> is the original setup: Unity Relay join codes " +
                "and Direct IP. <b>Steam</b> adds Steam lobbies, friends, invites, the overlay, Find Games and Quick Match, over Steam's relay network. " +
                "<b>Epic Games</b> does the same with Epic Online Services (EOS lobbies, Epic friends and overlay, EOS P2P). " +
                "With <b>Cross-Platform Join Codes</b> on, every Steam or Epic lobby also gets a short code anyone can type in, from any platform. " +
                "The others are reserved for later and use Default until they're built.", m_Wrap);
            EditorGUILayout.Space(4);

            DrawPlatformPopup(settings);
            DrawStatus(settings);

            EditorGUILayout.Space(6);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(OnlinePlatformSettings.fallbackToDefault)), new GUIContent("Fall Back to Default"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(OnlinePlatformSettings.editorUsesDefault)), new GUIContent("Editor Uses Default"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(OnlinePlatformSettings.keepDirectIp)), new GUIContent("Keep Direct IP / LAN"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(OnlinePlatformSettings.crossPlatformCodes)), new GUIContent("Cross-Platform Join Codes"));

            EditorGUILayout.Space(8);
            s_ShowSteam = EditorGUILayout.BeginFoldoutHeaderGroup(s_ShowSteam, "Steam");
            if (s_ShowSteam) DrawSteam(settings);
            EditorGUILayout.EndFoldoutHeaderGroup();

            s_ShowEpic = EditorGUILayout.BeginFoldoutHeaderGroup(s_ShowEpic, "Epic Games (Epic Online Services)");
            if (s_ShowEpic) DrawEpic(settings);
            EditorGUILayout.EndFoldoutHeaderGroup();

            s_ShowPlaceholders = EditorGUILayout.BeginFoldoutHeaderGroup(s_ShowPlaceholders, "Game Center, Google Play (placeholders)");
            if (s_ShowPlaceholders)
            {
                EditorGUILayout.HelpBox("Not implemented yet. These hold the values each platform will need, so the settings are ready when one is added " +
                                        "(see Streamworks_Guidelines.md, \"Adding another platform\").", MessageType.None);
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(OnlinePlatformSettings.appleGameCenter)), new GUIContent("Game Center (iOS)"), true);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(OnlinePlatformSettings.googlePlayGames)), new GUIContent("Google Play Games (Android)"), true);
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField(
                "Any build can be switched on the command line: <b>-onlineplatform default</b>, <b>-onlineplatform steam</b> or <b>-onlineplatform epic</b>. " +
                "Guides: Streamworks_Guidelines.md (Steam) and EpicOnlineServices_Guidelines.md (Epic, join codes) in the project folder.", m_Wrap);

            if (serializedObject.ApplyModifiedProperties())
            {
                OnlinePlatformSettings.ClearCache();
                if (settings.platform == OnlinePlatformKind.Steam) OnlinePlatformSetup.SyncSteamAppIdFile(settings);
            }
        }

        void DrawPlatformPopup(OnlinePlatformSettings settings)
        {
            var labels = new List<string>();
            int index = 0;
            for (int i = 0; i < s_Kinds.Length; i++)
            {
                var kind = s_Kinds[i];
                string label = kind == OnlinePlatformKind.Default ? "Default (Unity Relay + Direct IP)" : OnlinePlatforms.DisplayName(kind);
                if (kind != OnlinePlatformKind.Default && kind != OnlinePlatformKind.Steam && kind != OnlinePlatformKind.Epic) label += "  (not implemented)";
                labels.Add(label);
                if (kind == settings.platform) index = i;
            }

            var property = serializedObject.FindProperty(nameof(OnlinePlatformSettings.platform));
            EditorGUI.BeginChangeCheck();
            int picked = EditorGUILayout.Popup(new GUIContent("Platform", "The platform this project is built for."), index, labels.ToArray());
            if (EditorGUI.EndChangeCheck()) property.intValue = (int)s_Kinds[picked];
        }

        void DrawStatus(OnlinePlatformSettings settings)
        {
            switch (settings.platform)
            {
                case OnlinePlatformKind.Default:
                    EditorGUILayout.HelpBox("Multiplayer works as before: Host Online Game (a Unity Relay join code) or Direct IP / LAN.", MessageType.Info);
                    return;
                case OnlinePlatformKind.Steam:
                    DrawSteamStatus(settings);
                    return;
                case OnlinePlatformKind.Epic:
                    DrawEpicStatus();
                    return;
                default:
                    EditorGUILayout.HelpBox($"{OnlinePlatforms.DisplayName(settings.platform)} isn't implemented yet, so builds use the Default multiplayer " +
                                            "(and say so on the Multiplayer screen).", MessageType.Warning);
                    return;
            }
        }

        void DrawSteamStatus(OnlinePlatformSettings settings)
        {
            var package = OnlinePlatformSetup.SteamworksPackageInfo;
            bool installed = OnlinePlatformSetup.SteamworksInstalled;
            if (!installed)
            {
                EditorGUILayout.HelpBox("Steamworks.NET isn't installed, so Steam can't run: the game uses the Default multiplayer and builds stop with an error. " +
                                        "Install it (Unity downloads it from GitHub; Git must be installed).", MessageType.Error);
                using (new EditorGUI.DisabledScope(OnlinePlatformSetup.Installing))
                    if (GUILayout.Button(OnlinePlatformSetup.Installing ? "Installing Steamworks.NET..." : "Install Steamworks.NET"))
                        OnlinePlatformSetup.InstallSteamworks();
                return;
            }

            string version = package != null ? package.version : "copied into Assets";
            if (!OnlinePlatformSetup.StandaloneTarget)
            {
                EditorGUILayout.HelpBox($"Steamworks.NET {version} is installed, but the build target is {EditorUserBuildSettings.activeBuildTarget}. " +
                                        "Steam only runs in Windows, macOS and Linux builds: switch the build target (File > Build Profiles) to use it. " +
                                        "Builds for other targets use the Default multiplayer.", MessageType.Warning);
                return;
            }
            if (!OnlinePlatformSetup.SteamSupportCompiled)
            {
                EditorGUILayout.HelpBox($"Steamworks.NET {version} is installed but DeckParty.Steam hasn't compiled yet. Wait for Unity to finish " +
                                        "compiling; if this stays, check the Console for errors.", MessageType.Warning);
                return;
            }

            uint appId = settings.steam.appId == 0 ? SteamPlatformSettings.TestAppId : settings.steam.appId;
            string file = OnlinePlatformSetup.ReadAppIdFile();
            string fileNote = file == appId.ToString() ? "steam_appid.txt matches." : $"steam_appid.txt says {file ?? "nothing"} (it's rewritten when you change the App ID).";
            string testNote = appId == SteamPlatformSettings.TestAppId
                ? " Using Valve's test app 480 (Spacewar): fine for development, but players see \"Spacewar\" in Steam. Set your own App ID before release."
                : string.Empty;
            EditorGUILayout.HelpBox($"Steam is ready: Steamworks.NET {version}, App ID {appId}. {fileNote}{testNote}\n" +
                                    "Play Mode needs the Steam client running and signed in. Testing invites needs two PCs with two Steam accounts.",
                appId == SteamPlatformSettings.TestAppId ? MessageType.Warning : MessageType.Info);
            if (file != appId.ToString() && GUILayout.Button("Write steam_appid.txt")) OnlinePlatformSetup.SyncSteamAppIdFile(settings);
        }

        void DrawSteam(OnlinePlatformSettings settings)
        {
            var steam = serializedObject.FindProperty(nameof(OnlinePlatformSettings.steam));
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(steam.FindPropertyRelative(nameof(SteamPlatformSettings.appId)), new GUIContent("App ID"));
            EditorGUILayout.PropertyField(steam.FindPropertyRelative(nameof(SteamPlatformSettings.relaunchThroughSteam)), new GUIContent("Relaunch Through Steam"));
            EditorGUILayout.PropertyField(steam.FindPropertyRelative(nameof(SteamPlatformSettings.copyAppIdToDevelopmentBuilds)), new GUIContent("steam_appid.txt in Dev Builds"));
            EditorGUILayout.PropertyField(steam.FindPropertyRelative(nameof(SteamPlatformSettings.defaultVisibility)), new GUIContent("Default Lobby Visibility"));
            EditorGUILayout.PropertyField(steam.FindPropertyRelative(nameof(SteamPlatformSettings.searchDistance)), new GUIContent("Search Distance"));
            EditorGUILayout.PropertyField(steam.FindPropertyRelative(nameof(SteamPlatformSettings.useSteamNames)), new GUIContent("Use Steam Names"));
            EditorGUILayout.PropertyField(steam.FindPropertyRelative(nameof(SteamPlatformSettings.richPresenceTokens)), new GUIContent("Rich Presence Tokens"));
            EditorGUI.indentLevel--;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Steamworks Partner Site")) Application.OpenURL("https://partner.steamgames.com/apps");
            if (GUILayout.Button("Steamworks.NET Docs")) Application.OpenURL("https://steamworks.github.io/");
            EditorGUILayout.EndHorizontal();
        }

        // ------------------------------------------------------------------ Epic

        void DrawEpicStatus()
        {
            if (!OnlinePlatformSetup.EosInstalled)
            {
                EditorGUILayout.HelpBox("The EOS Plugin for Unity isn't installed, so Epic can't run: the game uses the Default multiplayer. " +
                                        "Install it (Unity downloads it from GitHub; Git and Git LFS must be installed).", MessageType.Error);
                using (new EditorGUI.DisabledScope(OnlinePlatformSetup.Installing))
                    if (GUILayout.Button(OnlinePlatformSetup.Installing ? "Installing..." : "Install the EOS Plugin for Unity"))
                        OnlinePlatformSetup.InstallEos();
                return;
            }

            var package = OnlinePlatformSetup.EosPackageInfo;
            string version = package != null ? package.version : "imported into Assets";
            if (!OnlinePlatformSetup.StandaloneTarget)
            {
                EditorGUILayout.HelpBox($"The EOS plugin {version} is installed, but the build target is {EditorUserBuildSettings.activeBuildTarget}. " +
                                        "Epic only runs in Windows, macOS and Linux builds here: switch the build target to use it. Other targets use the Default multiplayer.",
                    MessageType.Warning);
                return;
            }
            if (!OnlinePlatformSetup.EpicSupportCompiled)
            {
                EditorGUILayout.HelpBox($"The EOS plugin {version} is installed but DeckParty.Epic hasn't compiled yet. Wait for Unity to finish " +
                                        "compiling; if this stays, check the Console for errors.", MessageType.Warning);
                return;
            }
            EditorGUILayout.HelpBox($"Epic is ready to run: EOS plugin {version}. Fill in the product's ids and client credentials in " +
                                    "EOS Plugin > EOS Configuration (from the Epic Developer Portal) if you haven't. Play Mode signs in with the " +
                                    "Developer Authentication Tool when it's set below; otherwise the Multiplayer screen opens Epic's login window.",
                MessageType.Info);
            if (GUILayout.Button("Open EOS Configuration") && !OnlinePlatformSetup.OpenEosConfiguration())
                Debug.LogWarning($"[DeckParty] Couldn't open {OnlinePlatformSetup.EosConfigurationMenu}; find the EOS plugin's configuration window in the menu bar.");
        }

        void DrawEpic(OnlinePlatformSettings settings)
        {
            var epic = serializedObject.FindProperty(nameof(OnlinePlatformSettings.epic));
            EditorGUILayout.LabelField("The product's ids and client credentials live in the plugin's own window (EOS Plugin > EOS Configuration).", m_Wrap);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Signing in", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(epic.FindPropertyRelative(nameof(EpicPlatformSettings.useExchangeCode)), new GUIContent("Launcher Exchange Code"));
            EditorGUILayout.PropertyField(epic.FindPropertyRelative(nameof(EpicPlatformSettings.usePersistentLogin)), new GUIContent("Remember Login"));
            EditorGUILayout.PropertyField(epic.FindPropertyRelative(nameof(EpicPlatformSettings.useAccountPortal)), new GUIContent("Login Window"));
            EditorGUILayout.PropertyField(epic.FindPropertyRelative(nameof(EpicPlatformSettings.devAuthHost)), new GUIContent("Dev Auth Tool Host"));
            EditorGUILayout.PropertyField(epic.FindPropertyRelative(nameof(EpicPlatformSettings.devAuthCredential)), new GUIContent("Dev Auth Credential"));
            EditorGUILayout.PropertyField(epic.FindPropertyRelative(nameof(EpicPlatformSettings.allowDeviceIdFallback)), new GUIContent("Device ID Fallback"));
            EditorGUILayout.LabelField("Lobbies", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(epic.FindPropertyRelative(nameof(EpicPlatformSettings.defaultVisibility)), new GUIContent("Default Lobby Visibility"));
            EditorGUILayout.PropertyField(epic.FindPropertyRelative(nameof(EpicPlatformSettings.useEpicNames)), new GUIContent("Use Epic Names"));
            EditorGUILayout.PropertyField(epic.FindPropertyRelative(nameof(EpicPlatformSettings.lobbyBucket)), new GUIContent("Lobby Bucket"));
            EditorGUI.indentLevel--;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Epic Developer Portal")) Application.OpenURL("https://dev.epicgames.com/portal");
            if (GUILayout.Button("Developer Auth Tool")) Application.OpenURL("https://dev.epicgames.com/docs/epic-account-services/developer-authentication-tool");
            if (GUILayout.Button("EOS Plugin Docs")) Application.OpenURL("https://github.com/EOS-Contrib/eos_plugin_for_unity");
            EditorGUILayout.EndHorizontal();
        }
    }
}
