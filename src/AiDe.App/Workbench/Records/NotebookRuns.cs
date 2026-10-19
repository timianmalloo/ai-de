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
public sealed record AttemptScope(string WorkspaceId, long WorkspaceEpoch, long SessionGeneration);

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

    /// <summary>The attempt host this notebook's turns dispatch through, or null when they run directly.</summary>
    public IAttemptHost? Host => _host?.Host;

    /// <summary>The attempts read back from the store for this notebook's submitted envelopes (restart read-back).</summary>
    public IReadOnlyList<AttemptReceipt> ReadBack { get; private set; } = [];

    /// <summary>
    /// Binds the notebook to the workspace's attempt host (ADR-0041): from now on a sent turn is an attempt of the
    /// revision it ran, launched and owned by the host, never by a surface.
    /// </summary>
    public void BindHost(IAttemptHost host, AttemptScope scope)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(scope);
        _host = (host, scope);
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
            // assume: the host's run publishes to no sink until Subscribe lands (T5 node 2), so no lane is attached.
            // Confirm: IAttemptHost.Subscribe on main. If false: attach the subscription's reader here.
            return LastRun = DispatchAsync(bound.Host, bound.Scope, request, launch, ordinal, cancel);
        }

        var relay = new RunEventRelay();
        Observer?.RunStarted(ordinal, request.EngineId, relay);
        return LastRun = RunAsync(request, launch, relay, ordinal, cancel);
    }

    /// <summary>Holds queued turn <paramref name="ordinal"/> until the drain or Send now (Ruling 95: exactly one).</summary>
    public void Queue(int ordinal, GovernedRunRequest request, RunLaunch launch)
    {
        lock (_gate)
        {
            _queued = (ordinal, request, launch);
        }
    }

    /// <summary>Stop: cancels the run's token; the turn is StopRequested, never Interrupted (ADR-0041 §2).</summary>
    public void Stop(int ordinal)
    {
        lock (_gate)
        {
            if (_runs.TryGetValue(ordinal, out var cancel))
            {
                cancel.Cancel();
            }
        }
    }

    /// <summary>Send now on the queued turn: started when it is the queued one and no turn is in flight.</summary>
    /// <returns>The run, or null when nothing was started.</returns>
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

        Thread.Start(queued.Ordinal, DateTimeOffset.Now);
        return Start(queued.Ordinal, queued.Request, queued.Launch);
    }

    /// <summary>Cancel on the queued turn: dropped from the queue. True when it was the queued one.</summary>
    public bool Cancel(int ordinal)
    {
        lock (_gate)
        {
            if (_queued is not { } held || held.Ordinal != ordinal)
            {
                return false;
            }

            _queued = null;
            return true;
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
    private async Task DispatchAsync(IAttemptHost host, AttemptScope scope, GovernedRunRequest request, RunLaunch launch, int ordinal, CancellationTokenSource cancel)
    {
        try
        {
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

            var start = await host.DispatchAsync(
                new AttemptDispatch(request, scope.WorkspaceId, scope.WorkspaceEpoch, envelopeId, projectionSha,
                    _notebook.Id.Value, revision.RevisionNo, _notebook.Session.SessionId, scope.SessionGeneration, current),
                CancellationToken.None).ConfigureAwait(false);
            Record(ordinal, start.Receipt);
            var receipt = await start.Completion.ConfigureAwait(false);
            Record(ordinal, receipt);

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
            lock (_gate)
            {
                _runs.Remove(ordinal);
            }

            cancel.Dispose();
        }
    }

    private void Record(int ordinal, AttemptReceipt receipt)
    {
        lock (_gate)
        {
            _attempts[ordinal] = receipt;
        }
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
