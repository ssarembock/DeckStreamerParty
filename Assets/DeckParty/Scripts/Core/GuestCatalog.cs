using System;
using System.Collections.Generic;

namespace DeckParty.Core
{
    /// <summary>
    /// The complete 82-guest roster. The English names and descriptions here are fallbacks: players see the localized
    /// text from the strings CSV (keys char.&lt;GuestType&gt;.name / .desc / .action).
    /// </summary>
    public static class GuestCatalog
    {
        static readonly GuestInfo[] s_All = Build();

        /// <summary>
        /// Enum names guests had before V3, so art saved under the old id (Game Database) follows the character to its
        /// new name. Only types whose identifier changed are listed. Note "SocialMediaManager" was the Producer's old id;
        /// the new Social Media Manager is a different viewer.
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
            { "Algorithm", GuestType.SurpriseGuest },
        };

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
                new GuestInfo(GuestType.LoyalFan, "Loyal Fan", 2, Tbd,
                    "A basic starting viewer. Always shows up.", pop: 1),
                new GuestInfo(GuestType.Whale, "Whale", 3, Tbd,
                    "A basic starting viewer who pays for your first Sub Cap upgrades.", cash: 1),
                new GuestInfo(GuestType.IneffectiveTroll, "Ineffective Troll", 0, Tbd,
                    "Starting viewer only. Lots of noise, a little risk.", pop: 2, trouble: true, purchasable: false),
                new GuestInfo(GuestType.Producer, "Producer", 3, Tbd,
                    "Action: choose any viewer in the queue to join chat, if there's room.", ability: AbilityKind.Fetch, abilityName: "Shoutout"),
                new GuestInfo(GuestType.ChatAdmin, "Chat Admin", 4, Tbd,
                    "Action: remove one viewer from chat for the rest of the stream (even the Chat Admin).", ability: AbilityKind.Boot, abilityName: "Remove From Chat"),
                new GuestInfo(GuestType.Normie, "Normie", 4, Tbd,
                    "Cancels 1 Platform Ban Risk while in chat.", pop: 1, cancels: true),
                new GuestInfo(GuestType.MildlyRiskyCollaborator, "Mildly Risky Collaborator", 5, Tbd,
                    "Big numbers, a little risk.", pop: 3, cash: 2, trouble: true),
                new GuestInfo(GuestType.JuniorPrManager, "Junior PR Manager", 5, Tbd,
                    "Action: send everyone in chat back to the queue so they can join again. Used actions stay used.",
                    pop: 2, ability: AbilityKind.Reset, abilityName: "Situation Defusal"),
                new GuestInfo(GuestType.DieHardFan, "Die Hard Fan", 7, Tbd,
                    "Uncancellable: cancels 1 Platform Ban Risk while in chat.", pop: 2, cancels: true),
                new GuestInfo(GuestType.MarketingManager, "Marketing Manager", 5, Tbd,
                    "Viral Moment: brings one random viewer along when joining. Can push chat over the Sub Cap.", pop: 3, brings: 1),

                // --- Meme guests 1-10 ---
                new GuestInfo(GuestType.Doge, "Doge", 7, Animal,
                    "Such Synergy: +1 Subs for each different character group at the party (max +5). Other Doges don't count.", pop: 1),
                new GuestInfo(GuestType.Trollface, "Trollface", 9, Rage,
                    "Action: send another guest back outside (they can arrive again tonight). Gains +2 Subs for every time that guest has been trolled, including this one.",
                    trouble: true, ability: AbilityKind.Troll, abilityName: "U Mad?"),
                new GuestInfo(GuestType.Pepe, "Pepe", 8, Animal,
                    "Action: choose Happy (+3 Subs), Sad (+3 $) or Angry (+6 Subs and +1 Platform Ban Risk) until the party ends.",
                    ability: AbilityKind.Mood, abilityName: "Mood Swing"),
                new GuestInfo(GuestType.Gigachad, "Gigachad", 19, Wojak,
                    "Alpha Energy: +3 Subs if he's the only guest with 5+ printed Subs. Each extra Gigachad gives every Gigachad -2 Subs.", pop: 5),
                new GuestInfo(GuestType.ThisIsFineDog, "This Is Fine Dog", 12, Animal,
                    "Denial: the first time Platform Ban Risk would reach 3, the ban is called off and all Platform Ban Risk is cleared. This copy then leaves your Audience for good.", pop: 1),
                new GuestInfo(GuestType.Wojak, "Wojak", 5, Wojak,
                    "Shared Suffering: +2 Subs for every other guest who is suffering (Platform Ban Risk, negative stats, infected, zeroed or penalised), max +6.", pop: 1),
                new GuestInfo(GuestType.GrumpyCat, "Grumpy Cat", 9, Animal,
                    "Action: look at the next 3 waiting guests. Reject one for the night and choose who comes in next.",
                    pop: 2, ability: AbilityKind.Inspect, abilityName: "Absolutely Not"),
                new GuestInfo(GuestType.NyanCat, "Nyan Cat", 14, Animal,
                    "Rainbow Trail: every guest without Platform Ban Risk who arrives after her in an unbroken row earns +1, +2, +3... Subs (max +6 each). A risky arrival breaks the chain.", pop: 1),
                new GuestInfo(GuestType.StonksGuy, "Stonks Guy", 8, Reaction,
                    "Action: invest 2 Gifted Subs or 1 $. If the party succeeds you get triple back; if it fails, it's gone.",
                    cash: 1, ability: AbilityKind.Invest, abilityName: "Market Manipulation"),
                new GuestInfo(GuestType.Cheems, "Cheems", 5, Animal,
                    "Don't Bonk Me: with a Doge here, cancels 1 Platform Ban Risk and both Doge and Cheems get +2 Subs. Without one, -1 Subs per Platform Ban Risk.", pop: 2),

