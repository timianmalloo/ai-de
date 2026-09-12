#!/usr/bin/env python3
"""verify-terminal-host-exit-paths.py — the five terminal-host exit paths are a gate, not a comment.

The control for defect class DC-154 (a resource acquired for a child is released with the owner,
not with the child) and the fourth control of DC-131 (INV-0010): every path by which a terminal
session ends is MEASURED to leave no `conhost.exe --headless` behind, and the measurement is read
here by name so that a regression on any one path fails the build by a count rather than by
someone re-reading a process list for a sixth report.

    window close   AppWindowCloseLeavesNoTerminalHostTests.ClosingTheMainWindow_...     (App, real binary)
    owner exit     TerminalHostExitPathTests.AnOwnerThatExitsWithoutDisposing_...      (Core, helper)
    owner killed   TerminalHostExitPathTests.AnOwnerThatIsKilled_...                   (Core, helper)
    tab close      TerminalHostInLifePathTests.ADisposedSession_...                    (Core, helper)
    child exit     TerminalHostInLifePathTests.ASessionWhoseChildExited_...            (Core, helper)

WHY A READER AND NOT A SECOND RUN. The five already execute inside the Windows job's test step
(`verify-test-run.py`, which asserts the run completed and the counts held). Running them again
would bill the same minutes twice for the same evidence (CE9). What that step cannot say is
whether THESE FIVE ran and passed: its baseline is a count, and a count is satisfied by any five
tests. This reads the .trx files that step produced and requires each path by name — absent is
a failure, not a pass, because a test that was renamed, filtered out or skipped is exactly how a
measured path silently stops being measured.

Usage
  python tools/verify-terminal-host-exit-paths.py                  read artifacts/test-results/*.trx
  python tools/verify-terminal-host-exit-paths.py --results DIR    read another directory
  python tools/verify-terminal-host-exit-paths.py --self-test      prove the gate can fail (DC-104)

Exit 0 when all five are present and Passed, 1 otherwise.
"""
from __future__ import annotations

import argparse
import sys
import tempfile
import xml.etree.ElementTree as ET
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
RESULTS = REPO / "artifacts" / "test-results"
NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}

# Fully qualified, one per exit path. Adding a path means adding a line here AND the test.
EXIT_PATHS = {
    "window close": "AiDe.App.Tests.AppWindowCloseLeavesNoTerminalHostTests.ClosingTheMainWindow_LeavesNoHeadlessHostFiveSecondsLater",
    "owner exit": "AiDe.Core.Tests.TerminalHostExitPathTests.AnOwnerThatExitsWithoutDisposing_LeavesNoHeadlessHostFiveSecondsLater",
    "owner killed": "AiDe.Core.Tests.TerminalHostExitPathTests.AnOwnerThatIsKilled_LeavesNoHeadlessHostFiveSecondsLater",
    "tab close": "AiDe.Core.Tests.TerminalHostInLifePathTests.ADisposedSession_ReleasesItsHeadlessHostWhileTheOwnerLives",
    "child exit": "AiDe.Core.Tests.TerminalHostInLifePathTests.ASessionWhoseChildExited_ReleasesItsHeadlessHostWhileTheOwnerLives",
}

# Windows consoles default to cp1252 and cannot encode the glyphs below.
for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass


def outcomes(results: Path) -> dict[str, str]:
    """testName -> outcome across every .trx in the directory. A test seen twice keeps its WORST
    outcome: a stale local .trx that sorts later must not turn a Failed into a Passed."""
    seen: dict[str, str] = {}
    for trx in sorted(results.glob("*.trx")):
        try:
            root = ET.parse(trx).getroot()
        except ET.ParseError:
            continue
        for result in root.iter(f"{{{NS['t']}}}UnitTestResult"):
            name = result.get("testName")
            if not name:
                continue
            outcome = result.get("outcome", "")
            if name not in seen or seen[name] == "Passed":
                seen[name] = outcome
    return seen


