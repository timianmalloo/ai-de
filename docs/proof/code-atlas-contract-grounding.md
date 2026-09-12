---
id: proof-code-atlas-contract-grounding
title: "Code Atlas contract grounding spike"
type: doc
status: draft
owner: "@timianmalloo"
phase: "atlas-contracts-spike"
tags: [proof, code-atlas, contracts, spike, grounding]
links:
  - { to: architecture, rel: relates-to }
  - { to: spec-addendum-c-perspectives, rel: relates-to }
review-by: 2027-03-12
review-suggested: []
summary: >-
  Source-only plus targeted baseline-test grounding for the current Code Atlas implementation. It
  records the exact contracts observed before architecture decisions: extraction, source identity,
  graph/query/read surfaces, IPC wiring, storage grain, WPF surface registration, missing seams and
  barriers. Product code was not changed.
---

# Code Atlas contract grounding spike

- **Session / branch:** `atlas-contracts-gpt55` on `atlas/contracts-spike`, base `b0e092b5f4176766f2e1870124665d9f74748d00`.
- **Scope:** current implementation only. No product code changes. No app run, migration, cloud API, provider SDK, push or merge.
- **Execution graph:** serial only. Fan-out cap was 0. Planning was needed because this touched contracts, storage, IPC, WPF surfaces and tests.
- **Evidence split:** `Executed` rows were covered by the targeted test command below. `Source-only` rows cite implementation hooks and remain contract grounding, not behavior proof.

## Runtime and package versions

| Contract | Evidence | Confidence |
|---|---|---|
| .NET SDK pinned to `10.0.303`; host runtime observed `10.0.11`; Windows desktop runtimes `8.0.28`, `10.0.9`, `10.0.11` installed. | `dotnet --info`; `global.json` contains `sdk.version = 10.0.303`, `rollForward = latestFeature`, `allowPrerelease = false`. | Verified |
| Tests target `net10.0`; core/app packages are centrally managed. | `tests\AiDe.Core.Tests\AiDe.Core.Tests.csproj:3`; `Directory.Packages.props:9-34`. | Verified |
| Relevant NuGet versions: Roslyn `Microsoft.CodeAnalysis.* 4.14.0`, `Microsoft.Build.Locator 1.9.1`, SQLite `Microsoft.Data.Sqlite 10.0.11`, MCP `2.2.0`, xUnit `2.9.3`, test SDK `17.14.1`, AvalonEdit `6.*`, AvalonDock `5.0.0`, WebView2 `1.0.3485.44`, YamlDotNet `18.1.0`. | `Directory.Packages.props:11-34`. | Verified |

## Commands and results

| Command | Result | Notes |
|---|---|---|
| `dotnet test AiDe.sln --no-restore --list-tests --filter ...` | Exit 0, no listed tests. | Not treated as proof; output did not show discovered tests. |
| `dotnet build tests\AiDe.Core.Tests\AiDe.Core.Tests.csproj --no-restore --nologo --verbosity:minimal` | Failed with `NETSDK1004` missing `tests\AiDe.Core.Tests\obj\project.assets.json`. | This satisfied the no-restore-first rule and justified restore. |
| `dotnet restore tests\AiDe.Core.Tests\AiDe.Core.Tests.csproj --nologo --verbosity:minimal` | Restored Core, Daemon, MCP and probe projects in 149 ms. | No manifest or dependency additions were made. |
| `dotnet test tests\AiDe.Core.Tests\AiDe.Core.Tests.csproj --no-restore --filter "FullyQualifiedName~CSharpExtractorTests|FullyQualifiedName~CallEdgeTests|FullyQualifiedName~TypeMembersTests|FullyQualifiedName~WorkspaceIndexTests|FullyQualifiedName~GraphProjectionTests|FullyQualifiedName~DescribeCarriesKnowledgeTests|FullyQualifiedName~NodeContentTests|FullyQualifiedName~InteractionTests" --logger "trx;LogFileName=atlas-contracts.trx" --results-directory C:\Users\malla\.copilot\session-state\45bbc625-37e9-40a8-a2d0-8b39402c8e90\files\atlas-contracts-test-results` | `Passed! Failed: 0, Passed: 79, Skipped: 0, Total: 79, Duration: 2 s`. TRX counters: `total=79 executed=79 passed=79 failed=0`. | Targeted baseline for extraction, calls, members, workspace discovery, graph projection, describe knowledge, node content and interaction. TRX persisted in session artifact only: `files\atlas-contracts-test-results\atlas-contracts.trx`. |

