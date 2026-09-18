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
import json
import re
import subprocess
import sys
import tempfile
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
        capture_output=True, text=True, encoding="utf-8", errors="replace", check=True).stdout.strip())


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


# ---------------------------------------------------------------------------------------------
# Fixtures for verify-test-run.py, whose own failure modes are proven from HERE.
#
# WHY HERE AND NOT THERE. verify-test-run.py is on the frozen list above and must stay on it: the
# list may only shrink, and giving that tool a `--self-test` flag would make its entry STALE and
# fail this very gate. Its behaviours still have to be OBSERVED failing (DC-104), so they are
# proven from the outside, which is also the stronger position — the fixtures drive the tool's real
# command line rather than an internal function.
#
# IN A THROWAWAY REPOSITORY, never this one. verify-test-run.py derives its repository root from
# its own path (`REPO = Path(__file__).resolve().parent.parent`), so a COPY in a temporary
# tools/ directory reads that temporary tree's tests/, artifacts/test-results/ and baseline. A
# fixture planted in this worktree's artifacts/test-results/ would race a concurrent session's real
# run and could be mistaken for one.
TRX = (
    '<?xml version="1.0" encoding="utf-8"?>\n'
    '<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">\n'
    "  <Results>\n"
    "{results}"
    "  </Results>\n"
    '  <ResultSummary outcome="{outcome}">\n'
    '    <Counters total="{total}" executed="{executed}" passed="{passed}" failed="{failed}"'
    ' error="0" timeout="0" aborted="0" />\n'
    "  </ResultSummary>\n"
    "</TestRun>\n"
)

TRX_RESULT = (
    '    <UnitTestResult testName="{name}" outcome="{outcome}">\n'
    "      <Output><ErrorInfo><Message>{message}</Message></ErrorInfo></Output>\n"
    "    </UnitTestResult>\n"
)


def _run(command: list[str], cwd: Path) -> subprocess.CompletedProcess:
    return subprocess.run(command, cwd=str(cwd), capture_output=True, text=True,
                          encoding="utf-8", errors="replace", check=False)


def plant_test_run_fixture(root: Path, sandbox: Path, trx: str, baseline: dict) -> Path:
    """Build a throwaway tree verify-test-run.py can be run against; return the copied tool.

    BYTES, NEVER write_text. On Windows text mode rewrites every LF to CRLF, so an assertion over a
    multi-line block of the tool's output matches nothing while the run still looks green — a
    no-op control, which is worse than no control. Paid for once in this repository already.
    """
    (sandbox / "tools").mkdir(parents=True, exist_ok=True)
    (sandbox / "tests" / "Fixture.Tests").mkdir(parents=True, exist_ok=True)
    (sandbox / "artifacts" / "test-results").mkdir(parents=True, exist_ok=True)

    tool = sandbox / "tools" / "verify-test-run.py"
    tool.write_bytes((root / "tools" / "verify-test-run.py").read_bytes())
    (sandbox / "tests" / "Fixture.Tests" / "Fixture.Tests.csproj").write_bytes(
        b'<Project Sdk="Microsoft.NET.Sdk"></Project>\n')
    (sandbox / "artifacts" / "test-results" / "Fixture.Tests.trx").write_bytes(trx.encode("utf-8"))
    write_fixture_baseline(sandbox, baseline)
    return tool


def write_fixture_baseline(sandbox: Path, baseline: dict, skips: list[str] | None = None) -> None:
    (sandbox / "tools" / "expected-test-counts.json").write_bytes(
        (json.dumps({"minimumTotal": baseline, "expectedSkips": skips or [], "splits": {}},
                    indent=2) + "\n").encode("utf-8"))


def git_fixture(root: Path, sandbox: Path, trx: str, baseline: dict,
                skips: list[str] | None = None) -> Path:
    """plant_test_run_fixture, then COMMIT it — for the checks that compare against HEAD.

    `--update`'s refusal to lower reads the baseline as git has it, not as the working tree has it,
    so a fixture that is never committed cannot exercise it: an uncommitted baseline has no
    committed value to be below.
    """
    sandbox.mkdir(parents=True, exist_ok=True)
    tool = plant_test_run_fixture(root, sandbox, trx, baseline)
    write_fixture_baseline(sandbox, baseline, skips)
    for command in (("init", "-q", "-b", "main"),
                    ("config", "user.email", "gate@example.invalid"),
                    ("config", "user.name", "gate"),
                    ("add", "-A"),
                    ("commit", "-qm", "the committed baseline")):
        done = _run(["git", *command], sandbox)
        if done.returncode != 0:
            raise RuntimeError(f"fixture setup: git {' '.join(command)} exited "
                               f"{done.returncode}: {done.stdout}{done.stderr}")
    return tool


