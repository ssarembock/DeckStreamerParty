using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// A swipe of colour across its rect, drawn by the "DeckParty/UI/Colour Swipe" shader: a band of colour that crosses the
    /// card (a shine), or a wipe that fills the card with colour, holds and fades. Put it on an object that covers the card
    /// (or just the part you want swiped) above the content. You choose which cards it's on (Always, or only viewers with an
    /// action) and when it plays (on a loop, once when it appears, or when the card is hovered or highlighted with the
    /// controller). Every setting is in the inspector and loops live while you edit the prefab.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("Deck Party/Colour Swipe")]
    public sealed class ColourSwipe : MaskableGraphic, IActionEffect
    {
        public const string ShaderName = "DeckParty/UI/Colour Swipe";

        public enum ShowWhen
        {
            /// <summary>On every card.</summary>
            Always,
            /// <summary>Shop cards: only when the viewer has an action.</summary>
            ViewerHasAction,
        }

        public enum PlayMode
        {
            /// <summary>Swipes again and again, with Repeat Delay between swipes.</summary>
            Loop,
            /// <summary>One swipe each time it appears (a card showing a viewer it's on).</summary>
            OnceWhenShown,
            /// <summary>One swipe each time the pointer (or the controller selector) moves onto the card.</summary>
            OnHover,
            /// <summary>Swipes on a loop while the pointer (or the controller selector) is on the card.</summary>
            LoopWhileHovered,
        }

        public enum Style
        {
            /// <summary>A band of colour crosses the card.</summary>
            Band,
            /// <summary>The colour sweeps across and fills the card, holds, then fades out.</summary>
            Wipe,
        }

        [Header("When it plays")]
        [Tooltip("Always, or (on a shop card) only on viewers that have an action.")]
        public ShowWhen showWhen = ShowWhen.ViewerHasAction;
        [Tooltip("Loop: again and again. Once When Shown: one swipe when the card shows a viewer it's on. On Hover: one swipe " +
                 "each time the pointer or the controller selector moves onto the card. Loop While Hovered: loops while it's on the card.")]
        public PlayMode playMode = PlayMode.Loop;
        [Tooltip("Seconds one swipe takes to cross the card. Bigger is slower.")]
        [Min(0.05f)] public float swipeTime = 0.8f;
        [Tooltip("Loops: seconds of rest between one swipe ending and the next starting.")]
        [Min(0f)] public float repeatDelay = 2.5f;
        [Tooltip("Seconds to wait before the swipe starts (Once When Shown and On Hover).")]
        [Min(0f)] public float startDelay = 0f;
        [Tooltip("Loop: start each card at a different moment, so the cards in the shop don't all swipe together.")]
        public bool staggerCards = true;
        [Tooltip("Keep it looping while you edit the prefab, so you can see your changes (whatever the play mode).")]
        public bool previewInEditor = true;
        [Tooltip("Seconds it takes to fade in or out when it turns on or off.")]
        [Min(0f)] public float fadeTime = 0.2f;

        [Header("Look")]
        [Tooltip("Band: a stripe of colour crosses the card. Wipe: the colour sweeps across and fills the card, holds, then fades.")]
        public Style style = Style.Band;
        [Tooltip("The swipe's colour (its alpha is how strong it is). HDR: push the intensity above 1 for a brighter swipe.")]
        [ColorUsage(true, true)] public Color colour = new Color(0.54f, 0.31f, 1f, 0.45f);
        [Tooltip("Blend from Second Colour (the band's trailing edge, or the side a wipe starts from) to Colour.")]
        public bool useSecondColour;
        [ColorUsage(true, true)] public Color secondColour = new Color(0.2f, 0.9f, 1f, 0.45f);
        [Tooltip("On: the swipe adds light (a shine; good on dark and coloured cards). Off: it paints its colour over the card.")]
        public bool addLight;
        [Tooltip("The direction it travels, in degrees: 0 left to right, 90 bottom to top, 180 right to left, -90 top to bottom.")]
        [Range(-180f, 180f)] public float angle = 30f;
        [Tooltip("Band: how wide the stripe is. Wipe: how soft the moving front is. As a share of the card (0.25 = a quarter).")]
        [Range(0.01f, 1f)] public float bandWidth = 0.25f;
        [Tooltip("Band only. 0: hard-edged stripe. 1: fades out from its centre.")]
        [Range(0f, 1f)] public float softness = 0.6f;
        [Tooltip("Speed up at the start and slow down at the end of each swipe (off: constant speed).")]
        public bool easeInOut = true;
        [Tooltip("Wipe only: seconds the colour stays on the whole card before it fades.")]
        [Min(0f)] public float wipeHold = 0.3f;
        [Tooltip("Wipe only: seconds it takes to fade out after the hold.")]
        [Min(0f)] public float wipeFadeOut = 0.4f;
        [Tooltip("Overall opacity of the effect.")]
        [Range(0f, 1f)] public float opacity = 1f;

        [Header("Shape")]
        [Tooltip("How far inside the edge the swipe stops, in card pixels (for example the frame's border width).")]
        [Min(0f)] public float inset = 3f;
        [Tooltip("The roundness of the corners, in card pixels. Match it to your frame art so the swipe stays on the card.")]
        [Min(0f)] public float cornerRadius = 12f;

        [SerializeField, HideInInspector] Shader m_Shader;

        static readonly int SwipeColorId = Shader.PropertyToID("_SwipeColor");
        static readonly int SwipeColor2Id = Shader.PropertyToID("_SwipeColor2");
        static readonly int GradientId = Shader.PropertyToID("_Gradient");
        static readonly int RectSizeId = Shader.PropertyToID("_RectSize");
        static readonly int InsetId = Shader.PropertyToID("_Inset");
        static readonly int RadiusId = Shader.PropertyToID("_Radius");
        static readonly int AngleId = Shader.PropertyToID("_Angle");
        static readonly int BandWidthId = Shader.PropertyToID("_BandWidth");
        static readonly int SoftnessId = Shader.PropertyToID("_Softness");
        static readonly int StyleId = Shader.PropertyToID("_Style");
        static readonly int AdditiveId = Shader.PropertyToID("_Additive");
        static readonly int EaseId = Shader.PropertyToID("_Ease");
        static readonly int SwipeTimeId = Shader.PropertyToID("_SwipeTime");
        static readonly int RepeatDelayId = Shader.PropertyToID("_RepeatDelay");
        static readonly int HoldId = Shader.PropertyToID("_Hold");
        static readonly int FadeOutId = Shader.PropertyToID("_FadeOut");
        static readonly int OffsetId = Shader.PropertyToID("_Offset");
        static readonly int DrivenId = Shader.PropertyToID("_Driven");
        static readonly int LocalTimeId = Shader.PropertyToID("_LocalTime");
        static readonly int StrengthId = Shader.PropertyToID("_Strength");

        Material m_Instance;
        bool m_Wanted;
        float m_Shown;
        float m_Offset;
        // Driven play (everything but Loop): when the current swipe started (unscaled time), and whether one is running.
        float m_PlayStart;
        bool m_Playing;
        bool m_WasLit;
        bool m_Hovered;

        /// <summary>The shader asset (kept on the component so builds include it).</summary>
        public Shader SwipeShader
        {
            get => m_Shader;
            set
            {
                m_Shader = value;
                if (m_Instance != null) m_Instance.shader = value;
                SetMaterialDirty();
            }
        }

        /// <summary>Shop cards: the viewer has an action or not (used when Show When is Viewer Has Action).</summary>
        public void SetViewerHasAction(bool hasAction, bool instant)
        {
            if (showWhen != ShowWhen.ViewerHasAction) return;
            m_Wanted = hasAction;
            if (!instant) return;
            m_Shown = Lit() ? 1f : 0f;
            Push();
        }

        /// <summary>Starts one swipe now (any play mode but Loop; for code that wants a swipe on its own cue).</summary>
        public void Play()
        {
            m_PlayStart = Time.unscaledTime + startDelay;
            m_Playing = true;
        }

        /// <summary>Seconds one whole swipe lasts (a wipe's hold and fade included).</summary>
        float ActiveTime => swipeTime + (style == Style.Wipe ? wipeHold + wipeFadeOut : 0f);

        public override Material material
        {
            get
            {
                var instance = Instance();
                return instance != null ? instance : base.material;
            }
            set => base.material = value;
        }

        protected override void Awake()
        {
            if (!TryGetComponent<CanvasRenderer>(out _)) gameObject.AddComponent<CanvasRenderer>();
            base.Awake();
        }

        protected override void OnEnable()
        {
            if (!TryGetComponent<CanvasRenderer>(out _)) gameObject.AddComponent<CanvasRenderer>();
            base.OnEnable();
            m_Offset = staggerCards && Application.isPlaying ? Random.Range(0f, 30f) : 0f;
            m_Shown = Lit() ? 1f : 0f;
            m_WasLit = false;
            m_Playing = false;
            m_Hovered = false;
            Push();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (m_Instance == null) return;
            if (Application.isPlaying) Destroy(m_Instance);
            else DestroyImmediate(m_Instance);
            m_Instance = null;
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            Push();
        }

        void Update()
        {
            bool lit = Lit();
            float target = lit ? 1f : 0f;
            if (!Mathf.Approximately(m_Shown, target))
            {
                float step = fadeTime > 0f ? Time.unscaledDeltaTime / fadeTime : 1f;
                m_Shown = Mathf.MoveTowards(m_Shown, target, step);
            }
            if (m_Instance == null) return;
            m_Instance.SetFloat(StrengthId, m_Shown * opacity);
            if (!Driven()) return;

            // Once When Shown: a swipe each time it turns on.
            if (playMode == PlayMode.OnceWhenShown && lit && !m_WasLit) Play();
            m_WasLit = lit;

            // Hover: the pointer, or the controller selector (it moves the pointer position), is over this rect.
            if (playMode == PlayMode.OnHover || playMode == PlayMode.LoopWhileHovered)
            {
                bool hovered = lit && IsHovered();
                if (hovered && !m_Hovered) Play();
                m_Hovered = hovered;
            }

            float local = m_Playing ? Time.unscaledTime - m_PlayStart : -1f;
            if (m_Playing && local >= ActiveTime)
            {
                if (playMode == PlayMode.LoopWhileHovered && m_Hovered && local >= ActiveTime + repeatDelay)
                {
                    m_PlayStart += ActiveTime + repeatDelay;
                    local = Time.unscaledTime - m_PlayStart;
                }
                else if (playMode != PlayMode.LoopWhileHovered || !m_Hovered)
                {
                    m_Playing = false;
                    local = -1f;
                }
            }
            m_Instance.SetFloat(LocalTimeId, local);
        }

        bool IsHovered()
        {
            var root = canvas != null ? canvas.rootCanvas : null;
            var camera = root != null && root.renderMode != RenderMode.ScreenSpaceOverlay ? root.worldCamera : null;
            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, InputHelper.PointerPosition, camera);
        }

        /// <summary>The component drives the timing (every play mode but Loop, outside the editor preview).</summary>
        bool Driven()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return false;
