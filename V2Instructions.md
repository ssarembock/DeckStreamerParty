> V3 note: the fake chat is gone, Swatting is now **Platform Ban**, the Sub Cap crash is **Server Down**, and the shop and side panel changed. See **V3Instructions.md** for what's current.

# Deck Party V2 (Audience Capture): getting the update into Unity and testing it

This is the polish update planned in `PlanForPolish.md`. Players now see the game as **Audience Capture**. The product name, the repo and the Unity project are still "Deck Party". The update adds:

- the theme pass,
- a streamer select screen with 11 streamer powers,
- 5 languages,
- display and ultrawide support,
- animated characters,
- Feel and Sprite Shaders Ultimate polish,
- multiplayer presence, emotes and a turn timer.

- **Where to change things afterwards:** `UIGuide.md` (Deck Party > Open UI Guide).
- **Setup, art basics and multiplayer hosting:** `INSTRUCTIONS.md` and `V1Instructions.md`.

---

## V2.1: facecam, Activity / Chat tabs, streamer quotes and the card art fix

**What changed:**

| Area | Change |
|---|---|
| Card art (bug fix) | Viewer card portraits were invisible in chat, the Audience, pickers and the peek panel. Cause: the Sprite Shaders Ultimate card material had **Halftone** switched on at Fade 0. Halftone is a "fading" effect, so 0 means fully transparent. Halftone is now off. `FX/Materials/PortraitUber.mat` is already fixed on disk, and any rebuild repairs older copies. Shop rows were never affected because they don't use that material. |
| Select screen | The parody stat bars (RIZZ, DRAMA, AURA, COPIUM) are gone. The quote stays, but it now comes from a **Quotes** list on each streamer in the Game Database. A random quote shows each time a streamer is highlighted, and it differs from last time when there are two or more. |
| Side panel | The Stream Log and the chat share one panel with two tabs: **Activity** and **Chat**. The tab you're not on shows a pink count of new lines. Single player has only Activity. |
| Facecam | A square box in the bottom-right corner, under the tabs. It shows the streamer of the stream on screen, with a name plate; in Take Turns that's the active player's. Reactions: <br>- **One Swatting Risk from swatted:** the frame pulses red and the streamer trembles (or plays `Anxious`). <br>- **Swatted or crashed:** `Hit`. <br>- **Stream ends well:** `Celebrate`. <br>- **Power used:** `Select`. <br>Without an animation, each reaction is a punch. **No Character** shows "Camera off". In multiplayer, the emote buttons sit in a strip to the left of the facecam, so they work from either tab. |
| Swatting odds | "SWAT chance: N%" and "Over-cap chance: N%" are gone; players work the odds out themselves. Under the buttons there's only the number of viewers with Swatting Risk still in the queue. It turns red when one more risk would get you swatted. |
| Stream overlay | The fake viewer count is gone. The LIVE badge and stream timer stay. |

**Get it into Unity:**
1. Let Unity recompile.
2. Run **Deck Party > Setup > Rebuild Scenes and UI Prefabs (overwrites)**. This is needed because both scenes changed (select screen, side panel, facecam). Your Game Database and portraits are kept, as before. The rebuild also:
   - copies each streamer's original catchphrase into their Quotes list, once (delete it and it stays deleted);
   - repairs the card material if needed.
3. **Add quotes:** open **Deck Party > Select Game Database**. **Streamers** is now at the top, above Viewers.
   - **Each row:** the Portrait and **Quotes (one at random)**. Click **+** to add a line.
   - **More...:** Stream Setup, Color, Character Prefab and Animator Controller.
   - **Languages:** quotes show exactly as typed in every language. The original catchphrase stays translated as long as its text is unchanged.
   - **No Character:** keeps its built-in line ("Just here for the vibes.").

**Quick check (about 10 minutes):**

| # | Do this | What should happen |
|---|---|---|
| 1 | **Single Player** | No stat bars. A highlighted streamer shows a quote under the power box. Move away and back, and the quote changes if they have two or more. |
| 2 | Go live and add viewers | Card portraits show your sprites, not just coloured discs. The overlay shows only LIVE and the timer. |
| 3 | Look at the side panel | The **Activity** tab holds the Stream Log, and there's no Chat tab. The facecam in the bottom-right corner shows your streamer's portrait and name. |
| 4 | Let in risky viewers until you're at 2 risk | The line under the buttons reads "N viewers with Swatting Risk in the queue" in red, with no percentages. The facecam frame pulses red and the streamer trembles. |
| 5 | End a stream, then get swatted | The facecam punches each time (or plays `Celebrate` / `Hit` for an animated streamer). |
| 6 | Press **Audience [R]** | Every card shows its portrait, including Temporary Ban (now grey and flickering). |
| 7 | Multiplayer | **Chat** sits next to Activity. New messages put a pink count on the tab you're not viewing. The emote buttons are a 2-column grid left of the facecam. In Take Turns the facecam follows the active player and shows their name in their colour. |

