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

        public event Action Clicked;

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
            if (background != null) background.color = isLocal ? Color.Lerp(UIPalette.PanelAlt, color, 0.15f) : UIPalette.PanelAlt;
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
            colors.highlightedColor = Color.Lerp(Color.white, UIPalette.Teal, 0.35f);
            colors.pressedColor = Color.Lerp(Color.white, UIPalette.Teal, 0.6f);
            colors.selectedColor = Color.white;
            m_Button.colors = colors;
            var navigation = m_Button.navigation;
            navigation.mode = Navigation.Mode.None;
            m_Button.navigation = navigation;
            m_Button.onClick.AddListener(() => Clicked?.Invoke());
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
            m_Outline.enabled = viewed;
            m_Outline.effectColor = color;
            if (viewed) background.color = Color.Lerp(UIPalette.PanelAlt, color, 0.3f);
        }
    }
}
