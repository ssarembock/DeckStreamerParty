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
