using System.Diagnostics;
using AiDe.Core.Facts;
using AiDe.Core.Projections;
using AiDe.Core.Watcher;

namespace AiDe.Core.Mcp;

public static class McpErrorCodes
{
    public const string EgressDenied = "AIDE-MCP-EGRESS-DENIED";
    public const string CrossWorkspace = "AIDE-AUTH-CROSS-WORKSPACE";
    public const string LimitExceeded = "AIDE-MCP-LIMIT-EXCEEDED";

    /// <summary>The tool ran and its subject does not exist — distinct from a tool that refused.</summary>
    /// <remarks>
    /// An absent subject must not come back as an empty payload. An empty standing reads as "you
    /// have no rank and no reasons", which is a claim about the agent rather than about the lookup
    /// (DC-087) — and the same is true of an empty description or an empty result set.
    /// </remarks>
    public const string NotFound = "AIDE-MCP-NOT-FOUND";
}

/// <summary>Identifies the calling agent session, including where its bytes go next.</summary>
public sealed record McpCallerContext(
    string WorkspaceId,
    string SessionId,
    SessionProcessingClass ProcessingClass,
    CallerPrincipal Caller);

public enum ToolAuthorization
{
    /// <summary>Full bounded result.</summary>
    Allow,

    /// <summary>Non-sensitive counts and revision only — enough to say "there is data", not what it is.</summary>
    MinimumMetadataOnly,

    Deny,
}

/// <summary>A tool result that knows whether it was reduced, and why.</summary>
public sealed record McpToolResult(
    bool IsError,
    string? ErrorCode,
    ToolAuthorization Authorization,
    object? Payload,
    string SourceRevision);

