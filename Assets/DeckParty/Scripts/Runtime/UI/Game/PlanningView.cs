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
    }

    /// <summary>Between parties: last party result, guest shop, house expansion and the next-night button.</summary>
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

        public event Action<GuestType> BuyClicked;
        public event Action ExpandClicked;
        public event Action RolodexClicked;
        public event Action NextNightClicked;

        readonly List<GuestRowView> m_Rows = new List<GuestRowView>();

        void Awake()
        {
            expandButton.onClick.AddListener(() => ExpandClicked?.Invoke());
            rolodexButton.onClick.AddListener(() => RolodexClicked?.Invoke());
            nextNightButton.onClick.AddListener(() => NextNightClicked?.Invoke());
            ApplyCaptions();
        }

        /// <summary>Static shop captions are set here too, so scenes generated before a rename show the new terms.</summary>
        void ApplyCaptions()
        {
            var shop = transform.Find("Shop");
            if (shop == null) return;
            var header = shop.Find("Header");
            if (header != null && header.TryGetComponent<TMP_Text>(out var headerText)) headerText.text = "Recruit Guests";
            var hint = shop.Find("Hint");
            if (hint != null && hint.TryGetComponent<TMP_Text>(out var hintText))
                hintText.text = $"Spend {Terms.GiftedSubs} to add guests to your {Terms.Squad} for every future party. Hover a guest for details.";
        }

        public void Render(PlayerState s, PlanningContext ctx, string breakdown)
        {
            bool watching = ctx.Owner != null;
            bool waitingForTurn = ctx.Turns && !ctx.MyTurn && !watching;
            RenderResult(s, breakdown, ctx.Owner);

            string nightsLeft = $"<color=#B9AED6>{s.NightsRemaining} night{(s.NightsRemaining == 1 ? "" : "s")} left.</color>";
            nightLabel.text = watching
                ? $"{ctx.Owner}'s night {s.Night} of {s.TotalNights} is over.  {nightsLeft}"
                : $"Night {s.Night} of {s.TotalNights} is over.  {nightsLeft}";
            walletLabel.text = $"{(watching ? ctx.Owner + " has" : "You have")} {GuestVisuals.GiftedSubsText(s.Pop)} and {GuestVisuals.Colored(s.Cash + " $", UIPalette.Green)}";

            bool readyLocked = ctx.Multiplayer && ctx.SyncNights && ctx.LocalReady;
            bool locked = readyLocked || watching || waitingForTurn;
            string lockReason = watching ? $"This is {ctx.Owner}'s shop. You're watching."
                : waitingForTurn ? "Wait for your turn."
                : "You're marked ready.";

            var expandBlocker = PartyEngine.GetExpandBlocker(s);
            expandButton.interactable = expandBlocker == null && !locked;
            expandLabel.text = s.Capacity >= GameRules.MaxCapacity
                ? $"{Terms.SubCap} is maxed out"
                : $"{Terms.RaiseSubCap} ({s.Capacity} -> {s.Capacity + 1})  <color=#6BE38B>{PartyEngine.ExpansionCost(s)} $</color>";
            if (expandTooltip != null) expandTooltip.Text = watching || waitingForTurn ? lockReason
                : expandBlocker ?? $"A bigger {Terms.SubCap} means more guests at once (and more room for guests who bring friends).";

            rolodexLabel.text = watching
                ? $"View {ctx.Owner}'s {Terms.Squad} ({s.PermanentCount})  <color=#B9AED6>[R]</color>"
                : $"View {Terms.Squad} ({s.PermanentCount})  <color=#B9AED6>[R]</color>";

            bool nextInteractable = true;
            if (watching)
            {
                nextInteractable = false;
                nextNightLabel.text = ctx.Turns ? $"{ctx.Owner} is shopping" : $"Watching {ctx.Owner}";
                waitingLabel.text = ctx.Turns
                    ? "Their turn ends when they're done shopping."
                    : "Click your own name in the Players list to go back to your party.";
            }
            else if (ctx.Turns)
            {
                if (ctx.MyTurn)
                {
                    nextNightLabel.text = "End Turn  <size=70%>[Space]</size>";
                    waitingLabel.text = string.IsNullOrEmpty(ctx.NextName)
                        ? $"Night {s.Night + 1} starts after your turn."
                        : $"Next up: {ctx.NextName}.";
                }
                else
                {
                    nextInteractable = false;
                    nextNightLabel.text = "Waiting for your turn";
                    waitingLabel.text = string.IsNullOrEmpty(ctx.ActiveName) ? string.Empty : $"It's {ctx.ActiveName}'s turn.";
                }
            }
            else if (ctx.Multiplayer && ctx.SyncNights)
            {
                nextNightLabel.text = ctx.LocalReady ? "Cancel Ready" : $"Ready for Night {s.Night + 1}";
                waitingLabel.text = ctx.LocalReady
                    ? (string.IsNullOrEmpty(ctx.WaitingFor) ? "Starting..." : $"Waiting for: {ctx.WaitingFor}")
                    : "The next night starts when everyone is ready.";
            }
            else
            {
                nextNightLabel.text = $"Start Night {s.Night + 1}  <size=70%>[Space]</size>";
                waitingLabel.text = string.Empty;
            }
            nextNightButton.interactable = nextInteractable;

            for (int i = 0; i < s.Shop.Count; i++)
            {
                if (i >= m_Rows.Count)
                {
                    var row = Instantiate(shopRowPrefab, shopList);
                    row.BuyClicked += type => BuyClicked?.Invoke(type);
                    m_Rows.Add(row);
                }
                var view = m_Rows[i];
                view.gameObject.SetActive(true);
                var type = s.Shop[i];
                view.Bind(type, true);
                var info = GuestCatalog.Get(type);
                var blocker = locked ? lockReason : PartyEngine.GetBuyBlocker(s, type);
                view.SetShopState(s.CountOwned(type), info.MaxCopies, blocker);
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
                    resultTitle.text = s.LastDiamonds > 0 ? $"Party success! {Terms.DiamondCount(s.LastDiamonds)} came" : "Party success!";
                    resultDetail.text = $"Earned {GuestVisuals.Colored(s.LastPop + " " + Terms.GiftedSubs, UIPalette.Gold)} and {GuestVisuals.CashText(s.LastCash)}" +
                                        (s.LastPenalty > 0 ? $"\n<color=#FF5A5A>Unpaid fees cost {s.LastPenalty} {Terms.GiftedSubs}.</color>" : string.Empty);
                    break;
                case PartyOutcome.Raid:
                    accent = UIPalette.Red;
                    resultTitle.text = $"{Terms.Raid} The SWAT team showed up";
                    var blamed = s.LastBlamedId >= 0 ? s.Find(s.LastBlamedId) : null;
                    resultDetail.text = "No payout tonight." + (blamed != null ? $"\n{GuestVisuals.NameTag(blamed.Type)} got a {Terms.Ban} and sits out the next party." : string.Empty);
                    break;
                case PartyOutcome.FireMarshal:
                    accent = UIPalette.Orange;
                    resultTitle.text = $"Shut down! {Terms.SubCap} exceeded";
                    resultDetail.text = $"Too many guests for your {Terms.SubCap}. No payout tonight.";
                    break;
                default:
                    accent = UIPalette.Teal;
                    resultTitle.text = "Planning";
                    resultDetail.text = string.Empty;
                    break;
            }
            if (owner != null) resultTitle.text = $"{owner}: {resultTitle.text}";
            resultTitle.color = accent;
            if (resultAccent != null) resultAccent.color = accent;
            breakdownLabel.text = s.LastOutcome == PartyOutcome.Success ? breakdown ?? string.Empty : string.Empty;
        }
    }
}
