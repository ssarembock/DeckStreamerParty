using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using DeckParty.Platforms;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
#if DECKPARTY_RELAY
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
#endif

namespace DeckParty.Networking
{
    public enum ConnectionMode
    {
        None,
        Relay,
        Direct,
        /// <summary>An online platform's lobby and transport (Steam), see <see cref="OnlinePlatforms"/>.</summary>
        Platform,
    }

    /// <summary>The metadata a platform lobby carries, for searches and for players joining it.</summary>
    public static class LobbyKeys
    {
        /// <summary><see cref="NetworkService.ProtocolVersion"/>: searches only show compatible games.</summary>
        public const string Protocol = "dp_ver";
        /// <summary><see cref="PhaseOpen"/> while players can join, <see cref="PhasePlaying"/> once the match starts.</summary>
        public const string Phase = "dp_phase";
        public const string HostName = "dp_host";
        /// <summary>The character set (scenario id) and match mode, for Find Games.</summary>
        public const string Set = "dp_set";
        public const string Mode = "dp_mode";
        /// <summary>"1" for lobbies opened by Quick Match.</summary>
        public const string QuickMatch = "dp_qm";

        public const string PhaseOpen = "lobby";
        public const string PhasePlaying = "playing";
    }

    public readonly struct NetResult
    {
        public readonly bool Ok;
        public readonly string Error;

        NetResult(bool ok, string error)
        {
            Ok = ok;
            Error = error;
        }

        public static NetResult Success => new NetResult(true, null);
        public static NetResult Fail(string error) => new NetResult(false, error);
    }

    /// <summary>
    /// Owns the Netcode for GameObjects NetworkManager (created at runtime, so no scene setup is needed) and handles
    /// hosting/joining over Unity Relay (internet, join codes), direct IP (LAN / port forwarding) or an online platform's
    /// lobbies (Steam: <see cref="HostPlatformAsync"/>, <see cref="JoinPlatformLobbyAsync"/>, <see cref="QuickMatchAsync"/>).
    /// Every mode runs the same Netcode match; only the transport and how players find each other change.
    /// </summary>
    public sealed class NetworkService : MonoBehaviour
    {
        /// <summary>
        /// Bump on any breaking network change. deckparty-4: streamers, localized message keys, presence, emotes, turn timer.
        /// deckparty-5: 82-viewer roster, state format v4, the shared shop and sprite emotes.
        /// deckparty-8: V4.2 roster, Clout / Memetics Division / Bots (state format v6) and Free For All's shop gate.
        /// deckparty-9: V5 communities and card logic (state format v7), five Party House-style character sets.
        /// deckparty-10: playtest updates: stream / shop timers, Remove a Viewer (state format v8), emote limits.
        /// deckparty-11: viewer card edits: Dancing Jen (GuestType 83), group scoring, Account Manager action, community caps.
        /// deckparty-12: Crypto Enthusiast scores 0 / 1 / 4 / 9 / 16 Cash for 1 to 5 in chat.
        /// deckparty-13: online platforms (Steam): players carry a platform user id, the approval payload has a fourth part.
        /// </summary>
        public const string ProtocolVersion = "deckparty-13";
        public const ushort DefaultPort = 7777;
        public const int MaxPlayers = 6;
        const string SessionPrefabPath = "Network/DeckPartyNetSession";

        static NetworkService s_Instance;
#if DECKPARTY_RELAY
        static string s_AuthProfile;
#endif

        readonly Dictionary<ulong, string> m_PendingNames = new Dictionary<ulong, string>();
        readonly Dictionary<ulong, byte> m_PendingStreamers = new Dictionary<ulong, byte>();
        readonly Dictionary<ulong, ulong> m_PendingPlatformIds = new Dictionary<ulong, ulong>();

        /// <summary>The online platform's transport (Steam), added to the NetworkManager's object the first time it's needed.</summary>
        NetworkTransport m_PlatformTransport;
        IOnlinePlatform m_TransportPlatform;
        DeckPartyNetSession m_WatchedSession;

