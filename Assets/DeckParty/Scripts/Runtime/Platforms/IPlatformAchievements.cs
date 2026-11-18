using System;
using System.Collections.Generic;

namespace DeckParty.Platforms
{
    /// <summary>
    /// A platform's achievements (Steam Stats and Achievements, Epic achievements). Optional: a platform that implements
    /// <see cref="IOnlinePlatform"/> and this gets the game's own achievements passed on by
    /// <see cref="PlatformAchievementSync"/>. Ids here are the platform's own (AchievementDefinition.platformIds).
    /// Callbacks run on the main thread.
    /// </summary>
    public interface IPlatformAchievements
    {
        /// <summary>The player is signed in and achievements can be read and written.</summary>
        bool AchievementsReady { get; }

        /// <summary>Unlocks one achievement on the platform (already unlocked is fine). <paramref name="done"/> gets whether it worked.</summary>
        void UnlockAchievement(string platformId, Action<bool> done);

        /// <summary>
        /// Locks one achievement on the platform again, for testing. Not every platform allows it (Epic doesn't from the
        /// game): then <paramref name="done"/> gets false.
        /// </summary>
        void ResetAchievement(string platformId, Action<bool> done);

        /// <summary>Which of these achievements the player already has on the platform (null when it couldn't tell).</summary>
        void QueryUnlockedAchievements(IReadOnlyList<string> platformIds, Action<HashSet<string>> done);
    }
}
