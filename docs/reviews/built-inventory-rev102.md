---
id: built-inventory-rev102
title: "AI-DE built and remaining inventory — revision 102"
type: doc
status: proposed
owner: "@timianmalloo"
phase: "assessment"
tags: [forensic-review, inventory, architecture, factory]
links:
  - { to: forensic-review-rev102, rel: documents }
  - { to: spec-ai-native-ide, rel: relates-to }
  - { to: architecture, rel: relates-to }
review-by: 2027-01-17
summary: "AI-DE built and remaining inventory — revision 102: repository evidence at 9f13ca7a, measured baseline, explicit confidence and proposed next work."
review-suggested: []
---

# Built versus still to build

Baseline: `9f13ca7a13c16eb1a7b4e3bd4027c20c640b6545`, 2026-10-17, pack revision 102.
Scope: all four source projects, shared contracts, tests, specs, design/mockup artifacts,
build/release configuration and supporting scripts. This is a broad bounded forensic assessment,
not a claim that every source line or native interaction was exercised.

**Built** means implementation and composition were located. It does not mean every acceptance
criterion passed. **Partial** has implemented substrate but missing integration/semantics or a
verified concern. **Specified/deferred** is intent, not a shipped surface. New factory capabilities
are scope extensions, not automatically defects in the existing release.

No aggregate completion percentage is asserted: features do not have equal scope and source
presence, meaningful test evidence and native readiness are different dimensions.

