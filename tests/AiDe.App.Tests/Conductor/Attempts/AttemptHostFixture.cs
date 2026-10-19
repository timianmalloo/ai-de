using System.IO;
using System.Text.Json;
using AiDe.App.Conductor;
using AiDe.Core.AgentPlane;
using AiDe.Core.Ipc;
using AiDe.Core.Store;
using AiDe.Core.Store.Records;

namespace AiDe.App.Tests.Conductor.Attempts;

/// <summary>How the attempt stub answers <c>session/prompt</c> (written beside the script as <c>mode.json</c>).</summary>
public enum PromptBehaviour
{
    /// <summary>One chunk, then <c>end_turn</c> once the release file exists.</summary>
    EndTurnOnRelease,

    /// <summary>One chunk, then holds; answers <c>stopReason: cancelled</c> to the prompt when <c>session/cancel</c> arrives.</summary>
    HonourCancel,

    /// <summary>One chunk, then holds forever; <c>session/cancel</c> is traced and ignored.</summary>
    IgnoreCancel,

    /// <summary>As <see cref="HonourCancel"/>, but on <c>session/cancel</c> first asks a permission (as one already in flight would) and traces the answer.</summary>
    PermissionThenCancel,

    /// <summary>One chunk carrying <c>FrameRedaction.TestSentinel</c>, then <c>end_turn</c>.</summary>
    Sentinel,
}

/// <summary>
/// The attempt tests' engine: a dependency-free node script installed where
/// <c>EngineCatalog.ResolveLaunch("claude-code", root)</c> resolves, so <see cref="GovernedRunHost.RunAsync"/>
/// spawns it over stdio exactly as it spawns the real adapter. It extends <c>StubAcpAdapter</c>'s protocol
/// (same handshake, same release file, same trace) with the Stop behaviours the attempt host needs.
/// </summary>
/// <remarks>
/// simplify: a second stub script beside <c>StubAcpAdapter</c> rather than modes added to it, because that file
/// is outside this track's owned paths. Ceiling: two stub scripts. Upgrade trigger: a third ACP test engine, or
/// the next track that owns <c>StubAcpAdapter.cs</c> — then fold these modes into it.
/// </remarks>
internal static class AttemptStubEngine
{
    internal const string ReleaseFileName = ".aide-stub-release";

    internal static void Install(string installRoot, PromptBehaviour behaviour, string sentinel = "")
    {
        var package = Path.Combine(installRoot, "node_modules", "@agentclientprotocol", "claude-agent-acp");
        var dist = Path.Combine(package, "dist");
        Directory.CreateDirectory(dist);
        File.WriteAllText(Path.Combine(package, "package.json"), """{"name":"@agentclientprotocol/claude-agent-acp","version":"0.75.1"}""");
        File.WriteAllText(Path.Combine(dist, "mode.json"), JsonSerializer.Serialize(new { behaviour = behaviour.ToString(), sentinel }));
        File.WriteAllText(Path.Combine(dist, "index.js"), Script);
    }

