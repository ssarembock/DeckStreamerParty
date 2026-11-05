using System.Collections.Generic;
using System.Text;
using DeckParty.Core;
using DeckParty.Networking;
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

        static readonly int[] s_NightOptions = { 10, 15, 20, 25, 30 };

        MainMenuController m_Menu;
        DeckPartyNetSession m_Session;
        readonly List<PlayerRowView> m_Rows = new List<PlayerRowView>();
        List<(string id, string label)> m_VenueChoices;
        bool m_Applying;

        public void Init(MainMenuController menu) => m_Menu = menu;

        void Awake()
        {
            copyCodeButton.onClick.AddListener(CopyCode);
            readyButton.onClick.AddListener(ToggleReady);
            startButton.onClick.AddListener(StartMatch);
            leaveButton.onClick.AddListener(Leave);
            scenarioSelector.Changed += _ => PushSettings();
            modeSelector.Changed += _ => PushSettings();
            nightsSelector.Changed += _ => PushSettings();
            venueSelector.Changed += OnVenueChanged;
            if (timerSelector != null) timerSelector.Changed += _ => PushSettings();
            if (customTimerInput != null) customTimerInput.onEndEdit.AddListener(_ => PushSettings());
            if (streamerButton != null) streamerButton.onClick.AddListener(() => m_Menu.ShowStreamerSelect(StreamerSelectMode.Lobby));
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
            FillSelectors();
        }

        void FillSelectors()
        {
            var scenarioNames = new List<string>();
            foreach (var s in ScenarioCatalog.All) scenarioNames.Add(ContentText.ScenarioName(s.Id));
            scenarioSelector.SetTitle(Terms.CharacterSet);
            scenarioSelector.SetOptions(scenarioNames, 0);

            modeSelector.SetTitle(Loc.Get("lobby.mode"));
            modeSelector.SetOptions(MatchModes.Labels, 0);

            var nightNames = new List<string>();
            foreach (var n in s_NightOptions) nightNames.Add(Loc.Get("single.nights", "count", n));
            nightsSelector.SetTitle(Loc.Get("lobby.season_length"));
            nightsSelector.SetOptions(nightNames, 3);

            if (timerSelector != null)
            {
                timerSelector.SetTitle(Loc.Get("lobby.turn_timer"));
                timerSelector.SetOptions(TurnTimers.Labels, 0);
            }

            m_VenueChoices = GameDatabase.Instance.VenueChoices();
            var venueNames = new List<string>();
            int venueIndex = 0;
            for (int i = 0; i < m_VenueChoices.Count; i++)
            {
                venueNames.Add(m_VenueChoices[i].label);
                if (m_VenueChoices[i].id == AppSettings.VenueId) venueIndex = i;
            }
            venueSelector.SetTitle(Loc.Get("lobby.your_setup"));
            venueSelector.SetOptions(venueNames, venueIndex);

            Refresh();
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
        }

        // Starting a season loads the Game scene while the lobby is showing; the session outlives this screen.
        protected override void OnUnloaded() => OnHide();

        void OnPhaseChanged(MatchPhase phase) => Refresh();

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
            codeLabel.text = string.IsNullOrEmpty(service.JoinCode) ? "-" : service.JoinCode;
            codeHintLabel.text = service.Mode == ConnectionMode.Relay
                ? Loc.Get(isHost ? "lobby.code_hint_host" : "lobby.code_hint")
                : Loc.Get(isHost ? "lobby.ip_hint_host" : "lobby.ip_hint");

            // Players
            var players = m_Session.PlayersSnapshot();
            for (int i = 0; i < players.Count; i++)
            {
                if (i >= m_Rows.Count) m_Rows.Add(Instantiate(playerRowPrefab, playerList));
                var p = players[i];
                bool host = m_Session.IsHostPlayer(p.ClientId);
                bool ready = host || p.LobbyReady;
                m_Rows[i].gameObject.SetActive(true);
                m_Rows[i].Set(p.DisplayName, host ? Loc.Get("lobby.host") : (p.LobbyReady ? $"<color=#6BE38B>{Loc.Get("players.ready")}</color>" : $"<color=#B9AED6>{Loc.Get("lobby.not_ready")}</color>"),
                    UIPalette.PlayerColor(p.ColorIndex), ready, host, 0, p.ClientId == m_Session.LocalClientId);
                m_Rows[i].SetStreamer(p.Streamer == StreamerCatalog.RandomPick ? StreamerType.None : (StreamerType)p.Streamer);
            }
            for (int i = players.Count; i < m_Rows.Count; i++) m_Rows[i].gameObject.SetActive(false);
            playerCountLabel.text = Loc.Get("lobby.players", "count", players.Count, "max", NetworkService.MaxPlayers);

            // Settings
            var settings = m_Session.Settings;
            m_Applying = true;
            scenarioSelector.SetIndex(IndexOfScenario(settings.ScenarioId));
            modeSelector.SetIndex(MatchModes.IndexOf(settings.Mode));
            nightsSelector.SetIndex(IndexOfNights(settings.Nights));
            bool turns = settings.Mode == MatchMode.Turns;
            if (timerSelector != null)
            {
                timerSelector.gameObject.SetActive(turns);
                timerSelector.SetIndex(TurnTimers.IndexOf(settings.TurnSeconds));
                timerSelector.Interactable = isHost;
            }
            if (customTimerInput != null)
            {
                bool custom = turns && timerSelector != null && timerSelector.Index == TurnTimers.CustomIndex;
                customTimerInput.gameObject.SetActive(custom);
                customTimerInput.interactable = isHost;
                if (!customTimerInput.isFocused) customTimerInput.text = settings.TurnSeconds > 0 ? settings.TurnSeconds.ToString() : string.Empty;
            }
            m_Applying = false;
            // More than two players: Free For All is the only mode (the host switches automatically).
            bool freeForAllOnly = m_Session.ConnectedCount > MatchModes.MaxPlayersForAnyMode;
            scenarioSelector.Interactable = isHost;
            modeSelector.Interactable = isHost && !freeForAllOnly;
            nightsSelector.Interactable = isHost;

            var summary = new StringBuilder();
            summary.Append($"<b>{ContentText.ScenarioName(settings.ScenarioId)}</b> - {ContentText.ScenarioDescription(settings.ScenarioId)}\n");
            summary.Append(ModeSummary(settings.Mode));
            if (freeForAllOnly) summary.Append("\n<color=#FF9A3D>").Append(Loc.Get("lobby.ffa_only", "count", MatchModes.MaxPlayersForAnyMode)).Append("</color>");
            if (turns) summary.Append("\n").Append(Loc.Get("lobby.timer_summary", "timer", TurnTimers.Describe(settings.TurnSeconds)));
            settingsSummary.text = summary.ToString();

            // Buttons
            m_Session.TryGetLocalPlayer(out var local);
            if (streamerLabel != null)
            {
                var mine = local.Streamer == StreamerCatalog.RandomPick ? StreamerType.None : (StreamerType)local.Streamer;
                streamerLabel.text = Loc.Get("lobby.change_streamer", "streamer", StreamerVisuals.NameTag(mine));
            }
            if (streamerButton != null) streamerButton.interactable = !local.LobbyReady || isHost;
            readyButton.gameObject.SetActive(!isHost);
            readyLabel.text = Loc.Get(local.LobbyReady ? "lobby.unready" : "lobby.ready");
            startButton.gameObject.SetActive(isHost);
            bool canStart = m_Session.ServerCanStart(out var reason);
            startButton.interactable = isHost && canStart;
            if (isHost) statusLabel.text = canStart ? Loc.Get(players.Count == 1 ? "lobby.solo" : "lobby.all_ready") : Loc.Error(reason);
            else statusLabel.text = Loc.Get(local.LobbyReady ? "lobby.waiting_host" : "lobby.press_ready");
        }

        static string ModeSummary(MatchMode mode)
        {
            switch (mode)
            {
                case MatchMode.Turns:
                    return "<color=#FF4F8B>" + Loc.Get("mode.turns") + ":</color> " + Loc.Get("lobby.mode_turns", "count", GameRules.DiamondsToWin);
                case MatchMode.Race:
                    return "<color=#FF9A3D>" + Loc.Get("mode.race") + ":</color> " + Loc.Get("lobby.mode_race", "count", GameRules.DiamondsToWin);
                default:
                    return "<color=#3DDBD9>" + Loc.Get("mode.sync") + ":</color> " + Loc.Get("lobby.mode_sync", "count", GameRules.DiamondsToWin);
            }
        }

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
                Mode = MatchModes.At(modeSelector.Index),
                Nights = (byte)s_NightOptions[Mathf.Clamp(nightsSelector.Index, 0, s_NightOptions.Length - 1)],
                TurnSeconds = (ushort)SelectedTimer(),
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

        void OnVenueChanged(int index)
        {
            if (m_VenueChoices == null || index < 0 || index >= m_VenueChoices.Count) return;
            AppSettings.VenueId = m_VenueChoices[index].id;
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
            m_Session.ServerStartMatch();
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
