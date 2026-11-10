# Streamworks Guidelines: online platforms and Steam

Audience Capture's multiplayer can now run on more than one framework, chosen per project. Two are available:

- **Default**: the original setup. Unity Relay join codes plus Direct IP / LAN. Nothing about it changed.
- **Steam**: built with Steamworks (Steamworks.NET). It adds Steam lobbies, inviting Steam friends, Join Game from the
  Steam friends list, Find Games (a lobby browser), Quick Match, Steam names and avatars, and status in the friends list.
  Matches run peer to peer over Steam's relay network.

Epic, iOS (Game Center), Android (Google Play Games), Xbox, PlayStation and Switch have reserved slots, but nothing
behind them yet. If one of them is selected, the game says so and uses Default.

Open this guide from **Deck Party > Open Steamworks Guidelines (online platforms)**.

---

## 1. What to do after pulling this change

1. **Open the project.** Unity downloads Steamworks.NET 2025.165.0 from GitHub; it's already listed in
   `Packages/manifest.json`. **Git must be installed on the PC.** If the download fails, see Troubleshooting.
   On first load, Steamworks.NET also:
   - writes `steam_appid.txt` (containing 480) into the project folder;
   - adds the `STEAMWORKS_NET` define to the Standalone player settings.

   Both are expected.
2. **Run Deck Party > Setup > Rebuild Scenes and UI Prefabs.** This builds the new Steam card, the Find Games panel,
   the Invite Friends panel and the avatar slot on player rows. It also re-imports the strings.
3. **Open Deck Party > Online Platform...** (Project Settings > Deck Party > Online Platform) and set **Platform** to
   **Steam**. The page shows what's missing, if anything.
4. **Start the Steam client and sign in**, then press Play. The Console shows
   `[DeckParty] Steam is running multiplayer (signed in as ...)`.

The settings live in `Assets/DeckParty/Resources/OnlinePlatformSettings.asset`. Unity creates this asset the first
time it loads the project; commit it so both PCs share the choice.

With the platform left on **Default**, the game looks and plays exactly as before.

---

## 2. Choosing the platform

Everything is on **Project Settings > Deck Party > Online Platform**.

| Setting | What it does |
|---|---|
| **Platform** | The framework builds of this project use. *Default* = Unity Relay join codes and Direct IP. |
| **Fall Back to Default** | On (recommended): if the platform can't start (for example Steam isn't running), online play uses join codes and the Multiplayer screen says why. Off: online play is turned off until the platform starts. |
| **Editor Uses Default** | Play Mode in the Editor ignores the platform. Turn this on to test several players on one PC with Multiplayer Play Mode. |
| **Keep Direct IP / LAN** | Keeps the Direct IP card next to the Steam card. |
| **Steam > App ID** | Your Steam App ID. **480** is Valve's public test app (Spacewar), which every Steam account can use. Changing it rewrites `steam_appid.txt`. |
| **Steam > Relaunch Through Steam** | Release builds started outside Steam (for example by double-clicking the .exe) relaunch through Steam, so the player is signed in and owns the game. Skipped in the Editor, in development builds, and with App ID 480. |
| **Steam > steam_appid.txt in Dev Builds** | Development builds get a `steam_appid.txt` next to the .exe, so they start Steam without the Steam launcher. Release builds never get one; the build step removes a leftover copy. |
| **Steam > Default Lobby Visibility** | What the Host button starts on: Friends only / Public / Invite only. Players can change it, and the game remembers each player's last choice. |
| **Steam > Search Distance** | How far Find Games and Quick Match look: Close, Default (your region and nearby), Far, Worldwide. |
| **Steam > Use Steam Names** | Players are named after their Steam name, and the name box is hidden. |
| **Steam > Rich Presence Tokens** | Uses localized status tokens in the friends list. Leave this off until you've uploaded the tokens file (section 7). |

**Automatic exceptions:**

- **Multiplayer Play Mode virtual players always use Default.** One PC can only run one Steam account, so a second
  player on the same PC can't be a different Steam user.
