using System;
using System.Collections.Generic;
using DeckParty.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// Modal grid for choosing a viewer who isn't in chat (Shoutout, Grumpy Cat, Distracted Boyfriend...).
    /// Identical viewers are stacked unless grouping is turned off.
    /// </summary>
    public sealed class GuestPickerView : MonoBehaviour
    {
        [SerializeField] internal GameObject root;
        [SerializeField] internal TMP_Text titleLabel;
        [SerializeField] internal TMP_Text hintLabel;
        [SerializeField] internal RectTransform grid;
        [SerializeField] internal GuestCardView cardPrefab;
        [SerializeField] internal Button cancelButton;

        const float CardScale = 0.72f;

        readonly List<GuestCardView> m_Cards = new List<GuestCardView>();
        Action<int> m_OnPick;
        Action m_OnCancel;
        TMP_Text m_CancelLabel;

        public bool IsOpen => root.activeSelf;
        /// <summary>A card in the picker was hovered (viewer id) or left (-1). Used for multiplayer presence.</summary>
        public event Action<int> CardHovered;

        public GuestCardView FindCard(int guestId)
        {
            foreach (var card in m_Cards)
                if (card.gameObject.activeSelf && card.GuestId == guestId) return card;
            return null;
        }

        void Awake()
        {
            cancelButton.onClick.AddListener(Cancel);
            m_CancelLabel = cancelButton.GetComponentInChildren<TMP_Text>(true);
            root.SetActive(false);
        }

        /// <summary>
        /// Opens the picker. <paramref name="onCancel"/> runs when the player backs out (Cancel button or Esc);
        /// <paramref name="keepOrder"/> shows the guests in the given order (one card each) instead of grouped.
        /// </summary>
        public void Open(string title, string hint, PlayerState s, IReadOnlyList<int> ids, Action<int> onPick,
            Action onCancel = null, string cancelLabel = null, bool keepOrder = false)
        {
            m_OnPick = onPick;
            m_OnCancel = onCancel;
            titleLabel.text = title;
            hintLabel.text = hint;
            if (m_CancelLabel != null) m_CancelLabel.text = string.IsNullOrEmpty(cancelLabel) ? Loc.Get("common.cancel_esc") : cancelLabel;
            GuestCardView.FitGrid(grid, CardScale);

            var groups = new List<(GuestInstance guest, int count)>();
            foreach (var id in ids)
            {
                var g = s.Find(id);
                if (g == null) continue;
                int found = -1;
                for (int i = 0; !keepOrder && i < groups.Count; i++)
                {
                    var other = groups[i].guest;
                    if (other.Type == g.Type && other.BonusPop == g.BonusPop && other.StrikeCount == g.StrikeCount && other.Temporary == g.Temporary)
                    {
                        found = i;
                        break;
                    }
                }
                if (found >= 0) groups[found] = (groups[found].guest, groups[found].count + 1);
                else groups.Add((g, 1));
            }

            if (!keepOrder)
            {
                groups.Sort((a, b) =>
                {
                    if (a.guest.BaseInfo.IsDiamond != b.guest.BaseInfo.IsDiamond) return a.guest.BaseInfo.IsDiamond ? -1 : 1;
                    return string.CompareOrdinal(a.guest.BaseInfo.Name, b.guest.BaseInfo.Name);
                });
            }

            for (int i = 0; i < groups.Count; i++)
            {
                if (i >= m_Cards.Count)
                {
                    var created = Instantiate(cardPrefab, grid);
                    created.Clicked += Pick;
                    created.Hovered += (c, on) => CardHovered?.Invoke(on ? c.GuestId : -1);
                    m_Cards.Add(created);
                }
                var card = m_Cards[i];
                card.gameObject.SetActive(true);
                card.ShowGuest(groups[i].guest, s, false);
                card.SetCount(groups[i].count);
                card.SetClickable(true);
                card.SetHighlight(CardHighlight.None);
                card.SetScale(CardScale);
            }
            for (int i = groups.Count; i < m_Cards.Count; i++) m_Cards[i].gameObject.SetActive(false);

            root.SetActive(true);
            root.transform.SetAsLastSibling();
            UITween.PopIn(this, root.transform, 0.2f, 0f, 0.9f);
        }

        void Pick(GuestCardView card)
        {
            var callback = m_OnPick;
            int id = card.GuestId;
            Close();
            callback?.Invoke(id);
        }

        /// <summary>The player backed out: runs the cancel callback, then closes.</summary>
        public void Cancel()
        {
            var callback = m_OnCancel;
            Close();
            callback?.Invoke();
        }

        /// <summary>Closes without running any callback.</summary>
        public void Close()
        {
            m_OnPick = null;
            m_OnCancel = null;
            root.SetActive(false);
        }
    }
}
