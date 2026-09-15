---
id: note-understanding-views-n6-council
title: "N6 council on ADR-0038: PASS after Security re-review of a03fb622"
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
  N6 width-4 on ADR-0038. First pass: Security BLOCK. Repair a03fb622.
  Re-review: Security PASS, Tech Lead PASS. Data PASS and Simplifier
  PASS-WITH-CONDITIONS already held. Authors did not self-clear. ADR stays
  proposed. N7 (toolkit spike) is unblocked.
---

# N6 council on ADR-0038: Security BLOCK; Data PASS; Simplifier and Tech Lead conditions

- **Kind:** decision
- **Confidence:** **Verified** (four receipts opened; ADR HEAD opened)
- **Made during:** Conductor N6, session `grok-understanding-views-conductor`, 2026-09-15. Subject: `46160f21` on `understanding-views-architecture`.

## Panel (width 4)

| Lens | Session | First pass | Re-review (`a03fb622`) | Clears veto? |
|---|---|---|---|---|
| Data & Persistence Architect | `01a0a578-9ed7-7ad1-8d94-557f59cee6c6` | **PASS** | not re-run (grain held) | yes |
| Security & Identity Architect | `01a0a579-0208-7463-937a-a6cefd5834cc` then `01a0a588-9dbb-7df3-b50a-2bb41f13564d` | **BLOCK** | **PASS** | **yes** |
| The Simplifier | `01a0a579-0208-7463-937a-a6d27bc14307` | **PASS-WITH-CONDITIONS** | conditions landed in repair | yes |
| Tech Lead | `01a0a579-0209-7343-83dc-acc003dac114` then `01a0a588-9dbb-7df3-b50a-2bc466fff218` | **PASS-WITH-CONDITIONS** | **PASS** | **yes** |

Conductor does not override a hard veto. First-pass Security BLOCK stopped N7. Repair `a03fb622` closed the six Security blockers and the Tech Lead skip/drop-set conditions. Re-review cleared both. **N7 (toolkit spike) may start.** ADR-0038 stays **proposed** (authors did not mark accepted).

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

1. `conductor-join.py` `understanding-views-architecture` onto `understanding-views` (`--docs-only`; not `main`).
2. N7 Spike Protocol: tree toolkit (WPF TreeView vs alternative). Skip-widen of other walkers onto `UnanalysedLanguages.Skip` is optional after UV-0.
3. Then N8 `/ui-design`. Do not open core-query until N7 toolkit is named.

## What the Conductor must not do

Mark ADR-0038 accepted from this note (council PASS ≠ author self-accept). Implement UV-0 before N7 toolkit spike. `coord regen` / `coord install` from a worktree. Join to `main`. Atlas paths.

## Residual (do not reopen N6)

- Draft spec US-T11 still says a **process** probe; ADR §4 is App-assembly. Align spec on next spec edit (Security minor).
- `File.Exists` follows file reparse points — existing `ResolveWithinWorkspace` behaviour, not a new D-0 primitive.
- Caps 2000/5000 remain **Inferred**.
- N5 budget 119 vs plan 40 (GO9 finding).
