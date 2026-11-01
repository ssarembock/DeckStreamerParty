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
            root.SetActive(false);
        }

        public void ShowSingle(PlayerState s, Action playAgain, Action mainMenu)
        {
            bool won = s.Phase == GamePhase.Victory;
            var scenario = ScenarioCatalog.Get(s.ScenarioId);
            titleLabel.text = Loc.Get(won ? "over.victory" : "over.season_over");
            titleLabel.color = won ? UIPalette.Gold : UIPalette.TextDim;

            if (won)
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

            if (matchOver && players.Count > 0)
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

        static string Stats(PlayerState s)
        {
            string text = Loc.Get("over.stats1", ("great", s.SuccessfulParties), ("banned", s.Raids), ("server_down", s.FireMarshals)) + "\n" +
                          Loc.Get("over.stats2", ("subs", s.TotalPopEarned), ("cash", s.TotalCashEarned), ("viewers", s.PermanentCount),
                              ("diamonds", s.DiamondsOwned()), ("cap", s.Capacity));
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
