using System.Diagnostics;
using System.Text;
using AiDe.Core.Terminal;

namespace AiDe.Bench;

/// <summary>
/// <b>P2-PERF-03 — sustained terminal throughput.</b>
/// <para>The architecture budgets for a process emitting <b>1 MiB/s</b> of output. S3 measured the VT
/// scanner at 2361x that rate over a short burst, and the shipped renderer draws a 200x50 screen at
/// 5.50 ms p95 — but neither number was taken while the pipeline was actually <i>held</i> at the
/// budgeted rate. A burst measures the fast path; a sustained drive measures what accumulates.</para>
/// </summary>
/// <remarks>
/// <para><b>What this measures and what it deliberately does not.</b> The parse-and-screen-model half
/// runs here, in Core, with no WPF. The draw half cannot: it needs a dispatcher and a real visual
/// tree, and it is already gated by <c>AFullScreenRedraw_StaysInsideTheFrameBudget</c> in the App
/// tests. Splitting them is what lets the expensive half be measured at all — but it means this
/// number is <b>not</b> an end-to-end frame time, and must not be quoted as one.</para>
///
/// <para><b>The corpus is not random bytes.</b> A stream of printable ASCII would understate the
/// work: real output is mostly text with regular SGR colour changes, cursor moves and line erases,
/// and those are the paths with state transitions in them. Random bytes would overstate it in a
/// different direction, spending the whole budget in the escape-sequence error path.</para>
/// </remarks>
internal static class TerminalThroughput
{
    private const double BudgetBytesPerSecond = 1024 * 1024;
    private const int Columns = 200;
    private const int Rows = 50;

    /// <summary>How long to hold the rate. Long enough for growth to show, short enough to gate on.</summary>
    private static readonly TimeSpan Duration = TimeSpan.FromSeconds(10);

