using System.IO;
using AiDe.App.Conductor;
using AiDe.App.Workbench.Composer;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.AgentPlane;
using AiDe.Core.Dispatch;
using AiDe.Core.Facts;
using AiDe.Core.Ipc;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.PromptCompilation;
using AiDe.Core.Store;

namespace AiDe.App.Workbench.Records;

/// <summary>What a surface bound to the model hears about the model's runs; null when no surface is bound.</summary>
public interface INotebookRunObserver
{
    /// <summary>A run started publishing through <paramref name="relay"/> — a bound surface attaches its lane.</summary>
    void RunStarted(int ordinal, string engineId, RunEventRelay relay);

    /// <summary>A run ended; the thread already holds its terminal state.</summary>
    void RunEnded(NotebookRunEnd end);
}

/// <summary>How one run ended, for the surface's envelope record and its last-run readouts.</summary>
/// <param name="ConsumedReason">The envelope's <c>consumed</c> reason (ADR-0034).</param>
/// <param name="Attempt">The host's receipt when the run was an attempt (<see cref="IAttemptHost"/>), else null.</param>
public sealed record NotebookRunEnd(int Ordinal, GovernedRunResult? Result, string? Failure, string ConsumedReason, AttemptReceipt? Attempt = null);

/// <summary>The attempt identity the workspace gives a notebook's dispatches (<see cref="AttemptDispatch"/>'s own fields).</summary>
public sealed record AttemptScope(string WorkspaceId, long WorkspaceEpoch, long SessionGeneration)
{
    /// <summary>
    /// Reads the workspace epoch at each dispatch — the current handshake's, so a daemon restart after bind is seen.
    /// Null: <see cref="WorkspaceEpoch"/> holds for the binding's life.
    /// </summary>
    public Func<CancellationToken, Task<long>>? RefreshEpoch { get; init; }
}

/// <summary>What a turn's launch carries from the composer, captured at Send (never read back from the gate later).</summary>
/// <param name="Revision">The revision binding (<see cref="NotebookModel.BindResult"/>) — awaited before a host dispatch.</param>
/// <param name="TaskClassSource">The envelope's class provenance, stamped on a scored episode; null with no envelope.</param>
/// <param name="EnvelopeId">The prepared envelope the turn sent; null with none.</param>
/// <param name="ProjectionSha">The projection the envelope was submitted with: the attempt's prepared sha.</param>
/// <param name="CurrentProjectionSha">The envelope fold's projection NOW, read at the host's gate (G1), never cached.</param>
public sealed record RunLaunch(
    Task Revision,
    string? TaskClassSource,
    string? EnvelopeId = null,
    string? ProjectionSha = null,
    Func<string?>? CurrentProjectionSha = null);

/// <summary>
/// The notebook's runs (ADR-0045 §4; ADR-0041): the per-run cancellation, the one queued turn (Ruling 95) and the drain
/// — owned by the model, so a surface may be closed and reopened while a run is in flight ("Pane hidden. The run
/// continues."). Disposing the model is what cancels them.
/// </summary>
public sealed class NotebookRuns : IDisposable
{
    private readonly NotebookModel _notebook;
    private readonly SynchronizationContext? _home;
    private readonly CancellationTokenSource _disposing = new();
    private readonly Lock _gate = new();
    private readonly Dictionary<int, CancellationTokenSource> _runs = [];
    private readonly Dictionary<int, AttemptReceipt> _attempts = [];

    // Host-bound only: each turn's dispatch key, derived at Send (AttemptKey.For — nothing stored beside the envelope);
    // a successor held at the host, by its G0 and the cancellation that becomes its run's once it starts.
    private readonly Dictionary<int, string> _keys = [];
    private readonly Dictionary<int, (TaskCompletionSource G0, CancellationTokenSource Cancel)> _held = [];
    private (int Ordinal, GovernedRunRequest Request, RunLaunch Launch)? _queued;
    private (IAttemptHost Host, AttemptScope Scope)? _host;

