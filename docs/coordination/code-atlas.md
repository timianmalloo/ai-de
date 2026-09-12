---
id: coordination-code-atlas
title: "Coordination plan - Code Atlas first delivery horizon"
type: doc
status: proposed
owner: "@timianmalloo"
tags: [code-atlas, coordination, worktrees, blocked]
links:
  - { to: spec-addendum-e-code-atlas, rel: implements }
  - { to: architecture-code-atlas-proposed, rel: depends-on }
  - { to: coordination-code-atlas-resume, rel: depends-on }
  - { to: note-atlas-lane-admission, rel: depends-on }
review-by: 2026-12-12
summary: >-
  Branch-local safety authoring is admitted and active under an explicit Owner exception.
  One additive candidate writer follows the exact E0 design; shared Shell integration and
  normative registration remain separately gated.
---

# Code Atlas coordination - isolated authoring active, integration not admitted

**Current delivery blocker:** `req-01M2B86TXF7SHG61B31P4H4173` is still open.
The native request is a shared pull log, not delivery into Claude's conversation. It blocks
agreement on shared integration, **not the independently admitted authoring below**. The user's
continuation instruction led to `note-atlas-isolated-authoring`; the sole section-2 register now
records that branch-local exception and exact safety writer/files. Existing ownership rows remain.
The new narrowed request is `req-01M2BGHNCM6WRD4ZZMBBFEEB4K`. Neither request is acknowledged.

**Horizon:** E-0, the Owner's first physical inventory/type/member/source native journey.
E-1 through E-4 are roadmap constraints, not work released by this plan.
Metadata uses `type: doc` because the installed graph registry rejects `plan`; the execution-plan
headings/columns below retain the skill's machine-readable schema.

## Layer state

| check | result | meaning |
|---|---|---|
| `coord doctor` in conductor tree | 11 classified patterns; merge drivers effective | Installed per clone; every linked worktree inherits. No reinstall from a worker tree. |
| Shared regeneration marker | 6 owed entries at grounding | Not cleared with `coord regen`: its primary-owned marker must not be deleted as a worker side effect. |
| `pack-doctor` | Coordination PASS; graph has known dangling F5 proof link | Installation state and content readiness are separate. The F5 branch remains frozen. |
| Main at worker creation | `4d396411`, CV-1 merged after SH-2 | Both new worker trees resolve to this revision; reconcile again at shared integration. |
| Atlas code permission | New-file branch-local authoring admitted | Exact exception in section 2; existing shared files and main integration still excluded. |
| Candidate E | Content gates clear with downstream conditions | Candidate/draft only, not normative registration or implementation proof. |
| Architecture | PROPOSED; independent content reviews, Owner choices and final scoping conditions recorded | No source, migration, native or provider admission. |

## Artifact classes

| path / pattern | class | mechanism | coordination needed |
|---|---|---|---|
| Agreed product source/test files | authored | One registered writer per file | Yes; exact Core/Shell carve-outs required. |
| New Atlas spec/architecture/ADRs/design/proof | authored | Isolated branch, named scope and lease | Yes for authorship; no overlap with C/D documents. |
| `docs/audit/*.jsonl`, coordination logs | register | Existing append-only writers; content union at join | No content-writer lease choreography; never hand-merge away another entry. |
| `docs/docs-index.js`, audit view, API/doc bundle | derived | Existing generators, in dependency order | No manual merge; Conductor regenerates after audit at join. |
| `site/*.html` | authored with derived figure regions | Existing figure updater only | Never classify the whole page derived or overwrite another author's prose. |
| Private proposal branch / TheTerrace | reference only | Read-only; safe summaries only | No import into delivery history or publication. |

## Tracks

Only the exact source-safety files/writer currently recorded in the section-2 exception are dispatched.
The candidate's exact file manifest must be added there before dispatch. Other `owns` entries below
remain proposed integration responsibilities, not a grant.
Budgets are Inferred planning circuit breakers. A firing cap reports a finding; it never drops a gate.

