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

        [Header("Teams (2v2, co-op)")]
        [Tooltip("The line between the two players' sides of the chat (UI Skin: Team Divider). Cloned for every row. Optional: " +
                 "without it the sides are split by a plain grey line.")]
        [SerializeField] internal Image dividerTemplate;
        [Tooltip("Optional: the left player's name above their side of the chat.")]
        [SerializeField] internal TMP_Text leftSideLabel;
        [Tooltip("Optional: the right player's name above their side of the chat.")]
        [SerializeField] internal TMP_Text rightSideLabel;

        /// <summary>Teams: the names over the two sides (member 0 on the left, member 1 on the right), set by the game screen.</summary>
        public string LeftSideName { get; set; }
        public string RightSideName { get; set; }

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
            if (Viewport == null) return;
            if (m_TeamColumns > 0)
            {
                if (Viewport.rect.size != m_LastAreaSize) RelayoutTeam(m_TeamColumns, m_TeamRows);
                return;
            }
            if (m_Capacity <= 0) return;
            if (Viewport.rect.size != m_LastAreaSize || (Obstacle() - m_LastObstacle).sqrMagnitude > 1f) Relayout(m_Capacity);
        }

        RectTransform Viewport => scroll != null && scroll.viewport != null ? scroll.viewport : area;

        /// <summary>The design size of one chat seat (round avatars, or the old 200x290 cards).</summary>
        Vector2 Design => cardPrefab != null ? cardPrefab.DesignSize : new Vector2(GuestCardView.DesignWidth, GuestCardView.DesignHeight);

        Vector2 CellAt(float scale) => Design * scale;

        public void Render(PlayerState s)
        {
            m_State = s;
            if (s.IsTeam)
            {
                RenderTeam(s);
                return;
            }
            LeaveTeamLayout();
            // Hype Men's spare seats come after the Sub Cap's seats: grey dotted circles only a viewer a Hype Man brings in
            // can take (a viewer sitting in one gets the dotted ring too).
            int spare = PartyEngine.HypeSeats(s);
            int seated = PartyEngine.HypeSeated(s);
            int seats = Math.Max(s.Capacity + Math.Max(spare, seated), s.Occupancy);
            EnsureCards(seats);
            if (seats != m_Capacity || Viewport.rect.size != m_LastAreaSize) Relayout(seats);

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
            int emptyNormal = Math.Max(0, s.Capacity - (s.Occupancy - seated));
            for (int k = 0; k < emptyNormal && slot < seats; k++, slot++) Show(slot).ShowEmpty();
            for (int k = Math.Max(0, spare - seated); k > 0 && slot < seats; k--, slot++) Show(slot).ShowSpareSeat();
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

        // ================================================================== teams (2v2, co-op)

        /// <summary>What one cell of a team's chat shows.</summary>
        enum CellKind { Guest, Filler, Empty, Spare }

        struct Cell
        {
            public CellKind Kind;
            public GuestInstance Guest;
        }

        readonly List<RectTransform> m_Spacers = new List<RectTransform>();
        readonly List<Cell>[] m_Sides = { new List<Cell>(), new List<Cell>() };
        /// <summary>Teams: columns per side and rows of the split layout (0 when the solo layout is on).</summary>
        int m_TeamColumns;
        int m_TeamRows;
        /// <summary>The grid's own top padding, before the team layout made room for the side names.</summary>
        int m_SoloTopPadding = -1;

        /// <summary>Height of the band above a team's chat for the two side names.</summary>
        const int TeamLabelBand = 40;
        const float DividerWidth = 4f;

        /// <summary>
        /// Teams: the chat split down the middle. Member 0's viewers and seats on the left, member 1's on the right, with a
        /// line between them (a column of its own in the grid). Each side lists its viewers in joining order, then its empty
        /// seats; the Hype Men's free spare seats go on the side of the first Hype Man in chat.
        /// </summary>
        void RenderTeam(PlayerState s)
        {
            int spare = PartyEngine.HypeSeats(s);
            int seated = PartyEngine.HypeSeated(s);
            int spareSide = 0;
            for (int i = 0; i < s.House.Count; i++)
            {
                var g = s.Find(s.House[i]);
                if (g != null && PartyEngine.IsHypeMan(g)) { spareSide = g.Owner; break; }
            }

            for (int side = 0; side < 2; side++)
            {
                var cells = m_Sides[side];
                cells.Clear();
                for (int i = 0; i < s.House.Count; i++)
                {
                    var g = s.Find(s.House[i]);
                    if (g == null || g.Owner != side) continue;
                    cells.Add(new Cell { Kind = CellKind.Guest, Guest = g });
                    for (int extra = 1; extra < g.Info.Size; extra++) cells.Add(new Cell { Kind = CellKind.Filler, Guest = g });
                }
                int empty = Math.Max(0, s.CapacityOf(side) - (s.OccupancyOf(side) - PartyEngine.HypeSeated(s, side)));
                for (int k = 0; k < empty; k++) cells.Add(new Cell { Kind = CellKind.Empty });
                if (side == spareSide)
                    for (int k = Math.Max(0, spare - seated); k > 0; k--) cells.Add(new Cell { Kind = CellKind.Spare });
            }

            int widest = Math.Max(m_Sides[0].Count, m_Sides[1].Count);
            int perSide = Mathf.Clamp(widest, 1, Mathf.Max(1, BaseColumns / 2));
            int rows = Mathf.Max(1, Mathf.CeilToInt(widest / (float)perSide));

            int cards = m_Sides[0].Count + m_Sides[1].Count;
            EnsureCards(cards);
            int spacersNeeded = rows * (perSide * 2 + 1) - cards;
            EnsureSpacers(spacersNeeded);

            // Lay the cells out row by row: the left side's cells, the divider, the right side's cells.
            int cardIndex = 0, spacerIndex = 0, sibling = 0;
            for (int row = 0; row < rows; row++)
            {
                for (int side = 0; side < 2; side++)
                {
                    if (side == 1) PlaceSpacer(spacerIndex++, sibling++, true);
                    var cells = m_Sides[side];
                    for (int column = 0; column < perSide; column++)
                    {
                        int index = row * perSide + column;
                        if (index >= cells.Count)
                        {
                            PlaceSpacer(spacerIndex++, sibling++, false);
                            continue;
                        }
                        var card = Show(cardIndex++);
                        card.transform.SetSiblingIndex(sibling++);
                        var cell = cells[index];
                        switch (cell.Kind)
                        {
                            case CellKind.Guest: card.ShowGuest(cell.Guest, s, true); break;
                            case CellKind.Filler: card.ShowFiller(cell.Guest.Kind); break;
                            case CellKind.Spare: card.ShowSpareSeat(); break;
                            default: card.ShowEmpty(); break;
                        }
                    }
                }
            }
            for (int i = cardIndex; i < m_Cards.Count; i++) m_Cards[i].gameObject.SetActive(false);
            for (int i = spacerIndex; i < m_Spacers.Count; i++) m_Spacers[i].gameObject.SetActive(false);

            EnterTeamLayout();
            if (perSide != m_TeamColumns || rows != m_TeamRows || Viewport.rect.size != m_LastAreaSize) RelayoutTeam(perSide, rows);
            if (headerLabel != null)
                headerLabel.text = Loc.Get("chat.header", ("filled", s.Occupancy), ("cap", s.TotalCapacity), ("queue", s.Pool.Count));
            if (hintLabel != null) hintLabel.text = string.Empty;
        }

        void EnsureSpacers(int count)
        {
            while (m_Spacers.Count < count)
            {
                var spacer = new GameObject("Spacer " + (m_Spacers.Count + 1), typeof(RectTransform)).GetComponent<RectTransform>();
                spacer.SetParent(grid.transform, false);
                // The divider line, drawn only on the spacers in the middle column.
                Image line;
                if (dividerTemplate != null)
                {
                    line = Instantiate(dividerTemplate, spacer);
                    line.gameObject.SetActive(true);
                }
                else
                {
                    line = new GameObject("Line", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                    line.transform.SetParent(spacer, false);
                    line.color = new Color(0.6f, 0.6f, 0.65f, 1f);
                }
                line.name = "Line";
                line.raycastTarget = false;
                m_Spacers.Add(spacer);
            }
        }

        void PlaceSpacer(int index, int sibling, bool divider)
        {
            var spacer = m_Spacers[index];
            spacer.gameObject.SetActive(true);
            spacer.SetSiblingIndex(sibling);
            var line = spacer.childCount > 0 ? (RectTransform)spacer.GetChild(0) : null;
            if (line == null) return;
            line.gameObject.SetActive(divider);
            if (!divider) return;
            // A thin line down the middle of the cell, reaching into the gaps above and below so the rows join up.
            float gap = grid.spacing.y * 0.5f;
            line.anchorMin = new Vector2(0.5f, 0f);
            line.anchorMax = new Vector2(0.5f, 1f);
            line.pivot = new Vector2(0.5f, 0.5f);
            line.offsetMin = new Vector2(-DividerWidth * 0.5f, -gap);
            line.offsetMax = new Vector2(DividerWidth * 0.5f, gap);
        }

        /// <summary>Teams: room above the grid for the side names, which show the two players.</summary>
        void EnterTeamLayout()
        {
            if (m_SoloTopPadding < 0) m_SoloTopPadding = grid.padding.top;
            if (grid.padding.top != m_SoloTopPadding + TeamLabelBand)
                grid.padding = new RectOffset(grid.padding.left, grid.padding.right, m_SoloTopPadding + TeamLabelBand, grid.padding.bottom);
            SetSideLabel(leftSideLabel, LeftSideName);
            SetSideLabel(rightSideLabel, RightSideName);
        }

        /// <summary>Back to the solo layout (no spacers, no side names, the grid's own padding).</summary>
        void LeaveTeamLayout()
        {
            if (m_TeamColumns == 0 && m_Spacers.Count == 0) return;
            m_TeamColumns = 0;
            m_TeamRows = 0;
            m_Capacity = -1;
            foreach (var spacer in m_Spacers) spacer.gameObject.SetActive(false);
            // The team layout reordered the cards: back in seat order.
            for (int i = 0; i < m_Cards.Count; i++) m_Cards[i].transform.SetSiblingIndex(i);
            if (m_SoloTopPadding >= 0 && grid.padding.top != m_SoloTopPadding)
                grid.padding = new RectOffset(grid.padding.left, grid.padding.right, m_SoloTopPadding, grid.padding.bottom);
            SetSideLabel(leftSideLabel, null);
            SetSideLabel(rightSideLabel, null);
        }

        static void SetSideLabel(TMP_Text label, string text)
        {
            if (label == null) return;
            label.gameObject.SetActive(!string.IsNullOrEmpty(text));
            label.text = text ?? string.Empty;
        }

        /// <summary>Teams: sizes the split grid (both sides and the divider column) to fit the chat, scrolling past the minimum size.</summary>
        void RelayoutTeam(int perSide, int rows)
        {
            m_TeamColumns = perSide;
            m_TeamRows = rows;
            m_LastAreaSize = Viewport.rect.size;
            if (m_LastAreaSize.x <= 1f || m_LastAreaSize.y <= 1f) return;

            int columns = perSide * 2 + 1;
            float spacing = Mathf.Lerp(18f, 8f, Mathf.InverseLerp(5, GameRules.MaxCapacity, perSide * rows * 2));
            grid.spacing = new Vector2(spacing, spacing);
            var padding = grid.padding;
            float width = m_LastAreaSize.x - padding.horizontal;
            float height = m_LastAreaSize.y - padding.vertical;
            float scale = Mathf.Min(CardScaleFor(width, columns, spacing, true), CardScaleFor(height, rows, spacing, false));
            bool scrolling = scale < MinCardScale;
            float finalScale = Mathf.Clamp(scale, MinCardScale, MaxCardScale);

            CardScale = finalScale;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.cellSize = CellAt(finalScale);
            if (scroll != null) scroll.vertical = scrolling;
            var gridRect = (RectTransform)grid.transform;
            gridRect.anchoredPosition = new Vector2(0f, gridRect.anchoredPosition.y);
            foreach (var card in m_Cards) card.SetScale(finalScale);
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
