Read this document as if you are a highly experienced, world class game developer with a speciality in the unity game development environment. Implement the changes required by this document and then create a short instructions file for me which a short list of step by step instructions (or guidance) on where to put various sprite elements in order for the changes to work. 

# UI Changes

I want to remove the chat log of user chat and also the text of the activity log (adding viewers to chat log etc) from the visuals. I don’t think that information is useful or relevant to the game and I want it gone from any of the screens. To be clear I am particularly talking about the Stream Log UI and the information it displays (remove it entirely) and also the stream chat. Currently there is an option to hide this chat in the menu but I rather want to remove this entirely. 

The current screen is too cluttered with superfluous UI elements and I want to represent the theme of being in a stream/podcast chat in a different way. 

Only remove UI elements I have explicitly asked you to , don’t just remove other elements to reduce clutter. For now just remove the Log of activity and the chat text. 

Instead we will visually represent the fact that the scene is in an online chat by having a large, 9 sliced , square chat bubble as the background for the viewer or audience cards added to the chat so far this round. I will also need to add a tail sprite which is facing down from above to show the chat is coming down from the scene background image above. The game scenes during gameplay will typically have a header image above representing the streamer or type of content creator and there will typically be a computer somewhere in that image and then an arrow will come down from that computer into the 9 sliced chat bubble and then each character added will be added to a card contained within the large chat bubble which serves as the background for the rounds added viewers. 

I want the viewers that have been added to the chat to randomly show an emoji which floats upwards and fades away. I will create some sprite sheets for these emojis characterized under levels of danger. These emojis represent peaceful, slightly argumentative and very angry emojis and the type and frequency of the emojis these characters generate will change based on the danger level (which is currently swatting risk, but we will get to that now). 

I want to change the name of the Swatting Risk mechanic to Platform Ban Risk. Everywhere in the Project that refers to Swatting will now be referred to as Platform Ban. I will create an image of what to show when the Platform Ban risk goes over the limit and the stream ends automatically. Just add to the instructions where to put this image

I also want to change the name of the end of round mechanic where if the player brings in more than their allowed number of viewers (above the cap), that the round ends. This mechanic should now be called Server Down and I will create an image for what to show the user in the UI when this happens just add it to the instructions file where I need to put this image and how to add it to the project.

### Emote Button Changes
I am going to add sprite sheets for the multiplayer emote buttons. Instead of having multiplayer chat I am reducing the communication of players in multiplayers to these emotes. The buttons will be sprite images and will cause all players to see the emoticon pop up from the players avatar in the multiplayer HUD during gameplay (and fade similar to the viewers in the chat effect). I will create the sprite sheet just tell me where I need to put it in the project (in the instructions file) for it to work. Hide the functionality for text chat but do not remove it entirely as I may want it later for a different multiplayer mode. 

## Shop UI Changes

As with everywhere else, remove the text log from the shop. I think in both cases for the activity logs dont fully remove them but have them switched off in the menu by default. The fake chat feed you must remove entirely. 

For the shop I want you to also remove the UI stuff which tells you the summary of what happened in the round. I just want the player character streamer card (with active button where available), the button to increase cap and the button to see the players audience collection (roladex). Then the majority of the shop page UI should be for the purchasing of new viewers . Each of these items in the shop should only have a name, a picture (increase the size of this on screen) , character group icon (which I will add later), a price and the number of them left for players to purchase. The remaining information should either given via the information UI popup when the player places their mouse over for more information or alternatively by looking at their audience [R]. Keep the direct immediate UI for the shop simpler and increase the sprite sizes a bit. Also it doesn’t need to be a table with one row per entry–  you can make the shop elements more of a grid structure with rows and columns. 

# Some Game Logic/UI Bugs

I have noticed that in the multiplayer some of the constraints, information, etc is being shared between both players when it shouldnt be. For example I think I was seeing my opponents number owned on my turn when looking at a character. The rolodex, number of available purchases remaining, etc should apply to the player in question – particularly in multiplayer and when playing turn based. Also I noticed that when my opponent used his active the effect that it had on his player card (of rotating it slightly) A: was not temporary and did not reset the next time the player entered the shop and B: the competitors also had their player cards rotated as if they has used the active prior. I think you need to be more clear around the separation of concerns in terms of game logic and UI for the multiplayer. I previously requested (and you implemented excellently) that in turn based multiplayer the non-active players can watch what the active player is doing in real time (or pseudo real time with some delay and update frequency tolerance – but just to stress this is currently working well) so I don’t want any of these separation of concern changes and fixing of these bugs to impact that feature. In turn based multiplayer mode the non–active player should still be able to watch the actions of the active player.  Just do an audit on why the players might be sharing some stats or information in multiplayer and make sure their player states , collections, data and resources are tracked separately. The only thing they share is the characters/viewers available for purchase/recruitment in the shop. This available characters in the shop is a shared resource between all players in the game (this does not apply to actives which have a cost as they are particular to streamer characters). So just to be clear the recruitment options for purchase in the shop are the only shared resource and are the only actions so far that a player can take to impact the options of the other player (this may change with new viewer types later but I am saying it more to stress the nature of the bug I am seeing). 

# Audience Cards/ Viewers Updates

