using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace AiDe.Core.Understanding;

public enum AtlasCompletionState { Complete, Partial, Canceled, Refused, BudgetExceeded }
public enum AtlasDirectoryEntryKind { File, Directory, RejectedLink, Unavailable }
public enum AtlasFileClassification { CSharp, Text, Unknown }
public enum AtlasFileAvailability { Available, Unavailable, Refused, Unknown }
public enum AtlasDenominatorState { Known, Unknown, Withheld }
public enum AtlasSourceObservationStatus { Verified, IndexedMatch, Changed, Unavailable, Unverifiable, UnsupportedEncoding, TooLargeToVerify, ReadUnstable, Refused, Canceled }
public enum AtlasDeclarationKind { Type, Method, Constructor, Property, Accessor }
public enum AtlasDeclarationRole { Ordinary, PartialDefinition, PartialImplementation }

/// <summary>Native filesystem identity: volume serial and file index only.</summary>
public sealed class AtlasObjectIdentity : IEquatable<AtlasObjectIdentity>
{
    public AtlasObjectIdentity(string volumeSerial, string fileIndex)
    {
        VolumeSerial = AtlasIdentityCodec.RequiredToken(volumeSerial, nameof(volumeSerial));
        FileIndex = AtlasIdentityCodec.RequiredToken(fileIndex, nameof(fileIndex));
    }

    public string VolumeSerial { get; }
    public string FileIndex { get; }

    public bool Equals(AtlasObjectIdentity? other) =>
        other is not null
        && string.Equals(VolumeSerial, other.VolumeSerial, StringComparison.Ordinal)
        && string.Equals(FileIndex, other.FileIndex, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is AtlasObjectIdentity other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(
        StringComparer.Ordinal.GetHashCode(VolumeSerial),
        StringComparer.Ordinal.GetHashCode(FileIndex));
}

/// <summary>UTF-16 span where null means absent; zero length is still a present span.</summary>
public readonly record struct AtlasTextSpan
{
    public AtlasTextSpan(int start, int length)
    {
        Start = NonNegative(start, nameof(start));
        Length = NonNegative(length, nameof(length));
        _ = checked(Start + Length);
    }

    public int Start { get; }
    public int Length { get; }
    public int End => Start + Length >= Start ? Start + Length : throw new OverflowException("Span end overflowed.");

    private static int NonNegative(int value, string name) =>
        value >= 0 ? value : throw new ArgumentOutOfRangeException(name, value, "Value must be non-negative.");
}

/// <summary>Request/response bounds with an explicit denominator state; unknown is never encoded as zero.</summary>
public sealed class AtlasBounds
{
    public AtlasBounds(
        int requestedLimit,
        int effectiveLimit,
        int returnedRows,
        long returnedBytes,
        long? totalCount,
        AtlasDenominatorState totalState,
        string? omissionReason,
        string? limitingDimension)
    {
        RequestedLimit = NonNegative(requestedLimit, nameof(requestedLimit));
        EffectiveLimit = NonNegative(effectiveLimit, nameof(effectiveLimit));
        ReturnedRows = NonNegative(returnedRows, nameof(returnedRows));
        ReturnedBytes = NonNegative(returnedBytes, nameof(returnedBytes));
        if (EffectiveLimit > RequestedLimit)
        {
            throw new ArgumentException("Effective limit cannot exceed requested limit.", nameof(effectiveLimit));
        }
        if (ReturnedRows > EffectiveLimit)
        {
            throw new ArgumentException("Returned rows cannot exceed effective limit.", nameof(returnedRows));
        }

        TotalState = Defined(totalState, nameof(totalState));
        TotalCount = totalState is AtlasDenominatorState.Known
            ? NonNegativeRequired(totalCount, nameof(totalCount))
            : totalCount is null ? null : throw new ArgumentException("Unknown or withheld totals must be absent, not zero.", nameof(totalCount));
        if (TotalCount is { } total && total < ReturnedRows)
        {
            throw new ArgumentException("Known total cannot be lower than returned rows.", nameof(totalCount));
        }
        if (TotalState is not AtlasDenominatorState.Known && string.IsNullOrWhiteSpace(omissionReason))
        {
            throw new ArgumentException("Unknown or withheld totals require an omission reason.", nameof(omissionReason));
        }
        OmissionReason = AtlasIdentityCodec.OptionalToken(omissionReason, nameof(omissionReason));
        LimitingDimension = AtlasIdentityCodec.OptionalToken(limitingDimension, nameof(limitingDimension));
    }

