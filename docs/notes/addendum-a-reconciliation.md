---
id: note-addendum-a-reconciliation
title: "Reconciliation — Addendum A against the work already delivered"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [conductor, addendum-a, reconciliation, migration, naming, session]
links:
  - { to: spec-conductor, rel: refines }
  - { to: note-conductor-phase1-e18-close, rel: relates-to }
  - { to: plan-conductor-programme, rel: relates-to }
review-by: 2026-12-09
summary: >-
  Evidence-cited diff of Addendum A against Phase 1 as delivered. The A3 migration
  exposure is far smaller than anticipated - no run-log store was ever built, so the path
  rename costs nothing. Two new type names violate the naming rule and are cheap to fix
  now. The deferral of the Conductor Surface turns out to have avoided the rework the
  addendum would otherwise have caused.
---

# Reconciliation — Addendum A against the work already delivered

Every row below is a grep or a file check run against `main` at the Phase-1 close
(`2740bdc`), plus the in-flight Phase-2 worktree. **Nothing here is inferred from the plan.**

## The headline: A3's exposure is small, because the thing it renames was never built

| A3 concern | Measured state | Migration cost |
| --- | --- | --- |
| v1.0's run-log path `.aide/sessions/<run-id>.jsonl` | **No occurrence in `src/`.** Grep for `.aide/sessions` returns nothing | **Zero.** Phase 1 never built a run-log store |
| `AiDe.Core/Sessions/` namespace | **Does not exist** | **Zero** |
| Markdown projection `docs/sessions/<run-id>.md` | Not built | **Zero** |
| Bare "session" in new code | **Two types**: `GovernedSessionSource`, `GovernedSession` (`src/AiDe.Core/AgentPlane/GovernedSessionSource.cs:51,137`) | **Small and contained** — do it before more code lands |

Phase 1's eight nodes built the Agent Plane, not the session store: the store was always
Phase 3 work (`SessionStore`, `RunLogStore`). **The addendum's path change therefore costs
nothing at all** — it lands on a surface that does not yet exist, which is the cheapest
possible moment for it.

## The two naming violations, and why they are a judgment call

`GovernedSessionSource` and `GovernedSession` use "session" in the **Watcher** sense — a
registered lane identity carrying a `SessionCapability`, verified by `ITrustedRegistrar`.
A3's rule says *"bare 'session' in code means the user-facing container"* and *"new code
complies"*. This is new code, so on the letter it should be `GovernedLaneSource` /
`GovernedLane`.

But the underlying API it calls is itself session-named (`IngestHost.OpenEpisode` binds a
**session** id; `agent_session_dim` is the table). A3 also says the Watcher's vocabulary
migrates **opportunistically, no big-bang rename**. So renaming the caller while the callee
stays session-named may trade one confusion for another. **This is for the Owner**, not for
me — and it is cheap either way: two types, one file, no public consumers outside the
namespace.

## A live collision the addendum names and the code already has

`SurfaceContentFactory.KnownKinds` already contains a kind literally called **`"sessions"`** —
the Watcher sessions pane. A session **document** surface (A4.4) cannot reuse that string. It
needs a distinct kind, and the collision is exactly the one A3 exists to repair, sitting in
the code today.

## The deferral that avoided rework

Owner **Ruling 13** cut the single-lane Conductor Surface from Phase 1, keeping only the
headless App-layer launch path. Verified: no `ConductorPane`, `ConductorSurface` or
`SessionDocument` type exists anywhere in `src/`.

Addendum A7 anticipates the opposite case: *"If today's local work built the surface without
the entry flow, the migration is additive: wrap what exists in the session document, add the
sheet in front of it."* **We did not build it at all**, so R13–R16 land on greenfield rather
than on a surface that would have needed reshaping around a New Session flow it was never
designed for.

That deferral was made for unrelated reasons — no R1/R2/R4-core bullet required a pane. It
happens to have been the right call for a reason nobody knew at the time. Recorded as luck
observed, **not** as foresight claimed.

## What already conforms

- **`PromptDraftSurface`** exists (225 lines). A5 lifts its staging semantics into the
  composer and A7 keeps `File → New Terminal Session` unchanged — so it is **absorbed, not
  deleted**, exactly as the standing constraint requires.
- **ADR-0017** (`docs/adr/0017-primary-view-mode.md`) already establishes retain-never-rebuild
  against a live terminal. A4.4 extends the same invariant to session documents; the ADR
  stands rather than being amended.
- **The event envelope** (`RunEvent`) carries `kind` as an **open string, not an enum**
  (Phase 1 N1 ruling). A8 adds four new kinds — `session.open`, `session.config`,
  `artifact.registered`, `draft.transferred`. **They need no schema change**, because the
  open-string decision already made additive evolution free. `ext` preservation is proven
  against 88 real frames plus live traffic the corpus does not contain.
- **`EngineCatalog` + `ProviderRegistry`** already supply exactly what A4.3's sheet needs:
  engines as data with per-account health (`ready` / `needs-login` / `quota-degraded`).

## What is untouched by the addendum

The ACP client, lease/seam monitor, `mode` cohort column, worktree provisioner, spawn
contract and scoring path are all unaffected — the addendum specifies the front door, not the
planes behind it. `weave/1` stays pinned; A6's Profiler and Board modes are **placements of
existing panes**, not reworks.

## Recommended sequencing

1. **The two type renames first** — they are new code, the rule is effective immediately, and
   renames compound. Cheapest now.
2. **Storage paths need no migration**; write A3's shape when the store is first built.
3. **A distinct surface kind** for the session document, never `"sessions"`.
4. Then the surface tracks (R13/R14/R15/R16).

**The A3 "serial spine" the change order anticipated does not exist as feared.** There is no
store to freeze and no envelope change to land, so the spine is two renames and a naming rule
— hours, not a phase. The change order asked me to verify rather than assume this, and the
verification changed the answer.
