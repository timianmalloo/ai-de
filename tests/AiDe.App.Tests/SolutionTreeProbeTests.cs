using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using AiDe.App.Workbench;
using AiDe.Core.Projections;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit.Abstractions;

namespace AiDe.App.Tests;

/// <summary>
/// US-T11 / B14: App-assembly enum, Atlas, and View-source file-read probes for D-0.
/// </summary>
public sealed class SolutionTreeProbeTests
{
    private readonly ITestOutputHelper _output;
    public SolutionTreeProbeTests(ITestOutputHelper output) => _output = output;

    private static readonly Regex EnumApis = new(
        @"\b(EnumerateDirectories|EnumerateFiles|EnumerateFileSystemEntries|EnumerateFileSystemInfos|GetDirectories|GetFiles|GetFileSystemEntries|GetFileSystemInfos)\s*\(",
        RegexOptions.Compiled);

    private static readonly Regex FileReads = new(
        @"\bFile\.(ReadAllText|ReadAllTextAsync|ReadAllBytes|ReadAllBytesAsync|Open|OpenRead)\s*\(",
        RegexOptions.Compiled);

    private static readonly Dictionary<string, int> FileReadAllow = new(StringComparer.OrdinalIgnoreCase)
    {
        [Path.Combine("Workbench", "PromptDraftStore.cs")] = 1,
        [Path.Combine("Workbench", "TerminalCustomizationStore.cs")] = 1,
        [Path.Combine("Workbench", "Sessions", "RecentSessions.cs")] = 2,
        [Path.Combine("Conductor", "ConductorEntry.cs")] = 1,
    };

    [Fact]
    public void ProbeAppEnum_AppTypesThatAreNotWorkspaceQueries_DoNotEnumerateTheFilesystem()
    {
        var root = Path.Combine(RepoRoot(), "src", "AiDe.App");
        var scanned = 0;
        var offenders = new List<string>();

        foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            scanned++;
            var hits = File.ReadLines(file)
                .Select((line, i) => (line, i))
                .Where(x => EnumApis.IsMatch(x.line) && !x.line.TrimStart().StartsWith("//", StringComparison.Ordinal))
                .ToList();
            if (hits.Count == 0)
            {
                continue;
            }

            offenders.Add($"{Path.GetRelativePath(RepoRoot(), file)}:{hits[0].i + 1}: {hits[0].line.Trim()}");
        }

        Assert.True(scanned > 50, $"only {scanned} App file(s) scanned");
        Assert.True(offenders.Count == 0,
            "AiDe.App types that are not IWorkspaceQueries implementations must not enumerate the workspace:\n  "
            + string.Join("\n  ", offenders));

