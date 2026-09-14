// Engine handshake probe — modelled on spikes/acp-subscription-lane/probe-read.js.
// Spawns an ACP agent, sends initialize -> session/new (-> session/prompt when --prompt),
// records every received frame to <out>/frames.jsonl and every sent frame to
// <out>/frames.sent.jsonl, stderr to <out>/stderr.log, and a summary to stdout.
// It never signs in: a session/request_permission is answered with the reject option, and
// any other inbound request gets an empty result.
//
// usage: node probe-handshake.js --out <dir> --cmd <exe> [--arg <a>]... [--env K=V]... [--cwd <dir>]
//                                [--shell] [--prompt "<text>"] [--timeout-ms 30000]
const { spawn } = require("child_process");
const fs = require("fs");
const path = require("path");

const argv = process.argv.slice(2);
const opt = { args: [], env: {}, timeoutMs: 30000, cwd: process.cwd(), shell: false, prompt: null };
for (let i = 0; i < argv.length; i++) {
  const a = argv[i];
  if (a === "--out") opt.out = argv[++i];
  else if (a === "--cmd") opt.cmd = argv[++i];
  else if (a === "--arg") opt.args.push(argv[++i]);
  else if (a === "--env") { const kv = argv[++i]; const j = kv.indexOf("="); opt.env[kv.slice(0, j)] = kv.slice(j + 1); }
  else if (a === "--cwd") opt.cwd = argv[++i];
  else if (a === "--shell") opt.shell = true;
  else if (a === "--prompt") opt.prompt = argv[++i];
  else if (a === "--timeout-ms") opt.timeoutMs = Number(argv[++i]);
  else { console.error("unknown arg " + a); process.exit(2); }
}
if (!opt.out || !opt.cmd) { console.error("need --out and --cmd"); process.exit(2); }

fs.mkdirSync(opt.out, { recursive: true });
const RECV = path.join(opt.out, "frames.jsonl");
const SENT = path.join(opt.out, "frames.sent.jsonl");
const ERR = path.join(opt.out, "stderr.log");
fs.writeFileSync(RECV, ""); fs.writeFileSync(SENT, ""); fs.writeFileSync(ERR, "");

const started = Date.now();
const stamp = () => String(Date.now() - started).padStart(6, " ") + "ms ";
const log = s => console.log(stamp() + s);
log("SPAWN " + JSON.stringify({ cmd: opt.cmd, args: opt.args, cwd: opt.cwd, shell: opt.shell, env: Object.keys(opt.env) }));

const p = spawn(opt.cmd, opt.args, {
  stdio: ["pipe", "pipe", "pipe"], cwd: opt.cwd, shell: opt.shell,
  env: { ...process.env, ...opt.env },
});
let exited = false;
p.on("error", e => { log("SPAWN-ERROR " + e.message); finish(3); });
p.on("exit", (code, sig) => { exited = true; log("CHILD-EXIT code=" + code + " signal=" + sig); });

const send = o => { const s = JSON.stringify(o); fs.appendFileSync(SENT, s + "\n"); if (!exited) p.stdin.write(s + "\n"); };
let buf = "", sid = null, kinds = new Set(), nRecv = 0;
function finish(code) { try { p.kill(); } catch {} setTimeout(() => process.exit(code), 200); }

p.stdout.on("data", d => {
  buf += d.toString(); let i;
  while ((i = buf.indexOf("\n")) >= 0) {
    const line = buf.slice(0, i); buf = buf.slice(i + 1);
    if (!line.trim()) continue;
    fs.appendFileSync(RECV, line + "\n"); nRecv++;
    let m; try { m = JSON.parse(line); } catch { log("UNPARSED " + line.slice(0, 300)); continue; }
    if (m.method === "session/update") {
      const u = m.params && m.params.update, k = u && u.sessionUpdate;
      if (k && !kinds.has(k)) { kinds.add(k); log("FIRST[" + k + "] " + JSON.stringify(u).slice(0, 400)); }
    } else if (m.method === "session/request_permission") {
      log("PERMREQ " + JSON.stringify(m.params).slice(0, 600));
      const o = m.params.options || [];
      const rej = o.find(x => x.kind && x.kind.startsWith("reject")) || o[0];
      send({ jsonrpc: "2.0", id: m.id, result: { outcome: rej ? { outcome: "selected", optionId: rej.optionId } : { outcome: "cancelled" } } });
    } else if (m.method) {
      log("INBOUND " + m.method + " " + JSON.stringify(m.params).slice(0, 400));
      if (m.id !== undefined) send({ jsonrpc: "2.0", id: m.id, result: {} });
    } else if (m.id === 1) {
      log("INITIALIZE " + (m.result ? "result " : "error ") + JSON.stringify(m.result || m.error).slice(0, 1200));
      send({ jsonrpc: "2.0", id: 2, method: "session/new", params: { cwd: opt.cwd.split(String.fromCharCode(92)).join("/"), mcpServers: [] } });
    } else if (m.id === 2) {
      log("SESSION/NEW " + (m.result ? "result " : "error ") + JSON.stringify(m.result || m.error).slice(0, 1200));
      if (!m.result || !opt.prompt) { log("DONE kinds=" + [...kinds].join(",") + " frames=" + nRecv); finish(0); return; }
      sid = m.result.sessionId;
      send({ jsonrpc: "2.0", id: 3, method: "session/prompt", params: { sessionId: sid, prompt: [{ type: "text", text: opt.prompt }] } });
    } else if (m.id === 3) {
      log("PROMPT " + (m.result ? "result " : "error ") + JSON.stringify(m.result || m.error).slice(0, 600));
      log("DONE kinds=" + [...kinds].join(",") + " frames=" + nRecv); finish(0);
    }
  }
});
p.stderr.on("data", d => { fs.appendFileSync(ERR, d.toString()); log("STDERR " + d.toString().slice(0, 300).split(String.fromCharCode(10)).join(" | ")); });

send({ jsonrpc: "2.0", id: 1, method: "initialize", params: { protocolVersion: 1, clientCapabilities: { fs: { readTextFile: true, writeTextFile: true }, terminal: true } } });
setTimeout(() => { log("TIMEOUT after " + opt.timeoutMs + "ms; kinds=" + [...kinds].join(",") + " frames=" + nRecv); finish(4); }, opt.timeoutMs);
