using TMPro;
using UnityEngine;

namespace DeckParty.UI
{
    /// <summary>
    /// Applies the UI Skin's font to this text (if a font is set on the skin), and its colour from UI Skin > Text Colours when
    /// the entry with this text's <see cref="ColourId"/> overrides the colour. The override wins over the game's own colour
    /// (it is re-applied after the views write theirs); the text's alpha is kept, so fades still work.
    /// </summary>
    [ExecuteAlways]
    public sealed class SkinnedText : MonoBehaviour
    {
        [SerializeField] internal bool header;
        [Tooltip("This text's entry in UI Skin > Text Colours (set by the scene builders; empty: the text has no entry).")]
        [SerializeField] internal string colourId;
        [Tooltip("The colour the builder gave this text (restored in the editor when you untick its override).")]
        [SerializeField] internal Color builtColour = Color.white;
        [SerializeField] internal bool hasBuiltColour;
        [Tooltip("Only the colour comes from the UI Skin (this text keeps its own font).")]
        [SerializeField] internal bool colourOnly;

        TMP_Text m_Text;
        bool m_Overriding;
        Color m_Colour;

        public string ColourId => colourId;

        /// <summary>True while UI Skin > Text Colours gives this text its own colour.</summary>
        public bool OverridesColour => m_Overriding;

        /// <summary>
        /// Gives a text made by code (not by a scene builder) a Text Colours id. The id must be listed in
        /// <see cref="TextColourIds"/> so the UI Skin shows it.
        /// </summary>
        public static SkinnedText Attach(TMP_Text text, string id)
        {
            if (text == null) return null;
            if (!text.TryGetComponent<SkinnedText>(out var skinned)) skinned = text.gameObject.AddComponent<SkinnedText>();
            skinned.colourId = id;
            skinned.builtColour = text.color;
            skinned.hasBuiltColour = true;
            skinned.Apply();
            return skinned;
        }

        void OnEnable()
        {
            UISkin.SkinChanged += Apply;
            Apply();
        }

        void OnDisable() => UISkin.SkinChanged -= Apply;

        public void Apply()
        {
            if (this == null) return;
            var skin = GameDatabase.Instance.skin;
            if (skin == null) return;
            if (m_Text == null) m_Text = GetComponent<TMP_Text>();
            var font = header && skin.headerFont != null ? skin.headerFont : skin.bodyFont;
            if (!colourOnly && font != null && m_Text != null) m_Text.font = font;

            bool was = m_Overriding;
            m_Overriding = skin.TryGetTextColour(colourId, out m_Colour);
            if (m_Overriding) ApplyColour();
            else if (was) RestoreColour();
        }

        // After the views (Update) and the readability fixes, so the chosen colour is the one drawn.
        void LateUpdate()
        {
            if (m_Overriding) ApplyColour();
        }

        void ApplyColour()
        {
            if (m_Text == null && !TryGetComponent(out m_Text)) return;
            var current = m_Text.color;
            var wanted = new Color(m_Colour.r, m_Colour.g, m_Colour.b, current.a);
            if (current != wanted) m_Text.color = wanted;
        }

        /// <summary>The override was turned off: give the text back the colour it had (its Site Colours role, else the built one).</summary>
        void RestoreColour()
        {
            if (m_Text == null && !TryGetComponent(out m_Text)) return;
            if (TryGetComponent<SiteColour>(out var site)) site.Apply();
            else if (hasBuiltColour)
            {
                var current = m_Text.color;
                m_Text.color = new Color(builtColour.r, builtColour.g, builtColour.b, current.a);
            }
        }
    }
}
