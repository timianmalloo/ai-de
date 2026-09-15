using AiDe.Core.Understanding;
using Xunit.Abstractions;

namespace AiDe.Core.Tests.Understanding;

public sealed class AtlasReadBudgetTests(ITestOutputHelper output)
{
    [Fact]
    public async Task FiveRealAdmissionsDrainWithoutCancellationForProgress()
    {
        var budget = new AtlasReadBudget();
        using var cancellation = new CancellationTokenSource();
        var pending = new List<Task<AtlasReadBudget.Reservation>>();
        Task<PendingDrain>? draining = null;
        PendingDrain? result = null;
        Exception? progressFailure = null;
        (int Scopes, int Active, int Pending, long Owned, long Retained) queued = default;
        try
        {
            for (var i = 0; i < 5; i++)
                pending.Add(budget.EnterAsync(cancellation.Token).AsTask());
            queued = budget.Read();
            draining = DrainPendingAsync(pending);
            await draining.WaitAsync(TimeSpan.FromSeconds(2));
        }
        catch (Exception exception)
        {
            progressFailure = exception;
        }
        finally
        {
            if (draining is null || !draining.IsCompleted)
                cancellation.Cancel();
            result = await (draining ?? DrainPendingAsync(pending));
        }

        output.WriteLine(
            $"five-admissions queued={queued}; canceled={cancellation.IsCancellationRequested}; " +
            $"successes={result.Successes.Count}; faults={result.Exceptions.Count}; " +
            $"progressFailure={progressFailure}; final={budget.Read()}");
        Assert.Equal((0, 0, 0, 0L, 0L), budget.Read());
        Assert.Equal((0, 4, 1, 4 * AtlasReadBudget.OperationBytes, 0L), queued);
        Assert.Null(progressFailure);
        Assert.False(cancellation.IsCancellationRequested);
        Assert.Equal(5, result.Successes.Count);
        Assert.Empty(result.Exceptions);
    }

    [Fact]
    public void FourScopesRemainChargedUntilDisposed()
    {
        var budget = new AtlasReadBudget();
        var scopes = new List<AtlasReadBudget.Reservation>();
        Exception? firstRefusal = null;
        Exception? secondRefusal = null;
        (int Scopes, int Active, int Pending, long Owned, long Retained) held = default;
        (int Scopes, int Active, int Pending, long Owned, long Retained) replaced = default;
        try
        {
            for (var i = 0; i < 4; i++) scopes.Add(budget.ReserveScope());
            held = budget.Read();
            firstRefusal = Record.Exception(() => scopes.Add(budget.ReserveScope()));
            scopes[0].Dispose();
            scopes.RemoveAt(0);
            scopes.Add(budget.ReserveScope());
            replaced = budget.Read();
            secondRefusal = Record.Exception(() => scopes.Add(budget.ReserveScope()));
        }
        finally
        {
            foreach (var scope in scopes) scope.Dispose();
        }

        Assert.Equal("Atlas.Busy", Assert.IsType<AtlasReadException>(firstRefusal).Code);
        Assert.Equal("Atlas.Busy", Assert.IsType<AtlasReadException>(secondRefusal).Code);
        Assert.Equal(4, held.Scopes);
        Assert.Equal(4 * AtlasReadBudget.ConnectionBytes, held.Owned);
        Assert.Equal(4 * AtlasReadBudget.ScopeRetainedBytes, held.Retained);
        Assert.Equal(4, replaced.Scopes);
        Assert.Equal((0, 0, 0, 0L, 0L), budget.Read());
    }

    [Fact]
    public async Task FifoPendingCancellationRemovesOnlyItsTicket()
    {
        var budget = new AtlasReadBudget();
        var owned = new List<Task<AtlasReadBudget.Reservation>>();
        using var canceled = new CancellationTokenSource();
        using var cleanup = new CancellationTokenSource();
        AtlasReadBudget.Reservation? unexpectedFirst = null;
        AtlasReadBudget.Reservation? promoted = null;
        PendingDrain? drain = null;
        Exception? firstException = null;
        (int Scopes, int Active, int Pending, long Owned, long Retained) canceledPending = default;
        (int Scopes, int Active, int Pending, long Owned, long Retained) afterPromotion = default;
        try
        {
            for (var i = 0; i < 4; i++)
                owned.Add(budget.EnterAsync(cleanup.Token).AsTask());
            owned.Add(budget.EnterAsync(canceled.Token).AsTask());
            var first = owned[^1];
            owned.Add(budget.EnterAsync(cleanup.Token).AsTask());
            var next = owned[^1];
            canceled.Cancel();
            try { unexpectedFirst = await first.WaitAsync(TimeSpan.FromSeconds(2)); }
            catch (Exception ex) { firstException = ex; }
            canceledPending = budget.Read();
            (await owned[0]).Dispose();
            owned.RemoveAt(0);
            promoted = await next.WaitAsync(TimeSpan.FromSeconds(2));
            afterPromotion = budget.Read();
        }
        finally
        {
            canceled.Cancel();
            cleanup.Cancel();
            drain = await DrainPendingAsync(owned);
        }

        output.WriteLine(
            $"fifo canceledPending={canceledPending}; afterPromotion={afterPromotion}; " +
            $"drainedSuccesses={drain.Successes.Count}; drainedFaults={drain.Exceptions.Count}; " +
            $"final={budget.Read()}");
        Assert.Null(unexpectedFirst);
        Assert.NotNull(promoted);
        Assert.IsAssignableFrom<OperationCanceledException>(firstException);
        Assert.Equal((0, 4, 1, 4 * AtlasReadBudget.OperationBytes, 0L), canceledPending);
        Assert.Equal((0, 4, 0, 4 * AtlasReadBudget.OperationBytes, 0L), afterPromotion);
        Assert.Equal(4, drain.Successes.Count);
        Assert.IsAssignableFrom<OperationCanceledException>(Assert.Single(drain.Exceptions));
        Assert.Equal((0, 0, 0, 0L, 0L), budget.Read());
    }

