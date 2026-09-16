---
id: proof-d0-atlas-independence
title: "D0 direct static independence from Atlas"
type: proof
status: proposed
owner: "@timianmalloo"
tags: [d0, atlas, regression, static-analysis]
links:
  - { to: spec-understanding-views, rel: depends-on }
  - { to: adr-0038-d0-solution-tree-census-and-kind, rel: depends-on }
  - { to: session-contracts, rel: depends-on }
review-by: 2026-12-15
summary: "Finite semantic D0 boundary, real-source binding, adversarial controls and 20 headless cases; independent review pending."
---

# Contract and scope

Goal: replace the obsolete global absence assertion with a nonempty **DIRECT STATIC** D0 independence check. Done when the exact D0 roots bind, direct Atlas dependencies and unaccounted helper edges fail, coexistence outside the boundary passes, and independent review accepts the frozen change. Not in scope: product changes, policy/spec edits, arbitrary transitive or runtime dependency proof, UI execution, native qualification or full App tests. Tier T2; author fan-out one; parent owns independent review and integration.

Author/session: codex-astra-d0-author / codex-d0-atlas-independence. Tree: C:/Projects/ai-de-fix-d0-atlas-independence; branch fix/d0-atlas-independence; base 247e6b4eaefba92cea250f657159a8768e8fb91f. Conductor provisioned the tree; no EnterWorktree. Parent authorized the semantic join after the user's conflict-resolution instruction and independent Test/architecture plan review. The parent records the normative interpretation: D0 is not implemented through Atlas; Atlas elsewhere may coexist. The no-Atlas/native policy sources remain unchanged.

The former assertion banned the entire Core Understanding directory and all loaded Understanding namespaces. The fresh red run confirms that it rejects the assembled repository. The grounded documents are understanding-views.md lines 312/427 and ADR0038 lines 203/225/278; this repair tests the D0 implementation boundary rather than treating repository absence as its proxy.

Execution graph: ground exact roots/shared handoffs → installed-Roslyn real-project spike → adversarial red → finite enforcement → focused qualification → frozen source/proof → independent review. Planned 24 tool boundaries/30 minutes; actual 24 boundaries, including two Conductor-requested guard corrections. Checkpoint at eight. No child agents. Surface list: stored census → solution-tree projection/frame → local/IPC query → solution-tree registration/factory builder → surface/shell bind, refresh, populate, retry, activation and graph reveal → direct-static test evidence. Domain, persisted representation and product behavior are unchanged.

# Exact finite boundary

The two complete files include every body, lambda and local function:

- src/AiDe.App/Workbench/SolutionTreeSurface.cs
- src/AiDe.Core/Projections/SolutionTreeProjection.cs

Selected direct members in the remaining six files:

| File/type | Members or row |
| --- | --- |
| Core/Projections/IWorkspaceQueries.cs | IWorkspaceQueries.SolutionTreeAsync; LocalWorkspaceQueries.SolutionTreeAsync |
| Core/Ipc/WorkspaceClient.cs | WorkspaceClient.SolutionTreeAsync |
| Core/Projections/ProjectionService.cs | SolutionTree (three overloads); ShrinkTree; TagSolutionTree; IsFilenameOnly; IsHostileArtifactPath; FramedCost(SolutionTreeResult); Weigh(SolutionTreeResult) |
| App/Workbench/WorkbenchShell.cs | BindSolutionTrees; RefreshSolutionTrees; RetrySolutionTreePopulate; PopulateSolutionTreesAsync; OnSolutionTreeActivateRequested; OnSolutionTreeShowGraphRequested; OnSolutionTreeRetryRequested; fields _solutionTreeGeneration and _solutionTreeCts |
| Core/Ipc/WorkspaceOperations.cs | SolutionTree constant; exactly one endpoint.Register(SolutionTree, ...) invocation, including its lambda |
| App/Workbench/SurfaceContentFactory.cs | exactly one implicit construction with literal solution-tree key, including its builder |

Observed baseline: **26 selected roots, 3976 symbol-reference observations, 58 distinct accepted shared symbols, zero errors**. Reference observations count syntax visits, not unique dependencies. Root cardinalities and exact paths fail closed when absent/duplicated. Newly referenced AiDe helpers outside selected roots or named shared ports fail UNACCOUNTED.