                // --- Meme guests 11-20 ---
                new GuestInfo(GuestType.ChadVsVirgin, "Chad vs Virgin", 10, Wojak,
                    "Action: compare printed Subs with another guest. If theirs is lower, they permanently give Chad 1. If higher, Chad permanently loses 1.",
                    pop: 2, ability: AbilityKind.Mog, abilityName: "Know Your Place"),
                new GuestInfo(GuestType.Doomer, "Doomer", 7, Wojak,
                    "Doomscroll: +2 Subs per Platform Ban Risk at the party. At exactly 2 Platform Ban Risk, also +2 $.", pop: 1),
                new GuestInfo(GuestType.NpcWojak, "NPC Wojak", 6, Wojak,
                    "Current Thing: on arrival, copies the printed Subs and $ of the guest who arrived just before him. Not their action, Platform Ban Risk or Diamond."),
                new GuestInfo(GuestType.ForeverAlone, "Forever Alone", 8, Rage,
                    "Table for One: +6 Subs if he's the only guest when the party ends. Otherwise +1 Subs per empty space.", pop: 1),
                new GuestInfo(GuestType.MeGusta, "Me Gusta", 8, Rage,
                    "I Like That: gains +2 Subs whenever another guest adds Platform Ban Risk, and +1 $ for each Platform Ban Risk removed.", pop: 1),
                new GuestInfo(GuestType.KeyboardCat, "Keyboard Cat", 7, Animal,
                    "Action: evict another guest. Gains +2 Subs if they had Platform Ban Risk, otherwise +1 $. Their exit effects still happen.",
                    pop: 1, ability: AbilityKind.PlayOff, abilityName: "Play Him Off"),
                new GuestInfo(GuestType.BongoCat, "Bongo Cat", 11, Animal,
                    "The Beat Goes On: every third guest to arrive after Bongo Cat gives every Animal guest here +2 Subs for the night.", pop: 1),
                new GuestInfo(GuestType.PopCat, "Pop Cat", 9, Animal,
                    "Pop Pop Pop: gains +1 Subs whenever any guest uses an action. Every third action also earns +1 $."),
                new GuestInfo(GuestType.BigChungus, "Big Chungus", 16, Animal,
                    "Absolute Chungus: takes up 3 spaces. +1 Subs for each point of Sub Cap above 10 (max +5).", pop: 7, size: 3),
                new GuestInfo(GuestType.UgandanKnuckles, "Ugandan Knuckles", 9, Animal,
                    "Follow Me: on arrival, adds a temporary copy of himself to tonight's waiting line (copies don't make more copies). +1 Subs per other Knuckles here.", pop: 1),

