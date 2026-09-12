using System.Diagnostics;
using System.Text.Json;
using AiDe.App.Workbench;
using AiDe.Core.AgentPlane;
using AiDe.Core.Dispatch;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// A terminal pane that ends writes a <c>terminal.stop</c> line beside the <c>terminal.start</c> it
/// wrote when it was built — on every end path the pane has (INV-0010).
/// </summary>
/// <remarks>
/// <para><b>The one-sided log.</b> <c>WorkbenchDiagnostics.TerminalStart</c> records the launch
/// decision, on the normal path, with no flag — and nothing recorded the end. An operator holding a
/// process list and a log that says "started, started, started" cannot tell a pane that closed
/// from one that leaked, and neither can the census that reads the log for a creation-time match.
/// The stop line is the other half of the pair: written at the TOP of <c>Dispose</c> (the attempt,
/// the idiom <c>SessionDisposalSignal</c> uses), carrying the surface id so the two lines join.</para>
///
/// <para><b>Three end paths, three reasons, one line each.</b> A closed tab disposes a live
/// shell (<c>killed</c>); a shell the user ends with <c>exit</c> completes its session while the
/// pane lives on (<c>child-exited</c>, with the code); the App's window close disposes the shell
/// with every pane still open (<c>owner-closing</c>, one line per live terminal). A pane whose
/// shell exited and is then closed writes ONE stop, or <i>starts − stops</i> undercounts.</para>
///
/// <para><b>Red first</b>: each fact failed until the pane wrote its line. Every assertion is
/// keyed by the fact's own surface id: the sink is process-wide, and other tests' fixture shells
/// end on their own schedule (measured: a <c>terminal-1</c> child-exited line landed inside a
/// fact and tripped an unkeyed <c>Assert.Single</c>).</para>
/// </remarks>
public sealed class TerminalSurfaceStopLineTests
{
    [Fact]
    public void ADisposedTerminalPane_WritesATerminalStopLineForItsSurface()
    {
        const string surfaceId = "terminal#inv0010";

        var lines = Sta.Run(() =>
        {
            using var capture = new SinkCapture();

            // The product's construction path, so the surface under test is the one the shell
            // builds — and its Dispose is the one a closed tab and (if anything did) a closed
            // window would call.
            var content = new SurfaceContentFactory(queries: null)
                .Create(new Surface(surfaceId, "terminal", "Terminal")) as IDisposable;

            content?.Dispose();

            return capture.Lines;
        });

        var records = lines.Select(l => JsonDocument.Parse(l).RootElement).ToList();

        Assert.Single(records, r => Evt(r) == "terminal.start" && Surface(r) == surfaceId);

        // THE OTHER HALF. Same surface id, so a reader can pair the two and a census can subtract.
        var stop = Assert.Single(records, r => Evt(r) == "terminal.stop" && Surface(r) == surfaceId);
        Assert.Equal(surfaceId, Surface(stop));

        // A live shell closed by its tab is a kill — ours, not the child's — and it has no exit
        // code of its own; the field is absent rather than a plausible 0.
        Assert.Equal("killed", Str(stop, "reason"));
        Assert.Equal(JsonValueKind.Null, stop.GetProperty("exitCode").ValueKind);
        Assert.True(stop.GetProperty("durationMs").GetDouble() >= 0);
    }

