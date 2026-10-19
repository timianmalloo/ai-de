using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using AiDe.Core.AgentPlane;
using AiDe.Core.Dispatch;
using AiDe.Core.Facts;
using AiDe.Core.Health;
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
    /// Runs the gate and, when this call won it, launches the attempt — at once, or, for a queued successor
    /// (<see cref="AttemptDispatch.QueuedAfterDispatchKey"/>), after its predecessor's <c>ResponseComplete</c>.
    /// Returns once the gate decided; <see cref="AttemptStart.Completion"/> resolves after the terminal row committed.
    /// </summary>
    Task<AttemptStart> DispatchAsync(AttemptDispatch dispatch, CancellationToken cancellationToken);

    /// <summary>The attempt and its outcome events, or null when the key was never attempted.</summary>
    Task<AttemptReceipt?> ReadAsync(string dispatchKey, CancellationToken cancellationToken);

    /// <summary>
    /// Launches a held queued successor now — the explicit move of "Queued — awaits you". One launch however many
    /// callers race (and the drainer with them); only a still-<c>Pending</c> attempt is launched.
    /// </summary>
    Task<AttemptStart> SendNowAsync(string dispatchKey, CancellationToken cancellationToken);

    /// <summary>
    /// Stops a running attempt (ADR-0041 §4) and returns the receipt after its terminal row committed:
    /// <c>Interrupted</c> only on the engine's own <c>cancelled</c> reply after the Stop's mark, else
    /// <c>DeliveryUnknown{stop_not_acknowledged}</c> at the deadline. Never cancels a queued successor.
    /// </summary>
    /// <exception cref="KeyNotFoundException">The key was never attempted.</exception>
    Task<AttemptReceipt> StopAsync(string dispatchKey, CancellationToken cancellationToken);

    /// <summary>
    /// Withdraws a held queued successor before it launches (Ruling 95's Cancel on a host-held turn): under the gate
    /// lock it takes the one launch claim — so neither the drainer nor Send now launches it — and the store writes the
    /// existing terminal <c>Rejected{never_dispatched}</c> with the withdraw command's receipt. No process starts.
    /// </summary>
    /// <exception cref="AgentPlaneException"><see cref="AttemptHostCodes.WithdrawRefused"/>: not a successor this host holds, or its
    /// launch was already claimed (the drainer or Send now won) — nothing changes and a launched attempt stays live.</exception>
    Task<AttemptReceipt> WithdrawAsync(string dispatchKey, CancellationToken cancellationToken);

    /// <summary>The attempt's events: the run log so far, then the live relay, each seq once and in order.</summary>
    /// <exception cref="KeyNotFoundException">The key was never attempted.</exception>
    Task<AttemptSubscription> SubscribeAsync(string dispatchKey, CancellationToken cancellationToken);

    /// <summary>The attempt's NFR-11 measures, derived from its rows and its run log (ADR-0041 §7).</summary>
    /// <exception cref="KeyNotFoundException">The key was never attempted.</exception>
    Task<AttemptMeasures> MeasureAsync(string dispatchKey, CancellationToken cancellationToken);
}

