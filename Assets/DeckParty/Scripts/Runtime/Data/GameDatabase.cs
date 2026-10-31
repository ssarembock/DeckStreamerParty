using System;
using System.Collections.Generic;
using DeckParty.Core;
using UnityEngine;

namespace DeckParty
{
    [Serializable]
    public sealed class GuestArt
    {
        [HideInInspector] public string name;
        public GuestType type;
        [Tooltip("Character portrait from your sprite sheet. Leave empty for the coloured placeholder.")]
        public Sprite portrait;
        [Tooltip("Optional rename shown in the UI (rules are unaffected).")]
        public string displayNameOverride;
        [Tooltip("Placeholder / accent colour. Alpha 0 = use the automatic category colour.")]
        public Color tint = new Color(1f, 1f, 1f, 0f);
    }

    [Serializable]
    public sealed class EvolvingStage
    {
        public int minCapacity;
        public VenueDefinition venue;
    }

    /// <summary>
    /// The one asset that connects your art to the game (Resources/DeckPartyDatabase).
    /// Guest portraits, venues, the UI skin and audio all hang off it.
    /// </summary>
    [CreateAssetMenu(menuName = "Deck Party/Game Database", fileName = "DeckPartyDatabase")]
    public sealed class GameDatabase : ScriptableObject
    {
        public const string ResourcePath = "DeckPartyDatabase";

        public List<GuestArt> guests = new List<GuestArt>();
        public List<VenueDefinition> venues = new List<VenueDefinition>();
        [Tooltip("Venue sequence used by the 'Evolving' option: the house upgrades as capacity grows.")]
        public List<EvolvingStage> evolvingVenue = new List<EvolvingStage>();
        public UISkin skin;
        public AudioLibrary audio;

        static GameDatabase s_Instance;
        static bool s_IsFallback;
        [NonSerialized] Dictionary<GuestType, GuestArt> m_GuestLookup;
        [NonSerialized] List<VenueDefinition> m_RuntimeVenues;

        public static GameDatabase Instance
        {
            get
            {
                if (s_Instance != null && !s_IsFallback) return s_Instance;

                var loaded = Resources.Load<GameDatabase>(ResourcePath);
                if (loaded != null)
                {
                    s_Instance = loaded;
                    s_IsFallback = false;
                    return s_Instance;
                }

                if (s_Instance == null)
                {
                    Debug.LogWarning("[DeckParty] Resources/DeckPartyDatabase not found - using built-in placeholders. Run Deck Party > Setup > Build Everything.");
                    s_Instance = CreateInstance<GameDatabase>();
                    s_Instance.hideFlags = HideFlags.DontSave;
                    s_IsFallback = true;
                }
                return s_Instance;
            }
        }

        /// <summary>Forget the cached instance (used by editor tooling after regenerating assets).</summary>
        public static void ResetCache()
        {
            s_Instance = null;
            s_IsFallback = false;
        }

        public IReadOnlyList<VenueDefinition> Venues
        {
            get
            {
                if (venues != null && venues.Count > 0 && venues.TrueForAll(v => v != null)) return venues;
                if (m_RuntimeVenues == null) m_RuntimeVenues = VenuePresets.CreateRuntimeDefaults();
                return m_RuntimeVenues;
            }
        }

        public GuestArt GetGuestArt(GuestType type)
        {
            if (m_GuestLookup == null)
            {
                m_GuestLookup = new Dictionary<GuestType, GuestArt>();
                foreach (var art in guests)
                    if (art != null) m_GuestLookup[art.type] = art;
            }
            m_GuestLookup.TryGetValue(type, out var result);
            return result;
        }

        public VenueDefinition GetVenue(string id)
        {
            var list = Venues;
            for (int i = 0; i < list.Count; i++)
                if (list[i] != null && string.Equals(list[i].id, id, StringComparison.OrdinalIgnoreCase)) return list[i];
            return list.Count > 0 ? list[0] : null;
        }

        /// <summary>Resolves a venue choice ("auto", "evolving" or an id) for the given game.</summary>
        public VenueDefinition ResolveVenue(string choice, PlayerState state)
        {
            if (string.IsNullOrEmpty(choice) || choice == AppSettings.VenueAuto)
            {
                var scenario = ScenarioCatalog.Get(state != null ? state.ScenarioId : 0);
                if (scenario.IsRandom && state != null)
                {
                    var list = Venues;
                    return list[(int)(state.Seed % (uint)list.Count)];
                }
                return GetVenue(scenario.DefaultVenueId);
            }

            if (choice == AppSettings.VenueEvolving)
            {
                int capacity = state != null ? state.Capacity : GameRules.StartingCapacity;
                if (evolvingVenue != null && evolvingVenue.Count > 0)
                {
                    VenueDefinition best = null;
                    int bestThreshold = int.MinValue;
                    foreach (var stage in evolvingVenue)
                    {
                        if (stage == null || stage.venue == null) continue;
                        if (capacity >= stage.minCapacity && stage.minCapacity >= bestThreshold)
                        {
                            best = stage.venue;
                            bestThreshold = stage.minCapacity;
                        }
                    }
                    if (best != null) return best;
                }

                string id = VenuePresets.Evolving[0].venueId;
                foreach (var stage in VenuePresets.Evolving)
                    if (capacity >= stage.minCapacity) id = stage.venueId;
                return GetVenue(id);
            }

            return GetVenue(choice);
        }

        /// <summary>Choice ids for venue selectors: auto, evolving, then every venue.</summary>
        public List<(string id, string label)> VenueChoices()
        {
            var result = new List<(string id, string label)>
            {
                (AppSettings.VenueAuto, "Character Set Default"),
                (AppSettings.VenueEvolving, $"Evolving (grows with your {Terms.SubCap})"),
            };
            foreach (var venue in Venues)
                if (venue != null) result.Add((venue.id, venue.displayName));
            return result;
        }

        /// <summary>
        /// One entry per guest type, in roster order. Entries left over from an older roster (their stored name no
        /// longer matches the guest at that slot) are reset so a portrait never shows on the wrong character.
        /// Returns how many stale entries were reset.
        /// </summary>
        public int EnsureAllGuests()
        {
            int reset = 0;
            guests.RemoveAll(g => g == null || !GuestCatalog.IsValid(g.type));
            foreach (var info in GuestCatalog.All)
            {
                var existing = guests.Find(g => g.type == info.Type);
                if (existing == null) guests.Add(existing = new GuestArt { type = info.Type });
                else if (!string.IsNullOrEmpty(existing.name) && existing.name != info.Name)
                {
                    if (existing.portrait != null || !string.IsNullOrEmpty(existing.displayNameOverride)) reset++;
                    existing.portrait = null;
                    existing.displayNameOverride = null;
                    existing.tint = new Color(1f, 1f, 1f, 0f);
                }
                existing.name = info.Name;
            }
            guests.Sort((a, b) => a.type.CompareTo(b.type));
            m_GuestLookup = null;
            return reset;
        }

        void OnValidate() => m_GuestLookup = null;
    }
}