| Capability | Inventory status | Production / intent evidence | Available proof and limitation |
|---|---|---|---|
| Local WPF desktop shell | Built | src/AiDe.App/MainWindow.xaml.cs:29 | MainWindowViewModelTests; current App build passed |
| Four perspective navigation | Built | src/AiDe.Core/Workbench/Perspectives.cs:63–91 | Coding, Explore, Architecture, Coordination; Tests reserved |
| Docking, zones, layout restore | Built | src/AiDe.Core/Workbench/ZoneLayout.cs:219,260 | ZoneLayoutServiceTests; RestoredSessionDocumentsAreRevivedTests |
| Local per-workspace daemon | Built | src/AiDe.App/ViewModels/MainWindowViewModel.cs:252 | IpcTransportTests; daemon built in baseline |
| Connection-derived IPC identity | Built | src/AiDe.Core/Ipc/IpcPipe.cs:174,209,230 | IpcTransportTests; no fresh adversarial IPC suite run |
| Append-only fact store and snapshots | Built | src/AiDe.Core/Store/WorkspaceSchema.cs:16 | StoreImmutabilityTests; scope/writer invariant tests exist |
| Incremental fingerprints/reindex | Partial / verified defect | src/AiDe.Core/WorkspaceCore.cs:154–160,445–498 | Focused probe keeps old symbol on same-revision forced refresh |
| C# extraction | Built | src/AiDe.Core/Extraction/WorkspaceExtractors.cs:23 | CSharpExtractor; CallEdgeTests included in 57-test run |
| Python and TypeScript extraction | Built, static subset | src/AiDe.Core/Extraction/WorkspaceExtractors.cs:27–28 | Language tests exist; not full semantic/compiler coverage |
| Bicep extraction | Built, bounded semantics | src/AiDe.Core/Extraction/WorkspaceExtractors.cs:25 | BicepConstantFoldingTests; unsupported expressions disclosed |
| EF migration / SQL schema extraction | Built | src/AiDe.Core/Extraction/WorkspaceExtractors.cs:26,29 | SqlSchemaExtractorTests; Phase3ExtractorTests |
| Knowledge/frontmatter extraction | Built | src/AiDe.Core/Extraction/WorkspaceExtractors.cs:30 | Knowledge scopes and WalkingSkeletonTests |
| Evidence and provenance browsing | Built | src/AiDe.App/Workbench/SurfaceContentFactory.cs:153 | EvidencePaneTests; source presence is not fresh UI proof |
| Graph and bounded neighborhood queries | Built | src/AiDe.Core/Projections/IWorkspaceQueries.cs | GraphProjectionTests; omission/freshness semantics need review |
| Impact/path queries | Built | src/AiDe.Core/Projections/ProjectionService.cs | GraphPathsTests; bounded impact tests exist |
| Grouped large-repository overview | Built | src/AiDe.Core/Projections/IWorkspaceQueries.cs | GraphOverviewTests; no current scale benchmark executed |
| Solution tree / indexed-unindexed census | Built | src/AiDe.App/Workbench/SurfaceContentFactory.cs:181 | SolutionTreeProjectionTests |
| Entry-point listing | Partial / verified bound defect | src/AiDe.Core/Projections/EntryPointsProjection.cs | Heuristic classification; payload probe exceeds transport cap |
| Entry-point-driven full data flow | Specified / deferred | docs/specs/addendum-c-perspectives.md | D-2; current type-level static call flow is narrower |
| Native class diagram | Built, inferred association caveat | src/AiDe.App/Workbench/ClassHierarchyModel.cs:183–208 | ClassHierarchyAssociationTests; duplicate simple-name case missing |
| Static sequence diagram | Built, limited semantics | src/AiDe.App/Workbench/WorkbenchShell.cs:1482–1496 | InteractionTests; static ordered call sites, not runtime trace |
| Method/runtime-accurate sequences | Still to build | src/AiDe.Core/Projections/ProjectionService.cs:1168 | Current caller/callee scope is type-level |
| Crow’s-foot ER product surface | Specified / deferred | docs/specs/addendum-c-perspectives.md | D-3; mockup/documentation is not a registered ER view |
| Derived C4/layer/component product views | Specified / deferred | docs/specs/addendum-c-perspectives.md | D-4; committed documentation diagrams are separate |
| Read-only source viewer | Built | src/AiDe.App/Workbench/SurfaceContentFactory.cs:253 | CodeViewerView; no editable IDE proposed |
| Workspace content / symbol search | Built | src/AiDe.App/Workbench/WorkbenchShell.cs:707,2034 | SearchProviderBoundTests; stale scaffold comments are not current behavior |
| Sandboxed HTML knowledge reader | Built | src/AiDe.App/Workbench/HtmlSandboxPolicy.cs:29 | NodeContentHtmlKindTests; no fresh browser containment test run |
| ConPTY terminal | Built | src/AiDe.App/Workbench/SurfaceContentFactory.cs:195 | Terminal tests/probes exist; native runtime not launched here |
| Two-phase prompt receipts/recovery | Built | src/AiDe.Core/Dispatch/BoundaryDispatcher.cs:49,88 | BoundaryDispatchTests included in selected baseline |
| First-use/account/provider configuration | Built | src/AiDe.App/MainWindow.xaml.cs:270,322 | FirstUseTests; ProviderConfigurationTests |
| Multi-provider native catalog routes | Built routes; qualification separate | src/AiDe.Core/AgentPlane/EngineCatalog.cs | EngineCatalogTests; route presence does not qualify every installed harness |
| Conversation/composer/parallel send | Built | src/AiDe.App/Workbench/Sessions/SessionDocumentSurface.cs | ASendWhileATurnRunsOffersWaitOrParallelTests |
| Deterministic prompt templates/compilation | Compiler built; session catalog integration incomplete | src/AiDe.Core/Presentation/Composer/ComposerCompiler.cs | TemplateCompileIsDeterministicTests |
| Optional agentic prompt compilation | Built, gated | src/AiDe.App/Conductor/CompileCallHost.cs | Compile pin/admission tests; provider not invoked in review |
| Compilation history/provenance/purge | Built | src/AiDe.Core/Compilation/EnvelopeStore.cs:39,213 | TheEnvelopeStoreIsAppendOnlyTests; EnvelopePurge |
| Worktrees/advisory lease seams | Built | src/AiDe.Core/AgentPlane/WorktreeProvisioner.cs | AProvisionedWorktreeIsARealOneTests |
| Watcher sessions/board/ledger/standing | Built, evidence-dependent | src/AiDe.App/Workbench/SurfaceContentFactory.cs:210–226 | Watcher/query tests; evidence pointer is not semantic acceptance |
| Proof verification and scoring | Built, bounded meaning | src/AiDe.Core/Watcher/WeaveScore.cs:110 | ProofPackVerifierTests; no claim of automatic semantic correctness |
| Daydream candidate/promote lifecycle | Built, evidence-dependent | src/AiDe.Core/Watcher/DaydreamCandidate.cs:167 | Daydream tests; sparse capture affects useful output |
| OTLP receiver | Partial integration | src/AiDe.Core/Watcher/WatcherHost.cs:211 | No normal product caller of TryStartOtlp found |
| Independent periodic freshness probe | Primitive/tests only | src/AiDe.Core/Health/FreshnessProber.cs:24 | No production construction/call found |
| Upgrade / rollback orchestration | Partial integration | src/AiDe.Core/Upgrade/UpgradeCoordinator.cs:61 | Recovery path exists; full release rehearsal not performed |
| Core graph gateway available through shipped MCP | Not integrated | src/AiDe.Mcp/Tools.cs:22 | Five lifecycle/board tools; graph gateway remains separate |
| Spec-first work-item lifecycle / progress cockpit | Still to build | SurfaceContentFactory.Kinds and Core domain search | Existing goals/episodes/envelopes are substrate, not integrated obligation state |
| PR ingestion and guided human-review queue | Still to build | SurfaceContentFactory.Kinds and src capability search | No PR model/provider/query/workbench found |
| Spec / architecture / mockup drift engine | Still to build | src capability search; existing typed knowledge links | No semantic obligation comparison service found |
| Test coverage / worthiness / flake cockpit | Reserved / still to build | src/AiDe.Core/Workbench/Perspectives.cs:58 | Tests deliberately has no perspective row |
| General architecture / privacy / security rigor backlog UI | Still to build | SurfaceContentFactory.Kinds | Health incidents and governance documents are different inputs |
| Grounded turn summary with spec/test/diagram links | Still to build as integrated experience | Existing WorkEpisode and EnvelopeStore | Useful records exist; generated change-review packet does not |

