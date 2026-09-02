---
id: "note-20260902-collaboration-not-happening"
title: "Collaboration is not happening end-to-end: agents share one worktree and nothing posts to the board/ledger"
type: decision-note
status: draft
owner: "@timianmalloo"
phase: "watcher"
tags: [decision-note, watcher, loomkeeper, collaboration, worktree, coordination]
links:
  - { to: note-20260902-session-enlistment-telemetry-gap, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: 2027-03-01
review-suggested: []
summary: >-
  From the 2026-09-02 15:28 recording: harness identity + Verified trust now work and the Sessions
  surface leads with the live agents — but all three agents launch in the SAME worktree
  (TheTerrace/docs/fix-broken-design-links), causing conflicts, and nothing is posted to the Board or
  Ledger, so no actual collaboration occurs. The surfaces are Design's and are working; the missing
  pieces — per-session worktree isolation on launch, and an agent→board posting path — are the
  coordination substrate (Core's feature/agent-watcher-substrate).
---

# Collaboration is not happening end-to-end — the substrate, not the surface

*A decision note (`knowledge-visualization.md` V17). Written to give the Core
`feature/agent-watcher-substrate` session a precise, evidence-grounded diagnosis from the user's
2026-09-02 15:28 recording, and to record the Design/Core boundary so effort lands in the right place.*

- **Kind:** resolved-question (diagnosis)
- **Confidence:** Verified (observed directly in the recording); the *fixes* are Inferred (the launch
  and posting paths were not traced to source in this note — Core should confirm before acting, E15)
- **Made during:** a Design-session `/ui-design`-adjacent investigation of the user's report *"I am not
  convinced collab is happening… lots of conflicts as all three sessions kicked off in the same work
  tree and no collab and no updates in the board or ledger."*

## What is now WORKING (progress — do not re-fix)
Observed in the recording's Sessions surface:
- **Harness identity is recorded.** `GitHub Copilot` (`github-copilot`) and `Claude Code`
  (`claude-code`) show their harness, not "Not Recorded". The enlistment note's harness path is landing.
- **Trust is Verified.** Both agent sessions read `trust Verified` (not `Asserted`) — the OSC-133 /
  readiness evidence is being received for agent sessions.
- **The Sessions surface leads with the live agents.** The three `✓ Alive` sessions (Copilot, a pwsh
  terminal, Claude Code) sort to the top (the Design live-first elevate). The stale/ended history
  follows/collapses.

## What is BROKEN (the collaboration gaps)

### 1. Every agent launches in the SAME worktree → conflicts (the user's headline complaint)
All three `✓ Alive` sessions share `TheTerrace/docs/fix-broken-design-links`, and the user also ran
`claude` manually from the app's pwsh terminal in `C:\Projects\TheTerrace` (the primary checkout). So
multiple writing agents operate in **one working directory**, which is exactly the failure
`session-worktree-discipline.md` (**WT1**) exists to prevent: "two agents in one checkout share an
index, a HEAD and one set of generated artifacts… a stash in one silently reaches into the other's
uncommitted work and nothing fails loudly." The conflicts the user saw are the predictable result.
- **Gap:** the AI-DE app's "New Claude Code session" / "New GitHub Copilot session" launches the agent
  in the workspace root (or the current worktree), it does **not** create and enter a **per-session
  worktree**. The pack already has the mechanism — `coord worktree new --branch … --session …`
  (`coord-core.py`) — but the app does not call it at launch.
- **Owner:** Core (the agent-launch path — `TerminalSurface` / the launch command / `AgentEnvironmentFor`),
  because launch + coordination is the substrate. Design owns none of this.
- **Suggested shape:** on "New <agent> session", create a worktree (`coord worktree new`), set the
  agent terminal's working directory to it, and bind the session to that worktree identity (the
  `WorktreeIdentity` already exists in `SessionBinding`). Clean it up on session end (WT6–WT9).

### 2. Nothing posts to the Board or Ledger → no observable collaboration
Despite three live agents (and the user explicitly asking Copilot to *"send a message to the loomkeeper
board to let the other agents know you are here"*), the **Board reads empty** ("No board posts yet.")
and the **Ledger reads empty**. So even the sessions that ARE enlisted are not *coordinating*.
- **Gap:** there is no working **agent → board** posting path. The Board surface reads
  `IWatcherObservationStore.AllBoardMessages()`; an empty board means nothing wrote a board message.
  The agents have no tool/among their MCP servers that posts to the Loomkeeper board, or the path
  exists but is not wired to the store the surface reads. (In the recording, Copilot went looking:
  *"I found coord-core.py… I want to check if it has the messaging or board functionality that
  'loomkeeper board' might refer to"* — i.e. the agent did not have a board-post affordance.)
- **Owner:** Core (the coordination substrate + the agent-facing tool/MCP that writes board/ledger
  entries). Design owns the *rendering* of those entries (which works — the Ledger and Board surfaces
  display correctly when data exists, proven by their tests).
- **Suggested shape:** an agent-callable path (MCP tool or `coord-core.py` command the harness invokes)
  that appends a `BoardMessage` / `WorkEpisode` to the **same** `IWatcherObservationStore` the app's
  watcher host reads, signed with the session nonce so it lands as `Verified`.

### 3. Telemetry is still partial: model + spans
Every session still reads `Not Recorded` for the **model** and `0 span(s)`. Harness + trust landed;
model identity and observed spans have not. This is the remainder of the enlistment note's gap.
- **Owner:** Core (the harness telemetry that reports the model, and the span emission).

## The Design/Core boundary (so effort lands right)
- **Design (this session) — done / working:** the Sessions surface leads with live agents and collapses
  the inactive history; the Board/Ledger/Sessions render honestly with teaching empty states. These
  compose with the substrate: the moment worktree isolation, board posts and model/spans land, the
  surfaces already show them well.
- **Core (`feature/agent-watcher-substrate`) — the actual collaboration fix:** (1) per-session worktree
  on launch (WT1), (2) an agent→board/ledger posting path into the store the surface reads, (3) model +
  span telemetry.

## Validation condition
Holds until: a freshly launched agent session runs in **its own worktree** (distinct
`WorktreeIdentity`), a message the user asks an agent to post **appears on the Board**, and a live
session reads `Verified · N span(s) · Alive` with its **model**. When all three are observed in one
run, collaboration is happening end-to-end and this note retires.

## Promotion rule
If closing this becomes a multi-part substrate change (launch choreography + coordination protocol +
telemetry), it earns an ADR; write it, link `supersedes` this note, set this note `superseded`.
