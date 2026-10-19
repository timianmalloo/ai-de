using System.IO;
using AiDe.App.Conductor;
using AiDe.Core.AgentPlane;
using AiDe.Core.Facts;
using AiDe.Core.Health;

namespace AiDe.App.Tests.Conductor.Attempts;

/// <summary>
/// The run log, the relay's fan-out and <see cref="IAttemptHost.SubscribeAsync"/>, end to end through the product's
/// composition root (ADR-0041 §3; design, "Run log, relay, redaction").
/// </summary>
public sealed class AttemptRunLogTests
{
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(60);

    [Fact]
    public async Task ARun_WritesItsRunLog_AndASubscriberSeesEveryEventOnceInOrder()
    {
        using var f = new AttemptHostFixture(released: false);
        using var host = await f.OpenAsync();

        var start = await host.DispatchAsync(f.Dispatch(), CancellationToken.None);
        await f.PromptsArrivedAsync();
        var live = await host.SubscribeAsync(start.Receipt.Attempt.DispatchKey, CancellationToken.None);
        var seen = new List<RunEvent>();
        var reading = Task.Run(async () =>
        {
            await foreach (var e in live.ReadAllAsync(CancellationToken.None))
            {
                seen.Add(e);
            }
        });

        f.Release();
        await start.Completion.WaitAsync(Bound);
        await reading.WaitAsync(Bound);

        var logged = RunLogStore.Read(RunLogStore.PathFor(f.WorkspaceRoot, "s-1", start.Receipt.Attempt.DispatchKey));
        var framesTheEngineSent = f.TraceLines(">").Count();

        Assert.Equal(framesTheEngineSent, logged.Count);
        Assert.Equal(Enumerable.Range(1, logged.Count).Select(i => (long)i), logged.Select(e => e.Seq));
        Assert.Equal(logged.Select(e => e.Seq), seen.Select(e => e.Seq));
        Assert.All(logged, e => Assert.Equal(start.Receipt.Attempt.DispatchKey, e.RunId));

        // A late subscriber, after the run ended, catches up from the run log alone.
        var late = await host.SubscribeAsync(start.Receipt.Attempt.DispatchKey, CancellationToken.None);
        var caughtUp = new List<long>();
        await foreach (var e in late.ReadAllAsync(CancellationToken.None).WithCancellation(CancellationToken.None))
        {
            caughtUp.Add(e.Seq);
        }

        Assert.Equal(logged.Select(e => e.Seq), caughtUp);
    }

    [Fact]
    public async Task AnUnwritableRunLogLine_IsNotPublished_AndTheAttemptIsDeliveryUnknownRunLogUnwritable_WithAnIncident()
    {
        using var f = new AttemptHostFixture();
        var incidents = new HealthIncidentSidecar(Path.Combine(f.WorkspaceRoot, "health-incidents.jsonl"));
        var options = f.Options with
        {
            Incidents = incidents,
            // The disk refuses the first line after the Dispatched row: the prompt's own chunk.
            RunLogs = (session, key) => new RunLogStore(new RefusingStream("agent_message_chunk"), RunLogStore.PathFor(f.WorkspaceRoot, session, key)),
        };
        using var host = await f.OpenAsync(options);

        var start = await host.DispatchAsync(f.Dispatch(), CancellationToken.None);
        var receipt = await start.Completion.WaitAsync(Bound);

        Assert.NotNull(receipt.Dispatched);
        Assert.Equal(DispatchState.DeliveryUnknown, receipt.Terminal?.State);
        Assert.Equal("run_log_unwritable", receipt.Terminal?.Cause);
        Assert.Contains(incidents.Unacknowledged(), i => i.ScopeId == start.Receipt.Attempt.DispatchKey);
    }

    [Fact]
    public void TheRelay_WritesTheSameInstanceToEverySubscriber_AndTracksTheHighWaterSeq()
    {
        using var relay = new RunEventRelay();
        var lane = relay.Reader;
        var fold = relay.Subscribe();
        var evaluation = relay.Subscribe();

        var first = Observed(1);
        var second = Observed(2);
        relay.Publish(first);
        relay.Publish(second);

        foreach (var reader in new[] { lane, fold, evaluation })
        {
            Assert.True(reader.TryRead(out var a));
            Assert.True(reader.TryRead(out var b));
            Assert.Same(first, a);
            Assert.Same(second, b);
        }

        Assert.Equal(2, relay.HighWaterSeq);
    }

    private static ObservedRunEvent Observed(long seq) =>
        new(new RunEvent("run-1", "lane-1", null, seq, DateTimeOffset.UnixEpoch, "agent.msg", null, [], []), null, null);

    /// <summary>A disk that accepts lines until one contains <paramref name="refuseOn"/>, then refuses every write.</summary>
    private sealed class RefusingStream(string refuseOn) : MemoryStream
    {
        private bool _refusing;

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            _refusing |= System.Text.Encoding.UTF8.GetString(buffer).Contains(refuseOn, StringComparison.Ordinal);
            if (_refusing)
            {
                throw new IOException("simulated: the disk is full");
            }

            base.Write(buffer);
        }
    }
}
