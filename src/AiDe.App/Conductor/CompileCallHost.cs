using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using AiDe.Core.AgentPlane;
using AiDe.Core.PromptCompilation;

namespace AiDe.App.Conductor;

/// <summary>What one compile call needs — host-side sources only; nothing here comes from a page or from the model.</summary>
/// <param name="RepositoryRoot">The checkout the compile session is rooted in (ADR-0035: the constitution loads only there).</param>
/// <param name="AdapterInstallRoot">The directory whose <c>node_modules</c> holds the adapter — and the CLI binary the pin is verified against.</param>
/// <param name="EngineId">The session's bound engine.</param>
/// <param name="Model">The session's bound model — <c>model_configured</c> on the <c>called</c> row.</param>
/// <param name="AccountLabel">The configured account the call bills against.</param>
/// <param name="Providers">The provider rows, parsed by the caller.</param>
/// <param name="Prompt">The assembled <c>compile-prompt/1</c> text — its first bytes the host header.</param>
/// <param name="BoundMs"><c>opened.constants.bound_ms</c>: one linked deadline over the whole of Start → close.</param>
/// <param name="ProofDirectory">Where the gate-1 artifact is read from; defaults to <see cref="CompilePinArtifact.DefaultDirectory"/>.</param>
public sealed record CompileRequest(
    string RepositoryRoot,
    string AdapterInstallRoot,
    string EngineId,
    string Model,
    string AccountLabel,
    IReadOnlyList<ProviderRow> Providers,
    string Prompt,
    int BoundMs,
    string? ProofDirectory = null);

/// <summary>How a compile call ended at the host — before the typed boundary reads any text.</summary>
public static class CompileCallOutcomes
{
    /// <summary>The prompt ended and the raw text is on the result; the validator decides <c>succeeded</c> / <c>malformed</c> / <c>suspect</c>.</summary>
    public const string Answered = "answered";

    /// <summary>No binding, no launch, an unverified pin, or an engine that did not start (<c>called.outcome: unavailable</c>).</summary>
    public const string Unavailable = CallOutcomes.Unavailable;

    /// <summary>The identity gate refused (<c>AP-0009</c>–<c>AP-0013</c>, or <c>Bind</c>'s own) — the same refusal a lane gets.</summary>
    public const string Refused = CallOutcomes.Refused;

    /// <summary>The linked deadline fired; <c>Reason</c> names the step.</summary>
    public const string TimedOut = CallOutcomes.TimedOut;

    /// <summary>The caller cancelled (an edit during <c>preparing</c>).</summary>
    public const string Cancelled = CallOutcomes.Cancelled;
}

/// <summary>What one compile call produced — the receipt the <c>called</c> row is written from (§A10.3; ADR-0035 rule 1).</summary>
/// <param name="Outcome">One of <see cref="CompileCallOutcomes"/>.</param>
/// <param name="Reason">Why, for every outcome but <see cref="CompileCallOutcomes.Answered"/>; on <c>timed_out</c> it names the step.</param>
/// <param name="RawText">The model's text, verbatim — read only by <c>CompileOutputValidator</c>.</param>
/// <param name="Cost">The usage the wire reported, or null (never a zero).</param>
/// <param name="ModelObserved">The model the wire reported, or <see cref="Envelope.NotRecorded"/>.</param>
/// <param name="LatencyMs">Prompt sent → prompt answered, or null when it did not answer.</param>
/// <param name="ToolCalls">Every <c>tool_call</c> frame drained, any name — non-zero marks the compile <c>suspect</c>.</param>
/// <param name="PermissionRequests">Every <c>session/request_permission</c> answered with the reject option.</param>
/// <param name="AdapterPid">The engine's process id, when one was started.</param>
/// <param name="LateAnswer">An answer that arrived after the deadline — discarded and counted.</param>
/// <param name="PinVerifyMs">What verifying the pin triple cost on this call (not cached until 50 measured compiles).</param>
/// <param name="ObservedAuthKind">The adapter's reported auth kind, or <see cref="Envelope.NotRecorded"/>.</param>
/// <param name="SessionNewParameters">The <c>session/new</c> params as sent, <c>_meta</c> included, or null when none was sent.</param>
/// <param name="Diagnostics">Every line the host reported, in order.</param>
public sealed record CompileResult(
    string Outcome,
    string? Reason,
    string? RawText,
    RunEventCost? Cost,
    string ModelObserved,
    int? LatencyMs,
    int ToolCalls,
    int PermissionRequests,
    int? AdapterPid,
    bool LateAnswer,
    long PinVerifyMs,
    string ObservedAuthKind,
    JsonObject? SessionNewParameters,
    IReadOnlyList<string> Diagnostics);

