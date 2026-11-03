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

        void Awake()
        {
            if (useButton != null) useButton.onClick.AddListener(() => UseClicked?.Invoke());
            if (infoTooltip != null) infoTooltip.Provider = InfoTooltip;
        }

        string InfoTooltip()
        {
            if (m_State == null) return null;
            var type = m_State.Streamer;
            if (type == StreamerType.None) return Loc.Get("power.none");
            var info = StreamerCatalog.Get(type);
            string text = $"<b><size=115%>{StreamerVisuals.NameTag(type)}</size></b>\n" +
                          GuestVisuals.Colored(StreamerVisuals.KindLabel(type), UIPalette.Gold) + "   " + GuestVisuals.Colored(StreamerVisuals.PowerName(type), UIPalette.Pink) + "\n" +
                          StreamerVisuals.PowerDescription(type);
            text += "\n" + (info.IsPassive ? GuestVisuals.Colored(Loc.Get("power.passive"), UIPalette.Teal) : Status(m_State, info, m_Rivals));
            return text;
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
        public void Render(PlayerState s, bool rivalsAvailable, string lockReason, string owner = null)
        {
            if (owner != m_Owner)
            {
                m_Owner = owner;
                ResetPose();
            }

            m_State = s;
            m_Rivals = rivalsAvailable;
            var type = s.Streamer;
            var info = StreamerCatalog.Get(type);
            bool hasStreamer = type != StreamerType.None;
            if (emptyState != null) emptyState.SetActive(!hasStreamer);
            if (content != null) content.SetActive(hasStreamer);
            if (cooldownBadge != null) cooldownBadge.SetActive(hasStreamer && info.IsActive && s.PowerCooldown > 0 && !s.PowerArmed);
            if (cooldownLabel != null) cooldownLabel.text = s.PowerCooldown.ToString();
            if (armedBadge != null) armedBadge.SetActive(hasStreamer && s.PowerArmed);
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

            string blocker = PartyEngine.GetPowerBlocker(s, rivalsAvailable);
            if (statusLabel != null) statusLabel.text = Status(s, info, rivalsAvailable);

            if (useButton == null) return;
            useButton.gameObject.SetActive(true);
            string useWords = Loc.Get("power.use", "cost", StreamerVisuals.CostText(StreamerCatalog.Cost(s)));
            // Icon button: just the price next to the icon; the words are in the tooltip.
            if (useLabel != null) useLabel.text = infoTooltip != null ? StreamerVisuals.CostText(StreamerCatalog.Cost(s)) : useWords;
            useButton.interactable = blocker == null && lockReason == null;
            if (useTooltip != null)
            {
                string why = lockReason ?? (blocker != null ? Loc.Error(blocker) : null);
                useTooltip.Text = infoTooltip != null
                    ? $"<b>{StreamerVisuals.PowerName(type)}</b>  {useWords}\n{StreamerVisuals.CostRules(type)}" + (why != null ? "\n" + GuestVisuals.Colored(why, UIPalette.Orange) : string.Empty)
                    : why ?? StreamerVisuals.CostRules(type);
            }
        }

        static string Status(PlayerState s, StreamerInfo info, bool rivalsAvailable)
        {
            if (info.NeedsRival && !rivalsAvailable) return GuestVisuals.Colored(Loc.Get("power.single_player"), UIPalette.TextDim);
            if (s.PowerArmed) return GuestVisuals.Colored(Loc.Get("power.armed"), UIPalette.Gold);
            if (s.PowerCooldown > 0) return GuestVisuals.Colored(Loc.Get("power.cooldown", "count", s.PowerCooldown), UIPalette.Orange);
            return GuestVisuals.Colored(Loc.Get("power.ready"), UIPalette.Green) + "   " + GuestVisuals.Colored(StreamerVisuals.CostRules(s.Streamer), UIPalette.TextDim);
        }
    }
}
