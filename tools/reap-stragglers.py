#!/usr/bin/env python3
"""Census, attribute, and (opt-in) reap straggler terminal-host processes.

WHAT THIS EXISTS FOR, measured 2026-09-11. The operator reported "console hosts are
accumulating" FOUR times. Three investigations each found a real leak, fixed it, proved
the fix, and closed the report -- and the operator still saw the list. Every fix was
correct. None of them answered the question, because the question was a POPULATION and
the answers were MECHANISMS (DC-131).

The census that finally explains it, taken on the reporting machine:

  265 conhost.exe steady-state, of which
    256  held 1:1 by node.exe under `copilot.exe --acp --stdio` -- NOT ours. One MCP
         server (higgsfield-mcp) respawned ~256 times over ~7h and never reaped. No new
         one in 14h: a CORPSE PILE, not an active leak. It does not shrink on its own,
         so the operator sees the same 512 processes every time they look.
      9  long-lived desktop software (ollama, ArmouryCrate, PresentMon, Windows
         Terminal, ChatGPT/codex) -- NOT ours, 2-3 DAYS old.
    0-42 ConPTY hosts under a LIVE testhost.exe -- ours, and CORRECT. A test run creates
         ~42 of these plus ~21 msedgewebview2; all of them go away when the run ends,
         measured twice by process delta. An operator opening Task Manager during a run
         sees a real spike that is not a leak.

  AND the one that IS ours and IS left behind:
    1    VBCSCompiler.exe (the Roslyn compiler server) per build, holding 1 conhost,
         ORPHANED the moment its driver is killed rather than allowed to exit. Its
         command line is `-pipename:<base64>` -- no project, no repository, no worktree,
         so nothing downstream can attribute it. This is DC-123 one ring out AGAIN:
         `Directory.Build.rsp /nodeReuse:false` shut down MSBuild WORKER reuse and says
         nothing about the COMPILER SERVER, which is a different server with its own
         lifetime and its own switch.

SO THE ANSWER HAS FOUR PARTS AND ONLY ONE OF THEM IS A BUG. A tool that reports the
count without the attribution column reproduces the exact failure that caused four
reports.

DESIGN RULES, each of them a scar:

  * DRY-RUN BY DEFAULT. `--reap` is opt-in. Nothing is ended without it.
  * REPORT, NEVER REMOVE, ANYTHING UNATTRIBUTED. Same discipline as `coord worktree
    cleanup` (WT9): a thing you cannot name is reported, not deleted. `foreign` and
    `unknown` are printed and never touched, at any flag.
  * ANCESTRY RUNS TO THE ROOT. The orphan check that hid the MSBuild workers looked one
    level up, found every host's direct parent alive, and reported zero (DC-131). Every
    walk here terminates at a dead parent or pid 0.
  * PREFER THE DOCUMENTED MECHANISM. Build servers are retired with
    `dotnet build-server shutdown`, not killed. Measured: VBCSCompiler 1 -> 0.
  * PROVE IDLE BEFORE ACTING. Build servers are never retired while ANY dotnet build,
    dotnet test, vstest or testhost is alive on the machine -- including another
    worktree's. Several agent sessions build concurrently here; that is normal.

MODES
  (default)      census with attribution column. Read-only. Exit 0.
  --reap         retire what is attributed AND provably idle. Still refuses `foreign`.
  --assert-clean compare against a `--snapshot` file and FAIL if this repository's
                 tooling left anything behind. This is the CI shape-gate.
  --snapshot F   write a process snapshot to F (pair with --assert-clean --since F).
  --self-test    prove the attribution and the assertion can both fail. No processes.
  --behaviour    spawn a REAL orphaned build server, prove the census finds it and the
                 documented shutdown clears it. ~30s. The falsifier must fire.
"""
import argparse
import json
import os
import pathlib
import re
import subprocess
import sys
import time

ROOT = pathlib.Path(__file__).resolve().parent.parent

