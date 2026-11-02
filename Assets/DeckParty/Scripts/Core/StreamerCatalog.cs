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
    /// Actives are paid in Cash, and like raising the Sub Cap each use costs more (up to <see cref="StreamerCatalog.MaxPowerCost"/>).
    /// </summary>
    public sealed class StreamerInfo
    {
        public readonly StreamerType Type;
        public readonly PowerKind Kind;
        /// <summary>Cash the first use costs.</summary>
        public readonly int CashCost;
        /// <summary>Added to the cost after every use.</summary>
        public readonly int CashStep;
        /// <summary>Turns the power is unavailable after a use (0 = once per turn).</summary>
        public readonly int Cooldown;

        public StreamerInfo(StreamerType type, PowerKind kind, int cash = 0, int cashStep = 0, int cooldown = 0)
        {
            Type = type;
            Kind = kind;
            CashCost = cash;
            CashStep = cashStep;
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
        /// <summary>Raids alone can't push a stream past this much Deplatform Risk.</summary>
        public const int MaxRaidHeat = GameRules.RaidTroubleThreshold - 1;
        /// <summary>An active never costs more than this per use (the same ceiling as raising the Sub Cap).</summary>
        public const int MaxPowerCost = GameRules.MaxExpansionCost;
        public const int SmokeBombShield = 1;
        public const int GiveawayBonus = 1;
        public const int WealthTaxAmount = 2;
        public const int PalSphereMaxCost = 6;
        public const int XRayCount = 3;
        public const int StreakMaxBonus = 5;
        public const int BigStudioCapacity = 1;
        public const int BigStudioUpgradeTax = 1;

        static readonly StreamerInfo[] s_All =
        {
            new StreamerInfo(StreamerType.None, PowerKind.None),
            new StreamerInfo(StreamerType.Fate, PowerKind.Rival, cash: 1, cashStep: 1, cooldown: 2),
            new StreamerInfo(StreamerType.BroJogan, PowerKind.Passive),
            new StreamerInfo(StreamerType.Shinobi, PowerKind.NextStream, cash: 2, cashStep: 1, cooldown: 2),
            new StreamerInfo(StreamerType.IRunFast, PowerKind.Passive),
            new StreamerInfo(StreamerType.MrShark, PowerKind.NextStream, cash: 3, cashStep: 1, cooldown: 2),
            new StreamerInfo(StreamerType.EthicalFboi, PowerKind.Rival, cash: 1, cashStep: 1, cooldown: 2),
            new StreamerInfo(StreamerType.SoftHardBodyMan, PowerKind.Passive),
            new StreamerInfo(StreamerType.StrategicAbomination, PowerKind.NextStream, cash: 2, cashStep: 1, cooldown: 2),
            new StreamerInfo(StreamerType.Palmane, PowerKind.Instant, cash: 2, cashStep: 1, cooldown: 3),
            new StreamerInfo(StreamerType.ManimMakethMan, PowerKind.Passive),
            new StreamerInfo(StreamerType.RudeRadiologist, PowerKind.NextStream, cash: 1, cashStep: 1),
        };

        /// <summary>Real streamers in select-screen order (None and Random are extra tiles).</summary>
        public static readonly StreamerType[] Roster =
        {
            StreamerType.Fate, StreamerType.BroJogan, StreamerType.Shinobi, StreamerType.IRunFast,
            StreamerType.MrShark, StreamerType.EthicalFboi, StreamerType.SoftHardBodyMan, StreamerType.StrategicAbomination,
            StreamerType.Palmane, StreamerType.ManimMakethMan, StreamerType.RudeRadiologist,
        };

        public static IReadOnlyList<StreamerInfo> All => s_All;

        public static bool IsValid(StreamerType type) => (int)type >= 0 && (int)type < s_All.Length;

        public static StreamerInfo Get(StreamerType type) => IsValid(type) ? s_All[(int)type] : s_All[0];

        /// <summary>Turns a select-screen pick (which may be <see cref="RandomPick"/>) into a real streamer.</summary>
        public static StreamerType Resolve(byte pick, Random random)
        {
            if (pick == RandomPick) return Roster[random.Next(Roster.Length)];
            var type = (StreamerType)pick;
            return IsValid(type) ? type : StreamerType.None;
        }

        /// <summary>The Cash the next use costs: it grows with every use, like raising the Sub Cap, up to <see cref="MaxPowerCost"/>.</summary>
        public static int Cost(PlayerState s)
        {
            var info = Get(s.Streamer);
            if (!info.IsActive) return 0;
            return Math.Min(MaxPowerCost, info.CashCost + info.CashStep * Math.Max(0, s.PowerUses));
        }
    }
}
