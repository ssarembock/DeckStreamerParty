using DeckParty.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.EditorTools
{
    public enum ButtonStyle
    {
        Normal,
        Primary,
        Secondary,
        Danger,
        Gold,
        Ghost,
    }

    /// <summary>Helpers for building uGUI hierarchies from code (used by the prefab and scene builders).</summary>
    internal static class EditorUI
    {
        public const int UILayer = 5;

        public static TMP_FontAsset Font;
        public static Sprite Rounded;
        public static Sprite RoundedSmall;
        public static Sprite Frame;
        public static Sprite Circle;
        public static Sprite Glow;
        public static Sprite Star;
        public static Sprite Cross;
        public static Sprite Shield;
        public static Sprite Sparkle;
        public static Sprite Coin;
        public static Sprite Door;
        public static Sprite Crown;
        public static Sprite Check;

        public static void LoadArt()
        {
            // Only touch TMP_Settings once it exists; its getters throw if the essentials aren't imported yet.
            var settings = Resources.Load<TMP_Settings>("TMP Settings");
            Font = settings != null ? TMP_Settings.defaultFontAsset : null;
            if (Font == null) Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            // The multi-language body font (Noto Sans with a Chinese fallback) once Localization setup has made it.
            var skin = AssetDatabase.LoadAssetAtPath<UISkin>(DeckPartyPaths.Skin);
            if (skin != null && skin.bodyFont != null) Font = skin.bodyFont;

            Rounded = PlaceholderArt.Load(PlaceholderArt.RoundedName);
            RoundedSmall = PlaceholderArt.Load(PlaceholderArt.RoundedSmallName);
            Frame = PlaceholderArt.Load(PlaceholderArt.FrameName);
            Circle = PlaceholderArt.Load(PlaceholderArt.CircleName);
            Glow = PlaceholderArt.Load(PlaceholderArt.GlowName);
            Star = PlaceholderArt.Load(PlaceholderArt.StarName);
            Cross = PlaceholderArt.Load(PlaceholderArt.CrossName);
            Shield = PlaceholderArt.Load(PlaceholderArt.ShieldName);
            Sparkle = PlaceholderArt.Load(PlaceholderArt.SparkleName);
            Coin = PlaceholderArt.Load(PlaceholderArt.CoinName);
            Door = PlaceholderArt.Load(PlaceholderArt.DoorName);
            Crown = PlaceholderArt.Load(PlaceholderArt.CrownName);
            Check = PlaceholderArt.Load(PlaceholderArt.CheckName);
        }

        // ------------------------------------------------------------------ rects

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = UILayer;
            var rt = (RectTransform)go.transform;
            if (parent != null) rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>Stretch to the parent with insets (left, top, right, bottom).</summary>
        public static RectTransform Stretch(this RectTransform rt, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        /// <summary>Fixed-size rect at an anchor point. The pivot defaults to the anchor.</summary>
        public static RectTransform Place(this RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot ?? anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>Anchor box with pixel offsets (offsetMin = left/bottom, offsetMax = right/top, usually negative).</summary>
        public static RectTransform Box(this RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        /// <summary>A band glued to the top of the parent: from <paramref name="top"/> down <paramref name="height"/> pixels.</summary>
        public static RectTransform TopBand(this RectTransform rt, float top, float height, float left = 0, float right = 0)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(left, -top - height);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        /// <summary>A band glued to the bottom of the parent.</summary>
        public static RectTransform BottomBand(this RectTransform rt, float bottom, float height, float left = 0, float right = 0)
        {
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, bottom + height);
            return rt;
        }

        // ------------------------------------------------------------------ graphics

        public static Image Image(Transform parent, string name, Color color, Sprite sprite = null, SkinSlot? slot = null, bool raycast = false)
        {
            var rt = Rect(parent, name);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycast;
            if (sprite != null && sprite.border != Vector4.zero) image.type = UnityEngine.UI.Image.Type.Sliced;
            if (slot.HasValue) rt.gameObject.AddComponent<SkinnedImage>().slot = slot.Value;
            return image;
        }

        public static Image Panel(Transform parent, string name, Color? color = null, SkinSlot slot = SkinSlot.Panel, bool raycast = true)
        {
            return Image(parent, name, color ?? UIPalette.Panel, Rounded, slot, raycast);
        }

        public static Image Icon(Transform parent, string name, Sprite sprite, Color color, SkinSlot slot, float size)
        {
            var image = Image(parent, name, color, sprite, slot);
            image.preserveAspect = true;
            image.rectTransform.sizeDelta = new Vector2(size, size);
            return image;
        }

        public static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center, bool bold = false, bool header = false, bool wrap = true)
        {
            var rt = Rect(parent, name);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (Font != null) t.font = Font;
            // Captions written as keys ("menu.quit") show their English text and follow the in-game language.
            if (EditorLoc.IsKey(text))
            {
                rt.gameObject.AddComponent<LocalizedLabel>().key = text;
                text = EditorLoc.Get(text);
            }
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.raycastTarget = false;
            t.richText = true;
            t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            // Translations run 20-35% longer than English: every caption may shrink a little to fit.
            t.enableAutoSizing = true;
            t.fontSizeMax = size;
            t.fontSizeMin = Mathf.Max(11f, Mathf.Round(size * 0.6f));
            rt.gameObject.AddComponent<SkinnedText>().header = header;
            return t;
        }

        public static Color ColorFor(ButtonStyle style)
        {
            switch (style)
            {
                case ButtonStyle.Primary: return UIPalette.Pink;
                case ButtonStyle.Secondary: return UIPalette.Purple;
                case ButtonStyle.Danger: return UIPalette.Red;
                case ButtonStyle.Gold: return UIPalette.Gold;
                case ButtonStyle.Ghost: return UIPalette.PanelDeep;
                default: return UIPalette.PanelAlt;
            }
        }

        static SkinSlot SlotFor(ButtonStyle style)
        {
            switch (style)
            {
                case ButtonStyle.Primary:
                case ButtonStyle.Gold: return SkinSlot.ButtonPrimary;
                case ButtonStyle.Secondary: return SkinSlot.ButtonSecondary;
                case ButtonStyle.Danger: return SkinSlot.ButtonDanger;
                default: return SkinSlot.Button;
            }
        }

        public static Button Button(Transform parent, string name, string label, ButtonStyle style, float fontSize, out TextMeshProUGUI labelText)
        {
            var image = Image(parent, name, ColorFor(style), Rounded, SlotFor(style), true);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = new Color(0.94f, 0.94f, 0.94f, 1f);
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.selectedColor = new Color(0.94f, 0.94f, 0.94f, 1f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.6f, 0.55f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            NoNavigation(button);
            image.gameObject.AddComponent<ButtonFeedback>();

            var textColor = style == ButtonStyle.Gold ? UIPalette.Night : UIPalette.Text;
            labelText = Text(image.transform, "Label", label, fontSize, textColor, TextAlignmentOptions.Center, true, false, false);
            labelText.rectTransform.Stretch(12, 4, 12, 4);
            return button;
        }

        public static Button Button(Transform parent, string name, string label, ButtonStyle style, float fontSize = 28f)
        {
            return Button(parent, name, label, style, fontSize, out _);
        }

        public static void NoNavigation(Selectable selectable)
        {
            var nav = selectable.navigation;
            nav.mode = Navigation.Mode.None;
            selectable.navigation = nav;
        }

        // ------------------------------------------------------------------ layout

        public static VerticalLayoutGroup VLayout(Component target, float spacing, RectOffset padding = null, TextAnchor align = TextAnchor.UpperCenter,
            bool controlWidth = true, bool controlHeight = true, bool expandWidth = true, bool expandHeight = false)
        {
            var layout = target.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding ?? new RectOffset(0, 0, 0, 0);
            layout.childAlignment = align;
            layout.childControlWidth = controlWidth;
            layout.childControlHeight = controlHeight;
            layout.childForceExpandWidth = expandWidth;
            layout.childForceExpandHeight = expandHeight;
            return layout;
        }

        public static HorizontalLayoutGroup HLayout(Component target, float spacing, RectOffset padding = null, TextAnchor align = TextAnchor.MiddleCenter,
            bool controlWidth = true, bool controlHeight = true, bool expandWidth = false, bool expandHeight = true)
        {
            var layout = target.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding ?? new RectOffset(0, 0, 0, 0);
            layout.childAlignment = align;
            layout.childControlWidth = controlWidth;
            layout.childControlHeight = controlHeight;
            layout.childForceExpandWidth = expandWidth;
            layout.childForceExpandHeight = expandHeight;
            return layout;
        }

        public static LayoutElement Layout(Component target, float preferredWidth = -1, float preferredHeight = -1, float flexibleWidth = -1, float flexibleHeight = -1,
            float minWidth = -1, float minHeight = -1)
        {
            var element = target.gameObject.GetComponent<LayoutElement>();
            if (element == null) element = target.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = preferredWidth;
            element.preferredHeight = preferredHeight;
            element.flexibleWidth = flexibleWidth;
            element.flexibleHeight = flexibleHeight;
            element.minWidth = minWidth;
            element.minHeight = minHeight;
            return element;
        }

        public static ContentSizeFitter Fit(Component target, bool horizontal, bool vertical)
        {
            var fitter = target.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = horizontal ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = vertical ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
            return fitter;
        }

        public static RectOffset Pad(int all) => new RectOffset(all, all, all, all);
        public static RectOffset Pad(int horizontal, int vertical) => new RectOffset(horizontal, horizontal, vertical, vertical);

        // ------------------------------------------------------------------ composite controls

        /// <summary>Vertical (or grid) scroll view. Returns the ScrollRect; the content transform is in <paramref name="content"/>.</summary>
        public static ScrollRect Scroll(Transform parent, string name, out RectTransform content, float spacing = 8f, RectOffset padding = null,
            Vector2? gridCell = null)
        {
            var root = Image(parent, name, new Color(0, 0, 0, 0), null, null, true);
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.12f;

            var viewport = Rect(root.transform, "Viewport").Stretch(0, 0, 16, 0);
            viewport.gameObject.AddComponent<RectMask2D>();

            content = Rect(viewport, "Content");
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;

            if (gridCell.HasValue)
            {
                var grid = content.gameObject.AddComponent<GridLayoutGroup>();
                grid.cellSize = gridCell.Value;
                grid.spacing = new Vector2(spacing, spacing);
                grid.padding = padding ?? Pad(8);
                grid.childAlignment = TextAnchor.UpperCenter;
                grid.constraint = GridLayoutGroup.Constraint.Flexible;
            }
            else
            {
                VLayout(content, spacing, padding ?? Pad(4), TextAnchor.UpperCenter, true, true, true, false);
            }
            Fit(content, false, true);

            var bar = Image(root.transform, "Scrollbar", UIPalette.PanelDeep, RoundedSmall, null, true);
            bar.rectTransform.anchorMin = new Vector2(1, 0);
            bar.rectTransform.anchorMax = new Vector2(1, 1);
            bar.rectTransform.pivot = new Vector2(1, 0.5f);
            bar.rectTransform.sizeDelta = new Vector2(10, 0);
            bar.rectTransform.anchoredPosition = Vector2.zero;
            var slidingArea = Rect(bar.transform, "Sliding Area").Stretch();
            var handle = Image(slidingArea, "Handle", UIPalette.Border, RoundedSmall, SkinSlot.ScrollHandle, true);
            handle.rectTransform.Stretch();
            var scrollbar = bar.gameObject.AddComponent<Scrollbar>();
            scrollbar.handleRect = handle.rectTransform;
            scrollbar.targetGraphic = handle;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            NoNavigation(scrollbar);

            scroll.viewport = viewport;
            scroll.content = content;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scroll.verticalScrollbarSpacing = 4f;
            return scroll;
        }

        public static TMP_InputField Input(Transform parent, string name, string placeholder, float fontSize = 26f, int characterLimit = 32)
        {
            var background = Image(parent, name, UIPalette.PanelDeep, Rounded, SkinSlot.InputField, true);
            var field = background.gameObject.AddComponent<TMP_InputField>();

            var area = Rect(background.transform, "Text Area").Stretch(18, 6, 18, 6);
            area.gameObject.AddComponent<RectMask2D>();

            var hint = Text(area, "Placeholder", placeholder, fontSize, UIPalette.TextDim, TextAlignmentOptions.MidlineLeft, false, false, false);
            hint.fontStyle = FontStyles.Italic;
            hint.rectTransform.Stretch();
            var text = Text(area, "Text", string.Empty, fontSize, UIPalette.Text, TextAlignmentOptions.MidlineLeft, false, false, false);
            text.rectTransform.Stretch();

            field.textViewport = area;
            field.textComponent = text;
            field.placeholder = hint;
            field.targetGraphic = background;
            if (Font != null) field.fontAsset = Font;
            field.pointSize = fontSize;
            field.customCaretColor = true;
            field.caretColor = UIPalette.Text;
            field.selectionColor = UIPalette.Pink.WithAlpha(0.45f);
            field.characterLimit = characterLimit;
            field.lineType = TMP_InputField.LineType.SingleLine;
            return field;
        }

        public static Slider Slider(Transform parent, string name)
        {
            var resources = new DefaultControls.Resources { standard = Rounded, background = Rounded, knob = Circle };
            var go = DefaultControls.CreateSlider(resources);
            go.name = name;
            go.layer = UILayer;
            foreach (Transform child in go.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = UILayer;
            go.transform.SetParent(parent, false);

            var slider = go.GetComponent<Slider>();
            NoNavigation(slider);
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;

            var background = go.transform.Find("Background").GetComponent<Image>();
            background.color = UIPalette.PanelDeep;
            var fill = go.transform.Find("Fill Area/Fill").GetComponent<Image>();
            fill.color = UIPalette.Pink;
            var handle = go.transform.Find("Handle Slide Area/Handle").GetComponent<Image>();
            handle.color = UIPalette.Text;
            ((RectTransform)handle.transform).sizeDelta = new Vector2(34, 0);
            return slider;
        }

        public static ToggleSwitch Toggle(Transform parent, string name, string label)
        {
            var row = Rect(parent, name);
            HLayout(row, 16, Pad(0), TextAnchor.MiddleLeft, true, true, false, true);
            var text = Text(row, "Label", label, 28, UIPalette.Text, TextAlignmentOptions.MidlineLeft, false, false, false);
            Layout(text, -1, -1, 1);
            var button = Button(row, "Switch", "OFF", ButtonStyle.Normal, 24, out var stateLabel);
            Layout(button, 140, 56, 0);

            var toggle = row.gameObject.AddComponent<ToggleSwitch>();
            toggle.button = button;
            toggle.stateLabel = stateLabel;
            toggle.fill = button.GetComponent<Image>();
            return toggle;
        }

        public static OptionSelector Selector(Transform parent, string name, string title)
        {
            var root = Rect(parent, name);
            VLayout(root, 6, Pad(0), TextAnchor.UpperCenter, true, true, true, false);

            var titleText = Text(root, "Title", title, 22, UIPalette.TextDim, TextAlignmentOptions.MidlineLeft, true, false, false);
            Layout(titleText, -1, 28);

            var row = Rect(root, "Row");
            Layout(row, -1, 60);
            HLayout(row, 8, Pad(0), TextAnchor.MiddleCenter, true, true, false, true);

            var prev = Button(row, "Prev", "<", ButtonStyle.Normal, 30);
            Layout(prev, 60, 60, 0);
            var valueBox = Image(row, "Value", UIPalette.PanelDeep, Rounded, SkinSlot.InputField);
            Layout(valueBox, -1, 60, 1);
            var valueText = Text(valueBox.transform, "Label", "-", 24, UIPalette.Text, TextAlignmentOptions.Center, true, false, false);
            valueText.rectTransform.Stretch(8, 2, 8, 2);
            valueText.overflowMode = TextOverflowModes.Ellipsis;
            var next = Button(row, "Next", ">", ButtonStyle.Normal, 30);
            Layout(next, 60, 60, 0);

            var selector = root.gameObject.AddComponent<OptionSelector>();
            selector.prevButton = prev;
            selector.nextButton = next;
            selector.valueLabel = valueText;
            selector.titleLabel = titleText;
            return selector;
        }

        /// <summary>A labelled row: fixed-width label then a flexible control slot.</summary>
        public static RectTransform LabelledRow(Transform parent, string name, string label, float height, out RectTransform slot, float labelWidth = 300)
        {
            var row = Rect(parent, name);
            Layout(row, -1, height);
            HLayout(row, 16, Pad(0), TextAnchor.MiddleLeft, true, true, false, true);
            var text = Text(row, "Label", label, 28, UIPalette.Text, TextAlignmentOptions.MidlineLeft, false, false, false);
            Layout(text, labelWidth, -1, 0);
            slot = Rect(row, "Control");
            Layout(slot, -1, -1, 1);
            return row;
        }

        public static TooltipTrigger Tooltip(Component target, string text = null)
        {
            var trigger = target.gameObject.AddComponent<TooltipTrigger>();
            trigger.text = text;
            return trigger;
        }

        /// <summary>Full-screen modal shell: a component host (always active) with a Root child holding a dimmer and a centred panel.</summary>
        public static RectTransform ModalShell(Transform parent, string name, Vector2 panelSize, out GameObject root, out RectTransform panel)
        {
            var host = Rect(parent, name).Stretch();
            var rootRect = Rect(host, "Root").Stretch();
            root = rootRect.gameObject;
            var dim = Image(rootRect, "Dim", new Color(0.02f, 0.01f, 0.05f, 0.72f), null, null, true);
            dim.rectTransform.Stretch();
            var panelImage = Panel(rootRect, "Panel", UIPalette.PanelDeep, SkinSlot.Panel, true);
            panel = panelImage.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, panelSize);
            return host;
        }
    }
}
