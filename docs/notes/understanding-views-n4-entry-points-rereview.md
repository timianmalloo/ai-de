---
id: note-understanding-views-n4-entry-points-rereview
title: "N4 Test Architect re-review D-1 listing spec — PASS-WITH-CONDITIONS; Open Sequence stays mapping-unavailable"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "understanding-views-d1"
tags: [decision-note, understanding-views, N4, D-1, entry-points, test-architect, rereview]
links:
  - { to: spec-entry-points, rel: depends-on }
  - { to: note-understanding-views-n4-entry-points, rel: depends-on }
  - { to: note-d1-n1-inventory, rel: depends-on }
  - { to: note-d1-codex-entry-point-handshake-r3, rel: depends-on }
  - { to: note-d1-codex-entry-point-handshake-r5, rel: relates-to }
  - { to: note-d1-listing-query-architecture, rel: relates-to }
  - { to: note-understanding-views-n4-pass, rel: relates-to }
review-by: 2027-03-16
review-suggested: []
summary: >-
  Test Architect (Adversary) N4 re-review of docs/specs/entry-points.md after
  82f1710b. Prior BLOCK's four hard findings are closed. Verdict PASS-WITH-CONDITIONS.
  Open Sequence stays mapping-unavailable. Spec status stays draft. This reviewer
  did not author the spec or the repair and does not mark the spec accepted.
---

# N4 Test Architect re-review D-1 listing spec — PASS-WITH-CONDITIONS

*A decision note (`knowledge-visualization.md` V17). One note per call. This is the Test Architect re-review receipt, not product acceptance.*

- **Kind:** decision (review receipt)
- **Confidence:** **Verified** for citations below (files opened; projection and surface tests executed this turn). **Inferred** where labelled.
- **Made during:** Test Architect (Adversary) N4 **re-review** of `spec-entry-points` after BLOCK `note-understanding-views-n4-entry-points` (commit `b256f184`), 2026-09-17. Reviewer did **not** author the spec and did **not** author repair `82f1710b`. Spec `status` stays **draft**. This note does **not** mark the spec accepted (BoK §II.3 D3). This reviewer does **not** clear a veto they authored — the prior BLOCK was another reviewer.

```
PERSONA: test-architect   MODE: Adversary   TIER: T2
VERDICT: PASS-WITH-CONDITIONS
CLEARS-THE-VETO: yes — prior four Blockers closed in the spec; Unresolved Blocker count: 0.
  Traced tests for grain, F-EP kinds, Sequence freeze: yes.
  Proof Pack: N/A this specify N4 (same as the BLOCK residual; required at implement).
```

## Evidence opened (not recalled)

