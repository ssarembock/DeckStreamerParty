# Deck Party: Setup, Art & Multiplayer Guide

Deck Party is built on the rules in `Party_House_UFO50_Complete_Rules.md`, re-themed for streaming and memes (see `.claude/MajorUpdate.md`): 59 guests (10 renamed classics, 40 meme guests, 9 Diamonds), 5 character sets, a Random Mix (with the 5-in-a-row Cherry goal), an Open Lobby sandbox, 7 backgrounds plus "Character Set Default" and "Evolving", single player with autosave, and online/LAN multiplayer built on **Netcode for GameObjects** with **Unity Relay**.

Everything works out of the box with generated placeholder art. This guide covers setup and multiplayer. **For adding character sprites and background images, and for testing the V1 update, see `V1Instructions.md`** (Deck Party > Open V1 Instructions).

---

## 1. First launch

1. Open the project in **Unity 6000.0.73f1**. Package Manager downloads the new packages listed in `Packages/manifest.json`:
   - `com.unity.netcode.gameobjects` 2.13.3 (networking)
   - `com.unity.transport` 2.7.4
   - `com.unity.services.multiplayer` 2.4.1 (Unity Relay, for online play)
   - `com.unity.multiplayer.playmode` 1.6.3 (test several players inside the editor)
2. When compilation finishes, a **"Welcome to Deck Party"** dialog appears. Click **Build now**. It will:
   - import the TextMeshPro Essential Resources,
   - generate placeholder sprites in `Assets/DeckParty/Art/Placeholders`,
   - create the data assets (database, venues, UI skin, audio library),
   - create the network session prefab and the UI prefabs,
   - build `Assets/DeckParty/Scenes/MainMenu.unity` and `Game.unity`,
   - add both scenes to the build list and set the player settings (product name "Deck Party", run in background, windowed 1600x900).
3. Open **MainMenu** (it opens automatically) and press **Play**.

If the dialog doesn't appear (or you clicked Later), use **Deck Party > Setup > Build Everything** from the menu bar.

> Tip: you can also press Play directly in the **Game** scene. It starts a quick single-player game of the first character set, which is handy while you tweak the HUD.

### The Deck Party menu

| Menu item | What it does |
|---|---|
| Setup > Build Everything (keeps existing scenes) | Creates anything missing. Never overwrites your scenes or prefabs. Safe to run any time. |
| Setup > Rebuild Scenes and UI Prefabs (overwrites) | Regenerates both scenes and the 5 UI prefabs from scratch, discarding your edits to them. Art, venues, skin, audio and database are kept. |
| Setup > Regenerate Placeholder Art | Rewrites the generated placeholder PNGs. |
| Art > Auto-Assign Sprites by Name | Hooks up every sprite under `Assets/DeckParty/Art` using the naming rules in `V1Instructions.md`. |
| Art > Assign Selected Sheet to Guests in Roster Order | Select a sliced character sheet, then run this. Sprites become portraits in roster order. |
| Art > Clear Guest Portraits | Reverts all guests to placeholders. |
| Art > Log Expected File Names | Prints every character and background file name the auto-assigner recognises. |
| Art > Auto-Assign When Images Are Imported | On by default: images dropped into `Art/Characters`, `Art/Backgrounds` or `Art/UI` are set up as sprites and assigned automatically. |
| Open Instructions / Open V1 Instructions | Opens this guide / the V1 art and testing guide. |
| Select Game Database / UI Skin / Audio Library | Jumps to the asset in the Project window. |
| Open Main Menu Scene / Open Game Scene | Opens the scene. |

`Assets/Scenes/SampleScene.unity` came with the template and is no longer used. You can delete it.

---

## 2. Project layout

```
Assets/DeckParty/
  Art/
    Characters/     <- put your character sprite sheet(s) here
    Backgrounds/    <- put venue background images here
    UI/             <- put your UI sprite sheet(s) here
    Placeholders/   (generated; safe to ignore or delete once replaced)
  Audio/Music, Audio/SFX   <- optional audio clips
  Data/
    UISkin.asset          <- UI sprite slots + fonts
    AudioLibrary.asset    <- music + sound effect slots
    Venues/Venue_*.asset  <- one asset per background/venue
  Resources/
    DeckPartyDatabase.asset          <- guest portraits, venue list, links everything
    Network/DeckPartyNetSession.prefab   (networking, don't rename/move)
  Prefabs/UI/   GuestCard, GuestRow, ScenarioItem, PlayerRow, TextLine
  Scenes/       MainMenu.unity, Game.unity
  Scripts/
    Core/      pure C# rules engine (no Unity dependency)
    Runtime/   Unity code: UI, sessions, networking, audio, data
    Editor/    scene/prefab builders and art tools
```

