---
id: api-aide-core-store-records
title: "API: AiDe.Core.Store.Records"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.Core.Store.Records: 13 types, 37 members, 38% carrying a summary doc comment.
---

# API: `AiDe.Core.Store.Records`

**13 public types · 37 public members · 38% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `WorkbenchVocabulary`

*class* — `WorkbenchRecords.cs`

Closed vocabularies the store CHECKs. One home each; the DDL is built from the same lists.

| Member | Summary |
|---|---|
| `IReadOnlyList<string> JourneyPurposes = ["work", "investigation"]` | **(gap)** |
| `IReadOnlyList<string> JourneyOriginKinds =` | **(gap)** |
| `IReadOnlyList<string> DeliveryUnknownCauses =` | The `cause` of a `DeliveryUnknown` outcome (ADR-0039 §3). |
| `IReadOnlyList<string> AssessmentKinds =` | **(gap)** |
| `IReadOnlyList<string> ForbiddenPayloadKeys =` | Keys an assessment payload may never carry, at any depth: exactly the fold-derived names of ADR-0039 section 3 (Ruling 150). The store trigger matches them ignoring letter case and the separators `_`, `-` and space, s… |
| `IReadOnlyList<string> DispositionSubjectKinds =` | **(gap)** |
| `IReadOnlyDictionary<string, IReadOnlyList<string>> DispositionKindsBySubject =` | Disposition kinds allowed per subject kind; the DDL CHECK is built from this. |
| `IReadOnlyList<string> ProfileObservationKinds =` | Fixed profile-observation kinds; `capability:<name>` is the open-ended one. |
| `string CapabilityObservationPrefix = "capability:"` | **(gap)** |

## `JourneyRow`

*record* — `WorkbenchRecords.cs`

One journey version. Type-0: Purpose and the Origin* columns never change across versions.

| Member | Summary |
|---|---|
| `int VersionNo { get; init; }` | **(gap)** |
| `long IngressSeq { get; init; }` | **(gap)** |
| `string RecordedAt { get; init; } = ""` | **(gap)** |

## `NotebookRow`

*record* — `WorkbenchRecords.cs`

One notebook version. `Legacy` rows have no owner; adoption is a new version row with
`Legacy = false` and an owner (the one permitted change of an otherwise Type-0 owner).

| Member | Summary |
|---|---|
| `int VersionNo { get; init; }` | **(gap)** |
| `long IngressSeq { get; init; }` | **(gap)** |
| `string RecordedAt { get; init; } = ""` | **(gap)** |

## `NotebookRevisionRow`

*record* — `WorkbenchRecords.cs`

An immutable notebook revision (`prompt_revision_fact`; `draft_id` is the notebook id).

| Member | Summary |
|---|---|
| `string? BodySha { get; init; }` | Null = Not recorded (legacy rows); the writer computes it for every new row. |
| `long IngressSeq { get; init; }` | **(gap)** |
| `string SavedAt { get; init; } = ""` | **(gap)** |

## `AttemptRow`

*record* — `WorkbenchRecords.cs`

An execution attempt as bound at write-ahead (`dispatch_attempt_fact`).

| Member | Summary |
|---|---|
| `long IngressSeq { get; init; }` | **(gap)** |
| `string AttemptedAt { get; init; } = ""` | **(gap)** |

## `OutcomeRow`

*record* — `WorkbenchRecords.cs`

One outcome event on one attempt (`dispatch_outcome_fact`).

| Member | Summary |
|---|---|
| `long IngressSeq { get; init; }` | **(gap)** |
| `string RecordedAt { get; init; } = ""` | **(gap)** |

## `PermissionDecisionRow`

*record* — `WorkbenchRecords.cs`

One decision on one permission request; the request is `(SessionId, RunId, Seq)`.

| Member | Summary |
|---|---|
| `long IngressSeq { get; init; }` | **(gap)** |
| `string RecordedAt { get; init; } = ""` | **(gap)** |

## `EvidenceLinkRow`

*record* — `WorkbenchRecords.cs`

One link from one claim/criterion revision to one artifact or attempt.

| Member | Summary |
|---|---|
| `long IngressSeq { get; init; }` | **(gap)** |
| `string RecordedAt { get; init; } = ""` | **(gap)** |

## `AssessmentRow`

*record* — `WorkbenchRecords.cs`

One assessment of one scope at one baseline. `Payload` is opaque JSON owned by its producer.

| Member | Summary |
|---|---|
| `long IngressSeq { get; init; }` | **(gap)** |
| `string RecordedAt { get; init; } = ""` | **(gap)** |

## `DispositionRow`

*record* — `WorkbenchRecords.cs`

One actor's disposition on one subject at one baseline. Satisfaction is never a disposition.

| Member | Summary |
|---|---|
| `long IngressSeq { get; init; }` | **(gap)** |
| `string RecordedAt { get; init; } = ""` | **(gap)** |

## `ObligationRow`

*record* — `WorkbenchRecords.cs`

One obligation at one checklist revision; its existence is "proposed".

| Member | Summary |
|---|---|
| `long IngressSeq { get; init; }` | **(gap)** |
| `string RecordedAt { get; init; } = ""` | **(gap)** |

## `HarnessProfileRow`

*record* — `WorkbenchRecords.cs`

One harness-profile version. Type-0: `Family` and `EngineId`.

| Member | Summary |
|---|---|
| `long ProfileKey { get; init; }` | The version's surrogate key — what an attempt's `ProfileKey` points at. |
| `int VersionNo { get; init; }` | **(gap)** |
| `long IngressSeq { get; init; }` | **(gap)** |
| `string RecordedAt { get; init; } = ""` | **(gap)** |

## `ProfileObservationRow`

*record* — `WorkbenchRecords.cs`

One observation of one profile version. Absence of a row = Not recorded.

| Member | Summary |
|---|---|
| `long IngressSeq { get; init; }` | **(gap)** |
