# Deck Party (Audience Capture): Setup, Art & Multiplayer Guide

Deck Party is built on the rules in `Party_House_UFO50_Complete_Rules.md`, re-themed for streaming and memes. Players see it as **Audience Capture**; the project, repo and product name stay "Deck Party". It has:

- **Viewers:** 59 (10 renamed classics, 40 meme viewers, 9 Diamonds).
- **Streamers:** 11 playable streamers with powers, plus Random and No Streamer.
- **Character sets:** 5, plus a Random Mix (with the 5-in-a-row Cherry goal) and an Open Lobby sandbox.
- **Stream setups:** 7, plus one stage per streamer.
- **Languages:** 5 (English, Simplified Chinese, Russian, Latin-American Spanish, Brazilian Portuguese).
- **Modes:** single player with autosave, and online/LAN multiplayer built on **Netcode for GameObjects** with **Unity Relay**.

Everything works out of the box with generated placeholder art.

| Guide | Covers | Menu |
|---|---|---|
| This file | Setup, project layout, multiplayer, code map | Deck Party > Open Instructions |
| `V1Instructions.md` | Adding character sprites and backgrounds; building and sharing a copy; testing online with a friend | Deck Party > Open V1 Instructions |
| `V2Instructions.md` | The Audience Capture update: getting it into Unity, streamers, languages, animation, test passes | Deck Party > Open V2 Instructions |
| `UIGuide.md` | "To change X, open Y": menus, HUD, cards, backgrounds, animation, text, Feel, shaders, display | Deck Party > Open UI Guide |

---

## 1. First launch

1. Open the project in **Unity 6000.0.73f1**. Package Manager downloads the packages listed in `Packages/manifest.json`:
   - `com.unity.netcode.gameobjects` 2.13.3 (networking)
   - `com.unity.transport` 2.7.4
   - `com.unity.services.multiplayer` 2.4.1 (Unity Relay, for online play)
   - `com.unity.multiplayer.playmode` 1.6.3 (test several players inside the editor)
   - `com.unity.localization` 1.5.8 (languages; it brings Addressables)

   Two Asset Store packages live in `Assets/`: **Feel** (game-feel feedbacks) and **Sprite Shaders Ultimate** (card shader effects). The game still runs without them, with simpler effects.
2. When compilation finishes, a **"Welcome to Deck Party"** dialog appears. Click **Build now**. It will:
   - import the TextMeshPro Essential Resources,
   - generate placeholder sprites in `Assets/DeckParty/Art/Placeholders`,
   - create the data assets (database, stream setups, UI skin, audio library, FX library),
   - set up the languages (locales, string tables from the CSV, Addressables) and the fonts,
   - set up character animation (templates, portrait stage, the **CardPortraits** layer),
   - generate the Feel feedback prefabs and the shader materials,
   - create the network session prefab and the UI prefabs,
   - build `Assets/DeckParty/Scenes/MainMenu.unity` and `Game.unity`,
   - add both scenes to the build list and set the player settings (product name "Deck Party", run in background, windowed 1600x900).
3. Open **MainMenu** (it opens automatically) and press **Play**.

If the dialog doesn't appear (or you clicked Later), use **Deck Party > Setup > Build Everything** from the menu bar.

> Tip: you can also press Play directly in the **Game** scene. It starts a quick single-player game of the first character set, which is handy while you tweak the HUD.

### The Deck Party menu

