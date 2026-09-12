---
id: plan-code-atlas-fleet
title: "Code Atlas - Owner-led fleet execution checkpoints"
type: doc
status: draft
owner: "@timianmalloo"
phase: "specification and architecture"
tags: [code-atlas, fleet, gpt, coordination, worktrees]
links:
  - { to: note-code-atlas-proposal-provenance, rel: depends-on }
  - { to: spec-addendum-c-perspectives, rel: refines }
  - { to: architecture, rel: relates-to }
review-by: 2026-12-12
summary: >-
  Bounds the user-requested Astra Owner/Conductor and GPT execution fleet. Specification and
  contract grounding are isolated now; implementation is admitted only after the model,
  architecture, vetoes and cross-Claude ownership/integration seams are established.
---

# Code Atlas fleet: checkpoint plan

Graph metadata uses the installed registry's `doc` type: its validator rejects `plan`, despite
the reusable planning template suggesting it. The post-write inventory caught that mismatch.
The content remains a plan; no unsupported metadata tag is silently accepted.

## Goal and termination

**Goal:** specify the next Code Atlas addendum, define the overall architecture and implement
the Owner-admitted vertical slices through the AI-Forward gates.
**Done when:** every admitted slice has its stated end-to-end exit evidence and Owner closure;
unbuilt phases and external seam blockers are explicit, never described as completed.
**Not in scope:** modifying TheTerrace, publishing private reference material, replacing the
product runtime conductor by implication, or racing Claude's primary-checkout integrations.
**Tier:** T2. **Concurrent worker cap:** 4 (including decision/review seats).
**Main-line budget:** 50 coordination calls to the first delivery checkpoint, a planning estimate.

## Roles and models

| Role | Model | Mechanism | Authority |
|---|---|---|---|
| Owner | GPT-6 Astra | Separate `owner` agent, read-only tools, own worktree | Rules on scope/trade-offs; never authors code or clears non-delegable hard-floor trips |
| Conductor | GPT-6 Astra | Current CLI main thread, dedicated conductor worktree | Sequences gates, records rulings, dispatches/join workers; does not replace Owner decisions |
| Execution workers | GPT-5.5 where appropriate | Writable general-purpose agents in individually registered worktrees | Author assigned artifacts/code only; no self-acceptance or cross-lane edits |
| Specialist reviewers | Appropriate GPT model | Read-only persona seat, separated from author | Applicable veto/review scope, not automatic feature expansion |

The user explicitly selected this GPT development fleet. A custom persona's tool restrictions
are checked before dispatch: a read-only reviewer is not sent an implementation assignment.
Each writer receives exact cwd, identity, owned files, input contracts, budget and exit predicate.

## Execution graph

| Node | Capability | Inputs | Exit condition | Real dependency |
|---|---|---|---|---|
| F0 Ground and isolate | Deterministic mechanics | Current main, proposal provenance, registers | Distinct trees/identities; current lane state and next addendum checked | none |
| O0 Owner framing | Independent decision | User intent, C/D limits, current ownership | Recorded scoped rulings, or named evidence blocker | F0 |
| S0 Draft specification | Reasoning | Proposal plus governing specs | Full draft Functional/UX/UI layers and unresolved questions | F0; acceptance depends on O0 |
| K0 Contract grounding | Reasoning + deterministic mechanics | Current code/manifests/tests | Cited internal contracts and executed minimal baseline/spikes | F0 |
| S1 Specification gate | Independent review | S0, O0, K0 | Scope/model/UX/UI and verifiability gates cleared | S0, O0, K0 |
| A0 Whole architecture | Reasoning | Accepted spec, contract evidence | Bounded contexts, storage, composition, versioned seams, failure modes, vertical phases | S1 |
| A1 Architecture gate | Independent review | Architecture/ADRs/spikes | Security, data, distributed/async, AI, test and other triggered vetoes resolved | A0 |
| C0 Coordination admission | Reasoning + deterministic mechanics | Architecture, Claude acknowledgment | Canonical section-2 responsibilities and machine-readable track plan | A1; external seam agreement |
| Dn / In Vertical slices | Reasoning + deterministic mechanics | Admitted contracts and exact ownership | Design-slice -> red/green -> native/E2E proof -> independent review | C0; actual per-slice data edges |
| Jn Join and close | Deterministic mechanics + Owner decision | Worker commits/proofs | Integrated slice evidence, clean state, recorded residuals and Owner sign-off | In |

S0 and K0 are independent at draft time: one develops requirements, the other establishes
implementation evidence. Neither chooses an incompatible production signature. They join before
the spec gate. Architecture is not run in parallel with undecided requirements. Implementation
never runs ahead of unsettled shared interfaces.

No runtime-cost forecast is represented as a measurement. Prior prototype work is not a useful
duration baseline for a new native/code-analysis capability. Work/span will be costed at the
architecture-to-coordination checkpoint, once the vertical slices and actual seams are known.

## Current dispatch

| Worker | Worktree / branch | Bounded output |
|---|---|---|
| Owner | `ai-de-owner-code-atlas` / `owner/code-atlas` | Initial scoped rulings; 8 calls; read-only |
| Spec | `ai-de-atlas-specification` / `atlas/specification` | New draft addendum only; 30 calls; no product edits |
| Contracts | `ai-de-atlas-contracts-spike` / `atlas/contracts-spike` | Current contract/baseline proof only; 25 calls; product source unchanged |
| Conductor | `ai-de-conductor-code-atlas` / `conductor/code-atlas` | Provenance, rulings, coordination and joined artifacts |

## Artifact classes and integration

The installed registry declares `docs/audit/*.jsonl` and coordination JSONL logs append-only
registers; derived documentation/index surfaces regenerate rather than merge. Authored files
require a single agreed owner. Inherited merge drivers were observed effective by `coord doctor`.
The doctor reports a shared regeneration-owed marker; this tree does not clear a primary-owned
marker as a side effect. No reinstall/classification reset is run from a linked worktree.

Main integration remains with the Claude conductor until explicitly agreed. Atlas does not
stash, reset or stage the primary checkout. Shared machine-local coordination records may appear
there by design. Existing Shell/Conversation/X1 files remain with their section-2 owners.

## Fan-out and loop contract

- Width <=4; no shared authored output between active workers.
- Transient tool/provider failure: report the failure with its evidence. No silent retry of
  side effects or unbounded relaunch. A retry needs an identified transient cause and bounded backoff.
- Each worker stops at its artifact/claim list, not after “more useful research.”
- Join requires all hard floors for the affected slice. A partial worker result is recorded;
  independent admitted work may continue, but the failed dependent edge stays blocked.
- Review loops drain a named finite blocker list; at most two passes per gate before escalation
  of the remaining blocker to its authority. A cap is an estimate/control finding, not acceptance.
- Replan only at Owner scope, contract, architecture or ownership checkpoints. Do not invent
  new product goals during implementation.

## Floor nodes

Conceptual model and invariants first; typed provenance/snapshot/identity contracts; source
minimization and no implicit model egress; capability/eval admission; failure-mode and telemetry
questions; Testing Strategy trigger union and red-first oracles; E7 full surface reach; native
rendering and cross-surface identity proofs; independent vetoes; durable audit and proof capture.

The future `docs/coordination/code-atlas.md` is the execution driver's canonical track plan.
This checkpoint plan does not pretend that source ownership or implementation phases are already
accepted.
