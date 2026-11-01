using UnityEngine;

namespace DeckParty
{
    public enum VenueStyle
    {
        Apartment,
        Beach,
        Rooftop,
        Mansion,
        Haunted,
        Space,
        Disco,
    }

    /// <summary>
    /// A party background. Assign a <see cref="background"/> image (and optionally a <see cref="foreground"/>);
    /// a coloured placeholder scene is drawn from the palette and style only while the background is empty.
    /// </summary>
    [CreateAssetMenu(menuName = "Deck Party/Venue", fileName = "Venue")]
    public sealed class VenueDefinition : ScriptableObject
    {
        [Tooltip("Stable id used by saves and character sets (e.g. 'bedroom'). Background files named bg_<id> are assigned automatically.")]
        public string id = "venue";
        public string displayName = "Venue";
        [TextArea(2, 4)] public string description;

        [Header("Background image (the placeholder is only drawn while this is empty)")]
        [Tooltip("Full-screen background image. It is scaled to cover the screen while keeping its aspect ratio.")]
        public Sprite background;
        [Tooltip("Optional overlay drawn above the background (furniture, frame, vignette...).")]
        public Sprite foreground;
        [Tooltip("Optional small preview used on the venue selectors.")]
        public Sprite thumbnail;

        [Header("Stream chat bubble")]
        [Tooltip("Where the chat bubble's tail sits along the bubble's top edge (0 = left, 1 = right). Line it up with the computer in this background " +
                 "so the chat seems to come out of it.")]
        [Range(0f, 1f)] public float chatTailPosition = 0.5f;
        [Tooltip("Optional image shown in the strip above the chat bubble during a stream (e.g. the streamer at their desk, with the computer the tail " +
                 "points at). Leave empty to let the background show through.")]
        public Sprite header;

        [Header("Palette (also tints the procedural placeholder)")]
        public Color skyTop = new Color(0.10f, 0.07f, 0.20f);
        public Color skyBottom = new Color(0.30f, 0.12f, 0.35f);
        public Color wallColor = new Color(0.18f, 0.12f, 0.28f);
        public Color floorColor = new Color(0.12f, 0.08f, 0.16f);
        public Color accentA = new Color(1f, 0.31f, 0.55f);
        public Color accentB = new Color(0.24f, 0.86f, 0.85f);

        [Header("Placeholder")]
        public VenueStyle style = VenueStyle.Apartment;
        [Tooltip("Animated party lights drawn over the background. Also used with custom art.")]
        public bool partyLights = true;

        [Header("Audio (optional)")]
        public AudioClip music;

        public Sprite Preview => thumbnail != null ? thumbnail : background;
    }
}
