using System;
using System.Collections.Generic;
using DeckParty.Core;
using DeckParty.Platforms;
using UnityEditor;
using UnityEngine;

namespace DeckParty.EditorTools
{
    /// <summary>
    /// Deck Party > Achievements and Unlocks Tester: everything about achievements and unlocks in one window, to test that
    /// conditions fire and that locked streamers and emotes behave.
    /// <list type="bullet">
    /// <item><b>Simulate</b>: report a finished single-player, multiplayer or Endless game, exactly as the game does, so
    /// the real counting and unlocking runs (toasts included in Play Mode).</item>
    /// <item><b>Achievements</b>: each one's state and counter, Unlock / Lock buttons, and what it unlocks.</item>
    /// <item><b>Counters</b>: set or bump any counter.</item>
    /// <item><b>Streamers / Emotes</b>: their unlock condition and state, and a testing override (force unlocked or locked).</item>
    /// <item><b>Store</b>: sync with Steam / Epic, send what's waiting, lock achievements on Steam again.</item>
    /// </list>
    /// Works in Play Mode (live) and Edit Mode (changes this PC's saved progress, used the next time you press Play).
    /// Overrides only ever apply in the Editor and development builds.
    /// </summary>
    internal sealed class AchievementsTesterWindow : EditorWindow
    {
        Vector2 m_Scroll;
        bool m_ShowSimulate = true;
        bool m_ShowAchievements = true;
        bool m_ShowCounters = true;
        bool m_ShowStreamers = true;
        bool m_ShowEmotes = true;
        bool m_ShowStore = true;
        string m_StoreReport = string.Empty;
        GUIStyle m_Wrap;

        [MenuItem("Deck Party/Achievements and Unlocks Tester", priority = 45)]
        static void Open()
        {
            var window = GetWindow<AchievementsTesterWindow>("Achievements & Unlocks");
            window.minSize = new Vector2(560, 420);
            window.Show();
        }

        void OnEnable()
        {
            Achievements.Changed += Repaint;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        void OnDisable()
        {
            Achievements.Changed -= Repaint;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        void OnPlayModeChanged(PlayModeStateChange change)
        {
            // Play Mode wrote PlayerPrefs (and statics reset): read everything again.
            if (change == PlayModeStateChange.EnteredEditMode || change == PlayModeStateChange.EnteredPlayMode)
            {
                Achievements.Changed -= Repaint;
                Achievements.Changed += Repaint;
                ReloadFromDisk();
            }
        }

        void OnFocus()
        {
            if (!Application.isPlaying) ReloadFromDisk();
        }

        static void ReloadFromDisk()
        {
            Achievements.Reload();
            Unlocks.ReloadOverrides();
        }

        void OnGUI()
        {
            m_Wrap ??= new GUIStyle(EditorStyles.wordWrappedLabel) { richText = true };
            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);

            DrawHeader();
            Section(ref m_ShowSimulate, "Simulate finished games", DrawSimulate);
            Section(ref m_ShowAchievements, $"Achievements ({Achievements.UnlockedCount}/{Achievements.All.Count} unlocked)", DrawAchievements);
            Section(ref m_ShowCounters, "Counters", DrawCounters);
            Section(ref m_ShowStreamers, "Streamers", DrawStreamers);
            Section(ref m_ShowEmotes, $"Emotes ({Networking.Emotes.Count})", DrawEmotes);
            Section(ref m_ShowStore, "Store (Steam / Epic)", DrawStore);
            DrawDanger();

            EditorGUILayout.EndScrollView();
        }

        static void Section(ref bool open, string title, Action draw)
        {
            EditorGUILayout.Space(6);
            open = EditorGUILayout.BeginFoldoutHeaderGroup(open, title);
            if (open)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) draw();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ================================================================== header

        void DrawHeader()
        {
            string mode = Application.isPlaying
                ? "<b>Play Mode</b>: changes apply to the running game straight away (toasts included)."
                : "<b>Edit Mode</b>: changes are saved to this PC's progress and used the next time you press Play.";
            EditorGUILayout.LabelField(mode + " Progress is local (PlayerPrefs); the stores keep their own copy (see Store).", m_Wrap);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Select Achievement Library"))
                {
                    var library = DeckPartyAssets.EnsureAchievementLibrary();
                    AssetDatabase.SaveAssets();
                    Selection.activeObject = library;
                    EditorGUIUtility.PingObject(library);
                }
                if (GUILayout.Button("Select Game Database (unlock conditions)"))
                {
                    var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(DeckPartyPaths.Database);
                    if (db != null)
                    {
                        Selection.activeObject = db;
                        EditorGUIUtility.PingObject(db);
                    }
                }
                if (GUILayout.Button("Refresh", GUILayout.Width(80))) ReloadFromDisk();
            }
            if (AssetDatabase.LoadAssetAtPath<AchievementLibrary>(DeckPartyPaths.AchievementLibrary) == null)
                EditorGUILayout.HelpBox("There's no Achievement Library asset yet: the game uses the five starters. Select Achievement Library " +
                                        "(or run Build Everything) to create it and add your own.", MessageType.Info);
        }

