using System.Diagnostics;
using System.Runtime.Versioning;
using AiDe.Core.Facts;
using AiDe.Core.Ipc;
using AiDe.Core.Projections;
using AiDe.Core.Store;

namespace AiDe.Bench;

/// <summary>
/// <b>P2-PERF-02 — the cost of the daemon boundary.</b>
/// <para>Phase 2 moved the core's read surface across a named pipe. Every projection the shell
/// renders now pays serialisation, a pipe write, a read and deserialisation that it did not pay in
/// Phase 1 — and that cost had never been measured. This measures the SAME projection twice against
/// ONE store, in process and over a real pipe, so the difference is the boundary and nothing
/// else.</para>
/// <para>The budget is not invented here. P1-PERF gates `describe` at 100 ms and `impact` at 250 ms
/// against the user-facing behaviour; the boundary is a tax on those, so what matters is the
/// delta and whether the total still fits. Both are reported.</para>
/// </summary>
[SupportedOSPlatform("windows")]
internal static class P2Perf
{
    private const int Samples = 30;
    private const double DescribeBudgetMs = 100;
    private const double ImpactBudgetMs = 250;

    internal static async Task<int> RunAsync()
    {
        var failures = new List<string>();
        var dir = Path.Combine(Path.GetTempPath(), "aide-bench-p2", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var dbPath = Path.Combine(dir, "p2.db");

        Console.WriteLine("P2-PERF — AI-DE Phase-2 performance gate");
        Console.WriteLine(new string('=', 110));
        Console.WriteLine($"corpus shape    : {Corpus.TotalEdges:N0} edges over ~{Corpus.DistinctNodes:N0} nodes");
        Console.WriteLine($"samples         : {Samples} per measurement, warm; cold reported separately");
        Console.WriteLine($"build           : {(IsDebug() ? "**DEBUG — not a valid measurement**" : "Release")}");
        Console.WriteLine(new string('=', 110));
        Console.WriteLine();

        using (var seed = WorkspaceStore.Open(dbPath))
        {
            var assertions = Corpus.Build("bench", Corpus.TotalEdges);
            using var writer = seed.BeginWrite();
            // The store refuses a snapshot for a generation nobody asked for — the write-ahead
            // half of the complete-snapshot invariant. Declare the intent first.
            writer.DesireScopeGeneration("bench", 1, Corpus.Revision);
            writer.CommitSnapshot("bench", 1, Corpus.Revision, assertions, complete: true);
        }

        using var store = WorkspaceStore.Open(dbPath);
        var projections = new ProjectionService(store);
        var hub = Corpus.HotNode;
        const int MaxNeighbors = 50;
        const int MaxNodes = 200;
        const int MaxEdges = 400;

        // ------------------------------------------------------------------ in process (the floor)
        Console.WriteLine("P2-PERF-02  Read surface: in process versus across the daemon boundary");
        Console.WriteLine();

        var (inProcDescribe, inProcDescribeCold) =
            Measure.Run("describe (in process)", Samples, () => _ = projections.Describe(hub, MaxNeighbors));
        var (inProcImpact, inProcImpactCold) =
            Measure.Run("impact (in process)", Samples, () => _ = projections.Impact(hub, MaxNodes, MaxEdges));

        Console.WriteLine("  " + inProcDescribe.Format(DescribeBudgetMs));
        Console.WriteLine("  " + inProcImpact.Format(ImpactBudgetMs));
        Console.WriteLine();

        // ------------------------------------------------------------------ over a real pipe
        var pipeName = "aide-bench-" + Guid.NewGuid().ToString("N")[..12];
        var endpoint = new DaemonEndpoint(pipeName, new CapabilityRegistry(), _ => store.CoreEpoch);
        DaemonOperations.Register(endpoint, () => store.CoreEpoch);
        WorkspaceOperations.Register(endpoint, projections);

        var server = new IpcServer(pipeName, endpoint, new IpcServerOptions(StartupGrace: TimeSpan.FromSeconds(60)));
        using var life = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var running = server.RunAsync(life.Token);

        Summary pipeDescribe, pipeImpact;
        double pipeDescribeCold, pipeImpactCold;
        try
        {
            await using var client = await WorkspaceClient.ConnectAsync(
                pipeName, TimeSpan.FromSeconds(30), CancellationToken.None);

            // Blocking on the async call inside the harness keeps ONE measurement shape across both
            // paths. Measuring the in-process call synchronously and the pipe call asynchronously
            // would put the await machinery in only one of the two numbers being compared.
            (pipeDescribe, pipeDescribeCold) = Measure.Run(
                "describe (over pipe)", Samples,
                () => client.DescribeAsync(hub, MaxNeighbors, CancellationToken.None).GetAwaiter().GetResult());
            (pipeImpact, pipeImpactCold) = Measure.Run(
                "impact (over pipe)", Samples,
                () => client.ImpactAsync(hub, MaxNodes, MaxEdges, CancellationToken.None).GetAwaiter().GetResult());
        }
        finally
        {
            await life.CancelAsync();
            try { await running; } catch (OperationCanceledException) { }
        }

        Console.WriteLine("  " + pipeDescribe.Format(DescribeBudgetMs));
        Console.WriteLine("  " + pipeImpact.Format(ImpactBudgetMs));
        Console.WriteLine();

        // ------------------------------------------------------------------ the boundary's own cost
        Console.WriteLine("  The boundary tax (p95, over-pipe minus in-process):");
        Report("describe", inProcDescribe, pipeDescribe, DescribeBudgetMs);
        Report("impact  ", inProcImpact, pipeImpact, ImpactBudgetMs);
        Console.WriteLine();
        Console.WriteLine($"  cold first call over the pipe: describe {pipeDescribeCold:F2}ms, impact {pipeImpactCold:F2}ms");
        Console.WriteLine($"  (in process: describe {inProcDescribeCold:F2}ms, impact {inProcImpactCold:F2}ms)");
        Console.WriteLine();

        if (!pipeDescribe.Meets(DescribeBudgetMs))
            failures.Add($"describe over the pipe p95 {pipeDescribe.P95:F2}ms exceeds the {DescribeBudgetMs}ms budget");
        if (!pipeImpact.Meets(ImpactBudgetMs))
            failures.Add($"impact over the pipe p95 {pipeImpact.P95:F2}ms exceeds the {ImpactBudgetMs}ms budget");

        Console.WriteLine(new string('=', 110));
        Console.WriteLine(failures.Count == 0
            ? "P2-PERF-02: PASS — the daemon boundary keeps both reads inside the Phase-1 budgets."
            : string.Empty);
        foreach (var f in failures) Console.WriteLine($"**FAIL** {f}");
        Console.WriteLine();

        var throughputOk = TerminalThroughput.Run();

        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var settlementOk = ScopeSettlement.Run(repoRoot);

        Console.WriteLine(new string('=', 110));
        var ok = failures.Count == 0 && throughputOk && settlementOk;
        Console.WriteLine(ok
            ? "P2-PERF: PASS — boundary, terminal throughput and scope settlement all inside budget."
            : "P2-PERF: FAIL — see the cases marked **FAIL** above.");
        return ok ? 0 : 1;

        static void Report(string name, Summary inProc, Summary pipe, double budget)
        {
            var delta = pipe.P95 - inProc.P95;
            var factor = inProc.P95 > 0 ? pipe.P95 / inProc.P95 : double.NaN;
            var headroom = budget - pipe.P95;
            Console.WriteLine(
                $"    {name}  in-process {inProc.P95,8:F2}ms -> over-pipe {pipe.P95,8:F2}ms   " +
                $"tax {delta,7:F2}ms ({factor,5:F1}x)   headroom to budget {headroom,8:F2}ms");
        }
    }

    private static bool IsDebug()
    {
#if DEBUG
        return true;
#else
        return false;
#endif
    }
}
