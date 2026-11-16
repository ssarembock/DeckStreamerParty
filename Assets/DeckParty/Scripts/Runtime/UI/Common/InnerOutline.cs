using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// An inner outline (and optional inner glow) along the edge of its rect, with rounded corners, drawn by the
    /// "DeckParty/UI/Inner Outline" shader. Put it on an object that covers the card (or its frame), above the card's
    /// content. Every setting is in the inspector and shows live while you edit the prefab. On a shop card set to
    /// <see cref="ShowWhen.ViewerHasAction"/>, it lights up (fading in) only on viewers that have an action.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("Deck Party/Inner Outline")]
    public sealed class InnerOutline : MaskableGraphic
    {
        public const string ShaderName = "DeckParty/UI/Inner Outline";

        public enum ShowWhen
        {
            /// <summary>Always lit.</summary>
            Always,
            /// <summary>Shop cards: lit only when the viewer has an action.</summary>
            ViewerHasAction,
        }

        [Header("When it shows")]
        [Tooltip("Always, or (on a shop card) only when the viewer has an action.")]
        public ShowWhen showWhen = ShowWhen.ViewerHasAction;
        [Tooltip("Keep it lit while you edit the prefab, so you can see your changes.")]
        public bool previewInEditor = true;
        [Tooltip("Seconds it takes to fade in or out.")]
        [Min(0f)] public float fadeTime = 0.2f;

        [Header("Outline")]
        [Tooltip("The line's colour. HDR: push the intensity above 1 for a brighter line.")]
        [ColorUsage(true, true)] public Color outlineColour = new Color(0.54f, 0.31f, 1f, 1f);
        [Tooltip("How thick the line is, in card pixels.")]
        [Min(0f)] public float width = 4f;
        [Tooltip("How far inside the edge the line starts, in card pixels (for example the frame's border width).")]
        [Min(0f)] public float inset = 3f;
        [Tooltip("The roundness of the corners, in card pixels. Match it to your frame art.")]
        [Min(0f)] public float cornerRadius = 12f;
        [Tooltip("0: a crisp line. 1: the line fades out across its whole width.")]
        [Range(0f, 1f)] public float softness = 0.4f;
        [Tooltip("Overall opacity of the effect.")]
        [Range(0f, 1f)] public float opacity = 1f;

        [Header("Inner glow")]
        [Tooltip("How far the soft glow reaches into the card, in card pixels. 0: no glow.")]
        [Min(0f)] public float glowWidth = 14f;
        [Tooltip("How bright the glow is at the edge.")]
        [Range(0f, 1f)] public float glowStrength = 0.35f;

        [Header("Pulse")]
        [Tooltip("Pulses per second. 0: steady.")]
        [Min(0f)] public float pulseSpeed = 0.8f;
        [Tooltip("How much it dims at the bottom of each pulse. 0: no pulse.")]
        [Range(0f, 1f)] public float pulseAmount = 0.35f;

        [Header("Rotating colours")]
        [Tooltip("The outline fades between Outline Colour and Second Colour in a wave that travels round the border.")]
        public bool rotatingColours = true;
        [Tooltip("The colour the wave fades to. HDR: push the intensity above 1 for a brighter colour.")]
        [ColorUsage(true, true)] public Color secondColour = new Color(0.2f, 0.9f, 1f, 1f);
        [Tooltip("Turns round the border per second. Negative: the other way. 0: still.")]
        public float rotationSpeed = 0.25f;
        [Tooltip("How many times the two colours repeat round the border.")]
        [Range(1, 8)] public int colourRepeats = 2;
        [Tooltip("0: one long smooth fade between the colours. 1: almost hard bands of colour.")]
        [Range(0f, 1f)] public float colourSharpness = 0.2f;

        [Header("Border (clear centre)")]
        [Tooltip("How far in from the edge the effect reaches, in card pixels (measured from where the line starts, after " +
                 "the Inset). Everything closer to the centre stays clear, so the effect only shows along the border. This is " +
                 "the size of the clear centre's edge all the way round. 0: no limit.")]
        [Min(0f)] public float borderSize = 18f;
        [Tooltip("How softly the effect fades out at the border's inner edge, in card pixels. 0: a hard edge.")]
        [Min(0f)] public float borderFade = 6f;

        [SerializeField, HideInInspector] Shader m_Shader;

        static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        static readonly int RectSizeId = Shader.PropertyToID("_RectSize");
        static readonly int WidthId = Shader.PropertyToID("_Width");
        static readonly int InsetId = Shader.PropertyToID("_Inset");
        static readonly int RadiusId = Shader.PropertyToID("_Radius");
        static readonly int SoftnessId = Shader.PropertyToID("_Softness");
        static readonly int GlowWidthId = Shader.PropertyToID("_GlowWidth");
        static readonly int GlowStrengthId = Shader.PropertyToID("_GlowStrength");
        static readonly int PulseSpeedId = Shader.PropertyToID("_PulseSpeed");
        static readonly int PulseAmountId = Shader.PropertyToID("_PulseAmount");
        static readonly int StrengthId = Shader.PropertyToID("_Strength");
        static readonly int OutlineColor2Id = Shader.PropertyToID("_OutlineColor2");
        static readonly int ColorMixId = Shader.PropertyToID("_ColorMix");
        static readonly int RotationSpeedId = Shader.PropertyToID("_RotationSpeed");
        static readonly int ColorRepeatsId = Shader.PropertyToID("_ColorRepeats");
        static readonly int ColorSharpnessId = Shader.PropertyToID("_ColorSharpness");
        static readonly int BorderSizeId = Shader.PropertyToID("_BorderSize");
        static readonly int BorderFadeId = Shader.PropertyToID("_BorderFade");

        Material m_Instance;
        bool m_Wanted;
        float m_Shown;

        /// <summary>The shader asset (kept on the component so builds include it).</summary>
        public Shader OutlineShader
        {
            get => m_Shader;
            set
            {
                m_Shader = value;
                if (m_Instance != null) m_Instance.shader = value;
                SetMaterialDirty();
            }
        }

        /// <summary>Lights the outline (or turns it off), fading unless <paramref name="instant"/>.</summary>
        public void SetShown(bool shown, bool instant = false)
        {
            m_Wanted = shown;
            if (!instant) return;
            m_Shown = Lit() ? 1f : 0f;
            Push();
        }

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
            // Every UI graphic draws through a CanvasRenderer. Outlines made before this was required have none, and the
            // editor would then hand the UI code a "missing component" stand-in instead of adding one: add it here.
            if (!TryGetComponent<CanvasRenderer>(out _)) gameObject.AddComponent<CanvasRenderer>();
            base.Awake();
        }

        protected override void OnEnable()
        {
            if (!TryGetComponent<CanvasRenderer>(out _)) gameObject.AddComponent<CanvasRenderer>();
            base.OnEnable();
            m_Shown = Lit() ? 1f : 0f;
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
            float target = Lit() ? 1f : 0f;
            if (Mathf.Approximately(m_Shown, target)) return;
            float step = fadeTime > 0f ? Time.unscaledDeltaTime / fadeTime : 1f;
            m_Shown = Mathf.MoveTowards(m_Shown, target, step);
            if (m_Instance != null) m_Instance.SetFloat(StrengthId, m_Shown * opacity);
        }

        /// <summary>Whether it should be lit right now.</summary>
        bool Lit()
        {
            if (showWhen == ShowWhen.Always) return true;
#if UNITY_EDITOR
            if (!Application.isPlaying) return previewInEditor;
#endif
            return m_Wanted;
        }

        Material Instance()
        {
            if (m_Instance != null) return m_Instance;
            var shader = m_Shader != null ? m_Shader : Shader.Find(ShaderName);
            if (shader == null) return null;
            m_Instance = new Material(shader) { name = "Inner Outline (instance)", hideFlags = HideFlags.HideAndDontSave };
            Push();
            return m_Instance;
        }

        /// <summary>Sends the inspector's settings to the shader.</summary>
        void Push()
        {
            if (m_Instance == null) return;
            var size = rectTransform.rect.size;
            m_Instance.SetVector(RectSizeId, new Vector4(Mathf.Max(1f, size.x), Mathf.Max(1f, size.y), 0f, 0f));
            m_Instance.SetColor(OutlineColorId, outlineColour);
            m_Instance.SetFloat(WidthId, width);
            m_Instance.SetFloat(InsetId, inset);
            m_Instance.SetFloat(RadiusId, cornerRadius);
            m_Instance.SetFloat(SoftnessId, softness);
            m_Instance.SetFloat(GlowWidthId, glowWidth);
            m_Instance.SetFloat(GlowStrengthId, glowStrength);
            m_Instance.SetFloat(PulseSpeedId, pulseSpeed);
            m_Instance.SetFloat(PulseAmountId, pulseAmount);
            m_Instance.SetColor(OutlineColor2Id, secondColour);
            m_Instance.SetFloat(ColorMixId, rotatingColours ? 1f : 0f);
            m_Instance.SetFloat(RotationSpeedId, rotationSpeed);
            m_Instance.SetFloat(ColorRepeatsId, Mathf.Max(1, colourRepeats));
            m_Instance.SetFloat(ColorSharpnessId, colourSharpness);
            m_Instance.SetFloat(BorderSizeId, borderSize);
            m_Instance.SetFloat(BorderFadeId, Mathf.Min(borderFade, borderSize));
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
