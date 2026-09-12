---
id: review-code-atlas-data-constraints
title: "Code Atlas data and identity constraints before architecture"
type: doc
status: draft
owner: "@timianmalloo"
phase: "atlas-specification"
tags: [code-atlas, data, identity, review]
links:
  - { to: note-atlas-e1-identity, rel: depends-on }
  - { to: proof-code-atlas-contract-grounding, rel: depends-on }
  - { to: architecture, rel: relates-to }
review-by: 2026-12-12
summary: >-
  Records the independent Data/Persistence review's pre-model constraints: physical inventory,
  logical identity versus revision-bound spans, honest source binding, per-scope versus workspace
  coherence, and provenance/authority for interpretations and comparisons.
---
# Data constraints, not schema approval

**Reviewer:** `atlas-data-review-gpt55`, independent Data & Persistence Architect, GPT-5.5,
agent `91c51d36-21fc-4c00-8cf4-57fa50a1cb00`.
**Verdict:** PASS-WITH-CONDITIONS on these constraints; no implementation/migration approval.
The review inspected source/contract evidence and did not execute additional tests.

| Constraint | Finding / evidence | Required falsifier |
|---|---|---|
| Physical inventory is independent of semantic nodes | Node-addressed content cannot enumerate unsupported/unindexed files. Current evidence facts are not a complete physical inventory. | An authorized unsupported file remains in the explorer with an explicit state; no other file is substituted. |
| Source binding is explicit | `NodeContent.cs` and `ProjectionService.cs:1126-1245` return/read current file content after resolving indexed provenance, without sufficient indexed/current content identity. | Index R, edit without re-indexing, request content: refuse a false same-snapshot claim or return ChangedSinceIndex with explicit bindings. |
| Per-scope fencing is not workspace coherence | `WorkspaceCore.cs:119-215` and `StoreWriter.cs:64-95` fence one extraction scope/generation/revision. | A refreshed A and failed/old B cannot render as one coherent new workspace revision. |
| File, type and member identities differ | Type declarations, partials and member display text are different concerns. `has_member` is not a member key. | Same display text in two types/scopes cannot collide; partial declarations retain all locations without random first-match selection. |
| Spans are revision-bound | A location cannot be reused after file edits without content identity or explicit staleness. | Insert lines above a member; an old span is stale/unavailable rather than guessed into place. |
| Comparison binds immutable inputs and authority | Source set, document/clause status/revision, decision lineage and assessment rule/version all affect meaning. | Supersede a clause/decision; prior comparison is stale or lineage-qualified, never silently current compliance. |
| Interpretations retain their origin | Storage or human acceptance cannot transform model output into source evidence. | Unsupported model edge remains an interpretation after review; accepting it creates an annotation/decision record, not an extracted-fact origin. |
| One fact substrate and replayable projections | No new authoritative graph store; caches are labelled/rebuildable. | Rebuild a materialized view from its inputs and compare, or invalidate explicitly. |

## Conductor synthesis for the spec/architecture writers

The review's illustrative DTO tuples are **not** adopted as a schema. Keep logical identity
separate from revision-bound declaration/span/content records: inserting a line must not
silently turn a method into a different logical symbol. Scope/project/TFM belongs in the
collision model; exact encoding depends on the executed Roslyn probe.

"Every file" is bounded by authorized visibility. Metadata forbidden by policy is not forced
into the explorer; omission/coverage state must be honest where disclosure is permitted.

A coherent manifest must define its grain and source scope set. It is not a promise of an
atomic operating-system filesystem snapshot unless that stronger property is established.
Choose immutable captured content or hash-validated live reading deliberately; never infer
snapshot correspondence from path confinement alone.

## Blocked approaches

Deriving file inventory only from graph nodes; parsing `has_member` for identity; claiming
workspace coherence from per-scope fencing; claiming indexed source while returning unbound live
bytes; promoting model inference to extracted truth; or creating a second authoritative graph store.

These findings change requirements and architecture gates, not ownership. They authorize no
source edits before the agreed Core/Claude seam.
