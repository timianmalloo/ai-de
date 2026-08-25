---
id: privacy-review-ai-native-ide
title: "AI-native IDE — Privacy and data-governance review"
type: privacy-review
status: in-review
owner: "@timianmalloo"
phase: "0"
tags: [privacy, linddun, work-data, prompts, audit]
links:
  - { to: spec-ai-native-ide, rel: documents }
  - { to: knowledge-hub, rel: relates-to }
review-by: 2027-02-20
review-suggested: []
summary: >-
  Defines the privacy posture for local AI-IDE workspace data: data inventory, purpose,
  retention, deletion, indirect model egress, and LINDDUN-lite dispositions. It is a
  pre-implementation gate for the AI-native IDE specification.
---

# AI-native IDE — Privacy and data-governance review

## Scope and accountable roles

- **Workspace owner:** the signed-in local OS user who registers a repository/worktree and affirms
  authority to inspect its work data. The product must not infer this authority from a path alone.
- **Repository policy:** a repository’s stricter data-handling policy overrides workspace defaults.
  If a required policy is absent or unknown, the product treats derived content as sensitive and
  denies external/context attachment by default.
- **Product purpose:** local workspace inspection, user-directed agent coordination, user-directed
  prompt composition, and read-only inspection of eligible audit records. The product may not
  repurpose terminal, audit, source, or coordination data for model training, unrelated analytics,
  or automatic cross-session context.
- **Regulatory role:** controller/processor and workforce-notice obligations are **Flagged** for
  accountable human/legal confirmation before distribution outside the workspace owner’s personal
  use. Until that determination exists, external rich-context transfer is not a supported product
  capability.

## Data inventory, minimization, retention, and rights

| Category | Purpose / minimization | Retention and rights path |
|---|---|---|
| Repository-derived assertions and provenance | Derived visual inspection. Store normalized relationship metadata and source references only; do not retain arbitrary source bodies. | Rebuildable. Workspace owner can export the visible assertion set or delete the workspace; deletion purges indexes, caches, layouts, and derived exports, then emits a deletion receipt. |
| Terminal output | Live display only. Never automatic graph, audit, prompt, or telemetry input. | Ephemeral on terminal close unless explicitly exported. The owner can clear an active terminal buffer; export is visible and user initiated. |
| Prompt drafts/revisions and delivery receipts | User-directed composition and dispatch. Context requires explicit attachment and preview. | Owner can view, export, edit, or delete drafts. Deletion removes drafts/revisions and local attachments; receipt retention is minimum opaque outcome metadata only. |
| Audit records | Read-only, source-owned audit inspection. No second full-text copy by default. | Reader index contains only approved minimum metadata. The user can export/delete the reader cache; source-log remediation remains the repository owner’s action. |
| Runtime traces, work items, coordination claims | Named debugging/coordination evidence only. | Workspace policy declares capture duration before recording. Owner deletion propagates to projections, caches, exports, and backups with an auditable result. |
| Telemetry | Health/performance only using allowlisted opaque identifiers. | Finite configured retention. No prompts, response text, source snippets, terminal text, paths, secrets, or direct work/personal identifiers. |

**Rights mechanism:** the workspace owner can request view, export, correction (by a superseding
source/user event), and deletion from the Workspace privacy controls. A request reports every
affected local projection/cache/export and its result. A request that reaches repository history,
an external agent provider, or a third-party backup is not silently claimed complete; it creates a
human-owned remediation/escalation record.

### Classification and retention defaults

Every persisted field is classified `Public`, `Internal`, `Sensitive`, or `Restricted`; unknown is
`Restricted` and denied from indexing, export, and context attachment. Source paths, symbols,
ticket-like values, actor names, audit metadata, trace attributes, coordination author fields, and
export columns have per-category allowlists. Raw source and terminal bodies are never allowed
fields.

| Category | Default / maximum retention | Purge |
|---|---|---|
| Rebuildable evidence assertions | 180 / 365 days | Daily expiry; current graph rebuilds from repository artifacts. |
| Prompt drafts and command receipts | 90 / 365 days | Daily expiry plus owner deletion. |
| Coordination facts | 90 / 365 days | Daily expiry plus owner deletion. |
| Named trace observations | 30 / 90 days | Daily expiry plus owner deletion. |
| Audit-reader metadata cache | 30 / 90 days | Daily expiry; no full-text cache. |
| DPAPI local snapshots | 7 daily copies / 30 days | Rotation purges expired snapshot and WAL copies. |
| Telemetry | Local-only, 30 / 90 days | Daily expiry; no remote exporter in v1. |

