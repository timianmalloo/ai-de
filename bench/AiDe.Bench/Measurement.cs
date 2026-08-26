using System.Diagnostics;
using System.Globalization;

namespace AiDe.Bench;

/// <summary>A percentile summary over a sample set, reported with N so a reader can weigh it.</summary>
internal sealed record Summary(string Name, int Samples, double P50, double P95, double P99, double Min, double Max)
{
    /// <summary>
    /// Nearest-rank percentile. Chosen over interpolation because with N=30 an interpolated p99
    /// invents a value between two observations; nearest-rank always reports something measured.
    /// </summary>
    internal static Summary From(string name, IReadOnlyList<double> millis)
    {
        var sorted = millis.OrderBy(v => v).ToArray();
        return new Summary(name, sorted.Length,
            Percentile(sorted, 0.50), Percentile(sorted, 0.95), Percentile(sorted, 0.99),
            sorted[0], sorted[^1]);
    }

    private static double Percentile(double[] sorted, double q)
    {
        var rank = (int)Math.Ceiling(q * sorted.Length);
        return sorted[Math.Clamp(rank - 1, 0, sorted.Length - 1)];
    }

    internal string Format(double? budgetMs = null)
    {
        var verdict = budgetMs is null
            ? string.Empty
            : P95 <= budgetMs ? $"  PASS (budget p95 <{budgetMs}ms)" : $"  **FAIL** (budget p95 <{budgetMs}ms)";

        return string.Create(CultureInfo.InvariantCulture,
            $"{Name,-42} N={Samples,-4} p50={P50,9:F2}ms p95={P95,9:F2}ms p99={P99,9:F2}ms " +
            $"min={Min,8:F2} max={Max,9:F2}{verdict}");
    }

    internal bool Meets(double budgetMs) => P95 <= budgetMs;
}

internal static class Measure
{
    /// <summary>
    /// Runs <paramref name="action"/> <paramref name="samples"/> times after a warm-up, returning
    /// per-iteration wall-clock. Warm-up iterations are discarded and reported separately as the
    /// cold measurement — folding a cold JIT/page-cache run into a warm distribution would quietly
    /// inflate p99 and make the number mean nothing.
    /// </summary>
    internal static (Summary Warm, double ColdMs) Run(string name, int samples, Action action)
    {
        var coldWatch = Stopwatch.StartNew();
        action();
        coldWatch.Stop();
        var coldMs = coldWatch.Elapsed.TotalMilliseconds;

        // Additional un-recorded warm-up so the steady-state distribution is not polluted by tiering.
        for (var i = 0; i < 3; i++)
        {
            action();
        }

        var results = new List<double>(samples);
        for (var i = 0; i < samples; i++)
        {
            var watch = Stopwatch.StartNew();
            action();
            watch.Stop();
            results.Add(watch.Elapsed.TotalMilliseconds);
        }

        return (Summary.From(name, results), coldMs);
    }
}
