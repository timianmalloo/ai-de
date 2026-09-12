---
id: coordination-code-atlas-resume
title: "Code Atlas - blocked delivery and explicit resume conditions"
type: doc
status: proposed
owner: "@timianmalloo"
phase: "documentation-content preparation"
tags: [code-atlas, coordination, worktrees, blocker]
links:
  - { to: note-atlas-draft-content-while-blocked, rel: depends-on }
  - { to: note-atlas-lane-admission, rel: depends-on }
  - { to: plan-code-atlas-fleet, rel: relates-to }
review-by: 2026-12-12
summary: >-
  Records the real cross-conductor delivery blocker, source ownership requests and safe resume
  sequence. It is a request ledger, not a second ownership register or permission to dispatch code.
---
# Code Atlas: isolated authoring active; shared integration remains gated

The user explicitly directed the Owner and Conductor to resolve the earlier stop and continue.
`note-atlas-isolated-authoring` supersedes the blanket source freeze. The sole section-2 register
records exact branch-local authoring grants; existing Core/Shell ownership is unchanged. The safety
probe is dispatched from current main `4d396411`, and E0 detailed design proceeds independently.
The earlier documentation-only checkpoint below is retained as history, not the current exit condition.

## Actual blocker and channel

Full native request ID: **`req-01M2B86TXF7SHG61B31P4H4173`**, addressed to
`conductor-addendum-c`. It requests Addendum E reservation and Core/Claude responsibilities.

`coord-core.py:1718-1763` appends, resolves and lists request records.
`cmd_collaborate` includes them only in its `summary` output. No call in that path injects a
message into the other harness. The shared `.agents/requests.jsonl` is visible from all
worktrees, but visibility is not a delivery acknowledgment.

The request remained open at the most recent recorded checkpoint. The narrowed follow-up
`req-01M2BGHNCM6WRD4ZZMBBFEEB4K` names the actual shared-file handoff. A prior one-time human relay
request did not establish delivery. The Owner now directs an executable isolated fallback, not
another relay request or repeated polling. Do not infer approval or resume Claude in a second session.

## Required acknowledgers

- **Claude primary conductor**: reserve/confirm E, route Core responsibility, agree serialization
  of main integration and preserve its active SH/CV/X1/F5 boundaries.
- **Core owner through that coordination**: inventory, C# member facts and query/source/IPC changes.
- **Shell lane owner**: Architecture host integration, surface registration/menu/routing and native
  window/layout seams. Atlas does not write these concurrently.
- **Conversation owner, later only if needed**: any agent-plane/read-only analysis integration.
- **Astra Owner**: content/scope decisions and horizon exit. Owner framing does not waive the
  other sessions' acknowledgment or a hard floor.

## Proposed change surfaces to negotiate

These are **requests**, not ownership assignments. Section 2 of
`docs/collaboration/session-contracts.md` remains the sole register. Architecture authoring will
refine the new-path names; no rename here grants edit authority.

| Requested work | Proposed new/affected paths | Required agreement |
|---|---|---|
| Inventory, logical identity and source binding | New narrow namespace under `src/AiDe.Core/Understanding/`; corresponding `tests/AiDe.Core.Tests/Understanding/` | Core accepts the exact carve-out and contract scope before code dispatch. |
| C# member/declaration production | `src/AiDe.Core/Extraction/CSharpExtractor.cs` and its existing/new targeted tests | One Core-agreed owner; no `has_member` parsing shortcut. |
| Bounded query and source contracts | `IWorkspaceQueries.cs`, `ProjectionService.cs`, `NodeContent.cs` or an explicitly versioned successor; `WorkspaceCore.cs` if required | Core agrees the model, source/hash semantics and backward-compatible seam. |
| Wire | `src/AiDe.Core/Ipc/WorkspaceOperations.cs`, `WorkspaceClient.cs`, relevant message/schema contracts | Core/daemon owner; version/capability/refusal compatibility evidence. |
| Native Atlas surface | New narrow `src/AiDe.App/Workbench/Understanding/` and corresponding App test folder, subject to final architecture | Registered owner for the new surface/test set; no inferred ownership from a free path. |
| Shell integration | Existing `SurfaceContentFactory.cs`, `WorkbenchShell.cs` / `DockHost.cs`, `PerspectiveShell.cs`, controller/menu/layout files only as actually required | Remains Shell-owned; consume an agreed seam or request a Shell-authored integration commit. |
| Store evolution, if needed | Existing Workspace store schema/writer/reader | Explicit additive migration and rollback/cache-rebuild contract; no speculative second store. |
| Later analysis | Existing Conversation-owned agent-plane/Conductor contracts | Not part of E0; separate stage admission and seam agreement. |

No simultaneous writer may target one authored file. Register/derived artifacts retain their
established merge mechanisms; regenerating is not a reason to edit another session's primary index.

## Draft provenance and remaining gates

- Conductor branch/worktree: `conductor/code-atlas`,
  `C:\Projects\ai-de-conductor-code-atlas`.
- Proposal: `proposal/code-atlas` @ `1065a851`, local reference only; private corpus not imported.
- Candidate spec: `atlas/specification`, `docs/specs/addendum-e-code-atlas.md`, final corrected
  revision and content verdicts recorded at join.
- K0 contract proof: joined report and correction commits through `e4229845`; source/SDK/store/IPC
  proof is bounded and does not establish native E0 or code admission.
- Proposed architecture writer: `atlas/architecture`,
  `docs/architecture/code-atlas-proposed.md`, with five isolated proposed ADRs; final author revision
  `51961b6c`, joined through `aac360e7`. Content reviews, Owner choices and the checked final
  simplification conditions are recorded in
  `review-code-atlas-architecture-content-gates` and the linked Owner notes.
- Spec content: integrated candidate through `a50329b2`, with subsequent report-attribution wording
  correction under the Owner's conditional countersign. It remains candidate/draft.
- Remaining admission checks: registration/ownership; then-current main/SH2 reconciliation;
  native opened-object/hard-link/race and decoder tests; exact resource budgets; generic encoding/
  seal/replay and new wire proofs; phase-specific design/implementation/native/provider gates.
  None was satisfied by document review.

## Resume sequence

This sequence applies to shared-file integration and native-product closure. Independently granted
new-file authoring does not wait for step 1; it follows the active plan and its safety/design joins.

1. Have the intended counterpart read the full request and this ledger; obtain actual resolution
   and section-2 responsibility updates. Re-check that E is still the right addendum letter.
2. Reground against current main and active lanes; do not fast-forward/rebase another person's
   dirty tree or treat old commit evidence as the current shell contract.
3. Finalize the content gates and proposed decisions; distinguish document readiness from normative
   acceptance, ownership and source dispatch.
4. Release only E0, the Owner's first integrated physical file/type/member/source horizon.
   Run `/design-slice`, tests red-first, implementation, native real-workspace journey and independent
   review in assigned worktrees. No type-only or static-fixture substitute.
5. Converge serially through the agreed integrator, union append-only records, regenerate derived
   views after audit changes, read back gates/state and seek the Owner's horizon closure.
6. Admit each later stage separately. Until then report the later stages as unbuilt.