/// <summary>The engine a compile speaks to — <see cref="AcpEngineProcess"/> in the product; a pair of streams in a test.</summary>
internal interface ICompileEngine : IDisposable
{
    TextReader Output { get; }

    TextWriter Input { get; }

    int ProcessId { get; }

    bool HasExited { get; }
}

/// <summary>
/// The compile call's composition — the second, smaller composition of the agent plane's pieces,
/// and <b>not a run's composition root</b> (ADR-0035 rules 1 and 3).
/// </summary>
/// <remarks>
/// <para><b>Order:</b> pin verified → <c>ResolveLaunch</c> → <c>Start</c> at the repository root →
/// peer + client (a reject-by-kind chooser that counts) → <c>initialize</c> → observed auth →
/// <see cref="SpawnContract.AuthorizeBinding"/> → <c>session/new</c> with
/// <see cref="LaneSessionOptions.Compile"/> → the prompt → drain (counting <c>tool_call</c>) →
/// close. It never provisions a worktree, opens an episode, constructs a run request,
/// runs a seam monitor or scores — the negative-reference census over <c>Conductor/Compile*.cs</c>
/// holds the names out (<c>TheCompileCallIsNotARunRootTests</c>).</para>
///
/// <para><b>One linked deadline over the whole call</b> (ADR-0035 rule 1): <c>initialize</c>,
/// <c>session/new</c> and the prompt run under the same remaining budget, and a step that outlasts
/// it yields <c>timed_out</c> with the step named — the peer's own 60 s request and 15-minute prompt
/// timeouts would otherwise let a wedged adapter sit past the bound with no degraded state. PD-5's
/// second run is the measurement this exists for: a toolless session answered the read prompt with
/// tool-call XML as text in an unbounded loop, ~20k output tokens in six minutes, until it was
/// killed by hand. On <c>timed_out</c> and <c>cancelled</c> the engine is disposed — its tree
/// reaped — before the receipt is returned.</para>
///
/// <para><b>Distinguished in the root ledger by construction:</b> it opens
/// <c>compile-call.compose</c> on the same activity source, so <see cref="CompositionRootLedger.Roots"/>
/// reads 0 for a compile. No sibling ledger is written (ADR-0035 rule 3): the durable receipt is
/// the <c>called</c> row, and the span is the in-process count.</para>
///
/// <para>Patterns: Facade over the handshake (the run root's twin — the duplication is recorded,
/// the fold deferred: ADR-0035 follow-ups); least-privilege capability restriction (no tool
/// surface); Receipt Ledger (the <c>called</c> row). <c>[CapabilityTier(T3)]</c>'s reader is
/// <c>TheCompileCallIsNotARunRootTests</c> — every carrier of the attribute opens a <c>*.compose</c>
/// receipt.</para>
/// </remarks>
[CapabilityTier("T3")]
public static class CompileCallHost
{
    /// <summary>The activity this host opens for one composition — never <see cref="CompositionRootLedger.GovernedRunComposeActivity"/>.</summary>
    public const string ComposeActivity = "compile-call.compose";

    /// <summary>The bounded wait for the adapter's <c>_auth/status_update</c>, as the run root waits — and never past the linked deadline.</summary>
    private static readonly TimeSpan ObservedAuthWait = TimeSpan.FromSeconds(15);

    /// <summary>How long the peer loop may take to end after the engine is disposed — a reaped child closes its stdout at once.</summary>
    private static readonly TimeSpan PumpDrainGrace = TimeSpan.FromSeconds(5);

    /// <summary>Runs one compile call against the real engine.</summary>
    public static Task<CompileResult> CompileAsync(CompileRequest request, CancellationToken cancellationToken = default)
        => CompileAsync(request, StartEngine, cancellationToken);

