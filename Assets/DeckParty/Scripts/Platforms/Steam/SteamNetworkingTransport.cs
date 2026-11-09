// Steam code only exists when Steamworks.NET is installed (DECKPARTY_STEAMWORKS, a version define in DeckParty.Steam.asmdef)
// and the build target is Windows, macOS or Linux. Otherwise this file compiles to nothing and the game uses the Default multiplayer.
#if DECKPARTY_STEAMWORKS && UNITY_STANDALONE
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Steamworks;
using Unity.Netcode;
using UnityEngine;

namespace DeckParty.Platforms.Steam
{
    /// <summary>
    /// A Netcode for GameObjects transport over Steam Networking Sockets (ISteamNetworkingSockets): peer-to-peer by
    /// SteamID, routed through Steam Datagram Relay. That means NAT traversal, no IP addresses exposed, encryption and
    /// Steam-authenticated identities, with no servers of our own.
    /// <list type="bullet">
    /// <item>Host: a P2P listen socket. Incoming connections are only accepted from members of the host's Steam lobby
    /// (checked again for a few seconds, since the lobby join and the connection race each other).</item>
    /// <item>Client: one connection to the host's SteamID.</item>
    /// <item>Netcode client ids on the host are the connection handles (never 0); the host is <see cref="ServerClientId"/> 0.</item>
    /// <item>Reliable deliveries map to Steam's reliable messages, unreliable ones to unreliable. Nagle is off because
    /// Netcode already batches one send per tick.</item>
    /// <item>Disconnects linger, so Netcode's "why you were disconnected" message gets through before the connection closes.</item>
    /// </list>
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public sealed class SteamNetworkingTransport : NetworkTransport
    {
        const int VirtualPort = 0;
        const int MaxMessagesPerReceive = 256;
        /// <summary>How long the host waits for a connecting player to show up in its lobby (under Steam's 10 s connect timeout).</summary>
        const float MemberWaitSeconds = 6f;
        /// <summary>How long the listen socket stays open after shutdown so lingering disconnects can flush.</summary>
        const float LingerSeconds = 1.5f;
        const int EndReasonNotInLobby = (int)ESteamNetConnectionEnd.k_ESteamNetConnectionEnd_App_Min + 1;
        const int EndReasonClosed = (int)ESteamNetConnectionEnd.k_ESteamNetConnectionEnd_App_Generic;

        struct Peer
        {
            public HSteamNetConnection Connection;
            public ulong SteamId;
            public bool Announced;
        }

        struct PendingAccept
        {
            public HSteamNetConnection Connection;
            public ulong SteamId;
            public float Deadline;
        }

        struct QueuedEvent
        {
            public NetworkEvent Type;
            public ulong ClientId;
            public byte[] Payload;
        }

        Func<ulong, bool> m_AllowPeer;
        ulong m_HostSteamId;
        bool m_IsServer;
        bool m_Running;
        bool m_ServerAnnounced;

        HSteamListenSocket m_ListenSocket = HSteamListenSocket.Invalid;
        HSteamNetPollGroup m_PollGroup = HSteamNetPollGroup.Invalid;
        HSteamNetConnection m_ServerConnection = HSteamNetConnection.Invalid;
        HSteamListenSocket m_LingeringSocket = HSteamListenSocket.Invalid;
        float m_LingerUntil;

        readonly Dictionary<ulong, Peer> m_Peers = new Dictionary<ulong, Peer>();
        readonly List<PendingAccept> m_PendingAccepts = new List<PendingAccept>();
        readonly Queue<QueuedEvent> m_Events = new Queue<QueuedEvent>();
        readonly IntPtr[] m_Messages = new IntPtr[MaxMessagesPerReceive];
        int m_MessageCount;
        int m_MessageIndex;
        byte[] m_ReceiveBuffer = new byte[64 * 1024];
        bool m_WarnedSendLimit;
        /// <summary>Host: a player was disconnected with lingering this session (the listen socket must stay open a moment).</summary>
        bool m_LingerPending;

        Callback<SteamNetConnectionStatusChangedCallback_t> m_StatusChanged;

        public override ulong ServerClientId => 0;

        /// <summary>Host: who may connect (the lobby's members). Set by <see cref="SteamPlatform.PrepareHostTransport"/>.</summary>
        public void ConfigureHost(Func<ulong, bool> allowPeer) => m_AllowPeer = allowPeer;

        /// <summary>Client: the SteamID hosting the match. Set by <see cref="SteamPlatform.PrepareClientTransport"/>.</summary>
        public void ConfigureClient(ulong hostSteamId) => m_HostSteamId = hostSteamId;

        // ================================================================== lifecycle

        public override void Initialize(NetworkManager networkManager = null)
        {
            if (m_StatusChanged == null)
                m_StatusChanged = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnConnectionStatusChanged);
        }

