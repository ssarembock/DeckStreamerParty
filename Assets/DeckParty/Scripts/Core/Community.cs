using System;

namespace DeckParty.Core
{
    /// <summary>
    /// A viewer's community. A viewer belongs to none, one or several (see <see cref="GuestCatalog.CommunitiesOf"/>).
    /// From the second <b>different</b> viewer in chat a community scores, in real time: two different viewers score 1 point, three score 2 and so on (two copies of the same card
    /// count once), and a viewer leaving chat takes its point away. Points give the bonuses in <see cref="Communities"/>.
    /// Values are saved and sent over the network, so only ever append.
    /// <para>V5 (ViewerCardsAndCommunityLogic.xlsx): the communities in <see cref="Communities.All"/> are in the game. The
    /// others (<see cref="Communities.IsRetired"/>) keep their rules and art in the project but nobody belongs to them, so
    /// they never score.</para>
    /// </summary>
    public enum Community : byte
    {
        CoreFanbase = 0,
        Staff = 1,
        Content = 2,            // retired 2026-11-10
        Techspertise = 3,       // retired in V5
        FinanceBros = 4,        // retired in V5
        Trolls = 5,             // "Troll Army" since V5
        Doomsdayers = 6,        // retired in V5
        Boomer = 7,
        Millennial = 8,
        Zoomer = 9,             // retired in V5 (Gen-Z replaces it)
        ArmchairActivists = 10, // retired in V5
        Clout = 11,             // retired in V5
        MemeticsDivision = 12,  // retired in V5
        Bots = 13,              // retired in V5

        // --- V5 ---
        GenZ = 14,
        Gatekeepers = 15,
        HypeMen = 16,
        Mods = 17,              // retired 2026-11-10
        FullStream = 18,        // retired 2026-11-10
        Marketers = 19,         // retired 2026-11-10
        Saboteurs = 20,         // retired 2026-11-10
        ShopControl = 21,       // retired 2026-11-10
        Voyeurs = 22,
    }

    /// <summary>A set of communities (one bit per <see cref="Community"/>). Shown as a multi-select in the Game Database.</summary>
    [Flags]
    public enum CommunityMask
    {
        None = 0,
        CoreFanbase = 1 << 0,
        Staff = 1 << 1,
        Content = 1 << 2,
        Techspertise = 1 << 3,
        FinanceBros = 1 << 4,
        Trolls = 1 << 5,
        Doomsdayers = 1 << 6,
        Boomer = 1 << 7,
        Millennial = 1 << 8,
        Zoomer = 1 << 9,
        ArmchairActivists = 1 << 10,
        Clout = 1 << 11,
        MemeticsDivision = 1 << 12,
        Bots = 1 << 13,
        GenZ = 1 << 14,
        Gatekeepers = 1 << 15,
        HypeMen = 1 << 16,
        Mods = 1 << 17,
        FullStream = 1 << 18,
        Marketers = 1 << 19,
        Saboteurs = 1 << 20,
        ShopControl = 1 << 21,
        Voyeurs = 1 << 22,
    }

    /// <summary>
    /// Community rules and tuning. Chances are whole percentages, rolled with the season's deterministic RNG, so the host
    /// and every client agree. The communities in the game (2026-11-10, StreamersViewersCommunities.md):
    /// <list type="bullet">
    /// <item>Core Fanbase: 10% per point (max 3 points, 30%) that each Core Fanbase viewer scores twice (the scoring plays twice).</item>
    /// <item>Staff: a flat +1 Cash per point (max 3) when the stream is scored.</item>
    /// <item>Boomers: each point makes Sub Cap upgrades 1 Cash cheaper in your next shop. After a scored stream each Boomer
    /// in chat has a 10% chance to leave your Audience for good (and a Millennial inherits).</item>
    /// <item>Millennials: each time a Boomer leaves your Audience, a random Millennial you own permanently earns +1 Cash,
    /// once per point (at least once).</item>
    /// <item>Gen-Z: after a scored stream, each Gen-Z viewer in chat permanently gains +1 Clout or +1 Cash (random) per point.</item>
    /// <item>Gatekeepers: each point gives one more use of a Gatekeeper's eviction action this stream.</item>
    /// <item>Hype Men: 10% per point (max 2 points, 20%) that a Server Down stream still scores.</item>
    /// <item>Troll Army (Trolls): +2 Clout per point of Risk in the stream when it's scored, cancelled Risk included.</item>
    /// <item>Voyeurs: each point (max 1) gives one more use of a Voyeur's peek action this stream.</item>
    /// </list>
    /// Retired 2026-11-10 (still in the code, nobody belongs): Content, Mods, Full Stream, Marketers, Shop Control and
    /// Saboteurs; retired in V5: Techspertise, Finance Bros, Doomsdayers, Zoomer, Armchair Activists, Clout, Memetics
    /// Division and Bots. Their numbers are kept below for when they come back.
    /// </summary>
    public static class Communities
    {
        /// <summary>
        /// Different viewers from a community needed in chat before it scores its first point (PlayTestUpdates: one viewer is
        /// not a community). Every further different viewer adds a point, so the score is (different viewers - 1).
        /// </summary>
        public const int MembersForFirstPoint = 2;

