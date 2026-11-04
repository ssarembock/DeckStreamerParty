namespace DeckParty.Core
{
    /// <summary>
    /// All 83 guest types: the 82 viewer cards (44 in the current roster, the rest put aside; see
    /// <see cref="GuestCatalog.IsPutAside"/>) and the Bot token the Bots community sends. The numeric values are serialized
    /// (saves and network packets), so only ever append new entries at the end. Bump <see cref="StateSerializer"/>'s format
    /// version if you renumber. Identifiers follow the current English names; the Game Database migrates art stored under
    /// older names (see <see cref="GuestCatalog.RenamedFrom"/>).
    /// </summary>
    public enum GuestType : byte
    {
        // --- The audience classics (renamed in V3) ---
        LoyalFan = 0,
        Whale = 1,
        IneffectiveTroll = 2,
        Producer = 3,
        ChatAdmin = 4,
        Normie = 5,
        MildlyRiskyCollaborator = 6,
        JuniorPrManager = 7,
        DieHardFan = 8,
        MarketingManager = 9,

        // --- Meme guests 1-10 ---
        DegenDog = 10,
        Trollface = 11,
        Pepe = 12,
        Gigachad = 13,
        ThisIsFineDog = 14,
        Wojak = 15,
        GrumpyCat = 16,
        NyanCat = 17,
        StonksGuy = 18,
        Cheems = 19,

        // --- Meme guests 11-20 ---
        ChadVsVirgin = 20,
        Doomer = 21,
        NpcWojak = 22,
        ForeverAlone = 23,
        MeGusta = 24,
        KeyboardCat = 25,
        BongoCat = 26,
        PopCat = 27,
        BigChungus = 28,
        UgandanKnuckles = 29,

        // --- Meme guests 21-30 ---
        OverlyAttachedGirlfriend = 30,
        DisasterGirl = 31,
        HideThePainHarold = 32,
        SuccessKid = 33,
        BadLuckBrian = 34,
        TungTungTungSahur = 35,
        TralaleroTralala = 36,
        BombardiroCrocodilo = 37,
        BallerinaCappuccina = 38,
        SkibidiToilet = 39,

        // --- Meme guests 31-40 ---
        SigmaMale = 40,
        Karen = 41,
        Rickroll = 42,
        DistractedBoyfriend = 43,
        Drakeposting = 44,
        ConfusedMathLady = 45,
        SideEyeChloe = 46,
        RollSafe = 47,
        DistraughtCat = 48,
        DancingBaby = 49,

        // --- Diamond guests (formerly Stars, renamed in V3) ---
        BigBoomer = 50,
        ClippoTheClanker = 51,
        BiggerStar = 52,
        PrecociousYouth = 53,
        InternetStarpower = 54,
        PlatformOwner = 55,
        MainstreamCelebrity = 56,
        SeniorMarketingManager = 57,
        Evangelist = 58,

        // --- V3: the rest of the audience classics ---
        InfluentialTroll = 59,
        CybersecurityEngineer = 60,
        PaidSponsorship = 61,
        PromoCodeUser = 62,
        AccountManager = 63,
        CryptoEnthusiast = 64,
        GenerousDonator = 65,
        PrManager = 66,
        HighlyRiskyCollaborator = 67,
        ChatArbitrator = 68,
        BotDetector = 69,
        RiskyOrbiter = 70,
        SocialMediaManager = 71,
        BillionaireBenefactor = 72,
        TechAdmin = 73,
        OnlineScandal = 74,
        Gatekeeper = 75,
        BingeBunny = 76,
        HateWatcher = 77,
        Affiliate = 78,
        MasterOfCurrency = 79,
        ExecutivePr = 80,
        GenZWithJob = 81,

        // --- V4.2: not a viewer card. Rival Bots communities send these into your next stream (takes a space, earns nothing). ---
        Bot = 82,
    }

    /// <summary>
    /// The old character groups ("families"). No longer used by the game: viewers belong to <see cref="Community"/>
    /// groups instead (Doge and Bongo Cat now use those). Kept, with every viewer's old value, in case they come back.
    /// </summary>
    public enum GuestCategory : byte
    {
        Community = 0,
        Animal = 1,
        Wojak = 2,
        RageComic = 3,
        Reaction = 4,
        Brainrot = 5,
        Diamond = 6,
        Tbd = 7,
    }

    /// <summary>The mechanical effect of a guest's once-per-party action.</summary>
    public enum AbilityKind : byte
    {
        None = 0,
        Fetch,      // Choose a waiting guest to come in.
        Boot,       // Remove one guest for the rest of the party (can be itself).
        Reset,      // Send everyone back outside so they can arrive again.
        Troll,      // Trollface: send a guest back outside; bonus for repeat victims.
        Mood,       // Pepe: pick Happy, Sad or Angry.
        Inspect,    // Grumpy Cat: look at the next 3, reject one, order the rest.
        Invest,     // Stonks Guy: stake Gifted Subs or Cash on the party succeeding.
        PlayOff,    // Keyboard Cat: evict a guest for Subs or Cash.
        Mog,        // Chad vs Virgin: compare printed Subs and transfer 1 permanently.
        Arson,      // Disaster Girl: +1 Deplatform Risk and +3 Subs to another guest.
        Reroll,     // Success Kid: reroll the last random arrival, or peek at the next guest.
        Strike,     // Tung Tung Tung Sahur: three strikes and you're out.
        SwapOrder,  // Tralalero Tralala: swap two guests' places in the arrival order.
        Bombard,    // Bombardiro Crocodilo: replace up to 3 guests with random waiting ones.
        Pirouette,  // Ballerina Cappuccina: two guests exchange printed Subs.
        Refund,     // Karen: remove a guest and get half their cost as Subs.
        Rickroll,   // Rickroll: a guest temporarily becomes another character.
        Drake,      // Drakeposting: zero one guest, double another.
        Convert,    // Confused Math Lady: convert a guest's Subs to Cash or back.
        SideEye,    // Side Eye Chloe: move the next guest to the back of the line.
        Immunity,   // Roll Safe: a guest ignores further ability effects tonight.
        Peek,       // Cybersecurity Engineer / Social Media Manager: look at the next guest, then let them in or deny them.
        Photograph, // Crypto Enthusiast: score another guest right now (banked immediately).
        Calm,       // Chat Arbitrator: clear Deplatform Risk from every risky guest for the rest of the party.
        BootPair,   // Bot Detector: remove two guests sitting next to each other.
        Interview,  // Billionaire Benefactor: everyone's Subs drop to 0; bank 1 Cash per Billionaire Benefactor.
        Style,      // Affiliate: a guest permanently earns +1 Cash.
        SendBack,   // Junior PR Manager (1, 2, 3... random viewers, growing per use) / PR Manager (5): random viewers back to the queue.
    }

    /// <summary>Effect families for actions. Dancing Baby rewards two in a row from the same family.</summary>
    public enum ActionCategory : byte
    {
        None = 0,
        Recruit,
        Remove,
        Modify,
        Shuffle,
        Scout,
        Economy,
    }

    /// <summary>What an action asks the player to pick from the guests at the party.</summary>
    public enum TargetMode : byte
    {
        None = 0,       // No guest to pick.
        AnyAttendee,    // One guest, may be the actor.
        OtherAttendee,  // One guest other than the actor.
        TwoAttendees,   // Two different guests (either may be the actor).
        UpToThree,      // One to three guests other than the actor.
        WaitingGuest,   // A guest from the waiting line (picker).
        Inspect,        // Grumpy Cat's look-ahead (picker over the next guests).
        AdjacentPair,   // Two guests next to each other in chat (either may be the actor).
    }
}