    private const string Script = """
        'use strict';
        const fs = require('fs');
        const path = require('path');
        const readline = require('readline');

        const mode = JSON.parse(fs.readFileSync(path.join(__dirname, 'mode.json'), 'utf8'));
        const releaseFile = path.join(process.cwd(), '.aide-stub-release');
        const log = path.join(process.cwd(), '.aide-stub-' + process.pid + '.log');
        let sessions = 0;
        let nextId = 1000;
        let heldPrompt = null;
        const awaiting = {};

        function trace(direction, text) { try { fs.appendFileSync(log, new Date().toISOString() + ' ' + direction + ' ' + text + '\n'); } catch {} }
        function send(frame) { const text = JSON.stringify(frame); trace('>', text); process.stdout.write(text + '\n'); }
        function reply(id, result) { send({ jsonrpc: '2.0', id, result }); }
        function chunk(sessionId, text) {
          send({ jsonrpc: '2.0', method: 'session/update', params: { sessionId, update: { sessionUpdate: 'agent_message_chunk', content: { type: 'text', text } } } });
        }
        function whenReleased(then) { if (fs.existsSync(releaseFile)) { then(); return; } setTimeout(() => whenReleased(then), 50); }
        const usage = { inputTokens: 4, outputTokens: 2, cachedReadTokens: 0, cachedWriteTokens: 0, totalTokens: 6 };

        const rl = readline.createInterface({ input: process.stdin, terminal: false });
        rl.on('line', (line) => {
          if (!line.trim()) return;
          trace('<', line);
          let frame;
          try { frame = JSON.parse(line); } catch { return; }
          const { id, method, params } = frame;

          if (method === undefined) {
            const then = awaiting[id];
            if (then) { delete awaiting[id]; then(frame); }
            return;
          }

          if (method === 'initialize') {
            reply(id, { protocolVersion: params.protocolVersion, agentCapabilities: {}, agentInfo: { name: 'aide-attempt-stub', version: '0.0.0' } });
            send({ jsonrpc: '2.0', method: '_auth/status_update', params: { authStatus: { kind: 'account', label: 'Stub Max', account: { plan: 'max' } } } });
            return;
          }

          if (method === 'session/new') {
            sessions += 1;
            reply(id, { sessionId: 'stub-' + process.pid + '-' + sessions, modes: { currentModeId: 'default', availableModes: [] } });
            return;
          }

          if (method === 'session/prompt') {
            if (mode.behaviour === 'Sentinel') {
              chunk(params.sessionId, 'token=' + mode.sentinel + ' end');
              reply(id, { stopReason: 'end_turn', usage });
              return;
            }
            chunk(params.sessionId, 'ok');
            if (mode.behaviour === 'EndTurnOnRelease') {
              whenReleased(() => reply(id, { stopReason: 'end_turn', usage }));
              return;
            }
            heldPrompt = { id, sessionId: params.sessionId };
            return;
          }

          if (method === 'session/cancel') {
            if (!heldPrompt || mode.behaviour === 'IgnoreCancel') return;
            const held = heldPrompt;
            heldPrompt = null;
            if (mode.behaviour === 'PermissionThenCancel') {
              const askId = nextId++;
              awaiting[askId] = () => reply(held.id, { stopReason: 'cancelled' });
              send({ jsonrpc: '2.0', id: askId, method: 'session/request_permission', params: {
                sessionId: held.sessionId,
                toolCall: { toolCallId: 'tc-after-stop', title: 'Write file', kind: 'edit' },
                options: [ { optionId: 'allow', name: 'Allow', kind: 'allow_once' }, { optionId: 'reject', name: 'Reject', kind: 'reject_once' } ] } });
              return;
            }
            reply(held.id, { stopReason: 'cancelled' });
            return;
          }

          if (id !== undefined) send({ jsonrpc: '2.0', id, error: { code: -32601, message: 'stub: method not found: ' + method } });
        });

        rl.on('close', () => process.exit(0));
        """;
}

