using AiDe.App.Tests.Sessions.Thread;
using AiDe.App.Workbench.Composer;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Presentation.Sessions;

namespace AiDe.App.Tests.Records;

/// <summary>
/// T3's follow-ups (ADR-0041 §2's states): a <c>StopRequested</c> turn is still in flight but is not "running" — the
/// composer's offer uses T3's copy — and the Console follows a <c>Preparing</c> or <c>StopRequested</c> turn as live.
/// </summary>
public sealed class TheLiveStatesReadAsLiveTests
{
    private const string StopCopy = "Stop requested. Waiting for the harness to acknowledge.";

    private static TurnView InState(int ordinal, TurnState state) =>
        ThreadFixtures.Turn(ordinal, "in flight", [], state, null, ThreadFixtures.Lines(ordinal, 2));

    [Fact]
    public void TheComposersOffer_ForAStopRequestedTurn_UsesTheStopCopy_NeverRunning()
    {
        var offer = ComposerSurface.WaitOrParallelOffer(InState(1, TurnState.StopRequested));

        Assert.Equal($"b1: {StopCopy} Wait — send after b1, or start a parallel session.", offer);
        Assert.Equal("b1, stop requested", ComposerSurface.InFlightLinkName(InState(1, TurnState.StopRequested)));
        Assert.Equal("b1, running", ComposerSurface.InFlightLinkName(InState(1, TurnState.Running)));
        Assert.Equal("b1 is running. Wait — send after b1, or start a parallel session.", ComposerSurface.WaitOrParallelOffer(InState(1, TurnState.Running)));
    }

    [Theory]
    [InlineData(TurnState.Preparing)]
    [InlineData(TurnState.StopRequested)]
    [InlineData(TurnState.Running)]
    public void TheConsole_FollowsEveryLiveState(TurnState state)
    {
        Sta.Run(() =>
        {
            var console = new ConsoleSurface();
            console.Show([InState(1, state)]);
            Assert.Equal("following b1", console.Status);
        });
    }
}
