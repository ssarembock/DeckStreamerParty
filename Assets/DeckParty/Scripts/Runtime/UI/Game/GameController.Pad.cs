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
        bool m_PadPeek;

        /// <summary>Hooks the selector's starting point to this screen (called from Start).</summary>
        void SetupPad()
        {
            PadNavigator.DefaultProvider = PadDefault;
        }

        /// <summary>
        /// Where the selector starts: while picking a target, the first viewer that can be picked; in a stream, a viewer whose
        /// action is ready, else the first viewer in chat (Add Viewer and End the Stream are only on their pad buttons); in
        /// the shop, the first card.
        /// </summary>
        Selectable PadDefault()
        {
            var s = View;
            if (s == null) return null;
            if (s.Phase == GamePhase.Party)
            {
                // A peeked viewer waits for Approve / Deny.
                if (PadPeekOpen)
                {
                    if (partyControls.admitButton != null && partyControls.admitButton.interactable) return partyControls.admitButton;
                    if (partyControls.rejectButton != null) return partyControls.rejectButton;
                }
                if (Targeting)
                {
                    foreach (var card in house.Cards)
                        if (card.isActiveAndEnabled && card.highlight != null && card.highlight.enabled && card.button != null) return card.button;
                }
                foreach (var card in house.Cards)
                    if (card.isActiveAndEnabled && !card.IsEmpty && card.abilityButton != null && card.abilityButton.isActiveAndEnabled &&
                        card.abilityButton.interactable && card.button != null) return card.button;
                foreach (var card in house.Cards)
                    if (card.isActiveAndEnabled && !card.IsEmpty && card.button != null) return card.button;
                return null;
            }
            if (s.Phase == GamePhase.Planning) return planning.FirstTile;
            return null;
        }

        /// <summary>
        /// Something in the stream needs the selector: a dialog, a picker, picking a target, the Audience, the menu, or the
        /// Approve / Deny choice for a peeked viewer (those two have no pad buttons of their own).
        /// </summary>
        bool PadOverlayOpen =>
            Targeting || ModalDialog.IsOpen || picker.IsOpen || rolodex.IsOpen || pauseMenu.IsOpen || PadPeekOpen ||
            (roundSummary != null && roundSummary.IsOpen) || (raidBlame != null && raidBlame.gameObject.activeInHierarchy);

        /// <summary>The Approve / Deny choice for a peeked viewer is up.</summary>
        bool PadPeekOpen => partyControls.peekPanel != null && partyControls.peekPanel.activeInHierarchy;

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
            if (phase != m_PadPhase || Targeting != m_PadTargeting || PadPeekOpen != m_PadPeek)
            {
                m_PadPeek = PadPeekOpen;
                // A new stream starts with the selector hidden: it shows on the first direction press (the stream's own
                // buttons are on pad buttons). The shop and everything else show it straight away.
                if (phase != m_PadPhase)
                {
                    if (phase == GamePhase.Party) PadNavigator.Sleep();
                    else PadNavigator.Wake();
                }
                m_PadPhase = phase;
                m_PadTargeting = Targeting;
                PadNavigator.RequestRefocus();
            }
            // Anything opened over the stream needs the selector.
            if (PadNavigator.Dormant && PadOverlayOpen) PadNavigator.Wake();

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
