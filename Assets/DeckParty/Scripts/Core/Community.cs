using System;

namespace DeckParty.Core
{
    /// <summary>
    /// A viewer's community. A viewer belongs to none, one or several (see <see cref="GuestCatalog.CommunitiesOf"/>).
    /// Every <b>different</b> viewer in chat from a community gives it one point, in real time: two copies of the same card
    /// count once, and a viewer leaving chat takes its point away. Points give the bonuses in <see cref="Communities"/>.
    /// Values are saved and sent over the network.
    /// </summary>
    public enum Community : byte
    {
        CoreFanbase = 0,
        Staff = 1,
        Content = 2,
        Techspertise = 3,
        FinanceBros = 4,
        Trolls = 5,
        Doomsdayers = 6,
        Boomer = 7,
        Millennial = 8,
        Zoomer = 9,
        ArmchairActivists = 10,
        Clout = 11,
        MemeticsDivision = 12,
        Bots = 13,
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
    }

    /// <summary>
    /// Community rules and tuning. Chances are whole percentages, rolled with the season's deterministic RNG, so the host
    /// and every client agree.
    /// <list type="bullet">
    /// <item>Core Fanbase: every 3 points cancel 1 Deplatform Risk (live, like a Normie).</item>
    /// <item>Staff: 10% per point (max 90%) that each Staff viewer's Cash cost is waived when they're scored.</item>
    /// <item>Content: 10% per point (max 90%) that each viewer outside the Core Fanbase scores twice when the stream ends.</item>
    /// <item>Techspertise: 0/20/40/50% (max 3 points) that a Server Down stream still scores.</item>
    /// <item>Finance Bros: 10% per point (max 90%) that each viewer's Cash reward is scored twice.</item>
    /// <item>Trolls: 1 random Gifted Sub or Cash per point when the stream is deplatformed.</item>
    /// <item>Doomsdayers (multiplayer): 4% per point (max 20%) that each viewer in a rival's next stream scores nothing.</item>
    /// <item>Boomer: +2 Cash and -1 Subs per point when the stream is scored (fixed, once per stream).</item>
    /// <item>Millennial: +2 Subs and -1 Cash per point when the stream is scored (fixed, once per stream).</item>
    /// <item>Zoomer: two separate rolls when the stream is scored, each 20% per point (max 80% at 4): +6 Subs, and -3 Cash.</item>
    /// <item>Armchair Activists: 10% per point (max 50% at 5) that each Armchair Activist scores nothing.</item>
    /// <item>Clout: 10% per point (max 40% at 4) that each viewer outside the Core Fanbase earns +1 Gifted Sub more, rolled each time they score.</item>
    /// <item>Memetics Division: 1% per point (max 10% at 10) that the whole stream scores a second time.</item>
    /// <item>Bots (multiplayer): 10% per point (max 40% at 4), rolled for each Bots viewer in chat when the stream is scored, that a
    /// Bot joins the next player's Audience for their next stream (it takes a space and earns nothing).</item>
    /// </list>
    /// </summary>
    public static class Communities
    {
        public const int Count = 14;

        public const int CoreFanbasePointsPerCancel = 3;
        public const int StaffPercentPerPoint = 10;
        public const int StaffMaxPoints = 9;
        public const int ContentPercentPerPoint = 10;
        public const int ContentMaxPoints = 9;
        public const int TechspertiseMaxPoints = 3;
        static readonly int[] s_TechspertisePercent = { 0, 20, 40, 50 };
        public const int FinancePercentPerPoint = 10;
        public const int FinanceMaxPoints = 9;
        public const int TrollsRewardPerPoint = 1;
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

        public static readonly Community[] All =
        {
            Community.CoreFanbase, Community.Staff, Community.Content, Community.Techspertise,
            Community.FinanceBros, Community.Trolls, Community.Doomsdayers,
            Community.Boomer, Community.Millennial, Community.Zoomer, Community.ArmchairActivists,
            Community.Clout, Community.MemeticsDivision, Community.Bots,
        };

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
                case Community.Staff: return StaffMaxPoints;
                case Community.Content: return ContentMaxPoints;
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

        public static int CoreFanbaseCancels(int points) => Math.Max(0, points) / CoreFanbasePointsPerCancel;

