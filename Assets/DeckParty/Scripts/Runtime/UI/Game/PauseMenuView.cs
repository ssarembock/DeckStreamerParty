using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>In-game menu: stream setup, display and language, volume, quick rules, quit / leave.</summary>
    public sealed class PauseMenuView : MonoBehaviour
    {
        [SerializeField] internal GameObject root;
        [SerializeField] internal TMP_Text titleLabel;
        [SerializeField] internal Button resumeButton;
        [SerializeField] internal OptionSelector venueSelector;
        [SerializeField] internal Slider masterSlider;
        [SerializeField] internal Slider musicSlider;
        [SerializeField] internal Slider sfxSlider;
        [SerializeField] internal ToggleSwitch fastAnimationsToggle;
        [SerializeField] internal DisplaySettingsView display;
        [SerializeField] internal TMP_Text rulesText;
        [SerializeField] internal Button quitButton;
        [SerializeField] internal TMP_Text quitLabel;

        public event Action QuitClicked;
        public event Action<string> VenueChosen;
        public event Action<bool> OpenChanged;

        List<(string id, string label)> m_Venues;
        string m_VenueChoice;
        bool m_Multiplayer;

        public bool IsOpen => root.activeSelf;

        void Awake()
        {
            resumeButton.onClick.AddListener(Close);
            quitButton.onClick.AddListener(() => QuitClicked?.Invoke());
            masterSlider.onValueChanged.AddListener(v => AppSettings.MasterVolume = v);
            musicSlider.onValueChanged.AddListener(v => AppSettings.MusicVolume = v);
            sfxSlider.onValueChanged.AddListener(v => AppSettings.SfxVolume = v);
            fastAnimationsToggle.Changed += v => AppSettings.FastAnimations = v;
            venueSelector.Changed += index =>
            {
                if (m_Venues != null && index >= 0 && index < m_Venues.Count) VenueChosen?.Invoke(m_Venues[index].id);
            };
            Loc.Changed += OnLanguageChanged;
            root.SetActive(false);
        }

        void OnDestroy() => Loc.Changed -= OnLanguageChanged;

        void OnLanguageChanged()
        {
            if (IsOpen) Fill();
        }

        public void Open(string venueChoice, bool multiplayer)
        {
            m_VenueChoice = venueChoice;
            m_Multiplayer = multiplayer;
            Fill();

            masterSlider.SetValueWithoutNotify(AppSettings.MasterVolume);
            musicSlider.SetValueWithoutNotify(AppSettings.MusicVolume);
            sfxSlider.SetValueWithoutNotify(AppSettings.SfxVolume);
            fastAnimationsToggle.Value = AppSettings.FastAnimations;

            root.SetActive(true);
            root.transform.SetAsLastSibling();
            if (display != null) display.Refresh();
            UITween.PopIn(this, root.transform, 0.2f, 0f, 0.9f);
            OpenChanged?.Invoke(true);
        }

        void Fill()
        {
            m_Venues = GameDatabase.Instance.VenueChoices();
            var labels = new List<string>();
            int index = 0;
            for (int i = 0; i < m_Venues.Count; i++)
            {
                labels.Add(m_Venues[i].label);
                if (m_Venues[i].id == m_VenueChoice) index = i;
            }
            venueSelector.SetTitle(Loc.Get("pause.setup"));
            venueSelector.SetOptions(labels, index);

            rulesText.text = RulesText.Quick;
            titleLabel.text = m_Multiplayer ? Loc.Get("pause.title_mp") + $"  <size=60%><color=#B9AED6>{Loc.Get("pause.keeps_running")}</color></size>" : Loc.Get("pause.title");
            quitLabel.text = Loc.Get(m_Multiplayer ? "pause.leave" : "pause.save_quit");
        }

        public void Close()
        {
            if (!root.activeSelf) return;
            root.SetActive(false);
            AppSettings.Save();
            OpenChanged?.Invoke(false);
        }
    }
}