#endif
            return playMode != PlayMode.Loop;
        }

        /// <summary>Whether it should be on right now.</summary>
        bool Lit()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return previewInEditor;
#endif
            return showWhen == ShowWhen.Always || m_Wanted;
        }

        Material Instance()
        {
            if (m_Instance != null) return m_Instance;
            var shader = m_Shader != null ? m_Shader : Shader.Find(ShaderName);
            if (shader == null) return null;
            m_Instance = new Material(shader) { name = "Colour Swipe (instance)", hideFlags = HideFlags.HideAndDontSave };
            Push();
            return m_Instance;
        }

        /// <summary>Sends the inspector's settings to the shader.</summary>
        void Push()
        {
            if (m_Instance == null) return;
            var size = rectTransform.rect.size;
            m_Instance.SetVector(RectSizeId, new Vector4(Mathf.Max(1f, size.x), Mathf.Max(1f, size.y), 0f, 0f));
            m_Instance.SetColor(SwipeColorId, colour);
            m_Instance.SetColor(SwipeColor2Id, secondColour);
            m_Instance.SetFloat(GradientId, useSecondColour ? 1f : 0f);
            m_Instance.SetFloat(InsetId, inset);
            m_Instance.SetFloat(RadiusId, cornerRadius);
            m_Instance.SetFloat(AngleId, angle);
            m_Instance.SetFloat(BandWidthId, bandWidth);
            m_Instance.SetFloat(SoftnessId, softness);
            m_Instance.SetFloat(StyleId, style == Style.Wipe ? 1f : 0f);
            m_Instance.SetFloat(AdditiveId, addLight ? 1f : 0f);
            m_Instance.SetFloat(EaseId, easeInOut ? 1f : 0f);
            m_Instance.SetFloat(SwipeTimeId, swipeTime);
            m_Instance.SetFloat(RepeatDelayId, repeatDelay);
            m_Instance.SetFloat(HoldId, wipeHold);
            m_Instance.SetFloat(FadeOutId, wipeFadeOut);
            m_Instance.SetFloat(OffsetId, m_Offset);
            m_Instance.SetFloat(DrivenId, Driven() ? 1f : 0f);
            m_Instance.SetFloat(LocalTimeId, Driven() && m_Playing ? Time.unscaledTime - m_PlayStart : -1f);
            m_Instance.SetFloat(StrengthId, m_Shown * opacity);
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            raycastTarget = false;
            if (m_Shader == null) m_Shader = Shader.Find(ShaderName);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            if (m_Shader == null) m_Shader = Shader.Find(ShaderName);
            if (m_Instance != null && m_Shader != null && m_Instance.shader != m_Shader) m_Instance.shader = m_Shader;
            m_Shown = Lit() ? 1f : 0f;
            Push();
        }
#endif
    }
}
