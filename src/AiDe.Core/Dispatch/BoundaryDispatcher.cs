using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using AiDe.Core.Facts;
using AiDe.Core.Ipc;
using AiDe.Core.Store;
using AiDe.Core.Store.Records;
using Microsoft.Data.Sqlite;

namespace AiDe.Core.Dispatch;

/// <summary>The write-ahead half of a dispatch, as answered by whoever owns the store.</summary>
/// <param name="Receipt">
/// The receipt as it now stands: an existing one when this key was already attempted, otherwise the
/// freshly written <see cref="DispatchState.Pending"/> attempt.
/// </param>
/// <param name="AlreadyAttempted">
/// True when a prior attempt was found and nothing new was written. The caller must NOT write to the
/// terminal — that is what makes a retry safe rather than a second prompt in the agent's session.
/// </param>
public sealed record DispatchBeginResult(DispatchReceipt Receipt, bool AlreadyAttempted);

/// <summary>
/// The durable half of dispatch, split so it can be answered across the daemon boundary.
/// </summary>
/// <remarks>
/// <para><b>Why this split exists.</b> D1 settled that terminal processes live in the shell while the
/// store lives in the daemon, so the two halves of a two-phase delivery are now in <i>different
/// processes</i>: only the shell can write to the pty, and only the daemon can make the attempt
/// durable. <see cref="DispatchService"/> does both in one call and remains correct in-process; this
/// is the same choreography with the side effect lifted out.</para>
///
/// <para><b>The crash window got bigger, which makes the write-ahead matter more, not less.</b>
/// In-process the window between "attempt recorded" and "outcome recorded" was a pty write. Across
/// the boundary it is a pty write plus two IPC round trips plus the possibility that the shell dies
/// while the daemon lives. Every one of those leaves a <see cref="DispatchState.Pending"/> row, which
/// <see cref="DispatchService.SweepPendingToUnknown"/> resolves to an honest
/// <see cref="DispatchState.DeliveryUnknown"/> rather than a missing row a retry would read as
/// "never sent".</para>
/// </remarks>
public sealed class BoundaryDispatcher(WorkspaceStore store) : IAttemptLedger
{
    private readonly DispatchService _dispatch = new(store);

    /// <summary>
    /// Phase 1 — make the attempt durable. Runs where the STORE is.
    /// </summary>
    /// <remarks>
    /// The session-binding check deliberately does <b>not</b> happen here: this process has no
    /// session to check against. It is the caller's obligation, asserted in
    /// <see cref="BeginAndWriteAsync"/> before this is ever called, because a check performed against
    /// a value the caller also supplied would prove nothing.
    /// </remarks>
    public DispatchBeginResult Begin(DispatchCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Any prior attempt wins, including a still-Pending one. Reading first is what makes a
        // retry safe, and across a boundary a retry is no longer hypothetical — a lost reply is an
        // ordinary event.
        var existing = _dispatch.ReadReceipt(command.DispatchKey);
        if (existing is not null)
        {
            return new DispatchBeginResult(existing, AlreadyAttempted: true);
        }

        if (command.WorkspaceEpoch != store.CoreEpoch)
        {
            throw new WorkspaceStoreException(DispatchErrorCodes.EpochStale,
                $"command epoch {command.WorkspaceEpoch} is not the current {store.CoreEpoch}");
        }

        using (var writer = store.BeginWrite())
        {
            writer.SavePromptRevision(command.DraftId, command.RevisionNo, command.Body);
            writer.RecordDispatchAttempt(
                command.DispatchKey, command.WorkspaceId, command.WorkspaceEpoch,
                command.DraftId, command.RevisionNo, command.SessionId, command.SessionGeneration);
            writer.Commit();
        }

        return new DispatchBeginResult(_dispatch.ReadReceipt(command.DispatchKey)!, AlreadyAttempted: false);
    }

