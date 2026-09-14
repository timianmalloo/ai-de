---
id: note-atlas-defect-id-reconciliation
title: "Atlas defect identities - preserved history, pending reservation"
type: doc
status: proposed
owner: "@timianmalloo"
tags: [code-atlas, coordination, identifiers, lessons]
links:
  - { to: defect-classes, rel: relates-to }
  - { to: proof-code-atlas-production-adapters, rel: relates-to }
  - { to: investigation-code-atlas-publication-lifetime, rel: relates-to }
review-by: 2026-12-14
summary: >-
  Published register entries are preserved from a pinned main revision. Two Atlas meanings
  remain preserved here as named decisions pending collision-safe canonical reservation;
  historical audit records are not rewritten and source repair is not blocked on numbering.
---

# Preserve meanings without guessing new numbers

Owner turn 54 permits register-only reconciliation. Published entries numbered 177 through
208 are imported verbatim from `76c6d430a19492ffcd9b126e641ee70cda57ec69`. This is data reconciliation, not a product-main merge
or re-verification of every historical claim in those entries.

The collision check inspected 85 branch/remote refs and 47 live worktree locations. The next
sequential identity, numeric 209, is already present on `conductor/addendum-c` and its live
register for the non-text theme-indicator class. This pass therefore reserves **no replacement
number** and creates no holes or fabricated placeholder class. Numbering remains pending;
source repair proceeds under its independent grant.

## Mapping keys (not a global bare-ID replacement)

| Origin revision | Old DC numeric ID | Exact Atlas title | Canonical identity |
|---|---:|---|---|
| `7be3be92b8c6785d88895927f7dbe0647bb75a49` | 177 | Failure cleanup drops ownership before cleanup succeeds | Pending canonical reservation |
| `7be3be92b8c6785d88895927f7dbe0647bb75a49` | 178 | A textual patch targets a delimiter rather than its enclosing syntax | Pending canonical reservation |

These keys are revision + old number + title. Historical JSONL and historical branch commits
remain unchanged. A bare historical reference is not silently rewritten to a different class.
Current Atlas definitions are preserved verbatim below; they are **pending named decisions**,
not additional active numbered entries in the reconciled register.

## Pending publication class

**Name:** preparation completion mistaken for publication completion. Source work/pin and
cancellation ownership ended before the response writer. The original six native-Q reds,
the writer-lifetime correction and remaining post-drain/counter-oracle gates are recorded in
the linked investigation. Canonical numbering and semantic duplicate disposition remain open.

## Semantic-duplicate boundary

Published headings were checked for cleanup/disposal/lifetime/completion/publication and
patch/delimiter shapes. The nearest candidates include the one-time per-attach lifetime class,
lease duration versus edit duration, negative-oracle completion and poll-after-terminal-event
drain. This title screening does not establish full semantic equivalence. No Atlas class was
merged with one merely because both mention a lifetime or completion; full disposition occurs
before a canonical number is assigned.

## Preserved Atlas definitions

### Historical Atlas numeric 177: Failure cleanup drops ownership before cleanup succeeds

````markdown
### DC-177 — Failure cleanup drops ownership before cleanup succeeds

- **Additional native instance:** the Atlas qualification watch outlived its issuing
  thread, and then diagnostic inspection of a dead issuer threw before cleanup. The
  coherent candidate `c7c94153` captures immutable metadata while valid, keeps a bounded
  shared issuer alive and tests explicit diagnostic loss/cleanup. The cause run preserved
  three failures; Test inspected exited/live/cancel/mutation and shared-owner/drain oracles;
  Conductor replayed 350/350. Native cleanup-timeout accounting remains a separate production
  condition, not covered by those successful runs or the Shell control below.
- **Signature:** an asynchronous owner clears its lease/reader before awaited disposal,
  chains later transitions onto a faulted task, or destroys cancellation/admission primitives
  while operations still use them. Error text can also hide retained activation state.
- **Why it survives:** successful attach/close tests cover neither a throwing disposer nor
  in-flight admission. A caught exception can look like cleanup even though ownership was lost.
- **Instances:** 2026-09-13, Atlas Shell `dade5c77`; repaired in `8e691c6a`, joined `639be9d3`.
- **Class / sweep:** inspect the bounded Atlas owner and loading-host lifetime paths:
  factory, lease/reader disposal, view callbacks, queued invalidation, final close and
  admission failure. All share the owner's cleanup boundary. Inventory exceptions were
  already contained by the reader; that original review hypothesis was not a new defect.
  This is not a claim of a repository-wide lifecycle sweep.
- **Derive:** retain one owner's fields until successful cleanup, serialize replacement,
  retain failures for retry, isolate callback errors, and drain work before final primitive
  disposal. A permanently failing disposer leaves ownership retained, not falsely released.
- **Control:** `AtlasSharedHostAdmissionTests` repair cases for throw-once boundaries,
  close failure/retry, callback continuation, primitive drain/idempotence, admission
  cleanup and host retry. Ten initial semantic reds plus one admission-race red were
  read from the retained TRXs; Conductor's integrated run observed 90/90. Evidence and
  individual assertions are recorded in `proof-code-atlas-production-adapters`.
- **Boundary:** controlled for the tested Atlas component paths. Actual Core/MainWindow
  composition and noncooperative production dependencies remain unproved.
- **Status:** `controlled`.
````

### Historical Atlas numeric 178: A textual patch targets a delimiter rather than its enclosing syntax

````markdown
### DC-178 — A textual patch targets a delimiter rather than its enclosing syntax

- **Signature:** a new member is placed between an existing `try` and `catch` because the
  edit matches a nearby closing brace instead of the full enclosing member.
- **Why it survives:** the patch applies successfully; textual context is not a syntax tree.
- **Instances:** 2026-09-13, qualification-only Atlas diagnostics inserted two methods into
  exception blocks. No failing source was joined.
- **Class / sweep:** both new diagnostic members in `AtlasGitMembership.cs` had the same
  misplaced boundary. Conductor opened both locations and the CS1524/CS1513/CS1519 evidence.
- **Derive:** anchor structural edits to the complete named member and its surrounding
  declaration, not a generic closing brace. Keep the diagnostic change separate from a
  behavior repair so a compiler error cannot be mistaken for native failure evidence.
- **Control:** the normal Core build rejected both malformed placements before any new
  diagnostic test executed. Moving only the methods restored compilation; the same
  14/17 behavioral result then reproduced. Broken compiler log and diagnostic receipt
  are named in `proof-code-atlas-production-adapters`.
- **Boundary:** this compiler control detects malformed declarations, not valid syntax in
  the wrong semantic location; such edits still need their behavioral oracle.
- **Status:** `controlled`.
````
