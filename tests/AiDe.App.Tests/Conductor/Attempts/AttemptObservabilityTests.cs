using System.Diagnostics;
using System.IO;
using System.Text.Json;
using AiDe.App.Conductor;
using AiDe.Core.AgentPlane;
using AiDe.Core.Facts;

namespace AiDe.App.Tests.Conductor.Attempts;

/// <summary>
/// NFR-11 (ADR-0041 §7, measured not modeled) and the <c>fold.duration_ms</c> listener (design, "Fold inputs").
/// </summary>
public sealed class AttemptObservabilityTests
{
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(60);

    /// <summary>
    /// The NFR-11 oracle: every attempt has <c>Dispatched.ts</c> and terminal <c>ts</c> (duration derived), event
    /// count, bytes out, outcome, and <c>usage</c> or Not recorded — each compared with an independent count.
    /// </summary>
    [Fact]
    public async Task EveryAttempt_HasDispatchedAndTerminalTs_EventCount_Bytes_Outcome_AndUsage()
    {
        using var f = new AttemptHostFixture();
        using var host = await f.OpenAsync();

        var start = await host.DispatchAsync(f.Dispatch(), CancellationToken.None);
        await start.Completion.WaitAsync(Bound);
        var key = start.Receipt.Attempt.DispatchKey;
        var measures = await host.MeasureAsync(key, CancellationToken.None);

        var runLog = RunLogStore.PathFor(f.WorkspaceRoot, "s-1", key);
        Assert.NotNull(measures.DispatchedAt);
        Assert.NotNull(measures.TerminalAt);
        Assert.Equal(measures.TerminalAt - measures.DispatchedAt, measures.Duration);
        Assert.True(measures.Duration >= TimeSpan.Zero);
        Assert.Equal(f.TraceLines(">").Count(), measures.Events);
        Assert.Equal(new FileInfo(runLog).Length, measures.Bytes);
        Assert.Equal(nameof(DispatchState.ResponseComplete), measures.Outcome);
        Assert.Equal(4, JsonDocument.Parse(measures.Usage).RootElement.GetProperty("inputTokens").GetInt32());
    }

    [Fact]
    public async Task AnAttemptWithNoUsage_ReadsNotRecorded_NeverZero()
    {
        using var f = new AttemptHostFixture(PromptBehaviour.HonourCancel);
        using var host = await f.OpenAsync();

        var start = await host.DispatchAsync(f.Dispatch(), CancellationToken.None);
        await f.PromptsArrivedAsync();
        await host.StopAsync(start.Receipt.Attempt.DispatchKey, CancellationToken.None).WaitAsync(Bound);
        var measures = await host.MeasureAsync(start.Receipt.Attempt.DispatchKey, CancellationToken.None);

        Assert.Equal(nameof(DispatchState.Interrupted), measures.Outcome);
        Assert.Equal(JsonSerializer.Serialize(GovernedRunHost.NotRecorded), measures.Usage);
    }

    [Fact]
    public async Task AFoldActivity_WritesFoldDurationMs_ToTheRollingLog()
    {
        using var f = new AttemptHostFixture();
        using var host = await f.OpenAsync();
        using var source = new ActivitySource(AttemptHost.FoldActivitySourceName);

        using (source.StartActivity("fold"))
        {
            await Task.Delay(5);
        }

        // The listener is process-global, so another test's fold may land here too: contains, not single.
        Assert.Contains(f.Telemetry("fold.duration_ms"), line => line.GetProperty("value").GetDouble() >= 5);
    }
}
