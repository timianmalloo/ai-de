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
   work**. Restoration can always be skipped; inherited damage can always be detected.

   Commit or set aside your work before running this. That is not a courtesy to the tool.

3. A MARKER FILE, because guards 1 and 2 both protect the NEXT run and neither survives the kill.
   This was found the hard way, in the other session, twenty minutes after we agreed guard 2 was
   sufficient: a 10-minute CI-style cap SIGTERMed a replay, `finally` never ran, and a mutation was
   left live in the tree with everything still compiling.

   A kill can skip a `finally`. It cannot un-write a file that already exists. So the marker is
   written BEFORE the edit and deleted AFTER the restore, and a run that finds one on start heals
   the named path before doing anything else. That turns the previous run's damage from *detectable*
   into *self-healing*, and it is what makes this safe to gate on every push — without it, the first
   cancelled CI job leaves a live mutation, which is a defect INTRODUCED by the verification and
   strictly worse than the one it exists to catch.

   It also sharpens guard 2, which alone cannot tell "someone is working" from "a killed run left a
   mutation": a marker names the path, so named dirt is restored and unnamed dirt is still refused.

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

# Written before an edit, deleted after the restore. The one guard that survives a kill, because a
# kill cannot un-write a file that already exists. Git-ignored: it is machine-local crash state, and
# committing it would make one machine's interrupted run everyone else's problem.
MARKER = ROOT / ".mutation-in-progress"

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


def heal_from_marker() -> str | None:
    """Restore a path a killed run left mutated. Returns the path healed, or None.

    Runs BEFORE the dirty check, so a previous run's damage is self-healing rather than a refusal
    the operator has to diagnose. A marker naming a path this tool can restore is not "someone is
    working" — it is crash state, and the difference is exactly what guard 2 alone cannot see.
    """
    if not MARKER.exists():
        return None

    try:
        path = json.loads(MARKER.read_text(encoding="utf-8")).get("path")
    except (OSError, json.JSONDecodeError):
        path = None

    if not path:
        # A marker we cannot read still means a run died. Say so and refuse rather than guess which
        # file to restore — a wrong `git checkout --` discards real work.
        print("mutation-replay: a marker from an interrupted run exists but names no path.")
        print(f"  Inspect {MARKER.name} and `git status` by hand, then delete it.")
        return "?"

    print(f"mutation-replay: an interrupted run left {path} mutated. Restoring it.")
    git("checkout", "--", path)
    MARKER.unlink(missing_ok=True)
    return path


def mark(path: str) -> None:
    MARKER.write_text(
        json.dumps({"path": path, "started": _now()}), encoding="utf-8")


def _now() -> str:
    from datetime import datetime, timezone
    return datetime.now(timezone.utc).isoformat()


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

    return parse_run((proc.stdout or "") + (proc.stderr or ""))


def parse_run(out: str) -> tuple[set[str], int, str]:
    """Read a dotnet-test transcript. Separated so `--self-test` can exercise it without a build.

    THE GUARD THIS EXISTS FOR: absence of a summary is a HARNESS FAILURE, never "no test failed".
    Its own first version was written the other way and reported seven coverage gaps that did not
    exist.
    """
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


