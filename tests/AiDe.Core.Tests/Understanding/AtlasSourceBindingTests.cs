using AiDe.Core.Understanding;

namespace AiDe.Core.Tests.Understanding;

public sealed class AtlasSourceBindingTests
{
    private static readonly AtlasSourceBinding Binding = AtlasSourceBinding.Create(
        manifestIdentity: "manifest:42",
        manifestFileIdentity: "manifest-file:src/A.cs",
        policyIdentity: "policy:indexed-local-single-link",
        rootIdentity: "root:volume-1/file-10",
        fileIdentity: "file:volume-1/file-20",
        contentHash: "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

    [Fact]
    public void CompareTo_SelectedManifestRootFilePolicyAndHashMatch_ReturnsNone()
    {
        var comparison = Binding.CompareTo(
            manifestIdentity: "manifest:42",
            manifestFileIdentity: "manifest-file:src/A.cs",
            policyIdentity: "policy:indexed-local-single-link",
            rootIdentity: "root:volume-1/file-10",
            fileIdentity: "file:volume-1/file-20",
            contentHash: "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

        Assert.Equal(AtlasSourceBindingMismatch.None, comparison);
        Assert.True(Binding.Matches(
            "manifest:42",
            "manifest-file:src/A.cs",
            "policy:indexed-local-single-link",
            "root:volume-1/file-10",
            "file:volume-1/file-20",
            "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"));
    }

    [Theory]
    [InlineData("manifest:other", "manifest-file:src/A.cs", "policy:indexed-local-single-link", "root:volume-1/file-10", "file:volume-1/file-20", "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", AtlasSourceBindingMismatch.Manifest)]
    [InlineData("manifest:42", "manifest-file:src/B.cs", "policy:indexed-local-single-link", "root:volume-1/file-10", "file:volume-1/file-20", "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", AtlasSourceBindingMismatch.ManifestFile)]
    [InlineData("manifest:42", "manifest-file:src/A.cs", "policy:other", "root:volume-1/file-10", "file:volume-1/file-20", "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", AtlasSourceBindingMismatch.Policy)]
    [InlineData("manifest:42", "manifest-file:src/A.cs", "policy:indexed-local-single-link", "root:volume-2/file-10", "file:volume-1/file-20", "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", AtlasSourceBindingMismatch.Root)]
    [InlineData("manifest:42", "manifest-file:src/A.cs", "policy:indexed-local-single-link", "root:volume-1/file-10", "file:volume-1/file-21", "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", AtlasSourceBindingMismatch.File)]
    [InlineData("manifest:42", "manifest-file:src/A.cs", "policy:indexed-local-single-link", "root:volume-1/file-10", "file:volume-1/file-20", "sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", AtlasSourceBindingMismatch.Hash)]
    public void CompareTo_ComponentMismatch_ReturnsTheMismatchedComponent(
        string manifestIdentity,
        string manifestFileIdentity,
        string policyIdentity,
        string rootIdentity,
        string fileIdentity,
        string contentHash,
        AtlasSourceBindingMismatch expected)
    {
        var comparison = Binding.CompareTo(
            manifestIdentity,
            manifestFileIdentity,
            policyIdentity,
            rootIdentity,
            fileIdentity,
            contentHash);

        Assert.Equal(expected, comparison);
        Assert.False(Binding.Matches(
            manifestIdentity,
            manifestFileIdentity,
            policyIdentity,
            rootIdentity,
            fileIdentity,
            contentHash));
    }

    [Fact]
    public void Matches_SameHashWithDifferentManifestRootFileOrPolicy_ReturnsFalse()
    {
        const string sameHash = "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

        Assert.False(Binding.Matches("manifest:other", "manifest-file:src/A.cs", "policy:indexed-local-single-link", "root:volume-1/file-10", "file:volume-1/file-20", sameHash));
        Assert.False(Binding.Matches("manifest:42", "manifest-file:src/A.cs", "policy:other", "root:volume-1/file-10", "file:volume-1/file-20", sameHash));
        Assert.False(Binding.Matches("manifest:42", "manifest-file:src/A.cs", "policy:indexed-local-single-link", "root:other", "file:volume-1/file-20", sameHash));
        Assert.False(Binding.Matches("manifest:42", "manifest-file:src/A.cs", "policy:indexed-local-single-link", "root:volume-1/file-10", "file:other", sameHash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_BlankComponent_ThrowsArgumentException(string blank)
    {
        Assert.Throws<ArgumentException>(() => AtlasSourceBinding.Create(
            manifestIdentity: blank,
            manifestFileIdentity: "manifest-file:src/A.cs",
            policyIdentity: "policy:indexed-local-single-link",
            rootIdentity: "root:volume-1/file-10",
            fileIdentity: "file:volume-1/file-20",
            contentHash: "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"));
    }
}
