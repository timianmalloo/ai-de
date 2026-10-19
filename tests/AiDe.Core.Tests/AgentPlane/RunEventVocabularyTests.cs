using System.Text.Json;
using System.Text.Json.Nodes;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Sessions;

namespace AiDe.Core.Tests.AgentPlane;

/// <summary>
/// The one vocabulary the folds, the host and the UI share (ADR-0041 §2, ADR-0042 §6): the
/// extended <c>TurnState</c> and the <c>RunEvent</c> envelope's redaction fields and
/// <c>native.frame</c> namespace. Names are asserted as strings so a rename is a failing test, not a
/// silent edit.
/// Fails if: a value is missing, <c>Stopped</c> survives, or an envelope key is renamed or dropped.
/// </summary>
public sealed class RunEventVocabularyTests
{
    [Fact]
    public void TurnState_IsExtendedAndStoppedIsRenamedInterrupted()
    {
        var names = Enum.GetNames<TurnState>();

        string[] added = ["Preparing", "StopRequested", "DeliveryUnknown", "Rejected", "Denied", "OverLimit", "Interrupted"];
        Assert.All(added, name => Assert.Contains(name, names));
        Assert.DoesNotContain("Stopped", names);
        Assert.DoesNotContain("HasEvidence", names);
    }

    [Fact]
    public void RunEvent_CarriesRedactionsAndScannedWith_UnderTheirExactKeys()
    {
        var wire = new JsonObject
        {
            ["RunId"] = "run-1",
            ["AgentId"] = "agent-1",
            ["ParentAgentId"] = null,
            ["Seq"] = 1,
            ["Ts"] = "2026-10-18T00:00:00+00:00",
            ["Kind"] = "native.frame",
            ["Cost"] = null,
            ["Body"] = new JsonObject(),
            ["Ext"] = new JsonObject(),
            ["redactions"] = new JsonArray(new JsonObject { ["offset"] = 3, ["len"] = 12, ["pattern_id"] = "aws-key" }),
            ["scanned_with"] = "patterns v1",
        };

        var parsed = JsonSerializer.Deserialize<RunEvent>(wire.ToJsonString());
        var echoed = JsonNode.Parse(JsonSerializer.Serialize(parsed))!.AsObject();

        Assert.Equal(
            new[] { "Body", "AgentId", "Cost", "Ext", "Kind", "ParentAgentId", "RunId", "Seq", "Ts", "redactions", "scanned_with" }
                .Order(StringComparer.Ordinal),
            echoed.Select(p => p.Key).Order(StringComparer.Ordinal));
        Assert.Equal(
            new[] { "len", "offset", "pattern_id" },
            echoed["redactions"]!.AsArray()[0]!.AsObject().Select(p => p.Key).Order(StringComparer.Ordinal));
        Assert.Equal("patterns v1", (string?)echoed["scanned_with"]);
    }

    [Fact]
    public void RunEvent_WithoutRedactionFields_DeserializesAsNotScanned()
    {
        var legacy = """{"RunId":"r","AgentId":"a","ParentAgentId":null,"Seq":1,"Ts":"2026-10-18T00:00:00+00:00","Kind":"acp.frame","Cost":null,"Body":{},"Ext":{}}""";

        var parsed = JsonSerializer.Deserialize<RunEvent>(legacy);

        Assert.NotNull(parsed);
        var echoed = JsonNode.Parse(JsonSerializer.Serialize(parsed))!.AsObject();
        Assert.Null(echoed["redactions"]);
        Assert.Null(echoed["scanned_with"]);
    }

    [Fact]
    public void TheKindVocabulary_NamesTheNativeFrameNamespaceBesideAcpFrame()
    {
        var kinds = typeof(RunEvent).Assembly.GetType("AiDe.Core.AgentPlane.RunEventKinds");

        Assert.NotNull(kinds);
        Assert.Equal("native.frame", kinds!.GetField("NativeFrame")?.GetRawConstantValue());
        Assert.Equal("acp.frame", kinds.GetField("AcpFrame")?.GetRawConstantValue());
    }

