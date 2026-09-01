using System.Collections.Generic;

namespace AiDe.App.Workbench;

/// <summary>Which viewer an "Open as…" action opens for a node.</summary>
public enum NodeViewKind
{
    Source,
    ClassDiagram,
    Sequence,
    Metadata,
    GraphNeighbourhood,
    Read,
}

/// <summary>One contextual "Open as…" choice for a node — the verb and its menu label.</summary>
public sealed record NodeViewOption(NodeViewKind Kind, string Label);

/// <summary>
/// The IntelliJ-style contextual "Open as…" grammar (smoke 9-1 §3): a diagram/viewer is a *view*
/// opened from an entry point in the model, not a thing created blind. Right-clicking any node offers
/// exactly the viewers its TYPE supports — source and a class diagram for a type, a sequence diagram
/// for a method, "read" for a document.
/// </summary>
/// <remarks>
/// Type-driven from the producer's signal (<c>has_type</c> kind + the authoritative <c>IsKnowledge</c>
/// flag), never a spelling guess that fails across repositories — the DC-042 lesson, just fixed for
/// the Knowledge chip. Pure and dependency-free so the mapping is unit-tested off the UI thread.
/// </remarks>
public static class NodeViewMenu
{
    /// <summary>The viewers this node supports, most specific first. Never empty.</summary>
    public static IReadOnlyList<NodeViewOption> OptionsFor(string? nodeKind, bool isKnowledge)
    {
        var k = (nodeKind ?? string.Empty).ToLowerInvariant();

        // A document — read it. The authoritative flag wins over the kind spelling (DC-042).
        if (isKnowledge || IsKnowledgeKind(k))
        {
            return new List<NodeViewOption>
            {
                new(NodeViewKind.Read, "Read document"),
                new(NodeViewKind.Metadata, "Metadata & edges"),
                new(NodeViewKind.GraphNeighbourhood, "Reveal in graph"),
            };
        }

        // A method/function — its source and the sequence of calls it makes.
        if (IsMember(k))
        {
            return new List<NodeViewOption>
            {
                new(NodeViewKind.Source, "View source"),
                new(NodeViewKind.Sequence, "Sequence diagram"),
                new(NodeViewKind.Metadata, "Metadata & edges"),
            };
        }

        // A type — its source, its class diagram, its sequence of outgoing calls, its neighbourhood.
        if (IsType(k))
        {
            return new List<NodeViewOption>
            {
                new(NodeViewKind.Source, "View source"),
                new(NodeViewKind.ClassDiagram, "Class diagram"),
                new(NodeViewKind.Sequence, "Sequence diagram"),
                new(NodeViewKind.Metadata, "Metadata & edges"),
                new(NodeViewKind.GraphNeighbourhood, "Reveal in graph"),
            };
        }

        // A data shape — its DDL/source and its neighbourhood.
        if (IsData(k))
        {
            return new List<NodeViewOption>
            {
                new(NodeViewKind.Source, "View source"),
                new(NodeViewKind.Metadata, "Metadata & edges"),
                new(NodeViewKind.GraphNeighbourhood, "Reveal in graph"),
            };
        }

        // Anything else (external/BCL, azure-resource, unknown): what always works, and nothing that
        // would open an empty viewer for a node that cannot fill it.
        return new List<NodeViewOption>
        {
            new(NodeViewKind.Metadata, "Metadata & edges"),
            new(NodeViewKind.GraphNeighbourhood, "Reveal in graph"),
        };
    }

    private static bool IsType(string k) =>
        k is "class" or "interface" or "record" or "struct" or "enum" or "type";

    private static bool IsMember(string k) =>
        k is "method" or "function" or "property" or "field" or "constructor" or "member";

    private static bool IsData(string k) =>
        k is "table" or "column" or "schema" or "view" or "index" || k.StartsWith("sql");

    private static bool IsKnowledgeKind(string k) =>
        k is "knowledge" or "doc" or "adr" or "design" or "note" or "decision-note" or "spec"
            or "markdown" or "html" or "proof" or "glossary" or "investigation" or "lesson"
            or "requirement" or "acceptance";
}
