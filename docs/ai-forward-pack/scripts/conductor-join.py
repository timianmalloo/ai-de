#!/usr/bin/env python3
"""conductor-join.py - the join, as a script: every step gated by its exit code, none by a
shell line.

The control for defect class DC-113's fourth recurrence (measured 2026-09-13, a consuming
repo's join): the conductor resolved a register conflict, piped the conflict-marker gate
through `| tail -1`, read the gate's remedy text as a pass, and committed a merge carrying
`<<<<<<<` (DC-136's shape) - caught only because the gate runner was re-run bare before the
push. Three earlier recurrences had the same cause: a hand-typed line at the join whose
status was the formatter's, not the gate's. This script IS the join line. It has no pipes;
each step is a subprocess whose return code decides whether the next runs. The
`execute-with-coordination` skill names it as the only join line.

Steps, in order, stop on the first red (the exit status is the failing step's number):
  1. git merge --no-ff <branch>       a conflict stops here with the file list; resolve by hand,
                                      `git add`, `git commit --no-edit`, re-run with --continue
  2. audit marker                     `audit-log.py start --session <s> --skill execute-with-coordination`
                                      so the join's own duration is MEASURED (a resumed or
                                      hand-typed join set none: 14 of 14 join entries carried
                                      no duration, no tier, no fan-out)
  3. verify-no-conflict-markers       ALWAYS, before anything else reads the tree
  4. checks, then the recount         from join.json (`checks`, `recount`); the recount is timed
                                      as recount_seconds - the largest cost centre of a measured
                                      programme (44.8% of the conductor's active main line) and
                                      the one no entry recorded. --docs-only skips the recount.
  5. audit-log append                 tier T1, fan-out 0, the marker's duration, recount_seconds
  6. regenerate                       `regenerate` from join.json, default `coord-core.py regen`
  7. git add -A && git commit         the join commit (the pre-commit boundary runs)
  8. gates                            `gates` from join.json, default `run-verify-gates.py`
  9. git push <remote> <branch>       only if 8 passed; --no-push skips
 10. build                            `build` from join.json, optional; --no-build skips

Every step is ALSO recorded, as it runs, in `.agents/joins/<audit-shortname>.json` - step
number, command, exit code, timestamps - with a terminal `complete` record written only after
the last step of the run returns. **A state file without `complete` is an unfinished join**,
machine-readably; a record still `running` names the step the process was inside when it
stopped. That is DC-227 (Ruling 128): on 2026-09-17 this script was started twice for real
joins under a harness background-shell mode; both captured logs ended at `== step 7: commit`,
what the harness reported for each, verbatim, was `[exited with code 0]`, and nothing was
pushed - whether the process itself exited 0 is NOT recorded. The tool skipped nothing and
reported nothing; the observer stopped observing and read a status the tool never emitted, off
a log that lagged because stdout was block-buffered. Both halves are fixed here: the log is
line-buffered (`:77-82`) and the state file says what the log cannot. The file is evidence,
not a gate - with no writable `.agents/` the join runs unchanged and the record degrades to
"not recorded". `.agents/joins/` writes its own `.gitignore`; see `JoinState`.

join.json (default docs/coordination/join.json; --join <path> overrides; absent = defaults):
  { "checks":     [["python3", "tools/verify-register.py", "--fix-counts"]],
    "recount":    [["python3", "tools/verify-test-run.py", "--update"]],
    "regenerate": [["python3", "docs/ai-forward-pack/scripts/coord-core.py", "regen"]],
    "gates":      [["python3", "docs/ai-forward-pack/scripts/run-verify-gates.py"]],
    "build":      [["dotnet", "build", "src/App/App.csproj", "-c", "Release"]],
    "push_remote": "origin",
    "trailer_file": "docs/coordination/commit-trailer.txt" }
  A command whose first word is `python3` or `python` runs under THIS interpreter.

Usage (from the checkout the join lands on):
  python3 conductor-join.py <branch> --title "<merge title>" --audit-shortname <name>
      --audit-summary "<text>" --audit-goal "<text>" --audit-done-when "<text>"
      [--artifact <path> ...] [--docs-only] [--no-push] [--no-build] [--continue]
      [--join <join.json>] [--session <id>] [--self-test]

Exit 0 on a complete join; the failing step's number otherwise; 2 on a usage error.
Stdlib only.
"""
from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
import tempfile
import time
from pathlib import Path

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace", line_buffering=True)
        except (ValueError, OSError):
            pass