- **Steam only exists in Windows, macOS and Linux builds.** If the build target is iOS, Android or anything else, the
  Steam code isn't compiled and those builds use Default.
- **Any build can be switched on the command line**, for example
  `AudienceCapture.exe -onlineplatform default` or `-onlineplatform steam`.

---

## 3. What players see (Steam)

### Multiplayer screen

The **Play on Steam** card replaces the join-code card. The note says which Steam account is signed in. It offers:

- **Who can join**:
  - *Friends only*: friends of anyone in the lobby can join, from an invite or from the friends list.
  - *Public*: also shows up in Find Games and Quick Match.
  - *Invite only*: only players you invite.
- **Host a Lobby**: opens a Steam lobby and hosts the match in it.
- **Quick Match**: joins the best public game that hasn't started yet. Games under 120 ms ping come first, then the
  fullest, so games fill up and start. If there's no game to join, it opens a public lobby for others to find.
- **Find Games**: a list of public games, each showing:
  - the host;
  - the character set and mode;
  - the players (x/6);
  - an estimated ping.

  Join any of them. The box at the bottom joins a game by **lobby ID**.

The Direct IP / LAN card is still there.

If Steam was selected but isn't running, the join-code card shows the reason in orange, for example
"Steam isn't running...". Steam is tried again each time the Multiplayer screen opens, so starting Steam doesn't need
a game restart.

### Lobby screen

- Instead of a join code, the top-right panel shows who can join, plus an **Invite** button.
- **Invite** opens the **Invite Friends** panel:
  - your online friends, with friends already playing this game first;
  - a search box;
  - an **Invite** button per friend, which changes to *Invited* or *In lobby*;
  - **Open Steam Overlay**, for Steam's own invite window.
- **Player rows** show each player's Steam avatar. Clicking a row opens that player's Steam profile: in the overlay
  when it's available, otherwise in the browser.

### Invites and Join Game

- **Accepting an invite, or clicking Join Game on a friend in the Steam friends list:**
  - in the menus, the game joins the lobby right away;
  - during a match or a single-player stream, it first asks *"Join your friend?"*, then leaves and joins.
- **Starting the game from an invite** (Steam passes `+connect_lobby <id>`) goes straight into the friend's lobby.
- **Status in the friends list**: friends see
  - "In the menus";
  - "Streaming solo";
  - "In a lobby (2/6)";
  - "In a match (3 players)".

  Party members are grouped together. Join Game works while the lobby has free seats and isn't invite only.

---

## 4. Testing Steam

Steam allows **one Steam account per PC**. To test invites you need **both PCs, each signed in to a different Steam
account, and the two accounts must be Steam friends.**

1. On both PCs: Steam running and signed in, platform set to Steam, *Editor Uses Default* off. Press Play on both.
2. On PC A: **Multiplayer > pick a streamer > Host a Lobby** (Friends only). In the lobby, click **Invite** and pick
   PC B's account.
3. On PC B, **keep the game running in Play Mode** and accept the invite from the Steam notification or chat.
   PC B joins PC A's lobby.
   - **This matters while the App ID is 480.** If the game isn't running when the invite is accepted, Steam launches
     Valve's *Spacewar*, not this game. With your own App ID, Steam starts this game instead.
4. Try **Join Game** on PC A in PC B's friends list, **Find Games** with a *Public* lobby, **Quick Match** on both PCs
   at once, and a **lobby ID** pasted into Find Games.
5. Start a match. Then leave as the host and as a client, and check the other side gets the right message.

### Other ways to test

- **Multiplayer Play Mode** (one PC, several players): always uses Default. Use it for gameplay, not for Steam.
- **Development builds**: they get a `steam_appid.txt` and start without the Steam launcher. Run one on each PC, or
  one build plus the Editor on the other PC.
- **Compare with Default**: run the same build with `-onlineplatform default`.