        /// <summary>The streamer this player picked on the select screen; sent to the host when connecting.</summary>
        public static byte LocalStreamer
        {
            get => AppSettings.LastStreamer;
            set => AppSettings.LastStreamer = value;
        }
        GameObject m_SessionPrefab;
        bool m_LeavingOnPurpose;
        bool m_WasConnected;
        string m_LastDisconnectReason;

        public static NetworkService Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    var go = new GameObject("[NetworkService]");
                    DontDestroyOnLoad(go);
                    s_Instance = go.AddComponent<NetworkService>();
                }
                return s_Instance;
            }
        }

        public static bool HasInstance => s_Instance != null;

        public static bool RelaySupported
        {
            get
            {
#if DECKPARTY_RELAY
                return true;
#else
                return false;
#endif
            }
        }

        public NetworkManager Manager { get; private set; }
        public UnityTransport Transport { get; private set; }
        public ConnectionMode Mode { get; private set; }

        /// <summary>Relay join code, "ip:port" for direct connections, or the platform lobby id. Shown in the lobby.</summary>
        public string JoinCode { get; private set; }

        /// <summary>The platform lobby this session runs in (null outside <see cref="ConnectionMode.Platform"/>).</summary>
        public string PlatformLobbyId { get; private set; }

        /// <summary>Who can join the platform lobby.</summary>
        public LobbyVisibility PlatformVisibility { get; private set; }

        /// <summary>The session runs on an online platform (Steam lobby and transport).</summary>
        public bool IsPlatformSession => Mode == ConnectionMode.Platform;

        public bool IsBusy { get; private set; }
        /// <summary>True while a session is running (a pending shutdown counts as already gone; Netcode finishes it at end of frame).</summary>
        public bool IsActive => Manager != null && Manager.IsListening && !Manager.ShutdownInProgress;
        public bool IsHost => IsActive && Manager.IsHost;

        /// <summary>Raised when a session stops. The argument is a user-facing reason, or null if the local player left on purpose.</summary>
        public event Action<string> SessionEnded;

        /// <summary>Raised on a client once the connection to the host is established.</summary>
        public event Action Connected;

        void OnDestroy()
        {
            if (s_Instance == this) s_Instance = null;
            DeckPartyNetSession.Spawned -= OnSessionSpawned;
            DeckPartyNetSession.Despawned -= OnSessionDespawned;
            WatchSession(null);
        }

        // ------------------------------------------------------------------ setup

        void EnsureManager()
        {
            if (Manager != null) return;

            // Build the NetworkManager on an inactive object so its configuration exists before Awake/OnEnable run.
            var go = new GameObject("NetworkManager");
            go.SetActive(false);
            DontDestroyOnLoad(go);

            Transport = go.AddComponent<UnityTransport>();
            Manager = go.AddComponent<NetworkManager>();
            Manager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = Transport,
                ConnectionApproval = true,
                EnableSceneManagement = true,
                TickRate = 30,
                ClientConnectionBufferTimeout = 15,
                ForceSamePrefabs = true,
            };

            go.SetActive(true);

            Transport.MaxPayloadSize = 64 * 1024;
            Transport.MaxConnectAttempts = 12;
            Transport.ConnectTimeoutMS = 1000;

            m_SessionPrefab = Resources.Load<GameObject>(SessionPrefabPath);
            if (m_SessionPrefab != null) Manager.AddNetworkPrefab(m_SessionPrefab);
            else Debug.LogError($"[DeckParty] Missing Resources/{SessionPrefabPath}.prefab. Run Deck Party > Setup > Build Everything.");

            Manager.ConnectionApprovalCallback = ApprovalCheck;
            Manager.OnClientConnectedCallback += OnClientConnected;
            Manager.OnClientDisconnectCallback += OnClientDisconnected;
            Manager.OnClientStopped += OnClientStopped;
            Manager.OnTransportFailure += OnTransportFailure;

            // Keeps the platform lobby (joinable, phase, set) and friends-list status in step with the match.
            DeckPartyNetSession.Spawned += OnSessionSpawned;
            DeckPartyNetSession.Despawned += OnSessionDespawned;
        }

        /// <summary>Netcode reads the transport when a session starts, so each mode picks its own just before starting.</summary>
        void UseTransport(NetworkTransport transport)
        {
            if (Manager.NetworkConfig.NetworkTransport != transport) Manager.NetworkConfig.NetworkTransport = transport;
        }

        /// <summary>The platform's transport, created once and reused (a different platform gets a fresh one).</summary>
        NetworkTransport PlatformTransport(IOnlinePlatform platform)
        {
            if (m_PlatformTransport != null && m_TransportPlatform == platform) return m_PlatformTransport;
            if (m_PlatformTransport != null) Destroy(m_PlatformTransport);
            m_PlatformTransport = platform.CreateTransport(Manager.gameObject);
            m_TransportPlatform = platform;
            return m_PlatformTransport;
        }

        bool UsingPlatformTransport => m_PlatformTransport != null && Manager != null && Manager.NetworkConfig.NetworkTransport == m_PlatformTransport;

        /// <summary>Waits (a few frames at most) for the previous session to finish closing, e.g. after leaving to accept an invite.</summary>
        async Task WaitForShutdownAsync()
        {
            for (int i = 0; i < 300 && Manager != null && Manager.ShutdownInProgress; i++)
                await Task.Yield();
        }

        bool CanStart(out string error)
        {
            error = null;
            if (IsBusy) error = "net.busy";
            else if (IsActive) error = "net.already_in_game";
            else if (Manager != null && Manager.ShutdownInProgress) error = "net.still_closing";
            return error == null;
        }

        /// <param name="platformUserId">The local player's platform id (their SteamID), or 0 off-platform.</param>
        void PrepareConnection(string playerName, ulong platformUserId = 0)
        {
            // Payload: "protocol|name|streamer|platformUserId"
            Manager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(ProtocolVersion + "|" + AppSettings.SanitizeName(playerName) + "|" + LocalStreamer + "|" + platformUserId);
            m_LeavingOnPurpose = false;
            m_WasConnected = false;
            m_LastDisconnectReason = null;
            m_PendingNames.Clear();
            m_PendingStreamers.Clear();
            m_PendingPlatformIds.Clear();
        }

        void SpawnSession()
        {
            if (m_SessionPrefab == null) return;
            var instance = Instantiate(m_SessionPrefab);
            instance.name = "DeckPartyNetSession";
            instance.GetComponent<NetworkObject>().Spawn(false);
        }

        // ------------------------------------------------------------------ direct IP

        public NetResult HostDirect(string playerName, ushort port)
        {
            EnsureManager();
            if (!CanStart(out var error)) return NetResult.Fail(error);

            UseTransport(Transport);
            Transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");
            PrepareConnection(playerName);

            if (!Manager.StartHost())
                return NetResult.Fail(Core.Err.Of("net.port_busy", port));

            Mode = ConnectionMode.Direct;
            JoinCode = $"{GetLocalIPv4()}:{port}";
            m_WasConnected = true;
            SpawnSession();
            return NetResult.Success;
        }

        public NetResult JoinDirect(string address, ushort port, string playerName)
        {
            EnsureManager();
            if (!CanStart(out var error)) return NetResult.Fail(error);

            if (!TryResolveIPv4(address, out var ip))
                return NetResult.Fail(Core.Err.Of("net.bad_ip", address));

            UseTransport(Transport);
            Transport.SetConnectionData(ip, port);
            PrepareConnection(playerName);

            if (!Manager.StartClient())
                return NetResult.Fail("net.client_failed");

            Mode = ConnectionMode.Direct;
            JoinCode = $"{ip}:{port}";
            return NetResult.Success;
        }

        // ------------------------------------------------------------------ Unity Relay

        public async Task<NetResult> HostRelayAsync(string playerName)
        {
            EnsureManager();
            if (!CanStart(out var error)) return NetResult.Fail(error);
#if DECKPARTY_RELAY
            IsBusy = true;
            try
            {
                await EnsureSignedInAsync();
                var allocation = await RelayService.Instance.CreateAllocationAsync(MaxPlayers - 1);
                var code = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                UseTransport(Transport);
                Transport.SetRelayServerData(allocation.ToRelayServerData("dtls"));
                PrepareConnection(playerName);

                if (!Manager.StartHost()) return NetResult.Fail("net.relay_host_failed");

                Mode = ConnectionMode.Relay;
                JoinCode = code;
                m_WasConnected = true;
                SpawnSession();
                return NetResult.Success;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DeckParty] Relay host failed: {e}");
                return NetResult.Fail(FriendlyServicesError(e, hosting: true));
            }
            finally
            {
                IsBusy = false;
            }