## Contract table

| Surface | Current contract | Hook and evidence | Minimal falsifier | Status |
|---|---|---|---|---|
| Physical inventory and routing | `WorkspaceExtractors.Default()` composes known extractors; routed kinds are explicit. C# scopes are one project/TFM, not one repository. | `src\AiDe.Core\Extraction\WorkspaceExtractors.cs:19-39`; `tests\AiDe.Core.Tests\WorkspaceIndexTests.cs:56`, `:74`, `:152`. | Add a project under `obj` and see it indexed; add an unread language and get an empty-success summary. | Executed |
| Path safety and ignored/unindexed files | Fingerprints skip `bin`, `obj`, `.git`, `.vs`, `node_modules`, `artifacts`, `packages`, `TestResults`; unread language survey discloses unanalysed source. Node content is resolved only from scope location + provenance under the workspace root. | `src\AiDe.Core\Extraction\ScopeFingerprints.cs:90-99`; `src\AiDe.Core\WorkspaceCore.cs:504`; `src\AiDe.Core\Projections\ProjectionService.cs:1126-1210`; `tests\AiDe.Core.Tests\NodeContentTests.cs:98`, `:125`. | Ask for `../../../../Windows/System32/drivers/etc/hosts`; expected `NodeContentKind.None`. Put source in ignored build output; expected not indexed. | Executed |
| Snapshot, revision and source-read ownership | `RefreshScopeAsync` stamps caller revision with `ScopeFingerprints.ExtractorGeneration`, reuses latest complete matching snapshot, increments a durable generation for a new desired extraction, and stores scope location as `declared_at`. | `src\AiDe.Core\WorkspaceCore.cs:127-210`; `src\AiDe.Core\Extraction\SourceRevision.cs:31-57`; `src\AiDe.Core\Extraction\ScopeFingerprints.cs:90`; `src\AiDe.Core\Store\StoreReader.cs:493-501`. | Upgrade extractor generation and re-index same repo; expected not to silently reuse the old reader's facts. | Source-only here; separate tests exist in `UpgradingTheExtractorReExtractsTests`, not run in this targeted command. |
| C# type identity and SourceSpan | `CSharpExtractor` emits fully displayed type IDs, `has_type`, `declared_in`, `inherits`, `implements`/dependency facts and provenance as path relative to the project directory plus `line:column`; fallback is project file `1:1`. | `src\AiDe.Core\Extraction\CSharpExtractor.cs:26-41`, `:170-215`, `:474-493`; `tests\AiDe.Core.Tests\CSharpExtractorTests.cs:64-88`. | Type declared on line N must return file and `N:col`; missing project must be incomplete, not crash. | Executed |
| `has_member` strings | Members are formatted UML compartment text (`+ Name : string`, `# Method(int) : string`), own members only, skip implicit/generated/accessors/nested types, cap at `MaxMembersPerType = 40`, disclose `members_truncated` count. They are attributes, not independently addressable nodes. | `src\AiDe.Core\Extraction\CSharpExtractor.cs:192-210`, `:505-568`; `src\AiDe.Core\Projections\ProjectionService.cs:392-406`; `tests\AiDe.Core.Tests\TypeMembersTests.cs:56`, `:78`, `:116`, `:161`, `:210`. | A method selected by name cannot be addressed as a node unless a new method-node contract is added. A type with 52 members should return 40 plus declared count. | Executed |
| Method nodes | There is no independently addressable method node in current extraction. Methods exist as formatted `has_member` strings and call-site message names only. | Same hooks as above plus `calls_at` object shape below. | User clicks `Shop.Order.Run()` and expects `NodeContent` for a method; current seam can only serve `Shop.Order`. | Named gap |
| Calls and `calls_at` semantics | `calls` is deduplicated type-to-type relation. `calls_at` keeps every call site in call order, encoded as object `${callee}#${member}@${line:col}` so repeated calls survive the store natural key. `calls_at` is an attribute and never drawn as a graph edge. | `src\AiDe.Core\Extraction\CSharpExtractor.cs:360-420`; `src\AiDe.Core\Projections\ProjectionService.cs:936-947`; `tests\AiDe.Core.Tests\InteractionTests.cs:72`, `:90`, `:108`, `:136`, `:151`; `tests\AiDe.Core.Tests\CallEdgeTests.cs:70`, `:117`, `:541`. | `A -> B, A -> C, A -> B` should produce three interaction messages but only one `calls` edge per pair; self-call should not become a lifeline message. | Executed |
| Graph contract and limits | `GraphQuery` filters by max nodes, kinds, scope, include-external, group and excluded edge predicates before caps. Default max nodes is `5_000`. Attributes and disclosures are not edges. Result reports omitted nodes, disclosures, source revision and declared totals by kind. | `src\AiDe.Core\Projections\GraphProjection.cs:1-140`, `:140-250`; `tests\AiDe.Core.Tests\GraphProjectionTests.cs:26`, `:44`, `:61`, `:95`, `:170`, `:247`. | Apply `Kinds=[class]` after cap and return wrong nodes; draw `has_type`/`calls_at` as edges; omit denominator for kind totals. | Executed |
| Describe contract and limits | `Describe(nodeId,maxNeighbors)` clamps neighbors, reads touching assertions in SQL, reports `ResultBounds`, neighbor kinds, own members and declared member count, plus knowledge IDs for described node and neighbors. | `src\AiDe.Core\Projections\ProjectionService.cs:358-430`; `tests\AiDe.Core.Tests\DescribeCarriesKnowledgeTests.cs:55`, `:79`, `:103`, `:120`. | A knowledge neighbor rendered from a code node lacks `KnowledgeIds`; a god type's own members are starved by neighbor cap. | Executed |
| NodeContent contract and limits | Node content is on-demand, authority-side, path-confined, and capped at `MaxContentBytes = 256 KiB`. It returns `NodeId`, `RenderKind`, `Language`, `Content`, and `Shortfall`. Kinds are `Code`, `Text`, `None`. | `src\AiDe.Core\Projections\NodeContent.cs:1-44`; `src\AiDe.Core\Projections\ProjectionService.cs:1126-1245`; `tests\AiDe.Core.Tests\NodeContentTests.cs:69`, `:83`, `:98`, `:125`, `:212`. | Oversize file must say what was left; path escape must return `None`; client must not read file directly. | Executed |
| Interaction contract and limits | Interaction returns type-level outgoing messages in source order: `Ordinal`, `From`, `To`, `Member`, `Location`, plus `Truncated`, `Bounds`, `SourceRevision`. It is not method-level activation. | `src\AiDe.Core\Projections\Interaction.cs:1-24`; `src\AiDe.Core\Projections\ProjectionService.cs:947-980`; `tests\AiDe.Core.Tests\InteractionTests.cs:72-168`. | Repeated calls collapse; member name omitted; caller with no outgoing calls throws. | Executed |
| IWorkspaceQueries seam | The read surface is async and returns Core result types: Describe, Impact, Find, SearchContent, Interaction, Knowledge, NodeContent, Evidence, Graph, Paths, Overview. Local implementation delegates to `ProjectionService` without `Task.Run`. | `src\AiDe.Core\Projections\IWorkspaceQueries.cs:20-160`. | App creates parallel view DTOs or reads files locally; a remote daemon later requires a second UI path. | Source-only |
| IPC dispatch/version/result shape | Operations are explicit request records; command ids include `describe`, `impact`, `find`, `search-content`, `interaction`, `knowledge`, `nodeContent`, `evidence`, `graph`, `paths`, `overview`, `dispatch.begin`, `dispatch.finalize`, `index.solution`. Wire JSON uses web defaults and string enums. Register handlers wrap reads in `Refusable` and return `IpcResponse.Success(project(body), Wire)` or error. | `src\AiDe.Core\Ipc\WorkspaceOperations.cs:16-82`, `:129-230`, `:286-328`; `src\AiDe.Core\Ipc\IpcContract.cs:13-24`, `:100-167`; `src\AiDe.Core\Ipc\WorkspaceClient.cs:83-181`, `:238-260`. | Unknown command should be refused, unsupported version should report supported versions, enum renumbering must not alter wire meaning. | Source-only for this command; daemon operation tests were discovered but not selected. |
| Fact/dimension store | Dimensions: `workspace_dim`, `node_dim`, `session_dim` with Type-2 intervals where meaning changes. Facts are append-only tables with immutability triggers: evidence assertions, desired generations, committed snapshots, command receipts, dispatch attempts/outcomes, prompt revisions. `claim_current_cache` is the labelled mutable rebuildable cache. | `src\AiDe.Core\Store\WorkspaceSchema.cs:4-21`, `:28-120`, `:120-215`; `src\AiDe.Core\Store\StoreWriter.cs:169-220`. | `UPDATE`/`DELETE` against a fact table must abort; `node_dim` unchanged upsert must not rewrite history. | Source-only here; store-specific tests not in selected command. |
| Scope replacement and stale writers | A worker may commit a snapshot only when its generation/revision still matches the durable desired pair; late/stale commits throw `ScopeGenerationStale`. Refreshes that fail leave last complete snapshot rendering and record incidents/disclosures. | `src\AiDe.Core\Store\StoreWriter.cs:64-95`; `src\AiDe.Core\WorkspaceCore.cs:127-210`, `:435-515`; `src\AiDe.Core\Store\StoreReader.cs:18-35`. | Start generation 1, then desire generation 2; generation 1 commit must be rejected rather than deleting newer evidence. | Source-only here |
| Projection and node categories | Core node kinds come from `has_type`; coarse knowledge class is a declared `node_class=knowledge`. Graph totals are by producer kind, not UI category. Surface categories remain App concern. | `src\AiDe.Core\Projections\GraphProjection.cs:35-132`; `src\AiDe.Core\Projections\ProjectionService.cs:656-675` (knowledge IDs); `tests\AiDe.Core.Tests\GraphProjectionTests.cs:135`, `:154`; `DescribeCarriesKnowledgeTests.cs:55-127`. | A repo-defined spec kind not in a hard-coded UI list must still count as knowledge. | Executed |
| WPF composition seam | `SurfaceContentFactory.Kinds` is the descriptor list and row order is menu order. Architecture currently admits Graph/canvas, Evidence/view, Provenance/inspector, Class diagram, Sequence diagram, Contexts, Joins, and shared Code viewer. Code viewer is admitted by both Coding and Architecture. | `src\AiDe.App\Workbench\SurfaceContentFactory.cs:94-128`, `:173-188`, `:193-200`, `:255-259`; `tests\AiDe.App.Tests\Workbench\PerspectiveMenuTests.cs:203`, `:223-229`, `:313-321`, `:503-512`, `:624-637`. | New Architecture kind row should appear in menu/palette/routing without a second list edit; `sequence` opened from Coding should route to Architecture. | Source-only in this command; app tests not run because product-code change is forbidden and Core contract tests were the minimal baseline. |
| Visual-first / file-first acceptance intent | Current seams support graph-first plus selected-node file read (`Graph`, `Describe`, `NodeContent`) and sequence/class diagrams. They do not yet provide five altitude levels, semantic layer overlays, method-level UML activation, or implementation/spec/decision lineage as one query. | Existing hooks above; root docs/specs may name absent capabilities. | A spec asks for Azure semantic layers or decision lineage and expects one query result; current implementation requires composition from graph/query/doc data or new contracts. | Named gap |
| Model analysis harness | No direct provider SDK was used or chosen. Existing app has governed host code, but this spike did not run model analysis. Bound model analysis must remain through the approved runtime harness. | `src\AiDe.App\Conductor\GovernedRunHost.cs` found as existing surface by inventory; not opened because model analysis was not executed. | Adding a new provider SDK to architecture without read+execute evidence would violate the task boundary. | Barrier |

