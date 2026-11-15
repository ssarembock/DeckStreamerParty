using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeckParty
{
    /// <summary>A colour that is only used when ticked (otherwise the game's own colour applies).</summary>
    [Serializable]
    public struct OptionalColour
    {
        public bool use;
        public Color colour;

        public OptionalColour(Color colour, bool use = false)
        {
            this.use = use;
            this.colour = colour;
        }

        /// <summary>This colour when ticked, otherwise <paramref name="fallback"/>.</summary>
        public Color Or(Color fallback) => use ? colour : fallback;
    }

    /// <summary>The buttons in the shop's toolbar (the row above the viewer cards). Stored by value: only append.</summary>
    public enum ShopButton
    {
        /// <summary>The streamer's active (the Use button beside the portrait).</summary>
        Power = 0,
        RaiseSubCap = 1,
        RemoveViewer = 2,
        /// <summary>Older scenes only: the shop no longer has its own Audience button (the menu bar has one).</summary>
        Audience = 3,
        GoLive = 4,
    }

    /// <summary>What a shop button says beside its icon.</summary>
    public enum ShopButtonText
    {
        /// <summary>The game's own: +1 (Sub Cap), -1 (Remove a Viewer), your Audience count; nothing on Use and Go Live.</summary>
        BuiltIn = 0,
        /// <summary>A short word in the player's language ("Use", "Sub Cap +1", "Remove", "Audience", "Go Live").</summary>
        Words = 1,
        /// <summary>Your own text (the same in every language).</summary>
        Custom = 2,
        /// <summary>Only the icon, centred.</summary>
        None = 3,
    }

    /// <summary>One shop button's look (UI Skin > Shop). Empty or zero values keep the built-in look.</summary>
    [Serializable]
    public sealed class ShopButtonLook
    {
        [HideInInspector] public string name;
        public ShopButton button;

        [Header("Icon")]
        [Tooltip("Empty: the button's icon entry further down the UI Skin.")]
        public Sprite icon;
        public OptionalColour iconColour = new OptionalColour(Color.black);
        [Tooltip("Icon width and height in pixels. 0: the built-in size.")]
        public float iconSize;

        [Header("Shape")]
        [Tooltip("The button's background (a 9-slice sprite stretches without bending its corners). Empty: the built-in rounded rectangle.")]
        public Sprite shape;
        [Tooltip("Makes the 9-slice corners thinner (higher) or thicker (lower).")]
        public float shapeScale = 1f;
        public OptionalColour fill = new OptionalColour(Color.white);
        public OptionalColour border = new OptionalColour(Color.black);
        [Tooltip("Border thickness in pixels. -1: the built-in border; 0: no border.")]
        public float borderWidth = -1f;
        [Tooltip("Width and height in pixels. 0: the built-in size.")]
        public Vector2 size;

        [Header("Text")]
        public ShopButtonText text = ShopButtonText.BuiltIn;
        [Tooltip("Used when Text is Custom. Not translated: it shows the same in every language.")]
        public string customText;
        public OptionalColour textColour = new OptionalColour(Color.black);
        [Tooltip("Font size. 0: the built-in size.")]
        public float textSize;
    }

    /// <summary>
    /// UI Skin > Shop: the toolbar buttons (icon, text, shape), their prices above them, and the viewer cards (your own card
    /// prefab, sizes, piles and colours). Everything left empty, zero or unticked keeps the built-in look.
    /// </summary>
    [Serializable]
    public sealed class ShopLook
    {
        public List<ShopButtonLook> buttons = new List<ShopButtonLook>();

        [Header("Prices above the buttons")]
        [Tooltip("Font size of the price above Use, Sub Cap +1 and Remove a Viewer. 0: the built-in size.")]
        public float costSize;
        [Tooltip("Space between a price and the top of its button, in pixels. -1: the built-in gap.")]
        public float costGap = -1f;
        public OptionalColour costColour = new OptionalColour(Color.black);
        [Tooltip("The price's colour when you don't have enough Cash. Unticked: red.")]
        public OptionalColour costTooExpensive = new OptionalColour(new Color(0.86f, 0.2f, 0.24f));

        [Header("Viewer cards")]
        [Tooltip("Your own card design (a prefab). Empty: the built-in card. See ShopCardPrefab_Guidelines.md.")]
        public GameObject cardPrefab;
        [Tooltip("Cards never get narrower than this (more columns fit on wider screens). 0: built-in (160).")]
        public float minCardWidth;
        [Tooltip("Cards never get wider than this. 0: built-in (196).")]
        public float maxCardWidth;
        [Tooltip("Card height divided by its width. 0: built-in (1.4), or your prefab's own proportions.")]
        public float cardAspect;
        [Tooltip("Space between cards, across and down. 0: built-in (38 x 36).")]
        public Vector2 cardSpacing;
        [Tooltip("Extra space added between one row of cards and the next, on top of the space down. -1: built-in (32); 0: none.")]
        public float rowGap = -1f;

        [Tooltip("Space above the first row of cards (between the top of the card area and the first row), in pixels. " +
                 "-1: built-in (30 plus the extra space between rows).")]
        public float firstRowSpace = -1f;

        /// <summary>The extra space between rows when <see cref="rowGap"/> is -1.</summary>
        public const float BuiltInRowGap = 32f;

        /// <summary>The extra space between rows of cards, in pixels (the skin's value, or the built-in one).</summary>
        public float RowGap => rowGap >= 0f ? rowGap : BuiltInRowGap;
        [Tooltip("How much a card grows under the pointer (1 = not at all). 0: built-in (1.04).")]
        public float hoverScale;

        [Header("Built-in card")]
        [Tooltip("The card's outline and inside (a 9-slice sprite gives it rounded corners). Empty: square corners.")]
        public Sprite cardShape;
        [Tooltip("Card border thickness in pixels. -1: built-in (3); 0: no border.")]
        public float cardBorderWidth = -1f;
        [Tooltip("Hide the card backs stacked above each card.")]
        public bool hidePiles;
        [Tooltip("How far each card back peeks out above the one in front, in pixels. 0: built-in (7).")]
        public float pileStep;

        [Header("Card colours (any card)")]
        public OptionalColour trophyBorder = new OptionalColour(new Color(0.85f, 0.65f, 0.13f));
        public OptionalColour price = new OptionalColour(Color.black);
        public OptionalColour priceTooExpensive = new OptionalColour(new Color(0.86f, 0.2f, 0.24f));
        public OptionalColour copiesLeft = new OptionalColour(new Color(0.4f, 0.4f, 0.45f));
        public OptionalColour lastCopy = new OptionalColour(new Color(0.9f, 0.55f, 0.1f));
        public OptionalColour soldOut = new OptionalColour(new Color(0.86f, 0.2f, 0.24f));
        [Tooltip("The veil over a card you can't buy right now.")]
        public OptionalColour cantBuyVeil = new OptionalColour(new Color(1f, 1f, 1f, 0.62f));

        /// <summary>The look of one button (always present after <see cref="EnsureAll"/>).</summary>
        public ShopButtonLook Get(ShopButton button)
        {
            if (buttons == null) return null;
            foreach (var look in buttons)
                if (look != null && look.button == button) return look;
            return null;
        }

        /// <summary>Adds a look for any button that has none (Use starts with Words, so it says "Use").</summary>
        public void EnsureAll()
        {
            if (buttons == null) buttons = new List<ShopButtonLook>();
            foreach (ShopButton button in Enum.GetValues(typeof(ShopButton)))
            {
                var look = Get(button);
                if (look == null)
                {
                    look = new ShopButtonLook { button = button };
                    if (button == ShopButton.Power) look.text = ShopButtonText.Words;
                    buttons.Add(look);
                }
                look.name = button.ToString();
            }
            buttons.Sort((a, b) => a.button.CompareTo(b.button));
        }

        /// <summary>The translated word a button shows when its Text is Words.</summary>
        public static string WordsKey(ShopButton button)
        {
            switch (button)
            {
                case ShopButton.Power: return "shopbtn.power";
                case ShopButton.RaiseSubCap: return "shopbtn.raise_cap";
                case ShopButton.RemoveViewer: return "shopbtn.remove";
                case ShopButton.Audience: return "shopbtn.audience";
                default: return "shopbtn.go_live";
            }
        }

        /// <summary>The shop settings of the current UI Skin (null when there's no skin).</summary>
        public static ShopLook Current
        {
            get
            {
                var skin = GameDatabase.Instance != null ? GameDatabase.Instance.skin : null;
                return skin != null ? skin.shop : null;
            }
        }

        /// <summary>The colour of a price above a shop button.</summary>
        public static Color CostColour(bool affordable, Color siteText)
        {
            var look = Current;
            if (affordable) return look != null ? look.costColour.Or(siteText) : siteText;
            return look != null ? look.costTooExpensive.Or(SitePalette.Danger) : SitePalette.Danger;
        }
    }
}