    /// <summary>
    /// Runs one compile call with the engine <paramref name="startEngine"/> supplies — the product's
    /// <see cref="AcpEngineProcess"/>, or a test's pair of streams. One composition, exercised both ways.
    /// </summary>
    internal static async Task<CompileResult> CompileAsync(
        CompileRequest request,
        Func<EngineLaunch, string, Action<string>, ICompileEngine> startEngine,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(startEngine);

        // NOT the run root's activity: the root ledger counts only that name, so a compile reads 0.
        using var composed = CompositionRootSignal.Source.StartActivity(ComposeActivity);

        var diagnostics = new List<string>();
        var diagnosticsGate = new Lock();
        void Report(string line)
        {
            lock (diagnosticsGate)
            {
                diagnostics.Add(line);
            }
        }

        var clock = Stopwatch.StartNew();
        var permissionRequests = 0;

        CompileResult Result(string outcome, string? reason, int? pid = null, string? rawText = null, RunEventCost? cost = null,
            string? modelObserved = null, int? latencyMs = null, int toolCalls = 0, bool lateAnswer = false, long pinVerifyMs = 0,
            string? observedAuthKind = null, JsonObject? sessionNew = null)
        {
            lock (diagnosticsGate)
            {
                return new CompileResult(
                    outcome, reason, rawText, cost, modelObserved ?? Envelope.NotRecorded, latencyMs, toolCalls, permissionRequests, pid,
                    lateAnswer, pinVerifyMs, observedAuthKind ?? Envelope.NotRecorded, sessionNew, [.. diagnostics]);
            }
        }

        // THE PIN, VERIFIED AT EVERY CALL (ADR-0035 rule 1): the installed adapter file and CLI
        // binary are hashed and compared with the artifact's recorded triple before anything starts.
        var pinClock = Stopwatch.StartNew();
        var artifactPath = Path.Combine(request.ProofDirectory ?? CompilePinArtifact.DefaultDirectory, CompilePinArtifact.FileName);
        var artifact = CompilePinArtifact.Read(artifactPath, out var artifactProblem);
        if (artifact is null)
        {
            pinClock.Stop();
            Report($"pin: no artifact at {artifactPath}{(artifactProblem is null ? string.Empty : " (" + artifactProblem + ")")}");
            return Result(CompileCallOutcomes.Unavailable, "the pin is unverified for this adapter build: no compile-pin-spike artifact", pinVerifyMs: pinClock.ElapsedMilliseconds);
        }

        var installed = CompilePin.Installed(request.AdapterInstallRoot);
        if (CompilePin.Mismatch(installed, artifact.Triple) is { } mismatch)
        {
            pinClock.Stop();
            Report("pin: " + mismatch);
            return Result(CompileCallOutcomes.Unavailable, "the pin is unverified for this adapter build: " + mismatch, pinVerifyMs: pinClock.ElapsedMilliseconds);
        }

        pinClock.Stop();
        var pinVerifyMs = pinClock.ElapsedMilliseconds;
        Report($"pin: verified against {artifactPath} in {pinVerifyMs} ms");

        EngineLaunch launch;
        try
        {
            launch = EngineCatalog.ResolveLaunch(request.EngineId, request.AdapterInstallRoot);
        }
        catch (AgentPlaneException error)
        {
            Report($"launch refused {error.Code}: {error.Message}");
            return Result(CompileCallOutcomes.Unavailable, $"{error.Code} {error.Message}", pinVerifyMs: pinVerifyMs);
        }

        // ONE LINKED DEADLINE over Start → close. Every await below runs under `bound.Token`.
        using var bound = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        bound.CancelAfter(request.BoundMs);
        string Step() => cancellationToken.IsCancellationRequested ? CompileCallOutcomes.Cancelled : CompileCallOutcomes.TimedOut;
        string StepReason(string step) => cancellationToken.IsCancellationRequested
            ? "cancelled — draft edited"
            : $"compile bound {request.BoundMs} ms exceeded at {step}";

        ICompileEngine engine;
        try
        {
            engine = startEngine(launch, request.RepositoryRoot, Report);
        }
        catch (AgentPlaneException error)
        {
            Report($"engine refused {error.Code}: {error.Message}");
            return Result(CompileCallOutcomes.Unavailable, $"{error.Code} {error.Message}", pinVerifyMs: pinVerifyMs);
        }

        using (engine)
        {
            var pid = engine.ProcessId;
            Report($"engine pid {pid}");

            var peer = new AcpPeer(engine.Output, engine.Input, new AcpRunEventMapper("compile", "compile"), diagnostics: Report);
            var client = new AcpLaneClient(peer, choosePermission: parameters =>
            {
                // Reject by kind, and COUNT: a request that arrives at all is a finding (§A13.4 C1).
                Interlocked.Increment(ref permissionRequests);
                var chosen = RejectOption(parameters);
                Report($"permission request rejected ({chosen}): the compile session holds no tool");
                return chosen;
            });

            // The pump runs to END OF STREAM, not to the deadline: disposing the engine closes its
            // stdout, and every frame up to that point is read — so an answer that lands during the
            // teardown is counted (unmatched, its request gone with the deadline) rather than lost.
            var pump = peer.RunAsync(CancellationToken.None);
            var registry = new ProviderRegistry(request.Providers);
            string? observedKind = null;
            CompileResult? ended = null;
            string? deadlineStep = null;
            Task<JsonObject>? prompt = null;

            try
            {
                await client.InitializeAsync(bound.Token).ConfigureAwait(false);
                Report("initialize: the echoed protocolVersion was checked");

                var observed = await AwaitObservedAuthAsync(peer, bound.Token).ConfigureAwait(false);
                observedKind = observed?.Kind;

                BoundIdentity identity;
                try
                {
                    identity = SpawnContract.AuthorizeBinding(request.EngineId, request.Model, request.AccountLabel, observed, registry);
                }
                catch (AgentPlaneException error)
                {
                    Report($"binding refused {error.Code}: {error.Message}");
                    return Result(CompileCallOutcomes.Refused, $"{error.Code} {error.Message}", pid, pinVerifyMs: pinVerifyMs, observedAuthKind: observedKind);
                }

                Report($"binding: {identity.Binding.EngineId} {identity.Binding.Model} on {identity.Binding.Account.Label} ({identity.ObservedAuth.Kind})");

                // THE PIN ON THE WIRE — the named static, never an ad-hoc list (ADR-0035 rule 2).
                var session = await client.NewSessionAsync(request.RepositoryRoot, LaneSessionOptions.Compile, bound.Token).ConfigureAwait(false);
                var sent = client.SessionNewParameters;
                Report($"acp session {session} opened with session/new params {sent?.ToJsonString() ?? Envelope.NotRecorded}");

                var promptClock = Stopwatch.StartNew();
                prompt = client.PromptAsync(session, request.Prompt, bound.Token);
                var drained = await DrainAsync(peer.Events, prompt, bound.Token).ConfigureAwait(false);

                JsonObject answer;
                try
                {
                    answer = await prompt.ConfigureAwait(false);
                }
                catch (AgentPlaneException error)
                {
                    Report($"prompt refused {error.Code}: {error.Message}");
                    return Result(CompileCallOutcomes.Unavailable, $"{error.Code} {error.Message}", pid, pinVerifyMs: pinVerifyMs,
                        toolCalls: drained.ToolCalls, observedAuthKind: observedKind, sessionNew: sent);
                }

                promptClock.Stop();
                Report($"prompt stopReason: {Text(answer["stopReason"]) ?? Envelope.NotRecorded}; tool_call frames {drained.ToolCalls}; permission requests {permissionRequests}");

                ended = Result(
                    CompileCallOutcomes.Answered, null, pid,
                    rawText: drained.Text,
                    cost: CostOf(answer),
                    modelObserved: ModelObservedOf(answer),
                    latencyMs: checked((int)Math.Min(promptClock.ElapsedMilliseconds, int.MaxValue)),
                    toolCalls: drained.ToolCalls,
                    pinVerifyMs: pinVerifyMs,
                    observedAuthKind: observedKind,
                    sessionNew: sent);
            }
            catch (OperationCanceledException)
            {
                // THE DEADLINE, OR THE CALLER. The step is read from what was reported so far; the
                // engine is disposed in the finally below — its tree reaped — BEFORE the receipt is
                // built, and an answer that lands during the teardown is counted, never applied.
                deadlineStep = StepOf(diagnostics, diagnosticsGate);
                Report($"{Step()} at {deadlineStep} after {clock.ElapsedMilliseconds} ms");
            }
            catch (AgentPlaneException error)
            {
                Report($"handshake refused {error.Code}: {error.Message}");
                ended = Result(CompileCallOutcomes.Unavailable, $"{error.Code} {error.Message}", pid, pinVerifyMs: pinVerifyMs,
                    observedAuthKind: observedKind, sessionNew: client.SessionNewParameters);
            }
            finally
            {
                engine.Dispose();
                try
                {
                    // Bounded: a child whose stdout did not close with its kill is reported, never waited on forever.
                    await pump.WaitAsync(PumpDrainGrace).ConfigureAwait(false);
                }
                catch (Exception error) when (error is OperationCanceledException or AgentPlaneException or IOException or TimeoutException)
                {
                    Report("peer loop ended: " + error.Message);
                }
            }

            if (ended is not null)
            {
                return ended;
            }

            // WHAT LANDED AFTER THE DEADLINE, read from the plane's own queue once the pump has ended:
            // nobody drained it past the cancellation, so every event left is post-deadline. A
            // response frame there is a late answer — discarded, counted, never applied — and a tool
            // call there still counts (IO12: read from the wire, never assumed).
            var late = false;
            var lateToolCalls = 0;
            while (peer.Events.Reader.TryRead(out var leftover))
            {
                late |= prompt is not null && leftover.Event.Kind == "acp.result";
                lateToolCalls += leftover.Event.Kind == "tool.call" ? 1 : 0;
            }

            return Result(Step(), StepReason(deadlineStep!), pid, pinVerifyMs: pinVerifyMs, lateAnswer: late, toolCalls: lateToolCalls,
                observedAuthKind: observedKind, sessionNew: client.SessionNewParameters);
        }
    }

