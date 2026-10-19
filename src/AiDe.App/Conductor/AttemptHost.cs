using System.IO;
using System.Text.Json.Nodes;
using AiDe.Core.AgentPlane;
using AiDe.Core.Dispatch;
using AiDe.Core.Facts;
using AiDe.Core.Ipc;
using AiDe.Core.Store;
using AiDe.Core.Store.Records;

namespace AiDe.App.Conductor;

/// <summary>
/// The execution-attempt host as the App binding sees it (ADR-0041; docs/design/workbench-loops-wl-1a.md).
/// </summary>
/// <remarks>
/// An interface over one class for one reason: T8 builds its binding in parallel and its UI tests need a host
/// with no engine behind it (the design's Simplifier condition 3). Every member answers with a receipt read back
/// from the store, never an in-memory guess.
/// </remarks>
public interface IAttemptHost
{
    /// <summary>
    /// Runs the gate and, when this call won it, launches the attempt. Returns once the gate decided; the run
    /// continues in the host and <see cref="AttemptStart.Completion"/> resolves after its terminal row committed.
    /// </summary>
    Task<AttemptStart> DispatchAsync(AttemptDispatch dispatch, CancellationToken cancellationToken);

    /// <summary>The attempt and its outcome events, or null when the key was never attempted.</summary>
    Task<AttemptReceipt?> ReadAsync(string dispatchKey, CancellationToken cancellationToken);
}

/// <summary>A prepared revision, ready for the gate.</summary>
/// <param name="Run">What the composition root runs.</param>
/// <param name="CurrentProjectionSha">The envelope fold's projection now — read at the gate (G1), never cached.</param>
/// <param name="RouteFingerprint">The route binding; <c>acp:&lt;engine-id&gt;</c> when the binding has none (WL-3 binds it).</param>
public sealed record AttemptDispatch(
    GovernedRunRequest Run,
    string WorkspaceId,
    long WorkspaceEpoch,
    string EnvelopeId,
    string ProjectionSha,
    string NotebookId,
    int RevisionNo,
    string SessionId,
    long SessionGeneration,
    Func<string?> CurrentProjectionSha,
    string? QueuedAfterDispatchKey = null,
    string? ParentDispatchKey = null,
    string? RouteFingerprint = null)
{
    /// <summary>The attempt's identity and its <c>run_id</c>: deterministic from the prepared envelope.</summary>
    public string DispatchKey => AttemptKey.For(EnvelopeId, ProjectionSha, QueuedAfterDispatchKey, ParentDispatchKey);

    /// <summary>The attempt row G0 writes.</summary>
    public AttemptRow ToRow() => new(
        DispatchKey, WorkspaceId, WorkspaceEpoch, NotebookId, RevisionNo, SessionId, SessionGeneration,
        ProjectionSha: ProjectionSha,
        RouteFingerprint: RouteFingerprint ?? "acp:" + Run.EngineId,
        ParentDispatchKey: ParentDispatchKey,
        QueuedAfterDispatchKey: QueuedAfterDispatchKey);
}

/// <param name="Receipt">The attempt as the gate left it.</param>
/// <param name="AlreadyAttempted">True when the key was already attempted or is already running here: nothing launched.</param>
/// <param name="Completion">The receipt after the terminal row committed; the existing receipt when nothing launched.</param>
public sealed record AttemptStart(AttemptReceipt Receipt, bool AlreadyAttempted, Task<AttemptReceipt> Completion);

/// <summary>The in-run write points <see cref="GovernedRunHost.RunAsync"/> takes for an attempt.</summary>
/// <param name="Ledger">Where the rows go.</param>
/// <param name="DispatchKey">The attempt — and the run's <c>run_id</c>.</param>
public sealed record AttemptRun(IAttemptLedger Ledger, string DispatchKey);

/// <summary>
/// The stateful attempt host: one per workspace, holding the live set and the gate choreography, and calling
/// <see cref="GovernedRunHost.RunAsync"/> — still the one composition root — for every launch.
/// </summary>
public sealed class AttemptHost : IAttemptHost, IDisposable
{
    private readonly IAttemptLedger _ledger;
    private readonly FileStream _hostLock;

    // The gate lock: held across L→G0 and across snapshot→sweep, never across a launch (design, "Error & concurrency").
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<string, object> _live = new(StringComparer.Ordinal);

    private AttemptHost(IAttemptLedger ledger, FileStream hostLock)
    {
        _ledger = ledger;
        _hostLock = hostLock;
    }

    /// <summary>The last sweep this host ran — at least the startup sweep.</summary>
    internal AttemptSweepResult? LastSweep { get; private set; }

