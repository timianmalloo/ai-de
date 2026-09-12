using AiDe.Core.Understanding;

namespace AiDe.Core.Tests.Understanding;

public sealed class AtlasManifestTests
{
    [Fact]
    public void Create_WithoutExpectedRootIdentity_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => AtlasRootGrant.Create(
            "grant:v1", "workspace", "root", "policy", "session", "C:\\repo", null!, DateTimeOffset.UtcNow.AddMinutes(5)));
    }

    [Fact]
    public void Create_MutableEntrySource_CapturesImmutableSnapshot()
    {
        var entries = new List<AtlasDirectoryEntry>
        {
            new("src/A.cs", AtlasDirectoryEntryKind.File, null, 10, 1, null),
        };

        var observation = new DirectoryObservation(
            "directory-observation:1", Identity(), Identity(), AtlasCompletionState.Partial, 7, BoundsUnknown(), entries);
        entries.Add(new AtlasDirectoryEntry("src/B.cs", AtlasDirectoryEntryKind.File, null, 20, 1, null));

        Assert.Single(observation.Entries);
    }

    [Fact]
    public void Create_DefaultIdentity_Throws()
    {
        Assert.Throws<ArgumentException>(() => new AtlasObjectIdentity("", "file:2"));
        Assert.Throws<ArgumentException>(() => new AtlasObjectIdentity("volume:1", ""));
    }

    [Fact]
    public void Create_ZeroLengthSpanIsValidButOverflowEndThrows()
    {
        var empty = new AtlasTextSpan(0, 0);

        Assert.Equal(0, empty.End);
        Assert.Throws<OverflowException>(() => new AtlasTextSpan(int.MaxValue, 1));
    }

    [Fact]
    public void Create_MissingIdentityAndUnknownCounts_AreExplicitAbsenceNotZero()
    {
        var entry = new AtlasDirectoryEntry("src/A.cs", AtlasDirectoryEntryKind.Unavailable, null, null, null, "access-denied");
        var bounds = BoundsUnknown();

        Assert.Null(entry.ObjectIdentity);
        Assert.Null(entry.LengthBytes);
        Assert.Null(entry.LinkCount);
        Assert.Equal(AtlasDenominatorState.Unknown, bounds.TotalState);
        Assert.Null(bounds.TotalCount);
        Assert.Throws<ArgumentException>(() => new AtlasBounds(10, 10, 1, 20, null, AtlasDenominatorState.Known, null, null));
        Assert.Throws<ArgumentException>(() => new AtlasBounds(10, 10, 1, 20, 0, AtlasDenominatorState.Unknown, "not-counted", "entries"));
    }

    [Fact]
    public void Create_ReturnedRowsExceedLimitOrKnownTotal_Throws()
    {
        Assert.Throws<ArgumentException>(() => new AtlasBounds(10, 10, 11, 20, 11, AtlasDenominatorState.Known, null, null));
        Assert.Throws<ArgumentException>(() => new AtlasBounds(10, 10, 2, 20, 1, AtlasDenominatorState.Known, null, null));
        Assert.Throws<ArgumentException>(() => new AtlasBounds(10, 10, 1, 20, null, AtlasDenominatorState.Unknown, null, null));
    }

    [Fact]
    public void Create_PublicSourceConstructorWithNonVerifiedStatus_Throws()
    {
        Assert.Throws<ArgumentException>(() => new AtlasSourceObservation(
            "source:bad",
            "manifest:1",
            "file:value",
            "policy",
            Identity(),
            Identity(),
            "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            12,
            "utf-8",
            12,
            AtlasSourceObservationStatus.Unavailable,
            BoundsKnown(1)));
    }

    [Fact]
    public void Create_FailureSourceWithoutMetadata_SucceedsAndVerifiedWithoutMetadataThrows()
    {
        var failed = AtlasSourceObservation.Unavailable("source:failed", "manifest:1", "file:value", "policy", BoundsUnknown(), "access-denied");

        Assert.Null(failed.CanonicalSha256);
        Assert.Null(failed.RootIdentity);
        Assert.Null(failed.DecoderId);
        Assert.Throws<ArgumentNullException>(() => AtlasSourceObservation.Verified("source:bad", "manifest:1", "file:value", "policy", null!, Identity(), "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", 12, "utf-8", 12, BoundsKnown(1)));
    }

    [Fact]
    public void Create_ManifestContradictoryBinding_ThrowsEvenWhenPartial()
    {
        var grant = Grant();
        var file = new AtlasFileEntry("file:value", "src/A.cs", "src", AtlasDirectoryEntryKind.File, AtlasFileClassification.CSharp, Identity(), AtlasFileAvailability.Available, null);
        var source = AtlasSourceObservation.Verified("source:1", "manifest:1", "file:value", "policy", Identity(), Identity(), "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", 12, "utf-8", 12, BoundsKnown(1));
        var wrongBinding = AtlasSourceBinding.Create("manifest:1", "file:value", "other-policy", NativeToken(Identity()), NativeToken(Identity()), "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
        var declaration = new AtlasDeclaration("decl:1", "symbol:value", "source:1", "context:1", wrongBinding, AtlasDeclarationKind.Method, AtlasDeclarationRole.Ordinary, "M()", "M", new AtlasTextSpan(0, 1), new AtlasTextSpan(0, 4), null, null);

        Assert.Throws<ArgumentException>(() => new AtlasManifest("manifest:1", grant, "directory-observation:1", [file], [source], [declaration], AtlasCompletionState.Partial, BoundsKnown(1)));
    }

    [Fact]
    public void Create_RootGrantAndManifest_DoNotExposeMutableStateOrTextBodies()
    {
        var grant = AtlasRootGrant.Create("grant:v1", "workspace", "root", "policy", "session", "C:\\repo", Identity(), DateTimeOffset.UtcNow.AddMinutes(5));
        var file = new AtlasFileEntry("file:value", "src/A.cs", "src", AtlasDirectoryEntryKind.File, AtlasFileClassification.CSharp, null, AtlasFileAvailability.Available, null);
        var source = AtlasSourceObservation.Verified("source:1", "manifest:1", "file:value", "policy", Identity(), Identity(), "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", 12, "utf-8", 12, BoundsKnown(1));
        var declaration = new AtlasDeclaration("decl:1", "symbol:value", "source:1", "context:1", AtlasSourceBinding.Create("manifest:1", "file:value", "policy", NativeToken(Identity()), NativeToken(Identity()), "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"), AtlasDeclarationKind.Method, AtlasDeclarationRole.Ordinary, "M()", "M", new AtlasTextSpan(0, 1), new AtlasTextSpan(0, 4), null, "body-not-requested");
        var manifest = new AtlasManifest("manifest:1", grant, "directory-observation:1", [file], [source], [declaration], AtlasCompletionState.Partial, BoundsKnown(1));

        Assert.Equal("manifest:1", manifest.Token);
        Assert.Null(manifest.Declarations.Single().BodySpan);
        Assert.DoesNotContain(typeof(AtlasManifest).GetProperties(), property => property.Name.Contains("Text", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(typeof(AtlasManifest).GetProperties(), property => property.Name.Contains("Compilation", StringComparison.OrdinalIgnoreCase));
    }

    private static AtlasRootGrant Grant() => AtlasRootGrant.Create("grant:v1", "workspace", "root", "policy", "session", "C:\\repo", Identity(), DateTimeOffset.UtcNow.AddMinutes(5));
    private static AtlasObjectIdentity Identity() => new("volume:1", "file:2");
    private static string NativeToken(AtlasObjectIdentity identity) => AtlasIdentityCodec.ForNativeObject(identity);
    private static AtlasBounds BoundsUnknown() => new(128, 64, 1, 10, null, AtlasDenominatorState.Unknown, "not-counted", "entries");
    private static AtlasBounds BoundsKnown(long total) => new(128, 64, 1, 10, total, AtlasDenominatorState.Known, null, null);
}
