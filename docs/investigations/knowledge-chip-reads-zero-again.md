---
id: inv-knowledge-chip-reads-zero-again
title: "The Knowledge category chip reads 0 again — the App ignores the IsKnowledge flag"
type: investigation
status: draft
owner: "@timianmalloo"
phase: "facelift"
tags: [graph, canvas, knowledge, category, kind, isknowledge, regression, taxonomy]
links:
  - { to: inv-0004-graph-kind-taxonomy-and-knowledge, rel: refines }
  - { to: spec-knowledge-exploration, rel: relates-to }
  - { to: design-knowledge-explorer-mode, rel: relates-to }
review-by: 2026-12-01
summary: >-
  The Knowledge category chip reads 0 despite the workspace holding indexed knowledge (39 knowledge
  scopes, 4,471 headings in the boundary notes). Verified root cause: Core widened the graph node
  with an authoritative IsKnowledge flag to fix exactly this, but the App drops it at the CanvasNode
  boundary and still categorises nodes by spelling-matching the fine `kind` string — which cannot
  work across repositories. A secondary compounding cause is the 1,500-node degree cap, which can
  starve low-degree knowledge nodes out of the view the chip counts from. Recurrence of DC-042's
  named-but-uncontrolled residual risk (the canvas categoriser keyed on a producer's vocabulary).
---

# The Knowledge category chip reads 0 again

> **/investigate report — ends at human review. No implementation begun.**

## 1. Symptom

User opened the build and saw the **Knowledge** category chip read **0** ("again"), alongside the
graph status:

> "⚠ 1492 edge(s) omitted by the result bound · 27 analysis boundary note(s)"

and a long list of boundary notes — among them **`knowledge-headings-not-analysed (4,471 headings,
across 39 scopes)`**, **`knowledge-glossary-terms-not-analysed (3 documents)`**, and
**`knowledge-inline-code-not-resolved (26,970 spans, across 39 scopes)`**.

**The contradiction is the whole case:** the boundary notes prove the workspace *has* indexed
knowledge — **39 knowledge scopes**, thousands of headings — yet the chip that counts knowledge reads
**0**. A zero that is contradicted by the same status panel is a categorisation/count defect, not an
absence of knowledge.

## 2. Grounding (what it was supposed to do)

- **`spec-knowledge-exploration` (US-K1/US-K2)** and **`design-knowledge-explorer-mode`**: one graph
  over all artifacts; the category chips (Code/Data/Infra/Specs/Knowledge) let an operator see and
  filter the graph by artifact category.
- **`INV-0004`** (this note *refines* it): established that "Knowledge = 0" had multiple prior causes —
  the app graph was code-only (knowledge extractor unbuilt), and a coarse-vs-fine "kind" confusion
  (DC-041). Core subsequently **built knowledge discovery** and **widened the graph node with an
  authoritative `IsKnowledge` flag** so a chip could "ask the question directly instead of recognising
  spellings" (`GraphProjection.cs:22-33`).
- **Defect register:** **DC-041** (two kind fields), **DC-042** (a consumer keyed on a producer's
  vocabulary → misleading zero; its residual risk names *"the canvas's node kinds"* as an
  un-asserted instance of the same shape), and three prior "knowledge = 0" fixes (capped read;
  discovery not wired; double-count).

The spec wants the chip to reflect *how much knowledge is in the workspace*. The graph carries the
`IsKnowledge` signal to make that answerable without guessing at type spellings.

Graph traversal: `inv-knowledge-chip-reads-zero-again` → (refines) `inv-0004` → (relates-to)
`spec-knowledge-exploration`, `design-knowledge-explorer-mode`. No `tested-by` edge covers the canvas
*categoriser* — an orphan in the test graph, and the same gap DC-042's residual risk flagged.

## 3. Reproduction / characterization

Deterministic, by code path (a large polyglot workspace makes it visible; the user's is one — the
boundary notes carry bicep/azure, EF/SQL, python, typescript and C#). The chip count is computed
**client-side** from the received graph payload; the payload cannot contain the `IsKnowledge` signal
(§6), so the categoriser falls back to spelling the fine `kind`, and on any repo whose knowledge kinds
are not in the App's hardcoded list the count is exactly 0.

## 4. System map (the count path)

```
Core: GraphProjection.Compute
  └─ GraphNode { Id, Label, Kind, Degree, IsExternal, IsKnowledge }   ← authoritative knowledge flag
       │  (ordered: declared-first, then by Degree; capped at MaxNodes)
       ▼
Core: CanvasGraphViewModel.WholeGraphAsync
  └─ GraphAsync(GraphQuery(OverviewNodeCap=1500, IncludeExternal:false))
       └─ maps GraphNode → CanvasNode(Id, Label, Kind, IsRoot, Context, Count)   ← IsKnowledge DROPPED
       ▼
App: CanvasSurface.ShowGraph → PostWebMessageAsJson({ kind:"graph", graph })     ← payload has `kind`, not `isKnowledge`
       ▼
App: CanvasPage.js  record.cat = categoryOf(n.kind)   ← spelling-match; counts[cat]++
       ▼
"Knowledge 0"
```

Two independent facts fall out of the map: (a) the authoritative flag is **dropped at the CanvasNode
boundary**, so the client can only spell-match; and (b) the payload is the **degree-capped** 1,500-node
overview, which can exclude low-degree knowledge nodes before the client ever categorises them.

## 5. Hypotheses

| # | Hypothesis | Category |
|---|---|---|
| H-B | App categorises by spelling `kind` and ignores the authoritative `IsKnowledge` flag → knowledge miscategorised as code/specs | code / contract (primary) |
| H-A | The 1,500-node degree cap starves low-degree knowledge nodes out of the counted view | code / structural (compounding) |
| H-C | Knowledge discovery regressed and no knowledge nodes are produced | data / extractor |
| H-D | Coarse `node_kind` shown where fine `kind` meant (INV-0004 / DC-041) | code / display |

## 6. Evidence — verify each cause

### H-B — App ignores `IsKnowledge`, spells `kind` — **VERIFIED (necessary+sufficient, by code + Core's own measurement)**

1. Core provides the flag: `GraphNode(..., bool IsKnowledge = false)` (`GraphProjection.cs:36`),
   populated from `node_class = knowledge` assertions (`GraphProjection.cs:198, 269`). Its doc comment
   (`:22-33`) records the exact failure this exists to prevent: *"the Knowledge chip read 0 on a
   repository holding 2,343 knowledge nodes … the knowledge kinds are `spec` and
   `knowledge-epl-fan-platform` … A chip matching a fixed list of type names cannot work across
   repositories, and widening the list only moves the problem to the next repository."*
2. The App **drops** the flag: `CanvasNode(string Id, string Label, string Kind, bool IsRoot,
   string? Context, int Count)` (`CanvasGraphViewModel.cs:20`) has **no `IsKnowledge`**, and
   `WholeGraphAsync` maps `new CanvasNode(n.Id, n.Label, n.Kind, …)` (`:156`) — the flag is lost here.
3. The payload therefore carries only `kind` (`CanvasSurface.cs:98-99`).
4. The client spells it: `record.cat = categoryOf(n.kind)` (`CanvasPage.cs:661`), where `categoryOf`
   (`:156-165`) matches a **fixed list** — `knowledge, doc, adr, design, note, decision-note, markdown,
   html, diagram, proof`. Knowledge frontmatter types **not** in the list — `spec`→'specs',
   `investigation`, `glossary`, `lesson`, `backlog`, `architecture`, or any repo-invented kind — fall
   through to `'code'` or `'specs'`. Chip counts come from these cats (`:691`).
- **Necessary:** if the client categorised by `IsKnowledge`, every knowledge node in the payload would
  count as knowledge. **Sufficient:** because it spell-matches, a repo whose knowledge kinds are not in
  the list reads 0 — Core *measured* exactly this (item 1). The user's boundary notes (39 knowledge
  scopes) confirm knowledge exists; the chip still reads 0.
- **Why it survives:** the fix was cross-session and only half-landed — **Core widened the contract
  (`IsKnowledge`), the App never consumed it.** No test compares the App categoriser's vocabulary
  against the producer's signal, so the half-wiring is invisible from either side (the producer emits
  the flag correctly; the client spells correctly against its own list).

