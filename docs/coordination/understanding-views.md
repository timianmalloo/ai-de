---
id: coordination-understanding-views
title: "Coordination plan - Addendum C deferred understanding views"
type: doc
status: proposed
owner: "@timianmalloo"
tags: [coordination, worktrees, parallelism, addendum-c, understanding-views, grok]
links:
  - { to: spec-addendum-c-perspectives, rel: implements }
  - { to: plan-understanding-views, rel: depends-on }
  - { to: architecture, rel: depends-on }
  - { to: adr-0030-perspective-registry-and-allow-lists, rel: depends-on }
  - { to: note-addendum-c-council-rulings, rel: depends-on }
  - { to: spec-uml-erm-surfaces, rel: relates-to }
review-by: 2026-12-14
summary: >-
  Owner rules first; then one serial spine (specify → architecture → spike →
  ui-design → design-slice → implement → join) for a single admitted view.
  Two read-only tracks may run after the ruling. Seven parallel implementers
  and a D-5/D-6 code track are struck. Atlas files stay with the Copilot fleet.
---

# Coordination plan - Addendum C deferred understanding views

Produced by `/prepare-for-coordination` after `/optimize-graph` (`docs/plans/understanding-views.md`), session `grok-understanding-views-kickoff`, worktree `C:\Projects\ai-de-understanding-views`, branch `understanding-views`, base `bab5035e` (`main`). Consumed by `/execute-with-coordination`. Kickoff prompt: `docs/coordination/understanding-views-kickoff.md`.

**Goal state.** Goal: divide the deferred-views programme so each agent has one worktree, one owned authored set, and exit evidence. Done when: this file and its HTML twin are on `understanding-views`, the kickoff prompt is pasteable, the audit entry is written. Not in scope: product source, `main` merge, Atlas files, scaffolding unbuilt kinds. Tier T1. Fan-out 0 for this writing turn.

**The finding that shapes the plan.** Ruling 54 CONDITIONS re-admit **one view at a time, not the set**. AR3 forbids a menu or allow-list row with nothing behind it. D-0…D-4 share the Architecture allow-list column and the derived menu (ADR-0030). D-5/D-6 live in the composer aggregate and are blocked on ADR-0036. Parallel implementers fail GO5. The honest width is **Owner + Conductor + at most two read-only tracks after the ruling + one code spine**.

The 15× orchestrator-worker multiplier (GO6) is stated: extra tracks are paid in tokens, not saved in span. Worktrees here buy **isolation from the live Atlas fleet** and **context hygiene**, not speed.

## Layer state

