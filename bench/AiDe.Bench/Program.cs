using System.Diagnostics;
using System.Globalization;
using AiDe.Bench;
using AiDe.Core;
using AiDe.Core.Facts;
using AiDe.Core.Store;
using Microsoft.Data.Sqlite;

// P1-PERF-01..04 — the Phase-1 performance gate.
//
// This harness measures; it does not defend. Where a budget is missed the number is printed with
// **FAIL** and the run exits non-zero, because the point of the gate is to convert the
// architecture's Inferred targets into measured ones — including when the measurement is bad news.

// P2-PERF runs as a separate gate: `dotnet run --project bench/AiDe.Bench -c Release -- p2`.
// Kept behind an argument rather than appended to this run, because the Phase-1 numbers are a
// committed baseline and folding a second workload into the same process would change them.
if (args.Length > 0 && args[0].Equals("p2", StringComparison.OrdinalIgnoreCase))
{
    // The daemon boundary is named pipes with an owner-SID ACL, so the whole gate is Windows-only.
    // Refusing loudly beats measuring nothing and reporting a pass.
    if (!OperatingSystem.IsWindows())
    {
        Console.WriteLine("P2-PERF: this gate measures the named-pipe daemon boundary and requires Windows.");
        return 2;
    }

    return await P2Perf.RunAsync();
}

const int Samples = 30;                 // the architecture's stated minimum
const double RefreshBudgetMs = 500;
const double DescribeBudgetMs = 100;
const double ImpactBudgetMs = 250;

