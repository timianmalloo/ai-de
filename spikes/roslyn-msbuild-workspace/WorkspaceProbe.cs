using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace RoslynMsBuildSpike;

/// <summary>How a probe attempts to prevent repository code from executing.</summary>
internal enum Suppression
{
    /// <summary>No control at all — the baseline that establishes whether a threat exists.</summary>
    None,

    /// <summary>MSBuild properties only. Depends on the SDK targets honouring them.</summary>
    MsBuildProperties,

    /// <summary>
    /// Strip <c>AnalyzerReferences</c> from the loaded solution before compiling. A control we own
    /// outright at the Roslyn layer, independent of whether any MSBuild target cooperates.
    /// </summary>
    StripAnalyzerReferences,
}

/// <summary>Loads projects through <c>MSBuildWorkspace</c> and reports what actually happened.</summary>
/// <remarks>
/// Kept out of <c>Program</c> deliberately: touching an <c>MSBuildWorkspace</c> type triggers the
/// assembly load, and MSBuildLocator must have registered first. Splitting the class is the
/// documented way to keep the JIT from running ahead of the registration.
/// </remarks>
internal sealed class WorkspaceProbe(string repoRoot)
{
    private readonly string _sentinelPath =
        Path.Combine(Path.GetTempPath(), $"aide-spike-sentinel-{Environment.ProcessId}.txt");