    internal static bool Run()
    {
        Console.WriteLine("P2-PERF-03  Sustained terminal output at the 1 MiB/s budget");
        Console.WriteLine();

        var chunk = BuildChunk();
        var screen = new TerminalScreen(Columns, Rows);
        var parser = new VtParser(screen);

        // ---------------------------------------------------------------- unthrottled ceiling
        // What the pipeline can do when nothing holds it back — the comparable to S3's burst.
        var ceilingWatch = Stopwatch.StartNew();
        long ceilingBytes = 0;
        while (ceilingWatch.Elapsed < TimeSpan.FromSeconds(2))
        {
            parser.Consume(chunk);
            ceilingBytes += chunk.Length;
        }

        ceilingWatch.Stop();
        var ceiling = ceilingBytes / ceilingWatch.Elapsed.TotalSeconds;
        Console.WriteLine($"  unthrottled ceiling : {ceiling / (1024 * 1024),8:F1} MiB/s  " +
                          $"({ceiling / BudgetBytesPerSecond,6:F0}x the 1 MiB/s budget)");

        // ---------------------------------------------------------------- held at the budget
        // The question a burst cannot answer: does per-chunk cost DRIFT while the stream is held at
        // the budgeted rate? A parser that allocates per escape sequence looks fine for a moment and
        // degrades as the heap fills — which is precisely the failure a burst measurement hides.
        screen = new TerminalScreen(Columns, Rows);
        parser = new VtParser(screen);

        var latencies = new List<double>(4096);
        var sustained = Stopwatch.StartNew();
        long delivered = 0;
        var firstQuarter = new List<double>(1024);
        var lastQuarter = new List<double>(1024);

        while (sustained.Elapsed < Duration)
        {
            var due = sustained.Elapsed.TotalSeconds * BudgetBytesPerSecond;
            if (delivered >= due)
            {
                // Ahead of the budgeted rate: yield rather than spin, so the measurement reflects
                // the pipeline's cost and not this loop's.
                Thread.Sleep(1);
                continue;
            }

            var one = Stopwatch.GetTimestamp();
            parser.Consume(chunk);
            var elapsedMs = Stopwatch.GetElapsedTime(one).TotalMilliseconds;

            delivered += chunk.Length;
            latencies.Add(elapsedMs);
            if (sustained.Elapsed < Duration / 4) firstQuarter.Add(elapsedMs);
            if (sustained.Elapsed > Duration * 0.75) lastQuarter.Add(elapsedMs);
        }

        sustained.Stop();

        var achieved = delivered / sustained.Elapsed.TotalSeconds;
        var summary = Summary.From("chunk parse", latencies);
        var drift = Median(lastQuarter) - Median(firstQuarter);

        Console.WriteLine($"  sustained rate      : {achieved / (1024 * 1024),8:F2} MiB/s over {sustained.Elapsed.TotalSeconds:F1}s " +
                          $"({delivered / (1024.0 * 1024),0:F1} MiB, {latencies.Count:N0} chunks of {chunk.Length / 1024.0:F1} KiB)");
        Console.WriteLine("  " + summary.Format());
        Console.WriteLine($"  per-chunk drift     : median {Median(firstQuarter):F4} ms (first quarter) -> " +
                          $"{Median(lastQuarter):F4} ms (last quarter), delta {drift:+0.0000;-0.0000;0.0000} ms");
        Console.WriteLine($"  final screen state  : cursor r{screen.CursorRow} c{screen.CursorColumn}, dirty={screen.IsDirty}");
        Console.WriteLine();

        var failures = new List<string>();

        // The rate must actually have been held. A run that fell behind measured a slow machine, not
        // a slow parser, and its latency distribution means nothing.
        if (achieved < BudgetBytesPerSecond * 0.95)
        {
            failures.Add($"the harness did not sustain the budget: {achieved / (1024 * 1024):F2} MiB/s of 1.00 MiB/s");
        }

        // Headroom, not a frame budget: the draw is gated separately. 16.67 ms of parse for a chunk
        // this size would leave nothing for it.
        if (summary.P95 > 16.67)
        {
            failures.Add($"chunk parse p95 {summary.P95:F2} ms leaves no room for the draw");
        }

        // The point of sustaining. A pipeline whose cost climbs while the rate is flat is
        // accumulating something, and it will not be visible in any burst measurement.
        if (Median(firstQuarter) > 0 && drift > Median(firstQuarter))
        {
            failures.Add($"per-chunk cost more than doubled across the run (+{drift:F4} ms) — something is accumulating");
        }

        foreach (var f in failures) Console.WriteLine($"  **FAIL** {f}");
        if (failures.Count == 0)
        {
            Console.WriteLine("  P2-PERF-03: PASS — 1 MiB/s held with no per-chunk growth.");
        }

        Console.WriteLine();
        return failures.Count == 0;
    }

    private static double Median(List<double> values)
    {
        if (values.Count == 0) return 0;
        var sorted = values.ToArray();
        Array.Sort(sorted);
        return sorted[sorted.Length / 2];
    }

    /// <summary>
    /// One chunk of plausible build output: mostly text, with the SGR/cursor/erase traffic a real
    /// tool emits. Built once and re-consumed, so the measurement is of parsing rather than of
    /// generating the corpus.
    /// </summary>
    private static byte[] BuildChunk()
    {
        var builder = new StringBuilder(64 * 1024);
        var random = new Random(20260828);

        string[] words =
        [
            "Determining", "projects", "to", "restore", "Restored", "AiDe.Core.csproj",
            "warning", "CS8618", "Non-nullable", "property", "must", "contain", "a", "non-null",
            "value", "when", "exiting", "constructor", "Build", "succeeded", "Elapsed",
        ];

        while (builder.Length < 64 * 1024)
        {
            var roll = random.Next(100);
            if (roll < 12)
            {
                // SGR: colour and attribute changes, the commonest escape in build output.
                builder.Append("[").Append(random.Next(30, 38)).Append(';').Append(random.Next(0, 2)).Append('m');
            }
            else if (roll < 16)
            {
                builder.Append("[").Append(random.Next(1, Rows)).Append(';').Append(random.Next(1, Columns)).Append('H');
            }
            else if (roll < 19)
            {
                builder.Append("[2K");
            }
            else if (roll < 22)
            {
                builder.Append("[0m\r\n");
            }
            else
            {
                builder.Append(words[random.Next(words.Length)]).Append(' ');
            }
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
    }
}
