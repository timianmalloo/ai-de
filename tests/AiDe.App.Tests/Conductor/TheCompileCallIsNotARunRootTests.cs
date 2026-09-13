using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using AiDe.App.Conductor;
using AiDe.App.Tests.Sessions;
using AiDe.Core.AgentPlane;
using AiDe.Core.PromptCompilation;

namespace AiDe.App.Tests.Conductor;

/// <summary>
/// ADR-0035: the compile call is a second composition of the agent plane's pieces that is
/// <b>not</b> a run's root — pinned on the wire, gated on identity, bounded by one linked deadline,
/// counted apart from <c>governed-run.compose</c>, and kept out of the run root's types by census.
/// </summary>
/// <remarks>
/// <para><b>The real host over the real peer and client, with the child's stdio substituted</b> —
/// the same substitution <c>TheGovernedLaneHasNoShellTests</c> and <c>AcpPeerTests</c> make. A
/// scripted engine answers <c>initialize</c>, sends the auth status, answers <c>session/new</c> and
/// the prompt the way the corpus does, and can stay silent, emit a tool call, or answer late — so
/// every degraded state is driven by the one fact it names, with no node, no adapter and no model.
/// The pin is verified against a fixture install root whose bytes the artifact records.</para>
/// </remarks>
public sealed class TheCompileCallIsNotARunRootTests : IDisposable
{
    private readonly PinFixture _pin = new();

    public void Dispose() => _pin.Dispose();

    // ------------------------------------------------------------------ the censuses (ADR-0035 rule 3)

    /// <summary>
    /// The twelve names a compile host must never reference, enumerated <b>once</b>, run over the
    /// whole file set <c>src/AiDe.App/Conductor/Compile*.cs</c>, the set asserted non-empty before
    /// counting: a compile host that could authorise with a placeholder block or open a lane's tool
    /// set is an ungoverned lane in the primary checkout wearing the compile's name.
    /// </summary>
    [Fact]
    public void NoCompileFileReferencesTheRunRootsTypesOrThePlaceholderPaths()
    {
        string[] forbidden =
        [
            "GovernedRunHost", "GovernedRunRequest", "GovernedLaneSource", "WorktreeProvisioner", "LeaseMonitor",
            "LaneScoring", "IngestHost", "SpawnContract.Authorize(", "new GoalBlock(", "SessionTools.Lane", "preset",
            "governed-run.compose",
        ];

        var directory = Path.Combine(RepoRoot().FullName, "src", "AiDe.App", "Conductor");
        var files = Directory.EnumerateFiles(directory, "Compile*.cs", SearchOption.TopDirectoryOnly).Order(StringComparer.Ordinal).ToList();

        Assert.NotEmpty(files);
        Assert.Contains(files, f => Path.GetFileName(f) == "CompileCallHost.cs");

        var hits = new List<string>();
        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            foreach (var name in forbidden)
            {
                var index = text.IndexOf(name, StringComparison.Ordinal);
                while (index >= 0)
                {
                    // `Authorize(` inside `AuthorizeBinding(` is the factored gate, not the run's — the
                    // token is the run root's exact spelling with its opening parenthesis.
                    hits.Add($"{Path.GetFileName(file)}: '{name}' at {index}");
                    index = text.IndexOf(name, index + 1, StringComparison.Ordinal);
                }
            }
        }

