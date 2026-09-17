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
summary: "Six synthetic source-analysis groups and three negative controls observed with pinned Roslyn 4.14; product admission remains separate."
---

# Result: bounded source facts are available

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
