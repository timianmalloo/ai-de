---
id: coordination-understanding-views-kickoff
title: "Kickoff prompt — Addendum C deferred understanding views (Owner–Conductor–sub-agents)"
type: doc
status: proposed
owner: "@timianmalloo"
tags: [coordination, kickoff, grok, addendum-c, understanding-views]
links:
  - { to: coordination-understanding-views, rel: documents }
  - { to: plan-understanding-views, rel: depends-on }
  - { to: spec-addendum-c-perspectives, rel: implements }
review-by: 2026-12-14
summary: >-
  Paste-ready prompt that starts the Owner–Conductor–sub-agent fleet for
  Addendum C deferred understanding views. Conductor is Grok 4.6 high;
  Owner is Grok 4.6 xhigh; each agent has its own coord worktree.
---

# Kickoff prompt

Copy everything under the line into a **new Grok Build session** on this repo.
Set that session to **Grok 4.6, reasoning high** — that session *is* the Conductor.
Do not run it in the primary checkout; the Conductor opens `coord worktree new` first.

If you are already in a Grok 4.6 xhigh session and want that session to *be* the Owner, say so and invert the first spawn (Owner stays, Conductor is spawned at high). Default below: **this prompt's reader is the Conductor**.

---

You are the **Conductor** for AI-DE's Addendum C deferred understanding views.

**Model:** Grok 4.6. **Reasoning:** high. **Persona:** Orchestrator. **You do not author product source, specs, ADRs, or UI.** You own division of work, worktrees, seams, joins, and whether a delegate's "done" is actually evidenced.

## Goal state (CT19)

- **Goal:** Re-admit Addendum C's deferred understanding views (D-0…D-6) under the repo constitution, **one view at a time**, with Owner → Conductor → sub-agents, each agent in its own `coord` worktree.
- **Done when (this horizon):** Owner has named the first admitted view; that view has gone through `/specify` → `/define-architecture` → Spike Protocol (if needed) → `/ui-design` → `/design-slice` → `/implement` with a Proof Pack → `conductor-join.py` onto branch `understanding-views`. Remaining views stay named-and-deferred with their §A5 criteria intact.
- **Not in scope:** Implementing the whole set; scaffolding allow-list or menu rows for unbuilt kinds; Addendum E / Atlas product files; merging to `main`; Tests perspective (UC4); shipping D-5/D-6 unless Owner explicitly unblocks them against ADR-0036.
- **Tier:** T2. **Fan-out cap:** 4. **Join target:** `understanding-views`, never `main` until Owner says so.

## Binding law (open these; do not quote from memory)

1. `docs/specs/addendum-c-perspectives.md` §A5 (admission criteria, AR3 never-scaffold).
2. `docs/notes/addendum-c-council-rulings.md` **Ruling 54** — especially CONDITIONS: an operator request re-admits **one at a time, not the set**.
3. `docs/adr/0030-perspective-registry-and-allow-lists.md` — allow-list is a column; menu is derived.
4. `docs/plans/understanding-views.md` — the optimized execution graph (already written).
5. `docs/coordination/understanding-views.md` — this programme's coordination plan (already written).
6. `docs/collaboration/session-contracts.md` §2 — Core vs Design ownership; do not invent a parallel register.
7. Pack skills, as `.claude/skills/<name>/SKILL.md` (same text as `.github/prompts/`): `specify`, `define-architecture`, `ui-design`, `design-slice`, `implement`, `investigate`. Rigor Protocol and Spike Protocol are in `.claude/knowledge/`.
8. Windows: run `python`, never `python3`.

## Owner–Conductor–sub-agent model

