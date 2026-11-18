using System;
using System.Collections.Generic;

namespace DeckParty.Core
{
    /// <summary>
    /// Every guest type: the 91 viewer cards (the 55 of the current roster plus the ones put aside, see <see cref="IsPutAside"/>)
    /// and the Bot token. The English names and descriptions here are fallbacks: players see the localized text from the
    /// strings CSV (keys char.&lt;GuestType&gt;.name / .desc / .action).
    /// </summary>
    public static class GuestCatalog
    {
        // ------------------------------------------------------------------ risky variants

        /// <summary>
        /// Risky variants (2026-11-18, user request): a copy of an existing card with +1 Risk, 3 Cash cheaper, a new name
        /// and the original's communities. They follow the original's rules (passive scoring, action, joining effects): the
        /// engine scores them through <see cref="BehavesAs"/>, and the presentation falls back to the original's art and
        /// signature effect until they get their own.
        /// </summary>
        static readonly Dictionary<GuestType, GuestType> s_VariantOf = new Dictionary<GuestType, GuestType>
        {
            { GuestType.BunkerBillionaire, GuestType.BillionaireIsolationist },
            { GuestType.TerminallyOnline, GuestType.ForeverAlone },
            { GuestType.PowerTrippingMod, GuestType.TechAdmin },
            { GuestType.DataBroker, GuestType.SocialMediaManager },
            { GuestType.TabloidReporter, GuestType.OnlineJournalist },
        };

        /// <summary>How much cheaper a risky variant is than its original (in Cash).</summary>
        public const int VariantDiscount = 3;

        /// <summary>The card a risky variant copies (Bunker Billionaire -> Billionaire Isolationist), or false for any other card.</summary>
        public static bool TryGetVariantOf(GuestType type, out GuestType original) => s_VariantOf.TryGetValue(type, out original);

        /// <summary>The card whose rules this one follows: the original for a risky variant, the card itself otherwise.</summary>
        public static GuestType BehavesAs(GuestType type) => s_VariantOf.TryGetValue(type, out var original) ? original : type;

        static readonly GuestInfo[] s_All = Build();

        /// <summary>
        /// Enum names guests had before V3 (and the five renamed in V4.2), so art saved under the old id (Game Database)
        /// follows the character to its new name. Only types whose identifier changed are listed. Note "SocialMediaManager"
        /// was the Producer's old id; the new Social Media Manager is a different viewer.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, GuestType> RenamedFrom = new Dictionary<string, GuestType>
        {
            { "BotFarm", GuestType.IneffectiveTroll },
            { "SocialMediaManager", GuestType.Producer },
            { "Moderator", GuestType.ChatAdmin },
            { "AutoMod", GuestType.Normie },
            { "Collaborator", GuestType.MildlyRiskyCollaborator },
            { "Hodl", GuestType.JuniorPrManager },
            { "Vips", GuestType.DieHardFan },
            { "Parasocialite", GuestType.MarketingManager },
            { "DramaChannel", GuestType.BigBoomer },
            { "DivaStreamer", GuestType.ClippoTheClanker },
            { "RaidLeader", GuestType.BiggerStar },
            { "MysteryVTuber", GuestType.PrecociousYouth },
            { "WholesomeIcon", GuestType.InternetStarpower },
            { "PlatformAdmin", GuestType.PlatformOwner },
            { "MegaInfluencer", GuestType.MainstreamCelebrity },
            { "BrandSponsor", GuestType.SeniorMarketingManager },
            { "Algorithm", GuestType.Evangelist },
            // V4.2
            { "SurpriseGuest", GuestType.Evangelist },
            { "CryingCat", GuestType.DistraughtCat },
            { "Doge", GuestType.DegenDog },
            { "ComedianWithAPodcast", GuestType.PromoCodeUser },
            { "CryptoGuy", GuestType.CryptoEnthusiast },
            // 2026-11-10 (StreamersViewersCommunities.md): the old Whale is Fish (a new Whale took the name), Online Scandal is Online Journalist.
            { "Whale", GuestType.Fish },
            { "OnlineScandal", GuestType.OnlineJournalist },
            // 2026-11-16: the Gatekeeper viewer is the Influential Isolationist (Gatekeepers is a community).
            { "Gatekeeper", GuestType.InfluentialIsolationist },
        };

        // ------------------------------------------------------------------ the roster

        /// <summary>
        /// Viewer cards put aside in V4.2. They keep their code, rules, art and communities, but the current build never uses
        /// them: no shop, preset, Random Mix or Open Lobby offers them and the Game Database's viewer list hides them. Take a
        /// type out of this set to bring it back (2026-11-10: Degen Dog, Doomer and Forever Alone came back).
        /// </summary>
        static readonly HashSet<GuestType> s_PutAside = new HashSet<GuestType>
        {
            GuestType.Trollface, GuestType.Pepe, GuestType.Gigachad, GuestType.ThisIsFineDog, GuestType.Wojak, GuestType.GrumpyCat,
            GuestType.NyanCat, GuestType.StonksGuy, GuestType.Cheems, GuestType.ChadVsVirgin, GuestType.NpcWojak,
            GuestType.MeGusta, GuestType.KeyboardCat, GuestType.BongoCat, GuestType.PopCat, GuestType.BigChungus,
            GuestType.UgandanKnuckles, GuestType.OverlyAttachedGirlfriend, GuestType.DisasterGirl, GuestType.HideThePainHarold,
            GuestType.SuccessKid, GuestType.BadLuckBrian, GuestType.TungTungTungSahur, GuestType.TralaleroTralala,
            GuestType.BombardiroCrocodilo, GuestType.BallerinaCappuccina, GuestType.SkibidiToilet, GuestType.SigmaMale, GuestType.Karen,
            GuestType.Rickroll, GuestType.DistractedBoyfriend, GuestType.Drakeposting, GuestType.ConfusedMathLady,
            GuestType.SideEyeChloe, GuestType.RollSafe, GuestType.DancingBaby,
        };

