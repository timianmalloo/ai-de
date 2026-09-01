#!/usr/bin/env python3
"""A test harness may not report an assertion failure as a broken machine.

WHAT HAPPENED. Every STA harness in the App suite caught `Exception ex` and rethrew it as
`InvalidOperationException("STA work failed", failure)`. xUnit assertion failures went through that
path too, so the runner printed:

    System.InvalidOperationException : STA work failed
    ---- PROBE: the rail reported three buttons and two of them do nothing.

The finding — the sentence the test author wrote precisely so a failure would be legible — was
demoted to an inner exception, under a headline that names a cause which is not the cause.

WHY THIS ONE IS WORSE THAN THE FAMILY IT CAME FROM. The announcement defects (DC-074, DC-077) make a
USER believe something false. This makes an ENGINEER stop looking: "STA work failed" reads as a flaky
harness or a missing runtime, and the rational response to a flaky harness is to re-run it, not to
investigate it. A false claim pointed at the person diagnosing is the one that gets a real defect
dismissed rather than fixed.

MEASURED, NOT ASSUMED. Both messages above are real runs of the same planted failure, with the guard
and without it.

WHAT THIS CHECKS. Any test file that catches a failure and rethrows it wrapped must first rethrow
`XunitException` unwrapped. A genuine infrastructure failure keeps its wrapper — there the wrapper is
a true statement, and that distinction is the whole point.

WHAT IT DELIBERATELY DOES NOT DO. It does not check the wrapper's wording, and it does not look
outside `tests/`. Both would fire on ordinary code and be muted within a week, which is the lesson
`verify-id-allocators` and DC-075's control each had to be taught.

Exit 0 when clean, 1 otherwise. Stdlib only.
"""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
from pathlib import Path

TESTS = "tests"

# A harness rethrow: a wrapper exception whose LAST argument is a bare identifier — the inner
# exception. That is the structural signature of wrapping, and it is deliberately name-independent.
#
# THE FIRST VERSION OF THIS PATTERN NAMED THE VARIABLE (failure|caught|captured) and would have
# missed a harness that called it `error` — two exist in this repository, both currently correct, so
# the blind spot hid nothing today and would have hidden the next one. Found by reconciling this
# gate's count against the number of files declaring an STA thread and chasing the three-file gap,
# after the design session found the same narrowness in its own scan of the same subject. A checker
# that looks through a smaller window than its subject reports a plausible number and says nothing
# about the window (DC-079's lesson, arriving in the checker rather than in the code).
#
# A fixture that throws to SIMULATE an error passes only string literals, so it does not match and is
# none of this gate's business.
WRAPS = re.compile(
    r"throw new \w*Exception\([^;]*?,\s*([A-Za-z_]\w*)\s*\)\s*;",
    re.DOTALL)

GUARD = re.compile(r"is\s+(Xunit\.Sdk\.)?XunitException\s*\)?\s*throw")


def repo_root() -> Path:
    return Path(subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        capture_output=True, text=True, check=True).stdout.strip())


def check(root: Path) -> tuple[list[str], int]:
    problems: list[str] = []
    harnesses = 0

    directory = root / TESTS

    if not directory.is_dir():
        return ([f"no {TESTS}/ directory — this check is looking at nothing"], 0)

    for path in sorted(directory.rglob("*.cs")):
        text = path.read_text(encoding="utf-8", errors="replace")

        if not WRAPS.search(text):
            continue

        harnesses += 1

        if GUARD.search(text):
            continue

        relative = path.relative_to(root).as_posix()
        problems.append(
            f"{relative} rethrows a captured failure wrapped, with no XunitException guard before "
            "it. An assertion failure in this file is reported as a broken harness with the real "
            "message demoted to an inner exception — which reads as flakiness, and the response to "
            "flakiness is a re-run, not an investigation. Add: "
            "`if (failure is Xunit.Sdk.XunitException) throw failure;` before the wrap.")

    # The DC-016 guard. If the pattern stopped matching — a refactor renamed the captured variable,
    # say — this gate would report a clean run having examined nothing at all.
    if harnesses == 0:
        problems.append(
            "no wrapping harness was found anywhere under tests/, so this gate examined nothing. "
            "Either every harness was rewritten, or WRAPS no longer matches the shape it is about.")

    return (problems, harnesses)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--self-test", action="store_true",
        help="prove the control fires: an unguarded wrapping harness must fail")
    args = parser.parse_args()

    if args.self_test:
        return self_test()

    problems, harnesses = check(repo_root())

    if problems:
        print("verify-harness-diagnostics: FAILED")
        for problem in problems:
            print(f"  - {problem}")
        return 1

    print(f"verify-harness-diagnostics: OK — {harnesses} wrapping harness(es), every one rethrows an "
          "assertion failure as itself.")
    return 0


def self_test() -> int:
    """The control must be observed FAILING, or it is not a control (CI6)."""
    import tempfile

    with tempfile.TemporaryDirectory() as directory:
        place = Path(directory)
        (place / TESTS).mkdir(parents=True)

        (place / TESTS / "GuardedTests.cs").write_text(
            "if (failure is Xunit.Sdk.XunitException) throw failure;\n"
            'if (failure is not null) throw new InvalidOperationException("STA work failed", failure);\n',
            encoding="utf-8")

        (place / TESTS / "UnguardedTests.cs").write_text(
            'if (failure is not null) throw new InvalidOperationException("STA work failed", failure);\n',
            encoding="utf-8")

        # THE BLIND SPOT THE FIRST PATTERN HAD, kept as a fixture so the widening stays proven. This
        # names the captured variable `error` — two real harnesses do, both currently correct — and
        # the name-based pattern reported it clean. A widening that is not observed catching what the
        # narrow version missed is indistinguishable from one that changed nothing.
        (place / TESTS / "OtherNameTests.cs").write_text(
            'if (error is not null) throw new InvalidOperationException("STA work failed", error);\n',
            encoding="utf-8")

        # A fixture that throws to SIMULATE an error, built from a literal. Must not be reported —
        # a gate that fires on these would be muted, and then the real check goes with it.
        (place / TESTS / "FixtureTests.cs").write_text(
            'Dispatch = _ => throw new InvalidOperationException("pipe closed");\n',
            encoding="utf-8")

        problems, _ = check(place)

    for problem in problems:
        print(f"  planted -> {problem.split('.')[0]}")

    if not any("UnguardedTests.cs" in p for p in problems):
        print("verify-harness-diagnostics: SELF-TEST FAILED — an unguarded harness was not reported.")
        return 1

    if not any("OtherNameTests.cs" in p for p in problems):
        print("verify-harness-diagnostics: SELF-TEST FAILED — a harness whose captured variable is "
              "not called `failure` was not reported. That was this gate's original blind spot: it "
              "matched on the variable NAME, so it read a narrower window than its subject and said "
              "nothing about the window.")
        return 1

    if any("GuardedTests.cs" in p for p in problems):
        print("verify-harness-diagnostics: SELF-TEST FAILED — a guarded harness was reported, so the "
              "gate would be red on correct code.")
        return 1

    if any("FixtureTests.cs" in p for p in problems):
        print("verify-harness-diagnostics: SELF-TEST FAILED — a test fixture that throws a literal "
              "error was reported as a harness; this gate would fire on ordinary test code and be "
              "muted within a week.")
        return 1

    print("verify-harness-diagnostics: self-test OK — unguarded fails, guarded passes, and a "
          "literal-throwing fixture is left alone.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
