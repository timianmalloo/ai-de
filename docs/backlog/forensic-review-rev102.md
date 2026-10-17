---
id: forensic-review-rev102-backlog
title: "Forensic remediation backlog — revision 102"
type: doc
status: proposed
owner: "@timianmalloo"
phase: "assessment"
tags: [forensic-review, inventory, architecture, factory]
links:
  - { to: forensic-review-rev102, rel: documents }
  - { to: architecture, rel: relates-to }
review-by: 2027-01-17
summary: "Forensic remediation backlog — revision 102: repository evidence at 9f13ca7a, measured baseline, explicit confidence and proposed next work."
review-suggested: []
---

# Proposed remediation backlog

All items have status **proposed**; priorities are ordinal judgments, not quantified failure
probabilities. No P0 was demonstrated. Two P1 issues are executed reproductions. New factory
features belong to the proposal roadmap and are not mislabeled existing defects.

Phases: contain freshness and framing exposure first (FR-001/002); restore independent observation
and recovery next (FR-003/005/006); qualify diagrams, connector boundaries and release proof
(FR-004/007/008); complete documentation and integration decisions (FR-009/010/011). These are
independently triageable slices, not automatic execution authority.

## FR-001 — Dirty edits can survive forced reindex as old graph evidence

| Field | Proposed item |
|---|---|
| Kind / priority / status | issue / P1 / proposed |
| Category / scope | freshness; `src/AiDe.Core/WorkspaceCore.cs:154–160,445–498; WorkbenchShell.cs:697,791–794` |
| Evidence / confidence | [Review FR-001](../reviews/forensic-review-rev102.md); Verified executed. Same-revision force:true reported ScopesIndexed=1, Assertions=0 and kept Probe.BeforeEdit after the file became AfterEdit. A changed-revision control emitted AfterEdit. |
| Consequence | Graph, summaries and reviews can describe old source while reporting successful indexing. |
| Recommended remediation | Use a content identity/dirty-state revision that reaches every reuse key; record actual reuse versus extraction honestly. |
| Falsifiable acceptance | At fixed HEAD, normal and forced A→B refresh replaces A with B; reopen preserves it; telemetry names extracted/reused scopes. |
| Validation method | Run retained probe and a product-composed reindex/reopen test. |
| Disconfirming check attempted | Compared both fingerprint and revision reuse layers; executed same-revision and changed-revision Core controls. |
| Dependencies | none |
| Suggested owner / next skill | Core / extraction; `/investigate` |

## FR-002 — Entry-point results can exceed the IPC frame despite row clamping

| Field | Proposed item |
|---|---|
| Kind / priority / status | issue / P1 / proposed |
| Category / scope | resource bounds; `src/AiDe.Core/Projections/ProjectionService.cs:704–730; EntryPointsProjection.cs:62–70; EveryOperationFitsTheFrameTests.cs:50–51` |
| Evidence / confidence | [Review FR-002](../reviews/forensic-review-rev102.md); Verified executed. 1,300 facts with 2,000-character identifiers return 1,254 rows and serialize to 5,165,696 bytes against a 1,048,576-byte frame. |
| Consequence | Large/hostile repository content can make a normal read untransportable. |
| Recommended remediation | Enforce actual serialized byte budget including envelope and escaping; retain exact omission disclosure. |
| Falsifiable acceptance | Long Unicode/escaped names and members fit the complete frame at caller int.MaxValue; omitted counts match removed rows. |
| Validation method | Extend frame-oracle input domain; retain payload and outer-frame checks. |
| Disconfirming check attempted | Inspected final return path and measured Web-default JSON payload using the same serializer family as the existing frame oracle. |
| Dependencies | none |
| Suggested owner / next skill | Core / IPC; `/investigate` |

## FR-003 — Independent freshness detector is not composed on the production path

| Field | Proposed item |
|---|---|
| Kind / priority / status | issue / P2 / proposed |
| Category / scope | reliability; `src/AiDe.Core/Health/FreshnessProber.cs:24,34; docs/architecture.md former current component description` |
| Evidence / confidence | [Review FR-003](../reviews/forensic-review-rev102.md); Verified static. Only declaration, tests and explanatory comments reference FreshnessProber; no production instantiation or call found. |
| Consequence | Lost watcher signals lack the independent detection promised by current documentation. |
| Recommended remediation | Compose a measured periodic repository probe and bounded re-enqueue, or revise the contract explicitly. |
| Falsifiable acceptance | A quiet/lost-watcher dirty change emits a stale incident and queues the scope without manual indexing. |
| Validation method | Product-composition lost-signal probe with injected clock and observable queue. |
| Disconfirming check attempted | Global source type/call search; checked existing WalkingSkeletonTests which construct their own prober. |
| Dependencies | FR-001 |
| Suggested owner / next skill | Core / reliability; `/design-slice` |

