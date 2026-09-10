---
id: note-front-door-rulings-41-42
title: "Decision note — Rulings 41 and 42: session ViewModel placement, and the lease the sheet must not carry"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [conductor, front-door, ruling, f2, addendum-a, lease, placement, dc-118]
links:
  - { to: plan-conductor-front-door, rel: relates-to }
  - { to: note-front-door-council-rulings, rel: relates-to }
  - { to: note-addendum-a-ratification, rel: depends-on }
  - { to: note-front-door-ruling-38, rel: relates-to }
review-by: 2026-12-10
summary: >-
  F2 flagged two items rather than burying them. Both rulings went against the implementation, and
  the second went further than F2's own proposed alternative. Ruling 41 dissolves an apparent
  conflict between Addendum A §10 and F0's naming lint as a false dichotomy. Ruling 42 cuts the
  lease from the sheet entirely, because an all-covering lease carried out of the sheet is a
  disabled seam control, not a bounded shortcut.
---

# Decision note — Rulings 41 and 42

**Ruled by:** Owner agent, 2026-09-10. Confidence **Verified** — it opened the lint, Addendum A
§10, the ratification note's E7 surface list, `LeaseAndSeams.cs`, and both of F2's model files on
the branch.

Both questions came from **F2 flagging its own work** rather than shipping it quietly. Both rulings
went against the implementation, and on the second the Owner sided with F2's *argument* over F2's
*code*.

## Ruling 41 — the placement was a false dichotomy, and the conductor walked into it

**The apparent conflict:** Addendum A §10 places the session ViewModels in `AiDe.Core/Presentation/`.
F0's `tools/verify-r14b2-session-naming.py` fails any **new** type under `src/` whose name contains
"session" unless its path carries a `Sessions/` segment. So §10's location appeared to be forbidden
by a control this slice had itself shipped, and the conductor put it up as *"accept the deviation,
or move and amend the lint"*.

**Neither.** The Owner read the lint:

> Its only test is `"/Sessions/" in path` (`verify-r14b2-session-naming.py:59,138`), so
> **`AiDe.Core/Presentation/Sessions/` satisfies both §10 and the lint with no amendment.**

**Ruled:** the two §10-named types move to `src/AiDe.Core/Presentation/Sessions/`; the WPF surfaces
stay in `src/AiDe.App/Workbench/Sessions/`. No erratum. No lint amendment.

**The conductor's objection was also wrong on the facts.** It argued that moving would "split the
session surfaces across two projects" and bend the MVVM seam. The Owner verified the opposite:
`WatcherSessionsPaneViewModel` already lives in Core/Presentation with its surface in App, and both
of F2's types are already WPF-free (`System.*` and `AiDe.Core.*` only). **That split *is* the
convention.** Ruling 38's controls sit on the surfaces, which stay in App either way.

### A second deviation, unreported, and the conductor propagated it

> You wrote *"the ViewModels"*; the files on the branch are **`NewSessionSheetModel`** and
> **`SessionDocumentModel`**. **Neither §10 name exists anywhere in `src/`.**

§10 names them `NewSessionSheetViewModel` and `SessionDocumentViewModel`. The conductor took F2's
naming into a ruling request without opening the files — **DC-116 again**, and the reason the
ratification note's E7 surface list currently names a type that does not exist. The rename is part
of the move.

**Conditions:** (1) the r14b2 lint, the other nine gates and both suites re-run green on the merged
tree, no count lowered; (2) the ratification note's E7 surface list resolves to a **real** type name
after the move; (3) the placement rule — *"§10 Presentation types with 'session' in the name live at
`Presentation/Sessions/`"* — is written into `session-contracts.md` §2, so the next node does not
re-derive it.

## Ruling 42 — the sheet carries no lease

R19 says *"`Lease` is derived and displayed"*. At **sheet** time there is nothing to derive **from**:
the lease belongs to the **goal block** (spec §14.3), which is **F4's**. F2 built it as the whole
bound workspace, `["**"]`, under a `simplify:` marker — and then said so, volunteering that showing
*"not derivable until a goal block exists"* would be a better statement.

**The Owner went further than F2's alternative, and the reason is the point:**

> This is **worse than a weak display.** `NewSessionResult(Config, TaskClass, Lease,
> RoutableBackends)` carries a **live** `Lease(["**"])` out of the sheet, and `GovernedRunRequest`
> *requires* a lease — so the first downstream node that wires sheet-to-run hands the exit run a
> lease that never seams, which is exactly the case `LeaseAndSeams.cs:22-24` refuses:
> *"covers everything … looks like it is working."*
>
> **A `simplify:` whose stated ceiling is "the seam control does not discriminate" is not a bounded
> shortcut; it is a disabled control marked as one.**

**Ruled:** delete `NewSessionSheetModel.Lease` **and** the `Lease` member of `NewSessionResult` —
gone, not defaulted, so no downstream node can pick up a lease that never seams. The sheet displays
`Lease: not derivable until a goal block exists`.

**Marked plainly as EXTENDING Ruling 19, not reading it:** R19's purpose was that a lease is never
operator-typed. At sheet time there is no input to derive from, so **the honest display is its
absence.** Phase 1 exit evidence is unchanged — the exit run's lease comes from the goal block.

**Conditions:** (1) a test that `NewSessionResult` exposes **no** lease and the sheet renders that
sentence; (2) the plan's F4 section gains *"Fails if: a `GovernedRunRequest` is built with a lease
of `["**"]` or with no lease; the lease is the goal block's `lease.exclusive` (spec §14.3)"*,
red-first when F4 starts — **done, recorded in the plan under F4**; (3) the `simplify:` marker is
deleted **with** the property, not left describing code that no longer exists (HYG-A).

## The Owner's four one-line dispositions on F2's other findings

1. **`verify-surface-ownership.py` is non-recursive** over `src/AiDe.App/Workbench`, so F2's three
   new `*Surface.cs` files sit outside its scan. F2's by-hand assignment is **acceptable for this
   merge**; widening it is a **named follow-up node, not prose** — *or it is a memoir* (CI6). This
   is DC-118's shape in a **third gate in one day**, and the register carries it as such.
2. **The `providers.yaml` reader is not F2's.** Ruling 35 stands. Recorded against the node that
   owns the registry, with the sheet's empty-state sentence as the observed behaviour.
3. **The drain-thread race** — *"state whether a test fails **deterministically** without the lock;
   if none exists, record **not recorded**, not fixed."* A lock verified by an intermittent no
   longer appearing is **absence of evidence**. Put back to F2 as an honest three-way choice.
4. **The missing audit entry is the conductor's defect**, not F2's: the brief listed `docs/audit/*`
   among output not to commit, aimed at derived files, and swept up the append-only log.

**Merge order, as ruled:** Ruling 41's move, then Ruling 42's cut, then re-measure **both** suites
on the merged tree — *"Core's floor is unknown until observed there."*
