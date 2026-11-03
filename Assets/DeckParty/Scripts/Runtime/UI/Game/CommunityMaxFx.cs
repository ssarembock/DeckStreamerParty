using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// The "maxed out" look of a Community Beats icon, while its community's score is at its maximum (5/5, 9/9...):
    ///  - a gold halo behind the icon that breathes,
    ///  - a slowly turning sunburst of rays behind that,
    ///  - the icon itself pulsing gently, with a Sprite Shaders Ultimate outline, glow and travelling shine on it (when the
    ///    FX Library's portrait uber material is set up; the halo and rays work without it),
    ///  - a pop, a burst and a chime the moment the maximum is reached.
    /// Reduce Flashing keeps the halo and rays but calms the pulsing.
    /// </summary>
    public sealed class CommunityMaxFx : MonoBehaviour
    {
        [Tooltip("The community icon (pulses, and gets the shader highlight).")]
        [SerializeField] internal Image icon;
        [Tooltip("Soft glow behind the icon.")]
        [SerializeField] internal Image halo;
        [Tooltip("Sunburst behind the halo.")]
        [SerializeField] internal Image rays;
        [Tooltip("What pops when the maximum is reached (the whole chip).")]
        [SerializeField] internal RectTransform punchTarget;
        [SerializeField] internal Color haloColor = new Color(1f, 0.79f, 0.29f, 1f);
        [SerializeField] internal float raysSpeed = 40f;

        bool m_On;
        float m_Weight;
        float m_Burst;
        bool m_Idle;
        CardFx m_Shader;

        public bool Maxed => m_On;

        void Awake()
        {
            if (halo != null && halo.sprite == null) halo.sprite = RuntimeSprites.Glow;
            if (rays != null && rays.sprite == null) rays.sprite = RuntimeSprites.Rays;
            Apply(0f);
        }

        void OnDisable()
        {
            m_Weight = 0f;
            m_Burst = 0f;
            Apply(0f);
            if (icon != null) icon.rectTransform.localScale = Vector3.one;
        }

        /// <param name="on">The community's score is at its maximum.</param>
        /// <param name="color">The community's colour (tints the shader glow).</param>
        public void SetMaxed(bool on, Color color)
        {
            if (on == m_On) return;
            m_On = on;
            m_Idle = false;
            if (on)
            {
                m_Burst = 1f;
                if (isActiveAndEnabled)
                {
                    UITween.Punch(this, punchTarget != null ? punchTarget : (RectTransform)transform, 0.3f, 0.4f);
                    AudioService.PlayPitched(Sfx.StarArrive, 1.25f, 0.6f);
                }
                if (icon != null && m_Shader == null) m_Shader = icon.gameObject.AddComponent<CardFx>();
                if (m_Shader != null) m_Shader.SetHighlight(true, Color.Lerp(haloColor, color, 0.35f));
            }
            else
            {
                RemoveShader();
            }
        }

        void RemoveShader()
        {
            if (m_Shader == null) return;
            Destroy(m_Shader);
            m_Shader = null;
            // CardFx gave the icon its own material instance; go back to the default UI material.
            if (icon != null) icon.material = null;
        }

        void OnDestroy() => RemoveShader();

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            m_Weight = Mathf.MoveTowards(m_Weight, m_On ? 1f : 0f, dt * 4f);
            m_Burst = Mathf.MoveTowards(m_Burst, 0f, dt * 2.2f);
            bool idle = m_Weight <= 0f && m_Burst <= 0f;
            // One last Apply at zero hides the halo and rays; after that there's nothing to do.
            if (idle && m_Idle) return;
            m_Idle = idle;
            Apply(m_Weight);
        }

        void Apply(float weight)
        {
            float t = Time.unscaledTime;
            float calm = AppSettings.ReduceFlashing ? 0.35f : 1f;
            float pulse = Mathf.Sin(t * 3.2f);

            if (halo != null)
            {
                halo.enabled = weight > 0f;
                halo.color = haloColor.WithAlpha(weight * (0.62f + 0.25f * pulse * calm));
                // The burst: the halo starts big and settles in.
                float burst = 1f + m_Burst * m_Burst * 1.4f;
                halo.rectTransform.localScale = Vector3.one * ((1f + 0.12f * pulse * calm) * burst);
            }
            if (rays != null)
            {
                rays.enabled = weight > 0f;
                rays.color = Color.Lerp(haloColor, Color.white, 0.35f).WithAlpha(weight * (0.55f + 0.2f * Mathf.Sin(t * 2.1f) * calm));
                rays.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -t * raysSpeed);
                rays.rectTransform.localScale = Vector3.one * (1f + 0.5f * m_Burst);
            }
            if (icon != null) icon.rectTransform.localScale = Vector3.one * (1f + weight * 0.07f * Mathf.Sin(t * 3.2f + 0.8f) * calm);
        }
    }
}
