using AiDe.Core.Facts;
using AiDe.Core.Projections;

namespace AiDe.Core.Presentation;

/// <summary>
/// The complete state set for the evidence surface. Loading, Empty and Error are first-class here
/// because they are the states the urge to complete skips — and an unavailable result rendered as a
/// clean empty one is the specific dishonesty this product exists to avoid.
/// </summary>
public enum PaneState
{
    Loading,
    Empty,
    Ready,
    Stale,
    Error,
}

/// <summary>
/// A confidence badge that never relies on colour. Glyph and text carry the meaning; colour is the
/// third signal, so the badge still reads correctly in high-contrast mode and for a colour-blind
/// operator (WCAG 2.2 AA, "not colour alone").
/// </summary>
public sealed record ConfidenceBadge(string Glyph, string Text, string TokenName)
{
    public static ConfidenceBadge For(VerificationStatus status) => status switch
    {
        VerificationStatus.Verified => new ConfidenceBadge("✓", "Verified", "colors.verified"),
        VerificationStatus.Inferred => new ConfidenceBadge("~", "Inferred", "colors.inferred"),
        _ => new ConfidenceBadge("?", "Unverified", "colors.unverified"),
    };

    /// <summary>What a screen reader announces. Never just the colour name.</summary>
    public string AccessibleName => Text;
}

/// <summary>One row of the accessible evidence list — the permanent keyboard/screen-reader surface.</summary>
/// <param name="Evidence">
/// Why this row matched, when it matched on something other than its own identity — e.g.
/// <c>has_member = + addEventListener()</c>. Null when the id itself matched, where repeating it
/// would be noise.
/// </param>
/// <remarks>
/// The pane lists search results, and a search now matches attribute VALUES as well as identity. A
/// row that came back because one of its members matched is <b>correct</b> and reads as a wrong
/// result until it says so — the same defect fixed on the search surface, found here by enumerating
/// client records rather than by anybody noticing the pane.
/// </remarks>
public sealed record EvidenceRow(
    string NodeId,
    string DisplayLabel,
    string NodeKind,
    ConfidenceBadge Confidence,
    string? Evidence = null)
{
    public string AccessibleName => Evidence is null
        ? $"{DisplayLabel}, {NodeKind}, {Confidence.AccessibleName}"
        : $"{DisplayLabel}, {NodeKind}, matched on {Evidence}, {Confidence.AccessibleName}";
}

/// <summary>One section of the provenance pane, in the spec's fixed evidence order.</summary>
public sealed record ProvenanceSection(string Heading, IReadOnlyList<string> Lines);

/// <summary>
/// The Phase-1 evidence surface: a filterable list plus a provenance pane.
/// </summary>
/// <remarks>
/// This is not a fallback for a graph canvas — it is the accessibility equivalent the spec requires
/// to exist permanently, exposing the same selected-node identity, provenance, navigation actions and
/// result-limit state as the Phase-2 canvas will.
/// </remarks>
public sealed class EvidencePaneViewModel(IWorkspaceQueries queries)
{
    private IReadOnlyList<EvidenceRow> _allRows = [];

    public PaneState State { get; private set; } = PaneState.Loading;

    public IReadOnlyList<EvidenceRow> Rows { get; private set; } = [];

    public string? SelectedNodeId { get; private set; }

    public IReadOnlyList<ProvenanceSection> Provenance { get; private set; } = [];

    /// <summary>The one string the operator reads when something is off. Always states evidence, never reassurance.</summary>
    public string StatusMessage { get; private set; } = "Loading evidence…";

    public string? SourceRevision { get; private set; }

    /// <summary>Announced through a live region, so state changes reach a screen reader without motion.</summary>
    public string LiveAnnouncement { get; private set; } = string.Empty;

