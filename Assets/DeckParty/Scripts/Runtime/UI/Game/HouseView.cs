using System;
using System.Collections.Generic;
using DeckParty.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// The party: one card slot per Sub Cap space, laid out in rows of <see cref="GameRules.HouseColumns"/>.
    /// Cards scale to fit however big the Sub Cap grows.
    /// </summary>
    public sealed class HouseView : MonoBehaviour
    {
        [SerializeField] internal RectTransform area;
        [SerializeField] internal GridLayoutGroup grid;
        [SerializeField] internal GuestCardView cardPrefab;
        [SerializeField] internal TMP_Text headerLabel;
        [SerializeField] internal TMP_Text hintLabel;

        readonly List<GuestCardView> m_Cards = new List<GuestCardView>();
        int m_Capacity = -1;
        Vector2 m_LastAreaSize;

        public IReadOnlyList<GuestCardView> Cards => m_Cards;
        public event Action<GuestCardView> CardClicked;
        public event Action<GuestCardView> AbilityClicked;

        public GuestCardView FindCard(int guestId)
        {
            for (int i = 0; i < m_Cards.Count; i++)
                if (m_Cards[i].gameObject.activeSelf && m_Cards[i].GuestId == guestId) return m_Cards[i];
            return null;
        }

        void Update()
        {
            if (area != null && area.rect.size != m_LastAreaSize && m_Capacity > 0) Relayout(m_Capacity);
        }

        public void Render(PlayerState s)
        {
            EnsureCards(s.Capacity);
            if (s.Capacity != m_Capacity || area.rect.size != m_LastAreaSize) Relayout(s.Capacity);

            // Each guest takes one card; big guests (Big Chungus) also fill extra slots after their card.
            int slot = 0;
            for (int i = 0; i < s.House.Count; i++)
            {
                var g = s.Find(s.House[i]);
                if (g == null) continue;
                EnsureCards(slot + g.Info.Size);
                Show(slot++).ShowGuest(g, s, true);
                for (int extra = 1; extra < g.Info.Size; extra++) Show(slot++).ShowFiller(g.Kind);
            }
            for (; slot < s.Capacity; slot++) Show(slot).ShowEmpty();
            for (int i = slot; i < m_Cards.Count; i++) m_Cards[i].gameObject.SetActive(false);

            if (headerLabel != null)
                headerLabel.text = $"The Party  <size=75%><color=#B9AED6>{s.Occupancy}/{s.Capacity} {Terms.SubCap}  -  {s.Pool.Count} waiting outside</color></size>";

            if (hintLabel != null)
            {
                int trouble = PartyEngine.NetTrouble(s);
                if (PartyEngine.DiamondsPresent(s) >= GameRules.DiamondsToWin)
                    hintLabel.text = $"<color=#FFC94A><b>{GameRules.DiamondsToWin} {Terms.Diamonds} are here! End the party now to win!</b></color>";
                else if (trouble >= GameRules.RaidTroubleThreshold - 1)
                    hintLabel.text = $"<color=#FF5A5A>One more {Terms.Risk} and {Terms.RaidTeam} shows up.</color>";
                else if (s.EmptySpaces == 0)
                    hintLabel.text = $"<color=#FF9A3D>You've hit your {Terms.SubCap}. Use actions or end the party.</color>";
                else
                    hintLabel.text = string.Empty;
            }
        }

        GuestCardView Show(int slot)
        {
            var card = m_Cards[slot];
            card.gameObject.SetActive(true);
            return card;
        }

        void EnsureCards(int capacity)
        {
            while (m_Cards.Count < capacity)
            {
                var card = Instantiate(cardPrefab, grid.transform);
                card.name = "Slot " + (m_Cards.Count + 1);
                card.Clicked += c => CardClicked?.Invoke(c);
                card.AbilityClicked += c => AbilityClicked?.Invoke(c);
                m_Cards.Add(card);
            }
        }

        void Relayout(int capacity)
        {
            m_Capacity = capacity;
            m_LastAreaSize = area.rect.size;
            if (m_LastAreaSize.x <= 1f || m_LastAreaSize.y <= 1f) return;

            int columns = Mathf.Clamp(capacity, 1, GameRules.HouseColumns);
            int rows = Mathf.Max(1, Mathf.CeilToInt(capacity / (float)GameRules.HouseColumns));
            float spacing = Mathf.Lerp(18f, 8f, Mathf.InverseLerp(5, GameRules.MaxCapacity, capacity));
            grid.spacing = new Vector2(spacing, spacing);

            float cellW = (m_LastAreaSize.x - spacing * (columns - 1)) / columns;
            float cellH = (m_LastAreaSize.y - spacing * (rows - 1)) / rows;
            const float aspect = GuestCardView.DesignWidth / GuestCardView.DesignHeight;
            if (cellW / cellH > aspect) cellW = cellH * aspect;
            else cellH = cellW / aspect;

            float scale = Mathf.Min(cellW / GuestCardView.DesignWidth, 1.15f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.cellSize = new Vector2(GuestCardView.DesignWidth * scale, GuestCardView.DesignHeight * scale);

            foreach (var card in m_Cards) card.SetScale(scale);
        }
    }
}
