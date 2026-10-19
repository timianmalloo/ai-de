using AiDe.Core.AgentPlane;

namespace AiDe.Core.Tests.AgentPlane;

/// <summary>
/// The ingest-side credential scan (ADR-0042 §6): a pure function of the frame text and the
/// pattern-set version. Offset is where the marker starts in the redacted (stored) frame; Len is the length of the
/// original span the marker replaced. A sentinel that was never seen red
/// is not evidence.
/// Fails if: a sentinel survives, a non-sentinel byte moves, two overlapping matches stay two
/// spans, or the result is not stamped <c>patterns v1</c>.
/// </summary>
public sealed class FrameRedactionTests
{
    [Fact]
    public void SentinelAtTheStart()
    {
        var sentinel = FrameRedaction.TestSentinel;
        var frame = sentinel + " tail";

        var result = FrameRedaction.Apply(frame, FrameRedaction.PatternsVersion);

        AssertReplaced(frame, result, sentinel, "test-sentinel");
        Assert.Equal(0, Assert.Single(result.Redactions).Offset);
        Assert.Equal(FrameRedaction.Marker + " tail", result.Frame);
    }

    [Fact]
    public void SentinelInTheMiddle()
    {
        var sentinel = FrameRedaction.TestSentinel;
        var frame = "head " + sentinel + " tail";

        var result = FrameRedaction.Apply(frame, FrameRedaction.PatternsVersion);

        var span = AssertReplaced(frame, result, sentinel, "test-sentinel");
        Assert.Equal(frame.IndexOf(sentinel, StringComparison.Ordinal), span.Offset);
        Assert.Equal("head " + FrameRedaction.Marker + " tail", result.Frame);
    }

    [Fact]
    public void SentinelAtTheEnd()
    {
        var sentinel = FrameRedaction.TestSentinel;
        var frame = "head " + sentinel;

        var result = FrameRedaction.Apply(frame, FrameRedaction.PatternsVersion);

        var span = AssertReplaced(frame, result, sentinel, "test-sentinel");
        Assert.Equal(frame.Length - sentinel.Length, span.Offset);
        Assert.Equal("head " + FrameRedaction.Marker, result.Frame);
    }

    [Fact]
    public void SentinelTwice()
    {
        var sentinel = FrameRedaction.TestSentinel;
        var frame = sentinel + " mid " + sentinel;

        var result = FrameRedaction.Apply(frame, FrameRedaction.PatternsVersion);

        Assert.Equal(FrameRedaction.Marker + " mid " + FrameRedaction.Marker, result.Frame);
        Assert.DoesNotContain(sentinel, result.Frame, StringComparison.Ordinal);
        Assert.Equal(2, result.Redactions.Count);
        Assert.Equal(0, result.Redactions[0].Offset);
        Assert.Equal(sentinel.Length, result.Redactions[0].Len);
        Assert.Equal("test-sentinel", result.Redactions[0].PatternId);
        Assert.Equal(FrameRedaction.Marker.Length + " mid ".Length, result.Redactions[1].Offset);
        Assert.Equal(sentinel.Length, result.Redactions[1].Len);
        Assert.Equal("test-sentinel", result.Redactions[1].PatternId);
        AssertMarkersAtOffsets(result);
    }

    [Fact]
    public void SentinelInsideAJsonString()
    {
        var sentinel = FrameRedaction.TestSentinel;
        var frame = "{\"authorization\":\"" + sentinel + "\"}";

        var result = FrameRedaction.Apply(frame, FrameRedaction.PatternsVersion);

        var span = AssertReplaced(frame, result, sentinel, "test-sentinel");
        Assert.Equal(frame.IndexOf(sentinel, StringComparison.Ordinal), span.Offset);
        Assert.Equal("{\"authorization\":\"" + FrameRedaction.Marker + "\"}", result.Frame);
    }

    [Fact]
    public void SentinelAdjacentToAnother()
    {
        var sentinel = FrameRedaction.TestSentinel;
        var token = "ghp_" + new string('f', 36);
        var frame = "pre" + sentinel + token + "post";

        var result = FrameRedaction.Apply(frame, FrameRedaction.PatternsVersion);

        Assert.Equal("pre" + FrameRedaction.Marker + FrameRedaction.Marker + "post", result.Frame);
        Assert.DoesNotContain(sentinel, result.Frame, StringComparison.Ordinal);
        Assert.DoesNotContain(token, result.Frame, StringComparison.Ordinal);
        Assert.Equal(2, result.Redactions.Count);
        Assert.Equal(frame.IndexOf(sentinel, StringComparison.Ordinal), result.Redactions[0].Offset);
        Assert.Equal(sentinel.Length, result.Redactions[0].Len);
        Assert.Equal("test-sentinel", result.Redactions[0].PatternId);
        Assert.Equal(result.Redactions[0].Offset + FrameRedaction.Marker.Length, result.Redactions[1].Offset);
        AssertMarkersAtOffsets(result);
        Assert.Equal(token.Length, result.Redactions[1].Len);
        Assert.Equal("github-token", result.Redactions[1].PatternId);
    }