    public int RequestedLimit { get; }
    public int EffectiveLimit { get; }
    public int ReturnedRows { get; }
    public long ReturnedBytes { get; }
    public long? TotalCount { get; }
    public AtlasDenominatorState TotalState { get; }
    public string? OmissionReason { get; }
    public string? LimitingDimension { get; }

    internal static int NonNegative(int value, string name) =>
        value >= 0 ? value : throw new ArgumentOutOfRangeException(name, value, "Value must be non-negative.");

    internal static long NonNegative(long value, string name) =>
        value >= 0 ? value : throw new ArgumentOutOfRangeException(name, value, "Value must be non-negative.");

    private static long NonNegativeRequired(long? value, string name) =>
        value is { } actual ? NonNegative(actual, name) : throw new ArgumentException("Known totals require a total count.", name);

    internal static TEnum Defined<TEnum>(TEnum value, string name) where TEnum : struct, Enum =>
        Enum.IsDefined(value) ? value : throw new ArgumentOutOfRangeException(name, value, "Unsupported enum value.");
}

/// <summary>Core-issued immutable grant; only Core can construct it and expected root identity is required.</summary>
public sealed class AtlasRootGrant
{
    private AtlasRootGrant(
        string grantVersion,
        string workspaceToken,
        string rootToken,
        string policyToken,
        string sessionToken,
        string approvedAbsoluteRoot,
        AtlasObjectIdentity expectedNativeRootIdentity,
        DateTimeOffset expiresAt)
    {
        GrantVersion = grantVersion;
        WorkspaceToken = workspaceToken;
        RootToken = rootToken;
        PolicyToken = policyToken;
        SessionToken = sessionToken;
        ApprovedAbsoluteRoot = approvedAbsoluteRoot;
        ExpectedNativeRootIdentity = expectedNativeRootIdentity;
        ExpiresAt = expiresAt;
    }

    public string GrantVersion { get; }
    public string WorkspaceToken { get; }
    public string RootToken { get; }
    public string PolicyToken { get; }
    public string SessionToken { get; }
    public string ApprovedAbsoluteRoot { get; }
    public AtlasObjectIdentity ExpectedNativeRootIdentity { get; }
    public DateTimeOffset ExpiresAt { get; }

    internal static AtlasRootGrant Create(
        string grantVersion,
        string workspaceToken,
        string rootToken,
        string policyToken,
        string sessionToken,
        string approvedAbsoluteRoot,
        AtlasObjectIdentity? expectedNativeRootIdentity,
        DateTimeOffset expiresAt) =>
        new(
            AtlasIdentityCodec.RequiredToken(grantVersion, nameof(grantVersion)),
            AtlasIdentityCodec.RequiredToken(workspaceToken, nameof(workspaceToken)),
            AtlasIdentityCodec.RequiredToken(rootToken, nameof(rootToken)),
            AtlasIdentityCodec.RequiredToken(policyToken, nameof(policyToken)),
            AtlasIdentityCodec.RequiredToken(sessionToken, nameof(sessionToken)),
            AtlasIdentityCodec.RequiredToken(approvedAbsoluteRoot, nameof(approvedAbsoluteRoot)),
            expectedNativeRootIdentity ?? throw new ArgumentNullException(nameof(expectedNativeRootIdentity)),
            expiresAt);
}

/// <summary>Observed directory entry metadata. Missing identity/counts remain nullable absence.</summary>
public sealed class AtlasDirectoryEntry
{
    public AtlasDirectoryEntry(string relativePath, AtlasDirectoryEntryKind kind, AtlasObjectIdentity? objectIdentity, long? lengthBytes, long? linkCount, string? reason)
    {
        RelativePath = AtlasIdentityCodec.RequiredToken(relativePath, nameof(relativePath));
        Kind = AtlasBounds.Defined(kind, nameof(kind));
        ObjectIdentity = objectIdentity;
        LengthBytes = lengthBytes is null ? null : AtlasBounds.NonNegative(lengthBytes.Value, nameof(lengthBytes));
        LinkCount = linkCount is null ? null : AtlasBounds.NonNegative(linkCount.Value, nameof(linkCount));
        Reason = AtlasIdentityCodec.OptionalToken(reason, nameof(reason));
    }

