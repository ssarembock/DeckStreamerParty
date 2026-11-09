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
            if (tintPlaceholderOnly) image.color = GameIcons.SlotTint(slot, PlaceholderTint);
            else if (hasArt && entry.overrideColor) image.color = entry.color;
        }

        /// <summary>The colour for the built-in white placeholder shape.</summary>
        public Color PlaceholderTint => siteText ? SiteColours.Get(SiteColourRole.Text, transform) : placeholderTint;
    }
}
