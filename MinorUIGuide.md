# Minor UI Guide (UI fixes: black-and-white look, summaries, shop piles)

This covers the changes asked for in `UIFixes.md`. Open it in Unity with **Deck Party > Open Minor UI Guide**.

## 1. Do this first in Unity

1. Open the project and let the scripts compile.
2. Run **Deck Party > Setup > Rebuild Scenes and UI Prefabs**. This step is needed: it rebuilds the Game and Main Menu scenes and the shop tile prefab, and re-imports the strings (the new captions and Night → Stream). Your art, database and skin are kept, and the old prefabs are backed up to `Prefabs/Backup`.
3. Press Play and run through the check in section 4.

## 2. What changed

| Area | Now |
|---|---|
| **Look** | Panels are white with strong black borders, and text is black. Lines *inside* a panel (under titles, between columns) are light grey. The only grey surface in the stream is the typing area under the Stream Chat. Light icon buttons (Audience, Menu, Deny, Cancel, Raise Sub Cap) are white with a black border. |
| **Pop-ups** | Hover tooltips, dialogs, toasts, banners, the viewer picker, the Audience, the round summary, the Free For All shop gate and the "pick a viewer" prompt all use the white card with a black border and black text. Pale colours written for the old dark panels (gold, teal and green numbers, lilac hints) are darkened automatically so they stay readable on white. |
| **Stream Chat** | More space on the left and right of the avatars (44 px instead of 18). The two buttons are wider and spread apart. The risky-queue count (and the faces of the next viewers) sit at the left end of the typing area, and the projected take sits at the right end. |
| **Channel column** | The facecam is 10% bigger (370 px) and sits at the **bottom right**, like a streamer's camera. Community Beats is above it, with an icon, a "Community Beats" title and a grey line under the title. |
| **Maxed communities** | When a community reaches its maximum score (for example 5/5), its icon gets a breathing gold halo and a slowly turning sunburst. The icon also pulses, with a shader outline, glow and shine. A pop and a chime play the moment the maximum is reached. |
| **Shop tiles** | Narrower, playing-card shaped (about 5:7) with square corners. The picture is a plain rectangle with a black border, and there are bigger gaps between tiles. Behind each tile is a **pile of card backs** (up and to the left), one for each copy left after the top card. The pile shrinks as the stock runs out. Plays viewers never run out, so their pile stays full. |
| **After a stream** | The big "Great stream!" banner is now a smaller summary pop-up. It has one column per player: their streamer avatar, their name, their streamer, the result, Clout, Cash and Plays, and extras (streak, Server Restored, Bots sent). Grey lines separate the columns. |
| **After the shop** (multiplayer) | A matching **Shop Recap** shows, per player: recruits (grouped, for example "+ Loyal Fan ×2"), Sub Cap raises (5 → 6), actives used, and the Clout and Cash change. |
| **Words** | Night is now **Stream** everywhere the game counts rounds or starts the next one: the HUD (Stream 3/25), the banner (STREAM 4), Start / Ready for Stream N, the hints, the log, the lobby and the multiplayer headlines. This applies in all 5 languages (zh 场, ru Стрим, es Stream, pt Live). Viewer descriptions that say "tonight" are unchanged. |

### When the summaries appear

| Mode | Stream summary | Shop summary |
|---|---|---|
| Single player | After each stream (after the scoring, the Deplatform image or the blame pick) | None |
| Take Turns | After the active player's stream, for everyone (one column) | When their turn passes on (one column) |
| Free For All | When the **last** stream of the round ends (a column per player) | When the next round starts (a column per player who shopped) |
| Race | After your own stream | None |

To close a summary, press Continue, click outside it, or press Space, Enter or Esc. In Free For All your own stream still ends with the quick "Great stream!" banner, because the summary waits for everyone.

## 3. Guidelines for future UI

