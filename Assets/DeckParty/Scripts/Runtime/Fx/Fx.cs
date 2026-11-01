using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DeckParty
{
    /// <summary>Plays polish moments. The Feel integration (DeckParty.Juice) registers itself as the backend.</summary>
    public interface IFxBackend
    {
        /// <summary>Plays the cue on the target. Return false to let the built-in fallback handle it.</summary>
        bool Play(FxCue cue, Transform target, float intensity);
    }

    /// <summary>
    /// The single entry point for "juice". Game code says what happened (<c>Fx.Play(FxCue.ViewerJoined, card)</c>) and the
    /// Feel feedback assigned to that cue in the FX Library plays on the target. Without Feel (or with a cue switched
    /// off) a small built-in tween plays instead, so the game never depends on the package.
    /// Every effect runs on unscaled time, honours Fast Animations, the Screen Shake slider and Reduce Flashing.
    /// </summary>
    public static class Fx
    {
        public static IFxBackend Backend;

        static RectTransform s_ShakeRoot;
        static Vector2 s_ShakeOrigin;

        /// <summary>Screen shake strength after the player's slider and the FX Library multiplier (0 = off).</summary>
        public static float ShakeScale => AppSettings.ScreenShake * FxLibrary.Current.shakeMultiplier;

        /// <summary>Duration multiplier for every effect (FX Library x Fast Animations).</summary>
        public static float DurationScale => FxLibrary.Current.durationMultiplier * AppSettings.AnimationTimeScale;

        /// <summary>The object screen shakes move (the game UI root, never the camera: the canvases are overlay).</summary>
        public static RectTransform ShakeRoot => s_ShakeRoot;

        public static void RegisterShakeRoot(RectTransform root)
        {
            s_ShakeRoot = root;
            if (root != null) s_ShakeOrigin = root.anchoredPosition;
        }

        public static bool Enabled(FxCue cue)
        {
            var entry = FxLibrary.Current.Get(cue);
            return entry == null || entry.enabled;
        }

        public static void Play(FxCue cue, Transform target, float intensity = 1f)
        {
            if (target == null || !target.gameObject.activeInHierarchy) return;
            var entry = FxLibrary.Current.Get(cue);
            if (entry != null && !entry.enabled) return;
            float strength = intensity * (entry != null ? entry.intensity : 1f);
            if (IsShake(cue))
            {
                strength *= ShakeScale;
                if (strength <= 0.01f) return;
            }

            if (Backend != null)
            {
                try
                {
                    if (Backend.Play(cue, target, strength)) return;
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            }
            FxRunner.Instance.Fallback(cue, target, strength);
        }

        /// <summary>Shakes the whole game UI by roughly <paramref name="pixels"/> (scaled by the Screen Shake setting).</summary>
        public static void Shake(float pixels, float duration = 0.35f)
        {
            if (s_ShakeRoot == null || pixels * ShakeScale <= 0.5f) return;
            if (Backend != null && Enabled(FxCue.ScreenShake) && Backend.Play(FxCue.ScreenShake, s_ShakeRoot, pixels / 16f * ShakeScale)) return;
            FxRunner.Instance.ShakeRoot(s_ShakeRoot, s_ShakeOrigin, pixels * ShakeScale, duration * DurationScale);
        }

        static bool IsShake(FxCue cue) =>
            cue == FxCue.ScreenShake || cue == FxCue.PlatformBan || cue == FxCue.ServerDown || cue == FxCue.StreamerAnxious || cue == FxCue.ButtonDenied;

        /// <summary>A flash colour respecting Reduce Flashing (alpha scaled down).</summary>
        public static float FlashAlpha(float alpha) => AppSettings.ReduceFlashing ? alpha * 0.25f : alpha;
    }

    /// <summary>Runs the built-in fallback tweens (and any coroutine that must outlive a view).</summary>
    public sealed class FxRunner : MonoBehaviour
    {
        static FxRunner s_Instance;
        readonly Dictionary<Transform, Coroutine> m_Running = new Dictionary<Transform, Coroutine>();
        Coroutine m_Shake;

        public static FxRunner Instance
        {
            get
            {
                if (s_Instance != null) return s_Instance;
                var go = new GameObject("FxRunner") { hideFlags = HideFlags.HideInHierarchy };
                DontDestroyOnLoad(go);
                s_Instance = go.AddComponent<FxRunner>();
                return s_Instance;
            }
        }

        public void Fallback(FxCue cue, Transform target, float strength)
        {
            switch (cue)
            {
                case FxCue.ViewerJoined:
                case FxCue.Purchase:
                case FxCue.MenuIn:
                case FxCue.ToastIn:
                case FxCue.BannerSlam:
                case FxCue.TurnBanner:
                case FxCue.BoardSwitch:
                case FxCue.Stamp:
                    Restart(target, Pop(target, 0.45f, 0.3f));
                    break;
                case FxCue.DiamondJoined:
                case FxCue.SelectConfirm:
                case FxCue.Victory:
                    Restart(target, Pop(target, 0.2f, 0.45f));
                    break;
                case FxCue.ButtonDenied:
                case FxCue.SubCapFull:
                case FxCue.PlatformBan:
                case FxCue.ServerDown:
                case FxCue.StreamerAnxious:
                case FxCue.ScreenShake:
                    Restart(target, Wobble(target, 8f * strength, 0.3f));
                    break;
                case FxCue.CardHover:
                    Restart(target, Punch(target, 0.05f * strength, 0.18f));
                    break;
                default:
                    Restart(target, Punch(target, 0.14f * strength, 0.28f));
                    break;
            }
        }

        void Restart(Transform target, IEnumerator routine)
        {
            if (m_Running.TryGetValue(target, out var running) && running != null)
            {
                StopCoroutine(running);
                if (target != null) target.localScale = Vector3.one;
            }
            m_Running[target] = StartCoroutine(Track(target, routine));
        }

        IEnumerator Track(Transform target, IEnumerator routine)
        {
            yield return routine;
            m_Running.Remove(target);
        }

        static IEnumerator Pop(Transform target, float from, float duration)
        {
            duration *= Fx.DurationScale;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                if (target == null) yield break;
                float k = t / duration;
                const float s = 1.70158f;
                float eased = 1f + (s + 1f) * Mathf.Pow(k - 1f, 3f) + s * Mathf.Pow(k - 1f, 2f);
                target.localScale = Vector3.one * Mathf.LerpUnclamped(from, 1f, eased);
                yield return null;
            }
            if (target != null) target.localScale = Vector3.one;
        }

        static IEnumerator Punch(Transform target, float amount, float duration)
        {
            duration *= Fx.DurationScale;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                if (target == null) yield break;
                float k = t / duration;
                target.localScale = Vector3.one * (1f + amount * Mathf.Sin(k * Mathf.PI) * (1f - k * 0.5f));
                yield return null;
            }
            if (target != null) target.localScale = Vector3.one;
        }

        static IEnumerator Wobble(Transform target, float degrees, float duration)
        {
            duration *= Fx.DurationScale;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                if (target == null) yield break;
                float k = t / duration;
                target.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * Mathf.PI * 6f) * degrees * (1f - k));
                yield return null;
            }
            if (target != null) target.localRotation = Quaternion.identity;
        }

        public void ShakeRoot(RectTransform root, Vector2 origin, float pixels, float duration)
        {
            if (m_Shake != null)
            {
                StopCoroutine(m_Shake);
                if (root != null) root.anchoredPosition = origin;
            }
            m_Shake = StartCoroutine(ShakeRoutine(root, origin, pixels, duration));
        }

        IEnumerator ShakeRoutine(RectTransform root, Vector2 origin, float pixels, float duration)
        {
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                if (root == null) yield break;
                root.anchoredPosition = origin + Random.insideUnitCircle * pixels * (1f - t / duration);
                yield return null;
            }
            if (root != null) root.anchoredPosition = origin;
            m_Shake = null;
        }
    }
}
