using UnityEngine;

namespace DeckParty.UI
{
    /// <summary>
    /// While this is enabled, the gamepad selector stands aside: the screen moves and confirms with the pad itself (the
    /// streamer select grid uses the D-pad, Confirm and Back through <see cref="InputHelper.MenuPressed"/>).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PadSelfNavigation : MonoBehaviour
    {
        void OnEnable() => PadNavigator.AddSelfNavigation();

        void OnDisable()
        {
            PadNavigator.RemoveSelfNavigation();
            PadNavigator.RequestRefocus();
        }
    }
}
