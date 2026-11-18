// Epic code only exists when the EOS Plugin for Unity is installed (DECKPARTY_EOS, a version define in DeckParty.Epic.asmdef)
// and the build target is Windows, macOS or Linux. Otherwise this file compiles to nothing and the game uses the Default multiplayer.
#if DECKPARTY_EOS && UNITY_STANDALONE
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DeckParty.Networking;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using PlayEveryWare.EpicOnlineServices;
using UnityEngine;
using EosLobbySearch = Epic.OnlineServices.Lobby.LobbySearch;
using LobbyAttribute = Epic.OnlineServices.Lobby.Attribute;

namespace DeckParty.Platforms.Epic
{
    /// <summary>
    /// EOS lobbies. A lobby is how players find each other: the host creates one (public lobbies show in Find Games,
    /// friends-only ones join through the Epic overlay's presence, invite-only ones only by invite), joiners enter it, and
    /// the match itself runs over <see cref="EpicP2PTransport"/> to the host's product user id. The lobby carries the
    /// metadata (protocol version, phase, join code...) and decides who may connect.
    /// <para>
    /// Lobby changes are batched: the owner's attribute and joinability changes are collected and sent in one
    /// UpdateLobby at most twice a second, since EOS rate-limits lobby updates.
    /// </para>
    /// </summary>
    public sealed partial class EpicPlatform
    {
        /// <summary>The product user id running the match (the Netcode host).</summary>
        const string HostIdKey = "dp_hostid";
        const string VisibilityKey = "dp_vis";
        /// <summary>Each member's display name (member attribute), so the host and other members can name them.</summary>
        const string MemberNameKey = "dp_name";
        const float LobbyUpdateInterval = 0.5f;

        string m_LobbyId;
        bool m_LobbyOwner;
        LobbyVisibility m_LobbyVisibility;
        LobbyPermissionLevel m_LobbyPermission;
        readonly Dictionary<string, string> m_SentAttributes = new Dictionary<string, string>();
        readonly Dictionary<string, string> m_QueuedAttributes = new Dictionary<string, string>();
        bool? m_SentJoinable;
        bool? m_QueuedJoinable;
        bool m_UpdateInFlight;
        float m_NextLobbyUpdate;
        Task<PlatformResult<List<LobbyListing>>> m_Search;

        ulong m_InviteAcceptedNotify;
        ulong m_JoinAcceptedNotify;
        ulong m_MemberStatusNotify;

        static LobbyInterface Lobby => Sdk(m => m.GetEOSLobbyInterface());

        /// <summary>Lobbies are only found in the same bucket: the game's, per protocol version.</summary>
        string Bucket => (string.IsNullOrWhiteSpace(m_Settings.lobbyBucket) ? "deckparty" : m_Settings.lobbyBucket.Trim()) + ":" + NetworkService.ProtocolVersion;

        public string CurrentLobbyId => m_LobbyId;

        /// <summary>An EOS lobby id: a long run of letters and digits (32 hex characters for generated ids).</summary>
        public bool LooksLikeLobbyId(string text)
        {
            text = (text ?? string.Empty).Trim();
            if (text.Length < 16 || text.Length > LobbyInterface.MAX_LOBBYIDOVERRIDE_LENGTH) return false;
            foreach (char c in text)
                if (!(char.IsLetterOrDigit(c) && c < 128) && c != '-' && c != '_') return false;
            return true;
        }

        // ================================================================== create, join, leave

