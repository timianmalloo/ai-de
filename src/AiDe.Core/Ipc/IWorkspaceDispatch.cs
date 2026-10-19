using AiDe.Core.Dispatch;
using AiDe.Core.Facts;
using AiDe.Core.Store.Records;

namespace AiDe.Core.Ipc;

/// <summary>
/// The two durable phases of prompt dispatch, as a caller sees them.
/// </summary>
/// <remarks>
/// <para>Separate from <see cref="IWorkspaceCommands"/> because it is a different obligation: a
/// workspace can answer projections and re-index without being able to record a dispatch, and a
/// shell that cannot dispatch should discover that by the capability being absent rather than by a
/// call failing.</para>
///
/// <para><b>The side effect is deliberately not here.</b> Writing to the terminal is the shell's
/// job — it owns the process (D1) — so this interface covers only what must be durable, and
/// <see cref="BoundaryDispatcher.BeginAndWriteAsync"/> is what orders the three steps.</para>
/// </remarks>
public interface IWorkspaceDispatch
{
    /// <summary>The epoch a command must carry to be accepted.</summary>
    Task<long> EpochAsync(CancellationToken cancellationToken);

    /// <summary>Phase 1 — make the attempt durable before any byte leaves the shell.</summary>
    Task<DispatchBeginResult> DispatchBeginAsync(DispatchCommand command, CancellationToken cancellationToken);

    /// <summary>Phase 2 — record the outcome the shell observed.</summary>
    Task<DispatchReceipt> DispatchFinalizeAsync(
        string dispatchKey, DispatchState state, string? errorCode, CancellationToken cancellationToken);
}

/// <summary>
/// The store half of a native-route execution attempt (ADR-0041 §1), as the host sees it.
/// </summary>
/// <remarks>
/// <para><b>Two implementers, one gate.</b> <see cref="BoundaryDispatcher"/> answers it in-process (the daemon, and the
/// tests over a real store); <see cref="WorkspaceClient"/> answers it over the daemon boundary (the App, which never
/// opens the store, D1). Each call is one store transaction on the daemon side, so the gate is the same code either
/// way (docs/design/workbench-loops-wl-1a.md, "The gate and the write order").</para>
/// </remarks>
public interface IAttemptLedger
{
    /// <summary>G0: read-before-write in one transaction. An existing key returns its receipt and writes nothing.</summary>
    Task<AttemptBeginResult> BeginAttemptAsync(AttemptRow attempt, CancellationToken cancellationToken);

    /// <summary>G1–G3: one outcome event. A second <c>Dispatched</c> or a second terminal is not committed.</summary>
    Task<AttemptAppendResult> AppendAttemptOutcomeAsync(OutcomeRow outcome, CancellationToken cancellationToken);

    /// <summary>The attempt and its outcome events, or null when the key was never attempted.</summary>
    Task<AttemptReceipt?> ReadAttemptAsync(string dispatchKey, CancellationToken cancellationToken);

    /// <summary>Resolves abandoned native attempts to <c>DeliveryUnknown{cause}</c>; the caller names what it still runs.</summary>
    Task<AttemptSweepResult> SweepAttemptsAsync(AttemptSweepRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// The operator's withdraw of a never-launched attempt: one transaction appends <c>Rejected{never_dispatched}</c>
    /// and the withdraw command's completed receipt (command id = the dispatch key). An attempt that already has a
    /// <c>Dispatched</c> row or a terminal commits nothing — so a retry returns the original outcome.
    /// </summary>
    Task<AttemptAppendResult> WithdrawAttemptAsync(string dispatchKey, CancellationToken cancellationToken);
}
