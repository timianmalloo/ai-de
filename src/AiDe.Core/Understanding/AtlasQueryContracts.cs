using System.Collections.Immutable;

namespace AiDe.Core.Understanding;

/// <summary>Directory enumeration limits. Candidate ceilings are policy defaults, not measured guarantees.</summary>
public sealed class EnumerationLimits
{
    public EnumerationLimits(int maxEntries, int maxDepth, int maxDescriptors, TimeSpan timeout)
    {
        MaxEntries = Positive(maxEntries, nameof(maxEntries));
        MaxDepth = Positive(maxDepth, nameof(maxDepth));
        MaxDescriptors = Positive(maxDescriptors, nameof(maxDescriptors));
        Timeout = timeout > TimeSpan.Zero ? timeout : throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Timeout must be positive.");
    }

    public int MaxEntries { get; }
    public int MaxDepth { get; }
    public int MaxDescriptors { get; }
    public TimeSpan Timeout { get; }
    public static EnumerationLimits CandidateDefault { get; } = new(25_000, 64, 128, TimeSpan.FromSeconds(30));

    private static int Positive(int value, string name) =>
        value > 0 ? value : throw new ArgumentOutOfRangeException(name, value, "Value must be positive.");
}

/// <summary>Bounded page request for Atlas query ports.</summary>
public sealed class PageRequest
{
    public const int MaxLimit = 128;

    public PageRequest(int offset, int limit)
    {
        Offset = AtlasBounds.NonNegative(offset, nameof(offset));
        Limit = limit is >= 1 and <= MaxLimit
            ? limit
            : throw new ArgumentOutOfRangeException(nameof(limit), limit, "Limit must be between 1 and 128.");
    }

    public int Offset { get; }
    public int Limit { get; }
}

/// <summary>Closed source projection states for selection results.</summary>
public enum SourceProjectionState
{
    IndexedMatch,
    Changed,
    Unavailable,
    Unverifiable,
    UnsupportedEncoding,
    TooLargeToVerify,
    ReadUnstable,
    Refused,
    Canceled,
}

/// <summary>Coverage denominator; unknown and withheld carry no numeric fiction.</summary>
public sealed class SelectionCoverage
{
    private SelectionCoverage(AtlasDenominatorState state, double? value, string? reason)
    {
        State = AtlasBounds.Defined(state, nameof(state));
        Value = value;
        Reason = AtlasIdentityCodec.OptionalToken(reason, nameof(reason));
        if (state is AtlasDenominatorState.Known && value is not >= 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Coverage must be between 0 and 1.");
        }

        if (state is not AtlasDenominatorState.Known && (value is not null || Reason is null))
        {
            throw new ArgumentException("Unknown or withheld coverage requires absence of value and a reason.", nameof(reason));
        }
    }

    public AtlasDenominatorState State { get; }
    public double? Value { get; }
    public string? Reason { get; }
    public static SelectionCoverage Known(double value) => new(AtlasDenominatorState.Known, value, null);
    public static SelectionCoverage Unknown(string reason) => new(AtlasDenominatorState.Unknown, null, reason);
    public static SelectionCoverage Withheld(string reason) => new(AtlasDenominatorState.Withheld, null, reason);
}

/// <summary>One addressable declaration in a selection outline.</summary>
public sealed class OutlineDeclaration
{
    public OutlineDeclaration(string observationKey, string displayName, AtlasDeclarationKind kind, AtlasTextSpan span)
    {
        ObservationKey = AtlasIdentityCodec.RequiredToken(observationKey, nameof(observationKey));
        DisplayName = AtlasIdentityCodec.RequiredToken(displayName, nameof(displayName));
        Kind = AtlasBounds.Defined(kind, nameof(kind));
        Span = span;
    }

    public string ObservationKey { get; }
    public string DisplayName { get; }
    public AtlasDeclarationKind Kind { get; }
    public AtlasTextSpan Span { get; }
}

/// <summary>Structured, addressable outline. Empty outlines are valid.</summary>
public sealed class SelectionOutline
{
    public SelectionOutline(IEnumerable<OutlineDeclaration> declarations) =>
        Declarations = DirectoryObservation.RequiredItems(declarations, nameof(declarations));

    public ImmutableArray<OutlineDeclaration> Declarations { get; }
}

