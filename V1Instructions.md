# Deck Party V1: adding character sprites and backgrounds, and testing the update

This covers the streaming/meme update from `MajorUpdate.md`: the renames, the 40 new characters, the new character sets and the image-based backgrounds. Animations aren't part of V1.

Section 6 covers making a build you can send to a friend, and testing multiplayer together over the internet.

> **Updated for V2 (Audience Capture).** Sections 2, 3 and 6 use the current labels. Sections 0, 1 and 4 describe the V1 update as it was, so a few names there have changed since:
>
> | V1 name | Now |
> |---|---|
> | guest | viewer |
> | Squad | Audience |
> | Party Log | Stream Log |
> | venue | stream setup |
> | Open the Door | Add Viewer to Chat |
> | End Party | End the Stream |
> | Recruit Guests | Recruit Viewers |
> | Admit / Reject | Approve / Deny |
>
> On-screen text now lives in the strings CSV (see `UIGuide.md` section 7), not in `Terms.cs`. Streamers, languages, animation and the new test passes are in `V2Instructions.md`.

---

## 0. Get the update into Unity (about 2 minutes)

1. Click back into Unity and let it finish compiling.
2. Run **Deck Party > Setup > Build Everything (keeps existing scenes)**. It's safe and doesn't touch your scenes. It:
   - updates the Game Database to the new 59-character roster. Any portrait or rename that belonged to a removed character is cleared, and the Console says so.
   - renames the 7 old venue assets to the new backgrounds, keeping any images already assigned (for example `Venue_apartment` becomes `Venue_bedroom`).
3. You don't need to rebuild your scenes. The renamed captions (HUD, shop, Squad, menus) are applied when the game runs, and the new Confirm button and choice popup are created automatically.
   If you haven't customised the scenes yet, **Deck Party > Setup > Rebuild Scenes and UI Prefabs (overwrites)** also bakes the new names into the scene files.
4. Optional: **Deck Party > Setup > Regenerate Placeholder Art** turns the placeholder star icon into a diamond.
5. Old saves and progress don't carry over, because the rules changed. The **Continue** button disappears until you start a new season.
6. Multiplayer: everyone needs the new build. Old builds are rejected with a version message. Section 6 explains how to make and share one.

---

## 1. What got renamed

| Old | New |
|---|---|
| Pop (what guests earn at a party) | **Subs** |
| Pop you bank and spend on guests | **Gifted Subs** |
| Cash | Cash (unchanged) |
| Expand House / house size | **Raise Sub Cap** / **Sub Cap** |
| Trouble | **Swatting Risk** |
| Police raid | **SWATTED!** (the SWAT team shows up) |
| Taking the blame (sitting out the next party) | **Temporary Ban** |
| Stars | **Diamonds** (4 at one party to win) |
| Rolodex | **Squad** |
| Fire marshal | **Sub Cap exceeded** |
| Scenarios | **Character Sets** |

Renamed characters: Old Friend → **Loyal Fan**, Wild Buddy → **Bot Farm**, Rich Pal → **Whale**, Cute Dog → **VIPS**, Hippy → **AutoMod**, Security → **Moderator** ("Remove From Chat"), Rock Star → **Collaborator**, Driver → **Social Media Manager** ("Shoutout"), Grillmaster → **HODL** ("Reset the Stream"), Mr. Popular → **Parasocialite**.

The 9 Star characters are now Diamonds: Dinosaur → **Drama Channel**, Dragon → **Diva Streamer**, Mermaid → **Raid Leader**, Alien → **Mystery VTuber**, Unicorn → **Wholesome Icon**, Ghost → **Platform Admin** ("Ban Wave"), Superhero → **Mega Influencer**, Leprechaun → **Brand Sponsor**, Genie → **The Algorithm** ("Trending").

On-screen wording now lives in `Assets/DeckParty/Resources/Localization/AudienceCapture_Strings.csv`, one column per language. See `UIGuide.md` section 7.

---

## 2. Adding character sprites

**Folder:** `Assets/DeckParty/Art/Characters`

**Recommended art:** square-ish PNGs with transparent backgrounds, 512x512 (256x256 minimum). The portrait area in a card is about 180x178 and keeps your aspect ratio. Streamer portraits go in the same folder (or `Art/Streamers`) named `streamer_<name>`, e.g. `streamer_bro_jogan.png` (see `V2Instructions.md` section 2).

### Option A (easiest): one image per character
1. Name each file after the character using the **File name** column below. Case, spaces, `-` and `_` don't matter, so `this_is_fine_dog.png`, `ThisIsFineDog.png` and `This Is Fine Dog.png` all work.
2. Drag the files into `Assets/DeckParty/Art/Characters`.
3. That's it. On first import each file is set up as a Sprite, then automatically assigned to its character. The Console prints `Art auto-assign: Assigned N guest portrait(s)...`.

