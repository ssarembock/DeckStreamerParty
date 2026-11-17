using System;
using System.Collections.Generic;
using DeckParty.Core;

namespace DeckParty
{
    /// <summary>
    /// Couch Competitive (2026-11-17): two players on one computer, 1v1, taking turns like online Take Turns. A turn is
    /// your stream and then your shop; End Turn (the shop's Go Live button) hands over to the other player, who plays the
    /// same stream number. A player's next stream only starts when their turn comes round again, so a Raid or a Wealth Tax
    /// from the other player lands on it. The match ends after the round in which someone gets 4 Trophies in one stream
    /// (both: a draw), or after the last stream (the best single stream wins). The game screen shows a hand-over between
    /// turns and calls <see cref="PassTurn"/>. Not saved.
    /// </summary>
    public sealed partial class LocalGameSession
    {
        readonly PlayerState[] m_Seats = new PlayerState[2];

        /// <summary>Two players taking turns on this computer.</summary>
        public bool IsCouch { get; private set; }

        /// <summary>Whose turn it is: 0 = Player 1, 1 = Player 2.</summary>
        public int ActiveSeat { get; private set; }

        /// <summary>The player whose turn it is has finished it (End Turn, or their season ended); waiting for <see cref="PassTurn"/>.</summary>
        public bool TurnPending { get; private set; }

        /// <summary>The match is over (see <see cref="CouchWinner"/>).</summary>
        public bool CouchOver { get; private set; }

        /// <summary>The winner: 0 = Player 1, 1 = Player 2, -1 = a draw.</summary>
        public int CouchWinner { get; private set; } = -1;

        /// <summary>Passing the turn now ends the match (the round is complete and someone's season is over).</summary>
        public bool MatchEndsOnPass => IsCouch && TurnPending && ActiveSeat == 1 && (m_Seats[0].IsGameOver || m_Seats[1].IsGameOver);

        /// <summary>The turn passed (the screen shows the other player's board) or the match ended.</summary>
        public event Action TurnChanged;

        public PlayerState Seat(int seat) => IsCouch && seat >= 0 && seat < 2 ? m_Seats[seat] : seat == 0 ? m_Solo : null;

        /// <summary>Makes this a couch match: the session's own season is Player 1's, <paramref name="second"/> Player 2's.</summary>
        public void AttachCouch(PlayerState second)
        {
            m_Seats[0] = m_Solo;
            m_Seats[1] = second ?? throw new ArgumentNullException(nameof(second));
            IsCouch = true;
            ActiveSeat = 0;
        }

        void SubmitCouch(GameCommand command)
        {
            if (CouchOver || TurnPending) return;
            var s = State;
            int seat = ActiveSeat;
            var other = m_Seats[1 - seat];
            m_Events.Clear();
            m_RivalEvents.Clear();

            // End Turn: the shop's Go Live. The next stream starts when this player's turn comes round again.
            if (command.Type == CommandType.StartNextNight)
            {
                if (s.Phase != GamePhase.Planning || s.IsFinalNight)
                {
                    CommandRejected?.Invoke(Err.Of("err.finish_stream"));
                    return;
                }
                TurnPending = true;
                StateChanged?.Invoke(s, m_Events);
                return;
            }

            // Rival powers (Raid, Wealth Tax) hit the other player.
            var result = command.Type == CommandType.UseStreamerPower && s.StreamerInfo.NeedsRival
                ? PartyEngine.UseRivalPower(s, seat, other, 1 - seat, m_Events, m_RivalEvents)
                : PartyEngine.Execute(s, command, m_Events);
            if (!result.Ok)
            {
                CommandRejected?.Invoke(result.Error);
                return;
            }

            // A season that ended this turn (4 Trophies, or the last stream) hands over straight after its summary.
            if (s.IsGameOver) TurnPending = true;
            StateChanged?.Invoke(s, m_Events);
            if (m_RivalEvents.Count > 0) RivalChanged?.Invoke(other, m_RivalEvents);
        }

        /// <summary>
        /// The game screen is ready for the next turn: the other player's board comes up (their next stream starts if they were
        /// waiting in the shop), or, at the end of a round in which someone's season ended, the match ends.
        /// </summary>
        public void PassTurn()
        {
            if (!IsCouch || !TurnPending || CouchOver) return;
            TurnPending = false;
            if (MatchEndsOnPassAfter(ActiveSeat))
            {
                EndCouch();
                return;
            }

            ActiveSeat = 1 - ActiveSeat;
            m_Events.Clear();
            var next = State;
            if (next.Phase == GamePhase.Planning && !next.IsFinalNight) PartyEngine.Execute(next, GameCommand.StartNextNight(), m_Events);
            TurnChanged?.Invoke();
            StateChanged?.Invoke(next, m_Events);
        }

        bool MatchEndsOnPassAfter(int finished) => finished == 1 && (m_Seats[0].IsGameOver || m_Seats[1].IsGameOver);

        /// <summary>The round is over and someone's season ended: who won (4 Trophies first; else the best stream).</summary>
        void EndCouch()
        {
            var a = m_Seats[0];
            var b = m_Seats[1];
            bool aWon = a.Phase == GamePhase.Victory;
            bool bWon = b.Phase == GamePhase.Victory;
            var discard = new List<GameEvent>();
            if (aWon && !bWon)
            {
                CouchWinner = 0;
                PartyEngine.LoseToRival(b, a.WinNight, discard);
            }
            else if (bWon && !aWon)
            {
                CouchWinner = 1;
                PartyEngine.LoseToRival(a, b.WinNight, discard);
            }
            else if (aWon)
            {
                // Both got there in the same round.
                CouchWinner = -1;
            }
            else
            {
                // Nobody hit 4: the best single stream wins.
                CouchWinner = a.BestDiamonds > b.BestDiamonds ? 0 : b.BestDiamonds > a.BestDiamonds ? 1 : -1;
            }
            CouchOver = true;
            TurnChanged?.Invoke();
        }
    }
}