| Artifact | Pin | What was read |
|---|---|---|
| Spec | HEAD `82f1710b` blob `6945c5bc` `docs/specs/entry-points.md` | `status: draft` `:5`. Grain **(closed)** `:83`. Candidate set named `:85`. Identity `:87-89`. F-EP composed `:102`. US-L2/L3 declaring-type `:105-110`. US-L0 `:111-115`. US-L4 `mapping-unavailable` `:118-119`. UX mermaid still has `no node_id` `:173`, `:176`. Gate record still says grain Flagged `:199`. Residual says grain closed `:204`. Handoff still says identity minting `:206`. |
| Prior BLOCK | commit `b256f184` blob `8375b1cc` `docs/notes/understanding-views-n4-entry-points.md` | Four Blockers: grain Flagged; F-EP uncomposed; `has_member` `Main` ≠ extractor; candidate set unnamed. What-would-clear list. Sequence must stay dark. |
| Repair | `82f1710b` | Spec grain/F-EP/candidate-set; architecture identity; `MemberBareName`; F-EP oracle test. Spec stays draft. Author `timianmalloo`, not this reviewer. |
| N1 inventory | `docs/notes/d1-n1-inventory.md` | Members are `has_member` strings; no own `node_id`. Do not reuse D-0 census grain. |
| Handshake r3 | blob `e448383a` | Open Sequence disabled, reason `mapping-unavailable`. `node_id` / classification never E1 input. |
| Handshake r5 | blob `a3cb0d63` | Authorship freeze only. Sequence stays disabled. |
| Listing architecture | blob `e9041116` `docs/notes/d1-listing-query-architecture.md` | Member `NodeId` = declaring type; Display extractor-shaped; no minted member node. Still says “r4 unfrozen” `:21` and “r4 ACK pending” `:48`. |
| Projection tests | blob `e2d1121b` `tests/AiDe.Core.Tests/EntryPointsProjectionTests.cs` | **Executed this turn:** 8 passed, 0 failed (`dotnet test --filter FullyQualifiedName~EntryPointsProjectionTests`). `Fep_ComposedOracle` uses store `+ Main()`. `HasMember_MainIsCli_GraphIdIsDeclaringType` asserts `NodeId == "App.Program"`. `EmptyIndex_RowsEmpty_OmitZero`. `Listing_DoesNotCarryE1ObservationIds`. |
| Surface tests | `tests/AiDe.App.Tests/EntryPointsSurfaceTests.cs` | **Executed this turn:** 2 passed, 0 failed. `OpenSequenceEnabled` false; UIA Name contains `mapping-unavailable`. Activate is GraphNeighbourhood / Source only. |
| Projection | blob `40881de2` `src/AiDe.Core/Projections/EntryPointsProjection.cs` | Member `NodeId: typeId`. `MemberBareName("+ Main()")` → `Main`. Types then members, then `Take(cap)`. No `members_truncated` disclosure. |
| Extractor members | `CSharpExtractor.cs` `:188-201`, `:530-536`, `:203-213` | `has_member` object is UML text (`+ Main()`). Cap 40 + `members_truncated`. |
| Store | `StoreReader.SourceHasTypeNodes` `:372-392`; `SourceHasMembers` `:398-417` | Types: latest `has_type` where `node_kind <> 'knowledge'`, `ORDER BY subject`. Members: latest `has_member`, **no** knowledge filter, `ORDER BY subject, object`. |
| Workbench | `WorkbenchShell.OnEntryPointsActivateRequested` `:2170-2194` | Graph/source only. No Sequence branch. |
| Surface | `EntryPointsSurface.cs` `:11`, `:26`, `:99` | Button created disabled; `Show` forces `IsEnabled = false`. Comment still says “until mapper r4 is frozen.” |
| D-0 N4 precedent | `note-understanding-views-n4-pass` | First Test Architect pass BLOCKed; PASS after grain closed **in the spec** and F* composed. Spec stayed draft. Dual-prose at N10 was Major, not Blocker (`note-understanding-views-n10-design-acceptance`). |

Green tests prove the **implementation** grain and the Sequence freeze. They do **not** by themselves pass the spec. This receipt judges the repaired spec text against the prior BLOCK’s what-would-clear list.

## Prior Blockers — disposition

| # | Prior Blocker | Disposition |
|---|---|---|
| 1 | Spec grain still [Flagged]; member graph id three-valued | **Closed.** Conceptual model grain is **(closed)** `:83`. Identity `:87-89` matches tests (`HasMember_MainIsCli_GraphIdIsDeclaringType`: member `NodeId == "App.Program"`) and architecture `:33-36`. Borrow of declaring-type id, not a minted member node, not an E1 id. US-L2/L3 “row without `node_id`” Gherkin **withdrawn**; member select/source uses declaring type. |
| 2 | F-EP a name, not a composition | **Closed as a Blocker.** US-L1 Given lists store predicates; Then pins kinds. `Fep_ComposedOracle` is the same fixture. Remaining hole (Program type row / NodeIds not in Then) is **Major 1** below, not “uncomposed.” |
| 3 | F-EP `has_member` object `Main` ≠ extractor | **Closed.** Spec, tests, and `MemberBareName` use extractor shape `+ Main()`. Extractor `:534-535` emits visibility + `Name(params)`. Store-shaped fixture can occur. |
| 4 | Candidate set unnamed | **Closed.** Named `:85`: latest-generation `has_type` on non-knowledge subjects **union** latest-generation `has_member`. Order types-then-members; cap takes that prefix. N1 “do not reuse D-0 census grain” reconciled in writing (`:91` **Not:** D-0 census grain). This **is** a type+member census; never-silent-drop now has a failing input. |

## Open Sequence — must stay `mapping-unavailable` **[Verified]**

Not a residual. Not lifted by this receipt. US-L4 `:118-119`, core scenario `:61`, UX `:177`, UI `:190`. r3 blob `e448383a` frozen.

Tests that still lock the freeze (executed this turn):