    /// <summary>The product's engine: <see cref="AcpEngineProcess"/> rooted at the repository.</summary>
    private static ICompileEngine StartEngine(EngineLaunch launch, string repositoryRoot, Action<string> report)
        => new RealEngine(AcpEngineProcess.Start(launch, repositoryRoot, report));

    private sealed class RealEngine(AcpEngineProcess process) : ICompileEngine
    {
        public TextReader Output => process.Output;

        public TextWriter Input => process.Input;

        public int ProcessId => process.ProcessId;

        public bool HasExited => process.HasExited;

        public void Dispose() => process.Dispose();
    }

    /// <summary>What the drain observed: the reply's text, chunk by chunk, and every tool-call frame of any name.</summary>
    internal sealed record Drained(string Text, int ToolCalls);

    /// <summary>
    /// Drains the plane's queue until the prompt has answered and nothing is left: the text chunks
    /// become the raw reply; every <c>tool.call</c> is counted (§A13.4 C1 — a non-zero count marks
    /// the compile <c>suspect</c>).
    /// </summary>
    internal static async Task<Drained> DrainAsync(AcpEventQueue queue, Task prompt, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(prompt);

        var text = new StringBuilder();
        var toolCalls = 0;

        void Observe(ObservedRunEvent run)
        {
            switch (run.Event.Kind)
            {
                case "agent.msg":
                    if (run.Event.Body["content"] is JsonObject content && Text(content["text"]) is { } chunk)
                    {
                        text.Append(chunk);
                    }

                    break;
                case "tool.call":
                    toolCalls++;
                    break;
            }
        }

        // THE QUEUE RACED AGAINST THE PROMPT, not polled after each event: the peer publishes the
        // prompt's result frame BEFORE it resolves the pending request (Ruling 11's ordinal), so a
        // drain that read the last event and then asked `prompt.IsCompleted` could wait on an empty
        // queue until the deadline — seen when the tests ran together, not alone.
        while (true)
        {
            while (queue.Reader.TryRead(out var run))
            {
                Observe(run);
            }

            if (prompt.IsCompleted)
            {
                break;
            }

            var readable = queue.Reader.WaitToReadAsync(cancellationToken).AsTask();
            var first = await Task.WhenAny(readable, prompt).ConfigureAwait(false);
            if (first == prompt)
            {
                while (queue.Reader.TryRead(out var run))
                {
                    Observe(run);
                }

                break;
            }

            if (!await readable.ConfigureAwait(false))
            {
                break;
            }
        }

        return new Drained(text.ToString(), toolCalls);
    }

