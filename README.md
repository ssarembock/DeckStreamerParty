# Deck Party (Audience Capture)

A streaming- and meme-themed party-building card game for 1 to 6 players, based on the Party House rules in [Party_House_UFO50_Complete_Rules.md](Party_House_UFO50_Complete_Rules.md).

Players see it as **Audience Capture**:
- Pick a streamer with a unique power.
- Grow an Audience of 59 meme viewers.
- Get 4 Diamonds into one stream before the season ends.

It has single player and online multiplayer (Unity Relay join codes or direct IP), with live presence, emotes and an optional turn timer. It's in English, 简体中文, Русский, Español (Latinoamérica) and Português (Brasil).

## Requirements

- Unity **6000.0.73f1**
- Packages (installed automatically from `Packages/manifest.json`): Netcode for GameObjects 2.13.3, Unity Transport 2.7.4, Multiplayer Services 2.4.1, Multiplayer Play Mode 1.6.3, Localization 1.5.8
- Asset Store packages included in `Assets/`: Feel, Sprite Shaders Ultimate

## Opening the project

1. Clone the repo and add the folder in Unity Hub (**Add > Add project from disk**).
2. Open it with Unity 6000.0.73f1. The first import rebuilds the `Library` folder and takes a few minutes.
3. If the scenes haven't been generated yet, accept the **Build now** prompt (or run **Deck Party > Setup > Build Everything**).
4. Open `Assets/DeckParty/Scenes/MainMenu.unity` and press Play.

Online play needs the project linked to a Unity Cloud project with Relay enabled. See [V1Instructions.md](V1Instructions.md) section 6A.

## Docs

- [INSTRUCTIONS.md](INSTRUCTIONS.md): project setup, architecture, multiplayer and code map
- [V1Instructions.md](V1Instructions.md): art pipeline, making a build and playing online with friends
- [V2Instructions.md](V2Instructions.md): the Audience Capture update (streamers, languages, animation, polish) and its test passes
- [UIGuide.md](UIGuide.md): where to change every part of the UI, text, effects and display
