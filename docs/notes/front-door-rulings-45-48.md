---
id: note-front-door-rulings-45-48
title: "Decision note — Rulings 45–48: Terminal leaves Phase 1, edge ownership, maximize-on-create, craft-gate corpus"
type: doc
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [decision-note, ruling, conductor, front-door, ui, craft-gate, docking]
links:
  - { to: plan-conductor-front-door, rel: relates-to }
  - { to: plan-ui-and-windowing, rel: relates-to }
  - { to: note-front-door-council-rulings, rel: refines }
review-by: 2026-12-11
summary: >-
  Four rulings filed together. Three are the Owner's rulings on the UI track; the fourth is a
  retroactive filing of a number that was already being cited as authority. The numbering departs
  from the Owner's own allocation, and the reason is recorded here rather than corrected silently.
---

# Decision note — Rulings 45–48

## Why this note exists at all, and why the numbers moved

The Owner returned three rulings and allocated them **45, 46, 47**. Two of those numbers were
already in use.

A grep run while filing this note found eight. **The gate written to replace the grep found
eleven** — 1, 3, 5, 7, 32, 33, 34, 35, 37, 45 and 46 — all cited across `docs/` as binding
authority with no note defining them. *The first count was itself unmeasured.* `Ruling 35` is
cited six times as governing a dependency decision. `Ruling 7` is cited as forbidding an interface,
including by a note that warns a future session not to build the thing it forbids. **The Owner could
not see that 46 was taken, because nothing records it.** The absence of the register is what caused
the collision in the register.

So the allocation here is:

| Number | Decision | Provenance |
| --- | --- | --- |
| **45** | Terminal leaves Phase 1's built-in canvas modes | The Owner's Decision 3. Consistent with the citations that already existed. |
| **46** | Every edge in a slice's surface list is assigned to a node | **Retroactive filing.** Already cited by DC-130's control and by F4b's audit entry. Not re-litigated. |
| **47** | New Session maximizes its document | The Owner's Decision 1, **renumbered from 46**. |
| **48** | Craft gate reads committed source; fail-on-Major scoped to the artifact under review | The Owner's Decision 2, **renumbered from 47**. |

**This renumbering is the conductor's clerical act, not a change to any ruling's substance.** It is
recorded because a silent renumber of an Owner's allocation is exactly the shape of defect this note
is filed to stop. Recurrences 5 and 6 of **DC-013** carry the analysis;
`tools/verify-ruling-citations.py` is the control. Filing 45–48 leaves **nine frozen as 
pre-existing debt** (1, 3, 5, 7, 32, 33, 34, 35, 37): their text cannot be reconstructed without 
fabricating it, which is worse than the gap, so the gate freezes them **by name** and the list 
may only shrink.

---

## Ruling 45 — Terminal leaves Phase 1's built-in canvas modes; Console-only strip

**Operator feedback, verbatim:** *"a session should not have a terminal here - a session output is in
a console - dispatching to an active CLI session should be a fallback experience and to an active
terminal in the tool already not here"*.

**Ruling.** Drop Terminal from Phase 1's `BuiltIn` rows. Console is the only built-in mode until
observed-lane binding exists. **This is a Phase-1 registration cut — not a spec change and not a
default change.**

**Reasoning.** Ruling 21 is, in full, *"the canvas split is IN"*; it says nothing about a terminal.
The mode set is governed by **Ruling 22**, which requires modes to be data rows a later phase
appends. Addendum A §A6.1 defines Terminal as *"Observed lanes' live terminals for this session"* —
but `CanvasModeCatalog.cs:64-65` constructs a **new `TerminalSurface` per session**, which is not
what A6.1 specifies, and the ratified Phase-1 goal block reads *"with zero terminal hosting"*. The
ratification note already cut Artifacts, Profiler and Board on the rule *"a tab with nothing behind
it is dead UI"*; the same rule reaches the row that was left in. The operator's *"dispatch to an
active CLI is a fallback"* **is already the spec** — transfer-to-terminal survives as the
observed-lane path via the target selector — so that half changes nothing.

**Constrains.** `BuiltIn` = Console; split-control visibility binds to `CanvasModeCatalog.All.Count`;
Ruling 22's clause is re-proven with a test-registered mode; **Ruling 21 stands**.

**Explicitly does not touch.** Addendum A's text, ADR-0017, `TerminalSurface`, `Dispatch/`,
`Terminal/`, File → New Terminal Session (a spec invariant), or the Console default. Terminal
re-registers as a row — showing *existing observed lanes*, per A6.1 — in the phase that binds
observed lanes to a session.

**Confidence:** Verified.

---

## Ruling 46 — Every EDGE in a slice's surface list is assigned to a node (retroactive filing)

**Filed retroactively.** This number was already cited as authority by DC-130's control and by node
F4b's audit entry before any note existed. It is recorded here as it has been applied, not
re-litigated.