### H-A — Degree cap starves low-degree knowledge — **VERIFIED (structural, by code); its share on this repo INFERRED**

`WholeGraphAsync` requests `OverviewNodeCap = 1_500` (`CanvasGraphViewModel.cs:99, 127`);
`GraphProjection` orders **declared-first, then by degree** (`:249-253`) and takes the top
`MaxNodes` (`:255`). Knowledge documents are **low-degree** (a doc has a handful of typed links; a C#
type participates in hundreds of call/inherit edges). On a workspace with **> 1,500 declared code
nodes**, low-degree knowledge is ranked below the cap and omitted — so the chip counts "knowledge in
the current 1,500-node view", which structurally under-represents it. The "1,492 edges omitted" the
user sees is this cap firing.
- **Verified:** the ordering and cap are as described.
- **Inferred (spike):** whether, on the user's specific repo, knowledge nodes are *entirely* capped out
  (H-A dominant) or *present but miscategorised* (H-B dominant). Distinguish by instrumenting the
  payload: count `IsKnowledge==true` nodes actually sent. If > 0 while the chip is 0 → pure H-B; if 0 →
  H-A also fires. Both fixes are needed regardless.
- **Deeper structural cause it exposes:** the chip conflates *"in this bounded view"* with *"in the
  workspace."* The user asks "how much knowledge is here"; the chip answers "how many knowledge nodes
  are in the top-1,500-by-degree." Those are different claims and only sourcing the count from a
  workspace **aggregate** makes the chip honest.

