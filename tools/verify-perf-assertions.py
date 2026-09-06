#!/usr/bin/env python3
"""A test may not assert that a measured duration is under a constant.

THE CONTROL FOR DEFECT CLASS DC-107.

`TerminalViewTests` asserted `p95 < 16.67` — a frame budget taken on a developer workstation. The CI
runner is roughly 3x slower, so the correct draw path measured 17-23 ms there and the test failed
deterministically on hardware while the code was right. It stayed red for 38 consecutive runs and,
because it was ordered ahead of them, took 26 other gates down with it (INV-0005).

WHY THE THRESHOLD WAS NOT THE PROBLEM. The signal that test wanted was architectural and enormous:
GlyphRun-per-line at 6.64 ms against FormattedText-per-cell at 142.80 ms, a 21x difference. The
budget sat at 2.5x the good value while the spread between the machines it runs on is ~3x. The noise
was wider than the guard band, so the assertion could never reach its own subject. Raising the
constant would have widened the window until it caught nothing; lowering it reddens honest hardware.
The fix is not a better number, it is a comparison that carries its own baseline.

WHAT IS CHECKED. A relational comparison, inside an assertion, between something that is plainly a
wall-clock measurement and something that is plainly a compile-time constant.

    Assert.True(p95 < FrameBudgetMs)              <- rejected: the verdict depends on the host
    Assert.True(perLine * 5 < perCell)            <- fine: both sides measured on the same host
    Assert.True(process.WaitForExit(180_000))     <- fine: a hang guard, not a speed claim

The distinction between the last two and the first is deliberate and narrow. A TIMEOUT is an argument
to a call and asserts that something finished; a BUDGET is a relational operator asserting that
something was fast. Only the second makes the machine part of the verdict, and this repository's two
existing wall-clock assertions are both the first kind.

THE ESCAPE HATCH IS A MARKER, NOT AN EXCEPTION LIST. A genuine absolute budget - one pinned to known
hardware, or a bound so loose no runner could reach it - is declared in place:

    // perf-budget: <why an absolute number is right here, and on what hardware it holds>

which puts the justification next to the number instead of in a gate nobody reads, and makes the
deliberate cases greppable.

Exit 0 when clean, 1 on any finding.
"""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
from pathlib import Path

# Plainly a wall-clock measurement.
CLOCK = re.compile(
    r"\b(?:[Ee]lapsed\w*|TotalMilliseconds|TotalSeconds|\w*[Pp]95\w*|\w*Millis\w*|"
    r"\w*Duration\w*|\w*Latency\w*)\b")

# Plainly a compile-time number: a literal, or a const declared in the same file.
LITERAL = re.compile(r"^\s*[-+]?\d[\d_]*(?:\.\d+)?(?:[dfmDFM])?\s*$")
CONST_DECL = re.compile(r"\bconst\s+(?:double|float|int|long|decimal)\s+(\w+)\s*=")

ASSERT = re.compile(r"\bAssert\.(?:True|False)\s*\(")
COMPARISON = re.compile(r"([^<>=!]+?)\s*(<=|>=|<|>)\s*([^<>=!,]+)")

MARKER = "perf-budget:"

# This file necessarily contains the shapes it hunts, in its own prose and its self-test.
SELF = "tools/verify-perf-assertions.py"


def repo_root() -> Path:
    return Path(subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        capture_output=True, text=True, check=True).stdout.strip())


def assertion_spans(text: str) -> list[tuple[int, str]]:
    """Every Assert.True/False call, as (line number, argument text). Paren-balanced."""
    spans = []

    for match in ASSERT.finditer(text):
        depth, i = 1, match.end()
        while i < len(text) and depth:
            if text[i] == "(":
                depth += 1
            elif text[i] == ")":
                depth -= 1
            i += 1
        spans.append((text.count("\n", 0, match.start()) + 1, text[match.end():i - 1]))

    return spans


def scan_file(relative: str, text: str) -> list[str]:
    consts = set(CONST_DECL.findall(text))
    lines = text.splitlines()
    findings = []

    def constant(side: str) -> bool:
        side = side.strip().rstrip(")").strip()
        return bool(LITERAL.match(side)) or side in consts

    for line_no, argument in assertion_spans(text):
        # The marker is looked for just above the assertion, where a reader would put it.
        window = "\n".join(lines[max(0, line_no - 6):line_no])
        if MARKER in window or MARKER in argument:
            continue

        for left, op, right in COMPARISON.findall(argument):
            measured_left, measured_right = CLOCK.search(left), CLOCK.search(right)

            if not (measured_left or measured_right):
                continue

            # A budget is measured-vs-constant. Measured-vs-measured is the shape we want people to
            # write, and constant-vs-constant is not about time at all.
            #
            # AND ONLY AN UPPER BOUND. `measured < K` fails when the host is slow, which is DC-107.
            # `measured >= K` fails only if the clock under-reports, and slower hardware makes it
            # MORE true — the repository's instances of it assert that a recorded duration is at
            # least a delay the test itself injected, which time cannot violate in that direction.
            # Flagging those would be a gate firing on correct code, and a gate that does that is one
            # somebody turns off.
            upper = (measured_left and op in ("<", "<=")) or (measured_right and op in (">", ">="))

            if not upper:
                continue

            if (measured_left and constant(right)) or (measured_right and constant(left)):
                findings.append(
                    f"{relative}:{line_no} asserts a measured duration against a constant "
                    f"({left.strip()[:40]} {op} {right.strip()[:40]}). The verdict then depends on "
                    "the machine: this exact shape failed 38 consecutive CI runs while the code was "
                    "correct (DC-107). Compare against something measured in the same process, or "
                    f"declare it with a `{MARKER}` comment saying on what hardware it holds.")

    return findings


