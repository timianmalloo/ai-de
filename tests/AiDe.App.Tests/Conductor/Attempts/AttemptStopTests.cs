using System.Text.Json;
using AiDe.App.Conductor;
using AiDe.Core.Facts;

namespace AiDe.App.Tests.Conductor.Attempts;

/// <summary>
/// Stop (ADR-0041 §4; design, "Stop"): <c>StopRequested</c> with the relay's high-water seq →
/// <c>session/cancel</c> → outstanding permissions answered <c>cancelled</c> (<c>PermissionCancelledByStop</c>) →
/// a bounded wait. Only the engine's own <c>cancelled</c> reply after the mark is <c>Interrupted</c>.
/// </summary>
public sealed class AttemptStopTests
{
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(60);

    [Fact]
    public async Task Stop_SendsSessionCancel_AndTheEnginesCancelledReplyIsInterrupted()
    {
        using var f = new AttemptHostFixture(PromptBehaviour.HonourCancel);
        using var host = await f.OpenAsync();

        var start = await host.DispatchAsync(f.Dispatch(), CancellationToken.None);
        await f.PromptsArrivedAsync();
        var stopped = await host.StopAsync(start.Receipt.Attempt.DispatchKey, CancellationToken.None).WaitAsync(Bound);

        Assert.Equal(1, f.CancelsReceived);
        Assert.Equal(DispatchState.Interrupted, stopped.Terminal?.State);

        // StopRequested carries the high-water seq it was taken at, and precedes the terminal.
        var requested = Assert.Single(stopped.Outcomes, o => o.State == DispatchState.StopRequested);
        Assert.True(JsonDocument.Parse(requested.Detail!).RootElement.GetProperty("high_water_seq").GetInt64() > 0);
        Assert.True(requested.IngressSeq < stopped.Terminal!.IngressSeq);
        Assert.Equal(stopped.Terminal, (await start.Completion.WaitAsync(Bound)).Terminal);
    }

    [Fact]
    public async Task Stop_ThatIsNeverAcknowledged_IsDeliveryUnknownStopNotAcknowledged_AtTheDeadline()
    {
        using var f = new AttemptHostFixture(PromptBehaviour.IgnoreCancel);
        using var host = await f.OpenAsync(f.Options with { StopDeadline = TimeSpan.FromMilliseconds(500) });

        var start = await host.DispatchAsync(f.Dispatch(), CancellationToken.None);
        await f.PromptsArrivedAsync();
        var stopped = await host.StopAsync(start.Receipt.Attempt.DispatchKey, CancellationToken.None).WaitAsync(Bound);

        Assert.Equal(1, f.CancelsReceived);
        Assert.Equal(DispatchState.DeliveryUnknown, stopped.Terminal?.State);
        Assert.Equal("stop_not_acknowledged", stopped.Terminal?.Cause);
    }

    [Fact]
    public async Task APermissionAskedDuringStop_IsAnsweredCancelled_AndRecordedAsPermissionCancelledByStop()
    {
        using var f = new AttemptHostFixture(PromptBehaviour.PermissionThenCancel);
        using var host = await f.OpenAsync();

        var start = await host.DispatchAsync(f.Dispatch(), CancellationToken.None);
        await f.PromptsArrivedAsync();
        var stopped = await host.StopAsync(start.Receipt.Attempt.DispatchKey, CancellationToken.None).WaitAsync(Bound);

        var answer = Assert.Single(f.TraceLines("<"), l => l.Contains("\"id\":1000", StringComparison.Ordinal));
        Assert.Contains("\"outcome\":\"cancelled\"", answer, StringComparison.Ordinal);
        var cancelled = Assert.Single(stopped.Outcomes, o => o.State == DispatchState.PermissionCancelledByStop);
        Assert.True(cancelled.IngressSeq < stopped.Terminal!.IngressSeq);
        Assert.Equal(DispatchState.Interrupted, stopped.Terminal.State);
    }

    [Fact]
    public async Task ATokenCancelAlone_TheHostsOwnTeardown_IsNeverInterrupted()
    {
        // The engine WOULD acknowledge a session/cancel — but teardown sends none, so nothing acknowledges anything.
        using var f = new AttemptHostFixture(PromptBehaviour.HonourCancel);
        var host = await f.OpenAsync();

        var start = await host.DispatchAsync(f.Dispatch(), CancellationToken.None);
        await f.PromptsArrivedAsync();
        host.Dispose();
        var receipt = await start.Completion.WaitAsync(Bound);

        Assert.Equal(0, f.CancelsReceived);
        Assert.Equal(DispatchState.DeliveryUnknown, receipt.Terminal?.State);
        Assert.Equal("relay_lost", receipt.Terminal?.Cause);
        Assert.DoesNotContain(receipt.Outcomes, o => o.State == DispatchState.Interrupted);
    }
}
