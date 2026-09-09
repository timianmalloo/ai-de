using System.IO;
using System.Text.Json.Nodes;
using AiDe.Core.AgentPlane;
using AiDe.Core.Watcher;

namespace AiDe.App.Conductor;

/// <summary>
/// The App-layer composition root for a governed ACP lane — spec §6.2, and Phase 1's only entry to a
/// governed run.
/// </summary>
/// <remarks>
/// <para><b>One composition root, deliberately.</b> The Conductor Surface is deferred (Ruling 13) and
/// the headless entry exists now; both call <see cref="RunAsync"/>. N7's floor is explicit that a
/// hand-assembled harness does not count as exit evidence — a second assembly path would let the
/// demonstrated run and the shipped run differ in the wiring, which is the one thing the evidence is
/// about.</para>
///
/// <para><b>What it composes, in the order the order matters:</b> catalog → engine process →
/// peer/client handshake → <i>observed</i> auth → spawn authorization → worktree → governed episode
/// → ACP session rooted in that worktree → prompt → seams → close → score. The auth observation sits
/// before the authorization because the gate is on what the adapter says about itself, not on what
/// the configuration claims; and the worktree is cut after the authorization because a refused spawn
/// must leave nothing behind.</para>
///
/// <para><b>It hosts no terminal, and the run says so with a number.</b>
/// <see cref="TerminalHostingLedger"/> is opened around the whole run and its count travels on the
/// result, so the absence is a measurement rather than a sentence.</para>
/// </remarks>
public static class GovernedRunHost
{
    /// <summary>What every absent measurement reads as. Never zero, never a plausible substitute.</summary>
    public const string NotRecorded = "not recorded";

    /// <summary>The ACP tool-call kind the captured corpus carries for a write.</summary>
    private const string EditToolKind = "edit";

