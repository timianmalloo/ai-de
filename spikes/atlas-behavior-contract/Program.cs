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
        ObserveStructuralPages();
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

    private static readonly (string Id, string Method, string Source)[] StructuralSources =
    [
        ("B", "Branch", "class B {\n  void Ping() { }\n  void Branch(bool flag) {\n    Ping();\n    if (flag) { Ping(); } else { Ping(); }\n  }\n}\n"),
        ("L", "Loop", "class L {\n  void Tick() { }\n  void Loop(bool more) {\n    while (more) { Tick(); }\n  }\n}\n"),
        ("T", "WorkAsync", "using System.Threading.Tasks;\nclass T {\n  Task SendAsync() => Task.CompletedTask;\n  void Cleanup() { }\n  async Task WorkAsync() {\n    try { await SendAsync(); }\n    finally { Cleanup(); }\n  }\n}\n"),
        ("G", "Gaps", "class G {\n  void Ping() { }\n  void Gaps(object gate) {\n    void Local() { Ping(); }\n    lock (gate) { }\n  }\n}\n")
    ];

    private static string ChildPath(SyntaxNode node, SyntaxNode body)
    {
        var path = new Stack<int>();
        while (node != body)
        {
            var parent = node.Parent ?? throw new InvalidOperationException("node is outside selected body");
            path.Push(parent.ChildNodes().TakeWhile(child => child != node).Count());
            node = parent;
        }
        return string.Join('.', path);
    }

    private enum SubjectFault { None, DropBranch, DropCrossWindow, PageIdentity, AlwaysRefuse, OmitRegion, IgnoreAuxCap, TraverseNested, RuntimeEdge }

    private sealed record GraphNode(string Id, string Kind, string Role, int Start, int Length,
        string Path, int? Ordinal, string? Owner, string? ParentRegion,
        string? Condition, int? ConditionStart, int? ConditionLength, string? Target,
        string Confidence, string? Reason);
    private sealed record Relation(string Id, string From, string To, string Kind, int Arm,
        string Predicate, int Start, int Length, string Confidence);
    private sealed record SourceGraph(string Observation, GraphNode[] Primary, GraphNode[] Auxiliary, Relation[] Relations);
    private sealed record Boundary(Relation Relation, string MissingId, string Direction,
        int? MissingPrimaryOrdinal, int? MissingOwnerOrdinal, string Reason);
    private sealed record PageContent(GraphNode[] Primary, GraphNode[] Auxiliary, Relation[] Relations, Boundary[] Stubs);
    private sealed record PageResult(string Outcome, string? FirstLimit, PageContent? Published);
    private sealed class OracleFailure(string message) : Exception(message);

    private static string PrimaryIdentity(string observation, SyntaxNode node, string role, SyntaxNode body) =>
        $"{observation}|{node.Kind()}|{role}|{node.Span.Start}:{node.Span.Length}|{ChildPath(node, body)}";
    private static string RegionIdentity(string owner, string role) => $"{owner}|region:{role}";
    private static string RelationIdentity(string from, string to, string kind, int arm) => $"{from}>{to}|{kind}|{arm}";

    // This is the bounded experimental subject. It never reads the oracle tables below.
    private sealed class StructuralBuilder
    {
        private readonly Fixture fixture;
        private readonly MethodDeclarationSyntax method;
        private readonly string observation;
        private readonly SubjectFault fault;
        private readonly List<GraphNode> primary = [];
        private readonly List<GraphNode> auxiliary = [];
        private readonly List<Relation> relations = [];
        private int visited;

        internal StructuralBuilder((string Id, string Method, string Source) literal, SubjectFault fault)
        {
            fixture = Fixture.Create(literal.Id + ".cs", literal.Source);
            method = fixture.Entry(literal.Method);
            observation = literal.Id;
            this.fault = fault;
        }

        internal SourceGraph Build()
        {
            var body = method.Body ?? throw new InvalidOperationException("only literal method bodies admitted");
            var methodOwner = $"{observation}|method:{method.Span.Start}:{method.Span.Length}";
            var root = AddRegion(methodOwner, "MethodBody", null, body.Span);
            var entry = AddRegion(methodOwner, "Entry", null, new TextSpan(body.SpanStart, 1));
            var exit = AddRegion(methodOwner, "Exit", null, new TextSpan(body.Span.End - 1, 1));
            Edge(root, entry, "Contains"); Edge(root, exit, "Contains");
            Statements(body, root, 0);
            var ordered = primary.OrderBy(n => n.Start).ThenByDescending(n => n.Length)
                .ThenBy(n => n.Role, StringComparer.Ordinal).ThenBy(n => n.Path, StringComparer.Ordinal)
                .Select((n, i) => n with { Ordinal = i }).ToArray();
            var ordinals = ordered.ToDictionary(n => n.Id, n => n.Ordinal!.Value);
            var regions = auxiliary.OrderBy(n => n.Role == "Entry" ? -2 : n.Role == "MethodBody" ? -1 : n.Role == "Exit" ? int.MaxValue : ordinals[n.Owner!])
                .ThenBy(n => n.Role == "true" ? 0 : n.Role == "false" ? 1 : 0).ToArray();
            if (fault == SubjectFault.DropBranch) relations.RemoveAll(e => e.Kind == "WhenTrueRegion");
            if (fault == SubjectFault.RuntimeEdge)
            {
                var i = relations.FindIndex(e => e.Kind == "NextInSource");
                if (i >= 0) relations[i] = relations[i] with { Kind = "RuntimeSuccessor" };
            }
            return new SourceGraph(observation, ordered, regions,
                relations.OrderBy(e => e.Id, StringComparer.Ordinal).ToArray());
        }

        private GraphNode AddPrimary(SyntaxNode node, string role, GraphNode region,
            ExpressionSyntax? condition = null, string? reason = null)
        {
            var target = node is InvocationExpressionSyntax call
                ? (fixture.Model.GetSymbolInfo(call).Symbol as IMethodSymbol)?.ToDisplayString(TargetFormat) : null;
            var result = new GraphNode(PrimaryIdentity(observation, node, role, method.Body!),
                node.Kind().ToString(), role, node.SpanStart, node.Span.Length, ChildPath(node, method.Body!),
                null, null, region.Id, condition?.ToString(), condition?.SpanStart, condition?.Span.Length,
                target, reason is null ? "extracted" : "unknown", reason);
            primary.Add(result);
            return result;
        }

        private GraphNode AddRegion(string owner, string role, string? parent, TextSpan span)
        {
            var result = new GraphNode(RegionIdentity(owner, role), "SyntheticRegion", role,
                span.Start, span.Length, "", null, owner, parent, null, null, null, null, "extracted", null);
            auxiliary.Add(result);
            return result;
        }

        private void Edge(GraphNode from, GraphNode to, string kind, int arm = 0, ExpressionSyntax? condition = null)
        {
            var anchor = kind == "Contains" ? to : from;
            relations.Add(new Relation(RelationIdentity(from.Id, to.Id, kind, arm), from.Id, to.Id,
                kind, arm, condition?.ToString() ?? kind, condition?.SpanStart ?? anchor.Start,
                condition?.Span.Length ?? anchor.Length, "extracted"));
        }

        private void Statements(BlockSyntax block, GraphNode region, int depth)
        {
            if (depth > 64) throw new InvalidOperationException("experiment traversal depth exceeded");
            GraphNode? previous = null;
            foreach (var statement in block.Statements)
            {
                if (++visited > 10_000) throw new InvalidOperationException("experiment traversal charge exceeded");
                GraphNode current;
                switch (statement)
                {
                    case ExpressionStatementSyntax expression:
                        current = Expression(expression.Expression, region);
                        break;
                    case IfStatementSyntax branch when branch.Statement is BlockSyntax yes && branch.Else?.Statement is BlockSyntax no:
                        current = AddPrimary(branch, "control", region, branch.Condition);
                        var trueRegion = AddRegion(current.Id, "true", region.Id, branch.Span);
                        var falseRegion = AddRegion(current.Id, "false", region.Id, branch.Span);
                        Edge(current, trueRegion, "WhenTrueRegion", 0, branch.Condition);
                        Edge(current, falseRegion, "WhenFalseRegion", 1, branch.Condition);
                        Statements(yes, trueRegion, depth + 1); Statements(no, falseRegion, depth + 1);
                        break;
                    case WhileStatementSyntax loop when loop.Statement is BlockSyntax loopBlock:
                        current = AddPrimary(loop, "control", region, loop.Condition);
                        var loopRegion = AddRegion(current.Id, "body", region.Id, loop.Span);
                        Edge(current, loopRegion, "LoopBodyRegion");
                        Edge(current, loopRegion, "LoopConditionSource", 0, loop.Condition);
                        Statements(loopBlock, loopRegion, depth + 1);
                        break;
                    case TryStatementSyntax attempt when attempt.Catches.Count == 0 && attempt.Finally is not null:
                        current = AddPrimary(attempt, "control", region);
                        var tryRegion = AddRegion(current.Id, "try", region.Id, attempt.Span);
                        Edge(current, tryRegion, "Contains");
                        Statements(attempt.Block, tryRegion, depth + 1);
                        var finallyNode = AddPrimary(attempt.Finally, "control", tryRegion);
                        Edge(tryRegion, finallyNode, "Contains");
                        var finallyRegion = AddRegion(finallyNode.Id, "finally", tryRegion.Id, attempt.Finally.Span);
                        Edge(finallyNode, finallyRegion, "Contains");
                        Edge(tryRegion, finallyRegion, "FinallyDeclaration");
                        Statements(attempt.Finally.Block, finallyRegion, depth + 1);
                        break;
                    case LocalFunctionStatementSyntax local:
                        current = AddPrimary(local, "gap", region, reason: "nested-body-not-expanded");
                        if (fault == SubjectFault.TraverseNested && local.Body is not null) Statements(local.Body, region, depth + 1);
                        break;
                    case LockStatementSyntax locked:
                        current = AddPrimary(locked, "gap", region, reason: "unsupported-lock");
                        break;
                    default:
                        current = AddPrimary(statement, "gap", region, reason: "unsupported-experiment-syntax");
                        break;
                }
                Edge(region, current, "Contains");
                if (previous is not null) Edge(previous, current, "NextInSource");
                previous = current;
            }
        }

        private GraphNode Expression(ExpressionSyntax expression, GraphNode region)
        {
            if (expression is InvocationExpressionSyntax call) return AddPrimary(call, "call", region);
            if (expression is AwaitExpressionSyntax awaited && awaited.Expression is InvocationExpressionSyntax operand)
            {
                var outer = AddPrimary(awaited, "await", region);
                var inner = AddPrimary(operand, "call", region);
                Edge(outer, inner, "AwaitOperand");
                return outer;
            }
            return AddPrimary(expression, "gap", region, reason: "unsupported-experiment-expression");
        }
    }

    private static PageResult ProjectPage(SourceGraph graph, string observation, int offset, int limit,
        int auxiliaryCap = 64, SubjectFault fault = SubjectFault.None)
    {
        if (observation != graph.Observation) return new("invalid-observation", null, null);
        if (limit is < 1 or > 128) return new("invalid-limit", null, null);
        if (offset < 0 || offset >= graph.Primary.Length) return new("invalid-offset", null, null);
        if (fault == SubjectFault.AlwaysRefuse) return new("window-unrepresentable", "auxiliary-closure", null);
        var allNodes = graph.Primary.Concat(graph.Auxiliary).ToDictionary(n => n.Id);
        var primary = graph.Primary.Skip(offset).Take(limit).ToArray();
        var included = new HashSet<string>(primary.Select(n => n.Id), StringComparer.Ordinal);
        foreach (var common in graph.Auxiliary.Where(n => n.Role is "Entry" or "Exit" or "MethodBody")) included.Add(common.Id);
        foreach (var node in primary)
        {
            var parent = node.ParentRegion;
            while (parent is not null)
            {
                included.Add(parent);
                parent = allNodes[parent].ParentRegion;
            }
        }
        if (fault == SubjectFault.OmitRegion) included.RemoveWhere(id => allNodes[id].Role == "true");
        var auxiliary = graph.Auxiliary.Where(n => included.Contains(n.Id)).ToArray();
        if (auxiliary.Length > auxiliaryCap && fault != SubjectFault.IgnoreAuxCap)
            return new("window-unrepresentable", "auxiliary-closure", null);
        var edges = new List<Relation>(); var stubs = new List<Boundary>();
        foreach (var edge in graph.Relations)
        {
            if (fault == SubjectFault.DropCrossWindow && limit <= 2 && edge.Kind == "WhenTrueRegion") continue;
            var hasFrom = included.Contains(edge.From); var hasTo = included.Contains(edge.To);
            if (hasFrom && hasTo) edges.Add(edge);
            else if (hasFrom || hasTo)
            {
                var missing = allNodes[hasFrom ? edge.To : edge.From];
                var ownerOrdinal = missing.Owner is not null && allNodes.TryGetValue(missing.Owner, out var owner) ? owner.Ordinal : null;
                stubs.Add(new Boundary(edge, missing.Id, hasFrom ? "outgoing" : "incoming",
                    missing.Ordinal, ownerOrdinal, "outside-window"));
            }
        }
        if (edges.Count > 64) return new("window-unrepresentable", "edge-cap", null);
        if (stubs.Count > 64) return new("window-unrepresentable", "stub-cap", null);
        var content = new PageContent(primary, auxiliary, edges.ToArray(), stubs.ToArray());
        if (fault == SubjectFault.PageIdentity)
        {
            string Rewrite(string id) => id + $"|page:{offset}:{limit}";
            GraphNode RewriteNode(GraphNode n) => n with { Id = Rewrite(n.Id), ParentRegion = n.ParentRegion is null ? null : Rewrite(n.ParentRegion), Owner = n.Owner is null ? null : Rewrite(n.Owner) };
            Relation RewriteEdge(Relation e) => e with { Id = Rewrite(e.Id), From = Rewrite(e.From), To = Rewrite(e.To) };
            content = new(primary.Select(RewriteNode).ToArray(), auxiliary.Select(RewriteNode).ToArray(), edges.Select(RewriteEdge).ToArray(),
                stubs.Select(b => b with { Relation = RewriteEdge(b.Relation), MissingId = Rewrite(b.MissingId) }).ToArray());
        }
        if (System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(content).Length > 65_536)
            return new("window-unrepresentable", "encoded-byte-cap", null);
        return new("published", null, content);
    }

    // Expected inputs are transcribed from independent ledger 205d5da4 / Owner D1-D5.
    // Spans/ChildNodes paths were observed from the literals before this subject existed.
    // No expected graph, closure, relation, or identity is read from the subject.
    private sealed record ExpectedPrimary(string Label, string Kind, string Role, int Start, int Length,
        string Path, string Region, string? Target = null, string? Condition = null,
        int? ConditionStart = null, int? ConditionLength = null, string? Reason = null);
    private sealed record ExpectedRegion(string Label, string Role, string Owner, string? Parent, int Start, int Length);
    private sealed record ExpectedRelation(string From, string To, string Kind, int Start, int Length,
        int Arm = 0, string? Predicate = null);
    private sealed record ExpectedCase(SourceGraph Graph, Dictionary<string, string> Labels,
        Dictionary<string, string[]> Closure);

    private static ExpectedCase IndependentExpected(string id)
    {
        ExpectedPrimary[] rows; ExpectedRegion[] regions; ExpectedRelation[] edges;
        string methodOwner;
        Dictionary<string, string[]> closure;
        switch (id)
        {
            case "B":
                methodOwner = "B|method:30:83";
                rows = [
                    new("B1", "InvocationExpression", "call", 59, 6, "0.0", "MethodBody", "B.Ping()"),
                    new("B2", "IfStatement", "control", 71, 38, "1", "MethodBody", Condition: "flag", ConditionStart: 75, ConditionLength: 4),
                    new("B3", "InvocationExpression", "call", 83, 6, "1.1.0.0", "B.true", "B.Ping()"),
                    new("B4", "InvocationExpression", "call", 100, 6, "1.2.0.0.0", "B.false", "B.Ping()")];
                regions = [new("Entry", "Entry", "method", null, 53, 1), new("MethodBody", "MethodBody", "method", null, 53, 60),
                    new("B.true", "true", "B2", "MethodBody", 71, 38), new("B.false", "false", "B2", "MethodBody", 71, 38), new("Exit", "Exit", "method", null, 112, 1)];
                edges = [new("MethodBody", "Entry", "Contains", 53, 1), new("MethodBody", "Exit", "Contains", 112, 1),
                    new("MethodBody", "B1", "Contains", 59, 6), new("MethodBody", "B2", "Contains", 71, 38),
                    new("B1", "B2", "NextInSource", 59, 6), new("B2", "B.true", "WhenTrueRegion", 75, 4, 0, "flag"),
                    new("B2", "B.false", "WhenFalseRegion", 75, 4, 1, "flag"), new("B.true", "B3", "Contains", 83, 6), new("B.false", "B4", "Contains", 100, 6)];
                closure = new() { ["B1"] = ["Entry", "MethodBody", "Exit"], ["B2"] = ["Entry", "MethodBody", "Exit"],
                    ["B3"] = ["Entry", "MethodBody", "B.true", "Exit"], ["B4"] = ["Entry", "MethodBody", "B.false", "Exit"] };
                break;
            case "L":
                methodOwner = "L|method:30:55";
                rows = [new("L1", "WhileStatement", "control", 57, 24, "0", "MethodBody", Condition: "more", ConditionStart: 64, ConditionLength: 4),
                    new("L2", "InvocationExpression", "call", 72, 6, "0.1.0.0", "L.body", "L.Tick()")];
                regions = [new("Entry", "Entry", "method", null, 51, 1), new("MethodBody", "MethodBody", "method", null, 51, 34),
                    new("L.body", "body", "L1", "MethodBody", 57, 24), new("Exit", "Exit", "method", null, 84, 1)];
                edges = [new("MethodBody", "Entry", "Contains", 51, 1), new("MethodBody", "Exit", "Contains", 84, 1),
                    new("MethodBody", "L1", "Contains", 57, 24), new("L1", "L.body", "LoopBodyRegion", 57, 24),
                    new("L1", "L.body", "LoopConditionSource", 64, 4, 0, "more"), new("L.body", "L2", "Contains", 72, 6)];
                closure = new() { ["L1"] = ["Entry", "MethodBody", "Exit"], ["L2"] = ["Entry", "MethodBody", "L.body", "Exit"] };
                break;
            case "T":
                methodOwner = "T|method:105:86";
                rows = [new("T1", "TryStatement", "control", 134, 53, "0", "MethodBody"),
                    new("T2", "AwaitExpression", "await", 140, 17, "0.0.0.0", "T.try"),
                    new("T3", "InvocationExpression", "call", 146, 11, "0.0.0.0.0", "T.try", "T.SendAsync()"),
                    new("T4", "FinallyClause", "control", 165, 22, "0.1", "T.try"),
                    new("T5", "InvocationExpression", "call", 175, 9, "0.1.0.0.0", "T.finally", "T.Cleanup()")];
                regions = [new("Entry", "Entry", "method", null, 128, 1), new("MethodBody", "MethodBody", "method", null, 128, 63),
                    new("T.try", "try", "T1", "MethodBody", 134, 53), new("T.finally", "finally", "T4", "T.try", 165, 22), new("Exit", "Exit", "method", null, 190, 1)];
                edges = [new("MethodBody", "Entry", "Contains", 128, 1), new("MethodBody", "Exit", "Contains", 190, 1),
                    new("MethodBody", "T1", "Contains", 134, 53), new("T1", "T.try", "Contains", 134, 53),
                    new("T.try", "T2", "Contains", 140, 17), new("T2", "T3", "AwaitOperand", 140, 17),
                    new("T.try", "T4", "Contains", 165, 22), new("T4", "T.finally", "Contains", 165, 22),
                    new("T.finally", "T5", "Contains", 175, 9), new("T.try", "T.finally", "FinallyDeclaration", 134, 53)];
                closure = new() { ["T1"] = ["Entry", "MethodBody", "Exit"], ["T2"] = ["Entry", "MethodBody", "T.try", "Exit"],
                    ["T3"] = ["Entry", "MethodBody", "T.try", "Exit"], ["T4"] = ["Entry", "MethodBody", "T.try", "Exit"],
                    ["T5"] = ["Entry", "MethodBody", "T.try", "T.finally", "Exit"] };
                break;
            case "G":
                methodOwner = "G|method:30:77";
                rows = [new("G1", "LocalFunctionStatement", "gap", 59, 24, "0", "MethodBody", Reason: "nested-body-not-expanded"),
                    new("G2", "LockStatement", "gap", 88, 15, "1", "MethodBody", Reason: "unsupported-lock")];
                regions = [new("Entry", "Entry", "method", null, 53, 1), new("MethodBody", "MethodBody", "method", null, 53, 54), new("Exit", "Exit", "method", null, 106, 1)];
                edges = [new("MethodBody", "Entry", "Contains", 53, 1), new("MethodBody", "Exit", "Contains", 106, 1),
                    new("MethodBody", "G1", "Contains", 59, 24), new("MethodBody", "G2", "Contains", 88, 15), new("G1", "G2", "NextInSource", 59, 24)];
                closure = new() { ["G1"] = ["Entry", "MethodBody", "Exit"], ["G2"] = ["Entry", "MethodBody", "Exit"] };
                break;
            default: throw new InvalidOperationException("no independent literal oracle");
        }
        var ids = rows.ToDictionary(n => n.Label, n => $"{id}|{n.Kind}|{n.Role}|{n.Start}:{n.Length}|{n.Path}");
        foreach (var r in regions) ids.Add(r.Label, $"{(r.Owner == "method" ? methodOwner : ids[r.Owner])}|region:{r.Role}");
        var primary = rows.Select((n, i) => new GraphNode(ids[n.Label], n.Kind, n.Role, n.Start, n.Length, n.Path,
            i, null, ids[n.Region], n.Condition, n.ConditionStart, n.ConditionLength, n.Target,
            n.Reason is null ? "extracted" : "unknown", n.Reason)).ToArray();
        var auxiliary = regions.Select(r => new GraphNode(ids[r.Label], "SyntheticRegion", r.Role, r.Start, r.Length, "", null,
            r.Owner == "method" ? methodOwner : ids[r.Owner], r.Parent is null ? null : ids[r.Parent], null, null, null, null, "extracted", null)).ToArray();
        var relations = edges.Select(e => new Relation($"{ids[e.From]}>{ids[e.To]}|{e.Kind}|{e.Arm}", ids[e.From], ids[e.To],
            e.Kind, e.Arm, e.Predicate ?? e.Kind, e.Start, e.Length, "extracted")).OrderBy(e => e.Id, StringComparer.Ordinal).ToArray();
        return new(new(id, primary, auxiliary, relations), ids, closure);
    }

    private static PageContent ExpectedPage(ExpectedCase expected, int offset, int limit)
    {
        var graph = expected.Graph;
        var rows = graph.Primary.Where(n => n.Ordinal >= offset && n.Ordinal < offset + limit).ToArray();
        var selectedLabels = expected.Labels.Where(kv => rows.Any(n => n.Id == kv.Value)).Select(kv => kv.Key);
        var auxiliaryIds = selectedLabels.SelectMany(label => expected.Closure[label]).Select(label => expected.Labels[label]).ToHashSet(StringComparer.Ordinal);
        var auxiliary = graph.Auxiliary.Where(n => auxiliaryIds.Contains(n.Id)).ToArray();
        var present = rows.Concat(auxiliary).Select(n => n.Id).ToHashSet(StringComparer.Ordinal);
        var all = graph.Primary.Concat(graph.Auxiliary).ToDictionary(n => n.Id);
        var inside = graph.Relations.Where(e => present.Contains(e.From) && present.Contains(e.To)).ToArray();
        var boundaries = graph.Relations.Where(e => present.Contains(e.From) != present.Contains(e.To)).Select(e =>
        {
            var outgoing = present.Contains(e.From);
            var omitted = all[outgoing ? e.To : e.From];
            int? ownerOrdinal = omitted.Owner is null ? null : graph.Primary.SingleOrDefault(n => n.Id == omitted.Owner)?.Ordinal;
            return new Boundary(e, omitted.Id, outgoing ? "outgoing" : "incoming", omitted.Ordinal, ownerOrdinal, "outside-window");
        }).ToArray();
        return new(rows, auxiliary, inside, boundaries);
    }

    private static void Check(bool condition, string oracle)
    {
        if (!condition) throw new OracleFailure(oracle);
    }
    private static void Same<T>(IEnumerable<T> actual, IEnumerable<T> expected, string oracle) => Check(actual.SequenceEqual(expected), oracle);
    private static void AssertGraph(SourceGraph actual, ExpectedCase expected)
    {
        Same(actual.Primary, expected.Graph.Primary, $"{actual.Observation}:fixed-primary-identity-anchor-predicate");
        Same(actual.Auxiliary, expected.Graph.Auxiliary, $"{actual.Observation}:fixed-auxiliary");
        Same(actual.Relations, expected.Graph.Relations, $"{actual.Observation}:fixed-structural-relations");
    }
    private static void AssertPage(PageResult actual, PageContent expected, string context)
    {
        Check(actual.Outcome == "published" && actual.Published is not null, context + ":publication");
        var content = actual.Published!;
        Same(content.Primary, expected.Primary, context + ":stable-primary-identity");
        Same(content.Auxiliary, expected.Auxiliary, context + ":mandatory-closure");
        Same(content.Relations, expected.Relations, context + ":inside-relations");
        Same(content.Stubs, expected.Stubs, context + ":boundary-stubs");
    }

    private static void AssertRecomposition(SourceGraph graph, ExpectedCase expected, int size, SubjectFault fault = SubjectFault.None)
    {
        var pages = Enumerable.Range(0, (graph.Primary.Length + size - 1) / size)
            .Select(i => ProjectPage(graph, graph.Observation, i * size, size, fault: fault)).ToArray();
        Check(pages.All(p => p.Published is not null), "recomposition:publication");
        var primary = pages.SelectMany(p => p.Published!.Primary).OrderBy(n => n.Ordinal).ToArray();
        Same(primary, expected.Graph.Primary, "recomposition:primary-identities");
        var auxiliaryGroups = pages.SelectMany(p => p.Published!.Auxiliary).GroupBy(n => n.Id).ToArray();
        Check(auxiliaryGroups.All(g => g.All(n => n == g.First())), "recomposition:conflicting-auxiliary-evidence");
        Same(auxiliaryGroups.Select(g => g.First()).OrderBy(n => n.Id, StringComparer.Ordinal),
            expected.Graph.Auxiliary.OrderBy(n => n.Id, StringComparer.Ordinal), "recomposition:auxiliary-identities");
        var edgeGroups = pages.SelectMany(p => p.Published!.Relations.Concat(p.Published.Stubs.Select(b => b.Relation))).GroupBy(e => e.Id).ToArray();
        Check(edgeGroups.All(g => g.All(e => e == g.First())), "recomposition:conflicting-relation-evidence");
        var reconstructed = edgeGroups.Select(g => g.First()).OrderBy(e => e.Id, StringComparer.Ordinal).ToArray();
        Same(reconstructed, expected.Graph.Relations, "recomposition:relations");
        var nodeIds = primary.Concat(auxiliaryGroups.Select(g => g.First())).Select(n => n.Id).ToHashSet(StringComparer.Ordinal);
        Check(reconstructed.All(e => nodeIds.Contains(e.From) && nodeIds.Contains(e.To)), "recomposition:unresolved-window-stub");
    }

    private static void AssertCapControls(SourceGraph graph, ExpectedCase expected, SubjectFault fault = SubjectFault.None)
    {
        var refused = ProjectPage(graph, "B", 2, 1, 3, fault);
        Check(refused.Outcome == "window-unrepresentable" && refused.FirstLimit == "auxiliary-closure" && refused.Published is null,
            "D5:B3-cap3-refuses-auxiliary-first-no-publication");
        AssertPage(ProjectPage(graph, "B", 2, 1, 4, fault), ExpectedPage(expected, 2, 1), "D5:B3-cap4-publishes-four");
        AssertPage(ProjectPage(graph, "B", 0, 1, 3, fault), ExpectedPage(expected, 0, 1), "D5:B1-cap3-publishes-three");
    }

    private static void ObserveStructuralPages()
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var cases = StructuralSources.ToDictionary(x => x.Id, x => (Literal: x, Expected: IndependentExpected(x.Id)));
        var faultsObserved = 0;
        var observedFaultClasses = new HashSet<string>(StringComparer.Ordinal);
        void Reject(string name, Action oracle)
        {
            try { oracle(); }
            catch (OracleFailure failure)
            {
                Console.WriteLine($"SUBJECT-FAULT {name} oracle=FAIL-AS-EXPECTED reason={failure.Message}");
                faultsObserved++;
                observedFaultClasses.Add(name.Split('/')[0]);
                return;
            }
            throw new InvalidOperationException($"subject fault escaped its required oracle: {name}");
        }
        Console.WriteLine("STRUCTURAL-PAGE-NEGATIVE-FIRST ledger=205d5da4 owner=4a81eb11 structural_source_only=true");
        var branch = cases["B"];
        var badBranch = new StructuralBuilder(branch.Literal, SubjectFault.DropBranch).Build();
        Reject("drop-branch/fixed-set", () => AssertGraph(badBranch, branch.Expected));
        Reject("drop-branch/recomposition", () => AssertRecomposition(badBranch, branch.Expected, 1));
        var b = new StructuralBuilder(branch.Literal, SubjectFault.None).Build();
        Reject("drop-cross-window/per-page", () => AssertPage(ProjectPage(b, "B", 1, 1, fault: SubjectFault.DropCrossWindow), ExpectedPage(branch.Expected, 1, 1), "B1-page"));
        Reject("drop-cross-window/recomposition", () => AssertRecomposition(b, branch.Expected, 2, SubjectFault.DropCrossWindow));
        Reject("page-dependent-identity", () => AssertRecomposition(b, branch.Expected, 1, SubjectFault.PageIdentity));
        Reject("always-refuse", () => AssertCapControls(b, branch.Expected, SubjectFault.AlwaysRefuse));
        Reject("omit-owning-region", () => AssertCapControls(b, branch.Expected, SubjectFault.OmitRegion));
        Reject("ignore-auxiliary-cap", () => AssertCapControls(b, branch.Expected, SubjectFault.IgnoreAuxCap));
        Reject("traverse-opaque-local-body", () => AssertGraph(new StructuralBuilder(cases["G"].Literal, SubjectFault.TraverseNested).Build(), cases["G"].Expected));
        Reject("invent-runtime-edge", () => AssertGraph(new StructuralBuilder(branch.Literal, SubjectFault.RuntimeEdge).Build(), branch.Expected));
        Check(faultsObserved == 10, "exactly-ten-fault-oracle-rejections");

        var totalPrimary = 0; var totalAuxiliary = 0; var totalEdges = 0; var pageCount = 0; var recompositions = 0;
        foreach (var literal in StructuralSources)
        {
            var expected = cases[literal.Id].Expected;
            var graph = new StructuralBuilder(literal, SubjectFault.None).Build();
            AssertGraph(graph, expected);
            totalPrimary += graph.Primary.Length; totalAuxiliary += graph.Auxiliary.Length; totalEdges += graph.Relations.Length;
            var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(literal.Source))).ToLowerInvariant();
            var expectedHash = literal.Id switch
            {
                "B" => "eae4cee81bb7e417c19a62f7cedc04c2e7316b5087b65bc75f0c07094affbcfa",
                "L" => "fd35da4ab3e1c45ae74153dcffce467e7f17b9c3f5a33a496da0e8e996f17b5e",
                "T" => "9536cb46d00c9538f852f6795dd0512a1d65658340caffb11161cd68daea9bed",
                "G" => "b0ce284f93a055d586e0b5b14d69d5f432c489fbc39c28c7255a82f9fd11b066",
                _ => throw new InvalidOperationException("unexpected literal"),
            };
            Check(hash == expectedHash, "fixed-literal-source-hash");
            Console.WriteLine($"STRUCTURAL {literal.Id} primary={graph.Primary.Length} auxiliary={graph.Auxiliary.Length} nodes={graph.Primary.Length + graph.Auxiliary.Length} edges={graph.Relations.Length} source_sha256={hash}");
            foreach (var node in graph.Primary)
                Console.WriteLine($"  PRIMARY id={node.Id} ordinal={node.Ordinal} condition={node.Condition ?? "<none>"} conditionSpan={node.ConditionStart}:{node.ConditionLength} target={node.Target ?? "<none>"} confidence={node.Confidence} gap={node.Reason ?? "<none>"}");
            var labels = expected.Labels.ToDictionary(kv => kv.Value, kv => kv.Key);
            foreach (var region in graph.Auxiliary)
                Console.WriteLine($"  AUX {labels[region.Id]} id={region.Id} owner={region.Owner} parent={region.ParentRegion ?? "<root>"} anchor={region.Start}:{region.Length} confidence={region.Confidence}");
            foreach (var edge in graph.Relations)
                Console.WriteLine($"  RELATION {labels[edge.From]}>{labels[edge.To]} kind={edge.Kind} arm={edge.Arm} predicate={edge.Predicate} anchor={edge.Start}:{edge.Length} confidence={edge.Confidence}");
            foreach (var size in new[] { 1, 2, 7, 128 })
            {
                for (var offset = 0; offset < graph.Primary.Length; offset += size)
                {
                    var page = ProjectPage(graph, literal.Id, offset, size);
                    AssertPage(page, ExpectedPage(expected, offset, size), $"{literal.Id}:{offset}:{size}");
                    pageCount++;
                    var content = page.Published!;
                    Console.WriteLine($"  PAGE size={size} offset={offset} primary={content.Primary.Length} closure=[{string.Join(',', content.Auxiliary.Select(n => labels[n.Id]))}] edges={content.Relations.Length} stubs={content.Stubs.Length} encoded_content_bytes={System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(content).Length}");
                    foreach (var stub in content.Stubs)
                        Console.WriteLine($"    STUB {labels[stub.Relation.From]}>{labels[stub.Relation.To]} kind={stub.Relation.Kind} missing={labels[stub.MissingId]} direction={stub.Direction} primaryOrdinal={stub.MissingPrimaryOrdinal?.ToString() ?? "<none>"} ownerOrdinal={stub.MissingOwnerOrdinal?.ToString() ?? "<none>"} reason={stub.Reason}");
                }
                AssertRecomposition(graph, expected, size);
                recompositions++;
                Console.WriteLine($"  RECOMPOSE size={size} status=OBSERVED exact_identity_anchor_predicate_confidence_relations=true");
            }
        }
        AssertCapControls(b, branch.Expected);
        Console.WriteLine("D5 B3 cap=3 outcome=window-unrepresentable firstLimit=auxiliary-closure published=false");
        Console.WriteLine("D5 B3 cap=4 outcome=published auxiliaries=4; B1 cap=3 outcome=published auxiliaries=3");
        var invalids = new[] { ProjectPage(b, "B", 0, 0), ProjectPage(b, "B", 0, 129), ProjectPage(b, "B", -1, 1), ProjectPage(b, "B", 4, 1), ProjectPage(b, "foreign", 0, 1) };
        Same(invalids.Select(p => p.Outcome), new[] { "invalid-limit", "invalid-limit", "invalid-offset", "invalid-offset", "invalid-observation" }, "invalid-request-codes");
        Check(invalids.All(p => p.Published is null), "invalid-request-no-publication");
        Console.WriteLine("REFUSALS invalid-limit=2 invalid-offset=2 invalid-observation=1 published=0");
        Check(totalPrimary == 13 && totalAuxiliary == 17 && totalEdges == 30 && pageCount == 28 && recompositions == 16, "independent-total-ledger");
        Check(observedFaultClasses.Count == 8, "eight-observed-subject-fault-classes");
        Console.WriteLine($"STRUCTURAL-SUMMARY groups=4 primary={totalPrimary} auxiliary={totalAuxiliary} nodes={totalPrimary + totalAuxiliary} edges={totalEdges} pages={pageCount} recompositions={recompositions} fault_oracle_rejections={faultsObserved} distinct_subject_faults={observedFaultClasses.Count} elapsed_ms={watch.Elapsed.TotalMilliseconds.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}");
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
