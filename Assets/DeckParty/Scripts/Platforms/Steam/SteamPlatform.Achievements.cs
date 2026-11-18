// Steam code only exists when Steamworks.NET is installed (DECKPARTY_STEAMWORKS, a version define in DeckParty.Steam.asmdef)
// and the build target is Windows, macOS or Linux. Otherwise this file compiles to nothing and the game uses the Default multiplayer.
#if DECKPARTY_STEAMWORKS && UNITY_STANDALONE
using System;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;

namespace DeckParty.Platforms.Steam
{
    /// <summary>
    /// Steam achievements (ISteamUserStats). The game's own achievements are passed on by their Steam API names
    /// (AchievementDefinition.platformIds.steam), set up in Steamworks under App Admin > Stats & Achievements. Steam loads
    /// the player's stats when it starts; until it has, reads report nothing and unlocks are retried later.
    /// </summary>
    public sealed partial class SteamPlatform : IPlatformAchievements
    {
        public bool AchievementsReady => IsReady;

        public void UnlockAchievement(string platformId, Action<bool> done)
        {
            bool ok = false;
            if (IsReady && !string.IsNullOrEmpty(platformId))
            {
                // Setting an achievement that's already set is fine; StoreStats sends it (and shows Steam's pop-up).
                ok = SteamUserStats.SetAchievement(platformId) && SteamUserStats.StoreStats();
                if (!ok) Debug.LogWarning($"[DeckParty] Steam didn't take achievement {platformId} (unknown API name, or stats not loaded yet).");
            }
            done?.Invoke(ok);
        }

        public void ResetAchievement(string platformId, Action<bool> done)
        {
            bool ok = IsReady && !string.IsNullOrEmpty(platformId) && SteamUserStats.ClearAchievement(platformId) && SteamUserStats.StoreStats();
            done?.Invoke(ok);
        }

        public void QueryUnlockedAchievements(IReadOnlyList<string> platformIds, Action<HashSet<string>> done)
        {
            if (!IsReady)
            {
                done?.Invoke(null);
                return;
            }
            var unlocked = new HashSet<string>();
            int answered = 0;
            foreach (var id in platformIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                // False when Steam doesn't know the name, or hasn't loaded the stats yet.
                if (!SteamUserStats.GetAchievement(id, out bool achieved)) continue;
                answered++;
                if (achieved) unlocked.Add(id);
            }
            done?.Invoke(answered > 0 || platformIds.Count == 0 ? unlocked : null);
        }
    }
}
#endif
