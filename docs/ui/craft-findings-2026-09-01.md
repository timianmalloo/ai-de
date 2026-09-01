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
| 1b | **The evidence pane renders one of five fields.** `SurfaceContentFactory.EvidenceContent:74` sets `DisplayMemberPath = DisplayLabel` with no `ItemTemplate` — so `Evidence`, `NodeKind` and `Confidence` are all dropped, and `EvidenceRow.AccessibleName` (written to carry the match reason) is never read, because `AutomationProperties.SetName` is on the list, not its items | **Blocker** — same defect as `SearchSurface`'s, in a second pane, and adding the field to the record could not have fixed it | ~~Design~~ **Core** — `SurfaceContentFactory.cs` is Core's under §2; I assigned by symptom shape. **FIXED by Core at `fcd89f2`** with an `ItemTemplate` whose visible text and accessible name are composed from one `ListLine`, so the two paths cannot drift | done |
| 2b | **`KnowledgeNodeView.HealthFindings` — zero consumption sites** | **Major**, and one of only two findings that survived every retraction | Design | small |
| 2c | **`PathResult.Truncated` — zero consumption sites** | **Major**, the other survivor. A truncated path that reads as a complete route | Design | small |
| 3 | Sequence-diagram message cap unreported | Major | Design | small |
| 4 | Two near-miss hexes in `CanvasPage.cs` | Major | Design | trivial |
| 5 | Six undeclared hexes in `CanvasPage.cs` | Major | Design | small — or declare them in `DESIGN.md` if they are real roles |
| 6 | The craft gate can only see one file | Minor, but structural | Session 3 | a WPF-aware check is not cheap; recorded, not proposed |

## What this pass did not cover

Archetype fit, information architecture, whether the hard states exist at all, and whether the copy
is true — none of which the detector can see, and none of which were assessed here. This is a floor.

---

## 5. MEASURED — "new code viewer produced no tab" is not a model defect

Core handed this over saying the discriminator is *what the status line said*, and that neither of
its controls could see it. Driven through the **real `WorkbenchShell` and the real
`WorkbenchController`** by a throwaway probe (scratchpad, not committed), because six inferences
about runtime behaviour were wrong today:

```
DocumentPlacementPolicy.Decide  ->  tabInto=-  splitBeside=zone-center      (NOT null)
Execute("workbench.newCodeViewer")  ->  True
status line  ->  "Code viewer opened."
layout after ->  [zone-center] active=5 -> codeviewer:Source
                 canvas:Graph, view:Domain, sessions:Sessions, board:Board,
                 leaderboard:Leaderboard, codeviewer:Source
```

**The status line says success, not failure.** The *"There is no pane to open a code viewer in"*
branch requires `Decide` to return null, which happens only when `layout.AllStacks()` is empty —
and `AssertInvariant` forbids empty stacks. **That message is near-unreachable and is not this
symptom.**

**The model is entirely correct:** the surface exists, is in a stack, and is the **active** tab
(`ActiveIndex = 5`). So — stated as the conditional it is — *if a user saw no tab, the fault is at
or below the dock adapter*, in the layout→AvalonDock reconcile, not in creation, placement or
activation. `WorkbenchAdapter.cs` is Core's under §2.

### 5b. FIXED and verified — and the fix has a side effect that defeats its own intent

