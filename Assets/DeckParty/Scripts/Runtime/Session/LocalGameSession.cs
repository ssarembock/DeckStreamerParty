using System;
using System.Collections.Generic;
using DeckParty.Core;

namespace DeckParty
{
    /// <summary>
    /// Single-player session: runs the engine in-process, autosaves and records progress. Versus the AI (2026-11-16) adds a
    /// rival season played by <see cref="SimpleAI"/>: each time your stream ends, the AI plays the same stream number and
    /// its shop straight away (the first to win wins). Rival powers (Raid, Wealth Tax) hit the other side, both ways.
    /// </summary>
    public sealed partial class LocalGameSession : IGameSession
    {
        /// <summary>Most AI moves in one stream or shop visit (a safety net; a stream rarely needs a dozen).</summary>
        const int MaxAiMoves = 200;

        readonly List<GameEvent> m_Events = new List<GameEvent>();
        readonly List<GameEvent> m_RivalEvents = new List<GameEvent>();
        readonly bool m_Persist;
        string m_VenueChoice;
        SimpleAI m_Ai;

        /// <param name="persist">Autosave the season (Versus the AI isn't saved: the AI's season couldn't be resumed).</param>
        public LocalGameSession(PlayerState state, string venueChoice, bool persist = true)
        {
            m_Solo = state ?? throw new ArgumentNullException(nameof(state));
            m_VenueChoice = venueChoice;
            m_Persist = persist;
            if (m_Persist && !State.IsGameOver) SaveSystem.Save(State, m_VenueChoice);
        }

        public bool IsMultiplayer => false;
        /// <summary>The season being played: yours, or in couch play the season of whoever's turn it is.</summary>
        public PlayerState State => IsCouch ? m_Seats[ActiveSeat] : m_Solo;

        readonly PlayerState m_Solo;

        public event Action<PlayerState, IReadOnlyList<GameEvent>> StateChanged;
        public event Action<string> CommandRejected;

        // ------------------------------------------------------------------ Versus the AI

        /// <summary>Versus the AI: the AI's season (null in a normal single-player season).</summary>
        public PlayerState Rival => IsCouch ? m_Seats[1 - ActiveSeat] : m_Rival;
        PlayerState m_Rival;
        public bool HasRival => Rival != null;

        /// <summary>The AI played (its stream and shop, or a rival power hit it): its state and what happened.</summary>
        public event Action<PlayerState, IReadOnlyList<GameEvent>> RivalChanged;

        /// <summary>Versus the AI: the AI plays this season alongside yours.</summary>
        public void AttachRival(PlayerState rival, int aiSeed)
        {
            m_Rival = rival;
            m_Ai = new SimpleAI(aiSeed);
        }

        /// <summary>The AI won before you did.</summary>
        public bool RivalWon => Rival != null && Rival.Phase == GamePhase.Victory && State.Phase != GamePhase.Victory;

        // ------------------------------------------------------------------ commands

        public string VenueChoice
        {
            get => m_VenueChoice;
            set
            {
                m_VenueChoice = value;
                if (m_Persist && !State.IsGameOver) SaveSystem.Save(State, m_VenueChoice);
            }
        }

        public void Submit(GameCommand command)
        {
            if (IsCouch)
            {
                SubmitCouch(command);
                return;
            }
            bool wasOver = State.IsGameOver;
            bool wasStreaming = State.Phase == GamePhase.Party;
            m_Events.Clear();
            m_RivalEvents.Clear();

            CommandResult result;
            // Rival powers need the rival's season too, so the session resolves them (as the multiplayer host does).
            if (command.Type == CommandType.UseStreamerPower && HasRival && State.StreamerInfo.NeedsRival)
                result = PartyEngine.UseRivalPower(State, 0, Rival, 1, m_Events, m_RivalEvents);
            else
                result = PartyEngine.Execute(State, command, m_Events);

            if (!result.Ok)
            {
                CommandRejected?.Invoke(result.Error);
                return;
            }

            // Versus the AI: once your stream is over (scored, deplatformed or down), the AI plays the same stream.
            if (HasRival && wasStreaming && State.Phase != GamePhase.Party) PlayRivalTurn();

            if (State.IsGameOver)
            {
                if (!wasOver) RecordResult();
                if (m_Persist) SaveSystem.Delete();
            }
            else if (m_Persist)
            {
                SaveSystem.Save(State, m_VenueChoice);
            }

            StateChanged?.Invoke(State, m_Events);
            if (HasRival && m_RivalEvents.Count > 0) RivalChanged?.Invoke(Rival, m_RivalEvents);
        }