**Ruling.** A slice's E7 surface list assigns every **edge** to a node, not only every surface, and
**plan review fails when an edge is unowned**. The cheap form: for each pair of adjacent nodes,
write *"A's output reaches B by ___, owned by ___"*. If the blank cannot be filled with a node id,
the plan is incomplete — whether or not every node is.

**Why it was needed.** DC-130: F4 built a composer that constructed a real request and returned it to
a discarding caller; F2 built a lane reading a channel that was already drained exclusively
elsewhere. Both nodes discharged every clause they were given. Nothing in the product launched a run
from the UI at all, and the gap was found by the exit-evidence node writing an oracle for a clause
that turned out to be unsatisfiable. **A clause saying "the request is constructed" and a clause
saying "the lane renders events" do not, between them, say "the request starts a run."**

**Confidence:** Verified (the instance); the control's sufficiency is Inferred — no plan in this
repository carried an edge-ownership list before this slice.

---

## Ruling 47 — New Session maximizes its document via the existing `maximized` dock state

*(The Owner allocated this 46; renumbered per the table above.)*

**Ruling.** **Ratify** maximize-on-create, reopen-does-not-maximize — **on one condition**: it gets
its own red-first oracle before merge.

**Reasoning.** `DESIGN.md:157` already defines the state, and `StackState.Maximized` /
`workbench.maximizePane` are pre-existing and tested (`WorkbenchLayoutTests.cs:285-311`,
`WorkbenchControllerTests.cs:171-180`), so **no concept and no ADR is reopened**. The refusal path is
announced rather than swallowed (`MainWindow.xaml.cs:363-365`). The design review itself proposed
exactly this resolution and flagged that it *"needs an Owner nod before U2 builds it"* —
**U2 built ahead of the nod, which is a process breach, not a silent reinterpretation.**

**The gap that makes the condition non-negotiable:** no test in `tests/` exercises
`NewSession → Maximized` or `Reopen → not maximized`. The only maximize tests cover the command path,
so **this product behaviour currently has no oracle at all**.

**Conditions.**

1. One App-level test: creating a session leaves its stack in `StackState.Maximized`; reopening an
   existing session leaves the arrangement unchanged.
2. Register the defect class *a node executes a design-review recommendation that was marked as
   needing a ruling*, with the control being that a review's **"needs an Owner nod"** phrase becomes
   a **plan floor node**, not prose.

### CORRECTION, 2026-09-11 — this ruling's premise does not hold for the running path

**The ruling's reasoning cited coverage that does not cover the product.** It said
`StackState.Maximized` and `workbench.maximizePane` are *"pre-existing and tested"*, naming
`WorkbenchLayoutTests.cs:285-311` and `WorkbenchControllerTests.cs:171-180`. Node U2 opened them:
both construct **`new LayoutService()`**, the tree service. The shell constructs
**`new ZoneBackedLayoutService()`** (`WorkbenchShell.cs:113`), whose projection builds every stack
at the default `Docked` and hands `Layout` an **always-empty** maximize memo
(`ZonesToTree.cs:67` — `ImmutableDictionary<string, StackState>.Empty`). **`StackState` does not
round-trip, so `Maximized` is unobservable in this product.**

Swept: **66 tests construct the tree service, 8 construct the one the shell runs.**

**The ruling itself stands** — maximize-on-create is still ratified, and the behaviour still
arrives, because `ZoneLayoutService.Maximize` really does minimise siblings and `ZonesToTree`
renders that by omitting a collapsed zone. What was wrong was the *evidence* the ruling rested on,
which I supplied. The condition saved it: demanding a red-first oracle is what exposed the premise,
because the obvious assertion went red against a stub and **stayed red against the real body**. The
shipped oracle observes the effect `DESIGN.md` promises rather than the unobservable state.

Registered as **DC-135**. The lesson for every future ruling: *"pre-existing and tested"* must name
**which implementation** the coverage constructs, or it ratifies a property of code that does not
ship.

**Constrains.** Admits maximize-on-create as the delivery of feedback item 3's *"full window"*
request. Freezes `ShellViewMode` at two values. Cuts any *"third shell mode"* reading.

**Explicitly does not touch.** ADR-0017, A4.4.

**Confidence:** Verified.

---

## Ruling 48 — Craft gate reads committed source only; fail-on-Major scoped to the artifact under review

*(The Owner allocated this 47; renumbered per the table above.)*

**Ruling.** Fix the **corpus** first — pin targets to committed source paths. **Then** flip to
fail-on-Major **scoped to the artifact under review**, landed at the wave-2 join as a
conductor-owned commit. Not all of `docs/mockups`, and not inside a node branch.

**Reasoning.** `ui-craft-gate.py` hands targets straight to the detector with **no `bin`/`obj`
exclusion** (`:125`, `:290`), so a `src/AiDe.App` scan reads the git-ignored `bin/Debug` and
`bin/Release` copies. **The corpus is build-state-dependent.** But the consequence is smaller than
the conductor published: `src/AiDe.App/Web/composer.html` carries the same hex literals as its
`bin/Debug` copy — **19 and 19, re-measured** — so a clean checkout still finds them. **The defect is
duplication and staleness, not a phantom corpus**, and the node's claim that *every one of the 51*
came from `bin/Debug` **cannot be right as stated** while `Web/` is in the same walk. That count is
**Flagged until re-run on `src/AiDe.App/Web` alone**.

