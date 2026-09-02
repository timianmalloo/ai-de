---
id: api-aide-core-facts
title: "API: AiDe.Core.Facts"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.Core.Facts: 14 types, 10 members, 96% carrying a summary doc comment.
---

# API: `AiDe.Core.Facts`

**14 public types · 10 public members · 96% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `DisclosureKind`

*enum* — `DisclosureKinds.cs`

Whether a disclosure describes a decision or a defect.

## `DisclosureKinds`

*class* — `DisclosureKinds.cs`

Which disclosures are boundaries and which are gaps, stated once.

**Remarks.** **Why this is a list and not a rule about names.** The convention is real —
`-not-indexed` and `-not-analysed` tend to be boundaries, `-missing` and
`-not-resolved` tend to be gaps — and it is a convention, not a guarantee.
`schema-changed-by-raw-sql-not-read` reads like a boundary and is a gap: the schema can be
quietly wrong. A suffix rule would classify it confidently and wrongly, which is worse than a
list somebody has to maintain, because the list has a test that fails when it goes stale.





**Why the distinction earns its own type.** Conflating the two has cost this project
twice, both measured. Python disclosed 246 "unresolved" imports that were all standard library —
a boundary reported as a gap — and it was ranked the largest coverage hole in any extractor on
the strength of the number. TypeScript's equivalent was 83% invented facts. Both are DC-050, and
the fix each time was to say which kind it was.





A surface that can show only one line should show a gap; a panel listing everything should
separate them. Neither should have to infer it from a name.

| Member | Summary |
|---|---|
| `DisclosureKind KindOf(string disclosure)` | The kind of a disclosure, by class name or by a whole folded line. |
| `bool IsClassified(string disclosureClass)` | Whether this disclosure has been classified at all. |

### `DisclosureKind KindOf(string disclosure)`

The kind of a disclosure, by class name or by a whole folded line.

**Remarks.** An unknown name is a `Gap`, deliberately. A disclosure nobody has
classified is more likely to be new than to be harmless, and the cost of the two mistakes is
not symmetric: a boundary shown as a gap wastes a reader's attention once, while a gap shown
as a boundary is a defect filed under "working as intended". `EveryDisclosureHasAKind`
exists so this default stays a safety net rather than a habit.

## `DisclosureSummary`

*class* — `DisclosureSummary.cs`

Folds per-scope disclosures into one line per class, with the counts added up.

**Remarks.** **Every disclosure was right and the list was unusable.** A disclosure is emitted per
scope, conditional, and carrying its own count — which is the rule this codebase arrived at after
several defects, and it is correct. Nobody said what happens when 39 knowledge scopes each emit
the same two. MEASURED on TheTerrace after a real index: **178 disclosure strings, 108 distinct,
for 28 actual classes** — `knowledge-headings-not-analysed` alone appeared 39 times, each
with a different number, so `Distinct()` could not merge any of them.





The result filled the user's window: roughly sixty lines of near-identical text, with the
one finding that mattered — 109 prose links naming a file that is not there — buried in the
middle of it. **A boundary stated 39 times is noise, and noise is where a real signal goes to
hide.** That is this codebase's own lesson about disclosures, arrived at from the other
direction: it has spent a lot of effort making sure they fire, and none on what a reader does
with sixty of them.





**The counts are summed, not the lines deduplicated.** "914 headings in one scope" and
"4,471 headings across the workspace" are different facts, and only the second answers "how much
of this repository is unread". The explanatory half of the sentence is kept from the first
occurrence, because every scope emits the same template.

| Member | Summary |
|---|---|
| `IReadOnlyList<string> Fold(IEnumerable<string> disclosures)` | One line per disclosure class, counts summed across every scope that raised it. |
| `long CountIn(string folded)` | The count a folded disclosure carries, or zero when it names no number. |

### `long CountIn(string folded)`

The count a folded disclosure carries, or zero when it names no number.

**Remarks.** Exposed so a caller choosing WHICH disclosure to show does not re-parse the sentence this
class just built — two readers of one format is how the two halves of a rule drift apart.

## `SessionProcessingClass`

*enum* — `Dispatch.cs`

The declared downstream processing posture of an agent session. Authorization for every MCP tool
call is bound to this (ADR-0011): the transport says who connected, this says where the bytes go next.

## `CallerKind`

*enum* — `Dispatch.cs`

*No doc comment on this type.* **(gap)**

## `CallerPrincipal`