/// <summary>Projected source page with UTF-16 page and highlight spans.</summary>
public sealed class SourceTextPage
{
    public SourceTextPage(string text, AtlasTextSpan pageSpan, IEnumerable<AtlasTextSpan> highlights)
    {
        Text = AtlasIdentityCodec.ValidUnicode(text, nameof(text));
        PageSpan = pageSpan;
        if (Text.Length != pageSpan.Length)
        {
            throw new ArgumentException("Text length must equal the page span length.", nameof(text));
        }

        Highlights = highlights.ToImmutableArray();
        if (Highlights.Any(highlight => highlight.Start < pageSpan.Start || highlight.End > pageSpan.End))
        {
            throw new ArgumentException("Highlights must be contained in the page span.", nameof(highlights));
        }
    }

    public string Text { get; }
    public AtlasTextSpan PageSpan { get; }
    public ImmutableArray<AtlasTextSpan> Highlights { get; }
}

/// <summary>Source projection union. Only IndexedMatch may carry source text and highlights.</summary>
public sealed class SourceProjection
{
    private SourceProjection(SourceProjectionState state, string observationKey, AtlasSourceBinding? expectedBinding, string? decoderId, SourceTextPage? page)
    {
        State = AtlasBounds.Defined(state, nameof(state));
        ObservationKey = AtlasIdentityCodec.RequiredToken(observationKey, nameof(observationKey));
        ExpectedBinding = expectedBinding;
        DecoderId = AtlasIdentityCodec.OptionalToken(decoderId, nameof(decoderId));
        Page = page;
    }

    public SourceProjectionState State { get; }
    public string ObservationKey { get; }
    public AtlasSourceBinding? ExpectedBinding { get; }
    public string? DecoderId { get; }
    public SourceTextPage? Page { get; }

    public static SourceProjection IndexedMatch(AtlasSourceObservation observation, AtlasSourceBinding expectedBinding, string decoderId, SourceTextPage page)
    {
        ArgumentNullException.ThrowIfNull(observation);
        ArgumentNullException.ThrowIfNull(expectedBinding);
        ArgumentNullException.ThrowIfNull(page);
        if (observation.Status is not AtlasSourceObservationStatus.Verified)
        {
            throw new ArgumentException("IndexedMatch requires a verified source observation.", nameof(observation));
        }

        if (!BindingMatches(observation, expectedBinding) || !string.Equals(observation.DecoderId, decoderId, StringComparison.Ordinal))
        {
            throw new ArgumentException("IndexedMatch binding and decoder must match the verified source observation.", nameof(expectedBinding));
        }

        if (page.PageSpan.End > observation.DecodedUtf16Length)
        {
            throw new ArgumentException("Page span cannot exceed decoded source length.", nameof(page));
        }

        return new(SourceProjectionState.IndexedMatch, observation.ObservationKey, expectedBinding, decoderId, page);
    }

    public static SourceProjection Changed(string observationKey) => NonMatch(SourceProjectionState.Changed, observationKey);
    public static SourceProjection Unavailable(string observationKey) => NonMatch(SourceProjectionState.Unavailable, observationKey);
    public static SourceProjection Unavailable(string observationKey, SourceTextPage page) => throw new ArgumentException("Unavailable source projections cannot carry text.", nameof(page));
    public static SourceProjection Unverifiable(string observationKey) => NonMatch(SourceProjectionState.Unverifiable, observationKey);
    public static SourceProjection UnsupportedEncoding(string observationKey) => NonMatch(SourceProjectionState.UnsupportedEncoding, observationKey);
    public static SourceProjection TooLargeToVerify(string observationKey) => NonMatch(SourceProjectionState.TooLargeToVerify, observationKey);
    public static SourceProjection ReadUnstable(string observationKey) => NonMatch(SourceProjectionState.ReadUnstable, observationKey);
    public static SourceProjection Refused(string observationKey) => NonMatch(SourceProjectionState.Refused, observationKey);
    public static SourceProjection Canceled(string observationKey) => NonMatch(SourceProjectionState.Canceled, observationKey);

    private static SourceProjection NonMatch(SourceProjectionState state, string observationKey) =>
        new(state, observationKey, null, null, null);

    private static bool BindingMatches(AtlasSourceObservation source, AtlasSourceBinding binding) =>
        string.Equals(binding.ManifestIdentity, source.ManifestToken, StringComparison.Ordinal)
        && string.Equals(binding.ManifestFileIdentity, source.FileValue, StringComparison.Ordinal)
        && string.Equals(binding.PolicyIdentity, source.PolicyToken, StringComparison.Ordinal)
        && string.Equals(binding.RootIdentity, AtlasIdentityCodec.ForNativeObject(source.RootIdentity!), StringComparison.Ordinal)
        && string.Equals(binding.FileIdentity, AtlasIdentityCodec.ForNativeObject(source.FileIdentity!), StringComparison.Ordinal)
        && string.Equals(binding.ContentHash, source.CanonicalSha256, StringComparison.Ordinal);
}

