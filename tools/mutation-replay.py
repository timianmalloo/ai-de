#!/usr/bin/env python3
"""Break the code on purpose and check that a test notices — including across a component boundary.

WHY THIS EXISTS. A test written in the same edit as its fix has never been shown capable of failing.
It is green from birth, which is not evidence of anything (DC-099). On 2026-09-02 two sessions
replayed their own controls this way and found, between them: a test that had only ever passed
vacuously, a comment asserting a rule nothing checked, a clamp hiding a real state, and a coupling
across two components that neither side could see from its own code.

    A control is a control when it has been observed RED on the defect it claims to prevent.
    Until then it is a decoration with a good name.

THE CROSS-BOUNDARY MODE IS THE PART THAT IS HARD TO GET ANY OTHER WAY. A sweep run by the owner of
component A mutates only A's code and runs only A's tests, so a dependency that runs A -> B is
invisible to it, and equally invisible to B's own sweep. Every real instance of that in this
repository was found by mutating one side and running the OTHER side's tests:

    unevidenced episode trips a floor   ->  reddens the Daydream recorder's tests, not just scoring's
    scorer stops refusing to fabricate  ->  DaydreamObservationOutcome silently stops distinguishing

Declare `tests` on a mutation to run a different scope than the file's own, and the report names
which side reddened.

TWO GUARDS, BOTH LEARNED BY LOSING THEM.

1. A run that produces no parseable test summary is a HARNESS FAILURE, never "no test failed". The
   first version of a sweep here reported seven coverage gaps that did not exist: `shell=True` meant
   cmd.exe, `dotnet` was not on its PATH, every command returned empty, and empty was read as clean.
   A guard against examining nothing cannot tell you WHY nothing was examined, so it has to say
   which.

2. Refusing to start on a dirty tree matters more than restoring cleanly (DC-101), and for a worse
   reason than the one it was designed for. The design reason: a sweep killed mid-test leaves the
   mutation LIVE, everything still compiles, and every later measurement is silently about the wrong
   code. The reason found in use, within a day, is that this tool restores with `git checkout --` —
   so on a dirty tree it would not merely confuse a measurement, it would **destroy uncommitted
   work**. Restoration can always be skipped; inherited damage can always be detected. So: refuse
   first, restore from git second, and never from a copy in memory.

   Commit or set aside your work before running this. That is not a courtesy to the tool.

USAGE
    python tools/mutation-replay.py --set tools/mutation-sets/daydream-seam.json
    python tools/mutation-replay.py --set <file> --only "decline rule"   # one mutation, by label
    python tools/mutation-replay.py --list-sets

EXIT CODES
    0  every mutation reddened at least one test
    1  one or more mutations were UNCOVERED, or the tree was dirty afterwards
    2  refused to start (dirty tree, unreadable set, drifted source)

A MUTATION SET is JSON:

    { "project": "tests/AiDe.Core.Tests/AiDe.Core.Tests.csproj",
      "filter":  "FullyQualifiedName~Daydream",
      "mutations": [
        { "label": "...", "file": "src/...cs", "old": "...", "new": "...",
          "tests": { "project": "...", "filter": "..." } }   // optional: cross-boundary
      ] }

`old` must appear in the file exactly once-or-more; if it is absent the run REFUSES rather than
reporting the control uncovered, because a drifted source that mutates nothing is indistinguishable
from a test that catches everything.

Stdlib only.
"""
from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SETS = ROOT / "tools" / "mutation-sets"

# Encoding pinned both ways. Reading a tool's output as cp1252 raised UnicodeDecodeError inside a
# reader thread once and presented as "the detector scanned nothing"; printing U+FFFD to a narrow
# console raises UnicodeEncodeError and kills the run. Third and fourth instances of one class.
for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

FAIL_RE = re.compile(r"\s([\w.]+\.(\w+))\.(\w+) \[FAIL\]")
SUMMARY_RE = re.compile(
    r"(Passed!|Failed!)\s+-\s+Failed:\s+(\d+),\s+Passed:\s+(\d+).*?Total:\s+(\d+)")


def git(*args: str) -> subprocess.CompletedProcess:
    return subprocess.run(["git", *args], cwd=str(ROOT), capture_output=True,
                          text=True, encoding="utf-8", errors="replace", timeout=180)


def dirty() -> str:
    return git("status", "--short").stdout.strip()


def run_tests(project: str, test_filter: str) -> tuple[set[str], int, str]:
    """(failing test names, total executed, error). No summary is an ERROR, never a pass."""
    cmd = ["dotnet", "test", project, "--nologo"]
    if test_filter:
        cmd += ["--filter", test_filter]

    try:
        proc = subprocess.run(cmd, cwd=str(ROOT), capture_output=True, text=True,
                              encoding="utf-8", errors="replace", timeout=3600)
    except (OSError, subprocess.SubprocessError) as exc:
        return set(), 0, f"could not run dotnet test — {exc}"

    out = (proc.stdout or "") + (proc.stderr or "")

    if "error CS" in out:
        first = next((l.strip() for l in out.splitlines() if "error CS" in l), "")
        return set(), 0, f"did not compile — {first[:160]}"

    match = SUMMARY_RE.search(out)
    if not match:
        tail = " / ".join(l.strip() for l in out.strip().splitlines()[-3:])
        return set(), 0, f"NO TEST SUMMARY — the run did not happen. Tail: {tail[:220]}"

    total = int(match.group(4))
    if total == 0:
        return set(), 0, "the summary reported 0 tests executed — the filter matched nothing"

    return {f"{m[1]}.{m[2]}" for m in FAIL_RE.findall(out)}, total, ""


