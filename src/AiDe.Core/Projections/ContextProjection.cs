using AiDe.Core.Extraction;

namespace AiDe.Core.Projections;

/// <summary>One context as the domain view draws it.</summary>
/// <param name="Crossings">
/// Edges leaving this context for another. The number that matters in a context map — a context with
/// no crossings is isolated, and one with hundreds is not bounded.
/// </param>
public sealed record ContextView(
    string Name,
    string? Description,
    int Symbols,
    int InternalEdges,
    int Crossings);

/// <summary>One relationship between contexts, and how much traffic it carries.</summary>
public sealed record ContextEdge(string From, string To, int Weight);

/// <summary>The domain view: contexts, what connects them, and what belongs to none.</summary>
public sealed record ContextMapView(
    IReadOnlyList<ContextView> Contexts,
    IReadOnlyList<ContextEdge> Edges,
    int UncoveredSymbols,
    IReadOnlyList<string> Problems)
{
    public bool IsValid => Problems.Count == 0;
}

/// <summary>
/// Groups the graph by declared bounded context (ADR-0016).
/// </summary>
/// <remarks>
/// <para><b>This is what makes the joins legible.</b> A <c>maps_to</c> edge inside one context is
/// unremarkable; the same edge crossing two contexts is a coupling someone chose, and until the
/// contexts are drawn there is no way to tell those apart.</para>
///
/// <para><b>Uncovered symbols are counted, never assigned.</b> Placing a symbol in "the nearest"
/// context would be inference dressed as a declaration — the exact thing ADR-0016 rejected folder
/// convention for.</para>
/// </remarks>
public sealed class ContextProjection(BoundedContextMap map, IReadOnlyList<Facts.EvidenceAssertion> assertions)
{
    public ContextMapView Compute()
    {
        if (!map.IsValid)
        {
            // An invalid map draws NOTHING. A partially-applied context map is a diagram that is
            // wrong in a way nobody can see, which is worse than an absent one.
            return new ContextMapView([], [], map.TotalSymbols, [.. map.Problems.Select(p => p.Message)]);
        }

        var relational = assertions
            .Where(a => !Facts.EvidencePredicates.Attributes.Contains(a.Predicate))
            .ToList();

        // Owners are resolved for subjects AND objects of relational edges. Resolving only subjects
        // left every node that appears solely as a target — which is most of them — outside every
        // context, so a crossing between two contexts was silently counted as no crossing at all.
        var owner = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var node in assertions.Select(a => a.Subject)
            .Concat(relational.Select(a => a.Object))
            .Distinct(StringComparer.Ordinal))
        {
            var context = map.Contexts.FirstOrDefault(c => c.Includes.Any(p => BoundedContextReader.Matches(p, node)));
            if (context is not null) owner[node] = context.Name;
        }

        var internalEdges = new Dictionary<string, int>(StringComparer.Ordinal);
        var crossings = new Dictionary<(string From, string To), int>();

        foreach (var edge in relational)
        {
            if (!owner.TryGetValue(edge.Subject, out var from)) continue;
            if (!owner.TryGetValue(edge.Object, out var to)) continue;

            if (string.Equals(from, to, StringComparison.Ordinal))
            {
                internalEdges[from] = internalEdges.GetValueOrDefault(from) + 1;
                continue;
            }

            // Direction is kept. "Editorial reads Football" and "Football reads Editorial" are
            // different statements about who depends on whom, and a context map that collapsed them
            // would hide the one thing it exists to show.
            crossings[(from, to)] = crossings.GetValueOrDefault((from, to)) + 1;
        }

        var views = map.Contexts.Select(c =>
        {
            var symbols = owner.Count(kv => string.Equals(kv.Value, c.Name, StringComparison.Ordinal));
            var outward = crossings.Where(kv => kv.Key.From == c.Name).Sum(kv => kv.Value);
            var inward = crossings.Where(kv => kv.Key.To == c.Name).Sum(kv => kv.Value);

            return new ContextView(
                c.Name, c.Description, symbols, internalEdges.GetValueOrDefault(c.Name), outward + inward);
        }).ToList();

        var edges = crossings
            .Select(kv => new ContextEdge(kv.Key.From, kv.Key.To, kv.Value))
            .OrderByDescending(e => e.Weight)
            .ToList();

        return new ContextMapView(views, edges, map.Uncovered.Count, []);
    }
}
