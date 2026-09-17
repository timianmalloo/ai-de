using System.Diagnostics;
using System.Text;
using AiDe.Core.Watcher;

namespace AiDe.Core.Tests.Watcher;

public sealed class CanonicalCoordinationBindingTests
{
    [Fact]
    public void Bind_CopiedLinkedPointer_RefusesImpostorWithoutSource()
    {
        using var fixture = new LinkedFixture();
        var impostor = Path.Combine(fixture.Root.FullName, "impostor");
        Directory.CreateDirectory(impostor);
        File.Copy(Path.Combine(fixture.Linked, ".git"), Path.Combine(impostor, ".git"));
        var backlink = File.ReadAllBytes(fixture.Backlink);

        var primary = fixture.Bind(fixture.Primary);
        var genuine = fixture.Bind(fixture.Linked);
        var result = fixture.Bind(impostor);

        Assert.Equal(CanonicalBindingStatus.Bound, primary.Status);
        Assert.Equal(fixture.ExpectedSource, primary.SourcePath);
        Assert.Equal(CanonicalBindingStatus.Bound, genuine.Status);
        Assert.Equal(fixture.ExpectedSource, genuine.SourcePath);
        Assert.Equal(primary.Scope, genuine.Scope);
        Assert.Equal(CanonicalBindingStatus.Unbound, result.Status);
        Assert.Equal(CoordinationBindingErrors.Mismatch, result.Code);
        Assert.Null(result.SourcePath);
        Assert.Null(result.SourceId);
        Assert.Null(result.Scope);
        Assert.False(result.IsStorable);
        Assert.Equal(backlink, File.ReadAllBytes(fixture.Backlink));
    }

    [Fact]
    public void Bind_GitValidRelativeForwardPointer_BindsPhysicalPrimaryAndSameScope()
    {
        using var fixture = new LinkedFixture();
        var relative = Path.GetRelativePath(fixture.Linked, fixture.Admin);
        Assert.False(Path.IsPathRooted(relative));
        using (var pointer = File.Open(Path.Combine(fixture.Linked, ".git"), FileMode.Truncate, FileAccess.Write))
            pointer.Write(Encoding.UTF8.GetBytes("gitdir: " + relative + "\n"));
        var common = Git(fixture.Linked, "rev-parse", "--path-format=absolute", "--git-common-dir");
        Assert.Equal(Path.GetFullPath(Path.Combine(fixture.Primary, ".git")), Path.GetFullPath(common));

        var primary = fixture.Bind(fixture.Primary);
        var result = fixture.Bind(fixture.Linked);

        Assert.Equal(CanonicalBindingStatus.Bound, primary.Status);
        Assert.Equal(fixture.ExpectedSource, primary.SourcePath);
        Assert.Equal(CanonicalBindingStatus.Bound, result.Status);
        Assert.Equal(fixture.ExpectedSource, result.SourcePath);
        Assert.Equal(primary.SourceId, result.SourceId);
        Assert.Equal(primary.Scope, result.Scope);
        Assert.False(result.IsStorable);
    }

    [Fact]
    public void Bind_RelativeBacklink_ResolvesAgainstAdminDirectory()
    {
        using var fixture = new LinkedFixture();
        var relative = Path.GetRelativePath(fixture.Admin, Path.Combine(fixture.Linked, ".git"));
        Assert.False(Path.IsPathRooted(relative));
        File.WriteAllText(fixture.Backlink, relative + "\n");

        var primary = fixture.Bind(fixture.Primary);
        var result = fixture.Bind(fixture.Linked);

        Assert.Equal(CanonicalBindingStatus.Bound, result.Status);
        Assert.Equal(fixture.ExpectedSource, result.SourcePath);
        Assert.Equal(primary.Scope, result.Scope);
    }