var workingDirectory = Path.Combine(Path.GetTempPath(), "aide-bench", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(workingDirectory);
var databasePath = Path.Combine(workingDirectory, "bench.db");
var failures = new List<string>();

Console.WriteLine("P1-PERF — AI-DE Phase-1 performance gate");
Console.WriteLine(new string('=', 110));
Console.WriteLine($"corpus revision : {Corpus.Revision}");
Console.WriteLine($"corpus shape    : {Corpus.TotalEdges:N0} edges over ~{Corpus.DistinctNodes:N0} nodes, 20 hubs, 500-deep chain");
Console.WriteLine($"refresh scope   : {Corpus.RefreshScopeAssertions:N0} assertions");
Console.WriteLine($"samples         : {Samples} per measurement, warm; cold reported separately");
Console.WriteLine($"runtime         : {Environment.Version}, {(Debugger.IsAttached ? "DEBUGGER ATTACHED" : "no debugger")}");
Console.WriteLine($"build           : {(IsDebugBuild() ? "**DEBUG — not a valid measurement**" : "Release")}");
Console.WriteLine(new string('=', 110));
Console.WriteLine();

// ---------------------------------------------------------------- P1-PERF-01: refresh
Console.WriteLine("P1-PERF-01  Scope refresh (commit one complete snapshot)");

var refreshAssertions = Corpus.Build("refresh-scope", Corpus.RefreshScopeAssertions);

// Each sample commits into its OWN store. The first version of this harness reused one store, so
// sample 30 was inserting the 30th batch into a 300,000-row table and the "refresh" number was
// really an append-only growth curve. Independent samples measure what the budget actually gates:
// the cost of one scope refresh on a representative store.
var refreshRunId = 0;
List<double> RefreshSamples(int count, int priorGenerations)
{
    var runId = Interlocked.Increment(ref refreshRunId);
    var timings = new List<double>(count);
    for (var sample = 0; sample < count; sample++)
    {
        var path = Path.Combine(workingDirectory, $"refresh-r{runId}-{priorGenerations}-{sample}.db");
        using var store = WorkspaceStore.Open(path);
        var generation = 0L;

        void Commit()
        {
            generation++;
            var revision = $"{Corpus.Revision}-g{generation}";
            // Re-key to the new revision so each commit does the full insert work rather than
            // short-circuiting on the unique natural key.
            var batch = refreshAssertions
                .Select(a => new EvidenceAssertion(
                    a.ScopeId, revision, a.Subject, a.Predicate, a.Object, a.Origin, a.Status, a.Provenance))
                .ToList();

            using var writer = store.BeginWrite();
            writer.DesireScopeGeneration("refresh-scope", generation, revision);
            writer.CommitSnapshot("refresh-scope", generation, revision, batch, complete: true);
            writer.Commit();
        }

        for (var prior = 0; prior < priorGenerations; prior++)
        {
            Commit();
        }

        var watch = Stopwatch.StartNew();
        Commit();
        watch.Stop();
        timings.Add(watch.Elapsed.TotalMilliseconds);

        SqliteConnection.ClearAllPools();
    }

    return timings;
}

var refreshSummary = Summary.From("refresh 10k assertions (fresh store)", RefreshSamples(Samples, 0));
Console.WriteLine("  " + refreshSummary.Format(RefreshBudgetMs));
if (!refreshSummary.Meets(RefreshBudgetMs))
{
    failures.Add($"P1-PERF-01 refresh p95 {refreshSummary.P95:F1}ms > {RefreshBudgetMs}ms");
}

// Append-only means the fact table grows with every re-extraction. That is a real product
// characteristic, so it is measured rather than assumed — a refresh budget that only holds on a
// pristine store would be a budget that never holds in use.
Console.WriteLine();
Console.WriteLine("  Append-only growth: cost of ONE refresh after N prior generations of the same scope");
foreach (var prior in new[] { 0, 5, 10, 20 })
{
    var growth = Summary.From($"  after {prior,2} prior generation(s)", RefreshSamples(5, prior));
    Console.WriteLine("  " + growth.Format());
}

Console.WriteLine();

// ---------------------------------------------------------------- compaction: does it restore the budget?
// The growth curve above is the defect P1-PERF found. This measures whether the compaction policy
// actually fixes it, rather than assuming a smaller table must be faster.
Console.WriteLine("COMPACTION  refresh cost after 20 generations, before and after compacting");
{
    var path = Path.Combine(workingDirectory, "compaction.db");
    var generation = 0L;

    void Commit(WorkspaceStore store)
    {
        generation++;
        var revision = $"{Corpus.Revision}-c{generation}";
        var batch = refreshAssertions
            .Select(a => new EvidenceAssertion(
                a.ScopeId, revision, a.Subject, a.Predicate, a.Object, a.Origin, a.Status, a.Provenance))
            .ToList();
        using var writer = store.BeginWrite();
        writer.DesireScopeGeneration("refresh-scope", generation, revision);
        writer.CommitSnapshot("refresh-scope", generation, revision, batch, complete: true);
        writer.Commit();
    }

    double MeasureOne(WorkspaceStore store)
    {
        var watch = Stopwatch.StartNew();
        Commit(store);
        watch.Stop();
        return watch.Elapsed.TotalMilliseconds;
    }

    double beforeMs;
    using (var store = WorkspaceStore.Open(path))
    {
        for (var i = 0; i < 20; i++) { Commit(store); }
        beforeMs = MeasureOne(store);
    }

    SqliteConnection.ClearAllPools();
    var compaction = new StoreCompactor(path).Compact();

    double afterMs;
    using (var store = WorkspaceStore.Open(path))
    {
        afterMs = MeasureOne(store);
    }

    Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
        $"  refresh after 20 generations        {beforeMs,9:F2}ms  {(beforeMs <= RefreshBudgetMs ? "within" : "**OVER**")} budget"));
    Console.WriteLine($"  {compaction.Summary}");
    Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
        $"  refresh after compaction            {afterMs,9:F2}ms  {(afterMs <= RefreshBudgetMs ? "within" : "**OVER**")} budget"));

    if (afterMs > RefreshBudgetMs)
    {
        failures.Add($"COMPACTION refresh still {afterMs:F0}ms > {RefreshBudgetMs}ms after compacting");
    }

    SqliteConnection.ClearAllPools();
}

Console.WriteLine();

// ---------------------------------------------------------------- corpus load for query benchmarks
Console.WriteLine($"Loading the {Corpus.TotalEdges:N0}-edge query corpus…");
var queryDatabase = Path.Combine(workingDirectory, "query.db");
var loadWatch = Stopwatch.StartNew();