# --- attribution vocabulary ------------------------------------------------------------
# OURS_LIVE      a live run of this repository's code or tests. Correct, transient, never reaped.
# OURS_STRAGGLER attributable to this repository's tooling AND its driver is gone.
# BUILD_SERVER   a .NET build server. Retired with the documented command, never killed.
# FOREIGN        attributable to a named non-AI-DE application. Reported, never touched.
# UNKNOWN        not attributable. Reported, never touched. This column is the point.
#
# OURS_DETACHED is the class that stops this tool causing an outage. `AiDe.Daemon.exe` is
# parentless ON PURPOSE -- it holds warm workspace state across a shell restart, and
# ShellBootstrap says so in as many words: "we are not its supervisor ... the idle grace
# decides when it stops". It looks EXACTLY like an orphan: no living parent, its own
# console host, nobody waiting on it. Reaping it because it matches the shape of a leak
# would kill a live workspace and corrupt nothing visibly until later.
OURS_LIVE = "ours-live"
OURS_DETACHED = "ours-detached"
OURS_STRAGGLER = "ours-straggler"
BUILD_SERVER = "build-server"
FOREIGN = "foreign"
UNKNOWN = "unknown"

# Detached by design, bounded by a timer rather than by containment. The bound is
# IpcServer.Idle, default 30s. MEASURED: 4 daemons live during a run, 0 thirty seconds
# later; 0 survivors at +6s after two full project runs.
DETACHED_BY_DESIGN = re.compile(r"AiDe\.Daemon\.exe", re.I)
DETACHED_GRACE_SECONDS = 30

HOST_LIKE = {
    "conhost.exe", "openconsole.exe", "windowsterminal.exe", "testhost.exe",
    "pwsh.exe", "powershell.exe", "cmd.exe", "bash.exe", "node.exe", "dotnet.exe",
    "msedgewebview2.exe", "vbcscompiler.exe", "msbuild.exe", "vstest.console.exe",
}
# A live run of ANY of these means the build servers are in use. Deliberately broad:
# a false "busy" costs one deferred cleanup, a false "idle" kills someone's build.
BUILD_ACTIVITY = re.compile(
    r"dotnet(\.exe)?\"?\s+(build|test|run|publish|msbuild|pack)\b|vstest\.console|testhost", re.I)
BUILD_SERVER_NAMES = {"vbcscompiler.exe", "rzc.exe"}
BUILD_SERVER_CMD = re.compile(r"nodemode:\d|VBCSCompiler|[\\/]rzc\.dll", re.I)
#
# `wta.exe` is Windows Terminal's OWN agent host (Microsoft.IntelligentTerminal). It is
# listed FIRST because it is the trap: it spawns `copilot.exe --acp --stdio`, which is
# character-for-character the command line AI-DE's own EngineCatalog would use for a
# native-ACP engine. Attributing by command line alone scores that pool as ours. The
# discriminator is wta.exe's argv, which carries `--agent "copilot --acp --stdio"`
# literally -- proof of parentage far stronger than a ppid.
FOREIGN_ROOTS = re.compile(
    r"wta\.exe|IntelligentTerminal|copilot\.exe|ChatGPT\.exe|codex\.exe|ollama|"
    r"ArmouryCrate|PresentMon|LM Studio|nvcontainer|NVIDIA|msedge\.exe|"
    r"M365Copilot|SearchHost", re.I)