I want you to either update the data for the viewer cards based on the table provided here (if they exist already in the game logic/mechanics or alternatively add them as characters to the project logic). Most of these are slight variations on the characters provided in the original UFO 50 game description of which this project is a spiritual successor (with a lot of new mechanics and features so it was just included for reference). I am adding columns for the name of the original character in party house ufo 50, the new name I want (regardless of their name now in the project), the logic of the card – which might be slightly tweaked. Also I know there is a labelling/grouping mechanic which groups certain viewer types under a common umbrella group. Can you set these all to TBD (To be determined) for now and I will do those at a later stage. 



Old Name
Cost
Description


New Name
Power/Active Name
Old Friend
2
Basic starting guest; provides one popularity.


Loyal Fan


Wild Buddy
Unbuyable
Starting guest: generates two popularity but adds one Trouble.


Ineffective Troll


Rich Pal
3
Basic starting guest; provides one cash.


Whale


Driver
3
Choose a specific remaining guest from the Rolodex to invite, if space is available.


Producer


Monkey
3
High popularity with one Trouble.


Influencal Troll


Hippy
4
Prevents one unit of Trouble from counting toward the police threshold while present.


Normie


Watch Dog
4
Peek at the next arrival and optionally reject it for the remainder of this party.


Cybersecurity Engineer
Read Access
Security
4
Remove one guest (including itself) from the party and this night’s draw pool.


Chat Admin














Ticket Taker
4
Provides two cash but deducts one popularity when scored.


Paid Sponsorship














Rock Star
5
Provides three popularity and two cash but adds one Trouble.


Mildly Risky Collaborator


Comedian
5
Earns five popularity only if the house is full when scored; costs one cash.


Comedian with a podcast


Caterer
5
Provides four popularity at the cost of one cash.


Account Manager


Photographer
5
Immediately score another guest of your choice (including its score penalties), independently of the end-of-party scoring.


Crypto Guy
Rug Pull
Grillmaster
5
Return everyone currently at the party to the Rolodex for redrawing; used actions stay used.


Junior PR Manager
Situation Defusal
Greeter
5
On arrival brings in the next guest and immediately scores that incoming guest once.


Generous Donator
Give to the Rich!
Mr. Popular
5
Automatically brings one additional guest when entering; can overfill the house.


Marketing Manager
Viral Moment
Athlete
6
Return everyone currently at the party to the Rolodex for redrawing; used actions stay used.


PR Manager
Damage Control
Gangster
6
Provides four cash but adds one Trouble.


Highly Risky Collaborator
#NotHimToo?
Cute Dog
7
Prevents one unit of Trouble from counting toward the police threshold while present.


Die Hard Fan
Uncancellable












Counselor
7
Remove Trouble flags from all current Trouble guests for this party (including after they are reshuffled).


Chat Arbitrator
Keep the Peace
Cupid
8
Remove two horizontally adjacent guests from the party and from the remainder of this night’s Rolodex.


Bot Detector
Bot Removal
Gambler
7
Provides two popularity and three cash but adds one Trouble.


Risky Orbiter


Spy
8
Peek at the next arrival and optionally reject it for the remainder of this party.


Social Media Manager


Auctioneer
9
Set popularity to 0 and immediately score 1 cash for each billionaire on the board. Can only be used once per turn. +5 cash


Billionaire Benefactor
Softball Interview
Wrestler
9
Remove one chosen guest from the party and from the remainder of this night’s Rolodex.


Tech Admin
Admin Rights
Celebrity
11
Automatically brings two additional guests when entering; may overfill house.


Online Scandal
Unwanted Guests
Alien
40
Star guest with no additional effect or resources.


Precocious Youth






Name
Cost
Description
New Name
Power Name
Introvert
4
Earns one popularity plus one per vacant party space at scoring time.
Gatekeeper
You Shall Not Watch!
Mascot
5
Earns one popularity plus one per Old Friend present.
Rabbit Themed Name
Algorithmic Rabbit Hole
Werewolf
5
Earns four popularity; alternates between troublesome wolf and harmless human on successive entrances (first entrance wolf).
Hate-Watcher












Stylist
7
Permanently increases one selected guest’s cash score by one (up to the score cap); costs one cash when scored.
Affiliate
Checkout Code
Writer
8
Earns one cash plus two per Trouble-flagged guest at the party when scored.
Master of Currency
Chaos Ladder
Bartender
11
Earns two pop for every Trouble-flagged guest at the party when scored.
Executive PR
No Bad Press
Climber
12
Permanently gains one cash each time it enters (maximum nine), even if the party later fails.
Gen Z with Job
Secure The Bag
Dinosaur
25
Star guest with one Trouble.
Big Boomer
Opinion Express
Dragon
30
Star guest that costs three cash when scored.
Clippo The Clanker
Market Collapse
Mermaid
35
Star guest that automatically brings one additional guest on arrival.
Bigger Star
Uplifting
Unicorn
45
Star guest that prevents one unit of Trouble from counting toward police threshold.
Internet Starpower
Can Do No Wrong
Ghost
45
Star guest that can remove one guest from the party for the rest of the night.
Platform Owner
Free The Speech!
Superhero
50
Star guest granting three popularity.
Mainstream Celebrity
Mass Appeal
Leprechaun
50
Star guest granting three cash.
Senior Marketing Manager
Product Placement
Genie
55
Star guest that can fetch a specific remaining guest from Rolodex, including another Genie or star.
Surprise Guest
2-for-1!


Keep in mind I have changed the logic or mechanics subtly in some cases so the description I have written in these tables is to be trusted over the old name given. 


# Streamer Character Actives

 Make sures that any streamer characters (that a player can select in the character select screen) that if they have have Actives those actives cost money/cash as their usage resource and increase that cost per use similar to cap expansion. 






