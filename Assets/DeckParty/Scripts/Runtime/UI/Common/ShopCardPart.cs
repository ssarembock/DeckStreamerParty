using UnityEngine;

namespace DeckParty.UI
{
    /// <summary>
    /// Marks a piece of your own shop card prefab (UI Skin > Shop > Card prefab) so the game knows what to fill in: put it on
    /// the object and pick its Part. Instead of this component you can also name the object exactly like the part
    /// ("Portrait", "Price", "Name"...). See ShopCardPrefab_Guidelines.md in the project folder.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShopCardPart : MonoBehaviour
    {
        /// <summary>What a piece of the card shows. Stored by value: only append.</summary>
        public enum Part
        {
            /// <summary>Image: the card's inside. It's what you click (Raycast Target on).</summary>
            Background = 0,
            /// <summary>Image: the card's outline. The game turns it gold on Trophy viewers.</summary>
            Frame = 1,
            /// <summary>Image: the viewer's picture (or animation). Put it inside its own frame object.</summary>
            Portrait = 2,
            /// <summary>Image: a shape shown instead of the picture while a viewer has no art (tinted in the viewer's colour).</summary>
            Placeholder = 3,
            /// <summary>Text: the viewer's initials, shown on the placeholder.</summary>
            Initials = 4,
            /// <summary>Text: the viewer's name.</summary>
            Name = 5,
            /// <summary>Text: what the viewer does (the card description).</summary>
            Description = 6,
            /// <summary>Text: what the viewer scores, as icons and numbers (Cash, Influence, Risk).</summary>
            Stats = 7,
            /// <summary>Text: the Cash the viewer scores (a number like +2). Objects named "Clout" (its old name) count too.</summary>
            Cash = 8,
            /// <summary>Text: the Influence the viewer scores (a number like +1).</summary>
            Influence = 9,
            /// <summary>Text: the name of the viewer's action (empty when it has none).</summary>
            Action = 10,
            /// <summary>Text: the price (the Cash icon and the number), red when you can't afford it.</summary>
            Price = 11,
            /// <summary>Text: how many are left to buy ("×3"; empty for Trophy viewers, which never run out).</summary>
            CopiesLeft = 12,
            /// <summary>An object whose child Images become the community icons (in order; extra ones hide).</summary>
            Communities = 13,
            /// <summary>Any object: shown only on Trophy viewers.</summary>
            TrophyBadge = 14,
            /// <summary>Any object: shown only on viewers that add Deplatform Risk.</summary>
            RiskBadge = 15,
            /// <summary>Any object: shown only on viewers that cancel Deplatform Risk (a shield). Objects named "Shield" or "CancelsRiskBadge" (its old name) count too.</summary>
            ShieldBadge = 16,
            /// <summary>Any object: shown only on viewers with an action.</summary>
            ActionBadge = 17,
            /// <summary>Image: laid over the card while you can't buy it (its colour can be set in UI Skin > Shop).</summary>
            CantBuyVeil = 18,
            /// <summary>Any object: shown when none are left.</summary>
            SoldOut = 19,
            /// <summary>Any object: one card back of the pile; one shows per copy left after the top card. Use Order 1, 2, 3.</summary>
            PileCard = 20,
            /// <summary>RectTransform: the part that bounces when you buy (default: the whole card).</summary>
            AnimRoot = 21,
        }

        [SerializeField] internal Part part;
        [Tooltip("Pile cards only: 1 is the card back nearest the card (it's the last to go), then 2, 3...")]
        [SerializeField] internal int order = 1;

        public Part Kind => part;
        public int Order => order;
    }
}
