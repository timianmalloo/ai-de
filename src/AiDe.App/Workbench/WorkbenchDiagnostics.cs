using System.Diagnostics;
using System.IO;
using System.Text.Json;
using AiDe.Core.Workbench;

namespace AiDe.App.Workbench;

/// <summary>
/// Structured trace for workbench layout behaviour — pane placement, adds and closes. The workbench
/// had NO instrumentation, so a "my pane disappeared" report was untraceable after the fact. Each
/// event emits an OpenTelemetry-aligned <see cref="Activity"/> (via the <c>aide.workbench</c> source)
/// AND appends a compact JSON line to <c>%LOCALAPPDATA%/AiDe/logs/workbench-YYYYMMDD.log</c> so the
/// behaviour can be read back. Best-effort: diagnostics must never break the workbench, so every sink
/// path swallows its own failure.
/// </summary>
public static class WorkbenchDiagnostics
{
    public static readonly ActivitySource Source = new("aide.workbench");
    private static readonly object Gate = new();

    /// <summary>Test seam: when set, records go here instead of the log file (headless assertion).</summary>
    public static Action<string>? Sink { get; set; }

    /// <summary>Records a layout mutation and the resulting stack/surface topology.</summary>
    public static void LayoutMutation(
        string operation, string placement, string surfaceId, string? activeSurfaceId, Layout after)
    {
        using var activity = Source.StartActivity("workbench.layout.mutation");
        activity?.SetTag("workbench.operation", operation);
        activity?.SetTag("workbench.placement", placement);
        activity?.SetTag("workbench.surface", surfaceId);
        activity?.SetTag("workbench.active", activeSurfaceId);

        var stacks = after.AllStacks()
            .Select(s => new
            {
                id = s.Id,
                active = s.ActiveIndex,
                surfaces = s.Surfaces.Select(su => $"{su.SurfaceId}:{su.Kind}").ToArray(),
            })
            .ToArray();

        Write(new
        {
            ts = DateTimeOffset.UtcNow.ToString("O"),
            evt = "layout.mutation",
            operation,
            placement,
            surface = surfaceId,
            active = activeSurfaceId,
            stacks,
        });
    }

    /// <summary>
    /// Records the decision a terminal launch made, and how it ended.
    /// </summary>
    /// <remarks>
    /// <para><b>Why.</b> "New Claude Code session" opened a plain PowerShell prompt, twice, across
    /// two different root causes. Nothing about the launch was recorded — the log carried only layout
    /// mutations — so each round of diagnosis was static reading plus a screenshot, and the second
    /// round confirmed a fix that then did not change what the user saw.</para>
    ///
    /// <para>These are the INPUTS to the launch decision, not a narration of it: which executable was
    /// resolved, whether a readiness profile was found (that single value chooses between hosting the
    /// agent and running the command line as a shell), and whether the environment contract was
    /// attached. A wrong value here explains the symptom immediately; reading the code cannot, because
    /// the code is correct for the values it was written against.</para>
    ///
    /// <para>Terminal BYTES are never recorded (spec privacy). This is the launch decision only.</para>
    /// </remarks>
    public static void TerminalStart(
        string surfaceId, string? executable, string integration, bool hasReadinessProfile,
        string shellPath, int environmentCount, string? failure = null)
    {
        Write(new
        {
            ts = DateTimeOffset.UtcNow.ToString("O"),
            evt = "terminal.start",
            surface = surfaceId,
            executable,
            integration,
            readinessProfile = hasReadinessProfile,
            shellPath,
            environmentCount,
            failure,
        });
    }

    /// <summary>
    /// Records an unhandled exception, with the context that says which gesture produced it.
    /// </summary>
    /// <remarks>
    /// <para><b>Why this exists.</b> The shell crashed on "New Claude Code session" and left
    /// <b>nothing</b> — no Windows Error Reporting entry, no event-log record, and nothing in this
    /// log, which until now recorded only layout mutations. A user could say only that the .exe
    /// closed. The whole diagnosis had to start from a screenshot.</para>
    ///
    /// <para>A crash is the one moment when the product knows the most and reports the least. This
    /// does not change what happens next — the process still fails — it only makes the failure
    /// legible, which is the difference between "it crashed" and a stack trace pointing at a
    /// line.</para>
    /// </remarks>
    public static void Crash(string origin, Exception exception)
    {
        Write(new
        {
            ts = DateTimeOffset.UtcNow.ToString("O"),
            evt = "crash",
            origin,
            type = exception.GetType().FullName,
            message = exception.Message,

            // The inner exception is where a wrapped fault actually happened; reporting only the
            // outer one is how a real defect reads as an infrastructure complaint (DC-078).
            inner = exception.InnerException?.GetType().FullName,
            innerMessage = exception.InnerException?.Message,
            stack = exception.ToString(),
        });
    }

    private static void Write(object record)
    {
        string line;
        try { line = JsonSerializer.Serialize(record); }
        catch { return; }

        var sink = Sink;
        if (sink is not null)
        {
            try { sink(line); } catch { /* a test sink must not break the workbench either */ }
            return;
        }

        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AiDe", "logs");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"workbench-{DateTime.UtcNow:yyyyMMdd}.log");
            lock (Gate) { File.AppendAllText(path, line + Environment.NewLine); }
        }
        catch
        {
            // Diagnostics are best-effort; never let a logging failure surface as a workbench failure.
        }
    }
}
