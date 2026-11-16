using System.Collections.Generic;
using System.Text;
using DeckParty.UI;
using UnityEditor;
using UnityEngine;

namespace DeckParty.EditorTools
{
    /// <summary>
    /// UI Skin > Shop: the toolbar buttons (icon, text, shape), the prices above them and the viewer cards (your own card
    /// prefab, sizes, piles and colours). Applied live in the open Game scene; the cards change the next time the shop draws.
    /// </summary>
    internal sealed partial class UISkinEditor
    {
        const string ShopHelp =
            "The shop's toolbar buttons and viewer cards. Anything left empty, at 0 (or -1) or unticked keeps the built-in look. " +
            "Buttons change straight away in the open Game scene; the cards change the next time the shop draws (while playing, " +
            "straight away).";

        static readonly (ShopButton button, string name, string where, SkinSlot icon)[] s_ShopButtons =
        {
            (ShopButton.Power, "Streamer active", "At the left end of the toolbar: the streamer's portrait and the active's icon (hover it for the streamer and the power). Built-in text: none.", SkinSlot.IconAction),
            (ShopButton.RaiseSubCap, "Raise Sub Cap", "Built-in text: +1 (the Sub Cap when it's maxed).", SkinSlot.IconDoor),
            (ShopButton.RemoveViewer, "Remove a Viewer", "Built-in text: -1.", SkinSlot.IconRemoveViewer),
            (ShopButton.GoLive, "Go Live", "At the right end of the toolbar. Built-in text: none (just the icon).", SkinSlot.IconGoLive),
        };

        bool m_ShowShop;
        bool m_ShowShopButtons = true;
        bool m_ShowShopCards = true;
        readonly Dictionary<ShopButton, bool> m_ShopOpen = new Dictionary<ShopButton, bool>();
        readonly Dictionary<ShopButton, string> m_ShopFound = new Dictionary<ShopButton, string>();

        void DrawShop()
        {
            var shop = serializedObject.FindProperty("shop");
            if (shop == null) return;
            m_ShowShop = EditorGUILayout.Foldout(m_ShowShop, "Shop (buttons, prices, viewer cards)", true, EditorStyles.foldoutHeader);
            if (!m_ShowShop) return;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField(ShopHelp, s_Wrap);

            EditorGUILayout.Space(2);
            m_ShowShopButtons = EditorGUILayout.Foldout(m_ShowShopButtons, "Toolbar buttons", true);
            if (m_ShowShopButtons)
            {
                var buttons = shop.FindPropertyRelative("buttons");
                foreach (var (button, name, where, icon) in s_ShopButtons)
                    DrawShopButton(FindShopButton(buttons, button), button, name, where, icon);
            }

            EditorGUILayout.Space(2);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Prices above the buttons", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("The Influence icon and amount above the streamer active, Raise Sub Cap and Remove a Viewer.", s_Faint);
                EditorGUILayout.PropertyField(shop.FindPropertyRelative("costSize"), new GUIContent("Font size", "0: the built-in size (26)."));
                EditorGUILayout.PropertyField(shop.FindPropertyRelative("costGap"), new GUIContent("Gap above the button",
                    "Pixels between the price and the top of its button. -1: the built-in gap (6)."));
                EditorGUILayout.PropertyField(shop.FindPropertyRelative("costColour"), new GUIContent("Colour", "Unticked: the shop's text colour."));
                EditorGUILayout.PropertyField(shop.FindPropertyRelative("costTooExpensive"), new GUIContent("Colour when too expensive",
                    "When you don't have enough Influence. Unticked: red."));
            }

            EditorGUILayout.Space(2);
            m_ShowShopCards = EditorGUILayout.Foldout(m_ShowShopCards, "Viewer cards", true);
            if (m_ShowShopCards) DrawShopCards(shop);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                ((UISkin)target).NotifyChanged();
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
        }

        static SerializedProperty FindShopButton(SerializedProperty buttons, ShopButton button)
        {
            for (int i = 0; i < buttons.arraySize; i++)
            {
                var element = buttons.GetArrayElementAtIndex(i);
                if (element.FindPropertyRelative("button").intValue == (int)button) return element;
            }
            return null;
        }

