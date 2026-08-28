using System.Diagnostics;
using AiDe.Core.Extraction;
using AiDe.Core.Store;

namespace AiDe.Bench;

/// <summary>
/// <b>P2-PERF-01 — scope settlement.</b>
/// <para>One scope, from project file to committed snapshot. This is the first and only test of the
/// scope-per-(project, target-framework) decision, whose <c>simplify:</c> ceiling names this
/// measurement as its own upgrade trigger: <i>p95 scope settlement &gt; 10 s on the approved
/// corpus</i>.</para>
/// </summary>
/// <remarks>
/// <b>The corpus is this repository.</b> Not a synthetic project: the point of the budget is whether
/// a real solution settles, and a generated fixture would be tuned — however unintentionally — to
/// the extractor that reads it. <c>AiDe.Core</c> is the largest scope the product currently has.
/// </remarks>
internal static class ScopeSettlement
{
    private const double BudgetSeconds = 10.0;
    private const int Samples = 5;

    internal static bool Run(string repoRoot)
    {
        Console.WriteLine("P2-PERF-01  Scope settlement — project file to committed snapshot");
        Console.WriteLine();

        var projects = new[]
        {
            ("AiDe.Core", Path.Combine(repoRoot, "src", "AiDe.Core", "AiDe.Core.csproj")),
            ("AiDe.App", Path.Combine(repoRoot, "src", "AiDe.App", "AiDe.App.csproj")),
        };

        var failures = new List<string>();

        foreach (var (name, path) in projects)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine($"  {name,-12} SKIPPED — not found at {path}");
                continue;
            }

            var extractor = new CSharpExtractor();
            var tfm = extractor.TargetFrameworks(path).FirstOrDefault() ?? "net10.0";
            var scopeId = $"csharp:{name}:{tfm}";

            var extractMs = new List<double>(Samples);
            var settleMs = new List<double>(Samples);
            var assertions = 0;
            var disclosures = new List<string>();

            for (var i = 0; i < Samples; i++)
            {
                var dir = Path.Combine(Path.GetTempPath(), "aide-settle", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(dir);

                try
                {
                    var whole = Stopwatch.StartNew();

                    // The 60-second scope budget the design specifies, applied here so the
                    // measurement runs under the same cancellation the product will use.
                    using var budget = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                    var extractWatch = Stopwatch.StartNew();
                    var result = extractor
                        .ExtractAsync(new ExtractionRequest(scopeId, path, $"rev-{i}", i + 1), budget.Token)
                        .GetAwaiter().GetResult();
                    extractWatch.Stop();

                    if (!result.Complete)
                    {
                        failures.Add($"{name}: extraction reported incomplete — " +
                                     string.Join("; ", result.Diagnostics.Take(2).Select(d => d.ErrorCode)));
                        break;
                    }

                    using (var store = WorkspaceStore.Open(Path.Combine(dir, "settle.db")))
                    using (var writer = store.BeginWrite())
                    {
                        writer.DesireScopeGeneration(scopeId, i + 1, $"rev-{i}");
                        writer.CommitSnapshot(scopeId, i + 1, $"rev-{i}", result.Assertions, complete: true);
                        writer.Commit();
                    }

                    whole.Stop();

                    extractMs.Add(extractWatch.Elapsed.TotalMilliseconds);
                    settleMs.Add(whole.Elapsed.TotalMilliseconds);
                    assertions = result.Assertions.Count;
                    disclosures = result.Assertions
                        .Where(a => a.Predicate == CSharpExtractor.DisclosurePredicate)
                        .Select(a => a.Object).Distinct(StringComparer.Ordinal).ToList();
                }
                finally
                {
                    Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                    try { Directory.Delete(dir, recursive: true); } catch (IOException) { }
                }
            }

            if (settleMs.Count == 0) continue;

            var extract = Summary.From($"{name} extract", extractMs);
            var settle = Summary.From($"{name} settle", settleMs);

            Console.WriteLine($"  {name}  ({tfm}, {assertions:N0} assertions)");
            Console.WriteLine("    " + extract.Format());
            Console.WriteLine("    " + settle.Format(BudgetSeconds * 1000));
            Console.WriteLine($"    store commit share : {settle.P95 - extract.P95:F0} ms of {settle.P95:F0} ms p95");
            Console.WriteLine($"    disclosures        : {(disclosures.Count == 0 ? "(none)" : string.Join(", ", disclosures))}");

            if (!settle.Meets(BudgetSeconds * 1000))
            {
                failures.Add($"{name}: settlement p95 {settle.P95 / 1000:F2}s exceeds the {BudgetSeconds:F0}s budget " +
                             "— the simplify: ceiling on scope-per-project has been reached");
            }

            Console.WriteLine();
        }

        foreach (var f in failures) Console.WriteLine($"  **FAIL** {f}");
        if (failures.Count == 0)
        {
            Console.WriteLine("  P2-PERF-01: PASS — every scope settles inside its 10 s budget.");
        }

        Console.WriteLine();
        return failures.Count == 0;
    }
}
