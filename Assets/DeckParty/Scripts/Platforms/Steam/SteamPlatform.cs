// Steam code only exists when Steamworks.NET is installed (DECKPARTY_STEAMWORKS, a version define in DeckParty.Steam.asmdef)
// and the build target is Windows, macOS or Linux. Otherwise this file compiles to nothing and the game uses the Default multiplayer.
#if DECKPARTY_STEAMWORKS && UNITY_STANDALONE
using System;
using System.Collections.Generic;
using System.Text;
using Steamworks;
using UnityEngine;

namespace DeckParty.Platforms.Steam
{
    /// <summary>
    /// Steam (Steamworks.NET) behind <see cref="IOnlinePlatform"/>: SteamAPI start-up and shutdown, the callback pump,
    /// lobbies (<c>SteamPlatform.Lobbies.cs</c>), friends, avatars and invites (<c>SteamPlatform.Friends.cs</c>), rich
    /// presence, and the Netcode transport (<see cref="SteamNetworkingTransport"/>).
    /// <para>
    /// This assembly only compiles when Steamworks.NET is installed and the build target is Windows, macOS or Linux
    /// (DeckParty.Steam.asmdef define constraints), so the project still builds without it; then the game runs on the
    /// Default multiplayer.
    /// </para>
    /// </summary>
    public sealed partial class SteamPlatform : IOnlinePlatform
    {
        /// <summary>Steam passes this (and the lobby id) on the command line when a player starts the game from an invite.</summary>
        const string ConnectLobbyArg = "+connect_lobby";

        /// <summary>SteamAPI is up (the transport and the pump check it before every Steam call, e.g. while quitting).</summary>
        internal static bool Running { get; private set; }

        static SteamAPIWarningMessageHook_t s_WarningHook;

        SteamPlatformSettings m_Settings;
        AppId_t m_AppId;
        bool m_Ready;
        bool m_FinalFailure;
        PlatformUser m_LocalUser;
        SteamCallbackPump m_Pump;
        string m_LaunchJoin;
        bool m_FriendsDirty;
        float m_NextFriendsEvent;
        string m_PresenceSignature;

        Callback<GameLobbyJoinRequested_t> m_LobbyJoinRequested;
        Callback<GameRichPresenceJoinRequested_t> m_RichPresenceJoinRequested;
        Callback<NewUrlLaunchParameters_t> m_NewUrlLaunch;
        Callback<PersonaStateChange_t> m_PersonaChanged;
        Callback<AvatarImageLoaded_t> m_AvatarImageLoaded;
        Callback<LobbyChatUpdate_t> m_LobbyChatUpdate;
        Callback<LobbyKicked_t> m_LobbyKicked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register()
        {
            Running = false;
            OnlinePlatforms.Register(OnlinePlatformKind.Steam, () => new SteamPlatform());
        }

        public OnlinePlatformKind Kind => OnlinePlatformKind.Steam;
        public string DisplayName => "Steam";

        public PlatformFeatures Features =>
            PlatformFeatures.Lobbies | PlatformFeatures.LobbySearch | PlatformFeatures.QuickMatch | PlatformFeatures.Friends |
            PlatformFeatures.InviteOverlay | PlatformFeatures.Avatars | PlatformFeatures.RichPresence | PlatformFeatures.Profiles;

        public bool IsReady => m_Ready && Running;
        public bool InitFailureIsFinal => m_FinalFailure;
        public PlatformUser LocalUser => m_LocalUser;

        public event Action FriendsChanged;
        public event Action<ulong> AvatarLoaded;
        public event Action<string> JoinRequested;

        // ================================================================== start-up and shutdown

