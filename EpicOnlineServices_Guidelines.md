# Epic Online Services and Cross-Platform Join Codes

Two additions to online play (18 November 2026):

- **Epic Games** is a third online platform, next to Default (Unity Relay and Direct IP) and Steam. It's built on Epic
  Online Services (EOS) through the **EOS Plugin for Unity**. It gives you:
  - EOS lobbies, with Find Games and Quick Match;
  - Epic friends, invites and the Epic overlay;
  - status in the Epic friends list;
  - matches over EOS P2P (Epic's NAT traversal and relays, no IP addresses exposed).
- **Every online lobby has a join code**, whether Unity Relay, Steam or Epic hosts it. Anyone can type the code to
  join, from any platform: a Steam player can join an Epic lobby, an Epic player a Steam lobby, and a build with no
  platform either of them.

Steam is described in `Streamworks_Guidelines.md`; this guide covers Epic and the codes.

---

## 1. Do this first in Unity

1. Let the scripts compile.
2. Run **Deck Party > Setup > Rebuild Scenes and UI Prefabs**.
   - The Steam / Epic card on the Multiplayer screen gets a **join-code row**.
   - The Find Games "lobby ID" box takes 64 characters (EOS lobby ids are 32).
   - Without a Rebuild, everything still works, but the platform card has no code box.
3. Multiplayer needs every player on this build (protocol `deckparty-21`). Saves are unchanged.

Nothing else is needed for the join codes. They use Unity Relay, which is already set up for the Default multiplayer.

### To use Epic

1. **Install the plugin**: Project Settings > Deck Party > Online Platform, Epic section, **Install the EOS Plugin for
   Unity**.
   - Unity downloads package `com.playeveryware.eos` 6.3.0 from GitHub.
   - **Git and Git LFS** must be installed on the PC, because the plugin's native libraries come through Git LFS.
   - You can also add it in Window > Package Manager > + > Install package from git URL:
     `https://github.com/EOS-Contrib/eos_plugin_for_unity_upm.git#v6.3.0`
2. **Set up the product** in the [Epic Developer Portal](https://dev.epicgames.com/portal):
   - a product with a sandbox and a deployment;
   - a **client** with the *GameClient* policy;
   - an **Epic Account Services** application, linked to that client, with the permissions **Basic profile**,
     **Online presence** and **Friends**. The game asks for exactly these three, so they have to match.
3. Open **EOS Plugin > EOS Configuration** (there's a button for it in the Online Platform settings).
   - Fill in Product Name, Product ID, Sandbox ID, Deployment ID, Client ID and Client Secret from the portal.
   - Set a Product Version and an Encryption Key (the Key button makes one), then **Save All Changes**.
4. **Project Settings > Deck Party > Online Platform**: set **Platform** to **Epic Games**.
5. **To sign in during Play Mode**, use Epic's **Developer Authentication Tool**. It ships with the EOS SDK tools; the
   "Developer Auth Tool" button in the settings links to its page.
   - Start it on a port (6547 is the default), sign in with an Epic account and give the login a credential name.
   - Put the credential name in **Dev Auth Credential**. Leave **Dev Auth Tool Host** as `localhost:6547` unless you
     changed the port.
   - Without it, the Multiplayer screen opens Epic's login window instead.

---

## 2. Join codes (every platform)

| Who hosts | The code | Who can join with it |
|---|---|---|
| Default (Host Online Game) | The Unity Relay code, as before | Anyone, on any build |
| Steam lobby | A Unity Relay code made with the lobby | Anyone: Steam, Epic, or no platform |
| Epic lobby | A Unity Relay code made with the lobby | Anyone: Steam, Epic, or no platform |

- **Where it shows:** on the lobby screen, with a **Copy** button. In a Steam or Epic lobby, **Invite** sits just left
  of Copy, and the hint names the lobby's platform and who can join it. Players who came in by invite see the code
  too, so anyone in the lobby can pass it on.
- **Where to type it:**
  - the Join box on the Default card;
  - the new code row on the Steam / Epic card;
  - the Find Games "lobby ID" box, which takes either.
  - A Steam or Epic lobby id pasted in any of these still works too, when that platform is running.
- **How players connect:**
  - Lobby members (friends, invites, Find Games, Quick Match) connect through their platform, as before.
  - Players who type the code connect through Unity Relay.
  - The host listens on both at once.
- **Limits:**
  - The 6-player limit counts everyone, however they came in.
  - A code player's name is the one they typed (on Steam or Epic, their own platform name). Their avatar only shows to
    players on the same platform.
- **No Relay** (Unity Gaming Services not linked, or unreachable): Steam and Epic lobbies still work, without a code.
  The lobby shows who can join instead, as before.
- **Turning it off:** Project Settings > Deck Party > Online Platform > **Cross-Platform Join Codes**.

---

## 3. What players see on Epic

- **Signing in** happens without asking whenever possible:
  1. Started from the **Epic Games Launcher** (store builds): the launcher's sign-in is used, with no window.
  2. Otherwise, the **remembered login** from a previous run.
  3. In the Editor and development builds, the **Developer Authentication Tool**.
  4. If none of those work, the first time the player opens **Multiplayer**, Epic's **login window** opens. The card
     says "Signing in to Epic Games... Join codes already work" until it's done.
  5. If they close it, the game signs in with **this device's id** (Device ID Fallback).
     - This gives lobbies, join codes and matches.
     - It gives no friends, invites or Epic status, so those buttons hide.
- **Multiplayer screen**, on the Epic card:
  - Host a lobby (Friends Only, Public or Invite Only);
  - Quick Match and Find Games;
  - join by code.
- **Lobby screen**: the join code, **Copy** and **Invite**.
  - The **Invite Friends** panel lists Epic friends and invites those who have played the game before.
  - Its **Open Epic Games** button opens the Epic overlay's friends list, which can invite anyone.
  - Friends can also join a Friends Only lobby from the overlay.
- **Names**: players are named after their Epic display name when **Use Epic Names** is on (the name box hides).
- **Not on Epic:** avatars (EOS has none) and profile pages.

---

## 4. Testing

- **Join codes, without Epic:** Multiplayer Play Mode players always use Default, so host with **Host Online Game** in
  one player and type the code in another. To try a Steam host with a code player, run a Steam build on one PC and a
  Default build (or the Editor) on another.
- **Epic in the Editor:** the Developer Authentication Tool can hold several logins.
  - Make two credential names with two different Epic accounts.
  - Run the Editor on one, and a development build with `-epicdevauth=<second name>` on the other.
  - Two copies on one PC can't share one account: EOS P2P needs two product users.
- **Epic in builds:** start a development build normally to get the login window, or from the Epic Games Launcher
  (once the game is on the store) for the launcher sign-in.
- **Mixed match:** host on Epic, copy the code, and join it from a Steam build or a Default build.
  - Platform players connect over EOS P2P.
  - Code players connect over Unity Relay.
  - They all land in one lobby.
- **Command line:**
  - Any build can be forced to a platform with `-onlineplatform epic`, `-onlineplatform steam` or
    `-onlineplatform default`.
  - `-epicdevauth=<name>` and `-epicdevauthhost=<host:port>` pick a Developer Authentication Tool login.

---

## 5. How it works (where things are)

| Piece | File | What it does |
|---|---|---|
| Platform | `Scripts/Platforms/Epic/EpicPlatform.cs` (+ `.Login`, `.Lobbies`, `.Friends`) | `IOnlinePlatform` on EOS. It creates the plugin's `EOSManager`, which starts and ticks the SDK with the EOS Configuration. |
| Sign-in | `EpicPlatform.Login.cs` | Auth (exchange code, persistent, dev tool, account portal), then Connect (Epic id token or device id). It refreshes the Connect session before it expires. |
| Lobbies | `EpicPlatform.Lobbies.cs` | EOS lobbies in bucket `deckparty:<protocol>`. Owner changes are batched (at most two updates a second). Members' names are a member attribute (`dp_name`). Invites and overlay "Join" are handled here. |
| Friends, presence, overlay | `EpicPlatform.Friends.cs` | The friends list and their presence and names; product user ids for invites; the overlay's friends list; rich text status. |
| Transport | `Scripts/Platforms/Epic/EpicP2PTransport.cs` | Netcode over EOS P2P, socket `DeckParty`. Reliable messages are split into ordered 1170-byte packets and joined again on arrival. The host only accepts lobby members. |
| Assembly | `Scripts/Platforms/Epic/DeckParty.Epic.asmdef` | Only compiles with the EOS plugin installed (version define `DECKPARTY_EOS`) on Windows, macOS or Linux. Every file is wrapped in `#if`, as with Steam. |
| Join codes | `Runtime/Networking/BridgedTransport.cs` | The Steam / Epic host's transport: the platform transport plus Unity Transport on a Relay allocation. Relay ids pass through unchanged, because Unity Transport looks them up in Netcode. Platform ids get the top bit. |
| Hosting and joining | `NetworkService.cs` | `HostPlatformAsync` opens the Relay leg and stores the code in the lobby (`dp_code`). `JoinWithCodeAsync` tells a code from a lobby id. Approval checks lobby membership only for players who came in through the platform. |
| Ids | `OnlinePlatformTypes.cs`, `NetTypes.cs` | Platform user ids are strings now (a SteamID, an EOS product user id). Players carry their platform kind and id (`NetPlayer.PlatformKind`, `PlatformUserId`), so a mixed match knows whose avatar is whose. |
| Settings | `OnlinePlatformSettings.cs` (Project Settings > Deck Party > Online Platform) | Epic options and the Cross-Platform Join Codes switch. |
| Text | strings CSV | `epic.*` errors, `platform.signing_in`, `mp.platform_note_codes`, `lobby.code_hint_platform(_host)`, `mp.code_placeholder_any`. `platform.offline` and `platform.lobby_id_placeholder` were reworded. |

---

## 6. Before releasing on the Epic Games Store

- Fill in the **Live** sandbox and deployment in EOS Configuration. The launcher passes them on the command line and the
  plugin applies them.
- Keep **Launcher Exchange Code** on: store players are signed in by the launcher, with no window.
- The **Epic overlay** on Windows needs the plugin's native render plugin, which the package sets up; check it shows in
  a release build.
- Your Epic Account Services application must be **approved** (brand review) before players outside your organization
  can sign in.
- Client credentials are meant to ship in the game (that's what the *GameClient* policy is for), but don't commit
  production secrets to a public repository.

---

## 7. Decisions I made (please check)

1. **The EOS Plugin for Unity** is the integration:
   - It is Epic's official Unity plugin, maintained by the EOS-Contrib project.
   - It handles native libraries and Editor reloads; Play Mode can start EOS again and again.
   - It is pinned to 6.3.0.
   - Product credentials stay in its own window, not in our settings.
2. **Codes ride on Unity Relay** rather than a platform's own system. That's the one service every build already
   reaches, so a code works whatever platform the joiner is on.
3. **Device ID Fallback is on**: without an Epic account the game still plays online, minus friends.
4. **Friends who never played the game** can't be invited from the in-game panel: EOS invites need a product user id,
   which they don't have yet. The overlay can still invite them.
5. **Code players aren't checked against the lobby.** Knowing the code is the permission, as with Default join codes.
   Their platform id is only used for avatars, never for their name.

---

## 8. Troubleshooting

| Problem | Likely cause |
|---|---|
| "Epic Online Services isn't set up in this build" | EOS Configuration is empty or wasn't saved, or the native library is missing. Check the Console for the plugin's own errors. |
| The login window never opens in Play Mode | Login Window is off, or the Developer Authentication Tool login failed silently. Check that the tool is running and the credential name matches. |
| Signed in, but "its online services didn't answer" | The client isn't linked to the Epic Account Services application in the portal, or the permissions don't match Basic profile + Online presence + Friends. |
| No code in a Steam / Epic lobby | Unity Relay isn't set up or reachable. The Console says "No cross-platform join code for this lobby". The same setup as Host Online Game is needed. |
| A code joiner is refused with "already connected" | The same platform account is already in the match (one account, one seat). |
| Find Games shows nothing on Epic | Only Public lobbies are listed, from the same build (the bucket includes the protocol version). |
