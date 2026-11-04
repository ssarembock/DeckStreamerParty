namespace DeckParty.Core
{
    /// <summary>Why a guest left the party.</summary>
    public enum LeaveReason : byte
    {
        Evicted = 0,    // Out for the rest of the night.
        SentOutside = 1,// Back in the waiting line; may arrive again tonight.
        Replaced = 2,   // Swapped out by Bombardiro or Distracted Boyfriend; out for the night.
        Followed = 3,   // Overly Attached Girlfriend leaving with her partner.
        Gone = 4,       // Removed from the Squad for good (This Is Fine Dog).
    }

    /// <summary>Things that happened while executing a command, in order. The UI turns these into log lines, sounds and animation.</summary>
    public enum GameEventType : byte
    {
        NightStarted = 0,      // A = night
        GuestArrived = 1,      // Guest, A = source guest id (-1 = front door), B = slot
        GuestRemoved = 2,      // Guest, A = actor id (-1 = none), B = LeaveReason
        GuestRejected = 3,     // Guest
        GuestPeeked = 4,       // Guest, A = actor id
        GuestScored = 5,       // Guest, A = pop, B = cash, C = 1 for end-of-party scoring
        AbilityUsed = 6,       // Guest = actor, A = target A, B = target B, C = option
        EveryoneOutside = 7,   // Guest = actor, A = count
        PoliceRaid = 8,        // Deplatformed. A = net Deplatform Risk
        FireMarshal = 9,       // Server Down. A = occupancy, B = capacity
        PartyEnded = 10,       // A = pop, B = cash, C = diamonds
        CashShortfall = 11,    // A = unpaid cash, B = penalty
        GuestBlamed = 12,      // Guest (temporary ban)
        GuestPurchased = 13,   // Guest, A = cost
        HouseExpanded = 14,    // A = new capacity, B = cost
        Victory = 15,          // A = night
        Defeat = 16,
        GuestGained = 17,      // Guest, A = pop, B = cash (tonight's card bonus)
        PartyPotGained = 18,   // Guest = source, A = pop, B = cash (paid straight into tonight's take)
        TroubleAdded = 19,     // Guest, A = amount
        TroubleRemoved = 20,   // Guest, A = amount
        ThisIsFine = 21,       // Guest = dog, A = Deplatform Risk cleared
        Infected = 22,         // Guest = infected, A = source id
        Attached = 23,         // Guest = Overly Attached Girlfriend, A = partner id
        StatsCopied = 24,      // Guest = NPC Wojak, A = source id, B = pop, C = cash
        TemporaryCopy = 25,    // Guest = new copy
        ShieldBlocked = 26,    // Guest = Bad Luck Brian, A = new permanent bonus
        PermanentChange = 27,  // Guest, A = delta, B = new permanent bonus
        Struck = 28,           // Guest = target, A = strike number
        Transformed = 29,      // Guest = target, A = new kind
        PositionsSwapped = 30, // A = id, B = id
        PrintedExchanged = 31, // A = id, B = id
        MoodChosen = 32,       // Guest = Pepe, A = mood
        Invested = 33,         // A = pop, B = cash
        InvestmentPaid = 34,   // A = pop, B = cash
        MovedToBack = 35,      // Guest
        ImmunityGranted = 36,  // Guest
        Converted = 37,        // Guest, A = mode
        Zeroed = 38,           // Guest
        Doubled = 39,          // Guest, A = mode
        AttractionRevealed = 40, // Guest = Distracted Boyfriend, A = count
        AttractionResolved = 41, // A = replaced id (-1 = skipped), B = chosen id
        TemporaryCopiesLeft = 42,// A = count

        // Streamer powers
        PowerUsed = 43,        // A = streamer, B = Cash paid
        HeatApplied = 44,      // A = Deplatform Risk from rivals' raids at the start of this stream
        ShieldApplied = 45,    // A = Deplatform Risk ignored this stream (Smoke Bomb)
        GiveawayActive = 46,   // A = extra Subs per viewer this stream
        PalCaught = 47,        // Guest = the viewer caught from the shop
        SoftLanding = 48,      // A = Gifted Subs banked despite being deplatformed
        StreakBonus = 49,      // A = bonus Subs, B = streak length
        FreeActionUsed = 50,   // Guest = the viewer whose action refreshed (Double Move)
        XRayActive = 51,       // A = how many queued viewers are visible
        RaidReceived = 52,     // A = Deplatform Risk added to your next stream, B = raider's player index
        TaxPaid = 53,          // A = Cash paid, B = taxer's player index
        TaxReceived = 54,      // A = Cash received, B = payer's player index
        DoubleMoveActive = 55, // A = free action refreshes this stream

        // V3 audience classics
        InterviewGiven = 56,   // Guest = Billionaire Benefactor, A = Cash banked, B = viewers whose Subs dropped to 0
        CashBoosted = 57,      // Guest, A = delta, B = new permanent Cash bonus
        ScoredNow = 58,        // Guest = viewer scored mid-stream (banked), A = Subs, B = Cash, C = source viewer id
        MoodFlipped = 59,      // Guest = Hate-Watcher, A = 1 now hate-watching (risky), 0 now harmless

        // V4 communities
        CommunityBonus = 60,   // Guest, A = Community, B = amount. Staff: Cash cost waived (B). Content: scores twice.
                               // Finance Bros: Cash reward doubled (B = extra Cash). Doomsdayers: a rival's curse, scores nothing.
        ServerRestored = 61,   // Techspertise brought the server back, the stream still scores. A = chance %, B = occupancy, C = capacity
        TrollsPaid = 62,       // Deplatformed, but the Trolls pay out. A = Gifted Subs, B = Cash
        DoomApplied = 63,      // A = rival Doomsdayers points on this stream, B = chance per viewer (%)
        CommunityReward = 64,  // Stream-wide community reward when scored (Boomer, Millennial, Zoomer). A = Community, B = Subs, C = Cash
                               // (CommunityBonus with A = ArmchairActivists: that Armchair Activist scored nothing;
                               // A = Clout: B = extra Gifted Subs. C = 2 on anything rolled in the Memetics Division repeat)

        // V4.2 communities
        MemeticsRepeat = 65,   // Memetics Division: the whole stream scores a second time. A = chance %
        BotsSent = 66,         // Your Bots community sent Bots to the next player's next stream. A = count, B = chance per Bots viewer (%)
        BotsArrived = 67,      // A rival's Bots joined your Audience for this stream. A = count

        // V5 communities
        ActionRefreshed = 68,  // Gatekeepers / Marketers / Voyeurs: the action can be used again. Guest = actor, A = Community, B = extra uses left
        BoomerRetired = 69,    // A Boomer left your Audience for good after the stream. Guest = the Boomer (no longer owned)
        Inheritance = 70,      // Millennials: a Millennial inherited from a Boomer. Guest = Millennial, A = Cash gained, B = new permanent Cash bonus
        GenZGrowth = 71,       // Gen-Z: a viewer grew for good. Guest, A = Subs gained, B = Cash gained
        ModsShield = 72,       // Mods: A = 1: your next stream will start with 1 Risk cancelled (B = chance %); A = 2: applied as this stream starts
        ShopPerk = 73,         // A stream earned a shop effect. A = Community (Boomer, ShopControl, Saboteurs), B = amount
        SentBack = 74,         // Junior PR Manager / PR Manager sent random viewers back to the queue. Guest = actor, A = count sent
    }

    public struct GameEvent
    {
        public GameEventType Type;
        public int GuestId;
        public GuestType Guest;
        public int A;
        public int B;
        public int C;

        public override string ToString() => $"{Type} guest={GuestId}:{Guest} a={A} b={B} c={C}";
    }
}