## FR-004 — Simple-name association inference can select an ambiguous type

| Field | Proposed item |
|---|---|
| Kind / priority / status | risk / P2 / proposed |
| Category / scope | diagram integrity; `src/AiDe.App/Workbench/ClassHierarchyModel.cs:183–208` |
| Evidence / confidence | [Review FR-004](../reviews/forensic-review-rev102.md); Verified algorithm; inferred consequence. The association lookup overwrites entries keyed by simplified labels; collections are drawn as aggregation without an ownership proof. |
| Consequence | A diagram may imply a wrong relation or stronger ownership than extracted evidence supports. |
| Recommended remediation | Resolve qualified identity; represent unresolved ambiguity; disclose structural inference separately from ownership. |
| Falsifiable acceptance | Sales.Order and Shipping.Order do not collapse; ambiguous member types yield an explicit unresolved state, not an asserted edge. |
| Validation method | Headless model fixtures and rendered legend/accessibility checks. |
| Disconfirming check attempted | Read derivation and available association tests; no duplicate-qualified-name oracle found. |
| Dependencies | none |
| Suggested owner / next skill | App / graph visualization; `/investigate` |

## FR-005 — Core query and indexing activities lack a normal retained sink

| Field | Proposed item |
|---|---|
| Kind / priority / status | risk / P2 / proposed |
| Category / scope | observability; `src/AiDe.Core/WorkspaceCore.cs:19; ProjectionService.cs; production ActivityListener search` |
| Evidence / confidence | [Review FR-005](../reviews/forensic-review-rev102.md); Verified composition gap; inferred runtime visibility. Existing listeners are narrow terminal/composition/disposal ledgers; test-only telemetry listeners establish emission, not deployed retention. |
| Consequence | Operators may not answer query/index latency, volume and failure questions after normal usage. |
| Recommended remediation | Retain bounded local structured cost/outcome observations on the normal path; unknown stays not recorded. |
| Falsifiable acceptance | A fresh product index/query/failure leaves attributable latency, volume and result records without a diagnostic flag or test listener. |
| Validation method | Composed application run with local-sink capture; include no-sink behavior. |
| Disconfirming check attempted | Searched ActivityListener, OpenTelemetry/exporter composition; acknowledged existing durable UI diagnostics. |
| Dependencies | none |
| Suggested owner / next skill | Core / SRE; `/design-slice` |

## FR-006 — Failed episode import is marked complete before import succeeds

| Field | Proposed item |
|---|---|
| Kind / priority / status | issue / P2 / proposed |
| Category / scope | recovery; `src/AiDe.App/Workbench/WorkbenchShell.cs:2895–2897,2940` |
| Evidence / confidence | [Review FR-006](../reviews/forensic-review-rev102.md); Verified static control flow. _episodesImported is set before ImportAndScoreEpisodesFromAuditLog; the outer catch does not clear it. |
| Consequence | A transient first import failure can leave that attach missing episode evidence. |
| Recommended remediation | Record successful completion only after success; expose bounded retry/error/recovery status. |
| Falsifiable acceptance | Fail first import transiently, then succeed without reattach; no duplicate imported episodes; last failure is visible. |
| Validation method | Fault-injected watcher-loop test using actual import orchestration. |
| Disconfirming check attempted | Read import block, catch and reattach reset sites; no same-attach retry state found. |
| Dependencies | none |
| Suggested owner / next skill | App / watcher; `/investigate` |

## FR-007 — Read-only ACP label does not cover all settings-loaded tools

| Field | Proposed item |
|---|---|
| Kind / priority / status | risk / P2 / proposed |
| Category / scope | security and privacy; `src/AiDe.App/Conductor/GovernedRunHost.cs:88–95` |
| Evidence / confidence | [Review FR-007](../reviews/forensic-review-rev102.md); Verified documented residual. The code explicitly names mcp__* tools/connectors outside built-in denies; settings/hook behavior is a separate boundary. |
| Consequence | Read-only analysis can have a broader effective capability set than the user expects. |
| Recommended remediation | Qualify actual offered tools/connectors and execution policy; keep changed-tree checks as a backstop. |
| Falsifiable acceptance | Hostile write/process/delegation connector fixtures are denied before side effect; observed tool inventory is retained per actual harness version. |
| Validation method | Version-bound lane qualification and hostile connector probe. |
| Disconfirming check attempted | Read options and compile-lane distinctions; no exploit is alleged. |
| Dependencies | none |
| Suggested owner / next skill | Security / agent integration; `/design-slice` |

## FR-008 — Release plan lock/pin requirements are not yet implemented

