using AiDe.Core.Understanding;
using System.Diagnostics;
using System.Text;

namespace AiDe.Core.Tests.Understanding;

public sealed class AtlasDirectoryEnumeratorTests : IDisposable
{
    private readonly string _root = Path.Combine(Directory.GetCurrentDirectory(), ".atlas-inventory-tests", Guid.NewGuid().ToString("N"));
    private readonly string _escape = Path.Combine(Directory.GetCurrentDirectory(), ".atlas-inventory-tests", Guid.NewGuid().ToString("N"));
    private readonly List<(string Link, string TargetFile, byte[] Content)> _junctions = [];

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
        var enumerator = NewEnumerator();
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
        var enumerator = NewEnumerator();
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
        Assert.True(CreateJunction(junction, _escape, Path.Combine(_escape, "Secret.cs")), "Junction setup must succeed; otherwise this native proof did not execute.");
        var enumerator = NewEnumerator();
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
        var enumerator = NewEnumerator();
        var grant = Grant(enumerator);
        var limited = await enumerator.EnumerateAsync(grant, new EnumerationLimits(100, 10, 2, TimeSpan.FromSeconds(5)), CancellationToken.None);
        Assert.Equal(AtlasCompletionState.BudgetExceeded, limited.Completion);
        Assert.True(RenameRoundTrip(_root));

        using var cancel = new CancellationTokenSource();
        var canceling = NewEnumerator(() => cancel.Cancel());
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
    [InlineData("C:\\atlas:stream")]
    [InlineData("C:relative")]
    [InlineData("\\root-relative")]
    public async Task EnumerateAsync_RefusedInput_ReturnsRefusedBeforeEnumeration(string approvedRoot)
    {
        var enumerator = NewEnumerator();
        var identity = new AtlasObjectIdentity("vol", "idx");
        var grant = AtlasRootGrant.Create("grant:v1", "workspace", "root", "policy:test", "test", approvedRoot, identity, DateTimeOffset.UtcNow.AddMinutes(5));

        var observation = await enumerator.EnumerateAsync(grant, new EnumerationLimits(100, 8, 32, TimeSpan.FromSeconds(5)), CancellationToken.None);

        Assert.Equal(AtlasCompletionState.Refused, observation.Completion);
        Assert.Empty(observation.Entries);
    }

    [Fact]
    public async Task EnumerateAsync_ExpiredGrant_RefusesBeforeOpeningMissingRoot()
    {
        var enumerator = NewEnumerator();
        var grant = AtlasRootGrant.Create("grant:v1", "workspace", "root", "policy:test", "private-session",
            Path.Combine(_root, "missing"), new AtlasObjectIdentity("vol", "idx"), DateTimeOffset.MinValue);

        var result = await enumerator.EnumerateAsync(grant, new EnumerationLimits(100, 8, 32, TimeSpan.FromSeconds(5)), CancellationToken.None);

        Assert.Equal(AtlasCompletionState.Refused, result.Completion);
        Assert.Empty(result.Entries);
        Assert.Null(result.ObservedRootIdentity);
        Assert.Equal(0, result.Bounds.ReturnedRows);
        Assert.Equal(0, result.Bounds.ReturnedBytes);
    }

