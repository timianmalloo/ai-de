using System.Text;
using System.Text.RegularExpressions;

namespace AiDe.Core.AgentPlane;

/// <summary>
/// Ingest-side credential-sentinel scan (ADR-0042 §6). Pure over the frame text and the
/// pattern-set version: the same inputs always return the same redacted frame and spans.
/// </summary>
/// <remarks>
/// <para>Offsets and lengths are indexes into the <b>original</b> frame. A marker shorter or
/// longer than the secret would make a later span lie if it were measured on the redacted
/// text, and the caller has to be able to name the bytes that were removed.</para>
/// <para>v1 scans the raw frame text and does not decode JSON string values. That decode is
/// the mapper's job (ADR-0042, consequences). Overlapping matches collapse to one span; the
/// id kept is the match that starts earliest, then the longest, then the earlier pattern.
/// Adjacent matches stay two spans. The marker is fixed and contains none of the secret.</para>
/// </remarks>
public static class FrameRedaction
{
    /// <summary>The only pattern set this assembly scans with. Stamped on every result.</summary>
    public const string PatternsVersion = "patterns v1";

    /// <summary>Fixed replacement. Matches no v1 pattern, so a second scan does not grow.</summary>
    public const string Marker = "[REDACTED]";

    /// <summary>
    /// Sentinel the tests plant. No credential-shaped sentinel literal exists under src/ or the
    /// fixture corpus, so v1 defines this one string and the tests are the fixtures that use it.
    /// </summary>
    public const string TestSentinel = "aide-sentinel-credential-v1";

    /// <summary>
    /// Replace every v1 credential span in <paramref name="frame"/>.
    /// Fails if <paramref name="patternsVersion"/> is not <see cref="PatternsVersion"/>:
    /// stamping a version that did not scan would record a scan that did not happen.
    /// </summary>
    public static RedactedFrame Apply(string frame, string patternsVersion)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(patternsVersion);
        if (!string.Equals(patternsVersion, PatternsVersion, StringComparison.Ordinal))
        {
            throw new ArgumentOutOfRangeException(
                nameof(patternsVersion),
                patternsVersion,
                "Only " + PatternsVersion + " is defined.");
        }

        var hits = new List<Hit>();
        var patterns = Patterns;
        for (var p = 0; p < patterns.Length; p++)
        {
            foreach (Match match in patterns[p].Expression.Matches(frame))
            {
                if (match.Length == 0)
                    continue;
                hits.Add(new Hit(match.Index, match.Length, patterns[p].Id, p));
            }
        }

        hits.Sort(static (a, b) =>
        {
            var byStart = a.Offset.CompareTo(b.Offset);
            if (byStart != 0)
                return byStart;
            var byLength = b.Length.CompareTo(a.Length);
            if (byLength != 0)
                return byLength;
            return a.Order.CompareTo(b.Order);
        });

        var merged = new List<Hit>(hits.Count);
        foreach (var hit in hits)
        {
            if (merged.Count == 0)
            {
                merged.Add(hit);
                continue;
            }

            var last = merged[^1];
            var lastEnd = last.Offset + last.Length;
            // Touching spans are adjacent, not overlapping: [0, 5) and [5, 9) stay two spans.
            if (hit.Offset < lastEnd)
            {
                var end = Math.Max(lastEnd, hit.Offset + hit.Length);
                merged[^1] = last with { Length = end - last.Offset };
            }
            else
            {
                merged.Add(hit);
            }
        }

        var builder = new StringBuilder(frame.Length);
        var cursor = 0;
        var redactions = new RunEventRedaction[merged.Count];
        for (var i = 0; i < merged.Count; i++)
        {
            var span = merged[i];
            builder.Append(frame, cursor, span.Offset - cursor);
            builder.Append(Marker);
            cursor = span.Offset + span.Length;
            redactions[i] = new RunEventRedaction(span.Offset, span.Length, span.PatternId);
        }

        builder.Append(frame, cursor, frame.Length - cursor);
        return new RedactedFrame(builder.ToString(), redactions, PatternsVersion);
    }

    private readonly record struct Hit(int Offset, int Length, string PatternId, int Order);

    private readonly record struct Pattern(string Id, Regex Expression);

    private static readonly Pattern[] Patterns = CreatePatterns();

    private static Pattern[] CreatePatterns()
    {
        const RegexOptions options = RegexOptions.CultureInvariant | RegexOptions.Compiled;
        var timeout = TimeSpan.FromSeconds(1);

        // No word-boundary. A sentinel glued to another token, or to ordinary letters, is still a secret.
        Regex Compile(string body) => new(body, options, timeout);

        return
        [
            new("anthropic-key", Compile(@"sk-ant-[A-Za-z0-9_\-]{24,}")),
            // (?!ant-) keeps a widened body class from also swallowing an Anthropic key.
            new("openai-key", Compile(@"sk-(?:proj-|svcacct-|admin-)?(?!ant-)[A-Za-z0-9]{20,}")),
            new("xai-key", Compile(@"xai-[A-Za-z0-9]{20,}")),
            new("google-key", Compile(@"AIza[0-9A-Za-z_\-]{35}")),
            new("github-token", Compile(@"(?:ghp|gho|ghu|ghs|ghr)_[A-Za-z0-9]{36}")),
            new("github-fine-grained", Compile(@"github_pat_[A-Za-z0-9_]{22,}")),
            new("bearer", Compile(@"Bearer [A-Za-z0-9\-._~+/=]{8,}")),
            new("test-sentinel", Compile(Regex.Escape(TestSentinel))),
        ];
    }
}

/// <summary>The redacted frame, the original-frame spans, and the pattern set that scanned it.</summary>
/// <param name="Frame">The frame with each secret span replaced by <see cref="FrameRedaction.Marker"/>.</param>
/// <param name="Redactions">Empty when the scan matched nothing. Never null: this function always scans.</param>
/// <param name="ScannedWith"><see cref="FrameRedaction.PatternsVersion"/>.</param>
public sealed record RedactedFrame(
    string Frame,
    IReadOnlyList<RunEventRedaction> Redactions,
    string ScannedWith);
