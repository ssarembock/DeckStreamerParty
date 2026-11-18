using System;

namespace DeckParty.Core
{
    /// <summary>
    /// Every tunable number in the Deck Party rule set lives here so balance changes are one-line edits.
    /// </summary>
    public static class GameRules
    {
        public const int DefaultNights = 25;
        /// <summary>Sub Cap every season starts with (6 since 2026-11-10, to speed the game up a little; it was 5).</summary>
        public const int StartingCapacity = 6;
        /// <summary>
        /// The highest Sub Cap one player's chat can reach in a solo season (40 since the 2026-11-18 UI updates: the chat's 4
        /// rows of 10; it was 30, and 34 before that). The Hype Men's spare seats come on top of it, in one extra row
        /// (<see cref="HypeRowSeats"/>). Teams have <see cref="TeamMaxCapacity"/> a player. Use <see cref="MaxCapacityFor"/>
        /// (or PartyEngine.MaxCapacity) for the limit that applies to a season.
        /// </summary>
        public const int MaxCapacity = HouseColumns * HouseMaxRows;

        /// <summary>Teams (2v2, co-op): the highest Sub Cap one player's side of the chat can reach (50, 5 rows of 10; it was 40).</summary>
        public const int TeamMaxCapacity = HouseColumns * TeamHouseMaxRows;

        /// <summary>
        /// The Hype Men's spare seats never count against <see cref="MaxCapacity"/> (2026-11-18): a chat at (or near) its
        /// limit shows them in one extra row under the full ones, so a chat can briefly hold up to this many seats past it.
        /// </summary>
        public const int HypeRowSeats = HouseColumns;

        /// <summary>The seat limit of one player's chat: <see cref="MaxCapacity"/> solo, <see cref="TeamMaxCapacity"/> a player in a team.</summary>
        public static int MaxCapacityFor(bool team) => team ? TeamMaxCapacity : MaxCapacity;

        public const int FirstExpansionCost = 2;

        /// <summary>Net Deplatform Risk at (or above) this value gets the stream deplatformed (internally: a raid).</summary>
        public const int RaidTroubleThreshold = 3;

        /// <summary>
        /// Teams (2v2, co-op Endless): the two players share one Risk count for the whole stream, so they get a higher limit.
        /// Use <see cref="PartyEngine.RiskLimit"/> for the limit that applies to a season.
        /// </summary>
        public const int TeamRiskLimit = 4;

        /// <summary>The most Risk pips any season shows (the team limit).</summary>
        public const int MaxRiskLimit = TeamRiskLimit;

        /// <summary>Trophies that must be in one scored stream to win: single player (Puzzles, Versus the AI). Each season
        /// keeps its own goal (<see cref="PlayerState.TrophiesToWin"/>, read through <see cref="PartyEngine.TrophiesToWin"/>).</summary>
        public const int DiamondsToWin = 4;

        /// <summary>Competitive multiplayer (online and couch) wins with this many Trophies in one stream (2026-11-18).</summary>
        public const int CompetitiveTrophiesToWin = 3;

        /// <summary>Streams a competitive multiplayer match lasts by default (online lobbies and couch play; 2026-11-18).</summary>
        public const int CompetitiveStreams = 20;

        /// <summary>
        /// Copy limit per player for non-Diamond guest types (Diamonds are unlimited). Starting viewers count, except for
        /// Loyal Fan and Fish (in every shop since 2026-11-16): you can buy this many more of them on top of the starting copies.
        /// </summary>
        public const int MaxCopiesPerGuest = 4;

        /// <summary>
        /// The smallest stack of each non-Diamond viewer the shop holds for the whole match, shared by every player: a copy
        /// one player buys is gone for everyone. Starting viewers don't come from the shop. In single player this never
        /// limits more than <see cref="MaxCopiesPerGuest"/> already does.
        /// </summary>
        public const int SharedStockPerGuest = 4;

