using System;
using System.Collections.Generic;
using DeckParty.Core;
using DeckParty.Networking;
using UnityEngine;

namespace DeckParty
{
    /// <summary>
    /// Locked streamers and emotes. Each has an unlock condition in the Game Database: an achievement id, or None (it's
    /// there from the start). A locked one can't be picked or sent until its achievement unlocks. The first time a condition
    /// is met the streamer or emote counts toward <see cref="AchievementStat.StreamersUnlocked"/> /
    /// <see cref="AchievementStat.EmotesUnlocked"/>, and a toast says so.
    /// <para>
    /// The tester (Deck Party > Achievements and Unlocks Tester) can force any streamer or emote unlocked or locked, or unlock
    /// everything. Those overrides only work in the Editor and development builds, never in a release build.
    /// </para>
    /// </summary>
    public static class Unlocks
    {
        const string OverridesKey = "dp.debug.unlocks";

        public enum Override
        {
            /// <summary>Follow the unlock condition.</summary>
            Default = 0,
            ForceUnlocked = 1,
            ForceLocked = 2,
        }

        [Serializable]
        sealed class Overrides
        {
            public bool unlockEverything;
            public List<string> streamersUnlocked = new List<string>();
            public List<string> streamersLocked = new List<string>();
            public List<int> emotesUnlocked = new List<int>();
            public List<int> emotesLocked = new List<int>();
        }

        static Overrides s_Overrides;

        /// <summary>A streamer's condition was met for the first time (not raised for tester overrides).</summary>
        public static event Action<StreamerType> StreamerUnlocked;

        /// <summary>An emote's condition was met for the first time.</summary>
        public static event Action<int> EmoteUnlocked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_Overrides = null;
            StreamerUnlocked = null;
            EmoteUnlocked = null;
        }

        // ================================================================== conditions

        /// <summary>
        /// The condition a streamer has before the Game Database is set up (Build Everything copies it in once; after that the
        /// Database rules). The two streamers added on 2026-11-18 start locked.
        /// </summary>
        public static string DefaultStreamerCondition(StreamerType type)
        {
            switch (type)
            {
                case StreamerType.KaiSonnet: return AchievementLibrary.FirstEndless;
                case StreamerType.TrainWrex: return AchievementLibrary.FirstSinglePlayer;
                default: return string.Empty;
            }
        }

        /// <summary>The achievement a streamer needs ("" = unlocked from the start).</summary>
        public static string StreamerCondition(StreamerType type)
        {
            if (type == StreamerType.None) return string.Empty;
            var art = GameDatabase.Instance.GetStreamerArt(type);
            return art != null && art.unlockSeeded ? art.unlockAchievement ?? string.Empty : DefaultStreamerCondition(type);
        }

        /// <summary>The achievement an emote needs ("" = unlocked from the start).</summary>
        public static string EmoteCondition(int index)
        {
            var list = GameDatabase.Instance.emoteUnlocks;
            return list != null && index >= 0 && index < list.Count ? list[index] ?? string.Empty : string.Empty;
        }

        /// <summary>
        /// Whether a condition is met. A condition naming an achievement that no longer exists counts as met, so a deleted
        /// achievement never locks anything for good (the tester flags it).
        /// </summary>
        public static bool ConditionMet(string achievementId) =>
            string.IsNullOrEmpty(achievementId) || Achievements.Find(achievementId) == null || Achievements.IsUnlocked(achievementId);

        // ================================================================== streamers

        /// <summary>Whether this player can pick the streamer (No Streamer and Random always).</summary>
        public static bool IsStreamerUnlocked(StreamerType type)
        {
            if (type == StreamerType.None || !StreamerCatalog.IsValid(type)) return true;
            switch (StreamerOverride(type))
            {
                case Override.ForceUnlocked: return true;
                case Override.ForceLocked: return false;
            }
            return ConditionMet(StreamerCondition(type));
        }

        /// <summary>The streamers this player has unlocked, one bit each (sent to the host so Random only lands on those).</summary>
        public static uint StreamerMask()
        {
            uint mask = 0;
            foreach (var type in StreamerCatalog.Roster)
                if (IsStreamerUnlocked(type)) mask |= StreamerCatalog.MaskOf(type);
            return mask;
        }

        public static List<StreamerType> UnlockedRoster()
        {
            var list = new List<StreamerType>();
            foreach (var type in StreamerCatalog.Roster)
                if (IsStreamerUnlocked(type)) list.Add(type);
            return list;
        }

        // ================================================================== emotes

        public static bool IsEmoteUnlocked(int index)
        {
            switch (EmoteOverride(index))
            {
                case Override.ForceUnlocked: return true;
                case Override.ForceLocked: return false;
            }
            return ConditionMet(EmoteCondition(index));
        }

