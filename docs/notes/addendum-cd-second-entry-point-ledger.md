---
id: note-addendum-cd-second-entry-point-ledger
title: "The compile call is not a second composition root for a run — it opens compile-call.compose, not governed-run.compose, so CompositionRootLedger.Roots reads 0 for a compile and 1 for a compiled run; C16's named census and a negative-reference census over CompileCallHost hold the distinction"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "addendum-d"
tags: [decision-note, addendum-d, composition-root, ruling-13, compile, ledger]
links:
  - { to: adr-0035-compile-session-binding-and-pin, rel: relates-to }
  - { to: architecture-agent-plane, rel: relates-to }
  - { to: note-conductor-phase1-plan-approval, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: relates-to }
review-by: 2027-03-11
review-suggested: []
summary: >-
  Ruling 13's condition — the launch path is the one composition root, no second entry point — is
  enforced by two oracles: the named two-site census of `new GovernedRunRequest` and the
  CompositionRootLedger that counts governed-run.compose activities. The compile call composes the
  same agent-plane pieces in a smaller order and is kept out of both oracles by construction: it
  builds a CompileRequest, opens compile-call.compose on the same activity source, and references
  none of the run root's seven types. Blast radius: the F5 exit evidence's "one run, one root" clause,
  which stays a number.
---

# The compile call is not a second entry point, and the ledger can tell

- **Kind:** resolved-question (the brief's "second entry point question — Ruling 13 / clause 5")
- **Confidence:** Verified — `src/AiDe.App/Conductor/CompositionRootLedger.cs:45-80` (counts
  `governed-run.compose` on `aide.conductor.composition`; opens before anything that can throw);
  `GovernedRunHost.cs:62-67` (the activity opens first);
  `tests/AiDe.App.Tests/Composer/TheSendVerbIsHostOwnedTests.cs:189-213` (`C16` names the two
  files); `docs/notes/conductor-phase1-plan-approval.md:159-170` (Ruling 13's condition)
- **Made during:** `/define-architecture` of Addenda C and D (node A1, session `addendum-c-chain`)

## The call

**The question:** Ruling 13 forbids a second entry point for a run, and the F5 exit evidence asserts
it as a number — `CompositionRootLedger.Roots == 1` for one run. The agentic compile spawns the same
adapter, performs the same handshake and authorization, and prompts the same engine. Is it a second
root, and if not, how does the ledger *know*?

**The answer:** a composition root is defined by what it composes — for a run: catalog → process →
handshake → observed auth → **authorize → worktree → episode → prompt → seams → close → score**
(`agent-plane.md` §4). The compile call composes the first five and **prompt** only; it provisions
no worktree, opens no episode, monitors no lease, closes and scores nothing, and produces no
`GovernedRunRequest`. It is a *use of the plane*, not a *run*. The ledger distinguishes it by
construction, in three places, each a test:

1. **Activity name.** `CompileCallHost` opens **`compile-call.compose`** on the **same** activity
   source. `CompositionRootLedger` filters on the operation name `governed-run.compose`
   (`CompositionRootLedger.cs:66-70`), so a compile contributes **0** and a run whose prompt was
   compiled still reads **1**. Reusing the source, not the name, keeps one listener idiom and makes
   the distinction a string a test asserts. (No sibling ledger is written for the compile — the
   `called` row and the span count it; ADR-0035 rule 3.)
2. **The named census and the negative-reference census** — the oracles are **ADR-0035 rule 3**
   (stated once there: `C16`'s two named files; the eleven-name census over the non-empty
   `Conductor/Compile*.cs` set). This note keeps only the argument.

**What this does not permit:** a "compile" flag on `GovernedRunHost.RunAsync` that skips the run's
tail (ADR-0035's rejected alternative) — that is the second entry point wearing the first's name and
would count as a root in every ledger.

## Alternatives dismissed

- `a separate activity source for compiles` — a second listener idiom for one distinction; the name
  is enough and keeps `CompositionRootLedger` unchanged.
- `count compiles as roots and subtract` — makes the F5 clause a subtraction nobody re-derives; the
  clause must stay `Roots == 1`.
- `no ledger for compiles` — a compile is a model call; P10 and IO2 require it counted somewhere.

## Validation condition

Holds until/unless a compile is ever given a worktree, an episode or a lease (then it *is* a run
and must go through `GovernedRunHost`), **or the pin is ever loosened — a `CompileCallHost` whose
`session/new` carries anything but `SessionTools.None` is an ungoverned lane in the primary checkout,
whatever the ledger counts it as** (the Security Architect's finding; the sealed two-value type and
the negative-reference census in ADR-0035 are what keep the pin unvariable from there), or the F5
clause's oracle changes.

## Promotion rule

Carried by ADR-0035 rule 3; this note is the argument behind the rule.