### H-C — Knowledge discovery regressed — **RULED OUT (by evidence)**

The boundary notes report **39 knowledge scopes**, 4,471 headings, 3 glossary documents, 26,970 inline
code spans — the knowledge extractor is running and producing scopes. Discovery (the DC-042 2026-08-30
fix) is intact. The nodes exist; the *chip* is wrong.

### H-D — Coarse-vs-fine kind (DC-041) — **RULED OUT for the count**

DC-041 was about the *reader* displaying `node_kind` where `has_type` was meant, fixed in
`NodeReaderView`. The *chip count* path uses `categoryOf(kind)` on the graph payload, a different code
path; the fine `kind` is what it receives. Not the cause of the zero count (though it is the same
root family — a kind taxonomy consumed by spelling).

## 7. Disconfirmation (adversary pass)

- *"It's just the degree cap (H-A) — one bug."* Defeated: Core measured the chip reading 0 on a repo
  where the knowledge nodes were present but named `spec`/`knowledge-epl-fan-platform` — a
  categorisation failure independent of any cap. H-B holds even at zero omissions.
- *"It's just the spelling list (H-B) — widen the list."* Defeated by the producer's own comment: any
  fixed list "only moves the problem to the next repository." The correct fix is to consume the
  authoritative `IsKnowledge` flag, not to grow the list. Widening the list would be treating the
  symptom.
- *"Knowledge isn't indexed (H-C)."* Defeated by the 39 knowledge scopes in the user's own boundary
  notes.
- *Can the evidence pick H-A vs H-B as the dominant one on THIS repo?* Not from code alone — hence the
  Phase-0 payload instrumentation. The report does not guess; it labels H-B Verified-and-primary
  (necessary+sufficient by construction) and H-A Verified-structural / share-Inferred.

## 8. Verified root cause

**The App categorises graph nodes for the Knowledge chip by spelling the fine `kind` string, ignoring
the authoritative `IsKnowledge` flag Core added to `GraphNode` to make this answerable across
repositories.** The flag is dropped at the `CanvasNode` boundary (`CanvasGraphViewModel.cs:20/156`) and
never reaches the client, which falls back to a fixed spelling list (`CanvasPage.cs:156-165, 661`) that
cannot match a workspace whose knowledge kinds are `spec`/invented/`investigation`/`glossary`/`lesson`.
It is a **regression against a landed cross-session contract** — the producer's half shipped, the
consumer's half did not. The **1,500-node degree cap** compounds it by counting from a bounded view
that under-represents low-degree knowledge, and the chip's design conflates *view count* with
*workspace count*.

## 9. Specific fixes (for review — not yet implemented)

