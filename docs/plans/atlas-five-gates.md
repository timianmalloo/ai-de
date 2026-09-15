---
id: plan-atlas-five-gates
title: "Atlas five-gate repair graph"
type: doc
status: proposed
owner: "@timianmalloo"
tags: [atlas, execution-graph, coordination]
links:
  - { to: coordination-atlas-five-gates, rel: relates-to }
  - { to: session-contracts, rel: depends-on }
review-by: 2026-12-15
summary: "Repair the five frozen Atlas integration failures before main publication and E1/E2 continuation."
---

# Completion contract

Goal: clear the exact five integration gates handed off at dd9b338f4670f9ce64d0c38a5e7c196664250d9a.
Done when: actual repairs, negative controls, independent veto clearance, official integrated qualification and prepublication Release are complete; GHCP receives the reviewed candidate for serialized main publication.
Not in scope: E1/E2 expansion, native 46e2f266 qualification, watcher repairs, new ownership policy, peer worktree cleanup or Codex main publication.
Tier T2; fan-out cap four including Astra Owner and Conductor. User explicitly approved the five-gate work-area transfer. Existing Atlas intent and architecture are reused; this graph and the proof record specify the repair slice. No new feature specification is needed.

## Grounding and surface list

Verified: all five supplied gates fail independently in the new tree. Main 901320c4 has four later docs/ruling/audit commits; Owner requires incorporating it before repair freeze. Raw historical qualification remains in the integrator's read-only .artifacts directory. Release was not reached, and no historical green status is substituted for current acceptance.

Affected surfaces: file/handle bounds and filesystem path identity → source authorization → test exception propagation → static scanners → ownership register parsing → audit evidence capture → official integrated test/gate/Release invocation → proof and publisher handoff.

Existing controls and constitution: optimize-graph, prepare-for-coordination, execute-with-coordination; session-contracts §2; session-collaboration; Testing Strategy; Rigor/No-Guessing; end-to-end integrity; continuous improvement. Reuse existing defect classes after source inspection; record class → sweep → derive → prevent in each repair receipt. Invoke investigate for observed failures. Existing UI designs remain frozen; no rendered product change is proposed.

## Execution graph

### Exact repair ledger (dispatch contract)

All gate commands are `python tools/<gate>.py`; normal integrated rerun is the same command through `python tools/run-verify-gates.py`. Task receipts are exact authored paths listed below. No checker baseline, watermark or broad allowlist may change.

| Gate / initial red | Author and exact allowed implementation/test paths | Regression and falsification oracle | Returned proof |
|---|---|---|---|
| verify-audit-capture: 18 rows lack evidence | Audit worker: docs/audit/audit-log.jsonl through official writer only | Preserve original semantic payloads; append truthful superseders after inspecting each current failing row. Run existing capture self-test, including duplicate/nonduplicate and superseder negatives. No fabricated retrospective acceptance. | docs/proof/atlas-audit-capture-repair.md |
| verify-surface-ownership: historical rows136/412 called malformed | Parser worker: tools/verify-surface-ownership.py including its existing self-test | Valid historical non-Path table accepted; owner-shaped malformed/missing/mangled Path header still rejected. Recursive census, duplicate basenames, ambiguity, stale exceptions and existing eight mutants retained. Red-first new fixtures must reject old scanner. | docs/proof/atlas-ownership-table-repair.md |
| verify-bounds-are-enforced: MaxIndexBytes | Core worker: tools/verify-bounds-are-enforced.py; tests/AiDe.Core.Tests/Understanding/AtlasGitMembershipTests.cs | Real production caller at declared limit and one over limit, with explicit native support/refusal; over-limit must refuse index-byte-budget before publication. Forwarding long.MaxValue mutant and deleted helper clamp must fail. Mere helper-only proof or reason-only exemption fails acceptance. | docs/proof/atlas-core-gate-repair.md |
| verify-containment-comparisons: two OrdinalIgnoreCase sites | Same Core worker: src/AiDe.Core/Understanding/AtlasGitMembership.cs; src/AiDe.Core/WorkspaceCore.cs; tests/AiDe.Core.Tests/Understanding/AtlasProductionAdmissionTests.cs; preceding membership test file | Existing Windows-native membership contract and portable admission rules inspected; shared filesystem comparison at separator boundary; rooted/traversal refusals preserved. Case-distinct POSIX oracle only where the real API permits such input; no invented exploit. Any fixed Windows-only comparison requires exact evidence and Owner/reviewer decision, not broad suppression. | docs/proof/atlas-core-gate-repair.md |
| verify-harness-diagnostics: wrapper and unclassified STA | Harness worker: tools/verify-harness-diagnostics.py including self-test; tests/AiDe.App.Tests/Workbench/Understanding/AtlasSharedHostAdmissionTests.cs | Correlated original-exception TCS handoff recognized; healthy and wrapped/bad handoffs in SAME file fail. At line599 primary assertion plus cleanup failures must retain assertion headline and both diagnostics. Never ignore AggregateException globally. Inject assertion plus cleanup failure through real helper; no UIA behavior rewrite. | docs/proof/atlas-harness-gate-repair.md |

