// Steam code only exists when Steamworks.NET is installed (DECKPARTY_STEAMWORKS, a version define in DeckParty.Steam.asmdef)
// and the build target is Windows, macOS or Linux. Otherwise this file compiles to nothing and the game uses the Default multiplayer.
#if DECKPARTY_STEAMWORKS && UNITY_STANDALONE
using System;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;

namespace DeckParty.Platforms.Steam
{
    /// <summary>Steam friends (ISteamFriends): the friends list, names, avatars, invites, profiles and the overlay.</summary>
    public sealed partial class SteamPlatform
    {
        readonly Dictionary<ulong, Texture2D> m_Avatars = new Dictionary<ulong, Texture2D>();
        readonly HashSet<ulong> m_InfoRequested = new HashSet<ulong>();

        public List<PlatformFriend> GetFriends()
        {
            var friends = new List<PlatformFriend>();
            if (!IsReady) return friends;

            int count = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
            for (int i = 0; i < count; i++)
            {
                var id = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
                if (!id.IsValid()) continue;
                friends.Add(new PlatformFriend
                {
                    Id = id.m_SteamID,
                    Name = SteamFriends.GetFriendPersonaName(id),
                    Status = FriendStatus(id),
                    InYourLobby = IsLobbyMember(id.m_SteamID),
                });
            }
            return friends;
        }

        PlatformFriendStatus FriendStatus(CSteamID id)
        {
            if (SteamFriends.GetFriendGamePlayed(id, out var game) && game.m_gameID.IsValid() && game.m_gameID.AppID() == m_AppId)
                return PlatformFriendStatus.InThisGame;
            switch (SteamFriends.GetFriendPersonaState(id))
            {
                case EPersonaState.k_EPersonaStateOffline:
                case EPersonaState.k_EPersonaStateInvisible:
                    return PlatformFriendStatus.Offline;
                case EPersonaState.k_EPersonaStateAway:
                case EPersonaState.k_EPersonaStateSnooze:
                    return PlatformFriendStatus.Away;
                case EPersonaState.k_EPersonaStateBusy:
                    return PlatformFriendStatus.Busy;
                default:
                    return PlatformFriendStatus.Online;
            }
        }

        public string GetUserName(ulong userId)
        {
            if (!IsReady || userId == 0) return string.Empty;
            if (userId == m_LocalUser.Id) return SteamFriends.GetPersonaName();
            var id = new CSteamID(userId);
            // Known for friends and anyone sharing a lobby with us; otherwise ask Steam (PersonaStateChange_t follows).
            string name = SteamFriends.GetFriendPersonaName(id);
            if (string.IsNullOrEmpty(name) || name == "[unknown]")
            {
                if (m_InfoRequested.Add(userId)) SteamFriends.RequestUserInformation(id, true);
                return string.Empty;
            }
            return name;
        }

        public bool InviteToLobby(ulong friendId)
        {
            if (!IsReady || !m_Lobby.IsValid() || friendId == 0) return false;
            return SteamMatchmaking.InviteUserToLobby(m_Lobby, new CSteamID(friendId));
        }

        public bool OpenInviteOverlay()
        {
            if (!IsReady || !m_Lobby.IsValid() || !SteamUtils.IsOverlayEnabled()) return false;
            SteamFriends.ActivateGameOverlayInviteDialog(m_Lobby);
            return true;
        }

        public bool OpenProfile(ulong userId)
        {
            if (!IsReady || userId == 0) return false;
            if (SteamUtils.IsOverlayEnabled())
            {
                SteamFriends.ActivateGameOverlayToUser("steamid", new CSteamID(userId));
                return true;
            }
            // No overlay (e.g. in the Editor): the profile in the browser.
            Application.OpenURL("https://steamcommunity.com/profiles/" + userId);
            return true;
        }

        // ------------------------------------------------------------------ avatars

        public Texture2D GetAvatar(ulong userId)
        {
            if (!IsReady || userId == 0) return null;
            if (m_Avatars.TryGetValue(userId, out var cached) && cached != null) return cached;

            var id = new CSteamID(userId);
            int image = SteamFriends.GetMediumFriendAvatar(id);
            if (image == -1) return null; // Downloading: AvatarImageLoaded_t follows.
            if (image == 0)
            {
                // Steam doesn't know this user's avatar yet: ask once (PersonaStateChange_t follows).
                if (m_InfoRequested.Add(userId)) SteamFriends.RequestUserInformation(id, false);
                return null;
            }

            var texture = ToTexture(image);
            if (texture != null) m_Avatars[userId] = texture;
            return texture;
        }

        static Texture2D ToTexture(int image)
        {
            if (!SteamUtils.GetImageSize(image, out uint width, out uint height) || width == 0 || height == 0) return null;
            int w = (int)width, h = (int)height, stride = w * 4;
            var rgba = new byte[stride * h];
            if (!SteamUtils.GetImageRGBA(image, rgba, rgba.Length)) return null;

            // Steam's rows run top to bottom, Unity's bottom to top.
            var flipped = new byte[rgba.Length];
            for (int y = 0; y < h; y++) Buffer.BlockCopy(rgba, y * stride, flipped, (h - 1 - y) * stride, stride);

            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "SteamAvatar",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };
            texture.LoadRawTextureData(flipped);
            texture.Apply(false, true);
            return texture;
        }

        void OnAvatarImageLoaded(AvatarImageLoaded_t e)
        {
            ulong id = e.m_steamID.m_SteamID;
            ForgetAvatar(id);
            RaiseAvatarLoaded(id);
        }

        void OnPersonaChanged(PersonaStateChange_t e)
        {
            m_InfoRequested.Remove(e.m_ulSteamID);
            if ((e.m_nChangeFlags & EPersonaChange.k_EPersonaChangeAvatar) != 0)
            {
                ForgetAvatar(e.m_ulSteamID);
                RaiseAvatarLoaded(e.m_ulSteamID);
            }
            MarkFriendsChanged();
        }

        void ForgetAvatar(ulong id)
        {
            if (!m_Avatars.TryGetValue(id, out var texture)) return;
            m_Avatars.Remove(id);
            if (texture != null) UnityEngine.Object.Destroy(texture);
        }

        void ClearAvatars()
        {
            foreach (var texture in m_Avatars.Values)
                if (texture != null) UnityEngine.Object.Destroy(texture);
            m_Avatars.Clear();
            m_InfoRequested.Clear();
        }

        void RaiseAvatarLoaded(ulong id)
        {
            try
            {
                AvatarLoaded?.Invoke(id);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
#endif
