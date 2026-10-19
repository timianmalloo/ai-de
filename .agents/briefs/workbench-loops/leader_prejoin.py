"""Leader pre-join check: refuse to start conductor-join on a checkout carrying foreign uncommitted files.

conductor-join.py's step 7 stages every uncommitted file, so a Leader edit left in the working tree (an Owner's
register entry, obligations.md) lands inside the join commit: Ruling 171 was swept into the T18 join (61c9f4b0,
DC-NEW-wl-leader2 12th instance). This runs BEFORE the join: any modified or untracked path outside the
coordination ledgers that sessions write continuously (.agents/log, .agents/decisions, .agents/mail, .agents/joins,
.agents/requests.jsonl) refuses the start. Commit or hold the Leader's notes first.
Exit 0 = clean, start the join; 1 = refused (foreign paths listed); 2 = usage (the check did not run).
Usage: python -I leader_prejoin.py <repo root>
"""
import pathlib
import subprocess
import sys

USAGE = "usage: python -I leader_prejoin.py <repo root>   exit: 0 clean, 1 refused, 2 usage"
if len(sys.argv) != 2 or sys.argv[1].startswith("-"):
    print(USAGE)
    sys.exit(2)
ROOT = pathlib.Path(sys.argv[1])
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
sys.exit(0)