**Lobbies from other developers:** many developers test with App ID 480, so Spacewar has lots of public lobbies from
other games. Find Games and Quick Match only show lobbies with this game's protocol version (`NetworkService.ProtocolVersion`), so you
won't see theirs.

---

## 5. How it works

### The pieces

| Where | What |
|---|---|
| `Scripts/Runtime/Platforms/OnlinePlatformTypes.cs` | The shared vocabulary: platform kinds, lobby visibility, features, lobby listings, friends, results. |
| `Scripts/Runtime/Platforms/IOnlinePlatform.cs` | **The contract every platform implements**: start-up, the Netcode transport, lobbies, friends, invites, avatars, presence. The rest of the game only talks to this. |
| `Scripts/Runtime/Platforms/OnlinePlatformSettings.cs` | The settings asset (section 2), including placeholder settings for Epic, Game Center and Google Play. |
| `Scripts/Runtime/Platforms/OnlinePlatforms.cs` | Picks and starts the platform **before the first scene loads**. Holds the registry of platforms present in the build, the "why it isn't running" message, retries, invites waiting to be joined, and the presence helper. |
| `Scripts/Runtime/Platforms/PlatformJoinRouter.cs` | Invites that arrive during a match or a stream: ask, leave, hand over to the menu. |
| `Scripts/Platforms/Steam/` (**DeckParty.Steam** assembly) | The Steam implementation (next table). |
| `Scripts/Editor/Platforms/` | The Project Settings page, the settings asset, `steam_appid.txt`, the Steamworks.NET install button, and the build checks. |

**The Steam code only exists when Steamworks.NET is installed and the build target is Windows, macOS or Linux.**
Every Steam file is wrapped in `#if DECKPARTY_STEAMWORKS && UNITY_STANDALONE`. `DECKPARTY_STEAMWORKS` is a version
define in `DeckParty.Steam.asmdef` that turns on when the Steamworks.NET package is present. Without it, or on any
other target, the assembly compiles empty, the project still builds, and multiplayer is Default.

*Don't move this into the asmdef's Define Constraints.* Unity doesn't count an asmdef's own version defines there,
so it excludes the assembly, and the files then fall into `Assembly-CSharp` instead of being skipped.

The Steam assembly registers itself from `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`, so the main game
code never references Steamworks. That's the same pattern the Feel (Juice) assembly uses.

| Steam file | What |
|---|---|
| `SteamPlatform.cs` | **Startup and shutdown:** `SteamAPI.InitEx`, `RestartAppIfNecessary` (release builds only), checks for offline mode and an outdated client, `SteamAPI.Shutdown` when quitting or leaving Play Mode. **Callbacks:** invites (`GameLobbyJoinRequested_t`, `GameRichPresenceJoinRequested_t`, `+connect_lobby`). Also rich presence, and hands the transport to Netcode. |
| `SteamPlatform.Lobbies.cs` | Creating, joining, searching and leaving lobbies. Lobby data (only changed values are sent). Ping estimates from Steam's relay network. Friendly error messages. |
| `SteamPlatform.Friends.cs` | The friends list and statuses, names, avatars (downloaded on demand, then cached), invites, the overlay, profiles. |
| `SteamNetworkingTransport.cs` | **The Netcode transport** over Steam Networking Sockets (below). |
| `SteamCallbackPump.cs` | Runs `SteamAPI.RunCallbacks()` every frame; Steam only delivers results from inside it. |
| `SteamCall.cs` | Turns Steam's async calls into `await`able tasks, with a 20 s timeout. |
| `link.xml` | Stops IL2CPP code stripping from removing Steamworks.NET's callback types. |

### Hosting

1. **Host a Lobby** calls `NetworkService.HostPlatformAsync`.
2. Steam creates the lobby (`CreateLobby`) with the chosen visibility and 6 seats.
3. The host sets the lobby data:
   - `dp_ver`, the protocol version;
   - `dp_phase` = `lobby`;
   - `dp_host`, the host's name;
   - `dp_hostid`, the host's SteamID;
   - `dp_set` and `dp_mode`, the character set and mode;
   - `dp_qm`, set for Quick Match lobbies;
   - `dp_ping`, the host's ping location.
