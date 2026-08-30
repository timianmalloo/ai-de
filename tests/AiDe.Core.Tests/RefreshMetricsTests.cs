using AiDe.Core.Ipc;

namespace AiDe.Core.Tests;

/// <summary>
/// What re-indexing costs, measured on the normal path.
/// </summary>
/// <remarks>
/// <para><b>These exist because a design decision is blocked on the number.</b>
/// <c>docs/notes/note-20260830-sub-scope-incrementality.md</c> weighs four ways to make re-indexing
/// incremental below the scope and refuses to choose, because nobody has measured whether a refresh
/// is an occasional cost a user asks for or something they wait on constantly. A refresh had a span
/// but its STATUS carried no duration, so the one thing a caller could actually read was how many
/// assertions came back.</para>
/// </remarks>
public sealed class RefreshMetricsTests
{
    private static ScopeRefreshService Service(Func<string, string, CancellationToken, Task<int>> refresh) =>
        new(refresh);

    private static async Task<ScopeRefreshStatus> SettledAsync(
        ScopeRefreshService service, string commandId)
    {
        // The work runs detached, so the test waits for it rather than assuming it is done.
        for (var attempt = 0; attempt < 200; attempt++)
        {
            var status = service.Status(commandId);

            if (status is not null && status.State != ScopeRefreshState.Running)
            {
                return status;
            }

            await Task.Delay(10);
        }

        throw new TimeoutException($"'{commandId}' never settled");
    }

    [Fact]
    public async Task ACompletedRefreshReportsHowLongItTook()
    {
        var service = Service(async (_, _, _) =>
        {
            await Task.Delay(40);
            return 7;
        });

        service.Start("c1", "scope", "rev-1");
        var status = await SettledAsync(service, "c1");

        Assert.Equal(ScopeRefreshState.Completed, status.State);
        Assert.Equal(7, status.AssertionCount);
        Assert.True(status.DurationMilliseconds >= 30,
            $"duration was {status.DurationMilliseconds}ms, which cannot be right for a 40ms refresh");
    }

    [Fact]
    public async Task AFailedRefreshIsTimedToo()
    {
        // A run that takes twenty seconds and THEN throws is the one an operator most wants to see.
        // Excluding failures is how a percentile ends up describing only the easy cases.
        var service = Service(async (_, _, _) =>
        {
            await Task.Delay(30);
            throw new InvalidOperationException("the project would not load");
        });

        service.Start("c2", "scope", "rev-1");
        var status = await SettledAsync(service, "c2");

        Assert.Equal(ScopeRefreshState.Failed, status.State);
        Assert.True(status.DurationMilliseconds >= 20,
            $"a failure was recorded as taking {status.DurationMilliseconds}ms");

        var metrics = service.Metrics();
        Assert.Equal(1, metrics.Failed);
        Assert.Equal(0, metrics.Completed);
        Assert.True(metrics.MaxMilliseconds >= 20);
    }

    [Fact]
    public async Task TheSummaryCountsBothOutcomesAndKeepsTheWindow()
    {
        var service = Service((_, _, _) => Task.FromResult(1));

        for (var i = 0; i < 5; i++)
        {
            service.Start($"ok{i}", "scope", "rev-1");
            await SettledAsync(service, $"ok{i}");
        }

        var metrics = service.Metrics();

        Assert.Equal(5, metrics.Completed);
        Assert.Equal(0, metrics.Failed);
        Assert.NotNull(metrics.FirstAt);
        Assert.NotNull(metrics.LastAt);
        Assert.True(metrics.LastAt >= metrics.FirstAt);
    }

    [Fact]
    public void WithNothingMeasuredTheSummaryReportsNothing_NotAPlausibleNumber()
    {
        // Every measurement path degrades to "not recorded", never to a number somebody might
        // believe. A p95 interpolated from an empty list is the shape of a lie.
        var metrics = Service((_, _, _) => Task.FromResult(0)).Metrics();

        Assert.Equal(0, metrics.Completed);
        Assert.Equal(0, metrics.Failed);
        Assert.Equal(0, metrics.P50Milliseconds);
        Assert.Equal(0, metrics.P95Milliseconds);
        Assert.Null(metrics.FirstAt);
        Assert.Null(metrics.LastAt);
    }

    [Fact]
    public async Task ARetryOfTheSameCommandIsNotASecondMeasurement()
    {
        // The idempotency key already prevents a second extraction; it must also prevent a second
        // SAMPLE, or a client that polls by retrying would quietly halve the reported median.
        var runs = 0;

        var service = Service(async (_, _, _) =>
        {
            Interlocked.Increment(ref runs);
            await Task.Delay(20);
            return 1;
        });

        service.Start("same", "scope", "rev-1");
        service.Start("same", "scope", "rev-1");
        service.Start("same", "scope", "rev-1");

        await SettledAsync(service, "same");

        Assert.Equal(1, runs);
        Assert.Equal(1, service.Metrics().Completed);
    }
}
