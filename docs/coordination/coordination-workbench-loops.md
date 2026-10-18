---
id: coordination-workbench-loops
title: "Coordination plan - workbench loops (WL-0 → WL-6)"
type: plan
status: proposed
owner: "@timianmalloo"
tags: [coordination, worktrees, parallelism, workbench-loops]
links:
  - { to: plan-workbench-loops-drive-prompt, rel: implements }
  - { to: architecture, rel: implements }
  - { to: spec-workbench-loops-ux, rel: implements }
  - { to: coordination-workbench-loops-leader-kickoff, rel: depends-on }
  - { to: adr-0039-workbench-records-durable-representation, rel: depends-on }
  - { to: adr-0041-execution-attempt-lifecycle, rel: depends-on }
  - { to: adr-0045-perspective-set-factory-review-understand, rel: depends-on }
review-by: "2026-11-18"
summary: >-
  The division of the workbench-loops drive: WL-0 plus a three-item serial spine (schema v2,
  RunEvent/TurnState vocabulary, Perspective re-row) run first as the only coding tracks; WL-1a
  then fans out into host, folds, redaction and binding tracks; WL-S, E-1 and later slices open
  as their interfaces land. Ten coding tracks in the first horizon, at most six at once; the
  honest multiplier is about 4×, not 6×.
---

# Coordination plan — workbench loops

Leader: `wl-leader-20261018` (Claude Code, `claude-opus-5-5`), **epoch 1** (`coord leader pin`, 2026-10-18T22:03Z).
Owner: `wl-owner-20261018` (Fable 5.1). It rules into `docs/notes/rulings.md`. Compiled prompt:
`al-01M58FTCXG1K5J5BWC5X6MGVC2` (gate pass, `dispatchable: true`, zero decision requests). Its
goal state is this plan's goal state. Edit distance from the received drive-prompt file: **0.8904**
(`prompt-compile.py distance`). That file carries frontmatter and paste notes, so the distance
measures the file, not an operator edit.

**Goal (verbatim from the compiled prompt):** implement the workbench-loops evolution defined in
docs/architecture.md §Workbench loops (WL.1–WL.15) and ADR-0039…ADR-0045, to the acceptance
criteria of docs/specs/workbench-loops-ux.md, slice by slice WL-0 → WL-6, landing each slice on
main with its Proof Pack. **Done when:** every slice's "E2E checks (red first)" column in §WL.11 has
an observed red then green; each slice's human demo script has been run by the Owner against the
built app; docs/proof/workbench-loops-<slice>.md exists per slice with the artifacts named in its
episode-close; each slice landed on main with its Proof Pack, in the order WL-0 → … → WL-6.
**Tier:** T2. **Fan-out cap:** 6. **Context ceiling:** 400 000. **Main-line budget:** Leader ≤ 400 tool
calls per slice.

## Grounding — what was read (cited, 2026-10-18 22:03–22:40Z)

- `docs/architecture.md` §Workbench loops, lines 1232–1588 (WL.1–WL.15), read whole.
- ADR-0039 (all), ADR-0040 (all), ADR-0041 (all), ADR-0043 (all), ADR-0045 (all), and the Decision
  sections of ADR-0042 and ADR-0044.
- Spec §A4 (:96–127), §A5 (:129–378, 30 stories, **90** `AC-` criteria, counted by
  `grep -oE "AC-[A-Z]+[0-9]+[a-z]" | sort -u`), §C2 (:656–674), Traceability (:702–721).
  Compiled assumption #3 is confirmed: 30 stories, 90 criteria, and §WL.11 has an E2E column per slice.
- `docs/lessons/defect-classes.md` DC-163, DC-189, DC-190, DC-221, DC-228 (CAP-P). The CTX-A…G
  classes are in `.claude/knowledge/continuous-improvement.md` §6.
- `docs/reviews/forensic-review-rev102.md` FR-001 (:107–114) and FR-002 (:116–123).
- `coord doctor`, `git worktree list`, `coord session list`, `.agents/artifacts.yml`,
  `docs/coordination/join.json`, `pack-doctor.py --json`.
- Code boundaries: two read-only mapping passes over `src/` and `tests/`. Their file:line findings
  are cited in the rows below. Every row is **Verified** against the tree at `7e21707b` unless it
  is marked otherwise.

## Layer state

| check | result | meaning |
|---|---|---|
| registry | ok — 12 patterns | `.agents/artifacts.yml` classifies derived/register paths. Installed **once, in the primary checkout**. Every worktree this plan creates **inherits** it. Run `coord doctor` inside each new tree, never `coord install`. |
| merge driver | effective — `coord-regen`, `coord-register` registered | Derived files regenerate and registers union-merge at every join. |
| leader | `wl-leader-20261018` epoch 1, live | CO-L designation by the Owner's written brief. Renewed at every report and before every join. |
| heartbeat | 1 session beating, 0 stalled | Manual heartbeats only. No hook at the tool seam records them. |
| requests | 631 total, 311 open, `COORD-REQUEST-UNTYPED 311` | Legacy requests with no deadline or fallback. They are not this drive's, and they are never counted as its seams. |
| lease overlap | none (0 live leases) | — |
| worktrees | primary only (`C:/Projects/ai-de` at `7e21707b`, `main`) | No existing tree to adopt or avoid. |
| sessions | 1 active (`wl-leader-20261018`, 0 claims) | — |
| doorbells | not recorded (`pack-doctor`) | Mail is files plus the board. No push channel is advertised. |
| python | `python` works, `python3` does not (Windows) | Every command in every brief uses `python`. |
| ruling register | **`docs/notes/rulings.md` absent** | `coord-decide.py` `next_number` returns `max(…, default=0)+1`, so the first `rule next` allocates **Ruling 1**. Legacy Rulings 1–147 live across `docs/notes/*.md` (`verify-ruling-citations.py`: 145 defects on `main` today, pre-existing, not wired into the join). This is a **recorded risk**, reported to the Owner. A drive ruling must not reuse a legacy number. |
| HTML surface cap | **at the cap: 100 of `SURFACE_LIMIT = 100`** (`docs-graph.py:82`; 101 `.html` under `docs/` incl. the excluded `index.html`) | Rendering this plan's `.html` sibling made `docs-graph.py derive` fail with `SURFACE_LIMIT_EXCEEDED` (observed 2026-10-18). Removing it made derive pass (583 entries). One more HTML file under `docs/` breaks `tools/regenerate-derived.py`, and with it **every join's regenerate step**. So this plan and every drive artifact (design docs, Proof Packs) ship **Markdown only**. Markdown is canonical (V19); this is the recorded deviation from V19's HTML sibling and from the skill's "write both files". The rendered view is kept outside the repo. Reported to the Owner as a pack change (raise the cap, or exempt rendered companions). |
| contract log | `AIDE_CONTRACT_LOG` unset in this session | `episode-close` lines have no channel here. Each Proof Pack names its artifacts. The line is emitted wherever a track's environment carries the variable, and is otherwise reported as *not emitted*. |

