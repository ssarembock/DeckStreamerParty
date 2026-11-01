using System;
using DeckParty.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
    /// One viewer card (chat slot, picker entry, Audience entry...). All content lives under <see cref="visual"/>,
    /// which is designed at 200x290 (see DeckPartyAssets.CardLayout) and uniformly scaled to the layout cell.
    /// The portrait is a <see cref="CharacterPortrait"/> (animated when the Game Database has a prefab or controller)
    /// with a <see cref="CardFx"/> for Sprite Shaders Ultimate status effects.
    /// </summary>
    public sealed class GuestCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public const float DesignWidth = 200f;
        public const float DesignHeight = 290f;

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
        [SerializeField] internal CharacterPortrait characterPortrait;

        public int GuestId { get; private set; } = -1;
        public GuestType GuestType { get; private set; }
        public GuestType ShownType { get; private set; }
        public bool IsEmpty => GuestId < 0;
        public RectTransform AnimRoot => animRoot != null ? animRoot : (RectTransform)transform;
        public CharacterPortrait Portrait => characterPortrait;
        public CardFx Fx => m_Fx;

        public event Action<GuestCardView> Clicked;
        public event Action<GuestCardView> AbilityClicked;
        /// <summary>The pointer entered (true) or left (false). Used for multiplayer presence signals.</summary>
        public event Action<GuestCardView, bool> Hovered;

        Color m_FrameBase = Color.white;
        bool m_Hooked;
        CardFx m_Fx;
        bool m_Hover;
        float m_Lift;
        Vector2 m_Tilt;
        float m_BreathPhase;

        void Awake()
        {
            Hook();
            m_BreathPhase = UnityEngine.Random.value * 10f;
        }

        void OnDisable()
        {
            // Interrupted tweens must not leave the card half-scaled.
            if (animRoot != null)
            {
                animRoot.localScale = Vector3.one;
                animRoot.localRotation = Quaternion.identity;
                animRoot.anchoredPosition = Vector2.zero;
            }
            m_Hover = false;
            m_Lift = 0f;
        }

        void Hook()
        {
            if (m_Hooked) return;
            m_Hooked = true;
            if (button != null) button.onClick.AddListener(() => Clicked?.Invoke(this));
            if (abilityButton != null) abilityButton.onClick.AddListener(() => AbilityClicked?.Invoke(this));
            if (frame != null) m_FrameBase = frame.color;
            if (characterPortrait == null && portrait != null)
            {
                characterPortrait = portrait.GetComponent<CharacterPortrait>();
                if (characterPortrait == null) characterPortrait = portrait.gameObject.AddComponent<CharacterPortrait>();
                characterPortrait.image = portrait;
            }
            if (portrait != null)
            {
                m_Fx = portrait.GetComponent<CardFx>();
                if (m_Fx == null) m_Fx = portrait.gameObject.AddComponent<CardFx>();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            m_Hover = true;
            Hovered?.Invoke(this, true);
            if (!IsEmpty) AudioService.PlayHoverTick(transform.GetSiblingIndex());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            m_Hover = false;
            Hovered?.Invoke(this, false);
        }

        void Update()
        {
            if (highlight != null && highlight.enabled)
            {
                var c = highlight.color;
                c.a = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 6f);
                highlight.color = c;
            }
            UpdateMotion();
        }

        /// <summary>Hover lift (spring), tilt toward the cursor (N7) and idle breathing for static portraits (N8).</summary>
        void UpdateMotion()
        {
            if (animRoot == null || IsEmpty) return;
            var fx = FxLibrary.Current;
            float dt = Time.unscaledDeltaTime;

            float liftTarget = m_Hover ? 1f : 0f;
            m_Lift = Mathf.Lerp(m_Lift, liftTarget, 1f - Mathf.Exp(-14f * dt));
            animRoot.anchoredPosition = new Vector2(0f, 8f * m_Lift);

            Vector2 tiltTarget = Vector2.zero;
            if (fx.cardTilt && m_Hover && visual != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(visual, InputHelper.PointerPosition, null, out var local);
                var half = visual.rect.size * 0.5f;
                tiltTarget = new Vector2(Mathf.Clamp(local.y / Mathf.Max(1f, half.y), -1f, 1f) * -7f, Mathf.Clamp(local.x / Mathf.Max(1f, half.x), -1f, 1f) * 7f);
            }
            m_Tilt = Vector2.Lerp(m_Tilt, tiltTarget, 1f - Mathf.Exp(-10f * dt));
            if (m_Tilt.sqrMagnitude > 0.0001f || m_Hover) animRoot.localRotation = Quaternion.Euler(m_Tilt.x, m_Tilt.y, animRoot.localEulerAngles.z);

            if (portrait != null && fx.idleBreathing && (characterPortrait == null || !characterPortrait.IsAnimated))
            {
                float breath = 1f + 0.018f * Mathf.Sin(Time.unscaledTime * 1.6f + m_BreathPhase);
                portrait.rectTransform.localScale = new Vector3(1f / breath, breath, 1f);
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
            if (m_EmptyLabel != null) m_EmptyLabel.text = label ?? Loc.Get("card.empty");
            if (button != null) button.interactable = false;
            SetHighlight(CardHighlight.None);
            if (tooltip != null) tooltip.Provider = null;
            if (countBadge != null) countBadge.SetActive(false);
            if (frame != null) frame.color = m_FrameBase.WithAlpha(0.35f);
            if (characterPortrait != null) characterPortrait.Clear();
            if (m_Fx != null) m_Fx.Clear();
        }

        TMP_Text m_EmptyLabel;

        /// <summary>Shows a viewer. With a state and <paramref name="live"/>, stats reflect the current stream.</summary>
        public void ShowGuest(GuestInstance guest, PlayerState state, bool live)
        {
            Hook();
            GuestId = guest.Id;
            GuestType = guest.Type;
            live = live && state != null;

            // In chat a Rickrolled viewer looks like the character they've become.
            var shown = live ? guest.Kind : guest.Type;
            ShownType = shown;
            var info = GuestCatalog.Get(shown);

            content.SetActive(true);
            emptyState.SetActive(false);

            var sprite = GuestVisuals.Portrait(shown);
            var art = GameDatabase.Instance.GetGuestArt(shown);
            bool hasArt = sprite != null || (art != null && art.IsAnimated);
            if (characterPortrait != null) characterPortrait.ShowViewer(shown, IdleState(guest, state, live));
            else
            {
                portrait.enabled = sprite != null;
                portrait.sprite = sprite;
            }
            placeholder.gameObject.SetActive(!hasArt);
            placeholder.color = GuestVisuals.Tint(shown);
            initialsLabel.text = GuestVisuals.Initials(shown);

            nameLabel.text = live && guest.IsDisguised
                ? GuestVisuals.Colored(GuestVisuals.DisplayName(shown) + "?", UIPalette.Pink)
                : GuestVisuals.DisplayName(shown);
            statsLabel.text = live ? GuestVisuals.LiveStats(state, guest) : GuestVisuals.BaseStats(info);

            bool cleared = live && guest.TroubleCleared && PartyEngine.PrintedTrouble(guest);
            bool trouble = live ? PartyEngine.HasTroubleProperty(state, guest) || cleared : info.Trouble;
            starBadge.SetActive(info.IsDiamond);
            troubleBadge.SetActive(trouble);
            if (troubleIcon != null)
                troubleIcon.color = trouble && live && !PartyEngine.HasTroubleProperty(state, guest) ? UIPalette.TextDim.WithAlpha(0.6f) : UIPalette.Red;
            cancelBadge.SetActive(live ? PartyEngine.CancelsTroubleNow(state, guest) : info.CancelsTrouble);

            // Permanent upgrades: Subs (pink badge) first, otherwise Cash (Checkout Code, Gen Z with Job).
            int bonus = guest.BonusPop;
            int bonusCash = guest.BonusCash;
            bonusBadge.SetActive(bonus != 0 || bonusCash != 0);
            if (bonusLabel != null)
                bonusLabel.text = bonus != 0 ? (bonus > 0 ? "+" : "") + bonus : (bonusCash > 0 ? "+" : "") + bonusCash + Terms.CashSymbol;

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

            if (m_Fx != null) m_Fx.SetStatus(state, guest, live, false, false, state != null && state.BannedThisParty == guest.Id);
            if (tooltip != null) tooltip.Provider = () => GuestVisuals.Tooltip(shown, guest, state);
            if (countBadge != null) countBadge.SetActive(false);
            HideAbility();
        }

        /// <summary>Which idle animation fits: actions not used yet rest in Idle_Pre_Active, used ones in Idle_Post_Active.</summary>
        public static CharacterState IdleState(GuestInstance guest, PlayerState state, bool live)
        {
            var info = live ? guest.Info : guest.BaseInfo;
            if (!info.HasAbility) return CharacterState.Idle;
            if (live && state.UsedAbilities.Contains(guest.Id)) return CharacterState.IdlePostActive;
            return CharacterState.IdlePreActive;
        }

        /// <summary>An extra space taken up by a big viewer (Big Chungus fills 3).</summary>
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

        /// <summary>Joined chat: springy pop, glow-in, and the optional Enter animation.</summary>
        public void PlayArrive(float delay, bool diamond = false)
        {
            if (delay <= 0f || !isActiveAndEnabled) DoArrive(diamond);
            else StartCoroutine(ArriveLater(delay, diamond));
        }

        System.Collections.IEnumerator ArriveLater(float delay, bool diamond)
        {
            AnimRoot.localScale = Vector3.one * 0.01f;
            yield return new WaitForSecondsRealtime(delay * DeckParty.Fx.DurationScale);
            DoArrive(diamond);
        }

        void DoArrive(bool diamond)
        {
            AnimRoot.localScale = Vector3.one;
            DeckParty.Fx.Play(diamond ? FxCue.DiamondJoined : FxCue.ViewerJoined, AnimRoot);
            if (m_Fx != null)
            {
                m_Fx.Pulse(CardPulse.GlowIn);
                if (diamond) m_Fx.Pulse(CardPulse.Shine);
            }
            if (characterPortrait != null) characterPortrait.Play(CharacterState.Enter);
        }

        public void PlayPulse() => DeckParty.Fx.Play(FxCue.CounterPunch, AnimRoot, 0.8f);

        /// <summary>The card's action fired: the Active animation (if any) plus a wind-up punch.</summary>
        public void PlayActive()
        {
            DeckParty.Fx.Play(FxCue.AbilityUsed, AnimRoot);
            if (characterPortrait != null) characterPortrait.Play(CharacterState.Active);
        }
    }
}
