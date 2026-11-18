using UnityEngine;

namespace DeckParty.UI
{
    /// <summary>
    /// Keeps a button out of the gamepad selector: the selector never lands on it, but its own pad button
    /// (<see cref="PadShortcut"/>) and the mouse still press it. Used on Add Viewer and End the Stream (they have shortcuts)
    /// and on a viewer's action icon (the selector lands on the viewer itself, and Confirm uses its action).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PadSelectorSkip : MonoBehaviour
    {
        /// <summary>Marks <paramref name="target"/>'s button as one the selector skips.</summary>
        public static void Add(Component target)
        {
            if (target != null && !target.TryGetComponent<PadSelectorSkip>(out _)) target.gameObject.AddComponent<PadSelectorSkip>();
        }
    }
}
