const { spawn } = require("child_process");
const fs = require("fs");
const RECV_CAPTURE = __dirname + "/frames/write.jsonl";
const SENT_CAPTURE = __dirname + "/frames/write.sent.jsonl";
fs.mkdirSync(__dirname + "/frames", { recursive: true });
fs.writeFileSync(RECV_CAPTURE, "");
fs.writeFileSync(SENT_CAPTURE, "");
const p = spawn(process.execPath, [__dirname + "/node_modules/@agentclientprotocol/claude-agent-acp/dist/index.js"], { stdio: ["pipe","pipe","pipe"], cwd: __dirname });
const send = o => { const s = JSON.stringify(o); fs.appendFileSync(SENT_CAPTURE, s + "\n"); p.stdin.write(s + "\n"); };
let buf = "", sid = null, seen = new Set();
p.stdout.on("data", d => {
  buf += d.toString(); let i;
  while ((i = buf.indexOf("\n")) >= 0) {
    const line = buf.slice(0, i); buf = buf.slice(i + 1);
    if (!line.trim()) continue;
    fs.appendFileSync(RECV_CAPTURE, line + "\n");
    let m; try { m = JSON.parse(line); } catch { console.log("UNPARSED:" + line.slice(0,200)); continue; }
    if (m.method === "session/update") {
      const u = m.params.update, k = u.sessionUpdate;
      if (!seen.has(k)) { seen.add(k); console.log("FIRST[" + k + "] " + JSON.stringify(u).slice(0,700)); }
    } else if (m.method === "session/request_permission") {
      console.log("PERMREQ " + JSON.stringify(m.params).slice(0,900));
      send({jsonrpc:"2.0",id:m.id,result:{outcome:{outcome:"selected",optionId:(m.params.options.find(o=>o.kind&&o.kind.startsWith("reject"))||m.params.options[0]).optionId}}});
    } else if (m.method) {
      console.log("OTHER-INBOUND " + m.method + " " + JSON.stringify(m.params).slice(0,300));
      if (m.id !== undefined) send({jsonrpc:"2.0",id:m.id,result:{}});
    } else if (m.id === 1) {
      send({jsonrpc:"2.0",id:2,method:"session/new",params:{cwd:"C:/Users/malla/AppData/Local/Temp/claude/C--projects-ai-de/18fe7a5a-c1b6-434e-8033-3f0c4e841f24/scratchpad/probe",mcpServers:[]}});
    } else if (m.id === 2) {
      console.log("SESSION/NEW RESULT " + JSON.stringify(m.result || m.error).slice(0,600));
      if (!m.result) { p.kill(); process.exit(0); }
      sid = m.result.sessionId;
      send({jsonrpc:"2.0",id:3,method:"session/prompt",params:{sessionId:sid,prompt:[{type:"text",text:"Create a file named hello.txt containing the word hi in the current directory. Do nothing else."}]}});
    } else if (m.id === 3) {
      console.log("PROMPT RESULT " + JSON.stringify(m.result || m.error).slice(0,400));
      console.log("ALL UPDATE KINDS: " + [...seen].join(","));
      p.kill(); process.exit(0);
    }
  }
});
p.stderr.on("data", d => console.log("STDERR " + d.toString().slice(0,300)));
send({jsonrpc:"2.0",id:1,method:"initialize",params:{protocolVersion:1,clientCapabilities:{fs:{readTextFile:true,writeTextFile:true},terminal:true}}});
setTimeout(()=>{console.log("### timeout; kinds=" + [...seen].join(","));p.kill();process.exit(0);}, 120000);
