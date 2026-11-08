I want you to read this document and implement the required changes. I want you to read it and implement it as if you are a world class, highly experienced game developer with expertise in unity. When anything is unclear or left as a design option, I leave it to your better judgement - but flag these decisions at the end of the work. After we are done I want you to write a new short instructions and guidelines file for the recent changes

# Updates

## Free For All View Other Players

In the Free For All Mode, when players are taking their turns simultaneously, they should be able to click on each others icons on the Players panel on the left and it should “switch to their view”, this should show them a similar view to when the players are not active during a turn-based 1 on 1 multiplayer game. So when the players click on each others icons on the left UI panel, they should switch views to the other players view of the round and they should be able to watch what they are currently doing. A player clicking on their own icon on that UI panel should bring them back to their own view and restore control of actions in the game. Obviously a player viewing another players stream should not be able to impact it in any way and controls of game actions should only restore when the player clicks back to their own view. The viewing of other players works well in the turn-based mode (when you are not the active player but watching the other players turn). So use the same approach when viewing other players streams in free for all mode. 

#Timers  and Timers UI

The countdown timers should be an icon for a hourglass (I will supply the icon) and just a number representing the remaining time. (I’ve seen LEN added before the timer value - remove this). Allow me to add a sprite for this icon in the database editor. If a player is asked to ban someone from their audience at the end of the round, if the timer runs out and no selection is made then a random selection is made for the player. In the situations where a player is asked to ban one of their viewers, a viewer MUST be banned. Also there should be separate timers for the stream part and the shop part. Currently they are combined into one round timer, but I want a separate timer for each. 

## End of Round Stats

The UI popup that shows the players the end of round summary should be combined with the ready panel. Instead of having the popup (when the player has finished their stream) , the information from the stream end popup should be integrated into the Waiting for Other Players main UI panel. When all players have finished their stream, the stream summary columns from the UI popup should be added to the main UI and only then should the Ready for Shop button become available. So what I want is that in FFA, when a player ends their stream the UI shows “Waiting for Other Streamers” and when all players end their turn, instead of the UI popup we just add the stream summary to the main UI panel and only then show the button to click ready for the shop.  





# Shop UI Changes

I want the panel on the left in the shop that houses the button to expand the audience cap, the streamers actives ,  and the ready for next round button should be removed and each of these buttons should just be added to the UI panel where viewer cards are purchased in the shop. Everything that can be purchased in the shop (and the ready button) should be in one UI panel. 

Add a button in the shop that costs money but allows the player to permanently remove a viewer from their audience collection. It should start at costing 2 cash but should increase by 1 per use similar to the others (actives and cap increase)

## Shop Recap UI

I want you to get rid of the shop recap summary popup in the multiplayer UI and move that to a panel on the bottom left of the stream UI instead. Currently that space is empty in the multiplayer UI and so I want you to add a UI panel with the shop summary UI instead and then have it fade away after 10 seconds. 

## Emotes Limit in Multiplayer

Set some reasonable limits to the use of Emotes in the multiplayer. Prevent players from spamming the emotes. Also add a mute button to each player in the Players section. If a player mutes another player they will not see their emotes. 

## Scoring Icons
Currently when scoring, the icons representing the different resources gained when scoring fly up to the top of the screen to the UI icon for the clout. Make each resource fly up seperately to the appropriate icon in the UI panel above. The cash should fly up towards the cash icon and clout should fly up towards the clout icon, and the plays should fly up towards the play icons etc. 

## Replace the name of Plays with Trophies
I want you to change the name of the Plays score to Trophies. This is just a name change. Instead of a player requiring Plays to win , they now require Trophies. 

## Players list should expand, not scroll. 

When there are multiple players in the stream, currently the UI panel on the top left (that shows all the players) has a scroller that allows the player to scroll up and down. Rather remove this and expand the UI panel so that all players can be shown instead. 

## Remove Info From Players Panel 
Remove Stream Number and Status information from this panel . The viewers with plays with should be replaced with an icon (which I will provide in the database) and just a number representing the value next to that icon. Also remove the best from the UI panel. That information is not required. 

## Community Information
Currently the only place the player can see information on their communities is in the stream UI panel for the community beats. Add additional information in the UI panel for the audience overview about the current communities the player has in their audience collection. 

## Viewer Actives Change
Can you make it that when a viewer in chat has an active, that the active can be used by clicking anywhere on the viewers icon in chat (not just on the button on the border). 

## Communities Scores
Community Scores should only start at 2. Having a community score of 1 is not a community. Only activate and show the community effects when then the community score is 2 or above and also subtract 1 from all the community scores. So having 2 different viewers from the same community should set that community score to 1. Communities have no effect if there are fewer than 2 distinct viewer cards in chat from a given community. 