    public string RelativePath { get; }
    public AtlasDirectoryEntryKind Kind { get; }
    public AtlasObjectIdentity? ObjectIdentity { get; }
    public long? LengthBytes { get; }
    public long? LinkCount { get; }
    public string? Reason { get; }
}

/// <summary>Directory observation with expected/observed root identities and immutable ordered entries.</summary>
public sealed class DirectoryObservation
{
    public DirectoryObservation(string observationKey, AtlasObjectIdentity expectedRootIdentity, AtlasObjectIdentity? observedRootIdentity, AtlasCompletionState completion, long sequence, AtlasBounds bounds, IEnumerable<AtlasDirectoryEntry> entries)
    {
        ObservationKey = AtlasIdentityCodec.RequiredToken(observationKey, nameof(observationKey));
        ExpectedRootIdentity = expectedRootIdentity;
        ObservedRootIdentity = observedRootIdentity;
        Completion = AtlasBounds.Defined(completion, nameof(completion));
        Sequence = AtlasBounds.NonNegative(sequence, nameof(sequence));
        Bounds = bounds ?? throw new ArgumentNullException(nameof(bounds));
        Entries = RequiredItems(entries, nameof(entries));
    }

    public string ObservationKey { get; }
    public AtlasObjectIdentity ExpectedRootIdentity { get; }
    public AtlasObjectIdentity? ObservedRootIdentity { get; }
    public AtlasCompletionState Completion { get; }
    public long Sequence { get; }
    public AtlasBounds Bounds { get; }
    public ImmutableArray<AtlasDirectoryEntry> Entries { get; }

    internal static ImmutableArray<T> RequiredItems<T>(IEnumerable<T> items, string name) where T : class
    {
        ArgumentNullException.ThrowIfNull(items, name);
        var immutable = items.ToImmutableArray();
        return immutable.Any(static item => item is null)
            ? throw new ArgumentException("Collection must not contain null items.", name)
            : immutable;
    }
}

/// <summary>Manifest file row; file identity is the codec value, not a hash alias.</summary>
public sealed class AtlasFileEntry
{
    public AtlasFileEntry(string fileValue, string relativePath, string parentPathKey, AtlasDirectoryEntryKind kind, AtlasFileClassification classification, AtlasObjectIdentity? observedIdentity, AtlasFileAvailability availability, string? reason)
    {
        FileValue = AtlasIdentityCodec.RequiredToken(fileValue, nameof(fileValue));
        RelativePath = AtlasIdentityCodec.RequiredToken(relativePath, nameof(relativePath));
        ParentPathKey = AtlasIdentityCodec.RequiredToken(parentPathKey, nameof(parentPathKey));
        Kind = AtlasBounds.Defined(kind, nameof(kind));
        Classification = AtlasBounds.Defined(classification, nameof(classification));
        ObservedIdentity = observedIdentity;
        Availability = AtlasBounds.Defined(availability, nameof(availability));
        Reason = AtlasIdentityCodec.OptionalToken(reason, nameof(reason));
    }

