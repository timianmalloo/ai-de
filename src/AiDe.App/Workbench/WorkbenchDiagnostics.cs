using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
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
    /// <param name="stackId">The stack the operation acted on, when it acted on one (a maximize); null — recorded as null, never as a guess — otherwise.</param>
    public static void LayoutMutation(
        string operation, string placement, string surfaceId, string? activeSurfaceId, Layout after, string? stackId = null)
    {
        using var activity = Source.StartActivity("workbench.layout.mutation");
        activity?.SetTag("workbench.operation", operation);
        activity?.SetTag("workbench.placement", placement);
        activity?.SetTag("workbench.surface", surfaceId);
        activity?.SetTag("workbench.active", activeSurfaceId);
        activity?.SetTag("workbench.stack", stackId);

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
            stack = stackId,
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
    /// Records the <c>session/new</c> a governed lane was opened with — the params object the client
    /// sent, <c>_meta</c> included — keyed by run, lane and the ACP session id it came back with.
    /// </summary>
    /// <remarks>
    /// <b>Why this exists (Ruling 71 (a)).</b> The F5 Proof Pack must carry the outgoing frame, and a
    /// frame someone had to remember to capture is "not recorded". Emitted on the normal path from
    /// the one site that opens a lane's session, so the log holds it whether or not anyone watched.
    /// The unit test proves the shape; this line proves what was sent.
    /// </remarks>
    public static void LaneSessionNew(string runId, string laneId, string sessionId, JsonObject? parameters)
    {
        using var activity = Source.StartActivity("lane.session-new");
        activity?.SetTag("lane.run", runId);
        activity?.SetTag("lane.id", laneId);
        activity?.SetTag("lane.session", sessionId);

        Write(new
        {
            ts = DateTimeOffset.UtcNow.ToString("O"),
            evt = "lane.session-new",
            run = runId,
            lane = laneId,
            session = sessionId,
            @params = parameters,
        });
    }

    /// <summary>
    /// Records that the shell started, naming the binary it is: the informational version and the
    /// commit inside it, the build configuration, the docking theme, the DPI and the window.
    /// </summary>
    /// <remarks>
    /// <para><b>Why this exists (INV-0008).</b> Three Release builds from three commits were on one
    /// machine; the operator photographed one and nothing tied the screenshot to a commit. A fixed
    /// instance re-entered as a recurrence and the investigation re-derived the mechanism before it
    /// could read the version off the binary. The binary always carried its commit
    /// (<c>AssemblyInformationalVersion</c> = <c>1.0.0+&lt;sha&gt;</c>); the shell never said it.
    /// A UI report now starts from this line, and is attributed before it is triaged.</para>
    /// <para><b>On the normal path, no flag.</b> Emitted once from the main window's <c>Loaded</c>,
    /// after the window has a size and a DPI — the two facts a contrast or layout report needs and
    /// a screenshot cannot state.</para>
    /// <para>A build without a source revision records <c>commit</c> as null. It never invents one.</para>
    /// </remarks>
    public static void AppStart(string theme, DpiScale dpi, double width, double height, string windowState)
    {
        var assembly = typeof(WorkbenchDiagnostics).Assembly;
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var configuration = assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration;

        using var activity = Source.StartActivity("app.start");
        activity?.SetTag("service.version", version);
        activity?.SetTag("vcs.revision", CommitOf(version));
        activity?.SetTag("app.theme", theme);

        Write(new
        {
            ts = DateTimeOffset.UtcNow.ToString("O"),
            evt = "app.start",
            version,
            commit = CommitOf(version),
            configuration,
            theme,
            dpi = new { scaleX = dpi.DpiScaleX, scaleY = dpi.DpiScaleY, pixelsPerInchX = dpi.PixelsPerInchX, pixelsPerInchY = dpi.PixelsPerInchY },
            window = new { width, height, state = windowState },
        });
    }

    /// <summary>The 40-hex source revision an informational version carries after its <c>+</c>, or null.</summary>
    internal static string? CommitOf(string? informationalVersion)
    {
        var plus = informationalVersion?.LastIndexOf('+') ?? -1;
        if (informationalVersion is null || plus < 0) return null;

        var revision = informationalVersion[(plus + 1)..];
        return revision.Length == 40 && revision.All(c => c is (>= '0' and <= '9') or (>= 'a' and <= 'f')) ? revision : null;
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

    /// <summary>
    /// Records a change of the shell's primary view mode — which body is on screen — and what asked
    /// for it.
    /// </summary>
    /// <remarks>
    /// <b>Why this exists (INV-0009).</b> Two session documents were opened into the docking host
    /// while the Explorer was the body, and the shell announced each as opened. The log could show
    /// the mode had been entered only by an <c>explorer-graph</c> surface initialising — inferred
    /// from a side effect, never stated. The mode is the one fact every "I opened X and saw nothing"
    /// report turns on, so it is written on the normal path, once per change, with its trigger.
    /// </remarks>
    public static void ShellMode(ShellViewMode from, ShellViewMode to, string trigger)
    {
        using var activity = Source.StartActivity("workbench.shell.mode");
        activity?.SetTag("workbench.mode", to.ToString());
        activity?.SetTag("workbench.mode.from", from.ToString());
        activity?.SetTag("workbench.trigger", trigger);

        Write(new
        {
            ts = DateTimeOffset.UtcNow.ToString("O"),
            evt = "shell.mode",
            mode = to.ToString(),
            from = from.ToString(),
            trigger,
        });
    }

    /// <summary>
    /// Records that a session document's composer was bound to a run context — which session, which
    /// surface, and the checkout a run would be cut from.
    /// </summary>
    /// <remarks>
    /// The binding was the one step of <c>File → New Session</c> with no line of its own: the
    /// composer's <c>configured</c> transition says fields were pushed, not what they were bound to
    /// (INV-0009 F5). <paramref name="repositoryRoot"/> is the fact finding C could only infer.
    /// </remarks>
    public static void SessionDocumentBound(string sessionId, string surfaceId, string repositoryRoot)
    {
        using var activity = Source.StartActivity("workbench.session-document.bound");
        activity?.SetTag("workbench.session", sessionId);
        activity?.SetTag("workbench.surface", surfaceId);

        Write(new
        {
            ts = DateTimeOffset.UtcNow.ToString("O"),
            evt = "session-document.bound",
            session = sessionId,
            surface = surfaceId,
            repositoryRoot,
        });
    }

    /// <summary>
    /// Records that a session document's composer was left without a run binding, naming the field
    /// the refusal points at.
    /// </summary>
    /// <remarks>
    /// The refusal the operator read at 22:33:28Z (<i>repositoryRoot: this window has no open
    /// workspace…</i>) reached the composer's status line and nowhere else; the investigation had to
    /// reconstruct it from the code path (INV-0009 §7). The field and the reason are recorded as the
    /// operator read them; a reason names at most a path this log already carries (the workspace,
    /// the provider file), never a draft's text.
    /// </remarks>
    public static void SessionDocumentRefused(string sessionId, string? surfaceId, string field, string reason)
    {
        using var activity = Source.StartActivity("workbench.session-document.refused");
        activity?.SetTag("workbench.session", sessionId);
        activity?.SetTag("workbench.surface", surfaceId);
        activity?.SetTag("workbench.field", field);

        Write(new
        {
            ts = DateTimeOffset.UtcNow.ToString("O"),
            evt = "session-document.refused",
            session = sessionId,
            surface = surfaceId,
            field,
            reason,
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
