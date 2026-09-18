#!/usr/bin/env python3
"""verify-audit-log.py — catch two records that claim the same audit id.

The control for defect class DC-013.

The audit and change logs allocate ids by reading the highest one present and adding one. That is
correct in a single checkout and wrong the moment there are two, which is the normal state of this
repo: worktree discipline says a session that writes gets its own tree, so two trees routinely hold
the same `al-NNNN` as their highest entry and both hand it to the next writer. Neither notices. The
collision surfaces later as a merge conflict, or — worse — as a clean append-only merge in which two
unrelated records share an id and one silently wins the lookup.

It has happened twice in this repo. Both times it was resolved by discarding one entry and
re-logging it, which is the right fix and leaves no trace that would stop the third time.

This checks, for each log:

  1. no id appears twice
  2. no id present in the committed version has DISAPPEARED
  3. ids parse as <prefix>-<number> or <prefix>-<ULID> (the pack rev-59 allocator)
  4. every line is valid JSON with an id at all
  5. a log git TRACKS has not gone missing from the working tree
  6. every line that decodes is a JSON OBJECT, and the scan continues past one that is not

(1) is the class. (2) is the hole (1) left, and it cost a real entry: resolving a merge by unioning
keyed on id silently dropped one side, and THIS GATE STAYED GREEN — because uniqueness was satisfied
precisely by the removal. A control that only counts duplicates cannot see a deletion, and an
append-only log has no legitimate reason to shrink (DC-026). The rest are cheap neighbours worth
having while the file is open.

(5) and (6) are the two findings Codex made while giving this file its --self-test and correctly kept
out of that scope; the Owner admitted them as policy in Ruling 125 (iii). They are the same defect at
two scales. (2) refuses the loss of a LINE from a committed log; until (5) the loss of the whole FILE
read as "OK", because absence was accepted without asking whether git was tracking it — so the one
state where every entry is gone was the one state the gate called clean. (6) is the shape guard (2)
and (4) both assumed: a line may be valid JSON and still not be a record, and `[1, 2, 3]` reached
`entry.get` and took the whole scan down with an AttributeError — one stray paste hiding every
finding after it, including a duplicate id.

Usage
  python tools/verify-audit-log.py                     check the committed logs
  python tools/verify-audit-log.py <file> [<file> ...] check specific files (used to observe it red)

Exit 0 clean, 1 on any finding.
"""
from __future__ import annotations

import argparse
import contextlib
import hashlib
import importlib.util
import io
import json
import os
import re
import subprocess
import sys
import tempfile
from collections import Counter
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
DEFAULT_LOGS = [
    REPO / "docs" / "audit" / "audit-log.jsonl",
    REPO / "docs" / "audit" / "change-log.jsonl",
]
# Two id shapes are legitimate, and the second one arrived with pack revision 59.
#
#   al-0449                       the sequential allocator
#   al-01M1MYWGG050BEVR42EHRC7FBZ the pack's coord_ids.py ULID allocator
#
# The pack moved to ULIDs to stop two trees allocating the SAME id independently — which was not a
# hypothetical here: docs/adr carried four collisions (adr-0017..0020, each claimed by two unrelated
# decisions) from exactly that, and they failed this repository's own allocator gate on main until
# they were re-issued to adr-0023..0026 on 2026-09-05 (session-contracts §4o). The cost was 237
# citations disambiguated first, because renaming ahead of that turns an ambiguous reference into a
# confidently wrong one.
# A sequential allocator cannot be made safe across concurrent worktrees; a ULID cannot collide.
#
# Both are accepted rather than switching, because the log is append-only: 435 existing entries carry
# the sequential form and rewriting them to satisfy a format rule would be editing history to please
# a checker.
ID = re.compile(r"^([a-z]+)-(\d+|[0-9A-HJKMNP-TV-Z]{26})$")

# The remedy for a line that decodes and is still not a record. Named here rather than written inline
# so the refusal reads the same wherever it is raised, and so the mutation table has ONE append to
# disable when it proves this guard can fail.
NOT_AN_OBJECT = (
    "a JSONL log is one JSON OBJECT per line. A bare array, string or number decodes cleanly and "
    "then reaches entry.get, which raises AttributeError and takes the whole scan down instead of "
    "reporting one line. Re-write it as an object carrying an id, or delete it if it was a stray "
    "paste — the scan CONTINUES past this line, so any further finding in this file is reported in "
    "the same run."
)

