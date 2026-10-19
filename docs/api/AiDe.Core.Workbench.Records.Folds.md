---
id: api-aide-core-workbench-records-folds
title: "API: AiDe.Core.Workbench.Records.Folds"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.Core.Workbench.Records.Folds: 20 types, 20 members, 55% carrying a summary doc comment.
---

# API: `AiDe.Core.Workbench.Records.Folds`

**20 public types · 20 public members · 55% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `AcceptanceBaseline`

*class* — `AcceptanceBaseline.cs`

The one home of the acceptance baseline (WL-1b, ADR-0040 section 5): the string
`intent:<notebook_id>@<revision_no>` over the intent notebook's latest saved revision.

**Remarks.** Ruling 179: the deliverable is the accepted intent revision.
HEAD, PR head and dirty-content hash are ADR-0040 baseline parts with no producer in WL-1b; the slice
that adds one extends this type, and an old baseline then compares unequal and reads stale.

| Member | Summary |
|---|---|
| `string NoRevision = ""` | The baseline of a notebook with no saved revision. It equals no stored baseline. |
| `string For(string notebookId, int? latestRevisionNo)` | **(gap)** |

## `AcceptanceInput`

*record* — `AcceptanceFold.cs`

Everything one acceptance evaluation reads. All of it is already durable except the App's projection answer.

## `EvidenceState`

*enum* — `AcceptanceFold.cs`

*No doc comment on this type.* **(gap)**

## `HumanState`

*enum* — `AcceptanceFold.cs`

*No doc comment on this type.* **(gap)**

## `ObligationEvaluation`

*record* — `AcceptanceFold.cs`

One obligation's two derived states. A row and a fold never share one field.

## `AcceptanceGap`

*record* — `AcceptanceFold.cs`

*No doc comment on this type.* **(gap)**

## `AcceptanceEvaluation`

*record* — `AcceptanceFold.cs`

*No doc comment on this type.* **(gap)**

## `AcceptanceGapCodes`

*class* — `AcceptanceFold.cs`

Stable gap codes of `Code`.

| Member | Summary |
|---|---|
| `string ChecklistEmpty = "checklist-empty"` | **(gap)** |
| `string IntentMissing = "intent-missing"` | **(gap)** |
| `string EvidenceMissing = "evidence-missing"` | **(gap)** |
| `string EvidenceStale = "evidence-stale"` | **(gap)** |
| `string EvidenceNotRecorded = "evidence-not-recorded"` | **(gap)** |
| `string NeedsDecision = "needs-decision"` | **(gap)** |
| `string IntentNotAccepted = "intent-not-accepted"` | **(gap)** |

## `AcceptanceFold`

*class* — `AcceptanceFold.cs`

Acceptance eligibility as a pure fold over the v2 rows (WL-1b design, "The folds").

| Member | Summary |
|---|---|
| `string ObservedStatus = "observed"` | The only `verification_status` WL-1b writes: the link names an attempt that ended `ResponseComplete`. |
| `AcceptanceEvaluation Evaluate(AcceptanceInput input)` | **(gap)** |

## `AcceptancePolicy`

*class* — `AcceptancePolicy.cs`

Whether a human disposition removes an evidence gap on a mandatory obligation (ADR-0040 section 3).

**Remarks.** Ruling 177: no disposition covers a mandatory obligation. Adding a covering kind later is a ruling,
not an edit; `CoveringKinds` and its pinning test change together.

| Member | Summary |
|---|---|
| `IReadOnlySet<string> CoveringKinds = new HashSet<string>(StringComparer.Ordinal)` | The one table: the disposition kinds that cover a mandatory obligation. Empty by default. |
| `bool Covers(string dispositionKind)` | **(gap)** |

## `AttemptStateInput`

*record* — `AttemptStateFold.cs`

Everything one attempt's state fold reads (ADR-0041 §2). All of it is already durable.

## `AttemptState`

