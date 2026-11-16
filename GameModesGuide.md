# Game Modes, Streamer Pick and Hover Pop-ups (2026-11-16)

This guide covers three changes:

- the new game modes from `additionalgamemodes.md`;
- the streamer pick, which is now the last step before a match;
- the reworked hover pop-ups and the copy clean-up.

## 1. Do this first in Unity

1. Let the scripts compile.
2. Run **Deck Party > Setup > Rebuild Scenes and UI Prefabs**. It does four things:
   - builds the single-player mode screen, the Switch Team button, the Endless shop buttons, the team divider and the new tooltip card;
   - creates placeholder icons for the new UI Skin slots;
   - fills those slots;
   - re-imports the strings.
3. Old saves still load: state format 12 reads 2–11.
4. Multiplayer needs every player on this build (protocol `deckparty-18`).

## 2. Single player: three modes

Single Player now opens a screen with three big buttons:

| Button | What it starts |
|---|---|
| **Puzzles** | The character sets, as before: get 4 Trophies in one stream to win. |
| **Endless** | Last as many streams as you can (section 3). |
| **Versus the AI** | A season against a simple AI streamer (section 4). |

Each button then goes to the character set screen as before. **Start** opens the streamer select, which is now the last step (section 6). The character set screen shows each mode's own record:

- Endless: your longest run;
- Versus the AI: wins and losses.

## 3. Endless

- A run starts with **25 streams** (`GameRules.EndlessStartingStreams`). There is no win; the goal is to last.
- **Trophies are a currency.** Each Trophy viewer in a scored stream banks one Trophy.
  - The menu bar shows the banked total, with no "/4".
  - Hover it for the prices.
- Two extra shop buttons sit next to the others. Each costs **2 Trophies, then 1 more per use** (`EndlessFirstTrophyCost`, `EndlessTrophyCostStep`), and the price shows above the button.
  - **Add a Stream:** one more stream for this run. Buy it before your last stream ends.
  - **Add a Viewer to the Shop:** opens the viewer picker with **3 viewers the shop doesn't sell yet** (`EndlessViewerChoices`). The one you click stays in the shop for the rest of the run.
    - The offer is fixed until you buy, so it can't be rerolled.
- The run ends when the streams run out. Game over shows how many streams you lasted, and a new best is recorded.

## 4. Versus the AI

- Pick your streamer, then the AI's, on the same select screen.
- After each of your streams, the AI plays the same stream number and its shop straight away. **The first to win wins.**
- The AI is deliberately simple (`Core/SimpleAI.cs`):
  - It adds viewers and sometimes uses a random action on a random legal target.
  - It stops at a random moment that gets likelier as chat fills and Risk climbs. It never stops with fewer than 2 viewers in chat if it can add more.
  - In the shop it buys a Trophy viewer when it can afford one, otherwise random viewers. Now and then it buys a Sub Cap upgrade or its streamer's active.
  - It sends the same commands a player would, so it can't cheat.
- Rival powers (Raid, Wealth Tax) work both ways against it.
- The AI shows in the Players panel with a one-line summary of its last stream.
- Versus the AI is **not saved**, so an unfinished single-player season stays where it was.

## 5. Multiplayer: 2v2 and Co-op Endless

Pick the mode in the lobby.

| Mode | Players | Goal |
|---|---|---|
| **2v2** | Exactly 4: two teams of two | The first team to 4 Trophies in one stream wins. |
| **Co-op Endless** | Exactly 2: one team | The Endless rules (section 3): see how many streams you last together. |

### Teams in the lobby

- Players are split into two teams. **Switch Team** moves you to the other side if it has room.
- In both modes, a team's first player in the lobby picks the team's streamer.

### How a team plays

- **Shared:** Cash, Influence, Trophies, the shop and the streamer.
- **Your own:** each player has their own Audience (deck), queue and Sub Cap. Copy limits also count per player (for example, 8 Loyal Fans each).
- **The stream:** a vertical divider splits the chat. Player 1 is on the left, player 2 on the right, with names over each side.
  - Each player adds viewers from their own queue to their own side.
  - Your actions can target viewers on **either** side.
