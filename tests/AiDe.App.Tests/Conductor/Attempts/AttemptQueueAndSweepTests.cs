using AiDe.App.Conductor;
using AiDe.Core.Dispatch;
using AiDe.Core.Facts;
using AiDe.Core.Ipc;
using AiDe.Core.Store.Records;

namespace AiDe.App.Tests.Conductor.Attempts;

/// <summary>
/// The queue and Send-now (design, "Queued launch"; ADR-0041 §4), and the sweep's triggers and emission
/// (ADR-0041 §4 "Triggers"; design, "The sweep").
/// </summary>
public sealed class AttemptQueueAndSweepTests
{
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(60);

    // ---- Queue + Send-now.

    [Fact]
    public async Task AQueuedSuccessor_LaunchesOnlyAfterItsPredecessorsResponseComplete()
    {
        using var f = new AttemptHostFixture(released: false);
        using var host = await f.OpenAsync();

        var first = await host.DispatchAsync(f.Dispatch("env-1"), CancellationToken.None);
        await f.PromptsArrivedAsync();
        var queued = await host.DispatchAsync(f.Dispatch("env-2", queuedAfter: first.Receipt.Attempt.DispatchKey), CancellationToken.None);

        Assert.False(queued.AlreadyAttempted);
        Assert.True(queued.Receipt.IsPending);
        Assert.Equal(first.Receipt.Attempt.DispatchKey, queued.Receipt.Attempt.QueuedAfterDispatchKey);
        await Task.Delay(500);
        Assert.Equal(1, f.PromptsReceived);
        Assert.Empty((await host.SweepAsync("host_restart", CancellationToken.None)).Swept);

        f.Release();
        var predecessor = await first.Completion.WaitAsync(Bound);
        var successor = await queued.Completion.WaitAsync(Bound);

        Assert.Equal(2, f.PromptsReceived);
        Assert.Equal(DispatchState.ResponseComplete, predecessor.Terminal?.State);
        Assert.Equal(DispatchState.ResponseComplete, successor.Terminal?.State);
        Assert.True(successor.Dispatched!.IngressSeq > predecessor.Terminal!.IngressSeq);
    }

    [Fact]
    public async Task AQueuedSuccessor_BehindAPredecessorThatDidNotComplete_IsHeld_AndSendNowLaunchesItOnce()
    {
        using var f = new AttemptHostFixture();
        using var host = await f.OpenAsync();

        var rejected = await host.DispatchAsync(f.Dispatch("env-1", currentProjection: "edited-since"), CancellationToken.None);
        await rejected.Completion.WaitAsync(Bound);
        var queued = await host.DispatchAsync(f.Dispatch("env-2", queuedAfter: rejected.Receipt.Attempt.DispatchKey), CancellationToken.None);
        await Task.Delay(500);

        Assert.Equal(0, f.PromptsReceived);
        Assert.False(queued.Completion.IsCompleted);

        var both = await Task.WhenAll(
            host.SendNowAsync(queued.Receipt.Attempt.DispatchKey, CancellationToken.None),
            host.SendNowAsync(queued.Receipt.Attempt.DispatchKey, CancellationToken.None));
        await Task.WhenAll(both.Select(s => s.Completion)).WaitAsync(Bound);
        var sent = await queued.Completion.WaitAsync(Bound);

        Assert.Single(both, s => !s.AlreadyAttempted);
        Assert.Equal(1, f.PromptsReceived);
        Assert.Equal(DispatchState.ResponseComplete, sent.Terminal?.State);
    }

    // ---- Sweep triggers and the attempt.sweep emission.

    [Fact]
    public async Task TheStartupSweep_EmitsAttemptSweep_EvenAtZero()
    {
        using var f = new AttemptHostFixture();
        using var host = await f.OpenAsync();

        var emitted = Assert.Single(f.Telemetry("attempt.sweep"));
        Assert.Equal(0, emitted.GetProperty("count").GetInt32());
        Assert.Equal("host_startup", emitted.GetProperty("trigger").GetString());
        Assert.False(string.IsNullOrEmpty(emitted.GetProperty("last_sweep_at").GetString()));
    }

    [Fact]
    public async Task TheBackstopTimer_SweepsAndEmits_OnItsInterval()
    {
        using var f = new AttemptHostFixture();
        using var host = await f.OpenAsync(f.Options with { SweepBackstop = TimeSpan.FromMilliseconds(100) });

        var until = DateTime.UtcNow + Bound;
        while (f.Telemetry("attempt.sweep").Count(e => e.GetProperty("trigger").GetString() == "backstop") < 2)
        {
            Assert.True(DateTime.UtcNow < until, "the backstop never swept twice");
            await Task.Delay(50);
        }
    }

    [Fact]
    public async Task AnEngineExit_SweepsAnAbandonedDispatchedAttempt_ToRelayLost()
    {
        using var f = new AttemptHostFixture();
        using var host = await f.OpenAsync();

        // An attempt abandoned after its Dispatched row, by nobody live in this host (e.g. a lost relay).
        IAttemptLedger ledger = new BoundaryDispatcher(f.Store);
        var orphan = f.Dispatch("env-orphan").ToRow();
        await ledger.BeginAttemptAsync(orphan, CancellationToken.None);
        await ledger.AppendAttemptOutcomeAsync(new OutcomeRow(orphan.DispatchKey, DispatchState.Dispatched), CancellationToken.None);

        var start = await host.DispatchAsync(f.Dispatch("env-1"), CancellationToken.None);
        await start.Completion.WaitAsync(Bound);

        var swept = await host.ReadAsync(orphan.DispatchKey, CancellationToken.None);
        Assert.Equal(DispatchState.DeliveryUnknown, swept?.Terminal?.State);
        Assert.Equal("relay_lost", swept?.Terminal?.Cause);
        Assert.Contains(f.Telemetry("attempt.sweep"), e => e.GetProperty("trigger").GetString() == "engine_exit" && e.GetProperty("count").GetInt32() == 1);
    }
}
