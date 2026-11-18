using System.Collections.Generic;
using UnityEngine;

namespace DeckParty
{
    /// <summary>
    /// Every achievement in the game (Resources/AchievementLibrary.asset; Deck Party > Achievements and Unlocks Tester has a
    /// button to select it). Add more with + in the Inspector: give each a stable id, the stat it counts and its target,
    /// and its id on each store. The starters below are put back if they go missing (their settings are kept once they
    /// exist).
    /// </summary>
    public sealed class AchievementLibrary : ScriptableObject
    {
        public const string ResourceName = "AchievementLibrary";

        // The starter achievements' ids (unlock conditions refer to these).
        public const string FirstSinglePlayer = "first_single_player";
        public const string FirstMultiplayer = "first_multiplayer";
        public const string FirstEndless = "first_endless";
        public const string FirstStreamerUnlock = "first_streamer_unlock";
        public const string FirstEmoteUnlock = "first_emote_unlock";

        [Tooltip("The achievements, in the order the tester and any achievements list show them.")]
        public List<AchievementDefinition> achievements = new List<AchievementDefinition>();

        static AchievementLibrary s_Current;
        static AchievementLibrary s_BuiltInOnly;

        /// <summary>The library asset, or the built-in starters while it doesn't exist (Build Everything creates it).</summary>
        public static AchievementLibrary Current
        {
            get
            {
                if (s_Current != null) return s_Current;
                s_Current = Resources.Load<AchievementLibrary>(ResourceName);
                if (s_Current != null) return s_Current;
                if (s_BuiltInOnly == null)
                {
                    s_BuiltInOnly = CreateInstance<AchievementLibrary>();
                    s_BuiltInOnly.hideFlags = HideFlags.DontSave;
                    s_BuiltInOnly.EnsureStarters();
                }
                return s_BuiltInOnly;
            }
        }

        /// <summary>Forgets the cached asset (the editor calls this after creating it).</summary>
        public static void ClearCache()
        {
            s_Current = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_Current = null;

        public AchievementDefinition Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var definition in achievements)
                if (definition != null && definition.id == id) return definition;
            return null;
        }

        /// <summary>The starter achievements (the ones in the first brief), with their store ids.</summary>
        public static List<AchievementDefinition> Starters() => new List<AchievementDefinition>
        {
            Starter(FirstSinglePlayer, AchievementStat.SinglePlayerGamesPlayed, "Going Live", "Finish your first single-player game.", "FIRST_SINGLE_PLAYER"),
            Starter(FirstMultiplayer, AchievementStat.MultiplayerGamesPlayed, "Collab Stream", "Finish your first multiplayer game.", "FIRST_MULTIPLAYER"),
            Starter(FirstEndless, AchievementStat.EndlessGamesPlayed, "24/7 Stream", "Finish your first Endless run.", "FIRST_ENDLESS"),
            Starter(FirstStreamerUnlock, AchievementStat.StreamersUnlocked, "New Talent", "Unlock your first streamer.", "FIRST_STREAMER_UNLOCK"),
            Starter(FirstEmoteUnlock, AchievementStat.EmotesUnlocked, "Emote-Only Mode", "Unlock your first emote.", "FIRST_EMOTE_UNLOCK"),
        };

        static AchievementDefinition Starter(string id, AchievementStat stat, string name, string description, string storeId) => new AchievementDefinition
        {
            id = id,
            stat = stat,
            target = 1,
            fallbackName = name,
            fallbackDescription = description,
            platformIds = new PlatformAchievementIds { steam = storeId, epic = storeId },
        };

        /// <summary>Adds any starter that's missing (by id). Returns true when something was added.</summary>
        public bool EnsureStarters()
        {
            if (achievements == null) achievements = new List<AchievementDefinition>();
            achievements.RemoveAll(a => a == null);
            bool changed = false;
            var starters = Starters();
            for (int i = 0; i < starters.Count; i++)
            {
                if (Find(starters[i].id) != null) continue;
                achievements.Insert(Mathf.Min(i, achievements.Count), starters[i]);
                changed = true;
            }
            return changed;
        }
    }
}