    /// <summary>
    /// The child's own end: the shell exits with a code of its own and the pane stays. The line
    /// says <c>child-exited</c> with THAT code — asserted non-zero, so a line that carries a
    /// default 0 cannot pass — and the runtime's stop for the same session agrees with it
    /// (E12); closing the pane afterwards adds no second stop.
    /// </summary>
    /// <remarks>
    /// The pane's shell is the static <see cref="TerminalSurface.CommandLine"/>; here it is a
    /// batch file that exits 4 at once, restored in <c>finally</c> (the App tests run serially).
    /// Deterministic on a console-less host too — the batch does not read its stdin.
    /// </remarks>
    [Fact]
    public void AShellThatExits_WritesChildExitedWithTheCode_AndTheLaterDisposeWritesNoSecondStop()
    {
        const string surfaceId = "terminal#inv0010-exit";
        var batch = Path.Combine(Path.GetTempPath(), $"aide-exit4-{Guid.NewGuid():N}.cmd");
        File.WriteAllText(batch, "@exit 4\r\n");

        var (lines, exit, sessionId, runtimeStops) = Sta.Run(() =>
        {
            using var capture = new SinkCapture();
            var runtime = new List<Activity>();
            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == TerminalHostingLedger.TerminalActivitySource,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStarted = a => { if (a.OperationName == TerminalHostingLedger.TerminalStopActivity) { lock (runtime) { runtime.Add(a); } } },
            };
            ActivitySource.AddActivityListener(listener);

            var previousShell = TerminalSurface.CommandLine;
            TerminalSurface.CommandLine = batch;
            TerminalSurface surface;
            try
            {
                surface = Assert.IsType<TerminalSurface>(
                    new SurfaceContentFactory(queries: null).Create(new Surface(surfaceId, "terminal", "Terminal")));
            }
            finally
            {
                TerminalSurface.CommandLine = previousShell;
            }

            SessionExit reported;
            try
            {
                using var waiting = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                reported = surface.Session!.WaitForExitAsync(waiting.Token).GetAwaiter().GetResult();

                var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(30);
                while (DateTimeOffset.UtcNow < deadline
                       && !capture.Lines.Any(l => l.Contains("\"evt\":\"terminal.stop\"", StringComparison.Ordinal)
                                                  && l.Contains(surfaceId, StringComparison.Ordinal)))
                {
                    Thread.Sleep(100);
                }
            }
            finally
            {
                // The tab closes AFTER the shell ended — the App's shape. One stop, not two.
                surface.Dispose();
            }

            List<Activity> stops;
            lock (runtime) { stops = [.. runtime]; }
            return (capture.Lines, reported, surface.Session!.SessionId, stops);
        }, 90);

        try { File.Delete(batch); } catch (IOException) { }

        Assert.False(exit.Killed, "the shell was killed rather than exiting on its own");
        Assert.Equal(4, exit.ExitCode);

        var records = lines.Select(l => JsonDocument.Parse(l).RootElement).ToList();

        var stop = Assert.Single(records, r => Evt(r) == "terminal.stop" && Surface(r) == surfaceId);
        Assert.Equal(surfaceId, Surface(stop));
        Assert.Equal("child-exited", Str(stop, "reason"));
        Assert.Equal(4, stop.GetProperty("exitCode").GetInt32());
        Assert.Equal(sessionId, Str(stop, "session"));

