# Bug Fix Report (2026-11-17)

This report covers the six items in your bug list. For each one it gives what was wrong, why, what changed, and how to check it in Unity.

All code compiles (Core, Runtime, Steam, Editor). Nothing has been run in Unity yet.

## Do this first in Unity

1. Let the scripts compile.
2. Run **Deck Party > Setup > Rebuild Scenes and UI Prefabs**. The Rebuild:
   - bakes the new click area into the chat avatar prefab;
   - builds the Risk meter with 4 pips.

   Both fixes also work at runtime in scenes built before this change. The Rebuild just makes them part of the scene.
3. Saves from before still load: state format 13 reads 2–12.
4. Multiplayer needs every player on this build (protocol `deckparty-19`).

---

## 1. Two-player chat: seats spilling off the sides, and some right-hand seats too big

**What you saw**
- In 2v2 and Co-op Endless, seats ran off the left and right edges of the chat window.
- Some of the right-hand player's seats were bigger than the rest. This happened mostly late in a stream, and the size changed as seats were added.

**Why**

There were two separate bugs.

1. **Spilling off the sides.**
   - The split chat used the grid with the divider as a full-width column of its own.
   - Seats never got smaller than the minimum size. When the window was too narrow for 2 × 4 seats plus the divider column at that size, the whole grid overflowed on both sides.
   - The chat only scrolls vertically, so the overflow was simply cut off.
2. **Oversized seats.**
   - New seat objects are created whenever a stream needs more than before: a new Sub Cap seat, a Hype Man's spare seat, or Big Chungus's extra seat.
   - These new seats never received the chat's current scale, so they drew at full size inside smaller cells.
   - The right-hand player's seats are created last, so they were the ones affected.

**Fix**
- **The split chat now places the seats itself,** like the solo chat:
  - Each side gets half the window, minus a small gap around the divider.
  - Both sides use the same seat size and the same number of columns.
  - Rows hold up to 4 seats per side. Seats shrink to fit the width and height, and more columns are used only when seats would otherwise get too small.
  - Below the minimum size, a side gets as many columns as fit its half. Further rows then scroll down, so nothing hangs over an edge.
- **Each side's seats sit against the divider** and fill outwards.
- **The divider is one line** as tall as the rows, instead of a grid column copied onto every row.
- **New seats always take the chat's current size.** This also fixes the same latent issue in single player.

**Files:**
- `Runtime/UI/Game/HouseView.cs`: the team layout was rewritten. The solo layout is unchanged except that new seats now get the current size.

**Check in Unity**
1. Start Co-op Endless or 2v2 (Multiplayer Play Mode works).
2. Raise one player's Sub Cap well above the other's.
3. Fill both sides, including a Hype Man's spare seat and Big Chungus if you can.

All seats should stay inside the window, the same size on both sides, with one divider line.

---

## 2. Picking a viewer for an action only worked on its community icons

**What you saw**
- When an action asked you to pick a viewer, only the small community icons under the avatar responded to clicks.
- Viewers with no community couldn't be picked at all.

**Why**
- Every picture on a chat avatar (circle, ring, portrait, stats) is built with raycasts off.
- The only parts that could receive the pointer were the community badges and the action button. A click on a badge passed up to the avatar's button; anywhere else nothing was hit.
- The same thing affected:
  - clicking an avatar to use its own action;
  - the viewer's hover pop-up;
  - the hover pop-up on Hype Men spare seats.

**Fix**
- A new invisible **HitArea** graphic covers the whole avatar, behind everything else. It catches the pointer anywhere on the viewer and draws nothing.
- The community badges and the action button sit above it, so they still get their own clicks and pop-ups.
- The chat avatar prefab builder adds it. `GuestCardView` also adds it at runtime to avatars built before this change, so it works before a Rebuild.

**Files:**
- `Runtime/UI/Common/HitArea.cs` (new);
- `Runtime/UI/Common/GuestCardView.cs`;
- `Editor/DeckPartyAssets.cs` (chat avatar).

**Check in Unity**
1. Use an action that asks you to pick a viewer, such as Chat Admin or Account Manager.
2. Click the middle of a viewer's face, and click a viewer with no community.
3. Hover anywhere on a viewer: the pop-up should show.

---

## 3. Two-player sharing rules, and a Risk limit of 4 for teams

**Checked: what a team shares**

