using System;
using System.Collections.Generic;
using DeckParty.Core;
using UnityEngine;

namespace DeckParty
{
    /// <summary>Long-term single-player progress: cleared character sets and the Random Mix streak (Cherry goal).</summary>
    public static class ProgressStore
    {
        public const int CherryStreak = 5;
        // v2: the character sets were replaced, so progress from the old scenarios doesn't carry over.
        const string Key = "dp.progress.v2";

        [Serializable]
        sealed class ProgressData
        {
            public List<int> cleared = new List<int>();
            public int randomStreak;
            public int bestRandomStreak;
            public bool cherry;
            public int totalWins;
            public int totalSeasons;
        }

        static ProgressData s_Data;

        static ProgressData Data
        {
            get
            {
                if (s_Data != null) return s_Data;
                try
                {
                    var json = PlayerPrefs.GetString(Key, string.Empty);
                    s_Data = string.IsNullOrEmpty(json) ? new ProgressData() : JsonUtility.FromJson<ProgressData>(json);
                }
                catch (Exception)
                {
                    s_Data = null;
                }
                if (s_Data == null) s_Data = new ProgressData();
                return s_Data;
            }
        }

        public static bool IsCleared(int scenarioId) => Data.cleared.Contains(scenarioId);

        public static int PresetsCleared
        {
            get
            {
                int count = 0;
                foreach (var s in ScenarioCatalog.All)
                    if (s.IsPreset && IsCleared(s.Id)) count++;
                return count;
            }
        }

        public static int RandomStreak => Data.randomStreak;
        public static int BestRandomStreak => Data.bestRandomStreak;
        public static bool HasCherry => Data.cherry;
        public static int TotalWins => Data.totalWins;

        public static void RecordSeason(int scenarioId, bool won)
        {
            var data = Data;
            var scenario = ScenarioCatalog.Get(scenarioId);
            data.totalSeasons++;
            if (won)
            {
                data.totalWins++;
                if (!data.cleared.Contains(scenarioId)) data.cleared.Add(scenarioId);
            }

            if (scenario.IsRandom)
            {
                data.randomStreak = won ? data.randomStreak + 1 : 0;
                data.bestRandomStreak = Mathf.Max(data.bestRandomStreak, data.randomStreak);
                if (data.randomStreak >= CherryStreak) data.cherry = true;
            }

            Persist();
        }

        public static void ResetAll()
        {
            s_Data = new ProgressData();
            Persist();
        }

        static void Persist()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }
    }
}