        public override bool StartServer()
        {
            if (!SteamPlatform.Running) return false;
            CloseLingeringSocket();
            ResetState();
            m_IsServer = true;

            m_ListenSocket = SteamNetworkingSockets.CreateListenSocketP2P(VirtualPort, 0, null);
            if (m_ListenSocket == HSteamListenSocket.Invalid)
            {
                Debug.LogError("[DeckParty] Steam: couldn't open a P2P listen socket.");
                return false;
            }
            m_PollGroup = SteamNetworkingSockets.CreatePollGroup();
            m_Running = true;
            return true;
        }

        public override bool StartClient()
        {
            if (!SteamPlatform.Running || m_HostSteamId == 0) return false;
            ResetState();
            m_IsServer = false;

            var identity = new SteamNetworkingIdentity();
            identity.SetSteamID64(m_HostSteamId);
            m_ServerConnection = SteamNetworkingSockets.ConnectP2P(ref identity, VirtualPort, 0, null);
            if (m_ServerConnection == HSteamNetConnection.Invalid)
            {
                Debug.LogError("[DeckParty] Steam: couldn't start a P2P connection to the host.");
                return false;
            }
            m_Running = true;
            return true;
        }

        public override void Shutdown()
        {
            ReleaseReceivedMessages();
            bool wasRunning = m_Running;
            m_Running = false;

            if (wasRunning && SteamPlatform.Running)
            {
                bool lingering = m_LingerPending;
                foreach (var peer in m_Peers.Values)
                {
                    CloseGracefully(peer.Connection, "The host closed the game");
                    lingering = true;
                }
                foreach (var pending in m_PendingAccepts)
                    SteamNetworkingSockets.CloseConnection(pending.Connection, EndReasonClosed, "The host closed the game", false);
                if (m_ServerConnection != HSteamNetConnection.Invalid) CloseGracefully(m_ServerConnection, "Left the game");

                if (m_PollGroup != HSteamNetPollGroup.Invalid) SteamNetworkingSockets.DestroyPollGroup(m_PollGroup);
                if (m_ListenSocket != HSteamListenSocket.Invalid)
                {
                    // Closing the listen socket drops its connections at once; give lingering ones a moment to flush first.
                    if (lingering)
                    {
                        CloseLingeringSocket();
                        m_LingeringSocket = m_ListenSocket;
                        m_LingerUntil = Time.unscaledTime + LingerSeconds;
                    }
                    else
                    {
                        SteamNetworkingSockets.CloseListenSocket(m_ListenSocket);
                    }
                }
            }

            m_ListenSocket = HSteamListenSocket.Invalid;
            m_PollGroup = HSteamNetPollGroup.Invalid;
            m_ServerConnection = HSteamNetConnection.Invalid;
            m_Peers.Clear();
            m_PendingAccepts.Clear();
            m_Events.Clear();
            m_AllowPeer = null;
        }

        void ResetState()
        {
            ReleaseReceivedMessages();
            m_Peers.Clear();
            m_PendingAccepts.Clear();
            m_Events.Clear();
            m_ServerAnnounced = false;
            m_WarnedSendLimit = false;
            m_LingerPending = false;
        }

        void Update()
        {
            if (m_LingeringSocket != HSteamListenSocket.Invalid && Time.unscaledTime >= m_LingerUntil) CloseLingeringSocket();
        }

        void CloseLingeringSocket()
        {
            if (m_LingeringSocket == HSteamListenSocket.Invalid) return;
            if (SteamPlatform.Running) SteamNetworkingSockets.CloseListenSocket(m_LingeringSocket);
            m_LingeringSocket = HSteamListenSocket.Invalid;
        }

        void OnDestroy()
        {
            Shutdown();
            CloseLingeringSocket();
            m_StatusChanged?.Dispose();
            m_StatusChanged = null;
        }

