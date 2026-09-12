---
id: adr-01M2BBCCFJ34ZR3ND7HMHP5QA6
title: "PROPOSED — pinned comparison, explicit authority and governed interpretation"
type: adr
status: proposed
owner: "@timianmalloo"
phase: "atlas-proposed-architecture"
tags: [code-atlas, proposed, comparison, authority, privacy, ai]
links:
  - { to: architecture-code-atlas-proposed, rel: refines }
  - { to: spec-addendum-e-code-atlas, rel: refines }
  - { to: spec-addendum-d-compile-step, rel: depends-on }
  - { to: adr-01M2BBCCCDC207J5KQW74WX2M2, rel: depends-on }
  - { to: note-atlas-reference-custody, rel: depends-on }
review-by: 2026-12-12
summary: >-
  Binds comparison to immutable source/clause/decision inputs and keeps carrier, authority,
  origin and review separate. Defines a no-tool abstract interpretation port without selecting a provider.
---

# PROPOSED: interpretation cannot manufacture authority

- **Date / author:** 2026-09-12 / `atlas-architecture-astra`.
- **Deciders:** Owner content choice recorded below; Conductor reports Data/Security/AI content
  passes with future conditions. Final convergence, Privacy/Test admission and normative acceptance remain separate.
- **Native ID provenance:** Conductor-supplied native allocator output after 37-ADR/no-duplicate
  check; registration pending. No global numeric sequence takeover.
- **Status:** PROPOSED; comparison is E-3, interpretation E-4, each separately admitted.

**Owner content choice — “E-4 interpretation admission”:** separate Astra Owner
`61e506c4-2d12-42e9-85cb-153f2f916811`, turn 5, relayed by Conductor: defer until a separate
read-only adapter and processing/eval admission exist. No provider change or source permission.
This is a resolved content choice within an overall PROPOSED architecture.

## Context

Integrated candidate E at `a50329b2` requires implementation-to-intent comparison without compliance scores,
decision authority without carrier confusion and model interpretation without origin laundering.
Canonical architecture §§C/D.2–C/D.5 fixes prompt compilation as its own bounded context.
The requested development GPT fleet is not authorization to change product runtime providers.

## Proposed comparison and authority decision

Persist one immutable assessment for one input vector:
left/right source manifests, clause/document versions and status, applicable scope, accepted
decision references, rule/version, origin and coverage basis. Changed/superseded inputs create a
new assessment or explicit stale/lineage-qualified display, never mutate a past assessment.

Mapping state is exactly `aligned|planned|contrary|unknown|deferred|not-assessed`.
Missing evidence is unknown/not-assessed, not proof of absence. A planned future clause is not
automatically a current-code defect. Rule-driven states are deterministic; model proposals cannot
declare themselves accepted mappings or compliance. Counts preserve denominator/coverage; no percent.

Impact specifies direction (`dependencies` versus `dependents`) and typed predicates. Reversing
comparison swaps additions/removals while retaining edge semantics. Tours are inert ordered
selection references. Export is a separate authorized human action with pinned input/provenance/
bounds, not an automatic side effect of viewing or model processing.

Authority resolution checks trusted actor provenance; root-human authority **or** an accepted
delegation chain rooted in that human; scope; acceptance/effective order; supersession and conflict.
The root human requires no fictional delegation. Unverified names in documents are claims, not
authenticated actors. Audit entries, commits and session messages carry evidence of decisions;
their existence or timestamp does not make them authority.

Keep four fields separate:

| Field | Cannot be substituted by |
|---|---|
| Immutable origin | Current human review disposition. |
| Evidence confidence | Citation syntax or a confident explanation. |
| Review disposition | Source-extracted/observed status. |
| Authority | Timestamp, carrier, model summary or unauthenticated actor label. |

Accepting a model proposal appends a human annotation/decision linked to it. The model-origin
record remains model-origin. Conflicting accepted decisions remain a visible conflict unless an
authorized supersession resolves them; arrival order alone does not choose the winner.

## Proposed E-4 abstract read-only port

`InterpretReadOnly(contextManifest, question, outputSchemaVersion, budget, cancellation)`
returns a typed outcome and receipt. This is a semantic contract, **not a vendor SDK method**.

