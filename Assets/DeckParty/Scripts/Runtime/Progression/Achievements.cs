using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeckParty
{
    /// <summary>
    /// The game's own achievements: counters (<see cref="AchievementStat"/>) the game bumps as you play, and achievements
    /// (<see cref="AchievementLibrary"/>) that unlock when their counter reaches its target. Unlocks are kept on this PC
    /// (PlayerPrefs "dp.achievements.v1"), unlock streamers and emotes (<see cref="Unlocks"/>), and are passed on to the
    /// store the game runs on (<see cref="PlatformAchievementSync"/>: Steam, Epic). Unlocks a store already has are
    /// brought back in when it signs in, so a new PC gets them.
    /// <para>
    /// Everything runs on the main thread. Deck Party > Achievements and Unlocks Tester changes all of this while you test.
    /// </para>
    /// </summary>
    public static class Achievements
    {
        const string Key = "dp.achievements.v1";
        /// <summary>Unlocking can chain (an achievement unlocks a streamer, which counts toward another achievement...).</summary>
        const int MaxPasses = 16;

        [Serializable]
        sealed class StatEntry
        {
            public string stat;
            public int value;
        }

        [Serializable]
        sealed class UnlockEntry
        {
            public string id;
            /// <summary>When it unlocked (UTC ticks).</summary>
            public long ticks;
        }

        [Serializable]
        sealed class Data
        {
            public List<StatEntry> stats = new List<StatEntry>();
            public List<UnlockEntry> unlocked = new List<UnlockEntry>();
            /// <summary>Streamers whose unlock condition has been met (StreamerType names): counted once each.</summary>
            public List<string> streamersUnlocked = new List<string>();
            /// <summary>Emotes (by index) whose unlock condition has been met: counted once each.</summary>
            public List<int> emotesUnlocked = new List<int>();
        }

        static Data s_Data;
        static bool s_Evaluating;
        static bool s_Dirty;

        /// <summary>A new achievement unlocked while playing (not raised for ones brought in from a store).</summary>
        public static event Action<AchievementDefinition> Unlocked;

        /// <summary>Anything changed: counters, unlocks, streamers or emotes (the tester redraws).</summary>
        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_Data = null;
            s_Evaluating = false;
            s_Dirty = false;
            Unlocked = null;
            Changed = null;
        }

        static Data Store
        {
            get
            {
                if (s_Data != null) return s_Data;
                try
                {
                    string json = PlayerPrefs.GetString(Key, string.Empty);
                    s_Data = string.IsNullOrEmpty(json) ? new Data() : JsonUtility.FromJson<Data>(json);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[DeckParty] Achievement progress couldn't be read ({e.Message}); starting fresh.");
                    s_Data = null;
                }
                if (s_Data == null) s_Data = new Data();
                if (s_Data.stats == null) s_Data.stats = new List<StatEntry>();
                if (s_Data.unlocked == null) s_Data.unlocked = new List<UnlockEntry>();
                if (s_Data.streamersUnlocked == null) s_Data.streamersUnlocked = new List<string>();
                if (s_Data.emotesUnlocked == null) s_Data.emotesUnlocked = new List<int>();
                return s_Data;
            }
        }

        static void Save()
        {
            try
            {
                PlayerPrefs.SetString(Key, JsonUtility.ToJson(Store));
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DeckParty] Achievement progress couldn't be saved: {e.Message}");
            }
        }

        /// <summary>Forgets the cached progress so it's read again (the tester changed PlayerPrefs outside Play Mode).</summary>
        public static void Reload()
        {
            s_Data = null;
            RaiseChanged();
        }

        // ================================================================== queries

        public static IReadOnlyList<AchievementDefinition> All => AchievementLibrary.Current.achievements;

        public static AchievementDefinition Find(string id) => AchievementLibrary.Current.Find(id);

        public static bool IsUnlocked(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            foreach (var entry in Store.unlocked)
                if (entry.id == id) return true;
            return false;
        }

        /// <summary>When an achievement unlocked (local time), or null.</summary>
        public static DateTime? UnlockTime(string id)
        {
            foreach (var entry in Store.unlocked)
                if (entry.id == id) return entry.ticks > 0 ? new DateTime(entry.ticks, DateTimeKind.Utc).ToLocalTime() : (DateTime?)null;
            return null;
        }

        public static int UnlockedCount
        {
            get
            {
                int count = 0;
                foreach (var definition in All)
                    if (definition != null && IsUnlocked(definition.id)) count++;
                return count;
            }
        }

        public static int GetStat(AchievementStat stat)
        {
            string name = stat.ToString();
            foreach (var entry in Store.stats)
                if (entry.stat == name) return entry.value;
            return 0;
        }

        /// <summary>How far along an achievement is (0 to 1).</summary>
        public static float Progress(AchievementDefinition definition)
        {
            if (definition == null) return 0f;
            if (IsUnlocked(definition.id)) return 1f;
            if (definition.stat == AchievementStat.None) return 0f;
            return Mathf.Clamp01(GetStat(definition.stat) / (float)Mathf.Max(1, definition.target));
        }

        /// <summary>The achievement's name in the current language (the CSV row, else the library's fallback).</summary>
        public static string Name(AchievementDefinition definition)
        {
            if (definition == null) return string.Empty;
            string key = $"achievement.{definition.id}.name";
            return Loc.Has(key) ? Loc.Get(key) : definition.fallbackName;
        }

        /// <summary>The achievement's description; secret ones say so until they're unlocked.</summary>
        public static string Description(AchievementDefinition definition, bool revealHidden = false)
        {
            if (definition == null) return string.Empty;
            if (definition.hidden && !revealHidden && !IsUnlocked(definition.id)) return Loc.Get("achievement.hidden");
            string key = $"achievement.{definition.id}.desc";
            return Loc.Has(key) ? Loc.Get(key) : definition.fallbackDescription;
        }

        /// <summary>"Unlock: <name> (<what to do>)" for locked streamers and emotes.</summary>
        public static string RequirementText(string id)
        {
            var definition = Find(id);
            if (definition == null) return string.Empty;
            return Loc.Get("unlock.requirement", "name", Name(definition), "desc", Description(definition, true));
        }

        // ================================================================== counting

        /// <summary>A game reached its end screen (call once per game).</summary>
        public static void ReportGameFinished(bool multiplayer, bool endless)
        {
            AddStatQuietly(multiplayer ? AchievementStat.MultiplayerGamesPlayed : AchievementStat.SinglePlayerGamesPlayed, 1);
            if (endless) AddStatQuietly(AchievementStat.EndlessGamesPlayed, 1);
            Evaluate();
        }

        public static void AddStat(AchievementStat stat, int amount = 1)
        {
            AddStatQuietly(stat, amount);
            Evaluate();
        }

        /// <summary>Sets a counter (the tester; counting normally only goes up).</summary>
        public static void SetStat(AchievementStat stat, int value)
        {
            if (stat == AchievementStat.None) return;
            Entry(stat).value = Math.Max(0, value);
            Evaluate();
        }

        static void AddStatQuietly(AchievementStat stat, int amount)
        {
            if (stat == AchievementStat.None || amount == 0) return;
            var entry = Entry(stat);
            entry.value = Math.Max(0, entry.value + amount);
            s_Dirty = true;
        }

        static StatEntry Entry(AchievementStat stat)
        {
            string name = stat.ToString();
            foreach (var entry in Store.stats)
                if (entry.stat == name) return entry;
            var created = new StatEntry { stat = name };
            Store.stats.Add(created);
            return created;
        }

        // ================================================================== unlocking

        /// <summary>Unlocks an achievement now (the tester, or code with its own condition). Returns false if it already was.</summary>
        public static bool Unlock(string id)
        {
            var definition = Find(id);
            if (definition == null || IsUnlocked(id)) return false;
            bool changed = UnlockInternal(definition, true);
            Evaluate();
            return changed;
        }

        /// <summary>
        /// Locks an achievement again on this PC (the tester; the stores keep theirs, see <see cref="PlatformAchievementSync"/>).
        /// With <paramref name="rollBackCounter"/> its counter drops to one below the target, so it doesn't unlock again at once.
        /// Streamers and emotes it unlocked lock again too (and count again when they're re-earned).
        /// </summary>
        public static void Lock(string id, bool rollBackCounter = true)
        {
            var definition = Find(id);
            bool removed = Store.unlocked.RemoveAll(e => e.id == id) > 0;
            if (rollBackCounter && definition != null && definition.stat != AchievementStat.None)
            {
                var entry = Entry(definition.stat);
                if (entry.value >= definition.target) entry.value = Math.Max(0, definition.target - 1);
            }
            if (!removed && !rollBackCounter) return;
            Unlocks.ForgetCountsFor(id);
            Save();
            RaiseChanged();
        }

        /// <summary>Clears every counter, achievement and unlock count on this PC (the tester).</summary>
        public static void ResetAll()
        {
            s_Data = new Data();
            Save();
            RaiseChanged();
        }

        /// <summary>
        /// Brings in achievements a store already has (by our ids) without toasts and without sending them back. Returns how
        /// many were new here.
        /// </summary>
        internal static int Import(IEnumerable<string> ids)
        {
            int added = 0;
            foreach (var id in ids)
            {
                var definition = Find(id);
                if (definition == null || IsUnlocked(id)) continue;
                UnlockInternal(definition, false);
                added++;
            }
            if (added > 0) Evaluate();
            return added;
        }

        static bool UnlockInternal(AchievementDefinition definition, bool notify)
        {
            if (definition == null || IsUnlocked(definition.id)) return false;
            Store.unlocked.Add(new UnlockEntry { id = definition.id, ticks = DateTime.UtcNow.Ticks });
            s_Dirty = true;
            Debug.Log($"[DeckParty] Achievement unlocked: {definition.id}{(notify ? string.Empty : " (from the store)")}.");
            if (!notify) return true;
            PlatformAchievementSync.Push(definition);
            try
            {
                Unlocked?.Invoke(definition);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            return true;
        }

        /// <summary>
        /// Unlocks every achievement whose counter has reached its target, then lets <see cref="Unlocks"/> count newly unlocked
        /// streamers and emotes, which may unlock more achievements: repeated until nothing changes.
        /// </summary>
        public static void Evaluate()
        {
            if (s_Evaluating)
            {
                s_Dirty = true;
                return;
            }
            s_Evaluating = true;
            bool anything = s_Dirty;
            try
            {
                for (int pass = 0; pass < MaxPasses; pass++)
                {
                    s_Dirty = false;
                    foreach (var definition in All)
                    {
                        if (definition == null || definition.stat == AchievementStat.None || IsUnlocked(definition.id)) continue;
                        if (GetStat(definition.stat) >= Mathf.Max(1, definition.target)) UnlockInternal(definition, true);
                    }
                    Unlocks.Refresh();
                    if (!s_Dirty) break;
                    anything = true;
                }
            }
            finally
            {
                s_Evaluating = false;
            }
            if (anything) Save();
            RaiseChanged();
        }

        // ================================================================== streamer and emote counts (for Unlocks)

        internal static bool StreamerCounted(string name) => Store.streamersUnlocked.Contains(name);

        internal static void CountStreamer(string name)
        {
            if (Store.streamersUnlocked.Contains(name)) return;
            Store.streamersUnlocked.Add(name);
            AddStatQuietly(AchievementStat.StreamersUnlocked, 1);
        }

        internal static bool EmoteCounted(int index) => Store.emotesUnlocked.Contains(index);

        internal static void CountEmote(int index)
        {
            if (Store.emotesUnlocked.Contains(index)) return;
            Store.emotesUnlocked.Add(index);
            AddStatQuietly(AchievementStat.EmotesUnlocked, 1);
        }

        /// <summary>The tester locked a streamer or emote again: it may count again when its condition is met.</summary>
        internal static void UncountStreamer(string name) => Store.streamersUnlocked.Remove(name);

        internal static void UncountEmote(int index) => Store.emotesUnlocked.Remove(index);

        internal static void SaveNow() => Save();

        static void RaiseChanged()
        {
            try
            {
                Changed?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
