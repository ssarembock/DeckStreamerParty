// Epic code only exists when the EOS Plugin for Unity is installed (DECKPARTY_EOS, a version define in DeckParty.Epic.asmdef)
// and the build target is Windows, macOS or Linux. Otherwise this file compiles to nothing and the game uses the Default multiplayer.
#if DECKPARTY_EOS && UNITY_STANDALONE
using System;
using System.Collections.Generic;
using Epic.OnlineServices;
using Epic.OnlineServices.Friends;
using Epic.OnlineServices.Presence;
using PlayEveryWare.EpicOnlineServices;
using UnityEngine;

namespace DeckParty.Platforms.Epic
{
    /// <summary>
    /// Epic friends, names, invites, presence and the Epic overlay (EOS Friends, UserInfo, Presence, UI). All of it needs
    /// an Epic account; a device-id sign-in has none of it, and the menus hide those buttons
    /// (<see cref="Features"/>).
    /// <para>
    /// Friends are Epic account ids; lobby invites go to product user ids, so the friends' product user ids are looked up
    /// (EOS Connect mappings) when the list loads. A friend who has never played this game has none and can only be
    /// invited through the overlay. EOS has no avatar pictures, so rows show the plain look.
    /// </para>
    /// </summary>
    public sealed partial class EpicPlatform
    {
        const int MappingBatch = 128;
        const float FriendsRequeryDelay = 5f;

        sealed class FriendEntry
        {
            public EpicAccountId Account;
            public string Name = string.Empty;
            public Status Status = Status.Offline;
            public bool InThisGame;
            public ProductUserId Product;
            public bool InfoAsked;
            public bool PresenceAsked;
        }

        readonly Dictionary<string, FriendEntry> m_Friends = new Dictionary<string, FriendEntry>();
        readonly HashSet<string> m_MappingAsked = new HashSet<string>();
        bool m_FriendsQueryDue;
        float m_NextFriendsQuery;
        ulong m_FriendsUpdateNotify;
        ulong m_PresenceNotify;
        string m_PresenceSignature;

        static FriendsInterface Friends => Sdk(m => m.GetEOSFriendsInterface());
        static PresenceInterface Presence => Sdk(m => m.GetEOSPresenceInterface());

        // ================================================================== friends list

        void StartFriendServices()
        {
            RemoveFriendNotifications();
            m_Friends.Clear();
            m_MappingAsked.Clear();
            m_PresenceSignature = null;
            if (!HasEpicAccount) return;

            var friends = Friends;
            if (friends != null)
            {
                var update = new AddNotifyFriendsUpdateOptions();
                m_FriendsUpdateNotify = friends.AddNotifyFriendsUpdate(ref update, null, (ref OnFriendsUpdateInfo info) => m_FriendsQueryDue = true);
            }
            var presence = Presence;
            if (presence != null)
            {
                var changed = new AddNotifyOnPresenceChangedOptions();
                m_PresenceNotify = presence.AddNotifyOnPresenceChanged(ref changed, null, (ref PresenceChangedCallbackInfo info) =>
                {
                    if (info.PresenceUserId == null) return;
                    if (m_Friends.TryGetValue(info.PresenceUserId.ToString(), out var entry))
                    {
                        ReadPresence(entry);
                        MarkFriendsChanged();
                    }
                });
            }
            QueryFriends();
        }

        void TickFriends()
        {
            if (m_FriendsQueryDue && HasEpicAccount && Time.unscaledTime >= m_NextFriendsQuery) QueryFriends();
        }

        void QueryFriends()
        {
            m_FriendsQueryDue = false;
            m_NextFriendsQuery = Time.unscaledTime + FriendsRequeryDelay;
            var friends = Friends;
            if (friends == null || !HasEpicAccount) return;
            var options = new QueryFriendsOptions { LocalUserId = m_LocalAccount };
            friends.QueryFriends(ref options, null, (ref QueryFriendsCallbackInfo info) =>
            {
                if (info.ResultCode == Result.Success) RebuildFriends();
                else Debug.LogWarning($"[DeckParty] Epic: couldn't load the friends list ({info.ResultCode}).");
            });
        }