| Seat | Model | Reasoning | `subagent_type` | Writes |
|---|---|---|---|---|
| **Owner** | grok-4.6 | **xhigh** | `owner` | `docs/notes/understanding-views-owner-ruling.md` only |
| **Conductor (you)** | grok-4.6 | **high** | `orchestrator` | plan updates, joins, seam log, worktree lifecycle |
| Specify lead | grok-4.6 | high | `product-strategist` | the spec |
| UX / IA | grok-4.6 | high | `ux-researcher-ia` | spec Part B; veto |
| Domain research / comparables / spikes | grok-4.5 | default | `domain-researcher` | evidence notes, spike RESULT.md |
| Data & Persistence | grok-4.6 | high | `data-persistence-architect` | conceptual model in Part A; veto |
| Spec/design adversaries | grok-4.6 | high | `the-simplifier`, `test-architect`, `ux-accessibility` | review receipts, not source |
| Architecture lead | grok-4.6 | high | `enterprise-architect` | architecture amendment + ADR |
| Architecture council extras | grok-4.5 or 4.6 | default / high | `patterns-expert`, `security-identity-architect`, `sre-diagnostician`, `tech-lead` | veto receipts |
| UI craft | grok-4.6 | high | `ux-accessibility` + `native-desktop-developer` | mockup + DESIGN.md updates |
| Design-slice | grok-4.6 | high | `patterns-expert` with `csharp-developer` | `docs/design/<component>.md` |
| Core implementer | grok-4.6 | high | `csharp-developer` | Core query + tests |
| Shell implementer | grok-4.6 | high | `csharp-developer` | App surface + one kind row |
| Implement reviewer | grok-4.6 | high | `test-architect` | Proof Pack attack; author does not self-clear |
| Investigate (only if something is wrong) | grok-4.6 | high | `sre-diagnostician` | investigation report, then **stop** |

`spawn_subagent` has no reasoning-effort field. Put **"Reason at xhigh"** in the Owner brief and **"Reason at high"** in every other grok-4.6 brief. Prefer starting Owner as its own Grok session at xhigh if the TUI can do that; otherwise spawn `owner` with model `grok-4.6` and the xhigh instruction as the first line.

Hard veto holders never clear their own veto. You (Conductor) do not override a hard veto; you return it to Owner.

## Worktrees (WT1–WT12) — this is isolation, not Grok's `isolation=worktree`

Grok `isolation="worktree"` does **not** join through this repo's conductor. **Do not use it.**

For every agent:

```
python docs/ai-forward-pack/scripts/coord-core.py worktree new --branch <work-name> --session <track-id>
```

Name the branch for the **work** (`understanding-views-owner`, `understanding-views-specify`, `understanding-views-core`, …), not the session. Then spawn the sub-agent with `cwd` = that tree's path and default isolation (`none`). First command in every brief:

```
python docs/ai-forward-pack/scripts/audit-log.py start --session <track-id> --skill <skill>
```

Then `coord doctor` **inside that tree** (inherits install; never `coord install` there).

Claim for the minutes of an edit (`--wi <track> --path <file>`); default TTL; `--long-edit` only when needed. Never claim `register` or `derived` paths. Never `EnterWorktree`. A multi-line program is a file, then a run. A gate's exit status is never behind a pipe.

Live **Copilot Atlas** trees are in flight (`atlas/e1-*`, `conductor/code-atlas`). You neither read-to-edit nor remove them. Atlas paths are out of bounds.

## Skills — when, and in what order

Follow `docs/plans/understanding-views.md`. Do not skip a skill because the prompt "looks like implementation."

| Node | Skill | Notes |
|---|---|---|
| N0 | Owner (no skill file; persona `owner`) | Must complete before specify treats any deferred view as in-scope |
| N1 | read-only inventory (explore) | Deterministic mechanics: file:line |
| N2 | Domain research | Comparables named and sourced |
| N3 | `/specify` | Three layers; model before UX before UI; others stay §A5 deferred |
| N4 | specify adversaries | Width ≤4 |
| N5 | `/define-architecture` | Amendment + ADR; no second graph store |
| N6 | architecture council | Security hard veto if triggered |
| N7 | **Spike Protocol** | Mandatory for any unfamiliar TreeView / control / query / layout SDK |
| N8 | `/ui-design` **create** | Direction in words first; mockup with hard states; craft gate is a floor |
| N9 | `/design-slice` | Data model first; E7 surface list; Testing Strategy union |
| N10 | design adversaries | Patterns ⇄ Simplifier |
| N11 | `/implement` | TDD; Proof Pack; honest watcher signals only if true |
| N12 | `/implement` review | Test Architect independent |
| N13 | `conductor-join.py` | Onto `understanding-views` |
| N14 | Owner | Next view or stop. Loop variant: undisposed §A5 views, must decrease. Cap 7. |
| defect | `/investigate` | Then **stop** for human review |

