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
    [InlineData("release-extension", "BINDING")]
    [InlineData("core-release-extension", "BINDING")]
    [InlineData("competing-extensions", "BINDING")]
    [InlineData("population-overflow", "COVERAGE")]
    [InlineData("unsupported-dynamic", "UNSUPPORTED")]
    [InlineData("inconsistent-reference", "CLOSURE")]
    [InlineData("parse-options", "CLOSURE")]
    [InlineData("missing-generated", "CLOSURE")]
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
    [InlineData("unimported-extension")]
    [InlineData("incompatible-extension")]
    [InlineData("file-local-undef")]
    [InlineData("eight-assignments")]
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

    [Theory]
    [InlineData(0, false)] [InlineData(1, false)] [InlineData(2, false)] [InlineData(3, true)]
    [InlineData(4, false)] [InlineData(5, false)] [InlineData(6, false)] [InlineData(7, false)]
    public void ProbeAtlas_AssignmentParser_PreservesMaskedAmbiguity(int bits, bool ambiguous)
    {
        // Standalone composition counterexample, not an observed combined-boundary escape.
        var result = D0Boundary.MaskingAssignment(bits);
        _output.WriteLine(result.ToString());
        Assert.Equal(ambiguous, result.Ambiguous);
        Assert.Equal(ambiguous ? 1 : 0, result.Errors);
    }

    [Fact]
    public void ProbeAtlas_CanonicalOperators_RetainSignatureAndSourceAuthority()
    {
        var identities = D0Boundary.OperatorIdentities();
        _output.WriteLine(string.Join("\n",identities));
        Assert.Equal(6,identities.Length);
        Assert.Equal(6,identities.Distinct(StringComparer.Ordinal).Count());
        Assert.All(identities, identity => Assert.DoesNotContain("UNSUPPORTED",identity,StringComparison.Ordinal));
        Assert.StartsWith("builtin:",identities[0],StringComparison.Ordinal);
        Assert.Contains("M:OperatorFixture.Number.op_Equality",identities[5],StringComparison.Ordinal);
    }

    [Fact]
    public void ProbeAtlas_ExhaustiveCoverage_VisitsTheCompletePopulation()
    {
        var result = D0Boundary.Baseline.Value.Analyze();
        _output.WriteLine(result.ToString());
        Assert.True(result.CoverageComplete);
        Assert.Equal(1, result.Population);
        Assert.Equal(result.Population, result.Assignments);
        var eight = D0Boundary.Baseline.Value.Mutate("eight-assignments").Analyze();
        _output.WriteLine(eight.ToString());
        Assert.True(eight.CoverageComplete);
        Assert.Equal(8,eight.Population);
        Assert.Equal(8,eight.Assignments);
        var overflow = D0Boundary.Baseline.Value.Mutate("population-overflow").Analyze();
        Assert.Equal(16,overflow.Population);
        Assert.Equal(0,overflow.Assignments);
        Assert.False(overflow.CoverageComplete);
    }

    [Theory]
    [InlineData("src/AiDe.App/AiDe.App.csproj", "missing")]
    [InlineData("src/AiDe.App/AiDe.App.csproj", "import")]
    [InlineData("src/AiDe.App/AiDe.App.csproj", "project")]
    [InlineData("src/AiDe.Core/AiDe.Core.csproj", "compile")]
    [InlineData("src/Directory.Build.props", "ancestor")]
    [InlineData("src/AiDe.App/App.xaml", "generator")]
    [InlineData("global.json", "sdk")]
    public void ProbeAtlas_ClosureControls_RefuseUnreviewedInputs(string path, string change)
    {
        var boundary = D0Boundary.Baseline.Value;
        var original = File.Exists(Path.Combine(boundary.Root, path)) ? File.ReadAllText(Path.Combine(boundary.Root, path)) : "";
        var replacement = change switch
        {
            "missing" => null,
            "import" => original.Replace("</Project>", "<Import Project=\"unreviewed.props\" /></Project>", StringComparison.Ordinal),
            "project" => original.Replace("</Project>", "<ItemGroup><ProjectReference Include=\"../Unreviewed/Unreviewed.csproj\" /></ItemGroup></Project>", StringComparison.Ordinal),
            "compile" => original.Replace("</Project>", "<ItemGroup Condition=\"'$(Configuration)' == 'Release'\"><Compile Include=\"../Unreviewed.cs\" /></ItemGroup></Project>", StringComparison.Ordinal),
            "ancestor" => "<Project><Import Project=\"unreviewed.props\" /></Project>",
            _ => original + "\n<!-- changed assumption -->",
        };
        var result = (boundary with { BuildInputMutation = (path, replacement) }).Analyze();
        _output.WriteLine(result.ToString());
        Assert.Contains(result.Errors, error => error.Contains("CLOSURE", StringComparison.Ordinal));
        Assert.False(result.CoverageComplete);
    }

    // DIRECT STATIC boundary only. Shared ports end this check; no runtime/transitive claim.
    // Complete D0 files and exact shared-class members are selected with Roslyn, never regex bodies.
    private sealed record D0Boundary(CSharpCompilation Core, CSharpCompilation App, string Root)
    {
        internal (string Path, string? Contents)? BuildInputMutation { get; init; }
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

        internal sealed record Result(int Roots, int References, string[] Ports, string[] Errors,
            int Population = 0, int Assignments = 0, bool CoverageComplete = false, double Seconds = 0,
            int CorpusTrees = 0, int ConditionalSymbols = 0, int Expressions = 0)
        {
            public override string ToString() => $"DIRECT_STATIC roots={Roots} references={References} ports={Ports.Length} errors={Errors.Length} corpusTrees={CorpusTrees} symbols={ConditionalSymbols} expressions={Expressions} population={Population} assignments={Assignments} coverageComplete={CoverageComplete} accepted={CoverageComplete && Errors.Length == 0} seconds={Seconds:F6}\n"
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
            Assert.NotEmpty(paths);
            var trees = paths.Select(path => CSharpSyntaxTree.ParseText(File.Exists(path) ? File.ReadAllText(path) : "", new CSharpParseOptions(LanguageVersion.Preview), path)).ToArray();
            var references = new Dictionary<string, MetadataReference>(StringComparer.OrdinalIgnoreCase);
            var trusted = Assert.IsType<string>(AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"));
            foreach (var path in trusted.Split(Path.PathSeparator).Concat(Directory.GetFiles(AppContext.BaseDirectory, "*.dll")))
            {
                try
                {
                    var name = AssemblyName.GetAssemblyName(path).Name!;
                    if (name is "AiDe.App" or "AiDe.App.Tests") continue;
                    references.TryAdd(name, MetadataReference.CreateFromImage(File.ReadAllBytes(path), filePath: path));
                }
                catch (BadImageFormatException) { } // Native DLLs are not compiler references.
            }
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable);
            // Separate global-using scopes are necessary: Core imports System.IO, App uses WPF Path.
            // Current built references supply runtime contracts. This analysis does not emit or run
            // generators; any error in selected source fails, independent of unrelated source bodies.
            var boundary = new D0Boundary(CSharpCompilation.Create("AiDe.Core", trees.Where(t => InProject(root, t.FilePath, "AiDe.Core")),
                    references.Where(pair => pair.Key != "AiDe.Core").Select(pair => pair.Value), options),
                CSharpCompilation.Create("AiDe.App", trees.Where(t => InProject(root, t.FilePath, "AiDe.App")), references.Values, options), root);
            return boundary with { FrozenCoreReferences=boundary.Core.References.ToArray(), FrozenAppReferences=boundary.App.References.ToArray(),
                FrozenSources=boundary.SourcePaths().ToDictionary(path=>path,path=>Fingerprint(File.ReadAllText(path)),StringComparer.Ordinal) };
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


        // Reviewed finite input contract: App -> Core is the only linked source edge.
        // Daemon/Mcp are nonlinking build edges. New source files are enumerated; build
        // controls and these four generated outputs require explicit review to refresh.
        // simplify: fixed-input direct static proof, <=8 project/symbol assignments.
        // Generator execution and arbitrary MSBuild configurations are outside this contract.
        private static readonly Dictionary<string, string?> ClosurePins = new(StringComparer.Ordinal)
        {
            ["global.json"] = "9a043b1d3955efbb205f9dbd351e25dd0c7f060f9a9a08e03a54ba51a0971190",
            ["Directory.Build.props"] = "43d0fd406351a125e58aa5282409b22e528185bae595cd2938e1b427bc409e69",
            ["Directory.Build.targets"] = null,
            ["Directory.Packages.props"] = "a57d2ee58bd9787119f5d71c07b956a72f986fef30287647aabca25212e95f64",
            [".editorconfig"] = "65f15fcf24ab557f59e559ab600642d73fd91c99ce591606bc01875ee6912a90",
            ["src/AiDe.Core/AiDe.Core.csproj"] = "b63c41b97201dc3e1016b72b404a32411ac31e477231b281219dda64d0ad584a",
            ["src/AiDe.App/AiDe.App.csproj"] = "c986b4633fe66ef732b93c09be5655917a5465b1d9dc412cf1627b1fdabf717a",
            ["src/AiDe.App/App.xaml"] = "e1f2b34819e424a60a2c414c42ae0f62a242ee5681fd0b5275fae8f2b778dd2d",
            ["src/AiDe.App/MainWindow.xaml"] = "9ef0ec4da0f6b153a3902053d6f4870eaf7f0023157009c6c94880cefe98ebdf",
            ["src/AiDe.App/Workbench/DockRoundedTabs.xaml"] = "35dab0de498444b988c7fa41536c6b49456b4448de6a8173f4ab4d2fd7620008",
            ["src/Directory.Build.props"] = null,
            ["src/Directory.Build.targets"] = null,
            ["src/Directory.Packages.props"] = null,
            ["src/.editorconfig"] = null,
            ["src/AiDe.Core/Directory.Build.props"] = null,
            ["src/AiDe.Core/Directory.Build.targets"] = null,
            ["src/AiDe.Core/Directory.Packages.props"] = null,
            ["src/AiDe.Core/.editorconfig"] = null,
            ["src/AiDe.App/Directory.Build.props"] = null,
            ["src/AiDe.App/Directory.Build.targets"] = null,
            ["src/AiDe.App/Directory.Packages.props"] = null,
            ["src/AiDe.App/.editorconfig"] = null,
            ["src/AiDe.Core/obj/{configuration}/net10.0/AiDe.Core.GlobalUsings.g.cs"] = "8f86a2cbf00ff3eee0bda2bc0eb5e1fc19c8544d5dc650c53d0132325956b592",
            ["src/AiDe.App/obj/{configuration}/net10.0-windows/AiDe.App.GlobalUsings.g.cs"] = "7edae69b7b83f15f0a18cd279829bd011a2f5da720a54b569606e635305037da",
            ["src/AiDe.App/obj/{configuration}/net10.0-windows/App.g.cs"] = "b3558cb0eb7a1c2522c7e2069b98603eb084e39d4e0b3dd2acd09b4df3805b1e",
            ["src/AiDe.App/obj/{configuration}/net10.0-windows/MainWindow.g.cs"] = "1c468b97911b0bad8a3e0304de7d46f7d6f7204f027fba8ec2a8e21d3077ec80",
        };
        private MetadataReference[] FrozenCoreReferences { get; init; } = [];
        private MetadataReference[] FrozenAppReferences { get; init; } = [];
        private Dictionary<string,string> FrozenSources { get; init; } = new(StringComparer.Ordinal);
        // Fingerprints are decoded UTF-8 source text (BOM consumed), CRLF -> LF only.
        private static string Fingerprint(string text) => Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(text.Replace("\r\n", "\n", StringComparison.Ordinal))));
        private IEnumerable<string> SourcePaths() => new[] { "AiDe.Core", "AiDe.App" }.SelectMany(project =>
            Directory.EnumerateFiles(Path.Combine(Root, "src", project), "*.cs", SearchOption.AllDirectories)
                .Where(path => !Path.GetRelativePath(Root, path).Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj")));

        private string[] ClosureErrors()
        {
            try { return ReadClosureErrors(); }
            catch (IOException exception) { return ["CLOSURE unreadable input: " + exception.Message]; }
            catch (UnauthorizedAccessException exception) { return ["CLOSURE inaccessible input: " + exception.Message]; }
        }

        private string[] ReadClosureErrors()
        {
            var errors = new List<string>();
            var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
            foreach (var (pattern, expected) in ClosurePins)
            {
                var path = pattern.Replace("{configuration}", configuration, StringComparison.Ordinal);
                var absolute = Path.Combine(Root, path);
                string? content = BuildInputMutation is { } mutation && mutation.Path == path ? mutation.Contents
                    : File.Exists(absolute) ? File.ReadAllText(absolute) : null;
                if ((content is null ? null : Fingerprint(content)) != expected)
                    errors.Add("CLOSURE reviewed input changed/missing/new: " + path);
                if (pattern.Contains("/{configuration}/", StringComparison.Ordinal))
                {
                    var found = Trees.Where(t => Relative(t) == path).ToArray();
                    if (found.Length != 1 || Fingerprint(found[0].GetText().ToString()) != expected)
                        errors.Add("CLOSURE generated input: " + path);
                }
            }
            var xaml = Directory.EnumerateFiles(Path.Combine(Root, "src/AiDe.App"), "*.xaml", SearchOption.AllDirectories)
                .Where(path => !path.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj"))
                .Select(path => Path.GetRelativePath(Root,path).Replace('\\','/')).Order().ToArray();
            if (!xaml.SequenceEqual(ClosurePins.Keys.Where(path => path.EndsWith(".xaml",StringComparison.Ordinal)).Order()))
                errors.Add("CLOSURE unsupported generator input inventory");
            if (!Core.References.SequenceEqual(FrozenCoreReferences) || !App.References.SequenceEqual(FrozenAppReferences))
                errors.Add("CLOSURE inconsistent frozen references");
            var current = SourcePaths().Order().ToArray();
            if (!current.SequenceEqual(FrozenSources.Keys.Order()) || current.Any(path => Fingerprint(File.ReadAllText(path)) != FrozenSources[path]))
                errors.Add("CLOSURE source inputs mutated after capture");
            if (Trees.Any(tree => tree.Options is not CSharpParseOptions { LanguageVersion: LanguageVersion.Preview }))
                errors.Add("CLOSURE unsupported parse options");
            return errors.ToArray();
        }

        internal Result Analyze()
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            var closure = ClosureErrors();
            if (closure.Length != 0) return new(0,0,[],closure);
            var direct = AnalyzeDirect();
            if (direct.Roots != 26) return direct;
            var symbols = Census(Core,App);
            var population = System.Numerics.BigInteger.One << symbols.Length;
            if (population > 8) return direct with { Errors = ["COVERAGE assignment-ceiling population=" + population], Population = population <= int.MaxValue ? (int)population : int.MaxValue };
            var errors = new SortedSet<string>(direct.Errors,StringComparer.Ordinal);
            var attempted=0; var complete=true; var expressions=0;
            try
            {
                var expected = Snapshot(Core,App,out var baselineErrors,out _);
                expressions=expected.Count;
                if (baselineErrors != 0) { errors.Add("BINDING unresolved baseline"); complete=false; }
                for (var bits=0;bits<(int)population;bits++)
                {
                    var core=Assign(Core,"AiDe.Core",symbols,bits);
                    var app=Propagate(core,Assign(App,"AiDe.App",symbols,bits));
                    var actual=Snapshot(core,app,out var diagnostics,out var roots);
                    attempted++;
                    if (diagnostics != 0) { errors.Add("BINDING unresolved assignment="+bits+" diagnostics="+diagnostics); complete=false; }
                    if (roots != direct.Roots) { errors.Add("BINDING root population assignment="+bits); complete=false; }
                    if (actual.Values.Any(value=>value.Contains("UNSUPPORTED|",StringComparison.Ordinal)||value.Contains("UNMAPPED|",StringComparison.Ordinal)))
                    { errors.Add("UNSUPPORTED binding identity assignment="+bits); complete=false; }
                    var changes=expected.Keys.Union(actual.Keys).Count(key=>expected.GetValueOrDefault(key)!=actual.GetValueOrDefault(key));
                    if (changes!=0) errors.Add("BINDING changed assignment="+bits+" expressions="+changes);
                }
            }
            catch (InvalidOperationException exception) { errors.Add("COVERAGE incomplete: "+exception.Message); complete=false; }
            foreach (var error in ClosureErrors()) { errors.Add(error); complete=false; }
            return direct with { Errors=errors.ToArray(), Population=(int)population, Assignments=attempted,
                CoverageComplete=complete && attempted==(int)population, Seconds=timer.Elapsed.TotalSeconds,
                CorpusTrees=Trees.Count(), ConditionalSymbols=symbols.Length, Expressions=expressions };
        }

        private static CSharpCompilation Propagate(CSharpCompilation c, CSharpCompilation a)
        {
         var refs = a.References.Where(reference => a.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly && assembly.Name == "AiDe.Core").ToArray();
         if(refs.Length != 1) throw new InvalidOperationException("Expected exactly one Core project reference, got "+refs.Length);
         return a.RemoveReferences(refs).AddReferences(c.ToMetadataReference());
        }
        private string CanonicalIdentity(ISymbol? symbol,CSharpCompilation currentCore)
        {
         if(symbol is null) return "<none>";
         if(symbol is IAliasSymbol alias) symbol=alias.Target;
         if(symbol is INamespaceSymbol space) return "N:"+space.ToDisplayString();
         if(symbol is IArrayTypeSymbol array) return "array:"+array.Rank+":"+CanonicalIdentity(array.ElementType,currentCore);
         if(symbol is IPointerTypeSymbol pointer) return "pointer:"+CanonicalIdentity(pointer.PointedAtType,currentCore);
         if(symbol is INamedTypeSymbol named && !SymbolEqualityComparer.Default.Equals(named,named.OriginalDefinition))
          return CanonicalIdentity(named.OriginalDefinition,currentCore)+"<"+string.Join(",",named.TypeArguments.Select(t=>CanonicalIdentity(t,currentCore)))+">";
         if(symbol is IMethodSymbol {MethodKind:MethodKind.BuiltinOperator} builtin)
         {
          var signature="builtin:"+builtin.MetadataName+"|return:"+builtin.RefKind+":"+CanonicalIdentity(builtin.ReturnType,currentCore)
           +"|parameters:"+string.Join(";",builtin.Parameters.Select(p=>p.RefKind+":"+CanonicalIdentity(p.Type,currentCore)));
          return signature;
         }

         if(symbol is IMethodSymbol method) symbol=method.ReducedFrom??method;
         if(symbol is IMethodSymbol {AssociatedSymbol:{} associated}) symbol=associated;
         symbol=symbol.OriginalDefinition;
         var id=symbol.GetDocumentationCommentId();
         if(symbol.ContainingAssembly?.Name=="AiDe.Core"&&!symbol.Locations.Any(l=>l.IsInSource)&&id is not null)
         {
          var matches=DocumentationCommentId.GetSymbolsForDeclarationId(id,currentCore);
          if(matches.Length!=1) return "UNMAPPED|"+id;
          symbol=matches[0];
         }
         var paths=symbol.DeclaringSyntaxReferences.Select(r=>Relative(r.SyntaxTree)).Distinct().Order().ToArray();
         var authority=paths.Length>0 ? symbol.ContainingAssembly?.Name+"|"+string.Join(",",paths) : symbol.ContainingAssembly?.Identity.ToString();
         if(id is not null) return authority+"|"+id;
         if(symbol is IParameterSymbol parameter) return "parameter:"+CanonicalIdentity(parameter.ContainingSymbol,currentCore)+":"+parameter.Ordinal;
         if(symbol is ITypeParameterSymbol typeParameter) return "typeParameter:"+CanonicalIdentity(typeParameter.ContainingSymbol,currentCore)+":"+typeParameter.TypeParameterKind+":"+typeParameter.Ordinal;
         if(symbol is IDiscardSymbol discard) return "discard:"+CanonicalIdentity(discard.Type,currentCore);
         var locations=string.Join(";",symbol.DeclaringSyntaxReferences.Select(reference=>Relative(reference.SyntaxTree)+":"+reference.Span).Order());
         if(locations.Length>0 && symbol is ILocalSymbol local) return authority+"|local:"+locations+":"+CanonicalIdentity(local.Type,currentCore);
         if(locations.Length>0 && symbol is IRangeVariableSymbol) return authority+"|range:"+locations;
         if(locations.Length>0 && symbol is IMethodSymbol {MethodKind:MethodKind.AnonymousFunction or MethodKind.LocalFunction} function)
          return authority+"|function:"+locations+":"+function.RefKind+":"+CanonicalIdentity(function.ReturnType,currentCore)+":"+string.Join(";",function.Parameters.Select(parameter=>parameter.RefKind+":"+CanonicalIdentity(parameter.Type,currentCore)));
         return "UNSUPPORTED|"+symbol.Kind+"|"+symbol.ToDisplayString();
        }
        private Dictionary<string,string> Snapshot(CSharpCompilation c,CSharpCompilation a,out int errors,out int selectedRoots)
        {
         var roots=(this with { Core=c, App=a }).SelectRoots();
         selectedRoots=roots.Count; errors=0;
         var snapshot=new Dictionary<string,string>();
         for(var index=0;index<roots.Count;index++)
         {
          var selected=roots[index];
          var model=(c.SyntaxTrees.Contains(selected.SyntaxTree)?c:a).GetSemanticModel(selected.SyntaxTree);
          errors+=model.GetDiagnostics(selected.Span).Count(d=>d.Severity==DiagnosticSeverity.Error);
          var expressionIndex=0;
          foreach(var node in selected.DescendantNodesAndSelf().OfType<ExpressionSyntax>())
          {
           var info=model.GetSymbolInfo(node);
           var value=CanonicalIdentity(info.Symbol,c)+"|candidate:"+info.CandidateReason+"|type:"+CanonicalIdentity(model.GetTypeInfo(node).Type,c);
           if(info.CandidateSymbols.Length>0) value+="|candidates:"+string.Join(";",info.CandidateSymbols.Select(s=>CanonicalIdentity(s,c)).Order());
           snapshot.Add(index+":"+Relative(selected.SyntaxTree)+":"+(expressionIndex++)+":"+(node.SpanStart-selected.SpanStart)+":"+node.RawKind,value);
          }
         }
         return snapshot;
        }
        private static (string Project,string Symbol)[] Census(CSharpCompilation c,CSharpCompilation a) => new[]{("AiDe.Core",c),("AiDe.App",a)}
         .SelectMany(project=>project.Item2.SyntaxTrees.SelectMany(tree=>tree.GetRoot().DescendantTrivia(descendIntoTrivia:true)
          .Select(trivia=>trivia.GetStructure()).SelectMany(directive=>directive switch {
           IfDirectiveTriviaSyntax i=>i.Condition.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>(),
           ElifDirectiveTriviaSyntax e=>e.Condition.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>(),
           _=>Enumerable.Empty<IdentifierNameSyntax>()}).Select(identifier=>(project.Item1,identifier.Identifier.ValueText))))
         .Distinct().OrderBy(pair=>pair.Item1,StringComparer.Ordinal).ThenBy(pair=>pair.Item2,StringComparer.Ordinal).ToArray();
        private static CSharpCompilation Assign(CSharpCompilation compilation,string project,(string Project,string Symbol)[] symbols,int bits)
        {
         var owned=symbols.Where(pair=>pair.Project==project).Select(pair=>pair.Symbol).ToHashSet(StringComparer.Ordinal);
         foreach(var tree in compilation.SyntaxTrees.ToArray())
         {
          var options=(CSharpParseOptions)tree.Options;
          var defined=options.PreprocessorSymbolNames.Where(name=>!owned.Contains(name)).Concat(symbols.Select((pair,index)=>(pair,index))
           .Where(item=>item.pair.Project==project&&(bits&(1<<item.index))!=0).Select(item=>item.pair.Symbol));
          compilation=compilation.ReplaceSyntaxTree(tree,CSharpSyntaxTree.ParseText(tree.GetText(),options.WithPreprocessorSymbols(defined),tree.FilePath));
         }
         return compilation;
        }
        private Result AnalyzeDirect()
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


        internal static (bool Ambiguous,int Errors) MaskingAssignment(int bits)
        {
            const string source = "namespace BindingFixture; public interface I2 {} public partial interface I1 {} public partial class Receiver:I1 {} public static class Probe { public static string Pick(I1 value)=>\"baseline\"; public static string Run()=>Pick(new Receiver());\n#if A\npublic static string Pick(I2 value)=>\"conditional\";\n#endif\n}\n#if B\npublic partial class Receiver:I2 {}\n#endif\n#if C\npublic partial interface I1:I2 {}\n#endif\n";
            var tree=CSharpSyntaxTree.ParseText(source,new CSharpParseOptions(LanguageVersion.Preview),"Mask.cs");
            var compilation=CSharpCompilation.Create("Mask",[tree],Baseline.Value.Core.References,new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var keys=Census(compilation,compilation.RemoveAllSyntaxTrees());
            if (keys.Length!=3) throw new InvalidOperationException("mask census must contain three keys");
            compilation=Assign(compilation,"AiDe.Core",keys,bits);
            tree=compilation.SyntaxTrees.Single();
            var method=tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m=>m.Identifier.ValueText=="Run");
            var model=compilation.GetSemanticModel(tree);
            var info=model.GetSymbolInfo(method.DescendantNodes().OfType<InvocationExpressionSyntax>().Single());
            return (info.CandidateReason==CandidateReason.OverloadResolutionFailure,model.GetDiagnostics(method.Span).Count(d=>d.Severity==DiagnosticSeverity.Error));
        }

        internal static string[] OperatorIdentities()
        {
            var boundary=Baseline.Value;
            var results=new List<string>();
            foreach (var (expression,file) in new[] { ("a == b","First.cs"),("a != b","First.cs"),("(long)0 == 1L","First.cs"),("1 == 2","First.cs"),("a == b","Second.cs") })
            {
                var tree=CSharpSyntaxTree.ParseText("namespace OperatorFixture; public enum E { A } public static class Probe { public static bool Run(E a,E b)=>"+expression+"; }",path:Path.Combine(boundary.Root,"src/AiDe.App",file));
                var compilation=CSharpCompilation.Create("OperatorFixture",[tree],boundary.Core.References,new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
                results.Add(boundary.CanonicalIdentity(compilation.GetSemanticModel(tree).GetSymbolInfo(tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single()).Symbol,boundary.Core));
            }
            var user=CSharpSyntaxTree.ParseText("namespace OperatorFixture; public struct Number { public static bool operator ==(Number a,Number b)=>true; public static bool operator !=(Number a,Number b)=>false; public static bool Run(Number a,Number b)=>a==b; }",path:Path.Combine(boundary.Root,"src/AiDe.App/User.cs"));
            var userCompilation=CSharpCompilation.Create("OperatorFixture",[user],boundary.Core.References,new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            results.Add(boundary.CanonicalIdentity(userCompilation.GetSemanticModel(user).GetSymbolInfo(user.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single()).Symbol,boundary.Core));
            return results.ToArray();
        }

        internal D0Boundary Mutate(string mutation)
        {
            const string atlas = "System.GC.KeepAlive(typeof(AiDe.Core.Understanding.SourceProjectionState));";
            if (mutation is "release-extension" or "core-release-extension" or "unimported-extension" or "incompatible-extension" or "file-local-undef" or "competing-extensions")
            {
                var project = mutation == "core-release-extension" ? "AiDe.Core" : "AiDe.App";
                var ns = mutation == "unimported-extension" ? "Review.Unrelated" : "AiDe.App.Workbench";
                var argument = mutation == "incompatible-extension" ? "int" : "T";
                var generic = mutation == "incompatible-extension" ? "" : "<T>";
                string Source(string owner) => (mutation == "file-local-undef" ? "#undef RELEASE\n" : "") + "namespace " + ns
                    + ";\n#if RELEASE\npublic static class " + owner + " { public static System.Collections.Generic.List<" + argument + "> ToList" + generic
                    + "(this System.Collections.Generic.IEnumerable<" + argument + "> source) { " + atlas + " return System.Linq.Enumerable.ToList(source); } }\n#endif\n";
                var trees = (mutation == "competing-extensions" ? new[] { "ReviewExtensions", "CompetingExtensions" } : new[] { "ReviewExtensions" })
                    .Select(owner => CSharpSyntaxTree.ParseText(Source(owner), new CSharpParseOptions(LanguageVersion.Preview), Path.Combine(Root, "src", project, owner + ".cs"))).ToArray();
                return project == "AiDe.Core" ? this with { Core = Core.AddSyntaxTrees(trees) } : this with { App = App.AddSyntaxTrees(trees) };
            }
            if (mutation is "population-overflow" or "eight-assignments")
                return this with { App = App.AddSyntaxTrees(CSharpSyntaxTree.ParseText("#if A || B || C" + (mutation == "population-overflow" ? " || D" : "")
                    + "\nnamespace Unrelated { internal class CensusMarker {} }\n#endif\n", new CSharpParseOptions(LanguageVersion.Preview), Path.Combine(Root, "src/AiDe.App/CensusMarker.cs"))) };
            if (mutation == "parse-options") return this with { App=App.RemoveAllSyntaxTrees().AddSyntaxTrees(App.SyntaxTrees.Select(tree =>
                CSharpSyntaxTree.ParseText(tree.GetText(),new CSharpParseOptions(LanguageVersion.CSharp12),tree.FilePath)).ToArray()) };
            if (mutation == "inconsistent-reference") return this with { App = App.RemoveAllReferences() };
            if (mutation == "missing-generated") return this with { App = App.RemoveSyntaxTrees(App.SyntaxTrees.Single(tree => tree.FilePath.EndsWith("App.g.cs", StringComparison.Ordinal))) };
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
                "unsupported-dynamic" => Inject(Surface, "ShowLoading", "System.GC.KeepAlive((dynamic)0);"),
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
