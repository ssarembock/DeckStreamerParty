using System;
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

        public event Action Clicked;

        Button m_Button;
        Outline m_Outline;

        public void Set(string playerName, string status, Color color, bool ready, bool host, int rank = 0, bool isLocal = false)
        {
            nameLabel.text = isLocal ? playerName + "  <size=70%><color=#B9AED6>(you)</color></size>" : playerName;
            statusLabel.text = status;
            colorSwatch.color = color;
            readyIcon.SetActive(ready);
            hostIcon.SetActive(host);
            rankLabel.gameObject.SetActive(rank > 0);
            rankLabel.text = rank > 0 ? "#" + rank : string.Empty;
            if (background != null) background.color = isLocal ? Color.Lerp(UIPalette.PanelAlt, color, 0.15f) : UIPalette.PanelAlt;
        }

        /// <summary>Turns the row into a button (the in-game Players panel uses it to watch someone's party).</summary>
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

        /// <summary>Outlines the row whose party is on screen. Call after <see cref="Set"/>.</summary>
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