        /// <summary>
        /// Bigger matches get bigger stacks (2026-11-10): <see cref="StockPerPlayer"/> copies per player, so 6 for three
        /// players, 8 for four and so on (never below <see cref="SharedStockPerGuest"/>). The number of different viewers in
        /// the shop doesn't change.
        /// </summary>
        public const int StockPerPlayer = 2;

        /// <summary>Copies in each shared stack for a match of this many players.</summary>
        public static int SharedStock(int players) => Math.Max(SharedStockPerGuest, StockPerPlayer * Math.Max(1, players));

        /// <summary>Highest printed Influence permanent bonuses can raise a viewer to (Affiliate's Checkout Code, Gen Z with Job).</summary>
        public const int PermanentInfluenceCap = 9;

        /// <summary>Cash lost for every Influence a party cannot pay.</summary>
        public const int InfluenceShortfallCashPenalty = 7;

        /// <summary>Most viewers in one chat row (2026-11-18: 10, it was 8). The chat never scrolls: the avatars shrink to fit.</summary>
        public const int HouseColumns = 10;

        /// <summary>Most rows of Sub Cap seats in one player's chat (with <see cref="HouseColumns"/>: 40 seats, <see cref="MaxCapacity"/>; it was 3).</summary>
        public const int HouseMaxRows = 4;

        /// <summary>Teams: most rows of Sub Cap seats on one player's side of the chat (with <see cref="HouseColumns"/>: 50 seats, <see cref="TeamMaxCapacity"/>; it was 4).</summary>
        public const int TeamHouseMaxRows = 5;

        /// <summary>Most rows of Sub Cap seats in one player's chat (or side of a team's chat). The Hype Men's spare seats can add one more.</summary>
        public static int HouseMaxRowsFor(bool team) => team ? TeamHouseMaxRows : HouseMaxRows;

        // --- Meme guest tuning ---
        public const int DogeMaxBonus = 5;
        public const int GigachadSoloBonus = 3;
        public const int GigachadCrowdPenalty = 2;
        public const int WojakPerSufferer = 2;
        public const int WojakMaxBonus = 6;
        public const int NyanMaxPerArrival = 6;
        public const int StonksInvestCash = 2;
        public const int StonksInvestInfluence = 1;
        public const int StonksMultiplier = 3;
        public const int ForeverAloneSoloBonus = 10;
        public const int BigChungusCapacityBase = 10;
        public const int BigChungusMaxBonus = 5;
        public const int HaroldMaxBonus = 5;
        public const int SigmaMaxBonus = 8;
        public const int KarenMaxRefund = 8;
        public const int DancingBabyCombo = 3;
        public const int DancingBabyMaxBonus = 9;
        public const int GrumpyInspectCount = 3;
        public const int AttractionRevealCount = 3;

        // --- Audience classics tuning ---
        /// <summary>Promo Code User: Influence when chat is full at scoring time (2026-11-10; it was +5 Cash).</summary>
        public const int PromoCodeFullInfluence = 3;
        /// <summary>Master of Currency ($) and Executive PR (Cash): bonus per viewer still carrying Deplatform Risk.</summary>
        public const int PerRiskyViewerBonus = 2;
        /// <summary>
        /// PR Manager's Damage Control when it isn't a Gatekeeper (only if you take it out of the Gatekeepers in the Game
        /// Database): random viewers sent back to the queue. As a Gatekeeper it sends 3, 6 or everyone else (Communities.GatekeeperSendBack).
        /// </summary>
        public const int PrManagerSendBack = 5;

        // --- Group-scoring cards (viewer card edits, 2026-11-08) ---
        // The copies in chat score one total together, read from a table by how many are in chat (1 to 5), instead of
        // each copy scoring on its own. The original game's Dancer scored D squared each; Dancing Jen scores D squared in all.
        static readonly int[] s_DancingJenCash = { 1, 4, 9, 16, 25 };
        static readonly int[] s_CryptoEnthusiastInfluence = { 0, 1, 4, 9, 16 };

        /// <summary>Copy limit (and shared shop stock) of the group-scoring cards, so the table's last step can be reached.</summary>
        public const int GroupCardMaxCopies = 5;

