---
id: spike-atlas-behavior-contract
title: "Atlas behavior Roslyn contract spike"
type: reference
status: proposed
owner: "@timianmalloo"
tags: [atlas, spike, roslyn]
links:
  - { to: proof-atlas-behavior-contract, rel: relates-to }
review-by: 2026-12-15
summary: "Six source groups plus four structural/page fixtures, exact ledger oracles and subject faults; source-only experimental evidence, not product admission."
---

# Result: bounded source facts and structural/page experiment

The original six-group record below remains historical evidence. The later
**Structural/page extension** section adds the Owner D1-D5 experiment; its author
observations do not claim independent clearance or product acceptance.

**Verified, Windows only in this author receipt:** all six groups completed with
Roslyn 4.14.0.0 and .NET runtime 10.0.11. The console program compiles synthetic
syntax into a Roslyn semantic model; it does not call Emit or execute that syntax.
The executable itself runs only the analysis harness. No production files,
solution membership, or package versions were changed.

## Negative-first evidence

Before the repair, the harness stopped before fixtures with:

```text
Unhandled exception. System.InvalidOperationException: reflected signature not found: Microsoft.CodeAnalysis.CSharpExtensions.GetSymbolInfo
```

The installed package XML identifies the owner as
`Microsoft.CodeAnalysis.CSharp.CSharpExtensions`; its second parameter is
`ExpressionSyntax`. The prior author had already corrected GetDeclaredSymbol to
that namespace with `BaseMethodDeclarationSyntax`. The parser preflight now checks
the actual `SourceText` overload used by the fixtures. Preflight remains mandatory.

Three executed wrong-algorithm controls precede positive observations:

| Control | Required result | Observed wrong result |
|---|---|---|
| Deduplicate by target type | Three source occurrences | Two distinct target types |
| Sort line/column as strings | `9:1,10:1` | `10:1,9:1` |
| Treat dynamic syntax as a symbol | No resolved target | Nonempty `d.Go` despite null symbol |

These are explicit failing-oracle demonstrations, not mutation coverage of a
production implementation. Assertions require each demonstrated loss to occur.

## Observed scope

| Group | Observation | Bound |
|---|---|---|
| Repeated and recursive calls | Three separate spans, same repeated symbol, recursion binds entry | Source order only |
| Overloads | `Pick(int)` and `Pick(string)` bind distinct symbols | This fixture only |
| Unknown and dynamic | Null symbols; dynamic LateBound, missing name None | No invented dispatch target |
| Branch and loop | If and while condition syntax retained | No CFG or path reachability proof |
| Await, cancellation, throw | Await/throw syntax and CancellationToken method binding | No runtime scheduling or exception propagation proof |
| Malformed and unsupported | CS1026 and retained goto syntax | Goto is a spike policy gap, not unsupported by Roslyn |

## What this cannot answer

This subsection describes the original six-group spike. The bounded paging
observations in the extension below apply only to its four fixed literals.

This does not implement an occurrence model, stable cross-snapshot identity,
production diagnostics, virtual/interface dispatch, cross-project symbol identity,
CFG construction, nested-function support, truncation/paging, or source revision
validation. It does not prove runtime execution order, which branch runs, loop
counts, cancellation delivery, or throw propagation. Numeric source spans are
observed; a production source-location sorting repair is not implemented here.
No native Sequence/Activity rendering, accessibility, Source/Back navigation,
performance budget, or production API is admitted. Linux and project-coverage
before/after evidence belong to the Conductor's independent landing receipt.

Authority: Core Ruling 121, request `req-01M2KBCP7JY9PJTC63PD04WPT2`, resolved
2026-09-15. The spike stays outside AiDe.sln. Its author does not clear review gates.

## Reproduction and full output

Run from this worktree. Restore is needed in a fresh environment; the observed
command reused its existing restore:

```powershell
dotnet run --project spikes/atlas-behavior-contract/AtlasBehaviorContractSpike.csproj --no-restore
```

The following block is populated directly from the observed final command.