/// <summary>A repository with the attempt stub installed, a real store seeded with a revision, and a workspace root.</summary>
internal sealed class AttemptHostFixture : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("aide-attempt-").FullName;

    public AttemptHostFixture(PromptBehaviour behaviour = PromptBehaviour.EndTurnOnRelease, bool released = true, string sentinel = "")
    {
        Repository = Path.Combine(_root, "repo");
        Directory.CreateDirectory(Repository);
        AttemptStubEngine.Install(Install, behaviour, sentinel);
        if (released)
        {
            Release();
        }

        Store = WorkspaceStore.Open(DatabasePath);
        using var writer = Store.BeginWrite();
        writer.AppendJourney(new JourneyRow("j-1", "work", "Attempts"));
        writer.AppendNotebook(new NotebookRow("nb-1", "j-1", Legacy: false));
        writer.AppendNotebookRevision(new NotebookRevisionRow("nb-1", 1, "say ok"));
        writer.Commit();
    }

    public string Repository { get; }

    public string Install => Path.Combine(_root, "install");

    /// <summary>The workspace root: the lock, the run logs and the rolling log live under its <c>.aide</c>.</summary>
    public string WorkspaceRoot => _root;

    public string LockFile => Path.Combine(_root, ".aide", "attempt-host.lock");

    public string TelemetryLog => Path.Combine(_root, ".aide", "attempt-telemetry.jsonl");

    private string DatabasePath => Path.Combine(_root, "workspace.db");

    public WorkspaceStore Store { get; private set; }

    public void Release() => File.WriteAllText(Path.Combine(Repository, AttemptStubEngine.ReleaseFileName), "go");

    /// <summary>Lines every stub received (<c>&lt;</c>) or sent (<c>&gt;</c>), across processes.</summary>
    public IEnumerable<string> TraceLines(string direction) =>
        Traces().SelectMany(lines => lines).Where(l => l.Contains($" {direction} ", StringComparison.Ordinal));

    /// <summary><c>session/prompt</c> frames any stub received — the consequential write, counted at the far end.</summary>
    public int PromptsReceived => TraceLines("<").Count(l => l.Contains("\"method\":\"session/prompt\"", StringComparison.Ordinal));

    /// <summary><c>session/cancel</c> frames any stub received.</summary>
    public int CancelsReceived => TraceLines("<").Count(l => l.Contains("\"method\":\"session/cancel\"", StringComparison.Ordinal));

    /// <summary>Stub processes that received any frame at all.</summary>
    public int EnginesStarted => Traces().Count();

    /// <summary>Waits until a stub received <paramref name="prompts"/> prompts.</summary>
    public async Task PromptsArrivedAsync(int prompts = 1)
    {
        var until = DateTime.UtcNow + TimeSpan.FromSeconds(60);
        while (PromptsReceived < prompts)
        {
            Assert.True(DateTime.UtcNow < until, $"the stub never received prompt {prompts}");
            await Task.Delay(50);
        }
    }

    public AttemptHostOptions Options => new(WorkspaceRoot);

    public Task<AttemptHost> OpenAsync(AttemptHostOptions? options = null, IAttemptLedger? ledger = null) =>
        AttemptHost.OpenAsync(ledger ?? new AiDe.Core.Dispatch.BoundaryDispatcher(Store), LockFile, CancellationToken.None, options ?? Options);

    public AttemptDispatch Dispatch(string envelopeId = "env-1", string? currentProjection = "proj-sha", string? queuedAfter = null) => new(
        new GovernedRunRequest(
            RepositoryRoot: Repository,
            DataDirectory: Path.Combine(_root, "data"),
            AdapterInstallRoot: Install,
            EngineId: "claude-code",
            Model: "claude-sonnet-5",
            AccountLabel: "max",
            TaskClass: "attempt-test",
            Goal: null,
            Lease: null,
            Prompt: "say ok",
            ProofPackArtifacts: [],
            Providers: [new ProviderRow("anthropic", ProviderAuth.Subscription, [new ProviderAccount("max", AccountHealth.Ready)])]),
        "ws-1", Store.CoreEpoch, envelopeId, "proj-sha", "nb-1", 1, "s-1", 1, () => currentProjection,
        QueuedAfterDispatchKey: queuedAfter);

    /// <summary>The rolling log's lines whose <c>metric</c> is <paramref name="metric"/>.</summary>
    public IReadOnlyList<JsonElement> Telemetry(string metric)
    {
        if (!File.Exists(TelemetryLog))
        {
            return [];
        }

        using var stream = new FileStream(TelemetryLog, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        return [.. reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => JsonDocument.Parse(l).RootElement.Clone())
            .Where(e => e.GetProperty("metric").GetString() == metric)];
    }

    public void ReopenStore()
    {
        Store.Dispose();
        Store = WorkspaceStore.Open(DatabasePath);
    }

    private IEnumerable<string[]> Traces() =>
        Directory.EnumerateFiles(Repository, ".aide-stub-*.log").Select(f =>
        {
            using var stream = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        });

    public void Dispose()
    {
        Store.Dispose();
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { /* reaped later */ } catch (UnauthorizedAccessException) { /* reaped later */ }
    }
}