    public string FileValue { get; }
    public string RelativePath { get; }
    public string ParentPathKey { get; }
    public AtlasDirectoryEntryKind Kind { get; }
    public AtlasFileClassification Classification { get; }
    public AtlasObjectIdentity? ObservedIdentity { get; }
    public AtlasFileAvailability Availability { get; }
    public string? Reason { get; }
}

/// <summary>Verified source metadata. The body is request-local only and is not retained here.</summary>
public sealed class AtlasSourceObservation
{
    private static readonly Regex CanonicalSha256Pattern = new("\\Asha256:[0-9a-f]{64}\\z", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    public AtlasSourceObservation(string observationKey, string manifestToken, string fileValue, string policyToken, AtlasObjectIdentity rootIdentity, AtlasObjectIdentity fileIdentity, string canonicalSha256, long byteLength, string decoderId, int decodedUtf16Length, AtlasSourceObservationStatus status, AtlasBounds bounds)
        : this(observationKey, manifestToken, fileValue, policyToken, rootIdentity, fileIdentity, canonicalSha256, byteLength, decoderId, decodedUtf16Length, status, bounds, null)
    {
        if (status is not AtlasSourceObservationStatus.Verified)
        {
            throw new ArgumentException("The public all-fields constructor only creates verified source observations; use a failure-state factory for non-verified states.", nameof(status));
        }
    }

    private AtlasSourceObservation(string observationKey, string manifestToken, string fileValue, string policyToken, AtlasObjectIdentity? rootIdentity, AtlasObjectIdentity? fileIdentity, string? canonicalSha256, long? byteLength, string? decoderId, int? decodedUtf16Length, AtlasSourceObservationStatus status, AtlasBounds bounds, string? reason)
    {
        ObservationKey = AtlasIdentityCodec.RequiredToken(observationKey, nameof(observationKey));
        ManifestToken = AtlasIdentityCodec.RequiredToken(manifestToken, nameof(manifestToken));
        FileValue = AtlasIdentityCodec.RequiredToken(fileValue, nameof(fileValue));
        PolicyToken = AtlasIdentityCodec.RequiredToken(policyToken, nameof(policyToken));
        Status = AtlasBounds.Defined(status, nameof(status));
        Bounds = bounds ?? throw new ArgumentNullException(nameof(bounds));
        Reason = AtlasIdentityCodec.OptionalToken(reason, nameof(reason));
        var verified = Status is AtlasSourceObservationStatus.Verified or AtlasSourceObservationStatus.IndexedMatch;
        RootIdentity = verified ? rootIdentity ?? throw new ArgumentNullException(nameof(rootIdentity)) : rootIdentity;
        FileIdentity = verified ? fileIdentity ?? throw new ArgumentNullException(nameof(fileIdentity)) : fileIdentity;
        CanonicalSha256 = verified ? Hash(canonicalSha256!, nameof(canonicalSha256)) : OptionalHash(canonicalSha256, nameof(canonicalSha256));
        ByteLength = verified ? AtlasBounds.NonNegative(byteLength ?? throw new ArgumentNullException(nameof(byteLength)), nameof(byteLength)) : OptionalNonNegative(byteLength, nameof(byteLength));
        DecoderId = verified ? AtlasIdentityCodec.RequiredToken(decoderId, nameof(decoderId)) : AtlasIdentityCodec.OptionalToken(decoderId, nameof(decoderId));
        DecodedUtf16Length = verified ? AtlasBounds.NonNegative(decodedUtf16Length ?? throw new ArgumentNullException(nameof(decodedUtf16Length)), nameof(decodedUtf16Length)) : OptionalNonNegative(decodedUtf16Length, nameof(decodedUtf16Length));
    }

    public string ObservationKey { get; }
    public string ManifestToken { get; }
    public string FileValue { get; }
    public string PolicyToken { get; }
    public AtlasObjectIdentity? RootIdentity { get; }
    public AtlasObjectIdentity? FileIdentity { get; }
    public string? CanonicalSha256 { get; }
    public long? ByteLength { get; }
    public string? DecoderId { get; }
    public int? DecodedUtf16Length { get; }
    public AtlasSourceObservationStatus Status { get; }
    public AtlasBounds Bounds { get; }
    public string? Reason { get; }

    public static AtlasSourceObservation Verified(string observationKey, string manifestToken, string fileValue, string policyToken, AtlasObjectIdentity rootIdentity, AtlasObjectIdentity fileIdentity, string canonicalSha256, long byteLength, string decoderId, int decodedUtf16Length, AtlasBounds bounds) =>
        new(observationKey, manifestToken, fileValue, policyToken, rootIdentity, fileIdentity, canonicalSha256, byteLength, decoderId, decodedUtf16Length, AtlasSourceObservationStatus.Verified, bounds, null);

    public static AtlasSourceObservation Unavailable(string observationKey, string manifestToken, string fileValue, string policyToken, AtlasBounds bounds, string reason) =>
        new(observationKey, manifestToken, fileValue, policyToken, null, null, null, null, null, null, AtlasSourceObservationStatus.Unavailable, bounds, reason);

    private static string Hash(string value, string name)
    {
        var checkedValue = AtlasIdentityCodec.RequiredToken(value, name);
        return CanonicalSha256Pattern.IsMatch(checkedValue)
            ? checkedValue
            : throw new ArgumentException("Hash must be canonical sha256: plus 64 lowercase hex characters.", name);
    }

    private static string? OptionalHash(string? value, string name) =>
        value is null ? null : Hash(value, name);

    private static int? OptionalNonNegative(int? value, string name) =>
        value is null ? null : AtlasBounds.NonNegative(value.Value, name);

    private static long? OptionalNonNegative(long? value, string name) =>
        value is null ? null : AtlasBounds.NonNegative(value.Value, name);
}

public enum AtlasClassifierFlavor { Class, RecordClass, Struct, RecordStruct, Interface, Enum }
public enum AtlasLexicalParentState { NotApplicable, Present, Unavailable, OutsidePage }
public enum AtlasStructureProvenance { Extracted, Unavailable }

/// <summary>Descriptive syntax evidence; occurrence and source-binding identities remain unchanged.</summary>
public sealed record AtlasDeclarationStructure(
    AtlasClassifierFlavor? ClassifierFlavor, AtlasLexicalParentState ParentState,
    string? ParentObservationKey, AtlasStructureProvenance Provenance, string? Reason);

/// <summary>Declaration metadata. Logical symbol value can be absent when the compiler identity is unavailable.</summary>
public sealed class AtlasDeclaration
{
    public AtlasDeclaration(string observationKey, string? logicalSymbolValue, string sourceObservationKey, string contextKey, AtlasSourceBinding sourceBinding, AtlasDeclarationKind kind, AtlasDeclarationRole role, string displaySignature, string identifier, AtlasTextSpan identifierSpan, AtlasTextSpan declarationSpan, AtlasTextSpan? bodySpan, string? unresolvedReason, AtlasDeclarationStructure? structure = null)
    {
        ObservationKey = AtlasIdentityCodec.RequiredToken(observationKey, nameof(observationKey));
        LogicalSymbolValue = AtlasIdentityCodec.OptionalToken(logicalSymbolValue, nameof(logicalSymbolValue));
        SourceObservationKey = AtlasIdentityCodec.RequiredToken(sourceObservationKey, nameof(sourceObservationKey));
        ContextKey = AtlasIdentityCodec.RequiredToken(contextKey, nameof(contextKey));
        SourceBinding = sourceBinding ?? throw new ArgumentNullException(nameof(sourceBinding));
        Kind = AtlasBounds.Defined(kind, nameof(kind));
        Role = AtlasBounds.Defined(role, nameof(role));
        DisplaySignature = AtlasIdentityCodec.RequiredToken(displaySignature, nameof(displaySignature));
        Identifier = AtlasIdentityCodec.RequiredToken(identifier, nameof(identifier));
        IdentifierSpan = identifierSpan;
        DeclarationSpan = declarationSpan;
        BodySpan = bodySpan;
        UnresolvedReason = AtlasIdentityCodec.OptionalToken(unresolvedReason, nameof(unresolvedReason));
        Structure = structure;
    }

