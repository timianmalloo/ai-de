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
  3. every NotExecuted result is NAMED in the committed `expectedSkips` list
  4. the TOTAL count is >= the committed baseline

(4) is what catches the silent-abort case, because an aborted run's counters are *internally
consistent* — they just describe fewer tests than exist.

`total`, NOT `executed`, AND WHY THAT IS THE STRONGER FLOOR. Measured on CI run 35228503081's own
.trx artefacts against local runs of the same tree: `total` is equal on every key (App 1,053,
portable 2,575, nonportable 181) while `executed` differs on two — CI's nonportable job executes 177
of 181, local executes 181 of 181. The whole delta is the *dynamically skipped* tests, which a .trx
records as NotExecuted: **inside `total`, outside `executed`**. So `executed` varies with the
operating system and `total` does not, and a floor set from one machine's `executed` is unmeetable on
the other — which is what happened: two keys sat above anything CI could produce. `total` still falls
on the defect this gate exists for, because a host that dies partway writes fewer *results*. And
`total >= floor` alone would pass a run that skipped everything dynamically, so (3) closes that:
`total >= floor` PLUS `NotExecuted ⊆ expectedSkips` is strictly stronger than the `executed >= floor`
it replaces, and it is meetable on every machine.

`--update` REFUSES TO LOWER. Merging the observed counts over the baseline with no comparison is how
the floor was lost: the join runs `--update` three times before its check, so whichever machine
joined last silently re-set the floor to its own run. On 2026-09-17 the baseline sat 8 Core and 2 App
tests below reality and nothing fired. `--update` now refuses any key below its **committed** value
and prints the key, the old value and the new; `--allow-lower "<reason>"` is the written-down way
through, for the case where tests were genuinely removed. docs/coordination/join.json deliberately
does NOT pass it.

A FAILED report NAMES the tests it counted: every non-passed result in the .trx, with the first
line of its error message. Measured cost of not doing so: this gate was red on main for a day
saying only "1 failed", and recovering the name took a full local test run — from a file this tool
had already opened and parsed.

`--refuse-upward-drift BASE` adds the join's half of the same defect class: a floor-only baseline
drifts UP silently (executed 2,575 against an expected 2,567, because two landings added tests
without raising the floor) and every test of that gap is a silent abort nobody can see. CI's floor
semantics are deliberately unchanged — see the comment on `baseline_in`.

Usage
  python tools/verify-test-run.py                 run the suite and verify it
  python tools/verify-test-run.py --update        re-baseline after adding tests
  python tools/verify-test-run.py --no-run        verify result files already produced
  python tools/verify-test-run.py --update --allow-lower "REASON"
                                                  re-baseline DOWN, saying why
  python tools/verify-test-run.py --no-run --refuse-upward-drift HEAD^1
                                                  the join's line: also refuse an unmoved baseline

