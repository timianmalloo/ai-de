---
id: proof-d0-inactive-binding-spike
title: "D0 inactive binding: Roslyn rebinding design spike"
type: doc
status: proposed
owner: "@timianmalloo"
tags: [d0, atlas, design-spike, static-analysis]
links:
  - { to: proof-d0-atlas-independence-rereview, rel: depends-on }
  - { to: proof-d0-atlas-independence, rel: relates-to }
  - { to: session-contracts, rel: depends-on }
review-by: 2026-12-15
summary: "Roslyn rebinding detects extension/import/project-reference changes, but a measured masking fixture disproves flattened-all and single-region configuration completeness. No source correction authorized."
---

# Decision returned to Owner

**Verified: unsupported configuration-completeness boundary.** Installed Roslyn can expose declarations and rebind the real selected D0 roots. It distinguishes the review's applicable extension from an unimported namespace and an incompatible receiver. However, exposing all branches, even when supplemented by every single condition separately, misses a measured interacting subset. This spike does **not** support implementing a flattened-only guard under the existing all-inactive-binding promise.

No test/product source was corrected. Independent FR-003 BLOCK remains. Owner must choose the finite configuration/variant contract before correction authoring. This report supplies the supported primitive and the exact counterexample; it does not silently reduce the promised coverage or propose a hand-written extension lookup approximation.

# Contract, provenance and execution graph

Goal: measure whether the installed Roslyn API satisfies finite inactive binding coverage and return a supported implementation contract or precise unsupported boundary. Done when actual IDs, diagnostics, positive/negative distinctions, source-project propagation and configuration interactions are recorded in a frozen proof/audit. Not in scope: correction, preprocessor solver, generic transitive analyzer, MSBuild workspace/framework, new dependency, full/STA/native/shown testing, joins or pushes. T2; width one; no child agents. Parent provisioned the tree and recorded the programme graph delta before dispatch.

Author/session: codex-astra-d0-author / codex-d0-inactive-binding-spike. Tree C:/Projects/ai-de-spike-d0-inactive-binding; branch spike/d0-inactive-binding; base 3d2aa9001fe2afc9dd05c84dc698287acbdb00d0. Audit grounding 2026-09-16T01:47:17Z. Planned eight tool boundaries / fifteen minutes; checkpoint six; final boundary eight. Dependency graph: read new BLOCK/executed reviewer code -> build scratch and measure real source graph -> execute interactions/disconfirm completeness -> freeze unsupported-boundary receipt -> Owner decision. No retry loop; subsequent executions addressed concrete scratch-key/identity errors and added the required interaction experiment.

Surface list: existing Core census/projection and query ports -> source-project reference -> App selected roots/factory/shell -> exact semantic binding observations. Domain, persistence, UI and product behavior are unchanged. Existing workflow/Testing Strategy grounding is reused: D0 hygiene, deterministic D1/D2 input distinctions, D3 dependency direction, actual installed-engine contract rather than mocked binding.

The rereview receipt and actual Program.cs/spike-qualified.log were read from C:/Projects/ai-de-review-d0-atlas-correction/.artifacts/d0-rereview/. The decisive method identity is ReducedFrom before OriginalDefinition, not the identical-looking reduced display string.

Unchanged test Git blob/LF SHA256:

    4730db0143d47e56c51464ccb2f69cea8e9611955cec4c5fa2303bae92e49766

The receipt writer compared both the exact base blob and current source after LF normalization. No source mutation or 37-case rerun occurred.

# Executed engine contract

The scratch project references the existing test project and installed Roslyn 4.14. It calls the frozen Load/SelectRoots through reflection and creates only in-memory variants. Normal dotnet run builds current dependencies; no stale no-build execution. Commands:

    dotnet run --project .artifacts/d0-binding-spike/Spike.csproj -v q

The snapshot traverses **26 real D0 roots and 4551 expression observations** over **240 Core plus 106 App source/generated trees**. It records symbol identity, type identity, candidate reason/candidates and selected diagnostics. Extension methods normalize ReducedFrom, then OriginalDefinition. Source identities carry project and source paths; framework metadata identities carry assembly identity and documentation ID. Core metadata documentation IDs resolve uniquely back to Core source when available. Namespace containers use namespace identity, not their aggregate source-file population.

