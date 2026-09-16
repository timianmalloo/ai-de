---
id: proof-d0-atlas-independence-rereview
title: "D0 Atlas correction independent re-review"
type: doc
status: proposed
owner: "@timianmalloo"
tags: [d0, atlas, independent-review, static-analysis]
links:
  - { to: proof-d0-atlas-independence, rel: depends-on }
  - { to: proof-d0-atlas-independence-review, rel: depends-on }
  - { to: session-contracts, rel: depends-on }
review-by: 2026-12-15
summary: "BLOCK retained: original overload and whole-file counterexamples are corrected, but an inactive extension declaration changes a selected D0 binding without refusal."
---

# Disposition

**BLOCK - Verified.** The corrected frozen source passes its 37 headless cases. Independent execution confirms that the original new-overload escape and unrelated-conditional false positive are corrected. The conditional binding obligation remains incomplete: a new inactive extension method in D0's namespace can replace the selected method on activation while the inactive guard reports zero errors.

Reviewer/session: codex-astra-d0-review / codex-d0-atlas-rereview. Tree: `C:/Projects/ai-de-review-d0-atlas-correction`; branch: `review/d0-atlas-correction`. Source tip: `921cc2291f291d2cb6f99de9297dba60b90f0d54`, descended from original independent BLOCK `bbca8d6edc495b150c502999fd0fcc69fa922c72`. The earlier receipt remains unchanged; this document refines its disposition rather than erasing its history. No author/product source was edited.

## Goal, scope, and graph

Goal: independent CLEAR/BLOCK of the finite direct-static correction, resolving FR-001/FR-002 without widening scope. Done when actual source, shared handoffs, independent headless execution and discriminating counterexamples establish the disposition in a committed receipt. Not in scope: product edits, main/push, join, full App/Core, STA/native/shown tests, arbitrary transitive/runtime proof or a general preprocessor satisfiability framework. Tier T2; no subagents; 20 tool boundaries / 25 minutes; checkpoint 12.

The optimize-graph delta is bounded: frozen source and Owner contract -> normal-build baseline plus source inspection -> original independent counterexamples -> one concrete binding hypothesis with namespace disconfirmation -> receipt/audit. No retry loop. A scratch compilation error was diagnosed before running the same harness; later execution added exact method-owner instrumentation and the unrelated-namespace control, not a retry-until-green.

Grounding follows the corrected author's receipt, original independent review, and session-contract links; the parent plan's D0 correction section and Owner decision `cl-01M2KWRZ2TDFBQX9JTJK0B82SD` require conditional regions affecting selected binding to refuse, including inactive/Release branches, while unrelated code remains admitted. Existing D0 domain/representation is unchanged. Surface list: census store -> projection/frame -> local/IPC query and registration -> factory row -> complete solution-tree surface and selected shell members -> direct-static analyzer and results. Testing union: D0 hygiene, D1/D2 discriminating deterministic parser/binding inputs, D3 dependency direction, and actual Roslyn/build engine behavior.

## Frozen provenance

Verified working-tree byte SHA256 of `tests/AiDe.App.Tests/SolutionTreeProbeTests.cs`:

`4730DB0143D47E56C51464CCB2F69CEA8E9611955CEC4C5FA2303BAE92E49766`

This checkout uses LF bytes and matches the supplied LF-normalized hash. The author tree's supplied CRLF-byte hash is `0FBC79CDAF1944674A10B3488C32C5420185522382F01DB9D7A3B9CF495D9AE4`; the reviewer does not conflate those byte representations. Corrected author proof hash: `F7AACE09C6360C3EAE516392FC07B1E1E3F7935F2768732A52A155F329BF2883`. Original review receipt hash: `EFB319B3A31D9D7C4FF6DDD89BB15207C801424D43E817BB22DA425FFAE3FC3C`.

The author correction changes only the exact test source, its existing proof, and official audit. The reviewer receipt writer compares the enum block and the entire FileRead/FactoryBuild/RepoRoot suffix against `247e6b4eaefba92cea250f657159a8768e8fb91f`, after newline normalization, before writing this receipt. Those original method bodies remain equal. No product/spec change is part of the correction.

# Actual executions

Normal-build command in the review tree:

```text
dotnet test tests/AiDe.App.Tests/AiDe.App.Tests.csproj --filter 'FullyQualifiedName~SolutionTreeProbeTests.ProbeAtlas_' --logger 'trx;LogFileName=rereview-baseline.trx' --results-directory .artifacts/d0-rereview -v q
```

**Verified:** exit 0; actual TRX 37 executed, 37 passed, zero failed/not executed/aborted. All case names were read: baseline 1, negative cases 24, positive cases 9, directory-classification cases 3. TRX start `2026-09-15T18:36:06.7467163-07:00`, finish `2026-09-15T18:36:22.4402606-07:00`; measured span 15.6935443 seconds. Baseline: 26 roots, 3976 reference observations, 58 admitted identities, zero errors.