        /// <summary>
        /// Netcode calls this right before it polls: run Steam's callbacks now so connection changes (Connected, closed)
        /// are queued before this frame's messages are read, and settle connections waiting for lobby membership.
        /// </summary>
        protected override void OnEarlyUpdate()
        {
            if (!SteamPlatform.Running) return;
            SteamAPI.RunCallbacks();
            if (m_Running && m_IsServer) CheckPendingAccepts();
        }

        // ================================================================== connection changes

        void OnConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t change)
        {
            if (!m_Running) return;
            var connection = change.m_hConn;
            var info = change.m_info;

            switch (info.m_eState)
            {
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting:
                    // Someone is connecting to our listen socket (our own outgoing connection reports Connecting too).
                    if (m_IsServer && info.m_hListenSocket == m_ListenSocket)
                    {
                        ulong steamId = info.m_identityRemote.GetSteamID64();
                        if (m_AllowPeer == null || m_AllowPeer(steamId)) Accept(connection, steamId);
                        else m_PendingAccepts.Add(new PendingAccept { Connection = connection, SteamId = steamId, Deadline = Time.unscaledTime + MemberWaitSeconds });
                    }
                    break;

                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
                    if (m_IsServer)
                    {
                        ulong id = Id(connection);
                        if (m_Peers.TryGetValue(id, out var peer) && !peer.Announced) Announce(id, peer);
                    }
                    else if (connection == m_ServerConnection && !m_ServerAnnounced)
                    {
                        m_ServerAnnounced = true;
                        Enqueue(NetworkEvent.Connect, ServerClientId);
                    }
                    break;

                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
                    OnClosed(connection, info);
                    break;
            }
        }

        void Accept(HSteamNetConnection connection, ulong steamId)
        {
            var result = SteamNetworkingSockets.AcceptConnection(connection);
            if (result != EResult.k_EResultOK)
            {
                Debug.LogWarning($"[DeckParty] Steam: couldn't accept a connection from {steamId} ({result}).");
                SteamNetworkingSockets.CloseConnection(connection, EndReasonClosed, "Couldn't accept", false);
                return;
            }
            SteamNetworkingSockets.SetConnectionPollGroup(connection, m_PollGroup);
            m_Peers[Id(connection)] = new Peer { Connection = connection, SteamId = steamId };
        }

        void Announce(ulong id, Peer peer)
        {
            peer.Announced = true;
            m_Peers[id] = peer;
            Enqueue(NetworkEvent.Connect, id);
        }

        /// <summary>Connections waiting for their player to appear in the lobby: accept them when they do, refuse them after a few seconds.</summary>
        void CheckPendingAccepts()
        {
            for (int i = m_PendingAccepts.Count - 1; i >= 0; i--)
            {
                var pending = m_PendingAccepts[i];
                if (m_AllowPeer == null || m_AllowPeer(pending.SteamId))
                {
                    m_PendingAccepts.RemoveAt(i);
                    Accept(pending.Connection, pending.SteamId);
                }
                else if (Time.unscaledTime >= pending.Deadline)
                {
                    m_PendingAccepts.RemoveAt(i);
                    Debug.LogWarning($"[DeckParty] Steam: refused a connection from {pending.SteamId}, who isn't in the lobby.");
                    SteamNetworkingSockets.CloseConnection(pending.Connection, EndReasonNotInLobby, "Not in the lobby", false);
                }
            }
        }

        void OnClosed(HSteamNetConnection connection, SteamNetConnectionInfo_t info)
        {
            ulong id = Id(connection);
            if (m_IsServer)
            {
                for (int i = m_PendingAccepts.Count - 1; i >= 0; i--)
                    if (m_PendingAccepts[i].Connection == connection) m_PendingAccepts.RemoveAt(i);
                if (m_Peers.TryGetValue(id, out var peer))
                {
                    m_Peers.Remove(id);
                    if (peer.Announced) Enqueue(NetworkEvent.Disconnect, id);
                }
            }
            else if (connection == m_ServerConnection)
            {
                m_ServerConnection = HSteamNetConnection.Invalid;
                SetDisconnectEvent(MapEnd(info), info.m_szEndDebug);
                // Also covers a connection that never got through (the host is gone, or refused us).
                Enqueue(NetworkEvent.Disconnect, ServerClientId);
            }

            // Closed by the other side or by a problem: we still have to release the handle.
            SteamNetworkingSockets.CloseConnection(connection, 0, null, false);
        }

        static DisconnectEvents MapEnd(SteamNetConnectionInfo_t info)
        {
            if (info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer) return DisconnectEvents.ClosedByRemote;
            int reason = info.m_eEndReason;
            if (reason == (int)ESteamNetConnectionEnd.k_ESteamNetConnectionEnd_Remote_Timeout ||
                reason == (int)ESteamNetConnectionEnd.k_ESteamNetConnectionEnd_Misc_Timeout)
                return DisconnectEvents.ProtocolTimeout;
            return DisconnectEvents.ProtocolError;
        }

        // ================================================================== Netcode events

        public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
        {
            receiveTime = Time.realtimeSinceStartup;
            if (m_Events.Count > 0)
            {
                var queued = m_Events.Dequeue();
                clientId = queued.ClientId;
                payload = queued.Payload != null ? new ArraySegment<byte>(queued.Payload) : default;
                return queued.Type;
            }

            clientId = 0;
            payload = default;
            if (!m_Running || !SteamPlatform.Running) return NetworkEvent.Nothing;

            while (true)
            {
                if (m_MessageIndex >= m_MessageCount)
                {
                    m_MessageIndex = 0;
                    m_MessageCount = Receive();
                    if (m_MessageCount <= 0)
                    {
                        m_MessageCount = 0;
                        return NetworkEvent.Nothing;
                    }
                }

                IntPtr pointer = m_Messages[m_MessageIndex];
                m_Messages[m_MessageIndex++] = IntPtr.Zero;
                var message = SteamNetworkingMessage_t.FromIntPtr(pointer);
                int size = message.m_cbSize;
                var connection = message.m_conn;
                if (size > m_ReceiveBuffer.Length) m_ReceiveBuffer = new byte[Mathf.NextPowerOfTwo(size)];
                if (size > 0) Marshal.Copy(message.m_pData, m_ReceiveBuffer, 0, size);
                SteamNetworkingMessage_t.Release(pointer);

                ulong from;
                if (m_IsServer)
                {
                    from = Id(connection);
                    // A message from a connection we closed meanwhile.
                    if (!m_Peers.TryGetValue(from, out var peer)) continue;
                    if (!peer.Announced)
                    {
                        // Data raced ahead of its Connected callback: announce first, deliver the data right after.
                        Announce(from, peer);
                        EnqueueCopy(from, size);
                        return Dequeue(out clientId, out payload);
                    }
                }
                else
                {
                    from = ServerClientId;
                    if (!m_ServerAnnounced)
                    {
                        m_ServerAnnounced = true;
                        Enqueue(NetworkEvent.Connect, ServerClientId);
                        EnqueueCopy(ServerClientId, size);
                        return Dequeue(out clientId, out payload);
                    }
                }

                // Netcode copies the data out before polling again, so one reusable buffer is enough.
                clientId = from;
                payload = new ArraySegment<byte>(m_ReceiveBuffer, 0, size);
                return NetworkEvent.Data;
            }
        }

        int Receive()
        {
            if (m_IsServer)
                return m_PollGroup != HSteamNetPollGroup.Invalid ? SteamNetworkingSockets.ReceiveMessagesOnPollGroup(m_PollGroup, m_Messages, MaxMessagesPerReceive) : 0;
            return m_ServerConnection != HSteamNetConnection.Invalid ? SteamNetworkingSockets.ReceiveMessagesOnConnection(m_ServerConnection, m_Messages, MaxMessagesPerReceive) : 0;
        }

        /// <summary>Messages received but not handed to Netcode yet (shutdown mid-batch) still belong to Steam.</summary>
        void ReleaseReceivedMessages()
        {
            for (int i = m_MessageIndex; i < m_MessageCount; i++)
            {
                if (m_Messages[i] != IntPtr.Zero) SteamNetworkingMessage_t.Release(m_Messages[i]);
                m_Messages[i] = IntPtr.Zero;
            }
            m_MessageIndex = 0;
            m_MessageCount = 0;
        }

        void Enqueue(NetworkEvent type, ulong clientId) => m_Events.Enqueue(new QueuedEvent { Type = type, ClientId = clientId });

        void EnqueueCopy(ulong clientId, int size)
        {
            var copy = new byte[size];
            Buffer.BlockCopy(m_ReceiveBuffer, 0, copy, 0, size);
            m_Events.Enqueue(new QueuedEvent { Type = NetworkEvent.Data, ClientId = clientId, Payload = copy });
        }

        NetworkEvent Dequeue(out ulong clientId, out ArraySegment<byte> payload)
        {
            var queued = m_Events.Dequeue();
            clientId = queued.ClientId;
            payload = queued.Payload != null ? new ArraySegment<byte>(queued.Payload) : default;
            return queued.Type;
        }

        // ================================================================== sending

        public override void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery networkDelivery)
        {
            if (!m_Running || !SteamPlatform.Running || payload.Array == null || payload.Count == 0) return;
            var connection = ConnectionOf(clientId);
            if (connection == HSteamNetConnection.Invalid) return;

            if (payload.Count > Constants.k_cbMaxSteamNetworkingSocketsMessageSizeSend)
            {
                Debug.LogError($"[DeckParty] Steam: a {payload.Count} byte message is over Steam's {Constants.k_cbMaxSteamNetworkingSocketsMessageSizeSend} byte limit and was dropped.");
                return;
            }

            var pin = GCHandle.Alloc(payload.Array, GCHandleType.Pinned);
            try
            {
                IntPtr data = IntPtr.Add(pin.AddrOfPinnedObject(), payload.Offset);
                var result = SteamNetworkingSockets.SendMessageToConnection(connection, data, (uint)payload.Count, SendFlags(networkDelivery), out _);
                if (result == EResult.k_EResultLimitExceeded && !m_WarnedSendLimit)
                {
                    m_WarnedSendLimit = true;
                    Debug.LogWarning("[DeckParty] Steam: the send queue to a player is full (their connection is too slow); messages are being dropped.");
                }
            }
            finally
            {
                pin.Free();
            }
        }

        static int SendFlags(NetworkDelivery delivery)
        {
            switch (delivery)
            {
                case NetworkDelivery.Unreliable:
                case NetworkDelivery.UnreliableSequenced:
                    return Constants.k_nSteamNetworkingSend_UnreliableNoNagle;
                default:
                    // Reliable, ReliableSequenced and ReliableFragmentedSequenced: Steam's reliable messages are ordered and
                    // up to 512 KB, so fragmentation and sequencing come for free.
                    return Constants.k_nSteamNetworkingSend_ReliableNoNagle;
            }
        }

        HSteamNetConnection ConnectionOf(ulong clientId)
        {
            if (!m_IsServer) return m_ServerConnection;
            return m_Peers.TryGetValue(clientId, out var peer) ? peer.Connection : HSteamNetConnection.Invalid;
        }

        // ================================================================== disconnects and stats

        public override void DisconnectRemoteClient(ulong clientId)
        {
            if (!m_IsServer || !m_Peers.TryGetValue(clientId, out var peer)) return;
            m_Peers.Remove(clientId);
            m_LingerPending = true;
            CloseGracefully(peer.Connection, "Disconnected by the host");
        }

        public override void DisconnectLocalClient()
        {
            if (m_IsServer || m_ServerConnection == HSteamNetConnection.Invalid) return;
            CloseGracefully(m_ServerConnection, "Left the game");
            m_ServerConnection = HSteamNetConnection.Invalid;
        }

        /// <summary>Sends what's queued (e.g. Netcode's disconnect reason) and then closes; Steam keeps it alive until it's delivered.</summary>
        static void CloseGracefully(HSteamNetConnection connection, string why)
        {
            if (!SteamPlatform.Running || connection == HSteamNetConnection.Invalid) return;
            SteamNetworkingSockets.FlushMessagesOnConnection(connection);
            SteamNetworkingSockets.CloseConnection(connection, EndReasonClosed, why, true);
        }

        public override ulong GetCurrentRtt(ulong clientId)
        {
            var connection = ConnectionOf(clientId);
            if (!SteamPlatform.Running || connection == HSteamNetConnection.Invalid) return 0;
            var status = new SteamNetConnectionRealTimeStatus_t();
            var lanes = new SteamNetConnectionRealTimeLaneStatus_t();
            var result = SteamNetworkingSockets.GetConnectionRealTimeStatus(connection, ref status, 0, ref lanes);
            return result == EResult.k_EResultOK && status.m_nPing > 0 ? (ulong)status.m_nPing : 0;
        }

        static ulong Id(HSteamNetConnection connection) => connection.m_HSteamNetConnection;
    }
}
#endif