        var queryImplementers = typeof(SolutionTreeSurface).Assembly.GetTypes()
            .Where(t => typeof(IWorkspaceQueries).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false })
            .Select(t => t.FullName)
            .ToList();
        Assert.Empty(queryImplementers);
    }

    [Fact]
    public void ProbeAtlas_DirectStaticD0Boundary_DoesNotDependOnAtlas()
    {
        var result = D0Boundary.Baseline.Value.Analyze();
        _output.WriteLine(result.ToString());
        Assert.Equal(26, result.Roots);
        Assert.True(result.References > 0, "D0 reference population is empty");
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("direct", "ATLAS")]
    [InlineData("alias", "ATLAS")]
    [InlineData("lambda", "ATLAS")]
    [InlineData("local-function", "ATLAS")]
    [InlineData("shared-seam", "ATLAS")]
    [InlineData("source-path", "ATLAS")]
    [InlineData("new-helper", "UNACCOUNTED")]
    [InlineData("missing-member", "ROOT")]
    [InlineData("missing-file", "ROOT")]
    [InlineData("duplicate-row", "ROOT")]
    [InlineData("unresolved", "UNRESOLVED")]
    [InlineData("conditional-atlas", "CONDITIONAL")]
    public void ProbeAtlas_AdversarialD0Mutation_IsRejected(string mutation, string expected)
    {
        var result = D0Boundary.Baseline.Value.Mutate(mutation).Analyze();
        _output.WriteLine(mutation + " " + result);
        Assert.Contains(result.Errors, error => error.Contains(expected, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("comments-strings")]
    [InlineData("unrelated-atlas")]
    [InlineData("unrelated-registration")]
    [InlineData("shared-port")]
    public void ProbeAtlas_CoexistingCodeAndDeclaredPorts_AreAllowed(string mutation)
    {
        var result = D0Boundary.Baseline.Value.Mutate(mutation).Analyze();
        _output.WriteLine(mutation + " " + result);
        Assert.Equal(26, result.Roots);
        Assert.True(result.References > 0);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("src/AiDe.App/Workbench/AiDe.Core.Decoy.cs", false, true)]
    [InlineData("src/AiDe.Core/AiDe.App.Decoy.cs", true, false)]
    [InlineData("src/AiDe.App-lookalike/Decoy.cs", false, false)]
    public void ProbeAtlas_ProjectClassification_UsesExactProjectDirectory(string path, bool core, bool app)
    {
        var root = Path.Combine(Path.GetTempPath(), "AiDe.Core-workspace-name");
        var fullPath = Path.Combine(root, path);
        Assert.Equal(core, D0Boundary.InProject(root, fullPath, "AiDe.Core"));
        Assert.Equal(app, D0Boundary.InProject(root, fullPath, "AiDe.App"));
    }

    // DIRECT STATIC boundary only. Shared ports end this check; no runtime/transitive claim.
    // Complete D0 files and exact shared-class members are selected with Roslyn, never regex bodies.
    private sealed record D0Boundary(CSharpCompilation Core, CSharpCompilation App, string Root)
    {
        internal static readonly Lazy<D0Boundary> Baseline = new(Load);
        private const string Surface = "src/AiDe.App/Workbench/SolutionTreeSurface.cs";
        private const string Projection = "src/AiDe.Core/Projections/SolutionTreeProjection.cs";
        private const string Shell = "src/AiDe.App/Workbench/WorkbenchShell.cs";
        private const string Factory = "src/AiDe.App/Workbench/SurfaceContentFactory.cs";
        private const string Queries = "src/AiDe.Core/Projections/IWorkspaceQueries.cs";
        private const string Client = "src/AiDe.Core/Ipc/WorkspaceClient.cs";
        private const string Operations = "src/AiDe.Core/Ipc/WorkspaceOperations.cs";
        private const string Service = "src/AiDe.Core/Projections/ProjectionService.cs";

        // Named shared handoffs, not whole namespaces/classes. New AiDe helper calls fail closed.
        // The source/content and graph implementations were inspected at grounding; their arbitrary
        // transitive callees are outside this DIRECT STATIC contract.
        private static readonly HashSet<string> SharedMembers = new(StringComparer.Ordinal)
        {
            "AiDe.Core.PathComparison.ForThisFileSystem", "AiDe.Core.Extraction.UnanalysedLanguages.Skip",
            "AiDe.Core.Store.WorkspaceStore.BeginRead", "AiDe.Core.Store.StoreReader.FilesToSearch",
            "AiDe.Core.Store.StoreReader.ReadNodeKind", "AiDe.Core.Store.StoreReader.AllScopeLocations", "AiDe.Core.Store.StoreReader.CurrentSourceRevision",
            "AiDe.Core.Projections.ProjectionService.CandidateWithinWorkspace", "AiDe.Core.Projections.ProjectionService.Activity",
            "AiDe.Core.Projections.ProjectionService.FrameBytes", "AiDe.Core.Projections.ProjectionService.MaxFramedGraphBytes", "AiDe.Core.Projections.ProjectionService.Wire",
            "AiDe.Core.Ipc.WorkspaceClient.QueryAsync", "AiDe.Core.Ipc.WorkspaceOperations.Handle", "AiDe.Core.Ipc.WorkspaceOperations.Refusable",
            "AiDe.Core.Ipc.DaemonEndpoint.Register", "AiDe.Core.Ipc.IpcResponse.Success",
            "AiDe.Core.Workbench.PerspectiveSet.Architecture", "AiDe.Core.Workbench.Surface.Title",
            "AiDe.App.Workbench.NodeViewKind.Source", "AiDe.App.Workbench.NodeViewKind.Read", "AiDe.App.Workbench.NodeViewKind.GraphNeighbourhood",
            "AiDe.App.Workbench.RelayCommand..ctor", "AiDe.App.Workbench.SurfaceContentFactory.Instances.One",
            "AiDe.App.Workbench.SurfaceContentFactory.SurfaceKind..ctor", "AiDe.App.Workbench.SurfaceContentFactory.SurfaceEntry.Derived..ctor",
            "AiDe.App.Workbench.IWorkbenchAnnouncer.Announce", "AiDe.App.Workbench.WorkbenchShell.Announcer", "AiDe.App.Workbench.WorkbenchShell.Architecture",
            "AiDe.App.Workbench.WorkbenchShell._queries", "AiDe.App.Workbench.WorkbenchShell._lastSelectedNodeId",
            "AiDe.App.Workbench.WorkbenchShell.SurfaceContents", "AiDe.App.Workbench.WorkbenchShell.OpenKind",
            "AiDe.App.Workbench.WorkbenchShell.OpenNodeView", "AiDe.App.Workbench.WorkbenchShell.OpenCanvas", "AiDe.App.Workbench.WorkbenchShell.CentreOnAsync",
            "AiDe.App.Workbench.WorkbenchShell.OpenCodeViewers", "AiDe.App.Workbench.WorkbenchShell.ShowNodeInCodeViewersAsync",
        };
        private static readonly HashSet<string> SharedTypes = new(StringComparer.Ordinal)
        {
            "AiDe.App.Workbench.CanvasSurface", "AiDe.App.Workbench.CodeViewerView", "AiDe.App.Workbench.DockHost",
            "AiDe.App.Workbench.SurfaceContentFactory.SurfaceEntry", "AiDe.Core.Workbench.Perspective", "AiDe.Core.Ipc.IpcRequest",
        };

        internal sealed record Result(int Roots, int References, string[] Ports, string[] Errors)
        {
            public override string ToString() => $"DIRECT_STATIC roots={Roots} references={References} ports={Ports.Length} errors={Errors.Length}\n"
                + string.Join("\n", Errors) + "\nPORTS " + string.Join("; ", Ports);
        }

        private IEnumerable<SyntaxTree> Trees => Core.SyntaxTrees.Concat(App.SyntaxTrees);
        internal static bool InProject(string root, string path, string project) =>
            Path.GetRelativePath(root, path).Replace('\\', '/').StartsWith("src/" + project + "/", StringComparison.Ordinal);
        private string Relative(SyntaxTree tree) => Path.GetRelativePath(Root, tree.FilePath).Replace('\\', '/');
        private SyntaxTree Tree(string path)
        {
            var found = Trees.Where(tree => Relative(tree) == path).ToArray();
            if (found.Length != 1) throw new InvalidOperationException($"ROOT {path}: expected 1 source, found {found.Length}");
            return found[0];
        }

        private static D0Boundary Load()
        {
            var root = RepoRoot();
            var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
            var paths = new[] { "AiDe.Core", "AiDe.App" }.SelectMany(project =>
                Directory.EnumerateFiles(Path.Combine(root, "src", project), "*.cs", SearchOption.AllDirectories)
                    .Where(path => !Path.GetRelativePath(root, path).Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj")))
                .Concat(new[] { "AiDe.Core", "AiDe.App" }.Select(project => Path.Combine(root, "src", project, "obj", configuration,
                    project == "AiDe.App" ? "net10.0-windows" : "net10.0", project + ".GlobalUsings.g.cs")))
                .Concat(new[] { "App.g.cs", "MainWindow.g.cs" }.Select(name => Path.Combine(root, "src", "AiDe.App", "obj", configuration, "net10.0-windows", name)))
                .ToArray();
            Assert.True(paths.Length > 300, "real App/Core source corpus is empty or incomplete");
            var trees = paths.Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), new CSharpParseOptions(LanguageVersion.Preview), path)).ToArray();
            var references = new Dictionary<string, MetadataReference>(StringComparer.OrdinalIgnoreCase);
            var trusted = Assert.IsType<string>(AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"));
            foreach (var path in trusted.Split(Path.PathSeparator).Concat(Directory.GetFiles(AppContext.BaseDirectory, "*.dll")))
            {
                try
                {
                    var name = AssemblyName.GetAssemblyName(path).Name!;
                    if (name is "AiDe.App" or "AiDe.App.Tests") continue;
                    references.TryAdd(name, MetadataReference.CreateFromFile(path));
                }
                catch (BadImageFormatException) { } // Native DLLs are not compiler references.
            }
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable);
            // Separate global-using scopes are necessary: Core imports System.IO, App uses WPF Path.
            // Current built references supply runtime contracts. This analysis does not emit or run
            // generators; any error in selected source fails, independent of unrelated source bodies.
            return new(CSharpCompilation.Create("AiDe.Core", trees.Where(t => InProject(root, t.FilePath, "AiDe.Core")),
                    references.Where(pair => pair.Key != "AiDe.Core").Select(pair => pair.Value), options),
                CSharpCompilation.Create("AiDe.App", trees.Where(t => InProject(root, t.FilePath, "AiDe.App")), references.Values, options), root);
        }

        private List<SyntaxNode> SelectRoots()
        {
            var roots = new List<SyntaxNode> { Tree(Surface).GetRoot(), Tree(Projection).GetRoot() };
            TypeDeclarationSyntax Type(string path, string owner)
            {
                var types = Tree(path).GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>().Where(n => n.Identifier.ValueText == owner).ToArray();
                if (types.Length != 1) throw new InvalidOperationException($"ROOT {path}:{owner}: found {types.Length}");
                return types[0];
            }
            void Methods(string path, string owner, string name, int count = 1, string? parameter = null)
            {
                var found = Type(path, owner).Members.OfType<MethodDeclarationSyntax>().Where(n => n.Identifier.ValueText == name
                    && (parameter is null || n.ParameterList.Parameters is [var single] && single.Type!.ToString() == parameter)).ToArray();
                if (found.Length != count) throw new InvalidOperationException($"ROOT {owner}.{name}: expected {count}, found {found.Length}");
                roots.AddRange(found);
            }
            void Field(string path, string owner, string name)
            {
                var found = Type(path, owner).Members.OfType<FieldDeclarationSyntax>().Where(n => n.Declaration.Variables.Any(v => v.Identifier.ValueText == name)).ToArray();
                if (found.Length != 1) throw new InvalidOperationException($"ROOT {owner}.{name}: found {found.Length}");
                roots.Add(found[0]);
            }
            Methods(Queries, "IWorkspaceQueries", "SolutionTreeAsync");
            Methods(Queries, "LocalWorkspaceQueries", "SolutionTreeAsync");
            Methods(Client, "WorkspaceClient", "SolutionTreeAsync");
            Methods(Service, "ProjectionService", "SolutionTree", 3);
            foreach (var name in new[] { "ShrinkTree", "TagSolutionTree", "IsFilenameOnly", "IsHostileArtifactPath" }) Methods(Service, "ProjectionService", name);
            foreach (var name in new[] { "FramedCost", "Weigh" }) Methods(Service, "ProjectionService", name, parameter: "SolutionTreeResult");
            foreach (var name in new[] { "BindSolutionTrees", "RefreshSolutionTrees", "RetrySolutionTreePopulate", "PopulateSolutionTreesAsync",
                "OnSolutionTreeActivateRequested", "OnSolutionTreeShowGraphRequested", "OnSolutionTreeRetryRequested" }) Methods(Shell, "WorkbenchShell", name);
            Field(Shell, "WorkbenchShell", "_solutionTreeGeneration");
            Field(Shell, "WorkbenchShell", "_solutionTreeCts");
            Field(Operations, "WorkspaceOperations", "SolutionTree");
            var registrations = Type(Operations, "WorkspaceOperations").DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Where(n => n.Expression.ToString() == "endpoint.Register" && n.ArgumentList.Arguments.FirstOrDefault()?.Expression.ToString() == "SolutionTree").ToArray();
            if (registrations.Length != 1) throw new InvalidOperationException($"ROOT solution-tree IPC registration: found {registrations.Length}");
            roots.Add(registrations[0]);
            var rows = Type(Factory, "SurfaceContentFactory").DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>()
                .Where(n => n.ArgumentList.Arguments.FirstOrDefault()?.Expression is LiteralExpressionSyntax text && text.Token.ValueText == "solution-tree").ToArray();
            if (rows.Length != 1) throw new InvalidOperationException($"ROOT solution-tree factory row: found {rows.Length}");
            roots.Add(rows[0]);
            return roots;
        }

        internal Result Analyze()
        {
            List<SyntaxNode> roots;
            try { roots = SelectRoots(); }
            catch (InvalidOperationException error) { return new(0, 0, [], [error.Message]); }
            var errors = new SortedSet<string>(StringComparer.Ordinal);
            // simplify: these eight boundary files must be unconditional until build defines are
            // explicitly qualified. Check the files, including directives surrounding a root.
            foreach (var tree in roots.Select(root => root.SyntaxTree).Distinct())
                if (tree.GetRoot().DescendantTrivia(descendIntoTrivia: true).Any(trivia =>
                    trivia.IsKind(SyntaxKind.DisabledTextTrivia) || trivia.GetStructure() is
                        IfDirectiveTriviaSyntax or ElifDirectiveTriviaSyntax or ElseDirectiveTriviaSyntax or EndIfDirectiveTriviaSyntax))
                    errors.Add("CONDITIONAL D0 boundary source requires qualification: " + Relative(tree));
            var ports = new SortedSet<string>(StringComparer.Ordinal);
            var references = 0;
            bool Rooted(ISymbol symbol) => symbol.DeclaringSyntaxReferences.Any(reference =>
                roots.Any(root => root.SyntaxTree == reference.SyntaxTree && root.FullSpan.Contains(reference.Span)));
            bool RootOwner(INamedTypeSymbol type) => roots.Any(root => root.AncestorsAndSelf().OfType<TypeDeclarationSyntax>()
                .Any(owner => owner.Identifier.ValueText == type.Name && owner.SyntaxTree.FilePath == type.Locations.FirstOrDefault(location => location.IsInSource)?.SourceTree?.FilePath));
            void Inspect(ISymbol? symbol, SyntaxNode at)
            {
                if (symbol is IAliasSymbol alias) symbol = alias.Target;
                if (symbol is null or ILocalSymbol or IParameterSymbol or ITypeParameterSymbol or IRangeVariableSymbol or IDiscardSymbol) return;
                if (symbol is IArrayTypeSymbol array) { Inspect(array.ElementType, at); return; }
                if (symbol is INamedTypeSymbol generic)
                    foreach (var argument in generic.TypeArguments.Where(argument => argument is not ITypeParameterSymbol)) Inspect(argument, at);
                if (symbol is IMethodSymbol { AssociatedSymbol: { } associated }) symbol = associated;
                if (symbol.IsImplicitlyDeclared && symbol.ContainingType is not null) symbol = symbol.ContainingType;
                symbol = symbol.OriginalDefinition;
                // App binds current built Core metadata; map that symbol back to the real Core source
                // for exact selected spans and source-path checks, not namespace-only inference.
                if (symbol.ContainingAssembly?.Name == "AiDe.Core" && !symbol.Locations.Any(location => location.IsInSource)
                    && symbol.GetDocumentationCommentId() is { } id)
                {
                    var sourceSymbol = DocumentationCommentId.GetFirstSymbolForDeclarationId(id, Core);
                    if (sourceSymbol is null) { errors.Add("UNRESOLVED Core source declaration " + id); return; }
                    symbol = sourceSymbol;
                }
                var ns = symbol is INamespaceSymbol space ? space.ToDisplayString() : symbol.ContainingNamespace?.ToDisplayString() ?? "";
                if (ns == "AiDe.Core.Understanding" || ns.StartsWith("AiDe.Core.Understanding.", StringComparison.Ordinal)
                    || symbol is not INamespaceSymbol && symbol.DeclaringSyntaxReferences.Any(reference => Relative(reference.SyntaxTree).StartsWith("src/AiDe.Core/Understanding/", StringComparison.Ordinal)))
                {
                    errors.Add($"ATLAS {symbol.ToDisplayString()} at {Relative(at.SyntaxTree)}:{at.GetLocation().GetLineSpan().StartLinePosition.Line + 1}");
                    return;
                }
                if (symbol is INamespaceSymbol || ns == "System" || ns.StartsWith("System.", StringComparison.Ordinal)
                    || ns == "Microsoft.Win32" || Rooted(symbol)) return;
                if (symbol is INamedTypeSymbol named && RootOwner(named)) return;
                var key = symbol is INamedTypeSymbol ? symbol.ToDisplayString() : symbol.ContainingType?.ToDisplayString() + "." + symbol.Name;
                if (SharedMembers.Contains(key) || symbol is INamedTypeSymbol && (SharedTypes.Contains(key)
                    || SharedMembers.Any(member => member.StartsWith(key + ".", StringComparison.Ordinal))))
                    ports.Add(key);
                else errors.Add("UNACCOUNTED " + symbol.Kind + " " + key + " at " + Relative(at.SyntaxTree));
            }
            foreach (var root in roots)
            {
                var model = (Relative(root.SyntaxTree).StartsWith("src/AiDe.Core/", StringComparison.Ordinal) ? Core : App).GetSemanticModel(root.SyntaxTree);
                foreach (var error in model.GetDiagnostics(root.Span).Where(d => d.Severity == DiagnosticSeverity.Error)) errors.Add("UNRESOLVED " + error);
                foreach (var node in root.DescendantNodesAndSelf())
                {
                    var symbol = model.GetSymbolInfo(node).Symbol;
                    if (symbol is not null) { references++; Inspect(symbol, node); }
                    else if (node is InvocationExpressionSyntax invocation && invocation.Expression.ToString() != "nameof")
                        errors.Add("UNRESOLVED invocation " + node + " at " + Relative(node.SyntaxTree));
                    if (node is ExpressionSyntax expression && expression.ToString() != "nameof") Inspect(model.GetTypeInfo(expression).Type, node);
                }
            }
            return new(roots.Count, references, ports.ToArray(), errors.ToArray());
        }

        internal D0Boundary Mutate(string mutation)
        {
            const string atlas = "System.GC.KeepAlive(typeof(AiDe.Core.Understanding.SourceProjectionState));";
            D0Boundary Replace(string path, Func<SyntaxNode, SyntaxNode> change)
            {
                var old = Tree(path);
                var next = CSharpSyntaxTree.Create((CSharpSyntaxNode)change(old.GetRoot()), (CSharpParseOptions)old.Options, old.FilePath);
                return path.StartsWith("src/AiDe.Core/", StringComparison.Ordinal) ? this with { Core = Core.ReplaceSyntaxTree(old, next) } : this with { App = App.ReplaceSyntaxTree(old, next) };
            }
            D0Boundary Inject(string path, string name, string statement, bool helper = false, bool alias = false) => Replace(path, root =>
            {
                var method = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Single(n => n.Identifier.ValueText == name);
                var changed = method.WithBody(method.Body!.WithStatements(method.Body.Statements.Insert(0, SyntaxFactory.ParseStatement(statement))));
                var type = (TypeDeclarationSyntax)method.Parent!;
                var members = type.Members.Replace(method, changed);
                if (helper) members = members.Add(SyntaxFactory.ParseMemberDeclaration("private static void UnaccountedD0Helper() {}")!);
                var updated = root.ReplaceNode(type, type.WithMembers(members));
                return alias ? ((CompilationUnitSyntax)updated).AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("AiDe.Core.Understanding.SourceProjectionState"))
                    .WithAlias(SyntaxFactory.NameEquals("AtlasAlias"))) : updated;
            });
            if (mutation == "source-path")
            {
                var changed = Inject(Service, "IsHostileArtifactPath", "AiDe.Core.Projections.HiddenAtlasPath.Touch();");
                return changed with { Core = changed.Core.AddSyntaxTrees(CSharpSyntaxTree.ParseText(
                    "namespace AiDe.Core.Projections; public static class HiddenAtlasPath { public static void Touch() {} }",
                    new CSharpParseOptions(LanguageVersion.Preview), Path.Combine(Root, "src/AiDe.Core/Understanding/HiddenAtlasPath.cs"))) };
            }
            return mutation switch
            {
                "direct" => Inject(Surface, "ShowLoading", atlas),
                "conditional-atlas" => Inject(Surface, "ShowLoading", "\n#if WINDOWS\n" + atlas + "\n#endif\nSystem.GC.KeepAlive(0);"),
                "alias" => Inject(Surface, "ShowLoading", "System.GC.KeepAlive(typeof(AtlasAlias));", alias: true),
                "lambda" => Inject(Surface, "ShowLoading", "System.Action dependency = () => { " + atlas + " };"),
                "local-function" => Inject(Surface, "ShowLoading", "void Dependency() { " + atlas + " }"),
                "shared-seam" => Inject(Client, "SolutionTreeAsync", atlas),
                "new-helper" => Inject(Shell, "RefreshSolutionTrees", "UnaccountedD0Helper();", helper: true),
                "missing-member" => Replace(Shell, root => root.ReplaceNode(root.DescendantNodes().OfType<MethodDeclarationSyntax>().Single(n => n.Identifier.ValueText == "OnSolutionTreeShowGraphRequested"),
                    root.DescendantNodes().OfType<MethodDeclarationSyntax>().Single(n => n.Identifier.ValueText == "OnSolutionTreeShowGraphRequested").WithIdentifier(SyntaxFactory.Identifier("RemovedD0Root")))),
                "missing-file" => this with { Core = Core.RemoveSyntaxTrees(Tree(Projection)) },
                "duplicate-row" => Replace(Factory, root => CSharpSyntaxTree.ParseText(root.ToFullString().Replace("new(\"code-atlas\",", "new(\"solution-tree\",", StringComparison.Ordinal)).GetRoot()),
                "unresolved" => Inject(Surface, "ShowLoading", "MissingD0Symbol();"),
                "comments-strings" => Inject(Surface, "ShowLoading", "/* AiDe.Core.Understanding */ System.GC.KeepAlive(\"AiDe.Core.Understanding.SourceProjectionState\");"),
                "shared-port" => Inject(Surface, "ShowLoading", "System.GC.KeepAlive(AiDe.Core.PathComparison.ForThisFileSystem);"),
                "unrelated-atlas" => Replace(Factory, root => root.ReplaceNode(root.DescendantNodes().OfType<TypeDeclarationSyntax>().Single(n => n.Identifier.ValueText == "SurfaceContentFactory"),
                    root.DescendantNodes().OfType<TypeDeclarationSyntax>().Single(n => n.Identifier.ValueText == "SurfaceContentFactory").AddMembers(SyntaxFactory.ParseMemberDeclaration("private static void UnusedAtlas() { " + atlas + " }")!))),
                "unrelated-registration" => Replace(Factory, root =>
                {
                    var row = root.DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>().Single(n =>
                        n.ArgumentList.Arguments.FirstOrDefault()?.Expression is LiteralExpressionSyntax text && text.Token.ValueText == "code-atlas");
                    var builder = row.DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().Single();
                    return root.ReplaceNode(builder, builder.WithBody(SyntaxFactory.Block(SyntaxFactory.ParseStatement(atlas), SyntaxFactory.ReturnStatement((ExpressionSyntax)builder.Body))));
                }),
                _ => throw new ArgumentOutOfRangeException(nameof(mutation)),
            };
        }
    }

    [Fact]
    public void ProbeFileRead_ViewSourcePathDoesNotReadWorkspaceFiles()
    {
        var surfacePath = Path.Combine(RepoRoot(), "src", "AiDe.App", "Workbench", "SolutionTreeSurface.cs");
        var shellPath = Path.Combine(RepoRoot(), "src", "AiDe.App", "Workbench", "WorkbenchShell.cs");
        Assert.DoesNotMatch(FileReads, File.ReadAllText(surfacePath));

        var root = Path.Combine(RepoRoot(), "src", "AiDe.App");
        var scanned = 0;
        var offenders = new List<string>();
        foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            scanned++;
            var rel = Path.GetRelativePath(root, file);
            var pinned = FileReadAllow.GetValueOrDefault(rel, 0);
            var hits = File.ReadLines(file)
                .Select((line, i) => (line, i))
                .Where(x => FileReads.IsMatch(x.line) && !x.line.TrimStart().StartsWith("//", StringComparison.Ordinal))
                .ToList();

            if (hits.Count != pinned)
            {
                offenders.Add($"{rel}: {hits.Count} read(s), {pinned} pinned"
                    + string.Concat(hits.Select(h => $"\n      :{h.i + 1}: {h.line.Trim()}")));
            }
        }

        Assert.True(scanned > 50, $"only {scanned} App file(s) scanned");
        Assert.True(offenders.Count == 0,
            "new App File.Read/Open of workspace source (View source must use NodeContentAsync):\n  "
            + string.Join("\n  ", offenders));

        var source = File.ReadAllText(shellPath);
        Assert.Contains("OnSolutionTreeActivateRequested", source, StringComparison.Ordinal);
        Assert.Contains("OpenNodeView(request.NodeId, request.Kind)", source, StringComparison.Ordinal);
        Assert.Contains("NodeContentSource.GetAsync", source, StringComparison.Ordinal);
    }

    [Fact]
    public void FactoryBuild_SolutionTree_IsTheNativeSurface()
    {
        var content = Sta.Run(() =>
        {
            var created = new SurfaceContentFactory(queries: null)
                .Create(new AiDe.Core.Workbench.Surface("solution-tree-1", "solution-tree", "Solution tree"));
            while (created is Border { Child: FrameworkElement inner })
            {
                created = inner;
            }

            return created.GetType().Name;
        }, 60);

        Assert.Equal(nameof(SolutionTreeSurface), content);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AiDe.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir!.FullName;
    }
}