        public async Task<PlatformResult<PlatformLobby>> CreateLobbyAsync(LobbyVisibility visibility, int maxMembers, IReadOnlyDictionary<string, string> data)
        {
            if (!IsReady) return PlatformResult<PlatformLobby>.Fail("platform.offline");
            LeaveLobby();

            var permission = ToPermission(visibility);
            var options = new CreateLobbyOptions
            {
                LocalUserId = m_LocalProductUser,
                MaxLobbyMembers = (uint)Mathf.Clamp(maxMembers, 2, LobbyInterface.MAX_LOBBY_MEMBERS),
                PermissionLevel = permission,
                // Presence (friends joining from the Epic overlay) needs an Epic account.
                PresenceEnabled = HasEpicAccount,
                AllowInvites = true,
                BucketId = Bucket,
                // The Netcode host can't move, so neither can the lobby.
                DisableHostMigration = true,
                EnableRTCRoom = false,
                EnableJoinById = true,
                RejoinAfterKickRequiresInvite = false,
                CrossplayOptOut = false,
            };
            var done = new TaskCompletionSource<(Result Result, string Id)>();
            Lobby.CreateLobby(ref options, null, (ref CreateLobbyCallbackInfo info) => done.TrySetResult((info.ResultCode, info.LobbyId)));
            var created = await done.Task;
            if (!IsReady) return PlatformResult<PlatformLobby>.Fail("platform.offline");
            if (created.Result != Result.Success || string.IsNullOrEmpty(created.Id)) return PlatformResult<PlatformLobby>.Fail(LobbyError(created.Result, true));

            m_LobbyId = created.Id;
            m_LobbyOwner = true;
            m_LobbyVisibility = visibility;
            m_LobbyPermission = permission;
            ClearLobbyQueues();

            var attributes = new Dictionary<string, string>();
            if (data != null)
                foreach (var pair in data) attributes[pair.Key] = pair.Value ?? string.Empty;
            attributes[HostIdKey] = m_LocalUser.Id;
            attributes[VisibilityKey] = ((int)visibility).ToString();
            var updated = await UpdateLobbyNowAsync(attributes, true, m_LocalUser.Name);
            if (!IsReady) return PlatformResult<PlatformLobby>.Fail("platform.offline");
            if (updated != Result.Success)
            {
                LeaveLobby();
                return PlatformResult<PlatformLobby>.Fail(LobbyError(updated, true));
            }
            foreach (var pair in attributes) m_SentAttributes[pair.Key] = pair.Value;
            m_SentJoinable = true;

            var lobby = new PlatformLobby
            {
                Id = m_LobbyId,
                HostUserId = m_LocalUser.Id,
                MaxMembers = (int)options.MaxLobbyMembers,
                Visibility = visibility,
            };
            foreach (var pair in attributes) lobby.Data[pair.Key] = pair.Value;
            MarkFriendsChanged();
            return PlatformResult<PlatformLobby>.Success(lobby);
        }

        public async Task<PlatformResult<PlatformLobby>> JoinLobbyAsync(string lobbyId)
        {
            if (!IsReady) return PlatformResult<PlatformLobby>.Fail("platform.offline");
            lobbyId = (lobbyId ?? string.Empty).Trim();
            if (!LooksLikeLobbyId(lobbyId)) return PlatformResult<PlatformLobby>.Fail("platform.bad_lobby_id");
            if (m_LobbyId == lobbyId)
            {
                var current = ReadLobby();
                return current != null ? PlatformResult<PlatformLobby>.Success(current) : PlatformResult<PlatformLobby>.Fail("epic.lobby_gone");
            }
            LeaveLobby();

            var options = new JoinLobbyByIdOptions
            {
                LobbyId = lobbyId,
                LocalUserId = m_LocalProductUser,
                PresenceEnabled = HasEpicAccount,
                CrossplayOptOut = false,
            };
            var done = new TaskCompletionSource<Result>();
            Lobby.JoinLobbyById(ref options, null, (ref JoinLobbyByIdCallbackInfo info) => done.TrySetResult(info.ResultCode));
            var joined = await done.Task;
            if (!IsReady) return PlatformResult<PlatformLobby>.Fail("platform.offline");
            if (joined != Result.Success) return PlatformResult<PlatformLobby>.Fail(LobbyError(joined, false));

            m_LobbyId = lobbyId;
            m_LobbyOwner = false;
            ClearLobbyQueues();
            // Our name as a member attribute, so the host can name us (it can't look up product users' names itself).
            await UpdateLobbyNowAsync(null, null, m_LocalUser.Name);

            var lobby = ReadLobby();
            if (lobby == null)
            {
                LeaveLobby();
                return PlatformResult<PlatformLobby>.Fail("epic.lobby_gone");
            }
            m_LobbyVisibility = lobby.Visibility;
            MarkFriendsChanged();
            return PlatformResult<PlatformLobby>.Success(lobby);
        }

