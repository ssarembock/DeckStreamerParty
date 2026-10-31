using System;
using System.Collections;
using UnityEngine;

namespace DeckParty.UI
{
    /// <summary>Minimal coroutine tweens (no third-party dependency). Durations respect the "fast animations" setting.</summary>
    public static class UITween
    {
        public static float Scaled(float seconds) => seconds * AppSettings.AnimationTimeScale;

        public static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

        public static float EaseInOut(float t) => t * t * (3f - 2f * t);

        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        public static IEnumerator Run(float duration, Action<float> step, float delay = 0f)
        {
            if (delay > 0f) yield return new WaitForSecondsRealtime(Scaled(delay));
            float scaled = Mathf.Max(0.0001f, Scaled(duration));
            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.unscaledDeltaTime / scaled);
                step(t);
                yield return null;
            }
        }

        public static Coroutine Fade(MonoBehaviour host, CanvasGroup group, float to, float duration, float delay = 0f)
        {
            if (group == null) return null;
            if (host == null || !host.isActiveAndEnabled)
            {
                group.alpha = to;
                return null;
            }
            float from = group.alpha;
            return host.StartCoroutine(Run(duration, t => group.alpha = Mathf.Lerp(from, to, EaseOutCubic(t)), delay));
        }

        public static Coroutine PopIn(MonoBehaviour host, Transform target, float duration = 0.28f, float delay = 0f, float fromScale = 0.4f)
        {
            if (target == null) return null;
            if (host == null || !host.isActiveAndEnabled)
            {
                target.localScale = Vector3.one;
                return null;
            }
            target.localScale = Vector3.one * fromScale;
            return host.StartCoroutine(Run(duration, t => target.localScale = Vector3.one * Mathf.LerpUnclamped(fromScale, 1f, EaseOutBack(t)), delay));
        }

        public static Coroutine Punch(MonoBehaviour host, Transform target, float amount = 0.15f, float duration = 0.3f)
        {
            if (target == null || host == null || !host.isActiveAndEnabled) return null;
            var baseScale = Vector3.one;
            return host.StartCoroutine(Run(duration, t =>
            {
                float s = 1f + Mathf.Sin(t * Mathf.PI) * amount * (1f - t * 0.5f);
                target.localScale = baseScale * s;
            }));
        }
    }
}
