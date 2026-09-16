---
id: proof-d0-atlas-independence
title: "D0 direct static independence from Atlas"
type: doc
status: proposed
owner: "@timianmalloo"
tags: [d0, atlas, regression, static-analysis]
links:
  - { to: spec-understanding-views, rel: depends-on }
  - { to: adr-0038-d0-solution-tree-census-and-kind, rel: depends-on }
  - { to: session-contracts, rel: depends-on }
review-by: 2026-12-15
summary: "Exact shared declaration authority and scoped conditional coverage; 37 headless cases, independent re-review pending."
---

# Original attempt (historical; superseded below)

The original 24-call author unit and its results are retained. Independent review BLOCKed FR-001 and FR-002; the correction below supersedes the name-family admission and eight-file conditional ban.

## Contract and scope

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

Raw logs/TRX are retained in this author tree under .artifacts/d0-independence/. They are local evidence, not committed product source. Parent retained the original integrated failure under the integration tree's artifacts/atlas-five-gates/failed-slot-p1-01.

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


# Bounded FR-001 / FR-002 correction

Author/session: codex-astra-d0-author / codex-d0-atlas-correction. Tree C:/Projects/ai-de-fix-d0-atlas-correction, branch fix/d0-atlas-correction, base bbca8d6edc495b150c502999fd0fcc69fa922c72. The original 24/24 episode remains history. This independently authorized correction has a 16-call / 20-minute ceiling, checkpoint 12; final author boundary 16. No additional agents, product/project edits, dependency changes, UI/STA/native/shown/full-suite tests, join or push. The original independent BLOCK receipt remains unchanged; only the retained reviewer may clear it.

Goal: exact shared declaration authority and conditional coverage limited to D0 roots and their binding contexts. Done when the reviewer counterexamples and retained cases have observed red/green evidence and frozen source/proof are handed to independent review. Tier T2; width one. The parent already recorded the programme graph delta. Local graph: read BLOCK → measured Roslyn spike and exact port inventory → red controls → minimum identity/conditional correction → discriminating width controls → focused qualification → audit/freeze/release. Surface list and direct-static limits remain those above. Domain/persistence/UI behavior is unchanged.

## Exact semantic admission (FR-001)

SharedPorts is a frozen, reviewed-source manifest of **60 exact source-path + OriginalDefinition documentation-ID entries**, yielding the same **58 observed admitted identities** at baseline (two additional explicit identities are already-covered root-owner types). It is never discovered or refreshed at runtime. Each entry must resolve to exactly one symbol in its explicit AiDe.Core or AiDe.App source compilation and retain its exact path. Missing, multiple or unmapped identities refuse. Core metadata-to-source resolution is also unique. Members, constructors, generic arity and parameter signatures remain in the identity; a declaring type's admission does not admit its members.

For example, the admitted handoff is:

    src/AiDe.App/Workbench/WorkbenchShell.cs|M:AiDe.App.Workbench.WorkbenchShell.OpenKind(AiDe.App.Workbench.DockHost,System.String,System.Boolean)

The injected OpenKind(System.Int32) is UNACCOUNTED. The existing shared-port positive remains admitted. Moving PathComparison's declaration to another source path fails the frozen authority check. Constructed type and method arguments are inspected before OriginalDefinition normalization, including the permanent generic-Atlas negative.

The source-defined System.ReviewEscape counterexample was measured to escape the prior namespace-only exemption. Framework exemptions now require metadata origin and one of the explicit .NET/WPF framework public-key tokens, as well as the narrow existing namespace condition. Source-defined System types are not exempt. Tuple field projections are normalized to their underlying framework ValueTuple type; their constructed arguments are still inspected. This is an assembly-authority check, not a claim to validate cryptographic binaries or arbitrary runtime dispatch.

## Conditional scope (FR-002)

