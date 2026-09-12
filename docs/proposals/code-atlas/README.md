---
id: proposal-code-atlas
title: "Code Atlas 02 - TheTerrace, from solution to source"
type: doc
status: draft
owner: "@timianmalloo"
phase: proposal
tags: [proposal, architecture, code-understanding, solution-explorer, theterrace, ux]
links:
  - { to: spec-addendum-c-perspectives, rel: relates-to }
  - { to: spec-uml-erm-surfaces, rel: relates-to }
  - { to: architecture, rel: relates-to }
  - { to: proof-code-atlas-terrace, rel: tested-by }
review-by: 2026-10-12
summary: >-
  A repository-backed redesign of Code Atlas using TheTerrace. A full tracked-file explorer and
  visual-first overview converge on the same feature, type, member, sequence and source identities.
  Preserves the concept-map abstraction while adding concrete UML and source-line drill-down.
---

# Code Atlas 02: TheTerrace, from solution to source

[Proposal](index.html) | [Interactive mockup](mockup.html)

## What changed in the brief

The user's correction is structural, not a request to put real labels on the toy graph.
Revision 1's graph-to-tree toggle exposed the same small semantic sample. It was not a solution
explorer. It also offered diagram modes without an explicit journey between abstraction levels.
Revision 2 makes the physical file hierarchy first-class and binds every concrete diagram action
to an actual TheTerrace file and source line.

This is still a proposal, not AI-DE implementation. Claude owns the current UI/session delivery.
The accepted Architecture perspective, its sidebar destination and retained host are unchanged.

## Grounding and measured scale

Read-only source: `C:\Projects\TheTerrace`, clean HEAD
`dba2a29c868d9844cb144d693555265d071ecdeb` (observed 2026-09-12).
The source repository is not modified, run, deployed or sent to a model.

Initial inventory from `git ls-files`: **2,531 tracked files; 577 under src; 474 C# files;
85 Razor files; 401 test-tree files; 127 migration C# files**. There are four solution projects:
one product project and three test/compatibility projects. The 194,702 physical C# source lines
include generated migrations/snapshots, so they are not described as hand-authored code.
`terrace-evidence.json` carries the measured counters, source revision, path inventory and hashes.
No count from revision 1's Northstar fixture is retained.

The full tracked-file inventory is browsable. Semantic diagrams and source bodies are a declared
curated subset, not a claim that AI-DE has automatically analyzed every file. Unsupported or
unembedded files remain visible with that limitation; selecting one never substitutes another file.

A broader source scan also captures **1,008 non-migration C# declaration candidates** across the
feature folders. These are lexical declarations, not distinct compiler-resolved types; partial,
nested and conditional declarations are not collapsed into a false type count. Every feature can
descend to its declaration inventory. The deep class/member/sequence examples remain explicitly
curated, and other declarations do not acquire invented members or source bodies.

**Reading path:** TheTerrace README and solution -> Predictions UI components -> IPredictionStore
and PredictionStore -> Prediction invariants -> AppDbContext mapping -> lifecycle/settlement/scoring
-> existing tests. The independent architecture read covers TheTerrace's spec/architecture and
Bicep declarations. Graphify returned truncated documentation-level leads; direct source reads
establish this study's concrete journeys.

## Part A - one model, several altitudes

| Identity | What remains stable |
|---|---|
| Repository snapshot | Git revision and per-file hash. No deployment/runtime claim. |
| File | Real repository-relative path, even when no semantic node or embedded source exists. |
| Type | Declaring file + declared name + source anchor. File and class are not interchangeable. |
| Member | Owning type + member/signature + source anchor. An interface declaration is distinct from its implementation. |
| Relationship/message | Its predicate, participants, supporting file/line and evidence kind. |
| View | Starting style, altitude, lens, selection, open folders, source location and history. |

The physical hierarchy, semantic model and architectural interpretation are different projections.
A feature folder is not automatically a microservice or bounded context. This is one web
application; the abstraction may suggest boundaries but never invent deployable services.

## Part B - two entrances, a continuous journey

**File-first developer**

