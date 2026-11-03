# Audience Capture V4: icons, Communities and Deplatform

V4.2 (QuickUpdates.md) added three communities, cut the roster down to 44 cards, added Free For All multiplayer and reworked the stream layout. Sections 6 to 9 cover it; sections 1 to 5 are updated to match.

## 1. Get the update into Unity (do this first)

1. Open the project and let it compile.
2. Run **Deck Party > Setup > Rebuild Scenes and UI Prefabs (overwrites)**. You must run it, because it:
   - re-imports the strings, so the new text and icons replace the old;
   - rebuilds the Game scene (Community Beats, the icon-only HUD, and in V4.2 the facecam corner, the shop gate and the round summary);
   - rebuilds the card and shop tile prefabs (community icons, empty slots without text);
   - adds the **Community Icons** slots to the Game Database (14 now);
   - V4.2: gives the 44 roster cards the communities from your card table, **once** (see section 3);
   - V4.2: regenerates the Feel feedback **FX_StreamerAnxious** with the subtler recipe (your other feedback prefabs are untouched).
3. Open **Deck Party > Select Game Database** to check the new slots.

## 2. Where each sprite goes

Import every sprite as usual: **Texture Type** Sprite (2D and UI); slice sheets in the Sprite Editor (see V3Instructions.md, section 2).

| Sprite | Where | Notes |
|---|---|---|
| **Gifted Subs** icon | UI Skin > **IconPop** | Used on the HUD, the shop price and inside every line of text. |
| **Cash** icon | UI Skin > **IconCash** | Same. |
| **Deplatform Risk** icon | UI Skin > **IconTrouble** | Also the risk pips and the red card badge. |
| **Community** icons (14) | Game Database > Stream Art > **Community Icons** | One slot per community. They appear on cards, shop tiles, tooltips, inside text and on the Community Beats chips. Leave a slot empty to keep its built-in coloured badge (Clout is a yellow star, Memetics Division a pink repost loop, Bots a grey robot). |
| **Bot** portrait | Game Database > Viewers > **Bot** (last row) | The token rival Bots send into your stream. Give it a portrait like any viewer. |
| **Deplatform** image | Game Database > Stream Art > **Deplatform Image** | Your old Platform Ban image moved over by itself. |

