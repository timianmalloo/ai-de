using System.Collections.Immutable;

namespace AiDe.Core.Understanding;

/// <summary>Directory enumeration limits. Candidate ceilings are policy defaults, not measured guarantees.</summary>
public readonly record struct EnumerationLimits
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
public readonly record struct PageRequest
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

/// <summary>Closed source result set for selection projections.</summary>
public enum AtlasSourceResult
{
    Verified,
    Unavailable,
    Refused,
    Changed,
    Canceled,
}

/// <summary>Inventory page returned by query ports.</summary>
public sealed class InventoryPage
{
    public InventoryPage(PageRequest request, AtlasBounds bounds, IEnumerable<AtlasFileEntry> files)
    {
        Request = request;
        Bounds = bounds ?? throw new ArgumentNullException(nameof(bounds));
        Files = DirectoryObservation.RequiredItems(files, nameof(files));
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
    public SelectionProjection(string receiptToken, string manifestToken, string fileValue, long generation, string outline, AtlasSourceResult sourceResult, AtlasBounds bounds, double coverage, IEnumerable<string> limitations)
    {
        ReceiptToken = AtlasIdentityCodec.RequiredToken(receiptToken, nameof(receiptToken));
        ManifestToken = AtlasIdentityCodec.RequiredToken(manifestToken, nameof(manifestToken));
        FileValue = AtlasIdentityCodec.RequiredToken(fileValue, nameof(fileValue));
        Generation = AtlasBounds.NonNegative(generation, nameof(generation));
        Outline = AtlasIdentityCodec.RequiredToken(outline, nameof(outline));
        SourceResult = AtlasBounds.Defined(sourceResult, nameof(sourceResult));
        Bounds = bounds ?? throw new ArgumentNullException(nameof(bounds));
        Coverage = coverage is >= 0 and <= 1 ? coverage : throw new ArgumentOutOfRangeException(nameof(coverage), coverage, "Coverage must be between 0 and 1.");
        Limitations = RequiredStrings(limitations, nameof(limitations));
    }

    public string ReceiptToken { get; }
    public string ManifestToken { get; }
    public string FileValue { get; }
    public long Generation { get; }
    public string Outline { get; }
    public AtlasSourceResult SourceResult { get; }
    public AtlasBounds Bounds { get; }
    public double Coverage { get; }
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
