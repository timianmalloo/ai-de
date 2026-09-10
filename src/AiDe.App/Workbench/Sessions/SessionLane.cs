using System.Threading.Channels;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Sessions;

namespace AiDe.App.Workbench.Sessions;

/// <summary>
/// One lane feeding a session document: it drains the plane's own event queue and dispatches each
/// normalized event into the document, in receipt order.
/// </summary>
/// <remarks>
/// <para><b>It reads the production queue, and normalizes nothing.</b> The reader is
/// <see cref="AcpEventQueue.Reader"/> — the same channel <c>AcpPeer</c> publishes into and
/// <c>GovernedRunHost</c> drains. Ordering, <see cref="RunEvent.Seq"/> and receipt time are the
/// plane's; re-deriving any of them here would give the console a second opinion about what
/// arrived, and the retain-never-rebuild oracle asks exactly that question (DM7).</para>
///
/// <para><b>Disposal is announced before it happens</b> (<see cref="SessionDisposalSignal"/>), so a
/// mode switch that quietly killed a lane is a number rather than an inference.
/// <see cref="SessionDisposalLedger"/> is what reads it.</para>
///
/// <para><b>The pump is marshalled by the caller.</b> The document and the console model are read
/// by WPF controls, so the shell passes its dispatcher; a headless caller passes nothing and the
/// dispatch runs inline, which makes an assertion about ordering an assertion about ordering rather
/// than about a scheduler.</para>
/// </remarks>
public sealed class SessionLane : IDisposable
{
    private readonly ChannelReader<ObservedRunEvent> _events;
    private readonly SessionDocumentViewModel _document;
    private readonly Action<Action> _marshal;
    private readonly CancellationTokenSource _stopping = new();
    private long _delivered;
    private bool _disposed;

    /// <param name="laneId">The lane's stable id — <c>LaneIdentity.LaneId</c> for a governed lane.</param>
    /// <param name="displayName">What the rail shows for it.</param>
    /// <param name="events">The plane's event queue for this lane.</param>
    /// <param name="document">The session document to dispatch into.</param>
    /// <param name="marshal">
    /// Runs one dispatch. Defaults to running it inline on the pump's own thread; the shell passes
    /// its dispatcher.
    /// </param>
    public SessionLane(
        string laneId,
        string displayName,
        ChannelReader<ObservedRunEvent> events,
        SessionDocumentViewModel document,
        Action<Action>? marshal = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(laneId);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(document);

        LaneId = laneId;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? laneId : displayName;
        _events = events;
        _document = document;
        _marshal = marshal ?? (work => work());

        Pump = Task.Run(PumpAsync);
    }

    /// <summary>The lane's stable id.</summary>
    public string LaneId { get; }

    /// <summary>What the console rail shows for it.</summary>
    public string DisplayName { get; }

    /// <summary>How many events this lane has delivered into the document.</summary>
    public long Delivered => Interlocked.Read(ref _delivered);

    /// <summary>Whether this lane has been disposed. Read by the falsifier, which needs it to be true.</summary>
    public bool IsDisposed => _disposed;

    /// <summary>The drain, which completes when the queue closes or the lane is disposed.</summary>
    public Task Pump { get; }

    /// <summary>
    /// Waits until this lane has delivered at least <paramref name="count"/> events.
    /// </summary>
    /// <remarks>
    /// A completion condition, not a speed claim: the bound is there so a stalled pump reports a
    /// stall instead of hanging the run, exactly as a <c>WaitForExit</c> bound does.
    /// </remarks>
    /// <returns>
    /// True when the count was reached; false when the drain finished first or the bound elapsed.
    /// </returns>
    public async Task<bool> WaitForDeliveredAsync(long count, TimeSpan bound)
    {
        var deadline = DateTimeOffset.UtcNow + bound;

        while (Delivered < count)
        {
            // A finished pump can never deliver another event, so waiting out the bound would only
            // convert a definite answer into a slow one.
            if (Pump.IsCompleted || DateTimeOffset.UtcNow > deadline)
            {
                return false;
            }

            await Task.Delay(5).ConfigureAwait(false);
        }

        return true;
    }

    private async Task PumpAsync()
    {
        try
        {
            await foreach (var observed in _events.ReadAllAsync(_stopping.Token).ConfigureAwait(false))
            {
                var evt = observed.Event;
                _marshal(() => _document.Dispatch(LaneId, DisplayName, evt));
                Interlocked.Increment(ref _delivered);
            }
        }
        catch (OperationCanceledException)
        {
            // The lane was disposed. Its queue may still hold events; whoever disposed it decided
            // they were not wanted, and a lane that kept draining after that would be delivering
            // into a document nobody is showing.
        }
        catch (ChannelClosedException)
        {
        }
    }

    /// <summary>Stops the drain. Announced first, so the disposal is counted even if teardown throws.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        // Started BEFORE anything is torn down: the ledger counts the attempt, not the success.
        using var counted = SessionDisposalSignal.Source.StartActivity(
            SessionDisposalLedger.LaneDisposeActivity);

        _disposed = true;
        _stopping.Cancel();
        _stopping.Dispose();
    }
}