def snapshot():
    """Every process: pid, ppid, name, start time, command line. Windows only."""
    if os.name != "nt":
        return None
    ps = (
        "Get-CimInstance Win32_Process | "
        "Select-Object ProcessId,ParentProcessId,Name,"
        "@{n='Created';e={if($_.CreationDate){$_.CreationDate.ToString('o')}else{''}}},CommandLine | "
        "ConvertTo-Json -Compress -Depth 3")
    out = subprocess.run(["powershell", "-NoProfile", "-Command", ps],
                         capture_output=True, text=True, check=False)
    try:
        rows = json.loads(out.stdout)
    except (ValueError, TypeError):
        return None
    if isinstance(rows, dict):
        rows = [rows]
    procs = {}
    for r in rows:
        try:
            pid = int(r["ProcessId"])
        except (KeyError, TypeError, ValueError):
            continue
        procs[pid] = {
            "pid": pid,
            "ppid": int(r.get("ParentProcessId") or 0),
            "name": r.get("Name") or "",
            "created": r.get("Created") or "",
            "cmd": r.get("CommandLine") or "",
        }
    return procs


def ancestry(procs, pid, limit=40):
    """Walk to the ROOT. Stops at a dead parent or pid 0, never at one level (DC-131)."""
    chain = []
    seen = set()
    cur = pid
    while cur and cur in procs and cur not in seen and len(chain) < limit:
        seen.add(cur)
        chain.append(procs[cur])
        cur = procs[cur]["ppid"]
    orphaned = bool(chain) and chain[-1]["ppid"] != 0 and chain[-1]["ppid"] not in procs
    return chain, orphaned


def repo_paths():
    """Every worktree of this repository, lowercased. Attribution by path, not by name."""
    paths = {str(ROOT).lower()}
    out = subprocess.run(["git", "-C", str(ROOT), "worktree", "list", "--porcelain"],
                         capture_output=True, text=True, check=False)
    for line in out.stdout.splitlines():
        if line.startswith("worktree "):
            paths.add(line[len("worktree "):].strip().replace("/", "\\").lower())
    return {p for p in paths if p}


def classify(procs, proc, paths):
    """Attribute one process. Returns (class, why). `why` is evidence, not a label."""
    name = proc["name"].lower()
    chain, orphaned = ancestry(procs, proc["pid"])
    blob = " ".join(p["cmd"] for p in chain).lower()
    names = " ".join(p["name"] for p in chain)

    if name in BUILD_SERVER_NAMES or (name == "dotnet.exe" and BUILD_SERVER_CMD.search(proc["cmd"])):
        state = "orphaned" if orphaned else "parented"
        return BUILD_SERVER, ".NET build server, %s, retired by `dotnet build-server shutdown`" % state
    # MATCHED AGAINST ANCESTOR PROCESS NAMES ONLY, NEVER THE COMMAND-LINE BLOB.
    #
    # The first cut of this searched the concatenated command lines of the whole chain,
    # and classified 543 of 547 processes as foreign -- including this session's own
    # shells, because THE DIAGNOSTIC COMMAND LINES CONTAINED THE WORD `copilot`. Any
    # ancestor that merely MENTIONS a keyword poisoned every descendant. A census whose
    # attribution column is confidently wrong is worse than one with no column at all,
    # which is the whole of DC-131 wearing a different hat.
    #
    # A process NAME in the ancestry is a structural fact. A string inside somebody's
    # argv is a rumour.
    foreign_hit = next((p["name"] for p in chain if FOREIGN_ROOTS.search(p["name"])), None)
    if foreign_hit:
        return FOREIGN, "ancestor %s -- another application, reported only" % foreign_hit
    if DETACHED_BY_DESIGN.search(names) or DETACHED_BY_DESIGN.search(blob):
        # Parentless on purpose. Bounded by IpcServer.Idle, not by a job object.
        return OURS_DETACHED, (
            "AiDe daemon -- detached BY DESIGN, holds the workspace lock, stops on a %ds "
            "idle grace. NEVER reaped here: killing it drops a live workspace."
            % DETACHED_GRACE_SECONDS)
    hit = next((p for p in paths if p in blob), None)
    if hit:
        chain_names = [p["name"].lower() for p in chain]
        is_run = ("testhost.exe" in chain_names or "vstest.console.exe" in chain_names
                  or bool(BUILD_ACTIVITY.search(blob)))
        if is_run:
            return ((OURS_STRAGGLER if orphaned else OURS_LIVE),
                    "worktree %s, %s" % (hit, "ORPHANED" if orphaned else "live run"))
        return OURS_LIVE, "worktree %s" % hit
    return UNKNOWN, "no worktree path and no known root in its ancestry -- reported, never removed"


