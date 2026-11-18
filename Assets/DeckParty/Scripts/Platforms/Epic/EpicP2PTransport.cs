// Epic code only exists when the EOS Plugin for Unity is installed (DECKPARTY_EOS, a version define in DeckParty.Epic.asmdef)
// and the build target is Windows, macOS or Linux. Otherwise this file compiles to nothing and the game uses the Default multiplayer.
#if DECKPARTY_EOS && UNITY_STANDALONE
using System;
using System.Collections.Generic;
using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using PlayEveryWare.EpicOnlineServices;
using Unity.Netcode;
using UnityEngine;

namespace DeckParty.Platforms.Epic
{
    /// <summary>
    /// A Netcode for GameObjects transport over EOS P2P: peer-to-peer between product user ids, with Epic's NAT traversal
    /// and relays, no IP addresses exposed and no servers of our own.
    /// <list type="bullet">
    /// <item>Host: accepts connection requests on the "DeckParty" socket, only from members of its EOS lobby (checked again
    /// for a few seconds, since the lobby join and the connection race each other).</item>
    /// <item>Client: accepts the host's side and opens the connection with a small hello packet.</item>
    /// <item>Netcode client ids on the host count up from 1; the host is <see cref="ServerClientId"/> 0.</item>
    /// <item>EOS packets hold at most 1170 bytes, so reliable messages are split into ordered fragments on the reliable
    /// channel and put back together on arrival (Netcode's fragmented deliveries and its bigger batches need that).
    /// Unreliable messages go on their own channel; one too big for a packet is sent reliably instead.</item>
    /// <item>Disconnects send a goodbye and linger a moment, so Netcode's "why you were disconnected" message gets through.</item>
    /// </list>
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public sealed class EpicP2PTransport : NetworkTransport
    {
        const string SocketName = "DeckParty";
        const byte ReliableChannel = 0;
        const byte UnreliableChannel = 1;
        const int MaxPacket = P2PInterface.MAX_PACKET_SIZE;

        // First byte of every packet.
        const byte TagWhole = 0;
        const byte TagFirst = 1;
        const byte TagMore = 2;
        const byte TagHello = 3;
        const byte TagBye = 4;
        const int FirstHeader = 5;
        const int MoreHeader = 1;
        /// <summary>Largest reassembled message accepted (Netcode's own fragmented limit is far below this).</summary>
        const int MaxMessageSize = 4 * 1024 * 1024;

        /// <summary>How long the host waits for a connecting player to show up in its lobby.</summary>
        const float MemberWaitSeconds = 6f;
        /// <summary>How long a client waits for the host to accept before giving up.</summary>
        const float ConnectTimeoutSeconds = 20f;
        /// <summary>How long connections stay open after a disconnect so the last messages can flush.</summary>
        const float LingerSeconds = 1f;
        const int MaxPacketsPerPoll = 512;

        sealed class Peer
        {
            public ProductUserId User;
            public string Key;
            public bool Announced;
            // Reassembly of a fragmented reliable message.
            public byte[] Assembly;
            public int Expected;
            public int Received;
        }

        struct PendingAccept
        {
            public ProductUserId User;
            public string Key;
            public float Deadline;
        }

        struct PendingClose
        {
            public ProductUserId User;
            public float At;
        }

        struct QueuedEvent
        {
            public NetworkEvent Type;
            public ulong ClientId;
            public byte[] Payload;
        }

        Func<string, bool> m_AllowPeer;
        ProductUserId m_Host;
        string m_HostKey;
        Peer m_HostPeer;
        bool m_HostGone;
        bool m_IsServer;
        bool m_Running;
        bool m_ServerAnnounced;
        float m_ConnectDeadline;
        SocketId m_Socket;
        ulong m_NextId = 1;

        readonly Dictionary<ulong, Peer> m_Peers = new Dictionary<ulong, Peer>();
        readonly Dictionary<string, ulong> m_IdsByKey = new Dictionary<string, ulong>();
        readonly List<PendingAccept> m_PendingAccepts = new List<PendingAccept>();
        readonly List<PendingClose> m_PendingCloses = new List<PendingClose>();
        readonly Queue<QueuedEvent> m_Events = new Queue<QueuedEvent>();
        readonly byte[] m_ReceiveBuffer = new byte[MaxPacket];
        readonly byte[] m_SendBuffer = new byte[MaxPacket];
        float m_CloseAllAt = -1f;
        ProductUserId m_CloseAllUser;
        bool m_WarnedUnreliable;
        bool m_WarnedQueue;

        ulong m_RequestNotify;
        ulong m_EstablishedNotify;
        ulong m_ClosedNotify;
        OnIncomingConnectionRequestCallback m_OnRequest;
        OnPeerConnectionEstablishedCallback m_OnEstablished;
        OnRemoteConnectionClosedCallback m_OnClosed;

        public override ulong ServerClientId => 0;

        /// <summary>Host: who may connect (the lobby's members, by product user id). Set by <see cref="EpicPlatform.PrepareHostTransport"/>.</summary>
        public void ConfigureHost(Func<string, bool> allowPeer) => m_AllowPeer = allowPeer;

        /// <summary>Client: the product user id hosting the match. Set by <see cref="EpicPlatform.PrepareClientTransport"/>.</summary>
        public void ConfigureClient(string hostProductUserId)
        {
            m_Host = string.IsNullOrEmpty(hostProductUserId) ? null : ProductUserId.FromString(hostProductUserId);
            m_HostKey = m_Host != null && m_Host.IsValid() ? m_Host.ToString() : null;
        }

        static P2PInterface P2P => EpicPlatform.Sdk(m => m.GetEOSP2PInterface());
        static ProductUserId Local => EpicPlatform.Instance != null ? EpicPlatform.Instance.LocalProductUser : null;
        static bool Available => EpicPlatform.Instance != null && EpicPlatform.Instance.IsReady && Local != null && P2P != null;

        // ================================================================== lifecycle

        public override void Initialize(NetworkManager networkManager = null)
        {
            m_Socket = new SocketId { SocketName = SocketName };
            m_OnRequest = OnConnectionRequest;
            m_OnEstablished = OnConnectionEstablished;
            m_OnClosed = OnConnectionClosed;
        }

        public override bool StartServer()
        {
            if (!Available) return false;
            CloseLingering();
            ResetState();
            m_IsServer = true;
            AddNotifications();
            m_Running = true;
            return true;
        }

        public override bool StartClient()
        {
            if (!Available || m_HostKey == null) return false;
            CloseLingering();
            ResetState();
            m_IsServer = false;
            m_HostPeer = new Peer { User = m_Host, Key = m_HostKey };
            AddNotifications();

            // Accept the host's side too, then say hello: EOS opens the connection with the first packet.
            var accept = new AcceptConnectionOptions { LocalUserId = Local, RemoteUserId = m_Host, SocketId = m_Socket };
            var result = P2P.AcceptConnection(ref accept);
            if (result != Result.Success) Debug.LogWarning($"[DeckParty] Epic P2P: couldn't accept the host's connection ({result}).");
            SendControl(m_Host, TagHello);
            m_ConnectDeadline = Time.unscaledTime + ConnectTimeoutSeconds;
            m_Running = true;
            return true;
        }

        public override void Shutdown()
        {
            bool wasRunning = m_Running;
            m_Running = false;
            RemoveNotifications();

            if (wasRunning && Available)
            {
                // Say goodbye to everyone, then close the socket after a moment so queued messages still go out.
                if (m_IsServer)
                {
                    foreach (var peer in m_Peers.Values) SendControl(peer.User, TagBye);
                    foreach (var pending in m_PendingAccepts) CloseNow(pending.User);
                }
                else if (!m_HostGone && m_Host != null)
                {
                    SendControl(m_Host, TagBye);
                }
                m_CloseAllAt = Time.unscaledTime + LingerSeconds;
                m_CloseAllUser = Local;
            }

            m_Peers.Clear();
            m_IdsByKey.Clear();
            m_PendingAccepts.Clear();
            m_Events.Clear();
            m_HostPeer = null;
            m_AllowPeer = null;
        }

        void ResetState()
        {
            m_Peers.Clear();
            m_IdsByKey.Clear();
            m_PendingAccepts.Clear();
            m_Events.Clear();
            m_NextId = 1;
            m_ServerAnnounced = false;
            m_HostGone = false;
            m_WarnedUnreliable = false;
            m_WarnedQueue = false;
        }

        void OnDestroy()
        {
            Shutdown();
            CloseLingering();
        }

        void Update()
        {
            // Lingering closes run here: Netcode stops calling EarlyUpdate once the session is over.
            if (m_CloseAllAt >= 0f && Time.unscaledTime >= m_CloseAllAt) CloseLingering();
            ProcessPendingCloses(false);
        }

        void CloseLingering()
        {
            if (m_CloseAllAt < 0f) return;
            m_CloseAllAt = -1f;
            var user = m_CloseAllUser;
            m_CloseAllUser = null;
            ProcessPendingCloses(true);
            if (user == null || P2P == null) return;
            var options = new CloseConnectionsOptions { LocalUserId = user, SocketId = m_Socket };
            P2P.CloseConnections(ref options);
        }

        protected override void OnEarlyUpdate()
        {
            if (!m_Running) return;
            if (m_IsServer)
            {
                CheckPendingAccepts();
            }
            else if (!m_ServerAnnounced && !m_HostGone && Time.unscaledTime >= m_ConnectDeadline)
            {
                // The host never answered (gone, or it didn't let us in).
                m_HostGone = true;
                SetDisconnectEvent(DisconnectEvents.ProtocolTimeout, "The host didn't answer");
                Enqueue(NetworkEvent.Disconnect, ServerClientId);
            }
            ProcessPendingCloses(false);
        }

        // ================================================================== notifications

        void AddNotifications()
        {
            RemoveNotifications();
            var p2p = P2P;
            var local = Local;
            if (p2p == null || local == null) return;
            var request = new AddNotifyPeerConnectionRequestOptions { LocalUserId = local, SocketId = m_Socket };
            m_RequestNotify = p2p.AddNotifyPeerConnectionRequest(ref request, null, m_OnRequest);
            var established = new AddNotifyPeerConnectionEstablishedOptions { LocalUserId = local, SocketId = m_Socket };
            m_EstablishedNotify = p2p.AddNotifyPeerConnectionEstablished(ref established, null, m_OnEstablished);
            var closed = new AddNotifyPeerConnectionClosedOptions { LocalUserId = local, SocketId = m_Socket };
            m_ClosedNotify = p2p.AddNotifyPeerConnectionClosed(ref closed, null, m_OnClosed);
        }

        void RemoveNotifications()
        {
            var p2p = EpicPlatform.Instance != null && EpicPlatform.Instance.SdkRunning ? P2P : null;
            if (p2p != null)
            {
                if (m_RequestNotify != 0) p2p.RemoveNotifyPeerConnectionRequest(m_RequestNotify);
                if (m_EstablishedNotify != 0) p2p.RemoveNotifyPeerConnectionEstablished(m_EstablishedNotify);
                if (m_ClosedNotify != 0) p2p.RemoveNotifyPeerConnectionClosed(m_ClosedNotify);
            }
            m_RequestNotify = 0;
            m_EstablishedNotify = 0;
            m_ClosedNotify = 0;
        }

        void OnConnectionRequest(ref OnIncomingConnectionRequestInfo info)
        {
            if (!m_Running || info.RemoteUserId == null) return;
            string key = info.RemoteUserId.ToString();
            if (!m_IsServer)
            {
                // Only the host may talk to us.
                if (key == m_HostKey) Accept(info.RemoteUserId);
                return;
            }
            if (m_IdsByKey.ContainsKey(key))
            {
                Accept(info.RemoteUserId);
                return;
            }
            if (m_AllowPeer == null || m_AllowPeer(key)) AcceptPeer(info.RemoteUserId, key);
            else if (!m_PendingAccepts.Exists(p => p.Key == key))
                m_PendingAccepts.Add(new PendingAccept { User = info.RemoteUserId, Key = key, Deadline = Time.unscaledTime + MemberWaitSeconds });
        }

        void OnConnectionEstablished(ref OnPeerConnectionEstablishedInfo info)
        {
            if (!m_Running || info.RemoteUserId == null) return;
            string key = info.RemoteUserId.ToString();
            if (m_IsServer)
            {
                if (m_IdsByKey.TryGetValue(key, out var id) && m_Peers.TryGetValue(id, out var peer) && !peer.Announced) Announce(id, peer);
            }
            else if (key == m_HostKey && !m_ServerAnnounced)
            {
                m_ServerAnnounced = true;
                Enqueue(NetworkEvent.Connect, ServerClientId);
            }
        }

        void OnConnectionClosed(ref OnRemoteConnectionClosedInfo info)
        {
            if (!m_Running || info.RemoteUserId == null) return;
            string key = info.RemoteUserId.ToString();
            if (m_IsServer)
            {
                m_PendingAccepts.RemoveAll(p => p.Key == key);
                if (m_IdsByKey.TryGetValue(key, out var id))
                {
                    m_IdsByKey.Remove(key);
                    if (m_Peers.TryGetValue(id, out var peer))
                    {
                        m_Peers.Remove(id);
                        if (peer.Announced) Enqueue(NetworkEvent.Disconnect, id);
                    }
                }
            }
            else if (key == m_HostKey && !m_HostGone)
            {
                m_HostGone = true;
                SetDisconnectEvent(MapReason(info.Reason), info.Reason.ToString());
                // Also covers a connection that never got through (the host is gone, or refused us).
                Enqueue(NetworkEvent.Disconnect, ServerClientId);
            }
        }

        static DisconnectEvents MapReason(ConnectionClosedReason reason)
        {
            switch (reason)
            {
                case ConnectionClosedReason.ClosedByPeer: return DisconnectEvents.ClosedByRemote;
                case ConnectionClosedReason.TimedOut: return DisconnectEvents.ProtocolTimeout;
                case ConnectionClosedReason.ClosedByLocalUser: return DisconnectEvents.Disconnected;
                default: return DisconnectEvents.ProtocolError;
            }
        }

        void Accept(ProductUserId user)
        {
            var options = new AcceptConnectionOptions { LocalUserId = Local, RemoteUserId = user, SocketId = m_Socket };
            P2P.AcceptConnection(ref options);
        }

        void AcceptPeer(ProductUserId user, string key)
        {
            var options = new AcceptConnectionOptions { LocalUserId = Local, RemoteUserId = user, SocketId = m_Socket };
            var result = P2P.AcceptConnection(ref options);
            if (result != Result.Success)
            {
                Debug.LogWarning($"[DeckParty] Epic P2P: couldn't accept a connection from {key} ({result}).");
                return;
            }
            ulong id = m_NextId++;
            m_IdsByKey[key] = id;
            m_Peers[id] = new Peer { User = user, Key = key };
        }

        void Announce(ulong id, Peer peer)
        {
            peer.Announced = true;
            Enqueue(NetworkEvent.Connect, id);
        }

        /// <summary>Connections waiting for their player to appear in the lobby: accept them when they do, refuse them after a few seconds.</summary>
        void CheckPendingAccepts()
        {
            for (int i = m_PendingAccepts.Count - 1; i >= 0; i--)
            {
                var pending = m_PendingAccepts[i];
                if (m_AllowPeer == null || m_AllowPeer(pending.Key))
                {
                    m_PendingAccepts.RemoveAt(i);
                    AcceptPeer(pending.User, pending.Key);
                }
                else if (Time.unscaledTime >= pending.Deadline)
                {
                    m_PendingAccepts.RemoveAt(i);
                    Debug.LogWarning($"[DeckParty] Epic P2P: refused a connection from {pending.Key}, who isn't in the lobby.");
                    CloseNow(pending.User);
                }
            }
        }

        // ================================================================== Netcode events

        public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
        {
            receiveTime = Time.realtimeSinceStartup;
            if (m_Events.Count > 0) return Dequeue(out clientId, out payload);

            clientId = 0;
            payload = default;
            if (!m_Running || !Available) return NetworkEvent.Nothing;

            var p2p = P2P;
            var local = Local;
            if (p2p == null || local == null) return NetworkEvent.Nothing;
            for (int n = 0; n < MaxPacketsPerPoll; n++)
            {
                var sizeOptions = new GetNextReceivedPacketSizeOptions { LocalUserId = local, RequestedChannel = null };
                if (p2p.GetNextReceivedPacketSize(ref sizeOptions, out uint _) != Result.Success) break;

                var options = new ReceivePacketOptions { LocalUserId = local, MaxDataSizeBytes = MaxPacket, RequestedChannel = null };
                ProductUserId from = null;
                var socket = new SocketId();
                var result = p2p.ReceivePacket(ref options, ref from, ref socket, out byte _, new ArraySegment<byte>(m_ReceiveBuffer), out uint written);
                if (result != Result.Success) break;
                if (from == null || written == 0 || socket.SocketName != SocketName) continue;

                var type = HandlePacket(from, (int)written, out clientId, out payload);
                if (type != NetworkEvent.Nothing) return type;
                // A packet that only opened the connection (hello) still announced it: hand that over now, not next frame.
                if (m_Events.Count > 0) return Dequeue(out clientId, out payload);
            }
            clientId = 0;
            payload = default;
            return NetworkEvent.Nothing;
        }

        NetworkEvent HandlePacket(ProductUserId from, int size, out ulong clientId, out ArraySegment<byte> payload)
        {
            clientId = 0;
            payload = default;
            string key = from.ToString();
            ulong id;
            Peer peer;
            if (m_IsServer)
            {
                // EOS only delivers packets from connections we accepted; anything else is stale.
                if (!m_IdsByKey.TryGetValue(key, out id) || !m_Peers.TryGetValue(id, out peer)) return NetworkEvent.Nothing;
                // Data raced ahead of its "established" notification: announce first, deliver the data right after.
                if (!peer.Announced) Announce(id, peer);
            }
            else
            {
                if (key != m_HostKey || m_HostGone || m_HostPeer == null) return NetworkEvent.Nothing;
                id = ServerClientId;
                peer = m_HostPeer;
                if (!m_ServerAnnounced)
                {
                    m_ServerAnnounced = true;
                    Enqueue(NetworkEvent.Connect, ServerClientId);
                }
            }

            byte tag = m_ReceiveBuffer[0];
            switch (tag)
            {
                case TagWhole:
                    return Deliver(id, m_ReceiveBuffer, 1, size - 1, out clientId, out payload);

                case TagFirst:
                {
                    if (size < FirstHeader) return NetworkEvent.Nothing;
                    int total = m_ReceiveBuffer[1] | (m_ReceiveBuffer[2] << 8) | (m_ReceiveBuffer[3] << 16) | (m_ReceiveBuffer[4] << 24);
                    if (total <= 0 || total > MaxMessageSize)
                    {
                        Debug.LogWarning($"[DeckParty] Epic P2P: dropped a message announced as {total} bytes.");
                        peer.Assembly = null;
                        return NetworkEvent.Nothing;
                    }
                    peer.Assembly = new byte[total];
                    peer.Expected = total;
                    peer.Received = 0;
                    return Append(id, peer, FirstHeader, size - FirstHeader, out clientId, out payload);
                }

                case TagMore:
                    if (peer.Assembly == null) return NetworkEvent.Nothing;
                    return Append(id, peer, MoreHeader, size - MoreHeader, out clientId, out payload);

                default:
                    // Hello opens the connection; goodbye is followed by the "closed" notification.
                    return NetworkEvent.Nothing;
            }
        }

        NetworkEvent Append(ulong id, Peer peer, int offset, int count, out ulong clientId, out ArraySegment<byte> payload)
        {
            clientId = 0;
            payload = default;
            if (count <= 0) return NetworkEvent.Nothing;
            if (peer.Received + count > peer.Expected)
            {
                Debug.LogWarning("[DeckParty] Epic P2P: a fragmented message came out longer than announced; dropped.");
                peer.Assembly = null;
                return NetworkEvent.Nothing;
            }
            Buffer.BlockCopy(m_ReceiveBuffer, offset, peer.Assembly, peer.Received, count);
            peer.Received += count;
            if (peer.Received < peer.Expected) return NetworkEvent.Nothing;

            var whole = peer.Assembly;
            peer.Assembly = null;
            return Deliver(id, whole, 0, whole.Length, out clientId, out payload);
        }

        NetworkEvent Deliver(ulong id, byte[] buffer, int offset, int count, out ulong clientId, out ArraySegment<byte> payload)
        {
            if (count <= 0)
            {
                clientId = 0;
                payload = default;
                return m_Events.Count > 0 ? Dequeue(out clientId, out payload) : NetworkEvent.Nothing;
            }
            if (m_Events.Count > 0)
            {
                // A connect was queued just before this data: keep the order.
                var copy = new byte[count];
                Buffer.BlockCopy(buffer, offset, copy, 0, count);
                m_Events.Enqueue(new QueuedEvent { Type = NetworkEvent.Data, ClientId = id, Payload = copy });
                return Dequeue(out clientId, out payload);
            }
            // Netcode copies the data out before polling again, so the receive buffer can be handed over as it is.
            clientId = id;
            payload = new ArraySegment<byte>(buffer, offset, count);
            return NetworkEvent.Data;
        }

        void Enqueue(NetworkEvent type, ulong clientId) => m_Events.Enqueue(new QueuedEvent { Type = type, ClientId = clientId });

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
            if (!m_Running || !Available || payload.Array == null || payload.Count == 0) return;
            var user = UserOf(clientId);
            if (user == null) return;

            bool reliable = networkDelivery != NetworkDelivery.Unreliable && networkDelivery != NetworkDelivery.UnreliableSequenced;
            if (!reliable && payload.Count + 1 > MaxPacket)
            {
                if (!m_WarnedUnreliable)
                {
                    m_WarnedUnreliable = true;
                    Debug.LogWarning($"[DeckParty] Epic P2P: a {payload.Count} byte unreliable message is over one packet; sending it reliably.");
                }
                reliable = true;
            }

            if (payload.Count + 1 <= MaxPacket)
            {
                m_SendBuffer[0] = TagWhole;
                Buffer.BlockCopy(payload.Array, payload.Offset, m_SendBuffer, 1, payload.Count);
                SendRaw(user, payload.Count + 1, reliable);
                return;
            }

            // Fragmented: the first packet carries the total length, the rest follow in order on the reliable channel.
            int total = payload.Count;
            int sent = Math.Min(total, MaxPacket - FirstHeader);
            m_SendBuffer[0] = TagFirst;
            m_SendBuffer[1] = (byte)total;
            m_SendBuffer[2] = (byte)(total >> 8);
            m_SendBuffer[3] = (byte)(total >> 16);
            m_SendBuffer[4] = (byte)(total >> 24);
            Buffer.BlockCopy(payload.Array, payload.Offset, m_SendBuffer, FirstHeader, sent);
            if (!SendRaw(user, FirstHeader + sent, true)) return;
            while (sent < total)
            {
                int chunk = Math.Min(total - sent, MaxPacket - MoreHeader);
                m_SendBuffer[0] = TagMore;
                Buffer.BlockCopy(payload.Array, payload.Offset + sent, m_SendBuffer, MoreHeader, chunk);
                if (!SendRaw(user, MoreHeader + chunk, true)) return;
                sent += chunk;
            }
        }

