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

THE FIFTH REPORT (INV-0010, 2026-09-12) found the column right and the close wrong, three
ways, each now a rule here:

  * A product host whose OWNER IS DEAD hid in `unknown`: a `conhost.exe --headless` has one
    hop of ancestry, no path and no name. It is named by the RUNTIME'S OWN SIGNATURE
    (`ours-orphaned`): the headless host beside a shell carrying our integration script,
    same dead parent, created within two seconds of each other.
  * Windows Terminal's own seven processes and Ollama's launcher inflated `unknown` by nine:
    FOREIGN_ROOTS carried the right tokens and was matched against NAMES, while the tokens
    live in the EXECUTABLE PATH (`Microsoft.IntelligentTerminal_.../OpenConsole.exe`) or in
    the program a `cmd.exe /C` wrapper runs. A process's own executable path -- and its
    direct parent's -- is a structural fact; the whole chain's argv is still a rumour.
  * The largest class was `foreign` twice running and the close said "reported only"; the
    pool regrew and the operator reported it a fifth time (DC-155). The report now ends
    with the ONE ACTION that shrinks the largest foreign root, and asks for the re-count.
    And the pool was OURS BY CAUSE, foreign only by parent: a ConPTY shell of ours that
    inherited WT_SESSION from the Windows Terminal tab this harness runs in is treated by
    Windows Terminal's agent host as one of its tabs, which attaches an agent session (its
    MCP servers) per shell. Measured 4/4 -> 0/4 with WT_* stripped (INV-0010 slice 0). So
    ancestry is not attribution: the report also states how many of the pool were born
    beside one of our own terminal.start lines, and says "not recorded" when it cannot.

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
import base64
import binascii
import json
import os
import pathlib
import re
import subprocess
import sys
import time
from datetime import datetime

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
# OURS_ORPHANED  a console host or shell from OUR terminal runtime whose owner is dead. Named by
#                the runtime's own signature -- a `--headless` conhost beside a shell carrying our
#                integration script (`-EncodedCommand` of `$global:__AideNonce`), both created
#                within seconds, both recording the same dead parent -- because ancestry cannot
#                name a host whose parent is gone, and `unknown` is where a straggler hides (INV-0010).
OURS_ORPHANED = "ours-orphaned"
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

# The ConPTY host's shape. `CreatePseudoConsole` starts `conhost.exe --headless --width W
# --height H --signal 0x.. --server 0x..`; a console attached to an ordinary process is
# `conhost.exe 0x4`. Windows Terminal's `OpenConsole.exe --headless` carries the flag too,
# which is why the flag alone never attributes anything.
HEADLESS_HOST = re.compile(r"conhost\.exe.*--headless", re.I)
# Our shell integration travels as `-EncodedCommand <base64 of UTF-16LE script>` and the
# script sets `$global:__AideNonce` (ShellIntegration.cs). Decoded, never substring-matched
# on the base64: the marker is in the script, and the script is what the shell runs.
ENCODED_COMMAND = re.compile(r"-EncodedCommand\s+([A-Za-z0-9+/=]+)", re.I)
INTEGRATION_MARKER = "$global:__AideNonce"
SHELL_NAMES = {"powershell.exe", "pwsh.exe"}
# A host and its shell are created by one StartAsync, back to back. Two seconds is an order
# of magnitude above the measured gap (0.2 s, INV-0010 13:39:05.7 -> 13:39:05.9) and an order
# below the interval at which panes are opened by hand.
SIBLING_WINDOW_SECONDS = 2.0
# A `cmd.exe /C ...` wrapper: the program it runs is the last quoted `.exe` path in its argv.
CMD_WRAPPER = re.compile(r'cmd\.exe"?\s+/[cC]\b')
QUOTED_EXE = re.compile(r'"([A-Za-z]:\\[^"]*\.exe)"', re.I)


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
    reused = False
    cur = pid
    while cur and cur in procs and cur not in seen and len(chain) < limit:
        seen.add(cur)
        chain.append(procs[cur])
        parent = procs.get(procs[cur]["ppid"])
        # PID REUSE: a parent created AFTER its child is not its parent -- the owner died and
        # the id was handed to something else. The walk stops here, which is what makes the
        # orphaned-session signature (one hop, dead parent) reachable at all.
        if parent is not None:
            child_at, parent_at = created_at(procs[cur]), created_at(parent)
            if child_at is not None and parent_at is not None and parent_at > child_at:
                reused = True
                break
        cur = procs[cur]["ppid"]
    orphaned = bool(chain) and chain[-1]["ppid"] != 0 and (chain[-1]["ppid"] not in procs or reused)
    return chain, orphaned