HERE = Path(__file__).resolve().parent
PY = sys.executable
JOIN_JSON_DEFAULT = os.path.join("docs", "coordination", "join.json")
SKILL = "execute-with-coordination"


def _sibling(name: str) -> str:
    return str(HERE / name)


def repo_root(start: Path | None = None) -> Path | None:
    done = subprocess.run(["git", "rev-parse", "--show-toplevel"], cwd=str(start or Path.cwd()),
                          capture_output=True, text=True)
    if done.returncode != 0 or not done.stdout.strip():
        return None
    return Path(done.stdout.strip())


def _interp(command: list) -> list[str]:
    command = [str(c) for c in command]
    if command and command[0] in ("python3", "python"):
        command[0] = PY
    return command


def load_join(root: Path, path: str | None) -> dict:
    """The join contract for this repository, or the defaults. A malformed file is a usage
    error, never a silent fallback to defaults (a join that skipped the recount because its
    config had a typo would report as complete)."""
    candidate = Path(path) if path else root / JOIN_JSON_DEFAULT
    if not candidate.is_file():
        if path:
            raise SystemExit("conductor-join: --join {0} does not exist".format(candidate))
        return {}
    try:
        data = json.loads(candidate.read_text(encoding="utf-8"))
    except (OSError, ValueError) as error:
        raise SystemExit("conductor-join: {0} is not valid JSON ({1})".format(candidate, error))
    if not isinstance(data, dict):
        raise SystemExit("conductor-join: {0} must be a JSON object".format(candidate))
    return data


JOIN_STATE_GITIGNORE = (
    "# Written by conductor-join.py. A join-state file is ONE PROCESS'S progress record,\n"
    "# rewritten in place as that join runs. It is not `register` (append-only, merged by\n"
    "# union) and not `derived` (regenerable) - the two classes under which .agents/ content\n"
    "# is committed here (.agents/artifacts.yml). It is also out of phase with its own\n"
    "# commit: step 7 IS the commit, so the only version a join could ever commit is one with\n"
    "# no `complete`, and the completed version would land in a later commit or in none. In\n"
    "# the case this file exists for - a join killed before step 7 - nothing is committed at\n"
    "# all. The durable record of a join that finished is its step-5 audit entry; this file\n"
    "# is the record of one that did not. DC-227 / Ruling 128.\n"
    "*\n")