    [Theory]
    [InlineData("missing", "Unavailable")]
    [InlineData("empty", "Unavailable")]
    [InlineData("malformed", "Unavailable")]
    [InlineData("oversized", "Unavailable")]
    [InlineData("invalid-utf8", "Unavailable")]
    [InlineData("foreign", "Unbound")]
    [InlineData("swap", "Unbound")]
    [InlineData("reparse", "Unavailable")]
    public void Bind_InvalidBacklink_RefusesWithoutSource(string kind, string expectedStatus)
    {
        using var fixture = new LinkedFixture();
        var expected = Enum.Parse<CanonicalBindingStatus>(expectedStatus);
        var junction = Path.Combine(fixture.Admin, "backlink-junction");
        switch (kind)
        {
            case "missing": File.Delete(fixture.Backlink); break;
            case "empty": File.WriteAllText(fixture.Backlink, " \r\n"); break;
            case "malformed": File.WriteAllText(fixture.Backlink, "bad\0path"); break;
            case "oversized": File.WriteAllText(fixture.Backlink, new string('x', 4097)); break;
            case "invalid-utf8": File.WriteAllBytes(fixture.Backlink, [0xff]); break;
            case "foreign":
                File.WriteAllText(fixture.Backlink, Path.Combine(fixture.Root.FullName, "absent", ".git"));
                break;
            case "swap":
                var second = Path.Combine(fixture.Root.FullName, "second");
                Git(fixture.Primary, "worktree", "add", "--quiet", "--detach", second, "HEAD");
                File.WriteAllText(fixture.Backlink, Path.Combine(second, ".git"));
                break;
            case "reparse":
                File.Delete(fixture.Backlink);
                Directory.CreateDirectory(Path.Combine(fixture.Root.FullName, "target"));
                CreateReparseDirectory(junction, Path.Combine(fixture.Root.FullName, "target"));
                Directory.Move(junction, fixture.Backlink);
                break;
        }
        try
        {
            var result = fixture.Bind(fixture.Linked);

            Assert.Equal(expected, result.Status);
            Assert.Equal(expected == CanonicalBindingStatus.Unbound
                ? CoordinationBindingErrors.Mismatch : CoordinationBindingErrors.Invalid, result.Code);
            Assert.Null(result.SourcePath);
            Assert.Null(result.SourceId);
            Assert.Null(result.Scope);
            Assert.False(result.IsStorable);
        }
        finally
        {
            if (kind == "reparse") Directory.Delete(fixture.Backlink);
        }
    }

    [Fact]
    public void Bind_UnknownMembership_DoesNotProbeInvalidFilesystemPaths()
    {
        var record = CanonicalCoordinationRecord.Parse(CanonicalCoordinationRecordTests.Example()).Record!;
        var context = new CanonicalSourceContext(new(new RepositoryIdentity("bad\0", "same-name"),
            CanonicalCoordinationSourceBinding.Origin), "bad\0", "bad\0", "source",
            [new("unknown", record.StreamId!)]);

        var result = CanonicalCoordinationSourceBinding.Bind(record.Raw, context);

        Assert.Equal(CanonicalBindingStatus.Unbound, result.Status);
        Assert.Equal(CoordinationBindingErrors.Mismatch, result.Code);
        Assert.Null(result.SourcePath);
    }

