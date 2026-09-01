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
# THE TYPE NAME MAY BE QUALIFIED. `\w*Exception` does not match `System.InvalidOperationException`,
# and one real harness writes it that way — so the gate reported it clean while it wrapped. It was
# safe only because a guard had been added there by hand, which is the worst way to be safe: the
# check said nothing and the protection came from somewhere the check could not see.
#
# Found on the SECOND count reconciliation, after the first one fixed the variable-name narrowness
# above. Two blind spots in one 60-character pattern, each invisible to the audit that found the
# other, both located by a printed number disagreeing with an independent scan.
WRAPS = re.compile(
    r"throw new [\w.]*Exception\([^;]*?,\s*([A-Za-z_]\w*)\s*\)\s*;",
    re.DOTALL)

# The guard may be braced — `if (failure is XunitException) { throw failure; }` — which the first
# version of this pattern rejected, reporting a correctly guarded file as unguarded. That is the
# gentler failure direction (a false alarm gets investigated; a false clean does not), but it is the
# third narrowness found in this one small script, each by a count disagreeing with a count. The
# lesson is not "write better regexes": it is that a checker's window is itself a claim, and an
# unexamined one.
GUARD = re.compile(r"is\s+(Xunit\.Sdk\.)?XunitException\s*\)?\s*\{?\s*throw")


def repo_root() -> Path:
    return Path(subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        capture_output=True, text=True, check=True).stdout.strip())


