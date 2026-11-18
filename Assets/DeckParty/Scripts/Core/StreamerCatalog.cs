using System;
using System.Collections.Generic;

namespace DeckParty.Core
{
    /// <summary>The streamer a player plays as (chosen on the Select Your Streamer screen). Values are saved and sent over the network.</summary>
    public enum StreamerType : byte
    {
        None = 0,
        Fate = 1,
        BroJogan = 2,
        Shinobi = 3,
        IRunFast = 4,
        MrShark = 5,
        EthicalFboi = 6,
        SoftHardBodyMan = 7,
        StrategicAbomination = 8,
        Palmane = 9,
        ManimMakethMan = 10,
        RudeRadiologist = 11,

        // --- 2026-11-18: locked until their unlock achievement (Game Database > Streamers > Unlock Condition) ---
        /// <summary>The Subathon King (parody of a subathon streamer): Subathon saves the first Server Down.</summary>
        KaiSonnet = 12,
        /// <summary>The High Roller (parody of a gambling streamer): Double or Nothing.</summary>
        TrainWrex = 13,
    }

    public enum PowerKind : byte
    {
        None = 0,
        /// <summary>Always on; nothing to press.</summary>
        Passive = 1,
        /// <summary>Takes effect immediately when used.</summary>
        Instant = 2,
        /// <summary>Armed in the shop and applied when your next stream starts.</summary>
        NextStream = 3,
        /// <summary>Hits a rival (multiplayer only).</summary>
        Rival = 4,
    }

    /// <summary>
    /// Rule data for one streamer. Names and descriptions are localized on the Unity side (keys "streamer.&lt;Type&gt;.*").
    /// Actives are paid in Influence, and like raising the Sub Cap each use costs more (up to <see cref="StreamerCatalog.MaxPowerCost"/>).
    /// </summary>
    public sealed class StreamerInfo
    {
        public readonly StreamerType Type;
        public readonly PowerKind Kind;
        /// <summary>Influence the first use costs.</summary>
        public readonly int InfluenceCost;
        /// <summary>Added to the cost after every use.</summary>
        public readonly int InfluenceStep;
        /// <summary>Turns the power is unavailable after a use (0 = once per turn).</summary>
        public readonly int Cooldown;

        public StreamerInfo(StreamerType type, PowerKind kind, int influence = 0, int influenceStep = 0, int cooldown = 0)
        {
            Type = type;
            Kind = kind;
            InfluenceCost = influence;
            InfluenceStep = influenceStep;
            Cooldown = cooldown;
        }

        public bool IsActive => Kind == PowerKind.Instant || Kind == PowerKind.NextStream || Kind == PowerKind.Rival;
        public bool IsPassive => Kind == PowerKind.Passive;
        public bool NeedsRival => Kind == PowerKind.Rival;
    }

    /// <summary>The streamer roster and their powers. Every tunable number is here.</summary>
    public static class StreamerCatalog
    {
        /// <summary>The "Random" tile on the select screen. Resolved to a real streamer before the game starts.</summary>
        public const byte RandomPick = 255;

        // --- Power tuning ---
        public const int RaidHeat = 1;
        /// <summary>Raids alone can't push a solo stream past this much Deplatform Risk (a team's: <see cref="PartyEngine.RiskLimit"/> - 1).</summary>
        public const int MaxRaidHeat = GameRules.RaidTroubleThreshold - 1;
        /// <summary>An active never costs more than this per use (the same ceiling as raising the Sub Cap).</summary>
        public const int MaxPowerCost = GameRules.MaxExpansionCost;
        public const int SmokeBombShield = 1;
        public const int GiveawayBonus = 1;
        public const int WealthTaxAmount = 2;
        public const int PalSphereMaxCost = 6;
        public const int XRayCount = 3;
        /// <summary>Beautiful Sequence (Manim Maketh Man): Cash per great stream in a row, up to this much.</summary>
        public const int StreakMaxBonus = 5;
        /// <summary>Three-Hour Episode (Bro Jogan): extra Sub Cap for the next stream only.</summary>
        public const int EpisodeSeats = 1;
        /// <summary>Bark at Chat (IRunFast): extra Cash for each viewer carrying Risk in the next stream.</summary>
        public const int BarkBonus = 2;
        /// <summary>Double or Nothing (Train Wrex): a scored stream earns its Cash again, up to this much extra.</summary>
        public const int DoubleDownMaxBonus = 10;
        /// <summary>Double or Nothing: Influence lost when the stream fails (deplatformed or Server Down).</summary>
        public const int DoubleDownLoss = 2;

