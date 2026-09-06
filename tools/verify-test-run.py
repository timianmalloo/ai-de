#!/usr/bin/env python3
"""verify-test-run.py — fail a test run that only *looks* like it passed.

The control for defect class DC-012.

`dotnet test` prints `Passed!  - Failed: 0, Passed: 27` when the test host has **crashed partway
through**. Nothing failed — execution simply stopped — so every signal a reviewer reads is green:
exit status, the word "Passed", zero failures. The missing information is a *negative*, and negatives
are invisible unless something asserts on them. In this repo it hid 27 of 54 tests, and was caught
only because someone remembered a larger number from earlier the same day.

This asserts on the negative. For each test project it checks:

  1. a result file exists at all              — a host that died early writes none
  2. the run reports itself Completed         — not Aborted / Failed-to-complete
  3. no test was skipped unexpectedly
  4. the executed count is >= the committed baseline

(4) is what catches the silent-abort case, because an aborted run's counters are *internally
consistent* — they just describe fewer tests than exist.

Usage
  python tools/verify-test-run.py                 run the suite and verify it
  python tools/verify-test-run.py --update        re-baseline after adding tests
  python tools/verify-test-run.py --no-run        verify result files already produced

Exit 0 clean, 1 on any finding.
"""
from __future__ import annotations

import argparse
import json
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
BASELINE = REPO / "tools" / "expected-test-counts.json"
RESULTS = REPO / "artifacts" / "test-results"
NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}

# Windows consoles default to cp1252 and cannot encode the glyphs below.
for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass


def discover_projects() -> list[Path]:
    """Test projects under tests/ — not every project that happens to live there.

    `tests/` also holds HELPERS: AiDe.Core.TerminalHost is an executable the ConPTY conformance case
    launches with its own console, because ConPTY cannot attach a child from a console-less test host
    (DC-014). A helper produces no .trx, so globbing blindly makes this gate report "the test host
    almost certainly crashed" about a project that was never a test — a false alarm that trains people
    to ignore the gate, which is worse than the silence it exists to prevent.

    `<IsTestProject>false</IsTestProject>` is the declaration; it is opt-OUT, so a genuine test project
    that forgets it is still checked.
    """
    projects = []
    for project in sorted(REPO.glob("tests/*/*.csproj")):
        text = project.read_text(encoding="utf-8", errors="replace").lower()
        if "<istestproject>false</istestproject>" in text.replace(" ", ""):
            continue
        projects.append(project)

    return projects


def split_invariant(document: dict) -> list[str]:
    """Each split's halves must sum to the whole project's baseline.

    THE CONTROL THAT KEEPS "CHEAPER IS NEVER WEAKER" CHECKABLE. `AiDe.Core.Tests` is run in two
    halves on two operating systems: the portable majority on Linux and the Windows-locked minority
    on Windows. Two independent minimums would let someone quietly lower one to make a run pass,
    and the total — the number that says whether anything stopped running at all — would move with
    nobody watching. This asserts the halves still account for the whole, so the split can be
    re-balanced but the coverage cannot shrink without saying so out loud.
    """
    minimums = document.get("minimumExecuted", {})
    findings = []

    for whole, parts in document.get("splits", {}).items():
        if whole not in minimums:
            findings.append(f"split '{whole}' has no whole-project baseline to be checked against")
            continue

        missing = [p for p in parts if p not in minimums]
        if missing:
            findings.append(f"split '{whole}' names {missing}, which have no baseline")
            continue

        total = sum(minimums[p] for p in parts)
        if total != minimums[whole]:
            findings.append(
                f"split '{whole}': its halves total {total} but the whole expects "
                f"{minimums[whole]} — {abs(minimums[whole] - total)} test(s) are unaccounted for. "
                "Either a half's baseline was lowered without the total moving, or tests moved "
                "between halves and nobody said so.")

    return findings


def run_tests(projects: list[Path], filter_expr: str | None = None, key: str | None = None) -> None:
    RESULTS.mkdir(parents=True, exist_ok=True)

    # Only the results this invocation is about to replace. Deleting every .trx would make two
    # sequential runs — the App suite and one HALF of the Core suite, which is how the Windows job
    # now works — wipe each other's evidence, leaving the uploaded artifact describing whichever ran
    # last. The verification itself would still be correct; the record a human reads would not be.
    for project in projects:
        stale = RESULTS / f"{key or project.stem}.trx"
        stale.unlink(missing_ok=True)

    for project in projects:
        # Per-project runs give per-assembly counts, which is what the baseline is expressed in —
        # and it means one crashed assembly cannot mask another's numbers. `key` renames the result
        # file when only HALF a project is being run, so the two halves cannot overwrite each other.
        name = key or project.stem
        command = ["dotnet", "test", str(project), "--nologo",
                   "--logger", f"trx;LogFileName={name}.trx",
                   "--results-directory", str(RESULTS)]
        if filter_expr:
            command += ["--filter", filter_expr]

        subprocess.run(command, cwd=REPO, capture_output=True, text=True, check=False)


