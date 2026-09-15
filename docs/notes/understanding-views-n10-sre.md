---
id: note-understanding-views-n10-sre
title: "N10 D-0 Solution tree SRE: PASS-WITH-CONDITIONS; design stays draft"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "understanding-views"
tags: [decision-note, addendum-c, understanding-views, N10, D-0, solution-tree, sre, telemetry]
links:
  - { to: design-solution-tree, rel: relates-to }
  - { to: spec-understanding-views, rel: relates-to }
  - { to: adr-0038-d0-solution-tree-census-and-kind, rel: relates-to }
  - { to: note-understanding-views-n10-design-acceptance, rel: relates-to }
  - { to: note-understanding-views-owner-n14, rel: depends-on }
  - { to: proof-uv-0-solution-tree-core-query, rel: relates-to }
review-by: 2027-03-15
review-suggested:
  - { by: design-solution-tree, on: 2026-09-15, reason: "N10 SRE PASS-WITH-CONDITIONS; design stays draft; O12 tag table incomplete in TelemetryTests; production cancel not wired; caps stay Inferred" }
summary: >-
  N10 SRE & Systems Diagnostician (advisory) on D-0 Solution tree: PASS-WITH-CONDITIONS.
  Operator questions are named and mostly emitted on the success path. Caps 2000/5000 stay
  honestly Inferred. Cancel throws, not a partial tree, but the shell never cancels.
  Blast radius: no design Accepted, no D-1, no main.
---

# N10 D-0 Solution tree SRE: PASS-WITH-CONDITIONS; design stays draft

*A decision note (`knowledge-visualization.md` V17): below ADR weight, above chat-scrollback
weight. One note per call; written before the session that made it closes.*

- **Kind:** decision
- **Confidence:** **Verified** on tags emitted, cancel-throws, Disclosure copy, and the Inferred cap label; **Inferred** on overlapping fire-and-forget Show race; **Flagged** on production sampling of "how often"
- **Made during:** N10 design adversarial, Adversary Mode, SRE lens, session `understanding-views-n10`, 2026-09-15. Subject HEAD `ac584d9b08b3bf1a085aaf99f3454faf83e45911` (`understanding-views`). Reviewer did **not** author `docs/design/solution-tree.md`.

This receipt is **SRE & Systems Diagnostician (advisory)** only. N10 Test Architect + Simplifier already sat (`note-understanding-views-n10-design-acceptance`). Patterns Expert is still unsat. A partial panel does not close the N10 gate. This note does not accept the design.

## Evidence opened (not memory)

| Artifact | Opened | Used as |
|---|---|---|
| `docs/design/solution-tree.md` | census walk `:255-264`; error/concurrency `:319-327`; failure-mode table `:330-370`; telemetry O1–O13 `:491-517`; test plan cancel/telemetry `:552-556`; Flagged caps `:633` | subject |
| `docs/adr/0038-d0-solution-tree-census-and-kind.md` | Instrumentation IO1 `:172`; Bounds 2000/5000 **Inferred** `:160`; honest shortfall table `:144-155` | authority |
| `.claude/knowledge/observability-and-instrumentation.md` | O1–O13 | standard |
| `.claude/knowledge/instrumentation-over-inference.md` | IO1–IO11 | standard |
| `src/AiDe.Core/Projections/SolutionTreeProjection.cs` | cap constants `:60-67`; `Compute` clamp `:185-186`; `WalkCensus` CT + Disclosure `:269-340` | walk |
| `src/AiDe.Core/Projections/ProjectionService.cs` | `ActivitySource("aide.projection.query")` `:198`; `SolutionTree` span/outcome `:687-750`; `TagSolutionTree` `:786-818`; `FramedCost` `:1531-1538` | emit |
| `src/AiDe.Core/Projections/GraphProjection.cs` | `DefaultMaxNodes = 5_000` `:191` | cap alignment |
| `src/AiDe.Core/Ipc/WorkspaceOperations.cs` | Register `(request, _)` `:239-241` | daemon CT |
| `src/AiDe.Core/Ipc/DaemonEndpoint.cs` | handler `Func<IpcRequest, IpcPeer, IpcResponse>` `:42` | no CT on IPC |
| `src/AiDe.Core/Projections/IWorkspaceQueries.cs` | `LocalWorkspaceQueries` passes CT `:166-168` | in-process |
| `src/AiDe.App/Workbench/WorkbenchShell.cs` | `CancellationToken.None` `:2208`; `_ = PopulateSolutionTreesAsync` `:2167`, `:2178`, `:2265`; `_queries is null` → `ShowNoWorkspace` `:2196-2203` | product path |
| `tests/AiDe.Core.Tests/SolutionTreeProjectionTests.cs` | `CancelMidWalk_…` `:386-400`; `SolutionTreeSpan_EmitsCountsNotPaths` `:404-428` | O12 half |
| `tests/AiDe.Core.Tests/TelemetryTests.cs` | whole file; no `SolutionTree` call | O12 gap |
| `docs/proof/uv-0-solution-tree-core-query.md` | Claim 7 residual `:73-79`; caps Inferred `:71` | pack |
| `docs/notes/understanding-views-n10-design-acceptance.md` | panel incomplete; production caps Inferred in residual | sibling |