    /// <summary>Waits briefly for the adapter's <c>_auth/status_update</c> — never past the linked deadline — and answers null if it never arrives.</summary>
    private static async Task<ObservedAuthStatus?> AwaitObservedAuthAsync(AcpPeer peer, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + ObservedAuthWait;
        while (peer.ObservedAuth is null && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(25, cancellationToken).ConfigureAwait(false);
        }

        return peer.ObservedAuth;
    }

    /// <summary>The step the call was at when the deadline fired, read from what was reported so far.</summary>
    private static string StepOf(List<string> diagnostics, Lock gate)
    {
        lock (gate)
        {
            if (diagnostics.Any(l => l.StartsWith("acp session ", StringComparison.Ordinal)))
            {
                return "prompt";
            }

            if (diagnostics.Any(l => l.StartsWith("binding: ", StringComparison.Ordinal)))
            {
                return "session/new";
            }

            if (diagnostics.Any(l => l.StartsWith("initialize: ", StringComparison.Ordinal)))
            {
                return "observed-auth";
            }

            return "initialize";
        }
    }

    /// <summary>The reject option by its declared kind, as <see cref="AcpLaneClient"/> picks it by default.</summary>
    private static string RejectOption(JsonObject parameters)
    {
        string? last = null;
        foreach (var option in (parameters["options"] as JsonArray ?? []).OfType<JsonObject>())
        {
            if (Text(option["optionId"]) is not { } id)
            {
                continue;
            }

            last = id;
            if (Text(option["kind"]) is { } kind && kind.StartsWith("reject", StringComparison.Ordinal))
            {
                return id;
            }
        }

        return last ?? "reject";
    }

