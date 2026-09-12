using System.Text;

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

public readonly record struct AtlasSourceBinding
{
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
            Required(manifestIdentity, nameof(manifestIdentity)),
            Required(manifestFileIdentity, nameof(manifestFileIdentity)),
            Required(policyIdentity, nameof(policyIdentity)),
            Required(rootIdentity, nameof(rootIdentity)),
            Required(fileIdentity, nameof(fileIdentity)),
            Required(contentHash, nameof(contentHash)));

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
        if (!Same(ManifestIdentity, manifestIdentity)) return AtlasSourceBindingMismatch.Manifest;
        if (!Same(ManifestFileIdentity, manifestFileIdentity)) return AtlasSourceBindingMismatch.ManifestFile;
        if (!Same(PolicyIdentity, policyIdentity)) return AtlasSourceBindingMismatch.Policy;
        if (!Same(RootIdentity, rootIdentity)) return AtlasSourceBindingMismatch.Root;
        if (!Same(FileIdentity, fileIdentity)) return AtlasSourceBindingMismatch.File;
        return Same(ContentHash, contentHash) ? AtlasSourceBindingMismatch.None : AtlasSourceBindingMismatch.Hash;
    }

    private static bool Same(string expected, string actual) =>
        string.Equals(expected, Required(actual, nameof(actual)), StringComparison.Ordinal);

    private static string Required(string value, string name)
    {
        ArgumentNullException.ThrowIfNull(value, name);
        var normalized = value.Normalize(NormalizationForm.FormC);
        return string.IsNullOrWhiteSpace(normalized)
            ? throw new ArgumentException("Value must not be blank.", name)
            : normalized;
    }
}
