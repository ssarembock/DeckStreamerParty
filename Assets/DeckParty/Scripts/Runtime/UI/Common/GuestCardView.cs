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
    /// which is designed at <see cref="DesignSize"/> (cards 200x290, see DeckPartyAssets.CardLayout) and uniformly scaled to
    /// the layout cell. The portrait is a <see cref="CharacterPortrait"/> (animated when the Game Database has a prefab or
    /// controller) with a <see cref="CardFx"/> for Sprite Shaders Ultimate status effects.
    /// <para>
    /// The same view drives the round chat avatar (<see cref="avatarMode"/>, the ChatAvatar prefab): the viewer's head in a
    /// circle (Game Database > Avatar, or the portrait zoomed in), a ring whose colour shows their state, and while their
    /// action is still unused the ring pulses in the platform colour and the avatar wiggles now and then. Names only show on hover.
    /// </para>
    /// </summary>
    public sealed class GuestCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public const float DesignWidth = 200f;
        public const float DesignHeight = 290f;

        [Tooltip("The size everything under Visual is designed at (cards 200x290, chat avatars 180x206).")]
        [SerializeField] internal Vector2 designSize = new Vector2(DesignWidth, DesignHeight);
        [Tooltip("Round chat avatar: head in a circle, state ring, no name.")]
        [SerializeField] internal bool avatarMode;
        [Tooltip("Sprite Shaders Ultimate status effects on the portrait (cards). Avatars show state with their ring instead.")]
        [SerializeField] internal bool statusShaders = true;
        [Tooltip("Avatar: the circle (wiggles while the action is unused).")]
        [SerializeField] internal RectTransform faceRoot;
        [Tooltip("Avatar: the masked circle the head is framed in.")]
        [SerializeField] internal RectTransform faceMask;
        [Tooltip("Avatar: the empty seat's fill (tinted for the extra seats a big viewer takes).")]
        [SerializeField] internal Image emptyFill;

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
        [Tooltip("The viewer's community icons (left edge of the portrait).")]
        [SerializeField] internal CommunityIconsView communities;

        public int GuestId { get; private set; } = -1;
        public GuestType GuestType { get; private set; }
        public GuestType ShownType { get; private set; }
        public bool IsEmpty => GuestId < 0;
        public RectTransform AnimRoot => animRoot != null ? animRoot : (RectTransform)transform;
        public CharacterPortrait Portrait => characterPortrait;
        public CardFx Fx => m_Fx;
        public bool IsAvatar => avatarMode;

        /// <summary>The design size of this card (layout scales it uniformly).</summary>
        public Vector2 DesignSize => designSize.x > 1f && designSize.y > 1f ? designSize : new Vector2(DesignWidth, DesignHeight);

        /// <summary>The layout cell for this card shown at <paramref name="scale"/>.</summary>
        public Vector2 CellSizeAt(float scale) => DesignSize * scale;

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
        /// <summary>Avatar: the viewer's action hasn't been used this stream (ring pulse and wiggle).</summary>
        bool m_ActionReady;
        float m_WiggleClock;

        /// <summary>Seconds between an unused-action avatar's wiggles.</summary>
        const float WiggleInterval = 2.6f;
        const float WiggleLength = 0.5f;

        void Awake()
        {
            Hook();
            m_BreathPhase = UnityEngine.Random.value * 10f;
            m_WiggleClock = UnityEngine.Random.value * WiggleInterval;
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
            if (faceRoot != null)
            {
                faceRoot.localRotation = Quaternion.identity;
                faceRoot.localScale = Vector3.one;
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
            if (portrait != null && statusShaders)
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
            if (avatarMode) UpdateActionCue();
        }

        /// <summary>
        /// Avatar: while the viewer's action is unused, the ring pulses in the platform colour and the circle gives a little
        /// wiggle every few seconds, so players see at a glance which actions are still available.
        /// </summary>
        void UpdateActionCue()
        {
            if (IsEmpty || !m_ActionReady)
            {
                if (faceRoot != null && faceRoot.localRotation != Quaternion.identity) faceRoot.localRotation = Quaternion.identity;
                return;
            }
            float t = Time.unscaledTime;
            if (frame != null)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(t * 4.2f + m_BreathPhase);
                frame.color = Color.Lerp(SiteTheme.Accent, Color.Lerp(SiteTheme.Accent, Color.white, 0.55f), pulse);
            }
            if (faceRoot == null) return;
            m_WiggleClock += Time.unscaledDeltaTime;
            if (m_WiggleClock >= WiggleInterval) m_WiggleClock -= WiggleInterval;
            float k = m_WiggleClock / WiggleLength;
            float angle = k < 1f ? Mathf.Sin(k * Mathf.PI * 4f) * 7f * (1f - k) : 0f;
            float bump = k < 1f ? 1f + 0.05f * Mathf.Sin(k * Mathf.PI) : 1f;
            faceRoot.localRotation = Quaternion.Euler(0f, 0f, angle);
            faceRoot.localScale = new Vector3(bump, bump, 1f);
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

        /// <summary>The layout cell of a standard (200x290) card shown at <paramref name="scale"/>.</summary>
        public static Vector2 CellSize(float scale) => new Vector2(DesignWidth * scale, DesignHeight * scale);

        /// <summary>Sizes a GridLayoutGroup's cells for cards shown at <paramref name="scale"/>.</summary>
        public static void FitGrid(RectTransform gridRoot, float scale)
        {
            if (gridRoot == null) return;
            var layout = gridRoot.GetComponent<GridLayoutGroup>();
            if (layout != null) layout.cellSize = CellSize(scale);
        }

        /// <summary>An open chat slot: just the faded frame (no text), or a label such as a big viewer's name on its extra spaces.</summary>
        public void ShowEmpty(string label = null)
        {
            Hook();
            GuestId = -1;
            m_ActionReady = false;
            content.SetActive(false);
            emptyState.SetActive(true);
            if (m_EmptyLabel == null) m_EmptyLabel = emptyState.GetComponentInChildren<TMP_Text>(true);
            if (m_EmptyLabel != null) m_EmptyLabel.text = label ?? string.Empty;
            if (emptyFill != null) emptyFill.color = SitePalette.SurfaceAlt;
            if (button != null) button.interactable = false;
            SetHighlight(CardHighlight.None);
            if (tooltip != null) tooltip.Provider = null;
            if (countBadge != null) countBadge.SetActive(false);
            if (communities != null) communities.Clear();
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
            bool hasArt = sprite != null || (art != null && art.IsAnimated) || (avatarMode && art != null && art.avatar != null);
            if (avatarMode) ShowAvatarFace(shown, art, sprite, IdleState(guest, state, live));
            else if (characterPortrait != null) characterPortrait.ShowViewer(shown, IdleState(guest, state, live));
            else
            {
                portrait.enabled = sprite != null;
                portrait.sprite = sprite;
            }
            placeholder.gameObject.SetActive(!hasArt);
            placeholder.color = GuestVisuals.Tint(shown);
            initialsLabel.text = GuestVisuals.Initials(shown);

            // Names only show on hover (the tooltip); older card prefabs may still have the label.
            if (nameLabel != null)
                nameLabel.text = live && guest.IsDisguised
                    ? GuestVisuals.Colored(GuestVisuals.DisplayName(shown) + "?", UIPalette.Pink)
                    : GuestVisuals.DisplayName(shown);
            if (statsLabel != null) statsLabel.text = live ? GuestVisuals.LiveStats(state, guest) : GuestVisuals.BaseStats(info);

            bool cleared = live && guest.TroubleCleared && PartyEngine.PrintedTrouble(guest);
            bool trouble = live ? PartyEngine.HasTroubleProperty(state, guest) || cleared : info.Trouble;
            if (starBadge != null) starBadge.SetActive(info.IsDiamond);
            if (communities != null) communities.Bind(shown);
            if (troubleBadge != null) troubleBadge.SetActive(trouble);
            if (troubleIcon != null)
                troubleIcon.color = trouble && live && !PartyEngine.HasTroubleProperty(state, guest) ? UIPalette.TextDim.WithAlpha(0.6f) : GameIcons.Tint(ResourceIcon.Risk);
            if (cancelBadge != null) cancelBadge.SetActive(live ? PartyEngine.CancelsTroubleNow(state, guest) : info.CancelsTrouble);

            // Permanent upgrades: Subs (pink badge) first, otherwise Cash (Checkout Code, Gen Z with Job). Icons, not words.
            int bonus = guest.BonusPop;
            int bonusCash = guest.BonusCash;
            if (bonusBadge != null) bonusBadge.SetActive(bonus != 0 || bonusCash != 0);
            if (bonusLabel != null)
                bonusLabel.text = bonus != 0
                    ? (bonus > 0 ? "+" : "") + bonus + GameIcons.Tag(ResourceIcon.Subs)
                    : (bonusCash > 0 ? "+" : "") + bonusCash + GameIcons.Tag(ResourceIcon.Cash);

            if (frame != null && avatarMode)
            {
                // The ring shows the viewer's state; the action cue (pulsing accent) takes over while their action is unused.
                m_FrameBase = AvatarRingColor(info, guest, live);
                frame.color = m_FrameBase;
            }
            else if (frame != null)
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

        /// <summary>The avatar ring when no action is waiting: gold for Plays, green infected, teal immune, otherwise grey.</summary>
        static Color AvatarRingColor(GuestInfo info, GuestInstance guest, bool live)
        {
            if (live && guest.Infected) return UIPalette.Green;
            if (live && guest.Immune) return UIPalette.Teal;
            if (info.IsDiamond) return SitePalette.Gold;
            return SitePalette.Border;
        }

        /// <summary>
        /// Avatar: the head sprite fills the circle; without one the portrait (or animation) is zoomed in on the head using the
        /// viewer's Avatar Zoom and Offset.
        /// </summary>
        void ShowAvatarFace(GuestType shown, GuestArt art, Sprite portraitSprite, CharacterState idle)
        {
            var head = art != null ? art.avatar : null;
            float zoom = head != null ? 1f : art != null && art.avatarZoom > 0.01f ? art.avatarZoom : 1.8f;
            var offset = head != null ? Vector2.zero : art != null ? art.avatarOffset : new Vector2(0f, -0.32f);

            if (characterPortrait != null)
            {
                if (head != null) characterPortrait.Show(null, null, head, 1f, Vector2.zero, idle);
                else characterPortrait.Show(art?.characterPrefab, art?.animatorController, portraitSprite, art != null ? art.framingZoom : 1f,
                    art != null ? art.framingOffset : Vector2.zero, idle);
            }
            else if (portrait != null)
            {
                portrait.sprite = head != null ? head : portraitSprite;
                portrait.enabled = portrait.sprite != null;
            }

            var circle = faceMask != null ? faceMask.rect.size : new Vector2(140f, 140f);
            if (circle.x < 1f) circle = new Vector2(140f, 140f);
            Frame(portrait != null ? portrait.rectTransform : null, circle, zoom, offset);
            if (characterPortrait != null && characterPortrait.animated != null) Frame(characterPortrait.animated.rectTransform, circle, zoom, offset);
        }

        static void Frame(RectTransform rect, Vector2 circle, float zoom, Vector2 offset)
        {
            if (rect == null) return;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = circle * zoom;
            rect.anchoredPosition = new Vector2(offset.x * circle.x, offset.y * circle.y);
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
            ShowEmpty(avatarMode ? null : $"<size=80%>{GuestVisuals.DisplayName(owner)}</size>");
            if (frame != null) frame.color = GuestVisuals.Tint(owner).WithAlpha(0.35f);
            if (emptyFill != null) emptyFill.color = Color.Lerp(SitePalette.SurfaceAlt, GuestVisuals.Tint(owner), 0.35f);
        }

        public void SetCount(int count)
        {
            if (countBadge == null) return;
            countBadge.SetActive(count > 1);
            if (countLabel != null) countLabel.text = "x" + count;
        }

        /// <param name="label">The action's name.</param>
        /// <param name="enabled">Whether it can be pressed now.</param>
        /// <param name="blocker">Why not (already localized), or null.</param>
        /// <param name="used">Already used this stream: cards say so on the button, avatars hide it and stop their cue.</param>
        public void ShowAbility(string label, bool enabled, string blocker, bool used = false)
        {
            SetActionReady(avatarMode && !used);
            if (abilityButton == null) return;
            if (avatarMode && used)
            {
                abilityButton.gameObject.SetActive(false);
                return;
            }
            abilityButton.gameObject.SetActive(true);
            abilityButton.interactable = enabled && !used;
            if (abilityLabel != null) abilityLabel.text = used ? Loc.Get("card.used") : label;
            if (abilityTooltip != null)
            {
                // Icon buttons name their action on hover; text buttons only explain why they're disabled.
                if (abilityLabel == null) abilityTooltip.Text = enabled || string.IsNullOrEmpty(blocker) ? $"<b>{label}</b>" : $"<b>{label}</b>\n{blocker}";
                else abilityTooltip.Text = enabled ? null : blocker;
            }
        }

        public void HideAbility()
        {
            SetActionReady(false);
            if (abilityButton != null) abilityButton.gameObject.SetActive(false);
        }

        void SetActionReady(bool ready)
        {
            if (m_ActionReady == ready) return;
            m_ActionReady = ready;
            if (!ready)
            {
                if (frame != null && avatarMode) frame.color = m_FrameBase;
                if (faceRoot != null)
                {
                    faceRoot.localRotation = Quaternion.identity;
                    faceRoot.localScale = Vector3.one;
                }
            }
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
