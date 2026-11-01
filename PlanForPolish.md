# Audience Capture: Polish Plan

This is a draft for you to review. Nothing in the project has changed yet. Answer the questions in section 14, or just accept the defaults. You can also tick the nice-to-have boxes in sections 6.5 and 6.6. Then I'll implement everything in the order set out in section 11.

> Note: your file mentioned a link to the Feel documentation, but no link was included. I planned from the installed package (Feel v5.9.1) and the public docs at feel-docs.moremountains.com, so nothing is blocked.

## Contents

1. [Summary](#1-summary)
2. [What I found in the project](#2-what-i-found-in-the-project)
3. [Theme pass: names, title and leftover party wording](#3-theme-pass-names-title-and-leftover-party-wording)
4. [Localization](#4-localization)
5. [Animated characters](#5-animated-characters)
6. [Feel and Sprite Shaders Ultimate: polish](#6-feel-and-sprite-shaders-ultimate-polish)
7. [Multiplayer: the active and the passive player](#7-multiplayer-the-active-and-the-passive-player)
8. [Bigger character art](#8-bigger-character-art)
9. [Resolution, fullscreen and aspect ratios](#9-resolution-fullscreen-and-aspect-ratios)
10. [UI Updates Guide (outline)](#10-ui-updates-guide-outline)
11. [Build order](#11-build-order)
12. [Risks](#12-risks)
13. [What stays the same, and version keys](#13-what-stays-the-same-and-version-keys)
14. [Questions](#14-questions)

---

## 1. Summary

| You asked for | What it becomes |
|---|---|
| UI Updates Guide | `UIGuide.md`: a short "I want to change X, so I open Y" reference. It's written last so it describes the finished system (outline in section 10). |
| Localization | Unity Localization package. Five languages: English, Simplified Chinese, Russian, Latin-American Spanish and Brazilian Portuguese, all translated in the same tongue-in-cheek tone. Fonts that cover all five. A language picker in Settings and the in-game menu. A CSV with every string in every language that you can edit and re-import. |
| Card animations | The Game Database gets an optional **Character Prefab** and **Animator Controller** for each character, and the sprite stays as the fallback. The five states you described play automatically and look the same on every player's screen. There are also tools to generate the template controller and to check your animations. |
| Feel and Sprite Shaders Ultimate | A "juice layer" that `Deck Party > Setup` generates and wires up automatically, so you build nothing by hand. Items are split into **Core** polish and a menu of **Nice-to-have** items for you to tick. |
| Name changes | Party becomes stream and guest becomes viewer. The Rolodex is now **Audience**, the door becomes **Add Viewer to Chat**, and End Party becomes **End the Stream**. The title is **Audience Capture**. A full list of leftover wording is in section 3 for review. |
| Bigger art | Card portraits about 20% bigger (about 45% more area), and bigger shop portraits. The chat area scrolls instead of shrinking cards to tiny sizes. |
| Resolutions | Windowed or fullscreen, with 16:9, 16:10 and ultrawide resolutions, in both Settings and the in-game menu. The layout adapts to the aspect ratio with small adjustments. |
| Active and passive player | Everything above plays on the watchers' screens too. A new set of "presence signals" lets watchers see what the active player is pointing at, choosing and targeting before they commit (section 7). |

Three technical decisions shape most of the plan:

1. **Rigged characters need a "portrait stage".** Your `Doge_Test` is bone-rigged, and rigged sprites can't be drawn inside Unity's UI. Animated characters are therefore rendered off-screen into a shared texture that the cards display (section 5.3).
2. **All Feel and Sprite Shaders Ultimate (SSU) work lives in a separate assembly that the setup tool builds.** If either package is missing, for example on a fresh clone of a public repo, the game still compiles and falls back to today's effects.
3. **Every player-facing string moves into string tables, including the 59 character names and descriptions.** Multiplayer messages are sent as message keys, so each player reads them in their own language, and a lobby can mix languages.

**Order:** the renames come first, so we translate the final text once. Then display and layout, localization, animation, juice, multiplayer presence, and finally the docs.

---

## 2. What I found in the project

Each finding below changes how something has to be built.

1. **`Doge_Test.psb` is bone-rigged** (PSD Importer plus 2D Animation). `Doge_Test.anim` rotates and moves `bone_1/bone_2` and `bone_1/bone_5`. Rigged sprites are SpriteRenderer and SpriteSkin objects, which can't render inside a UI Canvas. An Animator Controller on its own also can't animate a rig, because the bones come from the character prefab. This is why the database needs a prefab field as well as the controller field you asked for (section 5).
2. **Feel compiles into an assembly called `MoreMountains.Tools`** through its `.asmref` files, so our code can reference it cleanly.
3. **SSU has no assembly definition**, so its scripts land in Unity's default assembly, which our code can't reference. We don't need them: we only use SSU's **shaders**. The one helper we would want (`ImageSSU`) only sets two size properties, and I'll reimplement that.
4. **SSU effects are compiled as `shader_feature` keywords**: 114 of them in the UI shader. A build only includes effect combinations used by *saved material assets*, so effects switched on from code would silently disappear in your build. The setup tool will therefore create the material assets. Every SSU effect has its own "Fade" property, which allows one "uber" material per card that fades each effect in and out by number.
5. **All three canvases are Screen Space Overlay.** URP post-processing (bloom, vignette, chromatic aberration) doesn't affect overlay UI. Shakes will move a UI root object instead of the camera. Post-processing on the UI is offered as a nice-to-have (N2) because it means switching canvas modes.
6. **The host's turn timer already uses real time**, not game time. I'll still avoid Feel's slow-motion and freeze-frame feedbacks, because they change `Time.timeScale` and would make players' screens drift apart in pacing. Every Feel feedback will run on unscaled time.
7. **The text is large.** There are about 1,400 string literals across the rules code, runtime and scene builders, plus 59 character names, descriptions and action names. Multiplayer headlines (for example "X joined the party") and rejection reasons are sent across the network as finished English sentences, so they have to become message keys.
8. **The Game Database matches characters by English name.** `EnsureAllGuests` clears a portrait if the stored name no longer matches. Renaming or translating characters would wipe the **23 portraits you've already assigned**. I'll re-key the entries by character id and migrate them automatically.
9. **Static captions are baked into the scenes** by the builders. Some are re-applied at runtime by object path in case you've edited the scenes. This affects how text gets localized (section 4.6) and question 28.
10. **The chat area always uses 9 columns** and shrinks cards to fit up to 34 slots, so at a high Sub Cap the cards get very small. The column count is purely visual (the rules never use it), so it's safe to adapt it to wider screens.
11. **Display settings:** the game starts in a resizable 1600x900 window. The main-menu Settings screen has a fullscreen toggle, but the in-game menu has none. The UI scales from a 1920x1080 reference using "Expand", so 16:10 gains height and ultrawide gains width, which stretches the board.
12. **The product name "Deck Party"** sets the window title and the exe name. It also decides where Unity stores your saves and settings, so renaming it needs care (question 4).
13. **Feel and SSU are paid Asset Store packages** and aren't in git yet. If your GitHub repo is public, they mustn't be pushed, because the Asset Store licence forbids redistribution (question 22).
14. **Feel's demo content is large**: 891 files and about 330 MB of the package's 410 MB. It slows down imports and script compiles and can be removed safely (question 21).
15. **Bugs I spotted:**
    - Stonks Guy's option reads "Invest 2 Subs" but it actually costs 2 Gifted Subs.
    - The How to Play tab says "All 46 Guests" when there are 59.
    - The main menu shows a hard-coded "v1.0" instead of the build version.

---

## 3. Theme pass: names, title and leftover party wording

### 3.1 Proposed glossary

These terms drive hundreds of strings, so please check this table first.

| Current | Proposed | Notes |
|---|---|---|
| Party (the live half of a night) | **Stream** | "tonight's party" becomes "tonight's stream" |
| Guest / guests | **Viewer / viewers** | The cards. Diamonds stay "Diamond" as a tag. |
| House (where guests are) | **Chat** | "in the house" becomes "in chat" (the Moderator already says "remove from chat") |
| Waiting outside / the waiting line | **The Queue** | "12 waiting outside" becomes "12 in the queue" |
| Open the Door | **Add Viewer to Chat** | You asked for this one |
| Waiting at the door... | **A viewer wants to join...** | |
| Admit / Let them in / Reject | **Approve / Deny** | Matches chat moderation wording |
| Evict / send back outside | **Kick / send back to the queue** | |
| Turned away | **Denied** | |
| End Party / End Party & WIN! | **End the Stream / End the Stream & WIN!** | You asked for this one |
| Party Log | **Stream Log** | |
| Rolodex / Squad | **Audience** | You asked for this one ("View Audience", "Audience [R]") |
| Venue | **Stream Setup** | "Your venue (just for you)" becomes "Your setup (only you see it)" |
| Throw a 4-Diamond party | **Pull off a 4-Diamond stream** | |
| Party success! | **Great stream!** | |
| Party shut down (Sub Cap exceeded) | **Stream crashed!** | Fits "too many viewers" |
| Online party / Party Lobby / Close Party | **Online game / Lobby / Close Lobby** | Optional alternative: "Host a Collab" |
| Night / Season | *(unchanged)* | Fits "a night of streaming" (question 2) |
| Subs / Gifted Subs / Diamonds / Sub Cap / Swatting Risk / Temporary Ban | *(unchanged)* | Already on theme |
| Cash ($) | *(unchanged)* | Optional: "Donations ($)" (question 3) |

### 3.2 Specific strings I'll change

| Where | Current | Proposed |
|---|---|---|
| Title screen | DECK PARTY | **AUDIENCE CAPTURE** |
| Title tagline | Go live, build your Squad and get 4 Diamonds to one party. | Go live, grow your Audience and get 4 Diamonds into one stream. |
| Chat area header | The Party · 5/8 Sub Cap · 12 waiting outside | Live Chat · 5/8 Sub Cap · 12 in the queue |
| Chat area hints | End the party now to win! / Use actions or end the party. | End the stream now to win! / Use actions or end the stream. |
| Stream controls | Open the Door / Waiting at the door... / Admit / Let them in | Add Viewer to Chat / A viewer wants to join... / Approve |
| Stream controls | End Party / End Party & WIN! | End the Stream / End the Stream & WIN! |
| Confirm dialog | End an empty party? | End an empty stream? |
| HUD button | Squad [R] | Audience [R] |
| Shop text | Spend Gifted Subs to add guests to your Squad for every future party. | Spend Gifted Subs to add viewers to your Audience for every future stream. |
| Result panel | Party success! | Great stream! |
| Log lines | X joins the party / is sent back outside / joins your Squad / Party wraps up... | X joins the chat / is sent back to the queue / joins your Audience / Stream ends... |
| Swatted text | The SWAT team ends the party. | The SWAT team ends the stream. |
| Overload text | Party shut down. | Stream crashed: too many viewers. |
| Temporary Ban text | sits out the next party | sits out the next stream |
| Watching strip | Look at My Party / Back to My Party / Watching X's party | My Stream / Back to My Stream / Watching X's stream |
| HUD tooltips | At this party / Best ever at one party | This stream / Best ever in one stream |
| Game over | A 4-Diamond party on night N / 4-DIAMOND PARTY! / Successful parties | A 4-Diamond stream on night N / 4-DIAMOND STREAM! / Successful streams |
| Multiplayer menu | Host Online Party / or join a friend's party | Host Online Game / or join a friend's game |
| Lobby | Your Party Lobby / Party Lobby / Close the party? / Close Party | Your Lobby / Lobby / Close the lobby? / Close Lobby |
| Network messages | joined/left the party, already in a party, No party found..., That party is full/has started, the host closed the party, same build of Deck Party | joined/left the game, ..., same build of Audience Capture |
| Headlines | X threw a 4-Diamond party and wins! / had 3 Diamonds at one party | X pulled off a 4-Diamond stream and wins! / had 3 Diamonds in one stream |
| Single player | Pick a character set and a venue / (upgrades as your house grows) | Pick a character set and a stream setup / (upgrades as your Sub Cap grows) |
| Rules engine errors | Decide on the guest at the door first. / Nobody is left waiting outside. / The party is over. / That guest isn't at the party. / You can only recruit between parties. | Decide on the waiting viewer first. / Nobody is left in the queue. / The stream is over. / That viewer isn't in chat. / You can only recruit between streams. |
| Character set "Open Lobby" | Everyone's invited (sandbox) | Everyone's welcome (sandbox) |
| How to Play | All 46 Guests (bug) | All 59 Viewers |
| Controls help | Space open the door ... E end the party ... R Squad | Space add viewer ... E end the stream ... R Audience |
| Rules text (How to Play and in-game) | "Throw one party...", "A night has two halves: 1. The party", "open the door"... | Rewritten in full with the new glossary |

### 3.3 Character descriptions

Thirteen descriptions mention "party", for example Pepe's "until the party ends" and Forever Alone's "when the party ends". Almost all 59 say "guest". They'll be rewritten with the glossary. Ability names such as "Reset the Stream" are already on theme and stay. The Stonks option label is fixed to "Invest 2 Gifted Subs".

### 3.4 Leftover audio and art

| Leftover | Change |
|---|---|
| The door-knock sound when a viewer arrives (`Sfx.DoorKnock`) | Becomes a chat "join" ping |
| The Sub Cap icon is a door (`SkinSlot.IconDoor`) | The placeholder becomes a chat bubble or people icon. The slot keeps its name, so a sprite you assign later still works. |
| Stream setup option "Animated party lights" | The tooltip says "Stream lighting (RGB)" |
| Setup dialog "Let's party" (editor only) | "Let's go live" |
| Main menu "v1.0" | Shows the real build version |

### 3.5 What I won't rename

- Internal code names (PartyEngine, HouseView, `GamePhase.Party` and so on).
- The Unity project, folders, assembly names and the editor's `Deck Party` menu.
- Save and settings keys.
- The product name or exe name, for now (question 4).

---

## 4. Localization

### 4.1 Languages

On Steam's monthly survey of player languages, English and Simplified Chinese are by far the largest, followed by Russian, Spanish and Brazilian Portuguese. German, Japanese, French and Korean come next. My five:

| Language | Locale | Native name in the picker |
|---|---|---|
| English (source) | `en` | English |
| Simplified Chinese | `zh-Hans` | 简体中文 |
| Russian | `ru` | Русский |
| Spanish (Latin America) | `es-419` | Español (Latinoamérica) |
| Portuguese (Brazil) | `pt-BR` | Português (Brasil) |

Question 14 covers whether "5" includes English, and which Spanish to use.

### 4.2 Package and settings

- Install `com.unity.localization` 1.5.x, the latest version compatible with Unity 6.0, pinned at install. It brings **Addressables** with it.
- Turn on "build Addressables with the player", so your build steps in V1Instructions 6B stay the same apart from one note.
- `Assets/DeckParty/Localization/` will hold the Localization Settings, the five locales and the string tables. The setup tool generates all of it.
- Language choice order: your saved choice, then your Windows language if it's supported, then English.

### 4.3 String tables

| Table | Contents | About |
|---|---|---|
| UI | menus, buttons, captions, settings, lobby | 220 entries |
| Gameplay | glossary terms, HUD, Stream Log lines, banners, toasts, tooltips, errors, rules text | 300 |
| Characters | 59 names, descriptions, action names and action options | 200 |
| Content | character sets and stream setups | 30 |
| Multiplayer | headlines, rejections, connection errors | 50 |

That's about 800 strings, each in 4 translated languages. Keys are readable, for example `char.doge.name`, `log.viewer_joined` and `ui.menu.single_player`.

### 4.4 How the code changes

- A small `Loc` helper replaces the hard-coded strings, for example `Loc.Get("log.viewer_joined", ("viewer", nameTag))`.
- **Strings are whole sentences with named placeholders**, because word order differs between languages. Plurals use each language's own rules through Smart Strings (Russian has three plural forms).
- **Colours and bold are applied in code** to the inserted values, so translators rarely touch rich-text tags.
- **Code won't uppercase text** (`ToUpperInvariant`). Text that should be capitals uses TextMeshPro's uppercase style.
- **`Terms.cs` becomes a glossary lookup** for standalone labels like HUD captions. Inside sentences, each language writes its own term so the grammar works (Russian changes word endings by case).
- **The rules engine keeps no player-facing text.** Names and descriptions move to the Characters table, keyed by character id. Rejection reasons become keys.
- **The Game Database is keyed by character id** with an automatic migration, so your 23 portraits stay. The unused "display name override" field goes away; you rename through the CSV instead.
- **The Stream Log keeps the events, not finished text**, so switching language re-renders lines you've already seen.

### 4.5 Multiplayer

- The host sends `{key, values}`, where player names are raw text and characters are sent as ids. Each player's game turns that into their own language.
- Chat messages are passed through untouched.
- This changes the network protocol, so it moves to `deckparty-4` (section 13).

### 4.6 Text in the scenes

- A `LocalizedLabel` component goes on every static caption. Fresh scene builds add it automatically.
- `Deck Party > Localization > Localize Existing Scenes` adds it to your current scenes by object path, without rebuilding them, so your scene edits are kept.

### 4.7 Fonts and layout

**Font stack.** All four are on Google Fonts under the open font licence, which allows commercial use.

| Use | Primary font | Fallback |
|---|---|---|
| Body | Noto Sans (Latin and Cyrillic) | Noto Sans SC (Chinese) |
| Titles | Rubik Bold/Black, a playful font with Latin and Cyrillic | ZCOOL KuaiLe, a playful Chinese display font |

**How the fonts are set up:**
- TextMeshPro font assets are generated by the setup tool and plugged into the UI Skin's `bodyFont` and `headerFont` slots.
- Chinese characters used in the tables are pre-baked into the atlas, so they're crisp and fast. A dynamic fallback covers chat messages and player names.

**Text length:** Spanish, Portuguese and Russian run 20–35% longer than English.
- Every label and button gets auto-sizing with sensible minimums and flexible widths.
- Text is cut off with "..." only as a last resort.
- Chinese gets proper line-breaking rules.

**Pseudo-language for testing:** an "English (Pseudo)" option makes every string 40% longer with accented letters. It's available in the editor and development builds, so you can find overflowing text without reading other languages.

**Build size:** about +10–20 MB, mostly the Chinese font.

### 4.8 Language picker

- It goes in Settings and in the in-game Menu, and switches instantly without a restart.
- Mixed-language multiplayer lobbies work, because each player's game localizes on its own.

### 4.9 Translation style: "transcreation"

As you asked, I'll adapt the jokes rather than translate them word for word. Where a meme has a famous local name, I'll use it. Here are samples to calibrate the tone (question 15). The final text is written during implementation.

**Core terms**

| English | 简体中文 | Русский | Español (LatAm) | Português (BR) |
|---|---|---|---|---|
| Audience Capture (title) | 流量密码 *("the viral cheat code")* | Захват аудитории | Captura de Audiencia | Captura de Audiência |
| Stream | 直播 | Стрим | Stream | Live |
| Viewer | 观众 | Зритель | Espectador | Espectador |
| Chat | 直播间 *("stream room")* | Чат | Chat | Chat |
| Add Viewer to Chat | 拉人进直播间 | Пустить зрителя в чат | Agregar espectador al chat | Adicionar espectador ao chat |
| End the Stream | 下播 *(streamer slang for going offline)* | Закончить стрим | Terminar stream | Encerrar a live |
| Audience | 粉丝团 *("fan club")* | Аудитория | Audiencia | Audiência |
| Gifted Subs | 礼物订阅 | Подарочные сабы | Subs regaladas | Subs de presente |
| Swatting Risk | 封号风险 *("account-ban risk", the Chinese equivalent of the threat)* | Риск сватинга | Riesgo de swatting | Risco de swatting |
| SWATTED! | 封号了！ | СВАТНУЛИ! | ¡TE SWATEARON! | TOMOU SWATTING! |
| Temporary Ban | 禁言 *("muted")* | Временный бан | Baneo temporal | Ban temporário |
| SUB CAP EXCEEDED! | 直播间爆满！ *("room's packed!")* | ЛИМИТ ПРЕВЫШЕН! | ¡LÍMITE SUPERADO! | LIMITE ESTOURADO! |

**Character names**

| English | 简体中文 | Русский | Español (LatAm) | Português (BR) |
|---|---|---|---|---|
| Loyal Fan | 铁粉 *("iron fan", real slang)* | Верный фанат | Fan incondicional | Fã de carteirinha *(idiom: card-carrying fan)* |
| Whale | 土豪 *(gamer slang for big spenders)* | Кит-донатер | Ballena | Baleia |
| Bot Farm | 水军 *("water army": paid fake accounts)* | Ботоферма | Granja de bots | Fazenda de bots |
| Moderator | 房管 *(stream-room admin)* | Модер | Moderador | Moderador |
| Doge | 神烦狗 *(its actual Chinese meme name)* | Доге | Doge | Doge |
| Pepe | 悲伤蛙 *("Sad Frog", its Chinese meme name)* | Пепе | Pepe la Rana | Sapo Pepe |
| Karen | 凯伦大妈 *("Auntie Karen")* | Яжмать *(the Russian internet's own Karen)* | Señora Karen | Karen |
| Skibidi Toilet | Skibidi马桶人 | Скибиди Туалет | Skibidi Toilet | Skibidi Toilet |
| Tung Tung Tung Sahur | *(unchanged everywhere: brainrot names are global)* | | | |

### 4.10 The CSV

- **File:** `Assets/DeckParty/Localization/AudienceCapture_Strings.csv`. It's saved as UTF-8 with a byte-order mark, so Excel shows Chinese and Cyrillic correctly.
- **Columns:** Table · Key · Context (where it appears, max length, placeholders) · English · 简体中文 (Chinese, Simplified) · Русский (Russian) · Español (Latinoamérica) · Português (Brasil) · Status (draft/reviewed).
- **Round trip:** `Deck Party > Localization > Export CSV` and `Import CSV`. You or a translator can edit in Excel or Google Sheets and re-import.

### 4.11 Caveat

I'll write the translations myself. They'll be good enough for playtesting and in the right spirit. Before a public release, have native speakers review them, especially the jokes. The Status column tracks what's been reviewed.

---

## 5. Animated characters

### 5.1 New fields in the Game Database

These go in the inspector you open with `Deck Party > Select Game Database`.

| Field | What it's for |
|---|---|
| Portrait (Sprite) | Existing. Still the fallback. |
| **Character Prefab** (new) | Your animated character: a rigged PSB prefab, or a prefab using sprite-swap animation. |
| **Animator Controller** (new) | Drives the prefab and overrides the prefab's own controller. With no prefab, it animates the card's portrait image directly (sprite-swap clips only). |
| **Framing** (new) | Fits the character to the card automatically, plus manual zoom and offset. |
| **Preview** (new) | A small live preview, plus a checklist showing which of the required states the controller has. |

The inspector also gets search, a "missing art" filter, a "has action" filter and a compact row per character.

### 5.2 Which source wins

The game uses the first of these that's set:

1. Prefab, with the optional controller override.
2. Controller only, animating the card image.
3. Sprite.
4. The coloured placeholder.

A character with nothing new assigned looks exactly as it does today.

### 5.3 Rendering rigged characters: the portrait stage

```
 Card (UI)                         Portrait stage (hidden layer "CardPortraits")
 ┌────────────┐                    ┌──────────────────────────────────────┐
 │ RawImage ◄─┼── its cell of ─────┤ One render texture, ONE camera       │
 │ + SSU FX   │   the texture      │ [Doge][Pepe][Karen][Doge][ ... ]     │
 └────────────┘                    │ each cell = one prefab instance      │
                                   │ with its own Animator                │
                                   └──────────────────────────────────────┘
```

- Every visible animated card gets a cell. Its prefab instance animates there, and the card shows that cell.
- The approach works for rigged and frame-by-frame characters, and scroll-view clipping still works.
- SSU effects (glow, outline, dissolve and so on) apply on top exactly as they do for static sprites.
- A global 2D light makes "lit" PSB sprites render the same as in your authoring scene.
- Cell resolution follows the screen: 256 px up to 1440p and 512 px at 4K.
- Off-screen cards pause their animation and free their cell. If the texture fills up, the card falls back to the static sprite.

### 5.4 States and when they play

| State | Needed for | Plays when |
|---|---|---|
| `Idle` | Viewers without an action (36 characters) | In chat, the shop, the Audience view and pickers |
| `Idle_Pre_Active` | Viewers with an action (23 characters) | In chat before the action is used. Also in the shop, Audience and pickers. |
| `Active` | Viewers with an action | Once, when the action is used. It then moves to `Idle_Post_Active` automatically. |
| `Idle_Post_Active` | Viewers with an action | In chat after the action has been used tonight |
| `Score` | Everyone | During the new end-of-stream scoring sequence (C13), on successful streams |

**What happens when a state is missing:**
- `Idle_Pre_Active` and `Idle_Post_Active` fall back to `Idle`, then to the controller's default state.
- A missing `Active` or `Score` just gets a Feel punch.

**Other rules:**
- Rickrolled viewers use the art of the character they're disguised as.
- Question 5 covers optional extra states (joined, kicked, swatted, celebrate).

### 5.5 Tools

- **`Deck Party > Art > Create Character Animator Template`** generates `AC_CharacterTemplate` with the five states. It already contains the `Active → Idle_Post_Active` transition, so you only make clips.
- **`Create Override Controller for Selected Character`** creates an override controller from the template. You drop your clips into its slots.
- **`Validate Character Animations`** reports, per character:
  - states missing for that character type (with or without an action);
  - a prefab with no controller;
  - clips whose paths don't match the prefab's bones.

### 5.6 Multiplayer

- States are worked out from the shared game state, such as who's in chat and which abilities have been used, plus the events in each update. Every screen shows the same state, and watchers see the `Active` animation as it happens.
- A watcher who switches boards mid-turn sees the correct idle states straight away.

### 5.7 Performance

One extra camera renders one texture. Only visible cards animate. Characters without new art cost nothing extra.

---

## 6. Feel and Sprite Shaders Ultimate: polish

### 6.1 How it's built

```
 Game events (from the shared snapshot, identical on every screen)
   │
   ▼
 GameController / views ──► Fx.Play(FxCue.ViewerJoined, card, details)
                                  │
                 ┌────────────────┴───────────────────┐
       Feel + SSU installed                      not installed
       DeckParty.Juice assembly:                 built-in fallback
       ├ FxDirector                              (today's tweens,
       ├ FxLibrary.asset  (cue → feedback,       shake, flash and
       │   colours, intensities, on/off)         confetti)
       ├ FX/Feedbacks/*.prefab  (Feel players)
       ├ Feel players inside the card prefab
       └ FX/Materials/*_Uber.mat  (SSU)
```

**What the setup tool does:**
- `Build Everything` generates every asset above.
- It never overwrites assets you've tweaked: it only creates missing ones.
- A new `Rebuild FX (overwrites)` menu item resets them.

**Separate assembly:**
- The juice layer is its own assembly, with a symbol the setup tool sets automatically when it detects Feel.
- If Feel is missing, the game still compiles and uses the fallback.

**How tweaking works:**
- To change a moment, open its feedback prefab or the `FxLibrary` asset and use Feel's own inspector: durations, curves, intensities and colours.
- The UI Guide will list which prefab drives which moment.

### 6.2 Rules for every effect

- Everything runs on **unscaled time**. Nothing changes `Time.timeScale`.
- **Fast Animations** (the existing setting) scales every Feel duration.
- **New accessibility settings:**
  - a **Screen shake** strength slider (0–100%);
  - a **Reduce flashing** toggle, which tones down flashes, strobes and glitches.
- **Watchers get the same effects** because effects come from the shared events. Optionally they can have reduced shake (P8).
- Effects never block input for more than the length of a short animation, and the long sequences can be skipped.

### 6.3 Core polish (recommended)

| # | Moment | What you'll see and hear | Tools |
|---|---|---|---|
| C1 | **Viewer joins chat** | The card slides from the queue to its slot with a springy overshoot, then squashes on landing. It glows into existence. A chat "ping" plays with slightly varied pitch. | Feel position, scale spring and squash-and-stretch; SSU glow dissolve |
| C2 | **Diamond joins** | C1, plus a gold shine sweep, a sparkle burst, a deep hit sound and a "DIAMOND!" banner. The HUD diamond gem lights up with a punch. | SSU Shine; Feel scale; text reveal |
| C3 | **Hover and press** | Cards lift on a spring and gain an outline. Buttons squash and spring back. Clicking a disabled button gives a short "no" shake and tick (this replaces today's ButtonFeedback). | Feel springs; position shake |
| C4 | **Ability ready and used** | An unused ability button "breathes" and the portrait glows soft purple. On use: a wind-up squash, then the `Active` animation, then the impact on the target(s). | Feel wiggle; SSU glow; flicker |
| C5 | **Action-type impacts** (6) | Recruit: the target streaks in from the queue. Remove: the target shakes, flashes red, burns out and flies to the queue. Modify: a colour pulse and number pop. Shuffle: cards swap along arcs. Scout: peeked cards flip up with a shimmer. Economy: coins or subs fly to the HUD. | Feel and SSU |
| C6 | **Targeting** | Valid targets pulse an outline and invalid ones grey out. A curved arrow runs from the acting card to the cursor, and watchers see it too (P3). | SSU outline and saturation; UI arrow |
| C7 | **Counters** | Subs, Gifted Subs and Cash count up, punch and flash. "+N" chips fly from the card to the counter. | Feel TMP count-to, scale, colour |
| C8 | **Swatting Risk tension** | Pips fill with a punch. At 2 the meter pulses red, the screen edges get a soft red vignette and a heartbeat plays. Cards carrying risk get a faint red rim. | Feel; SSU outline |
| C9 | **Sub Cap meter** | An occupancy bar with a delayed "catch-up" fill. It shakes when full. | Feel progress bar |
| C10 | **Diamond tracker** | Gems light up. At 3: a "ONE MORE!" pulse. At 4: the End the Stream button glows gold with a shine sweep. | Feel; SSU Shine |
| C11 | **SWATTED!** | A brief hit-stop (about 80 ms pause of UI animations only), a strong shake, the red/blue police strobe and a glitch on every card. The banner slams in and a siren plays. Cards then go grey. | Feel shake; SSU Glitch and saturation |
| C12 | **Sub Cap exceeded: "stream crashed"** | Cards squeeze and vibrate, the board pixelates and glitches, a buffering spinner appears, then the banner. | SSU Squeeze, Vibrate, Pixelate, Glitch |
| C13 | **End-of-stream scoring sequence** | Each viewer, in arrival order, plays `Score`. A "+N" flies to the total, the pitch rises with each viewer and the total counts up. Then "GREAT STREAM!". Click or Space to skip. About 0.25 s per viewer, capped at about 3 s. | Feel sequencing |
| C14 | **Victory and defeat** | Victory: confetti, sparkle bursts, diamonds shine and a fanfare. Defeat: the board desaturates and slowly fades. | Feel; SSU |
| C15 | **Turn and board switch** | A turn banner in the player's colour. When the view changes board, the old board slides out and the new one slides in, so watchers always know whose stream they're watching. | Feel position and canvas group |
| C16 | **Banners, toasts and menus** | Banner text types in and slams. Toasts slide in on a spring. Menu screens stagger in. | Feel TMP text reveal, springs |
| C17 | **Status visuals** (table 6.4) | Ongoing effects show every viewer's condition at a glance, for both players. | SSU |
| C18 | **Settings** | The shake slider, reduce flashing, and Fast Animations scaling Feel. | — |

### 6.4 Status visuals (part of C17)

These suit open information well: a watcher can read the board without hovering.

| Viewer condition | Effect |
|---|---|
| Action not used yet | Soft purple glow |
| Carries Swatting Risk / risk cleared | Faint red rim / the rim turns teal |
| Infected (Skibidi Toilet) | Green "poison" bubbling |
| Immune (Roll Safe) | Ice shimmer |
| Earns nothing (Drakeposting) | Desaturated |
| Doubled (Drakeposting) | Gold shimmer |
| Converted (Confused Math Lady) | Colour shift green ↔ gold |
| Temporary copy (Ugandan Knuckles) | Hologram |
| Rickrolled (disguised) | An occasional glitch flicker |
| Attached (Overly Attached Girlfriend and partner) | Pink pulsing glow |
| Pepe's mood | Happy = gold tint, Sad = blue, Angry = red |
| Diamond | A slow gold shine sweep |
| Temporary Ban (Audience view) | Greyscale halftone |
| Selectable / selected target | Animated gold / pink outline |

### 6.5 Nice-to-have

Tick the ones you want.

- [ ] **N1 Signature effects for every character with an action, plus the arrival specials** (table 6.6).
- [ ] **N2 Post-processing on the UI.** Bloom on gold, chromatic aberration on glitches, a real vignette on danger, a lens punch on SWATTED. This means switching the game canvases to camera mode, which risks slightly softer text and needs testing.
- [ ] **N3 Fake live-chat feed.** A Twitch-style side feed reacting to events with localized meme spam ("KEKW", "F", "W", "L", "POG"). Very on theme.
- [ ] **N4 Stream overlay dressing.** A pulsing "● LIVE" badge, a stream timer and a viewer-count ticker on the HUD.
- [ ] **N5 Spectator emotes.** Watchers send emotes that float up on everyone's screen.
- [ ] **N6 Ghost cursor.** Watchers see the active player's mouse pointer.
- [ ] **N7 Card tilt.** Cards tilt slightly toward the cursor in 3D.
- [ ] **N8 Idle breathing** for static-sprite characters, so un-animated characters feel alive.
- [ ] **N9 Reactive backgrounds.** Lights shift red/blue as risk rises, with ambient haze or wind per stream setup.
- [ ] **N10 Shiny title.** A shine or glitch on the "AUDIENCE CAPTURE" title and an animated menu background.
- [ ] **N11 Audio juice.** Music ducks under big moments and goes muffled while a menu is open.
- [ ] **N12 Musical hover ticks.** Hover sounds rise in pitch across the row of cards.
- [ ] **N13 Comic stamps.** Localized "BONK!", "NOPE.", "U MAD?" stamps.
- [ ] **N14 Link lines.** A heart line between attached partners and Nyan Cat's rainbow trail through a chain.

### 6.6 Signature effects (N1)

Every sound is original or procedural, with no copyrighted songs or clips.

| Character | Effect |
|---|---|
| Social Media Manager | Megaphone burst; the chosen viewer streaks in |
| Moderator | Ban-hammer slam; the target dissolves |
| HODL | Everyone sparkles out with "diamond hands" and rewinds into the queue |
| Trollface | The target spins and bounces off; "U MAD?" stamp |
| Pepe | Mood tint plus an emoji pop |
| Grumpy Cat | Three cards flip up; the rejected one gets a "NO." stamp |
| Stonks Guy | A green arrow and chart line; coin rain on payout |
| Chad vs Virgin | The two cards face off (one grows, one shrinks); the winner glows |
| Keyboard Cat | The target slides off stage to an original jingle |
| Disaster Girl | The target's edges burn; a smirk pop |
| Success Kid | A fist-pump punch; the rerolled card spins |
| Tung Tung Tung Sahur | A heavy BONK impact; strike pips crack |
| Tralalero Tralala | The two cards swap along an arc with a splash |
| Bombardiro Crocodilo | Three bombs drop and explode, with burn-dissolves; replacements drop in |
| Ballerina Cappuccina | The two cards pirouette and swap numbers |
| Karen | A "MANAGER!" stamp; the target burns; coins fly to Karen |
| Rickroll | The target glitches into its new character |
| Drakeposting | The first pick greys out ("nah"); the second glows gold ("yeah") |
| Confused Math Lady | Floating equations and a colour shift |
| Side Eye Chloe | A side-eye squint; the next viewer slides to the back of the queue |
| Roll Safe | A temple tap; the target freezes in ice |
| Platform Admin | A red ban wave sweeps across |
| The Algorithm | A scanline sweep; the summoned viewer zooms in |
| Skibidi Toilet *(arrival)* | The infection spreads as a green ripple with each arrival |
| NPC Wojak *(arrival)* | A grey scan copies the previous viewer |
| Overly Attached Girlfriend *(arrival)* | Pink link line and heart pulse |
| Ugandan Knuckles *(arrival)* | A hologram clone pops into the queue |
| Distracted Boyfriend *(arrival)* | Three cards reveal with a head-turn whip |
| This Is Fine Dog | Fire border, then extinguished |
| Nyan Cat / Bongo Cat / Big Chungus / Doge | Rainbow chain / drum hit on Animal cards / landing thump / floating "wow, such synergy" in a free Comic Sans lookalike |

---

## 7. Multiplayer: the active and the passive player

### 7.1 Already in place

- The host sends every player's state to everyone.
- In Take Turns, every screen follows the active player's board read-only, including banners, sounds and the log.
- Any player can click a name to watch that board.

Everything in sections 5 and 6 is driven by those same shared events, so watchers automatically get the same animations, effects and status visuals as the active player.

### 7.2 New presence signals (core)

These let watchers see what the active player is about to do, not just what they've done:

| # | Signal | What watchers see |
|---|---|---|
| P1 | **Hover intent** | A soft outline in the active player's colour, with their name tag, on whatever card, shop row or button the active player is pointing at |
| P2 | **Mirrored decisions** | When the active player opens a choice, watchers see the same dialog read-only, with the hovered option highlighted and the pick landing. This covers Pepe's mood, Stonks, Success Kid, Drakeposting, Math Lady, Grumpy Cat's inspect, Distracted Boyfriend's swap and the Temporary Ban pick. |
| P3 | **Shared targeting** | The actor-to-target arrow and the selected targets, live |
| P4 | **Live status line** | The Players panel and the strip above the board say what's happening, with typing dots: "choosing Trollface's target…", "browsing the shop…", "about to end the stream…" |
| P5 | **"Watching" indicator** | The active player sees "👁 2 watching", so they feel their audience |

**Optional extras:** P6 spectator emotes (N5), P7 ghost cursor (N6), P8 reduced shake for watchers.

### 7.3 How the signals are sent

- Tiny messages go from the active player to the host to the others, sent "unreliably" (dropping one doesn't matter).
- They're only sent when something changes, at most 10 per second.
- They carry ids only, never text.
- The host checks they come from the active player.
- They never touch the game state, so they can't desync or cheat.
- They're part of the `deckparty-4` protocol.

---

## 8. Bigger character art

| Element | Now | Proposed |
|---|---|---|
| Card (chat, Audience, pickers): design size | 200 × 260 | 200 × 290 |
| Card portrait area | 180 × 148 | 180 × 178: characters about 21% bigger, about 45% more area |
| Shop row height / portrait | 124 / 96 × 96 | 140 / 116 × 116: about 21% bigger |
| Chat area at a high Sub Cap | Cards shrink to fit (tiny at 30+) | Cards stop shrinking at a minimum size and the chat area scrolls |
| Wide screens | Always 9 columns | Up to 11 columns at 21:9, so cards are bigger on ultrawide |

- The sizes are constants in one place so you can tweak them.
- The setup tool regenerates the card and row prefabs, and saves a backup of the old prefabs first.

---

## 9. Resolution, fullscreen and aspect ratios

### 9.1 Settings

A **Display** block is added to Settings and to the in-game Menu (the HUD's Menu button):

- **Mode:** Windowed or Fullscreen. Fullscreen uses borderless fullscreen, which is the most alt-tab friendly.
- **Resolution:** a list grouped by aspect ratio.
- **Language**, **Screen shake** and **Reduce flashing** sit in the same block.
- **Apply** opens a 10-second "Keep these settings?" countdown, so a bad choice reverts by itself.
- **Alt+Enter** and **F11** toggle fullscreen anywhere.

### 9.2 Resolution list

| Group | Resolutions |
|---|---|
| 16:9 | 1280×720, 1366×768, 1600×900, 1920×1080, 2560×1440, 3840×2160 |
| 16:10 | 1280×800, 1440×900, 1680×1050, 1920×1200, 2560×1600 |
| Ultrawide 21:9 | 2560×1080, 3440×1440, 3840×1600 |
| Super-ultrawide 32:9 | 5120×1440 (only if the monitor supports it; question 24) |

Only resolutions that fit your monitor are listed. In windowed mode, sizes bigger than the desktop are hidden.

### 9.3 Layout adaptation

The goal is small adjustments, not separate layouts:

- **Keep** the 1920×1080 reference with "Expand" scaling. It's the right base for 16:9 through 21:9.
- **Content frame (new).** The game UI is limited to at most 21:9 and centred. The background always fills the screen, so on 32:9 the stream setup art shows at the sides.
- **Chat columns** adapt to the width (section 8). On 16:10 the extra height goes to bigger cards and less scrolling.
- **Side panel** width scales gently, from 420 to 520.
- **Top bar** groups flex and their text auto-sizes. This also helps the translations.
- **Overlays** (dialogs, banners, tooltips) stay centred and clamped to the screen.
- **Background art:** the UI Guide will say to author backgrounds at 21:9 and keep the important parts inside the central 16:9.

### 9.4 What you'll test

1280×720, 1920×1080, 1920×1200, 2560×1080 and 3440×1440 (plus 5120×1440 if included), each windowed and fullscreen. The UI Guide shows how to add these to the Game view.

---

## 10. UI Updates Guide (outline)

`UIGuide.md` is written at the end, so it matches what was built. Each section is a quick table: "to change X, open Y, do Z".

1. **Project map.** Scenes, scene builders, UI prefabs, view scripts and data assets, and what `Build Everything` versus `Rebuild` overwrites.
2. **Main menu.** Title text or logo, buttons, screens and background.
3. **HUD.** Top-bar stats and meters, icons (UI Skin), fonts and effects.
4. **Cards and shop rows.** Layout constants, frames, badges and portrait size.
5. **Backgrounds and stream setups.** Stream setup assets, image sizes, the ultrawide safe area, foregrounds and lights.
6. **Character art and animations.** Sprite versus prefab versus controller, state names, the template, the validator and framing.
7. **Text and languages.** Editing keys, the CSV round trip, adding a language, fonts and pseudo-language testing.
8. **Feel.** Which feedback prefab drives which moment, global intensity, tweaking a moment and adding a feedback.
9. **Sprite Shaders Ultimate.** The uber materials, which property drives which status, and adding an effect.
10. **Display and aspect ratios.** The content frame, column rules and testing resolutions.
11. **Troubleshooting.**

---

## 11. Build order

Each milestone leaves the game working. I'll compile-check everything without running tests, and you test in Unity after each milestone.

| # | Milestone | Contents | You test | Size |
|---|---|---|---|---|
| M0 | Housekeeping | Confirm the multiplayer fix; decide the git policy for Asset Store packages; trim the Feel and SSU demos; add the package detection symbols | The project opens cleanly | S |
| M1 | Theme pass | All renames, the title, rules text, the bug fixes, the leftover sound and icon | Read through every screen | M |
| M2 | Display and layout | Resolution and fullscreen UI, content frame, adaptive columns, bigger art, prefab regeneration with backups | The resolution matrix | M |
| M3 | Localization foundation | Package, tables, `Loc` helper, network keys, fonts, picker, CSV tools, English text extracted, pseudo-language | Pseudo-language pass; multiplayer still works | L |
| M4 | Translations | The four languages plus the CSV | Browse each language | L |
| M5 | Animated characters | Database fields, portrait stage, state driver, template and validator tools, Game Database inspector | Your Doge test in every view | M–L |
| M6 | Juice core | Juice layer, generated Feel and SSU assets, C1–C18 | Play a full season | L |
| M7 | Multiplayer presence | P1–P5 | Two players (Multiplayer Play Mode, then your friend) | M |
| M8 | Your picks | Nice-to-haves ticked in 6.5 and 6.6 | — | Varies |
| M9 | Docs | `UIGuide.md`, plus updates to INSTRUCTIONS.md and V1Instructions.md | — | S |

**Why this order:**
- The renames come before localization so the final English is translated only once.
- Layout comes before juice so effects are tuned on final sizes.
- Animation comes before juice because the effects build on the new card structure.
- Presence comes after localization so its status lines are localized.

---

## 12. Risks

| Risk | Mitigation |
|---|---|
| Rigged characters can't render in UI | The portrait stage: one camera and a shared texture (5.3) |
| Semi-transparent edges look dark on render textures | A premultiplied-alpha material on the portrait images, checked with your Doge |
| SSU effects missing from builds | Only saved material assets are used, via the uber materials. I'll add a build-time check. |
| Localization adds Addressables | Addressables build with the player. V1Instructions gets a note. |
| Translation quality and humour | Status column, native-speaker review, easy CSV round trip |
| Text overflow in longer languages | Auto-sizing everywhere plus a pseudo-language pass |
| Your scene edits lost on rebuild | Migration tools by default; a clean rebuild only if you confirm the scenes are untouched (question 28) |
| Your prefab edits lost (card size) | Automatic backup before regenerating |
| Feel or SSU missing on a fresh clone | The separate juice assembly plus automatic detection; the game still runs |
| Asset Store licence on a public repo | Keep Feel and SSU out of git if the repo is public (question 22) |
| Old builds joining new ones | Protocol `deckparty-4`. The version check refuses mismatches with a clear message. |
| Performance | Feel players are pooled per card; one portrait camera; status effects only on visible cards |
| Scope | Each milestone stands on its own. You can stop or cut after any of them. |

---

## 13. What stays the same, and version keys

- **The game rules don't change**, apart from the Stonks label fix.
- **Network protocol:** `deckparty-3` becomes `deckparty-4` at M3. If M7 ships as a separate build, it becomes `deckparty-5`. Both of you must rebuild after each.
- **Unchanged:** StateSerializer `FormatVersion 2` and progress key `dp.progress.v2`. Saved seasons and progress stay valid.
- **Game Database:** character entries are re-keyed by id. The migration is automatic and your portraits are kept.
- **Settings:** existing PlayerPrefs keys are unchanged. New keys are added for language, display and accessibility.

---

## 14. Questions

Each question has a **default** that I'll use if you don't answer it. Questions 1–13 are about game design and mechanics, which you asked me to check with you. The rest are technical or process choices.

### Theme and rules

1. **Is the glossary in section 3.1 OK?** Change any row you like. *Default: as proposed.*

Yes
2. **Keep "Night" and "Season"?** *Default: yes.*

Yes
3. **"Cash": keep it, or rename it "Donations ($)"?** *Default: keep "Cash".*
4. **Should the window title and exe name become "Audience Capture" now?** Renaming the product moves where Unity stores your settings and saves, so they'd appear reset. *Default: keep "Deck Party" for now (the title screen still says Audience Capture) and rename at release.*

No the name changes are only going to superficial in string text for the players and in certain graphics. Keep the repo and everything else under the working title of Deck Party as it has been 

### Animated characters

5. **When should `Score` play?** Only on successful streams, not when swatted or crashed? And should I support optional extra states (`Enter`, `Exit`, `Hit`, `Celebrate`) that play when present? *Default: Score on successful streams only; support the optional states.*

You can support these but at this stage I will mostly be working with single sprites that are not yet animated. I just wanted the functionality for it as it will be the next thing I test and prepare after this. If you think its best we implement the animations later and just use single sprites for now, I am happy to do that. 

6. **In the shop, Audience and pickers, should characters with an action show `Idle_Pre_Active`?** *Default: yes.*

Yes 

7. **Viewers whose effect fires on arrival** (Skibidi Toilet, NPC Wojak, Overly Attached Girlfriend, Ugandan Knuckles, Distracted Boyfriend) **have no action button. Should they use `Idle` only, or play an `Active`-style animation on arrival?** *Default: Idle only, plus an optional `Enter` state if you add one.*

Yes add an optional enter state animation that would play in those cases. But make it that any characters have this optional enter animation state which plays when any of them are added during a round (otherwise it just defaults to the idle when they are added )
8. **Is it OK that each character gets a Character Prefab field next to the Animator Controller?** Rigged characters need it (finding 1). *Default: yes.*

Yes

### Multiplayer

9. **Your file's last sentence is cut off:** "The active player is the one who's turn it is but the …". How does it end?

The active player is the player whos turn it is and who has control of the game. The passive players watch what the active player is doing while he is doing it. Give strong visual cues to the passive players so that they can see what actions the active player is taking, in particular during the buying phases. 
10. **The game is now turn-based at heart. Keep Synchronized Nights and Race as options, or make Take Turns the only mode?** *Default: keep them, with Take Turns as the default.*

Answer: The synchronization doesn't have to be real time but can afford to update at less regular interals etc. As the game is fundamentally turned based the updates only technically need to happen at the end of the entire turn for a player, but we will like to rather periodally update the players anyway so that they get a sense of watching the player as they are making decision. A lag here of these updates of up to 1 second would be totally acceptable and so no additional effort should be made to keep the latency low as long as the game states are consistent by the end of the round.
11. **How much should watchers see?** P1–P4 (hover outlines, mirrored dialogs, targeting arrow, status line) and/or the ghost cursor (N6)? *Default: P1–P5, no ghost cursor.*
Answer: They should see the same information as if it were their turn and they were making the moves, but in a different colour (and not with the same focus on real time synchronization)
12. **Spectator emotes (N5)?** They change the social feel of watching. *Default: off unless you tick N5.*
Answer: Allow emotes and text messaging for now
13. **Should there be an optional turn timer**, so a slow active player doesn't stall watchers? *Default: no timer.*

Yes add it as a a toggle option in the lobby for multiplayer. Have 30, 60, 120 seconds as options and then custom (with labels Hard, Medium, Easy, Custom)

### Localization

14. **Five languages including English** (English, Simplified Chinese, Russian, Spanish, Brazilian Portuguese), **or English plus five** (adding German)? And Latin-American or European Spanish? *Default: five including English; Latin-American Spanish.*

Answer: Whatever is easiest
15. **Is the transcreation level in 4.9 right** (for example 封号 for "swatted" in Chinese, Яжмать for Karen in Russian)? Or should it stay closer to literal? *Default: as in the samples.*

Answer: For other languages that are not english you can translate swatting to "Police Arrive" and "Police Arrival Risk"
16. **Translate the game title per language, or keep "Audience Capture" everywhere?** *Default: keep "Audience Capture" as the brand, with a translated subtitle underneath.*
Answer: Here you can go with "Fight for the Audience", "Streamer Battles" or something like that if its more appropriate for the language in question. 
17. **Character names:** keep globally known meme names (Doge, Gigachad, Tung Tung Tung Sahur) when that's what local players actually say, and adapt the rest? *Default: yes, as in the samples.*
Yes as far as possible
18. **Can I download the four free fonts** (Noto Sans, Noto Sans SC, Rubik, ZCOOL KuaiLe) **into `Assets/DeckParty/Fonts`?** *Default: yes.*

Yes as long as they are fine for use in commerical lisencing for games

### Feel and Sprite Shaders Ultimate

19. **Which nice-to-haves do you want?** Tick the boxes in 6.5 and 6.6. *Default: none, so only the Core list.*

Add all youve put in this plan as options
20. **Post-processing on the UI (N2):** OK to switch the game canvases to camera mode? *Default: no for now. We can try it after the core is in.*

No for now . Lets try it later
21. **Delete the Feel and SSU demo folders** (they can be re-imported any time)? *Default: yes.*

Yes
22. **Is the GitHub repo public or private?** If public, I'll add Feel and SSU to `.gitignore` and document how to re-import them. *Default: treat it as public.*

Its private

### Display

23. **16:10 and ultrawide: fullscreen only (as your file reads), or windowed too?** *Default: every resolution in both modes, with windowed limited to sizes that fit your desktop.*
24. **Include 32:9 (5120×1440)?** *Default: yes, shown only when the monitor supports it.*
25. **Where should the fullscreen control go:** the in-game Menu plus Alt+Enter/F11, or also a button on the HUD top bar? *Default: Menu plus shortcuts.*
26. **Offer exclusive fullscreen as well as borderless?** *Default: borderless only.*

### Art

27. **Are the sizes in section 8 right, and is "stop shrinking and scroll the chat area" OK?** *Default: yes.*

### Process

28. **Have you hand-edited the scenes or UI prefabs in Unity?** If not, a clean rebuild is simplest and tidiest. If so, I'll use the migration tools so your edits are kept. *Default: assume you have, and use migration tools.*

I have added character sprites for characters using the database tool 
29. **When the Unity editor is closed, can I run Unity in batch mode** to import packages and check that everything compiles? This runs no tests; it just catches errors before you open Unity. *Default: no. I'll ask you to open Unity at the M0, M3 and M6 checkpoints.*

No
30. **Should I commit and push at the end of each milestone?** *Default: only when you ask.*

Ask me but yes
31. **Did the round-2 freeze fix and Take Turns work in your last test?** If not, I'll fix that before anything else. *Default: assume it works.*

I havent tested it fully yet but it was looking very promising so thank you for that. I wanted to implement this first before testing the rest of it 


# FINAL ADDITIONAL

Final Addition: I forgot to add one major addition so can you implement this too when making this changes:

I also want you to add to the plan the following feature for the game which is a streamer player character select screen. This will require some additional UI and game mechanics. I want it that before joining a multiplayer lobby or starting a single player run the players are presented with a character select screen similar to that in a fighting game. (It should say Select Your Streamer and be presented like a parody of a fighting game as if the two streamers are going head to head). Each character class will have a slightly different ability (either passive or active). For active abilities they either cost gifted subs or cash or both, tend to get more expensive with re–use (and can have a cooldown) Each the streamers will be parodies of actual streamers including:

- Fate (A parody of Destiny). 

Power:
“Raid”, Active
 Add +1 Swat Risk to you opponent for the next turn. Cooldown 2 turns. Initial Cost 1 dollar. Add 1 dollar to cost after every use. 

I show this as one example, come up with appropriate powers for each streamer choice that is in the similar power level as the previous. Not too strong and if its an active, make it slightly stronger in terms of its impact on the game but make it either have a cooldown, or increasing cost per use or both. These streamer player characters that have actives can only use them during their turn and they should be a button on the same menu where new subscribers are added to the audience pool and bought (after each players turn). 

- Bro Jogan (A parody of Joe Rogan)
– Shinobi (a parody of the streamer Ninja)
– IRunFast (a parody of streamer I show speed)
– MrShark (a parody of Mr Beast)
– Ethical F**kboi ( a parody of Hasan Piker)
– Soft Hard Body Man  (a parody of tyler. He will look a bit like a marshmallow man and the joke is he looks hard but he is a softy)
- Strategic Abomination (This will be a 2 headed monster with 2 specific heads in mind of parodies of chess professionals who stream)
– Palmane (A parody of the streamer pokimane with a little nod to palworld)
- Manim Maketh Man (a parody of math  streamer 3 blue one brown grant sanderson)
– Rude Radiologist (a parody of dr disrespect) 

Come up with a unique but not overpowered power for each of these player characters and add a player character select screen (which includes random and no character as options). For no character the place where an active might be is empty for those players. This is in case the players dont want to play this variant of the game with streamer characters. But design the player character select screen very much in line with a fighting game character select screen

The background is shown during a round is dependent on the character the player selected to play with. With the player is 1 swatting risk away from being swatted, an animation will play at the top of the screen showing the streamer getting anxious (with a slight intermittent screen shake using feel) to signal to the player that they are at risk. 