    [Fact]
    public void NoSentinel_IsIdenticalAndEmpty()
    {
        const string frame = "status=ok path=src/AiDe.Core/AgentPlane/RunEvent.cs";

        var result = FrameRedaction.Apply(frame, FrameRedaction.PatternsVersion);

        Assert.Equal(frame, result.Frame);
        Assert.Empty(result.Redactions);
    }

    [Fact]
    public void OverlappingPatterns_AreOneSpan()
    {
        var key = "sk-" + new string('b', 24);
        var bearer = "Bearer " + key;
        var frame = "Authorization: " + bearer + " end";

        var result = FrameRedaction.Apply(frame, FrameRedaction.PatternsVersion);

        var span = Assert.Single(result.Redactions);
        Assert.Equal(frame.IndexOf("Bearer", StringComparison.Ordinal), span.Offset);
        Assert.Equal(bearer.Length, span.Len);
        Assert.Equal("bearer", span.PatternId);
        AssertMarkersAtOffsets(result);
        Assert.Equal("Authorization: " + FrameRedaction.Marker + " end", result.Frame);
        Assert.DoesNotContain(key, result.Frame, StringComparison.Ordinal);
        Assert.DoesNotContain("Bearer", result.Frame, StringComparison.Ordinal);
    }

    [Fact]
    public void PatternsVersion_IsStamped()
    {
        Assert.Equal("patterns v1", FrameRedaction.PatternsVersion);

        var clean = FrameRedaction.Apply("plain", FrameRedaction.PatternsVersion);
        var hit = FrameRedaction.Apply(FrameRedaction.TestSentinel, FrameRedaction.PatternsVersion);

        Assert.Equal("patterns v1", clean.ScannedWith);
        Assert.Equal("patterns v1", hit.ScannedWith);
    }

    [Theory]
    [MemberData(nameof(Families))]
    public void EachCredentialFamily_IsRedacted(string secret, string patternId)
    {
        var frame = "<<" + secret + ">>";

        var result = FrameRedaction.Apply(frame, FrameRedaction.PatternsVersion);

        var span = Assert.Single(result.Redactions);
        Assert.Equal(2, span.Offset);
        Assert.Equal(secret.Length, span.Len);
        Assert.Equal(patternId, span.PatternId);
        AssertMarkersAtOffsets(result);
        Assert.Equal("<<" + FrameRedaction.Marker + ">>", result.Frame);
        Assert.DoesNotContain(secret, result.Frame, StringComparison.Ordinal);
    }

    public static IEnumerable<object[]> Families()
    {
        yield return ["sk-ant-api03-" + new string('a', 24), "anthropic-key"];
        yield return ["sk-" + new string('b', 24), "openai-key"];
        yield return ["sk-proj-" + new string('c', 24), "openai-key"];
        yield return ["xai-" + new string('d', 24), "xai-key"];
        yield return ["AIza" + new string('e', 35), "google-key"];
        yield return ["ghp_" + new string('f', 36), "github-token"];
        yield return ["gho_" + new string('f', 36), "github-token"];
        yield return ["github_pat_" + new string('g', 22), "github-fine-grained"];
        yield return ["Bearer " + new string('h', 16), "bearer"];
        yield return [FrameRedaction.TestSentinel, "test-sentinel"];
    }

    private static RunEventRedaction AssertReplaced(
        string frame, RedactedFrame result, string secret, string patternId)
    {
        Assert.DoesNotContain(secret, result.Frame, StringComparison.Ordinal);
        var span = Assert.Single(result.Redactions);
        Assert.Equal(frame.IndexOf(secret, StringComparison.Ordinal), span.Offset);
        Assert.Equal(secret.Length, span.Len);
        Assert.Equal(patternId, span.PatternId);
        AssertMarkersAtOffsets(result);
        return span;
    }

    private static void AssertMarkersAtOffsets(RedactedFrame result)
    {
        foreach (var r in result.Redactions)
        {
            Assert.Equal(
                FrameRedaction.Marker,
                result.Frame.Substring(r.Offset, FrameRedaction.Marker.Length));
        }
    }
}
