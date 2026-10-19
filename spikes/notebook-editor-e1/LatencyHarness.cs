using System.Diagnostics;
using System.Text.Json;

namespace NotebookEditorE1Spike;

/// <summary>One position's typing run, measured two ways.</summary>
/// <param name="PageMs">Per keystroke: the keydown event's own timestamp to the first task after the next frame (page clock).</param>
/// <param name="HostMs">Per keystroke: SendInput on the host to the host's receipt of that keystroke's paint stamp (an upper bound: it adds the page→host message hop).</param>
/// <param name="EventTimingMs">The browser's Event Timing entries (duration ≥ 16 ms only — the API's floor).</param>
/// <param name="DocDelta">How many characters the document grew — the non-vacuity guard (every key must have edited).</param>
internal sealed record LatencyRun(string Label, int Keys, double[] PageMs, double[] HostMs, double[] EventTimingMs, int DocDelta);

/// <summary>
/// THE SHIPPED HARNESS (ADR-0043 NFR-02; reused by WL-2). Host half: real keystrokes through
/// SendInput at a fixed cadence into the focused editor, joined by sequence number to the page half
/// (<c>page/latency-harness.mjs</c>), which stamps each keydown and the first task after the frame
/// that painted it.
/// </summary>
/// <remarks>
/// To reuse: mount the page half on the EditorView (<c>installLatencyHarness(view, post)</c>), expose
/// <c>start/stop/count</c>, and call <see cref="RunAsync"/> with the caret placed where the
/// measurement belongs. The guard callback is mandatory: input is never sent unless the harness
/// window is the foreground window, so a lost focus fails the run instead of typing into another app.
/// </remarks>
internal static class LatencyHarness
{
    public static async Task<LatencyRun> RunAsync(Page page, string label, int keys, TimeSpan cadence, Func<bool> foreground)
    {
        var before = (await page.EvalAsync("nb.docLength()")).GetInt32();
        await page.EvalAsync("nb.latency.start()");
        page.Posted.Clear();
        var sent = new long[keys];
        for (var i = 0; i < keys; i++)
        {
            if (!foreground())
            {
                throw new InvalidOperationException($"{label}: the harness window lost the foreground at key {i}; no further input sent");
            }

            sent[i] = Stopwatch.GetTimestamp();
            Native.Key(Native.VkFor((char)('a' + (i % 26))));
            await Task.Delay(cadence);
        }

        await page.WaitAsync($"nb.latency.count() >= {keys}", TimeSpan.FromSeconds(10));
        await Task.Delay(200);
        var result = await page.EvalAsync("nb.latency.stop()");
        var after = (await page.EvalAsync("nb.docLength()")).GetInt32();

        var pageMs = result.GetProperty("samples").EnumerateArray().Select(e => e.GetDouble()).ToArray();
        var eventTiming = result.GetProperty("eventTiming").EnumerateArray().Select(e => e.GetDouble()).ToArray();
        var hostMs = new List<double>();
        foreach (var (ticks, json) in page.Posted)
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("k", out var k) && k.GetString() == "painted")
            {
                var seq = doc.RootElement.GetProperty("seq").GetInt32();
                if (seq >= 0 && seq < keys)
                {
                    hostMs.Add((ticks - sent[seq]) * 1000.0 / Stopwatch.Frequency);
                }
            }
        }

        return new LatencyRun(label, keys, pageMs, [.. hostMs], eventTiming, after - before);
    }

    /// <summary>Nearest-rank percentile; NaN for an empty set — never a plausible number for no data.</summary>
    public static double Percentile(IReadOnlyCollection<double> values, double p)
    {
        if (values.Count == 0)
        {
            return double.NaN;
        }

        var sorted = values.Order().ToArray();
        var rank = (int)Math.Ceiling(p / 100.0 * sorted.Length);
        return sorted[Math.Clamp(rank - 1, 0, sorted.Length - 1)];
    }
}
