---
id: spec-ai-native-ide
title: "AI-native IDE — Product specification"
type: spec
status: in-review
owner: "@timianmalloo"
phase: "0"
tags: [ai-native-ide, architecture-visualization, code-knowledge-graph, agent-coordination, prompts]
links:
  - { to: knowledge-hub, rel: refines }
  - { to: seed-ai-native-ide-sketch, rel: relates-to }
  - { to: architecture, rel: relates-to }
  - { to: privacy-review-ai-native-ide, rel: depends-on }
review-by: 2027-02-20
review-suggested: []
summary: >-
  Specifies a local-first AI-native IDE for working across isolated coding-agent sessions while
  understanding the code-derived architecture, domain, data, process, dependencies, knowledge,
  audit history, and coordinated work as linked visual views.
---

# AI-native IDE — Product specification

- **Status:** In review
- **Tier:** T2 — local work data, multi-agent coordination, agent prompt dispatch, and derived
  views cross privacy, integrity, and user-trust boundaries.
- **Seed:** [AI-Native IDE — Architecture Sketch](../knowledge/seed-material/ai-native-ide-architecture-sketch.md)
- **Grounding path:** `spec-ai-native-ide -> knowledge-hub -> {seed-ai-native-ide-sketch,
  code-knowledge-graphs, code-and-infra-extraction, mcp-and-agent-integration,
  multi-agent-coordination, ai-native-ide-shell, diagram-generation,
  domain-modeling-and-erm, microservice-interaction-visualization}`.

## Evidence and framing

### Verified constraints from current project knowledge

1. **Code-derived views, not editable diagrams, are the product boundary.** The knowledge hub
   records that the durable thesis is *code authoritative; diagrams and visualizations are
   derived views*.[^knowledge-hub]
2. **Every extracted relationship needs provenance and confidence.** Static analysis cannot
   resolve all DI, routing, ORM, dynamic infrastructure, or runtime relationships; an extracted
   view must distinguish asserted artifact facts, inferred relationships, and runtime
   observations.[^knowledge-hub]
3. **The graph store is an open decision.** The seed's Kuzu choice is invalidated because the
   project knowledge records Kuzu as archived and finds no direct replacement satisfying every
   original constraint.[^knowledge-hub]
4. **Coordination claims are not locks.** A lease without a fencing token accepted by the resource
   is advisory for efficiency, not mutual exclusion.[^knowledge-hub]
5. **MCP integration must accommodate a stateless current protocol and uneven client hooks.**
   The universal coordination signal is a repository/workspace event path; a particular CLI's
   hook support is an accelerator, not a prerequisite.[^knowledge-hub]

### Candidate comparables and reusable capability sources