    /// <summary>
    /// Phase 2 — record the outcome of the side effect the caller performed. Runs where the STORE is.
    /// </summary>
    /// <remarks>
    /// Finalizing a key that has already been finalized is a no-op returning the existing receipt,
    /// not an error: a retried finalize after a lost reply must not turn a delivered prompt into a
    /// failure.
    /// </remarks>
    public DispatchReceipt Finalize(string dispatchKey, DispatchState state, string? errorCode)
    {
        ArgumentException.ThrowIfNullOrEmpty(dispatchKey);

        var existing = _dispatch.ReadReceipt(dispatchKey)
            ?? throw new WorkspaceStoreException(DispatchErrorCodes.SessionUnknown,
                $"no dispatch attempt exists for key '{dispatchKey}'");

        if (existing.State != DispatchState.Pending)
        {
            return existing;
        }

        using (var writer = store.BeginWrite())
        {
            writer.RecordDispatchOutcome(dispatchKey, state, errorCode);
            writer.Commit();
        }

        return _dispatch.ReadReceipt(dispatchKey)!;
    }

    /// <summary>Maps a pty result onto the durable outcome. One place, so both hosting modes agree.</summary>
    public static (DispatchState State, string? ErrorCode) Outcome(PtyWriteResult result) => result switch
    {
        PtyWriteResult.Accepted => (DispatchState.PtyWriteAccepted, null),
        PtyWriteResult.GenerationChanged => (DispatchState.Rejected, DispatchErrorCodes.GenerationChanged),
        _ => (DispatchState.Failed, DispatchErrorCodes.WriteFailed),
    };

    /// <summary>
    /// The caller's side of the choreography: begin, write to the session it owns, finalize.
    /// </summary>
    /// <remarks>
    /// <para>Written once, here, and given the two durable phases as delegates — so the shell talking
    /// to a daemon and a core talking to itself run <b>the same ordering</b>. A second copy of this
    /// sequence for the remote case is how the two modes would drift into disagreeing about when the
    /// attempt becomes durable.</para>
    /// </remarks>
    /// <param name="readiness">
    /// Whether the session is known to be waiting for input. **Refused unless
    /// <see cref="SessionReadiness.Ready"/>**, and refused BEFORE anything is made durable.
    /// </param>
    public static async Task<DispatchReceipt> BeginAndWriteAsync(
        DispatchCommand command,
        ITerminalSession session,
        Func<DispatchCommand, CancellationToken, Task<DispatchBeginResult>> begin,
        Func<string, DispatchState, string?, CancellationToken, Task<DispatchReceipt>> finalize,
        CancellationToken cancellationToken = default,
        SessionReadiness readiness = SessionReadiness.Ready)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(session);

        // Refused BEFORE the write-ahead, so a refusal leaves no durable attempt behind — there is
        // nothing to sweep to DeliveryUnknown, because nothing was attempted.
        //
        // Measured, not theoretical: dispatched into a real agent CLI showing its first-run trust
        // gate, the write was accepted and the prompt was consumed by that dialog, where the same
        // Enter that submits a prompt confirms "No, exit". PtyWriteAccepted was true about the bytes
        // and misleading about the outcome (spikes/agent-dispatch).
        if (readiness != SessionReadiness.Ready)
        {
            throw new WorkspaceStoreException(
                DispatchErrorCodes.SessionNotReady, SessionReadinessPolicy.Explain(readiness));
        }

        // Checked HERE because this is the only place that holds the real session. Sending the
        // session id across the boundary and comparing it there would compare the caller's claim
        // with the caller's claim.
        if (!string.Equals(session.SessionId, command.SessionId, StringComparison.Ordinal))
        {
            throw new WorkspaceStoreException(DispatchErrorCodes.SessionUnknown,
                $"session '{command.SessionId}' was not the supplied session '{session.SessionId}'");
        }

        var began = await begin(command, cancellationToken).ConfigureAwait(false);
        if (began.AlreadyAttempted)
        {
            return began.Receipt;
        }

