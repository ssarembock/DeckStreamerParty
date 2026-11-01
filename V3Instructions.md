# Audience Capture V3: where to put your sprites

## 1. Get the update into Unity (do this first)

1. Open the project and let it compile.
2. Run **Deck Party > Setup > Rebuild Scenes and UI Prefabs (overwrites)**. This rebuilds the Game and Main Menu scenes and makes the new shop tile prefab. It also adds the new art slots to the Game Database and fixes the streamer-card tilt effect.
3. Open **Deck Party > Select Game Database**. The new section at the top is **Stream Art**. Everything below goes there unless it says otherwise.

Your existing viewer portraits stay on the right characters, including the renamed ones. The 23 new viewers start with placeholders.

## 2. Importing a sprite or sheet (same for everything below)

- Put files anywhere under `Assets/DeckParty/Art/UI/`, for example `Art/UI/Chat`, `Art/UI/Emojis`, `Art/UI/Emotes`, `Art/UI/Endings`, `Art/UI/Groups`.
- Select the image. In the Inspector, set **Texture Type** to **Sprite (2D and UI)** and click **Apply**.
- **Single image:** leave **Sprite Mode** on **Single**.
- **Sheet:** set **Sprite Mode** to **Multiple**, click **Apply**, then **Sprite Editor > Slice > Grid By Cell Size** (or **Automatic**) > **Slice** > **Apply**.
- **To fill a list from a sheet:**
  1. Click the Game Database so it stays in the Inspector (or lock the Inspector with the padlock).
  2. In the Project window, expand the sheet with its arrow and shift-click every slice.
  3. Drag them onto the list's name.

## 3. Where each sprite goes

| Sprite | Where | Notes |
|---|---|---|
| **Chat bubble** (the big square behind the viewers) | Stream Art > **Chat Bubble** | Must be **9-sliced**. In the Sprite Editor, drag the green border handles (or type **Border** L/T/R/B) so the corners and edge line sit outside the box. Click **Apply**. |
| **Bubble tail** (pointing down from the computer into the bubble) | Stream Art > **Chat Bubble Tail** | Draw it pointing **down**. It sits on the bubble's top edge and reaches up into the strip above. Its bottom overlaps the bubble by 10 px so the two join. |
| Bubble colour | Stream Art > **Chat Bubble Tint** | White keeps your art's own colours. |
| **Where the tail points** (lined up with the computer) | `Assets/DeckParty/Data/Venues/Venue_<setup>.asset` > **Chat Tail Position** | 0 = left edge of the bubble, 1 = right edge. Set it per stream setup. Each streamer has their own setup (`Venue_stage_<streamer>`). |
| Header image above the bubble (optional) | Same Venue asset > **Header** | Shown in the 190 px strip above the bubble, so the scene with the computer is always visible. Leave it empty to let the background show through. |
| **Viewer emojis**: peaceful sheet | Stream Art > Viewer Emojis > **Peaceful** | Calm streams send mostly these. |
| **Viewer emojis**: slightly argumentative sheet | Stream Art > Viewer Emojis > **Argumentative** | Show up once there's some Platform Ban Risk. |
| **Viewer emojis**: very angry sheet | Stream Art > Viewer Emojis > **Angry** | Take over one risk from a ban. Risky viewers lean angrier. |
| Emoji timing and size | Stream Art > Viewer Emojis > **Calm / Tense / Heated Interval**, **Size**, **Rise**, **Lifetime** | Until you add sheets, small text emojis (`:)`, `?!`, `#@!`) stand in. |
| **Multiplayer emote buttons** | Stream Art > **Emotes** | One slice per button, in button order (up to 16). Pressing one pops it up from your avatar in the Players panel on every screen. Until then, text emotes show. |
| **Platform Ban image** (shown when Platform Ban Risk goes over the limit) | Stream Art > **Platform Ban Image** | Shown full-screen with the caption underneath. Any size; keeps its shape (about 1200x640 fits best). |
| **Server Down image** (shown when chat goes over the Sub Cap) | Stream Art > **Server Down Image** | Same as above. |
| **Character group icons** (shop tiles) | Stream Art > **Group Icons** | One slot per group. Every audience-classic viewer and Diamond is **TBD** for now. Leave TBD empty and its tiles show no icon; give a group an icon to show it. |
| Viewer portraits (the 23 new ones) | Game Database > **Viewers** > each row's **Portrait** | Or name the files after the viewer (for example `crypto_guy.png`) and run **Deck Party > Art > Auto-Assign Sprites by Name**. **Log Expected File Names** lists them all. |

## 4. Worth knowing

- **Renames:**
  - Swatting Risk is now **Platform Ban Risk**, and the ending is a **Platform Ban**.
  - Going over the Sub Cap is now **Server Down**.
  - This applies everywhere: all 5 languages, the code and the docs.
- **Viewers:**
  - The table's viewers are in. 19 existing viewers were renamed or updated and 23 are new.
  - Your table's descriptions were followed. Where it left a stat out, I kept the existing value or took the original Party House one.
  - Three new character sets use the new viewers: **Talent Agency**, **Damage Control** and **Money Talks**. They are also in Random Mix and Open Lobby.
  - I picked these names myself; change them in `Assets/DeckParty/Resources/Localization/AudienceCapture_Strings.csv`:
    - **Influential Troll**: your table says "Influencal Troll".
    - **Binge Bunny**: your table says "Rabbit Themed Name".
    - **Sneak Peek**: the Social Media Manager's action, which had no name in the table.
- **Shop:**
  - The shop is now a grid of tiles showing name, picture, group icon, price and copies left. Hover a tile for the rest. Click a tile to buy.
  - The shop's stock is **shared by every player**: 4 copies of each non-Diamond viewer per match. Change this with `GameRules.SharedStockPerGuest`.
- **Streamer actives** cost **Cash** only, and every use costs 1 more, up to 12, like Raise Sub Cap. Cooldowns are unchanged.
- **Activity log:** off by default. Switch it on in **Settings > Show Activity Log** (main menu or in-game menu).
- **Text chat:** hidden, not deleted. To bring it back, open **Deck Party > FX > Select FX Library** and tick **Text Chat**.
- **Fake chat:** removed completely.

## 5. Quick check

1. **Single player, Talent Agency:**
   - The viewers sit in the chat bubble and the tail points up.
   - Emojis float up, and get angrier as risk rises.
   - Going over the limit shows your Platform Ban image (or the PLATFORM BAN! banner).
2. **Shop:**
   - A grid of big tiles.
   - Hovering a tile shows the details and your owned / left counts.
   - The round summary is gone.
   - Using an active costs Cash and the price goes up by 1.
3. **Two players, Take Turns:**
   - Use an active, then check that no streamer card stays tilted.
   - Buy a viewer, then check that the other player's "left" count drops.
   - Each player's Audience and owned counts are their own.
   - Emotes pop up from the sender's avatar on both screens.
   - The other player can still watch your turn live.
