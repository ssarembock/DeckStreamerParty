using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>Tiny helpers for the overlays that build their own visuals at runtime (fonts follow the UI Skin).</summary>
    public static class UiFactory
    {
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static RectTransform Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            return rect;
        }

        public static Image Image(string name, Transform parent, Color color, Sprite sprite = null, bool sliced = false)
        {
            var rect = Rect(name, parent);
            rect.gameObject.AddComponent<CanvasRenderer>();
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            if (sliced && sprite != null) image.type = UnityEngine.UI.Image.Type.Sliced;
            return image;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, float size, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center, bool header = false)
        {
            var rect = Rect(name, parent);
            rect.gameObject.AddComponent<CanvasRenderer>();
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            var skin = GameDatabase.Instance.skin;
            var font = skin != null ? (header && skin.headerFont != null ? skin.headerFont : skin.bodyFont) : null;
            if (font != null) text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.richText = true;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Min(12f, size);
            text.fontSizeMax = size;
            if (header) text.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            return text;
        }

        public static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
