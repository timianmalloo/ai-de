---
id: note-understanding-views-n4-entry-points
title: "N4 Test Architect BLOCK for D-1 listing spec — grain still Flagged; Open Sequence stays mapping-unavailable"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "understanding-views-d1"
tags: [decision-note, understanding-views, N4, D-1, entry-points, test-architect]
links:
  - { to: spec-entry-points, rel: depends-on }
  - { to: note-d1-n1-inventory, rel: depends-on }
  - { to: note-d1-codex-entry-point-handshake-r3, rel: depends-on }
  - { to: note-d1-codex-entry-point-handshake-r5, rel: relates-to }
  - { to: note-d1-codex-entry-point-handshake-r6, rel: relates-to }
  - { to: note-d1-listing-query-architecture, rel: relates-to }
  - { to: note-understanding-views-n4-pass, rel: relates-to }
review-by: 2027-03-16
review-suggested: []
summary: >-
  Test Architect (Adversary) N4 BLOCK on docs/specs/entry-points.md. Grain still
  Flagged in the spec while tests pin member NodeId = declaring type. F-EP is
  unnamed. Candidate set is unnamed. Open Sequence stays mapping-unavailable.
  Spec status stays draft. Authors do not self-clear. This reviewer did not
  author the spec and does not mark it accepted.
---

# N4 Test Architect BLOCK for D-1 listing spec

*A decision note (`knowledge-visualization.md` V17). One note per call. This is the Test Architect receipt, not product acceptance.*

- **Kind:** decision (review receipt)
- **Confidence:** **Verified** for citations below (files opened; projection tests executed). **Inferred** where labelled.
- **Made during:** Test Architect (Adversary) N4 of `spec-entry-points`, 2026-09-17. Reviewer did **not** author the spec. Spec `status` stays **draft**. This note does **not** mark the spec accepted (BoK §II.3 D3).

```
PERSONA: test-architect   MODE: Adversary   TIER: T2
VERDICT: BLOCK
CLEARS-THE-VETO: no — spec grain still Flagged; F-EP uncomposed; candidate set unnamed.
```

## Evidence opened (not recalled)

| Artifact | Pin | What was read |
|---|---|---|
| Spec | HEAD `e1589d01` blob `77f8861d` `docs/specs/entry-points.md` | Grain [Flagged] `:81`. US-L1–L5. US-L4 `mapping-unavailable`. Gate record “authors do not self-clear”. |
| N1 inventory | `docs/notes/d1-n1-inventory.md` | Graph id = `node_dim.node_id`. Members are `has_member` strings on the type, **no** own `node_id`. `KindOf` is type-kind. Do not reuse D-0 census grain. |
| Handshake r3 | blob `e448383a` (PRODUCER ACK frozen) | Open Sequence disabled, reason `mapping-unavailable`. `node_id` / classification never E1 input. Listing independent of mapper. |
| Handshake r5 | blob `a3cb0d63` | Authorship freeze only. Sequence stays disabled. |
| Handshake r6 | `docs/notes/d1-codex-entry-point-handshake-r6.md` **proposed** | Mapping-impl proposal. Empty/absent Core API ⇒ Sequence stays `mapping-unavailable`. ACK of r5 is not r6 ACK. |
| Listing architecture | blob `916c5186` `docs/notes/d1-listing-query-architecture.md` | Member `NodeId` **null** until minting. Stale vs tests. |
| Projection tests | blob `893548ca` `tests/AiDe.Core.Tests/EntryPointsProjectionTests.cs` | Executed: 7 passed, 0 failed (`dotnet test --filter EntryPointsProjectionTests`). |
| Grain-close commit | `e61e6aa7` | Member rows `NodeId: typeId` (was `null` in `e093dad0`). |
| Surface tests | `tests/AiDe.App.Tests/EntryPointsSurfaceTests.cs` | `OpenSequenceEnabled` false; UIA Name contains `mapping-unavailable`. |
| Projection | `src/AiDe.Core/Projections/EntryPointsProjection.cs` | Member `NodeId: typeId`. `KindFromDisplay` name heuristics. |
| Extractor members | `src/AiDe.Core/Extraction/CSharpExtractor.cs` `:188-201`, `:530-536` | `has_member` object is UML compartment text (`+ Main()`), not bare `Main`. Cap 40 + `members_truncated`. |
| Store | `StoreReader.SourceHasMembers` `:398-417` | `(TypeNodeId, Member)` grouped; listing display only. |
| Workbench | `WorkbenchShell.OnEntryPointsActivateRequested` `:2170-2194` | Graph/source only. No Sequence branch. |
| §A5 D-1 | `docs/specs/addendum-c-perspectives.md` `:299-303` | List API/UX/CLI as a node with kind; unclassified never silent. |
| D-0 N4 precedent | `note-understanding-views-n4-pass` | First Test Architect pass **BLOCK**ed on unsatisfiable fixture; PASS only after grain closed **in the spec** and F* composed. |

