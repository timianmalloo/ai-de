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
  Extracted public surface of AiDe.Core.Workbench.Records.Folds: 7 types, 5 members, 75% carrying a summary doc comment.
---

# API: `AiDe.Core.Workbench.Records.Folds`

**7 public types · 5 public members · 75% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

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

**Remarks.** assume: WL-1b's evidence writer uses this form. Confirm: its first link row. If false: HasEvidence reads false.

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
| `CurrentnessResult Evaluate(AttemptRow attempt, int? currentRevisionNo, string? liveProjectionSha)` | **(gap)** |