                // --- Meme guests 21-30 ---
                new GuestInfo(GuestType.OverlyAttachedGirlfriend, "Overly Attached Girlfriend", 8, Reaction,
                    "Forever Yours: on arrival, attaches to the best other guest. Their Subs are doubled and they can't be removed while she's here. If they're forced out, she leaves too.", pop: 1),
                new GuestInfo(GuestType.DisasterGirl, "Disaster Girl", 13, Reaction,
                    "Action: another guest gets +1 Platform Ban Risk and +3 Subs, and she gets +2 Subs. Can't be used if it would get the stream banned.",
                    pop: 2, ability: AbilityKind.Arson, abilityName: "Accidental Fire"),
                new GuestInfo(GuestType.HideThePainHarold, "Hide the Pain Harold", 10, Reaction,
                    "Smile Through It: guests here can't drop below their printed Subs. Gains +1 Subs for each drop he prevents (max +5).", pop: 2),
                new GuestInfo(GuestType.SuccessKid, "Success Kid", 12, Reaction,
                    "Action: reroll the last guest who arrived at random (they go back outside and someone new comes in), or peek at the next guest and decide whether to let them in.",
                    pop: 1, ability: AbilityKind.Reroll, abilityName: "Against All Odds"),
                new GuestInfo(GuestType.BadLuckBrian, "Bad Luck Brian", 6, Reaction,
                    "What Could Go Wrong?: once per party, the first attempt to remove him or clear his Platform Ban Risk fails, and he permanently gains +1 Subs.", pop: 4, trouble: true),
                new GuestInfo(GuestType.TungTungTungSahur, "Tung Tung Tung Sahur", 12, Brainrot,
                    "Action: strike another guest. 1st strike: +2 Subs. 2nd: clears their Platform Ban Risk. 3rd: evicts them and Tung Tung permanently gains +1 Subs. Strikes are remembered between parties.",
                    pop: 2, ability: AbilityKind.Strike, abilityName: "Three Strikes"),
                new GuestInfo(GuestType.TralaleroTralala, "Tralalero Tralala", 11, Brainrot,
                    "Action: swap two guests' places in the arrival order. Position-based bonuses update; arrival effects don't replay.",
                    pop: 3, ability: AbilityKind.SwapOrder, abilityName: "Tralalero Shuffle"),
                new GuestInfo(GuestType.BombardiroCrocodilo, "Bombardiro Crocodilo", 15, Brainrot,
                    "Action: evict up to 3 other guests and replace each one with a random waiting guest. Arrival and exit effects happen normally.",
                    pop: 5, trouble: true, ability: AbilityKind.Bombard, abilityName: "Bombardment"),
                new GuestInfo(GuestType.BallerinaCappuccina, "Ballerina Cappuccina", 10, Brainrot,
                    "Action: two guests swap their printed Subs for the rest of the night.",
                    pop: 2, ability: AbilityKind.Pirouette, abilityName: "Pirouette"),
                new GuestInfo(GuestType.SkibidiToilet, "Skibidi Toilet", 13, Brainrot,
                    "Skibidi Invasion: on arrival, infects another guest (+2 Subs, +1 Platform Ban Risk). Every time another guest arrives, the infection spreads to one more guest. Ends after the party.",
                    pop: 2, trouble: true),

                // --- Meme guests 31-40 ---
                new GuestInfo(GuestType.SigmaMale, "Sigma Male", 14, Wojak,
                    "The Grindset: +1 Subs per empty space (max +8). -1 Subs for every other Sigma Male here.", pop: 3),
                new GuestInfo(GuestType.Karen, "Karen", 8, Reaction,
                    "Action: remove another guest for the night and earn half their cost as Subs (max 8).",
                    trouble: true, ability: AbilityKind.Refund, abilityName: "I Demand a Refund"),
                new GuestInfo(GuestType.Rickroll, "Rickroll", 11, Reaction,
                    "Action: another non-Diamond guest temporarily becomes a random different character from the waiting line, with its stats and action. Back to normal after the party.",
                    pop: 2, ability: AbilityKind.Rickroll, abilityName: "Surprise!"),
                new GuestInfo(GuestType.DistractedBoyfriend, "Distracted Boyfriend", 9, Reaction,
                    "New Attraction: on arrival, reveals 3 waiting guests. You may swap one guest at the party for one of them; the replaced guest sits out tonight.", pop: 2),
                new GuestInfo(GuestType.Drakeposting, "Drakeposting", 10, Reaction,
                    "Action: pick two guests. The first earns nothing tonight; the second doubles their printed Subs or $.",
                    pop: 1, ability: AbilityKind.Drake, abilityName: "I Prefer This"),
                new GuestInfo(GuestType.ConfusedMathLady, "Confused Math Lady", 12, Reaction,
                    "Action: for tonight, convert a guest's Subs into $ (2 Subs = 1 $) or their $ into Subs (1 $ = 2 Subs).",
                    pop: 1, ability: AbilityKind.Convert, abilityName: "The Numbers Don't Add Up"),
                new GuestInfo(GuestType.SideEyeChloe, "Side Eye Chloe", 7, Reaction,
                    "While she's here you can see the next guest in line. Action: send that guest to the back of the line.",
                    pop: 1, ability: AbilityKind.SideEye, abilityName: "Side Eye"),
                new GuestInfo(GuestType.RollSafe, "Roll Safe", 9, Reaction,
                    "Action: a guest becomes immune to further action effects tonight, good or bad. What they already have stays.",
                    pop: 2, ability: AbilityKind.Immunity, abilityName: "Can't Lose If You Don't Play"),
                new GuestInfo(GuestType.CryingCat, "Crying Cat", 7, Animal,
                    "Everyone Leaves Me: gains +2 Subs and +1 $ whenever another guest leaves or is evicted."),
                new GuestInfo(GuestType.DancingBaby, "Dancing Baby", 10, Reaction,
                    "Looping Animation: whenever two actions in a row are the same type, gains +3 Subs (max +9 per party).", pop: 1),