| Menu item | What it does |
|---|---|
| Setup > Build Everything (keeps existing scenes) | Creates anything missing and re-imports the strings CSV. Never overwrites your scenes or prefabs. Safe to run any time. |
| Setup > Rebuild Scenes and UI Prefabs (overwrites) | Regenerates both scenes and the 5 UI prefabs from scratch, discarding your edits to them. The old prefabs are backed up to `Prefabs/Backup`. Art, animations, stream setups, skin, audio, FX and the database are kept. |
| Setup > Regenerate Placeholder Art | Rewrites the generated placeholder PNGs. |
| Art > Auto-Assign Sprites by Name | Hooks up every sprite under `Assets/DeckParty/Art` (viewers, streamers, backgrounds, UI) using the naming rules in `V1Instructions.md` and `V2Instructions.md`. |
| Art > Assign Selected Sheet to Guests in Roster Order | Select a sliced character sheet, then run this. Sprites become viewer portraits in roster order. |
| Art > Clear Guest Portraits | Reverts all viewers to placeholders. |
| Art > Log Expected File Names | Prints every viewer, streamer and background file name the auto-assigner recognises. |
| Art > Auto-Assign When Images Are Imported | On by default: images dropped into `Art/Characters`, `Art/Streamers`, `Art/Backgrounds` or `Art/UI` are set up as sprites and assigned automatically. |
| Art > Create Character / Streamer Animator Template | Selects (creating if needed) the template controllers with every state name the game plays. |
| Art > Validate Character Animations | Reports missing animation states and broken clip paths. |
| Localization > Import CSV into String Tables | Loads `Resources/Localization/AudienceCapture_Strings.csv` into the Unity string tables. |
| Localization > Export String Tables to CSV | Writes edits made in Unity's Localization Tables window back to the CSV. |
| Localization > Select Strings CSV / Rebuild Fonts | Jumps to the CSV / regenerates the TextMeshPro fonts and their fallbacks. |
| FX > Select FX Library / Rebuild FX (overwrites) | Jumps to the polish settings / regenerates the Feel prefabs and shader materials. |
| Open Instructions / V1 / V2 Instructions / UI Guide | Opens the guides. |
| Select Game Database / UI Skin / Audio Library | Jumps to the asset in the Project window. |
| Open Main Menu Scene / Open Game Scene | Opens the scene. |

`Assets/Scenes/SampleScene.unity` came with the template and is no longer used. You can delete it.

---

## 2. Project layout

```
Assets/DeckParty/
  Animation/      animator templates, per-viewer override controllers, PortraitUnlit material
  Art/
    Characters/   <- viewer sprites (and streamer sprites named streamer_<name>)
    Streamers/    <- streamer sprites (optional separate folder)
    Backgrounds/  <- stream setup images (bg_<id>, bg_stage_<streamer>)
    UI/           <- UI sprite sheet(s)
    Placeholders/ (generated; safe to ignore)
  Audio/Music, Audio/SFX   <- optional audio clips
  Data/
    UISkin.asset          <- UI sprite slots + fonts
    AudioLibrary.asset    <- music + sound effect slots
    FxLibrary.asset       <- Feel cues, shader materials, polish switches
    Venues/Venue_*.asset  <- one asset per stream setup (incl. Venue_stage_<streamer>)
  Fonts/          Noto Sans, Noto Sans SC, Rubik, ZCOOL KuaiLe (OFL) + generated TMP font assets
  FX/Feedbacks, FX/Materials   <- generated Feel prefabs and SSU uber materials
  Localization/   locales, string tables, LocalizationSettings (generated from the CSV)
  Resources/
    DeckPartyDatabase.asset          <- viewer + streamer art, stream setups, links everything
    Localization/AudienceCapture_Strings.csv   <- every on-screen text, all languages (the master copy)
    Network/DeckPartyNetSession.prefab   (networking, don't rename/move)
  Prefabs/UI/     GuestCard, GuestRow, ScenarioItem, PlayerRow, TextLine
  Prefabs/PortraitStage.prefab       (renders animated characters for the UI)
  Scenes/         MainMenu.unity, Game.unity
  Scripts/
    Core/      pure C# rules engine (no Unity dependency), incl. StreamerCatalog
    Runtime/   Unity code: UI, sessions, networking, audio, data, Localization, Fx, Animation
    Juice/     DeckParty.Juice: the Feel backend (and Editor/ builds the Feel prefabs)
    Editor/    scene/prefab builders and art, localization, animation and FX tools
```

---

## 3. Importing art