def created_at(proc):
    """The creation time as a datetime, or None when the row carries none. Never guessed."""
    text = (proc.get("created") or "").strip()
    if not text:
        return None
    # Win32_Process gives seven fractional digits; trim to the six datetime accepts everywhere.
    text = re.sub(r"(\.\d{6})\d+", r"\1", text)
    try:
        return datetime.fromisoformat(text)
    except ValueError:
        return None


def carries_integration_script(proc):
    """True when this process's -EncodedCommand decodes to our shell integration."""
    if proc["name"].lower() not in SHELL_NAMES:
        return False
    m = ENCODED_COMMAND.search(proc["cmd"])
    if not m:
        return False
    try:
        script = base64.b64decode(m.group(1), validate=False).decode("utf-16-le", errors="ignore")
    except (ValueError, binascii.Error):
        return False
    return INTEGRATION_MARKER in script


def is_headless_host(proc):
    """A ConPTY host by shape. Whether its owner is dead is the caller's question (ancestry)."""
    return proc["name"].lower() == "conhost.exe" and bool(HEADLESS_HOST.search(proc["cmd"]))


def orphaned_session_sibling(procs, proc):
    """For an orphaned headless host: our orphaned shell beside it, or None.

    Same dead parent, our integration script, created within SIBLING_WINDOW_SECONDS. All
    three, or nothing: a lone orphaned `--headless` host with no sibling stays `unknown`.
    """
    when = created_at(proc)
    if when is None:
        return None
    for other in procs.values():
        if other["pid"] == proc["pid"] or other["ppid"] != proc["ppid"]:
            continue
        if not carries_integration_script(other):
            continue
        other_when = created_at(other)
        if other_when is None:
            continue
        if abs((other_when - when).total_seconds()) <= SIBLING_WINDOW_SECONDS:
            return other
    return None


def executable_paths(proc):
    """The paths this process RUNS: its own image, and for a `cmd.exe /C` wrapper the program
    it launches (the last quoted .exe path in its argv). Structural, unlike the argv blob."""
    cmd = proc["cmd"]
    paths = []
    m = QUOTED_EXE.match(cmd)
    if m:
        paths.append(m.group(1))
    elif cmd:
        paths.append(cmd.split(" ", 1)[0])
    if CMD_WRAPPER.search(cmd):
        quoted = QUOTED_EXE.findall(cmd)
        if len(quoted) > 1:
            paths.append(quoted[-1])
    return paths


def foreign_by_path(procs, proc):
    """A FOREIGN_ROOTS token in the executable path of this process or of its DIRECT parent.

    Not the whole chain: everything the operator runs interactively descends from Windows
    Terminal, and a chain-wide path match would file this repository's own test runs as
    Windows Terminal's. One hop names a tab (OpenConsole, the tab's shell) and a wrapper's
    console host, and nothing further down.
    """
    candidates = [proc]
    parent = procs.get(proc["ppid"])
    if parent is not None:
        candidates.append(parent)
    for p in candidates:
        for path in executable_paths(p):
            hit = FOREIGN_ROOTS.search(path)
            if hit:
                return "%s (%s)" % (p["name"], hit.group(0))
    return None


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
    # A build server's own console host goes with its server: it ends when the server is
    # retired, and filing it `unknown` while the server is `build-server` splits one thing
    # across two rows (INV-0010).
    if name == "conhost.exe" and len(chain) > 1 and (
            chain[1]["name"].lower() in BUILD_SERVER_NAMES
            or (chain[1]["name"].lower() == "dotnet.exe" and BUILD_SERVER_CMD.search(chain[1]["cmd"]))):
        return BUILD_SERVER, "console host of a .NET build server, retired with it"
    # OURS BY SIGNATURE, WHEN ANCESTRY HAS NOTHING TO SAY (INV-0010). The owner is dead, so
    # the chain is one hop and carries no path and no name. A `--headless` host beside our
    # shell -- same dead parent, our integration script in its -EncodedCommand, created
    # within two seconds -- is a session of ours whose owner died. Both rows are ours.
    if orphaned and len(chain) == 1:
        if carries_integration_script(proc):
            return OURS_ORPHANED, ("our terminal runtime's shell (integration script in "
                                   "-EncodedCommand), owner pid %d dead" % proc["ppid"])
        if is_headless_host(proc):
            sibling = orphaned_session_sibling(procs, proc)
            if sibling is not None:
                return OURS_ORPHANED, ("ConPTY host beside our orphaned shell pid %d, owner pid %d dead"
                                       % (sibling["pid"], proc["ppid"]))
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
    # AFTER the worktree-path rule, so a run of ours launched from a Windows Terminal tab is
    # still ours; the tab itself, its OpenConsole, and a wrapper's console host are not.
    by_path = foreign_by_path(procs, proc)
    if by_path:
        return FOREIGN, "executable path of %s -- another application, reported only" % by_path
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


