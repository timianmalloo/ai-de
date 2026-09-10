using AiDe.Core.Sessions;

namespace AiDe.Core.Tests.Sessions;

/// <summary>
/// F0 clause 1 and clause 2 (docs/plans/conductor-front-door.md): <c>session.json</c>, not
/// <c>.yaml</c> (Ruling 23), and the run-log path is reserved for Phase 3's <c>RunLogStore</c> and
/// must be provably unused by this slice.
/// </summary>
public sealed class SessionPathContractTests
{
    private const string WorkspaceRoot = @"C:\workspace";
    private const string TestSessionId = "20260910T120000Z-deadbeef";

    [Fact]
    public void SessionFile_IsJsonNotYaml()
    {
        var path = SessionPaths.SessionFile(WorkspaceRoot, TestSessionId);

        Assert.EndsWith(".json", path, StringComparison.Ordinal);
        Assert.DoesNotContain(".yaml", path, StringComparison.Ordinal);
        Assert.DoesNotContain(".yml", path, StringComparison.Ordinal);
    }

    [Fact]
    public void SessionFile_IsAtTheExactContractPath()
    {
        var path = SessionPaths.SessionFile(WorkspaceRoot, TestSessionId);
        var expected = Path.Combine(
            WorkspaceRoot, ".aide", "sessions", TestSessionId, "session.json");

        Assert.Equal(expected, path);
    }

    [Fact]
    public void RunLogFile_IsAtTheReservedContractPath()
    {
        var path = SessionPaths.RunLogFile(WorkspaceRoot, TestSessionId, "run-1");
        var expected = Path.Combine(
            WorkspaceRoot, ".aide", "sessions", TestSessionId, "runs", "run-1.jsonl");

        Assert.Equal(expected, path);
    }

    /// <summary>
    /// Fails if: anything writes a run log anywhere. Static half of the oracle — the reserved path
    /// is computed by <see cref="SessionPaths.RunLogFile"/> so a caller could build the path, but
    /// nothing in this slice's own source calls it to actually write. The dynamic half is
    /// <c>SessionConfigStoreTests.Lifecycle_NeverWritesUnderTheReservedRunsDirectory</c>.
    /// </summary>
    [Fact]
    public void RunLogFile_IsCalledOnlyByItsOwnDeclaration_NothingElseInSessionsWritesThere()
    {
        var sessionsDirectory = Path.Combine(RepoRoot(), "src", "AiDe.Core", "Sessions");
        Assert.True(Directory.Exists(sessionsDirectory), $"expected {sessionsDirectory} to exist");

        var callers = new List<string>();

        foreach (var file in Directory.EnumerateFiles(sessionsDirectory, "*.cs", SearchOption.TopDirectoryOnly))
        {
            foreach (var line in File.ReadAllLines(file))
            {
                // Skip the method's own declaration line (it names itself, and defining the
                // reserved path is the whole point of clause 1/2 — only USING it to write is banned).
                if (line.Contains("RunLogFile", StringComparison.Ordinal)
                    && !line.TrimStart().StartsWith("public static string RunLogFile", StringComparison.Ordinal)
                    && !line.TrimStart().StartsWith("///", StringComparison.Ordinal)
                    && !line.TrimStart().StartsWith("//", StringComparison.Ordinal))
                {
                    callers.Add($"{Path.GetFileName(file)}: {line.Trim()}");
                }
            }
        }

        Assert.True(
            callers.Count == 0,
            "RunLogFile is reserved and must be asserted unused; found a use site: "
                + string.Join(" | ", callers));
    }

    /// <summary>
    /// Fails if: the session-config path takes a YAML dependency. Ruling 23's subject is session
    /// config, not the project as a whole — Ruling 35 explicitly carves YamlDotNet out, scoped to
    /// the template loader (<c>TemplateFrontmatterReader.cs</c> / <c>TemplateSchema.cs</c>), so a
    /// repo-wide <c>AiDe.Core.csproj</c> scan is falsified by that loader's own explanatory comment
    /// on the dependency, not by an actual violation of Ruling 23. Ruling 36 narrows this guard to
    /// what Ruling 23 actually governs: <see cref="SessionConfig"/> and
    /// <see cref="SessionConfigStore"/> take no YAML dependency, full stop. Same shape as
    /// <see cref="RunLogFile_IsCalledOnlyByItsOwnDeclaration_NothingElseInSessionsWritesThere"/>: a
    /// source scan collecting hits rather than a substring check on one file.
    /// </summary>
    [Fact]
    public void SessionConfigSource_ContainsNoYamlToken()
    {
        var sessionsDirectory = Path.Combine(RepoRoot(), "src", "AiDe.Core", "Sessions");
        var sessionConfigFiles = new[] { "SessionConfig.cs", "SessionConfigStore.cs" };

        var hits = new List<string>();

        foreach (var fileName in sessionConfigFiles)
        {
            var path = Path.Combine(sessionsDirectory, fileName);
            Assert.True(File.Exists(path), $"expected {path} to exist");

            foreach (var line in File.ReadAllLines(path))
            {
                if (line.Contains("Yaml", StringComparison.OrdinalIgnoreCase))
                {
                    hits.Add($"{fileName}: {line.Trim()}");
                }
            }
        }

        Assert.True(
            hits.Count == 0,
            "session config must take no YAML dependency (Ruling 23); found: " + string.Join(" | ", hits));
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AiDe.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("could not locate the repository root from the test output directory");
    }
}
