using AiDe.Core.Understanding;
using System.Text;

namespace AiDe.Core.Tests.Understanding;

public sealed class AtlasInventoryTests : IDisposable
{
    private readonly string _root = Path.Combine(Directory.GetCurrentDirectory(), ".atlas-inventory-tests", Guid.NewGuid().ToString("N"));

    public AtlasInventoryTests() => Directory.CreateDirectory(_root);

    [Fact]
    public async Task BuildManifestAsync_ObservationFiles_BuildsImmutableFileEntriesWithClassifications()
    {
        File.WriteAllText(Path.Combine(_root, "A.cs"), "class A {}", Encoding.UTF8);
        File.WriteAllText(Path.Combine(_root, "notes.md"), "# notes", Encoding.UTF8);
        var enumerator = new AtlasDirectoryEnumerator(isGrantCurrent: _ => true);
        var grant = Grant(enumerator);
        var inventory = new AtlasInventory(enumerator);

        var manifest = await inventory.BuildManifestAsync(grant, new EnumerationLimits(50, 4, 16, TimeSpan.FromSeconds(5)), AtlasInventoryPolicy.NonGit("manual-fixture"), CancellationToken.None);

        Assert.Equal(AtlasCompletionState.Complete, manifest.Completion);
        Assert.Contains(manifest.Files, f => f.RelativePath == "A.cs" && f.Classification == AtlasFileClassification.CSharp && f.Availability == AtlasFileAvailability.Available);
        Assert.Contains(manifest.Files, f => f.RelativePath == "notes.md" && f.Classification == AtlasFileClassification.Text);
        Assert.Contains(manifest.Files, f => f.FileValue == AtlasIdentityCodec.ForFile(grant.WorkspaceToken, grant.RootToken, "A.cs"));
        Assert.Contains(manifest.Files, f => f.FileValue == AtlasIdentityCodec.ForFile(grant.WorkspaceToken, grant.RootToken, "notes.md"));
    }

    [Fact]
    public async Task BuildManifestAsync_UnavailableMembership_DoesNotInventGitAuthorization()
    {
        File.WriteAllText(Path.Combine(_root, "A.cs"), "class A {}", Encoding.UTF8);
        var enumerator = new AtlasDirectoryEnumerator(isGrantCurrent: _ => true);
        var grant = Grant(enumerator);
        var inventory = new AtlasInventory(enumerator);

        var manifest = await inventory.BuildManifestAsync(grant, new EnumerationLimits(50, 4, 16, TimeSpan.FromSeconds(5)), AtlasInventoryPolicy.UnavailableMembership("git-not-established"), CancellationToken.None);

        Assert.Equal(AtlasCompletionState.Partial, manifest.Completion);
        Assert.Equal(AtlasDenominatorState.Unknown, manifest.Bounds.TotalState);
        Assert.Contains(manifest.Bounds.OmissionReason!, "git-not-established", StringComparison.Ordinal);
        Assert.Empty(manifest.Files);
        Assert.Equal(0, manifest.Bounds.ReturnedRows);
        Assert.Equal(0, manifest.Bounds.ReturnedBytes);
        Assert.Null(manifest.Bounds.TotalCount);
    }

