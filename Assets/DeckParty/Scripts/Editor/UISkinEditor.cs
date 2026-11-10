using System.Collections.Generic;
using System.Text;
using DeckParty.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.EditorTools
{
    /// <summary>
    /// The UI Skin inspector (Deck Party > Select UI Skin): every entry under a readable name, grouped by the part of the game
    /// it belongs to, with where it shows, what art it takes, its auto-assign file name and a Find button that highlights the
    /// objects using it. Names and descriptions come from <see cref="SkinSlotGuide"/>.
    /// </summary>
    [CustomEditor(typeof(UISkin))]
    internal sealed partial class UISkinEditor : Editor
    {
        const string Intro =
            "Each entry is one piece of UI art, named after where it shows in the game. Drop a sprite on an entry to replace the " +
            "built-in placeholder, or name the file as shown under the entry and run Deck Party > Art > Auto-Assign Sprites by Name. " +
            "Most changes show straight away in the open scene; the small icons inside text update the next time you press Play.";

        const string ColourHelp =
            "Colours: your own icon art shows in its own colours (the game only tints the white placeholders). Panels and buttons are " +
            "tinted with the colour of the spot they're in. Tick Override Colour to use your colour instead (white shows the art exactly " +
            "as drawn).";

        const string NotHereHelp =
            "The white website panels of the stream and shop (menu bar, Stream Chat window, Community Beats, shop, pop-ups) take " +
            "their colours from Site Colours below, and their icons from the entries further down. Text Colours gives any single " +
            "text its own colour. The green Add Viewer and red End the Stream buttons always keep their colours (their bases " +
            "under them are entries in the Stream section).";

        /// <summary>The named panels in the Site Colours section: what each is called and where it shows.</summary>
        static readonly (SiteArea area, string name, string where)[] s_Areas =
        {
            (SiteArea.MenuBar, "Menu bar", "The bar along the top of the stream and the shop."),
            (SiteArea.StreamChat, "Stream Chat window", "The chat window with the viewers' avatars, its title band and its footer."),
            (SiteArea.WaitingViewer, "Waiting-viewer panel", "The panel that opens over the bottom of the chat, with Approve and Deny."),
            (SiteArea.CommunityBeats, "Community Beats", "The Community Beats panel beside the chat."),
            (SiteArea.Shop, "Shop", "The shop panel: its title, its toolbar and the space around the tiles."),
            (SiteArea.ShopTiles, "Shop tiles", "The viewer tiles in the shop and the card piles behind them."),
            (SiteArea.SidePanel, "Side panel", "The cards on the left of the stream: Players, emotes and the shop recap (multiplayer)."),
            (SiteArea.PopUps, "Pop-ups and banners",
                "The Audience overview, viewer pickers, stream and shop summaries, the shop gate, dialogs, the targeting bar, banners and toasts."),
            (SiteArea.Tooltips, "Tooltips", "The boxes that appear when you hover something."),
        };

        bool m_ShowSiteColours = true;
        readonly Dictionary<SiteArea, string> m_AreaFound = new Dictionary<SiteArea, string>();

        /// <summary>Set by the Skinned Image inspector's Edit in UI Skin button: the skin opens showing just that entry.</summary>
        static SkinSlot? s_Focus;

        static GUIStyle s_Name, s_Wrap, s_Faint;

        readonly Dictionary<SkinSlotGuide.Area, bool> m_Open = new Dictionary<SkinSlotGuide.Area, bool>();
        readonly Dictionary<SkinSlot, (string message, Object[] objects)> m_Found = new Dictionary<SkinSlot, (string, Object[])>();
        string m_Search = string.Empty;
        SkinSlot? m_Only;
        bool m_ShowHelp = true;

        /// <summary>Selects the UI Skin and shows only this entry (the Skinned Image inspector calls this).</summary>
        internal static void Focus(SkinSlot slot)
        {
            var skin = GameDatabase.Instance.skin;
            if (skin == null) skin = AssetDatabase.LoadAssetAtPath<UISkin>(DeckPartyPaths.Skin);
            if (skin == null)
            {
                EditorUtility.DisplayDialog("Deck Party", "No UI Skin yet. Run Deck Party > Setup > Build Everything first.", "OK");
                return;
            }
            s_Focus = slot;
            Selection.activeObject = skin;
            EditorGUIUtility.PingObject(skin);
        }

        void OnEnable()
        {
            var skin = (UISkin)target;
            int before = skin.entries.Count;
            int areasBefore = skin.siteColours != null && skin.siteColours.areas != null ? skin.siteColours.areas.Count : -1;
            skin.EnsureAllSlots();
            if (skin.entries.Count != before || skin.siteColours.areas.Count != areasBefore) EditorUtility.SetDirty(skin);
            foreach (SkinSlotGuide.Area area in System.Enum.GetValues(typeof(SkinSlotGuide.Area)))
                m_Open[area] = area != SkinSlotGuide.Area.NotShown;
            m_Only = s_Focus;
            s_Focus = null;
            OnEnableTextColours();
        }

        public override void OnInspectorGUI()
        {
            EnsureStyles();
            serializedObject.Update();
            RefreshSceneState();

            m_ShowHelp = EditorGUILayout.Foldout(m_ShowHelp, "How the UI Skin works", true, EditorStyles.foldoutHeader);
            if (m_ShowHelp)
            {
                EditorGUILayout.HelpBox(Intro, MessageType.None);
                EditorGUILayout.HelpBox(ColourHelp, MessageType.None);
                EditorGUILayout.HelpBox(NotHereHelp, MessageType.None);
            }

            if (m_Only.HasValue)
            {
                EditorGUILayout.Space(4);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Showing the entry used by the image you came from.", EditorStyles.boldLabel);
                    if (GUILayout.Button("Show all entries", GUILayout.Width(130))) m_Only = null;
                }
                if (m_Only.HasValue) DrawEntry(SkinSlotGuide.Get(m_Only.Value));
                serializedObject.ApplyModifiedProperties();
                return;
            }

            EditorGUILayout.Space(4);
            EditorGUI.BeginChangeCheck();
            DrawSiteColours();
            bool coloursChanged = EditorGUI.EndChangeCheck();
            EditorGUILayout.Space(6);
            DrawFonts();
            EditorGUILayout.Space(6);
            DrawTextColours();
            EditorGUILayout.Space(6);
            if (coloursChanged)
            {
                // Show the new colours in the Scene and Game views straight away.
                serializedObject.ApplyModifiedProperties();
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                m_Search = GUILayout.TextField(m_Search, EditorStyles.toolbarSearchField);
                if (GUILayout.Button("Open all", EditorStyles.miniButtonLeft, GUILayout.Width(64))) SetAllOpen(true);
                if (GUILayout.Button("Close all", EditorStyles.miniButtonRight, GUILayout.Width(64))) SetAllOpen(false);
            }
            bool searching = !string.IsNullOrWhiteSpace(m_Search);
            if (searching) EditorGUILayout.LabelField("Matching names, descriptions, ids and file names.", s_Faint);

            foreach (SkinSlotGuide.Area area in System.Enum.GetValues(typeof(SkinSlotGuide.Area)))
            {
                var infos = new List<SkinSlotGuide.Info>();
                foreach (var info in SkinSlotGuide.All)
                    if (info.Area == area && (!searching || Matches(info, m_Search))) infos.Add(info);
                if (infos.Count == 0) continue;

                EditorGUILayout.Space(4);
                bool open = EditorGUILayout.Foldout(m_Open[area] || searching, $"{SkinSlotGuide.Title(area)}  ({infos.Count})", true, EditorStyles.foldoutHeader);
                if (!searching) m_Open[area] = open;
                if (!open) continue;
                EditorGUILayout.LabelField(SkinSlotGuide.Summary(area), s_Faint);
                foreach (var info in infos) DrawEntry(info);
            }

            serializedObject.ApplyModifiedProperties();
        }

        // ------------------------------------------------------------------ Site Colours

        void DrawSiteColours()
        {
            m_ShowSiteColours = EditorGUILayout.Foldout(m_ShowSiteColours, "Site Colours (stream and shop panels)", true, EditorStyles.foldoutHeader);
            if (!m_ShowSiteColours) return;
            var set = serializedObject.FindProperty("siteColours");
            if (set == null) return;

            EditorGUILayout.LabelField(
                "The colours of the website look: the page, the panels and their borders, the thin lines and the text. Changes show " +
                "straight away in the open Game scene and while playing, and they're kept when you rebuild. Buttons keep their own " +
                "colours.", s_Wrap);
            // Both decided in Layout events only (RefreshSceneState): what the inspector draws must never change between a
            // Layout pass and the event that follows it.
            if (!m_SceneOpen)
                EditorGUILayout.HelpBox("Open the Game scene (Deck Party > Open Game Scene) to see your changes as you make them.", MessageType.Info);
            else if (!m_SceneTagged)
                EditorGUILayout.HelpBox("The open Game scene was built before Site Colours. Run Deck Party > Setup > Rebuild Scenes and " +
                                        "UI Prefabs once so its panels follow these colours.", MessageType.Warning);

            // Every readability warning is worked out here, before any colour box is drawn, from the colours as they were at the
            // start of this pass. The colour picker's changes arrive in an event Unity doesn't mark as used, so a warning that
            // appeared in that same pass (because the new colour is hard to read) would break the layout and lose the change.
            // A new colour's warning shows from the next pass instead.
            var areas = set.FindPropertyRelative("areas");
            float sharedContrast = Contrast(set.FindPropertyRelative("panelFill").colorValue, set.FindPropertyRelative("text").colorValue);
            var areaContrast = new Dictionary<SiteArea, float>();
            foreach (var (area, _, _) in s_Areas)
            {
                var element = FindAreaElement(areas, area);
                if (element == null) continue;
                bool ownFill = element.FindPropertyRelative("overrideFill").boolValue;
                bool ownText = element.FindPropertyRelative("overrideText").boolValue;
                if (!ownFill && !ownText) continue;
                var fill = ownFill ? element.FindPropertyRelative("fill").colorValue : set.FindPropertyRelative("panelFill").colorValue;
                var ink = ownText ? element.FindPropertyRelative("text").colorValue : set.FindPropertyRelative("text").colorValue;
                areaContrast[area] = Contrast(fill, ink);
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Shared colours (every panel)", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Click a colour box for the picker, or type a hex value (like #1E9E4F) in the box beside it " +
                                           "and press Enter.", s_Faint);
                ColourRow(set, "page", "Page", "The page behind every panel.");
                ColourRow(set, "panelFill", "Panel inside", "The inside of every panel and tile.");
                ColourRow(set, "panelBorder", "Panel border", "The border around every panel, and the normal ring around chat avatars.");
                ColourRow(set, "divider", "Dividers", "The thin lines between sections.");
                ColourRow(set, "softFill", "Soft fill", "Empty chat seats, the circle behind portraits and scrollbar tracks.");
                ColourRow(set, "text", "Text", "Main text, and the placeholder icons on panels (the titles and menu bar icons).");
                ColourRow(set, "secondaryText", "Secondary text", "Hints, counts and subtitles.");
                ColourRow(set, "faintText", "Faint text", "Empty notices and Community Beats that haven't scored yet.");
                if (sharedContrast < MinContrast) ContrastWarning(sharedContrast);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Panels", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Pick or type a colour to give one panel its own (its box ticks itself); untick to go back to the " +
                                       "shared colour. A panel with its own inside or text colour also gets its dividers, soft fills " +
                                       "and secondary text worked out from those two.", s_Faint);
            foreach (var (area, name, where) in s_Areas)
                DrawArea(set, FindAreaElement(areas, area), area, name, where, areaContrast.TryGetValue(area, out var c) ? c : (float?)null);

            EditorGUILayout.Space(2);
            if (GUILayout.Button("Reset Site Colours to the built-in look")) ResetSiteColours();
        }

        /// <summary>Text needs at least this contrast with the panel behind it to read well.</summary>
        const float MinContrast = 4.5f;

        static SerializedProperty FindAreaElement(SerializedProperty areas, SiteArea area)
        {
            for (int i = 0; i < areas.arraySize; i++)
            {
                var candidate = areas.GetArrayElementAtIndex(i);
                if (candidate.FindPropertyRelative("area").intValue == (int)area) return candidate;
            }
            return null;
        }

        /// <summary>A shared colour: its colour box, a hex box, and what it colours. Every change is saved straight away.</summary>
        static void ColourRow(SerializedProperty set, string field, string label, string description)
        {
            var colour = set.FindPropertyRelative(field);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(colour, new GUIContent(label, description));
                if (EditorGUI.EndChangeCheck()) colour.serializedObject.ApplyModifiedProperties();
                var value = colour.colorValue;
                if (HexField(ref value))
                {
                    colour.colorValue = value;
                    colour.serializedObject.ApplyModifiedProperties();
                }
            }
            using (new EditorGUI.IndentLevelScope()) EditorGUILayout.LabelField(description, s_Faint);
        }

        /// <param name="contrast">The text contrast to warn about (worked out before anything was drawn), or null for none.</param>
        void DrawArea(SerializedProperty set, SerializedProperty element, SiteArea area, string name, string where, float? contrast)
        {
            if (element == null) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(name, s_Name);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(new GUIContent("Find", "Highlight this panel in the open scene."), EditorStyles.miniButton, GUILayout.Width(48)))
                        FindArea(area, name);
                }
                GUILayout.Label(where, s_Wrap);
                OverrideRow(element, "overrideFill", "fill", "Own inside", set.FindPropertyRelative("panelFill"));
                OverrideRow(element, "overrideBorder", "border", "Own border", set.FindPropertyRelative("panelBorder"));
                OverrideRow(element, "overrideText", "text", "Own text", set.FindPropertyRelative("text"));
                if (contrast.HasValue && contrast.Value < MinContrast) ContrastWarning(contrast.Value);
                // Find results only appear after a button click, which Unity marks as used, so they can't break the layout.
                if (m_AreaFound.TryGetValue(area, out var found)) EditorGUILayout.LabelField(found, s_Wrap);
            }
        }

        /// <summary>
        /// A tick, a colour box and a hex box. The colour shown is the one in effect and can always be edited: picking or typing a
        /// colour gives the panel its own (ticks the box); unticking goes back to the shared colour. Saved straight away.
        /// </summary>
        static void OverrideRow(SerializedProperty element, string toggleField, string colourField, string label, SerializedProperty shared)
        {
            var on = element.FindPropertyRelative(toggleField);
            var colour = element.FindPropertyRelative(colourField);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                bool value = EditorGUILayout.ToggleLeft(new GUIContent(label,
                    "Ticked: this panel uses its own colour. Unticked: it uses the shared colour."), on.boolValue, GUILayout.Width(110));
                if (EditorGUI.EndChangeCheck())
                {
                    // Start from the colour on screen.
                    if (value && !on.boolValue) colour.colorValue = shared.colorValue;
                    on.boolValue = value;
                    on.serializedObject.ApplyModifiedProperties();
                }

                var shown = on.boolValue ? colour.colorValue : shared.colorValue;
                EditorGUI.BeginChangeCheck();
                var picked = EditorGUILayout.ColorField(new GUIContent(string.Empty, on.boolValue
                    ? "This panel's own colour."
                    : "The shared colour. Pick a colour to give this panel its own."), shown, true, true, false);
                if (EditorGUI.EndChangeCheck()) Own(on, colour, picked);
                var typed = shown;
                if (HexField(ref typed)) Own(on, colour, typed);
                // Always drawn (only the words change) so the row's layout never changes mid-click.
                GUILayout.Label(on.boolValue ? "own" : "shared", s_Faint, GUILayout.Width(44));
            }
        }

        /// <summary>Gives the panel its own colour and saves it.</summary>
        static void Own(SerializedProperty on, SerializedProperty colour, Color value)
        {
            colour.colorValue = value;
            on.boolValue = true;
            colour.serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// A small box with the colour as hex (#RRGGBB, or #RRGGBBAA when see-through). Type a value (with or without #) and press
        /// Enter or click away. Returns true, with the new colour, when a valid colour was typed.
        /// </summary>
        static bool HexField(ref Color colour)
        {
            string shown = "#" + (colour.a < 0.999f ? ColorUtility.ToHtmlStringRGBA(colour) : ColorUtility.ToHtmlStringRGB(colour));
            EditorGUI.BeginChangeCheck();
            string typed = EditorGUILayout.DelayedTextField(shown, GUILayout.Width(86));
            if (!EditorGUI.EndChangeCheck() || string.IsNullOrWhiteSpace(typed)) return false;
            typed = typed.Trim();
            if (typed[0] != '#' && IsHex(typed)) typed = "#" + typed;
            if (!ColorUtility.TryParseHtmlString(typed, out var parsed)) return false;
            // Only RGB typed (#RGB or #RRGGBB): keep the alpha the colour had.
            if (typed[0] == '#' && (typed.Length == 4 || typed.Length == 7)) parsed.a = colour.a;
            colour = parsed;
            return true;
        }

        static bool IsHex(string text)
        {
            foreach (char c in text)
                if (!System.Uri.IsHexDigit(c)) return false;
            return text.Length > 0;
        }

        static void ContrastWarning(float ratio) =>
            EditorGUILayout.HelpBox($"Text may be hard to read on this inside colour (contrast {ratio:0.0} to 1; aim for {MinContrast:0.0} or more).",
                MessageType.Warning);

        /// <summary>The contrast ratio between two colours (1 = none, 21 = black on white), as used for readable text.</summary>
        static float Contrast(Color a, Color b)
        {
            float la = RelativeLuminance(a), lb = RelativeLuminance(b);
            return (Mathf.Max(la, lb) + 0.05f) / (Mathf.Min(la, lb) + 0.05f);
        }

        static float RelativeLuminance(Color c) => 0.2126f * Linear(c.r) + 0.7152f * Linear(c.g) + 0.0722f * Linear(c.b);

        static float Linear(float v) => v <= 0.03928f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);

        void ResetSiteColours()
        {
            if (!EditorUtility.DisplayDialog("Reset Site Colours", "Put every site colour back to the built-in look and clear the panels' own colours?", "Reset", "Cancel"))
                return;
            var skin = (UISkin)target;
            serializedObject.ApplyModifiedProperties();
            Undo.RecordObject(skin, "Reset Site Colours");
            skin.siteColours.ResetToDefaults();
            EditorUtility.SetDirty(skin);
            serializedObject.Update();
            skin.NotifyChanged();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            GUIUtility.ExitGUI();
        }

        /// <summary>Pings the panel in the open scene (or the prefab it's built into) and says where it is.</summary>
        void FindArea(SiteArea area, string name)
        {
            var hits = new List<Object>();
            foreach (var tag in Object.FindObjectsByType<SiteAreaTag>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (tag != null && tag.Area == area) hits.Add(tag.gameObject);
            if (hits.Count > 0)
            {
                EditorGUIUtility.PingObject(hits[0]);
                m_AreaFound[area] = $"In the open scene: {Names(hits)}.";
                return;
            }
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { DeckPartyPaths.Prefabs }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab == null || !prefab.TryGetComponent<SiteAreaTag>(out var tag) || tag.Area != area) continue;
                EditorGUIUtility.PingObject(prefab);
                m_AreaFound[area] = $"Built into the {prefab.name} prefab (highlighted in the Project window); the game places copies of it.";
                return;
            }
            m_AreaFound[area] = GameSceneOpen()
                ? $"Not in the open scene yet. Run Deck Party > Setup > Rebuild Scenes and UI Prefabs so the {name} is tagged."
                : "Open the Game scene (Deck Party > Open Game Scene) to find it.";
        }

        static bool GameSceneOpen()
        {
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).path == DeckPartyPaths.GameScene) return true;
            return false;
        }

        double m_SceneCheckedAt = -10;
        bool m_SceneOpen = true;
        /// <summary>False when the Game scene is open but was built before Site Colours (nothing in it is tagged).</summary>
        bool m_SceneTagged = true;

        /// <summary>
        /// Checks whether the Game scene is open and built with Site Colours. Only in Layout events, at most every 2 s: what the
        /// inspector draws must never change between a Layout pass and the event that follows it.
        /// </summary>
        void RefreshSceneState()
        {
            if (Event.current == null || Event.current.type != EventType.Layout) return;
            if (EditorApplication.timeSinceStartup - m_SceneCheckedAt < 2.0) return;
            m_SceneCheckedAt = EditorApplication.timeSinceStartup;
            m_SceneOpen = GameSceneOpen();
            m_SceneTagged = !m_SceneOpen ||
                            Object.FindObjectsByType<SiteAreaTag>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0;
        }

        // ------------------------------------------------------------------ fonts and art

        void DrawFonts()
        {
            EditorGUILayout.LabelField("Fonts", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("bodyFont"), new GUIContent("Body Font",
                    "Every normal text in the game."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("headerFont"), new GUIContent("Header Font",
                    "Titles and big numbers. Empty uses the Body Font."));
                EditorGUILayout.LabelField(
                    "Body Font is every normal text; Header Font is titles and big numbers (empty uses the Body Font). The built-in fonts " +
                    "have fallbacks for Chinese and Russian. A new font needs them too (Deck Party > Localization > Rebuild Fonts), or " +
                    "those languages show squares.", s_Wrap);
            }
        }

        void DrawEntry(SkinSlotGuide.Info info)
        {
            var element = FindEntry(info.Slot);
            if (element == null) return;
            var spriteProperty = element.FindPropertyRelative("sprite");
            var sprite = spriteProperty.objectReferenceValue as Sprite;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    var picked = EditorGUILayout.ObjectField(sprite, typeof(Sprite), false, GUILayout.Width(64), GUILayout.Height(64));
                    if (EditorGUI.EndChangeCheck()) spriteProperty.objectReferenceValue = picked;

                    using (new EditorGUILayout.VerticalScope())
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Label(info.Name, s_Name);
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button(new GUIContent("Find", "Highlight the objects that use this entry in the open scene " +
                                    "(or the UI prefabs that use it)."), EditorStyles.miniButton, GUILayout.Width(48)))
                                Find(info);
                        }
                        GUILayout.Label(info.Where, s_Wrap);
                        GUILayout.Label($"{Status(sprite)}     File name: {SkinSlotGuide.FileName(info.Slot)}     Id: {info.Slot}", s_Faint);
                    }
                }

                if (info.GameColoured)
                {
                    EditorGUILayout.LabelField("Colour: set by the game while playing (Override Colour has no lasting effect here).", s_Faint);
                }
                else
                {
                    var overrideColour = element.FindPropertyRelative("overrideColor");
                    var colour = element.FindPropertyRelative("color");
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(overrideColour, new GUIContent("Override Colour",
                            "Ticked: this colour tints the art instead of the game's colour for this spot (white = exactly as drawn)."));
                        // Always editable: picking a colour ticks Override Colour (a greyed-out box would open the picker
                        // and then throw the colour away).
                        EditorGUI.BeginChangeCheck();
                        var picked = EditorGUILayout.ColorField(new GUIContent(string.Empty, overrideColour.boolValue
                            ? "The colour this entry uses."
                            : "Pick a colour to override the game's colour for this entry."), colour.colorValue, true, true, false);
                        if (EditorGUI.EndChangeCheck()) Own(overrideColour, colour, picked);
                        var typed = colour.colorValue;
                        if (HexField(ref typed)) Own(overrideColour, colour, typed);
                    }
                }

                var imageType = element.FindPropertyRelative("imageType");
                if (info.Kind == SkinSlotGuide.Kind.Frame)
                {
                    EditorGUILayout.PropertyField(imageType, new GUIContent("Image Type",
                        "Sliced stretches the middle and keeps the corners (9-slice). Simple stretches the whole picture."));
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("pixelsPerUnitMultiplier"), new GUIContent("Border Scale",
                        "Makes the 9-slice corners thinner (higher) or thicker (lower)."));
                }
                else if (imageType.intValue != (int)Image.Type.Simple)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("This entry is set to a stretching Image Type, but it's drawn as a single picture.", s_Wrap);
                        if (GUILayout.Button("Use Simple", GUILayout.Width(90))) imageType.intValue = (int)Image.Type.Simple;
                    }
                }

                EditorGUILayout.LabelField(info.Slot == SkinSlot.Logo
                    ? "Art: your logo as a PNG with a transparent background (it keeps its proportions)."
                    : SkinSlotGuide.ArtAdvice(info.Kind), s_Faint);

                if (m_Found.TryGetValue(info.Slot, out var found))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(found.message, s_Wrap);
                        if (found.objects != null && found.objects.Length > 0 && GUILayout.Button("Select", GUILayout.Width(60)))
                            Selection.objects = found.objects;
                    }
                }
            }
        }

        SerializedProperty FindEntry(SkinSlot slot)
        {
            var entries = serializedObject.FindProperty("entries");
            for (int i = 0; i < entries.arraySize; i++)
            {
                var element = entries.GetArrayElementAtIndex(i);
                if (element.FindPropertyRelative("slot").intValue == (int)slot) return element;
            }
            return null;
        }

        static string Status(Sprite sprite)
        {
            if (sprite == null) return "Empty (keeps the built-in look)";
            string path = AssetDatabase.GetAssetPath(sprite);
            return path.Contains("/Placeholders/") ? "Built-in placeholder" : "Your art: " + sprite.name;
        }

        static bool Matches(SkinSlotGuide.Info info, string search)
        {
            string s = search.Trim();
            return Contains(info.Name, s) || Contains(info.Where, s) || Contains(info.Slot.ToString(), s) ||
                   Contains(SkinSlotGuide.FileName(info.Slot), s) || Contains(SkinSlotGuide.Title(info.Area), s);
        }

        static bool Contains(string text, string part) => text != null && text.IndexOf(part, System.StringComparison.OrdinalIgnoreCase) >= 0;

        void SetAllOpen(bool open)
        {
            foreach (SkinSlotGuide.Area area in System.Enum.GetValues(typeof(SkinSlotGuide.Area))) m_Open[area] = open;
        }

        /// <summary>
        /// Highlights (pings) the first object in the open scene, or the open prefab, that uses this entry and lists the rest. When
        /// none is open it looks in the UI prefabs instead. The Select button under the entry selects them all.
        /// </summary>
        void Find(SkinSlotGuide.Info info)
        {
            var hits = new List<Object>();
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            var images = stage != null
                ? stage.prefabContentsRoot.GetComponentsInChildren<SkinnedImage>(true)
                : Object.FindObjectsByType<SkinnedImage>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var image in images)
                if (image != null && image.Slot == info.Slot) hits.Add(image.gameObject);

            if (hits.Count > 0)
            {
                EditorGUIUtility.PingObject(hits[0]);
                string where = stage != null ? "the open prefab" : "the open scene";
                m_Found[info.Slot] = ($"In {where}: {Names(hits)}.", hits.ToArray());
                return;
            }

            var prefabs = new List<Object>();
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { DeckPartyPaths.Prefabs }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                foreach (var image in prefab.GetComponentsInChildren<SkinnedImage>(true))
                {
                    if (image.Slot != info.Slot) continue;
                    prefabs.Add(prefab);
                    break;
                }
            }
            if (prefabs.Count > 0)
            {
                EditorGUIUtility.PingObject(prefabs[0]);
                m_Found[info.Slot] = ($"Not in the open scene. Used by the prefab{(prefabs.Count == 1 ? "" : "s")} {Names(prefabs)} " +
                                      "(highlighted in the Project window; double-click one to open it).", prefabs.ToArray());
                return;
            }

            m_Found[info.Slot] = (info.Area == SkinSlotGuide.Area.NotShown
                ? "Nothing in the open scene or the UI prefabs uses it."
                : "Not in the open scene or the UI prefabs. It may be in the other scene (Deck Party > Open Game Scene for the stream " +
                  "and shop, the MainMenu scene for menus), or the game reads it while playing.", null);
        }

        static string Names(List<Object> objects)
        {
            var sb = new StringBuilder();
            int shown = Mathf.Min(objects.Count, 5);
            for (int i = 0; i < shown; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(objects[i] is GameObject go ? Path(go) : objects[i].name);
            }
            if (objects.Count > shown) sb.Append($" and {objects.Count - shown} more");
            return sb.ToString();
        }

        /// <summary>The object's place in the hierarchy, shortened to its last three levels ("Party > ChatButtons > Door").</summary>
        static string Path(GameObject go)
        {
            var parts = new List<string>();
            for (var t = go.transform; t != null && parts.Count < 3; t = t.parent) parts.Insert(0, t.name);
            return string.Join(" > ", parts);
        }

        static void EnsureStyles()
        {
            if (s_Name != null) return;
            s_Name = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
            s_Wrap = new GUIStyle(EditorStyles.label) { wordWrap = true };
            s_Faint = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            s_Faint.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.62f, 0.62f, 0.66f) : new Color(0.36f, 0.36f, 0.4f);
        }
    }
}
