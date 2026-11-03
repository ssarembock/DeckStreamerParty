# Deck Party / Audience Capture

A Unity 6000.0.73f1 game (Party House rules re-themed to streaming), single player and multiplayer (NGO 2.13.3 + Relay).
The player-facing title is **Audience Capture**. The user works on it from two PCs, so the project's Claude context
lives in this repo.

## Shared context (both PCs)

The project memory is kept in `.claude/memory/` and imported here, so every session on either PC starts with it:

@.claude/memory/user-tests-in-unity.md
@.claude/memory/github-repo.md
@.claude/memory/deck-party-project.md

- **Keep `.claude/memory/` current.** Whenever you save or update a memory for this project, also write the same file
  (and its line in `.claude/memory/MEMORY.md`) here in the repo, so the other PC gets it with the next pull.
- **Session transcripts** travel through `.claude/sessions/` (gzipped). Run `.claude/sync-claude.ps1 -Save` before
  committing when the user is switching PCs. After pulling on the other PC, run `-Load` once; then `claude --resume`
  (or the VS Code session list) shows the earlier sessions.
- Shared permissions are in `.claude/settings.json`; `.claude/settings.local.json` is per-PC and not committed.

## Working on the project

- Build the scenes and prefabs with **Deck Party > Setup > Rebuild Scenes and UI Prefabs** (the user runs it in Unity).
- Compile check, no tests: `bash .claude/tools/compile/build.sh`. It needs Unity 6000.0.73f1 at the default Hub path
  (or set `UnityData`) and the project opened in Unity once, so `Library/ScriptAssemblies` exists.
- All player-facing text goes through `Assets/DeckParty/Resources/Localization/AudienceCapture_Strings.csv`
  (5 languages). Never name the real streaming sites the platforms parody.
- Guides for the user live in the repo root (`INSTRUCTIONS.md`, `UIGuide.md`, `V1`-`V4Instructions.md`,
  `NewUIGuidelines.md`).