These are expression observations, not unique calls. Changes to method groups, invocation symbols and receiver types may produce several differences for one direct call. Diagnostics and actual binding IDs, not a zero console exit, are the oracle.

The source-project operation is measured:

1. Locate exactly one AiDe.Core assembly reference in App.
2. Remove it and add currentCore.ToMetadataReference().
3. Expose conditional declarations in Core before creating that reference, then rebind App with the propagated reference.
4. Compare variants against the same source-graph baseline.

The reference swap itself reports 15 identity differences against the compiled-metadata baseline, with zero selected diagnostics. Logged examples are synthesized operator identities whose assembly version changes from compiled 1.0.0.0 to source compilation 0.0.0.0 and which lack declaration documentation IDs. This spike does **not** claim those two baseline identity populations are equal or that every generated symbol is qualified. Variant comparisons consistently use the source-graph baseline. A future implementation must establish its own canonical identity contract for generated/implicit symbols; simply swapping references and comparing raw old metadata identities is unsupported.

# Final real-D0 observations

All final cases retain 26 roots and 4551 expressions.

| Case | Roots | Expressions | Changed observations | Selected errors |
| --- | ---: | ---: | ---: | ---: |
| baseline-source-project-reference | 26 | 4551 | 15 | 0 |
| app-extension-inactive | 26 | 4551 | 0 | 0 |
| app-extension-exposed | 26 | 4551 | 12 | 0 |
| app-extension-active | 26 | 4551 | 12 | 0 |
| unimported-extension-inactive | 26 | 4551 | 0 | 0 |
| unimported-extension-exposed | 26 | 4551 | 0 | 0 |
| unimported-extension-active | 26 | 4551 | 0 | 0 |
| incompatible-receiver-inactive | 26 | 4551 | 0 | 0 |
| incompatible-receiver-exposed | 26 | 4551 | 0 | 0 |
| incompatible-receiver-active | 26 | 4551 | 0 | 0 |
| conditional-import-exposed | 26 | 4551 | 93 | 3 |
| core-extension-stale-metadata | 26 | 4551 | 0 | 0 |
| core-extension-propagated | 26 | 4551 | 12 | 0 |
| competing-extensions-exposed | 26 | 4551 | 12 | 4 |

The actual RefreshSolutionTrees ToList documentation IDs are:

    M:System.Linq.Enumerable.ToList``1(System.Collections.Generic.IEnumerable{``0})
    M:AiDe.App.Workbench.ReviewConditionalExtensions.ToList``1(System.Collections.Generic.IEnumerable{``0})
    M:AiDe.App.Workbench.ReviewCoreExtensions.ToList``1(System.Collections.Generic.IEnumerable{``0})

The first has System.Linq assembly authority (version 10.0.0.0, key b03f5f7f11d50a3a). The second has AiDe.App plus src/AiDe.App/app-extension.cs authority. The third has AiDe.Core plus src/AiDe.Core/ReviewCoreExtensions.cs authority.

**Verified distinctions:**

- The actual reviewer extension, with the Atlas body retained, changes selected bindings when exposed or RELEASE-enabled. The inactive state retains framework binding.
- The identical extension in unimported Review.Unrelated leaves all selected expression identities unchanged in inactive, exposed and active states.
- The same-namespace extension accepting IEnumerable<int> is incompatible with the selected receiver and likewise causes zero changes/errors in all three states.
- A conditional global import of the unrelated namespace makes competing extensions applicable; selected ToList becomes unresolved with OverloadResolutionFailure and candidate symbols, rather than silently preserving a selected owner.
- Exposed Core extension declarations do not reach App through stale compiled metadata: zero changes and framework owner. Propagating the actual source compilation changes the real selected owner to ReviewCoreExtensions, with zero selected errors.
- Two applicable extension owners exposed together produce selected ambiguity/errors; the candidate mechanism detects the failure.
- Two existing guard checks were replayed directly, not the full suite: conditional-atlas and conditional-definition each retained one CONDITIONAL refusal. Their root/port refusals remain required; rebinding does not replace them.

# Executed masking counterexample

The focused overload fixture is a separate, finite hand-authored compilation using the installed engine. It is not a solver and is not alleged to exist in product source:

    interface I2 {}
    partial interface I1 {}
    partial class Receiver : I1 {}
    static string Pick(I1 value) => "baseline";
    static string Run() => Pick(new Receiver());