## Reusable seams already present

1. **Core read seam:** `IWorkspaceQueries` is the right client boundary for Atlas reads. Reuse it instead of App-side file access.
2. **Projection service:** `ProjectionService` already owns bounded `Graph`, `Describe`, `Interaction`, `NodeContent`, `Paths`, `Overview`, `Knowledge`, `Evidence`, `Find` and content search semantics.
3. **Extractor shape:** `CSharpExtractor` already emits type, relation, member and call-site facts with source provenance. It does not emit method nodes.
4. **Store grain:** the existing SQLite store is already fact/dimension shaped. Use append-only facts and current/read projections instead of a second Atlas store.
5. **WPF registry:** `SurfaceContentFactory.Kinds` + `PerspectiveMenu` is the current composition seam for Architecture perspective surfaces.

## Barriers and named gaps

| Gap | Why it matters | Recommendation |
|---|---|---|
| No method node identity. | Concrete UML sequence and member navigation cannot independently address a method body today. | First design decision: either keep Atlas type-level and say so, or add a method-symbol fact shape. Do not parse `has_member` strings as identity. |
| `has_member` is display text. | It is correct for class compartments and wrong as a durable member contract. | Treat it as presentation payload only. If decisions need member identity, add a new predicate/fact shape with provenance. |
| Sequence is type-level only. | A lifeline-per-method diagram would overclaim current evidence. | Minimal vertical slice should render type-level sequence from `Interaction` first, with UI copy stating the altitude. |
| Five altitudes absent as one contract. | User intent asks for five altitudes; current graph has filters, paths and overview but not an altitude vocabulary. | Define altitude terms against existing queries before adding storage. Start with file/type/member? only after method-node decision. |
| Azure semantic layers absent as one contract. | Current kinds/scopes are generic; Azure-specific layers need extractor/domain facts or a projection taxonomy. | Reuse `GraphQuery.Kinds`, `ScopeId`, `GroupId`, and `Overview`; add layer classification only from extracted evidence, not from path names alone. |
| Implementation/spec/decision lineage is not one query. | `Knowledge`, graph edges and docs graph exist, but Code Atlas needs concrete lineage between implementation, specs and decisions. | First slice: one selected code node -> existing `NodeContent` + `Describe` + `Paths`/`Knowledge` links if present. Name missing links as shortfall. |
| App Architecture tests not executed in this spike. | Surface registry hooks are source-grounded, not executed here. | Conductor/Shell owner should run `PerspectiveMenuTests` and `SurfaceContentTests` when seam approval allows Shell-owned paths. |
| Store immutability not executed in this spike. | Storage contract is source-grounded only. | If architecture depends on store mutation guarantees, run the smallest Store tests or propose an isolated store spike path. |