        /// <summary>Dancing Jen: Cash (Cash) the whole group scores with this many Jens in chat (25 at most).</summary>
        public static int DancingJenCash(int copies) => GroupStep(s_DancingJenCash, copies);

        /// <summary>Crypto Enthusiast: Influence the whole group scores with this many in chat (nothing alone, 16 at most).</summary>
        public static int CryptoEnthusiastInfluence(int copies) => GroupStep(s_CryptoEnthusiastInfluence, copies);

        /// <summary>The table as compact text for card stat lines ("1/4/9/16/25").</summary>
        public static string DancingJenSteps => string.Join("/", s_DancingJenCash);

        public static string CryptoEnthusiastSteps => string.Join("/", s_CryptoEnthusiastInfluence);

        static int GroupStep(int[] table, int copies) => copies <= 0 ? 0 : table[Math.Min(copies, table.Length) - 1];

        /// <summary>Remove a Viewer (shop): the first removal costs this much Influence (5 since the small changes of 2026-11-18; it was 3, and 2 before that).</summary>
        public const int FirstRemovalCost = 5;
        /// <summary>Remove a Viewer: every removal after the first costs this much more (5 since the small changes of 2026-11-18; it was 2, and 1 before that). No maximum.</summary>
        public const int RemovalCostStep = 5;
        /// <summary>Remove a Viewer can't take your Audience below this many viewer cards.</summary>
        public const int MinAudience = 5;

        /// <summary>Remove a Viewer's price after <paramref name="removalsBought"/> removals: 5, 10, 15, 20... Influence.</summary>
        public static int RemovalCost(int removalsBought) => FirstRemovalCost + RemovalCostStep * Math.Max(0, removalsBought);

        // --- Endless mode (2026-11-16, additionalgamemodes.md) ---
        /// <summary>Endless: streams a run starts with (Trophies buy more). 20 since 2026-11-18, for a harder run; it was 25.</summary>
        public const int EndlessStartingStreams = 20;
        /// <summary>
        /// Endless: Trophies the first extra shop viewer costs; each one after costs this much more. (Extra streams used
        /// the same prices until 2026-11-18; they follow <see cref="EndlessStreamCost"/> now.)
        /// </summary>
        public const int EndlessFirstTrophyCost = 2;
        public const int EndlessTrophyCostStep = 1;
        /// <summary>Endless: viewers offered when adding a viewer to the shop (pick one).</summary>
        public const int EndlessViewerChoices = 3;

        /// <summary>Endless: Trophies the next shop viewer costs after <paramref name="bought"/> of them (2, 3, 4...).</summary>
        public static int EndlessTrophyCost(int bought) => EndlessFirstTrophyCost + EndlessTrophyCostStep * Math.Max(0, bought);

        /// <summary>
        /// Endless: Trophies the next extra stream costs after <paramref name="bought"/> of them, along the Fibonacci numbers
        /// without the first 1: 1, 2, 3, 5, 8, 13, 21... (2026-11-18, user request; it was 2, then 1 more each time).
        /// </summary>
        public static int EndlessStreamCost(int bought)
        {
            int previous = 1, cost = 1;
            for (int i = 0; i < bought; i++)
            {
                int next = previous + cost;
                // Far past anything a run can afford: stop before int overflows.
                if (next < cost || next > 1_000_000_000) return 1_000_000_000;
                previous = cost;
                cost = next;
            }
            return cost;
        }

        // --- Teams (2v2, co-op) ---
        /// <summary>Players in a team (2v2 and co-op).</summary>
        public const int TeamSize = 2;

        /// <summary>
        /// When true, guest fees are paid from banked Influence before the Cash penalty applies.
        /// When false, only the party's own Influence income can cover fees.
        /// </summary>
        public static readonly bool FeesDrawFromBank = true;

        /// <summary>Raising the Sub Cap: 2 Influence, then 1 more each time, with no maximum (2026-11-18; it stopped at 12).</summary>
        public static int ExpansionCost(int expansionsBought)
        {
            return FirstExpansionCost + Math.Max(0, expansionsBought);
        }
    }
}