def read_counts(project_name: str) -> tuple[dict[str, int], str] | None:
    """Return (counters, outcome) for a project's result file, or None when it produced none."""
    trx = RESULTS / f"{project_name}.trx"
    if not trx.exists():
        return None

    root = ET.parse(trx).getroot()
    summary = root.find("t:ResultSummary", NS)
    if summary is None:
        return None

    counters = summary.find("t:Counters", NS)
    if counters is None:
        return None

    wanted = ("total", "executed", "passed", "failed", "error", "aborted", "timeout")
    return ({k: int(counters.get(k, 0)) for k in wanted}, summary.get("outcome", "Unknown"))


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--update", action="store_true",
                        help="re-baseline the expected counts from this run")
    parser.add_argument("--no-run", action="store_true",
                        help="verify existing result files instead of running the suite")
    parser.add_argument("--only", metavar="PROJECT",
                        help="run and verify one project by name (e.g. AiDe.Core.Tests)")
    parser.add_argument("--filter", dest="filter_expr", metavar="EXPR",
                        help="a dotnet test --filter, for running one HALF of a project")
    parser.add_argument("--key", metavar="NAME",
                        help="baseline key and result-file name for a filtered half "
                             "(e.g. AiDe.Core.Tests.portable)")
    args = parser.parse_args()

    if args.filter_expr and not args.key:
        print("verify-test-run: --filter needs --key, or the half would be measured against the "
              "whole project's baseline and a shortfall would read as success")
        return 1

    projects = discover_projects()

    if args.only:
        projects = [p for p in projects if p.stem == args.only]
        if not projects:
            print(f"verify-test-run: no test project named '{args.only}' — refusing to report "
                  "success over nothing")
            return 1

    if not projects:
        print("verify-test-run: no test projects found — refusing to report success over nothing")
        return 1

    document: dict = {}
    if BASELINE.exists():
        document = json.loads(BASELINE.read_text(encoding="utf-8"))
    baseline: dict[str, int] = document.get("minimumExecuted", {})

    # Checked on every invocation, including a filtered half: the halves must still account for the
    # whole, whichever side is running.
    invariant = split_invariant(document)
    if invariant and not args.update:
        print("verify-test-run: FAILED")
        for finding in invariant:
            print(f"  - {finding}")
        return 1

    if not args.no_run:
        scope = f"{args.key} ({args.filter_expr})" if args.key else f"{len(projects)} test project(s)"
        print(f"verify-test-run: running {scope}…")
        run_tests(projects, args.filter_expr, args.key)

    findings: list[str] = []
    observed: dict[str, int] = {}

    print()
    print(f"{'project':<28}{'executed':>10}{'expected':>10}{'outcome':>14}")
    print("-" * 62)

    for project in projects:
        name = args.key or project.stem
        result = read_counts(name)

        if result is None:
            # No result file, or one without counters. A host that dies early writes nothing —
            # the single loudest symptom of the defect this gate exists for.
            print(f"{name:<28}{'—':>10}{baseline.get(name, '—'):>10}{'NO RESULTS':>14}")
            findings.append(
                f"{name}: produced no usable result file — the test host almost certainly crashed")
            continue

        counters, outcome = result
        executed = counters["executed"]
        expected = baseline.get(name)
        observed[name] = executed

        flag = outcome
        if outcome != "Completed":
            findings.append(f"{name}: run outcome was '{outcome}', not 'Completed'")
            flag = f"**{outcome}**"
        if counters["failed"] or counters["error"] or counters["aborted"] or counters["timeout"]:
            findings.append(
                f"{name}: {counters['failed']} failed, {counters['error']} errored, "
                f"{counters['aborted']} aborted, {counters['timeout']} timed out")
        if expected is not None and executed < expected:
            findings.append(
                f"{name}: executed {executed} tests but the baseline expects at least {expected} — "
                f"{expected - executed} test(s) did not run. This is the silent-abort signature: "
                f"the counters are self-consistent, they just describe fewer tests than exist.")
            flag = "**SHORTFALL**"

        print(f"{name:<28}{executed:>10}{str(expected) if expected is not None else '—':>10}{flag:>14}")

    print()

    if args.update:
        BASELINE.parent.mkdir(parents=True, exist_ok=True)
        # MERGED, not replaced. A filtered run only observes its own half, and writing `observed`
        # wholesale would delete every key this invocation did not measure — including the whole
        # project's total, which is the one the split invariant is checked against.
        merged = dict(baseline)
        merged.update(observed)
        BASELINE.write_text(json.dumps({
            "_comment": (
                "Minimum tests that must EXECUTE per project. The control for defect class DC-012: "
                "a crashed test host reports success with a smaller count, and nothing else notices. "
                "Raise these with `python tools/verify-test-run.py --update` when you add tests; "
                "never lower one to make a run pass. Keys with a suffix are HALVES of a project run "
                "on one OS; `splits` says which halves must account for which whole."
            ),
            "minimumExecuted": merged,
            "splits": document.get("splits", {}),
        }, indent=2) + "\n", encoding="utf-8")
        observed = merged
        print(f"verify-test-run: baseline updated → {BASELINE.relative_to(REPO)}")
        for name, count in sorted(observed.items()):
            print(f"  {name}: {count}")
        return 0

    if findings:
        print("verify-test-run: FAILED")
        for finding in findings:
            print(f"  - {finding}")
        return 1

    total = sum(observed.values())
    print(f"verify-test-run: OK — {total} tests executed across {len(observed)} project(s), "
          f"every project met its baseline.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
