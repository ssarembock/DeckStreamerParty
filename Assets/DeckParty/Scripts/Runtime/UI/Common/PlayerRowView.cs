using System;
using DeckParty.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>A player entry for the lobby, the in-game opponents panel and the results table.</summary>
    public sealed class PlayerRowView : MonoBehaviour
    {
        [SerializeField] internal Image background;
        [SerializeField] internal Image colorSwatch;
        [SerializeField] internal TMP_Text nameLabel;
        [SerializeField] internal TMP_Text statusLabel;
        [SerializeField] internal TMP_Text rankLabel;
        [SerializeField] internal GameObject readyIcon;
        [SerializeField] internal GameObject hostIcon;
        [Tooltip("Optional: the player's streamer portrait.")]
        [SerializeField] internal Image streamerPortrait;
        [SerializeField] internal Image streamerPlaceholder;
        [SerializeField] internal TMP_Text streamerInitials;
        [Tooltip("Optional (in-game Players panel): mutes this player's emotes on your screen. Hidden on your own row.")]
        [SerializeField] internal Button muteButton;
        [Tooltip("The mute button's white fill; it turns red while the player is muted.")]
        [SerializeField] internal Image muteFill;
        [SerializeField] internal TooltipTrigger muteTooltip;
        [Tooltip("Optional (lobby, online platforms): the player's Steam avatar, where the rank sits in the results.")]
        [SerializeField] internal RawImage platformAvatar;

        public event Action Clicked;
        /// <summary>The mute button was pressed (the Players panel toggles that player's emotes).</summary>
        public event Action MuteClicked;

        /// <summary>The player's avatar: their streamer portrait, or the colour swatch for players without a streamer (emotes pop up from here).</summary>
        public RectTransform AvatarRect
        {
            get
            {
                if (streamerPortrait != null && streamerPortrait.transform.parent.gameObject.activeInHierarchy)
                    return (RectTransform)streamerPortrait.transform.parent;
                return colorSwatch != null ? colorSwatch.rectTransform : (RectTransform)transform;
            }
        }

        Button m_Button;
        Outline m_Outline;
        string m_Name;
        string m_Status;
        bool m_IsLocal;
        StreamerType m_Streamer;
        string m_Presence;

        /// <summary>
        /// The in-game Players panel (2026-11-18): the row takes its colours from UI Skin > Players and emotes (row, your row,
        /// the stream on screen, hover). The lobby and results rows keep the built-in dark look.
        /// </summary>
        public bool UseSkinColours { get; set; }

        public void Set(string playerName, string status, Color color, bool ready, bool host, int rank = 0, bool isLocal = false)
        {
            m_Name = playerName;
            m_Status = status;
            m_IsLocal = isLocal;
            RefreshName();
            RefreshStatus();
            colorSwatch.color = color;
            readyIcon.SetActive(ready);
            hostIcon.SetActive(host);
            rankLabel.gameObject.SetActive(rank > 0);
            rankLabel.text = rank > 0 ? "#" + rank : string.Empty;
            if (background != null)
                background.color = UseSkinColours ? SidePanelLook.OrDefault.RowColour(color, isLocal, false)
                    : isLocal ? Color.Lerp(UIPalette.PanelAlt, color, 0.15f) : UIPalette.PanelAlt;
            ApplyButtonTints();
        }

        /// <summary>Shows the player's streamer (portrait if the row has one, otherwise after the name).</summary>
        public void SetStreamer(StreamerType streamer)
        {
            m_Streamer = streamer;
            bool show = streamer != StreamerType.None;
            if (streamerPortrait != null)
            {
                var sprite = show ? StreamerVisuals.Portrait(streamer) : null;
                streamerPortrait.transform.parent.gameObject.SetActive(show);
                streamerPortrait.enabled = sprite != null;
                streamerPortrait.sprite = sprite;
                if (streamerPlaceholder != null)
                {
                    streamerPlaceholder.gameObject.SetActive(sprite == null);
                    streamerPlaceholder.color = StreamerVisuals.Color(streamer);
                }
                if (streamerInitials != null) streamerInitials.text = show ? StreamerVisuals.Initials(streamer) : string.Empty;
            }
            RefreshName();
        }

        /// <summary>The player's platform avatar (Steam) in the lobby; null hides it (no platform, or still downloading).</summary>
        public void SetPlatformAvatar(Texture texture)
        {
            if (platformAvatar == null) return;
            platformAvatar.texture = texture;
            platformAvatar.gameObject.SetActive(texture != null);
        }

        /// <summary>Multiplayer presence: what the player is doing right now, shown under their status. Null clears it.</summary>
        public void SetPresence(string line)
        {
            if (line == m_Presence) return;
            m_Presence = line;
            RefreshStatus();
        }

        void RefreshName()
        {
            if (nameLabel == null || m_Name == null) return;
            string text = m_Name;
            if (m_Streamer != StreamerType.None) text += $"  <size=75%>{StreamerVisuals.NameTag(m_Streamer)}</size>";
            if (m_IsLocal) text += $"  <size=70%><color=#B9AED6>{Loc.Get("players.you")}</color></size>";
            nameLabel.text = text;
        }

        void RefreshStatus()
        {
            if (statusLabel == null) return;
            statusLabel.text = string.IsNullOrEmpty(m_Presence) ? m_Status : m_Status + $"\n<i><color=#FF9AD5>{m_Presence}</color></i>";
        }

        /// <summary>Turns the row into a button (the in-game Players panel uses it to watch someone's stream).</summary>
        public void MakeClickable()
        {
            if (m_Button != null || background == null) return;
            if (!TryGetComponent(out m_Button)) m_Button = gameObject.AddComponent<Button>();
            background.raycastTarget = true;
            m_Button.targetGraphic = background;
            var colors = m_Button.colors;
            var look = UseSkinColours ? SidePanelLook.OrDefault : null;
            colors.highlightedColor = look != null ? look.rowHoverTint : Color.Lerp(Color.white, UIPalette.Teal, 0.35f);
            colors.pressedColor = look != null ? look.rowPressedTint : Color.Lerp(Color.white, UIPalette.Teal, 0.6f);
            colors.selectedColor = Color.white;
            m_Button.colors = colors;
            var navigation = m_Button.navigation;
            navigation.mode = Navigation.Mode.None;
            m_Button.navigation = navigation;
            m_Button.onClick.AddListener(() => Clicked?.Invoke());
        }

        /// <summary>The in-game rows follow UI Skin > Players and emotes for their hover and press tints (live while playing).</summary>
        void ApplyButtonTints()
        {
            if (m_Button == null || !UseSkinColours) return;
            var look = SidePanelLook.OrDefault;
            var colors = m_Button.colors;
            if (colors.highlightedColor == look.rowHoverTint && colors.pressedColor == look.rowPressedTint) return;
            colors.highlightedColor = look.rowHoverTint;
            colors.pressedColor = look.rowPressedTint;
            m_Button.colors = colors;
        }

        bool m_MuteHooked;

        /// <summary>
        /// Shows the mute button (other players' rows in the in-game Players panel) and whether that player is muted: a red
        /// fill while their emotes are hidden. <paramref name="available"/> false hides it (your own row, the lobby, results).
        /// </summary>
        public void SetMute(bool available, bool muted)
        {
            if (muteButton == null) return;
            if (!m_MuteHooked)
            {
                m_MuteHooked = true;
                muteButton.onClick.AddListener(() => MuteClicked?.Invoke());
            }
            muteButton.gameObject.SetActive(available);
            if (!available) return;
            if (muteFill != null) muteFill.color = muted ? SitePalette.Danger : SitePalette.Surface;
            if (muteTooltip != null) muteTooltip.Text = Loc.Get(muted ? "players.unmute" : "players.mute", "player", m_Name ?? string.Empty);
        }

        /// <summary>
        /// The in-game Players panel (narrower since 2026-11-18, three quarters of its old width): packs the row from both
        /// ends so the name and status get every pixel the visible pieces leave. Left: the rank (only when shown), a smaller
        /// colour dot and the streamer; right: the mute button, the ready tick and the host crown, only those showing.
        /// Call after <see cref="Set"/>, <see cref="SetStreamer"/> and <see cref="SetMute"/>.
        /// </summary>
        public void FitCompact()
        {
            const float edge = 8f;
            const float gap = 6f;
            float left = edge;
            if (rankLabel != null && rankLabel.gameObject.activeSelf)
            {
                PlaceLeft(rankLabel.rectTransform, left, 40f, 40f);
                left += 40f + gap;
            }
            if (colorSwatch != null)
            {
                PlaceLeft(colorSwatch.rectTransform, left, 18f, 18f);
                left += 18f + gap;
            }
            var streamerFrame = streamerPortrait != null ? streamerPortrait.transform.parent as RectTransform : null;
            if (streamerFrame != null && streamerFrame.gameObject.activeSelf)
            {
                PlaceLeft(streamerFrame, left, 46f, 46f);
                left += 46f + gap + 2f;
            }

            float right = edge;
            if (muteButton != null && muteButton.gameObject.activeSelf)
            {
                PlaceRight((RectTransform)muteButton.transform, right, 38f);
                right += 38f + gap;
            }
            if (readyIcon != null && readyIcon.activeSelf)
            {
                PlaceRight((RectTransform)readyIcon.transform, right, 30f);
                right += 30f + gap;
            }
            if (hostIcon != null && hostIcon.activeSelf)
            {
                PlaceRight((RectTransform)hostIcon.transform, right, 28f);
                right += 28f + gap;
            }

            if (nameLabel != null)
            {
                nameLabel.rectTransform.offsetMin = new Vector2(left, nameLabel.rectTransform.offsetMin.y);
                nameLabel.rectTransform.offsetMax = new Vector2(-right, nameLabel.rectTransform.offsetMax.y);
                nameLabel.enableAutoSizing = true;
                nameLabel.fontSizeMin = 16f;
                nameLabel.fontSizeMax = 24f;
            }
            if (statusLabel != null)
            {
                statusLabel.rectTransform.offsetMin = new Vector2(left, statusLabel.rectTransform.offsetMin.y);
                statusLabel.rectTransform.offsetMax = new Vector2(-right, statusLabel.rectTransform.offsetMax.y);
            }
        }

        static void PlaceLeft(RectTransform rect, float x, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, 0f);
        }

        static void PlaceRight(RectTransform rect, float inset, float size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = new Vector2(-inset, 0f);
        }

        /// <summary>Outlines the row whose stream is on screen. Call after <see cref="Set"/>.</summary>
        public void SetViewed(bool viewed, Color color)
        {
            if (background == null) return;
            if (m_Outline == null)
            {
                if (!viewed) return;
                if (!TryGetComponent(out m_Outline)) m_Outline = gameObject.AddComponent<Outline>();
                m_Outline.effectDistance = new Vector2(3f, -3f);
            }
            var skin = UseSkinColours ? SidePanelLook.OrDefault : null;
            m_Outline.enabled = viewed && (skin == null || skin.watchedOutline);
            m_Outline.effectColor = skin != null && skin.watchedOutlineColour.use ? skin.watchedOutlineColour.colour : color;
            if (viewed) background.color = skin != null ? skin.RowColour(color, m_IsLocal, true) : Color.Lerp(UIPalette.PanelAlt, color, 0.3f);
        }
    }
}
