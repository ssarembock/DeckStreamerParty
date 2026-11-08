using System;
using UnityEngine;

namespace DeckParty
{
    /// <summary>
    /// The satirical streaming platform a streamer broadcasts on. Purely visual: it picks the animated logo at the top-left of
    /// the stream and shop (since 2026-11-08 nothing else changes per platform). Set per streamer in the Game Database. The names describe
    /// the parody logos; the real sites are never named in the game.
    /// </summary>
    public enum StreamPlatform
    {
        [InspectorName("Cough (logo coughs, money flies out)")] Cough = 0,
        [InspectorName("Lookout (balloon with binoculars)")] Lookout = 1,
        [InspectorName("Post (text-post site)")] Post = 2,
        [InspectorName("Stomp (boot squashes an emoticon)")] Stomp = 3,
        [InspectorName("Tunes (music and podcasts)")] Tunes = 4,
        [InspectorName("Swipe (clock with a sweeping hand)")] Swipe = 5,
    }

    /// <summary>
    /// Art and tuning for one platform's animated logo (Game Database > Platforms). What each sprite slot and placement value
    /// means depends on the platform: <see cref="PartLabel"/> and <see cref="ValueLabel"/> name them, and the inspector shows
    /// those names. Empty slots use built-in placeholder shapes, so every logo animates before any art exists.
    /// </summary>
    [Serializable]
    public sealed class PlatformArt
    {
        [HideInInspector] public string name;
        public StreamPlatform platform;

        [Tooltip("Colour of the built-in placeholder logo (only used while the logo has no sprite). Buttons no longer change per platform.")]
        public Color accent = SitePalette.Accent;
        [Tooltip("Detail colour of the built-in placeholder logo (only used while the logo has no sprite).")]
        public Color accentContent = Color.white;
        [Tooltip("The logo's box, in pixels at 1080p.")]
        [Range(48f, 140f)] public float size = 84f;

        [Tooltip("The main logo sprite (see the label in the Game Database).")]
        public Sprite logo;
        public Sprite partA;
        public Sprite partB;
        public Sprite partC;

        [Tooltip("A point in the logo box: 0,0 is bottom-left and 1,1 top-right.")]
        public Vector2 pointA = new Vector2(0.5f, 0.5f);
        public Vector2 pointB = new Vector2(0.5f, 0.5f);
        [Range(0.03f, 1.2f)] public float partSize = 0.3f;
        [Range(0f, 2f)] public float travel = 0.5f;

        [Tooltip("Animation tempo (2 = twice as fast).")]
        [Range(0.25f, 3f)] public float speed = 1f;
        [Tooltip("Seconds of rest between loops.")]
        [Range(0f, 8f)] public float rest = 1.2f;

        /// <summary>A platform entry with its built-in colours and placements (no sprites).</summary>
        public static PlatformArt CreateDefault(StreamPlatform platform)
        {
            var art = new PlatformArt { platform = platform, name = Title(platform), accent = DefaultAccent(platform), accentContent = DefaultContent(platform) };
            switch (platform)
            {
                case StreamPlatform.Cough:
                    art.pointA = new Vector2(0.9f, 0.45f);
                    art.partSize = 0.28f;
                    art.travel = 1.1f;
                    art.rest = 1.4f;
                    break;
                case StreamPlatform.Lookout:
                    art.pointA = new Vector2(0.37f, 0.6f);
                    art.pointB = new Vector2(0.63f, 0.6f);
                    art.partSize = 0.12f;
                    art.travel = 0.05f;
                    art.rest = 0.5f;
                    break;
                case StreamPlatform.Stomp:
                    art.pointA = new Vector2(0.5f, 0.28f);
                    art.partSize = 0.46f;
                    art.travel = 0.24f;
                    art.rest = 1.1f;
                    break;
                case StreamPlatform.Swipe:
                    art.partSize = 0.42f;
                    art.rest = 0.35f;
                    break;
                default:
                    art.rest = 2.4f;
                    break;
            }
            return art;
        }

        /// <summary>Button colours, taken from the parodied sites' own colour schemes (artistic reference only).</summary>
        public static Color DefaultAccent(StreamPlatform platform)
        {
            switch (platform)
            {
                case StreamPlatform.Cough: return UIPalette.Hex(0xFF2D2D);
                case StreamPlatform.Lookout: return UIPalette.Hex(0x9146FF);
                case StreamPlatform.Post: return UIPalette.Hex(0x0F1419);
                case StreamPlatform.Stomp: return UIPalette.Hex(0x53FC18);
                case StreamPlatform.Tunes: return UIPalette.Hex(0x1DB954);
                case StreamPlatform.Swipe: return UIPalette.Hex(0xFE2C55);
                default: return SitePalette.Accent;
            }
        }

        public static Color DefaultContent(StreamPlatform platform) =>
            platform == StreamPlatform.Stomp || platform == StreamPlatform.Tunes ? UIPalette.Hex(0x0B0E0F) : Color.white;

        public static string Title(StreamPlatform platform) => platform.ToString();

        /// <summary>What a sprite slot holds on this platform (0 = Logo, 1-3 = parts A-C), or null when the platform doesn't use it.</summary>
        public static string PartLabel(StreamPlatform platform, int slot)
        {
            switch (platform)
            {
                case StreamPlatform.Cough:
                    return slot == 0 ? "Logo (the character that coughs)" : slot == 1 ? "Money (flies out)" : slot == 2 ? "Cough lines (all three in one sprite)" : null;
                case StreamPlatform.Lookout:
                    return slot == 0 ? "Balloon with binoculars" : slot == 1 ? "Pupil (black, drawn on each lens)" : null;
                case StreamPlatform.Stomp:
                    return slot == 0 ? "Emoticon (gets squashed)" : slot == 1 ? "Boot" : slot == 2 ? "Impact effect" : null;
                case StreamPlatform.Swipe:
                    return slot == 0 ? "Clock face: white face, black markers" : slot == 1 ? "Clock face: black face, white markers"
                        : slot == 2 ? "Hand (black, pointing up)" : "Hand (white, pointing up; optional)";
                default:
                    return slot == 0 ? "Logo" : null;
            }
        }

        /// <summary>What a placement value means on this platform ("pointA", "pointB", "partSize", "travel"), or null when unused.</summary>
        public static string ValueLabel(StreamPlatform platform, string field)
        {
            switch (platform)
            {
                case StreamPlatform.Cough:
                    return field == "pointA" ? "Mouth (where money and lines come out)" : field == "partSize" ? "Money size" : field == "travel" ? "How far money flies" : null;
                case StreamPlatform.Lookout:
                    return field == "pointA" ? "Left lens centre" : field == "pointB" ? "Right lens centre" : field == "partSize" ? "Pupil size" : field == "travel" ? "How far pupils move" : null;
                case StreamPlatform.Stomp:
                    return field == "pointA" ? "Impact point (top of the emoticon)" : field == "partSize" ? "Boot size" : field == "travel" ? "How high the boot lifts" : null;
                case StreamPlatform.Swipe:
                    return field == "partSize" ? "Hand length (0.5 = the box's radius)" : null;
                default:
                    return null;
            }
        }
    }
}
