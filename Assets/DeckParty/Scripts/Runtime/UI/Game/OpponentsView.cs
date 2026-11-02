using System;
using System.Collections.Generic;
using DeckParty.Core;
using DeckParty.Networking;
using TMPro;
using UnityEngine;

namespace DeckParty.UI
{
    /// <summary>Live summary of every player in a multiplayer season. Click a player to watch their stream.</summary>
    public sealed class OpponentsView : MonoBehaviour
    {
        [SerializeField] internal TMP_Text titleLabel;
        [SerializeField] internal RectTransform list;
        [SerializeField] internal PlayerRowView rowPrefab;

        public event Action<ulong> PlayerClicked;

        readonly List<PlayerRowView> m_Rows = new List<PlayerRowView>();
        readonly List<ulong> m_RowIds = new List<ulong>();

        public void Render(DeckPartyNetSession session, ulong viewedId = DeckPartyNetSession.NoPlayer)
        {
            if (session == null || !session.IsSpawned) return;
            var players = session.PlayersSnapshot();
            var mode = session.Settings.Mode;
            string modeColor = mode == MatchMode.Race ? "#FF9A3D" : mode == MatchMode.Turns ? "#FF4F8B" : "#B9AED6";
            titleLabel.text = Loc.Get("players.title") + $"  <size=70%><color={modeColor}>{MatchModes.ShortName(mode)}</color></size>";

            for (int i = 0; i < players.Count; i++)
            {
                if (i >= m_Rows.Count)
                {
                    var row = Instantiate(rowPrefab, list);
                    int index = m_Rows.Count;
                    row.MakeClickable();
                    row.Clicked += () => OnRowClicked(index);
                    m_Rows.Add(row);
                    m_RowIds.Add(0);
                }
                var p = players[i];
                var color = UIPalette.PlayerColor(p.ColorIndex);
                m_RowIds[i] = p.ClientId;
                m_Rows[i].gameObject.SetActive(true);
                m_Rows[i].Set(p.DisplayName, Status(p, mode, session.ActivePlayer == p.ClientId, session.Stage), color,
                    mode != MatchMode.Race && (p.ReadyNextNight || (mode == MatchMode.SyncNights && session.Stage != RoundStage.Shopping && p.ReadyForShop)),
                    session.IsHostPlayer(p.ClientId), p.Rank, p.ClientId == session.LocalClientId);
                m_Rows[i].SetStreamer((StreamerType)p.Streamer);
                m_Rows[i].SetViewed(p.ClientId == viewedId, color);
                m_Rows[i].SetPresence(PresenceLine != null ? PresenceLine(p.ClientId) : null);
            }
            for (int i = players.Count; i < m_Rows.Count; i++) m_Rows[i].gameObject.SetActive(false);
        }

        /// <summary>Multiplayer presence: what a player is doing right now ("choosing Trollface's target..."), or null.</summary>
        public Func<ulong, string> PresenceLine;

        public PlayerRowView RowFor(ulong clientId)
        {
            for (int i = 0; i < m_RowIds.Count && i < m_Rows.Count; i++)
                if (m_RowIds[i] == clientId && m_Rows[i].gameObject.activeSelf) return m_Rows[i];
            return null;
        }

        void OnRowClicked(int index)
        {
            if (index < m_RowIds.Count && m_Rows[index].gameObject.activeSelf) PlayerClicked?.Invoke(m_RowIds[index]);
        }

        /// <param name="stage">Free For All: where the round is (streams are private there, so no live details).</param>
        public static string Status(NetPlayer p, MatchMode mode, bool activeTurn, RoundStage stage = RoundStage.Shopping)
        {
            if (!p.Connected) return $"<color=#B9AED6>{Loc.Get("players.left")}</color>";
            string streaming = Loc.Get("players.live_detail", ("viewers", p.Guests), ("cap", p.Capacity),
                ("diamonds", $"<color=#FFC94A>{Terms.DiamondCount(p.DiamondsInHouse)}</color>"), ("risk", p.Trouble));
            string phase;
            if (mode == MatchMode.Turns && !p.Finished)
            {
                if (activeTurn)
                {
                    phase = p.GamePhase == GamePhase.Party ? $"<color=#FF4F8B>{Loc.Get("players.streaming_now")}</color> {streaming}"
                        : p.GamePhase == GamePhase.RaidBlame ? $"<color=#FF5A5A>{Terms.Raid}</color>"
                        : $"<color=#FF4F8B>{Loc.Get("players.shopping_now")}</color>";
                }
                else
                {
                    phase = p.ReadyNextNight ? $"<color=#6BE38B>{Loc.Get("players.done_tonight")}</color>" : Loc.Get("players.waiting_turn");
                }
            }
            else if (mode == MatchMode.SyncNights && !p.Finished)
            {
                switch (p.GamePhase)
                {
                    case GamePhase.Party: phase = $"<color=#FF4F8B>{Loc.Get("players.streaming")}</color>"; break;
                    case GamePhase.RaidBlame: phase = $"<color=#FF5A5A>{Terms.Raid}</color>"; break;
                    default:
                        phase = p.ReadyNextNight ? $"<color=#6BE38B>{Loc.Get("players.ready")}</color>"
                            : stage == RoundStage.Shopping ? $"<color=#FFC94A>{Loc.Get("players.shopping_now")}</color>"
                            : p.ReadyForShop ? $"<color=#6BE38B>{Loc.Get("players.ready_for_shop")}</color>"
                            : Loc.Get("players.stream_over");
                        break;
                }
            }
            else
            {
                bool sync = mode == MatchMode.SyncNights;
                switch (p.GamePhase)
                {
                    case GamePhase.Party: phase = Loc.Get("players.streaming") + " " + streaming; break;
                    case GamePhase.RaidBlame: phase = $"<color=#FF5A5A>{Terms.Raid}</color>"; break;
                    case GamePhase.Planning: phase = sync && p.ReadyNextNight ? $"<color=#6BE38B>{Loc.Get("players.ready")}</color>" : Loc.Get("players.planning"); break;
                    case GamePhase.Victory: phase = $"<color=#FFC94A>{Loc.Get("players.won", "night", p.WinNight)}</color>"; break;
                    default: phase = Loc.Get("players.season_over"); break;
                }
            }
            return Loc.Get("players.night", "night", p.Night) + "  -  " + phase + "\n" +
                   Loc.Get("players.wallet", ("subs", GuestVisuals.GiftedSubsText(p.Pop)), ("cash", $"<color=#6BE38B>{GameIcons.Amount(ResourceIcon.Cash, p.Cash)}</color>"),
                       ("owned", p.DiamondsOwned), ("best", p.BestDiamonds));
        }
    }
}