    public string ObservationKey { get; }
    public string? LogicalSymbolValue { get; }
    public string SourceObservationKey { get; }
    public string ContextKey { get; }
    public AtlasSourceBinding SourceBinding { get; }
    public AtlasDeclarationKind Kind { get; }
    public AtlasDeclarationRole Role { get; }
    public string DisplaySignature { get; }
    public string Identifier { get; }
    public AtlasTextSpan IdentifierSpan { get; }
    public AtlasTextSpan DeclarationSpan { get; }
    public AtlasTextSpan? BodySpan { get; }
    public string? UnresolvedReason { get; }
    public AtlasDeclarationStructure? Structure { get; }
}

/// <summary>In-memory manifest for detached reader producers; no source body or compiler object is retained.</summary>
public sealed class AtlasManifest
{
    public AtlasManifest(string token, AtlasRootGrant rootGrant, string directoryObservationKey, IEnumerable<AtlasFileEntry> files, IEnumerable<AtlasSourceObservation> sourceObservations, IEnumerable<AtlasDeclaration> declarations, AtlasCompletionState completion, AtlasBounds bounds)
    {
        Token = AtlasIdentityCodec.RequiredToken(token, nameof(token));
        RootGrant = rootGrant ?? throw new ArgumentNullException(nameof(rootGrant));
        DirectoryObservationKey = AtlasIdentityCodec.RequiredToken(directoryObservationKey, nameof(directoryObservationKey));
        Files = DirectoryObservation.RequiredItems(files, nameof(files));
        SourceObservations = DirectoryObservation.RequiredItems(sourceObservations, nameof(sourceObservations));
        Declarations = DirectoryObservation.RequiredItems(declarations, nameof(declarations));
        Completion = AtlasBounds.Defined(completion, nameof(completion));
        Bounds = bounds ?? throw new ArgumentNullException(nameof(bounds));
        ValidateMembership();
    }