Drop images into the matching `Art/` subfolder. On first import they're set up as sprites (Sprite (2D and UI), no mipmaps), and the auto-assigner hooks them up by file name.
- **Sprite sheets:** set **Sprite Mode: Multiple**, slice the sheet in the **Sprite Editor** and name the slices. Applying re-runs the auto-assign.
- **Pixel art:** set **Filter Mode: Point** and **Compression: None**. Your Inspector changes are kept.
- **Name matching:** ignores case, spaces, `_` and `-`.

You can always assign sprites by hand instead. Each section below says which asset field to use.

---

## 4. Characters (viewer and streamer art)

**Full step-by-step guide and the 59-character file-name table: `V1Instructions.md`, section 2. Streamers and animation: `V2Instructions.md`, sections 2 and 4.**

**Where they go:** `Assets/DeckParty/Resources/DeckPartyDatabase.asset`.

The inspector has a search box and filters, and two lists:
- **Guests:** one entry per viewer. Drag a sprite into **Portrait**.
- **Streamers:** one entry per streamer, with Portrait, Character Prefab, Animator Controller, Venue (their stage) and Color.

Each viewer entry also has:
- **Tint:** placeholder/accent colour (alpha 0 means the automatic family colour).
- **Animation...:** Character Prefab (rigged PSB or animated prefab), Animator Controller, Framing Zoom/Offset, and **Create Override Controller**.

Portraits are shown with "preserve aspect" in a 180x178 area of each 200x290 card. Square-ish art with a transparent background looks best.

Names are now translated. To rename a character on screen, edit its `char.<Type>.name` row in the strings CSV (`UIGuide.md` section 7).

---

## 5. Backgrounds (stream setups)

**Full step-by-step guide and the background file-name table: `V1Instructions.md`, section 3.**

Each background is a **Venue** asset in `Assets/DeckParty/Data/Venues/`:
- **7 general setups:** `Venue_bedroom`, `Venue_content_house`, `Venue_cursed_stream`, `Venue_studio_stage`, `Venue_city_irl`, `Venue_metaverse` and `Venue_irl_beach`.
- **One stage per streamer:** `Venue_stage_fate`, `Venue_stage_brojogan` and so on.

Players pick one in the Single Player screen, in the multiplayer lobby (each player picks their own), or from the in-game **Menu** at any time.

Each venue asset has three art fields:
- **Background:** full-screen image, file `bg_<id>`. **Author it at 21:9 (2520x1080 or larger) and keep the important parts inside the central 16:9.** Ultrawide screens show the full width; 16:9 crops the sides.
- **Foreground:** optional overlay, file `bg_<id>_fg`.
- **Thumbnail:** optional, file `bg_<id>_thumb`.

Other settings:
- **Palette and style:** only drive the placeholder scene drawn while Background is empty.
- **Party Lights:** adds moving spotlights, a red/blue flash when a stream gets deplatformed and a red alert at high risk. Untick it for a clean image.
- **Music:** optionally replaces the stream music for that setup.

