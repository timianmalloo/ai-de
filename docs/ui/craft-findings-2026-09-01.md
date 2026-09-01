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

## 1. ~~BLOCKER — the search surface renders none of the bounds Core publishes~~

> ## ⚠ RETRACTED IN PART, 2026-09-01 — and the method that produced it is the real finding
>
> **`Evidence` and `MatchedOn` were wrong. They are rendered.** Caught by Core, verified here:
>
> - `WorkbenchShell.cs:1257` maps `m.Evidence ?? string.Empty` into `SearchResult`'s fourth
>   positional argument, which is **`Detail`**.
> - `SearchSurface.ResultRow` renders `hit.Detail` as a muted `Run` appended to the label
>   (`SearchSurface.cs:201-206`).
> - `m.MatchedOn` is consumed at `WorkbenchShell.cs:1249` to select `SearchResultKind.Member`,
>   which becomes a **group header**.
>
> So searching `addEventListener` already shows `has_member = + addEventListener()` beneath the
> row. The *field names* are absent from the surface; the *values* are on screen.
>
> **How I got it wrong: I grepped for field names and called it verification.** A name search is
> not a data-flow check. The value crosses the boundary renamed — `Evidence` becomes `Detail` —
> and a grep for `Evidence` in `SearchSurface.cs` cannot see it.
>
> That is the same tell this session had already named three times (register §8.2, §8.3b, §2 of
> this document) — asserting a gap without following it to the file that closes it. **This is the
> fourth, and it was committed in the same breath as naming the other three.** Core checked
> precisely *because* the pattern had been flagged, which is the only reason it was caught in an
> hour rather than after someone built on it.
>
> **What this does to §8.3's nine-item list: it makes it unverified.** That list was assembled by
> this same method — reading §4a's request table and grepping for names. At least one entry was
> wrong. **The list must be re-derived by data flow, or by the harness, before anyone builds
> against it.** Recorded rather than quietly re-listed, because the count "nine" has already been
> quoted in three places.
>
> **And it is the strongest argument yet for the behavioural harness.** Two agents, both careful,
> both reading the same code, reached opposite wrong conclusions about whether a value reaches the
> screen. That question has one reliable answer: **render the surface and look.** Which is exactly
> what the harness does, and exactly what neither of us did.

**What survives — and it was real.** `FilesSkipped`, `FilesSearched` and `Truncated` genuinely were
not surfaced. `ContentSearchResult(Matches, FilesSearched, FilesSkipped, Truncated, Bounds,
SourceRevision)` (`ContentSearch.cs:19`) is consumed at `WorkbenchShell.cs:1259-1263`, which takes
`c.NodeId`, `c.RelativePath`, `c.Line` and `c.Text` and **nothing else**. So "12 results" meant 12
out of the files that could be opened, with no way to know 40 were skipped — `DC-025` re-entering at
the render boundary, against Core's own comment that these are reported rather than silently dropped
*"because a search that quietly skipped half the corpus and said nothing would be a coverage claim
nobody could check."*

**Now wired by Core** as an interim: a trailing `SearchResult` of kind `Other` reading
*"Searched 412 file(s) — 40 file(s) not read"* with *"This result is a lower bound."* as its detail —
a row rather than a field, because the provider's contract is `Task<IReadOnlyList<SearchResult>>`
and has nowhere else to put it.

**The interim is the wrong shape, and Core says so first.** `DESIGN.md` §4a specifies
`count.lower-bound` as a **capped chip with a tooltip naming the cap** — not a row. The row makes the
number visible today; the chip is what closes it. **That is a Design change** (`SearchSurface.cs`,
§2), and it is finding #1's remaining substance.

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
| 1 | `FilesSkipped`/`Truncated` shown as a **row**, where `DESIGN.md` §4a specifies a `count.lower-bound` **capped chip + tooltip**. (`Evidence`/`MatchedOn` retracted — they render via `Detail`) | **Major** — a correctness rule by `DESIGN.md`'s own words, now visible but in the wrong affordance | Design | small — the tokens are already specified |
| 2 | The §8.3 behavioural harness | **Blocker**, promoted — two agents reading the same code reached opposite wrong conclusions about what reaches the screen. Only rendering answers it | Session 3 | medium |
| 2a | Re-derive §8.3's nine-item list **by data flow or by the harness** | **Major** — the list was built by the method that produced the retraction above, so its count is unverified | Session 3 | small once the harness exists |
| 1a | **LIVE INSTANCE of `count.lower-bound`: the Knowledge chip reads 257 against 878 declared** — and the accessible name repeats the same false claim, so a visual-only fix leaves screen-reader users wrong. Spec: [bounded-count-chip.md](bounded-count-chip.md) | **Blocker** — measured on the user'"'"'s real store after the 2026-09-01.8 re-index | Design | small — Core shipped `KnowledgeDeclared`, no transport change |
| 1b | **The evidence pane renders one of five fields.** `SurfaceContentFactory.EvidenceContent:74` sets `DisplayMemberPath = DisplayLabel` with no `ItemTemplate` — so `Evidence`, `NodeKind` and `Confidence` are all dropped, and `EvidenceRow.AccessibleName` (written to carry the match reason) is never read, because `AutomationProperties.SetName` is on the list, not its items | **Blocker** — same defect as `SearchSurface`'s, in a second pane, and adding the field to the record could not have fixed it | Design | small — an `ItemTemplate` |
| 2b | **`KnowledgeNodeView.HealthFindings` — zero consumption sites** | **Major**, and one of only two findings that survived every retraction | Design | small |
| 2c | **`PathResult.Truncated` — zero consumption sites** | **Major**, the other survivor. A truncated path that reads as a complete route | Design | small |
| 3 | Sequence-diagram message cap unreported | Major | Design | small |
| 4 | Two near-miss hexes in `CanvasPage.cs` | Major | Design | trivial |
| 5 | Six undeclared hexes in `CanvasPage.cs` | Major | Design | small — or declare them in `DESIGN.md` if they are real roles |
| 6 | The craft gate can only see one file | Minor, but structural | Session 3 | a WPF-aware check is not cheap; recorded, not proposed |

## What this pass did not cover

Archetype fit, information architecture, whether the hard states exist at all, and whether the copy
is true — none of which the detector can see, and none of which were assessed here. This is a floor.
