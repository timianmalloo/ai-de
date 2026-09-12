---
id: note-coordination-two-lanes-not-three
title: "Two code lanes, not three — the composer's send gate is where Addendum C's read-only turn, its composer-as-conversation and Addendum D's Project() all land, so the Conversation lane owns the send path end to end; and two coordination-layer findings ready to append at the join"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "addendum-c"
tags: [decision-note, coordination, addendum-c, addendum-d, worktrees, simplifier, go5]
links:
  - { to: coordination-addendum-cd, rel: relates-to }
  - { to: note-addendum-cd-architecture-p1-inputs, rel: relates-to }
  - { to: adr-0033-prompt-compilation-bounded-context, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: relates-to }
  - { to: architecture, rel: relates-to }
review-by: 2027-03-11
review-suggested: []
summary: >-
  The brief offered twelve candidate tracks. Classifying the artifacts removed the generated files
  from contention; the remaining contention is one file, ComposerSendGate.cs, which three candidate
  tracks would author in one wave. The boundary fix is one Conversation lane (C-0 → C-3 → D-1 → D-2 →
  D-3's code) beside one Shell lane (C-1a → C-1b → C-2), width 3 with a side slot. The third code
  lane was GO5-admissible and still refused on the lexicographic objective; the cut is recorded with
  its re-plan trigger. Two layer findings — a registered derived artifact with no merge attribute,
  and a per-repository regen marker tracked in one checkout — are written as classes, ready to
  append when the conductor allocates their ids at the join.
---

# Two code lanes, not three

*Below-ADR judgement made by node P1 (`/prepare-for-coordination`, session `addendum-c-chain`,
2026-09-11). The plan is `coordination-addendum-cd`; this note carries the reasoning the plan's
tables compress.*

## 1. The decision

**Two code lanes** - Shell (`SH-1 → SH-2 → SH-3`) and Conversation (`CV-0 → CV-1 → CV-2 → CV-3 →
CV-4`) - plus three side tracks (`DS-1` docs, `PD-5` the attended spike, `X-1` the census controls),
under the standing width cap of 3. Not the three code lanes (Shell · Coding · Compile) the
architecture's slice table reads as at first sight.

## 2. Why the third lane was the real question

The brief's candidates split naturally into Addendum C (shell and composer) and Addendum D (the
compile step). A Compile lane is GO5-admissible on paper:

- **Data edge:** one-way. `AiDe.Core/Compilation` is a new namespace; the App consumes
  `Projection.Project(fold) → (GoalBlock, Lease, Prompt, TaskClass)` whose signature ADR-0033 fixes.
- **Decision edge:** none left - the event kinds, the fold, the projection's domain and the three
  named call sites are all decided in ADR-0033/0034.
- **Shared exclusive resource:** the two lanes **meet at two files** - `ComposerSendGate.cs`
  (`Send` via `Project()`, ADR-0033; the read-only shape, Ruling 73) and `ComposerCompiler.cs` (the
  render call site, the first of the three the census counts). Both are the Coding lane's.

So the Compile lane would deliver its App wiring as **seam requests** into the Coding lane, and the
E7 chain (Addendum C §B7 *consistency across surfaces*; D US-D1) would be closed by a node in a
different session from the one that built the fold. That is the shape the skill warns about: *a
track that splits an aggregate will generate seam requests forever*. The aggregate here is the send
path - typed text → pre-compile → envelope → `Project()` → `GovernedRunRequest` - and Ruling 73 makes
the read-only turn a **projection of that same envelope**, so C-0, C-3 and D-1 are three slices of
one aggregate, not three tracks.

**The objective settles it.** (1) completeness and rigor · (2) token cost · (3) speed. Three lanes:
rigor equal at best (arguably lower: the DC-135 shape - a property proven in one session and cited by
another - is exactly what a split send path invites), tokens ↑ (a third concurrent context at the
orchestrator-worker rate, GO6), speed ↑. A plan that is faster while equal on rigor and worse on
tokens is refused by construction.

**Re-plan trigger (GO17):** if the operator's wall-clock, not tokens, becomes the binding constraint,
split at CV-2: the Compile lane takes `AiDe.Core/Compilation/**`, `Cli/**`, `Conductor/Compile*.cs`,
`tools/compile-eval/**`; the App wiring of the send gate and render site stays with the Conversation
lane's owner as a named slice; the `Projection.Project(` census (root `src/`, recursive, allowlist
the three sites) is the seam's guard. Nothing else in the plan changes.

## 3. The other cuts, briefly

- **D-0 (the store)** folds into CV-2 as its first commit: one Core file, the fold reads it in the
  same session; a separate track saves ≈ 1,500 s of critical path for a second D&P context.
- **C-2** folds into the Shell lane as SH-3: `WorkbenchLayout.Default(perspective)` is layout data
  of the same aggregate SH-2 builds, and its human validation needs host B anyway.
