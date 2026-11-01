using DeckParty.Core;
using UnityEditor;
using UnityEngine;

namespace DeckParty.EditorTools
{
    /// <summary>
    /// The Game Database inspector (Deck Party > Select Game Database): the stream art (chat bubble and tail, viewer emojis,
    /// multiplayer emotes, Platform Ban / Server Down images, group icons), one compact row per streamer with their quotes,
    /// and one row per viewer with search and filters, the sprite (always the fallback), an optional Character Prefab and
    /// Animator Controller (which override the sprite), framing, and a checklist of the animation states the game looks for.
    /// </summary>
    [CustomEditor(typeof(GameDatabase))]
    internal sealed class GameDatabaseEditor : Editor
    {
        enum Filter
        {
            All,
            MissingArt,
            HasAction,
            Animated,
        }

        /// <summary>The stream art fields, drawn in their own section (and left out of the generic list at the bottom).</summary>
        static readonly string[] s_StreamArt =
        {
            "chatBubble", "chatBubbleTail", "chatBubbleTint", "viewerEmojis", "emotes", "platformBanImage", "serverDownImage", "groupIcons",
        };

        string m_Search = string.Empty;
        Filter m_Filter;
        bool m_ShowStreamArt = true;
        bool m_ShowViewers = true;
        bool m_ShowStreamers = true;
        int m_Expanded = -1;
        int m_ExpandedStreamer = -1;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var db = (GameDatabase)target;

            EditorGUILayout.HelpBox(
                "Each viewer shows its Portrait sprite unless it has a Character Prefab or Animator Controller, which animate instead. " +
                "States the game plays: Idle (no action) or Idle_Pre_Active / Active / Idle_Post_Active (with an action), Score, and optional Enter, Exit, Hit, Celebrate. " +
                "Deck Party > Art > Validate Character Animations checks everything.", MessageType.Info);

            m_ShowStreamArt = EditorGUILayout.Foldout(m_ShowStreamArt, "Stream Art (chat bubble, emojis, emotes, stream endings, group icons)", true, EditorStyles.foldoutHeader);
            if (m_ShowStreamArt) DrawStreamArt();

            EditorGUILayout.Space(8);
            m_ShowStreamers = EditorGUILayout.Foldout(m_ShowStreamers, $"Streamers ({db.streamers.Count})", true, EditorStyles.foldoutHeader);
            if (m_ShowStreamers) DrawStreamers(db);

            EditorGUILayout.Space(8);
            m_ShowViewers = EditorGUILayout.Foldout(m_ShowViewers, $"Viewers ({db.guests.Count})", true, EditorStyles.foldoutHeader);
            if (m_ShowViewers) DrawViewers(db);

            EditorGUILayout.Space(8);
            var excluded = new System.Collections.Generic.List<string> { "m_Script", "guests", "streamers" };
            excluded.AddRange(s_StreamArt);
            DrawPropertiesExcluding(serializedObject, excluded.ToArray());
            serializedObject.ApplyModifiedProperties();
        }

