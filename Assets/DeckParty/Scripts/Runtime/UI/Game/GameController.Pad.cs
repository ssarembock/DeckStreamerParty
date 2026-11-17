using DeckParty.Core;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// The game screen with a gamepad (2026-11-17). The stream and shop buttons have their own pad buttons
    /// (<see cref="PadShortcut"/>, bound in their views); the selector (<see cref="PadNavigator"/>) moves between the
    /// viewers in chat, the shop cards and anything else clickable, and Confirm presses what it highlights. Back steps out
    /// of whatever is open (a pop-up, picking a target, the Audience overview) and Menu closes the pause menu again.
    /// </summary>
    public sealed partial class GameController
    {
        GamePhase m_PadPhase = (GamePhase)255;
        bool m_PadTargeting;

        /// <summary>Hooks the selector's starting point to this screen (called from Start).</summary>
        void SetupPad()
        {
            PadNavigator.DefaultProvider = PadDefault;
        }

        /// <summary>
        /// Where the selector starts: while picking a target, the first viewer that can be picked; in a stream, a viewer whose
        /// action is ready, else Add Viewer; in the shop, the first card.
        /// </summary>
        Selectable PadDefault()
        {
            var s = View;
            if (s == null) return null;
            if (s.Phase == GamePhase.Party)
            {
                if (Targeting)
                {
                    foreach (var card in house.Cards)
                        if (card.isActiveAndEnabled && card.highlight != null && card.highlight.enabled && card.button != null) return card.button;
                }
                foreach (var card in house.Cards)
                    if (card.isActiveAndEnabled && !card.IsEmpty && card.abilityButton != null && card.abilityButton.isActiveAndEnabled &&
                        card.abilityButton.interactable && card.button != null) return card.button;
                return partyControls.doorButton != null && partyControls.doorButton.interactable ? partyControls.doorButton : null;
            }
            if (s.Phase == GamePhase.Planning) return planning.FirstTile;
            return null;
        }

        /// <summary>
        /// The pad's own buttons, each frame: Back closes the top layer (never opens the pause menu: that's Menu), Menu closes
        /// the pause menu, and the selector pauses while the scoring sequence or a summary is up. Returns true when it handled
        /// the frame's input.
        /// </summary>
        bool UpdatePad()
        {
            // The scoring sequence and the summaries take Confirm/Back themselves (skip, close).
            PadNavigator.Paused = m_Scoring || RecapOpen;

            // The phase or targeting changed: the selector jumps to where the action now is.
            var s = View;
            var phase = s != null ? s.Phase : (GamePhase)255;
            if (phase != m_PadPhase || Targeting != m_PadTargeting)
            {
                m_PadPhase = phase;
                m_PadTargeting = Targeting;
                PadNavigator.RequestRefocus();
            }

            if (!PadInput.Active) return false;
            if (PadInput.Pressed(PadAction.Menu) && pauseMenu.IsOpen)
            {
                PadInput.Consume(PadAction.Menu);
                pauseMenu.Close();
                return true;
            }
            if (PadInput.Pressed(PadAction.Back))
            {
                PadInput.Consume(PadAction.Back);
                if (ModalDialog.IsOpen) ModalDialog.CloseActive();
                else if (picker.IsOpen) picker.Cancel();
                else if (rolodex.IsOpen) rolodex.Close();
                else if (roundSummary != null && roundSummary.IsOpen) roundSummary.Close();
                else if (Targeting) CancelTargeting(true);
                else if (pauseMenu.IsOpen) pauseMenu.Close();
                PadNavigator.RequestRefocus();
                return true;
            }
            return false;
        }

        /// <summary>Confirm or Back skips the scoring sequence / closes a summary (the selector is paused meanwhile).</summary>
        static bool PadSkipPressed() => PadInput.Pressed(PadAction.Confirm) || PadInput.Pressed(PadAction.Back);
    }
}
