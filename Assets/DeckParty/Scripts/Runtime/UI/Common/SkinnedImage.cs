using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>Pulls this Image's sprite from the UI Skin slot (if the slot has a sprite). Works in edit mode too.</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Image))]
    public sealed class SkinnedImage : MonoBehaviour
    {
        [SerializeField] internal SkinSlot slot;
        [Tooltip("Icons: tint only the built-in placeholder shapes with Placeholder Tint; your own art shows in its own colours " +
                 "(unless the skin entry overrides the colour).")]
        [SerializeField] internal bool tintPlaceholderOnly;
        [SerializeField] internal Color placeholderTint = Color.white;
        [Tooltip("Icons on the website look's panels: the placeholder takes the text colour of the panel it sits in " +
                 "(UI Skin > Site Colours) instead of Placeholder Tint, so it stays visible on any panel colour.")]
        [SerializeField] internal bool siteText;
        [Tooltip("Frames and bases: with no art in the UI Skin entry this image goes back to the built-in sprite below, and with " +
                 "Override Colour unticked to the built-in colour (so clearing the entry undoes it without a rebuild).")]
        [SerializeField] internal bool restoreDefaults;
        [SerializeField] internal Sprite defaultSprite;
        [SerializeField] internal Color defaultColor = Color.white;
        [SerializeField] internal float defaultPixelsPerUnit = 1f;

        public SkinSlot Slot
        {
            get => slot;
            set
            {
                slot = value;
                Apply();
            }
        }

        void OnEnable()
        {
            UISkin.SkinChanged += Apply;
            Apply();
        }

        void OnDisable() => UISkin.SkinChanged -= Apply;

        public void Apply()
        {
            if (this == null) return;
            var skin = GameDatabase.Instance.skin;
            if (skin == null) return;
            var entry = skin.Get(slot);
            var image = GetComponent<Image>();
            bool hasArt = entry != null && entry.sprite != null;
            if (hasArt)
            {
                image.sprite = entry.sprite;
                image.type = entry.imageType;
                image.pixelsPerUnitMultiplier = Mathf.Max(0.01f, entry.pixelsPerUnitMultiplier);
            }
            else if (restoreDefaults)
            {
                image.sprite = defaultSprite;
                image.type = defaultSprite != null && defaultSprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
                image.pixelsPerUnitMultiplier = Mathf.Max(0.01f, defaultPixelsPerUnit);
            }

            bool ownColour = entry != null && entry.overrideColor;
            if (tintPlaceholderOnly) image.color = GameIcons.SlotTint(slot, PlaceholderTint);
            // The colour applies with or without art (an empty entry tints the built-in shape).
            else if (ownColour) image.color = entry.color;
            else if (restoreDefaults) image.color = defaultColor;
        }

        /// <summary>The colour for the built-in white placeholder shape.</summary>
        public Color PlaceholderTint => siteText ? SiteColours.Get(SiteColourRole.Text, transform) : placeholderTint;
    }
}
