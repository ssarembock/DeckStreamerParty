# Change Summary and Guidelines (PlayTestUpdates.md)

## 1. Do this first in Unity

1. Let the scripts compile.
2. Run **Deck Party > Setup > Rebuild Scenes and UI Prefabs**. It rebuilds the Game and Main Menu scenes and the player-row and chat-avatar prefabs, creates the new placeholder icons, fills the new UI Skin slots and re-imports the strings.
3. Saves from before still load (state format 8 reads 2–7). Multiplayer needs every player on this build (protocol `deckparty-10`).

## 2. What changed

| Area | Now |
|---|---|
| **Free For All: watching** | Click a player in the Players panel to watch their stream live, read-only (the same view as watching in Take Turns). Click your own name, or **Back to my stream** in the bar, to come back and get your controls again. Everyone is brought back to their own board when the shop countdown starts. |
| **Timers** | A **Stream Timer** and a separate **Shop Timer**, both set in the lobby, for Take Turns and Free For All (not Race). The timer shows only an hourglass icon and the seconds; hover it for which timer it is and whose turn. The player name before the number is gone. |
| **Timer runs out** | **Stream timer:** the stream ends and is scored. If the stream was deplatformed, a **random** viewer gets the Temporary Ban, because someone must always be banned. In Take Turns the shop timer then starts. **Shop timer:** Take Turns passes the turn; Free For All marks everyone ready for the next stream. In Free For All the stream timer also marks everyone ready for the shop. |
| **Free For All: end of stream** | No pop-up. The panel over the shop says **Waiting for other streamers** and who is still streaming. When the last stream ends, every player's stream summary (a column each) appears on that panel, and only then does **Ready for the Shop** appear. |
| **Shop layout** | One shop panel. The row under the title holds the streamer (portrait and active), **Raise Sub Cap**, **Remove a Viewer**, the **Audience**, the waiting text and **Go Live / Ready**. The viewer grid is underneath. |
| **Remove a Viewer** | A new shop button: pick one of your viewer cards and confirm, and it leaves your Audience for good. The first removal costs 2 Cash, then 1 more each time (up to 12). Your Audience can't go below 5 cards. The removed copy goes back to the shared shop stock. |
| **Shop summary (multiplayer)** | No pop-up. A panel at the bottom of the left column shows each player's shop: recruits, removals, Sub Cap, actives, Clout and Cash. It fades after 10 seconds; hovering it keeps it up. |
| **Emotes** | At most 4 emotes per player in any 12 seconds, at least 1.5 s apart. The host and the sender both check. The buttons dim while you have to wait, and a "slow down" toast tells you for how long. Each other player's row has a **mute** button that hides their emotes on your screen only; it turns red while muted. |
| **Scoring** | Each resource flies to its own menu-bar icon: Clout to Clout, Cash to Cash, and a Trophy viewer's +1 to the Trophy pips. Community rewards fly the same way. |
| **Plays → Trophies** | Renamed everywhere in all 5 languages (zh 奖杯, ru трофей, es Trofeo, pt Troféu). The icon is still the UI Skin's **IconStar** slot, now labelled "Trophies" in the Game Database. |
| **Players panel** | It grows with the number of players instead of scrolling. Each row shows only Clout, Cash and the Trophy icon with the number of Trophy viewers owned (no stream number, status or record). The game over table keeps the full details. |
| **Audience overview** | A **Communities** column on the right. For each community in your Audience it shows how many different viewers you own from it, the best score they could reach together in one chat, and the bonus at that score. Hover an icon for the rule. |
| **Viewer actions** | Click anywhere on a viewer in chat to use their action, not just the small button on the ring. |
| **Community scores** | A community needs **two different viewers** in chat to score: two score 1 point, three score 2, and so on. One viewer alone does nothing, so the Community Beats show 0 and nothing triggers. |

## 3. Where things are

- **New icons:** Game Database > **UI Icons (stream and shop)**, or the UI Skin directly.
  - **IconTimer:** the hourglass.
  - **IconRemoveViewer:** the shop button.
  - **IconMute:** the Players panel button.
  - All three have generated placeholders until you add art.
- **Timer options:** `TurnTimers` and `ShopTimers` in `NetTypes.cs`.
- **Emote limits:** `Emotes.Cooldown`, `BurstCount` and `BurstWindow` in `NetTypes.cs`.
- **Removal prices and the Audience minimum:** `GameRules.FirstRemovalCost`, `MaxRemovalCost` and `MinAudience`.
- **Community rule:** `Communities.MembersForFirstPoint` (= 2).
- **Shop recap panel timing:** **Visible Seconds** on the ShopRecap panel in the Game scene (`SidePanel/ShopRecap`).

## 4. Guidelines for future changes

- **Watching:** read-only viewing is controlled by `CanAct`, which is true only on your own board, and in Take Turns only on your turn. Anything new that sends commands must go through `Submit`, which checks it.
- **Timers:** the host keeps one deadline plus a stage (`MatchTimer.Stream` or `MatchTimer.Shop`). Start a timer with `ServerStartTimer(stage)`. Don't write to `m_TurnDeadline` directly.
- **Shop actions:** new purchases go into the toolbar row in `GameSceneBuilder.BuildPlanning`. Add the command to `IsShopCommand` and the marked-ready check in `DeckPartyNetSession`, so Free For All gates it like the others.
- **Summary columns:** build them with `GameSceneBuilder.RecapColumnTemplate` and fill them with `RecapColumns.Fill`. Both the pop-up and the shop gate use these.

## 5. Decisions I made (please check)