Workspace deletion immediately prevents new writes, starts an idempotent purge across facts,
projections, exports, WAL, snapshots, and telemetry, and returns an incomplete result for
repository-owned or external copies.

## Agent-session and egress classification

Each target session declares exactly one data-processing class before prompt transfer:

| Class | Meaning | Transfer rule |
|---|---|---|
| `LocalOnly` | Prompt is processed on the device and does not invoke an external provider. | Permitted after normal workspace/session/revision confirmation. |
| `ExternalProcessing` | The session may send prompt content to an external provider. | Future capability only. Version 1 blocks rich transfer; a later UI must display configured provider, model, residency, retention/training posture, and repository-policy approval before transfer. |
| `UnknownProcessing` | The product cannot establish the session’s downstream processing posture. | Rich prompt/context transfer is blocked. The user may open the terminal and work there without the IDE silently injecting context. |

The initial product remains **direct-egress deny by default**. It does not call model providers or
attach derived data to a model. Version 1 supports `LocalOnly` rich prompt transfer only. A user
may independently operate an externally processing terminal, but AI-DE does not inject rich
workspace context into it. External-processing configuration is a future governed capability, not
a property inferred from an executable name. It requires a human-approved provider record covering
purpose/basis, permitted data classes, processor/subprocessor role, residency/transfer mechanism,
training posture, retention, deletion/rights path, and repository-policy authorization. Its
processing attestation must be trusted, revalidated immediately before transfer, and invalidated on
session generation, executable, or relevant configuration change. An unverifiable or stale
attestation is downgraded to `UnknownProcessing` and fails closed.

## Audit-source safety

Audit records are classified before indexing or display as `Safe`, `Redacted`, `Unsafe`, or
`Unknown`.

- `Safe`: approved metadata/detail may be displayed within workspace access scope.
- `Redacted`: only redacted detail may be rendered; indexing, export, and attachment use the
  redacted representation.
- `Unsafe` or `Unknown`: show only minimum entry metadata and a clear warning. Suppress prompt,
  response, source snippet, and full-text search/export/context attachment.

The product records a remediation/escalation reference for unsafe historical content. A display
overlay never claims to remove the source record from repository history.

## LINDDUN-lite disposition

| Flow | LINDDUN concern | Required disposition and proof |
|---|---|---|
| Repository/worktree → extractor → graph/view | Linkability, Identifiability, Disclosure | Scope facts to workspace, use opaque telemetry IDs, exclude raw source text, validate/redact before persistence, and test seeded PII/secrets do not reach graph/view telemetry. |
| Terminal process → terminal view | Disclosure, Detectability, Unawareness | Keep output ephemeral and visibly local; do not capture/index it automatically; require explicit export and test terminal text never becomes a prompt/graph/audit record. |
| Prompt draft → target agent session | Disclosure, Unawareness, Non-compliance | Bind revision/session and show a processing-class/provider preview. Default-deny unknown/external policy; test blocked and approved transfer paths. |
| Repository audit → audit reader | Disclosure, Non-repudiation, Unawareness | Classify source integrity/redaction before indexing/rendering; fail closed on unsafe/unknown content; test safe/redacted/unsafe/unknown fixtures. |
| Coordination and workboard evidence | Linkability, Identifiability, Non-repudiation | Restrict to workspace purpose; distinguish user intent from derived state; expose author/evidence time only to authorized workspace viewers; test deletion/export and stale state. |
| Telemetry/export/backup | Disclosure, Detectability, Non-compliance | Allowlist opaque fields, finite retention, export preview, and deletion receipt. Test no PII/secrets in telemetry and report any external/back-up deletion limitation. |
| User controls and policy | Unawareness, Non-compliance | Explain sensitive-data state, processing class, retention, and deletion result in the UI. Block external transfer when policy, residency, or purpose is unknown. |

## Privacy verification gate

Before implementation approval:

1. Complete a component-level LINDDUN review and link its findings to the relevant design.
2. Provide PII/secret-seeded negative fixtures for graph ingestion, audit reading, prompt context,
   terminal capture, export, and telemetry; observe each control fail before the fix.
3. Demonstrate workspace export/deletion and derived-data/cache purge against a controlled
   workspace; record any repository-history or third-party limitation as incomplete.
4. Before enabling the future `ExternalProcessing` capability, verify the
   provider/model/residency/training-retention record and repository-policy approval; unknown
   fields fail closed.

## Residual risks

- Applicable workforce/privacy law and controller/processor roles need accountable human review.
- Existing repository audit history may already contain unsafe content outside the IDE’s deletion
  reach.
- A local agent can later change its downstream provider behavior; its declared processing class
  must be treated as an attestable, revalidated contract, not a permanent fact.