    /// <summary>The attempts this host is running now.</summary>
    internal IReadOnlyCollection<string> Live
    {
        get
        {
            _gate.Wait();
            try
            {
                return [.. _live.Keys];
            }
            finally
            {
                _gate.Release();
            }
        }
    }

    /// <summary>
    /// Takes the workspace's host lock and runs the startup sweep (<c>host_restart</c>). A second host on the same
    /// workspace is refused here, so it can neither dispatch nor sweep another host's live attempts.
    /// </summary>
    /// <param name="hostLockFile"><c>&lt;workspace&gt;/.aide/attempt-host.lock</c>; the OS releases it when the process dies.</param>
    public static async Task<AttemptHost> OpenAsync(IAttemptLedger ledger, string hostLockFile, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        ArgumentException.ThrowIfNullOrEmpty(hostLockFile);

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(hostLockFile))!);
        FileStream hostLock;
        try
        {
            hostLock = new FileStream(hostLockFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException error)
        {
            throw new AgentPlaneException(AttemptErrorCodes.HostNotSole,
                $"another attempt host holds '{hostLockFile}' ({error.Message}); a second host can neither dispatch nor sweep");
        }

        var host = new AttemptHost(ledger, hostLock);
        await host.SweepAsync(AttemptCauses.HostRestart, cancellationToken).ConfigureAwait(false);
        return host;
    }

    public async Task<AttemptStart> DispatchAsync(AttemptDispatch dispatch, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dispatch);

        if (dispatch.QueuedAfterDispatchKey is not null)
        {
            // The queue's launch path (live from queue time, launched after the predecessor's ResponseComplete) is
            // WL-1a node 2; refused before any write rather than launched at once, which would skip the predecessor.
            throw new NotSupportedException("a queued dispatch is WL-1a node 2 (docs/proof/workbench-loops-wl-1a/host/handoff.md)");
        }

        var key = dispatch.DispatchKey;
        var owner = new object();

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // L before G0, so a launch in progress is never swept. Already live: a duplicate in flight — launch
            // nothing and touch nothing; the running owner's entry stays (only it deregisters).
            if (!_live.TryAdd(key, owner))
            {
                var running = await _ledger.ReadAttemptAsync(key, cancellationToken).ConfigureAwait(false);
                return Unlaunched(running!);
            }

            AttemptBeginResult begun;
            try
            {
                begun = await _ledger.BeginAttemptAsync(dispatch.ToRow(), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception error) when (error is IOException or OperationCanceledException or WorkspaceStoreException)
            {
                _live.Remove(key);
                throw;
            }

            if (begun.AlreadyAttempted)
            {
                _live.Remove(key);
                return Unlaunched(begun.Receipt);
            }
        }
        finally
        {
            _gate.Release();
        }

        // G1: the envelope's projection NOW against the one the attempt was prepared from (AC-E04b). No process.
        if (!string.Equals(dispatch.CurrentProjectionSha(), dispatch.ProjectionSha, StringComparison.Ordinal))
        {
            var rejected = await _ledger.AppendAttemptOutcomeAsync(
                new OutcomeRow(key, DispatchState.Rejected, AttemptErrorCodes.StalePrepare, Detail: "rejected_stale"),
                cancellationToken).ConfigureAwait(false);
            await DeregisterAsync(key, owner).ConfigureAwait(false);
            return new AttemptStart(rejected.Receipt, AlreadyAttempted: false, Task.FromResult(rejected.Receipt));
        }

        var started = (await _ledger.ReadAttemptAsync(key, cancellationToken).ConfigureAwait(false))!;
        return new AttemptStart(started, AlreadyAttempted: false, LaunchAsync(dispatch, key, owner));
    }

    private static AttemptStart Unlaunched(AttemptReceipt receipt) =>
        new(receipt, AlreadyAttempted: true, Task.FromResult(receipt));

    public Task<AttemptReceipt?> ReadAsync(string dispatchKey, CancellationToken cancellationToken) =>
        _ledger.ReadAttemptAsync(dispatchKey, cancellationToken);

    /// <summary>Resolves abandoned attempts; the live-set snapshot and the sweep are one critical section with L→G0.</summary>
    internal async Task<AttemptSweepResult> SweepAsync(string dispatchedCause, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            LastSweep = await _ledger.SweepAttemptsAsync(new AttemptSweepRequest([.. _live.Keys], dispatchedCause), cancellationToken)
                .ConfigureAwait(false);
            return LastSweep;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>E → terminal for an attempt whose G0 this call won.</summary>
    /// <remarks>
    /// Only the four exception types a run can end with are mapped to a terminal. Anything else — a process death
    /// among them — propagates and leaves the rows a crash would leave, for the next host's sweep.
    /// </remarks>
    private async Task<AttemptReceipt> LaunchAsync(AttemptDispatch dispatch, string key, object owner)
    {
        try
        {
            await GovernedRunHost.RunAsync(dispatch.Run, CancellationToken.None, sink: null, attempt: new AttemptRun(_ledger, key))
                .ConfigureAwait(false);
        }
        catch (Exception error) when (error is AgentPlaneException or IOException or OperationCanceledException or WorkspaceStoreException)
        {
            await SettleAfterThrowAsync(key, error).ConfigureAwait(false);
        }
        finally
        {
            await DeregisterAsync(key, owner).ConfigureAwait(false);
        }

        return (await _ledger.ReadAttemptAsync(key, CancellationToken.None).ConfigureAwait(false))!;
    }

    /// <summary>The terminal for a run that threw: before G2 nothing was sent (<c>Failed</c>), after it the effects are unknown.</summary>
    private async Task SettleAfterThrowAsync(string key, Exception error)
    {
        var receipt = await _ledger.ReadAttemptAsync(key, CancellationToken.None).ConfigureAwait(false);
        if (receipt is null || receipt.Terminal is not null)
        {
            return;
        }

        var code = error is AgentPlaneException plane ? plane.Code : error.GetType().Name;
        var terminal = receipt.Dispatched is null
            ? new OutcomeRow(key, DispatchState.Failed, AttemptErrorCodes.NotDispatched, Detail: code)
            : new OutcomeRow(key, DispatchState.DeliveryUnknown, DispatchErrorCodes.DeliveryUnknown, AttemptCauses.RelayLost, Detail: code);
        await _ledger.AppendAttemptOutcomeAsync(terminal, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>D: only the owner that won G0 removes the entry, and only after the terminal write.</summary>
    private async Task DeregisterAsync(string key, object owner)
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_live.TryGetValue(key, out var held) && ReferenceEquals(held, owner))
            {
                _live.Remove(key);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Releases the host lock — what a process death does, so a restarted host can take it.</summary>
    public void Dispose()
    {
        _hostLock.Dispose();
        _gate.Dispose();
    }
}

/// <summary>The <c>DeliveryUnknown</c> causes the host writes (one vocabulary: <see cref="WorkbenchVocabulary.DeliveryUnknownCauses"/>).</summary>
internal static class AttemptCauses
{
    public const string NeverDispatched = "never_dispatched";
    public const string RelayLost = "relay_lost";
    public const string PromptTimeout = "prompt_timeout";
    public const string HostRestart = "host_restart";
}

/// <summary>The terminal an awaited prompt maps to (ADR-0041 §2) — the host's own correlated reply, nothing else.</summary>
internal static class AttemptTerminal
{
    public static OutcomeRow For(string key, Task<JsonObject> prompt)
    {
        ArgumentNullException.ThrowIfNull(prompt);

        if (prompt.IsCompletedSuccessfully)
        {
            var stopReason = (prompt.Result["stopReason"] as JsonValue)?.TryGetValue<string>(out var reason) == true ? reason : null;
            var detail = new JsonObject
            {
                ["stopReason"] = stopReason,
                ["usage"] = prompt.Result["usage"]?.DeepClone() ?? JsonValue.Create(GovernedRunHost.NotRecorded),
            }.ToJsonString();

            // A cancelled reply is Interrupted only under a Stop whose high-water mark it follows (§4, node 2);
            // with no Stop requested it is not our acknowledgment, so its effects are unknown.
            return stopReason == "cancelled"
                ? new OutcomeRow(key, DispatchState.DeliveryUnknown, DispatchErrorCodes.DeliveryUnknown, AttemptCauses.RelayLost, detail)
                : new OutcomeRow(key, DispatchState.ResponseComplete, Detail: detail);
        }

        var error = prompt.Exception?.GetBaseException();
        return error switch
        {
            AgentPlaneException { Code: AgentPlaneErrorCodes.EngineRequestTimedOut } timedOut =>
                new OutcomeRow(key, DispatchState.DeliveryUnknown, DispatchErrorCodes.DeliveryUnknown, AttemptCauses.PromptTimeout, timedOut.Code),
            AgentPlaneException { Code: AgentPlaneErrorCodes.EngineStreamEnded } ended =>
                new OutcomeRow(key, DispatchState.DeliveryUnknown, DispatchErrorCodes.DeliveryUnknown, AttemptCauses.RelayLost, ended.Code),
            AgentPlaneException refused => new OutcomeRow(key, DispatchState.Failed, refused.Code),
            _ => new OutcomeRow(key, DispatchState.DeliveryUnknown, DispatchErrorCodes.DeliveryUnknown, AttemptCauses.RelayLost,
                error?.GetType().Name ?? "cancelled"),
        };
    }
}
