using System;
using System.Collections.Generic;
using DeckParty.Core;
using DeckParty.Networking;

namespace DeckParty
{
    /// <summary>Multiplayer session: commands go to the host, authoritative snapshots come back.</summary>
    public sealed class NetworkGameSession : IGameSession
    {
        readonly DeckPartyNetSession m_Net;

        public NetworkGameSession(DeckPartyNetSession net)
        {
            m_Net = net;
            m_Net.StateReceived += OnStateReceived;
            m_Net.CommandRejected += OnCommandRejected;
        }

        public bool IsMultiplayer => true;
        public PlayerState State => m_Net != null ? m_Net.LocalState : null;
        public DeckPartyNetSession Net => m_Net;

        public event Action<PlayerState, IReadOnlyList<GameEvent>> StateChanged;
        public event Action<string> CommandRejected;

        public void Submit(GameCommand command)
        {
            if (m_Net != null && m_Net.IsSpawned) m_Net.SubmitCommand(command);
        }

        void OnStateReceived(PlayerState state, IReadOnlyList<GameEvent> events) => StateChanged?.Invoke(state, events);
        void OnCommandRejected(string reason) => CommandRejected?.Invoke(reason);

        public void Dispose()
        {
            if (m_Net != null)
            {
                m_Net.StateReceived -= OnStateReceived;
                m_Net.CommandRejected -= OnCommandRejected;
            }
            StateChanged = null;
            CommandRejected = null;
        }
    }
}
