namespace DeckParty.Networking
{
    /// <summary>
    /// Which online section the player came from on the multiplayer mode screen (2026-11-17): Online Competitive (Take
    /// Turns, Free For All, 2v2) or Online Co-Op Endless. A host's lobby starts in that section and its mode selector only
    /// offers that section's modes (<see cref="MatchModes.ForCategory"/>); a player who joins sees the lobby's own section.
    /// </summary>
    public static class OnlineCategory
    {
        public const int Competitive = 0;
        public const int Coop = 1;

        /// <summary>The section picked last on the multiplayer mode screen.</summary>
        public static int Current = Competitive;

        /// <summary>The section a mode belongs to.</summary>
        public static int Of(MatchMode mode) => mode == MatchMode.CoopEndless ? Coop : Competitive;

        /// <summary>The mode a new lobby of this section starts with.</summary>
        public static MatchMode DefaultMode(int category) => category == Coop ? MatchMode.CoopEndless : MatchMode.Turns;
    }
}
