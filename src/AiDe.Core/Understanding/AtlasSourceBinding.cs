using System.Text.RegularExpressions;

namespace AiDe.Core.Understanding;

public enum AtlasSourceBindingMismatch
{
    None,
    Manifest,
    ManifestFile,
    Policy,
    Root,
    File,
    Hash,
}

/// <summary>
/// Manifest-bound source observation identity. The five source tokens are opaque ordinal values;
/// the content hash is the canonical <c>sha256:</c> plus 64 lowercase hexadecimal characters.
/// </summary>
public sealed class AtlasSourceBinding : IEquatable<AtlasSourceBinding>
{
    private static readonly Regex CanonicalSha256 = new(
        "\\Asha256:[0-9a-f]{64}\\z",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    private AtlasSourceBinding(
        string manifestIdentity,
        string manifestFileIdentity,
        string policyIdentity,
        string rootIdentity,
        string fileIdentity,
        string contentHash)
    {
        ManifestIdentity = manifestIdentity;
        ManifestFileIdentity = manifestFileIdentity;
        PolicyIdentity = policyIdentity;
        RootIdentity = rootIdentity;
        FileIdentity = fileIdentity;
        ContentHash = contentHash;
    }

    public string ManifestIdentity { get; }

    public string ManifestFileIdentity { get; }

    public string PolicyIdentity { get; }

    public string RootIdentity { get; }

    public string FileIdentity { get; }

    public string ContentHash { get; }

    public static AtlasSourceBinding Create(
        string manifestIdentity,
        string manifestFileIdentity,
        string policyIdentity,
        string rootIdentity,
        string fileIdentity,
        string contentHash) =>
        new(
            Opaque(manifestIdentity, nameof(manifestIdentity)),
            Opaque(manifestFileIdentity, nameof(manifestFileIdentity)),
            Opaque(policyIdentity, nameof(policyIdentity)),
            Opaque(rootIdentity, nameof(rootIdentity)),
            Opaque(fileIdentity, nameof(fileIdentity)),
            Hash(contentHash, nameof(contentHash)));

    public bool Matches(
        string manifestIdentity,
        string manifestFileIdentity,
        string policyIdentity,
        string rootIdentity,
        string fileIdentity,
        string contentHash) =>
        CompareTo(manifestIdentity, manifestFileIdentity, policyIdentity, rootIdentity, fileIdentity, contentHash)
            is AtlasSourceBindingMismatch.None;

    public AtlasSourceBindingMismatch CompareTo(
        string manifestIdentity,
        string manifestFileIdentity,
        string policyIdentity,
        string rootIdentity,
        string fileIdentity,
        string contentHash)
    {
        if (!Same(ManifestIdentity, Opaque(manifestIdentity, nameof(manifestIdentity)))) return AtlasSourceBindingMismatch.Manifest;
        if (!Same(ManifestFileIdentity, Opaque(manifestFileIdentity, nameof(manifestFileIdentity)))) return AtlasSourceBindingMismatch.ManifestFile;
        if (!Same(PolicyIdentity, Opaque(policyIdentity, nameof(policyIdentity)))) return AtlasSourceBindingMismatch.Policy;
        if (!Same(RootIdentity, Opaque(rootIdentity, nameof(rootIdentity)))) return AtlasSourceBindingMismatch.Root;
        if (!Same(FileIdentity, Opaque(fileIdentity, nameof(fileIdentity)))) return AtlasSourceBindingMismatch.File;
        return Same(ContentHash, Hash(contentHash, nameof(contentHash))) ? AtlasSourceBindingMismatch.None : AtlasSourceBindingMismatch.Hash;
    }

    public bool Equals(AtlasSourceBinding? other) =>
        other is not null
        && Same(ManifestIdentity, other.ManifestIdentity)
        && Same(ManifestFileIdentity, other.ManifestFileIdentity)
        && Same(PolicyIdentity, other.PolicyIdentity)
        && Same(RootIdentity, other.RootIdentity)
        && Same(FileIdentity, other.FileIdentity)
        && Same(ContentHash, other.ContentHash);

    public override bool Equals(object? obj) => obj is AtlasSourceBinding other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(
            StringComparer.Ordinal.GetHashCode(ManifestIdentity),
            StringComparer.Ordinal.GetHashCode(ManifestFileIdentity),
            StringComparer.Ordinal.GetHashCode(PolicyIdentity),
            StringComparer.Ordinal.GetHashCode(RootIdentity),
            StringComparer.Ordinal.GetHashCode(FileIdentity),
            StringComparer.Ordinal.GetHashCode(ContentHash));

    public static bool operator ==(AtlasSourceBinding? left, AtlasSourceBinding? right) => Equals(left, right);

    public static bool operator !=(AtlasSourceBinding? left, AtlasSourceBinding? right) => !Equals(left, right);

    private static bool Same(string expected, string actual) =>
        string.Equals(expected, actual, StringComparison.Ordinal);

    private static string Hash(string value, string name)
    {
        var checkedValue = Opaque(value, name);
        return CanonicalSha256.IsMatch(checkedValue)
            ? checkedValue
            : throw new ArgumentException("Hash must be canonical sha256: plus 64 lowercase hex characters.", name);
    }

    private static string Opaque(string value, string name)
    {
        ArgumentNullException.ThrowIfNull(value, name);
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value must not be blank.", name)
            : ValidUnicode(value, name);
    }

    private static string ValidUnicode(string value, string name)
    {
        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];
            if (!char.IsSurrogate(current))
            {
                continue;
            }

            if (char.IsHighSurrogate(current)
                && i + 1 < value.Length
                && char.IsLowSurrogate(value[i + 1]))
            {
                i++;
                continue;
            }

            throw new ArgumentException("Value must contain valid Unicode scalar values.", name);
        }

        return value;
    }
}
