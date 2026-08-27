using System.Runtime.Versioning;
using System.Text.Json;
using AiDe.Core.Projections;

namespace AiDe.Core.Ipc;

/// <summary>
/// Raised when the daemon refuses a request, carrying the boundary's stable code.
/// </summary>
/// <remarks>
/// An exception rather than a nullable result because these are not outcomes a caller chooses
/// between — a stale epoch, a revoked capability and an unsupported version are all "this request
/// did not happen, and you must decide what to do about it". The code is on the exception so a
/// caller can decide without parsing prose.
/// </remarks>
public sealed class IpcRequestException(string code, string reason)
    : Exception($"{code}: {reason}")
{
    public string Code { get; } = code;
}

/// <summary>
/// The core's read surface, over the boundary, in the same shapes the in-process caller uses.
/// </summary>
/// <remarks>
/// <para><b>The result types are the core's own</b> — <see cref="DescribeResult"/> and its
/// siblings — rather than a parallel set of wire types. A second definition of one result is two
/// things to keep in step, and the first divergence would appear as a field that is present in
/// process and missing across the pipe.</para>
///
/// <para><b>The epoch is carried, not assumed.</b> Every request states which epoch it was authored
/// against, and the daemon rejects a mismatch. That is the fence that stops a command reasoning
/// about state that has since been replaced, and a client that quietly resent the daemon's current
/// epoch would defeat it while appearing to work.</para>
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class WorkspaceClient : IWorkspaceQueries, IAsyncDisposable
{
    private readonly IpcClient _client;
    private readonly string _workspaceId;
    private long _epoch;

    private WorkspaceClient(IpcClient client, string workspaceId, long epoch)
    {
        _client = client;
        _workspaceId = workspaceId;
        _epoch = epoch;
    }

    /// <summary>The epoch this client is bound to.</summary>
    public long Epoch => _epoch;

    /// <summary>Connects, handshakes, and returns a client ready to query.</summary>
    /// <remarks>
    /// The epoch comes from the handshake rather than from the caller: the daemon owns the store and
    /// is the only party that knows it. A caller-supplied epoch would be a guess, and the fence
    /// exists precisely to catch guesses.
    /// </remarks>
    public static async Task<WorkspaceClient> ConnectAsync(
        string pipeName, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var client = await IpcClient.ConnectAsync(pipeName, timeout, cancellationToken).ConfigureAwait(false);

        try
        {
            // Epoch 0 on the handshake because the handshake is the ONE exchange not judged
            // against the fence — a shell that has never spoken to this daemon has no way to know
            // the epoch, so it learns it here and states it on everything after.
            var opened = await client.OpenWorkspaceAsync(pipeName, 0, cancellationToken).ConfigureAwait(false);
            Throw(opened);

            return new WorkspaceClient(client, pipeName, client.Epoch);
        }
        catch
        {
            await client.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    public Task<DescribeResult> DescribeAsync(
        string nodeId, int maxNeighbors, CancellationToken cancellationToken) =>
        QueryAsync<DescribeResult>(
            WorkspaceOperations.Describe, new DescribeRequest(nodeId, maxNeighbors), cancellationToken);

    public Task<ImpactResult> ImpactAsync(
        string nodeId, int maxNodes, int maxEdges, CancellationToken cancellationToken) =>
        QueryAsync<ImpactResult>(
            WorkspaceOperations.Impact, new ImpactRequest(nodeId, maxNodes, maxEdges), cancellationToken);

    public Task<FindResult> FindAsync(string term, int maxResults, CancellationToken cancellationToken) =>
        QueryAsync<FindResult>(
            WorkspaceOperations.Find, new FindRequest(term, maxResults), cancellationToken);

    public Task<KnowledgeResult> KnowledgeAsync(
        string? term, string? type, int maxResults, CancellationToken cancellationToken) =>
        QueryAsync<KnowledgeResult>(
            WorkspaceOperations.Knowledge, new KnowledgeRequest(term, type, maxResults), cancellationToken);

    /// <summary>Re-reads the daemon's epoch, for a caller recovering from a stale-epoch rejection.</summary>
    public async Task<long> RefreshEpochAsync(CancellationToken cancellationToken)
    {
        var response = await _client
            .InvokeAsync(DaemonOperations.Epoch, Guid.NewGuid().ToString("N"), _workspaceId, _epoch, null, cancellationToken)
            .ConfigureAwait(false);

        Throw(response);
        _epoch = long.Parse(response.Payload!);
        return _epoch;
    }

    private async Task<TResult> QueryAsync<TResult>(
        string operation, object payload, CancellationToken cancellationToken)
    {
        var response = await _client.InvokeAsync(
            operation,
            Guid.NewGuid().ToString("N"),
            _workspaceId,
            _epoch,
            JsonSerializer.Serialize(payload, WorkspaceOperations.Wire),
            cancellationToken).ConfigureAwait(false);

        Throw(response);

        return JsonSerializer.Deserialize<TResult>(response.Payload!, WorkspaceOperations.Wire)
            ?? throw new IpcRequestException(
                IpcErrorCodes.MalformedEnvelope, $"the daemon returned no result for '{operation}'");
    }

    private static void Throw(IpcResponse response)
    {
        if (!response.Ok)
        {
            throw new IpcRequestException(response.ErrorCode!, response.Reason!);
        }
    }

    public ValueTask DisposeAsync() => _client.DisposeAsync();
}
