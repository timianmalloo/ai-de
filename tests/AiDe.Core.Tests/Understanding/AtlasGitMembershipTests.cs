using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Security.Cryptography;
using System.Text;
using AiDe.Core.Understanding;

namespace AiDe.Core.Tests.Understanding;

public sealed class AtlasGitMembershipTests : IDisposable
{
    private const string Git = @"C:\Program Files\Git\cmd\git.exe";
    private const string Version = "git version 2.55.0.windows.2";
    private readonly string _root = Path.Combine(AppContext.BaseDirectory, ".artifacts", "membership-fixtures", Guid.NewGuid().ToString("N"));

    public AtlasGitMembershipTests() => Directory.CreateDirectory(_root);

    [Theory]
    [InlineData("clone", false)]
    [InlineData("clone", true)]
    [InlineData("linked", false)]
    [InlineData("linked", true)]
    [InlineData("nested", false)]
    [InlineData("nested", true)]
    public async Task RequiredRepositoryFormsRetainNativeAssociationAndSourceBoundary(string form, bool packed)
    {
        var repository = await Clone();
        if (form == "linked")
        {
            var linked = Path.Combine(_root, "linked");
            await Command(repository, "worktree", "add", "--quiet", "-b", "linked", linked);
            repository = linked;
            Assert.True(File.Exists(Path.Combine(repository, ".git")));
        }
        if (packed)
        {
            await Command(repository, "pack-refs", "--all", "--prune");
        }
        var sourceRoot = form == "nested" ? Path.Combine(repository, "src") : repository;
        await using var snapshot = await Capture(sourceRoot);
        AssertCandidate(snapshot);
        Assert.Equal(6, snapshot.Invocations);
        Assert.Equal(repository, snapshot.Association!.Repository, ignoreCase: true);
        Assert.Equal(form == "nested" ? 1 : 2, snapshot.MembershipTotal);
        Assert.DoesNotContain(snapshot.Entries, entry => entry.RelativePath.Contains("untracked", StringComparison.Ordinal));
        Assert.Contains(snapshot.Entries, entry => entry.RelativePath == (form == "nested" ? "a.cs" : @"src\a.cs"));
        Assert.DoesNotContain(snapshot.Entries, entry => Path.IsPathRooted(entry.RelativePath) || entry.RelativePath.Contains("..", StringComparison.Ordinal));
        Assert.NotNull(snapshot.Head);
        Assert.Equal(64, snapshot.IndexDigest!.Length);
        Assert.True(snapshot.IsCurrent());
    }

    [Fact]
    public async Task HostileFsmonitorControlExecutesButGuardedCaptureDoesNot()
    {
        var repository = await Clone();
        var marker = Path.Combine(_root, "executed.marker");
        var hook = Path.Combine(_root, "monitor.cmd");
        File.WriteAllText(hook, $"@echo off\r\necho fired > \"{marker}\"\r\nexit /b 1\r\n", Encoding.ASCII);
        await Command(repository, "config", "core.fsmonitor", $"\"{hook}\"");
        await Command(repository, "ls-files", "--cached", "--stage", "-z", allowFailure: true);
        Assert.True(File.Exists(marker), "Hostile fixture must execute in the unguarded control.");
        File.Delete(marker);
        await Command(repository, "config", "credential.helper", $"!\"{hook}\"");
        await Command(repository, "config", "filter.evil.clean", $"\"{hook}\"");
        await Command(repository, "config", "core.hooksPath", _root);
        await Command(repository, "config", "remote.origin.url", $"ext::{hook}");
        await Command(repository, "config", "protocol.ext.allow", "always");
        await using var snapshot = await Capture(repository);
        AssertCandidate(snapshot);
        Assert.False(File.Exists(marker));
        Assert.True(snapshot.IsCurrent());
    }

