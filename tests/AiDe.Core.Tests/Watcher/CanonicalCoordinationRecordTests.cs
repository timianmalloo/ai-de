using System.Text.Json;
using System.Text;
using System.Text.Json.Nodes;
using System.Security.Cryptography;
using AiDe.Core.Watcher;

namespace AiDe.Core.Tests.Watcher;

public sealed class CanonicalCoordinationRecordTests
{
    [Theory]
    [InlineData("integer-generation")]
    [InlineData("boolean-sequence")]
    [InlineData("schema-float")]
    [InlineData("timestamp-bool")]
    [InlineData("huge-timestamp")]
    public void Parse_CodecAcceptedBadEnvelope_RejectsSchema(string name)
    {
        using var corpus = JsonDocument.Parse(File.ReadAllBytes(
            Path.Combine(AppContext.BaseDirectory, "canonical-coordination-v1.json")));
        var item = corpus.RootElement.GetProperty("invalid").EnumerateArray()
            .Single(item => item.GetProperty("name").GetString() == name);
        var bytes = Convert.FromHexString(item.GetProperty("hex").GetString()!);

        var result = CanonicalCoordinationRecord.Parse(bytes);

        Assert.Equal(CanonicalRecordStatus.Invalid, result.Status);
        Assert.Equal(name == "huge-timestamp" ? CanonicalErrors.Field : CanonicalErrors.Schema, result.Code);
        Assert.Null(result.Record);
    }

