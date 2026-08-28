using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace MsBuildTaskSpike;

/// <summary>
/// Separated from <c>Program</c> because touching an MSBuild type forces the assembly load, and
/// <c>MSBuildLocator</c> must have registered first. (The same split S2 needed.)
/// </summary>
internal static class TaskExecutionProbe
{
    /// <summary>
    /// The question: does opening a repository through <see cref="MSBuildWorkspace"/> execute code
    /// that the repository supplied? Returns true when the probe produced a TRUSTWORTHY answer —
    /// not when the answer is "safe".
    /// </summary>
    internal static async Task<bool> RunAsync(string projectPath)
    {
        Console.WriteLine("PROBE 1 — MSBuildWorkspace.OpenProjectAsync on the hostile project");
        Console.WriteLine();

        Markers.Clear();

        var failures = new List<WorkspaceDiagnostic>();
        using var workspace = MSBuildWorkspace.Create();
        workspace.WorkspaceFailed += (_, e) => failures.Add(e.Diagnostic);

        Project? project = null;
        try
        {
            project = await workspace.OpenProjectAsync(projectPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  OpenProjectAsync threw {ex.GetType().Name}: {ex.Message}");
        }

        Console.WriteLine($"  WorkspaceFailed diagnostics : {failures.Count}");
        foreach (var f in failures.Take(6)) Console.WriteLine($"    - {f.Kind}: {f.Message}");
        Console.WriteLine();

        // ---------------------------------------------------------------- non-vacuity guard
        // Absent markers mean "nothing executed" ONLY if the project actually loaded. A workspace
        // that loaded nothing writes no markers either, and would read as a clean bill of health
        // from an instrument that never ran (DC-009 / DC-016).
        var documents = project?.Documents.Count() ?? 0;
        var loaded = project is not null && documents > 0;

        Console.WriteLine($"  NON-VACUITY GUARD");
        Console.WriteLine($"    project loaded            : {(project is not null ? "yes" : "NO")}");
        Console.WriteLine($"    source documents          : {documents}");
        Console.WriteLine($"    assembly name             : {project?.AssemblyName ?? "(none)"}");
        Console.WriteLine($"    metadata references       : {project?.MetadataReferences.Count() ?? 0}");

        if (!loaded)
        {
            Console.WriteLine();
            Console.WriteLine("  ** THE PROBE IS VOID. The project did not load, so the absence of markers");
            Console.WriteLine("     says nothing about whether MSBuildWorkspace executes repository code.");
            return false;
        }

        Console.WriteLine("    => the workspace really did evaluate this project");
        Console.WriteLine();
        Markers.Report("MARKERS AFTER OpenProjectAsync:");
        return true;
    }
}
