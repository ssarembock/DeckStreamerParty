namespace DeckParty
{
    /// <summary>
    /// Standalone names for game mechanics (tooltips, rules). Resources are shown as icons in the game (see GameIcons); these
    /// names are what hovering an icon says. The rules code keeps its original internal names
    /// (Pop, Trouble, Star, Rolodex, House...). Inside sentences each language writes its own wording in the string
    /// tables, so grammar works; edit the "term.*" keys in AudienceCapture_Strings.csv to rename a mechanic.
    /// </summary>
    public static class Terms
    {
        /// <summary>What viewers bring in during a stream (was Pop).</summary>
        public static string Subs => Loc.Get("term.subs");
        /// <summary>The banked currency you recruit viewers with.</summary>
        public static string GiftedSubs => Loc.Get("term.gifted_subs");
        public static string Cash => Loc.Get("term.cash");

        /// <summary>Was Trouble (then Swatting Risk, then Deplatform Risk): Deplatform Risk.</summary>
        public static string Risk => Loc.Get("term.risk");
        /// <summary>Was the police raid (then SWATTED!, then PLATFORM BAN!): DEPLATFORMED!, the stream ending.</summary>
        public static string Raid => Loc.Get("term.raid");
        /// <summary>Was "taking the blame" after a raid.</summary>
        public static string Ban => Loc.Get("term.ban");

        /// <summary>Was Star.</summary>
        public static string Diamond => Loc.Get("term.diamond");
        public static string Diamonds => Loc.Get("term.diamonds");

        /// <summary>Was Rolodex: every viewer card you own.</summary>
        public static string Audience => Loc.Get("term.audience");

        /// <summary>Was the house size.</summary>
        public static string SubCap => Loc.Get("term.sub_cap");
        public static string RaiseSubCap => Loc.Get("term.raise_sub_cap");
        /// <summary>Was the fire marshal shutting the party down: Server Down (chat over the Sub Cap).</summary>
        public static string Overload => Loc.Get("term.overload");

        /// <summary>A preset scenario (its viewer shop).</summary>
        public static string CharacterSet => Loc.Get("term.character_set");

        public static string DiamondCount(int count) => Loc.Get("term.diamond_count", "count", count);
    }
}
