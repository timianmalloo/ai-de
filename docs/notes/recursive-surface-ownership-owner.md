---
id: note-recursive-surface-ownership-owner
title: "Recursive surface ownership: bounded Owner decisions"
type: decision-note
status: accepted
owner: "@timianmalloo"
tags: [coordination, ownership, verification, decision]
links:
  - { to: session-contracts, rel: relates-to }
review-by: 2026-12-15
summary: >-
  Defines the bounded interpretation of existing ownership declarations for the recursive
  Workbench surface gate. Assignments remain exclusively in session-contracts section 2.
---

# Recursive surface ownership: Owner decisions

## Goal and scope

Goal: make surface-ownership verification cover nested Workbench surfaces without inventing
ownership. Done when a reviewed, red-first-proven Python gate and Proof Pack are committed in
the isolated programme branch for integration handoff. T1; programme concurrency cap four,
including Astra Owner and Astra Conductor. This note is a decision artifact, not an ownership map.

Product source, UI behavior, new ownership policy, Atlas, Understanding Views, other sessions'
worktrees, and main integration without a recorded grant are out of scope. The Conductor controls
execution; the Owner resolves scope and semantics. Neither clears the author's hard veto.

## Evidence and reuse

Verified on this Owner worktree on 2026-09-15: `tools/verify-surface-ownership.py` reports
13 surfaces, 13 assigned, zero unassigned. An independent recursive filesystem listing finds
17 matching files. `Sessions/ProseView.cs` has no assignment in section 2's Path cells.
This is an unresolved ownership question, not evidence that its directory is Design-owned.

The existing gate docstring supplies the functional specification: exactly one owner or a
recorded unresolved assignment, with contradictions and stale exceptions rejected.
`docs/collaboration/session-contracts.md` section 2 supplies the sole authority and its existing
declaration notation. This note refines that interpretation; it does not introduce a new policy.
Architecture is the existing stdlib CLI and self-test entry point. No service, store, schema,
dependency, or product surface is added. UX is the existing developer diagnostic/exit-code flow;
visual UI specification is N/A. The slice is discovery through diagnostics and existing CI calls.

## Interpretation contract

1. Discover regular files recursively below `src/AiDe.App/Workbench` whose names end exactly in
   `Surface.cs` or `View.cs`. Do not widen discovery to Page, Builder, Dialog, or other suffixes.
   Emit repository-relative POSIX paths, preserving case. Do not case-fold distinct Git paths.
2. Read only section 2 ownership tables. Only the Path cell declares ownership. Prose, Why/Rule
   cells, later sections, and the Shared heading do not assign a surface. A new non-owner heading
   ends the preceding owner context.
3. Explicit repository-relative paths are exact identities. In a grouped Path cell, a bare token
   inherits the nearest preceding explicit path's directory in that same cell. A new qualified
   path resets that directory. Under Core Ruling 113, a standalone bare filename resolves only
   when exactly one recursively populated file has that basename; record its full relative
   identity. Zero matches fails as unresolved; multiple matches fail naming every candidate,
   even when another explicit row covers a candidate. Do not use basename identity afterwards.
4. Support the existing segment-local `*` and trailing directory `/**` forms. A single star
   cannot cross a slash. Directory coverage preserves path-segment boundaries. Do not introduce
   broader glob syntax or a general Markdown parser. Relevant unsupported forms must fail with
   a diagnostic rather than masquerade as coverage.
5. A surface-relevant malformed path row must be visible as a failure: missing delimiters or
   code-token structure, unresolved/ambiguous shorthand, unsupported glob form, absolute/traversing path, or
   absent owner context must not silently disappear. Unrelated non-surface rows remain outside
   this gate's jurisdiction. Missing section/contract and zero population fail closed.
6. Repeated matching declarations under the same owner deduplicate. Different owners matching
   one exact discovered path are a contradiction; there is no specificity precedence that can
   silently choose one. A recorded exception cannot suppress this contradiction.
7. UNASSIGNED uses exact repository-relative paths and nonblank reasons carrying the pending
   request id/ruling and retirement condition, as required by Core Ruling 113. It records only that
   a decision is pending. An exception is stale when its file disappears or section 2 assigns
   it. Preserve existing decisions and obtain the prescribed handoff before changing section 2.

