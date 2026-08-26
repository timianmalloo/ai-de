using System.Diagnostics;
using AiDe.Core.Facts;
using AiDe.Core.Projections;

namespace AiDe.Core.Mcp;

public static class McpErrorCodes
{
    public const string EgressDenied = "AIDE-MCP-EGRESS-DENIED";
    public const string CrossWorkspace = "AIDE-AUTH-CROSS-WORKSPACE";
    public const string LimitExceeded = "AIDE-MCP-LIMIT-EXCEEDED";
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
public sealed class McpToolGateway(ProjectionService projections, string workspaceId)
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

    private McpToolResult Guarded(
        McpCallerContext caller, string toolName, Func<(object Payload, string Revision, ResultBounds Bounds)> read)
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
