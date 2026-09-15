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

(1) is the class. (2) is the hole (1) left, and it cost a real entry: resolving a merge by unioning
keyed on id silently dropped one side, and THIS GATE STAYED GREEN — because uniqueness was satisfied
precisely by the removal. A control that only counts duplicates cannot see a deletion, and an
append-only log has no legitimate reason to shrink (DC-026). The rest are cheap neighbours worth
having while the file is open.

Usage
  python tools/verify-audit-log.py                     check the committed logs
  python tools/verify-audit-log.py <file> [<file> ...] check specific files (used to observe it red)

Exit 0 clean, 1 on any finding.
"""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
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
        if line.strip() and _parses(line)
    }))

    return findings


def _parses(line: str) -> bool:
    try:
        json.loads(line)
        return True
    except json.JSONDecodeError:
        return False


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


def _fixture_command(args: list[str], root: Path) -> subprocess.CompletedProcess[str]:
    environment = os.environ.copy()
    environment["GIT_CONFIG_GLOBAL"] = os.devnull
    environment["GIT_CONFIG_NOSYSTEM"] = "1"
    return subprocess.run(args, cwd=root, capture_output=True, text=True, encoding="utf-8",
                          errors="replace", timeout=30, check=False, env=environment)


def self_test() -> int:
    """Exercise the real CLI against isolated files and a real local Git HEAD."""
    failures: list[str] = []
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

    def run_gate(root: Path, *arguments: str,
                 environment: dict[str, str] | None = None) -> subprocess.CompletedProcess[str]:
        child_environment = os.environ.copy()
        if environment:
            child_environment.update(environment)
        child_environment["GIT_CONFIG_GLOBAL"] = os.devnull
        child_environment["GIT_CONFIG_NOSYSTEM"] = "1"
        return subprocess.run(
            [sys.executable, str(root / "tools" / "verify-audit-log.py"), *arguments],
            cwd=root, capture_output=True, text=True, encoding="utf-8", errors="replace",
            timeout=30, check=False, env=child_environment)

    def observe(case: str, result: subprocess.CompletedProcess[str], expected_exit: int,
                required: tuple[str, ...]) -> None:
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
        write_log(change, baseline_change)

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

    if not os.environ.get("AUDIT_GATE_MUTANT"):
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
            "normal CLI forced green": (
                "    if findings:\n        print(\"verify-audit-log: FAILED\")",
                "    if False and findings:\n        print(\"verify-audit-log: FAILED\")",
                "duplicate id rejected"),
            "normal CLI forced failure": (
                '          "claimed by exactly one entry.\")\n    return 0\n\n\ndef _audit_next_id',
                '          "claimed by exactly one entry.\")\n    return 1\n\n\ndef _audit_next_id',
                "valid default logs accepted"),
        }
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
                result = run_gate(mutant_root, "--self-test",
                                  environment={"AUDIT_GATE_MUTANT": "1"})
                output = result.stdout + result.stderr
                marker = f"SELF-TEST CASE FAILED [{killing_case}]"
                require(mutant_name, result.returncode != 0,
                        f"mutant survived with exit 0; output={output!r}")
                require(mutant_name, marker in output,
                        f"named killing oracle {marker!r} did not fail; output={output!r}")
                require(mutant_name, "Traceback" not in output,
                        f"mutant produced a traceback instead of a named oracle; output={output!r}")
                if result.returncode != 0 and marker in output and "Traceback" not in output:
                    print(f"mutant rejected: {mutant_name} -> {killing_case} (exit {result.returncode})")

    if failures:
        print("verify-audit-log: SELF-TEST FAILED", file=sys.stderr)
        for failure in failures:
            print(f"  - {failure}", file=sys.stderr)
        return 1

    print("verify-audit-log: self-test OK — 9 fixed Git/CLI cases and 7 semantic mutants; "
          f"source sha256 {source_hash}")
    return 0


def dispatch(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(add_help=False)
    parser.add_argument("--self-test", action="store_true")
    arguments, paths = parser.parse_known_args(argv)
    if arguments.self_test:
        if paths:
            print("verify-audit-log: --self-test does not accept log paths", file=sys.stderr)
            return 2
        return self_test()
    return main(paths)


if __name__ == "__main__":
    sys.exit(dispatch(sys.argv[1:]))