class JoinState:
    """The join's own progress record, machine-readable: `.agents/joins/<shortname>.json`.

    The control for DC-227 (Ruling 128 (1)). Measured instance, 2026-09-17: this script was
    started twice for real joins under a harness's background-shell mode. Both captured logs
    ended at `== step 7: commit`; what the harness reported for each, verbatim, was `[exited
    with code 0]`; nothing was pushed. Whether the process itself exited 0 is NOT recorded.
    The tool skipped nothing and reported nothing - the observer stopped observing and read a
    status the tool never emitted, off a log several steps behind because stdout was
    block-buffered.

    So the join writes down what it is doing while it does it, and the answer a reader needs is
    the ABSENCE of a key: one record per step invocation (step, what, command, exit code,
    timestamps) and a terminal `complete` record written only after the last step of the run
    returns. **A state file with no `complete` is an unfinished join** - no prose to parse. A
    record still `running` names the step the process was inside when it stopped, which is the
    question that was unanswerable on 2026-09-17.

    Evidence, never a gate (IO11). With no `.agents/` directory, or one that cannot be written,
    the join runs exactly as before and the record degrades to "not recorded" - it never
    degrades to a plausible wrong record. The directory is joined, never installed: a repo with
    no coordination layer gets no `.agents/`.
    """

    STAMP = "%Y-%m-%dT%H:%M:%SZ"

    def __init__(self, root: Path, shortname: str, log=print):
        self.root = root
        self.log = log
        self.path = None
        self.data = {}
        safe = "".join(c if (c.isalnum() or c in "._-") else "-" for c in (shortname or ""))
        self.name = safe.strip("-.") or "join"

    @classmethod
    def _now(cls) -> str:
        return time.strftime(cls.STAMP, time.gmtime())

    def begin(self, session: str, branch: str, merging) -> None:
        """Open the record for THIS run. The directory work happens here, not in __init__, so a
        join that never starts (a detached HEAD) leaves nothing behind."""
        base = self.root / ".agents"
        if not base.is_dir():
            self.log("   join state: not recorded (no .agents/ directory)")
            return
        directory = base / "joins"
        try:
            directory.mkdir(exist_ok=True)
            marker = directory / ".gitignore"
            if not marker.exists():
                marker.write_text(JOIN_STATE_GITIGNORE, encoding="utf-8")
        except OSError as error:
            self.log("   join state: not recorded ({0})".format(error))
            return
        self.path = directory / (self.name + ".json")
        previous = self._preserve()
        self.data = {"shortname": self.name, "session": session, "branch": branch,
                     "merging": merging, "pid": os.getpid(), "started": self._now(), "steps": []}
        if previous:
            self.data["previous"] = previous
        self._write()
        if self.path is not None:
            self.log("   join state: {0}{1}".format(
                Path(".agents") / "joins" / self.path.name,
                " (the earlier attempt is at {0})".format(previous) if previous else ""))

    def _preserve(self):
        """A second run under one shortname - a `--continue`, or a re-run after a fix - never
        overwrites the first attempt. One record is one process, because "did THAT process
        finish?" is the only question this file answers; so the earlier file moves aside under
        its own start stamp and the new one names it in `previous`, leaving the chain walkable
        and `<shortname>.json` always the current attempt."""
        if self.path is None or not self.path.exists():
            return None
        try:
            stamp = str(json.loads(self.path.read_text(encoding="utf-8")).get("started") or "")
        except (OSError, ValueError):
            stamp = ""
        if not stamp:
            try:
                stamp = time.strftime(self.STAMP, time.gmtime(self.path.stat().st_mtime))
            except OSError:
                stamp = self._now()
        stamp = "".join(c for c in stamp if c.isalnum())
        for n in range(1000):
            name = "{0}.{1}{2}.json".format(self.name, stamp, "" if n == 0 else "-{0}".format(n))
            target = self.path.parent / name
            if not target.exists():
                try:
                    os.replace(str(self.path), str(target))
                except OSError as error:
                    self.log("   join state: the earlier attempt could not be preserved "
                             "({0})".format(error))
                    return None
                return name
        return None

    def starting(self, step: int, what: str, command) -> None:
        if self.path is None:
            return
        self.data.setdefault("steps", []).append(
            {"step": step, "what": what, "command": [str(c) for c in command],
             "status": "running", "started": self._now(), "exit_code": None, "ended": None})
        self._write()

    def finished(self, exit_code: int, ok: bool) -> None:
        if self.path is None or not self.data.get("steps"):
            return
        record = self.data["steps"][-1]
        record["exit_code"] = exit_code
        record["status"] = "ok" if ok else "failed"
        record["ended"] = self._now()
        self._write()

    def step(self, step: int, what: str, command, exit_code: int, ok: bool) -> None:
        """A step the join runs itself rather than through `Join.run` - the merge."""
        self.starting(step, what, command)
        self.finished(exit_code, ok)

    def skipped(self, step: int, what: str, why: str) -> None:
        """A step that did not run is recorded as skipped, not omitted: a file whose records
        stop at 8 must mean the process stopped at 8, never "9 and 10 were switched off"."""
        if self.path is None:
            return
        now = self._now()
        self.data.setdefault("steps", []).append(
            {"step": step, "what": what, "command": [], "status": "skipped", "why": why,
             "started": now, "exit_code": None, "ended": now})
        self._write()

    def complete(self, **fields) -> None:
        """Written ONLY after the last step of the run returns. Its absence is the finding."""
        if self.path is None:
            return
        fields["at"] = self._now()
        self.data["complete"] = fields
        self._write()

    def _write(self) -> None:
        """Whole file, then `os.replace` - atomic, so a process terminated mid-write leaves the
        previous record rather than a truncated one no reader can parse."""
        if self.path is None:
            return
        temporary = self.path.with_name(self.path.name + ".writing")
        try:
            temporary.write_text(json.dumps(self.data, indent=2) + "\n", encoding="utf-8")
            os.replace(str(temporary), str(self.path))
        except OSError as error:
            self.log("   join state: not recorded ({0})".format(error))
            self.path = None