`TelemetryTests.cs` contains **zero** `SolutionTree` / `solution-tree` strings. **Verified** grep this turn.

## The four questions (IO2)

| Question | Named emitting source (design `:495-509`) | Emitted on the normal path? | Confidence |
|---|---|---|---|
| How long? | `Activity` duration on `aide.projection.query`; recorded, not CI-asserted (ADR-0029) | **Yes** — span is started for every `ProjectionService.SolutionTree` (`:695`). No extra flag. | **Verified** |
| How often? | Count of spans with `projection=solution-tree`. No new `Meter` (design O10 / ladder). | **Partial** — occurrence is the span. No counter. Same pattern as Graph. Production sampling is unstated. | **Flagged** |
| How much? | `returned.census_folders` / `file_artifacts` / `indexed_parent` / `unindexed`; `skip.omitted`; `omitted.by_cap`; `returned.bytes` | **Yes** on success — `TagSolutionTree` (`:802-809`) always sets these from the result, including a real observed 0. | **Verified** |
| Which path? | `projection`; `shortfall.causes` (omit if none); `shrunk.attempts`; `outcome` | **Yes** on success. `shortfall.causes` omitted when `Disclosures.Count == 0` (`:811-818`) — not a fake empty string. | **Verified** |
| Did it fail? | `outcome` = `ok` / `canceled` / error code | **Partial** — `ok` after a returned result (`:743`); `canceled` on OCE then rethrow (`:746-749`). Non-OCE exceptions leave **no** `outcome` tag (omit, not `ok`). Design named an error code; the span does not set one. | **Verified** (omit) / **Flagged** (no error code) |

Logs named in the design (`Solution tree {CensusFolders} folders…`, `:511`) are **not** emitted. `ProjectionService` has no `ILogger`. Graph does not log either. Tags are the signal (O10). Nit: drop the example from the design or emit a `LoggerMessage` inside the Activity (O1–O2).

## Caps 2000 / 5000 — is **Inferred** still honest?

**Yes.** Do not promote.

| Claim | Opened | Label |
|---|---|---|
| `DefaultMaxCensusFolders = 2_000` | `SolutionTreeProjection.cs:64` comment: "Inferred — no measured census cardinality yet" | **Inferred** |
| `DefaultMaxFileArtifacts = 5_000` | `:67` "Aligned with `GraphProjection.DefaultMaxNodes`" | **Inferred** (by alignment) |
| Graph `DefaultMaxNodes = 5_000` | `GraphProjection.cs:191` | **Verified** constant; Graph remarks say no measured repo reached it as a *count* ceiling; bytes at 5000 **have** been measured to overflow (`ProjectionService.cs:597-611`) |
| Design / ADR / architecture / UV-0 pack | design `:190`, `:633`; ADR `:160`; pack Claim 6 residual `:71` | still **Inferred** |

UV-0 now **emits** `returned.census_folders` and `omitted.by_cap`. That closes the *instrumentation* gap that forced the model. It does **not** close the *measurement* gap: this turn opened no observed cardinality from a representative workspace. IO7 holds: the number stays labelled, the gap is named ("no measured folder-census cardinality"). IO3 forbids reading the constants as if they were measured.

