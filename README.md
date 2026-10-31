# Deck Party

A streaming- and meme-themed party-building card game for 1 to 6 players, based on the Party House rules in [Party_House_UFO50_Complete_Rules.md](Party_House_UFO50_Complete_Rules.md). Single player and online multiplayer (Unity Relay join codes or direct IP).

## Requirements

- Unity **6000.0.73f1**
- Packages (installed automatically from `Packages/manifest.json`): Netcode for GameObjects 2.13.3, Unity Transport 2.7.4, Multiplayer Services 2.4.1, Multiplayer Play Mode 1.6.3

## Opening the project

1. Clone the repo and add the folder in Unity Hub (**Add > Add project from disk**).
2. Open it with Unity 6000.0.73f1. The first import rebuilds the `Library` folder and takes a few minutes.
3. Open `Assets/DeckParty/Scenes/MainMenu.unity` and press Play.

Online play needs the project linked to a Unity Cloud project with Relay enabled. See [V1Instructions.md](V1Instructions.md) section 6A.

## Docs

- [INSTRUCTIONS.md](INSTRUCTIONS.md): project setup, architecture and code map
- [V1Instructions.md](V1Instructions.md): art pipeline, testing, making a build and playing online with friends