        static List<GuestInfo> s_Roster;

        /// <summary>Put aside: kept in the code but out of the current build (see <see cref="s_PutAside"/>).</summary>
        public static bool IsPutAside(GuestType type) => s_PutAside.Contains(type);

        /// <summary>
        /// Not a card anyone owns: the Bot a rival's Bots community sends into your next stream. The Bots community is retired
        /// in V5, so no Bot is ever sent (the token stays in the code).
        /// </summary>
        public static bool IsToken(GuestType type) => type == GuestType.Bot;

        /// <summary>A viewer card in the current build (not put aside, not a token).</summary>
        public static bool InRoster(GuestType type) => IsValid(type) && !IsPutAside(type) && !IsToken(type);

        /// <summary>The viewer cards in the current build, in id order (what the shop, the tools and the guides list).</summary>
        public static IReadOnlyList<GuestInfo> Roster
        {
            get
            {
                if (s_Roster == null)
                {
                    s_Roster = new List<GuestInfo>();
                    foreach (var info in s_All)
                        if (InRoster(info.Type)) s_Roster.Add(info);
                }
                return s_Roster;
            }
        }

        // ------------------------------------------------------------------ communities

        const CommunityMask Core = CommunityMask.CoreFanbase;
        const CommunityMask Staff = CommunityMask.Staff;
        const CommunityMask Content = CommunityMask.Content;
        const CommunityMask Tech = CommunityMask.Techspertise;
        const CommunityMask Finance = CommunityMask.FinanceBros;
        const CommunityMask Trolls = CommunityMask.Trolls;
        const CommunityMask Boomer = CommunityMask.Boomer;
        const CommunityMask Millennial = CommunityMask.Millennial;
        const CommunityMask Zoomer = CommunityMask.Zoomer;
        const CommunityMask Armchair = CommunityMask.ArmchairActivists;
        const CommunityMask Clout = CommunityMask.Clout;
        const CommunityMask Bots = CommunityMask.Bots;
        const CommunityMask GenZ = CommunityMask.GenZ;
        const CommunityMask Gatekeepers = CommunityMask.Gatekeepers;
        const CommunityMask HypeMen = CommunityMask.HypeMen;
        const CommunityMask Mods = CommunityMask.Mods;
        const CommunityMask Voyeurs = CommunityMask.Voyeurs;
        const CommunityMask Lurkers = CommunityMask.Lurkers;

        /// <summary>
        /// The communities each roster card starts with (2026-11-16: the viewer blocks of StreamersViewersCommunities.md;
        /// before that the 2026-11-10 version and the V5 card table). The Game Database copies these and you can change them
        /// there (Viewers > Communities); <see cref="SetCommunities"/> applies your edits. Cards not listed have no community
        /// (Affiliate, Internet Starpower and Degen Dog). Since 2026-11-16 the old Gatekeepers (extra removal
        /// uses) are called Mods, and Gatekeepers are the random kickers (PR Manager, Distraught Cat). Content, Full Stream,
        /// Marketers, Shop Control and Saboteurs left the game, so nobody carries them. 2026-11-18: the Lurkers (the two
        /// Isolationists, Forever Alone, Risky Orbiter and two of the risky variants); each risky variant has its original's
        /// communities.
        /// </summary>
        static readonly Dictionary<GuestType, CommunityMask> s_RosterCommunities = new Dictionary<GuestType, CommunityMask>
        {
            { GuestType.LoyalFan, Core },
            { GuestType.Fish, Core },
            { GuestType.Whale, Core },
            { GuestType.IneffectiveTroll, Core | Trolls },
            { GuestType.InfluentialTroll, Trolls },
            { GuestType.BoomerTroll, Trolls | Boomer },
            { GuestType.Producer, Staff },
            { GuestType.ChatAdmin, Staff | Mods },
            { GuestType.Normie, Core },
            { GuestType.CybersecurityEngineer, Staff | Voyeurs },
            { GuestType.PaidSponsorship, Boomer },
            { GuestType.InfluentialIsolationist, Core | Lurkers },
            { GuestType.BillionaireIsolationist, Core | Boomer | Lurkers },
            { GuestType.MildlyRiskyCollaborator, Boomer },
            { GuestType.JuniorPrManager, Staff | GenZ },
            { GuestType.MarketingManager, Staff },
            { GuestType.PromoCodeUser, Core },
            { GuestType.AccountManager, Staff },
            { GuestType.CryptoEnthusiast, Millennial },
            { GuestType.GenerousDonator, HypeMen },
            { GuestType.BingeBunny, Core },
            { GuestType.HateWatcher, Trolls },
            { GuestType.PrManager, Staff | Gatekeepers },
            { GuestType.HighlyRiskyCollaborator, Millennial },
            { GuestType.DieHardFan, Core },
            { GuestType.ChatArbitrator, Staff },
            { GuestType.DistraughtCat, Gatekeepers },
            { GuestType.BotDetector, Mods },
            { GuestType.SocialMediaManager, Staff | Voyeurs },
            { GuestType.MasterOfCurrency, Boomer | Trolls },
            { GuestType.BillionaireBenefactor, Boomer },
            { GuestType.TechAdmin, Mods },
            { GuestType.OnlineJournalist, HypeMen },
            { GuestType.ExecutivePr, Staff | Boomer },
            { GuestType.GenZWithJob, GenZ },
            { GuestType.BigBoomer, Boomer },
            { GuestType.ClippoTheClanker, Staff },
            { GuestType.BiggerStar, HypeMen },
            { GuestType.PrecociousYouth, GenZ },
            { GuestType.PlatformOwner, Boomer | Mods },
            { GuestType.MainstreamCelebrity, Boomer },
            { GuestType.SeniorMarketingManager, Staff | Millennial },
            { GuestType.Evangelist, HypeMen },
            { GuestType.DancingJen, Millennial },
            { GuestType.ForeverAlone, Millennial | Lurkers },
            { GuestType.Doomer, Trolls | Millennial },
            { GuestType.RiskyOrbiter, Lurkers },
            // Risky variants (2026-11-18): the original's communities.
            { GuestType.BunkerBillionaire, Core | Boomer | Lurkers },
            { GuestType.TerminallyOnline, Millennial | Lurkers },
            { GuestType.PowerTrippingMod, Mods },
            { GuestType.DataBroker, Staff | Voyeurs },
            { GuestType.TabloidReporter, HypeMen },
        };

