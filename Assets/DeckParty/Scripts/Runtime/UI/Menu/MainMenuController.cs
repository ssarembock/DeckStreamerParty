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

            foreach (var screen in new UIScreen[] { mainScreen, singlePlayerScreen, multiplayerScreen, lobbyScreen, settingsScreen, howToPlayScreen, streamerSelectScreen, singlePlayerModeScreen })
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
                else if (m_Current == singlePlayerScreen) BackFromCharacterSets();
                // The mode screen handles its own Esc (back to the main menu).
                else if (m_Current == multiplayerScreen)
                {
                    // Find Games closes first.
                    if (!multiplayerScreen.CloseOverlay()) ShowMain();
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

        /// <summary>A mode was picked on the single-player mode screen: on to the character sets.</summary>
        public void StartSinglePlayerMode(SinglePlayerMode mode)
        {
            singlePlayerScreen.Mode = mode;
            Switch(singlePlayerScreen);
        }

        /// <summary>Back from the character sets: to the mode screen.</summary>
        void BackFromCharacterSets()
        {
            if (singlePlayerModeScreen != null) Switch(singlePlayerModeScreen);
            else ShowMain();
        }

        /// <summary>Called by the character set screen's Back button.</summary>
        public void ShowCharacterSetsBack() => BackFromCharacterSets();

        /// <summary>The season waiting for its streamer pick (single player): set by the character set screen's Start.</summary>
        SinglePlayerLaunch m_PendingLaunch;

        /// <summary>
        /// Start on the character set screen: everything else is chosen, so now the streamer (and, versus the AI, the AI's),
        /// then the season starts.
        /// </summary>
        public void PickStreamersAndStart(SinglePlayerLaunch launch)
        {
            m_PendingLaunch = launch;
            if (streamerSelectScreen == null)
            {
                // Scenes built before the select screen existed: the last streamer played.
                launch.Streamer = AppSettings.LastStreamer;
                SceneFlow.StartSinglePlayer(launch);
                return;
            }
            ShowStreamerSelect(launch.VersusAi ? StreamerSelectMode.VersusAi : StreamerSelectMode.SinglePlayer);
        }

        // ================================================================== multiplayer

        /// <summary>Multiplayer: host / join, then the lobby. Streamers are picked once everyone is ready and the host starts.</summary>
        public void ShowMultiplayer() => Switch(multiplayerScreen);

        /// <summary>The host / join screen itself (also when a connection drops).</summary>
        public void ShowMultiplayerConnect() => Switch(multiplayerScreen);

        /// <summary>
        /// The host started the match: everyone picks their streamer (with a countdown). A team's second player waits in the
        /// lobby while their teammate picks for the team.
        /// </summary>
        public void ShowMatchPick()
        {
            var net = DeckPartyNetSession.Instance;
            if (net == null || !net.PickingStreamers || streamerSelectScreen == null || !net.PicksStreamer(net.LocalClientId))
            {
                ShowLobby();
                return;
            }
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
            // The streamer screen is reused for the AI's pick: hide it first so it shows afresh.
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
            // The AI's streamer isn't yours (online it would become your pick).
            if (mode != StreamerSelectMode.Opponent) NetworkService.LocalStreamer = pick;
            switch (mode)
            {
                case StreamerSelectMode.SinglePlayer:
                    if (m_PendingLaunch == null) return;
                    m_PendingLaunch.Streamer = pick;
                    SceneFlow.StartSinglePlayer(m_PendingLaunch);
                    break;
                case StreamerSelectMode.VersusAi:
                    // Your streamer, then the AI's.
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
    }
}