        public void LeaveLobby()
        {
            if (m_LobbyId == null) return;
            string id = m_LobbyId;
            bool owner = m_LobbyOwner;
            m_LobbyId = null;
            m_LobbyOwner = false;
            ClearLobbyQueues();
            MarkFriendsChanged();
            if (!IsReady || Lobby == null) return;

            // The host closes the lobby for everyone; members just leave.
            if (owner)
            {
                var options = new DestroyLobbyOptions { LocalUserId = m_LocalProductUser, LobbyId = id };
                Lobby.DestroyLobby(ref options, null, (ref DestroyLobbyCallbackInfo info) =>
                {
                    if (info.ResultCode != Result.Success) Debug.LogWarning($"[DeckParty] Epic: couldn't close the lobby ({info.ResultCode}).");
                });
            }
            else
            {
                var options = new LeaveLobbyOptions { LocalUserId = m_LocalProductUser, LobbyId = id };
                Lobby.LeaveLobby(ref options, null, (ref LeaveLobbyCallbackInfo info) => { });
            }
        }

        void ClearLobbyQueues()
        {
            m_SentAttributes.Clear();
            m_QueuedAttributes.Clear();
            m_SentJoinable = null;
            m_QueuedJoinable = null;
        }

        // ================================================================== lobby data

        public void SetLobbyData(string key, string value)
        {
            if (m_LobbyId == null || !m_LobbyOwner || string.IsNullOrEmpty(key)) return;
            value = value ?? string.Empty;
            // Every change goes to every member, so only send what changed.
            if (m_QueuedAttributes.TryGetValue(key, out var queued))
            {
                if (queued == value) return;
            }
            else if (m_SentAttributes.TryGetValue(key, out var sent) && sent == value)
            {
                return;
            }
            m_QueuedAttributes[key] = value;
        }

        public string GetLobbyData(string key)
        {
            if (m_LobbyId == null || !IsReady || string.IsNullOrEmpty(key)) return string.Empty;
            if (m_QueuedAttributes.TryGetValue(key, out var queued)) return queued;
            var details = CopyDetails();
            if (details == null) return string.Empty;
            try
            {
                return ReadAttribute(details, key);
            }
            finally
            {
                details.Release();
            }
        }

        public void SetLobbyJoinable(bool joinable)
        {
            if (m_LobbyId == null || !m_LobbyOwner) return;
            if (m_QueuedJoinable == null && m_SentJoinable == joinable) return;
            m_QueuedJoinable = joinable;
        }

        public bool IsLobbyMember(string userId)
        {
            if (m_LobbyId == null || !IsReady || string.IsNullOrEmpty(userId)) return false;
            var details = CopyDetails();
            if (details == null) return false;
            try
            {
                var countOptions = new LobbyDetailsGetMemberCountOptions();
                uint count = details.GetMemberCount(ref countOptions);
                for (uint i = 0; i < count; i++)
                {
                    var memberOptions = new LobbyDetailsGetMemberByIndexOptions { MemberIndex = i };
                    var member = details.GetMemberByIndex(ref memberOptions);
                    if (member != null && member.ToString() == userId) return true;
                }
                return false;
            }
            finally
            {
                details.Release();
            }
        }

        /// <summary>Sends the queued owner changes (attributes, joinable) in one update, at most twice a second.</summary>
        void FlushLobbyUpdates()
        {
            if (m_LobbyId == null || !m_LobbyOwner || m_UpdateInFlight || Time.unscaledTime < m_NextLobbyUpdate) return;
            if (m_QueuedAttributes.Count == 0 && m_QueuedJoinable == null) return;
            _ = FlushAsync();
        }

