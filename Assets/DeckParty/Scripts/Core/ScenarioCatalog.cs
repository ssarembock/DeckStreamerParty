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

        // V4.2: eight sets built from the current 44-card roster, one per community idea. Every regular viewer is in at least
        // one set. Ids are kept (progress is stored per id), so a set's completion carries over to the set that replaced it.
        static readonly ScenarioDefinition[] s_All =
        {
            new ScenarioDefinition(0, "First Stream", "Learn the ropes",
                "Your first season live. Dependable regulars, a Producer and a Chat Admin to run the room, one risky collaborator and three friendly Diamonds.",
                "bedroom", new[]
                {
                    GuestType.LoyalFan, GuestType.Whale, GuestType.Producer, GuestType.ChatAdmin, GuestType.Normie,
                    GuestType.MildlyRiskyCollaborator, GuestType.MarketingManager, GuestType.BingeBunny, GuestType.DieHardFan, GuestType.PaidSponsorship,
                    GuestType.Gatekeeper,
                    GuestType.PrecociousYouth, GuestType.MainstreamCelebrity, GuestType.BigBoomer,
                }),
            new ScenarioDefinition(1, "Fan Club", "Loyalty pays",
                "Build a real Core Fanbase: every three different regulars cancel a Deplatform Risk, so trolls and orbiters become safe money. Binge Bunny and the Promo Code User love a packed chat.",
                "content_house", new[]
                {
                    GuestType.LoyalFan, GuestType.Whale, GuestType.Normie, GuestType.Gatekeeper, GuestType.BingeBunny,
                    GuestType.PromoCodeUser, GuestType.GenerousDonator, GuestType.DieHardFan, GuestType.InfluentialTroll, GuestType.HateWatcher,
                    GuestType.RiskyOrbiter,
                    GuestType.Evangelist, GuestType.InternetStarpower, GuestType.BigBoomer,
                }),
            new ScenarioDefinition(2, "Viral Moment", "Clout chasing",
                "Collaborators, scandals and celebrities. Content makes viewers outside your fanbase score twice and Clout tops them up with extra Subs, but the risk piles up fast.",
                "city_irl", new[]
                {
                    GuestType.MildlyRiskyCollaborator, GuestType.HighlyRiskyCollaborator, GuestType.RiskyOrbiter, GuestType.OnlineScandal, GuestType.MarketingManager,
                    GuestType.PaidSponsorship, GuestType.Affiliate, GuestType.ChatArbitrator, GuestType.Producer, GuestType.SocialMediaManager,
                    GuestType.InfluentialTroll,
                    GuestType.BiggerStar, GuestType.InternetStarpower, GuestType.MainstreamCelebrity,
                }),
            new ScenarioDefinition(3, "Meme Lab", "Such synergy",
                "The Memetics Division might make the whole stream score twice. Degen Dog wants a chat full of different communities, and Distraught Cat cashes in every time someone gets kicked.",
                "cursed_stream", new[]
                {
                    GuestType.DegenDog, GuestType.DistraughtCat, GuestType.ChatAdmin, GuestType.TechAdmin, GuestType.BotDetector,
                    GuestType.JuniorPrManager, GuestType.CybersecurityEngineer, GuestType.Gatekeeper, GuestType.SocialMediaManager, GuestType.MasterOfCurrency,
                    GuestType.GenZWithJob,
                    GuestType.ClippoTheClanker, GuestType.PlatformOwner, GuestType.SeniorMarketingManager,
                }),
            new ScenarioDefinition(4, "Staff Meeting", "Run a tight ship",
                "The whole team on payroll. Staff may waive their own Cash costs, Techspertise may bring a crashed server back, and every action gets the right viewer in or the wrong one out.",
                "studio_stage", new[]
                {
                    GuestType.Producer, GuestType.ChatAdmin, GuestType.CybersecurityEngineer, GuestType.AccountManager, GuestType.JuniorPrManager,
                    GuestType.MarketingManager, GuestType.PrManager, GuestType.ChatArbitrator, GuestType.SocialMediaManager, GuestType.ExecutivePr,
                    GuestType.TechAdmin,
                    GuestType.SeniorMarketingManager, GuestType.PlatformOwner, GuestType.Evangelist,
                }),
            new ScenarioDefinition(7, "Generation Gap", "OK boomer",
                "Boomers pay in Cash, Millennials in Subs and Zoomers roll the dice. Master of Currency and Executive PR cash in on every risky viewer in chat.",
                "content_house", new[]
                {
                    GuestType.MasterOfCurrency, GuestType.BillionaireBenefactor, GuestType.ExecutivePr, GuestType.SocialMediaManager, GuestType.GenZWithJob,
                    GuestType.Whale, GuestType.InfluentialTroll, GuestType.HighlyRiskyCollaborator, GuestType.HateWatcher, GuestType.Gatekeeper,
                    GuestType.LoyalFan,
                    GuestType.BigBoomer, GuestType.SeniorMarketingManager, GuestType.PrecociousYouth,
                }),
            new ScenarioDefinition(8, "Crypto Winter", "Cash out before it crashes",
                "Finance Bros may double every Cash reward. Rug pull with the Crypto Enthusiast, stack Checkout Codes, grow your Gen Z with Job and let the Online Scandal fill the room.",
                "city_irl", new[]
                {
                    GuestType.CryptoEnthusiast, GuestType.Affiliate, GuestType.PaidSponsorship, GuestType.GenerousDonator, GuestType.BillionaireBenefactor,
                    GuestType.MasterOfCurrency, GuestType.AccountManager, GuestType.GenZWithJob, GuestType.Whale, GuestType.OnlineScandal,
                    GuestType.Normie,
                    GuestType.ClippoTheClanker, GuestType.SeniorMarketingManager, GuestType.BiggerStar,
                }),
            new ScenarioDefinition(9, "Comment Section", "Don't feed the trolls",
                "Trolls pay out even when you get deplatformed. Ride the risk with the Chat Arbitrator on standby, and in multiplayer let Clippo's Bots spam the next streamer.",
                "cursed_stream", new[]
                {
                    GuestType.InfluentialTroll, GuestType.HateWatcher, GuestType.Gatekeeper, GuestType.HighlyRiskyCollaborator, GuestType.RiskyOrbiter,
                    GuestType.MildlyRiskyCollaborator, GuestType.ChatArbitrator, GuestType.BotDetector, GuestType.DieHardFan, GuestType.ExecutivePr,
                    GuestType.Normie,
                    GuestType.BigBoomer, GuestType.ClippoTheClanker, GuestType.InternetStarpower,
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

            // Only the current roster: put-aside viewers and the Bot token never reach a shop.
            if (scenario.IsSandbox)
            {
                foreach (var info in GuestCatalog.Roster)
                    if (info.Purchasable) shop.Add(info.Type);
            }
            else if (scenario.IsRandom)
            {
                var regulars = new List<GuestType>();
                var diamonds = new List<GuestType>();
                foreach (var info in GuestCatalog.Roster)
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
                foreach (var type in scenario.FixedShop)
                    if (GuestCatalog.InRoster(type)) shop.Add(type);
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