        bool SendRaw(ProductUserId user, int length, bool reliable)
        {
            var options = new SendPacketOptions
            {
                LocalUserId = Local,
                RemoteUserId = user,
                SocketId = m_Socket,
                Channel = reliable ? ReliableChannel : UnreliableChannel,
                Data = new ArraySegment<byte>(m_SendBuffer, 0, length),
                AllowDelayedDelivery = true,
                Reliability = reliable ? PacketReliability.ReliableOrdered : PacketReliability.UnreliableUnordered,
                DisableAutoAcceptConnection = false,
            };
            var result = P2P.SendPacket(ref options);
            if (result == Result.Success) return true;
            if (!m_WarnedQueue)
            {
                m_WarnedQueue = true;
                Debug.LogWarning($"[DeckParty] Epic P2P: a packet couldn't be sent ({result}); the connection may be too slow.");
            }
            return false;
        }

        void SendControl(ProductUserId user, byte tag)
        {
            if (user == null || !Available) return;
            m_SendBuffer[0] = tag;
            SendRaw(user, 1, true);
        }

        ProductUserId UserOf(ulong clientId)
        {
            if (!m_IsServer) return m_HostGone ? null : m_Host;
            return m_Peers.TryGetValue(clientId, out var peer) ? peer.User : null;
        }

