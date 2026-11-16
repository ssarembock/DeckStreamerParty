I want you to read this document as if you are a highly experienced, world class game developer and expert in unity. I want you to implement the required changes and then produce a guidelines document for me which covers the key changes made. 

# Single Player

I want you to add two additional single player options . When the player clicks on the single player from the main menu they should be presented with 3 buttons - 1 for the puzzles (as it currently is), 1 for endless mode and 1 to play against an AI (for now implement the simplest possible AI that can play the game using mostly random moves and random round stopping). 

For the endless mode we need to add 2 additional buttons to the shop at the top above the viewer cards (next to the other shop buttons at the top) - they both cost 2 trophy to start and increase in price by 1 after every use. The one button adds an additional round/stream to the endless mode and the other allows the player to add an additional viewer card to the shop that is not currently for sale in the selection available. I will explain the endless mode in more detail now and the differences there. 

## Endless Mode

The endless mode is a mode where the players start with a set number of rounds (25) and they see how many rounds they can last for. In this mode , the objective of the game is not to get 4 trophies and win, but rather to just last as many rounds as possible. In this version of the game, the trophies are not out of the winning value in the Menu Bar (I mean they shouldn’t read X/4) but should rather accumulate like the other resources do (only in this endless mode). Then the trophies can be “spent” on either increasing the number of rounds from 25 up or be spent to bring additional viewer cards into the shop. This mechanic only applies to the endless mode and the usual trophy win condition should still apply to the other modes. Allow me to modify the properties of these UI elements in the UISkin

## VS AI Mode

For this mode, the player should select their streamer and then select a streamer for their opponent and they should be able to play against a very simple and rudimentary AI. Just make the simplest possible AI that can play the game and doesn’t just end every round immediately. 

 
# Multiplayer Updates

For the multiplayer I want to include 2 additional modes. 2v2 and 2 person CO-OP endless mode. The 2 person co-op will essentially allow 2 players to play together in the endless mode and see how many rounds they can survive together. 

For 2 person (either for 2v2 multiplayer or for 2 person co-op) , both players share resources and score their turns together (the round only ends when both players end the stream or if the timer rounds out). Each player has an individual deck/cap , but they share resources such as cash, influence. Visually add a vertical separator to the stream when in 2v2 mode and have the only player occupy the left side and the other player occupy the right side. When scoring, using actives or considering the communities - both streams are considered as one. The only separation between the two players in a team is they have individual audiences and caps. They score their streams and share the resources they gain and can use viewer actives on one each others viewers in stream. 


Any new buttons or menus added during this change should be customizable in the UISkin


