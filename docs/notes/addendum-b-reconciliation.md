---
id: note-addendum-b-reconciliation
title: "Reconciliation — Addendum B against the front-door work in flight"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [conductor, addendum-b, templates, reconciliation, composer, assist]
links:
  - { to: note-addendum-a-ratification, rel: relates-to }
  - { to: plan-conductor-front-door, rel: relates-to }
  - { to: spec-conductor, rel: refines }
review-by: 2026-12-10
summary: >-
  Addendum B arrived before the mistake it exists to prevent. No goal-block editor exists,
  no composer exists, and the front-door plan is still blocked in council review - so the
  re-base is additive rather than a migration. GoalBlock.cs already has the exact shape a
  template form engine consumes.
---

# Reconciliation — Addendum B against the front-door work in flight

Every row is a grep or a file read against `main`. **Nothing is inferred from the plan.**

## The headline: it arrived before the mistake

Addendum B's stated worry is that *"the goal-block editor you may be building (or have built)
under A5/R15 must be the first instance of the template form engine, not a bespoke form that a
template engine later duplicates."*

**No such editor exists, and none is being built.** The front-door plan (`F4`, the composer) is
**blocked in council review** — both vetoes returned BLOCK, and it has Rulings 19–25 and Security
conditions C1–C8 still to be revised against. **Not one line of composer code has been written.**

So B is not a migration. It is a constraint arriving before the code it constrains — which is the
cheapest moment it could possibly have arrived.

## (a) Does a goal-block editor exist? No — and what exists is *better than neutral*

`src/AiDe.Core/AgentPlane/GoalBlock.cs` is a **validation record, not a form**:

| Symbol | What it is |
| --- | --- |
| `GoalBlockFields` | six `const string` wire field names — `goal`, `done_when`, `not_in_scope`, `tier`, `fan_out_cap`, `budget` |
| `GoalBlock` | the typed record |
| `GoalBlockError(string Field, string Message)` | **field-level** errors, already keyed by field name |

**That is precisely the shape a template form engine consumes**: named fields, a typed value
record, and per-field errors. `template-schema/1`'s `fields[].name` for the `goal-block` template
**is already these six constants**, and they are the wire names spec §14.3 specifies — so the
re-base does not rename anything on the wire.

`GoalBlock.cs` also carries a remark worth preserving verbatim through the re-base: Phase 1
**validates** `fan_out_cap` and `budget` without **enforcing** them, and says so rather than
hiding the gap behind a name that reads as closed. A template form must not quietly imply
otherwise.

**Consequence: the re-base is additive.** There is nothing to tear out, and the regression
criterion ("goal-block-as-template changes no observable A5/R15 gating behaviour") is testable
against `SpawnContractTests.cs`, which already holds **four** tests over these six fields.

## (b) Does the composer have a shape concept from A5? No

No composer exists. A5's *"two submission shapes"* was specified and never built. `grep` for a
composer or submission-shape type in `src/AiDe.App/Workbench/` returns only unrelated surfaces.

**So "Free-form + templated, goal-block being template `goal-block`" costs nothing to adopt** —
it changes a plan, not code.

## (c) Onboarding / New Session gating? None — R21 is greenfield

There is **no New Session command** and **no first-run gate**. The only `NewSession` hits are
`AcpLaneClient.NewSessionAsync` and `GovernedRunHost` — **ACP's `session/new`, a different
"session" entirely.** That is the A3 vocabulary collision again, in code, and it is a reason to
keep the R21 gate's naming deliberate.

## (d) Conflicts with `template-schema/1` or the B3.2 source layout? None

- **No `.aide/` directory exists in the repo.** B3.2's `.aide/templates/` and `~/.aide/templates/`
  are greenfield.
- **No `providers.yaml` parser exists.** `EngineCatalog.cs:4` and `ProviderRegistry.cs:51` carry
  §14.2 as *vocabulary in comments*; `ProviderRegistry.cs:87` leaves parsing to a caller that does
  not exist. So B5.1's `assist:` node lands on an unparsed file — **it must be created, not
  extended**, and Ruling 23 already moved session config to JSON on exactly this ladder argument.
  **Whether `providers.yaml` follows is an open question this reconciliation does not answer.**

**One thing B9 assumes that is not there:** it says `template-schema/1` joins `loomkeeper/1` and
`weave/1` in "the same documentation home." **There is no such home.** `weave/1` and
`loomkeeper/1` are documented across `docs/design/`, `docs/architecture/` and `docs/api/` with no
pinned-contracts registry. Creating one is a small, real piece of work the addendum implies and
does not name.

## Seams

| Seam | Who reads it | Freeze order |
| --- | --- | --- |
| `template-schema/1` + validator | every catalog and form track | **first — the spine** |
| `TemplateCompiler` determinism | composer, recipe, restart | with the spine |
| `GoalBlockFields` ↔ `goal-block` template `fields[]` | the re-base | after the spine, before form rendering |
| assist provider account state | R21 gate, sheet, Settings | **independent of both** |

## Should a track pause, finish-then-migrate, or absorb?

**None of the three.** No track is mid-flight on the goal-block editor, because the plan that
would authorise it is blocked. The honest answer is **absorb, at zero migration cost** — the
constraint lands before the code.

**The only real cost is plan size.** The front-door slice already carries five nodes, ten
unresolved Test-Architect Blockers, seven Simplifier Majors, and eight Security conditions.
R18 + R19 + R21 add a pinned contract, a catalog with four sources and precedence, twelve
authored built-ins, a form engine, and an account gate.

**R18 and R19 cannot sensibly leave this slice** — they exist precisely so the composer is not
built twice, and the composer is in this slice. **R21 is separable**: the account gate touches the
sheet and Settings, not the form engine. That is the Owner's call and is put to it as such.