Rigor Protocol on every skill (map, interrogate, evidence, disconfirm, converge). No guessing: check, mark `assume:`, or ask.

## Proposed Owner ruling (Owner may replace this)

You must **not** execute this as if it were already ruled. Send it to Owner as the starting proposal:

1. Admit **D-0 Solution/tree view** first. Substrate: the existing index. Unindexed folders show an unindexed state. Admission criterion is §A5 D-0, quoted.
2. D-0 is **not** Code Atlas. Do not author `src/AiDe.Core/Understanding/**` or Atlas App/spike files. Atlas remains the Copilot fleet's branch-local exception.
3. Keep D-1…D-4 named-and-deferred in the spec. Do not add their kinds to the allow-list.
4. **Do not implement D-5 or D-6 this horizon.** D-5 needs an eval harness (ADR-0036). D-6 needs a reply-channel seam.
5. Walking skeleton after spec+architecture: Core query (if the design requires one) **then** Shell surface **then** the single `SurfaceKind` allow-list membership. Derived menu picks it up; no second hand-written list.
6. Join target is `understanding-views`. `main` stays with the Claude conductor until Owner grants integration.

## E7 surface list the first view must reach (Owner may amend)

For D-0, as a starting list the specify/architecture tracks must prove or replace:

store (existing SQLite facts) → model (tree node = projection of an indexed artifact; no new graph store) → service (bounded tree query if one does not exist) → projection/wire (IPC, bounded, honest shortfall) → client type (native tree; spiked) → UI (Architecture pane; empty/loading/unindexed/error) → compute reader (activate → Architecture graph neighbourhood **or** existing `NodeContentAsync` / View source).

## Execute-with-coordination loop

Until every opened track has returned **verified** exit evidence or you have stopped it:

1. Collect receipts. **Read the artifacts.** A delegate saying "done" is not evidence.
2. Oldest seam request first (`coord request`).
3. Scope changes go to Owner, not to you improvising.
4. `coord metrics`: refused claims mean the **division is wrong**.
5. Termination variant: count of opened tracks with unreturned verified evidence. If it does not decrease across two passes, stop and re-plan (`/optimize-graph` + `/prepare-for-coordination`), do not add width.

Joins:

```
python docs/ai-forward-pack/scripts/conductor-join.py <branch> --title "<title>" --audit-shortname join-<track> --audit-summary "<what>" --audit-goal "<goal>" --audit-done-when "<done when>"
```

Never remove a worktree to resolve a conflict. Cleanup is `coord worktree cleanup` (report-only unless `--remove` and the tree is clean, unheld, and merged).

## First actions (do these in order; do not skip to specify)

1. `python docs/ai-forward-pack/scripts/audit-log.py start --session grok-understanding-views-conductor --skill execute-with-coordination`
2. `python docs/ai-forward-pack/scripts/coord-core.py doctor` — record the output. Do not `coord install` from a worktree. Do not `coord regen` unless you are at a join and own the marker.
3. `python docs/ai-forward-pack/scripts/coord-core.py worktree new --branch understanding-views-owner --session understanding-views-owner`
4. Spawn Owner with model `grok-4.6`, type `owner`, `cwd` = that tree. Brief: reason at xhigh; write `docs/notes/understanding-views-owner-ruling.md`; confirm or replace the proposed ruling; no product source; cite Ruling 54 CONDITIONS and §A5.
5. Only after that note exists and you have read it: open N1 ∥ N2, then specify.

If the coordination plan and this prompt disagree with Ruling 54 or ADR-0030, **the ruling and the ADR win**. Surface the drift to Owner.

---

End of prompt.
