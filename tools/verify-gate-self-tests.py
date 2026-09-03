#!/usr/bin/env python3
"""A new gate must be able to prove it can fail. Existing debt is frozen, never grown.

The control for defect class DC-104: a new control's first run is evidence about TWO things — the
code and the control — and the likelier defect is in the control, which is also the one nobody looks
for, because the control is what they just reasoned about carefully. A `--self-test` is how a gate
demonstrates it can fail at all, and today four separate controls were wrong on their first run.

WHY A RATCHET AND NOT A RULE. Nine of this repository's gates predate the convention. Enforcing it
outright would fail the build on tools nobody is currently rewriting, which is the fastest way to get
a gate switched off — and DC-104's own asymmetry says a false positive on a push gate costs more than
the finding is worth. So existing gaps are FROZEN by name, and the check fails only when the debt
moves:

    a gate not on the list lacks a self-test   -> NEW debt, refused
    a gate on the list now HAS a self-test     -> STALE entry, must be removed

Both directions fail, which is what stops the list becoming the thing that needs maintaining. A
frozen list that may only shrink is a register that cannot rot; one that may only be appended to is
DC-103 with extra steps.

WHY NOT JUST A COUNT. A number goes green again if someone adds a self-test to one gate and a new
gate without one — the debt held constant while the newest, least-proven tool is the part missing its
proof. Names catch the swap; a count cannot.
"""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
from pathlib import Path

# A gate offers a self-test if it accepts the flag. Matching the argparse declaration rather than any
# mention, so a gate that merely discusses self-tests in its docstring does not count as having one.
OFFERS_SELF_TEST = re.compile(r'add_argument\(\s*["\']--self-test["\']')

GATES = ("verify-*.py", "mutation-replay.py")

# Gates that predate the convention. THIS LIST MAY ONLY SHRINK.
#
# Not a permission to skip the self-test: it is the debt, written down by name so that adding to it
# is a visible act rather than an omission nobody can see. Removing a name requires giving that gate
# a self-test, which is the only edit this check will accept.
KNOWN_WITHOUT_SELF_TEST = {
    "verify-audit-log.py",
    "verify-bounds-are-enforced.py",
    "verify-defect-register.py",
    "verify-embedded-scripts.py",
    "verify-extractor-generation.py",
    "verify-fixture-derivation.py",
    "verify-project-coverage.py",
    "verify-published-layout.py",
    "verify-site-figures.py",
    "verify-test-run.py",
}


def repo_root() -> Path:
    return Path(subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        capture_output=True, text=True, check=True).stdout.strip())


def gate_files(root: Path) -> list[Path]:
    found: list[Path] = []
    for pattern in GATES:
        found.extend((root / "tools").glob(pattern))

    # This gate is not its own subject: it has a self-test, but including it would make the check
    # depend on its own shape, and a control that verifies itself has verified nothing.
    return sorted(p for p in found if p.name != Path(__file__).name)


def check(root: Path, frozen: set[str] | None = None) -> tuple[list[str], int, int]:
    """
    The frozen set is a parameter so the self-test can exercise the STALE direction without editing
    a live gate. Verifying that direction by appending a self-test to a real tool would mean the
    check is only ever proven by a run that modifies the repository, which is a worse trade than one
    argument.
    """
    problems: list[str] = []
    frozen = KNOWN_WITHOUT_SELF_TEST if frozen is None else frozen
    gates = gate_files(root)

    if not gates:
        return (["no gates were found — this check examined nothing"], 0, 0)

    without = {
        p.name for p in gates
        if not OFFERS_SELF_TEST.search(p.read_text(encoding="utf-8", errors="replace"))
    }

    for name in sorted(without - frozen):
        problems.append(
            f"{name} is a gate with no --self-test, and is not on the frozen list. A control that "
            "has never been observed failing has not been observed (DC-104): four controls were "
            "wrong on their first run today, and each was found by making the code wrong and "
            "watching what stayed quiet. Add a --self-test that proves this gate fires.")

    for name in sorted(frozen - without):
        problems.append(
            f"{name} now HAS a --self-test but is still on the frozen list. Remove it: a debt list "
            "that keeps names it no longer owns stops describing the debt, and the next reader "
            "cannot tell which entries are real.")

    missing_files = frozen - {p.name for p in gates}
    for name in sorted(missing_files):
        problems.append(
            f"{name} is on the frozen list but is not a gate any more. Remove it — the list must "
            "describe tools that exist.")

    return (problems, len(gates), len(without))


def self_test(root: Path) -> int:
    """Prove BOTH directions fire.

    The first version proved only the new-debt direction. A ratchet shown to catch additions and
    never shown to catch stale entries is half a control, and the untested half is the one that lets
    the frozen list quietly stop describing the debt. Found by the concurrent session running the
    direction this function did not cover — DC-104 aimed at a self-test rather than at a gate.
    """
    # DIRECTION 1: new debt. A gate that cannot prove it can fail must be refused.
    intruder = root / "tools" / "verify-_selftest_probe.py"
    intruder.write_text(
        '"""A gate that cannot prove it can fail."""\n'
        "def main():\n"
        "    return 0\n",
        encoding="utf-8")

    try:
        problems, _, _ = check(root)
    finally:
        intruder.unlink(missing_ok=True)

    if not any("verify-_selftest_probe.py" in problem for problem in problems):
        print("self-test FAILED: a new gate with no --self-test was not reported", file=sys.stderr)
        return 1

    # DIRECTION 2: a stale entry. A gate that HAS a self-test must not stay on the frozen list.
    # Exercised by passing a frozen set that names a gate known to have one, rather than by appending
    # a self-test to a live tool — a control provable only by a run that edits the repository is a
    # control nobody will run.
    stale = "verify-cited-controls.py"

    if not OFFERS_SELF_TEST.search((root / "tools" / stale).read_text(encoding="utf-8")):
        print(f"self-test FAILED: {stale} was expected to have a --self-test", file=sys.stderr)
        return 1

    problems, _, _ = check(root, frozen={stale})

    if not any(stale in problem and "still on the frozen list" in problem for problem in problems):
        print("self-test FAILED: a stale frozen entry was not reported", file=sys.stderr)
        return 1

    print("self-test OK — new debt is refused, and a stale frozen entry is reported")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--self-test", action="store_true",
                        help="prove both directions fire: new debt refused, stale frozen entry reported")
    args = parser.parse_args()

    root = repo_root()

    if args.self_test:
        return self_test(root)

    problems, gates, without = check(root)

    if problems:
        print("verify-gate-self-tests: FAILED")
        for problem in problems:
            print(f"  - {problem}")
        return 1

    print(f"verify-gate-self-tests: OK — {gates} gate(s), {without} without a self-test, "
          "all frozen and none added.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
