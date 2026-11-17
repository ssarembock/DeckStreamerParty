using System.Collections.Generic;
using System.Text;
using DeckParty.Core;
using DeckParty.Networking;
using DeckParty.Platforms;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>Pre-game lobby: players and their streamers, ready states, host match settings (incl. turn timer), personal stream setup and chat.</summary>
    public sealed class LobbyScreen : UIScreen
    {
        [SerializeField] internal TMP_Text titleLabel;
        [SerializeField] internal TMP_Text codeLabel;
        [SerializeField] internal TMP_Text codeHintLabel;
        [SerializeField] internal Button copyCodeButton;
        [SerializeField] internal RectTransform playerList;
        [SerializeField] internal PlayerRowView playerRowPrefab;
        [SerializeField] internal TMP_Text playerCountLabel;
        [SerializeField] internal OptionSelector scenarioSelector;
        [SerializeField] internal OptionSelector modeSelector;
        [SerializeField] internal OptionSelector nightsSelector;
        [SerializeField] internal OptionSelector venueSelector;
        [SerializeField] internal OptionSelector timerSelector;
        [Tooltip("The shop timer (Take Turns and Free For All), separate from the stream timer above.")]
        [SerializeField] internal OptionSelector shopTimerSelector;
        [SerializeField] internal TMP_InputField customTimerInput;
        [SerializeField] internal Button streamerButton;
        [SerializeField] internal TMP_Text streamerLabel;
        [SerializeField] internal TMP_Text settingsSummary;
        [SerializeField] internal Button readyButton;
        [SerializeField] internal TMP_Text readyLabel;
        [SerializeField] internal Button startButton;
        [SerializeField] internal Button leaveButton;
        [SerializeField] internal TMP_Text statusLabel;
        [SerializeField] internal ChatView chat;

        [Header("Teams (2v2)")]
        [Tooltip("2v2 only: move to the other team (two a side). Optional in scenes built before 2026-11-16.")]
        [SerializeField] internal Button switchTeamButton;

        [Header("Online platform (Steam)")]
        [Tooltip("Opens Invite Friends (in place of Copy while the lobby is a Steam lobby).")]
        [SerializeField] internal Button inviteButton;
        [SerializeField] internal FriendsInviteView friendsPanel;

        static readonly int[] s_NightOptions = { 10, 15, 20, 25, 30 };

        MainMenuController m_Menu;
        DeckPartyNetSession m_Session;
        readonly List<PlayerRowView> m_Rows = new List<PlayerRowView>();
        /// <summary>Each row's player's platform id (clicking a row opens their Steam profile).</summary>
        readonly List<ulong> m_RowPlatformIds = new List<ulong>();
        IOnlinePlatform m_AvatarPlatform;
        bool m_Applying;

        public void Init(MainMenuController menu) => m_Menu = menu;

        void Awake()
        {
            copyCodeButton.onClick.AddListener(CopyCode);
            if (inviteButton != null) inviteButton.onClick.AddListener(() => friendsPanel?.Open());
            readyButton.onClick.AddListener(ToggleReady);
            startButton.onClick.AddListener(StartMatch);
            leaveButton.onClick.AddListener(Leave);
            scenarioSelector.Changed += _ => PushSettings();
            modeSelector.Changed += _ => PushSettings();
            nightsSelector.Changed += _ => PushSettings();
            // Stream setups are no longer picked (2026-11-17): scenes built before still have the selector, hidden.
            if (venueSelector != null) venueSelector.gameObject.SetActive(false);
            if (timerSelector != null) timerSelector.Changed += _ => PushSettings();
            if (shopTimerSelector != null) shopTimerSelector.Changed += _ => PushSettings();
            if (customTimerInput != null) customTimerInput.onEndEdit.AddListener(_ => PushSettings());
            if (streamerButton != null) streamerButton.onClick.AddListener(() => m_Menu.ShowStreamerSelect(StreamerSelectMode.Lobby));
            if (switchTeamButton != null) switchTeamButton.onClick.AddListener(() => m_Session?.SwitchTeam());
            Loc.Changed += () =>
            {
                if (IsVisible && m_Session != null) FillSelectors();
            };
        }

        protected override void OnShow()
        {
            m_Session = DeckPartyNetSession.Instance;
            if (m_Session == null)
            {
                m_Menu.ShowMultiplayerConnect();
                return;
            }

            m_Session.PlayersChanged += Refresh;
            m_Session.SettingsChanged += Refresh;
            m_Session.PhaseChanged += OnPhaseChanged;
            DeckPartyNetSession.Despawned += OnSessionDespawned;
            chat.Bind(m_Session);
            // Text chat is off: the box shows lobby news only (players talk with emotes in game).
            chat.SetTextInput(FxLibrary.Current.textChat);
            m_AvatarPlatform = OnlinePlatforms.Current;
            if (m_AvatarPlatform != null) m_AvatarPlatform.AvatarLoaded += OnAvatarLoaded;
            // A host's new lobby starts in the section picked on the multiplayer mode screen (Competitive or Co-Op).
            if (m_Session.IsServer && OnlineCategory.Of(m_Session.Settings.Mode) != OnlineCategory.Current)
            {
                var settings = m_Session.Settings;
                settings.Mode = OnlineCategory.DefaultMode(OnlineCategory.Current);
                m_Session.ServerSetSettings(settings);
            }
            FillSelectors();
        }

        /// <summary>The lobby's section (from its mode): Competitive or Co-Op. The mode selector offers only its modes.</summary>
        int Category => m_Session != null ? OnlineCategory.Of(m_Session.Settings.Mode) : OnlineCategory.Current;

        int m_FilledCategory = -1;

        void OnAvatarLoaded(ulong userId) => Refresh();

        /// <summary>Esc: closes Invite Friends. Returns true when it did.</summary>
        public bool CloseOverlay()
        {
            if (friendsPanel == null || !friendsPanel.IsOpen) return false;
            friendsPanel.Close();
            return true;
        }

        void FillSelectors()
        {
            var scenarioNames = new List<string>();
            foreach (var s in ScenarioCatalog.All) scenarioNames.Add(ContentText.ScenarioName(s.Id));
            scenarioSelector.SetTitle(Terms.CharacterSet);
            scenarioSelector.SetOptions(scenarioNames, 0);

            FillModes();

            var nightNames = new List<string>();
            foreach (var n in s_NightOptions) nightNames.Add(Loc.Get("single.nights", "count", n));
            nightsSelector.SetTitle(Loc.Get("lobby.season_length"));
            nightsSelector.SetOptions(nightNames, 3);

            if (timerSelector != null)
            {
                timerSelector.SetTitle(Loc.Get("lobby.turn_timer"));
                timerSelector.SetOptions(TurnTimers.Labels, 0);
            }
            if (shopTimerSelector != null)
            {
                shopTimerSelector.SetTitle(Loc.Get("lobby.shop_timer"));
                shopTimerSelector.SetOptions(ShopTimers.Labels, 0);
            }

            Refresh();
        }

        /// <summary>The mode selector's options: this section's modes only (Take Turns, Free For All, 2v2; or just co-op).</summary>
        void FillModes()
        {
            int category = Category;
            m_FilledCategory = category;
            var labels = new List<string>();
            foreach (var mode in MatchModes.ForCategory(category)) labels.Add(MatchModes.Name(mode));
            modeSelector.SetTitle(Loc.Get("lobby.mode"));
            modeSelector.SetOptions(labels, 0);
        }

        static int IndexInCategory(MatchMode mode, int category)
        {
            var modes = MatchModes.ForCategory(category);
            int index = System.Array.IndexOf(modes, mode);
            return index >= 0 ? index : 0;
        }

        protected override void OnHide()
        {
            if (m_Session != null)
            {
                m_Session.PlayersChanged -= Refresh;
                m_Session.SettingsChanged -= Refresh;
                m_Session.PhaseChanged -= OnPhaseChanged;
            }
            DeckPartyNetSession.Despawned -= OnSessionDespawned;
            chat.Unbind();
            m_Session = null;
            if (m_AvatarPlatform != null) m_AvatarPlatform.AvatarLoaded -= OnAvatarLoaded;
            m_AvatarPlatform = null;
            if (friendsPanel != null) friendsPanel.Close();
        }

        // Starting a season loads the Game scene while the lobby is showing; the session outlives this screen.
        protected override void OnUnloaded() => OnHide();

        void OnPhaseChanged(MatchPhase phase)
        {
            Refresh();
            // The host started: on to the streamer select screen (a team's second player waits here while their teammate picks).
            if (phase == MatchPhase.PickingStreamers && IsVisible) m_Menu.ShowMatchPick();
        }

        void OnSessionDespawned()
        {
            if (IsVisible) m_Menu.ShowMultiplayerConnect();
        }

        void Refresh()
        {
            if (m_Session == null || !m_Session.IsSpawned) return;
            bool isHost = m_Session.IsServer;
            var service = NetworkService.Instance;

            titleLabel.text = Loc.Get(isHost ? "lobby.title_host" : "lobby.title");
            var platform = service.IsPlatformSession ? OnlinePlatforms.Current : null;
            if (platform != null)
            {
                // A Steam lobby: friends come in through invites and the friends list, so show who can join and the Invite button.
                codeLabel.text = MultiplayerScreen.VisibilityLabel(service.PlatformVisibility);
                codeHintLabel.text = Loc.Get("lobby.platform_hint", "platform", platform.DisplayName);
                copyCodeButton.gameObject.SetActive(false);
                if (inviteButton != null)
                    inviteButton.gameObject.SetActive((platform.Features & (PlatformFeatures.Friends | PlatformFeatures.InviteOverlay)) != 0);
            }
            else
            {
                codeLabel.text = string.IsNullOrEmpty(service.JoinCode) ? "-" : service.JoinCode;
                codeHintLabel.text = service.Mode == ConnectionMode.Relay
                    ? Loc.Get(isHost ? "lobby.code_hint_host" : "lobby.code_hint")
                    : Loc.Get(isHost ? "lobby.ip_hint_host" : "lobby.ip_hint");
                copyCodeButton.gameObject.SetActive(true);
                if (inviteButton != null) inviteButton.gameObject.SetActive(false);
            }

            // Players
            var players = m_Session.PlayersSnapshot();
            for (int i = 0; i < players.Count; i++)
            {
                if (i >= m_Rows.Count)
                {
                    int index = i;
                    var row = Instantiate(playerRowPrefab, playerList);
                    m_Rows.Add(row);
                    m_RowPlatformIds.Add(0);
                    row.Clicked += () => OpenProfile(index);
                }
                var p = players[i];
                bool host = m_Session.IsHostPlayer(p.ClientId);
                bool ready = host || p.LobbyReady;
                m_Rows[i].gameObject.SetActive(true);
                string status = host ? Loc.Get("lobby.host") : (p.LobbyReady ? $"<color=#6BE38B>{Loc.Get("players.ready")}</color>" : $"<color=#B9AED6>{Loc.Get("lobby.not_ready")}</color>");
                // 2v2: each player's team, in the team's colour.
                if (m_Session.Settings.Mode == MatchMode.TwoVsTwo) status = TeamTag(p.Team) + "  " + status;
                m_Rows[i].Set(p.DisplayName, status,
                    UIPalette.PlayerColor(p.ColorIndex), ready, host, 0, p.ClientId == m_Session.LocalClientId);
                // Streamers are picked after the lobby (once everyone is ready and the host starts), so none show here.
                m_Rows[i].SetStreamer(StreamerType.None);

                // Steam: the player's avatar, and their profile on click.
                m_RowPlatformIds[i] = platform != null ? p.PlatformUserId : 0;
                m_Rows[i].SetPlatformAvatar(platform != null && p.PlatformUserId != 0 ? platform.GetAvatar(p.PlatformUserId) : null);
                if (platform != null && p.PlatformUserId != 0 && (platform.Features & PlatformFeatures.Profiles) != 0) m_Rows[i].MakeClickable();
            }
            for (int i = players.Count; i < m_Rows.Count; i++) m_Rows[i].gameObject.SetActive(false);
            playerCountLabel.text = Loc.Get("lobby.players", "count", players.Count, "max", NetworkService.MaxPlayers);

            // Settings
            var settings = m_Session.Settings;
            int category = Category;
            m_Applying = true;
            if (category != m_FilledCategory) FillModes();
            scenarioSelector.SetIndex(IndexOfScenario(settings.ScenarioId));
            modeSelector.SetIndex(IndexInCategory(settings.Mode, category));
            nightsSelector.SetIndex(IndexOfNights(settings.Nights));
            bool turns = settings.TimersApply;
            if (timerSelector != null)
            {
                timerSelector.gameObject.SetActive(turns);
                timerSelector.SetIndex(TurnTimers.IndexOf(settings.TurnSeconds));
                timerSelector.Interactable = isHost;
            }
            if (shopTimerSelector != null)
            {
                shopTimerSelector.gameObject.SetActive(turns);
                shopTimerSelector.SetIndex(ShopTimers.IndexOf(settings.ShopSeconds));
                shopTimerSelector.Interactable = isHost;
            }
            if (customTimerInput != null)
            {
                bool custom = turns && timerSelector != null && timerSelector.Index == TurnTimers.CustomIndex;
                customTimerInput.gameObject.SetActive(custom);
                customTimerInput.interactable = isHost;
                if (!customTimerInput.isFocused) customTimerInput.text = settings.TurnSeconds > 0 ? settings.TurnSeconds.ToString() : string.Empty;
            }
            m_Applying = false;
            // More than four players: Free For All is the only mode (the host switches automatically).
            bool coop = category == OnlineCategory.Coop;
            bool freeForAllOnly = !coop && m_Session.ConnectedCount > MatchModes.TwoVsTwoPlayers;
            scenarioSelector.Interactable = isHost;
            // Co-Op Endless has one mode and its own number of streams: neither selector applies.
            modeSelector.gameObject.SetActive(!coop);
            modeSelector.Interactable = isHost && !freeForAllOnly;
            nightsSelector.gameObject.SetActive(!coop);
            nightsSelector.Interactable = isHost;
            if (switchTeamButton != null) switchTeamButton.gameObject.SetActive(settings.Mode == MatchMode.TwoVsTwo);

            // One short line on the mode, plus a warning when the players don't fit it (the selectors say the rest).
            var summary = new StringBuilder(ModeSummary(settings.Mode));
            if (freeForAllOnly) summary.Append("\n<color=#FF9A3D>").Append(Loc.Get("lobby.ffa_only", "count", MatchModes.TwoVsTwoPlayers)).Append("</color>");
            else if (MatchModes.RequiredPlayers(settings.Mode) > 0 && m_Session.ConnectedCount != MatchModes.RequiredPlayers(settings.Mode))
                summary.Append("\n<color=#FF9A3D>").Append(Loc.Get("lobby.mode_needs", "count", MatchModes.RequiredPlayers(settings.Mode))).Append("</color>");
            settingsSummary.text = summary.ToString();

            // Buttons
            m_Session.TryGetLocalPlayer(out var local);
            if (streamerLabel != null)
            {
                var mine = local.Streamer == StreamerCatalog.RandomPick ? StreamerType.None : (StreamerType)local.Streamer;
                streamerLabel.text = Loc.Get("lobby.change_streamer", "streamer", StreamerVisuals.NameTag(mine));
            }
            // The streamer is picked after the lobby now (scenes built before 2026-11-16 still have this button).
            if (streamerButton != null) streamerButton.gameObject.SetActive(false);
            readyButton.gameObject.SetActive(!isHost);
            readyLabel.text = Loc.Get(local.LobbyReady ? "lobby.unready" : "lobby.ready");
            startButton.gameObject.SetActive(isHost);
            bool canStart = m_Session.ServerCanStart(out var reason);
            startButton.interactable = isHost && canStart;
            if (m_Session.PickingStreamers)
            {
                // A team's second player waits here while their teammate picks the team's streamer.
                statusLabel.text = m_Session.PicksStreamer(m_Session.LocalClientId)
                    ? Loc.Get("lobby.picking", "count", m_Session.PlayersStillPicking)
                    : Loc.Get("lobby.teammate_picking", "count", m_Session.PlayersStillPicking);
                startButton.interactable = false;
                readyButton.interactable = false;
                return;
            }
            readyButton.interactable = true;
            if (isHost) statusLabel.text = canStart ? Loc.Get(players.Count == 1 ? "lobby.solo" : "lobby.all_ready") : Loc.Error(reason);
            else statusLabel.text = Loc.Get(local.LobbyReady ? "lobby.waiting_host" : "lobby.press_ready");
        }

        /// <summary>The mode in one short line ("Take turns streaming. First to 4 Trophies in one stream wins.").</summary>
        static string ModeSummary(MatchMode mode)
        {
            switch (mode)
            {
                case MatchMode.Turns: return Loc.Get("lobby.short_turns", "count", GameRules.DiamondsToWin);
                case MatchMode.TwoVsTwo: return Loc.Get("lobby.short_two_vs_two", "count", GameRules.DiamondsToWin, "risk", GameRules.TeamRiskLimit);
                case MatchMode.CoopEndless: return Loc.Get("lobby.short_coop", "count", GameRules.EndlessStartingStreams, "risk", GameRules.TeamRiskLimit);
                default: return Loc.Get("lobby.short_sync", "count", GameRules.DiamondsToWin);
            }
        }

        /// <summary>2v2: "Team A" / "Team B" in the team's colour.</summary>
        static string TeamTag(int team) => team == 0
            ? $"<color=#3DDBD9>{Loc.Get("team.a")}</color>"
            : $"<color=#FF9A3D>{Loc.Get("team.b")}</color>";

        static int IndexOfScenario(int id)
        {
            for (int i = 0; i < ScenarioCatalog.All.Count; i++)
                if (ScenarioCatalog.All[i].Id == id) return i;
            return 0;
        }

        static int IndexOfNights(int nights)
        {
            for (int i = 0; i < s_NightOptions.Length; i++)
                if (s_NightOptions[i] == nights) return i;
            return 3;
        }

        void PushSettings()
        {
            if (m_Applying || m_Session == null || !m_Session.IsServer) return;
            m_Session.ServerSetSettings(new MatchSettings
            {
                ScenarioId = ScenarioCatalog.All[Mathf.Clamp(scenarioSelector.Index, 0, ScenarioCatalog.All.Count - 1)].Id,
                Mode = ModeAt(modeSelector.Index),
                Nights = (byte)s_NightOptions[Mathf.Clamp(nightsSelector.Index, 0, s_NightOptions.Length - 1)],
                TurnSeconds = (ushort)SelectedTimer(),
                ShopSeconds = (ushort)(shopTimerSelector != null ? ShopTimers.Presets[Mathf.Clamp(shopTimerSelector.Index, 0, ShopTimers.Presets.Length - 1)] : 0),
            });
        }

        int SelectedTimer()
        {
            if (timerSelector == null) return 0;
            int index = timerSelector.Index;
            if (index < TurnTimers.CustomIndex) return TurnTimers.Presets[Mathf.Clamp(index, 0, TurnTimers.Presets.Length - 1)];
            int seconds = customTimerInput != null && int.TryParse(customTimerInput.text, out var parsed) ? parsed : AppSettings.LastTurnTimer;
            if (seconds <= 0) seconds = 90;
            seconds = Mathf.Clamp(seconds, TurnTimers.MinCustom, TurnTimers.MaxCustom);
            AppSettings.LastTurnTimer = seconds;
            return seconds;
        }

        /// <summary>The mode at a selector index within this lobby's section.</summary>
        MatchMode ModeAt(int index)
        {
            var modes = MatchModes.ForCategory(Category);
            return modes[Mathf.Clamp(index, 0, modes.Length - 1)];
        }

        void ToggleReady()
        {
            if (m_Session == null) return;
            m_Session.TryGetLocalPlayer(out var local);
            m_Session.SetLobbyReady(!local.LobbyReady);
        }

        void StartMatch()
        {
            if (m_Session == null || !m_Session.IsServer) return;
            if (!m_Session.ServerCanStart(out var reason))
            {
                ToastView.Show(Loc.Error(reason), ToastKind.Warning);
                return;
            }
            // Everyone picks their streamer now, last thing before the match (the match starts when all have locked in).
            m_Session.ServerBeginStreamerPick();
        }

        void OpenProfile(int rowIndex)
        {
            var platform = OnlinePlatforms.Current;
            if (platform == null || rowIndex < 0 || rowIndex >= m_RowPlatformIds.Count) return;
            ulong id = m_RowPlatformIds[rowIndex];
            if (id != 0) platform.OpenProfile(id);
        }

        void CopyCode()
        {
            var code = NetworkService.Instance.JoinCode;
            if (string.IsNullOrEmpty(code)) return;
            GUIUtility.systemCopyBuffer = code;
            ToastView.Show(Loc.Get("lobby.copied", "code", code), ToastKind.Success);
        }

        void Leave()
        {
            bool isHost = m_Session != null && m_Session.IsServer;
            ModalDialog.Show(Loc.Get(isHost ? "lobby.close_title" : "lobby.leave_title"),
                Loc.Get(isHost ? "lobby.close_body" : "lobby.leave_body"),
                () =>
                {
                    NetworkService.Instance.Leave();
                    m_Menu.ShowMultiplayerConnect();
                }, Loc.Get(isHost ? "over.close_lobby" : "over.leave"), Loc.Get("lobby.stay"));
        }
    }
}
