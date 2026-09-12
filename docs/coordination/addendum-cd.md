---
id: coordination-addendum-cd
title: "Coordination plan - Addenda C and D (the perspective shell and the compile step)"
type: doc
status: proposed
owner: "@timianmalloo"
phase: "addendum-c"
tags: [coordination, worktrees, parallelism, plan, addendum-c, addendum-d]
links:
  - { to: architecture, rel: implements }
  - { to: note-addendum-cd-architecture-p1-inputs, rel: implements }
  - { to: spec-addendum-c-perspectives, rel: relates-to }
  - { to: spec-addendum-d-compile-step, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: relates-to }
  - { to: plan-addendum-c-modes, rel: refines }
  - { to: ui-review-perspective-shell, rel: relates-to }
  - { to: ui-review-session-conversation, rel: relates-to }
  - { to: adr-0030-perspective-registry-and-allow-lists, rel: relates-to }
  - { to: adr-0031-second-docking-host, rel: relates-to }
  - { to: adr-0032-perspective-layout-slots, rel: relates-to }
  - { to: adr-0033-prompt-compilation-bounded-context, rel: relates-to }
  - { to: adr-0034-envelope-event-store, rel: relates-to }
  - { to: adr-0035-compile-session-binding-and-pin, rel: relates-to }
  - { to: adr-0036-compile-mode-ladder-deployment-gates, rel: relates-to }
  - { to: adr-0037-family-craft-profile-dimension, rel: relates-to }
  - { to: note-coordination-two-lanes-not-three, rel: relates-to }
  - { to: note-coordination-plan-artifact-type, rel: relates-to }
review-by: 2026-10-11
summary: >-
  Two code lanes (Shell; Conversation), three side tracks (a design slice, the P-D5 wire spike,
  the census controls), a three-node serial spine (F5 merge, INV-0009 merge, the settings and
  sentinels commit) and a width cap of 3. Seven candidate tracks struck for not clearing the
  multiplier. Layer state measured, not assumed; one registry gap repaired.
---

# Coordination plan - Addenda C and D

*Node P1 of `plan-addendum-c-modes`, session `addendum-c-chain`, produced by `/prepare-for-coordination`
on `feature/addendum-c` @ `eba82d5f` (= `main`). The conductor (`conductor-addendum-c`) dispatches it
through `/execute-with-coordination`. Every load-bearing claim carries **Verified** (observed here),
**Inferred** (a model, with the gap that forced it) or **Flagged**. A citation is not a promotion.*

**Goal state of this turn.** Goal: the division of Addenda C and D across parallel sessions with a
serial spine, honest cost, one owner per authored file, exit evidence per track. Done when: this file
and its `.html` twin are committed on `feature/addendum-c`, the audit entry is written, the gates are
green, the branch is pushed. Not in scope: any merge to `main`; any write under `src/`, `tests/`,
`docs/specs/`, `docs/adr/`; `coord install`. Tier T2. Fan-out cap 3 (three read-only digests ran).

**The finding that shapes the plan.** The contention here is *not* in the generated files - the
registry already routes those to a merge driver (§Layer state) - it is in **two aggregates that meet
at one file**: the composer's send gate (`ComposerSendGate.cs`) is where Addendum C's read-only turn
(Ruling 73), Addendum C's composer-as-conversation and Addendum D's `Project()` all land. Three
candidate tracks would author that file in the same wave. The boundary fix, not the schedule fix, is
**one Conversation lane** that owns the send path end to end, serialised inside one worktree, beside
**one Shell lane** that owns the perspective mechanism and never touches the composer. The honest
width is therefore **2 code lanes + 1 side-track slot = 3**, and the plan says so rather than
producing the twelve tracks the candidate list offered (§Struck tracks).

## Layer state

Measured in the worktree `C:\Projects\ai-de-feature-addendum-c` on 2026-09-11 with
`python docs/ai-forward-pack/scripts/coord-core.py doctor` and `pack-doctor.py --json`. The layer was
turned on once, in the primary checkout, on 2026-09-09 (SETUP 4); **`coord install` was not run here**
(DC-112: a worktree shares `.git/config` and `.git/hooks`; an install inside it repoints the clone's
drivers at a tree that will be deleted). Every worktree this plan creates **inherits** the
registration and reads it back with `coord doctor`; none needs an install of its own.

| check | result | meaning |
|---|---|---|
| registry | `ok - 11 pattern(s)` | `.agents/artifacts.yml`: 6 managed (`classify init`) + 5 repo-own (`docs/api/*.md`, `docs/_site/index.html`, `docs/_meta.json`, `.agents/log/*.jsonl`, `.agents/decisions/*.jsonl`). Verified by reading the file. |
| merge driver | `effective - coord-regen, coord-register declared ... registered` | `merge.coord-regen.driver` = the **primary checkout's** `coord-core.py` (`C:/projects/ai-de/...`), resolves. Repointed 2026-09-09 after DC-112. Verified with `git config --get`. |
| `.gitattributes` coverage of the registry | **10 of 11 before this plan; 11 of 11 after** | `docs/_meta.json` was registered `derived` by hand after the install but had **no `merge=coord-regen` attribute** (`git check-attr` → `unspecified`), so the driver could never fire for it and it merged as authored - exactly the conflict the registry's own comment records. Repaired here with the one line `coord install` would have written, after running its generator (`tools/build-doc-viewer.py`: the only diff is the `documented_sha` provenance stamp). `doctor` is **blind** to this gap: it checks that *a* driver is declared and registered, not that every registered pattern carries an attribute. Finding → `note-coordination-two-lanes-not-three` §Findings (a class, ready to append at the join). |
| regeneration | `7 artifact(s) OWED` before; **nothing owed on this branch** after | `.agents/regen-owed.txt` is a **tracked** file that lists 7 derived paths the driver once resolved to OURS at a merge. Running `coord regen` here regenerated all 7 with **no diff** - the artifacts were already current; the marker was stale bookkeeping (someone ran `regenerate-derived.py` and never `coord regen`). The marker is removed on this branch. **Side effect, reported:** `coord regen` resolves the record **per repository** (`repo_root()` via the git common dir) and therefore deleted the *primary checkout's* copy of the marker - an uncommitted `D .agents/regen-owed.txt` in `C:/Projects/ai-de` that this node was (correctly) refused permission to reverse. The conductor restores it with `git -C C:/Projects/ai-de checkout -- .agents/regen-owed.txt` or accepts the deletion (this branch removes the same file). Class: a per-repository store whose marker is a tracked file in one checkout (DC-112's sibling) - ready to append at the join. |
| `extensions.worktreeConfig` | unset | Every worktree shares config and hooks → one install per clone, never per tree. |
| pre-commit floor | installed in the shared `.git/hooks/pre-commit`; `exec`s the primary's `coord-core.py precommit` | **Enforced** in every worktree, every harness: a staged path under another session's live lease exits 3; an unset `AGENT_SESSION` exits 4 (fail-closed). Verified by reading the hook. |
| PreToolUse edit boundary (Claude Code) | **not wired** | `.claude/settings.json` has 0 references to coord; `.claude/hooks/` does not exist. `doctor`'s "claude ... enforcing (established 2026-08-24, spike S5)" is a **hard-coded capability line, not a measurement here** (its cited commits `50b849a`/`e1ec9d0` are not in this history). Qualified `unsupported` for this repo; the commit floor is the control. |
| `pack-doctor` | coordination PASS; knowledge graph FAIL (1) | The one graph problem is a dangling link `proof-conductor-front-door` from `docs/notes/front-door-ruling-49.md` - the target lands with F5 (`docs/proof/conductor-front-door.md` is on `feature/exit-evidence`). Resolves at spine node S0. |
| worktrees / sessions | 7 trees; 12 sessions active per `.agents/log` (8 h staleness); 0 claims | `git worktree list`; `coord session list`. No live lease anywhere - the record is quiet, which is *not* evidence the layer is unused (the OWED marker proves the driver fired). |
| audit start marker | `2026-09-11T23:38:59Z` | `audit-log.py start --session addendum-c-chain` at grounding (IO1); the closing entry measures `duration_seconds`. |

