# Community Update Guide (ViewerCardsAndCommunityLogic.xlsx)

This update applies the spreadsheet's two tabs (Synergies and Cards) and replaces the character sets with five sets modelled on Party House.

## 1. Do this first in Unity

1. Let the scripts compile.
2. Run **Deck Party > Setup > Rebuild Scenes and UI Prefabs**.
   - This rebuilds the Community Beats chips for the new communities and re-imports the strings.
   - It also gives each new community an icon slot in the Game Database.
3. Every roster card's communities are switched to the spreadsheet's once (database revision 4). Community edits you made before this are replaced. After that, your edits are kept again.
4. Old saves still load. Multiplayer needs everyone on this build (protocol `deckparty-9`).

## 2. What's in and out

- **Out of the game:**
  - Degen Dog is put aside.
  - The Bot token is gone, because the Bots community is out.
  - Eight communities are retired: Techspertise, Finance Bros, Doomsdayers, Zoomer, Armchair Activists, Clout, Memetics Division and Bots.

  Their code, art and icons stay in the project. They are hidden from the shop, the presets and the Game Database (viewer list, community picker and icon list). No card can belong to a retired community, so retired communities never score.
- **The roster:** 43 cards, with the stats and communities from the Cards tab. The sheet also tagged some cards Memetics Division, Bots or Clout; those tags were dropped because the communities are out.
- **Communities in the game (15):** Core Fanbase, Staff, Content, Boomers, Millennials, Gen-Z, Gatekeepers, Hype Men, Troll Army, Mods, Full Stream, Marketers, Voyeurs, Shop Control and Saboteurs.

## 3. How each community works

| Community | Rule (per point) | Max |
|---|---|---|
| Core Fanbase | 10% that each Core Fanbase viewer scores twice | 9 |
| Staff | 10% that a Staff viewer's Cash cost is waived (unchanged) | 9 |
| Content | 10% that each viewer outside the Core Fanbase scores twice (unchanged) | 9 |
| Boomers | Sub Cap upgrades cost 1 Cash less in your next shop. After a scored stream, each Boomer in chat has a 10% chance to leave your Audience for good (a toast, a flash and a summary line show it). | none |
| Millennials | When a Boomer leaves, a random Millennial you own gains +1 Cash for good. This happens once per point, and at least once. With no Millennials, nothing happens. | none |
| Gen-Z | After a scored stream, each Gen-Z viewer in chat gains +1 Clout or +1 Cash for good (random), once per point. Each stat is capped at 9. | 3 |
| Gatekeepers | One more use of a Gatekeeper's removal action this stream. The extra uses are shared, and the action lights up again with "AGAIN!". | none |
| Hype Men | 10 / 20 / 30% that a Server Down stream still scores | 3 |
| Troll Army | Same as the old Trolls: 1 random Clout or Cash when deplatformed | none |
| Mods | 10% (rolled when the stream is scored) that your next stream starts with 1 Risk cancelled | 3 |
| Full Stream | "Chat is full" bonuses (Promo Code User) allow 1 empty space | 3 |
| Marketers | One more use of a Marketer's recruit action this stream (shared) | none |
| Voyeurs | One more use of a Voyeur's peek action this stream (shared) | 2 |
| Shop Control | Your first recruit in the next shop costs 1 Clout less. The shop tile shows the old price struck through. | 5 |
| Saboteurs | Multiplayer: each rival's next recruit costs 1 Clout more | 5 |

## 4. Card changes

- **Stat changes:**
  - Producer: 1 Clout / −1 Cash
  - Chat Admin: 1 / −1
  - Junior PR Manager: 2 / −1
  - Paid Sponsorship: −1 / 3
  - Chat Arbitrator: 0 / −1
  - Executive PR: 1 / −1
  - Platform Owner: 0 / 3
  - Cybersecurity Engineer: 3 / −1
  - Marketing Manager: 4 / −1
  - PR Manager: 3 / −1
  - Social Media Manager: 3 / −1
  - Senior Marketing Manager: 4 / −1
  - Clippo The Clanker: −2 / −1
  - Plus 1 Cash: Generous Donator 1 / 1, Die Hard Fan 2 / 1, Big Boomer 0 / 1
- **Junior PR Manager:** sends 1 random viewer back to the queue, and each use sends one more (2, 3…). This is counted per card and kept for good.
- **PR Manager:** sends 5 random viewers back to the queue.
- **Distraught Cat:** gains +1 Clout or +1 Cash (random) each time someone leaves.

## 5. Character sets

There are five presets now; the old sets 7–9 are gone. Each is a Party House scenario, cast with the new versions of its guests.

| Set | Party House scenario | Plays |
|---|---|---|
| Going Live | Alien Invitation | Precocious Youth (Alien) |
| Clickbait Roulette | High or Low | Mainstream Celebrity, Bigger Star |
| Brand Deal Wishlist | Best Wishes | Evangelist, Big Boomer |
| Monetization Meeting | Money Management | Clippo The Clanker, Senior Marketing Manager |
| Algorithm Magic | A Magical Night | Platform Owner, Internet Starpower |

Party House guests this game doesn't have were swapped for the closest card:

| Party House guest | Swapped for |
|---|---|
| Private Eye | Cybersecurity Engineer |
| Dancer, Cheerleader | Binge Bunny |
| Magician | Distraught Cat |

The sets are in `ScenarioCatalog.cs`, and their text is under `scenario.0-4.*` in the strings CSV.

## 6. Calls I made where the sheet left room (please check)

1. **Gatekeepers, Marketers, Voyeurs:** the sheet says "+1 eviction / peek per point" and gives Marketers no rule. I read all three as extra uses of that community's action, and gave Marketers the same rule for recruits.
2. **PR Manager:** its removal counts as an eviction for Gatekeepers.
3. **Bot Detector:** it gets more uses rather than removing more neighbours at once.
4. **Gen-Z:** "per score" is read as once per point. Growth lasts for good.
5. **Mods, Shop Control and Boomers:** these roll or apply only when a stream is actually scored.
6. **Saboteurs:** the surcharge stays until the rival's next recruit, even if they skip a shop.
7. **No cards yet:** Shop Control and Saboteurs have no member cards. Tick them on cards in the Game Database to use them.
8. **Staff Cash costs:** every Staff card now earns −1 Cash. Cards whose Cash went down gained that much Clout. Clippo The Clanker went the other way (−3 to −1 Cash), so it lost 2 Clout to keep its value. To keep the economy level, Generous Donator, Die Hard Fan and Big Boomer each earn +1 Cash.
9. **Not updated:** `GameReference.md` and its CSVs still describe the previous rules.