        /// <summary>Number of <see cref="Community"/> values (array size for scores). Not all of them are in the game.</summary>
        public const int Count = 23;

        // --- V5 communities ---
        // Viewer card edits (2026-11-08, Communities.csv): Core Fanbase 20% a point up to 4, Staff and Content 30% up to 3.
        // 2026-11-10 (user request): Core Fanbase 10% a point up to 30% (was 20% up to 80%).
        public const int CoreFanbasePercentPerPoint = 10;
        public const int CoreFanbaseMaxPoints = 3;
        /// <summary>Staff (2026-11-10): Cash the stream earns per point when scored (it used to be a chance to waive fees).</summary>
        public const int StaffCashPerPoint = 1;
        public const int StaffMaxPoints = 3;
        public const int ContentPercentPerPoint = 30;
        public const int ContentMaxPoints = 3;
        /// <summary>Boomers: Cash off each Sub Cap upgrade in the next shop, per point.</summary>
        public const int BoomerDiscountPerPoint = 1;
        /// <summary>Boomers: chance (%) that each Boomer in chat leaves the Audience for good after a scored stream.</summary>
        public const int BoomerRetirePercent = 10;
        /// <summary>Millennials: Cash a Millennial inherits each time.</summary>
        public const int InheritanceCash = 1;
        public const int GenZMaxPoints = 2;
        public const int HypeMenPercentPerPoint = 10;
        public const int HypeMenMaxPoints = 2;
        /// <summary>Troll Army (2026-11-10): Clout per point of Risk in the stream when scored, cancelled Risk included.</summary>
        public const int TrollArmyCloutPerRisk = 2;
        public const int ModsPercentPerPoint = 10;
        public const int ModsMaxPoints = 2;
        public const int FullStreamMaxPoints = 3;
        public const int SaboteurMaxPoints = 5;
        public const int ShopControlMaxPoints = 5;
        public const int VoyeursMaxPoints = 1;
        /// <summary>Marketers: extra recruit uses this stream, at most (Communities.csv, 2026-11-08).</summary>
        public const int MarketersMaxPoints = 3;

        // --- retired communities (kept for when they come back) ---
        public const int CoreFanbasePointsPerCancel = 3;
        public const int TechspertiseMaxPoints = 3;
        static readonly int[] s_TechspertisePercent = { 0, 20, 40, 50 };
        public const int FinancePercentPerPoint = 10;
        public const int FinanceMaxPoints = 9;
        public const int DoomPercentPerPoint = 4;
        public const int DoomMaxPoints = 5;
        public const int BoomerCashPerPoint = 2;
        public const int BoomerSubsCostPerPoint = 1;
        public const int MillennialSubsPerPoint = 2;
        public const int MillennialCashCostPerPoint = 1;
        public const int ZoomerPercentPerPoint = 20;
        public const int ZoomerMaxPoints = 4;
        public const int ZoomerSubs = 6;
        public const int ZoomerCashCost = 3;
        public const int ArmchairPercentPerPoint = 10;
        public const int ArmchairMaxPoints = 5;
        public const int CloutPercentPerPoint = 10;
        public const int CloutMaxPoints = 4;
        public const int CloutSubs = 1;
        public const int MemeticsPercentPerPoint = 1;
        public const int MemeticsMaxPoints = 10;
        public const int BotsPercentPerPoint = 10;
        public const int BotsMaxPoints = 4;
        /// <summary>Most Bots that can wait for one player's next stream (several rivals' Bots add up to this).</summary>
        public const int BotsMaxIncoming = 6;