- **Architecture-perspective kinds** are five allow-list rows (SH-1) and Ruling 54's scaling fix (SH-3).
- **D-3 admission** is a calendar gate: the corpus is real operator envelopes (DC-127 forbids fixtures
  alone); its code is CV-4.
- **The craft profile** is pack-owned and struck; compiles run with `none` (ADR-0037, Flagged).
- **INV-0007 phases 5-6** share CV-1's files and fold in; INV-0008 phase 7 is an operator decision.
- **PD-5** is *promoted* out of D-2: the one case a worktree exists for - an attended run against a
  real engine whose failure is a hard stop for every agentic rung - and it depends on S0 only.

## 4. Two findings from turning the layer on, as classes (ids allocated at the join)

Neither is allocated a `DC-` number here: the family is **contiguous** and two live trees already
hold the two ids after `main`'s DC-147 (INV-0009's classes), so a branch-allocated number either collides or gaps
(DC-013). The conductor appends both at the first join and allocates then.

### DC-nnn — A registered derived artifact with no merge attribute merges as authored, and `doctor` reports the driver effective

- **Shape:** the coordination layer declares an artifact's class in two places written by two
  mechanisms - the registry line (`.agents/artifacts.yml`, hand-editable) and the git attribute
  (`.gitattributes`, written by `coord install` *from* the registry). A line added to the registry by
  hand after the install never gets its attribute. The class is declared; the driver never fires;
  the file conflicts with markers exactly as if it were authored - and `doctor` says *"merge driver
  effective"* because it checks that **a** driver is declared and registered, not that **every**
  registered pattern carries an attribute.
- **Signature:** `git check-attr merge <path>` → `unspecified` for a path the registry calls
  `derived`; a conflict with markers in a file whose class promised regeneration; the registry's own
  comment recording that exact conflict.
- **Instance (2026-09-11):** `docs/_meta.json` - registered `derived` by hand (the registry says so:
  "SETUP 4 registered only the first ... I declared one path anyway"), attribute absent, 10 of 11
  patterns attributed. Repaired by P1 with the one line `coord install` would have written, after
  running `tools/build-doc-viewer.py` (diff: the provenance stamp only).
- **Control (proposed):** a coverage check that reads the registry's `derived`/`register` patterns
  and fails when any lacks a `.gitattributes` line - the natural home is `coord doctor`'s merge-driver
  check (pack-owned) or, in this repository, `tools/regenerate-derived.py`'s existing
  `check_registry_coverage()` widened from "has a generator" to "has a generator **and** an
  attribute". Red first: remove the `_meta.json` line and the check must fail.
- **Status:** `partially-controlled` - the instance is fixed; the check is prose until the tool lands.

### DC-nnn — A per-repository coordination marker tracked as a file in one checkout is mutated by any worktree's command, and the mutation lands as an uncommitted change in a tree that did not run it

- **Shape:** the layer's record is **per repository** (`repo_root()` resolves through the git common
  dir, so `.agents/` is one directory for every worktree). One of its files - the regeneration-owed
  marker - is **tracked in git**. Any worktree that runs `coord regen` rewrites or deletes the marker
  in the *primary checkout's* working tree. The session that ran it cannot reverse the change (it is
  not its tree), and the session that owns the primary did not make it and may commit it unaware.
- **Signature:** `D .agents/regen-owed.txt` (or a modification) in `git status` of the primary
  checkout with no primary-side action that explains it; `doctor` in a worktree reporting "nothing
  owed" while the worktree's own tracked copy still lists paths.
- **Instance (2026-09-11):** P1 ran `coord regen` in `ai-de-feature-addendum-c` to clear seven owed
  regenerations (all regenerated with **no diff** - the marker was stale bookkeeping). The command
  deleted `C:/Projects/ai-de/.agents/regen-owed.txt`; the node was refused permission to restore it
  (correctly - the primary is not its tree). Reported to the conductor with the one-line restore.
- **Control (proposed):** the marker is runtime state, like `.agents/sessions/` - git-ignore it
  (`docs/notes/conductor-agents-gitignore-deviation.md` is where that decision lives), or have
  `coord regen` refuse when the record root is not the current worktree unless `--here` is given.
  Red first: run `coord regen` from a linked worktree against a fixture and assert the primary's
  tree is untouched.
- **Status:** `partially-controlled` - reported and the branch removes the stale marker; the
  mechanism is unchanged.

## 5. What this note does not decide

The CLI home for `aide compile fold` / `aide session purge` (`/design-slice`'s call; the plan
reserves `src/AiDe.App/Cli/**`); the exact write-tool set for the read-only lane (CV-0 reads it from
the SDK's tool list); whether a read-only turn runs in the workspace or a throwaway tree (Ruling 73
leaves it to the architecture; CV-0's constraint is that it cuts nothing the operator must clean up).
