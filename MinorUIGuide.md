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