def test_sources(root: Path) -> list[str]:
    out = subprocess.run(["git", "ls-files", "tests"], capture_output=True, text=True, cwd=root)
    return [f for f in out.stdout.split() if f.endswith(".cs") and f != SELF]


def check(root: Path, files: list[str] | None = None) -> tuple[list[str], int]:
    files = test_sources(root) if files is None else files
    findings, read = [], 0

    for relative in files:
        path = root / relative
        try:
            text = path.read_text(encoding="utf-8")
        except (OSError, UnicodeDecodeError):
            continue
        read += 1
        findings.extend(scan_file(relative, text))

    return (findings, read)


def self_test(root: Path) -> int:
    """Fires on the real pre-fix shape, and stays quiet on the three shapes that are fine."""
    scratch = root / "docs" / "ai-forward-pack"
    scratch.mkdir(parents=True, exist_ok=True)
    probe = scratch / "_selftest_perf.cs"

    cases = {
        # The shape that actually happened, verbatim in structure.
        "budget": ("        const double FrameBudgetMs = 16.67;\n"
                   "        Assert.True(p95 < FrameBudgetMs, \"over budget\");\n", True),
        # A bare literal, the same defect without the named constant.
        "literal": ("        Assert.True(elapsed.TotalMilliseconds < 250, \"slow\");\n", True),
        # The shape the fix uses: both sides measured on the same host.
        "ratio": ("        Assert.True(perLineP95 * 5.0 < perCellP95, \"reverted\");\n", False),
        # A hang guard. Asserts that it finished, not that it was fast.
        "timeout": ("        Assert.True(process.WaitForExit((int)Timeout.TotalMilliseconds));\n", False),
        # A LOWER bound beneath a delay the test injected: asserts the clock ran at all. Slower
        # hardware only makes it more true, so the host is not part of the verdict.
        "lower-bound": ("        Assert.True(status.DurationMilliseconds >= 30, \"a 40ms refresh\");\n", False),
        # The same bound written the other way round is still an upper bound, and still a budget.
        "reversed": ("        Assert.True(16.67 > p95, \"over budget\");\n", True),
        # Declared deliberately, with the justification in place.
        "declared": ("        // perf-budget: pinned to the lab box in DESIGN.md; not run on CI\n"
                     "        const double BudgetMs = 5.0;\n"
                     "        Assert.True(elapsed.TotalMilliseconds < BudgetMs);\n", False),
    }

    try:
        for name, (body, should_fire) in cases.items():
            probe.write_text(body, encoding="utf-8")
            findings, _ = check(root, [probe.relative_to(root).as_posix()])

            if should_fire and not findings:
                print(f"self-test FAILED: '{name}' should have been reported", file=sys.stderr)
                return 1
            if not should_fire and findings:
                print(f"self-test FAILED: '{name}' is fine but was reported: {findings}",
                      file=sys.stderr)
                return 1

        # PACK-P: a verdict over a corpus nobody established was non-empty is not a verdict.
        _, read = check(root, ["NO_SUCH_TEST_FILE.cs"])
        if read != 0:
            print("self-test FAILED: a missing file was counted as read", file=sys.stderr)
            return 1
    finally:
        probe.unlink(missing_ok=True)

    print("self-test OK — a budget against a constant and against a literal are reported; a ratio, "
          "a timeout guard and a declared budget are not; an unreadable corpus counts as nothing.")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--self-test", action="store_true",
                        help="prove the gate fires on DC-107's shape and not on the shapes that are fine")
    args = parser.parse_args()

    root = repo_root()

    if args.self_test:
        return self_test(root)

    findings, read = check(root)

    if read == 0:
        print("verify-perf-assertions: FAILED")
        print("  - no test source was read at all — this gate examined nothing, which is not the "
              "same as finding nothing")
        return 1

    if findings:
        print("verify-perf-assertions: FAILED")
        for finding in findings:
            print(f"  - {finding}")
        return 1

    print(f"verify-perf-assertions: OK — {read} test source(s) read, no duration asserted against "
          "a constant.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
