using TMPro;
using UnityEngine;

namespace DeckParty.UI
{
    /// <summary>
    /// Put on a text that sits on a white pop-up: whatever a view writes into it, pale colours (its base colour and any
    /// colour tags) are darkened so it stays readable (see <see cref="LightText"/>). The views keep writing their usual
    /// rich text, so the dark menus and the light pop-ups can share the same strings.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class ReadableOnLight : MonoBehaviour
    {
        TMP_Text m_Text;
        string m_Seen;
        Color m_SeenColor;

        void Awake() => m_Text = GetComponent<TMP_Text>();

        void OnEnable() => Apply();

        void LateUpdate() => Apply();

        void Apply()
        {
            if (m_Text == null) return;
            var color = m_Text.color;
            if (color != m_SeenColor)
            {
                // On a panel with a dark inside (UI Skin > Site Colours) pale colours are what reads, so they're kept.
                var readable = LightText.ReadableOn(transform, color);
                if (readable != color) m_Text.color = readable;
                m_SeenColor = m_Text.color;
            }
            // Only re-check when a view wrote new text (reference compare first: cheap every frame).
            var text = m_Text.text;
            if (ReferenceEquals(text, m_Seen)) return;
            var adapted = LightText.AdaptOn(transform, text);
            if (!ReferenceEquals(adapted, text) && adapted != text) m_Text.text = adapted;
            m_Seen = m_Text.text;
        }
    }
}
