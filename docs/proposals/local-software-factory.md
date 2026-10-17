---
id: proposal-local-software-factory
title: "AI-DE: a local, spec-first software factory"
type: doc
status: exploratory
owner: "@timianmalloo"
phase: "proposal"
tags: [proposal, desktop, factory, spec-first, assurance]
links:
  - { to: forensic-review-rev102, rel: depends-on }
  - { to: built-inventory-rev102, rel: depends-on }
  - { to: kb-spec-first-software-factory, rel: depends-on }
  - { to: mockup-factory-workbench, rel: relates-to }
  - { to: architecture, rel: relates-to }
  - { to: spec-ai-native-ide, rel: refines }
review-by: 2026-11-17
summary: "Exploratory evolution of AI-DE into a local desktop spec-first factory: work-centered delivery, scoped understanding and guided assurance over one versioned evidence model."
review-suggested: []
---

# AI-DE: understand the work, review the decisions

**Exploratory proposal, not an accepted specification.** The user's platform decision is settled:
AI-DE remains a **local desktop workbench**. The HTML is an interactive review artifact; it is not
a proposal to replace the product with a hosted website. No production change is made in this work.

[Open the interactive proposal](local-software-factory-interactive.html) or
[launch the working desktop mockup](../mockups/factory-workbench.html).

## Opportunity and recommendation

AI-DE already has a local daemon, durable fact store, graph/tree exploration, static UML views,
governed agent conversations, compilation history, dispatch receipts and watcher evidence.
Its primary orientation is sessions and perspectives; the requested factory needs a durable
connection from **accepted intent → implementation → assertion → executed proof → human decision**.
This is an evidence operating model, not another chat box or a more elaborate terminal.

**Recommendation, Inferred:** make accepted work items the default focus. Offer three stable
destinations: **Factory**, **Review**, **Understand**. Put PR/test/rigor lenses inside Review;
graph/architecture lenses inside Understand. Share project, scope, selection and the evidence drawer.
Keep conversations, code, full diffs and terminal output one deliberate drill-down away. Six user
jobs do not need six independent applications or six competing dashboards.

## Evidence and limits

**Verified local baseline:** 49 capability rows are inventoried; the normal App build produces
all four primary assemblies with no warnings/errors; 57 selected Core tests pass. Two focused
probes reproduce stale same-revision indexing and a response that exceeds its frame cap. The
forensic backlog contains 11 proposed findings, including those two P1 issues. This is not a
whole-test-suite, native accessibility, package release or provider-qualification sign-off.
See [review](../reviews/forensic-review-rev102.md),
[inventory](../reviews/built-inventory-rev102.md), and
[proof](../proof/factory-evolution-20261017.md).

**Verified primary-source patterns:** plans and artifacts, goal supervision, semantic walkthroughs,
review snapshots and progressive detail are now documented by the named vendors. Their hosting
models and quality claims are not automatically transferable. The new Claude project coordinator
and ultrareview are cloud-scoped; Grok's plan edit gate does not cover shell writes/subagents.
Current Copilot preview approvals differ from Anthropic's advisory review service.
[Research and 42-source ledger](../knowledge/spec-first-software-factory/index.md).

**Flagged:** no universal confidence, lines-changed or coverage threshold proves that a human
can safely be removed. Start conservatively and calibrate by local escaped defects, reversibility,
reviewer decisions and cost. The mockup's metrics, work items and PR are visibly synthetic.

## Audience and core scenario

The primary user is a technically expert owner or lead directing agents across a repository.
They need to know what changed, what is proven, what is risky and where their judgment matters.
A reviewer needs a smaller evidence packet without losing the ability to inspect everything.
An investigator needs scoped graph/UML/code navigation without a global hairball.

Core scenario: open a work item; review its accepted obligations and architecture constraints;
inspect the proposed execution plan; deliberately authorize work; receive a behavior-based change
summary; review only the decisions requiring human judgment while retaining full-diff access;
inspect test and drift evidence; accept or reject the exact change baseline with reasons.
New content or changed intent makes relevant evidence and decisions stale.

## Two real alternatives

