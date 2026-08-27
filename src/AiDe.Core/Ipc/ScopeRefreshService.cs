using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

namespace AiDe.Core.Ipc;

/// <summary>Where a scope refresh has got to.</summary>
public enum ScopeRefreshState
{
    /// <summary>Extraction is under way. Nothing has been committed yet.</summary>
    Running,

    /// <summary>A complete snapshot was committed.</summary>
    Completed,

    /// <summary>Extraction did not complete. The previous snapshot still renders.</summary>
    Failed,
}

/// <summary>What a caller learns about a refresh.</summary>
/// <remarks>
/// <see cref="Failure"/> is populated on <see cref="ScopeRefreshState.Failed"/> and states why. A
/// refresh that failed silently would leave the last good snapshot rendering with nothing to say it
/// is now stale — which is the "clean empty success over rotting evidence" this product exists to
/// avoid.
/// </remarks>
public sealed record ScopeRefreshStatus(
    string CommandId,
    string ScopeId,
    ScopeRefreshState State,
    int AssertionCount,
    string? Failure);

/// <summary>Asking the daemon to re-index a scope.</summary>
public sealed record RefreshRequest(string ScopeId, string ArtifactRevision);

/// <summary>Asking how a previously started refresh is doing.</summary>
public sealed record RefreshStatusRequest(string CommandId);

/// <summary>
/// Re-indexing a scope, across the boundary.
/// </summary>
/// <remarks>
/// <para><b>Started and polled, never awaited on the wire.</b> A scope has a 60-second budget and
/// the IPC lane serves one request at a time per connection — so a refresh that answered only when
/// it finished would hold that connection for a minute, and the daemon's response-write timeout
/// would abandon it long before. The control lane carries <i>commands</i>; a command that starts
/// long work returns as soon as the work is started.</para>
///
/// <para><b>The command id is the idempotency key</b>, exactly as the architecture's command
/// protocol specifies. Re-sending the same id returns the same job rather than starting a second
/// extraction — which matters most in the case it exists for: a client that did not see the reply
/// and retried. Two extractions of one scope would both bump the generation and the loser's work
/// would be discarded, having cost a full budget.</para>
///
/// <para><b>Nothing here re-implements ingestion.</b> The generation fence, the incomplete-result
/// handling and the snapshot commit all stay in <see cref="WorkspaceCore"/>; this decides only what
/// crossing the boundary means.</para>
/// </remarks>
public sealed class ScopeRefreshService
{
    private static readonly ActivitySource Telemetry = new("aide.ipc.command");

    /// <summary>Finished jobs kept for collection by a client that has not asked yet.</summary>
    /// <remarks>
    /// Bounded, because job records are keyed by a caller-chosen id and an unbounded map is a memory
    /// leak any client can drive. Oldest completed jobs are dropped first: a status nobody collected
    /// within this many refreshes is one nobody is waiting for.
    /// simplify: a flat cap with oldest-first eviction rather than a time-based expiry; ceiling 256
    /// retained jobs; upgrade trigger = a client legitimately polls for a result later than that.
    /// </remarks>
    private const int RetainedJobs = 256;

    private readonly ConcurrentDictionary<string, ScopeRefreshStatus> _jobs = new(StringComparer.Ordinal);
    private readonly ConcurrentQueue<string> _order = new();
    private readonly Func<string, string, CancellationToken, Task<int>> _refresh;

    /// <param name="refresh">
    /// Runs the extraction and returns the assertion count. Injected rather than taking a
    /// <see cref="WorkspaceCore"/> so the boundary's behaviour — idempotency, retention, what a
    /// failure looks like — is testable without standing up a store and an extractor.
    /// </param>
    public ScopeRefreshService(Func<string, string, CancellationToken, Task<int>> refresh) =>
        _refresh = refresh ?? throw new ArgumentNullException(nameof(refresh));

    /// <summary>Jobs currently held, running or finished.</summary>
    public int TrackedJobs => _jobs.Count;

