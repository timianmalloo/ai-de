---
id: proposal-workbench-loops
title: AI-DE — a notebook workbench for work and systems
type: doc
status: exploratory
owner: "@timianmalloo"
phase: proposal
tags: [proposal, desktop, notebook, factory, review, understanding]
links:
  - { to: proposal-local-software-factory, rel: refines }
  - { to: kb-workbench-loops-20261018, rel: depends-on }
  - { to: spec-workbench-loops-ux, rel: relates-to }
  - { to: mockup-factory-workbench-v2, rel: relates-to }
  - { to: architecture, rel: relates-to }
  - { to: built-inventory-rev102, rel: depends-on }
review-by: 2026-11-18
summary: An exploratory local desktop workbench with equally valid work-first and system-first entry, an immersive intelligent editor, peer execution/evaluation, dockable evidence surfaces and explicitly authenticated native harnesses.
review-suggested:
  - { by: architecture, on: 2026-10-18, reason: "2026-10-18 workbench-loops architecture: §WL added, ADR-0039..0045 proposed, Perspective set re-rowed (ADR-0045 supersedes ADR-0017/0030 rows)" }
---

# AI-DE: start with the work, or start with the system

**Exploratory proposal — not an accepted implementation specification.** AI-DE remains a local
Windows desktop workbench. This iteration preserves Factory, Review and Understand, changes the
primary authoring experience, and restores docking as a central product behavior.

[Open the full interactive proposal](local-software-factory-v2-interactive.html).
Try the mockup from [work](../mockups/factory-workbench-v2.html?entry=work),
[the system](../mockups/factory-workbench-v2.html?entry=system), or
[a PR review](../mockups/factory-workbench-v2.html?entry=review).

The recommendation is a **continuous, intelligent notebook beside a first-class execution and
evaluation surface**. Diagrams, code, metadata and checklists dock around that pair. The left
navigation describes where to work; the panes describe what is open. Moving a pane never changes
the identity of the question, draft, run or evidence inside it.

## Two legitimate paths

These are both viable product approaches, not a preferred design versus a deliberately weak
alternative. They solve different arrival conditions. The proposed product supports both over the
same underlying model and remembers the user's last context.

| | Path A — start with work | Path B — start with the system |
|---|---|---|
| Arrival | “I need to change, build or review this.” | “I need to understand what exists or what this PR means.” |
| First object | Intent notebook, task, work item or PR | Repository/PR investigation, question or selected symbol |
| First surface | Editor + execution/evaluation | Question editor + visualizer; execution and checklist alongside |
| Progress | Accepted intent → bounded run → evidence → review decision | Question → scoped answer → inspect relationships → capture uncertainty → next question |
| Structure | Explicit acceptance and ownership make delivery legible | Discovery is allowed before a correct task can be named |
| Strength | Productive when the desired change is already understood | Productive in unfamiliar repositories, ambiguous requirements and review |
| Cost/risk | Premature tasks can encode the wrong problem | Exploration can remain open-ended without a stopping question or scope |
| Control | Revise intent before execution; keep acceptance separate from a plan | Show investigated scope and open questions; explicitly promote selected findings to work |
| Legitimate stopping point | A reviewable change and its proof | A useful explanation, diagram or checklist—even if no code changes |

**Meet the user where they are.** Do not force an “Ask the repo” question through task creation.
Do not require someone with a precise task to tour the whole architecture first. “Promote to work”
is an explicit bridge that preserves the originating question, scope and evidence. A PR can enter
either path: as work requiring a decision, or as a system delta to understand.

## Three loops, one left navigation

| Section | Left-side children | Primary relationship |
|---|---|---|
| Factory — development | Prompt notebook, Work items, Execution, Coordination | Intent and acceptance beside the run that acts on them |
| Review | Pull requests, Review notebook, Test evidence, Rigor backlog | Questions and human judgment beside change/evidence packets |
| Understand | Ask the repo, Ask a PR, Visualizer, Checklists | Question, answer, diagram/code/metadata and open obligations |

