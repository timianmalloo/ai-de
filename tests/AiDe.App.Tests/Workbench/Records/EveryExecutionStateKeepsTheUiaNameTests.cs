using System.Windows.Automation;
using AiDe.App.Tests.Sessions.Thread;
using AiDe.Core.Presentation.Sessions;

namespace AiDe.App.Tests.Records;

/// <summary>
/// UIA names for the execution states (node 4 obligation): whatever state a turn row is in, its UIA Name is the ordinal
/// and the turn's own words (<c>TurnCopy.Name</c>), and the state's reason sentence stays in HelpText.
/// </summary>
public sealed class EveryExecutionStateKeepsTheUiaNameTests
{
    private const string Words = "Refactor the layout store";

    public static TheoryData<TurnState> States => [.. Enum.GetValues<TurnState>()];

    private static TurnView InState(TurnState state)
    {
        var outcome = TurnView.IsTerminal(state) ? new OutcomeView("claude-code", state == TurnState.Failed ? 1 : null, 0, null, null, 0) : null;
        var waiting = state == TurnState.Waiting ? new WaitingRequest("r1", "permission", "Allow the edit?", [TurnActionKind.Stop]) : null;
        return new TurnView(1, "env-0001", Words, [], state, outcome, waiting, [], "b", ThreadFixtures.T0);
    }

    [Theory]
    [MemberData(nameof(States))]
    public void TheRowsUiaName_IsTheOrdinalAndTheWords_AndTheReasonSentenceIsItsHelpText(TurnState state)
    {
        Sta.Pump(
            create: () => ThreadFixtures.Feed([InState(state)]).Feed,
            configure: window => { window.Width = 1200; window.Height = 700; },
            body: (window, feed) =>
            {
                var container = ThreadFixtures.Container(feed, 0);

                Assert.Equal("b1, " + Words, AutomationProperties.GetName(container));
                if (state != TurnState.Queued)
                {
                    Assert.Equal(TurnCopy.ReasonSentence(InState(state)), AutomationProperties.GetHelpText(container));
                }

                return Task.CompletedTask;
            });
    }
}
