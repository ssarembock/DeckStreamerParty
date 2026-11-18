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
| **Remove a Viewer** | A new shop button: pick one of your viewer cards and confirm, and it leaves your Audience for good. The first removal costs 2 Influence, then 1 more each time (up to 12). Your Audience can't go below 5 cards. The removed copy goes back to the shared shop stock. |
| **Shop summary (multiplayer)** | No pop-up. A panel at the bottom of the left column shows each player's shop: recruits, removals, Sub Cap, actives, Cash and Influence. It fades after 10 seconds; hovering it keeps it up. |
| **Emotes** | At most 4 emotes per player in any 12 seconds, at least 1.5 s apart. The host and the sender both check. The buttons dim while you have to wait, and a "slow down" toast tells you for how long. Each other player's row has a **mute** button that hides their emotes on your screen only; it turns red while muted. |
| **Scoring** | Each resource flies to its own menu-bar icon: Cash to Cash, Influence to Influence, and a Trophy viewer's +1 to the Trophy pips. Community rewards fly the same way. |
| **Plays → Trophies** | Renamed everywhere in all 5 languages (zh 奖杯, ru трофей, es Trofeo, pt Troféu). The icon is still the UI Skin's **IconStar** slot, now labelled "Trophies" in the Game Database. |
| **Players panel** | It grows with the number of players instead of scrolling. Each row shows only Cash, Influence and the Trophy icon with the number of Trophy viewers owned (no stream number, status or record). The game over table keeps the full details. |
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
| **Account Manager** | 3 Cash, −1 Influence. New action **Upsell**: one viewer in chat permanently earns +1 Influence (up to 9), like the Affiliate's Checkout Code. |
| **Crypto Enthusiast** | Costs 6. No Cash, no action (Rug Pull is gone). Millennials. All Crypto Enthusiasts in chat score Influence together: **0 / 1 / 4 / 9 / 16** in total for 1 / 2 / 3 / 4 / 5 of them. |
| **Dancing Jen** (new) | The original game's Dancer. Costs 7. Millennials. All Dancing Jens in chat score Cash together: **1 / 4 / 9 / 16 / 25** in total for 1 / 2 / 3 / 4 / 5 of them. |

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

- The group tables: `GameRules.DancingJenCash` and `GameRules.CryptoEnthusiastInfluence`. Group scoring is in `PartyEngine.ApplyPassive` and `GroupShare`.
- Per-card copy limits: the `maxCopies` argument in `GuestCatalog` (`GuestInfo.MaxCopies` and `ShopStock`).
- The fixed button colours: `SitePalette.Go` and `SitePalette.Stop`, used by `EditorUI.SiteButton.Go` and `SiteButton.Stop`.

### Decisions I made (please check)

1. **How a group's total is shown:** the total is split evenly across the copies in chat, so each of 3 Jens shows +3 (9 in all). If it doesn't split evenly, the first copy in chat takes the rest: with 2 Crypto Enthusiasts, the first scores +1 and the second 0. The total always matches your table. Community bonuses then apply to each copy's part. For example, Content gives each Jen its own chance to score its part again.
2. **5 copies:** the usual limit is 4 copies of a card. Dancing Jen and Crypto Enthusiast allow 5 (owned and in the shared shop), so the 25 step can be reached.
3. **Crypto's table (changed later on 2026-11-08):** 0 / 1 / 4 / 9 / 16 Influence in total for 1 to 5 in chat, so one on its own earns nothing and 5 earn 16. Scoring works the same way as before; only the numbers changed.
4. **Going Live:** Dancing Jen replaces Binge Bunny, which was standing in for the original Dancer. Brand Deal Wishlist keeps Binge Bunny (for the Cheerleader). Its description now says "Crypto hype" instead of "Rug pulls".
5. **Account Manager vs Affiliate:** both now have the same action and −1 Influence, but the Account Manager is cheaper (5 vs 7) and earns 3 Cash. Worth a look when you next balance the Affiliate.
6. **Marketers can't reach 3 yet:** only Producer and Marketing Manager are Marketers, so the most it can score is 1 point. Full Stream still can't score (only Promo Code User).
7. **Account Manager's stamp:** "UPSOLD!" when Upsell is used (all 5 languages). The old "RUG PULL!" stamp is no longer used.

## 7. Card and community edits from StreamersViewersCommunities.md (2026-11-16)

These come from your edits to `StreamersViewersCommunities.md`. Where the viewer blocks and the community section
disagreed, the viewer blocks won, as you asked. The document has been rewritten to match the game (every card was
checked against the code).

### Do this first in Unity

1. Let the scripts compile.
2. Run **Deck Party > Setup > Rebuild Scenes and UI Prefabs**. This:
   - adds the Boomer Troll and the Billionaire Isolationist to the Game Database;
   - gives every roster viewer the new communities once (revision 7; your later edits rule);
   - adds the **Mods** chip to Community Beats;
   - re-imports the strings.
3. Give the two new viewers portraits in the Game Database. Files named `BoomerTroll` or `BillionaireIsolationist` are
   picked up by **Deck Party > Art > Auto-Assign Sprites by Name**. The Influential Isolationist keeps the old
   Gatekeeper's art.
4. Give the **Mods** an icon in Game Database > Communities (it shows a placeholder until then). The Gatekeepers keep
   their icon.
5. Multiplayer needs every player on this build (protocol `deckparty-17`). Saves still load (state format 11 reads 2-10).

### Viewer cards

| Card | Now |
|---|---|
| **Influential Troll** | Costs 5. +4 Influence (was +4 Cash), Risk 1. |
| **Boomer Troll** (new) | Costs 9. +3 Cash, +2 Influence, Risk 1. Troll Army, Boomers. |
| **Paid Sponsorship** | Costs 10 (was 4). +4 Cash, Risk 1 (was −1 Cash, +3 Influence). |
| **Influential Isolationist** (was Gatekeeper) | Costs 7. +1 Influence, and +1 Influence per empty seat. Core Fanbase. |
| **Billionaire Isolationist** (new) | Costs 6. +1 Cash, and +1 Cash per empty seat. Core Fanbase, Boomers. |
| **PR Manager** | Damage Control sends 3, 6 or everyone else back, by the Gatekeepers' points. |
| **Billionaire Benefactor** | +4 Influence (was +5). |
| **Mainstream Celebrity** | +4 Cash, +1 Influence (was +3 Cash). |
| **Loyal Fan, Fish** | Sold in every shop (Random Mix too). You can buy 4 more of each on top of the starting copies. |