        public static int StaffFreeChance(int points) => Clamp(points, StaffMaxPoints) * StaffPercentPerPoint;

        public static int ContentEncoreChance(int points) => Clamp(points, ContentMaxPoints) * ContentPercentPerPoint;

        public static int TechspertiseChance(int points) => s_TechspertisePercent[Clamp(points, TechspertiseMaxPoints)];

        public static int FinanceDoubleChance(int points) => Clamp(points, FinanceMaxPoints) * FinancePercentPerPoint;

        public static int TrollsReward(int points) => Math.Max(0, points) * TrollsRewardPerPoint;

        public static int DoomChance(int points) => Clamp(points, DoomMaxPoints) * DoomPercentPerPoint;

        public static int ZoomerChance(int points) => Clamp(points, ZoomerMaxPoints) * ZoomerPercentPerPoint;

        public static int ArmchairSilenceChance(int points) => Clamp(points, ArmchairMaxPoints) * ArmchairPercentPerPoint;

        public static int CloutChance(int points) => Clamp(points, CloutMaxPoints) * CloutPercentPerPoint;

        public static int MemeticsRepeatChance(int points) => Clamp(points, MemeticsMaxPoints) * MemeticsPercentPerPoint;

        public static int BotChance(int points) => Clamp(points, BotsMaxPoints) * BotsPercentPerPoint;

        /// <summary>
        /// The fixed, once-per-stream rewards when a stream is scored: Boomer (+Cash, -Subs) and Millennial (+Subs, -Cash).
        /// Zoomer's two rolls are random, so they're not included here.
        /// </summary>
        public static void FixedRewards(int[] scores, out int subs, out int cash)
        {
            int boomer = Math.Max(0, scores[(int)Community.Boomer]);
            int millennial = Math.Max(0, scores[(int)Community.Millennial]);
            subs = millennial * MillennialSubsPerPoint - boomer * BoomerSubsCostPerPoint;
            cash = boomer * BoomerCashPerPoint - millennial * MillennialCashCostPerPoint;
        }

        /// <summary>One community's fixed reward (Boomer or Millennial; zero for the others).</summary>
        public static void FixedReward(Community community, int points, out int subs, out int cash)
        {
            points = Math.Max(0, points);
            switch (community)
            {
                case Community.Boomer:
                    subs = -points * BoomerSubsCostPerPoint;
                    cash = points * BoomerCashPerPoint;
                    break;
                case Community.Millennial:
                    subs = points * MillennialSubsPerPoint;
                    cash = -points * MillennialCashCostPerPoint;
                    break;
                default:
                    subs = cash = 0;
                    break;
            }
        }

        /// <summary>
        /// The headline number for a score: Risk cancelled (Core Fanbase), resources if deplatformed (Trolls), the Cash
        /// (Boomer) or Subs (Millennial) earned, or the chance in percent (everything else).
        /// </summary>
        public static int Effect(Community community, int points)
        {
            switch (community)
            {
                case Community.CoreFanbase: return CoreFanbaseCancels(points);
                case Community.Staff: return StaffFreeChance(points);
                case Community.Content: return ContentEncoreChance(points);
                case Community.Techspertise: return TechspertiseChance(points);
                case Community.FinanceBros: return FinanceDoubleChance(points);
                case Community.Trolls: return TrollsReward(points);
                case Community.Doomsdayers: return DoomChance(points);
                case Community.Boomer: return Math.Max(0, points) * BoomerCashPerPoint;
                case Community.Millennial: return Math.Max(0, points) * MillennialSubsPerPoint;
                case Community.Zoomer: return ZoomerChance(points);
                case Community.ArmchairActivists: return ArmchairSilenceChance(points);
                case Community.Clout: return CloutChance(points);
                case Community.MemeticsDivision: return MemeticsRepeatChance(points);
                case Community.Bots: return BotChance(points);
                default: return 0;
            }
        }

        /// <summary>Whether <see cref="Effect"/> is a percentage.</summary>
        public static bool IsChance(Community community) =>
            community != Community.CoreFanbase && community != Community.Trolls && community != Community.Boomer && community != Community.Millennial;

        static int Clamp(int points, int max) => Math.Max(0, Math.Min(max, points));
    }
}
