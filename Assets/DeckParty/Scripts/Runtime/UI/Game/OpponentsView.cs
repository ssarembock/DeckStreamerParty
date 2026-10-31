using System;
using System.Collections.Generic;
using DeckParty.Core;
using DeckParty.Networking;
using TMPro;
using UnityEngine;

namespace DeckParty.UI
{
    /// <summary>Live summary of every player in a multiplayer season. Click a player to watch their party.</summary>
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
            titleLabel.text = $"Players  <size=70%><color={modeColor}>{MatchModes.ShortName(mode)}</color></size>";

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
                m_Rows[i].Set(p.DisplayName, Status(p, mode, session.ActivePlayer == p.ClientId), color, mode != MatchMode.Race && p.ReadyNextNight,
                    session.IsHostPlayer(p.ClientId), p.Rank, p.ClientId == session.LocalClientId);
                m_Rows[i].SetViewed(p.ClientId == viewedId, color);
            }
            for (int i = players.Count; i < m_Rows.Count; i++) m_Rows[i].gameObject.SetActive(false);
        }

        void OnRowClicked(int index)
        {
            if (index < m_RowIds.Count && m_Rows[index].gameObject.activeSelf) PlayerClicked?.Invoke(m_RowIds[index]);
        }

        public static string Status(NetPlayer p, MatchMode mode, bool activeTurn)
        {
            if (!p.Connected) return "<color=#B9AED6>Left the game</color>";
            string partying = $"({p.Guests}/{p.Capacity}, <color=#FFC94A>{p.DiamondsInHouse} {Terms.Diamonds}</color>, risk {p.Trouble})";
            string phase;
            if (mode == MatchMode.Turns && !p.Finished)
            {
                if (activeTurn)
                {
                    phase = p.GamePhase == GamePhase.Party ? $"<color=#FF4F8B>Playing now</color> {partying}"
                        : p.GamePhase == GamePhase.RaidBlame ? "<color=#FF5A5A>Swatted!</color>"
                        : "<color=#FF4F8B>Shopping now</color>";
                }
                else
                {
                    phase = p.ReadyNextNight ? "<color=#6BE38B>Done tonight</color>" : "Waiting for their turn";
                }
            }
            else
            {
                bool sync = mode == MatchMode.SyncNights;
                switch (p.GamePhase)
                {
                    case GamePhase.Party: phase = $"Partying {partying}"; break;
                    case GamePhase.RaidBlame: phase = "<color=#FF5A5A>Swatted!</color>"; break;
                    case GamePhase.Planning: phase = sync && p.ReadyNextNight ? "<color=#6BE38B>Ready</color>" : "Planning"; break;
                    case GamePhase.Victory: phase = $"<color=#FFC94A>WON on night {p.WinNight}!</color>"; break;
                    default: phase = "Season over"; break;
                }
            }
            return $"Night {p.Night}  -  {phase}\n<color=#FFC94A>{p.Pop} {Terms.GiftedSubs}</color>  <color=#6BE38B>{p.Cash} $</color>  {Terms.Diamonds} owned {p.DiamondsOwned}  best {p.BestDiamonds}";
        }
    }
}
