using System.Diagnostics;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace ExtractionFidelitySpike;

/// <summary>
/// The <see cref="MSBuildWorkspace"/> baseline — what Option B is measured AGAINST, not what ships.
/// Loading a project this way executes its build logic (spike D3), which is safe here only because
/// every project loaded is one of ours.
/// </summary>
internal static class Baseline
{
    internal sealed record Result(
        string Project, bool Loaded, int Documents, int Types, int Edges, int UnresolvedEdges,
        double Millis, IReadOnlyList<string> TypeNames, string? Error)
    {
        internal double EdgeResolution => Edges == 0 ? 1.0 : 1.0 - ((double)UnresolvedEdges / Edges);
    }

    internal static void Register()
    {
        if (MSBuildLocator.IsRegistered) return;
        var instance = MSBuildLocator.QueryVisualStudioInstances().OrderByDescending(i => i.Version).First();
        MSBuildLocator.RegisterInstance(instance);
    }

    internal static async Task<Result> LoadAsync(string projectPath)
    {
        var watch = Stopwatch.StartNew();
        var failures = new List<WorkspaceDiagnostic>();
        using var workspace = MSBuildWorkspace.Create();
        workspace.WorkspaceFailed += (_, e) => failures.Add(e.Diagnostic);

        Project project;
        try
        {
            project = await workspace.OpenProjectAsync(projectPath);
        }
        catch (Exception ex)
        {
            watch.Stop();
            return new Result(Path.GetFileName(projectPath), false, 0, 0, 0, 0,
                watch.Elapsed.TotalMilliseconds, [], $"{ex.GetType().Name}: {ex.Message}");
        }

        var compilation = await project.GetCompilationAsync();
        watch.Stop();

        var types = new List<INamedTypeSymbol>();
        if (compilation is not null) Walk(compilation.Assembly.GlobalNamespace, types);
        var (edges, unresolved) = DirectExtractor.CountEdges(types);

        return new Result(
            Path.GetFileName(projectPath), true, project.Documents.Count(), types.Count,
            edges, unresolved, watch.Elapsed.TotalMilliseconds,
            types.Select(t => t.ToDisplayString()).OrderBy(n => n, StringComparer.Ordinal).ToList(),
            failures.Count == 0 ? null : string.Join(" | ", failures.Take(2).Select(f => f.Message)));

        static void Walk(INamespaceSymbol ns, List<INamedTypeSymbol> into)
        {
            foreach (var t in ns.GetTypeMembers()) into.Add(t);
            foreach (var c in ns.GetNamespaceMembers()) Walk(c, into);
        }
    }
}
