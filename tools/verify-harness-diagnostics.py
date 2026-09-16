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
EDI_GUARD = re.compile(
    r"if\s*\(\s*(?P<failure>\w+)\s+is\s+(?:Xunit\.Sdk\.)?XunitException\s*\)\s*\{?\s*"
    r"(?:System\.Runtime\.ExceptionServices\.)?ExceptionDispatchInfo\.Capture\(\s*(?P=failure)\s*\)\.Throw\(\s*\)")


def _edi_guards_wrapper(text: str, wrapper: re.Match[str]) -> bool:
    """Admit only an adjacent same-original or foreach-of-wrapped-collection EDI guard."""
    code = re.sub(r'//[^\n]*|/\*.*?\*/|@?"(?:""|\\.|[^"\\])*"',
                  lambda match: " " * len(match.group()), text, flags=re.DOTALL)
    scopes: list[tuple[int, int]] = []
    for method in re.finditer(
            r"\b(?:[\w.<>?\[\]]+\s+)+\w+\s*\([^;{}]*\)\s*\{", code):
        depth, end = 1, method.end()
        while end < len(code) and depth:
            depth += (code[end] == "{") - (code[end] == "}")
            end += 1
        scopes.append((method.end(), end - 1))

    def scope(position: int) -> tuple[int, int]:
        return min((bounds for bounds in scopes if bounds[0] <= position < bounds[1]),
                   key=lambda bounds: bounds[1] - bounds[0], default=(0, len(code)))

    bounds = scope(wrapper.start())
    for guard in EDI_GUARD.finditer(code, bounds[0], wrapper.start()):
        if scope(guard.start()) != bounds:
            continue
        # No reassignment or unrelated statement may intervene. The wrapper's
        # existing nonempty/null check and one guard/foreach closing brace may.
        tail = code[guard.end():wrapper.start()]
        if not re.fullmatch(r"\s*;\s*\}?\s*(?:if\s*\([^;{}]*\)\s*)?", tail):
            continue
        original = guard.group("failure")
        if original == wrapper.group(1):
            return True
        before = code[bounds[0]:guard.start()]
        if re.search(rf"foreach\s*\(\s*var\s+{re.escape(original)}\s+in\s+"
                     rf"{re.escape(wrapper.group(1))}\s*\)\s*\{{?\s*$", before):
            return True
    return False


def _tcs_handoffs(text: str) -> tuple[int, list[str]]:
    """Classify bounded async STA methods, checking every exception setter separately.

    This is a lexical contract, not C# semantic analysis. Unknown handoff forms stay
    unclassified; a healthy method never excuses a bad setter in the same file.
    """
    code = re.sub(r'//[^\n]*|/\*.*?\*/|@?"(?:""|\\.|[^"\\])*"',
                  lambda match: " " * len(match.group()), text, flags=re.DOTALL)
    methods = re.finditer(
        r"\b(?:private|internal|public)\s+(?:static\s+)?async\s+Task(?:<[^>]+>)?\s+"
        r"(?P<name>\w+)\s*\([^;{}]*\)\s*\{", code)
    count = 0
    problems: list[str] = []
    for method in methods:
        depth, end = 1, method.end()
        while end < len(code) and depth:
            depth += (code[end] == "{") - (code[end] == "}")
            end += 1
        body = code[method.end():end - 1]
        if "SetApartmentState(ApartmentState.STA)" not in body:
            continue
        setters = list(re.finditer(r"\b(\w+)\.(?:Try)?SetException\s*\(([^;]*)\)\s*;", body))
        if not setters:
            continue
        declared = set(re.findall(r"\b(\w+)\s*=\s*new\s+TaskCompletionSource(?:<[^>]+>)?\s*\(", body))
        catches = list(re.finditer(r"catch\s*\(\s*(?:System\.)?Exception\s+(\w+)\s*\)\s*\{([^{}]*)\}", body))
        valid = True
        for setter in setters:
            completion, original = setter.group(1), setter.group(2).strip()
            reason = None
            if (completion not in declared or
                    len(re.findall(rf"\b{re.escape(completion)}\s*=(?!=)", body)) != 1 or
                    not re.search(rf"\bawait\s+{re.escape(completion)}\.Task\b", body)):
                reason = "exception setter is not correlated to a declared and awaited same TCS"
            elif not re.fullmatch(r"\w+", original):
                reason = "exception setter wraps or replaces the original exception"
            else:
                direct = next((c for c in catches
                               if c.start() <= setter.start() < c.end() and c.group(1) == original), None)
                if direct and re.search(rf"\b{re.escape(original)}\s*=(?!=)", direct.group(2)):
                    reason = "caught exception is reassigned before its handoff"
            if reason is None and not direct:
                writes = list(re.finditer(rf"\b{re.escape(original)}\s*=\s*(?!=)([^;]+);", body))
                captured = [w for w in writes if any(
                    c.start() <= w.start() < c.end() and w.group(1).strip() == c.group(1) and
                    not re.search(rf"\b{re.escape(c.group(1))}\s*=(?!=)", c.group(2)) for c in catches)]
                initializers = list(re.finditer(
                    rf"\b(?:System\.)?Exception\?\s+{re.escape(original)}\s*=\s*null\s*;", body))
                if not captured or any(w not in captured and not any(
                        init.start() <= w.start() < init.end() for init in initializers) for w in writes):
                    reason = "exception setter argument is not an unchanged caught exception"
            if reason:
                valid = False
                problems.append(f"{method.group('name')}: {reason}")
        if valid:
            count += 1
    return count, problems


