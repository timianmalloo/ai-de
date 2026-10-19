using System.Windows.Automation;
using System.Windows.Controls;
using AiDe.App.Conductor;
using AiDe.App.Tests.Sessions.Thread;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.Store.Records;

namespace AiDe.App.Tests.Records;

/// <summary>
/// Ruling 161/162 (AC-R01a): the turn row's provenance disclosure shows the attempt's own revision, request and route,
/// read from the attempt via the host, and the same line is that disclosure's UIA HelpText. The row's Name is untouched.
/// </summary>
public sealed class TheTurnRowShowsItsAttemptBindingTests
{
    private const string Sha = "ab12cd34ef56a7b8c9d0";

    private static AttemptRow Attempt(int revision, string sha, string? route) =>
        new("k-" + revision, "ws", 1, "nb", revision, "s", 1, ProjectionSha: sha, RouteFingerprint: route);

    private static TurnView Turn(int ordinal) =>
        new(ordinal, "env-" + ordinal, "Words " + ordinal, [], TurnState.Completed,
            new OutcomeView("claude-code", null, 0, null, null, 0), null, [], "b", ThreadFixtures.T0);

    [Fact]
    public void TheLine_NamesRevisionRequestAndRoute_InTheRulingsWords()
    {
        var item = new TurnItem(Turn(1)) { Attempt = Attempt(3, Sha, "acp:claude-code") };

        Assert.Equal("Sent from revision 3 · request ab12cd3 · via claude-code", item.BindingLine);
    }

    [Fact]
    public void ATurnWithNoAttempt_SaysSo_AndInventsNoValues()
    {
        var item = new TurnItem(Turn(1));

        Assert.Equal("Attempt not recorded", item.BindingLine);
    }

    [Fact]
    public void AnAttemptMissingAField_SaysNotRecordedForThatField_NeverGuesses()
    {
        var item = new TurnItem(Turn(1)) { Attempt = Attempt(2, "ab", null) with { ProjectionSha = null } };

        Assert.Equal("Sent from revision 2 · request not recorded · via not recorded", item.BindingLine);
    }

    [Theory]
    [InlineData("acp:claude-code", "claude-code")]
    [InlineData("acp:codex", "codex")]
    [InlineData("pty", "Terminal")]
    [InlineData("acp:", "acp:")]
    [InlineData("route-x:v2", "route-x:v2")]
    [InlineData("PTY", "PTY")]
    public void TheRouteWords_AreTheEngineIdOrTerminal_AndAnyOtherShapeRendersVerbatim(string fingerprint, string expected)
    {
        Assert.Equal(expected, RouteWords.For(fingerprint));
    }

    [Theory]
    [InlineData("claude-code")]
    [InlineData("codex")]
    [InlineData("gemini")]
    public void TheSheetsEngineLine_AndTheRowsRoute_AreTheSameWords_ForTheSameEngine(string engineId)
    {
        var fingerprint = new AttemptDispatch(
            new GovernedRunRequest("r", "d", "a", engineId, "m", "l", "t", null, null, "p", [], []),
            "ws", 1, "env", "sha", "nb", 1, "s", 1, () => "sha").ToRow().RouteFingerprint!;

        Assert.Equal(RouteWords.Engine(engineId), RouteWords.For(fingerprint));
    }

    [Fact]
    public void AfterAnEditSendsTurnB_TurnAsRowStillShowsItsOwnRevisionAndRequest()
    {
        var a = Attempt(1, Sha, "acp:claude-code");
        var b = Attempt(2, "99ffee11223344556677", "acp:claude-code");
        Sta.Pump(
            create: () =>
            {
                var feed = ThreadFixtures.Feed([Turn(1), Turn(2)]).Feed;
                feed.AttemptOf = ordinal => ordinal == 1 ? a : b;
                return feed;
            },
            configure: window => { window.Width = 1200; window.Height = 700; },
            body: (window, feed) =>
            {
                Assert.Equal("Sent from revision 1 · request ab12cd3 · via claude-code", feed.Rows[0].BindingLine);
                Assert.Equal("Sent from revision 2 · request 99ffee1 · via claude-code", feed.Rows[1].BindingLine);
                return Task.CompletedTask;
            });
    }

    [Fact]
    public void TheLine_IsTheProvenanceDisclosuresHelpText_AndTheRowsNameIsUnchanged()
    {
        var a = Attempt(1, Sha, "acp:claude-code");
        Sta.Pump(
            create: () =>
            {
                var feed = ThreadFixtures.Feed([Turn(1)]).Feed;
                feed.AttemptOf = _ => a;
                return feed;
            },
            configure: window => { window.Width = 1200; window.Height = 700; },
            body: (window, feed) =>
            {
                var container = ThreadFixtures.Container(feed, 0);
                var expander = ThreadFixtures.Visuals<Expander>(container).First(e => AutomationProperties.GetName(e) == "Provenance of b1");

                Assert.Equal("Sent from revision 1 · request ab12cd3 · via claude-code", AutomationProperties.GetHelpText(expander));
                Assert.Equal("b1, Words 1", AutomationProperties.GetName(container));
                return Task.CompletedTask;
            });
    }
}
