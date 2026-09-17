namespace AiDe.Core.Watcher;

/// <summary>
/// The non-secret identity a terminal/agent session presents when it registers with the watcher - the
/// attributes the coordination-contract register event carries (US-4/US-6). Harness and model are
/// optional (a plain shell has neither); everything else is required for a well-formed registration.
/// </summary>
public sealed record SessionCoordinationIdentity(
    string RepoPath,
    string RepoDisplay,
    string WorktreeBranch,
    string WorktreePath,
    string TerminalId,
    string AgentName,
    string? Harness = null,
    string? HarnessVersion = null,
    string? Model = null,
    string? ModelVersion = null)
{
    /// <summary>Maps the identity onto the OTel attribute keys the register event uses.</summary>
    public IReadOnlyDictionary<string, string?> ToAttributes()
    {
        var attrs = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [OtelAttributes.RepoPath] = RepoPath,
            [OtelAttributes.RepoDisplay] = RepoDisplay,
            [OtelAttributes.WorktreeBranch] = WorktreeBranch,
            [OtelAttributes.WorktreePath] = WorktreePath,
            [OtelAttributes.TerminalId] = TerminalId,
            [OtelAttributes.AgentName] = AgentName,
        };

        // Only emitted when known - an absent harness/model is Not Recorded, never a guessed value (US-13).
        if (!string.IsNullOrEmpty(Harness))
        {
            attrs[OtelAttributes.ServiceName] = Harness;
            attrs[OtelAttributes.ServiceVersion] = HarnessVersion ?? string.Empty;
        }

        if (!string.IsNullOrEmpty(Model))
        {
            attrs[OtelAttributes.GenAiModel] = Model;
            attrs[OtelAttributes.GenAiModelVersion] = ModelVersion ?? string.Empty;
        }

        return attrs;
    }
}

/// <summary>
/// Writes the coordination-contract log a session opts in with so it appears in the watcher (US-4): a
/// register on start, periodic heartbeats while alive (so liveness stays Alive rather than going Stale),
/// and a session-end on close. This is the app-side WRITER; the <see cref="WatcherHost"/>'s pump is the
/// reader. Running both in one process is what makes a terminal launched in the app show up live.
/// </summary>
/// <remarks>
/// <para>Pure and explicit (Register / Heartbeat / HeartbeatAll / End) - no timer of its own, so it is
/// fully testable; the caller (the shell) drives heartbeats on whatever timer it already runs. It tracks
/// the live session ids so <see cref="HeartbeatAll"/> can keep them all alive with one call.</para>
/// <para>Re-reading the whole coordination log directory is idempotent on the reader side (registration
/// is keyed by external id), so a duplicate register is harmless; the emitter still guards against
/// re-registering an id it already tracks, to keep the log clean.</para>
/// </remarks>
public sealed class SessionCoordinationEmitter : IDisposable
{
    private readonly CoordContractWriter _writer;
    private readonly CoordinationEmitterBudget _budget;
    private readonly Dictionary<string, CoordinationEmitterState> _states = new(StringComparer.Ordinal);
    private readonly object _gate = new();
    private readonly Dictionary<string, SessionGate> _sessionGates = new(StringComparer.Ordinal);
    private bool _retired;
    private bool _batch;

    public SessionCoordinationEmitter(CoordContractWriter writer)
        : this(writer, CoordinationEmitterBudget.Production) { }