    public async Task LoadAsync(string searchTerm = "", CancellationToken cancellationToken = default)
    {
        State = PaneState.Loading;
        StatusMessage = "Loading evidence…";

        try
        {
            var result = await queries.FindAsync(
                searchTerm, ProjectionService.MaxNeighborsCeiling, cancellationToken);
            SourceRevision = result.SourceRevision;

            _allRows = [.. result.Matches.Select(m => new EvidenceRow(
                m.NodeId, m.DisplayLabel, m.NodeKind,
                ConfidenceBadge.For(VerificationStatus.Verified),
                // Only when the match was NOT on the id — otherwise the row would repeat itself.
                m.MatchedOn == Store.NodeMatchKind.Attribute ? m.Evidence : null))];

            Rows = _allRows;

            if (Rows.Count == 0)
            {
                State = PaneState.Empty;
                StatusMessage = "No evidence yet — this workspace has no committed snapshot.";
            }
            else
            {
                State = PaneState.Ready;
                StatusMessage = $"{Rows.Count} item(s) · rev {SourceRevision}";
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Degrade to an explicit failed state that keeps the last known rows visible; never
            // present an unreadable source as an empty success.
            State = PaneState.Error;
            StatusMessage = "Evidence unavailable — the workspace store could not be read.";
        }

        LiveAnnouncement = StatusMessage;
    }

    /// <summary>Marks the view stale without discarding it — the last successful revision still renders.</summary>
    public void MarkStale(string reason)
    {
        State = PaneState.Stale;
        StatusMessage = $"Graph is stale — {reason}. Viewing last successful snapshot.";
        LiveAnnouncement = StatusMessage;
    }

    public void Filter(string term)
    {
        Rows = string.IsNullOrWhiteSpace(term)
            ? _allRows
            : [.. _allRows.Where(r => r.DisplayLabel.Contains(term, StringComparison.OrdinalIgnoreCase))];

        if (Rows.Count == 0 && _allRows.Count > 0)
        {
            StatusMessage = $"No items match “{term}”.";
            LiveAnnouncement = StatusMessage;
        }
    }

    /// <summary>
    /// Selects a node and builds its provenance in the spec's fixed order:
    /// what it is → confidence/provenance → related nodes → source location → actions.
    /// </summary>
    public async Task SelectAsync(string nodeId, CancellationToken cancellationToken = default)
    {
        SelectedNodeId = nodeId;
        var describe = await queries.DescribeAsync(
            nodeId, ProjectionService.MaxNeighborsCeiling, cancellationToken);

        var sections = new List<ProvenanceSection>
        {
            new("What it is", [$"{describe.Node.DisplayLabel} ({describe.Node.NodeKind})"]),
        };

        if (describe.Neighbors.Count == 0)
        {
            sections.Add(new ProvenanceSection("Confidence and provenance", ["not recorded"]));
            sections.Add(new ProvenanceSection("Related nodes", ["No related evidence recorded."]));
            sections.Add(new ProvenanceSection("Source", ["not recorded"]));
        }
        else
        {
            sections.Add(new ProvenanceSection("Confidence and provenance",
                [.. describe.Neighbors.Select(n =>
                    $"{ConfidenceBadge.For(n.Status).Glyph} {ConfidenceBadge.For(n.Status).Text} · " +
                    $"{n.Origin} · {n.Provenance.ExtractorId} {n.Provenance.ExtractorVersion} · rev {n.ArtifactRevision}")]));

            sections.Add(new ProvenanceSection("Related nodes",
                [.. describe.Neighbors.Select(n => $"{n.Subject} —{n.Predicate}→ {n.Object}")]));

            sections.Add(new ProvenanceSection("Source",
                [.. describe.Neighbors
                    .Select(n => $"{n.Provenance.ArtifactPathId}:{n.Provenance.SourceLocation ?? "not recorded"}")
                    .Distinct(StringComparer.Ordinal)]));
        }

        // The limit state is part of the evidence, not a footnote: a truncated neighbourhood that
        // does not say so is indistinguishable from a complete one.
        if (describe.Bounds.OmittedEdges > 0 || describe.Bounds.ByteCapped)
        {
            sections.Add(new ProvenanceSection("Result limits",
                [$"Showing {describe.Bounds.ReturnedEdges} of " +
                 $"{describe.Bounds.ReturnedEdges + describe.Bounds.OmittedEdges} — " +
                 $"{describe.Bounds.OmittedEdges} omitted."]));
        }

        Provenance = sections;
        LiveAnnouncement = $"Selected {describe.Node.DisplayLabel}. {sections.Count} provenance sections.";
    }

    /// <summary>Empty-pane copy, shown before anything is selected.</summary>
    public static string EmptySelectionMessage => "Select an item to see its provenance.";
}