1. Start in the familiar Solution Explorer: solution, projects, folders, files, source editor.
2. Expand `src/TheTerrace/Components/Shared`; open `PredictCard.razor`.
3. Select `SubmitAsync` in its member outline or activate the event's source-line link.
4. Step up to the containing type/class model or trace that member's behavior.
5. Follow the `IPredictionStore` call, inspect its registration and concrete implementation.
6. Follow a sequence message to the exact supporting source line.
7. Go back without losing the previous file, member, branch or diagram.

**Visual-first architect**

1. Start with the system overview or entry-point catalogue.
2. Choose the Predictions capability, not an undifferentiated repository graph.
3. Keep the existing concept map for responsibility/relationship understanding.
4. Switch to concrete UML: visibility, property types, method signatures, interface realization,
   and schema-backed associations where known.
5. Select a method, inspect callers/branches, then drill into its sequence or source.
6. The physical explorer reveals the same file; it does not disappear because the user started visually.

**Altitude is explicit:** system -> component/feature -> type -> member -> file/source line.
Raising altitude preserves the concrete selection as context; descending never fabricates a
lower-level representation that is unavailable. Breadcrumbs and Back carry the return path.
Source is the lowest altitude, not an inspector teaser showing a few invented lines.

**Two complete behavior examples**

- Submit a prediction: `PredictCard.SubmitAsync` -> `PredictionStore.SubmitAsync` -> fixture
  lookup -> server clock / `Prediction.IsOpenAt` -> Closed / revise / create -> `SaveChangesAsync`
  -> outcome message and reload. `/match/{FixtureId:guid}` is a Blazor route; no fictional HTTP
  `POST /orders` endpoint is invented.
- Settle and score: `PredictionLifecycle.RunAsync` -> `PredictionSettlement.SettleAsync` ->
  seal -> conditional intermediate save -> score -> `PredictionScoring.Score` -> final save.
  A method-level static reconstruction, not a runtime trace or timing report.

Each sequence message and activity decision has a source anchor. The `Closed`/`NoSuchFixture`
branches and existing/new prediction branches stay distinct. Tests are linked source evidence,
not claimed as executed in this task or as measured coverage.

## Azure refinement

Show **named service types within layers**, with component responsibility and source-backed
connections: compute, data, secrets/identity, storage and observability as appropriate to the
actual declarations. “AKS, Key Vault, Cosmos DB” are examples of service vocabulary, not permission
to insert AKS or Cosmos DB into TheTerrace if the inspected Bicep does not declare them.

Keep logical application layers separate from deployment layers. Every resource can descend to
its declaration and configuration evidence. Distinguish deployment dependency, parent/child,
configuration reference and inferred runtime use. Resource health and deployed instances remain
not observed. Layer placement is a curated interpretation over declaration evidence.

The completed source check finds 24 declarations in `infra/main.bicep`: App Service plan/web,
VNet/subnet, SQL server/database/network rule, Storage/Blob/key-ring container, configuration
Key Vault/wrapping key, provider Key Vault, Application Insights/Log Analytics, communication/email
domain/sender, and role assignments. The separate provider-vault template overlaps these resources
and references an existing app. AKS and Cosmos DB are not declared in either inspected root.
Code references show Blob's Data Protection use, separate credential custody, SQL provider and
Azure Monitor configuration. Declarations, configured use and observed operations remain distinct.

**Important regrounding result:** TheTerrace's top-level architecture is now an in-review
editorial target, explicitly not a description of the running implementation. Proposed
Studio/PublicReader/PrivatePreview hosts and target DDD contexts are not manufactured as source
components. This mockup shows current source, with that design gap called out.

## Implementation versus specification

The user added this direction after the original redesign passed its review checkpoint. The
comparison is a view over **two evidence sets**, not a Git diff and not a conformance percentage:

- **As implemented:** the scoped code/declaration snapshot, its revision and extraction coverage.
- **As specified:** selected documents/clauses, their revision, authority, status and supersession.
- **Correspondence:** which source elements may implement which clause, with the basis visible.
- **Assessment:** aligned evidence, planned addition, contrary evidence, uncertain mapping or
  not assessed. Missing evidence is not proof of absence; a future target is not a failed requirement.

