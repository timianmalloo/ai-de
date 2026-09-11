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
    /// Records a reconcile of the <b>view</b> back into the model — the fold-in that follows a native
    /// tab drag — with the zone assignment before and after it.
    /// </summary>
    /// <remarks>
    /// <para><b>Why this exists (INV-0006).</b> A drag emitted <b>nothing at all</b>. The log for the
    /// reported session is empty across the entire defect window, 12:55:02Z to 12:59:50Z, which spans
    /// the three screenshots that contain the defect — so the gesture that produced it is permanently
    /// unrecoverable, and every future report of this shape would have been too. That absence, not the
    /// swap, is what made the report unanswerable: the code could be read, and the code is correct for
    /// the values it was written against.</para>
    /// <para><b>Before and after, not just after.</b> <see cref="LayoutMutation"/> records the
    /// resulting topology, which is enough for a placement ("the terminal went here") and useless for
    /// a reconcile: the question is what the reconcile DID, and a correct reconcile and a whole-column
    /// relabel produce topologies that look equally reasonable on their own. The pair, plus the list
    /// of surfaces whose zone changed, is what separates them.</para>
    /// <para>A reconcile that changed nothing and refused nothing writes nothing — the caller decides,
    /// because a log that records every no-op is a log nobody reads.</para>
    /// </remarks>
    public static void LayoutReconcile(
        string trigger, string? before, string? after, IReadOnlyList<string> moved, string? refusal)
    {
        using var activity = Source.StartActivity("workbench.layout.mutation");
        activity?.SetTag("workbench.operation", trigger);
        activity?.SetTag("workbench.placement", refusal is null ? "reconciled" : "refused");
        activity?.SetTag("workbench.zones.before", before);
        activity?.SetTag("workbench.zones.after", after);
        activity?.SetTag("workbench.moved", string.Join(",", moved));
        activity?.SetTag("error.code", refusal);

        Write(new
        {
            ts = DateTimeOffset.UtcNow.ToString("O"),
            evt = "layout.mutation",
            operation = trigger,
            placement = refusal is null ? "reconciled" : "refused",
            before,
            after,
            moved,
            refusal,
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

    /// <summary>
    /// Records what contributing to <c>.mcp.json</c> did, and the detail that must not be announced.
    /// </summary>
    /// <remarks>
    /// <para>The user-facing reason is deliberately content-free: a parse failure's message names the
    /// property it choked on — "Duplicate property 'ACME_API_KEY' encountered during deserialization"
    /// — and that property belongs to a third party, whose file the user never asked us to read out
    /// to the live region. The detail is nonetheless the half a person diagnosing actually needs, so
    /// it is recorded here rather than discarded.</para>
    ///
    /// <para>Written on EVERY outcome, not just the failures, because the question an operator asks
    /// first is which of the five things happened — and an event that only appears when something
    /// broke cannot answer "it did nothing, and that was correct".</para>
    /// </remarks>
    public static void McpConfig(string outcome, string? path, string? detail)
    {
        Write(new
        {
            ts = DateTimeOffset.UtcNow.ToString("O"),
            evt = "mcp.config",
            outcome,
            path,
            detail,
        });
    }

    /// <summary>
    /// Records the composer's rendered bounds: the editor host, the read-only compiled view, and the
    /// composer they share — at first layout and whenever either part moves past the surface's
    /// threshold.
    /// </summary>
    /// <remarks>
    /// <para><b>A value that could not be read is <c>null</c>, never 0</b> (DC-137). An element not
    /// yet arranged reports 0px, and 0px is also the defect — so the writer sends <c>null</c> for a
    /// part whose arrange is not valid, and a reader can tell "not laid out" from "laid out at
    /// nothing". <paramref name="visible"/> and <paramref name="loaded"/> separate a hidden pane's
    /// honest 0 from a starved one.</para>
    /// </remarks>
    public static void ComposerLayout(
        string surfaceId,
        double? composerWidth, double? composerHeight,
        double? editorWidth, double? editorHeight,
        double? compiledWidth, double? compiledHeight,
        bool visible, bool loaded, long inputs)
    {
        Write(new
        {
            ts = DateTimeOffset.UtcNow.ToString("O"),
            evt = "composer.layout",
            surface = surfaceId,
            composer = new { width = composerWidth, height = composerHeight },
            editor = new { width = editorWidth, height = editorHeight },
            compiled = new { width = compiledWidth, height = compiledHeight },
            visible,
            loaded,
            inputs,
        });
    }

    /// <summary>
    /// Records one transition of a web surface's host↔page handshake, with the surface's counts as
    /// they stood at that moment.
    /// </summary>
    /// <remarks>
    /// <para><b>The transitions</b> — from the host: <c>initialising</c>, <c>re-attached</c>,
    /// <c>init-failed</c>; from the composer: <c>navigation-started</c>, <c>configured</c>,
    /// <c>page-ready</c>, <c>init-pushed</c>, <c>input-received</c> (once per pushed init, at the
    /// first accepted <c>draft.changed</c>), <c>message-dropped</c> (once per kind and reason per
    /// navigation), <c>disposed</c>. Counts are lifetime totals as they stood at the transition;
    /// a count the caller does not measure is <c>null</c>, never invented. No character of the
    /// draft is ever recorded.</para>
    /// </remarks>
    public static void WebSurfaceHandshake(
        string surfaceId, string transition,
        int? navigations = null, long? inputs = null, long? drops = null, string? detail = null,
        string? errorCode = null, string? exceptionType = null)
    {
        Write(new
        {
            ts = DateTimeOffset.UtcNow.ToString("O"),
            evt = "web-surface.handshake",
            surface = surfaceId,
            transition,
            navigations,
            inputs,
            drops,
            detail,
            errorCode,
            exceptionType,
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
