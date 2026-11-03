---
name: user-tests-in-unity
description: User tests Deck Party themselves in the Unity editor; no automated tests from Claude; project-related work anywhere on the PC is pre-approved
metadata:
  node_type: memory
  type: feedback
  originSessionId: aba468d4-1e36-4626-84c9-3928edc68041
  modified: 2026-10-30T14:49:36.801Z
---

The user does all testing directly in Unity and asked Claude not to run automated tests. Changes inside the DeckParty project need no permission. On 2026-10-30 they also pre-approved any command on their PC that relates to the project (reading the Unity install, package cache, NuGet cache, dotnet builds in the scratchpad, checking things on the machine). Changes to anything unrelated to the project still need explicit permission first.

**Why:** stated when commissioning the Deck Party build (2026-10-29); they want to own verification in the editor. On 2026-10-30 they asked for broad permissions so a large prompt can run unattended for an hour with no prompts.

**How to apply:** don't write/run test suites or launch Unity. Compile-only type checks against Unity's DLLs in the scratchpad are fine as static checks, never called "testing". During long runs, don't block on AskUserQuestion: pick the sensible default, note it, and keep going. Project `.claude/settings.local.json` allows PowerShell/Bash/Read/Edit(project + scratchpad), with acceptEdits and a 5m question timeout. Avoid `cd` inside compound shell commands (it can still trigger a prompt), and don't edit `.claude/settings*.json` mid-run (protected path, prompts). See [[deck-party-project]].
