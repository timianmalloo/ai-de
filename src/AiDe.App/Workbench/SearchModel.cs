using System.Collections.Generic;
using System.Linq;

namespace AiDe.App.Workbench;

/// <summary>What kind of thing a search hit points at. Governs grouping order in the results list.</summary>
public enum SearchResultKind
{
    Type,
    Member,
    File,
    Node,
    Command,
    Other,
}

/// <summary>
/// One breadth-search hit. <see cref="Id"/> is opaque and belongs to the provider (a node id, a
/// file path, a command id) — the surface hands it back verbatim to the navigate action so the
/// provider decides what "go there" means.
/// </summary>
public sealed record SearchResult(string Id, SearchResultKind Kind, string Label, string Detail = "");

/// <summary>A named, ordered bucket of hits of one kind, for a grouped results view.</summary>
public sealed record SearchGroup(SearchResultKind Kind, string Header, IReadOnlyList<SearchResult> Results);

/// <summary>
/// Pure logic for the breadth-search surface (app-search-breadth): grouping hits by kind in a stable
/// order so the results list reads the same way every time, independent of the order the provider
/// returned them.
/// </summary>
/// <remarks>
/// <b>Scaffold.</b> The hits themselves come from a Core search index that does not exist yet; this
/// model shapes whatever a provider returns. Kept pure and dependency-free so it is unit-testable
/// off the UI thread, mirroring <see cref="SequenceModel"/> and <see cref="ClassHierarchyModel"/>.
/// </remarks>
public static class SearchModel
{
    /// <summary>The order kinds appear in the grouped results — most specific first.</summary>
    private static readonly SearchResultKind[] Order =
    [
        SearchResultKind.Type,
        SearchResultKind.Member,
        SearchResultKind.File,
        SearchResultKind.Node,
        SearchResultKind.Command,
        SearchResultKind.Other,
    ];

    /// <summary>
    /// Groups <paramref name="results"/> by kind in <see cref="Order"/>, dropping empty groups and
    /// preserving each provider's order within a group. A null or empty input yields no groups.
    /// </summary>
    public static IReadOnlyList<SearchGroup> Group(IReadOnlyList<SearchResult>? results)
    {
        if (results is null || results.Count == 0)
        {
            return new List<SearchGroup>();
        }

        var byKind = results
            .GroupBy(r => r.Kind)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<SearchResult>)g.ToList());

        var groups = new List<SearchGroup>();
        foreach (var kind in Order)
        {
            if (byKind.TryGetValue(kind, out var hits) && hits.Count > 0)
            {
                groups.Add(new SearchGroup(kind, Header(kind, hits.Count), hits));
            }
        }

        return groups;
    }

    /// <summary>Total hit count across a set of results (null-safe).</summary>
    public static int Count(IReadOnlyList<SearchResult>? results) => results?.Count ?? 0;

    private static string Header(SearchResultKind kind, int count)
    {
        var label = kind switch
        {
            SearchResultKind.Type => "Types",
            SearchResultKind.Member => "Members",
            SearchResultKind.File => "Files",
            SearchResultKind.Node => "Graph nodes",
            SearchResultKind.Command => "Commands",
            _ => "Other",
        };

        return $"{label} ({count})";
    }
}
