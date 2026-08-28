using System.Text.Json;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace ExtractionContainmentSpike;

/// <summary>
/// The sandboxed half. Runs in its OWN process so the containment has something to contain, and
/// reports through a file rather than stdout because a low-integrity child cannot always write to
/// a redirected handle owned by a medium-integrity parent.
/// </summary>
internal static class Child
{
    internal sealed record Report(bool Loaded, int Documents, int Types, string[] TypeNames, string? Error);

    internal static async Task<int> RunAsync(string projectPath, string reportPath)
    {
        Report report;
        try
        {
            if (!MSBuildLocator.IsRegistered)
            {
                var instance = MSBuildLocator.QueryVisualStudioInstances()
                    .OrderByDescending(i => i.Version).First();
                MSBuildLocator.RegisterInstance(instance);
            }
            report = await LoadAsync(projectPath);
        }
        catch (Exception ex)
        {
            report = new Report(false, 0, 0, [], $"{ex.GetType().Name}: {ex.Message}");
        }

        try
        {
            File.WriteAllText(reportPath, JsonSerializer.Serialize(report));
        }
        catch
        {
            // A contained child may be unable to write even here. The parent treats a missing
            // report as "did not complete", which is the honest reading.
        }
        return report.Loaded ? 0 : 1;
    }

    // Separated so touching an MSBuild type does not force the assembly load before registration.
    private static async Task<Report> LoadAsync(string projectPath)
    {
        using var workspace = MSBuildWorkspace.Create();
        var failures = new List<WorkspaceDiagnostic>();
        workspace.WorkspaceFailed += (_, e) => failures.Add(e.Diagnostic);

        Project project;
        try
        {
            project = await workspace.OpenProjectAsync(projectPath);
        }
        catch (Exception ex)
        {
            return new Report(false, 0, 0, [], $"OpenProjectAsync: {ex.GetType().Name}: {ex.Message}");
        }

        var compilation = await project.GetCompilationAsync();
        var types = new List<string>();
        if (compilation is not null) Walk(compilation.Assembly.GlobalNamespace, types);

        return new Report(
            true, project.Documents.Count(), types.Count, types.Order().ToArray(),
            failures.Count == 0 ? null : string.Join(" | ", failures.Take(3).Select(f => f.Message)));

        static void Walk(INamespaceSymbol ns, List<string> into)
        {
            foreach (var t in ns.GetTypeMembers()) into.Add(t.ToDisplayString());
            foreach (var c in ns.GetNamespaceMembers()) Walk(c, into);
        }
    }
}