- **Scoring:** the stream scores once **both** players have ended their side, or when the timer runs out. Both sides count as one stream for scoring, communities, Risk and actives.
- **Rounds:** both teams stream at the same time. The shop then opens for everyone, as in Free For All.

## 6. The streamer pick comes last

The streamer is now picked last, like choosing a fighter.

- **Single player:** mode, character set, setup and Start come first. Then the streamer select opens; in Versus the AI, the AI's select follows.
- **Multiplayer:** the lobby no longer has a streamer button.
  1. Once everyone is ready and the host presses Start, every player sees the select screen at the same time.
  2. A **30-second** countdown runs (`MatchModes.StreamerPickSeconds`).
  3. Lock in with Enter or a click. When time runs out, **Random** is picked for you.
  4. The match starts as soon as everyone has locked in.
- **Team modes:** only the team's first player picks. Their teammate waits in the lobby with a note.
- **Someone leaves during the pick:** everyone goes back to the lobby with a message.

## 7. Hover pop-ups

### The new look

Every hover pop-up now uses one card: the white panel with the black border from the shop and streamer panels.

- **Width:** sized to its text. Short tips are narrow; long ones wrap at **440 px**.
  - Change the limits with **Max Width** and **Min Width** on the Tooltip object (`OverlayCanvas > Tooltip`) in each scene.
- **Title row:** bold, with a small faint note beside it, such as a price, a key like `[Space]`, Risk `2/3`, or community points.
- **Divider:** a grey line between the title and the body.
- **Body:** short lines. A blank half-line separates sections:
  1. what it does;
  2. key facts;
  3. shop or live numbers.
- **Colours** follow fixed rules:
  - faint grey for details;
  - green for "ready" and "what it gives right now";
  - orange for why you can't do something.
  - The UI Skin's Site Colours for **Tooltips** still apply.

### How to write pop-up text

- **In code:** use `TipText` (`Runtime/Visuals/TipText.cs`):
  ```
  TipText.Titled(title, note).Line(...).Detail(...).Good(...).Warn(...)
  ```
- **In the strings CSV:** a tooltip string whose first line is bold (`<b>Title</b>\n...`) gets that line as its title row automatically. For example: `hud.tip.cash_banked`, `chat.spare_seat_tip`.

### What changed per pop-up

