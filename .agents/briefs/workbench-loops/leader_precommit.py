"""Leader pre-push check for non-join commits (Owner on report 10; DC-NEW-wl-leader2 control).

Runs the fast gates on a temporary DETACHED worktree at the commit being pushed, never on the working tree:
uncommitted files (e.g. an Owner's register edit not yet committed) once masked a red commit (4f62453b cited
Ruling 160 before its heading was committed). docs-graph validate fails on a problem (index drift alone is allowed:
joins regenerate the index). The repo copy tools/verify-ruling-citations.py is strict (Ruling 160).
Exit 0 = safe to push; 1 = refused (do not push); 2 = usage (the check did not run).
If the commit touches tools/, src/ or tests/, run the full gate_ratchet.py instead (the fast set does not cover them).
It also refuses a push whose range (<base>..<commit>, base defaulting to origin/main) carries a join commit
("chore(join): join-<name> - ...") with no "join-<name>-step8" audit entry in the pushed tree: a ruling commit
stacked on an unpushed join once published the join before its step-8 ratchet ran (T16, 657e9e51).
It refuses a range where any join commit ("chore(join): ..." or "join: ...") changes the rulings register:
conductor-join's step 7 swept an uncommitted Ruling 171 into the T18 join commit (61c9f4b0, never pushed).
Usage: python -I leader_precommit.py <repo root> [<commit> [<base>]]   (commit defaults to HEAD)
"""
import json
import pathlib
import re
import subprocess
import sys
import tempfile

USAGE = "usage: python -I leader_precommit.py <repo root> [<commit> [<base>]]   exit: 0 OK, 1 refused, 2 usage"
if len(sys.argv) < 2 or len(sys.argv) > 4 or any(a.startswith("-") for a in sys.argv[1:]):
    print(USAGE)
    sys.exit(2)
ROOT = pathlib.Path(sys.argv[1])
if not (ROOT / ".git").exists():
    print(f"leader-precommit: {ROOT} is not a git checkout\n{USAGE}")
    sys.exit(2)
COMMIT = sys.argv[2] if len(sys.argv) > 2 else "HEAD"
BASE = sys.argv[3] if len(sys.argv) > 3 else "origin/main"
PY = sys.executable
JOIN_SUBJECT = re.compile(r"^chore\(join\): (join-\S+) - ")


def git(*args, cwd=ROOT):
    return subprocess.run(["git", *args], cwd=cwd, capture_output=True, text=True, encoding="utf-8", errors="replace")


def run(tree, args):
    out = subprocess.run([PY, *args], cwd=tree, capture_output=True, text=True, encoding="utf-8", errors="replace")
    return out.returncode, (out.stdout or "") + (out.stderr or "")


REGISTER = "docs/notes/addendum-c-council-rulings.md"
JOIN_COMMIT = re.compile(r"^(chore\(join\)|join): ")
range_log = [line.split(" ", 1) for line in git("log", "--format=%H %s", f"{BASE}..{COMMIT}").stdout.splitlines()]
swept = [sha[:8] for sha, subject in range_log
         if JOIN_COMMIT.match(subject) and REGISTER in git("diff", "--name-only", sha + "^1", sha).stdout.split()]
if swept:
    print(f"leader-precommit: join commit(s) {', '.join(swept)} change {REGISTER}; a ruling is committed alone, never inside a join")
    print("  (conductor-join's step 7 stages every uncommitted file: commit or hold the register edit before the join)")
    sys.exit(1)

joins = [m.group(1) for _, subject in range_log if (m := JOIN_SUBJECT.match(subject))]
if joins:
    audit = git("show", f"{COMMIT}:docs/audit/audit-log.jsonl").stdout
    unratcheted = [j for j in joins if f'"shortname": "{j}-step8"' not in audit]
    if unratcheted:
        print("leader-precommit: the push would publish join(s) with no step-8 ratchet record: " + ", ".join(unratcheted))
        print("  run gate_ratchet.py and append '<join>-step8' before pushing, or push the other commit alone on origin/main")
        sys.exit(1)

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
