I want you to read this document as if you are a highly skilled, highly experienced game developer and unity engineer. I want you to first implement the required changes and then to write a short instructions .md file for me for guidance on testing the specific parts of the project that I will outline in this document. 

# Name Changes

I want to change the names of different game mechanics or resources. 

I want you to replace Pop/Population Cap with Subs/Subscribers and earned population points to spend on new characters/cards as gifted subs

I want you to keep the cash resource with the same name but instead of buying a larger house you are buying a larger subscriber cap

The removal of a character from the rolodex when the trouble goes over the limit should be called a “Temporary Ban” 

Replace the Trouble mechanic with the name “Swatting Risk” and if the trouble gets too high then the swat team shows up 

I also want to replace the names of the following characters (but not their functions/costs etc)

Old Name, New Name:

Old Friend, Loyal Fan
Wild Buddy, Bot Farm,
Rich Pal, Whale,
Cute Dog, VIPS,
Hippy, AutoMod,
Security Guard, Moderator, - “Removes From Chat” instead of booting,
Rock Star, Collaborator,
Driver, Social Media Manager,
Grillmaster, HODL, “Resets the Stream”,
Socialite, Parasocilate

Then for characters with stars replace them with names appropriate to this theme but keep all of them. 


Then the remaining characters should be removed from the game and replaced with the new characters (and their effects) . You should also remove the current “presets” for game characters as they are relying on the original character designs and powers. Instead come up with a new set of themed character sets representing different character sets for playing of the game including the random option. 

I also want to replace each of the different backgrounds with a background Image (I will add the sprites/images for the backgrounds myself manually just include instructions for how to do that). At a later stage I will add animations. 

Also replace the star mechanic name with diamonds. The player needs that many diamonds to win instead of stars

Also replace the name Rolodex with something more theme appropriate (for the current available deck that the player has created)

# New Characters

As I said earlier I want you to remove the remaining characters from the original specs (that were not mentioned in this document) and I want you to replace them with this list of new characters

Characters 1–10
#
Character
Cost
Base stats
Game logic / Ability
1
Doge
7
+1 P
Such Synergy: Gains +1 P for each different character category at the party, up to +5 additional P. Other Doges don't count toward this bonus.
2
Trollface
9
+1 T
U Mad?: Once per party, select a guest and force them to leave. They return to the Rolodex and can be drawn again tonight. Each time the same guest is expelled by this effect, Trollface gains +2 P.
3
Pepe
8
0
Mood Swing: Once per party, choose Happy (+3 P), Sad (+3 $), or Angry (+6 P and +1 T). The chosen effect lasts until the party ends.
4
Gigachad
19
+5 P
Alpha Energy: Gains +3 P if he's the only guest with base P of at least 5. Each additional Gigachad reduces every Gigachad's P by 2.
5
This Is Fine Dog
12
+1 P
Denial: The first time Trouble would reach 3, prevent the raid and clear all Trouble. Permanently remove this copy from the Rolodex afterward.
6
Wojak
5
+1 P
Shared Suffering: Gains +2 P for every other guest currently suffering a negative stat or harmful status effect, up to +6 P.
7
Grumpy Cat
9
+2 P
Absolutely Not: Once per party, inspect the next 3 guests. Reject one for the night and arrange the remaining two in your preferred order.
8
Nyan Cat
14
+1 P
Rainbow Trail: Each consecutive non-Trouble guest arriving after Nyan Cat generates an increasing P bonus (+1, +2, +3, etc., capped at +6 per arrival). A Trouble arrival breaks the chain.
9
Stonks Guy
8
+1 $
Market Manipulation: Invest either 2 P or 1 $. If the party succeeds, receive three times the invested amount; if it fails, lose the investment. Once per party.
10
Cheems
5
+2 P
Don't Bonk Me: If Doge is present, cancel 1 Trouble and give both Doge and Cheems +2 P. Otherwise, Cheems loses 1 P for each Trouble at the party.

Characters 11–20
#
Character
Cost
Base stats
Game logic / Ability
11
Chad vs Virgin
10
+2 P
Know Your Place: Choose another guest. If their base P is lower than Chad's, permanently transfer 1 P from them to Chad. If higher, Chad permanently loses 1 P. Once per party.
12
Doomer
7
+1 P
Doomscroll: Gains +2 P for each Trouble currently at the party. At exactly 2 Trouble, also generates +2 $.
13
NPC Wojak
6
0
Current Thing: On arrival, copies the printed P and $ of the immediately preceding guest. Doesn't copy their ability, Trouble, or Stars.
14
Forever Alone
8
+1 P
Table for One: Gains +6 P if he's the only guest when the party ends. Otherwise, gains +1 P per empty house slot.
15
Me Gusta
8
+1 P
I Like That: Gains +2 P whenever another guest causes Trouble. Gains +1 $ whenever Trouble is removed.
16
Keyboard Cat
7
+1 P
Play Him Off: Once per party, evict a guest. If they had Trouble, gain +2 P; otherwise gain +1 $. Triggers the evicted guest's On Exit ability.
17
Bongo Cat
11
+1 P
The Beat Goes On: Every third guest admitted after Bongo Cat gives all animal-meme guests currently present +2 P for tonight.
18
Pop Cat
9
0
Pop Pop Pop: Gains +1 P whenever any guest uses an Action. Every third Action also generates +1 $.
19
Big Chungus
16
+7 P
Absolute Chungus: Occupies 3 house slots. Gains +1 additional P for each point of house capacity above 10, capped at +5 P.
20
Ugandan Knuckles
9
+1 P
Follow Me: On arrival, adds a temporary copy of himself to tonight's draw pool. Each Knuckles gains +1 P per other Knuckles currently present. Temporary copies disappear after the night.