    internal SessionCoordinationEmitter(CoordContractWriter writer, CoordinationEmitterBudget budget)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _budget = budget;
    }

    /// <summary>Committed membership only; pending registrations are excluded.</summary>
    public int LiveCount
    {
        get { lock (_gate) { return _states.Values.Count(state => state.Registered); } }
    }

    public int RetainedCount { get { lock (_gate) { return _states.Count; } } }
    internal string[] OwnedSessionIds { get { lock (_gate) { return [.. _states.Keys]; } } }

    /// <summary>Payload, UTF-16 input/metadata and fingerprint bytes; excludes fixed CLR object overhead.</summary>
    public long RetainedDataBytes
    {
        get { lock (_gate) { return _states.Values.Sum(state => state.RetainedDataBytes); } }
    }

    public CoordinationEmitterResult RegisterResult(string session, SessionCoordinationIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        return RegisterCore(session, () => identity.ToAttributes());
    }

    public CoordinationEmitterResult RegisterResult(string session, IReadOnlyDictionary<string, string?> attributes)
    {
        ArgumentNullException.ThrowIfNull(attributes);
        return RegisterCore(session, () => attributes);
    }

    private CoordinationEmitterResult RegisterCore(string session, Func<IReadOnlyDictionary<string, string?>> input) =>
        ForSession(session, CoordinationEmitterOperation.Register, state =>
        {
            if (state.Registered && state.Pending is null) return Result(state, CoordinationEmitterOutcome.NoOp);
            if (state.Pending is not null && state.Pending != CoordinationEmitterOperation.Register)
                return Result(state, CoordinationEmitterOutcome.Refused, CoordinationEmitterCodes.InputConflict);
            var frozen = CoordinationEmitterState.Freeze(session, input());
            if (state.Pending is not null &&
                (state.Pending != CoordinationEmitterOperation.Register || !state.SameInput(frozen)))
            {
                return Result(state, CoordinationEmitterOutcome.Refused, CoordinationEmitterCodes.InputConflict);
            }
            if (state.Pending is null) Start(state, CoordinationEmitterOperation.Register, frozen);
            return Execute(state);
        });

    public CoordinationEmitterResult HeartbeatResult(string session) => Control(session, CoordinationEmitterOperation.Heartbeat);
    public CoordinationEmitterResult EndResult(string session) => Control(session, CoordinationEmitterOperation.End);
    public CoordinationEmitterResult RetryPending(string session) =>
        ForSession(session, CoordinationEmitterOperation.Retry, state => state.Pending is null
            ? Result(state, CoordinationEmitterOutcome.NoOp) : Execute(state));

    public CoordinationEmitterResult TryAbandonPending(string session) =>
        ForSession(session, CoordinationEmitterOperation.Abandon, state =>
        {
            if (state.Pending is null) return Result(state, CoordinationEmitterOutcome.NoOp);
            if (!state.ProvenNoWrite || state.Prepared?.Attempted == true)
                return Result(state, CoordinationEmitterOutcome.Uncertain, CoordinationEmitterCodes.WriteUncertain);
            lock (_gate)
            {
                state.ClearPending();
                return Result(state, CoordinationEmitterOutcome.Abandoned);
            }
        }, wait: false);

    public void Register(string externalSessionId, SessionCoordinationIdentity identity) =>
        ThrowIfFailed(RegisterResult(externalSessionId, identity));
    public void Heartbeat(string externalSessionId) => ThrowIfFailed(HeartbeatResult(externalSessionId));
    public void End(string externalSessionId) => ThrowIfFailed(EndResult(externalSessionId));

    /// <summary>
    /// Blocking compatibility bridge, not a UI-safe deadline. Workers never capture the caller's
    /// context and every asynchronous join uses ConfigureAwait(false).
    /// </summary>
    public void HeartbeatAll()
    {
        var results = HeartbeatBatchAsync(default, true).ConfigureAwait(false).GetAwaiter().GetResult();
        var failed = results.Where(result => !result.Succeeded).ToArray();
        if (failed.Length > 0) throw new CoordinationEmitterBatchException(failed);
    }

    public Task<IReadOnlyList<CoordinationEmitterResult>> HeartbeatAllResultsAsync(
        CancellationToken cancellationToken = default) => HeartbeatBatchAsync(cancellationToken, false);

    private async Task<IReadOnlyList<CoordinationEmitterResult>> HeartbeatBatchAsync(
        CancellationToken cancellationToken, bool reconcileWriterBusy)
    {
        CoordinationEmitterState[] sessions;
        lock (_gate)
        {
            if (_batch)
                return [new("", CoordinationEmitterMembership.Unknown, CoordinationEmitterOutcome.Busy,
                    CoordinationEmitterCodes.Busy)];
            if (_retired)
                return [new("", CoordinationEmitterMembership.Unknown, CoordinationEmitterOutcome.Refused,
                    CoordinationEmitterCodes.Retired)];
            _batch = true;
            sessions = [.. _states.Values.Where(state => state.Registered)];
        }
        var work = new List<Task<CoordinationEmitterResult>>(sessions.Length);
        foreach (var session in sessions)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                work.Add(Task.FromResult(Result(session, CoordinationEmitterOutcome.Busy,
                    CoordinationEmitterCodes.Cancelled)));
                continue;
            }
            var lease = Acquire(session.Session, CoordinationEmitterOperation.Heartbeat, false, out var failure, session);
            work.Add(lease is null ? Task.FromResult(failure!) :
                Task.Run(() => RunLease(session.Session, lease, CoordinationEmitterOperation.Heartbeat,
                    state => PerformControl(state, CoordinationEmitterOperation.Heartbeat))));
        }
        var completion = FinishBatch(work);
        try
        {
            var results = (await completion.WaitAsync(cancellationToken).ConfigureAwait(false)).ToArray();
            // Reconciliation may retry contention, never transfer work to a replacement lifecycle.
            if (reconcileWriterBusy)
                for (var index = 0; index < results.Length; index++)
                    if (results[index].Code == CoordinationWriteCodes.WriterBusy)
                        results[index] = ForSession(sessions[index].Session, CoordinationEmitterOperation.Heartbeat,
                            state => PerformControl(state, CoordinationEmitterOperation.Heartbeat),
                            wait: false, expected: sessions[index]);
            return results;
        }
        catch (OperationCanceledException)
        {
            // The join continues owning _batch and leases. Cancellation is not an I/O acknowledgement.
            return work.Select((task, index) => task.IsCompletedSuccessfully
                ? task.GetAwaiter().GetResult()
                : Result(sessions[index], CoordinationEmitterOutcome.InFlight,
                    CoordinationEmitterCodes.InFlight)).ToArray();
        }
    }

    private async Task<IReadOnlyList<CoordinationEmitterResult>> FinishBatch(
        List<Task<CoordinationEmitterResult>> work)
    {
        try { return await Task.WhenAll(work).ConfigureAwait(false); }
        finally { lock (_gate) { _batch = false; } }
    }

    public bool TryRetire()
    {
        lock (_gate)
        {
            if (_states.Count != 0 || _sessionGates.Count != 0 || _batch) return false;
            _retired = true;
            return true;
        }
    }

    public void Dispose()
    {
        if (!TryRetire()) throw new CoordinationEmitterException(
            new("", CoordinationEmitterMembership.Unknown, CoordinationEmitterOutcome.Busy,
                CoordinationEmitterCodes.Obligations));
    }

    private CoordinationEmitterResult Control(string session, CoordinationEmitterOperation operation) =>
        ForSession(session, operation, state => PerformControl(state, operation));

    private CoordinationEmitterResult PerformControl(CoordinationEmitterState state, CoordinationEmitterOperation operation)
    {
        if (state.Pending is not null && state.Pending != operation)
            return Result(state, CoordinationEmitterOutcome.Refused, CoordinationEmitterCodes.InputConflict);
        if (!state.Registered) return Result(state, CoordinationEmitterOutcome.NoOp);
        if (state.Pending is null) Start(state, operation, null);
        return Execute(state);
    }

    private void Start(CoordinationEmitterState state, CoordinationEmitterOperation operation,
        Dictionary<string, string?>? input)
    {
        lock (_gate) { state.Pending = operation; state.Frozen = input; state.ProvenNoWrite = true; }
    }

    private CoordinationEmitterResult Execute(CoordinationEmitterState state)
    {
        if (state.BlockedCode is not null) return Result(state, CoordinationEmitterOutcome.Refused, state.BlockedCode);
        if (state.Prepared is null)
        {
            if (state.Phase == CoordinationEmitterPhase.Uncertain)
                return Result(state, CoordinationEmitterOutcome.Uncertain, CoordinationEmitterCodes.RecoveryRequired);
            lock (_gate) { state.Phase = CoordinationEmitterPhase.Preparing; state.ProvenNoWrite = false; }
            var preparation = _writer.Prepare(state.Kind, state.Session, state.Frozen);
            lock (_gate)
            {
                // Capture the object before any append; a failure result may carry no Prepared at all.
                if (preparation.Prepared is not null)
                {
                    state.Prepared = preparation.Prepared;
                    state.Frozen = null;
                }
                state.ProvenNoWrite = preparation.Status != CoordinationWriteStatus.Uncertain;
                state.Phase = state.Prepared is null
                    ? CoordinationEmitterPhase.AwaitingPreparation : CoordinationEmitterPhase.Ready;
            }
            if (preparation.Status != CoordinationWriteStatus.Ready)
                return FromWrite(state, preparation);
        }
        var prepared = state.Prepared!;
        if (prepared.Admission.ByteCount > CoordContractWriter.MaximumLineBytes ||
            state.MetadataCodeUnits > CoordinationEmitterState.MaximumCodeUnits)
        {
            lock (_gate)
            {
                state.Prepared = null;
                state.BlockedCode = CoordinationEmitterCodes.RetentionBound;
                state.ProvenNoWrite = true;
            }
            return Result(state, CoordinationEmitterOutcome.Refused, CoordinationEmitterCodes.RetentionBound);
        }
        lock (_gate) { state.Phase = CoordinationEmitterPhase.Appending; state.ProvenNoWrite = false; }
        var appended = _writer.Append(prepared);
        lock (_gate)
        {
            state.ProvenNoWrite = !prepared.Attempted && appended.Status != CoordinationWriteStatus.Uncertain;
            state.Phase = appended.Status == CoordinationWriteStatus.Uncertain
                ? CoordinationEmitterPhase.Uncertain : CoordinationEmitterPhase.Ready;
            if (appended.Status == CoordinationWriteStatus.Admitted)
            {
                var ending = state.Pending == CoordinationEmitterOperation.End;
                state.Registered = !ending;
                state.ClearPending();
            }
            return FromWrite(state, appended);
        }
    }

    private CoordinationEmitterResult ForSession(string session, CoordinationEmitterOperation operation,
        Func<CoordinationEmitterState, CoordinationEmitterResult> action, bool wait = true,
        CoordinationEmitterState? expected = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(session);
        var lease = Acquire(session, operation, wait, out var failure, expected);
        return lease is null ? failure! : RunLease(session, lease, operation, action);
    }

    private SessionLease? Acquire(string session, CoordinationEmitterOperation operation, bool wait,
        out CoordinationEmitterResult? failure, CoordinationEmitterState? expected = null)
    {
        lock (_gate)
        {
            failure = null;
            if (expected is not null &&
                (!_states.TryGetValue(session, out var current) || !ReferenceEquals(current, expected)))
            {
                failure = Result(expected, CoordinationEmitterOutcome.Refused, CoordinationEmitterCodes.StaleLifecycle);
                return null;
            }
            if (operation == CoordinationEmitterOperation.Heartbeat &&
                expected?.Pending == CoordinationEmitterOperation.End)
            {
                failure = Result(expected, CoordinationEmitterOutcome.Busy, CoordinationEmitterCodes.Busy);
                return null;
            }
            // A completed transition still owns its state until its last lease (including observers) exits.
            if (_states.TryGetValue(session, out var completing) && !completing.Registered &&
                completing.Pending is null && completing.Phase != CoordinationEmitterPhase.Preparing &&
                _sessionGates.TryGetValue(session, out var completingGate) && completingGate.References > 0)
            {
                failure = Result(completing, CoordinationEmitterOutcome.InFlight, CoordinationEmitterCodes.InFlight);
                return null;
            }
            if (_retired) failure = SnapshotResult(session, CoordinationEmitterOutcome.Refused, CoordinationEmitterCodes.Retired);
            else if (!_states.ContainsKey(session))
            {
                if (operation != CoordinationEmitterOperation.Register)
                    failure = SnapshotResult(session, operation == CoordinationEmitterOperation.Retry
                        ? CoordinationEmitterOutcome.Uncertain : CoordinationEmitterOutcome.NoOp,
                        operation == CoordinationEmitterOperation.Retry ? CoordinationEmitterCodes.RecoveryRequired : null);
                else if (session.Length > CoordinationEmitterState.MaximumCodeUnits)
                    failure = SnapshotResult("", CoordinationEmitterOutcome.Refused, CoordinationEmitterCodes.RetentionBound);
                else if (!_budget.TryReserve(this))
                    failure = SnapshotResult(session, CoordinationEmitterOutcome.Refused, CoordinationEmitterCodes.Capacity);
                else _states.Add(session, new(session));
            }
            if (failure is not null) return null;
            if (!_sessionGates.TryGetValue(session, out var gate))
            {
                gate = new SessionGate();
                _sessionGates.Add(session, gate);
            }
            if (gate.References >= 2 || (!wait && gate.References > 0) ||
                (operation == CoordinationEmitterOperation.Heartbeat && gate.EndReferences > 0))
            {
                failure = SnapshotResult(session, !wait && gate.References > 0
                    ? CoordinationEmitterOutcome.InFlight : CoordinationEmitterOutcome.Busy, CoordinationEmitterCodes.Busy);
                return null;
            }
            gate.References++;
            if (operation == CoordinationEmitterOperation.End) gate.EndReferences++;
            return new(gate, _states[session]);
        }
    }

    private CoordinationEmitterResult RunLease(string session, SessionLease lease,
        CoordinationEmitterOperation operation, Func<CoordinationEmitterState, CoordinationEmitterResult> action)
    {
        var gate = lease.Gate;
        var acquired = false;
        try
        {
            using var activity = new System.Diagnostics.Activity("coordination.emitter");
            activity.Start();
            var started = System.Diagnostics.Stopwatch.GetTimestamp();
            gate.Semaphore.Wait();
            acquired = true;
            var result = RunAction(session, lease.State, operation, action);
            activity.SetTag("coordination.outcome", result.Outcome.ToString());
            activity.SetTag("coordination.membership", result.Membership.ToString());
            activity.SetTag("coordination.retained", RetainedCount);
            activity.SetTag("coordination.duration_ms", System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            activity.SetTag("error.type", result.Code);
            return result;
        }
        finally
        {
            lock (_gate)
            {
                // Waiting leases carry identity only; an ended lifecycle can retire before they run.
                if (acquired && operation is CoordinationEmitterOperation.End or CoordinationEmitterOperation.Abandon &&
                    _states.TryGetValue(session, out var completed) && ReferenceEquals(completed, lease.State) &&
                    !completed.Registered && completed.Pending is null)
                    Release(completed);
                if (acquired) gate.Semaphore.Release();
                if (operation == CoordinationEmitterOperation.End) gate.EndReferences--;
                if (--gate.References == 0)
                {
                    _sessionGates.Remove(session);
                    gate.Semaphore.Dispose();
                    if (_states.TryGetValue(session, out var state) && !state.Registered && state.Pending is null)
                        Release(state);
                }
            }
        }
    }

    private CoordinationEmitterResult RunAction(string session, CoordinationEmitterState expected,
        CoordinationEmitterOperation operation, Func<CoordinationEmitterState, CoordinationEmitterResult> action)
    {
        try
        {
            CoordinationEmitterState? state;
            lock (_gate)
            {
                _states.TryGetValue(session, out state);
                if (!ReferenceEquals(state, expected))
                    return Result(expected, CoordinationEmitterOutcome.Refused, CoordinationEmitterCodes.StaleLifecycle);
            }
            return action(expected);
        }
        catch (CoordinationEmitterInputException)
        {
            lock (_gate)
            {
                if (expected.Pending is null && !expected.Registered)
                {
                    expected.Pending = CoordinationEmitterOperation.Register;
                    expected.BlockedCode = CoordinationEmitterCodes.RetentionBound;
                }
            }
            return Result(expected, CoordinationEmitterOutcome.Refused, CoordinationEmitterCodes.RetentionBound);
        }
        catch (Exception)
        {
            lock (_gate)
            {
                expected.Pending ??= operation;
                expected.ProvenNoWrite = false;
                expected.Phase = CoordinationEmitterPhase.Uncertain;
            }
            return Result(expected, CoordinationEmitterOutcome.Uncertain, CoordinationEmitterCodes.WriteUncertain);
        }
    }

    private void Release(CoordinationEmitterState state)
    {
        _states.Remove(state.Session);
        _budget.Release(this);
    }

    private CoordinationEmitterResult SnapshotResult(string session, CoordinationEmitterOutcome outcome, string? code = null)
    {
        lock (_gate) { return _states.TryGetValue(session, out var state) ? Result(state, outcome, code)
            : new(session, CoordinationEmitterMembership.Unknown, outcome, code); }
    }

    private static CoordinationEmitterResult Result(CoordinationEmitterState state,
        CoordinationEmitterOutcome outcome, string? code = null, CoordinationAdmission? admission = null) =>
        new(state.Session, state.Registered ? CoordinationEmitterMembership.Live :
            state.Pending is not null || state.Phase == CoordinationEmitterPhase.Preparing
                ? CoordinationEmitterMembership.PendingRegistration : CoordinationEmitterMembership.Unknown,
            outcome, code, admission, state.Prepared, state.Phase);

    private static CoordinationEmitterResult FromWrite(CoordinationEmitterState state, CoordinationWriteResult result) =>
        Result(state, result.Status switch
        {
            CoordinationWriteStatus.Admitted => CoordinationEmitterOutcome.Admitted,
            CoordinationWriteStatus.Refused => CoordinationEmitterOutcome.Refused,
            CoordinationWriteStatus.Unavailable => CoordinationEmitterOutcome.Unavailable,
            _ => CoordinationEmitterOutcome.Uncertain
        }, result.Code, result.Admission);

    private static void ThrowIfFailed(CoordinationEmitterResult result)
    {
        if (!result.Succeeded) throw new CoordinationEmitterException(result);
    }

    private sealed record SessionLease(SessionGate Gate, CoordinationEmitterState State);

    private sealed class SessionGate
    {
        public int References;
        public int EndReferences;
        public SemaphoreSlim Semaphore { get; } = new(1, 1);
    }

    /// <summary>
    /// Reconciles the live set against the sessions that currently exist: registers a new one, heartbeats
    /// one already live, and ends one that has gone. This lets the caller drive the emitter from a simple
    /// periodic snapshot of "which sessions exist now" (e.g. the terminal surfaces in the layout) without
    /// precise per-session start/close events. <paramref name="identityFor"/> supplies the register
    /// attributes for a newly-seen session.
    /// </summary>
    public void Reconcile(IReadOnlySet<string> currentSessionIds, Func<string, SessionCoordinationIdentity> identityFor)
    {
        ArgumentNullException.ThrowIfNull(currentSessionIds);
        ArgumentNullException.ThrowIfNull(identityFor);

        foreach (var id in currentSessionIds)
        {
            bool live;
            lock (_gate) { live = _states.TryGetValue(id, out var state) && state.Registered; }
            if (live) Heartbeat(id);
            else ThrowIfFailed(RegisterCore(id, () => identityFor(id).ToAttributes()));
        }

        // End any tracked session that is no longer present.
        string[] gone;
        lock (_gate)
        {
            gone = [.. _states.Values.Where(state => state.Registered && !currentSessionIds.Contains(state.Session))
                .Select(state => state.Session)];
        }

        foreach (var id in gone)
        {
            End(id);
        }
    }
}
