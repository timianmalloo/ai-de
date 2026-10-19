"""Leader pre-push check for non-join commits (Owner on report 10; DC-NEW-wl-leader2 control).

Runs the fast gates on the primary checkout's committed HEAD. docs-graph validate fails on a problem (index drift
alone is allowed: joins regenerate the index); verify-ruling-citations applies the Ruling 153 filter by counting only
findings that are not 'no heading in docs/notes/rulings.md'. Exit 0 = safe to push; 1 = do not push.
If the commit touches tools/ or src/, run the full gate_ratchet.py instead (the fast set does not cover them).
Usage: python -I leader_precommit.py <repo root>
"""
import json
import pathlib
import subprocess
import sys

ROOT = pathlib.Path(sys.argv[1])
PY = sys.executable


def run(args):
    out = subprocess.run([PY, *args], cwd=ROOT, capture_output=True, text=True, encoding="utf-8", errors="replace")
    return out.returncode, (out.stdout or "") + (out.stderr or "")


changed = subprocess.run(["git", "diff", "--name-only", "HEAD^", "HEAD"], cwd=ROOT, capture_output=True, text=True,
                         encoding="utf-8", errors="replace").stdout.split()
if any(p.startswith(("tools/", "src/", "tests/")) for p in changed):
    print("leader-precommit: the commit touches tools/, src/ or tests/; run gate_ratchet.py instead")
    sys.exit(1)

failures = []
rc, text = run(["docs/ai-forward-pack/scripts/docs-graph.py", "validate"])
try:
    problems = json.loads(text[text.index("{"):text.rindex("}") + 1]).get("problems", [])
except ValueError:
    problems = None
if problems is None:
    failures.append("docs-graph validate: output not readable")
elif problems:
    failures.append("docs-graph validate: {0} problem(s): {1}".format(len(problems), problems[:3]))

rc, text = run(["docs/ai-forward-pack/scripts/verify-no-conflict-markers.py"])
if rc != 0:
    failures.append("verify-no-conflict-markers: " + text.strip()[-200:])

rc, text = run(["tools/verify-ruling-citations.py"])
if rc != 0:
    failures.append("tools/verify-ruling-citations: " + text.strip()[-300:])

rc, text = run(["tools/verify-no-account-identity.py"])
if rc != 0:
    failures.append("verify-no-account-identity: " + text.strip()[-300:])

for line in failures:
    print("FAIL  " + line)
print("leader-precommit: {0}".format("OK" if not failures else "{0} failure(s) - do not push".format(len(failures))))
sys.exit(1 if failures else 0)