## Conductor disposition: barrier before vertical slice

The earlier type-level slice and type-only recommendation are not adopted. Owner requires an integrated deterministic C# physical file -> type/member -> actual source walking skeleton, with a native Architecture journey over a real selectable workspace. Unsupported and unindexed files must remain visible. `NodeContent` for a selected indexed type is not a solution explorer, and a silent type-only scope cut is not allowed.

**Barrier:** physical-inventory and member-ID contracts must be designed and admitted before implementation. The existing source findings still hold: `has_member` is display text, no method/member node identity exists, and `Interaction` is type-level. These are blockers to the required journey, not reasons to complete with a type-only Atlas.

## Proof and residual risk

| Claim | Evidence | Oracle / falsifier | Confidence |
|---|---|---|---|
| Current C# extractor/member/call/query/path contracts have baseline tests. | Targeted `dotnet test` selected 79 tests; 79 executed and passed. | Listed test classes include repeated call, member truncation, graph attributes, path escape, knowledge carry and workspace discovery cases. | Verified |
| IPC, store and WPF registry hooks exist with cited source lines. | Source citations above. | A focused IPC/store/WPF test run could still fail; this spike did not execute them. | Source-only |
| No production code was changed. | Git diff before report creation was empty; only this report is intended for commit. | `git status --short` after commit must show clean. | To verify at close |