# Windows consoles default to cp1252 and cannot encode the glyphs below.
for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass


def committed_ids(path: Path) -> set[str] | None:
    """The ids in HEAD's version of this file, or None when git cannot say."""
    import subprocess

    relative = path.relative_to(REPO).as_posix()

    try:
        result = subprocess.run(["git", "show", f"HEAD:{relative}"],
                                cwd=REPO, capture_output=True, timeout=30, check=False)
    except (OSError, subprocess.SubprocessError):
        return None

    if result.returncode != 0:
        return None                      # new file, or no git: nothing to compare against

    found: set[str] = set()
    for line in result.stdout.decode("utf-8", "replace").splitlines():
        if not line.strip():
            continue
        try:
            identifier = json.loads(line).get("id")
        except json.JSONDecodeError:
            continue
        if identifier:
            found.add(str(identifier))

    return found


def is_tracked(path: Path) -> bool:
    """Does git have this path in the index?

    The question the absence branch below never asked. `git ls-files` is the only thing that can
    tell an optional log that has never existed from a committed log that has DISAPPEARED, and the
    two deserve opposite answers: the first is how a young project looks, the second is the entire
    file's worth of the deletion this gate already refuses line by line.

    A path outside the repository, or no git at all, is reported untracked — the same fail-soft as
    `committed_ids`: a gate that cannot consult git degrades to the behaviour it had before this
    check existed, and never to a confident finding it cannot support (IO12).
    """
    try:
        # ABSOLUTE, because git runs below with cwd=REPO: a relative argument would be resolved
        # against the repo root here and against the CALLER's cwd by path.exists(), so the two
        # halves of this decision could be about two different files.
        result = subprocess.run(["git", "ls-files", "--error-unmatch", "--", str(path.resolve())],
                                cwd=REPO, capture_output=True, timeout=30, check=False)
    except (OSError, subprocess.SubprocessError):
        return False

    return result.returncode == 0


def check_no_entry_vanished(path: Path, present: set[str]) -> list[str]:
    """
    An append-only log may grow. It may not shrink.

    Compared against HEAD rather than against a stored count, because the question is "did this
    working copy lose something that was committed", and HEAD is the only thing that knows.
    """
    was = committed_ids(path)
    if was is None:
        return []

    gone = sorted(was - present)
    if not gone:
        return []

    return [
        f"{path.name}: {len(gone)} id(s) present in HEAD are missing here: "
        + ", ".join(gone[:8]) + ("…" if len(gone) > 8 else "")
        + " — an append-only log does not shrink. A merge resolved by de-duplicating on id drops "
          "one side silently (DC-026); use tools/merge-append-only-log.py."
    ]


def check(path: Path) -> list[str]:
    if not path.exists():
        # TRACKED AND ABSENT IS A FINDING (Ruling 125 (iii)). A committed log that is gone from the
        # working tree is not an absence, it is a deletion — every entry it carried at HEAD is
        # missing at once, which is check_no_entry_vanished's whole subject arriving as one file
        # rather than one line. Before this branch that state printed "(absent)" and the gate exited
        # 0, so the loudest possible version of DC-026 was the one reading it could not see.
        if is_tracked(path):
            print(f"{path.name:<24} (absent — TRACKED)")
            return [
                f"{path.name}: git tracks this log but it is absent from the working tree — every "
                "id it carried at HEAD is missing at once (DC-026). Restore it in THIS tree "
                f"(`git restore -- {_display(path)}`, or `git checkout -- {_display(path)}`) before "
                "anything appends to a fresh one, and check no session has already written entries "
                "into a replacement. If the log was retired deliberately, remove it from the index "
                "in the same commit so it is untracked and this line reads (absent) again."
            ]

        # A missing change log is normal early in a project; a missing audit log is not, but that is
        # the Audit Mandate's business rather than this gate's.
        print(f"{path.name:<24} (absent)")
        return []

    findings: list[str] = []
    ids: list[str] = []

    for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
        if not line.strip():
            continue
        try:
            entry = json.loads(line)
        except json.JSONDecodeError as error:
            findings.append(f"{path.name}:{number}: not valid JSON — {error.msg}")
            continue

        # A SHAPE GUARD BEFORE entry.get. json.loads happily returns a list, a string or a
        # number, none of which has .get — so without this the next line raises and the scan dies
        # at the first stray paste, reporting nothing at all about a file that may also hold a
        # duplicate id. Reported and CONTINUED, never raised: one bad line is one finding.
        if not isinstance(entry, dict):
            findings.append(f"{path.name}:{number}: not an object — {NOT_AN_OBJECT}")
            continue

        identifier = entry.get("id")
        if not identifier:
            findings.append(f"{path.name}:{number}: entry has no id")
            continue

        if not ID.match(str(identifier)):
            findings.append(
                f"{path.name}:{number}: id '{identifier}' is neither <prefix>-<number> nor "
                "<prefix>-<ULID>")

        ids.append(str(identifier))

    duplicates = {i: n for i, n in Counter(ids).items() if n > 1}
    for identifier, count in sorted(duplicates.items()):
        findings.append(
            f"{path.name}: id '{identifier}' is claimed by {count} entries — two trees allocated it "
            f"independently. Renumber the later one; do not merge two records under one id.")

    print(f"{path.name:<24} {len(ids):>4} entries, {len(duplicates)} duplicate id(s)")
    findings.extend(check_no_entry_vanished(path, {
        str(json.loads(line).get("id"))
        for line in path.read_text(encoding="utf-8").splitlines()
        if line.strip() and _is_record(line)
    }))

    return findings


