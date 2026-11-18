using System.Collections.Generic;
using UnityEngine;

namespace DeckParty
{
    /// <summary>
    /// Keeps a UI element that effects punch and wobble (the Risk meter, the menu bar numbers, the Deplatform warning) at its
    /// built rotation and scale, and optionally its position. Feel's additive rotation and scale feedbacks and its relative
    /// position shakes take whatever pose the element has when they start as their starting point, so a second effect fired
    /// while the first is still moving (a Hate-Watcher's mood flip and its Risk land together), or an element hidden
    /// mid-effect and shown again, settles on a tilted or shifted pose and stays there. Once no effect has touched the
    /// element for <see cref="SettleAfter"/> seconds, this eases it back to the pose it had when it was set up.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FxRestPose : MonoBehaviour
    {
        /// <summary>Quiet time (unscaled seconds) after the last effect before the element is eased back.</summary>
        public const float SettleAfter = 1.2f;
        /// <summary>How fast it eases back (degrees per second, scale per second and pixels per second).</summary>
        const float TurnSpeed = 240f;
        const float ScaleSpeed = 3f;
        const float MoveSpeed = 600f;

        static readonly Dictionary<Transform, float> s_LastTouched = new Dictionary<Transform, float>();

        Quaternion m_Rotation;
        Vector3 m_Scale;
        Vector2 m_Position;
        bool m_KeepPosition;
        bool m_Captured;

        /// <summary>
        /// Adds the guard to <paramref name="target"/> (once), remembering its current pose as the rest pose. With
        /// <paramref name="position"/> it also brings a RectTransform back to its anchored position (shakes move it).
        /// </summary>
        public static FxRestPose Ensure(Transform target, bool position = false)
        {
            if (target == null) return null;
            if (!target.TryGetComponent<FxRestPose>(out var pose)) pose = target.gameObject.AddComponent<FxRestPose>();
            pose.Capture();
            if (position && !pose.m_KeepPosition && target is RectTransform rect)
            {
                pose.m_KeepPosition = true;
                pose.m_Position = rect.anchoredPosition;
            }
            return pose;
        }

        /// <summary>An effect or tween just moved <paramref name="target"/>: wait before easing it back.</summary>
        public static void Touch(Transform target)
        {
            if (target != null) s_LastTouched[target] = Time.unscaledTime;
        }

        void Awake() => Capture();

        void Capture()
        {
            if (m_Captured) return;
            m_Captured = true;
            m_Rotation = transform.localRotation;
            m_Scale = transform.localScale;
        }

        void OnDestroy() => s_LastTouched.Remove(transform);

        /// <summary>
        /// Switched off mid-effect (the Stream Chat window closes when a stream ends, the Deplatform warning hides): the
        /// effect's coroutine dies with it, so put the element straight back at rest.
        /// </summary>
        void OnDisable()
        {
            if (!m_Captured) return;
            transform.localRotation = m_Rotation;
            transform.localScale = m_Scale;
            if (m_KeepPosition && transform is RectTransform rect) rect.anchoredPosition = m_Position;
        }

        void LateUpdate()
        {
            if (s_LastTouched.TryGetValue(transform, out float touched) && Time.unscaledTime - touched < SettleAfter) return;
            var rotation = transform.localRotation;
            if (rotation != m_Rotation) transform.localRotation = Quaternion.RotateTowards(rotation, m_Rotation, TurnSpeed * Time.unscaledDeltaTime);
            var scale = transform.localScale;
            if (scale != m_Scale) transform.localScale = Vector3.MoveTowards(scale, m_Scale, ScaleSpeed * Time.unscaledDeltaTime);
            if (m_KeepPosition && transform is RectTransform rect && rect.anchoredPosition != m_Position)
                rect.anchoredPosition = Vector2.MoveTowards(rect.anchoredPosition, m_Position, MoveSpeed * Time.unscaledDeltaTime);
        }
    }
}