        Assert.Empty(hits);
    }

    /// <summary>No sibling ledger: the compile is counted by its span and the <c>called</c> row, never a fourth <c>ActivityListener</c> ledger.</summary>
    [Fact]
    public void NoCompileLedgerExists()
    {
        var conductor = Path.Combine(RepoRoot().FullName, "src", "AiDe.App", "Conductor");
        Assert.DoesNotContain(Directory.EnumerateFiles(conductor, "*.cs"), f => Path.GetFileName(f).Contains("Ledger", StringComparison.Ordinal) && Path.GetFileName(f).Contains("Compile", StringComparison.Ordinal));
        Assert.DoesNotContain(typeof(CompileCallHost).Assembly.GetTypes(), t => t.Name.Contains("CompileCallLedger", StringComparison.Ordinal));
    }

    /// <summary>
    /// LOA C1 with a reader: every type carrying <c>[CapabilityTier]</c> declares a
    /// <c>*.compose</c> activity name and opens it — the attribute is a control, not prose.
    /// </summary>
    [Fact]
    public void EveryCapabilityTierCarrierOpensAComposeReceipt()
    {
        var carriers = typeof(CompileCallHost).Assembly.GetTypes()
            .Where(t => t.GetCustomAttributes(typeof(CapabilityTierAttribute), inherit: false).Length > 0)
            .ToList();

        Assert.NotEmpty(carriers);
        foreach (var carrier in carriers)
        {
            var activity = carrier.GetField("ComposeActivity")?.GetRawConstantValue() as string;
            Assert.False(string.IsNullOrEmpty(activity), $"{carrier.Name} carries [CapabilityTier] and declares no ComposeActivity");
            Assert.EndsWith(".compose", activity, StringComparison.Ordinal);
            Assert.NotEqual(CompositionRootLedger.GovernedRunComposeActivity, activity);

            var source = File.ReadAllText(Path.Combine(RepoRoot().FullName, "src", "AiDe.App", "Conductor", carrier.Name + ".cs"));
            Assert.Contains("StartActivity(ComposeActivity)", source, StringComparison.Ordinal);
        }
    }

    // ------------------------------------------------------------------ the ledger (ADR-0035 rule 3; US-D8)

    /// <summary>One compile inside an open ledger reads <c>Roots == 0</c> and one <c>compile-call.compose</c>; a run after it reads <c>Roots == 1</c>.</summary>
    [Fact]
    public async Task ACompileReadsZeroRootsAndARunAfterItReadsOne()
    {
        using var ledger = CompositionRootLedger.Open();
        using var compiles = ComposeCounter.Open(CompileCallHost.ComposeActivity);

        var result = await CompileCallHost.CompileAsync(_pin.Request(boundMs: 10_000), Using(ScriptedEngine.Answering()), CancellationToken.None);

        Assert.True(result.Outcome == CompileCallOutcomes.Answered, result.Reason + " / " + string.Join(" / ", result.Diagnostics));
        Assert.Equal(0, ledger.Roots);
        Assert.Equal(1, compiles.Count);

        await Assert.ThrowsAsync<AgentPlaneException>(() => GovernedRunHost.RunAsync(TheOneCompositionRootIsCountedTests.RefusedRequest()));
        Assert.Equal(1, ledger.Roots);
    }

    // ------------------------------------------------------------------ the wire (ADR-0035 rule 2; US-D8 b2)

    /// <summary>
    /// Through the host: <c>session/new</c> carries exactly <c>{cwd, mcpServers: [], _meta}</c>,
    /// <c>_meta.claudeCode.options</c> exactly <c>{tools: [], disallowedTools}</c> with every denied
    /// name and <c>mcp__*</c>, its <c>cwd</c> the repository root (no worktree), the frame recorded
    /// on the result as sent.
    /// </summary>
    [Fact]
    public async Task TheCompileSessionIsOpenedAtTheRepositoryRootWithThePinTriple()
    {
        var engine = ScriptedEngine.Answering();

        var result = await CompileCallHost.CompileAsync(_pin.Request(boundMs: 10_000), Using(engine), CancellationToken.None);

        Assert.Equal(CompileCallOutcomes.Answered, result.Outcome);
        var frame = engine.Sent.Single(f => f["method"]?.GetValue<string>() == "session/new");
        var parameters = frame["params"]!.AsObject();

        Assert.Equal(["cwd", "mcpServers", "_meta"], parameters.Select(m => m.Key));
        Assert.Equal(_pin.RepositoryRoot, parameters["cwd"]!.GetValue<string>());
        Assert.Empty(parameters["mcpServers"]!.AsArray());
        var options = parameters["_meta"]!["claudeCode"]!["options"]!.AsObject();
        Assert.Equal(["tools", "disallowedTools", "strictMcpConfig", "model"], options.Select(m => m.Key));
        Assert.Empty(options["tools"]!.AsArray());
        var disallowed = options["disallowedTools"]!.AsArray().Select(n => n!.GetValue<string>()).ToHashSet(StringComparer.Ordinal);
        Assert.Equal(LaneSessionOptions.DeniedToolNames.Append("mcp__*").ToHashSet(StringComparer.Ordinal), disallowed);
        Assert.True(options["strictMcpConfig"]!.GetValue<bool>());
        // The binding's model, host-authored — never the CLI's own resolution (a repository settings.json could pick what bills).
        Assert.Equal("claude-sonnet-5", options["model"]!.GetValue<string>());
        Assert.Equal("4096", CompileCallHost.CompileChildEnvironment[AcpEngineProcess.MaxOutputTokensVariable]);

        Assert.Equal(parameters.ToJsonString(), result.SessionNewParameters?.ToJsonString());
        Assert.Contains(result.Diagnostics, line => line.Contains("session/new params", StringComparison.Ordinal) && line.Contains("mcp__*", StringComparison.Ordinal));
        // CLAUDE_CODE_EXECUTABLE never reaching the child is Core's (TheCompileSessionIsPinnedTests), the one Start site both hosts share.
    }

    /// <summary>The receipt: the reply's text verbatim, the cost the wire stated, the model the wire reported, a latency, zero counts.</summary>
    [Fact]
    public async Task AnAnsweredCompileCarriesTheWiresTextCostAndModel()
    {
        var result = await CompileCallHost.CompileAsync(_pin.Request(boundMs: 10_000), Using(ScriptedEngine.Answering()), CancellationToken.None);

        Assert.Equal(CompileCallOutcomes.Answered, result.Outcome);
        Assert.Equal("""{"contract":"compile-output/1","decorations":[]}""", result.RawText);
        Assert.NotNull(result.Cost);
        Assert.Equal(2, result.Cost!.TokensIn);
        Assert.Equal(469, result.Cost.TokensOut);
        Assert.Equal(4870, result.Cost.CacheRead);
        Assert.Equal("claude-opus-5[1m]", result.ModelObserved);
        Assert.NotNull(result.LatencyMs);
        Assert.Equal(0, result.ToolCalls);
        Assert.Equal(0, result.PermissionRequests);
        Assert.False(result.LateAnswer);
        Assert.Equal("account", result.ObservedAuthKind);
        Assert.True(result.PinVerifyMs >= 0);
    }

    /// <summary>Absent usage is null, never zero; an absent model_usage is <i>not recorded</i> (IO12).</summary>
    [Fact]
    public async Task AbsentUsageIsNullAndAnAbsentModelIsNotRecorded()
    {
        var result = await CompileCallHost.CompileAsync(_pin.Request(boundMs: 10_000), Using(ScriptedEngine.Answering(promptResult: """{"stopReason":"end_turn"}""")), CancellationToken.None);

        Assert.Equal(CompileCallOutcomes.Answered, result.Outcome);
        Assert.Null(result.Cost);
        Assert.Equal(Envelope.NotRecorded, result.ModelObserved);
    }

    // ------------------------------------------------------------------ the counts (§A13.4 C1; ADR-0035 test 6)

    /// <summary>A <c>tool_call</c> frame is counted; a <c>session/request_permission</c> is answered with the reject option and counted.</summary>
    [Fact]
    public async Task AToolCallFrameAndAPermissionRequestAreCountedAndTheRequestIsRejected()
    {
        var engine = ScriptedEngine.Answering(beforeAnswer:
        [
            """{"jsonrpc":"2.0","method":"session/update","params":{"sessionId":"s-c","update":{"sessionUpdate":"tool_call","toolCallId":"c1","title":"mcp__pd5-fixture__write_note","kind":"other","status":"pending"}}}""",
            """{"jsonrpc":"2.0","id":0,"method":"session/request_permission","params":{"sessionId":"s-c","toolCall":{"toolCallId":"c1","title":"write_note","kind":"other"},"options":[{"optionId":"allow-once","kind":"allow_once"},{"optionId":"reject","kind":"reject_once"}]}}""",
        ]);

        var result = await CompileCallHost.CompileAsync(_pin.Request(boundMs: 10_000), Using(engine), CancellationToken.None);

        Assert.Equal(CompileCallOutcomes.Answered, result.Outcome);
        Assert.Equal(1, result.ToolCalls);
        Assert.Equal(1, result.PermissionRequests);
        var answer = engine.Sent.Single(f => f["id"]?.GetValue<int>() == 0 && f["result"] is not null);
        Assert.Equal("reject", answer["result"]!["outcome"]!["optionId"]!.GetValue<string>());
    }

    // ------------------------------------------------------------------ the identity gate (ADR-0035 test 4)

    /// <summary>An observed non-subscription auth refuses with the lane's own <c>AP-0011</c> — and no <c>session/new</c> is sent.</summary>
    [Fact]
    public async Task AnObservedApiKeyRefusesTheCompileBeforeAnySessionIsOpened()
    {
        var engine = ScriptedEngine.Answering(authKind: "apiKey");

        var result = await CompileCallHost.CompileAsync(_pin.Request(boundMs: 10_000), Using(engine), CancellationToken.None);

        Assert.Equal(CompileCallOutcomes.Refused, result.Outcome);
        Assert.StartsWith(AgentPlaneErrorCodes.ObservedAuthNotSubscription, result.Reason, StringComparison.Ordinal);
        Assert.Null(result.SessionNewParameters);
        Assert.DoesNotContain(engine.Sent, f => f["method"]?.GetValue<string>() == "session/new");
        Assert.True(engine.Disposed);
    }

    /// <summary>The direct-api engine under a configured subscription is refused by the terms — the same <c>AP-0009</c> a lane gets — before any engine starts.</summary>
    [Fact]
    public async Task ADirectApiEngineIsRefusedByTheTermsWithoutStartingAnything()
    {
        var started = false;
        var result = await CompileCallHost.CompileAsync(
            _pin.Request(boundMs: 10_000) with { EngineId = ProviderRegistry.DirectApiEngineId },
            (_, _, _) => { started = true; return ScriptedEngine.Answering(); },
            CancellationToken.None);

        // The catalog refuses direct-api before the terms can: `unavailable` with the catalog's own
        // code, and nothing started. AP-0009 itself is proven through AuthorizeBinding in Core.
        Assert.Equal(CompileCallOutcomes.Unavailable, result.Outcome);
        Assert.StartsWith(AgentPlaneErrorCodes.UnknownEngine, result.Reason, StringComparison.Ordinal);
        Assert.False(started);
    }

    // ------------------------------------------------------------------ the pin (ADR-0035 test 1; US-D8)

    /// <summary>A one-byte-different <c>acp-agent.js</c> yields <c>unavailable</c> with no <c>session/new</c> — and no engine started.</summary>
    [Fact]
    public async Task AOneByteDifferentAdapterYieldsUnavailableWithNoSession()
    {
        File.AppendAllText(_pin.AdapterAgentPath, "x");
        var started = false;

        var result = await CompileCallHost.CompileAsync(_pin.Request(boundMs: 10_000), (_, _, _) => { started = true; return ScriptedEngine.Answering(); }, CancellationToken.None);

        Assert.Equal(CompileCallOutcomes.Unavailable, result.Outcome);
        Assert.Contains("the pin is unverified for this adapter build", result.Reason, StringComparison.Ordinal);
        Assert.Contains("acp-agent.js", result.Reason, StringComparison.Ordinal);
        Assert.Null(result.SessionNewParameters);
        Assert.False(started);
    }

    /// <summary>No artifact at all: the same refusal, and the pin's cost is still measured.</summary>
    [Fact]
    public async Task NoPinArtifactYieldsUnavailable()
    {
        File.Delete(_pin.ArtifactPath);

        var result = await CompileCallHost.CompileAsync(_pin.Request(boundMs: 10_000), Using(ScriptedEngine.Answering()), CancellationToken.None);

        Assert.Equal(CompileCallOutcomes.Unavailable, result.Outcome);
        Assert.Contains("no compile-pin-spike artifact", result.Reason, StringComparison.Ordinal);
    }

    // ------------------------------------------------------------------ the linked deadline (ADR-0035 test 7)

    /// <summary>
    /// One linked deadline, not three: a peer silent at <c>initialize</c> degrades at the injected
    /// bound with <c>reason</c> naming <c>initialize</c>, the engine disposed, no <c>session/new</c>.
    /// </summary>
    /// <remarks>
    /// <b>The outcome is the oracle, not a stopwatch</b> (DC-107: a measured duration against a
    /// constant depends on the machine). Had the peer's own 60 s request timeout fired instead of the
    /// linked bound, the host would read <c>unavailable</c> with <c>AP-0015</c> — a different outcome
    /// — which is exactly what the mutation "no <c>CancelAfter</c>" produced (after a minute).
    /// </remarks>
    [Fact]
    public async Task APeerSilentAtInitializeTimesOutAtTheLinkedBoundNamingTheStep()
    {
        var engine = ScriptedEngine.Silent();

        var result = await CompileCallHost.CompileAsync(_pin.Request(boundMs: 150), Using(engine), CancellationToken.None);

        Assert.Equal(CompileCallOutcomes.TimedOut, result.Outcome);
        Assert.Equal("compile bound 150 ms exceeded at initialize", result.Reason);
        Assert.True(engine.Disposed);
        Assert.Null(result.SessionNewParameters);
        Assert.False(result.LateAnswer);
        Assert.DoesNotContain(result.Diagnostics, line => line.Contains(AgentPlaneErrorCodes.EngineRequestTimedOut, StringComparison.Ordinal));
    }

    /// <summary>
    /// The prompt outlasting the bound — PD-5's runaway shape: chunks that never end — yields
    /// <c>timed_out</c> at <c>prompt</c>, the engine disposed before the receipt; an answer that lands
    /// during the teardown is discarded and counted as <c>late_answer</c>.
    /// </summary>
    [Fact]
    public async Task APromptThatOutlastsTheBoundTimesOutAndALateAnswerIsCountedNotApplied()
    {
        var engine = ScriptedEngine.Answering(answerPromptOnDispose: true);

        var result = await CompileCallHost.CompileAsync(_pin.Request(boundMs: 2_000), Using(engine), CancellationToken.None);

        Assert.Equal(CompileCallOutcomes.TimedOut, result.Outcome);
        Assert.Equal("compile bound 2000 ms exceeded at prompt", result.Reason);
        Assert.Null(result.RawText);
        Assert.True(engine.Disposed);
        Assert.True(result.LateAnswer, string.Join(" / ", result.Diagnostics));
        Assert.NotNull(result.SessionNewParameters);
    }

    /// <summary>
    /// PD-5 run 2's shape, deterministically: a reply that outruns <see cref="CompileCallHost.OutputCharBound"/>
    /// is cut — <c>malformed</c> naming the bound (a loop, not a schema failure), the engine disposed,
    /// no raw text returned — long before the wall-clock deadline.
    /// </summary>
    [Fact]
    public async Task AReplyThatOutrunsTheOutputBoundIsCutAsMalformedNamingTheBound()
    {
        var chunk = new string('x', 4_096);
        var frames = Enumerable.Range(0, 6).Select(_ => "{\"jsonrpc\":\"2.0\",\"method\":\"session/update\",\"params\":{\"sessionId\":\"s-c\",\"update\":{\"sessionUpdate\":\"agent_message_chunk\",\"content\":{\"type\":\"text\",\"text\":\"" + chunk + "\"}}}}").ToList();
        var engine = ScriptedEngine.Answering(beforeAnswer: frames, answerPrompt: false);

        var result = await CompileCallHost.CompileAsync(_pin.Request(boundMs: 60_000), Using(engine), CancellationToken.None);

        Assert.Equal(CallOutcomes.Malformed, result.Outcome);
        Assert.StartsWith("output bound exceeded at", result.Reason, StringComparison.Ordinal);
        Assert.Null(result.RawText);
        Assert.True(engine.Disposed);
    }

    /// <summary>The caller's cancellation (an edit during <c>preparing</c>) is <c>cancelled</c>, not <c>timed_out</c>, and the engine is disposed.</summary>
    [Fact]
    public async Task TheCallersCancellationIsCancelledAndTheEngineIsDisposed()
    {
        var engine = ScriptedEngine.Answering(answerPromptOnDispose: false, answerPrompt: false);
        using var cancel = new CancellationTokenSource(200);

        var result = await CompileCallHost.CompileAsync(_pin.Request(boundMs: 60_000), Using(engine), cancel.Token);

        Assert.Equal(CompileCallOutcomes.Cancelled, result.Outcome);
        Assert.Equal("cancelled — draft edited", result.Reason);
        Assert.True(engine.Disposed);
    }

    // ------------------------------------------------------------------ the drift control (ADR-0035 follow-ups)

    /// <summary>Both hosts authorize the binding before they open a session — the run root by source order, the compile host by the scripted wire.</summary>
    [Fact]
    public async Task BothHostsAuthorizeTheBindingBeforeTheyOpenASession()
    {
        var host = File.ReadAllText(Path.Combine(RepoRoot().FullName, "src", "AiDe.App", "Conductor", "GovernedRunHost.cs"));
        var authorize = host.IndexOf("SpawnContract.Authorize(", StringComparison.Ordinal);
        var readOnly = host.IndexOf("OpenReadOnlySessionAsync(client", StringComparison.Ordinal);
        var governed = host.IndexOf("OpenSessionAsync(client, worktree", StringComparison.Ordinal);
        Assert.True(authorize >= 0 && readOnly >= 0 && governed >= 0, "a token is absent — IndexOf's -1 would make the order vacuous");
        Assert.True(authorize < readOnly);
        Assert.True(authorize < governed);

        var engine = ScriptedEngine.Answering(authKind: "apiKey");
        await CompileCallHost.CompileAsync(_pin.Request(boundMs: 10_000), Using(engine), CancellationToken.None);
        Assert.Equal(["initialize"], engine.Sent.Where(f => f["method"] is not null).Select(f => f["method"]!.GetValue<string>()));
    }

    // ------------------------------------------------------------------ doubles

    /// <summary>The one engine the host is handed, whatever launch it resolved.</summary>
    private static Func<EngineLaunch, string, Action<string>, ICompileEngine> Using(ScriptedEngine engine) => (_, _, _) => engine;

    /// <summary>Counts activities of one name on the composition source — the in-process count ADR-0035 rule 3 says a test may open.</summary>
    private sealed class ComposeCounter : IDisposable
    {
        private readonly ActivityListener _listener;
        private long _count;

        private ComposeCounter(string name)
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == CompositionRootLedger.CompositionActivitySource,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStarted = activity =>
                {
                    if (activity.OperationName == name)
                    {
                        Interlocked.Increment(ref _count);
                    }
                },
            };
            ActivitySource.AddActivityListener(_listener);
        }

        public long Count => Interlocked.Read(ref _count);

        public static ComposeCounter Open(string name) => new(name);

        public void Dispose() => _listener.Dispose();
    }

    /// <summary>A fixture install root whose bytes the pin artifact records, and a request against it.</summary>
    private sealed class PinFixture : IDisposable
    {
        private readonly string _root = Directory.CreateTempSubdirectory("aide-compile-host-").FullName;

        public PinFixture()
        {
            RepositoryRoot = Path.Combine(_root, "repo");
            Directory.CreateDirectory(RepositoryRoot);
            var modules = Path.Combine(InstallRoot, "node_modules");
            Write(Path.Combine(modules, "@agentclientprotocol", "claude-agent-acp", "package.json"), """{"version":"0.75.1"}""");
            Write(AdapterAgentPath, "// adapter\n");
            Write(Path.Combine(modules, "@anthropic-ai", "claude-agent-sdk", "package.json"), """{"version":"0.3.257"}""");
            Write(Path.Combine(modules, "@anthropic-ai", CompilePin.CliPlatformPackage, CompilePin.CliFileName), "MZ\n");

            var installed = CompilePin.Installed(InstallRoot);
            Directory.CreateDirectory(ProofDirectory);
            File.WriteAllText(Path.Combine(ProofDirectory, CompilePinArtifact.FrameLogFileName), string.Empty);
            File.WriteAllText(ArtifactPath, new JsonObject
            {
                ["pin_triple"] = new JsonObject
                {
                    ["adapter_version"] = installed.AdapterVersion,
                    ["adapter_sha256"] = installed.AdapterSha256,
                    ["sdk_version"] = installed.SdkVersion,
                    ["cli_sha256"] = installed.CliSha256,
                },
                ["frame_log"] = new JsonObject { ["sha256"] = Convert.ToHexStringLower(SHA256.HashData(Array.Empty<byte>())) },
                ["tool_call_frame_count"] = 0,
            }.ToJsonString());
        }

        public string RepositoryRoot { get; }

        public string InstallRoot => Path.Combine(_root, "install");

        public string ProofDirectory => Path.Combine(_root, "proof");

        public string ArtifactPath => Path.Combine(ProofDirectory, CompilePinArtifact.FileName);

        public string AdapterAgentPath => Path.Combine(InstallRoot, "node_modules", "@agentclientprotocol", "claude-agent-acp", "dist", "acp-agent.js");

        public CompileRequest Request(int boundMs) => new(
            RepositoryRoot, InstallRoot, "claude-code", "claude-sonnet-5", "max",
            [new ProviderRow("anthropic", ProviderAuth.Subscription, [new ProviderAccount("max", AccountHealth.Ready)])],
            CompileContract.HostHeader + "\n## source_text\n\n```text\nhello\n```\n", boundMs, ProofDirectory);

        private static void Write(string path, string text)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, text);
        }

        public void Dispose() => Directory.Delete(_root, recursive: true);
    }

    /// <summary>
    /// The child's stdio, scripted: every request the host writes is parsed and answered the way the
    /// PD-5 corpus answers it; the auth status arrives after <c>initialize</c>; extra frames can
    /// precede the prompt's answer; the answer can be withheld, or delivered only at disposal.
    /// </summary>
    private sealed class ScriptedEngine : ICompileEngine
    {
        private readonly PushedOutputReader _stdout = new();
        private readonly ScriptedInput _stdin;
        private readonly string _authKind;
        private readonly IReadOnlyList<string> _beforeAnswer;
        private readonly string _promptResult;
        private readonly bool _answerPrompt;
        private readonly bool _answerPromptOnDispose;
        private readonly bool _silent;
        private int _promptId = -1;

        private ScriptedEngine(string authKind, IReadOnlyList<string> beforeAnswer, string promptResult, bool answerPrompt, bool answerPromptOnDispose, bool silent)
        {
            _authKind = authKind;
            _beforeAnswer = beforeAnswer;
            _promptResult = promptResult;
            _answerPrompt = answerPrompt;
            _answerPromptOnDispose = answerPromptOnDispose;
            _silent = silent;
            _stdin = new ScriptedInput(OnRequest);
        }

        public static ScriptedEngine Answering(string authKind = "account", IReadOnlyList<string>? beforeAnswer = null, string? promptResult = null, bool answerPrompt = true, bool answerPromptOnDispose = false)
            => new(authKind, beforeAnswer ?? [], promptResult ?? CorpusPromptResult, answerPrompt, answerPromptOnDispose, silent: false);

        public static ScriptedEngine Silent() => new("account", [], CorpusPromptResult, false, false, silent: true);

        private const string CorpusPromptResult =
            // The answering model FIRST (opus 469, then haiku 15): the max-output rule is exercised, not "last entry".
            """{"stopReason":"end_turn","usage":{"inputTokens":2,"outputTokens":469,"cachedReadTokens":4870,"cachedWriteTokens":0,"totalTokens":5341},"_meta":{"quota":{"model_usage":[{"model":"claude-opus-5[1m]","token_count":{"outputTokens":469}},{"model":"claude-haiku-4-5-20251001","token_count":{"outputTokens":15}}]}}}""";

        public List<JsonObject> Sent => _stdin.Frames;

        public bool Disposed { get; private set; }

        public TextReader Output => _stdout;

        public TextWriter Input => _stdin;

        public int ProcessId => 4242;

        public bool HasExited => Disposed;

        private void OnRequest(JsonObject frame)
        {
            if (_silent)
            {
                return;
            }

            var method = frame["method"]?.GetValue<string>();
            var id = frame["id"]?.GetValue<int>();
            switch (method)
            {
                case "initialize":
                    _stdout.PushFrame("{\"jsonrpc\":\"2.0\",\"id\":" + id + ",\"result\":{\"protocolVersion\":1,\"authMethods\":[]}}");
                    _stdout.PushFrame("{\"jsonrpc\":\"2.0\",\"method\":\"_auth/status_update\",\"params\":{\"authStatus\":{\"kind\":\"" + _authKind + "\",\"label\":\"Claude Max\",\"account\":{\"plan\":\"max\"}}}}");
                    break;
                case "session/new":
                    _stdout.PushFrame("{\"jsonrpc\":\"2.0\",\"id\":" + id + ",\"result\":{\"sessionId\":\"s-c\",\"modes\":{\"currentModeId\":\"default\"}}}");
                    break;
                case "session/prompt":
                    _promptId = id!.Value;
                    foreach (var extra in _beforeAnswer)
                    {
                        _stdout.PushFrame(extra);
                    }

                    _stdout.PushFrame("""{"jsonrpc":"2.0","method":"session/update","params":{"sessionId":"s-c","update":{"sessionUpdate":"agent_message_chunk","content":{"type":"text","text":"{\"contract\":\"compile-output/1\","}}}}""");
                    _stdout.PushFrame("""{"jsonrpc":"2.0","method":"session/update","params":{"sessionId":"s-c","update":{"sessionUpdate":"agent_message_chunk","content":{"type":"text","text":"\"decorations\":[]}"}}}}""");
                    if (_answerPrompt && !_answerPromptOnDispose)
                    {
                        _stdout.PushFrame($"{{\"jsonrpc\":\"2.0\",\"id\":{id},\"result\":{_promptResult}}}");
                    }

                    break;
            }
        }

        public void Dispose()
        {
            if (Disposed)
            {
                return;
            }

            Disposed = true;
            if (_answerPromptOnDispose && _promptId >= 0)
            {
                // The answer lands while the engine is being torn down — after the deadline.
                _stdout.PushFrame($"{{\"jsonrpc\":\"2.0\",\"id\":{_promptId},\"result\":{_promptResult}}}");
            }

            _stdout.EndOfStream();
        }

        /// <summary>The child's stdin: buffers to the newline, then hands the parsed frame to the script.</summary>
        private sealed class ScriptedInput(Action<JsonObject> onRequest) : TextWriter
        {
            private readonly StringBuilder _line = new();
            private readonly Lock _gate = new();

            public List<JsonObject> Frames { get; } = [];

            public override Encoding Encoding => Encoding.UTF8;

            public override void Write(char value)
            {
                lock (_gate)
                {
                    if (value != '\n')
                    {
                        _line.Append(value);
                        return;
                    }

                    var text = _line.ToString().TrimEnd('\r');
                    _line.Clear();
                    if (text.Length == 0)
                    {
                        return;
                    }

                    var frame = JsonNode.Parse(text)!.AsObject();
                    Frames.Add(frame);
                    onRequest(frame);
                }
            }

            public override void Write(string? value)
            {
                foreach (var c in value ?? string.Empty)
                {
                    Write(c);
                }
            }
        }
    }

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
