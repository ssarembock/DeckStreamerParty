using System;
using System.Collections.Generic;
using DeckParty.Platforms;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// The lobby's Invite Friends panel (online platforms only): your online friends, playing this game first, each with
    /// an Invite button, a name filter, and a button for the platform's own invite dialog (the Steam overlay). Works in
    /// the Editor too, where the Steam overlay doesn't show.
    /// </summary>
    public sealed class FriendsInviteView : MonoBehaviour
    {
        [SerializeField] internal GameObject root;
        [SerializeField] internal TMP_Text statusLabel;
        [SerializeField] internal TMP_InputField searchInput;
        [SerializeField] internal RectTransform list;
        [Tooltip("An inactive row under the list, copied for each friend.")]
        [SerializeField] internal FriendRowView rowTemplate;
        [SerializeField] internal Button overlayButton;
        [SerializeField] internal TMP_Text overlayLabel;
        [SerializeField] internal Button closeButton;

        readonly List<FriendRowView> m_Rows = new List<FriendRowView>();
        /// <summary>Friends already invited to the current lobby (cleared when the lobby changes).</summary>
        readonly HashSet<ulong> m_Invited = new HashSet<ulong>();
        string m_InvitedLobby;
        IOnlinePlatform m_Platform;

        public bool IsOpen => root != null && root.activeSelf;

        void Awake()
        {
            if (rowTemplate != null) rowTemplate.gameObject.SetActive(false);
            if (root != null) root.SetActive(false);
            closeButton.onClick.AddListener(Close);
            overlayButton.onClick.AddListener(OpenOverlay);
            if (searchInput != null) searchInput.onValueChanged.AddListener(_ => Refresh());
        }

        void OnDestroy() => Unsubscribe();

        public void Open()
        {
            m_Platform = OnlinePlatforms.Current;
            if (m_Platform == null) return;

            if (m_InvitedLobby != m_Platform.CurrentLobbyId)
            {
                m_InvitedLobby = m_Platform.CurrentLobbyId;
                m_Invited.Clear();
            }

            root.SetActive(true);
            if (searchInput != null) searchInput.text = string.Empty;
            if (overlayLabel != null) overlayLabel.text = Loc.Get("platform.open_overlay", "platform", m_Platform.DisplayName);
            overlayButton.gameObject.SetActive((m_Platform.Features & PlatformFeatures.InviteOverlay) != 0);
            m_Platform.FriendsChanged += Refresh;
            m_Platform.AvatarLoaded += OnAvatarLoaded;
            Refresh();
        }

        public void Close()
        {
            Unsubscribe();
            if (root != null) root.SetActive(false);
        }

        void Unsubscribe()
        {
            if (m_Platform == null) return;
            m_Platform.FriendsChanged -= Refresh;
            m_Platform.AvatarLoaded -= OnAvatarLoaded;
            m_Platform = null;
        }

        void Refresh()
        {
            if (m_Platform == null || !IsOpen) return;
            var friends = m_Platform.GetFriends();
            string filter = searchInput != null ? searchInput.text.Trim() : string.Empty;

            int offline = 0;
            var shown = new List<PlatformFriend>();
            foreach (var friend in friends)
            {
                if (friend.Status == PlatformFriendStatus.Offline)
                {
                    offline++;
                    continue;
                }
                if (filter.Length > 0 && (friend.Name ?? string.Empty).IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                shown.Add(friend);
            }
            // Playing this game first, then online, then away / busy; alphabetical within each.
            shown.Sort((a, b) =>
            {
                int rankA = Rank(a.Status), rankB = Rank(b.Status);
                return rankA != rankB ? rankA.CompareTo(rankB) : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });

            for (int i = 0; i < shown.Count; i++)
            {
                if (i >= m_Rows.Count)
                {
                    var row = Instantiate(rowTemplate, list);
                    row.InviteClicked += Invite;
                    m_Rows.Add(row);
                }
                var f = shown[i];
                m_Rows[i].gameObject.SetActive(true);
                m_Rows[i].Set(f, m_Invited.Contains(f.Id), m_Platform.GetAvatar(f.Id));
            }
            for (int i = shown.Count; i < m_Rows.Count; i++) m_Rows[i].gameObject.SetActive(false);

            int online = friends.Count - offline;
            statusLabel.text = online == 0
                ? Loc.Get("platform.no_friends_online")
                : Loc.Get("platform.friends_count", "online", online, "offline", offline);
        }

        static int Rank(PlatformFriendStatus status)
        {
            switch (status)
            {
                case PlatformFriendStatus.InThisGame: return 0;
                case PlatformFriendStatus.Online: return 1;
                default: return 2;
            }
        }

        void Invite(FriendRowView row)
        {
            if (m_Platform == null) return;
            if (m_Platform.InviteToLobby(row.FriendId))
            {
                m_Invited.Add(row.FriendId);
                ToastView.Show(Loc.Get("platform.invite_sent", "name", row.FriendName), ToastKind.Success);
            }
            else
            {
                ToastView.Show(Loc.Get("platform.invite_failed", "name", row.FriendName), ToastKind.Warning);
            }
            Refresh();
        }

        void OnAvatarLoaded(ulong id)
        {
            if (m_Platform == null) return;
            foreach (var row in m_Rows)
                if (row.gameObject.activeSelf && row.FriendId == id) row.SetAvatar(m_Platform.GetAvatar(id));
        }

        void OpenOverlay()
        {
            if (m_Platform == null) return;
            if (!m_Platform.OpenInviteOverlay())
                ToastView.Show(Loc.Get("platform.overlay_off", "platform", m_Platform.DisplayName), ToastKind.Warning);
        }
    }
}
