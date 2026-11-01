using System;
using System.Collections.Generic;
using DeckParty.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// The live chat: one card slot per Sub Cap space. The column count adapts to the screen's shape (more columns on
    /// ultrawide) and cards shrink to fit, but never below <see cref="MinCardScale"/>: past that the area scrolls.
    /// </summary>
    public sealed class HouseView : MonoBehaviour
    {
        /// <summary>Cards never get smaller than this (as a fraction of the 200x290 design size); the chat scrolls instead.</summary>
        public const float MinCardScale = 0.62f;
        /// <summary>Cards never get bigger than this.</summary>
        public const float MaxCardScale = 1.15f;
        /// <summary>Most columns at 16:9; wider screens get proportionally more, up to <see cref="MaxColumns"/>.</summary>
        public const int BaseColumns = GameRules.HouseColumns;
        public const int MaxColumns = 12;

        [SerializeField] internal RectTransform area;
        [SerializeField] internal GridLayoutGroup grid;
        [SerializeField] internal GuestCardView cardPrefab;
        [SerializeField] internal TMP_Text headerLabel;
        [SerializeField] internal TMP_Text hintLabel;
        [Tooltip("Optional: lets the chat scroll when cards hit their minimum size.")]
        [SerializeField] internal ScrollRect scroll;

        readonly List<GuestCardView> m_Cards = new List<GuestCardView>();
        int m_Capacity = -1;
        Vector2 m_LastAreaSize;
        PlayerState m_State;

        public IReadOnlyList<GuestCardView> Cards => m_Cards;
        public float CardScale { get; private set; } = 1f;
        public event Action<GuestCardView> CardClicked;
        public event Action<GuestCardView> AbilityClicked;
        public event Action<GuestCardView, bool> CardHovered;

        public GuestCardView FindCard(int guestId)
        {
            for (int i = 0; i < m_Cards.Count; i++)
                if (m_Cards[i].gameObject.activeSelf && m_Cards[i].GuestId == guestId) return m_Cards[i];
            return null;
        }

        void Awake() => Loc.Changed += OnLanguageChanged;
        void OnDestroy() => Loc.Changed -= OnLanguageChanged;

        void OnLanguageChanged()
        {
            if (m_State != null) Render(m_State);
        }

        void Update()
        {
            if (Viewport != null && Viewport.rect.size != m_LastAreaSize && m_Capacity > 0) Relayout(m_Capacity);
        }

        RectTransform Viewport => scroll != null && scroll.viewport != null ? scroll.viewport : area;

        public void Render(PlayerState s)
        {
            m_State = s;
            EnsureCards(s.Capacity);
            if (s.Capacity != m_Capacity || Viewport.rect.size != m_LastAreaSize) Relayout(s.Capacity);

            // Each viewer takes one card; big viewers (Big Chungus) also fill extra slots after their card.
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
                headerLabel.text = Loc.Get("chat.header", ("filled", s.Occupancy), ("cap", s.Capacity), ("queue", s.Pool.Count));

            if (hintLabel != null)
            {
                int trouble = PartyEngine.NetTrouble(s);
                if (PartyEngine.DiamondsPresent(s) >= GameRules.DiamondsToWin)
                    hintLabel.text = $"<color=#FFC94A><b>{Loc.Get("chat.hint.win", "count", GameRules.DiamondsToWin)}</b></color>";
                else if (trouble >= GameRules.RaidTroubleThreshold - 1)
                    hintLabel.text = $"<color=#FF5A5A>{Loc.Get("chat.hint.risk")}</color>";
                else if (s.EmptySpaces == 0)
                    hintLabel.text = $"<color=#FF9A3D>{Loc.Get("chat.hint.full")}</color>";
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
                card.Hovered += (c, on) => CardHovered?.Invoke(c, on);
                m_Cards.Add(card);
            }
        }

        void Relayout(int capacity)
        {
            m_Capacity = capacity;
            m_LastAreaSize = Viewport.rect.size;
            if (m_LastAreaSize.x <= 1f || m_LastAreaSize.y <= 1f) return;

            float spacing = Mathf.Lerp(18f, 8f, Mathf.InverseLerp(5, GameRules.MaxCapacity, capacity));
            grid.spacing = new Vector2(spacing, spacing);
            var padding = grid.padding;
            float width = m_LastAreaSize.x - padding.horizontal;
            float height = m_LastAreaSize.y - padding.vertical;

            // Wider screens allow more columns (16:9 -> 9, 21:9 -> 11...).
            float aspect = m_LastAreaSize.x / Mathf.Max(1f, m_LastAreaSize.y);
            const float referenceAspect = 1464f / 790f; // the chat area's shape on a 1920x1080 screen
            int maxColumns = Mathf.Clamp(Mathf.RoundToInt(BaseColumns * aspect / referenceAspect), 6, MaxColumns);

            // Pick the column count that gives the biggest cards while everything fits.
            int bestColumns = 1;
            float bestScale = 0f;
            for (int columns = 1; columns <= Mathf.Min(capacity, maxColumns); columns++)
            {
                int rows = Mathf.CeilToInt(capacity / (float)columns);
                float scale = Mathf.Min(CardScaleFor(width, columns, spacing, true), CardScaleFor(height, rows, spacing, false));
                if (scale > bestScale + 0.001f)
                {
                    bestScale = scale;
                    bestColumns = columns;
                }
            }

            float finalScale = Mathf.Min(bestScale, MaxCardScale);
            int finalColumns = bestColumns;
            if (finalScale < MinCardScale)
            {
                // Too many to fit: keep cards readable and let the area scroll.
                finalScale = MinCardScale;
                float cellW = GuestCardView.DesignWidth * finalScale;
                finalColumns = Mathf.Clamp(Mathf.FloorToInt((width + spacing) / (cellW + spacing)), 1, maxColumns);
            }

            CardScale = finalScale;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = finalColumns;
            grid.cellSize = GuestCardView.CellSize(finalScale);
            if (scroll != null) scroll.vertical = finalScale <= MinCardScale + 0.001f;

            foreach (var card in m_Cards) card.SetScale(finalScale);
        }

        static float CardScaleFor(float space, int count, float spacing, bool horizontal)
        {
            float cell = (space - spacing * (count - 1)) / count;
            return cell / (horizontal ? GuestCardView.DesignWidth : GuestCardView.DesignHeight);
        }
    }
}