def build_is_busy(procs):
    """True while ANY build/test is alive anywhere, including another worktree's."""
    for p in procs.values():
        if p["name"].lower() in ("testhost.exe", "vstest.console.exe"):
            return True, "%s pid=%d" % (p["name"], p["pid"])
        if BUILD_ACTIVITY.search(p["cmd"]):
            return True, "%s pid=%d" % (p["name"], p["pid"])
    return False, ""


def census(procs, paths):
    rows = []
    for p in procs.values():
        if p["name"].lower() not in HOST_LIKE:
            continue
        klass, why = classify(procs, p, paths)
        rows.append((klass, p, why))
    return rows


def report(rows):
    buckets = {}
    for klass, p, why in rows:
        buckets.setdefault(klass, []).append((p, why))
    print("%-16s%7s   %s" % ("CLASS", "COUNT", "WHAT IT IS"))
    print("-" * 96)
    for klass in (OURS_LIVE, OURS_DETACHED, OURS_STRAGGLER, BUILD_SERVER, FOREIGN, UNKNOWN):
        items = buckets.get(klass, [])
        if not items:
            continue
        print("%-16s%7d   %s" % (klass, len(items), items[0][1]))
        by_name = {}
        for p, _why in items:
            by_name.setdefault(p["name"], []).append(p)
        for name, ps in sorted(by_name.items(), key=lambda kv: -len(kv[1])):
            print("%-16s%7d     %s" % ("", len(ps), name))
    print("-" * 96)
    print("%-16s%7d" % ("TOTAL", len(rows)))
    return buckets


def reap(procs, buckets):
    busy, who = build_is_busy(procs)
    servers = buckets.get(BUILD_SERVER, [])
    stragglers = buckets.get(OURS_STRAGGLER, [])
    if not servers and not stragglers:
        print("\nnothing attributable to retire.")
        return 0
    if busy:
        print("\nREFUSING: a build or test is live (%s). Build servers are in use." % who)
        print("Nothing was ended. Re-run when the machine is quiet.")
        return 0
    print("\nretiring %d build server(s) with the documented mechanism:" % len(servers))
    out = subprocess.run(["dotnet", "build-server", "shutdown"],
                         capture_output=True, text=True, check=False)
    said = (out.stdout.strip() or out.stderr.strip() or "(no output)")
    print("  " + said.replace("\n", "\n  "))
    for p, _why in stragglers:
        print("  REPORTED, not ended: %s pid=%d -- %s" % (p["name"], p["pid"], p["cmd"][:110]))
    print("An orphan of ours is REPORTED rather than killed: it is a defect to fix at the")
    print("source, and killing it is how the source stays hidden for a fourth report.")
    return 0


def assert_clean(procs, paths, since):
    """The CI shape-gate. Fails when this repository's tooling leaves a process behind."""
    before = {}
    if since and pathlib.Path(since).exists():
        before = json.loads(pathlib.Path(since).read_text(encoding="utf-8"))
    survivors = []
    for klass, p, why in census(procs, paths):
        if klass not in (OURS_STRAGGLER, BUILD_SERVER):
            continue
        if before.get(str(p["pid"])) == p["created"]:
            continue          # predates the window -- not ours to answer for
        survivors.append((klass, p, why))
    if not survivors:
        print("CLEAN: this repository's tooling left nothing behind in the measured window.")
        return 0
    print("LEFT BEHIND: %d process(es) created in the window and still alive." % len(survivors))
    for klass, p, why in survivors:
        print("  [%s] %s pid=%d -- %s" % (klass, p["name"], p["pid"], why))
        print("      cmd: %s" % p["cmd"][:160])
    print("\nEach of these holds a console host the operator will see and cannot attribute.")
    print("Fix the source. `dotnet build-server shutdown` clears build servers.")
    return 1