| Decision | A. Work-item factory | B. Graph-centered evidence workbench |
|---|---|---|
| Primary object | Accepted outcome and change | System entity, dependency or question |
| Home | Work queue and human attention | Scoped repository map and search |
| Path | Intent → plan → run → review evidence → decide | Select context → examine code/spec/tests/risks → initiate/review work |
| Sessions | Child execution history of work | Contextual agent panel beside selection |
| Best fit | Spec-driven delivery and progress | Codebase understanding and architectural investigation |
| Reuse | Conversation, compiler, envelopes, receipts, watcher | Graph/tree, node menus, projections, diagrams, source viewer |
| New core | Versioned obligations and review decisions | Cross-domain joins and reliable trace queries |
| Risk | Workflow ceremony and false completion | Navigation load and graph spectacle |

Recommendation: A is the normal starting point, while B remains the full **Understand** destination
over the same identities and evidence. This preserves a distinct investigation path rather than
forcing every exploratory question into a delivery checklist. Keeping the existing session-centric
shell with added independent panels is the cheaper alternative, but leaves attention and evidence
fragmented; it is an incremental fallback, not the recommended end state.

## The shared model comes before screens

Proposed contexts: Intent, Delivery, Assurance and Decisions. Reference existing workspace/code
identities instead of cloning the fact graph. A WorkItem owns accepted intent at an exact SpecRevision;
assessments and decisions are separate small aggregates pinned to their own baseline. The invariant
is that accepted completion names the required evidence and exact obligation meaning, not an agent's
statement that it finished. Prior assessments and decision events remain append-only.

| Record grain | Durable meaning | Derived presentation |
|---|---|---|
| Criterion revision | One obligation meaning at one approved content version | Applicable/changed/ambiguous |
| Change set | One repository/base/head/dirty-content comparison | Changed behavior and affected scope |
| Evidence link | One producer's relation among versioned obligation, symbol, test or run | Trace view; never proof from link presence alone |
| Test attempt | One execution id, test id, attempt and environment | First-pass/retry-recovered/failure history |
| Assessment | One analyzer's inputs, output, inspected/omitted scope and failures | Review packet and incomplete/stale status |
| Decision event | One actor's disposition at a pinned policy/spec/change baseline | Outstanding human decisions and acceptance |

History rule: changing criterion meaning creates a new revision. Moving source changes artifact
identity; a changed relevant head/spec/policy invalidates the decision. Counts are additive only
over disjoint populations; rates and parallel wall times are non-additive. Never store a second
definition of status, coverage or remaining work. Proposed schema details require `/specify` and
Data & Persistence review before implementation.

## Journey 1 — Spec-driven development and progress

Factory opens an attention list, not a wall of progress bars: unresolved intent, missing proof,
changed contracts and work ready for review. Select an outcome to see its **Spec**, **Plan**,
**Runs** and **Evidence**. Acceptance criteria show expected behavior, its authority and proof.
An empty proof row says “No evidence linked”; it does not silently count as incomplete code or
as a passing criterion. Derived progress separates built, executed, reviewed and accepted.

Accepting a plan is distinct from authorizing tools, and both differ from accepting a change.
A run can be finished while its outcome remains blocked by missing evidence or a human decision.
The conversation and Console remain contextual children of that work item, with existing low-level
controls available for experienced operators. AI-Forward's knowledge/spec/architecture/design/
implementation/proof sequence becomes a visible chain of artifacts, not mandatory modal ceremony
for every trivial change. Existing proportionality and veto floors remain.

### A document-style authoring studio and a real prompt stack

The author's normal surface should be a **prompt document**, not a terminal input line. The user's
addition explicitly asks for rich text, multi-edit-style editing, a stack to move backward/forward
through earlier prompts, and a credible migration from today's implementation. These remain inside
Factory and the selected work item; they do not add another top-level application.

Distinguish three editing capabilities: **multi-cursor text editing** within a block; **structured
blocks** for goal, criteria, constraints, examples and context; and **scoped batch edits** with a
match preview and undo. They solve different tasks. The prototype implements rich bold/emphasis,
acceptance/code insertion, keyboard-reachable block movement, previewed exact-match replacement and
an undoable editor operation. It does not claim a full CodeMirror multi-cursor implementation.

The **prompt stack** is not text undo or the execution transcript. Back/Forward preserves the
current scratch copy; historical submissions remain immutable; reusing or branching an earlier
prompt creates a new draft identity. Show title, source work item, status, parent and position.
Compare earlier intent, constraints and context against the current canonical text. Navigation never
sends, replays an uncertain delivery or silently changes the target provider/worktree.

