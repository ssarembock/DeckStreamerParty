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
        public const int MaxCapacity = 34;

        public const int FirstExpansionCost = 2;
        public const int MaxExpansionCost = 12;

        /// <summary>Net Deplatform Risk at (or above) this value gets the stream deplatformed (internally: a raid).</summary>
        public const int RaidTroubleThreshold = 3;

        /// <summary>Diamonds that must attend one successful party to win.</summary>
        public const int DiamondsToWin = 4;

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

        /// <summary>Most viewer cards in one chat row: past this the chat starts a new row (more only once cards hit their minimum size).</summary>
        public const int HouseColumns = 8;

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

        /// <summary>Remove a Viewer (shop): the first removal costs this much Influence, every one after costs 1 more, up to the max.</summary>
        public const int FirstRemovalCost = 2;
        public const int MaxRemovalCost = 12;
        /// <summary>Remove a Viewer can't take your Audience below this many viewer cards.</summary>
        public const int MinAudience = 5;

        public static int RemovalCost(int removalsBought) => Math.Min(FirstRemovalCost + Math.Max(0, removalsBought), MaxRemovalCost);

        // --- Endless mode (2026-11-16, additionalgamemodes.md) ---
        /// <summary>Endless: streams a run starts with (Trophies buy more).</summary>
        public const int EndlessStartingStreams = 25;
        /// <summary>Endless: Trophies the first extra stream (and the first extra shop viewer) costs; each one after costs this much more.</summary>
        public const int EndlessFirstTrophyCost = 2;
        public const int EndlessTrophyCostStep = 1;
        /// <summary>Endless: viewers offered when adding a viewer to the shop (pick one).</summary>
        public const int EndlessViewerChoices = 3;

        /// <summary>Endless: Trophies the next purchase costs after <paramref name="bought"/> of the same kind.</summary>
        public static int EndlessTrophyCost(int bought) => EndlessFirstTrophyCost + EndlessTrophyCostStep * Math.Max(0, bought);

        // --- Teams (2v2, co-op) ---
        /// <summary>Players in a team (2v2 and co-op).</summary>
        public const int TeamSize = 2;

        /// <summary>
        /// When true, guest fees are paid from banked Influence before the Cash penalty applies.
        /// When false, only the party's own Influence income can cover fees.
        /// </summary>
        public static readonly bool FeesDrawFromBank = true;

        public static int ExpansionCost(int expansionsBought)
        {
            return Math.Min(FirstExpansionCost + Math.Max(0, expansionsBought), MaxExpansionCost);
        }
    }
}