- `EntryPointsSurfaceTests.Show_UnclassifiedRows_SequenceDisabled` — button disabled; UIA Name contains `mapping-unavailable`; still disabled after Enter / Ctrl+Enter; activate kind is GraphNeighbourhood then Source.
- `EntryPointsProjectionTests.Listing_DoesNotCarryE1ObservationIds` / `QueryJson_HasOnlyMaxRowsOnTheWire` — wire has no `observation` / `sequence`.
- `OnEntryPointsActivateRequested` has GraphNeighbourhood and Source only.

**Failing input (must keep failing):** any path that enables Open Sequence, passes `Kind`/`NodeId`/display into E1, or treats r6 proposal/ACK as Sequence activation.

Declaring-type `node_id` is a **graph/source** id, never an E1 observation id. Closing grain does **not** enable Sequence.

## FINDINGS

### Blockers (hard veto)

None. Prior four Blockers are closed in the spec. Unresolved Blocker count: 0.

### Majors (soft veto — conditions; do not restore the hard veto)

1. **[Major] (Verified) F-EP Then does not pin every Given occurrence, nor NodeId / row identity.** evidence: Given includes `App.Program` `has_type` **and** `has_member` `+ Main()`. Then names Controller / MainWindow / `+ Main()` member / Order — not the `App.Program` **type** row. `KindFromDisplay("App.Program")` is `Cli` (`EntryPointsListing.KindFromDisplay` `.Program` branch). `Fep_ComposedOracle` uses `Rows.Single(r => r.Display.Contains("Main()"))` and does not `Assert.Equal(5, result.Rows.Count)` or pin `NodeId`. A listing that dropped the Program type row and kept the member would still satisfy US-L1 Then as written. fix: F-EP Then (and the oracle) pin all five rows: Displays, Kinds, NodeIds (`Orders.OrdersController`, `Shell.MainWindow`, `App.Program`, `App.Program.+ Main()` with NodeId `App.Program`, `Domain.Order` unclassified + reason).

2. **[Major] (Verified) Gate record and handoff still carry a second grain.** evidence: `:199` “grain Flagged until architecture mints member identity”; `:206` “listing query + identity minting”; `:83` and `:204` grain **closed** as declaring-type borrow, **not** minted. Two definitions of one quantity (same class as N10 Zone dual prose — Major there, Major here). N5 that mints member nodes would reopen Blocker 1. fix: strike Flagged/minting from the gate record and handoff. N5 implements the **closed borrow**. This receipt is the pin if the leftover sentences remain until the next spec edit.

3. **[Major] (Verified) `members_truncated` disclosure is promised in the candidate-set sentence and has no Then.** evidence: spec `:85`; extractor `:203-213`; `FromHasType` disclosures are only `Omitted (N)` from the listing cap (`EntryPointsProjection.cs` `:75-77`). Listing those 40 without the fact is the DC-025 shape the extractor comment names. Prior BLOCK Major 8 — half-closed: named in the spec, not falsifiable as a user story, not implemented. fix: US-L5 (or a sibling) Given `members_truncated` Then disclosure present; or an explicit “implement residual — not US-L1 completeness.” If members are candidates, truncated members must disclose.

4. **[Major] (Verified) US-L0 error / no-workspace have Gherkin and no Entry-points surface oracle.** evidence: spec `:113-115`; `EntryPointsSurface.ShowError` / `ShowNoWorkspace` exist; `EntryPointsSurfaceTests` has no `ShowError` / `ShowNoWorkspace` facts (grep this turn: those names live on Solution tree / class diagram / diagnostics, not Entry-points). Empty index **is** tested (`EmptyIndex_RowsEmpty_OmitZero`). fix: surface facts for the two copy strings, or mark them UX-only with the surface as oracle in the spec.

### Minors / nits (do not hold N4)

5. **[Minor] (Verified) UX mermaid still branches on missing `node_id` (`:173`, `:176`) after Part A withdrew that path.** This slice’s candidate set always has a declaring type id. fix: label the branch reserved, or delete it.

6. **[Minor] (Verified) Architecture note identity matches the spec; Sequence comments still say r4 unfrozen / ACK pending (`:21`, `:48`).** r3 already froze Sequence dark. Residual on that draft note, not this spec’s grain.

7. **[Nit] (Verified)** `ProjectionService.EntryPoints` xml still says “Classification pending; all rows unclassified.” Tests classify.

8. **[Nit] (Verified)** `EntryPointRow.NodeId` xml still says “otherwise null.” Member rows always borrow a type id this slice.

9. **[Nit] (Verified)** `EntryPointsSurface` comment still says Sequence disabled “until mapper r4 is frozen.” r3 already froze it dark.