---

## 0. Get the update into Unity (about 5 minutes)

1. Click back into Unity and let it import. Package Manager adds **Localization 1.5.8**, which brings in **Addressables**. The first compile can take a minute or two.
   - If the Console shows errors only from `DeckParty.Juice` or the Localization code while packages are still importing, wait for the spinner (bottom right) to stop. Unity recompiles once the packages are in.
2. Run **Deck Party > Setup > Rebuild Scenes and UI Prefabs (overwrites)**. This rebuild is needed because the scenes and prefabs gained many new objects. These include the streamer select screen, the power panel, overlays, the content frame, the animated card portrait and the larger cards.
   - **Kept:** the Game Database, including the portraits you assigned with the database tool. Also kept: your art, stream setups (venues), UI Skin and audio.
   - **Backed up:** the old UI prefabs are copied to `Assets/DeckParty/Prefabs/Backup` first.
   - **Also runs:** everything *Build Everything* does (next step).
3. The rebuild also runs these steps (you don't need to do anything):
   - **Languages:** creates the 5 locales and imports `Resources/Localization/AudienceCapture_Strings.csv` into 6 string tables in `Assets/DeckParty/Localization/Tables`.
     - The Console says `Imported N strings into 6 string table collections.`
   - **Addressables:** turns on *Build Addressables with Player*, so the language tables end up in builds.
   - **Fonts:** builds the TextMeshPro fonts into `Assets/DeckParty/Fonts`:
     - Noto Sans, Noto Sans Bold, Noto Sans SC (Chinese), Rubik (headers) and ZCOOL KuaiLe (Chinese headers).
     - All are dynamic, with fallbacks, so any character is generated when first needed.
   - **Animation setup:**
     - creates the animator templates in `Assets/DeckParty/Animation`,
     - creates the portrait stage prefab,
     - names layer 31 **CardPortraits**.
   - **FX:** creates `Data/FxLibrary.asset`, the Sprite Shaders Ultimate uber materials in `FX/Materials`, and one Feel feedback prefab per moment in `FX/Feedbacks`.
   - **Streamers:** adds the 11 streamer entries to the Game Database, and a `Venue_stage_<streamer>` stream setup for each one.
4. Press Play in **MainMenu**.

**Saves and settings carry over.** The save format moved to version 3 and still reads version 2 saves, so **Continue** keeps working. A season that was saved before the update has no streamer.

**Multiplayer:** the network version is now `deckparty-4`, so older builds are turned away with "Version mismatch". Send your friend a new build (`V1Instructions.md`, section 6B to 6C).

**Not committed yet:** these folders are on disk but not in git yet:
- `Assets/Feel`
- `Assets/Sprite Shaders Ultimate`
- `Assets/DeckParty/Fonts`

The repo is private, so committing the Asset Store packages is fine. Say the word and I'll commit and push everything.

---

## 1. What changed, at a glance

| Area | What you'll see |
|---|---|
| Names | **Guests** are now **viewers**, and the **Squad** is your **Audience**. The **Party Log** is the **Stream Log**. *Open the Door* is now **Add Viewer to Chat**, and *End Party* is now **End the Stream**. **Venues** are **stream setups**, and *Admit/Reject* is now **Approve/Deny**. In the other languages, swatting becomes the police arriving (see section 3). |
| Title | **AUDIENCE CAPTURE**, with a shine sweep. Each language has its own title (section 3). |
| Streamers | **Select Your Streamer** comes before Single Player and before joining a multiplayer game. Each streamer brings a power and their own stream setup (section 2). |
| Cards | Cards are bigger (200x290 design size, with a taller portrait). They have a hover lift and tilt, and static portraits breathe gently. Status effects show as shader effects: Infected, Immune, Zeroed, Double, Converted, Rickrolled and Temporary Ban. |
| Animation | Characters can be a single sprite (as now), an animated prefab or rigged PSB, or a sprite with an Animator Controller (section 4). |
| Juice | Every moment has a Feel feedback: arrivals, Diamonds, each action type, kicks, risk pips and the Sub Cap filling up. Big moments include SWATTED!, the crash, victory and defeat. The scoring sequence counts each viewer up, and you can skip it with Space or a click. There are also comic stamps (BONK!, U MAD?), a fake chat feed, a LIVE overlay and link lines for Overly Attached Girlfriend and Nyan Cat chains. |
| Anxious streamer | At one Swatting Risk from the police, your streamer drops in at the top of the screen sweating, with an intermittent shake. The background lights turn red. |
| Display | **Settings > Display & Language** has Window Mode (Windowed / Fullscreen, borderless) and Resolution (16:9, 16:10, 21:9 and 32:9 presets that fit your monitor). Apply shows a 10-second "Keep these settings?" countdown. **F11** or **Alt+Enter** toggles fullscreen. The UI stops widening at 21:9, and the chat adds columns (9 at 16:9, up to 12) before it scrolls. |
| Accessibility | **Screen Shake** slider, **Reduce Flashing**, **Fake Chat Feed** and **Fast Animations**. These are in Settings and in the in-game menu. |
| Multiplayer | Watchers see what the active player points at, in that player's colour. There's a live status line under each player, a ghost cursor, emotes, rival power callouts and an optional **turn timer**. Streamers sync, and the lobby shows each player's streamer. |

---

## 2. Streamers

You pick a streamer on the **Select Your Streamer** screen:
- **Single Player:** after you click Single Player. *Back* returns to the main menu.
- **Multiplayer:** after you click Multiplayer, before you host or join. The lobby has a **Streamer: X (change)** button.
- **Controls:** arrow keys (or WASD) and Enter, or the mouse. **Random** picks one when you go live. **No Streamer** plays the classic rules.

**How powers work:**
- **Passive** powers are always on.
- **Active** powers are used from the **Streamer Power** panel in the shop, on your own turn, after your stream. They cost Gifted Subs (GS) and/or Cash. Most cost more each time you use them and then recharge for a few turns.
- **Next Stream** powers arm now and act during your next stream.
- **Rival** powers hit another player, so they only work in multiplayer. In single player the button says "Needs a rival (multiplayer only)", and the select screen warns you.

| Streamer (parody of) | Power | Type | Cost (first use, +increase per use) | Cooldown | Effect |
|---|---|---|---|---|---|
| Fate (Destiny) | Raid | Rival | $1, +$1 | 2 | The rival you pick starts their next stream with +1 Swatting Risk. Raids stack up to +2, so a raid alone can never swat anyone. |
| Bro Jogan (Joe Rogan) | Three-Hour Episode | Passive | - | - | Start with +1 Sub Cap. Sub Cap upgrades cost 1 $ more. |
| Shinobi (Ninja) | Smoke Bomb | Next Stream | 2 GS, +2 GS | 2 | Your next stream ignores 1 Swatting Risk. |
| IRunFast (IShowSpeed) | Bark at Chat | Passive | - | - | Viewers carrying Swatting Risk earn +1 Subs. |
| MrShark (MrBeast) | $10,000 Giveaway | Next Stream | $3, +$1 | 2 | Every viewer earns +1 Subs in your next stream. |
| Ethical F**kboi (Hasan Piker) | Wealth Tax | Rival | 1 GS, +1 GS | 2 | The richest rival pays you up to 2 $ (only what they have). |
| Soft Hard Body Man (Tyler1) | Soft Landing | Passive | - | - | When you get swatted, you still bank half that stream's Subs. |
| Strategic Abomination (two-headed chess streamer) | Double Move | Next Stream | 1 GS + $1, +1 each | 2 | The first viewer action you use in your next stream can be used again. |
| Palmane (Pokimane + Palworld) | Pal Sphere | Active (instant) | $2, +$2 | 3 | Catch a random shop viewer (cost 6 or less, not a Diamond) for free. |
| Manim Maketh Man (3Blue1Brown) | Beautiful Sequence | Passive | - | - | Each successful stream in a row earns +1, +2, +3... Subs (max +5). Getting swatted or crashing resets it. |
| Rude Radiologist (Dr Disrespect) | X-Ray Vision | Next Stream | $1, +$1 | once per turn | See the next 3 viewers in your queue during your next stream. |

**Tuning:**
- **Numbers:** the constants and the power table at the top of `Assets/DeckParty/Scripts/Core/StreamerCatalog.cs`. Change them freely.
- **Rule logic:** `PartyEngine.cs`. Search for `Streamer`.
- **Network version:** if you change rules that clients run, bump `ProtocolVersion` in `NetworkService.cs` to `deckparty-5`.

**Streamer art:**
- **File names:** drop `streamer_<name>.png` into `Assets/DeckParty/Art/Characters` or the new `Art/Streamers` folder, e.g. `streamer_fate.png` or `streamer_bro_jogan.png`. It's auto-assigned like the viewers. **Deck Party > Art > Log Expected File Names** lists every name.
- **By hand:** **Deck Party > Select Game Database > Streamers**. Each row has the Portrait and the **Quotes** shown on the select screen (one at random). **More...** opens Stream Setup, Color, Character Prefab and Animator Controller.
- **Stream setup background:** `bg_stage_<name>.png` in `Art/Backgrounds`, e.g. `bg_stage_fate.png`. Until you add one, each stage draws a placeholder scene in that streamer's colours.
- **Placeholders:** without a portrait, streamers show their initials on a disc in their colour.

**The stream background** follows your streamer when the stream setup is **Automatic (your streamer's setup)**, which is the default. Any other choice in the Single Player screen, the lobby or the in-game menu overrides it. With No Streamer, Automatic uses the character set's background.

---

## 3. Languages

**Settings > Display & Language > Language** switches between English, 简体中文, Русский, Español (Latinoamérica) and Português (Brasil). The text updates instantly.

- **Pseudo:** in the editor and development builds there is also **Pseudo**. It stretches and accents the English so you can spot text that overflows or was never extracted. Any plain English text left on screen in Pseudo is a missed string; tell me where.
- **Titles:**
  - zh: 直播争霸
  - ru: БИТВА ЗА АУДИТОРИЮ
  - es: ¡GUERRA DE STREAMERS!
  - pt: BATALHA PELA AUDIÊNCIA
- **Swatting:** outside English it becomes the police arriving, e.g. 警察上门了！ / ПОЛИЦИЯ ПРИЕХАЛА! / ¡LLEGÓ LA POLICÍA! / A POLÍCIA CHEGOU!
- **Names:** meme names that are known worldwide stay as they are (Doge, Pepe, Gigachad, Skibidi Toilet). The rest use local names (Chinese: 神烦狗, 悲伤蛙; Russian: Яжмать for Karen).
- **Spanish** uses neutral Latin-American "tú" forms.
- **Translation status:** every row is a first draft. The CSV's Status column says `draft`, so a native speaker's pass is worthwhile before release.
- **Already-written lines:** Stream Log lines written before a language switch stay in the old language. New lines use the new one.

How to edit text, add a language or fix fonts: `UIGuide.md` section 7.

---

## 4. Animated characters (optional)

Single sprites keep working exactly as before. To animate a viewer, open **Deck Party > Select Game Database**. The inspector now has a search box and filters. Click **Animation...** on the viewer, then use one of these options:

| You have | Do this |
|---|---|
| A rigged PSB or a prefab with an Animator | Drag it into **Character Prefab**. If its controller already has the state names below, you're done. |
| A sprite plus clips | Click **Create Override Controller**, then drop your clips into the override's slots in `Assets/DeckParty/Animation/Characters`. |
| Only a still sprite | Nothing. It breathes gently on its own (the Idle Breathing switch, N8). |

**The states the game plays.** Missing states fall back to the ones in brackets:

| State | When it plays |
|---|---|
| `Idle` | Viewers without an action, and the fallback for everything |
| `Idle_Pre_Active` | Viewers whose action is still available tonight, and every viewer in the shop, Audience and pickers (falls back to `Idle`) |
| `Active` | Once, when the action is used, then `Idle_Post_Active` (if it's missing, a Feel punch plays instead) |
| `Idle_Post_Active` | After the action is used tonight (falls back to `Idle`) |
| `Score` | The end-of-stream scoring count-up (falls back to `Celebrate`) |
| `Enter` (optional) | Once, when the viewer joins chat during a stream, then idle |
| `Exit`, `Hit`, `Celebrate` (optional) | Kicked or sent back to the queue; hit by another action; victory |
| Streamers: `Idle`, `Select`, `Anxious`, `Hit`, `Celebrate` | `Idle` in the facecam. `Select` when picked on the select screen and when the power is used. `Anxious` at one risk from the police (falls back to `Idle`). `Hit` when swatted or crashed. `Celebrate` when a stream ends well, and on victory. |

**Checking and framing:**
- **Deck Party > Art > Validate Character Animations** reports missing states and clips that animate paths the prefab doesn't have.
- **Framing Zoom** and **Framing Offset** on each entry adjust how the character sits in its card.

---

## 5. Single-player test pass (about 30 minutes)

Use **F9** for +50 Gifted Subs and +10 $ (editor and development builds), and **Open Lobby** so every viewer is in the shop.

| # | Do this | What should happen |
|---|---|---|
| 1 | Main menu | **AUDIENCE CAPTURE** title with a shine sweep every few seconds. Buttons: Continue (if you have a save), Single Player, Multiplayer, How to Play, Settings, Quit. |
| 2 | **Single Player** | **SELECT YOUR STREAMER**: a fighting-game screen with a 13-tile grid (11 streamers, Random, No Streamer). Your pick is on the left with the power box and a random quote from the Game Database. THE ALGORITHM is on the right, with a decorative 99 timer. |
| 3 | Move with the arrow keys, then pick **Fate** | The rival warning appears ("Rival power: it does nothing in single player."). |
| 4 | Pick **Shinobi** and press Enter | The "SHINOBI ENTERS THE STREAM!" announcer plays, then the Single Player screen opens. Pick Open Lobby and **Go Live!**. The log says "You're streaming as Shinobi. Power: Smoke Bomb." The background is Shinobi's stage, the Pro Gamer Dojo placeholder. |
| 5 | Add viewers | Cards dissolve in with a punch, Diamonds get a bigger burst, and the fake chat reacts. The LIVE badge and stream timer tick along. Shinobi is in the facecam (bottom right). |
| 6 | Let in risky viewers until you're at 2 risk | The anxious Shinobi drops in at the top and shakes now and then. The facecam frame pulses red. The background lights go red and the vignette pulses. The risk pips punch as they fill. |
| 7 | Use a few actions (Trollface, Tung Tung, Karen) | Each action type has its own impact. The comic stamps appear (U MAD?, BONK!, MANAGER!), and the target card glitches, burns or flashes. |
| 8 | **End the Stream** | Scoring sequence: each viewer counts up in turn, then the total lands in the HUD. Space or a click skips it. |
| 9 | In the shop, look at **Streamer Power** | The Smoke Bomb button shows **Use** with its cost (2 Gifted Subs). Click it, and it shows "Armed for your next stream" and the price rises. Next stream: the risk meter tooltip shows "Smoke Bomb: -1", and you can survive 3 risk. |
| 10 | Buy a few viewers | A purchase flourish plays on the row you bought from. |
| 11 | Get swatted on purpose | SWATTED! slam, red and blue flashes, a big shake (respecting the Screen Shake slider) and the fake chat spams F. |
| 12 | Settings: Screen Shake 0, Reduce Flashing on, Fast Animations on | No shake, softer flashes, no random glitching, and quicker animations. |
| 13 | Try other streamers (Menu > Save & Quit, then Single Player again) | **Palmane:** Pal Sphere adds a free viewer. **Rude Radiologist:** "X-Ray: ..." appears under the main button next stream. **MrShark:** +1 Subs per viewer. **Manim:** the streak bonus appears in the log after successful streams. **Soft Hard Body Man:** "Soft landing" banks half when swatted. **Bro Jogan:** starts at Sub Cap +1. |
| 14 | Settings > Display & Language: try each Resolution in Windowed and Fullscreen (in a build; the editor's Game view controls the size there). | Apply shows "Keep these settings? (10)". Letting it run out reverts. F11 and Alt+Enter toggle fullscreen. |
| 15 | Game view sizes: 1280x720, 1920x1080, 1920x1200, 2560x1080, 3440x1440, 5120x1440 | Nothing overlaps. From 21:9 up, the UI stops widening and you get side margins. The chat shows more columns on wide screens. |
| 16 | Switch Language to each one, then Pseudo | Every screen follows: menus, HUD, cards, tooltips, rules (How to Play), the select screen and the dialogs. Chinese text renders (no squares). |

---

## 6. Multiplayer test session (two players, about 45 minutes)

Start with **Multiplayer Play Mode** (Window > Multiplayer > Multiplayer Play Mode, tick Player 2), then repeat with your friend on a new build.

| # | Do this | What should happen |
|---|---|---|
| 1 | Both: **Multiplayer**. Each picks a different streamer, then host / join. | The lobby shows each player's streamer portrait or initials next to their name. **Streamer: X (change)** reopens the select screen and comes back to the lobby. |
| 2 | Host: set **Turn Timer** to Hard (30 s), then try **Custom** with 45 | The client sees "Turn timer: ...". Custom accepts 10 to 600 seconds. |
| 3 | Start a **Take Turns** season | "YOUR TURN" / "<name>'S TURN" banners. The turn timer pill counts down and turns urgent in the last 10 seconds. |
| 4 | Active player: hover cards, shop rows and buttons slowly | The watcher sees an outline and name tag in the active player's colour on the same thing, plus their mouse pointer (ghost cursor). The Players panel line under the active player says things like "eyeing Doge", "browsing the shop" or "about to end the stream". Expect up to about 1 s of lag; that's by design. |
| 5 | Active player: use a targeted action (Karen, Trollface) | The watcher sees the arrow from the acting viewer to the hovered target, and the picked targets highlighted, before it resolves. |
| 6 | Active player: buy viewers | The watcher sees the purchase flourish and a callout. |
| 7 | Both: click emotes (the grid left of the facecam) and chat (the **Chat** tab) | Emotes float up on both screens with the sender's name, with a 1.2 s cooldown. A message sent while the other player is on Activity puts a count on their Chat tab. |
| 8 | Let the timer run out on purpose | "<name> ran out of time!" The waiting viewer is denied, the stream ends and scores normally, and the turn passes. |
| 9 | Player with **Fate**: in the shop, **Use** Raid | With one rival it hits them straight away; with several, a picker asks which. The target sees "RAIDED!" and "<name> used Raid on you!". Their next stream starts with +1 risk, and their tooltip shows "From a rival's raid: +1". |
| 10 | Player with **Ethical F**kboi**: Wealth Tax | It takes up to 2 $ from the richest rival. Both see "TAXED!" / "<name> collected a Wealth Tax from <name>!". |
| 11 | Switch the client to Russian, the host to Spanish | Each sees their own language, including headlines, rejections and the Stream Log for new lines. |
| 12 | Repeat 1 to 3 in **Synchronized Nights** and **Race** | Presence shows on whichever board you're watching. The turn timer only applies to Take Turns. |

---

## 7. Decisions worth a look

| Decision | Why | Where to change it |
|---|---|---|
| Rival powers (Raid, Wealth Tax) do nothing in single player | There's no opponent's state to affect. The select screen warns you. | Give them a single-player effect in `PartyEngine.UsePower` (for example, Raid could add heat to your own next stream for a bonus). |
| Raid heat is capped at +2 | A raid can never swat someone on its own. | `StreamerCatalog.MaxRaidHeat` |
| The turn timer covers the whole turn (stream and shop), and Hard is 30 s | That's what the lobby options say. 30 s is very tight for a full turn, so Medium (60 s) is probably the friendliest default. | `TurnTimers.Presets` in `NetTypes.cs` |
| On a timeout the stream is **scored**, not wasted | A slow player shouldn't lose the night, only the rest of their turn. | `DeckPartyNetSession.ServerTurnTimedOut` |
| X-Ray has no cooldown but works once per turn | It's cheap information, and the price still escalates. | `StreamerCatalog` power table |
| Language changes don't rewrite old Stream Log lines | Lines are stored as text when they happen. | `EventText` (would need lines stored as keys) |

---

## 8. If something's off

| Problem | Fix |
|---|---|
| Compile errors mentioning `UnityEngine.Localization` or `MoreMountains` | Packages are still importing. Wait for it to finish. If it persists, check **Window > Package Manager** shows Localization 1.5.8, and that `Assets/Feel` exists. |
| Text is English in every language | Run **Deck Party > Localization > Import CSV into String Tables**. The game falls back to the CSV in `Resources/Localization` anyway, so check the Console for an import error. |
| Chinese shows squares | **Deck Party > Localization > Rebuild Fonts**. |
| Card portraits are blank (only the frame and name show) | The card material has Halftone switched on. Run **Deck Party > Setup > Build Everything**, which switches it off. Or select `FX/Materials/PortraitUber.mat` and untick **Enable Halftone**. |
| Cards have no status effects | Sprite Shaders Ultimate is missing or the uber material wasn't created. Run **Deck Party > FX > Rebuild FX (overwrites)**. |
| No Feel punches (only simple tweens) | Feel didn't compile. The Console line "Feel isn't installed" says so. Check `Assets/Feel` is present, then rebuild FX. |
| Animated character shows nothing | Run **Deck Party > Art > Validate Character Animations**. Also check the layer **CardPortraits** exists (Edit > Project Settings > Tags and Layers) and that the database's **Portrait Stage** is set. |
| A scene looks broken after editing | **Deck Party > Setup > Rebuild Scenes and UI Prefabs (overwrites)**. |
