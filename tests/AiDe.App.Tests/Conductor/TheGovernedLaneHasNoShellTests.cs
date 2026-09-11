using System.Text.RegularExpressions;
using AiDe.App.Conductor;

namespace AiDe.App.Tests.Conductor;

/// <summary>
/// Ruling 71: the governed lane's shell is pinned off on its <c>session/new</c>, and the pin is one
/// value on the one site.
/// </summary>
/// <remarks>
/// <para><b>Why the site is read from source.</b> <c>GovernedRunHost.RunAsync</c> starts a real
/// engine process before it opens the session, so the frame it sends cannot be captured here
/// without node. <c>AcpLaneClientTests</c> already proves what a client does with the record; what
/// is left to prove is that the host passes <i>this</i> record at <i>its</i> one call — the same
/// source-text oracle <c>AGovernedRunReachesTheConsoleTests.TheRunsOwnCountIsTheDrainsCount</c>
/// uses for the same reason.</para>
///
/// <para><b>And why the sweep is wider than the host.</b> A control proven on one site protects
/// that site, not the boundary (DC-019). Every session opened from <c>src/</c> must say what tools
/// it holds — a second caller that opened one with the bare two-argument form would ship unpinned,
/// and this is the test that would notice.</para>
/// </remarks>
public sealed partial class TheGovernedLaneHasNoShellTests
{
    /// <summary>Everything the preset allows, minus the shell — no base set is sent, only the exclusion.</summary>
    [Fact]
    public void TheGovernedLanesPinIsExactlyBashAndNothingElse()
    {
        var options = GovernedRunHost.GovernedLaneSession;

        Assert.Equal(["Bash"], options.DisallowedTools);
        Assert.Null(options.Tools);
    }

    /// <summary>The host has one <c>NewSessionAsync</c> call, and it passes the pin.</summary>
    [Fact]
    public void TheOneSessionSiteInTheHostPassesThePin()
    {
        var host = File.ReadAllText(Path.Combine(
            RepoRoot().FullName, "src", "AiDe.App", "Conductor", "GovernedRunHost.cs"));

        var site = Assert.Single(NewSessionCall().Matches(host));
        Assert.Equal("worktree, GovernedLaneSession, cancellationToken", site.Groups["arguments"].Value);
    }

    /// <summary>
    /// No session is opened anywhere in <c>src/</c> without a tools argument: every call outside
    /// the client's own delegation names its <c>LaneSessionOptions</c> explicitly — never the bare
    /// form, never <c>null</c>.
    /// </summary>
    [Fact]
    public void EverySessionOpenedFromSourceSaysWhatToolsItHolds()
    {
        var src = Path.Combine(RepoRoot().FullName, "src");
        var client = Path.Combine(src, "AiDe.Core", "AgentPlane", "AcpLaneClient.cs");

        var sites = Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .Where(file => !string.Equals(file, client, StringComparison.OrdinalIgnoreCase))
            .SelectMany(file => NewSessionCall().Matches(File.ReadAllText(file))
                .Select(match => (File: Path.GetRelativePath(src, file), Arguments: match.Groups["arguments"].Value)))
            .ToList();

        Assert.NotEmpty(sites);

        foreach (var (file, arguments) in sites)
        {
            var parts = arguments.Split(',', StringSplitOptions.TrimEntries);
            Assert.True(parts.Length == 3, $"{file}: NewSessionAsync({arguments}) does not say what tools the session holds");
            Assert.False(parts[1] is "null" or "options: null", $"{file}: NewSessionAsync({arguments}) opens a session with no tools argument");
        }
    }

    [GeneratedRegex(@"\.NewSessionAsync\((?<arguments>[^)]*)\)")]
    private static partial Regex NewSessionCall();

    private static DirectoryInfo RepoRoot()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);

        while (root is not null && !File.Exists(Path.Combine(root.FullName, "AiDe.sln")))
        {
            root = root.Parent;
        }

        Assert.NotNull(root);
        return root!;
    }
}