def check(results: Path) -> list[str]:
    """One finding per exit path that did not run or did not pass; empty when the gate holds."""
    if not results.is_dir() or not any(results.glob("*.trx")):
        return [f"no .trx results under {results} — the test step did not run, so no exit path was measured"]
    seen = outcomes(results)
    findings = []
    for path, test in EXIT_PATHS.items():
        outcome = seen.get(test)
        if outcome is None:
            findings.append(f"{path}: {test} was NOT EXECUTED — a path that is not measured is not clean")
        elif outcome != "Passed":
            findings.append(f"{path}: {test} outcome {outcome} — a headless host survived, or the key could not see one")
    return findings


def _trx(directory: Path, name: str, rows: dict[str, str]) -> None:
    ns = NS["t"]
    lines = [f'<?xml version="1.0" encoding="utf-8"?><TestRun xmlns="{ns}"><Results>']
    for test, outcome in rows.items():
        lines.append(f'<UnitTestResult testId="x" testName="{test}" outcome="{outcome}" />')
    lines.append("</Results></TestRun>")
    (directory / name).write_text("".join(lines), encoding="utf-8")


def self_test() -> int:
    """DC-104: a gate that has never been seen red is a belief. Four directions, all named."""
    failures = []

    def expect(label: str, got: object, want: object) -> None:
        if got != want:
            failures.append(f"{label}: got {got!r}, wanted {want!r}")

    with tempfile.TemporaryDirectory() as tmp:
        d = Path(tmp)
        # 1. nothing measured at all is a failure, not a pass.
        expect("no results is a finding", len(check(d)), 1)

        # 2. all five present and Passed, split across two files the way the Windows job writes them.
        core = {t: "Passed" for p, t in EXIT_PATHS.items() if t.startswith("AiDe.Core")}
        app = {t: "Passed" for p, t in EXIT_PATHS.items() if t.startswith("AiDe.App")}
        _trx(d, "AiDe.Core.Tests.nonportable.trx", core)
        _trx(d, "AiDe.App.Tests.trx", app)
        expect("five passed paths is clean", check(d), [])

        # 3. one path FAILED: the gate fires and names the path.
        broken = dict(core)
        broken[EXIT_PATHS["child exit"]] = "Failed"
        _trx(d, "AiDe.Core.Tests.nonportable.trx", broken)
        found = check(d)
        expect("a failed path is one finding", len(found), 1)
        expect("and it names the path", found[0].startswith("child exit:"), True)

        # 4. one path ABSENT (renamed, filtered, skipped): the gate fires — absent is not clean.
        _trx(d, "AiDe.Core.Tests.nonportable.trx", core)
        _trx(d, "AiDe.App.Tests.trx", {})
        found = check(d)
        expect("an unexecuted path is one finding", len(found), 1)
        expect("and it says so", "NOT EXECUTED" in found[0] and found[0].startswith("window close:"), True)

        # 5. a name seen twice keeps its worst outcome: a stale Passed in a later-sorting file must
        #    not override the Failed the real run wrote.
        _trx(d, "AiDe.App.Tests.trx", app)
        _trx(d, "AiDe.Core.Tests.nonportable.trx", broken)
        _trx(d, "AiDe.Core.Tests.trx", core)  # sorts after "…nonportable.trx"; all Passed
        found = check(d)
        expect("a later Passed does not hide an earlier Failed", len(found), 1)

    if failures:
        print("self-test FAILED — the gate's own oracle is wrong:", file=sys.stderr)
        for f in failures:
            print("  " + f, file=sys.stderr)
        return 1
    print("self-test OK — the gate fires on a failed path, on an unexecuted path, and on no results at all")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--results", metavar="DIR", default=str(RESULTS),
                        help="the directory holding the .trx files (default: artifacts/test-results)")
    parser.add_argument("--self-test", action="store_true", help="prove this gate can fail")
    args = parser.parse_args()

    if args.self_test:
        return self_test()

    findings = check(Path(args.results))
    if findings:
        print("verify-terminal-host-exit-paths: FAILED")
        for f in findings:
            print("  " + f)
        return 1
    print(f"verify-terminal-host-exit-paths: OK — {len(EXIT_PATHS)} exit paths executed and passed "
          "(window close · owner exit · owner killed · tab close · child exit → 0 headless hosts)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
