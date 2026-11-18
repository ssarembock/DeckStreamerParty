# Achievements and Unlocks

*18 November 2026. Menu: Deck Party > Open Achievements and Unlocks Guide.*

The game now keeps its own achievements on each PC and passes them on to Steam and Epic. Any streamer or multiplayer
emote can require an achievement before it can be used. Two new streamers, **Kai Sonnet** and **Train Wrex**, start
locked. The **Achievements and Unlocks Tester** window lets you set achievements, counters and unlocks while you test.

---

## 1. First steps in Unity

1. Run **Deck Party > Setup > Rebuild Scenes and UI Prefabs**. This:
   - creates `Resources/AchievementLibrary.asset`;
   - imports the new text;
   - adds the two new streamers to the Game Database with their default unlock conditions;
   - lays out the streamer select grid for 13 streamers + Random + No Streamer (8 columns).

   An old scene still works, because the grid widens itself at runtime, but the Rebuild lays it out properly.
2. Open **Deck Party > Achievements and Unlocks Tester** and keep it docked while you test (section 5).

---

## 2. The starter achievements

| Id (never change it) | Name | How you earn it | Counter | Steam / Epic id |
|---|---|---|---|---|
| `first_single_player` | Going Live | Finish your first single-player game. | Single Player Games Played ≥ 1 | `FIRST_SINGLE_PLAYER` |
| `first_multiplayer` | Collab Stream | Finish your first multiplayer game. | Multiplayer Games Played ≥ 1 | `FIRST_MULTIPLAYER` |
| `first_endless` | 24/7 Stream | Finish your first Endless run. | Endless Games Played ≥ 1 | `FIRST_ENDLESS` |
| `first_streamer_unlock` | New Talent | Unlock your first streamer. | Streamers Unlocked ≥ 1 | `FIRST_STREAMER_UNLOCK` |
| `first_emote_unlock` | Emote-Only Mode | Unlock your first emote. | Emotes Unlocked ≥ 1 | `FIRST_EMOTE_UNLOCK` |

**What counts as a game:**
- A game counts when it reaches its end screen (win, loss, or the last stream). Quitting halfway doesn't count.
- **Single player:** Puzzles, solo Endless and Versus the AI.
- **Multiplayer:** online matches and Couch Competitive.
- **Endless:** solo and Co-op Endless runs count as Endless too. A Co-op Endless run therefore adds to both the
  multiplayer and the Endless counters.

**Unlocks chain.** For example, finishing your first single-player game:
1. earns *Going Live*;
2. which unlocks Train Wrex;
3. which earns *New Talent*.

Each step shows a toast.

**Emote-Only Mode can't be earned yet.** It needs at least one emote with an unlock condition, and every emote starts as
None (section 4).

---

## 3. Adding or changing achievements

Select the library with the tester's **Select Achievement Library** button (the asset is
`Assets/DeckParty/Resources/AchievementLibrary.asset`). Each entry has these fields:

| Field | What it does |
|---|---|
| **Id** | The permanent name. Saved progress and the store link use it, so **never change it after release**. Use lower_snake_case. |
| **Stat** | The counter it watches. *None* means only code or the tester can unlock it. |
| **Target** | The counter value that unlocks it. For example, Single Player Games Played with target 10 = "Finish 10 single-player games". |
| **Fallback Name / Description** | Shown when the CSV has no translation for it (in every language). |
| **Icon** | For a future in-game achievements list. The stores use the icons you upload to them. |
| **Hidden** | The description reads "Secret achievement: keep playing to find out." until it's unlocked. |
| **Platform Ids** | The achievement's id on each store. Empty = not on that store. Xbox, PlayStation, Switch, Game Center and Google Play Games are placeholders for later. |

**Translations:** add CSV rows `achievement.<id>.name` and `achievement.<id>.desc` in all 5 languages. The five starters
already have them.

**What you can do without code:** add more achievements on the existing counters, for example "Finish 25 games" or
"Unlock 3 streamers".

**What needs code:** a new kind of counter, such as "win a stream with 4 Trophies" or "use 50 actions". That needs a new
`AchievementStat` value and the line where the game adds to it. Describe the condition and I'll add it.

---

## 4. Unlock conditions