Green projection tests prove the **implementation** grain. They do **not** prove the spec. A gate exit is not a spec pass.

## The call

**N4 BLOCK.** Hard veto stands. Architecture (N5) for the listing query is not unblocked by this receipt.

Open Sequence is **not** the blocker. US-L4 is falsifiable and currently held. The blocker is that US-L1/L2/L3 have **two values** for member identity, so a test cannot fail for a single Then.

### Open Sequence — must stay `mapping-unavailable` **[Verified]**

Spec `:34-44`, `:103-105`, UX flow `:162-163`, UI signature `:176`: live Open Sequence is disabled, reason **`mapping-unavailable`**. r3 frozen. r5 authorship-only. r6 proposed stub returns empty until Core observation API is admitted **and** r6 is ACKed.

Tests that lock the freeze:

- `EntryPointsSurfaceTests.Show_UnclassifiedRows_SequenceDisabled` — button disabled; UIA Name contains `mapping-unavailable`; still disabled after Enter / Ctrl+Enter.
- `EntryPointsProjectionTests.Listing_DoesNotCarryE1ObservationIds` / `QueryJson_HasOnlyMaxRowsOnTheWire` — wire has no `observation` / `sequence`.
- `OnEntryPointsActivateRequested` has GraphNeighbourhood and Source only.

**Failing input (must keep failing):** any path that enables Open Sequence, passes `Kind`/`NodeId`/display into E1, or treats r6 proposal/ACK as Sequence activation.

This receipt **forbids** enabling Sequence as a side-effect of closing grain. Declaring-type `node_id` is a **graph/source** id, never an E1 observation id.

### Grain as it must be closed (not closed in the spec)

Tests at `e61e6aa7` / `HasMember_MainIsCli_GraphIdIsDeclaringType` already pin:

> Member listing rows use the **declaring type** `node_id` for graph and source.

Spec `:81` still says grain is **[Flagged]**, members have no `node_id`, and “UV-0 must not pretend members already have `node_id`.” Listing architecture `:36` still says member `NodeId` null.

Three values for one quantity (defect signature):

| Surface | Member `NodeId` |
|---|---|
| Spec `:81` + US-L2/L3 “row without `node_id`” | none; graph/source disabled |
| Architecture note `:36` | null until mint |
| Tests + `EntryPointsListing.FromHasType` | declaring type id (`App.Program` for `.Main`) |

N1 is still true: members are **not** graph nodes. Reusing the type id is a **borrow** for `DescribeAsync`/`GraphAsync`/`NodeContentAsync`, not minting a member `node_id`. The spec must say that, or US-L2’s “not a fake neighbourhood” fails: selecting `Main` centres the **type** neighbourhood. Honest only when named. Fake if the operator is told it is the method.

**Row identity ≠ graph id.** After the borrow, type row and member rows share `NodeId`. US-L1 “stable row identity” is unnamed. `NodeId` cannot be the row key.

## FINDINGS

### Blockers (hard veto)

1. **[Blocker] (Verified) Spec grain still [Flagged]; member graph id is three-valued.** evidence: spec `:81`; architecture `:36`; test `HasMember_MainIsCli_GraphIdIsDeclaringType` asserts `NodeId == "App.Program"`; prior `e093dad0` asserted `null`. fix: authors close grain **in the spec** to the only grain this review will accept on re-review (see *What would clear*). Do not mint member nodes this slice. Do not leave US-L2/L3 “no `node_id`” as the member path if members are listed.

