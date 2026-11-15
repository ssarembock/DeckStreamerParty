# Shop look and your own shop card prefab

How to restyle the shop with **UI Skin > Shop**, and how to set up your own viewer card prefab.

Run **Deck Party > Setup > Rebuild Scenes and UI Prefabs** once first. It builds the new shop toolbar (prices above
the buttons) and connects the buttons to the UI Skin.

---

## 1. Where the settings are

Select the UI Skin (**Deck Party > Select UI Skin**) and open **Shop (buttons, prices, viewer cards)**.

The section has three parts:

- **Toolbar buttons:** one block per button (Streamer active, Raise Sub Cap, Remove a Viewer, Go Live). The contents of each button are centred together. The Streamer active button, at the left end, shows the streamer's portrait and the active's icon, with no text by default.
- **Prices above the buttons.**
- **Viewer cards:** your own card prefab, the card sizes, the built-in card's shape and piles, and the card colours.

Anything left empty, at 0 (or -1 where noted) or unticked keeps the built-in look. To undo a change, clear it.

Colour settings are a tick plus a colour box. Picking a colour ticks the box. Unticked means the game's own colour
applies.

---

## 2. Toolbar buttons

Each button block has a **Find** button that selects the button in the open Game scene. The shop panel (`Planning`) is
switched off in the scene. Switch it on to see the buttons while you work, and off again before you save.

| Setting | What it does |
|---|---|
| **Icon > Art** | Replaces the icon on this button only. Empty uses the icon entry further down the UI Skin. That entry may be shared with other places in the game. |
| **Icon > Colour** | Tints the icon. Unticked, your own art keeps its colours. |
| **Icon > Size** | The icon's width and height in pixels. |
| **Text > Shows** | **Built In:** the game's numbers (+1 on Raise Sub Cap, -1 on Remove a Viewer; nothing on the Streamer active and Go Live). **Words:** a short word in the player's language ("Use", "Sub Cap +1", "Remove", "Go Live"). **Custom:** your own text, the same in every language. **None:** only the icon, centred. |
| **Text > Colour / Font size** | The button's text. |
| **Shape > Background art** | The button's shape. Use a 9-slice sprite (set its borders in the Sprite Editor) so the corners don't stretch. |
| **Shape > Inside colour / Border colour / Border width** | The button's colours and outline. To show background art exactly as drawn, set Inside colour to white (ticked) and Border width to 0. |
| **Shape > Size** | The button's width and height. The row makes room automatically. The Streamer active button holds the streamer's 76 px portrait as well as the icon, so keep it at least about 170 px wide. |

The Streamer active and Go Live buttons are filled with the site accent colour and have no border. Tick their Inside colour to change it. The portrait's ring is the **Avatar ring** entry in the UI Skin.

The **Words** text for each button is in `Resources/Localization/AudienceCapture_Strings.csv`. The keys are
`shopbtn.power`, `shopbtn.raise_cap`, `shopbtn.remove` and `shopbtn.go_live`. Edit the word in
all five languages.

## 3. Prices above the buttons

The Streamer active, Raise Sub Cap and Remove a Viewer show their Influence price just above the button. The price turns red when you can't
afford it.

| Setting | What it does |
|---|---|
| **Font size** | The price's size (built-in 26). |
| **Gap above the button** | Pixels between the price and its button (built-in 6). |
| **Colour** | The price's colour (built-in: the shop's text colour). |
| **Colour when too expensive** | The colour when you're short of Influence (built-in red). |

The price texts are also listed in **UI Skin > Text Colours** (Shop > ... > Cost). A colour set there wins over these
settings, including the red.

---

## 4. Viewer cards: sizes, built-in card, colours