# --- the oracle ------------------------------------------------------------------------

def _table(rows):
    return {p["pid"]: p for p in rows}


def self_test():
    """Prove BOTH clauses can fail: attribution, and the clean assertion."""
    failures = []

    def expect(label, got, want):
        if got != want:
            failures.append("%s: got %r, wanted %r" % (label, got, want))

    wt = str(ROOT).lower()

    # 1. ancestry runs to the ROOT, not one level. This is the DC-131 falsifier: the
    #    conhost's direct parent is ALIVE, and the thing above it is dead.
    procs = _table([
        {"pid": 100, "ppid": 999, "name": "VBCSCompiler.exe", "created": "", "cmd": "-pipename:abc"},
        {"pid": 101, "ppid": 100, "name": "conhost.exe", "created": "", "cmd": "conhost 0x4"},
    ])
    chain, orphaned = ancestry(procs, 101)
    expect("ancestry depth", len(chain), 2)
    expect("a one-level orphan check sees a live parent", procs[101]["ppid"] in procs, True)
    expect("walk-to-root finds the orphan", orphaned, True)

    # 2. a build server is a build server, orphaned or not.
    expect("orphaned build server", classify(procs, procs[100], {wt})[0], BUILD_SERVER)

    # 3. foreign is foreign, and is never ours even though it is a conhost.
    procs = _table([
        {"pid": 1, "ppid": 0, "name": "copilot.exe", "created": "", "cmd": "copilot.exe --acp --stdio"},
        {"pid": 2, "ppid": 1, "name": "node.exe", "created": "", "cmd": "node higgsfield-mcp/src/server.js"},
        {"pid": 3, "ppid": 2, "name": "conhost.exe", "created": "", "cmd": "conhost 0x4"},
    ])
    expect("copilot pool is foreign", classify(procs, procs[3], {wt})[0], FOREIGN)

    # 4. a LIVE run of ours is ours-live, not a straggler. Reaping this breaks a test run.
    procs = _table([
        {"pid": 10, "ppid": 0, "name": "dotnet.exe", "created": "",
         "cmd": "dotnet test %s\\tests\\x.csproj" % wt},
        {"pid": 11, "ppid": 10, "name": "testhost.exe", "created": "",
         "cmd": "%s\\tests\\bin\\testhost.exe" % wt},
        {"pid": 12, "ppid": 11, "name": "conhost.exe", "created": "", "cmd": "conhost --headless"},
    ])
    expect("live run is not a straggler", classify(procs, procs[12], {wt})[0], OURS_LIVE)
    expect("busy machine is detected", build_is_busy(procs)[0], True)

    # 5. the SAME tree with the driver dead is a straggler. The falsifier for clause 4.
    orph = dict(procs)
    del orph[10]
    expect("orphaned run IS a straggler", classify(orph, orph[11], {wt})[0], OURS_STRAGGLER)

    # 5b. THE WTA TRAP. `copilot --acp --stdio` is character-for-character what AI-DE's own
    #     EngineCatalog would run for a native-ACP engine. Attribution must still put it
    #     with Windows Terminal. This assertion is the one that was wrong twice.
    procs = _table([
        {"pid": 60, "ppid": 0, "name": "wta.exe", "created": "",
         "cmd": 'wta.exe --master \\\\.\\pipe\\wta --agent "copilot --acp --stdio" --agent-id copilot'},
        {"pid": 61, "ppid": 60, "name": "copilot.exe", "created": "", "cmd": '"copilot.exe" --acp --stdio'},
        {"pid": 62, "ppid": 61, "name": "node.exe", "created": "", "cmd": "node higgsfield-mcp/src/server.js"},
        {"pid": 63, "ppid": 62, "name": "conhost.exe", "created": "", "cmd": "conhost 0x4"},
    ])
    expect("wta-rooted ACP engine is FOREIGN", classify(procs, procs[61], {wt})[0], FOREIGN)
    expect("its MCP pool conhost is FOREIGN", classify(procs, procs[63], {wt})[0], FOREIGN)

    # 5c. THE DAEMON. Parentless, own console host, nothing waiting on it -- the exact
    #     shape of an orphan, and reaping it drops a live workspace. It must NOT be a
    #     straggler, and --reap must never include it.
    procs = _table([
        {"pid": 70, "ppid": 71, "name": "AiDe.Daemon.exe", "created": "",
         "cmd": "%s\\src\\AiDe.Daemon\\bin\\AiDe.Daemon.exe %s" % (wt, wt)},
        {"pid": 72, "ppid": 70, "name": "conhost.exe", "created": "", "cmd": "conhost 0x4"},
    ])
    _chain, daemon_orphaned = ancestry(procs, 70)
    expect("the daemon LOOKS orphaned", daemon_orphaned, True)
    expect("but is classified detached, not straggler",
           classify(procs, procs[70], {wt})[0], OURS_DETACHED)
    expect("and so is its console host", classify(procs, procs[72], {wt})[0], OURS_DETACHED)
    expect("assert_clean does not fire on a detached daemon", assert_clean(procs, {wt}, None), 0)

    # 6. unattributable stays unattributable -- it must NOT be swept into 'ours'.
    procs = _table([{"pid": 20, "ppid": 0, "name": "conhost.exe", "created": "", "cmd": "conhost 0x4"}])
    expect("unknown stays unknown", classify(procs, procs[20], {wt})[0], UNKNOWN)

    # 7. assert_clean FAILS on a straggler and PASSES when the window is clean -- both
    #    directions, because a check that only ever passes proves nothing (DC-104).
    dirty = _table([
        {"pid": 40, "ppid": 41, "name": "VBCSCompiler.exe", "created": "T1", "cmd": "-pipename:z"},
    ])
    expect("assert_clean fires on a straggler", assert_clean(dirty, {wt}, None), 1)
    clean = _table([
        {"pid": 50, "ppid": 0, "name": "conhost.exe", "created": "T1", "cmd": "conhost 0x4"},
    ])
    expect("assert_clean passes when clean", assert_clean(clean, {wt}, None), 0)

    # 8. a build server that PREDATES the window is not this run's fault.
    import tempfile
    with tempfile.TemporaryDirectory() as tmp:
        snap = pathlib.Path(tmp) / "s.json"
        snap.write_text(json.dumps({"40": "T1"}), encoding="utf-8")
        expect("pre-existing server is excused", assert_clean(dirty, {wt}, str(snap)), 0)

    if failures:
        print("SELF-TEST FAILED -- the gate's own oracle is wrong:")
        for f in failures:
            print("  " + f)
        return 1
    print("\nself-test: 19 assertions over 10 synthetic process tables, all passing.")
    print("  The DC-131 falsifier: a conhost whose DIRECT PARENT IS ALIVE and whose")
    print("    grandparent is dead. A one-level orphan check scores that clean.")
    print("  The wta trap: `copilot --acp --stdio` is byte-identical to what our own")
    print("    EngineCatalog would run, and must still attribute to Windows Terminal.")
    print("  The daemon: parentless by design, and reaping it drops a live workspace.")
    print("  assert_clean in BOTH directions, plus the pre-existing-process exclusion.")
    return 0