#else
            await Task.CompletedTask;
            return NetResult.Fail("net.no_services");
#endif
        }

        public async Task<NetResult> JoinRelayAsync(string joinCode, string playerName)
        {
            EnsureManager();
            if (!CanStart(out var error)) return NetResult.Fail(error);

            joinCode = (joinCode ?? string.Empty).Trim().ToUpperInvariant();
            if (joinCode.Length < 4) return NetResult.Fail("net.enter_code");
#if DECKPARTY_RELAY
            IsBusy = true;
            try
            {
                await EnsureSignedInAsync();
                var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

                UseTransport(Transport);
                Transport.SetRelayServerData(allocation.ToRelayServerData("dtls"));
                PrepareConnection(playerName);

                if (!Manager.StartClient()) return NetResult.Fail("net.client_failed");

                Mode = ConnectionMode.Relay;
                JoinCode = joinCode;
                return NetResult.Success;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DeckParty] Relay join failed: {e}");
                return NetResult.Fail(FriendlyServicesError(e, hosting: false));
            }
            finally
            {
                IsBusy = false;
            }
#else
            await Task.CompletedTask;
            return NetResult.Fail("net.no_services");
#endif
        }

#if DECKPARTY_RELAY
        static async Task EnsureSignedInAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                // A unique profile per process lets several copies (or Multiplayer Play Mode players) sign in on one PC.
                if (string.IsNullOrEmpty(s_AuthProfile)) s_AuthProfile = "dp" + UnityEngine.Random.Range(100000, 999999);
                var options = new InitializationOptions();
                options.SetProfile(s_AuthProfile);
                await UnityServices.InitializeAsync(options);
            }

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
#endif

        static string FriendlyServicesError(Exception e, bool hosting)
        {
            var message = e.Message ?? string.Empty;
            var lower = message.ToLowerInvariant();
            if (lower.Contains("project") || lower.Contains("cloud") || lower.Contains("unauthorized") || lower.Contains("forbidden"))
                return "net.not_set_up";
            if (!hosting && (lower.Contains("not found") || lower.Contains("join code") || lower.Contains("invalid")))
                return "net.code_not_found";
            if (lower.Contains("network") || lower.Contains("timeout") || lower.Contains("connect"))
                return "net.unreachable";
            return Core.Err.Of(hosting ? "net.host_failed" : "net.join_failed", message.Replace(Core.Err.Separator, '/'));
        }

        // ------------------------------------------------------------------ online platform (Steam)

        /// <summary>The player's name for a platform session: their platform name (Steam name) when the settings ask for it.</summary>
        public static string PlatformPlayerName(string typedName)
        {
            var platform = OnlinePlatforms.Current;
            if (platform != null && OnlinePlatforms.UsePlatformNames && !string.IsNullOrWhiteSpace(platform.LocalUser.Name))
                return AppSettings.SanitizeName(platform.LocalUser.Name);
            return AppSettings.SanitizeName(typedName);
        }

        /// <summary>
        /// Opens a platform lobby (Steam) and hosts the match in it. Friends join through invites or the friends list;
        /// public lobbies also show up in Find Games and Quick Match.
        /// </summary>
        public async Task<NetResult> HostPlatformAsync(string playerName, LobbyVisibility visibility, bool quickMatch = false)
        {
            var platform = OnlinePlatforms.Current;
            if (platform == null) return NetResult.Fail("platform.offline");
            EnsureManager();
            await WaitForShutdownAsync();
            if (!CanStart(out var error)) return NetResult.Fail(error);

            IsBusy = true;
            bool lobbyOpen = false;
            try
            {
                string name = PlatformPlayerName(playerName);
                var data = new Dictionary<string, string>
                {
                    [LobbyKeys.Protocol] = ProtocolVersion,
                    [LobbyKeys.Phase] = LobbyKeys.PhaseOpen,
                    [LobbyKeys.HostName] = name,
                    [LobbyKeys.Set] = MatchSettings.Default.ScenarioId.ToString(),
                    [LobbyKeys.Mode] = ((int)MatchSettings.Default.Mode).ToString(),
                    [LobbyKeys.QuickMatch] = quickMatch ? "1" : "0",
                };
                var lobby = await platform.CreateLobbyAsync(visibility, MaxPlayers, data);
                if (!lobby.Ok) return NetResult.Fail(lobby.Error);
                lobbyOpen = true;

                var transport = PlatformTransport(platform);
                platform.PrepareHostTransport(transport);
                UseTransport(transport);
                PrepareConnection(name, platform.LocalUser.Id);
                if (!Manager.StartHost()) return NetResult.Fail("platform.host_failed");

                Mode = ConnectionMode.Platform;
                PlatformLobbyId = lobby.Value.Id;
                PlatformVisibility = visibility;
                JoinCode = lobby.Value.Id;
                lobbyOpen = false;
                m_WasConnected = true;
                SpawnSession();
                SyncPlatformLobby();
                return NetResult.Success;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DeckParty] {platform.DisplayName} host failed: {e}");
                return NetResult.Fail(Core.Err.Of("net.host_failed", (e.Message ?? string.Empty).Replace(Core.Err.Separator, '/')));
            }
            finally
            {
                if (lobbyOpen) platform.LeaveLobby();
                IsBusy = false;
            }
        }

        /// <summary>Joins a platform lobby (an invite, Join Game, Find Games or a pasted lobby id) and connects to its host.</summary>
        public async Task<NetResult> JoinPlatformLobbyAsync(string lobbyId, string playerName)
        {
            var platform = OnlinePlatforms.Current;
            if (platform == null) return NetResult.Fail("platform.offline");
            lobbyId = (lobbyId ?? string.Empty).Trim();
            if (lobbyId.Length == 0) return NetResult.Fail("platform.enter_lobby");
            EnsureManager();
            await WaitForShutdownAsync();
            if (!CanStart(out var error)) return NetResult.Fail(error);

            IsBusy = true;
            bool lobbyOpen = false;
            try
            {
                var joined = await platform.JoinLobbyAsync(lobbyId);
                if (!joined.Ok) return NetResult.Fail(joined.Error);
                lobbyOpen = true;

                var lobby = joined.Value;
                if (lobby.Get(LobbyKeys.Protocol) != ProtocolVersion) return NetResult.Fail("net.version_mismatch");
                if (lobby.Get(LobbyKeys.Phase) == LobbyKeys.PhasePlaying) return NetResult.Fail("net.already_started");
                if (lobby.HostUserId == 0 || lobby.HostUserId == platform.LocalUser.Id) return NetResult.Fail("platform.no_host");

                var transport = PlatformTransport(platform);
                platform.PrepareClientTransport(transport, lobby.HostUserId);
                UseTransport(transport);
                PrepareConnection(PlatformPlayerName(playerName), platform.LocalUser.Id);
                if (!Manager.StartClient()) return NetResult.Fail("net.client_failed");

                Mode = ConnectionMode.Platform;
                PlatformLobbyId = lobby.Id;
                PlatformVisibility = lobby.Visibility;
                JoinCode = lobby.Id;
                lobbyOpen = false;
                return NetResult.Success;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DeckParty] {platform.DisplayName} join failed: {e}");
                return NetResult.Fail(Core.Err.Of("net.join_failed", (e.Message ?? string.Empty).Replace(Core.Err.Separator, '/')));
            }
            finally
            {
                if (lobbyOpen) platform.LeaveLobby();
                IsBusy = false;
            }
        }

        /// <summary>Public lobbies with a free seat that run this version and haven't started (Find Games, Quick Match).</summary>
        public async Task<PlatformResult<List<LobbyListing>>> FindPlatformLobbiesAsync()
        {
            var platform = OnlinePlatforms.Current;
            if (platform == null) return PlatformResult<List<LobbyListing>>.Fail("platform.offline");
            var search = new LobbySearch { Distance = OnlinePlatformSettings.Current.steam.searchDistance };
            search.Matches[LobbyKeys.Protocol] = ProtocolVersion;
            search.Matches[LobbyKeys.Phase] = LobbyKeys.PhaseOpen;
            try
            {
                return await platform.FindLobbiesAsync(search);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DeckParty] {platform.DisplayName} lobby search failed: {e}");
                return PlatformResult<List<LobbyListing>>.Fail("platform.search_failed");
            }
        }

        /// <summary>
        /// Quick Match: joins the best public game that's still in its lobby (low ping first, then the fullest, so games
        /// fill up and start), or opens a public lobby for others to find when there isn't one.
        /// </summary>
        public async Task<NetResult> QuickMatchAsync(string playerName)
        {
            if (OnlinePlatforms.Current == null) return NetResult.Fail("platform.offline");
            if (!CanStart(out var error)) return NetResult.Fail(error);

            var found = await FindPlatformLobbiesAsync();
            if (found.Ok)
            {
                var candidates = new List<LobbyListing>(found.Value);
                candidates.Sort((a, b) =>
                {
                    int bandA = PingBand(a.PingMs), bandB = PingBand(b.PingMs);
                    if (bandA != bandB) return bandA.CompareTo(bandB);
                    if (a.Members != b.Members) return b.Members.CompareTo(a.Members);
                    return (a.PingMs < 0 ? int.MaxValue : a.PingMs).CompareTo(b.PingMs < 0 ? int.MaxValue : b.PingMs);
                });

                int tries = 0;
                foreach (var listing in candidates)
                {
                    if (listing.Members >= listing.MaxMembers) continue;
                    var joined = await JoinPlatformLobbyAsync(listing.Id, playerName);
                    if (joined.Ok) return joined;
                    if (++tries >= 4) break;
                }
            }
            return await HostPlatformAsync(playerName, LobbyVisibility.Public, quickMatch: true);
        }

        /// <summary>Quick Match prefers games under 120 ms, then under 250 ms, then the rest.</summary>
        static int PingBand(int ping) => ping < 0 ? 1 : ping <= 120 ? 0 : ping <= 250 ? 1 : 2;

        // Session -> platform: lobby joinable / phase / set (host) and friends-list status (everyone).

        void OnSessionSpawned(DeckPartyNetSession session)
        {
            WatchSession(session);
            SyncPlatformLobby();
        }

        void OnSessionDespawned() => WatchSession(null);

        void WatchSession(DeckPartyNetSession session)
        {
            if (m_WatchedSession == session) return;
            if (m_WatchedSession != null)
            {
                m_WatchedSession.PhaseChanged -= OnWatchedPhaseChanged;
                m_WatchedSession.SettingsChanged -= SyncPlatformLobby;
                m_WatchedSession.PlayersChanged -= SyncPlatformLobby;
            }
            m_WatchedSession = session;
            if (session != null)
            {
                session.PhaseChanged += OnWatchedPhaseChanged;
                session.SettingsChanged += SyncPlatformLobby;
                session.PlayersChanged += SyncPlatformLobby;
            }
        }

        void OnWatchedPhaseChanged(MatchPhase phase) => SyncPlatformLobby();

        void SyncPlatformLobby()
        {
            if (Mode != ConnectionMode.Platform) return;
            var platform = OnlinePlatforms.Current;
            var session = DeckPartyNetSession.Instance;
            if (platform == null || session == null || !session.IsSpawned) return;

            bool open = session.Phase == MatchPhase.Lobby;
            int members = session.ConnectedCount;
            bool joinable = open && members < MaxPlayers;
            if (IsHost)
            {
                var settings = session.Settings;
                platform.SetLobbyJoinable(joinable);
                platform.SetLobbyData(LobbyKeys.Phase, open ? LobbyKeys.PhaseOpen : LobbyKeys.PhasePlaying);
                platform.SetLobbyData(LobbyKeys.Set, settings.ScenarioId.ToString());
                platform.SetLobbyData(LobbyKeys.Mode, ((int)settings.Mode).ToString());
            }
            OnlinePlatforms.SetPresence(open ? PresenceState.InLobby : PresenceState.InMatch, PlatformLobbyId, members, MaxPlayers,
                joinable && PlatformVisibility != LobbyVisibility.InviteOnly);
        }

        /// <summary>Server: the platform user id (SteamID) a connecting client sent, checked against the lobby's members.</summary>
        public ulong TakePendingPlatformId(ulong clientId)
        {
            if (!m_PendingPlatformIds.TryGetValue(clientId, out var id)) return 0;
            m_PendingPlatformIds.Remove(clientId);
            return id;
        }

        bool PlatformIdInUse(ulong platformId)
        {
            foreach (var pending in m_PendingPlatformIds.Values)
                if (pending == platformId) return true;
            var session = DeckPartyNetSession.Instance;
            if (session == null || session.Players == null) return false;
            for (int i = 0; i < session.Players.Count; i++)
                if (session.Players[i].Connected && session.Players[i].PlatformUserId == platformId) return true;
            return false;
        }

        // ------------------------------------------------------------------ leaving

        public void Leave()
        {
            if (Manager == null || !Manager.IsListening || Manager.ShutdownInProgress) return;
            m_LeavingOnPurpose = true;

            if (Manager.IsServer && Mode == ConnectionMode.Platform)
            {
                // Steam hands a lobby to another member when its owner leaves: close it to new players first.
                var platform = OnlinePlatforms.Current;
                platform?.SetLobbyJoinable(false);
                platform?.SetLobbyData(LobbyKeys.Phase, LobbyKeys.PhasePlaying);
            }

            if (Manager.IsServer)
            {
                var clients = new List<ulong>(Manager.ConnectedClientsIds);
                foreach (var id in clients)
                    if (id != NetworkManager.ServerClientId) Manager.DisconnectClient(id, "net.host_closed");
            }

            Manager.Shutdown();
        }

        // ------------------------------------------------------------------ callbacks

        void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            response.CreatePlayerObject = false;
            response.Pending = false;

            // Payload: "protocol|name|streamer|platformUserId"
            string payload = request.Payload != null ? Encoding.UTF8.GetString(request.Payload) : string.Empty;
            var parts = payload.Split('|');
            string version = parts.Length > 0 ? parts[0] : string.Empty;
            string name = AppSettings.SanitizeName(parts.Length > 1 ? parts[1] : string.Empty);
            byte streamer = parts.Length > 2 && byte.TryParse(parts[2], out var parsed) ? parsed : (byte)0;
            ulong platformId = parts.Length > 3 && ulong.TryParse(parts[3], out var parsedId) ? parsedId : 0;

            // On a platform the host names players from the platform (their Steam name), not from what the client typed.
            bool platformSession = UsingPlatformTransport;
            var platform = platformSession ? OnlinePlatforms.Current : null;
            if (platform != null && platformId != 0 && OnlinePlatforms.UsePlatformNames)
            {
                string platformName = platform.GetUserName(platformId);
                if (!string.IsNullOrWhiteSpace(platformName)) name = AppSettings.SanitizeName(platformName);
            }

            if (request.ClientNetworkId == NetworkManager.ServerClientId)
            {
                m_PendingNames[request.ClientNetworkId] = name;
                m_PendingStreamers[request.ClientNetworkId] = streamer;
                if (platformSession) m_PendingPlatformIds[request.ClientNetworkId] = platformId;
                response.Approved = true;
                return;
            }

            if (version != ProtocolVersion)
            {
                response.Approved = false;
                response.Reason = "net.version_mismatch";
                return;
            }

            if (platformSession)
            {
                // The transport only lets lobby members connect; this also stops one account joining twice.
                if (platform == null || platformId == 0 || !platform.IsLobbyMember(platformId))
                {
                    response.Approved = false;
                    response.Reason = "platform.not_in_lobby";
                    return;
                }
                if (PlatformIdInUse(platformId))
                {
                    response.Approved = false;
                    response.Reason = "platform.already_connected";
                    return;
                }
            }

            var session = DeckPartyNetSession.Instance;
            if (session != null && session.Phase != MatchPhase.Lobby)
            {
                response.Approved = false;
                response.Reason = "net.already_started";
                return;
            }

            if (Manager.ConnectedClientsIds.Count >= MaxPlayers)
            {
                response.Approved = false;
                response.Reason = Core.Err.Of("net.full", MaxPlayers);
                return;
            }

            m_PendingNames[request.ClientNetworkId] = name;
            m_PendingStreamers[request.ClientNetworkId] = streamer;
            if (platformSession) m_PendingPlatformIds[request.ClientNetworkId] = platformId;
            response.Approved = true;
        }

        /// <summary>Server: the streamer a connecting client picked (a StreamerType, or the Random pick).</summary>
        public byte TakePendingStreamer(ulong clientId)
        {
            if (!m_PendingStreamers.TryGetValue(clientId, out var streamer)) return 0;
            m_PendingStreamers.Remove(clientId);
            return streamer;
        }

        /// <summary>Server: the name a connecting client sent in its approval payload.</summary>
        public string TakePendingName(ulong clientId)
        {
            if (!m_PendingNames.TryGetValue(clientId, out var name)) return null;
            m_PendingNames.Remove(clientId);
            return name;
        }

        void OnClientConnected(ulong clientId)
        {
            if (Manager.IsServer || clientId != Manager.LocalClientId) return;
            m_WasConnected = true;
            Connected?.Invoke();
        }

        void OnClientDisconnected(ulong clientId)
        {
            if (Manager.IsServer) return;
            if (!string.IsNullOrEmpty(Manager.DisconnectReason)) m_LastDisconnectReason = Manager.DisconnectReason;
        }

        void OnTransportFailure()
        {
            m_LastDisconnectReason = "net.transport_failed";
        }

        void OnClientStopped(bool wasHost)
        {
            string reason = null;
            if (!m_LeavingOnPurpose)
            {
                if (!string.IsNullOrEmpty(m_LastDisconnectReason)) reason = m_LastDisconnectReason;
                else if (!string.IsNullOrEmpty(Manager.DisconnectReason)) reason = Manager.DisconnectReason;
                else reason = m_WasConnected ? "net.lost_connection" : "net.could_not_connect";
            }

            m_LeavingOnPurpose = false;
            m_WasConnected = false;
            m_LastDisconnectReason = null;
            m_PendingNames.Clear();
            m_PendingStreamers.Clear();
            m_PendingPlatformIds.Clear();

            if (Mode == ConnectionMode.Platform)
            {
                // Leave the platform lobby too (the host leaving lets Steam close it), and go back to "in the menus".
                OnlinePlatforms.Current?.LeaveLobby();
                OnlinePlatforms.SetPresence(PresenceState.Menu);
            }
            Mode = ConnectionMode.None;
            JoinCode = null;
            PlatformLobbyId = null;

            SessionEnded?.Invoke(reason);

            if (reason != null && SceneManager.GetActiveScene().name != SceneNames.MainMenu)
                SceneFlow.GoToMainMenu(reason, openMultiplayer: true);
        }

        // ------------------------------------------------------------------ helpers

        static bool TryResolveIPv4(string address, out string ip)
        {
            ip = null;
            address = (address ?? string.Empty).Trim();
            if (address.Length == 0) return false;
            if (string.Equals(address, "localhost", StringComparison.OrdinalIgnoreCase)) { ip = "127.0.0.1"; return true; }

            if (IPAddress.TryParse(address, out var parsed))
            {
                if (parsed.AddressFamily != AddressFamily.InterNetwork) return false;
                ip = parsed.ToString();
                return true;
            }

            try
            {
                foreach (var candidate in Dns.GetHostAddresses(address))
                {
                    if (candidate.AddressFamily != AddressFamily.InterNetwork) continue;
                    ip = candidate.ToString();
                    return true;
                }
            }
            catch (Exception)
            {
                // Unresolvable host name.
            }
            return false;
        }

        /// <summary>Best guess at this machine's LAN address, for showing to friends on the same network.</summary>
        public static string GetLocalIPv4()
        {
            string best = null;
            int bestScore = -1;
            try
            {
                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus != OperationalStatus.Up || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    foreach (var unicast in nic.GetIPProperties().UnicastAddresses)
                    {
                        if (unicast.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                        var text = unicast.Address.ToString();
                        int score = text.StartsWith("192.168.") ? 3 : text.StartsWith("10.") ? 2 : text.StartsWith("172.") ? 1 : 0;
                        if (score > bestScore)
                        {
                            best = text;
                            bestScore = score;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Some platforms don't expose interfaces.
            }
            return best ?? "127.0.0.1";
        }
    }
}