### Option B: one sprite sheet
1. Drag the sheet into `Art/Characters`.
2. In the Inspector set **Sprite Mode: Multiple**, then click **Apply**.
3. Click **Sprite Editor**, then **Slice** (Grid By Cell Size or Automatic), then **Apply**.
4. In the Sprite Editor, rename each slice using the File name column (select a slice and edit *Name*), then **Apply**.
5. Applying reimports the sheet and the auto-assign runs again. You can also run **Deck Party > Art > Auto-Assign Sprites by Name** yourself; it shows a dialog of what matched.

### Option C: a sheet laid out in roster order
Lay the characters out left to right, top to bottom, in the **#** order below. Slice the sheet as in Option B (names don't matter), select the texture, then run **Deck Party > Art > Assign Selected Sheet to Guests in Roster Order**.

### Option D: by hand
Use **Deck Party > Select Game Database**. In the **Guests** list, drag a sprite into each character's **Portrait**. To rename a character on screen, edit its `char.<Type>.name` row in the strings CSV.

**Pixel art:** after importing, set **Filter Mode: Point (no filter)** and **Compression: None**, then click **Apply**. Your changes are kept; the importer only sets defaults on the first import.

**Check it worked:** in Play mode, go to **How to Play > All 59 Viewers**, or **Single Player > Open Lobby**, which lists every character. **Deck Party > Art > Log Expected File Names** prints this list in the Console.

| # | Character | File name | Family | | # | Character | File name | Family |
|---|---|---|---|---|---|---|---|---|
| 1 | Loyal Fan | `loyal_fan` | Community | | 31 | Overly Attached Girlfriend | `overly_attached_girlfriend` (or `oag`) | Reaction |
| 2 | Whale | `whale` | Community | | 32 | Disaster Girl | `disaster_girl` | Reaction |
| 3 | Bot Farm | `bot_farm` | Community | | 33 | Hide the Pain Harold | `hide_the_pain_harold` (or `harold`) | Reaction |
| 4 | Social Media Manager | `social_media_manager` (or `smm`) | Community | | 34 | Success Kid | `success_kid` | Reaction |
| 5 | Moderator | `moderator` | Community | | 35 | Bad Luck Brian | `bad_luck_brian` | Reaction |
| 6 | AutoMod | `automod` | Community | | 36 | Tung Tung Tung Sahur | `tung_tung_tung_sahur` (or `tungtung`) | Brainrot |
| 7 | Collaborator | `collaborator` | Community | | 37 | Tralalero Tralala | `tralalero_tralala` | Brainrot |
| 8 | HODL | `hodl` | Community | | 38 | Bombardiro Crocodilo | `bombardiro_crocodilo` | Brainrot |
| 9 | VIPS | `vips` | Community | | 39 | Ballerina Cappuccina | `ballerina_cappuccina` | Brainrot |
| 10 | Parasocialite | `parasocialite` | Community | | 40 | Skibidi Toilet | `skibidi_toilet` | Brainrot |
| 11 | Doge | `doge` | Animal | | 41 | Sigma Male | `sigma_male` | Wojak |
| 12 | Trollface | `trollface` | Rage Comic | | 42 | Karen | `karen` | Reaction |
| 13 | Pepe | `pepe` | Animal | | 43 | Rickroll | `rickroll` | Reaction |
| 14 | Gigachad | `gigachad` | Wojak | | 44 | Distracted Boyfriend | `distracted_boyfriend` | Reaction |
| 15 | This Is Fine Dog | `this_is_fine_dog` | Animal | | 45 | Drakeposting | `drakeposting` (or `drake`) | Reaction |
| 16 | Wojak | `wojak` | Wojak | | 46 | Confused Math Lady | `confused_math_lady` | Reaction |
| 17 | Grumpy Cat | `grumpy_cat` | Animal | | 47 | Side Eye Chloe | `side_eye_chloe` | Reaction |
| 18 | Nyan Cat | `nyan_cat` | Animal | | 48 | Roll Safe | `roll_safe` | Reaction |
| 19 | Stonks Guy | `stonks_guy` | Reaction | | 49 | Crying Cat | `crying_cat` | Animal |
| 20 | Cheems | `cheems` | Animal | | 50 | Dancing Baby | `dancing_baby` | Reaction |
| 21 | Chad vs Virgin | `chad_vs_virgin` (or `chad`) | Wojak | | 51 | Drama Channel (Diamond) | `drama_channel` | Diamond |
| 22 | Doomer | `doomer` | Wojak | | 52 | Diva Streamer (Diamond) | `diva_streamer` | Diamond |
| 23 | NPC Wojak | `npc_wojak` | Wojak | | 53 | Raid Leader (Diamond) | `raid_leader` | Diamond |
| 24 | Forever Alone | `forever_alone` | Rage Comic | | 54 | Mystery VTuber (Diamond) | `mystery_vtuber` | Diamond |
| 25 | Me Gusta | `me_gusta` | Rage Comic | | 55 | Wholesome Icon (Diamond) | `wholesome_icon` | Diamond |
| 26 | Keyboard Cat | `keyboard_cat` | Animal | | 56 | Platform Admin (Diamond) | `platform_admin` | Diamond |
| 27 | Bongo Cat | `bongo_cat` | Animal | | 57 | Mega Influencer (Diamond) | `mega_influencer` | Diamond |
| 28 | Pop Cat | `pop_cat` | Animal | | 58 | Brand Sponsor (Diamond) | `brand_sponsor` | Diamond |
| 29 | Big Chungus | `big_chungus` (or `chungus`) | Animal | | 59 | The Algorithm (Diamond) | `the_algorithm` (or `algorithm`) | Diamond |
| 30 | Ugandan Knuckles | `ugandan_knuckles` (or `knuckles`) | Animal | | | | | |

