using System;
using System.IO;
using System.Linq;
using DeckParty.Platforms;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace DeckParty.EditorTools
{
    /// <summary>
    /// Editor side of the online platform choice: the settings asset (Resources/OnlinePlatformSettings.asset, created
    /// on first load), the steam_appid.txt the Editor needs to start Steam, and Steamworks.NET detection / install.
    /// </summary>
    [InitializeOnLoad]
    internal static class OnlinePlatformSetup
    {
        public const string SettingsPath = DeckPartyPaths.Resources + "/" + OnlinePlatformSettings.ResourceName + ".asset";

        public const string SteamworksPackage = "com.rlabrecque.steamworks.net";
        /// <summary>Steamworks.NET from GitHub (Unity Package Manager git URL), pinned to a release.</summary>
        public const string SteamworksGitUrl = "https://github.com/rlabrecque/Steamworks.NET.git?path=/com.rlabrecque.steamworks.net#2025.165.0";

        const string AppIdFile = "steam_appid.txt";

        static AddRequest s_Install;

        static OnlinePlatformSetup()
        {
            // After the import settles: make sure the asset exists and steam_appid.txt matches it.
            EditorApplication.delayCall += () =>
            {
                var settings = GetOrCreate();
                if (settings != null && settings.platform == OnlinePlatformKind.Steam) SyncSteamAppIdFile(settings);
            };
        }

        /// <summary>The project's settings asset, created with the defaults (Default platform) when missing.</summary>
        public static OnlinePlatformSettings GetOrCreate()
        {
            var settings = AssetDatabase.LoadAssetAtPath<OnlinePlatformSettings>(SettingsPath);
            if (settings != null) return settings;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isUpdating || EditorApplication.isCompiling) return null;

            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath) ?? DeckPartyPaths.Resources);
            settings = ScriptableObject.CreateInstance<OnlinePlatformSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
            AssetDatabase.SaveAssets();
            OnlinePlatformSettings.ClearCache();
            Debug.Log($"[DeckParty] Created {SettingsPath} (online platform: Default). Change it in Project Settings > Deck Party > Online Platform.");
            return settings;
        }

        // ------------------------------------------------------------------ Steamworks.NET

        /// <summary>The installed Steamworks.NET package, or null.</summary>
        public static PackageInfo SteamworksPackageInfo =>
            PackageInfo.GetAllRegisteredPackages().FirstOrDefault(p => p.name == SteamworksPackage);

        /// <summary>Steamworks.NET's code is there (as a package, or copied into Assets the old way).</summary>
        public static bool SteamworksInstalled =>
            SteamworksPackageInfo != null || FindType("Steamworks.SteamAPI") != null || AssetDatabase.FindAssets("Steamworks.NET t:asmdef").Length > 0;

        /// <summary>DeckParty.Steam compiled for the current build target (needs Steamworks.NET and Windows / macOS / Linux).</summary>
        public static bool SteamSupportCompiled => FindType("DeckParty.Platforms.Steam.SteamPlatform") != null;

        public static bool StandaloneTarget => EditorUserBuildSettings.selectedBuildTargetGroup == BuildTargetGroup.Standalone;

        public static bool Installing => s_Install != null && !s_Install.IsCompleted;

        public static void InstallSteamworks()
        {
            if (Installing) return;
            Debug.Log($"[DeckParty] Installing Steamworks.NET from {SteamworksGitUrl} (needs Git on this PC)...");
            s_Install = Client.Add(SteamworksGitUrl);
            EditorApplication.update += WatchInstall;
        }

        static void WatchInstall()
        {
            if (s_Install == null || !s_Install.IsCompleted) return;
            EditorApplication.update -= WatchInstall;
            if (s_Install.Status == StatusCode.Success)
                Debug.Log($"[DeckParty] Installed Steamworks.NET {s_Install.Result.version}. Unity recompiles; DeckParty.Steam turns on for Windows / macOS / Linux builds.");
            else
                Debug.LogError($"[DeckParty] Couldn't install Steamworks.NET: {s_Install.Error?.message}. Is Git installed and on the PATH? " +
                               $"You can also add it in Window > Package Manager > + > Install package from git URL: {SteamworksGitUrl}");
            s_Install = null;
        }

        static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName, false);
                if (type != null) return type;
            }
            return null;
        }

        // ------------------------------------------------------------------ steam_appid.txt

        /// <summary>The project folder's steam_appid.txt (what SteamAPI reads when the Editor isn't started by Steam).</summary>
        public static string AppIdFilePath => Path.Combine(Directory.GetCurrentDirectory(), AppIdFile);

        public static string ReadAppIdFile()
        {
            try
            {
                return File.Exists(AppIdFilePath) ? File.ReadAllText(AppIdFilePath).Trim() : null;
            }
            catch (IOException)
            {
                return null;
            }
        }

        /// <summary>Writes the App ID into the project folder's steam_appid.txt when it differs.</summary>
        public static void SyncSteamAppIdFile(OnlinePlatformSettings settings)
        {
            uint appId = settings.steam.appId == 0 ? SteamPlatformSettings.TestAppId : settings.steam.appId;
            if (ReadAppIdFile() == appId.ToString()) return;
            try
            {
                File.WriteAllText(AppIdFilePath, appId.ToString());
                Debug.Log($"[DeckParty] steam_appid.txt now says {appId}. Restart Unity if Steam was already started for another App ID.");
            }
            catch (IOException e)
            {
                Debug.LogWarning($"[DeckParty] Couldn't write steam_appid.txt: {e.Message}");
            }
        }

        /// <summary>Writes steam_appid.txt next to a development build so it starts Steam without the Steam launcher.</summary>
        public static void WriteAppIdNextTo(string buildFolder, uint appId)
        {
            try
            {
                File.WriteAllText(Path.Combine(buildFolder, AppIdFile), appId.ToString());
            }
            catch (IOException e)
            {
                Debug.LogWarning($"[DeckParty] Couldn't write steam_appid.txt into the build: {e.Message}");
            }
        }

        /// <summary>Release builds must not ship steam_appid.txt (it lets the game skip Steam's ownership check).</summary>
        public static void DeleteAppIdNextTo(string buildFolder)
        {
            string path = Path.Combine(buildFolder, AppIdFile);
            if (!File.Exists(path)) return;
            try
            {
                File.Delete(path);
                Debug.Log("[DeckParty] Removed steam_appid.txt from the release build.");
            }
            catch (IOException e)
            {
                Debug.LogWarning($"[DeckParty] Couldn't remove steam_appid.txt from the release build: {e.Message}");
            }
        }
    }
}
