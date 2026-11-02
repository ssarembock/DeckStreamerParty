# Additional Communities

I want to add the following communities and their game logic to the project. 

Clout: +10% for each community score (from 0-40)  chance of an additional  gifted sub for each non core-fan viewer during scoring. 

Memetics Division: community score ranges from 0-10. Each representing a percentage chance that the entire scoring round is repeated. So this is not on an individual viewer card scoring basis. So a Memetics division score of 5 means a 5% chance each round that the scoring will happen twice.

Bots: The bot score also represents a kind of probability. But the bot score works like this. It can go from 0-4 representing a probability range from 0 to 40%. For each viewer card in the bot community, during scoring their is a percentage chance (determined by the bot score) that a bot card is added temporarily to the audience for the next player. Bot cards take up a slot when invited from the audience but they offer 0 rewards 0 cash and 0 gifted subs

# Viewer Card Updates

Remove from the game (not the build, keep the code but don’t use these viewer cards in the current build/tools/or database but mark them as put aside) any viewer cards that are not in the list below. I have also updated the logic/communities of some of the cards so update them to reflect this new table. Only viewer cards from this table should be accessible in the current build and database modification tools in unity. 

Either update or remove any cards that don’t fit the new table:



Name
Description
Community
Ineffective Troll
Starting viewer only (you begin the season with 4); never sold in the shop. No special effect.
Core Fanbase, Trolls
Loyal Fan
Starting viewer (you begin the season with 4). No special effect. Binge Bunny gains from every Loyal Fan in chat.
Core Fanbase
Influential Troll
No special effect.
Trolls
Producer
Action - Shoutout: choose any viewer in your queue to join chat, if there's room.
Staff
Whale
Starting viewer (you begin the season with 2). No special effect.
Core Fanbase
Chat Admin
Action - Remove From Chat: remove one viewer from chat for the rest of the stream (can remove itself).
Staff
Cybersecurity Engineer
Action - Read Access: look at the next viewer in the queue, then let them in or deny them for the rest of the stream.
Staff, Techpertise
Gatekeeper
You Shall Not Watch!: +1 Gifted Subs for every empty space in chat when scored.
Core Fanbase, Trolls
Normie
Cancels 1 Platform Ban Risk while in chat. No other effect.
Core Fanbase
Paid Sponsorship
No special effect.
Finance Bros
Account Manager
No special effect.
Staff
Binge Bunny
Algorithmic Rabbit Hole: +1 Gifted Subs for every Loyal Fan in chat.
Core Fanbase
Promo Code User
+5 Gifted Subs when scored, but only if chat is full.
Core Fanbase
Crypto Enthusiast
Action - Rug Pull: score another viewer in chat right now, penalties included, and bank it straight away. They still score again when the stream ends.
Independent Researcher
Generous Donator
Give to the Rich!: when joining, brings in the next viewer from the queue and scores them once right away (banked immediately). This can push chat over the Sub Cap (Server Down).
Core Fanbase, Finance Bros
Hate-Watcher
Switches mood every time they join chat: hate-watching (+1 Platform Ban Risk) on their first entrance, harmless on the next, and so on. The mood carries over between streams.
Trolls
Junior PR Manager
Action - Situation Defusal: send everyone in chat back to the queue so they can join again. Actions already used stay used.
Staff
Marketing Manager
Viral Moment: brings one random viewer from the queue along when joining. This can push chat over the Sub Cap (Server Down).
Staff
Mildly Risky Collaborator
No special effect.
Content
Highly Risky Collaborator
No special effect.
Content
PR Manager
Action - Damage Control: send everyone in chat back to the queue so they can join again. Actions already used stay used.
Staff
Affiliate
Action - Checkout Code: a viewer in chat permanently earns +1 Cash more (up to 9 Cash).
Finance Bros
Chat Arbitrator
Action - Keep the Peace: clear the Platform Ban Risk of every risky viewer in chat for the rest of the stream, even if they leave and come back. Needs at least one risky viewer in chat.
Staff
Die Hard Fan
Cancels 1 Platform Ban Risk while in chat. The card says "Uncancellable", but that has no extra rule yet, so it plays like a Normie with more Subs.
Core Fanbase
Risky Orbiter
No special effect.
Content
Bot Detector
Action - Bot Removal: remove two viewers sitting next to each other in chat (consecutive spots) for the rest of the stream. The Bot Detector can be one of them.
Techspertise
Master of Currency
Chaos Ladder: +2 Cash for every viewer in chat who has Platform Ban Risk (cancelled risk still counts).
Finance Bros, Boomer
Social Media Manager
Action - Sneak Peek: look at the next viewer in the queue, then let them in or deny them for the rest of the stream.
Staff, Zoomer
Billionaire Benefactor
Action - Softball Interview (once per stream, shared by every Billionaire Benefactor): every viewer in chat drops to 0 Subs tonight, and you bank 1 Cash right away for each Billionaire Benefactor in chat.
Boomer, Finance Bros
Tech Admin
Action - Admin Rights: remove one viewer from chat for the rest of the stream (can remove itself).
Techspertise
Executive PR
No Bad Press: +2 Gifted Subs for every viewer in chat who has Platform Ban Risk (cancelled risk still counts).
Staff, Boomer
Online Scandal
Unwanted Guests: brings two random viewers from the queue along when joining. This can push chat over the Sub Cap (Server Down).
Content
Gen Z with Job
Secure The Bag: permanently gains +1 Cash every time they join chat (up to 9 Cash), even if the stream ends badly.
Zoomer
Big Boomer
Diamond: one of the 4 you need in chat at the end of a successful stream to win. Unlimited shop stock (not shared). Opinion Express: no other effect.
Boomer
Clippo The Clanker
Diamond: one of the 4 you need in chat at the end of a successful stream to win. Unlimited shop stock (not shared). Market Collapse: no other effect.
Bots
Bigger Star
Diamond: one of the 4 you need in chat at the end of a successful stream to win. Unlimited shop stock (not shared). Uplifting: brings one random viewer from the queue along when joining (can push chat over the Sub Cap).
Content, Clout
Precocious Youth
Diamond: one of the 4 you need in chat at the end of a successful stream to win. Unlimited shop stock (not shared). No other effect.
Content
Internet Starpower
Diamond: one of the 4 you need in chat at the end of a successful stream to win. Unlimited shop stock (not shared). Can Do No Wrong: cancels 1 Platform Ban Risk while in chat.
Content, Clout
Platform Owner
Diamond: one of the 4 you need in chat at the end of a successful stream to win. Unlimited shop stock (not shared). Action - Free The Speech!: remove one viewer from chat for the rest of the stream (can remove itself).
Clout
Mainstream Celebrity
Diamond: one of the 4 you need in chat at the end of a successful stream to win. Unlimited shop stock (not shared). Mass Appeal: no other effect.
Clout, Content
Senior Marketing Manager
Diamond: one of the 4 you need in chat at the end of a successful stream to win. Unlimited shop stock (not shared). Product Placement: no other effect.
Staff, Millenial
Surprise Guest (Rename this to Evangalist)
Diamond: one of the 4 you need in chat at the end of a successful stream to win. Unlimited shop stock (not shared). Action - 2-for-1!: summon any viewer from the queue to join chat, even another Diamond.
Core Fanbase
Crying Cat (Rename to Distraught Cat)
Everyone Leaves Me: +2 Gifted Subs and +1 Cash whenever another viewer leaves or is kicked.
Memetics Division
Doge (Rename to Degen Dog)
Such Synergy: +1 Gifted Subs for each different card segment in chat (max +5). Other Doges and TBD viewers don't count.
Memetics Division








