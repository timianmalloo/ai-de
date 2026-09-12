using AiDe.Core.Understanding;
using System.Text;

namespace AiDe.Core.Tests.Understanding;

public sealed class AtlasInventoryTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "atlas-inventory-manifest", Guid.NewGuid().ToString("N"));

    public AtlasInventoryTests() => Directory.CreateDirectory(_root);

    [Fact]
    public async Task BuildManifestAsync_ObservationFiles_BuildsImmutableFileEntriesWithClassifications()
    {
        File.WriteAllText(Path.Combine(_root, "A.cs"), "class A {}", Encoding.UTF8);
        File.WriteAllText(Path.Combine(_root, "notes.md"), "# notes", Encoding.UTF8);
        var enumerator = new AtlasDirectoryEnumerator();
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
        var enumerator = new AtlasDirectoryEnumerator();
        var grant = Grant(enumerator);
        var inventory = new AtlasInventory(enumerator);

        var manifest = await inventory.BuildManifestAsync(grant, new EnumerationLimits(50, 4, 16, TimeSpan.FromSeconds(5)), AtlasInventoryPolicy.UnavailableMembership("git-not-established"), CancellationToken.None);

        Assert.Equal(AtlasCompletionState.Partial, manifest.Completion);
        Assert.Equal(AtlasDenominatorState.Unknown, manifest.Bounds.TotalState);
        Assert.Contains(manifest.Bounds.OmissionReason!, "git-not-established", StringComparison.Ordinal);
        Assert.All(manifest.Files, f => Assert.NotEqual("git-tracked", f.ParentPathKey));
    }

    private AtlasRootGrant Grant(AtlasDirectoryEnumerator enumerator) =>
        AtlasRootGrant.Create("grant:v1", "workspace", "root:test", "policy:test", "test-session", _root,
            enumerator.ObserveExpectedNativeRootIdentityForTest(_root), DateTimeOffset.UtcNow.AddMinutes(5));

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { }
    }
}
