using System.Collections.Generic;
using UnityEngine;

namespace DeckParty
{
    /// <summary>
    /// The built-in venue (background) line-up. The editor generator turns these into assets; the runtime uses them
    /// as a fallback. Each venue shows its Background image; the coloured scene is only drawn until one is assigned.
    /// </summary>
    public static class VenuePresets
    {
        public struct Preset
        {
            public string Id;
            /// <summary>The id this venue had before the streaming re-theme; existing assets are migrated in place.</summary>
            public string LegacyId;
            public string Name;
            public string Description;
            public VenueStyle Style;
            public int SkyTop, SkyBottom, Wall, Floor, AccentA, AccentB;
        }

        public static readonly Preset[] All =
        {
            new Preset { Id = "bedroom", LegacyId = "apartment", Name = "Bedroom Setup", Description = "RGB strips, a ring light and a bed that's definitely in frame.",
                Style = VenueStyle.Apartment, SkyTop = 0x1B1440, SkyBottom = 0x4B2A6B, Wall = 0x2C2148, Floor = 0x3A2A2A, AccentA = 0xFFC94A, AccentB = 0xFF4F8B },
            new Preset { Id = "irl_beach", LegacyId = "beach", Name = "IRL Beach Stream", Description = "Sunset, seagulls and a phone on a selfie stick.",
                Style = VenueStyle.Beach, SkyTop = 0x2B2E83, SkyBottom = 0xFF8A5B, Wall = 0x1F6F8B, Floor = 0xC9A26B, AccentA = 0xFFE066, AccentB = 0x3DDBD9 },
            new Preset { Id = "city_irl", LegacyId = "rooftop", Name = "Neon City IRL", Description = "Streaming from a rooftop. The skyline glows. So does chat.",
                Style = VenueStyle.Rooftop, SkyTop = 0x0B0B2B, SkyBottom = 0x3B1C6E, Wall = 0x151036, Floor = 0x24203A, AccentA = 0xFF4F8B, AccentB = 0x3DDBD9 },
            new Preset { Id = "content_house", LegacyId = "mansion", Name = "Content House", Description = "Ten creators, one kitchen, zero privacy.",
                Style = VenueStyle.Mansion, SkyTop = 0x2A0F1F, SkyBottom = 0x6B2338, Wall = 0x3D1626, Floor = 0x4A2C1C, AccentA = 0xFFC94A, AccentB = 0xFF9A3D },
            new Preset { Id = "cursed_stream", LegacyId = "haunted", Name = "Cursed 3AM Stream", Description = "Don't read the chat. Don't look behind you.",
                Style = VenueStyle.Haunted, SkyTop = 0x07131C, SkyBottom = 0x1F3B4D, Wall = 0x15232E, Floor = 0x1E1B24, AccentA = 0x9BFFB0, AccentB = 0xA77BFF },
            new Preset { Id = "metaverse", LegacyId = "space", Name = "Metaverse Lounge", Description = "Everyone is a floating torso. Nobody has legs.",
                Style = VenueStyle.Space, SkyTop = 0x02030F, SkyBottom = 0x14204F, Wall = 0x0C1230, Floor = 0x1D2240, AccentA = 0x5AA8FF, AccentB = 0xFF4F8B },
            new Preset { Id = "studio_stage", LegacyId = "disco", Name = "Studio Stage", Description = "Green screen, smoke machine, legendary clips.",
                Style = VenueStyle.Disco, SkyTop = 0x1A0623, SkyBottom = 0x3E0E4F, Wall = 0x250A33, Floor = 0x14081C, AccentA = 0xFF4FD8, AccentB = 0xFFC94A },
        };

