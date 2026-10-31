using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    public sealed class SettingsScreen : UIScreen
    {
        [SerializeField] internal TMP_InputField nameInput;
        [SerializeField] internal Slider masterSlider;
        [SerializeField] internal Slider musicSlider;
        [SerializeField] internal Slider sfxSlider;
        [SerializeField] internal ToggleSwitch fullscreenToggle;
        [SerializeField] internal ToggleSwitch fastAnimationsToggle;
        [SerializeField] internal Button resetProgressButton;
        [SerializeField] internal Button backButton;

        MainMenuController m_Menu;

        public void Init(MainMenuController menu) => m_Menu = menu;

        void Awake()
        {
            nameInput.onEndEdit.AddListener(value => AppSettings.PlayerName = value);
            masterSlider.onValueChanged.AddListener(v => AppSettings.MasterVolume = v);
            musicSlider.onValueChanged.AddListener(v => AppSettings.MusicVolume = v);
            sfxSlider.onValueChanged.AddListener(v =>
            {
                AppSettings.SfxVolume = v;
                AudioService.Play(Sfx.Click);
            });
            fullscreenToggle.Changed += v => AppSettings.Fullscreen = v;
            fastAnimationsToggle.Changed += v => AppSettings.FastAnimations = v;
            resetProgressButton.onClick.AddListener(ResetProgress);
            backButton.onClick.AddListener(() =>
            {
                AppSettings.Save();
                m_Menu.ShowMain();
            });
        }

        protected override void OnShow()
        {
            nameInput.text = AppSettings.PlayerName;
            masterSlider.SetValueWithoutNotify(AppSettings.MasterVolume);
            musicSlider.SetValueWithoutNotify(AppSettings.MusicVolume);
            sfxSlider.SetValueWithoutNotify(AppSettings.SfxVolume);
            fullscreenToggle.Value = AppSettings.Fullscreen;
            fastAnimationsToggle.Value = AppSettings.FastAnimations;
        }

        static void ResetProgress()
        {
            ModalDialog.Show("Reset progress?", "This clears cleared scenarios, your Random streak and your saved season.", () =>
            {
                ProgressStore.ResetAll();
                SaveSystem.Delete();
                ToastView.Show("Progress reset.", ToastKind.Success);
            }, "Reset", "Cancel");
        }
    }
}