        void RecordResult()
        {
            if (HasRival) ProgressStore.RecordVersus(State.Phase == GamePhase.Victory);
            else if (State.IsEndless) ProgressStore.RecordEndlessRun(PartyEngine.StreamsLasted(State));
            else ProgressStore.RecordSeason(State.ScenarioId, State.Phase == GamePhase.Victory);
        }

        /// <summary>
        /// The AI's turn after your stream: its stream (random moves until it stops), then, unless the season is over, its
        /// shop and the start of its next stream. If it wins before you, your season is lost. Nothing happens once you've won.
        /// </summary>
        void PlayRivalTurn()
        {
            if (Rival.IsGameOver || State.Phase == GamePhase.Victory) return;

            // Its stream.
            for (int i = 0; i < MaxAiMoves && !Rival.IsGameOver && (Rival.Phase == GamePhase.Party || Rival.Phase == GamePhase.RaidBlame); i++)
            {
                var move = m_Ai.NextStreamMove(Rival);
                if (!move.HasValue || !PartyEngine.Execute(Rival, move.Value, m_RivalEvents).Ok) break;
            }
            // A stuck stream (no legal move found) still has to end.
            if (Rival.Phase == GamePhase.Party) PartyEngine.Execute(Rival, GameCommand.EndParty(), m_RivalEvents);
            if (Rival.Phase == GamePhase.RaidBlame && Rival.RaidSuspects.Count > 0)
                PartyEngine.Execute(Rival, GameCommand.Blame(Rival.RaidSuspects[0]), m_RivalEvents);

            if (Rival.Phase == GamePhase.Victory)
            {
                PartyEngine.LoseToRival(State, Rival.WinNight, m_Events);
                return;
            }
            if (Rival.IsGameOver || Rival.Phase != GamePhase.Planning) return;

            // Its shop, then its next stream (which it plays after your next one).
            for (int i = 0; i < SimpleAI.MaxShopMoves; i++)
            {
                var move = m_Ai.NextShopMove(Rival, i, !State.IsGameOver);
                if (!move.HasValue) break;
                bool ok = move.Value.Type == CommandType.UseStreamerPower && Rival.StreamerInfo.NeedsRival
                    ? PartyEngine.UseRivalPower(Rival, 1, State, 0, m_RivalEvents, m_Events).Ok
                    : PartyEngine.Execute(Rival, move.Value, m_RivalEvents).Ok;
                if (!ok) break;
            }
            if (!Rival.IsFinalNight) PartyEngine.Execute(Rival, GameCommand.StartNextNight(), m_RivalEvents);
        }

        /// <summary>
        /// Testing aid (editor and development builds only): grants banked Cash and Influence outside the rules.
        /// Not available in multiplayer, where the host's engine is the only authority.
        /// </summary>
        public void DebugGrant(int cashBanked, int influence)
        {
            if (State.IsGameOver) return;
            State.Cash += cashBanked;
            State.Influence += influence;
            if (m_Persist) SaveSystem.Save(State, m_VenueChoice);
            m_Events.Clear();
            StateChanged?.Invoke(State, m_Events);
        }

        public void Dispose()
        {
            StateChanged = null;
            CommandRejected = null;
            RivalChanged = null;
            TurnChanged = null;
        }
    }
}
