using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using AiDe.App.Workbench;
using AiDe.Core.Projections;

namespace AiDe.App.Tests;

/// <summary>
/// US-T11 / B14: App-assembly enum, Atlas, and View-source file-read probes for D-0.
/// </summary>
public sealed class SolutionTreeProbeTests
{
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
    public void ProbeAtlas_NoUnderstandingNamespaceAndNoUnderstandingFolder()
    {
        var understandingDir = Path.Combine(RepoRoot(), "src", "AiDe.Core", "Understanding");
        Assert.False(Directory.Exists(understandingDir), "src/AiDe.Core/Understanding must not exist for D-0");

        var types = typeof(SolutionTreeSurface).Assembly.GetTypes()
            .Concat(typeof(IWorkspaceQueries).Assembly.GetTypes())
            .Where(t => t.Namespace is not null
                && t.Namespace.StartsWith("AiDe.Core.Understanding", StringComparison.Ordinal))
            .Select(t => t.FullName)
            .ToList();
        Assert.True(types.Count == 0, "Atlas types loaded: " + string.Join(", ", types));
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
