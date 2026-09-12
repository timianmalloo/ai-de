using AiDe.Core.Understanding;
using System.Runtime.InteropServices;
using System.Text;

namespace AiDe.Core.Tests.Understanding;

public sealed class AtlasDirectoryEnumeratorTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "atlas-inventory", Guid.NewGuid().ToString("N"));
    private readonly string _escape = Path.Combine(Path.GetTempPath(), "atlas-inventory-escape", Guid.NewGuid().ToString("N"));

    public AtlasDirectoryEnumeratorTests()
    {
        Directory.CreateDirectory(_root);
        Directory.CreateDirectory(_escape);
    }

    [Fact]
    public async Task EnumerateAsync_OrdinaryFiles_ReturnsRelativeEntriesAndManifestFiles()
    {
        Directory.CreateDirectory(Path.Combine(_root, "src", "nested"));
        File.WriteAllText(Path.Combine(_root, "src", "A.cs"), "class A {}", Encoding.UTF8);
        File.WriteAllText(Path.Combine(_root, "src", "readme.md"), "# readme", Encoding.UTF8);
        File.WriteAllText(Path.Combine(_root, "src", "nested", "B.txt"), "text", Encoding.UTF8);
        var enumerator = new AtlasDirectoryEnumerator();
        var grant = Grant(enumerator);

        var observation = await enumerator.EnumerateAsync(grant, new EnumerationLimits(100, 8, 32, TimeSpan.FromSeconds(5)), CancellationToken.None);

        Assert.Equal(AtlasCompletionState.Complete, observation.Completion);
        Assert.Contains(observation.Entries, e => e.RelativePath == "src/A.cs" && e.Kind == AtlasDirectoryEntryKind.File && e.ObjectIdentity is not null && e.LinkCount == 1);
        Assert.Contains(observation.Entries, e => e.RelativePath == "src/readme.md" && e.Kind == AtlasDirectoryEntryKind.File);
        Assert.Contains(observation.Entries, e => e.RelativePath == "src/nested" && e.Kind == AtlasDirectoryEntryKind.Directory);
        Assert.All(observation.Entries, e => Assert.DoesNotContain(_root, e.RelativePath, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(grant.ExpectedNativeRootIdentity, observation.ObservedRootIdentity);
    }

    [Fact]
    public async Task EnumerateAsync_ReplacedRootGrant_ReturnsRefusedWithoutEntries()
    {
        File.WriteAllText(Path.Combine(_root, "A.cs"), "class A {}", Encoding.UTF8);
        var enumerator = new AtlasDirectoryEnumerator();
        var grant = Grant(enumerator);
        var old = _root + ".old";
        Directory.Move(_root, old);
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "A.cs"), "class A {}", Encoding.UTF8);
        try
        {
            var observation = await enumerator.EnumerateAsync(grant, new EnumerationLimits(100, 8, 32, TimeSpan.FromSeconds(5)), CancellationToken.None);

            Assert.Equal(AtlasCompletionState.Refused, observation.Completion);
            Assert.Empty(observation.Entries);
            Assert.Contains("root identity", observation.Bounds.OmissionReason, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(_root, recursive: true);
            Directory.Move(old, _root);
        }
    }

    [Fact]
    public async Task EnumerateAsync_DirectAndParentListedJunctions_DoNotExposeOutsideNamesOrCounts()
    {
        Directory.CreateDirectory(Path.Combine(_root, "src"));
        File.WriteAllText(Path.Combine(_escape, "Secret.cs"), "class Secret {}", Encoding.UTF8);
        var junction = Path.Combine(_root, "src", "outside");
        if (!CreateJunction(junction, _escape)) return;
        var enumerator = new AtlasDirectoryEnumerator();
        var grant = Grant(enumerator);

        var parent = await enumerator.EnumerateAsync(grant, new EnumerationLimits(100, 8, 32, TimeSpan.FromSeconds(5)), CancellationToken.None);
        var directGrant = AtlasRootGrant.Create("grant:v1", "workspace", "root", "policy:test", "test", junction, grant.ExpectedNativeRootIdentity, DateTimeOffset.UtcNow.AddMinutes(5));
        var direct = await enumerator.EnumerateAsync(directGrant, new EnumerationLimits(100, 8, 32, TimeSpan.FromSeconds(5)), CancellationToken.None);

        Assert.DoesNotContain(parent.Entries, e => e.RelativePath.Contains("Secret", StringComparison.Ordinal));
        Assert.Contains(parent.Entries, e => e.RelativePath == "src/outside" && e.Kind == AtlasDirectoryEntryKind.RejectedLink);
        Assert.Equal(AtlasCompletionState.Refused, direct.Completion);
        Assert.Empty(direct.Entries);
    }

    [Fact]
    public async Task EnumerateAsync_BoundsAndCancellation_DisposeHandlesBeforeReturn()
    {
        Directory.CreateDirectory(Path.Combine(_root, "deep", "a", "b", "c"));
        var enumerator = new AtlasDirectoryEnumerator();
        var grant = Grant(enumerator);
        var limited = await enumerator.EnumerateAsync(grant, new EnumerationLimits(100, 10, 2, TimeSpan.FromSeconds(5)), CancellationToken.None);
        Assert.Equal(AtlasCompletionState.BudgetExceeded, limited.Completion);
        Assert.True(RenameRoundTrip(_root));

        using var cancel = new CancellationTokenSource();
        var canceling = new AtlasDirectoryEnumerator(() => cancel.Cancel());
        var canceled = await canceling.EnumerateAsync(grant, new EnumerationLimits(100, 10, 32, TimeSpan.FromSeconds(5)), cancel.Token);
        Assert.Equal(AtlasCompletionState.Canceled, canceled.Completion);
        Assert.Empty(canceled.Entries);
        Assert.True(RenameRoundTrip(Path.Combine(_root, "deep")));
    }

    [Theory]
    [InlineData("..")]
    [InlineData("src:stream")]
    [InlineData("\\\\.\\NUL")]
    [InlineData("\\\\server\\share")]
    public async Task EnumerateAsync_RefusedInput_ReturnsRefusedBeforeEnumeration(string approvedRoot)
    {
        var enumerator = new AtlasDirectoryEnumerator();
        var identity = new AtlasObjectIdentity("vol", "idx");
        var grant = AtlasRootGrant.Create("grant:v1", "workspace", "root", "policy:test", "test", approvedRoot, identity, DateTimeOffset.UtcNow.AddMinutes(5));

        var observation = await enumerator.EnumerateAsync(grant, new EnumerationLimits(100, 8, 32, TimeSpan.FromSeconds(5)), CancellationToken.None);

        Assert.Equal(AtlasCompletionState.Refused, observation.Completion);
        Assert.Empty(observation.Entries);
    }

    private AtlasRootGrant Grant(AtlasDirectoryEnumerator enumerator) =>
        AtlasRootGrant.Create("grant:v1", "workspace", "root:test", "policy:test", "test-session", _root,
            enumerator.ObserveExpectedNativeRootIdentityForTest(_root), DateTimeOffset.UtcNow.AddMinutes(5));

    public void Dispose()
    {
        TryDelete(_root);
        TryDelete(_escape);
    }

    private static bool RenameRoundTrip(string path)
    {
        var moved = path + ".moved";
        Directory.Move(path, moved);
        Directory.Move(moved, path);
        return true;
    }

    private static bool CreateJunction(string junction, string target)
    {
        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe", $"/c mklink /J \"{junction}\" \"{target}\"") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true });
        process!.WaitForExit(5000);
        return process.ExitCode == 0;
    }

    private static void TryDelete(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
