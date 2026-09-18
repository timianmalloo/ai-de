---
id: note-wind-down-state-2026-09-18
title: "Wind-down state, 2026-09-18: what landed, what is preserved, what every workstream owes next"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "conductor-watch-0915"
tags: [wind-down, watcher, worktrees, workstreams, coordination, known-good]
links:
  - { to: note-addendum-c-council-rulings, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: 2026-12-18
summary: >-
  The operator stood every session down and asked for a common known good before work is federated
  again. This is that state: what reached main, what is preserved and where, which lane cannot land
  and why, and the next action for each of ten workstreams. Nothing was deleted that held work, and
  no work exists in only one place.
---

# Wind-down state — 2026-09-18

The operator stood the GHCP watcher down, asked every session to wind down, and asked for a reset onto
outstanding tasks, worktree cleanup, and **a common known good before federating work out further**.
This session assumed the watcher role and produced the state below.

## The one-line answer

**`main` is green on CI — run 35379686294, `core-tests` · `gates` · `build` all success — and no work
anywhere exists in only one place.** `main` had been red since 2026-09-12. Both lanes have since
landed; the Explore lane's sixteen App tests were rewritten to Ruling 140's default rather than
weakened, and its receipt is `al-01M2V19J6D5PFZVEADASW69P7P`.

## What reached `main`

| Landing | What |
|---|---|
| `conductor/watch-0915` | Rulings 132–147, defect classes DC-226…DC-231, INV-0013 and INV-0014, the DC-229 census |
| `docs/atlas-audit-preservation` | Codex's seven Atlas audit records with their manifest proof, landed on the author's behalf after that session ended |
| `lane/main-red-0915` | the EntryPoints frame repair and clamp, the test-count floor that can no longer lower itself, the two App reds no session owned, and the App seam resolved once |
| direct to `main` | the DC-231 repair, and a superseding audit entry that cleared a capture gate this session's own landing had turned red |

The last full gate line before the final landing was **38 of 38, exit 0** — the first entirely green
run of this effort.

### Two corrections this session made against itself

- **The closing receipt for the earlier landing was wrong.** It attributed CI's build failure to the
  App suite. GitHub's own job record names the failing step as *"Mutation replay — the controls can
  actually fail"*, which **refused to start** because the tree was dirty and therefore measured
  nothing. Superseded by `al-01M2TTK8HVW7ERBX1FWQZ1JP0E`; the original is preserved. The watcher
  raised this and was right.
- **The 11px "conflict" was not one.** Reported as two committed controls disagreeing and routed to
  the Owner as unresolvable; Ruling 147 found the detector exempts short `kbd`-shaped text and the
  mockup had simply applied a keystroke token to running text.

## What is preserved, and where

| | Before | After |
|---|---|---|
| Worktrees | 148 | **34** |
| Local branches absent from the remote | **101** | **0** |
| Remote branches | ~92 | **193** |

The second cleanup pass ran only **after** every branch was on the remote and every peer session had
terminated, because until both were true a worktree could still be the only copy of something. 91
further trees were removed — all clean, all with their commits preserved on `origin` — including one
carrying 258 commits not on `main`. A worktree is a checkout; the work is the branch, and every
branch survives.

**A near-miss worth recording.** The dry run listed the *live* canvas lane as removable — "clean,
merged, unheld" — because its agent had not committed yet. With every peer session terminated,
**zero sessions were registered, so every tree read as unheld**, including one with work in flight.
The fail-safe rule protects committed work and uncommitted tracked changes; it cannot protect a tree
nobody has claimed. A `coord session start` on that lane moved it to KEEP and the set dropped 92 → 91.
The hold should have been taken before the first dry run, not after it.

The **34 that remain** are held for reasons the tool can state: 32 carry uncommitted changes, 1 is the
live lane, 1 is the primary. None holds product work — 11 are dirty only with untracked build output,
and 21 carry stale `docs/audit` copies whose canonical state is far ahead on `main`. They are reported
rather than removed, because the rule that refuses them is the same rule that saved the spikes.

- **23 worktrees removed** — every one clean, merged into `main`, and unheld. The fail-safe path;
  nothing else was touched.
- **101 branches pushed**, roughly 200 commits that existed only on this disk, including one branch
  with 35 commits. Preservation, not publication: none of it went near `main`.
- **Two complete Atlas contract spikes recovered** — 1,172 insertions across 9 files, fully staged and
  never committed, on a branch 0 commits ahead of `main`, in a tree queued for cleanup. They would
  have been destroyed silently. Committed exactly as their author staged them, judged in no way.
- Across every tree examined, **no product or tool file was uncommitted** — the dirt is build output
  and coordination ledgers.

## The lane that cannot land

`lane/p2-repairs-explore` carries P0a, P2, P3, P4, P6, Ruling 140's default change and two mockups.
**Core is green at 2,781. App fails 16**, every one a consequence of Ruling 140 deliberately changing
what the product opens with. Landing it would widen `main`'s red set by sixteen, which Ruling 112
forbids. Full diagnosis and what is owed: `note-explore-lane-not-landable`,
retained with the unlanded Explore lane. It is not an artifact on `main`, so
this note does not declare a main-graph link to it.

## The ten workstreams, and the next action for each

| # | Workstream | State | Next action |
|---|---|---|---|
| 1 | **Explore repairs (Stream X)** | P0a, P2, P3, P4, P6 built; lane unlandable | Rewrite the 16 App tests to the new default, in the desktop slot |
| 2 | **Sessions repairs (Stream Y)** | UX state table and mockup exist, **not approved** | A separate accessibility adversary clears the mockup (Ruling 147a), then phases 0→1→2→3→4→6 |
| 3 | **Canvas P0b + P1** | not started; Ruling 141 fixes the order | The container fix, with `fit()`+`place()` only on resize — never `layout2d`, or every node moves on a splitter drag |
| 4 | **EntryPoints byte budget** | Ruling 145 admitted | One projection-level `EntryPoints` call against a real store for `returned.rows` / `omitted.by_cap` / `WireBytes`, then the budget in `Evidence`'s accumulation shape |
| 5 | **Explore truthfulness** | named slice, not started | The two false numbers first: the banner counts nodes but says "edge(s)", and the chips read "Knowledge 0" on 878 knowledge nodes |
| 6 | **Explore Tree seam** | Ruling 140 deferred it | Reveal-in-tree, folder-to-group, expansion persisted by path, facet carry, Shift+F10 |
| 7 | **Gate controls** | DC-226/227/229/231 landed | The owed one: a CI clean-tree assertion **immediately after** the test step, so dirt is attributed to its cause and not to the next step that trips over it |
| 8 | **`understanding-views-d1`** (Grok, dark) | 14 commits; App seam already resolved in `main`'s favour | Decide the landing; its author's rendering is already adopted |
| 9 | **Atlas** (Codex, dark) | ~40 branches and 2 spikes preserved | A disposition pass — what lands, what is archived |
| 10 | **Cross-harness `xh`** (Copilot, dark) | `feature/xh-p2-projection` 35 commits, plus two more branches | A disposition pass by whoever picks it up |

**Held deliberately:** Ruling 126 groups 3–4 — the four `CodingsLeftExtentTests` and the intermittent
STA population. Untouched all session, and still held.

**Stale and reported, not deleted:** `chore/pack-63-gate` (816 behind, last touched 2026-09-07) and
`feature/ui-experience-refinement` (1,127 behind, 15 commits, last touched 2026-09-01). Both need a
keep-or-retire decision from their owners; neither is this session's to make.

## What a future session should read first

The register, then the rulings. Six defect classes were added in two days and every one of them is a
variant of a single shape: **a control's status was read as a statement about its subject.** A
harness's exit code for a tool's (DC-227); a checked-in control's version for the fleet's (DC-226); a
step's failure for a job's, where the step had executed nothing (DC-231); a default's green Core run
for a product surface with its coverage elsewhere (this session's own, in
`note-explore-lane-not-landable`). The controls are good. Reading them correctly is the harder half.
