using DeckParty.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty
{
    /// <summary>Transient Sprite Shaders Ultimate effects a card can flash (see <see cref="CardFx.Pulse"/>).</summary>
    public enum CardPulse
    {
        Glitch,
        Burn,
        GlowIn,
        Shine,
        Pixelate,
        Frozen,
        Hologram,
        Poison,
        Vibrate,
    }

    /// <summary>
    /// Ongoing status visuals for one card portrait (open information at a glance): each card gets its own instance of
    /// the FX Library's Sprite Shaders Ultimate "uber" material, whose effects all exist but start at Fade 0. Statuses
    /// and pulses ease the Fade properties in and out. Does nothing when SSU (or the material) is missing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CardFx : MonoBehaviour
    {
        // SSU GUI shader properties (every effect has its own Fade).
        static readonly int RectWidth = Shader.PropertyToID("_RectWidth");
        static readonly int RectHeight = Shader.PropertyToID("_RectHeight");
        static readonly int OutlineFade = Shader.PropertyToID("_OuterOutlineFade");
        static readonly int OutlineColor = Shader.PropertyToID("_OuterOutlineColor");
        static readonly int OutlineWidth = Shader.PropertyToID("_OuterOutlineWidth");
        static readonly int Saturation = Shader.PropertyToID("_Saturation");
        static readonly int SineGlowFade = Shader.PropertyToID("_SineGlowFade");
        static readonly int SineGlowColor = Shader.PropertyToID("_SineGlowColor");
        static readonly int HologramFade = Shader.PropertyToID("_HologramFade");
        static readonly int GlitchFade = Shader.PropertyToID("_GlitchFade");
        static readonly int FrozenFade = Shader.PropertyToID("_FrozenFade");
        static readonly int ShineFade = Shader.PropertyToID("_ShineFade");
        static readonly int ShineColor = Shader.PropertyToID("_ShineColor");
        static readonly int BurnFade = Shader.PropertyToID("_BurnFade");
        static readonly int PoisonFade = Shader.PropertyToID("_PoisonFade");
        static readonly int HalftoneFade = Shader.PropertyToID("_HalftoneFade");
        static readonly int AddHueFade = Shader.PropertyToID("_AddHueFade");
        static readonly int GlowDissolveFade = Shader.PropertyToID("_FullGlowDissolveFade");
        static readonly int PixelateFade = Shader.PropertyToID("_PixelateFade");
        static readonly int VibrateFade = Shader.PropertyToID("_VibrateFade");

        /// <summary>Every effect fade the card drives, reset to 0 on the instance so only active statuses show.</summary>
        public static readonly int[] AllFades =
        {
            OutlineFade, SineGlowFade, HologramFade, GlitchFade, FrozenFade, ShineFade, BurnFade, PoisonFade,
            AddHueFade, PixelateFade, VibrateFade,
        };

        // Halftone and Full Glow Dissolve are SSU "fading" effects: their Fade is how much of the sprite remains, so 0
        // hides the portrait. Halftone measures in sprite-sheet units (texture pixels / Pixels Per Unit), so it's no longer
        // used; if an older material still has it switched on, this Fade keeps every pixel of any sheet visible.
        public const float HalftoneClear = 100000f;

        const float EaseSpeed = 5f;

        Graphic m_Graphic;
        Material m_Instance;
        RectTransform m_Rect;
        Vector2 m_LastSize;

        // Current and target values per channel.
        float m_Outline, m_OutlineTarget;
        Color m_OutlineColor = Color.clear;
        float m_Glow, m_GlowTarget;
        Color m_GlowColor = Color.clear;
        float m_Saturation = 1f, m_SaturationTarget = 1f;
        float m_Holo, m_HoloTarget;
        float m_Frozen, m_FrozenTarget;
        float m_Shine, m_ShineTarget;
        Color m_ShineColor = Color.white;
        float m_Poison, m_PoisonTarget;
        float m_Hue, m_HueTarget;
        bool m_RandomGlitch;
        float m_NextGlitch;

        // Transient pulses (0-1, decaying).
        float m_Glitch, m_Burn, m_GlowIn, m_ShinePulse, m_Pixelate, m_FrozenPulse, m_HoloPulse, m_PoisonPulse, m_Vibrate;

        public bool Active => m_Instance != null;

        void Awake()
        {
            m_Graphic = GetComponent<Graphic>();
            m_Rect = transform as RectTransform;
            var source = FxLibrary.Current.portraitUber;
            if (m_Graphic == null || source == null || !FxLibrary.Current.statusEffects) return;
            m_Instance = new Material(source) { name = source.name + " (card)" };
            foreach (var id in AllFades)
                if (m_Instance.HasProperty(id)) m_Instance.SetFloat(id, 0f);
            if (m_Instance.HasProperty(HalftoneFade)) m_Instance.SetFloat(HalftoneFade, HalftoneClear);
            if (m_Instance.HasProperty(GlowDissolveFade)) m_Instance.SetFloat(GlowDissolveFade, 1f);
            m_Graphic.material = m_Instance;
        }

        void OnDestroy()
        {
            if (m_Instance != null) Destroy(m_Instance);
        }

        /// <summary>Clears every status (empty slot or a card shown out of context).</summary>
        public void Clear()
        {
            m_OutlineTarget = m_GlowTarget = m_HoloTarget = m_FrozenTarget = m_ShineTarget = m_PoisonTarget = m_HueTarget = 0f;
            m_SaturationTarget = 1f;
            m_RandomGlitch = false;
        }

        /// <summary>Works out tonight's look for a viewer in chat (or in the Audience view when <paramref name="live"/> is false).</summary>
        public void SetStatus(PlayerState s, GuestInstance g, bool live, bool selectable, bool selected, bool banned)
        {
            if (!Active) return;
            Clear();
            if (g == null) return;
            var info = live ? g.Info : g.BaseInfo;

            // Outline: selection beats Swatting Risk.
            if (selected) SetOutline(UIPalette.Pink, 1f);
            else if (selectable) SetOutline(UIPalette.Gold, 0.85f);
            else if (live && PartyEngine.HasTroubleProperty(s, g)) SetOutline(UIPalette.Red, 0.55f);
            else if (live && g.TroubleCleared && info.Trouble) SetOutline(UIPalette.Teal, 0.55f);

            // Glow: Pepe's mood > attached > an action still to use.
            if (live && g.Mood != PepeMood.None)
                SetGlow(g.Mood == PepeMood.Happy ? UIPalette.Gold : g.Mood == PepeMood.Sad ? UIPalette.Blue : UIPalette.Red, 0.8f);
            else if (live && (PartyEngine.IsProtected(s, g) || PartyEngine.AttachedPartner(s, g) != null))
                SetGlow(UIPalette.Pink, 0.75f);
            else if (live && info.HasAbility && s.Phase == GamePhase.Party && !s.UsedAbilities.Contains(g.Id))
                SetGlow(UIPalette.Purple, 0.45f);

            if (live)
            {
                if (g.Infected) m_PoisonTarget = 0.85f;
                if (g.Immune) m_FrozenTarget = 0.7f;
                if (g.Zeroed) m_SaturationTarget = 0f;
                if (g.DoubleMode != 0) SetShine(UIPalette.Gold, 0.9f);
                if (g.ConvertMode != 0) m_HueTarget = 0.6f;
                if (g.Temporary) m_HoloTarget = 0.75f;
                m_RandomGlitch = g.IsDisguised;
            }
            if (info.IsDiamond && m_ShineTarget <= 0f) SetShine(UIPalette.Gold, 0.55f);
            // Temporarily banned: drained of colour and flickering like a dead feed.
            if (banned)
            {
                m_SaturationTarget = 0.1f;
                m_HoloTarget = Mathf.Max(m_HoloTarget, 0.5f);
            }
            if (AppSettings.ReduceFlashing) m_RandomGlitch = false;
        }

        void SetOutline(Color color, float amount)
        {
            m_OutlineColor = color;
            m_OutlineTarget = amount;
        }

        void SetGlow(Color color, float amount)
        {
            m_GlowColor = color;
            m_GlowTarget = amount;
        }

        void SetShine(Color color, float amount)
        {
            m_ShineColor = color;
            m_ShineTarget = amount;
        }

        /// <summary>Flashes a transient effect (a kick burns, a Rickroll glitches, an arrival glows in).</summary>
        public void Pulse(CardPulse pulse, float strength = 1f)
        {
            if (!Active) return;
            if (AppSettings.ReduceFlashing && (pulse == CardPulse.Glitch || pulse == CardPulse.Pixelate || pulse == CardPulse.Vibrate)) strength *= 0.3f;
            switch (pulse)
            {
                case CardPulse.Glitch: m_Glitch = Mathf.Max(m_Glitch, strength); break;
                case CardPulse.Burn: m_Burn = Mathf.Max(m_Burn, strength); break;
                case CardPulse.GlowIn: m_GlowIn = Mathf.Max(m_GlowIn, strength); break;
                case CardPulse.Shine: m_ShinePulse = Mathf.Max(m_ShinePulse, strength); break;
                case CardPulse.Pixelate: m_Pixelate = Mathf.Max(m_Pixelate, strength); break;
                case CardPulse.Frozen: m_FrozenPulse = Mathf.Max(m_FrozenPulse, strength); break;
                case CardPulse.Hologram: m_HoloPulse = Mathf.Max(m_HoloPulse, strength); break;
                case CardPulse.Poison: m_PoisonPulse = Mathf.Max(m_PoisonPulse, strength); break;
                case CardPulse.Vibrate: m_Vibrate = Mathf.Max(m_Vibrate, strength); break;
            }
        }

        void Update()
        {
            if (!Active) return;
            float dt = Time.unscaledDeltaTime;
            float k = 1f - Mathf.Exp(-EaseSpeed * dt / Mathf.Max(0.2f, Fx.DurationScale));

            m_Outline = Mathf.Lerp(m_Outline, m_OutlineTarget, k);
            m_Glow = Mathf.Lerp(m_Glow, m_GlowTarget, k);
            m_Saturation = Mathf.Lerp(m_Saturation, m_SaturationTarget, k);
            m_Holo = Mathf.Lerp(m_Holo, m_HoloTarget, k);
            m_Frozen = Mathf.Lerp(m_Frozen, m_FrozenTarget, k);
            m_Shine = Mathf.Lerp(m_Shine, m_ShineTarget, k);
            m_Poison = Mathf.Lerp(m_Poison, m_PoisonTarget, k);
            m_Hue = Mathf.Lerp(m_Hue, m_HueTarget, k);

            float decay = dt * 2.2f / Mathf.Max(0.2f, Fx.DurationScale);
            m_Glitch = Mathf.Max(0f, m_Glitch - decay);
            m_Burn = Mathf.Max(0f, m_Burn - decay);
            m_GlowIn = Mathf.Max(0f, m_GlowIn - decay);
            m_ShinePulse = Mathf.Max(0f, m_ShinePulse - decay * 0.7f);
            m_Pixelate = Mathf.Max(0f, m_Pixelate - decay);
            m_FrozenPulse = Mathf.Max(0f, m_FrozenPulse - decay * 0.6f);
            m_HoloPulse = Mathf.Max(0f, m_HoloPulse - decay * 0.6f);
            m_PoisonPulse = Mathf.Max(0f, m_PoisonPulse - decay * 0.6f);
            m_Vibrate = Mathf.Max(0f, m_Vibrate - decay);

            if (m_RandomGlitch && Time.unscaledTime >= m_NextGlitch)
            {
                m_Glitch = Mathf.Max(m_Glitch, 0.6f);
                m_NextGlitch = Time.unscaledTime + Random.Range(1.8f, 4f);
            }

            var m = m_Instance;
            if (m_Rect != null && m_Rect.rect.size != m_LastSize)
            {
                m_LastSize = m_Rect.rect.size;
                m.SetFloat(RectWidth, m_LastSize.x);
                m.SetFloat(RectHeight, m_LastSize.y);
            }

            // Selected / selectable outlines breathe so they read as interactive.
            float outlinePulse = m_OutlineTarget >= 0.8f ? 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 6f) : 1f;
            m.SetFloat(OutlineFade, m_Outline * outlinePulse);
            m.SetColor(OutlineColor, m_OutlineColor * 2f);
            m.SetFloat(OutlineWidth, 0.02f + 0.02f * m_Outline);
            m.SetFloat(SineGlowFade, m_Glow);
            m.SetColor(SineGlowColor, m_GlowColor * 2f);
            m.SetFloat(Saturation, m_Saturation);
            m.SetFloat(HologramFade, Mathf.Max(m_Holo, m_HoloPulse));
            m.SetFloat(GlitchFade, m_Glitch);
            m.SetFloat(FrozenFade, Mathf.Max(m_Frozen, m_FrozenPulse));
            m.SetFloat(ShineFade, Mathf.Max(m_Shine, m_ShinePulse));
            m.SetColor(ShineColor, m_ShineColor * 2.5f);
            m.SetFloat(BurnFade, m_Burn);
            m.SetFloat(PoisonFade, Mathf.Max(m_Poison, m_PoisonPulse));
            m.SetFloat(AddHueFade, m_Hue);
            m.SetFloat(PixelateFade, m_Pixelate);
            m.SetFloat(VibrateFade, m_Vibrate);
            // Glow-in on arrival: a dissolve that resolves to fully visible.
            m.SetFloat(GlowDissolveFade, 1f - m_GlowIn);
        }
    }
}
