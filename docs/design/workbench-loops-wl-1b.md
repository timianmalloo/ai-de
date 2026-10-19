---
id: design-workbench-loops-wl-1b
title: "Design: WL-1b — obligations, evidence and acceptance (folds, guarded commands, minimal checklist kind)"
type: design
status: draft
owner: "@timianmalloo"
phase: "workbench-loops / WL-1b"
tags: [design, workbench-loops, wl-1b, checklist, obligation, evidence, disposition, acceptance, closure, fold]
links:
  - { to: architecture, rel: implements }
  - { to: adr-0039-workbench-records-durable-representation, rel: implements }
  - { to: adr-0040-journey-purpose-identity, rel: implements }
  - { to: adr-0045-perspective-set-factory-review-understand, rel: depends-on }
  - { to: spec-workbench-loops-ux, rel: depends-on }
  - { to: design-workbench-loops-wl-1a, rel: depends-on }
  - { to: design-workbench-loops-wl-s, rel: depends-on }
review-by: "2026-11-18"
summary: >-
  The detailed design of WL-1b. It adds no table, column or trigger: the v2 rows and their raw writers and
  readers already exist. It adds two pure folds (acceptance eligibility and work closure), one Core service
  that guards every write and re-checks eligibility inside the writer transaction, five IPC operations, a
  P1-PERF-style 100x bench, and a minimal checklist kind. No WL-1b disposition is an adoption, so WL-1b owes
  no v3 migration. It cuts four tracks; three are Core-only and start at once.
---

# Design: WL-1b — obligations, evidence and acceptance

Track T22 (`wl1b-design`), the plan-refresh node of wl-leader3. Base: `origin/main` `e8944ebc`. Every
`file:line` below was opened on that base on 2026-10-19. Anything not opened is labelled **Inferred** with an
`assume:` line.

## Responsibility

One responsibility: **answer "may this work be accepted, and if not, which gap blocks it?" from facts, and
write an acceptance only when that answer is yes.** Acknowledgment is a row. Satisfaction is a fold.
Acceptance is a row written only after the fold says yes, inside the same writer transaction.

## Grounding (quoted, DC-189)

- `docs/architecture.md:1504` (§WL.11, WL-1b): proves "Evaluate one mandatory obligation, link evidence, see
  acceptance blocked with the named gap, record a disposition, accept the declared deliverable". Real:
  "`checklist_obligation_fact` (+`mandatory`), `evidence_link_fact`, `disposition_fact` (obligation/work/intent
  kinds), the acceptance-eligibility and closure folds, a minimal `checklist` kind". Mocked: "assessments
  absent". Demo: "accept a plan deliverable without claiming implementation (AC-W03c)". E2E red first:
  "acknowledged ≠ satisfied (a row vs a fold); acceptance blocked while a mandatory obligation lacks a current
  link; saved review state is not acceptance; FK/trigger overhead measured P1-PERF-style at 100×".
- ADR-0039 §1 (`0039-…md:83`): evidence link = "one link from one claim/criterion revision to one artifact or
  attempt". `:85`: checklist obligation "`obligation_id`, `checklist_id`, `revision`, `text`, `mandatory`…
  The row's existence is 'proposed'". `:86`: disposition "one actor's disposition on one subject at one
  baseline … **Satisfaction is never a disposition** — it is the fold over `evidence_link_fact` at the current
  baseline". §3 `:129` and §4 `:151`: acceptance eligibility and closure are **fold-only, never stored**.
- ADR-0040 §4 (`0040-…md:68-79`): "*Work acceptance eligibility* = every obligation with `mandatory = 1` has a
  current-baseline `evidence_link_fact` … **or** a `disposition_fact` on that obligation explicitly permitted
  by policy at the current baseline; otherwise acceptance is blocked and the UI shows the gap". "*Intent
  acceptance* is a `disposition_fact` of kind `intent-accepted` on `(notebook_id, revision_no)`".
  `:115-117`: "until recorded, the default is *no* disposition covers a mandatory obligation except
  `needs-decision` escalation". `:112-114`: "the UI must render 'blocked — obligation X lacks current
  evidence' rather than a disabled button with no reason".
- ADR-0045 §2 (`0045-…md:96`): "`checklist` arrives with WL-1b". `:62`: Checklists is an **Understand**
  feature child.
