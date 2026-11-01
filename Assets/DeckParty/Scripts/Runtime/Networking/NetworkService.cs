using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
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
    /// Owns the Netcode for GameObjects NetworkManager (created at runtime, so no scene setup is needed)
    /// and handles hosting/joining over Unity Relay (internet, join codes) or direct IP (LAN / port forwarding).
    /// </summary>
    public sealed class NetworkService : MonoBehaviour
    {
        /// <summary>Bump on any breaking network change. deckparty-4: streamers, localized message keys, presence, emotes, turn timer.</summary>
        public const string ProtocolVersion = "deckparty-4";
        public const ushort DefaultPort = 7777;
        public const int MaxPlayers = 6;
        const string SessionPrefabPath = "Network/DeckPartyNetSession";

        static NetworkService s_Instance;
#if DECKPARTY_RELAY
        static string s_AuthProfile;
#endif

        readonly Dictionary<ulong, string> m_PendingNames = new Dictionary<ulong, string>();
        readonly Dictionary<ulong, byte> m_PendingStreamers = new Dictionary<ulong, byte>();

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

        /// <summary>Relay join code, or "ip:port" for direct connections. Shown in the lobby.</summary>
        public string JoinCode { get; private set; }

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
        }

        bool CanStart(out string error)
        {
            error = null;
            if (IsBusy) error = "net.busy";
            else if (IsActive) error = "net.already_in_game";
            else if (Manager != null && Manager.ShutdownInProgress) error = "net.still_closing";
            return error == null;
        }

        void PrepareConnection(string playerName)
        {
            Manager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(ProtocolVersion + "|" + AppSettings.SanitizeName(playerName) + "|" + LocalStreamer);
            m_LeavingOnPurpose = false;
            m_WasConnected = false;
            m_LastDisconnectReason = null;
            m_PendingNames.Clear();
            m_PendingStreamers.Clear();
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

        // ------------------------------------------------------------------ leaving

        public void Leave()
        {
            if (Manager == null || !Manager.IsListening || Manager.ShutdownInProgress) return;
            m_LeavingOnPurpose = true;

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

            // Payload: "protocol|name|streamer"
            string payload = request.Payload != null ? Encoding.UTF8.GetString(request.Payload) : string.Empty;
            var parts = payload.Split('|');
            string version = parts.Length > 0 ? parts[0] : string.Empty;
            string name = AppSettings.SanitizeName(parts.Length > 1 ? parts[1] : string.Empty);
            byte streamer = parts.Length > 2 && byte.TryParse(parts[2], out var parsed) ? parsed : (byte)0;

            if (request.ClientNetworkId == NetworkManager.ServerClientId)
            {
                m_PendingNames[request.ClientNetworkId] = name;
                m_PendingStreamers[request.ClientNetworkId] = streamer;
                response.Approved = true;
                return;
            }

            if (version != ProtocolVersion)
            {
                response.Approved = false;
                response.Reason = "net.version_mismatch";
                return;
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
            Mode = ConnectionMode.None;
            JoinCode = null;

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