## Recommendations to the spec author

- Treat physical inventory and member identity as required contracts, not optional later enhancements.
- Use current Core query names and result records as source evidence only; do not declare the native Architecture journey complete until physical file inventory and addressable member IDs are admitted.
- Put five altitudes in the spec as user-facing navigation levels, with member/method altitude blocked until identity and source-span contracts exist.
- Keep TheTerrace as read-only acceptance corpus. Do not copy it as a production fixture.
- Keep model-backed analysis out of the first slice unless it runs through the approved harness and has an eval/proof path.


## Bounded synthetic Roslyn and isolated contract-test research permitted

- **Owner ruling:** Bounded synthetic Roslyn and isolated contract-test research permitted; agent61e506... turn1.
- **Boundary:** research only. No tracked source/project edits, no package upgrades, no network-intended dependency changes, no TheTerrace/user data/model/cloud calls.
- **Ignored probe area:** `.agents\artifacts\atlas-contracts-gpt55\roslyn-probe\Probe.csproj`; verified with `git check-ignore --quiet` before creation.
- **Probe artifacts kept ignored:** `.agents\artifacts\atlas-contracts-gpt55\roslyn-probe\Program.cs`, `Probe.csproj`, `raw-output.txt`, `bin/`, `obj/`.

### Roslyn synthetic-source probe