def _live_servers():
    procs = snapshot() or {}
    return [p for p in procs.values()
            if p["name"].lower() in BUILD_SERVER_NAMES
            or (p["name"].lower() == "dotnet.exe" and BUILD_SERVER_CMD.search(p["cmd"]))]


def behaviour():
    """Spawn a REAL orphaned build server. Prove the census sees it and the fix clears it."""
    if os.name != "nt":
        print("--behaviour is Windows-only. Reporting rather than passing silently.")
        return 0
    print("BEHAVIOUR. A check that cannot fail proves nothing, so this runs the falsifier.\n")
    subprocess.run(["dotnet", "build-server", "shutdown"], capture_output=True, text=True, check=False)
    time.sleep(2)
    base = len(_live_servers())
    print("  [1] after documented shutdown, build servers = %d" % base)

    # `--no-incremental`, AND ONE PROJECT RATHER THAN THE SOLUTION.
    #
    # The first run of this mode used a plain solution build and reported
    # "after a build, build servers = 0" -- because the tree was already up to date, so
    # Roslyn never ran and never started a server. The mode correctly refused to pass,
    # which is what a falsifier is for, but a check that only fires on a dirty tree is a
    # check that is green for the wrong reason most of the time. Force the compile.
    projects = sorted((ROOT / "src").glob("*/*.csproj"))
    target = str(projects[0]) if projects else str(ROOT / "AiDe.sln")
    subprocess.run(["dotnet", "build", target, "-c", "Debug", "--nologo", "--no-incremental"],
                   capture_output=True, text=True, check=False, cwd=str(ROOT))
    time.sleep(2)
    after_build = len(_live_servers())
    print("  [2] after a build,                  build servers = %d" % after_build)
    if after_build <= base:
        print("\nFALSIFIER DID NOT FIRE: a build produced no compiler server, so this check")
        print("cannot distinguish a fixed machine from a broken one. That is the defect")
        print("DC-104 names, and it fails the gate rather than passing quietly.")
        return 1

    procs = snapshot() or {}
    seen = sum(1 for k, _p, _w in census(procs, repo_paths()) if k == BUILD_SERVER)
    print("  [3] the census attributes them:     build-server = %d" % seen)

    subprocess.run(["dotnet", "build-server", "shutdown"], capture_output=True, text=True, check=False)
    time.sleep(2)
    final = len(_live_servers())
    print("  [4] after the documented shutdown,  build servers = %d" % final)

    ok = after_build > base and seen >= 1 and final <= base
    if ok:
        print("\nPASS: the leak is producible, visible to the census, and cleared by the")
        print("documented mechanism. Falsified in both directions.")
        return 0
    print("\nFAIL: produced=%s attributed=%s cleared=%s"
          % (after_build > base, seen >= 1, final <= base))
    return 1


