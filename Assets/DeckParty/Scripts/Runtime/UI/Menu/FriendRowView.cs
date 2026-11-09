using System;
using DeckParty.Platforms;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>A friend in the lobby's Invite Friends panel: avatar, name, status and an Invite button.</summary>
    public sealed class FriendRowView : MonoBehaviour
    {
        [SerializeField] internal RawImage avatar;
        [Tooltip("Shown while the avatar downloads (or for friends without one).")]
        [SerializeField] internal Image avatarPlaceholder;
        [SerializeField] internal TMP_Text nameLabel;
        [SerializeField] internal TMP_Text statusLabel;
        [SerializeField] internal Button inviteButton;
        [SerializeField] internal TMP_Text inviteLabel;

        public ulong FriendId { get; private set; }
        public string FriendName { get; private set; }
        public event Action<FriendRowView> InviteClicked;

        void Awake()
        {
            if (inviteButton != null) inviteButton.onClick.AddListener(() => InviteClicked?.Invoke(this));
        }

        public void Set(PlatformFriend friend, bool invited, Texture avatarTexture)
        {
            FriendId = friend.Id;
            FriendName = friend.Name;
            nameLabel.text = friend.Name;
            statusLabel.text = StatusText(friend);
            SetAvatar(avatarTexture);

            bool canInvite = !friend.InYourLobby && !invited;
            inviteButton.interactable = canInvite;
            inviteLabel.text = Loc.Get(friend.InYourLobby ? "platform.in_lobby" : invited ? "platform.invited" : "platform.invite");
        }

        public void SetAvatar(Texture texture)
        {
            if (avatar != null)
            {
                avatar.texture = texture;
                avatar.enabled = texture != null;
            }
            if (avatarPlaceholder != null) avatarPlaceholder.enabled = texture == null;
        }

        static string StatusText(PlatformFriend friend)
        {
            switch (friend.Status)
            {
                case PlatformFriendStatus.InThisGame: return $"<color=#6BE38B>{Loc.Get("platform.status_in_game")}</color>";
                case PlatformFriendStatus.Away: return $"<color=#FFC94A>{Loc.Get("platform.status_away")}</color>";
                case PlatformFriendStatus.Busy: return $"<color=#FF9A3D>{Loc.Get("platform.status_busy")}</color>";
                case PlatformFriendStatus.Offline: return $"<color=#8E84A8>{Loc.Get("platform.status_offline")}</color>";
                default: return $"<color=#3DDBD9>{Loc.Get("platform.status_online")}</color>";
            }
        }
    }
}
