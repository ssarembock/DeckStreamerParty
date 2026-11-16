# Audience Capture: Streamers, Viewers and Communities

**Current rules (16 November 2026).** Everything below is what the game does today, including the card edits of 16
November: Mods and the new Gatekeepers, Hype Men spare seats, Boomer levels, the Boomer Troll and the two
Isolationists, and Loyal Fan and Fish in every shop. The communities are the built-in ones: the Game Database takes
them once after the next Setup, and your edits there rule after that.

**Changing it:**
- Rewrite any rule, number, community list or name directly.
- To add a viewer, copy a block and mark it **NEW**.
- To remove one, write **REMOVE** under it.
- Leave the `code id` in each heading as it is; that's how I find the card.
- Anything that needs explaining can go in a *Note for Claude:* line.

When you hand the file back, I compare it with this version to see exactly what changed.

**Terms used below:**

| Term | Meaning |
|---|---|
| **Cash** | What viewers earn and what you recruit with. Called Clout until 15 November 2026 (Gifted Subs before that). In the code: `Cash` (`PlayerState.Cash`, `ResourceIcon.Cash`); in text: `{icon:cash}`. |
| **Influence** | Pays for Sub Cap upgrades, streamer actives, viewer fees and Remove a Viewer. Called Cash until 15 November 2026. In the code: `Influence` (`PlayerState.Influence`, `ResourceIcon.Influence`); in text: `{icon:influence}`. |
| **Risk** | Deplatform Risk. |
| **Trophy viewer** | One of the star viewers; you need 4 of them in one stream to win. |
| **Audience** | All the viewer cards you own. |
| **Queue** | Your Audience, shuffled, waiting to join this stream. |
| **Chat** | Who's in the stream right now. |

---

## 1. The rules the cards plug into

### Streams and scoring

- **A stream.**
  - Viewers join chat from the queue one at a time (**Add Viewer**).
  - **End the Stream** scores everyone in chat: each viewer earns its Cash and Influence, then community rolls happen.
  - Each viewer's **action** can be used once per stream, unless a community gives extra uses.
- **Sub Cap.**
  - The number of seats in chat: starts at 6, maximum 34.
  - Raising it costs 2 Influence, then 1 more for each raise after that (maximum 12).
  - **Server Down:** if chat goes over the Sub Cap, the stream ends and pays nothing.
- **Deplatformed.**
  - Each viewer marked "Risk 1" adds 1 Risk; "cancels 1 Risk" viewers take 1 away.
  - At 3 net Risk the stream is deplatformed: it pays nothing, and you pick one of the risky viewers to sit out your
    next stream.