using (var store = WorkspaceStore.Open(queryDatabase))
{
    var assertions = Corpus.Build("bench", Corpus.TotalEdges);
    using var writer = store.BeginWrite();
    writer.DesireScopeGeneration("bench", 1, Corpus.Revision);
    writer.CommitSnapshot("bench", 1, Corpus.Revision, assertions, complete: true);
    writer.Commit();
}

loadWatch.Stop();
Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
    $"  loaded in {loadWatch.Elapsed.TotalSeconds:F1}s · db {new FileInfo(queryDatabase).Length / 1024.0 / 1024.0:F1} MiB"));
Console.WriteLine();

using var queryStore = WorkspaceStore.Open(queryDatabase);
var core = new AiDe.Core.Projections.ProjectionService(queryStore);

// ---------------------------------------------------------------- P1-PERF-02: describe
Console.WriteLine("P1-PERF-02  describe (bounded neighbourhood of the hottest node)");
var (describeWarm, describeCold) = Measure.Run(
    $"describe {Corpus.HotNode} maxNeighbors=50", Samples, () => core.Describe(Corpus.HotNode, 50));
Console.WriteLine("  " + describeWarm.Format(DescribeBudgetMs));
Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"  cold                                       {describeCold,9:F2}ms"));
if (!describeWarm.Meets(DescribeBudgetMs))
{
    failures.Add($"P1-PERF-02 describe p95 {describeWarm.P95:F1}ms > {DescribeBudgetMs}ms");
}

Console.WriteLine();

// ---------------------------------------------------------------- P1-PERF-03: impact
Console.WriteLine("P1-PERF-03  impact (bounded dependent-neighbourhood walk)");
var (impactHub, impactHubCold) = Measure.Run(
    $"impact {Corpus.HotNode} 200 nodes/500 edges", Samples, () => core.Impact(Corpus.HotNode, 200, 500));
Console.WriteLine("  " + impactHub.Format(ImpactBudgetMs));
Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"  cold                                       {impactHubCold,9:F2}ms"));
if (!impactHub.Meets(ImpactBudgetMs))
{
    failures.Add($"P1-PERF-03 impact(hub) p95 {impactHub.P95:F1}ms > {ImpactBudgetMs}ms");
}

var (impactChain, _) = Measure.Run(
    $"impact {Corpus.ChainHead} (deep chain)", Samples, () => core.Impact(Corpus.ChainHead, 200, 500));
Console.WriteLine("  " + impactChain.Format(ImpactBudgetMs));
if (!impactChain.Meets(ImpactBudgetMs))
{
    failures.Add($"P1-PERF-03 impact(chain) p95 {impactChain.P95:F1}ms > {ImpactBudgetMs}ms");
}

var (findWarm, _) = Measure.Run("find term=Hub maxResults=50", Samples, () => core.Find("Hub", 50));
Console.WriteLine("  " + findWarm.Format(DescribeBudgetMs));
if (!findWarm.Meets(DescribeBudgetMs))
{
    failures.Add($"P1-PERF-03 find p95 {findWarm.P95:F1}ms > {DescribeBudgetMs}ms");
}

var (knowledgeWarm, _) = Measure.Run("knowledge (no matches in this corpus)", Samples,
    () => core.Knowledge(new AiDe.Core.Projections.KnowledgeQuery(null, null, 50)));
Console.WriteLine("  " + knowledgeWarm.Format(DescribeBudgetMs));
if (!knowledgeWarm.Meets(DescribeBudgetMs))
{
    failures.Add($"P1-PERF-03 knowledge p95 {knowledgeWarm.P95:F1}ms > {DescribeBudgetMs}ms");
}

Console.WriteLine();

// ---------------------------------------------------------------- P1-PERF-04: query plans
// The budget "no full scan in the approved EXPLAIN QUERY PLAN" is a SEPARATE oracle from latency:
// a query can be fast on this corpus and still be a scan that degrades linearly on a larger one.
Console.WriteLine("P1-PERF-04  EXPLAIN QUERY PLAN (no full fact-table scan on a bounded read)");