def _is_record(line: str) -> bool:
    """Valid JSON AND an object. The second half is not decoration: this predicate guards a `.get`."""
    try:
        return isinstance(json.loads(line), dict)
    except json.JSONDecodeError:
        return False


def _display(path: Path) -> str:
    """The path as an operator would type it: repo-relative when it is inside the repo."""
    try:
        return path.resolve().relative_to(REPO).as_posix()
    except ValueError:
        return str(path)


def main(argv: list[str]) -> int:
    logs = [Path(a) for a in argv] if argv else DEFAULT_LOGS

    findings: list[str] = []
    counted = 0
    for path in logs:
        findings.extend(check(path))

        # PRINT THE CARDINALITY, NOT JUST THE VERDICT. "OK" and "OK — 347 entries" fail identically
        # and differ completely: only the second can be contradicted by a later run, and being
        # contradicted is how every blind spot found today surfaced. A gate that prints only OK
        # gives the next run nothing to disagree with.
        if path.exists():
            counted += sum(1 for line in path.read_text(encoding="utf-8", errors="replace").splitlines()
                           if line.strip())

    print()
    if findings:
        print("verify-audit-log: FAILED")
        for finding in findings:
            print(f"  - {finding}")
        return 1

    print(f"verify-audit-log: OK — {counted:,} entr(ies) across {len(logs)} log(s); every id is "
          "claimed by exactly one entry.")
    return 0


def _audit_next_id():
    """Load the installed audit writer's allocator contract without copying it."""
    script = REPO / "docs" / "ai-forward-pack" / "scripts" / "audit-log.py"
    spec = importlib.util.spec_from_file_location("auditlog_self_test", script)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"cannot load allocator from {script}")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module.next_id


def _fixture_environment() -> dict[str, str]:
    """Remove every repository-local variable reported by the installed Git."""
    environment = os.environ.copy()
    local_variables = subprocess.run(
        ["git", "rev-parse", "--local-env-vars"], capture_output=True, text=True,
        encoding="utf-8", errors="replace", timeout=30, check=False, env=environment)
    if local_variables.returncode != 0:
        raise RuntimeError(
            "git rev-parse --local-env-vars failed: "
            + local_variables.stderr.strip())
    for variable in local_variables.stdout.splitlines():
        environment.pop(variable.strip(), None)
    environment["GIT_CONFIG_GLOBAL"] = os.devnull
    environment["GIT_CONFIG_NOSYSTEM"] = "1"
    return environment


def _fixture_command(args: list[str], root: Path) -> subprocess.CompletedProcess[str]:
    return subprocess.run(args, cwd=root, capture_output=True, text=True, encoding="utf-8",
                          errors="replace", timeout=30, check=False,
                          env=_fixture_environment())


