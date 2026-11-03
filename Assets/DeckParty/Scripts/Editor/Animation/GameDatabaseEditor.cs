using DeckParty.Core;
using UnityEditor;
using UnityEngine;

namespace DeckParty.EditorTools
{
    /// <summary>
    /// The Game Database inspector (Deck Party > Select Game Database): the stream art (viewer emojis, multiplayer emotes,
    /// Deplatform / Server Down images, community icons), the platforms (animated logos and button colours), one compact row
    /// per streamer with their platform and quotes, and one row per viewer with search and filters, the sprite (always the
    /// fallback), the round chat avatar, an optional Character Prefab and Animator Controller (which override the sprite),
    /// framing, and a checklist of the animation states the game looks for.
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
            "viewerEmojis", "emotes", "deplatformImage", "serverDownImage", "communityIcons",
        };

        /// <summary>Fields drawn elsewhere or no longer used (the V5 Stream Chat window replaced the chat bubble and its tail).</summary>
        static readonly string[] s_Hidden =
        {
            "chatBubble", "chatBubbleTail", "chatBubbleTint", "platforms", "noStreamerPlatform",
        };

        string m_Search = string.Empty;
        Filter m_Filter;
        bool m_ShowStreamArt = true;
        bool m_ShowPlatforms = true;
        bool m_ShowViewers = true;
        bool m_ShowStreamers = true;
        int m_Expanded = -1;
        int m_ExpandedStreamer = -1;
        int m_ExpandedPlatform = -1;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var db = (GameDatabase)target;

            EditorGUILayout.HelpBox(
                "Each viewer shows its Portrait sprite unless it has a Character Prefab or Animator Controller, which animate instead. " +
                "States the game plays: Idle (no action) or Idle_Pre_Active / Active / Idle_Post_Active (with an action), Score, and optional Enter, Exit, Hit, Celebrate. " +
                "Deck Party > Art > Validate Character Animations checks everything.", MessageType.Info);

            m_ShowStreamArt = EditorGUILayout.Foldout(m_ShowStreamArt, "Stream Art (emojis, emotes, stream endings, community icons)", true, EditorStyles.foldoutHeader);
            if (m_ShowStreamArt) DrawStreamArt();

            EditorGUILayout.Space(8);
            m_ShowPlatforms = EditorGUILayout.Foldout(m_ShowPlatforms, $"Platforms ({db.platforms.Count}: animated logos and button colours)", true, EditorStyles.foldoutHeader);
            if (m_ShowPlatforms) DrawPlatforms(db);

            EditorGUILayout.Space(8);
            m_ShowStreamers = EditorGUILayout.Foldout(m_ShowStreamers, $"Streamers ({db.streamers.Count})", true, EditorStyles.foldoutHeader);
            if (m_ShowStreamers) DrawStreamers(db);

            EditorGUILayout.Space(8);
            m_ShowViewers = EditorGUILayout.Foldout(m_ShowViewers, $"Viewers ({GuestCatalog.Roster.Count} in the roster, plus the Bot)", true, EditorStyles.foldoutHeader);
            if (m_ShowViewers) DrawViewers(db);

            EditorGUILayout.Space(8);
            var excluded = new System.Collections.Generic.List<string> { "m_Script", "guests", "streamers" };
            excluded.AddRange(s_StreamArt);
            excluded.AddRange(s_Hidden);
            DrawPropertiesExcluding(serializedObject, excluded.ToArray());
            serializedObject.ApplyModifiedProperties();
        }

        void DrawStreamArt()
        {
            EditorGUILayout.HelpBox(
                "Emojis and Emotes: slice the sheet (Sprite Mode Multiple), select every slice in the Project window and drag them onto the list. " +
                "Stream Endings show full-screen when a stream gets deplatformed or the server goes down. " +
                "Community Icons appear on cards, shop tiles, tooltips, inside text and on the Community Beats chips. " +
                "The resource icons (Clout, Cash, Deplatform Risk, Plays) and the button icons live in the UI Skin: IconPop, IconCash, IconTrouble, " +
                "IconStar, and IconAddViewer, IconEndStream, IconAudience, IconMenu, IconAction, IconGoLive, IconClose, IconNight.", MessageType.None);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                foreach (var name in s_StreamArt)
                {
                    var property = serializedObject.FindProperty(name);
                    if (property != null) EditorGUILayout.PropertyField(property, true);
                }
            }
        }

        /// <summary>
        /// One block per satirical platform: the button colour and the animated logo's sprites, placement and timing, each
        /// labelled with what it means on that platform (unused slots are hidden).
        /// </summary>
        void DrawPlatforms(GameDatabase db)
        {
            if (db.platforms == null || db.platforms.Count == 0)
            {
                EditorGUILayout.HelpBox("No platforms yet. Run Deck Party > Setup > Build Everything to add one entry per platform.", MessageType.Warning);
                return;
            }
            EditorGUILayout.HelpBox(
                "Each streamer streams on one of these platforms (Streamers > Platform). The platform picks the animated logo at the top-left of the " +
                "stream and shop, and the colour of its main buttons. Empty sprite slots use simple built-in shapes, so every logo animates before " +
                "you add art. Placement points are fractions of the logo box (0,0 bottom-left, 1,1 top-right). Changes show next game.", MessageType.None);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("noStreamerPlatform"), new GUIContent("No Streamer Platform"));

            var platforms = serializedObject.FindProperty("platforms");
            for (int i = 0; i < platforms.arraySize && i < db.platforms.Count; i++)
            {
                var art = db.platforms[i];
                if (art == null) continue;
                var element = platforms.GetArrayElementAtIndex(i);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var preview = art.logo != null ? AssetPreview.GetAssetPreview(art.logo) : null;
                        GUILayout.Label(preview != null ? preview : Texture2D.grayTexture, GUILayout.Width(40), GUILayout.Height(40));
                        using (new EditorGUILayout.VerticalScope())
                        {
                            int users = db.streamers.FindAll(s => s != null && s.platform == art.platform).Count;
                            EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(art.platform.ToString()), $"{users} streamer{(users == 1 ? "" : "s")}", EditorStyles.boldLabel);
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUILayout.PropertyField(element.FindPropertyRelative("accent"), new GUIContent("Button Colour"));
                                EditorGUILayout.PropertyField(element.FindPropertyRelative("accentContent"), GUIContent.none, GUILayout.Width(60));
                            }
                        }
                        if (GUILayout.Button(m_ExpandedPlatform == i ? "Less" : "Logo...", GUILayout.Width(90), GUILayout.Height(38)))
                            m_ExpandedPlatform = m_ExpandedPlatform == i ? -1 : i;
                    }
                    if (m_ExpandedPlatform != i) continue;

                    var slots = new[] { "logo", "partA", "partB", "partC" };
                    for (int slot = 0; slot < slots.Length; slot++)
                    {
                        string label = PlatformArt.PartLabel(art.platform, slot);
                        if (label != null) EditorGUILayout.PropertyField(element.FindPropertyRelative(slots[slot]), new GUIContent(label));
                    }
                    foreach (var field in new[] { "pointA", "pointB", "partSize", "travel" })
                    {
                        string label = PlatformArt.ValueLabel(art.platform, field);
                        if (label != null) EditorGUILayout.PropertyField(element.FindPropertyRelative(field), new GUIContent(label));
                    }
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("size"), new GUIContent("Logo Box Size (px)"));
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("speed"), new GUIContent("Speed"));
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("rest"), new GUIContent(art.platform == StreamPlatform.Lookout ? "Hold At Centre (s)" : "Rest Between Loops (s)"));
                    if (GUILayout.Button("Reset Placement and Timing"))
                    {
                        serializedObject.ApplyModifiedProperties();
                        Undo.RecordObject(target, "Reset Platform");
                        var defaults = PlatformArt.CreateDefault(art.platform);
                        art.size = defaults.size;
                        art.pointA = defaults.pointA;
                        art.pointB = defaults.pointB;
                        art.partSize = defaults.partSize;
                        art.travel = defaults.travel;
                        art.speed = defaults.speed;
                        art.rest = defaults.rest;
                        EditorUtility.SetDirty(target);
                        serializedObject.Update();
                    }
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
                        EditorGUILayout.PropertyField(element.FindPropertyRelative("platform"), new GUIContent("Platform (logo, buttons)"));
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
                EditorGUILayout.LabelField("States: Idle, Select (picked), Anxious (one risk from being deplatformed), optional Hit and Celebrate (facecam).", EditorStyles.miniLabel);
            }
        }

        void DrawViewers(GameDatabase db)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                m_Search = EditorGUILayout.TextField(m_Search, EditorStyles.toolbarSearchField);
                m_Filter = (Filter)EditorGUILayout.EnumPopup(m_Filter, GUILayout.Width(110));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Communities: tick any number per viewer (Nothing = no community). Edits apply straight away.", EditorStyles.miniLabel);
                if (GUILayout.Button("Reset All Communities", GUILayout.Width(170)) &&
                    EditorUtility.DisplayDialog("Reset communities", "Put every viewer back on the built-in communities (the V4.2 card table)? Your community edits are lost.", "Reset", "Cancel"))
                {
                    serializedObject.ApplyModifiedProperties();
                    Undo.RecordObject(target, "Reset Communities");
                    db.ResetCommunitiesToDefaults();
                    EditorUtility.SetDirty(target);
                    serializedObject.Update();
                }
            }

            EditorGUILayout.LabelField("Only the current roster is listed (put-aside viewers keep their art in the database, hidden). " +
                                       "The Bot is the token the Bots community sends: give it a portrait like any viewer.", EditorStyles.wordWrappedMiniLabel);

            var guests = serializedObject.FindProperty("guests");
            for (int i = 0; i < guests.arraySize && i < db.guests.Count; i++)
            {
                var art = db.guests[i];
                if (art == null || GuestCatalog.IsPutAside(art.type) || !Matches(art)) continue;
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
                        string kind = GuestCatalog.IsToken(art.type) ? "Bots token (never bought)" : info.HasAbility ? "action: " + info.AbilityName : "no action";
                        EditorGUILayout.LabelField(art.name + badge, kind, EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(element.FindPropertyRelative("portrait"), new GUIContent("Portrait (fallback)"));
                        EditorGUILayout.PropertyField(element.FindPropertyRelative("avatar"), new GUIContent("Avatar (head, chat circle)"));
                        if (!GuestCatalog.IsToken(art.type))
                            EditorGUILayout.PropertyField(element.FindPropertyRelative("communities"), new GUIContent("Communities"));
                    }
                    if (GUILayout.Button(m_Expanded == index ? "Less" : "Animation...", GUILayout.Width(90), GUILayout.Height(38)))
                        m_Expanded = m_Expanded == index ? -1 : index;
                }

                if (m_Expanded != index) return;
                if (art.avatar == null)
                {
                    EditorGUILayout.LabelField("No Avatar sprite: the chat circle shows the portrait, zoomed in on the head with these:", EditorStyles.miniLabel);
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("avatarZoom"));
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("avatarOffset"));
                }
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
