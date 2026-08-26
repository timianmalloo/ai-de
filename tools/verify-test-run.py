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
    return sorted(REPO.glob("tests/*/*.csproj"))


def run_tests(projects: list[Path]) -> None:
    RESULTS.mkdir(parents=True, exist_ok=True)
    for stale in RESULTS.glob("*.trx"):
        stale.unlink()

    for project in projects:
        name = project.stem
        # Per-project runs give per-assembly counts, which is what the baseline is expressed in —
        # and it means one crashed assembly cannot mask another's numbers.
        subprocess.run(
            ["dotnet", "test", str(project), "--nologo",
             "--logger", f"trx;LogFileName={name}.trx",
             "--results-directory", str(RESULTS)],
            cwd=REPO, capture_output=True, text=True, check=False,
        )


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
    args = parser.parse_args()

    projects = discover_projects()
    if not projects:
        print("verify-test-run: no test projects found — refusing to report success over nothing")
        return 1

    if not args.no_run:
        print(f"verify-test-run: running {len(projects)} test project(s)…")
        run_tests(projects)

    baseline: dict[str, int] = {}
    if BASELINE.exists():
        baseline = json.loads(BASELINE.read_text(encoding="utf-8")).get("minimumExecuted", {})

    findings: list[str] = []
    observed: dict[str, int] = {}

    print()
    print(f"{'project':<28}{'executed':>10}{'expected':>10}{'outcome':>14}")
    print("-" * 62)

    for project in projects:
        name = project.stem
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
        BASELINE.write_text(json.dumps({
            "_comment": (
                "Minimum tests that must EXECUTE per project. The control for defect class DC-012: "
                "a crashed test host reports success with a smaller count, and nothing else notices. "
                "Raise these with `python tools/verify-test-run.py --update` when you add tests; "
                "never lower one to make a run pass."
            ),
            "minimumExecuted": observed,
        }, indent=2) + "\n", encoding="utf-8")
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