def headline(items):
    """The group header names EVERY distinct reason in the group, not the first one.

    Printing items[0] read as "529 processes, ancestor PresentMonService.exe" when
    PresentMonService owned exactly ONE of them and Windows Terminal's agent host owned the
    other 512. Each row's own classification was right; the HEADER invented a majority
    attribution nobody had measured -- DC-131 inside the tool written to prevent it. A
    distribution, or nothing. Covered by self_test, which shows the items[0] formula fails.
    """
    reasons = {}
    for _p, why in items:
        reasons[why] = reasons.get(why, 0) + 1
    ordered = sorted(reasons.items(), key=lambda kv: -kv[1])
    if len(ordered) == 1:
        return ordered[0][0]
    text = "; ".join("%d x %s" % (n, why) for why, n in ordered[:3])
    if len(ordered) > 3:
        text += "; +%d more reason(s)" % (len(ordered) - 3)
    return text


def report(rows, procs):
    buckets = {}
    for klass, p, why in rows:
        buckets.setdefault(klass, []).append((p, why))
    print("%-16s%7s   %s" % ("CLASS", "COUNT", "WHAT IT IS"))
    print("-" * 96)
    for klass in (OURS_LIVE, OURS_DETACHED, OURS_STRAGGLER, OURS_ORPHANED, BUILD_SERVER, FOREIGN, UNKNOWN):
        items = buckets.get(klass, [])
        if not items:
            continue
        print("%-16s%7d   %s" % (klass, len(items), headline(items)))
        by_name = {}
        for p, _why in items:
            by_name.setdefault(p["name"], []).append(p)
        for name, ps in sorted(by_name.items(), key=lambda kv: -len(kv[1])):
            print("%-16s%7d     %s" % ("", len(ps), name))
    print("-" * 96)
    print("%-16s%7d" % ("TOTAL", len(rows)))
    action = foreign_action(procs, buckets)
    if action:
        print("\n" + action)
    return buckets


def foreign_root_of(procs, proc):
    """The TOPMOST ancestor whose name is a foreign root, and the first one met on the way up."""
    chain, _orphaned = ancestry(procs, proc["pid"])
    hits = [p for p in chain if FOREIGN_ROOTS.search(p["name"])]
    if not hits:
        return None, None
    return hits[-1], hits[0]


# What shrinks a known foreign pool. Keyed on the process the pool hangs from (`via`), because
# that is the thing whose lifecycle is leaking; the text is the operator's action, not ours.
KNOWN_ACTIONS = (
    (re.compile(r"copilot\.exe", re.I),
     "CAUSED BY THIS REPOSITORY, foreign only by parent: a ConPTY shell of ours that inherited "
     "WT_SESSION/WT_PROFILE_ID from the Windows Terminal tab the harness runs in is treated by "
     "Windows Terminal's agent host as one of its own tabs, and it attaches an agent session "
     "(its MCP servers) per shell, kept for Windows Terminal's lifetime (INV-0010 slice 0, "
     "measured 4/4 -> 0/4). The runtime now strips WT_* from every ConPTY child; the pool that "
     "exists was born before that and does not shrink on its own. Restart Windows Terminal, then "
     "re-count with this tool: a birth AFTER the fix is a spawn path that still inherits WT_*."),
)

# The birth window: a member born this soon after one of our terminal.start lines is counted as
# correlated. The agent host attached within a second in the measurement; ten is generous.
BIRTH_WINDOW_SECONDS = 10.0


def our_terminal_starts():
    """UTC timestamps of every `terminal.start` line in today's and yesterday's workbench log,
    or None when no log can be read -- the correlation then reads "not recorded", never 0."""
    base = pathlib.Path(os.environ.get("LOCALAPPDATA", "")) / "AiDe" / "logs"
    if not base.is_dir():
        return None
    from datetime import timedelta, timezone
    now = datetime.now(timezone.utc)
    starts = []
    found = False
    for day in (now, now - timedelta(days=1)):
        log = base / ("workbench-%s.log" % day.strftime("%Y%m%d"))
        if not log.is_file():
            continue
        found = True
        for line in log.read_text(encoding="utf-8", errors="replace").splitlines():
            if '"evt":"terminal.start"' not in line:
                continue
            try:
                ts = json.loads(line).get("ts") or ""
                starts.append(datetime.fromisoformat(re.sub(r"(\.\d{6})\d+", r"\1", ts)))
            except (ValueError, TypeError):
                continue
    return sorted(starts) if found else None