Feature destinations do not become right-pane tabs. Document identities may have tabs where the
native dock needs them. A visualizer's Sequence / Components / Data / Code / Metadata selector
changes the representation of the current object; it is not a second navigation system.

## The editor must earn its place beyond CLI

The author works in a document, not in a stack of input boxes. The editing surface fills its pane:
headings, prose, code, inline references and acceptance statements share one reading rhythm. A
margin gutter carries section execution and diagnostics; contextual tools appear near a selection.
Formatting controls do not compete with the text all the time.

| Core capability | Expected experience | What is demonstrated / still needed |
|---|---|---|
| Autocomplete and IntelliSense | `@` files/symbols/requirements, slash skills, variables, Ctrl+Space; keyboard selection and escape | Working fixture suggestions in rich and source projections; production index, ranking and freshness integration needed |
| Inline help | Hover/focus/signature help shows the actual selected identity, documentation, source and baseline | Known and unknown-reference paths; no substitution of a different symbol |
| Diagnostics | Explain ambiguous acceptance, unresolved context, stale evidence or budget mismatch at its location | Measurability hint and explicit correction; production semantic diagnostics need contracts |
| Editing power | Multi-selection, find/replace, folding, structural navigation, undo/redo, code fences, precise caret behavior | Exact multi-edit preview/apply/undo works; true native multi-cursor/IME/clipboard behavior remains qualification work |
| Prompt stack | Back/forward, saved revisions, compare, branch; scratch preserved | Local draft history and immutable snapshots demonstrated |
| Scoped execution | Run the document, a section or selected text; inspect exact serialized input first | Working fixture input preview, revision binding and selection execution |
| Context composition | Insert a reference or selected range with scope and provenance; inspect what will be sent | Inline references and explicit selected context; no silent whole-repository attachment |
| Notebook output | Result remains linked to the submitted revision; edits mark the previous result stale | Working staleness and immutable submitted-input behavior |
| Multimodal context | Attach diagram selection, code range, image/annotation or test output to a question | Diagram/code selection works; file/image/audio ingestion is a subsequent scoped capability, not claimed in the HTML |

Deterministic completions should be immediate and index-backed. Optional model-generated rewrites,
next-edit suggestions and critiques are a distinct layer with preview/accept/reject and their own
latency/usage disclosure. The whole workbench is LLM-enabled, but an ungrounded model guess must not
masquerade as symbol resolution or an execution receipt.

**Editor implementation alternatives:** reuse the existing CodeMirror 6/WebView2 composer as a
canonical Markdown document with rich decorations, widgets and semantic services; or adopt a
schema-governed rich document engine with embedded code editors. Both are real paths. Recommend
CodeMirror first because it is already shipped and preserves source editing, multi-selection and
precise context ranges. Choose the second only if qualification shows that document semantics or
collaborative rich editing cannot be met cleanly. Do not introduce two competing authoritative
documents. The HTML uses bounded contenteditable/source projections to review interaction; it is
not evidence that a production rich engine is complete.

“Superior to CLI” is an acceptance obligation, not a marketing claim. Benchmark: insert and inspect
a symbol reference, revise repeated text safely, compare prompt versions, ask about a diagram edge,
understand a failed run and capture a test gap. The GUI must preserve keyboard speed while reducing
copy/paste, lost context and ambiguous scope. Native typing, IME, clipboard, undo and large-document
budgets must pass before making this the default.

## Execution is a peer, not a console drawer

The execution surface shows **approach summaries when supplied**, concrete tool activity, human
permissions, outputs, evaluations and gaps. It does not fabricate hidden reasoning. “No reasoning
summary supplied” is a valid state. Raw native output remains an inspectable evidence view, not the
primary user interface.

The surface distinguishes preparing, running, waiting for permission, waiting for input, queued,
stop requested, interruption acknowledged, failed, disconnected/outcome unknown, response complete
and evidence ready for review. An exit code is not semantic success. Denied tools, omitted scope
and absent usage remain visible. Closing the pane does not stop the run; Stop does not imply rollback.