The author must document and test the implemented grammar explicitly. If a current valid Path
cell cannot be interpreted under these rules, escalate the smallest counterexample to the Owner
before adding syntax. No owner may be inferred from product function or directory adjacency.

## Slice and proof floor

Affected surfaces: filesystem discovery -> path identity -> section 2 parsing -> ownership
decision -> diagnostics/exit status -> fixture filesystem -> existing CI invocation -> proof/audit.
The gate's scan comment must state root, recursion, token set, and allowlist, citing **DC-118
control half (b)**. The defect class is DC-118; reuse it rather than allocate a duplicate class.

Class -> sweep -> derive -> prevent: the class is a guard narrower than its asserted population;
sweep this gate's discovery, declaration, exception, and diagnostic identities; derive one exact
path identity end to end; prevent recurrence with recursive and duplicate-basename fixtures,
Path-cell-only parsing fixtures, ambiguity and stale-exception fixtures, and the real inventory.
The sweep does not authorize changing unrelated scan guards.

Testing trigger union: T1/T2/T4 and the CLI provider contract. Use meaningful deterministic
assertions, real temporary files, generated bounded invariants for parsing/matching, and injected
mutants that revert recursion, alias basenames, accept prose, bypass ambiguity, or retain stale
exceptions. Preserve the real gate and `--self-test` calls in `.github/workflows/build.yml`.
Observe the failure before the implementation fix; inspect actual diagnostics and cardinalities.
Do not claim the live register passes while an unresolved surface remains undispositioned.

The execution graph is grounding/handoff -> design and independent plan review -> one author
red/green -> independent implementation review -> Conductor join/proof/commit. Plan review must
include the optimize-graph Test Architect hard veto and Simplifier, Orchestrator, and SRE lenses.
Implementation review includes Test Architect and Python Developer; record each triggered lens's
shaped verdict. Reviewers remain independent of the author. The Owner's decisions waive no veto.

## Join-scope decision

Approved in scope: a programme-local join contract using the existing `conductor-join.py`, empty
`recount` and `build`, and `--no-push`. The script supports these explicitly. No .NET source or
test count changes in this slice, so a .NET recount/build does not prove this Python behavior.
Do not use `--docs-only`: Python behavior changes. Preserve conflict detection, append-only log
handling, audit, and derived regeneration. Keep applicable Python/register/docs gates plus the
actual surface gate and its self-test; the independent Test Architect must confirm the exact
selection against the trigger union. This decision does not authorize skipping a mandated gate.

## Conditions and residual risk

PASS-WITH-CONDITIONS for the bounded plan; T1 confirmed. Core tooling handoff is granted by
Ruling 113 in request `req-01M2JQ113TK7HGE7YKQ4CB92GA`; ProseView's ownership disposition is
pending request `req-01M2JQ3T5VMQWJ59Q3M0ZE420T`. Concurrent changes require rechecking the
inventory and declarations at join. The Conductor reports the missing graph-and-loop-engineering
evidence folder; treat that as a marked grounding gap and continue under the existing normative
GO protocol without inventing costs or measurements. No implementation or independent acceptance
is claimed by this note.

### Amendment: Core Ruling 113

Verified: the primary shared request ledger's resolution grants branch-local authoring on the
gate and its self-test entry, requires unique-population resolution for standalone bare names,
and requires exact exception identities with request/ruling provenance. This supersedes this
note's initial bare-first refusal. It is a new binding coordination decision, not permission
to infer ownership. The implementation must fixture both unique and ambiguous standalone names,
alongside grouped shorthand and duplicate basename isolation.

The grant lapses at main landing or stress-test end; section 2 changes still require a request
to the Claude conductor and an Owner ruling. Atlas may add `Workbench/Understanding/*View.cs`;
the second lander re-merges main and reconciles those declarations or recorded pending exceptions.
Candidate/base SHA and main-red Rulings 108/112 remain integration conditions; this programme
does not repair or conceal unrelated .NET failures.
