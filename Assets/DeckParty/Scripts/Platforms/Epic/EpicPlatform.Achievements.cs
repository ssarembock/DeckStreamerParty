// Epic code only exists when the EOS Plugin for Unity is installed (DECKPARTY_EOS, a version define in DeckParty.Epic.asmdef)
// and the build target is Windows, macOS or Linux. Otherwise this file compiles to nothing and the game uses the Default multiplayer.
#if DECKPARTY_EOS && UNITY_STANDALONE
using System;
using System.Collections.Generic;
using Epic.OnlineServices;
using Epic.OnlineServices.Achievements;
using UnityEngine;

namespace DeckParty.Platforms.Epic
{
    /// <summary>
    /// Epic achievements (EOS Achievements, on the product user id, so device-id players have them too). The game's own
    /// achievements are passed on by their Epic ids (AchievementDefinition.platformIds.epic), set up in the Epic Developer
    /// Portal under Game Services > Achievements. EOS can't lock an achievement again from the game; reset players in
    /// the Developer Portal when testing.
    /// </summary>
    public sealed partial class EpicPlatform : IPlatformAchievements
    {
        static AchievementsInterface AchievementsSdk => Sdk(m => m.GetEOSAchievementInterface());

        public bool AchievementsReady => IsReady && AchievementsSdk != null;

        public void UnlockAchievement(string platformId, Action<bool> done)
        {
            var achievements = AchievementsSdk;
            if (!IsReady || achievements == null || string.IsNullOrEmpty(platformId))
            {
                done?.Invoke(false);
                return;
            }
            var options = new UnlockAchievementsOptions { UserId = m_LocalProductUser, AchievementIds = new Utf8String[] { platformId } };
            achievements.UnlockAchievements(ref options, null, (ref OnUnlockAchievementsCompleteCallbackInfo info) =>
            {
                if (info.ResultCode != Result.Success) Debug.LogWarning($"[DeckParty] Epic didn't take achievement {platformId} ({info.ResultCode}).");
                done?.Invoke(info.ResultCode == Result.Success);
            });
        }

        public void ResetAchievement(string platformId, Action<bool> done) => done?.Invoke(false);

        public void QueryUnlockedAchievements(IReadOnlyList<string> platformIds, Action<HashSet<string>> done)
        {
            var achievements = AchievementsSdk;
            if (!IsReady || achievements == null)
            {
                done?.Invoke(null);
                return;
            }
            var query = new QueryPlayerAchievementsOptions { TargetUserId = m_LocalProductUser, LocalUserId = m_LocalProductUser };
            achievements.QueryPlayerAchievements(ref query, null, (ref OnQueryPlayerAchievementsCompleteCallbackInfo info) =>
            {
                if (info.ResultCode != Result.Success || AchievementsSdk == null)
                {
                    Debug.LogWarning($"[DeckParty] Epic couldn't list the player's achievements ({info.ResultCode}).");
                    done?.Invoke(null);
                    return;
                }
                var unlocked = new HashSet<string>();
                var countOptions = new GetPlayerAchievementCountOptions { UserId = m_LocalProductUser };
                uint count = AchievementsSdk.GetPlayerAchievementCount(ref countOptions);
                for (uint i = 0; i < count; i++)
                {
                    var copy = new CopyPlayerAchievementByIndexOptions { TargetUserId = m_LocalProductUser, AchievementIndex = i, LocalUserId = m_LocalProductUser };
                    if (AchievementsSdk.CopyPlayerAchievementByIndex(ref copy, out PlayerAchievement? achievement) != Result.Success || !achievement.HasValue) continue;
                    string id = achievement.Value.AchievementId;
                    if (!string.IsNullOrEmpty(id) && achievement.Value.UnlockTime.HasValue) unlocked.Add(id);
                }
                done?.Invoke(unlocked);
            });
        }
    }
}
#endif