1. **Consume `IsKnowledge` end-to-end (primary, App/Design).** Add `IsKnowledge` (and carry
   `IsExternal`) to `CanvasNode`; thread it through `WholeGraphAsync`/`OverviewAsync`/group loads and
   the WebView payload; in `CanvasPage.categoryOf`, decide `knowledge` by the flag (and `external`/BCL
   by `IsExternal`), keeping the spelling list only as a last-resort tiebreaker for the code
   sub-kinds (data/infra/specs). Rollback: revert to spelling. Regression test: a payload node with
   `IsKnowledge==true` and `kind=="spec"` categorises as `knowledge`, not `specs`/`code` (fails on
   today's code).
2. **Source chip counts from a workspace aggregate (structural, App + Core).** Compute the category
   counts from `OverviewAsync`/a dedicated category-count query over the *whole* workspace, not from
   the degree-capped payload — so the Knowledge chip reflects "knowledge in the workspace", and the
   omitted-by-cap note explains the *drawn* graph, not the *count*. Rollback: fall back to payload
   counts. Regression test: on a fixture with N knowledge nodes and a cap below N, the chip reads N.
3. **Class-prevention control (App).** A test that asserts the canvas categoriser is driven by
   producer-declared signals (`IsKnowledge`/`IsExternal`/the `has_type` families), and that every
   category the chips advertise has a producer signal the App consumes — the canvas analogue of
   `WorkspaceExtractors.RoutedKinds` (DC-042's control), extended to the exact residual risk DC-042
   named.

## 10. Generalization — the failure class

This is a **recurrence of DC-042** — *"a consumer keyed on a producer's vocabulary rather than on the
producer's authoritative signal, so a real-repository count reads a legitimate-looking zero"* — and
specifically the residual risk **DC-042 itself named and left uncontrolled**: *"the same shape exists
wherever a router matches on strings someone else emits … **the canvas's node kinds** are all keyed
this way; only extraction routing is asserted so far."* The control (RoutedKinds + KnowledgeExtractor
tests) was scoped to *extraction routing* and never reached the *canvas categoriser*, so the class
recurred exactly where the register predicted. **A recurrence means the control was too narrow, not
that anyone was careless.**

**Sweep for siblings (confirmed/ruled out):**
- `CanvasPage.categoryOf` (the chip categoriser) — **confirmed** (this bug).
- `NodeReaderView` type display — **already fixed** for display (INV-0004/DC-041), but still spells
  `kind` for its *category colour* if any; **candidate** — verify it consumes `IsKnowledge`/`has_type`.
- Join projection predicates and IPC operation names (DC-042's other named residual siblings) — **out
  of scope here**, still un-asserted; noted for the register.

**Broader solution (reusable rule):** a UI category/count must be driven by a **producer-declared
signal** carried on the node (a boolean/enum the extractor sets), never by the consumer spelling the
producer's free-form type strings; and a **count the user reads as "how much exists"** must come from
a workspace aggregate, not from a capped view. The class-prevention item is the canvas-categoriser
assertion (fix 3), mirroring RoutedKinds.

## 11. `simplify:`/`assume:` marker harvest

The graph/canvas subsystem (`CanvasPage.cs`, `CanvasSurface.cs`, `GraphProjection.cs`,
`CanvasGraphViewModel.cs`) carries **no** `simplify:`/`assume:` markers. The relevant bounded shortcut
— "categorise by spelling `kind`" — was never marked; had it been (`simplify: fixed kind list; ceiling
= a repo whose knowledge kinds are not in the list; upgrade trigger = consume IsKnowledge`), its
ceiling was reached the moment Core shipped `IsKnowledge`. Finding: the App categoriser predates the
flag and was never revisited when the contract widened.

## 12. Phased repair plan (for approval — nothing built yet)

| Phase | Scope (code + tests) | Eliminates | Validation | Depends on |
|---|---|---|---|---|
| **0 — Instrument** | Emit a count of `IsKnowledge==true` nodes in the sent payload + the pre-cap workspace knowledge total; surface in a trace/diagnostic | The H-A-vs-H-B ambiguity on the user's repo | Trace shows whether knowledge nodes reach the payload | — |
| **1 — Consume IsKnowledge (primary)** | Carry `IsKnowledge`/`IsExternal` on `CanvasNode` → payload → `categoryOf` uses the flag; test `IsKnowledge+kind:"spec"` → `knowledge` | H-B | New test fails on today's code, passes after; chip non-zero on a knowledge-bearing fixture | Phase 0 |
| **2 — Class-prevention control** | Assert the canvas categoriser is driven by producer signals; every advertised category has a consumed signal (canvas analogue of RoutedKinds) | The class DC-042 at the canvas | Test fails if a chip category has no producer signal | Phase 1 |
| **3 — Aggregate chip counts (structural)** | Category counts from `OverviewAsync`/a category-count query over the whole workspace; cap note explains the drawn graph only | H-A / the view-vs-workspace conflation | Fixture: N knowledge nodes, cap < N → chip reads N | **Core** (aggregate count query) |
| **4 — Sweep siblings** | Verify `NodeReaderView` category path consumes the flag; register join/IPC siblings from DC-042's residual risk | Latent same-shape siblings | Each sibling confirmed/ruled out with a test | Phase 2 |

## 13. Residual risk / what would change the diagnosis

- If Phase-0 instrumentation shows **zero** `IsKnowledge` nodes in the payload, H-A is dominant on this
  repo and Phase 1 alone will not raise the chip — Phase 3 (aggregate counts) becomes the load-bearing
  fix. The plan orders instrumentation first for exactly this reason.
- If Core's `node_class = knowledge` assertions are themselves missing on this workspace (an extractor
  regression), `IsKnowledge` would be false and both fixes would still read 0 — Phase 0 also checks the
  pre-cap workspace knowledge total, which distinguishes "extractor produced none" from "chip dropped
  them". That would re-open H-C as a Core defect.

## 14. Gate record

`GATE investigate · 2026-09-01 · SRE + Test-Architect (adversary) · exit criteria: primary root cause
verified necessary+sufficient by code and the producer's own measured evidence; competing causes ruled
out with evidence (H-C by the user's boundary notes, H-D by code-path); recurrence mapped to DC-042's
named residual risk; phased plan each item code+tests · verdict: PASS-WITH-CONDITIONS (H-A's share on
this specific repo labeled Inferred pending Phase-0 instrumentation; not asserted as the primary) ·
vetoes: none unresolved — Test-Architect: every fix carries a failing-first test.`

**STOP — human review.** Recommended order **0 → 1 → 2** (App-owned, high-leverage, no Core
dependency), then **3** with the Core session (aggregate count query), then **4**. Approve the phases
before any `/implement`.