### Streamers
In **Game Database > Streamers**, each streamer has an **Unlock Condition**:
- **None** = playable from the start.
- **An achievement** = locked until that achievement unlocks. The list header shows `[locked]` next to locked streamers.

**Defaults** (set once in the database, your choice rules after that):

| Streamer | Unlocked by |
|---|---|
| Kai Sonnet | 24/7 Stream (`first_endless`) |
| Train Wrex | Going Live (`first_single_player`) |
| Everyone else | None |

**On the select screen:**
- A locked streamer has a dark **LOCKED** overlay. Its power still shows, as a teaser.
- Under it: "Locked. Unlocks with **Going Live**: Finish your first single-player game."
- GO LIVE is disabled while a locked streamer is selected.
- **Random** only picks unlocked streamers.
- The locks apply to every pick made on this PC, including the AI's streamer in Versus the AI and both couch players.

**In multiplayer:**
- Each player's unlocks travel with them when they join the lobby.
- If a pick is locked for that player, the host turns it into Random. This can only happen with an older or modified
  client.
- Random picks from that player's own unlocks.

### Emotes
In **Game Database > Stream Art**, the **Emote Unlock Conditions** list sits under the Multiplayer emotes list. It has
one line per emote, with a preview:
- **None** = every player has it.
- **An achievement** = locked until that achievement unlocks.

**In the game:**
- A locked emote is dimmed in the emote bar.
- Pressing it plays the error sound, and a toast says what unlocks it.
- Locks only stop you *sending* an emote. You always see other players' emotes.

**Conditions follow the emote's position in the list.** If you reorder the emotes, check the conditions again. If you
haven't assigned emote sprites yet, the list follows the text emotes (POG, KEKW...).

### Missing achievements
A condition can name an achievement that has since been deleted from the library. It then shows in red as
**MISSING** and counts as met, so nobody gets stuck.

---

## 5. The tester (Deck Party > Achievements and Unlocks Tester)

**Play Mode or Edit Mode:**
- **Play Mode:** changes apply to the running game at once, with toasts.
- **Edit Mode:** changes go into this PC's saved progress and are used the next time you press Play.

| Section | What it does |
|---|---|
| **Simulate finished games** | Reports a finished single-player game, multiplayer game, solo Endless run or Co-op Endless run, exactly as the end screen does. The real counting and unlocking run. |
| **Achievements** | State, counter, unlock date, store ids and what each achievement unlocks. **Unlock** unlocks it now. **Lock** locks it again, puts its counter one below the target, and locks what it unlocked, so you can earn it again. |
| **Counters** | Type a value, or use +1 / 0. Reaching a target unlocks the achievement. Lowering a counter never locks anything (use Lock). |
| **Streamers / Emotes** | Each one's condition and state, plus a testing override: Default (follow the condition), ForceUnlocked or ForceLocked. **Unlock every streamer and emote** opens everything. **Clear all overrides** resets them. |
| **Store** | Play Mode only. Shows the store, whether achievements are available, and how many unlocks are waiting to be sent. **Sync now**, **Send waiting**, and **Lock all on the store** (Steam only). |
| **Reset all achievement progress on this PC** | Clears every achievement, counter and unlock count. The stores keep theirs, and overrides are kept. |

**Overrides never reach players:** they only work in the Editor and in development builds. They don't count as
unlocks, so they never earn New Talent or Emote-Only Mode.

**Test recipes:**
1. **Train Wrex unlock:**
   1. Reset all progress, then press Play.
   2. Train Wrex shows LOCKED.
   3. Finish a single-player game, or press **Single-player game** in Simulate.
   4. You should see three toasts: Going Live, Train Wrex unlocked, New Talent.
   5. Train Wrex can now be picked.
2. **Emote unlock:**
   1. Set emote 3's condition to Collab Stream.
   2. In a multiplayer game, emote 3 is dimmed.
   3. Press **Multiplayer game** in Simulate.
   4. The emote lights up and Emote-Only Mode unlocks.
3. **Trying the new streamers without earning them:** tick **Unlock every streamer and emote**.
4. **Running a test again:** press **Lock** on the achievements involved, or reset all progress.

---

## 6. Steam and Epic

**How the sync works:**
- **Sending:** each unlock is sent to the store at once. If the store isn't signed in yet, it waits and is retried every
  30 seconds and at sign-in.