| track | owns (authored) | depends on | tier | fan-out cap | budget | exit evidence | harness |
|---|---|---|---|---|---|---|---|
| E0-SAFETY | Four exact new probe project/source files and `docs/proof/code-atlas-source-safety.md`, listed in section 2 | Explicit Owner probe admission, now recorded | T2 | 0 | 30 calls including preflight/clarification, one bounded native handle/race batch | Windows opened-object/root/link/replacement/hash/decoder semantics observed; refusal/race falsifiers; no userdata or shared-store mutation | Existing GPT-5.5 writer, new `atlas/e0-source-safety` tree at `4d396411`; dispatched |
| E0-BUILD | Exact new `Core/Understanding` and detached `App/Workbench/Understanding` source/tests, named by E0 design and recorded in section 2 before dispatch | Owner new-file grant; E0 design; safety receipt before reader incorporation | T2 | 0 | 60 calls to one reviewed candidate checkpoint; no reset on a seam | Generated real files and compiler symbols, bounded inventory/identity/binding/history tests, inert supplied-projection controls; no fake host proof | One GPT-5.5 writer in reserved `atlas/e0-candidate` tree at `4d396411`; existing-file fallback uses a separately named standalone candidate |
| SH-INTEGRATION | Existing factory/menu/host/layout files retained by Claude/Shell | Stable E0-BUILD seam and accepted integration request | T2 | per current Claude plan | Set by owning conductor, not invented here | Registry/routing/layout/native-host path reaches the real new content; current SH2 behavior remains intact | Existing acknowledged Claude/Shell lane, its own worktree |
| E0-PROOF | New agreed proof/tests/probe artifacts, not product source | Joined E0-BUILD + SH-INTEGRATION revision | T2 | 0 | 30 calls, one bounded evidence pass plus named repairs | Actual Architecture entry -> file -> member -> source -> Back with UIA/focus/theme/DPI/bounds/stale/unknown states; independent source/wire/store consistency proof | GPT-5.5 proof worker, separate pinned worktree; relevant independent specialist reviewers |

One coherent implementation writer is deliberate: splitting inventory/model/source/selection across
several new writers would create serial schema and adapter seams while paying parallel context cost.
The fleet still separates Owner, Conductor, author, Shell integrator and verification authority.
It does not manufacture concurrent code lanes where dependencies fail the independence test.

## Serial spine

| item | why it cannot be parallel | who owns it |
|---|---|---|
| Register E and agree shared integration | Normative registration and existing-file/main authority are not supplied by the branch-local exception | Claude primary conductor + Core/Shell counterparts; Astra Owner for Atlas scope |
| Source identity/manifest/policy contract | Every downstream projection and source read depends on it | Accepted E0 design owner, Data/Security/Test gates |
| Windows source-reader safety choice | Unsafe opened-object/hash policy invalidates the source contract | Native/Security/Core design gate |
| Public wire/capability contract | UI and daemon must agree on bindings, bounds and refusal | Core/daemon owner |
| Shell integration | Existing active files have one owner | Claude/Shell |
| Main convergence | Shared index/HEAD cannot have two integrators | One explicitly agreed integrator; currently Claude |
| Horizon closure | Author reports are evidence, not acceptance | Separate Owner after required independent gates |

## Seams

| from -> to | the request | resolved by |
|---|---|---|
| Atlas -> Core | Accept exact inventory/member/source/query/wire source/test carve-out from architecture section 14 | Core authority routed by Claude conductor |
| Atlas -> Shell | Consume stable native content/selection/query seam; Shell owns registry/menu/host changes | Shell owner, not lease expiration |
| Core producer -> native consumer | Versioned manifest/scope/identity/hash/span/coverage/bounds survive every projection/wire hop | Single E0-BUILD author until contract stable; independent tests |
| Atlas -> Conversation, later | Dedicated read-only analysis contract; never compiler-envelope/direct-provider shortcut | Conversation owner only at E-4 admission |
| Workers -> Conductor | Commit/report/proof receipt with exact scope, tests, residuals and no self-acceptance | Conductor joins; Owner adjudicates |

