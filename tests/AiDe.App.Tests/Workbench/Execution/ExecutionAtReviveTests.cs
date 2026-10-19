using AiDe.App.Workbench.Execution;
using AiDe.Core.Dispatch;
using AiDe.Core.Facts;
using AiDe.Core.Store.Records;

namespace AiDe.App.Tests.Kinds;

/// <summary>
/// The revive rule's predicate (the WL-S design's §4): only the LAST read-back receipt, in dispatch order, and only when
/// the host's startup sweep concluded it <c>DeliveryUnknown{host_restart}</c>, opens an execution.
/// </summary>
public sealed class ExecutionAtReviveTests
{
    private static AttemptReceipt Receipt(string key, DispatchState? terminal, string? cause = null) =>
        new(new AttemptRow(key, "workspace", 1, "nb", 1, "s", 1),
            terminal is { } state ? [new OutcomeRow(key, state, Cause: cause)] : []);

    [Fact]
    public void TheLastReceipt_SweptAtHostRestart_IsTheOneThatOpens()
    {
        var live = Receipt("b", DispatchState.DeliveryUnknown, "host_restart");

        Assert.Same(live, ExecutionAtRevive.LiveAtClose([Receipt("a", DispatchState.ResponseComplete), live]));
    }

    [Fact]
    public void AnEarlierSweptReceipt_IsNotOpened_WhenALaterTurnRanAfterIt()
    {
        Assert.Null(ExecutionAtRevive.LiveAtClose([Receipt("a", DispatchState.DeliveryUnknown, "host_restart"), Receipt("b", DispatchState.ResponseComplete)]));
    }

    [Theory]
    [InlineData(DispatchState.DeliveryUnknown, "relay_lost")]
    [InlineData(DispatchState.Failed, "host_restart")]
    [InlineData(DispatchState.ResponseComplete, null)]
    public void ALastReceipt_WithAnyOtherTerminal_OpensNothing(DispatchState state, string? cause)
    {
        Assert.Null(ExecutionAtRevive.LiveAtClose([Receipt("a", state, cause)]));
    }

    [Fact]
    public void NoReadBack_AndAnOpenAttempt_OpenNothing()
    {
        Assert.Null(ExecutionAtRevive.LiveAtClose([]));
        Assert.Null(ExecutionAtRevive.LiveAtClose([Receipt("a", terminal: null)]));
    }
}