Condition A introduces Pick(I2). Condition B adds I2 to Receiver through another partial declaration. Condition C adds I2 as a base of I1 through another partial declaration. All three changes are valid C# declarations; actual parse symbol sets select the combinations.

| Defined symbols | Selected Run binding | Errors |
| --- | --- | ---: |
| none | Pick(I1) | 0 |
| A | Pick(I1) | 0 |
| B | Pick(I1) | 0 |
| C | Pick(I1) | 0 |
| A,B | **none: OverloadResolutionFailure, candidates Pick(I1) and Pick(I2)** | **1** |
| A,C | Pick(I1) | 0 |
| B,C | Pick(I1) | 0 |
| A,B,C | Pick(I1) | 0 |
| all directives erased / all branches exposed | Pick(I1) | 0 |

With A+B, Receiver implements unrelated I1 and I2 and both overloads are applicable. Adding C makes I1 more specific than I2, restoring the original chosen overload. Thus the exposed-all result and each singleton agree with baseline while an intermediate valid combination fails. The program logs exact OriginalDefinition IDs:

    M:BindingFixture.Probe.Pick(BindingFixture.I1)
    M:BindingFixture.Probe.Pick(BindingFixture.I2)

This is direct overload resolution, not arbitrary transitive/runtime behavior. It disproves configuration completeness for both flattened-all alone and flattened-all plus singleton exposure. No inference from method-name lookup was used.

# Smallest supported contract / unsupported remainder

**Supported primitive, Verified:** given an explicit source variant, Roslyn rebinding can compare selected expression identities and report changed, ambiguous or unresolved bindings. It can preserve unrelated-extension/receiver positives without a hand-written namespace/receiver approximation. Core variants must propagate through the actual source reference. Existing exact-port/root/conditional-context checks remain in front of or alongside this comparison.

**Not supported by this spike:** a rule declaring every conditional combination safe because the baseline, singleton and flattened-all variants match. The eight-state fixture refutes it. Enumeration/coverage of a permitted finite variant set, or an explicit Owner restriction on interactions that cannot be certified, is a policy/design decision still required. An unexamined combination cannot be labeled safe. No bounded variant-enumeration implementation, interaction detector or preprocessor solver was authored here.

The current snapshot also needs a qualified canonical identity rule for synthesized/implicit source-versus-metadata symbols before replacement of the existing guard. Comparing all variants within one source baseline demonstrates the API propagation contract, not equivalence to every generated runtime declaration. Conditional syntax may be ambiguous when branches are flattened; such errors are refusals for that variant, not evidence that every individually valid configuration is unsafe.

The unchanged 37 cases remain the implementation qualification floor after Owner dispatch. Independent review must also include the actual extension namespace/receiver pair, project-reference propagation, the masking fixture, and any eventual declared variant ceiling. The present spike is complete as an unsupported-boundary report; it does not clear FR-003.

# Execution receipts and corrections

Three console executions are retained:

- first.log: exit -532462766 before case results, duplicate snapshot key (nested syntax shared kind/start). The scratch key gained deterministic expression ordinal; source unchanged.
- qualified.log: exit 0 with actual cases and all eight masking states. Namespace-container source population polluted some difference counts. This was a scratch identity defect, not acceptance evidence.
- final.log: exit 0 after namespace identity normalization. Fourteen real-D0 comparison rows, nine masking rows (eight actual symbol combinations plus flattened-all), two preserved guard results and one baseline population record were read. No xUnit suite was run.

Final run's tool-measured shell wall time: 13.3909618 seconds. Console exit zero reports execution completion; the masked subset and identity drift are explicitly failing design predicates.

Class -> sweep -> derive -> prevent: DC-118, an all-configuration claim narrowed to a few exposed variants. Sweep covered applicable/unimported/incompatible extensions, imports, overload competition, source-project propagation and a three-condition interaction. Derive: semantic rebinding is necessary but variant coverage is a separate obligation. Prevent: retain this executable masking fixture and require it to disconfirm any proposed coverage contract before product/test correction. Parent owns the central register. Scratch snapshot-key/namespace normalization corrections remain in the raw history.

# Evidence hashes and close

All paths below are local retained scratch in this author tree; hashes are SHA256 of exact bytes.

