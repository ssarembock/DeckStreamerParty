using System.Collections.Generic;
using DeckParty.Core;
using UnityEngine;

namespace DeckParty.UI
{
    /// <summary>
    /// Couch Competitive (2026-11-17): two players on this computer take turns (stream, then shop) on the one screen, with
    /// the keyboard and mouse, controllers, or both. Between turns a hand-over asks the next player to take the controls;
    /// the board then shows their season. The Players panel lists both, with whose turn it is.
    /// </summary>
    public sealed partial class GameController
    {
        bool m_HandoverShown;
        /// <summary>The launch, for Play Again (the same two streamers).</summary>
        byte m_CouchStreamer1;
        byte m_CouchStreamer2;

        bool IsCouch => m_Local != null && m_Local.IsCouch;

        /// <summary>"Player 1" / "Player 2".</summary>
        static string CouchName(int seat) => Loc.Get("couch.player", "number", seat + 1);

        static string CouchTag(int seat) => GuestVisuals.Colored(CouchName(seat), UIPalette.PlayerColor(seat));

        /// <summary>Couch play: whose turn it is (named in the win and season-over banners), or null.</summary>
        string CouchWho => IsCouch ? CouchName(m_Local.ActiveSeat) : null;

        /// <summary>
        /// Couch play: Player 2's season (its own seed, so its queue isn't Player 1's, but the same shop), the turn hand-over and
        /// the Players panel. Nothing happens for any other season.
        /// </summary>
        void SetupCouch(SinglePlayerLaunch launch, PlayerState first)
        {
            if (launch == null || !launch.Couch) return;
            m_CouchStreamer1 = launch.Streamer;
            m_CouchStreamer2 = launch.OpponentStreamer;

            uint seed = first.Seed ^ 0xC0C4u;
            var streamer = StreamerCatalog.Resolve(launch.OpponentStreamer, new System.Random((int)seed), Unlocks.StreamerMask());
            var second = PartyEngine.CreateGame(first.ScenarioId, seed, first.TotalNights, streamer, trophiesToWin: first.TrophiesToWin);
            second.Shop.Clear();
            second.Shop.AddRange(first.Shop);
            m_Local.AttachCouch(second);
            m_Local.TurnChanged += OnCouchTurnChanged;
            m_Local.RivalChanged += (_, __) => RenderAll();

            opponents.gameObject.SetActive(true);
            DockNotices();
            log.Add(Loc.Get("couch.intro", ("first", CouchTag(0)), ("second", CouchTag(1)), ("count", PartyEngine.TrophiesToWin(first))));
        }

        /// <summary>
        /// Each frame: once the player whose turn it was is done (End Turn, or their season ended) and nothing else is on
        /// screen, ask the next player to take over. The last turn of a match that's over just ends it.
        /// </summary>
        void UpdateCouch()
        {
            if (!IsCouch || !m_Local.TurnPending || m_HandoverShown || m_Local.CouchOver) return;
            if (m_Scoring || RecapOpen || ModalDialog.IsOpen || picker.IsOpen || pauseMenu.IsOpen) return;
            // Let the stream's summary and banners land first.
            if (banner != null && banner.IsShowing) return;

            if (m_Local.MatchEndsOnPass)
            {
                m_Local.PassTurn();
                return;
            }
            m_HandoverShown = true;
            int next = 1 - m_Local.ActiveSeat;
            ModalDialog.Show(Loc.Get("couch.handover_title", "player", CouchName(next)), Loc.Get("couch.handover_body", "player", CouchName(next)), () =>
            {
                m_HandoverShown = false;
                m_Local.PassTurn();
            }, Loc.Get("couch.handover_go"), null, () => m_HandoverShown = false);
            // Closed without starting (Esc / Back): it simply comes back next frame, as the turn still has to pass.
        }

        /// <summary>The turn passed: the board shows the other player's season (nothing of the last board carries over).</summary>
        void OnCouchTurnChanged()
        {
            CancelTargeting(false);
            if (picker.IsOpen) picker.Close();
            if (rolodex.IsOpen) rolodex.Close();
            m_LastLive = null;
            m_Arrivals.Clear();
            m_Pulses.Clear();
            if (presence != null) presence.Clear();
            if (viewerEmojis != null) viewerEmojis.Clear();
            if (planning.power != null) planning.power.ResetPose();
            if (outcomeSplash != null) outcomeSplash.Hide();
            PadNavigator.RequestRefocus();

            if (m_Local.CouchOver)
            {
                RenderAll();
                CheckGameOver(State);
                return;
            }

            var s = State;
            TrackRecap(LocalId, s, null);
            SiteTheme.Apply(StreamerVisuals.Platform(s.Streamer), true);
            ApplyVenue(true);
            log.Add(Loc.Get("couch.turn_log", "player", CouchTag(m_Local.ActiveSeat), "night", s.Night));
            RenderAll();
            banner.Show(Loc.Get("couch.turn_title", "player", CouchName(m_Local.ActiveSeat)),
                Loc.Get("couch.turn_sub", "night", s.Night, "total", s.TotalNights), UIPalette.PlayerColor(m_Local.ActiveSeat), 1.4f);
        }

        /// <summary>Couch play: both players in the Players panel, whose turn it is first.</summary>
        void RenderCouchPanel()
        {
            var rows = new List<OpponentsView.LocalRow>();
            for (int seat = 0; seat < 2; seat++)
            {
                var s = m_Local.Seat(seat);
                if (s == null) continue;
                bool active = seat == m_Local.ActiveSeat && !m_Local.CouchOver;
                rows.Add(new OpponentsView.LocalRow
                {
                    Name = CouchName(seat),
                    Line = ResourceLine(s, active ? Loc.Get("couch.their_turn") : null),
                    Color = UIPalette.PlayerColor(seat),
                    Streamer = s.Streamer,
                    IsLocal = active,
                    Done = s.IsGameOver,
                });
            }
            opponents.RenderLocal(Loc.Get("players.title_couch"), rows);
        }

        /// <summary>Couch play's shop: Go Live ends your turn ("End Turn"), and says who plays next.</summary>
        void FillCouchPlanning(ref PlanningContext ctx)
        {
            ctx.Multiplayer = true;
            ctx.Turns = true;
            ctx.MyTurn = true;
            ctx.ActiveName = CouchName(m_Local.ActiveSeat);
            ctx.NextName = CouchName(1 - m_Local.ActiveSeat);
            ctx.RivalsAvailable = !m_Local.Rival.IsGameOver;
        }

        void ShowCouchGameOver()
        {
            gameOver.ShowCouch(m_Local.Seat(0), m_Local.Seat(1), m_Local.CouchWinner, CouchName(0), CouchName(1), PlayAgainCouch, () => SceneFlow.GoToMainMenu());
        }

        void PlayAgainCouch()
        {
            var s = m_Local.Seat(0);
            SceneFlow.StartSinglePlayer(new SinglePlayerLaunch
            {
                ScenarioId = s != null ? s.ScenarioId : 0,
                Seed = NewSeed(),
                VenueChoice = m_VenueChoice,
                Streamer = m_CouchStreamer1,
                OpponentStreamer = m_CouchStreamer2,
                Couch = true,
            });
        }
    }
}
