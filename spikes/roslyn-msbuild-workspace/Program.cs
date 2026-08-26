using System.Diagnostics;
using Microsoft.Build.Locator;

namespace RoslynMsBuildSpike;

/// <summary>
/// Spike S2 — can AI-DE load a real solution with <c>MSBuildWorkspace</c>, and can it do so without
/// executing the repository's own analyzers and source generators?
/// </summary>
/// <remarks>
/// <para>The second question is the reason this spike gates Phase 2. AI-DE's stated posture is that
/// repository content is <b>untrusted data</b>: symbol names, doc comments and provenance all arrive
/// as inert typed values, and P1-MCP-INERT tests that they never become instructions. A source
/// generator is different in kind — it is not data the extractor reads, it is <b>code the extractor
/// runs</b>, with the extractor's privileges, at the moment a workspace is opened. If that cannot be
/// turned off, then merely opening a workspace executes whatever the repository author wrote, and
/// the untrusted-content boundary has a hole in it that no amount of careful string handling
/// closes.</para>
///
/// <para><b>What makes this measurable.</b> The presence of an <c>AnalyzerReference</c> in the
/// project model proves only that a reference was <i>read</i>. Execution and mere reference look
/// identical from the project model, and only execution matters. So the fixture generator writes to
/// a sentinel file — a side effect outside the compilation, which no generated syntax can fake —
/// and records the <b>process id</b>, because a generator run by a child MSBuild node is a different
/// finding from one run inside our own process.</para>
/// </remarks>
internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var repoRoot = FindRepoRoot();
        var fixtureProject = Path.Combine(
            repoRoot, "spikes", "roslyn-msbuild-workspace", "fixture", "TargetLib", "TargetLib.csproj");
        var realSolution = Path.Combine(repoRoot, "AiDe.sln");

        Header("Q0 — which MSBuild does the host bind to?");
        var instance = RegisterMsBuild();
        if (instance is null)
        {
            return 1;
        }

        // The probes run in-process and MSBuildWorkspace caches aggressively, so each is a separate
        // workspace. Ordering matters only in that the sentinel is cleared before each.
        var probe = new WorkspaceProbe(repoRoot);

        Header("Q1 — does a REAL solution load, and does the host SDK have to match?");
        await probe.LoadRealSolutionAsync(realSolution, instance);

        Header("Q2 — do the repository's generators EXECUTE when we open and compile it?");
        var baseline = await probe.ExecutionProbeAsync(fixtureProject, Suppression.None);

        Header("Q3a — does suppression via MSBuild properties stop it?");
        var byProperties = await probe.ExecutionProbeAsync(fixtureProject, Suppression.MsBuildProperties);

        Header("Q3b — does stripping analyzer references at the Roslyn layer stop it?");
        var byStripping = await probe.ExecutionProbeAsync(fixtureProject, Suppression.StripAnalyzerReferences);

        Header("Verdict");
        Verdict(baseline, byProperties, byStripping);
        return 0;
    }

    private static VisualStudioInstance? RegisterMsBuild()
    {
        var instances = MSBuildLocator.QueryVisualStudioInstances().ToList();
        Console.WriteLine($"MSBuild instances visible to the host: {instances.Count}");
        foreach (var candidate in instances.OrderByDescending(i => i.Version))
        {
            Console.WriteLine($"  - {candidate.Name} {candidate.Version}  ({candidate.MSBuildPath})");
        }

        if (instances.Count == 0)
        {
            Console.WriteLine("FAIL: no MSBuild instance found; MSBuildWorkspace cannot be used here.");
            return null;
        }

        // Deliberately NOT the newest. The design question is whether extraction breaks when the
        // host's SDK differs from the one a repository was built with, so bind to the OLDEST
        // available and load a solution built by the newest.
        var chosen = instances.OrderBy(i => i.Version).First();
        MSBuildLocator.RegisterInstance(chosen);
        Console.WriteLine();
        Console.WriteLine($"Registered the OLDEST instance on purpose: {chosen.Name} {chosen.Version}");
        Console.WriteLine($"  Host runtime: {Environment.Version}");
        return chosen;
    }

    private static void Verdict(ExecutionResult baseline, ExecutionResult byProperties, ExecutionResult byStripping)
    {
        Console.WriteLine($"  no suppression            : {Describe(baseline)}");
        Console.WriteLine($"  MSBuild properties        : {Describe(byProperties)}");
        Console.WriteLine($"  stripped AnalyzerReferences: {Describe(byStripping)}");
        Console.WriteLine();

        if (!baseline.Loaded || !byProperties.Loaded || !byStripping.Loaded)
        {
            Console.WriteLine("RESULT: INCONCLUSIVE — at least one probe never loaded its project.");
            Console.WriteLine("  A silent sentinel from a project that did not open is a broken instrument,");
            Console.WriteLine("  not a security finding. Fix the load before reading anything into the");
            Console.WriteLine("  silence (defect class DC-009).");
            return;
        }

        if (!baseline.RanAtAll)
        {
            Console.WriteLine("RESULT: the project loaded and no execution was observed WITHOUT suppression.");
            Console.WriteLine("  Do NOT read this as 'generators are safe'. It means this path did not drive");
            Console.WriteLine("  them. An absence of observed execution is not a guarantee of no execution —");
            Console.WriteLine("  the control must still be applied, and the residual named.");
            return;
        }

        Console.WriteLine($"The threat is REAL: repository-authored code ran"
            + $"{(baseline.RanInOurProcess ? " inside our own process" : " in a child process")}.");

        var works = new List<string>();
        if (!byProperties.RanAtAll)
        {
            works.Add("MSBuild properties");
        }

        if (!byStripping.RanAtAll)
        {
            works.Add("stripping AnalyzerReferences");
        }

        if (works.Count == 0)
        {
            Console.WriteLine("RESULT: BLOCKER. Neither control suppressed execution.");
            Console.WriteLine("  Extraction would run untrusted repository code with our privileges. The");
            Console.WriteLine("  approach must change — sandboxed out-of-process extraction, or a load path");
            Console.WriteLine("  that never constructs a generator driver at all.");
            return;
        }

        Console.WriteLine($"RESULT: the control works — via {string.Join(" and ", works)}.");
        Console.WriteLine("  Phase 2 may proceed with that control MANDATORY on every load path and pinned");
        Console.WriteLine("  by a negative test that fails if a generator ever executes during extraction.");
        if (byProperties.RanAtAll)
        {
            Console.WriteLine("  Note: MSBuild properties alone did NOT hold. Prefer the Roslyn-layer control,");
            Console.WriteLine("  which does not depend on the repository's own build files cooperating.");
        }
    }

    private static string Describe(ExecutionResult result) => result switch
    {
        { Loaded: false } => "PROJECT DID NOT LOAD — this probe measured nothing",
        { RanInOurProcess: true } => $"YES — in-process (pid {result.Pids}) · phases: {result.Phases}",
        { RanAtAll: true } => $"YES — but in a CHILD process (pid {result.Pids}) · phases: {result.Phases}",
        _ => "no execution observed",
    };

    private static void Header(string title)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 78));
        Console.WriteLine(title);
        Console.WriteLine(new string('=', 78));
    }

    private static string FindRepoRoot()
    {
        // `.git` is a FILE in a linked worktree and a directory in a primary checkout. Checking only
        // for the directory finds no root at all when run from a worktree, which is where this repo's
        // sessions are supposed to work.
        static bool IsRoot(DirectoryInfo d) =>
            Directory.Exists(Path.Combine(d.FullName, ".git")) ||
            File.Exists(Path.Combine(d.FullName, ".git"));

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !IsRoot(directory))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("could not locate the repository root");
    }
}

/// <summary>What the sentinel observed for one probe.</summary>
/// <remarks>
/// <paramref name="Loaded"/> exists to keep a failed load from reading as a clean result. Without
/// it, "the project did not open" and "the project opened and nothing executed" are the same value —
/// a silent sentinel — and the second is a security finding while the first is a broken instrument.
/// An earlier run of this spike reported "no execution observed" for three probes that had loaded
/// zero projects. That is defect class DC-009 in its purest form, so it is made unrepresentable
/// here rather than guarded against by remembering to check.
/// </remarks>
internal sealed record ExecutionResult(
    bool Loaded, bool RanAtAll, bool RanInOurProcess, string Phases, string Pids)
{
    /// <summary>The project never opened. Says nothing whatsoever about generator execution.</summary>
    public static readonly ExecutionResult LoadFailed = new(false, false, false, "-", "-");

    /// <summary>The project opened and the sentinel stayed silent. A real observation.</summary>
    public static readonly ExecutionResult Silent = new(true, false, false, "-", "-");
}