        async Task FlushAsync()
        {
            m_UpdateInFlight = true;
            m_NextLobbyUpdate = Time.unscaledTime + LobbyUpdateInterval;
            string lobby = m_LobbyId;
            var attributes = new Dictionary<string, string>(m_QueuedAttributes);
            bool? joinable = m_QueuedJoinable;
            m_QueuedAttributes.Clear();
            m_QueuedJoinable = null;
            try
            {
                var result = await UpdateLobbyNowAsync(attributes, joinable, null);
                if (m_LobbyId != lobby) return;
                if (result == Result.Success)
                {
                    foreach (var pair in attributes) m_SentAttributes[pair.Key] = pair.Value;
                    if (joinable.HasValue) m_SentJoinable = joinable;
                    return;
                }
                // Rate limited or a hiccup: put the changes back (newer ones win) and try again shortly.
                Debug.LogWarning($"[DeckParty] Epic: lobby update failed ({result}); retrying.");
                foreach (var pair in attributes)
                    if (!m_QueuedAttributes.ContainsKey(pair.Key)) m_QueuedAttributes[pair.Key] = pair.Value;
                if (m_QueuedJoinable == null) m_QueuedJoinable = joinable;
                m_NextLobbyUpdate = Time.unscaledTime + LobbyUpdateInterval * 4f;
            }
            finally
            {
                m_UpdateInFlight = false;
            }
        }

        /// <summary>One UpdateLobby: owner attributes and joinability (owner only), and our member name.</summary>
        async Task<Result> UpdateLobbyNowAsync(IReadOnlyDictionary<string, string> attributes, bool? joinable, string memberName)
        {
            if (m_LobbyId == null || Lobby == null) return Result.NotFound;
            var modificationOptions = new UpdateLobbyModificationOptions { LocalUserId = m_LocalProductUser, LobbyId = m_LobbyId };
            var result = Lobby.UpdateLobbyModification(ref modificationOptions, out var modification);
            if (result != Result.Success || modification == null) return result;
            try
            {
                if (m_LobbyOwner && attributes != null)
                {
                    foreach (var pair in attributes)
                    {
                        var add = new LobbyModificationAddAttributeOptions
                        {
                            Attribute = new AttributeData { Key = pair.Key, Value = pair.Value ?? string.Empty },
                            Visibility = LobbyAttributeVisibility.Public,
                        };
                        var added = modification.AddAttribute(ref add);
                        if (added != Result.Success) Debug.LogWarning($"[DeckParty] Epic: lobby attribute {pair.Key} rejected ({added}).");
                    }
                }
                if (m_LobbyOwner && joinable.HasValue)
                {
                    // Closed = invite only with invites off: nobody new gets in, and searches stop showing it.
                    var permission = new LobbyModificationSetPermissionLevelOptions { PermissionLevel = joinable.Value ? m_LobbyPermission : LobbyPermissionLevel.Inviteonly };
                    modification.SetPermissionLevel(ref permission);
                    var invites = new LobbyModificationSetInvitesAllowedOptions { InvitesAllowed = joinable.Value };
                    modification.SetInvitesAllowed(ref invites);
                }
                if (!string.IsNullOrEmpty(memberName))
                {
                    var member = new LobbyModificationAddMemberAttributeOptions
                    {
                        Attribute = new AttributeData { Key = MemberNameKey, Value = memberName },
                        Visibility = LobbyAttributeVisibility.Public,
                    };
                    modification.AddMemberAttribute(ref member);
                }

                var done = new TaskCompletionSource<Result>();
                var update = new UpdateLobbyOptions { LobbyModificationHandle = modification };
                Lobby.UpdateLobby(ref update, null, (ref UpdateLobbyCallbackInfo info) => done.TrySetResult(info.ResultCode));
                return await done.Task;
            }
            finally
            {
                modification.Release();
            }
        }

        // ================================================================== reading lobbies

        LobbyDetails CopyDetails()
        {
            if (m_LobbyId == null || Lobby == null) return null;
            var options = new CopyLobbyDetailsHandleOptions { LobbyId = m_LobbyId, LocalUserId = m_LocalProductUser };
            return Lobby.CopyLobbyDetailsHandle(ref options, out var details) == Result.Success ? details : null;
        }

        PlatformLobby ReadLobby()
        {
            var details = CopyDetails();
            if (details == null) return null;
            try
            {
                var infoOptions = new LobbyDetailsCopyInfoOptions();
                if (details.CopyInfo(ref infoOptions, out var info) != Result.Success || !info.HasValue) return null;
                var lobby = new PlatformLobby
                {
                    Id = info.Value.LobbyId,
                    MaxMembers = (int)info.Value.MaxMembers,
                };
                ReadAttributes(details, lobby.Data);
                string host = lobby.Get(HostIdKey);
                lobby.HostUserId = !string.IsNullOrEmpty(host) ? host : info.Value.LobbyOwnerUserId?.ToString();
                lobby.Visibility = int.TryParse(lobby.Get(VisibilityKey), out var visibility) && Enum.IsDefined(typeof(LobbyVisibility), visibility)
                    ? (LobbyVisibility)visibility
                    : LobbyVisibility.FriendsOnly;
                return lobby;
            }
            finally
            {
                details.Release();
            }
        }