## Artifact classes

For every artifact the tracks touch. `derived` and `register` need **no coordination**: the driver
regenerates or union-merges, and the conductor runs `tools/regenerate-derived.py` then `coord regen`
at every join. `authored` is the only real contention: **one owner per file at a time**.

| path / pattern | class | mechanism | coordination needed |
|---|---|---|---|
| `docs/docs-index.js`, `docs/audit/audit-data.js`, `docs/audit/index.html` | derived | `coord-regen` (regenerate: `docs-graph.py derive`, `audit-log.py render`) | **None.** Regenerated at every join; never merged by hand. |
| `docs/api/*.md`, `docs/_site/index.html`, `docs/_meta.json` | derived | `coord-regen` (`tools/api-reference.py`, `tools/build-doc-viewer.py`) | **None.** `_meta.json`'s attribute added by this plan. |
| `docs/audit/audit-log.jsonl`, `docs/audit/change-log.jsonl`, `docs/health-history.jsonl` | register | `coord-register` (union) | **None.** Append through `audit-log.py` only; ids are ULIDs. |
| `.agents/log/*.jsonl`, `.agents/decisions/*.jsonl` | register | `coord-register` (union) | **None.** The loomkeeper contract logs; committed by mandate. |
| `.agents/sessions/*.md` | untracked liveness | none (machine-local) | **None.** Never states a path table (`.agents/sessions/README.md`); §2 wins on disagreement. |
| `docs/lessons/defect-classes.md` | authored, **append-only** | conventional markers; ids by `verify-id-allocators.py` (family `DC`, **contiguous**) | Rule-bound (§Seams): every track appends; the id is allocated by the **conductor at the join**, never on a branch - two live trees (`fix/session-document-render`, `investigate/session-document-render`) already hold the two ids after `main`'s DC-147 for INV-0009's classes, so a branch-allocated id either collides or gaps (DC-013). |
| `tools/expected-test-counts.json` | authored, **recomputed** | `tools/verify-test-run.py --update` (needs a full test run - too slow for a merge driver) | Rule-bound: never hand-merged; the conductor re-runs `--update` at every join (DC-082's shape: a count is recomputed, not carried). |
| `docs/collaboration/session-contracts.md` §2 | authored, **register of ownership** | conventional | **Conductor-owned for this plan.** Tracks never edit it; the rows in §"§2 rows, ready to apply" are applied by the conductor at dispatch as *moves*. |
| `.github/workflows/build.yml` | authored | conventional | One owner (X-1) in this horizon; other tracks **append a named step by seam request**. |
| `DESIGN.md` | authored | conventional | Shell lane owns; the Conversation lane's three additive `ComposerPageTheme.Roles` tokens (review P-4 residual) are a seam request. |
| `src/**`, `tests/**` per the Tracks table | authored | conventional | One owner per file; `tests/` follows the file under test (§2 rule). `*.csproj` edits are seam requests to the conductor. |
| `docs/proof/<track>.md` (one per track) | authored | conventional | The track's own Proof Pack; named in its exit evidence and in its `episode-close`. |
| `docs/design/*.md`, `docs/notes/*.md` written by a track | authored | conventional | Owned by the writing track; V2 frontmatter; `docs-graph.py derive` at close. |
| `site/*.html` | authored (figures recomputed by `verify-site-figures.py --update`) | conventional | **Deliberately not `derived`** (the registry says why: regenerating would discard prose). No track edits them; `regenerate-derived.py` refreshes the figures at the join. |
| `spikes/compile-session-pin-wire/**`, `docs/proof/compile-pin-spike.json` | authored (a run's record; replaced whole on re-run) | conventional | PD-5 owns. Not `register`: a re-run replaces, it does not union. |

**No new `derived` or `register` class was found for this plan's tracks.** Checked: every artifact a
track generates is either already registered (the API reference, the doc viewer, the audit views, the
index) or lives outside the repository (`~/.aide/proof/compile-eval-admission.json`,
`compile-pin-spike.frames.jsonl`, the per-workspace `envelope-events.jsonl`). The registry is
therefore **not extended**; the one repair was the missing attribute above.

## Tracks

Two **lanes** (one worktree each, one fresh sub-agent per slice, WT1a) and three **side tracks**.
A lane's slices are serial and share one owner role; the lane is the unit `/execute-with-coordination`
spawns a worktree for. Model tier per GO19: **opus** for contract, protocol and evidence judgement;
**sonnet** for low-novelty iteration. Budgets are seconds of node wall time from today's measured
`duration_seconds` (`docs/audit/audit-log.jsonl`, 2026-09-05..11): `implement` median **2,945 s**,
max **4,127 s** (n = 7); `ui-design` median **4,812 s** (n = 3); `define-architecture` 5,826 s
(n = 1). No `design-slice` run has been measured; no `prepare-for-coordination` run had been before
this one - **this run measured 1,964 s** at its close (audit `al-01M29F9B61V737BTK4HAQR05T0`) against
the parent plan's 900 s model (planned vs actual: 2.2×; the next plan uses the measurement).
A budget is *plan / stop*: at *stop* the node **reports and stops** (a cap firing is a defect
signal, never a termination argument). Tool calls are **not recorded** for implement nodes (only one
`specify` row carries `main_calls`: 75 of 90) - a finding; every track's closing audit entry records
`main_calls`. Conductor cost per node: 8-10 calls (measured, front-door plan).

| track | owns (authored) | depends on | tier | fan-out cap | budget | exit evidence | harness |
|---|---|---|---|---|---|---|---|
| **SH-1** Shell lane · the registry, allow-lists and derived menu (ADR-0030; C-1a) · **opus** | `src/AiDe.Core/Workbench/Perspectives.cs` (new: `Perspective`, `PerspectiveSet.All`), `src/AiDe.Core/Workbench/WorkbenchCommands.cs` (`perspective.*` rows replace `shell.toggleExplorer`), `src/AiDe.Core/Workbench/LayoutModel.cs` (kind rows), `src/AiDe.App/Workbench/SurfaceContentFactory.cs` (`Perspectives`, `Instances` columns), `src/AiDe.App/Workbench/PerspectiveMenu.cs` (new: `For`, `Resolve`), `src/AiDe.App/Workbench/MainMenuBuilder.cs`, `src/AiDe.App/Workbench/CommandPalette.cs`; their tests | S2 | T2 | 3 (persona reviews) | 2,945 / 4,127 | Reds first: `MainMenuTests.TheMenuCoversEveryCatalogCommand` (re-scoped to ≥ 1 perspective ∪ entry verbs), `EveryMenuItemShowsItsKeyboardChord` (bound-only), `ExplorerModeTests.Toggle_FlipsModeAndRaisesModeChanged` (three-row set; activate-the-active no-op), the announced-gesture uniqueness collector (four collisions today, US-C10 b2), the `Perspectives` non-empty-set build test, the §B3 literal-table menu oracle, the US-C3 routing table, the palette-rows-equal-menu test, the mutation test (a test-time kind row appears only where admitted). `docs/proof/perspective-registry.md`. UX & A reviews PS-M1-M4. | worktree `ai-de-lane-shell`; leases enforced at commit; edit boundary observed-only |
| **SH-2** Shell lane · the second host, the shell presenter, one slot per host, the rail (ADR-0031/0032, ADR-0017 amended; C-1b) · **opus** | `src/AiDe.App/Workbench/WorkbenchShell.cs` (the `DockHost` extraction; 2,967 lines - the largest edit), `src/AiDe.App/Workbench/DockHost.cs` (new), `src/AiDe.App/Workbench/ShellModeController.cs` → `PerspectiveShell.cs` (the rename, in this commit only), `src/AiDe.App/Workbench/LayoutPersistence.cs` (`SlotPathFor`, `RestoreResult`, `.pre-perspectives.bak`), `src/AiDe.Core/Workbench/ZoneBackedLayoutService.cs` (admitted kinds at construction), `src/AiDe.Core/Workbench/ZoneLayoutStore.cs` (per-host file; reported refusal), `src/AiDe.App/Workbench/WorkbenchController.cs`, `src/AiDe.App/Workbench/WebSurfaceHost.cs` (DC-138 once-gate), `src/AiDe.App/MainWindow.xaml`, `MainWindow.xaml.cs` (rail: three destinations, manual activation, states; title suffix; status strip), `src/AiDe.App/Workbench/WorkbenchDiagnostics.cs` (the switch event with `outcome`, `error_code`), `DockRoundedTabs.xaml` (review P-2), `DESIGN.md` (PS-R*, PS-T*, menu brushes P-5); their tests | SH-1; S1 (INV-0009's `DocumentOpening` seam is the first red this rule generalises) | T2 | 3 | 4,127 / 6,200 (**Inferred** ×1.5 - no node of this size measured; gap: the largest edit in Addendum C) | Reds first: ADR-0032's tests 1-7 (pre-C file restore drops `classdiagram`/`canvas`/`view`; one-instance duplicates; independent A/B files; the golden rollback round-trip with the frozen schema-1 DTO); ADR-0031's identity test across three bodies (US-C2) and routing test (Architecture active → host B only); the allow-list refusal at the service; `ANewSessionCreatedWhileExplorerIsTheBodyIsShown` **stays green through the extraction**; `AReopenedSessionIsShownAndItsComposerIsBound` green. DC-135 ratio watch: `new LayoutService(` vs `new ZoneBackedLayoutService(` under `tests/` must not widen from 66:8. **Attended (operator):** P-4 (WebView2/HWND identity, process count, private bytes across a switch with a terminal live and a run streaming), P-8 (the ≤ 150 ms p95 retained switch - measured, never a CI assert), P-7 (`Ctrl+3` from inside the Explore body's WebView2 page, then inside the terminal), P-1 (UIA walk of the rail), P-5 (the permission overlay while Architecture is active). `docs/proof/perspective-mechanism.md`. Data & Persistence clears the rollback round-trip; UX & A clears P-1/P-9. | as SH-1 |
| **SH-3** Shell lane · Coding's default, the Evidence pair, Architecture's existing content (C-2; Rulings 54/59/61) · **sonnet** (opus review) | `src/AiDe.Core/Workbench/ZoneLayout.cs` (`WorkbenchLayout.Default(perspective)`), `src/AiDe.App/Workbench/CanvasSurface.cs`, `CanvasPage.cs` (the second instance with the kind filter `code · data · architecture`; the `view`/`inspector` selection source), `src/AiDe.App/Workbench/ClassDiagramSurface.cs` (Ruling 54's scaling fix), `src/AiDe.App/Workbench/JoinSurface.cs` (only if Ruling 59's real-content check fails: leave the Architecture default), `ExplorerSurface.cs` (unchanged by ruling; owned so nobody else touches it); their tests | SH-2 (host B composed) | T1 | 2 | 2,945 / 4,127 | Reds first: `SurfaceContentTests.TheJoinsSurfaceIsBuilt_AndIsInTheDefaultLayout` and its four `*IsInTheDefaultLayout` siblings re-scoped to "reachable from the derived menu of the perspective that admits it" + §B4 defaults; the US-C6 positive master/detail oracle (both trees identical today); the US-C8 recording-fake kind filter (`FakeWorkspaceQueries`); US-C8 b3/b4 (in-host opens; drill-to-node return). **Attended:** open a fresh workspace and read §B4's layout; select an evidence row and read its provenance (P-6); Ruling 59's condition - does `joins` render real content against a real workspace? (**Inferred** by the Owner; measured here). `docs/proof/perspective-content.md`. | as SH-1 |
| **CV-0** Conversation lane · **the read-only turn** (Ruling 73; Coding's first slice - the REPL) · **opus** | `src/AiDe.App/Workbench/Composer/ComposerSendGate.cs` (Message / no-scope shape → read-only, no `Derive`), `src/AiDe.Core/Presentation/Composer/ComposerCompiler.cs`, `ComposerDraft.cs` (the decoration line state *"read-only - nothing will be written"*), `src/AiDe.App/Workbench/Composer/ComposerSurface.cs`, `src/AiDe.App/Web/composer.html`, `composer.mjs`, `src/AiDe.Core/Presentation/Composer/LeaseDerivation.cs` (the `:25` premise narrows to *no lease means no write capability*; signature unchanged), `src/AiDe.Core/AgentPlane/GoalBlock.cs` (`SpawnContract.Validate`/`Authorize` accept the read-only shape), `src/AiDe.Core/AgentPlane/LeaseAndSeams.cs`, `src/AiDe.App/Conductor/GovernedRunHost.cs` (`ReadOnlyLaneSession` beside F5's `GovernedLaneSession`: every write-capable tool disallowed - at least `Write`, `Edit`, `MultiEdit`, `NotebookEdit`, `Bash`; the exact set from the SDK's tool list, read, not recalled), `GovernedRunRequest.cs`; tests incl. `tests/AiDe.Core.Tests/AgentPlane/AcpLaneClientTests.cs`, `tests/AiDe.App.Tests/Conductor/TheGovernedLaneHasNoShellTests.cs` | S0 (`LaneSessionOptions`), S2 (`RunBudget.SubscriptionBounded`) | T2 | 3 | 2,945 / 4,127 | Reds first: the read-only lane's `session/new` **exact-key-set** wire test (the disallowed set on the outgoing frame; Ruling 71's shape); a Message-shaped send derives no lease and is not refused (the US-C13 refusal now fires for write-shaped turns only); a Goal-block with a derived scope still takes the lease gate unchanged; `Roots` unchanged. **Attended (operator):** one read-only turn in the real app, the Proof Pack records the outgoing frame, every observed tool-call name, and a clean `git status` after the turn (Ruling 73 condition 1-2). `docs/proof/read-only-turn.md`. Security clears (the shape the control protects is named). | worktree `ai-de-lane-conversation`; leases enforced at commit; edit boundary observed-only |
| **CV-1** Conversation lane · the composer as a conversation (C-3; US-C13; SC1-SC10; the thread `ItemsControl`) · **opus** | `src/AiDe.App/Workbench/Composer/*` (regions; Prepare's regions as WPF controls with a **fake deriver returning three strings**), `src/AiDe.App/Web/composer.html`, `composer.mjs`, `src/AiDe.App/Workbench/Sessions/SessionDocumentSurface.cs` (the thread `ItemsControl` per DS-1: SC8 keyboard model, SC9 announcements), `ConsoleSurface.cs` (SC7 folded Console per turn), `NewSessionSheetDialog.cs`, `NewSessionFlow.cs`, `SessionComposerBinder.cs` (from S1; Ruling 47's one binding site), `src/AiDe.Core/Presentation/Sessions/NewSessionSheetViewModel.cs` (Ruling 72: budget as a state, `free-form` preselected, **zero required inputs**), `src/AiDe.Core/Presentation/Composer/ComposerCompiler.cs`, `ComposerDraft.cs` (`ParseBudget` retired), `src/AiDe.Core/Sessions/SessionConfig.cs`, `SessionConfigStore.cs` (from S2), INV-0007 phases 5-6 (keyboard entry focus; the page-side contrast floor and `composer.html` fed to `ui-craft-gate`); tests | CV-0; **DS-1** (the thread control's design); S2 | T2 | 3 | 4,812 / 6,000 (**Inferred** from `ui-design`'s median - the slice is UI-heavy; gap: no composer implement of this size measured) | Reds first: `TheComposerRendersItsFieldLevelErrorsTests.ARequiredFieldGapBlocksSend…` (six fields on screen), `TheComposerIsOneValidationMechanismTests.Ruling26b_…`, `GoalBlockTemplateTests.TheTemplatesFieldsAreTheSameSetAsGoalBlockFields` (tier / fan-out / budget leave the per-prompt set), the positive send with nothing typed for tier/fan-out/budget (*"'tier' is required"* today), the `"free-form"` quoted-literal census (zero today, allowlist `Leaderboard.cs`), the numeral-absence render test for `SubscriptionBounded`, DS-1's SC8/SC9 oracles (keyboard model; once-only status, assertive errors). US-C13, US-C5 headless; P-11 (WPF half), P-12, P-13; the census-pending rows in `ContrastFloorTests`. **Attended:** type the US-C13 fixture; send at defaults with one gesture; read *"bounded by your subscription"*. `docs/proof/composer-conversation.md`. UX & A clears the census rows; the WPF styling expert reviews. | as CV-0 |
| **CV-2** Conversation lane · the mechanical compile, the envelope store, Prepare's states, purge (D-1 with D-0 folded; ADR-0033/0034) · **opus** | `src/AiDe.Core/Compilation/**` (new: `Envelope` + five event records `compiled-envelope/1`, `Fold`, `Current`, `Confirmed`, `EffectiveMode`, `Projection.Project`, `PreCompile`, `CompileContract`, `CompileOutputValidator`, `CompilePromptAssembler` + embedded resources, `EnvelopeStore` - **the store is the lane's first commit**), `src/AiDe.App/Workbench/Composer/ComposerSendGate.cs` (`Send` via `Project()`; `ComposerSendContext.TaskClass` from `default_task_class`), `src/AiDe.Core/Presentation/Composer/ComposerCompiler.cs` (the render site calls `Project()`), `LeaseDerivation.cs` (`HasMention`, the shared regex), `src/AiDe.App/Workbench/Sessions/SessionDocumentSurface.cs` (store lifetime: open on document open, `FileShare.None`, dispose on close; a locked file degrades Prepare), `src/AiDe.Core/Watcher/Leaderboard.cs` + the watcher store (`task_class_source` expand-only column, v5→v6, ADR-0028 amendment), `src/AiDe.App/Cli/**` (new; `aide compile fold`, `aide session purge` - the CLI home is `/design-slice`'s call, the glob is reserved here), the `compile.*` run-event vocabulary; tests incl. the reflection assertion that `EnvelopeStore` has append + reader only | CV-1 (Prepare's regions exist to fill); S2 | T2 | 3 | 4,127 / 6,000 (**Inferred** ×1.5 - a new bounded context with three vetoes) | Reds first: P-D1's fourteen tier inputs (no rule exists); the `Projection.Project(` named-call-site census (zero today; allowlist the three sites); the `LeaseDerivation.Derive(`/`Patterns(` named-site census with the `source_text` argument; the `RunBudget` named-member cap; ADR-0034's tests 1-3, 5, 6 (append-only by reflection; `prev_sha` chain; `FileShare.None`; containment cascade on Session delete; purge deletes the file only); the `HasMention` shared-regex test; the `projection_sha` domain test (an `operator` `task_class` row changes the sha); `GovernedRunRequest` **byte-identical** to today's for the same text; US-D12 signature stability. **Attended:** send a prompt and diff the compiled disclosure against the sent bytes; purge and confirm; `aide compile fold` recomputes the projection from real rows (P-D2). `docs/proof/mechanical-compile.md`. Data & Persistence and Security clear the store; Test Architect clears the census. The **E7 consistency test across surfaces** (Addendum C §B7, D US-D1) is this slice's: it closes the chain. | as CV-0 |
| **CV-3** Conversation lane · the compile call, the pin, gate 1, the eval harness (D-2; ADR-0035/0036) · **opus** | `src/AiDe.App/Conductor/CompileCallHost.cs` (new; `compile-call.compose`; the negative-reference census; no sibling ledger), `CompositionRootLedger.cs`, `src/AiDe.Core/AgentPlane/GoalBlock.cs` (`SpawnContract.AuthorizeBinding` factored; `Authorize` unchanged), `AcpLaneClient.cs`, `AcpEngineProcess.cs` (`LaneSessionOptions.Compile` = `tools: []`; the `settings` deny belt; `CLAUDE_CODE_EXECUTABLE` stripped; the pin triple verified per call), `src/AiDe.Core/Sessions/**` (the settings model reading `compile-pin-spike.json`; `agentic-advisory` selectable only with the installed adapter's sha), `tools/compile-eval/derive-fixtures.py`, `score.py` (`--affirm` on tracked paths; dedup by originating `called` row); tests | CV-2; **PD-5** (the artifact gate 1 reads) | T2 | 3 | 4,127 / 6,000 (**Inferred** ×1.5) | Reds first: the compile `session/new` exact-key-set wire test; `Roots == 0` for a compile; the `Conductor/Compile*.cs` negative-reference census (non-empty set asserted); `AuthorizeBinding`'s R0 case and the same `AP-0009..13` refusals from both entry points; the linked-deadline test (silent at `initialize`); the gate-1 artifact tests (missing / triple mismatch / recount ≠ 0); the report contract test (num/den only; the split witness; `--affirm`). US-D5, D8, D11 (b1, b2) headless. **Attended:** P-D8, P-D9 runtime (the hooks premise measured on a never-trusted fixture directory); prepare a prompt under advisory; keep one line and send; P-D4's first 50 begin. `docs/proof/compile-call.md`. Security clears the pin on the wire; AI Systems Engineer clears the harness. | as CV-0 |
| **CV-4** Conversation lane · admission's code: gate 2's reader, `ring.py`, the drift watermark (D-3's code only) · **sonnet** | `tools/compile-eval/ring.py`, the settings model's gate-2 reader (recompute every floor from num/den; never trust a verdict), `compile.mode.changed{from,to,trigger}`, the `readmitted_at` watermark; tests | CV-3 | T1 | 2 | 2,945 / 4,127 | Reds first: the drift watermark test; the canonicalisation fixture asserted by both sha implementations (C# and the pack's bundle test - the pack half is a finding for `ai-forward`); the A6 ring on a triple change. `docs/proof/compile-admission-code.md`. **The gate itself is not a track:** 50 scored + 50 holdout real envelopes accrue through operator use (weeks); `agentic` becomes selectable when `compile-eval-admission.json` meets the floors - a calendar event, recorded then. | as CV-0 |
| **DS-1** side track · `/design-slice` for the thread `ItemsControl` (review `ui-session-conversation` item 1: SC8 keyboard model, SC9 announcement policy) · **opus** | `docs/design/session-thread-itemscontrol.md` (new), `docs/notes/design-slice-*.md` it files, its audit and change-log entries | S1 (reads the binder's shape) - docs-only, no code dependency | T2 | 3 | 2,500 / 4,000 (**Inferred** by shape - a design node reading one review and one mockup; gap: no `design-slice` run measured here) | The design doc names the patterns (`ItemsControl` + `VirtualizingStackPanel`; the feed's paging; the mention picker's focus return; the once-only status and assertive-error announcement policy) with the Patterns-Expert-vs-Simplifier review recorded and the UX & A veto cleared; **the red-first oracles CV-1 lands** are written here by name (keyboard model; announcement policy; SC10's real ARIA properties). Reads `docs/mockups/session-conversation.html` and `DESIGN.md:974-1125`. | worktree `ai-de-side-design-slice`; writes under `docs/` only |
| **PD-5** side track · the wire spike: a pinned compile session yields zero tool calls with a repository `.mcp.json` (ADR-0035/0036 gate 1; Ruling 68 conditions) · **sonnet** + **the operator** | `spikes/compile-session-pin-wire/**` (fixture repo with permissive settings + `.mcp.json` stdio server; the pinned session; the hostile history line), `docs/proof/compile-pin-spike.md` + `compile-pin-spike.json` (adapter sha; `mcp__*` in the assertion) | S0 (`LaneSessionOptions`) - nothing else | T1 | 1 | 1,500 agent / 4,000; **attended ~30 min** | The frames log shows **zero** `tool_call` frames across the spike's prompts with and without the `settings` deny belt (the belt's key names and precedence are **Inferred** until this run); the artifact records the installed adapter's sha (0.75.1) and the pin triple. **A failed spike is a hard stop for every agentic rung, never a fallback.** Re-run on any adapter bump. Security reads the frames. | worktree `ai-de-side-pd5`; **isolation is the reason** (a real engine, a fixture repo, an attended run) |
| **X-1** side track · the census controls: INV-0008 phase 6 (census reach), DC-147's control (`tools/verify-mockup-audits.py` in CI), the four legacy mockups' `h_theme` error · **sonnet** | `tests/AiDe.App.ContrastProbe/**`, `tests/AiDe.App.Tests/**/ContrastFloorTests.cs` (the reach: disabled check/radio/menu-item states; hover and selected-inactive triggers), `tools/verify-mockup-audits.py` (new; fails on `Uncaught` in the console or a verdict strip still reading its placeholder), `.github/workflows/build.yml` (one appended step beside `verify-ui-craft-floor.py`), `docs/mockups/{app-facelift,context-map-join,knowledge-explorer,uml-erm-surfaces}.html` (the `ReferenceError` fix) | S0 (F5 also edits `build.yml`; merge first) | T1 | 1 | 2,945 / 4,127 | Red first: `verify-mockup-audits.py --self-test` plants a broken strip and must fail; the census reach adds rows that were unmeasured (count reported, not asserted). **Attended (operator decision, not code):** INV-0008 phase 7 - the `{colors.text-muted}` perceived-too-dim call; recorded as a finding and routed to the Shell lane as a `DESIGN.md` seam request, never applied here. `docs/proof/census-controls.md`. Test Architect clears the self-test. | worktree `ai-de-side-census`; writes under `tests/`, `tools/`, `docs/mockups/`, `.github/` |

**Owner roles.** Shell lane = "C# developer (App/Core) with the Tech Lead on the extraction"; Conversation
lane = "C# developer (Core) with Security on the send path"; the personas named per slice review in
Adversary Mode as read-only sub-agents and **do not count toward the width cap**. The author never
clears its own veto.

## Serial spine

What must not be parallel, and why each item fails GO5 (a data edge, a decision edge, or a shared
exclusive resource - all three must be absent for independence).

| item | why it cannot be parallel | who owns it |
|---|---|---|
| **S0 - merge `feature/exit-evidence` (@ `135e05e1`, 21 commits, 29 files, clean) into `main`** after F5's attended exit run | Ruling 51: Addendum C code branches from `main` only after F5 merges. **Data edge** to every track that opens a lane: `LaneSessionOptions` (`AcpLaneClient.cs:88` on that branch, not on `main` - Verified) is the typed tools argument CV-0's read-only pin, CV-3's `tools: []` and PD-5 all reuse; `GovernedRunHost.GovernedLaneSession` is the pattern CV-0 extends. F5 also edits `SessionConfig.cs`, `SessionConfigStore.cs`, `NewSessionSheetViewModel.cs`, `build.yml` - files S2, CV-1 and X-1 own next. The graph's one dangling link resolves here. **Attended:** the F5 exit run is one operator run away (Ruling 71 (a): the run is attended, the Proof Pack records the frame and every tool-call name). | conductor + operator |
| **S1 - merge `fix/session-document-render` (INV-0009 phases 1-4) into `main`** | **Shared exclusive resource:** the fix is live and **uncommitted** in `C:/Projects/ai-de-fix-session-document-render` (Verified 2026-09-11: `M ShellModeController.cs`, `WorkbenchShell.cs`, `MainWindow.xaml.cs`, `WorkbenchDiagnostics.cs`, `ExplorerModeTests.cs`, + 3 test files) - the exact files SH-2 owns. SH-2's first red *is* INV-0009's `DocumentOpening` seam, generalised; DS-1 reads the binder's shape; CV-1 owns `SessionComposerBinder`. **Decision edge:** INV-0009's phase 3 (F4: open the chooser's workspace before `opened`, or refuse) awaits an Owner ruling (the investigation's gate record) - **Flagged** from here; the conductor confirms before merging. | conductor + Owner |
| **S2 - the settings and sentinels commit** (T1, sonnet, in the primary's WT1 exception or a throwaway tree): `SessionConfig.cs` + `SessionConfigStore.cs` gain `fan_out_ceiling`, `budget_cap` (absent = subscription-bounded), `compile_mode` (default `mechanical-only`), `default_task_class` (default `free-form`) - **additive, an old file reads with defaults** (red first: the old-file-reads test); `TaskClasses.FreeForm` beside `ScoreSegment.Unclassified` in `Leaderboard.cs`; `RunBudget.SubscriptionBounded` (`Requests = int.MaxValue, Tokens = long.MaxValue`) with `Validate` accepting it (red first) | **Decision edge on a shared vocabulary:** CV-1 (the sheet), CV-2 (`PreCompile` reads the four settings; `ComposerSendContext.TaskClass`), SH-1 (nothing) and CV-0 (`SubscriptionBounded` in the compiled block) would otherwise each author `SessionConfig.cs` / `GoalBlock.cs` in the same wave. Fixing the vocabulary first is GO5's remedy: "until the interfaces are fixed, every track's result changes every other track's shape". The values are fixed by ADR-0033 and Rulings 56/72 - no judgement is left, hence sonnet. **The join after S2 verifies the driver** (the first join after this plan's `.gitattributes` change): the conductor observes `docs/_meta.json` resolve by regeneration rather than by conflict markers. | conductor (a T1 node) |
| **Inside each lane, the slices are serial** (SH-1 → SH-2 → SH-3; CV-0 → CV-1 → CV-2 → CV-3 → CV-4) | Each slice consumes the previous slice's types (a data edge) and shares its files (a shared exclusive resource). Two sessions in one lane would be two sessions in one layer - serial by default. | the lane |
| **The read-only turn ships first on the Coding side** (CV-0 before CV-1) | Ruling 73's SCOPE EFFECT: *the read-only turn is Coding's first slice, not its last - it is the 80% case's REPL.* CV-1 then renders the decoration line CV-0 defined; CV-2 later re-homes the shape decision into `Project()` with CV-0's wire test kept green. | Conversation lane |
| **Any change to a shared schema or vocabulary** (a `Perspective` row, a kind column, an event record, the `LaneSessionOptions` statics, a `DESIGN.md` token) | Lands in the owning slice **before** a consuming slice starts; a consumer that needs one mid-flight files a seam request and waits. | the owner |
| **`coord regen` and `regenerate-derived.py` at every join; `verify-test-run.py --update` at every join** | Two joins in flight would regenerate the same six derived files and recount the same floors twice - the conductor serialises joins in the primary checkout (the recorded WT1 exception). | conductor |

## Seams

Per shared surface: the owner, each **other** track's guard over it, and the joint satisfiability.
Every scan-shaped guard states its **root · recursion · token set · allowlist** (GO14a): widening
reddens at the join, narrowing stays green. A request crosses a seam as a message to the owner; the
downstream side rebases, the upstream side never has to ask.

| from -> to | the request | resolved by |
|---|---|---|
| SH-2 -> CV-1 | `PerspectiveShell`'s entry-verb transaction (`File → New Session` routes to host A and activates Coding, document-first) calls `NewSessionFlow` / `SessionComposerBinder.Bind(shell, config, providers, workspace, affirmation)` **as landed by S1** - a consume. If the transaction needs a signature change, SH-2 requests it; CV-1 lands it in its next commit. | CV-1's owner; the conductor sequences the join |
| CV-1 -> SH-2 | Three additive `ComposerPageTheme.Roles` tokens in `DESIGN.md` (review `ui-perspective-shell` item 2). **Guard (SH-2's):** `design-lint.py` and `ui-craft-gate.py` read `DESIGN.md` inward/outward; additive rows keep both green. | SH-2 appends the rows; CV-1 reads them after SH-2's commit |
| X-1 -> SH-2 | INV-0008 phase 7's palette decision (`text-muted` ink) as a finding; a token change if the operator rules one. | SH-2 (the `DESIGN.md` owner) |
| CV-3 -> X-1 (or the conductor after X-1 closes) | One appended CI step for `tools/compile-eval` (`score.py --self-test`). **Rule for `build.yml`:** append a named step at the end of the gates job; never edit another track's step block. | X-1 while live; else the conductor at the join |
| CV-2 -> SH-* | None expected. **Guard (CV-2's), stated:** the `Projection.Project(` census - root `src/`, recursive, token `Projection.Project(`, allowlist exactly `Presentation/Composer/ComposerCompiler.cs`, `Workbench/Composer/ComposerSendGate.cs`, `Cli/CompileFold.cs` (three sites). The Shell lane writes no `Projection.Project(` → **jointly satisfiable**. The `LeaseDerivation.Derive(`/`Patterns(` census - root `src/`, recursive, tokens `LeaseDerivation.Derive(`, `LeaseDerivation.Patterns(`, allowlist `ComposerSendGate.cs` and the display site, both with the `source_text` argument. Shell lane writes none → satisfiable. | - |
| CV-1 -> SH-* | **Guard (CV-1's), stated:** the `"free-form"` quoted-literal census - root `src/`, recursive, token `"free-form"`, allowlist `Watcher/Leaderboard.cs` (the constant's home, S2). The Shell lane and X-1 (`docs/`, `tests/`, `tools/` - outside the root) write none → satisfiable. | - |
| CV-3 -> SH-* | **Guard (CV-3's), stated:** the negative-reference census - root `src/AiDe.App/Conductor/`, non-recursive, files `Compile*.cs`, tokens `GovernedRunHost`, `governed-run.compose`, `SpawnContract.Authorize(`, allowlist none (the set of *hits* must be empty; the set of *files* non-empty). Nothing outside the lane writes under `Conductor/` → satisfiable. | - |
| SH-2 -> CV-* | **Guard (SH-2's), stated:** the DC-135 ratio watch - root `tests/`, recursive, tokens `new LayoutService(`, `new ZoneBackedLayoutService(`, allowlist none; the ratio must not widen from 66:8. The Conversation lane adds no layout construction → satisfiable. The `TheMenuCoversEveryCatalogCommand` successor reads `WorkbenchCommandCatalog.All` ∪ the perspective rows - the Conversation lane adds no catalog command in this horizon (a `Prompt` menu row is Design's existing table) → satisfiable. | - |
| every track -> conductor | A new defect class: append the entry with the id left as `DC-154` and a `**Status:**`; the conductor allocates the number at the join (contiguous family; two live trees hold the two ids after DC-147). A `tools/expected-test-counts.json` bump: never edit; the conductor runs `verify-test-run.py --update` at the join. A `*.csproj` change: request. | conductor |
| every track -> `docs/collaboration/session-contracts.md` §2 | Tracks never edit §2; the conductor applies the rows below at dispatch and retires them at converge (moves the files back to Core/Design). | conductor |

**Files with a lifetime hand-off inside one lane** (one owner at a time; the hand-off is the slice
boundary, recorded in the lane's audit entries): `ComposerSendGate.cs` and `ComposerCompiler.cs`
(CV-0 → CV-1 → CV-2), `GoalBlock.cs` (S2 → CV-0 → CV-3), `SessionDocumentSurface.cs` (S1 → CV-1 →
CV-2), `SessionConfig.cs` (S0 → S2 → CV-1 → CV-3's settings model).

## Struck tracks

The Simplifier's pass. Each was a candidate in the brief; each is struck because it does not clear
the multiplier - a parallel session is paid at roughly the orchestrator-worker rate (GO6: ≈ 15× a
single session's tokens for the fanned-out portion) and buys nothing but speed, which is last.

| track | why it was not worth its multiplier |
|---|---|
| **D-0 the envelope store** as its own track | One Core file plus its lifetime wiring and `aide session purge`; the fold reads it in the same session. Split, it saves ≈ 1,500 s of critical path for a second Data & Persistence context. **Folded** into CV-2 as the lane's first commit (the store lands before the fold that reads it). |
| **A third code lane: Compile (D-1 → D-2 → D-3) beside Coding (C-0 → C-3)** | GO5-admissible on paper (a new namespace, a one-way data edge on `Project`'s signature) - but the two lanes meet at `ComposerSendGate.cs` and `ComposerCompiler.cs`, so the Coding lane would carry the compile lane's App wiring as seam requests and the E7 chain would be closed by a node in a different session from the one that built the fold. Rigor equal at best, tokens ↑, speed ↑ - the lexicographic objective refuses it. **Merged** into the Conversation lane. Re-plan trigger (GO17): if the operator's wall-clock, not tokens, becomes the binding constraint, split at CV-2 with the App wiring assigned to CV-1's owner and the `Project(` census as the seam - `note-coordination-two-lanes-not-three` carries the cut. |
| **C-2 as a separate session** | Same aggregate as SH-2 (Perspective Layout: `WorkbenchLayout.Default(perspective)` is layout data; the Evidence pair is host B's content). A separate session would wait on SH-2 anyway. **Folded** as SH-3. |
| **Architecture-perspective kinds** | Rulings 54/59: existing kinds only (`classdiagram`, `sequence`, `contexts`, `joins`, `codeviewer`). That is five data rows in SH-1's allow-list plus Ruling 54's class-diagram scaling fix - one file. **Folded**: rows into SH-1, the fix into SH-3. |
| **D-3 admission** as a track | Its *code* is small and follows CV-3's harness (folded as CV-4, sonnet). Its *gate* is not work a session does: 50 scored + 50 holdout **real** envelopes accrue through operator use over weeks; the floors are fixed in advance and judged on the holdout. A track cannot manufacture that corpus without violating DC-127 (never fixtures alone). Recorded as a calendar gate in §Order of operations. |
| **The craft profile (`anthropic@1.0.0`)** | Pack-owned (`ai-forward` → `.claude/knowledge/craft-profiles/`), authored by `/collectknowledge` **in that repository**, with the manifest row and bundle test there. No authored path in this repository; compiles run with profile `none` until it exists (ADR-0037, Flagged). **Struck**; a next step for the pack. |
| **The composer phases 5-6 (INV-0007)** | Same files as CV-1 (`ComposerSurface.cs`, `composer.html`). **Folded** into CV-1. |
| **The contrast follow-ups as one track with the mockup sweep** | Kept as X-1 **only** for what is independent and mechanical (phase 6's census reach; DC-147's control; the legacy mockups' script error). Phase 7 is an operator decision, not code - an attended step of X-1 that yields a seam request. |
| **D-2's P-D5 spike inside CV-3** | Split the other way: the spike is the one case worktrees exist for - an attended, long-running run against a real engine in a fixture repository, whose result is a hard stop for every agentic rung. It depends on S0 only, so it runs early in the side slot rather than blocking CV-3 at its end. **Promoted** to PD-5. |

## Order of operations

The executable list. Width **≤ 3 concurrent writing sessions** (the standing contract; raised to 4
once today on the record - not here). A persona review is a read-only sub-agent and does not count.
`W` = the wave; a wave opens when its dependencies have merged to `main`.

| # | action | cost | why now |
|---|---|---|---|
| 1 | **Apply this plan:** merge `feature/addendum-c` (this file, the `.gitattributes` repair, the owed-marker removal) to `main`; restore or accept the primary's `.agents/regen-owed.txt` deletion; apply §"§2 rows" to `session-contracts.md` as moves; `verify-surface-ownership.py` green | conductor, ≈ 10 calls | The register must name the lanes before a lane commits; the attribute must exist before the first join that touches `_meta.json`. |
| 2 | **S0** F5's attended exit run, then merge `feature/exit-evidence` → `main`; `regenerate-derived.py`; `coord regen`; gates | operator ≈ 30 min attended; conductor ≈ 10 calls | Ruling 51; `LaneSessionOptions`; the graph's dangling link. |
| 3 | **S1** confirm INV-0009 phase 3's Owner ruling; the render-fix node commits phases 1-4; merge `fix/session-document-render` → `main`; `regenerate-derived.py`; `coord regen`; `verify-test-run.py --update`; gates | Owner (fable) 1 ruling; conductor ≈ 10 calls | SH-2's files; the binder; the `DocumentOpening` seam. |
| 4 | **S2** the settings and sentinels commit (T1, sonnet; one node); at its join **observe the driver resolve `docs/_meta.json` by regeneration** | 1 node, plan 1,500 / stop 2,945 (**Inferred**: below the implement median - additive fields with fixed values) | The shared vocabulary, fixed once. The driver's first verified join after the attribute repair. |
| 5 | **W1 (width 3):** `coord worktree new` × 3 → **SH-1** (opus) ∥ **CV-0** (opus) ∥ **DS-1** (opus, docs) | 3 nodes: 2,945 + 2,945 + 2,500 s plan | All three depend only on S0-S2; no shared file (§Seams); DS-1 must precede CV-1. |
| 6 | join each as it closes (serially, in the primary): `regenerate-derived.py` → `coord regen` → `verify-test-run.py --update` → gates; DC ids allocated | conductor ≈ 10 calls per join | The join rule; partial is acceptable - a failed node reports and its lane pauses, the others continue. |
| 7 | **W2 (width 3), as slots free:** **SH-2** (opus; after SH-1) ∥ **CV-1** (opus; after CV-0 **and** DS-1) ∥ **PD-5** (sonnet + operator; after S0; the side slot as soon as DS-1 closes) | 4,127 / 6,200 · 4,812 / 6,000 · 1,500 + 30 min attended | The two lanes' second slices; the spike early because its failure stops the ladder. |
| 8 | **W3:** **SH-3** (sonnet; after SH-2) ∥ **CV-2** (opus; after CV-1) ∥ **X-1** (sonnet; after S0; side slot after PD-5) | 2,945 · 4,127 / 6,000 · 2,945 | Lane order; the census controls when a slot is free (independent; CI6). |
| 9 | **W4:** **CV-3** (opus; after CV-2 **and** PD-5's artifact) - the Shell lane has converged; width falls to 1-2 | 4,127 / 6,000 | Gate 1 needs the spike's artifact; the harness ships with the first compile slice and before any agentic rung is selectable. |
| 10 | **W5:** **CV-4** (sonnet; after CV-3) | 2,945 | Admission's code, then the lane converges. |
| 11 | **Converge:** `coord worktree cleanup` (report first; `--remove` only for trees that are not primary, not a cwd, clean incl. untracked, carry no unique commit, unheld - **run by the conductor alone, never by a node**, DC-120); retire the §2 lane rows (files return to Core/Design); `/session-profiler` measures whether the division paid (planned vs actual per node) | conductor ≈ 20 calls | Cleanup is the half that rots; the profiler closes the loop this plan's budgets opened. |
| 12 | **Calendar gate (not a session):** after 50 real advisory envelopes are scored and the next 50 holdout meet §A14.4's floors, `compile-eval-admission.json` admits `agentic`; the A6 ring on every triple change; the drift detector demotes on regression | operator use; `score.py` runs | Ruling 68's order: spike → advisory → 50 → holdout 50 → agentic. A failed floor never falls back. |

**Fan-out contract for every wave:** width 3 · transient failure = report, never retry a node ·
per-branch exit = the track's exit evidence · join rule = partial is acceptable, the conductor
reports which lane paused · containment = one worktree per lane or side track, nothing writes under
another's paths, **no node runs a repo-wide destructive command** (`coord worktree cleanup`,
`git worktree prune`, `git stash`, a rebase), every node's audit entry carries `main_calls`.

**Span, Inferred** (the Conversation lane is the critical path): S0-S2 ≈ 1 h attended + 3 nodes;
then CV-0 (2,945) → CV-1 (4,812) → CV-2 (4,127) → CV-3 (4,127) → CV-4 (2,945) ≈ **19,000 s ≈ 5.3 h**
of node time plus five attended steps. The Shell lane (≈ 10,000 s) and the side tracks (≈ 7,000 s)
fit inside it. **Work** ≈ 36,000 s of node time. A one-session alternative runs the same order of
operations serially in one tree at ≈ 1× tokens and ≈ 10 h of node time; this plan spends the
multiplier on **isolation** (PD-5's real engine; the 2,967-line extraction away from the composer),
**context hygiene** (one lane holds the send path end to end, the other the docking mechanism) and
**the operator's attended steps interleaved across lanes** - not on speed.

## §2 rows, ready to apply

Applied by the conductor at dispatch (step 1) as **moves**: a path already in a `Core owns` /
`Design owns` table leaves that table for the horizon, or `verify-surface-ownership.py` fails on
"assigned to more than one owner" (`MainMenuBuilder.cs`, `CanvasSurface.cs`, `ClassDiagramSurface.cs`,
`ExplorerSurface.cs`, `JoinSurface.cs`, `WorkbenchShell.cs` today under Core/Design; `ComposerSurface.cs`,
`SessionDocumentSurface.cs`, `ConsoleSurface.cs` under Design). `derived` and `register` artifacts
carry **no owner** - the driver is the owner. Retired at converge (step 11).

### Shell lane owns

| Path | Why |
|---|---|
| `src/AiDe.Core/Workbench/Perspectives.cs`, `WorkbenchCommands.cs`, `LayoutModel.cs`, `ZoneLayout.cs`, `ZoneBackedLayoutService.cs`, `ZoneLayoutStore.cs` | The Perspective registry and the per-host layout aggregate (ADR-0030/0032) |
| `src/AiDe.App/Workbench/SurfaceContentFactory.cs`, `PerspectiveMenu.cs`, `MainMenuBuilder.cs`, `CommandPalette.cs` | The kind rows' allow-list columns and everything derived from the join (ADR-0030) - **the `MainMenuBuilder.Layout` carve-out is suspended**: the mapping moves onto the catalog entry in SH-1 |
| `src/AiDe.App/Workbench/WorkbenchShell.cs`, `DockHost.cs`, `PerspectiveShell.cs` (né `ShellModeController.cs`), `WorkbenchController.cs`, `LayoutPersistence.cs`, `WebSurfaceHost.cs`, `WorkbenchDiagnostics.cs` | The second host, the presenter, the slots (ADR-0031/0032) |
| `src/AiDe.App/MainWindow.xaml`, `MainWindow.xaml.cs`, `DockRoundedTabs.xaml`, `DESIGN.md` | The rail, the title, the status strip, the tab states, the tokens |
| `src/AiDe.App/Workbench/CanvasSurface.cs`, `CanvasPage.cs`, `ClassDiagramSurface.cs`, `JoinSurface.cs`, `ExplorerSurface.cs` | SH-3's content: the Evidence pair, the scaling fix, Ruling 59's check; Explore unchanged by ruling |

### Conversation lane owns

| Path | Why |
|---|---|
| `src/AiDe.App/Workbench/Composer/**` (`ComposerSurface.cs`, `ComposerSendGate.cs`, `ComposerPageContract.cs`, `ComposerPageTheme.cs`), `src/AiDe.App/Web/composer.html`, `composer.mjs` | The composer as a conversation; the send path (US-C13; ADR-0033) |
| `src/AiDe.App/Workbench/Sessions/SessionDocumentSurface.cs`, `ConsoleSurface.cs`, `NewSessionSheetDialog.cs`, `NewSessionFlow.cs`, `SessionComposerBinder.cs` | The thread, the folded Console, the sheet, the one binding site |
| `src/AiDe.Core/Presentation/Composer/**`, `src/AiDe.Core/Presentation/Sessions/NewSessionSheetViewModel.cs`, `src/AiDe.Core/Sessions/**` | The compiler's render site, the draft, lease derivation, the sheet's model, session settings and the compile-mode ladder's settings model |
| `src/AiDe.Core/Compilation/**`, `src/AiDe.App/Cli/**`, `tools/compile-eval/**` | The Prompt Compilation bounded context, its CLI verbs and its eval harness (ADR-0033-0036) |
| `src/AiDe.Core/AgentPlane/GoalBlock.cs`, `LeaseAndSeams.cs`, `AcpLaneClient.cs`, `AcpEngineProcess.cs`, `src/AiDe.App/Conductor/**` | The read-only shape, `AuthorizeBinding`, the pins, `CompileCallHost` (Rulings 71/73; ADR-0035) |
| `src/AiDe.Core/Watcher/Leaderboard.cs` and the watcher store's schema | `TaskClasses.FreeForm`; the `task_class_source` expand-only column (ADR-0028 amendment) |

### Side tracks own

| Path | Why |
|---|---|
| `docs/design/session-thread-itemscontrol.md` and the notes DS-1 files | DS-1 |
| `spikes/compile-session-pin-wire/**`, `docs/proof/compile-pin-spike.md`, `compile-pin-spike.json` | PD-5 |
| `tests/AiDe.App.ContrastProbe/**`, `tests/AiDe.App.Tests/**/ContrastFloorTests.cs`, `tools/verify-mockup-audits.py`, `.github/workflows/build.yml`, `docs/mockups/{app-facelift,context-map-join,knowledge-explorer,uml-erm-surfaces}.html` | X-1 |

### Shared, and therefore rule-bound (additions for the horizon)

| Path | Rule |
|---|---|
| `tests/AiDe.App.Tests/**`, `tests/AiDe.Core.Tests/**` | Whoever owns the file under test owns its test (unchanged) |
| `tools/expected-test-counts.json` | Never hand-merged; the conductor recounts at every join with `verify-test-run.py --update` |
| `docs/lessons/defect-classes.md` | Append only; **the id is allocated by the conductor at the join** (contiguous family; DC-013) |
| `.github/workflows/build.yml` | X-1 owns; others append a named step by request; never edit another's block |
| `docs/collaboration/session-contracts.md` | Conductor edits; tracks request |

## Harness qualification

What each track **needs** from a delegation mechanism, and what the harness here is **qualified** to
do - from what was verified in `coord-core.py`, the shared `.git/hooks`, `.claude/settings.json` and
today's audit log. `enforced` = a hook or commit-time check refuses; `observed-only` = recorded, not
refused; `unsupported` = no code path wired here. No mode is advertised that was not exercised.

| need | mechanism | qualification | evidence |
|---|---|---|---|
| one tree per agent | `coord worktree new <name> --session <id>` (`git worktree add -b`, sibling of the primary) | **observed-only** (creates the tree, records `session-start`; refuses nothing) | `cmd_worktree` `:1861-1897`; 7 trees exist today |
| a stated division of responsibility | leases: `coord claim` events folded on read (`fold`, TTL 300 s); `AGENT_SESSION` is the identity | **observed-only** as a record | `:139-193`, `:220-272`, `:505-507` |
| refusal of a write across the division | `coord precommit` via the shared `.git/hooks/pre-commit` (`exec`s the primary's script) - exit 3 on `deny`, exit 4 when `AGENT_SESSION` is unset | **enforced** at every commit, in every worktree and harness (the commit floor) | hook read directly; `:1398-1433`, `:606-612` |
| refusal at edit time (PreToolUse) | `coord hook` (`:1242-1400`) | **unsupported here** - not wired in `.claude/settings.json` (0 refs); `.claude/hooks/` absent; `doctor`'s "enforcing" line is a capability constant from a spike not reproducible in this history | `.claude/settings.json`; `HARNESS_STATUS` `:1180-1200` |
| a receipt back | the sub-agent's final report + its `kind:skill` audit entry with `duration_seconds`, `goal`, `done_when` (AL5b) + `episode-close` with `episode.artifacts` | **observed-only** (`verify-audit-log`, `verify-stranded-audit`, `verify-audit-capture` enforce the log's integrity and presence, not the receipt's truth - the conductor verifies independently) | `audit-log.jsonl` fields; `build.yml` gates |
| merge at the join without a human | the drivers: `coord-regen` (regenerate, record owed) / `coord-register` (union) | **enforced** by git for the 11 registered patterns (11/11 attributed after this plan); **`coord regen` must follow** (an owed artifact is stale until then) | `.gitattributes`; `doctor` |
| fail-safe cleanup | `coord worktree cleanup` (`worktree_safety` hard stops: primary, cwd, git-locked, live session, dirty incl. untracked, unique commits, branch elsewhere); `--remove` opt-in | **enforced** in code; **repo-wide** - the conductor alone runs it (DC-120) | `:1818-1850`, `:1933-1966` |
| the lease crossing a worktree | `git stash` (a repository-global stack), `bisect`, notes, config, hooks | **forbidden** (WT13); nothing mechanises it | `session-worktree-discipline` WT13 |
| model per node | Claude Code sub-agents: opus / sonnet per node (`model` on dispatch) | **observed-only** (the audit entry names the actor) | today's entries carry `actor` |

## Disconfirm (the gate)

- **Simplifier (soft veto):** twelve candidates → two lanes and three side tracks; seven struck with
  the merged alternative named (§Struck tracks). The third code lane was the strongest candidate and
  fails on the lexicographic objective, not on GO5. Cleared.
- **Test Architect (hard veto):** every track row carries red-first oracles from the architecture's
  planned-reds table, a named Proof Pack file, and - where a runtime claim is made - the attended step
  that records it. CV-4's gate is explicitly *not* a track because its evidence cannot be manufactured
  (DC-127). D-3's holdout is judged on real rows. Cleared by the reviewer, not the author: the conductor
  confirms at dispatch.
- **Data & Persistence:** every track boundary is an aggregate boundary (Perspective registry; Perspective
  Layout per host; the Envelope fold + store; the compile call's binding) - no track splits an aggregate;
  the send-gate meeting point is inside one lane. The expand-only columns (`task_class_source`; the four
  settings) are on the spine with old-file-reads tests. Cleared.
- **SRE:** the two attended runs against real machinery (F5's exit run; PD-5) are isolated in their own
  trees; budgets carry a *stop* value and the cap firing is a defect signal; every node's audit entry
  records `main_calls` (the missing instrumentation named). Cleared.
- **Tech Lead (casting vote on count):** width 3 as contracted; two code lanes the team can hold; the
  Shell lane's extraction gets opus and the largest budget; the operator has at most one attended step
  in flight at a time. **Count: 2 lanes + 3 side tracks.**
- **Enterprise Architect:** the bounded contexts match ADR-0033's map (Shell presentation; Prompt
  Compilation Conformist to the Agent Plane, Shared Kernel with Sessions); the pack-owned craft profile
  is outside this repository and struck. Cleared.

## Status

| Completed | Remaining | Best next action |
|---|---|---|
| Layer state measured (`doctor`, `pack-doctor`); `docs/_meta.json`'s missing merge attribute repaired after running its generator; the stale owed marker regenerated (no diff) and removed on this branch; artifact classes recorded; 2 lanes + 3 side tracks cut on aggregate boundaries with one owner per file; the 3-node spine; seams with stated guards; 7 struck tracks; budgets from today's measured node costs; harness qualified from the code and the hook; §2 rows ready; this `.md` + `.html`; two decision notes | The conductor's step 1 (apply the §2 rows; restore or accept the primary's `regen-owed.txt` deletion; merge this branch); S0-S2; then W1 | `/execute-with-coordination docs/coordination/addendum-cd.md` after S2 has merged - it parses this file; dispatch W1 at width 3 |
