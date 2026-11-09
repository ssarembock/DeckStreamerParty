using System;
using System.Collections.Generic;

namespace DeckParty.Platforms
{
    /// <summary>
    /// Which online service runs multiplayer (picked in Project Settings > Deck Party > Online Platform).
    /// <see cref="Default"/> is the original setup: Unity Relay join codes plus Direct IP / LAN.
    /// Only Steam is implemented; the others are reserved so a store build can be added later without moving anything.
    /// Stored in <see cref="OnlinePlatformSettings"/>: only append, never renumber.
    /// </summary>
    public enum OnlinePlatformKind
    {
        /// <summary>Unity Relay (join codes) and Direct IP. Needs no store account.</summary>
        Default = 0,
        /// <summary>Steamworks: Steam friends, invites, the Steam overlay, lobby search and quick match, Steam Datagram Relay.</summary>
        Steam = 1,
        /// <summary>Placeholder: Epic Online Services (EOS lobbies, friends, P2P).</summary>
        Epic = 2,
        /// <summary>Placeholder: iOS Game Center.</summary>
        AppleGameCenter = 3,
        /// <summary>Placeholder: Android Google Play Games.</summary>
        GooglePlayGames = 4,
        /// <summary>Placeholder: Xbox / Microsoft GDK.</summary>
        Xbox = 5,
        /// <summary>Placeholder: PlayStation.</summary>
        PlayStation = 6,
        /// <summary>Placeholder: Nintendo Switch.</summary>
        NintendoSwitch = 7,
    }

    /// <summary>Who can find and join a platform lobby.</summary>
    public enum LobbyVisibility
    {
        /// <summary>Friends of anyone in the lobby can join (from the friends list or an invite).</summary>
        FriendsOnly = 0,
        /// <summary>Anyone: shows up in Find Games and Quick Match.</summary>
        Public = 1,
        /// <summary>Only players you invite.</summary>
        InviteOnly = 2,
    }

    /// <summary>How far lobby searches reach (Steam's distance filter; other platforms map it as best they can).</summary>
    public enum LobbySearchDistance
    {
        /// <summary>The same region only (lowest ping, fewest games).</summary>
        Close = 0,
        /// <summary>The same and nearby regions (the platform default).</summary>
        Default = 1,
        /// <summary>About half the world away.</summary>
        Far = 2,
        /// <summary>Everywhere (expect high ping).</summary>
        Worldwide = 3,
    }

    /// <summary>What a platform can do; the menus only show the buttons a platform supports.</summary>
    [Flags]
    public enum PlatformFeatures
    {
        None = 0,
        /// <summary>Host and join lobbies.</summary>
        Lobbies = 1 << 0,
        /// <summary>Find Games: a list of public lobbies.</summary>
        LobbySearch = 1 << 1,
        /// <summary>Quick Match: join a public game or open one.</summary>
        QuickMatch = 1 << 2,
        /// <summary>The player's friends list (in-game Invite Friends panel).</summary>
        Friends = 1 << 3,
        /// <summary>The platform's own invite dialog (the Steam overlay).</summary>
        InviteOverlay = 1 << 4,
        /// <summary>Player avatars.</summary>
        Avatars = 1 << 5,
        /// <summary>Status in the platform's friends list, with Join Game.</summary>
        RichPresence = 1 << 6,
        /// <summary>Opening a player's profile.</summary>
        Profiles = 1 << 7,
    }

    /// <summary>What the player is doing, for the platform's friends list.</summary>
    public enum PresenceState
    {
        Menu,
        SinglePlayer,
        InLobby,
        InMatch,
    }

    /// <summary>A friend's status as the Invite Friends panel shows it.</summary>
    public enum PlatformFriendStatus
    {
        Offline,
        Online,
        Away,
        Busy,
        /// <summary>Playing this game right now.</summary>
        InThisGame,
    }

    /// <summary>A signed-in platform user.</summary>
    public readonly struct PlatformUser
    {
        public readonly ulong Id;
        public readonly string Name;

        public PlatformUser(ulong id, string name)
        {
            Id = id;
            Name = name ?? string.Empty;
        }

        public bool IsValid => Id != 0;
    }

    /// <summary>A friend in the Invite Friends panel.</summary>
    public sealed class PlatformFriend
    {
        public ulong Id;
        public string Name;
        public PlatformFriendStatus Status;
        /// <summary>Already in your lobby.</summary>
        public bool InYourLobby;
    }

    /// <summary>One lobby in Find Games.</summary>
    public sealed class LobbyListing
    {
        /// <summary>The platform's lobby id (a Steam lobby id as a number string).</summary>
        public string Id;
        public string HostName;
        public int Members;
        public int MaxMembers;
        /// <summary>Estimated ping to the host in milliseconds (-1 when unknown).</summary>
        public int PingMs = -1;
        /// <summary>The lobby's metadata (what the host set with <see cref="IOnlinePlatform.SetLobbyData"/>).</summary>
        public readonly Dictionary<string, string> Data = new Dictionary<string, string>();

        public string Get(string key) => Data.TryGetValue(key, out var value) ? value : string.Empty;
    }

    /// <summary>A lobby search: only lobbies whose data matches every filter, with a free seat.</summary>
    public sealed class LobbySearch
    {
        /// <summary>Exact matches on lobby data (the protocol version, the lobby phase...).</summary>
        public readonly Dictionary<string, string> Matches = new Dictionary<string, string>();
        public LobbySearchDistance Distance = LobbySearchDistance.Default;
        public int MaxResults = 50;
        public int FreeSlots = 1;
    }

    /// <summary>The lobby you just created or entered.</summary>
    public sealed class PlatformLobby
    {
        public string Id;
        /// <summary>The platform user id of the player hosting the match (connect the transport to them).</summary>
        public ulong HostUserId;
        public int MaxMembers;
        public LobbyVisibility Visibility;
        public readonly Dictionary<string, string> Data = new Dictionary<string, string>();

        public string Get(string key) => Data.TryGetValue(key, out var value) ? value : string.Empty;
    }

    /// <summary>The outcome of a platform call: a value, or a localization key (with Err arguments) saying what went wrong.</summary>
    public readonly struct PlatformResult<T>
    {
        public readonly bool Ok;
        public readonly T Value;
        /// <summary>A localization key (Core.Err format: "key|arg"); show it with Loc.Error.</summary>
        public readonly string Error;

        PlatformResult(bool ok, T value, string error)
        {
            Ok = ok;
            Value = value;
            Error = error;
        }

        public static PlatformResult<T> Success(T value) => new PlatformResult<T>(true, value, null);
        public static PlatformResult<T> Fail(string error) => new PlatformResult<T>(false, default, error);
    }
}