Independent scratch command:

```text
dotnet run --project .artifacts/d0-rereview/ReviewSpike.csproj -v q
```

The scratch project references the real test project and calls the frozen private analyzer by reflection. It changes only in-memory syntax trees. The final run returned exit 0 because it records results; **zero process exit is not the review oracle**. The recorded escaped conditional mutation is the review oracle.

Local review artifacts retained:

- `.artifacts/d0-rereview/baseline.log`
- `.artifacts/d0-rereview/rereview-baseline.trx`
- `.artifacts/d0-rereview/ReviewSpike.csproj`
- `.artifacts/d0-rereview/Program.cs`
- `.artifacts/d0-rereview/spike.log`
- `.artifacts/d0-rereview/spike-qualified.log`
- `.artifacts/d0-rereview/handoffs.txt`
- `.artifacts/d0-rereview/write-receipt.py`
- `.artifacts/d0-rereview/receipt-checks.json`

Author raw evidence was read separately under `C:/Projects/ai-de-fix-d0-atlas-correction/.artifacts/d0-correction/`: `correction-red.trx` has 28 executed/24 passed/4 failed; `conditional-width-red.trx` 13/11/2; `correction-frozen.trx` 37/37/0 with zero not executed. The last TRX SHA256 is `92F2168EF678E7B99D4E443406E7D4A199FDC5EA2052CC15825929D4F4BEF3B3`. These historical author runs do not replace the independent run.

# Original finding dispositions

## FR-001: exact shared declaration admission - cleared for the reviewed finite contract

**Verified.** `SharedPorts` now contains exact source-path plus OriginalDefinition documentation identities. Each entry must uniquely resolve and match its source path. Parameter and generic signatures remain in the ID. Core metadata maps uniquely back to source. Type admission does not admit other type members. Constructed named-type and method arguments are inspected before definition normalization. Framework exemption requires metadata origin, namespace, and a listed framework key token; source namespace spelling alone no longer grants admission.

The original independent `OpenKind(int)` injection now reports one UNACCOUNTED error. The identical `FreshReviewHelper(int)` control also reports one. Original existing-port positives remain green. Independently replayed source-defined `System.ReviewEscape` is rejected twice (type and method); generic Atlas argument reports Atlas errors. These results support the bounded declaration-authority correction; they do not assert cryptographic binary validation or arbitrary runtime independence.

## FR-002: original whole-file refusal - corrected; binding obligation still BLOCKED

**Verified.** The original unused conditional Atlas helper in `SurfaceContentFactory` now produces zero errors with the same 3976 D0 reference observations. Retained cases reject conditional bodies, enclosing roots, hidden roots, local/global aliases, inactive definitions, trailing complete-file code, and cross-file partial owner definitions. Unrelated conditional members, registrations, imports in unrelated files and same-name different owners pass their permanent cases. These correct the demonstrated whole-file false positive.

The next finding is a discriminating counterexample to FR-002's already-required binding coverage, not a new transitive or runtime requirement.

# FR-003 - Inactive extension method changes a D0 binding without refusal

**Issue; P1 / Major; Verified. Test Architect BLOCK; architecture/Security BLOCK.**

Evidence: `tests/AiDe.App.Tests/SolutionTreeProbeTests.cs:414` forms binding keys from the already-bound declaring type and member name. At lines 452-453, inactive declarations are relevant only if they match one of these baseline keys, already have local roots/declarations, or contain global imports. A new extension-method owner is not an already-bound owner. The classifier skips its inactive declaration even when C# name lookup can select it instead of an imported extension method.

Independent input: add this source tree at `src/AiDe.App/Workbench/ReviewConditionalExtensions.cs`; make no D0 source edit:

```csharp
namespace AiDe.App.Workbench;
#if RELEASE
internal static class ReviewConditionalExtensions
{
    public static System.Collections.Generic.List<T> ToList<T>(
        this System.Collections.Generic.IEnumerable<T> source)
    {
        System.GC.KeepAlive(typeof(AiDe.Core.Understanding.SourceProjectionState));
        return System.Linq.Enumerable.ToList(source);
    }
}
#endif
```

The spike builds the identical new syntax tree twice, once with no symbols and once with `RELEASE` defined. It inspects the existing `ToList` invocation inside selected `RefreshSolutionTrees` using the real semantic model and reports the method's `ReducedFrom` owner and OriginalDefinition documentation ID. Both variants have zero diagnostics in that selected method.

| Namespace / new-tree symbol | Guard result | Actual selected ToList owner |
| --- | --- | --- |
| AiDe.App.Workbench / RELEASE absent | **0 errors: escaped** | System.Linq.Enumerable |
| AiDe.App.Workbench / RELEASE defined | 3 errors: conditional plus unaccounted D0 calls in surface and shell | **AiDe.App.Workbench.ReviewConditionalExtensions** |
| Review.Unrelated / RELEASE absent | 0 errors | System.Linq.Enumerable |
| Review.Unrelated / RELEASE defined | 0 errors | System.Linq.Enumerable |

