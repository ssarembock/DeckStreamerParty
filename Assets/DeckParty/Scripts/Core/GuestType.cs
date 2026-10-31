namespace DeckParty.Core
{
    /// <summary>
    /// All 59 guest types. The numeric values are serialized (saves and network packets), so only ever
    /// append new entries at the end. Bump <see cref="StateSerializer"/>'s format version if you renumber.
    /// </summary>
    public enum GuestType : byte
    {
        // --- Community (the renamed classics) ---
        LoyalFan = 0,
        Whale = 1,
        BotFarm = 2,
        SocialMediaManager = 3,
        Moderator = 4,
        AutoMod = 5,
        Collaborator = 6,
        Hodl = 7,
        Vips = 8,
        Parasocialite = 9,

        // --- Meme guests 1-10 ---
        Doge = 10,
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
        CryingCat = 48,
        DancingBaby = 49,

        // --- Diamond guests (formerly Stars) ---
        DramaChannel = 50,
        DivaStreamer = 51,
        RaidLeader = 52,
        MysteryVTuber = 53,
        WholesomeIcon = 54,
        PlatformAdmin = 55,
        MegaInfluencer = 56,
        BrandSponsor = 57,
        Algorithm = 58,
    }

    /// <summary>Character families. Doge counts how many different ones are at the party; Bongo Cat buffs Animals.</summary>
    public enum GuestCategory : byte
    {
        Community = 0,
        Animal = 1,
        Wojak = 2,
        RageComic = 3,
        Reaction = 4,
        Brainrot = 5,
        Diamond = 6,
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
        Arson,      // Disaster Girl: +1 Swatting Risk and +3 Subs to another guest.
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
    }
}
