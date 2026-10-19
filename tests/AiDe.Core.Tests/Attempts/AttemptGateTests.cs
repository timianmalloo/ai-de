using AiDe.Core.Dispatch;
using AiDe.Core.Facts;
using AiDe.Core.Store;
using AiDe.Core.Store.Records;

namespace AiDe.Core.Tests.Attempts;

/// <summary>
/// The store half of the native-route attempt gate (ADR-0041 §1; docs/design/workbench-loops-wl-1a.md, "The gate
/// and the write order" and "The sweep"), over a real SQLite store with T2's v2 schema.
/// </summary>
public sealed class AttemptGateTests : IDisposable
{
    private readonly TestWorkspace _workspace = TestWorkspace.Create();

    public AttemptGateTests() => Seed(_workspace.Store);

    public void Dispose() => _workspace.Dispose();

    private BoundaryDispatcher Gate => new(_workspace.Store);

    /// <summary>A journey, a notebook and revision 1 — what the attempt trigger requires to exist.</summary>
    internal static void Seed(WorkspaceStore store)
    {
        using var writer = store.BeginWrite();
        writer.AppendJourney(new JourneyRow("j-1", "work", "Attempts"));
        writer.AppendNotebook(new NotebookRow("nb-1", "j-1", Legacy: false));
        writer.AppendNotebookRevision(new NotebookRevisionRow("nb-1", 1, "do the thing"));
        writer.Commit();
    }

    private AttemptRow Attempt(string envelopeId, string? queuedAfter = null) => new(
        AttemptKey.For(envelopeId, "proj-sha", queuedAfter, null), "ws-1", _workspace.Store.CoreEpoch, "nb-1", 1, "s-1", 1,
        ProjectionSha: "proj-sha", RouteFingerprint: "acp:claude-code", QueuedAfterDispatchKey: queuedAfter);

    private OutcomeRow Begin(string envelopeId, string? queuedAfter = null)
    {
        var row = Attempt(envelopeId, queuedAfter);
        Gate.BeginAttempt(row);
        return new OutcomeRow(row.DispatchKey, DispatchState.Dispatched);
    }

    // ---- G0: same dispatch_key twice ⇒ one attempt row, the second launches nothing.

    [Fact]
    public void BeginTwice_WritesOneAttemptRow_AndTheSecondIsAlreadyAttempted()
    {
        var row = Attempt("env-1");

        var first = Gate.BeginAttempt(row);
        var second = Gate.BeginAttempt(row);

        Assert.False(first.AlreadyAttempted);
        Assert.True(second.AlreadyAttempted);
        Assert.Equal(first.Receipt.Attempt.IngressSeq, second.Receipt.Attempt.IngressSeq);
        Assert.True(second.Receipt.IsPending);
    }

    [Fact]
    public void BeginAgain_AfterTheEpochMoved_StillReturnsTheExistingReceipt()
    {
        var row = Attempt("env-1");
        Gate.BeginAttempt(row);

        var retried = Gate.BeginAttempt(row with { WorkspaceEpoch = row.WorkspaceEpoch + 7 });

        Assert.True(retried.AlreadyAttempted);
    }

    // ---- G2: the host launches only when ITS OWN Dispatched row committed.

    [Fact]
    public void ASecondDispatchedRow_IsNotCommitted()
    {
        var dispatched = Begin("env-1");

        var first = Gate.AppendAttemptOutcome(dispatched);
        var second = Gate.AppendAttemptOutcome(dispatched);

        Assert.True(first.Committed);
        Assert.False(second.Committed);
        Assert.Single(second.Receipt.Outcomes, o => o.State == DispatchState.Dispatched);
    }

    [Fact]
    public void Dispatched_AfterATerminal_IsNotCommitted()
    {
        // The sweep resolved the attempt first (design: DSA Blocker 1). A Dispatched after it must not commit,
        // or a prompt would follow a row that says it was never dispatched.
        var dispatched = Begin("env-1");
        Gate.AppendAttemptOutcome(new OutcomeRow(dispatched.DispatchKey, DispatchState.DeliveryUnknown,
            DispatchErrorCodes.DeliveryUnknown, "never_dispatched"));

        var late = Gate.AppendAttemptOutcome(dispatched);

        Assert.False(late.Committed);
        Assert.Null(late.Receipt.Dispatched);
    }

    // ---- G3: at most one terminal row per attempt.

    [Fact]
    public void ASecondTerminal_IsNotCommitted_AndTheFirstStands()
    {
        var dispatched = Begin("env-1");
        Gate.AppendAttemptOutcome(dispatched);

        var first = Gate.AppendAttemptOutcome(new OutcomeRow(dispatched.DispatchKey, DispatchState.ResponseComplete));
        var second = Gate.AppendAttemptOutcome(new OutcomeRow(dispatched.DispatchKey, DispatchState.DeliveryUnknown,
            DispatchErrorCodes.DeliveryUnknown, "relay_lost"));

        Assert.True(first.Committed);
        Assert.False(second.Committed);
        Assert.Equal(DispatchState.ResponseComplete, second.Receipt.Terminal!.State);
    }

    // ---- The sweep: predicate, causes, exclusions.

    [Fact]
    public void Sweep_ResolvesEachAbandonedAttempt_ToDeliveryUnknownWithItsCause()
    {
        var never = Begin("env-never");                       // W1/W2: no Dispatched row
        var dispatched = Begin("env-dispatched");             // W3/W4: Dispatched, no terminal
        Gate.AppendAttemptOutcome(dispatched);
        var finished = Begin("env-finished");
        Gate.AppendAttemptOutcome(finished);
        Gate.AppendAttemptOutcome(new OutcomeRow(finished.DispatchKey, DispatchState.ResponseComplete));

        var swept = Gate.SweepAttempts(new AttemptSweepRequest([], "host_restart"));

        Assert.Equal(2, swept.Swept.Count);
        Assert.Equal("never_dispatched", Gate.ReadAttempt(never.DispatchKey)!.Terminal!.Cause);
        Assert.Equal("host_restart", Gate.ReadAttempt(dispatched.DispatchKey)!.Terminal!.Cause);
        Assert.Equal(DispatchState.ResponseComplete, Gate.ReadAttempt(finished.DispatchKey)!.Terminal!.State);
        Assert.False(string.IsNullOrEmpty(swept.SweptAt));
    }

    [Fact]
    public void Sweep_SkipsLiveAttempts()
    {
        var live = Begin("env-live");

        var swept = Gate.SweepAttempts(new AttemptSweepRequest([live.DispatchKey], "host_restart"));

        Assert.Empty(swept.Swept);
        Assert.True(Gate.ReadAttempt(live.DispatchKey)!.IsPending);
    }

    [Fact]
    public void Sweep_SkipsAQueuedSuccessorWhosePredecessorLacksResponseComplete()
    {
        var predecessor = Begin("env-first");
        Gate.AppendAttemptOutcome(predecessor);
        var successor = Begin("env-second", queuedAfter: predecessor.DispatchKey);

        Gate.SweepAttempts(new AttemptSweepRequest([predecessor.DispatchKey], "host_restart"));

        Assert.True(Gate.ReadAttempt(successor.DispatchKey)!.IsPending);
    }

    [Fact]
    public void ThePtySweep_NeverTouchesANativeAttempt()
    {
        // Finding F1: the daemon sweeps PTY attempts at its own start, while the App host may still run a native one.
        var native = Begin("env-native");

        var swept = new DispatchService(_workspace.Store).SweepPendingToUnknown();

        Assert.Equal(0, swept);
        Assert.True(Gate.ReadAttempt(native.DispatchKey)!.IsPending);
    }
}
