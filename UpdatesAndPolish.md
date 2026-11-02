I want you to read this document and implement the required changes. I want you to read it and implement it as if you are a world class, highly experienced game developer with expertise in unity. When anything is unclear or left as a design option, I leave it to your better judgement - but flag these decisions at the end of the work. After we are done I want you to write a new short instructions and guidelines file for the recent changes

# UI Updates, Icons and Name Changes

I want to change the name from Platform Ban to Deplatform. So the player gets deplatformed instead of getting a platform ban. This is purely an name change and does not impact the game mechanics or logic. 

For the resources in the UI, I no longer want to have it show text anywhere to indicate the name of the resource. I dont want it to say “Gifted Subs” or “Cash” instead I will supply sprite icons for these resources (if the player hovers their mouse over the info popup will give the name of the resource). This extends to the shop. When the price is shown there should be the sprite icon for that resource and just the text should only show the value or cost in that resource

I will be creating and supplying sprite icons for resources and for other UI elements we cover in this document. Later we will do a full audit of where we can make the UI less text heavy and more intuitive with images, but for now just apply this to the resources and anywhere else explicitly mentioned in this document.

## Community Beats UI

Aside from this change: I want to change the name of the “group” or “family” that a viewer card belongs to to be the viewers “Community”. A viewer typically has 1 or many Communities. I will cover exactly what they are and what the game logic should be later in this document, but for now I am mentioning that in the usual gameplay UI (the UI for the player in stream and inviting subscribers etc – the normal round flow before the shop) there should be a UI element covering these Community Beats. The way this game mechanic works is that for each unique viewer type in the chat from a community - then that community score goes +1 (which is reflected in this additional UI element). 2 of the same exact card name (so if the player brings in 2 of the exact same viewer – not just by community but actually they have bought 2 of the same card and both are in chat – then it doesnt count. This only applies to unique card names in the chat of a given community. To be clear the community points for a viewer community group only matches the number of unique viewers from that community in chat. If viewers are removed from the chat that would impact the community count, it should be adjusted accordingly. At the end of the turn the community values for each community impact how the round is scored is small ways. I will discuss this later in the document. This is just to outline the UI requirement. I want each increase in a community value to bestow an increasing benefit to the player 



### Basic Scoring Shop UI

I want to add 3 small icons in a key section in the info popup for shop cards.  To every viewer available for purchase, when the player puts their mouse over (hovers and the popup comes up) I want it have in the shop to indicate the basic resource changes when the round is scored. This should be shown via 3 icons (Cash, Gifted Subs, Deplatform Risk - We can also use the term Risk or Platform Risk Interchangably)


#Communities

I want the name for the group or family that a viewer card belongs to be called their “Community”. A viewer typically has 1 , but can have multiple communities. Their card in the chat and in the shop should have small icons indicating their communities - including the info popup for the cards. 

Each Unique viewer in chat from a given community increases that community score (Removing a unique viewer has the opposite effect – it is a real time community score not a score for the round). For each additional point there is a bonus or benefit which increases in value depending on the number of unique viewers from that group. This encourages certain kinds of synergies or groupings based on viewer communities. These are the communities , their names and the logic as they increase. If I have not included in this list then it should be removed from the available card groups/communities in the project



Core Fanbase
Every 3 community points in a round cancels 1 risk


Staff

Every 1 Additional Staff Points Increases the likelihood that each staff scoring doesn’t include the cost. I am going to set it up that every viewer in the staff category has a negative cash value in scoring. The way this will work is that if you have 1 staff point, that means that when scoring the viewers from the “Staff” Community, they have 10% of being free on each individual scoring. If its 2 unique staff, then its 20% all the way up to a maximum of 90 for 9 points. 

Content

Works similarly to staff in that the score impacts a probability. For each Content score from 1-9 it impacts the probability that a NON core–fanbase viewer will score twice during scoring

Techspertise
Has a 0, 20%, 40%, 50% of scoring after a server shutdown stream end (This has a maximum of 50% and a maximum score of 3 for this community)

Finance Bros
Similar to the above but the score scales the probability of scoring each viewers cash reward twice. At a score of 9 there is a 90% chance a viewers cash reward will be scored twice (or 2x cash reward)

Trolls

+1 Random Resource per troll in chat if the stream ends by deplatforming. Does not apply if the player ends the stream or if too many viewers are added. Only if the deplatform risk (or risk) limit is exceeded

Doomsdayers

This is the group or community that will be assigned to cards that have a negative effect on other players in competitive play. For each community score here at the end of the round, it increases the chance of not scoring each viewer for the competitor in the next round. So if the score here amounts to 20%, then in the next enemy round  when the competitor scores each of their viewers has a 20% of not scoring at the end of the round and they get none of that viewers benefits (or downsides if they apply at scoring). The maximum for this is 5 points which amounts to a 20% chance per viewer at scoring in the next enemy round. It goes 0,4%,8%,12%,16%,20% for 0,1,2,3,4,5 points respectively. 

I will add more community types and their logic at a later stage. Each community has a unique icon to represent it. 


Communities or viewer card families (or whatever you call them) I have not included here should be removed from the game but not the project. Keep the code logic in case I want to use those again but make sure all viewers have either at least one of the above communities or None. 

When the player hovers over a community icon there should be an information popup telling them what that community does and how it scales. 
