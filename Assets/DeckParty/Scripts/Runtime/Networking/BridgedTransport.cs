using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace DeckParty.Networking
{
    /// <summary>
    /// The host's transport for a Steam or Epic lobby that also has a cross-platform join code: two transports behind one.
    /// <list type="bullet">
    /// <item>The platform leg (Steam Networking Sockets, EOS P2P) takes the lobby's members, as before.</item>
    /// <item>The Relay leg (Unity Transport on a Unity Relay allocation) takes anyone who typed the join code, from any
    /// platform or none.</item>
    /// </list>
    /// Netcode only ever sees this transport. Ids from the Relay leg are passed through unchanged, because Unity Transport
    /// looks its own ids up in Netcode (sends to an id Netcode doesn't know are dropped). Platform ids get the top bit set,
    /// which a Unity Transport id never has (its high half is a connection version, a positive int).
    /// <para>
    /// Hosting only: clients connect with one transport (the platform's, or Unity Transport for a code). If the Relay leg
    /// fails mid-match, its players are dropped and the platform players carry on.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public sealed class BridgedTransport : NetworkTransport
    {
        /// <summary>Set on every id from the platform leg.</summary>
        public const ulong PlatformBit = 1UL << 63;

        static readonly FieldInfo s_MetricsField = typeof(NetworkTransport).GetField("NetworkMetrics", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        NetworkTransport m_Platform;
        UnityTransport m_Relay;
        bool m_PlatformRunning;
        bool m_RelayRunning;
        bool m_UseRelay;
        NetworkTransport m_HookedPlatform;
        UnityTransport m_HookedRelay;
        readonly HashSet<ulong> m_RelayClients = new HashSet<ulong>();

        /// <summary>The Relay leg is listening (the join code works).</summary>
        public bool RelayRunning => m_RelayRunning;

        /// <summary>Whether a transport id (as Netcode sees it) came in through the platform leg.</summary>
        public static bool IsPlatformClient(ulong transportId) => transportId != ulong.MaxValue && (transportId & PlatformBit) != 0;

        /// <summary>Host only, so this is never used; Netcode still asks.</summary>
        public override ulong ServerClientId => 0;

        /// <summary>
        /// Sets the two legs before the host starts. <paramref name="relay"/> must already have its Relay server data; pass
        /// null to host on the platform alone.
        /// </summary>
        public void Configure(NetworkTransport platform, UnityTransport relay)
        {
            m_Platform = platform;
            m_Relay = relay;
            m_UseRelay = relay != null;
        }

        // ================================================================== lifecycle

        public override void Initialize(NetworkManager networkManager = null)
        {
            Hook();
            // Netcode hands its metrics to the transport it knows: pass them on (Unity Transport reads them).
            object metrics = s_MetricsField != null ? s_MetricsField.GetValue(this) : null;
            if (m_Platform != null)
            {
                if (s_MetricsField != null) s_MetricsField.SetValue(m_Platform, metrics);
                m_Platform.Initialize(networkManager);
            }
            if (m_Relay != null && m_UseRelay)
            {
                if (s_MetricsField != null) s_MetricsField.SetValue(m_Relay, metrics);
                m_Relay.Initialize(networkManager);
            }
        }

        public override bool StartServer()
        {
            m_RelayClients.Clear();
            m_PlatformRunning = m_Platform != null && m_Platform.StartServer();
            if (!m_PlatformRunning)
            {
                Debug.LogError("[DeckParty] The platform transport couldn't start hosting.");
                return false;
            }

            m_RelayRunning = false;
            if (m_Relay != null && m_UseRelay)
            {
                try
                {
                    m_RelayRunning = m_Relay.StartServer();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                if (!m_RelayRunning) Debug.LogWarning("[DeckParty] The join code's Relay connection couldn't start; only platform players can join.");
            }
            return true;
        }

        public override bool StartClient()
        {
            Debug.LogError("[DeckParty] BridgedTransport only hosts; clients connect with one transport.");
            return false;
        }

        public override void Shutdown()
        {
            bool platform = m_PlatformRunning, relay = m_RelayRunning;
            m_PlatformRunning = false;
            m_RelayRunning = false;
            m_RelayClients.Clear();
            if (platform && m_Platform != null) m_Platform.Shutdown();
            if (relay && m_Relay != null)
            {
                try
                {
                    m_Relay.Shutdown();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            Unhook();
        }

        void OnDestroy() => Unhook();

        void Hook()
        {
            if (m_HookedPlatform != m_Platform)
            {
                if (m_HookedPlatform != null) m_HookedPlatform.OnTransportEvent -= OnPlatformEvent;
                m_HookedPlatform = m_Platform;
                if (m_Platform != null) m_Platform.OnTransportEvent += OnPlatformEvent;
            }
            var relay = m_UseRelay ? m_Relay : null;
            if (m_HookedRelay != relay)
            {
                if (m_HookedRelay != null) m_HookedRelay.OnTransportEvent -= OnRelayEvent;
                m_HookedRelay = relay;
                if (relay != null) relay.OnTransportEvent += OnRelayEvent;
            }
        }

        void Unhook()
        {
            if (m_HookedPlatform != null) m_HookedPlatform.OnTransportEvent -= OnPlatformEvent;
            if (m_HookedRelay != null) m_HookedRelay.OnTransportEvent -= OnRelayEvent;
            m_HookedPlatform = null;
            m_HookedRelay = null;
        }

        protected override void OnEarlyUpdate()
        {
            if (m_PlatformRunning) m_Platform.EarlyUpdate();
            // Unity Transport raises its events from here (OnRelayEvent passes them on).
            if (m_RelayRunning) m_Relay.EarlyUpdate();
        }

        protected override void OnPostLateUpdate()
        {
            if (m_PlatformRunning) m_Platform.PostLateUpdate();
            // Unity Transport sends its queued messages here.
            if (m_RelayRunning) m_Relay.PostLateUpdate();
        }

        // ================================================================== events

        /// <summary>The platform leg polls (Steam, Epic); event-driven ones go through <see cref="OnPlatformEvent"/>.</summary>
        public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
        {
            if (m_PlatformRunning)
            {
                var type = m_Platform.PollEvent(out clientId, out payload, out receiveTime);
                if (type != NetworkEvent.Nothing)
                {
                    if (type != NetworkEvent.TransportFailure) clientId |= PlatformBit;
                    return type;
                }
            }
            clientId = 0;
            payload = default;
            receiveTime = Time.realtimeSinceStartup;
            return NetworkEvent.Nothing;
        }

        void OnPlatformEvent(NetworkEvent type, ulong clientId, ArraySegment<byte> payload, float receiveTime)
        {
            if (!m_PlatformRunning) return;
            InvokeOnTransportEvent(type, type == NetworkEvent.TransportFailure ? clientId : clientId | PlatformBit, payload, receiveTime);
        }

        void OnRelayEvent(NetworkEvent type, ulong clientId, ArraySegment<byte> payload, float receiveTime)
        {
            if (!m_RelayRunning) return;
            if (type == NetworkEvent.TransportFailure)
            {
                // The Relay connection died: drop the code players, keep the match going for the platform players.
                Debug.LogWarning("[DeckParty] The join code's Relay connection failed; players who joined with the code were disconnected.");
                m_RelayRunning = false;
                var dropped = new List<ulong>(m_RelayClients);
                m_RelayClients.Clear();
                foreach (var id in dropped) InvokeOnTransportEvent(NetworkEvent.Disconnect, id, default, receiveTime);
                return;
            }
            if ((clientId & PlatformBit) != 0)
            {
                Debug.LogError($"[DeckParty] A Relay connection id ({clientId}) collides with the platform range; ignoring it.");
                return;
            }
            if (type == NetworkEvent.Connect) m_RelayClients.Add(clientId);
            else if (type == NetworkEvent.Disconnect) m_RelayClients.Remove(clientId);
            InvokeOnTransportEvent(type, clientId, payload, receiveTime);
        }

        // ================================================================== sending and connections

        public override void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery networkDelivery)
        {
            if ((clientId & PlatformBit) != 0)
            {
                if (m_PlatformRunning) m_Platform.Send(clientId & ~PlatformBit, payload, networkDelivery);
            }
            else if (m_RelayRunning)
            {
                m_Relay.Send(clientId, payload, networkDelivery);
            }
        }

        public override void DisconnectRemoteClient(ulong clientId)
        {
            if ((clientId & PlatformBit) != 0)
            {
                if (m_PlatformRunning) m_Platform.DisconnectRemoteClient(clientId & ~PlatformBit);
            }
            else if (m_RelayRunning)
            {
                m_RelayClients.Remove(clientId);
                m_Relay.DisconnectRemoteClient(clientId);
            }
        }

        public override void DisconnectLocalClient()
        {
            // Hosting only: nothing to leave.
        }

        public override ulong GetCurrentRtt(ulong clientId)
        {
            if ((clientId & PlatformBit) != 0) return m_PlatformRunning ? m_Platform.GetCurrentRtt(clientId & ~PlatformBit) : 0;
            return m_RelayRunning ? m_Relay.GetCurrentRtt(clientId) : 0;
        }
    }
}
