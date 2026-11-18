using System;
using DeckParty.Platforms;
using UnityEngine;

namespace DeckParty
{
    /// <summary>
    /// What the game counts for achievements. An achievement unlocks when its stat reaches its target. Values are saved
    /// by name in the player's progress, so only ever append (and never rename) entries.
    /// </summary>
    public enum AchievementStat
    {
        None = 0,
        /// <summary>Single-player games finished (puzzles, Endless, Versus the AI): the season reached its end screen.</summary>
        SinglePlayerGamesPlayed = 1,
        /// <summary>Multiplayer games finished (online or couch): your season reached its end screen.</summary>
        MultiplayerGamesPlayed = 2,
        /// <summary>Endless runs finished (solo Endless or co-op Endless).</summary>
        EndlessGamesPlayed = 3,
        /// <summary>Streamers unlocked (a locked streamer's unlock condition was met).</summary>
        StreamersUnlocked = 4,
        /// <summary>Emotes unlocked (a locked emote's unlock condition was met).</summary>
        EmotesUnlocked = 5,
    }

    /// <summary>
    /// One achievement: the game's own record (it works with no store at all), and the ids it has on each store so the
    /// unlock is passed on to Steam, Epic and later platforms. Names and descriptions come from the strings CSV
    /// (achievement.&lt;id&gt;.name / .desc); the fallbacks here are used until those rows exist.
    /// </summary>
    [Serializable]
    public sealed class AchievementDefinition
    {
        [Tooltip("Stable id (lower case, underscores). Saved in players' progress and used by unlock conditions: never change it once players have it.")]
        public string id = "new_achievement";

        [Tooltip("What's counted.")]
        public AchievementStat stat = AchievementStat.SinglePlayerGamesPlayed;

        [Tooltip("The count that unlocks it (1 = the first time).")]
        [Min(1)] public int target = 1;

        [Tooltip("Shown in the game until the strings CSV has achievement.<id>.name.")]
        public string fallbackName = "New Achievement";

        [Tooltip("Shown in the game until the strings CSV has achievement.<id>.desc.")]
        [TextArea(1, 3)] public string fallbackDescription = "";

        [Tooltip("Optional icon (the stores use their own, uploaded to their sites).")]
        public Sprite icon;

        [Tooltip("Hide the description until it's unlocked (a secret achievement).")]
        public bool hidden;

        [Tooltip("Its id on each store. Empty = the store doesn't have it (nothing is sent there).")]
        public PlatformAchievementIds platformIds = new PlatformAchievementIds();

        public AchievementDefinition Copy()
        {
            var copy = (AchievementDefinition)MemberwiseClone();
            copy.platformIds = platformIds != null ? platformIds.Copy() : new PlatformAchievementIds();
            return copy;
        }
    }

    /// <summary>
    /// An achievement's id on each store (its API name on Steam, its achievement id in the Epic Developer Portal...).
    /// Steam and Epic are wired up; the others are placeholders for when those platforms are added.
    /// </summary>
    [Serializable]
    public sealed class PlatformAchievementIds
    {
        [Tooltip("API Name in Steamworks (App Admin > Stats & Achievements).")]
        public string steam = "";
        [Tooltip("Achievement Id in the Epic Developer Portal (Game Services > Achievements).")]
        public string epic = "";

        [Header("Placeholders (platforms not built yet)")]
        public string xbox = "";
        public string playStation = "";
        public string nintendoSwitch = "";
        public string appleGameCenter = "";
        public string googlePlayGames = "";

        /// <summary>The id on this platform, or "" when it has none.</summary>
        public string For(OnlinePlatformKind kind)
        {
            switch (kind)
            {
                case OnlinePlatformKind.Steam: return steam ?? string.Empty;
                case OnlinePlatformKind.Epic: return epic ?? string.Empty;
                case OnlinePlatformKind.Xbox: return xbox ?? string.Empty;
                case OnlinePlatformKind.PlayStation: return playStation ?? string.Empty;
                case OnlinePlatformKind.NintendoSwitch: return nintendoSwitch ?? string.Empty;
                case OnlinePlatformKind.AppleGameCenter: return appleGameCenter ?? string.Empty;
                case OnlinePlatformKind.GooglePlayGames: return googlePlayGames ?? string.Empty;
                default: return string.Empty;
            }
        }

        public PlatformAchievementIds Copy() => (PlatformAchievementIds)MemberwiseClone();
    }

    /// <summary>
    /// Marks a string field as an achievement id: the Inspector shows a dropdown of the achievements (plus None).
    /// Used for unlock conditions (Game Database > Streamers, Emotes).
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class AchievementIdAttribute : PropertyAttribute
    {
        /// <summary>The label for "no condition".</summary>
        public readonly string noneLabel;

        public AchievementIdAttribute(string noneLabel = "None (unlocked from the start)") => this.noneLabel = noneLabel;
    }
}