    /// <summary>Starts a refresh, or returns the job this command id already started.</summary>
    public ScopeRefreshStatus Start(string commandId, string scopeId, string artifactRevision)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);
        ArgumentException.ThrowIfNullOrWhiteSpace(scopeId);

        // A fast path, not the control. Deleting it changes nothing observable, because the TryAdd
        // below refuses a duplicate anyway and returns the same job — which is where deduplication
        // actually happens. Kept because it avoids allocating a status for the common retry, and
        // labelled so nobody mistakes it for the guard (DC-016: know which of your checks can fire).
        if (_jobs.TryGetValue(commandId, out var existing))
        {
            return existing;
        }

        var started = new ScopeRefreshStatus(commandId, scopeId, ScopeRefreshState.Running, 0, null);

        if (!_jobs.TryAdd(commandId, started))
        {
            // Lost a race with an identical retry. The winner's job is the answer — starting a
            // second extraction here is the exact duplication the idempotency key prevents.
            return _jobs[commandId];
        }

        _order.Enqueue(commandId);
        Evict();

        _ = RunAsync(commandId, scopeId, artifactRevision);
        return started;
    }

    /// <summary>How a refresh is doing, or <c>null</c> if this daemon has no record of it.</summary>
    /// <remarks>
    /// Null rather than a synthesised "unknown" state: a job this daemon never started, or one it
    /// has since evicted, are both "I cannot tell you", and inventing a status would let a caller
    /// wait for a result that is never coming.
    /// </remarks>
    public ScopeRefreshStatus? Status(string commandId) =>
        _jobs.TryGetValue(commandId, out var status) ? status : null;

    private async Task RunAsync(string commandId, string scopeId, string artifactRevision)
    {
        using var span = Telemetry.StartActivity("ipc.scope_refresh");
        span?.SetTag("scope.id", scopeId);
        span?.SetTag("command.id", commandId);

        try
        {
            var count = await _refresh(scopeId, artifactRevision, CancellationToken.None)
                .ConfigureAwait(false);

            _jobs[commandId] = new ScopeRefreshStatus(
                commandId, scopeId, ScopeRefreshState.Completed, count, null);
            span?.SetTag("assertion.count", count);
        }
        catch (Exception ex)
        {
            // Any exception, and it is recorded rather than rethrown: this runs detached from the
            // request that started it, so an escaping exception would take down the daemon on behalf
            // of a caller who is no longer listening. The failure belongs in the status the caller
            // WILL ask for.
            _jobs[commandId] = new ScopeRefreshStatus(
                commandId, scopeId, ScopeRefreshState.Failed, 0, ex.Message);
            span?.SetStatus(ActivityStatusCode.Error, ex.Message);
        }
    }

    private void Evict()
    {
        while (_jobs.Count > RetainedJobs && _order.TryDequeue(out var oldest))
        {
            // A running job is never evicted: its status is the only record that it is happening,
            // and dropping it would report an in-flight extraction as one this daemon never heard of.
            if (_jobs.TryGetValue(oldest, out var status) && status.State == ScopeRefreshState.Running)
            {
                _order.Enqueue(oldest);
                return;
            }

            _jobs.TryRemove(oldest, out _);
        }
    }

    /// <summary>Registers refresh and its status query on the endpoint.</summary>
    public void Register(DaemonEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        endpoint.Register(Operations.Refresh, (request, _) =>
        {
            var body = Decode<RefreshRequest>(request);
            return body is null
                ? IpcResponse.Error(IpcErrorCodes.MalformedEnvelope, "refresh requires a scope and a revision")
                : IpcResponse.Success(JsonSerializer.Serialize(
                    Start(request.CommandId, body.ScopeId, body.ArtifactRevision),
                    WorkspaceOperations.Wire));
        });

        endpoint.Register(Operations.RefreshStatus, (request, _) =>
        {
            var body = Decode<RefreshStatusRequest>(request);
            if (body is null)
            {
                return IpcResponse.Error(IpcErrorCodes.MalformedEnvelope, "a command id is required");
            }

            var status = Status(body.CommandId);
            return status is null
                ? IpcResponse.Error(
                    IpcErrorCodes.CommandUnknown,
                    $"this daemon has no record of command '{body.CommandId}'")
                : IpcResponse.Success(JsonSerializer.Serialize(status, WorkspaceOperations.Wire));
        });
    }

    private static T? Decode<T>(IpcRequest request)
    {
        if (request.Payload is null)
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(request.Payload, WorkspaceOperations.Wire);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    /// <summary>The operation names, so both ends spell them the same way.</summary>
    public static class Operations
    {
        public const string Refresh = "refresh";
        public const string RefreshStatus = "refresh.status";
    }
}