The UI Skin is `Assets/DeckParty/Data/UISkin.asset` (also linked from the Game Database's **Skin** field).

Guidelines:
- **Shape:** square icons, readable at about 24 px, because they sit inside text.
- **Coloured art:** in the UI Skin entry, tick **Override Color** and leave it white, so neither the HUD nor the icons inside text tint your colours. This matters most if you named the files `icon_pop`, `icon_cash` or `icon_trouble`: those are also the placeholders' names, so the text icons would otherwise be tinted gold, green or red.
- **Auto-assign:** sprites under `Assets/DeckParty/Art` named `IconPop`, `IconCash` or `IconTrouble` (any case, with or without `_`, `ui_` or `skin_`) are slotted by **Deck Party > Art > Auto-Assign Sprites by Name**.
- **When changes show:** icons inside text are built when the game starts, so restart Play mode after changing them.

## 3. Communities

Every viewer belongs to none, one or several communities. Each **different** viewer in chat gives their community one point. Two copies of the same card count once, and a viewer who leaves takes their point away. Points apply as they change, during the stream.

| Community | Bonus | Max that counts |
|---|---|---|
| Core Fanbase | Every 3 points cancel 1 Deplatform Risk | No max |
| Staff | 10% per point that a Staff viewer's Cash cost is waived, rolled at each scoring | 9 (90%) |
| Content | 10% per point that each viewer **outside** the Core Fanbase scores twice when the stream ends | 9 (90%) |
| Techspertise | 20 / 40 / 50% that a Server Down stream still scores | 3 |
| Finance Bros | 10% per point that each viewer's Cash reward (positive Cash) is doubled | 9 (90%) |
| Trolls | 1 random Gifted Sub or Cash per point when the stream gets deplatformed. Nothing if you end it yourself or the server goes down | No max |
| Doomsdayers | Multiplayer. After your successful stream, every rival's next stream gives each viewer 4% per point to score nothing | 5 (20%) |
| Boomer | When the stream is scored: +2 Cash and -1 Gifted Subs per point (fixed, once per stream) | No max |
| Millennial | When the stream is scored: +2 Gifted Subs and -1 Cash per point (fixed, once per stream) | No max |
| Zoomer | When the stream is scored, two separate rolls of 20% per point: +6 Gifted Subs, and -3 Cash. Both, one or neither can happen | 4 (80%) |
| Armchair Activists | 10% per point that each Armchair Activist scores nothing when the stream ends, rolled per card. Other viewers aren't affected | 5 (50%) |
| **Clout** (V4.2) | 10% per point that each viewer **outside** the Core Fanbase brings +1 Gifted Sub more, rolled every time they score (an encore rolls twice) | 4 (40%) |
| **Memetics Division** (V4.2) | 1% per point that the **whole stream scores a second time**: every viewer and every community reward again, all rolls made afresh | 10 (10%) |
| **Bots** (V4.2) | Multiplayer. When your stream is scored, each Bots card in chat rolls 10% per point to send a **Bot** into the **next player's** next stream. A Bot takes a space in chat and earns nothing, then leaves | 4 (40%) |

The **End the Stream** button's preview includes the fixed Boomer and Millennial amounts; the random rolls aren't in it.

- **Change who belongs where:** Game Database > **Viewers** > each row's **Communities** dropdown. Tick as many as you like; *Nothing* means no community. Edits apply straight away. **Reset All Communities** puts everyone back on the defaults (your V4.2 table).
- **Change the numbers:** they are constants at the top of `Assets/DeckParty/Scripts/Core/Community.cs` (for example `CloutPercentPerPoint`, `MemeticsMaxPoints`, `BotsMaxIncoming`). The tooltips read them, so they stay correct. The rules page text (`rules.communities.body`) is the one place to update by hand.
- **Add a community later:**
  1. Add it to the `Community` enum and `CommunityMask` in `Community.cs`, and raise `Communities.Count`.
  2. Add its rule in `PartyEngine`, its strings (`community.<Name>.name / .desc / .now`), its colour and placeholder symbol in `GameIcons.cs`, and its default members (bump `GameDatabase.CommunityDefaultsRevision` so existing databases pick them up).
  3. Run Rebuild to give it a chip and an icon slot.
- **Default assignments** are your V4.2 card table. They live in `GuestCatalog.s_RosterCommunities` and in the Communities column of `AudienceCapture_Cards.csv`. Nobody is a Doomsdayer or an Armchair Activist in the current roster; those communities still work if you assign them.
- **Migration:** the first Setup / Rebuild after V4.2 replaces the communities of the 44 roster cards with the table, once. If you had edited communities in the Game Database, those edits on roster cards are overwritten by the table; edits you make afterwards are kept. Put-aside cards keep theirs.
- **Card icons:** cards and shop tiles have 3 community icon slots. The tooltip always lists every community.
- **Community Beats:** shows only the communities someone in your Audience belongs to, so the row grows as you recruit.

## 4. What else changed (V4)

- **Deplatform:** Platform Ban is now **Deplatform** in all 5 languages: "DEPLATFORMED!", Deplatform Risk, "the stream gets deplatformed". Rules are unchanged, and internal code names (Raid, Trouble) are unchanged.
- **Resources are icons:** no "Gifted Subs", "Cash" or "Risk" words in the game UI. This covers the HUD, the shop price (icon and number), card stats, tooltips, the Activity log, toasts and rules. Hovering any icon names the resource.
- **Shop tooltip:** a **When scored** row shows the three resource icons with the viewer's printed values.
- **Community Beats:** top-right of the chat bubble during a stream, one chip per community. Each chip shows the icon, the points and the current bonus. A chip pops when its score rises. Hover a chip for what it does and how it scales. A red chip shows while rival Doomsdayers curse your stream.
- **Old families:** the old character families are out of the game. Their code is kept (`GuestCategory`). Degen Dog counts different **communities**.
- **Bonus feedback:**
  - when a community bonus triggers at the end of a stream, the flying score chip is tagged (x2, FREE, Cash x2, DOOMED, ALL TALK, Clout +1) and a log line explains it;
  - Boomer, Millennial and Zoomer payouts fly from their Community Beats chip;
  - Memetics Division: a **SCORED AGAIN!** banner, then the whole chat scores a second time on screen;
  - Bots: a toast when rival Bots join your Audience, and "Bots sent" floats up when yours go out;
  - Techspertise saves show **SERVER RESTORED!**;
  - Trolls payouts float up when you get deplatformed.
- **Versions:** state format **v6** and network protocol **`deckparty-8`**. All players need the same build. Single-player saves from earlier versions still load.

## 5. Guidelines for writing text

- **Never name a resource in game text.** Write `{icon:subs}`, `{icon:cash}` or `{icon:risk}` in `AudienceCapture_Strings.csv`; they become the icons. Communities work the same way: `{icon:CoreFanbase}`, `{icon:Staff}`, `{icon:Content}`, `{icon:Techspertise}`, `{icon:FinanceBros}`, `{icon:Trolls}`, `{icon:Doomsdayers}`, `{icon:Boomer}`, `{icon:Millennial}`, `{icon:Zoomer}`, `{icon:ArmchairActivists}`, `{icon:Clout}`, `{icon:MemeticsDivision}`, `{icon:Bots}`.
- **Where names live:** the resource names are only in `term.*` and the hover tooltips (`hud.tip.gifted_subs`, `hud.tip.cash`, `hud.tip.risk_term`). The rules page also keeps them in its icon legend.
- **Prices and wallets:** put the icon first ("[icon] 5"). Gains and losses put the number first ("+3 [icon]").
- **Viewer text keys follow the code ids**, so renamed cards have new keys: `char.DegenDog.*`, `char.DistraughtCat.*`, `char.Evangelist.*`, `char.PromoCodeUser.*`, `char.CryptoEnthusiast.*`, plus `char.Bot.*`.
- **The streamer's voice:** warnings the streamer shouts say "we" (for example "ONE MORE AND WE'RE DEPLATFORMED!").

## 6. The roster (V4.2)

- **44 cards are in the game:** exactly the cards in your table. The other 38 (the meme cards) are **put aside**: their code, rules, art and communities stay in the project, but nothing in the build uses them. No shop, preset, Random Mix or Open Lobby offers them, the Game Database lists only the roster (their art stays saved, hidden), and the guide's viewer list shows only the roster.
- **Bring a card back:** delete its line from `s_PutAside` in `Assets/DeckParty/Scripts/Core/GuestCatalog.cs`, give it communities in `s_RosterCommunities` (its old ones are kept in `s_PutAsideCommunities`), and add it to a preset in `ScenarioCatalog.cs`.
- **Renames** (art and portraits followed automatically):

  | Was | Now |
  |---|---|
  | Surprise Guest | **Evangelist** |
  | Crying Cat | **Distraught Cat** |
  | Doge | **Degen Dog** |
  | Comedian with a Podcast | **Promo Code User** (your table's name for the card with the same rule) |
  | Crypto Guy | **Crypto Enthusiast** (same) |

- **Rules:** your table matched the current rules for every card, so only names and communities changed. Degen Dog's line in the table was older wording for the same rule ("card segment" = community).
- **Art tools:** **Deck Party > Art > Auto-Assign Sprites by Name** understands the new names (and still the old ones). **Assign portraits in roster order** now walks the 44 roster cards only; **Log Expected File Names** prints that order.

### Preset sets

New names, groupings and descriptions, one community idea each. Every regular card is in at least one set. Ids are kept, so a set you cleared before counts as cleared for the set that replaced it.

| Set | Idea | Diamonds |
|---|---|---|
| **First Stream** (Learn the ropes) | Simple regulars, a Producer and Chat Admin, one risky collaborator | Precocious Youth, Mainstream Celebrity, Big Boomer |
| **Fan Club** (Loyalty pays) | Core Fanbase cancels risk, so trolls and orbiters become safe | Evangelist, Internet Starpower, Big Boomer |
| **Viral Moment** (Clout chasing) | Content and Clout: collaborators, scandals, celebrities | Bigger Star, Internet Starpower, Mainstream Celebrity |
| **Meme Lab** (Such synergy) | Memetics Division, Degen Dog's variety, Distraught Cat with removers | Clippo, Platform Owner, Senior Marketing Manager |
| **Staff Meeting** (Run a tight ship) | Staff and Techspertise, every action card | Senior Marketing Manager, Platform Owner, Evangelist |
| **Generation Gap** (OK boomer) | Boomer / Millennial / Zoomer plus risk payers | Big Boomer, Senior Marketing Manager, Precocious Youth |
| **Crypto Winter** (Cash out before it crashes) | Finance Bros and Cash engines | Clippo, Senior Marketing Manager, Bigger Star |
| **Comment Section** (Don't feed the trolls) | Trolls, risk and Bots | Big Boomer, Clippo, Internet Starpower |

Change a set in `ScenarioCatalog.cs` (11 regulars + 3 Diamonds each) and its text in the strings (`scenario.<id>.name / .tagline / .desc`).

## 7. Multiplayer: Free For All (V4.2)

- **Up to 6 players** (that limit was already there). **Take Turns** and **Race** allow at most 2 players. With 3 or more, the lobby switches to **Free For All** by itself and locks the mode selector.
- **Free For All** replaces Synchronized Nights (same mode, new rules):
  1. Everyone streams at the same time. Nobody can watch anyone else: clicking a rival explains why, and the Players panel shows only each rival's status (Streaming, Deplatformed, Stream over, At the shop, Shopping, Ready).
  2. When your stream ends, a panel covers the shop: **Stream over!** and a **Go to the Shop** button (Space works too). You can take it back while others are still streaming.
  3. When every stream is over and everyone has clicked it, a big **3, 2, 1** plays, then **SHOP OPEN!**. The shop header says **FIRST COME, FIRST SERVED** (the stock is shared).
  4. After shopping, everyone clicks **Ready for night N**. The next night starts for all at once.
- **Round Summary:** the **Round Summary** button on the bar above the board opens what every player did this round and the one before: stream results, community payouts, Doomsdayers and Bots, recruits, Sub Cap raises, powers, Temporary Bans. At the bottom, **Waiting for the next streams** lists what's already set for each player (rival raids, Doomsdayers curses, Bots, armed powers).
- **Bots** go to the next player in player order (in a 2-player game, your rival). Bots from several rivals add up, to 6 at most (`Communities.BotsMaxIncoming`).
- **Turn based** works as before: when it isn't your turn you watch the active player live.
- **Change the countdown** in `MatchModes.ShopCountdownSeconds` (`Assets/DeckParty/Scripts/Runtime/Networking/NetTypes.cs`).

## 8. Stream layout and feedback (V4.2)

- **The chat bubble fills the width:** when the side column has nothing to show (single player with the Activity log off) the stream takes the whole width. In multiplayer the Players and emote panels keep their column.
- **Rows of 8:** the chat fills a row of up to 8 cards, then starts a new one, so it grows as a rectangle. Only when the cards would drop below their minimum size do rows get longer (up to 12 on wide screens), and past that the chat scrolls. Change the 8 in `GameRules.HouseColumns`.
- **No "Empty" text** on open slots: just the faded frame. A big viewer's extra spaces still show its name.
- **No status line:** "Live Chat 3/5 Sub Cap - 7 in the queue" is gone. The top-left keeps the hints (one risk left, chat full, win now).
- **The facecam** is now a square in the stream's bottom-right corner at 3/4 of its old size (318 px at 1080p), with the **LIVE** badge and the stream timer inside it. It belongs to the stream, so it's hidden in the shop (and on the Temporary Ban screen). The cards always keep clear of it: the card rectangle slides left, or shrinks a little when there's no room. The Add Viewer / End the Stream buttons centre in the space left of it. Change its size in `GameSceneBuilder.FacecamSize` and run Rebuild.
- **Subtler risk warning** (one Deplatform Risk from the limit): the screen shake is weaker and less frequent (every 2.6 s instead of 1.6 s, 2.5 px instead of 4), the red edge glow is softer and slower, and the streamer panel and facecam tremble less. Tune them on the Game scene's **AnxiousStreamer** object (`Shake Interval`, `Shake Pixels`, `Panel Jolt`) and in **FX_StreamerAnxious**.

## 9. Decisions I made (review these)

- **Crypto Enthusiast has no community:** your table lists "Independent Researcher", which isn't a defined community. Tell me its rule and I'll add it, or tick a community in the Game Database.
- **Bots and Doomsdayers do nothing in single player** (there's no next player). Bots go to the next player in player order, and a Bot never earns Clout.
- **Memetics Division repeats the chat's scoring** (viewers plus Boomer / Millennial / Zoomer), not investments, mid-stream pot gains or the streak bonus.
- **Clout rolls once per scoring:** a Content encore rolls it twice, the Memetics repeat rolls it again. Doomsdayers and Armchair Activists misses get nothing.
- **Take Turns and Race allow at most 2 players:** the brief only required this for Take Turns. I kept Race for 2 players because its own-pace shopping doesn't fit the shared shop gate.
- **Free For All hides rivals' boards for the whole round**, shop included; the round summary replaces watching.

## 10. Quick check

1. **Single player:**
   - Run a stream and watch the Community Beats light up as different viewers join.
   - Two copies of one viewer add only one point.
   - Hover a chip and an inline icon.
   - The chat bubble spans the screen, the facecam sits bottom-right with LIVE and the timer, and it disappears in the shop.
   - Raise the Sub Cap past 8: a second row starts.
2. **Shop:** prices show only an icon and a number; only the 44 roster cards appear; tile tooltips have the **When scored** icon row and the communities.
3. **Clout / Memetics:** put Bigger Star or Internet Starpower in chat for Clout +1 tags; Degen Dog plus Distraught Cat give Memetics 2 points (2% to score twice; set `MemeticsPercentPerPoint` high temporarily to see the replay).
4. **Get deplatformed:** with Trolls in chat, check the payout, your Deplatform image and the calmer warning before it.
5. **Two players:**
   - Clippo in chat at the end of a stream may send a Bot: the rival gets a toast and a Bot in their queue next stream.
   - Doomsdayers need a viewer assigned in the Game Database first, as none have one by default.
6. **Three players (Free For All):** the lobby switches mode; finish streams at different times, click Go to the Shop, watch the 3-2-1, race for the same viewer, open the Round Summary.
