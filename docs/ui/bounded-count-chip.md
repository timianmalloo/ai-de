---
id: ui-bounded-count-chip
title: "The bounded-count chip — rendering count.lower-bound on the category filters"
type: design
status: draft
owner: "@timianmalloo"
phase: "3"
tags: [ui, ux, bounded-reads, accessibility, canvas, session-3]
links:
  - { to: session-contracts, rel: relates-to }
  - { to: ui-craft-findings-2026-09-01, rel: refines }
review-by: 2026-12-01
review-suggested: []
summary: >-
  Spec for rendering DESIGN.md §4a's count.lower-bound on the canvas category chips, from a live
  measured instance: the Knowledge chip reads 257 against 878 declared. Covers the visible text, the
  accessible name — which today repeats the same false completeness claim and would be left wrong by
  a visual-only fix — the tooltip, and the unknown-total case. Implementation is Design's; this is
  the spec and the exact change.
---

# The bounded-count chip

**Session 3 · spec, not an implementation.** `CanvasPage.cs` is Design's under
[`session-contracts.md`](../collaboration/session-contracts.md) §2. This says what it should render
and why; the edit is Design's.

## 1. The live instance

Core measured it on the user's real store (`aide.31abcd25…`, 63,591 assertions) after the
`2026-09-01.8` re-index:

| | |
|---|---|
| the chip reads | **Knowledge 257** |
| knowledge nodes actually declared | **878** |
| graph draws | 1,500 of 2,992, most-connected first |
| knowledge median relation degree | **0** (non-knowledge: 4) |

**257 is true about what was drawn and reads as a statement about what exists.** That is
`DESIGN.md` §4a's rule verbatim — *a count that is a lower bound and one that is exact must be
distinguishable at a glance* — with a number attached, and it is the first live instance of
`count.lower-bound` anyone has been able to point at.

The reason it is low is not a rendering defect: the node budget is spent most-connected-first, and
docs hold intent, which has median degree zero. **The chip cannot fix that and must not hide it.**

## 2. What is not decided here

**How the node budget is allocated** — proportional per category, a floor per category, or
category-aware rank — is a decision about what the graph is *for*. Core deliberately did not pick
it; neither does this spec. It is a product decision and belongs to a decision note, not a chip.

**This spec holds regardless of that outcome.** Whatever the budget rule becomes, a drawn count
will still be a subset of a declared count for some category, so the chip needs to say so either
way. Fixing the budget does not remove the need; it changes the numbers.

## 3. The rendering

Core shipped the enabling half as **`DeclaredByKind`** — `IReadOnlyList<GraphKindTotal>?`, every
node kind the workspace declares with its count, drawn or not (`GraphProjection.cs:133`,
`CanvasGraphViewModel.cs:70`). It reaches the page as `graph.declaredByKind` through the existing
whole-record serialisation. **No new transport, no Core ask.**

It **replaced** the narrower `KnowledgeDeclared` rather than joining it — two fields for one
quantity is a defect signature (DM7), and nothing consumed the narrow one yet. The page's
`categoryOf` runs over the kinds to get per-category denominators, so Core never learns the
Code/Data/Infra/Specs/Knowledge taxonomy — that is the canvas's, and putting it in the projection
would make it wrong for every other consumer.

Two properties of it worth relying on, both of which Core established by measurement rather than
assumption: the knowledge flag travels **with each kind** rather than being inferred from it (no
kind is used both ways on this corpus — but that is one corpus, not a rule, and a total silently
wrong in a repository that used a kind both ways would look exactly like a correct one); and the
tests force the cap to bite and require the total to **differ** from the drawn count, because a
denominator that cannot disagree with its numerator would render `n of n` forever and every
assertion about it would pass.

### 3.1 Visible text

| State | Render | Why |
|---|---|---|
| drawn **<** declared | `Knowledge 257 of 878` | The ratio is strictly more informative than `≥ 257`, and it is available here. §4a's `≥ N` is the shape for a bound whose total is *unknown*; when the total is known, show it |
| drawn **=** declared | `Knowledge 878` | An exact count must stay visually plain, or the reader learns to ignore the qualifier — the same reasoning as Core's `ACompleteSearchPutsNoCaveatOnScreen` |
| total **unavailable** | `Knowledge ≥ 257` + `capped` chip | §4a's literal form. **Keep it — see §3.1a, this is the majority case today, not a legacy one** |
| zero drawn, some declared | `Knowledge 0 of 878` | **Not** hidden. A category the filter bar offers, showing nothing, with no explanation, is the defect the disclosure work exists to prevent |