def load_set(path: Path) -> dict:
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise SystemExit(f"mutation-replay: cannot read {path} — {exc}")

    if not data.get("mutations"):
        raise SystemExit(f"mutation-replay: {path} declares no mutations")
    return data


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--set", dest="set_path", help="path to a mutation set (JSON)")
    ap.add_argument("--only", help="run only mutations whose label contains this text")
    ap.add_argument("--list-sets", action="store_true", help="list the declared mutation sets")
    args = ap.parse_args()

    if args.list_sets:
        for f in sorted(SETS.glob("*.json")):
            spec = json.loads(f.read_text(encoding="utf-8"))
            print(f"  {f.relative_to(ROOT)}  —  {len(spec.get('mutations', []))} mutation(s): "
                  f"{spec.get('description', '(no description)')}")
        return 0

    if not args.set_path:
        ap.error("--set is required (or --list-sets)")

    spec = load_set(Path(args.set_path))
    default_project = spec.get("project", "")
    default_filter = spec.get("filter", "")

    mutations = spec["mutations"]
    if args.only:
        mutations = [m for m in mutations if args.only.lower() in m["label"].lower()]
        if not mutations:
            raise SystemExit(f"mutation-replay: no mutation label contains {args.only!r}")

    # GUARD 1. Two reasons, and the second is the one that makes it non-negotiable: this tool
    # restores with `git checkout --`, so running it on a dirty tree DESTROYS uncommitted work.
    # (The first reason is that a previous run killed mid-mutation leaves the mutation live.)
    if (d := dirty()):
        print("mutation-replay: REFUSING TO START — the tree is dirty.")
        print("This tool restores with `git checkout --`, so running it here would DISCARD the")
        print("changes below. Commit them, or set them aside, first. A dirty tree can also mean a")
        print("previous run died mid-mutation and left one live — check before assuming otherwise.")
        print(d)
        return 2

    # Every `old` must be present BEFORE anything is touched. A drifted source that mutates nothing
    # reports as a test catching everything, which is the most flattering possible wrong answer.
    for m in mutations:
        text = (ROOT / m["file"]).read_text(encoding="utf-8")
        if m["old"] not in text:
            print(f"mutation-replay: REFUSING TO START — source drifted for {m['label']!r}.")
            print(f"  {m['file']} no longer contains the text this mutation replaces.")
            print("  Update the set; do not let it run and report the control as covered.")
            return 2

    print("baseline:", end=" ", flush=True)
    fails, total, err = run_tests(default_project, default_filter)
    if err:
        print(f"UNUSABLE — {err}")
        return 2
    if fails:
        print(f"NOT GREEN — {len(fails)} failing; nothing below would mean anything")
        return 2
    print(f"{total} tests green\n")

    uncovered: list[str] = []
    unmeasured: list[str] = []

    for m in mutations:
        rel = m["file"]
        path = ROOT / rel
        original = path.read_text(encoding="utf-8")
        scope = m.get("tests", {})
        project = scope.get("project", default_project)
        test_filter = scope.get("filter", default_filter)
        crossed = bool(scope)

        try:
            path.write_text(original.replace(m["old"], m["new"], 1), encoding="utf-8", newline="")
            fails, total, err = run_tests(project, test_filter)

            arrow = " ->" if crossed else "   "
            if err:
                print(f"  !!{arrow} {m['label']}: {err}")
                unmeasured.append(m["label"])
            elif not fails:
                print(f"   0{arrow} red / {total}: {m['label']}   <-- UNCOVERED")
                uncovered.append(m["label"])
            else:
                print(f"  {len(fails):>2}{arrow} red / {total}: {m['label']}")
                for name in sorted(fails):
                    print(f"        {name}")
        finally:
            # GUARD 2. From git, never from `original` — a restore that trusts memory cannot be
            # verified, and this is the step that runs while something is already going wrong.
            git("checkout", "--", rel)

    print()
    if (d := dirty()):
        print("mutation-replay: !!! TREE DIRTY AFTER THE RUN — a restore did not complete:")
        print(d)
        return 1

    print("tree clean; every mutation restored via git checkout.")
    print(f"\n{len(mutations)} mutation(s) · {len(uncovered)} uncovered · {len(unmeasured)} not measured")
    for label in uncovered:
        print(f"  UNCOVERED: {label}")
    for label in unmeasured:
        print(f"  NOT MEASURED: {label}")

    if uncovered or unmeasured:
        print("\nAn uncovered mutation is not automatically a missing test. Ask what the mutated")
        print("line is FOR: sometimes the answer is that the behaviour was never modelled, which is")
        print("a finding about the design rather than about the suite.")
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
