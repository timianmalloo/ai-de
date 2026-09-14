using AiDe.Core.Understanding;

namespace AiDe.Core.Tests.Understanding;

public sealed class AtlasReadBudgetTests
{
    [Fact]
    public void FourScopesRemainChargedUntilDisposed()
    {
        var budget = new AtlasReadBudget();
        var scopes = Enumerable.Range(0, 4).Select(_ => budget.ReserveScope()).ToArray();
        try
        {
            Assert.Equal(4, budget.Read().Scopes);
            Assert.Equal(16 * 1024 * 1024, budget.Read().Retained);
            Assert.Throws<AtlasReadException>(() => budget.ReserveScope());
            scopes[0].Dispose();
            scopes[0].Dispose();
            using var replacement = budget.ReserveScope();
            Assert.Equal(4, budget.Read().Scopes);
        }
        finally
        {
            foreach (var scope in scopes) scope.Dispose();
        }
        Assert.Equal(0, budget.Read().Scopes);
    }

    [Fact]
    public async Task FifoPendingCancellationRemovesOnlyItsTicket()
    {
        var budget = new AtlasReadBudget();
        var active = new List<AtlasReadBudget.Reservation>();
        for (var i = 0; i < 4; i++) active.Add(await budget.EnterAsync(CancellationToken.None));
        using var canceled = new CancellationTokenSource();
        var first = budget.EnterAsync(canceled.Token).AsTask();
        var next = budget.EnterAsync(CancellationToken.None).AsTask();
        canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        Assert.Equal(1, budget.Read().Pending);
        Assert.False(next.IsCompleted);
        active[0].Dispose();
        using var promoted = await next;
        Assert.Equal(4, budget.Read().Active);
        foreach (var reservation in active) reservation.Dispose();
    }

    [Fact]
    public async Task PendingAndOwnedBuffersHaveHardCeilings()
    {
        var budget = new AtlasReadBudget();
        var active = new List<AtlasReadBudget.Reservation>();
        using var cancellation = new CancellationTokenSource();
        var pending = new List<Task<AtlasReadBudget.Reservation>>();
        try
        {
            for (var i = 0; i < 4; i++) active.Add(await budget.EnterAsync(cancellation.Token));
            for (var i = 0; i < 16; i++) pending.Add(budget.EnterAsync(cancellation.Token).AsTask());
            Assert.Throws<AtlasReadException>(() => budget.EnterAsync(cancellation.Token));
            Assert.Equal(16, budget.Read().Pending);
            Assert.True(budget.Read().Owned <= AtlasReadBudget.MaxOwnedBytes);
        }
        finally
        {
            cancellation.Cancel();
            foreach (var task in pending)
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await task);
            foreach (var reservation in active) reservation.Dispose();
        }
        Assert.Equal((0, 0, 0, 0L, 0L), budget.Read());
    }
}
