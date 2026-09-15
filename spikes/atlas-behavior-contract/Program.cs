using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AtlasBehaviorContractSpike;

internal static class Program
{
    private const string Disclosure = "static reconstruction — not observed runtime order";

    private const string RepeatedRecursiveSource = """
        class A
        {
            void Run()
            {
                B.N();
                Run();
                B.N();
            }
        }
        static class B { public static void N() { } }
        """;

    private const string UnknownDynamicSource = """
        class UnknownCalls
        {
            void Run(dynamic d)
            {
                d.Go();
                Missing();
            }
        }
        """;

    private static readonly Lazy<ImmutableArray<MetadataReference>> PlatformReferences = new(() =>
    {
        var trusted = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string
            ?? throw new InvalidOperationException("TRUSTED_PLATFORM_ASSEMBLIES was not recorded.");
        return [.. trusted.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.Ordinal)
            .Select(static path => MetadataReference.CreateFromFile(path))];
    });

    private static readonly SymbolDisplayFormat TargetFormat = SymbolDisplayFormat.CSharpErrorMessageFormat;

    public static int Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("ATLAS-BEHAVIOR-CONTRACT-SPIKE/1");
        Console.WriteLine($"runtime={Environment.Version}");
        Console.WriteLine($"roslyn.csharp={typeof(CSharpCompilation).Assembly.GetName().Version}");
        Console.WriteLine($"roslyn.common={typeof(SemanticModel).Assembly.GetName().Version}");
        Console.WriteLine("synthetic_source=true analyzed_source_executed=false emit_called=false");
        PrintSignatures();

        ObserveNegativeFirst();

        var observed = 0;
        ObserveRepeatedRecursive(); observed++;
        ObserveOverloads(); observed++;
        ObserveUnknownDynamic(); observed++;
        ObserveBranchLoop(); observed++;
        ObserveAwaitCancelThrow(); observed++;
        ObserveMalformedUnsupported(); observed++;

        Require(observed == 6, "exactly six fixture groups must complete");
        Console.WriteLine($"SUMMARY fixture_groups={observed} failures=0 disclosure=\"{Disclosure}\"");
        return 0;
    }

    private static void PrintSignatures()
    {
        Console.WriteLine("CONTRACT-SIGNATURES");
        PrintMethod(typeof(CSharpSyntaxTree), "ParseText", static method =>
        {
            var p = method.GetParameters();
            return p.Length == 4 && p[0].ParameterType == typeof(SourceText)
                && p[1].ParameterType == typeof(CSharpParseOptions)
                && p[2].ParameterType == typeof(string);
        });
        PrintMethod(typeof(CSharpCompilation), "Create", static method =>
        {
            var p = method.GetParameters();
            return p.Length == 4 && p[0].ParameterType == typeof(string);
        });
        PrintMethod(typeof(Microsoft.CodeAnalysis.CSharp.CSharpExtensions), "GetDeclaredSymbol", static method =>
        {
            var p = method.GetParameters();
            return p.Length == 3 && p[0].ParameterType == typeof(SemanticModel)
                && p[1].ParameterType == typeof(BaseMethodDeclarationSyntax);
        });
        PrintMethod(typeof(Microsoft.CodeAnalysis.CSharp.CSharpExtensions), "GetSymbolInfo", static method =>
        {
            var p = method.GetParameters();
            return p.Length == 3 && p[0].ParameterType == typeof(SemanticModel)
                && p[1].ParameterType == typeof(ExpressionSyntax);
        });
    }

    private static void PrintMethod(Type type, string name, Func<MethodInfo, bool> match)
    {
        var method = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .SingleOrDefault(candidate => candidate.Name == name && match(candidate));
        Require(method is not null, $"reflected signature not found: {type.FullName}.{name}");
        Console.WriteLine("  " + Signature(method!));
    }

    private static string Signature(MethodInfo method) =>
        $"{Friendly(method.ReturnType)} {method.DeclaringType!.FullName}.{method.Name}("
        + string.Join(", ", method.GetParameters().Select(parameter =>
            $"{Friendly(parameter.ParameterType)} {parameter.Name}")) + ")";

    private static string Friendly(Type type)
    {
        if (!type.IsGenericType) return type.FullName ?? type.Name;
        var name = (type.GetGenericTypeDefinition().FullName ?? type.Name).Split('`')[0];
        return name + "<" + string.Join(",", type.GetGenericArguments().Select(Friendly)) + ">";
    }

    private static void ObserveNegativeFirst()
    {
        Console.WriteLine("NEGATIVE-FIRST");

        var repeated = Fixture.Create("negative-repeat.cs", RepeatedRecursiveSource);
        var calls = repeated.Calls("Run");
        var naiveTypeCount = calls
            .Select(call => (repeated.Model.GetSymbolInfo(call).Symbol as IMethodSymbol)?.ContainingType.Name)
            .Where(static target => target is not null)
            .Distinct(StringComparer.Ordinal)
            .Count();
        Require(naiveTypeCount < calls.Length, "naive type dedup unexpectedly preserved call occurrences");
        Console.WriteLine($"  NEG-01 type-dedup expected=3 actual={naiveTypeCount} oracle=FAIL-AS-EXPECTED");

        var naiveOrder = string.Join(",", new[] { "9:1", "10:1" }.OrderBy(static value => value, StringComparer.Ordinal));
        Require(naiveOrder != "9:1,10:1", "lexical string sorting unexpectedly matched numeric source order");
        Console.WriteLine($"  NEG-02 string-location-order expected=9:1,10:1 actual={naiveOrder} oracle=FAIL-AS-EXPECTED");

        var unknown = Fixture.Create("negative-target.cs", UnknownDynamicSource);
        var dynamicCall = unknown.Calls("Run")[0];
        var dynamicInfo = unknown.Model.GetSymbolInfo(dynamicCall);
        var inventedTarget = dynamicCall.Expression.ToString();
        Require(dynamicInfo.Symbol is null && inventedTarget.Length > 0,
            "dynamic fixture did not separate syntax text from symbol evidence");
        Console.WriteLine($"  NEG-03 syntax-target-promotion text={inventedTarget} symbol=<null> oracle=FAIL-AS-EXPECTED");
    }

    private static void ObserveRepeatedRecursive()
    {
        const string group = "repeated-recursive-calls";
        var fixture = Fixture.Create("repeated-recursive.cs", RepeatedRecursiveSource);
        var entry = fixture.Entry("Run");
        var entrySymbol = fixture.Model.GetDeclaredSymbol(entry)
            ?? throw new InvalidOperationException("entry symbol unavailable");
        var calls = Fixture.Calls(entry);
        var targets = calls.Select(call => fixture.Model.GetSymbolInfo(call).Symbol as IMethodSymbol).ToArray();

        Require(calls.Length == 3, "repeated calls were not three source occurrences");
        Require(targets.All(static target => target is not null), "a resolved repeated/recursive target was missing");
        Require(SymbolEqualityComparer.Default.Equals(targets[0], targets[2]), "repeated target symbols differ");
        Require(SymbolEqualityComparer.Default.Equals(targets[1], entrySymbol), "recursive target did not bind to entry symbol");
        Require(calls.Select(static call => call.Span).Distinct().Count() == 3, "occurrence spans collapsed");

        Console.WriteLine($"FIXTURE {group} status=OBSERVED occurrences={calls.Length} disclosure=\"{Disclosure}\"");
        for (var i = 0; i < calls.Length; i++)
            Console.WriteLine($"  sourceOrdinal={i + 1} anchor={fixture.Anchor(calls[i])} target={targets[i]!.ToDisplayString(TargetFormat)} recursive={SymbolEqualityComparer.Default.Equals(targets[i], entrySymbol)}");
    }

    private static void ObserveOverloads()
    {
        const string source = """
            class Overloads
            {
                void Pick(int value) { }
                void Pick(string value) { }
                void Run()
                {
                    Pick(1);
                    Pick("one");
                }
            }
            """;
        const string group = "overload-symbols";
        var fixture = Fixture.Create("overloads.cs", source);
        var calls = fixture.Calls("Run");
        var targets = calls.Select(call => fixture.Model.GetSymbolInfo(call).Symbol as IMethodSymbol).ToArray();

        Require(calls.Length == 2 && targets.All(static target => target is not null), "overload binding was incomplete");
        Require(!SymbolEqualityComparer.Default.Equals(targets[0], targets[1]), "overloads collapsed to one symbol");
        Console.WriteLine($"FIXTURE {group} status=OBSERVED occurrences={calls.Length} distinctSymbols=2");
        for (var i = 0; i < calls.Length; i++)
            Console.WriteLine($"  anchor={fixture.Anchor(calls[i])} target={targets[i]!.ToDisplayString(TargetFormat)}");
    }

    private static void ObserveUnknownDynamic()
    {
        const string group = "unknown-dynamic-dispatch";
        var fixture = Fixture.Create("unknown-dynamic.cs", UnknownDynamicSource);
        var calls = fixture.Calls("Run");
        Require(calls.Length == 2, "unknown/dynamic fixture call count changed");

        Console.WriteLine($"FIXTURE {group} status=OBSERVED occurrences={calls.Length}");
        foreach (var call in calls)
        {
            var info = fixture.Model.GetSymbolInfo(call);
            Require(info.Symbol is null, "unknown/dynamic call unexpectedly resolved");
            Console.WriteLine($"  anchor={fixture.Anchor(call)} syntax={call.Expression} target=<gap> candidateReason={info.CandidateReason} candidates={info.CandidateSymbols.Length}");
        }
    }

    private static void ObserveBranchLoop()
    {
        const string source = """
            class Flow
            {
                void A() { }
                void B() { }
                void Run(int count)
                {
                    if (count > 0) { A(); } else { B(); }
                    while (count-- > 0) { A(); }
                }
            }
            """;
        const string group = "branch-loop-conditions";
        var fixture = Fixture.Create("branch-loop.cs", source);
        var entry = fixture.Entry("Run");
        var conditionNodes = entry.DescendantNodes().Where(static node =>
            node is IfStatementSyntax or WhileStatementSyntax).ToArray();
        Require(conditionNodes.Length == 2, "branch/loop syntax facts were not both observed");

        Console.WriteLine($"FIXTURE {group} status=OBSERVED facts={conditionNodes.Length}");
        foreach (var node in conditionNodes)
        {
            var condition = node switch
            {
                IfStatementSyntax item => item.Condition,
                WhileStatementSyntax item => item.Condition,
                _ => throw new InvalidOperationException("unexpected condition node"),
            };
            Console.WriteLine($"  kind={node.Kind()} anchor={fixture.Anchor(node)} condition=\"{condition}\" evidence=syntax");
        }
    }

    private static void ObserveAwaitCancelThrow()
    {
        const string source = """
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            class AsyncFlow
            {
                Task Work() => Task.CompletedTask;
                async Task Run(CancellationToken token)
                {
                    await Work();
                    token.ThrowIfCancellationRequested();
                    throw new InvalidOperationException();
                }
            }
            """;
        const string group = "await-cancel-throw-paths";
        var fixture = Fixture.Create("await-cancel-throw.cs", source);
        var entry = fixture.Entry("Run");
        var awaitNode = entry.DescendantNodes().OfType<AwaitExpressionSyntax>().Single();
        var throwNode = entry.DescendantNodes().OfType<ThrowStatementSyntax>().Single();
        var cancellationCall = Fixture.Calls(entry).Single(call => call.Expression.ToString().Contains("ThrowIfCancellationRequested", StringComparison.Ordinal));
        var cancellationTarget = fixture.Model.GetSymbolInfo(cancellationCall).Symbol as IMethodSymbol;
        Require(cancellationTarget?.ContainingType.ToDisplayString() == "System.Threading.CancellationToken",
            "cancellation call did not bind to CancellationToken");

        Console.WriteLine($"FIXTURE {group} status=OBSERVED facts=3");
        Console.WriteLine($"  kind=await anchor={fixture.Anchor(awaitNode)} relation=static-continuation runtimeScheduler=<unknown>");
        Console.WriteLine($"  kind=cancel-call anchor={fixture.Anchor(cancellationCall)} target={cancellationTarget!.ToDisplayString(TargetFormat)} evidence=resolved-symbol");
        Console.WriteLine($"  kind=throw anchor={fixture.Anchor(throwNode)} exception={throwNode.Expression} propagation=<unknown>");
    }

    private static void ObserveMalformedUnsupported()
    {
        const string source = """
            class Broken
            {
                void Run()
                {
                    if (true) { goto done; }
                    Missing(;
                done:
                    return;
                }
            }
            """;
        const string group = "malformed-unsupported-syntax";
        var fixture = Fixture.Create("malformed-unsupported.cs", source);
        var errors = fixture.Tree.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        var gotos = fixture.Root.DescendantNodes().OfType<GotoStatementSyntax>().ToArray();
        Require(errors.Length > 0, "malformed fixture produced no parse error");
        Require(gotos.Length == 1, "unsupported goto syntax was not retained");

        Console.WriteLine($"FIXTURE {group} status=OBSERVED parseErrors={errors.Length} unsupportedGaps={gotos.Length}");
        foreach (var error in errors)
            Console.WriteLine($"  gap=malformed-syntax id={error.Id} anchor={fixture.Anchor(error.Location.SourceSpan)}");
        Console.WriteLine($"  gap=unsupported-control kind={gotos[0].Kind()} anchor={fixture.Anchor(gotos[0])}");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class Fixture
    {
        private Fixture(SyntaxTree tree, CSharpCompilation compilation)
        {
            Tree = tree;
            Compilation = compilation;
            Root = tree.GetCompilationUnitRoot();
            Model = compilation.GetSemanticModel(tree, ignoreAccessibility: false);
        }

        internal SyntaxTree Tree { get; }
        internal CSharpCompilation Compilation { get; }
        internal CompilationUnitSyntax Root { get; }
        internal SemanticModel Model { get; }

        internal static Fixture Create(string path, string source)
        {
            var tree = CSharpSyntaxTree.ParseText(
                SourceText.From(source, Encoding.UTF8),
                new CSharpParseOptions(LanguageVersion.Preview),
                path,
                cancellationToken: default);
            var compilation = CSharpCompilation.Create(
                "SyntheticBehaviorFixture",
                [tree],
                PlatformReferences.Value,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            return new Fixture(tree, compilation);
        }

        internal MethodDeclarationSyntax Entry(string name) => Root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single(method => method.Identifier.ValueText == name);

        internal ImmutableArray<InvocationExpressionSyntax> Calls(string entryName) => Calls(Entry(entryName));

        internal static ImmutableArray<InvocationExpressionSyntax> Calls(MethodDeclarationSyntax entry) =>
            [.. entry.DescendantNodes(static node =>
                    node is not AnonymousFunctionExpressionSyntax and not LocalFunctionStatementSyntax)
                .OfType<InvocationExpressionSyntax>()
                .OrderBy(static invocation => invocation.SpanStart)];

        internal string Anchor(SyntaxNode node) => Anchor(node.Span);

        internal string Anchor(TextSpan span)
        {
            var start = Tree.GetLineSpan(span).StartLinePosition;
            return $"{Tree.FilePath}:{start.Line + 1}:{start.Character + 1}@utf16[{span.Start}..{span.End})";
        }
    }
}