        // THE TWO SURFACES AGREE (E12): the runtime's one stop for this session carries the same
        // id and the same code the workbench line does, so a census joining the two subtracts
        // the same session once.
        var runtimeStop = Assert.Single(runtimeStops, a => Equals(a.GetTagItem("session.id"), sessionId));
        Assert.Equal(4, runtimeStop.GetTagItem("session.exit_code"));
        Assert.Equal("child-exited", runtimeStop.GetTagItem("session.end_reason"));
    }

    /// <summary>
    /// A pane whose shell never started still closes its pair: one <c>disposed</c> line, no
    /// session id and no exit code — <c>null</c>, not a plausible 0 (DC-137).
    /// </summary>
    [Fact]
    public void APaneWhoseStartFailed_WritesOneDisposedStop_WithNoSessionAndNoCode()
    {
        const string surfaceId = "terminal#inv0010-start-failed";

        var lines = Sta.Run(() =>
        {
            using var capture = new SinkCapture();

            var previousShell = TerminalSurface.CommandLine;
            TerminalSurface.CommandLine = $"aide-no-such-shell-{Guid.NewGuid():N}.exe";
            IDisposable? content;
            try
            {
                content = new SurfaceContentFactory(queries: null)
                    .Create(new Surface(surfaceId, "terminal", "Terminal")) as IDisposable;
            }
            finally
            {
                TerminalSurface.CommandLine = previousShell;
            }

            content?.Dispose();
            return capture.Lines;
        });

        var records = lines.Select(l => JsonDocument.Parse(l).RootElement).ToList();

        // The launch decision is recorded BEFORE the attempt and the failure AFTER it, on the
        // same surface id: two `terminal.start` lines, exactly one of them carrying `failure`. A
        // reader counting starts against stops counts the lines with no failure.
        var starts = records.Where(r => Evt(r) == "terminal.start" && Surface(r) == surfaceId).ToList();
        Assert.Equal(2, starts.Count);
        Assert.Single(starts, r => r.GetProperty("failure").ValueKind != JsonValueKind.Null);

        var stop = Assert.Single(records, r => Evt(r) == "terminal.stop" && Surface(r) == surfaceId);
        Assert.Equal(surfaceId, Surface(stop));
        Assert.Equal("disposed", Str(stop, "reason"));
        Assert.Equal(JsonValueKind.Null, stop.GetProperty("session").ValueKind);
        Assert.Equal(JsonValueKind.Null, stop.GetProperty("exitCode").ValueKind);
    }

    /// <summary>
    /// The App's close: <c>MainWindow.Closed</c> → <c>WorkbenchShell.Dispose</c>, which disposes no
    /// terminal surface (the process exit ends them — measured clean, INV-0010 path 1). The log
    /// still has to say each pane ended, and why, or the operator's last session reads as a leak.
    /// </summary>
    [Fact]
    public void TheShellsDispose_WritesOneOwnerClosingStopPerLiveTerminal()
    {
        var (lines, terminalIds) = Sta.Run(() =>
        {
            using var capture = new SinkCapture();

            var shell = new WorkbenchShell(queries: null);
            var ids = shell.Service.Current.AllStacks()
                .SelectMany(s => s.Surfaces)
                .Where(s => s.Kind == "terminal")
                .Select(s => s.SurfaceId)
                .ToList();
            Assert.NotEmpty(ids);

            shell.Dispose();

            // Not the composition root's job: the surfaces are still alive, and on this path the
            // process exit is what ends them. Ended here so the test host holds nothing.
            foreach (var id in ids)
            {
                (shell.Adapter.ContentFor(id) as IDisposable)?.Dispose();
            }

            return (capture.Lines, ids);
        }, 60);

        // Only THIS shell's panes: the sink is process-wide, and a fixture shell from an earlier
        // test class may write its own child-exited line while this fact runs.
        var records = lines.Select(l => JsonDocument.Parse(l).RootElement).ToList();
        var stops = records.Where(r => Evt(r) == "terminal.stop" && Surface(r) is { } id && terminalIds.Contains(id)).ToList();

        foreach (var id in terminalIds)
        {
            var stop = Assert.Single(stops, r => Surface(r) == id);
            Assert.Equal("owner-closing", Str(stop, "reason"));
        }

        // Exactly one per terminal: the explicit dispose afterwards added nothing.
        Assert.Equal(terminalIds.Count, stops.Count);
    }

    /// <summary>The assembly's no-op sink is installed, so fixtures never reach the operator's log.</summary>
    [Fact]
    public void TheAssemblyInstallsANoOpSink_SoFixturesDoNotWriteTheOperatorsLog() =>
        Assert.NotNull(WorkbenchDiagnostics.Sink);

    private static string? Evt(JsonElement r) => Str(r, "evt");

    private static string? Surface(JsonElement r) => Str(r, "surface");

    private static string? Str(JsonElement r, string name) =>
        r.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    /// <summary>Routes the diagnostics sink into a list for the test's lifetime, then restores it.</summary>
    private sealed class SinkCapture : IDisposable
    {
        private readonly List<string> _lines = [];
        private readonly Action<string>? _previous = WorkbenchDiagnostics.Sink;

        public SinkCapture()
        {
            WorkbenchDiagnostics.Sink = line =>
            {
                lock (_lines) { _lines.Add(line); }
            };
        }

        public List<string> Lines
        {
            get { lock (_lines) { return [.. _lines]; } }
        }

        public void Dispose() => WorkbenchDiagnostics.Sink = _previous;
    }
}