2. **[Blocker] (Verified) US-L1 fixture F-EP is a name, not a composition.** evidence: spec `:92` “fixture F-EP with known API, UX, and CLI occurrences”; D-0 required a composed F* table (`spec-understanding-views` `:348-358`) before N4 PASS. No F-EP table, no expected kinds, no store predicates. fix: compose F-EP like F* (table of occurrences → expected kind + row identity + `NodeId`).

3. **[Blocker] (Verified) F-EP, if it uses the projection test’s `has_member` object `Main`, does not match the store.** evidence: test `:74` `Assertion("App.Program", "has_member", "Main")` and `Display.EndsWith(".Main")`; extractor `:534-535` emits `+ Main()` (visibility + signature). `KindFromDisplay` matches `.Main` / `Main`, not `+ Main()`. A real `Program.Main` from the extractor would not take the CLI branch the test names. fix: F-EP member objects must be extractor-shaped (or the listing must parse `has_member` text). A fixture that cannot occur in the store is Mock Fiction.

4. **[Blocker] (Verified) Candidate set is unnamed, so never-silent-drop cannot fail.** evidence: spec invariant `:79` “every candidate the index produced”; N1 “do not reuse census grain”; implementation lists **all** `has_type` then **all** `has_member`. That is a type+member census, not “entry points.” §A5 unclassified clause is “an **entry point** the index could not classify,” not every field of every class. Without a named candidate predicate, listing everything and listing a heuristic subset both satisfy “never drop a candidate.” fix: name the candidate set in the spec (predicate, not algorithm). Then never-silent-drop has a failing input.

### Majors (soft veto — do not clear the hard veto)

5. **[Major] (Verified) US-L1 “matching kind” has no oracle besides an unspecified name heuristic.** evidence: spec `:136` “does not pick the extractor algorithm; architecture must”; `KindFromDisplay` (`Controller`/`Endpoint`/`Api.`/`Program`/`Main`/`Window`/…). Architecture may pick the algorithm **after** F-EP pins expected kinds. Without that pin, “matching kind” is unfalsifiable.

6. **[Major] (Verified) `HasTypeNodes_AreUnclassified_NeverSilentDrop` does not test unclassified or never-silent-drop.** evidence: test name vs `:36-37` `EntryPointKind.Api` / `Cli` for `Api.Orders` / `Cli.Program`; `UnclassifiedReason` null. Coverage Theater if treated as the US-L1 unclassified oracle. The only unclassified assert is the pure `KindFromDisplay("Domain.Order")` fact — no `FromHasType` row with reason. fix: a fixture type that must appear as `unclassified` with a reason; rename the lying test.

7. **[Major] (Verified) Cap omit order unspecified.** evidence: `FromHasType` concatenates types then members, then `Take(cap)`. When types ≥ cap, every member is omitted. US-L5 only requires an omitted count. Same class as D-0 T5c (named drop set vs alphabetical prefix). fix: spec names omit order or a named survivor (or “order unspecified; tests must not require a particular victim”).

8. **[Major] (Verified) `members_truncated` is a silent candidate drop if members are candidates.** evidence: extractor cap 40 + `members_truncated` fact; listing does not surface it. Spec invariant forbids dropping a candidate without disclosure. fix: if members are candidates, truncated members disclose; if they are not candidates, say so in the candidate-set sentence.

9. **[Major] (Inferred) Part A has no Gherkin for empty index / query error / no-workspace.** UX flow names them (`:154-155`, recovery Retry). D-0 put those on the functional layer so the Test Architect could accept them. Residual until Part A has Given/When/Then or an explicit “UX-only; oracle is the surface.”

### Nits

10. **[Nit] (Verified)** `ProjectionService.EntryPoints` xml still says “all rows unclassified.” Tests classify. Two definitions of one quantity.
11. **[Nit] (Verified)** `EntryPointsSurface` comment still says Sequence disabled “until mapper r4 is frozen.” r3 already froze Sequence dark; r4 was not accepted; r5/r6 keep it dark.