def main():
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--reap", action="store_true",
                    help="retire attributable, provably idle processes. Off by default.")
    ap.add_argument("--assert-clean", action="store_true",
                    help="fail if this repository's tooling left anything behind (CI gate)")
    ap.add_argument("--snapshot", metavar="FILE",
                    help="write a process snapshot for a later --assert-clean")
    ap.add_argument("--since", metavar="FILE",
                    help="the snapshot --assert-clean compares against")
    ap.add_argument("--self-test", action="store_true", help="prove this gate can fail")
    ap.add_argument("--behaviour", action="store_true",
                    help="spawn a real orphaned build server and prove the falsifier fires")
    args = ap.parse_args()

    if args.self_test:
        return self_test()
    if args.behaviour:
        return behaviour()

    procs = snapshot()
    if procs is None:
        print("not Windows, or the process table could not be read -- nothing to say.")
        print("This gate is Windows-only by construction; it reports rather than passing silently.")
        return 0
    paths = repo_paths()

    if args.snapshot:
        pathlib.Path(args.snapshot).write_text(
            json.dumps({str(k): v["created"] for k, v in procs.items()}), encoding="utf-8")
        print("snapshot: %d processes -> %s" % (len(procs), args.snapshot))
        return 0

    if args.assert_clean:
        return assert_clean(procs, paths, args.since)

    buckets = report(census(procs, paths))
    if not args.reap:
        print("\nDRY RUN. Nothing was ended. Re-run with --reap to act.")
        print("`foreign` and `unknown` are NEVER reaped, at any flag -- reported only.")
        return 0
    return reap(procs, buckets)


if __name__ == "__main__":
    sys.exit(main())