*record* — `Dispatch.cs`

A stable principal, server-derived from the authenticated connection and invariant across
reconnects and core epochs. Never read from a command payload, and never connection-scoped —
a connection-scoped identity would void receipt dedup across the crash window it exists for.

## `DispatchState`

*enum* — `Dispatch.cs`

The folded state of one dispatch key. `ending` is durable and written *before* the
terminal write; recovery resolves an unresolved attempt to `eliveryUnknown`.

## `DispatchReceipt`

*record* — `Dispatch.cs`

The folded receipt for one dispatch key, derived from its attempt and outcome events.

| Member | Summary |
|---|---|
| `bool BlocksReExecution` | True when a retry may not re-execute. Every state qualifies once an attempt exists — that is the point of the write-ahead record. |

## `PtyWriteResult`

*enum* — `Dispatch.cs`

What a terminal write actually proved.

## `EvidenceOrigin`

*enum* — `EvidenceAssertion.cs`

How the evidence was acquired. Never collapsed with `erificationStatus`.

## `VerificationStatus`

*enum* — `EvidenceAssertion.cs`

How well the evidence is established. Deliberately separate from `videnceOrigin`:
the spec forbids collapsing acquisition and validation into one confidence word.

## `Provenance`

*record* — `EvidenceAssertion.cs`

Where an assertion came from, so a claim can always be traced back to an artifact.

## `EvidenceAssertion`

*record* — `EvidenceAssertion.cs`

The fact grain: one row is exactly one assertion by one extractor about one normalized
(subject, predicate, object) relation at one artifact revision.

| Member | Summary |
|---|---|
| `string AssertionId { get; } = ComputeId(` | Deterministic identity: re-extracting an unchanged artifact yields the same id, so a replay is idempotent rather than a duplicate. Computed, never supplied. |

## `EvidencePredicates`

*class* — `EvidencePredicates.cs`

Which predicates carry a VALUE and which carry a reference to another node.

**Remarks.** The fact grain deliberately does not distinguish them — every row is
(subject, predicate, object), and an attribute genuinely IS a relation to a value. What must not
follow is that every value becomes something a user can navigate to.





**Found by indexing a real repository.** `api_version` put `2020-02-02` in the
graph and `resource_name_expression` put `'${namePrefix}-acs'` there, so dates and
unevaluated strings ranked alongside types as things to explore.





**One list, used by every reader.** The first version of this fix lived only in the
ingest path, and search kept returning the junk because search reads the assertions directly
rather than the node table — two places deciding the same thing, one of them wrong.





An explicit list rather than a naming convention: a convention silently misclassifies the
first predicate that does not follow it, and a misclassification here puts junk in the graph
instead of failing.

| Member | Summary |
|---|---|
| `IReadOnlySet<string> Attributes { get; } = new HashSet<string>(StringComparer.Ordinal)` | Predicates whose object is a value, not a node. |
| `IReadOnlySet<string> Identity { get; } = new HashSet<string>(StringComparer.Ordinal)` | The few facts that say WHAT A NODE IS, as opposed to what it is connected to. |
| `string IdentitySqlList { get; } =` | The SQL literal list for `dentity`, generated from the same set. |
| `string SqlList { get; } =` | The SQL literal list for an `IN` clause. Built from the same set. |

### `IReadOnlySet<string> Identity { get; } = new HashSet<string>(StringComparer.Ordinal)`

The few facts that say WHAT A NODE IS, as opposed to what it is connected to.

**Remarks.** **Why a bounded read needs this.** A node's facts are capped, and they were ordered
alphabetically — so a node with more relations than the cap lost its own type, owner and class
to its own links. MEASURED: 12 of 877 knowledge documents were already over the 50-row ceiling
before anything was added to them, and simulating headings pushed nearly every structured
document over. `adr-0015-erasure-ledger-durable-model` would have returned 44 headings and
none of `has_type`, `node_class`, `owned_by`, `refines` or `review_by`.





**Deliberately not "all attributes".** `has_member` is an attribute and a type
can carry forty of them; putting the whole attribute set first would replace one flood with
another. This is the small, fixed set that answers "what is this thing" — everything else,
attribute or relation, competes on equal terms behind it.

### `string SqlList { get; } =`

The SQL literal list for an `IN` clause. Built from the same set.

**Remarks.** Generated rather than typed a second time: a hand-written copy in a query is exactly how the
two halves of this rule drift apart, which is the defect that produced it.