    /// <summary>The cost the wire states, and only that — absent usage is null, never a zero (IO12).</summary>
    private static RunEventCost? CostOf(JsonObject answer)
    {
        if (answer["usage"] is not JsonObject usage)
        {
            return null;
        }

        static long Number(JsonNode? node) => node is JsonValue v && v.TryGetValue<long>(out var n) ? n : 0;
        return new RunEventCost(Number(usage["inputTokens"]), Number(usage["outputTokens"]), Number(usage["cachedReadTokens"]), Requests: 1);
    }

    /// <summary>
    /// The model the wire reported — the <c>_meta.quota.model_usage</c> entry with the most output
    /// tokens (PD-5's frames: a haiku routing turn beside the answering model) — or <i>not recorded</i>.
    /// </summary>
    internal static string ModelObservedOf(JsonObject answer)
    {
        ArgumentNullException.ThrowIfNull(answer);

        var usage = answer["_meta"]?["quota"]?["model_usage"] as JsonArray;
        string? best = null;
        var bestOut = -1L;
        foreach (var entry in usage?.OfType<JsonObject>() ?? [])
        {
            var model = Text(entry["model"]);
            var outTokens = entry["token_count"]?["outputTokens"] is JsonValue v && v.TryGetValue<long>(out var n) ? n : 0;
            if (model is not null && outTokens > bestOut)
            {
                best = model;
                bestOut = outTokens;
            }
        }

        return best ?? Envelope.NotRecorded;
    }

    private static string? Text(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
}

/// <summary>
/// LOA C1: the capability tier a type composes at — <b>with a reader</b>: a test enumerates every
/// carrier and asserts each opens a <c>*.compose</c> receipt, so the attribute is a control, not
/// prose in attribute syntax (ADR-0035 LOA mapping; the Tech Lead's condition).
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class CapabilityTierAttribute(string tier) : Attribute
{
    /// <summary>T0 · T1 · T2 · T3.</summary>
    public string Tier { get; } = tier;
}