Honesty mechanism while Inferred: count clamp + ranked shrink + `Omitted (N)` derived from the only omit integer (`OmittedByCap`). A huge repo is truncated in daylight, not silently. Failure-mode row `:368` already accepts this.

**Condition:** retune only after reading emitted counts (and `omitted.by_cap`) off a real run. A green test is not a census size.

## Cancel mid-walk

**Core walk: throws, not a partial tree. Product path: never cancels.**

- `WalkCensus` calls `cancellationToken.ThrowIfCancellationRequested()` at each directory (`SolutionTreeProjection.cs:281`). IO/permission become Disclosure; cancel does not.
- `ProjectionService.SolutionTree` catch: `outcome=canceled`, rethrow. No `TagSolutionTree`, so counts are **omitted**, not zeros on a canceled walk (IO8).
- `CancelMidWalk_ThrowsOperationCanceled_NotAPartialTree` **Verified** — hook cancels and throws; no result returned.
- `LocalWorkspaceQueries` passes the token (`IWorkspaceQueries.cs:166-168`). Unique among the adapters: `GraphAsync` ignores its token (`:157-158`).
- Production shell: `SolutionTreeAsync(new SolutionTreeQuery(), CancellationToken.None)` (`WorkbenchShell.cs:2208`). Refresh/bind/retry all fire-and-forget `_ = PopulateSolutionTreesAsync` (`:2167`, `:2178`, `:2265`).
- Daemon: `Register` callback is `(request, _)` where `_` is `IpcPeer`, not a `CancellationToken` (`WorkspaceOperations.cs:239-241`; `DaemonEndpoint.cs:42`). Client `WorkspaceClient.SolutionTreeAsync` forwards CT to `QueryAsync`; the handler never sees it.

So the design promise "cancel → OCE, not empty success" is **true of the projection** and **false as an operator gesture**. Overlapping refreshes are not canceled; an older `Show(result)` can land after a newer one. That race is **Inferred** (code permits it; no test fired it).

Join `FilesToSearch` / shrink do not poll CT (`ProjectionService.cs:705-740`). Cancel during join is ignored until the walk. Minor.

## Failure modes — not-recorded, or a plausible wrong tree?

| Mode | Tree | Telemetry | Test | Confidence |
|---|---|---|---|---|
| `IOException` / `UnauthorizedAccessException` | no node; Disclosure `Not recorded` cause Io / Permission | `shortfall.causes` includes the enum name | T5a / T5b DTO | **Verified** |
| Reparse / junction | no node; do not descend; cause `ReparsePoint` | same | junction test | **Verified** |
| Count cap / shrink | named paths absent; `Omitted (N)` from `OmittedByCap` | `omitted.by_cap`; `shrunk.attempts` | T5c; `HostileCensus_ShrinksUnderTheFrame` | **Verified** |
| Skip-listed dir | no node; skip-count | `skip.omitted` (0 is real) | F* `bin` | **Verified** |
| Cancel | no result; OCE | `outcome=canceled`; counts omitted | cancel fact; **outcome tag untested** | **Verified** throw / **Flagged** tag |
| IPC / store throw | App `ShowError`; not a Disclosure | `outcome` omitted (not `ok`) | App catch `:2214-2219` | **Verified** omit |
| No workspace | App does not query; `ShowNoWorkspace` when `_queries is null` | no span | US-T9 B5 path | **Verified** App |
| Missing root if Core is called anyway | empty `Nodes` (walk skipped); **no** Disclosure | `outcome=ok` and **zero counts** | none | **Flagged** — zeros here are "did not walk", not "walked and found 0". Empty-but-existing root emits census-folder `""` (1 folder). Distinguishable, still a plausible success. App is supposed not to call. |
| Telemetry path / prompt | — | tags are counts and enum names; `shortfall.causes` is `Cause.ToString()`, never `Path` | `SolutionTreeSpan_…` rejects `path` in keys and workspace root in values | **Verified** for this span. `TelemetryTests.NoSpanAttribute_…` still does **not** call `SolutionTree` (pack Claim 7 residual). |

Census shortfalls are not ERROR logs (O5). That matches the design.