4. Netcode starts the host on `SteamNetworkingTransport`, which opens a P2P listen socket.
5. While the session runs, `NetworkService` keeps the lobby in step:
   - when settings change, it updates the character set and mode;
   - when the match starts, it sets `dp_phase` to `playing` and stops new players joining;
   - when the lobby is full, it also stops new players joining;
   - it keeps the friends-list status current.
6. When the host leaves, the lobby is closed to new players first, because Steam hands a lobby to another member when
   its owner leaves.

### Joining (an invite, Join Game, Find Games, Quick Match or a lobby ID)

1. `NetworkService.JoinPlatformLobbyAsync` joins the Steam lobby (`JoinLobby`).
2. It checks `dp_ver`. A different version shows "Version mismatch"; a started match shows "That game has already
   started".
3. It reads the host's SteamID and starts the Netcode client, which connects peer to peer to that SteamID
   (`ConnectP2P`).
4. **The host only accepts connections from players who are in its Steam lobby.** A player joining the lobby and
   their connection arriving race each other, so the transport waits up to 6 s for them to appear before refusing.
   This is what keeps an *Invite only* lobby invite-only.
5. Netcode's approval step checks:
   - the protocol version;
   - that the reported SteamID is a lobby member;
   - that the same account isn't joining twice.

   The player's name comes from Steam.

### The transport (`SteamNetworkingTransport`)

- **Routing and security.** Traffic is peer to peer by SteamID, through Steam Datagram Relay:
  - NAT punch-through and relaying are automatic;
  - no IP address is exposed;
  - traffic is encrypted;
  - Steam authenticates every connection;
  - there are no servers to run and no relay costs.
- **Delivery.** Netcode's reliable deliveries use Steam's reliable messages, which are ordered and up to 512 KB, so
  fragmentation is built in. Unreliable deliveries use unreliable messages. Nagle is off, because Netcode already
  sends one batch per tick.
- **Polling.** Steam's callbacks are run at the start of Netcode's EarlyUpdate. That way, "player connected" always
  arrives before that player's first message.
- **Disconnects.** Players are disconnected with *linger*, so Netcode's "why you were disconnected" message (for
  example "The host closed the lobby") gets through before the connection closes.

### Data and version changes

- **`NetPlayer.PlatformUserId`**: each player's SteamID, used for avatars and profiles. It's 0 for Default sessions.
- **Network protocol bumped to `deckparty-13`.** The approval message now has a fourth part, the platform user id.
  Builds from before this change can't play with builds from after it. The saved game format (8) is unchanged.
- **New strings**: about 70 rows in all 5 languages (`platform.*`, `steam.*`, `mp.platform_*`, ...) in
  `AudienceCapture_Strings.csv`. Brand names (Steam) aren't translated.
- **`steam_appid.txt` is in `.gitignore`.** Each PC regenerates it from the App ID setting.

---

## 6. Before releasing on Steam

1. **Get your App ID** from the Steamworks partner site and enter it under *Steam > App ID*. `steam_appid.txt` updates
   automatically.
2. **Leave *Relaunch Through Steam* on.** Release builds started outside Steam then relaunch through Steam.
3. **Make release builds** (not Development). The build step removes any `steam_appid.txt` from the build folder:
   shipping one would let the game skip Steam's ownership check. A release build with App ID 480 logs a warning.
4. **Bump `NetworkService.ProtocolVersion`** with every update that changes the network data, as before. Steam lobby
   searches use it, so players on different versions never see each other's games.
5. **Optional: rich presence tokens.** See section 7.
6. **Nothing to set up for lobbies or networking.** Steam lobbies, Steam Datagram Relay P2P and invites work for any
   app with no extra Steamworks settings.
7. **IL2CPP builds** are covered: `link.xml` keeps Steamworks.NET's types through code stripping.

