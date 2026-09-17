---
id: inv-0014-the-graph-stage-is-capped-and-typescript-provenance-is-a-scope-id
title: "Two Explore defects: the graph stage is capped at 440 CSS px inside a full-height pane, and the TypeScript/Python extractors write a scope id where the artifact path belongs — which also makes content search return silently wrong \"no matches\""
type: investigation
status: accepted
owner: "@timianmalloo"
phase: "conductor-watch-0915"
tags: [explore, graph-canvas, provenance, typescript, python, content-search, telemetry, clean-machine]
links:
  - { to: note-addendum-c-council-rulings, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
  - { to: inv-0013-the-sheet-asks-the-config-not-the-machine, rel: relates-to }
review-by: ""
summary: >-
  From the operator's clean-machine build. (1) The Explore graph fills 21.8% of its pane's height
  because CanvasPage's #stage carries a fixed height:440px inside a full-height WPF pane — measured
  1624x437 in a 1661x2002 pane, with fit()'s own arithmetic predicting the drawn disc to 1px.
  (2) "View source" returns a shortfall for every TypeScript node because TypeScriptExtractor and
  PythonExtractor pass request.ScopeId into Provenance.ArtifactPathId; the same field feeds content
  search, so every TS and Python file in every workspace is silently skipped and a search returns
  "no matches" rather than "I could not open these files". The view renders the projection
  faithfully; the projection is wrong.
---

# Two Explore defects, neither where the symptom pointed

*Investigated 2026-09-17 by the SRE & Systems Diagnostician lens under `/investigate`, on the
operator's clean-machine screenshots of 2026-09-16. Read-only: nothing implemented, no test run —
the suites that would close the remaining questions need a shown window (§6).*

## 1. The graph canvas never fills the dock — **Verified**

**Measured from the screenshot** (3831×2104, 100% scaling): the graph pane is **1661 × 2002** device
px and the splitter runs its full height, so the *pane* is full-height. The dark stage inside it is
**1624 × 437** — full width, **21.8% of the height**. The drawn graph is a **389 × 354** disc: 3.6%
of the pane's area, about **72 px² per drawn node**, which is why it reads as a hairball.

**Root cause — `src/AiDe.App/Workbench/CanvasPage.cs:65`**

```css
#stage { position: relative; height: 440px; margin-top: 10px; … }
```

A fixed height on the one region whose job is to consume the pane. **Necessary and sufficient**, and
the arithmetic is the proof rather than the narrative: `fit()` scales to the stage minus a 50 px
margin, so the tallest drawable extent is 437 − 100 = 337 px of node centres, plus ~18 px of node
gives 355 px. **Measured: 354 px.** The content is correctly filling a container that is wrongly
small.

Four rival causes were killed with evidence: the WPF host *does* stretch (`ExplorerSurface.cs:178-195`
adds no `RowDefinitions`, so one implicit `1*` row, and the splitter is full-height in the shot);
there is no `ScrollViewer`/`Height`/`MaxHeight`/`SizeToContent` anywhere in `CanvasSurface.cs:74-100`;
the content is not drawn at a fixed scale (the 355/354 match refutes it); and the re-frame path exists
and works — `CanvasPage.cs:584-592` installs a debounced `ResizeObserver` that calls `fit(); place()`
on a box that never changes height.

**Fix (one CSS rule):** body becomes a flex column and `#stage { flex: 1 1 auto; min-height: 240px }`.
Predicted gain with no layout change: the stage goes 440 → ≈1760 px, `fit()`'s limiting term moves
from height to width, **≈4.5× linear, ≈20× area**. *Do this before any node-density or LOD work — the
density complaint may largely be this defect.*

**Why no control caught it:** the value lives in **CSS inside a C# string literal**, invisible to the
XAML extent tests (`CodingsLeftExtentTests.cs` and friends) and to `ui-craft-gate.py`/`design-lint.py`
alike — a blind spot between two gates. `440` appears nowhere else in `src/` or `tests/`.

## 2. "View source" shows metadata — the projection, not the view — **Verified**

The reader's bottom line reads *"the source for this node could not be located
(`typescript:src/frontend`)"*. That parenthesised value is a **scope id**, not a path: no file is
named that.

**Root cause — `src/AiDe.Core/Extraction/TypeScriptExtractor.cs:781-785`** (and
`PythonExtractor.cs:404`, identical shape): the `Fact` helper passes **`request.ScopeId`** into
`Provenance`'s first positional parameter, which is **`ArtifactPathId`**
(`src/AiDe.Core/Facts/EvidenceAssertion.cs:25-30`), and `null` for `SourceLocation`.

The chain then behaves exactly as designed and produces a wrong answer:
`StoreReader.DeclaringAssertion:163` filters `artifact_path_id <> ''` — a guard against *empty*, not
against *non-path* — so the row is returned; `ProjectionService.cs:1302` combines root + scope
location + `typescript:src/frontend`; `File.Exists` fails (on Windows the embedded colon is ADS
syntax, so it either misses or throws into the catch at `:1386`); `:1305-1309` returns
`NodeContentKind.None` with the operator's exact sentence.

**The view is exonerated explicitly.** `NodeReaderView.cs:101-131` renders the projection's own
`Shortfall` verbatim, which is what Ruling 93 specifies; the metadata panel that remains is the
*selection* rendering and is supposed to stay. ADR-0025's renderer is never reached — `RenderKind` is
`None` before any renderer choice. **A correct rendering of a wrong fact.**

### The part the operator did not see, and it is worse

`ProjectionService.SearchContent:1190-1265` resolves files through the **same** call at `:1212`, fed
by the same `artifact_path_id`. So **every TypeScript and Python file in every workspace is silently
`skipped++`** (`:1216`). A content search over a React codebase returns *"no matches"* instead of
*"I could not open 1,100 files"* — and `ContentSearchResult` carries a bare skip count with **no
reason code**, so the number cannot be read as a defect signal. Any decision taken on a content
search over a JS/TS repo is retrospectively suspect, for an unknown span of time.

### The sweep: two of nine producers are wrong, and they are the two with no test

Every `new Provenance(` in `src/` was read. Seven producers pass a real relative path (`CSharpExtractor.cs:493`
is the reference implementation, path + `line:col`); three scope-summary rows are correct as written;
**`TypeScriptExtractor.cs:785` and `PythonExtractor.cs:404` pass the scope id.** Provenance is asserted
in tests only for C# (`CSharpExtractorTests.cs:88`, `CallEdgeTests.cs:138`) — **the two untested
extractors are the two defective ones.** The gap and the defect are the same shape.

## 3. A third finding, Inferred: nested scopes re-extract the same subtree

Every edge in the screenshot appears **exactly four times**. Mechanism, all read in code: scopes are
created per directory holding TS/Python directly (`CSharpScopeDiscovery.cs:103-114`); the extractor
walks **recursively** from its root (`TypeScriptExtractor.cs:320, 940-973`); module ids are
repo-relative (`ModuleNaming.cs:29-40`), so every ancestor scope emits the same subject; and `ScopeId`
is **inside** the identity hash (`EvidenceAssertion.cs:50-51`), so four ancestor scopes produce four
distinct assertion ids for one identical fact, which nothing deduplicates.

Cost: extraction work, store rows, projection bytes and rendered edges all scale with **someone else's
directory nesting depth**, uncapped — and the duplicates consume a result bound that is evicting real
edges, as the screenshot's own banner admits: *"8,280 edge(s) omitted by the result bound"*.

**Confirm before repairing** (do not optimise on the hunch):
`SELECT scope_id, COUNT(*) FROM evidence_assertion_fact WHERE subject = '…/CpuVersionZoneTable' GROUP BY scope_id;`
— four rows with four nested `typescript:` scope ids confirms; one row refutes.

## 4. Marker harvest — the finding is what the markers do *not* say

Four `simplify:` markers sit in this subsystem and **all four are on the two defective extractors**
(`TypeScriptExtractor.cs:50, 649`; `PythonExtractor.cs:27, 325`). Each declares a ceiling on *what is
recognised* — line-oriented rather than a grammar — and **none declares that the shortcut also
dropped provenance from file-grain to scope-grain.** The defect is an *undeclared cost of a declared
shortcut*. `CanvasPage.cs`, `ProjectionService.cs` and `StoreReader.cs` carry no markers at all, so
`440px` was never marked provisional either. Both defects live in the **unmarked** space — which is
the argument for the two mechanical controls below rather than for better prose.

## 5. Phased repair (not started; the Owner rules the phases)

| Phase | Scope (code + tests) | Failure mode eliminated | Depends on |
|---|---|---|---|
| **P0 — the red** | *Tests only.* A parameterised provenance test over **every** extractor: `File.Exists(Path.Combine(scopeDir, Provenance.ArtifactPathId))`. A `CanvasPage.Html` contract test forbidding an absolute `px` height on `#stage` and requiring a flex remainder. | none yet — this is the red both classes need | — |
| **P1 — the stage** | `CanvasPage.cs:33,65` — body flex column, `#stage { flex:1 1 auto; min-height:240px }` | graph uses ~100% of the pane, not 21.8%; ≈20× drawn area | P0 |
| **P2 — provenance** | thread the scope-relative path already computed at `TypeScriptExtractor.cs:770-778` into `Fact`; same for Python; populate `SourceLocation` where a line is known | View source **and** the silent content-search skip | P0 |
| **P3 — the store** | `Provenance` is outside `AssertionId`, so P2 causes no id churn **and no self-heal**: existing stores keep `typescript:…` until re-extracted. Forced re-extraction / generation bump per scope, before/after counts recorded. **Data & Persistence Architect owns this.** | the fix silently not applying — the operator retests and sees no change | P2 |
| **P4 — telemetry** | `NodeContent` opens a span with **no outcome tag**; add `content.outcome` (located / no-declaration / unresolvable / unreadable / not-rendered) and a stable error code; give `SearchContent`'s `skipped` a reason breakdown. Keep the *reply* opaque (`:1339-1348` deliberately does not describe the filesystem) — emit on the span only. | "wrong for two whole languages and nobody knew" — a screenshot is not a monitoring strategy | with P2 |
| **P5 — duplication** | confirm the 4× with the query above, then scope at the outermost directory or dedupe at write; assert no `(subject,predicate,object)` under two scopes of one kind | 4× rows and edges; real edges evicted by the bound | confirm first |

## 6. Residual risk

1. **P3 is where this most plausibly fails in the field** — a correct fix that needs a re-extraction,
   with no migration and no store version stamp, reads to the operator as "you didn't fix it".
2. **The canvas has no stated performance budget** — not for extraction, not for frame time at N
   nodes. P1 makes the stage ~20× larger in area and nobody has measured the canvas at any size.
   State the budget as an acceptance criterion on P1 and measure it in the same desktop slot.
3. **`min-height: 240px` is a guess** chosen to pair with `ExplorerSurface.cs:198`'s stacked
   `MinHeight = 200`; measure it rather than shipping a second unmarked constant.
4. **Finding 3 is Inferred** — if the 4× has another cause, P5 is aimed at the wrong thing.
5. **Content search has been confidently wrong for TS/Python for an unknown span**; nothing records
   how long.
6. The observed 44/56 pane split against `ExplorerSurface.cs:183-185`'s 0.58/0.42 default is
   unexplained; benign on this evidence (it is a width, the defect is vertical).

## 7. What needs a desktop slot (≈15 min, one launch)

Nothing here was run — both suites that would settle it show windows (`tests/AiDe.App.Tests` calls
`ComposedCoding.Show(1440,900)`; `AiDe.App.CanvasProbe` calls `window.Show()`). Scheduled work:
measure `stage.clientHeight` against the pane before and after P1 at 3840×2160, at the 1440×900
startup size, and in the stacked layout below 760 px; frame time at 1,500 nodes in the enlarged
stage; validate `min-height`. The View-source end-to-end is better written as a **headless**
`NodeContentTests` case over a fixture TS tree — cheaper, deterministic, and the regression test the
class needs anyway.