| Artifact | SHA256 |
| --- | --- |
| .artifacts/d0-binding-spike/Program.cs | 166f0ed205be1ac027bd0eb54e9764d378928e4191dd9d0f52687db1feda3b94 |
| .artifacts/d0-binding-spike/Spike.csproj | 110881d5a08200525000e1bb2a9b68f438e92ddb045984ad57ae89dad5bb7ff1 |
| .artifacts/d0-binding-spike/first.log | 89b6ddfb8af7076da87f9611b8c05d186d192e7b00ff120d1c9b1331eaf04709 |
| .artifacts/d0-binding-spike/qualified.log | acd29c82d9ac5d908dfd1fcb1e69924b0663dc9d0f4468a84cbd014d5e369468 |
| .artifacts/d0-binding-spike/final.log | ef50fadcfaf69d863e3d985a00f430029469a7fa6ff654e3ea6e67f843e17b77 |
| C:/Projects/ai-de-spike-d0-inactive-binding/.artifacts/d0-binding-spike/write-proof.py | ed3cabe487e2ef6963b68130f57d5a6798d61b5c78eec3e871b98fdef72c3b96 |

Only this proof and own official audit append are committed. The proof is written with explicit UTF-8 to a temporary file, byte-verified, then atomically replaced and fully read back. Exact short proof lease is released at closure; own PRIMARY liveness names the Owner decision wait. Automatic audit-data and raw scratch are retained. No AIDE_CONTRACT_LOG was supplied, so no capture destination was invented. Named episode artifact: docs/proof/d0-inactive-binding-spike.md.


# Exhaustive-assignment design candidate (subsequent Owner-authorized run)

## Decision returned: supported finite contract, awaiting Owner approval

**Verified candidate:** enumerate the complete Boolean assignment population for the actual Roslyn input/dependency closure, keyed by (source project, conditional symbol), then reparse original files and propagate the changed Core source reference into App for every assignment. Accept only if every required assignment has resolvable, unchanged selected bindings and the existing root/port refusals pass. **Proposed fixed ceiling: eight assignments.** A larger population produces a specific incomplete-coverage refusal before any partial sample could be mistaken for acceptance.

This is a design candidate, not a source correction. The preceding flattened-only failure remains valid history. Exhaustive enumeration visits the A+B state that flattened-all and singleton testing missed. The masking example is a **standalone composition counterexample**, not an observed escape of the combined existing 37-case guard. Independent FR-003 BLOCK remains until an authorized implementation and re-review; no implementation or 37-case rerun occurred here.

Same isolated session/tree, base frozen proof 9206f9407667a27f4dff26fade3f8e3efea568be. New audit marker: 2026-09-16T02:00:10Z. This run's goal: measured symbol census/population, enumeration cost, exact canonical identity and a fail-closed ceiling. Done when executable positive/negative distinctions and limits are frozen for Owner decision. T2, width one, eight-call/fifteen-minute ceiling, checkpoint six; actual final boundary eight. Earlier run budgets/results are unchanged.

Graph delta: census actual inputs and inspect prior discrepancies -> compiler-grounded canonical identity -> exhaustive assignment measurements/discriminators -> choose and measure proposed ceiling -> unsupported-identity control -> proof/audit -> Owner decision. No new agents, source projects, dependencies, SAT solver, MSBuild framework, product/test edits, joins or pushes. The existing surface list and D0/domain limits remain unchanged.

## Actual closure and population before ceiling choice

The loaded compiler input is **240 Core trees + 106 App trees = 346 trees**, including the loader's explicit generated global-using/WPF sources. Roslyn conditional directive expressions in this actual input contain **zero conditional identifiers**. The complete assignment population is therefore **2^0 = 1**, not an empty test.

The actual csproj source-reference readback establishes App -> Core as the compilation dependency edge. App's Daemon and Mcp project references explicitly have ReferenceOutputAssembly=false; they are build/copy dependencies, not source references in this Roslyn compilation. Core has no further project reference. Package/framework metadata remains fixed compiler input. This claim is about the declared current Roslyn source closure, not unlisted generator output or every possible MSBuild conditional Compile-item expansion.

The census reads IfDirectiveTriviaSyntax/ElifDirectiveTriviaSyntax condition IdentifierNameSyntax nodes through Roslyn, including nested inactive regions. It does not regex C# or manually approximate extension lookup. Keys include project identity. Original source text and parse options are retained; assigned external symbols are supplied using WithPreprocessorSymbols, so Roslyn still applies each file's #define/#undef in source order. Symbols locally forced by a file can create redundant global assignments; redundancy is retained rather than used to narrow coverage.

