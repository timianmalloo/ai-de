using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using AiDe.Core.AgentPlane;

namespace AiDe.Core.PromptCompilation;

/// <summary>Mints envelope ids: sortable (a UTC timestamp) and collision-resistant (CSPRNG entropy), the session id's own shape.</summary>
public static class EnvelopeIds
{
    /// <summary>A new id, <c>yyyyMMddTHHmmssfffZ-xxxxxxxx</c>.</summary>
    public static string New(DateTimeOffset? now = null) =>
        (now ?? DateTimeOffset.UtcNow).UtcDateTime.ToString("yyyyMMddTHHmmssfff'Z'", CultureInfo.InvariantCulture)
        + "-" + RandomNumberGenerator.GetHexString(8, lowercase: true);
}

/// <summary>
/// The <c>compile.*</c> run-event vocabulary (spec Addendum D §A10.3; ADR-0033 rule 6): kinds added
/// to the open vocabulary <see cref="RunEvent.Kind"/> accepts, emitted to the Console stream and the
/// profiler — an emission, never a second store.
/// </summary>
public static class CompileEventKinds
{
    /// <summary><c>compile.stage{stage, duration_ms, outcome, over_bound?}</c>.</summary>
    public const string Stage = "compile.stage";

    /// <summary><c>compile.degraded{reason, error_code}</c>.</summary>
    public const string Degraded = "compile.degraded";

    /// <summary><c>compile.mode.changed{from, to, trigger}</c>.</summary>
    public const string ModeChanged = "compile.mode.changed";

    /// <summary>The <c>Ext.origin</c> value every compile event carries — origin is never inferred from <c>RunId</c> / <c>AgentId</c> (a type pun).</summary>
    public const string Origin = "compile";

    /// <summary>The stages <see cref="Stage"/> names.</summary>
    public static class Stages
    {
        public const string PreCompile = "pre-compile";
        public const string Compile = "compile";
        public const string Prepare = "prepare";
        public const string Submit = "submit";
    }
}

/// <summary>
/// The compile step's instrumentation on the normal path (IO1–IO12): every fact is first an
/// attribute on an <c>aide.compile</c> activity (OpenTelemetry is the data model), and the
/// Console-stream <see cref="RunEvent"/> is built by the one translator here with
/// <c>Ext = {origin: "compile", …}</c>.
/// </summary>
public static class CompileSignal
{
    /// <summary>The activity source every compile-step span is published on. Observed by <c>CompileSignalTests</c>.</summary>
    public const string SourceName = "aide.compile";

    private static readonly ActivitySource Source = new(SourceName);

    /// <summary>Opens a <c>compile.stage</c> span; the caller sets <c>outcome</c> and disposes it. Null when nothing listens — a sampler that declines is not a failure.</summary>
    public static Activity? Stage(string stage)
    {
        var activity = Source.StartActivity(CompileEventKinds.Stage);
        activity?.SetTag("stage", stage);
        return activity;
    }

    /// <summary>Records a degraded state: <c>compile.degraded{reason, error_code}</c>.</summary>
    public static void Degraded(string reason, string errorCode)
    {
        using var activity = Source.StartActivity(CompileEventKinds.Degraded);
        activity?.SetTag("reason", reason);
        activity?.SetTag("error_code", errorCode);
    }

    /// <summary>
    /// The translator at the seam: a Console-stream event for one compile fact, with the required
    /// <c>RunId</c> / <c>AgentId</c> set to what the session document's channel uses for
    /// host-originated events and the unprojected keys under <c>Ext</c>. No consumer may infer
    /// origin from the ids; <c>Ext.origin</c> is the origin.
    /// </summary>
    public static RunEvent ToRunEvent(string runId, string agentId, long seq, DateTimeOffset at, string kind, string envelopeId, JsonObject body, RunEventCost? cost = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        ArgumentNullException.ThrowIfNull(body);

        if (!kind.StartsWith("compile.", StringComparison.Ordinal))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "the translator emits the compile.* vocabulary only");
        }

        var ext = new JsonObject { ["origin"] = CompileEventKinds.Origin, ["envelope_id"] = envelopeId };
        return new RunEvent(runId, agentId, null, seq, at, kind, cost, body, ext);
    }

    /// <summary>Whether a Console-stream event came from the compile step — by <c>Ext.origin</c>, never by its ids.</summary>
    public static bool IsCompileEvent(RunEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);
        return evt.Ext.TryGetPropertyValue("origin", out var origin)
            && origin is JsonValue value
            && value.TryGetValue<string>(out var text)
            && string.Equals(text, CompileEventKinds.Origin, StringComparison.Ordinal);
    }
}