Exit 0 clean, 1 on any finding.
"""
from __future__ import annotations

import argparse
import json
import os
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
    minimums = document.get("minimumTotal", {})
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

        # MSBuild's worker nodes outlive the driver by design: `dotnet test` starts them with
        # /nodeReuse:true so the next build can reuse them. The fifteen-minute idle timeout this
        # comment used to cite as their lifetime IS WRONG FOR ORPHANS, and the correction was
        # measured rather than reasoned: on 2026-09-11 a cohort of sixteen spawned at 06:22 was
        # still standing at 07:40 -- 1.3 hours, zero CPU, parent long dead. Treat their lifetime as
        # UNBOUNDED. When the driver is killed rather than
        # allowed to exit -- a cancelled CI job, a closed terminal, an agent session that ends --
        # the workers survive it and nothing is left that names them. COUNTS, with their
        # provenance: the investigation that opened this node found SIXTEEN such orphans; TWO were
        # still standing when the fix was written, both `/nodeReuse:true`, both children of a
        # process id that no longer existed. The MECHANISM is verified. ATTRIBUTION of any one
        # orphan to any one command is INFERRED and cannot be recovered afterwards -- a worker's
        # command line is `/nodemode:1 /nodeReuse:true` and names no project, no repository and no
        # worktree, so there is nothing in it to attribute by.
        #
        # Set in the ENVIRONMENT and not on the command line: this gate's argv is read by other
        # things, and an extra flag there is a change to a contract. MSBUILDDISABLENODEREUSE is
        # the documented switch and it is already the shape used at
        # spikes/extraction-containment/LowIntegrity.cs:145.
        #
        # THIS COVERS THIS GATE ONLY, and that was the defect: the sixteen orphans above came from
        # `dotnet build` run by hand, which never reaches this line. The boundary control is
        # Directory.Build.rsp at the repository root, pinned by tools/verify-node-reuse-control.py.
        # Both are kept -- this one survives a shadowing nested response file, that one covers every
        # invocation this gate never sees.
        environment = dict(os.environ)
        environment["MSBUILDDISABLENODEREUSE"] = "1"

        subprocess.run(
            command, cwd=REPO, capture_output=True, text=True, encoding="utf-8", errors="replace", check=False, env=environment)

    retire_build_servers()


def retire_build_servers() -> None:
    """Retire the .NET build servers this run started -- but only if the machine is quiet.

    MEASURED 2026-09-11, and it is the third ring of the same defect. The environment
    variable above retires MSBuild WORKER reuse. It says nothing about the ROSLYN COMPILER
    SERVER, which is a separate server with its own lifetime and its own switch. A full
    `verify-test-run.py` leaves EXACTLY ONE process behind -- `VBCSCompiler.exe`, orphaned,
    holding a `conhost.exe`, command line `-pipename:<base64>`: no project, no repository,
    no worktree, nothing to attribute it by. It is the operator's straggler, and it was
    the only one of 547 host-like processes that turned out to be ours.

    `dotnet build-server shutdown` is the DOCUMENTED mechanism and is preferred over
    killing anything. Measured 1 -> 0.

    THE GUARD IS NOT OPTIONAL. That command is per-USER, not per-worktree: it retires the
    servers of every concurrent build on the machine. Several agent sessions build here at
    once, in different worktrees -- that is normal and it is how this whole defect was
    measured. Retiring a peer's compiler server mid-build is a worse defect than the one
    being fixed, so the reaper's own idle check decides, and a busy machine is left alone.
    The `--assert-clean` gate still fails in that case, which is the honest outcome: the
    straggler is real, and this run was not in a position to clear it.

    NOT `UseSharedCompilation=false`, which would also stop the server existing: shared
    compilation is a large build-time win and the cost of losing it is UNMEASURED. Trading
    it away to avoid one process would be exactly the hunch this repository keeps
    recording.
    """
    reaper = REPO / "tools" / "reap-stragglers.py"
    if not reaper.exists():
        return
    subprocess.run([sys.executable, str(reaper), "--reap"],
                   cwd=REPO, capture_output=True, text=True, encoding="utf-8", errors="replace", check=False)


def non_passed(root: ET.Element) -> list[tuple[str, str, str]]:
    """Every UnitTestResult that did not pass: (test name, outcome, first line of its message).

    THE NAME IS ALREADY IN THE FILE. This tool opens the .trx to read seven summary counters and
    then throws the rest away, so a red gate reported "1 failed, 0 errored, 0 aborted, 0 timed out"
    and nothing else — and the name cost a full local test run to recover. A count a reader cannot
    act on is a control that reports without informing.

    NOT FILTERED to outcome == "Failed". A skipped result (NotExecuted) carries the reason it was
    skipped in the same Message element, and deciding for the reader which non-passes are
    interesting is how the name was lost in the first place. The outcome is printed beside each one
    so the distinction is the reader's.

    THE FIRST LINE ONLY. The Message of an assertion failure is followed by expected/actual blocks
    and, in an exception, a stack trace; in a CI log that buries the one line that says what broke.
    """
    named: list[tuple[str, str, str]] = []
    for result in root.iter(f"{{{NS['t']}}}UnitTestResult"):
        outcome = result.get("outcome", "Unknown")
        if outcome == "Passed":
            continue
        message = result.find("t:Output/t:ErrorInfo/t:Message", NS)
        text = (message.text or "").strip() if message is not None else ""
        lines = [line for line in text.splitlines() if line.strip()]
        named.append((result.get("testName", "(unnamed result)"), outcome,
                      lines[0].strip() if lines else "(the .trx carries no message for this result)"))
    return named


def read_counts(project_name: str) -> tuple[dict[str, int], str, list[tuple[str, str, str]]] | None:
    """Return (counters, outcome, non-passed results) for a project's result file, or None.

    One parse, three answers. The portable half's .trx is a multi-megabyte file with 2,568 results
    in it; opening it a second time to read the names would be paying for the same I/O twice.
    """
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
    return ({k: int(counters.get(k, 0)) for k in wanted},
            summary.get("outcome", "Unknown"),
            non_passed(root))


def git(*args: str) -> tuple[int, str]:
    done = subprocess.run(["git", *args], cwd=REPO, capture_output=True, text=True,
                          encoding="utf-8", errors="replace", check=False)
    return done.returncode, done.stdout


def baseline_in(rev: str) -> dict[str, int] | None:
    """The committed baseline as of `rev`, or None when that rev carried no baseline file at all.

    WHERE "THE JOIN" IS, AND WHY THE REFUSAL IS A FLAG HERE RATHER THAN A STEP THERE. Both files
    were read before choosing. `conductor-join.py` is the PACK's join script
    (docs/ai-forward-pack/scripts/), shipped to every repository that installs the pack; it knows
    nothing about tools/expected-test-counts.json, which is this repository's own artifact. A
    baseline rule written into it would import this repo's convention into every consumer's join,
    and would still need per-repo configuration to be usable at all. The join's designed extension
    point is the repo-local contract docs/coordination/join.json, whose `recount` list already ends
    with ["python", "tools/verify-test-run.py", "--no-run"] — a CHECK-mode run after the three
    `--update` runs. That line is the slot: the rule lives here, behind a flag, and the join opts in.

    THE BASE IS A REQUIRED PARAMETER BECAUSE THE JOIN DOES NOT HOLD ONE. conductor-join.py captures
    no pre-merge SHA — its only `rev-parse` is at step 9/10, for a log line. What it has is the
    SHAPE of step 1, `git merge --no-ff <branch>`: at step 4 HEAD is therefore a merge commit and
    HEAD^1 is the tip the candidate is landing on, on both the normal and the `--continue` path.
    That is the join's fact to assert in its own contract, not this tool's to assume, so BASE is
    required, is passed as a rev EXPRESSION that git resolves at run time, and a base that does not
    resolve is a refusal rather than a guess.

    PER KEY, NOT PER FILE. `git diff BASE -- tools/expected-test-counts.json` is the obvious reading
    of "the baseline moved", and it passes a candidate that raised some OTHER project's floor while
    the drifting one stayed put — the same silence with one more step in front of it. Comparing the
    key's own value costs one `git show`.

    AGAINST THE WORKING TREE, NOT HEAD. The candidate is what the join is about to commit at step 7,
    which includes the rewrite `--update` left uncommitted at step 4. So the comparison is BASE's
    committed value against the baseline this run loaded from disk.

    CI IS UNCHANGED, DELIBERATELY. .github/workflows/build.yml (:583, :641-642) runs this tool
    without the flag, so CI still fails only on executed < expected: CI runs a tree that may
    legitimately be behind the baseline, and refusing that would fail branches for a floor they
    were never asked to raise.
    """
    status, text = git("show", f"{rev}:{BASELINE.relative_to(REPO).as_posix()}")
    if status != 0:
        return None
    try:
        document = json.loads(text)
    except ValueError:
        return None
    return document.get("minimumTotal", {})


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--update", action="store_true",
                        help="re-baseline the expected counts from this run")
    parser.add_argument("--allow-lower", dest="allow_lower", metavar="REASON",
                        help="permit --update to write a key BELOW its committed value, for the "
                             "stated reason. Without it such a write is refused.")
    parser.add_argument("--no-run", action="store_true",
                        help="verify existing result files instead of running the suite")
    parser.add_argument("--only", metavar="PROJECT",
                        help="run and verify one project by name (e.g. AiDe.Core.Tests)")
    parser.add_argument("--filter", dest="filter_expr", metavar="EXPR",
                        help="a dotnet test --filter, for running one HALF of a project")
    parser.add_argument("--key", metavar="NAME",
                        help="baseline key and result-file name for a filtered half "
                             "(e.g. AiDe.Core.Tests.portable)")
    parser.add_argument("--refuse-upward-drift", dest="drift_base", metavar="BASE",
                        help="the join's line: also refuse a candidate whose executed count EXCEEDS "
                             "its baseline while that key's baseline did not move between BASE and "
                             "this tree (BASE is a rev, e.g. HEAD^1). Not for CI.")
    args = parser.parse_args()

    if args.filter_expr and not args.key:
        print("verify-test-run: --filter needs --key, or the half would be measured against the "
              "whole project's baseline and a shortfall would read as success")
        return 1

    if args.drift_base and args.update:
        print("verify-test-run: --refuse-upward-drift cannot be combined with --update. --update "
              "moves the baseline by construction, so the refusal would be satisfied by the same "
              "run that made it necessary. Run --update first, then this check.")
        return 1

    if args.allow_lower is not None and not args.update:
        print("verify-test-run: --allow-lower only means anything with --update — it is permission "
              "to WRITE a lower floor, and a check-mode run writes nothing. Refusing rather than "
              "accepting a flag that would have no effect.")
        return 1

    if args.allow_lower is not None and not args.allow_lower.strip():
        print("verify-test-run: --allow-lower needs a REASON, and an empty one is not a reason. "
              "The whole value of the flag is that lowering a floor leaves a sentence behind "
              "saying why; a blank string satisfies the flag and records nothing.")
        return 1

    drift_at_base: dict[str, int] | None = None
    if args.drift_base:
        # Fail CLOSED on a base that is not a commit. `git show <rev>:<path>` cannot tell an
        # unresolvable rev from an absent file, and the absent-file answer is "the baseline is new
        # in this candidate, so it moved" — which is exactly the wrong answer to give a typo.
        resolved, _ = git("rev-parse", "--verify", "--quiet", f"{args.drift_base}^{{commit}}")
        if resolved != 0:
            print("verify-test-run: FAILED")
            print(f"  - --refuse-upward-drift was given '{args.drift_base}', which does not resolve "
                  "to a commit in this checkout. The check needs the candidate's BASE to say "
                  "whether the baseline moved in this candidate; it will not invent one, and it "
                  "will not pass a run it could not check.")
            return 1
        drift_at_base = baseline_in(args.drift_base)

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
    baseline: dict[str, int] = document.get("minimumTotal", {})
    # A SET, and a committed one. Membership is the whole question, and the list is authored by a
    # human reading real .trx files — `--update` never appends to it, because a control that adds the
    # surprises it finds to its own list of expected surprises has stopped being a control.
    expected_skips: set[str] = set(document.get("expectedSkips", []))

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
    # Per project, the non-passed results to print UNDER its findings — populated only for projects
    # that actually produced a finding, so a green run's expected skip is not reported as news.
    named: dict[str, list[tuple[str, str, str]]] = {}

    print()
    print(f"{'project':<28}{'total':>10}{'expected':>10}{'outcome':>14}")
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

        counters, outcome, results = result
        total = counters["total"]
        expected = baseline.get(name)
        observed[name] = total
        before = len(findings)

        flag = outcome
        if outcome != "Completed":
            findings.append(f"{name}: run outcome was '{outcome}', not 'Completed'")
            flag = f"**{outcome}**"
        if counters["failed"] or counters["error"] or counters["aborted"] or counters["timeout"]:
            findings.append(
                f"{name}: {counters['failed']} failed, {counters['error']} errored, "
                f"{counters['aborted']} aborted, {counters['timeout']} timed out")

        # (3), the check the docstring above has always claimed. `total - executed` is the
        # dynamically-skipped tests; unexplained, it is a hole in the floor exactly that wide.
        surprises = sorted({test for test, test_outcome, _ in results
                            if test_outcome == "NotExecuted" and test not in expected_skips})
        if surprises:
            findings.append(
                f"{name}: {len(surprises)} test(s) were skipped (NotExecuted) that no committed "
                f"`expectedSkips` entry accounts for: {', '.join(surprises)}. A skip inside `total` "
                f"and outside `executed` is invisible to the floor, so an unlisted one is a hole in "
                f"the floor that wide. Either the skip is deliberate — add the name to "
                f"`expectedSkips` in {BASELINE.relative_to(REPO).as_posix()} — or a test stopped "
                f"running and this is the notice.")
            flag = "**SKIPPED**"

        if expected is not None and total < expected:
            findings.append(
                f"{name}: the run counted {total} tests but the baseline expects at least "
                f"{expected} — {expected - total} test(s) are missing from the results entirely. "
                f"This is the silent-abort signature: the counters are self-consistent, they just "
                f"describe fewer tests than exist.")
            flag = "**SHORTFALL**"
        elif (args.drift_base and expected is not None and total > expected
              and drift_at_base is not None and drift_at_base.get(name) == expected):
            findings.append(
                f"{name}: counted {total} tests against a baseline of {expected}, and that "
                f"baseline did not move in this candidate (it is still {drift_at_base.get(name)} at "
                f"{args.drift_base}). A floor-only baseline drifts UP silently: {total - expected} "
                f"test(s) now run that the floor does not know about, so a silent abort of up to "
                f"{total - expected} test(s) would pass this gate. Re-baseline IN THIS CANDIDATE "
                f"— python tools/verify-test-run.py --update — so the floor lands with the tests.")
            flag = "**DRIFT**"

        if len(findings) > before and results:
            named[name] = results

        print(f"{name:<28}{total:>10}{str(expected) if expected is not None else '—':>10}{flag:>14}")

    print()

    if args.update:
        # AGAINST THE COMMITTED VALUE, NOT THE ONE ON DISK. The join runs `--update` three times in
        # a row before its check, and each run loads what the previous one wrote. Comparing against
        # the working tree would let the first of those three lower a floor and the rest agree with
        # it — the laundering step is free. `git show HEAD:<baseline>` is the value a reviewer would
        # see in the diff, which is the value the refusal is about.
        #
        # FAIL LOUD, NOT OPEN, when HEAD cannot be read: a comparison that silently does not happen
        # is the defect this refusal exists for, wearing the refusal's clothes. The working tree's
        # own baseline is the fallback and the message says so, so the run is never unchecked.
        committed = baseline_in("HEAD")
        source = "HEAD"
        if committed is None:
            committed = baseline
            source = "the working tree's baseline file (HEAD carries none)"

        lowered = sorted((name, committed[name], count) for name, count in observed.items()
                         if name in committed and count < committed[name])

        if lowered and not args.allow_lower:
            print("verify-test-run: REFUSED — this --update would LOWER a committed floor:")
            for name, old, new in lowered:
                print(f"  - {name}: {old} → {new}  ({old - new} fewer, measured against {source})")
            print("  Nothing was written. A floor that can be lowered by whichever machine ran last "
                  "is not a floor: that is how this baseline came to sit 8 Core and 2 App tests "
                  "below reality with nothing firing (2026-09-17). If the tests were genuinely "
                  "removed, say so: --allow-lower \"<reason>\". If they were not, this run did not "
                  "see them, which is the defect this gate exists to catch.")
            return 1

        BASELINE.parent.mkdir(parents=True, exist_ok=True)
        # MERGED, not replaced. A filtered run only observes its own half, and writing `observed`
        # wholesale would delete every key this invocation did not measure — including the whole
        # project's total, which is the one the split invariant is checked against.
        merged = dict(baseline)
        merged.update(observed)
        # BYTES, not write_text: on Windows text mode rewrites every LF to CRLF, which turns a
        # one-key change into a whole-file diff and hides what actually moved.
        BASELINE.write_bytes((json.dumps({
            "_comment": (
                "Minimum tests that must be COUNTED per project — the .trx `total`, which includes "
                "dynamically skipped tests and so does not vary by operating system; `executed` "
                "does. The control for defect class DC-012: a crashed test host reports success "
                "with a smaller count, and nothing else notices. Every NotExecuted result must "
                "also be named in `expectedSkips`, or a skip inside `total` is a hole in the floor. "
                "Raise these with `python tools/verify-test-run.py --update` when you add tests; "
                "lowering one needs --allow-lower \"<reason>\" and is refused otherwise. Keys with "
                "a suffix are HALVES of a project run on one OS; `splits` says which halves must "
                "account for which whole."
            ),
            "minimumTotal": merged,
            # CARRIED THROUGH UNTOUCHED, comment included. --update recounts; it never decides that
            # a test which stopped running was supposed to. The key order here must match the file's
            # or every --update reorders it and the diff stops showing what moved.
            "_expectedSkips_comment": document.get("_expectedSkips_comment", ""),
            "expectedSkips": document.get("expectedSkips", []),
            "splits": document.get("splits", {}),
        }, indent=2) + "\n").encode("utf-8"))
        observed = merged
        print(f"verify-test-run: baseline updated → {BASELINE.relative_to(REPO)}")
        if lowered:
            print(f"  LOWERED, on the stated reason: {args.allow_lower}")
            for name, old, new in lowered:
                print(f"    {name}: {old} → {new}  (measured against {source})")
        for name, count in sorted(observed.items()):
            print(f"  {name}: {count}")
        return 0

    if findings:
        print("verify-test-run: FAILED")
        for finding in findings:
            print(f"  - {finding}")

        # THE NAMES, under the counts they belong to. Without this the whole report of a red run is
        # "1 failed, 0 errored" and the next reader spends a full test run recovering a string that
        # was in the .trx all along.
        for project_name, results in named.items():
            print()
            print(f"  {project_name} — {len(results)} result(s) that did not pass:")
            for test, test_outcome, message in results:
                print(f"      [{test_outcome}] {test}")
                print(f"        {message}")
        return 1

    counted = sum(observed.values())
    print(f"verify-test-run: OK — {counted} tests counted across {len(observed)} project(s), "
          f"every project met its baseline and every skip was one the baseline names.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