        static void ReadAttributes(LobbyDetails details, Dictionary<string, string> into)
        {
            var countOptions = new LobbyDetailsGetAttributeCountOptions();
            uint count = details.GetAttributeCount(ref countOptions);
            for (uint i = 0; i < count; i++)
            {
                var options = new LobbyDetailsCopyAttributeByIndexOptions { AttrIndex = i };
                if (details.CopyAttributeByIndex(ref options, out LobbyAttribute? attribute) != Result.Success || !attribute.HasValue) continue;
                var data = attribute.Value.Data;
                if (!data.HasValue || data.Value.Key == null) continue;
                into[data.Value.Key] = AttributeText(data.Value.Value);
            }
        }

        static string ReadAttribute(LobbyDetails details, string key)
        {
            var options = new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = key };
            if (details.CopyAttributeByKey(ref options, out LobbyAttribute? attribute) != Result.Success || !attribute.HasValue) return string.Empty;
            var data = attribute.Value.Data;
            return data.HasValue ? AttributeText(data.Value.Value) : string.Empty;
        }

        static string AttributeText(AttributeDataValue value)
        {
            switch (value.ValueType)
            {
                case AttributeType.String: return value.AsUtf8 != null ? (string)value.AsUtf8 : string.Empty;
                case AttributeType.Int64: return value.AsInt64.HasValue ? value.AsInt64.Value.ToString() : string.Empty;
                case AttributeType.Double: return value.AsDouble.HasValue ? value.AsDouble.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : string.Empty;
                case AttributeType.Boolean: return value.AsBool.HasValue && value.AsBool.Value ? "1" : "0";
                default: return string.Empty;
            }
        }

        /// <summary>A lobby member's name (their member attribute), or "".</summary>
        string MemberName(string productUserId)
        {
            if (m_LobbyId == null || string.IsNullOrEmpty(productUserId)) return string.Empty;
            var details = CopyDetails();
            if (details == null) return string.Empty;
            try
            {
                var user = ProductUserId.FromString(productUserId);
                if (user == null || !user.IsValid()) return string.Empty;
                var options = new LobbyDetailsCopyMemberAttributeByKeyOptions { TargetUserId = user, AttrKey = MemberNameKey };
                if (details.CopyMemberAttributeByKey(ref options, out LobbyAttribute? attribute) != Result.Success || !attribute.HasValue) return string.Empty;
                var data = attribute.Value.Data;
                return data.HasValue ? AttributeText(data.Value.Value) : string.Empty;
            }
            finally
            {
                details.Release();
            }
        }

        // ================================================================== search

        public Task<PlatformResult<List<LobbyListing>>> FindLobbiesAsync(DeckParty.Platforms.LobbySearch search)
        {
            if (m_Search != null && !m_Search.IsCompleted) return m_Search;
            m_Search = RunSearch(search ?? new DeckParty.Platforms.LobbySearch());
            return m_Search;
        }