    /// <param name="notebook">The model whose thread the runs fold into.</param>
    internal NotebookRuns(NotebookModel notebook)
    {
        _notebook = notebook;

        // The drain re-enters on the context the model was made on (the shell's dispatcher), where Send also runs, so a
        // Send and a drain never interleave between "is a turn in flight?" and "queue it".
        _home = SynchronizationContext.Current;
    }

    /// <summary>The bound surface, or null while no surface shows the notebook.</summary>
    public INotebookRunObserver? Observer { get; set; }

    /// <summary>Whether any run is in flight.</summary>
    public bool InFlight
    {
        get
        {
            lock (_gate)
            {
                return _runs.Count > 0;
            }
        }
    }

    /// <summary>The queued turn's ordinal, or null.</summary>
    public int? QueuedOrdinal
    {
        get
        {
            lock (_gate)
            {
                return _queued?.Ordinal;
            }
        }
    }

    /// <summary>The attempt host this notebook's turns dispatch through, or null while none is bound.</summary>
    public IAttemptHost? Host => _host?.Host;

    /// <summary>Raised each time an attempt becomes known to this notebook: a live receipt recorded.</summary>
    public event Action? AttemptsChanged;

    /// <summary>The attempts read back from the store for this notebook's submitted envelopes (restart read-back).</summary>
    public IReadOnlyList<AttemptReceipt> ReadBack { get; private set; } = [];