Come up with new puzzle set pieces and names and groupings based on these new viewer character card sets and themes and game logic. (I mean currently there are names for preset subsets of the audience like Italian Brainrot etc. They are out of date relative to the new updated list of viewer cards so make up new ones that are in line with the overall theme). 

# Multiplayer Updates

If the current functionality for multiplayer doesn’t support it (check if it does already) then add the functionality that more than 2 players can play a multiplayer game (Free For All Mode). In this mode the players cannot select turn based and instead take their turns together at the same time (first the round then the shop). They must all finish the first half of their turn and then click on the shop button (so they must all be ready) before they all transition to the shop together in this mode because the shop is a first come first serve system in the non-turn based mode. 

When all players are ready (ended their stream). Then there should be a mini countdown 3,2,1 before the shop opens. And also there should be something to indicate that the shop is first come first served. So to be clear in turn based mode you should watch the other player take their turn when you are not the active player. In the other mode (which is the only mode allowed in FFA or games with more than 2 players) is that all players take their turns simultaneously (and they cannot watch each others turns) and then when they are all ready they all get access to the shop simultaneously. For this mode add a small button in the UI which allows players to see a summary of all the pertinent actions that all players took during the turn including rewards and game mechanics that impact future turns potentially. 

# Minor Bug Fixes and UI Tweaks

The 9-sliced chat sprite used for the UI containing the viewer cards is meant to take up most of the horizontal space on the screen. Currently it still has empty space where optional or old UI elements would be. Make sure the 9 slicing always fills up the available horizontal space in the UI (with some padding of course). Also make it that there is a max row size for the viewer cards in a stream (in the chat). So that when we reach a certain number it starts a new row. The chat should create a rectangle of viewer cards as more viewers are added. Also remove the text “Empty” from the UI for the empty viewer card slots

Make the warning effects and feel/juice for the warning the player that theres deplatform risk slightly more subtle. Slightly Reduce the intensity and frequency of the screen shake there for example. Also the popup when the player is 1 risk away from deplatform should say “We’re” at risk of getting deplatformed not you as it said from the perspective of the streamer. 

Also remove the UI text telling the user the information about the current stream in text (I mean the text string in the UI saying Live Chat X/X Sub Cap Y in the queue etc). Also can you move the Live Text and counter to be in the box on the bottom right which shows the streamer (active player) . Also can you decrease the size of the box on the bottom right which shows the active player (make it ¾ the size). 

Then finally the box on the bottom right which shows the active players sprite should only be visible during the stream and not during the shop phase. 