## Reconciliations the plan states explicitly

1. **WL-1 is the group WL-1a ∥ WL-S → WL-1b → WL-1c** (§WL.11 re-cut). The drive prompt's "WL-1" means
   that group. The Owner's first demo is WL-1a's scenario on the real `claude-code` lane. The Proof Packs
   are `docs/proof/workbench-loops-wl-0.md`, `-wl-1a.md`, `-wl-s.md`, `-wl-1b.md`, `-wl-1c.md`,
   `-wl-2.md` … `-wl-6.md`.
2. **The re-row goes on the serial spine, ahead of WL-1a's shell binding.** The drive prompt says:
   "WL-0 (FR-001/002) and the serial spine (schema group v2 + migration golden test; `RunEvent`
   vocabulary extension; `PerspectiveSet` three rows with the build test) start immediately as the only
   tracks". ADR-0045 §9 says "WL-1a lands `notebook` + `execution` as kinds admitted to the **existing**
   Coding host first". The code settles the order. The re-row and WL-1a's binding both write the
   `SurfaceContentFactory.Kinds` table on the **same lines**: every `Perspectives:` argument, and the
   `session-document` row at `SurfaceContentFactory.cs:265-271` (:268). They also share
   `WorkbenchShell.cs`, which they touch at :3482, :3648, :3684, :3718 and :3793. A shared authored file
   is a boundary error (CO10), so the spine's re-row lands first. WL-1a then binds the existing
   `session-document` kind on the re-rowed host. That host is the same Coding host, renamed `factory`
   by ADR-0045 §7: "Slot `coding` → `factory`". This is an ordering consequence, not a change of
   direction.
3. **Which kinds WL-1a adds, and which WL-S adds.** §WL.11's WL-1a row says "the **existing**
   `session-document` kind in the **existing** Coding host re-bound to notebook + attempt" and lists
   "no re-row". Its WL-S row says "`notebook` + `execution` kinds admitted; slot migration with
   report". ADR-0045 §2 says "The existing `session-document` kind … is migrated into `notebook` +
   `execution` with drop-with-report until then". This plan follows §WL.11. WL-1a re-binds
   `session-document` to the notebook and attempt *models*. WL-S splits it into the `notebook` and
   `execution` kinds that bind those models, and migrates the placements.
4. **Which tracks carry `/design-slice` before `/implement`.** It runs inside the track, never as a
   pause of the programme:
   - **T4 `sp3-perspective-rerow`** designs all of ADR-0045 (re-row, kinds, migration, spike) in
     `docs/design/workbench-loops-wl-s.md`. T9 and T11 build to it.
   - **T5 `wl1a-attempt-host`** designs the WL-1a seam (ADR-0041 plus the binding API and fold inputs)
     in `docs/design/workbench-loops-wl-1a.md`. T6 and T8 build to it once that design commit lands.
   - **T10 `e1-notebook-spike`** follows the Spike Protocol, not `/design-slice`. ADR-0043's editor
     `/design-slice` runs inside WL-2's first track, after E-1's verdict. If E-1 reverses, the reversal
     trigger in ADR-0043 §7 applies and a new ADR is drafted.
   - The routine Sonnet tracks (T1, T2, T3, T6, T9) build to a design fixed by an ADR or by T4/T5's
     doc. They carry no design stage.
5. **The Leader stays in the primary checkout.** CO15 says "A Coordinator **MUST** enter its own
   worktree … before it delegates". The kickoff places the Leader in `C:\Projects\ai-de` on `main`,
   where `conductor-join.py` lands every join. This is the **recorded exception (WT4)**. The failure
   CO15 prevents, a parent entering a tree *after* delegating and stranding its delegates, cannot occur:
   the Leader never calls `EnterWorktree` at all. Concurrent-writer risk in the primary: the Owner
   session writes only register-class files (rulings, audit entries) and never authors.

## Artifact classes

| path / pattern | class | mechanism | coordination needed |
|---|---|---|---|
| `docs/docs-index.js`, `docs/audit/audit-data.js`, `docs/audit/index.html`, `docs/api/*.md`, `docs/_site/index.html`, `docs/_meta.json`, `docs/specs/workbench-loops-ux.html` | derived | `coord-regen` merge driver; `tools/regenerate-derived.py` at every join | **none** — regenerated, never resolved by hand; tracks never claim them |
| `docs/audit/audit-log.jsonl`, `docs/audit/change-log.jsonl`, `docs/health-history.jsonl`, `.agents/log/*.jsonl`, `.agents/decisions/*.jsonl` | register | `coord-register` union merge | **none** — append-only; never claimed (DC-163) |
| `docs/notes/rulings.md` | register (by `coord-decide.py` contract; not yet in `artifacts.yml`) | Owner appends via `coord decide rule next` | **none** for tracks — only the Owner writes it; see the Layer-state numbering risk |
| test-count baseline (written by `tools/verify-test-run.py --update`) | join-owned | `conductor-join.py` recount step | **none** — tracks never edit it; the join recounts |
| `docs/lessons/defect-classes.md` | authored, append-in-practice | tracks append new classes at the end with a placeholder id; the join's `verify-defect-register.py --fix-counts` | **serialized at the join** — two appends at EOF conflict; the Leader resolves by keeping both entries (union by hand), recorded per join |
| `docs/coordination/coordination-workbench-loops.md` (the briefs are rendered from its rows and kept outside the repo) | authored | Leader only | none — one owner |
| `docs/design/workbench-loops-*.md`, `docs/proof/workbench-loops-*.md`, `docs/proof/workbench-loops-<slice>/<track>/*` | authored | one named owning track each (Tracks table) | one owner per file |
| `src/**`, `tests/**`, `spikes/**` | authored | one owning track per file per window (Tracks table) | **the only real contention** — fixed by boundary, never by lease duration |

