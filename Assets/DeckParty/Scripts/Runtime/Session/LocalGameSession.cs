using System;
using System.Collections.Generic;
using DeckParty.Core;

namespace DeckParty
{
    /// <summary>Single-player session: runs the engine in-process, autosaves and records progress.</summary>
    public sealed class LocalGameSession : IGameSession
    {
        readonly List<GameEvent> m_Events = new List<GameEvent>();
        string m_VenueChoice;

        public LocalGameSession(PlayerState state, string venueChoice)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            m_VenueChoice = venueChoice;
            if (!State.IsGameOver) SaveSystem.Save(State, m_VenueChoice);
        }

        public bool IsMultiplayer => false;
        public PlayerState State { get; }

        public event Action<PlayerState, IReadOnlyList<GameEvent>> StateChanged;
        public event Action<string> CommandRejected;

        public string VenueChoice
        {
            get => m_VenueChoice;
            set
            {
                m_VenueChoice = value;
                if (!State.IsGameOver) SaveSystem.Save(State, m_VenueChoice);
            }
        }

        public void Submit(GameCommand command)
        {
            bool wasOver = State.IsGameOver;
            m_Events.Clear();

            var result = PartyEngine.Execute(State, command, m_Events);
            if (!result.Ok)
            {
                CommandRejected?.Invoke(result.Error);
                return;
            }

            if (State.IsGameOver)
            {
                if (!wasOver) ProgressStore.RecordSeason(State.ScenarioId, State.Phase == GamePhase.Victory);
                SaveSystem.Delete();
            }
            else
            {
                SaveSystem.Save(State, m_VenueChoice);
            }

            StateChanged?.Invoke(State, m_Events);
        }

        /// <summary>
        /// Testing aid (editor and development builds only): grants banked Gifted Subs and Cash outside the rules.
        /// Not available in multiplayer, where the host's engine is the only authority.
        /// </summary>
        public void DebugGrant(int giftedSubs, int cash)
        {
            if (State.IsGameOver) return;
            State.Pop += giftedSubs;
            State.Cash += cash;
            SaveSystem.Save(State, m_VenueChoice);
            m_Events.Clear();
            StateChanged?.Invoke(State, m_Events);
        }

        public void Dispose()
        {
            StateChanged = null;
            CommandRejected = null;
        }
    }
}