Queue adds a later turn without mutating the active input. Steer is an explicit amendment when the
native route supports it. Retry creates a new attempt after inspection; it does not replay work
silently. Reading old output suspends follow-latest rather than moving the user's scroll position.
Changing loops or selecting another harness cannot relabel or reset an existing run.

Evaluation joins intent to observations: criterion → production/test artifact → assertion → test
attempt/environment → evidence → human disposition. A model's useful response can be complete while
the work remains unproven. A checkbox can mean acknowledgment; it cannot manufacture a test pass.

## A shell designed for docking

Reuse AI-DE's WPF, AvalonDock, zone service, controller, rails and per-perspective layout persistence.
The existing `DockHost` already composes them. Keep surface identities independent of placement and
make model lifetimes workspace-owned; do not tie an active run to a pane's visual lifetime.

Default arrangements are **editor + execution**, **execution below**, **focus editor** and
**investigation**. They are starting points, not locked templates. Notebook, execution, visualizer
and checklist can move, resize, float, hide and reopen. Keyboard move/resize is a first-class route.
The HTML floating pane is deliberately an in-window approximation; native auxiliary windows and
mixed-DPI restoration require later proof.

Every content pane carries object identity and scope. Linked selection follows the current
investigation; pinning retains an older run/baseline visibly. Retain layout per loop and restore
focus and caret. Avoid treating navigation, document switching and moving a pane as the same action.

## Review and understanding journeys

**PR review:** open the exact base/head, read the behavioral change, inspect the human decisions,
ask a question in the review notebook, open the cited code/diagram/test evidence beside it, capture
a gap, and record a reasoned disposition. “Did they think of X?” can establish what the implementation
and recorded evidence show; it cannot establish the author's unrecorded intention. A new head makes
old answers and decisions stale. Recording a local disposition is distinct from approving or merging.

**Test review:** inspect assertion relevance, affected considerations, execution attempts, gaps,
redundancy, efficiency and suspected flakiness at PR, component or repository scope. Keep numerator,
denominator, time window and environment. “Not recorded” is not zero coverage; a successful retry
does not erase a failed attempt. Suggest a backlog item rather than deleting tests from a score.

**Understand:** a question is a first-class document. Its answer can open a sequence/component/data
diagram, source contract or metadata view. Selecting a node or code range becomes the next question.
Keep open questions, tasks and risks in a linked checklist. A useful explanation can be the final
outcome, with no work item. Architecture review uses the same loop at system and boundary scales;
runtime evidence must be distinguished from extracted or inferred source relationships.

**Rigor:** proposed resilience, compliance, privacy, performance and security findings carry scope,
evidence, impact, uncertainty, owner and disposition. A finding becomes accepted work only through
an explicit decision. Contradictory evidence remains attached rather than being hidden by a summary.

## Existing subscriptions, explicit native sign-in

This is grounded in the working `x-harness-x-model-bench` implementation at **`8f514607`**, not a new
credential-broker design. AI-DE already defines subscription credentials as engine-owned.

The flow is **choose native harness → check its existing native session → explicitly open its sign-in
only when needed → qualify the selected headless profile → run**. AI-DE does not transparently negotiate subscription
auth, offer a new OAuth identity, request API keys or extract a native token for a generic model
client. Where a harness uses a browser/device/terminal sign-in, the user sees that harness-owned flow.

| Choice | Preferred product route | Honest qualification boundary |
|---|---|---|
| Anthropic / Claude | Native Claude Code headless using native login or operator-issued setup-token | Respect credential refresh and distribution policy; SDK third-party login rules are separate from this native route |
| OpenAI / Codex | Native ChatGPT sign-in; local app-server or qualified native adapter | Verify model/profile access; no new app-owned OAuth flow required |
| Microsoft / GitHub Copilot | Native Copilot CLI and credential store; qualified programmatic/ACP route | GitHub entitlement and organization policy; consumer Copilot is distinct |
| Meta / Muse | Native Muse Code onboarding and exec JSON | New benchmark profile required; serve/MSP and subscription billing through it are separate qualification |
| Grok | Native Grok login; qualified ACP/headless profile | Respect credential-home concurrency and measured optional capabilities |
| Google / Antigravity | Native `agy` keyring sign-in and stream-json | Not Gemini CLI; policy-driven headless permissions are not an interactive approval protocol |