Named shared ports are the explicit SharedMembers/SharedTypes sets in the test; no whole AiDe namespace is allowed. They cover:

- PathComparison.ForThisFileSystem; UnanalysedLanguages.Skip.
- WorkspaceStore.BeginRead; StoreReader.FilesToSearch, ReadNodeKind, AllScopeLocations, CurrentSourceRevision.
- ProjectionService.CandidateWithinWorkspace, Activity, FrameBytes, MaxFramedGraphBytes, Wire.
- WorkspaceClient.QueryAsync; WorkspaceOperations.Handle and Refusable; DaemonEndpoint.Register; IpcResponse.Success; IpcRequest.
- PerspectiveSet.Architecture; Surface.Title; Perspective.
- RelayCommand constructor; NodeViewKind.Source, Read, GraphNeighbourhood.
- Factory Instances.One, SurfaceKind constructor, SurfaceEntry and Derived constructor.
- IWorkbenchAnnouncer.Announce; CanvasSurface, CodeViewerView, DockHost.
- Shell Announcer, Architecture, _queries, _lastSelectedNodeId, SurfaceContents, OpenKind, OpenNodeView, OpenCanvas, CentreOnAsync, OpenCodeViewers, ShowNodeInCodeViewersAsync.

A containing type is admitted when needed to refer to a named shared member; additional methods on that type remain unaccounted. The selected surface/store/frame types declared within complete roots are covered as roots.

Inspected shared handoffs: CandidateWithinWorkspace uses store scope locations and normalized path containment; the common skip policy remains shared. ShowNodeInCodeViewersAsync calls NodeContentSource.GetAsync and view Show. CentreOnAsync calls canvas.RefreshAsync. OpenNodeView routes the existing source/graph views. OpenKind uses the existing registration/navigation flow; the D0 calls use codeviewer/canvas keys. QueryAsync uses the existing IPC invoke/payload envelope. The D0 registration lambda calls ProjectionService.SolutionTree. No Atlas use was observed in these inspected handoff bodies. This observation does not establish arbitrary transitive independence beyond them.

# Binding contract and limits

The spike used the already installed Microsoft.CodeAnalysis.CSharp 4.14.0 dependency. No package/project changes. It loaded **346 source/generated trees and 259 managed references**. Separate real App/Core compilations are required: combining global usings polluted WPF Path binding with Core's System.IO import. The spike bound the initial 23 body roots without selected-source errors in approximately 2.27 seconds.

The final loader takes real source and exact generated global-using/WPF files from the current build configuration, plus installed runtime/build references. It does not emit or rerun source generators. Selected diagnostics fail closed; unrelated diagnostics outside selected roots do not imply successful whole-project compilation. Normal dotnet test build supplies current metadata. App-bound Core metadata symbols map back to current Core source by documentation identity; missing mappings fail. Actual source symbols, including lambdas/local functions, are not remapped as metadata.

Atlas refusal covers semantic namespace AiDe.Core.Understanding (and descendants), aliases/generic/array types and symbol source paths under src/AiDe.Core/Understanding/. Namespace-container aggregate locations are excluded from source-path attribution because AiDe spans the repository. Comments and string contents do not become semantic dependencies.

Project classification uses exact src/AiDe.Core/ or src/AiDe.App/ directory ancestry. A filename or workspace name containing the other project cannot classify a tree. Three adversarial directory cases kill the prior substring shape.

**Conditional compilation ceiling:** all eight boundary files must have no conditional directives or disabled text. This also catches directives surrounding selected roots. The parser does not guess current MSBuild defines; a future conditional edit anywhere in these files requires explicit qualification before changing this control. This deliberately conservative file-level rule is a bounded shortcut, documented inline. No current boundary file triggered it.

The contract stops at named shared ports. It does not prove arbitrary transitive callees, reflection, runtime dispatch, or string-based routing behavior. New helpers fail until classified; a change behind an existing shared port requires its own behavioral review. Source/model agreement requires a normal current build; stale no-build metadata is not the qualification recipe.

# Measured evidence

Raw logs/TRX are retained in this author tree under .artifacts/d0-independence/. They are local evidence, not committed product source. Parent retained the original integrated failure under the integration tree's .artifacts/atlas-five-gates/failed-slot-p1-01.

