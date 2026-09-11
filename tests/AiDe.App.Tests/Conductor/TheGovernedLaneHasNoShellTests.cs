using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AiDe.App.Conductor;
using AiDe.App.Tests.Sessions;
using AiDe.App.Workbench;
using AiDe.Core.AgentPlane;

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

    /// <summary>
    /// Ruling 71 (a): the frame the lane was opened with is recorded on the normal path — on the run's
    /// report and as a <c>lane.session-new</c> workbench log line — and what is recorded is what went
    /// down the wire, <c>_meta</c> included. No flag, nothing to remember.
    /// </summary>
    /// <remarks>
    /// The real client over the real peer, with the engine's stdin captured: the assertion compares
    /// the recorded params against the <c>session/new</c> frame the peer actually wrote — so a host
    /// that logged a re-computation of what it meant to send would still have to match the wire.
    /// </remarks>
    [Fact]
    public async Task TheFrameTheLaneWasOpenedWithIsRecordedOnTheReportAndInTheLog()
    {
        var absolute = Path.GetFullPath(Path.GetTempPath());
        var tree = new ProvisionedWorktree(absolute, "agent/claude-code/lane-0001", absolute, CoordInstalled: false);
        var stdout = new PushedOutputReader();
        var stdin = new StringWriter();
        var peer = new AcpPeer(stdout, TextWriter.Synchronized(stdin), new AcpRunEventMapper("run-1", "lane-1"), diagnostics: _ => { });
        var client = new AcpLaneClient(peer);
        var report = new List<string>();
        var log = new List<string>();

        var pump = peer.RunAsync();
        var previous = WorkbenchDiagnostics.Sink;
        WorkbenchDiagnostics.Sink = log.Add;
        string session;
        try
        {
            var open = GovernedRunHost.OpenSessionAsync(client, tree, "run-1", "lane-1", report.Add, CancellationToken.None);
            stdout.PushFrame("""{"jsonrpc":"2.0","id":1,"result":{"sessionId":"sess-9"}}""");
            session = await open;
        }
        finally
        {
            WorkbenchDiagnostics.Sink = previous;
        }

        stdout.EndOfStream();
        await pump;

        Assert.Equal("sess-9", session);

        // The wire: the one frame the peer wrote to the engine's stdin.
        var wire = JsonNode.Parse(stdin.ToString().Trim())!.AsObject();
        Assert.Equal("session/new", wire["method"]!.GetValue<string>());
        var sent = wire["params"]!.ToJsonString();
        Assert.Contains("""{"claudeCode":{"options":{"disallowedTools":["Bash"]}}}""", sent, StringComparison.Ordinal);

        // The report: the session id and the exact params, on one line.
        var line = Assert.Single(report);
        Assert.Contains("sess-9", line, StringComparison.Ordinal);
        Assert.Contains(sent, line, StringComparison.Ordinal);

        // The log: evt lane.session-new, keyed by run, lane and session, carrying the same params.
        var record = JsonDocument.Parse(Assert.Single(log)).RootElement;
        Assert.Equal("lane.session-new", record.GetProperty("evt").GetString());
        Assert.Equal("run-1", record.GetProperty("run").GetString());
        Assert.Equal("lane-1", record.GetProperty("lane").GetString());
        Assert.Equal("sess-9", record.GetProperty("session").GetString());
        Assert.Equal(sent, record.GetProperty("params").GetRawText());
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
