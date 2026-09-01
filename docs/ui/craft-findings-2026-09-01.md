---
id: ui-craft-findings-2026-09-01
title: "Craft pass — the surfaces on main, 2026-09-01"
type: investigation
status: draft
owner: "@timianmalloo"
phase: "3"
tags: [ui, craft-gate, review, bounded-reads, token-discipline, session-3]
links:
  - { to: session-contracts, rel: relates-to }
  - { to: knowledge-hub, rel: relates-to }
review-by: 2026-12-01
review-suggested: []
summary: >-
  First craft pass over the surfaces already on main, run by Session 3. Measured with
  ui-craft-gate.py before diagnosing. Headline: the search surface renders none of the five bound
  fields Core publishes, and DESIGN.md already specifies the affordance for exactly this — so the
  §8.3 gap is implementation, not specification, and Session 3's proposal to design one is withdrawn.
  Two surfaces already implement the pattern correctly, which makes this drift rather than a
  capability gap. Plus eight Major token findings in the canvas page, including two near-miss hexes.
---

# Craft pass — the surfaces on main

**Session 3 · 2026-09-01 · read-only.** No file in `src/` was edited. Everything below is a
finding for the owning session under [`session-contracts.md`](../collaboration/session-contracts.md)
§2 — `CanvasPage.cs` and `SearchSurface.cs` are Design's, `CanvasGraphViewModel.cs` is Core's.

**Measured before diagnosed** (DX23). The numbers come from `ui-craft-gate.py`, not from reading.

---

## 0. The measurement

```
python docs/ai-forward-pack/scripts/ui-craft-gate.py src/AiDe.App/Workbench/CanvasPage.cs

  measurement (DX23):
    Major    8
    Token discipline               8
```

`CanvasPage.cs` is the only file the detector can read: it carries the embedded HTML/CSS/JS for the
canvas. **The rest of the app is WPF built in code, which the detector cannot see** — so a clean run
over `src/AiDe.App/` would be an empty corpus reporting as clean, which is the failure `coord-core`'s
own R4 control exists to prevent. **This measurement covers one file. It is a floor, not a verdict**
(CD: a clean gate cannot see archetype fit, IA, whether the hard states exist, or whether the copy is
true).

Findings 1–3 below were found by reading, not by the gate, and are the more serious ones.

---

## 1. BLOCKER — the search surface renders none of the bounds Core publishes

**Verified:** `SearchSurface.cs` (230 lines) and `SearchModel.cs` contain **zero** references to
`Evidence`, `MatchedOn`, `FilesSkipped`, `Truncated` or `FilesSearched`.

Core publishes all five:

- `FindMatch(NodeId, NodeKind, DisplayLabel, Authorship, MatchedOn, Evidence?)` —
  `src/AiDe.Core/Projections/ProjectionService.cs:92`
- `ContentSearchResult(Matches, FilesSearched, FilesSkipped, Truncated, Bounds, SourceRevision)` —
  `src/AiDe.Core/Projections/ContentSearch.cs:19`

Two user-visible consequences, both live on `main` today:

| What the user sees | What is true |
|---|---|
| a class called `Element` returned for `addEventListener` | correct — it matched an attribute value — and it reads as a bug, because `MatchedOn`/`Evidence` are on the record precisely to say why, and are dropped |
| "12 results" | 12 results **out of the files that could be opened**. `FilesSkipped` counts the ones too large or unreadable, and is dropped |

The second is `DC-025` re-entering at the render boundary. Core's own comment on the field says these
are reported rather than silently dropped because *"a search that quietly skipped half the corpus and
said nothing would be a coverage claim nobody could check."* The surface then makes exactly that
claim.

**Owner:** Design (`SearchSurface.cs`, §2). **Not fixed here.**

---

## 2. The affordance is already specified — my own §8.3 proposal is withdrawn

`DESIGN.md` §"§4a rendering tokens — bounded reads & emphasis" **already specifies this**, and has
for some time:

| Token | Value |
|---|---|
| `count.lower-bound` | `≥ N` in mono + `capped` chip + tooltip naming the cap |
| `count.exact` | `N` in mono, `{colors.text}` |
| `state.not-declared` | first-run empty state: glyph + one line + first-action button |
| `emphasis.dominant`, `emphasis.dominant-bar` | promotes the dominant class out of the grey suffix |

And it already names the class, in stronger language than §8.3 did:

> **The bounded-read rule is a correctness rule (`EvidenceRead.Shortfall`):** a count that is a lower
> bound and one that is exact must be **distinguishable at a glance**. `20,000 results` and
> `≥ 20,000 results (capped)` are different claims; rendering them identically is the surface
> inventing the completeness the read could not establish — **the same failure class as provenance
> laundering.**

**So §8.3's first proposal — "one shared disclosure affordance, specified once" — is withdrawn. It
exists.** I proposed designing a thing that was already designed, because I read the Core contracts
and the register and had not opened `DESIGN.md`'s §4a section. That is the same failure this session
has now made three times: asserting a gap without opening the file that would have closed it.

**This makes the case for the control stronger, not weaker.** The spec exists, is agreed, is written
in correctness language, and the newest surface does not follow it. A specification without a control
is a memoir too (CI6). §8.3's proposal #2 — the behavioural harness — is the part that was load-bearing
all along.

---

## 3. It is drift, not a capability gap — two surfaces already do it right

The same codebase already implements the pattern in three places:

| Surface | Bound rendered | Where |
|---|---|---|
| `ContextMapSurface` | `IsDeclared == false` → an empty state | `ContextMapSurface.cs:77` |
| `ContextMapSurface` | `DominantTarget` / `DominantCount` | `ContextMapSurface.cs:211,223` |
| `CodeViewerView` | `Shortfall` | `CodeViewerView.cs:96-98` |

So this is not "the team does not know how". The pattern is understood, specified and shipped — and
the two newest surfaces (`SearchSurface`, and the sequence diagram's message cap) do not carry it.
**Drift between what a design system says and what the newest code does is precisely what a gate is
for**, and precisely what prose cannot hold.

---

## 4. MAJOR ×8 — token discipline in `CanvasPage.cs`, including two near-miss hexes

All eight are off-token colours (`U3`/`U20`). Two of them are the interesting kind:

| In the code | Nearest declared token | Line |
|---|---|---|
| `#B08AD0` (`specs` category) | `#B08CD9` | `CanvasPage.cs:146` |
| `#E0955F` (`infra` category) | no declared match | `CanvasPage.cs:145` |

`#B08AD0` against `#B08CD9` is not a different colour choice — it is **the same colour re-typed from
memory instead of referenced**. Six others are undeclared entirely: `#21303f`, `#CDE3FF`, `#B99A5E`,
`#47566B`, `#63748C`.

`#5B9DD9` *is* used correctly (lines 39, 75) — the accent is on-token. So the file is not
untokenised; it is tokenised **and drifting**, which is the harder state to see and the one that
compounds.

**Owner:** Design (`CanvasPage.cs`, §2).

---

## Ranked plan

| # | Finding | Severity | Owner | Cost |
|---|---|---|---|---|
| 1 | `SearchSurface` renders no bound: `Evidence`, `MatchedOn`, `FilesSkipped`, `Truncated` | **Blocker** — a correctness rule by `DESIGN.md`'s own words | Design | small — the tokens are already specified |
| 2 | The §8.3 behavioural harness | **Major** — the only thing that stops #1 recurring | Session 3, on Design's yes | medium |
| 3 | Sequence-diagram message cap unreported | Major | Design | small |
| 4 | Two near-miss hexes in `CanvasPage.cs` | Major | Design | trivial |
| 5 | Six undeclared hexes in `CanvasPage.cs` | Major | Design | small — or declare them in `DESIGN.md` if they are real roles |
| 6 | The craft gate can only see one file | Minor, but structural | Session 3 | a WPF-aware check is not cheap; recorded, not proposed |

## What this pass did not cover

Archetype fit, information architecture, whether the hard states exist at all, and whether the copy
is true — none of which the detector can see, and none of which were assessed here. This is a floor.