        PtyWriteResult result;
        try
        {
            result = await session
                .WriteAsync(command.SessionGeneration, Encoding.UTF8.GetBytes(command.Body), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return await finalize(
                command.DispatchKey, DispatchState.TimedOut, DispatchErrorCodes.WriteFailed, CancellationToken.None)
                .ConfigureAwait(false);
        }

        var (state, errorCode) = Outcome(result);

        // CancellationToken.None on purpose: the write already happened, and abandoning the finalize
        // because the caller's token tripped would leave a delivered prompt recorded as Pending and
        // later swept to DeliveryUnknown — losing an outcome we actually have.
        return await finalize(command.DispatchKey, state, errorCode, CancellationToken.None)
            .ConfigureAwait(false);
    }

    // ---- The native-route attempt gate (ADR-0041 §1, docs/design/workbench-loops-wl-1a.md). Runs where the STORE is.

    /// <summary>G0 — make the attempt durable, or return the attempt that already holds this key.</summary>
    /// <remarks>
    /// Read-before-write inside ONE writer transaction: the existence check and the insert cannot be split by another
    /// writer, because the writer holds the store's single-writer semaphore. Any prior attempt wins, even after the
    /// epoch moved — exactly <see cref="Begin"/>'s rule, so a retry after a lost reply returns the receipt.
    /// </remarks>
    public AttemptBeginResult BeginAttempt(AttemptRow attempt)
    {
        ArgumentNullException.ThrowIfNull(attempt);

        bool existed;
        using (var writer = store.BeginWrite())
        {
            existed = writer.AttemptExists(attempt.DispatchKey);
            if (!existed)
            {
                if (attempt.WorkspaceEpoch != store.CoreEpoch)
                {
                    throw new WorkspaceStoreException(DispatchErrorCodes.EpochStale,
                        $"attempt epoch {attempt.WorkspaceEpoch} is not the current {store.CoreEpoch}");
                }

                writer.AppendAttempt(attempt);
                writer.Commit();
            }
        }

        return new AttemptBeginResult(ReadAttempt(attempt.DispatchKey)!, AlreadyAttempted: existed);
    }

    /// <summary>G1–G3 — append one outcome event to an attempt, unless the attempt may not take it.</summary>
    /// <remarks>
    /// <para>Refused (not committed, never thrown): a second <see cref="DispatchState.Dispatched"/>; a
    /// <c>Dispatched</c> after a terminal — a sweep that resolved the attempt first wins, and no prompt may follow it
    /// (design, DSA Blocker 1); a second terminal — the first stands (ADR-0041 §2). The checks run in the writer
    /// transaction; the partial unique indexes are the net, and their violation maps to the same answer.</para>
    /// </remarks>
    public AttemptAppendResult AppendAttemptOutcome(OutcomeRow outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);

        var committed = false;
        using (var writer = store.BeginWrite())
        {
            if (Admits(writer, outcome))
            {
                try
                {
                    writer.AppendOutcome(outcome);
                    writer.Commit();
                    committed = true;
                }
                catch (SqliteException error) when (error.SqliteErrorCode == ConstraintViolation && error.Message.Contains("UNIQUE", StringComparison.Ordinal))
                {
                    committed = false;
                }
            }
        }

