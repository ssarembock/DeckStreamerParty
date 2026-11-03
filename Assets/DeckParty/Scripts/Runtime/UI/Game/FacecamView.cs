using DeckParty.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// The facecam in the stream's bottom-right corner: a square showing the streamer of the stream on screen (yours, or in
    /// multiplayer the board being shown, which in Take Turns is the active player's) with a name plate, the LIVE badge and
    /// the stream timer. It belongs to the stream, so it hides in the shop. One Deplatform Risk from being deplatformed the
    /// streamer plays Anxious and the frame trembles red; Hit plays when the stream gets deplatformed or the server goes down,
    /// Celebrate when it ends well. Static portraits bob gently. Built as a fixed square (Game scenes from before V4.2 have it
    /// in the side panel, where it fills the row's width unless the side strip is showing).
    /// </summary>
    public sealed class FacecamView : MonoBehaviour
    {
        [SerializeField] internal RectTransform frame;
        [SerializeField] internal Image border;
        [Tooltip("Trembles while the streamer is anxious.")]
        [SerializeField] internal RectTransform content;
        [SerializeField] internal Image glow;
        [SerializeField] internal CharacterPortrait portrait;
        [SerializeField] internal Image placeholder;
        [SerializeField] internal TMP_Text initialsLabel;
        [SerializeField] internal TMP_Text nameLabel;
        [SerializeField] internal Image recDot;
        [Tooltip("Optional strip to the left of the square (the emote buttons in multiplayer).")]
        [SerializeField] internal RectTransform sideStrip;
        [SerializeField] internal LayoutElement layoutElement;
        [Tooltip("Website look: no name plate on the video; hovering it names the streamer (and player).")]
        [SerializeField] internal TooltipTrigger tooltip;

        [Tooltip("Largest square while the side strip is showing.")]
        public float sizeWithSide = 280f;
        [Tooltip("Narrowest the side strip may get.")]
        public float minSideWidth = 100f;
        public float gap = 12f;

        // 254 = nothing shown yet (255 is the Random pick).
        StreamerType m_Streamer = (StreamerType)254;
        bool m_Anxious;
        Color m_Accent = UIPalette.Border;
        float m_LastWidth = -1f;
        bool m_LastSide;

        // Blank until the first state arrives (multiplayer clients wait for the host).
        void Awake()
        {
            if (nameLabel != null) nameLabel.text = string.Empty;
            if (initialsLabel != null) initialsLabel.text = string.Empty;
        }

        /// <param name="playerName">The board owner's name (multiplayer), or null.</param>
        public void Render(PlayerState s, string playerName, Color playerColor, bool anxious)
        {
            if (s == null) return;
            var type = s.Streamer;
            bool none = type == StreamerType.None;
            if (type != m_Streamer)
            {
                m_Streamer = type;
                var art = GameDatabase.Instance.GetStreamerArt(type);
                var sprite = none ? null : StreamerVisuals.Portrait(type);
                if (portrait != null)
                {
                    if (none) portrait.Clear();
                    else portrait.ShowStreamer(type, anxious ? CharacterState.Anxious : CharacterState.Idle);
                }
                bool hasArt = sprite != null || (art != null && art.IsAnimated);
                if (placeholder != null)
                {
                    placeholder.gameObject.SetActive(!hasArt);
                    placeholder.color = none ? UIPalette.PanelAlt : StreamerVisuals.Color(type);
                }
                if (initialsLabel != null) initialsLabel.text = StreamerVisuals.Initials(type);
                m_Accent = none ? UIPalette.Border : StreamerVisuals.Color(type);
                if (glow != null) glow.color = m_Accent.WithAlpha(none ? 0f : 0.28f);
                m_Anxious = anxious;
            }
            else if (anxious != m_Anxious)
            {
                m_Anxious = anxious;
                if (portrait != null) portrait.SetIdle(anxious ? CharacterState.Anxious : CharacterState.Idle);
            }

            string streamerName = none ? GuestVisuals.Colored(Loc.Get("facecam.off"), UIPalette.TextDim) : StreamerVisuals.NameTag(type);
            string caption = playerName == null ? streamerName : $"{streamerName}\n<size=70%>{GuestVisuals.Colored(playerName, playerColor)}</size>";
            if (nameLabel != null) nameLabel.text = caption;
            if (tooltip != null) tooltip.Text = playerName == null ? $"<b>{streamerName}</b>" : $"<b>{streamerName}</b>\n{GuestVisuals.Colored(playerName, playerColor)}";
        }

        /// <summary>A one-shot reaction (Hit, Celebrate, Select). Static portraits just get the punch.</summary>
        public void React(CharacterState state)
        {
            if (frame == null || !isActiveAndEnabled || m_Streamer == StreamerType.None) return;
            bool played = portrait != null && portrait.Play(state);
            Fx.Play(FxCue.CounterPunch, frame, played ? 0.5f : 1f);
        }

        void LateUpdate()
        {
            UpdateLayout();
            float t = Time.unscaledTime;

            if (border != null)
                border.color = m_Anxious ? Color.Lerp(m_Accent, UIPalette.Red, 0.35f + 0.35f * Mathf.Sin(t * 4.5f)) : m_Accent;
            // A light tremble: the warning should read, not shout.
            if (content != null)
                content.localRotation = m_Anxious ? Quaternion.Euler(0f, 0f, Mathf.Sin(t * 29f) * 0.9f + Mathf.Sin(t * 11f) * 0.5f) : Quaternion.identity;

            // Static portraits breathe (N8), growing upward from their feet.
            if (portrait != null && portrait.image != null && !portrait.IsAnimated)
            {
                float breath = FxLibrary.Current.idleBreathing ? 1f + 0.015f * Mathf.Sin(t * 1.4f) : 1f;
                portrait.image.rectTransform.localScale = new Vector3(1f / breath, breath, 1f);
            }

            if (recDot != null) recDot.color = recDot.color.WithAlpha(0.35f + 0.65f * Mathf.Abs(Mathf.Sin(t * 2.2f)));
        }

        /// <summary>Keeps the frame square: full row width, or narrower when the side strip needs room.</summary>
        void UpdateLayout()
        {
            // The V4.2 facecam is a fixed square in the stream's corner: nothing to fit.
            if (layoutElement == null) return;
            var row = (RectTransform)transform;
            float width = row.rect.width;
            bool side = sideStrip != null && sideStrip.gameObject.activeSelf;
            if (Mathf.Abs(width - m_LastWidth) < 0.5f && side == m_LastSide) return;
            m_LastWidth = width;
            m_LastSide = side;

            float size = Mathf.Max(64f, side ? Mathf.Min(sizeWithSide, width - minSideWidth - gap) : width);
            if (layoutElement != null)
            {
                layoutElement.minHeight = size;
                layoutElement.preferredHeight = size;
            }
            if (frame != null)
            {
                frame.anchorMin = frame.anchorMax = new Vector2(1f, 0f);
                frame.pivot = new Vector2(0.5f, 0.5f);
                frame.sizeDelta = new Vector2(size, size);
                frame.anchoredPosition = new Vector2(-size * 0.5f, size * 0.5f);
            }
            if (side)
            {
                sideStrip.anchorMin = Vector2.zero;
                sideStrip.anchorMax = Vector2.one;
                sideStrip.offsetMin = Vector2.zero;
                sideStrip.offsetMax = new Vector2(-(size + gap), 0f);
            }
        }
    }
}
