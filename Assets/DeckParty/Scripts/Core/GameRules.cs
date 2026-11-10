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

        /// <summary>Copy limit per player for non-Diamond guest types (Diamonds are unlimited). Starting viewers count.</summary>
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

        /// <summary>Highest printed Cash permanent bonuses can raise a viewer to (Affiliate's Checkout Code, Gen Z with Job).</summary>
        public const int PermanentCashCap = 9;

        /// <summary>Gifted Subs lost for every Cash a party cannot pay.</summary>
        public const int CashShortfallPopPenalty = 7;

        /// <summary>Most viewer cards in one chat row: past this the chat starts a new row (more only once cards hit their minimum size).</summary>
        public const int HouseColumns = 8;

        // --- Meme guest tuning ---
        public const int DogeMaxBonus = 5;
        public const int GigachadSoloBonus = 3;
        public const int GigachadCrowdPenalty = 2;
        public const int WojakPerSufferer = 2;
        public const int WojakMaxBonus = 6;
        public const int NyanMaxPerArrival = 6;
        public const int StonksInvestPop = 2;
        public const int StonksInvestCash = 1;
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
        /// <summary>Promo Code User: Cash when chat is full at scoring time (2026-11-10; it was +5 Subs).</summary>
        public const int PromoCodeFullCash = 3;
        /// <summary>Master of Currency ($) and Executive PR (Subs): bonus per viewer still carrying Deplatform Risk.</summary>
        public const int PerRiskyViewerBonus = 2;
        /// <summary>PR Manager's Damage Control: random viewers sent back to the queue.</summary>
        public const int PrManagerSendBack = 5;

        // --- Group-scoring cards (viewer card edits, 2026-11-08) ---
        // The copies in chat score one total together, read from a table by how many are in chat (1 to 5), instead of
        // each copy scoring on its own. The original game's Dancer scored D squared each; Dancing Jen scores D squared in all.
        static readonly int[] s_DancingJenClout = { 1, 4, 9, 16, 25 };
        static readonly int[] s_CryptoEnthusiastCash = { 0, 1, 4, 9, 16 };

        /// <summary>Copy limit (and shared shop stock) of the group-scoring cards, so the table's last step can be reached.</summary>
        public const int GroupCardMaxCopies = 5;

        /// <summary>Dancing Jen: Clout (Subs) the whole group scores with this many Jens in chat (25 at most).</summary>
        public static int DancingJenClout(int copies) => GroupStep(s_DancingJenClout, copies);

        /// <summary>Crypto Enthusiast: Cash the whole group scores with this many in chat (nothing alone, 16 at most).</summary>
        public static int CryptoEnthusiastCash(int copies) => GroupStep(s_CryptoEnthusiastCash, copies);

        /// <summary>The table as compact text for card stat lines ("1/4/9/16/25").</summary>
        public static string DancingJenSteps => string.Join("/", s_DancingJenClout);

        public static string CryptoEnthusiastSteps => string.Join("/", s_CryptoEnthusiastCash);

        static int GroupStep(int[] table, int copies) => copies <= 0 ? 0 : table[Math.Min(copies, table.Length) - 1];

        /// <summary>Remove a Viewer (shop): the first removal costs this much Cash, every one after costs 1 more, up to the max.</summary>
        public const int FirstRemovalCost = 2;
        public const int MaxRemovalCost = 12;
        /// <summary>Remove a Viewer can't take your Audience below this many viewer cards.</summary>
        public const int MinAudience = 5;

        public static int RemovalCost(int removalsBought) => Math.Min(FirstRemovalCost + Math.Max(0, removalsBought), MaxRemovalCost);

        /// <summary>
        /// When true, guest fees are paid from banked Cash before the Gifted Subs penalty applies.
        /// When false, only the party's own Cash income can cover fees.
        /// </summary>
        public static readonly bool FeesDrawFromBank = true;

        public static int ExpansionCost(int expansionsBought)
        {
            return Math.Min(FirstExpansionCost + Math.Max(0, expansionsBought), MaxExpansionCost);
        }
    }
}
