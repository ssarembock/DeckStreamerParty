using System.Collections.Generic;
using System.Text;
using DeckParty.Core;
using UnityEngine;

namespace DeckParty.UI
{
    /// <summary>
    /// The game modes added on 2026-11-16 (additionalgamemodes.md): Versus the AI (the AI's row in the Players panel and a
    /// summary of its turns in the log), Endless (the shop's Add a Stream and Add a Viewer to the Shop) and the team member
    /// you are in 2v2 and co-op.
    /// </summary>
    public sealed partial class GameController
    {
        /// <summary>The single-player session (null in multiplayer).</summary>
        LocalGameSession m_Local;
        /// <summary>Teams (2v2, co-op): which member of your team's season you are (0 otherwise).</summary>
        int m_Member;
        /// <summary>Versus the AI: the AI's name (its streamer).</summary>
        string m_RivalName;
        /// <summary>What Play Again starts: the same mode and, versus the AI, the same rival streamer.</summary>
        GameMode m_LaunchMode;
        bool m_LaunchVersus;
        byte m_LaunchOpponent;
        /// <summary>Versus the AI: a short line on the AI's last stream for its row in the Players panel.</summary>
        string m_RivalLastLine;

        /// <summary>
        /// Teams: the member whose queue, Sub Cap and controls the screen shows: you on your own board, the player you're
        /// watching on theirs (0 outside team play).
        /// </summary>
        int ViewMember => ViewingOwn ? m_Member : m_Net != null ? m_Net.MemberOf(m_ViewId) : 0;

        bool HasLocalRival => m_Local != null && m_Local.HasRival;
        PlayerState LocalRival => m_Local != null ? m_Local.Rival : null;

        /// <summary>Whether a rival power has someone to hit: a rival online, or the AI while its season is on.</summary>
        bool RivalsAvailableFor(ulong clientId) =>
            (m_Net != null && m_Net.HasRival(clientId)) || (HasLocalRival && !LocalRival.IsGameOver);

        // ================================================================== Versus the AI

        /// <summary>
        /// Versus the AI: creates the AI's season (its own seed, so its queue isn't yours, but the same shop) and hooks up its
        /// turns. Nothing happens for a normal single-player season.
        /// </summary>
        void SetupRival(SinglePlayerLaunch launch, PlayerState state)
        {
            m_LaunchMode = state.Mode;
            m_LaunchVersus = launch != null && launch.VersusAi;
            m_LaunchOpponent = launch != null ? launch.OpponentStreamer : (byte)StreamerType.None;
            if (!m_LaunchVersus) return;

            uint rivalSeed = state.Seed ^ 0x5A17u;
            var streamer = StreamerCatalog.Resolve(launch.OpponentStreamer, new System.Random((int)rivalSeed));
            var rival = PartyEngine.CreateGame(state.ScenarioId, rivalSeed, state.TotalNights, streamer);
            // Same shop as yours (a Random Mix shop depends on the seed).
            rival.Shop.Clear();
            rival.Shop.AddRange(state.Shop);
            m_Local.AttachRival(rival, (int)(rivalSeed & 0x7FFFFFFF));
            m_Local.RivalChanged += OnRivalChanged;

            m_RivalName = streamer == StreamerType.None ? Loc.Get("select.opponent_algorithm") : StreamerVisuals.Name(streamer);
            opponents.gameObject.SetActive(true);
            log.Add(Loc.Get("game.versus_intro", "rival", RivalTag, "count", GameRules.DiamondsToWin));
        }

        string RivalTag => GuestVisuals.Colored(m_RivalName ?? Loc.Get("select.opponent_algorithm"), UIPalette.PlayerColor(1));

        /// <summary>The AI played its turn: what happened goes in the log (and a toast for its big moments).</summary>
        void OnRivalChanged(PlayerState rival, IReadOnlyList<GameEvent> events)
        {
            var bought = new List<string>();
            foreach (var e in events)
            {
                switch (e.Type)
                {
                    case GameEventType.PartyEnded:
                        m_RivalLastLine = Loc.Get("game.rival_scored", "values", EventText.Amounts(e.A, e.B), "trophies", e.C);
                        log.Add(Loc.Get("game.rival_stream", ("rival", RivalTag), ("values", EventText.Amounts(e.A, e.B)), ("count", e.C)));
                        break;
                    case GameEventType.PoliceRaid:
                        m_RivalLastLine = Loc.Get("game.rival_banned_short");
                        log.Add(Loc.Get("game.rival_banned", "rival", RivalTag));
                        break;
                    case GameEventType.FireMarshal:
                        m_RivalLastLine = Loc.Get("game.rival_down_short");
                        log.Add(Loc.Get("game.rival_down", "rival", RivalTag));
                        break;
                    case GameEventType.GuestPurchased:
                        bought.Add(GuestVisuals.NameTag(e.Guest));
                        break;
                    case GameEventType.PowerUsed:
                        log.Add(Loc.Get("game.rival_power", "rival", RivalTag, "power", StreamerVisuals.PowerName((StreamerType)e.A)));
                        break;
                    case GameEventType.Victory:
                        ToastView.Show(Loc.Get("game.rival_won_toast", "rival", RivalTag, "count", GameRules.DiamondsToWin), ToastKind.Warning);
                        break;
                }
            }
            if (bought.Count > 0) log.Add(Loc.Get("game.rival_bought", "rival", RivalTag, "viewers", string.Join(", ", bought)));
            RenderAll();
        }