1. **Free For All timers:** the spreadsheet only described Take Turns timing, so I added both timers to Free For All as well. When the stream timer runs out, everyone is also marked ready for the shop, so a player who walked away can't block the round. Race has no timers.
2. **Shop timer presets:** Off, 30, 60, 90 or 120 seconds. The stream timer keeps its Off / Hard / Medium / Easy / Custom options.
3. **First timer of a match:** it gets 5 extra seconds while scenes load.
4. **Remove a Viewer:** your Audience must keep at least 5 cards. Starting viewers can be removed too.
5. **Community caps:** with "different viewers − 1", some maximums could no longer be reached with the current cards, so I lowered them to what is reachable. The bonus per point is unchanged.
   - Core Fanbase: 9 → 8
   - Content: 9 → 7
   - Gen-Z: 3 → 2
   - Hype Men: 3 → 2 (20%)
   - Mods: 3 → 2 (20%)
   - Voyeurs: 2 → 1
6. **Full Stream can't score now:** its only member card is Promo Code User, so it needs a second Full Stream card. Tag one in the Game Database.
7. **Trophy icon:** the Players panel uses the existing Trophy icon (IconStar), not a new slot, so one sprite covers every Trophy icon.
8. **Presence in Free For All:** presence updates (cursor and targets) are only sent while someone is watching you.
9. **Out of date:** `CommunityUpdateGuide.md` (community maximums) and `GameReference.md` still describe the previous rules.

## 6. Viewer card and community edits, and fixed button colours (2026-11-08)

These come from your edited `ViewerList.csv` and `Communities.csv`.

### Do this first in Unity

1. Let the scripts compile.
2. Run **Deck Party > Setup > Rebuild Scenes and UI Prefabs**. This adds Dancing Jen to the Game Database, gives Crypto Enthusiast and Dancing Jen their Millennials tag (no other viewer's communities change), rebuilds the green and red buttons and re-imports the strings.
3. Give Dancing Jen a portrait in the Game Database. Sprites named `DancingJen`, `Jen` or `Dancer` are picked up by **Deck Party > Art > Auto-Assign Sprites by Name**.
4. Multiplayer needs every player on this build (protocol `deckparty-12`). Saves still load.

### Viewer cards

| Card | Now |
|---|---|
| **Account Manager** | 3 Clout, −1 Cash. New action **Upsell**: one viewer in chat permanently earns +1 Cash (up to 9), like the Affiliate's Checkout Code. |
| **Crypto Enthusiast** | Costs 6. No Clout, no action (Rug Pull is gone). Millennials. All Crypto Enthusiasts in chat score Cash together: **0 / 1 / 4 / 9 / 16** in total for 1 / 2 / 3 / 4 / 5 of them. |
| **Dancing Jen** (new) | The original game's Dancer. Costs 7. Millennials. All Dancing Jens in chat score Clout together: **1 / 4 / 9 / 16 / 25** in total for 1 / 2 / 3 / 4 / 5 of them. |

### Communities

| Community | Now |
|---|---|
| Core Fanbase | 20% a point, up to 4 points (80%) |
| Staff | 30% a point, up to 3 points (90%) |
| Content | 30% a point, up to 3 points (90%) |
| Marketers | Up to 3 extra recruits |

The tooltips and the rules page show the new numbers in all 5 languages.

### Buttons and platforms

- **Add Viewer** is always green and **End the Stream** is always red. With 4 Trophies in chat, End the Stream still pulses, but it stays red.
- The platform now changes only the animated logo in the menu bar. Every other accent (Go Live, actions, Approve, Ready, the ring on viewers whose action is ready) uses the same colour on every platform: `SitePalette.Accent`.
- In Game Database > Platforms, the old **Button Colour** is now **Placeholder Colours**. It only colours the built-in placeholder logo, and only until you add a logo sprite.

### Where things are

- The group tables: `GameRules.DancingJenClout` and `GameRules.CryptoEnthusiastCash`. Group scoring is in `PartyEngine.ApplyPassive` and `GroupShare`.
- Per-card copy limits: the `maxCopies` argument in `GuestCatalog` (`GuestInfo.MaxCopies` and `ShopStock`).
- The fixed button colours: `SitePalette.Go` and `SitePalette.Stop`, used by `EditorUI.SiteButton.Go` and `SiteButton.Stop`.

### Decisions I made (please check)

1. **How a group's total is shown:** the total is split evenly across the copies in chat, so each of 3 Jens shows +3 (9 in all). If it doesn't split evenly, the first copy in chat takes the rest: with 2 Crypto Enthusiasts, the first scores +1 and the second 0. The total always matches your table. Community bonuses then apply to each copy's part. For example, Content gives each Jen its own chance to score its part again.
2. **5 copies:** the usual limit is 4 copies of a card. Dancing Jen and Crypto Enthusiast allow 5 (owned and in the shared shop), so the 25 step can be reached.
3. **Crypto's table (changed later on 2026-11-08):** 0 / 1 / 4 / 9 / 16 Cash in total for 1 to 5 in chat, so one on its own earns nothing and 5 earn 16. Scoring works the same way as before; only the numbers changed.
4. **Going Live:** Dancing Jen replaces Binge Bunny, which was standing in for the original Dancer. Brand Deal Wishlist keeps Binge Bunny (for the Cheerleader). Its description now says "Crypto hype" instead of "Rug pulls".
5. **Account Manager vs Affiliate:** both now have the same action and −1 Cash, but the Account Manager is cheaper (5 vs 7) and earns 3 Clout. Worth a look when you next balance the Affiliate.
6. **Marketers can't reach 3 yet:** only Producer and Marketing Manager are Marketers, so the most it can score is 1 point. Full Stream still can't score (only Promo Code User).
7. **Account Manager's stamp:** "UPSOLD!" when Upsell is used (all 5 languages). The old "RUG PULL!" stamp is no longer used.