| Item | Evidence |
|---|---|
| Exact command | `dotnet run --project .agents\artifacts\atlas-contracts-gpt55\roslyn-probe\Probe.csproj --no-restore` after an ignored probe-project restore with `dotnet restore .agents\artifacts\atlas-contracts-gpt55\roslyn-probe\Probe.csproj --ignore-failed-sources --nologo --verbosity:minimal`. |
| Loaded assembly versions | Raw output reported `Microsoft.CodeAnalysis 4.14.0.0` and `Microsoft.CodeAnalysis.CSharp 4.14.0.0`. |
| Synthetic inputs | Scope `csharp:P1:net10.0`: two syntax trees, `P1/Widget.Part1.cs` and `P1/Widget.Part2.cs`, with `namespace Same; public partial class Widget`, constructors, property `Name`, expression property `Count`, overloads `Save(int)`/`Save(string)`, and partial method declaration/implementation `Hook`. Scope `csharp:P2:net10.0`: `P2/Widget.cs` with the same `Same.Widget`, `Name`, and `Save(int)`. |
| Observed API behavior | Roslyn documentation IDs are semantic but not scope-qualified: both scopes emitted `T:Same.Widget`, and `Save(int)` doc IDs also compared equal across scopes. Partial type symbol carried two source locations. Constructors, properties, property accessors, overloads and partial methods all had documentation IDs and source spans. Missing type/member declaration IDs resolved to null. |
| Falsifying outcome | If documentation IDs were globally unique across projects/scopes, `COLLISION_ACROSS_SCOPES typeDocEqual` and `memberDocEqual` would be `False`; observed `True`, so any member identity design must include a scope/project axis. |

Raw probe output:

```text
ASSEMBLY|Microsoft.CodeAnalysis|4.14.0.0
ASSEMBLY|Microsoft.CodeAnalysis.CSharp|4.14.0.0
TYPE|csharp:P1:net10.0|Same.Widget|doc=T:Same.Widget|locs=P1/Widget.Part1.cs:2:22-2:28,P1/Widget.Part2.cs:2:22-2:28
MEMBER|csharp:P1:net10.0|Method|Same.Widget.Widget()|doc=M:Same.Widget.#ctor|locs=P1/Widget.Part1.cs:4:12-4:18
MEMBER|csharp:P1:net10.0|Method|Same.Widget.Widget(int)|doc=M:Same.Widget.#ctor(System.Int32)|locs=P1/Widget.Part1.cs:5:12-5:18
MEMBER|csharp:P1:net10.0|Method|Same.Widget.Count.get|doc=M:Same.Widget.get_Count~System.Int32|locs=P1/Widget.Part1.cs:7:25-7:26
MEMBER|csharp:P1:net10.0|Method|Same.Widget.Name.get|doc=M:Same.Widget.get_Name~System.String|locs=P1/Widget.Part1.cs:6:26-6:29
MEMBER|csharp:P1:net10.0|Method|Same.Widget.Hook()|doc=M:Same.Widget.Hook|locs=P1/Widget.Part1.cs:9:18-9:22
MEMBER|csharp:P1:net10.0|Method|Same.Widget.Save(int)|doc=M:Same.Widget.Save(System.Int32)|locs=P1/Widget.Part1.cs:8:17-8:21
MEMBER|csharp:P1:net10.0|Method|Same.Widget.Save(string)|doc=M:Same.Widget.Save(System.String)|locs=P1/Widget.Part2.cs:4:17-4:21
MEMBER|csharp:P1:net10.0|Method|Same.Widget.Name.set|doc=M:Same.Widget.set_Name(System.String)|locs=P1/Widget.Part1.cs:6:31-6:34
MEMBER|csharp:P1:net10.0|Property|Same.Widget.Count|doc=P:Same.Widget.Count|locs=P1/Widget.Part1.cs:7:16-7:21
MEMBER|csharp:P1:net10.0|Property|Same.Widget.Name|doc=P:Same.Widget.Name|locs=P1/Widget.Part1.cs:6:19-6:23
TYPE|csharp:P2:net10.0|Same.Widget|doc=T:Same.Widget|locs=P2/Widget.cs:2:14-2:20
MEMBER|csharp:P2:net10.0|Method|Same.Widget.Name.get|doc=M:Same.Widget.get_Name~System.String|locs=P2/Widget.cs:4:26-4:29
MEMBER|csharp:P2:net10.0|Method|Same.Widget.Save(int)|doc=M:Same.Widget.Save(System.Int32)|locs=P2/Widget.cs:5:17-5:21
MEMBER|csharp:P2:net10.0|Method|Same.Widget.Name.set|doc=M:Same.Widget.set_Name(System.String)|locs=P2/Widget.cs:4:31-4:34
MEMBER|csharp:P2:net10.0|Property|Same.Widget.Name|doc=P:Same.Widget.Name|locs=P2/Widget.cs:4:19-4:23
COLLISION_ACROSS_SCOPES|typeDocEqual=True|p1=T:Same.Widget|p2=T:Same.Widget
COLLISION_ACROSS_SCOPES|memberDocEqual=True
MISSING_ID|nullType=True|nullMember=True
```

