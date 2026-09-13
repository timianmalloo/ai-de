// probe-thought.js — capture a real `agent_thought_chunk` frame (Ruling 82 condition 1; CV-5.3).
//
// One prompt that provokes extended thinking (a small reasoning puzzle), on a session pinned
// read-only exactly as docs/proof/compile-pin-spike.json records (`tools: []` + the disallowed
// list), with `thinking.display: "summarized"` requested through the same `_meta.claudeCode.options`
// — the adapter's own comment (acp-agent.js, case "thinking_delta") says recent models default the
// display to "omitted", which streams signature-only blocks whose text is empty and which the
// adapter never forwards. Without the display request there is no thought text to capture at all.
//
// Redaction, applied to every captured line in both directions before it reaches disk:
//   1. email-shaped substrings → `redacted@example.invalid` (copied from
//      spikes/compile-session-pin-wire/run-spike.js `redact`);
//   2. the operator's home-directory prefix, both spellings → `C:\<REDACTED_HOME>` /
//      `C:/<REDACTED_HOME>` (frames/PROVENANCE.md's second rule, derived from os.homedir()).
// The values are never named here (naming what you removed is how a redaction undoes itself).
//
// Usage: node probe-thought.js [--prompt <n>]   (ACP_ADAPTER overrides the adapter entry path)
const { spawn } = require("child_process");
const fs = require("fs");
const os = require("os");
const path = require("path");

const ADAPTER = process.env.ACP_ADAPTER
  || path.join(__dirname, "node_modules/@agentclientprotocol/claude-agent-acp/dist/index.js");
const RECV_CAPTURE = path.join(__dirname, "frames/thought.jsonl");
const SENT_CAPTURE = path.join(__dirname, "frames/thought.sent.jsonl");

const PROMPTS = [
  "Three boxes are labelled Apples, Oranges and Mixed, and every label is wrong. You may draw one fruit from one box without looking inside. Which box do you draw from, and how do you then relabel all three? Think it through first, then answer in three sentences. Do not use any tools.",
  "A bat and a ball cost 1.10 together and the bat costs 1.00 more than the ball. How much is the ball? Reason carefully before answering; give the answer in one sentence. Do not use any tools.",
];
const promptIndex = Math.max(0, Math.min(PROMPTS.length - 1, Number(process.argv[process.argv.indexOf("--prompt") + 1]) || 0));

const EMAIL_PATTERN = /[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}/g;
const home = os.homedir();                                   // e.g. C:\Users\<name>
const homeBackslashEscaped = home.replace(/\\/g, "\\\\");    // as it appears inside a JSON string value
const homeForward = home.replace(/\\/g, "/");
function redact(line) {
  return line
    .replace(EMAIL_PATTERN, "redacted@example.invalid")
    .split(homeBackslashEscaped).join("C:\\\\<REDACTED_HOME>")
    .split(homeForward).join("C:/<REDACTED_HOME>")
    .split(home).join("C:\\<REDACTED_HOME>");
}

fs.mkdirSync(path.join(__dirname, "frames"), { recursive: true });
fs.writeFileSync(RECV_CAPTURE, "");
fs.writeFileSync(SENT_CAPTURE, "");

const p = spawn(process.execPath, [ADAPTER], { stdio: ["pipe", "pipe", "pipe"], cwd: __dirname });
const send = o => { const s = JSON.stringify(o); fs.appendFileSync(SENT_CAPTURE, redact(s) + "\n"); p.stdin.write(s + "\n"); };
let buf = "", sid = null, seen = new Set(), thoughts = 0, thoughtChars = 0;

const readOnly = {
  tools: [],
  disallowedTools: ["Write", "Edit", "MultiEdit", "NotebookEdit", "EnterWorktree", "ExitWorktree", "CronCreate", "CronDelete",
    "Bash", "PowerShell", "REPL", "Monitor", "Tmux", "LSP", "self_hosted_runner_spawn_local", "Agent", "Task", "Workflow",
    "RemoteTrigger", "self_hosted_runner_requeue_session", "Artifact", "Projects", "SendFile", "SendUserFile", "ClaudeDesign",
    "Snip", "WebBrowser", "SubscribePR", "DesignSync", "ConnectGitHub"],
  thinking: { type: "adaptive", display: "summarized" },
};

p.stdout.on("data", d => {
  buf += d.toString(); let i;
  while ((i = buf.indexOf("\n")) >= 0) {
    const line = buf.slice(0, i); buf = buf.slice(i + 1);
    if (!line.trim()) continue;
    fs.appendFileSync(RECV_CAPTURE, redact(line) + "\n");
    let m; try { m = JSON.parse(line); } catch { console.log("UNPARSED:" + line.slice(0, 200)); continue; }
    if (m.method === "session/update") {
      const u = m.params.update, k = u.sessionUpdate;
      if (k === "agent_thought_chunk") { thoughts++; thoughtChars += (u.content && u.content.text ? u.content.text.length : 0); }
      if (!seen.has(k)) { seen.add(k); console.log("FIRST[" + k + "] " + redact(JSON.stringify(u)).slice(0, 700)); }
    } else if (m.method === "session/request_permission") {
      console.log("PERMREQ " + redact(JSON.stringify(m.params)).slice(0, 900));
      send({ jsonrpc: "2.0", id: m.id, result: { outcome: { outcome: "selected", optionId: (m.params.options.find(o => o.kind && o.kind.startsWith("reject")) || m.params.options[0]).optionId } } });
    } else if (m.method) {
      console.log("OTHER-INBOUND " + m.method + " " + redact(JSON.stringify(m.params)).slice(0, 300));
      if (m.id !== undefined) send({ jsonrpc: "2.0", id: m.id, result: {} });
    } else if (m.id === 1) {
      send({ jsonrpc: "2.0", id: 2, method: "session/new", params: { cwd: "C:/projects/ai-de", mcpServers: [], _meta: { claudeCode: { options: readOnly } } } });
    } else if (m.id === 2) {
      console.log("SESSION/NEW RESULT " + redact(JSON.stringify(m.result || m.error)).slice(0, 600));
      if (!m.result) { p.kill(); process.exit(0); }
      sid = m.result.sessionId;
      send({ jsonrpc: "2.0", id: 3, method: "session/prompt", params: { sessionId: sid, prompt: [{ type: "text", text: PROMPTS[promptIndex] }] } });
    } else if (m.id === 3) {
      console.log("PROMPT RESULT " + redact(JSON.stringify(m.result || m.error)).slice(0, 400));
      console.log("ALL UPDATE KINDS: " + [...seen].join(","));
      console.log("THOUGHT CHUNKS: " + thoughts + " (" + thoughtChars + " chars)");
      p.kill(); process.exit(0);
    }
  }
});
p.stderr.on("data", d => console.log("STDERR " + redact(d.toString()).slice(0, 300)));
send({ jsonrpc: "2.0", id: 1, method: "initialize", params: { protocolVersion: 1, clientCapabilities: { fs: { readTextFile: false, writeTextFile: false }, terminal: false } } });
setTimeout(() => { console.log("### timeout; kinds=" + [...seen].join(",") + "; thoughts=" + thoughts); p.kill(); process.exit(0); }, 180000);