Core fixed §5a at `b5ca354` (`ZoneBackedLayoutService.Move` read the drop's target node and ignored
its `Kind` for everything except `Float`, so `SplitRight` against the Center resolved to "move into
the Center"). Re-run against the fix, same probe:

```
newCodeViewer   -> [zone-right] active=0 -> codeviewer:Source      (a NEW zone; graph stays put)
newClassDiagram -> [zone-right] active=1 -> classdiagram           (rule 2, tabs beside it)
```

Both correct. The split is honoured, the document region is created beside the graph rather than on
top of it, and a second document joins the first. My §5a finding is closed.

**But the same run shows a side effect nobody has looked at.** Immediately after `newCodeViewer`:

```
[zone-center] active=4 -> leaderboard:Leaderboard
              canvas, view, sessions, board, leaderboard
```

**Opening a code viewer changed which tab is selected in a different zone** — the graph went from
active to fourth-of-five, replaced by the Leaderboard. The entire stated purpose of rule 3 is
*"split one BESIDE the graph so the graph stays visible"*, and the operation that achieves the split
**deselects the graph on the way**. It is visible in the sense of "its zone is on screen" and not in
the sense a user means.

It is also inconsistent: the subsequent `newClassDiagram` leaves `zone-center` back at
`active=0 -> canvas:Graph`. So the deselection is transient and depends on the operation, which
makes it the kind of thing that reproduces for a user and not for whoever tries to confirm it.

Not diagnosed further here — `ZoneBackedLayoutService` is Core's under §2. Measured, not inferred:
the numbers above are the real service's, read from a probe driving the real shell.

### 5a. A second, independent finding: the placement policy's decision is discarded

`Decide` returned **`splitBeside=zone-center`** — rule 3, whose stated intent in its own comment is
*"split one BESIDE the graph so the graph stays visible."* The surface was then **tabbed into**
`zone-center` as its sixth tab, on top of the graph.

So the policy computes a split, and the zone-backed layout service tabs instead. Whatever the right
answer is, **the code viewer currently opens over the graph rather than beside it, and the code that
exists to prevent exactly that has no effect.** Either the policy is dead in the zone-backed world
and should be deleted, or its result is being dropped and should be honoured — but a policy whose
output is computed and ignored is worse than no policy, because it reads as though the behaviour
were considered.

Owner: Core (`WorkbenchAdapter.cs`, `DocumentPlacement.cs` sits with the App-side placement Core
routes). Raised, not taken.

---

## 6. MEASURED — "the graph repaints in the right dock after being moved left"

The last unobserved symptom, and it needed no AvalonDock: **it is in the layout model.** A probe
drove the real `ZoneBackedLayoutService` with every `DropKind` against every zone.

| Drop target | Landed in | Announcement |
|---|---|---|
| `zone-left` + **any split kind** | **`zone-center`** | *"Moved Graph within the center."* — and the graph becomes the **last tab** |
| `zone-right` + any split kind | **`zone-center`** | *"Moved Graph within the center."* — last tab |
| `zone-bottom` + any split kind | **`zone-center`** | *"Moved Graph within the center."* — last tab |
| `zone-center` + any split kind | `zone-right` | *"Moved Graph to the right zone."* |
| any zone + `JoinStack` | **the targeted zone** ✓ | correct |

**Only `JoinStack` honours the zone it was dropped on.** Every split-kind drop onto a side zone
puts the surface back in the centre.

### What the user saw

Dropping the graph on the **left** with a split gesture leaves it in `zone-center` **as the last
tab** — the right-hand end of the centre tab strip. Whether "right dock" was meant literally or as
"it ended up over on the right", **the graph demonstrably does not go left**, and it visibly moves
rightward from index 0 to last. That is the strongest available explanation of the report and it is
measured rather than reasoned.

### The announcement is confidently wrong

*"Moved Graph within the center"* is announced when the user dropped on the **left zone**. The
operation reports success and describes an outcome the user did not ask for — so a screen-reader
user is told the move worked and told the wrong destination. **This is the §8.3a family in the
announcer**: the honest data (the requested target) exists, and what reaches the user is a
confident statement about something else.

### It is the fifth partial sweep, inside the fix for the fourth

Core fixed `SplitRight` against the Center at `b5ca354` — `ZoneBackedLayoutService.Move` read the
drop's target node and ignored its `Kind` for everything except `Float`. That fix made
`zone-center` + split resolve to `zone-right`. **The sibling cases — a split dropped on `zone-left`,
`zone-right` or `zone-bottom` — were never swept**, and all of them still fall through to "within
the center".

Same shape as §8.11 and §8.13's first lesson: the fix asserted about the case in front of it and
not about the rule it was serving. `ZoneBackedLayoutService` is Core's under §2 — measured, raised,
not taken.

---

## 7. MEASURED — "Graph centred on X." is announced before the centring, and the centring can silently not happen

Core asked for one search rather than several bug reports: *is there a class of "reports success and
names the wrong destination"?* There is, and the strongest instance is a step further than the class
they described — **it announces success before attempting the work, and never looks at the result.**

Three sites, identical shape (`WorkbenchShell.cs:913`, `:1044`, `:1447`):

```csharp
Announcer.Announce($"Graph centred on {nodeId}.");
_ = canvas.RefreshAsync(nodeId);          // fire-and-forget
```

Two independent reasons the claim can be false:

1. **`RefreshAsync` opens with `if (!Ready) return;`** — it silently no-ops while the WebView2 is
   still initialising, which is the normal state immediately after a canvas opens.
2. **The task is discarded** (`_ =`), so a fault inside it is never observed by anyone.

**Observed, not read.** A probe constructed a real `CanvasSurface` outside a window:

```
canvas.Ready                      : False
RefreshAsync completed            : True
GraphSource was asked             : 0 time(s)
```

The refresh **did nothing at all** — the graph source was never even consulted — and in the shell
the announcement has already been made. A screen-reader user is told the graph centred on a node it
never looked up.

### Why this is worth its own entry rather than a line in §8.3a

Every earlier member of the family concerned a value that *existed* and did not reach the user. This
one concerns a **statement about an action**, made before the action, about an action that may not
occur. The information is not dropped in transit; **it was never true when it was said.**

There is also a live interaction with the node-budget finding (§1a of the chip spec): the graph draws
1,500 of 2,992 nodes, most-connected-first, and knowledge has median degree 0. So even when
`RefreshAsync` does run, a search hit the user selected may not be in the drawn graph — and *"Graph
centred on {label}"* is announced regardless. The two defects compound: the announcement is
unconditional, and the thing it describes is a lower-bound view.

### The fix shape

Announce **after**, from the result — the same correction Core made to the drop announcement, which
now names where the surface actually landed rather than where it was asked to go (verified: 20 of 20
combinations correct in both placement and wording). Here that means awaiting the refresh and
announcing what it achieved, including the honest negative: *"{label} is not in the current view"* is
a better sentence than a centring that did not happen.

`WorkbenchShell.cs` is Core's under §2. Measured, raised, not taken.