def _self_test(run_mutants: bool) -> int:
    """Exercise the real CLI against isolated files and a real local Git HEAD."""
    failures: list[str] = []
    cases_completed = 0
    mutants_completed = 0
    source_path = Path(__file__).resolve()
    source = source_path.read_bytes()
    source_text = source.decode("utf-8")
    source_hash = hashlib.sha256(source).hexdigest()

    def require(case: str, condition: bool, detail: str) -> None:
        if not condition:
            failures.append(f"SELF-TEST CASE FAILED [{case}]: {detail}")

    def git(root: Path, *args: str) -> subprocess.CompletedProcess[str]:
        hooks = root / ".no-hooks"
        hooks.mkdir(exist_ok=True)
        return _fixture_command([
            "git", "-c", "user.name=Audit Gate Self-Test",
            "-c", "user.email=audit-gate-self-test.invalid",
            "-c", "commit.gpgsign=false", "-c", f"core.hooksPath={hooks}", *args,
        ], root)

    def write_log(path: Path, entries: list[object] | None) -> None:
        if entries is None:
            path.unlink(missing_ok=True)
            return
        path.write_text("".join(
            (entry if isinstance(entry, str) else json.dumps(entry, sort_keys=True)) + "\n"
            for entry in entries), encoding="utf-8")

    def run_gate(root: Path, *arguments: str) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            [sys.executable, str(root / "tools" / "verify-audit-log.py"), *arguments],
            cwd=root, capture_output=True, text=True, encoding="utf-8", errors="replace",
            timeout=30, check=False, env=_fixture_environment())

    def observe(case: str, result: subprocess.CompletedProcess[str], expected_exit: int,
                required: tuple[str, ...]) -> None:
        nonlocal cases_completed
        cases_completed += 1
        output = result.stdout + result.stderr
        require(case, result.returncode == expected_exit,
                f"expected exit {expected_exit}, got {result.returncode}; output={output!r}")
        for fragment in required:
            require(case, fragment in output,
                    f"missing diagnostic {fragment!r}; exit={result.returncode}; output={output!r}")
        require(case, "Traceback" not in output, f"unexpected traceback; output={output!r}")

    try:
        next_id = _audit_next_id()
        ulid = next_id([], "al")
        require("allocator-minted ULID", bool(ID.fullmatch(ulid)) and len(ulid) == 29,
                f"canonical next_id returned {ulid!r}")
    except Exception as error:
        print(f"verify-audit-log: SELF-TEST FAILED — allocator setup: {error}", file=sys.stderr)
        return 1

    with tempfile.TemporaryDirectory(prefix="verify-audit-log-") as directory:
        root = Path(directory)
        gate = root / "tools" / "verify-audit-log.py"
        audit = root / "docs" / "audit" / "audit-log.jsonl"
        change = root / "docs" / "audit" / "change-log.jsonl"
        gate.parent.mkdir(parents=True)
        audit.parent.mkdir(parents=True)
        gate.write_bytes(source)
        require("byte-identical verifier copy", gate.read_bytes() == source,
                "fixture verifier differs from the tested source")

        baseline_audit = [{"id": "al-0001"}, {"id": ulid}]
        baseline_change = [{"id": "cl-0001"}]
        write_log(audit, baseline_audit)

        # The change log is deliberately NOT in the baseline commit. "A missing optional log is
        # accepted" means precisely "a missing UNTRACKED log is accepted": once a TRACKED log that
        # has gone missing is a finding, a fixture that commits the change log and then deletes it
        # would be asserting the opposite of the policy it is meant to prove. Committing only the
        # audit log gives the suite both shapes — one tracked log to delete (a finding) and one
        # untracked log to delete (the "(absent)" line, unchanged).
        setup = [git(root, "init"), git(root, "add", "tools", "docs"),
                 git(root, "commit", "--no-gpg-sign", "-m", "fixture baseline")]
        setup_output = "".join(result.stdout + result.stderr for result in setup)
        if any(result.returncode != 0 for result in setup):
            print("verify-audit-log: SELF-TEST FAILED — Git fixture setup\n" + setup_output,
                  file=sys.stderr)
            return 1

        write_log(audit, baseline_audit)
        write_log(change, baseline_change)
        observe("valid default logs accepted", run_gate(root), 0,
                ("OK — 3 entr(ies) across 2 log(s)",))

        write_log(audit, baseline_audit + [{"id": "al-0002"}])
        write_log(change, baseline_change)
        observe("unique append accepted", run_gate(root), 0,
                ("OK — 4 entr(ies) across 2 log(s)",))

        write_log(audit, list(reversed(baseline_audit)))
        write_log(change, baseline_change)
        observe("reversed valid explicit log accepted", run_gate(root, str(audit)), 0,
                ("OK — 2 entr(ies) across 1 log(s)",))

        write_log(audit, baseline_audit)
        write_log(change, None)
        observe("missing optional log accepted", run_gate(root), 0,
                ("change-log.jsonl", "(absent)", "OK — 2 entr(ies) across 2 log(s)"))

        observe("--self remains a positional path", run_gate(root, "--self"), 0,
                ("--self", "(absent)", "OK — 0 entr(ies) across 1 log(s)"))

        observe("--help remains a positional path", run_gate(root, "--help"), 0,
                ("--help", "(absent)", "OK — 0 entr(ies) across 1 log(s)"))

        write_log(audit, baseline_audit + [{"id": "al-0001"}])
        observe("duplicate id rejected", run_gate(root, str(audit)), 1,
                ("id 'al-0001' is claimed by 2 entries",))

        write_log(audit, [baseline_audit[1]])
        observe("committed id deletion rejected", run_gate(root, str(audit)), 1,
                ("present in HEAD are missing here: al-0001",))

        write_log(audit, baseline_audit + ["{broken"])
        observe("malformed JSON rejected", run_gate(root, str(audit)), 1,
                ("audit-log.jsonl:3: not valid JSON",))

        write_log(audit, baseline_audit + [{}])
        observe("missing id rejected", run_gate(root, str(audit)), 1,
                ("audit-log.jsonl:3: entry has no id",))

        write_log(audit, baseline_audit + [{"id": "bad id"}])
        observe("invalid id rejected", run_gate(root, str(audit)), 1,
                ("id 'bad id' is neither <prefix>-<number> nor <prefix>-<ULID>",))

        # A line that decodes to something that is not a record. The SECOND diagnostic is the point:
        # the scan must CONTINUE past the bad line, so the invalid id on the following line is still
        # reported. A guard that returned early would hide every finding after the first paste error.
        write_log(audit, baseline_audit + ["[1, 2, 3]", {"id": "bad id"}])
        observe("non-object line rejected", run_gate(root, str(audit)), 1,
                ("audit-log.jsonl:3: not an object",
                 "id 'bad id' is neither <prefix>-<number> nor <prefix>-<ULID>"))

        # The tracked log has gone missing. Committed, then absent: the file that carries the work.
        write_log(audit, None)
        observe("tracked absent log rejected", run_gate(root, str(audit)), 1,
                ("git tracks this log but it is absent from the working tree",
                 "git restore -- docs/audit/audit-log.jsonl"))

    mutations = {
            "duplicate guard disabled": (
                "duplicates = " + "{i: n for i, n in Counter(ids).items() if n > 1}",
                "duplicates = " + "{}", "duplicate id rejected"),
            "deletion guard disabled": (
                "gone = " + "sorted(was - present)", "gone = " + "[]",
                "committed id deletion rejected"),
            "malformed JSON guard disabled": (
                "            findings.append(" +
                'f"{path.name}:{number}: not valid JSON — {error.msg}")',
                "            if False:\n                findings.append(" +
                'f"{path.name}:{number}: not valid JSON — {error.msg}")',
                "malformed JSON rejected"),
            "missing-id guard disabled": (
                "            findings.append(" + 'f"{path.name}:{number}: entry has no id")',
                "            if False:\n                findings.append(" +
                'f"{path.name}:{number}: entry has no id")',
                "missing id rejected"),
            "invalid-id guard disabled": (
                "if not ID.match(" + "str(identifier)):",
                "if False and not ID.match(" + "str(identifier)):",
                "invalid id rejected"),
            "tracked-absent guard disabled": (
                "        if " + "is_tracked(path):",
                "        if " + "False and is_tracked(path):",
                "tracked absent log rejected"),
            "non-object guard disabled": (
                "            findings.append(" +
                'f"{path.name}:{number}: not an object — {NOT_AN_OBJECT}")',
                "            if False:\n                findings.append(" +
                'f"{path.name}:{number}: not an object — {NOT_AN_OBJECT}")',
                "non-object line rejected"),
            "normal CLI forced green": (
                "    if findings:\n        print(\"verify-audit-log: FAILED\")",
                "    if False and findings:\n        print(\"verify-audit-log: FAILED\")",
                "duplicate id rejected"),
            "normal CLI forced failure": (
                '          "claimed by exactly one entry.\")\n    return 0\n\n\ndef _audit_next_id',
                '          "claimed by exactly one entry.\")\n    return 1\n\n\ndef _audit_next_id',
                "valid default logs accepted"),
        }
    if run_mutants:
        for mutant_name, (old, new, killing_case) in mutations.items():
            count = source_text.count(old)
            require(f"{mutant_name} mutation applied exactly once", count == 1,
                    f"target occurred {count} time(s)")
            if count != 1:
                continue
            with tempfile.TemporaryDirectory(prefix="verify-audit-log-mutant-") as directory:
                mutant_root = Path(directory)
                mutant_gate = mutant_root / "tools" / "verify-audit-log.py"
                scripts = mutant_root / "docs" / "ai-forward-pack" / "scripts"
                mutant_gate.parent.mkdir(parents=True)
                scripts.mkdir(parents=True)
                mutant_gate.write_text(source_text.replace(old, new, 1), encoding="utf-8")
                for name in ("audit-log.py", "coord_ids.py"):
                    installed = REPO / "docs" / "ai-forward-pack" / "scripts" / name
                    (scripts / name).write_bytes(installed.read_bytes())
                output_stream = io.StringIO()
                try:
                    spec = importlib.util.spec_from_file_location(
                        f"audit_gate_mutant_{mutants_completed}", mutant_gate)
                    if spec is None or spec.loader is None:
                        raise RuntimeError(f"cannot load mutant module from {mutant_gate}")
                    module = importlib.util.module_from_spec(spec)
                    with contextlib.redirect_stdout(output_stream), \
                            contextlib.redirect_stderr(output_stream):
                        spec.loader.exec_module(module)
                        result_code = module.self_test(False)
                    output = output_stream.getvalue()
                except Exception as error:
                    result_code = -1
                    output = output_stream.getvalue() + f"mutant module error: {error!r}"
                mutants_completed += 1
                marker = f"SELF-TEST CASE FAILED [{killing_case}]"
                require(mutant_name, result_code != 0,
                        f"mutant survived with exit 0; output={output!r}")
                require(mutant_name, marker in output,
                        f"named killing oracle {marker!r} did not fail; output={output!r}")
                require(mutant_name, "Traceback" not in output,
                        f"mutant produced a traceback instead of a named oracle; output={output!r}")
                if result_code != 0 and marker in output and "Traceback" not in output:
                    print(f"mutant rejected: {mutant_name} -> {killing_case} (exit {result_code})")

    require("all semantic mutants executed",
            not run_mutants or mutants_completed == len(mutations),
            f"completed {mutants_completed} of {len(mutations)} configured mutants")

    if failures:
        print("verify-audit-log: SELF-TEST FAILED", file=sys.stderr)
        for failure in failures:
            print(f"  - {failure}", file=sys.stderr)
        return 1

    print(f"verify-audit-log: self-test OK — {cases_completed} fixed Git/CLI cases and "
          f"{mutants_completed} semantic mutants; source sha256 {source_hash}")
    return 0


def self_test(run_mutants: bool = True) -> int:
    """Run full mutation proof by default; the private false path serves loaded mutants."""
    if not run_mutants:
        return _self_test(False)

    marker = object()
    inherited = os.environ.get("AUDIT_GATE_MUTANT", marker)
    os.environ["AUDIT_GATE_MUTANT"] = "inherited-state-regression"
    try:
        return _self_test(True)
    finally:
        if inherited is marker:
            os.environ.pop("AUDIT_GATE_MUTANT", None)
        else:
            os.environ["AUDIT_GATE_MUTANT"] = inherited


def dispatch(argv: list[str]) -> int:
    if argv == ["--self-test"]:
        parser = argparse.ArgumentParser(add_help=False, allow_abbrev=False)
        parser.add_argument("--self-test", action="store_true")
        parser.parse_args(argv)
        return self_test()
    return main(argv)


if __name__ == "__main__":
    sys.exit(dispatch(sys.argv[1:]))