### 3.1a Keep the `≥ N` fallback — and not for the reason it was offered

Core replaced `KnowledgeDeclared` with `DeclaredByKind` covering all 29 kinds, and asked whether the
`≥ N` fallback could now be dropped, suggesting it might still be worth keeping for stores written
before the field existed.

**Checked rather than answered: the fallback is not a legacy concern, it is the majority case
today.** `CanvasGraphViewModel` constructs `CanvasGraph` at **nine** sites. Exactly **one** passes
`DeclaredByKind`:

| Site | View | Totals |
|---|---|---|
| `:169` | the rooted/whole graph | **yes** |
| `:267` | focused view (`DescribeAsync`) | no |
| `:344` | **the overview — the canvas's default view** | no |
| `:404` | group drill-down | no |
| `:522` | route result | no |
| `:389`, `:449`, `:454`, `:466` | empty and error states | no — correctly, there are no chips to qualify |

So **four of the five populated views ship no denominators right now**, including the one a user
sees first. The field is declared `IReadOnlyList<GraphKindTotal>? = null`, so those sites compile
and return `null` silently.

**Therefore:**

1. **The fallback stays.** Dropping it would render the default view's chips with no qualifier at
   all — the exact false-completeness claim this spec exists to remove, reintroduced on the most
   visited surface.
2. **The four populated sites want wiring** (`:267`, `:344`, `:404`, `:522`). Core's file, Core's
   call; raised, not taken.
3. Until they are, the chip must **distinguish "no total available" from "total is zero"**. A
   missing map renders `≥ N`; a present map with no entry for a category genuinely means zero
   declared, and renders `0 of 0` only if the category is shown at all.

**This is the fourth partial-sweep of the day**, and the sharpest, because it is inside the field
added specifically to fix a partial-visibility problem: a nullable parameter with a default makes
every un-updated call site compile and return nothing, which is the same shape as a hand-listed set
— *the sites I remembered* — with the compiler unable to help. §8.10's pair applies directly: the
class was swept for readers and not for writers.

The `of 878` half carries `count.lower-bound`'s treatment — `{typography.mono}` and the muted
foreground already used for `.fchip[aria-pressed="false"]` — so the drawn number stays the primary
figure and the total reads as its context.

### 3.2 The accessible name — a second instance of the same defect, unnamed until now

`CanvasPage.cs:195-197` builds the chip's accessible name separately from its visible text:

```js
function label() {
  return c.label + ', ' + n + ' node(s), ' + (activeCats[c.id] ? 'shown' : 'hidden');
}
```

So a screen-reader user hears **"Knowledge, 257 node(s), shown"** — the identical false
completeness claim, in a second place, built from the same `n`.

**A visual-only fix leaves this wrong**, and it would leave it wrong *silently*, because the
accessible name is the one surface nobody looks at while testing a visual change. It must be
updated in the same edit:

> `Knowledge, 257 of 878 node(s) drawn, shown`

This is the §8.3a family reaching the accessibility layer: the honest number exists, one rendering
path uses it, and the other states the unqualified figure. WCAG 2.2 SC 1.3.1 — the accessible name
must carry the same information as the visible label; here the visible label is about to become
*more* honest than the name unless both move together.

### 3.3 Tooltip

`title` on the chip, naming the cause rather than restating the numbers:

> `878 declared; 257 drawn. The graph draws the 1,500 most-connected nodes and documentation is
> rarely the most connected.`

§4a asks the tooltip to *name the cap*. Naming only the number ("1,500 node cap") would be true and
useless — a reader cannot act on it. Naming the *rule* tells them why their docs are missing and
that nothing is broken.

## 4. The exact change, for Design

One site, `CanvasPage.cs` ~185-201:

1. Read `graph.knowledgeDeclared` alongside `counts` when composing `CATS`.
2. Compose the visible text per §3.1 rather than `c.label + ' ' + n`.
3. Compose `label()` per §3.2 — **the same edit**, not a follow-up.
4. Set `title` per §3.3.

**Verification, per §8.9's costing:** this is text in the DOM, so the existing real-WebView2 host
(`CanvasFocusIntegrationTests`) plus `CanvasSurface.EvaluateAsync` can assert it — remembering that
`EvaluateAsync` returns its own failure as an ordinary string, so the failure sentinel must be
excluded first, and that a green DOM assertion means *the value arrived*, never *a person can read
it*.

## 5. Residual, stated

This spec makes a bounded count legible. It does **not** make the graph show more knowledge, and a
reader who wants their docs drawn still cannot get them until the budget decision in §2 is made. The
chip's job is to stop the number lying; it is not a substitute for the product decision behind it.