        // ================================================================== disconnects and stats

        public override void DisconnectRemoteClient(ulong clientId)
        {
            if (!m_IsServer || !m_Peers.TryGetValue(clientId, out var peer)) return;
            m_Peers.Remove(clientId);
            m_IdsByKey.Remove(peer.Key);
            SendControl(peer.User, TagBye);
            m_PendingCloses.Add(new PendingClose { User = peer.User, At = Time.unscaledTime + LingerSeconds });
        }

        public override void DisconnectLocalClient()
        {
            if (m_IsServer || m_HostGone || m_Host == null) return;
            SendControl(m_Host, TagBye);
            m_PendingCloses.Add(new PendingClose { User = m_Host, At = Time.unscaledTime + LingerSeconds });
            m_HostGone = true;
        }

        void ProcessPendingCloses(bool all)
        {
            for (int i = m_PendingCloses.Count - 1; i >= 0; i--)
            {
                if (!all && Time.unscaledTime < m_PendingCloses[i].At) continue;
                var user = m_PendingCloses[i].User;
                m_PendingCloses.RemoveAt(i);
                CloseNow(user);
            }
        }

        void CloseNow(ProductUserId user)
        {
            var local = Local;
            if (user == null || local == null || P2P == null) return;
            var options = new CloseConnectionOptions { LocalUserId = local, RemoteUserId = user, SocketId = m_Socket };
            P2P.CloseConnection(ref options);
        }

        /// <summary>EOS P2P doesn't report round-trip times.</summary>
        public override ulong GetCurrentRtt(ulong clientId) => 0;
    }
}
#endif