Until a character has a portrait, its card shows a coloured disc with initials. The colour is its family: blue Community, orange Animal, teal Wojak, pink Rage Comic, purple Reaction, green Brainrot, gold Diamond.

---

## 3. Adding background images

**Folder:** `Assets/DeckParty/Art/Backgrounds`

**Recommended art:** 21:9, 2520x1080 or larger, PNG or JPG. Backgrounds fill the whole screen while keeping their aspect ratio: ultrawide screens show the full width and 16:9 crops the sides. Keep the important parts inside the central 16:9. Each streamer also has a stage (`bg_stage_<streamer>.png`, e.g. `bg_stage_fate.png`); see `V2Instructions.md` section 2.

1. Name each image `bg_<id>.png` using the table below. Inside the Backgrounds folder the plain id also works (`bedroom.png`).
2. Drag the images into `Assets/DeckParty/Art/Backgrounds`.
3. They're imported as sprites and assigned to their venue automatically. The Console confirms with `... N background image(s)`.

| Background | id | File name | Default for character set |
|---|---|---|---|
| Bedroom Setup | `bedroom` | `bg_bedroom.png` | Starter Stream |
| Content House | `content_house` | `bg_content_house.png` | Cat Cafe |
| Cursed 3AM Stream | `cursed_stream` | `bg_cursed_stream.png` | Doomscroll |
| Studio Stage | `studio_stage` | `bg_studio_stage.png` | Italian Brainrot |
| Neon City IRL | `city_irl` | `bg_city_irl.png` | Reaction Channel |
| Metaverse Lounge | `metaverse` | `bg_metaverse.png` | Random Mix (Random also rolls any venue) |
| IRL Beach Stream | `irl_beach` | `bg_irl_beach.png` | Open Lobby |

**Optional extra layers**, using the same naming scheme:
- `bg_<id>_fg.png`: a transparent overlay drawn on top of the background, such as a desk edge, frame or vignette.
- `bg_<id>_thumb.png`: a small preview (about 320x180) for the venue selectors. Without one, the background is used.

**By hand:** open `Assets/DeckParty/Data/Venues/Venue_<id>.asset` and drag your image into **Background** (plus Foreground or Thumbnail if you have them).

**Other venue settings:**
- **Party Lights:** soft moving spotlights over your image, which also flash red and blue when you get swatted. Untick it if you want the image completely clean.
- **Music** (optional): this venue's party track.
- The coloured placeholder scene is only drawn while **Background** is empty.

**New background:** right-click in `Data/Venues`, choose **Create > Deck Party > Venue**, give it a unique **Id**, then add it to the database's **Venues** list. Name the image `bg_<that id>`.

**Check it worked:** **Single Player** shows the selected stream setup's preview in the bottom bar. In game, open **Menu > Stream Setup** to switch backgrounds live.

---

## 4. Testing the update

**Shortcuts for testing:**
- **F9** (editor and development builds, single player only): +50 Gifted Subs and +10 $. Press it during planning to recruit anything.
- **Single Player > Open Lobby:** every character is in the shop. With F9 you can test any of them within a couple of nights.
- **Hover any card** to see its family, its action type and tonight's effects: Infected, Immune, Attached, Copied, Rickrolled, Zeroed, Double, Converted, strikes and permanent changes.
- The **Party Log** on the right narrates every effect, including "Rickrolled into...", "takes strike 2", "catches the Skibidi infection" and "This is fine."