Keep one editable source model with two projections: rich document/text-block editing and the
exact canonical Markdown/typed payload that will be sent. A context chip may identify a spec clause,
graph node, diagram, code range, test run or prior answer at an exact version. **Mention, attach and
grant write scope are distinct.** Do not expand a reference into private source or grant permissions
because it appears in a rich editor. Prepared output becomes stale after material edits or changed
context. The existing host-owned compiler/send gate remains authoritative.

| Richer capability | Interaction | Guardrail / reason |
|---|---|---|
| Typed blocks | Edit/reorder goal, criteria, constraints, examples and context; keyboard move controls | Stable block identity and deterministic serialization; no parallel editable truth |
| Prompt stack | Back/Forward, save revision, compare and branch | Preserve unsent draft; never resend through navigation |
| Multi-cursor mode | Optional repeated edits within a text block | Qualify actual pinned CodeMirror configuration and accessibility separately |
| Multi-edit | Preview exact match count and selected scope, apply once, undo | Stale preview refuses; changing write-scope mentions invalidates Prepare |
| Context tray | Pin a criterion, node, source range, diagram or prior answer | Show version, provenance, inclusion and privacy before expansion |
| Authoring checks | Missing success criteria, contradictions, unresolved/stale references | Advisory findings plus deterministic validation; no model “prompt quality” approval score |
| Reviewed recipes | Parameterized structures for review/investigation/implementation | Repository text cannot gain host execution authority |
| Variants and comparison | Branch a prompt and compare goal/constraint/context deltas | Optional evaluation is a separate explicitly budgeted run, never an automatic resend |
| Draft recovery and retention | Restore local unsent work; visible save failures and per-workspace retention | Avoid losing drafts or silently storing attachment bytes/secrets |
| Input/cost preview | Exact payload, supported limits and estimated size/cost with its basis | Unknown cost stays not recorded; provider/account/permissions stay visible and host-owned |

**Verified current foundation:** the session composer already uses CodeMirror6 Markdown, history,
bounded host-fed mention sources, typed FreeForm/GoalBlock/Template shapes, paste fencing and an
exact compiled preview. Reuse and Jump exist, as does append-only compilation history. These are
not a reversible prompt-stack UX or arbitrary rich blocks. Inspecting source found important
integration gaps: file/graph candidate arrays are empty in host.init, the session Configure caller
omits the optional TemplateCatalog, and ComposerDraftStore has no production consumer. A separate
standalone Prompt draft store is actually wired. Do not call these mechanisms fully delivered merely
because Core tests exist. See the [authoring inventory](../reviews/authoring-inventory-rev102.md).

### Get there from here without rebuilding the editor

1. **Finish existing wiring and preserve drafts.** Deliberately connect template/catalog and context
   candidates; integrate unsent session draft persistence with a visible failure state. Keep the
   working standalone draft path while migrating. Preserve literal mentions and exact preview.
2. **Wrap the current composer with revisioned authoring.** Add a small local draft/revision/parent
   model and Back/Forward. Keep editor undo, draft history and submitted envelopes separate.
   Reuse starts a new draft and retains the original submission/receipt identity.
3. **Add richer projections and transactions.** Structured blocks, compare/branch and previewed
   batch edits use the same canonical model. True multi-cursor/rectangular selection is a separately
   qualified CodeMirror enhancement, not a second editor framework. Test canonical round-trip,
   selection/undo across reinitialization, stale Prepare and the existing field-size refusal.
4. **Connect authoring to accepted work.** Version-pin obligations and context, expose authoring
   gaps, and reuse the shared evidence drawer. Keep provider/account, permissions and Send in the
   host. A renderer message must not become a new execution channel.

The prototype's rich document and prompt stack are fully local demonstration state. Formatting,
navigation, version save/branch, comparison, exact preview and batch edits run without an LLM.
Persistent restart recovery, production compilation, true multi-cursor behavior and sending remain
implementation work with explicit acceptance criteria, not mockup claims.

## Journey 2 — PR analysis and productive human review