- **Default style:** black text, a white fill and a black border (rounded `RoundedSmall`, or square for cards and pictures). Use light grey (`SitePalette.Divider`) only for lines that divide one panel. Use colour for meaning (resources, danger, the platform accent), not for surfaces.
- **New pop-ups in the scene builders:** use `EditorUI.ModalShell(..., light: true)` and `EditorUI.SiteTextButton`. Wrap any label that shows rich text in `EditorUI.Readable(...)`. That adds `ReadableOnLight`, which darkens pale colours automatically, so existing strings can be reused.
- **Square, card-like panels:** `EditorUI.SharpPanel`. **Bordered cards that hold a layout:** `GameSceneBuilder.BorderedCard`.
- **Site Colours (UI Skin):** build site panels with `EditorUI.SitePanel(..., area)` (or `BorderedCard`) inside the Game scene builder, which opens `EditorUI.SiteColourScope()`. Panels, `SitePalette.Divider` lines, `SurfaceAlt` fills and `Text`/`TextDim`/`TextFaint` text then get a `SiteColour` and follow the UI Skin. Code that colours site graphics itself should use `SiteColours.Get(role, transform)`. Buttons are built inside `EditorUI.PlainColourScope()` and keep their own colours. A new named panel needs a `SiteArea` value (append it) and a row in the UI Skin inspector.
- **Maxed effect tuning:** select a `Beat_<Community>` chip in the Game scene (under `Channel/CommunityPanel`). On **Community Max Fx** you can set the halo colour and the ray speed. The shader layer uses the FX Library's portrait uber material, so **Status Effects** must be on. The halo and rays work without it. Reduce Flashing calms the pulsing.
- **Shop piles:** the three card backs are `Stack/Back1-3` in the ShopItem prefab. Tile sizes are on the Game scene's **Planning** object (Min/Max Tile Width, Tile Aspect).
- **Icon slot:** the stream counter still uses the **IconNight** UI Skin slot (a moon placeholder). You may want to draw a stream or broadcast icon for it. The Community Beats title uses **IconAudience**.

## 4. Quick check

1. Hover anything: the tooltip is white with a black border and dark text.
2. Start a stream: the facecam is at the bottom right, Community Beats has its title and line, and the chat has wider side margins and wider buttons.
3. Get a community to its maximum: its icon shines and pulses.
4. End the stream: the smaller summary pop-up appears (Continue or Space closes it).
5. In the shop: the tiles are narrow square-cornered cards with piles that shrink as you buy.
6. In multiplayer (Take Turns or Free For All): the stream and shop summaries show one column per player, each with an avatar, name and streamer.
7. The HUD and the next-round button say **Stream**, not Night.

## 5. Left as they were

- The pause menu, the game over screen, the main menu screens, the Activity log and the dark cards in the picker and Audience keep their dark style. Their sliders, selectors and cards are built for dark panels, so they need a separate restyle if you want them white too.
- The rival-power banner text and the anxious-streamer and presence overlays are built in code and unchanged.

## 6. Layout update (floating menu bar, buttons under the chat)

Run **Deck Party > Setup > Rebuild Scenes and UI Prefabs** to see it.