Reuse benchmark environment sanitation so an inherited API key cannot silently change the requested
billing route. Respect native credential-store and refresh semantics; never archive copied secrets.
Reuse native-home concurrency controls. Do **not** import benchmark always-approve/trust policies as
workbench defaults. Keep installed, signed in, account/plan known, model available, quota known and
headless-ready as separate observations. Auth failure, quota pressure, unsupported capability and
permission denial have different recoveries. The UI offers a deliberate route change, never hidden
API billing or an unannounced model fallback.

ACP is useful where qualified; it is not a requirement to erase native capabilities. Codex RPC,
Copilot programmatic/ACP, Grok ACP, Antigravity stream-json and Muse exec can project into a common
event model while retaining the raw source, version and unsupported states. The
[research note](../knowledge/workbench-loops-20261018.md) gives official sources and precise limits.

## Bring current AI-Forward coordination into the workbench

The source is pinned to **`83a7834`**, revision **112**. AI-DE's manifest says 102, but its runner,
board, decision helper and coordination doctrine already match source 112 byte-for-byte. The primary
gap is a product projection and control integration—not simply copying ten pack revisions.

Support native sub-agent fleets, cross-harness spawned workers and independently registered
sessions. Show relationship and harness separately. An Owner is a model seat; the human supplies
consent. A Coordinator decomposes, dispatches and joins. A registered peer is not an owned process.

The coordination workspace should reveal:

- the designated leader/epoch and observation freshness;
- a task graph with the actual five-part contract, allowed tools, artifacts, budgets and termination;
- scoped claims and seams, without pretending leases are correctness locks;
- requests, exact-revision acknowledgments, overdue decisions and numbered rulings;
- presence, meaningful progress and task outcome as separate dimensions;
- transport completion, evidence presence, review readiness, acceptance and integration as separate stages;
- interrupted attempts and concrete recovery actions, without silent replay, reassignment or cleanup.

Reuse pack refs, request/mail/ruling stores and qualified runner controls as their authorities.
Do not create a disconnected C# coordination database. Preserve source identities, epochs,
sequence and qualification fingerprints through a read model. AI-DE's `ParentAgentId` is currently
unpopulated and its watcher uses heartbeat age; neither proves a functioning delegation/progress
graph. The source names `coord delegate` but does not implement that command: use the actual runner
manifest contract. Preserve the repo's enhanced durable join recovery during any future pack update.

## Terminal's retained role

Retire terminal/CLI prompting as the default navigation and authoring model. “Open repo terminal”
remains explicit and creates a hosted native CLI at the chosen repository/worktree path. It is useful
for direct tools, diagnostics and native sign-in; it does not replace the notebook or silently grant
an agent execution permission. Raw console output is available as evidence. Existing terminal code
can remain as this secondary capability while the main workflow migrates.

## Shared model and durable representation

Notebook, run, assessment, layout and harness binding have separate invariants. Core identities are
dimensions; revisions, attempts, permission decisions and evidence observations are append-only facts.
One revision row is exactly one immutable document revision; one run row is exactly one attempt
against one pinned input and route; one evidence-link row connects one exact claim/criterion revision
to one artifact/attempt; one decision row records one actor's disposition at one baseline.

Use revision history when an attribute change would rewrite past meaning. Derive readiness and
coverage rather than storing competing definitions. Counts are additive only for disjoint populations;
rates retain numerator/denominator; parallel durations are not additive wall time. Provider usage and
cost remain Not recorded unless actually emitted. The next component design must bind these objects
to existing store and wire contracts before implementing additional panes.

## How to get there from here