        /// <summary>The communities the put-aside cards had when they left the roster, ready for when they come back.</summary>
        static readonly Dictionary<GuestType, CommunityMask> s_PutAsideCommunities = new Dictionary<GuestType, CommunityMask>
        {
            { GuestType.Trollface, Trolls | Millennial },
            { GuestType.Pepe, Content | Millennial },
            { GuestType.Gigachad, Finance | Zoomer },
            { GuestType.ThisIsFineDog, Content | Millennial },
            { GuestType.Wojak, Content | Millennial },
            { GuestType.GrumpyCat, Content | Millennial },
            { GuestType.NyanCat, Content | Millennial },
            { GuestType.StonksGuy, Finance },
            { GuestType.Cheems, Core | Zoomer },
            { GuestType.ChadVsVirgin, Trolls | Zoomer },
            { GuestType.Doomer, Content | Zoomer },
            { GuestType.NpcWojak, Tech | Armchair },
            { GuestType.ForeverAlone, Core | Millennial },
            { GuestType.MeGusta, Trolls | Millennial },
            { GuestType.KeyboardCat, Tech | Millennial },
            { GuestType.BongoCat, Tech | Zoomer },
            { GuestType.PopCat, Content | Zoomer },
            { GuestType.BigChungus, Content | Zoomer },
            { GuestType.UgandanKnuckles, Trolls | Zoomer },
            { GuestType.OverlyAttachedGirlfriend, Core | Millennial },
            { GuestType.DisasterGirl, Trolls },
            { GuestType.HideThePainHarold, Content | Boomer },
            { GuestType.SuccessKid, Content | Millennial },
            { GuestType.BadLuckBrian, Content | Millennial },
            { GuestType.TungTungTungSahur, Content | Zoomer },
            { GuestType.TralaleroTralala, Content | Zoomer },
            { GuestType.BombardiroCrocodilo, Trolls | Zoomer },
            { GuestType.BallerinaCappuccina, Content | Zoomer },
            { GuestType.SkibidiToilet, Trolls | Zoomer },
            { GuestType.SigmaMale, Finance | Zoomer },
            { GuestType.Karen, Trolls | Boomer | Armchair },
            { GuestType.Rickroll, Trolls | Millennial },
            { GuestType.DistractedBoyfriend, Content | Millennial },
            { GuestType.Drakeposting, Content | Millennial },
            { GuestType.ConfusedMathLady, Finance },
            { GuestType.SideEyeChloe, Content | Zoomer },
            { GuestType.RollSafe, Content | Armchair },
            { GuestType.DancingBaby, Content | Millennial },
        };

        static CommunityMask[] s_Communities;

        /// <summary>
        /// The viewer's communities in the game (your Game Database edits once applied, otherwise the defaults). Retired
        /// communities are left out, so they never score.
        /// </summary>
        public static CommunityMask CommunitiesOf(GuestType type)
        {
            if (s_Communities == null) ResetCommunities();
            int index = (int)type;
            return index >= 0 && index < s_Communities.Length ? s_Communities[index] & Communities.ActiveMask : CommunityMask.None;
        }

        public static bool InCommunity(GuestType type, Community community) => Communities.Has(CommunitiesOf(type), community);

        public static CommunityMask DefaultCommunitiesOf(GuestType type)
        {
            if (s_RosterCommunities.TryGetValue(type, out var roster)) return roster;
            // Roster cards missing from the table have no community (the put-aside table only covers cards still put aside).
            if (!IsPutAside(type)) return CommunityMask.None;
            return s_PutAsideCommunities.TryGetValue(type, out var old) ? old : CommunityMask.None;
        }