- **Bringing back:** at sign-in, the game asks the store which achievements the player already has and brings them in
  quietly (no toasts), so a new PC or a reinstall gets them back. It also sends the ones the store is missing.
- **Where progress lives:** the game's own record (PlayerPrefs `dp.achievements.v1`) decides what's unlocked. The stores
  mirror it.

### Steam
1. In the Steamworks partner site, go to your app > **Stats & Achievements > Achievements**.
2. Add one achievement per entry. The **API Name** must equal the entry's Steam id (`FIRST_SINGLE_PLAYER`...).
3. Add names, descriptions and the two 64 × 64 icons (achieved / unachieved), then **publish** the changes.
4. While you use the test app id 480, your achievements don't exist on Steam. The game logs a warning and the
   achievements still work locally.
5. **Lock all on the store** in the tester clears them on Steam (ClearAchievement), so you can earn them again.

### Epic
1. In the Epic Developer Portal, go to your product > **Game Services > Achievements**.
2. Create one achievement per entry. The **Achievement ID** must equal the entry's Epic id. The game unlocks them
   directly, so they don't need EOS stats.
3. Publish them to the deployment the game uses.
4. Epic doesn't let the game lock an achievement again. To test again, reset the test player's achievements with the
   Developer Portal's tools.
5. Players without an Epic account (device-id sign-in) still earn them, but only an Epic account shows them in the
   launcher and overlay.

### Other stores later
Fill in the matching **Platform Ids** field. That platform's code must then implement `IPlatformAchievements`, the way
`SteamPlatform.Achievements.cs` and `EpicPlatform.Achievements.cs` do.

---

## 7. The two new streamers

Both are **next-stream actives**, bought in the shop like the others. Their placeholder portraits show initials (KS / TW).
Add art, animation and more quotes in **Game Database > Streamers**.

### Kai Sonnet: The Subathon King
- **Platform:** Lookout. Colour: amber.
- **Unlock:** 24/7 Stream.
- **Power: Subathon.** Costs 2 Influence (+1 per use), cooldown 2.
- **Rule:**
  - In your next stream, the first viewer who would cause a **Server Down** goes back to the queue instead, and the
    stream goes on (once).
  - Hype Men spare seats are used first.
  - The viewer can join again later. Whoever they would have brought stays in the queue.
- **Quote:** "We are NOT ending stream, chat."
- **Stream setup:** Subathon Bedroom.

### Train Wrex: The High Roller
- **Platform:** Stomp. Colour: green.
- **Unlock:** Going Live.
- **Power: Double or Nothing.** Costs 1 Influence (+1 per use), cooldown 2.
- **Rule:**
  - If your next stream pays out, its Cash is paid a second time, up to +10.
  - If it fails (deplatformed or Server Down), you lose 2 Influence (never below 0).
  - The projected score in the stream includes the double.
- **Quote:** "Let it ride, chat."
- **Stream setup:** High Roller Lounge.

The numbers live in `StreamerCatalog`:
- Costs and cooldowns are in the streamer table.
- `DoubleDownMaxBonus` = 10 and `DoubleDownLoss` = 2.

StreamersViewersCommunities.md section 2 describes both.

---

## 8. Versions

- **Network protocol:** `deckparty-22`. Everyone needs this build, because the join message now carries the player's
  unlocked streamers.
- **Save format:** 14. It reads older saves; the two new powers' state is saved.
- **PlayerPrefs:**
  - `dp.achievements.v1`: progress.
  - `dp.debug.unlocks`: tester overrides.

---

## 9. Calls I made (change any of them)

- **Train Wrex is unlocked by Going Live,** so a new player sees an unlock after their first game. **Kai Sonnet is
  unlocked by 24/7 Stream,** which fits his subathon theme.
- **A game counts at its end screen,** win or lose. Quitting doesn't count.
- **Couch Competitive counts as multiplayer.**
- **All five starters have target 1.** The counters keep going up, so "Finish 10 games" achievements can use them later.
- **Every emote starts unlocked;** you add the conditions.
- **Store names and icons are set in the store portals.** The game only sends ids.
- **A locked streamer in an online pick becomes Random,** rather than the player being refused entry to the lobby.
