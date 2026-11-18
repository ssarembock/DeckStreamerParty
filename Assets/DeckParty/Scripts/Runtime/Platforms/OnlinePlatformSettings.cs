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

        [Tooltip("Steam and Epic lobbies also get a short join code (a Unity Relay code) that anyone can type in to join, from any " +
                 "platform: Steam, Epic or a build without either. Players on the lobby's own platform still connect through it. " +
                 "Needs Unity Gaming Services (the same setup as the Default join codes).")]
        public bool crossPlatformCodes = true;

        public SteamPlatformSettings steam = new SteamPlatformSettings();

        public EpicPlatformSettings epic = new EpicPlatformSettings();

        [Header("Placeholders (not implemented yet)")]
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

        /// <summary>Who can join a new lobby on this platform unless the player picked something else.</summary>
        public LobbyVisibility DefaultVisibility(OnlinePlatformKind kind) =>
            kind == OnlinePlatformKind.Epic ? epic.defaultVisibility : steam.defaultVisibility;

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

    /// <summary>
    /// Epic Online Services options (the EOS Plugin for Unity, package com.playeveryware.eos). The product's ids and client
    /// credentials (Product, Sandbox, Deployment, Client ID / Secret) are entered in the plugin's own window,
    /// EOS Plugin > EOS Configuration, which stores them where its EOSManager reads them; these are the game's choices.
    /// </summary>
    [Serializable]
    public sealed class EpicPlatformSettings
    {
        [Tooltip("Builds started by the Epic Games Launcher sign in with the launcher's exchange code (no login window). " +
                 "This is how players from the Epic Games Store are signed in.")]
        public bool useExchangeCode = true;

        [Tooltip("Remember the player's Epic sign-in between runs (EOS persistent auth), so they only log in once.")]
        public bool usePersistentLogin = true;

        [Tooltip("When nothing else signs the player in, open Epic's account portal (a browser or overlay login window) " +
                 "the first time the Multiplayer screen opens.")]
        public bool useAccountPortal = true;

        [Tooltip("Editor and development builds: sign in through Epic's Developer Authentication Tool (EOS SDK Tools). " +
                 "Host and port the tool listens on.")]
        public string devAuthHost = "localhost:6547";

        [Tooltip("The credential name you created in the Developer Authentication Tool. Leave empty to skip it. A second copy " +
                 "of the game can use another one with -epicdevauth=<name> on its command line.")]
        public string devAuthCredential = "";

        [Tooltip("Without an Epic account (no launcher, login cancelled), sign in with this device's id instead: lobbies, " +
                 "join codes and matches work, but there are no Epic friends, invites or presence.")]
        public bool allowDeviceIdFallback = true;

        [Tooltip("Who can join when a player hosts (they can change it on the Multiplayer screen).")]
        public LobbyVisibility defaultVisibility = LobbyVisibility.FriendsOnly;

        [Tooltip("Players are named after their Epic display name (the name box is hidden on the Multiplayer screen).")]
        public bool useEpicNames = true;

        [Tooltip("EOS lobby bucket: games only find lobbies in the same bucket. The protocol version is added to it, so " +
                 "incompatible builds never see each other.")]
        public string lobbyBucket = "deckparty";
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
