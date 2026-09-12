#!/usr/bin/env python3
"""run-verify-gates.py — every tools/verify-*.py, one exit status.

The control for DC-113's third recurrence (2026-09-12, the conductor's join of SH-3): a shell loop
ran every gate, printed `FAIL rc=1 tools/verify-stranded-audit.py`, and the line went on to
`git commit && git push` because a `for` loop's exit status is its last command's, not the worst
one's. Twice before the fix was "chain with `&&`" — a rule the loop shape defeats by construction,
since there is no single status to chain on.

This runner IS that status. It runs each gate, prints one line per gate, and exits with the
number of failures capped at 1 — so `python tools/run-verify-gates.py && git commit …` cannot
commit red, and no loop is written by hand at a join again.

Usage
  python tools/run-verify-gates.py                 run every tools/verify-*.py
  python tools/run-verify-gates.py --skip NAME …   skip gates by basename (e.g. verify-test-run.py,
                                                    which needs its own arguments; see below)
  python tools/run-verify-gates.py --self-test     prove the runner can fail (DC-104)

Gates that take arguments are run in their argument-free form, which every gate here supports as
"check the repository"; `verify-test-run.py` is run as `--no-run` (it reads the last trx files) and
`verify-terminal-host-exit-paths.py` reads the same artifacts. A gate that is missing, cannot be
started, or runs past its 300 s budget counts as a failure, never as a skip.

Exit 0 when every gate passed, 1 when any failed, 2 on a usage error. Stdlib only.
"""
from __future__ import annotations

import argparse
import subprocess
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TOOLS = ROOT / "tools"
BUDGET_SECONDS = 300
EXTRA_ARGS = {
    "verify-test-run.py": ["--no-run"],
}


def gates() -> list[Path]:
    return sorted(p for p in TOOLS.glob("verify-*.py") if p.is_file())


def run_one(gate: Path) -> tuple[bool, float, str]:
    command = [sys.executable, str(gate), *EXTRA_ARGS.get(gate.name, [])]
    started = time.monotonic()
    try:
        completed = subprocess.run(
            command, cwd=ROOT, capture_output=True, text=True, encoding="utf-8",
            errors="replace", timeout=BUDGET_SECONDS)
    except subprocess.TimeoutExpired:
        return False, time.monotonic() - started, f"ran past {BUDGET_SECONDS} s"
    except OSError as error:
        return False, time.monotonic() - started, f"could not start: {error}"
    elapsed = time.monotonic() - started
    tail = (completed.stdout + completed.stderr).strip().splitlines()
    return completed.returncode == 0, elapsed, (tail[-1] if tail else "(no output)")


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--skip", nargs="*", default=[], metavar="NAME",
                        help="gate basenames to skip (each is printed as skipped, never silently)")
    parser.add_argument("--self-test", action="store_true",
                        help="prove the runner reports a failing gate as a failure")
    args = parser.parse_args(argv)

    if args.self_test:
        return self_test()

    selected = gates()
    if not selected:
        print("run-verify-gates: no tools/verify-*.py found", file=sys.stderr)
        return 2

    failures = 0
    for gate in selected:
        if gate.name in args.skip:
            print(f"  skip   {gate.name}")
            continue
        ok, elapsed, last = run_one(gate)
        failures += 0 if ok else 1
        print(f"  {'ok  ' if ok else 'FAIL'}   {gate.name:44} {elapsed:6.1f} s  {last[:110]}")

    ran = len(selected) - len([g for g in selected if g.name in args.skip])
    if failures:
        print(f"run-verify-gates: {failures} of {ran} gate(s) FAILED — the line stops here.")
        return 1
    print(f"run-verify-gates: OK — {ran} gate(s) passed.")
    return 0


def self_test() -> int:
    """A gate that exits 1 must make the runner exit 1; a gate that exits 0 must not."""
    import tempfile
    with tempfile.TemporaryDirectory() as tmp:
        red = Path(tmp) / "verify-red.py"
        red.write_text("import sys; print('red'); sys.exit(1)\n", encoding="utf-8")
        green = Path(tmp) / "verify-green.py"
        green.write_text("print('green')\n", encoding="utf-8")
        ok_red, _, _ = run_one(red)
        ok_green, _, _ = run_one(green)
    if ok_red or not ok_green:
        print("run-verify-gates --self-test: FAILED — a red gate was not reported red, or a green one was")
        return 1
    print("run-verify-gates --self-test: OK — a red gate is a failure, a green gate is not")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
