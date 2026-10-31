using System.Collections.Generic;
using System.Text;
using DeckParty.Core;
using DeckParty.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>Pre-game lobby: players, ready states, host match settings, personal venue choice and chat.</summary>
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
        }

        protected override void OnShow()
        {
            m_Session = DeckPartyNetSession.Instance;
            if (m_Session == null)
            {
                m_Menu.ShowMultiplayer();
                return;
            }

            m_Session.PlayersChanged += Refresh;
            m_Session.SettingsChanged += Refresh;
            m_Session.PhaseChanged += OnPhaseChanged;
            DeckPartyNetSession.Despawned += OnSessionDespawned;
            chat.Bind(m_Session);

            var scenarioNames = new List<string>();
            foreach (var s in ScenarioCatalog.All) scenarioNames.Add(s.Name);
            scenarioSelector.SetTitle(Terms.CharacterSet);
            scenarioSelector.SetOptions(scenarioNames, 0);

            modeSelector.SetTitle("Mode");
            modeSelector.SetOptions(MatchModes.Labels, 0);

            var nightNames = new List<string>();
            foreach (var n in s_NightOptions) nightNames.Add(n + " nights");
            nightsSelector.SetTitle("Season length");
            nightsSelector.SetOptions(nightNames, 3);

            m_VenueChoices = GameDatabase.Instance.VenueChoices();
            var venueNames = new List<string>();
            int venueIndex = 0;
            for (int i = 0; i < m_VenueChoices.Count; i++)
            {
                venueNames.Add(m_VenueChoices[i].label);
                if (m_VenueChoices[i].id == AppSettings.VenueId) venueIndex = i;
            }
            venueSelector.SetTitle("Your venue (just for you)");
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
            if (IsVisible) m_Menu.ShowMultiplayer();
        }

        void Refresh()
        {
            if (m_Session == null || !m_Session.IsSpawned) return;
            bool isHost = m_Session.IsServer;
            var service = NetworkService.Instance;

            titleLabel.text = isHost ? "Your Party Lobby" : "Party Lobby";
            codeLabel.text = string.IsNullOrEmpty(service.JoinCode) ? "-" : service.JoinCode;
            codeHintLabel.text = service.Mode == ConnectionMode.Relay
                ? (isHost ? "Share this join code with your friends." : "Join code")
                : (isHost ? "Friends join with this address (Direct IP). Port must be reachable." : "Host address");

            // Players
            var players = m_Session.PlayersSnapshot();
            for (int i = 0; i < players.Count; i++)
            {
                if (i >= m_Rows.Count) m_Rows.Add(Instantiate(playerRowPrefab, playerList));
                var p = players[i];
                bool host = m_Session.IsHostPlayer(p.ClientId);
                bool ready = host || p.LobbyReady;
                m_Rows[i].gameObject.SetActive(true);
                m_Rows[i].Set(p.DisplayName, host ? "Host" : (p.LobbyReady ? "<color=#6BE38B>Ready</color>" : "<color=#B9AED6>Not ready</color>"),
                    UIPalette.PlayerColor(p.ColorIndex), ready, host, 0, p.ClientId == m_Session.LocalClientId);
            }
            for (int i = players.Count; i < m_Rows.Count; i++) m_Rows[i].gameObject.SetActive(false);
            playerCountLabel.text = $"Players ({players.Count}/{NetworkService.MaxPlayers})";

            // Settings
            var settings = m_Session.Settings;
            m_Applying = true;
            scenarioSelector.SetIndex(IndexOfScenario(settings.ScenarioId));
            modeSelector.SetIndex(MatchModes.IndexOf(settings.Mode));
            nightsSelector.SetIndex(IndexOfNights(settings.Nights));
            m_Applying = false;
            scenarioSelector.Interactable = isHost;
            modeSelector.Interactable = isHost;
            nightsSelector.Interactable = isHost;

            var scenario = ScenarioCatalog.Get(settings.ScenarioId);
            var summary = new StringBuilder();
            summary.Append($"<b>{scenario.Name}</b> - {scenario.Description}\n");
            summary.Append(ModeSummary(settings.Mode));
            settingsSummary.text = summary.ToString();

            // Buttons
            m_Session.TryGetLocalPlayer(out var local);
            readyButton.gameObject.SetActive(!isHost);
            readyLabel.text = local.LobbyReady ? "Not Ready" : "Ready!";
            startButton.gameObject.SetActive(isHost);
            bool canStart = m_Session.ServerCanStart(out var reason);
            startButton.interactable = isHost && canStart;
            if (isHost) statusLabel.text = canStart ? (players.Count == 1 ? "You can start solo, or wait for friends to join." : "Everyone's ready!") : reason;
            else statusLabel.text = local.LobbyReady ? "Waiting for the host to start..." : "Press Ready when you're set.";
        }

        static string ModeSummary(MatchMode mode)
        {
            switch (mode)
            {
                case MatchMode.Turns:
                    return $"<color=#FF4F8B>Take Turns:</color> each night, players throw their parties one after another while everyone else watches live. " +
                           $"The season ends after the first night someone gets {GameRules.DiamondsToWin} {Terms.Diamonds}.";
                case MatchMode.Race:
                    return $"<color=#FF9A3D>Race:</color> play at your own pace. The first {GameRules.DiamondsToWin}-{Terms.Diamond} party wins instantly. Click a player to watch them.";
                default:
                    return $"<color=#3DDBD9>Synchronized:</color> everyone plays the same night at once. The first night someone gets {GameRules.DiamondsToWin} {Terms.Diamonds} ends the season. Click a player to watch them.";
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
            });
        }

        void OnVenueChanged(int index)
        {
            if (m_VenueChoices == null || index < 0 || index >= m_VenueChoices.Count) return;
            AppSettings.VenueId = m_VenueChoices[index].id;
            var settings = m_Session != null ? m_Session.Settings : MatchSettings.Default;
            var venue = GameDatabase.Instance.ResolveVenue(AppSettings.VenueId, PartyEngine.CreateGame(settings.ScenarioId, 1));
            m_Menu.PreviewVenue(venue);
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
                ToastView.Show(reason, ToastKind.Warning);
                return;
            }
            m_Session.ServerStartMatch();
        }

        void CopyCode()
        {
            var code = NetworkService.Instance.JoinCode;
            if (string.IsNullOrEmpty(code)) return;
            GUIUtility.systemCopyBuffer = code;
            ToastView.Show($"Copied {code} to the clipboard.", ToastKind.Success);
        }

        void Leave()
        {
            bool isHost = m_Session != null && m_Session.IsServer;
            ModalDialog.Show(isHost ? "Close the party?" : "Leave the party?",
                isHost ? "Everyone in the lobby will be disconnected." : "You can rejoin with the same code while the lobby is open.",
                () =>
                {
                    NetworkService.Instance.Leave();
                    m_Menu.ShowMultiplayer();
                }, isHost ? "Close Party" : "Leave", "Stay");
        }
    }
}
