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
        [SerializeField] internal StreamerSelectScreen streamerSelectScreen;
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
            if (streamerSelectScreen != null) streamerSelectScreen.Init(this);

            foreach (var screen in new UIScreen[] { mainScreen, singlePlayerScreen, multiplayerScreen, lobbyScreen, settingsScreen, howToPlayScreen, streamerSelectScreen })
                if (screen != null) screen.gameObject.SetActive(false);

            var venues = GameDatabase.Instance.Venues;
            if (venues.Count > 0) background.Apply(venues[0]);
            m_NextVenueAt = Time.unscaledTime + venueCycleSeconds;
            AudioService.PlayMusic(AudioService.MenuMusic());

            NetworkService.Instance.SessionEnded += OnSessionEnded;

            if (NetworkService.Instance.IsActive && DeckPartyNetSession.Instance != null) ShowLobby();
            else if (SceneFlow.OpenMultiplayerOnMenu) ShowMultiplayerConnect();
            else ShowMain();
            SceneFlow.OpenMultiplayerOnMenu = false;

            var message = SceneFlow.TakePendingMessage();
            if (!string.IsNullOrEmpty(message)) ModalDialog.Show(Loc.Get("menu.heads_up"), Loc.Error(message), null, Loc.Get("common.ok"), null);
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
                if (m_Current == settingsScreen || m_Current == howToPlayScreen) ShowMain();
                else if (m_Current == singlePlayerScreen) ShowStreamerSelect(StreamerSelectMode.SinglePlayer);
                else if (m_Current == multiplayerScreen) ShowStreamerSelect(StreamerSelectMode.Multiplayer);
                // The select screen handles its own Esc (back).
            }
        }

        void OnSessionEnded(string reason)
        {
            if (m_Current == lobbyScreen || m_Current == multiplayerScreen)
            {
                ShowMultiplayerConnect();
                if (!string.IsNullOrEmpty(reason)) multiplayerScreen.SetStatus(Loc.Error(reason), true);
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

        /// <summary>Single Player starts with Select Your Streamer, then the character set screen.</summary>
        public void ShowSinglePlayer() => ShowStreamerSelect(StreamerSelectMode.SinglePlayer);

        /// <summary>Multiplayer starts with Select Your Streamer, then host / join.</summary>
        public void ShowMultiplayer() => ShowStreamerSelect(StreamerSelectMode.Multiplayer);

        /// <summary>The host / join screen itself (after a streamer is picked, or when a connection drops).</summary>
        public void ShowMultiplayerConnect() => Switch(multiplayerScreen);

        public void ShowStreamerSelect(StreamerSelectMode mode)
        {
            if (streamerSelectScreen == null)
            {
                // Scenes built before the select screen existed: skip straight on.
                if (mode == StreamerSelectMode.SinglePlayer) Switch(singlePlayerScreen);
                else if (mode == StreamerSelectMode.Multiplayer) Switch(multiplayerScreen);
                else Switch(lobbyScreen);
                return;
            }

            if (m_Current != null && m_Current != streamerSelectScreen) m_Current.Hide();
            m_Current = streamerSelectScreen;
            m_BackgroundLocked = false;
            streamerSelectScreen.Open(mode, pick => OnStreamerPicked(mode, pick), () =>
            {
                if (mode == StreamerSelectMode.Lobby) Switch(lobbyScreen);
                else Switch(mainScreen);
            });
        }

        void OnStreamerPicked(StreamerSelectMode mode, byte pick)
        {
            NetworkService.LocalStreamer = pick;
            switch (mode)
            {
                case StreamerSelectMode.SinglePlayer:
                    singlePlayerScreen.Streamer = pick;
                    Switch(singlePlayerScreen);
                    break;
                case StreamerSelectMode.Multiplayer:
                    Switch(multiplayerScreen);
                    break;
                default:
                    if (DeckPartyNetSession.Instance != null) DeckPartyNetSession.Instance.SetStreamer(pick);
                    Switch(lobbyScreen);
                    break;
            }
            // A streamer brings their own stream setup: preview it behind the menu.
            var venue = StreamerVisuals.Venue((Core.StreamerType)pick);
            if (venue != null) PreviewVenue(venue);
        }
        public void ShowLobby() => Switch(lobbyScreen);
        public void ShowSettings() => Switch(settingsScreen);
        public void ShowHowToPlay() => Switch(howToPlayScreen);
    }
}