def births_near(members, starts, window=BIRTH_WINDOW_SECONDS):
    """How many members were born within `window` seconds AFTER one of `starts`.

    The cause-vs-parent rule (DC-155): a process whose parent is another application's and whose
    birth follows one of OUR terminal starts is ours by cause. Members with no creation time are
    not counted either way -- an honest denominator is the count of those that have one.
    """
    counted = 0
    dated = 0
    for p in members:
        when = created_at(p)
        if when is None:
            continue
        dated += 1
        for start in starts:
            gap = (when - start).total_seconds()
            if 0 <= gap <= window:
                counted += 1
                break
    return counted, dated


def leaf_of(proc):
    """`name <last three path segments of the first argument>` -- the workload a pool member runs."""
    cmd = proc["cmd"]
    m = QUOTED_EXE.match(cmd)
    rest = cmd[m.end():] if m else (cmd.split(" ", 1)[1] if " " in cmd else "")
    arg = rest.strip().split(" ", 1)[0].strip('"') if rest.strip() else ""
    parts = re.split(r"[\\/]", arg)
    label = "/".join(parts[-3:]) if arg else ""
    return ("%s %s" % (proc["name"], label)).strip()


def foreign_action(procs, buckets):
    """DC-155's control: the one action that shrinks the largest foreign root, or None.

    A census whose largest class is `foreign` and whose close says "reported only" leaves the
    operator's screen exactly as it was; the same pool was attributed twice and reported a
    fifth time. The label was right. The close answered the wrong question.
    """
    items = buckets.get(FOREIGN, [])
    if not items:
        return None
    groups = {}
    for p, _why in items:
        root, via = foreign_root_of(procs, p)
        if root is None:
            key = ("path", foreign_by_path(procs, p) or "executable path")
            groups.setdefault(key, {"root": None, "vias": {}, "members": []})["members"].append(p)
            continue
        group = groups.setdefault(("root", root["pid"]), {"root": root, "vias": {}, "members": []})
        group["members"].append(p)
        group["vias"][via["pid"]] = via
        group.setdefault("via_counts", {})[via["pid"]] = group.get("via_counts", {}).get(via["pid"], 0) + 1
    key, group = max(groups.items(), key=lambda kv: len(kv[1]["members"]))
    members = group["members"]
    if group["root"] is None:
        return ("ACTION: %d under %s: not AiDe's -- another application's own processes; "
                "reported only." % (len(members), key[1]))
    root = group["root"]
    # The process the pool hangs from: the ancestor most of the members share, not whichever
    # member was enumerated first (a header that names a minority is DC-131 inside the tool).
    via_pid = max(group["via_counts"].items(), key=lambda kv: kv[1])[0]
    via = group["vias"][via_pid]
    # The workload: the most common leaf command among the pool's non-console members.
    leaves = {}
    for p in members:
        if p["name"].lower() == "conhost.exe":
            continue
        leaf = leaf_of(p)
        leaves[leaf] = leaves.get(leaf, 0) + 1
    workload = ""
    if leaves:
        leaf, n = max(leaves.items(), key=lambda kv: kv[1])
        workload = " (%d x %s)" % (n, leaf[:80])
    text = ("not AiDe's -- reported only. The action that shrinks it belongs to %s's owner: "
            "end or restart it, then re-count." % root["name"])
    for pattern, known in KNOWN_ACTIONS:
        if pattern.search(via["name"]) or pattern.search(root["name"]):
            text = known
            break
    chain = "%s[%d]" % (root["name"], root["pid"])
    if via["pid"] != root["pid"]:
        chain += " -> %s[%d]" % (via["name"], via["pid"])
    starts = our_terminal_starts()
    if starts is None:
        correlation = "birth correlation with our terminal.start lines: not recorded (no workbench log)"
    else:
        near, dated = births_near(members, starts)
        correlation = ("%d of %d dated members born within %ds of one of our %d terminal.start lines "
                       "(workbench log: the App's sessions; test-run sessions are not in it)"
                       % (near, dated, BIRTH_WINDOW_SECONDS, len(starts)))
    return "ACTION: %d under %s%s: %s\n        %s" % (len(members), chain, workload, text, correlation)