    [Fact]
    public void Parse_WholeBodies_MatchesPinnedPythonOracle()
    {
        using var corpus = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "validator-oracle.json")));
        var cases = corpus.RootElement.GetProperty("cases").EnumerateArray().ToArray();
        Assert.Equal(115, cases.Length);
        foreach (var item in cases)
        {
            var result = CanonicalCoordinationRecord.Parse(Encoding.UTF8.GetBytes(item.GetProperty("source").GetString()!));
            var expected = item.GetProperty("expectedCode").GetString();
            Assert.True(result.Code == expected, item.GetProperty("name").GetString() + ": " + result.Code);
            Assert.Equal(expected == "OK" ? CanonicalRecordStatus.Valid : CanonicalRecordStatus.Invalid, result.Status);
            Assert.Equal(item.GetProperty("canonical").GetString(),
                result.Record is { } record ? Encoding.UTF8.GetString(record.Canonical) : null);
        }
    }

    internal static byte[] Example()
    {
        using var corpus = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "canonical-coordination-v1.json")));
        return Encoding.UTF8.GetBytes(corpus.RootElement.GetProperty("events")[0].GetProperty("source").GetString()!);
    }

    internal static byte[] Seal(JsonObject body)
    {
        var bytes = Encoding.UTF8.GetBytes(body.ToJsonString());
        body["payloadDigest"] = Convert.ToHexStringLower(SHA256.HashData(CanonicalCoordinationCodec.Bytes(bytes)));
        return Encoding.UTF8.GetBytes(body.ToJsonString());
    }

    [Fact]
    public void Parse_OriginalGoldenEvents_PreservesFullBytesAndKeys()
    {
        using var corpus = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "canonical-coordination-v1.json")));
        foreach (var item in corpus.RootElement.GetProperty("events").EnumerateArray())
        {
            var result = CanonicalCoordinationRecord.Parse(Encoding.UTF8.GetBytes(item.GetProperty("source").GetString()!));
            Assert.Equal(CanonicalRecordStatus.Valid, result.Status);
            Assert.Equal(item.GetProperty("canonical").GetString(), Encoding.UTF8.GetString(result.Record!.Canonical));
            Assert.Equal(item.GetProperty("key").EnumerateArray().Select(v => v.GetString()),
                new[] { result.Record.RepositoryId, result.Record.StreamId, result.Record.EventId });
            Assert.Equal("Unknown", result.Record.IssuerQualification);
            Assert.Equal("Unknown", result.Record.GenerationQualification);
            Assert.Equal("Denied", result.Record.Authorization);
        }
    }

    [Theory]
    [InlineData(65536, "\n", true)]
    [InlineData(65536, "\r\n", true)]
    [InlineData(65537, "\n", false)]
    [InlineData(65537, "\r\n", false)]
    public void Parse_LineBoundary_CountsContentSeparately(int content, string ending, bool valid)
    {
        var input = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(Example()).PadRight(content) + ending);
        var result = CanonicalCoordinationRecord.Parse(input);
        Assert.Equal(valid ? CanonicalRecordStatus.Valid : CanonicalRecordStatus.Invalid, result.Status);
        Assert.Equal(input.Length, result.RawBytes);
    }

    [Theory]
    [InlineData("{\"x\":NaN}", "XH.NONFINITE_JSON")]
    [InlineData("{\"x\":Infinity}", "XH.NONFINITE_JSON")]
    [InlineData("{\"x\":1e309}", "XH.NONFINITE_JSON")]
    [InlineData("{\"x\":1,\"\\u0078\":2}", "XH.DUPLICATE_KEY")]
    [InlineData("{\"x\":\"\\ud800\"}", "XH.SCHEMA_INVALID")]
    [InlineData("{\"x\":\"\\udc00\"}", "XH.SCHEMA_INVALID")]
    [InlineData("null", "XH.SCHEMA_INVALID")]
    public void Parse_InvalidLexicalForms_ReturnsStableCode(string input, string code) =>
        Assert.Equal(code, CanonicalCoordinationRecord.Parse(Encoding.UTF8.GetBytes(input)).Code);

    [Fact]
    public void Parse_UnsupportedVersionsAndIntegerBound_AreVisible()
    {
        var body = JsonNode.Parse(Example())!.AsObject();
        body["schemaVersion"] = 2;
        Assert.Equal(CanonicalRecordStatus.Unsupported, CanonicalCoordinationRecord.Parse(Seal(body)).Status);
        body["schemaVersion"] = 1;
        body["eventType"] = "triage";
        Assert.Equal(CanonicalRecordStatus.Unsupported, CanonicalCoordinationRecord.Parse(Seal(body)).Status);
        var big = Encoding.UTF8.GetBytes("{\"x\":" + new string('9', 4301) + "}");
        Assert.Equal(CanonicalErrors.IntegerUnsupported, CanonicalCoordinationRecord.Parse(big).Code);
    }

    [Fact]
    public void Parse_ReceiptExcludedFromDigest_StillValidatesReceiptAndDigest()
    {
        var body = JsonNode.Parse(Example())!.AsObject();
        var original = CanonicalCoordinationRecord.Parse(Seal(body)).Record!.Canonical.ToArray();
        body["recordedAt"] = 1234.0;
        Assert.Equal(original, CanonicalCoordinationRecord.Parse(Encoding.UTF8.GetBytes(body.ToJsonString())).Record!.Canonical.ToArray());
        body["recordedAt"] = null;
        Assert.Equal(CanonicalErrors.Schema, CanonicalCoordinationRecord.Parse(Seal(body)).Code);
        body["recordedAt"] = 1234.0;
        body["payloadDigest"] = new string('0', 64);
        Assert.Equal(CanonicalErrors.Digest, CanonicalCoordinationRecord.Parse(Encoding.UTF8.GetBytes(body.ToJsonString())).Code);
    }

    [Fact]
    public void Parse_FullReserializedFactBoundary_IncludesReceiptHashAndSpaces()
    {
        using var corpus = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "validator-oracle.json")));
        foreach (var size in new[] { 65536, 65537 })
        {
            var item = corpus.RootElement.GetProperty("cases").EnumerateArray()
                .Single(item => item.GetProperty("name").GetString() == "full-fact-" + size);
            var source = Encoding.UTF8.GetBytes(item.GetProperty("source").GetString()!);
            using var document = JsonDocument.Parse(source);
            Assert.True(source.Length < 65536);
            Assert.Equal(size, item.GetProperty("fullSize").GetInt32());
            Assert.Equal(size, CanonicalCoordinationCodec.FullSize(document.RootElement));
            Assert.Equal(size == 65536 ? "OK" : CanonicalErrors.TooLarge,
                CanonicalCoordinationRecord.Parse(source).Code);
        }
    }

    [Fact]
    public void Parse_ExpandedResponseBoundary_RejectsBeforeDigestComparison()
    {
        var body = JsonNode.Parse(Example())!.AsObject();
        body["payload"]!["numbers"] = new JsonArray(Enumerable.Range(0, 6000)
            .Select(_ => (JsonNode?)JsonValue.Create(100.0)).ToArray());
        var raw = Encoding.UTF8.GetBytes(body.ToJsonString());
        // System.Text.Json writes 100; replace that one array with compact exponent tokens.
        var source = Encoding.UTF8.GetString(raw).Replace("100", "1e2", StringComparison.Ordinal);
        Assert.Equal(CanonicalErrors.Schema, CanonicalCoordinationRecord.Parse(Encoding.UTF8.GetBytes(source)).Code);
    }
}
