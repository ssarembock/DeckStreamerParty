using System;

namespace DeckParty.Core
{
    /// <summary>
    /// Every tunable number in the Deck Party rule set lives here so balance changes are one-line edits.
    /// </summary>
    public static class GameRules
    {
        public const int DefaultNights = 25;
        public const int StartingCapacity = 5;
        public const int MaxCapacity = 34;

        public const int FirstExpansionCost = 2;
        public const int MaxExpansionCost = 12;

        /// <summary>Net Platform Ban Risk at (or above) this value gets the stream banned (internally: a raid).</summary>
        public const int RaidTroubleThreshold = 3;

        /// <summary>Diamonds that must attend one successful party to win.</summary>
        public const int DiamondsToWin = 4;

        /// <summary>Copy limit per player for non-Diamond guest types (Diamonds are unlimited). Starting viewers count.</summary>
        public const int MaxCopiesPerGuest = 4;

        /// <summary>
        /// Copies of each non-Diamond viewer the shop holds for the whole match, shared by every player: a copy one player
        /// buys is gone for everyone. Starting viewers don't come from the shop. In single player this never limits more
        /// than <see cref="MaxCopiesPerGuest"/> already does.
        /// </summary>
        public const int SharedStockPerGuest = 4;

        /// <summary>Highest printed Cash permanent bonuses can raise a viewer to (Affiliate's Checkout Code, Gen Z with Job).</summary>
        public const int PermanentCashCap = 9;

        /// <summary>Gifted Subs lost for every Cash a party cannot pay.</summary>
        public const int CashShortfallPopPenalty = 7;

        /// <summary>Guests are shown in rows of this many.</summary>
        public const int HouseColumns = 9;

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
        public const int ForeverAloneSoloBonus = 6;
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
        /// <summary>Comedian with a Podcast: Subs when chat is full at scoring time.</summary>
        public const int ComedianFullBonus = 5;
        /// <summary>Master of Currency ($) and Executive PR (Subs): bonus per viewer still carrying Platform Ban Risk.</summary>
        public const int PerRiskyViewerBonus = 2;

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
