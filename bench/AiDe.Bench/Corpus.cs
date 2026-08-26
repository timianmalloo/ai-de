using AiDe.Core.Facts;

namespace AiDe.Bench;

/// <summary>
/// The approved Phase-1 benchmark corpus, generated deterministically from a fixed seed so every
/// run measures the same graph and two runs are comparable.
/// </summary>
/// <remarks>
/// The architecture states the corpus as "10,000-assertion / 50,000-edge". In this fact model one
/// assertion IS one edge, so those are two different numbers for the same thing. This harness reads
/// them as the two budgets they actually gate:
/// <list type="bullet">
/// <item><b>Refresh budget</b> (p95 &lt; 500 ms) — committing one 10,000-assertion scope snapshot.</item>
/// <item><b>Query budgets</b> (describe p95 &lt; 100 ms, impact p95 &lt; 250 ms) — against the full
/// 50,000-edge corpus.</item>
/// </list>
/// The interpretation is recorded here rather than in a commit message because a benchmark whose
/// corpus definition is ambiguous cannot be re-run to mean the same thing.
/// </remarks>
internal static class Corpus
{
    internal const string Revision = "bench-corpus-v1";

    internal const int TotalEdges = 50_000;
    internal const int RefreshScopeAssertions = 10_000;
    internal const int DistinctNodes = 10_000;

    /// <summary>
    /// Builds a graph with realistic shape rather than a uniform mesh: a few high-degree hubs, a
    /// long tail of low-degree nodes, and a deep chain. A uniform random graph would flatter the
    /// traversal — real impact queries hit hubs, and hubs are where a bounded walk earns its bounds.
    /// </summary>
    internal static List<EvidenceAssertion> Build(string scopeId, int edgeCount, int seed = 20260826)
    {
        var random = new Random(seed);
        var observedAt = new DateTimeOffset(2026, 8, 26, 0, 0, 0, TimeSpan.Zero);
        var assertions = new List<EvidenceAssertion>(edgeCount);
        var seen = new HashSet<(string, string, string)>();

        // A deep chain guarantees the traversal has somewhere far to go, so a depth-bounded walk is
        // actually exercised instead of terminating early.
        var chainLength = Math.Min(500, edgeCount / 10);
        for (var i = 0; i < chainLength; i++)
        {
            Add($"Chain{i:D5}", "depends_on", $"Chain{i + 1:D5}");
        }

        // 20 hubs absorb a large share of the edges, mirroring the framework/service types that
        // dominate a real dependency graph.
        const int hubCount = 20;
        while (assertions.Count < edgeCount)
        {
            var useHub = random.Next(100) < 35;
            var subject = useHub
                ? $"Hub{random.Next(hubCount):D2}"
                : $"Node{random.Next(DistinctNodes):D5}";
            var target = $"Node{random.Next(DistinctNodes):D5}";
            if (subject == target)
            {
                continue;
            }

            var predicate = (random.Next(4)) switch
            {
                0 => "depends_on",
                1 => "persisted_in",
                2 => "calls",
                _ => "references",
            };

            Add(subject, predicate, target);
        }

        return assertions;

        void Add(string subject, string predicate, string target)
        {
            if (assertions.Count >= edgeCount || !seen.Add((subject, predicate, target)))
            {
                return;
            }

            // A third of the corpus is Inferred, so the weakest-status fold is exercised at scale
            // rather than on a uniformly Verified graph that would never trigger it.
            var status = assertions.Count % 3 == 0 ? VerificationStatus.Inferred : VerificationStatus.Verified;

            assertions.Add(new EvidenceAssertion(
                scopeId, Revision, subject, predicate, target,
                EvidenceOrigin.Static, status,
                new Provenance($"src/{subject}.cs", $"{assertions.Count % 400 + 1}:1",
                    "bench-extractor", "1.0.0", observedAt)));
        }
    }

    /// <summary>The hub a query benchmark should target: the worst realistic case, not the average one.</summary>
    internal const string HotNode = "Hub00";

    /// <summary>The head of the deep chain, for a traversal that must actually walk.</summary>
    internal const string ChainHead = "Chain00000";
}