```text
ATLAS-BEHAVIOR-CONTRACT-SPIKE/1
runtime=10.0.11
roslyn.csharp=4.14.0.0
roslyn.common=4.14.0.0
synthetic_source=true analyzed_source_executed=false emit_called=false
CONTRACT-SIGNATURES
  Microsoft.CodeAnalysis.SyntaxTree Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(Microsoft.CodeAnalysis.Text.SourceText text, Microsoft.CodeAnalysis.CSharp.CSharpParseOptions options, System.String path, System.Threading.CancellationToken cancellationToken)
  Microsoft.CodeAnalysis.CSharp.CSharpCompilation Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create(System.String assemblyName, System.Collections.Generic.IEnumerable<Microsoft.CodeAnalysis.SyntaxTree> syntaxTrees, System.Collections.Generic.IEnumerable<Microsoft.CodeAnalysis.MetadataReference> references, Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions options)
  Microsoft.CodeAnalysis.IMethodSymbol Microsoft.CodeAnalysis.CSharp.CSharpExtensions.GetDeclaredSymbol(Microsoft.CodeAnalysis.SemanticModel semanticModel, Microsoft.CodeAnalysis.CSharp.Syntax.BaseMethodDeclarationSyntax declarationSyntax, System.Threading.CancellationToken cancellationToken)
  Microsoft.CodeAnalysis.SymbolInfo Microsoft.CodeAnalysis.CSharp.CSharpExtensions.GetSymbolInfo(Microsoft.CodeAnalysis.SemanticModel semanticModel, Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax expression, System.Threading.CancellationToken cancellationToken)
NEGATIVE-FIRST
  NEG-01 type-dedup expected=3 actual=2 oracle=FAIL-AS-EXPECTED
  NEG-02 string-location-order expected=9:1,10:1 actual=10:1,9:1 oracle=FAIL-AS-EXPECTED
  NEG-03 syntax-target-promotion text=d.Go symbol=<null> oracle=FAIL-AS-EXPECTED
FIXTURE repeated-recursive-calls status=OBSERVED occurrences=3 disclosure="static reconstruction — not observed runtime order"
  sourceOrdinal=1 anchor=repeated-recursive.cs:5:9@utf16[39..44) target=B.N() recursive=False
  sourceOrdinal=2 anchor=repeated-recursive.cs:6:9@utf16[54..59) target=A.Run() recursive=True
  sourceOrdinal=3 anchor=repeated-recursive.cs:7:9@utf16[69..74) target=B.N() recursive=False
FIXTURE overload-symbols status=OBSERVED occurrences=2 distinctSymbols=2
  anchor=overloads.cs:7:9@utf16[108..115) target=Overloads.Pick(int)
  anchor=overloads.cs:8:9@utf16[125..136) target=Overloads.Pick(string)
FIXTURE unknown-dynamic-dispatch status=OBSERVED occurrences=2
  anchor=unknown-dynamic.cs:5:9@utf16[59..65) syntax=d.Go target=<gap> candidateReason=LateBound candidates=0
  anchor=unknown-dynamic.cs:6:9@utf16[75..84) syntax=Missing target=<gap> candidateReason=None candidates=0
FIXTURE branch-loop-conditions status=OBSERVED facts=2
  kind=IfStatement anchor=branch-loop.cs:7:9@utf16[85..122) condition="count > 0" evidence=syntax
  kind=WhileStatement anchor=branch-loop.cs:8:9@utf16[131..159) condition="count-- > 0" evidence=syntax
FIXTURE await-cancel-throw-paths status=OBSERVED facts=3
  kind=await anchor=await-cancel-throw.cs:9:9@utf16[183..195) relation=static-continuation runtimeScheduler=<unknown>
  kind=cancel-call anchor=await-cancel-throw.cs:10:9@utf16[205..241) target=System.Threading.CancellationToken.ThrowIfCancellationRequested() evidence=resolved-symbol
  kind=throw anchor=await-cancel-throw.cs:11:9@utf16[251..289) exception=new InvalidOperationException() propagation=<unknown>
FIXTURE malformed-unsupported-syntax status=OBSERVED parseErrors=1 unsupportedGaps=1
  gap=malformed-syntax id=CS1026 anchor=malformed-unsupported.cs:6:17@utf16[85..86)
  gap=unsupported-control kind=GotoStatement anchor=malformed-unsupported.cs:5:21@utf16[56..66)
SUMMARY fixture_groups=6 failures=0 disclosure="static reconstruction — not observed runtime order"
```

## Structural/page extension — Owner D1-D5

**Owner option B — relation-order qualification (2026-09-15).** This experiment
qualifies the **relation set**: identity, endpoints, kind, arm, predicate, anchor
and confidence, including closure, directional stubs and page recomposition.
Sorting relations lexicographically by ID is comparison canonicalization only.
It does **not** qualify the design's endpoint-ordinal/kind-rank/arm-index display
order; the auxiliary endpoint display key and that display-order oracle remain
unproved. Primary ordinal checks and auxiliary inventory/order checks are unchanged.
The recorded output, source, fixtures and expectations are unchanged; no product
gate is cleared by this narrower evidence claim.

**Verified author observation, Windows:** the first complete structural run observed
13 primary facts, 17 auxiliaries, 30 nodes and 30 structural relations; 28 pages and
16 exact recompositions; ten failing-oracle receipts across eight subject faults.
The unchanged six source groups and three earlier negative controls also completed.
The final recording command and full output follow this section.

Governing inputs: independent expected ledger `205d5da4`,
`docs/proof/atlas-views-plan-review.md` in the independent design-review tree;
Owner D1-D5 `4a81eb11ba3d190d59a2680be98be9ee0ca9238a`; design `83e1139b`.
The experiment uses exactly the four literal LF sources B/Branch, L/Loop,
T/WorkAsync and G/Gaps. `StructuralSources` holds them; normal output records SHA256.
The fixed expected hashes reject an unnoticed fixture change.

### Negative-first subject receipts

These deliberately change the subject, then run the dependent normal oracle.
Only the dedicated `OracleFailure` is accepted as a detected fault; a compiler,
parser or unrelated runtime exception fails the process. The normal graph/page
path is then run with no subject fault.

| Subject fault | Observed rejection |
|---|---|
| Remove true branch-region relation | Fixed B relation set and recomposed relation set |
| Remove that relation from small pages | Per-page directional stub set and recomposed relation set |
| Add page size/offset to node identity | Recomposition primary identity comparison |
| Always refuse | B3 cap-4 publication control |
| Omit owning true region | B3 cap-3 refusal/no-publication control |
| Ignore auxiliary cap | B3 cap-3 refusal/no-publication control |
| Traverse opaque Local body | G fixed primary count/identity/anchor set |
| Relabel source relation as runtime | B fixed structural relation set |

The three core faults have five dependent receipts; the other five have one each.
Distinct observed class names and actual caught rejections are counted, rather than
printing eight or ten as unconditional success.

### Normal fixed inventory and pages

| Literal | Primary | Auxiliary | Total nodes | Relations | Pages at size 1 / 2 / 7 / 128 |
|---|---:|---:|---:|---:|---|
| B | 4 | 5 | 9 | 9 | 4 / 2 / 1 / 1 |
| L | 2 | 4 | 6 | 6 | 2 / 1 / 1 / 1 |
| T | 5 | 5 | 10 | 10 | 5 / 3 / 1 / 1 |
| G | 2 | 3 | 5 | 5 | 2 / 1 / 1 / 1 |

Every page checks primary identity/ordinal, exact syntax span/path, predicate,
condition span, target evidence, confidence, auxiliary closure, relation identity
and directional stubs against independent expected data. Recomposition joins each
size back to that complete expected graph, verifies endpoint resolution and rejects
conflicting duplicate auxiliary/relation evidence. Size 128 independently matches
the fixed full graph; the oracle does not merely compare two subject outputs.