- Spec AC-W03a/b/c (`workbench-loops-ux.md:162-164`), AC-P02c (`:298`: "Saving an outstanding review state is
  allowed but is not acceptance and does not satisfy the obligation; missing or stale evidence is never
  relabeled as current proof"), the Assurance/Checklist row (`:109`: "Human acknowledgment and demonstrated
  satisfaction are distinct facts"), the Work-intent row (`:104`: "Work acceptance always names the declared
  deliverable, exact intent baseline and required evidence/dispositions"), the checklist state set (`:671`:
  "Empty, proposed, acknowledged, evidence missing, satisfied by cited evidence, stale, deferred; long
  labels").
- Ruling 156 (`docs/notes/addendum-c-council-rulings.md:3306-3325`): the adoption actor is v3
  `notebook_dim.adopted_by`, "in the first slice that offers adoption … Until it lands, AppendNotebook's
  adoption branch refuses with a stable code".

## What already exists on main (Verified — the code outranks the brief, rule 14)

| Piece | Where | What it does |
|---|---|---|
| The three tables | `WorkspaceSchema.cs:293-305` (`evidence_link_fact`), `:323-332` (`checklist_obligation_fact`), `:335-349` (`disposition_fact`) | In v2. `V2FactTables` (`:198-208`) gives each the no-update/no-delete triggers (`TriggerSql`, `:541`). `disposition_fact` has the per-subject kind CHECK (`:348`) and an FK `(notebook_id, revision_no) → prompt_revision_fact` (`:347`). `verification_status` is free `TEXT NOT NULL` (`:302`). No column names a journey on the checklist. |
| Frozen v2 | `WorkspaceSchema.cs:220-223` | "The vocabulary lists … may grow only with a v3 migration". So WL-1b must fit the existing CHECKs. |
| Raw writers | `StoreWriter.Workbench.cs:137` (`AppendEvidenceLink`), `:155` (`AppendDisposition`), `:164` (`AppendObligation`) | Plain INSERTs. No guard, no ownership check, no idempotency. |
| Raw readers | `StoreReader.Workbench.cs:156` (`ReadEvidenceLinks` by subject **and** revision), `:176` (`ReadDispositions`, "The caller picks the current one per actor"), `:186` (`ReadObligations`), `:194` (`ReadLatestChecklistRevision`) | By-key reads. `ReadPlanTests` (`ConstraintTests.cs:408-433`) asserts each named SQL is an index search. |
| Record types | `WorkbenchRecords.cs:141-154` (`EvidenceLinkRow`), `:170-182` (`DispositionRow`), `:185-189` (`ObligationRow`); vocabulary `:35-47` | No behaviour. |
| Callers | `grep` over `src/`: only the store itself | No service, no IPC operation, no UI reads or writes these rows today. |
| Folds | `CurrentnessFold.cs:29-51` (`Evaluate(attempt, currentRevisionNo, liveProjectionSha)`; NotRecorded when either side is null), `AttemptStateFold.cs:56` (`AttemptArtifactRef = "attempt://" + key`, with an `assume:` at `:55` that WL-1b's writer uses this form), `FoldSignal` (`:133-145`) | Pure; one `fold.duration_ms` activity each. |
| Service precedent | `NotebookOperations.cs:25-54` (`NotebookService`, one writer transaction per write), `WorkspaceOperations.cs:308-325` (`RegisterNotebook`), `WorkspaceOperations.cs:359-365` (`Refusable` maps only `WorkspaceStoreException` to a stable error response), `src/AiDe.Daemon/Program.cs:186` (registration), `WorkspaceClient.cs:129-141` (client calls) | WL-1b copies this shape exactly. |
| Caller identity | `IpcContract.cs:193` (`IpcPeer.ToPrincipal()` → `shell:<pid>`, `CallerKind.Shell`); `CallerKind.McpClient` (`Facts/Dispatch.cs:22`) has no producer in `src/` | Every pipe caller is a shell principal. |

**Finding F1 (two definitions of currentness).** `CurrentnessFold` (Core) has no caller in `src/` outside its
own file. The App decides staleness separately in `NotebookModel.CurrentnessOf` (`NotebookModel.cs:210-234`,
live draft sha against the ran revision) and keeps its own copy of the stale sentence
(`NotebookModel.cs:67` and `CurrentnessFold.cs:27`). DM7 calls two definitions of one quantity a defect
signature. WL-1b's acceptance fold consumes `CurrentnessFold`, so from WL-1b on the attempt-output answer has
two homes that can disagree. This is a finding for the Leader, not WL-1b scope (see DR-3).

## Data model (DM — settled first)

**No new table, column, index or trigger.** WL-1b writes only existing v2 rows, so v2 stays frozen
(`WorkspaceSchema.cs:220`) and no migration is owed.

### Aggregates

| Aggregate | Root | The one invariant it protects | References by identity |
|---|---|---|---|
| **Checklist** | `checklist_id` (≡ `journey_id`, below) | a revision is the complete set of obligations; revision numbers rise by one per change | journey |
| **Obligation evidence** | `link_id` | a link names one obligation at one checklist revision and one artifact at one baseline | obligation, attempt |
| **Disposition** | `(subject_kind, subject_id, ingress_seq)` | a disposition never changes; a newer row from the same actor supersedes it | obligation / journey / notebook revision |
| **Work acceptance** (a disposition, `subject_kind = work`) | `journey_id` | written only when eligibility holds at that baseline, in the same transaction that checked it; at most one per `(journey, baseline)` | journey |

One aggregate per transaction holds except for `work.accept`, which reads four aggregates and writes one.
That read is the invariant ("written only when eligible"), so it is one transaction on purpose.

### Grain, additivity, history (every row is append-only; triggers forbid UPDATE/DELETE)

| Row | One row is exactly one … | Written when | Measures |
|---|---|---|---|
| `checklist_obligation_fact` | obligation at one checklist revision | a proposal or edit creates checklist revision `max+1`, re-writing every obligation of the new set | count of obligations: additive within one revision, never summed across revisions |
| `evidence_link_fact` | link from one obligation revision to one attempt | the user links a completed attempt | count: additive over disjoint obligations |
| `disposition_fact` (`obligation`) | one actor's acknowledged / deferred / needs-decision on one obligation at one baseline | the user records it | a level per actor (latest wins), never summed |
| `disposition_fact` (`intent`) | one actor's acceptance of one notebook revision as the intent | the user accepts the intent | a level |
| `disposition_fact` (`work`) | one accepted deliverable of one journey at one baseline | `work.accept` succeeds | a level |

History rule: every attribute is Type-2 by construction (append-only; a change is a new row). No Type-1.

### Identities and the value objects (the decisions)

1. **`checklist_id ≡ journey_id`** — a documented alias, the same move ADR-0039 makes for
   `draft_id ≡ notebook_id` (`0039-…md:79`). ADR-0040 §2 requires exactly one owning journey per checklist,
   and `checklist_obligation_fact` has no owner column (`WorkspaceSchema.cs:323-332`). The alias gives the
   owner with no schema change. It allows one checklist per journey, which is all WL-1b and WL-1c need. A gap
   in ADR-0039, so it is **DR-1** below (default = this alias).
2. **`obligation_id` is minted by the service, globally unique** (`obl-` + `Guid.NewGuid().ToString("N")`,
   the stdlib form the bench already uses at `bench/AiDe.Bench/Program.cs:41`). It is stable across checklist
   revisions: the same id at a later revision is the same obligation, possibly with new text. Global
   uniqueness matters because `disposition_fact` and `evidence_link_fact` key the subject by
   `(subject_kind, subject_id)` alone (`:306`, `:346`). Two journeys with an obligation "o1" would otherwise
   share dispositions.
3. **Subject keys.** An obligation link: `subject_kind = 'obligation'`, `subject_id = obligation_id`,
   `subject_revision = checklist revision at link time`. An obligation disposition:
   `subject_kind = 'obligation'`, `subject_id = obligation_id`. An intent acceptance:
   `subject_kind = 'intent'`, `subject_id = journey_id`, `notebook_id`, `revision_no` set (the CHECK at
   `:348` requires both). A work acceptance: `subject_kind = 'work'`, `subject_id = journey_id`.
4. **`AcceptanceBaseline`** (Core value object, one home): the string `intent:<notebook_id>@<revision_no>`,
   where the revision is the intent notebook's **latest saved revision** when the row is written. ADR-0040 §5
   lists HEAD, PR head and dirty-content hash as further parts. WL-1b has no producer for them ("assessments
   absent"). The slice that adds a producer extends the value object. An old baseline then compares unequal and
   reads stale. That is the honest direction: never a false current.
5. **The intent notebook** is named by the caller on every request and checked by the service: its latest
   `notebook_dim` row must have `owner_journey_id = journey_id` (`StoreReader.Workbench.cs:100`).
   `OpenNotebook` makes this 1:1 today (`StoreWriter.Notebook.cs:28-37`).
6. **`verification_status`** vocabulary, Core-side (the column has no CHECK, `:302`): `observed` is the only
   value WL-1b writes. It means the link names an attempt whose terminal is `ResponseComplete`. Only `observed`
   links can satisfy. The list lives in the new acceptance code, **not** in `WorkbenchVocabulary`, so the
   frozen-vocabulary tests (`WorkspaceSchema.cs:220-223`) are untouched.
   assume: no test reflects over every public list in the Core assembly. Confirm: Track B's first build. If
   false: that test names the list and Track B adds it there with a comment.
7. **`artifact_ref = attempt://<dispatch_key>`**, built only by `AttemptStateFold.AttemptArtifactRef`
   (`AttemptStateFold.cs:56`). This discharges the `assume:` at `:55`: Track B deletes it and points the doc
   comment at the writer. WL-1b refuses any other artifact form (`simplify:` ceiling: attempt evidence only;
   upgrade trigger: WL-5's file, test and assessment evidence).

### Derived, never stored (DM7)

Per-obligation evidence state, per-obligation human state, the gap list, acceptance eligibility, work
closure, "current" for a link, "current" for an intent acceptance. **No cache.**
`simplify:` the fold reads its inputs on every evaluate. Ceiling: evaluate p95 ≤ 100 ms at 100× (Track C
measures it). Upgrade trigger: Track C over budget → a labelled rebuildable cache with a rebuild-equality test
(`claim_current_cache` precedent, `WorkspaceSchema.cs:184-194`).

### Is any WL-1b disposition an adoption? **No.** (Ruling 156)

- Adoption is a `notebook_dim` version row that sets an owner on a legacy notebook, or moves a notebook out of
  the terminal-drafts journey. `StoreWriter.AppendNotebook` refuses both (`StoreWriter.Workbench.cs:59-73`,
  `AIDE-STORE-ADOPTION-NOT-OFFERED`).
- WL-1b's operations write only `checklist_obligation_fact`, `evidence_link_fact` and `disposition_fact`.
  None calls `AppendNotebook` or `OpenNotebook`. An `intent-accepted` row references a notebook revision by
  FK (`:347`) and changes no ownership.
- WL-1b **refuses** a notebook that is not owned (legacy, `owner_journey_id` NULL), owned by
  `journey-terminal-drafts` (`WorkspaceSchema.cs:464`), or owned by a non-`work` journey
  (`AIDE-WORK-NOT-A-WORK-JOURNEY`). So no checklist can attach to an unadopted notebook "by using it".
- Each disposition already records its actor in `disposition_fact.actor`. Ruling 156's v3 column exists
  because `notebook_dim` has no actor column. WL-1b's rows do not have that gap.
- **Therefore WL-1b is not the first adopting slice and owes no v3 migration.** Track B's check B3 proves it:
  a full WL-1b scenario leaves the `notebook_dim` row count unchanged and every `notebook_dim` row
  byte-identical.

## The shape

```
App  ChecklistSurface ── ChecklistViewModel ──WorkspaceClient (5 calls)══IPC══▶ daemon
                                                                              ChecklistService (Core/Ipc)
                                                                                ├─ StoreReader (reads, one read txn)
                                                                                ├─ AcceptanceFold.Evaluate   (pure)
                                                                                ├─ WorkClosureFold.Evaluate  (pure)
                                                                                └─ StoreWriter (one writer txn per write;
                                                                                   work.accept re-folds inside it)
```

### The folds (Core, `src/AiDe.Core/Workbench/Records/Folds/AcceptanceFold.cs`) — Track A

```csharp
public sealed record AcceptanceInput(
    string JourneyId,
    string IntentNotebookId,
    int? IntentLatestRevisionNo,                  // null = the notebook has no revision
    IReadOnlyList<ObligationRow> CurrentObligations,   // latest checklist revision; empty = none declared
    IReadOnlyDictionary<string, IReadOnlyList<ObligationRow>> ObligationHistory, // by obligation_id, every revision
    IReadOnlyDictionary<string, IReadOnlyList<EvidenceLinkRow>> Links,           // by obligation_id, every revision
    IReadOnlyDictionary<string, IReadOnlyList<DispositionRow>> ObligationDispositions,
    IReadOnlyList<DispositionRow> IntentDispositions,
    IReadOnlyDictionary<string, AttemptRow> LinkedAttempts,  // by dispatch_key
    string? LiveProjectionSha);                   // the App's answer; null = Not recorded

public enum EvidenceState { Missing, Satisfied, Stale, NotRecorded }
public enum HumanState    { None, Acknowledged, Deferred, NeedsDecision }

public sealed record ObligationEvaluation(
    string ObligationId, string Text, bool Mandatory,
    EvidenceState Evidence, HumanState Human,      // two fields: a row and a fold never share one
    IReadOnlyList<EvidenceLinkRow> CitedCurrentLinks);

public sealed record AcceptanceGap(string Code, string? ObligationId, string Sentence);

public sealed record AcceptanceEvaluation(
    string Baseline,                               // AcceptanceBaseline.For(notebook, latest revision)
    IReadOnlyList<ObligationEvaluation> Obligations,
    IReadOnlyList<AcceptanceGap> Gaps,
    bool Eligible);                                // == Gaps.Count == 0, computed once
```

Rules (each is a fold test, Track A):

- **Satisfied** ⇔ some link of that obligation has `verification_status = observed`, its `baseline` equals the
  current baseline, the obligation's `(text, mandatory)` at the link's `subject_revision` equals its
  `(text, mandatory)` now ("the criterion did not change"), and, for its `attempt://` artifact,
  `CurrentnessFold.Evaluate(attempt, IntentLatestRevisionNo, LiveProjectionSha).State == Current`.
- **Stale** ⇔ links exist and none is current, and at least one failed only on baseline, criterion or
  `Stale` currentness. **NotRecorded** ⇔ the only blocker is `Currentness.NotRecorded` (it is never read as
  current, `CurrentnessFold.cs:12`). **Missing** ⇔ no link at all.
- **Human state** = the latest row per actor. The obligation shows the most recent of those. `needs-decision`
  from any actor whose latest row it is blocks acceptance, even with current evidence (AC-P02c: "unresolved
  mandatory judgment"). Only a later row from the same actor resolves it.
- **No disposition covers a mandatory obligation** — the ADR-0040 default (`0040-…md:115-117`). So
  `acknowledged` and `deferred` never remove an evidence gap. The policy is one Core table,
  `AcceptancePolicy.Covers(kind) = false` for every kind, with a test that pins it. The Owner can change it
  by ruling (DR-2).
- Gaps, in this order: `checklist-empty` (no obligations: blocked — spec `:73`, an implementation goal "is not
  accepted merely because the agent stopped", and `:104` acceptance "names … required evidence"),
  `intent-missing` (no revision), then per mandatory obligation `evidence-missing` / `evidence-stale` /
  `evidence-not-recorded` / `needs-decision`, then `intent-not-accepted` (no `intent-accepted` row of this
  journey whose `(notebook_id, revision_no)` equals the intent notebook's latest revision).
- A non-mandatory obligation never produces a gap. It still shows its states.

`WorkClosureFold.Evaluate(IReadOnlyList<DispositionRow> workDispositions, string currentBaseline)` →
`WorkClosure(NotAccepted | Accepted, string? AcceptedBaseline, bool AtCurrentBaseline)`. It reads only
`subject_kind = work` rows. An `accepted` row at an older baseline reads `Accepted` with
`AtCurrentBaseline = false`, so the record stays historical and is never re-labelled current. Investigation
closure is WL-1c's.

Both folds call `FoldSignal.Record("acceptance" | "work-closure", ms)` (`AttemptStateFold.cs:139`).

### The service (Core, new `src/AiDe.Core/Ipc/ChecklistOperations.cs`) — Track B

`ChecklistService(WorkspaceStore store)`, the `NotebookService` shape (`NotebookOperations.cs:25-54`). Every
request carries `JourneyId` and `IntentNotebookId`. Every write first runs the **work-journey guard**: the
journey exists with `purpose = work`, is not `journey-terminal-drafts`, and owns the intent notebook. If not:
`AIDE-WORK-NOT-A-WORK-JOURNEY`.

| Operation (wire name) | Request → result | Writes | Refusals (stable codes, thrown as `WorkspaceStoreException` so `Refusable` maps them) |
|---|---|---|---|
| `checklist.evaluate` | `(JourneyId, IntentNotebookId, LiveProjectionSha?)` → `AcceptanceEvaluation` + `WorkClosure` + `EvidenceCandidates` (the intent notebook's attempts at its latest revision whose terminal is `ResponseComplete`) | nothing (read txn) | guard |
| `checklist.obligations.set` | `(…, Obligations: [{ObligationId?, Text, Mandatory}])` → the new revision number | revision `max+1`, one row per obligation; a null id is minted; **no row** when the set equals the latest revision (the `SaveNotebookRevision` idempotency, `StoreWriter.Notebook.cs:44-54`) | `AIDE-WORK-OBLIGATION-UNKNOWN` (an id not in the checklist's history); `AIDE-WORK-FIELD-TOO-LONG` (text > 2,000 chars) |
| `checklist.disposition.record` | `(…, ObligationId?, Kind, Reason?)` → the row | one `disposition_fact`. `obligation` kinds need an `ObligationId` at the latest revision. `intent-accepted` needs none and binds the intent notebook's **latest** revision. Baseline = current | `AIDE-WORK-OBLIGATION-UNKNOWN`; `AIDE-STORE-VOCABULARY-VIOLATION` for a kind outside the subject's list (the CHECK at `:348`, also pre-checked); `AIDE-WORK-FIELD-TOO-LONG` (reason > 2,000) |
| `checklist.evidence.link` | `(…, ObligationId, DispatchKey, LiveProjectionSha?)` → the link | one `evidence_link_fact`, `artifact_ref = AttemptArtifactRef(key)`, `verification_status = observed`, `subject_revision` = latest checklist revision, baseline = current; **no row** if an identical current link exists | `AIDE-WORK-EVIDENCE-NOT-ELIGIBLE` with cause `not-this-journey` (the attempt's `draft_id` ≠ intent notebook), `not-complete` (no `ResponseComplete` terminal, from `AttemptStateFold` over the outcome rows), `not-current` (`CurrentnessFold` ≠ `Current`); `AIDE-WORK-OBLIGATION-UNKNOWN` |
| `work.accept` | `(JourneyId, IntentNotebookId, LiveProjectionSha?)` → `WorkClosure` | **inside one writer transaction**: re-read, re-fold; if `Eligible`, append `disposition_fact(work, journey, accepted, actor, baseline)`; if an `accepted` row at this baseline exists, return it and write nothing | `AIDE-WORK-ACCEPTANCE-BLOCKED`, the message lists every gap code |

- `actor` = `peer.ToPrincipal().Id` (`IpcContract.cs:193`), the `AttemptWithdraw` precedent
  (`WorkspaceOperations.cs:305`). It is never read from the payload.
- New readers (Track B, each added to `ReadPlanTests`' list at `ConstraintTests.cs:414-428`):
  `ReadEvidenceLinksForSubject(kind, id)` (prefix of `ix_evidence_link_subject`, `WorkspaceSchema.cs:306`),
  `ReadObligationHistory(checklistId)` (prefix of the PK, `:331`), `ReadAttemptsForNotebook(notebookId)`
  (prefix of `ix_attempt_revision (draft_id, revision_no)`).
- Registration: `WorkspaceOperations.RegisterChecklist(endpoint, service)` beside `RegisterNotebook`
  (`:313`), one line at `src/AiDe.Daemon/Program.cs:186`, five `WorkspaceClient` methods beside `:129-141`.

### The bench (new `bench/AiDe.Bench/Wl1bPerf.cs`, `-- wl1b`) — Track C

The P1-PERF harness is `bench/AiDe.Bench`: `Measure.Run(name, samples, action)` (`Measurement.cs:49`, warm and
cold reported apart), budgets and a `failures` list that makes the run exit non-zero (`Program.cs:9-14`,
`:36-45`), an argument branch per gate (`p2`, `Program.cs:24-35`), and the EXPLAIN-plan oracle (P1-PERF-04,
`Program.cs:264-318`). WL-1b adds a `wl1b` branch in the same shape; P1's committed numbers are untouched
(`Program.cs:15-17`).

- **1× population** (the demo): 1 work journey, 1 notebook, 3 revisions, 3 attempts, 2 checklist revisions × 3
  obligations, 3 links, 6 obligation dispositions, 1 intent and 1 work disposition.
  **100× breadth**: 100 such journeys in one store. **100× depth**: one journey with 300 obligations, 300 links
  and 600 dispositions.
  assume: the demo's shape is the right 1×. Confirm: the Owner reads the bench header. If false: change the
  constants; the gate's shape holds.
- **Measures:** (1) append p50/p95 for each WL-1b row on the product store, and on a **control store** built
  from the same DDL without the WL-1b triggers and the FK. The **overhead** is the difference, reported and
  not gated. (2) `checklist.evaluate` p95 in-process at 100× depth. Budget **100 ms**, the spec's
  selection/focus budget (NFR-02, `workbench-loops-ux.md:389`) — the nearest stated one. Labelled Inferred
  as a fit. (3) EXPLAIN QUERY PLAN for every WL-1b read: no full scan of `evidence_link_fact`,
  `disposition_fact`, `checklist_obligation_fact` or `dispatch_attempt_fact`.
- **Red first:** the plan oracle first runs one deliberately unindexed control query (`WHERE text = …` on
  `checklist_obligation_fact`). It must print `**FULL SCAN**` and fail. That proves the oracle can fail.
  Then the product reads run green.
- Output: `docs/proof/workbench-loops-wl-1b/perf/wl1b-perf-<date>.txt`, Release build (the header refuses
  to call Debug valid, `Program.cs:54`).

### The minimal `checklist` kind (App) — Track D

- **Row** in `SurfaceContentFactory.Kinds`: `checklist`, Title "Checklist", `Instances.Many` (one per
  journey), `Entry: Derived("_View")` (the opener "New Checklist", `SurfaceContentFactory.cs:55`), admitted to
  **Understand** (ADR-0045 `:62`) **and Factory** — the work notebook "foregrounds deliverable and acceptance
  obligations" (spec `:665`), and checklists are "independently dockable" beside it (`:618`). Understand wins
  routing, as for `codeviewer` (WL-S design §3). `Zone: Right`.
  assume: `ZoneId.Right` exists. Confirm: `ZoneId` on the T11 join head. If false: `Zone: null` (the
  placement policy places it).
- It follows the row contract (WL-S design §4): a UIA name, an allow-list entry, the opener, a
  `KindAllowListsTests` column entry (`tests/AiDe.App.Tests/Workbench/KindAllowListsTests.cs`). It adds **no**
  `SurfaceEntry` subtype, so D6's switch rule (an Owner amendment, carried on T11's branch, not on main —
  `.agents/briefs/workbench-loops/obligations.md:47`) needs no new arm. T11's reflection test covers the row.
- **Binding:** the surface binds the journey and intent notebook of the focused notebook (`NotebookOpenResult`,
  `NotebookOperations.cs:10`). It holds no state the store does not. Reopen and restore re-evaluate.
- **LiveProjectionSha** is the attempt envelope's projection, recomputed when the call is made
  (`ComposerSendGate.CurrentProjectionSha(envelope)`, `ComposerSendGate.cs:261`, as
  `SessionDocumentSurface.cs:728` does). It is **not** the composer's next, empty envelope. See risk R1.

## E7 change-surface list (every row WL-1b touches; the Leader ticks it at the join)

| Surface | WL-1b change | Track |
|---|---|---|
| store | none (v2 rows exist; v2 stays frozen) | — |
| store read API | 3 readers + their `ReadPlanTests` rows | B |
| model (records + folds) | `AcceptanceFold`, `WorkClosureFold`, `AcceptanceBaseline`, `AcceptancePolicy`, the evaluation records | A |
| service | `ChecklistService` (guards, minting, idempotency, in-txn eligibility) | B |
| projection / wire | 5 IPC operations, request/result records, `RegisterChecklist`, daemon line | B |
| client type | 5 `WorkspaceClient` methods; `ChecklistViewModel` | B (client), D (VM) |
| UI | `checklist` kind row, `ChecklistSurface`, every state in spec `:671` plus needs-decision, the gap sentences | D |
| compute reader | `checklist.evaluate` and `work.accept` call the folds; `AttemptStateFold.HasEvidence` now reads real links | A, B |
| measurement | `bench … -- wl1b` | C |
| docs | this design, the Proof Pack, DESIGN.md copy rows (Track D, granted at dispatch) | T22, D |

## Error and concurrency model

- One writer transaction per write, under the store's single-writer gate (`StoreWriter.Notebook.cs:5-6`).
  `work.accept` reads and writes inside it, so an edit cannot land between "eligible" and "accepted". That is
  the TOCTOU defence. Revision numbers (`max+1`) are read and written in the same transaction.
- Idempotency without a new key: `checklist.obligations.set` writes nothing for an equal set; `evidence.link`
  writes nothing for an identical current link; `work.accept` returns the existing row at the same baseline.
  A retry after a lost reply is therefore safe. `disposition.record` is **not** deduplicated: a repeated
  "acknowledged" is a second fact, harmless, because the latest per actor wins.
- Refusals are `WorkspaceStoreException` with a code (`WorkspaceStore.cs:26-30`), mapped by `Refusable`.
  The App shows the sentence and keeps the user's input.
- No async messaging, no queue, no retry loop. Distributed Systems: this is a single-process, single-writer
  store behind a request/response pipe. The one consistency boundary (App's projection vs daemon's store) is
  crossed by value (`LiveProjectionSha`), and a null degrades to NotRecorded.

## Failure-mode analysis

| # | Mode (from a design choice) | Disposition | Test |
|---|---|---|---|
| 1 | evaluate says eligible, then an edit lands before accept | **prevent**: re-fold inside the writer txn | B1 |
| 2 | accept retried after a lost reply | **prevent**: return the existing row at the same baseline | B1 |
| 3 | evidence from another journey's attempt | **prevent**: ownership check | B2 |
| 4 | evidence from a failed, running or stopped attempt | **prevent**: `ResponseComplete` required | B2 |
| 5 | stale attempt output linked | **prevent** at write (`not-current`), **detect** in the fold | B2, A2 |
| 6 | the App cannot compute the live projection | **degrade**: NotRecorded → gap `evidence-not-recorded`, never current | A2 |
| 7 | the composer's next envelope passed instead of the attempt's (R1) | **detect**: D2 asserts evidence stays current after the composer clears | D2 |
| 8 | checklist edited after a link | **detect**: criterion comparison; unchanged obligations keep their evidence | A2 |
| 9 | two writers propose at once | **prevent**: single-writer gate; `max+1` in txn | B (unit of B1 setup) |
| 10 | obligation id collision across journeys | **prevent**: minted global id | B3 |
| 11 | legacy, terminal-drafts or investigation notebook | **prevent**: guard refuses with cause; no adoption | B3 |
| 12 | `needs-decision` raised, then evidence linked | **prevent**: still blocked until the same actor supersedes it | A1 |
| 13 | empty checklist accepted "because nothing is mandatory" | **prevent**: gap `checklist-empty` | A1 |
| 14 | daemon not connected | **degrade**: the surface says "Obligations are not available: no workspace daemon is connected." (the `RevisionsNotRecorded` idiom, `NotebookModel.cs:70`) | D1 |
| 15 | store opened read-only (newer file) | **detect**: existing `AIDE-STORE-NEWER-SCHEMA-READ-ONLY` surfaces as the refusal sentence | existing |
| 16 | fold slow at scale | **detect**: Track C's p95 gate; `fold.duration_ms` per evaluate | C |
| 17 | clock skew | **prevent**: order is `ingress_seq` (`WorkspaceSchema.cs:37-38`) | existing |
| 18 | oversize text or reason fills the frame | **prevent**: 2,000-char caps | B2 |

## Adversarial analysis (STRIDE-lite)

Trust boundary: the named pipe, shell → daemon (owner-SID ACL, single user). The store is behind it.

| Threat | Disposition |
|---|---|
| **S** — another same-user process (including a harness child) opens the pipe and calls `work.accept` | **mitigate (the real control):** eligibility is enforced in the writer transaction, so no caller can accept with a gap. **Accept (residual):** such a process could record `intent-accepted` and link a completed attempt, then accept. Single-user local posture (security stays at the spec floors). assume: the harness child does not inherit the pipe's capability. Confirm: `ChildEnvironment.Sanitize` and the IPC open handshake (not opened here). If false: a refusal for non-shell callers needs a producer of `CallerKind.McpClient` — a seam to the IPC owner. Negative test B1: `work.accept` with a gap is refused whoever calls. |
| **T** — rewrite a link or disposition after the fact | **mitigate:** the immutability triggers (`TriggerSql`, `:541`) and `recursive_triggers` (`:533-538`); the existing `ForbiddenUpdateTests` cover all three tables (`ConstraintTests.cs:355-395`) |
| **R** — "who accepted this?" | **mitigate:** `actor` from the transport, never the payload. **Accept (residual):** `shell:<pid>` names a process, not a person — the `AttemptWithdraw` precedent |
| **I** — leak of obligation text or reasons | **accept:** workspace-local rows, no egress in WL-1b; error messages carry codes and ids, not text |
| **D** — flood the checklist | **mitigate:** 2,000-char caps; the frame limit (`EveryOperationFitsTheFrameTests`) |
| **E** — `acknowledged` read as satisfaction | **prevent:** two fields in the evaluation (`HumanState` vs `EvidenceState`); `AcceptancePolicy` pinned |

Rollups: `docs/security/threat-model.md` and `privacy-review.md` are outside this node's owned paths.
**Deviation:** the Leader runs `docs-graph.py rollup` at the join (recorded in handoff.md).

## Privacy analysis (LINDDUN-lite)

No personal data. Obligation text, reasons and attempt references are the user's own work content in a
local store. `actor` is a process principal (`shell:<pid>`), not a person. No telemetry carries text.

## UI and interaction design (Track D)

- **Medium:** WPF desktop, the existing token set in `DESIGN.md` (no new tokens; no arbitrary values).
  Archetype: a **data** surface (a list of records with states), reading-parallel. `ui-craft-gate.py` runs on
  the built surface at Track D's close (rung-2 control, CD12 floors).
- **Hierarchy:** focal point = the acceptance line at the top ("Ready to accept" or the first blocking gap).
  Then one row per obligation: text, a *Mandatory* tag, the **evidence** state and the **human** state as two
  separate labels, then the row's actions. Then the two closing actions: *Accept intent (revision N)* and
  *Accept deliverable*.
- **States** (spec `:671` plus needs-decision): Empty, proposed, acknowledged, evidence missing, satisfied by
  cited evidence, stale, deferred, needs decision, evidence currentness not recorded, loading, daemon not
  connected, refusal, long labels (wrap, never truncate the obligation text), accepted at this baseline,
  accepted at an earlier revision.
- **Copy (load-bearing; Owner confirms; DESIGN.md rows in Track D):**
  - Empty: "No obligations yet. Add the conditions this work must meet."
  - Acknowledged: "Acknowledged — not satisfied. Acknowledging is not evidence."
  - Satisfied: "Satisfied by run {7-char key} (revision {n})."
  - Stale: "The evidence belongs to an earlier draft. Link evidence from the current draft."
  - Not recorded: "Evidence currentness is not recorded, so this does not count as current."
  - Gap line (ADR-0040 `:113`): "Blocked — “{obligation}” has no current evidence." · "Blocked — “{obligation}”
    needs a decision." · "Blocked — intent revision {n} is not accepted." · "Blocked — no obligations are
    declared for this work."
  - Accepted: "Accepted: the declared deliverable at intent revision {n}. This records acceptance of the
    deliverable only. It does not say the work is implemented or merged."
  - Not a work journey: "This notebook is unassigned (pre-workbench). Obligations need a work journey."
- *Accept deliverable* is **enabled** while blocked: pressing it calls `work.accept`, so the store gives the
  refusal and the gaps are read out (polite live region). That is the opposite of a silent disabled button.
- **Accessibility:** every action and state has a UIA name. The row's name joins the obligation and both state
  labels. Keyboard: Tab through rows and actions; Space or Enter acts. No motion beyond the theme's.

## Telemetry (measured by default)

- `fold.duration_ms {fold=acceptance|work-closure}` on `aide.attempt.fold` (`FoldSignal`), every evaluate.
- New `ActivitySource "aide.work"`: `work.accept {journey_id, outcome=accepted|already-accepted|blocked,
  gap_codes}`, `checklist.evidence.link {outcome, cause}`. No text, no actor.
- Stable codes: `AIDE-WORK-NOT-A-WORK-JOURNEY`, `AIDE-WORK-OBLIGATION-UNKNOWN`, `AIDE-WORK-FIELD-TOO-LONG`,
  `AIDE-WORK-EVIDENCE-NOT-ELIGIBLE` (cause `not-this-journey` | `not-complete` | `not-current`),
  `AIDE-WORK-ACCEPTANCE-BLOCKED`. Declared beside `StoreErrorCodes` (`WorkspaceStore.cs:7-23`).
- No HTTP surface, so no RFC 9457 response.
- Track C's bench numbers are the scale measurement. A test asserts that `work.accept` emits its activity
  on the blocked path (load-bearing: it is the only trace of a refused acceptance).

## Test plan (Testing Strategy — the union of triggered directives)

Triggers: T1 pure folds → **D1** (unit; mutation-resilient boundary cases); T4 SQLite → **D4** (real store,
`SchemaV2Support`); T6 IPC operations others call → **D5-provider** (`NotebookOperationsTests` precedent:
through the daemon endpoint); T7 payload schemas → **D6** (a golden JSON per request/result record; the
`FieldsSurviveTheClientBoundaryTests` idiom); T3 new namespace use (`Ipc` → `Workbench.Records.Folds`, App →
Core client only) → **D3** (an architecture assertion that the folds reference no `Store` writer and no App
type); **D0** hygiene always. T8: no fake is planned (App tests use the in-process daemon); a fake written
later triggers **D7**. T9–T14: none (no AI call).

The checks, red first, three per node at most (rule 16):

| Track | Check | The red that must fail first |
|---|---|---|
| A | **A1 acknowledged ≠ satisfied** | an obligation with an `acknowledged` row and no link reads `Evidence = Missing`, `Human = Acknowledged`, gap `evidence-missing`; `AcceptancePolicy` covers no kind; `needs-decision` blocks with current evidence; `checklist-empty` blocks |
| A | **A2 blocked while a mandatory obligation lacks a current link** | each of: no link; link at an old baseline; criterion changed; `CurrentnessFold` Stale; projection null → NotRecorded; a non-mandatory gap-free; unchanged obligations keep evidence across a checklist revision |
| A | **A3 work closure** | no `accepted` row → NotAccepted whatever else exists; an `accepted` row at an old baseline → Accepted, `AtCurrentBaseline = false` |
| B | **B1 accept only when eligible, in one txn** | `work.accept` with a gap → `AIDE-WORK-ACCEPTANCE-BLOCKED`, zero rows; eligible → one row; again → same row, still one; the blocked path emits `work.accept` |
| B | **B2 evidence link guards** | other journey's attempt, a `Failed` attempt, a stale attempt → refused with the cause; the stored `artifact_ref` equals `AttemptArtifactRef`; `AttemptStateFold.HasEvidence` reads true; caps refused |
| B | **B3 no adoption** | the whole scenario leaves `notebook_dim` byte-identical; legacy, terminal-drafts and investigation notebooks refused; two journeys get distinct obligation ids |
| C | **C1 overhead and plans at 100×** | the control unindexed query prints FULL SCAN and fails; then product plans indexed, evaluate p95 ≤ 100 ms, overhead table printed |
| D | **D1 the demo path, headless** | the checklist kind opens from View in Factory and Understand; empty → propose → "evidence missing"; acknowledge → "Acknowledged — not satisfied", still blocked |
| D | **D2 link, gap, intent, accept** | link the completed run → satisfied; *Accept deliverable* → refusal read out naming "intent revision 1 is not accepted"; accept intent; accept → the Accepted line, and no string contains "implemented" or "merged" except the negating sentence; with the composer cleared after the run, evidence still reads current (R1) |
| D | **D3 saved review state is not acceptance** | record `deferred` and `needs-decision`, save the layout, restart: the checklist restores with the same states, `work` has no `accepted` row, the gap line is unchanged |

## Patterns

- **Specification over facts / pure fold (LOA P10, "history is the data")** — eligibility and closure are
  functions of append-only rows. Survives the Simplifier: it is the WL-1a fold idiom reused
  (`AttemptStateFold`, `CurrentnessFold`).
- **Guarded command, check-then-act in one transaction** — the `BoundaryDispatcher` read-before-write gate,
  reused for `work.accept`. Survives the Simplifier: without it, a UI check races the store.
- **Value object** — `AcceptanceBaseline`, one home for the baseline string.
- **Policy table** — `AcceptancePolicy`, a pinned, empty-by-default rule set (ADR-0040's default), so the
  Owner's DR-2 ruling is one line and one test, not a new mechanism.
- Rejected: a stored `satisfied` flag or status column (ADR-0039 §3 forbids it); a separate
  `ChecklistReader` class (the `StoreReader` partials are the home); a new `checklist_dim` (DR-1's alias does
  the job); an event/notification on acceptance (no consumer in WL-1b).

## Solution-Selection Ladder

YAGNI: no schema, no cache, no non-attempt evidence, no investigation closure. Reuse: the v2 rows, raw
writers and readers, `NotebookService`'s shape, `Refusable`, `FoldSignal`, `CurrentnessFold`,
`AttemptArtifactRef`, the bench's `Measure`. Stdlib: `Guid`. No new dependency.

## Track cut (red-first; dependencies are real edges only, GO5)

| Track | Owned paths | Checks (red → green) | Depends on (real edges) | Harness / model |
|---|---|---|---|---|
| **T23 `wl1b-folds`** (Core) | new `src/AiDe.Core/Workbench/Records/Folds/AcceptanceFold.cs`, `WorkClosureFold.cs`, `AcceptanceBaseline.cs`, `AcceptancePolicy.cs`; new `tests/AiDe.Core.Tests/Workbench/Folds/AcceptanceFoldTests.cs`, `WorkClosureFoldTests.cs`; `docs/proof/workbench-loops-wl-1b/folds/**` | A1, A2, A3 | none — reads existing row types (`WorkbenchRecords.cs`) and `CurrentnessFold`. **Starts at once.** Publishes the record types (§ The folds) as its first commit, for T24 | Claude Code · Sonnet |
| **T24 `wl1b-service`** (Core) | new `src/AiDe.Core/Ipc/ChecklistOperations.cs`; `src/AiDe.Core/Store/Records/StoreReader.Workbench.cs` (3 readers); `WorkspaceOperations.cs` (`RegisterChecklist` + 5 names only); `WorkspaceClient.cs` (5 methods only); `WorkspaceStore.cs` (the new codes only); `AttemptStateFold.cs:55` (delete the `assume:` only); `src/AiDe.Daemon/Program.cs:186` (one line); `tests/AiDe.Core.Tests/Store/SchemaV2/ConstraintTests.cs` (`ReadPlanTests` rows only); new `tests/AiDe.Core.Tests/Checklist/ChecklistOperationsTests.cs`; `docs/proof/workbench-loops-wl-1b/service/**` | B1, B2, B3 | **T23's record-types commit** (the service returns them). The readers and guards can start at once. **Starts at once.** | Claude Code · Sonnet |
| **T25 `wl1b-perf`** (bench) | new `bench/AiDe.Bench/Wl1bPerf.cs`; `bench/AiDe.Bench/Program.cs` (the `wl1b` branch only); `docs/proof/workbench-loops-wl-1b/perf/**` | C1 | n1 (append overhead, control store, existing readers' plans): **none, starts at once**. n2 (evaluate p95, new readers' plans): **T24 joined** | Claude Code · Sonnet |
| **T26 `wl1b-checklist-kind`** (App) | `src/AiDe.App/Workbench/SurfaceContentFactory.cs` (the `checklist` row only); new `src/AiDe.App/Workbench/Checklist/**`; `tests/AiDe.App.Tests/Workbench/KindAllowListsTests.cs` (the column entry only); new `tests/AiDe.App.Tests/Workbench/Checklist/**`; `DESIGN.md` (the copy rows only, granted at dispatch); `docs/proof/workbench-loops-wl-1b/checklist/**` | D1, D2, D3 | **T24 joined** (the client calls — data); **T11 joined** (the WL-S window of `SurfaceContentFactory.cs`, D6, and the `notebook` kind whose journey the surface binds — data and file); **T21 joined** (`SurfaceContentFactory.cs` is in T21's window too — file). **Scheduled after the WL-S joins.** Rules 19 and 20 gate its App suite | Claude Code · Opus |

Critical path: T23 types → T24 → T26 (after WL-S). T25 n1 and T23/T24 run in parallel now. Nothing joins
until the user's WL-1a demo passes (brief). The Leader writes the WL-1b human demo after T26 joins; the
draft is in the Proof Pack.

## Decision-request drafts (for the Leader to raise; not rulings)

- **DR-1 — Where does a checklist's owning journey live?** ADR-0040 §2 requires exactly one owner;
  `checklist_obligation_fact` has no owner column (`WorkspaceSchema.cs:323-332`). *Recommendation:*
  `checklist_id ≡ journey_id`, a documented alias like `draft_id ≡ notebook_id`. No migration; one checklist
  per journey. *Alternative:* a v3 `owner_journey_id` column, which makes WL-1b a migration slice.
  *Default if unruled:* the alias.
- **DR-2 — May any human disposition cover a mandatory obligation?** ADR-0040 `:115-117` defers it to a
  policy and sets the default to none. *Recommendation:* keep none for WL-1b. `deferred` keeps acceptance
  blocked, and AC-W03c's plan deliverable is met by evidence, not by deferral. *Default:* none.
- **DR-3 — Two currentness definitions (finding F1).** *Recommendation:* a WL-2-or-later track replaces
  `NotebookModel.CurrentnessOf` with `CurrentnessFold`, or the design records why the App's rule differs
  (the empty-draft clause, `NotebookModel.cs:205-208`). Not WL-1b scope. *Default:* record it as a defect
  class candidate at the WL-1b close.
- **DR-4 — Where does "the declared deliverable" live?** Spec `:104` says acceptance names it, and no v2
  column holds a deliverable kind. *Recommendation:* the deliverable is declared in the accepted intent
  revision's text and bounded by its checklist. The Accepted line names the intent revision and never claims
  implementation (AC-W03c). *Alternative:* a v3 deliverable-kind column. *Default:* the recommendation.

## Flagged risks and residual unknowns

- **R1 (Flagged):** `LiveProjectionSha` must be the attempt's own envelope projection. If the App passes the
  composer's next, empty envelope, every evidence link reads stale right after a run. D2 holds the red test.
- **R2 (Inferred):** the harness child cannot reach the daemon pipe with a capability (STRIDE S).
- **R3 (Inferred):** 100 ms is the nearest stated budget, not one written for this fold.
- **R4:** the obligation-edit UI is minimal (text + mandatory). Ordering and removal are a new revision
  without the removed row. Removal is not designed past that.

## Confidence ledger

| Claim | Label |
|---|---|
| The three tables, their CHECKs, FK and triggers exist and are frozen | Verified (opened) |
| Writers and readers exist with no caller outside the store | Verified (grep over `src/`) |
| No WL-1b write touches `notebook_dim`, so no adoption | Verified by design; proved by B3 |
| `Refusable` maps only `WorkspaceStoreException` | Verified (`WorkspaceOperations.cs:359-365`) |
| Single-writer gate makes in-txn re-fold race-free | Verified (`StoreWriter.Notebook.cs:5-6`) |
| The App can supply the attempt envelope's projection | Inferred (R1) |
| Evaluate p95 ≤ 100 ms at 100× | Flagged until T25 n2 |

## Status and next action

| | |
|---|---|
| **Completed** | WL-1b designed: data model (no schema change), folds, guarded service, wire, bench, checklist kind, track cut, DR drafts |
| **Remaining** | T23–T26 implementation; WL-1b Proof Pack evidence; the human demo after T26 |
| **Best next action** | dispatch T23 and T24 (and T25 n1) now; T26 after the T11 and T21 joins |

## Gate record

Adversary Mode, run inline in this one context (fan-out 0, as the brief requires). The author ran these
personas, so a veto here is cleared by the Leader's read-back, not by the author.

**Patterns Expert ⇄ The Simplifier — PASS-WITH-CONDITIONS.**
- Simplifier 1: "Five operations is a lot for a minimal kind." Kept. Each maps to one demo gesture
  (evaluate, propose, record, link, accept). Merging *record* and *link* would put two row types behind one
  verb.
- Simplifier 2: "Drop `ObligationHistory` and compare only the revision number." Rejected. Revision-only
  comparison stales every link when any obligation is added. Patterns Expert agrees: the criterion is the
  subject (ADR-0039 `:83`, "claim/criterion revision").
- Simplifier 3: "No cache." Accepted, with the `simplify:` marker and T25 as its trigger.
- Patterns Expert 1: "Name the check-then-act." Done (Guarded command; `BoundaryDispatcher` precedent).
- Patterns Expert 2: "The policy should be data, not an `if`." Done (`AcceptancePolicy`, pinned).
- Condition: T24 keeps the service in the `NotebookService` shape; no new base class.

**Data & Persistence Architect — PASS (no veto; nothing irreversible).** No DDL, no migration, no backfill;
v2 stays frozen. Checked: grain stated for every row; additivity (levels never summed across time); Type-2 by
construction; derive-don't-store (no stored eligibility, satisfaction or closure); the FK and CHECK at
`:347-348` are relied on, not duplicated. Conditions: (1) DR-1's alias is written into the ADR-0039 §1 table
by the Leader if ruled, so the alias has one documented home; (2) B3's byte-identical `notebook_dim` assertion
is mandatory, because it is what makes "no adoption" a proof and not a claim; (3) the bench's control store is
built from the same DDL text with the triggers and FK removed, never hand-copied, so the overhead it measures
is the product's.

**Test Architect — PASS-WITH-CONDITIONS (hard veto not raised).** Every promise in the §WL.11 row maps to a
check: acknowledged ≠ satisfied → A1; blocked without a current link → A2 + B1; saved review state is not
acceptance → D3; overhead at 100× → C1. Conditions: C1's red is the oracle self-test (a bench has no other
honest red); D2's copy assertion uses the shipped strings, not a fixture.

**Security & Identity — PASS-WITH-CONDITIONS.** The real control is in-txn eligibility. The STRIDE-S residual
is recorded with its `assume:`. Condition: if R2 is false, the non-shell refusal is a seam, not a WL-1b
workaround.

**SRE — PASS.** Every handled mode has a test or an existing code; the refused acceptance is visible from
telemetry alone (`work.accept outcome=blocked`).

**Distributed Systems — N/A, PASS.** No async messaging or retries. The only cross-process value is the
projection sha, which degrades to NotRecorded.

**UX & Accessibility — PASS-WITH-CONDITIONS.** Two labels per row keep "acknowledged" and "satisfied" apart.
The enabled-while-blocked button follows ADR-0040 `:113`. Condition: the Owner confirms the copy before T26's
DESIGN.md rows land.
