using System;
using System.Collections.Generic;
using DeckParty.Core;
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
    }

    /// <summary>Between streams: last stream result, the viewer shop, the streamer power, Sub Cap upgrade and the next-night button.</summary>
    public sealed class PlanningView : MonoBehaviour
    {
        [SerializeField] internal Image resultAccent;
        [SerializeField] internal TMP_Text resultTitle;
        [SerializeField] internal TMP_Text resultDetail;
        [SerializeField] internal TMP_Text breakdownLabel;
        [SerializeField] internal TMP_Text nightLabel;
        [SerializeField] internal Button expandButton;
        [SerializeField] internal TMP_Text expandLabel;
        [SerializeField] internal TooltipTrigger expandTooltip;
        [SerializeField] internal Button rolodexButton;
        [SerializeField] internal TMP_Text rolodexLabel;
        [SerializeField] internal Button nextNightButton;
        [SerializeField] internal TMP_Text nextNightLabel;
        [SerializeField] internal TMP_Text waitingLabel;
        [SerializeField] internal TMP_Text walletLabel;
        [SerializeField] internal RectTransform shopList;
        [SerializeField] internal GuestRowView shopRowPrefab;
        [SerializeField] internal StreamerPowerView power;

        public event Action<GuestType> BuyClicked;
        public event Action ExpandClicked;
        public event Action RolodexClicked;
        public event Action NextNightClicked;
        public event Action PowerClicked;
        /// <summary>A shop row was hovered (type) or left (null). Used for multiplayer presence signals.</summary>
        public event Action<GuestType?> ShopHovered;

        readonly List<GuestRowView> m_Rows = new List<GuestRowView>();
        PlayerState m_State;
        PlanningContext m_Context;
        string m_Breakdown;

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
            Loc.Changed += OnLanguageChanged;
        }

        void OnDestroy() => Loc.Changed -= OnLanguageChanged;

        void OnLanguageChanged()
        {
            if (m_State != null) Render(m_State, m_Context, m_Breakdown);
        }

        public GuestRowView FindRow(GuestType type)
        {
            foreach (var row in m_Rows)
                if (row.gameObject.activeSelf && row.Type == type) return row;
            return null;
        }

        public void Render(PlayerState s, PlanningContext ctx, string breakdown)
        {
            m_State = s;
            m_Context = ctx;
            m_Breakdown = breakdown;
            bool watching = ctx.Owner != null;
            bool waitingForTurn = ctx.Turns && !ctx.MyTurn && !watching;
            RenderResult(s, breakdown, ctx.Owner);

            string nightsLeft = $"<color=#B9AED6>{Loc.Get("shop.nights_left", "count", s.NightsRemaining)}</color>";
            nightLabel.text = (watching
                ? Loc.Get("shop.night_over_other", ("player", ctx.Owner), ("night", s.Night), ("total", s.TotalNights))
                : Loc.Get("shop.night_over", "night", s.Night, "total", s.TotalNights)) + "  " + nightsLeft;
            string subs = GuestVisuals.GiftedSubsText(s.Pop);
            string cash = GuestVisuals.Colored(s.Cash + " $", UIPalette.Green);
            walletLabel.text = watching
                ? Loc.Get("shop.wallet_other", ("player", ctx.Owner), ("subs", subs), ("cash", cash))
                : Loc.Get("shop.wallet", "subs", subs, "cash", cash);

            bool readyLocked = ctx.Multiplayer && ctx.SyncNights && ctx.LocalReady;
            bool locked = readyLocked || watching || waitingForTurn;
            string lockReason = watching ? Loc.Get("shop.lock_watching", "player", ctx.Owner)
                : waitingForTurn ? Loc.Get("shop.lock_turn")
                : Loc.Get("shop.lock_ready");

            var expandBlocker = PartyEngine.GetExpandBlocker(s);
            expandButton.interactable = expandBlocker == null && !locked;
            expandLabel.text = s.Capacity >= GameRules.MaxCapacity
                ? Loc.Get("shop.cap_maxed")
                : Loc.Get("shop.raise_cap", ("from", s.Capacity), ("to", s.Capacity + 1), ("cost", $"<color=#6BE38B>{PartyEngine.ExpansionCost(s)} $</color>"));
            if (expandTooltip != null) expandTooltip.Text = watching || waitingForTurn ? lockReason
                : expandBlocker != null ? Loc.Error(expandBlocker) : Loc.Get("shop.raise_cap_tip");

            rolodexLabel.text = (watching
                ? Loc.Get("shop.view_audience_other", "player", ctx.Owner, "count", s.PermanentCount)
                : Loc.Get("shop.view_audience", "count", s.PermanentCount)) + "  <color=#B9AED6>[R]</color>";

            if (power != null) power.Render(s, ctx.RivalsAvailable, locked ? lockReason : null);

            bool nextInteractable = true;
            if (watching)
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

            for (int i = 0; i < s.Shop.Count; i++)
            {
                if (i >= m_Rows.Count)
                {
                    var row = Instantiate(shopRowPrefab, shopList);
                    row.BuyClicked += type => BuyClicked?.Invoke(type);
                    row.Hovered += (r, on) => ShopHovered?.Invoke(on ? r.Type : (GuestType?)null);
                    m_Rows.Add(row);
                }
                var view = m_Rows[i];
                view.gameObject.SetActive(true);
                var type = s.Shop[i];
                view.Bind(type, true);
                var info = GuestCatalog.Get(type);
                var blocker = PartyEngine.GetBuyBlocker(s, type);
                view.SetShopState(s.CountOwned(type), info.MaxCopies, locked ? lockReason : blocker != null ? Loc.Error(blocker) : null);
            }
            for (int i = s.Shop.Count; i < m_Rows.Count; i++) m_Rows[i].gameObject.SetActive(false);
        }

        void RenderResult(PlayerState s, string breakdown, string owner)
        {
            Color accent;
            switch (s.LastOutcome)
            {
                case PartyOutcome.Success:
                    accent = UIPalette.Gold;
                    resultTitle.text = s.LastDiamonds > 0 ? Loc.Get("result.success_diamonds", "diamonds", Terms.DiamondCount(s.LastDiamonds)) : Loc.Get("result.success");
                    resultDetail.text = Loc.Get("result.earned", "subs", GuestVisuals.GiftedSubsText(s.LastPop), "cash", GuestVisuals.CashText(s.LastCash)) +
                                        (s.LastPenalty > 0 ? $"\n<color=#FF5A5A>{Loc.Get("result.fees", "count", s.LastPenalty)}</color>" : string.Empty);
                    break;
                case PartyOutcome.Raid:
                    accent = UIPalette.Red;
                    resultTitle.text = Loc.Get("result.swatted");
                    var blamed = s.LastBlamedId >= 0 ? s.Find(s.LastBlamedId) : null;
                    resultDetail.text = Loc.Get("result.no_payout") + (blamed != null ? "\n" + Loc.Get("result.banned", "viewer", GuestVisuals.NameTag(blamed.Type)) : string.Empty);
                    break;
                case PartyOutcome.FireMarshal:
                    accent = UIPalette.Orange;
                    resultTitle.text = Loc.Get("result.crashed");
                    resultDetail.text = Loc.Get("result.crashed_detail");
                    break;
                default:
                    accent = UIPalette.Teal;
                    resultTitle.text = Loc.Get("result.planning");
                    resultDetail.text = string.Empty;
                    break;
            }
            if (owner != null) resultTitle.text = Loc.Get("result.owner_prefix", "player", owner, "text", resultTitle.text);
            resultTitle.color = accent;
            if (resultAccent != null) resultAccent.color = accent;
            breakdownLabel.text = s.LastOutcome == PartyOutcome.Success ? breakdown ?? string.Empty : string.Empty;
        }
    }
}
