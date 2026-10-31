using System;
using System.Collections.Generic;

namespace DeckParty.Core
{
    /// <summary>The complete 59-guest roster.</summary>
    public static class GuestCatalog
    {
        static readonly GuestInfo[] s_All = Build();

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
            const GuestCategory Community = GuestCategory.Community;
            const GuestCategory Animal = GuestCategory.Animal;
            const GuestCategory Wojak = GuestCategory.Wojak;
            const GuestCategory Rage = GuestCategory.RageComic;
            const GuestCategory Reaction = GuestCategory.Reaction;
            const GuestCategory Brainrot = GuestCategory.Brainrot;
            const GuestCategory Diamond = GuestCategory.Diamond;

            var list = new GuestInfo[]
            {
                // --- Community (the renamed classics) ---
                new GuestInfo(GuestType.LoyalFan, "Loyal Fan", 2, Community,
                    "Always shows up. Never misses a stream.", pop: 1),
                new GuestInfo(GuestType.Whale, "Whale", 3, Community,
                    "Funds your first Sub Cap upgrades.", cash: 1),
                new GuestInfo(GuestType.BotFarm, "Bot Farm", 0, Community,
                    "Cheap views, risky attention. Starting guest only.", pop: 2, trouble: true, purchasable: false),
                new GuestInfo(GuestType.SocialMediaManager, "Social Media Manager", 3, Community,
                    "Action: choose any waiting guest to come in.", ability: AbilityKind.Fetch, abilityName: "Shoutout"),
                new GuestInfo(GuestType.Moderator, "Moderator", 4, Community,
                    "Action: remove one guest from chat for the rest of the party (even the Moderator).", ability: AbilityKind.Boot, abilityName: "Remove From Chat"),
                new GuestInfo(GuestType.AutoMod, "AutoMod", 4, Community,
                    "Cancels 1 Swatting Risk while present.", pop: 1, cancels: true),
                new GuestInfo(GuestType.Collaborator, "Collaborator", 5, Community,
                    "Big numbers, bigger risk.", pop: 3, cash: 2, trouble: true),
                new GuestInfo(GuestType.Hodl, "HODL", 5, Community,
                    "Action: send everyone outside so they can arrive again.", pop: 2, ability: AbilityKind.Reset, abilityName: "Reset the Stream"),
                new GuestInfo(GuestType.Vips, "VIPS", 7, Community,
                    "Cancels 1 Swatting Risk while present.", pop: 2, cancels: true),
                new GuestInfo(GuestType.Parasocialite, "Parasocialite", 5, Community,
                    "Brings one random guest along.", pop: 3, brings: 1),

                // --- Meme guests 1-10 ---
                new GuestInfo(GuestType.Doge, "Doge", 7, Animal,
                    "Such Synergy: +1 Subs for each different character family at the party (max +5). Other Doges don't count.", pop: 1),
                new GuestInfo(GuestType.Trollface, "Trollface", 9, Rage,
                    "Action: send another guest back outside (they can arrive again tonight). Gains +2 Subs for every time that guest has been trolled, including this one.",
                    trouble: true, ability: AbilityKind.Troll, abilityName: "U Mad?"),
                new GuestInfo(GuestType.Pepe, "Pepe", 8, Animal,
                    "Action: choose Happy (+3 Subs), Sad (+3 $) or Angry (+6 Subs and +1 Swatting Risk) until the party ends.",
                    ability: AbilityKind.Mood, abilityName: "Mood Swing"),
                new GuestInfo(GuestType.Gigachad, "Gigachad", 19, Wojak,
                    "Alpha Energy: +3 Subs if he's the only guest with 5+ printed Subs. Each extra Gigachad gives every Gigachad -2 Subs.", pop: 5),
                new GuestInfo(GuestType.ThisIsFineDog, "This Is Fine Dog", 12, Animal,
                    "Denial: the first time Swatting Risk would reach 3, the SWAT team is called off and all Swatting Risk is cleared. This copy then leaves your Squad for good.", pop: 1),
                new GuestInfo(GuestType.Wojak, "Wojak", 5, Wojak,
                    "Shared Suffering: +2 Subs for every other guest who is suffering (Swatting Risk, negative stats, infected, zeroed or penalised), max +6.", pop: 1),
                new GuestInfo(GuestType.GrumpyCat, "Grumpy Cat", 9, Animal,
                    "Action: look at the next 3 waiting guests. Reject one for the night and choose who comes in next.",
                    pop: 2, ability: AbilityKind.Inspect, abilityName: "Absolutely Not"),
                new GuestInfo(GuestType.NyanCat, "Nyan Cat", 14, Animal,
                    "Rainbow Trail: every guest without Swatting Risk who arrives after her in an unbroken row earns +1, +2, +3... Subs (max +6 each). A Swatting Risk arrival breaks the chain.", pop: 1),
                new GuestInfo(GuestType.StonksGuy, "Stonks Guy", 8, Reaction,
                    "Action: invest 2 Gifted Subs or 1 $. If the party succeeds you get triple back; if it fails, it's gone.",
                    cash: 1, ability: AbilityKind.Invest, abilityName: "Market Manipulation"),
                new GuestInfo(GuestType.Cheems, "Cheems", 5, Animal,
                    "Don't Bonk Me: with a Doge here, cancels 1 Swatting Risk and both Doge and Cheems get +2 Subs. Without one, -1 Subs per Swatting Risk.", pop: 2),

                // --- Meme guests 11-20 ---
                new GuestInfo(GuestType.ChadVsVirgin, "Chad vs Virgin", 10, Wojak,
                    "Action: compare printed Subs with another guest. If theirs is lower, they permanently give Chad 1. If higher, Chad permanently loses 1.",
                    pop: 2, ability: AbilityKind.Mog, abilityName: "Know Your Place"),
                new GuestInfo(GuestType.Doomer, "Doomer", 7, Wojak,
                    "Doomscroll: +2 Subs per Swatting Risk at the party. At exactly 2 Swatting Risk, also +2 $.", pop: 1),
                new GuestInfo(GuestType.NpcWojak, "NPC Wojak", 6, Wojak,
                    "Current Thing: on arrival, copies the printed Subs and $ of the guest who arrived just before him. Not their action, Swatting Risk or Diamond."),
                new GuestInfo(GuestType.ForeverAlone, "Forever Alone", 8, Rage,
                    "Table for One: +6 Subs if he's the only guest when the party ends. Otherwise +1 Subs per empty space.", pop: 1),
                new GuestInfo(GuestType.MeGusta, "Me Gusta", 8, Rage,
                    "I Like That: gains +2 Subs whenever another guest adds Swatting Risk, and +1 $ for each Swatting Risk removed.", pop: 1),
                new GuestInfo(GuestType.KeyboardCat, "Keyboard Cat", 7, Animal,
                    "Action: evict another guest. Gains +2 Subs if they had Swatting Risk, otherwise +1 $. Their exit effects still happen.",
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
                    "Action: another guest gets +1 Swatting Risk and +3 Subs, and she gets +2 Subs. Can't be used if it would bring the SWAT team.",
                    pop: 2, ability: AbilityKind.Arson, abilityName: "Accidental Fire"),
                new GuestInfo(GuestType.HideThePainHarold, "Hide the Pain Harold", 10, Reaction,
                    "Smile Through It: guests here can't drop below their printed Subs. Gains +1 Subs for each drop he prevents (max +5).", pop: 2),
                new GuestInfo(GuestType.SuccessKid, "Success Kid", 12, Reaction,
                    "Action: reroll the last guest who arrived at random (they go back outside and someone new comes in), or peek at the next guest and decide whether to let them in.",
                    pop: 1, ability: AbilityKind.Reroll, abilityName: "Against All Odds"),
                new GuestInfo(GuestType.BadLuckBrian, "Bad Luck Brian", 6, Reaction,
                    "What Could Go Wrong?: once per party, the first attempt to remove him or clear his Swatting Risk fails, and he permanently gains +1 Subs.", pop: 4, trouble: true),
                new GuestInfo(GuestType.TungTungTungSahur, "Tung Tung Tung Sahur", 12, Brainrot,
                    "Action: strike another guest. 1st strike: +2 Subs. 2nd: clears their Swatting Risk. 3rd: evicts them and Tung Tung permanently gains +1 Subs. Strikes are remembered between parties.",
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
                    "Skibidi Invasion: on arrival, infects another guest (+2 Subs, +1 Swatting Risk). Every time another guest arrives, the infection spreads to one more guest. Ends after the party.",
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
                new GuestInfo(GuestType.DramaChannel, "Drama Channel", 25, Diamond,
                    "DIAMOND. Brings the drama.", trouble: true, diamond: true),
                new GuestInfo(GuestType.DivaStreamer, "Diva Streamer", 30, Diamond,
                    "DIAMOND. Expensive taste.", cash: -3, diamond: true),
                new GuestInfo(GuestType.RaidLeader, "Raid Leader", 35, Diamond,
                    "DIAMOND. Brings one random guest along.", diamond: true, brings: 1),
                new GuestInfo(GuestType.MysteryVTuber, "Mystery VTuber", 40, Diamond,
                    "DIAMOND. No fees, no face, no fuss.", diamond: true),
                new GuestInfo(GuestType.WholesomeIcon, "Wholesome Icon", 45, Diamond,
                    "DIAMOND. Cancels 1 Swatting Risk while present.", diamond: true, cancels: true),
                new GuestInfo(GuestType.PlatformAdmin, "Platform Admin", 45, Diamond,
                    "DIAMOND. Action: remove one guest for the rest of the party (even itself).", diamond: true, ability: AbilityKind.Boot, abilityName: "Ban Wave"),
                new GuestInfo(GuestType.MegaInfluencer, "Mega Influencer", 50, Diamond,
                    "DIAMOND. Pure reach.", pop: 3, diamond: true),
                new GuestInfo(GuestType.BrandSponsor, "Brand Sponsor", 50, Diamond,
                    "DIAMOND. Pays to be seen.", cash: 3, diamond: true),
                new GuestInfo(GuestType.Algorithm, "The Algorithm", 55, Diamond,
                    "DIAMOND. Action: summon any waiting guest.", diamond: true, ability: AbilityKind.Fetch, abilityName: "Trending"),
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
