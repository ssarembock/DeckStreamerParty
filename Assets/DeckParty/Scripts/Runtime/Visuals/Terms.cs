namespace DeckParty
{
    /// <summary>
    /// Every player-facing name for a game mechanic, in one place. The rules code keeps its original internal
    /// names (Pop, Trouble, Star, Rolodex, House...); change the wording here to re-theme the whole UI.
    /// </summary>
    public static class Terms
    {
        /// <summary>What guests bring in during a party (was Pop).</summary>
        public const string Subs = "Subs";
        /// <summary>The banked currency you recruit guests with.</summary>
        public const string GiftedSubs = "Gifted Subs";
        public const string Cash = "Cash";
        public const string CashSymbol = "$";

        /// <summary>Was Trouble.</summary>
        public const string Risk = "Swatting Risk";
        /// <summary>Was the police raid.</summary>
        public const string Raid = "SWATTED!";
        public const string RaidTeam = "the SWAT team";
        /// <summary>Was "taking the blame" after a raid.</summary>
        public const string Ban = "Temporary Ban";

        /// <summary>Was Star.</summary>
        public const string Diamond = "Diamond";
        public const string Diamonds = "Diamonds";

        /// <summary>Was Rolodex: every guest card you own.</summary>
        public const string Squad = "Squad";

        /// <summary>Was the house size.</summary>
        public const string SubCap = "Sub Cap";
        public const string RaiseSubCap = "Raise Sub Cap";
        /// <summary>Was the fire marshal shutting the party down.</summary>
        public const string Overload = "SUB CAP EXCEEDED!";

        /// <summary>A preset scenario (its guest shop).</summary>
        public const string CharacterSet = "Character Set";

        public static string Plural(int count, string singular, string plural) => count == 1 ? singular : plural;

        public static string DiamondCount(int count) => $"{count} {Plural(count, Diamond, Diamonds)}";
    }
}