/// <summary>What the host is configured with.</summary>
/// <param name="WorkspaceRoot">The workspace: run logs at <c>&lt;ws&gt;/.aide/sessions/&lt;id&gt;/runs/</c>, the rolling log at <c>&lt;ws&gt;/.aide/attempt-telemetry.jsonl</c>.</param>
public sealed record AttemptHostOptions(string WorkspaceRoot)
{
    /// <summary>How long a Stop waits for the engine's <c>cancelled</c> reply (ADR-0041 §4: 10 s, config).</summary>
    public TimeSpan StopDeadline { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>The backstop sweep's interval (ADR-0041 §4: 30 s).</summary>
    public TimeSpan SweepBackstop { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Where an unwritable run log is reported; null reports nowhere.</summary>
    public HealthIncidentSidecar? Incidents { get; init; }

    /// <summary>Opens an attempt's run log from <c>(session_id, dispatch_key)</c>; null opens <see cref="RunLogStore.Open"/> under <see cref="WorkspaceRoot"/>.</summary>
    public Func<string, string, RunLogStore>? RunLogs { get; init; }

    /// <summary>The rolling log the host's metrics go to.</summary>
    public string TelemetryLog => Path.Combine(WorkspaceRoot, ".aide", "attempt-telemetry.jsonl");
}

/// <summary>One subscriber's view of one attempt's events (design, "Relay": a late subscriber catches up by <c>seq &gt; last_seen</c>).</summary>
/// <remarks>
/// The relay channel is taken <b>before</b> the run log is read, and the log is written before every publish, so
/// nothing falls between the two: an event is in the log, in the channel, or both — and "both" is skipped by seq.
/// </remarks>
public sealed class AttemptSubscription
{
    private readonly string _runLog;
    private readonly ChannelReader<ObservedRunEvent>? _live;

    internal AttemptSubscription(string runLog, ChannelReader<ObservedRunEvent>? live)
    {
        _runLog = runLog;
        _live = live;
    }

    /// <summary>Every event, each seq once, in seq order; ends when the run ends.</summary>
    public async IAsyncEnumerable<RunEvent> ReadAllAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        long last = 0;
        foreach (var logged in RunLogStore.Read(_runLog))
        {
            last = logged.Seq;
            yield return logged;
        }

        if (_live is null)
        {
            yield break;
        }

        await foreach (var observed in _live.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            if (observed.Event.Seq > last)
            {
                last = observed.Event.Seq;
                yield return observed.Event;
            }
        }
    }
}

/// <summary>NFR-11 (ADR-0041 §7): every value measured from a row or the run log, or null when not recorded.</summary>
/// <param name="Usage">The terminal's <c>usage</c> as JSON, or the JSON string <c>"not recorded"</c>.</param>
public sealed record AttemptMeasures(
    string DispatchKey,
    DateTimeOffset? DispatchedAt,
    DateTimeOffset? TerminalAt,
    TimeSpan? Duration,
    long Events,
    long Bytes,
    string? Outcome,
    string Usage);

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
public sealed record AttemptRun(IAttemptLedger Ledger, string DispatchKey)
{
    /// <summary>The attempt's durable run log; every event is appended before it is published. Null: no run log.</summary>
    public RunLogStore? RunLog { get; init; }

    /// <summary>The Stop handshake between the host and this run's own settle.</summary>
    internal AttemptStop Stop { get; init; } = new();
}

/// <summary>
/// The Stop handshake of one attempt (ADR-0041 §4), between <see cref="AttemptHost.StopAsync"/> and the run's own
/// settle: the mark the Stop was taken at, the line the run bound for it, the engine's acknowledgment, the deadline.
/// </summary>
internal sealed class AttemptStop
{
    private readonly Lock _gate = new();
    private readonly TaskCompletionSource<BoundLine> _bound = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private long? _highWater;
    private long _cancelledReplySeq;
    private bool _deadlinePassed;
    private int _permissionsRecorded;

    /// <summary>What a Stop acts on: the client, the ACP session and the awaited prompt.</summary>
    internal sealed record BoundLine(AcpLaneClient Client, string Session, Task<JsonObject> Prompt);

    internal Task<BoundLine> Bound => _bound.Task;

    /// <summary>True once the Stop's deadline passed with the prompt still open: the terminal is <c>stop_not_acknowledged</c>, unconditionally.</summary>
    internal bool DeadlinePassed
    {
        get { lock (_gate) { return _deadlinePassed; } }
    }

    /// <summary>True when the engine's <c>cancelled</c> reply carries a seq above the Stop's mark — the only <c>Interrupted</c>.</summary>
    internal bool AcknowledgedAfterMark
    {
        get { lock (_gate) { return _highWater is { } mark && _cancelledReplySeq > mark; } }
    }

    /// <summary>Records the Stop's mark; false when a Stop was already requested.</summary>
    internal bool TryRequest(long highWaterSeq)
    {
        lock (_gate)
        {
            if (_highWater is not null)
            {
                return false;
            }

            _highWater = highWaterSeq;
            return true;
        }
    }

    /// <summary>Before G2: a Stop that came first means no prompt is sent (nothing was dispatched, so <c>Failed</c>).</summary>
    internal void RefuseIfRequested(string dispatchKey)
    {
        lock (_gate)
        {
            if (_highWater is not null)
            {
                throw new AgentPlaneException(AttemptErrorCodes.NotDispatched, $"attempt {dispatchKey}: stopped before dispatch; no prompt is sent");
            }
        }
    }

    /// <summary>After the prompt is written: what a Stop sends <c>session/cancel</c> on and waits for.</summary>
    internal void Bind(AcpLaneClient client, string session, Task<JsonObject> prompt) =>
        _bound.TrySetResult(new BoundLine(client, session, prompt));

    /// <summary>Every drained event passes here: the prompt's <c>cancelled</c> reply is the acknowledgment, by its seq.</summary>
    internal void Observe(RunEvent runEvent)
    {
        if (runEvent.Kind == "acp.result"
            && (runEvent.Ext["result"]?["stopReason"] as JsonValue)?.TryGetValue<string>(out var reason) == true
            && reason == "cancelled")
        {
            lock (_gate)
            {
                _cancelledReplySeq = runEvent.Seq;
            }
        }
    }

