# New UI Guidelines (V5, phase 1: visuals and UI)

The stream and the shop now look like a simple streaming website: a light page, a white menu bar, a black-bordered chat window and icon buttons. Words now only appear in hover pop-ups. Phase 2 (community and card logic) is still to come.

## 1. Do this first

1. Run **Deck Party > Setup > Rebuild Scenes and UI Prefabs**. This rebuilds the Game scene and the prefabs, makes the new placeholder icons and skin slots, and fills in the platform entries and the new strings. Your art and database entries are kept.
2. Press Play. Every logo and button works with placeholder shapes until you add art.

## 2. What changed

| Area | Now |
|---|---|
| **Page** | Light page and white surfaces (`SitePalette`). The stream setup background no longer shows in the stream or shop. The pause-menu setup choice now only changes the music. |
| **Menu bar** (top) | The animated platform logo, then night, Clout, Cash, Sub Cap, the risk pips and the Plays pips. These are icons and numbers only, with a tooltip on each. Audience and Menu are icon buttons on the right. |
| **Stream Chat** | A white, slightly rounded window with a black border. A centred "Stream Chat" title sits over a thin line. Viewers are round avatars. A grey typing area at the bottom holds two icon buttons: **Add Viewer** (platform colour) and **End the Stream** (black). It also shows the projected take (dark pill), the number of risky viewers left in the queue, and the faces of the next viewers when X-Ray reveals them. The chat bubble and its tail are gone. |
| **Channel column** (right) | The facecam with LIVE and the timer (hover it for the streamer's name), and the Community Beats chips below it. |
| **Avatars** | Circles that show only the head. The ring colour shows state: grey normally, gold for Plays, green when infected, teal when immune. **While a viewer's action is unused, the ring pulses in the site accent (the same on every platform), the circle wiggles every few seconds, and a bolt button sits at its bottom-right.** Names and details are in the hover tooltip. |
| **Shop** | Two white cards. The left card holds the streamer portrait (hover for the power), the active as a bolt button with its price, **+1 Sub Cap** with its price, the Audience with your count, and a big **Go Live** (play) button. The right card holds the "Recruit Viewers" title (kept) and the tiles. Tiles show the picture, the community icons, the price and how many are left ("×3"). There's no name, and a red X means none are left. |
| **Names** | Gifted Subs is now **Clout**, and Diamonds are now **Plays**, in all 5 languages. The quick rules and the Plays viewers' descriptions show the Plays icon (`{icon:plays}`, usable in any string). Code names are unchanged (Pop, Diamond). |
| **Buttons** | Every stream and shop button is an icon. Its name, hotkey and why it's disabled are in its tooltip. |

## 3. Where to set things

**Platform per streamer.** Go to Game Database > Streamers > **Platform**. Six satirical platforms are available: Cough, Lookout, Post, Stomp, Tunes and Swipe. Each streamer starts with a default; change any of them. Since 2026-11-08 a platform changes only the animated logo in the menu bar: colours, buttons and layout are the same for every streamer (Add Viewer is always green, End the Stream always red). Players with no streamer use **No Streamer Platform**.

**Platform logos.** Go to Game Database > **Platforms**. Each entry has:
- **Placeholder Colours**: the colours of the built-in placeholder logo, used only while the logo has no sprite. They no longer colour any buttons.
- **Logo...**: the sprite slots for that platform. The inspector names each slot (for example "Boot", "Impact effect", "Pupil"). It also has placement points (0,0 is bottom-left of the logo box, 1,1 is top-right), size, speed and rest time, plus **Reset Placement and Timing**.

**Chat avatars.** Go to Game Database > Viewers > **Avatar (head, chat circle)**. Use a head-only sprite, square, with a transparent or plain background; it fills the circle. You can also name the sprites `LoyalFan_Avatar`, `avatar_loyal_fan` or `LoyalFan_Head` and run **Deck Party > Art > Auto-Assign Sprites by Name**. A viewer without an avatar shows its portrait zoomed in on the head; adjust **Avatar Zoom** and **Avatar Offset** under "Animation...".

**Icons (UI Skin).** Each button icon has its own slot. Deck Party > Select UI Skin lists every slot by name (for example "Add Viewer button icon") with where it shows and a Find button:

| Slot | Button |
|---|---|
| IconAddViewer | Add Viewer |
| IconEndStream | End the Stream |
| IconAudience | Audience |
| IconMenu | Menu |
| IconAction | Viewer actions and the streamer's active |
| IconGoLive | Go Live (next night) |
| IconClose | Deny, cancel, sold out |
| IconNight | The night counter |
| IconCheck | Approve, confirm, ready |
| IconStar | **Plays** |
| IconPop | **Clout** |
| AvatarRing | The ring around the avatars |

Draw button icons and the ring in **white**: the game tints them (dark on grey buttons, white or black on platform-coloured buttons, the state colour on rings). For coloured art, tick **Override Color** on that slot and leave the colour white.

## 4. The logo animations, and how to make your art work with them

**Recommended approach.** All four described logos are built from separate sprites that code moves with squash and stretch, the same technique as Feel's Squash and Stretch feedback. Code keeps several parts in sync: pupils with the lean, the boot with the squash, the hand with the colour wipe. No Animator, rigging or frame-by-frame work is needed. Use these only where noted below, if you want more personality. Timing and placement are tunable per platform in the database.

**Art rules for every logo.**
- Export each part as its **own sprite**, cropped tight and roughly square.
- Keep the main part **standing on the bottom edge** of its sprite: squashes pivot from the bottom-centre.
- Parts are placed with points in the logo box, so you don't need to line them up in the art.

**Cough** (the logo coughs, money flies out)
- Two coughs per loop. Each one is an intake (stretch up, lean back), a bark (squash, lean forward) and a spring back.
- On each bark, bills shoot sideways out of the **Mouth** point, spinning and fading fast, and the three **Cough lines** (one sprite) flash on for a moment.
- Art: Logo, Money (one bill or coin), Cough lines (all three strokes in one sprite).
- Alternatives:
  - A Feel UI particle burst instead of the code bills.
  - A 2–3 frame mouth swap (closed, open) on each bark, if you want it to read as more human.

**Lookout** (balloon with binoculars)
- Looks left, back to centre, right, back to centre, on a loop. The **Pupil** sprite slides across both lenses.
- The body leans and widens toward the look, with a little squash on each turn, and the balloon bobs gently.
- Art: the balloon-with-binoculars sprite and one black pupil. Set the **Left/Right lens centre** points to your lenses and tune the pupil size and travel.

**Stomp** (boot squashes an emoticon)
- The boot drops onto the emoticon, which squashes flat while the **Impact effect** shows.
- It holds; the effect hides first; then the boot lifts and the emoticon springs back with a little overshoot. It waits, then repeats.
- Art: Emoticon, Boot (toe facing right, sole on the bottom edge), Impact effect.

**Swipe** (clock)
- Done with Unity's radial fill, so no masks or shaders are needed. The face being revealed sits on top of the old face as a clockwise radial fill that follows the hand exactly.
- After a full sweep the faces swap and **the hand switches colour**, so it's always visible against the face it's sweeping over.
- Art:
  - **white face with black markers** (Logo)
  - **black face with white markers** (part A)
  - the **hand pointing up** from the centre: a black hand, plus an optional white hand. If you give one hand, draw it white and it gets tinted.
- Set **Hand length** to your face's inner radius (0.5 is the full box radius).

**Post and Tunes** (no animation described yet)
- Both do a hop with anticipation, stretch and a springy landing.
- Easy upgrades with the same technique, once the art exists:
  - Post: a small speech bubble pops out and floats up.
  - Tunes: three equaliser bars bounce in turn.

## 5. Things to know

- **"Clout" is now two things.** It's the resource, and it's also the community "Clout" (which gives +1 Clout). The two have different icons, but you may want to rename the community in phase 2.
- **Unchanged for now:**
  - Pop-up dialogs, the Audience and picker windows, banners and the main menus keep the dark style. Cards in those windows no longer show names.
  - The Activity log panel stays dark (it's off by default).
  - The Free For All shop gate keeps its text buttons.
- **The ring colour has meaning.** If your AvatarRing art is coloured, tick Override Color, and you'll lose the state colours.
- Logo, platform and colour changes show **the next game** (not live in Play mode).
- The old chat-bubble sprites are still in the database file but hidden; you can clear them.

## 6. Quick check

1. Start a game. The logo matches the streamer's platform and animates, and the main buttons use its colour.
2. Add viewers. Avatars appear in circles, and a viewer with an action pulses and wiggles until you use the bolt.
3. Hover everything: menu bar items, buttons, avatars, tiles and the facecam all explain themselves.
4. End the stream and visit the shop: the white cards, the icon buttons and tiles with no names.
5. Switch a streamer's platform in the database and start a new game: the logo and button colour change.
