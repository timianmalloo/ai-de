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

    /// <summary>Fails if: a YAML dependency appears in this node (Ruling 23).</summary>
    [Fact]
    public void CoreProject_TakesNoYamlDependency()
    {
        var csproj = Path.Combine(RepoRoot(), "src", "AiDe.Core", "AiDe.Core.csproj");
        var text = File.ReadAllText(csproj);

        Assert.DoesNotContain("Yaml", text, StringComparison.OrdinalIgnoreCase);
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
