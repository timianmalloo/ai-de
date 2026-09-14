using AiDe.Core.Understanding;

namespace AiDe.Core.Tests.Understanding;

public sealed class AtlasReadBudgetTests
{
    [Fact]
    public void FourScopesRemainChargedUntilDisposed()
    {
        var budget = new AtlasReadBudget();
        var scopes = new List<AtlasReadBudget.Reservation>();
        AtlasReadException? firstRefusal = null;
        AtlasReadException? secondRefusal = null;
        (int Scopes, int Active, int Pending, long Owned, long Retained) held = default;
        (int Scopes, int Active, int Pending, long Owned, long Retained) replaced = default;
        try
        {
            for (var i = 0; i < 4; i++) scopes.Add(budget.ReserveScope());
            held = budget.Read();
            firstRefusal = Assert.Throws<AtlasReadException>(() => budget.ReserveScope());
            scopes[0].Dispose();
            scopes.RemoveAt(0);
            scopes.Add(budget.ReserveScope());
            replaced = budget.Read();
            secondRefusal = Assert.Throws<AtlasReadException>(() => budget.ReserveScope());
        }
        finally
        {
            foreach (var scope in scopes) scope.Dispose();
        }

        Assert.Equal("Atlas.Busy", firstRefusal!.Code);
        Assert.Equal("Atlas.Busy", secondRefusal!.Code);
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
        var active = new List<AtlasReadBudget.Reservation>();
        using var canceled = new CancellationTokenSource();
        AtlasReadBudget.Reservation? promoted = null;
        Exception? firstException = null;
        (int Scopes, int Active, int Pending, long Owned, long Retained) canceledPending = default;
        (int Scopes, int Active, int Pending, long Owned, long Retained) afterPromotion = default;
        try
        {
            for (var i = 0; i < 4; i++) active.Add(await budget.EnterAsync(CancellationToken.None));
            var first = budget.EnterAsync(canceled.Token).AsTask();
            var next = budget.EnterAsync(CancellationToken.None).AsTask();
            canceled.Cancel();
            try { await first; }
            catch (Exception ex) { firstException = ex; }
            canceledPending = budget.Read();
            active[0].Dispose();
            active.RemoveAt(0);
            promoted = await next;
            afterPromotion = budget.Read();
        }
        finally
        {
            promoted?.Dispose();
            foreach (var reservation in active) reservation.Dispose();
        }

        Assert.IsAssignableFrom<OperationCanceledException>(firstException);
        Assert.Equal(1, canceledPending.Pending);
        Assert.Equal(4, afterPromotion.Active);
        Assert.Equal((0, 0, 0, 0L, 0L), budget.Read());
    }

    [Fact]
    public async Task PendingAndOwnedBuffersHaveExactQueueLedgerChargesAndDrain()
    {
        var budget = new AtlasReadBudget();
        var active = new List<AtlasReadBudget.Reservation>();
        using var cancellation = new CancellationTokenSource();
        var pending = new List<Task<AtlasReadBudget.Reservation>>();
        PendingDrain? drain = null;
        AtlasReadException? overCapacity = null;
        (int Scopes, int Active, int Pending, long Owned, long Retained) held = default;
        try
        {
            for (var i = 0; i < 4; i++) active.Add(await budget.EnterAsync(cancellation.Token));
            for (var i = 0; i < 16; i++) pending.Add(budget.EnterAsync(cancellation.Token).AsTask());
            held = budget.Read();
            overCapacity = Assert.Throws<AtlasReadException>(() => budget.EnterAsync(cancellation.Token));
        }
        finally
        {
            cancellation.Cancel();
            foreach (var reservation in active) reservation.Dispose();
            drain = await DrainPendingAsync(pending);
        }

        Assert.Equal("Atlas.Busy", overCapacity!.Code);
        Assert.Equal(0, held.Scopes);
        Assert.Equal(4, held.Active);
        Assert.Equal(16, held.Pending);
        Assert.Equal(4 * AtlasReadBudget.OperationBytes, held.Owned);
        Assert.Equal(0, held.Retained);
        Assert.Empty(drain!.Successes);
        Assert.Equal(16, drain.Exceptions.Count);
        Assert.All(drain.Exceptions, exception => Assert.IsAssignableFrom<OperationCanceledException>(exception));
        Assert.Equal((0, 0, 0, 0L, 0L), budget.Read());
    }

    [Fact]
    public async Task PendingDrainControl_DisposesUnexpectedSuccessAndRetainsFaultObject()
    {
        var budget = new AtlasReadBudget();
        var fault = new InvalidOperationException("synthetic pending fault");

        var drain = await DrainPendingAsync(
            [budget.EnterAsync(CancellationToken.None).AsTask(), Task.FromException<AtlasReadBudget.Reservation>(fault)]);

        Assert.Single(drain.Successes);
        Assert.Same(fault, Assert.Single(drain.Exceptions));
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
                successes.Add(await task);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        foreach (var success in successes)
            success.Dispose();
        return new PendingDrain(successes, exceptions);
    }
}
