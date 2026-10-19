using System.IO;
using System.Text.Json.Nodes;
using AiDe.App.Workbench;
using AiDe.Core.AgentPlane;
using AiDe.Core.Dispatch;
using AiDe.Core.Facts;
using AiDe.Core.Ipc;
using AiDe.Core.Store;
using AiDe.Core.Store.Records;
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
/// <para><b>A read-only turn is the same root with the write half left out</b> (Ruling 73). The
/// request carries no lease (<see cref="GovernedRunRequest.IsReadOnly"/>), and from that one fact:
/// the spawn is authorized read-only (<see cref="SpawnRequest.ReadOnly"/>: the identity gates
/// unchanged, the goal-block precondition waived), <b>no worktree is cut</b> — the ACP session is rooted in the
/// repository root itself, because the lane cannot write and a throwaway tree would be exactly the
/// thing the operator must then clean up (Ruling 73's constraint; ADR-0035 roots the compile session
/// the same way, and outside the repository the constitution does not load) — the session is opened
/// with <see cref="ReadOnlyLaneSession"/>, no episode is opened and nothing is scored (an episode is
/// work judged against a done-condition; a Message has none), no seam monitor runs (there is no
/// lease to monitor), and the permission chooser refuses any edit that arrives anyway. The
/// composition-root activity is opened once either way, so one send reads one root whichever shape
/// the turn takes. <b>Boundary named:</b> rooted in the repository, the lane loads the repository's
/// own settings and hooks (<c>settingSources</c> user · project · local) — repository content runs
/// in the lane, the same exposure as opening Claude Code there (ADR-0035 accepts it for the
/// compile session on the same grounds). After the turn the tree is measured against its own
/// baseline (<see cref="TreeDelta"/>): a changed tree is Ruling 73 condition (2)'s stop.</para>
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

    /// <summary>The adapter's tool-call kinds that only read (<c>tools.js:64, :137, :201, :226</c>).</summary>
    private static readonly HashSet<string> ReadOnlyKinds = new(["read", "search", "fetch", "think"], StringComparer.Ordinal);

    /// <summary>
    /// What the governed lane's model may hold: everything the adapter's preset allows <b>except the
    /// shell</b>. Ruling 71 — the lease bounds the file writes the seams observe; it does not bound
    /// tools, and with no pin the lane holds <c>Bash</c> under whatever the user's, the repository's
    /// and the local settings allow. The pin is one value on one site, tested on the outgoing frame.
    /// </summary>
    internal static readonly LaneSessionOptions GovernedLaneSession = new(DisallowedTools: ["Bash"]) { ThinkingDisplay = LaneSessionOptions.ThinkingSummarized };

    /// <summary>
    /// What a read-only turn's model may hold: everything the adapter's preset allows <b>except
    /// every tool that writes the tree or a durable file, executes code, a shell or a process,
    /// delegates to an agent, sends a local file anywhere, or cannot be read</b> — Ruling 73, the
    /// second use of Ruling 71's typed argument.
    /// </summary>
    /// <remarks>
    /// <para><b>The names are <see cref="LaneSessionOptions.DeniedToolNames"/> — one constant</b>
    /// (ADR-0035 rule 2), read from two sources, not recalled: the SDK's schema union
    /// (<c>@anthropic-ai/claude-agent-sdk</c> 0.3.257 <c>sdk-tools.d.ts:11-56</c>) and the
    /// shipped CLI's own tool-name table (<c>claude-agent-sdk-win32-x64/claude.exe</c>, the
    /// <c>Amo</c> array, 183 names — the schema is a subset of the pool, and <c>PowerShell</c> is a
    /// shell the schema does not list). Every name in either is classified in
    /// <c>docs/proof/read-only-turn.md</c>. The SDK forwards the list verbatim as
    /// <c>--disallowedTools</c> (<c>sdk.mjs:100</c>); the adapter spreads it after its own
    /// (<c>acp-agent.js:6007</c>). Asserted as a set equality against a literal
    /// (<c>TheGovernedLaneHasNoShellTests</c>); an SDK or adapter bump re-reads both sources — the
    /// trigger P-D5 carries for the compile pin. The compile session adds <c>mcp__*</c>
    /// (<see cref="LaneSessionOptions.Compile"/>); this lane does not yet — PD-5's finding 1 is
    /// measured for the compile, and widening the read-only lane is its own attended run.</para>
    ///
    /// <para><b>A name in the CLI's table with no readable tool object is pinned, not cut:</b>
    /// <c>PowerShell</c> is the proof that such a name becomes a live tool by a remote flag with no
    /// version bump, and an unmatched deny is inert.</para>
    ///
    /// <para><b>Not closed here, named:</b> <c>mcp__*</c> tools the adapter loads from settings or
    /// claude.ai connectors independently of the frame's <c>mcpServers: []</c> (<c>acp-agent.js:5962</c>,
    /// <c>:5971</c>) — closing them needs <c>strictMcpConfig</c> / <c>disableClaudeAiConnectors</c>
    /// on the wire, a widening of Ruling 71's two-member record that is the conductor's to rule on
    /// (CV-3's settings belt; P-D5's check: no <c>mcp__</c> name in the observed tool list).</para>
    /// </remarks>
    internal static readonly LaneSessionOptions ReadOnlyLaneSession = new(DisallowedTools: LaneSessionOptions.DeniedToolNames) { ThinkingDisplay = LaneSessionOptions.ThinkingSummarized };

    /// <summary>
    /// Stage-0 triage for a read-only turn: no plan, no council, no seam to steward. Not
    /// <see cref="RunTriage.For"/>'s (a Message has no block; the skip path there stewards seams).
    /// </summary>
    private static readonly RunTriage ReadOnlyTriage = new(
        SkipsPlanAndCouncil: true,
        Stages: [RunStage.Triage, RunStage.Dispatch, RunStage.Converge],
        Reason: "a read-only turn writes nothing: no plan, no council, no seam to steward (Ruling 73)");

    /// <summary>
    /// Runs one governed lane to completion and scores it.
    /// </summary>
    /// <param name="request">What to run.</param>
    /// <param name="cancellationToken">Bounds the whole run.</param>
    /// <param name="sink">
    /// An optional observer of every event this run drains — <c>RunEventRelay.Publish</c> is what the
    /// Conductor Surface passes. <b>Null by default, and genuinely inert:</b> the headless path names
    /// no sink and the drain then behaves exactly as it did before this parameter existed.
    /// </param>
    /// <remarks>
    /// <b>The sink is last, after the cancellation token, on purpose.</b> The usual .NET ordering
    /// would put a token last, but <see cref="ConductorEntry"/> calls
    /// <c>RunAsync(request, bound.Token)</c> positionally and §F5 clause 5 asserts the root count by
    /// ledger — an added parameter must not become a reason to edit the one other caller, because an
    /// edit there is how "one composition root" starts being a claim about two.
    /// </remarks>
    public static async Task<GovernedRunResult> RunAsync(
        GovernedRunRequest request,
        CancellationToken cancellationToken = default,
        Action<ObservedRunEvent>? sink = null,
        AttemptRun? attempt = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        // CLAUSE 5'S ORACLE, AND IT OPENS BEFORE ANYTHING THAT CAN THROW. ResolveLaunch below is the
        // first statement that refuses, and CompositionRootLedger counts the ATTEMPT — so two calls
        // with an unknown engine id read 2 with no adapter, no node and no network. That is what
        // makes "one run, one root" falsifiable by a test anybody will re-run.
        using var composed = CompositionRootSignal.Source.StartActivity(
            CompositionRootLedger.GovernedRunComposeActivity);

        var diagnostics = new System.Collections.Concurrent.ConcurrentQueue<string>();
        void Report(string line) => diagnostics.Enqueue(line);

        // ADR-0041 §1: an attempt's run_id IS its dispatch key; a headless run (no envelope) mints its own.
        var runId = attempt?.DispatchKey ?? "run-" + Guid.NewGuid().ToString("n")[..8];
        var laneId = "lane-" + Guid.NewGuid().ToString("n")[..8];
        var time = TimeProvider.System;

        // The oracle for "zero terminal hosting" opens FIRST, so everything the run does afterwards
        // happens inside the window it counts.
        using var terminals = TerminalHostingLedger.Open();

        // R2 BEFORE ANY ENGINE: a write-shaped request (a lease) with no goal block is not a third
        // shape and is not demoted to read-only — it is the goal block's own refusal, here rather
        // than after a process was started for it.
        var triage = request.IsReadOnly
            ? ReadOnlyTriage
            : RunTriage.For(request.Goal ?? throw new AgentPlaneException(
                AgentPlaneErrorCodes.GoalBlockIncomplete,
                "a write-shaped run carries a lease and no goal block; no block, no spawn (R2)"));
        Report("triage: " + triage.Reason);

        var launch = EngineCatalog.ResolveLaunch(request.EngineId, request.AdapterInstallRoot);
        Report($"engine: {launch.FileName} {string.Join(' ', launch.Arguments)}");

        // THE ROW AND THE ACCOUNT'S ENVIRONMENT, BEFORE THE SPAWN (seam req-01M2GKC2RY667S51X22QVV7FGJ,
        // 2026-09-14): the engine starts before the binding is authorised — the auth the contract
        // reads is observed on the running peer — so the one thing the child must be born with, an
        // enterprise host on the account (Ruling 105), is looked up here without authorising anything.
        // An account the registry does not carry yields nothing and is refused where it always was.
        var row = EngineCatalog.Find(request.EngineId);
        var environment = EnvironmentFor(request);
        if (environment.Count > 0)
        {
            Report($"engine environment: {string.Join(", ", environment.Keys)} from account '{request.AccountLabel}'");
        }

        using var engine = AcpEngineProcess.Start(launch, request.RepositoryRoot, Report, environment: environment);
        Report($"engine pid {engine.ProcessId}");

        var provisioner = new WorktreeProvisioner(new ProcessRunner(), request.CoordCommand);
        var registry = new ProviderRegistry(request.Providers);

        // The shell's own watcher composition (WorkbenchShell), not a second one. A read-only turn
        // opens no episode and scores nothing, so it opens no watcher either.
        using var watcher = request.IsReadOnly
            ? null
            : WatcherHost.Open(request.DataDirectory, Path.Combine(request.DataDirectory, "loomkeeper-coord"), time);

        ProvisionedWorktree? worktree = null;

        var peer = new AcpPeer(
            engine.Output, engine.Input, new AcpRunEventMapper(runId, laneId), diagnostics: Report);

        // The permission policy IS the governance: an edit inside the lease is allowed, an edit
        // outside it is refused. A client that allows everything has removed the control the plane
        // exists to provide; one that refuses everything cannot do the work.
        // Told its engine: the claude pin goes to the claude-code adapter and to nothing else, and a
        // pin that is not sent is reported on this run's own lines, never dropped in silence.
        var client = new AcpLaneClient(
            peer,
            choosePermission: parameters => Decide(parameters, request.Lease, worktree?.Path, Report),
            engine: row,
            diagnostics: Report);

        var pump = peer.RunAsync(cancellationToken);

        await client.InitializeAsync(cancellationToken).ConfigureAwait(false);
        Report("initialize: the echoed protocolVersion was checked");

        var observed = await AwaitObservedAuthAsync(peer, cancellationToken).ConfigureAwait(false);

        // R2 and §4.2: no block, no spawn (for a lane that can write); and no observed subscription,
        // no spawn (for every lane). The shape is the lease's absence — the same fact that pins the
        // lane's tools below — never a claim the request makes about itself.
        var spawn = SpawnContract.Authorize(SpawnRequestFor(request, observed), registry);

        if (request.IsReadOnly)
        {
            // THE READ-ONLY HALF, inline so the write path's statements below stay where they are:
            // session at the repository root under the pin, the prompt, the drain with no seam
            // monitor, the tree measured against its own baseline — no worktree, no episode, no score.
            Report($"read-only turn: no worktree, no lease, no episode; lane {laneId} on {spawn.Binding.EngineId}");

            var runner = new ProcessRunner();
            var baseline = TreeStatus(runner, request.RepositoryRoot);

            var readOnlySession = await OpenReadOnlySessionAsync(client, request.RepositoryRoot, runId, laneId, spawn.Binding, Report, cancellationToken)
                .ConfigureAwait(false);

            await DispatchedGateAsync(attempt, Report, cancellationToken).ConfigureAwait(false);
            var readOnlyPrompt = client.PromptAsync(readOnlySession, request.Prompt, cancellationToken);

            var readOnlyDrained = await DrainAsync(peer.Events, readOnlyPrompt, seams: null, Report, sink, cancellationToken)
                .ConfigureAwait(false);
            await SettleAsync(attempt, readOnlyPrompt, Report).ConfigureAwait(false);

            var readOnlyOutcome = EpisodeOutcome.Completed;
            try
            {
                var answer = await readOnlyPrompt.ConfigureAwait(false);
                Report("prompt stopReason: " + (StringValue(answer["stopReason"]) ?? NotRecorded));
            }
            catch (AgentPlaneException error)
            {
                Report($"prompt refused {error.Code}: {error.Message}");
                readOnlyOutcome = EpisodeOutcome.Blocked;
            }

            // RULING 73 CONDITION (2), MEASURED ON THE NORMAL PATH: the tree after the turn against
            // the tree before it. Zero is the expected reading; anything else is an irreversible act
            // outside the plan and stops at the human — reported here by name, never averaged away.
            var delta = TreeDelta(baseline, TreeStatus(runner, request.RepositoryRoot));
            Report(delta switch
            {
                null => "read-only tree check: not recorded (git did not answer)",
                0 => "read-only tree check: unchanged against its baseline",
                _ => $"READ-ONLY VIOLATION: {delta} line(s) of the tree's state changed during the turn — stop at the human; the pin is re-spiked (Ruling 73 condition 2)",
            });

            if (delta > 0)
            {
                // A turn that wrote did not complete under its contract; a consumer must not read
                // Completed beside a non-zero delta.
                readOnlyOutcome = EpisodeOutcome.Blocked;
            }

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
                SessionId: NotRecorded,
                EpisodeId: NotRecorded,
                WorktreePath: request.RepositoryRoot,
                WorktreeBranch: NotRecorded,
                CoordInstalled: false,
                ObservedAuthKind: observed?.Kind ?? NotRecorded,
                ObservedAuthLabel: observed?.Label ?? NotRecorded,
                ObservedAuthPlan: observed?.Plan ?? NotRecorded,
                TerminalHostConstructions: terminals.Constructions,
                Stages: [.. triage.Stages.Select(s => s.ToString())],
                SkippedPlanAndCouncil: triage.SkipsPlanAndCouncil,
                TriageReason: triage.Reason,
                EventsObserved: readOnlyDrained.Events,
                EventKinds: [.. readOnlyDrained.Kinds.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)],
                LatencyMeasured: readOnlyDrained.LatenciesMs.Count,
                LatencyP50Ms: Percentile(readOnlyDrained.LatenciesMs, 0.50),
                LatencyP95Ms: Percentile(readOnlyDrained.LatenciesMs, 0.95),
                LatencyHost: Environment.MachineName,
                SeamsRaised: 0,
                SeamResolutionRatio: 1.0,
                Outcome: readOnlyOutcome.ToString(),
                WorktreeDisposition: "none: a read-only turn cuts no worktree (Ruling 73)",
                Scored: false,
                ScoreVerdict: NotRecorded,
                ScoreHeadline: NotRecorded,
                TaskClass: NotRecorded,
                SegmentIsComparable: false,
                IncomparableReason: "a read-only turn opens no episode and is not scored",
                Mode: null,
                EngineProcessId: engine.ProcessId,
                EngineExited: engine.HasExited,
                EnvironmentFindings: engine.EnvironmentFindings,
                Diagnostics: [.. diagnostics],
                ReadOnlyTreeDelta: delta);
        }

        worktree = provisioner.Provision(request.RepositoryRoot, spawn.Binding.EngineId, laneId);
        if (attempt is not null)
        {
            await attempt.Ledger.AppendAttemptOutcomeAsync(
                new OutcomeRow(attempt.DispatchKey, DispatchState.WorktreeProvisioned,
                    Detail: new JsonObject { ["path"] = worktree.Path, ["branch"] = worktree.Branch }.ToJsonString()),
                cancellationToken).ConfigureAwait(false);
        }
        Report($"worktree: {worktree.Branch} at {worktree.Path} (coord install: {worktree.CoordInstalled})");

        var seams = new LeaseMonitor(request.Lease!, worktree.Path);

        var identity = new LaneIdentity(
            LaneId: laneId,
            AgentName: spawn.Binding.EngineId + "-lane",
            RepositoryPath: request.RepositoryRoot,
            RepositoryDisplay: Path.GetFileName(request.RepositoryRoot.TrimEnd('\\', '/')),
            WorktreeBranch: worktree.Branch,
            WorktreePath: worktree.Path,
            Harness: spawn.Binding.EngineId,
            Model: spawn.Binding.Model);

        var session = new GovernedLaneSource(watcher!.Ingest).Open(identity, request.Goal!);
        Report($"episode {session.EpisodeId} opened on session {session.SessionId}");

        var acpSession = await OpenSessionAsync(client, worktree, runId, laneId, spawn.Binding, Report, cancellationToken)
            .ConfigureAwait(false);

        await DispatchedGateAsync(attempt, Report, cancellationToken).ConfigureAwait(false);
        var prompt = client.PromptAsync(acpSession, request.Prompt, cancellationToken);

        var drained = await DrainAsync(peer.Events, prompt, seams, Report, sink, cancellationToken)
            .ConfigureAwait(false);
        await SettleAsync(attempt, prompt, Report).ConfigureAwait(false);

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
            EventsObserved: drained.Events,
            EventKinds: [.. drained.Kinds.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)],
            LatencyMeasured: drained.LatenciesMs.Count,
            LatencyP50Ms: Percentile(drained.LatenciesMs, 0.50),
            LatencyP95Ms: Percentile(drained.LatenciesMs, 0.95),
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
    /// G2 (ADR-0041 §1): the attempt's <c>Dispatched</c> row, written immediately before the consequential
    /// <c>session/prompt</c>. No attempt (the headless entry): nothing to gate.
    /// </summary>
    private static async Task DispatchedGateAsync(AttemptRun? attempt, Action<string> report, CancellationToken cancellationToken)
    {
        if (attempt is null)
        {
            return;
        }

        // The host launches only when ITS OWN Dispatched row committed (partial unique index). Not committed — a
        // second launcher, or a sweep that resolved the attempt first — means no prompt is sent.
        var dispatched = await attempt.Ledger.AppendAttemptOutcomeAsync(
            new OutcomeRow(attempt.DispatchKey, DispatchState.Dispatched), cancellationToken).ConfigureAwait(false);
        if (!dispatched.Committed)
        {
            throw new AgentPlaneException(AttemptErrorCodes.NotDispatched,
                $"attempt {attempt.DispatchKey}: the Dispatched row did not commit; no prompt is sent");
        }

        report($"attempt {attempt.DispatchKey}: Dispatched");
    }

    /// <summary>
    /// G3: the one terminal row, from this host's own awaited prompt (ADR-0041 §2) — written after the drain, so
    /// every event the prompt produced was observed first.
    /// </summary>
    private static async Task SettleAsync(AttemptRun? attempt, Task<JsonObject> prompt, Action<string> report)
    {
        if (attempt is null)
        {
            return;
        }

        var terminal = AttemptTerminal.For(attempt.DispatchKey, prompt);
        await attempt.Ledger.AppendAttemptOutcomeAsync(terminal, CancellationToken.None).ConfigureAwait(false);
        report($"attempt {attempt.DispatchKey}: {terminal.State} {terminal.Cause ?? terminal.ErrorCode ?? ""}".TrimEnd());
    }

    /// <summary>
    /// Opens the lane's session and records the frame it was opened with — the params the client
    /// sent, <c>_meta</c> included — on the run's report and in the workbench log.
    /// </summary>
    /// <remarks>
    /// Ruling 71 (a): the F5 Proof Pack carries the outgoing <c>session/new</c>, and a frame somebody
    /// had to remember to capture is "not recorded". So it is emitted on the normal path, from the one
    /// site that opens a lane's session, and the value written is the object the client handed the
    /// peer — never a re-computation of what it should have sent.
    /// </remarks>
    /// <param name="binding">The turn's (engine, model, account) — written on the ledger row (Ruling 105 condition 3); null reads "not recorded".</param>
    internal static async Task<string> OpenSessionAsync(
        AcpLaneClient client,
        ProvisionedWorktree worktree,
        string runId,
        string laneId,
        LaneBinding? binding,
        Action<string> report,
        CancellationToken cancellationToken)
    {
        // R1 bullet 1: the ACP session's cwd IS the provisioned worktree. The provisioner's own type
        // is passed rather than a path string, so the lane cannot be rooted anywhere else. Ruling 71:
        // and the lane's shell is pinned off on the same frame.
        var acpSession = await client.NewSessionAsync(worktree, GovernedLaneSession, cancellationToken).ConfigureAwait(false);
        RecordSessionNew(client, acpSession, runId, laneId, binding, report);
        return acpSession;
    }

    /// <summary>
    /// Opens a <b>read-only</b> turn's session rooted in the repository root — no worktree is cut
    /// (Ruling 73) — with every write-capable tool disallowed, and records the frame exactly as
    /// <see cref="OpenSessionAsync"/> does.
    /// </summary>
    /// <remarks>
    /// The host's second session site. A string root, not a <see cref="ProvisionedWorktree"/>: what
    /// bounds where this lane may write is the pin, not its cwd.
    /// </remarks>
    internal static async Task<string> OpenReadOnlySessionAsync(
        AcpLaneClient client,
        string repositoryRoot,
        string runId,
        string laneId,
        LaneBinding? binding,
        Action<string> report,
        CancellationToken cancellationToken)
    {
        var acpSession = await client.NewSessionAsync(repositoryRoot, ReadOnlyLaneSession, cancellationToken).ConfigureAwait(false);
        RecordSessionNew(client, acpSession, runId, laneId, binding, report);
        return acpSession;
    }

    /// <summary>
    /// Records the <c>session/new</c> params the client actually sent, <c>_meta</c> included, on the
    /// run's report and as a <c>lane.session-new</c> workbench log line.
    /// </summary>
    private static void RecordSessionNew(AcpLaneClient client, string acpSession, string runId, string laneId, LaneBinding? binding, Action<string> report)
    {
        // What was sent, not what should have been: the object the client handed the peer. A client
        // that recorded nothing reads as "not recorded" — never as the frame it was meant to send.
        // The binding rides the same row (Ruling 105 condition 3): one turn, one (engine, model,
        // account) — the fact row a per-turn account switch is observed on.
        var sent = client.SessionNewParameters;
        report($"acp session {acpSession} opened with session/new params {sent?.ToJsonString() ?? NotRecorded}"
            + $" on {binding?.EngineId ?? NotRecorded} · {binding?.Model ?? NotRecorded} · {binding?.Account.Label ?? NotRecorded}");
        WorkbenchDiagnostics.LaneSessionNew(
            runId, laneId, acpSession, sent,
            binding?.EngineId ?? NotRecorded, binding?.Model ?? NotRecorded, binding?.Account.Label ?? NotRecorded);
    }

    /// <summary>
    /// The spawn request a run makes, with its shape read from the one fact the lane's pin is also
    /// read from — the request carries no lease (Ruling 73) — never from a claim.
    /// </summary>
    internal static SpawnRequest SpawnRequestFor(GovernedRunRequest request, ObservedAuthStatus? observed)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new SpawnRequest(
            request.Goal, request.EngineId, request.Model, request.AccountLabel, observed, ReadOnly: request.IsReadOnly);
    }

    /// <summary>
    /// The variables the engine's child is born with — the account's enterprise host through the
    /// engine's own variable (<see cref="EngineCatalog.LaunchEnvironment"/>), or nothing.
    /// </summary>
    /// <remarks>
    /// <para><b>A lookup, not a binding.</b> <see cref="SpawnContract.Authorize"/> still decides whether
    /// the account may run, after the peer's auth is observed; this only answers what the child must
    /// carry to reach the right tenant, which cannot wait for a process that is already running. An
    /// account the registry does not carry yields an empty environment — the contract's refusal is
    /// the one that names it, as it always did — so this never invents a host and never pre-empts
    /// that refusal. A host the engine cannot honour is refused here, before the spawn, by the
    /// catalog's own rule.</para>
    /// </remarks>
    internal static IReadOnlyDictionary<string, string> EnvironmentFor(GovernedRunRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return EngineCatalog.LaunchEnvironment(EngineCatalog.Find(request.EngineId), request.Providers, request.AccountLabel);
    }

    /// <summary>
    /// The tree's state at <paramref name="root"/> as text — <c>git status --porcelain</c>, the
    /// working-tree diff against <c>HEAD</c>, and a content hash per untracked path — or <c>null</c>
    /// when git did not answer: "not recorded", never an empty reading taken as clean.
    /// </summary>
    /// <remarks>
    /// Porcelain alone cannot see a second modification of a file that was already dirty at the
    /// baseline (the common case in a working checkout): its line is identical before and after.
    /// The diff makes a tracked re-modification visible; the hashes make an untracked one visible.
    /// Ignored paths and nested repositories are invisible to all three — a named gap.
    /// </remarks>
    internal static string? TreeStatus(IProcessRunner runner, string root)
    {
        ArgumentNullException.ThrowIfNull(runner);

        var status = runner.Run("git", ["status", "--porcelain"], root);
        if (status.ExitCode != 0)
        {
            return null;
        }

        var diff = runner.Run("git", ["diff", "HEAD"], root);
        if (diff.ExitCode != 0)
        {
            return null;
        }

        var untracked = status.StandardOutput
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.TrimEnd('\r'))
            .Where(line => line.StartsWith("?? ", StringComparison.Ordinal))
            .Select(line => line[3..].Trim('"'))
            .ToList();

        var hashes = string.Empty;
        if (untracked.Count > 0)
        {
            var hashed = runner.Run("git", ["hash-object", "--", .. untracked], root);
            if (hashed.ExitCode != 0)
            {
                return null;
            }

            hashes = hashed.StandardOutput;
        }

        return status.StandardOutput + "\n--- diff ---\n" + diff.StandardOutput + "\n--- untracked ---\n" + hashes;
    }

    /// <summary>
    /// How many lines differ between two readings of the tree (<see cref="TreeStatus"/>) — the
    /// operator's tree may be dirty before the turn, so the oracle is <i>unchanged against its
    /// baseline</i>, not <i>clean</i>. <c>null</c> when either reading is not recorded.
    /// </summary>
    internal static int? TreeDelta(string? before, string? after)
    {
        if (before is null || after is null)
        {
            return null;
        }

        var a = Lines(before);
        var b = Lines(after);
        return a.Except(b, StringComparer.Ordinal).Count() + b.Except(a, StringComparer.Ordinal).Count();

        static HashSet<string> Lines(string status) =>
            [.. status.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.TrimEnd('\r'))];
    }

    /// <summary>What one drain of the plane's queue observed. The run's own numbers, at their source.</summary>
    /// <param name="Events">How many normalized events arrived — <c>EventsObserved</c> on the result.</param>
    /// <param name="Kinds">Every kind, in receipt order.</param>
    /// <param name="LatenciesMs">Normalization latency for the events that carried one. Never padded.</param>
    internal sealed record DrainedEvents(int Events, IReadOnlyList<string> Kinds, IReadOnlyList<double> LatenciesMs);

    /// <summary>
    /// Drains the plane's queue until the prompt has answered and nothing is left, observing each
    /// event exactly once.
    /// </summary>
    /// <remarks>
    /// <para><b>One loop, one count, one sink call.</b> <see cref="AcpEventQueue"/> is
    /// <c>SingleReader = true</c>, so a console that wanted its own drain could not have one; the
    /// sink is therefore invoked from inside this loop, on the same iteration that increments the
    /// count. That is what makes "the sink received exactly <c>EventsObserved</c> events" an
    /// equality rather than an approximation — one authoritative producer for one quantity (DM7),
    /// and the same retain-never-rebuild rule <c>SessionLane</c> already applies to ordering.</para>
    ///
    /// <para><b>The sink is called after the count, and its exceptions are not swallowed.</b> A
    /// caught-and-ignored sink failure would leave the run reporting a number the console never
    /// received, which is the one outcome the equality exists to rule out.</para>
    ///
    /// <para><b>Extracted from <see cref="RunAsync"/> so the equality is testable at all.</b> A
    /// whole governed run needs an adapter, node, a provisioned worktree and a scored episode; this
    /// needs a queue. The shipped loop and the exercised loop are the same method, which is the
    /// point — a second copy for the test would be the hand-assembled harness N7 refuses.</para>
    /// </remarks>
    /// <param name="queue">The plane's own queue for this lane.</param>
    /// <param name="prompt">The in-flight prompt. Its completion is half the exit condition.</param>
    /// <param name="seams">
    /// The lease monitor every observed event is offered to; <c>null</c> for a read-only turn, which
    /// has no lease to monitor (Ruling 73 — the pin and <see cref="Decide"/> are its controls).
    /// </param>
    /// <param name="report">Where a raised seam is named.</param>
    /// <param name="sink">The optional second consumer. Null is inert.</param>
    /// <param name="cancellationToken">Bounds the drain.</param>
    internal static async Task<DrainedEvents> DrainAsync(
        AcpEventQueue queue,
        Task prompt,
        LeaseMonitor? seams,
        Action<string> report,
        Action<ObservedRunEvent>? sink,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(report);

        var kinds = new List<string>();
        var latencies = new List<double>();
        var events = 0;

        // THE DRAIN ENDS WHEN THE PROMPT HAS COMPLETED AND NOTHING IS QUEUED — WHICHEVER HAPPENS
        // LAST. The answer frame is published as an event BEFORE the reply completes the prompt
        // task, so a drain that re-checked the condition only on the next event's arrival could
        // consume the answer, see the prompt still open, and wait for an event that never comes
        // (found under Ruling 95's drain; TheDrainEndsWhenThePromptEndsTests). So the wait is on
        // both: the queue becoming readable, or the prompt ending — completed, faulted or cancelled.
        Task<bool>? readable = null;
        while (true)
        {
            readable ??= queue.Reader.WaitToReadAsync(cancellationToken).AsTask();
            await Task.WhenAny(readable, prompt).ConfigureAwait(false);

            if (readable.IsCompleted)
            {
                var open = await readable.ConfigureAwait(false);
                readable = null;
                if (!open)
                {
                    break;
                }

                while (queue.Reader.TryRead(out var run))
                {
                    events++;
                    kinds.Add(run.Event.Kind);

                    sink?.Invoke(run);

                    if (run.NormalizationLatency is { } latency)
                    {
                        latencies.Add(latency.TotalMilliseconds);
                    }

                    foreach (var seam in seams?.Observe(run.Event) ?? [])
                    {
                        report($"seam {seam.SeamId}: {seam.Path} is outside the lease");
                    }

                    if (prompt.IsCompleted && queue.Reader.Count == 0)
                    {
                        return new DrainedEvents(events, kinds, latencies);
                    }
                }
            }

            if (prompt.IsCompleted && queue.Reader.Count == 0)
            {
                break;
            }
        }

        return new DrainedEvents(events, kinds, latencies);
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
    ///
    /// <para><b>A read-only turn has no lease, and only a read is allowed</b> (Ruling 73). The
    /// adapter's <c>kind</c> is coarse — <c>edit</c> is Write/Edit only, a notebook edit or a shell
    /// arrives as <c>other</c> or <c>execute</c> (<c>tools.js:39, :100, :292</c>) — so on a lane with
    /// no lease the chooser allows the read kinds (<c>read</c>, <c>search</c>, <c>fetch</c>,
    /// <c>think</c>) and refuses everything else by name. The pin means no such request should ever
    /// arrive; this is the second belt, reported by name so the Proof Pack's observed tool-call names
    /// can carry it.</para>
    /// </remarks>
    internal static string Decide(JsonObject parameters, Lease? lease, string? worktreeRoot, Action<string> report)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(report);

        var options = parameters["options"] as JsonArray ?? [];
        var toolCall = parameters["toolCall"] as JsonObject;
        var kind = StringValue(toolCall?["kind"]);

        if (lease is null)
        {
            // The pinned NAME first (the request carries it, presentation.js:63): `Agent` arrives
            // as kind `think`, so a kind allow-list alone would let a delegation through if the pin
            // ever failed. Then the kind: only a read is allowed.
            var name = StringValue(toolCall?["name"]);
            var pinned = name is not null && ReadOnlyLaneSession.DisallowedTools!.Contains(name, StringComparer.Ordinal);

            return !pinned && ReadOnlyKinds.Contains(kind ?? string.Empty)
                ? Option(options, "allow", report, $"a '{kind}' call reads")
                : Option(
                    options, "reject", report,
                    $"READ-ONLY TURN: '{name ?? "unnamed"}' ({kind ?? "no-kind"}; {StringValue(toolCall?["title"]) ?? "untitled"}) was asked for on a lane opened with every write-capable tool disallowed; refused (Ruling 73 condition 2)");
        }

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
    private static double? Percentile(IReadOnlyList<double> values, double quantile)
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

// ---- The attempt host (docs/design/workbench-loops-wl-1a.md). Lives here until seam request
// req-01M599H0364THVM23WJJ68R11S rules on its own file, src/AiDe.App/Conductor/AttemptHost.cs.

/// <summary>
/// The execution-attempt host as the App binding sees it (ADR-0041; docs/design/workbench-loops-wl-1a.md).
/// </summary>
/// <remarks>
/// An interface over one class for one reason: T8 builds its binding in parallel and its UI tests need a host
/// with no engine behind it (the design's Simplifier condition 3). Every member answers with a receipt read back
/// from the store, never an in-memory guess.
/// </remarks>
public interface IAttemptHost
{
    /// <summary>
    /// Runs the gate and, when this call won it, launches the attempt. Returns once the gate decided; the run
    /// continues in the host and <see cref="AttemptStart.Completion"/> resolves after its terminal row committed.
    /// </summary>
    Task<AttemptStart> DispatchAsync(AttemptDispatch dispatch, CancellationToken cancellationToken);

    /// <summary>The attempt and its outcome events, or null when the key was never attempted.</summary>
    Task<AttemptReceipt?> ReadAsync(string dispatchKey, CancellationToken cancellationToken);
}

/// <summary>A prepared revision, ready for the gate.</summary>
/// <param name="Run">What the composition root runs.</param>
/// <param name="CurrentProjectionSha">The envelope fold's projection now — read at the gate (G1), never cached.</param>
/// <param name="RouteFingerprint">The route binding; <c>acp:&lt;engine-id&gt;</c> when the binding has none (WL-3 binds it).</param>
public sealed record AttemptDispatch(
    GovernedRunRequest Run,
    string WorkspaceId,
    long WorkspaceEpoch,
    string EnvelopeId,
    string ProjectionSha,
    string NotebookId,
    int RevisionNo,
    string SessionId,
    long SessionGeneration,
    Func<string?> CurrentProjectionSha,
    string? QueuedAfterDispatchKey = null,
    string? ParentDispatchKey = null,
    string? RouteFingerprint = null)
{
    /// <summary>The attempt's identity and its <c>run_id</c>: deterministic from the prepared envelope.</summary>
    public string DispatchKey => AttemptKey.For(EnvelopeId, ProjectionSha, QueuedAfterDispatchKey, ParentDispatchKey);

    /// <summary>The attempt row G0 writes.</summary>
    public AttemptRow ToRow() => new(
        DispatchKey, WorkspaceId, WorkspaceEpoch, NotebookId, RevisionNo, SessionId, SessionGeneration,
        ProjectionSha: ProjectionSha,
        RouteFingerprint: RouteFingerprint ?? "acp:" + Run.EngineId,
        ParentDispatchKey: ParentDispatchKey,
        QueuedAfterDispatchKey: QueuedAfterDispatchKey);
}

/// <param name="Receipt">The attempt as the gate left it.</param>
/// <param name="AlreadyAttempted">True when the key was already attempted or is already running here: nothing launched.</param>
/// <param name="Completion">The receipt after the terminal row committed; the existing receipt when nothing launched.</param>
public sealed record AttemptStart(AttemptReceipt Receipt, bool AlreadyAttempted, Task<AttemptReceipt> Completion);

/// <summary>The in-run write points <see cref="GovernedRunHost.RunAsync"/> takes for an attempt.</summary>
/// <param name="Ledger">Where the rows go.</param>
/// <param name="DispatchKey">The attempt — and the run's <c>run_id</c>.</param>
public sealed record AttemptRun(IAttemptLedger Ledger, string DispatchKey);

/// <summary>
/// The stateful attempt host: one per workspace, holding the live set and the gate choreography, and calling
/// <see cref="GovernedRunHost.RunAsync"/> — still the one composition root — for every launch.
/// </summary>
public sealed class AttemptHost : IAttemptHost, IDisposable
{
    private readonly IAttemptLedger _ledger;
    private readonly FileStream _hostLock;

    // The gate lock: held across L→G0 and across snapshot→sweep, never across a launch (design, "Error & concurrency").
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<string, object> _live = new(StringComparer.Ordinal);

    private AttemptHost(IAttemptLedger ledger, FileStream hostLock)
    {
        _ledger = ledger;
        _hostLock = hostLock;
    }

    /// <summary>The last sweep this host ran — at least the startup sweep.</summary>
    internal AttemptSweepResult? LastSweep { get; private set; }

    /// <summary>The attempts this host is running now.</summary>
    internal IReadOnlyCollection<string> Live
    {
        get
        {
            _gate.Wait();
            try
            {
                return [.. _live.Keys];
            }
            finally
            {
                _gate.Release();
            }
        }
    }

    /// <summary>
    /// Takes the workspace's host lock and runs the startup sweep (<c>host_restart</c>). A second host on the same
    /// workspace is refused here, so it can neither dispatch nor sweep another host's live attempts.
    /// </summary>
    /// <param name="hostLockFile"><c>&lt;workspace&gt;/.aide/attempt-host.lock</c>; the OS releases it when the process dies.</param>
    public static async Task<AttemptHost> OpenAsync(IAttemptLedger ledger, string hostLockFile, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        ArgumentException.ThrowIfNullOrEmpty(hostLockFile);

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(hostLockFile))!);
        FileStream hostLock;
        try
        {
            hostLock = new FileStream(hostLockFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException error)
        {
            throw new AgentPlaneException(AttemptErrorCodes.HostNotSole,
                $"another attempt host holds '{hostLockFile}' ({error.Message}); a second host can neither dispatch nor sweep");
        }

        var host = new AttemptHost(ledger, hostLock);
        await host.SweepAsync(AttemptCauses.HostRestart, cancellationToken).ConfigureAwait(false);
        return host;
    }

    public async Task<AttemptStart> DispatchAsync(AttemptDispatch dispatch, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dispatch);

        if (dispatch.QueuedAfterDispatchKey is not null)
        {
            // The queue's launch path (live from queue time, launched after the predecessor's ResponseComplete) is
            // WL-1a node 2; refused before any write rather than launched at once, which would skip the predecessor.
            throw new NotSupportedException("a queued dispatch is WL-1a node 2 (docs/proof/workbench-loops-wl-1a/host/handoff.md)");
        }

        var key = dispatch.DispatchKey;
        var owner = new object();

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // L before G0, so a launch in progress is never swept. Already live: a duplicate in flight — launch
            // nothing and touch nothing; the running owner's entry stays (only it deregisters).
            if (!_live.TryAdd(key, owner))
            {
                var running = await _ledger.ReadAttemptAsync(key, cancellationToken).ConfigureAwait(false);
                return Unlaunched(running!);
            }

            AttemptBeginResult begun;
            try
            {
                begun = await _ledger.BeginAttemptAsync(dispatch.ToRow(), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception error) when (error is IOException or OperationCanceledException or WorkspaceStoreException)
            {
                _live.Remove(key);
                throw;
            }

            if (begun.AlreadyAttempted)
            {
                _live.Remove(key);
                return Unlaunched(begun.Receipt);
            }
        }
        finally
        {
            _gate.Release();
        }

        // G1: the envelope's projection NOW against the one the attempt was prepared from (AC-E04b). No process.
        if (!string.Equals(dispatch.CurrentProjectionSha(), dispatch.ProjectionSha, StringComparison.Ordinal))
        {
            var rejected = await _ledger.AppendAttemptOutcomeAsync(
                new OutcomeRow(key, DispatchState.Rejected, AttemptErrorCodes.StalePrepare, Detail: "rejected_stale"),
                cancellationToken).ConfigureAwait(false);
            await DeregisterAsync(key, owner).ConfigureAwait(false);
            return new AttemptStart(rejected.Receipt, AlreadyAttempted: false, Task.FromResult(rejected.Receipt));
        }

        var started = (await _ledger.ReadAttemptAsync(key, cancellationToken).ConfigureAwait(false))!;
        return new AttemptStart(started, AlreadyAttempted: false, LaunchAsync(dispatch, key, owner));
    }

    private static AttemptStart Unlaunched(AttemptReceipt receipt) =>
        new(receipt, AlreadyAttempted: true, Task.FromResult(receipt));

    public Task<AttemptReceipt?> ReadAsync(string dispatchKey, CancellationToken cancellationToken) =>
        _ledger.ReadAttemptAsync(dispatchKey, cancellationToken);

    /// <summary>Resolves abandoned attempts; the live-set snapshot and the sweep are one critical section with L→G0.</summary>
    internal async Task<AttemptSweepResult> SweepAsync(string dispatchedCause, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            LastSweep = await _ledger.SweepAttemptsAsync(new AttemptSweepRequest([.. _live.Keys], dispatchedCause), cancellationToken)
                .ConfigureAwait(false);
            return LastSweep;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>E → terminal for an attempt whose G0 this call won.</summary>
    /// <remarks>
    /// Only the four exception types a run can end with are mapped to a terminal. Anything else — a process death
    /// among them — propagates and leaves the rows a crash would leave, for the next host's sweep.
    /// </remarks>
    private async Task<AttemptReceipt> LaunchAsync(AttemptDispatch dispatch, string key, object owner)
    {
        try
        {
            await GovernedRunHost.RunAsync(dispatch.Run, CancellationToken.None, sink: null, attempt: new AttemptRun(_ledger, key))
                .ConfigureAwait(false);
        }
        catch (Exception error) when (error is AgentPlaneException or IOException or OperationCanceledException or WorkspaceStoreException)
        {
            await SettleAfterThrowAsync(key, error).ConfigureAwait(false);
        }
        finally
        {
            await DeregisterAsync(key, owner).ConfigureAwait(false);
        }

        return (await _ledger.ReadAttemptAsync(key, CancellationToken.None).ConfigureAwait(false))!;
    }

    /// <summary>The terminal for a run that threw: before G2 nothing was sent (<c>Failed</c>), after it the effects are unknown.</summary>
    private async Task SettleAfterThrowAsync(string key, Exception error)
    {
        var receipt = await _ledger.ReadAttemptAsync(key, CancellationToken.None).ConfigureAwait(false);
        if (receipt is null || receipt.Terminal is not null)
        {
            return;
        }

        var code = error is AgentPlaneException plane ? plane.Code : error.GetType().Name;
        var terminal = receipt.Dispatched is null
            ? new OutcomeRow(key, DispatchState.Failed, AttemptErrorCodes.NotDispatched, Detail: code)
            : new OutcomeRow(key, DispatchState.DeliveryUnknown, DispatchErrorCodes.DeliveryUnknown, AttemptCauses.RelayLost, Detail: code);
        await _ledger.AppendAttemptOutcomeAsync(terminal, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>D: only the owner that won G0 removes the entry, and only after the terminal write.</summary>
    private async Task DeregisterAsync(string key, object owner)
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_live.TryGetValue(key, out var held) && ReferenceEquals(held, owner))
            {
                _live.Remove(key);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Releases the host lock — what a process death does, so a restarted host can take it.</summary>
    public void Dispose()
    {
        _hostLock.Dispose();
        _gate.Dispose();
    }
}

/// <summary>The <c>DeliveryUnknown</c> causes the host writes (one vocabulary: <see cref="WorkbenchVocabulary.DeliveryUnknownCauses"/>).</summary>
internal static class AttemptCauses
{
    public const string NeverDispatched = "never_dispatched";
    public const string RelayLost = "relay_lost";
    public const string PromptTimeout = "prompt_timeout";
    public const string HostRestart = "host_restart";
}

/// <summary>The terminal an awaited prompt maps to (ADR-0041 §2) — the host's own correlated reply, nothing else.</summary>
internal static class AttemptTerminal
{
    public static OutcomeRow For(string key, Task<JsonObject> prompt)
    {
        ArgumentNullException.ThrowIfNull(prompt);

        if (prompt.IsCompletedSuccessfully)
        {
            var stopReason = (prompt.Result["stopReason"] as JsonValue)?.TryGetValue<string>(out var reason) == true ? reason : null;
            var detail = new JsonObject
            {
                ["stopReason"] = stopReason,
                ["usage"] = prompt.Result["usage"]?.DeepClone() ?? JsonValue.Create(GovernedRunHost.NotRecorded),
            }.ToJsonString();

            // A cancelled reply is Interrupted only under a Stop whose high-water mark it follows (§4, node 2);
            // with no Stop requested it is not our acknowledgment, so its effects are unknown.
            return stopReason == "cancelled"
                ? new OutcomeRow(key, DispatchState.DeliveryUnknown, DispatchErrorCodes.DeliveryUnknown, AttemptCauses.RelayLost, detail)
                : new OutcomeRow(key, DispatchState.ResponseComplete, Detail: detail);
        }

        var error = prompt.Exception?.GetBaseException();
        return error switch
        {
            AgentPlaneException { Code: AgentPlaneErrorCodes.EngineRequestTimedOut } timedOut =>
                new OutcomeRow(key, DispatchState.DeliveryUnknown, DispatchErrorCodes.DeliveryUnknown, AttemptCauses.PromptTimeout, timedOut.Code),
            AgentPlaneException { Code: AgentPlaneErrorCodes.EngineStreamEnded } ended =>
                new OutcomeRow(key, DispatchState.DeliveryUnknown, DispatchErrorCodes.DeliveryUnknown, AttemptCauses.RelayLost, ended.Code),
            AgentPlaneException refused => new OutcomeRow(key, DispatchState.Failed, refused.Code),
            _ => new OutcomeRow(key, DispatchState.DeliveryUnknown, DispatchErrorCodes.DeliveryUnknown, AttemptCauses.RelayLost,
                error?.GetType().Name ?? "cancelled"),
        };
    }
}