The installed Roslyn 4.14 spike showed If/EndIf directive spans, inactive alias/body DisabledTextTrivia, and same-offset inactive method/import syntax after directive characters were replaced by spaces. Output is retained in .artifacts/d0-correction/spike.log; Program.cs and Spike.csproj are retained below spike/. The first spike compile failed for a missing System.IO import in scratch only; the correction succeeded. No product dependency was added.

The conditional check exposes all branch syntax at original offsets. A semantic model of that view is used only to classify declaration owners, never to certify an executable all-branches program or claim Debug/Release compilation coverage. Unknown declaration identity and ambiguous syntax in a relevant region refuse.

Protection covers:

- Both complete D0 files, including trailing inactive declarations beyond the last active token.
- Selected roots and regions enclosing/concealing them. An entirely missing inactive selected root fails ROOT.
- Referenced source declarations/shared ports, conditional imports in relevant files and conditional global imports in any source.
- Inactive definitions capable of changing a selected binding, including a partial owner introduced in another source file.

Binding-risk keys include the exact declaring-type documentation identity plus member name. This intentionally considers all overloads of the same owner when determining whether a *conditional definition could change binding*; it does not grant shared-port admission, which always uses the full signature and source authority. A same-named method on an unrelated owner remains admitted. Unrelated conditional member bodies, whole inactive members, registration builders and local imports in unrelated files all have positive controls.

This remains a finite direct-static guard. It does not solve arbitrary preprocessor satisfiability, emit every build configuration or prove arbitrary transitive/runtime dependencies. Relevant ambiguous syntax/binding fails until qualified. The old blanket eight-file ban is removed.

## Actual correction evidence

All raw logs/TRX are retained in this tree under .artifacts/d0-correction/:

| Receipt | Observed result / exit |
| --- | --- |
| correction-red.trx; red.log | 28 executed, 24 passed, 4 failed; exit 1. OpenKind(int) and source-defined System helper escaped with zero errors; unrelated conditional body and inactive whole member were falsely refused. |
| correction-first.trx; first.log | 28 executed, 21 passed, 7 failed; exit 1. Diagnostic iteration: tuple-field source locations and namespace-container declaration aggregation caused false positives. Both were corrected; no acceptance claim attaches to this run. |
| correction-second.trx; second.log | 28 executed, 28 passed; exit 0. |
| correction-qualified.trx; qualified.log | 34 executed, 34 passed; exit 0. Additional source-authority, generic argument, hidden root/global alias and unrelated registration/import cases. |
| conditional-width-red.trx; width-red.log | 13 executed, 11 passed, 2 failed; exit 1. Inactive same-name/different-owner declaration was falsely refused; inactive partial owner in another file escaped. Owner-qualified binding keys and cross-file definition classification correct these two FR-002 manifestations. |
| correction-frozen.trx; frozen.log | **37 executed, 37 passed, 0 failed/skipped; exit 0.** Normal current build, all retained 20 cases included. |

Final population: one real baseline; **24 negative cases**; **9 positive cases**; three project-directory classification cases. Baseline output remains **26 roots, 3976 symbol-reference observations, 58 admitted shared identities, zero errors**. Added controls are the exact overload, System source helper, Release conditional body, enclosing region, local conditional alias, inactive overload definition, hidden root, global alias, relocated shared source, generic Atlas argument, trailing inactive complete-file definition, cross-file partial owner, unrelated conditional body/whole member/import/registration/same-name owner.

The final normal-build command was:

    dotnet test tests/AiDe.App.Tests/AiDe.App.Tests.csproj --no-restore --filter 'FullyQualifiedName~SolutionTreeProbeTests.ProbeAtlas_' --logger 'trx;LogFileName=correction-frozen.trx' --results-directory .artifacts/d0-correction -v q

Final source byte SHA256: 0fbc79cdaf1944674a10b3488c32c5420185522382f01db9d7a3b9cf495d9ae4. LF-normalized SHA256: 4730db0143d47e56c51464ccb2f69cea8e9611955cec4c5fa2303bae92e49766.