| Shared by both players | Separate for each player |
|---|---|
| Cash, Influence, Trophies | Audience (deck) and queue |
| Risk (one count across both sides of the chat) | Sub Cap, and its upgrade price |
| The shop (the same stock) and the streamer | Copy limits (e.g. 8 Loyal Fans each) |
| | **Remove a Viewer price (fixed, see below)** |

**What was wrong**
- The Remove a Viewer price climbed for the whole team: one player's removals made the other player's next removal dearer. Each player has their own Audience, so each now has their own price ladder (2, then +1 per removal), just as each has their own Sub Cap upgrade price.
- This needed a new saved number, so the state format is now 13.

**Risk limit 4**
- 2v2 and Co-op Endless now deplatform at **4** shared Risk instead of 3. Everything that depends on the limit follows it:
  - the deplatform check;
  - actions that refuse to push you over the limit (e.g. Disaster Girl);
  - the Raid cap (a Raid alone can bring a team to 3);
  - the Risk pips: **4 in team games**, 3 otherwise;
  - the red "one away" pip, the Risk pop-up, the near-deplatform warning and the streamer's anxiety;
  - the Risk icon's pop-up and the AI.
- Solo seasons, Versus the AI and the other multiplayer modes keep 3.
- The rule lives in `GameRules.TeamRiskLimit` (4), read through `PartyEngine.RiskLimit(state)`.
- **Risk pips:**
  - Scenes are now built with 4 pips, and the HUD hides the 4th outside team play.
  - Older scenes with 3 pips get a copy of the last pip when a team game needs it, so a Rebuild isn't required.
- **Lobby descriptions:** the 2v2 and Co-op Endless descriptions now say what is shared, including "Risk (the limit is 4)".

**Files:**
- `Core/GameRules.cs`, `Core/PartyEngine.cs`, `Core/PlayerState.cs` (`PartnerRemovals`, `RemovalsOf`);
- `Core/StateSerializer.cs` (v13), `Core/SimpleAI.cs`, `Core/StreamerCatalog.cs`;
- `Runtime/UI/Game/HudView.cs` (pips), `GameController.cs`, `PartyControlsView.cs`, `HouseView.cs`, `ViewerEmojiView.cs`, `PlanningView.cs`;
- `Runtime/Visuals/GameIcons.cs`, `Runtime/UI/Menu/LobbyScreen.cs`;
- `Runtime/Networking/NetworkService.cs` (protocol 19), `Editor/GameSceneBuilder.cs`.

**Check in Unity**
1. In a team game, bring in 3 Risk: the stream continues, and the 4th pip is red.
2. Have one player remove a viewer: the other player's Remove a Viewer price stays at 2.

---

## 4. "Close to winning" notices in Endless

**What you saw:** Endless has no win, but players still got "almost won" and "win" messages.

**Fix:** in Endless (single-player Endless and Co-op Endless):
- **No "one more and they win!" headline.** It used to fire when a stream ended with 3 Trophies.
- **End the Stream never turns into "End the Stream & WIN!",** and never flashes gold.
- **No "4 Trophies are here! End the stream now to win!" hint.** This applies to older scenes that still show it.
- **The Trophy icon's pop-up explains banking** ("Each one in a scored stream banks a Trophy to spend in the shop") instead of "4 to win".
- **New opening headline for Co-op Endless:** "Co-op Endless begins: [set], 25 streams. Last as long as you can together!" It replaces "First stream with 4 Trophies wins!".

**2v2 is not affected.** Your note mentioned 2v2, but 2v2 does have a win (first team to 4 Trophies in one stream), so its "one more" headline and WIN button stay. Tell me if you want them removed there too.

The season-start headline also said "nights"; it now says "streams" in all languages.

**Files:**
- `Runtime/Networking/DeckPartyNetSession.cs`;
- `Runtime/UI/Game/PartyControlsView.cs`, `HouseView.cs`;
- `Runtime/Visuals/GameIcons.cs`.

---

## 5. Risk icons staying skewed (Hate-Watcher with IRunFast)

**What you saw:** after the Hate-Watcher came into chat, the Risk pips above the chat tilted and never straightened.