        return new AttemptAppendResult(ReadAttempt(outcome.DispatchKey)!, committed);
    }

    /// <summary>SQLITE_CONSTRAINT; the message names UNIQUE when a partial unique index raised it.</summary>
    private const int ConstraintViolation = 19;

    private static bool Admits(StoreWriter writer, OutcomeRow outcome) => outcome.State switch
    {
        DispatchState.Dispatched => !writer.HasOutcome(outcome.DispatchKey, [DispatchState.Dispatched, .. AttemptReceipt.TerminalStates]),
        var state when AttemptReceipt.TerminalStates.Contains(state) => !writer.HasOutcome(outcome.DispatchKey, AttemptReceipt.TerminalStates),
        _ => true,
    };

    /// <summary>The attempt row and every outcome event on it, in total order; null when the key was never attempted.</summary>
    public AttemptReceipt? ReadAttempt(string dispatchKey)
    {
        ArgumentException.ThrowIfNullOrEmpty(dispatchKey);

        using var reader = store.BeginRead();
        var attempt = reader.ReadAttempt(dispatchKey);
        return attempt is null ? null : new AttemptReceipt(attempt, reader.ReadOutcomes(dispatchKey));
    }

    /// <summary>Resolves every abandoned native attempt to <see cref="DispatchState.DeliveryUnknown"/> with a cause.</summary>
    /// <remarks>
    /// One writer transaction: the candidates are selected, the cause computed and the rows inserted inside it (design,
    /// DSA 4) — never the PTY sweep's read-then-write. The caller's live set is excluded; so is a queued successor whose
    /// predecessor has no <c>ResponseComplete</c> (it folds to "Queued — awaits you"). PTY rows are not candidates.
    /// </remarks>
    public AttemptSweepResult SweepAttempts(AttemptSweepRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var live = request.LiveKeys.ToHashSet(StringComparer.Ordinal);
        var swept = new List<OutcomeRow>();
        using (var writer = store.BeginWrite())
        {
            foreach (var (key, dispatched) in writer.UnresolvedNativeAttempts())
            {
                if (live.Contains(key))
                {
                    continue;
                }

                var row = new OutcomeRow(key, DispatchState.DeliveryUnknown, DispatchErrorCodes.DeliveryUnknown,
                    dispatched ? request.DispatchedCause : "never_dispatched");
                writer.AppendOutcome(row);
                swept.Add(row);
            }

            writer.Commit();
        }

        return new AttemptSweepResult(swept, WorkspaceStore.Iso(DateTimeOffset.UtcNow));
    }

    Task<AttemptBeginResult> IAttemptLedger.BeginAttemptAsync(AttemptRow attempt, CancellationToken cancellationToken) =>
        Task.FromResult(BeginAttempt(attempt));

    Task<AttemptAppendResult> IAttemptLedger.AppendAttemptOutcomeAsync(OutcomeRow outcome, CancellationToken cancellationToken) =>
        Task.FromResult(AppendAttemptOutcome(outcome));

    Task<AttemptReceipt?> IAttemptLedger.ReadAttemptAsync(string dispatchKey, CancellationToken cancellationToken) =>
        Task.FromResult(ReadAttempt(dispatchKey));

    Task<AttemptSweepResult> IAttemptLedger.SweepAttemptsAsync(AttemptSweepRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(SweepAttempts(request));
}

/// <summary>The stable codes of the native-route attempt gate (docs/design/workbench-loops-wl-1a.md).</summary>
public static class AttemptErrorCodes
{
    /// <summary>G1: the envelope's current projection is not the one the attempt was prepared from (AC-E04b).</summary>
    public const string StalePrepare = "AIDE-ATTEMPT-STALE-PREPARE";

    /// <summary>The attempt ended before its <c>Dispatched</c> row committed: no prompt was sent, so Retry is safe.</summary>
    public const string NotDispatched = "AIDE-ATTEMPT-NOT-DISPATCHED";

    /// <summary>A second terminal was offered for an attempt that already has one; the first stands.</summary>
    public const string SecondTerminal = "AIDE-ATTEMPT-SECOND-TERMINAL";

    /// <summary>Another attempt host already holds this workspace; a second one can neither dispatch nor sweep.</summary>
    public const string HostNotSole = "AIDE-ATTEMPT-HOST-NOT-SOLE";
}

/// <summary>The native-route dispatch key (ADR-0041 §1). One home: the host and the binding compute the same key.</summary>
public static class AttemptKey
{
    /// <summary>Joins the four fields; U+001F cannot occur in an id or a sha, so no two field tuples share a preimage.</summary>
    private const char Separator = '\u001f';