Communities moved as you set them in the viewer blocks:

- **Mods:** Chat Admin, Tech Admin, Platform Owner, Bot Detector.
- **Gatekeepers:** PR Manager, Distraught Cat.
- **Boomers:** joined by Mildly Risky Collaborator, Mainstream Celebrity and the two new cards.
- **Millennials:** joined by Highly Risky Collaborator and Doomer (Doomer is also Troll Army).
- **Gen-Z:** joined by Precocious Youth.

### Communities

| Community | Now |
|---|---|
| **Mods** | The old Gatekeepers rule: each point gives one more use of a Mod's removal action this stream (shared). No cap. The Mods' own old rule (a chance to cancel 1 Risk) is gone. |
| **Gatekeepers** | A Gatekeeper's action sends random other viewers back to the queue: 3 at 0 points, 6 at 1, everyone else at 2. Never itself. |
| **Hype Men** | Each point (up to 2) is a spare seat past the Sub Cap. Only a viewer a Hype Man brings in can take one, so it doesn't crash the server. Free spare seats show as grey dotted circles after the normal seats; a viewer sitting in one gets a grey dotted ring. Add Viewer still stops at the Sub Cap. |
| **Boomers** | Levels: 2 different Boomers make level 1, 4 make level 2. Each level takes 1 Influence off every Sub Cap upgrade **and every use of your streamer's active** in the next shop (never below 0). The tooltip says "Level 1/2". |
| **Millennials** | The inheritance is +1 **Cash** (was Influence), once per point (at least once). |

The tooltips, Community Beats chips, rules page, set descriptions and logs say all this in all 5 languages.

### Sets

Every shop sells Loyal Fan and Fish on top of its list. Random Mix sells them plus 12 random viewers and 3 Trophies.

| Set | Out | In |
|---|---|---|
| Going Live | Cybersecurity Engineer | Junior PR Manager (a Gen-Z partner for the Precocious Youth) |
| Clickbait Roulette | Tech Admin | Boomer Troll |
| Brand Deal Wishlist | Tech Admin, Hate-Watcher | Generous Donator, Boomer Troll (3 Hype Men, 4 Boomers) |
| Monetization Meeting | Risky Orbiter | Billionaire Isolationist (a second Boomer) |
| Algorithm Magic | Normie, Gen Z with Job | Billionaire Isolationist, PR Manager (kicks leave empty seats for the Isolationists) |

Section 5 of `StreamersViewersCommunities.md` has the full lists and the strongest communities in each set.

### Where things are

