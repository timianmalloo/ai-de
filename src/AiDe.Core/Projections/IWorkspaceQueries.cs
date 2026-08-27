namespace AiDe.Core.Projections;

/// <summary>
/// The workspace's read surface, however it is reached.
/// </summary>
/// <remarks>
/// <para><b>This seam exists so the shell does not know whether the core is in this process.</b>
/// ADR-0009 keeps both hosting modes supported — in-process first, then a separate daemon — and a
/// UI written against one of them is a UI that has to be rewritten to get the other. The whole
/// difference between the two is which implementation is handed in.</para>
///
/// <para><b>Asynchronous because the remote case is the real one.</b> A synchronous seam would force
/// every remote call to block a thread, and on a UI thread that is a frozen window for the length of
/// a pipe round trip. The in-process adapter completes immediately, which costs it nothing.</para>
///
/// <para><b>The result types are the core's own.</b> A parallel set of "view" types would be a second
/// definition of every result to keep in step, and the first divergence would show up as a field
/// present one way and missing the other.</para>
/// </remarks>
public interface IWorkspaceQueries
{
    Task<DescribeResult> DescribeAsync(string nodeId, int maxNeighbors, CancellationToken cancellationToken);

    Task<ImpactResult> ImpactAsync(string nodeId, int maxNodes, int maxEdges, CancellationToken cancellationToken);

    Task<FindResult> FindAsync(string term, int maxResults, CancellationToken cancellationToken);

    Task<KnowledgeResult> KnowledgeAsync(string? term, string? type, int maxResults, CancellationToken cancellationToken);
}

/// <summary>The read surface answered by a <see cref="ProjectionService"/> in this process.</summary>
/// <remarks>
/// Completed tasks rather than <c>Task.Run</c>: the projections are synchronous and fast, and moving
/// them to a thread pool thread would add a context switch and a scheduling hop to hide latency that
/// is not there.
/// </remarks>
public sealed class LocalWorkspaceQueries(ProjectionService projections) : IWorkspaceQueries
{
    public Task<DescribeResult> DescribeAsync(
        string nodeId, int maxNeighbors, CancellationToken cancellationToken) =>
        Task.FromResult(projections.Describe(nodeId, maxNeighbors));

    public Task<ImpactResult> ImpactAsync(
        string nodeId, int maxNodes, int maxEdges, CancellationToken cancellationToken) =>
        Task.FromResult(projections.Impact(nodeId, maxNodes, maxEdges));

    public Task<FindResult> FindAsync(
        string term, int maxResults, CancellationToken cancellationToken) =>
        Task.FromResult(projections.Find(term, maxResults));

    public Task<KnowledgeResult> KnowledgeAsync(
        string? term, string? type, int maxResults, CancellationToken cancellationToken) =>
        Task.FromResult(projections.Knowledge(new KnowledgeQuery(term, type, maxResults)));
}