### A. Renames
- HUD: **GIFTED SUBS**, **CASH**, **SUB CAP** (used/cap during a party), a **Swatting Risk x/3** meter, a **Diamonds x/4** meter and a **Squad [R]** button.
- Cards say `+N Subs`. The shop is titled **Recruit Guests** and prices are in Gifted Subs. Planning shows **Raise Sub Cap** and **View Squad**.
- **Getting swatted:** let 3 Bot Farms in (Starter Stream, night 1). You should see the **SWATTED!** banner with red and blue lights, then the Temporary Ban screen. The banned guest appears in the planning result, is dimmed in the Squad view the next night, and the tooltip says "Temporary Ban".
- **Sub Cap exceeded:** with 1 space left, let in a Parasocialite (or Big Chungus with fewer than 3 spaces left). You should see the **SUB CAP EXCEEDED!** banner.

### B. Character sets
Single Player lists **Starter Stream, Cat Cafe, Doomscroll, Italian Brainrot, Reaction Channel, Random Mix, Open Lobby**. Selecting one shows its guests. Random Mix rolls 11 guests and 3 Diamonds per season.

### C. Winning with Diamonds
Use F9, buy 4 Diamonds in Open Lobby and play until all 4 are at one party. **End Party & WIN!** flashes, then the VICTORY screen appears.

### D. The new characters (what you should see)

| Character | How to test |
|---|---|
| Doge | Mix families at the party; his Subs go up by 1 per family (max +5). |
| Trollface | Action sends a guest back outside, and they can come in again tonight. Trolling the same guest again (even another night) pays more. |
| Pepe | The action opens a Happy / Sad / Angry popup. Angry adds Swatting Risk. |
| Gigachad | +3 if he's the only guest with 5+ printed Subs. A second Gigachad makes both -2. |
| This Is Fine Dog | Hit 3 Swatting Risk with him at the party. The banner says THIS IS FINE, the risk is cleared, and he leaves your Squad for good. |
| Wojak | Gains +2 per suffering guest (with Swatting Risk, infected, zeroed, negative stats or penalised), max +6. |
| Grumpy Cat | The action shows the next 3 in line. Pick one to reject, then pick who comes next. |
| Nyan Cat | Each risk-free arrival after her pays +1, +2, +3... into tonight's take. A Swatting Risk arrival resets the chain. |
| Stonks Guy | Invest popup (2 Gifted Subs or 1 $). The money leaves your bank now; the End Party total includes 3x back. |
| Cheems | With a Doge: shield badge (cancels 1 risk), and both get +2. Without one: -1 per risk. |
| Chad vs Virgin | Pick a guest. The log shows the permanent change, and the card gets a +1/-1 badge. |
| Doomer | +2 per Swatting Risk; +2 $ at exactly 2. |
| NPC Wojak | Copies the printed stats of the guest before him (log: "copies ..."). |
| Forever Alone | Alone at the end: +6. Otherwise +1 per empty space. |
| Me Gusta | +2 Subs whenever someone adds Swatting Risk; +$ when risk is removed. |
| Keyboard Cat | Evict a guest: +2 Subs if they had risk, otherwise +1 $. |
| Bongo Cat | Every 3rd arrival after her gives all Animals +2. |
| Pop Cat | +1 per action anyone uses; every 3rd action adds +1 $. |
| Big Chungus | Fills 3 card slots in the party grid; bonus from a Sub Cap above 10. |
| Ugandan Knuckles | Arrival adds a temporary copy to the line (it vanishes after the party). +1 per other Knuckles. |
| Overly Attached Girlfriend | Attaches to the best guest (tooltip: "Attached"). That guest's Subs double and they can't be targeted for removal. |
| Disaster Girl | Target gets +1 risk and +3 Subs. The button is disabled if it would bring the SWAT team. |
| Hide the Pain Harold | Penalties (Gigachad, Sigma, Cheems, a Drakeposting zero) can't drop guests below printed Subs; he gains +1 per block. |
| Success Kid | Popup: **Reroll** swaps the last random arrival for a new one; **Peek** shows the door panel. |
| Bad Luck Brian | The first removal or risk-clear on him each party fails (log message), and he gains a permanent +1. |
| Tung Tung Tung Sahur | Strike the same guest three times (once per party per Tung Tung; strikes are remembered between nights): +2 Subs, then their risk is cleared, then evicted. The tooltip shows the strike count. |
| Tralalero Tralala | Pick two guests; they swap places (NPC Wojak re-copies). |
| Bombardiro Crocodilo | Click up to 3 guests, then **Confirm** (or Space). Each is replaced by a random waiting guest. |
| Ballerina Cappuccina | Pick two guests; they swap printed Subs for the night. |
| Skibidi Toilet | Infects a guest on arrival (green frame), and each new arrival spreads it. Every infection adds Swatting Risk, so end early! |
| Sigma Male | +1 per empty space (max +8); -1 per other Sigma. |
| Karen | Remove a guest; half their cost (max 8) goes into tonight's take. |
| Rickroll | A guest becomes another character from the waiting line (pink name with "?"). It can use the new action. |
| Distracted Boyfriend | On arrival a picker reveals 3 waiting guests. Pick one, then click who leaves, or **Skip**. |
| Drakeposting | Popup (double Subs or $), then pick the zeroed guest, then the doubled guest. |
| Confused Math Lady | Popup (Subs to $ or $ to Subs), then pick a guest. Their total converts at End Party. |
| Side Eye Chloe | While she's here, the door button shows **Next up: ...**. The action sends that guest to the back. |
| Roll Safe | The target gets a teal frame and can no longer be picked by anyone's actions tonight. |
| Crying Cat | +2 Subs and +1 $ whenever someone else leaves. |
| Dancing Baby | Use two actions of the same type in a row (e.g. two Remove actions): +3 each time, max +9. The action type is in each tooltip. |

