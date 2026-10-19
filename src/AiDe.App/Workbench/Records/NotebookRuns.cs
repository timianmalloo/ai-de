using System.IO;
using AiDe.App.Conductor;
using AiDe.App.Workbench.Composer;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.PromptCompilation;

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
public sealed record NotebookRunEnd(int Ordinal, GovernedRunResult? Result, string? Failure, string ConsumedReason);

/// <summary>What a turn's launch carries from the composer, captured at Send (never read back from the gate later).</summary>
/// <param name="Revision">The revision binding (<see cref="NotebookModel.BindResult"/>) — awaited before a host dispatch.</param>
/// <param name="TaskClassSource">The envelope's class provenance, stamped on a scored episode; null with no envelope.</param>
public sealed record RunLaunch(Task Revision, string? TaskClassSource);

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
    private (int Ordinal, GovernedRunRequest Request, RunLaunch Launch)? _queued;

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

    /// <summary>The last run's own task — completed before the first. It never faults: a refusal is a line of the run.</summary>
    public Task LastRun { get; private set; } = Task.CompletedTask;

    private RunChannelSessionThread Thread => _notebook.Thread;

    /// <summary>Starts the run for accepted turn <paramref name="ordinal"/>; the task never faults.</summary>
    public Task Start(int ordinal, GovernedRunRequest request, RunLaunch launch)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(launch);

        var relay = new RunEventRelay();
        var cancel = CancellationTokenSource.CreateLinkedTokenSource(_disposing.Token);
        lock (_gate)
        {
            _runs[ordinal] = cancel;
        }

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
            // Reported, never thrown away: the sentence is a line of the run, under the lane that refused (Ruling 81).
            var failure = $"{error.GetType().Name}: {error.Message}";
            Thread.Append(ordinal, new EventLine(DateTimeOffset.Now, request.EngineId, "stderr", failure, "run"));
            Thread.Conclude(ordinal, TurnState.Failed, DateTimeOffset.Now, exitCode: null, edits: null);
            Observer?.RunEnded(new NotebookRunEnd(ordinal, null, failure, ConsumedReasons.LaneExited(null)));
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