**Not a BLOCK.** The walk's failure modes disclose or throw. The remaining lie-shaped risks are (1) Core missing-root zeros if called, (2) overlapping Show of an older census — both conditions, not an undebuggable hole.

## O1–O13 (this unit)

| Dir | Result |
|---|---|
| O1–O2 | No structured log. Span exists; a log inside it was designed and not shipped. |
| O3 | IPC handler does not take W3C context / CT. Repo-wide daemon shape, not unique to D-0. |
| O4 | One span per `SolutionTree` invocation. |
| O5 | Disclosures are not ERROR. Uncaught projection throw has no severity on the span. |
| O6 | Repo projection tag names, not OTel HTTP semconv. Conforms to existing Graph/Evidence tags. |
| O7 | IPC uses `IpcErrorCodes`. Census shortfalls are causes, not codes (named). Non-OCE span has no code. |
| O8 | N/A (no HTTP). |
| O10 | Traces + tags; no extra Meter. Correct ladder. |
| O11 | No workspace path / source text in tags. **Verified** on the solution-tree span test. |
| O12 | **Incomplete.** Design `:517` / implementer list `:609` named `TelemetryTests`. The case landed in `SolutionTreeProjectionTests.SolutionTreeSpan_EmitsCountsNotPaths` and asserts only `projection`, `returned.census_folders`, `returned.file_artifacts`, `skip.omitted`, `outcome=ok`, and privacy. It does **not** assert `returned.indexed_parent`, `returned.unindexed`, `omitted.by_cap`, `returned.bytes`, `shrunk.attempts`, omit-if-none `shortfall.causes`, or `outcome=canceled`. |
| O13 | Tag values are counts, a small enum-name set, and `ok`/`canceled`. Low cardinality. |

## The call

**PASS-WITH-CONDITIONS.** Advisory lens: no hard veto. Not undebuggable. No unmet latency SLO (none was stated; duration is recorded, not asserted).

`docs/design/solution-tree.md` **stays `status: draft`**. Do **not** mark it Accepted from this receipt. Do **not** admit D-1. Do **not** join `main`.

```
PERSONA: sre-diagnostician   MODE: Adversary   TIER: T2
VERDICT: PASS-WITH-CONDITIONS
FINDINGS:
  - [Major] (Verified) O12 is incomplete against the named tag table. TelemetryTests never calls SolutionTree. SolutionTreeSpan asserts 4 of 11 tags and never asserts outcome=canceled or omit-if-none shortfall.causes.  evidence: TelemetryTests.cs grep; SolutionTreeProjectionTests.cs:418-427; design :495-517, :609; UV-0 pack Claim 7 residual  fix: assert the full table on a F* run and a cancel run; invoke SolutionTree from the privacy test (or keep one test file, but cover the contract).
  - [Major] (Verified) Production cancel is not wired. Shell always passes CancellationToken.None; daemon Register has no CT. Core walk can throw; the operator cannot cancel; refresh is fire-and-forget.  evidence: WorkbenchShell.cs:2208, :2167; WorkspaceOperations.cs:239-241; DaemonEndpoint.cs:42  fix: CTS per in-flight query, cancel on refresh/dispose; or document cancel as Core-only and do not advertise it. Never return a partial tree.
  - [Major] (Inferred) Overlapping PopulateSolutionTreesAsync can Show an older result after a newer one, with no generation. That is a plausible wrong tree, not "not recorded".  evidence: three `_ = PopulateSolutionTreesAsync` call sites; no in-flight token  fix: single-flight or monotonic generation; drop stale completions.
  - [Minor] (Verified) Non-OCE failures omit `outcome` rather than writing a stable error code the design table named. Omit is honest (IO8); the named tag is missing.  evidence: catch is OCE-only :746-749  fix: set outcome to the Ipc/Projection code in a catch-all before rethrow, or keep omit and strike "error code" from the design table.
  - [Minor] (Flagged) Core missing-root still returns outcome=ok with zero counts if called. App prevents when `_queries is null`. Distinguishable from root-only (1 folder), still a success-shaped empty.  evidence: Compute :170-174; TagSolutionTree always sets zeros  fix: omit count tags (or outcome=not_recorded) when the walk did not run.
  - [Nit] (Verified) Designed structured log template is not emitted. Graph also logs nothing.  evidence: no ILogger in ProjectionService  fix: drop the example or add LoggerMessage inside the Activity.
  - [Nit] (Verified) Design cites ProjectionService.Clamp at :1393; Clamp is at :1585 and SolutionTree uses Math.Clamp in Compute instead. Same bounds.  fix: authors retarget the citation on the next design edit.
CLEARS-THE-VETO: n/a (advisory) — not undebuggable; no unmet stated perf budget. Unresolved Blocker count: 0.
RESIDUAL RISK: production sampling of "how often"; daemon CT (repo-wide); join/shrink ignore CT; caps 2000/5000 unmeasured; overlapping Show race until C-SRE-3; TelemetryTests privacy does not exercise this span.
```