        /// <summary>Versus the AI: you and the AI in the Players panel (Cash, Influence, Trophy viewers owned, its last stream).</summary>
        void RenderRivalPanel(PlayerState own)
        {
            var rival = LocalRival;
            if (rival == null || own == null) return;
            var rows = new List<OpponentsView.LocalRow>
            {
                new OpponentsView.LocalRow
                {
                    Name = Loc.Get("common.you"), Line = ResourceLine(own, null), Color = UIPalette.PlayerColor(0), Streamer = own.Streamer,
                    IsLocal = true, Done = own.IsGameOver,
                },
                new OpponentsView.LocalRow
                {
                    Name = Loc.Get("game.ai_name", "rival", m_RivalName), Line = ResourceLine(rival, m_RivalLastLine), Color = UIPalette.PlayerColor(1),
                    Streamer = rival.Streamer, Done = rival.IsGameOver,
                },
            };
            opponents.RenderLocal(Loc.Get("players.title_versus"), rows);
        }

        static string ResourceLine(PlayerState s, string extra)
        {
            var sb = new StringBuilder();
            sb.Append(GameIcons.Amount(ResourceIcon.Cash, s.Cash)).Append("   ")
              .Append(GameIcons.Amount(ResourceIcon.Influence, s.Influence)).Append("   ")
              .Append(GameIcons.Tag(ResourceIcon.Plays)).Append(' ').Append(s.DiamondsOwned());
            if (!string.IsNullOrEmpty(extra)) sb.Append('\n').Append(GuestVisuals.Colored(extra, UIPalette.TextDim));
            return sb.ToString();
        }

        // ================================================================== teams

        /// <summary>
        /// Teams (2v2, co-op): the names over the two sides of the chat, member 0 on the left and member 1 on the right
        /// ("You" on your own side). Cleared outside team play.
        /// </summary>
        void SetTeamSideNames(PlayerState s)
        {
            if (s == null || !s.IsTeam || m_Net == null)
            {
                house.LeftSideName = null;
                house.RightSideName = null;
                return;
            }
            int team = m_Net.TeamOf(ViewingOwn ? LocalId : m_ViewId);
            string left = null, right = null;
            foreach (var p in m_Net.TeamPlayers(team))
            {
                string name = p.ClientId == LocalId ? Loc.Get("team.you", "player", p.DisplayName) : p.DisplayName;
                if (p.Member == 0) left = name;
                else right = name;
            }
            house.LeftSideName = left;
            house.RightSideName = right;
        }

        // ================================================================== Endless: the shop's two Trophy buttons

        /// <summary>Endless: one more stream for Trophies.</summary>
        void BuyExtraStream()
        {
            var s = State;
            if (s == null || !CanAct) return;
            if (ShopGated)
            {
                OnCommandRejected("mp.shop_closed");
                return;
            }
            var blocker = PartyEngine.GetExtraStreamBlocker(s);
            if (blocker != null)
            {
                OnCommandRejected(blocker);
                return;
            }
            Submit(GameCommand.BuyExtraStream());
        }

        /// <summary>
        /// Endless: Add a Viewer to the Shop. The viewers on offer (a few the shop doesn't sell yet) open in the viewer picker;
        /// the one you click is added to the shop for the rest of the run, for Trophies.
        /// </summary>
        void OpenShopViewerChoice()
        {
            var s = State;
            if (s == null || !CanAct || s.Phase != GamePhase.Planning) return;
            if (ShopGated)
            {
                OnCommandRejected("mp.shop_closed");
                return;
            }
            var blocker = PartyEngine.GetShopViewerBlocker(s);
            if (blocker != null)
            {
                OnCommandRejected(blocker);
                return;
            }

            // The picker shows viewer cards from a state: a throwaway one holding one card per viewer on offer.
            var offer = new PlayerState { Phase = GamePhase.Planning };
            var ids = new List<int>();
            foreach (var type in PartyEngine.ShopViewerChoices(s))
            {
                var card = new GuestInstance { Id = offer.NextInstanceId++, Type = type };
                offer.Rolodex.Add(card);
                ids.Add(card.Id);
            }
            string price = GameIcons.Amount(ResourceIcon.Plays, PartyEngine.ShopViewerCost(s));
            picker.Open(Loc.Get("pick.shop_viewer_title", "cost", price), Loc.Get("pick.shop_viewer_hint"), offer, ids, pick =>
            {
                var chosen = offer.Find(pick);
                if (chosen != null) Submit(GameCommand.BuyShopViewer(chosen.Type));
            }, null, null, true);
        }
    }
}
