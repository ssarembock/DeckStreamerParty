using DeckParty.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.EditorTools
{
    /// <summary>
    /// Deck Party > Shop > Create Shop Card Template: makes a starting prefab for your own shop card, with every part the game
    /// can fill in, each marked with a <see cref="ShopCardPart"/> and holding a placeholder value. Edit it (or make your own
    /// from scratch) and drop it on UI Skin > Shop > Card prefab. See ShopCardPrefab_Guidelines.md.
    /// </summary>
    internal static class ShopCardTemplate
    {
        public const string Folder = DeckPartyPaths.Root + "/Prefabs/Custom";
        /// <summary>The template's design size (the shop scales the card to fit its cell).</summary>
        static readonly Vector2 Size = new Vector2(180f, 252f);

        [MenuItem("Deck Party/Shop/Create Shop Card Template", priority = 40)]
        public static void CreateFromMenu()
        {
            var prefab = Create();
            if (prefab == null) return;
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            EditorUtility.DisplayDialog("Shop card template",
                $"Made {AssetDatabase.GetAssetPath(prefab)}.\n\nOpen it to design your card (keep the part names or their Shop Card " +
                "Part components), then drop it on UI Skin > Shop > Card prefab. ShopCardPrefab_Guidelines.md explains each part.", "OK");
        }

        /// <summary>Saves a new template prefab (never overwrites one) and returns it.</summary>
        public static GameObject Create()
        {
            EditorUI.LoadArt();
            EnsureFolder();
            var root = EditorUI.Rect(null, "ShopCardTemplate");
            root.sizeDelta = Size;

            // The pile: three card backs peeking out above the card, farthest first so the nearest draws over it.
            var pile = EditorUI.Rect(root, "Pile").Stretch();
            for (int i = 3; i >= 1; i--)
            {
                var back = EditorUI.Image(pile, "PileCard" + i, SitePalette.Outline, EditorUI.RoundedSmall);
                back.rectTransform.Box(Vector2.zero, Vector2.one, new Vector2(0, 7 * i), new Vector2(0, 7 * i));
                var inside = EditorUI.Image(back.transform, "Inside", SitePalette.Surface, EditorUI.RoundedSmall);
                inside.rectTransform.Stretch(3, 3, 3, 3);
                Mark(back, ShopCardPart.Part.PileCard, i);
            }

            // Everything that bounces when you buy the card.
            var body = EditorUI.Rect(root, "Body").Stretch();
            Mark(body, ShopCardPart.Part.AnimRoot);
            var frame = EditorUI.Image(body, "Frame", SitePalette.Outline, EditorUI.RoundedSmall);
            frame.rectTransform.Stretch();
            Mark(frame, ShopCardPart.Part.Frame);
            var background = EditorUI.Image(body, "Background", SitePalette.Surface, EditorUI.RoundedSmall, null, true);
            background.rectTransform.Stretch(3, 3, 3, 3);
            Mark(background, ShopCardPart.Part.Background);

            // The picture in its own frame (the purchase flourish flies from the frame).
            var pictureFrame = EditorUI.Image(body, "PictureFrame", SitePalette.SurfaceAlt, null);
            pictureFrame.rectTransform.Box(new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.94f), Vector2.zero, Vector2.zero);
            var placeholder = EditorUI.Image(pictureFrame.transform, "Placeholder", UIPalette.Teal, EditorUI.Circle);
            placeholder.rectTransform.Box(new Vector2(0.15f, 0.08f), new Vector2(0.85f, 0.92f), Vector2.zero, Vector2.zero);
            placeholder.preserveAspect = true;
            Mark(placeholder, ShopCardPart.Part.Placeholder);
            var initials = Text(placeholder.transform, "Initials", "AB", 40, UIPalette.Night, TextAlignmentOptions.Center, true);
            initials.rectTransform.Stretch();
            Mark(initials, ShopCardPart.Part.Initials);
            var portrait = EditorUI.Image(pictureFrame.transform, "Portrait", Color.white);
            portrait.rectTransform.Stretch(4, 4, 4, 4);
            portrait.preserveAspect = true;
            Mark(portrait, ShopCardPart.Part.Portrait);

            var communities = EditorUI.Rect(body, "Communities").Place(new Vector2(0, 1), new Vector2(8, -8), new Vector2(3 * 28 + 2 * 3, 28), new Vector2(0, 1));
            EditorUI.HLayout(communities, 3, EditorUI.Pad(0), TextAnchor.MiddleLeft, false, false, false, false);
            for (int i = 1; i <= 3; i++)
            {
                var icon = EditorUI.Image(communities, "Icon" + i, Color.white, EditorUI.Circle, null, true);
                icon.rectTransform.sizeDelta = new Vector2(28, 28);
            }
            Mark(communities, ShopCardPart.Part.Communities);
            var trophy = EditorUI.Image(body, "TrophyBadge", UIPalette.Gold, EditorUI.Star);
            trophy.rectTransform.Place(new Vector2(1, 1), new Vector2(-8, -8), new Vector2(30, 30), new Vector2(1, 1));
            trophy.preserveAspect = true;
            Mark(trophy, ShopCardPart.Part.TrophyBadge);
            var risk = EditorUI.Image(body, "RiskBadge", UIPalette.Red, EditorUI.Cross);
            risk.rectTransform.Place(new Vector2(1, 1), new Vector2(-8, -42), new Vector2(26, 26), new Vector2(1, 1));
            risk.preserveAspect = true;
            Mark(risk, ShopCardPart.Part.RiskBadge);

            var name = Text(body, "Name", "Viewer Name", 22, SitePalette.Text, TextAlignmentOptions.Center, true);
            name.rectTransform.Box(new Vector2(0.05f, 0.24f), new Vector2(0.95f, 0.35f), Vector2.zero, Vector2.zero);
            Mark(name, ShopCardPart.Part.Name);
            // What the viewer scores: a plain group holding the Clout number (left) and the Cash number (right), no icons.
            // Risk isn't here: the Risk Badge shows it.
            var stats = EditorUI.Rect(body, "Stats");
            stats.Box(new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.24f), Vector2.zero, Vector2.zero);
            AddScoreTexts(stats, SitePalette.Text, 20, true, null, null, false);
            // Available but switched off: switch it on (and make the card taller) to show what the viewer does.
            var description = Text(body, "Description", "What this viewer does when the stream ends.", 14, SitePalette.TextDim, TextAlignmentOptions.Center, false);
            description.rectTransform.Box(new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.3f), Vector2.zero, Vector2.zero);
            description.gameObject.SetActive(false);
            Mark(description, ShopCardPart.Part.Description);

            var price = Text(body, "Price", "5", 24, SitePalette.Text, TextAlignmentOptions.MidlineLeft, true);
            price.rectTransform.Box(new Vector2(0.06f, 0.02f), new Vector2(0.6f, 0.14f), Vector2.zero, Vector2.zero);
            Mark(price, ShopCardPart.Part.Price);
            var left = Text(body, "CopiesLeft", "×3", 20, SitePalette.TextDim, TextAlignmentOptions.MidlineRight, true);
            left.rectTransform.Box(new Vector2(0.6f, 0.02f), new Vector2(0.94f, 0.14f), Vector2.zero, Vector2.zero);
            Mark(left, ShopCardPart.Part.CopiesLeft);

            // Over everything: the veil while you can't buy the card (off until then) and the sold-out cross.
            var veil = EditorUI.Image(body, "CantBuyVeil", new Color(1f, 1f, 1f, 0.62f), EditorUI.RoundedSmall);
            veil.rectTransform.Stretch();
            veil.enabled = false;
            Mark(veil, ShopCardPart.Part.CantBuyVeil);
            var soldOut = EditorUI.Image(body, "SoldOut", SitePalette.Danger.WithAlpha(0.85f), EditorUI.Cross);
            soldOut.rectTransform.Place(new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(80, 80));
            soldOut.preserveAspect = true;
            soldOut.gameObject.SetActive(false);
            Mark(soldOut, ShopCardPart.Part.SoldOut);

            string path = AssetDatabase.GenerateUniqueAssetPath(Folder + "/ShopCardTemplate.prefab");
            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, path);
            Object.DestroyImmediate(root.gameObject);
            return prefab;
        }

        /// <summary>
        /// Fills a Stats group with its two halves: "Clout" on the left and "Cash" on the right, each marked with its Shop
        /// Card Part and holding a placeholder number. Font and material, when given, are copied from an old Stats text.
        /// </summary>
        static void AddScoreTexts(RectTransform group, Color color, float size, bool bold, TMP_FontAsset font, Material material, bool undo)
        {
            var halves = new[] { ("Clout", "+2", ShopCardPart.Part.Clout, 0f), ("Cash", "+1", ShopCardPart.Part.Cash, 0.5f) };
            foreach (var (name, placeholder, part, from) in halves)
            {
                var label = Text(group, name, placeholder, size, color, TextAlignmentOptions.Center, bold);
                label.rectTransform.Box(new Vector2(from, 0f), new Vector2(from + 0.5f, 1f), Vector2.zero, Vector2.zero);
                // Group cards print their steps (Dancing Jen's Clout "1/4/9/16/25"): let the number shrink to fit.
                label.overflowMode = TextOverflowModes.Overflow;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.enableAutoSizing = true;
                label.fontSizeMax = size;
                label.fontSizeMin = Mathf.Min(9f, size);
                if (font != null) label.font = font;
                if (material != null) label.fontSharedMaterial = material;
                Mark(label, part);
                if (undo) Undo.RegisterCreatedObjectUndo(label.gameObject, "Split Stats into Clout and Cash");
            }
        }

        // ------------------------------------------------------------------ cards made from an older template

        const string SplitMenu = "Deck Party/Shop/Split Stats into Clout and Cash";

        /// <summary>The card's Stats text (icons for Clout, Cash and Risk on one line), if it has one.</summary>
        public static TMP_Text StatsText(GameObject card) => ShopItemView.FindPart<TMP_Text>(card, ShopCardPart.Part.Stats);

        [MenuItem(SplitMenu, priority = 41)]
        static void SplitSelected()
        {
            int done = 0;
            foreach (var go in Selection.gameObjects)
                if (SplitStats(go)) done++;
            if (done == 0)
                EditorUtility.DisplayDialog("Split Stats", "Select a shop card prefab whose Stats is one text (icons for Clout, Cash " +
                                                           "and Risk) in the Project window, then try again.", "OK");
        }

        [MenuItem(SplitMenu, true)]
        static bool CanSplitSelected()
        {
            foreach (var go in Selection.gameObjects)
                if (StatsText(go) != null) return true;
            return false;
        }

        /// <summary>
        /// Turns a card prefab's Stats text into the template's layout: Stats becomes a plain group (same place and size)
        /// holding a "Clout" and a "Cash" text, styled like the old one, with no icons and no Risk. If the prefab is open in
        /// Prefab Mode, the change is made there (undoable; save the prefab); otherwise the prefab asset is saved.
        /// </summary>
        public static bool SplitStats(GameObject card)
        {
            if (card == null || StatsText(card) == null) return false;
            var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && (stage.prefabContentsRoot == card || card.scene == stage.scene))
            {
                Split(stage.prefabContentsRoot, true);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(stage.scene);
                return true;
            }

            string path = AssetDatabase.GetAssetPath(card);
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab")) return false;
            if (stage != null && stage.assetPath == path)
            {
                Split(stage.prefabContentsRoot, true);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(stage.scene);
                return true;
            }
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (!Split(root, false)) return false;
                PrefabUtility.SaveAsPrefabAsset(root, path);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static bool Split(GameObject root, bool undo)
        {
            var old = StatsText(root);
            if (old == null) return false;
            var go = old.gameObject;
            var color = old.color;
            float size = old.enableAutoSizing ? old.fontSizeMax : old.fontSize;
            bool bold = (old.fontStyle & FontStyles.Bold) != 0;
            var font = old.font;
            var material = old.fontSharedMaterial;

            // Everything that belongs to the text goes; the object, its place and any layout components stay.
            foreach (var sub in go.GetComponentsInChildren<TMP_SubMeshUI>(true))
                if (sub != null && sub.transform.parent == go.transform) Remove(sub.gameObject, undo);
            foreach (var component in go.GetComponents<Component>())
            {
                bool statsMarker = component is ShopCardPart marker && marker.Kind == ShopCardPart.Part.Stats;
                if (statsMarker || component is SkinnedText || (component != old && NeedsGraphic(component))) Remove(component, undo);
            }
            Remove(old, undo);
            if (go.GetComponent<Graphic>() == null && go.TryGetComponent<CanvasRenderer>(out var canvasRenderer)) Remove(canvasRenderer, undo);
            if (go.name != "Stats")
            {
                if (undo) Undo.RecordObject(go, "Split Stats into Clout and Cash");
                go.name = "Stats";
            }

            AddScoreTexts((RectTransform)go.transform, color, size, bold, font, material, undo);
            return true;
        }

        /// <summary>A component Unity won't let stay without the text (a [RequireComponent] on a Graphic or text type).</summary>
        static bool NeedsGraphic(Component component)
        {
            if (component == null || component is Transform) return false;
            foreach (RequireComponent required in component.GetType().GetCustomAttributes(typeof(RequireComponent), true))
                foreach (var type in new[] { required.m_Type0, required.m_Type1, required.m_Type2 })
                    if (type != null && typeof(Graphic).IsAssignableFrom(type)) return true;
            return false;
        }

        static void Remove(Object target, bool undo)
        {
            if (target == null) return;
            if (undo) Undo.DestroyObjectImmediate(target);
            else Object.DestroyImmediate(target, true);
        }

        static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Color color, TextAlignmentOptions align, bool bold)
        {
            var label = EditorUI.Text(parent, name, text, size, color, align, bold, false, true);
            label.overflowMode = TextOverflowModes.Ellipsis;
            return label;
        }

        static void Mark(Component target, ShopCardPart.Part part, int order = 1)
        {
            var marker = target.gameObject.AddComponent<ShopCardPart>();
            marker.part = part;
            marker.order = order;
        }

        static void EnsureFolder()
        {
            if (AssetDatabase.IsValidFolder(Folder)) return;
            AssetDatabase.CreateFolder(DeckPartyPaths.Root + "/Prefabs", "Custom");
        }
    }
}
