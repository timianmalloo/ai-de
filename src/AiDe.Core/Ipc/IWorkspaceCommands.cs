namespace AiDe.Core.Ipc;

/// <summary>
/// The workspace's write surface, however it is reached.
/// </summary>
/// <remarks>
/// <para><b>Separate from <see cref="Projections.IWorkspaceQueries"/> because reads and writes are
/// not the same kind of thing.</b> A read can be repeated freely; a write bumps a generation and
/// commits a snapshot, carries an idempotency key, and is judged against the epoch fence. Folding
/// them into one interface would put a name on the seam ("queries") that half its members
/// contradict, and would make every read-only consumer hold a handle that can also mutate.</para>
///
/// <para>Both hosting modes satisfy it — the in-process core and the daemon client — for the same
/// reason the read seam exists: ADR-0009 keeps both, and a UI written against one of them is a UI
/// that has to be rewritten to get the other.</para>
/// </remarks>
public interface IWorkspaceCommands
{
    /// <summary>Re-indexes a scope and reports how it went.</summary>
    Task<ScopeRefreshStatus> RefreshScopeAsync(
        string scopeId, string artifactRevision, CancellationToken cancellationToken);

    /// <summary>
    /// Discovers and indexes every C# scope in the workspace — one per (project, target framework).
    /// </summary>
    /// <remarks>
    /// Awaited on the wire rather than started-and-polled, unlike scope refresh: indexing a
    /// repository is the user pressing a button and waiting for a graph, and its own per-scope
    /// budget already bounds how long it can take.
    /// </remarks>
    Task<IndexSummary> IndexSolutionAsync(string artifactRevision, CancellationToken cancellationToken);
}

/// <summary>What an index run found, as the shell reports it.</summary>
public sealed record IndexSummary(
    int ScopesFound,
    int ScopesIndexed,
    int Assertions,
    IReadOnlyList<string> Failed,
    IReadOnlyList<string> Disclosures,
    string? Contexts = null)
{
    /// <summary>One sentence for the announcement channel, including what was NOT seen.</summary>
    public string Describe()
    {
        if (ScopesFound == 0) return "No C# projects found in this workspace.";

        var text = $"Indexed {ScopesIndexed} of {ScopesFound} scope(s): {Assertions:N0} assertion(s).";
        if (Failed.Count > 0) text += $" {Failed.Count} scope(s) failed and were quarantined.";

        // Disclosures are part of the result, not a footnote: a graph that silently omits package
        // types looks complete, and the user has no way to know it is not.
        if (Disclosures.Count > 0) text += " Not analysed: " + string.Join(", ", Disclosures) + ".";

        // Coverage is reported alongside the count, so "we have contexts" cannot quietly mean
        // "we have contexts for a fraction of the code" (ADR-0016).
        if (!string.IsNullOrWhiteSpace(Contexts)) text += " " + Contexts;
        return text;
    }
}

/// <summary>The write surface applied by a core in this process.</summary>
/// <remarks>
/// Takes the refresh as a delegate rather than a <see cref="WorkspaceCore"/> so that what the
/// in-process mode reports — a completed count, or a failure with its reason — is decided in one
/// place and testable without a store.
/// </remarks>
public sealed class LocalWorkspaceCommands(
    Func<string, string, CancellationToken, Task<int>> refresh,
    Func<string, CancellationToken, Task<IndexSummary>>? index = null)
    : IWorkspaceCommands
{
    /// <inheritdoc />
    public Task<IndexSummary> IndexSolutionAsync(string artifactRevision, CancellationToken cancellationToken) =>
        index is null
            ? Task.FromResult(new IndexSummary(0, 0, 0, [], []))
            : index(artifactRevision, cancellationToken);

    public async Task<ScopeRefreshStatus> RefreshScopeAsync(
        string scopeId, string artifactRevision, CancellationToken cancellationToken)
    {
        // The command id is synthesised here because in-process there is no retry to deduplicate:
        // the caller and the work share a stack frame, so a lost reply is not a thing that can
        // happen. Across the boundary it is the idempotency key and it matters a great deal.
        var commandId = Guid.NewGuid().ToString("N");

        try
        {
            var count = await refresh(scopeId, artifactRevision, cancellationToken).ConfigureAwait(false);
            return new ScopeRefreshStatus(
                commandId, scopeId, ScopeRefreshState.Completed, count, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Reported, not thrown, so both hosting modes hand a caller the same shape. Across the
            // boundary a failure arrives as a status; if it arrived here as an exception, every
            // caller would need two ways to learn the same fact.
            return new ScopeRefreshStatus(
                commandId, scopeId, ScopeRefreshState.Failed, 0, ex.Message);
        }
    }
}
