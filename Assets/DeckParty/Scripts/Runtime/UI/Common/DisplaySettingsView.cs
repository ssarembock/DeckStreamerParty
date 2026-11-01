using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// The Display &amp; Language block shared by the main menu Settings screen and the in-game Menu: language, window mode,
    /// resolution (with a 10-second "keep these settings?" countdown), screen shake, reduce flashing and the fake chat.
    /// </summary>
    public sealed class DisplaySettingsView : MonoBehaviour
    {
        const float RevertSeconds = 10f;

        [SerializeField] internal OptionSelector languageSelector;
        [SerializeField] internal OptionSelector modeSelector;
        [SerializeField] internal OptionSelector resolutionSelector;
        [SerializeField] internal Button applyButton;
        [SerializeField] internal TMP_Text applyLabel;
        [SerializeField] internal Slider shakeSlider;
        [SerializeField] internal ToggleSwitch reduceFlashingToggle;
        [SerializeField] internal ToggleSwitch fakeChatToggle;

        readonly List<string> m_LanguageCodes = new List<string>();
        List<DisplayResolution> m_Resolutions;
        bool m_Fullscreen;

        // Revert countdown after Apply
        bool m_Confirming;
        float m_ConfirmLeft;
        bool m_PreviousFullscreen;
        int m_PreviousWidth;
        int m_PreviousHeight;

        void Awake()
        {
            if (languageSelector != null) languageSelector.Changed += OnLanguagePicked;
            if (modeSelector != null) modeSelector.Changed += index =>
            {
                m_Fullscreen = index == 1;
                RefreshResolutions();
            };
            if (applyButton != null) applyButton.onClick.AddListener(OnApply);
            if (shakeSlider != null) shakeSlider.onValueChanged.AddListener(v => AppSettings.ScreenShake = v);
            if (reduceFlashingToggle != null) reduceFlashingToggle.Changed += v => AppSettings.ReduceFlashing = v;
            if (fakeChatToggle != null) fakeChatToggle.Changed += v => AppSettings.FakeChat = v;
            Loc.Changed += Refresh;
        }

        void OnDestroy() => Loc.Changed -= Refresh;

        void OnEnable() => Refresh();

        void OnDisable()
        {
            // Closing the menu mid-countdown keeps the new settings (the player clearly saw them).
            m_Confirming = false;
        }

        public void Refresh()
        {
            if (languageSelector != null)
            {
                m_LanguageCodes.Clear();
                var labels = new List<string>();
                foreach (var language in Loc.Languages)
                {
                    m_LanguageCodes.Add(language.Code);
                    labels.Add(language.NativeName);
                }
                if (Loc.PseudoAvailable)
                {
                    m_LanguageCodes.Add(Loc.Pseudo);
                    labels.Add("English (Pseudo)");
                }
                languageSelector.SetTitle(Loc.Get("settings.language"));
                languageSelector.SetOptions(labels, Mathf.Max(0, m_LanguageCodes.IndexOf(Loc.LanguageCode)));
            }

            m_Fullscreen = DisplaySettings.IsFullscreen;
            if (modeSelector != null)
            {
                modeSelector.SetTitle(Loc.Get("settings.window_mode"));
                modeSelector.SetOptions(new[] { Loc.Get("settings.windowed"), Loc.Get("settings.fullscreen") }, m_Fullscreen ? 1 : 0);
            }
            RefreshResolutions();

            if (shakeSlider != null) shakeSlider.SetValueWithoutNotify(AppSettings.ScreenShake);
            if (reduceFlashingToggle != null) reduceFlashingToggle.Value = AppSettings.ReduceFlashing;
            if (fakeChatToggle != null) fakeChatToggle.Value = AppSettings.FakeChat;
            UpdateApplyLabel();
        }

        void RefreshResolutions()
        {
            m_Resolutions = DisplaySettings.Available(m_Fullscreen);
            if (resolutionSelector == null) return;
            var labels = new List<string>();
            foreach (var r in m_Resolutions) labels.Add(r.Label);
            resolutionSelector.SetTitle(Loc.Get("settings.resolution"));
            resolutionSelector.SetOptions(labels, DisplaySettings.ClosestIndex(m_Resolutions));
        }

        void OnLanguagePicked(int index)
        {
            if (index >= 0 && index < m_LanguageCodes.Count) Loc.SetLanguage(m_LanguageCodes[index]);
        }

        void OnApply()
        {
            if (m_Confirming)
            {
                // Second press: keep.
                m_Confirming = false;
                UpdateApplyLabel();
                AudioService.Play(Sfx.Click);
                return;
            }
            if (m_Resolutions == null || m_Resolutions.Count == 0) return;
            var pick = m_Resolutions[Mathf.Clamp(resolutionSelector != null ? resolutionSelector.Index : 0, 0, m_Resolutions.Count - 1)];

            m_PreviousFullscreen = DisplaySettings.IsFullscreen;
            m_PreviousWidth = Screen.width;
            m_PreviousHeight = Screen.height;
            DisplaySettings.Apply(m_Fullscreen, pick.Width, pick.Height);
            if (Application.isEditor)
            {
                ToastView.Show(Loc.Get("settings.editor_note"), ToastKind.Info);
                return;
            }
            m_Confirming = true;
            m_ConfirmLeft = RevertSeconds;
            UpdateApplyLabel();
        }

        void Update()
        {
            if (!m_Confirming) return;
            m_ConfirmLeft -= Time.unscaledDeltaTime;
            if (m_ConfirmLeft <= 0f)
            {
                m_Confirming = false;
                DisplaySettings.Apply(m_PreviousFullscreen, m_PreviousWidth, m_PreviousHeight);
                ToastView.Show(Loc.Get("settings.reverted"), ToastKind.Info);
                Refresh();
                return;
            }
            UpdateApplyLabel();
        }

        void UpdateApplyLabel()
        {
            if (applyLabel == null) return;
            applyLabel.text = m_Confirming
                ? Loc.Get("settings.keep", "seconds", Mathf.CeilToInt(m_ConfirmLeft))
                : Loc.Get("settings.apply");
        }
    }
}