**Special choices in every setup selector:**
- **Automatic (your streamer's setup)** (the default): the chosen streamer's stage. With No Streamer, each character set's own background (Random Mix rolls one from its seed).
- **Evolving (grows with your Sub Cap):** your background upgrades as your Sub Cap grows (bedroom, then city at 9, studio at 14, content house at 20, metaverse at 27). Edit the stages in the database's **Evolving Venue** list.

**Adding a new setup:**
1. Right-click in `Data/Venues` and choose **Create > Deck Party > Venue**.
2. Give it a unique **Id** and assign art.
3. Add it to the database's **Venues** list. It appears in every selector.
4. Add `venue.<id>` and `venue.<id>.desc` rows to the strings CSV for its name.

---

## 6. UI art (UI Skin)

All menus, the HUD, cards and buttons draw from `Assets/DeckParty/Data/UISkin.asset`. Assign a sprite to a slot and every element using that slot updates, in edit mode too.

| Slot | Used for | Sprite name | Tips |
|---|---|---|---|
| Panel | Screen panels, side panels, dialogs | `ui_panel` | 9-slice |
| PanelAlt | Rows, result card, character set items, streamer power panel | `ui_panelalt` | 9-slice |
| Button | Normal buttons | `ui_button` | 9-slice |
| ButtonPrimary | Main actions (Add Viewer to Chat, Go Live, Buy) and gold buttons | `ui_buttonprimary` | 9-slice |
| ButtonSecondary | Secondary actions (Multiplayer, action buttons) | `ui_buttonsecondary` | 9-slice |
| ButtonDanger | Deny / Quit / Reset | `ui_buttondanger` | 9-slice |
| CardFrame | Viewer card frame | `ui_cardframe` | 9-slice |
| CardFrameStar | Frame for Diamond viewers (optional) | `ui_cardframestar` | 9-slice |
| SlotEmpty | Empty Sub Cap space | `ui_slotempty` | 9-slice outline |
| Badge | Small badges (permanent +1, x2) | `ui_badge` | 9-slice |
| Circle | Placeholder portrait disc, player colour dot | `ui_circle` | |
| TopBar | HUD bar | `ui_topbar` | 9-slice |
| InputField | Text fields and selector value boxes | `ui_inputfield` | 9-slice |
| Tooltip | Tooltip and toast background | `ui_tooltip` | 9-slice |
| ScrollHandle | Scrollbar handle | `ui_scrollhandle` | 9-slice |
| Glow | Light glow | `ui_glow` | soft radial |
| Logo | Main-menu logo (replaces the "AUDIENCE CAPTURE" text) | `ui_logo` | |
| IconPop / IconCash / IconDoor | HUD stat icons (Gifted Subs, Cash, Sub Cap); IconPop and IconCash are also the resource icons inside text | `icon_pop`, `icon_cash`, `icon_door` | |
| IconTrouble / IconStar | Deplatform Risk and Diamond pips, card badges; IconTrouble is also the risk icon inside text | `icon_trouble`, `icon_star` | |
| IconCancel | "Cancels risk" badge (AutoMod, VIPS, Wholesome Icon, Cheems with a Doge) | `icon_cancel` | |
| IconCrown / IconCheck | Host and ready markers | `icon_crown`, `icon_check` | |

Notes:
- **9-slice:** in the Sprite Editor, drag the green border handles so the corners don't stretch. Slots marked 9-slice default to *Sliced* drawing.
- **Colours:** UI elements tint their sprite (for example, buttons are tinted pink). If your sprites are already coloured, tick **Override Color** on that slot and leave the colour **white**.
- **Fonts:** **Body Font** and **Header Font** default to the generated Noto Sans and Rubik assets, with Chinese fallbacks. To use your own, create a *dynamic* TMP font asset and assign it. Then add `Fonts/NotoSans SDF` and `Fonts/NotoSansSC SDF` as its fallbacks so every language still renders.
- The placeholder PNGs in `Art/Placeholders` are what the slots use by default. Replace them through the skin rather than editing the PNGs.

---

## 7. Audio (optional)

`Assets/DeckParty/Data/AudioLibrary.asset` has **Menu Music**, **Party Music**, **Planning Music** and one slot per sound effect:

| Slot | Plays when |
|---|---|
| Click, Hover | A button is clicked or hovered |
| DoorKnock, GuestArrive | A viewer is waiting / joins chat |
| StarArrive | A Diamond joins chat |
| Boot, Ability | A viewer is kicked / uses an action |
| Coin, Purchase | Money comes in / a viewer is bought |
| Expand | The Sub Cap is raised |
| PartySuccess | A stream ends successfully |
| Raid | The stream gets deplatformed |
| FireMarshal | Server Down (chat over the Sub Cap) |
| Victory, Defeat, Error, Notify, Peek | The matching moments |

Any empty slot plays a small synthesized placeholder, so the game is never silent. A setup's own **Music** overrides Party Music. With the **Audio Juice** polish switch on, music ducks under big moments and goes muffled while a menu is open.

---

## 8. Editing the scenes

You're free to rearrange, restyle and reparent anything in **MainMenu** and **Game**. Every script reference is a normal serialized field, visible in the Inspector.

**MainMenu:** `MainMenuController` (scene root) links the seven screens:
- Main
- **Select Your Streamer**
- Single Player
- Multiplayer
- Lobby
- Settings
- How to Play

**Game:** `GameController` (scene root, split across `GameController.cs`, `.Presence.cs` and `.Juice.cs`) links these parts:
- the HUD and the chat grid (a scroll view inside a **ContentFrame** that caps the UI at 21:9),
- the stream controls and the shop with the **Streamer Power** panel,
- the police screen,
- the stream: a stage strip on top and the **chat bubble** (`ChatBubbleView`) with the viewer cards and floating viewer emojis (`ViewerEmojiView`),
- the side panel: players and the emote bar (multiplayer), the optional Activity log (`SideTabsView`, off by default); the **facecam** (`FacecamView`) sits in the stream's bottom-right corner,
- the overlays (picker, Audience, pause, game over, banner, Deplatform / Server Down images, effects, anxious streamer, turn timer, presence, stream overlay, link lines).
- the Community Beats chips at the top of the chat bubble (`CommunityBeatsView`) and the community icons on cards and shop tiles (`CommunityIconsView`).

**Text:**
- **Static captions:** carry a **LocalizedLabel** with a CSV key.
- **Editing a caption in the scene:** change its key, not its text. The text is replaced at runtime.
- **Lists:** cards, shop rows, players and log lines are spawned from the prefabs in `Prefabs/UI`. Edit those prefabs to restyle every card or row at once.

**Cards:**
- **Design size:** 200x290, scaled to fit. Keep new card content inside the card prefab's `Visual/Anim` object.
- **Portrait:** the **CharacterPortrait** (still sprite or animated) with a **CardFx** (shader statuses).

Running **Build Everything** again never touches existing scenes. Only **Rebuild ... (overwrites)** does. `UIGuide.md` has the detailed map.

---

## 9. Playing

- **Goal:** get 4 Diamond viewers into one stream and end it successfully before the last night (25 by default).
- **Streamer:** pick one on **Select Your Streamer** (before Single Player, and before hosting or joining). Passive powers are always on. Active powers are used from the shop's **Streamer Power** panel on your turn. See `V2Instructions.md` section 2 for every power.
- **Stream:** **Add Viewer to Chat** (Space) lets the next viewer in the queue join.
  - Purple buttons on cards are once-per-stream actions. Some open a choice popup first.
  - Actions that take several viewers show a **Confirm** button (or press Space).
  - **End the Stream** (E) banks everyone's Subs as Gifted Subs, plus Cash.
  - A skippable scoring sequence counts each viewer up.
- **Deplatform Risk:** 3 net Deplatform Risk and the stream gets **deplatformed**. You get no payout, and one viewer gets a Temporary Ban for the next stream.
  - The HUD shows the meter. Under the buttons you see how many viewers with Deplatform Risk are still in the queue, but never the odds: working them out is part of the game.
- **Communities:** every viewer belongs to none, one or several communities. Each **different** viewer in chat gives their community a point (live), and points give bonuses (Core Fanbase cancels risk, Staff waives costs, Content scores twice, Techspertise saves Server Downs, Finance Bros double Cash, Trolls pay out when deplatformed, Doomsdayers curse rivals). Rules: `Core/Community.cs`; who belongs where: Game Database > Viewers > Communities.
- **Server Down:** going over the Sub Cap (viewers who bring others) ends the stream with no payout.
  - At one risk from the police, your streamer appears at the top of the screen looking anxious, and the facecam (bottom right) pulses red.
- **Shop:** spend Gifted Subs recruiting viewers and Cash raising your Sub Cap, use your streamer power, then start the next night (Space).
- **Audience** (R) shows all your viewers and permanent changes.
- **Esc** cancels targeting or opens the menu. **F11** or **Alt+Enter** toggles fullscreen. Hover anything for details.
- **F9** (editor and development builds, single player): +50 Gifted Subs and +10 $ for testing.
- Single-player seasons autosave after every action. **Continue** appears on the main menu.

The full rules and the viewer glossary are also in-game under **How to Play**. The meme viewers' rule interpretations are listed in `V1Instructions.md` section 5.

**Implementation decisions where the rules doc was ambiguous** (all adjustable in `Scripts/Core/GameRules.cs`):
- Viewer fees are paid from your Cash bank first. Only the unpaid remainder costs 7 Gifted Subs per Cash (`FeesDrawFromBank`).
- Being deplatformed on the final night ends the season without a Temporary Ban.
- Going over the Sub Cap doesn't ban anyone.

**Where to change things:**
- **Character sets and viewer stats:** `ScenarioCatalog.cs` / `GuestCatalog.cs`.
- **Streamer powers:** `StreamerCatalog.cs`.
- **On-screen wording:** the strings CSV (`UIGuide.md` section 7).

---

## 10. Multiplayer: how it works

- **Players:** 1 to 6.
  - The **host** runs every player's game (server-authoritative).
  - Clients send their actions and get their own state back, plus a live summary of everyone else in the side panel.
- **Same start:** everyone gets the **same character set** and starting Audience, then builds their own stream. Each player brings their own **streamer**. A Random pick is rolled when the season starts.
- **Open information:** the host sends every player's season to everyone. Click a player in the in-game **Players** panel to watch their stream live (read-only). The strip above the board shows whose stream is on screen.
- **Presence:**
  - While someone is acting, everyone watching sees what they point at and pick, in that player's colour: outlines and name tags on cards, shop rows and buttons, plus target arrows.
  - A live status line appears under their name ("eyeing Doge", "browsing the shop").
  - Watchers also see their mouse pointer.
  - Updates are relayed by the host a few times a second. Up to a second of lag is expected and harmless.
- **Emotes, not chat:** multiplayer text chat is switched off (FX Library > Text Chat; the code is kept). Players talk with the emote buttons under the Players panel; a sent emote pops up from the sender's avatar on every screen. The buttons use the sprites in Game Database > Stream Art > Emotes (text emotes such as POG and o7 until you add them).
- **Rival powers:** Raid and Wealth Tax are resolved by the host on both players' games, with a callout to everyone.

**Modes:**
- **Take Turns** (default): each night, players stream one after another in lobby order while everyone else watches the active player's board.
  - **End Turn** (after the stream and shopping) passes to the next player.
  - The host can set a **turn timer**: Off, Hard 30 s, Medium 60 s, Easy 120 s, or Custom 10 to 600 s. It covers the whole turn. When it runs out, the stream ends (scored normally) and the turn passes.
  - The season ends after the first night someone ends a 4-Diamond stream, once everyone has played that night.
- **Free For All** (was Synchronized Nights; the only mode with 3+ players): everyone streams the same night at once without seeing each other, the shop opens for everyone together after a 3-2-1 once all have clicked *Go to the Shop* (first come, first served), and the next night starts when every player presses *Ready* (see V4Instructions.md, section 7). The first night anyone ends a 4-Diamond stream, the season ends once everyone has finished that night. Ties go to the earlier win, then to more total Gifted Subs earned.
- **Race:** everyone plays at their own pace, and the first 4-Diamond stream wins instantly.
- **No winner:** if nobody wins, the best Diamond count in a successful stream wins, then total Gifted Subs earned.

**Lobby and season:**
- The host picks the character set, mode, season length (10 to 30 nights) and turn timer in the lobby. Everyone else presses **Ready**, then the host presses **Start Season**. The host can start solo for testing.
- The lobby shows lobby news (joins, leaves). Deplatformings, Server Downs, Doomsdayers curses, wins, Diamond recruits, streamer powers and joins pop up for everyone, each in the reader's own language.
- **Shared shop:** each player's season is separate (Audience, wallet, Sub Cap, streamer power costs), except the shop's stock: a copy one player recruits is gone for everyone (`GameRules.SharedStockPerGuest`, 4 per viewer; Diamonds unlimited).
- After the season, the host can press **Back to Lobby** to play again with the same group.
- Everyone must run the **same build**. A version check (`ProtocolVersion` in `NetworkService.cs`, currently `deckparty-4`) rejects mismatches with a clear message.

There are two ways to connect, both on the Multiplayer screen:

| | Online (join code, Unity Relay) | Direct IP / LAN |
|---|---|---|
| Works over | The internet, no port forwarding | Same Wi-Fi/LAN, or internet with a forwarded port |
| Host clicks | **Host Online Game** | **Host** |
| Host shares | A 6-character **join code** | The host's IP address and port (7777) |
| Setup needed | Link the project to Unity Cloud (once, see 11d) | None for LAN |

---

## 11. Testing multiplayer

### a) Fastest: two players in the editor (Multiplayer Play Mode)
1. Open **Window > Multiplayer > Multiplayer Play Mode** (installed by this project).
2. Tick **Player 2** (and more if you like) and wait for the virtual player to boot.
3. Open the **MainMenu** scene and press **Play**. A second window appears for Player 2.
4. Main editor: **Multiplayer**, pick a streamer, then **Host** (under Direct IP / LAN).
5. Player 2 window: **Multiplayer**, pick a streamer, enter IP `127.0.0.1` and port `7777`, then **Join by IP**. Press **Ready**.
6. Main editor: **Start Season**.

Relay works here too once 11d is done: **Host Online Game** in one window, paste the code in the other. Each window signs in with its own profile automatically.

### b) Two builds on one PC
1. **File > Build Profiles > Windows** (scenes are already in the list) > **Build**.
2. Run the .exe twice (it's windowed). Host in one, and join `127.0.0.1` : `7777` in the other.

### c) Friends on the same Wi-Fi / LAN
1. Send your friends the build folder (zip the whole output folder).
2. The host clicks **Host**. The lobby shows the host's address, e.g. `192.168.1.20:7777`. It's also shown on the Multiplayer screen as *Your address*.
3. When Windows Firewall asks, **allow Deck Party on Private networks**.
4. Friends type that IP and port and click **Join by IP**.

### d) Friends over the internet: Unity Relay (recommended)
One-time setup:
1. Sign in to Unity in the editor (account menu at the top-left).
2. **Edit > Project Settings > Services**: create or link a Unity Cloud project for Deck Party.
3. Go to https://cloud.unity.com, open that project, and enable **Relay** (Multiplayer > Relay). Relay has a free tier that's plenty for friends.
4. Check that **Authentication** is available for the project. Anonymous sign-in is used automatically, so there's no login screen.
5. Make a build (11b) and share it.

To play:
1. The host clicks **Host Online Game** and shares the join code from the lobby (there's a **Copy** button).
2. Friends click **Multiplayer**, pick a streamer, paste the code, then click **Join**.

If the project isn't linked, Deck Party shows "Online play isn't set up yet..." instead of failing silently.

For a step-by-step guide to building, sending the game to a friend and a test session together, see section 6 of `V1Instructions.md`. Then run the presence, timer and rival-power checks in section 6 of `V2Instructions.md`.

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
| Text shows squares | **Deck Party > Localization > Rebuild Fonts**. If you use your own font, give it the Noto fallbacks (section 6). |
| Text is English in every language | **Deck Party > Localization > Import CSV into String Tables**, then check the Console. |
| Text shows `[some.key]` | That key is missing from the strings CSV. Add the row and re-import. |
| Cards have no glow or status effects | **Deck Party > FX > Rebuild FX (overwrites)**. Check that Sprite Shaders Ultimate is in `Assets/`. |
| Client can't connect on LAN | Same port on both sides? Firewall allowed? Try `127.0.0.1` on the same PC first. |
| "Version mismatch" | Everyone needs the exact same build. |
| "That game has already started" | Joining is only possible while the host is in the lobby. |
| A client's window seems frozen when unfocused | Builds have Run In Background enabled. In the editor, keep Multiplayer Play Mode windows open rather than minimized. |
| I broke a scene | **Deck Party > Setup > Rebuild Scenes and UI Prefabs (overwrites)**. |
| Reset my progress | **Settings > Reset Progress** (also clears the saved season). |

---

## 13. Code map (for further development)

**Rules engine (Core):**
- `Core/PartyEngine.cs`: every rule. This covers:
  - arrivals, entrance effects and reactions, bring chains,
  - Deplatform Risk and being deplatformed, the Sub Cap and Server Down, communities,
  - all 21 action types, passive scoring,
  - the shop and Sub Cap upgrades,
  - streamer powers (`UsePower`, `UseRivalPower`, `GetPowerBlocker`).

  It's deterministic: all randomness lives in `PlayerState.RngState`, and the queue is shuffled once per stream. Rejections are localization keys built with `Err.Of`.
- `Core/StreamerCatalog.cs`: streamers, power kinds, costs, cooldowns and tuning constants.
- `Core/GuestCatalog.cs` / `GameRules.cs` / `ScenarioCatalog.cs`: viewer stats, tuning numbers, character sets.
- `Core/StateSerializer.cs`: compact binary state (format version 3; reads version 2), used for save files and network snapshots.

**Localization and visuals:**
- `Runtime/Localization/Loc.cs`: all text. `Loc.Get(key, args)`, plurals, language switching, the CSV fallback and the pseudo-language. `LocUnityTables.cs` connects it to Unity Localization; `LocalizedLabel.cs` handles static captions.
- `Runtime/Visuals/`: `Terms.cs` (mechanic names), `GuestVisuals.cs`, `StreamerVisuals.cs`, `EventText.cs` (Stream Log lines), `ContentText.cs`, `RulesText.cs`.

**Effects and animation:**
- `Runtime/Fx/`: `Fx.cs` (cue playback, shake, Feel or the built-in fallback), `CardFx.cs` (SSU statuses), `SignatureFx.cs` (per-viewer effects and stamps).
- `Juice/` (`DeckParty.Juice`): the Feel backend. `Juice/Editor` builds the Feel prefabs.
- `Runtime/Animation/`: `PortraitStage.cs` (render-texture stage, `CharacterAnimDriver`) and `CharacterPortrait.cs` (what cards, rows and the select screen use).

**Sessions and networking:**
- `Runtime/Session/`: `IGameSession`, with `LocalGameSession` (single player and autosave) and `NetworkGameSession` (multiplayer). The UI only talks to this interface.
- `Runtime/Networking/NetworkService.cs`: creates the NetworkManager at runtime, Relay and direct hosting/joining, and connection approval (version, lobby full, game in progress). The connection payload carries the player's streamer.
- `Runtime/Networking/DeckPartyNetSession.cs`: lobby, match settings, and per-player games on the host (every snapshot is broadcast to everyone). It also handles:
  - the take turns / synchronized nights / race logic and the turn timer,
  - rival powers, presence and emotes relays,
  - rankings, chat and headlines (RPCs, NetworkVariables and a NetworkList).

**UI:**
- `Runtime/UI/Game/GameController*.cs`: the game scene.
  - **Core file:** renders state, runs the action flow (choice popup, target picks, Confirm) and hotkeys.
  - **Presence file:** what's sent and shown to watchers.
  - **Juice file:** event playback, the scoring sequence (with community bonus tags), stamps and the Deplatform / Server Down images.
- `Runtime/UI/Menu/StreamerSelectScreen.cs`: the character select screen.
- `Runtime/App/DisplaySettings.cs`: resolution and window mode presets, F11.
- `Editor/*SceneBuilder.cs`, `StreamerSelectBuilder.cs` and `DeckPartyAssets.cs`: generate the scenes and prefabs. They're useful as a reference for the hierarchy.