| Area | Now |
|---|---|
| **Menu bar** | A white rounded panel with a pure black border, floating just below the top of the screen. The logo, stream count, Clout, Cash, Sub Cap, risk, Plays, Audience and Menu each sit in their own section between short grey separators. The icons are slightly bigger. |
| **Risky queue** | Just the risk icon and the number of risky viewers still in the queue (no background or border), under the Audience button inside the menu bar, during a stream only. The bar is 128 px tall to fit it; the Audience and Menu buttons sit slightly above its middle. |
| **Projected take** | The pill by the End the Stream button is gone. Hover End the Stream to see what ending now would bank. |
| **Stream Chat** | The grey typing area is gone. Add Viewer and End the Stream are wider (320 px) and sit in a row under the chat window. Along the bottom of the window is a footer band for your picture. |
| **Footer picture** | **Game Database > Stream Art > Chat Footer Image**: centred along the bottom of the chat, under the viewers. Nothing shows until you add one. |
| **Near-deplatform warning** | The anxious-streamer pop-up now appears centred in that footer band, on top of the picture, instead of at the top of the screen. |
| **Players panel** (multiplayer) | Now on the left, before the Stream Chat. |
| **Borders** | Site panels (menu bar, chat, Community Beats, shop cards, players) have pure black borders, 4.5 px thick; pop-ups 3.5 px. |
| **Community Beats** | Icons are 62 px (the old 38 px was raised in two steps); chips are 70 px tall, so the panel fits about 12 chips at 1080p. |
| **Turn timer** | Top-right of the Stream Chat window's title band. |
| **Grey dividers** | Horizontal ones are 4 px thick and stop 20 px short of each side of their panel; the vertical menu-bar and summary separators are 3 px. |
| **Communities on chat avatars** | Each community icon (30 px) sits on a small white circle with a black border, sitting on the bottom of the avatar's ring. One community is centred at the bottom; two or three fan out evenly from there. The spacing is on the ChatAvatar prefab's **Community Icons View** (Arc Radius, Arc Step). |
| **Rewards under each avatar** | Just the icons and numbers, with no dark pill behind them. |
| **Near-deplatform warning** | 1480 × 210 px (never wider than the chat window: Max Width and Side Margin on its StreamerAnxietyView), with a larger portrait and text. The footer band grew to 224 px to hold it. |
| **Avatar rings** | Pure black by default (gold for Plays, green when infected, teal when immune; the platform colour pulses while an action is unused). Empty seats stay faint grey. |
| **Rounder panels** | Panels use the large rounded sprite (24 px corners instead of 11), with the inner fill matched so the black border keeps its thickness round the curve (EditorUI.RoundPanel). The facecam's video is clipped to the same rounded shape. |
| **Community Beats entries** | No background or border: just the icon, the score and the bonus. |
| **Title icons** | The Stream Chat title is a 160 px icon (UI Skin slot **IconStreamChat**, a speech-bubble placeholder; hover it for "Stream Chat") in a 176 px title band. Community Beats has no words, just a 120 px icon (**IconCommunityBeats**, separate from the Audience button's **IconAudience**; hover it for "Community Beats") in a 136 px band. Sizes: ChatTitleIcon, ChatHeaderHeight, BeatsTitleIcon, BeatsHeaderHeight in GameSceneBuilder. |
| **UI Icons in the Game Database** | **Game Database > UI Icons (stream and shop)** lists the UI Skin's stream and shop icon slots with where each shows; drop a sprite on one to replace it. |

## 7. Full-width menu bar, risk pips in the chat, buttons inside the chat (2026-11-08)

Run **Deck Party > Setup > Rebuild Scenes and UI Prefabs** to see it.

| Area | Now |
|---|---|
| **Menu bar** | A plain rectangle along the top edge of the screen, the full width of the screen (outside the 21:9 frame), with a border along its bottom edge only. Its colours are in UI Skin > Site Colours > Menu bar. |
| **Menu bar items** | Left: the platform logo and the stream count. Centred on the screen: Clout, Cash and Trophies. Right: Audience and Menu, with the risky-queue count under Audience during a stream. The Sub Cap count is gone (the chat's seats show it); the Raise Sub Cap button in the shop is unchanged. |
| **Trophies** | One Trophy icon and "in chat / needed to win" (for example 2/4), counting up as Trophy viewers join the stream. It shows 0 in the shop. Hover it for your Trophies owned and your best. |
| **Deplatform Risk** | The three risk pips moved from the menu bar to the Stream Chat's title band, centred, where the Stream Chat title icon was (92 px each: `GameSceneBuilder.RiskPipSize`). They light up the same way; hover them for the details. The IconStreamChat UI Skin entry is no longer shown. |
| **Stream Chat window** | It now reaches the bottom of the stream, level with the bottom of the facecam. |
| **Add Viewer / End the Stream** | Inside the chat window, in a 112 px band along its bottom (`ButtonsBand`), 480 px wide each (50% wider than before). They sit about 12 px higher than before, to clear the window's border. |
| **Next viewers' faces** (X-Ray) | Just above the buttons at the left of the chat (the wider buttons cover their old spot). |
| **Waiting-viewer panel, footer picture, near-deplatform warning** | Moved up with the buttons' band, so they stay where they were on screen. |