### E. Multiplayer
Use **Window > Multiplayer > Multiplayer Play Mode** with Player 2, host under Direct IP / LAN, then join `127.0.0.1:7777`. The choice popups, multi-picks and Distracted Boyfriend decisions all go through the host, and each player sees only their own decisions.

The lobby defaults to **Take Turns**: both windows show the active player's party, and the turn passes when they click **End Turn**. Click a player in the Players panel to watch their board in any mode (see section 6D).

To test with a friend over the internet, see section 6.

---

## 5. Interpretations I made (easy to change)

- **"Socialite"** isn't in the original roster. I treated it as **Mr. Popular** (the guest who brings a friend) and spelled it **Parasocialite**.
- **Subs vs Gifted Subs:** guests earn *Subs* during a party. When it ends they're banked as *Gifted Subs*, which you spend on characters.
- **Fire marshal** became **Sub Cap exceeded**, to match "buying a larger subscriber cap".
- **Rolodex** became **Squad**. The renamed **Character Sets** replace the old scenario presets.
- **Overly Attached Girlfriend** picks her partner automatically (highest printed Subs; if she arrives first, she waits for the next arrival). Her partner can't be targeted by removal actions; if the partner is forced out (e.g. Reset the Stream), she leaves too.
- **Skibidi Toilet** spreads to one random uninfected guest per new arrival.
- **Ugandan Knuckles** copies don't make more copies (otherwise the line never ends).
- **Trollface** pays +2 Subs for every time that guest has been trolled, including this time. The count is remembered between parties.
- **Success Kid**'s "random outcome" is the most recent guest who arrived at random (door, brought along or bombarded in).
- **Rickroll** never turns anyone into Big Chungus, because that would instantly break your Sub Cap. Transformations end when the guest leaves or the party ends.
- **Hide the Pain Harold** also blocks Drakeposting's zero on Subs (not on $).
- **Nyan Cat** and **Karen** pay into tonight's take, so their Subs survive even if Nyan or Karen leaves. "Gains" effects (Me Gusta, Crying Cat and others) go on the guest's own card.
- **Tung Tung**'s counter resets after the third strike.
- **Stonks Guy** invests from your bank; it's lost if the party is swatted or shut down.
- **Grumpy Cat** needs at least 2 guests waiting; **Side Eye Chloe**'s action needs 2 as well.
- The **door** now admits the next guest in a pre-shuffled line rather than a fresh random pick each time. It's equally random, and it's what lets Grumpy Cat, Side Eye Chloe and Success Kid see "the next guest".

Rules tuning numbers live in `Assets/DeckParty/Scripts/Core/GameRules.cs`, character stats and text in `GuestCatalog.cs`, and character sets in `ScenarioCatalog.cs`.

---

## 6. Sharing a build with a friend and testing multiplayer online

This takes you from Unity to a copy of the game your friend can run, then through a test session for the two of you. It assumes you're both on Windows. If your friend uses a Mac, that needs a separate macOS build and a few extra steps on their side.

Online play uses **Unity Relay**: no port forwarding, your friend doesn't need Unity or an account, and you share a 6-character join code. Your PC (the host) runs the game for both of you.

### A. Adding Relay to the project (one-time, before your first build)

You've already turned Relay on in your Unity account. What's left is making sure this Unity project is linked to the **same** Unity Cloud project, because the game requests Relay servers using that project's ID. Each build stores the ID, so do this **before** building. A copy built without it can't go online and shows "Online play isn't set up yet...".

The code side is already done. The project includes the **Multiplayer Services** package, which contains Relay, and Deck Party turns online play on automatically when that package is present. You don't need to add any components, scripts or settings objects.