def repo_root() -> Path:
    return Path(subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        capture_output=True, text=True, encoding="utf-8", errors="replace", check=True).stdout.strip())


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
    handed_off = 0

    directory = root / TESTS

    if not directory.is_dir():
        return ([f"no {TESTS}/ directory — this check is looking at nothing"], 0)

    for path in sorted(directory.rglob("*.cs")):
        text = path.read_text(encoding="utf-8", errors="replace")

        wrapper_matches = list(WRAPS.finditer(text))
        wraps = bool(wrapper_matches)

        has_guard = GUARD.search(text) or EDI_GUARD.search(text)
        if has_guard:
            guarded.add(path)

        threads = text.count("SetApartmentState(ApartmentState.STA)")

        if threads:
            sta.add(path)
            handoffs, handoff_problems = _tcs_handoffs(text)
            problems.extend(f"{path.relative_to(root).as_posix()} {problem}" for problem in handoff_problems)

            # THE UNIT OF ANALYSIS, the one error a wider pattern cannot fix. This gate reaches a
            # verdict per FILE, but the thing that can be right or wrong is the HARNESS. A file
            # standing up two INDEPENDENT harnesses - one wrapping, one plain - gets a single
            # verdict, and the passing one masks the failing one. No regex width changes that; only
            # counting the right unit does.
            #
            # Measured on a THIRD denominator: 31 files, 31 thread creations, so nothing is exposed
            # today. Two files declare an extra overload, but each DELEGATES to the single creation
            # rather than standing up its own thread - one harness with two entry points, benign,
            # and the reason a count of declarations (29) and a count of creations (31) disagree
            # without either being wrong. One creation per file is the invariant that matters.
            #
            # SCOPED TO TEST FILES, and the consolidation is what taught this. A shared harness
            # library legitimately stands up two threads - a plain one and a dispatcher-pumping one -
            # whose failures funnel through ONE rethrow. Nothing can mask there, because there is a
            # single verdict by construction. The masking risk needs two harnesses each carrying
            # their own verdict, which is a property of a file holding TESTS.
            #
            # Scoping by "does this file contain [Fact]" rather than by naming the harness file: an
            # allowance list has the catch-all property that hides a miss, which is the trap the
            # reconciliation below already fell into once.
            if threads > 1 and ("[Fact]" in text or "[Theory]" in text):
                problems.append(
                    f"{path.relative_to(root).as_posix()} stands up {threads} separate STA threads, "
                    "so it holds more than one harness - but this gate reaches one verdict per FILE. "
                    "One harness passing would mask another failing, and no widening of the patterns "
                    "would show it, because the error is the unit of analysis rather than the "
                    "window. Split them into separate files, or have one delegate to the other so "
                    "there is a single thing to judge.")

            # THE CATEGORIES MUST BE MUTUALLY EXCLUSIVE OR THE SUM IS MEANINGLESS. A wrapping file
            # usually ALSO contains a plain rethrow elsewhere, so counting both gave 18 + 30 + 1 = 49
            # against a denominator of 31 — caught by this reconciliation on its first run, which is
            # the cheapest possible demonstration that it does something.
            if wraps:
                pass                                     # counted as a wrap below
            elif handoffs and not handoff_problems:
                accounted.add(path)
                handed_off += 1
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

        if GUARD.search(text) or all(_edi_guards_wrapper(text, wrapper) for wrapper in wrapper_matches):
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
          f"{harnesses} wrapping + {plain} plain rethrow(s) + {handed_off} original TCS handoff(s) "
          f"+ {other} whose subject is an exception.")

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

        # TWO INDEPENDENT HARNESSES IN ONE FILE: the granularity error, which no pattern width fixes.
        # None exists in the repository today, so without this fixture the check would never be seen
        # to fire at all.
        (place / TESTS / "TwoHarnessTests.cs").write_text(
            # [Fact] because the check is scoped to files holding TESTS. A fixture that does not look
            # like its subject proves nothing — and this one caught the scoping change the moment it
            # was made, by going red rather than by being reasoned about.
            "[Fact] public void T() { }\n"
            "thread.SetApartmentState(ApartmentState.STA);\n"
            "if (failure is not null) throw failure;\n"
            "other.SetApartmentState(ApartmentState.STA);\n"
            'if (failure is not null) '
            'throw new InvalidOperationException("STA work failed", failure);\n',
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

        tcs = '''
[Fact] public void T() { }
private static async Task RunAsync(Func<Task> body)
{
    var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var thread = new Thread(() =>
    {
        Exception? failure = null;
        async Task BodyAsync()
        {
            try { await body(); }
            catch (Exception exception) { failure = exception; }
        }
        try
        {
            BodyAsync().GetAwaiter().GetResult();
            if (failure is not null) completed.TrySetException(failure);
            else completed.TrySetResult();
        }
        catch (Exception exception) { completed.TrySetException(exception); }
    });
    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    await completed.Task.WaitAsync(TimeSpan.FromSeconds(30));
}
'''
        tcs_cases = {
            "HealthyTcsTests.cs": (tcs, False),
            "WrappedTcsTests.cs": (tcs.replace("TrySetException(failure)", 'TrySetException(new InvalidOperationException("wrapped", failure))'), True),
            "WrongAwaitTcsTests.cs": (tcs.replace("await completed.Task", "await unrelated.Task"), True),
            "UncaughtTcsTests.cs": (tcs.replace("TrySetException(failure)", "TrySetException(unrelated)"), True),
            "ReassignedTcsTests.cs": (tcs.replace("if (failure is not null)", 'failure = new Exception("replacement"); if (failure is not null)'), True),
            "NullResetTcsTests.cs": (tcs.replace("if (failure is not null)", 'failure = null; if (failure is not null)'), True),
            "ChangedCompletionTcsTests.cs": (tcs.replace("thread.Start();", "completed = unrelated; thread.Start();"), True),
            "ChangedCatchTcsTests.cs": (tcs.replace("completed.TrySetException(exception);", 'exception = new Exception("replacement"); completed.TrySetException(exception);'), True),
            "ChangedCapturedSourceTcsTests.cs": (tcs.replace("failure = exception;", 'exception = new Exception("replacement"); failure = exception;'), True),
            "HealthyAndBadTcsTests.cs": (tcs + tcs.replace("RunAsync", "BadAsync").replace("TrySetException(failure)", 'TrySetException(new Exception("wrapped", failure))'), True),
            "HealthyAndBadSetterTests.cs": (tcs.replace("else completed.TrySetResult();", 'else completed.TrySetException(new Exception("bad setter"));'), True),
        }
        for name, (source, _) in tcs_cases.items():
            (place / TESTS / name).write_text(source, encoding="utf-8")
        edi = ("foreach (var failure in failures)\nif (failure is Xunit.Sdk.XunitException) "
               "System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();\n"
               'throw new AggregateException("retained", failures);\n')
        (place / TESTS / "EdiGuardTests.cs").write_text(edi, encoding="utf-8")
        (place / TESTS / "WrongEdiGuardTests.cs").write_text(
            edi.replace("Capture(failure)", "Capture(unrelated)"), encoding="utf-8")
        scoped_edi_cases = {
            "ExactMixedEdiTests.cs": (
                'private static void Healthy(Exception failure) { if (failure is Xunit.Sdk.XunitException) '
                'System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw(); '
                'throw new InvalidOperationException("retained", failure); } '
                'private static void Broken(Exception unrelated) { '
                'throw new InvalidOperationException("broken wrapper", unrelated); }', True),
            "SameNameMixedEdiTests.cs": (
                'private static void Healthy(Exception failure) { if (failure is Xunit.Sdk.XunitException) '
                'System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw(); '
                'throw new InvalidOperationException("retained", failure); } '
                'private static void Broken(Exception failure) { '
                'throw new InvalidOperationException("broken wrapper", failure); }', True),
            "MixedEdiTests.cs": (
                "void Healthy(Exception[] failures) {\n" + edi + "}\n"
                'void Broken(Exception unrelated) { throw new InvalidOperationException("broken", unrelated); }', True),
            "UnrelatedCollectionEdiTests.cs": (edi.replace('"retained", failures', '"retained", unrelated'), True),
            "UnrelatedOriginalEdiTests.cs": (
                'void Broken(Exception failure, Exception unrelated) {\n' +
                edi[edi.index("if ("):].replace('"retained", failures', '"retained", unrelated') + "}", True),
            "SameOriginalEdiTests.cs": (
                'void Healthy(Exception failure) {\n' +
                edi[edi.index("if ("):].replace('"retained", failures', '"retained", failure') + "}", False),
            "SameCollectionEdiTests.cs": ("void Healthy(Exception[] failures) {\n" + edi + "}", False),
        }
        for name, (source, _) in scoped_edi_cases.items():
            (place / TESTS / name).write_text(source, encoding="utf-8")
        problems, _ = check(place)

    escaped = [name for name, (_, should_fail) in scoped_edi_cases.items()
               if any(name in problem for problem in problems) != should_fail]
    if escaped:
        print(f"verify-harness-diagnostics: SELF-TEST FAILED — scoped EDI verdicts: {escaped}")
        return 1
    for name, (_, should_fail) in tcs_cases.items():
        if any(name in problem for problem in problems) != should_fail:
            matching = [problem for problem in problems if name in problem]
            print(f"verify-harness-diagnostics: SELF-TEST FAILED — TCS correlation {name}: {matching}")
            return 1
    if any("EdiGuardTests.cs " in problem and "WrongEdiGuardTests.cs" not in problem for problem in problems):
        print("verify-harness-diagnostics: SELF-TEST FAILED — same-instance EDI guard rejected")
        return 1
    if not any("WrongEdiGuardTests.cs" in problem for problem in problems):
        print("verify-harness-diagnostics: SELF-TEST FAILED — unrelated EDI capture accepted")
        return 1

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

    if not any("TwoHarnessTests.cs" in p and "separate STA threads" in p for p in problems):
        print("verify-harness-diagnostics: SELF-TEST FAILED - a file standing up two independent "
              "harnesses was not reported. That is the granularity error: one verdict per file lets "
              "a passing harness mask a failing one, and no pattern width would show it.")
        return 1

    if any("FixtureTests.cs" in p for p in problems):
        print("verify-harness-diagnostics: SELF-TEST FAILED — a test fixture that throws a literal "
              "error was reported as a harness; this gate would fire on ordinary test code and be "
              "muted within a week.")
        return 1

    print("verify-harness-diagnostics: self-test OK — unguarded fails, guarded passes, "
          "11 TCS fixtures and 9 EDI fixtures retain correlation, same-file negatives fail, "
          "and a literal-throwing fixture is left alone.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