The context manifest pins files/clauses/decisions, exact hashes/ranges, source manifest, ordered
context identity, redactions/omissions, requesting principal, purpose, processing authorization,
destination/account/model, required residency/retention/training terms, confirmation and budget.
Authorization is checked again at dispatch; changed context/terms requires a new preview/confirmation.
No ambient file/repository/session access and no tools.

Budgets cover input/output bytes/tokens, deadline and call count. Actual subscription quota/spend
semantics come from the admitted provider contract, never an invented price. Missing cost is
“not recorded”; estimates are labelled estimates. Unknown required processing terms block the call.

Outcome variants: proposal, cancelled, unavailable, refused, invalid, quota-exceeded or failed.
Validator checks typed schema, output bounds and citations restricted to previewed evidence.
Valid citations do not prove semantic correctness. Unsupported claims stay labelled proposals.
Tool requests, authority-clearing claims, invented/out-of-preview citations and schema failures
are rejected/quarantined with privacy-minimized diagnostics. Raw rejected text is not retained by
default. Human disposition is a separate authenticated, idempotent audited command.

One bounded invocation per admitted request; no unbounded planner, automatic provider fallback
or retry after an uncertain billable outcome. Future retry/cancellation behavior needs the
adapter's executed idempotency/resource-lifecycle contract. Deterministic views remain available.

## Runtime and privacy boundary

The product headless-Claude/subscription contract remains unchanged. Do not use Addendum D's
compiled envelope or `GovernedRunHost` as a generic analysis API. Select no new provider/SDK/
transport here. A later adapter may reuse an approved runtime capability only after a separate
read-and-run spike proves this no-tool interpretation contract.

Processing authorization is not publication authorization. Local viewing, durable source/body
retention, model egress and export are four distinct decisions. No private reference corpus,
raw source fixture, session excerpt or generated capture enters product artifacts by default.
Reproducibility records use exact context identities and authorized retained inputs; if policy
forbids retaining bodies, record that exact replay needs authorized reacquisition rather than
silently keeping the bodies or claiming replay from hashes.

Retention/deletion basis for model inputs/outputs, rejected text, decisions and evidence must be
accepted before E-4. Provider terms unknown or source outside purpose ⇒ no dispatch.

## Admission and falsifiers

E-3: direct root-human decision, accepted delegation, forged actor label, timestamp-only note,
unaccepted delegation, conflicting accepted scopes, superseded clause, missing history,
reverse comparison/impact, target/current distinction and denied export.

E-4: independently versioned eval corpus against no-model/cheaper baseline; hostile source and
prompt injection; invented/out-of-preview citation; model tool request; model statement clearing
authority; changed preview after consent; output overflow; quota/unavailable; cancellation/deadline;
model/version drift; observed receipt and privacy-safe actual runtime call. Zero accepted security
violations in the admission corpus; independent reviewers set semantic quality thresholds and
report sample-size/generalization limits. A mocked provider test is not provider conformance.

## Alternatives, consequences and LOA

| Alternative | Why rejected |
|---|---|
| Audit/commit/session as authority | Confuses a transport/carrier with trusted accepted scope. |
| Human acceptance upgrades model origin | Rewrites provenance and falsely asserts extracted truth. |
| Compliance percentage | Hides unknown/deferred/target-state scope and denominators. |
| Reuse compiled envelopes/generalize compiler | Crosses C/D context/runtime contract and existing ownership. |
| Frontier agent with tools for analysis | Adds unnecessary nondeterminism and side effects to a read-only task. |

Proposed primary F, optional bounded D; T0 authority/policy/validation, T3 interpretation only
after admission. Patterns: Grounded Context Injector, Schema-Constrained Output, Deterministic
Verifier, Token Budget Throttle, Receipt Ledger, Idempotent Action, Graceful Degradation.
P1–P11 apply as mapped in the architecture; C1–C3/C10 require annotated/budgeted Activity+receipt
model calls; C4/C5/C6/C11 require typed validated principal-carrying command boundaries.
No current conformance or provider execution is claimed.

Cost: context/receipts/eval have measurable latency, volume and retention costs. Bound all of them;
do not save tokens by dropping source limitations or privacy checks. Rollback demotes/disables the
interpretation capability and preserves deterministic Atlas, historical origin and dispositions.