If Steamworks.NET is missing, **a build with Steam selected stops with an error**, instead of quietly shipping without
Steam.

---

## 7. Rich presence tokens (optional)

The "status" text friends see works without any setup. For localized statuses in Steam's own friends list:

1. On the Steamworks partner site, open your app's **Edit Steamworks Settings > Community > Rich Presence**. Upload a
   localization file like the one below. Add a section per language (`schinese`, `russian`, `latam`, `brazilian`).
2. Turn on **Rich Presence Tokens** in the settings.

```
"lang"
{
	"english"
	{
		"tokens"
		{
			"#Status_Menu"          "In the menus"
			"#Status_SinglePlayer"  "Streaming solo"
			"#Status_Lobby"         "In a lobby (%players%/%maxplayers%)"
			"#Status_Match"         "In a match (%players% players)"
		}
	}
}
```

---

## 8. Adding another platform (Epic, iOS, Android, consoles)

The slots are already in place: the `OnlinePlatformKind` enum, the platform popup, and placeholder settings for Epic,
Game Center and Google Play Games. To build one:

1. **Create `Scripts/Platforms/<Name>/`** with its own assembly definition. It should reference `DeckParty.Core`,
   `DeckParty.Runtime`, `Unity.Netcode.Runtime` and the platform's SDK. Use define constraints so it only compiles
   where the SDK exists; copy `DeckParty.Steam.asmdef`.
2. **Implement `IOnlinePlatform`.**
   - **Lobbies**: EOS Lobbies, Game Center matchmaking, or Play Games.
   - **Friends, invites, presence, avatars**: the platform's equivalents.
   - **A Netcode transport**: for example EOS P2P. Or reuse Unity Relay: create a `UnityTransport` in
     `CreateTransport` and put the Relay join code in the lobby data.
   - Set `Features` to only what it supports; the menus hide the rest.
3. **Register it** from a `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` method:
   `OnlinePlatforms.Register(OnlinePlatformKind.Epic, () => new EpicPlatform());`
4. **Editor side:**
   - fill in its settings class in `OnlinePlatformSettings.cs`;
   - show its status in `OnlinePlatformSettingsEditor.cs`;
   - add build checks to `OnlinePlatformBuildProcessor.cs`.
5. **Add any new text** as CSV rows in all 5 languages.

`NetworkService`, the Multiplayer and Lobby screens, invites and presence then work unchanged.

---

## 9. Troubleshooting

| What you see | Why / what to do |
|---|---|
| The Multiplayer screen shows join codes, not the Steam card | The orange note says why. Check that Steam is running and signed in, the platform is Steam, and *Editor Uses Default* is off. MPPM virtual players always use Default. The build target must be Windows, macOS or Linux. |
| "Steam isn't running..." | Start Steam and sign in, then reopen the Multiplayer screen; it tries again. |
| "Steam is in offline mode..." | Switch Steam to online (Steam > Go Online). |
| Package Manager: "No 'git' executable was found" | Install Git for Windows, restart Unity. Or use the **Install Steamworks.NET** button on the settings page. |
| The settings page says DeckParty.Steam hasn't compiled | Wait for compiling to finish. If it stays, the Console has the error. Check the build target is Standalone. |
| Accepting an invite launched *Spacewar* | That happens with App ID 480 when the game isn't running. Keep it running (Play Mode) before accepting. |
| The Steam overlay doesn't open in the Editor | Normal: the overlay needs the game started by Steam. Use the Invite list in the lobby, which works everywhere. |
| A friend can't find my game in Find Games | Only *Public* lobbies are listed. *Friends only* games are joined through invites or Join Game. Check *Search Distance* too. |
| "Join the lobby first..." | The host refused a connection that wasn't in its Steam lobby within 6 s. Rejoin from the invite or Find Games. |
| "Version mismatch" | Both players need builds with the same `ProtocolVersion`. |
| Two copies of the game on one PC can't play over Steam | One Steam account per PC. Use Default (MPPM, or `-onlineplatform default`) or a second PC. |