The first measurement used a scratch trial ceiling of 32 after calculating the population. No production ceiling was selected from that trial. After observing the actual one-state cost and two-/four-state discriminators, the candidate chose **eight**, then measured all eight states over the full actual corpus plus an unrelated three-symbol conditional fixture. Four-symbol population sixteen was refused before enumeration. Future unrelated conditions can therefore trigger a resource-coverage refusal; that is an explicit ceiling, not a global member or namespace ban. The current baseline fits and passes; it is not permanently refused.

## Exact identity correction: the 15 discrepancies resolved

Roslyn reports the discrepant enum operations as **MethodKind.BuiltinOperator**. A built-in operator has no source method declaration; its synthetic containing assembly version is not its semantic declaration identity. The candidate uses the compiler-provided complete operator signature:

    BuiltinOperator + MetadataName
    + return RefKind + canonical ReturnType
    + ordered parameter RefKind + canonical ParameterType

Each type retains source project/path + exact declaration ID, or full metadata assembly identity + declaration ID. Constructed generic arguments, arrays/rank and pointers are recursive identities. No assembly-version stripping applies to ordinary metadata symbols. No synthetic-symbol blanket exemption is used. Operators become explicitly compared identities, not ignored expressions.

**Observed:** canonical source-reference baseline versus compiled-metadata baseline now has **zero changed observations, zero selected diagnostics, zero unsupported identities** across all 26 roots/4551 expressions. This resolves the earlier observed 15 discrepancies for the measured corpus.

Adversarial identities distinguish:

- enum equality from inequality (op_Equality/op_Inequality);
- Int64 operands from Int32 operands;
- the same enum/operator signature declared in FirstAuthority.cs versus SecondAuthority.cs;
- a **UserDefinedOperator**, which retains its source project/path and exact M:...op_Equality(Number,Number) documentation ID rather than entering the BuiltinOperator branch.

Ordinary methods still normalize ReducedFrom before OriginalDefinition and retain full declaration identities/signatures. Core metadata resolves uniquely to current Core source by documentation ID. Source locals/range variables/functions use their compiler declaration location and type/signature; parameters/type parameters use owner identity plus ordinal/kind. Any other unhandled identity is explicitly UNSUPPORTED, never an empty or plausible fallback. DynamicType is deliberately unsupported in this candidate. A real selected-root in-memory dynamic-expression control reports three unsupported observations and a refused assignment, with zero compiler errors; unsupported identity therefore does not rely on a compilation failure to stop acceptance.

## Exhaustive algorithm contract

1. Freeze/validate the current source input set, project graph and metadata references. Require the established nonempty exact D0 roots.
2. Census every conditional symbol in every tree of that source closure. Use sorted (project,symbol) keys.
3. Calculate 2^N with an arbitrary-precision integer **before** casting or enumerating. If population > 8, report incomplete coverage with census/population/ceiling and refuse.
4. For every assignment, reparse original text under project-specific symbols, preserving file-local directives. Rebuild current Core and replace exactly one Core App reference with currentCore.ToMetadataReference(). A stale compiled Core reference is not admissible.
5. Re-select the D0 roots, compare compiler-grounded canonical identities, and read candidate sets and selected diagnostics. Record assignment visits and refusal reasons. Missing/duplicate roots, exceptions, unresolved/ambiguous binding, changed binding, unrecognized identity or incomplete enumeration cannot return accepted.
6. Retain existing exact source/signature shared-port policy and conditional root/port/import refusals. Rebinding complements those controls. Green means complete valid coverage AND no forbidden/changed binding in any assignment.

The scratch ENUM complete flag denotes **all assignments visited**, not accepted: unsupported or changed bindings still increase refusedAssignments. An implementation must expose coverage-complete and acceptance separately and name the refusal reason. The dynamic control visits its single state but refuses certification. Console exit zero likewise means evidence collection finished, not a green policy verdict.

For fixed source files, references and Roslyn parse options, all Boolean assignments of all conditional identifiers cover their preprocessing choices. File-local directives may collapse assignments, not introduce an uncensused external Boolean variable. This is finite enumeration, not a claim about changing the project graph, source generator inputs, compiler versions, metadata binaries or files between variants.

