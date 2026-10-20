"""T40 F2 driver: N full App runs, each bracketed by a board read and a read-only left-button read."""
import re
import subprocess
import sys
import time

T = r"C:\Projects\ai-de-wl-focus-none"
D = T + r"\docs\proof\workbench-loops-support\focus-none"
MOUSE = T + r"\docs\proof\workbench-loops-support\deadholder-repro\mouse-state.ps1"
RUNS = T + r"\focus-runs"
N = int(sys.argv[1]) if len(sys.argv) > 1 else 10


def utc():
    return time.strftime("%H:%M:%SZ", time.gmtime())


def say(line):
    with open(D + r"\runs.txt", "a") as f:
        f.write(line + "\n")


def board_hold():
    out = subprocess.run(["python", "docs/ai-forward-pack/scripts/coord-board.py", "board"], cwd=T,
                         capture_output=True, text=True).stdout.splitlines()
    demo = join = None
    for ln in out:
        s = ln.strip()
        if "dispatch" in s[:40] or "report " in s[:40]:
            continue
        if s.startswith("USER DEMO IN PROGRESS"):
            demo = True
        if s.startswith("USER DEMO DONE"):
            demo = False
        m = re.match(r"(join in flight|join done|join abandoned)", s)
        if m:
            join = m.group(1) == "join in flight"
    return "USER DEMO IN PROGRESS" if demo else ("join in flight" if join else None)


def mouse():
    return subprocess.run(["powershell.exe", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", MOUSE],
                          capture_output=True, text=True).stdout.strip()


for k in range(1, N + 1):
    hold = board_hold()
    if hold:
        say("%d HELD before run: %s" % (k, hold))
        break
    s = mouse()
    with open(D + r"\mouse.txt", "a") as f:
        f.write("%d start  %s\n" % (k, s))
    if "0x8000" in s:
        say("%d STOPPED: left button down at start" % k)
        break
    t0 = utc()
    with open(RUNS + r"\app-%d.txt" % k, "w") as out:
        subprocess.run(["dotnet", "test", "tests/AiDe.App.Tests", "--no-build", "--logger", "trx;LogFileName=app-%d.trx" % k,
                        "--results-directory", RUNS], cwd=T, stdout=out, stderr=subprocess.STDOUT)
    t1 = utc()
    e = mouse()
    with open(D + r"\mouse.txt", "a") as f:
        f.write("%d end    %s\n" % (k, e))
    last = ""
    for ln in open(RUNS + r"\app-%d.txt" % k, errors="replace"):
        if re.match(r"(Passed|Failed)!", ln):
            last = ln.strip()
    say("%d  %s  %s  app: %s" % (k, t0, t1, last))
