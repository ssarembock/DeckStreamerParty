using TMPro;
using UnityEngine;

namespace DeckParty
{
    /// <summary>
    /// Shows a localized string on a static caption and updates it when the language changes. The scene builders add one
    /// to every caption; to reword a caption, edit its key's text in the CSV (or the Unity string table), not the scene.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    [DisallowMultipleComponent]
    public sealed class LocalizedLabel : MonoBehaviour
    {
        [Tooltip("Key in AudienceCapture_Strings.csv, e.g. ui.menu.single_player")]
        public string key;
        [Tooltip("Optional text placed before the localized string (icons, hotkey hints).")]
        public string prefix;
        [Tooltip("Optional text placed after the localized string.")]
        public string suffix;

        TMP_Text m_Text;

        public void SetKey(string newKey)
        {
            key = newKey;
            Apply();
        }

        void OnEnable()
        {
            Loc.Changed += Apply;
            Apply();
        }

        void OnDisable() => Loc.Changed -= Apply;

        public void Apply()
        {
            if (string.IsNullOrEmpty(key)) return;
            if (m_Text == null) m_Text = GetComponent<TMP_Text>();
            if (m_Text != null) m_Text.text = prefix + Loc.Get(key) + suffix;
        }
    }
}