TheTerrace is a useful real example because code and target documents can be in the **same git
revision** while describing different delivery states. The architecture's in-review target is not
silently promoted to an accepted specification. A candidate match between the current app and
"Studio" needs review; similar worker names do not establish matching responsibilities.

Each difference must open both its document clause and its source evidence. The proposed analysis
pipeline is: identify authority/scope -> retrieve candidates -> show the evidence -> classify the
gap under an explicit rule -> human review. AI may propose correspondence and explanations; it must
not invent the governing requirement and then declare the code compliant with it. A real conflict
requires contrary source evidence against an applicable, authoritative clause.

This addition is still T1 proposal work, with the same two-agent cap and a bounded 15-call
main-line addition estimate. It adds no product implementation or repository-wide compliance claim.

## Part C - direction

**Medium:** standalone HTML iteration artifact for a future Windows WPF/WebView2 workstation.
Reuse AI-DE's DESIGN.md tokens; do not edit the Shell lane's design language.
**Archetype:** linked IDE workstation, file explorer + editor/canvas + outline/evidence.
**Qualities:** familiar, not constraining; visual, not vague; concrete, not overwhelming.

The main visual change is a permanent, recognizable file explorer, not a second graph-shaped tree.
Use restrained file/folder glyphs, indentation, expanders, selected rows and visible extensions.
Separate the entry-style choice from the operation/lens and the altitude. Preserve the concept
view as an abstraction; add real class compartments, member selection and a full code-reading surface.
Named references remain VS/VS Code for file navigation, UML tools for concrete class/member
drill-down, Code Map for progressive scope, and Azure resource diagrams for layered service identity.

UI-T1 (expert visualization) and UI-T3 (model interpretation) apply. UI-T4 describes the future
native product but this artifact cannot prove native UIA or windowing. UI-T2 does not apply.
No real analysis model call is made. AI suggestions remain explicitly simulated interpretations.

## Acceptance: prove journeys, not the existence of tabs

1. Every tracked path appears in the data inventory; the explorer preserves real folder/file names.
2. Opening a curated file reveals its actual source body/excerpt, hash, original line numbers and
   coverage. Opening an unembedded file states that fact without substituted source.
3. File-first can reach member -> sequence -> message -> concrete source -> Back.
4. Visual-first can reach system -> feature -> concept map -> UML class -> member -> source.
5. Class member names/signatures and sequence anchors match the selected source revision.
6. The same selected file/type/member is represented in tree, breadcrumb, diagram and source.
7. Both prediction submission and settlement have distinct, source-backed sequences; branch choice
   is hypothetical/static, never reported as an observed execution.
8. Azure names the actual service families, their layers, roles and relationship semantics, and
   lets the user open the declaring Bicep. Absent examples are labelled absent, not fabricated.
9. The selected snapshot is consistent everywhere; scale disclosures separate complete file
   inventory, curated semantic coverage and visible diagram bounds.
10. Keyboard, focus, error/unavailable, empty/filter and wide/compact reading remain usable.

## Execution shape and stop

Read-only repository snapshot and source trace -> revised IA -> bounded data fixture + HTML ->
exact-journey browser proof -> independent review -> local commit. Architecture/infra reading is
independent of the main-line Predictions trace, so those run in parallel. Both join before the
mockup is finalized. Width <=2; no shared artifact writers. The reviewer is read-only.
Per-branch budgets are explicit; at most two review passes, draining a finite blocker list.

The previous run measured 2,162 seconds for its main UI-design episode. No claim is made that
this different, larger-corpus revision will be faster. The critical path is the source-to-journey
binding, not the number of diagram modes. Stop when the ten acceptance statements have evidence;
do not implement the underlying AI-DE feature.

## Correction class

**Semantic sample mistaken for physical exploration.** A graph's filtered node list cannot stand
in for a developer's file hierarchy; a collection of diagram tabs cannot stand in for a journey.
Sweep: both entry styles, all altitude transitions, source availability, declaration-vs-type IDs,
and message-to-source navigation. Derive every representation from the same snapshot and
selection state. Control: execute both starting paths through to a real source anchor and back,
and compare all representations' identities. Revision 1 could not satisfy this contract.