| Field | Proposed item |
|---|---|
| Kind / priority / status | issue / P2 / proposed |
| Category / scope | release reproducibility; `docs/release/ai-native-ide-release-plan.md:77–80; Directory.Packages.props:11,17; .github/workflows/build.yml:49,58` |
| Evidence / confidence | [Review FR-008](../reviews/forensic-review-rev102.md); Verified repository state. No NuGet packages.lock.json/locked restore configuration found; AvalonEdit floats at 6.* and Actions use version tags. |
| Consequence | Build provenance can vary; this is reproducibility debt, not a claimed vulnerability. |
| Recommended remediation | Implement locked dependency resolution/immutable action references or formally reconcile the release contract. |
| Falsifiable acceptance | Clean restores reproduce the dependency graph; resolved versions/action identities appear in publish evidence; failures are fail-closed. |
| Validation method | Clean locked restore and publish/provenance rehearsal. |
| Disconfirming check attempted | Searched all lockfiles and restore settings; distinguished npm lockfiles and the successful current NuGet advisory query. |
| Dependencies | none |
| Suggested owner / next skill | Release engineer; `/design-slice` |

## FR-009 — Current diagrams contradict shipped composition and capability limits

| Field | Proposed item |
|---|---|
| Kind / priority / status | issue / P2 / proposed |
| Category / scope | documentation; `docs/diagrams/component.md:98,109 at baseline; MainWindowViewModel.cs:252; SequenceModel.cs:30` |
| Evidence / confidence | [Review FR-009](../reviews/forensic-review-rev102.md); Verified contradiction. The current component diagram described in-process shell defaults; the shell already connects/launches the daemon. Other scaffold comments understate implemented search/sequence. |
| Consequence | Planning and reviews can rebuild capabilities already shipped or assume a missing protection exists. |
| Recommended remediation | Refresh current recovery/diagrams; preserve historical intent; separately triage stale production comments. |
| Falsifiable acceptance | Current docs name default daemon, ACP/compile/stdio MCP seams and exact static-versus-runtime sequence limits; code citations resolve. |
| Validation method | Documentation claim-to-code walk and docs graph validation. |
| Disconfirming check attempted | Walked composition roots and tests; excluded intentionally dated historical architecture baseline from the accusation. |
| Dependencies | FR-001, FR-003 |
| Suggested owner / next skill | Documentation steward; `/document` |

## FR-010 — Core graph gateway is not the shipped stdio MCP tool surface

| Field | Proposed item |
|---|---|
| Kind / priority / status | todo / P3 / proposed |
| Category / scope | agent integration; `src/AiDe.Mcp/Tools.cs:22; src/AiDe.Core/Mcp/McpToolGateway.cs; WorkbenchShell.cs:2917` |
| Evidence / confidence | [Review FR-010](../reviews/forensic-review-rev102.md); Verified product-path distinction. The stdio dispatch exposes five identity/board/episode tools, while Core graph methods are a separate internal gateway. |
| Consequence | Agents cannot assume every internal graph operation is externally callable. |
| Recommended remediation | Choose a qualified bounded graph tool transport or document the internal-only boundary explicitly. |
| Falsifiable acceptance | Actual tools/list matches published capabilities, identity and limits; no advertised but unreachable graph operation. |
| Validation method | Stdio tool-list/query conformance probe against a real workspace. |
| Disconfirming check attempted | Inspected stdio schema/dispatch and daemon query paths; did not count tested Core methods as shipped MCP availability. |
| Dependencies | none |
| Suggested owner / next skill | Agent integration / Core; `/design-slice` |

## FR-011 — Dormant OTLP startup can report success before async bind failure

| Field | Proposed item |
|---|---|
| Kind / priority / status | risk / P3 / proposed |
| Category / scope | lifecycle; `src/AiDe.Core/Watcher/WatcherHost.cs:211–226; OtlpReceiver.cs:196–198` |
| Evidence / confidence | [Review FR-011](../reviews/forensic-review-rev102.md); Verified static; product path unintegrated. TryStartOtlp discards RunAsync and returns true; listener.Start occurs inside the async method. |
| Consequence | Future integration may lose startup failure and leave an abandoned task. |
| Recommended remediation | Own the receiver task and startup handshake only if this capability is integrated. |
| Falsifiable acceptance | Occupied/refused prefix yields observable failed startup; cancellation disposes the task/listener. |
| Validation method | Bind-failure and cancellation lifecycle probe. |
| Disconfirming check attempted | Read startup and receiver flow; no normal product caller found. |
| Dependencies | none |
| Suggested owner / next skill | Watcher / SRE; `/design-slice` |


FR-009 documentation corrections are partly delivered by the forensic documentation sweep; stale production comments remain unmodified and triage remains proposed. No remote issue was created.
