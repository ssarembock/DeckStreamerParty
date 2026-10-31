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
        public const int PresetCount = 5;

        const int RandomRegularGuests = 11;
        const int RandomDiamondGuests = 3;

        static readonly ScenarioDefinition[] s_All =
        {
            new ScenarioDefinition(0, "Starter Stream", "Learn the ropes",
                "Your first season live. Dependable regulars, a couple of moderators, Doge and Cheems, and three friendly Diamonds.",
                "bedroom", new[]
                {
                    GuestType.LoyalFan, GuestType.Whale, GuestType.SocialMediaManager, GuestType.Moderator, GuestType.AutoMod,
                    GuestType.Collaborator, GuestType.Vips, GuestType.Parasocialite, GuestType.Doge, GuestType.Cheems,
                    GuestType.Pepe,
                    GuestType.MysteryVTuber, GuestType.MegaInfluencer, GuestType.DramaChannel,
                }),
            new ScenarioDefinition(1, "Cat Cafe", "The internet runs on animals",
                "Every animal meme in one room. Bongo Cat buffs the whole zoo, Nyan Cat chains arrivals, Doge rewards variety and Big Chungus needs a big Sub Cap.",
                "content_house", new[]
                {
                    GuestType.Doge, GuestType.Cheems, GuestType.GrumpyCat, GuestType.NyanCat, GuestType.KeyboardCat,
                    GuestType.BongoCat, GuestType.PopCat, GuestType.CryingCat, GuestType.ThisIsFineDog, GuestType.BigChungus,
                    GuestType.UgandanKnuckles,
                    GuestType.WholesomeIcon, GuestType.RaidLeader, GuestType.BrandSponsor,
                }),
            new ScenarioDefinition(2, "Doomscroll", "Swatting Risk is good content",
                "Wojaks feed on suffering. Doomer and Me Gusta love Swatting Risk, Gigachad and Sigma want the spotlight alone, and This Is Fine Dog keeps the SWAT team guessing.",
                "cursed_stream", new[]
                {
                    GuestType.Wojak, GuestType.Doomer, GuestType.NpcWojak, GuestType.ChadVsVirgin, GuestType.Gigachad,
                    GuestType.SigmaMale, GuestType.ForeverAlone, GuestType.MeGusta, GuestType.Trollface, GuestType.Karen,
                    GuestType.ThisIsFineDog, GuestType.AutoMod,
                    GuestType.DramaChannel, GuestType.DivaStreamer, GuestType.PlatformAdmin,
                }),
            new ScenarioDefinition(3, "Italian Brainrot", "Tung tung tung tung",
                "Chaos on a loop. Strikes, shuffles, bombardments and infections. Pop Cat and Dancing Baby turn every action into Subs.",
                "studio_stage", new[]
                {
                    GuestType.TungTungTungSahur, GuestType.TralaleroTralala, GuestType.BombardiroCrocodilo, GuestType.BallerinaCappuccina, GuestType.SkibidiToilet,
                    GuestType.Rickroll, GuestType.DancingBaby, GuestType.PopCat, GuestType.Hodl, GuestType.Vips,
                    GuestType.Moderator,
                    GuestType.Algorithm, GuestType.RaidLeader, GuestType.MegaInfluencer,
                }),
            new ScenarioDefinition(4, "Reaction Channel", "Control the narrative",
                "Manipulators and schemers. Rearrange the line, swap, zero and double stats, convert Subs to Cash and back, and protect your stars from bad luck.",
                "city_irl", new[]
                {
                    GuestType.DistractedBoyfriend, GuestType.Drakeposting, GuestType.ConfusedMathLady, GuestType.SideEyeChloe, GuestType.RollSafe,
                    GuestType.SuccessKid, GuestType.BadLuckBrian, GuestType.DisasterGirl, GuestType.HideThePainHarold, GuestType.OverlyAttachedGirlfriend,
                    GuestType.StonksGuy,
                    GuestType.Algorithm, GuestType.BrandSponsor, GuestType.WholesomeIcon,
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