    /// <summary>At the deadline: true (and the terminal fixed) only when the prompt is still open.</summary>
    internal bool TryPassDeadline(Task prompt)
    {
        lock (_gate)
        {
            if (prompt.IsCompleted)
            {
                return false;
            }

            _deadlinePassed = true;
            return true;
        }
    }

    /// <summary>The permission requests answered <c>cancelled</c> since the last call — each is recorded once.</summary>
    internal IReadOnlyList<JsonObject> TakeCancelledPermissions()
    {
        if (!_bound.Task.IsCompletedSuccessfully)
        {
            return [];
        }

        var all = _bound.Task.Result.Client.CancelledPermissions.ToArray();
        lock (_gate)
        {
            var fresh = all[_permissionsRecorded..];
            _permissionsRecorded = all.Length;
            return fresh;
        }
    }
}

/// <summary>
/// The stateful attempt host: one per workspace, holding the live set, the gate choreography, the queue, Stop and
/// the subscriptions, and calling <see cref="GovernedRunHost.RunAsync"/> — still the one composition root — for
/// every launch.
/// </summary>
public sealed class AttemptHost : IAttemptHost, IDisposable
{
    /// <summary>The activity source T6's fold reports on; the host turns each stopped activity into <c>fold.duration_ms</c>.</summary>
    public const string FoldActivitySourceName = "aide.attempt.fold";

    private readonly IAttemptLedger _ledger;
    private readonly FileStream _hostLock;
    private readonly AttemptHostOptions _options;
    private readonly AttemptRollingLog _telemetry;
    private readonly ActivityListener _foldListener;
    private readonly CancellationTokenSource _closing = new();

    // The gate lock: held across L→G0 and across snapshot→sweep, never across a launch (design, "Error & concurrency").
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<string, LiveAttempt> _live = new(StringComparer.Ordinal);

    private AttemptHost(IAttemptLedger ledger, FileStream hostLock, AttemptHostOptions options)
    {
        _ledger = ledger;
        _hostLock = hostLock;
        _options = options;
        _telemetry = new AttemptRollingLog(options.TelemetryLog);
        _foldListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == FoldActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => _telemetry.Write("fold.duration_ms", new JsonObject
            {
                ["value"] = activity.Duration.TotalMilliseconds,
                ["operation"] = activity.OperationName,
            }),
        };
        ActivitySource.AddActivityListener(_foldListener);
    }

    /// <summary>The last sweep this host ran — at least the startup sweep.</summary>
    internal AttemptSweepResult? LastSweep { get; private set; }

    /// <summary>
    /// Completes when the attempt's launch has fully ended — run log disposed, entry deregistered — whether it ended in a
    /// terminal or threw; already complete when the host holds no such attempt. A test seam for the in-process kill.
    /// </summary>
    internal async Task LaunchSettledAsync(string dispatchKey)
    {
        Task settled;
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            settled = _live.TryGetValue(dispatchKey, out var live) ? live.Done.Task : Task.CompletedTask;
        }
        finally
        {
            _gate.Release();
        }

