using UnityEngine;

namespace DeckParty.UI
{
    /// <summary>
    /// Marks a named panel of the stream or shop (menu bar, Stream Chat, shop...). Everything under it that's coloured by the
    /// UI Skin's Site Colours takes this panel's own colours where it overrides them (UI Skin > Site Colours > Panels).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SiteAreaTag : MonoBehaviour
    {
        [Tooltip("Which panel this is in UI Skin > Site Colours.")]
        [SerializeField] internal SiteArea area;

        public SiteArea Area
        {
            get => area;
            set
            {
                area = value;
                RefreshChildren();
            }
        }

        void OnValidate()
        {
#if UNITY_EDITOR
            // Not during OnValidate itself (the children may still be loading).
            UnityEditor.EditorApplication.delayCall += RefreshChildren;
#endif
        }

        /// <summary>Re-colours everything under this panel (after its area changed).</summary>
        public void RefreshChildren()
        {
            if (this == null) return;
            foreach (var colour in GetComponentsInChildren<SiteColour>(true)) colour.Refresh();
            foreach (var image in GetComponentsInChildren<SkinnedImage>(true)) image.Apply();
        }
    }
}