                // --- Diamonds ---
                new GuestInfo(GuestType.BigBoomer, "Big Boomer", 25, Tbd,
                    "DIAMOND. Opinion Express: adds 1 Platform Ban Risk.", trouble: true, diamond: true),
                new GuestInfo(GuestType.ClippoTheClanker, "Clippo The Clanker", 30, Tbd,
                    "DIAMOND. Market Collapse: costs 3 $ when scored.", cash: -3, diamond: true),
                new GuestInfo(GuestType.BiggerStar, "Bigger Star", 35, Tbd,
                    "DIAMOND. Uplifting: brings one random viewer along when joining.", diamond: true, brings: 1),
                new GuestInfo(GuestType.PrecociousYouth, "Precocious Youth", 40, Tbd,
                    "DIAMOND. No extra effect.", diamond: true),
                new GuestInfo(GuestType.InternetStarpower, "Internet Starpower", 45, Tbd,
                    "DIAMOND. Can Do No Wrong: cancels 1 Platform Ban Risk while in chat.", diamond: true, cancels: true),
                new GuestInfo(GuestType.PlatformOwner, "Platform Owner", 45, Tbd,
                    "DIAMOND. Action: remove one viewer for the rest of the stream (even itself).", diamond: true, ability: AbilityKind.Boot, abilityName: "Free The Speech!"),
                new GuestInfo(GuestType.MainstreamCelebrity, "Mainstream Celebrity", 50, Tbd,
                    "DIAMOND. Mass Appeal.", pop: 3, diamond: true),
                new GuestInfo(GuestType.SeniorMarketingManager, "Senior Marketing Manager", 50, Tbd,
                    "DIAMOND. Product Placement.", cash: 3, diamond: true),
                new GuestInfo(GuestType.SurpriseGuest, "Surprise Guest", 55, Tbd,
                    "DIAMOND. Action: summon any viewer from the queue, even another Diamond.", diamond: true, ability: AbilityKind.Fetch, abilityName: "2-for-1!"),