On the threshold: `ui-craft.yml` scans only `docs/mockups` and `DESIGN.md` and **declares itself
advisory in its own header**, so AGENTS.md's *"gate CI on it"* floor is **unmet, not falsified**.
`--gate` can fire only on Blocker (`:314`) and none exist. **Gating all of `docs/mockups` on Majors
would go permanently red on 66 findings the workflow header says are deliberate DX17 dense-meta
text — and the next step would be muting, which is worse than advisory.** The new artifact measured
**0 Majors**, so a Major gate *there* has discriminating power and costs nothing.

**Admits.** A `--fail-on <severity>` option in `ui-craft-gate.py`; a second CI step gating
`docs/mockups/session-front-door.html` + `DESIGN.md` at Major; an **advisory** scan of
`src/AiDe.App/Web` — committed source, **never `src/AiDe.App`**.

**Defers.** Major-gating `src/AiDe.App/Web` until the composer HTML is tokenized. Major-gating the
legacy IDE mockups until `docs/reviews/ui-mockups-craft-gate.md` dispositions them.

**Cuts.** Any new exclusion machinery — **the explicit path is the fix**.

**Conditions.**

1. The corpus fix lands **before or with** the threshold flip, never after.
2. `src/AiDe.App/Web` is re-measured alone and reported with paths; **the 51 is not carried forward**.
3. The workflow header's *"advisory"* sentence is rewritten to name what is gated and what is not.
4. Register the defect class: a gate whose target directory includes generated output. *(Filed as the
   2026-09-11 recurrence of **DC-006**.)*

**Confidence:** Verified on the script, the workflow and the corpus. The **13-vs-66 Majors discrepancy remains NOT RECONCILED** rather than resolved — the scopes were never compared.

### Condition 2, discharged 2026-09-11 — and the arithmetic is exact

Re-measured with `--json`, per target, over committed source:

| Target | Findings | Severity | From build output |
| --- | ---: | --- | ---: |
| `docs/mockups/session-front-door.html` | 5 | all Minor | 0 |
| `DESIGN.md` | 0 | — | 0 |
| **`src/AiDe.App/Web`** (committed source alone) | **17** | **all Major** | **0** |

**51 = 17 × 3** — the source file, its `bin/Debug` copy and its `bin/Release` copy. So the node's attribution was wrong in the way the Owner said (the findings are not *from* `bin/`), and right about the magnitude for a reason nobody had stated: **every finding was reported three times**. All 17 are real and live in committed source. The 51 is not carried forward.

### A deviation from this ruling's STATED MECHANISM, recorded rather than taken quietly

The ruling admits *"a `--fail-on <severity>` option in `ui-craft-gate.py`"*. **That was not built.** `ui-craft-gate.py` lives in `docs/ai-forward-pack/`, whose entire file history is pack-revision commits; `pack-apply.py` three-way-merges repo-local deviations and, **on conflict, parks the incoming text under `docs/ai-forward-pack/conflicts/`** for manual reconciliation. A threshold this repository's CI depends on should not live where a future pack update can send it to a conflicts directory.

Instead: **`tools/verify-ui-craft-floor.py`**, a repo-owned wrapper — the shape `tools/regenerate-derived.py` and `tools/verify-derived-views.py` already use for pack scripts. It invokes the pack script with `--json` and applies the threshold itself. **The ruling's substance is unchanged** — corpus pinned to committed source first, threshold scoped to the artifact under review — and it asserts two things the pack script cannot: a gated target under a build-output directory is **refused outright** rather than filtered (a gate that silently drops part of its corpus is the defect one level down), and an empty corpus is refused (CD9). Wired into the **every-push** ring, not the weekly one, because a floor that runs weekly is not a merge gate.

---

## On EvaluatorIntegrity — not a floor trip, and where the line is

The Owner ruled explicitly that this does **not** trip the human's EvaluatorIntegrity floor: the
evaluator reports what it observed and says in its own header that it does not gate. A threshold
that cannot fire is a **coverage gap**, which is the Owner's to rule on.

**It would cross the line** if a required check were *named* as the craft gate while unable to fail,
or if the report claimed to cover source it did not scan. **If the corpus fix is skipped and
`src/AiDe.App` is wired into CI as-is, this is re-raised to the human** — that would be a gate
reporting on files that are not the repository.

## The process finding, which is the conductor's and not the nodes'

Two of the three decisions were **executed before the ruling was recorded**: the maximize build, and
the "Ruling 45" correction issued to a node mid-task. The plan's own floor — *"a node must not
silently reinterpret a ruling"* — was **honoured by the nodes and skipped by the conductor path**.
The conditions above convert it into a control.