- **Community numbers:** `Communities` in `Core/Community.cs`. The relevant members are:
  - `PointsFromMembers` (the Boomers' levels);
  - `BoomerDiscount`;
  - `GatekeeperSendBack` (the 3 / 6 table);
  - `HypeSeats`;
  - `InheritanceCash`.
- **Spare seats:** in `PartyEngine` (`HypeSeats`, `HypeSeated`, `FreeHypeSeats`, `SettleHypeSeats`, the overflow check in `Arrive`).
  - The flag is `GuestInstance.HypeSeat`.
  - The chat draws them in `HouseView.Render` and `GuestCardView.ShowSpareSeat`.
  - The dotted ring is `RuntimeSprites.DottedRing`.
- **Send-back count:** `PartyEngine.SendBackCount`.
- **The Boomers' discount on actives:** `StreamerCatalog.Cost` (and `BaseCost` without it).
- **Always-in-shop viewers:** `ScenarioCatalog.AlwaysInShop`.

### Decisions I made (please check)

1. **Bot Detector is a Mod.** Its block still said "Gatekeepers (Gatekeepers points give extra uses)", which describes
   the old rule (now Mods). It also picks its two viewers, and you described Gatekeepers as the random kickers.
2. **Distraught Cat is a Gatekeeper** (its block says Gatekeepers). It has no action, so it adds a point and gains from
   every viewer the PR Manager sends back. With only two Gatekeepers, the "everyone else" level can't be reached yet.
3. **Millennials:** I read "This bonus increases per Millennials point" as the existing "once per point" rule, so
   each departing Boomer gives +1 Cash to a random Millennial once per point. If you meant a bigger single bonus (for
   example +2 Cash at 1 point), say so.
4. **"Brought in by a Hype Man":**
   - These count: the viewers Online Journalist and Bigger Star bring along, the Generous Donator's guest, and the
     Evangelist's 2-for-1!. The Evangelist's action works in a full chat while a spare seat is free.
   - A Marketing Manager's guest doesn't count.
   - When a normal seat frees up, a viewer in a spare seat moves into it.
5. **Loyal Fan and Fish copy limits:** with the usual limit of 4 (starting copies count), you could never buy a Loyal
   Fan. You can now buy 4 more of each on top of the starting copies.
6. **Spelling:** "Influencial Isolationist" is spelled **Influential Isolationist** in the game, like the Influential
   Troll.
7. **Old card names:**
   - **Art:** the old Gatekeeper's art and Game Database entry stay with the Influential Isolationist (same slot).
   - **Strings:** the `char.Gatekeeper.*` keys were renamed to `char.InfluentialIsolationist.*`.
8. **Translations** of the new names:
   - Boomer Troll: 婴儿潮喷子 / Тролль-бумер / Trol boomer / Troll boomer.
   - Influential Isolationist: 大V孤立主义者 / Влиятельный изоляционист / Aislacionista influyente / Isolacionista influente.
   - Billionaire Isolationist: 亿万富翁孤立主义者 / Изоляционист-миллиардер / Aislacionista multimillonario / Isolacionista bilionário.

## 8. The Lurkers and five risky variants (2026-11-18)

### Do this first in Unity

1. Let the scripts compile.
2. Run **Deck Party > Setup > Rebuild Scenes and UI Prefabs**. This:
   - adds the five new viewers to the Game Database;
   - gives the Lurkers to the cards that have them by default (revision 8). It only adds Lurkers, so your other
     community edits are kept;
   - adds the **Lurkers** chip to Community Beats;
   - re-imports the strings.
3. Give the **Lurkers** an icon in Game Database > Communities. Until then they show a placeholder: two eyes peeking over
   a wall.
4. Optional: give the new viewers their own art. Until you do, each one shows its original's art; the Game Database
   row says "[shows Billionaire Isolationist's art]" and so on. Files named after the card (for example
   `BunkerBillionaire`) are picked up by **Deck Party > Art > Auto-Assign Sprites by Name**.
5. Multiplayer needs every player on this build (protocol `deckparty-20`). Saves still load (state format unchanged).

### The Lurkers

| | |
|---|---|
| **Rule** | When the stream is scored, each point earns **+1 Cash for every empty seat** in chat. It flies from the Lurkers chip. |
| **Cap** | 2 points, so at most +2 Cash per empty seat. |
| **Example** | Sub Cap 8 with 5 viewers in chat leaves 3 empty seats. With 3 different Lurkers among them (2 points), the stream earns +6 Cash. |
| **Members** | Influential Isolationist, Billionaire Isolationist, Forever Alone, Risky Orbiter, and the new Bunker Billionaire and Terminally Online. |

Empty seats are the ones the Isolationists already count, so the two stack. In a team game both halves of the chat
count. The tooltip shows the Cash the stream would earn right now. The chip shows "+1/space" or "+2/space", and the
rules page has a Lurkers line.

### Risky variants

Each one is an existing card with no Risk, given **Risk 1**, a **3 lower cost** and a new name. It keeps its
original's stats, rules and communities.

| New card | Copies | Cost | Scores | Rule | Communities |
|---|---|---|---|---|---|
| **Bunker Billionaire** | Billionaire Isolationist | 3 | +1 Cash | Doomsday Bunker: +1 Cash per empty seat | Core Fanbase, Boomers, Lurkers |
| **Terminally Online** | Forever Alone | 5 | +1 Cash | Never Logs Off: +10 Cash if alone, otherwise +1 Cash per empty seat | Millennials, Lurkers |
| **Power-Tripping Mod** | Tech Admin | 6 | +2 Cash | Action Ban Hammer: remove one viewer (even itself) | Mods |
| **Data Broker** | Social Media Manager | 5 | +3 Cash, −1 Influence | Action Harvest Data: peek at the next viewer, let them in or keep them out | Staff, Voyeurs |
| **Tabloid Reporter** | Online Journalist | 8 | +2 Cash, +3 Influence | Exclusive Scoop: brings two viewers along when joining | Hype Men |

They're in **Open Lobby** and can come up in **Random Mix**. They're not in any preset set yet; add them in
`ScenarioCatalog` (or tell me which sets) if you want them there.

### Where things are

- **Lurkers numbers:** `Communities.LurkersCashPerSpace` / `LurkersMaxPoints` in `Core/Community.cs`.
- **Lurkers payout:**
  - `PartyEngine.LurkersCash`, paid in `ScoreCommunityRewards`;
  - counted in `ProjectScore`.
- **Variants:**
  - `GuestCatalog.VariantDiscount` (3) and the `s_VariantOf` table. `GuestCatalog.BehavesAs` makes the engine score a
    variant as its original.
  - The cards themselves are at the end of `GuestCatalog.Build`. Keep them in step if you change an original.
- **Art fallback:** `GameDatabase.GetGuestArt` falls back to the original. `GetGuestArtEntry` is the card's own entry,
  which the art tools use. The action effect falls back to the original's in `SignatureFx.TryGet`.

### Decisions I made (please check)

1. **Which cards:**
   - Only cards with no Risk and no Risk cancel could be copied.
   - I left out group cards, Trophy viewers and very cheap cards (cost 5 or less), which would have cost 2 or less.
   - Two of the five are Lurkers, so the new community has 6 members.
2. **The Risky Orbiter is a Lurker.** It was the only Lurker candidate with no community.
3. **Power-Tripping Mod** can remove itself, taking its Risk with it, like the Tech Admin.
4. **Names and stamps:**
   - New action names: Ban Hammer and Harvest Data.
   - New stamps: BANNED! and DATA SOLD!
   - Translations:
     - Lurkers: 潜水党 / Молчуны / Fantasmas / Fantasmas.
     - Bunker Billionaire: 地堡富豪 / Миллиардер из бункера / Multimillonario del búnker / Bilionário do bunker.
     - Terminally Online: 网瘾晚期 / Хронически онлайн / Crónicamente online / Cronicamente online.
     - Power-Tripping Mod: 滥权房管 / Мод-самодур / Mod tirano / Mod tirano.
     - Data Broker: 数据贩子 / Торговец данными / Vendedor de datos / Corretor de dados.
     - Tabloid Reporter: 八卦小报记者 / Репортёр жёлтой прессы / Reportero sensacionalista / Repórter de fofoca.

## 9. Minor changes: risky viewers in every set, competitive defaults, 30-seat chat, no maximum prices (2026-11-18)

### Do this first in Unity

1. Let the scripts compile.
2. Run **Deck Party > Setup > Rebuild Scenes and UI Prefabs**. This:
   - leaves more room under the viewers in the Stream Chat window (32 px instead of 8);
   - re-imports the strings.

   The new grid works in an old scene too; only the extra room needs the Rebuild.
3. Multiplayer needs every player on this build (protocol `deckparty-23`). Saves still load: state format 15 reads
   older saves, and those keep 4 Trophies to win.

### Character sets

Each preset set gets one more viewer carrying Risk:

| Set | Added | Why |
|---|---|---|
| Going Live | Data Broker | Cheap staff, which fits the set's "staff to run the room". |
| Clickbait Roulette | Tabloid Reporter | Clickbait journalism. |
| Brand Deal Wishlist | Paid Sponsorship | The brand deal itself, and another Boomer. |
| Monetization Meeting | Mildly Risky Collaborator | A third Boomer; when one logs off, a Millennial inherits. |
| Algorithm Magic | Risky Orbiter | The set had no Risk at all; this adds a fourth Lurker. |

I didn't put a risky variant in a set that already has its original. Until the variants get their own art they look
exactly like the original, and names are hidden on cards. That ruled out:
- Data Broker in Monetization Meeting (it has the Social Media Manager);
- Power-Tripping Mod in Algorithm Magic (it has the Tech Admin).

**Random Mix** now draws **13** regular viewers (it was 12). At least **3** of them carry Risk: three are drawn from the
risky viewers first, then the rest from everyone left. The Hate-Watcher can still come up, but it doesn't count toward
the three, because its Risk only comes every other time it joins.

### Competitive multiplayer: 3 Trophies, 20 streams

| | Single player (Puzzles, Versus the AI) | Competitive multiplayer (online Take Turns, Free For All, 2v2, and Couch Competitive) |
|---|---|---|
| Trophies to win | 4 (unchanged) | **3** |
| Streams | 25 | **20** by default (online lobbies still offer 10 to 30) |

- **Where the goal lives:** every season stores its own goal (`PlayerState.TrophiesToWin`).
- **Where it shows:** every line that mentions it uses the season's own number: the menu bar's "n/3", the win hint,
  the WIN button, banners, the log, the game-over screen and the pause menu's quick rules.
- **Fixed texts:** the lobby summary, the multiplayer screen subtitle and the multiplayer rules page now say 3.
- **Co-op Endless** has no win, so nothing changes there.
- **Versus the AI** stays at 4 Trophies. It's single player, even though it's competitive.

### The stream chat: 3 rows of 10, no scrolling

- **Seats per player:** a player's chat holds at most **30** seats (3 rows of 10). The chat never scrolls: the avatars
  resize to fit.
- **Teams (2v2, co-op):** each player has their own 30 seats on their half of the chat, so 60 in all.
- **Layout:** the game picks the row length that gives the biggest avatars, up to 10 to a row and 3 rows. For example,
  10 seats show as 2 rows of 5 rather than 1 small row of 10. A layout with more rows is only used when it makes the
  avatars at least 8% bigger, so a short chat stays on one row.
- **The 30-seat limit in the rules:**
  - The Sub Cap now stops at **30** (it was 34).
  - The Hype Men's spare seats only appear while the chat is under 30 seats. At a Sub Cap of 29 there is 1 spare seat;
    at 30 there are none.
  - Bro Jogan's Three-Hour Episode can't be used at a Sub Cap of 30 ("Your Sub Cap is maxed out.").
- **One brief exception:** the viewer who crashes a full chat's server shows for a moment in a fourth row before the
  stream ends.

### No maximum prices

Every shop price that grew with use now grows without end:
- **Raise Sub Cap:** 2, 3, 4... Influence (it stopped at 12).
- **Streamer actives:** +1 Influence per use (they stopped at 12).
- **Remove a Viewer:** 2, 3, 4... Influence (it stopped at 12).

The Boomers' discount still applies. Endless's Trophy prices never had a maximum.

### Where things are

- **Sets:** `ScenarioCatalog` (`s_All`). Random Mix: `RandomRegularGuests` (13) and `RandomRiskyGuests` (3), in
  `BuildShop`.
- **Trophies and streams:**
  - `GameRules.CompetitiveTrophiesToWin` (3) and `GameRules.CompetitiveStreams` (20); `GameRules.DiamondsToWin`
    (4, single player).
  - `PartyEngine.TrophiesToWin(state)` reads a season's goal.
  - `DeckPartyNetSession.MatchTrophiesToWin` is the online goal.
  - The lobby's default streams option is `LobbyScreen.DefaultNightsIndex`.
- **Seats:**
  - `GameRules.HouseColumns` (10), `HouseMaxRows` (3) and `MaxCapacity` (their product, 30).
  - `PartyEngine.HypeSeats` / `HypeRoom` (spare seats) and `EpisodeRoom` (Three-Hour Episode).
  - The layout is `HouseView.BestColumns`; the room under the viewers is `GameSceneBuilder.HouseBottomGap`.
- **Prices:** `GameRules.ExpansionCost`, `GameRules.RemovalCost`, `StreamerCatalog.BaseCost`.

### Decisions I made (please check)

1. **The five additions** are the ones in the table above. Swap any of them in `ScenarioCatalog`.
2. **"Carrying Risk"** means a card with a printed +1 Risk. The Hate-Watcher doesn't count toward Random Mix's three.
3. **Couch Competitive** counts as competitive multiplayer (3 Trophies, 20 streams). Versus the AI keeps 4 Trophies and
   25 streams.
4. **There's no lobby setting for the Trophy goal.** Every competitive match uses 3. A setting (3 / 4 / 5) is easy to add
   if you want one.
5. **The 30-seat limit is part of the rules,** not just the layout. Without it the Sub Cap upgrades, the Hype Men and
   Three-Hour Episode could go past 3 rows.
6. **Room under the stream:** "a little more vertical space below" was read as 24 px more between the last row of
   avatars and the footer (the near-deplatform warning and the Next Up faces). Change `HouseBottomGap` if you want more.

## 10. Team fixes, shop summary, notices, narrower left column, 40-seat team chat, Remove a Viewer 3 +2 (2026-11-18)

### Do this first in Unity

1. Let the scripts compile.
2. Run **Deck Party > Setup > Build Everything** (or **Rebuild Scenes and UI Prefabs**) once, so the new strings are
   imported. Nothing else needs a Rebuild: the Audience columns, the second avatar on team summaries, the notices' new
   place, the narrower left column and the removed bar all happen at runtime in the current scenes.
3. Multiplayer needs every player on this build (protocol `deckparty-24`, state format 16).

### Teams: each player has their own streamer

- **The pick:** in 2v2 and Co-op every player picks their own streamer. It used to be one streamer per team, picked by
  the team's first player.
- **No doubles:** teammates can't play the same streamer.
  - Once your teammate locks one in, its tile shows **TEAMMATE** and Confirm stays off on it. The details under the
    portrait say who picked it.
  - If you both confirm the same one at the same moment, the first to reach the host keeps it. The other player gets an
    error sound and picks again; their timer keeps running.
  - **Random**, and the timer running out, never land on your teammate's streamer.
- **Your own active:** its own price ladder, cooldown and armed state, paid from the team's shared Influence. The shop's
  active button shows your streamer; when you watch another player, it shows theirs.
- **Your own face:** on your board the facecam, the anxiety footer, the menu bar's platform logo and the automatic stream
  setup follow your own streamer. The Players panel shows each player's streamer, and so does the game over screen.
- **What each active touches in a team:**

| Your side of the chat | The whole team |
|---|---|
| Giveaway (your viewers earn the extra Cash) | Smoke Bomb (the team has one Risk count) |
| Bark at Chat (your viewers carrying Risk) | Soft Landing |
| Double Move (the first action used on your side) | Subathon (any Server Down ends the shared stream) |
| X-Ray Vision (your queue) | Double or Nothing (the stream's Cash is shared) |
| Three-Hour Episode (+1 on your Sub Cap only) | Beautiful Sequence (the team's streak) |
| Pal Sphere (the catch joins your Audience) | Raid and Wealth Tax (they hit another team) |

- **Rival powers in 2v2:** the "who do you hit" choice lists each other team once, by both names, and never your own
  teammate (it listed everyone before, teammate included).

### Teams: your teammate's moves no longer close your panels

- **Pickers:** the viewer picker (Remove a Viewer, Fetch and the others) now closes only when the season moves to
  another phase, such as the stream ending. Before, any state update from your teammate closed it.
- **Your own feedback:** your teammate's purchases, removals, Sub Cap raises and actives no longer fly a "+1" to your
  Audience button, punch your Raise Sub Cap or active button, or say "You removed...". The active's banner still shows,
  so you see what they used.
- **The log:** the Activity log names your teammate for their moves instead of saying "you".

### Audience window in team games

- **Two columns:** each player's viewer cards are in their own column with their name and card count on top, split by a
  grey line. Your column says "(you)".
- **Streamers:** the summary above the columns lists both players' streamers.
- **Solo seasons:** they keep the single grid.

### Summaries in team games: both players

- **Names and faces:** stream and shop summaries now show one column per team. It reads **"Ana and Bo"** (member 0 first,
  each name in its player colour), with both streamers' faces side by side and "Streamer A and Streamer B" under the
  names.
- **The old problem:** before, a team's summary was filed under whichever teammate had acted, so it showed one player.
  The shop summary could also wait for the "other" teammate and never show.

### Multiplayer shop summary: four icons

- **What the panel shows:** the panel at the bottom of the left column now has, per player or team, only the name and
  one row of four icons:

| Icon | Shows |
|---|---|
| Audience (UI Skin: Icon Audience) | Viewer cards gained or lost in the shop: recruits and Pal Sphere catches are +, removals are - |
| Cash | The Cash change |
| Influence | The Influence change |
| Active (UI Skin: Icon Action) | Actives used in that shop (both players' in a team) |

- **Nothing changed:** a value that didn't change shows a grey 0.
- **Hovering:** hover an icon to name it.
- **Where the icons come from:** the same UI Skin slots as the buttons, so your button art shows here too.

### Notices at the bottom left

- **Where they show:** in multiplayer (online and Couch Competitive) the notices that used to drop over the menu bar
  appear at the bottom of the left column instead. This covers headlines such as "Ana raided Bo" and the messages when
  the game turns a move down.
- **With the shop summary:** while the shop summary shows, they sit just above it.
- **Size:** they are as wide as the column and grow taller for longer text.
- **Single player:** Versus the AI included, the notices stay at the top.

### Narrower left column

- **The width:** the left column (Players, emotes, Activity, shop summary) is three quarters of its old width, 315 to 390
  px instead of 420 to 520. The Stream Chat gets the space.
- **Old scenes:** current scenes switch to the new width by themselves (`ContentFrame.widthRevision`).
- **The players' rows:** they pack themselves for the narrower column (`PlayerRowView.FitCompact`). Rank, colour dot and
  streamer are on the left; mute, ready and host on the right, only those showing. The name auto-sizes.

### The bar above the stream is gone

- **What was removed:** the strip of text between the menu bar and the Stream Chat in multiplayer ("Your stream",
  "Watching Ana", "Ana's turn", the Free For All round status, and its buttons).
- **Switching boards:** click a name in the Players panel; your own name brings you back.
- **The Round Summary button:** it lived on that bar, so the round summary pop-up is no longer reachable. Free For All's
  stream recaps are on the shop gate. `RoundSummaryView` is still in the scene, unused.

### Team chat: 40 seats a player, 4 rows of 10

- **The limit:** in 2v2 and Co-op each player's Sub Cap can reach **40**, and their side of the chat takes up to 4 rows
  of 10. Solo chats stay at 30 in 3 rows.
- **Spare seats:** the Hype Men spare seats and Three-Hour Episode's extra seat respect the 40 too.

### Remove a Viewer: 3, 5, 7, 9...

- **The price:** the first removal costs **3** Influence, and each one after it costs **2** more. It was 2, then +1.
- **Still the same:** no maximum, a separate ladder for each team player, and the Audience can't go below 5 cards.
- **The texts:** the hover text and the How to Play money page say so (the money page also no longer claims you can't
  remove viewers).

### Where things are

- **Team streamers:**
  - The state: `PlayerState.PartnerStreamer` / `PartnerPowerUses` / `PartnerPowerCooldown` / `PartnerPowerArmed`, read
    through `StreamerOf(member)`, `PowerUsesOf`, `PowerCooldownOf`, `PowerArmedOf` and `MemberWithStreamer`.
  - The rules: `PartyEngine.GetPowerBlocker(s, rivals, member)`, `UsePower`, `UseRivalPower(..., actorMember)`,
    `ApplyArmedPower`, and `StreamerCatalog.Cost(s, member)`.
- **The pick:**
  - The host: `DeckPartyNetSession.TeammateLockedStreamer`, `StreamerTakenRpc` / `StreamerPickTaken`, and
    `ResolveDistinct`.
  - The screen: `StreamerSelectScreen` (`RefreshTaken`, `OnPickTaken`) and `StreamerTileView.SetTaken`.
- **Teammate moves:** `GameController.OnPlayerState` (`partner`), `GuestPickerView.OpenedPhase`, and
  `ProcessEvents(..., partner)`.
- **Audience columns:** `RolodexView` (`SetTeamLayout`, `MemberName`).
- **Summaries:**
  - The logic: `GameController.Recap.cs` (`RecapKey`, `RecapUnits`, `TeamPair`, `BaseEntry`, `CompactShopEntry`).
  - The columns: `RecapColumnView` (the second avatar).
  - The inline icons: `GameIcons.AudienceTag` / `ActiveTag`.
- **Notices:** `ToastView.DockTo`, called from `GameController.DockNotices`.
- **The left column:** `ContentFrame` (`minSideWidth` 315, `maxSideWidth` 390) and `PlayerRowView.FitCompact`.
- **Seats:** `GameRules.TeamMaxCapacity` (40), `TeamHouseMaxRows` (4) and `MaxCapacityFor(team)`; `PartyEngine.MaxCapacity(s)`;
  `HouseView.TeamMaxRows`.
- **Removal price:** `GameRules.FirstRemovalCost` (3) and `RemovalCostStep` (2).

### Decisions I made (please check)

1. **Active effects in a team:** see the table above. Effects on viewers or seats belong to their owner's side; effects on
   the shared stream, Risk or wallet belong to the team.
2. **First to lock in keeps a streamer;** the other teammate picks again.
3. **The Round Summary pop-up** went with the bar (the request said the bar wasn't needed). Say if you want its button
   back somewhere else, for example in the pause menu.
4. **Notices move in Couch Competitive too** (it's local multiplayer). Single player and Versus the AI keep them at the top.
5. **The shop summary's Audience number** counts viewer cards in the Audience, so a Pal Sphere catch counts +1, like a
   recruit.
6. **The "Player N" fallback** names a team column only if a player's name can't be found (it reuses the couch string).

## 11. Bigger chats, the Hype Men's row, live menu-bar totals, 20-stream Endless (2026-11-18)

### Do this first in Unity

1. Let the scripts compile.
2. Run **Deck Party > Setup > Build Everything** once, so the updated rules text is imported. No Rebuild is needed: the
   chat lays out its rows at runtime.
3. Multiplayer needs every player on this build (protocol `deckparty-25`; the state format is still 16, so saves load).

### Seat limits

| Mode | Sub Cap limit | Rows of 10 |
|---|---|---|
| Single player, Versus the AI, Take Turns, Free For All, Couch | **40** (was 30) | 4 |
| 2v2 and Co-op Endless | **50 a player** (was 40) | 5 on each side |

- **Raise Sub Cap** stops at the limit, and so does Three-Hour Episode's extra seat.
- The chat still never scrolls: the avatars shrink to fit the extra rows.

### The Hype Men's row

- The Hype Men's spare seats (up to 2) no longer count against the limit. Before, a Sub Cap at the limit left no spare
  seats, and one seat short of it left only one.
- When the Sub Cap is at the limit, or one short of it, the spare seats show in **one extra row** under the full rows.
  The extra row keeps the 10-seat column grid, so it lines up with the rows above. In a team it goes on the side of the
  first Hype Man in chat, as the spare seats did before.
- The same row also holds the viewer who crashes a full chat's server, for the moment before Server Down.

### The menu bar counts up during the scoring

- In the end-of-stream scoring sequence, each chip adds its amount to the menu bar **when it lands**:
  - a Cash chip adds to Cash;
  - an Influence chip adds to Influence (or takes off a fee);
  - a community reward chip (Staff, Troll Army, Lurkers...) adds its own amounts.
  The number punches each time.
- **Trophies:** in Endless a Trophy chip banks its Trophy as it lands (one fewer "+n in chat", one more banked). In the
  other modes the Trophy count already counts the viewers in chat, so the landing only punches it.
- The final totals appear after the last chip lands (the sequence waits up to 1.5 s for it). They also include what
  never flies:
  - fees the bank couldn't pay, and their Cash penalty;
  - Double or Nothing's bonus;
  - what the stream's pot collected during the stream.
  Only the numbers that differ from the running count punch again.
- During the count, Cash and Influence never show below 0. Fees are settled against the whole stream's Influence at
  the end, as before.
- **Skipping** the sequence (click or Space) shows the final totals at once; chips still in the air change nothing.
- Money banked during a stream (Crypto Guy, Generous Donator) is unchanged: the menu bar updates at once and the chip
  flies after.

### Endless

- A run starts with **20 streams** (was 25), in single player and in Co-op Endless. The lobby summary and the opening
  headline read the new number.

### Where it is in code

- **Limits:**
  - `GameRules.HouseMaxRows` (4), `TeamHouseMaxRows` (5), `MaxCapacity` (40), `TeamMaxCapacity` (50);
  - `GameRules.HypeRowSeats` (10), the room past the limit for spare seats;
  - `PartyEngine.HypeSeats` (no longer capped by the limit) and `HypeRoom`.
- **The extra row:** `HouseView.BestColumns` keeps 10 to a row once the seats pass the full rows.
- **Live totals:**
  - `HudView.BeginTally` / `Tally` / `EndTally`;
  - `GameController.FlyResources(..., tally)` with `Landing`, and `m_ChipsInFlight`;
  - `EffectsView.FlyChip(..., landed)`.
- **Endless:** `GameRules.EndlessStartingStreams` (20).

### Decisions I made (please check)

1. **Spare seats only past the limit.** Below the limit they work as before. Each spare seat past it is one more avatar,
   so a full solo chat can show 42 seats for a moment.
2. **The extra row isn't only for spare seats.** Viewers keep their joining order, so a viewer sitting in a spare seat
   can be in an earlier row, with the last normal viewer in the extra row. It's the same number of seats either way.
3. **Mid-stream banking** (Crypto Guy, Generous Donator) still updates the number at once. The request was about the
   end-of-stream scoring; say if you want those to count up on landing too.

## 12. Community updates: Boomers' Early Retirement, Millennials, Core Fanbase, Legacy Media Mogul (2026-11-18)

### Do this first in Unity

1. Let the scripts compile.
2. Run **Deck Party > Setup > Build Everything** once. It imports the new strings and adds the Legacy Media Mogul to
   the Game Database (no portrait yet: give it one under Viewers). It also saves the Boomer change below into the
   database. A Rebuild isn't needed.
3. Multiplayer needs every player on this build (protocol `deckparty-26`; the state format is unchanged, so saves load).

### Boomers: Early Retirement

- **The action:** every Boomer has the action **Early Retirement**, unless it has an action of its own.
- **What it does:**
  - The whole chat scores right now, as if the stream ended: every viewer, the community rolls (Core Fanbase scoring
    twice), then Staff, Troll Army and Lurkers.
  - The money is banked at once (fees too), with the same scoring animation; the menu bar counts up as the chips land.
  - **The stream goes on**, and scores again when it ends.
  - Trophies, the stream's pot, investments, Double or Nothing and Checkout Codes wait for the real end.
- **Then the Boomer retires** (leaves your Audience for good). The Boomers' points lower the chance:

  | Different Boomers in chat | Points | Chance it retires |
  |---|---|---|
  | 1 | 0 | 100% |
  | 2 | 1 | 50% |
  | 3 or more | 2 (the cap) | 25% |

  A Boomer that stays shows "isn't ready to retire yet". It has still used its action for this stream.
- **Confirmation:** using it asks first, because clicking a chat avatar uses its action. The dialog shows what the chat
  would bank (without the random rolls) and the chance to retire.
- **Removed:**
  - the Boomer levels (2 and 4 Boomers); Boomers score like every other community now;
  - the 10% chance to retire after every scored stream;
  - the shop discount on Sub Cap upgrades and the streamer's active.
- **Community Beats:** the Boomers chip shows the current chance to retire (100% / 50% / 25%).

### Which viewers are Boomers now

- **Left the Boomers** (they have their own action, which would hide Early Retirement):
  - **Billionaire Benefactor** now has no other community and isn't a Trophy viewer, so it joined the **Core Fanbase**.
  - **Platform Owner** stays a **Mod**. (Had it been left with no community, it still wouldn't have joined the Core
    Fanbase, because it's a Trophy viewer.)
  - **Karen** (put aside) is now Troll Army and Armchair Activists.
- **Your Game Database:** the same rule runs once over your community edits (Game Database communities revision 9).
  Any card you made a Boomer that has its own action leaves the Boomers too. Nothing else in your edits changes.
- **Boomers now** (10): Paid Sponsorship, Mildly Risky Collaborator, Billionaire Isolationist, Master of Currency,
  Executive PR, Boomer Troll, **Legacy Media Mogul** (new), Big Boomer, Mainstream Celebrity, Bunker Billionaire.
- **Later edits:** making a card without an action a Boomer in the Game Database gives it Early Retirement. A card
  with its own action keeps that action, even as a Boomer.

### The new Boomer: Legacy Media Mogul

- **Card:** cost **12**, scores **+4 Cash and +3 Influence**, no Risk, community **Boomers**.
- **Its action:** Early Retirement, like every Boomer.
- **Why it's "strong":** big printed numbers, which its Early Retirement cashes out twice in one stream.
- **Where it shows up:** it isn't in any preset set (Open Lobby and Random Mix only).
- **Identity:** `GuestType.LegacyMediaMogul` = 92, with the strings `char.LegacyMediaMogul.*`.

### Millennials

- **Inheritance:** when a Boomer retires, a random Millennial you own permanently earns **+1 Cash, +1 more per
  Millennials point**.
- **Cap:** 2 points, so +3 at most, with 3 different Millennials in chat at the moment the Boomer retires.
- **Unchanged:** the heir comes from your whole Audience, and printed Cash stops growing at 9.
- **Before:** +1 for each point.
- **Display:** the Millennials chip shows the inheritance (+1 / +2 / +3), and the stream summary names the amount.

### Core Fanbase

- **Cap:** up to **5 points: a 50% chance** that each Core Fanbase viewer scores twice. That takes 6 different Core
  Fanbase viewers in chat.
- **Before:** 3 points, 30%.

### Where it is in code

- **The action:**
  - `AbilityKind.EarlyRetirement`.
  - `GuestInfo.Ability` is now a property: the card's own action (`OwnAbility`), or else the community's
    (`GuestCatalog.CommunityAbility`). `GuestInfo.HasCommunityAbility` tells which.
  - Text: `GuestVisuals.ActionName` / `CommunityActionText` (strings `ability.EarlyRetirement.name` / `.desc`).
- **The rules:**
  - `PartyEngine.EarlyRetirement` and `RetireBoomer`.
  - `PartyEngine.ProjectEarlyRetirement` and `EarlyRetirementChance` (used by the dialog).
  - `Communities.BoomerRetireChance`, `InheritanceAmount`, `BoomerMaxPoints` 2, `MillennialMaxPoints` 2,
    `CoreFanbaseMaxPoints` 5.
- **Events:** `GameEventType.EarlyRetirement` (90) and `BoomerStayed` (91). `BoomerRetired` now happens mid-stream.
- **Screen:**
  - `GameController.ConfirmEarlyRetirement`.
  - `TryStartScoring` / `ScoringSequence(..., retirement)`.
- **Who's a Boomer:**
  - `GuestCatalog.WithoutActionBoomers` (the rule above).
  - `GameDatabase.CommunityDefaultsRevision` 9.
- **Versus the AI:** the AI never uses Early Retirement on a Trophy viewer, nor while the chat would bank little
  (`SimpleAI.WastesEarlyRetirement`).

### Decisions I made (please check)

1. **"Scores the entire stream as is"** was read as scoring the chat now while the stream goes on. Ending the stream
   early would do nothing End the Stream doesn't already do, and would still cost the Boomer. So a Boomer
   cashes the chat out an extra time.
2. **What that scoring includes:**
   - In: viewers, community rolls and the Staff / Troll Army / Lurkers rewards.
   - Out: Trophies (no win, no Endless banking), the pot, investments, Double or Nothing and Checkout Codes. Otherwise
     one stream could bank Trophies or permanent bonuses twice.
3. **The Boomers' shop discount is gone.** The request described the Boomers' community power as the retire chance
   only. It's one line to bring the discount back (`PartyEngine.CommunityAftermath`).
4. **Millennials' "increases the reward by the score":** the reward is one inheritance of 1 + points (+1 / +2 / +3),
   not several +1s.
5. **The retire chance uses the Boomers in chat when the action is used**, counting the Boomer that uses it: one Boomer
   alone (or two copies of the same card) always retires.
6. **Trophy Boomers** (Big Boomer, Mainstream Celebrity) keep Early Retirement (they have no action of their own), so
   they can retire too.
7. **The Legacy Media Mogul isn't in a preset set.** Monetization Meeting (Boomers and Millennials) would suit it.

## 13. Small changes: no "first come, first served", level Deplatform warning, removed viewers stay out, Remove a Viewer 5 +5, Staff +1 Cash (2026-11-18)

### Do this first in Unity

1. Let the scripts compile.
2. Run **Deck Party > Setup > Build Everything** once, which imports the changed strings. A Rebuild isn't needed.
3. Multiplayer needs every player on this build: protocol `deckparty-27`, state format 17 (older saves still load).

### What changed

- **"First come, first served" is gone from the screen.** Free For All still works the same way (everyone shops at once,
  and the stock is shared). Only the text is gone, so the rules can explain it:
  - the orange tag beside the shop's title;
  - the footer line on the shop gate;
  - the subtitle of the **SHOP OPEN!** banner;
  - the "shop is open" headline and bar, which now just say **The shop is open!**
- **The Deplatform warning no longer sticks at a tilt.** The warning at the bottom of the chat when you're 1 Risk away
  gets Feel shakes (a slam when it appears, a jolt every 2.6 s).
  - **The cause:** those shakes start from wherever the panel is. One cut short (the warning hidden mid-wobble when the
    Risk drops, then shown again) left it tilted, or slightly shifted, for good.
  - **The fix:** the panel now has `FxRestPose`, the same guard the Risk pips got. It snaps the panel back when it hides,
    and eases it level and back in place 1.2 s after the last shake.
- **Removed and retired viewers are out of the game.** A card that leaves your Audience for good no longer goes back on
  the shop's pile, in solo and multiplayer. That covers:
  - **Remove a Viewer**;
  - a Boomer retired by **Early Retirement**;
  - the **This Is Fine Dog**.

  If the pile runs out, that viewer is sold out for the rest of the season. Your own copy limit still counts only the
  cards you own.
- **Remove a Viewer costs 5 Influence, then 5 more each time** (5, 10, 15, 20...). It was 3, then +2.
- **Staff is slightly stronger.** Each point now pays **+1 Influence and +1 Cash** when the stream is scored (it was +1
  Influence only). That's +1 / +1, +2 / +2, then +3 / +3 at the cap of 3 points. The chip shows both, and both fly from
  it.

### Where it is in code

- **Shop text:** `PlanningView` (the header and the gate notice), `ShopGateView` (hides an empty notice line) and
  `GameController.Rounds.OnStageChanged` (the banner). The CSV rows `shop.first_come`, `gate.notice` and
  `game.shop_open_sub` were deleted.
- **Warning:** `StreamerAnxietyView.Build` calls `FxRestPose.Ensure(panel, position: true)`.
  `FxRestPose.Ensure` now takes the rest pose at once and can also restore a RectTransform's anchored position.
- **Out of the game:**
  - `PlayerState.GoneForGood` (per viewer type), set by `PartyEngine.MarkGoneForGood` in `Dismiss`, `RetireBoomer`
    and the This Is Fine Dog's save.
  - `PartyEngine.TakenFromShop` counts these cards as taken, so `SharedLeft` and the host's shared-shop sync never
    give them back.
  - Saved in state format 17.
- **Price:** `GameRules.FirstRemovalCost` 5 and `RemovalCostStep` 5. The tooltip line reads the step
  (`shop.remove_viewer_rule` has `{step}`).
- **Staff:** `Communities.StaffCashPerPoint` and `StaffCash`, paid in `PartyEngine.ScoreCommunityRewards` (the
  `CommunityReward` event carries both amounts). Also counted in `ProjectScore` and `ProjectEarlyRetirement`.

### Decisions I made (please check)

1. **"Very slightly improve" Staff** became +1 Cash per point on top of the +1 Influence. One whole Influence more per
   point would be a big jump at level 1 (twice as much). Cash is the cheaper resource (an unpaid fee costs 7 Cash per
   Influence), so this is the smallest boost that helps every level. To change it, edit
   `Communities.StaffCashPerPoint` (0 turns it off) or `StaffInfluencePerPoint`.
2. **The shop gate's footer line was removed rather than reworded.** It also said that everyone goes in together and
   that the stock is shared. The countdown already shows the first part, and the rules can say the rest.
3. **The This Is Fine Dog counts as gone for good too.** It's the only other way a card leaves your Audience for good,
   and it's put aside, so it doesn't happen in the current roster.
4. **Removing one of your starting cards doesn't change the shop.** Those cards were never on the pile.
5. **The removal texts now say the card leaves "the game"**, not just your Audience: the button's tooltip, the confirm
   dialog and the money rules.
