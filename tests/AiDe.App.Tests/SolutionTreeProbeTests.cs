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
    [InlineData("shared-overload", "UNACCOUNTED")]
    [InlineData("system-source", "UNACCOUNTED")]
    [InlineData("conditional-release", "CONDITIONAL")]
    [InlineData("conditional-enclosing", "CONDITIONAL")]
    [InlineData("conditional-alias", "CONDITIONAL")]
    [InlineData("conditional-definition", "CONDITIONAL")]
    [InlineData("conditional-hidden-root", "ROOT")]
    [InlineData("conditional-global-alias", "CONDITIONAL")]
    [InlineData("relocated-port", "UNRESOLVED")]
    [InlineData("generic-atlas", "ATLAS")]
    [InlineData("conditional-trailing-root", "CONDITIONAL")]
    [InlineData("conditional-partial-definition", "CONDITIONAL")]
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
    [InlineData("unrelated-conditional")]
    [InlineData("unrelated-inactive-member")]
    [InlineData("unrelated-conditional-import")]
    [InlineData("unrelated-conditional-registration")]
    [InlineData("unrelated-conditional-same-name")]
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

        // Reviewed declaration identities: exact source authority and full original signature.
        // This manifest is frozen source, never discovered or refreshed by the running test.
        private static readonly HashSet<string> SharedPorts = new(StringComparer.Ordinal)
        {
            "src/AiDe.App/Workbench/CanvasSurface.cs|T:AiDe.App.Workbench.CanvasSurface",
            "src/AiDe.App/Workbench/CodeViewerView.cs|T:AiDe.App.Workbench.CodeViewerView",
            "src/AiDe.App/Workbench/DockHost.cs|T:AiDe.App.Workbench.DockHost",
            "src/AiDe.App/Workbench/NodeViewMenu.cs|F:AiDe.App.Workbench.NodeViewKind.GraphNeighbourhood",
            "src/AiDe.App/Workbench/NodeViewMenu.cs|F:AiDe.App.Workbench.NodeViewKind.Read",
            "src/AiDe.App/Workbench/NodeViewMenu.cs|F:AiDe.App.Workbench.NodeViewKind.Source",
            "src/AiDe.App/Workbench/NodeViewMenu.cs|T:AiDe.App.Workbench.NodeViewKind",
            "src/AiDe.App/Workbench/RelayCommand.cs|M:AiDe.App.Workbench.RelayCommand.#ctor(System.Action)",
            "src/AiDe.App/Workbench/RelayCommand.cs|T:AiDe.App.Workbench.RelayCommand",
            "src/AiDe.App/Workbench/SurfaceContentFactory.cs|F:AiDe.App.Workbench.SurfaceContentFactory.Instances.One",
            "src/AiDe.App/Workbench/SurfaceContentFactory.cs|M:AiDe.App.Workbench.SurfaceContentFactory.SurfaceEntry.Derived.#ctor(System.String)",
            "src/AiDe.App/Workbench/SurfaceContentFactory.cs|M:AiDe.App.Workbench.SurfaceContentFactory.SurfaceKind.#ctor(System.String,System.String,System.String,System.Func{AiDe.App.Workbench.SurfaceContentFactory,AiDe.Core.Workbench.Surface,System.Windows.FrameworkElement},System.Collections.Generic.IReadOnlyList{AiDe.Core.Workbench.Perspective},AiDe.App.Workbench.SurfaceContentFactory.Instances,AiDe.App.Workbench.SurfaceContentFactory.SurfaceEntry,System.Boolean,System.Nullable{AiDe.Core.Workbench.ZoneId})",
            "src/AiDe.App/Workbench/SurfaceContentFactory.cs|T:AiDe.App.Workbench.SurfaceContentFactory.Instances",
            "src/AiDe.App/Workbench/SurfaceContentFactory.cs|T:AiDe.App.Workbench.SurfaceContentFactory.SurfaceEntry",
            "src/AiDe.App/Workbench/SurfaceContentFactory.cs|T:AiDe.App.Workbench.SurfaceContentFactory.SurfaceEntry.Derived",
            "src/AiDe.App/Workbench/SurfaceContentFactory.cs|T:AiDe.App.Workbench.SurfaceContentFactory.SurfaceKind",
            "src/AiDe.App/Workbench/WorkbenchAnnouncer.cs|M:AiDe.App.Workbench.IWorkbenchAnnouncer.Announce(System.String)",
            "src/AiDe.App/Workbench/WorkbenchAnnouncer.cs|T:AiDe.App.Workbench.IWorkbenchAnnouncer",
            "src/AiDe.App/Workbench/WorkbenchShell.cs|F:AiDe.App.Workbench.WorkbenchShell._lastSelectedNodeId",
            "src/AiDe.App/Workbench/WorkbenchShell.cs|F:AiDe.App.Workbench.WorkbenchShell._queries",
            "src/AiDe.App/Workbench/WorkbenchShell.cs|M:AiDe.App.Workbench.WorkbenchShell.CentreOnAsync(AiDe.App.Workbench.CanvasSurface,System.String,System.String,System.Boolean)",
            "src/AiDe.App/Workbench/WorkbenchShell.cs|M:AiDe.App.Workbench.WorkbenchShell.OpenCanvas",
            "src/AiDe.App/Workbench/WorkbenchShell.cs|M:AiDe.App.Workbench.WorkbenchShell.OpenCodeViewers",
            "src/AiDe.App/Workbench/WorkbenchShell.cs|M:AiDe.App.Workbench.WorkbenchShell.OpenKind(AiDe.App.Workbench.DockHost,System.String,System.Boolean)",
            "src/AiDe.App/Workbench/WorkbenchShell.cs|M:AiDe.App.Workbench.WorkbenchShell.OpenNodeView(System.String,AiDe.App.Workbench.NodeViewKind)",
            "src/AiDe.App/Workbench/WorkbenchShell.cs|M:AiDe.App.Workbench.WorkbenchShell.ShowNodeInCodeViewersAsync(System.String,System.Collections.Generic.IReadOnlyList{AiDe.App.Workbench.CodeViewerView},System.Boolean)",
            "src/AiDe.App/Workbench/WorkbenchShell.cs|M:AiDe.App.Workbench.WorkbenchShell.SurfaceContents``1(System.String)",
            "src/AiDe.App/Workbench/WorkbenchShell.cs|P:AiDe.App.Workbench.WorkbenchShell.Announcer",
            "src/AiDe.App/Workbench/WorkbenchShell.cs|P:AiDe.App.Workbench.WorkbenchShell.Architecture",
            "src/AiDe.Core/Extraction/UnanalysedLanguages.cs|F:AiDe.Core.Extraction.UnanalysedLanguages.Skip",
            "src/AiDe.Core/Extraction/UnanalysedLanguages.cs|T:AiDe.Core.Extraction.UnanalysedLanguages",
            "src/AiDe.Core/Ipc/DaemonEndpoint.cs|M:AiDe.Core.Ipc.DaemonEndpoint.Register(System.String,System.Func{AiDe.Core.Ipc.IpcRequest,AiDe.Core.Ipc.IpcPeer,AiDe.Core.Ipc.IpcResponse})",
            "src/AiDe.Core/Ipc/DaemonEndpoint.cs|T:AiDe.Core.Ipc.DaemonEndpoint",
            "src/AiDe.Core/Ipc/IpcContract.cs|M:AiDe.Core.Ipc.IpcResponse.Success``1(``0,System.Text.Json.JsonSerializerOptions)",
            "src/AiDe.Core/Ipc/IpcContract.cs|T:AiDe.Core.Ipc.IpcRequest",
            "src/AiDe.Core/Ipc/IpcContract.cs|T:AiDe.Core.Ipc.IpcResponse",
            "src/AiDe.Core/Ipc/WorkspaceClient.cs|M:AiDe.Core.Ipc.WorkspaceClient.QueryAsync``1(System.String,System.Object,System.Threading.CancellationToken,System.String)",
            "src/AiDe.Core/Ipc/WorkspaceOperations.cs|M:AiDe.Core.Ipc.WorkspaceOperations.Handle``1(AiDe.Core.Ipc.IpcRequest,System.Func{``0,System.Object})",
            "src/AiDe.Core/Ipc/WorkspaceOperations.cs|M:AiDe.Core.Ipc.WorkspaceOperations.Refusable(System.Func{AiDe.Core.Ipc.IpcResponse})",
            "src/AiDe.Core/Ipc/WorkspaceOperations.cs|T:AiDe.Core.Ipc.WorkspaceOperations",
            "src/AiDe.Core/PathComparison.cs|P:AiDe.Core.PathComparison.ForThisFileSystem",
            "src/AiDe.Core/PathComparison.cs|T:AiDe.Core.PathComparison",
            "src/AiDe.Core/Projections/ProjectionService.cs|F:AiDe.Core.Projections.ProjectionService.Activity",
            "src/AiDe.Core/Projections/ProjectionService.cs|F:AiDe.Core.Projections.ProjectionService.FrameBytes",
            "src/AiDe.Core/Projections/ProjectionService.cs|F:AiDe.Core.Projections.ProjectionService.MaxFramedGraphBytes",
            "src/AiDe.Core/Projections/ProjectionService.cs|F:AiDe.Core.Projections.ProjectionService.Wire",
            "src/AiDe.Core/Projections/ProjectionService.cs|M:AiDe.Core.Projections.ProjectionService.CandidateWithinWorkspace(AiDe.Core.Store.StoreReader,System.String,System.String)",
            "src/AiDe.Core/Projections/ProjectionService.cs|T:AiDe.Core.Projections.ProjectionService",
            "src/AiDe.Core/Store/StoreReader.cs|M:AiDe.Core.Store.StoreReader.AllScopeLocations",
            "src/AiDe.Core/Store/StoreReader.cs|M:AiDe.Core.Store.StoreReader.CurrentSourceRevision",
            "src/AiDe.Core/Store/StoreReader.cs|M:AiDe.Core.Store.StoreReader.FilesToSearch",
            "src/AiDe.Core/Store/StoreReader.cs|M:AiDe.Core.Store.StoreReader.ReadNodeKind(System.String)",
            "src/AiDe.Core/Store/StoreReader.cs|T:AiDe.Core.Store.StoreReader",
            "src/AiDe.Core/Store/WorkspaceStore.cs|M:AiDe.Core.Store.WorkspaceStore.BeginRead",
            "src/AiDe.Core/Store/WorkspaceStore.cs|T:AiDe.Core.Store.WorkspaceStore",
            "src/AiDe.Core/Workbench/LayoutModel.cs|P:AiDe.Core.Workbench.Surface.Title",
            "src/AiDe.Core/Workbench/LayoutModel.cs|T:AiDe.Core.Workbench.Surface",
            "src/AiDe.Core/Workbench/Perspectives.cs|P:AiDe.Core.Workbench.PerspectiveSet.Architecture",
            "src/AiDe.Core/Workbench/Perspectives.cs|T:AiDe.Core.Workbench.Perspective",
            "src/AiDe.Core/Workbench/Perspectives.cs|T:AiDe.Core.Workbench.PerspectiveSet",
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
            var declarations = new List<SyntaxNode>();
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in SharedPorts)
            {
                var parts = entry.Split('|');
                var compilation = parts[0].StartsWith("src/AiDe.Core/", StringComparison.Ordinal) ? Core : App;
                var matches = DocumentationCommentId.GetSymbolsForDeclarationId(parts[1], compilation);
                if (matches.Length != 1 || Identity(matches[0]) != entry)
                    errors.Add("UNRESOLVED shared declaration " + entry);
                else declarations.AddRange(matches[0].DeclaringSyntaxReferences.Select(reference => reference.GetSyntax()));
            }
            string? Identity(ISymbol symbol)
            {
                var paths = symbol.DeclaringSyntaxReferences.Select(reference => Relative(reference.SyntaxTree)).Distinct().ToArray();
                var id = symbol.OriginalDefinition.GetDocumentationCommentId();
                var project = symbol.ContainingAssembly?.Name;
                return paths.Length == 1 && id is not null && project is "AiDe.Core" or "AiDe.App"
                    && paths[0].StartsWith("src/" + project + "/", StringComparison.Ordinal) ? paths[0] + "|" + id : null;
            }
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
                if (symbol is IFieldSymbol { ContainingType.IsTupleType: true } tupleField)
                { Inspect(tupleField.ContainingType.TupleUnderlyingType, at); return; }
                if (symbol is INamedTypeSymbol generic)
                    foreach (var argument in generic.TypeArguments.Where(argument => argument is not ITypeParameterSymbol)) Inspect(argument, at);
                if (symbol is IMethodSymbol method)
                    foreach (var argument in method.TypeArguments.Where(argument => argument is not ITypeParameterSymbol)) Inspect(argument, at);
                if (symbol is IMethodSymbol { AssociatedSymbol: { } associated }) symbol = associated;
                if (symbol.IsImplicitlyDeclared && symbol.ContainingType is not null) symbol = symbol.ContainingType;
                symbol = symbol.OriginalDefinition;
                // App binds current built Core metadata; map that symbol back to the real Core source
                // for exact selected spans and source-path checks, not namespace-only inference.
                if (symbol.ContainingAssembly?.Name == "AiDe.Core" && !symbol.Locations.Any(location => location.IsInSource)
                    && symbol.GetDocumentationCommentId() is { } id)
                {
                    var sourceSymbols = DocumentationCommentId.GetSymbolsForDeclarationId(id, Core);
                    if (sourceSymbols.Length != 1) { errors.Add("UNRESOLVED Core source declaration " + id); return; }
                    symbol = sourceSymbols[0];
                }
                var ns = symbol is INamespaceSymbol space ? space.ToDisplayString() : symbol.ContainingNamespace?.ToDisplayString() ?? "";
                if (ns == "AiDe.Core.Understanding" || ns.StartsWith("AiDe.Core.Understanding.", StringComparison.Ordinal)
                    || symbol is not INamespaceSymbol && symbol.DeclaringSyntaxReferences.Any(reference => Relative(reference.SyntaxTree).StartsWith("src/AiDe.Core/Understanding/", StringComparison.Ordinal)))
                {
                    errors.Add($"ATLAS {symbol.ToDisplayString()} at {Relative(at.SyntaxTree)}:{at.GetLocation().GetLineSpan().StartLinePosition.Line + 1}");
                    return;
                }
                if (symbol is INamespaceSymbol) return;
                names.Add(BindingName(symbol));
                declarations.AddRange(symbol.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax()));
                if (Rooted(symbol)) return;
                // Framework namespace spelling alone is not authority: source-defined System
                // helpers must be accounted. The metadata assembly must carry a framework key.
                var frameworkKey = symbol.ContainingAssembly is { } assembly
                    ? Convert.ToHexString(assembly.Identity.PublicKeyToken.ToArray()) : "";
                if (!symbol.Locations.Any(location => location.IsInSource)
                    && (ns == "System" || ns.StartsWith("System.", StringComparison.Ordinal) || ns == "Microsoft.Win32")
                    && frameworkKey is "7CEC85D7BEA7798E" or "B03F5F7F11D50A3A" or "B77A5C561934E089" or "31BF3856AD364E35" or "CC7B13FFCD2DDD51") return;
                if (symbol is INamedTypeSymbol named && RootOwner(named)) return;
                var key = Identity(symbol);
                if (key is not null && SharedPorts.Contains(key)) ports.Add(key);
                else errors.Add("UNACCOUNTED " + symbol.Kind + " " + symbol.ToDisplayString() + " at " + Relative(at.SyntaxTree));
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
            CheckConditions(roots, declarations, names, errors);
            return new(roots.Count, references, ports.ToArray(), errors.ToArray());
        }

        private static string BindingName(ISymbol symbol) => symbol is INamedTypeSymbol
            ? symbol.OriginalDefinition.GetDocumentationCommentId() ?? ""
            : symbol.ContainingType?.OriginalDefinition.GetDocumentationCommentId() + "|" + symbol.Name;

        private void CheckConditions(List<SyntaxNode> roots, List<SyntaxNode> declarations, HashSet<string> names, SortedSet<string> errors)
        {
            // Syntax-only view exposes all inactive branches at their original offsets. It is
            // not a Debug/Release compilation. Ambiguous or binding-affecting regions refuse.
            foreach (var tree in Trees)
            {
                var syntax = tree.GetRoot();
                var directives = syntax.DescendantTrivia(descendIntoTrivia: true).Select(trivia => trivia.GetStructure())
                    .OfType<DirectiveTriviaSyntax>().ToArray();
                if (!directives.Any(directive => directive is IfDirectiveTriviaSyntax)) continue;
                var text = syntax.ToFullString().ToCharArray();
                foreach (var directive in directives)
                    for (var i = directive.FullSpan.Start; i < directive.FullSpan.End; i++)
                        if (text[i] is not '\r' and not '\n') text[i] = ' ';
                var flatTree = CSharpSyntaxTree.ParseText(new string(text), (CSharpParseOptions)tree.Options, tree.FilePath);
                var flat = flatTree.GetRoot();
                // Declaration owners can be read even when multiple branches coexist. This model
                // classifies names only; it never certifies the flattened program as executable.
                var compilation = Core.SyntaxTrees.Contains(tree) ? Core : App;
                var flatModel = compilation.ReplaceSyntaxTree(tree, flatTree).GetSemanticModel(flatTree);
                var declared = flat.DescendantNodes().Where(node => node is BaseTypeDeclarationSyntax or MethodDeclarationSyntax
                    or ConstructorDeclarationSyntax or PropertyDeclarationSyntax or EventDeclarationSyntax or DelegateDeclarationSyntax or VariableDeclaratorSyntax)
                    .Select(node => (Node: node, Symbol: flatModel.GetDeclaredSymbol(node))).ToArray();
                var stack = new Stack<int>();
                foreach (var directive in directives)
                {
                    if (directive is IfDirectiveTriviaSyntax) stack.Push(directive.SpanStart);
                    if (directive is not EndIfDirectiveTriviaSyntax) continue;
                    if (!stack.TryPop(out var start)) { errors.Add("CONDITIONAL unmatched directive " + Relative(tree)); continue; }
                    var span = Microsoft.CodeAnalysis.Text.TextSpan.FromBounds(start, directive.Span.End);
                    var localRoots = roots.Where(root => root.SyntaxTree == tree).ToArray();
                    var localDeclarations = declarations.Where(node => node.SyntaxTree == tree).ToArray();
                    bool Touches(SyntaxNode node) => node.Span.OverlapsWith(span);
                    var imports = flat.DescendantNodes().OfType<UsingDirectiveSyntax>().Where(Touches).ToArray();
                    var bindings = declared.Where(item => Touches(item.Node) && item.Symbol is not null && names.Contains(BindingName(item.Symbol))).ToArray();
                    var relevant = localRoots.Length != 0 || localDeclarations.Length != 0 || bindings.Length != 0 || imports.Any(node => node.GlobalKeyword.RawKind != 0);
                    if (!relevant) continue;
                    var members = flat.DescendantNodes().OfType<MemberDeclarationSyntax>().Where(Touches).ToArray();
                    // A whole enclosing class is not a dependency on each of its unrelated bodies.
                    bool RelevantDeclaration(SyntaxNode node) => node is not BaseTypeDeclarationSyntax && Touches(node);
                    var blocked = Relative(tree) is Surface or Projection || localRoots.Any(Touches) || localDeclarations.Any(RelevantDeclaration)
                        || imports.Length != 0
                        || bindings.Any(item => span.Contains(item.Node.SpanStart))
                        || declared.Any(item => span.Contains(item.Node.SpanStart) && item.Symbol is null);
                    var body = flat.DescendantNodes().OfType<BlockSyntax>().Any(block => block.Span.Contains(span)
                        && !localRoots.Any(root => root.Span.OverlapsWith(block.Span))
                        && !localDeclarations.Any(node => node is not BaseTypeDeclarationSyntax && node.Span.OverlapsWith(block.Span)));
                    var independentMembers = members.Where(member => span.Contains(member.Span)).ToArray();
                    if (blocked || flat.GetDiagnostics().Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error && diagnostic.Location.SourceSpan.OverlapsWith(span))
                        || !body && independentMembers.Length == 0)
                        errors.Add("CONDITIONAL binding/root region requires qualification: " + Relative(tree) + ":" + span.Start);
                }
                if (stack.Count != 0) errors.Add("CONDITIONAL unmatched region " + Relative(tree));
            }
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
            if (mutation == "system-source")
            {
                var changed = Inject(Surface, "ShowLoading", "System.ReviewEscape.Touch();");
                return changed with { App = changed.App.AddSyntaxTrees(CSharpSyntaxTree.ParseText(
                    "namespace System; public static class ReviewEscape { public static void Touch() { GC.KeepAlive(typeof(AiDe.Core.Understanding.SourceProjectionState)); } }",
                    new CSharpParseOptions(LanguageVersion.Preview), Path.Combine(Root, "src/AiDe.App/ReviewEscape.cs"))) };
            }
            if (mutation is "conditional-global-alias" or "unrelated-conditional-import")
                return this with { App = App.AddSyntaxTrees(CSharpSyntaxTree.ParseText(
                    "#if RELEASE\n" + (mutation == "conditional-global-alias" ? "global " : "")
                    + "using ConditionalAlias = AiDe.Core.Understanding.SourceProjectionState;\n#endif\nnamespace Unrelated; internal class ImportOwner {}",
                    new CSharpParseOptions(LanguageVersion.Preview), Path.Combine(Root, "src/AiDe.App/UnrelatedImport.cs"))) };
            if (mutation == "conditional-partial-definition")
                return this with { App = App.AddSyntaxTrees(CSharpSyntaxTree.ParseText(
                    "namespace AiDe.App.Workbench;\n#if RELEASE\npublic partial class WorkbenchShell { private static void OpenKind(int sentinel) {} }\n#endif\n",
                    new CSharpParseOptions(LanguageVersion.Preview), Path.Combine(Root, "src/AiDe.App/ConditionalShell.cs"))) };
            return mutation switch
            {
                "conditional-trailing-root" => Replace(Projection, root => CSharpSyntaxTree.ParseText(root.ToFullString()
                    + "\n#if RELEASE\ninternal static class HiddenD0Dependency { static void Dependency() { " + atlas + " } }\n#endif\n").GetRoot()),
                "unrelated-conditional-same-name" => Replace(Factory, root => CSharpSyntaxTree.ParseText(root.ToFullString()
                    + "\n#if RELEASE\ninternal static class UnrelatedOwner { static void OpenKind(int sentinel) { " + atlas + " } }\n#endif\n").GetRoot()),
                "generic-atlas" => Inject(Surface, "ShowLoading", "System.GC.KeepAlive(System.Array.Empty<AiDe.Core.Understanding.SourceProjectionState>());"),
                "relocated-port" => this with { Core = Core.ReplaceSyntaxTree(Tree("src/AiDe.Core/PathComparison.cs"),
                    Tree("src/AiDe.Core/PathComparison.cs").WithFilePath(Path.Combine(Root, "src/AiDe.Core/RelocatedPathComparison.cs"))) },
                "conditional-hidden-root" => Replace(Shell, root =>
                {
                    var method = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Single(n => n.Identifier.ValueText == "RefreshSolutionTrees");
                    return CSharpSyntaxTree.ParseText(root.ToFullString().Insert(method.Span.End, "\n#endif\n").Insert(method.SpanStart, "\n#if RELEASE\n")).GetRoot();
                }),
                "shared-overload" => Replace(Shell, root =>
                {
                    var owner = root.DescendantNodes().OfType<TypeDeclarationSyntax>().Single(n => n.Identifier.ValueText == "WorkbenchShell");
                    var method = owner.Members.OfType<MethodDeclarationSyntax>().Single(n => n.Identifier.ValueText == "RefreshSolutionTrees");
                    return root.ReplaceNode(owner, owner.WithMembers(owner.Members.Replace(method, method.WithBody(method.Body!.WithStatements(
                        method.Body.Statements.Insert(0, SyntaxFactory.ParseStatement("OpenKind(0);"))))).Add(
                        SyntaxFactory.ParseMemberDeclaration("private static void OpenKind(int sentinel) { " + atlas + " }")!)));
                }),
                "conditional-release" => Inject(Shell, "RefreshSolutionTrees", "\n#if !DEBUG\n" + atlas + "\n#endif\nSystem.GC.KeepAlive(0);"),
                "conditional-enclosing" => Replace(Shell, root =>
                {
                    var method = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Single(n => n.Identifier.ValueText == "RefreshSolutionTrees");
                    return CSharpSyntaxTree.ParseText(root.ToFullString().Insert(method.Span.End, "\n#endif\n").Insert(method.SpanStart, "\n#if true\n")).GetRoot();
                }),
                "conditional-alias" => Replace(Shell, root => CSharpSyntaxTree.ParseText("#if RELEASE\nusing ConditionalAlias = AiDe.Core.Understanding.SourceProjectionState;\n#endif\n" + root.ToFullString()).GetRoot()),
                "conditional-definition" => Replace(Shell, root =>
                {
                    var owner = root.DescendantNodes().OfType<TypeDeclarationSyntax>().Single(n => n.Identifier.ValueText == "WorkbenchShell");
                    return CSharpSyntaxTree.ParseText(root.ToFullString().Insert(owner.CloseBraceToken.SpanStart,
                        "\n#if RELEASE\nprivate static void OpenKind(int sentinel) { " + atlas + " }\n#endif\n")).GetRoot();
                }),
                "unrelated-conditional" => Replace(Factory, root => root.ReplaceNode(root.DescendantNodes().OfType<TypeDeclarationSyntax>().Single(n => n.Identifier.ValueText == "SurfaceContentFactory"),
                    root.DescendantNodes().OfType<TypeDeclarationSyntax>().Single(n => n.Identifier.ValueText == "SurfaceContentFactory").AddMembers(
                        SyntaxFactory.ParseMemberDeclaration("private static void UnusedConditionalAtlas() {\n#if true\n" + atlas + "\n#endif\n}")!))),
                "unrelated-inactive-member" => Replace(Factory, root =>
                {
                    var owner = root.DescendantNodes().OfType<TypeDeclarationSyntax>().Single(n => n.Identifier.ValueText == "SurfaceContentFactory");
                    return CSharpSyntaxTree.ParseText(root.ToFullString().Insert(owner.CloseBraceToken.SpanStart,
                        "\n#if RELEASE\nprivate static void UnusedConditionalAtlas() { " + atlas + " }\n#endif\n")).GetRoot();
                }),
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
                "unrelated-registration" or "unrelated-conditional-registration" => Replace(Factory, root =>
                {
                    var row = root.DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>().Single(n =>
                        n.ArgumentList.Arguments.FirstOrDefault()?.Expression is LiteralExpressionSyntax text && text.Token.ValueText == "code-atlas");
                    var builder = row.DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().Single();
                    var statement = mutation == "unrelated-registration" ? atlas : "\n#if true\n" + atlas + "\n#endif\n;";
                    return root.ReplaceNode(builder, builder.WithBody(SyntaxFactory.Block(SyntaxFactory.ParseStatement(statement), SyntaxFactory.ReturnStatement((ExpressionSyntax)builder.Body))));
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