        void DrawStreamArt()
        {
            EditorGUILayout.HelpBox(
                "Chat Bubble: a square sprite with 9-slice borders (Sprite Editor). Tail: a sprite pointing down, placed on the bubble's top edge; " +
                "its spot along the edge is set per stream setup (Venue > Chat Tail Position). " +
                "Emojis and Emotes: slice the sheet (Sprite Mode Multiple), select every slice in the Project window and drag them onto the list. " +
                "Stream Endings show full-screen when a stream gets a Platform Ban or the server goes down.", MessageType.None);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                foreach (var name in s_StreamArt)
                {
                    var property = serializedObject.FindProperty(name);
                    if (property != null) EditorGUILayout.PropertyField(property, true);
                }
            }
        }

        void DrawStreamers(GameDatabase db)
        {
            if (db.streamers.Count == 0)
            {
                EditorGUILayout.HelpBox("No streamers yet. Run Deck Party > Setup > Build Everything to add one entry per streamer.", MessageType.Warning);
                return;
            }
            EditorGUILayout.HelpBox(
                "Quotes show under the streamer on the Select Your Streamer screen: one is picked at random each time they're highlighted. " +
                "Use + to add lines; they appear exactly as typed in every language.", MessageType.None);

            var streamers = serializedObject.FindProperty("streamers");
            for (int i = 0; i < streamers.arraySize && i < db.streamers.Count; i++)
            {
                var art = db.streamers[i];
                if (art == null) continue;
                DrawStreamerRow(art, streamers.GetArrayElementAtIndex(i), i);
            }
        }

        void DrawStreamerRow(StreamerArt art, SerializedProperty element, int index)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var preview = art.portrait != null ? AssetPreview.GetAssetPreview(art.portrait) : null;
                    GUILayout.Label(preview != null ? preview : Texture2D.grayTexture, GUILayout.Width(40), GUILayout.Height(40));

                    using (new EditorGUILayout.VerticalScope())
                    {
                        var info = StreamerCatalog.Get(art.type);
                        string badge = art.IsAnimated ? "  [animated]" : art.portrait != null ? "  [sprite]" : "  [placeholder]";
                        int quoteCount = art.quotes != null ? art.quotes.FindAll(q => !string.IsNullOrWhiteSpace(q)).Count : 0;
                        EditorGUILayout.LabelField(art.name + badge, $"{info.Kind} power  -  {quoteCount} quote{(quoteCount == 1 ? "" : "s")}", EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(element.FindPropertyRelative("portrait"), new GUIContent("Portrait"));
                    }
                    if (GUILayout.Button(m_ExpandedStreamer == index ? "Less" : "More...", GUILayout.Width(90), GUILayout.Height(38)))
                        m_ExpandedStreamer = m_ExpandedStreamer == index ? -1 : index;
                }

                EditorGUILayout.PropertyField(element.FindPropertyRelative("quotes"), new GUIContent("Quotes (one at random)"), true);

                if (m_ExpandedStreamer != index) return;
                EditorGUILayout.PropertyField(element.FindPropertyRelative("venue"), new GUIContent("Stream Setup (background)"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("color"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("characterPrefab"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("animatorController"));
                EditorGUILayout.LabelField("States: Idle, Select (picked), Anxious (one risk from a Platform Ban), optional Hit and Celebrate (facecam).", EditorStyles.miniLabel);
            }
        }

        void DrawViewers(GameDatabase db)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                m_Search = EditorGUILayout.TextField(m_Search, EditorStyles.toolbarSearchField);
                m_Filter = (Filter)EditorGUILayout.EnumPopup(m_Filter, GUILayout.Width(110));
            }

            var guests = serializedObject.FindProperty("guests");
            for (int i = 0; i < guests.arraySize && i < db.guests.Count; i++)
            {
                var art = db.guests[i];
                if (art == null || !Matches(art)) continue;
                var element = guests.GetArrayElementAtIndex(i);
                DrawRow(art, element, i);
            }
        }

        bool Matches(GuestArt art)
        {
            if (!string.IsNullOrEmpty(m_Search) && (art.name ?? string.Empty).IndexOf(m_Search, System.StringComparison.OrdinalIgnoreCase) < 0) return false;
            switch (m_Filter)
            {
                case Filter.MissingArt: return art.portrait == null && !art.IsAnimated;
                case Filter.HasAction: return GuestCatalog.Get(art.type).HasAbility;
                case Filter.Animated: return art.IsAnimated;
                default: return true;
            }
        }

        void DrawRow(GuestArt art, SerializedProperty element, int index)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var preview = art.portrait != null ? AssetPreview.GetAssetPreview(art.portrait) : null;
                    GUILayout.Label(preview != null ? preview : Texture2D.grayTexture, GUILayout.Width(40), GUILayout.Height(40));

                    using (new EditorGUILayout.VerticalScope())
                    {
                        var info = GuestCatalog.Get(art.type);
                        string badge = art.IsAnimated ? "  [animated]" : art.portrait != null ? "  [sprite]" : "  [placeholder]";
                        EditorGUILayout.LabelField(art.name + badge, info.HasAbility ? "action: " + info.AbilityName : "no action", EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(element.FindPropertyRelative("portrait"), new GUIContent("Portrait (fallback)"));
                    }
                    if (GUILayout.Button(m_Expanded == index ? "Less" : "Animation...", GUILayout.Width(90), GUILayout.Height(38)))
                        m_Expanded = m_Expanded == index ? -1 : index;
                }

                if (m_Expanded != index) return;
                EditorGUILayout.PropertyField(element.FindPropertyRelative("characterPrefab"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("animatorController"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("framingZoom"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("framingOffset"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("tint"));

                var required = CharacterAnimationTools.RequiredStates(art.type);
                var names = new System.Text.StringBuilder("Needs: ");
                foreach (var state in required) names.Append(CharacterAnimDriver.StateName(state)).Append("  ");
                EditorGUILayout.LabelField(names.ToString(), EditorStyles.miniLabel);
                if (art.IsAnimated)
                {
                    var missing = CharacterAnimationTools.MissingStates(art);
                    EditorGUILayout.LabelField(missing.Count == 0 ? "All required states found." : "Missing: " + string.Join(", ", missing),
                        missing.Count == 0 ? EditorStyles.miniLabel : EditorStyles.miniBoldLabel);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Create Override Controller"))
                    {
                        serializedObject.ApplyModifiedProperties();
                        Undo.RecordObject(target, "Create Override Controller");
                        var controller = CharacterAnimationTools.CreateOverride(art);
                        EditorUtility.SetDirty(target);
                        EditorGUIUtility.PingObject(controller);
                        serializedObject.Update();
                    }
                    if (GUILayout.Button("Clear Animation"))
                    {
                        element.FindPropertyRelative("characterPrefab").objectReferenceValue = null;
                        element.FindPropertyRelative("animatorController").objectReferenceValue = null;
                    }
                }
            }
        }
    }
}
