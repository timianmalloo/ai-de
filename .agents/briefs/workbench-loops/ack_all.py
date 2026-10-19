"""Ack every unacked message in one session's inbox through coord-mail.py (the single writer)."""
import json
import pathlib
import subprocess
import sys

repo, session = pathlib.Path(sys.argv[1]), sys.argv[2]
inbox = repo / ".agents" / "mail" / f"{session}.jsonl"
rows = [json.loads(line) for line in inbox.read_text(encoding="utf-8").splitlines() if line.strip()]
acked = {r.get("ref") for r in rows if r.get("kind") == "ack"}
pending = [r["id"] for r in rows if r.get("kind") != "ack" and r.get("id") and r["id"] not in acked]
print("unacked:", len(pending))
failed = 0
for mid in pending:
    done = subprocess.run([sys.executable, str(repo / "docs/ai-forward-pack/scripts/coord-mail.py"), "ack", "--session", session, mid],
                          cwd=repo, capture_output=True, text=True)
    if done.returncode != 0:
        failed += 1
        print("ack failed", mid, done.stderr.strip()[:200])
print("acked:", len(pending) - failed, "failed:", failed)
sys.exit(1 if failed else 0)
