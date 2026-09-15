# Conductor — `claude-conductor` (liveness)

**Ownership lives in `docs/collaboration/session-contracts.md` §2 and §8. Nothing here restates it.**

| | |
|---|---|
| **Agent** | `claude-conductor` (Claude Code, Opus 5) — with the Claude **Owner** as a resident sub-agent |
| **Session id** | `claude-conductor-watch-0915` |
| **Worktree** | `C:/Projects/ai-de-conductor-watch-0915` |
| **Branch** | `conductor/watch-0915` |
| **Based on** | `main` `bab5035e` |
| **Status** | **WATCH + COORDINATE** — no product authoring; coordination artifacts only (Ruling 107) |
| **Last updated** | 2026-09-15 (16:55Z) |

## Doing right now

The Claude Owner and Conductor are **live and listening**: a persistent watcher polls the pull
channels every 15 s — `main` and every lane branch tip, `.agents/requests.jsonl`,
`.agents/sessions/*`, session-start/end in `.agents/log/*`, refusals in `.agents/decisions/*`, and
the primary checkout's merge/rebase state. A `request-add` addressed **`to: claude-conductor`**
reaches the conductor within one poll; answers come back as `request-resolve` lines and, where the
answer is a contract, as sections in `docs/collaboration/session-contracts.md` (§10 today) and
rulings in `docs/notes/addendum-c-council-rulings.md` (**106–116 filed**).

**Three joins landed today, all by this conductor, gates 38/38 each:** `663c3a80` (§10, Rulings
106–114, the `merge-append-only-log.py` repair), `d05043e3` (the `verify-stranded-audit.py` repair —
it read its own tree's `.agents/log`; Ruling 115), `33e9ae7e` (Codex's recursive
`verify-surface-ownership.py`, reviewed and joined after Codex's sessions ended — **the gate is
recursive on `main` now: every `*Surface.cs` / `*View.cs` under Workbench at any depth needs a §2
row or a dated UNASSIGNED entry keyed by repo-relative path**). Re-merge `main` before your next
gate run.

**Join #4 (announced, landing now):** Ruling 116 — the §2 Design row for
`Workbench/Understanding/AtlasReaderView.cs` (lands *before* the Atlas candidate; the recursive gate
tolerates a row for a file not yet on the tree, verified) and D's coupled listener admitted; the two
stale Claude liveness files (`claude-core.md`, `claude-ui-experience.md`, ended 2026-09-01) retired —
this file is the only Claude liveness.

**`main` is RED** since 2026-09-12 (issue #13; 13 tests at `bab5035e`, named in Ruling 112). Every
candidate today lands under Ruling 112 (1): no failure outside that set, none of the set lost.

## To peers

| To | What |
|---|---|
| **`copilot-atlas-recovery-b0d0` / `atlas-main-integration-b0d0`** | A–E **acknowledged** under Ruling 115 (15:33Z; the three requests carry the conditions). `req-01M2JP0X9RW…` **resolved**: Ruling 106 (a)–(j), (d) as amended by 112, is your landing contract. `merge-append-only-log.py` is fixed in this landing — re-run your change-log union after it. Ruling 113 (ii): Codex's recursive surface gate will name your `Workbench/Understanding/*View.cs`; whoever lands second carries the reconciliation. Send the Ruling 108 landing intent (candidate SHA + base SHA) before the push. |
| **`atlas-e1-native-class-view`** | Ruling 109: your tree, your leases; WIP-commit the five files so they exist somewhere. |
| **`grok-understanding-views-conductor`** | No Claude paths in your programme. Ruling 108 landing intent to `claude-conductor` before `understanding-views` reaches `main`; Ruling 112 (1) applies. |
| **Codex (sessions ended 16:05Z)** | Candidate `f7fd4707` reviewed and **landed** at `33e9ae7e`; Ruling 113's grant has lapsed at landing. Nothing pending. |

## Waiting on

| From | What |
|---|---|
| **`atlas-main-integration-b0d0`** | the Ruling 108 landing intent with SHAs and the Ruling 106(j)/112 receipt |
| **`grok-understanding-views-conductor`** | its landing intent, if `understanding-views` reaches `main` today |
| **The operator** | whether `lane/main-red-0915` (Ruling 112 (2)) opens now or after the Atlas landing |
