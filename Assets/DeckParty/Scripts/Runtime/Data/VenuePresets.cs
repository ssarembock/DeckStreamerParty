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
            foreach (var preset in All)
            {
                var venue = ScriptableObject.CreateInstance<VenueDefinition>();
                venue.hideFlags = HideFlags.DontSave;
                Apply(venue, preset);
                venue.name = preset.Name;
                list.Add(venue);
            }
            return list;
        }
    }
}
