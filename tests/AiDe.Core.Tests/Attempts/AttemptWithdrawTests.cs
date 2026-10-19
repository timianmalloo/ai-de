using AiDe.Core.Dispatch;
using AiDe.Core.Facts;
using AiDe.Core.Ipc;
using AiDe.Core.Store.Records;

namespace AiDe.Core.Tests.Attempts;

/// <summary>
/// The store half of the withdraw (seam req-01M59G3YK7REPTB5W3EDSDKDN7; the Owner's shape on report 8): ONE writer
/// transaction appends the existing terminal <c>Rejected{never_dispatched}</c> and the withdraw command's completed
/// <c>command_receipt_fact</c> row. "Withdrawn" is derived from the two together, never from the terminal alone.
/// </summary>
public sealed class AttemptWithdrawTests : IDisposable
{
    private readonly TestWorkspace _workspace = TestWorkspace.Create();

    public AttemptWithdrawTests() => AttemptGateTests.Seed(_workspace.Store);

    public void Dispose() => _workspace.Dispose();

    private BoundaryDispatcher Gate => new(_workspace.Store);

    private AttemptRow Held()
    {
        var row = new AttemptRow(
            AttemptKey.For("env-1", "proj-sha", null, null), "ws-1", _workspace.Store.CoreEpoch, "nb-1", 1, "s-1", 1,
            ProjectionSha: "proj-sha", RouteFingerprint: "acp:claude-code");
        Gate.BeginAttempt(row);
        return row;
    }

    [Fact]
    public async Task AWithdraw_WritesRejectedNeverDispatched_AndTheCompletedCommand_SoTheReceiptReadsWithdrawn()
    {
        var row = Held();
        IAttemptLedger ledger = Gate;

        var withdrawn = await ledger.WithdrawAttemptAsync(row.DispatchKey, CancellationToken.None);

        Assert.True(withdrawn.Committed);
        Assert.Equal((DispatchState.Rejected, "never_dispatched"), (withdrawn.Receipt.Terminal?.State, withdrawn.Receipt.Terminal?.Cause));
        Assert.Null(withdrawn.Receipt.Dispatched);
        Assert.True(withdrawn.Receipt.WithdrawCompleted);
        Assert.True(Gate.ReadAttempt(row.DispatchKey)!.IsWithdrawn);
    }

    [Fact]
    public async Task ARetriedWithdraw_WithTheSameCommandId_ReturnsTheOriginalOutcome_AndWritesNoSecondRowOrReceipt()
    {
        var row = Held();
        IAttemptLedger ledger = Gate;

        var first = await ledger.WithdrawAttemptAsync(row.DispatchKey, CancellationToken.None);
        var retried = await ledger.WithdrawAttemptAsync(row.DispatchKey, CancellationToken.None);

        Assert.False(retried.Committed);
        Assert.Equal(first.Receipt.Terminal, retried.Receipt.Terminal);
        Assert.Single(retried.Receipt.Outcomes);
        Assert.True(retried.Receipt.IsWithdrawn);
    }

    /// <summary>The withdraw over the real daemon pipe (the client the shell holds), as T14's notebook ops are proved.</summary>
    [Fact]
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public async Task AWithdraw_RoundTripsOverThePipe_AndTheStoreReadsWithdrawn()
    {
        var row = Held();
        var pipe = "aide-wd-" + Guid.NewGuid().ToString("N");
        var endpoint = new DaemonEndpoint(pipe, new CapabilityRegistry(), _ => _workspace.Store.CoreEpoch);
        DaemonOperations.Register(endpoint, () => _workspace.Store.CoreEpoch);
        WorkspaceOperations.RegisterDispatch(endpoint, new BoundaryDispatcher(_workspace.Store));
        var server = new IpcServer(pipe, endpoint, new IpcServerOptions(StartupGrace: TimeSpan.FromSeconds(45)));
        using var life = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var running = server.RunAsync(life.Token);

        try
        {
            await using var client = await WorkspaceClient.ConnectAsync(pipe, TimeSpan.FromSeconds(20), CancellationToken.None);

            var withdrawn = await client.WithdrawAttemptAsync(row.DispatchKey, CancellationToken.None);
            var retried = await client.WithdrawAttemptAsync(row.DispatchKey, CancellationToken.None);
            var readBack = await client.ReadAttemptAsync(row.DispatchKey, CancellationToken.None);

            Assert.True(withdrawn.Committed);
            Assert.Equal((DispatchState.Rejected, "never_dispatched"), (withdrawn.Receipt.Terminal?.State, withdrawn.Receipt.Terminal?.Cause));
            Assert.True(withdrawn.Receipt.IsWithdrawn);
            Assert.False(retried.Committed);
            Assert.True(readBack!.IsWithdrawn);
            Assert.Null(readBack.Dispatched);
            Assert.True(Gate.ReadAttempt(row.DispatchKey)!.IsWithdrawn);
        }
        finally
        {
            await life.CancelAsync();
            try { await running; } catch (OperationCanceledException) { }
        }
    }

    [Fact]
    public void TheSameTerminal_WithNoWithdrawCommand_IsNotWithdrawn()
    {
        var row = Held();

        Gate.AppendAttemptOutcome(new OutcomeRow(row.DispatchKey, DispatchState.Rejected, Cause: "never_dispatched"));
        var receipt = Gate.ReadAttempt(row.DispatchKey)!;

        Assert.Equal(DispatchState.Rejected, receipt.Terminal?.State);
        Assert.False(receipt.WithdrawCompleted);
        Assert.False(receipt.IsWithdrawn);
    }

    [Fact]
    public async Task AWithdraw_OfADispatchedAttempt_CommitsNothing()
    {
        var row = Held();
        Gate.AppendAttemptOutcome(new OutcomeRow(row.DispatchKey, DispatchState.Dispatched));
        IAttemptLedger ledger = Gate;

        var refused = await ledger.WithdrawAttemptAsync(row.DispatchKey, CancellationToken.None);

        Assert.False(refused.Committed);
        Assert.Null(refused.Receipt.Terminal);
        Assert.False(refused.Receipt.WithdrawCompleted);
    }
}