    /// <summary>
    /// Runs one governed lane to completion and scores it.
    /// </summary>
    /// <param name="request">What to run.</param>
    /// <param name="cancellationToken">Bounds the whole run.</param>
    public static async Task<GovernedRunResult> RunAsync(
        GovernedRunRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var diagnostics = new System.Collections.Concurrent.ConcurrentQueue<string>();
        void Report(string line) => diagnostics.Enqueue(line);

        var runId = "run-" + Guid.NewGuid().ToString("n")[..8];
        var laneId = "lane-" + Guid.NewGuid().ToString("n")[..8];
        var time = TimeProvider.System;

        // The oracle for "zero terminal hosting" opens FIRST, so everything the run does afterwards
        // happens inside the window it counts.
        using var terminals = TerminalHostingLedger.Open();

        var triage = RunTriage.For(request.Goal);
        Report("triage: " + triage.Reason);

        var launch = EngineCatalog.ResolveLaunch(request.EngineId, request.AdapterInstallRoot);
        Report($"engine: {launch.FileName} {string.Join(' ', launch.Arguments)}");

        using var engine = AcpEngineProcess.Start(launch, request.RepositoryRoot, Report);
        Report($"engine pid {engine.ProcessId}");

        var provisioner = new WorktreeProvisioner(new ProcessRunner(), request.CoordCommand);
        var registry = new ProviderRegistry(request.Providers);

        // The shell's own watcher composition (WorkbenchShell), not a second one.
        using var watcher = WatcherHost.Open(
            request.DataDirectory, Path.Combine(request.DataDirectory, "loomkeeper-coord"), time);

        ProvisionedWorktree? worktree = null;

        var peer = new AcpPeer(
            engine.Output, engine.Input, new AcpRunEventMapper(runId, laneId), diagnostics: Report);

        // The permission policy IS the governance: an edit inside the lease is allowed, an edit
        // outside it is refused. A client that allows everything has removed the control the plane
        // exists to provide; one that refuses everything cannot do the work.
        var client = new AcpLaneClient(
            peer,
            choosePermission: parameters => Decide(parameters, request.Lease, worktree?.Path, Report));

        var pump = peer.RunAsync(cancellationToken);

        await client.InitializeAsync(cancellationToken).ConfigureAwait(false);
        Report("initialize: the echoed protocolVersion was checked");

        var observed = await AwaitObservedAuthAsync(peer, cancellationToken).ConfigureAwait(false);

        // R2 and §4.2: no block, no spawn; and no observed subscription, no spawn.
        var spawn = SpawnContract.Authorize(
            new SpawnRequest(request.Goal, request.EngineId, request.Model, request.AccountLabel, observed),
            registry);

        worktree = provisioner.Provision(request.RepositoryRoot, spawn.Binding.EngineId, laneId);
        Report($"worktree: {worktree.Branch} at {worktree.Path} (coord install: {worktree.CoordInstalled})");

        var seams = new LeaseMonitor(request.Lease, worktree.Path);

        var identity = new LaneIdentity(
            LaneId: laneId,
            AgentName: spawn.Binding.EngineId + "-lane",
            RepositoryPath: request.RepositoryRoot,
            RepositoryDisplay: Path.GetFileName(request.RepositoryRoot.TrimEnd('\\', '/')),
            WorktreeBranch: worktree.Branch,
            WorktreePath: worktree.Path,
            Harness: spawn.Binding.EngineId,
            Model: spawn.Binding.Model);

        var session = new GovernedSessionSource(watcher.Ingest).Open(identity, request.Goal);
        Report($"episode {session.EpisodeId} opened on session {session.SessionId}");

        // R1 bullet 1: the ACP session's cwd IS the provisioned worktree. The provisioner's own type
        // is passed rather than a path string, so the lane cannot be rooted anywhere else.
        var acpSession = await client.NewSessionAsync(worktree, cancellationToken).ConfigureAwait(false);
        Report("acp session " + acpSession);

        var kinds = new List<string>();
        var latencies = new List<double>();
        var events = 0;

        var prompt = client.PromptAsync(acpSession, request.Prompt, cancellationToken);

        await foreach (var run in peer.Events.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            events++;
            kinds.Add(run.Event.Kind);

            if (run.NormalizationLatency is { } latency)
            {
                latencies.Add(latency.TotalMilliseconds);
            }

            foreach (var seam in seams.Observe(run.Event))
            {
                Report($"seam {seam.SeamId}: {seam.Path} is outside the lease");
            }

            if (prompt.IsCompleted && peer.Events.Reader.Count == 0)
            {
                break;
            }
        }

        var outcome = EpisodeOutcome.Completed;
        try
        {
            var answer = await prompt.ConfigureAwait(false);
            Report("prompt stopReason: " + (StringValue(answer["stopReason"]) ?? NotRecorded));
        }
        catch (AgentPlaneException error)
        {
            Report($"prompt refused {error.Code}: {error.Message}");
            outcome = EpisodeOutcome.Blocked;
        }

        // THE SEQUENCE THAT DECIDES WHETHER THIS RUN SCORES AT ALL. ClosedEpisodeScoring reads the
        // declared artifacts and asks ProofPackVerifier whether each is a real file; an episode that
        // declares none derives no verification path and scores Not Scored. So the declaration
        // happens before the close, and the Proof Pack it names is committed before the run.
        var declared = session.DeclareArtifacts(request.ProofPackArtifacts);
        Report($"declared {declared} evidence path(s)");

        var state = provisioner.Inspect(worktree);
        var lane = new GovernedLane(session, provisioner, worktree, seams);
        var teardown = lane.Close(outcome, LaneClosure.Unresolved, state, removeWorktreeWhenSafe: false);
        Report($"episode closed {teardown.Outcome}; tree {teardown.Worktree?.Kind}: {teardown.Worktree?.Reason}");

        var scored = ScoreOrReport(watcher, time, session.EpisodeId, request.TaskClass, Report);

        engine.Dispose();
        try
        {
            await pump.ConfigureAwait(false);
        }
        catch (Exception error) when (error is OperationCanceledException or AgentPlaneException or IOException)
        {
            Report("peer loop ended: " + error.Message);
        }

        return new GovernedRunResult(
            RunId: runId,
            SessionId: session.SessionId,
            EpisodeId: session.EpisodeId,
            WorktreePath: worktree.Path,
            WorktreeBranch: worktree.Branch,
            CoordInstalled: worktree.CoordInstalled,
            ObservedAuthKind: observed?.Kind ?? NotRecorded,
            ObservedAuthLabel: observed?.Label ?? NotRecorded,
            ObservedAuthPlan: observed?.Plan ?? NotRecorded,
            TerminalHostConstructions: terminals.Constructions,
            Stages: [.. triage.Stages.Select(s => s.ToString())],
            SkippedPlanAndCouncil: triage.SkipsPlanAndCouncil,
            TriageReason: triage.Reason,
            EventsObserved: events,
            EventKinds: [.. kinds.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)],
            LatencyMeasured: latencies.Count,
            LatencyP50Ms: Percentile(latencies, 0.50),
            LatencyP95Ms: Percentile(latencies, 0.95),
            LatencyHost: Environment.MachineName,
            SeamsRaised: seams.RaisedCount,
            SeamResolutionRatio: seams.SeamResolutionRatio,
            Outcome: teardown.Outcome.ToString(),
            WorktreeDisposition: $"{teardown.Worktree?.Kind}: {teardown.Worktree?.Reason}",
            Scored: scored is not null,
            ScoreVerdict: scored?.Scorecard.Verdict.ToString() ?? NotRecorded,
            ScoreHeadline: scored?.Scorecard.Headline ?? NotRecorded,
            TaskClass: scored?.Segment.TaskClass ?? NotRecorded,
            SegmentIsComparable: scored?.Segment.IsComparable ?? false,
            IncomparableReason: scored?.Segment.IncomparableReason,
            Mode: watcher.Store.FindEpisodeMode(session.EpisodeId),
            EngineProcessId: engine.ProcessId,
            EngineExited: engine.HasExited,
            EnvironmentFindings: engine.EnvironmentFindings,
            Diagnostics: [.. diagnostics]);
    }

    /// <summary>
    /// Scores the closed episode, reporting a refusal rather than throwing it away.
    /// </summary>
    /// <remarks>
    /// A run that produced work and then could not be scored is exactly the finding N7 exists to
    /// surface, so it must reach the result rather than the exception path — a thrown refusal takes
    /// the worktree path, the terminal count and the latency with it.
    /// </remarks>
    private static ScoredEpisode? ScoreOrReport(
        WatcherHost watcher, TimeProvider time, string episodeId, string taskClass, Action<string> report)
    {
        try
        {
            return LaneScoring.ScoreGoverned(watcher.Store, time, episodeId, taskClass);
        }
        catch (AgentPlaneException error)
        {
            report($"scoring refused {error.Code}: {error.Message}");
            return watcher.Store.FindScoredEpisode(episodeId);
        }
    }

    /// <summary>
    /// Waits briefly for the adapter's <c>_auth/status_update</c>, and answers <c>null</c> if it
    /// never arrives.
    /// </summary>
    /// <remarks>
    /// <b>A bounded wait, and null on expiry.</b> The frame is an <c>_</c>-prefixed extension that
    /// the captured corpus shows arriving immediately after the handshake, but "immediately" is not a
    /// guarantee and a spawn must never proceed on an assumption about where it bills. Null is what
    /// <see cref="SpawnContract"/> refuses on, so an adapter that goes quiet fails closed.
    /// </remarks>
    private static async Task<ObservedAuthStatus?> AwaitObservedAuthAsync(
        AcpPeer peer, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(15);
        while (peer.ObservedAuth is null && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
        }

        return peer.ObservedAuth;
    }

    /// <summary>
    /// Answers one permission request: allow an edit inside the lease, refuse one outside it.
    /// </summary>
    /// <remarks>
    /// <para><b>Matched by <c>kind</c>, not by option id.</b> The corpus shows ids that are the
    /// adapter's own strings (<c>allow-once</c>, <c>reject</c>) beside stable <c>kind</c> values
    /// (<c>allow_once</c>, <c>reject_once</c>), so keying on the kind survives a rename.</para>
    ///
    /// <para><b>A non-edit call is allowed.</b> The lease is a <i>write</i> scope (§14.3); refusing
    /// reads and shell calls would leave the lane unable to see what it is editing, which is not what
    /// the control says. An edit whose locations are not stated is refused: an unstated target cannot
    /// be shown to be inside the scope, and a governance control degrades toward saying no.</para>
    /// </remarks>
    private static string Decide(JsonObject parameters, Lease lease, string? worktreeRoot, Action<string> report)
    {
        var options = parameters["options"] as JsonArray ?? [];
        var toolCall = parameters["toolCall"] as JsonObject;
        var kind = StringValue(toolCall?["kind"]);

        if (!string.Equals(kind, EditToolKind, StringComparison.Ordinal))
        {
            return Option(options, "allow", report, $"a '{kind ?? "no-kind"}' call is not a write");
        }

        var paths = (toolCall?["locations"] as JsonArray ?? [])
            .OfType<JsonObject>()
            .Select(location => StringValue(location["path"]))
            .OfType<string>()
            .ToList();

        if (worktreeRoot is null || paths.Count == 0)
        {
            return Option(
                options, "reject", report, "an edit whose target is not stated cannot be shown to be in the lease");
        }

        foreach (var path in paths)
        {
            var relative = Path.GetRelativePath(worktreeRoot, path);
            if (Path.IsPathRooted(relative)
                || relative.StartsWith("..", StringComparison.Ordinal)
                || !lease.Covers(relative))
            {
                return Option(options, "reject", report, $"'{relative}' is outside the lease");
            }
        }

        return Option(options, "allow", report, $"{paths.Count} path(s) inside the lease");
    }

    /// <summary>Picks the first option whose declared kind starts with <paramref name="prefix"/>.</summary>
    private static string Option(JsonArray options, string prefix, Action<string> report, string because)
    {
        string? last = null;

        foreach (var option in options.OfType<JsonObject>())
        {
            if (StringValue(option["optionId"]) is not { } id)
            {
                continue;
            }

            last = id;
            if (StringValue(option["kind"]) is { } kind && kind.StartsWith(prefix, StringComparison.Ordinal))
            {
                report($"permission {prefix}: {because}");
                return id;
            }
        }

        report($"permission {prefix} (no option of that kind was offered): {because}");
        return last ?? prefix;
    }

    /// <summary>
    /// The nearest-rank percentile, or <c>null</c> when nothing was measured.
    /// </summary>
    /// <remarks>
    /// <b>Null, never 0.</b> A latency of zero and a latency nobody measured are different facts, and
    /// only one of them is good news (IO12).
    /// </remarks>
    private static double? Percentile(List<double> values, double quantile)
    {
        if (values.Count == 0)
        {
            return null;
        }

        var sorted = values.Order().ToList();
        var rank = (int)Math.Ceiling(quantile * sorted.Count) - 1;
        return sorted[Math.Clamp(rank, 0, sorted.Count - 1)];
    }

    /// <summary>
    /// Reads a JSON string value, or <c>null</c>.
    /// </summary>
    /// <remarks>
    /// <b>Named apart from Core's <c>AcpJson.Text</c> on purpose.</b> That helper is
    /// <c>internal</c> to <c>AiDe.Core</c>, so this assembly cannot reach it, and widening a
    /// one-line reader to <c>public</c> for a single caller buys a supported surface for nothing.
    /// The duplication is across an assembly boundary and is recorded rather than hidden.
    /// </remarks>
    private static string? StringValue(JsonNode? node)
        => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
}