def self_test() -> int:
    """Prove this tool's own guards can fire, without building anything.

    DC-104: a new control's first run is not a verification, it is the first test of the CONTROL —
    and three times in one day here, the first run found a defect in the control rather than in the
    code. This tool was wired into build.yml with no self-test at all, which is that class committed
    by the author who registered it.

    Each case below is a transcript or input that MUST be refused. A tool that cannot demonstrate its
    own refusals is asking to be trusted on the strength of never having complained.
    """
    failures: list[str] = []

    def check(label: str, condition: bool) -> None:
        print(f"  {'ok  ' if condition else 'FAIL'}  {label}")
        if not condition:
            failures.append(label)

    # 1. The guard whose absence produced seven imaginary coverage gaps.
    _, _, err = parse_run("")
    check("empty output is a harness failure, not a clean run", "NO TEST SUMMARY" in err)

    _, _, err = parse_run("dotnet: command not found")
    check("a missing runner is a harness failure", "NO TEST SUMMARY" in err)

    # 2. A run that happened and executed nothing is also not a pass.
    _, _, err = parse_run("Passed!  - Failed: 0, Passed: 0, Skipped: 0, Total: 0, Duration: 1 ms")
    check("zero executed tests is refused", "0 tests executed" in err)

    # 3. A compile break is named as itself rather than as a passing sweep.
    _, _, err = parse_run("Foo.cs(1,1): error CS1002: ; expected")
    check("a compile error is reported as one", "did not compile" in err)

    # 4. A real transcript parses, and failures are attributed to their class.
    fails, total, err = parse_run(
        "    AiDe.Core.Tests.Watcher.ThingTests.ItWorks [FAIL]\n"
        "Failed!  - Failed: 1, Passed: 9, Skipped: 0, Total: 10, Duration: 5 ms")
    check("a real transcript parses", err == "" and total == 10 and "ThingTests.ItWorks" in fails)

    # 5. DC-103's preflight fires when a covering test sits outside the filter, and is quiet when it
    #    does not. Both directions: a check that only ever passes is decoration (DC-016).
    real = [{"file": "src/AiDe.Core/Watcher/DaydreamReachProbe.cs", "old": "", "new": ""}]
    check("a filter that excludes a covering test is a gap",
          len(scope_gaps(real, "FullyQualifiedName~NothingMatchesThis")) > 0)
    # Both tokens, which is the filter the set actually carries. Written first with only ~Daydream,
    # and it FAILED on this tool's very first self-test run — correctly, because
    # WhatTheRealCorpusCanProduceTests names the probe and does not contain "Daydream", so it is a
    # real gap under that filter. The code was right and the assertion was wrong. DC-104 arriving
    # inside the self-test written to demonstrate DC-104, on its first execution.
    check("a filter that reaches its tests is not a gap",
          scope_gaps(real, "FullyQualifiedName~Daydream|FullyQualifiedName~WhatTheRealCorpus") == [])

    # 6. And a cross-boundary mutation is exempt — the false positive that would have got this gate
    #    switched off inside a day, found only by running it.
    crossed = [dict(real[0], tests={"project": "x", "filter": "y"})]
    check("a cross-boundary mutation is exempt from the scope check",
          scope_gaps(crossed, "FullyQualifiedName~NothingMatchesThis") == [])

    print()
    if failures:
        print(f"mutation-replay --self-test: {len(failures)} guard(s) did not fire.")
        return 1

    print("mutation-replay --self-test: every guard fires, and the scope check is quiet when clean.")
    return 0


