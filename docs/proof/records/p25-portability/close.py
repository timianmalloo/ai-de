"""Record the bounded author checkpoint and verify only its owned audit surface."""
from pathlib import Path
import json
import os
import subprocess
import sys

root = Path(__file__).resolve().parents[4]
evidence = Path(__file__).parent
os.chdir(root)
os.environ.update(AGENT_SESSION="xh-p2-projection-b0d0", AGENT_NAME="copilot-p2-portability",
                  PYTHONIOENCODING="utf-8")
audit = [sys.executable, "docs/ai-forward-pack/scripts/audit-log.py"]
prompt = (
    "WRITE-CAPABLE C# portability repair within approved P2 COMPAT floor, <=45 tools, no agents. "
    "Own registered P2 tree C:\\Projects\\ai-de-feature-xh-p2-projection, "
    "HEAD 194863021d030faef6fb20a9f7bc385fb0c8d260, session xh-p2-projection-b0d0. "
    "Restore safe non-Windows ordinary RegistrationPublisher.Publish; retain Windows alias, junction, "
    "ownership, immutable full-byte conflict and monotonic projection guarantees. Real Linux baseline "
    "two existing ordinary tests RED, then GREEN plus symlink/ownership/fault-cleanup controls and "
    "cross-process directory replacement where feasible. Windows 79/79 focused and 252/254 floor "
    "with only known legacy N1/N2 REDs. Official SDK in own isolated storage allowed; source stays local. "
    "Do not edit live observers, App, hooks, dependencies or global configuration; no push or agents. "
    "Update owned design/ADR/phase plan/canonical proof plus audit. Commit with Copilot trailer, clean, "
    "released, ended. Independent COMPAT re-gate next; P0-P5 qualification remains pending."
)
# This is an explicit task summary, not a purported verbatim prompt capture.
record = {"shortname": "p2-compat-linux-portability", "session": "xh-p2-projection-b0d0",
          "skill": "implement", "kind": "skill", "prompt": "[Task summary; verbatim prompt remains in harness] " + prompt,
          "summary": "Restored public Linux x86_64 publication using no-follow directory descriptors. "
                     "Real Linux baseline 0/2 then 19/19; six focal mutant failures. Windows 79/79 and "
                     "252/254 with only original N1/N2. Source/API/SDK/binary/TRX pins in canonical proof. "
                     "Independent COMPAT pending; bootstrap certificate message is an isolation caveat.",
          "goal": "Restore safe ordinary Linux publication without weakening Windows publication.",
          "done_when": "Linux baseline and controls plus Windows floor, canonical proof, clean author commit.",
          "tier": "T2", "fan_out": 0, "outcome": "partial",
          "artifacts": ["docs/proof/cross-harness-coordination-proof-pack.md",
                        "docs/proof/records/p25-portability/final-manifest.json"]}
path = evidence / "audit-input.json"
path.write_text(json.dumps(record, indent=2), encoding="utf-8")


def checked(label, command):
    result = subprocess.run(command, cwd=root, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    (evidence / (label + ".log")).write_bytes(result.stdout)
    print(label, result.returncode, result.stdout.decode("utf-8", errors="replace")[-1600:])
    if result.returncode:
        raise SystemExit(result.returncode)


checked("audit-append", audit + ["append", "--from-json", str(path), "--main-budget", "41/45"])
checked("audit-render", audit + ["--root", "docs", "--project", "ai-de", "render"])
checked("audit-verify", audit + ["--root", "docs", "verify"])
checked("diff-check", ["git", "diff", "--check"])
for name in ["sdk.tar.gz", "sdk-source.json", "open.html", "rename.html", "link.html", "flock.html"]:
    path = root / ".p25-portability" / name
    if path.is_file():
        path.unlink()
scratch = root / ".p25-portability"
if scratch.exists():
    scratch.rmdir()
cleanup = {"windows_bootstrap_directory_exists": scratch.exists(),
           "linux_setup_retained": "/home/timmall/.aide-p25-b0d0",
           "reason": "Independent COMPAT reproduction with isolated SDK/build/package state; no worktree removed."}
(evidence / "cleanup.json").write_text(json.dumps(cleanup, indent=2), encoding="utf-8")
