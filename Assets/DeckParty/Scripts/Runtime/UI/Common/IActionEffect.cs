namespace DeckParty.UI
{
    /// <summary>
    /// An effect on a shop card that reacts to whether the card's viewer has an action (Inner Outline, Colour Swipe, Action
    /// Colour). The card (<see cref="ShopItemView"/>) finds every one inside it and tells them each time it shows a viewer.
    /// </summary>
    public interface IActionEffect
    {
        /// <param name="hasAction">The card's viewer has an action.</param>
        /// <param name="instant">The card now shows a different viewer: switch straight away instead of fading.</param>
        void SetViewerHasAction(bool hasAction, bool instant);
    }
}