## What would clear (authors — not this reviewer)

Re-review is a **new** Test Architect pass on repaired spec text. This reviewer does not clear that pass.

Grain close **in the spec** (must match tests; must not enable Sequence):

- One row is exactly one **listing occurrence** in the current snapshot: either one `has_type` node **or** one `has_member` fact.
- **Type row:** `NodeId` = that type’s `node_dim.node_id`.
- **Member row:** `NodeId` = **declaring type** `node_id`, used only for `DescribeAsync` / `GraphAsync` / `NodeContentAsync`. This is a borrow, not a minted member graph node, not an E1 observation id.
- **Select / View source** on a member row scopes/opens the **declaring type**. Named as type neighbourhood/source so it is not a fake member node (US-L2).
- **Row identity** is not `NodeId` (shared). Name it: `Display` and/or `(declaring_node_id, has_member object)`.
- **Open Sequence** remains disabled, reason `mapping-unavailable`, for every row. r6 does not turn it on.
- US-L2/L3 “row without `node_id`” either (a) is withdrawn for this slice (every listed occurrence has a declaring type id) or (b) is reserved for a named non-graph candidate that this slice does not emit.

Also required before re-review:

- Compose **F-EP** (API, UX, CLI, unclassified) with store predicates. Member objects match extractor text, or the spec says the listing parses `has_member`.
- Name the **candidate set**. If it is not “all `has_type` + all `has_member`,” say what is excluded. If it is that census, reconcile N1 “do not reuse census grain” in writing.
- Keep US-L4 and the wire/UI tests that forbid E1 ids.

Then: non-author Test Architect re-review. Spec `status` stays draft until a later acceptance the authors do not self-issue.

## Alternatives dismissed

- **PASS / PASS-WITH-CONDITIONS now.** Grain Flagged in the spec is the same class D-0 blocked on. Conditions already in the spec could justify PASS-WITH-CONDITIONS; these are not in the spec.
- **Treat green `EntryPointsProjectionTests` as N4.** That suite traces the implementation, not blob `77f8861d`. Unverified Green for the spec.
- **Accept member `NodeId` null (`e093dad0`).** Superseded by `e61e6aa7`. Listing members with no graph id leaves US-L2/L3 dark for every CLI `Main`. The declared grain is declaring-type id for graph/source.
- **Mint member `node_id`s this slice.** N1: members are attributes. Minting is a new identity domain. Out of this listing spec. Sequence still would not consume them (r3).
- **Enable Open Sequence because members now have a `NodeId`.** `NodeId` is the type graph id. r3: classification and `node_id` are never E1 input. r6 empty map keeps Sequence dark.
- **Mark the spec accepted.** Authors and this reviewer do not self-accept.
- **Edit `src/` in this turn.** Out of scope.

## Residuals (not this veto)

- Classifier algorithm (architecture / design-slice) once F-EP pins expected kinds.
- Production cap value (Inferred; tests inject `MaxRows`).
- p95 latency (recorded, not CI-asserted — ADR-0029 class).
- Proof Pack at implement (red-before-green). Absence of a Proof Pack is **not** this BLOCK’s reason; this is specify N4.
- r6 ACK track (Codex). Listing N4 does not wait on it. Sequence still does.

## What the Conductor may do next

1. Return this BLOCK to spec authors. Repair grain + F-EP + candidate set **in** `docs/specs/entry-points.md`.
2. Dispatch a **different** Test Architect (or this persona in a new session that did not author the repair) on the repaired blob.
3. Keep Open Sequence disabled in spec, tests, and UI.

## What the Conductor must not do

- Mark `spec-entry-points` `accepted`.
- Treat this note as N4 PASS.
- Enable Open Sequence, pass listing rows into E1, or implement a mapper as a listing fix.
- Author `src/` from this receipt.

## Promotion rule

This note is the N4 Test Architect receipt for D-1 listing. It does not supersede r3/r5. It does not accept r6. A later N4 PASS note (new file or this file superseded) is required before anyone claims the specify gate is clear.