def baseline_value(sandbox: Path, key: str) -> int | None:
    document = json.loads(
        (sandbox / "tools" / "expected-test-counts.json").read_text(encoding="utf-8"))
    return document.get("minimumTotal", {}).get(key)


def test_run_refuses_a_lowered_baseline(root: Path) -> list[str]:
    """DIRECTION 5: `--update` may not write a key BELOW its committed value unsaid.

    The defect, measured 2026-09-17: `--update` merged `observed` over the baseline with no
    comparison at all, and the join runs it three times before the check. Whichever machine joined
    last silently re-set the floor to its own run — so a control meant to detect a silently-aborted
    test run quietly gave away its margin, and the baseline sat 8 Core and 2 App tests below reality
    with nothing firing.
    """
    problems: list[str] = []
    with tempfile.TemporaryDirectory() as tmp:
        sandbox = Path(tmp) / "repo"
        trx = TRX.format(results="", outcome="Completed", total=8, executed=8, passed=8, failed=0)
        tool = git_fixture(root, sandbox, trx, {"Fixture.Tests": 10})

        refused = _run([sys.executable, str(tool), "--update", "--no-run"], sandbox)
        output = refused.stdout + refused.stderr
        if refused.returncode != 1:
            problems.append(
                f"--update wrote a key from 10 down to 8 and exited {refused.returncode}, not 1 — "
                f"the silent re-baseline is exactly the defect:\n{output}")
        for token in ("Fixture.Tests", "10", "8", "--allow-lower"):
            if token not in output:
                problems.append(
                    f"the refusal did not print '{token}'. A refusal that does not name the key, "
                    f"its old value and its new value cannot be acted on:\n{output}")
        if baseline_value(sandbox, "Fixture.Tests") != 10:
            problems.append("the baseline was REWRITTEN by a run that refused — a refusal that "
                            "still writes is not a refusal")

        # An empty reason is not a reason. `--allow-lower ""` would otherwise satisfy the flag
        # while recording nothing, which is the flag existing and the control not.
        empty = _run([sys.executable, str(tool), "--update", "--no-run", "--allow-lower", ""],
                     sandbox)
        if empty.returncode != 1:
            problems.append(
                f"--allow-lower with an EMPTY reason exited {empty.returncode}, not 1:\n"
                f"{empty.stdout}{empty.stderr}")

        # The other half of the ratchet. A refusal that can never be satisfied is a wall, and the
        # legitimate case — tests genuinely removed — has to have a way through that is written down.
        allowed = _run([sys.executable, str(tool), "--update", "--no-run",
                        "--allow-lower", "two tests were deleted in this candidate"], sandbox)
        if allowed.returncode != 0:
            problems.append(
                f"--allow-lower with a reason was still refused (exit {allowed.returncode}):\n"
                f"{allowed.stdout}{allowed.stderr}")
        elif baseline_value(sandbox, "Fixture.Tests") != 8:
            problems.append(
                f"--allow-lower was accepted but the baseline is "
                f"{baseline_value(sandbox, 'Fixture.Tests')}, not the observed 8")
    return problems


