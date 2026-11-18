# Controller Support, Couch Play and Multiplayer Menu (2026-11-17)

This guide covers four changes:

- gamepad support, with a selector and remappable buttons;
- the new multiplayer mode screen;
- Couch Competitive;
- the multiplayer clean-up: Race removed, a shorter lobby text and no stream-setup pickers.

## 1. Do this first in Unity

1. Let the scripts compile.
2. Run **Deck Party > Setup > Rebuild Scenes and UI Prefabs**. It builds:
   - the multiplayer mode screen;
   - Settings > Controller;
   - the Controller button in Settings;
   - the lobby and character set screens without the stream-setup pickers;
   - the pause menu without its setup picker.
3. Before the Rebuild, most of it already works. The gamepad, the shortcuts and the hidden setup pickers all work on the old scenes. These need the Rebuild:
   - the multiplayer mode screen (old scenes go straight to host/join);
   - the Controller screen.
4. No save or network change: state format 13 and protocol `deckparty-19`, as before.

## 2. Gamepad

Plug in any controller (Xbox, PlayStation, Switch Pro and most others) and use it. The game switches to controller mode as soon as a pad is touched, and back to mouse mode when the mouse moves or a key is pressed. In controller mode:

- a **selector frame** highlights what you're on;
- **button prompts** show on the stream and shop buttons, by default only while **LB** is held (see Settings > Controller);
- the mouse cursor hides.

### The selector

- **D-pad or left stick:** moves to the nearest thing in that direction. Holding it repeats.
- **What it covers:** everything you could click right now. That includes menu buttons, character sets, lobby settings and dialog buttons, plus in-game the viewers in chat and the shop cards.
- **What it skips:**
  - anything under an open pop-up or out of reach;
  - **Add Viewer** and **End the Stream**, which are only on their pad buttons (X and Y);
  - a viewer's **action icon**: the selector lands on the viewer itself.
- **Confirm (A):** presses what's highlighted. A viewer in chat uses its action (if it has one ready), or gets picked as the target while an action is waiting for one. A shop card is bought.
- **Hover pop-ups** show for whatever is highlighted.
- **Long lists:** they scroll to keep the highlight in view, and the right stick scrolls them too.
- **Sliders** (volume) move with left and right.
- **Where it starts:**
  - **Menus:** on each screen's main button (Continue or Single Player, the first mode card, Start, a dialog's main button).
  - **A stream:** the selector is **hidden** when the stream starts. The first press of the D-pad or left stick shows it on a viewer whose action is ready, otherwise on the first viewer in chat. It also appears by itself when something needs it: a dialog, a picker, picking a target, the Audience, the game menu, or Approve / Deny for a peeked viewer.
  - **The shop:** shown straight away on the first card.
- **Streamer select** keeps its own grid controls: D-pad to choose, A to lock in, B to go back.

### Default buttons (Xbox names)

| Button | Everywhere | In a stream | In the shop |
|---|---|---|---|
| **A** | Select / press | | |
| **B** | Back / cancel (closes the top pop-up, stops picking a target) | | |
| **Menu** | Game menu (press again to close it) | | |
| **View** | Audience | | |
| **LB** (hold) | Show the button prompts | | |
| **X** | | Add Viewer | Raise Sub Cap |
| **Y** | | End the Stream | Go Live / End Turn (also Ready for the shop in Free For All) |
| **RB** | | | Streamer's active |
| **LT** | | | Remove a Viewer |
| **RT** | | | Add a Stream (Endless) |
| **RS** (right stick press) | | | Add a Viewer to the Shop (Endless) |

Stream and shop actions share buttons because they never happen at the same time.

### Settings > Controller

- **Show controller shortcuts** (the top row) decides when the button prompts show. Press the row to step through:
  - **Hold** (default): only while the Show shortcuts button (LB) is held;
  - **Always**: whenever the controller is in use;
  - **Never**: no prompts, only the selector.

  The shortcuts work the same with all three settings; only the prompts change.