class Join:
    def __init__(self, root: Path, env: dict, log=print, state: JoinState | None = None):
        self.root = root
        self.env = env
        self.log = log
        self.state = state

    def run(self, step: int, what: str, command: list[str], allow=(0,)) -> subprocess.CompletedProcess:
        self.log("\n== step {0}: {1}\n   $ {2}".format(step, what, " ".join(command)))
        if self.state is not None:
            self.state.starting(step, what, command)
        completed = subprocess.run(command, cwd=str(self.root), env=self.env, text=True,
                                   encoding="utf-8", errors="replace", capture_output=True)
        if self.state is not None:
            self.state.finished(completed.returncode, completed.returncode in allow)
        tail = (completed.stdout + completed.stderr).strip().splitlines()
        for line in tail[-8:]:
            self.log("   | " + line[:200])
        if completed.returncode not in allow:
            self.log("\nconductor-join: step {0} ({1}) exited {2} - the join stops here.".format(
                step, what, completed.returncode))
            raise SystemExit(step)
        return completed

    def git(self, *args) -> subprocess.CompletedProcess:
        return subprocess.run(["git", *args], cwd=str(self.root), capture_output=True, text=True,
                              encoding="utf-8", errors="replace")


def join(args, root: Path, contract: dict, log=print) -> int:
    env = dict(os.environ)
    env.setdefault("PYTHONIOENCODING", "utf-8")
    session = args.session or env.get("AGENT_SESSION") or contract.get("session") or "conductor"
    env["AGENT_SESSION"] = session
    env.setdefault("AGENT_NAME", session)
    state = JoinState(root, args.audit_shortname, log)
    j = Join(root, env, log, state)

    branch_now = j.git("branch", "--show-current").stdout.strip()
    if not branch_now:
        log("conductor-join: run from a checkout on a branch (HEAD is detached)")
        return 2
    state.begin(session, branch_now, args.branch if not args.cont else None)

    trailer = ""
    trailer_file = args.trailer_file or contract.get("trailer_file")
    if trailer_file:
        trailer = "\n\n" + (root / trailer_file).read_text(encoding="utf-8").strip() + "\n"

    if not args.cont:
        if not args.branch:
            log("conductor-join: a branch is required unless --continue")
            return 2
        log("\n== step 1: merge --no-ff {0} into {1}".format(args.branch, branch_now))
        merge = j.git("merge", "--no-ff", args.branch, "-m", args.title + trailer)
        for line in (merge.stdout + merge.stderr).strip().splitlines()[-6:]:
            log("   | " + line[:200])
        if merge.returncode != 0:
            conflicts = j.git("diff", "--name-only", "--diff-filter=U").stdout.strip().splitlines()
            log("\nconductor-join: the merge has conflicts - resolve by hand (a DERIVED file is "
                "regenerated, never resolved), `git add` them, `git commit --no-edit`, then "
                "re-run with --continue:")
            for c in conflicts:
                log("   - " + c)
            state.step(1, "merge", ["git", "merge", "--no-ff", args.branch],
                       merge.returncode, False)
            return 1
        state.step(1, "merge", ["git", "merge", "--no-ff", args.branch], merge.returncode, True)
    else:
        log("\n== step 1: --continue (the merge was resolved by hand)")
        state.skipped(1, "merge", "--continue (resolved by hand)")

    # The join's own marker (F-22): one marker measures one join (AL4a), keyed to the skill
    # so a prompt logged meanwhile cannot consume it.
    j.run(2, "audit marker", [PY, _sibling("audit-log.py"), "start", "--session", session, "--skill", SKILL])

    j.run(3, "no conflict markers", [PY, _sibling("verify-no-conflict-markers.py")])

    for command in contract.get("checks") or []:
        j.run(4, "check", _interp(command))
    recount_started = time.monotonic()
    recount = contract.get("recount") or []
    if args.docs_only:
        log("\n== step 4: recount skipped (--docs-only)")
        state.skipped(4, "recount", "--docs-only")
    elif not recount:
        log("\n== step 4: no recount configured (join.json `recount` is empty)")
        state.skipped(4, "recount", "join.json `recount` is empty")
    else:
        for command in recount:
            j.run(4, "recount", _interp(command))
    recount_seconds = int(time.monotonic() - recount_started)

    audit = [PY, _sibling("audit-log.py"), "append",
             "--shortname", args.audit_shortname, "--session", session,
             "--skill", SKILL, "--kind", "skill", "--tier", "T1", "--fan-out", "0",
             "--prompt", "the join of {0} into {1}".format(args.branch or "the resolved merge", branch_now),
             "--summary", "{0} recount_seconds={1} (docs_only={2}).".format(
                 args.audit_summary, recount_seconds, args.docs_only),
             "--goal", args.audit_goal, "--done-when", args.audit_done_when,
             "--signal-verification-path", "true", "--signal-verification-executed", "true",
             "--signal-acceptance-met", "true"]
    for a in args.artifact:
        audit += ["--artifact", a]
    j.run(5, "audit entry", audit)

    regenerate = contract.get("regenerate")
    if regenerate is None:
        regenerate = [[PY, _sibling("coord-core.py"), "regen"]]
    for command in regenerate:
        j.run(6, "regenerate derived views", _interp(command))

    j.run(7, "stage", ["git", "add", "-A"])
    j.run(7, "commit", ["git", "commit", "-q", "-m",
                        "chore(join): {0} - the join's audit entry, derived views, floors{1}".format(
                            args.audit_shortname, trailer)])

    gates = contract.get("gates")
    if gates is None:
        gates = [[PY, _sibling("run-verify-gates.py")]]
    for command in gates:
        j.run(8, "every verify gate", _interp(command))

    if args.no_push:
        log("\n== step 9: push skipped (--no-push)")
        state.skipped(9, "push", "--no-push")
    else:
        j.run(9, "push", ["git", "push", contract.get("push_remote") or "origin", branch_now])

    sha = j.git("rev-parse", "--short", "HEAD").stdout.strip()
    build = contract.get("build") or []
    if args.no_build or not build:
        why = "--no-build" if args.no_build else "none configured"
        log("\n== step 10: build skipped ({0}); {1} at {2}".format(why, branch_now, sha))
        state.skipped(10, "build", why)
    else:
        for command in build:
            j.run(10, "build", _interp(command))
        log("\nconductor-join: complete - {0} at {1}; build ran".format(branch_now, sha))
    # Only here - after the LAST step of the run returned. Every earlier exit, red or killed,
    # leaves the file without this key, which is the whole signal (DC-227).
    state.complete(status="ok", branch=branch_now, head=sha, pushed=not args.no_push,
                   built=bool(build) and not args.no_build)
    return 0