D5: B3 `[2,3)` with auxiliary cap 3 refuses `window-unrepresentable`, first limit
`auxiliary-closure`, without a published object. Cap 4 publishes all four required
auxiliaries. B1 `[0,1)` at cap 3 publishes the three common auxiliaries. Edge and stub
caps are 64 each; encoded page-content cap 65,536 UTF-8 bytes; statement-visit charge
10,000 and region recursion depth 64. These are experimental settings, not production
defaults. The byte check measures serialized PageContent, not a production envelope.
Invalid limits 0/129, offsets -1/4 and a foreign observation all refuse with no
published object. This harness has no Core token or receipt-minting path.

### Identity and expected-result independence

Before the graph subject existed, direct Roslyn parsing of the literal sources
recorded exact UTF-16 spans and `ChildNodes()` index paths from the selected method
body. Those literal numbers are frozen in `IndependentExpected`; no expected span,
region, relation or closure is read from `StructuralBuilder` or `ProjectPage`.
The expected page adapter applies set inclusion to the fixed independent relation
list and an explicit per-primary closure table. Both sides share only record types,
not graph construction, closure traversal or the subject identity helper.

Experimental observation names B/L/T/G refer exclusively to these hash-checked
literals. Primary identity includes that observation, syntax kind/role, full span
and child path. Auxiliary identity adds owning method/primary and region role.
Relations use canonical endpoints, kind and arm. These strings are test identities,
not accepted Core tokens or a production wire format.

### Limits

This is a **structural source graph**, not executable CFG or runtime behavior.
No successor, loop-back, scheduler, catch-target or execution edge is admitted.
G's local and lock bodies are opaque; no nested Ping node/edge/target survives.
Only the four fixed syntax combinations are qualified. Generic nested blocks,
other controls, real-workspace observation authority, source mutation, production
continuations/receipts, truncation after global caps, semantic unresolved-target
versus cap-gap states, parser stress/cancellation, dispatch races, frame targets,
native UI and performance admission remain unproved. Boundary-stub distinction from
semantic/cap gaps is a design rule, not a new exercised claim here.

The normal-path elapsed time includes faults, all four analyses, paging and console
work in this synthetic process; it is not p95 or a production latency measurement.
Linux compatibility and project-coverage before/after remain Conductor receipts for
this revised source. Independent review and P0.7 product semantics remain open.

### Final recording command and full observed output