### Existing isolated contract suites

| Item | Evidence |
|---|---|
| Fixture isolation inspected | `UpgradingTheExtractorReExtractsTests` uses `Path.GetTempPath()` + `Guid.NewGuid()` and deletes in `Dispose`; `StoreImmutabilityTests` and `StoreCompactionTests` use `TestWorkspace.Create()`/disposable temp stores; `BoundaryDispatchTests` and `DaemonOperationsTests` use `TestWorkspace.Create()` plus unique pipe names from `Guid.NewGuid()`. |
| Exact command | `dotnet test tests\AiDe.Core.Tests\AiDe.Core.Tests.csproj --no-restore --filter "FullyQualifiedName~UpgradingTheExtractorReExtractsTests|FullyQualifiedName~StoreImmutabilityTests|FullyQualifiedName~StoreCompactionTests|FullyQualifiedName~BoundaryDispatchTests|FullyQualifiedName~DaemonOperationsTests" --logger "trx;LogFileName=atlas-contracts-batch2.trx" --results-directory C:\Users\malla\.copilot\session-state\45bbc625-37e9-40a8-a2d0-8b39402c8e90\files\atlas-contracts-test-results-2 --verbosity:minimal`. |
| Readback counts | Console and TRX agreed: `Failed: 0, Passed: 40, Skipped: 0, Total: 40`; TRX counters `total=40 executed=40 passed=40 failed=0 skipped=0`. |
| Covered contract classes | Source-revision re-extraction, fact-store immutability/compaction, boundary dispatch IPC, and daemon operation payload/refusal/version paths. |

### Observed API behavior vs proposed identity design

| Observed behavior | Design implication |
|---|---|
| Roslyn `DocumentationCommentId` distinguishes overloads (`Save(System.Int32)` vs `Save(System.String)`), constructors (`#ctor` with overload parameters), properties (`P:` IDs), accessors (`get_`/`set_`) and partial methods. | It is usable as one component of member identity. |
| Roslyn IDs do not include project/scope; same namespace/type/member in two compilations collided. | Member IDs for Atlas must include at least scope/project identity plus documentation ID, and likely source path/span for display/navigation. |
| Partial type locations are multiple; partial method declaration/implementation collapsed to one symbol with one observed implementation span in this probe. | Source walking must support multiple declaration spans for types and must define how declaration vs implementation spans are represented for partial members. |
| Missing declaration IDs resolve to null. | Query contracts must return explicit not-found/shortfall results, not fabricate member nodes. |



### Data reviewer correction: evidence IDs are not source versions

- **Reviewer finding:** `NodeContent` resolves the indexed provenance to a workspace path and then reads the current live bytes. The resolved path is fenced to the workspace, but the content bytes are not bound to the source revision or a recorded hash. Scope fencing proves path containment; it does not prove whole-workspace coherence.
- **Version distinction:** Roslyn documentation IDs identify declarations semantically, not a source version. The existing probe shows overload IDs are distinct (`M:Same.Widget.Save(System.Int32)` vs `M:Same.Widget.Save(System.String)`) and the same method ID collides across two project scopes, requiring scope/project qualification. The same ID would also remain the same under whitespace or line movement while the source span changes, so it cannot stand in for declaration-version identity.
- **First-horizon effect:** the native Architecture journey must design/admit a physical-inventory contract that binds selectable files/members to a coherent source version or hash before it can claim actual-source walking. Human acceptance can approve this constraint; it must not be promoted from AI-origin text into implementation without the admitted contract.

### Updated barrier

Physical-inventory and member-ID contracts remain first-order barriers. The new probe strengthens, not relaxes, the prior correction: addressable members are feasible with Roslyn 4.14 evidence, but only if the identity design includes scope/project qualification and explicit source-span semantics. A type-only `NodeContent` journey remains insufficient for Owner's native Architecture requirement.
