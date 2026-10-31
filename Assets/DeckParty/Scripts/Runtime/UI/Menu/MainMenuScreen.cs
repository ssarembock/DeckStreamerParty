using DeckParty.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    public sealed class MainMenuScreen : UIScreen
    {
        [SerializeField] internal TMP_Text titleLabel;
        [Tooltip("Shown instead of the title text when the UI Skin has a Logo sprite.")]
        [SerializeField] internal Image logoImage;
        [SerializeField] internal GameObject titleGroup;
        [SerializeField] internal TMP_Text taglineLabel;
        [SerializeField] internal Button continueButton;
        [SerializeField] internal TMP_Text continueLabel;
        [SerializeField] internal Button singlePlayerButton;
        [SerializeField] internal Button multiplayerButton;
        [SerializeField] internal Button howToPlayButton;
        [SerializeField] internal Button settingsButton;
        [SerializeField] internal Button quitButton;
        [SerializeField] internal TMP_Text progressLabel;
        [SerializeField] internal TMP_Text versionLabel;

        MainMenuController m_Menu;

        public void Init(MainMenuController menu) => m_Menu = menu;

        void Awake()
        {
            continueButton.onClick.AddListener(() => SceneFlow.StartSinglePlayer(new SinglePlayerLaunch { ContinueSave = true }));
            singlePlayerButton.onClick.AddListener(() => m_Menu.ShowSinglePlayer());
            multiplayerButton.onClick.AddListener(() => m_Menu.ShowMultiplayer());
            howToPlayButton.onClick.AddListener(() => m_Menu.ShowHowToPlay());
            settingsButton.onClick.AddListener(() => m_Menu.ShowSettings());
            quitButton.onClick.AddListener(Quit);
        }

        protected override void OnShow()
        {
            var skin = GameDatabase.Instance.skin;
            var logo = skin != null ? skin.GetSprite(SkinSlot.Logo) : null;
            if (logoImage != null)
            {
                logoImage.gameObject.SetActive(logo != null);
                logoImage.sprite = logo;
            }
            if (titleGroup != null) titleGroup.SetActive(logo == null);
            if (taglineLabel != null) taglineLabel.text = $"Go live, build your {Terms.Squad} and get {GameRules.DiamondsToWin} {Terms.Diamonds} to one party.";

            var save = SaveSystem.Describe();
            continueButton.gameObject.SetActive(save != null);
            if (save != null) continueLabel.text = $"Continue  <size=65%><color=#B9AED6>{save}</color></size>";

            var progress = $"Character sets cleared: <color=#FFC94A>{ProgressStore.PresetsCleared}/{ScenarioCatalog.PresetCount}</color>" +
                           $"     Random streak: <color=#3DDBD9>{ProgressStore.RandomStreak}</color> (best {ProgressStore.BestRandomStreak})";
            if (ProgressStore.HasCherry) progress += "     <color=#FF4F8B>Cherry earned!</color>";
            progressLabel.text = progress;
            versionLabel.text = "v" + Application.version;
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
