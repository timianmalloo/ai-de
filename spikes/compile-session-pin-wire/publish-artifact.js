// publish-artifact.js <frames-dir> — writes the gate-1 artifact under ~/.aide/proof/ from a frames
// directory whose assertions ALREADY PASSED (Run-PinSpike.ps1 calls this after assert-spike.py exits 0;
// Security loop 2, condition C1). ADR-0036's path-resolution rule: the product reads the gate artifact
// at the MACHINE level, `~/.aide/proof/`, beside `~/.aide/providers.json` — the pin is about this
// machine's installed adapter, SDK and CLI, not about which workspace is open. The frame log rides
// beside it so the recount never depends on a checkout.
const fs = require("fs");
const path = require("path");
const framesDir = process.argv[2];
if (!framesDir || !fs.existsSync(path.join(framesDir, "summary.json"))) {
  console.error("usage: node publish-artifact.js <frames-dir with summary.json and recv.jsonl>");
  process.exit(2);
}
const summary = JSON.parse(fs.readFileSync(path.join(framesDir, "summary.json"), "utf8"));
if (summary.mode !== "full") {
  console.error(`refusing to publish a ${summary.mode} run as the gate artifact`);
  process.exit(3);
}
const proofDir = path.join(require("os").homedir(), ".aide", "proof");
fs.mkdirSync(proofDir, { recursive: true });
fs.writeFileSync(path.join(proofDir, "compile-pin-spike.json"), JSON.stringify(summary, null, 2));
fs.copyFileSync(path.join(framesDir, "recv.jsonl"), path.join(proofDir, "compile-pin-spike.frames.jsonl"));
console.log(`[publish-artifact] gate-1 artifact written: ${path.join(proofDir, "compile-pin-spike.json")} (+ compile-pin-spike.frames.jsonl)`);