Review begins with “What needs your judgment?” and why. A packet groups semantic changes by
behavior or architecture boundary, shows a before/after scenario and affected sequence, then
production code, changed tests, actual runs, counter-evidence and residual uncertainty.
Review by requirement or changed behavior rather than alphabetical files.

Every packet names exact base/head/content identity, inspected scope, omissions and analyzer
failures. A tour shows what it covered and offers the complete diff and impacted dependencies.
Human actions record **fixed**, **false positive**, **intentional**, **deferred** or **needs decision**
with a reason. Do not turn dismissed comments into a correctness score.

Policy initially requires human judgment for credentials/authorization, destructive persistence,
private-data boundaries, release authority, new domain invariants or architecture precedents,
contradictory intent and incomplete analysis. Routine reversible cases may later qualify for
lighter review only after local outcome calibration. Vendor runtime diff limits and model scores
are not policy thresholds. No auto-merge or provider action occurs in the prototype.

### Ask this PR — a reviewer can interrogate the change

The user's late requirement is part of the proposal: a human reviewer can ask “Did they think
of x?” or “How is y implemented?” from the whole PR, a semantic change group, diagram, file or
selected line. Keep the question attached to the exact change/spec/architecture baseline, not
a fresh unscoped chat. Answers show supporting production code, test assertions and actual runs;
separate observed evidence, inference, conflicting evidence and what was not found. Absence of
evidence never proves the author did not consider something.

Follow-ups preserve the selected scope and prior question context. A useful explanation can open
its sequence/class/ERM slice; an unresolved concern can become a proposed review question,
test-gap item or author clarification with its evidence intact. These actions do not authorize
execution or grant approval. New head/spec content marks the answer stale and requires reassessment.
The mockup accepts arbitrary questions and demonstrates three known fixture answers plus an honest
not-established fallback; it does not pretend to call an LLM or inspect the live PR.