def self_test() -> int:
    """Joins in throwaway repositories, all with --no-push --docs-only:
    (a) a clean branch completes, and the audit entry carries tier T1, fan_out 0 and a
    measured duration, and the state file carries `complete`; (b) a branch that commits a file
    with a conflict marker - DC-136's shape, a hand-resolved file - stops at step 3 with NO
    join commit; (c) DC-227: a join KILLED at step 8 leaves a captured log that reached step 8
    and a state file with no `complete`, over a preserved earlier attempt."""
    def git(cwd, *a):
        done = subprocess.run(["git", *a], cwd=str(cwd), capture_output=True, text=True)
        if done.returncode != 0:
            raise AssertionError("git {0}: {1}".format(" ".join(a), done.stderr))
        return done.stdout

    problems = []
    with tempfile.TemporaryDirectory() as tmp:
        repo = Path(tmp) / "repo"
        repo.mkdir()
        git(tmp, "init", "-q", "-b", "main", str(repo))
        git(repo, "config", "user.email", "join@example.invalid")
        git(repo, "config", "user.name", "join")
        (repo / "docs" / "audit").mkdir(parents=True)
        (repo / ".agents").mkdir()
        (repo / "README.md").write_text("base\n", encoding="utf-8")
        git(repo, "add", "-A")
        git(repo, "commit", "-qm", "init")
        contract = {"regenerate": [], "gates": [["python3", "-c", "print('gates ok')"]]}
        (repo / "join.json").write_text(json.dumps(contract), encoding="utf-8")
        git(repo, "add", "-A")
        git(repo, "commit", "-qm", "join contract")

        # (a) the clean branch
        git(repo, "checkout", "-q", "-b", "feature/clean")
        (repo / "clean.txt").write_text("done\n", encoding="utf-8")
        git(repo, "add", "-A")
        git(repo, "commit", "-qm", "clean work")
        git(repo, "checkout", "-q", "main")
        lines = []
        args = _parser().parse_args(["feature/clean", "--title", "merge clean", "--audit-shortname",
                                     "join-clean", "--audit-summary", "s", "--audit-goal", "g",
                                     "--audit-done-when", "d", "--no-push", "--docs-only",
                                     "--join", str(repo / "join.json"), "--session", "selftest"])
        try:
            status = join(args, repo, contract, log=lines.append)
        except SystemExit as exc:
            status = exc.code
        if status != 0:
            problems.append("a clean join exited {0}: {1}".format(status, "\n".join(lines[-12:])))
        else:
            head = git(repo, "log", "-1", "--format=%s").strip()
            if not head.startswith("chore(join): join-clean"):
                problems.append("the join commit is not at HEAD ({0!r})".format(head))
            entries = [json.loads(l) for l in (repo / "docs" / "audit" / "audit-log.jsonl")
                       .read_text(encoding="utf-8").splitlines() if l.strip()]
            entry = entries[-1] if entries else {}
            if entry.get("tier") != "T1" or entry.get("fan_out") != 0:
                problems.append("the join entry lacks tier T1 / fan_out 0: {0}".format(entry))
            if "duration_seconds" not in entry:
                problems.append("the join entry carries no measured duration")
            if "recount_seconds=" not in entry.get("summary", ""):
                problems.append("the join entry does not record recount_seconds")
            state_path = repo / ".agents" / "joins" / "join-clean.json"
            if not state_path.is_file():
                problems.append("a clean join wrote no state file at .agents/joins/join-clean.json")
            elif not json.loads(state_path.read_text(encoding="utf-8")).get("complete"):
                problems.append("a clean join's state file carries no complete record: {0}".format(
                    state_path.read_text(encoding="utf-8")[:400]))

        # (b) the hand-resolved file carrying a marker
        git(repo, "checkout", "-q", "-b", "feature/markers")
        (repo / "resolved-by-hand.md").write_text(
            "before\n" + "<" * 7 + " HEAD\nmine\n=======\ntheirs\n" + ">" * 7 + " theirs\n",
            encoding="utf-8")
        git(repo, "add", "-A")
        git(repo, "commit", "-qm", "a file resolved by hand")
        git(repo, "checkout", "-q", "main")
        before = git(repo, "rev-parse", "HEAD").strip()
        lines = []
        args = _parser().parse_args(["feature/markers", "--title", "merge markers", "--audit-shortname",
                                     "join-markers", "--audit-summary", "s", "--audit-goal", "g",
                                     "--audit-done-when", "d", "--no-push", "--docs-only",
                                     "--join", str(repo / "join.json"), "--session", "selftest"])
        try:
            status = join(args, repo, contract, log=lines.append)
        except SystemExit as exc:
            status = exc.code
        if status != 3:
            problems.append("a merge carrying a conflict marker did not stop at step 3 (exit {0})".format(status))
        after = git(repo, "log", "-1", "--format=%s").strip()
        if after.startswith("chore(join)"):
            problems.append("a join commit was made over a conflict marker")
        if git(repo, "rev-parse", "HEAD").strip() == before:
            problems.append("precondition: the merge itself should have landed before the gate")

    # (c) DC-227 / Ruling 128 (b): a KILLED step. Its own repository and its own subprocess,
    # because the question is what an externally terminated process leaves behind, which an
    # in-process call cannot answer - the join must really die mid-step, as it did on
    # 2026-09-17, when two real joins were started under this harness's background-shell mode,
    # both captured logs ended at `== step 7: commit`, the harness reported `[exited with code
    # 0]` for each, verbatim, and nothing was pushed. Whether the process itself exited 0 is
    # NOT recorded. Here a child of the join terminates the join at step 8, and the run proves
    # both halves of the control: the captured log reaches the step that was really running,
    # and the state file left behind carries no `complete`. Run twice under one shortname, so
    # a second attempt is also shown not to erase the first.
    with tempfile.TemporaryDirectory() as tmp:
        repo = Path(tmp) / "repo"
        repo.mkdir()
        git(tmp, "init", "-q", "-b", "main", str(repo))
        git(repo, "config", "user.email", "join@example.invalid")
        git(repo, "config", "user.name", "join")
        (repo / "docs" / "audit").mkdir(parents=True)
        (repo / ".agents").mkdir()
        (repo / "README.md").write_text("base\n", encoding="utf-8")
        killer = {"regenerate": [], "checks": [], "gates": [
            ["python3", "-c", "import os, signal; os.kill(os.getppid(), signal.SIGTERM)"]]}
        (repo / "join.json").write_text(json.dumps(killer), encoding="utf-8")
        git(repo, "add", "-A")
        git(repo, "commit", "-qm", "init")

        killed = None
        for n in (1, 2):
            branch = "feature/killed-{0}".format(n)
            git(repo, "checkout", "-q", "-b", branch)
            (repo / "killed-{0}.txt".format(n)).write_text("work\n", encoding="utf-8")
            git(repo, "add", "-A")
            git(repo, "commit", "-qm", "work {0}".format(n))
            git(repo, "checkout", "-q", "main")
            killed = subprocess.run(
                [PY, str(Path(__file__).resolve()), branch, "--title", "merge killed",
                 "--audit-shortname", "join-killed", "--audit-summary", "s", "--audit-goal", "g",
                 "--audit-done-when", "d", "--no-push", "--docs-only",
                 "--join", str(repo / "join.json"), "--session", "selftest"],
                cwd=str(repo), capture_output=True, text=True, encoding="utf-8", errors="replace")
        if killed.returncode == 0:
            problems.append("the killed join exited 0 - the planted kill did not land")
        if "== step 8" not in (killed.stdout or ""):
            tail = (killed.stdout or "").strip().splitlines()[-1:]
            problems.append("a terminated join's captured log never reaches step 8 (last "
                            "captured line {0!r}) - the log is block-buffered".format(
                                tail[0] if tail else ""))
        joins_dir = repo / ".agents" / "joins"
        state_path = joins_dir / "join-killed.json"
        if not state_path.is_file():
            problems.append("a terminated join wrote no state file at .agents/joins/join-killed.json")
        else:
            state = json.loads(state_path.read_text(encoding="utf-8"))
            if "complete" in state:
                problems.append("a terminated join's state file carries a complete record")
            running = [s for s in state.get("steps", []) if s.get("status") == "running"]
            if not running or running[-1].get("step") != 8:
                problems.append("a terminated join's state file does not name step 8 as the "
                                "step that was running: {0}".format(state.get("steps")))
            first = state.get("previous")
            if not first or not (joins_dir / first).is_file():
                problems.append("a second run of the same shortname did not preserve the first "
                                "attempt (previous={0!r}, dir={1})".format(
                                    first, sorted(x.name for x in joins_dir.iterdir())))
            elif "complete" in json.loads((joins_dir / first).read_text(encoding="utf-8")):
                problems.append("the preserved first attempt carries a complete record")
    if problems:
        print("conductor-join --self-test: FAILED - " + "; ".join(problems))
        return 1
    print("conductor-join --self-test: OK - a clean join completes with a measured T1 entry "
          "and a state file carrying `complete`; a merge carrying a conflict marker stops at "
          "step 3 with no join commit; a join killed at step 8 leaves a log flushed to step 8 "
          "and a state file with no `complete`, over a preserved first attempt")
    return 0


