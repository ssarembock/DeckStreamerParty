using DeckParty.Networking;
using DeckParty.Platforms;
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

        UIScreen m_Current;

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

            AudioService.PlayMusic(AudioService.MenuMusic());

            NetworkService.Instance.SessionEnded += OnSessionEnded;

            if (NetworkService.Instance.IsActive && DeckPartyNetSession.Instance != null) ShowLobby();
            else if (SceneFlow.OpenMultiplayerOnMenu) ShowMultiplayerConnect();
            else ShowMain();
            SceneFlow.OpenMultiplayerOnMenu = false;

            var message = SceneFlow.TakePendingMessage();
            if (!string.IsNullOrEmpty(message)) ModalDialog.Show(Loc.Get("menu.heads_up"), Loc.Error(message), null, Loc.Get("common.ok"), null);

            // A Steam invite accepted before this menu opened (or the game was started from one).
            OnlinePlatforms.PendingJoinChanged += TryJoinPendingInvite;
            TryJoinPendingInvite();
        }

        void OnDestroy()
        {
            if (NetworkService.HasInstance) NetworkService.Instance.SessionEnded -= OnSessionEnded;
            OnlinePlatforms.PendingJoinChanged -= TryJoinPendingInvite;
        }

        void Update()
        {
            if (InputHelper.Pressed(HotKey.Escape) && !InputHelper.IsTyping && !ModalDialog.IsOpen)
            {
                if (m_Current == settingsScreen || m_Current == howToPlayScreen) ShowMain();
                else if (m_Current == singlePlayerScreen) ShowStreamerSelect(StreamerSelectMode.SinglePlayer);
                else if (m_Current == multiplayerScreen)
                {
                    // Find Games closes first.
                    if (!multiplayerScreen.CloseOverlay()) ShowStreamerSelect(StreamerSelectMode.Multiplayer);
                }
                else if (m_Current == lobbyScreen) lobbyScreen.CloseOverlay();
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
            // Left a game to accept an invite: join the friend's lobby once Netcode has finished closing the old one.
            if (!string.IsNullOrEmpty(OnlinePlatforms.PendingJoin)) StartCoroutine(JoinPendingInviteNextFrame());
        }

        System.Collections.IEnumerator JoinPendingInviteNextFrame()
        {
            yield return null;
            TryJoinPendingInvite();
        }

        /// <summary>
        /// Joins the lobby of an accepted Steam invite (or Join Game) when nothing else is running. While a session runs,
        /// <see cref="PlatformJoinRouter"/> asks first and leaves; this then runs again from <see cref="OnSessionEnded"/>.
        /// </summary>
        void TryJoinPendingInvite()
        {
            if (string.IsNullOrEmpty(OnlinePlatforms.PendingJoin) || !OnlinePlatforms.Active) return;
            if (NetworkService.Instance.IsActive || NetworkService.Instance.IsBusy) return;

            string lobby = OnlinePlatforms.TakePendingJoin();
            ShowMultiplayerConnect();
            multiplayerScreen.JoinPlatformLobby(lobby);
        }

        void Switch(UIScreen next)
        {
            if (m_Current == next) return;
            if (m_Current != null) m_Current.Hide();
            m_Current = next;
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
        }
        public void ShowLobby() => Switch(lobbyScreen);
        public void ShowSettings() => Switch(settingsScreen);
        public void ShowHowToPlay() => Switch(howToPlayScreen);
    }
}
