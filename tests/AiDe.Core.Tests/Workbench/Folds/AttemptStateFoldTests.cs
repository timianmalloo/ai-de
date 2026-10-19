using System.Diagnostics;
using System.Text.Json.Nodes;
using AiDe.Core.AgentPlane;
using AiDe.Core.Facts;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.Store.Records;
using AiDe.Core.Workbench.Records.Folds;

namespace AiDe.Core.Tests.Workbench.Folds;

/// <summary>ADR-0041 §2: non-terminal state is a pure fold over outcome rows and the durable run log.</summary>
public sealed class AttemptStateFoldTests
{
    private const string Key = "dk-1";

    private static RunEvent Ev(long seq, string kind) =>
        new("run-1", "claude-code", null, seq, DateTimeOffset.UnixEpoch.AddSeconds(seq), kind, null, new JsonObject(), new JsonObject());

    private static OutcomeRow Out(DispatchState s, long ingress, string? cause = null, string? detail = null) =>
        new(Key, s, null, cause, detail) { IngressSeq = ingress };

    private static PermissionDecisionRow Decision(long seq, string option) =>
        new("s1", "run-1", seq, "edit", "scope", "sha", "[]", option, "operator") { IngressSeq = seq };

    private static AttemptState Fold(
        RunEvent[]? events = null, OutcomeRow[]? outcomes = null,
        PermissionDecisionRow[]? decisions = null, EvidenceLinkRow[]? links = null) =>
        AttemptStateFold.Evaluate(new AttemptStateInput(
            Key, events ?? [], outcomes ?? [], decisions ?? [], links ?? []));

    private static readonly OutcomeRow Dispatched = Out(DispatchState.Dispatched, 1);

    [Fact]
    public void NoOutcomeRows_IsPreparing() =>
        Assert.Equal(TurnState.Preparing, Fold().State);

    [Fact]
    public void DispatchedWithOnlyAgentOutput_IsRunning() =>
        Assert.Equal(TurnState.Running, Fold([Ev(1, "agent.msg")], [Dispatched]).State);

    [Fact]
    public void AnUnansweredPermissionRequest_IsWaiting() =>
        Assert.Equal(TurnState.Waiting, Fold([Ev(1, "agent.msg"), Ev(2, "permission.request")], [Dispatched]).State);

    [Fact]
    public void AnAnsweredAllowedRequest_IsRunningAgain() =>
        Assert.Equal(TurnState.Running,
            Fold([Ev(2, "permission.request"), Ev(3, "tool.result")], [Dispatched], [Decision(2, "allow_once")]).State);

    [Fact]
    public void ARejectedRequestWithNoLaterEvent_IsDenied() =>
        Assert.Equal(TurnState.Denied,
            Fold([Ev(1, "agent.msg"), Ev(2, "permission.request")], [Dispatched], [Decision(2, "reject_once")]).State);

    [Fact]
    public void ARejectedRequestFollowedByAnEvent_IsRunning() =>
        Assert.Equal(TurnState.Running,
            Fold([Ev(2, "permission.request"), Ev(3, "agent.msg")], [Dispatched], [Decision(2, "reject_once")]).State);

    [Fact]
    public void OnlyTheLastDecisionDecidesDenied() =>
        Assert.Equal(TurnState.Running,
            Fold([Ev(2, "permission.request"), Ev(4, "permission.request")], [Dispatched],
                [Decision(2, "reject_once"), Decision(4, "allow_once")]).State);

    [Theory]
    [InlineData("budget.exceeded")]
    [InlineData("context.ceiling")]
    public void ABudgetOrContextCeilingEvent_IsOverLimit(string kind) =>
        Assert.Equal(TurnState.OverLimit, Fold([Ev(1, "agent.msg"), Ev(2, kind)], [Dispatched]).State);

    [Fact]
    public void StopRequestedDominatesWaiting() =>
        Assert.Equal(TurnState.StopRequested,
            Fold([Ev(1, "permission.request")], [Dispatched, Out(DispatchState.StopRequested, 2, detail: "1")]).State);