| Slice | Reuse | Build / change | Exit evidence |
|---|---|---|---|
| 0. Stabilize evidence foundations | Existing forensic inventory and focused probes | Resolve known freshness/framing findings before relying on large-scale automated assurance | Failure-specific regression evidence, not a full-suite count alone |
| 1. Desktop composition | DockHost, zone services, perspective registry, layout persistence | Left feature hierarchy; editor/run/visualizer/checklist surface identities; two entry paths; preserve old layout migration | Native focus, close/reopen, float/redock and mixed-DPI restoration with live run/draft |
| 2. Intelligent authoring | CodeMirror 6/WebView2 composer, draft and host-owned prepare/send | Versioned notebook, semantic completion sources, inline help/diagnostics, selected scope, history and canonical request | IME/clipboard/multi-selection/undo, language/whitespace round-trip, 100k-character input and stale-prepare refusal |
| 3. Native profile bootstrap | ProviderRegistry, FirstUse, benchmark profile/credential isolation patterns | Explicit native sign-in, environment sanitation, exact-profile readiness, account/model/capability observations; qualify Muse | Six separate capability records, no inherited API-key surprise, auth/refresh/concurrency failure fixtures |
| 4. Execution and evaluation | GovernedRunHost, RunEventRelay, session/read models, cancellation/queue primitives | Peer execution surface, immutable attempt identity, native permissions, partial/disconnect/stop acknowledgment, evidence evaluation | Provider-specific stream, cancellation, permission and unknown-event tests; no competing stream reader |
| 5. Review and understanding | Source viewer, diagrams, graph/document knowledge, assurance research | Repo/PR question notebooks, linked visualizers, test-worthiness and rigor checklist, explicit promotion | End-to-end question→evidence→gap→decision with stale-head invalidation and unknown fallback |
| 6. Coordination projection, then commands | Current pack runner/board/decide, existing lease/seam and durable join evidence | Read-only graph and requests first; then qualified dispatch, permissions, enqueue/finish/cancel and join previews | Native-child/cross-harness/registered cases, stale ACK/leader epoch/stalled progress/interrupted join proof |

Each slice is a vertical migration, not a second workbench running beside the first. Introduce the
new surface behind the existing host; preserve draft/run identity and migrate layout by stable IDs.
Keep Console as a secondary pane. A renderer message cannot become an uncontrolled execution path.

## What this iteration proves—and leaves for implementation

The HTML is self-contained and contains working local interactions, including both entry paths,
left navigation, rich/source editing, completion and help, revision/branch/compare, multi-edit,
request preview, run/permission/queue/stop fixtures, diagrams, checklist, docking and native-login
simulation. It makes no real provider call, repository edit, login or approval. All example PRs,
metrics and runs are labeled fixtures.

The [functional evolution specification](../specs/workbench-loops-ux.md), [review](../reviews/ui-factory-workbench-v2.md) and
[proof](../proof/workbench-loops-20261018.md) retain the interaction checks and limitations. Native
UIA/IME/DPI, actual providers, complete rich-editor semantics, large repository indexing, package
trust and production coordination remain implementation work.

## Decisions settled and specification handoff

Settled by the user: local desktop; dockable shell; Factory/Review/Understand; first-class intelligent
editor and execution; left feature navigation; optional hosted terminal; explicit native subscription
sign-in; both work-first and system-first paths. These are not reopened as permission questions.

The [functional specification](../specs/workbench-loops-ux.html) now formalizes this proposal as
30 user stories and 90 acceptance criteria, with distinct work and system journeys and explicit
bridges. It defines document interoperability, authority and migration obligations; exact native
versions and release-policy decisions remain visibly qualified dependencies. They do not silently
default to broad execution authority or a lighter human review policy.

The next step is `/define-architecture` for journey/notebook/attempt/assurance and coordination
boundaries, followed by `/design-slice` for the **notebook/run identity seam**, native profile
bootstrap and editor qualification. This proposal remains exploratory; the specification governs
future implementation behavior, and the mockup remains a bounded illustration of the direction.
