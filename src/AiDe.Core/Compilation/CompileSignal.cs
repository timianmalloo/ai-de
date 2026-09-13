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
/// profiler — an emission, never a second store. This slice emits <c>compile.degraded</c>; the
/// stage rows arrive with the agentic rung.
/// </summary>
public static class CompileEventKinds
{
    /// <summary><c>compile.degraded{reason, error_code}</c>.</summary>
    public const string Degraded = "compile.degraded";

    /// <summary><c>compile.stage{stage, duration_ms, outcome}</c> — one per stage (§A10.3).</summary>
    public const string Stage = "compile.stage";

    /// <summary>The <c>Ext.origin</c> value every compile event carries — origin is never inferred from <c>RunId</c> / <c>AgentId</c> (a type pun).</summary>
    public const string Origin = "compile";
}

/// <summary>
/// The compile step's instrumentation on the normal path (IO1–IO12): every fact is an attribute on
/// an <c>aide.compile</c> activity (OpenTelemetry is the data model). The Console-stream translator
/// (<c>compile.stage</c> rows with <c>Ext.origin = "compile"</c>) is the agentic rung's, written
/// with its first emitter (CV-3) — the mechanical rung records no stage.
/// </summary>
public static class CompileSignal
{
    /// <summary>The activity source every compile-step span is published on. Observed by <c>CompileSignalTests</c>.</summary>
    public const string SourceName = "aide.compile";

    private static readonly ActivitySource Source = new(SourceName);

    /// <summary>Records one stage's timing and outcome: <c>compile.stage{stage, duration_ms, outcome}</c>; a stage that did not run is never recorded as 0.</summary>
    public static void Stage(string stage, long durationMs, string outcome)
    {
        using var activity = Source.StartActivity(CompileEventKinds.Stage);
        activity?.SetTag("stage", stage);
        activity?.SetTag("duration_ms", durationMs);
        activity?.SetTag("outcome", outcome);
    }

    /// <summary>Records a degraded state: <c>compile.degraded{reason, error_code}</c>.</summary>
    public static void Degraded(string reason, string errorCode)
    {
        using var activity = Source.StartActivity(CompileEventKinds.Degraded);
        activity?.SetTag("reason", reason);
        activity?.SetTag("error_code", errorCode);
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
