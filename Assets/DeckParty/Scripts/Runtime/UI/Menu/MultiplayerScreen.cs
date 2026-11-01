using DeckParty.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>Host or join a game, online through Unity Relay or directly by IP.</summary>
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

        MainMenuController m_Menu;
        bool m_Busy;

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
        }

        protected override void OnShow()
        {
            nameInput.text = AppSettings.PlayerName;
            codeInput.text = AppSettings.LastJoinCode;
            ipInput.text = AppSettings.LastIp;
            portInput.text = AppSettings.LastPort.ToString();

            onlineNote.text = NetworkService.RelaySupported ? Loc.Get("mp.online_note") : $"<color=#FF9A3D>{Loc.Get("net.no_services")}</color>";
            lanNote.text = Loc.Get("mp.lan_note", "address", $"<color=#3DDBD9>{NetworkService.GetLocalIPv4()}</color>");

            DeckPartyNetSession.Spawned += OnSessionSpawned;
            NetworkService.Instance.SessionEnded += OnSessionEnded;
            SetBusy(NetworkService.Instance.IsBusy, null);
            if (!m_Busy && string.IsNullOrEmpty(statusLabel.text)) SetStatus(null, false);
        }

        protected override void OnHide()
        {
            DeckPartyNetSession.Spawned -= OnSessionSpawned;
            if (NetworkService.HasInstance) NetworkService.Instance.SessionEnded -= OnSessionEnded;
        }

        protected override void OnUnloaded() => OnHide();

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
