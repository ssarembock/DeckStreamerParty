using System;
using System.Collections.Generic;
using DeckParty.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// The live chat: one card slot per Sub Cap space, in rows of at most <see cref="GameRules.HouseColumns"/>, so the chat
    /// grows as a rectangle and starts a new row each time one fills. Cards shrink to fit, but never below
    /// <see cref="MinCardScale"/>: then rows may get longer, and past that the area scrolls. The cards keep clear of
    /// <see cref="avoid"/> (the facecam in the stream's bottom-right corner).
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
        [Tooltip("Optional: a rect overlapping the chat that the cards keep clear of (the facecam).")]
        [SerializeField] internal RectTransform avoid;

        /// <summary>Gap kept between the cards and <see cref="avoid"/>.</summary>
        const float AvoidGap = 12f;

        static readonly Vector3[] s_Corners = new Vector3[4];

        readonly List<GuestCardView> m_Cards = new List<GuestCardView>();
        int m_Capacity = -1;
        Vector2 m_LastAreaSize;
        Vector2 m_LastObstacle;
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
            if (Viewport == null || m_Capacity <= 0) return;
            if (Viewport.rect.size != m_LastAreaSize || (Obstacle() - m_LastObstacle).sqrMagnitude > 1f) Relayout(m_Capacity);
        }

        RectTransform Viewport => scroll != null && scroll.viewport != null ? scroll.viewport : area;

        /// <summary>The design size of one chat seat (round avatars, or the old 200x290 cards).</summary>
        Vector2 Design => cardPrefab != null ? cardPrefab.DesignSize : new Vector2(GuestCardView.DesignWidth, GuestCardView.DesignHeight);

        Vector2 CellAt(float scale) => Design * scale;

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

            // Optional (scenes from before V4.2 have it): the "Live Chat 3/5 Sub Cap - 7 in the queue" line.
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
            m_LastObstacle = Obstacle();
            if (m_LastAreaSize.x <= 1f || m_LastAreaSize.y <= 1f) return;

            float spacing = Mathf.Lerp(18f, 8f, Mathf.InverseLerp(5, GameRules.MaxCapacity, capacity));
            grid.spacing = new Vector2(spacing, spacing);
            var padding = grid.padding;
            float width = m_LastAreaSize.x - padding.horizontal;
            float height = m_LastAreaSize.y - padding.vertical;

            // Wider screens allow longer rows when the cards would otherwise get too small (16:9 -> 9, 21:9 -> 11...).
            float aspect = m_LastAreaSize.x / Mathf.Max(1f, m_LastAreaSize.y);
            const float referenceAspect = 1464f / 790f; // the chat area's shape on a 1920x1080 screen
            int maxColumns = Mathf.Clamp(Mathf.RoundToInt(BaseColumns * aspect / referenceAspect), BaseColumns, MaxColumns);

            // Rows of at most HouseColumns: the chat is a rectangle that gains a row each time one fills up.
            int columns = Mathf.Clamp(capacity, 1, BaseColumns);
            float scale = ScaleFor(capacity, columns, width, height, spacing);

            if (scale < MinCardScale)
            {
                // Too many rows to stay readable: longer rows (the biggest cards that fit), then scrolling.
                for (int c = columns + 1; c <= Mathf.Min(capacity, maxColumns); c++)
                {
                    float candidate = ScaleFor(capacity, c, width, height, spacing);
                    if (candidate > scale + 0.001f)
                    {
                        scale = candidate;
                        columns = c;
                    }
                }
            }

            float finalScale = Mathf.Min(scale, MaxCardScale);
            int finalColumns = columns;
            bool scrolling = finalScale < MinCardScale;
            if (scrolling)
            {
                finalScale = MinCardScale;
                float cellW = Design.x * finalScale;
                finalColumns = Mathf.Clamp(Mathf.FloorToInt((width + spacing) / (cellW + spacing)), 1, maxColumns);
            }

            // Keep the cards clear of the facecam: slide the rectangle left, or narrow it when there's no room to slide.
            float shift = 0f;
            if (!scrolling && m_LastObstacle.x > 0f && m_LastObstacle.y > 0f)
                shift = AvoidObstacle(capacity, finalColumns, ref finalScale, width, height, spacing);

            CardScale = finalScale;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = finalColumns;
            grid.cellSize = CellAt(finalScale);
            if (scroll != null) scroll.vertical = scrolling;
            var gridRect = (RectTransform)grid.transform;
            gridRect.anchoredPosition = new Vector2(shift, gridRect.anchoredPosition.y);

            foreach (var card in m_Cards) card.SetScale(finalScale);
        }

        float ScaleFor(int capacity, int columns, float width, float height, float spacing)
        {
            int rows = Mathf.CeilToInt(capacity / (float)columns);
            return Mathf.Min(CardScaleFor(width, columns, spacing, true), CardScaleFor(height, rows, spacing, false));
        }

        /// <summary>
        /// How far (in pixels, negative = left) the centred card rectangle must move so no card sits under the facecam. When
        /// sliding isn't enough, the cards shrink to fit the width left of it instead.
        /// </summary>
        float AvoidObstacle(int capacity, int columns, ref float scale, float width, float height, float spacing)
        {
            var cell = CellAt(scale);
            int rows = Mathf.CeilToInt(capacity / (float)columns);
            int lastRow = capacity - (rows - 1) * columns;
            float blockW = columns * cell.x + (columns - 1) * spacing;
            float blockH = rows * cell.y + (rows - 1) * spacing;
            float left = (width - blockW) * 0.5f;
            float bottom = (height - blockH) * 0.5f;

            // The facecam covers the bottom-right corner of the content: x past obstacleLeft, y below obstacleTop.
            float obstacleLeft = width - m_LastObstacle.x - AvoidGap;
            float obstacleTop = m_LastObstacle.y + AvoidGap;

            // The rightmost card edge among the rows reaching down into the facecam's height.
            float reach = float.MinValue;
            for (int row = 0; row < rows; row++)
            {
                float rowBottom = bottom + (rows - 1 - row) * (cell.y + spacing);
                if (rowBottom >= obstacleTop) continue;
                int count = row == rows - 1 ? lastRow : columns;
                reach = Mathf.Max(reach, left + count * cell.x + (count - 1) * spacing);
            }
            if (reach <= obstacleLeft) return 0f;

            float needed = reach - obstacleLeft;
            if (needed <= left) return -needed;

            // No room to slide: fit the rectangle to the width left of the facecam and centre it there.
            float room = Mathf.Max(1f, obstacleLeft);
            scale = Mathf.Max(MinCardScale, Mathf.Min(scale, CardScaleFor(room, columns, spacing, true)));
            float narrowW = columns * Design.x * scale + (columns - 1) * spacing;
            return Mathf.Min(0f, (room - narrowW) * 0.5f - (width - narrowW) * 0.5f);
        }

        /// <summary>How much of the chat's bottom-right corner <see cref="avoid"/> covers (width, height), or zero.</summary>
        Vector2 Obstacle()
        {
            var viewport = Viewport;
            if (avoid == null || viewport == null || !avoid.gameObject.activeInHierarchy) return Vector2.zero;
            avoid.GetWorldCorners(s_Corners);
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < 4; i++)
            {
                Vector2 local = viewport.InverseTransformPoint(s_Corners[i]);
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }
            var rect = viewport.rect;
            if (min.x >= rect.xMax || max.x <= rect.xMin || min.y >= rect.yMax || max.y <= rect.yMin) return Vector2.zero;
            return new Vector2(rect.xMax - Mathf.Max(rect.xMin, min.x), Mathf.Min(rect.yMax, max.y) - rect.yMin);
        }

        float CardScaleFor(float space, int count, float spacing, bool horizontal)
        {
            float cell = (space - spacing * (count - 1)) / count;
            return cell / (horizontal ? Design.x : Design.y);
        }
    }
}