**Step 1: check the package is installed**
1. Open **Window > Package Manager** and choose **In Project** on the left.
2. Find **Multiplayer Services** and check it shows **2.4.1**. Netcode for GameObjects (2.13.3) and Unity Transport (2.7.4) should also be listed. Don't upgrade any of them, because newer versions need a newer Unity.
3. If Multiplayer Services is missing, click **+ > Install package by name...**, enter `com.unity.services.multiplayer` as the name and `2.4.1` as the version, then click **Install**.

**Step 2: sign in to the editor**
The account button at the top left of the Unity editor should show the account you used to turn on Relay. If it says **Sign in**, sign in with that account.

**Step 3: find the project Relay is turned on for**
1. Go to https://cloud.unity.com and open the project where you turned on Relay.
2. Note its **project ID**, a long code like `a19a0635-1551-4d61-a0c6-c7df3bf1e25d`. It appears in the browser's address bar after `/projects/`, and on the project's settings page.
3. Open **Relay** in that project's left-hand menu (under Multiplayer). It should show the Relay overview. If you see a **Get started** button instead, Relay isn't on for this project yet: click it and follow the prompts.

**Step 4: link the Unity project to it**
1. In Unity, open **Edit > Project Settings > Services**.
2. This project is currently linked to project ID `a19a0635-1551-4d61-a0c6-c7df3bf1e25d` (project **DeckParty**, organisation `overlord1987`). Compare that with the ID from step 3:
   - **Same ID:** nothing to change. Go to step 5.
   - **Different ID**, because you turned Relay on in a different project: the simplest fix is to turn Relay on for the DeckParty project as well (step 3, on that project). Alternatively, click **Unlink project** here and link the other project as described in the next point.
   - **Not linked** (the page asks you to link a project): choose your **Organization**, choose the option to use an **existing** Unity project ID, pick the project from step 3 and click **Link project ID**.
3. Under **Services > Environments**, leave the environment as **production**, the default. The game doesn't set an environment of its own.

**Step 5: check it works in the editor**
1. Open the MainMenu scene, press **Play** and click **Multiplayer**.
2. Pick any streamer. Under **Online (Join Code)**, the note should say "Play over the internet with a join code (Unity Relay)..." and **Host Online Game** should be clickable. An orange "Online play needs the Multiplayer Services package" message means step 1 isn't done.
3. Click **Host Online Game**. After a few seconds the lobby opens with a 6-character join code, which means this project can reach Relay.
4. To check joining as well: stop Play and open **Window > Multiplayer > Multiplayer Play Mode**. Tick **Player 2**, then press Play. Host online in the main window. In the Player 2 window, click **Multiplayer**, pick a streamer, paste the code into **Join code** and click **Join**. Player 2 should appear in the lobby's player list.

**If step 5 fails**

| What you see | Cause and fix |
|---|---|
| "Online play isn't set up yet..." | The project isn't linked, it's linked to a project without Relay, or the editor isn't signed in. Check steps 2 to 4. After turning on Relay or changing the link, wait a minute or two and try again. If it still fails, restart Unity. |
| "Couldn't reach Unity's servers..." | This PC can't reach Unity's servers. Check the internet connection, and turn off any VPN or firewall that might block it. |
| Any other message | The Console has a line starting `[DeckParty] Relay host failed:` with Unity's full error. Paste it to me. |

Once step 5 works, make the build (section B). If you ever link a different project later, rebuild and resend, because existing copies keep the old ID.

### B. Make the build

1. Run **Deck Party > Setup > Build Everything** once. It applies the build options the game needs: both scenes in the build list (MainMenu first), a 1600x900 resizable window, and **Run In Background**, so the host's game keeps running while minimised.
2. In **Edit > Project Settings > Player**, change **Version** every time you send a build (1.0, 1.1, 1.2...). It's shown in the corner of the main menu (for example `v1.1`), which is the easiest way to check you're both on the same copy.
3. Open **File > Build Profiles**, select **Windows** in the Platforms list and click **Switch Platform** if it isn't already active. Under **Scene List**, check that `MainMenu` comes first, then `Game`, both ticked.
4. For your test builds, tick **Development Build**. Errors then show on screen and in the log with more detail, and F9 works in single player. Untick it when you make a "finished" copy.
5. Click **Build**. Make a new empty folder outside `Assets`, for example `C:\Users\ssare\DeckParty\Builds\DeckParty-1.1`, and select it. The first build takes a few minutes; later ones are quicker.
6. The folder now contains `Deck Party.exe`, `Deck Party_Data`, `MonoBleedingEdge`, `D3D12`, `UnityPlayer.dll` and `UnityCrashHandler64.exe`. These are the game. It also contains `Deck Party_BurstDebugInformation_DoNotShip`, which is only for debugging crashes, so don't send that one.

