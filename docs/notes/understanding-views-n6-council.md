---
id: note-understanding-views-n6-council
title: "N6 council on ADR-0038: Security BLOCK; Data PASS; Simplifier and Tech Lead conditions"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "understanding-views"
tags: [decision-note, addendum-c, understanding-views, N6, D-0, adr-0038]
links:
  - { to: adr-0038-d0-solution-tree-census-and-kind, rel: relates-to }
  - { to: spec-understanding-views, rel: relates-to }
  - { to: note-understanding-views-n4-pass, rel: relates-to }
review-by: 2027-03-15
review-suggested: []
summary: >-
  N6 width-4 on ADR-0038 at 46160f21. Security hard BLOCK (path confinement,
  junctions, skip fail-closed, DropRelativePaths on the wire). Data PASS with
  projection-join majors. Simplifier PASS-WITH-CONDITIONS. Tech Lead conditions
  on skip binding. Authors do not self-clear. N7 not opened.
---

# N6 council on ADR-0038: Security BLOCK; Data PASS; Simplifier and Tech Lead conditions

- **Kind:** decision
- **Confidence:** **Verified** (four receipts opened; ADR HEAD opened)
- **Made during:** Conductor N6, session `grok-understanding-views-conductor`, 2026-09-15. Subject: `46160f21` on `understanding-views-architecture`.

## Panel (width 4)

| Lens | Session | Verdict | Clears veto? |
|---|---|---|---|
| Data & Persistence Architect | `01a0a578-9ed7-7ad1-8d94-557f59cee6c6` | **PASS** | yes — grain/Coverage/no-store hold; majors are join rules |
| Security & Identity Architect | `01a0a579-0208-7463-937a-a6cefd5834cc` | **BLOCK** | **no** — hard veto |
| The Simplifier | `01a0a579-0208-7463-937a-a6d27bc14307` | **PASS-WITH-CONDITIONS** | yes |
| Tech Lead | `01a0a579-0209-7343-83dc-acc003dac114` | **PASS-WITH-CONDITIONS** | **no** until skip binding + drop-set leave the wire |

Conductor does not override a hard veto. **N7 is not opened.** Author repairs ADR-0038; Security re-reviews. Author does not mark the ADR accepted.

## Security blockers (must land in the ADR before re-review)

1. File-artifact join uses `ResolveWithinWorkspace` (containment + exists), not raw `ScopeLocation` + `artifact_path_id`.
2. Census walk does not follow reparse points / junctions (`EnvelopePurge` already refuses this class; `UnanalysedLanguages.Enumerate` does not).
3. Skip policy is fail-closed for UV-0: consume `UnanalysedLanguages.Skip` (Tech Lead binding); at least `bin`, `node_modules`, `.git`, `obj` omitted in production, not only in tests.
4. `DropRelativePaths` is **not** on the IPC/request type. Integer caps stay on the wire. Named drop-set lives on the projection/test host.
5. PROBE-APP-ENUM is an App-assembly / non-`IWorkspaceQueries` rule, not a PID rule (ADR-0009 in-process host).
6. Payload: count caps plus a byte/frame bound (`EveryOperationFitsTheFrameTests` / shrink), not count-only.

## Data majors (fold into the same repair; not a grain reopening)

- Identity / collapse / `declared_at` use `PathComparison.ForThisFileSystem` and the same slash normalisation as the drop-set.
- Coverage `indexed-parent` also if a descendant census-folder is indexed-parent.
- Drop file-artifacts whose parent census-folder is absent from `Nodes`.
- One numeric home for cap-omit (`OmittedByCap`); Disclosure copy derives from it.

## Simplifier / Tech Lead conditions (same repair)

- Do not ship public `IWorkspaceDirectoryCensus` or `IDirectorySkipPolicy` in UV-0. Internal callback / real IO is enough.
- Collapse `SolutionTreeQuery` / `SolutionTreeRequest` if fields stay identical.
- Caps 2000/5000 stay **Inferred**, not a product requirement.

## N5 budget finding

Harness counted **119** tool calls on N5 vs plan budget **40**. Author reported 40/40. Recorded as a defect signal (GO9), not a termination argument. Do not raise the plan budget silently.

## What the Conductor may do next

Resume architecture author on `understanding-views-architecture` with the blocker list. Then spawn Security (and Tech Lead on skip/drop-set) for re-review. Do not open N7 until Security PASS.

## What the Conductor must not do

Override Security. Join ADR-0038 to `understanding-views` while BLOCK stands. Implement UV-0 on the unrepaired text. `coord regen` from a worktree.
