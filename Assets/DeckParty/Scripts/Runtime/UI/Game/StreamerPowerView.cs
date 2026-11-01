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

        public event Action UseClicked;
        public RectTransform UseButtonRect => useButton != null ? (RectTransform)useButton.transform : null;

        string m_Owner;

        void Awake()
        {
            if (useButton != null) useButton.onClick.AddListener(() => UseClicked?.Invoke());
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

            var type = s.Streamer;
            var info = StreamerCatalog.Get(type);
            bool hasStreamer = type != StreamerType.None;
            if (emptyState != null) emptyState.SetActive(!hasStreamer);
            if (content != null) content.SetActive(hasStreamer);
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
            if (useLabel != null) useLabel.text = Loc.Get("power.use", "cost", StreamerVisuals.CostText(StreamerCatalog.Cost(s)));
            useButton.interactable = blocker == null && lockReason == null;
            if (useTooltip != null) useTooltip.Text = lockReason ?? (blocker != null ? Loc.Error(blocker) : StreamerVisuals.CostRules(type));
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