## Measured results

All real-corpus assignments rebind 26 roots. Ordinary variants have 4551 expression observations. The deliberately edited in-memory dynamic root has 4560; its ordinal-key shifts produce many differences, so the observed three unsupported identities are the specific independent refusal signal.

| Log | Actual enumeration result |
| --- | --- |
| measured | actual-corpus examined=1 complete=true refusedAssignments=0 seconds=1.471713 |
| measured | app-extension examined=2 complete=true refusedAssignments=1 seconds=1.828319 |
| measured | unimported examined=2 complete=true refusedAssignments=0 seconds=1.831571 |
| measured | incompatible examined=2 complete=true refusedAssignments=0 seconds=1.917846 |
| measured | import examined=2 complete=true refusedAssignments=1 seconds=1.740692 |
| measured | core-propagation examined=2 complete=true refusedAssignments=1 seconds=1.604767 |
| measured | project-symbol-independence examined=4 complete=true refusedAssignments=2 seconds=2.266915 |
| measured | file-local-define-undef examined=2 complete=true refusedAssignments=0 seconds=1.028762 |
| measured | competing examined=2 complete=true refusedAssignments=1 seconds=0.901715 |
| measured | overflow examined=0 complete=false refusal=assignment-ceiling |
| boundary | ceiling-eight examined=8 complete=true refusedAssignments=0 seconds=7.794592 |
| boundary | ceiling-overflow-sixteen examined=0 complete=false refusal=assignment-ceiling |
| identity | unsupported-dynamic-root examined=1 complete=true refusedAssignments=1 seconds=1.518216 |

The actual one-assignment corpus took **1.471713 seconds**. The full eight-assignment ceiling fixture took **7.794592 seconds**. These are Stopwatch wall measurements around census, reparsing, source-reference propagation and root snapshot comparison, not inferred scaling. They are observations on this machine/corpus, not latency guarantees. The second normal-build console invocation for the boundary controls took 14.3248468 seconds overall; the later identity-control invocation took 7.8652484 seconds.

Required distinctions observed:

- Applicable RELEASE extension: two states visited; the enabled state changes twelve expression identities and is refused.
- Identical unimported-namespace and incompatible-receiver extensions: both states pass with zero differences/errors/unsupported identities.
- Conditional global import: enabled state becomes ambiguous/unresolved and refuses.
- Core conditional extension: the enabled assignment reaches App through the rebuilt source reference and changes binding.
- Same symbol spelling in App and Core: separate keys produce **four** assignments; both states with Core RELEASE enabled refuse, while App-only RELEASE does not affect selected binding.
- File-local #undef keeps the otherwise-applicable extension disabled in both external assignments; file-local #define keeps an unrelated declaration active. Both assignments preserve D0 binding.
- Competing extensions: the applicable joint state has ambiguous binding and refuses.
- The standalone A/B/C masking composition is evaluated in **all eight states**. Bits 3 (A+B) reports OverloadResolutionFailure and one diagnostic; the other seven retain Pick(I1). Enumeration finds the previously masked state.
- Existing conditional-atlas and conditional-definition guard checks each retain one CONDITIONAL refusal; no full 37-case rerun was used.
- Population 64 refused against trial ceiling 32; population 16 refused against proposed ceiling 8, with zero states sampled and explicit assignment-ceiling reason.
- Dynamic identity is UNSUPPORTED and refuses its selected-root assignment independently of diagnostics.

Recorded population across the successful executable logs: **28 real-corpus assignment visits**, plus **8 standalone masking assignments**. Overflow cases intentionally examine zero assignments. These are design-spike measurements, not xUnit test counts.

## Supported scope and remaining qualification

The measured candidate supports a finite implementation contract at ceiling eight for this **fixed, explicit source closure**. Baseline coverage is complete and passes. It resolves the previous operator identity mismatch without broad metadata normalization. No hand-written extension applicability model is needed.

Before claiming an implementation complete, the author must translate this measured contract into the existing test, preserve all 37 cases and the independent veto predicates, and have it independently reviewed. The source-input closure must remain explicit: conditional MSBuild source-item inclusion, generator output varying with symbols, new source-project dependencies, changing metadata and broader runtime/transitive behavior are not covered by the present two-project snapshot. A new or unsupported closure must refuse qualification until incorporated; it must not silently become green. This candidate does not remove the existing loader's generated-source ceiling.

