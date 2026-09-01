using AiDe.Core.Presentation;

namespace AiDe.App.Workbench;

/// <summary>A UML relationship kind we derive from the graph (ADR-0020).</summary>
public enum ClassRelationKind
{
    /// <summary>`inherits` — a subclass to its base class (UML generalization, solid hollow triangle).</summary>
    Generalization,

    /// <summary>`implements` — a class to an interface (UML realization, dashed hollow triangle).</summary>
    Realization,

    /// <summary>`depends_on` — a using dependency (UML dependency, dashed line, open arrowhead).</summary>
    Dependency,
}

/// <summary>A type in the class diagram — a class or interface. Members are not available yet (ADR-0020).</summary>
public sealed record ClassTypeNode(string Id, string Label, bool IsInterface, string? Context);

/// <summary>One generalization/realization edge between two types in the diagram.</summary>
public sealed record ClassRelation(string From, string To, ClassRelationKind Kind);

/// <summary>
/// The class-hierarchy view model (ADR-0020): the classes/interfaces and their generalization/
/// realization relationships, projected from the graph the App already holds. A pure function so the
/// projection is verifiable headlessly. Member-less by construction — no extractor emits members yet;
/// the surface says so rather than implying empty classes.
/// </summary>
public sealed record ClassHierarchy(
    IReadOnlyList<ClassTypeNode> Types,
    IReadOnlyList<ClassRelation> Relations,
    int ExternalRelations,
    IReadOnlyList<ClassRelation>? Dependencies = null)
{
    public bool IsEmpty => Types.Count == 0;

    /// <summary>UML dependency edges (`depends_on`), kept separate from the inheritance relations so
    /// they never affect the generalization ranking/layout; drawn only when the user asks.</summary>
    public IReadOnlyList<ClassRelation> Deps => Dependencies ?? [];
}

/// <summary>Builds a <see cref="ClassHierarchy"/> from graph nodes and edges (ADR-0020 Phase 1).</summary>
public static class ClassHierarchyModel
{
    // The fine has_type values that are a "type" for a class diagram. Enums are excluded (they are not
    // classes/interfaces in the generalization sense); azure/table/etc. are not types.
    private static readonly HashSet<string> TypeKinds = new(StringComparer.OrdinalIgnoreCase)
    {
        "class", "interface", "struct", "record",
        "python-class", "typescript-class", "typescript-interface",
    };

    private static bool IsType(string? kind) => kind is not null && TypeKinds.Contains(kind);

    private static bool IsInterface(string? kind) =>
        kind is not null && kind.Contains("interface", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Projects the class hierarchy. Keeps only class/interface nodes; keeps `inherits` (generalization)
    /// and `implements` (realization) edges whose BOTH endpoints are kept types (the internal hierarchy);
    /// counts relations whose target is not a kept type as <c>ExternalRelations</c> (a base class or
    /// interface outside the analysed scope — an honest disclosure, not drawn).
    /// </summary>
    public static ClassHierarchy Build(
        IReadOnlyList<CanvasNode>? nodes, IReadOnlyList<CanvasEdge>? edges)
    {
        nodes ??= [];
        edges ??= [];

        var types = nodes
            .Where(n => IsType(n.Kind))
            .GroupBy(n => n.Id, StringComparer.Ordinal)
            .Select(g => g.First())
            .Select(n => new ClassTypeNode(n.Id, n.Label, IsInterface(n.Kind), n.Context))
            .ToList();

        var ids = new HashSet<string>(types.Select(t => t.Id), StringComparer.Ordinal);

        var relations = new List<ClassRelation>();
        var seen = new HashSet<(string, string, ClassRelationKind)>();
        var external = 0;

        var deps = new List<ClassRelation>();
        var depSeen = new HashSet<(string, string)>();

        foreach (var e in edges)
        {
            // Dependencies (`depends_on`) are collected separately — they are NOT part of the
            // generalization hierarchy and must never affect ranking/layout. Both endpoints must be
            // drawn types; self-edges are dropped; deduped.
            if (e.Predicate == "depends_on")
            {
                if (e.From != e.To
                    && ids.Contains(e.From) && ids.Contains(e.To)
                    && depSeen.Add((e.From, e.To)))
                {
                    deps.Add(new ClassRelation(e.From, e.To, ClassRelationKind.Dependency));
                }
                continue;
            }

            var kind = e.Predicate switch
            {
                "inherits" => ClassRelationKind.Generalization,
                "implements" => ClassRelationKind.Realization,
                _ => (ClassRelationKind?)null,
            };
            if (kind is null) { continue; }

            // The relation must ORIGINATE from a type we are drawing.
            if (!ids.Contains(e.From)) { continue; }

            // A target outside the analysed scope (external base type / interface) is disclosed, not drawn.
            if (!ids.Contains(e.To)) { external++; continue; }

            var key = (e.From, e.To, kind.Value);
            if (seen.Add(key))
            {
                relations.Add(new ClassRelation(e.From, e.To, kind.Value));
            }
        }

        // A dependency that duplicates an inheritance/realization edge is redundant noise — drop it.
        var related = relations.Select(r => (r.From, r.To)).ToHashSet();
        deps = deps.Where(d => !related.Contains((d.From, d.To))).ToList();

        return new ClassHierarchy(types, relations, external, deps);
    }

    /// <summary>
    /// Filters a hierarchy to types whose label contains <paramref name="term"/> (case-insensitive),
    /// keeping only relations whose BOTH endpoints survive; relations to a filtered-out (or external)
    /// target are recounted as external. An empty/whitespace term returns the hierarchy unchanged.
    /// Pure, so the filter is verifiable headlessly.
    /// </summary>
    public static ClassHierarchy Filter(ClassHierarchy hierarchy, string? term)
    {
        if (string.IsNullOrWhiteSpace(term)) { return hierarchy; }

        var t = term.Trim();
        var kept = hierarchy.Types
            .Where(x => x.Label.Contains(t, StringComparison.OrdinalIgnoreCase)
                || x.Id.Contains(t, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var ids = new HashSet<string>(kept.Select(x => x.Id), StringComparer.Ordinal);

        var relations = new List<ClassRelation>();
        var external = 0;
        foreach (var r in hierarchy.Relations)
        {
            if (!ids.Contains(r.From)) { continue; }         // source filtered out — the relation goes with it
            if (!ids.Contains(r.To)) { external++; continue; } // target filtered out — disclosed, not drawn
            relations.Add(r);
        }

        var deps = hierarchy.Deps
            .Where(d => ids.Contains(d.From) && ids.Contains(d.To))
            .ToList();

        return new ClassHierarchy(kept, relations, external, deps);
    }
}