## Alternatives dismissed

- **BLOCK.** Operator questions are named and the success path emits duration, volume, path, and ok/canceled. Failure modes disclose or throw. Caps are still labelled Inferred. A Blocker here is "how would we even debug this?" — we would read the span. Incomplete O12 and unwired cancel are conditions, not an absent measurement path.
- **PASS and accept the design.** O12 is a named contract with a hole. Caps are not measured. Cancel is not an operator path. Accepting would mint a second definition of "telemetry done".
- **Promote 2000/5000 to Verified because tags exist.** Emitting a count is not having read a representative cardinality (IO3). The label is the honesty.
- **Treat HandleKey/App None as "cancel works".** Core test ≠ product cancel.
- **Admit D-1 / join `main` / self-clear.** Owner N14; this reviewer did not author the design.

## Conditions (must hold for a later accept; this receipt does not accept)

1. **C-SRE-1.** Load-bearing tag table asserted: all named tags on a happy F* span; `shortfall.causes` omitted when there are no disclosures; `outcome=canceled` on the cancel fact; privacy test actually runs `SolutionTree`. File name may stay `SolutionTreeProjectionTests` if the contract is covered.
2. **C-SRE-2.** Production caps **stay Inferred** until emitted `returned.census_folders` / `omitted.by_cap` are read from a representative workspace and the constants retuned (or explicitly kept). Do not advertise 2000/5000 as measured.
3. **C-SRE-3.** Either wire a cancelable in-flight query (cancel prior walk on refresh/dispose; no overlapping Show) **or** keep cancel as a Core-only fact in the design Flagged table. Daemon CT is a later hosting slice (ADR-0009 in-process first) — name it, do not pretend Register already has it.
4. **C-SRE-4.** Non-OCE: `outcome` is a stable code **or** the design table drops "error code" and keeps omit. Never `ok` after a throw.

N10 Test Architect C1–C5 are not this lens. They still block accept.

## Validation condition

Holds until a later N10 panel (this receipt + Test Architect + Patterns Expert) records PASS or PASS-WITH-CONDITIONS **and** a non-author marks `docs/design/solution-tree.md` Accepted — or until Owner supersedes N14. Emitting tags without reading them does not retune the caps.

## Promotion rule

This note is the N10 SRE receipt. It does not supersede ADR-0038. It does not accept the design. A later accept of `design-solution-tree` links `relates-to` this note.

## What the Conductor may do next

Keep D-0 residuals on `understanding-views` (C-SRE-1–4, TA C4 Ctrl+Enter Flagged, Patterns Expert). Do not mark the design Accepted from this note.

## What the Conductor must not do

Mark `docs/design/solution-tree.md` Accepted. Admit D-1…D-6. Join `main`. Promote 2000/5000 to Verified. Treat `CancellationToken.None` as cancel-proven. Author Atlas / `Understanding/**`. Convene this receipt as a full N10 panel close.

## Gate record

`GATE design · 2026-09-15 · N10 SRE & Systems Diagnostician (advisory) · Adversary Mode · subject ac584d9b docs/design/solution-tree.md + SolutionTreeProjection/TagSolutionTree · verdict: **PASS-WITH-CONDITIONS** · design **stays draft** · Blockers: 0 · panel still incomplete (Patterns Expert unsat) · authors did not self-clear · reviewer did not accept the design`
