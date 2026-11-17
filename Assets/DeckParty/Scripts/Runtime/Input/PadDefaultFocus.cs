using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// Marks the button the gamepad selector starts on when this screen or pop-up opens (the main choice: Single Player,
    /// Confirm, Ready...). The highest <see cref="priority"/> among the markers that can be clicked right now wins, so a
    /// dialog's marker beats the screen under it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PadDefaultFocus : MonoBehaviour
    {
        [Tooltip("Higher wins when several are on screen (pop-ups use a higher number than the screens under them).")]
        public int priority;

        static readonly List<PadDefaultFocus> s_Active = new List<PadDefaultFocus>();

        internal static IReadOnlyList<PadDefaultFocus> Active => s_Active;

        public Selectable Target => GetComponent<Selectable>();

        /// <summary>Adds the marker to <paramref name="target"/> (once).</summary>
        public static PadDefaultFocus Mark(Component target, int priority = 0)
        {
            if (target == null) return null;
            if (!target.TryGetComponent<PadDefaultFocus>(out var marker)) marker = target.gameObject.AddComponent<PadDefaultFocus>();
            marker.priority = priority;
            return marker;
        }

        void OnEnable()
        {
            if (!s_Active.Contains(this)) s_Active.Add(this);
            PadNavigator.RequestRefocus();
        }

        void OnDisable() => s_Active.Remove(this);
    }
}
