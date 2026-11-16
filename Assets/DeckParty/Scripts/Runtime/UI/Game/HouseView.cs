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
        [Tooltip("The line between the two players' sides of the chat (UI Skin: Team Divider). One copy runs down the middle, as tall as the rows. Optional: " +
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
            if (m_TeamLayout)
            {
                if (Viewport.rect.size != m_LastAreaSize) LayoutTeam();
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
                if (!s.IsEndless && PartyEngine.DiamondsPresent(s) >= GameRules.DiamondsToWin)
                    hintLabel.text = $"<color=#FFC94A><b>{Loc.Get("chat.hint.win", "count", GameRules.DiamondsToWin)}</b></color>";
                else if (trouble >= PartyEngine.RiskLimit(s) - 1)
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

        readonly List<Cell>[] m_Sides = { new List<Cell>(), new List<Cell>() };
        /// <summary>Teams: how many cards each side shows (left, right), in card order: the left side's cards come first.</summary>
        readonly int[] m_SideCounts = new int[2];
        /// <summary>Teams: the split layout is on (the grid component is paused and the cards are placed by hand).</summary>
        bool m_TeamLayout;
        /// <summary>The grid's own top padding, before the team layout made room for the side names.</summary>
        int m_SoloTopPadding = -1;
        /// <summary>Teams: the single line between the two sides (made from <see cref="dividerTemplate"/>).</summary>
        RectTransform m_Divider;
        ContentSizeFitter m_GridFitter;

        /// <summary>Height of the band above a team's chat for the two side names.</summary>
        const int TeamLabelBand = 40;
        const float DividerWidth = 4f;
        /// <summary>Clear space on each side of the divider (on top of the usual gap between cards).</summary>
        const float DividerGap = 14f;

        /// <summary>
        /// Teams: the chat split down the middle. Member 0's viewers and seats on the left, member 1's on the right, with one
        /// line between them. Each side lists its viewers in joining order, then its empty seats; the Hype Men's free spare
        /// seats go on the side of the first Hype Man in chat. Both sides always use the same seat size and fit inside the chat
        /// like the solo layout: seats shrink and wrap into more rows, and only past the minimum size does the chat scroll.
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

            EnterTeamLayout();
            int cards = m_Sides[0].Count + m_Sides[1].Count;
            EnsureCards(cards);
            int cardIndex = 0;
            for (int side = 0; side < 2; side++)
            {
                var cells = m_Sides[side];
                m_SideCounts[side] = cells.Count;
                for (int index = 0; index < cells.Count; index++)
                {
                    var card = Show(cardIndex);
                    card.transform.SetSiblingIndex(cardIndex++);
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
            for (int i = cardIndex; i < m_Cards.Count; i++) m_Cards[i].gameObject.SetActive(false);

            // Every render: a seat may have been added without the size of the chat changing.
            LayoutTeam();
            if (headerLabel != null)
                headerLabel.text = Loc.Get("chat.header", ("filled", s.Occupancy), ("cap", s.TotalCapacity), ("queue", s.Pool.Count));
            if (hintLabel != null) hintLabel.text = string.Empty;
        }

        /// <summary>
        /// Teams: pauses the grid component (the two sides are placed by hand) and makes room above the seats for the side
        /// names, which show the two players.
        /// </summary>
        void EnterTeamLayout()
        {
            if (!m_TeamLayout)
            {
                m_TeamLayout = true;
                if (m_SoloTopPadding < 0) m_SoloTopPadding = grid.padding.top;
                grid.enabled = false;
                if (m_GridFitter == null) m_GridFitter = grid.GetComponent<ContentSizeFitter>();
                if (m_GridFitter != null) m_GridFitter.enabled = false;
                m_LastAreaSize = Vector2.zero;
            }
            EnsureDivider();
            SetSideLabel(leftSideLabel, LeftSideName);
            SetSideLabel(rightSideLabel, RightSideName);
        }

        /// <summary>Back to the solo layout: the grid places the cards again, in seat order, with no divider or side names.</summary>
        void LeaveTeamLayout()
        {
            if (!m_TeamLayout) return;
            m_TeamLayout = false;
            m_Capacity = -1;
            if (m_Divider != null) m_Divider.gameObject.SetActive(false);
            // The team layout reordered the cards: back in seat order.
            for (int i = 0; i < m_Cards.Count; i++) m_Cards[i].transform.SetSiblingIndex(i);
            grid.enabled = true;
            if (m_GridFitter != null) m_GridFitter.enabled = true;
            SetSideLabel(leftSideLabel, null);
            SetSideLabel(rightSideLabel, null);
            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)grid.transform);
        }

        /// <summary>The one line between the two sides (the UI Skin's Team Divider, or a plain grey line in older scenes).</summary>
        void EnsureDivider()
        {
            if (m_Divider != null) return;
            Image line;
            if (dividerTemplate != null) line = Instantiate(dividerTemplate, grid.transform);
            else
            {
                line = new GameObject("TeamDivider", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                line.transform.SetParent(grid.transform, false);
                line.color = new Color(0.6f, 0.6f, 0.65f, 1f);
            }
            line.name = "TeamDivider";
            line.raycastTarget = false;
            // Never a cell of the solo grid.
            var element = line.GetComponent<LayoutElement>();
            if (element == null) element = line.gameObject.AddComponent<LayoutElement>();
            element.ignoreLayout = true;
            m_Divider = line.rectTransform;
            m_Divider.anchorMin = m_Divider.anchorMax = new Vector2(0.5f, 1f);
            m_Divider.pivot = new Vector2(0.5f, 1f);
        }

        static void SetSideLabel(TMP_Text label, string text)
        {
            if (label == null) return;
            label.gameObject.SetActive(!string.IsNullOrEmpty(text));
            label.text = text ?? string.Empty;
        }

        /// <summary>
        /// Teams: sizes and places both sides. Each side gets half the chat's width (less the divider's gap) and the same
        /// number of columns, picked like the solo chat: rows of at most half the solo row, more columns only when the seats
        /// would otherwise get too small, and the biggest seats that fit both the width and the height. Below the minimum
        /// seat size the columns are cut to what fits the width at that size and the chat scrolls down instead, so nothing
        /// ever hangs over the sides.
        /// </summary>
        void LayoutTeam()
        {
            m_LastAreaSize = Viewport.rect.size;
            if (m_LastAreaSize.x <= 1f || m_LastAreaSize.y <= 1f) return;

            int widest = Mathf.Max(1, Mathf.Max(m_SideCounts[0], m_SideCounts[1]));
            int total = m_SideCounts[0] + m_SideCounts[1];
            float spacing = Mathf.Lerp(18f, 8f, Mathf.InverseLerp(5, GameRules.MaxCapacity, total));
            var padding = grid.padding;
            int top = m_SoloTopPadding + TeamLabelBand;
            float middle = DividerWidth + 2f * DividerGap;
            float sideWidth = Mathf.Max(1f, (m_LastAreaSize.x - padding.horizontal - middle) * 0.5f);
            float height = Mathf.Max(1f, m_LastAreaSize.y - top - padding.bottom);

            // Wider chats allow longer rows when the seats would otherwise get too small (as in the solo chat).
            int baseColumns = Mathf.Max(1, BaseColumns / 2);
            int maxColumns = Mathf.Max(baseColumns, MaxColumns / 2);
            int columns = Mathf.Clamp(widest, 1, baseColumns);
            float scale = TeamScale(widest, columns, sideWidth, height, spacing);
            if (scale < MinCardScale)
            {
                for (int c = columns + 1; c <= Mathf.Min(widest, maxColumns); c++)
                {
                    float candidate = TeamScale(widest, c, sideWidth, height, spacing);
                    if (candidate > scale + 0.001f)
                    {
                        scale = candidate;
                        columns = c;
                    }
                }
            }

            float finalScale = Mathf.Min(scale, MaxCardScale);
            bool scrolling = false;
            if (finalScale < MinCardScale)
            {
                // Too many seats to stay readable: the minimum size, as many columns as fit the width, then scrolling. A side
                // too narrow for even one seat at the minimum gets smaller seats instead of spilling over the edge.
                finalScale = Mathf.Min(MinCardScale, CardScaleFor(sideWidth, 1, spacing, true));
                float cellWidth = Design.x * finalScale;
                columns = Mathf.Clamp(Mathf.FloorToInt((sideWidth + spacing) / (cellWidth + spacing)), 1, Mathf.Max(1, widest));
                int neededRows = Mathf.CeilToInt(widest / (float)columns);
                scrolling = neededRows * Design.y * finalScale + (neededRows - 1) * spacing > height + 0.5f;
            }

            int rows = Mathf.Max(1, Mathf.CeilToInt(widest / (float)columns));
            var cell = CellAt(finalScale);
            CardScale = finalScale;
            grid.spacing = new Vector2(spacing, spacing);
            grid.cellSize = cell;
            if (scroll != null) scroll.vertical = scrolling;

            // The content: the name band, the rows, the bottom padding. Centred in the chat like the solo grid.
            float rowsHeight = rows * cell.y + (rows - 1) * spacing;
            var gridRect = (RectTransform)grid.transform;
            gridRect.sizeDelta = new Vector2(0f, top + rowsHeight + padding.bottom);
            gridRect.anchoredPosition = new Vector2(0f, scrolling ? gridRect.anchoredPosition.y : 0f);

            // Each side's block hugs the divider; its rows fill outwards from there, left to right as usual.
            float blockWidth = columns * cell.x + (columns - 1) * spacing;
            float half = middle * 0.5f;
            int cardIndex = 0;
            for (int side = 0; side < 2; side++)
            {
                float blockLeft = side == 0 ? -half - blockWidth : half;
                for (int index = 0; index < m_SideCounts[side]; index++, cardIndex++)
                {
                    if (cardIndex >= m_Cards.Count) break;
                    int row = index / columns;
                    int column = index % columns;
                    var rect = (RectTransform)m_Cards[cardIndex].transform;
                    rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.sizeDelta = cell;
                    rect.anchoredPosition = new Vector2(blockLeft + column * (cell.x + spacing) + cell.x * 0.5f,
                        -(top + row * (cell.y + spacing) + cell.y * 0.5f));
                }
            }
            foreach (var card in m_Cards) card.SetScale(finalScale);

            if (m_Divider != null)
            {
                m_Divider.gameObject.SetActive(true);
                m_Divider.SetAsLastSibling();
                m_Divider.sizeDelta = new Vector2(DividerWidth, rowsHeight);
                m_Divider.anchoredPosition = new Vector2(0f, -top);
            }
        }

        /// <summary>Teams: the seat scale that fits <paramref name="seats"/> seats per side in rows of <paramref name="columns"/>.</summary>
        float TeamScale(int seats, int columns, float sideWidth, float height, float spacing)
        {
            int rows = Mathf.CeilToInt(seats / (float)columns);
            return Mathf.Min(CardScaleFor(sideWidth, columns, spacing, true), CardScaleFor(height, rows, spacing, false));
        }

        void EnsureCards(int capacity)
        {
            while (m_Cards.Count < capacity)
            {
                var card = Instantiate(cardPrefab, grid.transform);
                card.name = "Slot " + (m_Cards.Count + 1);
                // A seat added after the last layout pass (a Hype Man's guest, a big viewer's extra seat, a teammate's new
                // seat) must show at the chat's current size, not the prefab's full size.
                card.SetScale(CardScale);
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