    /// <summary>
    /// <c>sha256(envelope_id ‖ projection_sha ‖ queued_after_dispatch_key ‖ parent_dispatch_key)</c>, lower-case hex,
    /// 64 characters. Deterministic from the prepared envelope, so a retried click or a lost reply recomputes it.
    /// </summary>
    public static string For(string envelopeId, string projectionSha, string? queuedAfterDispatchKey, string? parentDispatchKey)
    {
        ArgumentException.ThrowIfNullOrEmpty(envelopeId);
        ArgumentException.ThrowIfNullOrEmpty(projectionSha);

        var joined = string.Join(Separator, envelopeId, projectionSha, queuedAfterDispatchKey ?? "", parentDispatchKey ?? "");
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(joined)));
    }
}

/// <summary>One attempt as the store holds it: the attempt row and its outcome events in total order.</summary>
/// <remarks>Every member past the two fields is DERIVED on read (DM: derive, don't store) and never crosses the wire.</remarks>
public sealed record AttemptReceipt(AttemptRow Attempt, IReadOnlyList<OutcomeRow> Outcomes)
{
    /// <summary>The terminal states — exactly the set the store's <c>ux_outcome_one_terminal</c> index names.</summary>
    public static readonly IReadOnlySet<DispatchState> TerminalStates = new HashSet<DispatchState>
    {
        DispatchState.ResponseComplete, DispatchState.Failed, DispatchState.Interrupted,
        DispatchState.DeliveryUnknown, DispatchState.Rejected,
    };

    [JsonIgnore]
    public string DispatchKey => Attempt.DispatchKey;

    /// <summary>The one <see cref="DispatchState.Dispatched"/> row, or null when the prompt was never launched.</summary>
    [JsonIgnore]
    public OutcomeRow? Dispatched => Outcomes.FirstOrDefault(o => o.State == DispatchState.Dispatched);

    /// <summary>The one terminal row (the lowest ingress sequence wins, ADR-0041 §2), or null while the attempt is open.</summary>
    [JsonIgnore]
    public OutcomeRow? Terminal => Outcomes.FirstOrDefault(o => TerminalStates.Contains(o.State));

    /// <summary>An attempt row with no outcome event yet — the PTY path's meaning of Pending, kept.</summary>
    [JsonIgnore]
    public bool IsPending => Outcomes.Count == 0;
}

/// <param name="Receipt">The attempt as it now stands.</param>
/// <param name="AlreadyAttempted">True when the key existed and nothing was written: the caller launches NOTHING.</param>
public sealed record AttemptBeginResult(AttemptReceipt Receipt, bool AlreadyAttempted);

/// <param name="Receipt">The attempt as it now stands.</param>
/// <param name="Committed">
/// False when the store already held a row this one may not join — a second <see cref="DispatchState.Dispatched"/>
/// or a second terminal. A host whose <c>Dispatched</c> did not commit sends no prompt.
/// </param>
public sealed record AttemptAppendResult(AttemptReceipt Receipt, bool Committed);

/// <param name="LiveKeys">The attempts the calling host still runs; never swept.</param>
/// <param name="DispatchedCause">
/// The cause written on an attempt that has a <see cref="DispatchState.Dispatched"/> row: <c>host_restart</c> at host
/// startup, <c>relay_lost</c> after an engine exit. One with no <c>Dispatched</c> row is always <c>never_dispatched</c>.
/// </param>
public sealed record AttemptSweepRequest(IReadOnlyList<string> LiveKeys, string DispatchedCause);

/// <param name="Swept">One <see cref="DispatchState.DeliveryUnknown"/> row per attempt this sweep resolved.</param>
/// <param name="SweptAt">When the sweep ran — <c>last_sweep_at</c>, emitted even when nothing was swept.</param>
public sealed record AttemptSweepResult(IReadOnlyList<OutcomeRow> Swept, string SweptAt);
