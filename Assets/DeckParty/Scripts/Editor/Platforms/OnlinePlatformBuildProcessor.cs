using System.IO;
using DeckParty.Platforms;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DeckParty.EditorTools
{
    /// <summary>
    /// Build checks for the online platform. Steam builds stop early when Steamworks.NET is missing (otherwise they'd
    /// quietly ship the Default multiplayer). Development builds get a steam_appid.txt next to the executable so they
    /// start without the Steam launcher; release builds never ship one.
    /// </summary>
    internal sealed class OnlinePlatformBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var settings = OnlinePlatformSetup.GetOrCreate();
            if (settings == null || settings.platform == OnlinePlatformKind.Default) return;

            if (settings.platform != OnlinePlatformKind.Steam)
            {
                Debug.LogWarning($"[DeckParty] The online platform is {OnlinePlatforms.DisplayName(settings.platform)}, which isn't implemented yet: this build uses the Default multiplayer.");
                return;
            }

            if (!IsSteamTarget(report.summary.platform))
            {
                Debug.LogWarning($"[DeckParty] Steam only runs on Windows, macOS and Linux; this {report.summary.platform} build uses the Default multiplayer.");
                return;
            }
            if (!OnlinePlatformSetup.SteamworksInstalled)
                throw new BuildFailedException("[DeckParty] The online platform is Steam but Steamworks.NET isn't installed. Install it from " +
                                               "Project Settings > Deck Party > Online Platform, or set the platform to Default.");
            if (!OnlinePlatformSetup.SteamSupportCompiled)
                throw new BuildFailedException("[DeckParty] The online platform is Steam but DeckParty.Steam didn't compile. Check the Console for errors.");

            bool development = (report.summary.options & BuildOptions.Development) != 0;
            if (!development && (settings.steam.appId == 0 || settings.steam.appId == SteamPlatformSettings.TestAppId))
                Debug.LogWarning("[DeckParty] Release build with Valve's test App ID 480 (Spacewar). Set your own App ID in Project Settings > Deck Party > Online Platform before shipping.");
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            var settings = OnlinePlatformSetup.GetOrCreate();
            if (settings == null || settings.platform != OnlinePlatformKind.Steam || !IsSteamTarget(report.summary.platform)) return;

            // Windows / Linux: the executable's folder. macOS: the folder holding the .app (where it's launched from in testing).
            string folder = Path.GetDirectoryName(report.summary.outputPath);
            if (string.IsNullOrEmpty(folder)) return;

            bool development = (report.summary.options & BuildOptions.Development) != 0;
            if (development && settings.steam.copyAppIdToDevelopmentBuilds)
            {
                uint appId = settings.steam.appId == 0 ? SteamPlatformSettings.TestAppId : settings.steam.appId;
                OnlinePlatformSetup.WriteAppIdNextTo(folder, appId);
            }
            else if (!development)
            {
                OnlinePlatformSetup.DeleteAppIdNextTo(folder);
            }
        }

        static bool IsSteamTarget(BuildTarget target) =>
            target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64 ||
            target == BuildTarget.StandaloneOSX || target == BuildTarget.StandaloneLinux64;
    }
}