## Tracks

Every row carries **leader epoch 1**. Every track's decision requests go to **`wl-owner-20261018`**,
which rules into **`docs/notes/rulings.md`**. Budget is tool calls · peak context · wall clock.
"Owns" lists the authored paths. Any path not listed is out of bounds for that track, and touching it
needs a seam request to its owner.

| track | owns (authored) | depends on | tier | fan-out cap | budget | exit evidence | harness |
|---|---|---|---|---|---|---|---|
| **T1 `wl0-evidence`** (WL-0) | `src/AiDe.Core/WorkspaceCore.cs`, `src/AiDe.Core/Extraction/SourceRevision.cs`, `src/AiDe.Core/Projections/EntryPointsProjection.cs`, `src/AiDe.Core/Projections/ProjectionService.cs`, new `tests/AiDe.Core.Tests/Freshness/DirtyEditSurvivesReindexTests.cs`, `tests/AiDe.Core.Tests/EveryOperationFitsTheFrameTests.cs`, `tests/AiDe.App.Tests/EntryPointsSurfaceTests.cs` + `BoundsReachTheSurfaceTests.cs` (ceiling-line references only), `docs/proof/workbench-loops-wl-0.md` + `docs/proof/workbench-loops-wl-0/**` | — | T2 | 0 | 150 calls · 400k · 3 h | FR-001 regression red→green (same `ArtifactRevision`, dirty content ⇒ new fact, old one not reported current); FR-002 regression red→green (the 1,300-fact / 2,000-char case ⇒ framed bytes ≤ `IpcFraming.MaxFrameBytes`); full `dotnet test` green; red and green outputs under `docs/proof/workbench-loops-wl-0/`; Proof Pack | Claude Code · Sonnet (`claude-sonnet-5-5`) |
| **T2 `sp1-schema-v2`** (spine → WL-1a checks) | `src/AiDe.Core/Store/WorkspaceSchema.cs`, `WorkspaceStore.cs`, `StoreWriter.cs`, `StoreReader.cs`, `src/AiDe.Core/Upgrade/UpgradeCoordinator.cs` (+`StoreSnapshot`), `MigrationJournal.cs`, `src/AiDe.Core/Facts/Dispatch.cs` (outcome vocabulary), new `src/AiDe.Core/Store/Records/**` (row types), new `tests/AiDe.Core.Tests/Store/SchemaV2/**`, `tests/AiDe.Core.Tests/StoreImmutabilityTests.cs`, `UpgradeTests.cs`, `docs/proof/workbench-loops-wl-1a/sp1/**` | — | T2 | 0 | 220 calls · 400k · 4 h | the migration golden round-trip incl. WAL snapshot/restore and fresh-vs-migrated `sqlite_master` equality; the DM11 forbidden-update test; one violation test per ADR-0039 §4 constraint row; the read-only-on-newer-file test; each observed red then green; `dotnet test` green | Claude Code · Sonnet |
| **T3 `sp2-runevent-vocab`** (spine) | `src/AiDe.Core/AgentPlane/RunEvent.cs`, `AcpRunEventMapper.cs` (kind constants + frame fields only), `src/AiDe.Core/Presentation/Sessions/SessionThread.cs`, `ThreadAnnouncementPolicy.cs`, `RunChannelSessionThread.cs`, `src/AiDe.App/Workbench/Sessions/TurnItem.cs`, `ThreadFeed.cs`, `SessionDocumentSurface.cs` (the :653 `Stopped` site only), the eight test files that name `TurnState.Stopped`, new `tests/AiDe.Core.Tests/AgentPlane/RunEventVocabularyTests.cs`, `docs/proof/workbench-loops-wl-1a/sp2/**` | — | T2 | 0 | 140 calls · 400k · 2.5 h | `TurnState` +`Preparing, StopRequested, DeliveryUnknown, Rejected, Denied, OverLimit`; `Stopped` → `Interrupted`; a red-first test that `TurnState` is never serialized; the `RunEvent` envelope carries `redactions[{offset,len,pattern_id}]` + `scanned_with` and the `native.frame` namespace; exact-key tests; `dotnet test` green | Claude Code · Sonnet |
| **T4 `sp3-perspective-rerow`** (spine → WL-S) | `src/AiDe.Core/Workbench/Perspectives.cs`, `ZoneLayout.cs`, `src/AiDe.App/Workbench/SurfaceContentFactory.cs`, `PerspectiveShell.cs`, `DockHost.cs`, `ExplorerSurface.cs` (delete), `WorkbenchShell.cs` (spine window), `CenterEmptyState.cs`, `MainMenuBuilder.cs`, `PerspectiveRail.cs`, `PerspectiveMenu.cs`, `CommandPalette.cs`, `LayoutPersistence.cs` (slot naming), `src/AiDe.App/MainWindow.xaml` + `.xaml.cs`, `CanvasPage.cs` (comment), the perspective/explorer tests and probes the mapping pass listed, `docs/design/workbench-loops-wl-s.md`, `docs/proof/workbench-loops-wl-s/sp3/**` | — | T2 | 0 | 280 calls · 400k · 5 h | `/design-slice` doc committed first; the `Perspectives` three-row build test; the no-row-uses-`FullWindow` test; SH-4.1's oracle on the Factory host; old-row tests re-scoped (a red list then green); the Loomkeeper kinds under Factory → Coordination; `dotnet build` + `dotnet test` green | Claude Code · **Opus** (`claude-opus-5-5`) |
| **T5 `wl1a-attempt-host`** (WL-1a) | `src/AiDe.App/Conductor/GovernedRunHost.cs`, `RunEventRelay.cs`, `ConductorEntry.cs`, `CompositionRootLedger.cs`, `src/AiDe.Core/AgentPlane/AcpPeer.cs`, `AcpLaneClient.cs`, `AcpEngineProcess.cs`, `AcpRunEventMapper.cs` (WL-1a window), new `src/AiDe.Core/AgentPlane/RunLogStore.cs`, `src/AiDe.Core/Sessions/SessionPaths.cs`, `src/AiDe.Core/Dispatch/BoundaryDispatcher.cs`, `DispatchService.cs`, `src/AiDe.Core/Store/StoreWriter.cs` + `StoreReader.cs` (WL-1a window), `src/AiDe.Core/Ipc/WorkspaceOperations.cs`, `WorkspaceClient.cs`, `IWorkspaceDispatch.cs`, `src/AiDe.Daemon/Program.cs`, new `tests/AiDe.Core.Tests/Attempts/**`, `tests/AiDe.App.Tests/Conductor/Attempts/**`, `docs/design/workbench-loops-wl-1a.md`, `docs/proof/workbench-loops-wl-1a.md` + `docs/proof/workbench-loops-wl-1a/host/**` | T2, T3 (joined) | T2 | 1 (one read-only research sub-agent) | 400 calls · 400k (split at ceiling) · 8 h | design doc first; crash injection at W1–W4 ⇒ `DeliveryUnknown{cause}`, never resend; same `dispatch_key` twice ⇒ one launch; stale-prepare refusal; Stop on the **real** claude-code lane: `session/cancel` sent ⇒ `acp.result cancelled` observed ⇒ `Interrupted`; token cancel alone ⇒ not `Interrupted`; the NFR-11 oracle; `attempt.sweep{count,last_sweep_at}` at zero; the redaction fixture end to end (sentinel ⇒ run-log bytes clean + redaction row); each red then green; the slice Proof Pack | Claude Code · **Opus** |
| **T6 `wl1a-folds`** (WL-1a) | new `src/AiDe.Core/Workbench/Records/Folds/AttemptStateFold.cs`, `CurrentnessFold.cs`, new `tests/AiDe.Core.Tests/Workbench/Folds/**`, `docs/proof/workbench-loops-wl-1a/folds/**` | T2, T3 (joined); T5's design commit | T2 | 0 | 140 calls · 400k · 3 h | the pure folds (ADR-0041 §2: Running / Waiting / Denied / OverLimit from the run log; `StopRequested` dominates `Waiting`; a cited seq absent ⇒ Not recorded); currentness (ADR-0040 §5); fixture tests red then green; a `fold.duration_ms` emission test | Claude Code · Sonnet |
| **T7 `wl1a-redaction`** (WL-1a) | new `src/AiDe.Core/AgentPlane/FrameRedaction.cs`, new `tests/AiDe.Core.Tests/AgentPlane/FrameRedactionTests.cs`, `docs/proof/workbench-loops-wl-1a/redaction/**` | T3 (joined) | T2 | 0 | 40 calls · 150k · 1 h (Grok short turn) | a pure `(frame, patterns vN) → (redacted frame, redactions[])`; a sentinel in each frame position is replaced and recorded; non-sentinel bytes are unchanged; tests red then green. T5 wires the call into the mapper. | **Grok Build** (`grok agent -m grok-4.7 --reasoning-effort high stdio`, served `grok-4.7-build` read back) via `coord-runner.py --launch` **if Q0 qualifies it**; otherwise Claude Code · Sonnet in the same tree |
| **T8 `wl1a-notebook-binding`** (WL-1a) | `src/AiDe.App/Workbench/Sessions/SessionDocumentSurface.cs` (WL-1a window), `SessionComposerBinder.cs`, `ConsoleDocumentHost.cs`, `src/AiDe.App/Workbench/Composer/ComposerSurface.cs`, `ComposerSendGate.cs`, `src/AiDe.App/Workbench/WorkbenchShell.cs` (WL-1a window, session regions :85-91, :536-576, :3428-3828), `SurfaceContentFactory.cs` (WL-1a window: the `session-document` row only), `CenterEmptyState.cs`, `PerspectiveShell.cs` (:177 only), `MainWindow.xaml.cs` (:299, :346, :504 only), new `src/AiDe.App/Workbench/Records/**` (notebook/attempt view models), new `tests/AiDe.App.Tests/Workbench/Records/**`, `docs/proof/workbench-loops-wl-1a/binding/**` | T4 (joined), T5's host-interface commit (seam), T2 | T2 | 0 | 320 calls · 400k (split at ceiling) · 6 h | prepare-from-revision (v1 protocol, zero new kinds); edit ⇒ the old result is marked stale; hide the pane mid-run, reopen, the run continues (the CTS has left the surface); the model re-key test (create/move/hide/close never disposes a model); restart ⇒ the attempt and its events read back; UIA names; red then green | Claude Code · **Opus** |
| **T9 `wls-unparent-spike`** (WL-S) | `spikes/second-dock-host-unparent/**` | T4's design commit | T1 | 0 | 80 calls · 300k · 2 h | the A↔B↔C cycle with a live WebView2 and a raw `HwndHost` exits 0; private bytes per host, idle and with a 100k-character notebook, recorded in `RESULT.md` | Claude Code · Sonnet |
| **T10 `e1-notebook-spike`** (WL-2 de-risk) | new `spikes/notebook-editor-e1/**` | spine joined (the drive prompt's "only tracks" rule) | T2 | 0 | 250 calls · 400k · 5 h | ADR-0043 §7's floor, each item measured: IME composition, clipboard round-trip, multi-selection edit/undo, language/whitespace round-trip, 100k-char input p95 < 50 ms with the harness shipped, completion-popup UIA with no dangling option; `RESULT.md` with a pass/reverse verdict | Claude Code · **Opus** |
| **T11 `wls-kinds-migration`** (WL-S) | `SurfaceContentFactory.cs` (WL-S window), `LayoutPersistence.cs` (WL-S window), new `src/AiDe.App/Workbench/Notebook/**` + `Execution/**` surfaces, `tests/AiDe.App.Tests/Workbench/LayoutSlotMigrationTests.cs`, `docs/proof/workbench-loops-wl-s.md` + `docs/proof/workbench-loops-wl-s/**` | T4, T8 (joined), T9 | T2 | 0 | 260 calls · 400k · 5 h | the `notebook` + `execution` kinds bind T8's models; slot migration golden round-trip with `.bak` and a drop report; restore today's saved layout ⇒ report; `Ctrl+1/2/3` with focus inside a WebView2 page; hide/float/redock a live notebook; the WL-S Proof Pack | Claude Code · **Opus** |
| **Q0 `q0-grok`, `q0-agy`** (qualification, not slice work) | `docs/notes/qualify-worker-wl.md` (in each worker's own tree; never joined; the branch is tagged `archive/q0-*`) | — | T1 | 0 | 15 calls · 60k · 20 min each, run one at a time | the runner's structured events; the served model read back per response; worktree isolation observed; then a per-dimension record of `enforced` / `observed-only` / `unsupported` in this plan | `coord-runner.py --launch` · Grok 1.0.41 (`-m grok-4.7 --reasoning-effort high`) then Antigravity 1.4.2 (`--model gemini-3.8-flash-high --output-format stream-json`, policy profile allow command/read/write, deny mcp/url) |

**Later horizon (refreshed from actuals at each slice close, GO19):** WL-1b `wl1b-acceptance` (Sonnet:
obligations, evidence links, dispositions, the acceptance-eligibility and closure folds, a minimal
`checklist` kind, the 100× P1-PERF overhead run) → WL-1c `wl1c-system-journey` (Sonnet: investigation
purpose, origin columns, promote/cancel) → WL-2 (Opus editor `/design-slice` + Sonnet protocol-v2 kinds +
**Antigravity** red-first scaffolds from the WL-2 E2E column) ∥ WL-3 (Sonnet profile binding + **Grok**
mapper tracks for agy/muse with **Antigravity** golden-frame fixtures) → WL-4 → WL-5 (**Grok** test-result
extractor after its spike) → WL-6 (**Grok** `CoordinationReadModel` fixtures). Each delegated row
depends on Q0's verdict for its harness.

### Track contracts (the five-part contract fields the table cannot hold)

| track | objective (one line) | deadline | fallback | termination | seams expected | ADR line the row builds (quoted, DC-189) |
|---|---|---|---|---|---|---|
| T1 | Fix FR-001 and FR-002 in Core so index answers are fresh and frame-bounded | 3 h wall from dispatch | Leader reads the state; kick ladder; a second Sonnet session in the same tree | both regressions green and the suite green, or a `blocked` naming the reason | FR-001 may need `WorkbenchShell.cs:697/735-738/790-795` (revision captured once from HEAD). That file is **T4's** in the spine window, so T1 raises a seam to T4 and does not edit it | FR-001: "Same-revision force:true reported ScopesIndexed=1, Assertions=0 and kept Probe.BeforeEdit after the file became AfterEdit" (forensic review :111) |
| T2 | Land `WorkspaceSchema` v2 with the first `workspace.db` migration chain and a typed store API for every v2 row | 4 h | as T1 | every ADR-0039 §4 row has a violation test; the migration golden test is green | none in the window; downstream reads go through `StoreReader`/`StoreWriter` (T5 owns them in the WL-1a window) | ADR-0039 §5: "`WorkspaceSchema.Version = 2`; `EnsureSchema` reads `max(version)` and applies an ordered `Migrations` list through `UpgradeCoordinator.Run` (the watcher pattern); migration DDL emits the triggers for every new table … tested by `sqlite_master` equality fresh-vs-migrated" · "Snapshot is WAL-safe: `VACUUM INTO` (or `wal_checkpoint(TRUNCATE)` with the store closed) and `Restore` deletes `-wal`/`-shm` first" |
| T3 | Extend the `RunEvent` envelope and `TurnState` once, so the folds, host and UI share one vocabulary | 2.5 h | as T1 | the vocabulary tests green and every `TurnState.Stopped` consumer moved | none | ADR-0041 §2: "`TurnState` is extended, not duplicated: `+ Preparing, StopRequested, DeliveryUnknown, Rejected, Denied, OverLimit`; `Stopped` is renamed `Interrupted` with changed semantics (native acknowledgment only) — the slice's red test asserts `TurnState` is never serialized" · ADR-0042 §6: "the stored raw frame (`acp.frame` / `native.frame`) is the redacted frame plus `redactions: [{offset, len, pattern_id}]` and `scanned_with: patterns v<n>`" |
| T4 | Re-row the Perspective set to Factory · Review · Understand and retire `FullWindow` | 5 h | as T1; a design gap becomes a `DR-n` only if it is a product trade-off | the build tests green and SH-4.1's oracle green on the Factory host | T1's possible `WorkbenchShell` request; T8 and T11 consume its rows after the join | ADR-0045 §1(a): "`PerspectiveSet` becomes exactly three rows: `factory`, `review`, `understand`, each `PerspectiveBody.DockHost`, each with its own `Landing` zone (Factory: Center = notebook; Review: Center = change packet; Understand: Center = visualizer) and a `RoutingOrder` of Understand → Review → Factory" · §1(b): "no row keeps `PerspectiveBody.FullWindow` … with a build test that no row uses `FullWindow`" |
| T5 | Make an execution attempt write-ahead, deterministic, durable and honestly stoppable on the real claude-code lane | 8 h (design commit by +1.5 h) | as T1; at the 400k ceiling split into host-gate / Stop+sweep halves | every T5 E2E check red then green and the Proof Pack drafted | publishes the host interface (dispatch, queue, stop, attempt read) as an early commit for T8; wires T7's `FrameRedaction` | ADR-0041 §1: "`dispatch_key = sha256(envelope_id ‖ projection_sha ‖ queued_after_dispatch_key ‖ parent_dispatch_key)`" · "**read-before-write inside one store transaction; an existing key returns the existing receipt and launches nothing.**" · §4 Stop: "wait on the prompt Task with a **bounded deadline** (10 s, config) → on the native `cancelled` reply with `seq` greater than the high-water mark: `Interrupted`; on deadline: the terminal is `DeliveryUnknown{stop_not_acknowledged}` **unconditionally**" · §3: "`RunLogStore` (WL-1a) appends and flushes one line per `RunEvent` to `runs/<dispatch_key>.jsonl` **before** `Events.PublishAsync`" |
| T6 | Derive non-terminal attempt state and currentness as pure folds | 3 h | as T1 | fold fixtures green | reads T2's row types and T3's vocabulary; no writes | ADR-0041 §2: "Running / Waiting (one state for input *or* permission, per spec §C2) / Denied … / OverLimit … **fold over the durable run log** for `run_id`; an uncorrelated `acp.result`/`acp.error` never yields a terminal state" |
| T7 | A pure ingest-side credential-sentinel redaction transform | 1 h (Grok short turn; RUN-B) | the same brief on Sonnet in the same tree | tests green | T5 wires it | ADR-0042 §6: "The credential-sentinel scan is an **ingest-side transform inside each mapper** … unredacted bytes never reach the run log, the relay, a Proof Pack or an export" |
| T8 | Bind the session document to a notebook revision and an attempt, so a run outlives its pane | 6 h | as T1; split at the ceiling | every T8 check green; the WL-1a demo steps run by the Leader on the built app before review-ready | consumes T5's host interface; T11 consumes its models | ADR-0045 §4: "those dictionaries are re-keyed from surface to model (`Dictionary<NotebookId, NotebookModel>`), and the active attempt's cancellation and queue move out of `SessionDocumentSurface` … into the attempt host (ADR-0041)" · ADR-0043 §2: "WL-1a needs **zero** new message kinds (v1 suffices for save/prepare from a revision)" |
| T9 | Prove three DockHosts under one presenter with live WebView2 | 2 h | as T1 | spike exit 0 with the budget recorded | none | ADR-0045 §3: "the cycle spike is extended to A↔B↔C, recording private bytes per host idle and with a 100k-character notebook as the memory budget" |
| T10 | Qualify CodeMirror 6 as the notebook floor, or trigger the reversal | 5 h | as T1 | a verdict per floor item | none | ADR-0043 §7: "IME composition, clipboard round-trip, multi-selection edit/undo, language/whitespace round-trip, 100k-character input p95 < 50 ms, UIA name/role for the completion popup with no dangling active option" |
| T11 | Split `session-document` into the `notebook` + `execution` kinds and migrate saved layouts with a report | 5 h | as T1 | the WL-S checks green and the Proof Pack drafted | consumes T8's models | ADR-0045 §7: "a kind no longer admitted is dropped **with a report** (DC-219's control), a one-time `.bak` is written, and an unusable/off-screen placement recovers to the perspective default" |
| Q0 | Qualify each delegated harness by one exercised launch | 20 min each | record the harness `unsupported`; its rows fall back to Sonnet | the record is written | none | drive prompt: "Qualify each harness as `enforced` / `observed-only` / `unsupported` from what you actually exercise; never advertise a mode you have not run" |

## Serial spine

| item | why it cannot be parallel | who owns it |
|---|---|---|
| Schema group v2 + migration chain + typed store API (T2) | Every WL-1a…WL-5 track reads or writes these rows. Until the DDL and the writer API are fixed, each track's shape changes, so GO5(b) fails outright. It is also the first `workspace.db` migration (§WL.13), and the first time two sessions rely on that chain. | T2 |
| `RunEvent` envelope + `TurnState` vocabulary (T3) | It is a shared vocabulary. The host (T5), the folds (T6), the redaction (T7) and the binding (T8) all encode it, and a rename of `Stopped` touches every consumer. | T3 |
| Perspective re-row (T4) | It writes the same `SurfaceContentFactory.Kinds` lines and `WorkbenchShell.cs` regions that T8 and T11 need (Reconciliation 2). A shared authored file is a boundary, not a schedule (CO10). | T4 |
| The first join after the spine lands | `coord doctor` is effective, but no drive join has exercised the merge driver yet. The spine joins one at a time (T2 → T3 → T4 → T1), each with `coord regen` read back, before any wave-2 join. | Leader |
| WL-1a → Owner demo → WL-1b → WL-1c | The drive prompt's slice order and the Owner's acceptance gate. WL-1b's folds read WL-1a's attempt and evidence rows, and WL-1c's bridge reads WL-1b's dispositions. | Leader (gate), Owner (accept) |

T1 (WL-0) is **not** on the spine. It has no data edge to T2/T3/T4. Its one possible shared file
(`WorkbenchShell.cs`) goes to T4 as a seam, not as a co-edit.

## Seams

| from -> to | the request | resolved by |
|---|---|---|
| T1 -> T4 | if FR-001 needs the shell to re-resolve the revision (`WorkbenchShell.cs:697`, `:735-738`, `:790-795`), T4 makes that change in its window, or T1 makes it after T4 joins | T4, or the Leader re-windows the file to T1 after T4's join |
| T5 -> T8 | the attempt-host interface (dispatch with insert-as-gate, queue, Stop, attempt read-back, a subscriber channel), committed early on T5's branch and pinned by blob hash | T5 (an early interface commit; T8 rebases on it) |
| T5 <- T7 | `FrameRedaction` as a pure function; T5 wires the call inside the mapper | T7 (delivers the function); T5 (the call site) |
| T6 -> T5 | the run-log line format the fold reads (T3's `RunEvent` serialization) | T3's vocabulary; the format is fixed before T6 starts |
| T8 -> T5 | any new IPC operation or store read the binding needs (`WorkspaceOperations.cs`, `StoreReader.cs` belong to T5 in the WL-1a window) | T5 |
| T11 -> T8 | the notebook/attempt model types the new kinds bind | T8 (joined before T11 starts) |
| any -> `docs/lessons/defect-classes.md` | a new defect class found in a track | appended in the branch; serialized at the join (Artifact classes) |

**Joint satisfiability of guards over shared surfaces (GO14a).**
- `SurfaceContentFactory.Kinds`. T4's guards are the three-row build test, the no-`FullWindow` test and
  `KindAllowListsTests` over the `Perspectives:` column. T8 may change only the `session-document` row's
  `Build` binding. T11 adds the `notebook`/`execution` rows after T8. `SurfaceContentTests` walks the
  whole list (root: `Kinds`; no recursion; token set: every row's `Kind`; allowlist: none). It passes
  while every row builds. All three are satisfiable together because no later track writes a
  `Perspectives:` argument except T11's two new rows, which name `PerspectiveSet.Factory`.
- `TurnState`. T3's guard is the never-serialized test (root `src/`, recursive, tokens
  `JsonStringEnumConverter`/`Enum.Parse<TurnState>`/`[JsonConverter]` near `TurnState`, allowlist empty).
  T5 and T8 consume the enum and never serialize it. This is jointly satisfiable.
- `FactTables` / the trigger set. T2's guard is fresh-vs-migrated `sqlite_master` equality (every table,
  index and trigger). `StoreCompactor.cs:242` reads `FactTables`, and T5 adds no table. This is jointly
  satisfiable. A later v3 (`test_attempt_fact`, WL-5) re-runs the same oracle.

## Struck tracks

| track | why it was not worth its multiplier |
|---|---|
| WL-0 split into FR-001 and FR-002 tracks | Two short Core fixes. One Sonnet session pays grounding once, and wall time is not the binding constraint while the spine runs (objective order: tokens before speed). |
| A separate IPC/wire spine track | IPC operations follow from the host's transaction design (insert-as-gate is one store transaction), which T5's `/design-slice` decides. A wire track ahead of that design would guess the shape. T5 owns the IPC files in the WL-1a window. |
| An Antigravity red-first scaffold track for WL-1a | WL-1a's tests encode the host design that T5 makes inside its own track. Scaffolds written before that design would be rewritten. Antigravity's first rows are WL-2's E2E scaffolds and WL-3's golden frames, where the contract is fixed by then. |
| A separate "Proof Pack writer" track per slice | The track holding most of a slice's checks owns its Proof Pack (T1, T5, T11). The others drop raw evidence under `docs/proof/workbench-loops-<slice>/<track>/`. |
| A `SurfaceModelRegistry` class track | ADR-0045 §4: "no new registry class … A registry type is introduced only when a second owner appears". The re-key is part of T8. |
| E-1 in the first wave | The drive prompt: WL-0 and the spine "start immediately as the only tracks". E-1 opens when the spine has joined. |
| Six concurrent tracks in wave 1 | Only four coding tracks exist before the interfaces land. Q0 runs one worker at a time beside them. |

**Honest multiplier.** Wave 1 runs 4 coding tracks plus one Q0 worker. Wave 2 peaks at 6
(T5, T6, T7, T8, T9, T10) for a few hours. Most of WL-1a's wall time is serial anyway:
T5's design → T5's interface → T8, then the Owner demo. The plan pays about 4× the tokens of one
session for about 2× the throughput. The parallelism buys **context hygiene** (each hard track starts
clean under its 400k ceiling), **isolation** (T4 deletes and renames across the shell; T5 rewrites
the run root), and **genuine independence** (T2/T3/T4/T1 share no file; T9/T10 are standalone spikes).
It does not buy speed on the critical path.

## Order of operations

| # | action | cost | why now |
|---|---|---|---|
| 1 | `coord leader pin wl-leader-20261018 --ttl 900` (done, epoch 1); renew every report and before every join | 1 call | CO-L. Every track row carries epoch 1. |
| 2 | Commit this plan (Markdown only — HTML surface cap); `docs-graph.py derive`; audit entry with `--compiled-from al-01M58FTCXG1K5J5BWC5X6MGVC2 --edit-distance 0.8904`; push | ~6 calls | `/execute-with-coordination` parses the committed md. |
| 3 | `coord worktree new` for T1, T2, T3, T4 (branches `wl0-evidence`, `sp1-schema-v2`, `sp2-runevent-vocab`, `sp3-perspective-rerow`, base `main`); `coord doctor` inside each | 8 calls | One tree per track before the first spawn (CO15, WT3). |
| 4 | Dispatch T1–T4 through the `Agent` tool (`model: sonnet` for T1–T3, `model: opus` for T4) with CTX-F briefs rendered from the Tracks and Track-contracts rows | 4 spawns | The spine and WL-0 are the only coding tracks (drive prompt). |
| 5 | Q0: compile one qualification brief; `coord-runner.py prepare` / `run` for Grok, then Antigravity; record the per-dimension verdict here | ~15 calls · 40 min | Runs in the spine's shadow, so a delegated row is qualified before wave 2. |
| 6 | First board post to `wl-owner-20261018` (table + four lines), then every 15 min | 1–2 calls each | The cadence rule. |
| 7 | Verify each spine track's exit evidence (tests re-run in its tree, files present, red evidence read); `coord leader who`; `conductor-join.py <branch> --epoch 1` in order T2 → T3 → T4 → T1; push after each | ~10 calls per join | Upstream first. The first joins prove the merge driver. |
| 8 | WL-0 review-ready → send the Owner the demo script + `docs/proof/workbench-loops-wl-0.md`; join only on *accept* | 2 calls | Human demo per slice. |
| 9 | Wave 2: trees + dispatch T5 (Opus), T6 (Sonnet, after T5's design commit), T7 (Grok if qualified), T9 (Sonnet), T10 (Opus); T8 (Opus) after T4 joins and T5's interface commit | ≤ 6 concurrent | The interfaces are committed. |
| 10 | WL-1a join (T7 → T6 → T5 → T8) after read-back; the Owner runs the WL-1a demo on the built app; accept ⇒ push | — | The first Owner demo. |
| 11 | T11, then WL-S join + demo; refresh this plan for WL-1b/WL-1c from actuals; dispatch | — | GO19: planned vs actual feeds the next slice's plan. |
| 12 | At the 400k context ceiling, or when only running tracks remain: hand off to the Owner with plan path, track states, worktrees, open requests, resume line | — | The kickoff's ceiling rule. |

## Harness qualification (CO12 — only what was exercised here)

| harness | version observed 2026-10-18 | model id check | worktree isolation | instructions | permissions | verdict |
|---|---|---|---|---|---|---|
| Claude Code (Agent tool) | 2.1.302 | `sonnet` → `claude-sonnet-5-5`, `opus` → `claude-opus-5-5` (to be read back from the first spawn's report) | observed-only (a sub-agent works by absolute path in its tree; nothing prevents a write elsewhere — the commit floor and the Leader's read-back are the controls) | observed-only | observed-only (bypass mode on this session) | **observed-only** — to be confirmed at the first dispatch |
| Grok Build via `coord-runner` | 1.0.41 (`grok --version`) | `~/.grok/models_cache.json` (fetched 2026-10-14) lists `grok-4.7`, `grok-4.7-build-fast`, `grok-4.6`, `grok-4.5`; **`grok-4.7-build` is the served alias of the `grok-4.7` pin** (bench ADR-0024 at `8f514607`: "`grok-4.7` ↔ `grok-4.7-build`"), so the argv pins `-m grok-4.7` and every response's served id is read back | not exercised here | not exercised here | not exercised here | **unsupported until Q0** (bench Q0b was observed-only at 1.0.41 in another repo; that is not this repo) |
| Antigravity via `coord-runner` | 1.4.2 (`agy --version`; bench qualified 1.2.13/1.3.3) | `agy models` lists `gemini-3.8-flash-high` | not exercised here | not exercised here | headless has no interactive permission response; launch only with the allow command/read/write + deny mcp/url profile; never `ask`, never `--dangerously-skip-permissions` | **unsupported until Q0** |

## Backlog per slice (§WL.11 "E2E checks (red first)" items, counted at `7e21707b`)

| slice | checks | owner tracks |
|---|---|---|
| WL-0 | 2 regressions (+ suite stays green) | T1 |
| WL-1a | 11 (W1–W4 crash injection; same key twice; stale-prepare; Stop cancel ⇒ `Interrupted`; token cancel ⇒ not `Interrupted`; migration golden round-trip; DM11 forbidden update; constraint-table violations; NFR-11 oracle; sweep at zero; redaction fixture) | T2 (3), T5 (7), T5+T7 (1) |
| WL-S | 5 (three-row build test; no `FullWindow`; SH-4.1 on Factory; slot migration round-trip; unparent spike exit 0) | T4 (3), T11 (1), T9 (1) |
| WL-1b | 4 | later |
| WL-1c | 3 | later |
| WL-2 | 5 (E-1 is one) | T10 (E-1), later |
| WL-3 | 5 | later |
| WL-4 | 6 | later |
| WL-5 | 5 | later |
| WL-6 | 3 fixture groups | later |

## E7 surface list per slice (§WL.10: store → model → service → wire → client type → UI → compute reader)

| slice | store | model | service | wire | client type | UI | compute reader |
|---|---|---|---|---|---|---|---|
| WL-0 | assertion revision identity (FR-001) | — | `WorkspaceCore` refresh | `EntryPoints` frame (FR-002) | — | `EntryPointsSurface` (bounded result) | graph/summary readers over the index |
| WL-1a | v2 + migration (T2) | row types; `AttemptState`/`Currentness` folds (T2, T6) | attempt host, `RunLogStore`, sweep, Stop (T5) | `RunEvent` envelope + redaction (T3, T7); dispatch IPC (T5) | notebook/attempt view models (T8) | session document re-bound; execution states incl. `StopRequested`/`DeliveryUnknown` copy (T8) | folds' consumers: the stale marker, the execution view (T6 → T8) |
| WL-S | layout slot files (T11) | `PerspectiveSet` rows (T4) | slot migration + report (T11) | — | `PerspectiveMenu.For` derivation (T4) | three DockHosts, left hierarchy, the `notebook`/`execution` kinds (T4, T11) | `Ctrl+1/2/3` routing, `RoutingOrder` (T4) |
| WL-1b → WL-6 | per §WL.11's "Real" column; each refreshed plan names the owning track per row | | | | | | |

## Risks recorded (not mitigated beyond what is stated)

- **Ruling numbering.** See Layer state. Reported to the Owner in the first board post. This plan raises
  no `DR-n` because the repo answers every open question so far.
- **Defect-register appends conflict at EOF.** Serialized and union-resolved by the Leader at each join.
- **Real-lane Stop qualification** (`session/cancel` honoured by the claude adapter/CLI) is Flagged in
  §WL.12. If the adapter ignores it, T5's check observes `DeliveryUnknown{stop_not_acknowledged}`,
  which is the ADR's honest terminal, not a failure of the track. The Proof Pack says which was observed.
- **Self-updating CLIs** (§WL.7). Q0 verdicts are bound to the observed version. A version change before a
  delegated dispatch re-runs Q0.

## Status

| | |
|---|---|
| **Completed** | Grounding (cited above); layer measured; boundaries mapped from code; four reconciliations stated; 11 coding tracks + Q0 defined; spine named; seams and struck tracks recorded |
| **Remaining** | Commit and push this plan; open T1–T4 trees and dispatch; Q0; 15-min reports; spine joins; WL-0 demo; wave 2 |
| **Best next action** | Order of operations #2–#4: commit, then dispatch T1–T4 |