                // --- V3: the rest of the audience classics ---
                new GuestInfo(GuestType.InfluentialTroll, "Influential Troll", 3, Tbd,
                    "Lots of Subs, but trouble follows.", pop: 4, trouble: true),
                new GuestInfo(GuestType.CybersecurityEngineer, "Cybersecurity Engineer", 4, Tbd,
                    "Action: look at the next viewer in the queue, then let them in or deny them for the rest of the stream.",
                    pop: 2, ability: AbilityKind.Peek, abilityName: "Read Access"),
                new GuestInfo(GuestType.PaidSponsorship, "Paid Sponsorship", 4, Tbd,
                    "Pays well, but viewers skip the ad: -1 Subs when scored.", pop: -1, cash: 2),
                new GuestInfo(GuestType.ComedianWithAPodcast, "Comedian with a Podcast", 5, Tbd,
                    "+5 Subs, but only if chat is full when scored. Always costs 1 $.", cash: -1),
                new GuestInfo(GuestType.AccountManager, "Account Manager", 5, Tbd,
                    "Plenty of Subs, but expects to be paid: costs 1 $ when scored.", pop: 4, cash: -1),
                new GuestInfo(GuestType.CryptoGuy, "Crypto Guy", 5, Tbd,
                    "Action: score another viewer in chat right now, penalties included (banked straight away). They still score again when the stream ends.",
                    pop: 1, cash: -1, ability: AbilityKind.Photograph, abilityName: "Rug Pull"),
                new GuestInfo(GuestType.GenerousDonator, "Generous Donator", 5, Tbd,
                    "Give to the Rich!: when joining, brings in the next viewer from the queue and scores them once right away (banked).", pop: 1),
                new GuestInfo(GuestType.PrManager, "PR Manager", 6, Tbd,
                    "Action: send everyone in chat back to the queue so they can join again. Used actions stay used.",
                    pop: 1, cash: 1, ability: AbilityKind.Reset, abilityName: "Damage Control"),
                new GuestInfo(GuestType.HighlyRiskyCollaborator, "Highly Risky Collaborator", 6, Tbd,
                    "#NotHimToo?: big money, big risk.", cash: 4, trouble: true),
                new GuestInfo(GuestType.ChatArbitrator, "Chat Arbitrator", 7, Tbd,
                    "Action: clear the Platform Ban Risk of every risky viewer in chat for the rest of the stream, even if they leave and come back.",
                    ability: AbilityKind.Calm, abilityName: "Keep the Peace"),
                new GuestInfo(GuestType.BotDetector, "Bot Detector", 8, Tbd,
                    "Action: remove two viewers sitting next to each other in chat for the rest of the stream (the Bot Detector can be one of them).",
                    pop: 1, ability: AbilityKind.BootPair, abilityName: "Bot Removal"),
                new GuestInfo(GuestType.RiskyOrbiter, "Risky Orbiter", 7, Tbd,
                    "Brings Subs and money, and a little risk.", pop: 2, cash: 3, trouble: true),
                new GuestInfo(GuestType.SocialMediaManager, "Social Media Manager", 8, Tbd,
                    "Action: look at the next viewer in the queue, then let them in or deny them for the rest of the stream.",
                    cash: 2, ability: AbilityKind.Peek, abilityName: "Sneak Peek"),
                new GuestInfo(GuestType.BillionaireBenefactor, "Billionaire Benefactor", 9, Tbd,
                    "Action (once per stream for all of them): every viewer in chat drops to 0 Subs tonight, and you bank 1 $ right now for each Billionaire Benefactor in chat.",
                    cash: 5, ability: AbilityKind.Interview, abilityName: "Softball Interview"),
                new GuestInfo(GuestType.TechAdmin, "Tech Admin", 9, Tbd,
                    "Action: remove one viewer for the rest of the stream (even the Tech Admin).",
                    pop: 2, ability: AbilityKind.Boot, abilityName: "Admin Rights"),
                new GuestInfo(GuestType.OnlineScandal, "Online Scandal", 11, Tbd,
                    "Unwanted Guests: brings two random viewers along when joining. Can push chat over the Sub Cap.", pop: 2, cash: 3, brings: 2),
                new GuestInfo(GuestType.Gatekeeper, "Gatekeeper", 4, Tbd,
                    "You Shall Not Watch!: +1 Subs for every empty space in chat when scored.", pop: 1),
                new GuestInfo(GuestType.BingeBunny, "Binge Bunny", 5, Tbd,
                    "Algorithmic Rabbit Hole: +1 Subs for every Loyal Fan in chat.", pop: 1),
                new GuestInfo(GuestType.HateWatcher, "Hate-Watcher", 5, Tbd,
                    "Switches mood every time they join chat: hate-watching (+1 Platform Ban Risk) on their first entrance, harmless on the next, and so on.", pop: 4),
                new GuestInfo(GuestType.Affiliate, "Affiliate", 7, Tbd,
                    "Action: a viewer in chat permanently earns +1 $ more (up to 9 $). Costs 1 $ when scored.",
                    cash: -1, ability: AbilityKind.Style, abilityName: "Checkout Code"),
                new GuestInfo(GuestType.MasterOfCurrency, "Master of Currency", 8, Tbd,
                    "Chaos Ladder: +2 $ for every viewer in chat who still has Platform Ban Risk (cancelled risk still counts).", cash: 1),
                new GuestInfo(GuestType.ExecutivePr, "Executive PR", 11, Tbd,
                    "No Bad Press: +2 Subs for every viewer in chat who still has Platform Ban Risk (cancelled risk still counts)."),
                new GuestInfo(GuestType.GenZWithJob, "Gen Z with Job", 12, Tbd,
                    "Secure The Bag: permanently gains +1 $ every time they join chat (up to 9 $), even if the stream ends badly."),
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