*record* — `AttemptStateFold.cs`

The fold's answer: a state, the derived evidence bool, and the cited seqs the log lacks.

## `AttemptStateFold`

*class* — `AttemptStateFold.cs`

Derives an attempt's state from outcome rows and the durable run log (ADR-0041 §2).

**Remarks.** Pure and total: no I/O and no store write. Terminals come only from the host's outcome rows
(lowest `ingress_seq` wins); an `acp.result` or `acp.error` event never yields one.
Precedence among non-terminals: StopRequested, OverLimit, Denied, Waiting, Running. A cited seq
the log lacks is reported in `NotRecordedSeqs` and never acted on.




assume: the budget, context-ceiling and input-request kinds below are this fold's own
vocabulary until WL-4 and the harness mapper emit them. Confirm: the mapper's kind list. If false:
OverLimit and input-Waiting never derive (they read Running), never a wrong terminal.




assume: a permission decision rejects when `OptionChosen` starts with `reject`
(ACP kinds `reject_once` / `reject_always`, adapter id `reject`). Confirm: the
decision writer's stored value. If false: Denied never derives.

| Member | Summary |
|---|---|
| `string AttemptArtifactRef(string dispatchKey)` | The `artifact_ref` an evidence link to this attempt carries. |
| `AttemptState Evaluate(AttemptStateInput input)` | **(gap)** |

### `string AttemptArtifactRef(string dispatchKey)`

The `artifact_ref` an evidence link to this attempt carries.

**Remarks.** Written only by `ChecklistService.LinkEvidence`, which refuses any other form of attempt reference.

## `FoldSignal`

*class* — `AttemptStateFold.cs`

The fold instrumentation: one `fold.duration_ms` activity per evaluation (ADR-0041 §7).

**Remarks.** Core publishes on an `ActivitySource` exactly as `CompileSignal` does; the host's
listener writes it to the rolling log. With no listener nothing is recorded, never a zero.

| Member | Summary |
|---|---|
| `string SourceName = "aide.attempt.fold"` | **(gap)** |

## `Currentness`

*enum* — `CurrentnessFold.cs`

Whether an attempt's output still belongs to the live draft.

## `CurrentnessResult`

*record* — `CurrentnessFold.cs`

A currentness verdict; `Copy` is the operator-facing line for a stale result, else null.

## `CurrentnessFold`

*class* — `CurrentnessFold.cs`

ADR-0040 §5 for WL-1a: an attempt bound to `(notebook_id, revision_no, projection_sha)` is
stale when the notebook's current revision or the live envelope's projection moved. The attempt
row is retained; staleness is derived, never stored.

| Member | Summary |
|---|---|
| `string StaleCopy = "This output belongs to an earlier draft. Run again to refresh it."` | Spec §C3, verbatim. |
| `CurrentnessResult Evaluate(AttemptRow attempt, int? currentRevisionNo, string? liveProjectionSha)` | A stored attempt, judged by its own `projection_sha` against the live projection sha. |
| `CurrentnessResult Evaluate(int ranRevisionNo, int? currentRevisionNo, string? ranSha, string? liveSha)` | The one definition.  and  are the same kind of digest, taken by the caller's one definition of it (a projection sha for a stored attempt, a body sha for a notebook result); the fold only compares them, so neither side… |

## `WorkClosureState`

*enum* — `WorkClosureFold.cs`

*No doc comment on this type.* **(gap)**

## `WorkClosure`

*record* — `WorkClosureFold.cs`

Work closure. An acceptance at an older baseline stays `Accepted` with
false: the record is historical and never re-labelled current.

## `WorkClosureFold`

*class* — `WorkClosureFold.cs`

Whether a journey's deliverable was accepted, derived from `subject_kind = work` dispositions only.

| Member | Summary |
|---|---|
| `WorkClosure Evaluate(IReadOnlyList<DispositionRow> workDispositions, string currentBaseline)` | **(gap)** |