        /// <summary>
        /// Overrides one viewer's communities (the Game Database calls this with your edits as it loads). Every peer loads the
        /// same database, and only the host's rules decide scores.
        /// </summary>
        public static void SetCommunities(GuestType type, CommunityMask mask)
        {
            if (s_Communities == null) ResetCommunities();
            int index = (int)type;
            if (index >= 0 && index < s_Communities.Length) s_Communities[index] = mask & (CommunityMask)((1 << Communities.Count) - 1);
        }

        /// <summary>Back to the built-in communities.</summary>
        public static void ResetCommunities()
        {
            var table = new CommunityMask[Count];
            for (int i = 0; i < table.Length; i++) table[i] = DefaultCommunitiesOf((GuestType)i);
            s_Communities = table;
        }

        public static int Count => s_All.Length;

        public static IReadOnlyList<GuestInfo> All => s_All;

        public static GuestInfo Get(GuestType type)
        {
            int index = (int)type;
            if (index < 0 || index >= s_All.Length)
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown guest type");
            return s_All[index];
        }

        public static bool IsValid(GuestType type) => (int)type >= 0 && (int)type < s_All.Length;

        static GuestInfo[] Build()
        {
            const GuestCategory Animal = GuestCategory.Animal;
            const GuestCategory Wojak = GuestCategory.Wojak;
            const GuestCategory Rage = GuestCategory.RageComic;
            const GuestCategory Reaction = GuestCategory.Reaction;
            const GuestCategory Brainrot = GuestCategory.Brainrot;
            // The audience classics and the Diamonds wait for their groups to be decided.
            const GuestCategory Tbd = GuestCategory.Tbd;

            var list = new GuestInfo[]
            {
                // --- The audience classics ---
                // Loyal Fan and Fish are in every shop (ScenarioCatalog.AlwaysInShop); you can buy 4 more of each on top
                // of the starting copies.
                new GuestInfo(GuestType.LoyalFan, "Loyal Fan", 2, Tbd,
                    "A basic starting viewer. Always shows up.", cash: 1, maxCopies: 4 + GameRules.MaxCopiesPerGuest),
                new GuestInfo(GuestType.Fish, "Fish", 3, Tbd,
                    "A basic starting viewer who pays for your first Sub Cap upgrades.", influence: 1, maxCopies: 2 + GameRules.MaxCopiesPerGuest),
                new GuestInfo(GuestType.IneffectiveTroll, "Ineffective Troll", 0, Tbd,
                    "Starting viewer only. Lots of noise, a little risk.", cash: 2, trouble: true, purchasable: false),
                new GuestInfo(GuestType.Producer, "Producer", 3, Tbd,
                    "Action: choose any viewer in the queue to join chat, if there's room.", cash: 2, influence: -1, ability: AbilityKind.Fetch, abilityName: "Shoutout"),
                new GuestInfo(GuestType.ChatAdmin, "Chat Admin", 4, Tbd,
                    "Action: remove one viewer from chat for the rest of the stream (even the Chat Admin).", cash: 1, influence: -1, ability: AbilityKind.Boot, abilityName: "Remove From Chat"),
                new GuestInfo(GuestType.Normie, "Normie", 4, Tbd,
                    "Cancels 1 Deplatform Risk while in chat.", cash: 1, cancels: true),
                new GuestInfo(GuestType.MildlyRiskyCollaborator, "Mildly Risky Collaborator", 5, Tbd,
                    "Big numbers, a little risk.", cash: 3, influence: 2, trouble: true),
                new GuestInfo(GuestType.JuniorPrManager, "Junior PR Manager", 5, Tbd,
                    "Action: send 1 random viewer in chat back to the queue; every use sends one more (2, 3...). Used actions stay used.",
                    cash: 2, influence: -1, ability: AbilityKind.SendBack, abilityName: "Situation Defusal"),
                new GuestInfo(GuestType.DieHardFan, "Die Hard Fan", 7, Tbd,
                    "Uncancellable: cancels 1 Deplatform Risk while in chat.", cash: 2, influence: 1, cancels: true),
                new GuestInfo(GuestType.MarketingManager, "Marketing Manager", 5, Tbd,
                    "Viral Moment: brings one random viewer along when joining. Can push chat over the Sub Cap.", cash: 4, influence: -1, brings: 1),

                // --- Meme guests 1-10 ---
                new GuestInfo(GuestType.DegenDog, "Degen Dog", 7, Animal,
                    "Such Synergy: +1 Cash for each different community in chat (max +5). Other Degen Dogs don't count.", cash: 1),
                new GuestInfo(GuestType.Trollface, "Trollface", 9, Rage,
                    "Action: send another guest back outside (they can arrive again tonight). Gains +2 Cash for every time that guest has been trolled, including this one.",
                    trouble: true, ability: AbilityKind.Troll, abilityName: "U Mad?"),
                new GuestInfo(GuestType.Pepe, "Pepe", 8, Animal,
                    "Action: choose Happy (+3 Cash), Sad (+3 Influence) or Angry (+6 Cash and +1 Deplatform Risk) until the party ends.",
                    ability: AbilityKind.Mood, abilityName: "Mood Swing"),
                new GuestInfo(GuestType.Gigachad, "Gigachad", 19, Wojak,
                    "Alpha Energy: +3 Cash if he's the only guest with 5+ printed Cash. Each extra Gigachad gives every Gigachad -2 Cash.", cash: 5),
                new GuestInfo(GuestType.ThisIsFineDog, "This Is Fine Dog", 12, Animal,
                    "Denial: the first time Deplatform Risk would reach 3, the deplatforming is called off and all Deplatform Risk is cleared. This copy then leaves your Audience for good.", cash: 1),
                new GuestInfo(GuestType.Wojak, "Wojak", 5, Wojak,
                    "Shared Suffering: +2 Cash for every other guest who is suffering (Deplatform Risk, negative stats, infected, zeroed or penalised), max +6.", cash: 1),
                new GuestInfo(GuestType.GrumpyCat, "Grumpy Cat", 9, Animal,
                    "Action: look at the next 3 waiting guests. Reject one for the night and choose who comes in next.",
                    cash: 2, ability: AbilityKind.Inspect, abilityName: "Absolutely Not"),
                new GuestInfo(GuestType.NyanCat, "Nyan Cat", 14, Animal,
                    "Rainbow Trail: every guest without Deplatform Risk who arrives after her in an unbroken row earns +1, +2, +3... Cash (max +6 each). A risky arrival breaks the chain.", cash: 1),
                new GuestInfo(GuestType.StonksGuy, "Stonks Guy", 8, Reaction,
                    "Action: invest 2 Cash or 1 Influence. If the party succeeds you get triple back; if it fails, it's gone.",
                    influence: 1, ability: AbilityKind.Invest, abilityName: "Market Manipulation"),
                new GuestInfo(GuestType.Cheems, "Cheems", 5, Animal,
                    "Don't Bonk Me: with a Doge here, cancels 1 Deplatform Risk and both Doge and Cheems get +2 Cash. Without one, -1 Cash per Deplatform Risk.", cash: 2),

                // --- Meme guests 11-20 ---
                new GuestInfo(GuestType.ChadVsVirgin, "Chad vs Virgin", 10, Wojak,
                    "Action: compare printed Cash with another guest. If theirs is lower, they permanently give Chad 1. If higher, Chad permanently loses 1.",
                    cash: 2, ability: AbilityKind.Mog, abilityName: "Know Your Place"),
                new GuestInfo(GuestType.Doomer, "Doomer", 7, Wojak,
                    "Doomscroll: +2 Cash per Deplatform Risk in the stream.", cash: 1),
                new GuestInfo(GuestType.NpcWojak, "NPC Wojak", 6, Wojak,
                    "Current Thing: on arrival, copies the printed Cash and Influence of the guest who arrived just before him. Not their action, Deplatform Risk or Diamond."),
                new GuestInfo(GuestType.ForeverAlone, "Forever Alone", 8, Rage,
                    "Table for One: +10 Cash if he's the only viewer in chat when scored. Otherwise +1 Cash per empty seat.", cash: 1),
                new GuestInfo(GuestType.MeGusta, "Me Gusta", 8, Rage,
                    "I Like That: gains +2 Cash whenever another guest adds Deplatform Risk, and +1 Influence for each Deplatform Risk removed.", cash: 1),
                new GuestInfo(GuestType.KeyboardCat, "Keyboard Cat", 7, Animal,
                    "Action: evict another guest. Gains +2 Cash if they had Deplatform Risk, otherwise +1 Influence. Their exit effects still happen.",
                    cash: 1, ability: AbilityKind.PlayOff, abilityName: "Play Him Off"),
                new GuestInfo(GuestType.BongoCat, "Bongo Cat", 11, Animal,
                    "The Beat Goes On: every third viewer to join after Bongo Cat gives Bongo Cat and every viewer sharing one of its communities +2 Cash for the night.", cash: 1),
                new GuestInfo(GuestType.PopCat, "Pop Cat", 9, Animal,
                    "Pop Pop Pop: gains +1 Cash whenever any guest uses an action. Every third action also earns +1 Influence."),
                new GuestInfo(GuestType.BigChungus, "Big Chungus", 16, Animal,
                    "Absolute Chungus: takes up 3 spaces. +1 Cash for each point of Sub Cap above 10 (max +5).", cash: 7, size: 3),
                new GuestInfo(GuestType.UgandanKnuckles, "Ugandan Knuckles", 9, Animal,
                    "Follow Me: on arrival, adds a temporary copy of himself to tonight's waiting line (copies don't make more copies). +1 Cash per other Knuckles here.", cash: 1),

                // --- Meme guests 21-30 ---
                new GuestInfo(GuestType.OverlyAttachedGirlfriend, "Overly Attached Girlfriend", 8, Reaction,
                    "Forever Yours: on arrival, attaches to the best other guest. Their Cash are doubled and they can't be removed while she's here. If they're forced out, she leaves too.", cash: 1),
                new GuestInfo(GuestType.DisasterGirl, "Disaster Girl", 13, Reaction,
                    "Action: another guest gets +1 Deplatform Risk and +3 Cash, and she gets +2 Cash. Can't be used if it would get the stream deplatformed.",
                    cash: 2, ability: AbilityKind.Arson, abilityName: "Accidental Fire"),
                new GuestInfo(GuestType.HideThePainHarold, "Hide the Pain Harold", 10, Reaction,
                    "Smile Through It: guests here can't drop below their printed Cash. Gains +1 Cash for each drop he prevents (max +5).", cash: 2),
                new GuestInfo(GuestType.SuccessKid, "Success Kid", 12, Reaction,
                    "Action: reroll the last guest who arrived at random (they go back outside and someone new comes in), or peek at the next guest and decide whether to let them in.",
                    cash: 1, ability: AbilityKind.Reroll, abilityName: "Against All Odds"),
                new GuestInfo(GuestType.BadLuckBrian, "Bad Luck Brian", 6, Reaction,
                    "What Could Go Wrong?: once per party, the first attempt to remove him or clear his Deplatform Risk fails, and he permanently gains +1 Cash.", cash: 4, trouble: true),
                new GuestInfo(GuestType.TungTungTungSahur, "Tung Tung Tung Sahur", 12, Brainrot,
                    "Action: strike another guest. 1st strike: +2 Cash. 2nd: clears their Deplatform Risk. 3rd: evicts them and Tung Tung permanently gains +1 Cash. Strikes are remembered between parties.",
                    cash: 2, ability: AbilityKind.Strike, abilityName: "Three Strikes"),
                new GuestInfo(GuestType.TralaleroTralala, "Tralalero Tralala", 11, Brainrot,
                    "Action: swap two guests' places in the arrival order. Position-based bonuses update; arrival effects don't replay.",
                    cash: 3, ability: AbilityKind.SwapOrder, abilityName: "Tralalero Shuffle"),
                new GuestInfo(GuestType.BombardiroCrocodilo, "Bombardiro Crocodilo", 15, Brainrot,
                    "Action: evict up to 3 other guests and replace each one with a random waiting guest. Arrival and exit effects happen normally.",
                    cash: 5, trouble: true, ability: AbilityKind.Bombard, abilityName: "Bombardment"),
                new GuestInfo(GuestType.BallerinaCappuccina, "Ballerina Cappuccina", 10, Brainrot,
                    "Action: two guests swap their printed Cash for the rest of the night.",
                    cash: 2, ability: AbilityKind.Pirouette, abilityName: "Pirouette"),
                new GuestInfo(GuestType.SkibidiToilet, "Skibidi Toilet", 13, Brainrot,
                    "Skibidi Invasion: on arrival, infects another guest (+2 Cash, +1 Deplatform Risk). Every time another guest arrives, the infection spreads to one more guest. Ends after the party.",
                    cash: 2, trouble: true),

                // --- Meme guests 31-40 ---
                new GuestInfo(GuestType.SigmaMale, "Sigma Male", 14, Wojak,
                    "The Grindset: +1 Cash per empty space (max +8). -1 Cash for every other Sigma Male here.", cash: 3),
                new GuestInfo(GuestType.Karen, "Karen", 8, Reaction,
                    "Action: remove another guest for the night and earn half their cost as Cash (max 8).",
                    trouble: true, ability: AbilityKind.Refund, abilityName: "I Demand a Refund"),
                new GuestInfo(GuestType.Rickroll, "Rickroll", 11, Reaction,
                    "Action: another non-Diamond guest temporarily becomes a random different character from the waiting line, with its stats and action. Back to normal after the party.",
                    cash: 2, ability: AbilityKind.Rickroll, abilityName: "Surprise!"),
                new GuestInfo(GuestType.DistractedBoyfriend, "Distracted Boyfriend", 9, Reaction,
                    "New Attraction: on arrival, reveals 3 waiting guests. You may swap one guest at the party for one of them; the replaced guest sits out tonight.", cash: 2),
                new GuestInfo(GuestType.Drakeposting, "Drakeposting", 10, Reaction,
                    "Action: pick two guests. The first earns nothing tonight; the second doubles their printed Cash or Influence.",
                    cash: 1, ability: AbilityKind.Drake, abilityName: "I Prefer This"),
                new GuestInfo(GuestType.ConfusedMathLady, "Confused Math Lady", 12, Reaction,
                    "Action: for tonight, convert a guest's Cash into Influence (2 Cash = 1 Influence) or their Influence into Cash (1 Influence = 2 Cash).",
                    cash: 1, ability: AbilityKind.Convert, abilityName: "The Numbers Don't Add Up"),
                new GuestInfo(GuestType.SideEyeChloe, "Side Eye Chloe", 7, Reaction,
                    "While she's here you can see the next guest in line. Action: send that guest to the back of the line.",
                    cash: 1, ability: AbilityKind.SideEye, abilityName: "Side Eye"),
                new GuestInfo(GuestType.RollSafe, "Roll Safe", 9, Reaction,
                    "Action: a guest becomes immune to further action effects tonight, good or bad. What they already have stays.",
                    cash: 2, ability: AbilityKind.Immunity, abilityName: "Can't Lose If You Don't Play"),
                new GuestInfo(GuestType.DistraughtCat, "Distraught Cat", 7, Animal,
                    "Everyone Leaves Me: gains +1 Cash or +1 Influence (at random) whenever another guest leaves or is evicted."),
                new GuestInfo(GuestType.DancingBaby, "Dancing Baby", 10, Reaction,
                    "Looping Animation: whenever two actions in a row are the same type, gains +3 Cash (max +9 per party).", cash: 1),

                // --- Diamonds ---
                new GuestInfo(GuestType.BigBoomer, "Big Boomer", 25, Tbd,
                    "DIAMOND. Opinion Express: adds 1 Deplatform Risk.", influence: 1, trouble: true, diamond: true),
                new GuestInfo(GuestType.ClippoTheClanker, "Clippo The Clanker", 25, Tbd,
                    "DIAMOND. Market Collapse: costs 1 Influence and 2 Cash when scored.", cash: -2, influence: -1, diamond: true),
                new GuestInfo(GuestType.BiggerStar, "Bigger Star", 35, Tbd,
                    "DIAMOND. Uplifting: brings one random viewer along when joining.", diamond: true, brings: 1),
                new GuestInfo(GuestType.PrecociousYouth, "Precocious Youth", 35, Tbd,
                    "DIAMOND. No extra effect.", diamond: true),
                new GuestInfo(GuestType.InternetStarpower, "Internet Starpower", 45, Tbd,
                    "DIAMOND. Can Do No Wrong: cancels 1 Deplatform Risk while in chat.", diamond: true, cancels: true),
                new GuestInfo(GuestType.PlatformOwner, "Platform Owner", 45, Tbd,
                    "DIAMOND. Action: remove one viewer for the rest of the stream (even itself).", influence: 3, diamond: true, ability: AbilityKind.Boot, abilityName: "Free The Speech!"),
                new GuestInfo(GuestType.MainstreamCelebrity, "Mainstream Celebrity", 50, Tbd,
                    "DIAMOND. Mass Appeal.", cash: 4, influence: 1, diamond: true),
                new GuestInfo(GuestType.SeniorMarketingManager, "Senior Marketing Manager", 50, Tbd,
                    "DIAMOND. Product Placement.", cash: 5, influence: -1, diamond: true),
                new GuestInfo(GuestType.Evangelist, "Evangelist", 55, Tbd,
                    "DIAMOND. Action: summon any viewer from the queue, even another Diamond.", diamond: true, ability: AbilityKind.Fetch, abilityName: "2-for-1!"),

                // --- V3: the rest of the audience classics ---
                new GuestInfo(GuestType.InfluentialTroll, "Influential Troll", 5, Tbd,
                    "Lots of Influence, but trouble follows.", influence: 4, trouble: true),
                new GuestInfo(GuestType.CybersecurityEngineer, "Cybersecurity Engineer", 4, Tbd,
                    "Action: look at the next viewer in the queue, then let them in or deny them for the rest of the stream.",
                    cash: 3, influence: -1, ability: AbilityKind.Peek, abilityName: "Read Access"),
                new GuestInfo(GuestType.PaidSponsorship, "Paid Sponsorship", 10, Tbd,
                    "Pays very well, but the sponsor brings risk.", cash: 4, trouble: true),
                new GuestInfo(GuestType.PromoCodeUser, "Promo Code User", 5, Tbd,
                    "+3 Influence, but only if chat is full when scored.", cash: 1),
                new GuestInfo(GuestType.AccountManager, "Account Manager", 5, Tbd,
                    "Action: a viewer in chat permanently earns +1 Influence more (up to 9 Influence). Costs 1 Influence when scored.",
                    cash: 3, influence: -1, ability: AbilityKind.Style, abilityName: "Upsell"),
                new GuestInfo(GuestType.CryptoEnthusiast, "Crypto Enthusiast", 6, Tbd,
                    "To the Moon: Crypto Enthusiasts score together, 0 / 1 / 4 / 9 / 16 Influence in total for 1 to 5 of them in chat.",
                    maxCopies: GameRules.GroupCardMaxCopies),
                new GuestInfo(GuestType.GenerousDonator, "Generous Donator", 5, Tbd,
                    "Give to the Rich!: when joining, brings in the next viewer from the queue and scores them once right away (banked).", cash: 1, influence: 1),
                new GuestInfo(GuestType.PrManager, "PR Manager", 6, Tbd,
                    "Action: send random viewers in chat back to the queue so they can join again: 3, 6 or everyone else, by the Gatekeepers' level. Used actions stay used.",
                    cash: 3, influence: -1, ability: AbilityKind.SendBack, abilityName: "Damage Control"),
                new GuestInfo(GuestType.HighlyRiskyCollaborator, "Highly Risky Collaborator", 6, Tbd,
                    "#NotHimToo?: big money, big risk.", influence: 4, trouble: true),
                new GuestInfo(GuestType.ChatArbitrator, "Chat Arbitrator", 7, Tbd,
                    "Action: clear the Deplatform Risk of every risky viewer in chat for the rest of the stream, even if they leave and come back.",
                    influence: -1, ability: AbilityKind.Calm, abilityName: "Keep the Peace"),
                new GuestInfo(GuestType.BotDetector, "Bot Detector", 8, Tbd,
                    "Action: remove two viewers sitting next to each other in chat for the rest of the stream (the Bot Detector can be one of them).",
                    cash: 1, ability: AbilityKind.BootPair, abilityName: "Bot Removal"),
                new GuestInfo(GuestType.RiskyOrbiter, "Risky Orbiter", 7, Tbd,
                    "Brings Cash and money, and a little risk.", cash: 2, influence: 3, trouble: true),
                new GuestInfo(GuestType.SocialMediaManager, "Social Media Manager", 8, Tbd,
                    "Action: look at the next viewer in the queue, then let them in or deny them for the rest of the stream.",
                    cash: 3, influence: -1, ability: AbilityKind.Peek, abilityName: "Sneak Peek"),
                new GuestInfo(GuestType.BillionaireBenefactor, "Billionaire Benefactor", 9, Tbd,
                    "Action (once per stream for all of them): every viewer in chat drops to 0 Cash tonight, and you bank 1 Influence right now for each Billionaire Benefactor in chat.",
                    influence: 4, ability: AbilityKind.Interview, abilityName: "Softball Interview"),
                new GuestInfo(GuestType.TechAdmin, "Tech Admin", 9, Tbd,
                    "Action: remove one viewer for the rest of the stream (even the Tech Admin).",
                    cash: 2, ability: AbilityKind.Boot, abilityName: "Admin Rights"),
                new GuestInfo(GuestType.OnlineJournalist, "Online Journalist", 11, Tbd,
                    "Unwanted Guests: brings two random viewers along when joining. Can push chat over the Sub Cap.", cash: 2, influence: 3, brings: 2),
                new GuestInfo(GuestType.InfluentialIsolationist, "Influential Isolationist", 7, Tbd,
                    "You Shall Not Watch!: +1 Influence for every empty space in chat when scored.", influence: 1),
                new GuestInfo(GuestType.BingeBunny, "Binge Bunny", 5, Tbd,
                    "Algorithmic Rabbit Hole: +1 Cash for every Loyal Fan in chat.", cash: 1),
                new GuestInfo(GuestType.HateWatcher, "Hate-Watcher", 5, Tbd,
                    "Switches mood every time they join chat: hate-watching (+1 Deplatform Risk) on their first entrance, harmless on the next, and so on.", cash: 4),
                new GuestInfo(GuestType.Affiliate, "Affiliate", 8, Tbd,
                    "Checkout Code: when the stream is scored, a random viewer in chat permanently earns +1 Influence more (up to 9 Influence).", cash: 1),
                new GuestInfo(GuestType.MasterOfCurrency, "Master of Currency", 8, Tbd,
                    "Chaos Ladder: +2 Influence for every viewer in chat who still has Deplatform Risk (cancelled risk still counts).", influence: 1),
                new GuestInfo(GuestType.ExecutivePr, "Executive PR", 11, Tbd,
                    "No Bad Press: +2 Cash for every viewer in chat who still has Deplatform Risk (cancelled risk still counts).", cash: 1, influence: -1),
                new GuestInfo(GuestType.GenZWithJob, "Gen Z with Job", 12, Tbd,
                    "Secure The Bag: permanently gains +1 Influence every time they join chat (up to 9 Influence), even if the stream ends badly."),

                // --- V4.2: the Bots community's token (never bought, never kept) ---
                new GuestInfo(GuestType.Bot, "Bot", 0, Tbd,
                    "A spam bot from a rival's stream. Takes up a space in chat and earns nothing. Gone after this stream.", purchasable: false),

                // --- Viewer card edits (2026-11-08): the original game's Dancer, scored as a group ---
                new GuestInfo(GuestType.DancingJen, "Dancing Jen", 7, Tbd,
                    "Dance Break: Dancing Jens score together, 1 / 4 / 9 / 16 / 25 Cash in total for 1 to 5 of them in chat.",
                    maxCopies: GameRules.GroupCardMaxCopies),

                // --- Card edits (2026-11-10): the big Influence viewer (the starter that used to be called Whale is Fish) ---
                new GuestInfo(GuestType.Whale, "Whale", 8, Tbd,
                    "No frills: scores a lot of Influence.", influence: 4),

                // --- Card edits (2026-11-16) ---
                new GuestInfo(GuestType.BoomerTroll, "Boomer Troll", 9, Tbd,
                    "Lots of Cash, but trouble follows.", cash: 3, influence: 2, trouble: true),
                new GuestInfo(GuestType.BillionaireIsolationist, "Billionaire Isolationist", 6, Tbd,
                    "You Shall Not Watch!: +1 Cash for every empty space in chat when scored.", cash: 1),

                // --- Risky variants (2026-11-18): the original's stats, rules and communities, +1 Risk and 3 cheaper
                //     (s_VariantOf). Keep them in step with the original when it changes. ---
                new GuestInfo(GuestType.BunkerBillionaire, "Bunker Billionaire", 6 - VariantDiscount, Tbd,
                    "Doomsday Bunker: +1 Cash for every empty space in chat when scored.", cash: 1, trouble: true),
                new GuestInfo(GuestType.TerminallyOnline, "Terminally Online", 8 - VariantDiscount, Tbd,
                    "Never Logs Off: +10 Cash if they're the only viewer in chat when scored. Otherwise +1 Cash per empty seat.", cash: 1, trouble: true),
                new GuestInfo(GuestType.PowerTrippingMod, "Power-Tripping Mod", 9 - VariantDiscount, Tbd,
                    "Action: remove one viewer for the rest of the stream (even the Power-Tripping Mod).",
                    cash: 2, trouble: true, ability: AbilityKind.Boot, abilityName: "Ban Hammer"),
                new GuestInfo(GuestType.DataBroker, "Data Broker", 8 - VariantDiscount, Tbd,
                    "Action: look at the next viewer in the queue, then let them in or deny them for the rest of the stream.",
                    cash: 3, influence: -1, trouble: true, ability: AbilityKind.Peek, abilityName: "Harvest Data"),
                new GuestInfo(GuestType.TabloidReporter, "Tabloid Reporter", 11 - VariantDiscount, Tbd,
                    "Exclusive Scoop: brings two random viewers along when joining. Can push chat over the Sub Cap.", cash: 2, influence: 3, trouble: true, brings: 2),
            };

            // Guard against an entry being out of order relative to the enum.
            for (int i = 0; i < list.Length; i++)
            {
                if ((int)list[i].Type != i)
                    throw new InvalidOperationException($"GuestCatalog entry {i} is {list[i].Type}; entries must follow GuestType order.");
            }

            return list;
        }
    }
}
