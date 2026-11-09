// Steam code only exists when Steamworks.NET is installed (DECKPARTY_STEAMWORKS, a version define in DeckParty.Steam.asmdef)
// and the build target is Windows, macOS or Linux. Otherwise this file compiles to nothing and the game uses the Default multiplayer.
#if DECKPARTY_STEAMWORKS && UNITY_STANDALONE
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Steamworks;
using UnityEngine;

namespace DeckParty.Platforms.Steam
{
    /// <summary>
    /// Steam lobbies (ISteamMatchmaking). A lobby is how players find each other: the host creates one, friends join it
    /// from an invite or the friends list, strangers find public ones through a lobby search. The match itself runs over
    /// <see cref="SteamNetworkingTransport"/> straight to the host's SteamID; the lobby carries the metadata
    /// (protocol version, phase, host...) and decides who may connect.
    /// </summary>
    public sealed partial class SteamPlatform
    {
        /// <summary>The SteamID running the match (the Steam owner can change hands if the host drops, the Netcode host can't).</summary>
        const string HostIdKey = "dp_hostid";
        /// <summary>The host's Steam ping location: searches estimate the ping to each game from it without connecting.</summary>
        const string PingKey = "dp_ping";
        const string VisibilityKey = "dp_vis";

        CSteamID m_Lobby = CSteamID.Nil;
        LobbyVisibility m_LobbyVisibility;
        readonly Dictionary<string, string> m_SentLobbyData = new Dictionary<string, string>();
        bool? m_SentJoinable;
        Task<PlatformResult<List<LobbyListing>>> m_Search;

        public string CurrentLobbyId => m_Lobby.IsValid() ? m_Lobby.m_SteamID.ToString() : null;

        public async Task<PlatformResult<PlatformLobby>> CreateLobbyAsync(LobbyVisibility visibility, int maxMembers, IReadOnlyDictionary<string, string> data)
        {
            if (!IsReady) return PlatformResult<PlatformLobby>.Fail("platform.offline");
            LeaveLobby();

            var (ok, created) = await SteamCall.Run<LobbyCreated_t>(SteamMatchmaking.CreateLobby(ToLobbyType(visibility), maxMembers));
            if (!IsReady) return PlatformResult<PlatformLobby>.Fail("platform.offline");
            if (!ok) return PlatformResult<PlatformLobby>.Fail("steam.no_connection");
            if (created.m_eResult != EResult.k_EResultOK) return PlatformResult<PlatformLobby>.Fail(CreateError(created.m_eResult));

            m_Lobby = new CSteamID(created.m_ulSteamIDLobby);
            m_LobbyVisibility = visibility;
            m_SentLobbyData.Clear();
            m_SentJoinable = null;

            var lobby = new PlatformLobby
            {
                Id = CurrentLobbyId,
                HostUserId = m_LocalUser.Id,
                MaxMembers = maxMembers,
                Visibility = visibility,
            };
            if (data != null)
                foreach (var pair in data)
                {
                    SetLobbyData(pair.Key, pair.Value);
                    lobby.Data[pair.Key] = pair.Value;
                }
            SetLobbyData(HostIdKey, m_LocalUser.Id.ToString());
            SetLobbyData(VisibilityKey, ((int)visibility).ToString());
            if (TryGetPingLocation(out var ping)) SetLobbyData(PingKey, ping);
            SetLobbyJoinable(true);
            MarkFriendsChanged();
            return PlatformResult<PlatformLobby>.Success(lobby);
        }

