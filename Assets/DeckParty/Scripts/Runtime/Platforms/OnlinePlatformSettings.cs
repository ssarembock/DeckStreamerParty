using System;
using UnityEngine;

namespace DeckParty.Platforms
{
    /// <summary>
    /// Which online platform this project builds for, and each platform's options. Lives at
    /// Resources/OnlinePlatformSettings.asset; edit it in Project Settings > Deck Party > Online Platform
    /// (menu Deck Party > Online Platform...). With no asset, or the platform set to Default, multiplayer works exactly
    /// as before (Unity Relay join codes and Direct IP).
    /// </summary>
    public sealed class OnlinePlatformSettings : ScriptableObject
    {
        public const string ResourceName = "OnlinePlatformSettings";

        [Tooltip("The platform this project is built for. Default = Unity Relay join codes and Direct IP (the original multiplayer).")]
        public OnlinePlatformKind platform = OnlinePlatformKind.Default;

        [Tooltip("If the platform can't start (e.g. Steam isn't running), use the Default multiplayer instead of turning online play off.")]
        public bool fallbackToDefault = true;

        [Tooltip("Play Mode in the Editor uses the Default multiplayer, whatever the platform. Multiplayer Play Mode's virtual players " +
                 "always do: one PC runs one Steam account, so a second player on the same PC can't be a second Steam user.")]
        public bool editorUsesDefault;

        [Tooltip("Keep the Direct IP / LAN card on the Multiplayer screen when a platform is active.")]
        public bool keepDirectIp = true;

        public SteamPlatformSettings steam = new SteamPlatformSettings();

        [Header("Placeholders (not implemented yet)")]
        public EpicPlatformSettings epic = new EpicPlatformSettings();
        public AppleGameCenterSettings appleGameCenter = new AppleGameCenterSettings();
        public GooglePlayGamesSettings googlePlayGames = new GooglePlayGamesSettings();

        static OnlinePlatformSettings s_Current;

        /// <summary>The project's settings (defaults when the asset doesn't exist yet).</summary>
        public static OnlinePlatformSettings Current
        {
            get
            {
                if (s_Current == null)
                {
                    s_Current = Resources.Load<OnlinePlatformSettings>(ResourceName);
                    if (s_Current == null)
                    {
                        s_Current = CreateInstance<OnlinePlatformSettings>();
                        s_Current.hideFlags = HideFlags.DontSave;
                    }
                }
                return s_Current;
            }
        }

        /// <summary>Forgets the cached asset (the editor calls this after creating or replacing it).</summary>
        public static void ClearCache() => s_Current = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_Current = null;
    }

    /// <summary>Steamworks options (Steamworks.NET).</summary>
    [Serializable]
    public sealed class SteamPlatformSettings
    {
        /// <summary>Valve's public test app (Spacewar). Fine for development; every Steam user owns it.</summary>
        public const uint TestAppId = 480;

        [Tooltip("Your Steam App ID from the Steamworks partner site. 480 (Spacewar) is Valve's test app: use it until you have your own. " +
                 "It is also written to steam_appid.txt in the project folder so the Editor can start Steam.")]
        public uint appId = TestAppId;

        [Tooltip("Release builds started outside Steam relaunch through Steam (SteamAPI.RestartAppIfNecessary), so the player is " +
                 "signed in and owns the game. Skipped in the Editor, in development builds and with the test App ID.")]
        public bool relaunchThroughSteam = true;

        [Tooltip("Development builds get a steam_appid.txt next to the executable, so they start without the Steam launcher. " +
                 "Never ship that file: release builds don't get it.")]
        public bool copyAppIdToDevelopmentBuilds = true;

        [Tooltip("Who can join when a player hosts (they can change it on the Multiplayer screen).")]
        public LobbyVisibility defaultVisibility = LobbyVisibility.FriendsOnly;

        [Tooltip("How far Find Games and Quick Match search. Default = your region and nearby ones.")]
        public LobbySearchDistance searchDistance = LobbySearchDistance.Default;

        [Tooltip("Players are named after their Steam name (the name box is hidden on the Multiplayer screen).")]
        public bool useSteamNames = true;

        [Tooltip("Rich presence uses localized tokens (#Status_Menu, #Status_Lobby, #Status_Match...) for the friends list. " +
                 "Upload the tokens file from Streamworks_Guidelines.md to the Steamworks site first; until then leave this off.")]
        public bool richPresenceTokens;
    }

    /// <summary>Placeholder for Epic Online Services: the values an EOS implementation will need.</summary>
    [Serializable]
    public sealed class EpicPlatformSettings
    {
        public string productId;
        public string sandboxId;
        public string deploymentId;
        public string clientId;
        [Tooltip("Never commit a real client secret to a public repository.")]
        public string clientSecret;
    }

    /// <summary>Placeholder for iOS Game Center.</summary>
    [Serializable]
    public sealed class AppleGameCenterSettings
    {
        [Tooltip("Game Center matchmaking uses Apple's real-time matches; leaderboards/achievements could live here too.")]
        public bool useMatchmaking = true;
    }

    /// <summary>Placeholder for Android Google Play Games.</summary>
    [Serializable]
    public sealed class GooglePlayGamesSettings
    {
        public string applicationId;
    }
}