def check(root: Path) -> tuple[list[str], int]:
    problems: list[str] = []
    harnesses = 0

    # THE SECOND DENOMINATOR, and the reason this gate reconciles instead of just counting.
    #
    # Two scans of one corpus with the same pattern family are ONE scan. A gate printing 17 and an
    # "independent" scan printing 17 agreed here for four hours because both used `\w*Exception`, and
    # neither could see `System.InvalidOperationException` — in the one file that was hand-guarded,
    # so nothing ever failed. Agreement between instruments that share a blind spot carries no
    # information at all (§8.3d).
    #
    # A file that declares an STA thread is found by a DIFFERENT means than the wrap pattern, so it
    # cannot share its blind spot. Every such file must land in exactly one category. One that lands
    # in none is the shape both scans missed, and it is now a red gate with the filename in it.
    sta: set[Path] = set()
    accounted: set[Path] = set()
    guarded: set[Path] = set()
    wrapped: set[Path] = set()
    plain = 0
    other = 0

    directory = root / TESTS

    if not directory.is_dir():
        return ([f"no {TESTS}/ directory — this check is looking at nothing"], 0)

    for path in sorted(directory.rglob("*.cs")):
        text = path.read_text(encoding="utf-8", errors="replace")

        wraps = bool(WRAPS.search(text))

        if GUARD.search(text):
            guarded.add(path)

        if "SetApartmentState(ApartmentState.STA)" in text:
            sta.add(path)

            # THE CATEGORIES MUST BE MUTUALLY EXCLUSIVE OR THE SUM IS MEANINGLESS. A wrapping file
            # usually ALSO contains a plain rethrow elsewhere, so counting both gave 18 + 30 + 1 = 49
            # against a denominator of 31 — caught by this reconciliation on its first run, which is
            # the cheapest possible demonstration that it does something.
            if wraps:
                pass                                     # counted as a wrap below
            elif re.search(r"throw (failure|error|caught|thrown)\s*;", text):
                # Rethrows the captured exception as itself — correct, and needing no guard.
                accounted.add(path)
                plain += 1
            elif re.search(r"Assert\.(Null|NotNull|IsType)\s*\(\s*(thrown|failure|error)", text):
                # A test whose CAUGHT EXCEPTION IS ITS OWN SUBJECT is not a harness at all: it
                # asserts that nothing was thrown. One exists (a cross-thread announce test) and it
                # must be neither a defect nor an unexplained gap.
                accounted.add(path)
                other += 1

        if not wraps:
            continue

        harnesses += 1
        accounted.add(path)
        wrapped.add(path)

        if GUARD.search(text):
            continue

        relative = path.relative_to(root).as_posix()
        problems.append(
            f"{relative} rethrows a captured failure wrapped, with no XunitException guard before "
            "it. An assertion failure in this file is reported as a broken harness with the real "
            "message demoted to an inner exception — which reads as flakiness, and the response to "
            "flakiness is a re-run, not an investigation. Add: "
            "`if (failure is Xunit.Sdk.XunitException) throw failure;` before the wrap.")

    # A GUARD WITH NOTHING TO GUARD. This is the check that actually found the qualified-name blind
    # spot, and it is stronger than the reconciliation below because it detects a MISCLASSIFIED file
    # rather than an unclassifiable one.
    #
    # Somebody wrote `if (failure is XunitException) throw failure;` in a file this gate believes has
    # no wrapper. Exactly one of two things is true, and both are worth a look: the guard is dead code
    # and should go, or there is a wrap here that WRAPS cannot see — which is what a narrow pattern
    # looks like from the outside. The one real instance was the second: a hand-added guard in a file
    # wrapping with a fully-qualified type name, and the hand-guard is precisely what kept the gate's
    # blindness symptomless for four hours.
    for path in sorted(guarded - wrapped):
        problems.append(
            f"{path.relative_to(root).as_posix()} carries an XunitException guard but this gate sees "
            "no wrapper for it to guard. Either the guard is dead code and should be removed, or "
            "there is a wrap here the pattern cannot read — a hand-written guard over a wrap the "
            "check cannot see is protection from somewhere the check does not know about, and its "
            "silence is then indistinguishable from coverage.")

    # THE RECONCILIATION. Every file found by the independent denominator must have been classified
    # by one of the pattern-based rules. A file in neither is precisely the shape a narrow pattern
    # hides, and it is named rather than summarised.
    #
    # ITS LIMIT, MEASURED RATHER THAN ASSUMED. This catches an UNCLASSIFIABLE file, not a
    # MISCLASSIFIED one. Re-running the day-one blind pattern with the reconciliation in place still
    # sums correctly — 17 + 13 + 1 = 31 — because the blind file was absorbed by the plain-rethrow
    # category, its guard line containing a bare `throw failure;`. A category broad enough to absorb
    # a miss cannot report it. That is why the guard-with-nothing-to-guard check above exists, and
    # why claiming this reconciliation "would have caught it" was wrong until it was run.
    for path in sorted(sta - accounted):
        problems.append(
            f"{path.relative_to(root).as_posix()} declares an STA thread but matched none of this "
            "gate's categories — not a wrap, not a plain rethrow, not a test whose subject is an "
            "exception. That gap is the shape a narrow pattern hides: the file is real, the scan was "
            "full, and the silence is indistinguishable from coverage. Read it and either widen a "
            "pattern or add its category.")

    # The DC-016 guard. If the pattern stopped matching — a refactor renamed the captured variable,
    # say — this gate would report a clean run having examined nothing at all.
    if harnesses == 0:
        problems.append(
            "no wrapping harness was found anywhere under tests/, so this gate examined nothing. "
            "Either every harness was rewritten, or WRAPS no longer matches the shape it is about.")

    if problems:
        return (problems, harnesses)

    print(f"verify-harness-diagnostics: {len(sta)} file(s) declare an STA thread = "
          f"{harnesses} wrapping + {plain} plain rethrow(s) + {other} whose subject is an exception.")

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

        # THE BRACED GUARD, which the first pattern rejected — a correctly guarded file reported as
        # unguarded. Kept so the gate cannot narrow back into a false alarm.
        (place / TESTS / "BracedGuardTests.cs").write_text(
            "if (failure is Xunit.Sdk.XunitException) { throw failure; }\n"
            'if (failure is not null) { throw new System.InvalidOperationException("x", failure); }\n',
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

        # THE SECOND BLIND SPOT: a fully-qualified type name. `\w*Exception` does not match
        # `System.InvalidOperationException`, and a real harness writes it that way — the gate
        # called it clean while it wrapped.
        (place / TESTS / "QualifiedTypeTests.cs").write_text(
            'if (failure is not null) '
            'throw new System.InvalidOperationException("STA work failed", failure);\n',
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

    if any("BracedGuardTests.cs" in p for p in problems):
        print("verify-harness-diagnostics: SELF-TEST FAILED — a BRACED guard was not recognised, so "
              "a correctly guarded file is reported as unguarded. A false alarm is the gentler "
              "direction, but it trains readers to ignore this gate.")
        return 1

    if not any("QualifiedTypeTests.cs" in p for p in problems):
        print("verify-harness-diagnostics: SELF-TEST FAILED — a harness wrapping with a "
              "FULLY-QUALIFIED exception type was not reported. That was this gate's second blind "
              "spot: `\\w*Exception` does not match `System.InvalidOperationException`.")
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