        /// <summary>The communities in the game, in display order (Community Beats, tooltips, the Game Database).</summary>
        public static readonly Community[] All =
        {
            Community.CoreFanbase, Community.Staff, Community.Boomer, Community.Millennial, Community.GenZ,
            Community.Gatekeepers, Community.HypeMen, Community.Trolls, Community.Voyeurs,
        };

        /// <summary>Every community value, retired ones included (art slots, saved masks).</summary>
        public static readonly Community[] Every = BuildEvery();

        /// <summary>The bits of the communities in the game. Masks are cut down to this, so retired communities never score.</summary>
        public static readonly CommunityMask ActiveMask = BuildActiveMask();

        static Community[] BuildEvery()
        {
            var every = new Community[Count];
            for (int i = 0; i < Count; i++) every[i] = (Community)i;
            return every;
        }

        static CommunityMask BuildActiveMask()
        {
            var mask = CommunityMask.None;
            foreach (var community in All) mask |= Bit(community);
            return mask;
        }

        /// <summary>Taken out of the game in V5: kept in the code, but no viewer belongs to it.</summary>
        public static bool IsRetired(Community community) => !Has(ActiveMask, community);

        public static CommunityMask Bit(Community community) => (CommunityMask)(1 << (int)community);

        public static bool Has(CommunityMask mask, Community community) => (mask & Bit(community)) != 0;

        public static int CountIn(CommunityMask mask)
        {
            int count = 0;
            for (int bits = (int)mask & ((1 << Count) - 1); bits != 0; bits &= bits - 1) count++;
            return count;
        }

        /// <summary>The highest score that still changes anything (0 = no cap: every point keeps adding).</summary>
        public static int MaxPoints(Community community)
        {
            switch (community)
            {
                case Community.CoreFanbase: return CoreFanbaseMaxPoints;
                case Community.Staff: return StaffMaxPoints;
                case Community.Content: return ContentMaxPoints;
                case Community.GenZ: return GenZMaxPoints;
                case Community.HypeMen: return HypeMenMaxPoints;
                case Community.Mods: return ModsMaxPoints;
                case Community.FullStream: return FullStreamMaxPoints;
                case Community.Saboteurs: return SaboteurMaxPoints;
                case Community.ShopControl: return ShopControlMaxPoints;
                case Community.Voyeurs: return VoyeursMaxPoints;
                case Community.Marketers: return MarketersMaxPoints;
                // Retired.
                case Community.Techspertise: return TechspertiseMaxPoints;
                case Community.FinanceBros: return FinanceMaxPoints;
                case Community.Doomsdayers: return DoomMaxPoints;
                case Community.Zoomer: return ZoomerMaxPoints;
                case Community.ArmchairActivists: return ArmchairMaxPoints;
                case Community.Clout: return CloutMaxPoints;
                case Community.MemeticsDivision: return MemeticsMaxPoints;
                case Community.Bots: return BotsMaxPoints;
                default: return 0;
            }
        }

        /// <summary>Points that count (capped at <see cref="MaxPoints"/> where there is a cap).</summary>
        public static int Capped(Community community, int points)
        {
            int max = MaxPoints(community);
            return max > 0 ? Clamp(points, max) : Math.Max(0, points);
        }

        public static int CoreFanbaseEncoreChance(int points) => Clamp(points, CoreFanbaseMaxPoints) * CoreFanbasePercentPerPoint;

        /// <summary>Staff: the flat Cash a scored stream earns (+1 per point, up to the cap).</summary>
        public static int StaffCash(int points) => Clamp(points, StaffMaxPoints) * StaffCashPerPoint;

        public static int ContentEncoreChance(int points) => Clamp(points, ContentMaxPoints) * ContentPercentPerPoint;

        public static int BoomerDiscount(int points) => Math.Max(0, points) * BoomerDiscountPerPoint;

        /// <summary>How many times a Millennial inherits when one Boomer leaves (once per point, at least once).</summary>
        public static int Inheritances(int millennialPoints) => Math.Max(1, millennialPoints);

        public static int GenZGrowth(int points) => Clamp(points, GenZMaxPoints);

        /// <summary>Extra uses of a member's action this stream (Gatekeepers, Marketers, Voyeurs).</summary>
        public static int ExtraUses(Community community, int points) => Capped(community, points);

        public static int HypeMenChance(int points) => Clamp(points, HypeMenMaxPoints) * HypeMenPercentPerPoint;

        /// <summary>Troll Army: Clout a scored stream earns with this much Risk in it (cancelled Risk counts), once the community scores.</summary>
        public static int TrollArmyClout(int points, int risk) => points > 0 ? TrollArmyCloutPerRisk * Math.Max(0, risk) : 0;