| Capability | Candidate / comparable | Evidence and constraint | Confidence |
|---|---|---|---|
| Rich-text prompt composition | [Tiptap](https://github.com/ueberdosis/tiptap) | MIT-licensed, headless editor toolkit with extension points; candidate for rich prompt documents rather than a required implementation choice. | Verified |
| Block-oriented authoring | [BlockNote](https://github.com/TypeCellOS/BlockNote) | Its core and advanced packages have different licences; reuse requires package-level licence review. | Verified |
| Hosted terminal UX | [xterm.js](https://github.com/xtermjs/xterm.js) and [node-pty](https://github.com/microsoft/node-pty) | MIT repositories used as a browser-terminal reference; suitability must be spiked against the selected host. | Verified |
| Interactive graph panes | [React Flow / xyflow](https://github.com/xyflow/xyflow), [AntV G6](https://github.com/antvis/G6) | MIT candidates with different interaction-versus-scale trade-offs; no renderer is selected by this specification. | Verified |
| Generated diagrams | [Mermaid](https://github.com/mermaid-js/mermaid), [D2](https://github.com/terrastruct/d2), [Structurizr DSL](https://github.com/structurizr/dsl) | Existing project knowledge already treats diagram DSL as a projection concern. | Verified |
| Canvas and graph-navigation inspiration | [Obsidian](https://obsidian.md/), [Graphify](https://graphify.com/), [VS Code](https://code.visualstudio.com/), [Eclipse Theia](https://theia-ide.org/) | User-requested interaction references. Obsidian is a knowledge-navigation reference, not a required dependency; Graphify remains an on-device code-graph reference. | Verified (user request / project knowledge) |

[^knowledge-hub]: [`docs/knowledge/index.md`](../knowledge/index.md), “The four findings that
change the plan” and “Cross-cutting design implications,” compiled 2026-08-23.

---

# Part A — Functional specification

## Problem

A developer directing several coding agents across worktrees has no single, trustworthy way to
see how the repository's implemented architecture, domain model, data model, runtime flows,
coordination activity, prompts, audit history, and work backlog relate. Code, infrastructure,
and session artifacts exist, but the human must reconstruct their relationships manually across
terminals and disconnected tools. The product must reduce that reconstruction work without
making visual models a competing source of truth.

## Target users and jobs-to-be-done

| Persona | Job-to-be-done | Constraints |
|---|---|---|
| **Primary: multi-agent technical lead** | When directing concurrent coding sessions, I want to inspect derived architectural and work-state evidence so that I can give precise feedback without re-reading every file or terminal transcript. | Works across repositories and isolated worktrees; needs fast keyboard and terminal access; cannot trust an unlabeled inferred relationship. |
| **System/domain architect** | When assessing a change, I want to traverse domain, data, infrastructure, and flow views from the same source evidence so that I can find cross-boundary consequences. | Needs provenance, confidence, and a path back to source files. |
| **Coding-agent operator** | When preparing work for an agent, I want to compose, retain, and deliberately dispatch a prompt to the intended ready session so that unfinished thinking is not lost or sent to the wrong session. | Prompt dispatch is consequential but must remain user-confirmed. |

**User evidence:** The product owner directly supplied the eight required scenarios and the
multi-session workflow on 2026-08-24. This verifies the primary user’s reconstruction,
coordination, prompt-staging, and feedback needs for this specification. Independent usability
research with additional operators remains **Flagged** and is required before claiming that the
chosen interaction model generalizes beyond the product owner.

## Core scenario

The technical lead opens one local workspace that contains several repositories and active
agent worktrees. They select a domain aggregate, inspect its source provenance, see its persisted
data and service/infrastructure relationships, switch to an observed process flow, identify the
worktree/session currently changing a dependent component, read its audit prompt and response,
refine a staged feedback prompt, and explicitly send that prompt to the intended ready terminal
session. Every visual claim remains traceable to a repo artifact, a runtime observation, or a
clearly labeled human-authored coordination record.

## Scope

### In scope

- A local workspace that groups repositories, worktrees, agent sessions, and derived knowledge.
- Visual, navigable derived views for:
  - logical and as-built architecture from code and infrastructure artifacts;
  - class/domain models, including hierarchy, recognized entity models, aggregate roots, and
    confidence where a stereotype is inferred;
  - entity-relationship models from schema artifacts;
  - data/process flow as sequence and activity diagrams, with runtime observations distinguished
    from static derivations;
  - cross-service, cross-library, and infrastructure dependencies;
  - repository knowledge as an inspectable graph and hierarchy;
  - cross-agent coordination, session/worktree state, and work slices/tasks;
  - audit prompts and responses.
- Multiple terminal and visualization tabs in one workspace experience, plus rich-text prompt
  drafts that are staged until an explicit transfer to a ready session.
- Derived graph updates as repository/worktree artifacts and coordination evidence change.
- Reuse evaluation of existing open-source capabilities before building equivalent features.

### Out of scope

- Replacing a general-purpose source-code editor or owning code editing as a primary workflow.
- Hand-authoring or hand-editing implementation architecture, ERM, class, or flow diagrams.
- Claiming perfect semantic extraction for all languages, frameworks, dynamic DI, or runtime paths.
- Cloud synchronization, multi-user graph mutation, or autonomous cross-repository code changes.
- Treating advisory coordination claims as a correctness lock.
- Selecting a graph database, desktop framework, diagram renderer, terminal component, or extractor
  implementation; those are `/define-architecture` decisions after spikes.

## Conceptual domain model

### Bounded contexts and ubiquitous language

The product deliberately separates authorities instead of putting all durable concepts in one
model:

| Bounded context | Authority and integration language |
|---|---|
| **Workspace Registry** | Defines the local inspection scope and contains repository/worktree membership. It supplies stable workspace identities to all other contexts. |
| **Evidence and Projection** | Receives immutable source or runtime **evidence assertions** and derives relationship **claims** and non-authoritative views. It never creates facts by user editing. |
| **Agent Operations** | Owns terminal-session lifecycle, prompt drafts/revisions, dispatch attempts, and delivery receipts. It publishes session identity and readiness; it does not infer work completion. |
| **Work Coordination** | Owns human work intent and advisory coordination claims. It derives a work-state assessment from evidence; it does not store a second independent completion status. |
| **Audit Reading** | Reads repository-owned audit records through a versioned, privacy-filtered reader. It does not claim an audit record is authentic until its integrity state is known. |

A **workspace** is a local inspection scope. A **derived view** is a non-authoritative visual
projection. An **evidence assertion** is one immutable statement by one extractor or runtime
observer about one normalized relationship at one artifact revision/observation time. A
**relationship claim** groups compatible assertions about the same normalized subject, predicate,
and object. **Provenance** identifies the assertion source. **Evidence origin** is `Static` or
`Runtime`; **verification status** is `Verified`, `Inferred`, or `Unverified`; the two are never
collapsed into one confidence word. A **prompt draft** is user-authored and revisioned content not
yet dispatched. A **work item** is a human-owned work intention. A **work-state assessment** is
derived from coordination evidence. A **coordination claim** is advisory, never an exclusive lock.

### Entities, value objects, aggregates, and history

| Kind | Concept | Definition / invariant |
|---|---|---|
| Aggregate | **Workspace Registry** (root: Workspace) | Contains `RepositoryMembership` and `WorktreeMembership`. **Invariant:** a canonical, trusted-side repository/worktree identity has at most one membership in the workspace; foreign or path-escaped membership is rejected. Other contexts refer to the workspace and membership by identity only. |
| Entity | Repository Membership | A registered repository identity and approved root. |
| Entity | Worktree Membership | A registered worktree identity, repository membership, and branch state. |
| Aggregate | **Relationship Claim** (root: Relationship Claim) | Represents one normalized subject–predicate–object relationship in one workspace. **Invariant:** every displayed claim has one or more compatible, attributable evidence assertions; no assertion means `not recorded`, not a claim. An assertion may have an `Unverified` verification status. Correction creates a superseding assertion rather than overwriting prior evidence. |
| Entity | Evidence Assertion | **Grain:** one extractor/observer assertion about one normalized relation at one artifact revision or observation time. Static and runtime assertions may corroborate or conflict. |
| Value object | Provenance | Artifact/revision, source location, extractor/observer, observation time, and integrity state. |
| Value object | Evidence Origin / Verification Status | Non-overlapping classifications of acquisition and validation. |
| Aggregate | **Agent Session** (root: Agent Session) | Owns the session lifecycle and its current worktree-membership reference. **Invariant:** at one moment, an active session references zero or one active worktree; readiness is an attributed session-generation state, not a name match. |
| Aggregate | **Prompt Draft** (root: Prompt Draft) | Owns immutable revisions and dispatch attempts. **Invariant:** a delivery attempt binds exactly one immutable revision, workspace, target session identity/generation, and user dispatch key. A Prompt Revision’s grain is one saved revision of one draft at one user-recorded moment; correction creates a later revision and preserves the former revision’s identity. |
| Entity | Delivery Receipt | **Grain:** one delivery outcome for one dispatch key. Its outcome is `Acknowledged`, `Rejected`, `TimedOut`, `Failed`, or `NotRecorded`; duplicate use of the same key cannot create a second delivery. |
| Aggregate | **Work Item** (root: Work Item) | Owns the declared slice, dependencies, and assigned identity references. **Invariant:** user intent is distinct from a derived Work-State Assessment; no stored “done” field may override evidence. |
| Entity | Work-State Assessment | **Grain:** one assessment of one work item from a named evidence set at one time. It is `Planned`, `Active`, `Blocked`, `Done`, `Conflicted`, `Stale`, or `Unknown`; `Conflicted`, `Stale`, and `Unknown` override a success-shaped status. |
| Entity | Coordination Claim | **Grain:** one append-only advisory assertion by one session/user about one work-item or resource scope at one recorded time. It identifies author, workspace, evidence basis, validity/expiry, and optional superseded claim; it cannot itself exclude other work. |
| Entity | Audit Entry | **Grain:** one source audit record, read in source order with integrity/redaction state. It is append-only except for access-controlled redaction overlays or retention deletion. |
| Entity | Derived View | A saved query/filter/layout preference. It is never an implementation fact and is rebuildable from its inputs. |

All evidence assertions, prompt revisions, delivery receipts, work-state assessments, and audit
entries are append-only facts. There are no additive business measures in this specification.
Their current representations are derived deterministically from recorded history; corrections and
redactions carry a new attributable event rather than silently rewriting a claim.

### Domain model acceptance criteria

- **Given** a derived node or edge, **when** a user inspects it, **then** the product shall show
  its provenance and confidence, or shall label the fact `not recorded`.
- **Given** a user changes a visual representation, **when** the view is refreshed, **then** no
  hand edit shall persist as an implementation fact; only its saved query, filter, or layout
  preference may persist.
- **Given** a coordination claim is expired or lacks resource-enforced fencing evidence, **when**
  it is displayed, **then** it shall be labeled advisory and shall not prevent another session
  from working.
- **Given** an extractor emits a duplicate assertion for the same source revision, **when** it is
  ingested, **then** the resulting relationship claim shall remain idempotent and retain source
  provenance instead of duplicating its displayed relationship.

## Functional user stories and acceptance criteria

### US-1 — Inspect implemented architecture

**As a technical lead, I want architecture views generated from code and infrastructure artifacts
so that I can understand logical and as-built relationships without drawing a parallel model.**

- **Given** a workspace contains supported code and infrastructure artifacts, **when** I open an
  architecture view, **then** I can filter and expand services, components, libraries, and
  infrastructure relationships and inspect provenance/confidence for each displayed relationship.
- **Given** a source or infrastructure relationship cannot be resolved, **when** the view is
  generated, **then** it is omitted or labeled `Inferred`; it is never displayed as verified.
- **Given** an extracted source artifact changes, **when** its supported extraction completes,
  **then** an affected open derived view reports the artifact revision it represents or explicitly
  reports a stale/failed refresh. An approved fixture manifest defines the expected nodes, edges,
  filter result, and affected-view set.

### US-2 — Understand domain and data models

**As an architect, I want class and ERM views that relate domain concepts to code and data
evidence so that I can identify aggregates, entities, value objects, hierarchy, and persistence
relationships.**

- **Given** a supported codebase has recognized domain stereotypes or configured conventions,
  **when** I open a domain view, **then** I can distinguish aggregate roots, entities, value
  objects, plain types, and class hierarchy with a confidence label for every classification.
- **Given** schema artifacts contain tables, columns, and foreign keys, **when** I open an ERM
  view, **then** I can navigate relationships and inspect the source artifact behind them.
- **Given** an aggregate-to-table link cannot be established, **when** I inspect either side,
  **then** the product shall show it as an unjoined or inferred relationship rather than
  fabricate a link.
- **Given** an approved classification fixture contains positive, negative, ambiguous, and
  unsupported stereotypes, **when** its domain view is generated, **then** every type shall match
  the fixture’s classification or `Unverified` state and no inferred persistence link shall be
  promoted to verified.

### US-3 — Understand process and dependency flow

**As a technical lead, I want sequence/activity views and cross-boundary dependency navigation so
that I can reason about the impact and behavior of a change.**

- **Given** static artifact evidence supports a local process, **when** I select that process,
  **then** I can inspect a generated activity view with source provenance.
- **Given** a recorded runtime trace exists for a named scenario, **when** I open its sequence
  view, **then** runtime-observed calls are visually distinct from static relationships.
- **Given** I select a node, **when** I request its impact, **then** I receive a bounded,
  navigable dependent-neighborhood result or an explicit size-limit response. The response shall
  publish the applied node, edge, and context-byte limits, returned count, omitted count, and
  continuation action; it shall not silently load an unbounded workspace graph into a pane or
  agent context.

### US-4 — Navigate repository knowledge

**As a technical lead, I want graph and hierarchy navigation over repository knowledge so that I
can inspect decisions, terms, specifications, code facts, and their links in one place.**

- **Given** a workspace contains knowledge artifacts, **when** I use graph or hierarchy mode,
  **then** a versioned knowledge fixture defines the expected search, type/repository/confidence
  filters, bounded-neighbor expansion, backlinks, and source-location results.
- **Given** a knowledge node has no source, owner, link, or freshness information, **when** it
  is shown, **then** the missing evidence is visible as a health finding.

### US-5 — Coordinate sessions and worktrees

**As a multi-agent operator, I want to see active sessions, worktrees, claims, and work items on
one board so that I can avoid duplicate work and direct the next slice deliberately.**

- **Given** one or more sessions/worktrees emit coordination evidence, **when** I open the work
  board, **then** I can see each work item’s status, associated session, agent, worktree,
  repository, dependencies, and evidence timestamp.
- **Given** the state is incomplete, stale, or only advisory, **when** it is rendered, **then**
  the board shall show `unknown`, `stale`, or `advisory` rather than a success-shaped status.
- **Given** a user filters by a session or repository, **when** the board updates, **then** it
  shall not mutate task ownership or coordination state merely because the visual grouping
  changed.
- **Given** no evidence, stale evidence, contradictory claims, a failed evidence read, or a
  failed claim write, **when** the work board is displayed, **then** its work-state assessment
  shall respectively be `Unknown`, `Stale`, `Conflicted`, or an explicit failed-write/read state;
  the fixture’s deterministic precedence table is the oracle.

### US-6 — Stage and dispatch prompts deliberately

**As a coding-agent operator, I want rich-text prompt drafts in workspace tabs so that I can
prepare feedback and send the reviewed content to the intended ready CLI session.**

- **Given** I create a prompt draft, **when** I edit, format, or link repository context, **then**
  it remains a draft until I explicitly choose a target session and confirm transfer.
- **Given** I choose a target session, **when** the session is not ready, unavailable, or belongs
  to a different workspace, **then** transfer is blocked with an actionable explanation and the
  draft remains intact.
- **Given** I confirm a transfer to a ready session, **when** dispatch completes, **then** the
  product records the prompt revision, target session, timestamp, and delivery outcome in the
  audit trail without exposing credentials or unrelated prompt content.
- **Given** a session changes generation, becomes unavailable, acknowledges late, or receives a
  repeated delivery request, **when** dispatch is attempted or retried, **then** the product shall
  revalidate the immutable draft-revision/workspace/session-generation binding and return the
  documented idempotent delivery outcome without silently retargeting or duplicating the prompt.

### US-7 — Inspect audit history

**As a technical lead, I want to browse prompts and responses across sessions and worktrees so
that I can recover context and base feedback on what actually happened.**

- **Given** a repository follows the AI-Forward audit directive, **when** I select an audit
  entry, **then** I can inspect its prompt, response/summary, session, artifacts, outcome, and
  available duration without treating a missing measurement as zero.
- **Given** I search or filter audit history, **when** no matching or accessible entry exists,
  **then** the product shows an explicit empty or permission/error state rather than an empty
  success result.
- **Given** an audit entry includes sensitive content excluded by policy, **when** it is rendered,
  **then** the product redacts it according to the repository’s stored redaction state and
  identifies that content is redacted.

### US-8 — Work in one workspace chrome

**As a coding-agent operator, I want terminal tabs and independently configurable visual/prompt
tabs in one workspace so that I can inspect evidence and send contextual feedback without losing
the working session.**

- **Given** I open multiple terminal, view, and prompt tabs, **when** I rearrange or switch
  among them, **then** each tab preserves its declared workspace context and its own
  loading/error/empty state.
- **Given** a terminal session ends or a view’s source becomes unavailable, **when** I return to
  its tab, **then** the tab reports the terminal/view state and offers a non-destructive recovery
  path; it does not silently attach to a different session.

## Verification contract and boundary traceability

This is a **pre-implementation verification plan**, not a claim that code or a Proof Pack already
exists. `/implement` must turn each row into red-first evidence and attach the resulting Proof
Pack; a pre-implementation requirement cannot truthfully claim red-observed execution.

| Requirement area | Fixture / input | Oracle | Boundary cases |
|---|---|---|---|
| US-1 architecture | Versioned supported-artifact corpus and expected graph manifest. | Exact normalized node/edge/provenance set, filter/expand result, and stale/failed refresh state. | Empty, unsupported, changed, malformed, and hostile artifact. |
| US-2 domain/data | Stereotype and schema fixtures. | Expected hierarchy/classification/link state; absent/ambiguous link cannot become verified. | Positive, negative, ambiguous, convention-only, and absent persistence link. |
| US-3 flow/dependency | Static-process fixtures and named trace fixtures. | Origin-specific relation semantics and published bounded-result response. | No trace, trace conflict, over-limit query, async/message path. |
| US-4 knowledge | Graph/frontmatter fixture. | Search/filter/neighborhood results plus required health fields. | Missing source, dangling link, stale, orphan, no match. |
| US-5 coordination | Clock-controlled event/claim fixture. | Work-state precedence and no mutation from a visual filter. | No event, expired claim, contradiction, reader/write failure, concurrent update. |
| US-6 prompt dispatch | Session-state and acknowledgement protocol fixture. | Immutable binding and one delivery receipt per dispatch key. | Busy, wrong workspace, generation change, timeout, reject, duplicate retry. |
| US-7 audit | PII-scrubbed versioned audit fixtures. | Reader state, redaction, duration-unavailable, and access behavior. | Missing, malformed, summary-only, redacted, untrusted, unavailable. |
| US-8 workspace tabs | Stable tab/session identity fixture. | No cross-workspace/session attachment and defined recovery transition. | Ended, replaced, disconnected, stale view, restored layout. |

The refresh performance oracle uses an approved fixture manifest, a documented hardware/OS and
warm/cold setup, at least 30 measurements, start/end events, p95 calculation, and a report of
failures and outliers. No benchmark result is represented as verified before that run.

## Non-functional requirements — ISO/IEC 25010

| Attribute | Requirement |
|---|---|
| Performance efficiency | The first supported vertical slice shall refresh an affected derived view with p95 under 2 seconds after a single-file supported C# change on the agreed reference repository. Larger-graph navigation budgets are **Flagged** until the graph-store spike measures a representative corpus. |
| Reliability | A failed extraction, trace import, terminal connection, or audit read shall preserve the last successfully identified view state and visibly identify it as stale or failed. |
| Security | Workspace-local credentials, terminal tokens, and agent environment secrets shall not be rendered, indexed, written into prompts, or recorded in views/audit surfaces. Prompt dispatch is user-confirmed. |
| Usability | A keyboard-capable primary user shall reach any core visual surface, the work board, audit history, and prompt draft from the workspace navigation without relying on a pointer-only interaction. |
| Compatibility | The initial product is Windows-desktop-first and must preserve repository/worktree isolation. Other platforms are explicitly out of scope until a later spec. |
| Maintainability | Extractors and visual projections shall have independently testable artifact contracts; no view is the authoritative store of its source fact. |
| Portability | Workspace data and saved user layout/query preferences shall have a documented export/recovery path before the product stores irreplaceable user knowledge. |
| Functional suitability | Every displayed relationship shall disclose provenance and confidence; absence of evidence shall remain explicit. |

## Boundary set

- Empty repository, empty knowledge graph, first-run workspace, no audit log, and no active
  session.
- Unsupported language/infrastructure artifact and partially extractable project.
- Deleted, dirty, stale, or inaccessible worktree; active session with no coordination event.
- Large graph query or excessive impact neighborhood.
- Missing, malformed, redacted, or legacy audit entry.
- Prompt draft with a disconnected, busy, wrong-workspace, or terminated target session.
- Concurrent workspace refreshes and contradictory advisory claims.
- Hostile repository content that attempts to influence an agent through names, generated diagrams,
  logs, source comments, or retrieved content.

## Governance lenses

| Lens | Applies | Specification response |
|---|---|---|
| Requirements traceability | Yes | Each user story has falsifiable criteria and maps to Parts B/C flows/screens. |
| Quality attributes | Yes | ISO 25010 requirements and performance budget above. |
| Threat model (STRIDE) | Yes | Terminal, MCP, prompt dispatch, repository content, local service, and visual rendering are trust boundaries; a detailed threat model is required before implementation. |
| Privacy and data governance | Yes | Repository work data, prompts, audit entries, and agent output are minimized, local-first by default, redacted on display where recorded, and never sent to a model/provider without a separately established basis. |
| Accessibility | Yes | Part C requires WCAG 2.2 AA and keyboard access. |
| Performance | Yes | Derived-view refresh target stated; graph-store and rendering targets await measured spikes. |
| Release / rollback | Yes | Workspace metadata and generated projections need a backwards-compatible, exportable migration and recovery path. |
| Observability | Yes | Refresh/extraction, graph-query, prompt-transfer, and view-failure state need source-attributed, privacy-safe telemetry. |
| Supply chain | Yes | Candidate dependency adoption requires licence, maintenance, provenance, and vulnerability review. |
| Incident readiness | Yes | Operators must see stale/failing extractors, disconnected sessions, and failed dispatches without inspecting logs manually. |

### Security and trust-boundary requirements

| Boundary / STRIDE concern | Requirement and falsifiable control |
|---|---|
| Workspace registration and path containment | Registration and every later use shall resolve trusted-side canonical filesystem identity, reject path escape/junction/symlink replacement and foreign-workspace references, and revalidate identity before privileged action. Fixtures shall attempt aliases, links, and TOCTOU replacement. |
| Terminal process → renderer | Terminal output is untrusted data. Non-display terminal control effects, unsafe hyperlinks, clipboard writes, and terminal-originated host actions are disabled or require explicit user initiation; output size/rate limits have an observable overload state. |
| Prompt dispatch | Confirmation and delivery bind immutable draft revision, workspace, target session identity/generation, and dispatch key. Delivery revalidates those values immediately before transfer and records an integrity-verifiable idempotent receipt. |
| MCP server and tool calls | Enrollment is default-deny; each endpoint identity, transport, server, and tool capability is explicitly approved. Tool authorization is evaluated at the tool boundary using the requesting workspace/user scope. Tool output and retrieved content are data, never instructions for a later action. |
| Repository, diagram, rich text, and source rendering | Analysis and rendering do not execute repository content or hooks. Parsers validate format and enforce input/resource limits. Renderers sanitize active content, unsafe URI schemes, scripts, external fetches, and hostile SVG/markup. Negative fixtures must remain inert. |
| Audit evidence | An audit record exposes source, integrity, ordering, access, and redaction state before it is treated as authoritative; redaction occurs before indexing, search, rendering, export, or model attachment. |
| Dependencies | Adoption evidence includes authoritative origin, exact version/lock, licence compatibility, SBOM, and no known-exploitable shipped transitive CVE. |

Every STRIDE threat discovered during `/design` is mitigated by a deterministic control, transferred
to a named human decision, or explicitly accepted with rationale and residual risk. Each
mitigation has a negative test observed red before the implementation is accepted.

### Privacy and data governance requirements

| Data category | Purpose and default | Retention / deletion requirement |
|---|---|---|
| Source-derived facts and provenance | Local workspace inspection only; retain normalized facts and source references, not arbitrary source/terminal text. Unknown classification is denied from prompt/context attachment. | Rebuildable from repositories. Workspace deletion purges indexes, caches, and saved projections, then records a deletion receipt. |
| Terminal output and scrollback | Display only in the live terminal. It is never automatically indexed, attached to prompts, or copied into audit/telemetry. | Ephemeral; discarded when the terminal closes unless the user explicitly exports it. |
| Prompt drafts, revisions, and delivery receipts | User-authored prompt composition and user-confirmed transfer. The product does not silently add graph/audit/terminal content. | Drafts persist locally until explicit deletion; deletion removes local revisions and context attachments. Receipts retain minimum opaque identifiers/outcome needed for audit until workspace deletion or configured retention expiry. |
| Repository audit records | Read-only inspection of an already-authoritative repository audit record, subject to source integrity/redaction state. The product does not make a second full-text copy by default. | Reader indexes only approved minimized metadata. Unclassified/redaction-unknown entries cannot be full-text indexed, exported, or attached to prompts. |
| Runtime traces, coordination claims, and work assessments | Bounded local inspection and debugging. | Retention is named per workspace before capture; deletion propagates to projections, caches, exports, and backups with a recorded outcome. |
| Telemetry | Health and performance only. Allowlisted fields use rotating opaque identifiers; no paths, prompts, responses, source snippets, terminal output, credentials, or direct personal/work identifiers. | Configured finite retention; tests seeded with PII/secrets must prove none reaches logs, traces, metrics, or export. |

The initial product is **egress-deny by default**: it does not invoke a model provider or send
workspace-derived context outside the local device. Launching or transferring a user-authored
prompt to a locally selected agent session is explicit user action only after the session is
classified as `LocalOnly` or approved `ExternalProcessing`. `UnknownProcessing` blocks rich
prompt/context transfer. The linked [privacy review](../security/ai-native-ide-privacy-review.md)
defines workspace-owner authority, repository-policy precedence, retention/deletion/export
rights, LINDDUN dispositions, and source-audit fail-closed behavior. Existing repository audit
logs may themselves contain unsafe historical content; the IDE shall not treat their existence as
a governance basis or remedy.

## AI-integrated allocation

- **Product archetypes:** Long-Horizon Agent support (H) and Tool-Mediated Constructor (C) at
  the agent integration boundary; Grounded Synthesizer (D) only for bounded workspace context
  queries.
- **Tier allocation:** T0 parses, graph queries, routing, diagram projection, provenance,
  dispatch confirmation, and deterministic validation. T1/T2 may rank/search bounded context.
  T3 is optional only for assistant explanation or synthesis, never for extraction truth,
  relationship authorization, diagram source of record, or prompt dispatch.
- **Non-determinism guard:** model output can annotate or propose; deterministic policy and
  explicit user confirmation gate every consequential action.

---

# Part B — UX specification

## Personas and experience qualities

The primary user is an expert operator working under cognitive load with multiple live sessions.
They arrive **investigative and time-constrained**, not exploratory-for-its-own-sake. The
experience must be **dense, not cramped; calm, not passive; powerful, not opaque**. The user needs
parallel reading of evidence and serial entry of commands/prompts.

## Information architecture

| Area | Contents | Primary labels |
|---|---|---|
| Workspace | Repository/worktree/session context and current health. | Workspace, Repositories, Worktrees, Sessions |
| Explore | Derived visual evidence. | Architecture, Domain, Data, Flows, Dependencies, Knowledge |
| Coordinate | Intent and work state across agents. | Work board, Claims, Slices, Conflicts |
| Compose | Human-authored, staged feedback. | Prompt drafts, Context, Target session |
| Audit | Durable history. | Timeline, Prompts, Responses, Artifacts, Decisions |

Each visual node inspection exposes the same evidence order: **what it is → confidence/provenance
→ related nodes → source/artifact location → available actions**. Global search accepts an explicit
scope selector: **Source artifacts** are code/infra/schema facts, **Knowledge** is linked
specifications/terms/notes, and **Audit decisions** are immutable log/decision records. This
protects recognition over recall and prevents a graph from becoming a visual-only dead end.

## User flows

### Inspect an architectural relationship and provide feedback

```mermaid
flowchart TD
  A[Open workspace] --> B{Workspace evidence healthy?}
  B -->|yes| C[Choose Explore view and select node]
  B -->|partial or stale| BS[Show source-specific stale status] --> C
  C --> D[Inspect provenance, confidence and dependencies]
  D --> E{Evidence sufficient?}
  E -->|yes| F[Open related worktree/session and audit context]
  E -->|no| G[Label unknown or request bounded refresh]
  G --> H{Refresh succeeds?}
  H -->|yes| D
  H -->|no| I[Keep last known view and show recovery choices]
  I --> K{Retry, inspect source, or cancel?}
  K -->|retry| G
  K -->|inspect source| L[Open source/provenance inspector] --> D
  K -->|cancel| M([Return to Explore with context preserved])
  F --> J[Create or open prompt draft]
```

### Stage, review, and dispatch a prompt

```mermaid
flowchart TD
  A[Create prompt draft] --> B[Compose rich-text prompt and attach bounded context references]
  B --> C[Review target session]
  C --> D{Target ready and in workspace?}
  D -->|yes| E[Show exact revision and confirm transfer]
  D -->|busy, disconnected, wrong workspace| F[Keep draft and show reason]
  F --> C
  E --> G[Dispatch]
  G --> H{Delivery acknowledged?}
  H -->|yes| I[Record audit receipt and open session]
  H -->|no| J[Keep draft and record failed outcome]
  J --> K{Retry, retarget, or cancel?}
  K -->|retry| C
  K -->|retarget| C
  K -->|cancel| X([Preserve draft and exit])
```

### Coordinate work across worktrees

```mermaid
flowchart TD
  A[Open work board] --> B[Filter by repository, worktree, session, or agent]
  B --> BN{Items match?}
  BN -->|no| BX[Show no-match state and clear or change filter] --> B
  BN -->|yes| C[Inspect work item evidence and dependencies]
  C --> D{State current and authoritative?}
  D -->|yes| E[Use state to plan feedback or next work slice]
  D -->|advisory, stale, unknown| F[Show label and inspect source/audit evidence]
  F --> G{Record new advisory claim?}
  G -->|no| H[Return to board]
  G -->|yes| GI{Write succeeds?}
  GI -->|yes| H
  GI -->|no| GF[Keep prior state and show write failure/retry] --> G
  E --> H[Return to board]
```

### Open a workspace, explore evidence, or recover

```mermaid
flowchart TD
  A[Open workspace] --> B{Membership and local service available?}
  B -->|yes| C{Evidence available?}
  B -->|no| BR[Show contained path/session failure and recovery choices]
  BR -->|retry after user repair| A
  BR -->|cancel| X([Preserve context and exit])
  C -->|yes| D[Search or browse Explore]
  C -->|empty or unsupported| CE[Explain empty/unsupported source and link to source configuration]
  CE --> D
  D --> E[Select node, diagram, source, or knowledge item]
  E --> F{Bounded data available?}
  F -->|yes| G[Inspect source/provenance and navigate related item]
  F -->|over limit or refresh failed| H[Show limits or stale snapshot with retry/cancel]
  H -->|retry succeeds| G
  H -->|cancel| D
```

### Search audit history and recover a tab/session

```mermaid
flowchart TD
  A[Open Audit or a saved tab] --> B{Underlying source/session available?}
  B -->|audit available| C[Filter timeline and select entry]
  B -->|session/view ended or unavailable| R[Show preserved identity, last state, and recovery options]
  R -->|reconnect or reopen succeeds| A
  R -->|cancel| X([Keep layout; do not retarget])
  C --> D{Entry integrity and redaction approved?}
  D -->|yes| E[Show permitted detail and source links]
  D -->|no, redacted, malformed, or inaccessible| F[Show state and safe recovery/filter action]
  F --> C
```

## Wireframe-level structure

- **Workspace shell:** vertical primary navigation; a central tab strip for terminals, visual
  views, and prompts; optional inspector pane; status strip showing workspace/extractor/session
  health.
- **Explore view:** query/search and filters above; graph/diagram canvas in the central region;
  accessible node list/tree alternative; persistent inspector to the side.
- **Work board:** board/list toggle, filters, and a card/list presentation where each work item
  visibly identifies its session, agent, worktree, repository, evidence age, and dependencies.
- **Prompt studio:** tabbed rich-text document, context-reference strip, target-session selector,
  explicit dispatch preview, and delivery status.
- **Audit view:** timeline/list, filters, selected-entry detail with prompt, response/summary,
  artifacts, duration, and redaction/staleness state.

## UX acceptance criteria

- A user can reach an Explore view, a prompt draft, a work-board item, and an audit entry from the
  workspace navigation using the keyboard.
- Every primary flow above has a specified stale, unavailable, error, or recovery branch; no
  unavailable evidence appears as a clean empty result.
- A selected graph node exposes its provenance and confidence before any action that dispatches
  context based on it.
- A work item card/list row identifies its session and worktree without requiring hover-only
  content or visual color alone.
- Prompt transfer requires a readable final revision and explicit user confirmation.

---

# Part C — UI specification

## UI archetype signature

- **Archetype:** **B1 · Keyboard-Velocity GUI** as the workspace shell, with embedded C1 spatial
  graph and B3 telemetry/health views where their job requires them.
- **Signature:** `KeyboardVelocity { Type:OLTP; Arch:SPA; Layout:MasterDetail; Density:Compact;
  Nav:CommandPalette+Sidebar; Viewport:DesktopBound; Input:KeyboardFirst+PrecisionPointer;
  Color:DarkAdaptive; Type:Utilitarian; Depth:Flat; Sync:LocalFirst; Persistence:Session;
  Feedback:Optimistic; Motion:Micro; Pacing:Freeform; Transition:HardCut;
  A11y:WCAG_2.2_AA; }`
- **Selection:** Auto-selected from the dominant JTBD: an expert must rapidly navigate and
  coordinate many sessions and evidence surfaces, so keyboard velocity and compact
  master-detail inspection fit better than a dashboard, a guided flow, or a canvas-only shell.
  Graph and diagram panes retain their own domain-appropriate interaction patterns inside this
  shell rather than forcing every task into one view.
- **Facet deviation:** `Persistence:Session` retains workspace layout/session context; durable
  prompt drafts, audit records, and coordination evidence have their own explicit retention and
  export policies.

## Medium and platform guidelines

- **Primary medium:** Windows desktop workspace application with embedded visual surfaces and
  terminal tabs.
- **Guidelines:** Microsoft Fluent 2 and Windows keyboard/focus conventions for the shell; the
  Windows Terminal and VS Code interaction patterns are comparable references for tab, split,
  terminal, command-palette, and status feedback behavior.
- **Secondary accessibility surface:** every graph/diagram canvas must provide a keyboard-operable
  tree/list and source inspector equivalent.

## Visual intent and design language

- **Intent:** technical, calm, high-signal, and dense without looking like a generic dashboard.
  Opposites: noisy, decorative, cramped, or opaque.
- **Design language:** `DESIGN.md` does not yet exist; `/design` shall produce it before visual
  implementation. It shall define primitive, semantic, and component tokens; light, dark, and
  high-contrast semantic modes; typography; density; motion; and contrast audits.
- **Token discipline:** no raw visual values or copy placeholders in production UI components.
  The design-language inward linter and implementation-facing token/craft control shall be part
  of the implementation proof.

## Key screens and complete states

| Screen | Focal point | Required states |
|---|---|---|
| Workspace / terminals | Current session and its workspace identity | ready, busy, disconnected, ended, recovery available |
| Explore graph/diagram | Selected evidence node and inspector | loading, empty graph, partial/stale graph, extraction failure, node unavailable, bounded-result limit |
| Work board | Work-item state with session/worktree assignment | no work items, advisory/stale state, filter no-match, conflicting claims, update failure |
| Prompt studio | Reviewed prompt revision and target-session confirmation | empty draft, dirty/saved, ready target, busy target, disconnected target, delivery pending, delivered, failed |
| Audit explorer | Selected durable entry and its provenance | loading, empty/no-log, redacted, malformed/legacy entry, source unavailable |

### Component-state and accessibility contract

| Component | States / transitions | Keyboard and accessibility contract |
|---|---|---|
| Navigation, tab strip, panes | default, selected, dirty, loading, disconnected, closed, restored; opening/closing/reordering a tab restores focus to the triggering control or selected pane. | Semantic tab/list roles, current item state, labelled close/reorder controls, predictable focus order, keyboard shortcuts discoverable from a command palette. |
| Graph/diagram canvas and equivalent list | loading, empty, partial, stale, over-limit, selected, unavailable, error/retry; selection and filtering synchronize with the equivalent list. | Pointer-independent node traversal, named nodes/edges/provenance, focusable selection, source-link activation, and no graph-only action. |
| Prompt editor and context references | empty, dirty, validation error, context blocked/removed, ready/busy/disconnected target, review, confirmation, pending, acknowledged, timed-out, rejected, failed. | Semantic rich-text editing surface; labelled context and target controls; confirmation dialog traps focus, states consequence, and restores focus on cancel/result. |
| Terminal | starting, ready, busy, disconnected, ended, output-overload, recovery. | Keyboard-native input; terminal status has accessible non-visual announcement; untrusted output cannot cause host actions. |
| Filters, command palette, work board, and retry controls | default, focus, no-match, loading, stale, failure, retrying, disabled, success, long-content/overflow. | Accessible names, roles, values, live status where changed, target size meeting WCAG 2.2 AA, and no color-only indication. |

## Motion, copy, accessibility, and performance

- **Motion inventory:** tab selection (150ms ease-out), local pane layout changes (200ms
  ease-out), and delivery-status changes (immediate status announcement plus optional 150ms
  indicator) are the only animated moments. Motion never blocks input; reduced-motion substitutes
  are immediate state changes with the same announcement.
- **Copy:** write concise, evidence-first status messages. Examples: `Graph is stale — Bicep
  extraction failed. View last successful snapshot`; `Session is busy — prompt remains staged`;
  `Relationship inferred from naming convention; inspect source`.
- **Accessibility:** meet WCAG 2.2 AA. The future `DESIGN.md` must measure text/non-text contrast
  in light, dark, and high-contrast modes. Automated checks and documented NVDA keyboard traces
  cover focus order/restoration, names/roles/value changes, target sizes, and graph/list
  equivalence.
- **Performance:** meet the Part A p95 refresh requirement. On the approved benchmark corpus,
  initial selected-view render shall be p95 under 2 seconds; node selection/focus response p95
  under 100ms; filter result update p95 under 250ms. Each result reports corpus revision,
  measurement environment, N, cancellation, and degraded-result behavior. Graph and diagrams
  degrade by bounded neighborhoods, filtering, summarization, and explicit size-limit states,
  never by silently omitting relationships.

## AI interaction

- **Applicable HAX guidelines:** G1/G2 (state capability and limits), G7–G11 (invoke, dismiss,
  correct, scope under uncertainty, and explain), G15–G18 (feedback, consequence, global
  control, and change notification).
- **Shape-of-AI patterns:** Wayfinders for prompt-start templates; Tuners for bounded context and
  target-session selection; Governors for dispatch preview/confirmation and pause/retarget;
  Trust builders for provenance, confidence, audit receipts, and explicit AI disclosure.
- **Wrong-answer and unsafe-context path:** users can remove a context reference, revise a draft,
  cancel a staged transfer before confirmation, and view the extraction confidence. The product
  never presents an AI-generated explanation as a verified repository fact.

## UI acceptance criteria

- Every key screen and component implements the applicable state/transition contract above; an
  automated state fixture and dependency-free review harness render each state before
  implementation approval.
- `DESIGN.md` defines primitive, semantic, and component tokens plus measured AA contrast in
  light, dark, and high-contrast modes before UI implementation begins.
- All controls, terminal actions, graph-node operations, and prompt dispatch paths are keyboard
  operable with the declared focus/role/name/value contract and meet WCAG 2.2 AA.
- A graph canvas and its equivalent hierarchy/list expose the same selected-node identity,
  provenance, navigation actions, and result-limit state.
- A user sees a prompt draft’s exact target session identity/generation, workspace, final
  revision, and dispatch consequence before confirmation; the product distinguishes
  `Acknowledged`, observed agent activity, unknown downstream outcome, inferred content, and
  verified repository fact.
- The interface renders `not recorded`, `Inferred`, `Observed`, `stale`, and `redacted` as
  semantically distinct visible states without relying on color alone.

---

## Flagged risks and residual unknowns

| Risk / unknown | Why it matters | Cheapest resolving action |
|---|---|---|
| Graph-store choice | The original Kuzu choice is invalid and candidate stores trade query model, embedding, licence, and .NET support. | Spike representative graph size, query patterns, update latency, export, and licence using two viable candidates. |
| Extraction fidelity | Some architecture/domain relationships are structurally invisible or convention-derived. | Define an extractor confidence contract and measure coverage/false joins on a reference repository. |
| Terminal host control | Terminal stream behavior and accessibility affect the shell’s viability. | Prototype two host/control candidates against an actual agent CLI and screen-reader/keyboard checks. |
| Prompt editor licence and data model | Rich-text candidate packages differ in extension licensing and export semantics. | Spike Tiptap and one higher-level candidate with prompt revisions, context references, and safe plain-text transfer. |
| Audit response representation | Repositories may record summaries rather than full response text, or redact sensitive fields. | Define an audit-reader contract against representative AI-Forward audit logs. |
| Coordination truthfulness | Existing claims cannot guarantee exclusion without resource-level fencing. | Specify advisory UI semantics first; identify each actual write resource that would require fencing before claiming exclusivity. |
| Scale and refresh budget | No representative node/edge count or graph interaction budget has been measured. | Benchmark the Phase 0 extractor corpus before selecting storage or renderer. |
| Personal/work data | Repository files, prompts, audits, and terminal output may contain sensitive data. | Complete STRIDE and privacy review before implementation; establish local retention/redaction and any model egress basis. |

## Confidence ledger

| Claim | Evidence | Disconfirmation / limit | Label |
|---|---|---|---|
| Derived visual views must not become editable implementation truth. | Project knowledge hub’s derived-views conclusion. | User preferences/query layouts may persist, but do not alter Artifact Facts. | Verified |
| Kuzu cannot be adopted merely because the seed selected it. | Knowledge hub records its archival and the failed replacement criteria. | A future store spike can select another behind an interface. | Verified |
| Coordination leases are advisory without fencing. | Project coordination research, summarized by the knowledge hub. | A particular resource may provide fencing; that requires evidence. | Verified |
| Tiptap is a viable rich-text candidate. | Official repository and licence. | Candidate has not been tested against prompt dispatch/revision needs. | Verified for availability; Flagged for fit |
| A keyboard-velocity shell best fits the primary operator job. | JTBD analysis and B1 catalog posture. | User can override the selected archetype after reviewing this spec. | Inferred |

## Gate record

`GATE specify · 2026-08-24 · Test Architect, Data & Persistence Architect, UX Researcher/IA, UX & Accessibility, Security & Identity, Privacy & Data Governance · criteria: conceptual model, functional fixture/oracle matrix, UX recovery flows, accessibility/state contract, STRIDE controls, and LINDDUN-lite review present · verdict: PASS-WITH-CONDITIONS · vetoes→resolution: Data & Persistence PASS; UX Researcher/IA PASS; Security, Privacy, UX & Accessibility, and Test Architect clear the specification gate subject to their named pre-implementation evidence gates.`

---

**Handoff:** `/define-architecture` after resolving the graph-store, extraction-fidelity, terminal-host,
prompt-editor, audit-reader, privacy, and coordination spikes.
