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
        Assert.Throws<ArgumentException>(() => new AtlasBounds(10, 10, 1, 20, 0, AtlasDenominatorState.Unknown, null, null));
    }

    [Fact]
    public void Create_RootGrantAndManifest_DoNotExposeMutableStateOrTextBodies()
    {
        var grant = AtlasRootGrant.Create("grant:v1", "workspace", "root", "policy", "session", "C:\\repo", Identity(), DateTimeOffset.UtcNow.AddMinutes(5));
        var file = new AtlasFileEntry("file:value", "src/A.cs", "src", AtlasDirectoryEntryKind.File, AtlasFileClassification.CSharp, null, AtlasFileAvailability.Available, null);
        var source = new AtlasSourceObservation("source:1", "manifest:1", "file:value", "policy", Identity(), Identity(), "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", 12, "utf-8", 12, AtlasSourceObservationStatus.Verified, BoundsKnown(1));
        var declaration = new AtlasDeclaration("decl:1", "symbol:value", "source:1", "context:1", AtlasSourceBinding.Create("manifest:1", "file:value", "policy", "root", "file", "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"), AtlasDeclarationKind.Method, AtlasDeclarationRole.Ordinary, "M()", "M", new AtlasTextSpan(0, 1), new AtlasTextSpan(0, 4), null, "body-not-requested");
        var manifest = new AtlasManifest("manifest:1", grant, "directory-observation:1", [file], [source], [declaration], AtlasCompletionState.Partial, BoundsKnown(1));

        Assert.Equal("manifest:1", manifest.Token);
        Assert.Null(manifest.Declarations.Single().BodySpan);
        Assert.DoesNotContain(typeof(AtlasManifest).GetProperties(), property => property.Name.Contains("Text", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(typeof(AtlasManifest).GetProperties(), property => property.Name.Contains("Compilation", StringComparison.OrdinalIgnoreCase));
    }

    private static AtlasObjectIdentity Identity() => new("volume:1", "file:2");
    private static AtlasBounds BoundsUnknown() => new(128, 64, 1, 10, null, AtlasDenominatorState.Unknown, "not-counted", "entries");
    private static AtlasBounds BoundsKnown(long total) => new(128, 64, 1, 10, total, AtlasDenominatorState.Known, null, null);
}