    [Fact]
    public async Task EnumerateAsync_DistinctInstances_KeysDoNotCollideAndMetadataHasNoPayloadBytes()
    {
        File.WriteAllBytes(Path.Combine(_root, "A.cs"), new byte[8192]);
        var first = NewEnumerator();
        var second = NewEnumerator();
        var grant = Grant(first);
        var limits = new EnumerationLimits(100, 8, 32, TimeSpan.FromSeconds(5));

        var results = await Task.WhenAll(first.EnumerateAsync(grant, limits, CancellationToken.None),
            second.EnumerateAsync(grant, limits, CancellationToken.None));

        Assert.NotEqual(results[0].ObservationKey, results[1].ObservationKey);
        Assert.All(results, result =>
        {
            Assert.Equal(0, result.Bounds.ReturnedBytes);
            Assert.Equal(8192, Assert.Single(result.Entries).LengthBytes);
            Assert.DoesNotContain(grant.SessionToken, result.ObservationKey, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task EnumerateAsync_RootBelowJunction_RefusesAncestorRedirection()
    {
        Directory.CreateDirectory(Path.Combine(_escape, "nested"));
        File.WriteAllText(Path.Combine(_escape, "nested", "secret.cs"), "synthetic");
        var junction = Path.Combine(_root, "redirect");
        Assert.True(CreateJunction(junction, _escape, Path.Combine(_escape, "nested", "secret.cs")));
        var enumerator = NewEnumerator();
        var redirected = Path.Combine(junction, "nested");
        var grant = AtlasRootGrant.Create("grant:v1", "workspace", "root", "policy:test", "test",
            redirected, enumerator.ObserveExpectedNativeRootIdentityForTest(redirected), DateTimeOffset.MaxValue);

        var result = await enumerator.EnumerateAsync(grant, new EnumerationLimits(100, 8, 32, TimeSpan.FromSeconds(5)), CancellationToken.None);

        Assert.Equal(AtlasCompletionState.Refused, result.Completion);
        Assert.Empty(result.Entries);
        Assert.True(RenameRoundTrip(Path.Combine(_escape, "nested")));
    }

    [Fact]
    public async Task EnumerateAsync_ConcurrentCalls_IsolatesBudgetsAndReleasesHandles()
    {
        Directory.CreateDirectory(Path.Combine(_root, "child"));
        File.WriteAllText(Path.Combine(_root, "child", "A.cs"), "synthetic");
        using var barrier = new Barrier(2);
        var arrivals = 0;
        var enumerator = NewEnumerator(() =>
        {
            if (Interlocked.Increment(ref arrivals) <= 2)
                Assert.True(barrier.SignalAndWait(TimeSpan.FromSeconds(10)), "Both native operations must overlap.");
        });
        var grant = Grant(enumerator);
        var limits = new EnumerationLimits(10, 8, 32, TimeSpan.FromSeconds(15));

        var results = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ =>
            Task.Run(() => enumerator.EnumerateAsync(grant, limits, CancellationToken.None))));

        Assert.All(results, result =>
        {
            Assert.Equal(AtlasCompletionState.Complete, result.Completion);
            Assert.Equal(2, result.Entries.Length);
            Assert.Equal(2, result.Bounds.TotalCount);
        });
        Assert.Equal(2, results.Select(result => result.ObservationKey).Distinct().Count());
        Assert.True(RenameRoundTrip(_root));
    }

    [Fact]
    public async Task EnumerateAsync_NoTrustedGrantCheck_RefusesPossessionAlone()
    {
        var enumerator = new AtlasDirectoryEnumerator();
        var grant = Grant(enumerator);

        var result = await enumerator.EnumerateAsync(grant, EnumerationLimits.CandidateDefault, CancellationToken.None);

        Assert.Equal(AtlasCompletionState.Refused, result.Completion);
        Assert.Empty(result.Entries);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EnumerateAsync_ExpiryOrRevocationDuringWork_DiscardsMetadataAndReleases(bool revoke)
    {
        File.WriteAllText(Path.Combine(_root, "A.cs"), "synthetic");
        var clock = new MutableClock();
        var live = true;
        var enumerator = new AtlasDirectoryEnumerator(() =>
        {
            if (revoke) live = false;
            else clock.Now = clock.Now.AddHours(1);
        }, clock, _ => live);
        var grant = AtlasRootGrant.Create("v1", "workspace", "root", "policy", "session", _root,
            enumerator.ObserveExpectedNativeRootIdentityForTest(_root), clock.Now.AddMinutes(1));

        var result = await enumerator.EnumerateAsync(grant, EnumerationLimits.CandidateDefault, CancellationToken.None);

        Assert.Equal(AtlasCompletionState.Refused, result.Completion);
        Assert.Empty(result.Entries);
        Assert.Equal(0, result.Bounds.ReturnedRows);
        Assert.Null(result.ObservedRootIdentity);
        Assert.True(RenameRoundTrip(_root));
    }

    [Fact]
    public async Task EnumerateAsync_PreCanceled_DoesNotOpenMissingRoot()
    {
        var grant = AtlasRootGrant.Create("v1", "workspace", "root", "policy", "session",
            Path.Combine(_root, "missing"), new AtlasObjectIdentity("v", "f"), DateTimeOffset.MaxValue);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var result = await NewEnumerator().EnumerateAsync(grant, EnumerationLimits.CandidateDefault, cancellation.Token);

        Assert.Equal(AtlasCompletionState.Canceled, result.Completion);
        Assert.Empty(result.Entries);
    }

    [Fact]
    public async Task EnumerateAsync_InspectedMetadata_BlocksReplacementAndWriteUntilReleased()
    {
        var path = Path.Combine(_root, "A.cs");
        File.WriteAllBytes(path, new byte[123]);
        var observations = 0;
        var enumerator = NewEnumerator(() =>
        {
            observations++;
            var rename = Assert.Throws<IOException>(() => File.Move(path, path + ".moved"));
            Assert.Equal(32, rename.HResult & 0xffff);
            var write = Assert.Throws<IOException>(() => File.WriteAllBytes(path, new byte[456]));
            Assert.Equal(32, write.HResult & 0xffff);
        });

        var result = await enumerator.EnumerateAsync(Grant(enumerator), EnumerationLimits.CandidateDefault, CancellationToken.None);

        var entry = Assert.Single(result.Entries);
        Assert.Equal(AtlasDirectoryEntryKind.File, entry.Kind);
        Assert.Equal(123, entry.LengthBytes);
        Assert.Equal(1, entry.LinkCount);
        Assert.Equal(1, observations);
        Assert.NotNull(entry.ObjectIdentity);
        File.Move(path, path + ".moved");
        File.WriteAllBytes(path + ".moved", new byte[456]);
        Assert.Equal(456, new FileInfo(path + ".moved").Length);
    }

    [Theory]
    [InlineData(2, 8, 32, "entries")]
    [InlineData(20, 1, 32, "depth")]
    [InlineData(20, 8, 1, "descriptors")]
    public async Task EnumerateAsync_Limits_ReportActualDimensionAndBoundedTelemetry(int entries, int depth, int descriptors, string dimension)
    {
        Directory.CreateDirectory(Path.Combine(_root, "child", "nested"));
        for (var i = 0; i < 8; i++) File.WriteAllText(Path.Combine(_root, $"f{i}.cs"), "synthetic");
        var activities = new System.Collections.Concurrent.ConcurrentBag<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "AiDe.Core.Understanding.AtlasDirectoryEnumerator",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => activities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);
        using var parent = new Activity("synthetic-budget-test").Start();
        var enumerator = NewEnumerator();

        var result = await enumerator.EnumerateAsync(Grant(enumerator),
            new EnumerationLimits(entries, depth, descriptors, TimeSpan.FromSeconds(5)), CancellationToken.None);

        Assert.Equal(AtlasCompletionState.BudgetExceeded, result.Completion);
        Assert.Equal(dimension, result.Bounds.LimitingDimension);
        Assert.Equal(AtlasDenominatorState.Unknown, result.Bounds.TotalState);
        Assert.Null(result.Bounds.TotalCount);
        Assert.InRange(result.Entries.Length, 0, entries);
        var activity = Assert.Single(activities, item => item.ParentId == parent.Id);
        Assert.InRange(Assert.IsType<int>(activity.GetTagItem("atlas.inventory.descriptors.peak")), 0, descriptors);
        Assert.InRange(Assert.IsType<int>(activity.GetTagItem("atlas.inventory.collection.peak")), 0, entries);
        Assert.IsType<double>(activity.GetTagItem("atlas.inventory.duration_ms"));
        Assert.All(activity.TagObjects, item => Assert.DoesNotContain(_root, item.Value?.ToString() ?? "", StringComparison.OrdinalIgnoreCase));
        Assert.True(RenameRoundTrip(_root));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task EnumerateAsync_ExactEntryBoundary_IsComplete(int count)
    {
        for (var i = 0; i < count; i++) File.WriteAllText(Path.Combine(_root, $"A{i}.cs"), "synthetic");
        var enumerator = NewEnumerator();

        var result = await enumerator.EnumerateAsync(Grant(enumerator), new EnumerationLimits(2, 8, 32, TimeSpan.FromSeconds(5)), CancellationToken.None);

        Assert.Equal(AtlasCompletionState.Complete, result.Completion);
        Assert.Equal(count, result.Bounds.TotalCount);
        Assert.Equal(count, result.Bounds.ReturnedRows);
    }

    private static AtlasDirectoryEnumerator NewEnumerator(Action? afterEntry = null) =>
        new(afterEntry, isGrantCurrent: _ => true);

    private sealed class MutableClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private AtlasRootGrant Grant(AtlasDirectoryEnumerator enumerator) =>
        AtlasRootGrant.Create("grant:v1", "workspace", "root:test", "policy:test", "test-session", _root,
            enumerator.ObserveExpectedNativeRootIdentityForTest(_root), DateTimeOffset.UtcNow.AddMinutes(5));

    public void Dispose()
    {
        foreach (var junction in _junctions)
        {
            Directory.Delete(junction.Link, recursive: false);
            Assert.False(Directory.Exists(junction.Link));
            Assert.True(File.Exists(junction.TargetFile));
            Assert.Equal(junction.Content, File.ReadAllBytes(junction.TargetFile));
        }
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

    private bool CreateJunction(string junction, string target, string targetFile)
    {
        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe", $"/c mklink /J \"{junction}\" \"{target}\"") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true });
        Assert.NotNull(process);
        if (!process.WaitForExit(5000))
        {
            process.Kill(entireProcessTree: true);
            Assert.Fail("Junction setup exceeded five seconds.");
        }
        Assert.Equal(0, process.ExitCode);
        _junctions.Add((junction, targetFile, File.ReadAllBytes(targetFile)));
        return process.ExitCode == 0;
    }

    private static void TryDelete(string path)
    {
        if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
    }
}
