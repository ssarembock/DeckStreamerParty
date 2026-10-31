using TMPro;
using UnityEngine;

namespace DeckParty.UI
{
    /// <summary>Applies the UI Skin's font to this text (if a font is set on the skin).</summary>
    [ExecuteAlways]
    public sealed class SkinnedText : MonoBehaviour
    {
        [SerializeField] internal bool header;

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
            var font = header && skin.headerFont != null ? skin.headerFont : skin.bodyFont;
            var text = GetComponent<TMP_Text>();
            if (font != null && text != null) text.font = font;
        }
    }
}