/// <summary>
/// The MCP boundary. Every tool call is authorized against the calling session's declared
/// processing class before any workspace content is assembled (ADR-0011).
/// </summary>
/// <remarks>
/// Pattern: Policy-Bound Egress. Loopback binding answers "who connected", not "where do these bytes
/// go next" — an externally-processing agent runs locally and forwards to its provider, so a
/// transport control cannot close that path. Authorization therefore follows the session class, and
/// an unknown class fails closed exactly like an external one.
/// </remarks>
public sealed class McpToolGateway(
    ProjectionService projections,
    string workspaceId,
    AiDe.Core.Presentation.IWatcherLeaderboardQuery? scoredEpisodes = null)
{
    private static readonly ActivitySource Activity = new("aide.mcp.request");

    /// <summary>
    /// The deterministic (T0) authorization decision. A model never influences this — it runs before
    /// any content is read.
    /// </summary>
    public static ToolAuthorization Authorize(McpCallerContext caller, string toolName)
    {
        if (caller.ProcessingClass is SessionProcessingClass.LocalOnly)
        {
            return ToolAuthorization.Allow;
        }

        // Writes from a non-local session are denied outright: an attributed record authored via a
        // provider-backed session cannot carry a trustworthy attribution.
        return toolName is "record_note" or "record_decision" or "announce_claim"
            ? ToolAuthorization.Deny
            : ToolAuthorization.MinimumMetadataOnly;
    }

    public McpToolResult Describe(McpCallerContext caller, string nodeId, int maxNeighbors)
        => Guarded(caller, "describe", () =>
        {
            var result = projections.Describe(nodeId, maxNeighbors);
            return (result, result.SourceRevision, result.Bounds);
        });

    public McpToolResult Find(McpCallerContext caller, string term, int maxResults)
        => Guarded(caller, "find", () =>
        {
            var result = projections.Find(term, maxResults);
            return (result, result.SourceRevision, result.Bounds);
        });

    /// <summary>
    /// The agent's own standing for one episode: rank where comparable, trend, and one
    /// evidence-backed reason per dimension (US-16).
    /// </summary>
    /// <remarks>
    /// <para><b>A PULL, deliberately.</b> US-16 says the agent receives its standing each turn, and
    /// the obvious reading is a push. A push would put the scorer's output into the agent's context
    /// every turn whether or not it asked — and ADR-0019's anti-Goodhart section is precisely about
    /// what an agent is shown regarding its own scoring. An agent that asks has chosen to look.</para>
    ///
    /// <para><b>Guarded like every other tool</b>, which is the reason it lives here rather than on a
    /// new seam: it inherits the workspace check, the authorization gate and the
    /// minimum-metadata degradation rather than restating any of them. A standing is an evaluation
    /// of the caller, so a tool that skipped the cross-workspace check would let one workspace read
    /// another's scoring.</para>
    ///
    /// <para><b>An unknown episode is an error, not an empty standing.</b> An empty one reads as
    /// "you have no rank and no reasons" — a claim about the agent rather than about the lookup
    /// (DC-087).</para>
    /// </remarks>
    public McpToolResult Standing(McpCallerContext caller, string episodeId)
        => Guarded(caller, "standing", () =>
        {
            var episodes = scoredEpisodes?.GetScoredEpisodes() ?? [];
            var subject = episodes.FirstOrDefault(
                e => string.Equals(e.EpisodeId, episodeId, StringComparison.Ordinal));

            if (subject is null)
            {
                return (null, "none", new ResultBounds(0, 0, 0, 0, 0, 0, 0, false, null));
            }

            var board = new LeaderboardComposer().Compose(episodes, subject.TaskClass, subject.SchemaVersion);
            var standing = new StandingComposer().Compose(subject, board, episodes);

            // One node (the subject), one "edge" per reason: the bounds a caller degraded to
            // minimum-metadata sees, which must still be true rather than zero.
            return (standing, subject.Scorecard.SchemaVersion,
                new ResultBounds(1, standing.Reasons.Count, 0, 1, 0, standing.Reasons.Count, 0, false, null));
        });

    /// <param name="read">
    /// Produces the payload. A <b>null</b> payload means the subject does not exist, and is turned
    /// into a <see cref="McpErrorCodes.NotFound"/> error rather than returned as an empty result —
    /// the guard is the only place that distinction can be made once for every tool.
    /// </param>
    private McpToolResult Guarded(
        McpCallerContext caller, string toolName, Func<(object? Payload, string Revision, ResultBounds Bounds)> read)
    {
        using var activity = Activity.StartActivity("aide.mcp.request");
        activity?.SetTag("tool", toolName);
        activity?.SetTag("session.processing_class", caller.ProcessingClass.ToString());

        if (!string.Equals(caller.WorkspaceId, workspaceId, StringComparison.Ordinal))
        {
            activity?.SetTag("error.code", McpErrorCodes.CrossWorkspace);
            return new McpToolResult(true, McpErrorCodes.CrossWorkspace, ToolAuthorization.Deny, null, "none");
        }

        var authorization = Authorize(caller, toolName);
        activity?.SetTag("authorization", authorization.ToString());

        if (authorization is ToolAuthorization.Deny)
        {
            activity?.SetTag("error.code", McpErrorCodes.EgressDenied);
            return new McpToolResult(true, McpErrorCodes.EgressDenied, authorization, null, "none");
        }

        var (payload, revision, bounds) = read();

        if (payload is null)
        {
            activity?.SetTag("error.code", McpErrorCodes.NotFound);
            return new McpToolResult(true, McpErrorCodes.NotFound, authorization, null, revision);
        }

        if (authorization is ToolAuthorization.MinimumMetadataOnly)
        {
            // Deliberately no labels, no paths, no provenance strings: counts and a revision only.
            // This says "evidence exists" without exporting what it says.
            return new McpToolResult(false, McpErrorCodes.EgressDenied, authorization,
                new MinimumMetadata(bounds.ReturnedNodes, bounds.ReturnedEdges, revision), revision);
        }

        return new McpToolResult(false, null, authorization, payload, revision);
    }
}

/// <summary>What a non-LocalOnly caller is allowed to learn: that there is data, not what it says.</summary>
public sealed record MinimumMetadata(int NodeCount, int EdgeCount, string SourceRevision);
