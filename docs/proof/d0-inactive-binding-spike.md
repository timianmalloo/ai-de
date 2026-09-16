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
