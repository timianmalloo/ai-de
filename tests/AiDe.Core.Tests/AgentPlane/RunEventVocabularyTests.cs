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
}
