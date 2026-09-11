---
id: plan-ui-and-windowing
title: "Execution graph — UI elevation, windowing behaviour, and F5 in parallel"
type: doc
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [plan, execution-graph, ui-design, windowing, docking, f5, coordination]
links:
  - { to: plan-conductor-front-door, rel: relates-to }
  - { to: kb-graph-and-loop-engineering, rel: depends-on }
  - { to: note-front-door-residuals, rel: relates-to }
review-by: 2026-12-11
summary: >-
  Three tracks planned as one graph: F5 exit evidence, UI elevation from seven pieces of operator
  feedback, and a windowing investigation with a numbered repro. Two assumed contentions were
  disproved by grounding, and the real constraint turned out to be a single decision gate over one
  of seven UI items rather than an ordering over all three tracks.
---

# Execution graph — UI, windowing, and F5 in parallel

## Stage 0 — Triage: plan, do not skip

Three tracks, a fan-out, two triggered hard vetoes (**UX & Accessibility** on contrast; **Owner**
on a feedback item that contests an accepted ruling), and a loop with no natural termination
(*"elevate the UI"*). **GO16's skip path does not apply.**

## Stage 1 — The naive graph, and what it would have cost

An unplanned agent does one of two things, and both are bad:

| Naive shape | What it costs |
| --- | --- |
| **Serial**: F5 → UI → windowing | Span = sum of all three. F5 alone is N7-shaped (**2,404 s measured** in Phase 1), and the UI loop is unbounded. |
| **All three at once on `src/AiDe.App/Workbench/**`** | Three worktrees writing one directory. The join is where it is discovered, which is the most expensive place. |

## Stage 2 — Floor nodes, immovable

| Floor | Why it triggers |
| --- | --- |
| **Owner ruling on feedback item 2** | *"a session should not have a terminal here"* **contests Ruling 21 and Addendum A's mode set**. A node must not silently reinterpret a ruling. |
| **UX & Accessibility hard veto** | Item 4 is a **contrast** defect — *"text is not readable… dark font on dark background… common issue in app"*. That is WCAG 2.2 AA territory and the lens holds a hard veto. |
| **`ui-craft-gate.py` + `design-lint.py`** | Already wired in `.github/workflows/ui-craft.yml`. The craft floor is a **gate**, not prose. |
| **Red-first for every claimed control** (CI6) | Including the windowing fix — a repro that is not observed failing first is a description. |
| **F5's oracle committed BEFORE its run** | Already a §F5 clause. *Seven points written after seeing the run are a description, not a test.* |
| **Audit entries** (AL5/AL5b) | Every substantive node. |

## Stage 3 — Edge classification, and two deletions

| Edge | Kind | Verdict |
| --- | --- | --- |
| F5 → UI work | *assumed ordering* | **DELETED.** F5's Proof Pack is a **snapshot citing its sha**. UI landing afterwards does not invalidate it; it means the evidence describes the tree it ran on, which is what evidence is. Holding UI behind F5 is **incidental ordering**. |
| UI ↔ windowing on `DockThemeAccents.cs` | *assumed data edge* | **DELETED — the premise was false.** Grounding shows `DockRoundedTabs.cs` is **33 lines of styling**; the tab-move handlers are in `WorkbenchAdapter.cs`, `WorkbenchController.cs` and `SurfaceChrome.cs`. **B touches theme and surfaces; C touches adapter and controller.** |
| Owner ruling → UI **mode** work | **decision edge** | **KEPT.** It changes the shape of that work. |
| Owner ruling → UI **contrast / icons / composer ergonomics** | *assumed* | **DELETED.** The ruling gates **one of seven items**. Gating all seven on it is the same incidental ordering in miniature. |
| Windowing investigation → windowing fix | **data edge** | **KEPT.** The fix's shape depends on the finding. |

**This is the whole optimization.** The naive span was three tracks in series behind one ruling.
The real graph is **three independent heads**, with one decision gate over **one seventh** of one
track.

## Stage 4 — Span, and the ceiling

Work `T₁` ≈ six nodes. Span `T∞` = **U1 → Owner ruling → U2 → close** — the UI chain, because it
alone carries a design stage, a decision gate and an implementation stage.

**F5 is not on the span.** That matters: F5 is the most expensive single node (**Verified: N7 was
2,404 s**), and the naive plan put it *at the head of the chain*. Moving it off the span is worth
more than any widening.

**Ceiling check before widening (GO4a):** three heads, each in its own worktree, no shared
authored file. Fan-out overhead here is one worktree creation and one merge per node — small
against node durations measured in tens of minutes. **Widening is justified; it was not assumed.**

## Stage 5 — Concurrency, with its five-part contract

**Wave 1 — three heads, at the cap:**

| Node | Goal | Writes | Capability |
| --- | --- | --- | --- |
| **A1 — F5** | Exit evidence + Proof Pack | `docs/proof/`, one oracle test | Reasoning |
| **U1 — UI design** | `/ui-design` stages 1–3: direction in words, design system, mockup with hard states, **rubric critique** | `DESIGN.md`, `docs/mockups/` | Reasoning |
| **C1 — windowing investigation** | Root-cause the asymmetric tab swap | a findings note | Independent review |

