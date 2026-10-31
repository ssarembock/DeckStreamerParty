using DeckParty.Networking;
using UnityEngine;

namespace DeckParty.UI
{
    /// <summary>Owns the menu screens and switches between them. Lives in the MainMenu scene.</summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] internal MainMenuScreen mainScreen;
        [SerializeField] internal SinglePlayerScreen singlePlayerScreen;
        [SerializeField] internal MultiplayerScreen multiplayerScreen;
        [SerializeField] internal LobbyScreen lobbyScreen;
        [SerializeField] internal SettingsScreen settingsScreen;
        [SerializeField] internal HowToPlayScreen howToPlayScreen;
        [SerializeField] internal VenueBackgroundView background;
        [SerializeField] internal float venueCycleSeconds = 9f;

        UIScreen m_Current;
        float m_NextVenueAt;
        int m_VenueIndex;
        bool m_BackgroundLocked;

        public VenueBackgroundView Background => background;

        void Start()
        {
            mainScreen.Init(this);
            singlePlayerScreen.Init(this);
            multiplayerScreen.Init(this);
            lobbyScreen.Init(this);
            settingsScreen.Init(this);
            howToPlayScreen.Init(this);

            foreach (var screen in new UIScreen[] { mainScreen, singlePlayerScreen, multiplayerScreen, lobbyScreen, settingsScreen, howToPlayScreen })
                screen.gameObject.SetActive(false);

            var venues = GameDatabase.Instance.Venues;
            if (venues.Count > 0) background.Apply(venues[0]);
            m_NextVenueAt = Time.unscaledTime + venueCycleSeconds;
            AudioService.PlayMusic(AudioService.MenuMusic());

            NetworkService.Instance.SessionEnded += OnSessionEnded;

            if (NetworkService.Instance.IsActive && DeckPartyNetSession.Instance != null) ShowLobby();
            else if (SceneFlow.OpenMultiplayerOnMenu) ShowMultiplayer();
            else ShowMain();
            SceneFlow.OpenMultiplayerOnMenu = false;

            var message = SceneFlow.TakePendingMessage();
            if (!string.IsNullOrEmpty(message)) ModalDialog.Show("Heads up", message, null, "OK", null);
        }

        void OnDestroy()
        {
            if (NetworkService.HasInstance) NetworkService.Instance.SessionEnded -= OnSessionEnded;
        }

        void Update()
        {
            if (!m_BackgroundLocked && m_Current == mainScreen && Time.unscaledTime >= m_NextVenueAt)
            {
                var venues = GameDatabase.Instance.Venues;
                if (venues.Count > 1)
                {
                    m_VenueIndex = (m_VenueIndex + 1) % venues.Count;
                    background.Apply(venues[m_VenueIndex]);
                }
                m_NextVenueAt = Time.unscaledTime + venueCycleSeconds;
            }

            if (InputHelper.Pressed(HotKey.Escape) && !InputHelper.IsTyping && !ModalDialog.IsOpen)
            {
                if (m_Current == singlePlayerScreen || m_Current == settingsScreen || m_Current == howToPlayScreen || m_Current == multiplayerScreen)
                    ShowMain();
            }
        }

        void OnSessionEnded(string reason)
        {
            if (m_Current == lobbyScreen || m_Current == multiplayerScreen)
            {
                ShowMultiplayer();
                if (!string.IsNullOrEmpty(reason)) multiplayerScreen.SetStatus(reason, true);
            }
        }

        /// <summary>Lets a screen preview a venue on the menu background (null releases it back to the slideshow).</summary>
        public void PreviewVenue(VenueDefinition venue)
        {
            m_BackgroundLocked = venue != null;
            if (venue != null) background.Apply(venue);
            m_NextVenueAt = Time.unscaledTime + venueCycleSeconds;
        }

        void Switch(UIScreen next)
        {
            if (m_Current == next) return;
            if (m_Current != null) m_Current.Hide();
            m_Current = next;
            if (next != singlePlayerScreen && next != lobbyScreen) m_BackgroundLocked = false;
            next.Show();
        }

        public void ShowMain() => Switch(mainScreen);
        public void ShowSinglePlayer() => Switch(singlePlayerScreen);
        public void ShowMultiplayer() => Switch(multiplayerScreen);
        public void ShowLobby() => Switch(lobbyScreen);
        public void ShowSettings() => Switch(settingsScreen);
        public void ShowHowToPlay() => Switch(howToPlayScreen);
    }
}