    [Fact]
    public async Task PendingAndOwnedBuffersHaveExactQueueLedgerChargesAndDrain()
    {
        var budget = new AtlasReadBudget();
        using var cancellation = new CancellationTokenSource();
        var owned = new List<Task<AtlasReadBudget.Reservation>>();
        PendingDrain? drain = null;
        Exception? overCapacity = null;
        (int Scopes, int Active, int Pending, long Owned, long Retained) held = default;
        try
        {
            for (var i = 0; i < 4; i++) owned.Add(budget.EnterAsync(cancellation.Token).AsTask());
            for (var i = 0; i < 16; i++) owned.Add(budget.EnterAsync(cancellation.Token).AsTask());
            held = budget.Read();
            overCapacity = Record.Exception(() => owned.Add(budget.EnterAsync(cancellation.Token).AsTask()));
        }
        finally
        {
            cancellation.Cancel();
            drain = await DrainPendingAsync(owned);
        }

        output.WriteLine(
            $"capacity held={held}; drainedSuccesses={drain.Successes.Count}; " +
            $"drainedFaults={drain.Exceptions.Count}; final={budget.Read()}");
        Assert.Equal("Atlas.Busy", Assert.IsType<AtlasReadException>(overCapacity).Code);
        Assert.Equal(0, held.Scopes);
        Assert.Equal(4, held.Active);
        Assert.Equal(16, held.Pending);
        Assert.Equal(4 * AtlasReadBudget.OperationBytes, held.Owned);
        Assert.Equal(0, held.Retained);
        Assert.Equal(4, drain.Successes.Count);
        Assert.Equal(16, drain.Exceptions.Count);
        Assert.All(drain.Exceptions, exception => Assert.IsAssignableFrom<OperationCanceledException>(exception));
        Assert.Equal((0, 0, 0, 0L, 0L), budget.Read());
    }

