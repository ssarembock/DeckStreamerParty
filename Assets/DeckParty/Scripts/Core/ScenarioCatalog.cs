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
        /// <summary>Preset character sets (ids 0-4; 5 and 6 are Random Mix and Open Lobby).</summary>
        public const int PresetCount = 5;

        const int RandomRegularGuests = 12;
        const int RandomDiamondGuests = 3;

        // V5: five preset sets, one per Party House scenario (Alien Invitation, High or Low, Best Wishes, Money Management,
        // A Magical Night), each holding the new versions of that scenario's guests and stars. Guests this game doesn't have
        // were swapped for the closest viewer (Private Eye -> Cybersecurity Engineer, Cheerleader -> Binge Bunny,
        // Magician -> Distraught Cat); the Dancer is Dancing Jen since 2026-11-08. The first set has one Trophy, like Alien
        // Invitation; the others have two.
        // 2026-11-16 (card edits): every shop also sells Loyal Fan and Fish (AlwaysInShop), and the sets were tuned to the
        // new communities: Going Live gives the Precocious Youth a Gen-Z partner, Clickbait Roulette and Brand Deal Wishlist
        // get the Boomer Troll, Brand Deal Wishlist a third Hype Man, Monetization Meeting a second Boomer, and Algorithm
        // Magic is about kicking (Mods, Gatekeepers) and the empty seats it leaves (both Isolationists, Forever Alone).
        // Ids are kept (progress is stored per id); the old sets 7-9 are gone.
        static readonly ScenarioDefinition[] s_All =
        {
            new ScenarioDefinition(0, "Going Live", "Your first broadcast",
                "A gentle first season: cheap regulars, staff to run the room and a single Trophy to collect.",
                "bedroom", new[]
                {
                    GuestType.Producer, GuestType.ChatAdmin, GuestType.Normie, GuestType.JuniorPrManager, GuestType.PaidSponsorship,
                    GuestType.MildlyRiskyCollaborator, GuestType.MarketingManager, GuestType.PromoCodeUser, GuestType.DancingJen,
                    GuestType.BillionaireBenefactor, GuestType.InfluentialTroll, GuestType.Whale,
                    GuestType.PrecociousYouth,
                }),
            new ScenarioDefinition(1, "Clickbait Roulette", "Big risks, bigger numbers",
                "Risky collaborators and hate-watchers pay well, a Boomer profits from every one of them, Gen-Z grows and Voyeurs peek at who's next.",
                "city_irl", new[]
                {
                    GuestType.RiskyOrbiter, GuestType.DieHardFan, GuestType.SocialMediaManager, GuestType.JuniorPrManager, GuestType.CybersecurityEngineer,
                    GuestType.MasterOfCurrency, GuestType.GenZWithJob, GuestType.BoomerTroll, GuestType.HighlyRiskyCollaborator,
                    GuestType.HateWatcher, GuestType.InfluentialTroll, GuestType.Doomer,
                    GuestType.MainstreamCelebrity, GuestType.BiggerStar,
                }),
            new ScenarioDefinition(2, "Brand Deal Wishlist", "Be careful what you wish for",
                "Crypto hype, checkout codes and PR spin. Hype Men keep the chat packed, a Degen Dog chases every community, and two very different Trophies wait at the top.",
                "content_house", new[]
                {
                    GuestType.MildlyRiskyCollaborator, GuestType.Affiliate, GuestType.ChatArbitrator, GuestType.PrManager, GuestType.CryptoEnthusiast,
                    GuestType.BingeBunny, GuestType.OnlineJournalist, GuestType.ExecutivePr, GuestType.Normie,
                    GuestType.GenerousDonator, GuestType.BoomerTroll, GuestType.DegenDog,
                    GuestType.Evangelist, GuestType.BigBoomer,
                }),
            new ScenarioDefinition(3, "Monetization Meeting", "Every cent accounted for",
                "Staff on payroll, sponsors on call and a Trophy that costs a fortune. Count every coin: a Millennial inherits whenever a Boomer logs off.",
                "studio_stage", new[]
                {
                    GuestType.SocialMediaManager, GuestType.PrManager, GuestType.ChatAdmin, GuestType.Affiliate, GuestType.CryptoEnthusiast,
                    GuestType.CybersecurityEngineer, GuestType.DieHardFan, GuestType.AccountManager, GuestType.PaidSponsorship,
                    GuestType.HighlyRiskyCollaborator, GuestType.BillionaireIsolationist, GuestType.Whale,
                    GuestType.ClippoTheClanker, GuestType.SeniorMarketingManager,
                }),
            new ScenarioDefinition(4, "Algorithm Magic", "One perfect stream",
                "Mods kick, Gatekeepers clear the room and the Isolationists profit from every empty seat. Hype Men refill the chat for two rare Trophies.",
                "cursed_stream", new[]
                {
                    GuestType.BillionaireBenefactor, GuestType.OnlineJournalist, GuestType.GenerousDonator, GuestType.BotDetector, GuestType.InfluentialIsolationist,
                    GuestType.BillionaireIsolationist, GuestType.PrManager, GuestType.DieHardFan, GuestType.ChatAdmin,
                    GuestType.TechAdmin, GuestType.DistraughtCat, GuestType.ForeverAlone,
                    GuestType.PlatformOwner, GuestType.InternetStarpower,
                }),
            new ScenarioDefinition(RandomScenarioId, "Random Mix", "A fresh character set every time",
                "Loyal Fans, Fish, twelve random viewers and three random Trophy viewers. Win five in a row to earn the Cherry.",
                "metaverse", null, isRandom: true),
            new ScenarioDefinition(SandboxScenarioId, "Open Lobby", "Everyone's invited (sandbox)",
                "Every recruitable guest is in the shop. Great for experimenting; does not count toward progress.",
                "irl_beach", null, isSandbox: true),
        };

        /// <summary>
        /// Sold in every shop, whatever the set (2026-11-16): the two buyable starting viewers, like the original game's Old
        /// Friend and Rich Pal. Random Mix sells them on top of its twelve random viewers.
        /// </summary>
        public static readonly GuestType[] AlwaysInShop = { GuestType.LoyalFan, GuestType.Fish };

        public static bool IsAlwaysInShop(GuestType type) => System.Array.IndexOf(AlwaysInShop, type) >= 0;

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
                    if (!info.Purchasable || IsAlwaysInShop(info.Type)) continue;
                    (info.IsDiamond ? diamonds : regulars).Add(info.Type);
                }

                ulong rng = DeterministicRandom.Seed(seed ^ 0x5EEDu);
                PickInto(regulars, RandomRegularGuests, shop, ref rng);
                PickInto(diamonds, RandomDiamondGuests, shop, ref rng);
            }
            else
            {
                foreach (var type in scenario.FixedShop)
                    if (GuestCatalog.InRoster(type) && !shop.Contains(type)) shop.Add(type);
            }

            // Loyal Fan and Fish are in every shop.
            foreach (var type in AlwaysInShop)
                if (GuestCatalog.InRoster(type) && GuestCatalog.Get(type).Purchasable && !shop.Contains(type)) shop.Add(type);

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
