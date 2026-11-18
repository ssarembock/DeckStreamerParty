using System;
using System.Collections.Generic;
using DeckParty.Platforms;
using UnityEngine;

namespace DeckParty
{
    /// <summary>
    /// Passes the game's achievements on to the store it runs on (Steam, Epic; <see cref="IPlatformAchievements"/>).
    /// <list type="bullet">
    /// <item>A new unlock is sent straight away.</item>
    /// <item>When the platform signs in, the two sides are matched: achievements the store already has unlock here
    /// (silently), and ones unlocked here (for example offline, or before the store was set up) are sent to the store.</item>
    /// <item>Anything that couldn't be sent is tried again every half minute (<see cref="AchievementRunner"/>).</item>
    /// </list>
    /// Achievements with no id for the running platform stay local. The Default multiplayer has no store, so nothing is sent.
    /// </summary>
    public static class PlatformAchievementSync
    {
        static readonly HashSet<string> s_Pending = new HashSet<string>();
        static IOnlinePlatform s_SyncedWith;
        static bool s_Syncing;

        /// <summary>Last thing that happened, for the tester.</summary>
        public static string LastStatus { get; private set; } = "Not synced yet.";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_Pending.Clear();
            s_SyncedWith = null;
            s_Syncing = false;
            LastStatus = "Not synced yet.";
        }

        /// <summary>The running platform's achievements, or null (no platform, or it has none).</summary>
        public static IPlatformAchievements Target
        {
            get
            {
                var platform = OnlinePlatforms.Current;
                return platform is IPlatformAchievements achievements && achievements.AchievementsReady ? achievements : null;
            }
        }

        public static OnlinePlatformKind TargetKind => Target != null ? OnlinePlatforms.Current.Kind : OnlinePlatformKind.Default;

        /// <summary>Achievements waiting to be sent to the store.</summary>
        public static int PendingCount => s_Pending.Count;

        /// <summary>A new unlock: send it now, or keep it for later.</summary>
        internal static void Push(AchievementDefinition definition)
        {
            if (definition == null) return;
            s_Pending.Add(definition.id);
            Flush();
        }

        /// <summary>Sends what's waiting (called again by the runner every half minute while anything is).</summary>
        public static void Flush()
        {
            var target = Target;
            if (target == null || s_Pending.Count == 0) return;
            var kind = OnlinePlatforms.Current.Kind;
            foreach (var id in new List<string>(s_Pending))
            {
                var definition = Achievements.Find(id);
                string platformId = definition != null && definition.platformIds != null ? definition.platformIds.For(kind) : string.Empty;
                if (string.IsNullOrEmpty(platformId))
                {
                    // Not on this store: nothing to send.
                    s_Pending.Remove(id);
                    continue;
                }
                target.UnlockAchievement(platformId, ok =>
                {
                    if (ok)
                    {
                        s_Pending.Remove(id);
                        LastStatus = $"Sent {id} to {OnlinePlatforms.DisplayName(kind)} ({platformId}).";
                    }
                    else
                    {
                        LastStatus = $"{OnlinePlatforms.DisplayName(kind)} didn't take {id} ({platformId}); trying again later.";
                    }
                });
            }
        }

        /// <summary>
        /// Matches the store and this PC: the store's unlocks come in, ours go out. Runs when the platform signs in (and from
        /// the tester's Sync button).
        /// </summary>
        public static void Sync()
        {
            var target = Target;
            if (target == null)
            {
                LastStatus = "No store with achievements is running (Default multiplayer, or the platform isn't signed in).";
                return;
            }
            if (s_Syncing) return;
            s_Syncing = true;
            var platform = OnlinePlatforms.Current;
            var kind = platform.Kind;

            // Our ids by their id on this store.
            var byPlatformId = new Dictionary<string, string>();
            foreach (var definition in Achievements.All)
            {
                if (definition == null || definition.platformIds == null) continue;
                string platformId = definition.platformIds.For(kind);
                if (!string.IsNullOrEmpty(platformId)) byPlatformId[platformId] = definition.id;
            }

            target.QueryUnlockedAchievements(new List<string>(byPlatformId.Keys), unlocked =>
            {
                s_Syncing = false;
                s_SyncedWith = platform;
                int imported = 0;
                if (unlocked != null)
                {
                    var ours = new List<string>();
                    foreach (var platformId in unlocked)
                        if (byPlatformId.TryGetValue(platformId, out var id)) ours.Add(id);
                    imported = Achievements.Import(ours);
                }

                // Everything unlocked here that the store didn't list goes out.
                foreach (var definition in Achievements.All)
                {
                    if (definition == null || !Achievements.IsUnlocked(definition.id)) continue;
                    string platformId = definition.platformIds != null ? definition.platformIds.For(kind) : string.Empty;
                    if (string.IsNullOrEmpty(platformId) || (unlocked != null && unlocked.Contains(platformId))) continue;
                    s_Pending.Add(definition.id);
                }
                LastStatus = unlocked == null
                    ? $"{OnlinePlatforms.DisplayName(kind)} couldn't list its achievements yet; sending ours ({s_Pending.Count})."
                    : $"Synced with {OnlinePlatforms.DisplayName(kind)}: {imported} brought in, {s_Pending.Count} to send.";
                Debug.Log($"[DeckParty] Achievements: {LastStatus}");
                Flush();
            });
        }

        /// <summary>The platform changed (signed in, out, or another one): sync once it's ready.</summary>
        internal static void OnPlatformStatusChanged()
        {
            var platform = OnlinePlatforms.Current;
            if (platform == null)
            {
                s_SyncedWith = null;
                return;
            }
            if (platform != s_SyncedWith) Sync();
        }

        /// <summary>
        /// Tester: locks achievements on the store again so they can be earned again (Steam allows it from the game; Epic
        /// doesn't, use the Developer Portal's tools there).
        /// </summary>
        public static void ResetOnPlatform(IEnumerable<string> ids, Action<string> report)
        {
            var target = Target;
            if (target == null)
            {
                report?.Invoke("No store with achievements is running.");
                return;
            }
            var kind = OnlinePlatforms.Current.Kind;
            foreach (var id in ids)
            {
                var definition = Achievements.Find(id);
                string platformId = definition != null && definition.platformIds != null ? definition.platformIds.For(kind) : string.Empty;
                if (string.IsNullOrEmpty(platformId)) continue;
                s_Pending.Remove(id);
                target.ResetAchievement(platformId, ok =>
                    report?.Invoke(ok ? $"Locked {platformId} on {OnlinePlatforms.DisplayName(kind)}." : $"{OnlinePlatforms.DisplayName(kind)} can't lock {platformId} from the game."));
            }
        }
    }
}