    [Fact]
    public async Task PendingDrainControl_DisposesUnexpectedSuccessAndRetainsFaultObject()
    {
        var budget = new AtlasReadBudget();
        var fault = new InvalidOperationException("synthetic pending fault");
        var owned = new List<Task<AtlasReadBudget.Reservation>>();
        using var cancellation = new CancellationTokenSource();
        Task<PendingDrain>? draining = null;
        PendingDrain? drain = null;
        Exception? progressFailure = null;
        (int Scopes, int Active, int Pending, long Owned, long Retained) queued = default;
        try
        {
            for (var i = 0; i < 5; i++)
                owned.Add(budget.EnterAsync(cancellation.Token).AsTask());
            owned.Add(Task.FromException<AtlasReadBudget.Reservation>(fault));
            queued = budget.Read();
            draining = DrainPendingAsync(owned);
            await draining.WaitAsync(TimeSpan.FromSeconds(2));
        }
        catch (Exception exception)
        {
            progressFailure = exception;
        }
        finally
        {
            if (draining is null || !draining.IsCompleted)
                cancellation.Cancel();
            drain = await (draining ?? DrainPendingAsync(owned));
        }

        output.WriteLine(
            $"pending-success-fault queued={queued}; canceled={cancellation.IsCancellationRequested}; " +
            $"successes={drain.Successes.Count}; faults={drain.Exceptions.Count}; " +
            $"sameFault={ReferenceEquals(fault, drain.Exceptions.LastOrDefault())}; final={budget.Read()}");
        Assert.Equal((0, 0, 0, 0L, 0L), budget.Read());
        Assert.Equal((0, 4, 1, 4 * AtlasReadBudget.OperationBytes, 0L), queued);
        Assert.Null(progressFailure);
        Assert.False(cancellation.IsCancellationRequested);
        Assert.Equal(5, drain.Successes.Count);
        Assert.Same(fault, Assert.Single(drain.Exceptions));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(3, false)]
    [InlineData(3, true)]
    public async Task PartialAcquisitionAndSimultaneousFaultsRetainExceptionObjects(
        int acquired, bool injectSecondary)
    {
        var budget = new AtlasReadBudget();
        var primary = new InvalidOperationException("injected acquisition failure");
        var secondary = new ArgumentException("injected concurrent pending failure");
        var owned = new List<Task<AtlasReadBudget.Reservation>>();
        var scopes = new List<AtlasReadBudget.Reservation>();
        var completion = new TaskCompletionSource<AtlasReadBudget.Reservation>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        Exception? observedPrimary = null;
        PendingDrain? drain = null;
        (int Scopes, int Active, int Pending, long Owned, long Retained) beforeDrain = default;
        (int Scopes, int Active, int Pending, long Owned, long Retained) scopeResidual = default;
        try
        {
            scopes.Add(budget.ReserveScope());
            try
            {
                for (var i = 0; i < acquired; i++)
                    owned.Add(budget.EnterAsync(CancellationToken.None).AsTask());
                if (injectSecondary)
                {
                    owned.Add(completion.Task);
                    completion.SetException(secondary);
                }
                beforeDrain = budget.Read();
                throw primary;
            }
            catch (Exception exception)
            {
                observedPrimary = exception;
            }
            finally
            {
                drain = await DrainPendingAsync(owned);
                scopeResidual = budget.Read();
            }
        }
        finally
        {
            foreach (var scope in scopes) scope.Dispose();
        }

        output.WriteLine(
            $"partial acquired={acquired}; secondary={injectSecondary}; beforeDrain={beforeDrain}; " +
            $"scopeResidual={scopeResidual}; successes={drain!.Successes.Count}; faults={drain.Exceptions.Count}; " +
            $"samePrimary={ReferenceEquals(primary, observedPrimary)}; " +
            $"sameSecondary={ReferenceEquals(secondary, drain.Exceptions.SingleOrDefault())}; final={budget.Read()}");
        Assert.Same(primary, observedPrimary);
        Assert.Equal((1, acquired, 0,
            AtlasReadBudget.ConnectionBytes + acquired * AtlasReadBudget.OperationBytes,
            AtlasReadBudget.ScopeRetainedBytes), beforeDrain);
        Assert.Equal((1, 0, 0, AtlasReadBudget.ConnectionBytes, AtlasReadBudget.ScopeRetainedBytes), scopeResidual);
        Assert.Equal(acquired, drain.Successes.Count);
        if (injectSecondary)
            Assert.Same(secondary, Assert.Single(drain.Exceptions));
        else
            Assert.Empty(drain.Exceptions);
        Assert.Equal((0, 0, 0, 0L, 0L), budget.Read());
    }

    [Fact]
    public async Task ExpectedRefusalCaptureOwnsUnexpectedScopeAndOperationSuccesses()
    {
        var budget = new AtlasReadBudget();
        var scopes = new List<AtlasReadBudget.Reservation>();
        var owned = new List<Task<AtlasReadBudget.Reservation>>();
        Exception? scopeRefusal = null;
        Exception? operationRefusal = null;
        PendingDrain? drain = null;
        (int Scopes, int Active, int Pending, long Owned, long Retained) captured = default;
        try
        {
            scopeRefusal = Record.Exception(() => scopes.Add(budget.ReserveScope()));
            operationRefusal = Record.Exception(() => owned.Add(budget.EnterAsync(CancellationToken.None).AsTask()));
            captured = budget.Read();
        }
        finally
        {
            drain = await DrainPendingAsync(owned);
            foreach (var scope in scopes) scope.Dispose();
        }

        output.WriteLine(
            $"unexpected-refusal-success captured={captured}; successes={drain.Successes.Count}; " +
            $"faults={drain.Exceptions.Count}; final={budget.Read()}");
        Assert.Null(scopeRefusal);
        Assert.Null(operationRefusal);
        Assert.Equal((1, 1, 0, AtlasReadBudget.ConnectionBytes + AtlasReadBudget.OperationBytes,
            AtlasReadBudget.ScopeRetainedBytes), captured);
        Assert.Single(scopes);
        Assert.Single(drain.Successes);
        Assert.Empty(drain.Exceptions);
        Assert.Equal((0, 0, 0, 0L, 0L), budget.Read());
    }

    private sealed record PendingDrain(
        IReadOnlyList<AtlasReadBudget.Reservation> Successes,
        IReadOnlyList<Exception> Exceptions);

    private static async Task<PendingDrain> DrainPendingAsync(IEnumerable<Task<AtlasReadBudget.Reservation>> pending)
    {
        var successes = new List<AtlasReadBudget.Reservation>();
        var exceptions = new List<Exception>();
        foreach (var task in pending)
        {
            try
            {
                var success = await task;
                successes.Add(success);
                success.Dispose();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }
        return new PendingDrain(successes, exceptions);
    }
}
