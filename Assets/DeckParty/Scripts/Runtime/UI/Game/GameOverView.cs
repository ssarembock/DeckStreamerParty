using System;
using System.Collections.Generic;
using DeckParty.Core;
using DeckParty.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>End-of-season screen: victory/defeat for single player, rankings for multiplayer.</summary>
    public sealed class GameOverView : MonoBehaviour
    {
        [SerializeField] internal GameObject root;
        [SerializeField] internal TMP_Text titleLabel;
        [SerializeField] internal TMP_Text subtitleLabel;
        [SerializeField] internal TMP_Text statsLabel;
        [SerializeField] internal GameObject rankingGroup;
        [SerializeField] internal RectTransform rankingList;
        [SerializeField] internal PlayerRowView rowPrefab;
        [SerializeField] internal TMP_Text waitingLabel;
        [SerializeField] internal Button primaryButton;
        [SerializeField] internal TMP_Text primaryLabel;
        [SerializeField] internal Button secondaryButton;
        [SerializeField] internal TMP_Text secondaryLabel;

        readonly List<PlayerRowView> m_Rows = new List<PlayerRowView>();
        Action m_Primary;
        Action m_Secondary;

        public bool IsOpen => root.activeSelf;

        void Awake()
        {
            primaryButton.onClick.AddListener(() => m_Primary?.Invoke());
            secondaryButton.onClick.AddListener(() => m_Secondary?.Invoke());
            PadDefaultFocus.Mark(primaryButton, 60);
            root.SetActive(false);
        }

        /// <param name="rival">Versus the AI: the AI's season (null otherwise).</param>
        /// <param name="rivalName">Versus the AI: the AI's name.</param>
        public void ShowSingle(PlayerState s, Action playAgain, Action mainMenu, PlayerState rival = null, string rivalName = null)
        {
            bool won = s.Phase == GamePhase.Victory;
            var scenario = ScenarioCatalog.Get(s.ScenarioId);
            titleLabel.text = Loc.Get(won ? "over.victory" : "over.season_over");
            titleLabel.color = won ? UIPalette.Gold : UIPalette.TextDim;

            if (s.IsEndless)
            {
                // Endless: no win, just how long the run lasted.
                int streams = PartyEngine.StreamsLasted(s);
                bool best = streams >= ProgressStore.BestEndlessRun;
                titleLabel.text = Loc.Get("over.endless_title");
                titleLabel.color = best ? UIPalette.Gold : UIPalette.Teal;
                subtitleLabel.text = Loc.Get("over.endless_detail", "count", streams, "bought", s.StreamsBought) + "\n" +
                                     (best ? $"<color=#FFC94A>{Loc.Get("over.endless_new_best")}</color>" : Loc.Get("modes.endless_best", "count", ProgressStore.BestEndlessRun));
            }
            else if (rival != null)
            {
                // Versus the AI: you, the AI, or nobody.
                bool rivalWon = !won && rival.Phase == GamePhase.Victory;
                titleLabel.text = won ? Loc.Get("over.versus_won", "rival", rivalName) : rivalWon ? Loc.Get("over.versus_lost", "rival", rivalName) : Loc.Get("over.versus_nobody");
                titleLabel.color = won ? UIPalette.Gold : rivalWon ? UIPalette.Red : UIPalette.TextDim;
                subtitleLabel.text = won
                    ? Loc.Get("over.won_detail", ("count", GameRules.DiamondsToWin), ("night", s.WinNight), ("total", s.TotalNights), ("set", ContentText.ScenarioName(s.ScenarioId)))
                    : rivalWon
                        ? Loc.Get("over.versus_lost_detail", ("rival", rivalName), ("count", GameRules.DiamondsToWin), ("night", rival.WinNight))
                        : Loc.Get("over.versus_nobody_detail", ("best", Terms.DiamondCount(s.BestDiamonds)), ("rival", rivalName), ("rival_best", Terms.DiamondCount(rival.BestDiamonds)));
                subtitleLabel.text += "\n" + Loc.Get("single.versus_record", "wins", ProgressStore.VersusWins, "losses", ProgressStore.VersusLosses);
            }
            else if (won)
            {
                subtitleLabel.text = Loc.Get("over.won_detail", ("count", GameRules.DiamondsToWin), ("night", s.WinNight), ("total", s.TotalNights), ("set", ContentText.ScenarioName(s.ScenarioId)));
                if (scenario.IsRandom)
                    subtitleLabel.text += "\n" + Loc.Get("over.random_streak", "streak", ProgressStore.RandomStreak, "goal", ProgressStore.CherryStreak) +
                                          (ProgressStore.HasCherry ? $"  <color=#FF4F8B>{Loc.Get("over.cherry")}</color>" : string.Empty);
            }
            else
            {
                subtitleLabel.text = Loc.Get("over.lost_detail", "count", GameRules.DiamondsToWin, "best", Terms.DiamondCount(s.BestDiamonds));
            }

            statsLabel.text = Stats(s);
            rankingGroup.SetActive(false);
            waitingLabel.text = string.Empty;
            SetButtons(Loc.Get("over.play_again"), playAgain, Loc.Get("over.main_menu"), mainMenu);
            Open();
        }

        public void ShowMultiplayer(DeckPartyNetSession net, PlayerState local, bool matchOver, Action primary, string primaryText, Action secondary, string secondaryText, string waiting)
        {
            var players = net.PlayersSnapshot();
            players.Sort((a, b) =>
            {
                if (a.Rank != 0 && b.Rank != 0) return a.Rank.CompareTo(b.Rank);
                if (a.Won != b.Won) return a.Won ? -1 : 1;
                return b.BestDiamonds.CompareTo(a.BestDiamonds);
            });

            if (net.Settings.Mode == MatchMode.CoopEndless && local != null && (matchOver || local.IsGameOver))
            {
                // Co-op: no winner, the run you lasted together.
                titleLabel.text = Loc.Get("over.coop_title");
                titleLabel.color = UIPalette.Teal;
                subtitleLabel.text = Loc.Get("over.coop_detail", "team", net.TeamName(0), "count", PartyEngine.StreamsLasted(local));
            }
            else if (matchOver && players.Count > 0 && net.IsTeamMode)
            {
                // 2v2: the winning team (you share your teammate's result).
                var winner = players[0];
                bool localWon = winner.Team == net.TeamOf(net.LocalClientId);
                string team = net.TeamName(winner.Team);
                titleLabel.text = localWon ? Loc.Get("over.team_you_win") : Loc.Get("over.team_wins", "team", team);
                titleLabel.color = localWon ? UIPalette.Gold : UIPalette.PlayerColor(winner.ColorIndex);
                subtitleLabel.text = winner.Won
                    ? Loc.Get("over.mp_won_detail", "count", GameRules.DiamondsToWin, "night", winner.WinNight)
                    : Loc.Get("over.mp_nobody", "count", GameRules.DiamondsToWin, "best", winner.BestDiamonds);
            }
            else if (matchOver && players.Count > 0)
            {
                var winner = players[0];
                bool localWon = winner.ClientId == net.LocalClientId;
                titleLabel.text = localWon ? Loc.Get("over.you_win") : Loc.Get("over.player_wins", "player", winner.DisplayName);
                titleLabel.color = localWon ? UIPalette.Gold : UIPalette.PlayerColor(winner.ColorIndex);
                subtitleLabel.text = winner.Won
                    ? Loc.Get("over.mp_won_detail", "count", GameRules.DiamondsToWin, "night", winner.WinNight)
                    : Loc.Get("over.mp_nobody", "count", GameRules.DiamondsToWin, "best", winner.BestDiamonds);
            }
            else
            {
                bool won = local != null && local.Phase == GamePhase.Victory;
                titleLabel.text = won ? Loc.Get("over.diamond_stream", "count", GameRules.DiamondsToWin) : Loc.Get("over.your_season_over");
                titleLabel.color = won ? UIPalette.Gold : UIPalette.TextDim;
                subtitleLabel.text = Loc.Get("over.waiting_others");
            }

            statsLabel.text = local != null ? Stats(local) : string.Empty;

            rankingGroup.SetActive(true);
            var mode = net.Settings.Mode;
            for (int i = 0; i < players.Count; i++)
            {
                if (i >= m_Rows.Count) m_Rows.Add(Instantiate(rowPrefab, rankingList));
                var p = players[i];
                m_Rows[i].gameObject.SetActive(true);
                m_Rows[i].Set(p.DisplayName, OpponentsView.Status(p, mode, net.ActivePlayer == p.ClientId), UIPalette.PlayerColor(p.ColorIndex), false,
                    net.IsHostPlayer(p.ClientId), matchOver ? p.Rank : 0, p.ClientId == net.LocalClientId);
            }
            for (int i = players.Count; i < m_Rows.Count; i++) m_Rows[i].gameObject.SetActive(false);

            waitingLabel.text = waiting ?? string.Empty;
            SetButtons(primaryText, primary, secondaryText, secondary);
            Open();
        }

        /// <summary>
        /// Couch Competitive: who won (or a draw), on which stream, and each player's best stream; the stats of the winner (or
        /// Player 1 on a draw).
        /// </summary>
        public void ShowCouch(PlayerState first, PlayerState second, int winner, string firstName, string secondName, Action playAgain, Action mainMenu)
        {
            var players = new[] { first, second };
            var names = new[] { firstName, secondName };
            if (winner >= 0)
            {
                var won = players[winner];
                titleLabel.text = Loc.Get("over.player_wins", "player", names[winner]);
                titleLabel.color = UIPalette.PlayerColor(winner);
                subtitleLabel.text = won.Phase == GamePhase.Victory
                    ? Loc.Get("couch.won_detail", ("player", names[winner]), ("count", GameRules.DiamondsToWin), ("night", won.WinNight))
                    : Loc.Get("couch.won_best_detail", ("player", names[winner]), ("count", won.BestDiamonds));
            }
            else
            {
                titleLabel.text = Loc.Get("couch.draw");
                titleLabel.color = UIPalette.TextDim;
                subtitleLabel.text = Loc.Get("couch.draw_detail");
            }
            subtitleLabel.text += "\n" + Loc.Get("couch.best_line", ("first", firstName), ("first_best", first.BestDiamonds), ("second", secondName), ("second_best", second.BestDiamonds));

            statsLabel.text = Stats(players[Mathf.Max(0, winner)]);
            rankingGroup.SetActive(false);
            waitingLabel.text = string.Empty;
            SetButtons(Loc.Get("over.play_again"), playAgain, Loc.Get("over.main_menu"), mainMenu);
            Open();
        }

        static string Stats(PlayerState s)
        {
            string text = Loc.Get("over.stats1", ("great", s.SuccessfulParties), ("banned", s.Raids), ("server_down", s.FireMarshals)) + "\n" +
                          Loc.Get("over.stats2", ("cash", s.TotalCashEarned), ("influence", s.TotalInfluenceEarned), ("viewers", s.PermanentCount),
                              ("diamonds", s.DiamondsOwned()), ("cap", s.TotalCapacity));
            if (s.IsEndless)
                text += "\n" + Loc.Get("over.stats_endless", ("trophies", s.Trophies), ("streams", s.StreamsBought), ("viewers", s.ShopViewersBought));
            if (s.Streamer != StreamerType.None)
                text += "\n" + Loc.Get("over.stats_streamer", "streamer", StreamerVisuals.NameTag(s.Streamer), "uses", s.PowerUses);
            return text;
        }

        void SetButtons(string primaryText, Action primary, string secondaryText, Action secondary)
        {
            m_Primary = primary;
            m_Secondary = secondary;
            primaryButton.gameObject.SetActive(primary != null);
            primaryLabel.text = primaryText;
            secondaryButton.gameObject.SetActive(secondary != null);
            secondaryLabel.text = secondaryText;
        }

        void Open()
        {
            if (root.activeSelf) return;
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            UITween.PopIn(this, root.transform, 0.35f, 0f, 0.7f);
        }

        public void Hide() => root.SetActive(false);
    }
}
