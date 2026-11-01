using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// The stream's chat as a big chat bubble: a 9-sliced bubble behind the viewers in chat, with a tail on its top edge
    /// pointing up at the computer in the scene above (the stream setup's background, or its optional header image).
    /// Art comes from the Game Database (Stream Chat Bubble); the tail's spot along the edge comes from the stream setup
    /// (Venue > Chat Tail Position). Until art is assigned a rounded panel and a simple triangle stand in.
    /// </summary>
    public sealed class ChatBubbleView : MonoBehaviour
    {
        [SerializeField] internal Image bubble;
        [Tooltip("Child of the bubble, anchored to its top edge; it sits above the bubble and below the viewer cards.")]
        [SerializeField] internal Image tail;
        [Tooltip("Optional: the stream setup's header image in the strip above the bubble.")]
        [SerializeField] internal Image header;
        [Tooltip("Tail height in pixels. Its width follows the sprite's proportions.")]
        [SerializeField] internal float tailHeight = 110f;
        [Tooltip("How far the tail overlaps the bubble's top edge, so the two join seamlessly.")]
        [SerializeField] internal float tailOverlap = 10f;

        static readonly Color PlaceholderColor = new Color(0.13f, 0.10f, 0.22f, 0.86f);

        VenueDefinition m_Venue;
        Sprite m_DefaultBubble;
        bool m_Captured;

        void OnEnable() => Refresh();

        /// <summary>The sprite the scene was built with stands in until a chat bubble is assigned.</summary>
        void CaptureDefaults()
        {
            if (m_Captured) return;
            m_Captured = true;
            if (bubble != null) m_DefaultBubble = bubble.sprite;
        }

        /// <summary>Follows the stream setup on screen (its tail position and header image).</summary>
        public void Apply(VenueDefinition venue)
        {
            m_Venue = venue;
            Refresh();
        }

        public void Refresh()
        {
            CaptureDefaults();
            var db = GameDatabase.Instance;
            bool customBubble = db.chatBubble != null;
            var color = customBubble ? db.chatBubbleTint : PlaceholderColor;

            if (bubble != null)
            {
                var sprite = customBubble ? db.chatBubble : m_DefaultBubble != null ? m_DefaultBubble : RuntimeSprites.Rounded;
                bubble.sprite = sprite;
                bubble.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
                bubble.color = color;
            }

            if (tail != null)
            {
                var sprite = db.chatBubbleTail != null ? db.chatBubbleTail : RuntimeSprites.TailDown;
                tail.sprite = sprite;
                tail.type = Image.Type.Simple;
                tail.preserveAspect = true;
                // The tail matches the bubble unless it's your own art (which carries its own colours, tinted like the bubble).
                tail.color = db.chatBubbleTail != null ? db.chatBubbleTint : color;

                float aspect = sprite != null && sprite.rect.height > 0f ? sprite.rect.width / sprite.rect.height : 1f;
                float x = m_Venue != null ? Mathf.Clamp01(m_Venue.chatTailPosition) : 0.5f;
                var rect = tail.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(x, 1f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.sizeDelta = new Vector2(tailHeight * aspect, tailHeight);
                rect.anchoredPosition = new Vector2(0f, -tailOverlap);
            }

            if (header != null)
            {
                var sprite = m_Venue != null ? m_Venue.header : null;
                header.enabled = sprite != null;
                header.sprite = sprite;
                header.preserveAspect = true;
            }
        }
    }
}