/// <summary>Inventory page returned by query ports.</summary>
public sealed class InventoryPage
{
    public InventoryPage(PageRequest request, AtlasBounds bounds, IEnumerable<AtlasFileEntry> files)
    {
        Request = request ?? throw new ArgumentNullException(nameof(request));
        Bounds = bounds ?? throw new ArgumentNullException(nameof(bounds));
        Files = DirectoryObservation.RequiredItems(files, nameof(files));
        if (Files.Length != Bounds.ReturnedRows)
        {
            throw new ArgumentException("File count must match bounds returned rows.", nameof(files));
        }
    }

    public PageRequest Request { get; }
    public AtlasBounds Bounds { get; }
    public ImmutableArray<AtlasFileEntry> Files { get; }
}

/// <summary>Selection input. It names manifest/file/declaration observations and cannot issue grant authority.</summary>
public sealed class SelectionRequest
{
    public SelectionRequest(string manifestToken, string fileValue, string? declarationObservationKey, long requestSequence)
    {
        ManifestToken = AtlasIdentityCodec.RequiredToken(manifestToken, nameof(manifestToken));
        FileValue = AtlasIdentityCodec.RequiredToken(fileValue, nameof(fileValue));
        DeclarationObservationKey = AtlasIdentityCodec.OptionalToken(declarationObservationKey, nameof(declarationObservationKey));
        RequestSequence = AtlasBounds.NonNegative(requestSequence, nameof(requestSequence));
    }

    public string ManifestToken { get; }
    public string FileValue { get; }
    public string? DeclarationObservationKey { get; }
    public long RequestSequence { get; }
}

/// <summary>Selection output with coverage, bounds, limitations, source status and Core-issued receipt.</summary>
public sealed class SelectionProjection
{
    public SelectionProjection(string receiptToken, string manifestToken, string fileValue, long generation, SelectionOutline outline, SourceProjection source, AtlasBounds bounds, SelectionCoverage coverage, IEnumerable<string> limitations)
    {
        ReceiptToken = AtlasIdentityCodec.RequiredToken(receiptToken, nameof(receiptToken));
        ManifestToken = AtlasIdentityCodec.RequiredToken(manifestToken, nameof(manifestToken));
        FileValue = AtlasIdentityCodec.RequiredToken(fileValue, nameof(fileValue));
        Generation = AtlasBounds.NonNegative(generation, nameof(generation));
        Outline = outline ?? throw new ArgumentNullException(nameof(outline));
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Bounds = bounds ?? throw new ArgumentNullException(nameof(bounds));
        Coverage = coverage ?? throw new ArgumentNullException(nameof(coverage));
        Limitations = RequiredStrings(limitations, nameof(limitations));
    }

    public string ReceiptToken { get; }
    public string ManifestToken { get; }
    public string FileValue { get; }
    public long Generation { get; }
    public SelectionOutline Outline { get; }
    public SourceProjection Source { get; }
    public AtlasBounds Bounds { get; }
    public SelectionCoverage Coverage { get; }
    public ImmutableArray<string> Limitations { get; }

    private static ImmutableArray<string> RequiredStrings(IEnumerable<string> values, string name)
    {
        ArgumentNullException.ThrowIfNull(values, name);
        return values.Select(value => AtlasIdentityCodec.RequiredToken(value, name)).ToImmutableArray();
    }
}

/// <summary>Port for directory enumeration under a pre-issued root grant.</summary>
public interface IAtlasDirectoryEnumerator
{
    Task<DirectoryObservation> EnumerateAsync(AtlasRootGrant rootGrant, EnumerationLimits limits, CancellationToken cancellationToken);
}

/// <summary>Read-only Atlas query port. Implementations revalidate receipts and never trust caller bindings.</summary>
public interface IAtlasQueries
{
    Task<InventoryPage> InventoryAsync(PageRequest request, CancellationToken cancellationToken);
    Task<SelectionProjection> SelectAsync(SelectionRequest request, CancellationToken cancellationToken);
    Task<SelectionProjection> RestoreAsync(string issuedReceiptToken, long requestSequence, CancellationToken cancellationToken);
}