Every row retains 26 roots, 3976 observations and 58 admitted ports. The exact switched documentation IDs are:

```text
M:System.Linq.Enumerable.ToList``1(System.Collections.Generic.IEnumerable{``0})
M:AiDe.App.Workbench.ReviewConditionalExtensions.ToList``1(System.Collections.Generic.IEnumerable{``0})
```

The namespace-only disconfirmation keeps the new method name, signature, Atlas body and conditional syntax identical. Moving it to an unimported namespace preserves the real D0 binding in both states and is correctly admitted. Thus simply banning all inactive declarations with a matching member name would widen the policy incorrectly.

Consequence: the loader's inactive parse can certify D0's original framework calls while a permitted inactive declaration changes those same direct calls into an unreviewed Atlas helper when enabled. The guard explicitly promises refusal of inactive binding-affecting definitions; checking only the currently bound owner does not meet that promise. This is not evidence that existing product source currently invokes Atlas through this path, and the artificial `RELEASE` symbol is a controlled parse input rather than an assertion about MSBuild's predefined symbols.

**Veto clears when:**

1. The applicable inactive extension counterexample is refused before its branch is enabled; the active version also remains refused.
2. The identical extension in an unimported unrelated namespace is admitted in both states. Keep the existing unrelated owner/member/registration positives.
3. Permanent discriminating controls establish both behaviors, alongside the retained original 37 cases, exact source/declaration identities, constructed arguments, local/global aliases and enclosing/hidden/partial roots.
4. Independent re-review observes those results against a frozen normal-build source. No whole-file/global member-name ban, Debug-only inference, arbitrary transitive traversal, or general preprocessor solver is admitted by this finding.

# Shared handoffs and residual ceilings

**Verified:** all admitted non-type shared declarations were resolved by exact documentation ID and their current bodies/declarations read from `handoffs.txt`: enum fields, RelayCommand constructor, factory record constructors, announcer interface, shell fields/properties and seven routing/content methods, skip set, endpoint registration, response serialization, query envelope, operation Handle/Refusable, filesystem path comparison, projection framing/activity fields and CandidateWithinWorkspace, store read methods/BeginRead, Surface.Title and Architecture perspective. No direct Atlas use was observed in those handoff bodies. The factory solution-tree row still constructs SolutionTreeSurface; local query and IPC registration still invoke ProjectionService.SolutionTree. Store source remains the existing snapshot/file census, not Atlas.

This closes the earlier review's missing direct shared-handoff-body inspection for the finite contract. It does not prove all callees behind those ports, runtime routing/reflection, generated code beyond the loader's declared generated trees, or rendered D0 behavior. Core/App current compilations, unique declaration binding, root cardinalities and diagnostics are the finite static mechanism. The discovered inactive binding escape is sufficient to retain BLOCK; no broader exploratory review or product repair was undertaken.

**Simplifier disposition: PASS for the bounded repair envelope.** The required correction is the demonstrated direct binding edge plus its unrelated-namespace control. A generic transitive dependency engine or preprocessor framework is outside scope. This is a scope recommendation, not a clearance of the correctness veto.

## Class -> sweep -> derive -> prevent

Class: DC-118, a promised semantic boundary is narrowed during transcription. Sweep: exact original overload and conditional helper, source-defined framework namespace, generic arguments, local/global aliases, partial owner and different-owner positives, and the applicable-versus-unimported extension pair. Derive: a potentially selected declaration need not share the owner of the declaration selected today. Prevent: retain the two extension namespaces and active/inactive variants as permanent oracles; reviewer/parent acceptance must read actual selected symbol identity. The control remains pending author repair and independent observation; the reviewer does not claim it implemented.

Scratch correction: the first console build lacked explicit System.IO, because WPF implicit imports do not provide it. Compilation failed before any mutation ran. The scratch import was corrected; normal compilation is the fail-closed control. No production lesson file was edited; parent owns central lesson-register updates.

# Closure

The bounded review is complete with a precise BLOCK. The parent owns author correction, re-review provisioning, derived/audit union and later canonical qualification/Release under a separate slot. The original BLOCK receipt remains unchanged. This receipt is written with explicit UTF-8, verified temporary bytes and atomic replacement. Own audit and liveness are the only other authored repository/session records. Generated audit-data and raw scratch are retained, never hand-merged or discarded.

Planned budget 20 boundaries / 25 minutes; actual count and automatically measured duration are in the official audit entry. No subagents or budget expansion. The harness did not supply AIDE_CONTRACT_LOG, so no destination was invented. Episode evidence: `docs/proof/d0-atlas-independence-rereview.md`.
