using System;
using System.Collections.Generic;
using DeckParty.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// The live chat: one seat per Sub Cap space (plus the Hype Men's spare seats), at most <see cref="Columns"/> (10) to a
    /// row and <see cref="MaxRows"/> (4) rows, so a player's chat holds up to 40 Sub Cap seats (2026-11-18). The chat never scrolls:
    /// the seats resize to fit, in the row length that gives the biggest seats. Teams split the chat in two, with up to 50
    /// seats (5 rows of 10, <see cref="TeamMaxRows"/>) on each side. The Hype Men's spare seats past a full (or nearly full)
    /// chat go in one extra row under the full ones. The cards keep clear of <see cref="avoid"/> (when set).
    /// </summary>
    public sealed class HouseView : MonoBehaviour
    {
        /// <summary>
        /// The smallest seat size the effects are tuned for (emoji sizes; a fraction of the design size). Seats can get smaller
        /// than this in a very full chat, since the chat no longer scrolls.
        /// </summary>
        public const float MinCardScale = 0.62f;
        /// <summary>Cards never get bigger than this.</summary>
        public const float MaxCardScale = 1.15f;
        /// <summary>Most seats in one row (<see cref="GameRules.HouseColumns"/>).</summary>
        public const int Columns = GameRules.HouseColumns;
        /// <summary>Most rows in one player's chat (<see cref="GameRules.HouseMaxRows"/>). Only the viewer who crashes a full chat's server shows in a row past it, for a moment.</summary>
        public const int MaxRows = GameRules.HouseMaxRows;
        /// <summary>Teams: most rows on each player's side (<see cref="GameRules.TeamHouseMaxRows"/>, 5 since the 2026-11-18 UI updates: 50 seats a player).</summary>
        public const int TeamMaxRows = GameRules.TeamHouseMaxRows;
        /// <summary>A layout with more rows is only picked when it makes the seats at least this much bigger (one tidy row beats two ragged ones).</summary>
        const float MoreRowsGain = 1.08f;

        [SerializeField] internal RectTransform area;
        [SerializeField] internal GridLayoutGroup grid;
        [SerializeField] internal GuestCardView cardPrefab;
        [SerializeField] internal TMP_Text headerLabel;
        [SerializeField] internal TMP_Text hintLabel;
        [Tooltip("Optional: the chat's ScrollRect (scenes built before 2026-11-18). The chat no longer scrolls; it stays switched off.")]
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
                if (!s.IsEndless && PartyEngine.DiamondsPresent(s) >= PartyEngine.TrophiesToWin(s))
                    hintLabel.text = $"<color=#FFC94A><b>{Loc.Get("chat.hint.win", "count", PartyEngine.TrophiesToWin(s))}</b></color>";
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
        /// like the solo layout: up to 10 to a row and 5 rows a side (50 seats per player), plus a row for the Hype Men's spare seats, resized to fit, never scrolling.
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
                {
                    // The spare seats never count against the side's 50 Sub Cap seats: past them they take the extra row.
                    int free = Math.Min(Math.Max(0, spare - seated),
                        Math.Max(0, GameRules.TeamMaxCapacity + GameRules.HypeRowSeats - s.CapacityOf(side) - PartyEngine.HypeSeated(s, side)));
                    for (int k = free; k > 0; k--) cells.Add(new Cell { Kind = CellKind.Spare });
                }
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
        /// number of columns, picked like the solo chat (<see cref="BestColumns"/>): at most 10 to a row and 5 rows a side (one more for spare seats), the
        /// biggest seats that fit both the width and the height. The chat never scrolls.
        /// </summary>
        void LayoutTeam()
        {
            m_LastAreaSize = Viewport.rect.size;
            if (m_LastAreaSize.x <= 1f || m_LastAreaSize.y <= 1f) return;

            int widest = Mathf.Max(1, Mathf.Max(m_SideCounts[0], m_SideCounts[1]));
            int total = m_SideCounts[0] + m_SideCounts[1];
            float spacing = Spacing(total, 2 * GameRules.TeamMaxCapacity);
            var padding = grid.padding;
            int top = m_SoloTopPadding + TeamLabelBand;
            float middle = DividerWidth + 2f * DividerGap;
            float sideWidth = Mathf.Max(1f, (m_LastAreaSize.x - padding.horizontal - middle) * 0.5f);
            float height = Mathf.Max(1f, m_LastAreaSize.y - top - padding.bottom);

            int columns = BestColumns(widest, sideWidth, height, spacing, TeamMaxRows, out float finalScale);
            int rows = Mathf.Max(1, Mathf.CeilToInt(widest / (float)columns));
            var cell = CellAt(finalScale);
            CardScale = finalScale;
            grid.spacing = new Vector2(spacing, spacing);
            grid.cellSize = cell;
            StopScrolling();

            // The content: the name band, the rows, the bottom padding. Centred in the chat like the solo grid.
            float rowsHeight = rows * cell.y + (rows - 1) * spacing;
            var gridRect = (RectTransform)grid.transform;
            gridRect.sizeDelta = new Vector2(0f, top + rowsHeight + padding.bottom);
            gridRect.anchoredPosition = Vector2.zero;

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

        /// <summary>
        /// The row length for <paramref name="seats"/> seats in a width x height space, and the seat scale it gives: at most
        /// <see cref="Columns"/> (10) to a row and <paramref name="maxRows"/> rows (4 solo, 5 a side in a team; a chat over its
        /// limit for a moment gets the rows it needs), the biggest seats that fit. More rows only win when they make the seats clearly bigger
        /// (<see cref="MoreRowsGain"/>), so a short chat stays on one row.
        /// </summary>
        int BestColumns(int seats, float width, float height, float spacing, int maxRows, out float scale)
        {
            seats = Mathf.Max(1, seats);
            if (seats > maxRows * Columns)
            {
                // Past the full rows (the Hype Men's spare seats on a chat at or near its limit, or the viewer crashing a full
                // chat's server): every row stays 10 long, so the extra seats read as their own row under the full ones.
                scale = Mathf.Max(0.05f, Mathf.Min(ScaleFor(seats, Columns, width, height, spacing), MaxCardScale));
                return Columns;
            }
            int most = Mathf.Min(seats, Columns);
            int rowsAllowed = Mathf.Max(maxRows, Mathf.CeilToInt(seats / (float)Columns));
            int fewest = Mathf.Clamp(Mathf.CeilToInt(seats / (float)rowsAllowed), 1, most);

            int best = most;
            scale = Mathf.Min(ScaleFor(seats, most, width, height, spacing), MaxCardScale);
            for (int columns = most - 1; columns >= fewest; columns--)
            {
                float candidate = Mathf.Min(ScaleFor(seats, columns, width, height, spacing), MaxCardScale);
                if (candidate <= scale * MoreRowsGain) continue;
                scale = candidate;
                best = columns;
            }
            // Never zero or negative (a tiny chat area while the screen lays out).
            scale = Mathf.Max(0.05f, scale);
            return best;
        }

        /// <summary>The gap between seats: roomy in a small chat, tighter as it fills (<paramref name="most"/> seats = the tightest).</summary>
        static float Spacing(int seats, int most) => Mathf.Lerp(18f, 8f, Mathf.InverseLerp(5, most, seats));

        /// <summary>The chat never scrolls (2026-11-18): the ScrollRect older scenes have stays off, with its content at rest.</summary>
        void StopScrolling()
        {
            if (scroll == null) return;
            scroll.vertical = false;
            scroll.horizontal = false;
            scroll.velocity = Vector2.zero;
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

            float spacing = Spacing(capacity, GameRules.MaxCapacity);
            grid.spacing = new Vector2(spacing, spacing);
            var padding = grid.padding;
            float width = m_LastAreaSize.x - padding.horizontal;
            float height = m_LastAreaSize.y - padding.vertical;

            // At most 10 to a row and 4 rows (40 seats) plus the spare seats' row, resized to fit: the chat never scrolls.
            int columns = BestColumns(capacity, width, height, spacing, MaxRows, out float finalScale);

            // Keep the cards clear of the facecam: slide the rectangle left, or narrow it when there's no room to slide.
            float shift = 0f;
            if (m_LastObstacle.x > 0f && m_LastObstacle.y > 0f)
                shift = AvoidObstacle(capacity, columns, ref finalScale, width, height, spacing);

            CardScale = finalScale;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.cellSize = CellAt(finalScale);
            StopScrolling();
            var gridRect = (RectTransform)grid.transform;
            gridRect.anchoredPosition = new Vector2(shift, 0f);

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
            scale = Mathf.Max(0.05f, Mathf.Min(scale, CardScaleFor(room, columns, spacing, true)));
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