        async Task<PlatformResult<List<LobbyListing>>> RunSearch(DeckParty.Platforms.LobbySearch search)
        {
            if (!IsReady) return PlatformResult<List<LobbyListing>>.Fail("platform.offline");
            var createOptions = new CreateLobbySearchOptions { MaxResults = (uint)Mathf.Clamp(search.MaxResults, 1, LobbyInterface.MAX_SEARCH_RESULTS) };
            if (Lobby.CreateLobbySearch(ref createOptions, out EosLobbySearch handle) != Result.Success || handle == null)
                return PlatformResult<List<LobbyListing>>.Fail("platform.search_failed");
            try
            {
                SetSearchParameter(handle, LobbyInterface.SEARCH_BUCKET_ID, Bucket, ComparisonOp.Equal);
                foreach (var pair in search.Matches) SetSearchParameter(handle, pair.Key, pair.Value, ComparisonOp.Equal);
                var slots = new LobbySearchSetParameterOptions
                {
                    Parameter = new AttributeData { Key = LobbyInterface.SEARCH_MINSLOTSAVAILABLE, Value = (long?)Mathf.Max(1, search.FreeSlots) },
                    ComparisonOp = ComparisonOp.Greaterthanorequal,
                };
                handle.SetParameter(ref slots);

                var done = new TaskCompletionSource<Result>();
                var find = new LobbySearchFindOptions { LocalUserId = m_LocalProductUser };
                handle.Find(ref find, null, (ref LobbySearchFindCallbackInfo info) => done.TrySetResult(info.ResultCode));
                var found = await done.Task;
                if (!IsReady) return PlatformResult<List<LobbyListing>>.Fail("platform.offline");
                // No matches is a result, not an error.
                if (found == Result.NotFound) return PlatformResult<List<LobbyListing>>.Success(new List<LobbyListing>());
                if (found != Result.Success) return PlatformResult<List<LobbyListing>>.Fail("platform.search_failed");

                var list = new List<LobbyListing>();
                var countOptions = new LobbySearchGetSearchResultCountOptions();
                uint count = handle.GetSearchResultCount(ref countOptions);
                for (uint i = 0; i < count; i++)
                {
                    var copy = new LobbySearchCopySearchResultByIndexOptions { LobbyIndex = i };
                    if (handle.CopySearchResultByIndex(ref copy, out var details) != Result.Success || details == null) continue;
                    try
                    {
                        var infoOptions = new LobbyDetailsCopyInfoOptions();
                        if (details.CopyInfo(ref infoOptions, out var info) != Result.Success || !info.HasValue) continue;
                        string id = info.Value.LobbyId;
                        if (string.IsNullOrEmpty(id) || id == m_LobbyId) continue;
                        if (info.Value.AvailableSlots < (uint)Mathf.Max(1, search.FreeSlots)) continue;
                        var listing = new LobbyListing
                        {
                            Id = id,
                            MaxMembers = (int)info.Value.MaxMembers,
                            Members = (int)(info.Value.MaxMembers - info.Value.AvailableSlots),
                        };
                        ReadAttributes(details, listing.Data);
                        listing.HostName = listing.Get(NetworkLobbyHostName);
                        list.Add(listing);
                    }
                    finally
                    {
                        details.Release();
                    }
                }
                return PlatformResult<List<LobbyListing>>.Success(list);
            }
            finally
            {
                handle.Release();
            }
        }

        /// <summary>The host's name as the lobby data carries it (NetworkService writes it).</summary>
        static string NetworkLobbyHostName => LobbyKeys.HostName;

        static void SetSearchParameter(EosLobbySearch handle, string key, string value, ComparisonOp op)
        {
            var options = new LobbySearchSetParameterOptions
            {
                Parameter = new AttributeData { Key = key, Value = value ?? string.Empty },
                ComparisonOp = op,
            };
            handle.SetParameter(ref options);
        }

        // ================================================================== notifications (invites, the overlay, members)

        void StartLobbyServices()
        {
            RemoveLobbyNotifications();
            var lobby = Lobby;
            if (lobby == null) return;

            // An invite accepted (in the Epic overlay or the game's toast) while the game runs.
            var invite = new AddNotifyLobbyInviteAcceptedOptions();
            m_InviteAcceptedNotify = lobby.AddNotifyLobbyInviteAccepted(ref invite, null, (ref LobbyInviteAcceptedCallbackInfo info) =>
            {
                if (info.LobbyId != null) RequestJoin(info.LobbyId);
            });

            // Join from a friend's presence in the Epic overlay.
            var join = new AddNotifyJoinLobbyAcceptedOptions();
            m_JoinAcceptedNotify = lobby.AddNotifyJoinLobbyAccepted(ref join, null, (ref JoinLobbyAcceptedCallbackInfo info) => OnJoinFromOverlay(info.UiEventId));

            // Members coming and going (the Invite Friends panel's marks), and our own lobby closing under us.
            var members = new AddNotifyLobbyMemberStatusReceivedOptions();
            m_MemberStatusNotify = lobby.AddNotifyLobbyMemberStatusReceived(ref members, null, (ref LobbyMemberStatusReceivedCallbackInfo info) =>
            {
                if (m_LobbyId == null || info.LobbyId == null || (string)info.LobbyId != m_LobbyId) return;
                MarkFriendsChanged();
                bool aboutUs = info.TargetUserId != null && info.TargetUserId.ToString() == m_LocalUser.Id;
                if (info.CurrentStatus == LobbyMemberStatus.Closed || (aboutUs && info.CurrentStatus == LobbyMemberStatus.Kicked))
                {
                    Debug.LogWarning("[DeckParty] The Epic lobby closed.");
                    m_LobbyId = null;
                    m_LobbyOwner = false;
                    ClearLobbyQueues();
                }
            });
        }

