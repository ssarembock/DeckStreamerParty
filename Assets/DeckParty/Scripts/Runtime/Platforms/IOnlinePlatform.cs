using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace DeckParty.Platforms
{
    /// <summary>
    /// One online platform (Steam today; Epic, Game Center, Google Play... later). The game talks to multiplayer through
    /// this interface only, so a new platform is a new assembly that implements it and calls
    /// <see cref="OnlinePlatforms.Register"/>; nothing in the menus or the match code changes.
    /// <para>
    /// Netcode for GameObjects still runs every match. A platform supplies the <see cref="NetworkTransport"/> that
    /// carries its traffic (Steam: Steam Networking Sockets over Steam Datagram Relay) plus lobbies, friends, invites
    /// and presence around it. All calls are made on the main thread.
    /// </para>
    /// </summary>
    public interface IOnlinePlatform
    {
        OnlinePlatformKind Kind { get; }
        /// <summary>The platform's brand name for the menus ("Steam"); brand names aren't translated.</summary>
        string DisplayName { get; }
        PlatformFeatures Features { get; }
        /// <summary>The SDK started and the player is signed in.</summary>
        bool IsReady { get; }
        PlatformUser LocalUser { get; }

        /// <summary>Starts the platform's SDK. On failure returns false and a localization key saying why.</summary>
        bool Initialize(OnlinePlatformSettings settings, out string error);

        /// <summary>
        /// False when a failed <see cref="Initialize"/> is worth trying again later (e.g. Steam wasn't running yet);
        /// true when it never will work in this run (wrong platform, missing library).
        /// </summary>
        bool InitFailureIsFinal { get; }

        /// <summary>Closes the SDK (the game is quitting, or Play Mode stopped).</summary>
        void Shutdown();

        // ------------------------------------------------------------------ Netcode

        /// <summary>Adds this platform's Netcode transport to the NetworkManager's object (called once, then reused).</summary>
        NetworkTransport CreateTransport(GameObject networkManagerObject);

        /// <summary>Gets the transport ready to host: only members of the current lobby may connect.</summary>
        void PrepareHostTransport(NetworkTransport transport);

        /// <summary>Points the transport at the lobby host before Netcode starts the client.</summary>
        void PrepareClientTransport(NetworkTransport transport, ulong hostUserId);

        // ------------------------------------------------------------------ lobbies

        /// <summary>The lobby you're in (null when none).</summary>
        string CurrentLobbyId { get; }

        Task<PlatformResult<PlatformLobby>> CreateLobbyAsync(LobbyVisibility visibility, int maxMembers, IReadOnlyDictionary<string, string> data);
        Task<PlatformResult<PlatformLobby>> JoinLobbyAsync(string lobbyId);
        Task<PlatformResult<List<LobbyListing>>> FindLobbiesAsync(LobbySearch search);
        void LeaveLobby();

        /// <summary>Lobby owner only: sets a metadata value searches and joiners can read (unchanged values aren't re-sent).</summary>
        void SetLobbyData(string key, string value);
        string GetLobbyData(string key);
        /// <summary>Lobby owner only: whether new players may join (off once the match starts).</summary>
        void SetLobbyJoinable(bool joinable);
        bool IsLobbyMember(ulong userId);

        // ------------------------------------------------------------------ friends, invites, profiles

        /// <summary>The friends list (online and offline), freshest first; the panel sorts and filters it.</summary>
        List<PlatformFriend> GetFriends();
        /// <summary>Raised when friends come online, change status or change name.</summary>
        event Action FriendsChanged;

        bool InviteToLobby(ulong friendId);
        /// <summary>Opens the platform's own invite dialog. False when it isn't available (e.g. the Steam overlay is off).</summary>
        bool OpenInviteOverlay();
        bool OpenProfile(ulong userId);

        /// <summary>A user's avatar, or null while it downloads (then <see cref="AvatarLoaded"/> fires).</summary>
        Texture2D GetAvatar(ulong userId);
        event Action<ulong> AvatarLoaded;
        /// <summary>A user's current display name ("" if unknown).</summary>
        string GetUserName(ulong userId);

        /// <summary>The player accepted an invite or clicked Join Game in the platform's friends list (the lobby id).</summary>
        event Action<string> JoinRequested;
        /// <summary>A lobby to join from the launch command line (the game was started by accepting an invite), once.</summary>
        string TakeLaunchJoinRequest();

        // ------------------------------------------------------------------ presence

        /// <summary>Updates the status friends see. <paramref name="lobbyId"/> enables Join Game while the lobby is open.</summary>
        void SetPresence(PresenceState state, string lobbyId, int members, int maxMembers, bool joinable);
    }
}