    [Fact]
    public async Task NativePinsExcludeRootIndexAndLooseRefReplacementAndReleaseOnDispose()
    {
        var repository = await Clone();
        await using var snapshot = await Capture(repository);
        AssertCandidate(snapshot);
        var index = snapshot.Association!.Index;
        var reference = Path.Combine(snapshot.Association.CommonDirectory, "refs", "heads", "main");
        Assert.ThrowsAny<IOException>(() => Directory.Move(repository, repository + "-moved"));
        Assert.ThrowsAny<IOException>(() => File.WriteAllBytes(index, []));
        Assert.ThrowsAny<IOException>(() => File.WriteAllText(reference, new string('0', 40)));
        Assert.True(snapshot.IsCurrent());
        await snapshot.DisposeAsync();
        Assert.False(snapshot.IsCurrent());
        File.WriteAllText(Path.Combine(repository, "after-release"), "released");
        Directory.Move(repository, repository + "-moved");
    }

    [Fact]
    public async Task PackedRefNamespaceAbaIsDetectedWithoutRelyingOnContentHashes()
    {
        var repository = await Clone();
        await Command(repository, "pack-refs", "--all", "--prune");
        await using var snapshot = await Capture(repository);
        AssertCandidate(snapshot);
        var reference = Path.Combine(snapshot.Association!.CommonDirectory, "refs", "heads", "main");
        Assert.False(File.Exists(reference));
        File.WriteAllText(reference, snapshot.Head + "\n");
        File.Delete(reference);
        Assert.False(File.Exists(reference));
        Assert.False(snapshot.IsCurrent(), "Kernel namespace evidence must detect insertion/removal ABA even though HEAD bytes agree.");
    }

    [Fact]
    public async Task MissingIndexAndWrongPinsAreNotCompleteEmptyMembership()
    {
        var repository = await Clone();
        var member = NewMembership();
        var actual = Identity(repository);
        await using var wrongRoot = await member.CaptureForQualificationAsync(repository, new AtlasObjectIdentity("0", "0"), CancellationToken.None);
        Assert.Equal(AtlasMembershipCaptureState.Refused, wrongRoot.State);
        Assert.Null(wrongRoot.MembershipTotal);
        var wrongDigest = new AtlasGitMembership(Git, new string('0', 64), Version);
        await using var wrongBinary = await wrongDigest.CaptureForQualificationAsync(repository, actual, CancellationToken.None);
        Assert.Equal(AtlasMembershipCaptureState.Refused, wrongBinary.State);
        Assert.Null(wrongBinary.MembershipTotal);
        File.Delete(Path.Combine(repository, ".git", "index"));
        await using var missing = await member.CaptureForQualificationAsync(repository, actual, CancellationToken.None);
        Assert.NotEqual(AtlasMembershipCaptureState.CandidateComplete, missing.State);
        Assert.Null(missing.MembershipTotal);
        Assert.Empty(missing.Entries);
    }

