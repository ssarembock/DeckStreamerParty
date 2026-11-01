using System.Collections.Generic;

namespace DeckParty.Core
{
    /// <summary>A character set: which guests can be recruited this season.</summary>
    public sealed class ScenarioDefinition
    {
        public readonly int Id;
        public readonly string Name;
        public readonly string Tagline;
        public readonly string Description;
        public readonly string DefaultVenueId;
        public readonly GuestType[] FixedShop;
        public readonly bool IsRandom;
        public readonly bool IsSandbox;

        public ScenarioDefinition(int id, string name, string tagline, string description, string venueId,
            GuestType[] shop, bool isRandom = false, bool isSandbox = false)
        {
            Id = id;
            Name = name;
            Tagline = tagline;
            Description = description;
            DefaultVenueId = venueId;
            FixedShop = shop ?? new GuestType[0];
            IsRandom = isRandom;
            IsSandbox = isSandbox;
        }

        /// <summary>Preset character sets count toward the completion goal.</summary>
        public bool IsPreset => !IsRandom && !IsSandbox;
    }

    public static class ScenarioCatalog
    {
        public const int RandomScenarioId = 5;
        public const int SandboxScenarioId = 6;
        /// <summary>Preset character sets (ids 0-4 and 7-9; 5 and 6 are Random Mix and Open Lobby).</summary>
        public const int PresetCount = 8;

        const int RandomRegularGuests = 11;
        const int RandomDiamondGuests = 3;

        static readonly ScenarioDefinition[] s_All =
        {
            new ScenarioDefinition(0, "Starter Stream", "Learn the ropes",
                "Your first season live. Dependable regulars, a couple of moderators, Doge and Cheems, and three friendly Diamonds.",
                "bedroom", new[]
                {
                    GuestType.LoyalFan, GuestType.Whale, GuestType.Producer, GuestType.ChatAdmin, GuestType.Normie,
                    GuestType.MildlyRiskyCollaborator, GuestType.DieHardFan, GuestType.MarketingManager, GuestType.Doge, GuestType.Cheems,
                    GuestType.Pepe,
                    GuestType.PrecociousYouth, GuestType.MainstreamCelebrity, GuestType.BigBoomer,
                }),
            new ScenarioDefinition(1, "Cat Cafe", "The internet runs on animals",
                "Every animal meme in one room. Bongo Cat buffs the whole zoo, Nyan Cat chains arrivals, Doge rewards variety and Big Chungus needs a big Sub Cap.",
                "content_house", new[]
                {
                    GuestType.Doge, GuestType.Cheems, GuestType.GrumpyCat, GuestType.NyanCat, GuestType.KeyboardCat,
                    GuestType.BongoCat, GuestType.PopCat, GuestType.CryingCat, GuestType.ThisIsFineDog, GuestType.BigChungus,
                    GuestType.UgandanKnuckles,
                    GuestType.InternetStarpower, GuestType.BiggerStar, GuestType.SeniorMarketingManager,
                }),
            new ScenarioDefinition(2, "Doomscroll", "Platform Ban Risk is good content",
                "Wojaks feed on suffering. Doomer and Me Gusta love Platform Ban Risk, Gigachad and Sigma want the spotlight alone, and This Is Fine Dog keeps the ban hammer guessing.",
                "cursed_stream", new[]
                {
                    GuestType.Wojak, GuestType.Doomer, GuestType.NpcWojak, GuestType.ChadVsVirgin, GuestType.Gigachad,
                    GuestType.SigmaMale, GuestType.ForeverAlone, GuestType.MeGusta, GuestType.Trollface, GuestType.Karen,
                    GuestType.ThisIsFineDog, GuestType.Normie,
                    GuestType.BigBoomer, GuestType.ClippoTheClanker, GuestType.PlatformOwner,
                }),
            new ScenarioDefinition(3, "Italian Brainrot", "Tung tung tung tung",
                "Chaos on a loop. Strikes, shuffles, bombardments and infections. Pop Cat and Dancing Baby turn every action into Subs.",
                "studio_stage", new[]
                {
                    GuestType.TungTungTungSahur, GuestType.TralaleroTralala, GuestType.BombardiroCrocodilo, GuestType.BallerinaCappuccina, GuestType.SkibidiToilet,
                    GuestType.Rickroll, GuestType.DancingBaby, GuestType.PopCat, GuestType.JuniorPrManager, GuestType.DieHardFan,
                    GuestType.ChatAdmin,
                    GuestType.SurpriseGuest, GuestType.BiggerStar, GuestType.MainstreamCelebrity,
                }),
            new ScenarioDefinition(4, "Reaction Channel", "Control the narrative",
                "Manipulators and schemers. Rearrange the line, swap, zero and double stats, convert Subs to Cash and back, and protect your stars from bad luck.",
                "city_irl", new[]
                {
                    GuestType.DistractedBoyfriend, GuestType.Drakeposting, GuestType.ConfusedMathLady, GuestType.SideEyeChloe, GuestType.RollSafe,
                    GuestType.SuccessKid, GuestType.BadLuckBrian, GuestType.DisasterGirl, GuestType.HideThePainHarold, GuestType.OverlyAttachedGirlfriend,
                    GuestType.StonksGuy,
                    GuestType.SurpriseGuest, GuestType.SeniorMarketingManager, GuestType.InternetStarpower,
                }),
            // V3: the audience classics, split across three sets so every one of them has a home.
            new ScenarioDefinition(7, "Talent Agency", "The audience classics",
                "Back to basics. Reliable earners, a Producer to call in the right viewer, a Cybersecurity Engineer on the door and a Comedian who needs a full chat.",
                "studio_stage", new[]
                {
                    GuestType.Whale, GuestType.Producer, GuestType.InfluentialTroll, GuestType.Normie, GuestType.CybersecurityEngineer,
                    GuestType.PaidSponsorship, GuestType.ComedianWithAPodcast, GuestType.AccountManager, GuestType.GenerousDonator, GuestType.MarketingManager,
                    GuestType.BingeBunny,
                    GuestType.PrecociousYouth, GuestType.MainstreamCelebrity, GuestType.BiggerStar,
                }),
            new ScenarioDefinition(8, "Damage Control", "Risk pays, if you can contain it",
                "Risky collaborators bring the money and the Hate-Watcher can't make up their mind. Master of Currency and Executive PR cash in on the chaos while the Chat Arbitrator keeps the peace.",
                "cursed_stream", new[]
                {
                    GuestType.MildlyRiskyCollaborator, GuestType.HighlyRiskyCollaborator, GuestType.RiskyOrbiter, GuestType.HateWatcher, GuestType.MasterOfCurrency,
                    GuestType.ExecutivePr, GuestType.ChatArbitrator, GuestType.DieHardFan, GuestType.BotDetector, GuestType.TechAdmin,
                    GuestType.JuniorPrManager,
                    GuestType.BigBoomer, GuestType.InternetStarpower, GuestType.PlatformOwner,
                }),
            new ScenarioDefinition(9, "Money Talks", "Cash out before it crashes",
                "Rug pulls, checkout codes and softball interviews. Bank Subs mid-stream with the Crypto Guy, grow your Gen Z with Job and let the Online Scandal fill the room.",
                "city_irl", new[]
                {
                    GuestType.CryptoGuy, GuestType.Affiliate, GuestType.GenZWithJob, GuestType.BillionaireBenefactor, GuestType.PrManager,
                    GuestType.SocialMediaManager, GuestType.OnlineScandal, GuestType.Gatekeeper, GuestType.Whale, GuestType.ChatAdmin,
                    GuestType.Normie,
                    GuestType.SeniorMarketingManager, GuestType.ClippoTheClanker, GuestType.SurpriseGuest,
                }),
            new ScenarioDefinition(RandomScenarioId, "Random Mix", "A fresh character set every time",
                "Eleven random guests and three random Diamonds. Win five in a row to earn the Cherry.",
                "metaverse", null, isRandom: true),
            new ScenarioDefinition(SandboxScenarioId, "Open Lobby", "Everyone's invited (sandbox)",
                "Every recruitable guest is in the shop. Great for experimenting; does not count toward progress.",
                "irl_beach", null, isSandbox: true),
        };