Existing enum, FileRead, FactoryBuild and RepoRoot bodies were compared again against 247e6b4e and observed byte-equivalent after newline normalization. No product mutation was written: all adversaries operate on in-memory syntax trees. Only the exact source/proof and own official audit append are committed. Scratch evidence is retained locally.

## Class → sweep → derive → prevent and handoff

Class remains DC-118: widening exact declarations to name families, and selected members to enclosing files. Sweep included the exact port inventory, new overload and framework namespace authority, active/inactive/enclosing/import/partial-source conditions, different-owner name collisions, and all retained cases. Derive: shared admission needs exact semantic/source identity; conditional risk needs the actual declaration owner and selected context. Prevent: permanent discriminating negative/positive cases above. The tuple/namespace diagnostic iteration and second width-red are retained as corrections, not hidden retries. Parent owns the central register and derived union.

Clerical corrections in this commit: frontmatter type proof → doc; integrated failure receipt path .artifacts/atlas-five-gates/failed-slot-p1-01 → artifacts/atlas-five-gates/failed-slot-p1-01. The original author results and independent BLOCK remain historical evidence.

Independent re-review is pending. The author has not cleared the veto. AIDE_CONTRACT_LOG was not supplied; no capture destination is invented. This proof is the named episode artifact. Source/shared-handoff/runtime ceilings from the original review remain; parent owns independent acceptance and integration.

Final TRX start 2026-09-15T18:26:52.0080886-07:00; finish 2026-09-15T18:27:07.2597988-07:00; measured span 15.2517102 seconds. The scratch receipt extractor initially matched a RepoRoot call instead of its declaration and stopped before writing this proof; matching the explicit declaration corrected the extractor, after which all four original bodies compared equal.

## Separate closure recovery

The original correction unit reached its 16/16 ceiling without committing: the scratch proof writer used the Windows default cp1252 encoding and failed on an arrow after truncating the destination. This is a recorded encoding defect and cap firing, not budget-compliant completion. The Owner authorized a separate two-call closure recovery. It reconstructed this proof from the exact bbca8d6edc495b150c502999fd0fcc69fa922c72 blob, applied the intended clerical/evidence updates, and wrote explicit UTF-8 through a verified temporary file followed by atomic replacement. Source bytes and the previously observed 37-case result remain unchanged; no tests were rerun. The prevention is explicit encoding plus byte-verified atomic replacement in the retained receipt writer. All original red/green evidence and the independent BLOCK are preserved.


## FR-003 finite conditional closure author unit (independent review pending)

Goal: close the inactive-extension binding gap with the Owner-admitted finite exhaustive mechanism. Done when the fixed-input boundary and adversarial controls qualify and the frozen author result is returned for independent review. T2; one author, no children; exact source scope remains this test and this proof. Owner decision `cl-01M2KZXAG8BYSSH345PC435EKN` at parent `b027de4f` admitted implementation from spike `b76581ae38bb1344457ef161326bb5bc861400a8`. Previous independent BLOCK receipts and failed attempts above remain history; the author does not clear them.

Session `codex-d0-atlas-conditional-closure`, author `codex-astra-d0-author`, provisioned branch `fix/d0-atlas-conditional-closure`. Marker started 2026-09-16T02:15:32Z. Evidence readback at 2026-09-16T02:34:49.232576+00:00. Planned20 calls/30minutes, checkpoint12; closure uses calls19 and20. No product/project/dependency changes, broad App/STA/native/UI tests, join or push. Primary liveness is `.agents/sessions/codex-d0-atlas-conditional-closure.md` in the primary checkout. Parent owns derived audit/docs views and central lesson union.

### Fixed-input contract