        void RebuildFriends()
        {
            var friends = Friends;
            if (friends == null || !HasEpicAccount) return;
            var countOptions = new GetFriendsCountOptions { LocalUserId = m_LocalAccount };
            int count = friends.GetFriendsCount(ref countOptions);
            var seen = new HashSet<string>();
            var unmapped = new List<string>();
            for (int i = 0; i < count; i++)
            {
                var indexOptions = new GetFriendAtIndexOptions { LocalUserId = m_LocalAccount, Index = i };
                var id = friends.GetFriendAtIndex(ref indexOptions);
                if (id == null || !id.IsValid()) continue;
                var statusOptions = new GetStatusOptions { LocalUserId = m_LocalAccount, TargetUserId = id };
                if (friends.GetStatus(ref statusOptions) != FriendsStatus.Friends) continue;

                string key = id.ToString();
                seen.Add(key);
                if (!m_Friends.TryGetValue(key, out var entry)) m_Friends[key] = entry = new FriendEntry { Account = id };
                AskUserInfo(entry);
                AskPresence(entry);
                if (entry.Product == null && m_MappingAsked.Add(key)) unmapped.Add(key);
            }

            var gone = new List<string>();
            foreach (var key in m_Friends.Keys)
                if (!seen.Contains(key)) gone.Add(key);
            foreach (var key in gone) m_Friends.Remove(key);

            for (int start = 0; start < unmapped.Count; start += MappingBatch)
                QueryProductIds(unmapped.GetRange(start, Math.Min(MappingBatch, unmapped.Count - start)));
            MarkFriendsChanged();
        }

        void AskUserInfo(FriendEntry entry)
        {
            if (entry.InfoAsked)
            {
                string name = BestDisplayName(m_LocalAccount, entry.Account);
                if (!string.IsNullOrEmpty(name)) entry.Name = name;
                return;
            }
            entry.InfoAsked = true;
            var userInfo = Sdk(m => m.GetEOSUserInfoInterface());
            if (userInfo == null) return;
            var options = new global::Epic.OnlineServices.UserInfo.QueryUserInfoOptions { LocalUserId = m_LocalAccount, TargetUserId = entry.Account };
            userInfo.QueryUserInfo(ref options, null, (ref global::Epic.OnlineServices.UserInfo.QueryUserInfoCallbackInfo info) =>
            {
                if (info.ResultCode != Result.Success || !HasEpicAccount) return;
                entry.Name = BestDisplayName(m_LocalAccount, entry.Account);
                MarkFriendsChanged();
            });
        }

        void AskPresence(FriendEntry entry)
        {
            if (entry.PresenceAsked)
            {
                ReadPresence(entry);
                return;
            }
            entry.PresenceAsked = true;
            var presence = Presence;
            if (presence == null) return;
            var options = new QueryPresenceOptions { LocalUserId = m_LocalAccount, TargetUserId = entry.Account };
            presence.QueryPresence(ref options, null, (ref QueryPresenceCallbackInfo info) =>
            {
                if (info.ResultCode != Result.Success || !HasEpicAccount) return;
                ReadPresence(entry);
                MarkFriendsChanged();
            });
        }

        void ReadPresence(FriendEntry entry)
        {
            var presence = Presence;
            if (presence == null || !HasEpicAccount) return;
            var options = new CopyPresenceOptions { LocalUserId = m_LocalAccount, TargetUserId = entry.Account };
            if (presence.CopyPresence(ref options, out Info? info) != Result.Success || !info.HasValue) return;
            entry.Status = info.Value.Status;
            string product = info.Value.ProductId;
            entry.InThisGame = entry.Status != Status.Offline && !string.IsNullOrEmpty(m_ProductId) && product == m_ProductId;
        }

