using System;
using System.Collections.Generic;
using DeckParty.Core;
using TMPro;
using UnityEngine;

namespace DeckParty.UI
{
    /// <summary>After a Platform Ban: pick which viewer gets a Temporary Ban (they sit out the next stream).</summary>
    public sealed class RaidBlameView : MonoBehaviour
    {
        [SerializeField] internal TMP_Text titleLabel;
        [SerializeField] internal TMP_Text messageLabel;
        [SerializeField] internal RectTransform grid;
        [SerializeField] internal GuestCardView cardPrefab;

        const float CardScale = 0.8f;

        public event Action<int> GuestChosen;

        readonly List<GuestCardView> m_Cards = new List<GuestCardView>();

        /// <param name="watching">When watching someone else, who's choosing (cards aren't clickable). Null for your own party.</param>
        public void Render(PlayerState s, string watching = null)
        {
            titleLabel.text = Terms.Raid;
            messageLabel.text = watching != null
                ? Loc.Get("ban.message") + "\n" + watching
                : Loc.Get("ban.message") + "\n" + Loc.Get("ban.choose");
            GuestCardView.FitGrid(grid, CardScale);

            for (int i = 0; i < s.RaidSuspects.Count; i++)
            {
                if (i >= m_Cards.Count)
                {
                    var created = Instantiate(cardPrefab, grid);
                    created.Clicked += card => GuestChosen?.Invoke(card.GuestId);
                    m_Cards.Add(created);
                }
                var view = m_Cards[i];
                var g = s.Find(s.RaidSuspects[i]);
                view.gameObject.SetActive(g != null);
                if (g == null) continue;
                view.ShowGuest(g, s, false);
                view.SetClickable(watching == null);
                view.SetHighlight(g.Info.Trouble ? CardHighlight.Selectable : CardHighlight.None);
                view.SetScale(CardScale);
            }
            for (int i = s.RaidSuspects.Count; i < m_Cards.Count; i++) m_Cards[i].gameObject.SetActive(false);
        }
    }
}