| Setting | What it does |
|---|---|
| **Narrowest / Widest** | The card width range. The shop fits as many columns as it can (built-in 160 to 196 px). |
| **Height / width** | The card's proportions. Built-in is 1.4; 0 with your own prefab uses that prefab's proportions. |
| **Space between** | The gap between cards, across and down (built-in 38 x 36). |
| **Extra space between rows** | Added between one row of cards and the next, on top of the space down, so the card piles above each card don't crowd the row above. -1 = built-in (32 px); 0 = none. |
| **Space above the first row** | The gap between the top of the card area and the first row (the first row's card piles peek into it). -1 = built-in (30 px plus the extra space between rows, so the first row gets the same extra room as the others); 0 = none. |
| **Hover growth** | How much a card grows under the pointer (built-in 1.04; 1 = not at all). |
| **Shape** *(built-in card)* | A 9-slice sprite for the card's outline and inside, for example rounded corners. It's also used for the card backs in the pile. |
| **Border width** *(built-in card)* | The card's outline (built-in 3; 0 = none). |
| **Hide the card piles / Pile step** *(built-in card)* | Hides the card backs stacked above each card, or changes how far each one peeks out (built-in 7). |
| **Trophy card border, Price, Price too expensive, Copies left, Last copy, Sold out, Can't-buy veil** | Colours for any card, yours included. |

The built-in card's inside, border and text colours are in **Site Colours > Shop tiles** and **Text Colours > Shop
tiles**.

---

## 5. Your own card prefab

### Quick start

1. Make the template: **Deck Party > Shop > Create Shop Card Template**, or **Make a template prefab** in UI Skin >
   Shop. It's saved to `Assets/DeckParty/Prefabs/Custom/ShopCardTemplate.prefab`. Each new one gets its own name, so
   it never overwrites an earlier one.
2. Open it (double-click) and design your card. Move, resize, restyle and replace anything. Keep each part's object
   name or its **Shop Card Part** component, so the game knows what it is.
3. Drop the prefab on **UI Skin > Shop > Viewer cards > Card prefab**. Under the field, the inspector lists the parts
   it found and warns about anything a card needs.
4. Press Play and open the shop.

To go back to the built-in card, clear the **Card prefab** field.

You can also duplicate `Assets/DeckParty/Prefabs/UI/ShopItem.prefab` (the built-in card, already wired up) into
`Prefabs/Custom` and edit the copy. Don't edit `ShopItem.prefab` itself: a rebuild regenerates it. Your prefabs in
`Prefabs/Custom` are never touched by a rebuild.

### Cards made from the older template

Templates made before 12 November have **Stats** as one text, with icons for Cash, Influence and Risk. New templates split
it into two numbers with no icons: a **Cash** text and a **Influence** text inside a **Stats** group. Risk is left out,
because the Risk Badge already shows it.

To change an older card, either:
- select the prefab in the Project window and choose **Deck Party > Shop > Split Stats into Cash and Influence**; or
- press **Split Stats into Cash and Influence** under the **Card prefab** field in UI Skin > Shop. The button only shows
  when the card has the old Stats text.

Stats keeps its place and size and becomes the group. The two new texts copy your old Stats text's font, size, colour
and bold, and each fills half the group. Your other changes to the card stay as they are. If the prefab is open in
Prefab Mode, the change is made there (Ctrl+Z undoes it): save the prefab afterwards.

### Rules for the card's root

- The root needs a **RectTransform**, anchors in the **middle**, and a fixed **Width** and **Height**. That's the size
  you design at. The template is 180 x 252.
- The shop scales the whole card, as one piece, to fit its slot in the grid. Your texts and pictures keep their
  proportions, and the card's Width/Height ratio sets the grid's proportions (unless you set Height / width in the
  skin).
- If the root's anchors stretch, the card is treated as 180 x 252.
- **No Canvas** inside the card: it lives in the shop's canvas.
- Use **TextMeshPro** texts. The price and stats contain small icons, which only TextMeshPro can show. Use the
  project's fonts (`Assets/DeckParty/Fonts`), which include Chinese and Russian.
- The game adds what a card needs if it's missing: the click (Button), the hover growth and the hover tooltip with the
  viewer's details.

### How the game finds each part

Each part is found in one of two ways:
- **A Shop Card Part component** on the object (Add Component > Shop Card Part, then pick its **Part**). This is the
  safest way.
- **The object's name**, exactly the part's name (case doesn't matter).

Whatever you type or place is a placeholder. The game replaces it for each viewer when the shop draws. Objects that
aren't parts (decorations, extra labels, backgrounds) are left as you made them.

| Part | Put it on | What the game does | Other names it accepts |
|---|---|---|---|
| **Background** | Image | The card's inside. It's what you click, so leave **Raycast Target** on. **Required** (or an Image on the root). | |
| **Frame** | Image | The card's outline. The game turns it gold on Trophy viewers. | |
| **Portrait** | Image | The viewer's picture or animation. Put it inside its own frame object: the purchase flourish flies from that parent. **Recommended.** | Art |
| **Placeholder** | Image | Shown instead of the picture while a viewer has no art, tinted in the viewer's colour. | |
| **Initials** | Text | The viewer's initials, shown on the placeholder. | |
| **Name** | Text | The viewer's name. | ViewerName |
| **Description** | Text | What the viewer does (the card description; it can be long). | |
| **Stats** | Text | What the viewer scores on one line, with icons: Cash, Influence and Risk. The template doesn't use it (see Cash and Influence below). | |
| **Cash** / **Influence** | Text | Just the number the viewer scores, no icon (+2, +1, 0). Group cards show their steps instead (Dancing Jen's Cash 1/4/9/16/25, Crypto Enthusiast's Influence 0/1/4/9/16), so let these texts shrink to fit (Auto Size). In the template they sit in a group named **Stats**: Cash on the left, Influence on the right. The group itself is just a holder. | |
| **Action** | Text | The name of the viewer's action; empty when it has none. | |
| **Price** | Text | The Cash icon and the price. The game colours it: your text colour if you can afford it, red if you can't. **Recommended.** | Cost |
| **Copies Left** | Text | How many are left to buy ("×3"); empty for Trophy viewers, which never run out. Turns orange on the last copy and red when sold out. | Left, Copies |
| **Communities** | An empty object | Its child Images become the community icons, in order (three is plenty). Unused ones hide. Hovering one explains the community. | |
| **Trophy Badge** | Any object | Shown only on Trophy viewers. | Trophy |
| **Risk Badge** | Any object | Shown only on viewers that add Deplatform Risk. | |
| **Cancels Risk Badge** | Any object | Shown only on viewers that cancel Deplatform Risk. | |
| **Action Badge** | Any object | Shown only on viewers with an action. | |
| **Cant Buy Veil** | Image | Laid over the card while you can't buy it. Leave the Image switched off; the game switches it on. The colour can be set in the skin. | Veil, Dim |
| **Sold Out** | Any object | Shown when none are left. Leave it switched off. | |
| **Pile Card** | Any object | One card back of the pile behind the card. Give each an **Order** (1 = nearest the card). The game shows one per copy left after the top card, so the pile shrinks as the shop runs out. Place them yourself, and put them before the card's body in the hierarchy so they draw behind it. | PileCard1, PileCard2... (by name) |
| **Anim Root** | Any object | The part that bounces when you buy the card. Default: the whole card. If you have piles, use a "Body" object holding everything except the piles (as the template does). | |

Every part is optional except Background. A part you leave out simply doesn't show (for example, no Name means no
name on the card; it's still in the hover tooltip).

### Wiring by hand (optional)

You can also add a **Shop Item View** component to the card's root and drag objects into its fields. The game only
looks up parts for the fields you leave empty.

### Tips

- **Animated viewers** work in the Portrait part as they do on the built-in card. Give the Portrait image a square-ish
  space with Preserve Aspect on.
- **Editing the prefab while playing:** cards already in the shop don't change. Stop and press Play again, or change
  any UI Skin > Shop setting to rebuild them.
- **Text Colours:** texts inside your own prefab aren't listed in UI Skin > Text Colours. Colour them in the prefab
  itself, except Price and Copies Left, which the game colours (use the skin's card colours for those).
- **Multiplayer:** nothing to do. Every player sees their own UI Skin.