        void DrawShopButton(SerializedProperty look, ShopButton button, string name, string where, SkinSlot iconSlot)
        {
            if (look == null) return;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool open = m_ShopOpen.TryGetValue(button, out var o) && o;
                    m_ShopOpen[button] = EditorGUILayout.Foldout(open, name, true);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(new GUIContent("Find", "Highlight this button in the open Game scene."), EditorStyles.miniButton, GUILayout.Width(48)))
                        FindShopButtonInScene(button);
                }
                if (m_ShopFound.TryGetValue(button, out var found)) EditorGUILayout.LabelField(found, s_Wrap);
                if (!m_ShopOpen[button]) return;
                EditorGUILayout.LabelField(where, s_Faint);

                EditorGUILayout.LabelField("Icon", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(look.FindPropertyRelative("icon"), new GUIContent("Art",
                    $"Empty: the \"{SkinSlotGuide.Get(iconSlot).Name}\" entry further down the UI Skin."));
                EditorGUILayout.PropertyField(look.FindPropertyRelative("iconColour"), new GUIContent("Colour",
                    "Unticked: the built-in colour (your own icon art shows in its own colours)."));
                EditorGUILayout.PropertyField(look.FindPropertyRelative("iconSize"), new GUIContent("Size", "Pixels. 0: the built-in size."));

                EditorGUILayout.LabelField("Text", EditorStyles.boldLabel);
                var text = look.FindPropertyRelative("text");
                EditorGUILayout.PropertyField(text, new GUIContent("Shows",
                    "Built In: the game's numbers. Words: a short word in the player's language. Custom: your own text (not translated). None: only the icon, centred."));
                if (text.intValue == (int)ShopButtonText.Custom)
                    EditorGUILayout.PropertyField(look.FindPropertyRelative("customText"), new GUIContent("Your text", "The same in every language."));
                if (text.intValue == (int)ShopButtonText.Words)
                    EditorGUILayout.LabelField($"The word is \"{ShopLook.WordsKey(button)}\" in Resources/Localization/AudienceCapture_Strings.csv (5 languages).", s_Faint);
                EditorGUILayout.PropertyField(look.FindPropertyRelative("textColour"), new GUIContent("Colour", "Unticked: the built-in colour."));
                EditorGUILayout.PropertyField(look.FindPropertyRelative("textSize"), new GUIContent("Font size", "0: the built-in size."));

                EditorGUILayout.LabelField("Shape", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(look.FindPropertyRelative("shape"), new GUIContent("Background art",
                    "A 9-slice sprite (set its borders in the Sprite Editor so the corners don't stretch). Empty: the built-in rounded rectangle."));
                if (look.FindPropertyRelative("shape").objectReferenceValue != null)
                    EditorGUILayout.PropertyField(look.FindPropertyRelative("shapeScale"), new GUIContent("Corner scale",
                        "Makes the 9-slice corners thinner (higher) or thicker (lower)."));
                EditorGUILayout.PropertyField(look.FindPropertyRelative("fill"), new GUIContent("Inside colour",
                    "Unticked: the built-in colour. White shows your background art in its own colours."));
                if (button != ShopButton.Power && button != ShopButton.GoLive)
                {
                    EditorGUILayout.PropertyField(look.FindPropertyRelative("border"), new GUIContent("Border colour", "Unticked: the built-in black."));
                    EditorGUILayout.PropertyField(look.FindPropertyRelative("borderWidth"), new GUIContent("Border width",
                        "Pixels. -1: the built-in border (2.5); 0: no border (use this with art that has its own outline)."));
                }
                EditorGUILayout.PropertyField(look.FindPropertyRelative("size"), new GUIContent("Size", "Width and height in pixels. 0: the built-in size."));
            }
        }

        void FindShopButtonInScene(ShopButton button)
        {
            foreach (var skin in Object.FindObjectsByType<ShopButtonSkin>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (skin == null || skin.Button != button) continue;
                EditorGUIUtility.PingObject(skin.gameObject);
                Selection.activeGameObject = skin.gameObject;
                m_ShopFound[button] = "Selected in the open scene (the shop is switched off in the scene: switch Planning on to see it, and off again before saving).";
                return;
            }
            m_ShopFound[button] = GameSceneOpen()
                ? "Not in the open scene. Run Deck Party > Setup > Rebuild Scenes and UI Prefabs so the shop buttons follow the UI Skin."
                : "Open the Game scene (Deck Party > Open Game Scene) to find it.";
        }

        void DrawShopCards(SerializedProperty shop)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Your own card", EditorStyles.boldLabel);
                var prefab = shop.FindPropertyRelative("cardPrefab");
                EditorGUILayout.PropertyField(prefab, new GUIContent("Card prefab",
                    "Your own shop card design. Empty: the built-in card. See ShopCardPrefab_Guidelines.md in the project folder."));
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(new GUIContent("Make a template prefab", "A starting card with every part the game fills in, " +
                            "in Assets/DeckParty/Prefabs/Custom.")))
                    {
                        var made = ShopCardTemplate.Create();
                        if (made != null)
                        {
                            EditorGUIUtility.PingObject(made);
                            m_ShopCardNote = $"Made {AssetDatabase.GetAssetPath(made)} (highlighted). Edit it, then drop it on Card prefab.";
                        }
                    }
                    if (GUILayout.Button("Open the guidelines", GUILayout.Width(150))) OpenGuidelines();
                }
                if (!string.IsNullOrEmpty(m_ShopCardNote)) EditorGUILayout.LabelField(m_ShopCardNote, s_Wrap);
                if (prefab.objectReferenceValue is GameObject card) DrawPrefabCheck(card);

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Sizes", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(shop.FindPropertyRelative("minCardWidth"), new GUIContent("Narrowest", "Pixels. 0: built-in (160)."));
                EditorGUILayout.PropertyField(shop.FindPropertyRelative("maxCardWidth"), new GUIContent("Widest", "Pixels. 0: built-in (196)."));
                EditorGUILayout.PropertyField(shop.FindPropertyRelative("cardAspect"), new GUIContent("Height / width",
                    "0: built-in (1.4), or your prefab's own proportions."));
                EditorGUILayout.PropertyField(shop.FindPropertyRelative("cardSpacing"), new GUIContent("Space between", "Across and down, in pixels. 0: built-in (38 x 36)."));
                EditorGUILayout.PropertyField(shop.FindPropertyRelative("rowGap"), new GUIContent("Extra space between rows",
                    "Pixels added between one row of cards and the next, on top of the space down. -1: built-in (32); 0: none."));
                EditorGUILayout.PropertyField(shop.FindPropertyRelative("firstRowSpace"), new GUIContent("Space above the first row",
                    "Pixels between the top of the card area and the first row (its card piles peek into it). " +
                    "-1: built-in (30 plus the extra space between rows); 0: none."));
                EditorGUILayout.PropertyField(shop.FindPropertyRelative("hoverScale"), new GUIContent("Hover growth",
                    "How much a card grows under the pointer (1 = not at all). 0: built-in (1.04)."));

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Built-in card", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Only for the built-in card (your own prefab has its own shape and pile). Its inside, " +
                                           "border and text colours are in Site Colours (Shop tiles) and Text Colours (Shop tiles).", s_Faint);
                EditorGUILayout.PropertyField(shop.FindPropertyRelative("cardShape"), new GUIContent("Shape",
                    "A 9-slice sprite for the card's outline and inside (rounded corners, for example). Empty: square corners."));
                EditorGUILayout.PropertyField(shop.FindPropertyRelative("cardBorderWidth"), new GUIContent("Border width", "Pixels. -1: built-in (3); 0: no border."));
                EditorGUILayout.PropertyField(shop.FindPropertyRelative("hidePiles"), new GUIContent("Hide the card piles"));
                EditorGUILayout.PropertyField(shop.FindPropertyRelative("pileStep"), new GUIContent("Pile step",
                    "How far each card back peeks out above the one in front, in pixels. 0: built-in (7)."));

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Colours (any card)", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(shop.FindPropertyRelative("trophyBorder"), new GUIContent("Trophy card border", "Unticked: gold."));
                EditorGUILayout.PropertyField(shop.FindPropertyRelative("price"), new GUIContent("Price", "Unticked: the tiles' text colour."));
                EditorGUILayout.PropertyField(shop.FindPropertyRelative("priceTooExpensive"), new GUIContent("Price, too expensive", "Unticked: red."));
                EditorGUILayout.PropertyField(shop.FindPropertyRelative("copiesLeft"), new GUIContent("Copies left", "Unticked: the tiles' secondary text colour."));
                EditorGUILayout.PropertyField(shop.FindPropertyRelative("lastCopy"), new GUIContent("Last copy", "Unticked: orange."));
                EditorGUILayout.PropertyField(shop.FindPropertyRelative("soldOut"), new GUIContent("Sold out", "Unticked: red."));
                EditorGUILayout.PropertyField(shop.FindPropertyRelative("cantBuyVeil"), new GUIContent("Can't-buy veil",
                    "Laid over a card you can't buy right now. Unticked: white at 62%."));
            }
        }

        string m_ShopCardNote;

        /// <summary>Says which parts the game found in your card prefab, and warns about the ones a card needs.</summary>
        static void DrawPrefabCheck(GameObject card)
        {
            var parts = ShopItemView.FindParts(card);
            var found = new StringBuilder();
            var missing = new StringBuilder();
            foreach (var pair in parts)
            {
                var target = pair.Value ? found : missing;
                if (target.Length > 0) target.Append(", ");
                target.Append(ObjectNames.NicifyVariableName(pair.Key.ToString()));
            }
            EditorGUILayout.LabelField("Found: " + (found.Length > 0 ? found.ToString() : "nothing"), s_Wrap);
            EditorGUILayout.LabelField("Not in this card (fine if you don't want them): " + (missing.Length > 0 ? missing.ToString() : "none"), s_Faint);

            if (!parts[ShopCardPart.Part.Cash] && !parts[ShopCardPart.Part.Influence] && ShopCardTemplate.StatsText(card) != null)
            {
                EditorGUILayout.HelpBox("This card's Stats is one text with icons for Cash, Influence and Risk. New templates split it into " +
                                        "a Cash number and an Influence number instead (no icons; the Risk Badge shows Risk).", MessageType.Info);
                if (GUILayout.Button("Split Stats into Cash and Influence"))
                    EditorApplication.delayCall += () => ShopCardTemplate.SplitStats(card);
            }
            if (ShopCardTemplate.HasActionOutline(card))
                EditorGUILayout.LabelField("Action outline: yes. Open the prefab and select its Inner Outline object to adjust it.", s_Wrap);
            else
            {
                EditorGUILayout.HelpBox("No action outline: an inner outline along the card's edge that lights up on viewers with an action.",
                    MessageType.Info);
                if (GUILayout.Button("Add the action outline"))
                    EditorApplication.delayCall += () => ShopCardTemplate.AddActionOutline(card);
            }
            if (!parts[ShopCardPart.Part.Background])
                EditorGUILayout.HelpBox("No Background (and no Image on the card's root): the card can't be clicked. Mark an Image as Background.", MessageType.Error);
            if (!parts[ShopCardPart.Part.Portrait])
                EditorGUILayout.HelpBox("No Portrait: the card won't show the viewer's picture.", MessageType.Warning);
            if (!parts[ShopCardPart.Part.Price])
                EditorGUILayout.HelpBox("No Price: players won't see what the viewer costs.", MessageType.Warning);
            if (card.GetComponentInChildren<Canvas>(true) != null)
                EditorGUILayout.HelpBox("The card has a Canvas in it. Remove it: the card goes inside the shop's canvas.", MessageType.Warning);

            var size = ShopCardFit.DesignSize(card);
            var rect = card.transform as RectTransform;
            if (rect == null || rect.anchorMin != rect.anchorMax || rect.sizeDelta.x <= 1f || rect.sizeDelta.y <= 1f)
                EditorGUILayout.HelpBox($"The card's root has no fixed Width and Height (its anchors stretch), so it's treated as {size.x:0} x {size.y:0}. " +
                                        "Set the root's anchors to the middle and give it a Width and Height.", MessageType.Info);
            else
                EditorGUILayout.LabelField($"Designed at {size.x:0} x {size.y:0}: the shop scales it to fit (height / width {size.y / size.x:0.00}).", s_Faint);
        }

        static void OpenGuidelines()
        {
            string path = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "ShopCardPrefab_Guidelines.md"));
            if (System.IO.File.Exists(path)) EditorUtility.OpenWithDefaultApp(path);
            else EditorUtility.DisplayDialog("Shop card guidelines", "ShopCardPrefab_Guidelines.md isn't in the project folder.", "OK");
        }
    }
}