        /// <summary>Friends' product user ids (invites go to those); a friend who never played this game has none.</summary>
        void QueryProductIds(List<string> accountIds)
        {
            var connect = Connect;
            if (connect == null || m_LocalProductUser == null || accountIds.Count == 0) return;
            var ids = new Utf8String[accountIds.Count];
            for (int i = 0; i < ids.Length; i++) ids[i] = accountIds[i];
            var options = new global::Epic.OnlineServices.Connect.QueryExternalAccountMappingsOptions
            {
                LocalUserId = m_LocalProductUser,
                AccountIdType = ExternalAccountType.Epic,
                ExternalAccountIds = ids,
            };
            connect.QueryExternalAccountMappings(ref options, null, (ref global::Epic.OnlineServices.Connect.QueryExternalAccountMappingsCallbackInfo info) =>
            {
                if (info.ResultCode != Result.Success || Connect == null || m_LocalProductUser == null) return;
                foreach (var key in accountIds)
                {
                    if (!m_Friends.TryGetValue(key, out var entry)) continue;
                    var mapping = new global::Epic.OnlineServices.Connect.GetExternalAccountMappingsOptions
                    {
                        LocalUserId = m_LocalProductUser,
                        AccountIdType = ExternalAccountType.Epic,
                        TargetExternalUserId = key,
                    };
                    var product = Connect.GetExternalAccountMapping(ref mapping);
                    if (product != null && product.IsValid()) entry.Product = product;
                }
                MarkFriendsChanged();
            });
        }

        void RemoveFriendNotifications()
        {
            if (SdkRunning)
            {
                if (m_FriendsUpdateNotify != 0) Friends?.RemoveNotifyFriendsUpdate(m_FriendsUpdateNotify);
                if (m_PresenceNotify != 0) Presence?.RemoveNotifyOnPresenceChanged(m_PresenceNotify);
            }
            m_FriendsUpdateNotify = 0;
            m_PresenceNotify = 0;
        }

        public List<PlatformFriend> GetFriends()
        {
            var list = new List<PlatformFriend>();
            if (!IsReady || !HasEpicAccount) return list;
            foreach (var pair in m_Friends)
            {
                var entry = pair.Value;
                list.Add(new PlatformFriend
                {
                    Id = pair.Key,
                    Name = entry.Name ?? string.Empty,
                    Status = FriendStatus(entry),
                    InYourLobby = entry.Product != null && IsLobbyMember(entry.Product.ToString()),
                });
            }
            return list;
        }

        static PlatformFriendStatus FriendStatus(FriendEntry entry)
        {
            if (entry.InThisGame) return PlatformFriendStatus.InThisGame;
            switch (entry.Status)
            {
                case Status.Online: return PlatformFriendStatus.Online;
                case Status.Away:
                case Status.ExtendedAway: return PlatformFriendStatus.Away;
                case Status.DoNotDisturb: return PlatformFriendStatus.Busy;
                default: return PlatformFriendStatus.Offline;
            }
        }

        // ================================================================== names, invites, overlay, profiles

        /// <summary>A name for a product user id (lobby members, players) or an Epic account id (friends).</summary>
        public string GetUserName(string userId)
        {
            if (!IsReady || string.IsNullOrEmpty(userId)) return string.Empty;
            if (userId == m_LocalUser.Id || (m_LocalAccount != null && userId == m_LocalAccount.ToString())) return m_LocalUser.Name;
            if (m_Friends.TryGetValue(userId, out var friend)) return friend.Name ?? string.Empty;

            string member = MemberName(userId);
            if (!string.IsNullOrEmpty(member)) return member;

            // EOS Connect sometimes knows a product user's display name too.
            var connect = Connect;
            var user = ProductUserId.FromString(userId);
            if (connect != null && user != null && user.IsValid())
            {
                var options = new global::Epic.OnlineServices.Connect.CopyProductUserInfoOptions { TargetUserId = user };
                if (connect.CopyProductUserInfo(ref options, out var info) == Result.Success && info.HasValue && info.Value.DisplayName != null)
                    return info.Value.DisplayName;
            }
            return string.Empty;
        }