def scope_gaps(mutations: list[dict], test_filter: str) -> list[tuple[str, str, str]]:
    """Test files that exercise a mutated type but sit OUTSIDE the filter.

    DC-103: a verification whose scope is NAMED rather than derived drifts out of date silently, and
    its report does not say so. This set's filter was `FullyQualifiedName~Daydream`; a new test class
    called `WhatTheRealCorpusCanProduceTests` was therefore outside the sweep whose entire job is
    proving controls can fail — while the report still read "18 mutations, 0 uncovered". The newest
    and least-proven code was the part not covered, and nothing warned.

    The obvious fix is to drop the filter and run everything, which is what the sibling harness does.
    MEASURED here rather than assumed: an unfiltered Core run is 71s, so 18 mutations would take ~21
    minutes against the filtered 74s. That is not an every-push gate, and the cheapest minute is the
    one never billed (CE). So the filter stays and the SILENCE goes: for every type this set mutates,
    any test file naming that type must be selectable by the filter.

    Derived from the mutated files, not from a second list to keep in step — restating the list is
    the defect one level down (DC-021).
    """
    tokens = [t.strip() for t in re.findall(r"FullyQualifiedName~([^|&]+)", test_filter)]
    if not tokens:
        return []

    gaps: list[tuple[str, str, str]] = []
    seen: set[tuple[str, str]] = set()

    for m in mutations:
        # A mutation with an explicit `tests` scope has CHOSEN to run something other than its own
        # file's tests — that is the whole cross-boundary mode. Checking it against the default
        # filter would flag every test of the mutated component as a gap, which is the opposite of
        # true: those belong to that component's own sweep. Observed doing exactly that on the
        # WeaveScore mutation before this line existed.
        if m.get("tests"):
            continue

        type_name = Path(m["file"]).stem
        for test_file in (ROOT / "tests").rglob(f"*{'Tests'}.cs"):
            if "bin" in test_file.parts or "obj" in test_file.parts:
                continue
            try:
                if type_name not in test_file.read_text(encoding="utf-8", errors="replace"):
                    continue
            except OSError:
                continue

            cls = test_file.stem
            if any(tok in cls for tok in tokens):
                continue
            if (cls, type_name) in seen:
                continue
            seen.add((cls, type_name))
            gaps.append((cls, type_name, str(test_file.relative_to(ROOT))))

    return gaps


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
    ap.add_argument("--self-test", action="store_true",
                    help="prove this tool's own guards can fire; builds nothing")
    args = ap.parse_args()

    if args.self_test:
        return self_test()

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

    # GUARD 3 FIRST, because it is the one that survives a kill and it makes guard 1 answerable.
    # A marker names the file a dead run left mutated, so that dirt is healed rather than refused.
    if heal_from_marker() == "?":
        return 2

    # GUARD 1. Two reasons, and the second is the one that makes it non-negotiable: this tool
    # restores with `git checkout --`, so running it on a dirty tree DESTROYS uncommitted work.
    # (The first reason is that a previous run killed mid-mutation leaves the mutation live — which
    # guard 3 above has now already handled, so anything still dirty here is a person's work.)
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

    # DC-103. Before measuring anything, check the sweep is aimed at everything it should be.
    if (gaps := scope_gaps(mutations, default_filter)):
        print("mutation-replay: SCOPE GAP — a test exercises mutated code and the filter excludes it.")
        print(f"  filter: {default_filter}")
        for cls, type_name, path in gaps:
            print(f"  {cls} names {type_name} but is not selected  ({path})")
        print()
        print("These tests sit outside the sweep that proves controls can fail, and the report would")
        print("still have said '0 uncovered'. Widen the set's filter, or move the mutation.")
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
            # Marker BEFORE the edit, never after. Between these two statements is the only window
            # where a kill loses information, and it is one filesystem write wide.
            mark(rel)
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
            # The marker is cleared only AFTER the restore, so a kill in between still heals.
            git("checkout", "--", rel)
            MARKER.unlink(missing_ok=True)

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
        print()
        print("NO TEST FAILED is two different findings and this tool cannot tell them apart:")
        print()
        print("  UNCOVERED   the behaviour is real and nothing checks it. Write the test.")
        print("  EQUIVALENT  the mutation cannot change behaviour, so no test could ever redden.")
        print("              Then the LINE is the defect, not the suite — a branch nothing can")
        print("              reach is not defence in depth, it is a permanent false entry that")
        print("              trains the next reader to ignore one. Delete it.")
        print()
        print("Deciding which is the author's judgement and it is the point of the run. Silently")
        print("excluding an equivalent is how a mutation set stops meaning anything — if one is")
        print("genuinely equivalent, remove the CODE and the mutation with it, never just the")
        print("mutation. (Both kinds were found in one sweep on 2026-09-03: a fixture whose two")
        print("paths were equal so a fallback produced the identical answer, and a `Directory.Exists")
        print("(p) || !File.Exists(p)` whose first half can never change the outcome.)")
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
