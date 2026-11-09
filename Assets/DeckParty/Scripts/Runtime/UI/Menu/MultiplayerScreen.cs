using System.Collections.Generic;
using DeckParty.Networking;
using DeckParty.Platforms;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// Host or join a game. With an online platform running (Steam), the platform card replaces the join-code card:
    /// host a Steam lobby (friends only, public or invite only), Quick Match, or Find Games. Otherwise online play
    /// uses Unity Relay join codes. Direct IP / LAN is always there (unless the platform settings hide it).
    /// </summary>
    public sealed class MultiplayerScreen : UIScreen
    {
        [SerializeField] internal TMP_InputField nameInput;
        [SerializeField] internal Button hostOnlineButton;
        [SerializeField] internal TMP_InputField codeInput;
        [SerializeField] internal Button joinOnlineButton;
        [SerializeField] internal TMP_Text onlineNote;
        [SerializeField] internal TMP_InputField ipInput;
        [SerializeField] internal TMP_InputField portInput;
        [SerializeField] internal Button hostLanButton;
        [SerializeField] internal Button joinLanButton;
        [SerializeField] internal TMP_Text lanNote;
        [SerializeField] internal TMP_Text statusLabel;
        [SerializeField] internal Button backButton;

        [Header("Online platform (Steam)")]
        [Tooltip("The name row: hidden when players are named after their Steam name.")]
        [SerializeField] internal GameObject nameRow;
        [Tooltip("The Unity Relay join-code card (hidden while a platform runs multiplayer).")]
        [SerializeField] internal GameObject onlineCard;
        [SerializeField] internal GameObject lanCard;
        [SerializeField] internal GameObject platformCard;
        [SerializeField] internal TMP_Text platformTitle;
        [SerializeField] internal TMP_Text platformNote;
        [SerializeField] internal OptionSelector visibilitySelector;
        [SerializeField] internal Button hostPlatformButton;
        [SerializeField] internal Button quickMatchButton;
        [SerializeField] internal Button findGamesButton;
        [SerializeField] internal LobbyBrowserView lobbyBrowser;

        static readonly LobbyVisibility[] s_Visibilities = { LobbyVisibility.FriendsOnly, LobbyVisibility.Public, LobbyVisibility.InviteOnly };

        MainMenuController m_Menu;
        bool m_Busy;
        Vector2 m_PlatformCardHome;
        bool m_HomeKnown;

        public void Init(MainMenuController menu) => m_Menu = menu;

        void Awake()
        {
            hostOnlineButton.onClick.AddListener(HostOnline);
            joinOnlineButton.onClick.AddListener(JoinOnline);
            hostLanButton.onClick.AddListener(HostLan);
            joinLanButton.onClick.AddListener(JoinLan);
            backButton.onClick.AddListener(Back);
            nameInput.onEndEdit.AddListener(value => AppSettings.PlayerName = value);
            codeInput.onSubmit.AddListener(_ => JoinOnline());

            if (hostPlatformButton != null) hostPlatformButton.onClick.AddListener(HostPlatform);
            if (quickMatchButton != null) quickMatchButton.onClick.AddListener(QuickMatch);
            if (findGamesButton != null) findGamesButton.onClick.AddListener(() => lobbyBrowser?.Open());
            if (lobbyBrowser != null) lobbyBrowser.JoinRequested += JoinPlatformLobby;
            if (visibilitySelector != null) visibilitySelector.Changed += index => AppSettings.LastLobbyVisibility = (int)VisibilityAt(index);
            Loc.Changed += OnLanguageChanged;
        }

        protected override void OnDestroy()
        {
            Loc.Changed -= OnLanguageChanged;
            base.OnDestroy();
        }

        void OnLanguageChanged()
        {
            if (IsVisible) ApplyPlatform();
        }

        protected override void OnShow()
        {
            nameInput.text = AppSettings.PlayerName;
            codeInput.text = AppSettings.LastJoinCode;
            ipInput.text = AppSettings.LastIp;
            portInput.text = AppSettings.LastPort.ToString();

            // Steam may have been started since the game launched: try the platform again before drawing.
            OnlinePlatforms.RetryIfNeeded();
            ApplyPlatform();
            lanNote.text = Loc.Get("mp.lan_note", "address", $"<color=#3DDBD9>{NetworkService.GetLocalIPv4()}</color>");

            DeckPartyNetSession.Spawned += OnSessionSpawned;
            NetworkService.Instance.SessionEnded += OnSessionEnded;
            OnlinePlatforms.StatusChanged += ApplyPlatform;
            SetBusy(NetworkService.Instance.IsBusy, null);
            if (!m_Busy && string.IsNullOrEmpty(statusLabel.text)) SetStatus(null, false);
        }

        protected override void OnHide()
        {
            DeckPartyNetSession.Spawned -= OnSessionSpawned;
            if (NetworkService.HasInstance) NetworkService.Instance.SessionEnded -= OnSessionEnded;
            OnlinePlatforms.StatusChanged -= ApplyPlatform;
            if (lobbyBrowser != null) lobbyBrowser.Close();
        }

        protected override void OnUnloaded() => OnHide();

        /// <summary>Esc: closes Find Games first. Returns true when it did.</summary>
        public bool CloseOverlay()
        {
            if (lobbyBrowser == null || !lobbyBrowser.IsOpen) return false;
            lobbyBrowser.Close();
            return true;
        }

        /// <summary>Shows the platform card (Steam) or the join-code card, and the notes that go with them.</summary>
        void ApplyPlatform()
        {
            var platform = OnlinePlatforms.Current;
            bool blocked = OnlinePlatforms.OnlineBlocked;
            bool usePlatform = platform != null && platformCard != null;
            var settings = OnlinePlatformSettings.Current;

            if (platformCard != null) platformCard.SetActive(usePlatform || (blocked && platformCard != null));
            if (onlineCard != null) onlineCard.SetActive(!usePlatform && !blocked);
            bool showLan = !usePlatform || settings.keepDirectIp;
            if (lanCard != null) lanCard.SetActive(showLan);
            if (nameRow != null) nameRow.SetActive(!(usePlatform && OnlinePlatforms.UsePlatformNames));

            // Alone on the screen, the platform card moves to the middle.
            if (platformCard != null)
            {
                var rect = (RectTransform)platformCard.transform;
                if (!m_HomeKnown)
                {
                    m_PlatformCardHome = rect.anchoredPosition;
                    m_HomeKnown = true;
                }
                rect.anchoredPosition = showLan ? m_PlatformCardHome : new Vector2(0f, m_PlatformCardHome.y);
            }

            if (usePlatform)
            {
                bool lobbies = (platform.Features & PlatformFeatures.Lobbies) != 0;
                if (platformTitle != null) platformTitle.text = Loc.Get("mp.platform_header", "platform", platform.DisplayName);
                if (platformNote != null) platformNote.text = Loc.Get("mp.platform_note", "name", $"<color=#3DDBD9>{platform.LocalUser.Name}</color>");
                if (quickMatchButton != null) quickMatchButton.gameObject.SetActive((platform.Features & PlatformFeatures.QuickMatch) != 0);
                if (findGamesButton != null) findGamesButton.gameObject.SetActive((platform.Features & PlatformFeatures.LobbySearch) != 0);
                if (hostPlatformButton != null) hostPlatformButton.gameObject.SetActive(lobbies);
                FillVisibility();
            }
            else if (blocked)
            {
                // The project says "this platform only" and it isn't running: say why, offer nothing online.
                if (platformTitle != null) platformTitle.text = Loc.Get("mp.platform_header", "platform", OnlinePlatforms.DisplayName(OnlinePlatforms.Selected));
                if (platformNote != null) platformNote.text = $"<color=#FF9A3D>{Loc.Error(OnlinePlatforms.Problem)}</color>";
                if (quickMatchButton != null) quickMatchButton.gameObject.SetActive(false);
                if (findGamesButton != null) findGamesButton.gameObject.SetActive(false);
                if (hostPlatformButton != null) hostPlatformButton.gameObject.SetActive(false);
                if (visibilitySelector != null) visibilitySelector.gameObject.SetActive(false);
            }

            if (!usePlatform && !blocked)
            {
                string note = NetworkService.RelaySupported ? Loc.Get("mp.online_note") : $"<color=#FF9A3D>{Loc.Get("net.no_services")}</color>";
                // A platform was selected but couldn't start: say so above the join-code fallback.
                if (OnlinePlatforms.Selected != OnlinePlatformKind.Default && !string.IsNullOrEmpty(OnlinePlatforms.Problem))
                    note = $"<color=#FF9A3D>{Loc.Error(OnlinePlatforms.Problem)}</color>\n" + note;
                onlineNote.text = note;
            }
            SetBusy(m_Busy, null);
        }

        void FillVisibility()
        {
            if (visibilitySelector == null) return;
            visibilitySelector.gameObject.SetActive(true);
            visibilitySelector.SetTitle(Loc.Get("mp.visibility"));
            var labels = new List<string>();
            foreach (var visibility in s_Visibilities) labels.Add(VisibilityLabel(visibility));
            int saved = AppSettings.LastLobbyVisibility;
            var current = saved >= 0 ? (LobbyVisibility)saved : OnlinePlatformSettings.Current.steam.defaultVisibility;
            visibilitySelector.SetOptions(labels, IndexOf(current));
        }

        public static string VisibilityLabel(LobbyVisibility visibility)
        {
            switch (visibility)
            {
                case LobbyVisibility.Public: return Loc.Get("platform.visibility_public");
                case LobbyVisibility.InviteOnly: return Loc.Get("platform.visibility_invite");
                default: return Loc.Get("platform.visibility_friends");
            }
        }

        static LobbyVisibility VisibilityAt(int index) => s_Visibilities[Mathf.Clamp(index, 0, s_Visibilities.Length - 1)];

        static int IndexOf(LobbyVisibility visibility)
        {
            for (int i = 0; i < s_Visibilities.Length; i++)
                if (s_Visibilities[i] == visibility) return i;
            return 0;
        }

        public void SetStatus(string message, bool error)
        {
            if (!string.IsNullOrEmpty(message)) message = Loc.Error(message);
            statusLabel.text = string.IsNullOrEmpty(message) ? string.Empty : (error ? $"<color=#FF5A5A>{message}</color>" : message);
        }

        void SetBusy(bool busy, string message)
        {
            m_Busy = busy;
            hostOnlineButton.interactable = !busy && NetworkService.RelaySupported;
            joinOnlineButton.interactable = !busy && NetworkService.RelaySupported;
            hostLanButton.interactable = !busy;
            joinLanButton.interactable = !busy;
            bool platform = OnlinePlatforms.Active;
            if (hostPlatformButton != null) hostPlatformButton.interactable = !busy && platform;
            if (quickMatchButton != null) quickMatchButton.interactable = !busy && platform;
            if (findGamesButton != null) findGamesButton.interactable = !busy && platform;
            if (visibilitySelector != null) visibilitySelector.Interactable = !busy && platform;
            if (lobbyBrowser != null) lobbyBrowser.SetInteractable(!busy && platform);
            if (message != null) SetStatus(message, false);
        }

        string PlayerName()
        {
            AppSettings.PlayerName = nameInput.text;
            return AppSettings.PlayerName;
        }

        ushort Port()
        {
            if (!ushort.TryParse(portInput.text, out var port) || port == 0) port = NetworkService.DefaultPort;
            AppSettings.LastPort = port;
            portInput.text = port.ToString();
            return port;
        }

        // ------------------------------------------------------------------ online platform (Steam)

        async void HostPlatform()
        {
            if (m_Busy || !OnlinePlatforms.Active) return;
            var visibility = visibilitySelector != null ? VisibilityAt(visibilitySelector.Index) : OnlinePlatformSettings.Current.steam.defaultVisibility;
            AppSettings.LastLobbyVisibility = (int)visibility;
            SetBusy(true, Loc.Get("platform.creating", "platform", OnlinePlatforms.Current.DisplayName));
            var result = await NetworkService.Instance.HostPlatformAsync(PlayerName(), visibility);
            if (this == null) return;
            SetBusy(false, null);
            if (!result.Ok) SetStatus(result.Error, true);
        }

        async void QuickMatch()
        {
            if (m_Busy || !OnlinePlatforms.Active) return;
            SetBusy(true, Loc.Get("platform.quick_matching"));
            var result = await NetworkService.Instance.QuickMatchAsync(PlayerName());
            if (this == null) return;
            if (!result.Ok)
            {
                SetBusy(false, null);
                SetStatus(result.Error, true);
                return;
            }
            // Joined someone's game (waiting for the host) or opened our own (the lobby screen shows right away).
            if (!NetworkService.Instance.IsHost) SetStatus(Loc.Get("platform.connecting"), false);
            else SetBusy(false, null);
        }

        /// <summary>Joins a platform lobby: from Find Games, a pasted lobby id, or a friend's invite (the main menu calls this).</summary>
        public async void JoinPlatformLobby(string lobbyId)
        {
            if (m_Busy || string.IsNullOrEmpty(lobbyId)) return;
            if (!OnlinePlatforms.Active)
            {
                SetStatus("platform.offline", true);
                return;
            }
            if (lobbyBrowser != null) lobbyBrowser.Close();
            SetBusy(true, Loc.Get("platform.joining"));
            var result = await NetworkService.Instance.JoinPlatformLobbyAsync(lobbyId, PlayerName());
            if (this == null) return;
            if (!result.Ok)
            {
                SetBusy(false, null);
                SetStatus(result.Error, true);
                return;
            }
            SetStatus(Loc.Get("platform.connecting"), false);
        }

        // ------------------------------------------------------------------ Unity Relay and Direct IP

        async void HostOnline()
        {
            if (m_Busy) return;
            SetBusy(true, Loc.Get("mp.creating"));
            var result = await NetworkService.Instance.HostRelayAsync(PlayerName());
            if (this == null) return;
            SetBusy(false, null);
            if (!result.Ok) SetStatus(result.Error, true);
        }

        async void JoinOnline()
        {
            if (m_Busy) return;
            var code = codeInput.text.Trim().ToUpperInvariant();
            AppSettings.LastJoinCode = code;
            SetBusy(true, Loc.Get("mp.joining", "code", code));
            var result = await NetworkService.Instance.JoinRelayAsync(code, PlayerName());
            if (this == null) return;
            if (!result.Ok)
            {
                SetBusy(false, null);
                SetStatus(result.Error, true);
                return;
            }
            SetStatus(Loc.Get("mp.connected_relay"), false);
        }

        void HostLan()
        {
            if (m_Busy) return;
            var result = NetworkService.Instance.HostDirect(PlayerName(), Port());
            if (!result.Ok) SetStatus(result.Error, true);
        }

        void JoinLan()
        {
            if (m_Busy) return;
            AppSettings.LastIp = ipInput.text.Trim();
            var port = Port();
            var result = NetworkService.Instance.JoinDirect(AppSettings.LastIp, port, PlayerName());
            if (!result.Ok)
            {
                SetStatus(result.Error, true);
                return;
            }
            SetBusy(true, Loc.Get("mp.connecting", "address", $"{AppSettings.LastIp}:{port}"));
        }

        void OnSessionSpawned(DeckPartyNetSession session)
        {
            SetBusy(false, string.Empty);
            m_Menu.ShowLobby();
        }

        void OnSessionEnded(string reason)
        {
            SetBusy(false, null);
            if (!string.IsNullOrEmpty(reason)) SetStatus(reason, true);
        }

        void Back()
        {
            if (NetworkService.Instance.IsActive) NetworkService.Instance.Leave();
            SetBusy(false, string.Empty);
            m_Menu.ShowStreamerSelect(StreamerSelectMode.Multiplayer);
        }
    }
}