**Why**
- When the Risk meter reacts, it plays the "Risk pip" effect, a punch plus a rotation wobble (Feel).
- Feel's wobble is *additive*: each time it starts, it takes the meter's current rotation as its starting point and returns to it at the end.
- The meter is left tilted for good if the wobble is cut off partway, in one of two ways:
  - **A second wobble starts mid-wobble.** It records the tilted rotation as its starting point and settles back to it. The Hate-Watcher's mood flip triggers the wobble, and the same arrival can trigger more Risk reactions; a run with Bark at Chat has plenty of Risk events.
  - **The chat window is switched off mid-wobble,** which stops the effect where it is.
- Nothing ever reset the meter afterwards.

**Fix**
- **One Risk wobble per frame.** Several Risk events from one arrival now give one wobble.
- **New `FxRestPose` guard** on the Risk meter and the menu-bar numbers (Cash, Influence, Trophies). It remembers the pose each element was built with.
  - Once no effect has touched the element for 1.2 seconds, it eases the element back to that rotation and scale.
  - If the element is switched off mid-effect, it snaps back straight away.
- Every effect and punch on these elements reports to the guard, so the guard never fights an effect that is still playing.

**Files:**
- `Runtime/Fx/FxRestPose.cs` (new), `Runtime/Fx/Fx.cs`, `Runtime/UI/Common/UITween.cs`;
- `Runtime/UI/Game/GameController.Juice.cs`, `HudView.cs`.

**Check in Unity**
1. Play IRunFast with a Hate-Watcher, or anything that adds Risk in quick succession.
2. Spam Add Viewer.

The pips should wobble and always come back straight.

---

## 6. Community Beats: show different viewers, so the first step reads 2

**What you asked:**
- The counters should count the different viewers from a community in chat, against the number needed for the top bonus.
- The first bonus still starts at 2, so it should read **2/X**.

**Now**
- **Each Community Beats chip** shows **different viewers in chat / viewers needed for the top bonus**. For example:
  - Core Fanbase: 1/4, 2/4 (first bonus), 3/4, 4/4 (top);
  - Hype Men and Gatekeepers: x/3;
  - Boomers: x/4 (level 1 at 2, level 2 at 4);
  - Voyeurs: x/2;
  - Troll Army, Millennials and Mods have no cap, so their chip shows just the count.
- **The chip stays dimmed** until the first bonus (2 viewers).
- **The max shine** lights at the top count.
- **Every new viewer gives the chip a small punch,** and the coin sound plays when a new bonus step starts.
- **The community pop-up** shows the same count beside its title ("2/4 in chat").
- **The rules in the community pop-ups are worded in viewers** instead of invisible points, in all 5 languages. For example: "With 2 different Core Fanbase viewers in chat, each of them has a 10% chance to score twice, +10% for every extra one (30% with 4)."
- **The Audience overview's** "best N points: bonus" now reads "up to bonus".
- **Unchanged:** the rules themselves (the bonus still comes from different viewers minus one). Only the display and wording changed.

**Files:**
- `Core/PartyEngine.cs` (`CommunityMembers`);
- `Runtime/UI/Game/CommunityBeatsView.cs`;
- `Runtime/Visuals/CommunityVisuals.cs` (`MembersForMax`, `MembersText`);
- strings: `community.members`, `community.members_max`, and the 10 community descriptions.

**Not changed:** the How to Play page still explains communities in points ("two score 1 point, three score 2…"). That's still accurate, but say if you'd like it reworded to match.

---

## Strings (all 5 languages)

| Change | Keys |
|---|---|
| **Added** | `community.members`, `community.members_max`, `hud.tip.plays_term_endless`, `mp.coop_begins` |
| **Reworded** | `mp.season_begins`, `audience.community_best`, `lobby.mode_two_vs_two`, `lobby.mode_coop`, and 10 community descriptions (Core Fanbase, Staff, Troll Army, Boomers, Millennials, Gen-Z, Gatekeepers, Hype Men, Mods, Voyeurs) |
| **Removed** (no longer used) | `community.points`, `community.points_max`, `community.level_max` |

## Decisions to check

1. **Teams still share one streamer.** Your list of shared things (Cash, Influence, Trophies, Risk) doesn't mention it, so I left it as it was. Say if each player should pick their own.
2. **Endless only:** the near-win notices are gone in Endless, not in 2v2 (section 4).
3. **Remove a Viewer price is now per player in teams** (section 3). Before, it was shared.
4. **The Raid in team games:**
   - A Raid alone can push a team to 3 Risk (one under its limit of 4).
   - The streamer's Raid still adds 1 Risk.