var plans = new (string Label, string Sql)[]
{
    ("describe/impact by subject", """
        SELECT subject, predicate, object FROM evidence_assertion_fact
        WHERE scope_id = 'bench' AND generation = 1 AND subject = 'Hub00';
        """),
    ("describe by object", """
        SELECT subject, predicate, object FROM evidence_assertion_fact
        WHERE scope_id = 'bench' AND generation = 1 AND object = 'Node00042';
        """),
    ("latest committed snapshot", """
        SELECT generation FROM scope_snapshot_committed_fact
        WHERE scope_id = 'bench' AND complete = 1 ORDER BY generation DESC LIMIT 1;
        """),
    ("current assertions (join to latest)", """
        SELECT a.subject FROM evidence_assertion_fact a
        JOIN (SELECT scope_id, max(generation) AS generation
              FROM scope_snapshot_committed_fact WHERE complete = 1 GROUP BY scope_id) latest
          ON latest.scope_id = a.scope_id AND latest.generation = a.generation;
        """),
};

using (var planConnection = new SqliteConnection(
    new SqliteConnectionStringBuilder { DataSource = queryDatabase, Pooling = false }.ToString()))
{
    planConnection.Open();
    foreach (var (label, sql) in plans)
    {
        using var command = planConnection.CreateCommand();
        command.CommandText = "EXPLAIN QUERY PLAN " + sql;
        using var reader = command.ExecuteReader();
        var lines = new List<string>();
        while (reader.Read())
        {
            lines.Add(reader.GetString(3));
        }

        var scans = lines.Where(l => l.Contains("SCAN", StringComparison.Ordinal)).ToList();
        var scansFactTable = scans.Any(l => l.Contains("evidence_assertion_fact", StringComparison.Ordinal));
        Console.WriteLine($"  {label,-40} {(scansFactTable ? "**FULL SCAN**" : "indexed")}");
        foreach (var line in lines)
        {
            Console.WriteLine($"      {line}");
        }

        if (scansFactTable)
        {
            failures.Add($"P1-PERF-04 '{label}' performs a full scan of evidence_assertion_fact");
        }
    }
}

Console.WriteLine();

// ---------------------------------------------------------------- restore/replay vs the 15-min RTO
Console.WriteLine("RTO  restore + replay of the corpus (architecture states 15 minutes)");
var restoreWatch = Stopwatch.StartNew();
var restorePath = Path.Combine(workingDirectory, "restored.db");
File.Copy(queryDatabase, restorePath, overwrite: true);
using (var restored = WorkspaceStore.Open(restorePath))
{
    var projections = new AiDe.Core.Projections.ProjectionService(restored);
    var rebuilt = projections.DeriveClaimCurrent();
    restoreWatch.Stop();
    Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
        $"  restore + full claim rebuild ({rebuilt.Count:N0} claims)  {restoreWatch.Elapsed.TotalSeconds,8:F2}s   " +
        $"{(restoreWatch.Elapsed.TotalMinutes <= 15 ? "PASS (<15 min)" : "**FAIL**")}"));
    if (restoreWatch.Elapsed.TotalMinutes > 15)
    {
        failures.Add("RTO restore/replay exceeded 15 minutes");
    }
}

Console.WriteLine();
Console.WriteLine(new string('=', 110));
if (failures.Count == 0)
{
    Console.WriteLine("RESULT: all measured budgets met.");
}
else
{
    Console.WriteLine($"RESULT: {failures.Count} budget(s) MISSED —");
    foreach (var failure in failures)
    {
        Console.WriteLine($"  - {failure}");
    }
}

Console.WriteLine(new string('=', 110));

SqliteConnection.ClearAllPools();
try
{
    Directory.Delete(workingDirectory, recursive: true);
}
catch (IOException)
{
    Console.WriteLine($"(left working directory in place: {workingDirectory})");
}

return failures.Count == 0 ? 0 : 1;

static bool IsDebugBuild()
{
#if DEBUG
    return true;
#else
    return false;
#endif
}
