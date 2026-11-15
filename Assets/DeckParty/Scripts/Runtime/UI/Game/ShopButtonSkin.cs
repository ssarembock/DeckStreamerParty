using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// Gives one shop toolbar button its look from UI Skin > Shop: icon (art, colour, size), shape (background sprite, fill,
    /// border, size) and text (built-in numbers, translated words, your own text or none), plus the size and gap of the price
    /// above it. Applies live in the editor and while playing. The views pass the built-in caption through
    /// <see cref="SetCaption"/>; the built-in look is stored here by the scene builder, so clearing a setting brings it back.
    /// </summary>
    [ExecuteAlways]
    public sealed class ShopButtonSkin : MonoBehaviour
    {
        [SerializeField] internal ShopButton button;
        [Tooltip("The button's outline (on white buttons). Empty on filled buttons (Use, Go Live).")]
        [SerializeField] internal Image border;
        [Tooltip("The button's inside (the button's own image on filled buttons).")]
        [SerializeField] internal Image fill;
        [SerializeField] internal Image icon;
        [SerializeField] internal TMP_Text label;
        [Tooltip("The price above the button (optional).")]
        [SerializeField] internal TMP_Text cost;

        [Header("Built-in look (set by the scene builder)")]
        [SerializeField] internal Sprite defaultShape;
        [SerializeField] internal Color defaultFill = Color.white;
        [SerializeField] internal Color defaultBorder = Color.black;
        [SerializeField] internal float defaultBorderWidth = 2.5f;
        [SerializeField] internal Vector2 defaultSize = new Vector2(200, 96);
        [SerializeField] internal float defaultIconSize = 46f;
        [Tooltip("The smallest space between the icon (or text) and the button's sides. The icon and text are centred together.")]
        [SerializeField] internal float iconInset = 20f;
        [SerializeField] internal Color defaultIconColour = Color.black;
        [SerializeField] internal Sprite defaultIcon;
        [SerializeField] internal float defaultTextSize = 28f;
        [SerializeField] internal Color defaultTextColour = Color.black;
        [SerializeField] internal ShopButtonText defaultText = ShopButtonText.BuiltIn;
        [SerializeField] internal float defaultCostSize = 26f;
        [SerializeField] internal float defaultCostGap = 6f;
        [Tooltip("What the button says in the editor before the game writes it (the built-in caption).")]
        [SerializeField] internal string previewCaption = string.Empty;

        string m_BuiltIn;
        bool m_HasBuiltIn;

        public ShopButton Button => button;

        void OnEnable()
        {
            UISkin.SkinChanged += Apply;
            Apply();
        }

        void OnDisable() => UISkin.SkinChanged -= Apply;

        /// <summary>The game's own caption for this button (shown when UI Skin > Shop sets its Text to Built-in).</summary>
        public void SetCaption(string builtIn)
        {
            m_BuiltIn = builtIn ?? string.Empty;
            m_HasBuiltIn = true;
            ApplyText(Look());
        }

        ShopButtonLook Look()
        {
            var shop = ShopLook.Current;
            return shop != null ? shop.Get(button) : null;
        }

        public void Apply()
        {
            if (this == null) return;
            var look = Look();
            ApplySize(look);
            ApplyShape(look);
            ApplyIcon(look);
            ApplyText(look);
            ApplyCost();
        }

        /// <summary>The button's size: UI Skin > Shop's, or the built-in one.</summary>
        Vector2 Size(ShopButtonLook look)
        {
            var size = defaultSize;
            if (look != null && look.size.x > 0f) size.x = look.size.x;
            if (look != null && look.size.y > 0f) size.y = look.size.y;
            return size;
        }

        void ApplySize(ShopButtonLook look)
        {
            var size = Size(look);
            var rect = (RectTransform)transform;
            if (rect.sizeDelta != size) rect.sizeDelta = size;
            if (TryGetComponent<LayoutElement>(out var layout))
            {
                layout.preferredWidth = size.x;
                layout.preferredHeight = size.y;
            }
        }

        void ApplyShape(ShopButtonLook look)
        {
            var sprite = look != null && look.shape != null ? look.shape : defaultShape;
            float scale = look != null && look.shape != null ? Mathf.Max(0.01f, look.shapeScale) : 1f;
            float width = look != null && look.borderWidth >= 0f ? look.borderWidth : defaultBorderWidth;

            if (border != null)
            {
                SetSprite(border, sprite, scale);
                // The outline is also what you click, so a hidden border stays (see-through) instead of switching off.
                var colour = look != null ? look.border.Or(defaultBorder) : defaultBorder;
                border.color = width > 0f ? colour : new Color(colour.r, colour.g, colour.b, 0f);
            }
            if (fill != null)
            {
                SetSprite(fill, sprite, scale);
                if (border != null && fill != border)
                {
                    var rect = fill.rectTransform;
                    float inset = Mathf.Max(0f, width);
                    rect.offsetMin = new Vector2(inset, inset);
                    rect.offsetMax = new Vector2(-inset, -inset);
                }
                bool own = look != null && look.fill.use;
                // The accent buttons (Use, Go Live) take the site accent unless you give them your own colour.
                if (fill.TryGetComponent<SiteAccent>(out var accent))
                {
                    accent.enabled = !own;
                    if (own) fill.color = look.fill.colour;
                    else if (Application.isPlaying) accent.Apply();
                    else fill.color = defaultFill;
                }
                else fill.color = own ? look.fill.colour : defaultFill;
            }
        }

        static void SetSprite(Image image, Sprite sprite, float scale)
        {
            if (image.sprite != sprite) image.sprite = sprite;
            image.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.pixelsPerUnitMultiplier = scale;
        }

        void ApplyIcon(ShopButtonLook look)
        {
            if (icon == null) return;
            float size = look != null && look.iconSize > 0f ? look.iconSize : defaultIconSize;
            icon.rectTransform.sizeDelta = new Vector2(size, size);

            bool ownArt = look != null && look.icon != null;
            bool ownColour = look != null && look.iconColour.use;
            var skinned = icon.GetComponent<SkinnedImage>();
            var accent = icon.GetComponent<SiteAccent>();
            if (skinned != null) skinned.enabled = !ownArt && !ownColour;
            if (accent != null) accent.enabled = !ownColour;
            if (ownArt || ownColour)
            {
                var skin = GameDatabase.Instance.skin;
                var slotSprite = skinned != null && skin != null ? skin.GetSprite(skinned.Slot) : null;
                icon.sprite = ownArt ? look.icon : slotSprite != null ? slotSprite : defaultIcon;
                icon.color = ownColour ? look.iconColour.colour
                    : skinned != null ? GameIcons.SlotTint(skinned.Slot, skinned.PlaceholderTint) : defaultIconColour;
            }
            else if (skinned != null) skinned.Apply();
            if (accent != null && accent.enabled && Application.isPlaying) accent.Apply();
        }

        string Caption(ShopButtonLook look)
        {
            var mode = look != null ? look.text : defaultText;
            switch (mode)
            {
                case ShopButtonText.Words: return Words();
                case ShopButtonText.Custom: return look != null ? look.customText ?? string.Empty : string.Empty;
                case ShopButtonText.None: return string.Empty;
                default: return m_HasBuiltIn ? m_BuiltIn : previewCaption;
            }
        }

        string Words()
        {
            try
            {
                return Loc.Get(ShopLook.WordsKey(button));
            }
            catch
            {
                return ShopLook.WordsKey(button);
            }
        }

        void ApplyText(ShopButtonLook look)
        {
            string caption = Caption(look);
            bool hasText = !string.IsNullOrEmpty(caption) && label != null;
            if (label != null) label.gameObject.SetActive(hasText);
            if (!hasText)
            {
                Arrange(look, false);
                return;
            }
            if (label.text != caption) label.text = caption;
            float size = look != null && look.textSize > 0f ? look.textSize : defaultTextSize;
            label.fontSizeMax = size;
            label.fontSize = size;
            label.fontSizeMin = Mathf.Max(11f, Mathf.Round(size * 0.6f));
            bool ownColour = look != null && look.textColour.use;
            if (label.TryGetComponent<SiteAccent>(out var accent))
            {
                accent.enabled = !ownColour;
                if (ownColour) label.color = look.textColour.colour;
                else if (Application.isPlaying) accent.Apply();
                else label.color = defaultTextColour;
            }
            else label.color = ownColour ? look.textColour.colour : defaultTextColour;
            Arrange(look, true);
        }

        /// <summary>Space between the icon and the text.</summary>
        const float IconTextGap = 10f;

        /// <summary>
        /// Centres the icon and the text together on the button, as one group: the text's own width decides where the icon
        /// goes. Text too long for the button shrinks to fit (never closer than <see cref="iconInset"/> to the sides).
        /// </summary>
        void Arrange(ShopButtonLook look, bool hasText)
        {
            var size = Size(look);
            bool hasIcon = icon != null && icon.gameObject.activeSelf;
            float iconSize = hasIcon ? icon.rectTransform.sizeDelta.x : 0f;
            float gap = hasIcon && hasText ? IconTextGap : 0f;
            float margin = Mathf.Max(0f, iconInset);

            float textWidth = 0f;
            if (hasText)
            {
                float room = Mathf.Max(0f, size.x - 2f * margin - iconSize - gap);
                float wanted = label.GetPreferredValues(label.text, float.PositiveInfinity, size.y).x + 2f;
                textWidth = Mathf.Min(wanted, room);
            }
            float start = Mathf.Max(0f, (size.x - (iconSize + gap + textWidth)) * 0.5f);

            if (hasIcon) Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(start, 0f), new Vector2(0f, 0.5f));
            if (!hasText) return;
            var rect = label.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            float x = start + iconSize + gap;
            rect.offsetMin = new Vector2(x, 4f);
            rect.offsetMax = new Vector2(x + textWidth, -4f);
            label.horizontalAlignment = HorizontalAlignmentOptions.Center;
        }

        static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 pivot)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
        }

        void ApplyCost()
        {
            if (cost == null) return;
            var shop = ShopLook.Current;
            float size = shop != null && shop.costSize > 0f ? shop.costSize : defaultCostSize;
            float gap = shop != null && shop.costGap >= 0f ? shop.costGap : defaultCostGap;
            cost.fontSizeMax = size;
            cost.fontSize = size;
            cost.fontSizeMin = Mathf.Max(11f, Mathf.Round(size * 0.6f));
            var rect = cost.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(0f, gap);
            rect.offsetMax = new Vector2(0f, gap + size * 1.3f);
        }
    }
}
