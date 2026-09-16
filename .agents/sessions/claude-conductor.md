# Conductor — `claude-conductor` (liveness)

**Ownership lives in `docs/collaboration/session-contracts.md` §2 and §8. Nothing here restates it.**

| | |
|---|---|
| **Agent** | `claude-conductor` (Claude Code, Opus 5) — with the Claude **Owner** as a resident sub-agent |
| **Session id** | `claude-conductor-watch-0915` |
| **Worktree** | `C:/Projects/ai-de-conductor-watch-0915` |
| **Branch** | `conductor/watch-0915` |
| **Based on** | `main` `bcf4959b` |
| **Status** | **NOT WATCHING** — GHCP `copilot-main-watch-b0d0` is the watcher. Claude is the Owner/ruling seat on request, no product authoring |
| **Last updated** | 2026-09-16 (21:10Z) |

## Doing right now

**Claude's watcher is OFF** (stopped 2026-09-16 on the operator's instruction). `copilot-main-watch-b0d0`
is the single watcher for `main` and cross-session coordination; do not expect a Claude poll of the
ledger. Claude Code remains the **Owner / ruling seat** for Core- and Design-owned decisions (§2) and
answers when a request is routed to it — but it reads the ledger only when the operator asks, so a
decision that needs the Claude Owner should also be raised through the operator or the watcher.

**Rulings 106–121 are on `main`.** One is drafted and **not filed**: Ruling 122 (the
`DeclaredDeploymentContext` seam for Codex's E2 — admitted as a specified read-only seam, D&P
Architect review gating implementation). `req-01M2KC9CR6…` is therefore still open, along with the
G6 grammar finding and the Ruling 121 spike checkpoint. They are owed by this seat.

**`main` is still RED** (issue #13, since 2026-09-12; the 13 tests are named in Ruling 112 and
diagnosed in `INV-0012`). Ruling 117 holds the repair order; `lane/main-red-0915` has not opened.

## To peers

| To | What |
|---|---|
| **`copilot-main-watch-b0d0`** | You hold the watch. Route to Claude only what needs the Owner ruling (Core/Design §2 decisions, new grants, seam carve-outs); everything else is yours. |
| **Codex / Grok / Atlas** | Branch-local grants under Rulings 113/115/116/119/121 stand as filed; Ruling 108 landing intents still come to `claude-conductor` for a Claude-owned path, and the watcher for everything else. |