10. **[Nit] (Verified)** `EntryPointsProjectionTests` class summary still says “has_type nodes as unclassified.”

## What would still BLOCK (none of these hold)

- Grain Flagged **in the conceptual model**, or member `NodeId` three-valued across spec / architecture / tests.
- F-EP a name with no store predicates.
- `has_member` fixture that cannot occur in the store (`Main` vs `+ Main()`).
- Candidate set unnamed so never-silent-drop cannot fail.
- Open Sequence enabled, or listing rows passed into E1.

## Conditions (PASS-WITH-CONDITIONS)

Hard veto from `b256f184` **clears**. Soft conditions:

1. **N5 implements the closed grain: declaring-type borrow, no member-node mint this slice.** Gate-record Flagged/minting sentences are stale; this receipt overrides them. Authors strike them on the next spec edit.
2. **Open Sequence stays `mapping-unavailable`.** Not a condition that can be bargained. r6 ACK does not turn it on from this listing spec.
3. **F-EP Then / oracle pin all five occurrences including NodeIds** (Major 1) — authors or the listing test plan before anyone claims US-L1 never-silent-drop is fully oracled.
4. **`members_truncated` Then or named implement residual** (Major 3).
5. **US-L0 error / no-workspace surface oracles** (Major 4) — implement / UV-1, not a specify re-BLOCK.

Spec `status` stays **draft**. N4 specify gate is **not** product acceptance.

## Alternatives dismissed

- **BLOCK again.** The four hard findings the prior reviewer named are closed in the spec. Re-blocking on leftover gate-record prose would treat a stale sentence as the grain definition while `:83` is closed. N10 precedent: dual prose is Major.
- **PASS with no conditions.** Gate-record minting would let N5 undo the grain close. F-EP Then still lets a dropped Program type row through. Conditions are written so N5 cannot “mint because the handoff said so.”
- **Mark the spec `accepted`.** Authors and this reviewer do not self-accept. D-0 N4 PASS also left the spec draft.
- **Enable Open Sequence because members now have a `NodeId`.** `NodeId` is the type graph id. r3: classification and `node_id` are never E1 input.
- **Treat green `Fep_ComposedOracle` as N4.** It traces the implementation against the composed fixture. This receipt traces the spec. Unverified Green if used as the specify gate.
- **Mint member `node_id`s this slice.** N1: members are attributes. Out of this listing spec. Sequence still would not consume them (r3).
- **Edit `src/` in this turn.** Out of scope.

## Residuals (not this veto)

- Classifier algorithm (architecture / design-slice). F-EP pins expected kinds for that fixture; comparables `:150` still leave the algorithm to architecture. Name-heuristic is Flagged in residual risk — acceptable once F-EP is the oracle.
- Production cap value (Inferred; tests inject `MaxRows`). Types-then-members prefix means a type count ≥ cap omits every member — now specified.
- `SourceHasMembers` does not apply the non-knowledge filter that `SourceHasTypeNodes` does. Spec candidate set names that asymmetry. Not a silent drop of a named candidate.
- p95 latency (recorded, not CI-asserted — ADR-0029 class).
- Proof Pack at implement (red-before-green). Absence of a Proof Pack is **not** a reason to BLOCK specify N4.
- r6 ACK track (Codex). Listing N4 does not wait on it. Sequence still does.

## What the Conductor may do next

1. Record this receipt as N4 **PASS-WITH-CONDITIONS** for D-1 listing specify. Hard veto from `b256f184` is cleared by a **different** Test Architect than the repair author.
2. Open N5 `/define-architecture` for the **listing query only**, implementing the closed grain (borrow). Do not mint member nodes.
3. Keep Open Sequence disabled in spec, tests, and UI.
4. Leave `spec-entry-points` `status: draft`.

## What the Conductor must not do

- Mark `spec-entry-points` `accepted`.
- Treat this note as product acceptance or as permission to enable Open Sequence.
- Pass listing rows into E1, or implement a mapper as a listing fix.
- Author `src/` from this receipt.
- Let N5 mint member identity because gate-record `:199` / handoff `:206` still say “Flagged” / “minting.”

## Promotion rule

This note is the N4 Test Architect **re-review** receipt for D-1 listing. It does not supersede r3/r5. It does not accept r6. It does not supersede the BLOCK note (that note remains the first-pass record). A later product-acceptance of the spec is a different act the authors do not self-issue.