    [Fact]
    public void Bind_IsolatedGitRepositoriesAndLinkedCheckout_BindsOnlyTrustedFullIdentity()
    {
        var fixture = Directory.CreateTempSubdirectory("canonical-binding-");
        try
        {
            var first = Path.Combine(fixture.FullName, "one", "same-name");
            var other = Path.Combine(fixture.FullName, "two", "same-name");
            var linked = Path.Combine(fixture.FullName, "linked");
            Init(first);
            Init(other);
            Git(first, "remote", "add", "origin", "https://example.invalid/team/same-name.git");
            Git(other, "remote", "add", "origin", "https://example.invalid/other/same-name.git");
            Assert.Equal("https://example.invalid/team/same-name.git",
                Git(first, "remote", "get-url", "origin"));
            Assert.Equal("https://example.invalid/other/same-name.git",
                Git(other, "remote", "get-url", "origin"));
            Git(first, "worktree", "add", "--quiet", "--detach", linked, "HEAD");
            var raw = CanonicalCoordinationRecordTests.Example();
            var record = CanonicalCoordinationRecord.Parse(raw).Record!;
            CanonicalSourceContext Context(string primary, string checkout) => new(
                new(new RepositoryIdentity(primary, "same-name"), CanonicalCoordinationSourceBinding.Origin),
                primary, checkout, "public-source", [new(record.RepositoryId!, record.StreamId!)]);
            var primary = CanonicalCoordinationSourceBinding.Bind(raw, Context(first, first));
            var lane = CanonicalCoordinationSourceBinding.Bind(raw, Context(first, linked));
            var second = CanonicalCoordinationSourceBinding.Bind(raw, Context(other, other));

            Assert.Equal(CanonicalBindingStatus.Bound, primary.Status);
            Assert.Equal(CanonicalBindingStatus.Bound, lane.Status);
            Assert.Equal(primary.Scope, lane.Scope);
            Assert.Equal(Path.Combine(first, ".agents", "requests.jsonl"), lane.SourcePath);
            Assert.NotEqual(primary.Scope, second.Scope);
            Assert.Equal(CanonicalBindingStatus.Unbound, CanonicalCoordinationSourceBinding.Bind(raw, Context(other, linked)).Status);
            Assert.Equal(CanonicalBindingStatus.Unbound, CanonicalCoordinationSourceBinding.Bind(raw,
                Context(first, first) with { Streams = [new(record.RepositoryId!, "wrong")] }).Status);
            Assert.Equal(CanonicalBindingStatus.Unbound, CanonicalCoordinationSourceBinding.Bind(raw,
                Context(first, first) with { Streams = [] }).Status);
            Assert.Equal(CanonicalBindingStatus.Unbound, CanonicalCoordinationSourceBinding.Bind(raw,
                Context(first, first) with { Streams = [new(record.RepositoryId!, record.StreamId!), new(record.RepositoryId!, record.StreamId!)] }).Status);
            var padded = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(raw).PadRight(65536) + "\r\n");
            var bound = CanonicalCoordinationSourceBinding.Bind(padded, Context(first, linked));
            Assert.Equal(CanonicalBindingStatus.OriginBound, bound.Status);
            Assert.False(bound.IsStorable);
            Assert.Equal(65538, bound.Parsed.RawBytes);
            Assert.True(bound.ElapsedMilliseconds >= 0);

            foreach (var kind in new[] { "request-add", "request-resolve" })
            {
                var legacy = Encoding.UTF8.GetBytes("{\"kind\":\"" + kind + "\",\"id\":\"old\",\"at\":1,\"session\":\"peer\"}");
                var result = CanonicalCoordinationSourceBinding.Bind(legacy, Context(first, first));
                Assert.Equal(CanonicalBindingStatus.Bound, result.Status);
                Assert.Equal(CanonicalRecordStatus.Legacy, result.Parsed.Status);
                Assert.Equal(legacy, result.Parsed.Record!.Raw.ToArray());
                Assert.Null(result.Parsed.Record.RepositoryId);
                Assert.Null(result.Parsed.Record.StreamId);
                Assert.False(result.Parsed.Record.Body.TryGetProperty("recipient", out _));
                Assert.Equal("Unknown", result.Parsed.Record.GenerationQualification);
            }
            var agents = Path.Combine(first, ".agents");
            File.Delete(Path.Combine(agents, "requests.jsonl"));
            Directory.Delete(agents);
            CreateReparseDirectory(agents, Path.Combine(other, ".agents"));
            try
            {
                Assert.Equal(CanonicalBindingStatus.Unavailable,
                    CanonicalCoordinationSourceBinding.Bind(raw, Context(first, first)).Status);
            }
            finally { Directory.Delete(agents); }
        }
        finally
        {
            foreach (var file in fixture.EnumerateFiles("*", new EnumerationOptions
                     { RecurseSubdirectories = true, AttributesToSkip = FileAttributes.ReparsePoint }))
                file.Attributes &= ~FileAttributes.ReadOnly;
            fixture.Delete(recursive: true);
        }
    }

    private sealed class LinkedFixture : IDisposable
    {
        internal DirectoryInfo Root { get; } = Directory.CreateTempSubdirectory("canonical-reciprocal-");
        internal string Primary { get; }
        internal string Linked { get; }
        internal string Admin { get; }
        internal string Backlink => Path.Combine(Admin, "gitdir");
        internal string ExpectedSource => Path.Combine(Primary, ".agents", "requests.jsonl");
        private readonly byte[] _raw = CanonicalCoordinationRecordTests.Example();

        internal LinkedFixture()
        {
            Primary = Path.Combine(Root.FullName, "primary");
            Linked = Path.Combine(Root.FullName, "linked");
            try
            {
                Init(Primary);
                Git(Primary, "worktree", "add", "--quiet", "--detach", Linked, "HEAD");
                Admin = Path.GetFullPath(Git(Linked, "rev-parse", "--absolute-git-dir"));
                Assert.Equal(Path.Combine(Primary, ".git", "worktrees", "linked"), Admin);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal CanonicalBindingResult Bind(string checkout)
        {
            var record = CanonicalCoordinationRecord.Parse(_raw).Record!;
            return CanonicalCoordinationSourceBinding.Bind(_raw, new(
                new(new RepositoryIdentity(Primary, "not-a-repository-selector"), CanonicalCoordinationSourceBinding.Origin),
                Primary, checkout, "trusted-synthetic-source", [new(record.RepositoryId!, record.StreamId!)]));
        }

        public void Dispose()
        {
            foreach (var file in Root.EnumerateFiles("*", new EnumerationOptions
                     { RecurseSubdirectories = true, AttributesToSkip = FileAttributes.ReparsePoint }))
                file.Attributes &= ~FileAttributes.ReadOnly;
            Root.Delete(recursive: true);
        }
    }

    private static void Init(string path)
    {
        Directory.CreateDirectory(path);
        Git(path, "init", "--quiet");
        Git(path, "-c", "user.name=Synthetic", "-c", "user.email=synthetic@example.invalid",
            "commit", "--quiet", "--allow-empty", "-m", "synthetic");
        Directory.CreateDirectory(Path.Combine(path, ".agents"));
        File.WriteAllText(Path.Combine(path, ".agents", "requests.jsonl"), "");
    }

    private static string Git(string directory, params string[] args)
        => Run("git", directory, args);

    private static void CreateReparseDirectory(string path, string target)
    {
        if (!OperatingSystem.IsWindows())
        {
            Directory.CreateSymbolicLink(path, target);
            return;
        }
        // Junctions exercise the same reparse guard without granting SeCreateSymbolicLinkPrivilege.
        var command = "$ErrorActionPreference='Stop'; New-Item -ItemType Junction -Path '"
            + path.Replace("'", "''") + "' -Target '" + target.Replace("'", "''") + "'";
        Run(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
            "WindowsPowerShell", "v1.0", "powershell.exe"), Path.GetDirectoryName(path)!,
            "-NoProfile", "-NonInteractive", "-Command", command);
        Assert.True((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0);
    }

    private static string Run(string executable, string directory, params string[] args)
    {
        var start = new ProcessStartInfo(executable) { WorkingDirectory = directory,
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var key in start.Environment.Keys.Where(k => k.StartsWith("GIT_", StringComparison.Ordinal)).ToArray())
            start.Environment.Remove(key);
        start.Environment["GIT_TERMINAL_PROMPT"] = "0";
        start.Environment["GIT_NO_LAZY_FETCH"] = "1";
        start.Environment["GIT_AUTHOR_DATE"] = "2000-01-01T00:00:00Z";
        start.Environment["GIT_COMMITTER_DATE"] = "2000-01-01T00:00:00Z";
        foreach (var arg in args) start.ArgumentList.Add(arg);
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        Assert.True(process.WaitForExit(10000), "Synthetic Git timed out");
        Assert.True(process.ExitCode == 0, stderr);
        return stdout.Trim();
    }
}