    private static readonly DateTimeOffset T0 = new(2026, 10, 18, 9, 0, 0, TimeSpan.Zero);

    private static TurnView TurnIn(TurnState state) => new(
        1, "env-1", "words of b1", [new("shape", "goal block", "projection", "")], state,
        TurnView.IsTerminal(state) ? new OutcomeView("claude-code", null, 1, null, TimeSpan.FromSeconds(5), 3) : null,
        null, [], "bytes", T0);

    [Theory]
    [InlineData(TurnState.Interrupted, true, "stopped by you", "Turn b1 stopped by you after")]
    [InlineData(TurnState.Preparing, false, "preparing", "Turn b1 accepted as a goal block")]
    [InlineData(TurnState.StopRequested, false, "stop requested", "Turn b1: stop requested. Waiting for the harness to acknowledge.")]
    [InlineData(TurnState.DeliveryUnknown, true, "delivery unknown", "Turn b1: delivery is unknown")]
    [InlineData(TurnState.Rejected, true, "rejected", "Turn b1: the harness rejected this turn")]
    [InlineData(TurnState.Denied, true, "denied", "Turn b1: a permission request of this turn was denied")]
    [InlineData(TurnState.OverLimit, true, "over limit", "Turn b1: this turn went over a limit")]
    public void EachNewTurnState_HasAWordATerminalityAndAnAnnouncement(TurnState state, bool terminal, string word, string announced)
    {
        Assert.Equal(terminal, TurnView.IsTerminal(state));
        Assert.Equal(word, TurnCopy.OutcomeWord(state, exitCode: null));

        var policy = new ThreadAnnouncementPolicy();
        policy.Next(new ThreadSnapshot([TurnIn(TurnState.Running)], 0, true));
        var emitted = policy.Next(new ThreadSnapshot([TurnIn(state)], 1, true));

        Assert.StartsWith(announced, Assert.Single(emitted).Text, StringComparison.Ordinal);
    }

    [Fact]
    public void OnlyInterrupted_ReadsStoppedByYou_SoAShutdownOrATokenCancelNeverClaimsTheOperatorStoppedIt()
    {
        // Ruling 167: "by you" is true only because Interrupted is written solely under an operator Stop
        // (AttemptStopTests.ATokenCancelAlone_..._IsNeverInterrupted: shutdown ends DeliveryUnknown{relay_lost}).
        Assert.Equal("stopped by you", TurnCopy.OutcomeWord(TurnState.Interrupted, exitCode: null));
        foreach (var state in Enum.GetValues<TurnState>().Where(s => s != TurnState.Interrupted))
        {
            Assert.NotEqual("stopped by you", TurnCopy.OutcomeWord(state, exitCode: null));
        }

        Assert.DoesNotContain("by you", TurnCopy.OutcomeWord(TurnState.DeliveryUnknown, exitCode: null), StringComparison.Ordinal);
    }

    [Fact]
    public void StopRequested_IsInFlight_SoASendDuringItQueues()
    {
        var snapshot = new ThreadSnapshot([TurnIn(TurnState.StopRequested)], 1, true);

        Assert.Equal(1, snapshot.InFlight?.Ordinal);
    }

    [Fact]
    public void RequestStop_MovesAnInFlightTurnToStopRequested_AndOnlyTheHarnessConcludesItInterrupted()
    {
        var thread = new RunChannelSessionThread(caughtUp: true);
        var ordinal = thread.Accept("words", [], "bytes", T0);

        thread.RequestStop(ordinal);
        Assert.Equal(TurnState.StopRequested, thread.Current.Turns.Single().State);
        Assert.Null(thread.Current.Turns.Single().Outcome);

        thread.Conclude(ordinal, TurnState.Interrupted, T0.AddSeconds(2));
        Assert.Equal(TurnState.Interrupted, thread.Current.Turns.Single().State);
        Assert.Throws<InvalidOperationException>(() => thread.RequestStop(ordinal));
    }
}