- Below it, one row per action, grouped as Everywhere, In a stream and In the shop.
- Click a row (or highlight it and press A), then press the pad button you want. You have 8 seconds; Esc on the keyboard cancels.
- If another action that can happen at the same time already uses that button, the two swap, so nothing is ever left without a button.
- **Reset to Default** puts the table above back (it doesn't change Show controller shortcuts).
- The mapping is saved on this PC. A mapping saved before this update moves Remove a Viewer, Add a Stream and Add a Viewer to the Shop to their new defaults, unless you had changed them.

### UI Skin

- **Controller section** (near the Shop section) sets:
  - the selector frame's padding, colour and breathing;
  - **Move time (seconds):** how long the frame takes to slide to the next thing. Bigger is slower; 0 jumps. Default 0.2.
  - **Move easing:** Linear, Smooth (default, slows into place), Ease In Out, or Overshoot (springs a little past and settles);
  - whether prompts show at all (this switches them off for everyone, whatever the player's setting), their size, which corner they sit on and their overhang;
  - whether the mouse cursor hides.
- **Controller (gamepad) entries:**
  - **Selector frame:** a 9-slice outline. Override Colour tints it.
  - **One glyph per pad button:** A, B, X, Y, the bumpers, the triggers, View, Menu and the stick presses. Without art, a prompt is a coloured circle with the button's name (A green, B red, X blue, Y yellow).

## 3. Multiplayer mode screen

Multiplayer now opens on three big cards, styled and skinned like the single-player ones. All three use the **Mode Button** background, and each has its own icon entry: Online Competitive, Online Co-Op Endless and Couch icons.

| Card | Goes to | Lobby choices |
|---|---|---|
| **Online Competitive** | Host / join, then the lobby | Take Turns, Free For All or 2v2 |
| **Online Co-Op Endless** | Host / join, then the lobby | Co-op only. The mode and stream-count selectors are hidden. |
| **Couch Competitive** | The character sets, then Player 1's and Player 2's streamer picks | None: 1v1, taking turns |

Notes on the online lobbies:
- A host's lobby starts in the section they picked.
- A player who joins sees whatever the host set up.
- Back on host/join returns to this screen (it used to open the streamer select by mistake).

## 4. Couch Competitive

Two players share one computer, using the mouse and keyboard, controllers, or both. Whoever's turn it is plays.

**How a match runs:**
1. Pick a character set, then **Player 1** picks a streamer, then **Player 2**.
2. **A turn** is your stream, then your shop. The shop's Go Live button reads **End Turn**.
3. **Hand-over:** after End Turn, a dialog says "Player 2, you're up!". Their board comes up when they press **Start My Turn**.
4. **Each player has their own season:** Audience, queue, Sub Cap, Cash and Influence. Only the shop is the same.
5. **Rival powers:** Raid and Wealth Tax hit the other player.
6. **The Players panel** lists both players and whose turn it is.

**How a match ends:**
- **4 Trophies in one stream:** if Player 1 gets there, Player 2 still plays that stream ("one last turn to match it").
  - If only one player made it, they win.
  - If both made it, it's a **draw**.
- **After the last stream:** the best single stream wins (equal is a draw).

Couch matches aren't saved and don't change your single-player records.

## 5. Multiplayer clean-up

- **Race is removed.** Only Take Turns and Free For All remain, plus 2v2 and co-op. A lobby still set to Race plays Take Turns.
- **Lobby text:** the block under the lobby settings is now one short line about the mode, plus a warning when the players don't fit it. It used to hold the character set description, the full mode rules and the timer summary.
- **A co-op lobby stays co-op** when a third player joins. It can't start until that player leaves. Competitive lobbies still switch to Free For All when there are too many players for the mode (more than two for Take Turns, more than four for 2v2).
- **Stream setups can't be picked any more.** The pickers are gone from three places: the character set screen (with its preview and description), the lobby and the pause menu. Each game uses the streamer's own setup, or the default.

## 6. Where things are

| What | Where |
|---|---|
| Pad input, bindings and saved mapping | `Runtime/Input/PadInput.cs`, `PadBindings.cs` |
| The selector and its frame | `Runtime/Input/PadNavigator.cs` (also `Sleep`/`Wake` for the hidden start of a stream), `PadFocusFrame.cs`, `PadRunner.cs` (runs every frame, before anything else) |
| Buttons the selector skips | `Runtime/Input/PadSelectorSkip.cs` (added in `PartyControlsView` and `GuestCardView`) |
| Show controller shortcuts setting | `AppSettings.PadPrompts`, `PadShortcut.PromptsWanted`, the top row of `ControllerSettingsScreen` |
| A button's own pad button and prompt | `Runtime/Input/PadShortcut.cs` (bound in `PartyControlsView`, `PlanningView`, `StreamerPowerView`, `HudView`, `ShopGateView`) |
| Default focus / screens with their own pad controls | `PadDefaultFocus.cs`, `PadSelfNavigation.cs` |
| The game screen with a pad | `Runtime/UI/Game/GameController.Pad.cs` |
| Settings > Controller | `Runtime/UI/Menu/ControllerSettingsScreen.cs`, built in `MainMenuSceneBuilder.BuildControllerScreen` |
| Controller look | `Runtime/Data/PadLook.cs` (UI Skin > Controller), `Editor/UISkinEditor.Pad.cs` |
| Multiplayer mode screen | `Runtime/UI/Menu/MultiplayerModeScreen.cs`, `Runtime/Networking/OnlineCategory.cs`, `MatchModes.Competitive` / `Coop` |
| Couch play | `Runtime/Session/LocalGameSession.Couch.cs`, `Runtime/UI/Game/GameController.Couch.cs`, `GameOverView.ShowCouch` |

## 7. Calls I made (check these)

1. **Shared buttons:** stream and shop actions share default buttons (X: Add Viewer / Raise Sub Cap; Y: End the Stream / Go Live). Every action is still its own entry in Settings > Controller.
   - **LB is Show shortcuts everywhere**, so Remove a Viewer moved to LT, Add a Stream to RT, and Add a Viewer to the Shop to RS (right stick press).
   - Holding LB only shows prompts; it never presses anything.
2. **The D-pad and left stick always move the selector** and can't be remapped. Only the face buttons, bumpers, triggers, View, Menu and stick presses can.
3. **B never opens the game menu.** Only Menu does, and Menu also closes it again.
4. **Couch play: any pad or the keyboard and mouse controls whoever's turn it is.** Pads aren't assigned to a player.
5. **Couch ties:** both players hitting 4 Trophies in the same round is a draw, and so is an equal best stream after the last stream.
6. **Couch timing:** a Raid or Wealth Tax lands on the other player's next stream, which starts when their turn comes round.
7. **Quick Match / Find Games (Steam)** can still find a lobby from the other section, for example a co-op lobby while you're in Competitive. Tell me if they should be filtered.
8. **Gamepad typing:** text fields (player name, join code) still need a keyboard.
9. **Nothing has been run in Unity.** Please test:
   - a real pad in the menus, the stream, the shop and the dialogs;
   - remapping;
   - a full couch match, including the last stream and a 4-Trophy finish.
