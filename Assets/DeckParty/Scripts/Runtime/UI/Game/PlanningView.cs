using System;
using System.Collections.Generic;
using DeckParty.Core;
using DeckParty.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>Multiplayer context shown on the planning screen.</summary>
    public struct PlanningContext
    {
        public bool Multiplayer;
        public bool SyncNights;
        public bool LocalReady;
        public string WaitingFor;

        /// <summary>Take Turns mode: players shop on their own turn, one after another.</summary>
        public bool Turns;
        public bool MyTurn;
        /// <summary>Whose turn it is (Take Turns), or null.</summary>
        public string ActiveName;
        /// <summary>Who plays after the current turn this night (Take Turns), or null when the night ends with it.</summary>
        public string NextName;
        /// <summary>Set while watching another player's planning: their name. Null on your own board.</summary>
        public string Owner;
        /// <summary>Whether there's a rival to hit with a rival power (multiplayer with another player still in the season).</summary>
        public bool RivalsAvailable;

        /// <summary>Free For All: every stream runs at once, then the shop opens for everyone together.</summary>
        public bool FreeForAll;
        /// <summary>Free For All: where the round is.</summary>
        public RoundStage Stage;
        /// <summary>Free For All: the shop hasn't opened yet (the gate covers it).</summary>
        public bool ShopGated;
        /// <summary>Free For All: you clicked Go to the Shop.</summary>
        public bool LocalReadyForShop;
        /// <summary>Free For All: rivals still streaming, and rivals done but not yet ready for the shop (comma lists).</summary>
        public string StillStreaming;
        public string NotReadyForShop;
    }

    /// <summary>
    /// Between streams. The left column holds the player's streamer card (with its active, when it has one), Raise Sub Cap,
    /// the Audience button and the next-night button; the rest of the screen is the shop: a grid of viewers to recruit.
    /// Everything shown belongs to the player whose board is on screen (yours, or the one you're watching); only the shop's
    /// stock is shared between players.
    /// </summary>
    public sealed class PlanningView : MonoBehaviour
    {
        [SerializeField] internal TMP_Text headerLabel;
        [SerializeField] internal Button expandButton;
        [SerializeField] internal TMP_Text expandLabel;
        [SerializeField] internal TooltipTrigger expandTooltip;
        [SerializeField] internal Button rolodexButton;
        [SerializeField] internal TMP_Text rolodexLabel;
        [SerializeField] internal Button nextNightButton;
        [SerializeField] internal TMP_Text nextNightLabel;
        [SerializeField] internal TMP_Text waitingLabel;
        [Tooltip("The scroll view's viewport (the grid fits as many columns as this width allows).")]
        [SerializeField] internal RectTransform shopViewport;
        [SerializeField] internal GridLayoutGroup shopGrid;
        [SerializeField] internal ShopItemView shopItemPrefab;
        [SerializeField] internal StreamerPowerView power;
        [Tooltip("Free For All: covers the shop until it opens for everyone (Go to the Shop, then the 3-2-1).")]
        [SerializeField] internal ShopGateView gate;

        [Header("Shop grid")]
        [Tooltip("Tiles never get narrower than this (more columns fit on wider screens).")]
        [SerializeField] internal float minTileWidth = 190f;
        [SerializeField] internal float maxTileWidth = 250f;
        [Tooltip("Tile height = width x this.")]
        [SerializeField] internal float tileAspect = 1.36f;

        public event Action<GuestType> BuyClicked;
        public event Action ExpandClicked;
        public event Action RolodexClicked;
        public event Action NextNightClicked;
        public event Action PowerClicked;
        /// <summary>Free For All: Go to the Shop (or take it back) on the gate.</summary>
        public event Action ShopReadyClicked;
        /// <summary>A shop tile was hovered (type) or left (null). Used for multiplayer presence signals.</summary>
        public event Action<GuestType?> ShopHovered;

        readonly List<ShopItemView> m_Items = new List<ShopItemView>();
        PlayerState m_State;
        PlanningContext m_Context;
        float m_LastWidth = -1f;

        public RectTransform ExpandButtonRect => (RectTransform)expandButton.transform;
        public RectTransform NextNightButtonRect => (RectTransform)nextNightButton.transform;
        public RectTransform RolodexButtonRect => (RectTransform)rolodexButton.transform;
        public RectTransform PowerButtonRect => power != null ? power.UseButtonRect : null;

        void Awake()
        {
            expandButton.onClick.AddListener(() => ExpandClicked?.Invoke());
            rolodexButton.onClick.AddListener(() => RolodexClicked?.Invoke());
            nextNightButton.onClick.AddListener(() => NextNightClicked?.Invoke());
            if (power != null) power.UseClicked += () => PowerClicked?.Invoke();
            if (gate != null) gate.ReadyClicked += () => ShopReadyClicked?.Invoke();
            Loc.Changed += OnLanguageChanged;
        }

        void OnDestroy() => Loc.Changed -= OnLanguageChanged;

        void OnLanguageChanged()
        {
            if (m_State != null) Render(m_State, m_Context);
        }

        void Update()
        {
            if (shopViewport != null && !Mathf.Approximately(shopViewport.rect.width, m_LastWidth)) LayoutGrid();
        }

        /// <summary>The tile selling this viewer (for the purchase flourish and presence highlights), or null.</summary>
        public ShopItemView FindItem(GuestType type)
        {
            foreach (var item in m_Items)
                if (item.gameObject.activeSelf && item.Type == type) return item;
            return null;
        }

        public void Render(PlayerState s, PlanningContext ctx)
        {
            m_State = s;
            m_Context = ctx;
            bool watching = ctx.Owner != null;
            bool waitingForTurn = ctx.Turns && !ctx.MyTurn && !watching;

            bool gated = ctx.ShopGated && !watching;
            if (headerLabel != null)
            {
                string header = watching ? Loc.Get("shop.header_other", "player", ctx.Owner) : Loc.Get("shop.header");
                // Free For All: the stock is shared and everyone shops at once.
                if (ctx.FreeForAll && !watching) header += $"  <size=60%><color=#FF9A3D>{Loc.Get("shop.first_come")}</color></size>";
                headerLabel.text = header;
            }

            bool readyLocked = ctx.Multiplayer && ctx.SyncNights && ctx.LocalReady;
            bool locked = readyLocked || watching || waitingForTurn || gated;
            string lockReason = watching ? Loc.Get("shop.lock_watching", "player", ctx.Owner)
                : waitingForTurn ? Loc.Get("shop.lock_turn")
                : gated ? Loc.Get("shop.lock_gate")
                : Loc.Get("shop.lock_ready");
            RenderGate(ctx, gated);

            var expandBlocker = PartyEngine.GetExpandBlocker(s);
            expandButton.interactable = expandBlocker == null && !locked;
            expandLabel.text = s.Capacity >= GameRules.MaxCapacity
                ? Loc.Get("shop.cap_maxed")
                : Loc.Get("shop.raise_cap", ("from", s.Capacity), ("to", s.Capacity + 1), ("cost", $"<color=#6BE38B>{GameIcons.Amount(ResourceIcon.Cash, PartyEngine.ExpansionCost(s))}</color>"));
            if (expandTooltip != null) expandTooltip.Text = watching || waitingForTurn ? lockReason
                : expandBlocker != null ? Loc.Error(expandBlocker) : Loc.Get("shop.raise_cap_tip");

            rolodexLabel.text = (watching
                ? Loc.Get("shop.view_audience_other", "player", ctx.Owner, "count", s.PermanentCount)
                : Loc.Get("shop.view_audience", "count", s.PermanentCount)) + "  <color=#B9AED6>[R]</color>";

            if (power != null) power.Render(s, ctx.RivalsAvailable, locked ? lockReason : null, ctx.Owner);

            RenderNextButton(s, ctx, watching);

            for (int i = 0; i < s.Shop.Count; i++)
            {
                if (i >= m_Items.Count)
                {
                    var item = Instantiate(shopItemPrefab, shopGrid.transform);
                    item.BuyClicked += type => BuyClicked?.Invoke(type);
                    item.Hovered += (view, on) => ShopHovered?.Invoke(on ? view.Type : (GuestType?)null);
                    m_Items.Add(item);
                }
                var tile = m_Items[i];
                tile.gameObject.SetActive(true);
                var type = s.Shop[i];
                var blocker = PartyEngine.GetBuyBlocker(s, type);
                tile.Bind(type, s, locked ? lockReason : blocker != null ? Loc.Error(blocker) : null);
            }
            for (int i = s.Shop.Count; i < m_Items.Count; i++) m_Items[i].gameObject.SetActive(false);
            LayoutGrid();
        }

        /// <summary>Free For All: the gate over the shop until it opens for everyone.</summary>
        void RenderGate(PlanningContext ctx, bool gated)
        {
            if (gate == null) return;
            if (!gated)
            {
                gate.Hide();
                return;
            }

            string notice = Loc.Get("gate.notice");
            if (ctx.Stage == RoundStage.Countdown)
            {
                gate.Show(Loc.Get("gate.countdown_title"), Loc.Get("gate.countdown_body"), notice, null);
                return;
            }

            string waiting = !string.IsNullOrEmpty(ctx.StillStreaming) ? Loc.Get("gate.still_streaming", "players", ctx.StillStreaming)
                : !string.IsNullOrEmpty(ctx.NotReadyForShop) ? Loc.Get("gate.not_ready_yet", "players", ctx.NotReadyForShop)
                : Loc.Get("gate.everyone_ready");
            if (ctx.LocalReadyForShop)
                gate.Show(Loc.Get("gate.waiting_title"), waiting, notice, Loc.Get("gate.cancel_ready"));
            else
                gate.Show(Loc.Get("gate.stream_over_title"), Loc.Get("gate.ready_body") + "\n" + waiting, notice, Loc.Get("gate.go_to_shop") + "  <size=70%>[Space]</size>");
        }

        void RenderNextButton(PlayerState s, PlanningContext ctx, bool watching)
        {
            bool nextInteractable = true;
            if (ctx.ShopGated && !watching)
            {
                // Free For All: the next night waits until everyone has shopped.
                nextInteractable = false;
                nextNightLabel.text = Loc.Get("shop.ready_for", "night", s.Night + 1);
                waitingLabel.text = Loc.Get("shop.gate_hint");
            }
            else if (watching)
            {
                nextInteractable = false;
                nextNightLabel.text = ctx.Turns ? Loc.Get("shop.other_shopping", "player", ctx.Owner) : Loc.Get("shop.watching", "player", ctx.Owner);
                waitingLabel.text = Loc.Get(ctx.Turns ? "shop.turn_ends_hint" : "shop.go_back_hint");
            }
            else if (ctx.Turns)
            {
                if (ctx.MyTurn)
                {
                    nextNightLabel.text = Loc.Get("shop.end_turn") + "  <size=70%>[Space]</size>";
                    waitingLabel.text = string.IsNullOrEmpty(ctx.NextName)
                        ? Loc.Get("shop.next_night_after", "night", s.Night + 1)
                        : Loc.Get("shop.next_player", "player", ctx.NextName);
                }
                else
                {
                    nextInteractable = false;
                    nextNightLabel.text = Loc.Get("shop.waiting_turn");
                    waitingLabel.text = string.IsNullOrEmpty(ctx.ActiveName) ? string.Empty : Loc.Get("shop.their_turn", "player", ctx.ActiveName);
                }
            }
            else if (ctx.Multiplayer && ctx.SyncNights)
            {
                nextNightLabel.text = ctx.LocalReady ? Loc.Get("shop.cancel_ready") : Loc.Get("shop.ready_for", "night", s.Night + 1);
                waitingLabel.text = ctx.LocalReady
                    ? (string.IsNullOrEmpty(ctx.WaitingFor) ? Loc.Get("shop.starting") : Loc.Get("shop.waiting_for", "players", ctx.WaitingFor))
                    : Loc.Get("shop.everyone_ready_hint");
            }
            else
            {
                nextNightLabel.text = Loc.Get("shop.start_night", "night", s.Night + 1) + "  <size=70%>[Space]</size>";
                waitingLabel.text = string.Empty;
            }
            nextNightButton.interactable = nextInteractable;
        }

        /// <summary>As many columns as fit, tiles between the minimum and maximum width.</summary>
        void LayoutGrid()
        {
            if (shopGrid == null || shopViewport == null) return;
            m_LastWidth = shopViewport.rect.width;
            float width = m_LastWidth - shopGrid.padding.horizontal;
            if (width <= 1f) return;
            float spacing = shopGrid.spacing.x;
            int columns = Mathf.Max(1, Mathf.FloorToInt((width + spacing) / (minTileWidth + spacing)));
            float tileWidth = Mathf.Min(maxTileWidth, (width - spacing * (columns - 1)) / columns);
            shopGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            shopGrid.constraintCount = columns;
            shopGrid.cellSize = new Vector2(tileWidth, tileWidth * tileAspect);
        }
    }
}
