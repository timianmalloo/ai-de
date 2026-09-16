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
/// Classification predicates do not exist yet — all rows are <see cref="EntryPointKind.Unclassified"/>.
/// Open Sequence is not this query (mapper r4 unfrozen).
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
            .Select(c => new EntryPointRow(
                EntryPointKind.Unclassified,
                c.NodeId,
                c.NodeId,
                EntryPointsProjection.UnclassifiedReasonPendingClassifier))
            .ToList();

        IReadOnlyList<string> disclosures = omitted > 0
            ? [$"Omitted ({omitted})"]
            : [];

        return new EntryPointsResult(rows, omitted, disclosures, sourceRevision);
    }
}