If the build fails with an IL2CPP error, set **Player > Other Settings > Scripting Backend** to **Mono** and build again. Mono needs no extra installs.

**Check the build on your PC:** run `Deck Party.exe`, click **Multiplayer**, pick a streamer and click **Host Online Game**. Then press Play in the editor and join that code from there. If you both reach the lobby, the build is ready to share.

### C. Send it to your friend

1. Open the build folder and select everything **except** `Deck Party_BurstDebugInformation_DoNotShip`. Right-click and choose **Compress to > ZIP File**, then name it something like `DeckParty-1.1.zip`.
2. Upload the zip to Google Drive, OneDrive or Dropbox and send your friend the link (WeTransfer works too). Discord's free upload limit is too small for a game build.
3. Send them the steps below.

**Steps for your friend (copy and paste):**
> 1. Download the zip, right-click it and choose **Extract All**, then open the extracted folder. Don't run the game from inside the zip, because it can't find its files that way. If Google Drive says it can't scan the file for viruses, click **Download anyway** (it says that for every large file). If your browser warns about the download, choose **Keep**.
> 2. Double-click **Deck Party.exe**. Windows will probably say "Windows protected your PC" because the game isn't from a registered publisher. Click **More info**, then **Run anyway**.
> 3. If Windows Firewall asks about Deck Party, click **Allow**.
> 4. Click **Multiplayer** and pick a streamer on the **Select Your Streamer** screen (Enter or **GO LIVE!**). Type your name in **Your Name**, paste the join code I send you into **Join code**, and click **Join**.
> 5. In the lobby, click **Ready!**. You can change your streamer there, and pick your own background under **Your stream setup**; I choose everything else.
> 6. When I send a new version, delete the old folder and extract the new zip. The version number is in the corner of the main menu, and ours must match.

### D. Playing online

1. **You (host):** click **Multiplayer**, pick a streamer, type your name, then click **Host Online Game**. The lobby shows a 6-character join code. Click **Copy** and send it to your friend (Discord, WhatsApp, a text). You get a new code every time you host.
2. **Your friend** joins with the code and appears in the **Players** list.
3. You choose the **Character Set**, **Mode**, **Season length** and **Turn Timer**. Your friend sees your choices but can't change them. Each of you picks your own streamer and stream setup.
4. When your friend shows **Ready**, click **Start Season**.
5. Keep your game running while you play, because your PC runs both games. Minimising or alt-tabbing is fine. Closing the game, or **Menu > Leave Game**, ends the season for both of you. If your friend leaves, they can't rejoin that season.

Being on a voice call while you test makes it much easier to compare what each of you sees.

**Modes** (chosen in the lobby):
- **Take Turns** (the default): each night, players throw their parties one after another, in lobby order (the host first). While it's someone's turn, **everyone's screen shows that player's party live**: every arrival, action, banner and sound, plus their Party Log. When the player has finished their party and their shopping, they click **End Turn** and every screen switches to the next player. After the last player, the next night starts with the first player again. The season ends after the first night on which someone gets 4 Diamonds, once everyone has played that night.
- **Synchronized Nights:** everyone plays the same night at the same time, and the next night starts when everyone is ready.
- **Race:** everyone plays at their own pace, and the first 4-Diamond party wins instantly.

**Watching other players (every mode):** click any player in the **Players** panel on the right to see their party live. Their board is read-only: you can hover cards and open their Squad, but you can't click anything. The strip above the board says whose party is on screen and whose turn it is, and its button takes you back to your own party (or, in Take Turns, to the player whose turn it is). In Synchronized Nights you're put back on your own board automatically when your next night starts.

### E. A test session for the two of you (about 60 minutes)

Do the steps in order: the first ones check the connection, and the later ones check the parts most likely to break over a network.