**Verified documented prior art:** GitHub provides PR-, file- and line-scoped Copilot questions
and can use the originating agent's session logs for agent-created PRs. The additional obligation
links, uncertainty and question-to-backlog workflow here are **Inferred AI-DE design**.
[GitHub PR exploration](https://docs.github.com/en/copilot/tutorials/explore-pull-requests).

## Journey 3 — Test coverage and worthiness


Use the same selection with **current change / component / whole repository** scope. Keep separate:
requirement/failure-mode coverage, executed statement/branch coverage, assertion strength,
integration fidelity, purposeful versus suspected redundant protection, elapsed/compute cost and
reliability. Missing collector data is “Not recorded”; 90% line coverage is never 90% acceptance.

The operator can move from an uncovered criterion to the relevant code and missing boundary case;
from a suspected redundant test to overlapping assertions and its platform purpose; from a slow
suite to measured setup/runtime breakdown; and from a flaky result to first attempts, retries,
environment and observation window. Preserve raw mutant states and review suspected equivalence
separately. Quarantine/retries cannot erase failures or create a clean pass rate. Proposed remediation
creates backlog entries, not automatic test deletion or weakened gates.

## Journey 4 — Codebase understanding at scale

Understand starts with a question and scope: changed component, bounded context, entry point,
data entity, or a full-repository grouped overview. A summary graph leads to smaller neighborhoods;
the tree/table is an equally capable route. Every view discloses selected/returned/omitted nodes
and limits. Search, focus, expand, breadcrumbs and zoom preserve where the user came from.

Choose the representation by question: dependency graph for impact; class/interface for contracts;
sequence for an operation and failures; ERM for keys/cardinality/history. Distinguish extracted
static evidence, runtime observation and agent inference in the relationship and legend.
The source viewer opens from a selected claim or symbol. It is a drill-down, not the default
operating surface or a new editable IDE. Unsupported language/ambiguous relation states remain visible.

## Journey 5 — Architecture review

Compare accepted constraints and ADRs with the observed component/dependency map. Show one
boundary crossing or cycle at a time, its affected criteria, runtime/data consequence and proof.
Use before/after overlays for changed dependencies and ERM/schema evolution. Assertions about
ownership, bounded contexts or runtime calls carry explicit evidence status.

The view can link a drift to implementation repair, an approved architecture change, an obsolete
baseline or an unresolved question. It must not redraw accepted architecture to make a violation
disappear. Existing native/static diagrams are reusable; full C4/ERM product surfaces remain new
work and must not be counted as shipped because a documentation diagram exists.

## Journey 6 — Rigor and risk backlog

One review queue gathers **resilience, compliance/policy, privacy, performance and security**
items with kind risk/issue/todo, priority, affected scope, evidence and disconfirmation, consequence,
owner, disposition, dependencies and a falsifiable validation method. Architecture risks join the
same evidence model. It reuses existing health incidents and governance artifacts as inputs without
pretending those inputs already form an integrated assurance service.

Compliance means evidence against a selected policy/obligation profile, not an AI-generated
certification badge. Privacy reviews show actual data boundaries and unknowns; performance
recommendations use measured cost axes. The operator may accept a backlog item for planning,
dispute it with a reason, or defer it visibly. Those are not production-change authorizations.

## Grounded change summaries

An immutable episode packet contains intent and architecture baselines; behavioral delta;
production symbols/contracts/schema changes; new or changed tests; actual command/run outcomes;
small before/after diagrams; omissions and stale evidence; human decisions; remaining work.
Every narrative assertion can open the evidence drawer at the exact artifact version. A model
may explain retained facts but cannot promote inferred links or invented measurements into truth.

## Evolve, do not rebuild

| Phase | Deliverable | Reuse / dependency | Proof before expanding |
|---|---|---|---|
| 0. Trust the evidence | Triage/repair freshness and framed bytes; make recovery/current docs observable | FR-001/002 first, then independent observation | Same-revision dirty edit and hostile serialized response tests |
| 1. Intent and evidence spine | Local work items/criteria/evidence plus revisioned authoring, prompt stack and canonical preview | Existing store identities, envelopes, episodes and graph | Meaning/history invariants; stale baseline and missing-proof states |
| 2. Guided change review | Provider-neutral change snapshots, semantic packets and human decisions | Phase1; existing source/sequence/impact views | All changed scope accounted for; changed head invalidates decisions |
| 3. Test assurance | Runner/coverage history, flake/efficiency and meaningful obligation views | Run identities and existing test tooling | Accurate first-attempt/retry denominators; no fake completeness |
| 4. Architecture and rigor | Constraint comparisons and evidence-backed risk queue | Same scoped graph and decisions | Ambiguity/authority handling; native usability and accessibility |

Migrate by addition and measured adoption: introduce work items around existing sessions; reuse
current views; preserve saved layouts and session access; move the default landing only after
the equivalent tasks succeed; retire duplicate navigation only after its replacement is proven.
Avoid a big-bang shell rewrite or a second fact store. Each phase is a vertical slice with real
empty/error/stale states and normal-path instrumentation before rollout.

## Success measures and trade-offs

Proposed pilot measures: time to locate required human decisions; time to explain a changed
behavior; criteria without meaningful proof; material boundaries not reviewed; stale decisions;
first-pass and retry-recovered outcomes; review dispositions and escaped defects; run latency,
spend, volume and failures. No productivity multiplier or target percentage is asserted before a
baseline exists. Keep human review time and defect outcomes together when calibrating policy.

Main trade-offs: more durable identity and provenance work versus less repeated context assembly;
guided summaries versus the risk of hiding unexamined code; compact defaults versus expert access;
local authority versus optional provider/network features. The shared drawer and complete-diff
escape hatch are controls for those trade-offs, not guarantees that analysis is correct.

## Open decisions for `/specify`

| Question | Needed for | Recommendation |
|---|---|---|
| Which artifact/actor may accept a spec baseline? | Intent authority | Explicit versioned acceptance; no implicit latest-file rule |
| Which review obligations can be waived and by whom? | Human policy | Conservative initial human acceptance, named reason and audit |
| Which runner/coverage/provider integration first? | First vertical slice | C# plus existing local runner and one explicit optional PR adapter |
| How should a work item span repos/worktrees? | Aggregate boundaries | Refer by workspace identity; avoid distributed aggregate promises |
| How is meaningful test evidence confirmed? | Oracle trust | Link plus reviewed assertion and executed run; calibrate agents |
| Which risk profile is authoritative? | Compliance/privacy scope | Selected organizational/domain obligations, never invented certification |

The local-desktop decision is already resolved. Remaining decisions are explicitly deferred to
specification; the prototype demonstrates hypotheses with synthetic state. Human triage of the
forensic backlog and choice of proposal direction precede implementation.