        public static IReadOnlyList<ScenarioDefinition> All => s_All;

        public static ScenarioDefinition Get(int id)
        {
            for (int i = 0; i < s_All.Length; i++)
                if (s_All[i].Id == id) return s_All[i];
            return s_All[0];
        }

        /// <summary>Builds the shop for a character set. Random shops are derived from the game seed.</summary>
        public static List<GuestType> BuildShop(int scenarioId, uint seed)
        {
            var scenario = Get(scenarioId);
            var shop = new List<GuestType>();

            if (scenario.IsSandbox)
            {
                foreach (var info in GuestCatalog.All)
                    if (info.Purchasable) shop.Add(info.Type);
            }
            else if (scenario.IsRandom)
            {
                var regulars = new List<GuestType>();
                var diamonds = new List<GuestType>();
                foreach (var info in GuestCatalog.All)
                {
                    if (!info.Purchasable) continue;
                    (info.IsDiamond ? diamonds : regulars).Add(info.Type);
                }

                ulong rng = DeterministicRandom.Seed(seed ^ 0x5EEDu);
                PickInto(regulars, RandomRegularGuests, shop, ref rng);
                PickInto(diamonds, RandomDiamondGuests, shop, ref rng);
            }
            else
            {
                shop.AddRange(scenario.FixedShop);
            }

            shop.Sort((a, b) =>
            {
                var ia = GuestCatalog.Get(a);
                var ib = GuestCatalog.Get(b);
                if (ia.IsDiamond != ib.IsDiamond) return ia.IsDiamond ? 1 : -1;
                int c = ia.Cost.CompareTo(ib.Cost);
                return c != 0 ? c : ((int)a).CompareTo((int)b);
            });
            return shop;
        }

        static void PickInto(List<GuestType> source, int count, List<GuestType> into, ref ulong rng)
        {
            var bag = new List<GuestType>(source);
            for (int i = 0; i < count && bag.Count > 0; i++)
            {
                int index = DeterministicRandom.Range(ref rng, bag.Count);
                into.Add(bag[index]);
                bag.RemoveAt(index);
            }
        }
    }
}