```powershell
dotnet run --project spikes/atlas-behavior-contract/AtlasBehaviorContractSpike.csproj --no-restore
```
```text
ATLAS-BEHAVIOR-CONTRACT-SPIKE/1
runtime=10.0.11
roslyn.csharp=4.14.0.0
roslyn.common=4.14.0.0
synthetic_source=true analyzed_source_executed=false emit_called=false
CONTRACT-SIGNATURES
  Microsoft.CodeAnalysis.SyntaxTree Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(Microsoft.CodeAnalysis.Text.SourceText text, Microsoft.CodeAnalysis.CSharp.CSharpParseOptions options, System.String path, System.Threading.CancellationToken cancellationToken)
  Microsoft.CodeAnalysis.CSharp.CSharpCompilation Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create(System.String assemblyName, System.Collections.Generic.IEnumerable<Microsoft.CodeAnalysis.SyntaxTree> syntaxTrees, System.Collections.Generic.IEnumerable<Microsoft.CodeAnalysis.MetadataReference> references, Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions options)
  Microsoft.CodeAnalysis.IMethodSymbol Microsoft.CodeAnalysis.CSharp.CSharpExtensions.GetDeclaredSymbol(Microsoft.CodeAnalysis.SemanticModel semanticModel, Microsoft.CodeAnalysis.CSharp.Syntax.BaseMethodDeclarationSyntax declarationSyntax, System.Threading.CancellationToken cancellationToken)
  Microsoft.CodeAnalysis.SymbolInfo Microsoft.CodeAnalysis.CSharp.CSharpExtensions.GetSymbolInfo(Microsoft.CodeAnalysis.SemanticModel semanticModel, Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax expression, System.Threading.CancellationToken cancellationToken)
NEGATIVE-FIRST
  NEG-01 type-dedup expected=3 actual=2 oracle=FAIL-AS-EXPECTED
  NEG-02 string-location-order expected=9:1,10:1 actual=10:1,9:1 oracle=FAIL-AS-EXPECTED
  NEG-03 syntax-target-promotion text=d.Go symbol=<null> oracle=FAIL-AS-EXPECTED
FIXTURE repeated-recursive-calls status=OBSERVED occurrences=3 disclosure="static reconstruction — not observed runtime order"
  sourceOrdinal=1 anchor=repeated-recursive.cs:5:9@utf16[39..44) target=B.N() recursive=False
  sourceOrdinal=2 anchor=repeated-recursive.cs:6:9@utf16[54..59) target=A.Run() recursive=True
  sourceOrdinal=3 anchor=repeated-recursive.cs:7:9@utf16[69..74) target=B.N() recursive=False
FIXTURE overload-symbols status=OBSERVED occurrences=2 distinctSymbols=2
  anchor=overloads.cs:7:9@utf16[108..115) target=Overloads.Pick(int)
  anchor=overloads.cs:8:9@utf16[125..136) target=Overloads.Pick(string)
FIXTURE unknown-dynamic-dispatch status=OBSERVED occurrences=2
  anchor=unknown-dynamic.cs:5:9@utf16[59..65) syntax=d.Go target=<gap> candidateReason=LateBound candidates=0
  anchor=unknown-dynamic.cs:6:9@utf16[75..84) syntax=Missing target=<gap> candidateReason=None candidates=0
FIXTURE branch-loop-conditions status=OBSERVED facts=2
  kind=IfStatement anchor=branch-loop.cs:7:9@utf16[85..122) condition="count > 0" evidence=syntax
  kind=WhileStatement anchor=branch-loop.cs:8:9@utf16[131..159) condition="count-- > 0" evidence=syntax
FIXTURE await-cancel-throw-paths status=OBSERVED facts=3
  kind=await anchor=await-cancel-throw.cs:9:9@utf16[183..195) relation=static-continuation runtimeScheduler=<unknown>
  kind=cancel-call anchor=await-cancel-throw.cs:10:9@utf16[205..241) target=System.Threading.CancellationToken.ThrowIfCancellationRequested() evidence=resolved-symbol
  kind=throw anchor=await-cancel-throw.cs:11:9@utf16[251..289) exception=new InvalidOperationException() propagation=<unknown>
FIXTURE malformed-unsupported-syntax status=OBSERVED parseErrors=1 unsupportedGaps=1
  gap=malformed-syntax id=CS1026 anchor=malformed-unsupported.cs:6:17@utf16[85..86)
  gap=unsupported-control kind=GotoStatement anchor=malformed-unsupported.cs:5:21@utf16[56..66)
SUMMARY fixture_groups=6 failures=0 disclosure="static reconstruction — not observed runtime order"
STRUCTURAL-PAGE-NEGATIVE-FIRST ledger=205d5da4 owner=4a81eb11 structural_source_only=true
SUBJECT-FAULT drop-branch/fixed-set oracle=FAIL-AS-EXPECTED reason=B:fixed-structural-relations
SUBJECT-FAULT drop-branch/recomposition oracle=FAIL-AS-EXPECTED reason=recomposition:relations
SUBJECT-FAULT drop-cross-window/per-page oracle=FAIL-AS-EXPECTED reason=B1-page:boundary-stubs
SUBJECT-FAULT drop-cross-window/recomposition oracle=FAIL-AS-EXPECTED reason=recomposition:relations
SUBJECT-FAULT page-dependent-identity oracle=FAIL-AS-EXPECTED reason=recomposition:primary-identities
SUBJECT-FAULT always-refuse oracle=FAIL-AS-EXPECTED reason=D5:B3-cap4-publishes-four:publication
SUBJECT-FAULT omit-owning-region oracle=FAIL-AS-EXPECTED reason=D5:B3-cap3-refuses-auxiliary-first-no-publication
SUBJECT-FAULT ignore-auxiliary-cap oracle=FAIL-AS-EXPECTED reason=D5:B3-cap3-refuses-auxiliary-first-no-publication
SUBJECT-FAULT traverse-opaque-local-body oracle=FAIL-AS-EXPECTED reason=G:fixed-primary-identity-anchor-predicate
SUBJECT-FAULT invent-runtime-edge oracle=FAIL-AS-EXPECTED reason=B:fixed-structural-relations
STRUCTURAL B primary=4 auxiliary=5 nodes=9 edges=9 source_sha256=eae4cee81bb7e417c19a62f7cedc04c2e7316b5087b65bc75f0c07094affbcfa
  PRIMARY id=B|InvocationExpression|call|59:6|0.0 ordinal=0 condition=<none> conditionSpan=: target=B.Ping() confidence=extracted gap=<none>
  PRIMARY id=B|IfStatement|control|71:38|1 ordinal=1 condition=flag conditionSpan=75:4 target=<none> confidence=extracted gap=<none>
  PRIMARY id=B|InvocationExpression|call|83:6|1.1.0.0 ordinal=2 condition=<none> conditionSpan=: target=B.Ping() confidence=extracted gap=<none>
  PRIMARY id=B|InvocationExpression|call|100:6|1.2.0.0.0 ordinal=3 condition=<none> conditionSpan=: target=B.Ping() confidence=extracted gap=<none>
  AUX Entry id=B|method:30:83|region:Entry owner=B|method:30:83 parent=<root> anchor=53:1 confidence=extracted
  AUX MethodBody id=B|method:30:83|region:MethodBody owner=B|method:30:83 parent=<root> anchor=53:60 confidence=extracted
  AUX B.true id=B|IfStatement|control|71:38|1|region:true owner=B|IfStatement|control|71:38|1 parent=B|method:30:83|region:MethodBody anchor=71:38 confidence=extracted
  AUX B.false id=B|IfStatement|control|71:38|1|region:false owner=B|IfStatement|control|71:38|1 parent=B|method:30:83|region:MethodBody anchor=71:38 confidence=extracted
  AUX Exit id=B|method:30:83|region:Exit owner=B|method:30:83 parent=<root> anchor=112:1 confidence=extracted
  RELATION B2>B.false kind=WhenFalseRegion arm=1 predicate=flag anchor=75:4 confidence=extracted
  RELATION B2>B.true kind=WhenTrueRegion arm=0 predicate=flag anchor=75:4 confidence=extracted
  RELATION B.false>B4 kind=Contains arm=0 predicate=Contains anchor=100:6 confidence=extracted
  RELATION B.true>B3 kind=Contains arm=0 predicate=Contains anchor=83:6 confidence=extracted
  RELATION B1>B2 kind=NextInSource arm=0 predicate=NextInSource anchor=59:6 confidence=extracted
  RELATION MethodBody>B2 kind=Contains arm=0 predicate=Contains anchor=71:38 confidence=extracted
  RELATION MethodBody>B1 kind=Contains arm=0 predicate=Contains anchor=59:6 confidence=extracted
  RELATION MethodBody>Entry kind=Contains arm=0 predicate=Contains anchor=53:1 confidence=extracted
  RELATION MethodBody>Exit kind=Contains arm=0 predicate=Contains anchor=112:1 confidence=extracted
  PAGE size=1 offset=0 primary=1 closure=[Entry,MethodBody,Exit] edges=3 stubs=2 encoded_content_bytes=2893
    STUB B1>B2 kind=NextInSource missing=B2 direction=outgoing primaryOrdinal=1 ownerOrdinal=<none> reason=outside-window
    STUB MethodBody>B2 kind=Contains missing=B2 direction=outgoing primaryOrdinal=1 ownerOrdinal=<none> reason=outside-window
  PAGE size=1 offset=1 primary=1 closure=[Entry,MethodBody,Exit] edges=3 stubs=4 encoded_content_bytes=3809
    STUB B2>B.false kind=WhenFalseRegion missing=B.false direction=outgoing primaryOrdinal=<none> ownerOrdinal=1 reason=outside-window
    STUB B2>B.true kind=WhenTrueRegion missing=B.true direction=outgoing primaryOrdinal=<none> ownerOrdinal=1 reason=outside-window
    STUB B1>B2 kind=NextInSource missing=B1 direction=incoming primaryOrdinal=0 ownerOrdinal=<none> reason=outside-window
    STUB MethodBody>B1 kind=Contains missing=B1 direction=outgoing primaryOrdinal=0 ownerOrdinal=<none> reason=outside-window
  PAGE size=1 offset=2 primary=1 closure=[Entry,MethodBody,B.true,Exit] edges=3 stubs=3 encoded_content_bytes=3728
    STUB B2>B.true kind=WhenTrueRegion missing=B2 direction=incoming primaryOrdinal=1 ownerOrdinal=<none> reason=outside-window
    STUB MethodBody>B2 kind=Contains missing=B2 direction=outgoing primaryOrdinal=1 ownerOrdinal=<none> reason=outside-window
    STUB MethodBody>B1 kind=Contains missing=B1 direction=outgoing primaryOrdinal=0 ownerOrdinal=<none> reason=outside-window
  PAGE size=1 offset=3 primary=1 closure=[Entry,MethodBody,B.false,Exit] edges=3 stubs=3 encoded_content_bytes=3750
    STUB B2>B.false kind=WhenFalseRegion missing=B2 direction=incoming primaryOrdinal=1 ownerOrdinal=<none> reason=outside-window
    STUB MethodBody>B2 kind=Contains missing=B2 direction=outgoing primaryOrdinal=1 ownerOrdinal=<none> reason=outside-window
    STUB MethodBody>B1 kind=Contains missing=B1 direction=outgoing primaryOrdinal=0 ownerOrdinal=<none> reason=outside-window
  RECOMPOSE size=1 status=OBSERVED exact_identity_anchor_predicate_confidence_relations=true
  PAGE size=2 offset=0 primary=2 closure=[Entry,MethodBody,Exit] edges=5 stubs=2 encoded_content_bytes=3798
    STUB B2>B.false kind=WhenFalseRegion missing=B.false direction=outgoing primaryOrdinal=<none> ownerOrdinal=1 reason=outside-window
    STUB B2>B.true kind=WhenTrueRegion missing=B.true direction=outgoing primaryOrdinal=<none> ownerOrdinal=1 reason=outside-window
  PAGE size=2 offset=2 primary=2 closure=[Entry,MethodBody,B.true,B.false,Exit] edges=4 stubs=4 encoded_content_bytes=5182
    STUB B2>B.false kind=WhenFalseRegion missing=B2 direction=incoming primaryOrdinal=1 ownerOrdinal=<none> reason=outside-window
    STUB B2>B.true kind=WhenTrueRegion missing=B2 direction=incoming primaryOrdinal=1 ownerOrdinal=<none> reason=outside-window
    STUB MethodBody>B2 kind=Contains missing=B2 direction=outgoing primaryOrdinal=1 ownerOrdinal=<none> reason=outside-window
    STUB MethodBody>B1 kind=Contains missing=B1 direction=outgoing primaryOrdinal=0 ownerOrdinal=<none> reason=outside-window
  RECOMPOSE size=2 status=OBSERVED exact_identity_anchor_predicate_confidence_relations=true
  PAGE size=7 offset=0 primary=4 closure=[Entry,MethodBody,B.true,B.false,Exit] edges=9 stubs=0 encoded_content_bytes=5442
  RECOMPOSE size=7 status=OBSERVED exact_identity_anchor_predicate_confidence_relations=true
  PAGE size=128 offset=0 primary=4 closure=[Entry,MethodBody,B.true,B.false,Exit] edges=9 stubs=0 encoded_content_bytes=5442
  RECOMPOSE size=128 status=OBSERVED exact_identity_anchor_predicate_confidence_relations=true
STRUCTURAL L primary=2 auxiliary=4 nodes=6 edges=6 source_sha256=fd35da4ab3e1c45ae74153dcffce467e7f17b9c3f5a33a496da0e8e996f17b5e
  PRIMARY id=L|WhileStatement|control|57:24|0 ordinal=0 condition=more conditionSpan=64:4 target=<none> confidence=extracted gap=<none>
  PRIMARY id=L|InvocationExpression|call|72:6|0.1.0.0 ordinal=1 condition=<none> conditionSpan=: target=L.Tick() confidence=extracted gap=<none>
  AUX Entry id=L|method:30:55|region:Entry owner=L|method:30:55 parent=<root> anchor=51:1 confidence=extracted
  AUX MethodBody id=L|method:30:55|region:MethodBody owner=L|method:30:55 parent=<root> anchor=51:34 confidence=extracted
  AUX L.body id=L|WhileStatement|control|57:24|0|region:body owner=L|WhileStatement|control|57:24|0 parent=L|method:30:55|region:MethodBody anchor=57:24 confidence=extracted
  AUX Exit id=L|method:30:55|region:Exit owner=L|method:30:55 parent=<root> anchor=84:1 confidence=extracted
  RELATION L1>L.body kind=LoopBodyRegion arm=0 predicate=LoopBodyRegion anchor=57:24 confidence=extracted
  RELATION L1>L.body kind=LoopConditionSource arm=0 predicate=more anchor=64:4 confidence=extracted
  RELATION L.body>L2 kind=Contains arm=0 predicate=Contains anchor=72:6 confidence=extracted
  RELATION MethodBody>L1 kind=Contains arm=0 predicate=Contains anchor=57:24 confidence=extracted
  RELATION MethodBody>Entry kind=Contains arm=0 predicate=Contains anchor=51:1 confidence=extracted
  RELATION MethodBody>Exit kind=Contains arm=0 predicate=Contains anchor=84:1 confidence=extracted
  PAGE size=1 offset=0 primary=1 closure=[Entry,MethodBody,Exit] edges=3 stubs=2 encoded_content_bytes=2973
    STUB L1>L.body kind=LoopBodyRegion missing=L.body direction=outgoing primaryOrdinal=<none> ownerOrdinal=0 reason=outside-window
    STUB L1>L.body kind=LoopConditionSource missing=L.body direction=outgoing primaryOrdinal=<none> ownerOrdinal=0 reason=outside-window
  PAGE size=1 offset=1 primary=1 closure=[Entry,MethodBody,L.body,Exit] edges=3 stubs=3 encoded_content_bytes=3806
    STUB L1>L.body kind=LoopBodyRegion missing=L1 direction=incoming primaryOrdinal=0 ownerOrdinal=<none> reason=outside-window
    STUB L1>L.body kind=LoopConditionSource missing=L1 direction=incoming primaryOrdinal=0 ownerOrdinal=<none> reason=outside-window
    STUB MethodBody>L1 kind=Contains missing=L1 direction=outgoing primaryOrdinal=0 ownerOrdinal=<none> reason=outside-window
  RECOMPOSE size=1 status=OBSERVED exact_identity_anchor_predicate_confidence_relations=true
  PAGE size=2 offset=0 primary=2 closure=[Entry,MethodBody,L.body,Exit] edges=6 stubs=0 encoded_content_bytes=3625
  RECOMPOSE size=2 status=OBSERVED exact_identity_anchor_predicate_confidence_relations=true
  PAGE size=7 offset=0 primary=2 closure=[Entry,MethodBody,L.body,Exit] edges=6 stubs=0 encoded_content_bytes=3625
  RECOMPOSE size=7 status=OBSERVED exact_identity_anchor_predicate_confidence_relations=true
  PAGE size=128 offset=0 primary=2 closure=[Entry,MethodBody,L.body,Exit] edges=6 stubs=0 encoded_content_bytes=3625
  RECOMPOSE size=128 status=OBSERVED exact_identity_anchor_predicate_confidence_relations=true
STRUCTURAL T primary=5 auxiliary=5 nodes=10 edges=10 source_sha256=9536cb46d00c9538f852f6795dd0512a1d65658340caffb11161cd68daea9bed
  PRIMARY id=T|TryStatement|control|134:53|0 ordinal=0 condition=<none> conditionSpan=: target=<none> confidence=extracted gap=<none>
  PRIMARY id=T|AwaitExpression|await|140:17|0.0.0.0 ordinal=1 condition=<none> conditionSpan=: target=<none> confidence=extracted gap=<none>
  PRIMARY id=T|InvocationExpression|call|146:11|0.0.0.0.0 ordinal=2 condition=<none> conditionSpan=: target=T.SendAsync() confidence=extracted gap=<none>
  PRIMARY id=T|FinallyClause|control|165:22|0.1 ordinal=3 condition=<none> conditionSpan=: target=<none> confidence=extracted gap=<none>
  PRIMARY id=T|InvocationExpression|call|175:9|0.1.0.0.0 ordinal=4 condition=<none> conditionSpan=: target=T.Cleanup() confidence=extracted gap=<none>
  AUX Entry id=T|method:105:86|region:Entry owner=T|method:105:86 parent=<root> anchor=128:1 confidence=extracted
  AUX MethodBody id=T|method:105:86|region:MethodBody owner=T|method:105:86 parent=<root> anchor=128:63 confidence=extracted
  AUX T.try id=T|TryStatement|control|134:53|0|region:try owner=T|TryStatement|control|134:53|0 parent=T|method:105:86|region:MethodBody anchor=134:53 confidence=extracted
  AUX T.finally id=T|FinallyClause|control|165:22|0.1|region:finally owner=T|FinallyClause|control|165:22|0.1 parent=T|TryStatement|control|134:53|0|region:try anchor=165:22 confidence=extracted
  AUX Exit id=T|method:105:86|region:Exit owner=T|method:105:86 parent=<root> anchor=190:1 confidence=extracted
  RELATION T2>T3 kind=AwaitOperand arm=0 predicate=AwaitOperand anchor=140:17 confidence=extracted
  RELATION T4>T.finally kind=Contains arm=0 predicate=Contains anchor=165:22 confidence=extracted
  RELATION T.finally>T5 kind=Contains arm=0 predicate=Contains anchor=175:9 confidence=extracted
  RELATION T1>T.try kind=Contains arm=0 predicate=Contains anchor=134:53 confidence=extracted
  RELATION T.try>T2 kind=Contains arm=0 predicate=Contains anchor=140:17 confidence=extracted
  RELATION T.try>T4 kind=Contains arm=0 predicate=Contains anchor=165:22 confidence=extracted
  RELATION T.try>T.finally kind=FinallyDeclaration arm=0 predicate=FinallyDeclaration anchor=134:53 confidence=extracted
  RELATION MethodBody>T1 kind=Contains arm=0 predicate=Contains anchor=134:53 confidence=extracted
  RELATION MethodBody>Entry kind=Contains arm=0 predicate=Contains anchor=128:1 confidence=extracted
  RELATION MethodBody>Exit kind=Contains arm=0 predicate=Contains anchor=190:1 confidence=extracted
  PAGE size=1 offset=0 primary=1 closure=[Entry,MethodBody,Exit] edges=3 stubs=1 encoded_content_bytes=2485
    STUB T1>T.try kind=Contains missing=T.try direction=outgoing primaryOrdinal=<none> ownerOrdinal=0 reason=outside-window
  PAGE size=1 offset=1 primary=1 closure=[Entry,MethodBody,T.try,Exit] edges=3 stubs=5 encoded_content_bytes=4794
    STUB T2>T3 kind=AwaitOperand missing=T3 direction=outgoing primaryOrdinal=2 ownerOrdinal=<none> reason=outside-window
    STUB T1>T.try kind=Contains missing=T1 direction=incoming primaryOrdinal=0 ownerOrdinal=<none> reason=outside-window
    STUB T.try>T4 kind=Contains missing=T4 direction=outgoing primaryOrdinal=3 ownerOrdinal=<none> reason=outside-window
    STUB T.try>T.finally kind=FinallyDeclaration missing=T.finally direction=outgoing primaryOrdinal=<none> ownerOrdinal=3 reason=outside-window
    STUB MethodBody>T1 kind=Contains missing=T1 direction=outgoing primaryOrdinal=0 ownerOrdinal=<none> reason=outside-window
  PAGE size=1 offset=2 primary=1 closure=[Entry,MethodBody,T.try,Exit] edges=2 stubs=6 encoded_content_bytes=4979
    STUB T2>T3 kind=AwaitOperand missing=T2 direction=incoming primaryOrdinal=1 ownerOrdinal=<none> reason=outside-window
    STUB T1>T.try kind=Contains missing=T1 direction=incoming primaryOrdinal=0 ownerOrdinal=<none> reason=outside-window
    STUB T.try>T2 kind=Contains missing=T2 direction=outgoing primaryOrdinal=1 ownerOrdinal=<none> reason=outside-window
    STUB T.try>T4 kind=Contains missing=T4 direction=outgoing primaryOrdinal=3 ownerOrdinal=<none> reason=outside-window
    STUB T.try>T.finally kind=FinallyDeclaration missing=T.finally direction=outgoing primaryOrdinal=<none> ownerOrdinal=3 reason=outside-window
    STUB MethodBody>T1 kind=Contains missing=T1 direction=outgoing primaryOrdinal=0 ownerOrdinal=<none> reason=outside-window
  PAGE size=1 offset=3 primary=1 closure=[Entry,MethodBody,T.try,Exit] edges=3 stubs=5 encoded_content_bytes=4785
    STUB T4>T.finally kind=Contains missing=T.finally direction=outgoing primaryOrdinal=<none> ownerOrdinal=3 reason=outside-window
    STUB T1>T.try kind=Contains missing=T1 direction=incoming primaryOrdinal=0 ownerOrdinal=<none> reason=outside-window
    STUB T.try>T2 kind=Contains missing=T2 direction=outgoing primaryOrdinal=1 ownerOrdinal=<none> reason=outside-window
    STUB T.try>T.finally kind=FinallyDeclaration missing=T.finally direction=outgoing primaryOrdinal=<none> ownerOrdinal=3 reason=outside-window
    STUB MethodBody>T1 kind=Contains missing=T1 direction=outgoing primaryOrdinal=0 ownerOrdinal=<none> reason=outside-window
  PAGE size=1 offset=4 primary=1 closure=[Entry,MethodBody,T.try,T.finally,Exit] edges=4 stubs=5 encoded_content_bytes=5485
    STUB T4>T.finally kind=Contains missing=T4 direction=incoming primaryOrdinal=3 ownerOrdinal=<none> reason=outside-window
    STUB T1>T.try kind=Contains missing=T1 direction=incoming primaryOrdinal=0 ownerOrdinal=<none> reason=outside-window
    STUB T.try>T2 kind=Contains missing=T2 direction=outgoing primaryOrdinal=1 ownerOrdinal=<none> reason=outside-window
    STUB T.try>T4 kind=Contains missing=T4 direction=outgoing primaryOrdinal=3 ownerOrdinal=<none> reason=outside-window
    STUB MethodBody>T1 kind=Contains missing=T1 direction=outgoing primaryOrdinal=0 ownerOrdinal=<none> reason=outside-window
  RECOMPOSE size=1 status=OBSERVED exact_identity_anchor_predicate_confidence_relations=true
  PAGE size=2 offset=0 primary=2 closure=[Entry,MethodBody,T.try,Exit] edges=5 stubs=3 encoded_content_bytes=4778
    STUB T2>T3 kind=AwaitOperand missing=T3 direction=outgoing primaryOrdinal=2 ownerOrdinal=<none> reason=outside-window
    STUB T.try>T4 kind=Contains missing=T4 direction=outgoing primaryOrdinal=3 ownerOrdinal=<none> reason=outside-window
    STUB T.try>T.finally kind=FinallyDeclaration missing=T.finally direction=outgoing primaryOrdinal=<none> ownerOrdinal=3 reason=outside-window
  PAGE size=2 offset=2 primary=2 closure=[Entry,MethodBody,T.try,Exit] edges=3 stubs=6 encoded_content_bytes=5624
    STUB T2>T3 kind=AwaitOperand missing=T2 direction=incoming primaryOrdinal=1 ownerOrdinal=<none> reason=outside-window
    STUB T4>T.finally kind=Contains missing=T.finally direction=outgoing primaryOrdinal=<none> ownerOrdinal=3 reason=outside-window
    STUB T1>T.try kind=Contains missing=T1 direction=incoming primaryOrdinal=0 ownerOrdinal=<none> reason=outside-window
    STUB T.try>T2 kind=Contains missing=T2 direction=outgoing primaryOrdinal=1 ownerOrdinal=<none> reason=outside-window
    STUB T.try>T.finally kind=FinallyDeclaration missing=T.finally direction=outgoing primaryOrdinal=<none> ownerOrdinal=3 reason=outside-window
    STUB MethodBody>T1 kind=Contains missing=T1 direction=outgoing primaryOrdinal=0 ownerOrdinal=<none> reason=outside-window
  PAGE size=2 offset=4 primary=1 closure=[Entry,MethodBody,T.try,T.finally,Exit] edges=4 stubs=5 encoded_content_bytes=5485
    STUB T4>T.finally kind=Contains missing=T4 direction=incoming primaryOrdinal=3 ownerOrdinal=<none> reason=outside-window
    STUB T1>T.try kind=Contains missing=T1 direction=incoming primaryOrdinal=0 ownerOrdinal=<none> reason=outside-window
    STUB T.try>T2 kind=Contains missing=T2 direction=outgoing primaryOrdinal=1 ownerOrdinal=<none> reason=outside-window
    STUB T.try>T4 kind=Contains missing=T4 direction=outgoing primaryOrdinal=3 ownerOrdinal=<none> reason=outside-window
    STUB MethodBody>T1 kind=Contains missing=T1 direction=outgoing primaryOrdinal=0 ownerOrdinal=<none> reason=outside-window
  RECOMPOSE size=2 status=OBSERVED exact_identity_anchor_predicate_confidence_relations=true
  PAGE size=7 offset=0 primary=5 closure=[Entry,MethodBody,T.try,T.finally,Exit] edges=10 stubs=0 encoded_content_bytes=6295
  RECOMPOSE size=7 status=OBSERVED exact_identity_anchor_predicate_confidence_relations=true
  PAGE size=128 offset=0 primary=5 closure=[Entry,MethodBody,T.try,T.finally,Exit] edges=10 stubs=0 encoded_content_bytes=6295
  RECOMPOSE size=128 status=OBSERVED exact_identity_anchor_predicate_confidence_relations=true
STRUCTURAL G primary=2 auxiliary=3 nodes=5 edges=5 source_sha256=b0ce284f93a055d586e0b5b14d69d5f432c489fbc39c28c7255a82f9fd11b066
  PRIMARY id=G|LocalFunctionStatement|gap|59:24|0 ordinal=0 condition=<none> conditionSpan=: target=<none> confidence=unknown gap=nested-body-not-expanded
  PRIMARY id=G|LockStatement|gap|88:15|1 ordinal=1 condition=<none> conditionSpan=: target=<none> confidence=unknown gap=unsupported-lock
  AUX Entry id=G|method:30:77|region:Entry owner=G|method:30:77 parent=<root> anchor=53:1 confidence=extracted
  AUX MethodBody id=G|method:30:77|region:MethodBody owner=G|method:30:77 parent=<root> anchor=53:54 confidence=extracted
  AUX Exit id=G|method:30:77|region:Exit owner=G|method:30:77 parent=<root> anchor=106:1 confidence=extracted
  RELATION G1>G2 kind=NextInSource arm=0 predicate=NextInSource anchor=59:24 confidence=extracted
  RELATION MethodBody>G1 kind=Contains arm=0 predicate=Contains anchor=59:24 confidence=extracted
  RELATION MethodBody>G2 kind=Contains arm=0 predicate=Contains anchor=88:15 confidence=extracted
  RELATION MethodBody>Entry kind=Contains arm=0 predicate=Contains anchor=53:1 confidence=extracted
  RELATION MethodBody>Exit kind=Contains arm=0 predicate=Contains anchor=106:1 confidence=extracted
  PAGE size=1 offset=0 primary=1 closure=[Entry,MethodBody,Exit] edges=3 stubs=2 encoded_content_bytes=2897
    STUB G1>G2 kind=NextInSource missing=G2 direction=outgoing primaryOrdinal=1 ownerOrdinal=<none> reason=outside-window
    STUB MethodBody>G2 kind=Contains missing=G2 direction=outgoing primaryOrdinal=1 ownerOrdinal=<none> reason=outside-window
  PAGE size=1 offset=1 primary=1 closure=[Entry,MethodBody,Exit] edges=3 stubs=2 encoded_content_bytes=2889
    STUB G1>G2 kind=NextInSource missing=G1 direction=incoming primaryOrdinal=0 ownerOrdinal=<none> reason=outside-window
    STUB MethodBody>G1 kind=Contains missing=G1 direction=outgoing primaryOrdinal=0 ownerOrdinal=<none> reason=outside-window
  RECOMPOSE size=1 status=OBSERVED exact_identity_anchor_predicate_confidence_relations=true
  PAGE size=2 offset=0 primary=2 closure=[Entry,MethodBody,Exit] edges=5 stubs=0 encoded_content_bytes=2893
  RECOMPOSE size=2 status=OBSERVED exact_identity_anchor_predicate_confidence_relations=true
  PAGE size=7 offset=0 primary=2 closure=[Entry,MethodBody,Exit] edges=5 stubs=0 encoded_content_bytes=2893
  RECOMPOSE size=7 status=OBSERVED exact_identity_anchor_predicate_confidence_relations=true
  PAGE size=128 offset=0 primary=2 closure=[Entry,MethodBody,Exit] edges=5 stubs=0 encoded_content_bytes=2893
  RECOMPOSE size=128 status=OBSERVED exact_identity_anchor_predicate_confidence_relations=true
D5 B3 cap=3 outcome=window-unrepresentable firstLimit=auxiliary-closure published=false
D5 B3 cap=4 outcome=published auxiliaries=4; B1 cap=3 outcome=published auxiliaries=3
REFUSALS invalid-limit=2 invalid-offset=2 invalid-observation=1 published=0
STRUCTURAL-SUMMARY groups=4 primary=13 auxiliary=17 nodes=30 edges=30 pages=28 recompositions=16 fault_oracle_rejections=10 distinct_subject_faults=8 elapsed_ms=59.441
```