def reap(procs, buckets):
    busy, who = build_is_busy(procs)
    servers = [(p, w) for p, w in buckets.get(BUILD_SERVER, []) if p["name"].lower() != "conhost.exe"]
    stragglers = buckets.get(OURS_STRAGGLER, []) + buckets.get(OURS_ORPHANED, [])
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
        if klass not in (OURS_STRAGGLER, OURS_ORPHANED, BUILD_SERVER):
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
    checks = [0]
    tables = [0]

    def expect(label, got, want):
        checks[0] += 1
        if got != want:
            failures.append("%s: got %r, wanted %r" % (label, got, want))

    def table(rows):
        tables[0] += 1
        return _table(rows)

    wt = str(ROOT).lower()

    # 1. ancestry runs to the ROOT, not one level. This is the DC-131 falsifier: the
    #    conhost's direct parent is ALIVE, and the thing above it is dead.
    procs = table([
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
    procs = table([
        {"pid": 1, "ppid": 0, "name": "copilot.exe", "created": "", "cmd": "copilot.exe --acp --stdio"},
        {"pid": 2, "ppid": 1, "name": "node.exe", "created": "", "cmd": "node higgsfield-mcp/src/server.js"},
        {"pid": 3, "ppid": 2, "name": "conhost.exe", "created": "", "cmd": "conhost 0x4"},
    ])
    expect("copilot pool is foreign", classify(procs, procs[3], {wt})[0], FOREIGN)

    # 4. a LIVE run of ours is ours-live, not a straggler. Reaping this breaks a test run.
    procs = table([
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
    procs = table([
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
    procs = table([
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

    # 5d. THE ORPHANED SESSION (INV-0010, the fifth report). A ConPTY host is a child of the
    #     process that called CreatePseudoConsole, and when that owner dies -- the App closed, a
    #     test host killed -- the host's ppid still names it, and ancestry stops dead at the
    #     first hop. Nothing in the chain is named AiDe, no worktree path is in a conhost's argv,
    #     so the census files it under `unknown` and never counts it as ours. The RUNTIME'S OWN
    #     SIGNATURE is what names it: `conhost.exe --headless` (CreatePseudoConsole's shape, not a
    #     console-attached `0x4`) created within seconds of a sibling shell that carries our
    #     integration script, both recording the same dead parent.
    procs = table([
        {"pid": 80, "ppid": 500, "name": "conhost.exe", "created": "2026-09-12T13:39:05.7000000+00:00",
         "cmd": r"\??\C:\Windows\system32\conhost.exe --headless --width 120 --height 30 --signal 0x1a4 --server 0x1a0"},
        {"pid": 81, "ppid": 500, "name": "powershell.exe", "created": "2026-09-12T13:39:05.9000000+00:00",
         "cmd": '"powershell.exe" -NoLogo -NoExit -EncodedCommand IwAgAEEASQAtAEQARQAgAHMAaABlAGwAbAAgAGkAbgB0AGUAZwByAGEAdABpAG8AbgAuAAoAJABnAGwAbwBiAGEAbAA6AF8AXwBBAGkAZABlAE4AbwBuAGMAZQAgAD0AIAAnAGQAZQBhAGQAYgBlAGUAZgBjAGEAZgBlAGYAMAAwAGQAJwAKAA=='},
    ])
    _chain, session_orphaned = ancestry(procs, 80)
    expect("the orphaned session host LOOKS unattributable", session_orphaned, True)
    expect("an orphaned ConPTY host beside our orphaned shell is OURS-ORPHANED",
           classify(procs, procs[80], {wt})[0], OURS_ORPHANED)
    expect("and the orphaned shell carrying our integration script is OURS-ORPHANED",
           classify(procs, procs[81], {wt})[0], OURS_ORPHANED)

    # 5e. The negative half, so the rule cannot be satisfied by matching `--headless` alone:
    #     Windows Terminal's OpenConsole is headless too, and a lone orphaned conhost with no
    #     sibling and no signature is NOT ours -- it stays honestly unattributed.
    procs = table([
        {"pid": 90, "ppid": 91, "name": "WindowsTerminal.exe", "created": "", "cmd": "WindowsTerminal.exe"},
        {"pid": 92, "ppid": 90, "name": "OpenConsole.exe", "created": "",
         "cmd": "OpenConsole.exe --headless --textMeasurement graphemes --width 120 --height 27 --signal 0x8dc --server 0x8d4"},
        {"pid": 93, "ppid": 600, "name": "conhost.exe", "created": "2026-09-12T13:39:05.7000000+00:00",
         "cmd": r"\??\C:\Windows\system32\conhost.exe --headless --width 80 --height 25 --signal 0x2 --server 0x3"},
    ])
    expect("Windows Terminal's own headless host is not ours",
           classify(procs, procs[92], {wt})[0] != OURS_ORPHANED, True)
    expect("a lone orphaned headless host with no signature stays unknown",
           classify(procs, procs[93], {wt})[0], UNKNOWN)

    # 5f. WINDOWS TERMINAL'S OWN PROCESSES, by executable PATH (INV-0010). The token is in the
    #     package directory, not the image name, so a name match filed all seven `unknown`. The
    #     tab shell has no token of its own -- its DIRECT parent does. And a run of OURS launched
    #     from that tab keeps its worktree attribution: the path rule is one hop, never the chain.
    pkg = r"C:\Program Files\WindowsApps\Microsoft.IntelligentTerminal_0.2.2395.0_x64__8wekyb3d8bbwe"
    procs = table([
        {"pid": 100, "ppid": 0, "name": "explorer.exe", "created": "", "cmd": "explorer.exe"},
        {"pid": 101, "ppid": 100, "name": "WindowsTerminal.exe", "created": "", "cmd": '"%s\\WindowsTerminal.exe" ' % pkg},
        {"pid": 102, "ppid": 101, "name": "OpenConsole.exe", "created": "",
         "cmd": '"%s\\OpenConsole.exe" --headless --textMeasurement graphemes --width 120 --height 27 --signal 0x8dc --server 0x8d4' % pkg},
        {"pid": 103, "ppid": 101, "name": "powershell.exe", "created": "",
         "cmd": '"C:\\WINDOWS\\System32\\WindowsPowerShell\\v1.0\\powershell.exe"'},
        {"pid": 104, "ppid": 103, "name": "dotnet.exe", "created": "", "cmd": "dotnet test %s\\tests\\x.csproj" % wt},
        {"pid": 105, "ppid": 104, "name": "conhost.exe", "created": "", "cmd": "conhost --headless"},
    ])
    expect("Windows Terminal itself is foreign by path", classify(procs, procs[101], {wt})[0], FOREIGN)
    expect("its OpenConsole tab host is foreign by path", classify(procs, procs[102], {wt})[0], FOREIGN)
    expect("its tab shell is foreign by its parent's path", classify(procs, procs[103], {wt})[0], FOREIGN)
    expect("a run of ours from that tab is still OURS", classify(procs, procs[104], {wt})[0], OURS_LIVE)
    expect("and its host is ours, not Windows Terminal's", classify(procs, procs[105], {wt})[0], OURS_LIVE)
    # ONE HOP, pinned: a token-less shell two hops under Windows Terminal (a bash the tab shell
    # started) is not the tab's -- it stays honestly unknown, which is what keeps this rule from
    # being the chain-wide match that filed 543 of 547 processes foreign once.
    procs[106] = {"pid": 106, "ppid": 103, "name": "bash.exe", "created": "", "cmd": '"C:\\Program Files\\Git\\bin\\bash.exe"'}
    expect("two hops under Windows Terminal is not one hop", classify(procs, procs[106], {wt})[0], UNKNOWN)

    # 5g. OLLAMA'S LAUNCHER: a `cmd.exe /C` wrapper whose program is the quoted path it runs,
    #     and the wrapper's own console host one hop below. Both foreign; neither `unknown`.
    procs = table([
        {"pid": 110, "ppid": 999, "name": "cmd.exe", "created": "",
         "cmd": '"C:\\Windows\\system32\\cmd.exe" /C set PATH=C:\\Users\\x\\AppData\\Local\\Programs\\Ollama;%PATH% & "C:\\Users\\x\\AppData\\Local\\Programs\\Ollama\\ollama app.exe"'},
        {"pid": 111, "ppid": 110, "name": "conhost.exe", "created": "", "cmd": "\\??\\C:\\Windows\\system32\\conhost.exe 0x4"},
    ])
    expect("Ollama's cmd wrapper is foreign by the program it runs", classify(procs, procs[110], {wt})[0], FOREIGN)
    expect("and its console host follows it", classify(procs, procs[111], {wt})[0], FOREIGN)

    # 5h. A build server's console host is filed WITH its server, not `unknown`.
    procs = table([
        {"pid": 120, "ppid": 999, "name": "VBCSCompiler.exe", "created": "", "cmd": "-pipename:abc"},
        {"pid": 121, "ppid": 120, "name": "conhost.exe", "created": "", "cmd": "\\??\\C:\\Windows\\system32\\conhost.exe 0x4"},
    ])
    expect("the compiler server's console host is build-server", classify(procs, procs[121], {wt})[0], BUILD_SERVER)

    # 5i. assert_clean READS ours-orphaned: an orphaned session of ours fails the gate.
    procs = table([
        {"pid": 130, "ppid": 500, "name": "conhost.exe", "created": "2026-09-12T13:39:05.7000000+00:00",
         "cmd": r"\??\C:\Windows\system32\conhost.exe --headless --width 120 --height 30 --signal 0x1a4 --server 0x1a0"},
        {"pid": 131, "ppid": 500, "name": "powershell.exe", "created": "2026-09-12T13:39:05.9000000+00:00",
         "cmd": '"powershell.exe" -NoLogo -NoExit -EncodedCommand IwAgAEEASQAtAEQARQAgAHMAaABlAGwAbAAgAGkAbgB0AGUAZwByAGEAdABpAG8AbgAuAAoAJABnAGwAbwBiAGEAbAA6AF8AXwBBAGkAZABlAE4AbwBuAGMAZQAgAD0AIAAnAGQAZQBhAGQAYgBlAGUAZgBjAGEAZgBlAGYAMAAwAGQAJwAKAA=='},
    ])
    expect("assert_clean fires on an orphaned session of ours", assert_clean(procs, {wt}, None), 1)
    # ... but not when the host and the shell are FOUR seconds apart: two panes, not one session.
    apart = table([
        dict(procs[130]),
        dict(procs[131], created="2026-09-12T13:39:09.9000000+00:00"),
    ])
    expect("a host four seconds from the shell is not its sibling",
           classify(apart, apart[130], {wt})[0], UNKNOWN)
    expect("the lone shell is still ours by its script", classify(apart, apart[131], {wt})[0], OURS_ORPHANED)

    # 5j. DC-155's CONTROL: the report names the ONE ACTION that shrinks the largest foreign
    #     root. A close that says "foreign -- reported only" left the same pool for a fifth
    #     report. Three MCP servers and their hosts under wta -> copilot, one PresentMon: the
    #     line must name wta, copilot and the config file, and count the pool, not PresentMon.
    procs = table([
        {"pid": 60, "ppid": 0, "name": "wta.exe", "created": "",
         "cmd": 'wta.exe --master \\\\.\\pipe\\wta --agent "copilot --acp --stdio" --agent-id copilot'},
        {"pid": 61, "ppid": 60, "name": "copilot.exe", "created": "", "cmd": '"copilot.exe" --acp --stdio'},
        {"pid": 62, "ppid": 61, "name": "node.exe", "created": "", "cmd": "node higgsfield-mcp/src/server.js"},
        {"pid": 63, "ppid": 62, "name": "conhost.exe", "created": "", "cmd": "conhost 0x4"},
        {"pid": 64, "ppid": 61, "name": "node.exe", "created": "", "cmd": "node higgsfield-mcp/src/server.js"},
        {"pid": 65, "ppid": 64, "name": "conhost.exe", "created": "", "cmd": "conhost 0x4"},
        {"pid": 66, "ppid": 61, "name": "node.exe", "created": "", "cmd": "node higgsfield-mcp/src/server.js"},
        {"pid": 67, "ppid": 66, "name": "conhost.exe", "created": "", "cmd": "conhost 0x4"},
        {"pid": 70, "ppid": 0, "name": "PresentMonService.exe", "created": "", "cmd": "PresentMonService.exe"},
        {"pid": 71, "ppid": 70, "name": "conhost.exe", "created": "", "cmd": "conhost 0x4"},
    ])
    buckets = {}
    for klass, p, why in census(procs, {wt}):
        buckets.setdefault(klass, []).append((p, why))
    action = foreign_action(procs, buckets) or ""
    expect("the report carries an action line", action.startswith("ACTION: "), True)
    expect("it counts the largest root's pool, not PresentMon's", action.startswith("ACTION: 6 under wta.exe[60]"), True)
    expect("it names the process the pool hangs from", "copilot.exe[61]" in action, True)
    expect("it names the workload", "higgsfield-mcp" in action, True)
    expect("it names the cause: our shells, not the operator's config",
           "CAUSED BY THIS REPOSITORY" in action and "WT_SESSION" in action, True)
    expect("it does not send the operator to their MCP config", "mcp-config.json" in action, False)
    expect("it carries the birth correlation or says not recorded",
           "born within" in action or "not recorded" in action, True)
    expect("no foreign rows, no action line", foreign_action(procs, {}), None)

    # 5k. THE CAUSE-VS-PARENT RULE (DC-155): births beside our terminal.start lines are counted,
    #     births elsewhere are not, and a member with no creation time is left out of both.
    from datetime import timedelta, timezone
    t0 = datetime(2026, 9, 12, 13, 39, 5, tzinfo=timezone.utc)
    pool = [
        {"pid": 1, "ppid": 0, "name": "node.exe", "created": (t0 + timedelta(seconds=1)).isoformat(), "cmd": ""},
        {"pid": 2, "ppid": 0, "name": "node.exe", "created": (t0 + timedelta(seconds=9)).isoformat(), "cmd": ""},
        {"pid": 3, "ppid": 0, "name": "node.exe", "created": (t0 + timedelta(seconds=60)).isoformat(), "cmd": ""},
        {"pid": 4, "ppid": 0, "name": "node.exe", "created": (t0 - timedelta(seconds=5)).isoformat(), "cmd": ""},
        {"pid": 5, "ppid": 0, "name": "node.exe", "created": "", "cmd": ""},
    ]
    expect("two of four dated births follow the start", births_near(pool, [t0]), (2, 4))
    expect("no starts, no correlation", births_near(pool, []), (0, 4))

    # 5l. PID REUSE: the dead owner's id was handed to a process created AFTER our shell. The
    #     walk must not adopt the impostor as the parent, or the signature rule never fires and
    #     the orphan hides under whatever the impostor's chain says.
    procs = table([
        {"pid": 500, "ppid": 0, "name": "explorer.exe", "created": "2026-09-12T14:00:00.0000000+00:00", "cmd": "explorer.exe"},
        {"pid": 130, "ppid": 500, "name": "conhost.exe", "created": "2026-09-12T13:39:05.7000000+00:00",
         "cmd": r"\??\C:\Windows\system32\conhost.exe --headless --width 120 --height 30 --signal 0x1a4 --server 0x1a0"},
        {"pid": 131, "ppid": 500, "name": "powershell.exe", "created": "2026-09-12T13:39:05.9000000+00:00",
         "cmd": '"powershell.exe" -NoLogo -NoExit -EncodedCommand IwAgAEEASQAtAEQARQAgAHMAaABlAGwAbAAgAGkAbgB0AGUAZwByAGEAdABpAG8AbgAuAAoAJABnAGwAbwBiAGEAbAA6AF8AXwBBAGkAZABlAE4AbwBuAGMAZQAgAD0AIAAnAGQAZQBhAGQAYgBlAGUAZgBjAGEAZgBlAGYAMAAwAGQAJwAKAA=='},
    ])
    _chain, reused = ancestry(procs, 130)
    expect("a parent born after its child ends the walk", len(_chain), 1)
    expect("and the shell is still ours-orphaned under a reused pid", classify(procs, procs[131], {wt})[0], OURS_ORPHANED)
    expect("and so is its host", classify(procs, procs[130], {wt})[0], OURS_ORPHANED)

    # 6. unattributable stays unattributable -- it must NOT be swept into 'ours'.
    procs = table([{"pid": 20, "ppid": 0, "name": "conhost.exe", "created": "", "cmd": "conhost 0x4"}])
    expect("unknown stays unknown", classify(procs, procs[20], {wt})[0], UNKNOWN)

    # 7. assert_clean FAILS on a straggler and PASSES when the window is clean -- both
    #    directions, because a check that only ever passes proves nothing (DC-104).
    dirty = table([
        {"pid": 40, "ppid": 41, "name": "VBCSCompiler.exe", "created": "T1", "cmd": "-pipename:z"},
    ])
    expect("assert_clean fires on a straggler", assert_clean(dirty, {wt}, None), 1)
    clean = table([
        {"pid": 50, "ppid": 0, "name": "conhost.exe", "created": "T1", "cmd": "conhost 0x4"},
    ])
    expect("assert_clean passes when clean", assert_clean(clean, {wt}, None), 0)

    # 8. a build server that PREDATES the window is not this run's fault.
    import tempfile
    with tempfile.TemporaryDirectory() as tmp:
        snap = pathlib.Path(tmp) / "s.json"
        snap.write_text(json.dumps({"40": "T1"}), encoding="utf-8")
        expect("pre-existing server is excused", assert_clean(dirty, {wt}, str(snap)), 0)

    # The header falsifier (DC-131 inside the tool): a group where the FIRST item's reason
    # is the rarest one. The items[0] formula would headline the 1-of-513 attribution.
    skewed = [(None, "ancestor PresentMonService.exe")] +              [(None, "ancestor WindowsTerminal.exe")] * 512
    expect("header is a distribution, majority first",
           headline(skewed), "512 x ancestor WindowsTerminal.exe; 1 x ancestor PresentMonService.exe")
    expect("header of a single-reason group is that reason",
           headline([(None, "ancestor node.exe")] * 3), "ancestor node.exe")
    expect("the items[0] formula is the defect, not the control",
           skewed[0][1] == headline(skewed), False)

    if failures:
        print("SELF-TEST FAILED -- the gate's own oracle is wrong:")
        for f in failures:
            print("  " + f)
        return 1
    print("\nself-test: %d assertions over %d synthetic process tables, all passing." % (checks[0], tables[0]))
    print("  The DC-131 falsifier: a conhost whose DIRECT PARENT IS ALIVE and whose")
    print("    grandparent is dead. A one-level orphan check scores that clean.")
    print("  The wta trap: `copilot --acp --stdio` is byte-identical to what our own")
    print("    EngineCatalog would run, and must still attribute to Windows Terminal.")
    print("  The daemon: parentless by design, and reaping it drops a live workspace.")
    print("  assert_clean in BOTH directions, plus the pre-existing-process exclusion.")
    print("  INV-0010: an orphaned session of ours is named by the runtime's signature; Windows")
    print("    Terminal and Ollama are foreign by executable path; the largest foreign root")
    print("    gets an ACTION line, not a label (DC-155).")
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

    buckets = report(census(procs, paths), procs)
    if not args.reap:
        print("\nDRY RUN. Nothing was ended. Re-run with --reap to act.")
        print("`foreign` and `unknown` are NEVER reaped, at any flag -- reported only.")
        return 0
    return reap(procs, buckets)


if __name__ == "__main__":
    sys.exit(main())