The source closure is App -> Core. The pinned App project declares Daemon/Mcp as nonlinking build edges. All ordinary `.cs` files under Core/App are discovered anew; no346-tree population pin remains. The exact26 D0 roots and60 reviewed port declarations are retained. The guard inspects direct static references; it makes no arbitrary transitive/runtime independence claim.

`ClosurePins` fixes26 reviewed inputs/absences: SDK selection, root and source-ancestor build/package/editor controls, both source projects, three XAML inputs, and four generated inputs (Core/App global usings plus App.g.cs/MainWindow.g.cs). Pins use decoded UTF-8 text with BOM consumed and CRLF normalized to LF, matching Roslyn input text. New imports/project edges/conditional Compile changes alter the reviewed project/build input and refuse. Missing or altered generated input, a new XAML generator input, an introduced ancestor control, altered SDK selection and inconsistent references refuse with CLOSURE. No runtime pin refresh exists. A pin update requires behavioral qualification and independent review.

The four generated outputs are an explicit ceiling. This guard does not execute generators or establish arbitrary MSBuild/environment/configuration completeness. Different generator output or changed pinned controls refuse; silently admitting unknown build closure is not the contract. Ordinary .cs files are automatically enumerated, but conditional project inclusion requires renewed manifest review. Metadata images, syntax texts and parse options are immutable during enumeration; Core is rebuilt and supplied as a fresh source compilation reference to App for every assignment. Disk source inventory/content and reviewed controls are checked before/after enumeration; an observed mutation refuses. A transient disk mutation that restores original content between checks cannot change the immutable compilation inputs. No adversarial concurrent disk-writer test was run; inconsistent references and unsupported parse options have permanent controls.

Conditional keys are `(project,symbol)`, gathered using Roslyn If/Elif syntax from the complete admitted source closure, including inactive text. Population is calculated before enumeration; >8 refuses without sampling. Each assignment reparses original text with its own symbols, preserving file-local define/undef. Existing root/port/import/declaration conditional refusals remain. Canonical identity uses ReducedFrom then OriginalDefinition, exact source/project authority and declaration signatures, full metadata assembly identity, and explicit BuiltinOperator operand/return/operator signatures. Unsupported/unmapped identities and unresolved/ambiguous bindings refuse. Complete enumeration and acceptance are reported separately.

### Red-first and observed qualification

The original37 cases are retained. Additional controls cover real App/Core RELEASE extension escapes; competing extensions; unrelated namespace and incompatible receiver positives; file-local undef; capacity8 and overflow16; unsupported dynamic binding; inconsistent reference/parse-option inputs; seven build/generator manifest mutations; all8 standalone A+B+C masking states; and six distinct operator/signature/source-authority identities. The masking fixture is a standalone composition counterexample, not an observed bypass of the combined37-case guard.

Initial red had56/38/18. A lazy extension-tree enumerable caused fixture exceptions. After materializing it, meaningful red31/24/7 directly observed empty error collections for both RELEASE escapes. A preceding shell quoting error prevented one intended fixture edit; its redundant run is retained rather than counted as meaningful FR003 evidence. First implementation56/12/44 exposed two WPF BOM fingerprint mismatches; the contract now uses the same decoded text representation as compilation. Second build failed three CS0103 output-helper names and produced no TRX. Third66/65/1 exposed a mixed-language fixture failure before the guard; the corrected fixture consistently changes all App trees. These author defects and raw receipts are preserved.

Final normal-build filter: `FullyQualifiedName~SolutionTreeProbeTests.ProbeAtlas_`. Actual final counters:66 executed,66 passed,0 failed,0 skipped; `green.exit` records the actual dotnet exit. This is headless semantic/synthetic qualification only. The enum, File.Read and native-factory test method bodies were byte-equivalent after newline normalization against the base, and were not executed by this filter.

