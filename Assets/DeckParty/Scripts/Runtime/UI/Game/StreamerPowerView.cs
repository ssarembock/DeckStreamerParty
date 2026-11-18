using System;
using DeckParty.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// The streamer power slot in the shop: portrait, power name and description, cost/cooldown status and the Use button.
    /// Players without a streamer see an empty slot.
    /// </summary>
    public sealed class StreamerPowerView : MonoBehaviour
    {
        [SerializeField] internal Image portrait;
        [SerializeField] internal Image placeholder;
        [SerializeField] internal TMP_Text initialsLabel;
        [SerializeField] internal TMP_Text nameLabel;
        [SerializeField] internal TMP_Text kindLabel;
        [SerializeField] internal TMP_Text powerLabel;
        [SerializeField] internal TMP_Text descriptionLabel;
        [SerializeField] internal TMP_Text statusLabel;
        [SerializeField] internal Button useButton;
        [SerializeField] internal TMP_Text useLabel;
        [SerializeField] internal TooltipTrigger useTooltip;
        [Tooltip("The price above the Use button (scenes built since 2026-11-11; older scenes show it on the button).")]
        [SerializeField] internal TMP_Text useCost;
        [SerializeField] internal GameObject emptyState;
        [SerializeField] internal GameObject content;

        [Header("Website look (optional)")]
        [Tooltip("Hovering the streamer: name, power, what it does and its status (the words that used to sit on the card).")]
        [SerializeField] internal TooltipTrigger infoTooltip;
        [Tooltip("A small badge with the turns left before the active can be used again.")]
        [SerializeField] internal GameObject cooldownBadge;
        [SerializeField] internal TMP_Text cooldownLabel;
        [Tooltip("Shown while the active is armed for the next stream.")]
        [SerializeField] internal GameObject armedBadge;

        public event Action UseClicked;
        public RectTransform UseButtonRect => useButton != null ? (RectTransform)useButton.transform : null;

        string m_Owner;
        PlayerState m_State;
        bool m_Rivals;
        /// <summary>Teams: whose active this button is (each team member plays their own streamer since 2026-11-18).</summary>
        int m_Member;

        void Awake()
        {
            if (useButton != null) useButton.onClick.AddListener(() => UseClicked?.Invoke());
            PadShortcut.Bind(useButton, PadAction.UseActive);
            if (infoTooltip != null) infoTooltip.Provider = InfoTooltip;
        }

        string InfoTooltip() => Info()?.ToString();

        /// <summary>Who the streamer is, what their power does and whether it's ready (the price rules sit under it).</summary>
        TipText Info()
        {
            if (m_State == null) return null;
            var type = m_State.StreamerOf(m_Member);
            if (type == StreamerType.None) return TipText.Titled(Loc.Get("power.none"));
            var info = StreamerCatalog.Get(type);
            var tip = TipText.Titled(StreamerVisuals.Name(type), StreamerVisuals.KindLabel(type));
            tip.Line(TipText.Bold(GuestVisuals.Colored(StreamerVisuals.PowerName(type), UIPalette.Pink)) + "  " + StreamerVisuals.PowerDescription(type));
            tip.Section().Line(info.IsPassive ? GuestVisuals.Colored(Loc.Get("power.passive"), UIPalette.Teal) : Status(m_State, m_Member, info, m_Rivals, false));
            if (!info.IsPassive) tip.Detail(StreamerVisuals.CostRules(type));
            return tip;
        }

        void OnDisable() => ResetPose();

        /// <summary>
        /// Puts the card back at rest. This one card shows whichever player's shop is on screen, so an effect still running
        /// (or one that ended off-rest) from another player's power must never carry over to the next board.
        /// </summary>
        public void ResetPose()
        {
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        /// <param name="owner">Whose shop this is (null for your own); switching to another player's board resets the card's pose.</param>
        /// <param name="lockReason">Why the button can't be pressed right now (watching, not your turn...), or null.</param>
        /// <param name="member">Teams: whose streamer the button shows (the member on screen; 0 for a solo season).</param>
        public void Render(PlayerState s, bool rivalsAvailable, string lockReason, string owner = null, int member = 0)
        {
            if (owner != m_Owner)
            {
                m_Owner = owner;
                ResetPose();
            }

            m_State = s;
            m_Rivals = rivalsAvailable;
            m_Member = s.IsTeam ? member : 0;
            var type = s.StreamerOf(m_Member);
            int cooldown = s.PowerCooldownOf(m_Member);
            bool armed = s.PowerArmedOf(m_Member);
            var info = StreamerCatalog.Get(type);
            bool hasStreamer = type != StreamerType.None;
            if (emptyState != null) emptyState.SetActive(!hasStreamer);
            if (content != null) content.SetActive(hasStreamer);
            if (cooldownBadge != null) cooldownBadge.SetActive(hasStreamer && info.IsActive && cooldown > 0 && !armed);
            if (cooldownLabel != null) cooldownLabel.text = cooldown.ToString();
            if (armedBadge != null) armedBadge.SetActive(hasStreamer && armed);
            if (!hasStreamer)
            {
                if (useButton != null) useButton.gameObject.SetActive(false);
                return;
            }

            var sprite = StreamerVisuals.Portrait(type);
            if (portrait != null)
            {
                portrait.enabled = sprite != null;
                portrait.sprite = sprite;
            }
            if (placeholder != null)
            {
                placeholder.gameObject.SetActive(sprite == null);
                placeholder.color = StreamerVisuals.Color(type);
            }
            if (initialsLabel != null) initialsLabel.text = StreamerVisuals.Initials(type);
            if (nameLabel != null) nameLabel.text = StreamerVisuals.NameTag(type);
            if (kindLabel != null) kindLabel.text = StreamerVisuals.KindLabel(type);
            if (powerLabel != null) powerLabel.text = StreamerVisuals.PowerName(type);
            if (descriptionLabel != null) descriptionLabel.text = StreamerVisuals.PowerDescription(type);

            if (info.IsPassive)
            {
                if (statusLabel != null) statusLabel.text = GuestVisuals.Colored(Loc.Get("power.passive"), UIPalette.Teal);
                if (useButton != null) useButton.gameObject.SetActive(false);
                return;
            }

            string blocker = PartyEngine.GetPowerBlocker(s, rivalsAvailable, m_Member);
            if (statusLabel != null) statusLabel.text = Status(s, m_Member, info, rivalsAvailable);

            if (useButton == null) return;
            useButton.gameObject.SetActive(true);
            string useWords = Loc.Get("power.use", "cost", StreamerVisuals.CostText(StreamerCatalog.Cost(s, m_Member)));
            // Boomers made the active cheaper for this shop (as they do Sub Cap upgrades).
            if (s.ExpansionDiscount > 0 && StreamerCatalog.BaseCost(s, m_Member) > 0)
                useWords += "\n" + GameIcons.Tag(Community.Boomer) + " " + Loc.Get("log.perk_boomer", "value", s.ExpansionDiscount);
            // Icon button: just the price next to the icon; the words are in the tooltip.
            int cost = StreamerCatalog.Cost(s, m_Member);
            bool skinned = useButton.TryGetComponent<ShopButtonSkin>(out var useSkin);
            if (skinned)
            {
                // The button shows the streamer and the active's icon (UI Skin > Shop can add text); the price sits above it.
                useSkin.SetCaption(string.Empty);
                if (useCost != null)
                {
                    useCost.text = StreamerVisuals.CostText(cost);
                    useCost.color = ShopLook.CostColour(s.Influence >= cost, SiteColours.Get(SiteColourRole.Text, useCost.transform));
                }
            }
            // Older scenes: just the price next to the icon; the words are in the tooltip.
            else if (useLabel != null) useLabel.text = infoTooltip != null ? StreamerVisuals.CostText(cost) : useWords;
            useButton.interactable = blocker == null && lockReason == null;
            if (useTooltip != null)
            {
                string why = lockReason ?? (blocker != null ? Loc.Error(blocker) : null);
                if (infoTooltip == null && skinned)
                {
                    // Scenes built since 2026-11-15: the streamer is on the button, so its one tooltip says who they are,
                    // what the power does, its status and the price.
                    useTooltip.Text = Info().Line(useWords).Warn(why);
                }
                else useTooltip.Text = infoTooltip != null
                    ? TipText.Titled(StreamerVisuals.PowerName(type)).Line(useWords).Detail(StreamerVisuals.CostRules(type)).Warn(why)
                    : why ?? StreamerVisuals.CostRules(type);
            }
        }

        /// <param name="withRules">False for the pop-up, which shows the price rules on a line of their own.</param>
        static string Status(PlayerState s, int member, StreamerInfo info, bool rivalsAvailable, bool withRules = true)
        {
            if (info.NeedsRival && !rivalsAvailable) return GuestVisuals.Colored(Loc.Get("power.single_player"), UIPalette.TextDim);
            if (s.PowerArmedOf(member)) return GuestVisuals.Colored(Loc.Get("power.armed"), UIPalette.Gold);
            int cooldown = s.PowerCooldownOf(member);
            if (cooldown > 0) return GuestVisuals.Colored(Loc.Get("power.cooldown", "count", cooldown), UIPalette.Orange);
            string ready = GuestVisuals.Colored(Loc.Get("power.ready"), UIPalette.Green);
            return withRules ? ready + "   " + GuestVisuals.Colored(StreamerVisuals.CostRules(s.StreamerOf(member)), UIPalette.TextDim) : ready;
        }
    }
}