        public async Task<PlatformResult<PlatformLobby>> JoinLobbyAsync(string lobbyId)
        {
            if (!IsReady) return PlatformResult<PlatformLobby>.Fail("platform.offline");
            if (!ulong.TryParse((lobbyId ?? string.Empty).Trim(), out var raw) || !new CSteamID(raw).IsLobby())
                return PlatformResult<PlatformLobby>.Fail("platform.bad_lobby_id");

            var id = new CSteamID(raw);
            if (m_Lobby == id) return PlatformResult<PlatformLobby>.Success(ReadLobby(id));
            LeaveLobby();

            var (ok, entered) = await SteamCall.Run<LobbyEnter_t>(SteamMatchmaking.JoinLobby(id));
            if (!IsReady) return PlatformResult<PlatformLobby>.Fail("platform.offline");
            if (!ok) return PlatformResult<PlatformLobby>.Fail("steam.no_connection");
            var response = (EChatRoomEnterResponse)entered.m_EChatRoomEnterResponse;
            if (response != EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
            {
                // Steam may still list us as entering; make sure we're out.
                SteamMatchmaking.LeaveLobby(id);
                return PlatformResult<PlatformLobby>.Fail(JoinError(response));
            }

            m_Lobby = id;
            m_SentLobbyData.Clear();
            m_SentJoinable = null;
            var lobby = ReadLobby(id);
            m_LobbyVisibility = lobby.Visibility;
            MarkFriendsChanged();
            return PlatformResult<PlatformLobby>.Success(lobby);
        }

        PlatformLobby ReadLobby(CSteamID id)
        {
            var lobby = new PlatformLobby
            {
                Id = id.m_SteamID.ToString(),
                MaxMembers = SteamMatchmaking.GetLobbyMemberLimit(id),
            };
            ReadLobbyData(id, lobby.Data);
            lobby.HostUserId = ulong.TryParse(lobby.Get(HostIdKey), out var host) ? host : SteamMatchmaking.GetLobbyOwner(id).m_SteamID;
            lobby.Visibility = int.TryParse(lobby.Get(VisibilityKey), out var visibility) && Enum.IsDefined(typeof(LobbyVisibility), visibility)
                ? (LobbyVisibility)visibility
                : LobbyVisibility.FriendsOnly;
            return lobby;
        }

        static void ReadLobbyData(CSteamID id, Dictionary<string, string> into)
        {
            int count = SteamMatchmaking.GetLobbyDataCount(id);
            for (int i = 0; i < count; i++)
                if (SteamMatchmaking.GetLobbyDataByIndex(id, i, out var key, Constants.k_nMaxLobbyKeyLength, out var value, Constants.k_cubChatMetadataMax))
                    into[key] = value;
        }

        public Task<PlatformResult<List<LobbyListing>>> FindLobbiesAsync(LobbySearch search)
        {
            // The filters are global to the next RequestLobbyList call, so searches never overlap: share the running one.
            if (m_Search != null && !m_Search.IsCompleted) return m_Search;
            m_Search = RunSearch(search ?? new LobbySearch());
            return m_Search;
        }

        async Task<PlatformResult<List<LobbyListing>>> RunSearch(LobbySearch search)
        {
            if (!IsReady) return PlatformResult<List<LobbyListing>>.Fail("platform.offline");

            foreach (var pair in search.Matches)
                SteamMatchmaking.AddRequestLobbyListStringFilter(pair.Key, pair.Value, ELobbyComparison.k_ELobbyComparisonEqual);
            SteamMatchmaking.AddRequestLobbyListFilterSlotsAvailable(Mathf.Max(1, search.FreeSlots));
            SteamMatchmaking.AddRequestLobbyListDistanceFilter(ToDistance(search.Distance));
            SteamMatchmaking.AddRequestLobbyListResultCountFilter(Mathf.Clamp(search.MaxResults, 1, 200));

            var (ok, found) = await SteamCall.Run<LobbyMatchList_t>(SteamMatchmaking.RequestLobbyList());
            if (!IsReady) return PlatformResult<List<LobbyListing>>.Fail("platform.offline");
            if (!ok) return PlatformResult<List<LobbyListing>>.Fail("steam.no_connection");

            var list = new List<LobbyListing>();
            for (int i = 0; i < found.m_nLobbiesMatching; i++)
            {
                var id = SteamMatchmaking.GetLobbyByIndex(i);
                if (!id.IsValid() || id == m_Lobby) continue;

                var listing = new LobbyListing
                {
                    Id = id.m_SteamID.ToString(),
                    Members = SteamMatchmaking.GetNumLobbyMembers(id),
                    MaxMembers = SteamMatchmaking.GetLobbyMemberLimit(id),
                };
                ReadLobbyData(id, listing.Data);
                listing.HostName = GetUserName(SteamMatchmaking.GetLobbyOwner(id).m_SteamID);
                listing.PingMs = EstimatePing(listing.Get(PingKey));
                list.Add(listing);
            }
            return PlatformResult<List<LobbyListing>>.Success(list);
        }

        public void LeaveLobby()
        {
            if (m_Lobby.IsValid() && Running) SteamMatchmaking.LeaveLobby(m_Lobby);
            bool hadLobby = m_Lobby.IsValid();
            m_Lobby = CSteamID.Nil;
            m_SentLobbyData.Clear();
            m_SentJoinable = null;
            if (hadLobby) MarkFriendsChanged();
        }

        public void SetLobbyData(string key, string value)
        {
            if (!m_Lobby.IsValid() || !Running || string.IsNullOrEmpty(key)) return;
            value = value ?? string.Empty;
            // Every change is sent to every member, so only send what changed.
            if (m_SentLobbyData.TryGetValue(key, out var sent) && sent == value) return;
            if (SteamMatchmaking.SetLobbyData(m_Lobby, key, value)) m_SentLobbyData[key] = value;
        }

        public string GetLobbyData(string key) =>
            m_Lobby.IsValid() && Running && !string.IsNullOrEmpty(key) ? SteamMatchmaking.GetLobbyData(m_Lobby, key) : string.Empty;

        public void SetLobbyJoinable(bool joinable)
        {
            if (!m_Lobby.IsValid() || !Running || m_SentJoinable == joinable) return;
            if (SteamMatchmaking.SetLobbyJoinable(m_Lobby, joinable)) m_SentJoinable = joinable;
        }

        public bool IsLobbyMember(ulong userId)
        {
            if (!m_Lobby.IsValid() || !Running || userId == 0) return false;
            int count = SteamMatchmaking.GetNumLobbyMembers(m_Lobby);
            for (int i = 0; i < count; i++)
                if (SteamMatchmaking.GetLobbyMemberByIndex(m_Lobby, i).m_SteamID == userId) return true;
            return false;
        }

        void OnLobbyChatUpdate(LobbyChatUpdate_t e)
        {
            if (e.m_ulSteamIDLobby != m_Lobby.m_SteamID) return;
            // Someone entered or left: the Invite Friends panel's "in your lobby" marks change.
            MarkFriendsChanged();
        }

        void OnLobbyKicked(LobbyKicked_t e)
        {
            if (e.m_ulSteamIDLobby != m_Lobby.m_SteamID) return;
            Debug.LogWarning("[DeckParty] Removed from the Steam lobby (lost connection to Steam?).");
            m_Lobby = CSteamID.Nil;
            m_SentLobbyData.Clear();
            m_SentJoinable = null;
            MarkFriendsChanged();
        }

        // ------------------------------------------------------------------ ping estimates (Steam Datagram Relay)

        static bool TryGetPingLocation(out string location)
        {
            location = null;
            // Negative age = the relay network hasn't measured anything yet (InitRelayNetworkAccess was just called).
            if (SteamNetworkingUtils.GetLocalPingLocation(out var ping) < 0f) return false;
            SteamNetworkingUtils.ConvertPingLocationToString(ref ping, out location, Constants.k_cchMaxSteamNetworkingPingLocationString);
            return !string.IsNullOrEmpty(location);
        }

        static int EstimatePing(string location)
        {
            if (string.IsNullOrEmpty(location) || !SteamNetworkingUtils.ParsePingLocationString(location, out var remote)) return -1;
            int ms = SteamNetworkingUtils.EstimatePingTimeFromLocalHost(ref remote);
            return ms >= 0 ? ms : -1;
        }

        // ------------------------------------------------------------------ mapping

        static ELobbyType ToLobbyType(LobbyVisibility visibility)
        {
            switch (visibility)
            {
                case LobbyVisibility.Public: return ELobbyType.k_ELobbyTypePublic;
                case LobbyVisibility.InviteOnly: return ELobbyType.k_ELobbyTypePrivate;
                default: return ELobbyType.k_ELobbyTypeFriendsOnly;
            }
        }

        static ELobbyDistanceFilter ToDistance(LobbySearchDistance distance)
        {
            switch (distance)
            {
                case LobbySearchDistance.Close: return ELobbyDistanceFilter.k_ELobbyDistanceFilterClose;
                case LobbySearchDistance.Far: return ELobbyDistanceFilter.k_ELobbyDistanceFilterFar;
                case LobbySearchDistance.Worldwide: return ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide;
                default: return ELobbyDistanceFilter.k_ELobbyDistanceFilterDefault;
            }
        }

        static string CreateError(EResult result)
        {
            switch (result)
            {
                case EResult.k_EResultNoConnection:
                case EResult.k_EResultTimeout:
                    return "steam.no_connection";
                case EResult.k_EResultLimitExceeded:
                case EResult.k_EResultRateLimitExceeded:
                    return "steam.too_many_lobbies";
                case EResult.k_EResultAccessDenied:
                    return "steam.not_allowed";
                default:
                    return Core.Err.Of("steam.lobby_failed", result.ToString().Replace("k_EResult", string.Empty));
            }
        }

        static string JoinError(EChatRoomEnterResponse response)
        {
            switch (response)
            {
                case EChatRoomEnterResponse.k_EChatRoomEnterResponseDoesntExist:
                    return "steam.lobby_gone";
                case EChatRoomEnterResponse.k_EChatRoomEnterResponseFull:
                    return Core.Err.Of("net.full", Networking.NetworkService.MaxPlayers);
                case EChatRoomEnterResponse.k_EChatRoomEnterResponseNotAllowed:
                case EChatRoomEnterResponse.k_EChatRoomEnterResponseBanned:
                case EChatRoomEnterResponse.k_EChatRoomEnterResponseCommunityBan:
                case EChatRoomEnterResponse.k_EChatRoomEnterResponseLimited:
                    return "steam.not_allowed";
                case EChatRoomEnterResponse.k_EChatRoomEnterResponseMemberBlockedYou:
                case EChatRoomEnterResponse.k_EChatRoomEnterResponseYouBlockedMember:
                    return "steam.blocked";
                default:
                    return Core.Err.Of("steam.lobby_failed", response.ToString().Replace("k_EChatRoomEnterResponse", string.Empty));
            }
        }
    }
}
#endif
