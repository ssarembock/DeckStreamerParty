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
        Clout = 11,             // retired in V5 (a community, not the old Clout resource)
        MemeticsDivision = 12,  // retired in V5
        Bots = 13,              // retired in V5

        // --- V5 ---
        GenZ = 14,
        Gatekeepers = 15,       // 2026-11-16: random kickers (the old Gatekeepers rule is Mods now)
        HypeMen = 16,
        Mods = 17,              // retired 2026-11-10, back 2026-11-16 with the old Gatekeepers rule (extra removal uses)
        FullStream = 18,        // retired 2026-11-10
        Marketers = 19,         // retired 2026-11-10
        Saboteurs = 20,         // retired 2026-11-10
        ShopControl = 21,       // retired 2026-11-10
        Voyeurs = 22,

        // --- 2026-11-18 ---
        Lurkers = 23,           // +1 Cash per empty space per point when the stream is scored
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
        Lurkers = 1 << 23,
    }

    /// <summary>
    /// Community rules and tuning. Chances are whole percentages, rolled with the season's deterministic RNG, so the host
    /// and every client agree. The communities in the game (2026-11-16, StreamersViewersCommunities.md):
    /// <list type="bullet">
    /// <item>Core Fanbase: 10% per point (max 5 points, 50%; 2026-11-18) that each Core Fanbase viewer scores twice (the scoring plays twice).</item>
    /// <item>Staff: a flat +1 Influence per point (max 3) when the stream is scored.</item>
    /// <item>Boomers (2026-11-18): every Boomer without an action of its own has Early Retirement: the whole chat scores right
    /// now (banked, the stream goes on), then the Boomer retires (leaves your Audience for good). The points (max 2) lower
    /// that chance: 100% / 50% / 25%. The old shop discount and the 10% retirement after scored streams are gone.</item>
    /// <item>Millennials: each time a Boomer retires, a random Millennial you own permanently earns +1 Cash, +1 more per point
    /// (max 2 points, +3; 2026-11-18).</item>
    /// <item>Gen-Z: after a scored stream, each Gen-Z viewer in chat permanently gains +1 Cash or +1 Influence (random) per point.</item>
    /// <item>Gatekeepers: a Gatekeeper's action sends random other viewers back to the queue: 3 at 0 points, 6 at 1 point,
    /// everyone else at 2 points.</item>
    /// <item>Hype Men: each point (max 2) is a spare seat past the Sub Cap that only viewers brought in by a Hype Man can
    /// take, so they don't crash the server.</item>
    /// <item>Mods: each point gives one more use of a Mod's removal action this stream (the old Gatekeepers rule).</item>
    /// <item>Troll Army (Trolls): +2 Cash per point of Risk in the stream when it's scored, cancelled Risk included.</item>
    /// <item>Voyeurs: each point (max 1) gives one more use of a Voyeur's peek action this stream.</item>
    /// <item>Lurkers (2026-11-18): +1 Cash per empty space in chat per point (max 2) when the stream is scored.</item>
    /// </list>
    /// Retired 2026-11-10 (still in the code, nobody belongs): Content, Full Stream, Marketers, Shop Control and Saboteurs
    /// (and the old Mods rule, a chance to cancel 1 Risk next stream); retired in V5: Techspertise, Finance Bros,
    /// Doomsdayers, Zoomer, Armchair Activists, Clout, Memetics Division and Bots. Their numbers are kept below for when
    /// they come back.
    /// </summary>
    public static class Communities
    {
        /// <summary>
        /// Different viewers from a community needed in chat before it scores its first point (PlayTestUpdates: one viewer is
        /// not a community). Every further different viewer adds a point, so the score is (different viewers - 1).
        /// </summary>
        public const int MembersForFirstPoint = 2;

        /// <summary>Number of <see cref="Community"/> values (array size for scores). Not all of them are in the game.</summary>
        public const int Count = 24;

        // --- V5 communities ---
        // Viewer card edits (2026-11-08, Communities.csv): Core Fanbase 20% a point up to 4, Staff and Content 30% up to 3.
        // 2026-11-10 (user request): Core Fanbase 10% a point up to 30% (was 20% up to 80%).
        // 2026-11-18 (community updates): up to 5 points, 50% (6 different Core Fanbase viewers in chat).
        public const int CoreFanbasePercentPerPoint = 10;
        public const int CoreFanbaseMaxPoints = 5;
        /// <summary>Staff (2026-11-10): Influence the stream earns per point when scored (it used to be a chance to waive fees).</summary>
        public const int StaffInfluencePerPoint = 1;
        public const int StaffMaxPoints = 3;
        public const int ContentPercentPerPoint = 30;
        public const int ContentMaxPoints = 3;
        /// <summary>
        /// The Boomers' old shop discount (Influence off Sub Cap upgrades and the active, per level). Retired on 2026-11-18:
        /// the Boomers' points now lower the chance that Early Retirement retires the Boomer.
        /// </summary>
        public const int BoomerDiscountPerPoint = 1;
        /// <summary>Boomers (2026-11-18): the usual points (different Boomers in chat - 1), at most 2 (3 Boomers in chat).</summary>
        public const int BoomerMaxPoints = 2;
        /// <summary>Boomers (2026-11-18): the chance (%) that Early Retirement retires the Boomer, at 0, 1 and 2 points.</summary>
        static readonly int[] s_BoomerRetirePercent = { 100, 50, 25 };
        /// <summary>The old chance (%) that each Boomer in a scored stream retired. Retired on 2026-11-18 (Early Retirement does it now).</summary>
        public const int BoomerRetirePercent = 10;
        /// <summary>Millennials (2026-11-16: Cash, it was Influence): Cash a Millennial inherits when a Boomer retires, before the Millennials' points.</summary>
        public const int InheritanceCash = 1;
        /// <summary>Millennials (2026-11-18): each point adds this much Cash to the inheritance, up to <see cref="MillennialMaxPoints"/> (+3 in all).</summary>
        public const int InheritanceCashPerPoint = 1;
        public const int MillennialMaxPoints = 2;
        public const int GenZMaxPoints = 2;
        /// <summary>Hype Men (2026-11-16): spare seats past the Sub Cap per point, for viewers a Hype Man brings in.</summary>
        public const int HypeMenSeatsPerPoint = 1;
        public const int HypeMenMaxPoints = 2;
        /// <summary>Gatekeepers (2026-11-16): random viewers a Gatekeeper's action sends back at 0 and 1 points; at 2 points, everyone else.</summary>
        static readonly int[] s_GatekeeperSendBack = { 3, 6 };
        public const int GatekeepersMaxPoints = 2;
        /// <summary>Troll Army (2026-11-10): Cash per point of Risk in the stream when scored, cancelled Risk included.</summary>
        public const int TrollArmyCashPerRisk = 2;
        /// <summary>The Mods' old rule (retired 2026-11-16, Mods give extra removal uses now): chance per point to cancel 1 Risk next stream.</summary>
        public const int ModsPercentPerPoint = 10;
        public const int ModsShieldMaxPoints = 2;
        public const int FullStreamMaxPoints = 3;
        public const int SaboteurMaxPoints = 5;
        public const int ShopControlMaxPoints = 5;
        public const int VoyeursMaxPoints = 1;
        /// <summary>Lurkers (2026-11-18): Cash per empty space in chat, per point, when the stream is scored.</summary>
        public const int LurkersCashPerSpace = 1;
        public const int LurkersMaxPoints = 2;
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
        public const int BoomerInfluencePerPoint = 2;
        public const int BoomerCashCostPerPoint = 1;
        public const int MillennialCashPerPoint = 2;
        public const int MillennialInfluenceCostPerPoint = 1;
        public const int ZoomerPercentPerPoint = 20;
        public const int ZoomerMaxPoints = 4;
        public const int ZoomerCash = 6;
        public const int ZoomerInfluenceCost = 3;
        public const int ArmchairPercentPerPoint = 10;
        public const int ArmchairMaxPoints = 5;
        public const int CloutPercentPerPoint = 10;
        public const int CloutMaxPoints = 4;
        public const int CloutCash = 1;
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
            Community.Mods, Community.Gatekeepers, Community.HypeMen, Community.Lurkers, Community.Trolls, Community.Voyeurs,
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

        /// <summary>
        /// A community's score from the number of <b>different</b> members in chat: one point for every member after the
        /// first (<see cref="MembersForFirstPoint"/>). Caps are applied by the rules (<see cref="Capped"/>), so the score can
        /// pass them. (The Boomers had levels at 2 and 4 members from 2026-11-16 until the 2026-11-18 community updates.)
        /// </summary>
        public static int PointsFromMembers(Community community, int members) => Math.Max(0, members - MembersForFirstPoint + 1);

        /// <summary>Different members in chat a community needs for this point.</summary>
        public static int MembersForPoint(Community community, int point) => MembersForFirstPoint + Math.Max(1, point) - 1;

        /// <summary>The highest score that still changes anything (0 = no cap: every point keeps adding).</summary>
        public static int MaxPoints(Community community)
        {
            switch (community)
            {
                case Community.CoreFanbase: return CoreFanbaseMaxPoints;
                case Community.Staff: return StaffMaxPoints;
                case Community.Content: return ContentMaxPoints;
                case Community.Boomer: return BoomerMaxPoints;
                case Community.Millennial: return MillennialMaxPoints;
                case Community.GenZ: return GenZMaxPoints;
                case Community.HypeMen: return HypeMenMaxPoints;
                case Community.Gatekeepers: return GatekeepersMaxPoints;
                case Community.FullStream: return FullStreamMaxPoints;
                case Community.Saboteurs: return SaboteurMaxPoints;
                case Community.ShopControl: return ShopControlMaxPoints;
                case Community.Voyeurs: return VoyeursMaxPoints;
                case Community.Lurkers: return LurkersMaxPoints;
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

        /// <summary>Staff: the flat Influence a scored stream earns (+1 per point, up to the cap).</summary>
        public static int StaffInfluence(int points) => Clamp(points, StaffMaxPoints) * StaffInfluencePerPoint;

        public static int ContentEncoreChance(int points) => Clamp(points, ContentMaxPoints) * ContentPercentPerPoint;

        /// <summary>The Boomers' old shop discount (retired on 2026-11-18; nothing applies it any more).</summary>
        public static int BoomerDiscount(int level) => Clamp(level, BoomerMaxPoints) * BoomerDiscountPerPoint;

        /// <summary>
        /// Boomers (2026-11-18): the chance (%) that a Boomer's Early Retirement retires it: 100% at 0 points (one Boomer in
        /// chat), 50% at 1 (two different Boomers), 25% at 2 (three or more).
        /// </summary>
        public static int BoomerRetireChance(int points) => s_BoomerRetirePercent[Clamp(points, BoomerMaxPoints)];

        /// <summary>
        /// Millennials (2026-11-18): the Cash a random Millennial you own inherits when a Boomer retires: +1, plus 1 per
        /// Millennials point (at most 2), so +1 / +2 / +3.
        /// </summary>
        public static int InheritanceAmount(int millennialPoints) => InheritanceCash + Clamp(millennialPoints, MillennialMaxPoints) * InheritanceCashPerPoint;

        /// <summary>How many times a Millennial inherits when one Boomer leaves. Once since 2026-11-18 (the points raise the amount, <see cref="InheritanceAmount"/>).</summary>
        public static int Inheritances(int millennialPoints) => 1;

        public static int GenZGrowth(int points) => Clamp(points, GenZMaxPoints);

        /// <summary>Extra uses of a member's action this stream (Mods, Marketers, Voyeurs).</summary>
        public static int ExtraUses(Community community, int points) => Capped(community, points);

        /// <summary>Hype Men: spare seats past the Sub Cap for viewers a Hype Man brings in (1 per point, at most 2).</summary>
        public static int HypeSeats(int points) => Clamp(points, HypeMenMaxPoints) * HypeMenSeatsPerPoint;

        /// <summary>
        /// Gatekeepers: how many random other viewers a Gatekeeper's action sends back: 3 at 0 points, 6 at 1 point, and
        /// everyone else (<see cref="int.MaxValue"/>) at 2 points.
        /// </summary>
        public static int GatekeeperSendBack(int points)
        {
            int level = Clamp(points, GatekeepersMaxPoints);
            return level < s_GatekeeperSendBack.Length ? s_GatekeeperSendBack[level] : int.MaxValue;
        }

        /// <summary>Gatekeepers' send-back counts as compact text for tooltips ("3/6").</summary>
        public static string GatekeeperSteps => string.Join("/", s_GatekeeperSendBack);

        /// <summary>Troll Army: Cash a scored stream earns with this much Risk in it (cancelled Risk counts), once the community scores.</summary>
        public static int TrollArmyCash(int points, int risk) => points > 0 ? TrollArmyCashPerRisk * Math.Max(0, risk) : 0;

        /// <summary>Lurkers: Cash per empty space at this score (+1 per point, at most 2).</summary>
        public static int LurkersPerSpace(int points) => Clamp(points, LurkersMaxPoints) * LurkersCashPerSpace;

        /// <summary>Lurkers: Cash a scored stream earns with this many empty spaces in chat.</summary>
        public static int LurkersCash(int points, int emptySpaces) => LurkersPerSpace(points) * Math.Max(0, emptySpaces);

        /// <summary>The Mods' old rule (retired 2026-11-16): the chance to cancel 1 Risk next stream. Nothing rolls it any more.</summary>
        public static int ModsShieldChance(int points) => Clamp(points, ModsShieldMaxPoints) * ModsPercentPerPoint;

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
        /// The old fixed, once-per-stream rewards (V4.1 Boomer +Influence/-Cash, Millennial +Cash/-Influence). Since V5 Boomers and
        /// Millennials work differently, so this is always zero.
        /// </summary>
        public static void FixedRewards(int[] scores, out int cash, out int influence)
        {
            cash = 0;
            influence = 0;
        }

        /// <summary>
        /// The headline number for a score: the chance in percent, or for counting communities the amount (Influence off
        /// upgrades, extra uses, spare seats, viewers sent back (0 = everyone), spaces of slack, Cash on/off a recruit,
        /// inheritances, growth).
        /// </summary>
        public static int Effect(Community community, int points)
        {
            switch (community)
            {
                case Community.CoreFanbase: return CoreFanbaseEncoreChance(points);
                case Community.Staff: return StaffInfluence(points);
                case Community.Content: return ContentEncoreChance(points);
                case Community.Boomer: return BoomerRetireChance(points);
                case Community.Millennial: return InheritanceAmount(points);
                case Community.GenZ: return GenZGrowth(points);
                case Community.Mods:
                case Community.Marketers:
                case Community.Voyeurs: return ExtraUses(community, points);
                case Community.Gatekeepers:
                {
                    int sent = GatekeeperSendBack(points);
                    return sent == int.MaxValue ? 0 : sent;
                }
                case Community.HypeMen: return HypeSeats(points);
                case Community.Lurkers: return LurkersPerSpace(points);
                case Community.Trolls: return points > 0 ? TrollArmyCashPerRisk : 0;
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
                case Community.Boomer:
                case Community.Content:
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