Shared-surface guards are jointly scoped: Atlas prohibits its interpretation path from invoking
compile/run APIs, not those APIs everywhere in shared files. Shell retains its legitimate catalog/
layout operations. Any new scan guard must state root, recursion, tokens and named allowlist.
No guard may require removal of another lane's authorized behavior.

## Struck tracks

| track | why it was not worth its multiplier |
|---|---|
| Separate file/type/member implementers before contracts | Splits one identity and source-binding invariant; every change becomes a seam request. |
| Independent second graph service/store | Duplicates authority, history and privacy state without measured need. |
| Parallel E1/E2/E3/E4 feature lanes now | Owner has not admitted them; E0 identity/source foundation and stage-specific proofs are prerequisites. |
| Atlas edits to shared Shell files in parallel | Existing ownership and main-integration race, not useful parallelism. |
| More workers to overcome missing acknowledgment | Width cannot supply authority or make an unreceived request an agreement. |

## Order of operations

**Active exception:** source-safety implementation and isolated E0 design/additive candidate proceed
now under the exact section-2 grant. The sequence below governs production integration and native
acceptance; step 1 is not a predecessor of every independent authoring node. No existing SH3 file,
project/package file, live data or shared store is touched by the admitted candidate.

| # | action | cost | why now |
|---|---|---|---|
| 1 | Obtain actual resolution of full native request ID and section-2 updates | External acknowledgment; not an estimated timer | Code permission cannot be inferred. |
| 2 | Reconcile current main, registration, content decisions and exact source/test seams | Bounded read/rebase/design checkpoint | Main has advanced since original source evidence. |
| 3 | Admit E0 design-slice, safety spike and numeric resource budgets | Owner + triggered independent gates | Queue, source, memory, storage and telemetry floors precede code. |
| 4 | Execute E0-SAFETY, then one E0-BUILD writer under TDD | Track budgets above; estimates, not measured speedup | Source safety and one identity contract constrain all consumers. |
| 5 | Join owning Shell integration serially | Owning conductor's agreed budget | Real native entry path closes here, not in a fake factory. |
| 6 | Execute E0-PROOF and independent gates; repair named findings | Bounded proof loop | Code/demo/test evidence must describe the same revision. |
| 7 | Regenerate after audit, read back state and seek Owner horizon closure | Deterministic mechanics | No false completed/private-published/clean-state claim. |
| 8 | Only then ask Owner to admit the next vertical stage | New phase decision | Architecture completeness does not admit every implementation phase. |

## Harness and fan-out contract

Width <=4 across active author/review/decision seats. No autonomous child fan-out by a worker.
Each worktree is separately registered, with its own branch/index; each shell call sets cwd/identity.
Current worker writes and commit-floor checks were observed. `coord doctor`'s older per-harness
edit-boundary qualification is historical and is not promoted to a fresh current-version proof.
No automatic fallback from an enforced boundary to a merely observed one is allowed.

Transient failures are reported and retried only with a named transient cause and bounded backoff;
accepted observations/side effects are not blindly repeated. Join requires all affected hard floors.
One failed track blocks its descendants, not unrelated admitted work. Review repairs drain a finite
named finding list, at most two passes before explicit escalation. No source/authority floor is
silently traded for deadline, token budget or fan-out.

| Completed | Remaining | Best next action |
|---|---|---|
| Owner corrected the blanket freeze; exact safety writer/files recorded and probe dispatched in its own tree. | E0 design and safety results, candidate implementation/proof, actual shared-integration agreement and native-product acceptance. | Join the safety/design receipts and dispatch the exact additive candidate; pursue shared integration separately without human relay or repeated polling. |
