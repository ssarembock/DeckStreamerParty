using System.Linq;
using DeckParty.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace DeckParty.EditorTools
{
    /// <summary>Pieces every Deck Party scene shares: camera, event system, canvases, background and overlays.</summary>
    internal static class SceneCommon
    {
        public static void CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = UIPalette.Night;
            camera.transform.position = new Vector3(0, 0, -10);
            go.AddComponent<AudioListener>();
        }

        public static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            var module = go.AddComponent<InputSystemUIInputModule>();
            AssignDefaultUIActions(module);
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

#if ENABLE_INPUT_SYSTEM
        const string DefaultActionsPath = "Packages/com.unity.inputsystem/InputSystem/Plugins/PlayerInput/DefaultInputActions.inputactions";

        /// <summary>
        /// Points the UI module at the Input System package's saved DefaultInputActions asset (what Unity's own
        /// "Create > UI > Event System" does). Don't use InputSystemUIInputModule.AssignDefaultActions() here: it builds
        /// an in-memory asset that can't be saved into a scene and is unloaded when the builder opens the next scene.
        /// If the asset can't be found, the module is left empty and assigns built-in defaults itself at runtime.
        /// </summary>
        static void AssignDefaultUIActions(InputSystemUIInputModule module)
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(DefaultActionsPath);
            if (asset == null) return;

            var references = AssetDatabase.LoadAllAssetsAtPath(DefaultActionsPath).OfType<InputActionReference>().ToList();
            InputActionReference Find(string actionName) => references.FirstOrDefault(r =>
                r != null && r.action != null && r.action.actionMap != null && r.action.actionMap.name == "UI" && r.action.name == actionName);

            module.actionsAsset = asset;
            module.point = Find("Point");
            module.leftClick = Find("Click");
            module.rightClick = Find("RightClick");
            module.middleClick = Find("MiddleClick");
            module.scrollWheel = Find("ScrollWheel");
            module.move = Find("Navigate");
            module.submit = Find("Submit");
            module.cancel = Find("Cancel");
            module.trackedDevicePosition = Find("TrackedDevicePosition");
            module.trackedDeviceOrientation = Find("TrackedDeviceOrientation");
        }