        public bool InviteToLobby(string friendId)
        {
            if (!IsReady || !HasEpicAccount || m_LobbyId == null || string.IsNullOrEmpty(friendId)) return false;
            if (!m_Friends.TryGetValue(friendId, out var entry) || entry.Product == null) return false;
            var options = new global::Epic.OnlineServices.Lobby.SendInviteOptions { LobbyId = m_LobbyId, LocalUserId = m_LocalProductUser, TargetUserId = entry.Product };
            Lobby.SendInvite(ref options, null, (ref global::Epic.OnlineServices.Lobby.SendInviteCallbackInfo info) =>
            {
                if (info.ResultCode != Result.Success) Debug.LogWarning($"[DeckParty] Epic: the invite to {entry.Name} failed ({info.ResultCode}).");
            });
            return true;
        }

        /// <summary>The Epic overlay's friends list: invite from there (it works for friends who never played the game too).</summary>
        public bool OpenInviteOverlay()
        {
            var ui = Sdk(m => m.GetEOSUIInterface());
            if (!IsReady || !HasEpicAccount || ui == null) return false;
            var options = new global::Epic.OnlineServices.UI.ShowFriendsOptions { LocalUserId = m_LocalAccount };
            ui.ShowFriends(ref options, null, (ref global::Epic.OnlineServices.UI.ShowFriendsCallbackInfo info) =>
            {
                if (info.ResultCode != Result.Success) Debug.LogWarning($"[DeckParty] Epic: the overlay didn't open ({info.ResultCode}).");
            });
            return true;
        }

        /// <summary>EOS has no profile pages on PC.</summary>
        public bool OpenProfile(string userId) => false;

        /// <summary>EOS has no avatar pictures.</summary>
        public Texture2D GetAvatar(string userId) => null;

        // ================================================================== presence

        public void SetPresence(PresenceState state, string lobbyId, int members, int maxMembers, bool joinable)
        {
            if (!IsReady || !HasEpicAccount) return;
            // The match reports player changes many times a minute: only talk to Epic when something visible changed.
            string signature = $"{state}|{lobbyId}|{members}|{maxMembers}|{joinable}|{Loc.LanguageCode}";
            if (signature == m_PresenceSignature) return;
            m_PresenceSignature = signature;

            string text;
            switch (state)
            {
                case PresenceState.SinglePlayer: text = Loc.Get("platform.presence_solo"); break;
                case PresenceState.InLobby: text = Loc.Get("platform.presence_lobby", "count", members, "max", maxMembers); break;
                case PresenceState.InMatch: text = Loc.Get("platform.presence_match", "count", members); break;
                default: text = Loc.Get("platform.presence_menu"); break;
            }
            if (text.Length > 255) text = text.Substring(0, 255);

            var presence = Presence;
            if (presence == null) return;
            var create = new CreatePresenceModificationOptions { LocalUserId = m_LocalAccount };
            if (presence.CreatePresenceModification(ref create, out var modification) != Result.Success || modification == null) return;
            try
            {
                var status = new PresenceModificationSetStatusOptions { Status = Status.Online };
                modification.SetStatus(ref status);
                var rich = new PresenceModificationSetRawRichTextOptions { RichText = text };
                modification.SetRawRichText(ref rich);
                // Joining from the overlay goes through the lobby (it has presence on while it's open).
                var options = new SetPresenceOptions { LocalUserId = m_LocalAccount, PresenceModificationHandle = modification };
                presence.SetPresence(ref options, null, (ref SetPresenceCallbackInfo info) =>
                {
                    if (info.ResultCode != Result.Success) Debug.LogWarning($"[DeckParty] Epic: presence wasn't updated ({info.ResultCode}).");
                });
            }
            finally
            {
                modification.Release();
            }
        }

        void ClearPresence() => m_PresenceSignature = null;
    }
}
#endif
