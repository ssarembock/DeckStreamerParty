using System.Collections.Generic;
using DeckParty.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeckParty.EditorTools
{
    /// <summary>
    /// UI Skin > Text Colours: every text of the stream and shop (listed by the scene builders, see
    /// <see cref="TextColourRegistry"/>), grouped by the part of the game it's in, with its own colour box.
    /// </summary>
    internal sealed partial class UISkinEditor
    {
        const string TextColoursHelp =
            "Every text of the stream and the shop, grouped by where it is. Tick a text (or pick or type a colour) to give it its " +
            "own colour; untick it to give it back the game's colour. Your colour also replaces the colours the game sets while " +
            "playing (like a red price you can't afford); see-through fades still work. Texts in the UI prefabs (chat avatars, " +
            "shop tiles, viewer cards and rows, player rows) change every copy, in the menus too. Select a text in the scene to " +
            "set its colour from its own inspector.";

        /// <summary>Most rows drawn at once (a long list makes the inspector slow); search to narrow it.</summary>
        const int MaxTextRows = 150;

        /// <summary>Set by the Skinned Text inspector's Show in UI Skin button: the skin opens with that text found.</summary>
        static string s_FocusText;

        bool m_ShowTextColours;
        string m_TextSearch = string.Empty;
        bool m_OnlyOwn;
        readonly Dictionary<string, bool> m_TextGroupOpen = new Dictionary<string, bool>();
        readonly Dictionary<string, string> m_TextFound = new Dictionary<string, string>();

        /// <summary>Selects the UI Skin with the Text Colours section open on this text.</summary>
        internal static void FocusText(string id)
        {
            var skin = GameDatabase.Instance.skin;
            if (skin == null) skin = AssetDatabase.LoadAssetAtPath<UISkin>(DeckPartyPaths.Skin);
            if (skin == null) return;
            s_FocusText = id;
            Selection.activeObject = skin;
            EditorGUIUtility.PingObject(skin);
        }

        void OnEnableTextColours()
        {
            TextColourRegistry.EnsureCodeTexts((UISkin)target);
            if (s_FocusText == null) return;
            m_ShowTextColours = true;
            m_TextSearch = s_FocusText;
            m_OnlyOwn = false;
            s_FocusText = null;
        }

        void DrawTextColours()
        {
            var skin = (UISkin)target;
            var list = skin.textColours ?? new List<TextColourEntry>();
            int own = 0, built = 0;
            foreach (var entry in list)
            {
                if (entry == null) continue;
                if (entry.overrideColor) own++;
                if (entry.source != TextColourRegistry.CodeSource) built++;
            }

            m_ShowTextColours = EditorGUILayout.Foldout(m_ShowTextColours,
                $"Text Colours (stream and shop)  ({list.Count} texts, {own} with their own colour)", true, EditorStyles.foldoutHeader);
            if (!m_ShowTextColours) return;

            EditorGUILayout.LabelField(TextColoursHelp, s_Wrap);
            if (built == 0)
                EditorGUILayout.HelpBox("The scene's texts aren't listed yet. Run Deck Party > Setup > Rebuild Scenes and UI Prefabs once.",
                    MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                m_TextSearch = GUILayout.TextField(m_TextSearch, EditorStyles.toolbarSearchField);
                m_OnlyOwn = GUILayout.Toggle(m_OnlyOwn, new GUIContent("Own colours only", "Show only the texts with a colour of their own."),
                    EditorStyles.miniButton, GUILayout.Width(110));
                if (GUILayout.Button("Open all", EditorStyles.miniButtonLeft, GUILayout.Width(64))) SetTextGroupsOpen(list, true);
                if (GUILayout.Button("Close all", EditorStyles.miniButtonRight, GUILayout.Width(64))) SetTextGroupsOpen(list, false);
            }
            bool filtering = !string.IsNullOrWhiteSpace(m_TextSearch) || m_OnlyOwn;
            if (filtering) EditorGUILayout.LabelField("Matching names, what the text says, where it is and its id.", s_Faint);

            int shown = 0;
            foreach (var (group, entries) in Groups(list))
            {
                EditorGUILayout.Space(2);
                bool wasOpen = m_TextGroupOpen.TryGetValue(group, out var o) && o;
                int groupOwn = 0;
                foreach (var entry in entries) if (entry.overrideColor) groupOwn++;
                string title = groupOwn > 0 ? $"{group}  ({entries.Count}, {groupOwn} own)" : $"{group}  ({entries.Count})";
                bool open = EditorGUILayout.Foldout(wasOpen || filtering, title, true);
                if (!filtering) m_TextGroupOpen[group] = open;
                if (!open) continue;
                foreach (var entry in entries)
                {
                    if (shown++ >= MaxTextRows) break;
                    DrawTextRow(skin, entry);
                }
                if (shown > MaxTextRows)
                {
                    EditorGUILayout.LabelField($"Only the first {MaxTextRows} texts are shown: search to find the rest.", s_Faint);
                    break;
                }
            }

            if (own > 0)
            {
                EditorGUILayout.Space(2);
                if (GUILayout.Button("Give every text back the game's colour")) ClearTextColours(skin);
            }
        }

        void DrawTextRow(UISkin skin, TextColourEntry entry)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                string name = entry.missing ? entry.label + "  (no longer in the UI)" : entry.label;
                EditorGUI.BeginChangeCheck();
                bool on = EditorGUILayout.ToggleLeft(new GUIContent(name,
                    $"Ticked: this text uses the colour beside it. Unticked: the game's colour.\nId: {entry.id}\nFrom: {entry.source}"),
                    entry.overrideColor, GUILayout.MinWidth(150));
                if (EditorGUI.EndChangeCheck()) SetText(skin, entry, on, entry.color);

                GUILayout.Label(string.IsNullOrEmpty(entry.sample) ? "(empty)" : "\"" + entry.sample + "\"", s_Faint, GUILayout.MinWidth(40));

                // Always editable: picking or typing a colour ticks the text.
                EditorGUI.BeginChangeCheck();
                var picked = EditorGUILayout.ColorField(new GUIContent(string.Empty, entry.overrideColor
                    ? "This text's colour."
                    : "Pick a colour to give this text its own."), entry.color, true, false, false, GUILayout.Width(56));
                if (EditorGUI.EndChangeCheck()) SetText(skin, entry, true, picked);
                var typed = entry.color;
                if (HexField(ref typed)) SetText(skin, entry, true, typed);
                if (GUILayout.Button(new GUIContent("Find", "Highlight this text in the open scene (or the prefab it's built into)."),
                        EditorStyles.miniButton, GUILayout.Width(40)))
                    FindText(entry);
            }
            if (m_TextFound.TryGetValue(entry.id, out var found)) EditorGUILayout.LabelField(found, s_Wrap);
        }

        /// <summary>The entries matching the search, by group: the stream and shop first, the pop-ups next, anything else last.</summary>
        List<(string group, List<TextColourEntry> entries)> Groups(List<TextColourEntry> list)
        {
            var byGroup = new Dictionary<string, List<TextColourEntry>>();
            var order = new List<string>(TextColourRegistry.GroupOrder);
            string search = m_TextSearch.Trim();
            foreach (var entry in list)
            {
                if (entry == null) continue;
                if (m_OnlyOwn && !entry.overrideColor) continue;
                if (search.Length > 0 && !Contains(entry.label, search) && !Contains(entry.sample, search) && !Contains(entry.id, search) &&
                    !Contains(entry.group, search))
                    continue;
                string group = string.IsNullOrEmpty(entry.group) ? "Other" : entry.group;
                if (!byGroup.TryGetValue(group, out var entries)) byGroup[group] = entries = new List<TextColourEntry>();
                entries.Add(entry);
                if (!order.Contains(group)) order.Add(group);
            }
            // "Other" goes last.
            if (order.Remove("Other")) order.Add("Other");
            var result = new List<(string, List<TextColourEntry>)>();
            foreach (var group in order)
                if (byGroup.TryGetValue(group, out var entries)) result.Add((group, entries));
            return result;
        }

        void SetTextGroupsOpen(List<TextColourEntry> list, bool open)
        {
            foreach (var entry in list)
                if (entry != null) m_TextGroupOpen[string.IsNullOrEmpty(entry.group) ? "Other" : entry.group] = open;
        }

        /// <summary>Saves a text's colour and shows it straight away.</summary>
        static void SetText(UISkin skin, TextColourEntry entry, bool on, Color colour)
        {
            Undo.RecordObject(skin, "Text Colour");
            entry.overrideColor = on;
            entry.color = new Color(colour.r, colour.g, colour.b, 1f);
            EditorUtility.SetDirty(skin);
            skin.NotifyChanged();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        static void ClearTextColours(UISkin skin)
        {
            if (!EditorUtility.DisplayDialog("Text Colours", "Untick every text, so they all use the game's colours again?", "Untick all", "Cancel"))
                return;
            Undo.RecordObject(skin, "Clear Text Colours");
            foreach (var entry in skin.textColours)
                if (entry != null) entry.overrideColor = false;
            // Texts the UI no longer has are only kept for their colour.
            skin.textColours.RemoveAll(e => e == null || e.missing);
            EditorUtility.SetDirty(skin);
            skin.NotifyChanged();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            GUIUtility.ExitGUI();
        }

        /// <summary>Pings the text in the open scene or prefab, or the prefab it's built into, and says where it is.</summary>
        void FindText(TextColourEntry entry)
        {
            if (entry.source == TextColourRegistry.CodeSource)
            {
                m_TextFound[entry.id] = "The game makes this text while playing, so it isn't in the scene. Press Play to see it.";
                return;
            }
            var hits = new List<Object>();
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            var texts = stage != null
                ? stage.prefabContentsRoot.GetComponentsInChildren<SkinnedText>(true)
                : Object.FindObjectsByType<SkinnedText>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var text in texts)
                if (text != null && text.ColourId == entry.id) hits.Add(text.gameObject);
            if (hits.Count > 0)
            {
                EditorGUIUtility.PingObject(hits[0]);
                Selection.objects = hits.ToArray();
                m_TextFound[entry.id] = $"In {(stage != null ? "the open prefab" : "the open scene")}: {Names(hits)} (selected).";
                return;
            }
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { DeckPartyPaths.Prefabs }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab == null) continue;
                foreach (var text in prefab.GetComponentsInChildren<SkinnedText>(true))
                {
                    if (text.ColourId != entry.id) continue;
                    EditorGUIUtility.PingObject(prefab);
                    m_TextFound[entry.id] = $"Built into the {prefab.name} prefab (highlighted in the Project window; double-click it to open it).";
                    return;
                }
            }
            m_TextFound[entry.id] = entry.missing
                ? "The last rebuild didn't make this text any more. Untick it, or keep it in case it comes back."
                : GameSceneOpen()
                    ? "Not in the open scene. Run Deck Party > Setup > Rebuild Scenes and UI Prefabs if the scene is older than this list."
                    : "Open the Game scene (Deck Party > Open Game Scene) to find it.";
        }
    }
}