---

## 3. Importing art

Drop images into the matching `Art/` subfolder. On first import they're set up as sprites (Sprite (2D and UI), no mipmaps), and the auto-assigner hooks them up by file name. For a sprite sheet, set **Sprite Mode: Multiple**, slice it in the **Sprite Editor** and name the slices; applying re-runs the auto-assign. For pixel art, set **Filter Mode: Point** and **Compression: None** (your Inspector changes are kept). Matching ignores case, spaces, `_` and `-`.

You can always assign sprites by hand instead. Each section below says which asset field to use.

---

## 4. Characters (guest portraits)

**Full step-by-step guide and the 59-character file-name table: `V1Instructions.md`, section 2.**

**Where they go:** `Assets/DeckParty/Resources/DeckPartyDatabase.asset`, in the **Guests** list (one entry per guest, labelled by name). Drag a sprite into **Portrait**. Each entry also has:
- **Display Name Override:** rename a guest in the UI (rules unaffected).
- **Tint:** placeholder/accent colour (alpha 0 means the automatic family colour).

Portraits are shown with "preserve aspect" in a roughly square frame (about 180x140 inside each card), so square-ish character art with a transparent background looks best.

---

## 5. Backgrounds (venues)

**Full step-by-step guide and the background file-name table: `V1Instructions.md`, section 3.**

Each background is a **Venue** asset in `Assets/DeckParty/Data/Venues/` (`Venue_bedroom`, `Venue_content_house`, `Venue_cursed_stream`, `Venue_studio_stage`, `Venue_city_irl`, `Venue_metaverse`, `Venue_irl_beach`). Players pick one in the Single Player screen, in the multiplayer lobby (each player picks their own), or from the in-game **Menu** at any time.

Each venue asset has three art fields: **Background** (full-screen image, 1920x1080+ 16:9, file `bg_<id>`), optional **Foreground** overlay (`bg_<id>_fg`) and optional **Thumbnail** (`bg_<id>_thumb`). The palette and style only drive the placeholder scene drawn while Background is empty. **Party Lights** adds moving spotlights (and the red/blue flash when you get swatted); untick it for a clean image. **Music** optionally replaces the party music for that venue.

**Special choices in every venue selector:**
- **Character Set Default:** each character set's own background. Random Mix rolls one from its seed.
- **Evolving:** your background upgrades as your Sub Cap grows (bedroom, then city at 9, studio at 14, content house at 20, metaverse at 27). Edit the stages in the database's **Evolving Venue** list.

**Adding a new venue:** right-click in `Data/Venues` and choose **Create > Deck Party > Venue**. Give it a unique **Id**, assign art, then add it to the database's **Venues** list. It appears in every selector automatically.

---

## 6. UI art (UI Skin)

All menus, the HUD, cards and buttons draw from `Assets/DeckParty/Data/UISkin.asset`. Assign a sprite to a slot and every element using that slot updates, in edit mode too.

