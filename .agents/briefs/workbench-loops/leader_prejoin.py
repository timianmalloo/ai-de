"""Leader pre-join check: refuse to start conductor-join on a checkout carrying foreign uncommitted files.

conductor-join.py's step 7 stages every uncommitted file, so a Leader edit left in the working tree (an Owner's
register entry, obligations.md) lands inside the join commit: Ruling 171 was swept into the T18 join (61c9f4b0,
DC-NEW-wl-leader2 12th instance). This runs BEFORE the join: any modified or untracked path outside the
coordination ledgers that sessions write continuously (.agents/log, .agents/decisions, .agents/mail, .agents/joins,
.agents/requests.jsonl) refuses the start. Commit or hold the Leader's notes first.
With --branch <b> --session <s> it also refuses a branch whose changed paths lack a coord claim from that node's
session (DC-NEW-wl-t36-claim): every path in `git diff --name-only <merge-base of main and b> <b>` must be covered by
a `claim` event in <repo root>/.agents/log/<s>.jsonl (exact path, or a directory lease minus its --except paths), or
be exempt because coord refuses to claim it. The exemption is coord-core.py's own: the coordination record
(.agents/**, COORD-CLAIM-SELF) and the register and derived classes from `classify`. A missing or unreadable log refuses.
A claim whose path ends in "/" refuses (PD-6: the record keeps the slash and the lease covers nothing). A changed
path whose newest `kind: refused` row in <repo root>/.agents/decisions/<s>.jsonl is newer than every covering claim
refuses as claimed-after-refusal. coord records only contention and hook denials as refused rows; a stale-controls,
register-class, derived-class or TTL-cap claim refusal at the CLI leaves no row (L2-refusals.txt).
Exit 0 = clean, start the join; 1 = refused (foreign or unclaimed paths listed); 2 = usage (the check did not run).
--base <rev> replaces the merge-base (to re-check a branch already merged into main, where the merge-base is its
own head and the diff is empty); an empty diff refuses rather than passes.
Usage: python -I leader_prejoin.py <repo root> [--branch <b> --session <s> [--base <rev>]]
"""
import importlib.util
import json
import pathlib
import subprocess
import sys

USAGE = ("usage: python -I leader_prejoin.py <repo root> [--branch <b> --session <s> [--base <rev>]]   "
         "exit: 0 clean, 1 refused, 2 usage")
ARGV = sys.argv[1:]
BRANCH = SESSION = BASE = None
if len(ARGV) in (5, 7) and ARGV[1] == "--branch" and ARGV[3] == "--session":
    BRANCH, SESSION = ARGV[2], ARGV[4]
    if len(ARGV) == 7 and ARGV[5] == "--base":
        BASE = ARGV[6]
        ARGV = ARGV[:1]
    elif len(ARGV) == 5:
        ARGV = ARGV[:1]
if len(ARGV) != 1 or ARGV[0].startswith("-"):
    print(USAGE)
    sys.exit(2)
ROOT = pathlib.Path(ARGV[0])
if not (ROOT / ".git").exists():
    print(f"leader-prejoin: {ROOT} is not a git checkout\n{USAGE}")
    sys.exit(2)

LEDGER_DIRS = (".agents/log/", ".agents/decisions/", ".agents/mail/", ".agents/joins/")
LEDGER_FILES = {".agents/requests.jsonl"}

status = subprocess.run(["git", "status", "--porcelain", "--untracked-files=all"], cwd=ROOT,
                        capture_output=True, text=True, encoding="utf-8", errors="replace")
if status.returncode != 0:
    print(f"leader-prejoin: git status failed: {status.stderr.strip()}\n{USAGE}")
    sys.exit(2)

foreign = []
for line in status.stdout.splitlines():
    path = line[3:].split(" -> ")[-1].strip('"')
    if path in LEDGER_FILES or path.startswith(LEDGER_DIRS):
        continue
    foreign.append(line)

if foreign:
    print("leader-prejoin: REFUSED - the join's step 7 would sweep these uncommitted paths into the join commit:")
    for line in foreign:
        print("  " + line)
    print("  commit or hold them first (a ruling is committed alone; Leader notes in their own commit)")
    sys.exit(1)
print("leader-prejoin: clean (only coordination ledgers are uncommitted)")
if BRANCH is None:
    sys.exit(0)