        // ================================================================== simulate

        void DrawSimulate()
        {
            EditorGUILayout.LabelField("These report a finished game exactly as the game does at its end screen, so the real counters, " +
                                       "achievements and unlocks run.", m_Wrap);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Single-player game")) Achievements.ReportGameFinished(false, false);
                if (GUILayout.Button("Multiplayer game")) Achievements.ReportGameFinished(true, false);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Solo Endless run")) Achievements.ReportGameFinished(false, true);
                if (GUILayout.Button("Co-op Endless run")) Achievements.ReportGameFinished(true, true);
            }
        }

        // ================================================================== achievements

        void DrawAchievements()
        {
            foreach (var definition in Achievements.All)
            {
                if (definition == null) continue;
                bool unlocked = Achievements.IsUnlocked(definition.id);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var previous = GUI.color;
                        GUI.color = unlocked ? new Color(0.55f, 1f, 0.6f) : new Color(1f, 0.8f, 0.55f);
                        GUILayout.Label(unlocked ? "UNLOCKED" : "LOCKED", EditorStyles.miniButton, GUILayout.Width(80));
                        GUI.color = previous;
                        EditorGUILayout.LabelField($"{Achievements.Name(definition)}  ({definition.id})", EditorStyles.boldLabel);
                        using (new EditorGUI.DisabledScope(unlocked))
                            if (GUILayout.Button("Unlock", GUILayout.Width(70))) Achievements.Unlock(definition.id);
                        using (new EditorGUI.DisabledScope(!unlocked && Achievements.GetStat(definition.stat) < definition.target))
                            if (GUILayout.Button(new GUIContent("Lock", "Locks it again and drops its counter below the target, so it can be earned again."), GUILayout.Width(60)))
                                Achievements.Lock(definition.id);
                    }
                    EditorGUILayout.LabelField(Achievements.Description(definition, true), EditorStyles.wordWrappedMiniLabel);
                    string counter = definition.stat == AchievementStat.None
                        ? "Counter: none (only Unlock / code unlocks it)"
                        : $"Counter: {definition.stat} {Achievements.GetStat(definition.stat)}/{definition.target}";
                    var time = Achievements.UnlockTime(definition.id);
                    if (time.HasValue) counter += $"   Unlocked {time.Value:yyyy-MM-dd HH:mm}";
                    EditorGUILayout.LabelField(counter, EditorStyles.miniLabel);
                    string stores = $"Steam: {Or(definition.platformIds?.steam)}   Epic: {Or(definition.platformIds?.epic)}";
                    EditorGUILayout.LabelField(stores, EditorStyles.miniLabel);
                    string unlocks = WhatItUnlocks(definition.id);
                    if (unlocks.Length > 0) EditorGUILayout.LabelField("Unlocks: " + unlocks, EditorStyles.wordWrappedMiniLabel);
                }
            }
        }

        static string Or(string value) => string.IsNullOrEmpty(value) ? "-" : value;

        static string WhatItUnlocks(string id)
        {
            var parts = new List<string>();
            foreach (var type in StreamerCatalog.Roster)
                if (Unlocks.StreamerCondition(type) == id) parts.Add(type.ToString());
            int emotes = Networking.Emotes.Count;
            for (int i = 0; i < emotes; i++)
                if (Unlocks.EmoteCondition(i) == id) parts.Add($"emote {i + 1}");
            return string.Join(", ", parts);
        }

        // ================================================================== counters

        void DrawCounters()
        {
            foreach (AchievementStat stat in Enum.GetValues(typeof(AchievementStat)))
            {
                if (stat == AchievementStat.None) continue;
                using (new EditorGUILayout.HorizontalScope())
                {
                    int value = Achievements.GetStat(stat);
                    EditorGUI.BeginChangeCheck();
                    int changed = EditorGUILayout.DelayedIntField(ObjectNames.NicifyVariableName(stat.ToString()), value);
                    if (EditorGUI.EndChangeCheck()) Achievements.SetStat(stat, changed);
                    if (GUILayout.Button("+1", GUILayout.Width(40))) Achievements.AddStat(stat, 1);
                    if (GUILayout.Button("0", GUILayout.Width(30))) Achievements.SetStat(stat, 0);
                }
            }
            EditorGUILayout.LabelField("Setting a counter to or past an achievement's target unlocks it; lowering it doesn't lock anything (use Lock).", EditorStyles.wordWrappedMiniLabel);
        }

        // ================================================================== streamers and emotes

        void DrawStreamers()
        {
            DrawUnlockEverything();
            foreach (var type in StreamerCatalog.Roster)
            {
                string condition = Unlocks.StreamerCondition(type);
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool unlocked = Unlocks.IsStreamerUnlocked(type);
                    StateLabel(unlocked);
                    EditorGUILayout.LabelField(type.ToString(), ConditionText(condition));
                    var current = Unlocks.StreamerOverride(type);
                    EditorGUI.BeginChangeCheck();
                    var picked = (Unlocks.Override)EditorGUILayout.EnumPopup(current, GUILayout.Width(130));
                    if (EditorGUI.EndChangeCheck()) Unlocks.SetStreamerOverride(type, picked);
                }
            }
            EditorGUILayout.LabelField("The override is for testing only (Editor and development builds). Conditions are set in the Game Database > Streamers.", EditorStyles.wordWrappedMiniLabel);
        }

        void DrawEmotes()
        {
            int count = Networking.Emotes.Count;
            if (count == 0)
            {
                EditorGUILayout.LabelField("No emotes.");
                return;
            }
            for (int i = 0; i < count; i++)
            {
                string condition = Unlocks.EmoteCondition(i);
                using (new EditorGUILayout.HorizontalScope())
                {
                    StateLabel(Unlocks.IsEmoteUnlocked(i));
                    var sprite = Networking.Emotes.SpriteAt(i);
                    string name = sprite != null ? $"Emote {i + 1} ({sprite.name})" : $"Emote {i + 1} ({Networking.Emotes.TextAt(i)})";
                    EditorGUILayout.LabelField(name, ConditionText(condition));
                    var current = Unlocks.EmoteOverride(i);
                    EditorGUI.BeginChangeCheck();
                    var picked = (Unlocks.Override)EditorGUILayout.EnumPopup(current, GUILayout.Width(130));
                    if (EditorGUI.EndChangeCheck()) Unlocks.SetEmoteOverride(i, picked);
                }
            }
            EditorGUILayout.LabelField("Conditions are set in the Game Database > Stream Art > Emote Unlock Conditions.", EditorStyles.wordWrappedMiniLabel);
        }

        static void DrawUnlockEverything()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                bool all = EditorGUILayout.ToggleLeft("Unlock every streamer and emote (testing)", Unlocks.UnlockEverything);
                if (EditorGUI.EndChangeCheck()) Unlocks.UnlockEverything = all;
                if (GUILayout.Button("Clear all overrides", GUILayout.Width(140))) Unlocks.ClearOverrides();
            }
        }

        static void StateLabel(bool unlocked)
        {
            var previous = GUI.color;
            GUI.color = unlocked ? new Color(0.55f, 1f, 0.6f) : new Color(1f, 0.8f, 0.55f);
            GUILayout.Label(unlocked ? "open" : "locked", EditorStyles.miniButton, GUILayout.Width(60));
            GUI.color = previous;
        }

        static string ConditionText(string condition)
        {
            if (string.IsNullOrEmpty(condition)) return "Condition: none (unlocked from the start)";
            var definition = Achievements.Find(condition);
            if (definition == null) return $"Condition: MISSING achievement '{condition}' (counts as met)";
            return $"Condition: {Achievements.Name(definition)} ({condition}){(Achievements.IsUnlocked(condition) ? " - earned" : string.Empty)}";
        }

        // ================================================================== store

        void DrawStore()
        {
            var platform = OnlinePlatforms.Current;
            if (!Application.isPlaying)
            {
                EditorGUILayout.LabelField("The stores only run in Play Mode (or a build). Enter Play Mode with Steam or Epic as the online platform to sync.", m_Wrap);
                return;
            }
            string running = platform == null ? "None (Default multiplayer): achievements stay on this PC." : platform.DisplayName;
            bool ready = PlatformAchievementSync.Target != null;
            EditorGUILayout.LabelField("Platform", running);
            EditorGUILayout.LabelField("Achievements", ready ? "available" : "not available");
            EditorGUILayout.LabelField("Waiting to send", PlatformAchievementSync.PendingCount.ToString());
            EditorGUILayout.LabelField(PlatformAchievementSync.LastStatus, EditorStyles.wordWrappedMiniLabel);
            using (new EditorGUI.DisabledScope(!ready))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Sync now")) PlatformAchievementSync.Sync();
                if (GUILayout.Button("Send waiting")) PlatformAchievementSync.Flush();
                if (GUILayout.Button(new GUIContent("Lock all on the store", "Steam only (ClearAchievement). Epic can't lock achievements from the game: reset the player in the Epic Developer Portal.")))
                {
                    var ids = new List<string>();
                    foreach (var definition in Achievements.All)
                        if (definition != null) ids.Add(definition.id);
                    m_StoreReport = string.Empty;
                    PlatformAchievementSync.ResetOnPlatform(ids, line => { m_StoreReport += line + "\n"; Repaint(); });
                }
            }
            if (m_StoreReport.Length > 0) EditorGUILayout.HelpBox(m_StoreReport.TrimEnd(), MessageType.None);
        }

        // ================================================================== reset

        static void DrawDanger()
        {
            EditorGUILayout.Space(10);
            if (GUILayout.Button("Reset all achievement progress on this PC") &&
                EditorUtility.DisplayDialog("Reset achievement progress?",
                    "Clears every achievement, counter and streamer / emote unlock on this PC (the stores keep theirs). Overrides are kept.",
                    "Reset", "Cancel"))
            {
                Achievements.ResetAll();
            }
        }
    }
}
