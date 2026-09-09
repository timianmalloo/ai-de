using System.Text.Json.Nodes;

namespace AiDe.Core.AgentPlane;

/// <summary>
/// An agent-plane failure carrying a stable <see cref="Code"/> (Observability Standard O7). The
/// message is for humans and may change; the code is for machines and search and does not.
/// </summary>
/// <remarks>
/// Mirrors <c>AiDe.Core.Watcher.WatcherException</c> deliberately rather than sharing a base type:
/// two exception families with two code ranges keep a caller's <c>catch</c> honest about which
/// subsystem failed, and the duplication is a constructor and a property.
/// </remarks>
public sealed class AgentPlaneException : Exception
{
    public AgentPlaneException(string code, string message) : base(message) => Code = code;

    /// <summary>A stable <see cref="AgentPlaneErrorCodes"/> value.</summary>
    public string Code { get; }
}

/// <summary>Stable error codes for the agent plane. Search-key stability is the whole point.</summary>
public static class AgentPlaneErrorCodes
{
    /// <summary>An engine id that the catalog does not carry. Never defaulted.</summary>
    public const string UnknownEngine = "AP-0001";

    /// <summary>An engine whose declared ACP mode is not <c>adapter</c>, the only Phase-1 launch path.</summary>
    public const string LaunchPathNotImplemented = "AP-0002";

    /// <summary>An adapter engine whose entry module has never been observed on a real install.</summary>
    public const string AdapterEntryModuleNotRecorded = "AP-0003";

    /// <summary>A wire frame that is not a JSON object, so it carries no event at all.</summary>
    public const string MalformedFrame = "AP-0004";
}

/// <summary>
/// The metered cost of one run event, in the spec's §14.1 shape
/// (<c>tokens_in · tokens_out · cache_read · requests · credits?</c>).
/// </summary>
/// <remarks>
/// <para><b>Null, never zero, when the wire did not say.</b> Most ACP frames carry no usage at all,
/// and a zero would read as "this cost nothing" rather than "not recorded" (IO12).</para>
/// <para><c>Credits</c> is nullable because it belongs to metered lanes only; no Phase-1 engine is
/// metered, so nothing populates it yet.</para>
/// </remarks>
public sealed record RunEventCost(
    long TokensIn,
    long TokensOut,
    long CacheRead,
    int Requests,
    decimal? Credits = null);

/// <summary>
/// The one normalized envelope every run-event source produces — spec §7.2 and §14.1.
/// </summary>
/// <remarks>
/// <para><b><c>Kind</c> is an open string, not an enum.</b> The spec states the rule itself:
/// evolution is "additive only; consumers ignore unknown kinds". An enum would make every new
/// adapter kind a compile break in every consumer, which converts an additive protocol change into
/// a breaking one — the exact failure the ACP fidelity-drift risk (§13, risk 1) names. The v1 kinds
/// are a vocabulary, not a closed set: <c>agent.msg</c>, <c>tool.call</c>, <c>tool.result</c>,
/// <c>permission.request</c> and the rest are what a producer <i>may</i> emit, and an unrecognized
/// wire kind is namespaced (<c>acp.*</c>) and carried rather than rejected.</para>
///
/// <para><b><c>ParentAgentId</c> is carried and never populated in Phase 1.</b> There is no
/// Conductor until Phase 2, so nothing can know a parent. The field exists so the envelope is the
/// spec's shape today rather than a migration tomorrow; building anything on it now would be
/// building on a value that is always null.</para>
///
/// <para><b><c>Ext</c> preservation is load-bearing.</b> Anything the mapper did not recognize
/// survives verbatim under <c>Ext</c>. §13's mitigation for adapter drift is precisely "unknown
/// events preserved under ext" — a dropped field is silent data loss in what §7.1 calls the truth.
/// </para>
/// </remarks>
/// <param name="RunId">The governed run this event belongs to. Supplied by the plane, not the wire.</param>
/// <param name="AgentId">The lane that produced it. Supplied by the plane, not the wire.</param>
/// <param name="ParentAgentId">Always null in Phase 1 — see the remarks.</param>
/// <param name="Seq">Per-run monotonic ordinal, assigned at receipt. ACP frames carry no sequence.</param>
/// <param name="Ts">Receipt time, stamped by the reader. ACP frames carry no timestamp.</param>
/// <param name="Kind">An open string — see the remarks.</param>
/// <param name="Cost">Null when the wire recorded no usage.</param>
/// <param name="Body">The payload of a recognized kind; empty for an unrecognized one.</param>
/// <param name="Ext">Everything not projected into <paramref name="Body"/>, verbatim.</param>
public sealed record RunEvent(
    string RunId,
    string AgentId,
    string? ParentAgentId,
    long Seq,
    DateTimeOffset Ts,
    string Kind,
    RunEventCost? Cost,
    JsonObject Body,
    JsonObject Ext);
