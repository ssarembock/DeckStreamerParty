using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// Screen-space effects: flash, shake, confetti, floating text, comic stamps, chips that fly to the HUD, the danger
    /// vignette and sparkles. Shakes and flashes honour the Screen Shake and Reduce Flashing settings.
    /// </summary>
    public sealed class EffectsView : MonoBehaviour
    {
        [SerializeField] internal Image flash;
        [SerializeField] internal RectTransform shakeTarget;
        [SerializeField] internal RectTransform particleRoot;
        [Tooltip("Optional full-screen red edge glow for danger (created at runtime when empty).")]
        [SerializeField] internal Image vignette;

        struct Particle
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Velocity;
            public float Spin;
            public float Life;
            public float MaxLife;
        }

        readonly List<Particle> m_Particles = new List<Particle>();
        Coroutine m_Flash;
        Vector2 m_ShakeOrigin;

        void Awake()
        {
            if (flash != null)
            {
                flash.color = new Color(1f, 1f, 1f, 0f);
                flash.raycastTarget = false;
            }
            if (shakeTarget != null) m_ShakeOrigin = shakeTarget.anchoredPosition;
            Fx.RegisterShakeRoot(shakeTarget);
            EnsureVignette();
        }

        public RectTransform ParticleRoot => particleRoot;

        public void Flash(Color color, float duration = 0.45f)
        {
            if (flash == null || !isActiveAndEnabled) return;
            if (m_Flash != null) StopCoroutine(m_Flash);
            float peak = Fx.FlashAlpha(0.45f);
            m_Flash = StartCoroutine(UITween.Run(duration, t => flash.color = new Color(color.r, color.g, color.b, (1f - t) * peak)));
        }

        public void Shake(float intensity = 16f, float duration = 0.4f) => Fx.Shake(intensity, duration);

        // ------------------------------------------------------------------ danger vignette (Platform Ban Risk tension)

        Coroutine m_Vignette;
        float m_VignetteTarget;

        void EnsureVignette()
        {
            if (vignette != null || particleRoot == null) return;
            var go = new GameObject("DangerVignette", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(particleRoot.parent != null ? particleRoot.parent : particleRoot, false);
            rect.SetAsFirstSibling();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            vignette = go.GetComponent<Image>();
            vignette.sprite = RuntimeSprites.Vignette;
            vignette.raycastTarget = false;
            vignette.color = new Color(1f, 0.15f, 0.2f, 0f);
        }

        /// <summary>Red screen edges that breathe while the stream is one Platform Ban Risk from a Platform Ban (0 = off).</summary>
        public void SetDanger(float level)
        {
            if (vignette == null) return;
            m_VignetteTarget = Mathf.Clamp01(level);
            if (m_Vignette == null && isActiveAndEnabled) m_Vignette = StartCoroutine(VignetteRoutine());
        }

        IEnumerator VignetteRoutine()
        {
            while (vignette != null)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.2f);
                float alpha = m_VignetteTarget * Mathf.Lerp(0.25f, 0.55f, pulse);
                if (AppSettings.ReduceFlashing) alpha = m_VignetteTarget * 0.3f;
                var c = vignette.color;
                c.a = Mathf.MoveTowards(c.a, alpha, Time.unscaledDeltaTime * 1.5f);
                vignette.color = c;
                if (m_VignetteTarget <= 0f && c.a <= 0.001f) break;
                yield return null;
            }
            m_Vignette = null;
        }

        // ------------------------------------------------------------------ stamps, chips and sparkles

        /// <summary>A comic stamp ("BONK!", "NOPE.") slammed over a point on screen.</summary>
        public void Stamp(string text, Vector3 worldPosition, Color color, float fontSize = 64f)
        {
            if (particleRoot == null || !isActiveAndEnabled || string.IsNullOrEmpty(text)) return;
            var rect = NewText("Stamp", text, fontSize, color, out var label);
            rect.position = worldPosition;
            rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-14f, 14f));
            label.fontStyle = FontStyles.Bold | FontStyles.Italic;
            StartCoroutine(StampRoutine(rect, label));
        }

        IEnumerator StampRoutine(RectTransform rect, TMP_Text label)
        {
            Fx.Play(FxCue.Stamp, rect);
            yield return UITween.Run(1.1f, t =>
            {
                if (rect == null) return;
                if (t < 0.12f) rect.localScale = Vector3.one * Mathf.Lerp(2.2f, 1f, t / 0.12f);
                label.alpha = t < 0.75f ? 1f : 1f - (t - 0.75f) / 0.25f;
            });
            if (rect != null) Destroy(rect.gameObject);
        }

        /// <summary>A "+3" chip that flies from one point to another (a card to the HUD counter).</summary>
        public void FlyChip(string richText, Vector3 from, Vector3 to, float delay = 0f)
        {
            if (particleRoot == null || !isActiveAndEnabled) return;
            var rect = NewText("Chip", richText, 40f, Color.white, out var label);
            rect.position = from;
            label.alpha = 0f;
            StartCoroutine(ChipRoutine(rect, label, from, to, delay));
        }

        IEnumerator ChipRoutine(RectTransform rect, TMP_Text label, Vector3 from, Vector3 to, float delay)
        {
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
            var control = Vector3.Lerp(from, to, 0.5f) + Vector3.up * Mathf.Max(80f, Vector3.Distance(from, to) * 0.35f);
            yield return UITween.Run(0.65f, t =>
            {
                if (rect == null) return;
                float k = UITween.EaseInOut(t);
                rect.position = (1 - k) * (1 - k) * from + 2 * (1 - k) * k * control + k * k * to;
                rect.localScale = Vector3.one * Mathf.Lerp(1.15f, 0.7f, k);
                label.alpha = t < 0.1f ? t / 0.1f : 1f;
            });
            if (rect != null) Destroy(rect.gameObject);
        }

        /// <summary>A small burst of sparkles (Diamonds, victories).</summary>
        public void Sparkles(Vector3 worldPosition, Color color, int count = 14)
        {
            if (particleRoot == null || !isActiveAndEnabled) return;
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Sparkle", typeof(RectTransform));
                go.transform.SetParent(particleRoot, false);
                var rect = (RectTransform)go.transform;
                rect.position = worldPosition;
                rect.sizeDelta = Vector2.one * Random.Range(8f, 16f);
                rect.localRotation = Quaternion.Euler(0, 0, 45f);
                var image = go.AddComponent<Image>();
                image.raycastTarget = false;
                image.color = Color.Lerp(color, Color.white, Random.Range(0f, 0.5f));
                float life = Random.Range(0.6f, 1.1f);
                var dir = Random.insideUnitCircle.normalized * Random.Range(160f, 420f);
                m_Particles.Add(new Particle { Rect = rect, Image = image, Velocity = dir, Spin = Random.Range(-540f, 540f), Life = life, MaxLife = life });
            }
        }

        RectTransform NewText(string name, string richText, float fontSize, Color color, out TMP_Text label)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(particleRoot, false);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(700f, 140f);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = richText;
            text.fontSize = fontSize;
            text.color = color;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.outlineWidth = 0.25f;
            text.outlineColor = new Color32(0, 0, 0, 220);
            go.AddComponent<SkinnedText>();
            label = text;
            return rect;
        }

        // Shakes run through Fx (Feel when installed), so the Screen Shake slider applies everywhere.

        public void Confetti(int count = 90)
        {
            if (particleRoot == null) return;
            if (AppSettings.ReduceFlashing) count /= 2;
            var size = particleRoot.rect.size;
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Confetti", typeof(RectTransform));
                go.transform.SetParent(particleRoot, false);
                var rect = (RectTransform)go.transform;
                rect.sizeDelta = new Vector2(Random.Range(10f, 18f), Random.Range(16f, 28f));
                rect.anchoredPosition = new Vector2(Random.Range(-size.x * 0.5f, size.x * 0.5f), size.y * 0.5f + Random.Range(20f, 300f));
                rect.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
                var image = go.AddComponent<Image>();
                image.raycastTarget = false;
                image.color = UIPalette.PlayerColor(Random.Range(0, UIPalette.PlayerColors.Length));
                float life = Random.Range(2.5f, 4f);
                m_Particles.Add(new Particle
                {
                    Rect = rect,
                    Image = image,
                    Velocity = new Vector2(Random.Range(-90f, 90f), Random.Range(-420f, -220f)),
                    Spin = Random.Range(-360f, 360f),
                    Life = life,
                    MaxLife = life,
                });
            }
        }

        public void FloatText(string richText, Vector2 anchoredPosition, float fontSize = 54f)
        {
            if (particleRoot == null || !isActiveAndEnabled) return;
            var go = new GameObject("FloatText", typeof(RectTransform));
            go.transform.SetParent(particleRoot, false);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(900f, 120f);
            rect.anchoredPosition = anchoredPosition;
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = richText;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.outlineWidth = 0.2f;
            text.outlineColor = new Color32(0, 0, 0, 200);
            go.AddComponent<SkinnedText>();
            StartCoroutine(FloatRoutine(rect, text));
        }

        IEnumerator FloatRoutine(RectTransform rect, TMP_Text text)
        {
            var start = rect.anchoredPosition;
            yield return UITween.Run(1.6f, t =>
            {
                rect.anchoredPosition = start + new Vector2(0f, UITween.EaseOutCubic(t) * 140f);
                rect.localScale = Vector3.one * Mathf.LerpUnclamped(0.7f, 1f, UITween.EaseOutBack(Mathf.Min(1f, t * 3f)));
                text.alpha = t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f;
            });
            Destroy(rect.gameObject);
        }

        void Update()
        {
            if (m_Particles.Count == 0) return;
            float dt = Time.unscaledDeltaTime;
            for (int i = m_Particles.Count - 1; i >= 0; i--)
            {
                var p = m_Particles[i];
                p.Life -= dt;
                if (p.Life <= 0f || p.Rect == null)
                {
                    if (p.Rect != null) Destroy(p.Rect.gameObject);
                    m_Particles.RemoveAt(i);
                    continue;
                }
                p.Velocity += new Vector2(Mathf.Sin(p.Life * 3f) * 40f, -60f) * dt;
                p.Rect.anchoredPosition += p.Velocity * dt;
                p.Rect.Rotate(0f, 0f, p.Spin * dt);
                var c = p.Image.color;
                c.a = Mathf.Clamp01(p.Life / (p.MaxLife * 0.3f));
                p.Image.color = c;
                m_Particles[i] = p;
            }
        }
    }
}
