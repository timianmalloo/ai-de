using System.Diagnostics;
using System.Text;
using AiDe.Core.Watcher;

namespace AiDe.Core.Tests.Watcher;

public sealed class CanonicalCoordinationBindingTests
{
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