| # | Do this | What should happen |
|---|---|---|
| 1 | You host online and your friend joins with the code. Each of you sends a chat message in the lobby. | Your friend appears in **Players (2/6)** as "Not ready", with a "joined the game" headline. Both messages appear on both screens. |
| 2 | You change the Character Set, Mode and Season length. | Your friend's lobby updates straight away, and their selectors are greyed out. |
| 3 | Pick **Starter Stream**, **Take Turns**, **10** nights. Your friend clicks **Ready!**, then you click **Start Season**. | Both screens show **your** board. You get a "YOUR TURN" banner; your friend gets "<your name>'S TURN". The strip above the board says whose turn it is. |
| 4 | Play your turn: open the door a few times, use an action, end the party, buy a guest, then click **End Turn**. | Your friend sees every arrival, action, banner and purchase live, and their Party Log narrates it. Their buttons stay greyed out. After End Turn, both screens switch to your friend's board and they get "YOUR TURN". |
| 5 | Your friend plays their turn while you try clicking the door and the cards. | You see everything, but nothing on your side is clickable. When they click **End Turn**, night 2 starts with your turn. (This is where the game used to freeze.) |
| 6 | During your friend's turn, click your own name in **Players**. Then click the button on the strip. | Your own board appears read-only ("Waiting for your turn"). The strip's button takes you back to your friend's live turn. |
| 7 | One of you lets in 3 Bot Farms on purpose. | SWATTED! appears on both screens. The swatted player chooses the Temporary Ban. The other sees "<name> must choose who gets a Temporary Ban." |
| 8 | Chat during a turn. | Messages appear for both of you. |
| 9 | Play on to the end of night 10. | You both see the final rankings, and your friend sees "Waiting for the host to start another round...". Click **Back to Lobby**, and you both return to the lobby. (If one season ends before the other, the scoreboard has a **Keep Watching** button.) |
| 10 | In the lobby, pick **Open Lobby** and **Synchronized Nights**, then start. Play night 1 at the same time, and click the other player's row in **Players**. | Their board appears with "Watching <name>'s party". **Back to My Party** returns to yours. When you're both ready, night 2 starts and you're both put back on your own boards. |
| 11 | Your friend buys characters with special choices: Pepe (8), Karen (8), Grumpy Cat (9) and Distracted Boyfriend (9), then Tralalero Tralala (11) and Bombardiro Crocodilo (15) once they can afford them. You watch their board while they use them. | Popups, target picks, **Confirm** and Distracted Boyfriend's picker appear only on your friend's screen, and behave exactly as in single player (section 4D). You see the results live: log lines and card changes. |
| 12 | Leave, host again with **Race (own pace)**, and play a couple of nights at different speeds. | Neither of you waits for the other between nights. |
| 13 | You click **Menu > Leave Game**. | Your friend is sent back to the Multiplayer screen with "The host closed the lobby." |
| 14 | Host again (new code). Your friend joins and you start. Your friend clicks **Menu > Leave Game**, then tries to join again with the same code. | You see "left the game" and can keep playing (in Take Turns, their turns are skipped). Your friend gets "That game has already started." |

### F. If something goes wrong

| Message or problem | What to do |
|---|---|
| "Online play isn't set up yet..." | Relay isn't enabled, or the build was made before the project was linked. Do section A, then rebuild and resend. |
| "Couldn't reach Unity's servers..." | That PC is offline, or a VPN, school or work network is blocking the connection. Try another network, or use the Tailscale fallback below. |
| "No game found with that code." | There's a typo, or the host left and hosted again (each hosting gets a new code). Ask for the current code. |
| "Version mismatch..." | You're running different builds. Both of you should use the newest zip. |
| "That game has already started." | You can only join while the host is in the lobby. |
| The game won't start, or says it can't find `Deck Party_Data` | It was run from inside the zip. Use **Extract All** first. |
| Antivirus removes `Deck Party.exe` | This sometimes happens with unsigned indie games. Restore the file and allow it in the antivirus. |
| Something breaks during a game | Send me what you were doing, who was hosting, and the log files from **both** PCs (see below). |

**Log files:** press **Win+R**, paste `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Deck Party` and press Enter. `Player.log` is the latest run and `Player-prev.log` the one before. The log is replaced every time the game starts, so copy it before you relaunch. (If you set a **Company Name** in Player Settings, the `DefaultCompany` part of the folder name changes to match.)

**Fallback if Relay won't connect: Tailscale (free)**
1. You both install Tailscale from https://tailscale.com and sign in. From the Tailscale admin console, share your PC with your friend, or invite them to your network.
2. You click **Host** (under Direct IP / LAN). Your Tailscale address (it starts with `100.`) is shown in the Tailscale app. The address the game shows is your home network address, so don't send that one.
3. Your friend types your `100.x.x.x` address and port `7777` under **Direct IP / LAN**, then clicks **Join by IP**.
4. If Windows Firewall asks on your PC, allow Deck Party on both private and public networks.

Port forwarding also works without any accounts; see section 11e of `INSTRUCTIONS.md`.

### G. Sending an update

1. Change **Version**, build into a new folder, then zip and send it as in B and C.
2. Before hosting, check that the main-menu version matches on both PCs.
3. The game only turns away a copy with a different network version, which I change whenever the rules or save format change. Smaller changes can still connect, so the matching menu version is what keeps you both on the same rules. If you edit the rules code yourself, change `ProtocolVersion` in `Assets/DeckParty/Scripts/Runtime/Networking/NetworkService.cs` (it's currently `deckparty-4`; change it to `deckparty-5`, and so on) so older copies get the "Version mismatch" message.