        /// <summary>
        /// One stream setup per streamer, shown while they're live (and when you watch their stream). The generator turns
        /// these into venue assets and links each one to its streamer in the Game Database; drop a background image into
        /// the asset to replace the drawn scene.
        /// </summary>
        public static readonly Preset[] StreamerStages =
        {
            new Preset { Id = "stage_fate", Name = "Debate Bunker", Description = "Three monitors, two arguments, one very loud microphone.",
                Style = VenueStyle.Apartment, SkyTop = 0x120F3A, SkyBottom = 0x352B7A, Wall = 0x1E1950, Floor = 0x2A2340, AccentA = 0x7C6CFF, AccentB = 0xFF4F6B },
            new Preset { Id = "stage_brojogan", Name = "Podcast Studio", Description = "Wood panels, neon sign, elk jerky. Episode four hundred and twenty.",
                Style = VenueStyle.Mansion, SkyTop = 0x2A140A, SkyBottom = 0x6B3A1E, Wall = 0x3D2214, Floor = 0x4A2E1C, AccentA = 0xFF9A3D, AccentB = 0xFFC94A },
            new Preset { Id = "stage_shinobi", Name = "Pro Gamer Dojo", Description = "Blue LEDs, an energy drink fridge and a headband on a pedestal.",
                Style = VenueStyle.Disco, SkyTop = 0x041A2E, SkyBottom = 0x0E4A7A, Wall = 0x0A2A44, Floor = 0x0B1C2C, AccentA = 0x2EC5FF, AccentB = 0xFFE066 },
            new Preset { Id = "stage_irunfast", Name = "Screaming Room", Description = "Acoustic foam, a football and something on fire.",
                Style = VenueStyle.Rooftop, SkyTop = 0x2E0606, SkyBottom = 0x7A1A0E, Wall = 0x3A0B0B, Floor = 0x2A1010, AccentA = 0xFF4646, AccentB = 0xFFD23A },
            new Preset { Id = "stage_mrshark", Name = "Giveaway Warehouse", Description = "Pallets of cash, a countdown clock and a lot of very nervous contestants.",
                Style = VenueStyle.Mansion, SkyTop = 0x061A33, SkyBottom = 0x0F4C8A, Wall = 0x0B2A50, Floor = 0x13283F, AccentA = 0x3FA9F5, AccentB = 0xFFC94A },
            new Preset { Id = "stage_ethicalfboi", Name = "The Mansion (Socialist Edition)", Description = "A two-million-dollar living room with a hammer-and-sickle throw pillow.",
                Style = VenueStyle.Mansion, SkyTop = 0x2A0610, SkyBottom = 0x7A1428, Wall = 0x3D0A18, Floor = 0x3A2418, AccentA = 0xE8364F, AccentB = 0xFFC94A },
            new Preset { Id = "stage_softhardbodyman", Name = "Gym of Feelings", Description = "Dumbbells, a punching bag and a box of tissues. For the punching bag.",
                Style = VenueStyle.Apartment, SkyTop = 0x3A1A2E, SkyBottom = 0x9A5A7A, Wall = 0x5A2E48, Floor = 0x4A3A44, AccentA = 0xF5C4DC, AccentB = 0xFFFFFF },
            new Preset { Id = "stage_strategicabomination", Name = "Chessboard Arena", Description = "Sixty-four squares, two heads and zero inside voices.",
                Style = VenueStyle.Space, SkyTop = 0x041A0E, SkyBottom = 0x0E4A2A, Wall = 0x0A2A18, Floor = 0x1A2A1E, AccentA = 0x3DBE6B, AccentB = 0xF2F2F2 },
            new Preset { Id = "stage_palmane", Name = "Pal Meadow", Description = "Pastel hills, fluffy creatures and a suspicious number of capture spheres.",
                Style = VenueStyle.Beach, SkyTop = 0x3A1A4A, SkyBottom = 0xFF8AC6, Wall = 0x2E7A5A, Floor = 0x7AC96B, AccentA = 0xFF7AC6, AccentB = 0x9BFFB0 },
            new Preset { Id = "stage_manimmakethman", Name = "Blackboard Void", Description = "An infinite dark canvas where vectors gently rotate.",
                Style = VenueStyle.Space, SkyTop = 0x05070F, SkyBottom = 0x14203A, Wall = 0x0C1224, Floor = 0x1A1E2A, AccentA = 0x4C8EEA, AccentB = 0xC8A26B },
            new Preset { Id = "stage_ruderadiologist", Name = "Champions Club Arena", Description = "Smoke, lasers, a lightbox of X-rays and a very large trophy.",
                Style = VenueStyle.Disco, SkyTop = 0x140406, SkyBottom = 0x4A0A12, Wall = 0x22060A, Floor = 0x120608, AccentA = 0xD21E32, AccentB = 0x2EC5FF },
        };

        /// <summary>The stage preset id for a streamer ("stage_fate"...), or null for no streamer.</summary>
        public static string StreamerStageId(DeckParty.Core.StreamerType type) =>
            type == DeckParty.Core.StreamerType.None ? null : "stage_" + type.ToString().ToLowerInvariant();

        /// <summary>Sub Cap thresholds for the "Evolving" venue option: your setup upgrades as it grows.</summary>
        public static readonly (int minCapacity, string venueId)[] Evolving =
        {
            (0, "bedroom"),
            (9, "city_irl"),
            (14, "studio_stage"),
            (20, "content_house"),
            (27, "metaverse"),
        };

        public static void Apply(VenueDefinition venue, Preset preset)
        {
            venue.id = preset.Id;
            venue.displayName = preset.Name;
            venue.description = preset.Description;
            venue.style = preset.Style;
            venue.skyTop = UIPalette.Hex(preset.SkyTop);
            venue.skyBottom = UIPalette.Hex(preset.SkyBottom);
            venue.wallColor = UIPalette.Hex(preset.Wall);
            venue.floorColor = UIPalette.Hex(preset.Floor);
            venue.accentA = UIPalette.Hex(preset.AccentA);
            venue.accentB = UIPalette.Hex(preset.AccentB);
            venue.partyLights = true;
        }

        public static List<VenueDefinition> CreateRuntimeDefaults()
        {
            var list = new List<VenueDefinition>();
            foreach (var preset in AllWithStages())
            {
                var venue = ScriptableObject.CreateInstance<VenueDefinition>();
                venue.hideFlags = HideFlags.DontSave;
                Apply(venue, preset);
                venue.name = preset.Name;
                list.Add(venue);
            }
            return list;
        }

        /// <summary>The regular setups followed by the streamer stages.</summary>
        public static IEnumerable<Preset> AllWithStages()
        {
            foreach (var preset in All) yield return preset;
            foreach (var preset in StreamerStages) yield return preset;
        }
    }
}
