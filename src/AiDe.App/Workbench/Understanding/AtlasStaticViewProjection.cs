using AiDe.Core.Understanding;

namespace AiDe.App.Workbench.Understanding;

public enum AtlasPresentationMode { Source, Class }

/// <summary>A display occurrence; its token and UTF-16 window remain Core-issued request inputs.</summary>
public sealed record AtlasStaticOccurrence(AtlasOutlineRowDto Declaration)
{
    public string Token => Declaration.DeclarationToken;
    public bool IsClassifier => Declaration.Structure is
        { Provenance: AtlasStructureProvenance.Extracted, ClassifierFlavor: not null };
    public bool IsClass => IsClassifier && Declaration.Structure!.ClassifierFlavor is
        AtlasClassifierFlavor.Class or AtlasClassifierFlavor.RecordClass;
    public string AccessibleName => $"{Declaration.Kind} {Declaration.DisplayName}; UTF-16 "
        + $"{Declaration.Span.Start} to {Declaration.Span.Start + Declaration.Span.Length}; "
        + $"{Declaration.Structure?.Provenance.ToString() ?? "Structure unavailable"}; "
        + $"{Declaration.Structure?.ParentState.ToString() ?? "parent not established"}; {Declaration.Structure?.Reason}";
    public override string ToString() => AccessibleName;
}

/// <summary>One current outline page, never a project graph or a cache of previous source bodies.</summary>
public sealed record AtlasStaticViewProjection(
    IReadOnlyList<AtlasStaticOccurrence> Occurrences,
    IReadOnlyList<AtlasStaticOccurrence> Classifiers,
    AtlasStaticOccurrence? Classifier,
    IReadOnlyList<AtlasStaticOccurrence> Members,
    string Status,
    string Bounds,
    string PageContext,
    bool CanNavigate)
{
    public const string ProfileDisclosure = "File-limited — project and target framework not established";
    public const string RelationshipDisclosure = "Base types not resolved in this profile. Calls and lifetime relationships are not established.";
    public const string PageDisclosure = "More declarations outside this page.";

    public static AtlasStaticViewProjection Create(AtlasSelectionDto selection, string? classifierToken = null)
    {
        ArgumentNullException.ThrowIfNull(selection);
        var rows = selection.Outline.Select(row => new AtlasStaticOccurrence(row))
            .OrderBy(row => row.Declaration.Span.Start)
            .ThenBy(row => row.Token, StringComparer.Ordinal).ToArray();
        var byToken = rows.ToDictionary(row => row.Token, StringComparer.Ordinal);
        foreach (var row in rows)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal) { row.Token };
            var current = row;
            while (current.Declaration.Structure is { ParentState: AtlasLexicalParentState.Present } structure)
            {
                if (structure.ParentDeclarationToken is null || !byToken.TryGetValue(structure.ParentDeclarationToken, out var parent)
                    || !seen.Add(parent.Token))
                    throw new ArgumentException("ATLAS-STATIC-PARENT: invalid current-page parent relation.", nameof(selection));
                current = parent;
            }
        }

        var classifiers = rows.Where(row => row.IsClassifier).ToArray();
        var selected = rows.FirstOrDefault(row => row.Token == selection.DeclarationToken);
        var preferred = classifierToken ?? (selected?.IsClassifier == true ? selected.Token
            : selected?.Declaration.Structure is { ParentState: AtlasLexicalParentState.Present } selectedParent
                ? selectedParent.ParentDeclarationToken : null);
        var classifier = classifiers.FirstOrDefault(row => row.Token == preferred) ?? classifiers.FirstOrDefault();
        var members = classifier is null ? [] : rows.Where(row => row.Declaration.Structure is
                { Provenance: AtlasStructureProvenance.Extracted, ParentState: AtlasLexicalParentState.Present } parent
                && parent.ParentDeclarationToken == classifier.Token).ToArray();
        var ready = selection.Source.State == SourceProjectionState.IndexedMatch && selection.Source.BindingToken is not null;
        var status = selection.Source.State switch
        {
            SourceProjectionState.Changed => "Source changed — refresh required",
            SourceProjectionState.Refused => "Source access refused",
            SourceProjectionState.Canceled => "Source request canceled",
            _ when !ready => $"Source {selection.Source.State}; source navigation unavailable.",
            _ when selection.OutlineState != AtlasOutlineState.Available =>
                $"Outline {selection.OutlineState}: {selection.OutlineReason}",
            _ when rows.Length == 0 => "No supported declarations on this page.",
            _ when rows.All(row => row.Declaration.Structure is null
                || row.Declaration.Structure.Provenance == AtlasStructureProvenance.Unavailable) =>
                "Structure unavailable. This reader returned no admitted structural metadata.",
            _ when classifier is null => "No classifier on this page.",
            _ => classifier.IsClass ? "Class occurrence — extracted lexical declarations." : "Classifier occurrence — not a UML class.",
        };
        var outside = selection.OutlineNextOffset.HasValue || selection.OutlineBounds.DenominatorState != AtlasDenominatorState.Known
            || selection.OutlineBounds.DenominatorValue > rows.Length || selection.OutlineBounds.OmissionReason is not null;
        var pageContext = outside ? PageDisclosure : "Current retained page; unsupported declarations are not established.";
        return new(Array.AsReadOnly(rows), Array.AsReadOnly(classifiers), classifier, Array.AsReadOnly(members),
            status, DescribeBounds(selection.OutlineBounds) + " " + pageContext, pageContext,
            ready && selection.OutlineState == AtlasOutlineState.Available);
    }

    public static string DescribeBounds(AtlasBoundsDto bounds) =>
        $"{bounds.Dimension}; request {bounds.RequestedLimit}; effective {bounds.EffectiveLimit}; rows {bounds.ReturnedRows}; "
        + $"content bytes {bounds.ReturnedContentBytes}; total {bounds.DenominatorState} {bounds.DenominatorValue} "
        + $"({bounds.DenominatorReason}); omission {bounds.OmissionDimension} {bounds.OmissionReason}.";
}