    [Fact]
    public async Task BuildManifestAsync_NestedMetadata_UsesRealParentsAndKeepsUnsupportedFiles()
    {
        Directory.CreateDirectory(Path.Combine(_root, "generated"));
        Directory.CreateDirectory(Path.Combine(_root, "vendor"));
        Directory.CreateDirectory(Path.Combine(_root, "migrations"));
        File.WriteAllText(Path.Combine(_root, "generated", "A.g.cs"), "synthetic");
        File.WriteAllText(Path.Combine(_root, "vendor", "blob.bin"), "synthetic");
        File.WriteAllText(Path.Combine(_root, "migrations", "001.sql"), "synthetic");
        var enumerator = new AtlasDirectoryEnumerator(isGrantCurrent: _ => true);
        var grant = Grant(enumerator);

        var manifest = await new AtlasInventory(enumerator).BuildManifestAsync(grant,
            new EnumerationLimits(50, 8, 32, TimeSpan.FromSeconds(5)), AtlasInventoryPolicy.NonGit("explicit-fixture"), CancellationToken.None);

        Assert.Equal(AtlasCompletionState.Complete, manifest.Completion);
        Assert.Equal(6, manifest.Files.Length);
        Assert.Equal(0, manifest.Bounds.ReturnedBytes);
        Assert.Equal(6, manifest.Bounds.TotalCount);
        foreach (var file in manifest.Files)
        {
            var slash = file.RelativePath.LastIndexOf('/');
            var expectedParent = slash < 0 ? grant.RootToken
                : AtlasIdentityCodec.ForFile(grant.WorkspaceToken, grant.RootToken, file.RelativePath[..slash]);
            Assert.Equal(expectedParent, file.ParentPathKey);
            if (slash >= 0) Assert.Contains(manifest.Files, parent => parent.FileValue == file.ParentPathKey);
        }

        Assert.Contains(manifest.Files, file => file.RelativePath == "generated/A.g.cs" && file.Classification == AtlasFileClassification.CSharp);
        Assert.Contains(manifest.Files, file => file.RelativePath == "vendor/blob.bin" && file.Classification == AtlasFileClassification.Unknown);
        Assert.Contains(manifest.Files, file => file.RelativePath == "migrations/001.sql" && file.Classification == AtlasFileClassification.Unknown);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task BuildManifestAsync_ExplicitMembership_HidesOthersAndStatesCoverage(bool complete)
    {
        Directory.CreateDirectory(Path.Combine(_root, "generated"));
        File.WriteAllText(Path.Combine(_root, "generated", "A.g.cs"), "synthetic");
        File.WriteAllText(Path.Combine(_root, "hidden.cs"), "synthetic");
        var enumerator = new AtlasDirectoryEnumerator(isGrantCurrent: _ => true);
        var grant = Grant(enumerator);
        var members = new List<string> { "generated/A.g.cs" };
        var policy = AtlasInventoryPolicy.KnownMembership(grant, members, complete, "synthetic-membership");
        members.Add("hidden.cs");

        var manifest = await new AtlasInventory(enumerator).BuildManifestAsync(grant,
            EnumerationLimits.CandidateDefault, policy, CancellationToken.None);

        Assert.Equal(complete ? AtlasCompletionState.Complete : AtlasCompletionState.Partial, manifest.Completion);
        Assert.Equal(2, manifest.Files.Length);
        Assert.DoesNotContain(manifest.Files, file => file.RelativePath == "hidden.cs");
        Assert.Equal(2, manifest.Bounds.ReturnedRows);
        Assert.Equal(complete ? 2L : null, manifest.Bounds.TotalCount);
        Assert.Equal(complete ? AtlasDenominatorState.Known : AtlasDenominatorState.Unknown, manifest.Bounds.TotalState);
        Assert.Contains(manifest.Files, file => file.RelativePath == "generated");
    }

    [Fact]
    public async Task BuildManifestAsync_MembershipBoundToAnotherGrant_RefusesWithoutPhysicalRead()
    {
        var enumerator = new AtlasDirectoryEnumerator(isGrantCurrent: _ => throw new InvalidOperationException("Must not enumerate"));
        var grant = Grant(enumerator);
        var policy = AtlasInventoryPolicy.KnownMembership(grant, ["A.cs"], true, "synthetic-membership");

        var manifest = await new AtlasInventory(enumerator).BuildManifestAsync(Grant(enumerator),
            EnumerationLimits.CandidateDefault, policy, CancellationToken.None);

        Assert.Equal(AtlasCompletionState.Refused, manifest.Completion);
        Assert.Empty(manifest.Files);
        Assert.Null(manifest.Bounds.TotalCount);
    }

    [Fact]
    public async Task BuildManifestAsync_UnavailableMembership_DoesNotReadPhysicalTree()
    {
        var enumerator = new AtlasDirectoryEnumerator(isGrantCurrent: _ => throw new InvalidOperationException("Must not enumerate"));
        var manifest = await new AtlasInventory(enumerator).BuildManifestAsync(Grant(enumerator),
            EnumerationLimits.CandidateDefault, AtlasInventoryPolicy.UnavailableMembership("not-established"), CancellationToken.None);

        Assert.Equal(AtlasCompletionState.Partial, manifest.Completion);
        Assert.Empty(manifest.Files);
    }

    private AtlasRootGrant Grant(AtlasDirectoryEnumerator enumerator) =>
        AtlasRootGrant.Create("grant:v1", "workspace", "root:test", "policy:test", "test-session", _root,
            enumerator.ObserveExpectedNativeRootIdentityForTest(_root), DateTimeOffset.UtcNow.AddMinutes(5));

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }
}