def git_out(*args):
    r = subprocess.run(["git", *args], cwd=ROOT, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if r.returncode != 0:
        print(f"leader-prejoin: REFUSED - git {' '.join(args)} failed: {r.stderr.strip()}")
        sys.exit(1)
    return r.stdout.strip()


def load_coord():
    path = pathlib.Path(__file__).resolve().parents[3] / "docs" / "ai-forward-pack" / "scripts" / "coord-core.py"
    spec = importlib.util.spec_from_file_location("coord_core_for_prejoin", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


coord = load_coord()
log = ROOT / ".agents" / "log" / f"{SESSION}.jsonl"
if not log.is_file():
    print(f"leader-prejoin: REFUSED - claim check not recorded: no session log {log} (a missing log is never a pass)")
    sys.exit(1)
leases = []
try:
    for line in log.read_text(encoding="utf-8").splitlines():
        if line.strip():
            event = json.loads(line)
            if event.get("kind") == "claim" and event.get("session") == SESSION:
                leases.append({"path": event["path"], "except": event.get("except", []), "at": float(event.get("at", 0))})
except (OSError, ValueError, KeyError, AttributeError) as exc:
    print(f"leader-prejoin: REFUSED - claim check not recorded: session log {log} unreadable ({exc})")
    sys.exit(1)

slashed = sorted({lease["path"] for lease in leases if lease["path"].endswith("/")})
if slashed:
    print("leader-prejoin: REFUSED - a trailing-slash claim is a false grant (PD-6): coord records the slash and "
          "the lease covers nothing:")
    for path in slashed:
        print(f"  {path}  ->  coord claim --wi workbench-loops --path {path.rstrip('/')}")
    sys.exit(1)

refusals = {}
decisions = ROOT / ".agents" / "decisions" / f"{SESSION}.jsonl"
try:
    for line in (decisions.read_text(encoding="utf-8").splitlines() if decisions.is_file() else []):
        if line.strip():
            event = json.loads(line)
            if event.get("kind") == "refused" and event.get("session") == SESSION:
                path = coord._norm(event["path"])
                refusals[path] = max(refusals.get(path, 0.0), float(event.get("at", 0)))
except (OSError, ValueError, KeyError, AttributeError) as exc:
    print(f"leader-prejoin: REFUSED - claim check not recorded: decision log {decisions} unreadable ({exc})")
    sys.exit(1)

base = BASE or git_out("merge-base", "main", BRANCH)
changed = [p for p in git_out("diff", "--name-only", base, BRANCH).splitlines() if p]
if not changed:
    print(f"leader-prejoin: REFUSED - claim check not recorded: {BRANCH} changes no path against {base[:12]} "
          "(already merged into main? pass --base <fork point>)")
    sys.exit(1)
unclaimed, after_refusal, exempt = [], [], {}
for path in changed:
    if coord._norm(path).split("/")[0] == coord.COORD_DIRNAME:
        exempt.setdefault("coordination-record", []).append(path)
        continue
    klass, _why = coord.classify(ROOT / coord.COORD_DIRNAME, path)
    if klass in ("register", "derived"):
        exempt.setdefault(klass, []).append(path)
    elif not any(coord.lease_covers(lease, path) for lease in leases):
        unclaimed.append(path)
    elif refusals.get(coord._norm(path), -1.0) > max(
            lease["at"] for lease in leases if coord.lease_covers(lease, path)):
        after_refusal.append(path)
exempt_text = ", ".join(f"{k} {len(v)}" for k, v in sorted(exempt.items())) or "none"
print(f"leader-prejoin: claim check session {SESSION} branch {BRANCH}: checked {len(changed)}, "
      f"exempt {sum(len(v) for v in exempt.values())} ({exempt_text}), unclaimed {len(unclaimed)}, "
      f"claimed-after-refusal {len(after_refusal)}")
if unclaimed:
    print("leader-prejoin: REFUSED - UNCLAIMED paths (no coord claim from this session covers them):")
    for path in unclaimed:
        print("  " + path)
    sys.exit(1)
if after_refusal:
    print("leader-prejoin: REFUSED - claimed-after-refusal (the last coord outcome for the path is a refusal, "
          "newer than any claim covering it; a refused claim is not granted, DC-NEW-wl-t36-claim):")
    for path in after_refusal:
        print("  " + path)
    sys.exit(1)
sys.exit(0)