#endif

        public static RectTransform CreateCanvas(string name, int sortingOrder, bool interactive = true)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = EditorUI.UILayer;
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            canvas.pixelPerfect = false;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.referencePixelsPerUnit = 100;

            if (interactive) go.AddComponent<GraphicRaycaster>();
            return (RectTransform)go.transform;
        }

        public static VenueBackgroundView CreateBackground(Transform canvas)
        {
            var root = EditorUI.Rect(canvas, "Venue").Stretch();

            var gradientRect = EditorUI.Rect(root, "Gradient").Stretch();
            var gradient = gradientRect.gameObject.AddComponent<RawImage>();
            gradient.color = UIPalette.Night;
            gradient.raycastTarget = false;

            var artRect = EditorUI.Rect(root, "Art").Stretch();
            var art = artRect.gameObject.AddComponent<Image>();
            art.raycastTarget = false;
            art.enabled = false;
            var fitter = artRect.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9f;

            var decor = EditorUI.Rect(root, "Decor").Stretch();
            var lights = EditorUI.Rect(root, "Lights").Stretch();

            var foreground = EditorUI.Image(root, "Foreground", Color.white);
            foreground.rectTransform.Stretch();
            foreground.enabled = false;

            var tint = EditorUI.Image(root, "Tint", new Color(0, 0, 0, 0));
            tint.rectTransform.Stretch();

            var readability = EditorUI.Image(root, "Readability", new Color(0.03f, 0.02f, 0.08f, 0.28f));
            readability.rectTransform.Stretch();

            var view = root.gameObject.AddComponent<VenueBackgroundView>();
            view.gradient = gradient;
            view.art = art;
            view.artFitter = fitter;
            view.decorRoot = decor;
            view.lightsRoot = lights;
            view.foreground = foreground;
            view.tint = tint;
            return view;
        }

        /// <summary>Header title + subtitle at the top-left of a screen.</summary>
        public static TextMeshProUGUI Header(Transform screen, string title, string subtitle)
        {
            var t = EditorUI.Text(screen, "Title", title, 64, UIPalette.Text, TextAlignmentOptions.MidlineLeft, true, true, false);
            t.rectTransform.Place(new Vector2(0, 1), new Vector2(80, -36), new Vector2(1100, 80), new Vector2(0, 1));
            if (!string.IsNullOrEmpty(subtitle))
            {
                var s = EditorUI.Text(screen, "Subtitle", subtitle, 26, UIPalette.TextDim, TextAlignmentOptions.MidlineLeft, false, false, false);
                s.rectTransform.Place(new Vector2(0, 1), new Vector2(82, -112), new Vector2(1300, 40), new Vector2(0, 1));
            }
            return t;
        }

        public static RectTransform Screen(Transform parent, string name)
        {
            var screen = EditorUI.Rect(parent, name).Stretch();
            screen.gameObject.AddComponent<CanvasGroup>();
            return screen;
        }

        public static Button BackButton(Transform screen)
        {
            var back = EditorUI.Button(screen, "Back", "common.back", ButtonStyle.Ghost, 28);
            back.GetComponent<RectTransform>().Place(new Vector2(0, 0), new Vector2(60, 40), new Vector2(220, 80), new Vector2(0, 0));
            return back;
        }

        public static EventLogView LogList(Transform parent, string name, TMP_Text linePrefab, int maxLines)
        {
            var scroll = EditorUI.Scroll(parent, name, out var content, 6, EditorUI.Pad(8, 6));
            var log = scroll.gameObject.AddComponent<EventLogView>();
            log.scroll = scroll;
            log.content = content;
            log.linePrefab = linePrefab;
            log.maxLines = maxLines;
            return log;
        }

        public static ChatView Chat(RectTransform parent, float top, TMP_Text linePrefab)
        {
            var host = EditorUI.Rect(parent, "ChatView").Box(Vector2.zero, Vector2.one, new Vector2(14, 14), new Vector2(-14, -top));
            var log = LogList(host, "Messages", linePrefab, 80);
            log.GetComponent<RectTransform>().Box(Vector2.zero, Vector2.one, new Vector2(0, 70), Vector2.zero);

            var row = EditorUI.Rect(host, "InputRow").BottomBand(0, 60);
            EditorUI.HLayout(row, 10, EditorUI.Pad(0), TextAnchor.MiddleCenter, true, true, false, true);
            var input = EditorUI.Input(row, "Input", "lobby.chat_placeholder", 22, 100);
            EditorUI.Layout(input, -1, 60, 1);
            var send = EditorUI.Button(row, "Send", "lobby.send", ButtonStyle.Primary, 24);
            EditorUI.Layout(send, 120, 60, 0);

            var chat = host.gameObject.AddComponent<ChatView>();
            chat.log = log;
            chat.input = input;
            chat.sendButton = send;
            return chat;
        }

        /// <summary>Modal dialog, toast and tooltip, in that order (tooltip last so it draws on top).</summary>
        public static void CreateOverlays(Transform overlay)
        {
            // Modal dialog
            var host = EditorUI.ModalShell(overlay, "ModalDialog", new Vector2(940, 460), out var root, out var panel);
            var title = EditorUI.Text(panel, "Title", "Title", 44, UIPalette.Text, TextAlignmentOptions.Center, true, true, false);
            title.rectTransform.TopBand(32, 60, 40, 40);
            var message = EditorUI.Text(panel, "Message", "Message", 28, UIPalette.TextDim, TextAlignmentOptions.Center);
            message.rectTransform.Box(Vector2.zero, Vector2.one, new Vector2(56, 136), new Vector2(-56, -104));
            var buttons = EditorUI.Rect(panel, "Buttons").BottomBand(36, 84, 70, 70);
            EditorUI.HLayout(buttons, 24, EditorUI.Pad(0), TextAnchor.MiddleCenter, true, true, true, true);
            var cancel = EditorUI.Button(buttons, "Cancel", "common.cancel", ButtonStyle.Normal, 28, out var cancelLabel);
            var confirm = EditorUI.Button(buttons, "Confirm", "common.ok", ButtonStyle.Primary, 28, out var confirmLabel);
            // The dialog sets these per use, so they must not be overwritten by their localized captions.
            Object.DestroyImmediate(cancelLabel.GetComponent<LocalizedLabel>());
            Object.DestroyImmediate(confirmLabel.GetComponent<LocalizedLabel>());
            var dialog = host.gameObject.AddComponent<ModalDialog>();
            dialog.root = root;
            dialog.titleLabel = title;
            dialog.messageLabel = message;
            dialog.confirmButton = confirm;
            dialog.confirmLabel = confirmLabel;
            dialog.cancelButton = cancel;
            dialog.cancelLabel = cancelLabel;

            // Toast
            var toastRect = EditorUI.Rect(overlay, "Toast").Place(new Vector2(0.5f, 1), new Vector2(0, -22), new Vector2(1180, 84), new Vector2(0.5f, 1));
            var group = toastRect.gameObject.AddComponent<CanvasGroup>();
            var toastBg = EditorUI.Image(toastRect, "Background", UIPalette.PanelDeep, EditorUI.Rounded, SkinSlot.Tooltip);
            toastBg.rectTransform.Stretch();
            var accent = EditorUI.Image(toastRect, "Accent", UIPalette.Teal, EditorUI.RoundedSmall);
            accent.rectTransform.Box(new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 8), new Vector2(12, -8));
            var toastLabel = EditorUI.Text(toastRect, "Label", string.Empty, 26, UIPalette.Text, TextAlignmentOptions.Center);
            toastLabel.rectTransform.Stretch(30, 8, 20, 8);
            toastLabel.enableAutoSizing = true;
            toastLabel.fontSizeMin = 16;
            toastLabel.fontSizeMax = 26;
            var toast = toastRect.gameObject.AddComponent<ToastView>();
            toast.group = group;
            toast.label = toastLabel;
            toast.accent = accent;

            // Tooltip
            var tipHost = EditorUI.Rect(overlay, "Tooltip").Stretch();
            var tipGroup = tipHost.gameObject.AddComponent<CanvasGroup>();
            var tipPanel = EditorUI.Image(tipHost, "Panel", UIPalette.PanelDeep.WithAlpha(0.98f), EditorUI.Rounded, SkinSlot.Tooltip);
            tipPanel.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            tipPanel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            tipPanel.rectTransform.pivot = new Vector2(0, 1);
            tipPanel.rectTransform.sizeDelta = new Vector2(440, 120);
            EditorUI.VLayout(tipPanel, 0, EditorUI.Pad(20, 16), TextAnchor.UpperLeft, true, true, true, false);
            EditorUI.Fit(tipPanel, false, true);
            var tipLabel = EditorUI.Text(tipPanel.transform, "Label", string.Empty, 21, UIPalette.Text, TextAlignmentOptions.TopLeft);
            var tooltip = tipHost.gameObject.AddComponent<TooltipView>();
            tooltip.panel = tipPanel.rectTransform;
            tooltip.label = tipLabel;
            tooltip.group = tipGroup;
        }

        /// <summary>
        /// The Display &amp; Language block (language, window mode, resolution + Apply, screen shake, reduce flashing, fake chat),
        /// shared by the Settings screen and the in-game menu.
        /// </summary>
        public static DisplaySettingsView DisplayBlock(Transform parent, float labelWidth = 260f)
        {
            var root = EditorUI.Rect(parent, "Display");
            EditorUI.VLayout(root, 10, EditorUI.Pad(0), TextAnchor.UpperCenter, true, true, true, false);

            var language = EditorUI.Selector(root, "Language", "settings.language");
            EditorUI.Layout(language, -1, 96);
            var mode = EditorUI.Selector(root, "WindowMode", "settings.window_mode");
            EditorUI.Layout(mode, -1, 96);

            var resolutionRow = EditorUI.Rect(root, "ResolutionRow");
            EditorUI.Layout(resolutionRow, -1, 96);
            EditorUI.HLayout(resolutionRow, 12, EditorUI.Pad(0), TextAnchor.LowerCenter, true, true, false, false);
            var resolution = EditorUI.Selector(resolutionRow, "Resolution", "settings.resolution");
            EditorUI.Layout(resolution, -1, 96, 1);
            var apply = EditorUI.Button(resolutionRow, "Apply", "settings.apply", ButtonStyle.Secondary, 22, out var applyLabel);
            EditorUI.Layout(apply, 200, 60, 0);
            Object.DestroyImmediate(applyLabel.GetComponent<LocalizedLabel>());

            var hint = EditorUI.Text(root, "Hint", "settings.fullscreen_hint", 18, UIPalette.TextDim, TextAlignmentOptions.MidlineLeft, false, false, false);
            EditorUI.Layout(hint, -1, 24);

            var shake = MainMenuSceneBuilder.SliderRow(root, "Shake", "settings.shake", labelWidth);
            var reduce = EditorUI.Toggle(root, "ReduceFlashing", "settings.reduce_flashing");
            EditorUI.Layout(reduce, -1, 56);
            var fakeChat = EditorUI.Toggle(root, "FakeChat", "settings.fake_chat");
            EditorUI.Layout(fakeChat, -1, 56);

            var view = root.gameObject.AddComponent<DisplaySettingsView>();
            view.languageSelector = language;
            view.modeSelector = mode;
            view.resolutionSelector = resolution;
            view.applyButton = apply;
            view.applyLabel = applyLabel;
            view.shakeSlider = shake;
            view.reduceFlashingToggle = reduce;
            view.fakeChatToggle = fakeChat;
            return view;
        }
    }
}