    [Fact]
    public async Task CancellationReapsItsOwnedProcessAndEmitsCaptureMeasurement()
    {
        var repository = await Clone();
        using var cancellation = new CancellationTokenSource();
        var processIds = new List<int>();
        var member = NewMembership();
        member.ProcessStartedForQualification = id => { processIds.Add(id); cancellation.Cancel(); };
        long captures = 0;
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, sink) =>
        {
            if (instrument.Meter.Name == "AiDe.Core.AtlasGitMembership" && instrument.Name == "atlas.membership.captures")
                sink.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, value, _, _) => Interlocked.Add(ref captures, value));
        listener.Start();
        await using var snapshot = await member.CaptureForQualificationAsync(repository, Identity(repository), cancellation.Token);
        Assert.Equal(AtlasMembershipCaptureState.Canceled, snapshot.State);
        Assert.Single(processIds);
        foreach (var id in processIds)
        {
            try
            {
                using var process = Process.GetProcessById(id);
                Assert.True(process.HasExited);
            }
            catch (ArgumentException) { }
        }
        Assert.True(captures >= 1);
        Assert.True(snapshot.Elapsed >= TimeSpan.Zero);
    }

    [Theory]
    [InlineData("missing-terminator")]
    [InlineData("invalid-stage")]
    [InlineData("duplicate")]
    [InlineData("traversal")]
    [InlineData("bad-mode")]
    [InlineData("invalid-utf8")]
    public void MalformedMembershipCannotBecomeAnEmptySuccess(string mutation)
    {
        var row = "100644 " + new string('a', 40) + " 0\tfile.cs\0";
        var body = Encoding.UTF8.GetBytes(mutation switch
        {
            "missing-terminator" => row[..^1],
            "invalid-stage" => row.Replace(" 0\t", " 1\t", StringComparison.Ordinal),
            "duplicate" => row + row,
            "traversal" => row.Replace("file.cs", "../escape.cs", StringComparison.Ordinal),
            "bad-mode" => row.Replace("100644", "040000", StringComparison.Ordinal),
            _ => row,
        });
        if (mutation == "invalid-utf8")
            body[^2] = 0xff;
        if (mutation == "invalid-utf8")
        {
            Assert.Throws<DecoderFallbackException>(() => AtlasGitMembership.DecodeMembership(body, _root, _root));
        }
        else
        {
            Assert.Equal(AtlasMembershipCaptureState.Refused,
                Assert.Throws<AtlasGitMembership.CaptureFailure>(() => AtlasGitMembership.DecodeMembership(body, _root, _root)).State);
        }
    }

    private AtlasGitMembership NewMembership() => new(Git, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Git))), Version);
    private static AtlasObjectIdentity Identity(string root) =>
        new AtlasDirectoryEnumerator().ObserveExpectedNativeRootIdentityForTest(root);
    private ValueTask<AtlasMembershipSnapshot> Capture(string root) =>
        NewMembership().CaptureForQualificationAsync(root, Identity(root), CancellationToken.None);
    private static void AssertCandidate(AtlasMembershipSnapshot snapshot) =>
        Assert.True(snapshot.State == AtlasMembershipCaptureState.CandidateComplete, $"{snapshot.State}: {snapshot.Reason}; invocations={snapshot.Invocations}");

    private async Task<string> Clone()
    {
        var seed = Path.Combine(_root, "seed");
        Directory.CreateDirectory(seed);
        await Command(seed, "-c", "init.templateDir=", "init", "--quiet", "--initial-branch=main");
        Directory.CreateDirectory(Path.Combine(seed, "src"));
        File.WriteAllText(Path.Combine(seed, "src", "a.cs"), "class A {}", Encoding.UTF8);
        File.WriteAllText(Path.Combine(seed, "outside.txt"), "outside nested root", Encoding.UTF8);
        await Command(seed, "add", "--", "src/a.cs", "outside.txt");
        await Command(seed, "commit", "--quiet", "-m", "fixture");
        var clone = Path.Combine(_root, "clone");
        await Command(_root, "clone", "--quiet", "--local", "--no-hardlinks", seed, clone);
        File.WriteAllText(Path.Combine(clone, "untracked.txt"), "physical inventory only", Encoding.UTF8);
        return clone;
    }

    private static Task Command(string directory, string a, string b, string c, string d, bool allowFailure) =>
        CommandCore(directory, [a, b, c, d], allowFailure);
    private static Task Command(string directory, params string[] arguments) => CommandCore(directory, arguments, false);

    private static async Task CommandCore(string directory, string[] arguments, bool allowFailure)
    {
        var start = new ProcessStartInfo(Git)
        {
            WorkingDirectory = directory, UseShellExecute = false,
            RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true,
        };
        start.Environment.Clear();
        start.Environment["SystemRoot"] = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        start.Environment["PATH"] = Path.GetDirectoryName(Git) + Path.PathSeparator + Environment.SystemDirectory;
        start.Environment["ComSpec"] = Path.Combine(Environment.SystemDirectory, "cmd.exe");
        start.Environment["GIT_CONFIG_NOSYSTEM"] = "1";
        start.Environment["GIT_CONFIG_GLOBAL"] = "NUL";
        start.Environment["GIT_CONFIG_SYSTEM"] = "NUL";
        start.Environment["GIT_TERMINAL_PROMPT"] = "0";
        foreach (var argument in new[] { "-c", "user.name=AtlasFixture", "-c", "user.email=atlas-fixture@invalid", "-c", "core.hooksPath=NUL" }.Concat(arguments))
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            await process.WaitForExitAsync(deadline.Token);
            await Task.WhenAll(output, error);
            Assert.True(allowFailure || process.ExitCode == 0, await error);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
        }
    }

    public void Dispose()
    {
        foreach (var file in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, File.GetAttributes(file) & ~FileAttributes.ReadOnly);
        }
        Directory.Delete(_root, recursive: true);
        Assert.False(Directory.Exists(_root));
    }
}