## Original product stories

US-1 architecture inspection: implemented graph/type views, with framing/freshness caveats.
US-2 domain/data understanding: contexts and extracted schema/join evidence exist; full crow's-foot
ERM remains deferred. US-3 flow: static type-level call scenarios exist; runtime/method semantics
are narrower. US-4 knowledge: extraction/reader/search exist. US-5 coordination: session/worktree/
watcher substrate exists. US-6 deliberate dispatch: templates/compilation/receipts exist. US-7 audit:
watcher/ledger/proof paths exist, with import and capture caveats. US-8/9 workspace chrome/layout:
implemented. These are evidence-informed inventory judgments, not acceptance sign-offs.

## Remaining product layers for the requested evolution

Versioned accepted intent and obligations; change-set and PR snapshots; provider-neutral assessment
ingestion; criterion/code/assertion/run evidence links; semantic drift dispositions; guided human
review and revocable baseline-bound decisions; result/coverage/retry history; test worthiness and
efficiency analysis; architecture constraints and risk registers; evidence-grounded turn summaries.
The interactive proposal explores these additions while reusing the current desktop foundation.

## Rich authoring expansion

The [17-row authoring inventory](authoring-inventory-rev102.md) separates actual production wiring from tested-but-uncomposed mechanisms. CodeMirror Markdown, edit history and exact preview exist; a draft/revision prompt stack, arbitrary rich blocks and qualified multi-cursor behavior remain new. File/graph candidates, session catalog and unsent-session persistence need integration. The proposed rich editor migrates around the current compiler/send authority instead of rebuilding a CLI.