| Raw TRX | Executed | Passed | Failed | SHA256 |
|---|---:|---:|---:|---|
| `closure-first.trx` | 56 | 12 | 44 | `a866804158b9bbcc872804c02232dff13ed17f5137f1c01555fccf0ddae15278` |
| `closure-fixture-red.trx` | 31 | 24 | 7 | `dcaa810e0b1b088dc7868b39cb375e6d8c9154e2acbd23da3f6df3d66f797fde` |
| `closure-green.trx` | 66 | 66 | 0 | `4320d5a0865a9f63c0a72001afed190c29f184bcc653fe79517a5f5bd567aee8` |
| `closure-meaningful-red.trx` | 31 | 24 | 7 | `0617a1271c9420d4e23b98afa65fadfbedb74da332f0cb354e6b7bdd714fa6c7` |
| `closure-red.trx` | 56 | 38 | 18 | `26f453666614e5d133c46481a93425af40a20cfa448ed4a3b758efd7b26944d2` |
| `closure-third.trx` | 66 | 65 | 1 | `89e304a79db727ca700cdedf83e9bf9da0bc0b172288d8d51de9e7c91cd09698` |

Selected actual emitted observations:

```text
AiDe.App.Tests.SolutionTreeProbeTests.ProbeAtlas_DirectStaticD0Boundary_DoesNotDependOnAtlas
DIRECT_STATIC roots=26 references=3976 ports=58 errors=0 corpusTrees=346 symbols=0 expressions=4551 population=1 assignments=1 coverageComplete=True accepted=True seconds=0.869016

AiDe.App.Tests.SolutionTreeProbeTests.ProbeAtlas_AdversarialD0Mutation_IsRejected(mutation: "parse-options", expected: "CLOSURE")
parse-options DIRECT_STATIC roots=0 references=0 ports=0 errors=1 corpusTrees=0 symbols=0 expressions=0 population=0 assignments=0 coverageComplete=False accepted=False seconds=0.000000
CLOSURE unsupported parse options
AiDe.App.Tests.SolutionTreeProbeTests.ProbeAtlas_AdversarialD0Mutation_IsRejected(mutation: "unsupported-dynamic", expected: "UNSUPPORTED")
unsupported-dynamic DIRECT_STATIC roots=26 references=3983 ports=58 errors=2 corpusTrees=346 symbols=0 expressions=4560 population=1 assignments=1 coverageComplete=False accepted=False seconds=0.864930
UNACCOUNTED DynamicType dynamic at src/AiDe.App/Workbench/SolutionTreeSurface.cs
UNSUPPORTED binding identity assignment=0
AiDe.App.Tests.SolutionTreeProbeTests.ProbeAtlas_AdversarialD0Mutation_IsRejected(mutation: "release-extension", expected: "BINDING")
release-extension DIRECT_STATIC roots=26 references=3976 ports=58 errors=1 corpusTrees=347 symbols=1 expressions=4551 population=2 assignments=2 coverageComplete=True accepted=False seconds=1.308531
BINDING changed assignment=1 expressions=12
AiDe.App.Tests.SolutionTreeProbeTests.ProbeAtlas_AdversarialD0Mutation_IsRejected(mutation: "core-release-extension", expected: "BINDING")
core-release-extension DIRECT_STATIC roots=26 references=3976 ports=58 errors=1 corpusTrees=347 symbols=1 expressions=4551 population=2 assignments=2 coverageComplete=True accepted=False seconds=1.165590
BINDING changed assignment=1 expressions=12
AiDe.App.Tests.SolutionTreeProbeTests.ProbeAtlas_AdversarialD0Mutation_IsRejected(mutation: "competing-extensions", expected: "BINDING")
competing-extensions DIRECT_STATIC roots=26 references=3976 ports=58 errors=2 corpusTrees=348 symbols=1 expressions=4551 population=2 assignments=2 coverageComplete=False accepted=False seconds=1.305208
BINDING changed assignment=1 expressions=12
BINDING unresolved assignment=1 diagnostics=4
AiDe.App.Tests.SolutionTreeProbeTests.ProbeAtlas_AdversarialD0Mutation_IsRejected(mutation: "population-overflow", expected: "COVERAGE")
population-overflow DIRECT_STATIC roots=26 references=3976 ports=58 errors=1 corpusTrees=0 symbols=0 expressions=0 population=16 assignments=0 coverageComplete=False accepted=False seconds=0.000000
COVERAGE assignment-ceiling population=16
AiDe.App.Tests.SolutionTreeProbeTests.ProbeAtlas_ExhaustiveCoverage_VisitsTheCompletePopulation
DIRECT_STATIC roots=26 references=3976 ports=58 errors=0 corpusTrees=346 symbols=0 expressions=4551 population=1 assignments=1 coverageComplete=True accepted=True seconds=0.845556

DIRECT_STATIC roots=26 references=3976 ports=58 errors=0 corpusTrees=347 symbols=3 expressions=4551 population=8 assignments=8 coverageComplete=True accepted=True seconds=3.900621

```