def _parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0],
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("branch", nargs="?", help="the branch to merge (omit with --continue)")
    parser.add_argument("--title", default="", help="the merge commit's title (the trailer is appended)")
    parser.add_argument("--audit-shortname", help="the join's audit shortname")
    parser.add_argument("--audit-summary", default="join")
    parser.add_argument("--audit-goal", default="")
    parser.add_argument("--audit-done-when", default="")
    parser.add_argument("--artifact", action="append", default=[], help="proof paths for the audit entry")
    parser.add_argument("--docs-only", action="store_true", help="skip the recount (no test or product change)")
    parser.add_argument("--continue", dest="cont", action="store_true",
                        help="the merge was resolved by hand; start at step 2")
    parser.add_argument("--no-push", action="store_true")
    parser.add_argument("--no-build", action="store_true")
    parser.add_argument("--join", help="the join contract (default docs/coordination/join.json)")
    parser.add_argument("--session", help="audit session id (default $AGENT_SESSION, then join.json, then 'conductor')")
    parser.add_argument("--trailer-file", dest="trailer_file", help="text appended to every commit message")
    parser.add_argument("--self-test", action="store_true",
                        help="prove a red step stops the join and a killed one leaves no `complete`")
    return parser


def main(argv: list[str]) -> int:
    args = _parser().parse_args(argv)
    if args.self_test:
        return self_test()
    if not args.audit_shortname or not args.title:
        print("conductor-join: --title and --audit-shortname are required", file=sys.stderr)
        return 2
    root = repo_root()
    if root is None:
        print("conductor-join: not inside a git repository", file=sys.stderr)
        return 2
    contract = load_join(root, args.join)
    try:
        return join(args, root, contract)
    except SystemExit as exc:
        return int(exc.code or 0)


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