| Slot | Used for | Sprite name | Tips |
|---|---|---|---|
| Panel | Screen panels, side panels, dialogs | `ui_panel` | 9-slice |
| PanelAlt | Rows, result card, character set items | `ui_panelalt` | 9-slice |
| Button | Normal buttons | `ui_button` | 9-slice |
| ButtonPrimary | Main actions (Open the Door, Start, Buy) and gold buttons | `ui_buttonprimary` | 9-slice |
| ButtonSecondary | Secondary actions (Multiplayer, ability buttons) | `ui_buttonsecondary` | 9-slice |
| ButtonDanger | Reject / Quit / Reset | `ui_buttondanger` | 9-slice |
| CardFrame | Guest card frame | `ui_cardframe` | 9-slice |
| CardFrameStar | Frame for Diamond guests (optional) | `ui_cardframestar` | 9-slice |
| SlotEmpty | Empty Sub Cap space | `ui_slotempty` | 9-slice outline |
| Badge | Small badges (permanent +1, x2) | `ui_badge` | 9-slice |
| Circle | Placeholder portrait disc, player colour dot | `ui_circle` | |
| TopBar | HUD bar | `ui_topbar` | 9-slice |
| InputField | Text fields and selector value boxes | `ui_inputfield` | 9-slice |
| Tooltip | Tooltip and toast background | `ui_tooltip` | 9-slice |
| ScrollHandle | Scrollbar handle | `ui_scrollhandle` | 9-slice |
| Glow | Party light glow | `ui_glow` | soft radial |
| Logo | Main-menu logo (replaces the "DECK PARTY" text) | `ui_logo` | |
| IconPop / IconCash / IconDoor | HUD stat icons (Gifted Subs, Cash, Sub Cap) | `icon_pop`, `icon_cash`, `icon_door` | |
| IconTrouble / IconStar | Swatting Risk and Diamond pips, card badges | `icon_trouble`, `icon_star` | |
| IconCancel | "Cancels risk" badge (AutoMod, VIPS, Wholesome Icon, Cheems with a Doge) | `icon_cancel` | |
| IconCrown / IconCheck | Host and ready markers | `icon_crown`, `icon_check` | |

Notes:
- **9-slice:** in the Sprite Editor, drag the green border handles so the corners don't stretch. Slots marked 9-slice default to *Sliced* drawing.
- **Colours:** UI elements tint their sprite (for example, buttons are tinted pink). If your sprites are already coloured, tick **Override Color** on that slot and leave the colour **white**.
- **Fonts:** create a TextMeshPro font asset (**Window > TextMeshPro > Font Asset Creator**) from your TTF, then drop it into **Body Font** and/or **Header Font** on the skin.
- The placeholder PNGs in `Art/Placeholders` are what the slots use by default. Replace them through the skin rather than editing the PNGs.

---

## 7. Audio (optional)

`Assets/DeckParty/Data/AudioLibrary.asset` has **Menu Music**, **Party Music**, **Planning Music** and one slot per sound effect (Click, Hover, DoorKnock, GuestArrive, StarArrive = a Diamond arrives, Boot, Ability, Coin, Purchase, Expand = Sub Cap raised, PartySuccess, Raid = swatted, FireMarshal = Sub Cap exceeded, Victory, Defeat, Error, Notify, Peek). Any empty slot plays a small synthesized placeholder, so the game is never silent. A venue's own **Music** overrides Party Music.

---

## 8. Editing the scenes

You're free to rearrange, restyle and reparent anything in **MainMenu** and **Game**. Every script reference is a normal serialized field, visible in the Inspector:
- **MainMenu:** `MainMenuController` (scene root) links the six screens: Main, Single Player, Multiplayer, Lobby, Settings, How to Play.
- **Game:** `GameController` (scene root) links the HUD, party grid, party controls, planning/shop, SWAT screen, side panel (players, log, chat) and the overlays (picker, Squad, pause, game over, banner, effects).
- Lists (cards, shop rows, players, log lines) are spawned from the prefabs in `Prefabs/UI`. Edit those prefabs to restyle every card or row at once.
- Cards are designed at **200x260** and scaled to fit. Keep new card content inside the card prefab's `Visual/Anim` object.

Running **Build Everything** again never touches existing scenes. Only **Rebuild ... (overwrites)** does.

---

## 9. Playing

- **Goal:** get 4 Diamond guests into one party and end it successfully before the last night (25 by default).
- **Party:** *Open the Door* (Space) admits the next guest in line. Purple buttons on cards are once-per-party actions. Some open a choice popup first; multi-guest actions show a **Confirm** button (or press Space). *End Party* (E) banks everyone's Subs as Gifted Subs, plus Cash.
- **Swatting Risk:** 3 net Swatting Risk brings the SWAT team (no payout, and one guest gets a Temporary Ban for the next party). The HUD shows the meter, and the controls show the live SWAT and Sub Cap risk.
- **Planning:** spend Gifted Subs recruiting guests and Cash raising your Sub Cap, then start the next night (Space).
- **Squad** (R) shows all your guests and permanent changes. **Esc** cancels targeting or opens the menu. Hover anything for details.
- **F9** (editor and development builds, single player): +50 Gifted Subs and +10 $ for testing.
- Single-player seasons autosave after every action. **Continue** appears on the main menu.