        static readonly StreamerInfo[] s_All =
        {
            new StreamerInfo(StreamerType.None, PowerKind.None),
            new StreamerInfo(StreamerType.Fate, PowerKind.Rival, influence: 1, influenceStep: 1, cooldown: 2),
            new StreamerInfo(StreamerType.BroJogan, PowerKind.NextStream, influence: 2, influenceStep: 1, cooldown: 2),
            new StreamerInfo(StreamerType.Shinobi, PowerKind.NextStream, influence: 2, influenceStep: 1, cooldown: 2),
            new StreamerInfo(StreamerType.IRunFast, PowerKind.NextStream, influence: 2, influenceStep: 1, cooldown: 2),
            new StreamerInfo(StreamerType.MrShark, PowerKind.NextStream, influence: 3, influenceStep: 1, cooldown: 2),
            new StreamerInfo(StreamerType.EthicalFboi, PowerKind.Rival, influence: 1, influenceStep: 1, cooldown: 2),
            new StreamerInfo(StreamerType.SoftHardBodyMan, PowerKind.NextStream, influence: 1, influenceStep: 1),
            new StreamerInfo(StreamerType.StrategicAbomination, PowerKind.NextStream, influence: 2, influenceStep: 1, cooldown: 2),
            new StreamerInfo(StreamerType.Palmane, PowerKind.Instant, influence: 2, influenceStep: 1, cooldown: 3),
            new StreamerInfo(StreamerType.ManimMakethMan, PowerKind.Instant, influence: 1, influenceStep: 1, cooldown: 2),
            new StreamerInfo(StreamerType.RudeRadiologist, PowerKind.NextStream, influence: 1, influenceStep: 1),
            new StreamerInfo(StreamerType.KaiSonnet, PowerKind.NextStream, influence: 2, influenceStep: 1, cooldown: 2),
            new StreamerInfo(StreamerType.TrainWrex, PowerKind.NextStream, influence: 1, influenceStep: 1, cooldown: 2),
        };

        /// <summary>Real streamers in select-screen order (None and Random are extra tiles).</summary>
        public static readonly StreamerType[] Roster =
        {
            StreamerType.Fate, StreamerType.BroJogan, StreamerType.Shinobi, StreamerType.IRunFast,
            StreamerType.MrShark, StreamerType.EthicalFboi, StreamerType.SoftHardBodyMan, StreamerType.StrategicAbomination,
            StreamerType.Palmane, StreamerType.ManimMakethMan, StreamerType.RudeRadiologist, StreamerType.KaiSonnet, StreamerType.TrainWrex,
        };

        public static IReadOnlyList<StreamerInfo> All => s_All;

        public static bool IsValid(StreamerType type) => (int)type >= 0 && (int)type < s_All.Length;

        public static StreamerInfo Get(StreamerType type) => IsValid(type) ? s_All[(int)type] : s_All[0];

        /// <summary>Turns a select-screen pick (which may be <see cref="RandomPick"/>) into a real streamer.</summary>
        public static StreamerType Resolve(byte pick, Random random) => Resolve(pick, random, AllUnlocked);

        /// <summary>
        /// Turns a pick into a real streamer for a player with these streamers unlocked (one bit per <see cref="StreamerType"/>,
        /// see <see cref="MaskOf"/>). Random only lands on unlocked streamers, and a locked pick becomes a random one.
        /// </summary>
        public static StreamerType Resolve(byte pick, Random random, uint unlockedMask)
        {
            if (unlockedMask == 0) unlockedMask = AllUnlocked;
            var type = (StreamerType)pick;
            if (pick != RandomPick && IsValid(type) && (type == StreamerType.None || InMask(type, unlockedMask))) return type;
            if (pick != RandomPick && !IsValid(type)) return StreamerType.None;

            var pool = new List<StreamerType>();
            foreach (var candidate in Roster)
                if (InMask(candidate, unlockedMask)) pool.Add(candidate);
            return pool.Count > 0 ? pool[random.Next(pool.Count)] : StreamerType.None;
        }

        /// <summary>Every streamer unlocked (a player whose unlocks aren't known, and the AI's own rules).</summary>
        public static uint AllUnlocked
        {
            get
            {
                uint mask = 0;
                foreach (var type in Roster) mask |= MaskOf(type);
                return mask;
            }
        }

        public static uint MaskOf(StreamerType type) => (int)type > 0 && (int)type < 32 ? 1u << (int)type : 0u;

        public static bool InMask(StreamerType type, uint mask) => (mask & MaskOf(type)) != 0;

        /// <summary>
        /// The Influence the next use costs: it grows with every use, like raising the Sub Cap, up to <see cref="MaxPowerCost"/>.
        /// The Boomers' discount for this shop comes off (never below 0), as it does for Sub Cap upgrades.
        /// </summary>
        public static int Cost(PlayerState s)
        {
            var info = Get(s.Streamer);
            if (!info.IsActive) return 0;
            return Math.Max(0, BaseCost(s) - Math.Max(0, s.ExpansionDiscount));
        }

        /// <summary>The next use's price before the Boomers' discount.</summary>
        public static int BaseCost(PlayerState s)
        {
            var info = Get(s.Streamer);
            if (!info.IsActive) return 0;
            return Math.Min(MaxPowerCost, info.InfluenceCost + info.InfluenceStep * Math.Max(0, s.PowerUses));
        }
    }
}