The operator corpus and adversaries ground the measured canonical cases. Future unhandled symbol kinds require explicit compiler-grounded identity support or refusal; they are not auto-admitted. Population growth above eight requires an explicit new cost/coverage decision. A partially evaluated large space is never coverage.

Scratch correction history: first candidate compilation failed CS8321 because the inherited flattened Expose function was dead under the new exhaustive design. It was removed rather than suppressed. The earlier snapshot/encoding histories remain untouched. Actual source hash remains the original LF/Git blob hash. No source correction has been authorized or authored.

Class -> sweep -> derive -> prevent: DC-118, sampled variants substituted for complete coverage and synthetic assembly ownership substituted for built-in operator signature. Sweep covered the actual closure/census, project-symbol independence, local directives, source-reference propagation, all masking states, explicit type/operator/source authority differences, overflow and unsupported identity. Derive: complete finite enumeration needs a measured population and fail-closed ceiling; built-in semantic identity comes from compiler signature/type authority. Prevent: these executable discriminators and explicit unsupported/overflow refusals are the proposed implementation floor. Parent owns the central lesson record.

## Continuation artifact hashes and closure

New executable scratch and logs are retained under exhaustive/. Hashes are exact-byte SHA256. Earlier raw artifacts and their hashes remain above.

| Artifact | SHA256 |
| --- | --- |
| .artifacts/d0-binding-spike/exhaustive/boundary.log | e601794df606a553edf71373053765b40e582e01ae879d414da9eae64b3132aa |
| .artifacts/d0-binding-spike/exhaustive/Candidate.csproj | 058898a6b0d26b5636bf2febd430abbddd9f7803da60adc8552d9099199960e4 |
| .artifacts/d0-binding-spike/exhaustive/extend.py | 7c896373c68783d57a683d69ad2d73c70c0f4f3e97a859c90f84aca29c182c9b |
| .artifacts/d0-binding-spike/exhaustive/first.log | add7de68b39372d705c1dad2d17da67921dcb4e87e82955805d3e43cd54d603f |
| .artifacts/d0-binding-spike/exhaustive/fix.py | 80ce72e7deed4e2a5f2e6232abb207f445f00fbd7182f5ec44eae41b14a730d2 |
| .artifacts/d0-binding-spike/exhaustive/identity-control.py | 170a77a6594685bee7c5b80d2ec9b33c21eb970b0ff7d0cf4427f7a8931a5476 |
| .artifacts/d0-binding-spike/exhaustive/identity.log | dfc5396a2293e79a6d22c41d1b60f90a8b978ff422c05c2ad00c665ca34640e0 |
| .artifacts/d0-binding-spike/exhaustive/measured.log | faa7b1066609769fc364298e7db077863cd24e682afead4d41aa9237b7386919 |
| .artifacts/d0-binding-spike/exhaustive/Prefix.cs.txt | 4f2e370247b4009663919edeeef3745e6b371aa33980bcce5a552e0920e8c60c |
| .artifacts/d0-binding-spike/exhaustive/prepare.py | 19c8e961457577c4adcfac5066fa0958cfc3a14dcbca1b0ebf705cd94b80a015 |
| .artifacts/d0-binding-spike/exhaustive/Program.cs | d39dd2067974e114142ea082f72a2f6eaea69c49ce8dd539160e614830f3e2c6 |
| .artifacts/d0-binding-spike/exhaustive/prompt.txt | 88a7992e6c23166f99ffb2ad95188071aa1d137a5bdca3af2d6ddb08d1259143 |
| .artifacts/d0-binding-spike/exhaustive/Tail.cs.txt | 1b599806806d6af16cd833935ad6a94c9b24744e323ae20834956ef56a86f82e |
| .artifacts/d0-binding-spike/exhaustive/write-proof.py | 316884a4ff0e709e8b7740f7ffd434efda68d78f642878198f9b2f0323fda802 |

Only this existing proof and own official audit append are committed. The append is explicit UTF-8, byte-verified through a temporary file and atomic replacement. Own PRIMARY liveness is updated and the exact proof lease released. Generated audit-data and raw evidence remain retained. Owner decision is required before any correction authoring; independent BLOCK remains in force.