The full rules and the guest glossary are also in-game under **How to Play**. The meme guests' rule interpretations are listed in `V1Instructions.md` section 5.

**Implementation decisions where the rules doc was ambiguous** (all adjustable in `Scripts/Core/GameRules.cs`):
- Guest fees are paid from your Cash bank first. Only the unpaid remainder costs 7 Gifted Subs per Cash (`FeesDrawFromBank`).
- Getting swatted on the final night ends the season without a Temporary Ban.
- Going over the Sub Cap doesn't ban anyone.

To change character sets or guest stats, edit `ScenarioCatalog.cs` / `GuestCatalog.cs`. On-screen wording is in `Runtime/Visuals/Terms.cs`.

---

## 10. Multiplayer: how it works

- 1 to 6 players. The **host** runs every player's game (server-authoritative). Clients send their actions and get their own state back, plus a live summary of everyone else in the side panel.
- Everyone gets the **same character set** and starting Squad, then builds their own party.
- It's an open-information game: the host sends every player's season to everyone. Click a player in the in-game **Players** panel to watch their party live (read-only); the strip above the board shows whose party is on screen.
- **Take Turns** (default): each night, players throw their parties one after another in lobby order while everyone else watches the active player's board. **End Turn** (after the party and shopping) passes to the next player. The season ends after the first night someone ends a 4-Diamond party, once everyone has played that night.
- **Synchronized Nights:** everyone plays the same night at once, and the next night starts when every player presses *Ready*. The first night anyone ends a 4-Diamond party, the season ends once everyone has finished that night. Ties go to the earlier win, then to more total Gifted Subs earned.
- **Race:** everyone plays at their own pace, and the first 4-Diamond party wins instantly.
- If nobody wins, the best Diamond count at a successful party wins, then total Gifted Subs earned.
- The host picks the character set, mode and season length (10 to 30 nights) in the lobby. Everyone else presses **Ready**, then the host presses **Start Season**. The host can start solo for testing.
- Chat works in the lobby and in game. Swattings, wins, Diamond recruits and joins pop up for everyone.
- After the season, the host can press **Back to Lobby** to play again with the same group.
- Everyone must run the **same build**. A version check rejects mismatches with a clear message.

There are two ways to connect, both on the Multiplayer screen:

| | Online (Unity Relay) | Local Network / Direct IP |
|---|---|---|
| Works over | The internet, no port forwarding | Same Wi-Fi/LAN, or internet with a forwarded port |
| Host shares | A 6-character **join code** | The host's IP address and port (7777) |
| Setup needed | Link the project to Unity Cloud (once, see 11d) | None for LAN |

---

## 11. Testing multiplayer

### a) Fastest: two players in the editor (Multiplayer Play Mode)
1. Open **Window > Multiplayer > Multiplayer Play Mode** (installed by this project).
2. Tick **Player 2** (and more if you like) and wait for the virtual player to boot.
3. Open the **MainMenu** scene and press **Play**. A second window appears for Player 2.
4. Main editor: **Multiplayer > Host on this Network**.
5. Player 2 window: **Multiplayer**, enter IP `127.0.0.1` and port `7777`, then **Join**. Press **Ready**.
6. Main editor: **Start Season**.

Relay works here too once 11d is done: Host Online Party in one window, paste the code in the other. Each window signs in with its own profile automatically.