    public string Token { get; }
    public AtlasRootGrant RootGrant { get; }
    public string DirectoryObservationKey { get; }
    public ImmutableArray<AtlasFileEntry> Files { get; }
    public ImmutableArray<AtlasSourceObservation> SourceObservations { get; }
    public ImmutableArray<AtlasDeclaration> Declarations { get; }
    public AtlasCompletionState Completion { get; }
    public AtlasBounds Bounds { get; }

    private void ValidateMembership()
    {
        var files = Files.ToDictionary(file => file.FileValue, StringComparer.Ordinal);
        var sources = SourceObservations.ToDictionary(source => source.ObservationKey, StringComparer.Ordinal);
        if (sources.Values.Any(source => !string.Equals(source.ManifestToken, Token, StringComparison.Ordinal) || !files.ContainsKey(source.FileValue)))
        {
            throw new ArgumentException("Source observations must reference this manifest and one of its files.", nameof(SourceObservations));
        }

        foreach (var declaration in Declarations)
        {
            if (!sources.TryGetValue(declaration.SourceObservationKey, out var source) || source.Status is not AtlasSourceObservationStatus.Verified)
            {
                throw new ArgumentException("Declarations must reference an existing verified source observation.", nameof(Declarations));
            }

            var sourceBinding = declaration.SourceBinding;
            if (!string.Equals(sourceBinding.ManifestIdentity, source.ManifestToken, StringComparison.Ordinal)
                || !string.Equals(sourceBinding.ManifestFileIdentity, source.FileValue, StringComparison.Ordinal)
                || !string.Equals(sourceBinding.PolicyIdentity, source.PolicyToken, StringComparison.Ordinal)
                || !string.Equals(sourceBinding.RootIdentity, AtlasIdentityCodec.ForNativeObject(source.RootIdentity!), StringComparison.Ordinal)
                || !string.Equals(sourceBinding.FileIdentity, AtlasIdentityCodec.ForNativeObject(source.FileIdentity!), StringComparison.Ordinal)
                || !string.Equals(sourceBinding.ContentHash, source.CanonicalSha256, StringComparison.Ordinal))
            {
                throw new ArgumentException("Declaration source binding must match its verified source observation.", nameof(Declarations));
            }
        }
    }
}