def test_run_floors_on_total_not_executed(root: Path) -> list[str]:
    """DIRECTION 6: the floor is `total`, which does not vary by operating system.

    MEASURED on CI run 35228503081's own .trx artefacts against local runs at the same tree:
    `total` is equal on every key (App 1053, portable 2575, nonportable 181) while `executed`
    differs on two — CI's nonportable run executes 177 of 181 and local executes 181 of 181. The
    delta is exactly the dynamically-skipped tests, which a .trx records as NotExecuted: INSIDE
    total, OUTSIDE executed. A crashed host writes fewer results, so `total` still drops on the
    DC-012 shape it exists to catch. Flooring on `executed` put two keys above anything CI could
    produce; flooring on `total` is both stronger and meetable everywhere.
    """
    problems: list[str] = []
    skipped = "Fixture.Tests.DynamicallySkipped"
    with tempfile.TemporaryDirectory() as tmp:
        sandbox = Path(tmp) / "repo"
        # The CI shape: four of ten did not execute, all of them named in the committed list.
        results = "".join(
            TRX_RESULT.format(name=f"{skipped}{n}", outcome="NotExecuted", message="skipped")
            for n in range(4))
        trx = TRX.format(results=results, outcome="Completed",
                         total=10, executed=6, passed=6, failed=0)
        tool = git_fixture(root, sandbox, trx, {"Fixture.Tests": 10},
                           skips=[f"{skipped}{n}" for n in range(4)])

        met = _run([sys.executable, str(tool), "--no-run"], sandbox)
        if met.returncode != 0:
            problems.append(
                f"total 10 against a floor of 10, with executed 6, exited {met.returncode} — the "
                f"floor is still reading `executed`, which no CI run of this shape can meet:\n"
                f"{met.stdout}{met.stderr}")

        # And it still catches the thing it exists for: a host that died writes fewer RESULTS, so
        # total itself drops.
        (sandbox / "artifacts" / "test-results" / "Fixture.Tests.trx").write_bytes(
            TRX.format(results="", outcome="Completed",
                       total=9, executed=9, passed=9, failed=0).encode("utf-8"))
        short = _run([sys.executable, str(tool), "--no-run"], sandbox)
        output = short.stdout + short.stderr
        if short.returncode != 1 or "missing from the results" not in output:
            problems.append(
                f"total 9 against a floor of 10 exited {short.returncode} and was not reported as "
                f"a shortfall:\n{output}")
    return problems


def test_run_refuses_an_uncommitted_skip(root: Path) -> list[str]:
    """DIRECTION 7: every NotExecuted name must appear in the committed expectedSkips list.

    THE CHECK THE DOCSTRING ALREADY CLAIMED. verify-test-run.py's own list of what it checks said
    "no test was skipped unexpectedly" at :16 and the code performed no such check — a control
    documented and not built, which reads to a reviewer exactly like one that works. It is also what
    makes the move to `total` safe: `total >= floor` alone would pass a run that skipped a hundred
    tests dynamically. `total >= floor` PLUS `NotExecuted subset of expectedSkips` is strictly
    stronger than the `executed >= floor` it replaces.
    """
    problems: list[str] = []
    known = "Fixture.Tests.SkippedOnPurpose"
    surprise = "Fixture.Tests.SkippedWithNobodyWatching"
    with tempfile.TemporaryDirectory() as tmp:
        sandbox = Path(tmp) / "repo"
        results = (TRX_RESULT.format(name=known, outcome="NotExecuted", message="by design")
                   + TRX_RESULT.format(name=surprise, outcome="NotExecuted", message="who knows"))
        trx = TRX.format(results=results, outcome="Completed",
                         total=10, executed=8, passed=8, failed=0)
        tool = git_fixture(root, sandbox, trx, {"Fixture.Tests": 10}, skips=[known])

        refused = _run([sys.executable, str(tool), "--no-run"], sandbox)
        output = refused.stdout + refused.stderr
        if refused.returncode != 1:
            problems.append(
                f"a NotExecuted result absent from expectedSkips exited {refused.returncode}, "
                f"not 1:\n{output}")
        if surprise not in output:
            problems.append(f"the unlisted skip was not NAMED:\n{output}")
        if refused.returncode == 1 and output.count(known) and "expectedSkips" not in output:
            problems.append(f"the finding did not say which list the name is missing from:\n{output}")

        # The other half: a skip that IS on the list is not news.
        write_fixture_baseline(sandbox, {"Fixture.Tests": 10}, skips=[known, surprise])
        accepted = _run([sys.executable, str(tool), "--no-run"], sandbox)
        if accepted.returncode != 0:
            problems.append(
                f"both skips were committed to expectedSkips and the run was still refused "
                f"(exit {accepted.returncode}):\n{accepted.stdout}{accepted.stderr}")
    return problems


