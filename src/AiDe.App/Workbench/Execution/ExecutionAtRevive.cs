using AiDe.App.Conductor;
using AiDe.Core.Dispatch;
using AiDe.Core.Facts;

namespace AiDe.App.Workbench.Execution;

/// <summary>
/// Which execution a revived notebook opens (the WL-S design's §4): one, for the attempt that was live when the window
/// closed. The slot file records no attempts, so "was live" is read from the store: the host's startup sweep concluded
/// that attempt <c>DeliveryUnknown{host_restart}</c>, and the read-back returns it last, in dispatch order.
/// </summary>
public static class ExecutionAtRevive
{
    /// <summary>
    /// The LAST read-back receipt (dispatch order) when its terminal is <c>DeliveryUnknown{host_restart}</c>, else null.
    /// An earlier swept attempt is not opened: a later turn ran after it, so the window did not close on it.
    /// </summary>
    /// <param name="readBack">The notebook's read-back, in dispatch order (<see cref="Records.NotebookRuns.ReadBack"/>).</param>
    public static AttemptReceipt? LiveAtClose(IReadOnlyList<AttemptReceipt> readBack)
    {
        ArgumentNullException.ThrowIfNull(readBack);
        return readBack is [.., { Terminal: { State: DispatchState.DeliveryUnknown, Cause: AttemptCauses.HostRestart } } last] ? last : null;
    }
}