        public bool Initialize(OnlinePlatformSettings settings, out string error)
        {
            error = null;
            if (IsReady) return true;
            m_Settings = settings.steam ?? new SteamPlatformSettings();
            m_AppId = new AppId_t(m_Settings.appId == 0 ? SteamPlatformSettings.TestAppId : m_Settings.appId);

            // Wrong native library for this build (e.g. a 32-bit steam_api in a 64-bit build): retrying won't help.
            if (!Packsize.Test() || !DllCheck.Test())
            {
                m_FinalFailure = true;
                error = "steam.error_library";
                Debug.LogError("[DeckParty] Steamworks.NET's native library doesn't match this build (Packsize / DllCheck failed). Reinstall Steamworks.NET.");
                return false;
            }

            ESteamAPIInitResult result;
            string message;
            try
            {
                // Release builds started outside Steam: let Steam relaunch the game (signed in, ownership checked) and quit this copy.
                if (ShouldRelaunchThroughSteam() && SteamAPI.RestartAppIfNecessary(m_AppId))
                {
                    m_FinalFailure = true;
                    error = "steam.relaunching";
                    Application.Quit();
                    return false;
                }
                result = SteamAPI.InitEx(out message);
            }
            catch (DllNotFoundException e)
            {
                m_FinalFailure = true;
                error = "steam.error_library";
                Debug.LogError($"[DeckParty] steam_api couldn't be loaded: {e.Message}");
                return false;
            }

            if (result != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
            {
                // Steam not running can be fixed by starting Steam (the Multiplayer screen retries); an outdated client can't.
                m_FinalFailure = result == ESteamAPIInitResult.k_ESteamAPIInitResult_VersionMismatch;
                Debug.LogWarning($"[DeckParty] SteamAPI.InitEx failed ({result}): {message}");
                switch (result)
                {
                    case ESteamAPIInitResult.k_ESteamAPIInitResult_NoSteamClient:
                        error = "steam.not_running";
                        break;
                    case ESteamAPIInitResult.k_ESteamAPIInitResult_VersionMismatch:
                        error = "steam.outdated";
                        break;
                    default:
                        error = Core.Err.Of("steam.init_failed", (message ?? string.Empty).Replace(Core.Err.Separator, '/'));
                        break;
                }
                return false;
            }

            Running = true;
            if (!SteamUser.BLoggedOn())
            {
                // Steam in offline mode: lobbies and networking need the Steam servers. Try again later.
                Debug.LogWarning("[DeckParty] Steam is in offline mode; Steam multiplayer needs Steam online.");
                Running = false;
                SteamAPI.Shutdown();
                error = "steam.offline";
                return false;
            }

            if (Debug.isDebugBuild)
            {
                s_WarningHook = OnSteamWarning;
                SteamClient.SetWarningMessageHook(s_WarningHook);
            }

            m_LocalUser = new PlatformUser(SteamUser.GetSteamID().m_SteamID, SteamFriends.GetPersonaName());
            m_Pump = SteamCallbackPump.Create(this);
            RegisterCallbacks();

            // Start measuring the relay network now, so the first lobby gets a ping location and the first connection is quick.
            SteamNetworkingUtils.InitRelayNetworkAccess();

            m_LaunchJoin = ParseConnectLobby(Environment.GetCommandLineArgs()) ?? ReadSteamLaunchCommandLine();
            m_Ready = true;
            m_FinalFailure = false;
            Debug.Log($"[DeckParty] Steam started: app {m_AppId.m_AppId}, user {m_LocalUser.Name} ({m_LocalUser.Id}).");
            return true;
        }

        bool ShouldRelaunchThroughSteam()
        {
            if (Application.isEditor || Debug.isDebugBuild || !m_Settings.relaunchThroughSteam) return false;
            // Every Steam user "owns" the test app, and it can't relaunch into this game anyway.
            return m_AppId.m_AppId != SteamPlatformSettings.TestAppId;
        }

        public void Shutdown()
        {
            if (!Running)
            {
                m_Ready = false;
                return;
            }

            try
            {
                LeaveLobby();
                SteamFriends.ClearRichPresence();
                m_PresenceSignature = null;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            DisposeCallbacks();
            ClearAvatars();
            if (m_Pump != null) UnityEngine.Object.Destroy(m_Pump.gameObject);
            m_Pump = null;
            m_Ready = false;
            Running = false;
            SteamAPI.Shutdown();
        }

        [AOT.MonoPInvokeCallback(typeof(SteamAPIWarningMessageHook_t))]
        static void OnSteamWarning(int severity, StringBuilder text) => Debug.LogWarning($"[Steamworks] {text}");

        // ================================================================== callbacks

        void RegisterCallbacks()
        {
            // An invite accepted (or Join Game on a friend in a lobby) while the game runs.
            m_LobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(e => RequestJoin(e.m_steamIDLobby.m_SteamID.ToString()));
            // Join Game through rich presence ("connect" key) while the game runs.
            m_RichPresenceJoinRequested = Callback<GameRichPresenceJoinRequested_t>.Create(e => RequestJoin(ParseConnectLobby(Tokenize(e.m_rgchConnect))));
            // steam://run/<app>//+connect_lobby <id> while the game runs.
            m_NewUrlLaunch = Callback<NewUrlLaunchParameters_t>.Create(_ => RequestJoin(ReadSteamLaunchCommandLine()));
            m_PersonaChanged = Callback<PersonaStateChange_t>.Create(OnPersonaChanged);
            m_AvatarImageLoaded = Callback<AvatarImageLoaded_t>.Create(OnAvatarImageLoaded);
            m_LobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
            m_LobbyKicked = Callback<LobbyKicked_t>.Create(OnLobbyKicked);
        }

        void DisposeCallbacks()
        {
            m_LobbyJoinRequested?.Dispose();
            m_RichPresenceJoinRequested?.Dispose();
            m_NewUrlLaunch?.Dispose();
            m_PersonaChanged?.Dispose();
            m_AvatarImageLoaded?.Dispose();
            m_LobbyChatUpdate?.Dispose();
            m_LobbyKicked?.Dispose();
            m_LobbyJoinRequested = null;
            m_RichPresenceJoinRequested = null;
            m_NewUrlLaunch = null;
            m_PersonaChanged = null;
            m_AvatarImageLoaded = null;
            m_LobbyChatUpdate = null;
            m_LobbyKicked = null;
        }

        /// <summary>Called by the pump every frame after SteamAPI.RunCallbacks.</summary>
        internal void Tick()
        {
            // Persona changes arrive in bursts (hundreds when the friends list loads): tell the UI at most twice a second.
            if (m_FriendsDirty && Time.unscaledTime >= m_NextFriendsEvent)
            {
                m_FriendsDirty = false;
                m_NextFriendsEvent = Time.unscaledTime + 0.5f;
                Raise(FriendsChanged);
            }
        }

        void MarkFriendsChanged() => m_FriendsDirty = true;

        void RequestJoin(string lobbyId)
        {
            if (string.IsNullOrEmpty(lobbyId)) return;
            try
            {
                JoinRequested?.Invoke(lobbyId);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public string TakeLaunchJoinRequest()
        {
            var lobby = m_LaunchJoin;
            m_LaunchJoin = null;
            return lobby;
        }

        string ReadSteamLaunchCommandLine()
        {
            if (!Running) return null;
            int length = SteamApps.GetLaunchCommandLine(out string line, 1024);
            return length > 0 ? ParseConnectLobby(Tokenize(line)) : null;
        }

        static string[] Tokenize(string line) =>
            string.IsNullOrEmpty(line) ? Array.Empty<string>() : line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        /// <summary>The lobby id after "+connect_lobby", if any (and only if it really is a lobby id).</summary>
        static string ParseConnectLobby(IReadOnlyList<string> args)
        {
            if (args == null) return null;
            for (int i = 0; i < args.Count - 1; i++)
            {
                if (!string.Equals(args[i], ConnectLobbyArg, StringComparison.OrdinalIgnoreCase)) continue;
                if (ulong.TryParse(args[i + 1].Trim('"'), out var id) && new CSteamID(id).IsLobby()) return id.ToString();
            }
            return null;
        }

        static void Raise(Action action)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        // ================================================================== rich presence

        public void SetPresence(PresenceState state, string lobbyId, int members, int maxMembers, bool joinable)
        {
            if (!IsReady) return;
            // The match reports player changes many times a minute: only talk to Steam when something visible changed.
            string signature = $"{state}|{lobbyId}|{members}|{maxMembers}|{joinable}|{Loc.LanguageCode}";
            if (signature == m_PresenceSignature) return;
            m_PresenceSignature = signature;

            // "status" shows in the friends list's game info; localized tokens (steam_display) need the tokens file uploaded first.
            string status;
            string token;
            switch (state)
            {
                case PresenceState.SinglePlayer:
                    status = Loc.Get("platform.presence_solo");
                    token = "#Status_SinglePlayer";
                    break;
                case PresenceState.InLobby:
                    status = Loc.Get("platform.presence_lobby", "count", members, "max", maxMembers);
                    token = "#Status_Lobby";
                    break;
                case PresenceState.InMatch:
                    status = Loc.Get("platform.presence_match", "count", members);
                    token = "#Status_Match";
                    break;
                default:
                    status = Loc.Get("platform.presence_menu");
                    token = "#Status_Menu";
                    break;
            }
            SteamFriends.SetRichPresence("status", status);
            if (m_Settings.richPresenceTokens)
            {
                SteamFriends.SetRichPresence("players", members.ToString());
                SteamFriends.SetRichPresence("maxplayers", maxMembers.ToString());
                SteamFriends.SetRichPresence("steam_display", token);
            }

            // Groups the party together in friends lists, and "connect" enables Join Game while seats are open.
            bool grouped = (state == PresenceState.InLobby || state == PresenceState.InMatch) && !string.IsNullOrEmpty(lobbyId);
            SteamFriends.SetRichPresence("steam_player_group", grouped ? lobbyId : string.Empty);
            SteamFriends.SetRichPresence("steam_player_group_size", grouped ? members.ToString() : string.Empty);
            SteamFriends.SetRichPresence("connect", grouped && joinable ? ConnectLobbyArg + " " + lobbyId : string.Empty);
        }

        // ================================================================== Netcode transport

        public Unity.Netcode.NetworkTransport CreateTransport(GameObject networkManagerObject)
        {
            var transport = networkManagerObject.GetComponent<SteamNetworkingTransport>();
            return transport != null ? transport : networkManagerObject.AddComponent<SteamNetworkingTransport>();
        }

        public void PrepareHostTransport(Unity.Netcode.NetworkTransport transport)
        {
            // Only players who are in our Steam lobby may open a connection: an invite-only lobby really is invite-only.
            if (transport is SteamNetworkingTransport steam) steam.ConfigureHost(IsLobbyMember);
        }

        public void PrepareClientTransport(Unity.Netcode.NetworkTransport transport, ulong hostUserId)
        {
            if (transport is SteamNetworkingTransport steam) steam.ConfigureClient(hostUserId);
        }
    }
}
#endif