Characters 21–30
#
Character
Cost
Base stats
Game logic / Ability
21
Overly Attached Girlfriend
8
+1 P
Forever Yours: On arrival, attaches to another guest. Doubles their P but prevents them from voluntarily leaving or being evicted while she remains. If they are forcibly removed, she leaves too.
22
Disaster Girl
13
+2 P
Accidental Fire: Once per party, give another guest +1 T and +3 P. Disaster Girl also gains +2 P. Cannot activate if it would immediately trigger a raid.
23
Hide the Pain Harold
10
+2 P
Smile Through It: Prevents reductions below printed P for guests at the party. Gains +1 P per prevented reduction, capped at +5 additional P.
24
Success Kid
12
+1 P
Against All Odds: Once per party, either reroll a random ability outcome or inspect the next guest and decide whether to admit them.
25
Bad Luck Brian
6
+4 P, +1 T
What Could Go Wrong?: Once per party, the first attempt to remove Brian, evict him, or cancel his Trouble fails. Brian permanently gains +1 printed P.
26
Tung Tung Tung Sahur
12
+2 P
Three Strikes: Strike a guest once per party. First strike gives +2 P; second removes their Trouble; third evicts them and permanently increases Tung Tung Tung Sahur's P by 1. Strike counters persist across parties.
27
Tralalero Tralala
11
+3 P
Tralalero Shuffle: Once per party, swap two guests' positions in the arrival order. Recalculate position-dependent bonuses without replaying On Arrival abilities.
28
Bombardiro Crocodilo
15
+5 P, +1 T
Bombardment: Once per party, evict up to 3 guests and replace them with the same number of random unused guests, if available. Arrival and exit effects activate normally.
29
Ballerina Cappuccina
10
+2 P
Pirouette: Once per party, choose two guests and exchange their printed P values for the rest of the night. Other stats and abilities remain unchanged.
30
Skibidi Toilet
13
+2 P, +1 T
Skibidi Invasion: On arrival, infects another guest. Infected guests gain +2 P and +1 T. When further guests are invited, infected guests spread the infection to an uninfected guest. Infection ends after the party.

Characters 31–40
#
Character
Cost
Base stats
Game logic / Ability
31
Sigma Male
14
+3 P
The Grindset: Gains +1 P for each empty house slot, up to +8 P. Loses 1 P for every other Sigma Male present.
32
Karen
8
+1 T
I Demand a Refund: Once per party, remove another guest and receive half their original recruitment cost as P, rounded down and capped at 8 P. That guest cannot return tonight.
33
Rickroll
11
+2 P
Surprise!: Once per party, select a non-Star guest. They temporarily become a randomly chosen different non-Star character type from the remaining Rolodex, adopting its printed stats and ability. Their permanent identity stays unchanged.
34
Distracted Boyfriend
9
+2 P
New Attraction: On arrival, reveal 3 guests from the remaining Rolodex. You may replace one guest currently present with one of them. The replaced guest becomes unavailable tonight; unchosen guests are shuffled back.
35
Drakeposting
10
+1 P
I Prefer This: Once per party, choose 2 guests. The first produces zero P and $ tonight; double either the printed P or printed $ output of the second.
36
Confused Math Lady
12
+1 P
The Numbers Don't Add Up: Once per party, convert one guest's P into $ at a rate of 2:1 (rounded down), or their $ into P at 1:2. Conversion lasts until tonight ends.
37
Side Eye Chloe
7
+1 P
Side Eye: Always reveals the next guest waiting outside. Once per party, move that guest to the bottom of the remaining draw pool without admitting them.
38
Roll Safe
9
+2 P
Can't Lose If You Don't Play: Once per party, choose one guest. They become immune to further ability effects for tonight, including both positive and negative modifiers. Existing stats and status effects remain.
39
Crying Cat
7
0
Everyone Leaves Me: Whenever another guest leaves or is evicted, Crying Cat gains +2 P and +1 $ for tonight.
40
Dancing Baby
10
+1 P
Looping Animation: Whenever two consecutive guest Actions belong to the same effect category, Dancing Baby gains +3 P, capped at +9 P per party.

Once you have implemented the required changes I want you to output a file called

V1Instructions.md which should have explicit instructions for how I can add the new character sprites (not animations yet) and also the background images to the game. 








