"""Leader pre-push check for non-join commits (Owner on report 10; DC-NEW-wl-leader2 control).

Runs the fast gates on a temporary DETACHED worktree at the commit being pushed, never on the working tree:
uncommitted files (e.g. an Owner's register edit not yet committed) once masked a red commit (4f62453b cited
Ruling 160 before its heading was committed). docs-graph validate fails on a problem (index drift alone is allowed:
joins regenerate the index). The repo copy tools/verify-ruling-citations.py is strict (Ruling 160).
Exit 0 = safe to push; 1 = do not push.
If the commit touches tools/, src/ or tests/, run the full gate_ratchet.py instead (the fast set does not cover them).
Usage: python -I leader_precommit.py <repo root> [<commit>]   (commit defaults to HEAD)
"""
import json
import pathlib
import subprocess
import sys
import tempfile

ROOT = pathlib.Path(sys.argv[1])
COMMIT = sys.argv[2] if len(sys.argv) > 2 else "HEAD"
PY = sys.executable


def git(*args, cwd=ROOT):
    return subprocess.run(["git", *args], cwd=cwd, capture_output=True, text=True, encoding="utf-8", errors="replace")


def run(tree, args):
    out = subprocess.run([PY, *args], cwd=tree, capture_output=True, text=True, encoding="utf-8", errors="replace")
    return out.returncode, (out.stdout or "") + (out.stderr or "")


changed = git("diff", "--name-only", COMMIT + "^", COMMIT).stdout.split()
if any(p.startswith(("tools/", "src/", "tests/")) for p in changed):
    print("leader-precommit: the commit touches tools/, src/ or tests/; run gate_ratchet.py instead")
    sys.exit(1)

tree = pathlib.Path(tempfile.mkdtemp(prefix="leader-precommit-")) / "tree"
added = git("worktree", "add", "--detach", str(tree), COMMIT)
if added.returncode != 0:
    print("leader-precommit: could not create the check tree: " + added.stderr.strip()[-200:])
    sys.exit(1)
failures = []
try:
    rc, text = run(tree, ["docs/ai-forward-pack/scripts/docs-graph.py", "validate"])
    try:
        problems = json.loads(text[text.index("{"):text.rindex("}") + 1]).get("problems", [])
    except ValueError:
        problems = None
    if problems is None:
        failures.append("docs-graph validate: output not readable")
    elif problems:
        failures.append("docs-graph validate: {0} problem(s): {1}".format(len(problems), problems[:3]))
    for gate in ("docs/ai-forward-pack/scripts/verify-no-conflict-markers.py",
                 "tools/verify-ruling-citations.py",
                 "tools/verify-no-account-identity.py"):
        rc, text = run(tree, [gate])
        if rc != 0:
            failures.append(gate + ": " + text.strip()[-300:])
finally:
    git("worktree", "remove", "--force", str(tree))

for line in failures:
    print("FAIL  " + line)
print("leader-precommit ({0}): {1}".format(COMMIT, "OK" if not failures else "{0} failure(s) - do not push".format(len(failures))))
sys.exit(1 if failures else 0)
