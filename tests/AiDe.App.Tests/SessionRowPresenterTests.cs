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

        Assert.Equal("Copilot · TheTerrace@workspace · Copilot-", SessionRowPresenter.Identity(row));
    }

    /// <summary>
    /// A branch name containing a slash cannot be read as a path.
    /// </summary>
    /// <remarks>
    /// <para>Reported from the running product: the row read
    /// <c>TheTerrace/docs/fix-broken-design-links</c> and the reporter asked why sessions were not at
    /// the repository root. They were — that is a repo and a branch, and the branch's own slash made
    /// the pair look like a directory.</para>
    ///
    /// <para>The old test used the branch name "workspace", which has no slash, so it could not have
    /// caught this. A fixture that cannot exhibit the defect is a fixture that certifies its
    /// absence.</para>
    /// </remarks>
    [Fact]
    public void Identity_DoesNotReadAsAPath_WhenTheBranchContainsASlash()
    {
        var row = Row("Claude Code", "TheTerrace", "docs/fix-broken-design-links", LivenessState.Alive);

        var identity = SessionRowPresenter.Identity(row);

        Assert.Contains("TheTerrace@docs/fix-broken-design-links", identity);
        Assert.DoesNotContain("TheTerrace/docs", identity);
    }

    /// <summary>
    /// Two sessions alike in every visible field are still told apart.
    /// </summary>
    /// <remarks>
    /// The reported state exactly: three live rows rendered as three identical strings because
    /// agent, repository and branch matched. The session id was on the record the whole time and
    /// was never shown, so the surface could not answer "which one is this".
    /// </remarks>
    [Fact]
    public void Identity_DistinguishesSessionsThatMatchOnEveryOtherField()
    {
        var first = Row("Claude Code", "TheTerrace", "main", LivenessState.Alive) with { SessionId = "aaaa1111zzzz" };
        var second = Row("Claude Code", "TheTerrace", "main", LivenessState.Alive) with { SessionId = "bbbb2222zzzz" };

        Assert.NotEqual(SessionRowPresenter.Identity(first), SessionRowPresenter.Identity(second));
        Assert.Contains("aaaa1111", SessionRowPresenter.Identity(first));
        Assert.Contains("bbbb2222", SessionRowPresenter.Identity(second));
    }

    /// <summary>
    /// The spoken form says "on branch" rather than reading the separator aloud.
    /// </summary>
    /// <remarks>
    /// <c>@</c> removes a visual ambiguity and would introduce an audible one: a screen reader says
    /// "at", and a listener has no way to know it separates two fields rather than being part of
    /// one.
    /// </remarks>
    [Fact]
    public void TheSpokenFormNamesTheBranchRatherThanReadingTheSeparator()
    {
        var row = Row("Claude Code", "TheTerrace", "docs/fix-broken-design-links", LivenessState.Alive);

        Assert.Contains("in TheTerrace on branch docs/fix-broken-design-links", row.AccessibleName);
        Assert.DoesNotContain("@", row.AccessibleName);
    }

    /// <summary>
    /// A named session leads with the operator's name, and still says what it is.
    /// </summary>
    /// <remarks>
    /// <para>The name is resolved at render from the shell's terminal customization, not stored on
    /// the session: a terminal id IS a surface id, and the name already persists there across a
    /// restart. Carrying it into the watcher store would have been a second copy of a fact the
    /// shell already holds — two definitions of one quantity (DM7).</para>
    ///
    /// <para>The harness is KEPT rather than replaced. A row named "refactor the parser" that no
    /// longer says which harness is running has traded one missing fact for another; the operator
    /// named it to tell it from its siblings, not to hide what it is.</para>
    /// </remarks>
    [Fact]
    public void Identity_LeadsWithTheOperatorsName_WithoutLosingTheHarness()
    {
        var row = Row("Claude Code", "TheTerrace", "main", LivenessState.Alive);

        var identity = SessionRowPresenter.Identity(row, "refactor the parser");

        Assert.StartsWith("refactor the parser · ", identity);
        Assert.Contains("Claude Code", identity);
        Assert.Contains("TheTerrace@main", identity);
    }

    /// <summary>An unnamed session is unchanged — the name is additive, never a placeholder.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Identity_WithoutAName_IsTheUnnamedForm(string? name)
    {
        var row = Row("Claude Code", "TheTerrace", "main", LivenessState.Alive);

        Assert.Equal(SessionRowPresenter.Identity(row), SessionRowPresenter.Identity(row, name));
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
    public void Partition_LeadsWithLive_AndCollapsesStaleAndEndedAsInactive()
    {
        // The graveyard problem (smoke video 2026-09-02): a long-running workspace piles up stale/ended
        // terminals that bury the ones collaborating now. Only Alive is live; Stale and Ended both
        // collapse into the inactive history so the live agents lead cleanly.
        var rows = new List<WatcherSessionRow>
        {
            NoTelemetry("t1", "w1", LivenessState.Ended),
            Row("Claude Code", "TheTerrace", "main", LivenessState.Alive),
            NoTelemetry("t2", "w2", LivenessState.Ended),
            NoTelemetry("t3", "w3", LivenessState.Stale),
            NoTelemetry("t4", "w4", LivenessState.Ended),
        };

        var (live, inactive) = SessionRowPresenter.Partition(rows);

        Assert.Single(live);                             // only the Alive one leads
        Assert.Equal("Alive", live[0].Liveness.Text);
        Assert.Equal(4, inactive.Count);                 // the Stale + the three Ended, collapsed
        Assert.Equal("Stale", inactive[0].Liveness.Text); // stale-before-ended within the history
        Assert.All(inactive.Skip(1), r => Assert.Equal("Ended", r.Liveness.Text));
    }

    [Fact]
    public void Partition_WhenNoneAreAlive_LeavesLiveEmpty()
    {
        var rows = new List<WatcherSessionRow>
        {
            NoTelemetry("t1", "w1", LivenessState.Stale),
            NoTelemetry("t2", "w2", LivenessState.Ended),
        };

        var (live, inactive) = SessionRowPresenter.Partition(rows);

        Assert.Empty(live);
        Assert.Equal(2, inactive.Count);
    }

    [Fact]
    public void InactiveHeader_CountsTheCollapsedHistory()
        => Assert.Equal("14 inactive session(s)", SessionRowPresenter.InactiveHeader(14));
}
