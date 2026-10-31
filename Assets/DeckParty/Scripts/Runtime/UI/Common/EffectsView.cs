using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>Screen flash, camera-style shake, confetti and floating score text.</summary>
    public sealed class EffectsView : MonoBehaviour
    {
        [SerializeField] internal Image flash;
        [SerializeField] internal RectTransform shakeTarget;
        [SerializeField] internal RectTransform particleRoot;

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
        Coroutine m_Shake;
        Vector2 m_ShakeOrigin;

        void Awake()
        {
            if (flash != null)
            {
                flash.color = new Color(1f, 1f, 1f, 0f);
                flash.raycastTarget = false;
            }
            if (shakeTarget != null) m_ShakeOrigin = shakeTarget.anchoredPosition;
        }

        public void Flash(Color color, float duration = 0.45f)
        {
            if (flash == null || !isActiveAndEnabled) return;
            if (m_Flash != null) StopCoroutine(m_Flash);
            m_Flash = StartCoroutine(UITween.Run(duration, t => flash.color = new Color(color.r, color.g, color.b, (1f - t) * 0.45f)));
        }

        public void Shake(float intensity = 16f, float duration = 0.4f)
        {
            if (shakeTarget == null || !isActiveAndEnabled) return;
            if (m_Shake != null)
            {
                StopCoroutine(m_Shake);
                shakeTarget.anchoredPosition = m_ShakeOrigin;
            }
            m_Shake = StartCoroutine(ShakeRoutine(intensity, duration));
        }

        IEnumerator ShakeRoutine(float intensity, float duration)
        {
            yield return UITween.Run(duration, t =>
            {
                float falloff = 1f - t;
                shakeTarget.anchoredPosition = m_ShakeOrigin + Random.insideUnitCircle * intensity * falloff;
            });
            shakeTarget.anchoredPosition = m_ShakeOrigin;
            m_Shake = null;
        }

        public void Confetti(int count = 90)
        {
            if (particleRoot == null) return;
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