| Pop-up | Now |
|---|---|
| **Viewer** (chat, shop, Audience) | Name with its price, then what it does. Below that, one small line with its tags and communities, and its action and action type. In the shop: **When scored** icons, then "You own N · You can recruit N more". In chat: its live score and this stream's modifiers. |
| **Community** | The icon and name, with the points beside them during a stream. Then the rule, then "Now: ..." in green. |
| **Deplatform Risk** | The title shows `net/limit`. Each source is on its own line (from viewers, a rival's Raid, cancelled, Core Fanbase, ignored), then the rule. |
| **Trophies** | In chat `n/4`, in your Audience, best stream, then the rule. Endless: banked, in chat, and what they buy. |
| **Add Viewer / End the Stream** | The title with its key. Then what it does, the next viewer and the queue count, or **"End now and bank [payout]"**, plus why it's disabled. |
| **Shop buttons** | The title with the price as its note (Sub Cap `6 → 7`, Remove a Viewer, Add a Stream, Add a Viewer to the Shop). Then one line on what it does, the rule in grey, and the reason if it's locked. |
| **Streamer active** | Streamer name and power type, then the power name and description, Ready or Recharging, the price rules, the use price and the reason if it's locked. |
| **Timer, Menu, Audience, action buttons** | A short title, plus one line where needed. |

### Copy clean-up (all 5 languages)

- **"Tonight" is gone from game text.** This was a Party House leftover. It now says "this stream" (zh 本场直播, ru в этом стриме, es en este stream, pt nesta live), or the phrase is dropped, as in "No payout".
  - Affected: bans, logs, errors, statuses and the Audience hint.
  - The descriptions of put-aside meme cards were left alone.
- **Card tags** are sentence case instead of shouting capitals ("Brings 2", "Takes 3 spaces", "Action: Shoutout"). The old **"PLAY"** tag now reads **"Trophy"**.
- **"(s)" plurals** in community lines are now proper plurals ("1 spare seat free", "2 extra uses left").
- **Shorter community rules** for Core Fanbase, Staff, Troll Army, Boomers, Gatekeepers, Hype Men, Mods and Voyeurs.
- **Shorter lobby mode descriptions** for Take Turns, Free For All and 2v2.
- **Removed strings** (no longer used): `hud.audience_button`, `hud.tip.risk`, `hud.tip.risk_idle`, `hud.tip.diamonds`, `hud.tip.trophies_endless`, `shop.remove_viewer`, `card.action_type`.

## 8. UI Skin entries

All of these are editable in the UI Skin (or Game Database > UI Icons) and have generated placeholders until you add art.

| Slot | Where |
|---|---|
| **ModeButton** (47) | Background of the three big buttons on the single-player mode screen. |
| **IconModePuzzles** / **IconModeEndless** / **IconModeVersusAi** (48–50) | Their icons. |
| **IconAddStream** / **IconAddShopViewer** (51–52) | The two Endless shop buttons. |
| **TeamDivider** (53) | The vertical line between the two sides of a team's chat. |
| **IconSwitchTeam** (54) | The lobby's Switch Team button. |

The two Endless buttons are also in **UI Skin > Shop**, as **Add Stream** and **Add Shop Viewer**, like the other shop buttons: icon, shape, text and size.

## 9. Where things are

| What | Where |
|---|---|
| Endless rules | `GameRules` (Endless constants), `Core/PartyEngine.Modes.cs` |
| The AI | `Core/SimpleAI.cs`, run by `Runtime/Session/LocalGameSession.cs` |
| Team play rules | `Core/PartyEngine.cs` (member queues, Sub Caps, sides), `PlayerState` (`Members`, `GuestInstance.Owner`) |
| Team networking and the pick phase | `Runtime/Networking/DeckPartyNetSession.cs`, `NetTypes.cs` (`MatchMode.TwoVsTwo` / `CoopEndless`, `MatchPhase.PickingStreamers`) |
| Mode screen and menu flow | `Runtime/UI/Menu/SinglePlayerModeScreen.cs`, `MainMenuController.cs`, `StreamerSelectScreen.cs` |
| In-game mode UI | `Runtime/UI/Game/GameController.Modes.cs`, `HouseView.cs` (team split) |
| Records | `ProgressStore` (best Endless run, Versus wins and losses) |
| Pop-up layout | `Runtime/UI/Common/TooltipView.cs`, `Runtime/Visuals/TipText.cs`, built in `Editor/SceneCommon.cs` |

## 10. Calls I made (check these)

1. **Team streamer:** the team's first player picks it, and the teammate doesn't pick.
2. **Team actions:** you use your own viewers' actions, but they can target either side. Fetch-style actions draw from the acting player's own queue.
3. **Hype Men spare seats in teams:** free spare seats show on the first Hype Man's side.
4. **Copy limits in teams** apply to each player's own Audience. The shop stock is shared by the team.
5. **Add a Stream** must be bought before your last stream ends. Once the run is over, it's over.
6. **Add a Viewer to the Shop** offers 3 viewers and doesn't reroll.
7. **Versus the AI** isn't saved, and the AI plays its stream instantly after yours rather than in real time.
8. **The team divider** takes one full column of the chat grid.
9. **2v2 and Co-op Endless need a real playtest** with four (or two) clients, for example with Multiplayer Play Mode virtual players. Every path compiles, but nothing has been run.
10. **Pop-up text:** a CSV tooltip whose first line starts in bold is treated as having a title row. If you want a bold first word without a title, start the line with something else.