def test_run_names_its_failures(root: Path) -> list[str]:
    """DIRECTION 3: a counted failure must be NAMED.

    The defect, measured: this gate was red on main for a day and its whole report was "1 failed,
    0 errored, 0 aborted, 0 timed out". Recovering the test's name cost a full local run — from a
    .trx the tool had already opened and parsed. A count that cannot be acted on is a control that
    reports without informing.
    """
    failing = ("AiDe.Core.Tests.EveryOperationFitsTheFrameTests."
               "NoOperationCanBuildAResponseTheTransportWouldRefuse")
    first = "these responses cannot cross the 1,048,576-byte frame: EntryPointsAsync = 2,191,570 bytes"
    second = "   at AiDe.Core.Tests.EveryOperationFitsTheFrameTests.MoveNext()"

    with tempfile.TemporaryDirectory() as tmp:
        sandbox = Path(tmp)
        trx = TRX.format(
            results=TRX_RESULT.format(name=failing, outcome="Failed", message=first + "\n" + second),
            outcome="Failed", total=2, executed=2, passed=1, failed=1)
        tool = plant_test_run_fixture(root, sandbox, trx, {"Fixture.Tests": 2})
        done = _run([sys.executable, str(tool), "--no-run"], sandbox)
        output = done.stdout + done.stderr

        if done.returncode != 1:
            return [f"a .trx carrying one failed result exited {done.returncode}, not 1:\n{output}"]
        if failing not in output:
            return ["a failure was counted but not NAMED — the name was in the .trx this tool had "
                    f"already parsed:\n{output}"]
        if first not in output:
            return [f"the failing test was named without the first line of its message:\n{output}"]
        if second.strip() in output:
            return ["more than the FIRST line of the error message was printed — a stack trace in a "
                    f"CI log buries the line that says what broke:\n{output}"]
    return []


def test_run_refuses_upward_drift(root: Path) -> list[str]:
    """DIRECTION 4: executed OVER the baseline, with the baseline unmoved, is refused at the join.

    The defect, measured on CI run 35228503081: executed 2,575 against an expected 2,567, because
    two landings added tests without raising the floor. The gate fails only on executed < expected,
    so nothing fired — and a silent abort of up to eight tests had become invisible. The margin
    shrinks with every landing that does not recount.
    """
    problems: list[str] = []
    with tempfile.TemporaryDirectory() as tmp:
        sandbox = Path(tmp) / "repo"
        sandbox.mkdir()
        trx = TRX.format(results="", outcome="Completed", total=5, executed=5, passed=5, failed=0)
        tool = plant_test_run_fixture(root, sandbox, trx, {"Fixture.Tests": 2})

        for command in (("init", "-q", "-b", "main"),
                        ("config", "user.email", "gate@example.invalid"),
                        ("config", "user.name", "gate"),
                        ("add", "-A"),
                        ("commit", "-qm", "the candidate's base")):
            done = _run(["git", *command], sandbox)
            if done.returncode != 0:
                return [f"fixture setup: git {' '.join(command)} exited {done.returncode}: "
                        f"{done.stdout}{done.stderr}"]

        line = [sys.executable, str(tool), "--no-run", "--refuse-upward-drift", "HEAD"]

        refused = _run(line, sandbox)
        output = refused.stdout + refused.stderr
        if refused.returncode != 1 or "did not move" not in output:
            problems.append(
                f"executed 5 over a baseline of 2, unmoved in this candidate, exited "
                f"{refused.returncode} and was not refused:\n{output}")

        # The other half of the ratchet. A control that only ever refuses has proven nothing about
        # WHICH candidates it refuses: here the baseline DID move in the candidate (3, uncommitted,
        # exactly what `--update` leaves behind at a join) and is still below the executed count.
        write_fixture_baseline(sandbox, {"Fixture.Tests": 3})
        accepted = _run(line, sandbox)
        if accepted.returncode != 0:
            problems.append(
                f"the baseline MOVED in this candidate and the run was still refused (exit "
                f"{accepted.returncode}):\n{accepted.stdout}{accepted.stderr}")
    return problems


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

    # DIRECTIONS 3 TO 7 belong to verify-test-run.py, which is frozen above and therefore cannot
    # carry its own --self-test without falsifying this gate's own list. They are proven here, from
    # the outside, against a copy of that tool in a throwaway tree.
    # ALL are evaluated before ANY is reported: stopping at the first would hide the rest, and five
    # fixtures planted in one run are five pieces of evidence, not one.
    test_run = (test_run_names_its_failures(root)
                + test_run_refuses_upward_drift(root)
                + test_run_refuses_a_lowered_baseline(root)
                + test_run_floors_on_total_not_executed(root)
                + test_run_refuses_an_uncommitted_skip(root))
    if test_run:
        for finding in test_run:
            print(f"self-test FAILED: verify-test-run.py: {finding}", file=sys.stderr)
        return 1

    print("self-test OK — new debt is refused, a stale frozen entry is reported, verify-test-run "
          "names the failures it counts, an unmoved baseline under a higher count is refused at the "
          "join, --update refuses to lower a committed floor unsaid, the floor reads `total` rather "
          "than `executed`, and an unlisted NotExecuted result is a finding")
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
