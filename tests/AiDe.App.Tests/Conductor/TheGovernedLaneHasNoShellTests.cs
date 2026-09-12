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

    /// <summary>
    /// Ruling 73: the read-only lane's pin is every tool that writes the tree or a durable file,
    /// executes code, a shell or a process, delegates to an agent, sends a local file anywhere, or
    /// cannot be read — read from two sources, not recalled: the SDK's schema union
    /// (<c>@anthropic-ai/claude-agent-sdk</c> 0.3.257 <c>sdk-tools.d.ts:11-56</c>) and the shipped
    /// CLI's own tool-name table (<c>claude-agent-sdk-win32-x64/claude.exe</c>, the <c>Amo</c> array,
    /// 183 names — a superset of the schema; <c>PowerShell</c> is a shell the schema does not list).
    /// Asserted as a <b>set equality against this literal</b>, so a name added or dropped on either
    /// side is a red. No base set is sent: the preset stays, minus these. <b>Re-read both sources on
    /// any SDK or adapter bump</b> — <c>EngineCatalogTests</c> pins the adapter version, and this
    /// literal is the thing that bump must re-derive (docs/proof/read-only-turn.md holds the table) —
    /// and so is <c>GovernedRunHost.ReadOnlyKinds</c>, the adapter's read kinds the chooser allows
    /// on a lane with no lease. A name in the CLI's table with no readable tool object is pinned
    /// fail-closed, not cut: PowerShell became a live tool by a remote flag with no version bump.
    /// </summary>
    [Fact]
    public void TheReadOnlyLanesPinIsEveryWriteCapableToolAndNothingElse()
    {
        var options = GovernedRunHost.ReadOnlyLaneSession;

        string[] expected =
        [
            // The tree and durable files: FileWriteInput :794, FileEditInput :758, NotebookEditInput :901;
            // MultiEdit (no schema in 0.3.257; the CLI table and sdk.mjs:198 name it as file-editing);
            // EnterWorktreeInput :3135 / ExitWorktreeInput :3145 cut and remove a tree and a branch;
            // CronCreateInput :2794 `durable: true` persists to .claude/scheduled_tasks.json, CronDelete edits it.
            "Write", "Edit", "MultiEdit", "NotebookEdit", "EnterWorktree", "ExitWorktree", "CronCreate", "CronDelete",

            // Code, shells and processes: BashInput :696; PowerShell (CLI table; `enablesCodeExecution`,
            // "execute Windows PowerShell commands"); REPLInput :2748 ("JavaScript code to execute");
            // MonitorInput :2864 ("Shell command or script"); Tmux (CLI table, a terminal); LSP (CLI
            // table, starts a configured server process); self_hosted_runner_spawn_local (CLI table,
            // "start a local self-hosted runner process").
            "Bash", "PowerShell", "REPL", "Monitor", "Tmux", "LSP", "self_hosted_runner_spawn_local",

            // Delegation: AgentInput :658 (and `Task`, the SDK's alias, sdk.mjs:198 `Task:"Agent"`;
            // `isolation: "worktree"` also cuts a tree); WorkflowInput :2762 (agent()/parallel());
            // RemoteTriggerInput :2841 (runs a cloud agent); self_hosted_runner_requeue_session (CLI table).
            "Agent", "Task", "Workflow", "RemoteTrigger", "self_hosted_runner_requeue_session",

            // A local file sent or saved by a side door, or a consequential remote write:
            // ArtifactInput :3050 (`read_asset` saves into the working directory); ProjectsInput :2653
            // (`project_write` uploads any local file, `project_delete` is irreversible); SendFile /
            // SendUserFile (CLI table, a local file to a channel).
            "Artifact", "Projects", "SendFile", "SendUserFile",

            // Opaque — fail-closed: ClaudeDesignInput :2641 (server-validated operations); Snip,
            // WebBrowser, SubscribePR, DesignSync, ConnectGitHub (CLI table, semantics not readable).
            "ClaudeDesign", "Snip", "WebBrowser", "SubscribePR", "DesignSync", "ConnectGitHub",
        ];

        Assert.NotNull(options.DisallowedTools);
        Assert.Equal(expected.ToHashSet(StringComparer.Ordinal), options.DisallowedTools!.ToHashSet(StringComparer.Ordinal));
        Assert.Equal(expected.Length, options.DisallowedTools!.Count);
        Assert.Equal(expected.Length, expected.Distinct(StringComparer.Ordinal).Count());
        Assert.Null(options.Tools);

        // Ruling 71's pin is the first use of the argument and is contained in the second.
        Assert.Subset(options.DisallowedTools!.ToHashSet(StringComparer.Ordinal), GovernedRunHost.GovernedLaneSession.DisallowedTools!.ToHashSet(StringComparer.Ordinal));
    }

    /// <summary>
    /// The host has exactly two <c>NewSessionAsync</c> calls — the governed lane's, rooted in its
    /// provisioned worktree with Ruling 71's pin (its arguments byte-identical to before Ruling 73),
    /// and the read-only lane's, rooted in the repository root with Ruling 73's pin.
    /// </summary>
    [Fact]
    public void TheTwoSessionSitesInTheHostEachPassTheirOwnPin()
    {
        var host = File.ReadAllText(Path.Combine(
            RepoRoot().FullName, "src", "AiDe.App", "Conductor", "GovernedRunHost.cs"));

        var sites = NewSessionCall().Matches(host).Select(m => m.Groups["arguments"].Value).Order(StringComparer.Ordinal).ToList();

        Assert.Equal(
            ["repositoryRoot, ReadOnlyLaneSession, cancellationToken", "worktree, GovernedLaneSession, cancellationToken"],
            sites);
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

    /// <summary>
    /// Ruling 73 condition (1): the read-only lane's <c>session/new</c> carries <b>exactly</b> the key
    /// set <c>{cwd, mcpServers: [], _meta}</c> with the disallowed set as a set equality on the wire,
    /// no <c>tools</c> member, its <c>cwd</c> the repository root (no worktree is cut), and the frame
    /// is recorded on the report and in the log on the normal path — the same oracle as the governed
    /// lane's, over the real client and the real peer with the engine's stdin captured.
    /// </summary>
    [Fact]
    public async Task TheReadOnlyFrameCarriesExactlyCwdEmptyMcpServersAndTheDisallowedSet()
    {
        var root = Path.GetFullPath(Path.GetTempPath());
        var stdout = new PushedOutputReader();
        var stdin = new StringWriter();
        var peer = new AcpPeer(stdout, TextWriter.Synchronized(stdin), new AcpRunEventMapper("run-2", "lane-2"), diagnostics: _ => { });
        var client = new AcpLaneClient(peer);
        var report = new List<string>();
        var log = new List<string>();

        var pump = peer.RunAsync();
        var previous = WorkbenchDiagnostics.Sink;
        WorkbenchDiagnostics.Sink = log.Add;
        string session;
        try
        {
            var open = GovernedRunHost.OpenReadOnlySessionAsync(client, root, "run-2", "lane-2", report.Add, CancellationToken.None);
            stdout.PushFrame("""{"jsonrpc":"2.0","id":1,"result":{"sessionId":"sess-ro"}}""");
            session = await open;
        }
        finally
        {
            WorkbenchDiagnostics.Sink = previous;
        }

        stdout.EndOfStream();
        await pump;

        Assert.Equal("sess-ro", session);

        var wire = JsonNode.Parse(stdin.ToString().Trim())!.AsObject();
        Assert.Equal("session/new", wire["method"]!.GetValue<string>());
        var parameters = wire["params"]!.AsObject();

        Assert.Equal(["cwd", "mcpServers", "_meta"], parameters.Select(m => m.Key));
        Assert.Equal(root, parameters["cwd"]!.GetValue<string>());
        Assert.Empty(parameters["mcpServers"]!.AsArray());

        var options = parameters["_meta"]!["claudeCode"]!["options"]!.AsObject();
        Assert.Equal(["disallowedTools"], options.Select(m => m.Key));
        Assert.Equal(
            GovernedRunHost.ReadOnlyLaneSession.DisallowedTools!.ToHashSet(StringComparer.Ordinal),
            options["disallowedTools"]!.AsArray().Select(n => n!.GetValue<string>()).ToHashSet(StringComparer.Ordinal));

        var sent = parameters.ToJsonString();
        var line = Assert.Single(report);
        Assert.Contains("sess-ro", line, StringComparison.Ordinal);
        Assert.Contains(sent, line, StringComparison.Ordinal);

        var record = JsonDocument.Parse(Assert.Single(log)).RootElement;
        Assert.Equal("lane.session-new", record.GetProperty("evt").GetString());
        Assert.Equal("run-2", record.GetProperty("run").GetString());
        Assert.Equal("lane-2", record.GetProperty("lane").GetString());
        Assert.Equal("sess-ro", record.GetProperty("session").GetString());
        Assert.Equal(sent, record.GetProperty("params").GetRawText());
    }

    /// <summary>
    /// The spawn's shape is read from the request's missing lease — the same fact that picks the
    /// pin — and never from a claim: a lease-less request spawns read-only, a leased one does not.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheSpawnShapeIsTheLeasesAbsence(bool leased)
    {
        var request = TheOneCompositionRootIsCountedTests.RefusedRequest() with
        {
            Goal = leased ? new GoalBlock("g", "d", "n", "T1", 0, new RunBudget(1, 1)) : null,
            Lease = leased ? new Lease(["src/**"]) : null,
        };

        var spawn = GovernedRunHost.SpawnRequestFor(request, observed: null);

        Assert.Equal(!leased, spawn.ReadOnly);
        Assert.Equal(request.Goal, spawn.Goal);
        Assert.Equal(request.EngineId, spawn.EngineId);
    }

    /// <summary>
    /// The read-only drain runs with no seam monitor: an <c>edit</c>-kind tool call is drained,
    /// counted and passed to the sink like every other event — nothing raises, nothing is dropped.
    /// </summary>
    [Fact]
    public async Task TheReadOnlyDrainCountsAnEditFrameWithNoSeamMonitor()
    {
        var queue = new AcpEventQueue(capacity: 8);
        var sink = new List<ObservedRunEvent>();
        var report = new List<string>();
        var prompt = new TaskCompletionSource();

        var drain = GovernedRunHost.DrainAsync(queue, prompt.Task, seams: null, report.Add, sink.Add, CancellationToken.None);

        RunEvent Event(long seq, string kind, JsonObject body) => new(
            "run-1", "lane-1", null, seq, DateTimeOffset.UnixEpoch, kind, null, body, new JsonObject());

        // An edit frame, exactly as the corpus carries one — the shape a seam monitor would raise on.
        await queue.PublishAsync(new ObservedRunEvent(
            Event(1, "tool.call", new JsonObject
            {
                ["kind"] = "edit",
                ["toolCallId"] = "call-1",
                ["locations"] = new JsonArray(new JsonObject { ["path"] = Path.Combine(Path.GetTempPath(), "x.txt") }),
            }),
            ReceivedAt: DateTimeOffset.UnixEpoch,
            NormalizationLatency: TimeSpan.FromMilliseconds(1)));

        prompt.SetResult();
        await queue.PublishAsync(new ObservedRunEvent(Event(2, "message", new JsonObject()), DateTimeOffset.UnixEpoch, null));

        var drained = await drain;

        Assert.Equal(2, drained.Events);
        Assert.Equal(["tool.call", "message"], drained.Kinds);
        Assert.Equal(2, sink.Count);
        Assert.Single(drained.LatenciesMs);
        Assert.DoesNotContain(report, line => line.StartsWith("seam ", StringComparison.Ordinal));
    }

    /// <summary>
    /// Ruling 73 condition (2)'s second belt: on a lane with no lease the permission chooser allows
    /// only the read kinds and refuses everything else by name — an edit, a shell, and the coarse
    /// <c>other</c> a notebook edit arrives as — and says READ-ONLY TURN in the report.
    /// </summary>
    [Theory]
    [InlineData("edit", "Edit", "reject")]
    [InlineData("execute", "Bash", "reject")]
    [InlineData("other", "NotebookEdit", "reject")]
    [InlineData("switch_mode", "ExitPlanMode", "reject")]
    [InlineData(null, "Write", "reject")]
    [InlineData("think", "Agent", "reject")]
    [InlineData("think", "Task", "reject")]
    [InlineData("read", "Read", "allow")]
    [InlineData("search", "Grep", "allow")]
    [InlineData("fetch", "WebFetch", "allow")]
    [InlineData("think", "TodoWrite", "allow")]
    [InlineData("read", null, "allow")]
    public void WithNoLeaseTheChooserAllowsOnlyReads(string? kind, string? name, string expected)
    {
        var report = new List<string>();
        var toolCall = new JsonObject { ["title"] = "Edit x.txt" };
        if (kind is not null)
        {
            toolCall["kind"] = kind;
        }

        if (name is not null)
        {
            toolCall["name"] = name;
        }

        var parameters = new JsonObject
        {
            ["toolCall"] = toolCall,
            ["options"] = new JsonArray(
                new JsonObject { ["optionId"] = "allow-once", ["kind"] = "allow_once" },
                new JsonObject { ["optionId"] = "reject", ["kind"] = "reject_once" }),
        };

        var chosen = GovernedRunHost.Decide(parameters, lease: null, worktreeRoot: null, report.Add);

        Assert.Equal(expected == "allow" ? "allow-once" : "reject", chosen);
        if (expected == "reject")
        {
            Assert.Contains(report, line => line.Contains("READ-ONLY TURN", StringComparison.Ordinal));
        }
    }

    /// <summary>The write path's chooser is unchanged: with a lease, a non-edit is allowed and an edit is judged against the lease.</summary>
    [Fact]
    public void WithALeaseTheChooserIsUnchanged()
    {
        var report = new List<string>();
        var lease = new Lease(["src/**"]);
        var root = Path.GetFullPath(Path.GetTempPath());
        JsonObject Parameters(string kind, string path) => new()
        {
            ["toolCall"] = new JsonObject
            {
                ["kind"] = kind,
                ["locations"] = new JsonArray(new JsonObject { ["path"] = Path.Combine(root, path) }),
            },
            ["options"] = new JsonArray(
                new JsonObject { ["optionId"] = "allow-once", ["kind"] = "allow_once" },
                new JsonObject { ["optionId"] = "reject", ["kind"] = "reject_once" }),
        };

        Assert.Equal("allow-once", GovernedRunHost.Decide(Parameters("execute", "x"), lease, root, report.Add));
        Assert.Equal("allow-once", GovernedRunHost.Decide(Parameters("edit", "src/a.cs"), lease, root, report.Add));
        Assert.Equal("reject", GovernedRunHost.Decide(Parameters("edit", "docs/a.md"), lease, root, report.Add));
    }

    /// <summary>
    /// The tree check measures change against the tree's own baseline, not cleanliness: a dirty tree
    /// that stays dirty reads 0, a new path reads 1, a status git did not answer reads "not recorded".
    /// </summary>
    [Theory]
    [InlineData("", "", 0)]
    [InlineData(" M a.cs\n", " M a.cs\n", 0)]
    [InlineData(" M a.cs\n", " M a.cs\n?? probe.txt\n", 1)]
    [InlineData("", "?? probe.txt\r\n", 1)]
    [InlineData("?? probe.txt\n", "?? probe.txt\r\n", 0)]
    [InlineData(" M a.cs\n", "", 1)]
    [InlineData("--- diff ---\n+old\n", "--- diff ---\n+new\n", 2)]
    [InlineData(null, "", null)]
    [InlineData("", null, null)]
    public void TheTreeDeltaIsChangeAgainstTheBaseline(string? before, string? after, int? expected)
    {
        Assert.Equal(expected, GovernedRunHost.TreeDelta(before, after));
    }

    /// <summary>
    /// The reading is porcelain + the diff against HEAD + a hash per untracked path, so a file that
    /// was already dirty at the baseline and is modified again during the turn is seen — porcelain
    /// alone reads the same line twice. Git not answering on any of the three is "not recorded".
    /// </summary>
    [Fact]
    public void TheTreeStatusSeesASecondModificationOfAnAlreadyDirtyFile()
    {
        var before = new FakeRunner(
            ("status --porcelain", " M a.cs\n?? probe.txt\n"),
            ("diff HEAD", "-old\n+first change\n"),
            ("hash-object -- probe.txt", "aaaa\n"));
        var after = new FakeRunner(
            ("status --porcelain", " M a.cs\n?? probe.txt\n"),
            ("diff HEAD", "-old\n+second change\n"),
            ("hash-object -- probe.txt", "bbbb\n"));

        var delta = GovernedRunHost.TreeDelta(
            GovernedRunHost.TreeStatus(before, Path.GetTempPath()),
            GovernedRunHost.TreeStatus(after, Path.GetTempPath()));

        // The porcelain lines are identical; the diff line and the hash line are not: 2 + 2.
        Assert.Equal(4, delta);
        Assert.Equal(Path.GetTempPath(), after.LastDirectory);
    }

    [Fact]
    public void TheTreeStatusIsNotRecordedWhenGitDoesNotAnswer()
    {
        Assert.Null(GovernedRunHost.TreeStatus(new FakeRunner(("status --porcelain", null)), Path.GetTempPath()));
        Assert.Null(GovernedRunHost.TreeStatus(new FakeRunner(("status --porcelain", ""), ("diff HEAD", null)), Path.GetTempPath()));

        var clean = new FakeRunner(("status --porcelain", ""), ("diff HEAD", ""));
        Assert.Equal(0, GovernedRunHost.TreeDelta(GovernedRunHost.TreeStatus(clean, Path.GetTempPath()), GovernedRunHost.TreeStatus(clean, Path.GetTempPath())));
    }

    /// <summary>A fake git: each known argument line answers with its output, or exit 128 for a null.</summary>
    private sealed class FakeRunner(params (string Arguments, string? Output)[] answers) : IProcessRunner
    {
        public string? LastDirectory { get; private set; }

        public ProcessResult Run(string fileName, IReadOnlyList<string> arguments, string workingDirectory)
        {
            LastDirectory = workingDirectory;
            Assert.Equal("git", fileName);
            var key = string.Join(' ', arguments);
            var answer = answers.SingleOrDefault(a => a.Arguments == key);
            return answer.Output is null
                ? new ProcessResult(128, string.Empty, "fatal: " + key)
                : new ProcessResult(0, answer.Output, string.Empty);
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
