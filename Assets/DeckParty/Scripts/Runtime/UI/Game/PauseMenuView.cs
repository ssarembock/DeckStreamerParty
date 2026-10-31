using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>In-game menu: change venue, volume, quick rules, quit / leave.</summary>
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
        [SerializeField] internal TMP_Text rulesText;
        [SerializeField] internal Button quitButton;
        [SerializeField] internal TMP_Text quitLabel;

        public event Action QuitClicked;
        public event Action<string> VenueChosen;

        List<(string id, string label)> m_Venues;

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
            root.SetActive(false);
        }

        public void Open(string venueChoice, bool multiplayer)
        {
            m_Venues = GameDatabase.Instance.VenueChoices();
            var labels = new List<string>();
            int index = 0;
            for (int i = 0; i < m_Venues.Count; i++)
            {
                labels.Add(m_Venues[i].label);
                if (m_Venues[i].id == venueChoice) index = i;
            }
            venueSelector.SetTitle("Venue");
            venueSelector.SetOptions(labels, index);

            masterSlider.SetValueWithoutNotify(AppSettings.MasterVolume);
            musicSlider.SetValueWithoutNotify(AppSettings.MusicVolume);
            sfxSlider.SetValueWithoutNotify(AppSettings.SfxVolume);
            fastAnimationsToggle.Value = AppSettings.FastAnimations;
            rulesText.text = RulesText.Quick;
            titleLabel.text = multiplayer ? "Menu  <size=60%><color=#B9AED6>(the game keeps running)</color></size>" : "Paused";
            quitLabel.text = multiplayer ? "Leave Game" : "Save & Quit to Menu";

            root.SetActive(true);
            root.transform.SetAsLastSibling();
            UITween.PopIn(this, root.transform, 0.2f, 0f, 0.9f);
        }

        public void Close()
        {
            root.SetActive(false);
            AppSettings.Save();
        }
    }
}
