using TMPro;
using UnityEngine;

namespace DeckParty.UI
{
    /// <summary>
    /// Shiny title (nice-to-have N10): a light sweep travels across the letters, they bob gently, and every few seconds the
    /// title glitches for a moment. Works on any TextMeshPro text (and in every language); switch it off in the FX Library.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class TitleShine : MonoBehaviour
    {
        public Color shineColor = Color.white;
        [Tooltip("Seconds for the light sweep to cross the title.")]
        public float sweepSeconds = 2.4f;
        public float bobPixels = 6f;
        public Vector2 glitchInterval = new Vector2(4f, 8f);

        TMP_Text m_Text;
        float m_NextGlitch;
        float m_GlitchUntil;

        void Awake()
        {
            m_Text = GetComponent<TMP_Text>();
            m_NextGlitch = Time.unscaledTime + Random.Range(glitchInterval.x, glitchInterval.y);
        }

        void LateUpdate()
        {
            if (!FxLibrary.Current.shinyTitle || m_Text == null) return;
            m_Text.ForceMeshUpdate();
            var info = m_Text.textInfo;
            int count = info.characterCount;
            if (count == 0) return;

            float t = Time.unscaledTime;
            if (t >= m_NextGlitch && !AppSettings.ReduceFlashing)
            {
                m_GlitchUntil = t + 0.18f;
                m_NextGlitch = t + Random.Range(glitchInterval.x, glitchInterval.y);
            }
            bool glitching = t < m_GlitchUntil;
            float sweep = Mathf.Repeat(t / Mathf.Max(0.2f, sweepSeconds), 1.6f) - 0.3f;

            for (int i = 0; i < count; i++)
            {
                var ch = info.characterInfo[i];
                if (!ch.isVisible) continue;
                int mat = ch.materialReferenceIndex, v = ch.vertexIndex;
                var verts = info.meshInfo[mat].vertices;
                var colors = info.meshInfo[mat].colors32;

                float along = count > 1 ? i / (float)(count - 1) : 0f;
                var offset = new Vector3(0f, Mathf.Sin(t * 2.2f + i * 0.45f) * bobPixels, 0f);
                if (glitching) offset += new Vector3(Random.Range(-10f, 10f), Random.Range(-3f, 3f), 0f);
                for (int k = 0; k < 4; k++) verts[v + k] += offset;

                float shine = Mathf.Clamp01(1f - Mathf.Abs(along - sweep) * 6f);
                if (shine <= 0f && !glitching) continue;
                for (int k = 0; k < 4; k++)
                {
                    var c = (Color)colors[v + k];
                    var lit = Color.Lerp(c, shineColor, shine * 0.75f);
                    if (glitching) lit = Color.Lerp(lit, k % 2 == 0 ? UIPalette.Teal : UIPalette.Pink, 0.5f);
                    lit.a = c.a;
                    colors[v + k] = lit;
                }
            }
            m_Text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
        }
    }
}