    /// <summary>Q1 — does a real, current solution load, and what does it complain about?</summary>
    public async Task LoadRealSolutionAsync(string solutionPath, VisualStudioInstance boundTo)
    {
        Console.WriteLine($"Solution : {Path.GetRelativePath(repoRoot, solutionPath)}");
        Console.WriteLine($"Bound to : MSBuild {boundTo.Version} (the oldest available, on purpose)");
        Console.WriteLine();

        var failures = new List<WorkspaceDiagnostic>();
        using var workspace = MSBuildWorkspace.Create();
        workspace.WorkspaceFailed += (_, e) => failures.Add(e.Diagnostic);

        var started = DateTimeOffset.UtcNow;
        Solution solution;
        try
        {
            solution = await workspace.OpenSolutionAsync(solutionPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAIL: OpenSolutionAsync threw {ex.GetType().Name}: {ex.Message}");
            return;
        }

        var elapsed = DateTimeOffset.UtcNow - started;
        var projects = solution.Projects.ToList();
        Console.WriteLine($"Loaded {projects.Count} project(s) in {elapsed.TotalSeconds:F1}s");

        foreach (var project in projects.OrderBy(p => p.Name))
        {
            var compilation = await project.GetCompilationAsync();
            var typeCount = compilation is null
                ? 0
                : compilation.GlobalNamespace.GetNamespaceMembers()
                    .SelectMany(CountTypes).Count();

            Console.WriteLine(
                $"  {project.Name,-22} docs={project.Documents.Count(),-4} "
                + $"refs={project.MetadataReferences.Count,-4} analyzers={project.AnalyzerReferences.Count,-3} "
                + $"types={typeCount,-4} compilation={(compilation is null ? "NULL" : "ok")}");
        }

        Console.WriteLine();
        Console.WriteLine($"WorkspaceFailed diagnostics: {failures.Count}");
        foreach (var group in failures.GroupBy(f => Classify(f.Message)).OrderByDescending(g => g.Count()))
        {
            Console.WriteLine($"  [{group.Key}] ×{group.Count()}");
            Console.WriteLine($"      e.g. {Truncate(group.First().Message, 160)}");
        }

        // The load-bearing question is not "were there diagnostics" — there almost always are — but
        // "did we still get real semantic content out". A partial load that yields types is a
        // degraded success; a clean load that yields none is a failure wearing a green label.
        var anySymbols = projects.Count > 0;
        Console.WriteLine();
        Console.WriteLine(anySymbols
            ? "→ A real solution loads against a NON-matching (older) SDK."
            : "→ The solution did not load.");
    }

    /// <summary>Q2/Q3 — does repository-authored generator code run, and can that be stopped?</summary>
    public async Task<ExecutionResult> ExecutionProbeAsync(string projectPath, Suppression suppression)
    {
        Console.WriteLine($"Suppression: {suppression}");

        File.Delete(_sentinelPath);
        Environment.SetEnvironmentVariable("AIDE_SPIKE_SENTINEL", _sentinelPath);

        var properties = new Dictionary<string, string>();
        if (suppression == Suppression.MsBuildProperties)
        {
            // The documented SDK switches. Whether they reach the workspace's project model at all
            // is exactly what is being measured — they are candidates, not a known-good answer.
            properties["RunAnalyzers"] = "false";
            properties["RunAnalyzersDuringBuild"] = "false";
            properties["EnforceCodeStyleInBuild"] = "false";
        }

        var failures = new List<WorkspaceDiagnostic>();
        using var workspace = MSBuildWorkspace.Create(properties);
        workspace.WorkspaceFailed += (_, e) => failures.Add(e.Diagnostic);

        Project project;
        try
        {
            project = await workspace.OpenProjectAsync(projectPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  FAIL: OpenProjectAsync threw {ex.GetType().Name}: {ex.Message}");
            return ExecutionResult.LoadFailed;
        }

        Console.WriteLine($"  analyzer references after load : {project.AnalyzerReferences.Count}");
        foreach (var reference in project.AnalyzerReferences)
        {
            Console.WriteLine($"      {reference.Display}");
        }

        Console.WriteLine($"  sentinel after OpenProjectAsync: {SentinelSummary()}");

        if (suppression == Suppression.StripAnalyzerReferences)
        {
            var stripped = project.Solution.WithProjectAnalyzerReferences(project.Id, []);
            project = stripped.GetProject(project.Id)!;
            Console.WriteLine($"  analyzer references after strip: {project.AnalyzerReferences.Count}");
        }

        // Two distinct drives. GetCompilationAsync is what an extractor calls for symbols;
        // GetSourceGeneratedDocumentsAsync is what explicitly runs the generator pipeline. They are
        // measured separately because an extractor that only ever calls the first has a materially
        // smaller exposure than one that calls both.
        var compilation = await project.GetCompilationAsync();
        Console.WriteLine($"  sentinel after GetCompilation  : {SentinelSummary()}"
            + $"   (compilation {(compilation is null ? "NULL" : "ok")})");

        var generated = await project.GetSourceGeneratedDocumentsAsync();
        Console.WriteLine($"  sentinel after GetSourceGenerated: {SentinelSummary()}"
            + $"   (generated documents: {generated.Count()})");

        // What the control COSTS. Suppressing execution is only viable if the hand-written semantic
        // model survives it — an extractor that is safe and blind has not solved the problem. These
        // are the symbols Phase 2 actually extracts, so they are counted rather than assumed.
        if (compilation is not null)
        {
            var declared = compilation.GlobalNamespace.GetNamespaceMembers()
                .SelectMany(CountTypes)
                .Where(t => t.Locations.Any(l => l.IsInSource))
                .ToList();
            Console.WriteLine($"  source-declared types visible  : {declared.Count}"
                + $"  [{string.Join(", ", declared.Take(6).Select(t => t.Name))}]");
            Console.WriteLine($"  compilation diagnostics (error): "
                + $"{compilation.GetDiagnostics().Count(d => d.Severity == DiagnosticSeverity.Error)}");
        }

        if (failures.Count > 0)
        {
            Console.WriteLine($"  workspace diagnostics: {failures.Count}"
                + $" — e.g. {Truncate(failures[0].Message, 140)}");
        }

        return Read();
    }

    private ExecutionResult Read()
    {
        if (!File.Exists(_sentinelPath))
        {
            return ExecutionResult.Silent;
        }

        var lines = File.ReadAllLines(_sentinelPath)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Split('\t'))
            .ToList();

        if (lines.Count == 0)
        {
            return ExecutionResult.Silent;
        }

        var phases = string.Join(",", lines.Select(p => p[0]).Distinct());
        var pids = lines
            .Select(p => p.Length > 1 ? p[1].Replace("pid=", string.Empty) : "?")
            .Distinct()
            .ToList();

        var ours = Environment.ProcessId.ToString();
        return new ExecutionResult(true, true, pids.Contains(ours), phases, string.Join(",", pids));
    }

    private string SentinelSummary()
    {
        var result = Read();
        return result.RanAtAll
            ? $"FIRED [{result.Phases}] pid={result.Pids}{(result.RanInOurProcess ? " (OURS)" : " (child)")}"
            : "silent";
    }

    private static IEnumerable<INamedTypeSymbol> CountTypes(INamespaceSymbol ns) =>
        ns.GetTypeMembers().Concat(ns.GetNamespaceMembers().SelectMany(CountTypes));

    private static string Classify(string message) => message switch
    {
        var m when m.Contains("not supported", StringComparison.OrdinalIgnoreCase) => "unsupported project type",
        var m when m.Contains("SDK", StringComparison.OrdinalIgnoreCase) => "SDK resolution",
        var m when m.Contains("could not be found", StringComparison.OrdinalIgnoreCase) => "missing file",
        var m when m.Contains("Msbuild failed", StringComparison.OrdinalIgnoreCase) => "MSBuild evaluation",
        _ => "other",
    };

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";
}