- **Fees.**
  - Negative Influence (a viewer's "−1 Influence") is paid from your bank.
  - Every Influence you can't pay costs 7 Cash.
- **Winning.** 4 Trophy viewers in one stream that gets scored wins. A season lasts 25 streams by default (10, 15,
  20, 25 or 30 in multiplayer).

### Your Audience and the shop

- **Starting Audience:** 4 Loyal Fan, 2 Fish, 5 Ineffective Troll.
- **Always in the shop:** Loyal Fan and Fish are sold in every shop, whatever the set (Random Mix included).
- **Recruiting.**
  - Viewers cost Cash.
  - You can own up to 4 copies of each regular viewer; Dancing Jen and Crypto Enthusiast allow 5.
  - Loyal Fan and Fish: you can buy 4 more on top of your starting copies (8 Loyal Fans and 6 Fish in all).
  - Trophy viewers have no limit.
- **Shared stock (multiplayer).** The shop's stock is shared by every player. Each regular viewer's stack holds
  **2 copies per player**: 4 for two players, 6 for three, 8 for four, and so on. Group cards always have at least 5.
  Trophy viewers are unlimited. The number of different viewers in the shop doesn't change.
- **Remove a Viewer** (shop): 2 Influence, +1 per use (maximum 12). It can't take your Audience below 5 viewers.

### How communities score

- **Points.** A community scores **1 point for every different member in chat after the first.** Two different
  members score 1 point, three score 2, and so on.
- **Boomers have levels instead:** 2 different Boomers in chat make level 1, 4 make level 2 (the most).
- **Copies.** Two copies of the same card count once.
- **Leaving.** A member who leaves chat takes its point away.
- **Caps.** Some communities stop counting at a maximum.

---

## 2. Streamers

Each player picks one streamer, or **No Streamer** (classic rules, no power), or **Random**.

**How the powers work:**
- **Actives** are bought in the shop with Influence. Each use costs 1 more than the last (never more than 12). Boomers
  can make them cheaper in the next shop (section 4).
- **Next stream** powers take effect in your next stream.
- **Rival** powers hit another player and only exist in multiplayer.
- **Cooldown 2** means the power can't be used again in the shops of the next 2 streams. It does not keep the power
  active: a power that affects a future stream only applies to the next one.

**Every streamer has an active** (since 15 November 2026). Bro Jogan, IRunFast, Soft Hard Body Man and Manim Maketh
Man had passives until then; their actives below are first drafts, so change them freely.

The platform only changes the animated logo in the menu bar.

### Fate · `Fate`
- **Title:** The Debate Lord · **Platform:** Lookout
- **Power: Raid** (active, rival, multiplayer only). Costs 1 Influence (+1 per use), cooldown 2.
- **Rule:** pick a rival. Their next stream starts with +1 Risk. Raids alone can't push a stream past 2 Risk.
- **Quote:** "Debate me, bro."

### Bro Jogan · `BroJogan`
- **Title:** The Three-Hour Podcaster · **Platform:** Tunes
- **Power: Three-Hour Episode** (active, next stream). Costs 2 Influence (+1 per use), cooldown 2.
- **Rule:** your next stream has +1 Sub Cap (that stream only). Was a passive until 15 November (+1 Sub Cap from the
  start, Sub Cap upgrades cost 1 Influence more).
- **Quote:** "Jamie, pull that up."

### Shinobi · `Shinobi`
- **Title:** The Headband Legend · **Platform:** Lookout
- **Power: Smoke Bomb** (active, next stream). Costs 2 Influence (+1 per use), cooldown 2.
- **Rule:** your next stream ignores 1 Risk.
- **Quote:** "You didn't see that. Nobody did."

### IRunFast · `IRunFast`
- **Title:** The Human Siren · **Platform:** Cough
- **Power: Bark at Chat** (active, next stream). Costs 2 Influence (+1 per use), cooldown 2.
- **Rule:** in your next stream, every viewer carrying Risk earns +2 Cash when scored. Was a passive until 15
  November (+1 Cash every stream).
- **Quote:** "SIUUUUUUU!"

### MrShark · `MrShark`
- **Title:** The Giveaway King · **Platform:** Cough
- **Power: $10,000 Giveaway** (active, next stream). Costs 3 Influence (+1 per use), cooldown 2.
- **Rule:** in your next stream, every viewer earns +1 Cash.
- **Quote:** "Last viewer to leave chat wins!"

### Ethical F**kboi · `EthicalFboi`
- **Title:** The Mansion Socialist · **Platform:** Stomp
- **Power: Wealth Tax** (active, rival, multiplayer only). Costs 1 Influence (+1 per use), cooldown 2.
- **Rule:** pick a rival (with only one rival, it's picked for you). They pay you up to 2 Influence. The pick list shows each
  rival's Influence, and a rival with no Influence can't be picked. If no pick reaches the host, the rival with the
  most Influence pays.
- **Quote:** "Eat the rich. Not me, though."

### Soft Hard Body Man · `SoftHardBodyMan`
- **Title:** Looks Hard, Is Soft · **Platform:** Swipe
- **Power: Soft Landing** (active, next stream). Costs 1 Influence (+1 per use), no cooldown (once per turn).
- **Rule:** if your next stream gets deplatformed, you still bank half of that stream's Cash. Was a passive until 15
  November (every stream).
- **Quote:** "REFORMED. And a little bit squishy."

### Strategic Abomination · `StrategicAbomination`
- **Title:** Two Heads, Zero Chill · **Platform:** Post
- **Power: Double Move** (active, next stream). Costs 2 Influence (+1 per use), cooldown 2.
- **Rule:** in your next stream, the first viewer action you use can be used again.
- **Quote:** "The rook! THE ROOK!"

### Palmane · `Palmane`
- **Title:** Gotta Catch 'Em (Legally) · **Platform:** Swipe
- **Power: Pal Sphere** (active, instant). Costs 2 Influence (+1 per use), cooldown 3.
- **Rule:** catch a random viewer from your shop for free. Only viewers that cost 6 or less, aren't Trophy viewers and
  are still in stock; the copy comes out of the shared stock.
- **Quote:** "Hiii chat, who wants to be my pal?"

### Manim Maketh Man · `ManimMakethMan`
- **Title:** The Animated Proof · **Platform:** Post
- **Power: Beautiful Sequence** (active, instant). Costs 1 Influence (+1 per use), cooldown 2.
- **Rule:** gain +1 Cash for each great (scored) stream in a row, up to +5, straight away in the shop. Needs a
  streak of at least 1; a failed stream resets the streak. Using it doesn't reset the streak. Was a passive until 15
  November (each scored stream in a row earned +1, +2, +3... Cash, max +5).
- **Quote:** "Let's visualize that."

### Rude Radiologist · `RudeRadiologist`
- **Title:** Two-Time Back-to-Back Diagnostician · **Platform:** Stomp
- **Power: X-Ray Vision** (active, next stream). Costs 1 Influence (+1 per use), no cooldown (once per turn).
- **Rule:** in your next stream, you see the next 3 viewers in your queue.
- **Quote:** "Violence. Speed. Radiology."

### No Streamer · `None`
- **Title:** Classic rules
- **Power:** none. The power slot stays empty.
- **Quote:** "Just here for the vibes."

### Random · `Random`
- **Title:** Let chat decide
- **Rule:** one of the 11 streamers above is picked at random when the match starts.

---

## 3. Viewers (50 cards in the game)

Each block lists:

- **Cost**, in Cash.
- **Scores**: what the viewer earns when the stream is scored.
- **Risk**: what it adds to or cancels from Deplatform Risk.
- **Action**: once per stream.
- **Effect**: anything else it does.
- **Communities**.
- **Sets**: the preset character sets whose shop sells it.

Every viewer also appears in **Open Lobby**, and every buyable one can appear in **Random Mix** (Loyal Fan and Fish,
12 random regular viewers and 3 random Trophy viewers).

### 3.1 Starting viewers

#### Loyal Fan · `LoyalFan`
- **Cost** 2 · **Scores** +1 Cash
- **Effect:** a basic starting viewer. Always shows up.
- **Communities:** Core Fanbase
- **Sets:** starting Audience (4 copies), and sold in **every** shop (you can buy 4 more)

#### Fish · `Fish`
- **Cost** 3 · **Scores** +1 Influence
- **Effect:** a basic starting viewer who pays for your first Sub Cap upgrades. Called Whale until 10 November; the
  new Whale (below) is the big Influence viewer.
- **Communities:** Core Fanbase
- **Sets:** starting Audience (2 copies), and sold in **every** shop (you can buy 4 more)

#### Ineffective Troll · `IneffectiveTroll`
- **Cost** can't be bought · **Scores** +2 Cash · **Risk** 1
- **Effect:** starting viewer only. Lots of noise, a little Risk.
- **Communities:** Core Fanbase, Troll Army
- **Sets:** starting Audience (5 copies)

### 3.2 Regular viewers (by cost)

#### Producer · `Producer`
- **Cost** 3 · **Scores** +2 Cash, −1 Influence
- **Action · Shoutout:** choose any viewer in the queue to join chat, if there's room. Trophy viewers included: this is
  the same action as the Evangelist's.
- **Communities:** Staff
- **Sets:** Going Live

#### Influential Troll · `InfluentialTroll`
- **Cost** 5 · **Scores** +4 Influence · **Risk** 1
- **Effect:** lots of Influence, but trouble follows.
- **Communities:** Troll Army
- **Sets:** Going Live, Clickbait Roulette

#### Boomer Troll · `BoomerTroll`
- **Cost** 9 · **Scores** +3 Cash, +2 Influence · **Risk** 1
- **Effect:** lots of Cash, but trouble follows. (New on 16 November.)
- **Communities:** Troll Army, Boomers
- **Sets:** Clickbait Roulette, Brand Deal Wishlist

#### Chat Admin · `ChatAdmin`
- **Cost** 4 · **Scores** +1 Cash, −1 Influence
- **Action · Remove From Chat:** remove one viewer from chat for the rest of the stream (it can remove itself).
- **Communities:** Staff, Mods (Mods points give extra uses)
- **Sets:** Going Live, Monetization Meeting, Algorithm Magic

#### Normie · `Normie`
- **Cost** 4 · **Scores** +1 Cash · **Risk** cancels 1
- **Communities:** Core Fanbase
- **Sets:** Going Live, Brand Deal Wishlist

#### Cybersecurity Engineer · `CybersecurityEngineer`
- **Cost** 4 · **Scores** +3 Cash, −1 Influence
- **Action · Read Access:** look at the next viewer in the queue, then let them in or keep them out for the rest of the
  stream.
- **Communities:** Staff, Voyeurs (Voyeurs points give extra uses)
- **Sets:** Clickbait Roulette, Monetization Meeting

#### Paid Sponsorship · `PaidSponsorship`
- **Cost** 10 · **Scores** +4 Cash · **Risk** 1
- **Effect:** pays very well, but the sponsor brings Risk.
- **Communities:** Boomers
- **Sets:** Going Live, Monetization Meeting

#### Influential Isolationist · `InfluentialIsolationist`
- **Cost** 7 · **Scores** +1 Influence
- **Effect · You Shall Not Watch!:** +1 Influence for every empty seat in chat when scored.
- **Communities:** Core Fanbase
- **Sets:** Algorithm Magic
- Called **Gatekeeper** until 16 November (it keeps that card's art); the Gatekeepers are now a community.

#### Billionaire Isolationist · `BillionaireIsolationist`
- **Cost** 6 · **Scores** +1 Cash
- **Effect · You Shall Not Watch!:** +1 Cash for every empty seat in chat when scored. (New on 16 November.)
- **Communities:** Core Fanbase, Boomers
- **Sets:** Monetization Meeting, Algorithm Magic

#### Mildly Risky Collaborator · `MildlyRiskyCollaborator`
- **Cost** 5 · **Scores** +3 Cash, +2 Influence · **Risk** 1
- **Communities:** Boomers
- **Sets:** Going Live, Brand Deal Wishlist

#### Junior PR Manager · `JuniorPrManager`
- **Cost** 5 · **Scores** +2 Cash, −1 Influence
- **Action · Situation Defusal:** send 1 random viewer in chat back to the queue (they can join again). Each copy
  sends one more every time it's used, for good: 1, then 2, then 3... Actions already used stay used.
- **Communities:** Staff, Gen-Z
- **Sets:** Going Live, Clickbait Roulette

#### Marketing Manager · `MarketingManager`
- **Cost** 5 · **Scores** +4 Cash, −1 Influence
- **Effect · Viral Moment:** brings the next viewer from the queue along when joining. This can push chat over the
  Sub Cap.
- **Communities:** Staff
- **Sets:** Going Live

#### Promo Code User · `PromoCodeUser`
- **Cost** 5 · **Scores** +1 Cash, and +3 Influence only if chat is full when scored
- **Communities:** Core Fanbase
- **Sets:** Going Live

#### Account Manager · `AccountManager`
- **Cost** 5 · **Scores** +3 Cash, −1 Influence
- **Action · Upsell:** a viewer in chat permanently earns +1 Influence more (up to 9 Influence printed).
- **Communities:** Staff
- **Sets:** Monetization Meeting

#### Generous Donator · `GenerousDonator`
- **Cost** 5 · **Scores** +1 Cash, +1 Influence
- **Effect · Give to the Rich!:** when joining, brings in the next viewer from the queue and scores them once right
  away. That score is banked, so it's kept even if the stream fails.
- **Communities:** Hype Men (the viewer it brings can take a spare seat)
- **Sets:** Brand Deal Wishlist, Algorithm Magic

#### Binge Bunny · `BingeBunny`
- **Cost** 5 · **Scores** +1 Cash
- **Effect · Algorithmic Rabbit Hole:** +1 Cash for every Loyal Fan in chat.
- **Communities:** Core Fanbase
- **Sets:** Brand Deal Wishlist

#### Hate-Watcher · `HateWatcher`
- **Cost** 5 · **Scores** +4 Cash · **Risk** 1 on alternate entrances
- **Effect:** switches mood every time they join chat. Hate-watching (Risk 1) on the first entrance, harmless on the
  next, and so on. The mood is remembered between streams.
- **Communities:** Troll Army
- **Sets:** Clickbait Roulette

#### Crypto Enthusiast · `CryptoEnthusiast`
- **Cost** 6 · **Scores** as a group (below) · **Copies** up to 5
- **Effect · To the Moon:** all Crypto Enthusiasts in chat score Influence together: 0 / 1 / 4 / 9 / 16 Influence in total for
  1 / 2 / 3 / 4 / 5 of them. The total is split between the copies, with the remainder on the first one in chat.
- **Communities:** Millennials
- **Sets:** Brand Deal Wishlist, Monetization Meeting

#### PR Manager · `PrManager`
- **Cost** 6 · **Scores** +3 Cash, −1 Influence
- **Action · Damage Control:** send random viewers in chat back to the queue (they can join again): **3** at 0
  Gatekeepers points, **6** at 1 point, **everyone else** at 2 points. Never itself. Actions already used stay used.
- **Communities:** Staff, Gatekeepers
- **Sets:** Brand Deal Wishlist, Monetization Meeting, Algorithm Magic

#### Highly Risky Collaborator · `HighlyRiskyCollaborator`
- **Cost** 6 · **Scores** +4 Influence · **Risk** 1
- **Effect · #NotHimToo?:** big money, big Risk.
- **Communities:** Millennials
- **Sets:** Clickbait Roulette, Monetization Meeting

#### Die Hard Fan · `DieHardFan`
- **Cost** 7 · **Scores** +2 Cash, +1 Influence · **Risk** cancels 1
- **Effect · Uncancellable:** the name only; it works exactly like Normie's cancel.
- **Communities:** Core Fanbase
- **Sets:** Clickbait Roulette, Monetization Meeting, Algorithm Magic

#### Chat Arbitrator · `ChatArbitrator`
- **Cost** 7 · **Scores** −1 Influence
- **Action · Keep the Peace:** clear the Risk of every risky viewer in chat for the rest of the stream, even if they
  leave and come back.
- **Communities:** Staff
- **Sets:** Brand Deal Wishlist

#### Risky Orbiter · `RiskyOrbiter`
- **Cost** 7 · **Scores** +2 Cash, +3 Influence · **Risk** 1
- **Communities:** none
- **Sets:** Clickbait Roulette

#### Distraught Cat · `DistraughtCat`
- **Cost** 7 · **Scores** nothing printed
- **Effect · Everyone Leaves Me:** gains +1 Cash or +1 Influence (at random) for this stream whenever another viewer
  leaves chat or is removed.
- **Communities:** Gatekeepers (adds to the PR Manager's send-back, and profits from it)
- **Sets:** Algorithm Magic

#### Dancing Jen · `DancingJen`
- **Cost** 7 · **Scores** as a group (below) · **Copies** up to 5
- **Effect · Dance Break:** all Dancing Jens in chat score Cash together: 1 / 4 / 9 / 16 / 25 Cash in total for
  1 / 2 / 3 / 4 / 5 of them. The total is split between the copies, with the remainder on the first one in chat.
- **Communities:** Millennials
- **Sets:** Going Live

#### Degen Dog · `DegenDog`
- **Cost** 7 · **Scores** +1 Cash
- **Effect · Such Synergy:** +1 Cash for each different community in chat (max +5). Other Degen Dogs don't count.
- **Communities:** none
- **Sets:** Brand Deal Wishlist

#### Doomer · `Doomer`
- **Cost** 7 · **Scores** +1 Cash
- **Effect · Doomscroll:** +2 Cash per Risk in the stream (net Risk, after cancels).
- **Communities:** Troll Army, Millennials
- **Sets:** Clickbait Roulette

#### Affiliate · `Affiliate`
- **Cost** 8 · **Scores** +1 Cash
- **Effect · Checkout Code:** when the stream is scored, a random viewer in chat permanently earns +1 Influence more (up
  to 9 Influence printed). The game picks the viewer (it can be the Affiliate itself), and the +1 already counts in that
  stream.
- **Communities:** none
- **Sets:** Brand Deal Wishlist, Monetization Meeting

#### Bot Detector · `BotDetector`
- **Cost** 8 · **Scores** +1 Cash
- **Action · Bot Removal:** remove two viewers sitting next to each other in chat for the rest of the stream (the Bot
  Detector can be one of them).
- **Communities:** Mods (Mods points give extra uses)
- **Sets:** Algorithm Magic

#### Social Media Manager · `SocialMediaManager`
- **Cost** 8 · **Scores** +3 Cash, −1 Influence
- **Action · Sneak Peek:** look at the next viewer in the queue, then let them in or keep them out for the rest of the
  stream. This is the same action as the Cybersecurity Engineer's.
- **Communities:** Staff, Voyeurs (Voyeurs points give extra uses)
- **Sets:** Clickbait Roulette, Monetization Meeting

#### Master of Currency · `MasterOfCurrency`
- **Cost** 8 · **Scores** +1 Influence
- **Effect · Chaos Ladder:** +2 Influence for every viewer in chat who carries Risk (cancelled Risk still counts).
- **Communities:** Troll Army, Boomers
- **Sets:** Clickbait Roulette

#### Whale · `Whale`
- **Cost** 8 · **Scores** +4 Influence
- **Effect:** none. It just scores a lot of Influence.
- **Communities:** Core Fanbase
- **Sets:** Going Live, Monetization Meeting

#### Forever Alone · `ForeverAlone`
- **Cost** 8 · **Scores** +1 Cash
- **Effect · Table for One:** +10 Cash if he's the only viewer in chat when scored. Otherwise +1 Cash per empty seat.
- **Communities:** Millennials
- **Sets:** Algorithm Magic

#### Billionaire Benefactor · `BillionaireBenefactor`
- **Cost** 9 · **Scores** +4 Influence
- **Action · Softball Interview** (once per stream for all copies together): every viewer in chat drops to 0 Cash
  for this stream, and you bank 1 Influence right away for each Billionaire Benefactor in chat.
- **Communities:** Boomers
- **Sets:** Going Live, Algorithm Magic

#### Tech Admin · `TechAdmin`
- **Cost** 9 · **Scores** +2 Cash
- **Action · Admin Rights:** remove one viewer from chat for the rest of the stream (it can remove itself).
- **Communities:** Mods (Mods points give extra uses)
- **Sets:** Algorithm Magic

#### Online Journalist · `OnlineJournalist`
- **Cost** 11 · **Scores** +2 Cash, +3 Influence
- **Effect · Unwanted Guests:** brings the next two viewers from the queue along when joining. This can push chat over
  the Sub Cap, unless they can take Hype Men spare seats. (Called Online Scandal until 10 November.)
- **Communities:** Hype Men
- **Sets:** Brand Deal Wishlist, Algorithm Magic

#### Executive PR · `ExecutivePr`
- **Cost** 11 · **Scores** +1 Cash, −1 Influence
- **Effect · No Bad Press:** +2 Cash for every viewer in chat who carries Risk (cancelled Risk still counts).
- **Communities:** Staff, Boomers
- **Sets:** Brand Deal Wishlist

#### Gen Z with Job · `GenZWithJob`
- **Cost** 12 · **Scores** nothing printed at first
- **Effect · Secure The Bag:** permanently gains +1 Influence every time they join chat (up to 9 Influence printed), even if the
  stream then fails.
- **Communities:** Gen-Z
- **Sets:** Clickbait Roulette

### 3.3 Trophy viewers

Four of these in one stream that gets scored wins the season. There's no copy limit, and the shop never runs out of
them.

#### Big Boomer · `BigBoomer`
- **Cost** 25 · **Scores** +1 Influence · **Risk** 1
- **Effect · Opinion Express:** adds 1 Risk.
- **Communities:** Boomers
- **Sets:** Brand Deal Wishlist

#### Clippo The Clanker · `ClippoTheClanker`
- **Cost** 25 · **Scores** −2 Cash, −1 Influence
- **Effect · Market Collapse:** costs 1 Influence and 2 Cash when scored.
- **Communities:** Staff
- **Sets:** Monetization Meeting

#### Bigger Star · `BiggerStar`
- **Cost** 35 · **Scores** nothing
- **Effect · Uplifting:** brings the next viewer from the queue along when joining (it can take a Hype Men spare seat).
- **Communities:** Hype Men
- **Sets:** Clickbait Roulette

#### Precocious Youth · `PrecociousYouth`
- **Cost** 35 · **Scores** nothing
- **Effect:** none.
- **Communities:** Gen-Z
- **Sets:** Going Live

#### Internet Starpower · `InternetStarpower`
- **Cost** 45 · **Scores** nothing · **Risk** cancels 1
- **Effect · Can Do No Wrong:** cancels 1 Risk while in chat.
- **Communities:** none
- **Sets:** Algorithm Magic

#### Platform Owner · `PlatformOwner`
- **Cost** 45 · **Scores** +3 Influence
- **Action · Free The Speech!:** remove one viewer from chat for the rest of the stream (it can remove itself).
- **Communities:** Boomers, Mods (Mods points give extra uses)
- **Sets:** Algorithm Magic

#### Mainstream Celebrity · `MainstreamCelebrity`
- **Cost** 50 · **Scores** +4 Cash, +1 Influence
- **Effect · Mass Appeal:** no extra effect.
- **Communities:** Boomers
- **Sets:** Clickbait Roulette

#### Senior Marketing Manager · `SeniorMarketingManager`
- **Cost** 50 · **Scores** +5 Cash, −1 Influence
- **Effect · Product Placement:** no extra effect.
- **Communities:** Staff, Millennials
- **Sets:** Monetization Meeting

#### Evangelist · `Evangelist`
- **Cost** 55 · **Scores** nothing
- **Action · 2-for-1!:** choose any viewer in the queue to join chat, even another Trophy viewer. With chat at the Sub
  Cap it still works while a Hype Men spare seat is free (the viewer takes it).
- **Communities:** Hype Men
- **Sets:** Brand Deal Wishlist

---

## 4. Communities (10 in the game)

**Points:** the number of different members in chat, minus 1, up to the cap. The Boomers count levels instead (see
below).

The member lists are worked out from the viewer blocks above. To move a viewer, edit its block; you don't need to
update both places.

**16 November:** the old Gatekeepers rule (extra removal uses) is now the **Mods**, and **Gatekeepers** are the random
kickers. The Mods' own old rule (a chance to cancel 1 Risk next stream) is gone.

### Core Fanbase · `CoreFanbase`
- **Rule:** each point gives every Core Fanbase viewer a **10% chance to score twice** when the stream ends. A viewer
  who scores twice plays its scoring animation twice.
- **Cap:** 3 points (30%).
- **Members (10):** Loyal Fan, Fish, Whale, Ineffective Troll, Normie, Influential Isolationist, Billionaire
  Isolationist, Promo Code User, Binge Bunny, Die Hard Fan.

### Staff · `Staff`
- **Rule:** each point adds **+1 Influence** when the stream is scored. It's a flat reward, and it flies from the Staff chip
  in Community Beats.
- **Cap:** 3 points (+3 Influence).
- **Members (12):** Producer, Chat Admin, Cybersecurity Engineer, Junior PR Manager, Marketing Manager, Account
  Manager, PR Manager, Chat Arbitrator, Social Media Manager, Executive PR, Clippo The Clanker, Senior Marketing
  Manager.

### Boomers · `Boomer`
- **Levels, not points:** **2** different Boomers in chat make **level 1**, **4** make **level 2** (the most).
- **Rule:** each level makes **Sub Cap upgrades and your streamer's active 1 Influence cheaper in your next shop**,
  for every upgrade and every use in that shop, never below 0. So the most off each one is 2.
- **Retirement:** after a scored stream, each Boomer that was in chat has a **10% chance to leave your Audience for
  good**.
- **Cap:** level 2.
- **Members (10):** Paid Sponsorship, Mildly Risky Collaborator, Billionaire Isolationist, Master of Currency,
  Billionaire Benefactor, Executive PR, Boomer Troll, Big Boomer, Platform Owner, Mainstream Celebrity.

### Millennials · `Millennial`
- **Rule:** each time a Boomer leaves your Audience, a random Millennial you own **permanently earns +1 Cash** (it was
  +1 Influence until 16 November).
- **How often:** once per point, and at least once even with 0 points, so the bonus grows with the points. You need a
  Millennial in your Audience; they don't have to be in chat. Printed Cash stops growing at 9.
- **Members (6):** Crypto Enthusiast, Highly Risky Collaborator, Dancing Jen, Doomer, Forever Alone, Senior Marketing
  Manager.

### Gen-Z · `GenZ`
- **Rule:** after a scored stream, each Gen-Z viewer in chat **permanently gains +1 Cash or +1 Influence** (at random),
  once per point.
- **Cap:** 2 points. Printed values stop growing at 9.
- **Members (3):** Junior PR Manager, Gen Z with Job, Precocious Youth.

### Mods · `Mods`
- **Rule:** each point gives **one more use of a Mod's removal action** this stream. The extra uses are shared between
  all Mods in chat. (This was the Gatekeepers' rule until 16 November.)
- **Which actions count:** Remove From Chat, Admin Rights, Free The Speech!, Bot Removal (and sending viewers back, if
  a Mod ever has that action).
- **Cap:** none.
- **Members (4):** Chat Admin, Bot Detector, Tech Admin, Platform Owner.

### Gatekeepers · `Gatekeepers`
- **Rule:** a Gatekeeper's action sends **random** other viewers in chat back to the queue (Mods pick who to kick;
  Gatekeepers don't): **3** at 0 points, **6** at 1 point, **everyone else** at 2 points. It never sends itself.
- **Which actions count:** Damage Control (PR Manager). Distraught Cat has no action: it adds to the points and gains
  from every viewer who leaves.
- **Cap:** 2 points.
- **Members (2):** PR Manager, Distraught Cat. *Two members means 1 point at most (6 viewers).*

### Hype Men · `HypeMen`
- **Rule:** each point gives **1 spare seat past the Sub Cap** for this stream. Only a viewer **a Hype Man brings in**
  can take one, so the Hype Men's guests don't crash the server.
- **What counts as "brought in by a Hype Man":** the viewers Online Journalist and Bigger Star bring along, the viewer
  Generous Donator brings in, and the viewer the Evangelist's 2-for-1! fetches (it still works in a full chat while a
  spare seat is free).
- **What it doesn't change:** once the Sub Cap is full you can't Add Viewer yourself, even with spare seats free, and
  anyone else pushing chat over the Sub Cap (a Marketing Manager's guest, say) still crashes the server.
- **How they look:** a free spare seat is a grey dotted circle after the normal seats, and a viewer sitting in one has a
  grey dotted ring. When a normal seat frees up, a viewer in a spare seat moves into it.
- **Cap:** 2 points (2 spare seats).
- **Members (4):** Generous Donator, Online Journalist, Bigger Star, Evangelist.

### Troll Army · `Trolls`
- **Rule:** once the community has at least 1 point, a scored stream earns **+2 Cash for every point of Risk** in it.
  Cancelled Risk counts too. It flies from the Troll Army chip. Deplatformed streams aren't scored, so they get
  nothing.
- **Cap:** none. Points beyond the first don't add more.
- **Members (6):** Ineffective Troll, Influential Troll, Boomer Troll, Hate-Watcher, Master of Currency, Doomer.

### Voyeurs · `Voyeurs`
- **Rule:** each point gives **one more use of a Voyeur's peek action** this stream (shared): Read Access and Sneak
  Peek.
- **Cap:** 1 point.
- **Members (2):** Cybersecurity Engineer, Social Media Manager.

**No community:** Risky Orbiter, Affiliate, Degen Dog, Internet Starpower.

### Retired communities (in the code, nobody belongs to them)

These left the game on 10 November (the first six) or in V5 (the rest). Their rules are kept so they can come back:
give a viewer one of them and say so. "Put-aside members" are viewers from section 7 that still carry the tag. The
Mods are back in the game (section 4) with a new rule; their old rule is kept below.

#### Content · `Content` (retired 10 November)
- **Rule:** each point gave **every viewer outside the Core Fanbase**, not only Content viewers, a **30% chance to
  score twice**.
- **Cap:** 3 points (90%).
- **Put-aside members:** Pepe, This Is Fine Dog, Wojak, Grumpy Cat, Nyan Cat, Pop Cat, Big Chungus, Hide the Pain
  Harold, Success Kid, Bad Luck Brian, Tung Tung Tung Sahur, Tralalero Tralala, Ballerina Cappuccina, Distracted
  Boyfriend, Drakeposting, Side Eye Chloe, Roll Safe, Dancing Baby.

#### The Mods' old rule (retired 10 November; the Mods came back with a new rule on 16 November)
- **Rule:** when the stream was scored, each point gave a **10% chance your next stream starts with 1 Risk
  cancelled**.
- **Cap:** 2 points (20%).

#### Hype Men's old rule (replaced 16 November)
- **Rule:** if the server went down, each point gave a **10% chance the stream still scored** (20% at 2 points).

#### Full Stream · `FullStream` (retired 10 November)
- **Rule:** bonuses that need a full chat counted chat as full with **up to 1 empty seat per point**.
- **Cap:** 3 points.

#### Marketers · `Marketers` (retired 10 November)
- **Rule:** each point gave **one more use of a Marketer's recruit action** this stream (shared).
- **Cap:** 3 points.

#### Shop Control · `ShopControl` (retired 10 November)
- **Rule:** when the stream was scored, each point made **your first recruit in the next shop cost 1 Cash less**.
- **Cap:** 5 points.

#### Saboteurs · `Saboteurs` (retired 10 November, multiplayer)
- **Rule:** when the stream was scored, each point made **every rival's first recruit in their next shop cost 1 Cash
  more**.
- **Cap:** 5 points.

#### Techspertise · `Techspertise` (retired)
- **Rule:** if the server goes down, the stream may still score: **20% / 40% / 50%** at 1 / 2 / 3 points.
- **Cap:** 3 points.
- **Put-aside members:** NPC Wojak, Keyboard Cat, Bongo Cat.

#### Finance Bros · `FinanceBros` (retired)
- **Rule:** each point gives every viewer a **10% chance to score its Influence twice**.
- **Cap:** 9 points (90%).
- **Put-aside members:** Gigachad, Stonks Guy, Sigma Male, Confused Math Lady.

#### Doomsdayers · `Doomsdayers` (retired, multiplayer)
- **Rule:** after your scored stream, each point gives every viewer in your rivals' next stream a **4% chance to score
  nothing**.
- **Cap:** 5 points (20%).
- **Put-aside members:** none.

#### Zoomer · `Zoomer` (retired; Gen-Z replaced it)
- **Rule:** when the stream is scored, two separate rolls, each **20% per point**: one for **+6 Cash**, one for
  **−3 Influence**.
- **Cap:** 4 points (80%).
- **Put-aside members:** Gigachad, Cheems, Chad vs Virgin, Bongo Cat, Pop Cat, Big Chungus, Ugandan Knuckles,
  Tung Tung Tung Sahur, Tralalero Tralala, Bombardiro Crocodilo, Ballerina Cappuccina, Skibidi Toilet, Sigma Male,
  Side Eye Chloe.

#### Armchair Activists · `ArmchairActivists` (retired)
- **Rule:** each point gives every Armchair Activist in chat a **10% chance to score nothing**. Other viewers aren't
  affected.
- **Cap:** 5 points (50%).
- **Put-aside members:** NPC Wojak, Karen, Roll Safe.

#### Clout · `Clout` (retired community; it has nothing to do with the resource that was called Clout, now Cash)
- **Rule:** each viewer outside the Core Fanbase has a **10% chance per point to bring +1 Cash more** every time it
  scores.
- **Cap:** 4 points (40%).
- **Put-aside members:** none.

#### Memetics Division · `MemeticsDivision` (retired)
- **Rule:** each point gives a **1% chance that the whole stream scores a second time**: every viewer and every
  community reward, rolled again.
- **Cap:** 10 points (10%).
- **Put-aside members:** none (Degen Dog came back without it).

#### Bots · `Bots` (retired, multiplayer)
- **Rule:** when your stream is scored, each Bots viewer in chat has a **10% chance per point to send a Bot** into the
  next player's next stream. A Bot takes a seat and earns nothing.
- **Cap:** 4 points (40%).
- **Put-aside members:** none.

---

## 5. Character sets

**How shops work:**
- **Every shop** sells **Loyal Fan and Fish**, on top of what's listed below (Random Mix included).
- **Preset sets:** each one's shop sells the viewers listed.
- **Random Mix:** sells Loyal Fan, Fish, 12 random regular viewers and 3 random Trophy viewers, picked again every
  season.
- **Open Lobby:** sells every buyable viewer.

| Set | Viewers in the shop (plus Loyal Fan and Fish) | Trophy viewers |
|---|---|---|
| **Going Live** (0) | Producer, Chat Admin, Normie, Junior PR Manager, Paid Sponsorship, Mildly Risky Collaborator, Marketing Manager, Promo Code User, Dancing Jen, Billionaire Benefactor, Influential Troll, Whale | Precocious Youth |
| **Clickbait Roulette** (1) | Risky Orbiter, Die Hard Fan, Social Media Manager, Junior PR Manager, Cybersecurity Engineer, Master of Currency, Gen Z with Job, Boomer Troll, Highly Risky Collaborator, Hate-Watcher, Influential Troll, Doomer | Mainstream Celebrity, Bigger Star |
| **Brand Deal Wishlist** (2) | Mildly Risky Collaborator, Affiliate, Chat Arbitrator, PR Manager, Crypto Enthusiast, Binge Bunny, Online Journalist, Executive PR, Normie, Generous Donator, Boomer Troll, Degen Dog | Evangelist, Big Boomer |
| **Monetization Meeting** (3) | Social Media Manager, PR Manager, Chat Admin, Affiliate, Crypto Enthusiast, Cybersecurity Engineer, Die Hard Fan, Account Manager, Paid Sponsorship, Highly Risky Collaborator, Billionaire Isolationist, Whale | Clippo The Clanker, Senior Marketing Manager |
| **Algorithm Magic** (4) | Billionaire Benefactor, Online Journalist, Generous Donator, Bot Detector, Influential Isolationist, Billionaire Isolationist, PR Manager, Die Hard Fan, Chat Admin, Tech Admin, Distraught Cat, Forever Alone | Platform Owner, Internet Starpower |

**What changed on 16 November, and why:**

| Set | Out | In | Why |
|---|---|---|---|
| Going Live | Cybersecurity Engineer | Junior PR Manager | The Precocious Youth is Gen-Z now; the Junior PR Manager is its Gen-Z partner. |
| Clickbait Roulette | Tech Admin | Boomer Troll | More Troll Army and Boomers for a risky set. Tech Admin was its only Mod. |
| Brand Deal Wishlist | Tech Admin, Hate-Watcher | Generous Donator, Boomer Troll | A third Hype Man (spare seats), and a fourth Boomer for level 2 (with the Big Boomer). |
| Monetization Meeting | Risky Orbiter | Billionaire Isolationist | A second Boomer for level 1, and another Boomer who can log off so a Millennial inherits. |
| Algorithm Magic | Normie, Gen Z with Job, Gatekeeper | Influential Isolationist (the old Gatekeeper), Billionaire Isolationist, PR Manager | Kicking (Mods, and the Gatekeepers with the Distraught Cat) leaves empty seats, which the Isolationists and Forever Alone profit from. |

**Communities per set** (the most each can reach with that set's viewers, Loyal Fan and Fish included; Ineffective
Trolls from the starting Audience count too):

| Set | Strong communities |
|---|---|
| Going Live | Staff, Core Fanbase, Boomers (level 1), Troll Army, Gen-Z (1 point) |
| Clickbait Roulette | Troll Army, Boomers (level 1), Millennials, Gen-Z (1 point), Voyeurs, Staff |
| Brand Deal Wishlist | Boomers (level 2), Hype Men (2 spare seats), Staff, Core Fanbase, Troll Army |
| Monetization Meeting | Staff, Millennials, Boomers (level 1), Voyeurs, Core Fanbase |
| Algorithm Magic | Mods (3 points), Gatekeepers (1 point), Hype Men (1 spare seat), Boomers (level 1), Core Fanbase |

---

## 6. Things you may want to decide

These are how the game behaves today. They look unintended or unfinished:

1. **Streamer actives:** Bro Jogan, IRunFast, Soft Hard Body Man and Manim Maketh Man got first-draft actives on 15
   November (see section 2). Change them if you have better ideas.
2. **Gatekeepers** have only two members (PR Manager, Distraught Cat), so they top out at 1 point: the PR Manager
   sends 3 or 6 viewers back, never "everyone else". A third Gatekeeper would unlock that.
3. **Troll Army** only needs 1 point; more points don't add more Cash. Say so if you want it to scale with points.
4. **Staff** stops at 3 points (+3 Influence), as before. It has 12 members, so the cap is easy to reach.
5. **Distraught Cat** is a Gatekeeper but has no action. It only adds to the Gatekeepers' points (and gains from the
   PR Manager's send-back).
6. **Producer's Shoutout** can already pull Trophy viewers from the queue, the same as the Evangelist's action, which
   is described as the one that can.
7. **Die Hard Fan's "Uncancellable"** is only a name: it behaves exactly like Normie.
8. **Crypto Enthusiast** still has an unused action name ("Rug Pull") in the strings, left from before it lost its
   action.
9. **Wealth Tax's in-game description** says "the most influential rival pays you", but the player picks the rival
   (see section 2). The description could say "a rival you pick" instead.
10. **The Mods have no icon yet.** The Gatekeepers icon stays on the Gatekeepers (the new random kickers). Give the
    Mods one in Game Database > Communities; until then they show a placeholder.

---

## 7. Viewers put aside (in the code, not in the game)

These 36 meme viewers were taken out in V4.2 (Degen Dog, Doomer and Forever Alone came back on 10 November). They keep
their rules, art and communities. To bring one back, write **BRING BACK** under it (and change anything you like); it
then joins section 3. Some of their communities are retired; those are marked.

#### Trollface · `Trollface`
- **Cost** 9 · **Scores** nothing printed · **Risk** 1
- **Action · U Mad?:** send another viewer back to the queue (they can join again). Gains +2 Cash for every time that
  viewer has been trolled, this time included.
- **Communities:** Troll Army, Millennials

#### Pepe · `Pepe`
- **Cost** 8 · **Scores** nothing printed
- **Action · Mood Swing:** choose Happy (+3 Cash), Sad (+3 Influence) or Angry (+6 Cash and +1 Risk) for the rest of the
  stream.
- **Communities:** Content (retired), Millennials

#### Gigachad · `Gigachad`
- **Cost** 19 · **Scores** +5 Cash
- **Effect · Alpha Energy:** +3 Cash if he's the only viewer in chat with 5+ printed Cash. Each extra Gigachad gives
  every Gigachad −2 Cash.
- **Communities:** Finance Bros (retired), Zoomer (retired)

#### This Is Fine Dog · `ThisIsFineDog`
- **Cost** 12 · **Scores** +1 Cash
- **Effect · Denial:** the first time Risk would reach 3, the deplatforming is called off and all Risk is cleared. This
  copy then leaves your Audience for good.
- **Communities:** Content (retired), Millennials

#### Wojak · `Wojak`
- **Cost** 5 · **Scores** +1 Cash
- **Effect · Shared Suffering:** +2 Cash for every other viewer who is suffering (carries Risk, has negative stats,
  is infected, zeroed or penalised), max +6.
- **Communities:** Content (retired), Millennials

#### Grumpy Cat · `GrumpyCat`
- **Cost** 9 · **Scores** +2 Cash
- **Action · Absolutely Not:** look at the next 3 viewers in the queue. Reject one for the stream and choose who joins
  next.
- **Communities:** Content (retired), Millennials

#### Nyan Cat · `NyanCat`
- **Cost** 14 · **Scores** +1 Cash
- **Effect · Rainbow Trail:** every viewer without Risk who joins after her in an unbroken row earns +1, +2, +3...
  Cash (max +6 each). A risky viewer joining breaks the chain.
- **Communities:** Content (retired), Millennials

#### Stonks Guy · `StonksGuy`
- **Cost** 8 · **Scores** +1 Influence
- **Action · Market Manipulation:** invest 2 Cash or 1 Influence. If the stream is scored you get triple back; if it
  fails, it's gone.
- **Communities:** Finance Bros (retired)

#### Cheems · `Cheems`
- **Cost** 5 · **Scores** +2 Cash
- **Effect · Don't Bonk Me:** with a Degen Dog in chat, cancels 1 Risk and both get +2 Cash. Without one, −1 Cash
  per Risk.
- **Communities:** Core Fanbase, Zoomer (retired)

#### Chad vs Virgin · `ChadVsVirgin`
- **Cost** 10 · **Scores** +2 Cash
- **Action · Know Your Place:** compare printed Cash with another viewer. If theirs is lower, they permanently give
  Chad 1. If higher, Chad permanently loses 1.
- **Communities:** Troll Army, Zoomer (retired)

#### NPC Wojak · `NpcWojak`
- **Cost** 6 · **Scores** nothing printed
- **Effect · Current Thing:** on joining, copies the printed Cash and Influence of the viewer who joined just before him.
  Not their action, Risk or Trophy.
- **Communities:** Techspertise (retired), Armchair Activists (retired)

#### Me Gusta · `MeGusta`
- **Cost** 8 · **Scores** +1 Cash
- **Effect · I Like That:** +2 Cash whenever another viewer adds Risk, and +1 Influence for each Risk removed.
- **Communities:** Troll Army, Millennials

#### Keyboard Cat · `KeyboardCat`
- **Cost** 7 · **Scores** +1 Cash
- **Action · Play Him Off:** remove another viewer. +2 Cash if they had Risk, otherwise +1 Influence. Their leaving effects
  still happen.
- **Communities:** Techspertise (retired), Millennials

#### Bongo Cat · `BongoCat`
- **Cost** 11 · **Scores** +1 Cash
- **Effect · The Beat Goes On:** every third viewer to join after Bongo Cat gives Bongo Cat and every viewer sharing
  one of its communities +2 Cash for the stream.
- **Communities:** Techspertise (retired), Zoomer (retired)

#### Pop Cat · `PopCat`
- **Cost** 9 · **Scores** nothing printed
- **Effect · Pop Pop Pop:** +1 Cash whenever any viewer uses an action. Every third action also earns +1 Influence.
- **Communities:** Content (retired), Zoomer (retired)

#### Big Chungus · `BigChungus`
- **Cost** 16 · **Scores** +7 Cash · **Takes** 3 seats
- **Effect · Absolute Chungus:** +1 Cash for each point of Sub Cap above 10 (max +5).
- **Communities:** Content (retired), Zoomer (retired)

#### Ugandan Knuckles · `UgandanKnuckles`
- **Cost** 9 · **Scores** +1 Cash
- **Effect · Follow Me:** on joining, adds a temporary copy of himself to the queue (copies don't make more copies).
  +1 Cash per other Knuckles in chat.
- **Communities:** Troll Army, Zoomer (retired)

#### Overly Attached Girlfriend · `OverlyAttachedGirlfriend`
- **Cost** 8 · **Scores** +1 Cash
- **Effect · Forever Yours:** on joining, attaches to the best other viewer. Their Cash is doubled and they can't be
  removed while she's in chat. If they're forced out, she leaves too.
- **Communities:** Core Fanbase, Millennials

#### Disaster Girl · `DisasterGirl`
- **Cost** 13 · **Scores** +2 Cash
- **Action · Accidental Fire:** another viewer gets +1 Risk and +3 Cash, and she gets +2 Cash. Can't be used if it
  would get the stream deplatformed.
- **Communities:** Troll Army

#### Hide the Pain Harold · `HideThePainHarold`
- **Cost** 10 · **Scores** +2 Cash
- **Effect · Smile Through It:** viewers in chat can't drop below their printed Cash. Gains +1 Cash for each drop he
  prevents (max +5).
- **Communities:** Content (retired), Boomers

#### Success Kid · `SuccessKid`
- **Cost** 12 · **Scores** +1 Cash
- **Action · Against All Odds:** reroll the last viewer who joined (they go back to the queue and someone new joins),
  or peek at the next viewer and decide whether to let them in.
- **Communities:** Content (retired), Millennials

#### Bad Luck Brian · `BadLuckBrian`
- **Cost** 6 · **Scores** +4 Cash · **Risk** 1
- **Effect · What Could Go Wrong?:** once per stream, the first attempt to remove him or clear his Risk fails, and he
  permanently gains +1 Cash.
- **Communities:** Content (retired), Millennials

#### Tung Tung Tung Sahur · `TungTungTungSahur`
- **Cost** 12 · **Scores** +2 Cash
- **Action · Three Strikes:** strike another viewer. 1st strike: +2 Cash. 2nd: clears their Risk. 3rd: removes them
  and Tung Tung permanently gains +1 Cash. Strikes are remembered between streams.
- **Communities:** Content (retired), Zoomer (retired)

#### Tralalero Tralala · `TralaleroTralala`
- **Cost** 11 · **Scores** +3 Cash
- **Action · Tralalero Shuffle:** swap two viewers' places in the joining order. Position-based bonuses update; joining
  effects don't replay.
- **Communities:** Content (retired), Zoomer (retired)

#### Bombardiro Crocodilo · `BombardiroCrocodilo`
- **Cost** 15 · **Scores** +5 Cash · **Risk** 1
- **Action · Bombardment:** remove up to 3 other viewers and replace each one with a random viewer from the queue.
  Joining and leaving effects happen normally.
- **Communities:** Troll Army, Zoomer (retired)

#### Ballerina Cappuccina · `BallerinaCappuccina`
- **Cost** 10 · **Scores** +2 Cash
- **Action · Pirouette:** two viewers swap their printed Cash for the rest of the stream.
- **Communities:** Content (retired), Zoomer (retired)

#### Skibidi Toilet · `SkibidiToilet`
- **Cost** 13 · **Scores** +2 Cash · **Risk** 1
- **Effect · Skibidi Invasion:** on joining, infects another viewer (+2 Cash, +1 Risk). Every time another viewer
  joins, the infection spreads to one more. Ends after the stream.
- **Communities:** Troll Army, Zoomer (retired)

#### Sigma Male · `SigmaMale`
- **Cost** 14 · **Scores** +3 Cash
- **Effect · The Grindset:** +1 Cash per empty seat (max +8). −1 Cash for every other Sigma Male in chat.
- **Communities:** Finance Bros (retired), Zoomer (retired)

#### Karen · `Karen`
- **Cost** 8 · **Scores** nothing printed · **Risk** 1
- **Action · I Demand a Refund:** remove another viewer for the stream and earn half their cost as Cash (max 8).
- **Communities:** Troll Army, Boomers, Armchair Activists (retired)

#### Rickroll · `Rickroll`
- **Cost** 11 · **Scores** +2 Cash
- **Action · Surprise!:** another viewer (not a Trophy viewer) temporarily becomes a random different character from
  the queue, with its stats and action. Back to normal after the stream.
- **Communities:** Troll Army, Millennials

#### Distracted Boyfriend · `DistractedBoyfriend`
- **Cost** 9 · **Scores** +2 Cash
- **Effect · New Attraction:** on joining, reveals 3 viewers in the queue. You may swap one viewer in chat for one of
  them; the replaced viewer sits out the stream.
- **Communities:** Content (retired), Millennials

#### Drakeposting · `Drakeposting`
- **Cost** 10 · **Scores** +1 Cash
- **Action · I Prefer This:** pick two viewers. The first earns nothing this stream; the second doubles their printed
  Cash or Influence.
- **Communities:** Content (retired), Millennials

#### Confused Math Lady · `ConfusedMathLady`
- **Cost** 12 · **Scores** +1 Cash
- **Action · The Numbers Don't Add Up:** for this stream, convert a viewer's Cash into Influence (2 Cash = 1 Influence) or
  their Influence into Cash (1 Influence = 2 Cash).
- **Communities:** Finance Bros (retired)

#### Side Eye Chloe · `SideEyeChloe`
- **Cost** 7 · **Scores** +1 Cash
- **Effect:** while she's in chat you can see the next viewer in the queue.
- **Action · Side Eye:** send that viewer to the back of the queue.
- **Communities:** Content (retired), Zoomer (retired)

#### Roll Safe · `RollSafe`
- **Cost** 9 · **Scores** +2 Cash
- **Action · Can't Lose If You Don't Play:** a viewer becomes immune to further action effects this stream, good or
  bad. What they already have stays.
- **Communities:** Content (retired), Armchair Activists (retired)

#### Dancing Baby · `DancingBaby`
- **Cost** 10 · **Scores** +1 Cash
- **Effect · Looping Animation:** whenever two actions in a row are the same type, gains +3 Cash (max +9 per stream).
- **Communities:** Content (retired), Millennials
