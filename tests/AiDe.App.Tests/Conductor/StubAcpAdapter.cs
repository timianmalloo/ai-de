using System.IO;
using System.Text.Json;

namespace AiDe.App.Tests.Conductor;

/// <summary>
/// A stand-in ACP adapter at the process boundary: a dependency-free node script installed at the
/// path <c>EngineCatalog.ResolveLaunch("claude-code", root)</c> resolves, so the product's own
/// composition root (<c>SessionDocumentSurface.Launch</c> → <c>GovernedRunHost.RunAsync</c> →
/// <c>AcpEngineProcess.Start</c>) spawns it as it would spawn the real adapter. Nothing in the
/// product is faked; the peer on the other end of stdio is.
/// </summary>
/// <remarks>
/// <para><b>Why a process and not a fake peer.</b> Ruling 95's drain runs inside the document's
/// own run loop (<c>RunOneAsync</c>'s completion), and the document takes no seam for the host —
/// one composition root, counted by <c>CompositionRootLedger</c>. The only way to hold a turn
/// <i>running</i> long enough to queue behind it, then let it complete, stop or fail on cue, is an
/// engine that waits for a cue. The cue is a file: <see cref="Release"/> creates it.</para>
///
/// <para><b>What the stub speaks</b>, shaped on the spike corpus
/// (<c>spikes/acp-subscription-lane/frames/read.jsonl</c>): <c>initialize</c> echoes the version
/// and is followed by <c>_auth/status_update</c> with an <c>account</c> kind (the observed-auth gate
/// fails closed without it); <c>session/new</c> answers a session id; <c>session/prompt</c> emits one
/// <c>agent_message_chunk</c>, waits for the release file, then answers <c>end_turn</c> with a
/// <c>usage</c> object. <see cref="Mode.FailAtInitialize"/> holds <c>initialize</c> until the release
/// and then echoes the wrong protocol version, which the client refuses — the run fails after a turn
/// was queued behind it.</para>
/// </remarks>
internal static class StubAcpAdapter
{
    /// <summary>How the stub behaves, written beside the script as <c>mode.json</c> before the spawn.</summary>
    /// <param name="FailAtInitialize">Hold <c>initialize</c> until the release, then echo protocolVersion 99 — the client refuses, the run fails.</param>
    /// <param name="InputTokens">The <c>usage.inputTokens</c> the prompt result reports.</param>
    /// <param name="OutputTokens">The <c>usage.outputTokens</c> the prompt result reports.</param>
    internal sealed record Mode(bool FailAtInitialize = false, int InputTokens = 4, int OutputTokens = 2);

    internal const string ReleaseFileName = ".aide-stub-release";

    /// <summary>Installs the stub under <paramref name="installRoot"/> and returns the entry module's path.</summary>
    internal static string Install(string installRoot, Mode? mode = null)
    {
        var dist = Path.Combine(installRoot, "node_modules", "@agentclientprotocol", "claude-agent-acp", "dist");
        Directory.CreateDirectory(dist);
        File.WriteAllText(Path.Combine(installRoot, "node_modules", "@agentclientprotocol", "claude-agent-acp", "package.json"), """{"name":"@agentclientprotocol/claude-agent-acp","version":"0.75.1"}""");
        File.WriteAllText(Path.Combine(dist, "mode.json"), JsonSerializer.Serialize(mode ?? new Mode()));
        var entry = Path.Combine(dist, "index.js");
        File.WriteAllText(entry, Script);
        return entry;
    }

    /// <summary>Lets every held request in <paramref name="repositoryRoot"/>'s stubs proceed.</summary>
    internal static void Release(string repositoryRoot) =>
        File.WriteAllText(Path.Combine(repositoryRoot, ReleaseFileName), "go");

    /// <summary>Every stub's trace in <paramref name="repositoryRoot"/> — what it received and sent, by pid — for a failure message.</summary>
    internal static string Traces(string repositoryRoot) =>
        string.Join("\n", Directory.EnumerateFiles(repositoryRoot, ".aide-stub-*.log").Select(f => Path.GetFileName(f) + ":\n" + File.ReadAllText(f)));

    /// <summary>Whether a stub has opened its session in <paramref name="repositoryRoot"/> (it writes a marker on <c>session/new</c>).</summary>
    internal static bool SessionOpened(string repositoryRoot) =>
        File.Exists(Path.Combine(repositoryRoot, ".aide-stub-session"));

    private const string Script = """
        'use strict';
        const fs = require('fs');
        const path = require('path');
        const readline = require('readline');

        const mode = JSON.parse(fs.readFileSync(path.join(__dirname, 'mode.json'), 'utf8'));
        const releaseFile = path.join(process.cwd(), '.aide-stub-release');
        const sessionMarker = path.join(process.cwd(), '.aide-stub-session');
        let sessions = 0;

        const log = path.join(process.cwd(), '.aide-stub-' + process.pid + '.log');
        function trace(direction, text) {
          try { fs.appendFileSync(log, new Date().toISOString() + ' ' + direction + ' ' + text + '\n'); } catch {}
        }

        function send(frame) {
          const text = JSON.stringify(frame);
          trace('>', text);
          process.stdout.write(text + '\n');
        }

        function reply(id, result) {
          send({ jsonrpc: '2.0', id, result });
        }

        function whenReleased(then) {
          if (fs.existsSync(releaseFile)) { then(); return; }
          setTimeout(() => whenReleased(then), 50);
        }

        const rl = readline.createInterface({ input: process.stdin, terminal: false });
        rl.on('line', (line) => {
          if (!line.trim()) return;
          trace('<', line);
          let frame;
          try { frame = JSON.parse(line); } catch { return; }
          const { id, method, params } = frame;
          if (method === undefined) return;

          if (method === 'initialize') {
            const answer = () => {
              reply(id, {
                protocolVersion: mode.FailAtInitialize ? 99 : params.protocolVersion,
                agentCapabilities: {},
                agentInfo: { name: 'aide-stub-acp', version: '0.0.0' },
              });
              send({ jsonrpc: '2.0', method: '_auth/status_update',
                     params: { authStatus: { kind: 'account', label: 'Stub Max', account: { plan: 'max' } } } });
            };
            if (mode.FailAtInitialize) whenReleased(answer); else answer();
            return;
          }

          if (method === 'session/new') {
            sessions += 1;
            fs.writeFileSync(sessionMarker, String(process.pid));
            reply(id, { sessionId: 'stub-' + process.pid + '-' + sessions, modes: { currentModeId: 'default', availableModes: [] } });
            return;
          }

          if (method === 'session/prompt') {
            send({ jsonrpc: '2.0', method: 'session/update',
                   params: { sessionId: params.sessionId, update: { sessionUpdate: 'agent_message_chunk', content: { type: 'text', text: 'ok' } } } });
            whenReleased(() => {
              reply(id, { stopReason: 'end_turn',
                          usage: { inputTokens: mode.InputTokens, outputTokens: mode.OutputTokens, cachedReadTokens: 0, cachedWriteTokens: 0,
                                   totalTokens: mode.InputTokens + mode.OutputTokens } });
              send({ jsonrpc: '2.0', method: 'session/update',
                     params: { sessionId: params.sessionId, update: { sessionUpdate: 'usage_update', used: mode.InputTokens + mode.OutputTokens } } });
            });
            return;
          }

          if (id !== undefined) {
            send({ jsonrpc: '2.0', id, error: { code: -32601, message: 'stub: method not found: ' + method } });
          }
        });

        rl.on('close', () => process.exit(0));
        """;
}