        // ================================================================== counting newly unlocked ones

        /// <summary>
        /// Counts streamers and emotes whose condition has just been met (once each, ever) and says so. Achievements calls
        /// this whenever something unlocks; tester overrides don't count.
        /// </summary>
        public static void Refresh()
        {
            foreach (var type in StreamerCatalog.Roster)
            {
                string condition = StreamerCondition(type);
                if (string.IsNullOrEmpty(condition) || !Achievements.IsUnlocked(condition)) continue;
                string name = type.ToString();
                if (Achievements.StreamerCounted(name)) continue;
                Achievements.CountStreamer(name);
                Raise(StreamerUnlocked, type);
            }

            int emotes = Emotes.Count;
            for (int i = 0; i < emotes; i++)
            {
                string condition = EmoteCondition(i);
                if (string.IsNullOrEmpty(condition) || !Achievements.IsUnlocked(condition) || Achievements.EmoteCounted(i)) continue;
                Achievements.CountEmote(i);
                Raise(EmoteUnlocked, i);
            }
        }

        /// <summary>An achievement was locked again (tester): what it unlocked may count again later.</summary>
        internal static void ForgetCountsFor(string achievementId)
        {
            if (string.IsNullOrEmpty(achievementId)) return;
            foreach (var type in StreamerCatalog.Roster)
                if (StreamerCondition(type) == achievementId) Achievements.UncountStreamer(type.ToString());
            int emotes = Emotes.Count;
            for (int i = 0; i < emotes; i++)
                if (EmoteCondition(i) == achievementId) Achievements.UncountEmote(i);
        }

        static void Raise<T>(Action<T> handler, T value)
        {
            try
            {
                handler?.Invoke(value);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        // ================================================================== tester overrides (Editor and development builds)

        /// <summary>Overrides only work where testing happens, never in a release build.</summary>
        public static bool OverridesAllowed => Application.isEditor || Debug.isDebugBuild;

        static Overrides Current
        {
            get
            {
                if (s_Overrides != null) return s_Overrides;
                try
                {
                    string json = PlayerPrefs.GetString(OverridesKey, string.Empty);
                    s_Overrides = string.IsNullOrEmpty(json) ? new Overrides() : JsonUtility.FromJson<Overrides>(json);
                }
                catch (Exception)
                {
                    s_Overrides = null;
                }
                return s_Overrides ?? (s_Overrides = new Overrides());
            }
        }

        static void SaveOverrides()
        {
            PlayerPrefs.SetString(OverridesKey, JsonUtility.ToJson(Current));
            PlayerPrefs.Save();
        }

        /// <summary>Re-reads the overrides (the tester changed them outside Play Mode).</summary>
        public static void ReloadOverrides() => s_Overrides = null;

        /// <summary>Tester: every streamer and emote unlocked, whatever their conditions.</summary>
        public static bool UnlockEverything
        {
            get => OverridesAllowed && Current.unlockEverything;
            set
            {
                Current.unlockEverything = value;
                SaveOverrides();
            }
        }

        public static Override StreamerOverride(StreamerType type)
        {
            if (!OverridesAllowed) return Override.Default;
            if (Current.unlockEverything) return Override.ForceUnlocked;
            string name = type.ToString();
            if (Current.streamersUnlocked.Contains(name)) return Override.ForceUnlocked;
            if (Current.streamersLocked.Contains(name)) return Override.ForceLocked;
            return Override.Default;
        }

        public static void SetStreamerOverride(StreamerType type, Override value)
        {
            string name = type.ToString();
            Current.streamersUnlocked.Remove(name);
            Current.streamersLocked.Remove(name);
            if (value == Override.ForceUnlocked) Current.streamersUnlocked.Add(name);
            else if (value == Override.ForceLocked) Current.streamersLocked.Add(name);
            SaveOverrides();
        }

        public static Override EmoteOverride(int index)
        {
            if (!OverridesAllowed) return Override.Default;
            if (Current.unlockEverything) return Override.ForceUnlocked;
            if (Current.emotesUnlocked.Contains(index)) return Override.ForceUnlocked;
            if (Current.emotesLocked.Contains(index)) return Override.ForceLocked;
            return Override.Default;
        }

        public static void SetEmoteOverride(int index, Override value)
        {
            Current.emotesUnlocked.Remove(index);
            Current.emotesLocked.Remove(index);
            if (value == Override.ForceUnlocked) Current.emotesUnlocked.Add(index);
            else if (value == Override.ForceLocked) Current.emotesLocked.Add(index);
            SaveOverrides();
        }

        /// <summary>Clears every tester override.</summary>
        public static void ClearOverrides()
        {
            s_Overrides = new Overrides();
            SaveOverrides();
        }
    }
}
