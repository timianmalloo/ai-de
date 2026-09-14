using System.Text.Json.Nodes;
using AiDe.App.Conductor;
using AiDe.Core.AgentPlane;

namespace AiDe.App.Tests.Conductor;

/// <summary>
/// <c>GovernedRunHost.DrainAsync</c> ends when the prompt has completed and nothing is queued —
/// <b>whichever happens last</b>. Found under Ruling 95's drain: the queued turn's stub answered
/// in one burst, the drain consumed the answer frame's event before the reply's continuation
/// completed the prompt task, and then waited for an event that was never coming. The turn read
/// <i>running</i> forever with every event delivered.
/// </summary>
/// <remarks>
/// <b>Red observed</b>: <c>the drain did not end within 5 s after the prompt completed with an
/// empty queue</c> — the exit condition was evaluated only on the arrival of an event, so a prompt
/// that completed after the last event could never be seen. The same shape is the measured
/// "11 events" vs "12 events" on two identical live turns (docs/proof/send-while-running.md).
/// </remarks>
public sealed class TheDrainEndsWhenThePromptEndsTests
{
    private static RunEvent Event(long seq, string kind) => new(
        "run-1", "lane-1", null, seq, DateTimeOffset.UnixEpoch, kind, null, new JsonObject(), new JsonObject());

    private static ObservedRunEvent Observed(long seq, string kind) =>
        new(Event(seq, kind), DateTimeOffset.UnixEpoch, TimeSpan.FromMilliseconds(1));

    [Fact]
    public async Task ThePromptCompletingAfterTheLastEvent_EndsTheDrain()
    {
        var queue = new AcpEventQueue(capacity: 8);
        var sink = new List<ObservedRunEvent>();
        var prompt = new TaskCompletionSource();

        var drain = GovernedRunHost.DrainAsync(queue, prompt.Task, seams: null, _ => { }, sink.Add, CancellationToken.None);

        await queue.PublishAsync(Observed(1, "agent.msg"));
        await queue.PublishAsync(Observed(2, "acp.result"));

        // Every event is consumed first — the drain is idle on an empty queue with the prompt open.
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (sink.Count < 2 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(10);
        }

        Assert.Equal(2, sink.Count);
        Assert.False(drain.IsCompleted);

        // Then the prompt completes, after the last event.
        prompt.SetResult();

        var finished = await Task.WhenAny(drain, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.True(ReferenceEquals(finished, drain), "the drain did not end within 5 s after the prompt completed with an empty queue");
        var drained = await drain;
        Assert.Equal(2, drained.Events);
        Assert.Equal(["agent.msg", "acp.result"], drained.Kinds);
    }

    /// <summary>The other order is unchanged: an event arriving after the prompt completed is drained, then the drain ends.</summary>
    [Fact]
    public async Task AnEventAfterThePromptCompleted_IsStillDrained_ThenTheDrainEnds()
    {
        var queue = new AcpEventQueue(capacity: 8);
        var sink = new List<ObservedRunEvent>();
        var prompt = new TaskCompletionSource();

        var drain = GovernedRunHost.DrainAsync(queue, prompt.Task, seams: null, _ => { }, sink.Add, CancellationToken.None);
        await queue.PublishAsync(Observed(1, "acp.result"));
        prompt.SetResult();
        await queue.PublishAsync(Observed(2, "acp.session.update.usage_update"));

        var finished = await Task.WhenAny(drain, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.True(ReferenceEquals(finished, drain), "the drain did not end");
        Assert.True((await drain).Events is 1 or 2);
    }

    /// <summary>A faulted prompt (the engine refused it) ends the drain the same way — never a hang on a refusal.</summary>
    [Fact]
    public async Task AFaultedPrompt_EndsTheDrain()
    {
        var queue = new AcpEventQueue(capacity: 8);
        var prompt = new TaskCompletionSource();

        var drain = GovernedRunHost.DrainAsync(queue, prompt.Task, seams: null, _ => { }, null, CancellationToken.None);
        await queue.PublishAsync(Observed(1, "acp.error"));
        await Task.Delay(50);
        prompt.SetException(new AgentPlaneException(AgentPlaneErrorCodes.EngineReturnedError, "refused"));

        var finished = await Task.WhenAny(drain, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.True(ReferenceEquals(finished, drain), "the drain did not end after the prompt faulted");
        Assert.Equal(1, (await drain).Events);
    }
}
