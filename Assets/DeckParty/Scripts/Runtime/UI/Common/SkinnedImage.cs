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
            if (entry == null || entry.sprite == null) return;

            var image = GetComponent<Image>();
            image.sprite = entry.sprite;
            image.type = entry.imageType;
            image.pixelsPerUnitMultiplier = Mathf.Max(0.01f, entry.pixelsPerUnitMultiplier);
            if (tintPlaceholderOnly) image.color = GameIcons.SlotTint(slot, placeholderTint);
            else if (entry.overrideColor) image.color = entry.color;
        }
    }
}
