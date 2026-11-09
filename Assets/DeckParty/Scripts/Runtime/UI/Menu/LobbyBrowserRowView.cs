using System;
using DeckParty.Networking;
using DeckParty.Platforms;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>One game in Find Games: host, character set and mode, players, estimated ping, Join.</summary>
    public sealed class LobbyBrowserRowView : MonoBehaviour
    {
        [SerializeField] internal TMP_Text nameLabel;
        [SerializeField] internal TMP_Text detailLabel;
        [SerializeField] internal TMP_Text playersLabel;
        [SerializeField] internal TMP_Text pingLabel;
        [SerializeField] internal Button joinButton;

        public string LobbyId { get; private set; }
        public event Action<string> JoinClicked;

        void Awake()
        {
            if (joinButton != null) joinButton.onClick.AddListener(() => JoinClicked?.Invoke(LobbyId));
        }

        public void Set(LobbyListing listing)
        {
            LobbyId = listing.Id;

            string host = listing.Get(LobbyKeys.HostName);
            if (string.IsNullOrWhiteSpace(host)) host = listing.HostName;
            nameLabel.text = string.IsNullOrWhiteSpace(host) ? Loc.Get("platform.open_game") : Loc.Get("platform.lobby_of", "host", host);

            string detail = string.Empty;
            if (int.TryParse(listing.Get(LobbyKeys.Set), out var set)) detail = ContentText.ScenarioName(set);
            if (int.TryParse(listing.Get(LobbyKeys.Mode), out var mode) && Enum.IsDefined(typeof(MatchMode), (byte)mode))
                detail += (detail.Length > 0 ? "  ·  " : string.Empty) + MatchModes.ShortName((MatchMode)mode);
            if (listing.Get(LobbyKeys.QuickMatch) == "1")
                detail += (detail.Length > 0 ? "  ·  " : string.Empty) + Loc.Get("mp.quick_match");
            detailLabel.text = detail;

            playersLabel.text = $"{listing.Members}/{listing.MaxMembers}";
            pingLabel.text = PingText(listing.PingMs);
        }

        public bool Interactable
        {
            set
            {
                if (joinButton != null) joinButton.interactable = value;
            }
        }

        static string PingText(int ms)
        {
            if (ms < 0) return "<color=#B9AED6>-</color>";
            string color = ms <= 80 ? "#6BE38B" : ms <= 150 ? "#FFC94A" : "#FF9A3D";
            return $"<color={color}>{Loc.Get("platform.ping_ms", "ms", ms)}</color>";
        }
    }
}