| Artifact stem | Actual result / exit | Meaning |
| --- | --- | --- |
| obsolete-red | 1 executed, 0 passed, 1 failed; exit 1 | Original global absence assertion rejects coexistence; TRX span 0.5810924 s |
| semantic-policy-red | 15 executed, 8 passed, 7 failed; exit 1 | Binding-only precursor did not reject direct/alias/lambda/local-function/shared-seam/path/new-helper dependencies; 7.276138 s |
| semantic-first-check | failed | Initial source-path attribution falsely treated namespace-container locations as Atlas; corrected by excluding namespace containers, plus discard-symbol normalization |
| semantic-green | 15 passed; exit 0 | Initial finite policy passed its 15 cases |
| semantic-final | 16 passed; exit 0 | Unrelated registration positive added; 8.4342754 s |
| semantic-freeze | 19 executed, 14 passed, 5 failed; exit 1 | Strict metadata mapping accidentally remapped real-source anonymous/local functions; corrected to metadata-only mapping |
| classification-mutant | 3 executed, 0 passed, 3 failed; exit 1 | Replacing exact ancestry with path.Contains(project) was detected; original exact ancestry restored |
| semantic-qualified | 19 passed; exit 0 | Corrected mapping/classification; 8.1369749 s |
| conditional-red | 1 executed, 0 passed, 1 failed; exit 1 | WINDOWS-conditional Atlas use disappeared as disabled trivia before the control |
| semantic-frozen | **20 executed, 20 passed, 0 failed/skipped; exit 0** | Final source; **8.7146163 s** TRX start-to-finish |

Final population: one real baseline, twelve negative mutation cases, four positive coexistence/shared-port cases, three directory-classification cases. Negative cases: direct, alias, lambda, local function, shared seam, forbidden source path with benign namespace, new helper, missing member, missing file, duplicate row, unresolved invocation, conditional Atlas. Positive cases: comments/strings, unused Atlas code outside roots, an unrelated code-atlas registration builder, and an explicit shared port. Mutations replace in-memory syntax trees; no product files were modified.

Final executed command:

    dotnet test tests/AiDe.App.Tests/AiDe.App.Tests.csproj --no-restore --filter 'FullyQualifiedName~SolutionTreeProbeTests.ProbeAtlas_' --logger 'trx;LogFileName=semantic-frozen.trx' --results-directory .artifacts/d0-independence -v q

Final source SHA256 (file bytes): A5338A154CE836CE8D321F012F2C3637280293B33C304AAD12153275CDE3123E. The earlier frozen-source-sha scratch receipt was overwritten after the last correction. Source restoration was read back before the final run. Existing enum, file-read, FactoryBuild and RepoRoot method bodies were compared with base and observed unchanged. FactoryBuild/STA, shown/native UI, full App and Core suites were not run in this unit.

# Class → sweep → derive → prevent

Class: DC118 width drift / invalid widening of a bounded implementation invariant to global repository absence. Sweep: inspected the D0 spec/ADR, eight actual boundary files and named shared handoffs; preserved the other enum/FileRead guards. Derive: Atlas coexistence and D0 independence are distinct; a nonempty finite semantic root set tests the latter. Prevent: required cardinalities, selected diagnostics, semantic/path refusal, new-helper refusal and meaningful negative/positive mutations.

Corrections within this unit were also class-shaped: project-name substring classification is prevented by three directory adversaries; conditional-code omission is prevented by the disabled-code mutant and finite file-level refusal; metadata/source identity confusion is prevented by real-source lambda/local-function positives plus metadata-only mapping. Namespace-container source-location aggregation was corrected without weakening type/member forbidden-path checks. Parent owns the central register and derived/audit union; this branch does not edit them.

# Handoff

Independent review is pending; the author does not clear it. Authored source manifest: tests/AiDe.App.Tests/SolutionTreeProbeTests.cs and this proof, plus the official own-session audit append. No product, project, policy, shared register or derived changes. AIDE_CONTRACT_LOG was absent in this harness, so no capture event destination was invented; this proof is named in the author close. Parent owns reviewed integration and any canonical broader qualification.