Exact receipt paths are part of each lane's allowlist; no shared lesson/audit/derived writes by simultaneous workers. Conductor serializes returned lesson recurrences under existing DC-104/DC-118/diagnostic classes after evidence review. Each worker receives its own tree and stable identity. Additional paths require a seam decision before writing.

Owner's audit normalization decision is a C exit before Audit dispatch: official coord-register ignores only id/renumbered_from for its fingerprint; compare both parents by that rule, record mapping and check references. Initial Owner readback found zero lost payloads and no references to33 removed alias IDs; Conductor must independently inspect that result. It is not permission to delete unique history or invent capture signals.

Qualification union: exact five gates plus their self-tests, focused Core membership/admission tests, focused App failure-propagation tests under GHCP desktop scheduling where needed; Testing Strategy D0/D1 plus filesystem/resource/async directives apply to matching changes. Final official join.json runs `python tools/verify-test-run.py --update`, the Core `--filter Platform!=Windows --key AiDe.Core.Tests.portable` and `--filter Platform=Windows --key AiDe.Core.Tests.nonportable` recounts, `--no-run`, regeneration, and every static gate. Final Release is `dotnet build src/AiDe.App/AiDe.App.csproj -c Release -nologo -v q`. No docs-only/no-build shortcut in final qualification. Raw command output, actual TRX paths, exit, test counts and measured duration must be added to the proof after execution; no future path is claimed as evidence. GHCP schedules the desktop and publishes only after qualification. Release does not replace any test/gate.

| Node | Inputs and purpose | Exit / oracle | Dependency |
|---|---|---|---|
| A | Frozen failures, authority, current main; startup records | Scope, exact isolation and measured layer state | None |
| B | Official preparatory docs-only main join | Main ancestor present; merge history and actual failed qualification retained | A |
| C | Audit rows and parser semantics | Exact manifest/design, Owner ruling, independent plan gates | B |
| D | G1 audit capture | Original rows preserved; truthful compliant corrective rows; capture self-tests | C |
| E | G5 ownership parser | Historical non-Path tables accepted; malformed Path rows and prior negative controls rejected | C |
| F | G2/G3 bound and containment | Real cap evidence, at/over-bound oracle, platform path semantics, no broad suppression | C |
| G | G4 harness diagnostics | Assertion headlines and cleanup failures preserved; all STA paths classified truthfully | C |
| H | Returned commits and raw results | Independent Test/Security/SRE/Tech Lead and applicable persona clearance | D,E,F,G |
| I | Official join and current main | Whole required test/gate union and Release; raw outputs inspected | H |
| J | Candidate/proof/empty blocker set | GHCP publication receipt and advertised main ancestry | I |

```mermaid
graph TD
 A-->B-->C
 C-->D
 C-->E
 C-->F
 C-->G
 D-->H
 E-->H
 F-->H
 G-->H
 H-->I-->J
```

Naive graph serializes D/E/F/G; optimized graph separates independent artifact work after semantics settle. Schedule at most two worker agents with Owner and Conductor; G follows a freed worker slot. No worker edits shared audit/derived output concurrently. Root coordinates; authors perform repair units. Models: Astra for ownership/path/exception semantics and Owner decisions; Sol high for bounded independent review when adequate. Reviewers never clear their own authored work.

Modeled, not measured: with unit-cost nodes, T1=10, serial span=10, optimized dependency span=7; actual machine/runtime costs are not yet known, so no claimed speedup. Two worker slots and coordination overhead limit realized gain. Main budget initially 60 tool boundaries; each author initially 24, review 16. Budget exhaustion triggers diagnosis/replanning, never dropping gates. No automatic semantic retries; one retry only for a diagnosed transient tool failure. Partial returns remain unaccepted.

Loop variant: unresolved enumerated repair/review predicates decreases toward zero. Two passes without reduction escalate evidence/options to Owner; new findings do not silently enlarge scope. Material main movement, path-manifest change or new gate failure triggers graph reassessment. Final qualification uses GHCP's desktop allocation, never concurrent full App/native runs.

## Planned versus actual

Initial: five red gates reproduced before any code edit. Separate Owner confirmed main-first and sole GHCP publication. Pack doctor: 10 PASS, 3 WARN, 0 FAIL; Python substitution, Copilot context/effort configuration and docs graph freshness warnings retained. Coordination doctor: effective drivers, 11 patterns, four regeneration debts reported; regeneration then verified actual views. Startup marker began 2026-09-15T22:29:17Z, after initial diagnostics; it does not measure earlier grounding. Main901 is now an ancestor through official merge4473f260; site conflicts were exclusively generated counts, rebuilt from the merged sources. Preparatory join record c2f94cb8 exists; official checks are running, not accepted. Independent plan review initially BLOCKED missing exact repair predicates; the ledger above addresses those predicates and requires retained reviewer clearance before author dispatch.