    /// <summary>
    /// Binds the notebook to the workspace's attempt host (ADR-0041): from now on a sent turn is an attempt of the
    /// revision it ran, launched and owned by the host, never by a surface.
    /// </summary>
    /// <remarks>Called on the model's home context: <see cref="HostRefusalChanged"/> is raised inline.</remarks>
    public void BindHost(IAttemptHost host, AttemptScope scope)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(scope);
        ChangeRefusal(() => _host = (host, scope), inline: true);
    }

    /// <summary>
    /// The test seam (Ruling 159(2)): turns run directly through <see cref="GovernedRunHost"/>, unrecorded, with no host.
    /// Internal, reached only through InternalsVisibleTo; a census test fails on any call under <c>src/</c>.
    /// </summary>
    internal void UnhostedForTests() => ChangeRefusal(() => _unhosted = true, inline: true);

    private bool _unhosted;

    /// <summary>Ruling 159(1): the copy Send shows while another window holds the workspace's attempt host.</summary>
    public const string HostedElsewhere = "This workspace's runs are hosted in another window. Send from that window, or close it to host runs here.";

    /// <summary>The refusal when the host's open ended with no cause this binding knows.</summary>
    public const string NotOpened = "Runs cannot be hosted in this window: the attempt host did not open. Retry to open the host again.";

    /// <summary>Ruling 159(2): the refusal of a window with no workspace client — nothing here can record a turn, and no retry can help.</summary>
    public const string NoWorkspace = "Runs cannot be hosted in this window: the workspace service is not connected, so a turn would not be recorded. Reopen the workspace once the service runs.";

    /// <summary>
    /// Why this window cannot host runs, or null. A turn sent meanwhile is refused with it and never runs directly.
    /// With neither a host nor an opener the window has no workspace client: <see cref="NoWorkspace"/>.
    /// </summary>
    public string? HostRefusal =>
        _host is not null || _unhosted ? null : _openFailure ?? (_opener is null ? NoWorkspace : null);

    /// <summary>True while a retry can help: the refusal came from an open that may succeed on another try.</summary>
    public bool CanRetryHost => _host is null && _opener is not null && _openFailure is not null and not HostedElsewhere;

    private string? _openFailure;

    /// <summary>
    /// Binds to the workspace's attempt host once <paramref name="open"/> completes (Ruling 159): a turn sent before then
    /// waits for it, and a host that cannot be opened refuses every send with its cause — never a direct run, which would
    /// break ADR-0041's write-ahead invariant.
    /// </summary>
    public void BindHostWhenOpen(Func<Task<IAttemptHost>> open, AttemptScope scope)
    {
        ArgumentNullException.ThrowIfNull(open);
        ArgumentNullException.ThrowIfNull(scope);
        ChangeRefusal(() => _opener = (open, scope), inline: true);
        _hostOpening = OpenHostAsync(open, scope);
    }

    /// <summary>The refusal's retry: opens the host again. Completes once this try bound the host or set its refusal.</summary>
    public Task RetryHostAsync() =>
        _host is null && _opener is { } opener ? _hostOpening = OpenHostAsync(opener.Open, opener.Scope) : Task.CompletedTask;

    private (Func<Task<IAttemptHost>> Open, AttemptScope Scope)? _opener;
    private Task _hostOpening = Task.CompletedTask;

    /// <summary>Raised on the model's home context each time <see cref="HostRefusal"/> is set or cleared.</summary>
    public event Action? HostRefusalChanged;

    /// <summary>How often a hosted-elsewhere window tries the open again, so the copy shows only while the holder lives.</summary>
    public static readonly TimeSpan HolderProbe = TimeSpan.FromSeconds(1);

    private async Task OpenHostAsync(Func<Task<IAttemptHost>> open, AttemptScope scope)
    {
        // Terminates: each pass binds, refuses with a cause that needs the operator's Retry, or ends with the workspace.
        while (!_disposing.IsCancellationRequested)
        {
            try
            {
                var host = await open().ConfigureAwait(false);
                ChangeRefusal(() => (_host, _openFailure) = ((host, scope), null), inline: false);
                return;
            }
            catch (AgentPlaneException error) when (error.Code == AttemptErrorCodes.HostNotSole)
            {
                // Only a live holder keeps the lock: the OS releases it with the holding process (AttemptHost.OpenAsync).
                ChangeRefusal(() => _openFailure = HostedElsewhere, inline: false);
            }
            catch (Exception error) when (error is AgentPlaneException or IOException or IpcRequestException or OperationCanceledException or UnauthorizedAccessException or WorkspaceStoreException)
            {
                ChangeRefusal(() => _openFailure = $"Runs cannot be hosted in this window: {error.Message} Retry to open the host again.", inline: false);
                return;
            }

            await Task.Delay(HolderProbe, _disposing.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }

    /// <summary>
    /// Applies <paramref name="change"/> and raises <see cref="HostRefusalChanged"/> when the refusal it derives changed:
    /// inline for a caller on the home context, else posted there (the open's continuation runs on the pool).
    /// </summary>
    private void ChangeRefusal(Action change, bool inline)
    {
        var before = HostRefusal;
        change();
        if (before == HostRefusal)
        {
            return;
        }

        if (inline)
        {
            HostRefusalChanged?.Invoke();
            return;
        }

        Home(() => HostRefusalChanged?.Invoke());
    }

    /// <summary>Turn <paramref name="ordinal"/>'s attempt as the host last answered it, or null when it was not one.</summary>
    public AttemptReceipt? AttemptOf(int ordinal)
    {
        lock (_gate)
        {
            return _attempts.GetValueOrDefault(ordinal);
        }
    }

    /// <summary>
    /// Restart read-back: each submitted envelope's dispatch key is derived (<see cref="AttemptKey.For"/>, nothing
    /// stored beside it) and read from the host; an envelope never attempted is skipped.
    /// </summary>
    public async Task ReadBackAsync(IEnumerable<(string EnvelopeId, string ProjectionSha)> submitted, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(submitted);
        if (Host is not { } host)
        {
            return;
        }

        List<AttemptReceipt> found = [];
        foreach (var (envelopeId, projectionSha) in submitted)
        {
            if (await host.ReadAsync(AttemptKey.For(envelopeId, projectionSha, null, null), cancellationToken).ConfigureAwait(false) is { } receipt)
            {
                found.Add(receipt);
            }
        }

        ReadBack = found;
    }

    /// <summary>The last run's own task — completed before the first. It never faults: a refusal is a line of the run.</summary>
    public Task LastRun { get; private set; } = Task.CompletedTask;

    private RunChannelSessionThread Thread => _notebook.Thread;

    /// <summary>Starts the run for accepted turn <paramref name="ordinal"/>; the task never faults.</summary>
    public Task Start(int ordinal, GovernedRunRequest request, RunLaunch launch)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(launch);

        var cancel = CancellationTokenSource.CreateLinkedTokenSource(_disposing.Token);
        lock (_gate)
        {
            _runs[ordinal] = cancel;
        }

        if (_host is { } bound)
        {
            return LastRun = DispatchAsync(bound.Host, bound.Scope, request, launch, ordinal, cancel, queuedAfter: null);
        }

        if (_opener is not null)
        {
            return LastRun = AfterTheHostOpensAsync(request, launch, ordinal, cancel);
        }

        if (!_unhosted)
        {
            // Ruling 159(2): no workspace client, so nothing can record the turn — refused, never run directly.
            lock (_gate)
            {
                _runs.Remove(ordinal);
            }

            cancel.Dispose();
            Fail(ordinal, request.EngineId, NoWorkspace);
            return LastRun = Task.CompletedTask;
        }

        var relay = new RunEventRelay();
        Observer?.RunStarted(ordinal, request.EngineId, relay);
        return LastRun = RunAsync(request, launch, relay, ordinal, cancel);
    }

    /// <summary>
    /// Queues turn <paramref name="ordinal"/> (Ruling 95: exactly one). Host-bound, the queue is the host's (ADR-0045 §4,
    /// design "Queued launch"): the turn is dispatched now as the held successor of the turn in flight, and the host
    /// launches it after that one's <c>ResponseComplete</c>. Otherwise it is held here until the drain or Send now.
    /// </summary>
    public void Queue(int ordinal, GovernedRunRequest request, RunLaunch launch)
    {
        string? predecessor = null;
        lock (_gate)
        {
            _queued = (ordinal, request, launch);
            if (_host is not null && Thread.Current.InFlight is { } running && launch is { EnvelopeId: not null, ProjectionSha: not null })
            {
                predecessor = _keys.GetValueOrDefault(running.Ordinal);
            }
        }

        if (_host is not { } bound || predecessor is null)
        {
            return;
        }

        var cancel = CancellationTokenSource.CreateLinkedTokenSource(_disposing.Token);
        var g0 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_gate)
        {
            _held[ordinal] = (g0, cancel);
        }

        LastRun = DispatchAsync(bound.Host, bound.Scope, request, launch, ordinal, cancel, predecessor, g0);
    }

    /// <summary>
    /// Stop: the turn is StopRequested, never Interrupted by our own word (ADR-0041 §2). Host-bound, the Stop is the
    /// host's (<see cref="IAttemptHost.StopAsync"/>), whose terminal the turn then takes; otherwise the run's token is cancelled.
    /// </summary>
    public void Stop(int ordinal)
    {
        lock (_gate)
        {
            if (!_runs.TryGetValue(ordinal, out var cancel))
            {
                return;
            }

            cancel.Cancel();
        }

        if (_host is not { } bound)
        {
            return;
        }

        if (Thread.Current.Turns.FirstOrDefault(t => t.Ordinal == ordinal) is { State: TurnState.Preparing or TurnState.Running or TurnState.Waiting })
        {
            try
            {
                Thread.RequestStop(ordinal);
            }
            catch (InvalidOperationException)
            {
                // The attempt concluded between the read and the request: its terminal already stands.
            }
        }

        // Before G0 the dispatch itself sees the cancellation and sends nothing (DispatchAsync).
        if (AttemptOf(ordinal) is { } attempt)
        {
            _ = StopAttemptAsync(bound.Host, ordinal, attempt.DispatchKey);
        }
    }

    /// <summary>Send now on the queued turn: started when it is the queued one and no turn is in flight.</summary>
    /// <returns>The run (host-bound and held at the host: the host's launch), or null when nothing was started.</returns>
    public Task? SendNow(int ordinal)
    {
        (int Ordinal, GovernedRunRequest Request, RunLaunch Launch) queued;
        lock (_gate)
        {
            if (_queued is not { } held || held.Ordinal != ordinal || Thread.Current.InFlight is not null)
            {
                return null;
            }

            queued = held;
            _queued = null;
        }

        if (_host is { } bound && HeldG0(ordinal) is { } g0)
        {
            // One launch however many callers race — the host's drainer among them (IAttemptHost.SendNowAsync).
            MarkStarted(ordinal);
            return SendHeldAsync(bound.Host, ordinal, g0);
        }

        Thread.Start(queued.Ordinal, DateTimeOffset.Now);
        return Start(queued.Ordinal, queued.Request, queued.Launch);
    }

    /// <summary>Cancel on the queued turn: dropped from the queue. True when it was the queued one.</summary>
    /// <remarks>
    /// A successor held at the host is withdrawn there (<see cref="IAttemptHost.WithdrawAsync"/>): true only when the
    /// store's receipt reads withdrawn. A withdraw the host refuses — its launch was already claimed — leaves the turn
    /// as it is, and the refusal is a line of the turn.
    /// </remarks>
    public async Task<bool> CancelAsync(int ordinal)
    {
        string? dispatchKey;
        lock (_gate)
        {
            if (_queued is not { } held || held.Ordinal != ordinal)
            {
                return false;
            }

            if (!_held.ContainsKey(ordinal))
            {
                _queued = null;
                return true;
            }

            dispatchKey = _attempts.GetValueOrDefault(ordinal)?.DispatchKey;
        }

        if (_host is not { } bound || dispatchKey is null)
        {
            return false;
        }

        try
        {
            var receipt = await bound.Host.WithdrawAsync(dispatchKey, CancellationToken.None).ConfigureAwait(true);
            lock (_gate)
            {
                _queued = null;
            }

            return receipt.IsWithdrawn;
        }
        catch (Exception error) when (error is AgentPlaneException or IOException or WorkspaceStoreException or IpcRequestException)
        {
            Thread.Append(ordinal, new EventLine(DateTimeOffset.Now, "attempt-host", "stderr", $"Cancel: {error.GetType().Name}: {error.Message}", "run"));
            return false;
        }
    }

    /// <summary>
    /// The run's sink: every observed event reaches the relay (the lane) and the turn's fold (the read model) from ONE
    /// call, so the two can never disagree.
    /// </summary>
    public Action<ObservedRunEvent> Sink(string lane, RunEventRelay relay, int ordinal) =>
        observed =>
        {
            relay.Publish(observed);

            // One definition of a line's text (the console model's), one origin rule (the read model's), one reading of
            // a tool frame's facts (Ruling 82).
            var evt = observed.Event;
            var cost = evt.Cost is { } c ? new Spend(c.TokensIn, c.CacheRead, c.TokensOut, c.Requests) : null;
            Thread.Append(
                ordinal,
                new EventLine(evt.Ts, lane, evt.Kind, ConsoleStreamModel.TextOf(evt), CompileSignal.IsCompileEvent(evt) ? "compile" : "run", ToolFacts.Of(evt)),
                cost);
        };

    private async Task RunAsync(GovernedRunRequest request, RunLaunch launch, RunEventRelay relay, int ordinal, CancellationTokenSource cancel)
    {
        var sink = Sink(request.EngineId, relay, ordinal);

        try
        {
            var result = await GovernedRunHost.RunAsync(request, cancel.Token, sink).ConfigureAwait(false);
            var ending = request.Goal is null ? TurnState.Answered : TurnState.Completed;
            Thread.Conclude(ordinal, ending, DateTimeOffset.Now);
            Observer?.RunEnded(new NotebookRunEnd(ordinal, result, null, ConsumedReasons.Completed));
            SessionDocumentSurface.StampTaskClassSource(request.DataDirectory, result, launch.TaskClassSource);
            Home(() => DrainAfter(ending));
        }
        catch (OperationCanceledException) when (cancel.IsCancellationRequested && !_disposing.IsCancellationRequested)
        {
            // Our own token cancel is not a native acknowledgment, so the turn is StopRequested, never Interrupted.
            Thread.RequestStop(ordinal);
            Observer?.RunEnded(new NotebookRunEnd(ordinal, null, "OperationCanceledException: stopped by the operator", ConsumedReasons.StoppedByOperator));
        }
        catch (Exception error) when (error is AgentPlaneException or IOException or OperationCanceledException)
        {
            Fail(ordinal, request.EngineId, $"{error.GetType().Name}: {error.Message}");
        }
        finally
        {
            relay.Complete();
            lock (_gate)
            {
                _runs.Remove(ordinal);
            }

            cancel.Dispose();
        }
    }

    /// <summary>
    /// A turn as an attempt (docs/design/workbench-loops-wl-1a.md, "The gate and the write order"): the revision it ran
    /// is awaited, the host's gate decides, and the terminal row the host committed is the turn's terminal state.
    /// </summary>
    /// <param name="queuedAfter">The predecessor's dispatch key when the host holds this turn as its successor, else null.</param>
    /// <param name="g0">Signalled once the host's gate decided a held successor, so Send now never races its G0.</param>
    private async Task DispatchAsync(
        IAttemptHost host, AttemptScope scope, GovernedRunRequest request, RunLaunch launch, int ordinal,
        CancellationTokenSource cancel, string? queuedAfter, TaskCompletionSource? g0 = null)
    {
        using var lines = new CancellationTokenSource();
        try
        {
            if (launch is { EnvelopeId: { } derivedFrom, ProjectionSha: { } derivedSha })
            {
                lock (_gate)
                {
                    _keys[ordinal] = AttemptKey.For(derivedFrom, derivedSha, queuedAfter, null);
                }
            }

            await launch.Revision.ConfigureAwait(false);
            if (_notebook.RevisionOf(ordinal) is not { } revision)
            {
                // An attempt is of a saved revision (ADR-0043 section 2): nothing unsaved is dispatched.
                Fail(ordinal, request.EngineId, _notebook.RevisionState ?? NotebookModel.RevisionsNotRecorded);
                return;
            }

            if (launch is not { EnvelopeId: { } envelopeId, ProjectionSha: { } projectionSha, CurrentProjectionSha: { } current })
            {
                Fail(ordinal, request.EngineId, "no prepared envelope: an attempt is dispatched only from a submitted envelope");
                return;
            }

            if (StopRequested(cancel))
            {
                Fail(ordinal, request.EngineId, "stopped before dispatch; no prompt is sent");
                return;
            }

            var start = await host.DispatchAsync(
                new AttemptDispatch(request, scope.WorkspaceId,
                    scope.RefreshEpoch is { } refresh ? await refresh(CancellationToken.None).ConfigureAwait(false) : scope.WorkspaceEpoch,
                    envelopeId, projectionSha,
                    _notebook.Id.Value, revision.RevisionNo, _notebook.Session.SessionId, scope.SessionGeneration, current,
                    QueuedAfterDispatchKey: queuedAfter),
                CancellationToken.None).ConfigureAwait(false);
            Record(ordinal, start.Receipt);
            g0?.TrySetResult();
            if (StopRequested(cancel))
            {
                _ = StopAttemptAsync(host, ordinal, start.Receipt.DispatchKey);
            }

            var pump = FollowAsync(host, start.Receipt.DispatchKey, request.EngineId, ordinal, lines.Token);
            var receipt = await start.Completion.ConfigureAwait(false);
            Record(ordinal, receipt);
            if (receipt.IsWithdrawn)
            {
                // The operator's Cancel concludes the turn (CancelAsync's caller); the host only wrote its terminal.
                return;
            }

            // The relay completes as the run ends, so the lines are in; the bound is for an attempt that never launched.
            lines.CancelAfter(LiveLinesGrace);
            await pump.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

            var terminal = receipt.Terminal;
            var answered = request.Goal is null ? TurnState.Answered : TurnState.Completed;
            var state = terminal?.State switch
            {
                DispatchState.ResponseComplete => answered,
                DispatchState.Failed => TurnState.Failed,
                DispatchState.Interrupted => TurnState.Interrupted,
                DispatchState.Rejected => TurnState.Rejected,
                _ => TurnState.DeliveryUnknown,
            };

            string? failure = null;
            if (state != answered)
            {
                // The host's own words: the stable code and cause it committed, as a line of the run (Ruling 81).
                failure = $"{terminal?.State.ToString() ?? "no terminal"}: {terminal?.ErrorCode ?? "-"} {terminal?.Cause} {terminal?.Detail}".TrimEnd();
                Thread.Append(ordinal, new EventLine(DateTimeOffset.Now, request.EngineId, "stderr", failure, "run"));
            }

            Thread.Conclude(ordinal, state, DateTimeOffset.Now);
            Observer?.RunEnded(new NotebookRunEnd(ordinal, null, failure,
                state == answered ? ConsumedReasons.Completed : ConsumedReasons.LaneExited(null), receipt));
            Home(() => DrainAfter(state));
        }
        catch (Exception error) when (error is AgentPlaneException or IOException or OperationCanceledException or WorkspaceStoreException or IpcRequestException)
        {
            Fail(ordinal, request.EngineId, $"{error.GetType().Name}: {error.Message}");
        }
        finally
        {
            g0?.TrySetResult();
            lock (_gate)
            {
                _runs.Remove(ordinal);
                _held.Remove(ordinal);
                _keys.Remove(ordinal);
            }

            cancel.Dispose();
        }
    }

    /// <summary>Ruling 159(4): a turn sent while the host opens waits for it; one the host cannot take is refused.</summary>
    private async Task AfterTheHostOpensAsync(GovernedRunRequest request, RunLaunch launch, int ordinal, CancellationTokenSource cancel)
    {
        await _hostOpening.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        if (_host is { } bound)
        {
            await DispatchAsync(bound.Host, bound.Scope, request, launch, ordinal, cancel, queuedAfter: null).ConfigureAwait(false);
            return;
        }

        lock (_gate)
        {
            _runs.Remove(ordinal);
        }

        cancel.Dispose();
        Fail(ordinal, request.EngineId, HostRefusal ?? NotOpened);
    }

    /// <summary>How long a run's lines may trail its terminal row before the follow is let go.</summary>
    private static readonly TimeSpan LiveLinesGrace = TimeSpan.FromSeconds(5);

    private bool StopRequested(CancellationTokenSource cancel) => cancel.IsCancellationRequested && !_disposing.IsCancellationRequested;

    /// <summary>The host's Stop (ADR-0041 §4); its terminal reaches the turn through the attempt's completion.</summary>
    private async Task StopAttemptAsync(IAttemptHost host, int ordinal, string dispatchKey)
    {
        try
        {
            await host.StopAsync(dispatchKey, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception error) when (error is AgentPlaneException or IOException or WorkspaceStoreException or IpcRequestException or KeyNotFoundException)
        {
            Thread.Append(ordinal, new EventLine(DateTimeOffset.Now, "attempt-host", "stderr", $"Stop: {error.GetType().Name}: {error.Message}", "run"));
        }
    }

    /// <summary>
    /// The attempt's lines (<see cref="IAttemptHost.SubscribeAsync"/>: the run log so far, then the live relay) folded
    /// into the turn and relayed to the bound surface's lane through the one sink, from the attempt's first event.
    /// </summary>
    private async Task FollowAsync(IAttemptHost host, string dispatchKey, string engineId, int ordinal, CancellationToken cancellationToken)
    {
        RunEventRelay? relay = null;
        try
        {
            var subscription = await host.SubscribeAsync(dispatchKey, cancellationToken).ConfigureAwait(false);
            Action<ObservedRunEvent>? sink = null;
            await foreach (var runEvent in subscription.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                if (sink is null)
                {
                    MarkStarted(ordinal);
                    relay = new RunEventRelay();
                    Observer?.RunStarted(ordinal, engineId, relay);
                    sink = Sink(engineId, relay, ordinal);
                }

                sink(new ObservedRunEvent(runEvent, ReceivedAt: null, NormalizationLatency: null));
            }
        }
        finally
        {
            relay?.Complete();
        }
    }

    /// <summary>A successor held at the host has started (its first event, or Send now): the turn runs from now, once.</summary>
    private void MarkStarted(int ordinal)
    {
        lock (_gate)
        {
            if (!_held.Remove(ordinal, out var held))
            {
                return;
            }

            _runs[ordinal] = held.Cancel;
            if (_queued?.Ordinal == ordinal)
            {
                _queued = null;
            }
        }

        if (Thread.Current.Turns.FirstOrDefault(t => t.Ordinal == ordinal) is { State: TurnState.Queued })
        {
            Thread.Start(ordinal, DateTimeOffset.Now);
        }
    }

    private Task? HeldG0(int ordinal)
    {
        lock (_gate)
        {
            return _held.TryGetValue(ordinal, out var held) ? held.G0.Task : null;
        }
    }

    /// <summary>Send now on a successor held at the host: after its G0, the host's launch.</summary>
    private async Task SendHeldAsync(IAttemptHost host, int ordinal, Task g0)
    {
        await g0.ConfigureAwait(false);
        if (AttemptOf(ordinal) is not { } attempt)
        {
            return;
        }

        try
        {
            await host.SendNowAsync(attempt.DispatchKey, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception error) when (error is AgentPlaneException or IOException or WorkspaceStoreException or IpcRequestException or KeyNotFoundException)
        {
            Thread.Append(ordinal, new EventLine(DateTimeOffset.Now, "attempt-host", "stderr", $"Send now: {error.GetType().Name}: {error.Message}", "run"));
        }
    }

    private void Record(int ordinal, AttemptReceipt receipt)
    {
        lock (_gate)
        {
            _attempts[ordinal] = receipt;
        }

        AttemptsChanged?.Invoke();
    }

    /// <summary>Reported, never thrown away: the sentence is a line of the run, under the lane that refused (Ruling 81).</summary>
    private void Fail(int ordinal, string engineId, string failure)
    {
        Thread.Append(ordinal, new EventLine(DateTimeOffset.Now, engineId, "stderr", failure, "run"));
        Thread.Conclude(ordinal, TurnState.Failed, DateTimeOffset.Now, exitCode: null, edits: null);
        Observer?.RunEnded(new NotebookRunEnd(ordinal, null, failure, ConsumedReasons.LaneExited(null), AttemptOf(ordinal)));
    }

    /// <summary>The drain (Ruling 95): the queued turn is sent only after the turn it waited behind Completed or Answered.</summary>
    private void DrainAfter(TurnState ending)
    {
        if (ending is not (TurnState.Completed or TurnState.Answered) || _disposing.IsCancellationRequested)
        {
            return;
        }

        if (QueuedOrdinal is { } queued)
        {
            SendNow(queued);
        }
    }

    private void Home(Action work)
    {
        if (_home is null)
        {
            work();
            return;
        }

        _home.Post(_ => work(), null);
    }

    /// <summary>The workspace closing: every run is cancelled; nothing is drained.</summary>
    public void Dispose()
    {
        if (_disposing.IsCancellationRequested)
        {
            return;
        }

        _disposing.Cancel();
    }
}
