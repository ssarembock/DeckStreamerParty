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
        [Tooltip("Single Player's first screen: Puzzles, Endless or Versus the AI (scenes from before 2026-11-16 go straight to the character sets).")]
        [SerializeField] internal SinglePlayerModeScreen singlePlayerModeScreen;
        [Tooltip("Multiplayer's first screen: Online Competitive, Online Co-Op Endless or Couch Competitive (scenes from before 2026-11-17 go straight to host / join).")]
        [SerializeField] internal MultiplayerModeScreen multiplayerModeScreen;
        [Tooltip("Settings > Controller: the gamepad mapping (scenes from before 2026-11-17 have none).")]
        [SerializeField] internal ControllerSettingsScreen controllerScreen;

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
            if (singlePlayerModeScreen != null) singlePlayerModeScreen.Init(this);
            if (multiplayerModeScreen != null) multiplayerModeScreen.Init(this);
            if (controllerScreen != null) controllerScreen.Init(this);

            foreach (var screen in new UIScreen[] { mainScreen, singlePlayerScreen, multiplayerScreen, lobbyScreen, settingsScreen, howToPlayScreen, streamerSelectScreen, singlePlayerModeScreen, multiplayerModeScreen, controllerScreen })
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
            // Settings > Controller waiting for a pad button: every pad button is an answer, only Esc on the keyboard cancels.
            if (controllerScreen != null && m_Current == controllerScreen && controllerScreen.Listening)
            {
                if (InputHelper.Pressed(HotKey.Escape)) controllerScreen.CancelListening();
                return;
            }

            if (InputHelper.MenuPressed(HotKey.Escape) && !InputHelper.IsTyping && !ModalDialog.IsOpen)
            {
                if (m_Current == settingsScreen || m_Current == howToPlayScreen) ShowMain();
                else if (m_Current == controllerScreen) ShowSettings();
                else if (m_Current == singlePlayerScreen) BackFromCharacterSets();
                // The mode screens handle their own Esc (back to the main menu).
                else if (m_Current == multiplayerScreen)
                {
                    // Find Games closes first.
                    if (!multiplayerScreen.CloseOverlay()) BackFromConnect();
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

        // ================================================================== single player

        /// <summary>
        /// Single Player: the mode screen (Puzzles, Endless, Versus the AI), then the character set screen, then (last, like a
        /// fighting game) Select Your Streamer and, versus the AI, the AI's streamer. Scenes built before the mode screen skip it.
        /// </summary>
        public void ShowSinglePlayer()
        {
            if (singlePlayerModeScreen != null) Switch(singlePlayerModeScreen);
            else StartSinglePlayerMode(SinglePlayerMode.Puzzles);
        }

        /// <summary>A mode was picked on the single-player mode screen (or Couch on the multiplayer one): on to the character sets.</summary>
        public void StartSinglePlayerMode(SinglePlayerMode mode)
        {
            singlePlayerScreen.Mode = mode;
            Switch(singlePlayerScreen);
        }

        /// <summary>Back from the character sets: to the mode screen it came from (couch play: the multiplayer one).</summary>
        void BackFromCharacterSets()
        {
            if (singlePlayerScreen.Mode == SinglePlayerMode.Couch && multiplayerModeScreen != null) Switch(multiplayerModeScreen);
            else if (singlePlayerModeScreen != null) Switch(singlePlayerModeScreen);
            else ShowMain();
        }

        /// <summary>Called by the character set screen's Back button.</summary>
        public void ShowCharacterSetsBack() => BackFromCharacterSets();

        /// <summary>The season waiting for its streamer pick (single player): set by the character set screen's Start.</summary>
        SinglePlayerLaunch m_PendingLaunch;

        /// <summary>
        /// Start on the character set screen: everything else is chosen, so now the streamer (versus the AI: the AI's too;
        /// couch play: Player 1's, then Player 2's), then the season starts.
        /// </summary>
        public void PickStreamersAndStart(SinglePlayerLaunch launch)
        {
            m_PendingLaunch = launch;
            if (streamerSelectScreen == null)
            {
                // Scenes built before the select screen existed: the last streamer played.
                launch.Streamer = AppSettings.LastStreamer;
                launch.OpponentStreamer = Core.StreamerCatalog.RandomPick;
                SceneFlow.StartSinglePlayer(launch);
                return;
            }
            streamerSelectScreen.Couch = launch.Couch;
            ShowStreamerSelect(launch.VersusAi || launch.Couch ? StreamerSelectMode.VersusAi : StreamerSelectMode.SinglePlayer);
        }

        // ================================================================== multiplayer

        /// <summary>
        /// Multiplayer: the mode screen (Online Competitive, Online Co-Op Endless, Couch Competitive). Online modes go on to
        /// host / join and the lobby (streamers are picked once everyone is ready and the host starts); couch play goes to the
        /// character sets. Scenes built before the mode screen go straight to host / join.
        /// </summary>
        public void ShowMultiplayer()
        {
            if (multiplayerModeScreen != null) Switch(multiplayerModeScreen);
            else StartMultiplayerMode(MultiplayerMode.OnlineCompetitive);
        }

        /// <summary>A mode was picked on the multiplayer mode screen.</summary>
        public void StartMultiplayerMode(MultiplayerMode mode)
        {
            if (mode == MultiplayerMode.Couch)
            {
                StartSinglePlayerMode(SinglePlayerMode.Couch);
                return;
            }
            OnlineCategory.Current = mode == MultiplayerMode.OnlineCoop ? OnlineCategory.Coop : OnlineCategory.Competitive;
            Switch(multiplayerScreen);
        }

        /// <summary>The host / join screen itself (also when a connection drops).</summary>
        public void ShowMultiplayerConnect() => Switch(multiplayerScreen);

        /// <summary>Back (or Esc) on the host / join screen: to the multiplayer mode screen.</summary>
        public void BackFromConnect()
        {
            if (multiplayerModeScreen != null) Switch(multiplayerModeScreen);
            else ShowMain();
        }

        /// <summary>
        /// The host started the match: everyone picks their streamer (with a countdown). In the team modes each player picks
        /// their own too (2026-11-18), and teammates can't pick the same one.
        /// </summary>
        public void ShowMatchPick()
        {
            var net = DeckPartyNetSession.Instance;
            if (net == null || !net.PickingStreamers || streamerSelectScreen == null)
            {
                ShowLobby();
                return;
            }
            streamerSelectScreen.Couch = false;
            ShowStreamerSelect(StreamerSelectMode.MatchPick);
        }

        // ================================================================== the streamer select screen

        public void ShowStreamerSelect(StreamerSelectMode mode)
        {
            if (streamerSelectScreen == null)
            {
                // Scenes built before the select screen existed: skip straight on.
                if (mode == StreamerSelectMode.SinglePlayer || mode == StreamerSelectMode.VersusAi || mode == StreamerSelectMode.Opponent)
                {
                    if (m_PendingLaunch != null) SceneFlow.StartSinglePlayer(m_PendingLaunch);
                    else Switch(singlePlayerScreen);
                }
                else if (mode == StreamerSelectMode.Multiplayer) Switch(multiplayerScreen);
                else Switch(lobbyScreen);
                return;
            }

            if (m_Current != null && m_Current != streamerSelectScreen) m_Current.Hide();
            // The streamer screen is reused for the second pick: hide it first so it shows afresh.
            if (m_Current == streamerSelectScreen) streamerSelectScreen.Hide();
            m_Current = streamerSelectScreen;
            if (mode == StreamerSelectMode.Opponent && m_PendingLaunch != null) streamerSelectScreen.PlayerPick = m_PendingLaunch.Streamer;
            streamerSelectScreen.Open(mode, pick => OnStreamerPicked(mode, pick), () =>
            {
                switch (mode)
                {
                    case StreamerSelectMode.Opponent: ShowStreamerSelect(StreamerSelectMode.VersusAi); break;
                    case StreamerSelectMode.SinglePlayer:
                    case StreamerSelectMode.VersusAi: Switch(singlePlayerScreen); break;
                    // The pick was called off (someone left): back to the lobby.
                    case StreamerSelectMode.MatchPick:
                    case StreamerSelectMode.Lobby: Switch(lobbyScreen); break;
                    default: Switch(mainScreen); break;
                }
            });
        }

        void OnStreamerPicked(StreamerSelectMode mode, byte pick)
        {
            // The AI's (or Player 2's) streamer isn't yours (online it would become your pick).
            if (mode != StreamerSelectMode.Opponent) NetworkService.LocalStreamer = pick;
            switch (mode)
            {
                case StreamerSelectMode.SinglePlayer:
                    if (m_PendingLaunch == null) return;
                    m_PendingLaunch.Streamer = pick;
                    SceneFlow.StartSinglePlayer(m_PendingLaunch);
                    break;
                case StreamerSelectMode.VersusAi:
                    // Your streamer (Player 1's on the couch), then the AI's (Player 2's).
                    if (m_PendingLaunch != null) m_PendingLaunch.Streamer = pick;
                    ShowStreamerSelect(StreamerSelectMode.Opponent);
                    break;
                case StreamerSelectMode.Opponent:
                    if (m_PendingLaunch == null) return;
                    m_PendingLaunch.OpponentStreamer = pick;
                    SceneFlow.StartSinglePlayer(m_PendingLaunch);
                    break;
                case StreamerSelectMode.MatchPick:
                    // Locked in: the screen waits for the others, then the match starts.
                    if (DeckPartyNetSession.Instance != null) DeckPartyNetSession.Instance.LockStreamer(pick);
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

        /// <summary>Settings > Controller (the gamepad mapping).</summary>
        public void ShowControllerSettings()
        {
            if (controllerScreen != null) Switch(controllerScreen);
        }
    }
}
