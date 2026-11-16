using System.Collections.Generic;
using UnityEngine;

namespace DeckParty
{
    /// <summary>
    /// Keeps a UI element that effects punch and wobble (the Risk meter, the menu bar numbers) at its built rotation and
    /// scale. Feel's additive rotation and scale feedbacks take whatever pose the element has when they start as their
    /// starting point, so a second effect fired while the first is still moving (a Hate-Watcher's mood flip and its Risk
    /// land together) settles on a tilted pose and stays there. Once no effect has touched the element for
    /// <see cref="SettleAfter"/> seconds, this eases it back to the pose it had when the scene loaded.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FxRestPose : MonoBehaviour
    {
        /// <summary>Quiet time (unscaled seconds) after the last effect before the element is eased back.</summary>
        public const float SettleAfter = 1.2f;
        /// <summary>How fast it eases back (degrees per second, and scale per second).</summary>
        const float TurnSpeed = 240f;
        const float ScaleSpeed = 3f;

        static readonly Dictionary<Transform, float> s_LastTouched = new Dictionary<Transform, float>();

        Quaternion m_Rotation;
        Vector3 m_Scale;
        bool m_Captured;

        /// <summary>Adds the guard to <paramref name="target"/> (once), remembering its current pose as the rest pose.</summary>
        public static FxRestPose Ensure(Transform target)
        {
            if (target == null) return null;
            if (!target.TryGetComponent<FxRestPose>(out var pose)) pose = target.gameObject.AddComponent<FxRestPose>();
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
        /// Switched off mid-effect (the Stream Chat window closes when a stream ends): the effect's coroutine dies with it, so
        /// put the element straight back at rest.
        /// </summary>
        void OnDisable()
        {
            if (!m_Captured) return;
            transform.localRotation = m_Rotation;
            transform.localScale = m_Scale;
        }

        void LateUpdate()
        {
            if (s_LastTouched.TryGetValue(transform, out float touched) && Time.unscaledTime - touched < SettleAfter) return;
            var rotation = transform.localRotation;
            if (rotation != m_Rotation) transform.localRotation = Quaternion.RotateTowards(rotation, m_Rotation, TurnSpeed * Time.unscaledDeltaTime);
            var scale = transform.localScale;
            if (scale != m_Scale) transform.localScale = Vector3.MoveTowards(scale, m_Scale, ScaleSpeed * Time.unscaledDeltaTime);
        }
    }
}