### b) Two builds on one PC
1. **File > Build Profiles > Windows** (scenes are already in the list) > **Build**.
2. Run the .exe twice (it's windowed). Host on this Network in one, join `127.0.0.1` : `7777` in the other.

### c) Friends on the same Wi-Fi / LAN
1. Send your friends the build folder (zip the whole output folder).
2. Host clicks **Host on this Network**. The lobby shows the host's address, e.g. `192.168.1.20:7777` (also shown on the Multiplayer screen as *Your address*).
3. When Windows Firewall asks, **allow Deck Party on Private networks**.
4. Friends type that IP and port and click **Join**.

### d) Friends over the internet: Unity Relay (recommended)
One-time setup:
1. Sign in to Unity in the editor (account menu at the top-left).
2. **Edit > Project Settings > Services**: create or link a Unity Cloud project for Deck Party.
3. Go to https://cloud.unity.com, open that project, and enable **Relay** (Multiplayer > Relay). Relay has a free tier that's plenty for friends.
4. Check that **Authentication** is available for the project (anonymous sign-in is used automatically, so there's no login screen).
5. Make a build (11b) and share it.

To play: the host clicks **Host Online Party** and shares the join code from the lobby (there's a **Copy** button). Friends click **Multiplayer**, paste the code, then **Join**.

If the project isn't linked, Deck Party shows "Online play isn't set up yet..." instead of failing silently.

For a step-by-step guide to building, sending the game to a friend and a test session together, see section 6 of `V1Instructions.md`.

### e) Friends over the internet: Direct IP (no Unity account)
1. On the host's router, forward **UDP port 7777** to the host PC's LAN IP.
2. Friends join using the host's **public** IP (search "what is my IP") and port 7777.
3. The port can be changed on the Multiplayer screen (both sides must match).

---

## 12. Troubleshooting

| Problem | Fix |
|---|---|
| Compile errors right after opening | Let Package Manager finish (bottom-right spinner). If a package failed, open **Window > Package Manager** and install the versions listed in section 1. |
| "Missing Resources/Network/DeckPartyNetSession.prefab" | Run **Deck Party > Setup > Build Everything**. |
| TextMeshPro "Import TMP Essentials" window | Click **Import TMP Essentials**, then run Build Everything again. |
| Text shows squares | A character isn't in your font asset. Regenerate it with the needed character set, or use a Dynamic font asset. |
| Client can't connect on LAN | Same port on both sides? Firewall allowed? Try `127.0.0.1` on the same PC first. |
| "Version mismatch" | Everyone needs the exact same build. |
| "That party has already started" | Joining is only possible while the host is in the lobby. |
| A client's window seems frozen when unfocused | Builds have Run In Background enabled. In the editor, keep Multiplayer Play Mode windows open rather than minimized. |
| I broke a scene | **Deck Party > Setup > Rebuild Scenes and UI Prefabs (overwrites)**. |
| Reset my progress | **Settings > Reset Progress** (also clears the saved season). |

---

## 13. Code map (for further development)

- `Core/PartyEngine.cs`: every rule (arrivals, entrance effects and reactions, bring chains, Swatting Risk, SWAT team, Sub Cap, all 21 action types, passive scoring, shop, Sub Cap upgrades). It's deterministic: all randomness lives in `PlayerState.RngState`, and the waiting line is shuffled once per party.
- `Core/GuestCatalog.cs` / `GameRules.cs` / `ScenarioCatalog.cs`: guest stats and text, tuning numbers, character sets.
- `Runtime/Visuals/Terms.cs`: every on-screen mechanic name (Subs, Swatting Risk, Diamonds, Squad...).
- `Core/StateSerializer.cs`: compact binary state, used for save files and network snapshots.
- `Runtime/Session/`: `IGameSession`, with `LocalGameSession` (single player and autosave) and `NetworkGameSession` (multiplayer). The UI only talks to this interface.
- `Runtime/Networking/NetworkService.cs`: creates the NetworkManager at runtime, Relay and direct hosting/joining, connection approval (version, lobby full, game in progress).
- `Runtime/Networking/DeckPartyNetSession.cs`: lobby, match settings, per-player games on the host (every snapshot is broadcast to everyone), take turns / synchronized nights / race logic, rankings, chat and headlines (RPCs, NetworkVariables and a NetworkList).
- `Runtime/UI/Game/ViewBarView.cs`: the multiplayer strip above the board (whose party is on screen, whose turn it is). Built at runtime.
- `Runtime/UI/Game/GameController.cs`: the game scene: renders state, plays events (sounds, banners, confetti), runs the action flow (choice popup, target picks, Confirm) and hotkeys.
- `Editor/*SceneBuilder.cs`, `DeckPartyAssets.cs`: generate the scenes and prefabs (useful as a reference for the hierarchy).