Source SHA256 bytes: `60fa8cccb388e396076787f023694a7c3e4eac377db1c553cb73da3a95528023`. LF SHA256: `60fa8cccb388e396076787f023694a7c3e4eac377db1c553cb73da3a95528023`. Raw logs, TRXs and executable author scripts remain under `.artifacts/d0-closure/`; the complete file/hash inventory is `.artifacts/d0-closure/inventory.json`. They are retained local evidence, not additional authored repository paths.

Class -> sweep -> derive -> prevent: parent-recorded DC118 width drift and the binding/identity coverage class apply. The sweep reached all admitted Core/App sources, source-project propagation and build controls; the derived control exhausts bounded assignments rather than flattening alternatives. Permanent discriminators preserve coexistence and reject RELEASE extension/ambiguity/overflow/identity/closure changes. The temporary lazy-tree, text-encoding and fixture-compilation defects were found by actual red/qualification runs, fixed locally, and recorded for the parent's central lesson union. No self-clearance or production behavior change is claimed.

Final closure readback:20/20 tool boundaries, approximately20 minutes from the measured marker; final source-only post-qualification change removed trailing whitespace (token sequence unchanged). Final66-case normal build exited0. Independent review remains pending. Automatic audit-data.js and local raw .artifacts files remain dirty/retained; they are not discarded or manually staged.


### Administrative closure recurrence and authorized recovery

At the original20-call ceiling, the official audit writer rejected the closing command because required `--shortname` was absent. The command stopped before staging, commit or lease release. The earlier PRIMARY liveness statement that release had completed was premature. The Conductor authorized one administrative recovery call under the same scope. Actual total: **21/20 calls (overrun)**, not budget compliance; the original20-call evidence and marker are preserved. This paragraph supersedes earlier closure wording where it implied successful completion at20 calls.

Recovery at 2026-09-16T02:37:36.295602+00:00: use the actual writer contract with `--shortname d0-conditional-closure`, preserve the original timing marker, append the own audit, commit only test/proof/audit JSONL, and release the exact two leases in finally. PRIMARY liveness now records the observed release state. No source edit or test rerun occurs during recovery. Source SHA256 remains `60fa8cccb388e396076787f023694a7c3e4eac377db1c553cb73da3a95528023`. The final66/66 TRX is `.artifacts/d0-closure/closure-green.trx`, SHA256 `4320d5a0865a9f63c0a72001afed190c29f184bcc653fe79517a5f5bd567aee8`; its actual exit remains0.

Recurrence class: administrative command contract omissions and premature lifecycle claims. Sweep: inspected the required audit field, exact staged manifest, source/TRX fingerprints and own leases. Derive/prevent: the retained recovery script asserts the immutable source and counters, supplies the required shortname, stops on writer/commit failure, and releases in finally with observed liveness readback. Parent owns the central class/control union. This recovery does not enlarge implementation scope or clear independent review.
