using System;
using System.Collections.Generic;
using DeckParty.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace DeckParty
{
    [Serializable]
    public sealed class GuestArt
    {
        [HideInInspector] public string name;
        /// <summary>Stable id (the GuestType name). Entries are matched by this, so renaming or translating a viewer never moves art.</summary>
        [HideInInspector] public string key;
        public GuestType type;
        [Tooltip("Character portrait from your sprite sheet. Leave empty for the coloured placeholder. Always used as the fallback.")]
        public Sprite portrait;
        [Tooltip("Placeholder / accent colour. Alpha 0 = use the colour of the viewer's first community.")]
        public Color tint = new Color(1f, 1f, 1f, 0f);
        [Tooltip("The viewer's communities (none, one or several). Every different viewer in chat from a community gives it a point; " +
                 "points give the community's bonus. Starts from the built-in defaults.")]
        public CommunityMask communities;

        [Header("Animation (optional, overrides the portrait)")]
        [Tooltip("Your animated character prefab (rigged PSB or sprite-swap). Rendered onto the card by the portrait stage.")]
        public GameObject characterPrefab;
        [Tooltip("Drives the prefab (overrides its own controller). With no prefab, it animates a sprite renderer showing the portrait. " +
                 "States: Idle, Idle_Pre_Active, Active, Idle_Post_Active, Score, optional Enter/Exit/Hit/Celebrate.")]
        public RuntimeAnimatorController animatorController;
        [Tooltip("Zoom applied after the character is fitted to the card (1 = fit).")]
        [Range(0.3f, 3f)] public float framingZoom = 1f;
        [Tooltip("Offset after fitting, in fractions of the card portrait (x right, y up).")]
        public Vector2 framingOffset;

        /// <summary>Legacy rename field (now done through the localization CSV). Kept so old databases still load.</summary>
        [HideInInspector] public string displayNameOverride;

        public bool IsAnimated => characterPrefab != null || animatorController != null;
    }

    /// <summary>Art for one streamer (the character a player picks on the Select Your Streamer screen).</summary>
    [Serializable]
    public sealed class StreamerArt
    {
        [HideInInspector] public string name;
        public StreamerType type;
        [Tooltip("Select-screen, shop and facecam portrait. Leave empty for the coloured placeholder.")]
        public Sprite portrait;
        [Tooltip("Optional animated character. States: Idle, Select (picked on the select screen), Anxious (one Deplatform Risk from being deplatformed), " +
                 "optional Hit (deplatformed or Server Down) and Celebrate (a stream ends well), both played in the facecam.")]
        public GameObject characterPrefab;
        [Tooltip("Optional controller for the prefab (or for the portrait on its own).")]
        public RuntimeAnimatorController animatorController;
        [Tooltip("The stream setup (background) shown while this streamer is live.")]
        public VenueDefinition venue;
        [Tooltip("Accent colour. Alpha 0 = use the built-in colour.")]
        public Color color = new Color(1f, 1f, 1f, 0f);
        [Tooltip("Lines shown under the streamer on the select screen. One is picked at random each time they're highlighted. " +
                 "They show exactly as typed in every language (the original catchphrase stays translated while its text is unchanged).")]
        [TextArea(1, 3)] public List<string> quotes = new List<string>();
        /// <summary>Set once the original catchphrase has been copied into <see cref="quotes"/>, so deleting it stays deleted.</summary>
        [HideInInspector] public bool quotesSeeded;

        public bool IsAnimated => characterPrefab != null || animatorController != null;
    }

    [Serializable]
    public sealed class EvolvingStage
    {
        public int minCapacity;
        public VenueDefinition venue;
    }

    /// <summary>The icon for one viewer community: on cards, shop tiles, tooltips, text and the Community Beats chips.</summary>
    [Serializable]
    public sealed class CommunityIcon
    {
        [HideInInspector] public string name;
        public Community community;
        [Tooltip("Small square icon for this community (it also appears inside text, so keep it readable at ~24 px). Leave empty for the built-in badge.")]
        public Sprite icon;
    }

    /// <summary>Old: the icon for one character group (groups were replaced by communities; kept so old databases load).</summary>
    [Serializable]
    public sealed class GroupIcon
    {
        [HideInInspector] public string name;
        public GuestCategory group;
        [Tooltip("Small square icon for this group. Leave empty to show no icon.")]
        public Sprite icon;
    }

    /// <summary>
    /// Emojis that float up from the viewers in chat. Three moods, picked by how close the stream is to being deplatformed:
    /// calm streams send mostly peaceful ones, tense streams argue, and one Deplatform Risk from the limit they get angry.
    /// </summary>
    [Serializable]
    public sealed class ViewerEmojiSet
    {
        [Tooltip("Happy / peaceful emojis. Slice your sprite sheet, select all the slices and drag them onto this list.")]
        public List<Sprite> peaceful = new List<Sprite>();
        [Tooltip("Slightly argumentative emojis.")]
        public List<Sprite> argumentative = new List<Sprite>();
        [Tooltip("Very angry emojis.")]
        public List<Sprite> angry = new List<Sprite>();

        [Header("Timing (seconds between emojis across the whole chat)")]
        [Tooltip("No Deplatform Risk.")]
        [Range(0.3f, 10f)] public float calmInterval = 3.2f;
        [Tooltip("Some Deplatform Risk.")]
        [Range(0.2f, 10f)] public float tenseInterval = 1.8f;
        [Tooltip("One Deplatform Risk from the limit.")]
        [Range(0.1f, 10f)] public float heatedInterval = 0.9f;
        [Tooltip("Emoji size in pixels (at 1080p).")]
        [Range(24f, 160f)] public float size = 64f;
        [Tooltip("How far they float up, in pixels.")]
        [Range(20f, 300f)] public float rise = 110f;
        [Tooltip("How long one emoji lasts.")]
        [Range(0.5f, 5f)] public float lifetime = 1.8f;

        public bool HasArt => peaceful.Count + argumentative.Count + angry.Count > 0;
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
        public List<StreamerArt> streamers = new List<StreamerArt>();
        public List<VenueDefinition> venues = new List<VenueDefinition>();
        [Tooltip("Venue sequence used by the 'Evolving' option: the house upgrades as capacity grows.")]
        public List<EvolvingStage> evolvingVenue = new List<EvolvingStage>();
        public UISkin skin;
        public AudioLibrary audio;
        [Tooltip("Feel feedbacks, Sprite Shaders Ultimate materials and polish switches (generated by Deck Party > Setup).")]
        public FxLibrary fx;
        [Tooltip("Renders animated characters onto cards (generated by Deck Party > Setup).")]
        public GameObject portraitStage;

        [Header("Stream chat bubble")]
        [Tooltip("The big square chat bubble the viewers sit in during a stream. Give the sprite 9-slice borders in the Sprite Editor so it stretches cleanly.")]
        public Sprite chatBubble;
        [Tooltip("The bubble's tail: a sprite pointing DOWN, drawn on top of the bubble's top edge so the chat seems to come down from the computer in the background. " +
                 "Where it sits along the edge is set per stream setup (Venue > Chat Bubble Tail Position).")]
        public Sprite chatBubbleTail;
        [Tooltip("Tint for the bubble and its tail (white = the sprites' own colours).")]
        public Color chatBubbleTint = Color.white;

        [Header("Viewer emojis (float up from viewers in chat)")]
        public ViewerEmojiSet viewerEmojis = new ViewerEmojiSet();

        [Header("Multiplayer emotes")]
        [Tooltip("One sprite per emote button (slice your emote sheet and drag the slices here, in button order). Pressing one makes it pop up " +
                 "from your avatar on every player's screen. Leave empty to use the built-in text emotes. Up to 16.")]
        public List<Sprite> emotes = new List<Sprite>();

        [Header("Stream endings")]
        [Tooltip("Shown full-screen when Deplatform Risk goes over the limit and the stream ends (the streamer gets deplatformed).")]
        [FormerlySerializedAs("platformBanImage")]
        public Sprite deplatformImage;
        [Tooltip("Shown full-screen when chat goes over the Sub Cap and the stream ends.")]
        public Sprite serverDownImage;

        [Header("Viewer communities")]
        [Tooltip("An icon for each community, shown on cards, shop tiles, tooltips, in text and on the Community Beats chips.")]
        public List<CommunityIcon> communityIcons = new List<CommunityIcon>();

        /// <summary>Old character group icons (groups were replaced by communities). Hidden; kept so nothing you assigned is lost.</summary>
        [HideInInspector] public List<GroupIcon> groupIcons = new List<GroupIcon>();

        /// <summary>Set once every viewer's communities have been copied from the built-in defaults; after that your edits rule.</summary>
        [HideInInspector] public bool communitiesSeeded;

        /// <summary>
        /// Which batch of built-in communities this database has taken in. When communities are added to the game, their
        /// default members are merged in once (your edits to the older communities are kept). 0 = seeded before revisions.
        /// </summary>
        [HideInInspector] public int communitiesRevision;

        /// <summary>
        /// 1: the first seven communities. 2: Boomer, Millennial, Zoomer and Armchair Activists. 3: the V4.2 card table (Clout,
        /// Memetics Division and Bots; every roster card takes the table's communities).
        /// </summary>
        public const int CommunityDefaultsRevision = 3;

        /// <summary>The revision whose built-in communities replace a roster card's (instead of merging into them).</summary>
        const int RosterTableRevision = 3;

        static CommunityMask AddedInRevision(int revision)
        {
            switch (revision)
            {
                case 2: return CommunityMask.Boomer | CommunityMask.Millennial | CommunityMask.Zoomer | CommunityMask.ArmchairActivists;
                default: return CommunityMask.None;
            }
        }

        /// <summary>
        /// A viewer's communities with the defaults of any communities added since this database was seeded. The V4.2 card
        /// table redefined every roster card's communities, so those take the table once (put-aside cards keep theirs).
        /// </summary>
        CommunityMask WithNewDefaults(GuestArt art)
        {
            var mask = art.communities;
            for (int revision = Math.Max(1, communitiesRevision) + 1; revision <= CommunityDefaultsRevision; revision++)
            {
                if (revision == RosterTableRevision)
                {
                    if (GuestCatalog.InRoster(art.type)) mask = GuestCatalog.DefaultCommunitiesOf(art.type);
                    continue;
                }
                mask |= GuestCatalog.DefaultCommunitiesOf(art.type) & AddedInRevision(revision);
            }
            return mask;
        }

        static GameDatabase s_Instance;
        static bool s_IsFallback;
        [NonSerialized] Dictionary<GuestType, GuestArt> m_GuestLookup;
        [NonSerialized] Dictionary<StreamerType, StreamerArt> m_StreamerLookup;
        [NonSerialized] List<VenueDefinition> m_RuntimeVenues;

        public static GameDatabase Instance
        {
            get
            {
                if (s_Instance != null && !s_IsFallback) return s_Instance;

                var loaded = Resources.Load<GameDatabase>(ResourcePath);
                if (loaded != null)
                {
                    bool fresh = s_Instance != loaded;
                    s_Instance = loaded;
                    s_IsFallback = false;
                    if (fresh) loaded.ApplyCommunities();
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

        public Sprite GetCommunityIcon(Community community)
        {
            if (communityIcons == null) return null;
            foreach (var entry in communityIcons)
                if (entry != null && entry.community == community) return entry.icon;
            return null;
        }

        /// <summary>One icon slot per community (icons you assign are kept).</summary>
        public void EnsureAllCommunityIcons()
        {
            if (communityIcons == null) communityIcons = new List<CommunityIcon>();
            communityIcons.RemoveAll(c => c == null || !Enum.IsDefined(typeof(Community), c.community));
            foreach (var community in Communities.All)
            {
                var entry = communityIcons.Find(c => c.community == community);
                if (entry == null) communityIcons.Add(entry = new CommunityIcon { community = community });
                entry.name = community.ToString();
            }
            communityIcons.Sort((a, b) => a.community.CompareTo(b.community));
        }

        /// <summary>
        /// Hands every viewer's communities to the rules (<see cref="GuestCatalog.SetCommunities"/>). Until the database has
        /// been seeded (Deck Party > Setup) the built-in defaults are used.
        /// </summary>
        public void ApplyCommunities()
        {
            GuestCatalog.ResetCommunities();
            if (!communitiesSeeded || guests == null) return;
            // New communities get their default members straight away, even before Setup saves them into the database.
            foreach (var art in guests)
                if (art != null && GuestCatalog.IsValid(art.type)) GuestCatalog.SetCommunities(art.type, WithNewDefaults(art));
        }

        /// <summary>Puts every viewer back on the built-in communities.</summary>
        public void ResetCommunitiesToDefaults()
        {
            foreach (var art in guests)
                if (art != null && GuestCatalog.IsValid(art.type)) art.communities = GuestCatalog.DefaultCommunitiesOf(art.type);
            communitiesSeeded = true;
            communitiesRevision = CommunityDefaultsRevision;
            ApplyCommunities();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ApplyCommunitiesOnLoad() => Instance.ApplyCommunities();

        public Sprite GetGroupIcon(GuestCategory group)
        {
            if (groupIcons == null) return null;
            foreach (var entry in groupIcons)
                if (entry != null && entry.group == group) return entry.icon;
            return null;
        }

        /// <summary>One icon slot per character group (icons you assign are kept).</summary>
        public void EnsureAllGroupIcons()
        {
            if (groupIcons == null) groupIcons = new List<GroupIcon>();
            groupIcons.RemoveAll(g => g == null);
            foreach (GuestCategory group in Enum.GetValues(typeof(GuestCategory)))
            {
                var entry = groupIcons.Find(g => g.group == group);
                if (entry == null) groupIcons.Add(entry = new GroupIcon { group = group });
                entry.name = group == GuestCategory.Tbd ? "TBD (to be determined)" : group.ToString();
            }
            groupIcons.Sort((a, b) => a.group.CompareTo(b.group));
        }

        public StreamerArt GetStreamerArt(StreamerType type)
        {
            if (m_StreamerLookup == null)
            {
                m_StreamerLookup = new Dictionary<StreamerType, StreamerArt>();
                foreach (var art in streamers)
                    if (art != null) m_StreamerLookup[art.type] = art;
            }
            m_StreamerLookup.TryGetValue(type, out var result);
            return result;
        }

        /// <summary>The venue with exactly this id, or null.</summary>
        public VenueDefinition FindVenue(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var list = Venues;
            for (int i = 0; i < list.Count; i++)
                if (list[i] != null && string.Equals(list[i].id, id, StringComparison.OrdinalIgnoreCase)) return list[i];
            return null;
        }

        public VenueDefinition GetVenue(string id)
        {
            var list = Venues;
            for (int i = 0; i < list.Count; i++)
                if (list[i] != null && string.Equals(list[i].id, id, StringComparison.OrdinalIgnoreCase)) return list[i];
            return list.Count > 0 ? list[0] : null;
        }

        /// <summary>Resolves a stream setup choice ("auto", "evolving" or an id) for the given game.</summary>
        public VenueDefinition ResolveVenue(string choice, PlayerState state)
        {
            if (string.IsNullOrEmpty(choice) || choice == AppSettings.VenueAuto)
            {
                // A streamer brings their own setup.
                if (state != null && state.Streamer != StreamerType.None)
                {
                    var own = GetStreamerArt(state.Streamer)?.venue ?? FindVenue(VenuePresets.StreamerStageId(state.Streamer));
                    if (own != null) return own;
                }
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
                (AppSettings.VenueAuto, Loc.Get("venue.auto")),
                (AppSettings.VenueEvolving, Loc.Get("venue.evolving")),
            };
            foreach (var venue in Venues)
                if (venue != null) result.Add((venue.id, VenueName(venue)));
            return result;
        }

        public static string VenueName(VenueDefinition venue) => venue == null ? string.Empty : Loc.RawOr($"venue.{venue.id}", venue.displayName);

        /// <summary>One entry per streamer, in roster order. New entries start with the streamer's original catchphrase as their first quote.</summary>
        public void EnsureAllStreamers()
        {
            streamers.RemoveAll(s => s == null || !StreamerCatalog.IsValid(s.type) || s.type == StreamerType.None);
            foreach (var type in StreamerCatalog.Roster)
            {
                var existing = streamers.Find(s => s.type == type);
                if (existing == null) streamers.Add(existing = new StreamerArt { type = type });
                existing.name = type.ToString();
                if (existing.quotes == null) existing.quotes = new List<string>();
                if (!existing.quotesSeeded)
                {
                    existing.quotesSeeded = true;
                    string original = Loc.EnglishRaw(StreamerVisuals.OriginalQuoteKey(type));
                    if (existing.quotes.Count == 0 && !string.IsNullOrEmpty(original)) existing.quotes.Add(original);
                }
            }
            streamers.Sort((a, b) => a.type.CompareTo(b.type));
            m_StreamerLookup = null;
        }

        /// <summary>
        /// One entry per viewer type, in roster order, matched by a stable id (the GuestType name) so renames and
        /// translations never move art. Entries from an older roster (whose id no longer matches the type at that slot)
        /// are reset so a portrait never shows on the wrong character. Returns how many stale entries were reset.
        /// </summary>
        public int EnsureAllGuests()
        {
            int reset = 0;
            guests.RemoveAll(g => g == null || !GuestCatalog.IsValid(g.type));
            foreach (var info in GuestCatalog.All)
            {
                string key = info.Type.ToString();
                var existing = guests.Find(g => g.type == info.Type);
                if (existing == null) guests.Add(existing = new GuestArt { type = info.Type, communities = GuestCatalog.DefaultCommunitiesOf(info.Type) });
                else
                {
                    // Old databases have no id yet: their (English) name is checked once, then the id takes over.
                    // A viewer renamed in V3 keeps its art: its old id maps to its new type.
                    bool renamed = !string.IsNullOrEmpty(existing.key) && GuestCatalog.RenamedFrom.TryGetValue(existing.key, out var newType) && newType == info.Type;
                    bool stale = renamed ? false
                        : !string.IsNullOrEmpty(existing.key) ? existing.key != key
                        : !string.IsNullOrEmpty(existing.name) && existing.name != info.Name;
                    if (stale)
                    {
                        if (existing.portrait != null || existing.IsAnimated) reset++;
                        existing.portrait = null;
                        existing.characterPrefab = null;
                        existing.animatorController = null;
                        existing.tint = new Color(1f, 1f, 1f, 0f);
                        existing.communities = GuestCatalog.DefaultCommunitiesOf(info.Type);
                    }
                }
                // Databases from before communities start from the built-in ones (once; your edits are kept after that).
                // Communities added later bring their default members in once.
                if (!communitiesSeeded) existing.communities = GuestCatalog.DefaultCommunitiesOf(info.Type);
                else existing.communities = WithNewDefaults(existing);
                existing.key = key;
                existing.name = info.Name;
                existing.displayNameOverride = null;
            }
            guests.Sort((a, b) => a.type.CompareTo(b.type));
            communitiesSeeded = true;
            communitiesRevision = CommunityDefaultsRevision;
            m_GuestLookup = null;
            ApplyCommunities();
            return reset;
        }

        void OnValidate()
        {
            m_GuestLookup = null;
            m_StreamerLookup = null;
            // Community edits in the inspector apply straight away (the next stream uses them).
            if (s_Instance == this) ApplyCommunities();
        }
    }
}