        await settled.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }

    /// <summary>The attempts this host is running now (queued successors among them, from queue time).</summary>
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
    /// Takes the workspace's host lock, runs the startup sweep (<c>host_restart</c>) and starts the 30 s backstop.
    /// A second host on the same workspace is refused here, so it can neither dispatch nor sweep another host's live
    /// attempts.
    /// </summary>
    /// <param name="hostLockFile"><c>&lt;workspace&gt;/.aide/attempt-host.lock</c>; the OS releases it when the process dies.</param>
    /// <param name="options">Null: the workspace is the lock file's <c>&lt;workspace&gt;</c> (the design fixes the lock's place) and every default holds.</param>
    public static async Task<AttemptHost> OpenAsync(IAttemptLedger ledger, string hostLockFile, CancellationToken cancellationToken, AttemptHostOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        ArgumentException.ThrowIfNullOrEmpty(hostLockFile);

        var lockPath = Path.GetFullPath(hostLockFile);
        Directory.CreateDirectory(Path.GetDirectoryName(lockPath)!);
        FileStream hostLock;
        try
        {
            hostLock = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException error)
        {
            throw new AgentPlaneException(AttemptErrorCodes.HostNotSole,
                $"another attempt host holds '{hostLockFile}' ({error.Message}); a second host can neither dispatch nor sweep");
        }

        var host = new AttemptHost(ledger, hostLock, options ?? new AttemptHostOptions(Path.GetDirectoryName(Path.GetDirectoryName(lockPath))!));
        await host.SweepAsync(AttemptCauses.HostRestart, cancellationToken, SweepTriggers.HostStartup).ConfigureAwait(false);
        _ = host.BackstopAsync();
        return host;
    }

    public async Task<AttemptStart> DispatchAsync(AttemptDispatch dispatch, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dispatch);

        var key = dispatch.DispatchKey;
        var live = new LiveAttempt(dispatch);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // L before G0, so a launch in progress — or a queued successor — is never swept. Already live: a
            // duplicate in flight — launch nothing and touch nothing; the running owner's entry stays.
            if (!_live.TryAdd(key, live))
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
            await DeregisterAsync(key, live).ConfigureAwait(false);
            return new AttemptStart(rejected.Receipt, AlreadyAttempted: false, Task.FromResult(rejected.Receipt));
        }

        var started = (await _ledger.ReadAttemptAsync(key, cancellationToken).ConfigureAwait(false))!;
        if (dispatch.QueuedAfterDispatchKey is { } predecessor)
        {
            _ = DrainQueuedAsync(live, predecessor);
        }
        else
        {
            await TryLaunchAsync(live).ConfigureAwait(false);
        }

        return new AttemptStart(started, AlreadyAttempted: false, live.Done.Task);
    }

    public Task<AttemptReceipt?> ReadAsync(string dispatchKey, CancellationToken cancellationToken) =>
        _ledger.ReadAttemptAsync(dispatchKey, cancellationToken);

    public async Task<AttemptStart> SendNowAsync(string dispatchKey, CancellationToken cancellationToken)
    {
        var live = await FindLiveAsync(dispatchKey, cancellationToken).ConfigureAwait(false);
        if (live?.Dispatch.QueuedAfterDispatchKey is null)
        {
            return Unlaunched(await ReadKnownAsync(dispatchKey, cancellationToken).ConfigureAwait(false));
        }

        var launched = await TryLaunchAsync(live).ConfigureAwait(false);
        var receipt = await ReadKnownAsync(dispatchKey, cancellationToken).ConfigureAwait(false);
        return new AttemptStart(receipt, AlreadyAttempted: !launched, live.Done.Task);
    }

    public async Task<AttemptReceipt> StopAsync(string dispatchKey, CancellationToken cancellationToken)
    {
        var live = await FindLiveAsync(dispatchKey, cancellationToken).ConfigureAwait(false);
        if (live is null || !live.Launched)
        {
            // Not running here: already concluded, or a held successor — Stop never cancels a queued successor.
            return await ReadKnownAsync(dispatchKey, cancellationToken).ConfigureAwait(false);
        }

        var mark = live.Relay.HighWaterSeq;
        if (live.Stop.TryRequest(mark))
        {
            await _ledger.AppendAttemptOutcomeAsync(
                new OutcomeRow(dispatchKey, DispatchState.StopRequested, Detail: new JsonObject { ["high_water_seq"] = mark }.ToJsonString()),
                cancellationToken).ConfigureAwait(false);

            // The run binds its line after the prompt is written; a run that ends first (or refuses at G2) never does.
            if (await Task.WhenAny(live.Stop.Bound, live.Done.Task).ConfigureAwait(false) == live.Stop.Bound)
            {
                var line = await live.Stop.Bound.ConfigureAwait(false);
                try
                {
                    await line.Client.CancelAsync(line.Session, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception error) when (error is IOException or ObjectDisposedException)
                {
                    // The engine is already gone; the prompt fails on its own and the deadline still bounds the wait.
                }

                using var deadline = new CancellationTokenSource(_options.StopDeadline);
                await ((Task)line.Prompt).WaitAsync(deadline.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
                if (live.Stop.TryPassDeadline(line.Prompt))
                {
                    await live.Cancel.CancelAsync().ConfigureAwait(false);
                }
            }
        }

        return await live.Done.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<AttemptReceipt> WithdrawAsync(string dispatchKey, CancellationToken cancellationToken)
    {
        LiveAttempt? live;
        AttemptAppendResult withdrawn;
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            live = _live.GetValueOrDefault(dispatchKey);
            if (live?.Dispatch.QueuedAfterDispatchKey is null || !live.TryClaimLaunch())
            {
                throw new AgentPlaneException(AttemptHostCodes.WithdrawRefused,
                    $"attempt {dispatchKey}: not a queued successor held by this host, or already launched; nothing changed");
            }

            withdrawn = await _ledger.WithdrawAttemptAsync(dispatchKey, CancellationToken.None).ConfigureAwait(false);
            _live.Remove(dispatchKey);
        }
        finally
        {
            _gate.Release();
        }

        live.Relay.Complete();
        live.Done.TrySetResult(withdrawn.Receipt);
        return withdrawn.Receipt.IsWithdrawn
            ? withdrawn.Receipt
            : throw new AgentPlaneException(AttemptHostCodes.WithdrawRefused,
                $"attempt {dispatchKey}: already concluded {withdrawn.Receipt.Terminal?.State}; nothing changed");
    }

    public async Task<AttemptSubscription> SubscribeAsync(string dispatchKey, CancellationToken cancellationToken)
    {
        var live = await FindLiveAsync(dispatchKey, cancellationToken).ConfigureAwait(false);
        if (live is not null)
        {
            // The channel first, then the log path: see AttemptSubscription's remarks.
            var reader = live.Relay.Subscribe();
            return new AttemptSubscription(live.RunLogPath ?? DefaultRunLogPath(live.Dispatch.SessionId, dispatchKey), reader);
        }

        var receipt = await ReadKnownAsync(dispatchKey, cancellationToken).ConfigureAwait(false);
        return new AttemptSubscription(DefaultRunLogPath(receipt.Attempt.SessionId, dispatchKey), live: null);
    }

    public async Task<AttemptMeasures> MeasureAsync(string dispatchKey, CancellationToken cancellationToken)
    {
        var receipt = await ReadKnownAsync(dispatchKey, cancellationToken).ConfigureAwait(false);
        var runLog = DefaultRunLogPath(receipt.Attempt.SessionId, dispatchKey);
        var dispatchedAt = Timestamp(receipt.Dispatched?.RecordedAt);
        var terminalAt = Timestamp(receipt.Terminal?.RecordedAt);
        var usage = ParseDetail(receipt.Terminal?.Detail)?["usage"]?.ToJsonString()
            ?? JsonSerializer.Serialize(GovernedRunHost.NotRecorded);

        return new AttemptMeasures(
            dispatchKey,
            dispatchedAt,
            terminalAt,
            dispatchedAt is { } from && terminalAt is { } to ? to - from : null,
            RunLogStore.Read(runLog).Count,
            File.Exists(runLog) ? new FileInfo(runLog).Length : 0,
            receipt.Terminal?.State.ToString(),
            usage);
    }

    private static AttemptStart Unlaunched(AttemptReceipt receipt) =>
        new(receipt, AlreadyAttempted: true, Task.FromResult(receipt));

    /// <summary>Resolves abandoned attempts; the live-set snapshot and the sweep are one critical section with L→G0.</summary>
    /// <remarks>Every sweep emits <c>attempt.sweep{count, last_sweep_at, trigger}</c> to the rolling log, zero included (ADR-0041 §4).</remarks>
    internal async Task<AttemptSweepResult> SweepAsync(string dispatchedCause, CancellationToken cancellationToken, string trigger = SweepTriggers.Manual)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            LastSweep = await _ledger.SweepAttemptsAsync(new AttemptSweepRequest([.. _live.Keys], dispatchedCause), cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }

        _telemetry.Write("attempt.sweep", new JsonObject
        {
            ["count"] = LastSweep.Swept.Count,
            ["last_sweep_at"] = LastSweep.SweptAt,
            ["trigger"] = trigger,
        });
        return LastSweep;
    }

    /// <summary>The 30 s backstop (ADR-0041 §4): a dispatched attempt nobody here is running is a lost relay.</summary>
    private async Task BackstopAsync()
    {
        using var timer = new PeriodicTimer(_options.SweepBackstop);
        try
        {
            while (await timer.WaitForNextTickAsync(_closing.Token).ConfigureAwait(false))
            {
                try
                {
                    await SweepAsync(AttemptCauses.RelayLost, _closing.Token, SweepTriggers.Backstop).ConfigureAwait(false);
                }
                catch (Exception error) when (error is IOException or WorkspaceStoreException or AgentPlaneException)
                {
                    _telemetry.Write("attempt.sweep.failed", new JsonObject { ["trigger"] = SweepTriggers.Backstop, ["error"] = error.Message });
                }
            }
        }
        catch (OperationCanceledException)
        {
            // The host closed.
        }
    }

    /// <summary>Waits for the predecessor's terminal; launches only after its <c>ResponseComplete</c>, else holds for Send-now.</summary>
    private async Task DrainQueuedAsync(LiveAttempt live, string predecessorKey)
    {
        var predecessor = await FindLiveAsync(predecessorKey, CancellationToken.None).ConfigureAwait(false);
        if (predecessor is not null)
        {
            await ((Task)predecessor.Done.Task).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }

        var receipt = await _ledger.ReadAttemptAsync(predecessorKey, CancellationToken.None).ConfigureAwait(false);
        if (receipt?.Terminal?.State == DispatchState.ResponseComplete)
        {
            await TryLaunchAsync(live).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// The one launch of a live attempt, whoever asks (the dispatch, the drainer, Send-now): the first claimant
    /// re-reads the receipt and launches only a still-<c>Pending</c> attempt (design, re-review Minor 3).
    /// </summary>
    private async Task<bool> TryLaunchAsync(LiveAttempt live)
    {
        if (!live.TryClaimLaunch())
        {
            return false;
        }

        var key = live.Dispatch.DispatchKey;
        var receipt = await _ledger.ReadAttemptAsync(key, CancellationToken.None).ConfigureAwait(false);
        if (receipt is null || !receipt.IsPending)
        {
            await DeregisterAsync(key, live).ConfigureAwait(false);
            live.Done.TrySetResult(receipt!);
            return false;
        }

        _ = ForwardAsync(live.Done, LaunchAsync(live));
        return true;
    }

    private static async Task ForwardAsync(TaskCompletionSource<AttemptReceipt> done, Task<AttemptReceipt> launch)
    {
        try
        {
            done.TrySetResult(await launch.ConfigureAwait(false));
        }
        catch (Exception error)
        {
            done.TrySetException(error);
        }
    }

    /// <summary>E → terminal for an attempt this host launches.</summary>
    /// <remarks>
    /// Only the four exception types a run can end with are mapped to a terminal. Anything else — a process death
    /// among them — propagates and leaves the rows a crash would leave, for the next host's sweep; and only a run
    /// that did not crash triggers the engine-exit sweep.
    /// </remarks>
    private async Task<AttemptReceipt> LaunchAsync(LiveAttempt live)
    {
        var key = live.Dispatch.DispatchKey;
        RunLogStore? runLog = null;
        try
        {
            runLog = (_options.RunLogs ?? OpenDefaultRunLog)(live.Dispatch.SessionId, key);
            live.RunLogPath = runLog.Path;
            var run = new AttemptRun(_ledger, key) { RunLog = runLog, Stop = live.Stop };
            await GovernedRunHost.RunAsync(live.Dispatch.Run, live.Cancel.Token, sink: Sink(live), attempt: run).ConfigureAwait(false);
        }
        catch (Exception error) when (error is AgentPlaneException or IOException or OperationCanceledException or WorkspaceStoreException)
        {
            await SettleAfterThrowAsync(key, live.Stop, error).ConfigureAwait(false);
        }
        finally
        {
            live.Relay.Complete();
            runLog?.Dispose();
            await DeregisterAsync(key, live).ConfigureAwait(false);
        }

        var receipt = (await _ledger.ReadAttemptAsync(key, CancellationToken.None).ConfigureAwait(false))!;
        ReportIncident(receipt);
        await SweepAsync(AttemptCauses.RelayLost, CancellationToken.None, SweepTriggers.EngineExit).ConfigureAwait(false);
        return receipt;
    }

    private static Action<ObservedRunEvent> Sink(LiveAttempt live) => observed =>
    {
        live.Stop.Observe(observed.Event);
        live.Relay.Publish(observed);
    };

    /// <summary>The terminal for a run that threw: before G2 nothing was sent (<c>Failed</c>), after it the effects are unknown.</summary>
    private async Task SettleAfterThrowAsync(string key, AttemptStop stop, Exception error)
    {
        var receipt = await _ledger.ReadAttemptAsync(key, CancellationToken.None).ConfigureAwait(false);
        if (receipt is null || receipt.Terminal is not null)
        {
            return;
        }

        await AttemptTerminal.RecordStopPermissionsAsync(_ledger, key, stop).ConfigureAwait(false);
        var code = error is AgentPlaneException plane ? plane.Code : error.GetType().Name;
        var terminal = receipt.Dispatched is null
            ? new OutcomeRow(key, DispatchState.Failed, AttemptErrorCodes.NotDispatched, Detail: code)
            : new OutcomeRow(key, DispatchState.DeliveryUnknown, DispatchErrorCodes.DeliveryUnknown, AttemptTerminal.CauseAfterThrow(stop, code), Detail: code);
        await _ledger.AppendAttemptOutcomeAsync(terminal, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>ADR-0041 §4: an unwritable run log is a sidecar incident as well as a terminal.</summary>
    private void ReportIncident(AttemptReceipt receipt)
    {
        var terminal = receipt.Terminal;
        if (terminal is null || (terminal.Cause != AttemptCauses.RunLogUnwritable && terminal.Detail != RunLogStore.UnwritableCode))
        {
            return;
        }

        _options.Incidents?.Record("attempt.run_log_unwritable", receipt.Attempt.DispatchKey,
            $"attempt {receipt.Attempt.DispatchKey}: a run-log line could not be made durable; {terminal.State} {terminal.Cause ?? terminal.ErrorCode}",
            DateTimeOffset.UtcNow);
    }

    private RunLogStore OpenDefaultRunLog(string sessionId, string dispatchKey) =>
        RunLogStore.Open(_options.WorkspaceRoot, sessionId, dispatchKey);

    private string DefaultRunLogPath(string sessionId, string dispatchKey) =>
        RunLogStore.PathFor(_options.WorkspaceRoot, sessionId, dispatchKey);

    private async Task<LiveAttempt?> FindLiveAsync(string dispatchKey, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return _live.GetValueOrDefault(dispatchKey);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<AttemptReceipt> ReadKnownAsync(string dispatchKey, CancellationToken cancellationToken) =>
        await _ledger.ReadAttemptAsync(dispatchKey, cancellationToken).ConfigureAwait(false)
        ?? throw new KeyNotFoundException($"attempt {dispatchKey} was never attempted");

    private static DateTimeOffset? Timestamp(string? recordedAt) =>
        DateTimeOffset.TryParse(recordedAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var at) ? at : null;

    private static JsonObject? ParseDetail(string? detail)
    {
        if (string.IsNullOrEmpty(detail))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(detail) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>D: only the owner that won G0 removes the entry, and only after the terminal write.</summary>
    private async Task DeregisterAsync(string key, LiveAttempt owner)
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

    /// <summary>
    /// The host's own teardown — what a process death does, so a restarted host can take the lock. Live runs are
    /// cancelled by token, never by <c>session/cancel</c>, so a teardown-caused exit is never <c>Interrupted</c>.
    /// </summary>
    public void Dispose()
    {
        _closing.Cancel();
        _foldListener.Dispose();
        foreach (var live in Live.Select(key => _live.GetValueOrDefault(key)).OfType<LiveAttempt>())
        {
            live.Cancel.Cancel();
        }

        _hostLock.Dispose();
    }

    /// <summary>One attempt this host holds live, from L (queue time included) to D.</summary>
    private sealed class LiveAttempt(AttemptDispatch dispatch)
    {
        private int _launchClaimed;

        public AttemptDispatch Dispatch { get; } = dispatch;

        public RunEventRelay Relay { get; } = new();

        public AttemptStop Stop { get; } = new();

        public CancellationTokenSource Cancel { get; } = new();

        /// <summary>The receipt after the terminal; resolved by whichever launcher won, or with the existing receipt.</summary>
        public TaskCompletionSource<AttemptReceipt> Done { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public string? RunLogPath { get; set; }

        public bool Launched => Volatile.Read(ref _launchClaimed) == 1;

        public bool TryClaimLaunch() => Interlocked.Exchange(ref _launchClaimed, 1) == 0;
    }
}

/// <summary>
/// The rolling log the host's metrics go to (ADR-0041 §7: <c>attempt.sweep</c>, <c>fold.duration_ms</c>): one JSON
/// object per line, rolled to <c>.1</c> past its cap. A line that cannot be written is not recorded, never a guess.
/// </summary>
internal sealed class AttemptRollingLog(string path, long capBytes = 1 << 20)
{
    private readonly Lock _gate = new();

    public void Write(string metric, JsonObject fields)
    {
        var line = new JsonObject { ["metric"] = metric, ["at"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture) };
        foreach (var (name, value) in fields.ToArray())
        {
            fields.Remove(name);
            line[name] = value;
        }

        lock (_gate)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                if (File.Exists(path) && new FileInfo(path).Length > capBytes)
                {
                    File.Move(path, path + ".1", overwrite: true);
                }

                File.AppendAllText(path, line.ToJsonString() + "\n");
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                // Not recorded: the rolling log degrades to a missing line, never to a wrong number.
            }
        }
    }
}

/// <summary>The stable codes the attempt host itself refuses with.</summary>
public static class AttemptHostCodes
{
    /// <summary>A withdraw of an attempt that is not a held successor, or whose launch was already claimed.</summary>
    public const string WithdrawRefused = "AIDE-ATTEMPT-WITHDRAW-REFUSED";
}

/// <summary>Why a sweep ran — the <c>trigger</c> of <c>attempt.sweep</c>.</summary>
internal static class SweepTriggers
{
    public const string HostStartup = "host_startup";
    public const string EngineExit = "engine_exit";
    public const string Backstop = "backstop";
    public const string Manual = "manual";
}

/// <summary>The <c>DeliveryUnknown</c> causes the host writes (one vocabulary: <see cref="WorkbenchVocabulary.DeliveryUnknownCauses"/>).</summary>
internal static class AttemptCauses
{
    public const string NeverDispatched = "never_dispatched";
    public const string RelayLost = "relay_lost";
    public const string PromptTimeout = "prompt_timeout";
    public const string HostRestart = "host_restart";
    public const string StopNotAcknowledged = "stop_not_acknowledged";
    public const string RunLogUnwritable = "run_log_unwritable";
}

/// <summary>The terminal an awaited prompt maps to (ADR-0041 §2) — the host's own correlated reply, nothing else.</summary>
internal static class AttemptTerminal
{
    public static OutcomeRow For(AttemptRun run, Task<JsonObject> prompt)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(prompt);
        if (!prompt.IsCompleted)
        {
            // DC-NEW-wl-t5-host-n2: a terminal mapped from a task in flight is a guess ("cancelled"), not a reply.
            throw new InvalidOperationException("the terminal is mapped from the completed prompt only; await it first");
        }

        var key = run.DispatchKey;

        // ADR-0041 §4: at the Stop deadline the terminal is stop_not_acknowledged, unconditionally.
        if (run.Stop.DeadlinePassed)
        {
            return new OutcomeRow(key, DispatchState.DeliveryUnknown, DispatchErrorCodes.DeliveryUnknown, AttemptCauses.StopNotAcknowledged, "stop deadline passed");
        }

        if (prompt.IsCompletedSuccessfully)
        {
            var stopReason = (prompt.Result["stopReason"] as JsonValue)?.TryGetValue<string>(out var reason) == true ? reason : null;
            var detail = new JsonObject
            {
                ["stopReason"] = stopReason,
                ["usage"] = prompt.Result["usage"]?.DeepClone() ?? JsonValue.Create(GovernedRunHost.NotRecorded),
            }.ToJsonString();

            // A cancelled reply is Interrupted only under a Stop whose high-water mark it follows (§4); with no
            // Stop requested it is not our acknowledgment, so its effects are unknown.
            return stopReason == "cancelled"
                ? run.Stop.AcknowledgedAfterMark
                    ? new OutcomeRow(key, DispatchState.Interrupted, Detail: detail)
                    : new OutcomeRow(key, DispatchState.DeliveryUnknown, DispatchErrorCodes.DeliveryUnknown, AttemptCauses.RelayLost, detail)
                : new OutcomeRow(key, DispatchState.ResponseComplete, Detail: detail);
        }

        var error = prompt.Exception?.GetBaseException();
        return error switch
        {
            AgentPlaneException { Code: RunLogStore.UnwritableCode } unwritable =>
                new OutcomeRow(key, DispatchState.DeliveryUnknown, DispatchErrorCodes.DeliveryUnknown, AttemptCauses.RunLogUnwritable, unwritable.Code),
            AgentPlaneException { Code: AgentPlaneErrorCodes.EngineRequestTimedOut } timedOut =>
                new OutcomeRow(key, DispatchState.DeliveryUnknown, DispatchErrorCodes.DeliveryUnknown, AttemptCauses.PromptTimeout, timedOut.Code),
            AgentPlaneException { Code: AgentPlaneErrorCodes.EngineStreamEnded } ended =>
                new OutcomeRow(key, DispatchState.DeliveryUnknown, DispatchErrorCodes.DeliveryUnknown, AttemptCauses.RelayLost, ended.Code),
            AgentPlaneException refused => new OutcomeRow(key, DispatchState.Failed, refused.Code),
            _ => new OutcomeRow(key, DispatchState.DeliveryUnknown, DispatchErrorCodes.DeliveryUnknown, AttemptCauses.RelayLost,
                error?.GetType().Name ?? "cancelled"),
        };
    }

    /// <summary>The cause of a dispatched run that threw: the Stop deadline, an unwritable run log, or a lost relay.</summary>
    public static string CauseAfterThrow(AttemptStop stop, string code) =>
        stop.DeadlinePassed ? AttemptCauses.StopNotAcknowledged
        : code == RunLogStore.UnwritableCode ? AttemptCauses.RunLogUnwritable
        : AttemptCauses.RelayLost;

    /// <summary>
    /// <c>PermissionCancelledByStop</c> (ADR-0041 §4): one outcome row per permission request answered
    /// <c>cancelled</c> during the Stop, written before the terminal, never a <c>permission_decision_fact</c>.
    /// </summary>
    public static async Task RecordStopPermissionsAsync(IAttemptLedger ledger, string key, AttemptStop stop)
    {
        foreach (var request in stop.TakeCancelledPermissions())
        {
            var toolCall = (request["toolCall"]?["toolCallId"] as JsonValue)?.TryGetValue<string>(out var id) == true ? id : GovernedRunHost.NotRecorded;
            await ledger.AppendAttemptOutcomeAsync(
                new OutcomeRow(key, DispatchState.PermissionCancelledByStop, Detail: new JsonObject { ["tool_call_id"] = toolCall }.ToJsonString()),
                CancellationToken.None).ConfigureAwait(false);
        }
    }
}