**U1 and C1 are read-only on `src/`.** That is what makes wave 1 safe, and it is a property of the
work, not a promise: a design stage produces mockups, and an investigation produces a diagnosis.

**Fan-out contract:** width **3** (the stated cap) · transient failure = report, never retry a
node · per-branch exit = each node's own exit condition below · **join rule: partial is
acceptable** — each merges independently, none blocks another · containment = one worktree per
node, no node runs a repo-wide destructive command (**DC-120**).

**Persona convenings do not consume the cap** — they are read-only reviewers with no worktree. The
Owner ruling on item 2 runs **during** wave 1.

**Wave 2 — two heads:**

| Node | Depends on | Why |
| --- | --- | --- |
| **U2 — UI implementation** | U1 (data) + Owner ruling (decision, for the mode item only) | Builds to an approved design |
| **C2 — windowing fix** | C1 (data) | Builds to a root cause |

## Stage 6 — Granularity, determinism, and transcription width

**Promoted:** the Owner ruling from "a question U2 asks" to **its own gate** — it is a decision
carrying independent risk, and a gate that fires early costs one node while the same gate firing
late costs everything built on it.

**Not collapsed:** U1 and U2 stay separate. `/ui-design`'s own discipline is *direction before
pixels, design system before screens*, and the gate sits between them.

**Declared shared surfaces, with jointly-satisfiable fail-clauses (GO14a):**

| Surface | U2's clause | C2's clause | Jointly satisfiable? |
| --- | --- | --- | --- |
| `src/AiDe.App/Workbench/**` | *Fails if:* a **theme, contrast, icon or surface-content** change alters tab placement or move behaviour | *Fails if:* a **placement or move** change alters any colour token or contrast pair | **Yes** — disjoint by concern, and each clause is **scoped to its own concern rather than to the directory**. |
| `DESIGN.md` | U2 owns it | C2 does not write it | **Yes** — single owner. |

*Neither clause is written as "fails if this directory changes", which is the unscoped form that
made two nodes collide at the front-door join.*

## Stage 7 — The loop, bounded

*"Elevate the UI"* has no natural end. `/ui-design`'s rubric critique is the cyclic node:

- **Variant:** the count of rubric findings at severity **≥ major**, strictly decreasing each pass.
- **Well-founded floor:** zero major findings.
- **Exit condition:** no major findings remain **and** `ui-craft-gate.py` exits 0 **and** the
  accessibility floor is met — *three conditions, because the gate is a floor and not a verdict.*
- **Cap:** 3 passes. **A firing cap is a defect signal, not a termination argument** — if pass 3
  still has majors, the node stops and reports, and the remaining findings become a ranked plan.

## Stage 8 — Disconfirm

**Test Architect:** does this prove everything the naive plan proved? The naive plan proved
nothing extra — it merely ran later. **No check is weaker.** The one addition: C2 must observe the
repro **failing first**, which the naive plan did not require.

**Simplifier:** does every boundary earn its place? The U1/U2 split does — a gate sits between
them. The C1/C2 split does — the fix's shape is unknown until the cause is. **The three-way wave-1
width earns its place because the heads share no authored file**, which was checked rather than
assumed.

**SRE:** is the bottleneck claim measured? **Yes** — N7 at 2,404 s is Verified from Phase 1's
ledger. Is parallelism layered on contention? **No** — grounding disproved the assumed shared file.

## Stage 9 — Before and after

| | Naive | Optimized |
| --- | --- | --- |
| Span | three tracks in series behind one ruling | **U1 → ruling → U2** |
| Width | 1 | **3**, contracted |
| F5's position | head of the chain | **off the span entirely** |
| Decision gates | one, implicit, late | **one, explicit, early, scoped to 1 of 7 items** |
| Loops bounded | 0 | **1**, with a variant |
| Floors | discovered at the join | **6, immovable, named up front** |

**Budget:** wave 1 ≈ 3 nodes, wave 2 ≈ 2, plus one convening. **Degradation path:** if a node
overruns, it **reports and stops** — it does not drop a gate, and it does not silently narrow. The
UI loop degrades by **reporting its remaining ranked findings**, never by lowering the craft floor.

**Re-plan checkpoints (GO17):** ① the Owner ruling on item 2 — if Terminal leaves the mode set,
U2's scope changes materially; ② C1's root cause — if the tab swap is in the **docking library**
rather than our code, C2 becomes a vendor-boundary question, not a fix.

## What the grounding changed, recorded because it was the useful part

The conductor briefed this planner with **two contentions that turned out to be false**: that
Track B and Track C would collide on `DockThemeAccents.cs`, and that F5's evidence forced an
ordering over the other two. Opening the files dissolved both. **The plan that survived is wider
and shorter than the one that was asked for**, and the reason is that the contention was asserted
from the directory name rather than from the call sites.
