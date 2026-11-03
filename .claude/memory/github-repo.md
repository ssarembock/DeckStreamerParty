---
name: github-repo
description: "Deck Party project is in git at github.com/ssarembock/DeckStreamerParty (branch main), pushed 2026-10-31"
metadata:
  node_type: memory
  type: reference
  originSessionId: aba468d4-1e36-4626-84c9-3928edc68041
  modified: 2026-11-03T14:01:18.574Z
---

The Unity project at C:\Users\ssare\DeckParty is a git repo pushed to https://github.com/ssarembock/DeckStreamerParty.git, branch `main` (initial commit b6b1226 on 2026-10-31). GitHub credentials are saved in Git Credential Manager, so pushes need no sign-in.

The .gitignore excludes Library/Temp/Logs/UserSettings, the `Builds/` and `FirstTest/` build folders, generated .csproj/.slnx, .vscode and `.claude/settings.local.json`. Empty asset folders (Art/Backgrounds, Art/UI, Audio/Music, Audio/SFX) hold a `.gitkeep` so their .meta files aren't orphaned on clone. No Git LFS (project ~24 MB); revisit if large art or audio is added.

The repo is **private**, so the Asset Store packages (Assets/Feel, Assets/Sprite Shaders Ultimate, demos deleted) and the OFL fonts in Assets/DeckParty/Fonts may be committed (user confirmed 2026-10-31). They were committed with the V2 + V2.1 work in 86ee408 (pushed 2026-11-01; ~3,870 files, none over 25 MB, still no LFS needed).

Large pushes over HTTPS failed with "send-pack: unexpected disconnect while reading sideband packet" (while git falsely printed "Everything up-to-date"); setting repo-local `http.postBuffer 524288000` fixed it (2026-11-02, V4/V4.2 commit 5107c87). Always verify with `git status -sb` after pushing.

Two PCs (2026-11-03): the user works on this project on this PC and another, sharing Claude context through the repo. Root `CLAUDE.md` imports the memory copies in `.claude/memory/` (keep those in step with this folder: write every memory change to both). `.claude/sync-claude.ps1 -Save` copies memory and gzipped transcripts into `.claude/memory` and `.claude/sessions` (commit + push); `-Load` on the other PC restores them into `~/.claude/projects/<path slug>/` for `claude --resume`. Shared permissions are in committed `.claude/settings.json`; `.claude/settings.local.json` stays per-PC. The compile check lives in the repo at `.claude/tools/compile/build.sh` (needs Unity 6000.0.73f1 at the default Hub path and the project opened once so Library/ScriptAssemblies exists).

Only commit or push when the user asks; for the V2 polish the user said "Ask me but yes" (ask first, expect approval). Related: [[deck-party-project]].
