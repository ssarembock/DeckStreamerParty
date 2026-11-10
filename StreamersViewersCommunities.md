# Audience Capture: Streamers, Viewers and Communities

**Current rules (10 November 2026).** Everything below is what the game does today, taken from the game code, the Game
Database and the English strings.

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
| **Clout** | What viewers earn and what you recruit with (internally "Subs" / Pop). |
| **Cash** | Pays for Sub Cap upgrades, streamer actives, viewer fees and Remove a Viewer. |
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
  - **End the Stream** scores everyone in chat: each viewer earns its Clout and Cash, then community rolls happen.
  - Each viewer's **action** can be used once per stream, unless a community gives extra uses.
- **Sub Cap.**
  - The number of seats in chat: starts at 6, maximum 34.
  - Raising it costs 2 Cash, then 1 more for each raise after that (maximum 12).
  - **Server Down:** if chat goes over the Sub Cap, the stream ends and pays nothing.
- **Deplatformed.**
  - Each viewer marked "Risk 1" adds 1 Risk; "cancels 1 Risk" viewers take 1 away.
  - At 3 net Risk the stream is deplatformed: it pays nothing, and you pick one of the risky viewers to sit out your
    next stream.
- **Fees.**
  - Negative Cash (a viewer's "−1 Cash") is paid from your bank.
  - Every Cash you can't pay costs 7 Clout.
- **Winning.** 4 Trophy viewers in one stream that gets scored wins. A season lasts 25 streams by default (10 to 30
  in multiplayer).

### Your Audience and the shop

- **Starting Audience:** 4 Loyal Fan, 2 Fish, 5 Ineffective Troll.
- **Recruiting.**
  - Viewers cost Clout.
  - You can own up to 4 copies of each regular viewer; Dancing Jen and Crypto Enthusiast allow 5.
  - Trophy viewers have no limit.
- **Shared stock (multiplayer).** The shop's stock is shared by every player. Each regular viewer's stack holds
  **2 copies per player**: 4 for two players, 6 for three, 8 for four, and so on. Group cards always have at least 5.
  Trophy viewers are unlimited. The number of different viewers in the shop doesn't change.
- **Remove a Viewer** (shop): 2 Cash, +1 per use (maximum 12). It can't take your Audience below 5 viewers.

### How communities score

- **Points.** A community scores **1 point for every different member in chat after the first.** Two different
  members score 1 point, three score 2, and so on.
- **Copies.** Two copies of the same card count once.
- **Leaving.** A member who leaves chat takes its point away.
- **Caps.** Some communities stop counting at a maximum.

---

## 2. Streamers

Each player picks one streamer, or **No Streamer** (classic rules, no power), or **Random**.

**How the powers work:**
- **Passives** are always on.
- **Actives** are bought in the shop with Cash. Each use costs 1 more than the last (never more than 12).
- **Next stream** powers take effect in your next stream.
- **Rival** powers hit another player and only exist in multiplayer.
- **Cooldown 2** means the power can't be used again in the shops of the next 2 streams. It does not keep the power
  active: a power that affects a future stream only applies to the next one.

**Waiting on you:** streamers are to lose their passives, and each gets a unique active instead. The new actives
aren't written in this file yet, so Bro Jogan, IRunFast, Soft Hard Body Man and Manim Maketh Man still have their
passives below. Write each one's new active in its section.

The platform only changes the animated logo in the menu bar.

### Fate · `Fate`
- **Title:** The Debate Lord · **Platform:** Lookout
- **Power: Raid** (active, rival, multiplayer only). Costs 1 Cash (+1 per use), cooldown 2.
- **Rule:** pick a rival. Their next stream starts with +1 Risk. Raids alone can't push a stream past 2 Risk.
- **Quote:** "Debate me, bro."

### Bro Jogan · `BroJogan`
- **Title:** The Three-Hour Podcaster · **Platform:** Tunes
- **Power: Three-Hour Episode** (passive)
- **Rule:** start the season with +1 Sub Cap (7 seats). Every Sub Cap raise costs 1 Cash more.
- **Quote:** "Jamie, pull that up."

### Shinobi · `Shinobi`
- **Title:** The Headband Legend · **Platform:** Lookout
- **Power: Smoke Bomb** (active, next stream). Costs 2 Cash (+1 per use), cooldown 2.
- **Rule:** your next stream ignores 1 Risk.
- **Quote:** "You didn't see that. Nobody did."

### IRunFast · `IRunFast`
- **Title:** The Human Siren · **Platform:** Cough
- **Power: Bark at Chat** (passive)
- **Rule:** every viewer carrying Risk earns +1 Clout when scored.
- **Quote:** "SIUUUUUUU!"

### MrShark · `MrShark`
- **Title:** The Giveaway King · **Platform:** Cough
- **Power: $10,000 Giveaway** (active, next stream). Costs 3 Cash (+1 per use), cooldown 2.
- **Rule:** in your next stream, every viewer earns +1 Clout.
- **Quote:** "Last viewer to leave chat wins!"

### Ethical F**kboi · `EthicalFboi`
- **Title:** The Mansion Socialist · **Platform:** Stomp
- **Power: Wealth Tax** (active, rival, multiplayer only). Costs 1 Cash (+1 per use), cooldown 2.
- **Rule:** the richest rival pays you up to 2 Cash. A rival with no Cash can't be picked.
- **Quote:** "Eat the rich. Not me, though."

### Soft Hard Body Man · `SoftHardBodyMan`
- **Title:** Looks Hard, Is Soft · **Platform:** Swipe
- **Power: Soft Landing** (passive)
- **Rule:** when your stream gets deplatformed, you still bank half of that stream's Clout.
- **Quote:** "REFORMED. And a little bit squishy."

### Strategic Abomination · `StrategicAbomination`
- **Title:** Two Heads, Zero Chill · **Platform:** Post
- **Power: Double Move** (active, next stream). Costs 2 Cash (+1 per use), cooldown 2.
- **Rule:** in your next stream, the first viewer action you use can be used again.
- **Quote:** "The rook! THE ROOK!"

### Palmane · `Palmane`
- **Title:** Gotta Catch 'Em (Legally) · **Platform:** Swipe
- **Power: Pal Sphere** (active, instant). Costs 2 Cash (+1 per use), cooldown 3.
- **Rule:** catch a random viewer from your shop for free. Only viewers that cost 6 or less, aren't Trophy viewers and
  are still in stock; the copy comes out of the shared stock.
- **Quote:** "Hiii chat, who wants to be my pal?"

### Manim Maketh Man · `ManimMakethMan`
- **Title:** The Animated Proof · **Platform:** Post
- **Power: Beautiful Sequence** (passive)
- **Rule:** each scored stream in a row earns +1, +2, +3... Clout (maximum +5). A failed stream resets the streak.
- **Quote:** "Let's visualize that."

### Rude Radiologist · `RudeRadiologist`
- **Title:** Two-Time Back-to-Back Diagnostician · **Platform:** Stomp
- **Power: X-Ray Vision** (active, next stream). Costs 1 Cash (+1 per use), no cooldown (once per turn).
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

## 3. Viewers (48 cards in the game)

Each block lists:

- **Cost**, in Clout.
- **Scores**: what the viewer earns when the stream is scored.
- **Risk**: what it adds to or cancels from Deplatform Risk.
- **Action**: once per stream.
- **Effect**: anything else it does.
- **Communities**.
- **Sets**: the preset character sets whose shop sells it.

Every viewer also appears in **Open Lobby**, and every buyable one can appear in **Random Mix** (12 random regular
viewers plus 3 random Trophy viewers).

### 3.1 Starting viewers

#### Loyal Fan · `LoyalFan`
- **Cost** 2 · **Scores** +1 Clout
- **Effect:** a basic starting viewer. Always shows up.
- **Communities:** Core Fanbase
- **Sets:** starting Audience (4 copies)

#### Fish · `Fish`
- **Cost** 3 · **Scores** +1 Cash
- **Effect:** a basic starting viewer who pays for your first Sub Cap upgrades. Called Whale until 10 November; the
  new Whale (below) is the big Cash viewer.
- **Communities:** Core Fanbase
- **Sets:** starting Audience (2 copies)

#### Ineffective Troll · `IneffectiveTroll`
- **Cost** can't be bought · **Scores** +2 Clout · **Risk** 1
- **Effect:** starting viewer only. Lots of noise, a little Risk.
- **Communities:** Core Fanbase, Troll Army
- **Sets:** starting Audience (5 copies)

### 3.2 Regular viewers (by cost)

#### Producer · `Producer`
- **Cost** 3 · **Scores** +2 Clout, −1 Cash
- **Action · Shoutout:** choose any viewer in the queue to join chat, if there's room. Trophy viewers included: this is
  the same action as the Evangelist's.
- **Communities:** Staff
- **Sets:** Going Live

#### Influential Troll · `InfluentialTroll`
- **Cost** 3 · **Scores** +4 Clout · **Risk** 1
- **Effect:** lots of Clout, but trouble follows.
- **Communities:** Troll Army
- **Sets:** Going Live, Clickbait Roulette

#### Chat Admin · `ChatAdmin`
- **Cost** 4 · **Scores** +1 Clout, −1 Cash
- **Action · Remove From Chat:** remove one viewer from chat for the rest of the stream (it can remove itself).
- **Communities:** Staff, Gatekeepers (Gatekeepers points give extra uses)
- **Sets:** Going Live, Monetization Meeting, Algorithm Magic

#### Normie · `Normie`
- **Cost** 4 · **Scores** +1 Clout · **Risk** cancels 1
- **Communities:** Core Fanbase
- **Sets:** Going Live, Brand Deal Wishlist, Algorithm Magic

#### Cybersecurity Engineer · `CybersecurityEngineer`
- **Cost** 4 · **Scores** +3 Clout, −1 Cash
- **Action · Read Access:** look at the next viewer in the queue, then let them in or keep them out for the rest of the
  stream.
- **Communities:** Staff, Voyeurs (Voyeurs points give extra uses)
- **Sets:** Going Live, Clickbait Roulette, Monetization Meeting

#### Paid Sponsorship · `PaidSponsorship`
- **Cost** 4 · **Scores** −1 Clout, +3 Cash
- **Effect:** pays well, but viewers skip the ad.
- **Communities:** Boomers
- **Sets:** Going Live, Monetization Meeting

#### Gatekeeper · `Gatekeeper`
- **Cost** 4 · **Scores** +1 Clout
- **Effect · You Shall Not Watch!:** +1 Clout for every empty seat in chat when scored.
- **Communities:** Core Fanbase
- **Sets:** Algorithm Magic

#### Mildly Risky Collaborator · `MildlyRiskyCollaborator`
- **Cost** 5 · **Scores** +3 Clout, +2 Cash · **Risk** 1
- **Communities:** none
- **Sets:** Going Live, Brand Deal Wishlist

#### Junior PR Manager · `JuniorPrManager`
- **Cost** 5 · **Scores** +2 Clout, −1 Cash
- **Action · Situation Defusal:** send 1 random viewer in chat back to the queue (they can join again). Each copy
  sends one more every time it's used, for good: 1, then 2, then 3... Actions already used stay used.
- **Communities:** Staff, Gen-Z
- **Sets:** Clickbait Roulette

#### Marketing Manager · `MarketingManager`
- **Cost** 5 · **Scores** +4 Clout, −1 Cash
- **Effect · Viral Moment:** brings the next viewer from the queue along when joining. This can push chat over the
  Sub Cap.
- **Communities:** Staff
- **Sets:** Going Live

#### Promo Code User · `PromoCodeUser`
- **Cost** 5 · **Scores** +1 Clout, and +3 Cash only if chat is full when scored
- **Communities:** Core Fanbase
- **Sets:** Going Live

#### Account Manager · `AccountManager`
- **Cost** 5 · **Scores** +3 Clout, −1 Cash
- **Action · Upsell:** a viewer in chat permanently earns +1 Cash more (up to 9 Cash printed).
- **Communities:** Staff
- **Sets:** Monetization Meeting

#### Generous Donator · `GenerousDonator`
- **Cost** 5 · **Scores** +1 Clout, +1 Cash
- **Effect · Give to the Rich!:** when joining, brings in the next viewer from the queue and scores them once right
  away. That score is banked, so it's kept even if the stream fails.
- **Communities:** Hype Men
- **Sets:** Algorithm Magic

#### Binge Bunny · `BingeBunny`
- **Cost** 5 · **Scores** +1 Clout
- **Effect · Algorithmic Rabbit Hole:** +1 Clout for every Loyal Fan in chat.
- **Communities:** Core Fanbase
- **Sets:** Brand Deal Wishlist

#### Hate-Watcher · `HateWatcher`
- **Cost** 5 · **Scores** +4 Clout · **Risk** 1 on alternate entrances
- **Effect:** switches mood every time they join chat. Hate-watching (Risk 1) on the first entrance, harmless on the
  next, and so on. The mood is remembered between streams.
- **Communities:** Troll Army
- **Sets:** Clickbait Roulette, Brand Deal Wishlist

#### Crypto Enthusiast · `CryptoEnthusiast`
- **Cost** 6 · **Scores** as a group (below) · **Copies** up to 5
- **Effect · To the Moon:** all Crypto Enthusiasts in chat score Cash together: 0 / 1 / 4 / 9 / 16 Cash in total for
  1 / 2 / 3 / 4 / 5 of them. The total is split between the copies, with the remainder on the first one in chat.
- **Communities:** Millennials
- **Sets:** Brand Deal Wishlist, Monetization Meeting

#### PR Manager · `PrManager`
- **Cost** 6 · **Scores** +3 Clout, −1 Cash
- **Action · Damage Control:** send 5 random viewers in chat back to the queue (they can join again). Actions already
  used stay used.
- **Communities:** Staff, Gatekeepers (Gatekeepers points give extra uses)
- **Sets:** Brand Deal Wishlist, Monetization Meeting

#### Highly Risky Collaborator · `HighlyRiskyCollaborator`
- **Cost** 6 · **Scores** +4 Cash · **Risk** 1
- **Effect · #NotHimToo?:** big money, big Risk.
- **Communities:** none
- **Sets:** Clickbait Roulette, Monetization Meeting

#### Die Hard Fan · `DieHardFan`
- **Cost** 7 · **Scores** +2 Clout, +1 Cash · **Risk** cancels 1
- **Effect · Uncancellable:** the name only; it works exactly like Normie's cancel.
- **Communities:** Core Fanbase
- **Sets:** Clickbait Roulette, Monetization Meeting, Algorithm Magic

#### Chat Arbitrator · `ChatArbitrator`
- **Cost** 7 · **Scores** −1 Cash
- **Action · Keep the Peace:** clear the Risk of every risky viewer in chat for the rest of the stream, even if they
  leave and come back.
- **Communities:** Staff
- **Sets:** Brand Deal Wishlist

#### Risky Orbiter · `RiskyOrbiter`
- **Cost** 7 · **Scores** +2 Clout, +3 Cash · **Risk** 1
- **Communities:** none
- **Sets:** Clickbait Roulette, Monetization Meeting

#### Distraught Cat · `DistraughtCat`
- **Cost** 7 · **Scores** nothing printed
- **Effect · Everyone Leaves Me:** gains +1 Clout or +1 Cash (at random) for this stream whenever another viewer
  leaves chat or is removed.
- **Communities:** Gatekeepers
- **Sets:** Algorithm Magic

#### Dancing Jen · `DancingJen`
- **Cost** 7 · **Scores** as a group (below) · **Copies** up to 5
- **Effect · Dance Break:** all Dancing Jens in chat score Clout together: 1 / 4 / 9 / 16 / 25 Clout in total for
  1 / 2 / 3 / 4 / 5 of them. The total is split between the copies, with the remainder on the first one in chat.
- **Communities:** Millennials
- **Sets:** Going Live

#### Degen Dog · `DegenDog`
- **Cost** 7 · **Scores** +1 Clout
- **Effect · Such Synergy:** +1 Clout for each different community in chat (max +5). Other Degen Dogs don't count.
- **Communities:** none
- **Sets:** Brand Deal Wishlist

#### Doomer · `Doomer`
- **Cost** 7 · **Scores** +1 Clout
- **Effect · Doomscroll:** +2 Clout per Risk in the stream (net Risk, after cancels).
- **Communities:** none
- **Sets:** Clickbait Roulette

#### Affiliate · `Affiliate`
- **Cost** 8 · **Scores** +1 Clout
- **Effect · Checkout Code:** when the stream is scored, a random viewer in chat permanently earns +1 Cash more (up
  to 9 Cash printed). The game picks the viewer (it can be the Affiliate itself), and the +1 already counts in that
  stream.
- **Communities:** none
- **Sets:** Brand Deal Wishlist, Monetization Meeting

#### Bot Detector · `BotDetector`
- **Cost** 8 · **Scores** +1 Clout
- **Action · Bot Removal:** remove two viewers sitting next to each other in chat for the rest of the stream (the Bot
  Detector can be one of them).
- **Communities:** Gatekeepers (Gatekeepers points give extra uses)
- **Sets:** Algorithm Magic

#### Social Media Manager · `SocialMediaManager`
- **Cost** 8 · **Scores** +3 Clout, −1 Cash
- **Action · Sneak Peek:** look at the next viewer in the queue, then let them in or keep them out for the rest of the
  stream. This is the same action as the Cybersecurity Engineer's.
- **Communities:** Staff, Voyeurs (Voyeurs points give extra uses)
- **Sets:** Clickbait Roulette, Monetization Meeting

#### Master of Currency · `MasterOfCurrency`
- **Cost** 8 · **Scores** +1 Cash
- **Effect · Chaos Ladder:** +2 Cash for every viewer in chat who carries Risk (cancelled Risk still counts).
- **Communities:** Troll Army, Boomers
- **Sets:** Clickbait Roulette

#### Whale · `Whale`
- **Cost** 8 · **Scores** +4 Cash
- **Effect:** none. It just scores a lot of Cash.
- **Communities:** Core Fanbase
- **Sets:** Going Live, Monetization Meeting

#### Forever Alone · `ForeverAlone`
- **Cost** 8 · **Scores** +1 Clout
- **Effect · Table for One:** +10 Clout if he's the only viewer in chat when scored. Otherwise +1 Clout per empty seat.
- **Communities:** Millennials
- **Sets:** Algorithm Magic

#### Billionaire Benefactor · `BillionaireBenefactor`
- **Cost** 9 · **Scores** +5 Cash
- **Action · Softball Interview** (once per stream for all copies together): every viewer in chat drops to 0 Clout
  for this stream, and you bank 1 Cash right away for each Billionaire Benefactor in chat.
- **Communities:** Boomers
- **Sets:** Going Live, Algorithm Magic

#### Tech Admin · `TechAdmin`
- **Cost** 9 · **Scores** +2 Clout
- **Action · Admin Rights:** remove one viewer from chat for the rest of the stream (it can remove itself).
- **Communities:** Gatekeepers (Gatekeepers points give extra uses)
- **Sets:** Clickbait Roulette, Brand Deal Wishlist, Algorithm Magic

#### Online Journalist · `OnlineJournalist`
- **Cost** 11 · **Scores** +2 Clout, +3 Cash
- **Effect · Unwanted Guests:** brings the next two viewers from the queue along when joining. This can push chat over
  the Sub Cap. (Called Online Scandal until 10 November.)
- **Communities:** Hype Men
- **Sets:** Brand Deal Wishlist, Algorithm Magic

#### Executive PR · `ExecutivePr`
- **Cost** 11 · **Scores** +1 Clout, −1 Cash
- **Effect · No Bad Press:** +2 Clout for every viewer in chat who carries Risk (cancelled Risk still counts).
- **Communities:** Staff, Boomers
- **Sets:** Brand Deal Wishlist

#### Gen Z with Job · `GenZWithJob`
- **Cost** 12 · **Scores** nothing printed at first
- **Effect · Secure The Bag:** permanently gains +1 Cash every time they join chat (up to 9 Cash printed), even if the
  stream then fails.
- **Communities:** Gen-Z
- **Sets:** Clickbait Roulette, Algorithm Magic

### 3.3 Trophy viewers

Four of these in one stream that gets scored wins the season. There's no copy limit, and the shop never runs out of
them.

#### Big Boomer · `BigBoomer`
- **Cost** 25 · **Scores** +1 Cash · **Risk** 1
- **Effect · Opinion Express:** adds 1 Risk.
- **Communities:** Boomers
- **Sets:** Brand Deal Wishlist

#### Clippo The Clanker · `ClippoTheClanker`
- **Cost** 25 · **Scores** −2 Clout, −1 Cash
- **Effect · Market Collapse:** costs 1 Cash and 2 Clout when scored.
- **Communities:** Staff
- **Sets:** Monetization Meeting

#### Bigger Star · `BiggerStar`
- **Cost** 35 · **Scores** nothing
- **Effect · Uplifting:** brings the next viewer from the queue along when joining.
- **Communities:** Hype Men
- **Sets:** Clickbait Roulette

#### Precocious Youth · `PrecociousYouth`
- **Cost** 35 · **Scores** nothing
- **Effect:** none.
- **Communities:** none
- **Sets:** Going Live

#### Internet Starpower · `InternetStarpower`
- **Cost** 45 · **Scores** nothing · **Risk** cancels 1
- **Effect · Can Do No Wrong:** cancels 1 Risk while in chat.
- **Communities:** none
- **Sets:** Algorithm Magic

#### Platform Owner · `PlatformOwner`
- **Cost** 45 · **Scores** +3 Cash
- **Action · Free The Speech!:** remove one viewer from chat for the rest of the stream (it can remove itself).
- **Communities:** Boomers, Gatekeepers (Gatekeepers points give extra uses)
- **Sets:** Algorithm Magic

#### Mainstream Celebrity · `MainstreamCelebrity`
- **Cost** 50 · **Scores** +3 Clout, +1 Cash
- **Effect · Mass Appeal:** no extra effect.
- **Communities:** none
- **Sets:** Clickbait Roulette

#### Senior Marketing Manager · `SeniorMarketingManager`
- **Cost** 50 · **Scores** +5 Clout, −1 Cash
- **Effect · Product Placement:** no extra effect.
- **Communities:** Staff, Millennials
- **Sets:** Monetization Meeting

#### Evangelist · `Evangelist`
- **Cost** 55 · **Scores** nothing
- **Action · 2-for-1!:** choose any viewer in the queue to join chat, even another Trophy viewer.
- **Communities:** Hype Men
- **Sets:** Brand Deal Wishlist

---

## 4. Communities (9 in the game)

**Points:** the number of different members in chat, minus 1, up to the cap.

The member lists are worked out from the viewer blocks above. To move a viewer, edit its block; you don't need to
update both places.

### Core Fanbase · `CoreFanbase`
- **Rule:** each point gives every Core Fanbase viewer a **10% chance to score twice** when the stream ends. A viewer
  who scores twice plays its scoring animation twice.
- **Cap:** 3 points (30%).
- **Members (9):** Loyal Fan, Fish, Whale, Ineffective Troll, Normie, Gatekeeper, Promo Code User, Binge Bunny, Die
  Hard Fan.

### Staff · `Staff`
- **Rule:** each point adds **+1 Cash** when the stream is scored. It's a flat reward, and it flies from the Staff chip
  in Community Beats.
- **Cap:** 3 points (+3 Cash).
- **Members (12):** Producer, Chat Admin, Cybersecurity Engineer, Junior PR Manager, Marketing Manager, Account
  Manager, PR Manager, Chat Arbitrator, Social Media Manager, Executive PR, Clippo The Clanker, Senior Marketing
  Manager.

### Boomers · `Boomer`
- **Rule:** each point makes Sub Cap upgrades **1 Cash cheaper in your next shop**.
- **Retirement:** after a scored stream, each Boomer that was in chat has a **10% chance to leave your Audience for
  good**.
- **Cap:** none.
- **Members (6):** Paid Sponsorship, Master of Currency, Billionaire Benefactor, Executive PR, Big Boomer, Platform
  Owner.

### Millennials · `Millennial`
- **Rule:** each time a Boomer leaves your Audience, a random Millennial you own **permanently earns +1 Cash**.
- **How often:** once per point, and at least once even with 0 points. You need a Millennial in your Audience;
  they don't have to be in chat.
- **Members (4):** Crypto Enthusiast, Dancing Jen, Forever Alone, Senior Marketing Manager.

### Gen-Z · `GenZ`
- **Rule:** after a scored stream, each Gen-Z viewer in chat **permanently gains +1 Clout or +1 Cash** (at random),
  once per point.
- **Cap:** 2 points. Printed values stop growing at 9.
- **Members (2):** Junior PR Manager, Gen Z with Job. *Two members means 1 point at most.*

### Gatekeepers · `Gatekeepers`
- **Rule:** each point gives **one more use of a Gatekeeper's removal action** this stream. The extra uses are shared
  between all Gatekeepers in chat.
- **Which actions count:** Remove From Chat, Admin Rights, Free The Speech!, Bot Removal, and sending viewers back
  (Damage Control).
- **Cap:** none.
- **Members (6):** Chat Admin, PR Manager, Distraught Cat, Bot Detector, Tech Admin, Platform Owner.

### Hype Men · `HypeMen`
- **Rule:** if the server goes down (chat over the Sub Cap), each point gives a **10% chance the stream still
  scores**.
- **Cap:** 2 points (20%).
- **Members (4):** Generous Donator, Online Journalist, Bigger Star, Evangelist.

### Troll Army · `Trolls`
- **Rule:** once the community has at least 1 point, a scored stream earns **+2 Clout for every point of Risk** in it.
  Cancelled Risk counts too. It flies from the Troll Army chip. Deplatformed streams aren't scored, so they get
  nothing.
- **Cap:** none. Points beyond the first don't add more.
- **Members (4):** Ineffective Troll, Influential Troll, Hate-Watcher, Master of Currency.

### Voyeurs · `Voyeurs`
- **Rule:** each point gives **one more use of a Voyeur's peek action** this stream (shared): Read Access and Sneak
  Peek.
- **Cap:** 1 point.
- **Members (2):** Cybersecurity Engineer, Social Media Manager.

### Retired communities (in the code, nobody belongs to them)

These left the game on 10 November (the first six) or in V5 (the rest). Their rules are kept so they can come back:
give a viewer one of them and say so. "Put-aside members" are viewers from section 7 that still carry the tag.

#### Content · `Content` (retired 10 November)
- **Rule:** each point gave **every viewer outside the Core Fanbase**, not only Content viewers, a **30% chance to
  score twice**.
- **Cap:** 3 points (90%).
- **Put-aside members:** Pepe, This Is Fine Dog, Wojak, Grumpy Cat, Nyan Cat, Pop Cat, Big Chungus, Hide the Pain
  Harold, Success Kid, Bad Luck Brian, Tung Tung Tung Sahur, Tralalero Tralala, Ballerina Cappuccina, Distracted
  Boyfriend, Drakeposting, Side Eye Chloe, Roll Safe, Dancing Baby.

#### Mods · `Mods` (retired 10 November)
- **Rule:** when the stream was scored, each point gave a **10% chance your next stream starts with 1 Risk
  cancelled**.
- **Cap:** 2 points (20%).

#### Full Stream · `FullStream` (retired 10 November)
- **Rule:** bonuses that need a full chat counted chat as full with **up to 1 empty seat per point**.
- **Cap:** 3 points.

#### Marketers · `Marketers` (retired 10 November)
- **Rule:** each point gave **one more use of a Marketer's recruit action** this stream (shared).
- **Cap:** 3 points.

#### Shop Control · `ShopControl` (retired 10 November)
- **Rule:** when the stream was scored, each point made **your first recruit in the next shop cost 1 Clout less**.
- **Cap:** 5 points.

#### Saboteurs · `Saboteurs` (retired 10 November, multiplayer)
- **Rule:** when the stream was scored, each point made **every rival's first recruit in their next shop cost 1 Clout
  more**.
- **Cap:** 5 points.

#### Techspertise · `Techspertise` (retired)
- **Rule:** if the server goes down, the stream may still score: **20% / 40% / 50%** at 1 / 2 / 3 points.
- **Cap:** 3 points.
- **Put-aside members:** NPC Wojak, Keyboard Cat, Bongo Cat.

#### Finance Bros · `FinanceBros` (retired)
- **Rule:** each point gives every viewer a **10% chance to score its Cash twice**.
- **Cap:** 9 points (90%).
- **Put-aside members:** Gigachad, Stonks Guy, Sigma Male, Confused Math Lady.

#### Doomsdayers · `Doomsdayers` (retired, multiplayer)
- **Rule:** after your scored stream, each point gives every viewer in your rivals' next stream a **4% chance to score
  nothing**.
- **Cap:** 5 points (20%).
- **Put-aside members:** none.

#### Zoomer · `Zoomer` (retired; Gen-Z replaced it)
- **Rule:** when the stream is scored, two separate rolls, each **20% per point**: one for **+6 Clout**, one for
  **−3 Cash**.
- **Cap:** 4 points (80%).
- **Put-aside members:** Gigachad, Cheems, Chad vs Virgin, Bongo Cat, Pop Cat, Big Chungus, Ugandan Knuckles,
  Tung Tung Tung Sahur, Tralalero Tralala, Bombardiro Crocodilo, Ballerina Cappuccina, Skibidi Toilet, Sigma Male,
  Side Eye Chloe.

#### Armchair Activists · `ArmchairActivists` (retired)
- **Rule:** each point gives every Armchair Activist in chat a **10% chance to score nothing**. Other viewers aren't
  affected.
- **Cap:** 5 points (50%).
- **Put-aside members:** NPC Wojak, Karen, Roll Safe.

#### Clout · `Clout` (retired community; not the resource)
- **Rule:** each viewer outside the Core Fanbase has a **10% chance per point to bring +1 Clout more** every time it
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
- **Preset sets:** each one's shop sells the viewers listed.
- **Random Mix:** sells 12 random regular viewers and 3 random Trophy viewers, picked again every season.
- **Open Lobby:** sells every buyable viewer.

| Set | Viewers in the shop | Trophy viewers |
|---|---|---|
| **Going Live** (0) | Producer, Chat Admin, Normie, Cybersecurity Engineer, Paid Sponsorship, Mildly Risky Collaborator, Marketing Manager, Promo Code User, Dancing Jen, Billionaire Benefactor, Influential Troll, Whale | Precocious Youth |
| **Clickbait Roulette** (1) | Risky Orbiter, Die Hard Fan, Social Media Manager, Junior PR Manager, Cybersecurity Engineer, Master of Currency, Gen Z with Job, Tech Admin, Highly Risky Collaborator, Hate-Watcher, Influential Troll, Doomer | Mainstream Celebrity, Bigger Star |
| **Brand Deal Wishlist** (2) | Mildly Risky Collaborator, Affiliate, Chat Arbitrator, PR Manager, Crypto Enthusiast, Binge Bunny, Online Journalist, Executive PR, Normie, Tech Admin, Hate-Watcher, Degen Dog | Evangelist, Big Boomer |
| **Monetization Meeting** (3) | Social Media Manager, PR Manager, Chat Admin, Affiliate, Crypto Enthusiast, Cybersecurity Engineer, Die Hard Fan, Account Manager, Paid Sponsorship, Highly Risky Collaborator, Risky Orbiter, Whale | Clippo The Clanker, Senior Marketing Manager |
| **Algorithm Magic** (4) | Billionaire Benefactor, Online Journalist, Generous Donator, Bot Detector, Gatekeeper, Gen Z with Job, Normie, Die Hard Fan, Chat Admin, Tech Admin, Distraught Cat, Forever Alone | Platform Owner, Internet Starpower |

---

## 6. Things you may want to decide

These are how the game behaves today. They look unintended or unfinished:

1. **Streamer actives:** the four passive streamers (Bro Jogan, IRunFast, Soft Hard Body Man, Manim Maketh Man)
   still need their new actives (see section 2).
2. **Gen-Z** has only two members now (Affiliate left it), so it tops out at 1 point.
3. **Troll Army** only needs 1 point; more points don't add more Clout. Say so if you want it to scale with points.
4. **Staff** stops at 3 points (+3 Cash), as before. It has 12 members, so the cap is easy to reach.
5. **Distraught Cat** is a Gatekeeper but has no removal action. It only adds to the Gatekeepers' points.
6. **Producer's Shoutout** can already pull Trophy viewers from the queue, the same as the Evangelist's action, which
   is described as the one that can.
7. **Die Hard Fan's "Uncancellable"** is only a name: it behaves exactly like Normie.
8. **Crypto Enthusiast** still has an unused action name ("Rug Pull") in the strings, left from before it lost its
   action.

---

## 7. Viewers put aside (in the code, not in the game)

These 36 meme viewers were taken out in V4.2 (Degen Dog, Doomer and Forever Alone came back on 10 November). They keep
their rules, art and communities. To bring one back, write **BRING BACK** under it (and change anything you like); it
then joins section 3. Some of their communities are retired; those are marked.

#### Trollface · `Trollface`
- **Cost** 9 · **Scores** nothing printed · **Risk** 1
- **Action · U Mad?:** send another viewer back to the queue (they can join again). Gains +2 Clout for every time that
  viewer has been trolled, this time included.
- **Communities:** Troll Army, Millennials

#### Pepe · `Pepe`
- **Cost** 8 · **Scores** nothing printed
- **Action · Mood Swing:** choose Happy (+3 Clout), Sad (+3 Cash) or Angry (+6 Clout and +1 Risk) for the rest of the
  stream.
- **Communities:** Content (retired), Millennials

#### Gigachad · `Gigachad`
- **Cost** 19 · **Scores** +5 Clout
- **Effect · Alpha Energy:** +3 Clout if he's the only viewer in chat with 5+ printed Clout. Each extra Gigachad gives
  every Gigachad −2 Clout.
- **Communities:** Finance Bros (retired), Zoomer (retired)

#### This Is Fine Dog · `ThisIsFineDog`
- **Cost** 12 · **Scores** +1 Clout
- **Effect · Denial:** the first time Risk would reach 3, the deplatforming is called off and all Risk is cleared. This
  copy then leaves your Audience for good.
- **Communities:** Content (retired), Millennials

#### Wojak · `Wojak`
- **Cost** 5 · **Scores** +1 Clout
- **Effect · Shared Suffering:** +2 Clout for every other viewer who is suffering (carries Risk, has negative stats,
  is infected, zeroed or penalised), max +6.
- **Communities:** Content (retired), Millennials

#### Grumpy Cat · `GrumpyCat`
- **Cost** 9 · **Scores** +2 Clout
- **Action · Absolutely Not:** look at the next 3 viewers in the queue. Reject one for the stream and choose who joins
  next.
- **Communities:** Content (retired), Millennials

#### Nyan Cat · `NyanCat`
- **Cost** 14 · **Scores** +1 Clout
- **Effect · Rainbow Trail:** every viewer without Risk who joins after her in an unbroken row earns +1, +2, +3...
  Clout (max +6 each). A risky viewer joining breaks the chain.
- **Communities:** Content (retired), Millennials

#### Stonks Guy · `StonksGuy`
- **Cost** 8 · **Scores** +1 Cash
- **Action · Market Manipulation:** invest 2 Clout or 1 Cash. If the stream is scored you get triple back; if it
  fails, it's gone.
- **Communities:** Finance Bros (retired)

#### Cheems · `Cheems`
- **Cost** 5 · **Scores** +2 Clout
- **Effect · Don't Bonk Me:** with a Degen Dog in chat, cancels 1 Risk and both get +2 Clout. Without one, −1 Clout
  per Risk.
- **Communities:** Core Fanbase, Zoomer (retired)

#### Chad vs Virgin · `ChadVsVirgin`
- **Cost** 10 · **Scores** +2 Clout
- **Action · Know Your Place:** compare printed Clout with another viewer. If theirs is lower, they permanently give
  Chad 1. If higher, Chad permanently loses 1.
- **Communities:** Troll Army, Zoomer (retired)

#### NPC Wojak · `NpcWojak`
- **Cost** 6 · **Scores** nothing printed
- **Effect · Current Thing:** on joining, copies the printed Clout and Cash of the viewer who joined just before him.
  Not their action, Risk or Trophy.
- **Communities:** Techspertise (retired), Armchair Activists (retired)

#### Me Gusta · `MeGusta`
- **Cost** 8 · **Scores** +1 Clout
- **Effect · I Like That:** +2 Clout whenever another viewer adds Risk, and +1 Cash for each Risk removed.
- **Communities:** Troll Army, Millennials

#### Keyboard Cat · `KeyboardCat`
- **Cost** 7 · **Scores** +1 Clout
- **Action · Play Him Off:** remove another viewer. +2 Clout if they had Risk, otherwise +1 Cash. Their leaving effects
  still happen.
- **Communities:** Techspertise (retired), Millennials

#### Bongo Cat · `BongoCat`
- **Cost** 11 · **Scores** +1 Clout
- **Effect · The Beat Goes On:** every third viewer to join after Bongo Cat gives Bongo Cat and every viewer sharing
  one of its communities +2 Clout for the stream.
- **Communities:** Techspertise (retired), Zoomer (retired)

#### Pop Cat · `PopCat`
- **Cost** 9 · **Scores** nothing printed
- **Effect · Pop Pop Pop:** +1 Clout whenever any viewer uses an action. Every third action also earns +1 Cash.
- **Communities:** Content (retired), Zoomer (retired)

#### Big Chungus · `BigChungus`
- **Cost** 16 · **Scores** +7 Clout · **Takes** 3 seats
- **Effect · Absolute Chungus:** +1 Clout for each point of Sub Cap above 10 (max +5).
- **Communities:** Content (retired), Zoomer (retired)

#### Ugandan Knuckles · `UgandanKnuckles`
- **Cost** 9 · **Scores** +1 Clout
- **Effect · Follow Me:** on joining, adds a temporary copy of himself to the queue (copies don't make more copies).
  +1 Clout per other Knuckles in chat.
- **Communities:** Troll Army, Zoomer (retired)

#### Overly Attached Girlfriend · `OverlyAttachedGirlfriend`
- **Cost** 8 · **Scores** +1 Clout
- **Effect · Forever Yours:** on joining, attaches to the best other viewer. Their Clout is doubled and they can't be
  removed while she's in chat. If they're forced out, she leaves too.
- **Communities:** Core Fanbase, Millennials

#### Disaster Girl · `DisasterGirl`
- **Cost** 13 · **Scores** +2 Clout
- **Action · Accidental Fire:** another viewer gets +1 Risk and +3 Clout, and she gets +2 Clout. Can't be used if it
  would get the stream deplatformed.
- **Communities:** Troll Army

#### Hide the Pain Harold · `HideThePainHarold`
- **Cost** 10 · **Scores** +2 Clout
- **Effect · Smile Through It:** viewers in chat can't drop below their printed Clout. Gains +1 Clout for each drop he
  prevents (max +5).
- **Communities:** Content (retired), Boomers

#### Success Kid · `SuccessKid`
- **Cost** 12 · **Scores** +1 Clout
- **Action · Against All Odds:** reroll the last viewer who joined (they go back to the queue and someone new joins),
  or peek at the next viewer and decide whether to let them in.
- **Communities:** Content (retired), Millennials

#### Bad Luck Brian · `BadLuckBrian`
- **Cost** 6 · **Scores** +4 Clout · **Risk** 1
- **Effect · What Could Go Wrong?:** once per stream, the first attempt to remove him or clear his Risk fails, and he
  permanently gains +1 Clout.
- **Communities:** Content (retired), Millennials

#### Tung Tung Tung Sahur · `TungTungTungSahur`
- **Cost** 12 · **Scores** +2 Clout
- **Action · Three Strikes:** strike another viewer. 1st strike: +2 Clout. 2nd: clears their Risk. 3rd: removes them
  and Tung Tung permanently gains +1 Clout. Strikes are remembered between streams.
- **Communities:** Content (retired), Zoomer (retired)

#### Tralalero Tralala · `TralaleroTralala`
- **Cost** 11 · **Scores** +3 Clout
- **Action · Tralalero Shuffle:** swap two viewers' places in the joining order. Position-based bonuses update; joining
  effects don't replay.
- **Communities:** Content (retired), Zoomer (retired)

#### Bombardiro Crocodilo · `BombardiroCrocodilo`
- **Cost** 15 · **Scores** +5 Clout · **Risk** 1
- **Action · Bombardment:** remove up to 3 other viewers and replace each one with a random viewer from the queue.
  Joining and leaving effects happen normally.
- **Communities:** Troll Army, Zoomer (retired)

#### Ballerina Cappuccina · `BallerinaCappuccina`
- **Cost** 10 · **Scores** +2 Clout
- **Action · Pirouette:** two viewers swap their printed Clout for the rest of the stream.
- **Communities:** Content (retired), Zoomer (retired)

#### Skibidi Toilet · `SkibidiToilet`
- **Cost** 13 · **Scores** +2 Clout · **Risk** 1
- **Effect · Skibidi Invasion:** on joining, infects another viewer (+2 Clout, +1 Risk). Every time another viewer
  joins, the infection spreads to one more. Ends after the stream.
- **Communities:** Troll Army, Zoomer (retired)

#### Sigma Male · `SigmaMale`
- **Cost** 14 · **Scores** +3 Clout
- **Effect · The Grindset:** +1 Clout per empty seat (max +8). −1 Clout for every other Sigma Male in chat.
- **Communities:** Finance Bros (retired), Zoomer (retired)

#### Karen · `Karen`
- **Cost** 8 · **Scores** nothing printed · **Risk** 1
- **Action · I Demand a Refund:** remove another viewer for the stream and earn half their cost as Clout (max 8).
- **Communities:** Troll Army, Boomers, Armchair Activists (retired)

#### Rickroll · `Rickroll`
- **Cost** 11 · **Scores** +2 Clout
- **Action · Surprise!:** another viewer (not a Trophy viewer) temporarily becomes a random different character from
  the queue, with its stats and action. Back to normal after the stream.
- **Communities:** Troll Army, Millennials

#### Distracted Boyfriend · `DistractedBoyfriend`
- **Cost** 9 · **Scores** +2 Clout
- **Effect · New Attraction:** on joining, reveals 3 viewers in the queue. You may swap one viewer in chat for one of
  them; the replaced viewer sits out the stream.
- **Communities:** Content (retired), Millennials

#### Drakeposting · `Drakeposting`
- **Cost** 10 · **Scores** +1 Clout
- **Action · I Prefer This:** pick two viewers. The first earns nothing this stream; the second doubles their printed
  Clout or Cash.
- **Communities:** Content (retired), Millennials

#### Confused Math Lady · `ConfusedMathLady`
- **Cost** 12 · **Scores** +1 Clout
- **Action · The Numbers Don't Add Up:** for this stream, convert a viewer's Clout into Cash (2 Clout = 1 Cash) or
  their Cash into Clout (1 Cash = 2 Clout).
- **Communities:** Finance Bros (retired)

#### Side Eye Chloe · `SideEyeChloe`
- **Cost** 7 · **Scores** +1 Clout
- **Effect:** while she's in chat you can see the next viewer in the queue.
- **Action · Side Eye:** send that viewer to the back of the queue.
- **Communities:** Content (retired), Zoomer (retired)

#### Roll Safe · `RollSafe`
- **Cost** 9 · **Scores** +2 Clout
- **Action · Can't Lose If You Don't Play:** a viewer becomes immune to further action effects this stream, good or
  bad. What they already have stays.
- **Communities:** Content (retired), Armchair Activists (retired)

#### Dancing Baby · `DancingBaby`
- **Cost** 10 · **Scores** +1 Clout
- **Effect · Looping Animation:** whenever two actions in a row are the same type, gains +3 Clout (max +9 per stream).
- **Communities:** Content (retired), Millennials
