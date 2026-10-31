using System;
using DeckParty.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    public enum CardHighlight
    {
        None,
        Selectable,
        Selected,
        Dimmed,
    }

    /// <summary>
    /// One guest card (house slot, picker entry, rolodex entry...). All content lives under <see cref="visual"/>,
    /// which is designed at 200x260 and uniformly scaled to whatever cell size the layout provides.
    /// </summary>
    public sealed class GuestCardView : MonoBehaviour
    {
        public const float DesignWidth = 200f;
        public const float DesignHeight = 260f;

        [SerializeField] internal Button button;
        [Tooltip("Scaled to fit the layout cell.")]
        [SerializeField] internal RectTransform visual;
        [Tooltip("Child of visual that animations scale (keeps layout scale intact).")]
        [SerializeField] internal RectTransform animRoot;
        [SerializeField] internal Image frame;
        [SerializeField] internal Image highlight;
        [SerializeField] internal GameObject content;
        [SerializeField] internal GameObject emptyState;
        [SerializeField] internal Image portrait;
        [SerializeField] internal Image placeholder;
        [SerializeField] internal TMP_Text initialsLabel;
        [SerializeField] internal TMP_Text nameLabel;
        [SerializeField] internal TMP_Text statsLabel;
        [SerializeField] internal GameObject starBadge;
        [SerializeField] internal GameObject troubleBadge;
        [SerializeField] internal Image troubleIcon;
        [SerializeField] internal GameObject cancelBadge;
        [SerializeField] internal GameObject bonusBadge;
        [SerializeField] internal TMP_Text bonusLabel;
        [SerializeField] internal GameObject countBadge;
        [SerializeField] internal TMP_Text countLabel;
        [SerializeField] internal Button abilityButton;
        [SerializeField] internal TMP_Text abilityLabel;
        [SerializeField] internal TooltipTrigger abilityTooltip;
        [SerializeField] internal Image dim;
        [SerializeField] internal TooltipTrigger tooltip;

        public int GuestId { get; private set; } = -1;
        public GuestType GuestType { get; private set; }
        public bool IsEmpty => GuestId < 0;

        public event Action<GuestCardView> Clicked;
        public event Action<GuestCardView> AbilityClicked;

        Color m_FrameBase = Color.white;
        bool m_Hooked;

        void Awake() => Hook();

        void OnDisable()
        {
            // Interrupted tweens must not leave the card half-scaled.
            if (animRoot != null) animRoot.localScale = Vector3.one;
        }

        void Hook()
        {
            if (m_Hooked) return;
            m_Hooked = true;
            if (button != null) button.onClick.AddListener(() => Clicked?.Invoke(this));
            if (abilityButton != null) abilityButton.onClick.AddListener(() => AbilityClicked?.Invoke(this));
            if (frame != null) m_FrameBase = frame.color;
        }

        void Update()
        {
            if (highlight != null && highlight.enabled)
            {
                var c = highlight.color;
                c.a = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 6f);
                highlight.color = c;
            }
        }

        public void SetScale(float scale)
        {
            if (visual != null) visual.localScale = Vector3.one * scale;
        }

        public static Vector2 CellSize(float scale) => new Vector2(DesignWidth * scale, DesignHeight * scale);

        /// <summary>Sizes a GridLayoutGroup's cells for cards shown at <paramref name="scale"/>.</summary>
        public static void FitGrid(RectTransform gridRoot, float scale)
        {
            if (gridRoot == null) return;
            var layout = gridRoot.GetComponent<GridLayoutGroup>();
            if (layout != null) layout.cellSize = CellSize(scale);
        }

        public void ShowEmpty(string label = null)
        {
            Hook();
            GuestId = -1;
            content.SetActive(false);
            emptyState.SetActive(true);
            if (m_EmptyLabel == null) m_EmptyLabel = emptyState.GetComponentInChildren<TMP_Text>(true);
            if (m_EmptyLabel != null) m_EmptyLabel.text = label ?? "Empty";
            if (button != null) button.interactable = false;
            SetHighlight(CardHighlight.None);
            if (tooltip != null) tooltip.Provider = null;
            if (countBadge != null) countBadge.SetActive(false);
            if (frame != null) frame.color = m_FrameBase.WithAlpha(0.35f);
        }

        TMP_Text m_EmptyLabel;

        /// <summary>Shows a guest. With a state and <paramref name="live"/>, stats reflect the current party.</summary>
        public void ShowGuest(GuestInstance guest, PlayerState state, bool live)
        {
            Hook();
            GuestId = guest.Id;
            GuestType = guest.Type;
            live = live && state != null;

            // At the party a Rickrolled guest looks like the character they've become.
            var shown = live ? guest.Kind : guest.Type;
            var info = GuestCatalog.Get(shown);

            content.SetActive(true);
            emptyState.SetActive(false);

            var sprite = GuestVisuals.Portrait(shown);
            portrait.enabled = sprite != null;
            portrait.sprite = sprite;
            placeholder.gameObject.SetActive(sprite == null);
            placeholder.color = GuestVisuals.Tint(shown);
            initialsLabel.text = GuestVisuals.Initials(shown);

            nameLabel.text = live && guest.IsDisguised
                ? GuestVisuals.Colored(GuestVisuals.DisplayName(shown) + "?", UIPalette.Pink)
                : GuestVisuals.DisplayName(shown);
            statsLabel.text = live ? GuestVisuals.LiveStats(state, guest) : GuestVisuals.BaseStats(info);

            bool cleared = live && guest.TroubleCleared && info.Trouble;
            bool trouble = live ? PartyEngine.HasTroubleProperty(state, guest) || cleared : info.Trouble;
            starBadge.SetActive(info.IsDiamond);
            troubleBadge.SetActive(trouble);
            if (troubleIcon != null)
                troubleIcon.color = trouble && live && !PartyEngine.HasTroubleProperty(state, guest) ? UIPalette.TextDim.WithAlpha(0.6f) : UIPalette.Red;
            cancelBadge.SetActive(live ? PartyEngine.CancelsTroubleNow(state, guest) : info.CancelsTrouble);

            int bonus = guest.BonusPop;
            bonusBadge.SetActive(bonus != 0);
            if (bonusLabel != null) bonusLabel.text = (bonus > 0 ? "+" : "") + bonus;

            if (frame != null)
            {
                m_FrameBase = info.IsDiamond ? Color.Lerp(UIPalette.PanelAlt, UIPalette.Gold, 0.28f) : UIPalette.PanelAlt;
                if (live && guest.Infected) m_FrameBase = Color.Lerp(m_FrameBase, UIPalette.Green, 0.3f);
                if (live && guest.Immune) m_FrameBase = Color.Lerp(m_FrameBase, UIPalette.Teal, 0.3f);
                frame.color = m_FrameBase;

                // Diamonds use the CardFrameStar skin sprite when one is assigned.
                var skin = GameDatabase.Instance.skin;
                if (skin != null)
                {
                    var diamondFrame = skin.GetSprite(SkinSlot.CardFrameStar);
                    var normalFrame = skin.GetSprite(SkinSlot.CardFrame);
                    var chosen = info.IsDiamond && diamondFrame != null ? diamondFrame : normalFrame;
                    if (chosen != null) frame.sprite = chosen;
                }
            }

            if (tooltip != null) tooltip.Provider = () => GuestVisuals.Tooltip(shown, guest, state);
            if (countBadge != null) countBadge.SetActive(false);
            HideAbility();
        }

        /// <summary>An extra space taken up by a big guest (Big Chungus fills 3).</summary>
        public void ShowFiller(GuestType owner)
        {
            ShowEmpty($"<size=80%>{GuestVisuals.DisplayName(owner)}</size>");
            if (frame != null) frame.color = GuestVisuals.Tint(owner).WithAlpha(0.35f);
        }

        public void SetCount(int count)
        {
            if (countBadge == null) return;
            countBadge.SetActive(count > 1);
            if (countLabel != null) countLabel.text = "x" + count;
        }

        public void ShowAbility(string label, bool enabled, string blocker)
        {
            if (abilityButton == null) return;
            abilityButton.gameObject.SetActive(true);
            abilityButton.interactable = enabled;
            abilityLabel.text = label;
            if (abilityTooltip != null) abilityTooltip.Text = enabled ? null : blocker;
        }

        public void HideAbility()
        {
            if (abilityButton != null) abilityButton.gameObject.SetActive(false);
        }

        public void SetClickable(bool clickable)
        {
            if (button != null) button.interactable = clickable && !IsEmpty;
        }

        public void SetHighlight(CardHighlight mode)
        {
            if (highlight != null)
            {
                highlight.enabled = mode == CardHighlight.Selectable || mode == CardHighlight.Selected;
                highlight.color = (mode == CardHighlight.Selected ? UIPalette.Pink : UIPalette.Gold);
            }
            if (dim != null) dim.enabled = mode == CardHighlight.Dimmed;
        }

        public void PlayArrive(float delay) => UITween.PopIn(this, animRoot, 0.32f, delay, 0.35f);

        public void PlayPulse() => UITween.Punch(this, animRoot, 0.12f, 0.3f);
    }
}