Measured 2026-09-15 from the primary checkout and this tree. `coord doctor` exit 1 because regeneration is owed; that is a finding, not permission to `coord regen` from this session (the owed marker is per-repository and a worker regen deletes the primary's copy — recorded on the Addenda C/D plan).

| check | result | meaning |
|---|---|---|
| registry | `ok - 11 pattern(s)` | `.agents/artifacts.yml` classified. Verified by `coord doctor`. |
| merge driver | `effective` — `coord-regen`, `coord-register` declared and registered | Inherited by every linked worktree. **Do not `coord install` from a worktree.** |
| regeneration | `8 artifact(s) OWED` | Not cleared here. Join of this plan may regen after authored commits. |
| `pack-doctor` coordination | PASS, 11 patterns | Graph WARN (stale/flagged/orphan nodes) — content health, not a block on this plan. |
| python | `python` not `python3` on this machine | Verified by pack-doctor WARN. All briefs use `python`. |
| Grok edit boundary | **observed-only / unsupported as enforced** | No Grok PreToolUse coord hook measured in this repo. **Commit floor is the control** (pre-commit `coord-core.py precommit`). |
| Grok `spawn_subagent` `cwd` | schema exists | Use `cwd` = the track's `coord` worktree. Do **not** use `isolation=worktree` (Grok isolation does not join through `conductor-join.py`). |
| Grok reasoning `xhigh`/`high` | **Flagged** | `spawn_subagent` has no reasoning-effort field. Put the effort in the brief; start Owner as the session that already runs at xhigh when possible. |
| live sessions | Atlas fleet active; `atlas-e1-legacy-fixture` holds 5 claims | Do not claim Atlas paths. Do not remove Atlas trees. |
| `extensions.worktreeConfig` | unset (inherited) | One install per clone. |
| this tree | `C:\Projects\ai-de-understanding-views` on `understanding-views` | Created with `coord worktree new`; session `grok-understanding-views-kickoff` registered. |

## Artifact classes

| path / pattern | class | mechanism | coordination needed |
|---|---|---|---|
| `docs/specs/understanding-views.md` (and per-view companions) | authored | one specify writer | Yes |
| `docs/architecture.md`, `docs/adr/NNNN-*.md` | authored | one architecture writer | Yes |
| `docs/design/<component>.md`, `docs/mockups/<surface>.*` | authored | ui-design then design-slice, serial | Yes — different nodes, same later join |
| `src/AiDe.Core/Projections/IWorkspaceQueries.cs`, IPC DTOs, Core tests for the new query | authored | core track | Yes |
| `src/AiDe.App/Workbench/**` new surface, `SurfaceContentFactory` kind row, App tests | authored | shell track | Yes |
| `docs/proof/**`, `docs/notes/**` for this programme | authored | the node that produces them | Yes |
| `docs/audit/*.jsonl`, `.agents/log/*.jsonl`, `.agents/decisions/*.jsonl` | register | append-only union | No claim. `audit-log.py` / `coord` writers only. |
| `docs/docs-index.js`, `docs/audit/audit-data.js`, `docs/api/*.md`, `docs/_site/**`, `docs/_meta.json` | derived | generators in `.agents/artifacts.yml` | No. Regen at join. |
| `src/AiDe.Core/Understanding/**`, Atlas App/spike files | authored **by Atlas** | Copilot fleet section-2 exception | **Out of bounds.** Seam request only. |
| `docs/lessons/defect-classes.md` | register (append-only) | both tracks append, nobody rewrites | No claim. |

## Tracks

| track | owns (authored) | depends on | tier | fan-out cap | budget | exit evidence | harness |
|---|---|---|---|---|---|---|---|
| **owner** | `docs/notes/understanding-views-owner-ruling.md` (new) | — | T2 | 0 | 20 calls | One admitted view named; Atlas seam; D-5/D-6 disposition; next-view rule. No product source. | Grok 4.6 **xhigh**, `owner`. `enforced`: none on reasoning. `observed-only`: brief text. |
| **specify** | `docs/specs/understanding-views.md` | owner | T2 | 4 (review panel only) | 50 | Spec with Parts A/B/C; Gherkin that can fail; conceptual model; deferred views still §A5. | Grok 4.6 high, `product-strategist` lead. Reviewers: `ux-researcher-ia`, `test-architect`, `the-simplifier`, `data-persistence-architect`, `ux-accessibility`. Comparables helper: Grok 4.5 `domain-researcher`. |
| **architecture** | amendments to `docs/architecture.md`; one new ADR | specify | T2 | 4 (council) | 40 | ADR for the one new kind; no second graph store; allow-list change described, not implemented. | Grok 4.6 high, `enterprise-architect` + `orchestrator`. Council: `security-identity-architect`, `patterns-expert`, `the-simplifier`, `sre-diagnostician`, `tech-lead`. Spike helper: Grok 4.5 `domain-researcher`. |
| **core-query** | new query/DTO/tests under `src/AiDe.Core/Projections/**`, `src/AiDe.Core/Ipc/**`, `tests/AiDe.Core.Tests/**` **named in the design**; no Atlas paths | architecture + spike | T2 | 0 | 40 | Red-then-green tests for the query; bounded payload; unclassified/unindexed states. | Grok 4.6 high, `csharp-developer` pairing with `test-architect`. |
| **shell-surface** | new Architecture surface + **one** `SurfaceKind` row allow-list membership; App tests; mockup already exists | core-query (data edge) | T2 | 0 | 50 | §A5 admission criterion observed; AR3: the kind exists before the menu shows it; derived menu has the new entry **because** the row exists. | Grok 4.6 high, `native-desktop-developer` + `ux-accessibility` for `/ui-design` then `csharp-developer` for `/implement`. |
| **conductor** | this plan, joins, seam log, worktree lifecycle | all | T1 | 4 | 30 + join | Each join through `conductor-join.py`; planned vs actual appended. Does **not** author track source. | Grok 4.6 **high**, `orchestrator`. |

`core-query` and `shell-surface` are **serial**, not parallel: Shell consumes the query. They are separate tracks because session-contracts §2 splits Core projections from App surfaces. They are **not** opened until architecture and the spike have frozen the contract.

## Serial spine

| item | why it cannot be parallel | who owns it |
|---|---|---|
| Owner ruling | Decision edge: every later node's shape depends on which view is in-scope (GO5b). | owner |
| Specify | Data+decision edge into architecture. | specify |
| Architecture + ADR | Decision edge into spike, design, and which file core/shell may touch. | architecture |
| Spike | Unfamiliar contract. Building on an unspiked TreeView/WebView/query is forbidden. | architecture helper, then conductor releases core/shell |
| Core query | Shell has nothing honest to bind. | core-query |
| Shell surface + allow-list row | AR3: the kind must exist. ADR-0030: one column, derived menu. | shell-surface |
| Join | One integrator. Not `main`. | conductor |
| Next view | N14 loop. Owner only. | owner |

## Seams

| from -> to | the request | resolved by |
|---|---|---|
| owner -> specify | The one admitted view and the non-goals | Owner ruling note |
| specify -> architecture | Spec id; Gherkin the architecture must satisfy | Architecture quotes spec statements |
| architecture -> core-query | Query name, DTO, bounds, unindexed/unclassified states | Exact file list in a seam request; conductor assigns |
| core-query -> shell-surface | Frozen query + IPC | Shell binds; no second client-side query |
| shell-surface -> conductor | Proof Pack + owned paths | `conductor-join.py` |
| any -> Atlas fleet | None in this horizon | If a need appears: `coord request add`. Never edit Atlas files. |
| D-5/D-6 -> Conversation lane | Not this horizon | Owner records the blocker (ADR-0036 / reply-channel) |

Scan guards for core-query: root `src/AiDe.Core` + `tests/AiDe.Core.Tests`; recursion on those roots; tokens = paths in the seam; allowlist = those paths. Shell: root `src/AiDe.App/Workbench` + `tests/AiDe.App.Tests`; no `MainWindow.xaml` unless the design names it; no composer files.

ADR-0030 trigger, quoted not paraphrased: *"Add an explicit `Perspectives` column to the App's `SurfaceKind` row — a non-defaulted `IReadOnlySet<string>` of perspective ids"* and *"an empty set fails the build test"* (ADR-0030 §Decision 2 / US-C3 b5).

## Struck tracks

| track | why it was not worth its multiplier |
|---|---|
| Seven parallel D-0…D-6 implementers | Ruling 54 one-at-a-time; shared allow-list (GO5 exclusive resource); AR3 scaffolding. |
| D-5 structure-deriver implementer | Admission needs eval harness. ADR-0036 agentic ladder is not this horizon. |
| D-6 round-trip implementer | Needs reply-channel seam; depends on D-5. |
| Atlas-integration track | Different conductor, open native requests, live claims on Atlas paths. |
| Tests perspective | Ruling 54 non-goal; AR3. |
| Parallel core-query ∥ shell-surface | Data edge: Shell binds Core. Coupling test fails. |
| Explore-graph-capability track | Ruling 54: no new graph capability in Explore. |
| `main` integration track | Owner has not granted it. Join target is `understanding-views`. |

## Order of operations

| # | action | cost | why now |
|---|---|---|---|
| 1 | Paste `docs/coordination/understanding-views-kickoff.md` into a Grok 4.6 **high** Conductor session, or continue this session as Conductor | 0 | The brief is the contract. |
| 2 | Conductor: `coord doctor` in the conductor tree; do not `coord install` | minutes | Layer on, measured. |
| 3 | Spawn Owner (Grok 4.6 xhigh, `owner`) in `coord worktree new --branch understanding-views-owner` | ≤20 calls | N0. Nothing else authors scope. |
| 4 | After the ruling: N1 inventory ∥ N2 comparables (read-only trees) | ≤25 each | Independent. |
| 5 | Specify track in its own tree; then review panel width ≤4 | 50 | Serial after 4. |
| 6 | Architecture + ADR; council; spike | 40+20 | Serial. |
| 7 | Core-query tree, then shell-surface tree | 40 then 50 | Data edge. |
| 8 | Independent Proof Pack review; `conductor-join.py` onto `understanding-views` | 30 | Not `main`. |
| 9 | Owner N14: next view or stop | 20 | Loop variant must decrease. |

## Status table

| | |
|---|---|
| **Completed** | Execution graph; this plan; kickoff; N0–N4; N5 ADR-0038 proposed (`a03fb622`); N6 PASS after Security re-review (`note-understanding-views-n6-council`). Spec still draft. |
| **Remaining** | Join architecture onto `understanding-views`; N7 toolkit spike; N8 ui-design; N9–N10 design; N11 core-query then shell-surface; N12 Proof Pack; N13; N14. |
| **Best next action** | Join `understanding-views-architecture` (`--docs-only`), then N7 Spike Protocol on the tree toolkit. Do not open core or shell. |