        public static int ModsChance(int points) => Clamp(points, ModsMaxPoints) * ModsPercentPerPoint;

        /// <summary>Full Stream: how many empty spaces chat may have and still count as full.</summary>
        public static int FullStreamSlack(int points) => Clamp(points, FullStreamMaxPoints);

        public static int SabotageAmount(int points) => Clamp(points, SaboteurMaxPoints);

        public static int ShopControlDiscount(int points) => Clamp(points, ShopControlMaxPoints);

        // --- retired communities ---

        public static int CoreFanbaseCancels(int points) => 0;

        public static int TechspertiseChance(int points) => s_TechspertisePercent[Clamp(points, TechspertiseMaxPoints)];

        public static int FinanceDoubleChance(int points) => Clamp(points, FinanceMaxPoints) * FinancePercentPerPoint;

        public static int DoomChance(int points) => Clamp(points, DoomMaxPoints) * DoomPercentPerPoint;

        public static int ZoomerChance(int points) => Clamp(points, ZoomerMaxPoints) * ZoomerPercentPerPoint;

        public static int ArmchairSilenceChance(int points) => Clamp(points, ArmchairMaxPoints) * ArmchairPercentPerPoint;

        public static int CloutChance(int points) => Clamp(points, CloutMaxPoints) * CloutPercentPerPoint;

        public static int MemeticsRepeatChance(int points) => Clamp(points, MemeticsMaxPoints) * MemeticsPercentPerPoint;

        public static int BotChance(int points) => Clamp(points, BotsMaxPoints) * BotsPercentPerPoint;

        /// <summary>
        /// The old fixed, once-per-stream rewards (V4.1 Boomer +Cash/-Subs, Millennial +Subs/-Cash). Since V5 Boomers and
        /// Millennials work differently, so this is always zero.
        /// </summary>
        public static void FixedRewards(int[] scores, out int subs, out int cash)
        {
            subs = 0;
            cash = 0;
        }

        /// <summary>
        /// The headline number for a score: the chance in percent, or for counting communities the amount (Cash off
        /// upgrades, resources if deplatformed, extra uses, spaces of slack, Clout on/off a recruit, inheritances, growth).
        /// </summary>
        public static int Effect(Community community, int points)
        {
            switch (community)
            {
                case Community.CoreFanbase: return CoreFanbaseEncoreChance(points);
                case Community.Staff: return StaffCash(points);
                case Community.Content: return ContentEncoreChance(points);
                case Community.Boomer: return BoomerDiscount(points);
                case Community.Millennial: return points > 0 ? Inheritances(points) : 0;
                case Community.GenZ: return GenZGrowth(points);
                case Community.Gatekeepers:
                case Community.Marketers:
                case Community.Voyeurs: return ExtraUses(community, points);
                case Community.HypeMen: return HypeMenChance(points);
                case Community.Trolls: return points > 0 ? TrollArmyCloutPerRisk : 0;
                case Community.Mods: return ModsChance(points);
                case Community.FullStream: return FullStreamSlack(points);
                case Community.Saboteurs: return SabotageAmount(points);
                case Community.ShopControl: return ShopControlDiscount(points);
                // Retired.
                case Community.Techspertise: return TechspertiseChance(points);
                case Community.FinanceBros: return FinanceDoubleChance(points);
                case Community.Doomsdayers: return DoomChance(points);
                case Community.Zoomer: return ZoomerChance(points);
                case Community.ArmchairActivists: return ArmchairSilenceChance(points);
                case Community.Clout: return CloutChance(points);
                case Community.MemeticsDivision: return MemeticsRepeatChance(points);
                case Community.Bots: return BotChance(points);
                default: return 0;
            }
        }

        /// <summary>Whether <see cref="Effect"/> is a percentage.</summary>
        public static bool IsChance(Community community)
        {
            switch (community)
            {
                case Community.CoreFanbase:
                case Community.Content:
                case Community.HypeMen:
                case Community.Mods:
                case Community.Techspertise:
                case Community.FinanceBros:
                case Community.Doomsdayers:
                case Community.Zoomer:
                case Community.ArmchairActivists:
                case Community.Clout:
                case Community.MemeticsDivision:
                case Community.Bots:
                    return true;
                default:
                    return false;
            }
        }

        static int Clamp(int points, int max) => Math.Max(0, Math.Min(max, points));
    }
}