        void OnJoinFromOverlay(ulong uiEventId)
        {
            var lobby = Lobby;
            string lobbyId = null;
            var options = new CopyLobbyDetailsHandleByUiEventIdOptions { UiEventId = uiEventId };
            if (lobby != null && lobby.CopyLobbyDetailsHandleByUiEventId(ref options, out var details) == Result.Success && details != null)
            {
                var infoOptions = new LobbyDetailsCopyInfoOptions();
                if (details.CopyInfo(ref infoOptions, out var info) == Result.Success && info.HasValue) lobbyId = info.Value.LobbyId;
                details.Release();
            }
            var ui = Sdk(m => m.GetEOSUIInterface());
            if (ui != null)
            {
                var acknowledge = new global::Epic.OnlineServices.UI.AcknowledgeEventIdOptions { UiEventId = uiEventId, Result = lobbyId != null ? Result.Success : Result.NotFound };
                ui.AcknowledgeEventId(ref acknowledge);
            }
            if (lobbyId != null) RequestJoin(lobbyId);
        }

        void RemoveLobbyNotifications()
        {
            var lobby = SdkRunning ? Lobby : null;
            if (lobby != null)
            {
                if (m_InviteAcceptedNotify != 0) lobby.RemoveNotifyLobbyInviteAccepted(m_InviteAcceptedNotify);
                if (m_JoinAcceptedNotify != 0) lobby.RemoveNotifyJoinLobbyAccepted(m_JoinAcceptedNotify);
                if (m_MemberStatusNotify != 0) lobby.RemoveNotifyLobbyMemberStatusReceived(m_MemberStatusNotify);
            }
            m_InviteAcceptedNotify = 0;
            m_JoinAcceptedNotify = 0;
            m_MemberStatusNotify = 0;
        }

        // ================================================================== mapping

        static LobbyPermissionLevel ToPermission(LobbyVisibility visibility)
        {
            switch (visibility)
            {
                case LobbyVisibility.Public: return LobbyPermissionLevel.Publicadvertised;
                case LobbyVisibility.InviteOnly: return LobbyPermissionLevel.Inviteonly;
                default: return LobbyPermissionLevel.Joinviapresence;
            }
        }

        static string LobbyError(Result result, bool creating)
        {
            switch (result)
            {
                case Result.NoConnection:
                case Result.TimedOut:
                case Result.NetworkDisconnected:
                    return "epic.no_connection";
                case Result.NotFound:
                case Result.LobbyInvalidSession:
                    return "epic.lobby_gone";
                case Result.LobbyTooManyPlayers:
                    return Core.Err.Of("net.full", NetworkService.MaxPlayers);
                case Result.LobbyNoPermission:
                case Result.LobbyNotAllowed:
                case Result.AccessDenied:
                case Result.LobbySandboxNotAllowed:
                case Result.LobbyPlatformNotAllowed:
                    return "epic.not_allowed";
                case Result.TooManyRequests:
                case Result.LimitExceeded:
                case Result.LobbyHostAtCapacity:
                case Result.LobbySandboxAtCapacity:
                case Result.LobbyDeploymentAtCapacity:
                case Result.LobbyPresenceLobbyExists:
                    return creating ? "epic.too_many_lobbies" : "epic.not_allowed";
                default:
                    return Core.Err.Of("epic.lobby_failed", result.ToString());
            }
        }
    }
}
#endif
