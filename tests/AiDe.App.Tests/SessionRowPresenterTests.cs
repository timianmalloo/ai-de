using System.Collections.Generic;
using AiDe.App.Workbench;
using AiDe.Core.Presentation;
using AiDe.Core.Watcher;
using Xunit;

namespace AiDe.App.Tests;

/// <summary>
/// The Sessions surface presentation policy (smoke 9-1 #15): a session must read as a stable identity
/// with a distinct liveness, and a telemetry gap the whole list shares must be stated once rather than
/// repeated down every row. Pure — no WPF — so it is verified here headlessly; the rendering that
/// consumes it is thin.
/// </summary>
public sealed class SessionRowPresenterTests
{
    private static WatcherSessionRow Row(
        string agent, string repo, string worktree, LivenessState liveness,
        string harness = "pwsh", string model = "sonnet", int spans = 3, bool disputed = false) =>
        new(
            SessionId: $"{agent}-{worktree}",
            Repository: repo,
            Worktree: worktree,
            Agent: agent,
            Harness: harness,
            Model: model,
            Liveness: LivenessBadge.For(liveness),
            Trust: "Asserted",
            SpanCount: spans,
            Disputed: disputed);

    private static WatcherSessionRow NoTelemetry(string agent, string worktree, LivenessState liveness) =>
        Row(agent, "TheTerrace", worktree, liveness,
            harness: WatcherSessionText.NotRecorded, model: WatcherSessionText.NotRecorded, spans: 0);

    [Theory]
    [InlineData(LivenessState.Alive, "VerifiedBrush", "✓ Alive")]
    [InlineData(LivenessState.Stale, "InferredBrush", "~ Stale")]
    [InlineData(LivenessState.Ended, "UnverifiedBrush", "× Ended")]
    public void Chip_ColoursByLiveness_AndCarriesGlyphPlusText(
        LivenessState state, string expectedBrush, string expectedText)
    {
        var badge = LivenessBadge.For(state);

        Assert.Equal(expectedBrush, SessionRowPresenter.ChipBrushKey(badge));
        Assert.Equal(expectedText, SessionRowPresenter.ChipText(badge)); // colour is never the only signal
    }

    [Fact]
    public void Identity_LeadsWithAgentAndLocation()
    {
        var row = Row("Copilot", "TheTerrace", "workspace", LivenessState.Alive);

        Assert.Equal("Copilot · TheTerrace/workspace", SessionRowPresenter.Identity(row));
    }

    [Fact]
    public void Details_CarriesTheMetadata_AndFlagsADispute()
    {
        var row = Row("Copilot", "TheTerrace", "workspace", LivenessState.Alive, disputed: true);

        var details = SessionRowPresenter.Details(row);

        Assert.Contains("pwsh", details);
        Assert.Contains("sonnet", details);
        Assert.Contains("3 span(s)", details);
        Assert.Contains(WatcherSessionRow.DisputedText, details);
    }

    [Fact]
    public void SharedTelemetryNote_StatesTheGapOnce_WhenEveryRowLacksHarnessAndModel()
    {
        var rows = new List<WatcherSessionRow>
        {
            NoTelemetry("a", "w1", LivenessState.Stale),
            NoTelemetry("b", "w2", LivenessState.Stale),
            NoTelemetry("c", "w3", LivenessState.Ended),
        };

        var note = SessionRowPresenter.SharedTelemetryNote(rows);

        Assert.NotNull(note);
        Assert.Contains("3 sessions", note);
        Assert.Contains("harness", note);
    }

    [Fact]
    public void SharedTelemetryNote_IsNull_WhenAnyRowHasTelemetry()
    {
        var rows = new List<WatcherSessionRow>
        {
            NoTelemetry("a", "w1", LivenessState.Stale),
            Row("b", "TheTerrace", "w2", LivenessState.Alive), // this one has harness+model
        };

        Assert.Null(SessionRowPresenter.SharedTelemetryNote(rows));
    }

    [Fact]
    public void SharedTelemetryNote_IsNull_ForASingleRow()
        => Assert.Null(SessionRowPresenter.SharedTelemetryNote(
            new List<WatcherSessionRow> { NoTelemetry("a", "w1", LivenessState.Stale) }));

    [Fact]
    public void Partition_LeadsWithLive_AndSeparatesTheEndedHistory()
    {
        // The graveyard problem (smoke video 2026-09-02): a long-running workspace piles up ended
        // terminals that bury the one collaborating now. Live (Alive+Stale) must come out separately
        // from Ended so the surface can lead with it and collapse the rest.
        var rows = new List<WatcherSessionRow>
        {
            NoTelemetry("t1", "w1", LivenessState.Ended),
            Row("Claude Code", "TheTerrace", "main", LivenessState.Alive),
            NoTelemetry("t2", "w2", LivenessState.Ended),
            NoTelemetry("t3", "w3", LivenessState.Stale),
            NoTelemetry("t4", "w4", LivenessState.Ended),
        };

        var (live, ended) = SessionRowPresenter.Partition(rows);

        Assert.Equal(2, live.Count);   // the Alive + the Stale
        Assert.Equal(3, ended.Count);  // the three Ended, collapsed
        Assert.Equal("Alive", live[0].Liveness.Text);   // Alive leads
        Assert.Equal("Stale", live[1].Liveness.Text);   // then Stale
        Assert.All(ended, r => Assert.Equal("Ended", r.Liveness.Text));
    }

    [Fact]
    public void Partition_WhenEveryoneHasEnded_LeavesLiveEmpty()
    {
        var rows = new List<WatcherSessionRow>
        {
            NoTelemetry("t1", "w1", LivenessState.Ended),
            NoTelemetry("t2", "w2", LivenessState.Ended),
        };

        var (live, ended) = SessionRowPresenter.Partition(rows);

        Assert.Empty(live);
        Assert.Equal(2, ended.Count);
    }

    [Fact]
    public void EndedHeader_CountsTheCollapsedHistory()
        => Assert.Equal("14 ended session(s)", SessionRowPresenter.EndedHeader(14));
}
