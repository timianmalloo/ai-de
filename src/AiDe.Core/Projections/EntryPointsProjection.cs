namespace AiDe.Core.Projections;

/// <summary>How many listing rows a query asks for.</summary>
public sealed record EntryPointsQuery(int MaxRows = EntryPointsProjection.DefaultMaxRows);

public enum EntryPointKind { Api, Ux, Cli, Unclassified }

/// <summary>One row is one entry-point candidate occurrence in the current snapshot.</summary>
/// <param name="NodeId"><c>node_dim.node_id</c> when the occurrence is already a graph node; otherwise null.</param>
public sealed record EntryPointRow(
    EntryPointKind Kind,
    string? NodeId,
    string Display,
    string? UnclassifiedReason);

public sealed record EntryPointsResult(
    IReadOnlyList<EntryPointRow> Rows,
    int OmittedByCap,
    IReadOnlyList<string> Disclosures,
    string SourceRevision);

/// <summary>
/// UV-0 listing: every latest-generation <c>has_type</c> source node is a row.
/// Name heuristics classify api/ux/cli; otherwise unclassified (never silent-drop).
/// Open Sequence is not this query (mapper r5: authorship only, Sequence still disabled).
/// </summary>
public static class EntryPointsProjection
{
    /// <summary>Inferred until measured. Same order as graph node default.</summary>
    public const int DefaultMaxRows = 5_000;

    public const string UnclassifiedReasonPendingClassifier = "classifier-not-admitted";
}

/// <summary>Store-facing listing. Lives next to the DTO so tests can name the query without IPC.</summary>
public static class EntryPointsListing
{
    public static EntryPointsResult FromHasType(
        IReadOnlyList<(string NodeId, string TypeKind)> candidates,
        int maxRows,
        string sourceRevision)
    {
        var cap = maxRows < 1 ? 1 : maxRows;
        var omitted = Math.Max(0, candidates.Count - cap);
        var rows = candidates
            .Take(cap)
            .Select(c => Classify(c.NodeId, c.TypeKind))
            .ToList();

        IReadOnlyList<string> disclosures = omitted > 0
            ? [$"Omitted ({omitted})"]
            : [];

        return new EntryPointsResult(rows, omitted, disclosures, sourceRevision);
    }

    /// <summary>
    /// Listing-kind heuristics on the type display name. Not Core method-observation identity
    /// (r5: has_member/call facts are not observation ids).
    /// </summary>
    internal static EntryPointRow Classify(string nodeId, string _)
    {
        var display = nodeId;
        var kind = KindFromDisplay(display);
        return new EntryPointRow(
            kind,
            nodeId,
            display,
            kind == EntryPointKind.Unclassified
                ? EntryPointsProjection.UnclassifiedReasonPendingClassifier
                : null);
    }

    internal static EntryPointKind KindFromDisplay(string display)
    {
        if (display.EndsWith(".Program", StringComparison.Ordinal)
            || display.Equals("Program", StringComparison.Ordinal)
            || display.Contains("CommandLine", StringComparison.Ordinal))
        {
            return EntryPointKind.Cli;
        }

        if (display.Contains("Controller", StringComparison.Ordinal)
            || display.Contains("Endpoint", StringComparison.Ordinal)
            || display.Contains(".Api.", StringComparison.Ordinal)
            || display.StartsWith("Api.", StringComparison.Ordinal)
            || display.EndsWith("Api", StringComparison.Ordinal))
        {
            return EntryPointKind.Api;
        }

        if (display.Contains("Window", StringComparison.Ordinal)
            || display.Contains("UserControl", StringComparison.Ordinal)
            || display.Contains("Page", StringComparison.Ordinal))
        {
            return EntryPointKind.Ux;
        }

        return EntryPointKind.Unclassified;
    }
}
