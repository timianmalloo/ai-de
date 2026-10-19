using System.IO;
using AiDe.Core.AgentPlane;

namespace AiDe.App.Tests.Conductor.Attempts;

/// <summary>
/// Redaction at ingest, end to end (ADR-0042 §6; design, "Run log, relay, redaction"): the engine sends a frame
/// carrying <see cref="FrameRedaction.TestSentinel"/>; the run log's bytes and every subscriber are clean, and the
/// event carries the redaction row (<c>Offset</c> = the marker's start in the redacted frame, <c>Len</c> = the
/// original span's length).
/// </summary>
public sealed class AttemptRedactionTests
{
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(60);

    [Fact]
    public async Task ASentinelInAFrame_IsStoredRedacted_WithARedactionRow_AndNoSubscriberSeesIt()
    {
        using var f = new AttemptHostFixture(PromptBehaviour.Sentinel, sentinel: FrameRedaction.TestSentinel);
        using var host = await f.OpenAsync();

        var start = await host.DispatchAsync(f.Dispatch(), CancellationToken.None);
        await start.Completion.WaitAsync(Bound);
        var key = start.Receipt.Attempt.DispatchKey;
        var path = RunLogStore.PathFor(f.WorkspaceRoot, "s-1", key);

        var bytes = File.ReadAllText(path);
        Assert.DoesNotContain(FrameRedaction.TestSentinel, bytes, StringComparison.Ordinal);
        Assert.Contains(FrameRedaction.Marker, bytes, StringComparison.Ordinal);

        var logged = RunLogStore.Read(path);
        Assert.All(logged, e => Assert.Equal(FrameRedaction.PatternsVersion, e.ScannedWith));
        var chunk = Assert.Single(logged, e => e.Redactions is { Count: > 0 });
        var redaction = Assert.Single(chunk.Redactions!);
        Assert.Equal("test-sentinel", redaction.PatternId);
        Assert.Equal(FrameRedaction.TestSentinel.Length, redaction.Len);

        // Offset indexes the redacted frame: re-redacting the engine's own line puts the marker exactly there.
        var raw = f.TraceLines(">").Single(l => l.Contains(FrameRedaction.TestSentinel, StringComparison.Ordinal));
        var sent = raw[(raw.IndexOf(" > ", StringComparison.Ordinal) + 3)..];
        var stored = FrameRedaction.Apply(sent, FrameRedaction.PatternsVersion).Frame;
        Assert.Equal(FrameRedaction.Marker, stored.Substring(redaction.Offset, FrameRedaction.Marker.Length));

        // A subscriber reads the same clean events.
        var late = await host.SubscribeAsync(key, CancellationToken.None);
        await foreach (var e in late.ReadAllAsync(CancellationToken.None))
        {
            Assert.DoesNotContain(FrameRedaction.TestSentinel, System.Text.Json.JsonSerializer.Serialize(e), StringComparison.Ordinal);
        }
    }
}