    [Fact]
    public void AnUncorrelatedAcpResultOrError_NeverYieldsATerminalState()
    {
        Assert.Equal(TurnState.Running, Fold([Ev(1, "acp.result")], [Dispatched]).State);
        Assert.Equal(TurnState.Running, Fold([Ev(1, "acp.error")], [Dispatched]).State);
    }

    [Theory]
    [InlineData(DispatchState.ResponseComplete, TurnState.Completed)]
    [InlineData(DispatchState.Rejected, TurnState.Rejected)]
    [InlineData(DispatchState.Failed, TurnState.Failed)]
    [InlineData(DispatchState.DeliveryUnknown, TurnState.DeliveryUnknown)]
    [InlineData(DispatchState.Interrupted, TurnState.Interrupted)]
    public void ATerminalOutcomeRow_WinsOverTheLog(DispatchState row, TurnState expected) =>
        Assert.Equal(expected,
            Fold([Ev(1, "permission.request")], [Dispatched, Out(row, 5)]).State);

    [Fact]
    public void TwoTerminals_TheLowestIngressSeqWins()
    {
        var state = Fold(outcomes: [Dispatched, Out(DispatchState.Failed, 9), Out(DispatchState.ResponseComplete, 4)]);
        Assert.Equal(TurnState.Completed, state.State);
    }

    [Fact]
    public void DeliveryUnknown_CarriesItsCause() =>
        Assert.Equal("relay_lost",
            Fold(outcomes: [Dispatched, Out(DispatchState.DeliveryUnknown, 3, cause: "relay_lost")]).Cause);

    [Fact]
    public void ADecisionCitingASeqTheLogLacks_IsNotRecorded_AndNeverDenied()
    {
        var state = Fold([Ev(1, "agent.msg")], [Dispatched], [Decision(7, "reject_once")]);

        Assert.Equal(TurnState.Running, state.State);
        Assert.Equal([7L], state.NotRecordedSeqs);
    }

    [Fact]
    public void AStopHighWaterMarkTheLogLacks_IsNotRecorded()
    {
        var state = Fold([Ev(1, "agent.msg")], [Dispatched, Out(DispatchState.StopRequested, 2, detail: "40")]);

        Assert.Equal(TurnState.StopRequested, state.State);
        Assert.Equal([40L], state.NotRecordedSeqs);
    }

    [Fact]
    public void HasEvidence_IsADerivedBool_NotAState()
    {
        var link = new EvidenceLinkRow("l1", "criterion", "c1", 1, AttemptStateFold.AttemptArtifactRef(Key), "p", "b", "unverified");
        var interrupted = Fold(outcomes: [Dispatched, Out(DispatchState.Interrupted, 3)], links: [link]);

        Assert.Equal(TurnState.Interrupted, interrupted.State);
        Assert.True(interrupted.HasEvidence);
        Assert.False(Fold(outcomes: [Dispatched]).HasEvidence);
    }

    [Fact]
    public void ALinkToAnotherAttempt_IsNotEvidenceForThisOne()
    {
        var other = new EvidenceLinkRow("l1", "criterion", "c1", 1, AttemptStateFold.AttemptArtifactRef("dk-2"), "p", "b", "unverified");
        Assert.False(Fold(outcomes: [Dispatched], links: [other]).HasEvidence);
    }

    [Fact]
    public void TheFoldIsPure_SameInputSameAnswer()
    {
        RunEvent[] events = [Ev(1, "permission.request")];
        OutcomeRow[] outcomes = [Dispatched];
        Assert.Equal(Fold(events, outcomes).State, Fold(events, outcomes).State);
    }

    [Fact]
    public void EachEvaluation_EmitsOneFoldDurationMs()
    {
        var seen = new List<(string Fold, double Ms)>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == FoldSignal.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a => seen.Add((
                (string)a.GetTagItem("fold")!, (double)a.GetTagItem("duration_ms")!)),
        };
        ActivitySource.AddActivityListener(listener);

        Fold([Ev(1, "agent.msg")], [Dispatched]);
        Fold();
        CurrentnessFold.Evaluate(new AttemptRow(Key, "w", 1, "n", 1, "s", 1, "p1"), 1, "p1");

        Assert.Equal(["attempt-state", "attempt-state", "currentness"], seen.Select(s => s.Fold));
        Assert.All(seen, s => Assert.True(s.Ms >= 0));
    }
}
