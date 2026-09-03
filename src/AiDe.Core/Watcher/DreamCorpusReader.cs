using System.Text.Json;
using System.Text.RegularExpressions;

namespace AiDe.Core.Watcher;

/// <summary>
/// What the offline Dream has already promoted, so Daydream stops re-proposing it.
/// </summary>
/// <remarks>
/// <see cref="Present"/> is the honest part: <c>false</c> means the AI-Forward Pack's corpus was not
/// found, which is different from finding it empty. A repository without the pack is the normal
/// case, not a failure, and the two must never render alike.
/// </remarks>
public sealed record DreamCorpus(bool Present, IReadOnlySet<string> KnownLearnings, string Source)
{
    /// <summary>The corpus for a repository that has no pack — an absence, stated.</summary>
    public static DreamCorpus Absent { get; } =
        new(false, new HashSet<string>(StringComparer.OrdinalIgnoreCase), "not recorded — no corpus found");

    /// <summary>
    /// Whether a candidate has already been promoted, by any route.
    /// </summary>
    /// <remarks>
    /// Matched on the signature's own words appearing in a promoted learning's text. Deliberately
    /// loose in the direction of <b>suppressing a duplicate proposal</b> rather than making a
    /// claim: a false match costs a candidate that a human can still find on the surface, and a
    /// false miss costs a re-proposal of something already known. Neither is a correctness failure,
    /// which is why this is allowed to be a heuristic where nothing else in Daydream is.
    /// </remarks>
    public bool AlreadyKnown(DaydreamSignature signature)
    {
        ArgumentNullException.ThrowIfNull(signature);
        if (!Present || KnownLearnings.Count == 0)
        {
            return false;
        }

        var terms = new[] { signature.Floors, signature.Shortfalls }
            .Where(t => t.Length > 0)
            .SelectMany(t => t.Split('+', StringSplitOptions.RemoveEmptyEntries))
            .Select(t => t.Split(':')[0])
            .Where(t => t.Length > 3)
            .ToList();

        // Every term, not any: one shared word between a floor name and a paragraph of prose is a
        // coincidence, and suppressing a candidate on a coincidence hides the thing being proposed.
        return terms.Count > 0 && KnownLearnings.Any(
            learning => terms.All(t => learning.Contains(t, StringComparison.OrdinalIgnoreCase)));
    }
}

/// <summary>
/// Reads the AI-Forward Pack's promoted corpus, when a repository has one.
/// </summary>
/// <remarks>
/// <para><b>Detected, never assumed, and read-only.</b> AI-DE requires nothing of the pack. This
/// looks for two plain files a repository may or may not have, and reports their absence as an
/// absence. It never invokes <c>dream.py</c>: shelling out would make Python and a vendored pack a
/// runtime dependency of the product, which is the inversion
/// <c>design-watcher-daydream-dream-seam</c> exists to refuse.</para>
///
/// <para><b>Why these two files and not an inbox.</b> A spike on 2026-09-02 read
/// <c>dream.py</c>'s <c>load_corpus</c> and found it reads five FIXED paths with no discovery and no
/// extension point — falsifying the original seam design, which had proposed emitting into an
/// inbox the script would have to have been taught to read. These two are what it actually
/// maintains, so they are what can be read back.</para>
/// </remarks>
public static class DreamCorpusReader
{
    // "### DC-042 — a heading" : the register's own shape, and the only structure relied on.
    private static readonly Regex DefectClassHeading = new(
        @"^###\s+(?<id>[A-Z][A-Z0-9-]*)\s*[—-]\s*(?<title>.+)$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    /// <summary>Reads the corpus rooted at a repository, or reports its absence.</summary>
    public static DreamCorpus Read(string? repositoryRoot)
    {
        if (string.IsNullOrWhiteSpace(repositoryRoot) || !Directory.Exists(repositoryRoot))
        {
            return DreamCorpus.Absent;
        }

        var register = Path.Combine(repositoryRoot, "docs", "lessons", "defect-classes.md");
        var mitigations = Path.Combine(repositoryRoot, "docs", "lessons", "mitigations.jsonl");

        var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sources = new List<string>();

        if (TryRead(register, out var registerText))
        {
            foreach (Match m in DefectClassHeading.Matches(registerText))
            {
                known.Add(m.Groups["id"].Value + " " + m.Groups["title"].Value);
            }

            sources.Add("defect-classes.md");
        }

        if (TryRead(mitigations, out var mitigationText))
        {
            foreach (var line in mitigationText.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0)
                {
                    continue;
                }

                try
                {
                    using var doc = JsonDocument.Parse(trimmed);
                    var summary = doc.RootElement.TryGetProperty("summary", out var s) ? s.GetString() : null;
                    var klass = doc.RootElement.TryGetProperty("class", out var c) ? c.GetString() : null;
                    if (!string.IsNullOrWhiteSpace(summary))
                    {
                        known.Add((klass ?? string.Empty) + " " + summary);
                    }
                }
                catch (JsonException)
                {
                    // A malformed line is skipped, never fatal: this is someone else's file and a
                    // hand edit in it must not stop Daydream reading the rest.
                }
            }

            sources.Add("mitigations.jsonl");
        }

        return sources.Count == 0
            ? DreamCorpus.Absent
            : new DreamCorpus(true, known, string.Join(" + ", sources));
    }

    private static bool TryRead(string path, out string text)
    {
        try
        {
            if (File.Exists(path))
            {
                text = File.ReadAllText(path);
                return true;
            }
        }
        catch (IOException)
        {
            // Unreadable is NOT absent. Reported as absent here would claim the repository has no
            // corpus when it has one this process could not open — so it falls through to the same
            // "not present" answer, and the caller's guarantee is only ever "nothing suppressed".
        }
        catch (UnauthorizedAccessException)
        {
        }

        text = string.Empty;
        return false;
    }
}
