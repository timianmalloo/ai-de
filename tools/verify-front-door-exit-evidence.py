#!/usr/bin/env python3
"""The nine clauses of §F5, as an executable oracle over one recorded exit run.

WHY THIS IS A SCRIPT AND NOT A PARAGRAPH. `docs/plans/conductor-front-door.md` §F5 opens with the
only sentence in that plan that is about the ORDER of two acts rather than their content:

    The oracle is committed BEFORE the run and its SHA cited in the Proof Pack - seven points
    written after seeing the run are a description, not a test.

Prose cannot satisfy that. A paragraph written before the run and a paragraph written after it are
the same bytes, and nothing distinguishes them once both exist. A file has a commit; a commit has a
time; and `clause0_the_oracle_predates_the_run` below reads both, plus the stronger fact that THIS
FILE at that commit is byte-identical to the one now running - so an oracle quietly widened after
the run reddens rather than passing.

WHAT IT READS. One JSON record written by the exit-run harness, whose shape is declared here BEFORE
the harness exists. That ordering is the point: the fields are what the run is required to produce,
not a description of what it happened to emit.

WHAT IT DELIBERATELY DOES NOT DO. Clause 6 is a RECORDED measurement, never an asserted one
(ADR-0029, DC-107). This script checks that the latency figures are present, that the host is named,
and that an absent measurement reads "not recorded" rather than 0. It contains no comparison of a
duration to a constant, and must not grow one: a duration compared against a constant is DC-107
being re-created inside the control that exists to keep it out.

Exit 0 when every clause holds, 1 on any finding.
"""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
import tempfile
from pathlib import Path

EVIDENCE = "spikes/conductor-front-door-exit-run/exit-evidence.json"
PROOF_PACK = "docs/proof/conductor-front-door.md"
SCHEMA = "front-door-exit-evidence/1"
SELF = "tools/verify-front-door-exit-evidence.py"

FRONT_DOOR_ORIGIN = "main-menu.new-session"
DIRECT_ORIGIN = "direct"
NOT_RECORDED = "not recorded"

# A Residual cell that says nothing. `none` is the one this clause names explicitly - "populated" is
# satisfied by "none" in every cell - and the rest are the same act with different spelling.
EMPTY_RESIDUAL = {"", "none", "n/a", "na", "-", "--", "—", "–", "tbd", "todo", "nil", "n/k"}

# Markup a cell may wear without changing what it says.
MARKUP = re.compile(r"[*`_]|<[^>]+>")


def repo_root() -> Path:
    return Path(subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        capture_output=True, text=True, check=True).stdout.strip())


def _git(root: Path, *args: str) -> subprocess.CompletedProcess:
    return subprocess.run(["git", "-C", str(root), *args], capture_output=True, text=True)


# ---------------------------------------------------------------------------------------------
# Clause 0 - the ordering the plan puts above all nine.
# ---------------------------------------------------------------------------------------------

def clause0_the_oracle_predates_the_run(root: Path, evidence: dict) -> list[str]:
    """The oracle's commit exists, predates the run, and still holds this file's bytes."""
    findings: list[str] = []

    sha = evidence.get("oracleCommit")
    if not sha:
        return ["clause 0: the evidence record names no oracleCommit"]

    if _git(root, "cat-file", "-e", f"{sha}^{{commit}}").returncode != 0:
        return [f"clause 0: oracleCommit {sha} is not a commit in this repository"]

    committed = _git(root, "show", "-s", "--format=%ct", sha).stdout.strip()
    started = evidence.get("runStartedAtUnix")
    if not isinstance(started, int):
        findings.append("clause 0: the evidence record carries no integer runStartedAtUnix")
    elif int(committed) > started:
        findings.append(
            f"clause 0: the oracle was committed at {committed} and the run started at {started} - "
            "an oracle that post-dates its run is a description")

    # THE HALF THAT CANNOT BE FAKED BY WAITING. An oracle committed early and then widened after the
    # run reads identically to one that was right first time, unless the bytes are compared.
    at_commit = _git(root, "show", f"{sha}:{SELF}")
    if at_commit.returncode != 0:
        findings.append(f"clause 0: {SELF} does not exist at {sha}")
    elif at_commit.stdout != (root / SELF).read_text(encoding="utf-8"):
        findings.append(
            f"clause 0: {SELF} has changed since {sha} - the oracle that ran is not the oracle that "
            "was committed")

    return findings


# ---------------------------------------------------------------------------------------------
# The nine.
# ---------------------------------------------------------------------------------------------

def clause1_started_from_the_front_door(root: Path, evidence: dict) -> list[str]:
    """`session.open` carries `origin`, set only on the Ctrl+N / MainMenuBuilder path."""
    findings: list[str] = []
    c = evidence.get("clause1", {})

    if c.get("sessionOpenOrigin") != FRONT_DOOR_ORIGIN:
        findings.append(
            f"clause 1: the exit run's session.open origin is {c.get('sessionOpenOrigin')!r}, "
            f"not {FRONT_DOOR_ORIGIN!r}")

    # THE COMPANION. A field that is always the same value proves nothing, so the run is only
    # evidence alongside a session that did NOT come through the front door reading differently.
    if c.get("companionOrigin") != DIRECT_ORIGIN:
        findings.append(
            f"clause 1: the companion session's origin is {c.get('companionOrigin')!r}, "
            f"not {DIRECT_ORIGIN!r} - asserted-about is what N7 was blocked for")

    for key in ("companionTest", "scanTest"):
        name = c.get(key)
        if not name:
            findings.append(f"clause 1: no {key} is named")
        elif not _declared_in_tests(root, name):
            findings.append(f"clause 1: {key} names {name!r}, which is declared nowhere under tests/")

    return findings


def clause2_composed_and_streamed(evidence: dict) -> list[str]:
    """Composed in the composer, streamed in Console mode."""
    findings: list[str] = []
    c = evidence.get("clause2", {})

    if c.get("composerSendCount") != 1:
        findings.append(f"clause 2: the composer's send count is {c.get('composerSendCount')!r}, not 1")

    rendered = c.get("compiledViewSha256")
    sent = c.get("promptSha256")
    if not rendered or not sent:
        findings.append("clause 2: the compiled view and the sent prompt are not both digested")
    elif rendered != sent:
        findings.append(
            "clause 2: the bytes sent are not the bytes the composer rendered "
            f"({sent} != {rendered})")

    if c.get("activeModeId") != "console":
        findings.append(f"clause 2: the active canvas mode is {c.get('activeModeId')!r}, not 'console'")

    rows = c.get("consoleRowsRendered")
    if not isinstance(rows, int) or rows < 1:
        findings.append(f"clause 2: the console rendered {rows!r} rows - nothing streamed")

    # RULING 45's exposure, as a measured path fact rather than an assumption. The Terminal canvas
    # mode constructs a real ConPTY in its own constructor, so a run that built it would have
    # incremented the counter clause 4 asserts is zero.
    if c.get("terminalModeBuilt") is not False:
        findings.append(
            f"clause 2: terminalModeBuilt is {c.get('terminalModeBuilt')!r} - if the run built the "
            "Terminal canvas mode, clause 4's zero is about a different path than this clause")

    return findings


def clause3_scored_end_to_end(evidence: dict) -> list[str]:
    """Scored end to end, cell `IsComparable == true`, read from `scored_episode_cell`."""
    findings: list[str] = []
    c = evidence.get("clause3", {})

    if c.get("scored") is not True:
        findings.append(f"clause 3: scored is {c.get('scored')!r}")
    if c.get("segmentIsComparable") is not True:
        findings.append(
            f"clause 3: segmentIsComparable is {c.get('segmentIsComparable')!r}, "
            f"reason {c.get('incomparableReason')!r}")
    if c.get("incomparableReason") is not None:
        findings.append(f"clause 3: incomparableReason is {c.get('incomparableReason')!r}, not null")
    if c.get("readFrom") != "scored_episode_cell":
        findings.append(
            f"clause 3: the cell was read from {c.get('readFrom')!r}, not the store's "
            "scored_episode_cell table")
    if not c.get("storeRow"):
        findings.append("clause 3: no store row is recorded - a verdict with no row is a claim")

    return findings


def clause4_zero_terminal_hosting(root: Path, evidence: dict) -> list[str]:
    """`terminalHostConstructions == 0`, with N7's companion falsifier carried forward verbatim."""
    findings: list[str] = []
    c = evidence.get("clause4", {})

    if c.get("terminalHostConstructions") != 0:
        findings.append(
            f"clause 4: the run hosted {c.get('terminalHostConstructions')!r} terminals")

    # A COUNTER NOTHING INCREMENTS READS 0 FOREVER. The zero above is worth nothing without the
    # observation that the same counter goes to 1 for a real ConPTY.
    name = c.get("falsifierTest")
    if not name:
        findings.append("clause 4: no falsifier test is named")
    elif not _declared_in_tests(root, name):
        findings.append(f"clause 4: falsifierTest names {name!r}, declared nowhere under tests/")

    if c.get("falsifierObservedConstructions") != 1:
        findings.append(
            f"clause 4: the falsifier observed {c.get('falsifierObservedConstructions')!r} "
            "constructions, not 1")

    return findings


def clause5_one_composition_root(evidence: dict) -> list[str]:
    """Launched through the same composition root, no second entry point (Ruling 13)."""
    findings: list[str] = []
    c = evidence.get("clause5", {})

    if c.get("compositionRoots") != 1:
        findings.append(f"clause 5: the ledger counted {c.get('compositionRoots')!r} roots, not 1")

    launched = c.get("launchedBy")
    if not launched:
        findings.append("clause 5: no launch site is named")
    elif not launched.startswith("src/"):
        findings.append(
            f"clause 5: the run was launched by {launched!r}, which is not product code - "
            "a hand-assembled harness does not count as exit evidence")

    if c.get("governedRunRequestSites") != 2:
        findings.append(
            f"clause 5: {c.get('governedRunRequestSites')!r} sites construct a GovernedRunRequest; "
            "C16 caps it at 2")

    return findings


def clause6_recorded_measurement(evidence: dict) -> list[str]:
    """Event count, p50/p95, host named - RECORDED, never asserted (ADR-0029, DC-107).

    There is deliberately no comparison of any figure here to a constant. The only failures this
    clause can produce are absence, a missing host, and a zero standing in for a measurement nobody
    took.
    """
    findings: list[str] = []
    c = evidence.get("clause6", {})

    events = c.get("eventsObserved")
    if not isinstance(events, int) or events < 1:
        findings.append(f"clause 6: eventsObserved is {events!r}")

    measured = c.get("latencyMeasured")
    if not isinstance(measured, int):
        findings.append(f"clause 6: latencyMeasured is {measured!r}")

    for key in ("latencyP50Ms", "latencyP95Ms"):
        value = c.get(key)
        if value == NOT_RECORDED:
            continue
        if not isinstance(value, (int, float)):
            findings.append(
                f"clause 6: {key} is {value!r} - a measurement degrades to {NOT_RECORDED!r}, "
                "never to a plausible wrong number")

    # An absent measurement that reads 0 is the failure IO12 names: zero and unmeasured are
    # different facts and only one of them is good news.
    if measured == 0 and c.get("latencyP50Ms") != NOT_RECORDED:
        findings.append(
            "clause 6: nothing was measured, yet latencyP50Ms carries a number rather than "
            f"{NOT_RECORDED!r}")

    host = c.get("latencyHost")
    if not isinstance(host, str) or not host.strip() or host == NOT_RECORDED:
        findings.append(f"clause 6: the host is {host!r} - a magnitude with no host is not portable")

    return findings


def clause7_proof_pack_residuals(root: Path, evidence: dict) -> list[str]:
    """Every Residual cell names a measurement or an explicit uncovered input."""
    findings: list[str] = []

    pack = root / PROOF_PACK
    if not pack.exists():
        return [f"clause 7: {PROOF_PACK} does not exist"]

    text = pack.read_text(encoding="utf-8")
    if "## Residual" not in text:
        findings.append(f"clause 7: {PROOF_PACK} carries no Residual section")

    empty = _empty_residual_cells(text)
    for row, cell in empty:
        findings.append(
            f"clause 7: a Residual cell reads {cell!r} - \"populated\" is satisfied by \"none\" in "
            f"every cell. Row: {row[:110]}")

    if not evidence.get("clause7", {}).get("residualsSource"):
        findings.append("clause 7: the evidence record names no residualsSource")

    return findings


def clause8_r13b2_qualification(root: Path, evidence: dict) -> list[str]:
    """Only `claude-code` was exercised; codex and copilot are refused by N2's own test."""
    findings: list[str] = []
    c = evidence.get("clause8", {})

    if c.get("enginesExercised") != ["claude-code"]:
        findings.append(
            f"clause 8: enginesExercised is {c.get('enginesExercised')!r}; R13 b2's qualification "
            "(Ruling 18) is that only claude-code was exercised")

    refusals = c.get("refusalTests") or []
    if len(refusals) < 2:
        findings.append(
            "clause 8: fewer than two refusal tests are named - codex and copilot are each refused "
            "by their own case, and the qualification is stated, not stubbed")
    for name in refusals:
        if not _declared_in_tests(root, name):
            findings.append(f"clause 8: refusalTest {name!r} is declared nowhere under tests/")

    return findings


def clause9_dc115(evidence: dict) -> list[str]:
    """DC-115: the repository-root shape is carried, never silently."""
    findings: list[str] = []
    c = evidence.get("clause9", {})

    shape = c.get("repositoryRootShape")
    if shape not in ("linked-worktree", "clone", "primary-checkout"):
        return [f"clause 9: repositoryRootShape is {shape!r}"]

    if shape == "clone" and not (c.get("qualification") or "").strip():
        findings.append(
            "clause 9: the run rooted in a clone and carries no DC-115 qualification - Phase 1 "
            "carried it in writing, and never silently")

    # RULING 17. Under the linked-worktree shape a Not Scored verdict is an EvaluatorIntegrity trip
    # to the human, NOT a qualification that may be written into a Residual cell.
    if shape == "linked-worktree" and evidence.get("clause3", {}).get("scored") is not True:
        findings.append(
            "clause 9: the run rooted in a linked worktree and did not score - Ruling 17 makes that "
            "an EvaluatorIntegrity trip to the human, not a carried qualification")

    return findings


# ---------------------------------------------------------------------------------------------

def _declared_in_tests(root: Path, name: str) -> bool:
    """Whether a cited test class or method is declared anywhere under tests/.

    Deliberately generous, for the reason `verify-cited-controls.py` gives: the failure being caught
    is "names nothing at all", not "names the wrong kind of thing".
    """
    bare = name.rsplit(".", 1)[-1]
    pattern = re.compile(rf"\b(?:class|void|Task)\s+{re.escape(bare)}\b")
    tests = root / "tests"
    if not tests.exists():
        return False
    for path in tests.rglob("*.cs"):
        try:
            if pattern.search(path.read_text(encoding="utf-8", errors="ignore")):
                return True
        except OSError:
            continue
    return False


def _empty_residual_cells(text):
    """Every cell that says nothing in a table whose subject is residuals.

    TWO COLUMNS, BECAUSE A RESIDUAL TABLE COMES IN TWO SHAPES and the loophole is in the second.
    Where `Residual` is the last column it carries the substance, and reading it is the whole job.
    Where the table is a register - `| Residual | Kind | Detail |` - the `Residual` column carries
    only the NAME, and a row reading `| the thing we did not do | named | none |` would satisfy a
    check that looked at the first column alone. That is the same evasion one column to the right,
    so `Detail` is read too wherever the table declares one.
    """
    findings: list[tuple[str, str]] = []
    columns: list[int] = []

    for line in text.splitlines():
        stripped = line.strip()
        if not stripped.startswith("|"):
            columns = []
            continue

        cells = [c.strip() for c in stripped.strip("|").split("|")]

        if set("".join(cells)) <= set("-: "):
            continue  # the separator row

        headers = [MARKUP.sub("", c).strip().lower() for c in cells]
        if "residual" in headers:
            columns = [headers.index("residual")]
            if "detail" in headers:
                columns.append(headers.index("detail"))
            continue

        for column in columns:
            if column >= len(cells):
                continue
            cell = MARKUP.sub("", cells[column]).strip()
            if cell.lower() in EMPTY_RESIDUAL:
                findings.append((stripped, cell))

    return findings


def check(root: Path, evidence_path: Path) -> list[str]:
    if not evidence_path.exists():
        return [f"the exit-evidence record was not found at {evidence_path}"]

    try:
        evidence = json.loads(evidence_path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as error:
        return [f"the exit-evidence record is not JSON: {error}"]

    if evidence.get("schema") != SCHEMA:
        return [f"the exit-evidence record declares schema {evidence.get('schema')!r}, not {SCHEMA!r}"]

    return [
        *clause0_the_oracle_predates_the_run(root, evidence),
        *clause1_started_from_the_front_door(root, evidence),
        *clause2_composed_and_streamed(evidence),
        *clause3_scored_end_to_end(evidence),
        *clause4_zero_terminal_hosting(root, evidence),
        *clause5_one_composition_root(evidence),
        *clause6_recorded_measurement(evidence),
        *clause7_proof_pack_residuals(root, evidence),
        *clause8_r13b2_qualification(root, evidence),
        *clause9_dc115(evidence),
    ]


# ---------------------------------------------------------------------------------------------
# Self-test: this gate proving it can fail, one clause at a time (DC-104).
# ---------------------------------------------------------------------------------------------

def _sample(root: Path) -> dict:
    """A record that passes every clause EXCEPT clause 0, which needs a real commit."""
    return {
        "schema": SCHEMA,
        "oracleCommit": "0" * 40,
        "runStartedAtUnix": 2_000_000_000,
        "clause1": {
            "sessionOpenOrigin": FRONT_DOOR_ORIGIN,
            "companionOrigin": DIRECT_ORIGIN,
            "companionTest": "ASessionConstructedDirectlyReadsTheOtherValue",
            "scanTest": "ExactlyOneSiteInSrcCanStampTheFrontDoorOrigin",
        },
        "clause2": {
            "composerSendCount": 1,
            "compiledViewSha256": "abc",
            "promptSha256": "abc",
            "activeModeId": "console",
            "consoleRowsRendered": 12,
            "terminalModeBuilt": False,
        },
        "clause3": {
            "scored": True,
            "segmentIsComparable": True,
            "incomparableReason": None,
            "readFrom": "scored_episode_cell",
            "storeRow": {"episode_id": "e", "task_class": "t"},
        },
        "clause4": {
            "terminalHostConstructions": 0,
            "falsifierTest": "AnOpenLedgerCountsARealTerminalHostConstruction",
            "falsifierObservedConstructions": 1,
        },
        "clause5": {
            "compositionRoots": 1,
            "launchedBy": "src/AiDe.App/Workbench/Sessions/SessionDocumentSurface.cs",
            "governedRunRequestSites": 2,
        },
        "clause6": {
            "eventsObserved": 40,
            "latencyMeasured": 40,
            "latencyP50Ms": 0.02,
            "latencyP95Ms": 0.06,
            "latencyHost": "SOMEHOST",
        },
        "clause7": {"residualsSource": "docs/notes/front-door-residuals-carried.md"},
        "clause8": {
            "enginesExercised": ["claude-code"],
            "refusalTests": [
                "ANonAdapterModeIsRefusedWithANamedReason",
                "AnAdapterWithNoObservedEntryModuleIsRefusedRatherThanGuessed",
            ],
        },
        "clause9": {"repositoryRootShape": "linked-worktree", "qualification": ""},
    }


def _breaks() -> list[tuple[str, callable]]:
    """One mutation per clause, each breaking exactly the clause it is named for."""
    def origin_is_constant(record):
        record["clause1"]["companionOrigin"] = FRONT_DOOR_ORIGIN

    def sent_is_not_rendered(record):
        record["clause2"]["promptSha256"] = "different"

    def not_comparable(record):
        record["clause3"]["segmentIsComparable"] = False
        record["clause3"]["incomparableReason"] = "the task class was defaulted"

    def a_terminal_was_hosted(record):
        record["clause4"]["terminalHostConstructions"] = 1

    def a_second_entry_point(record):
        record["clause5"]["launchedBy"] = "tests/AiDe.App.ExitRunProbe/Program.cs"

    def zero_standing_in_for_unmeasured(record):
        record["clause6"]["latencyMeasured"] = 0
        record["clause6"]["latencyP50Ms"] = 0

    def host_not_named(record):
        record["clause6"]["latencyHost"] = NOT_RECORDED

    def counter_never_increments(record):
        record["clause4"]["falsifierObservedConstructions"] = 0

    def clone_with_no_qualification(record):
        record["clause9"]["repositoryRootShape"] = "clone"

    def not_scored_in_a_worktree(record):
        record["clause3"]["scored"] = False

    return [
        ("clause 1", origin_is_constant),
        ("clause 2", sent_is_not_rendered),
        ("clause 3", not_comparable),
        ("clause 4", a_terminal_was_hosted),
        ("clause 4", counter_never_increments),
        ("clause 5", a_second_entry_point),
        ("clause 6", zero_standing_in_for_unmeasured),
        ("clause 6", host_not_named),
        ("clause 9", clone_with_no_qualification),
        ("clause 9", not_scored_in_a_worktree),
    ]


def self_test(root: Path) -> int:
    failures: list[str] = []

    # The Residual reader, on the shape clause 7 exists to catch and the shape it must accept.
    empty = _empty_residual_cells(
        "| Claim | Residual |\n| --- | --- |\n| a | none |\n| b | **measured: 4 of 599** |\n")
    if [cell for _, cell in empty] != ["none"]:
        failures.append(f"the Residual reader found {empty!r} instead of exactly the 'none' cell")

    if _empty_residual_cells("| Claim | Evidence |\n| --- | --- |\n| a | none |\n"):
        failures.append("the Residual reader flagged a cell in a column that is not Residual")

    # THE REGISTER SHAPE, where the substance sits one column further right.
    register = _empty_residual_cells("""
| Residual | Kind | Detail |
| --- | --- | --- |
| a thing | named | none |
| another | measured | 4 of 599 |
""")
    if [cell for _, cell in register] != ["none"]:
        failures.append(
            f"the Residual reader found {register!r} in a register table instead of the empty Detail")

    # A cited test that exists resolves; one that does not, does not.
    if not _declared_in_tests(root, "AnOpenLedgerCountsARealTerminalHostConstruction"):
        failures.append("a cited test that really exists was not found under tests/")
    if _declared_in_tests(root, "AControlThatHasNeverExistedTests"):
        failures.append("a cited test that does not exist was reported as found")

    with tempfile.TemporaryDirectory() as directory:
        path = Path(directory) / "exit-evidence.json"

        # Every mutation must redden, and must redden its OWN clause.
        for clause, mutate in _breaks():
            record = _sample(root)
            mutate(record)
            path.write_text(json.dumps(record), encoding="utf-8")
            findings = [f for f in check(root, path) if not f.startswith("clause 0")]
            if not any(f.startswith(clause) for f in findings):
                failures.append(f"the {clause} mutation did not redden {clause}: {findings}")

        # And an unmutated record must produce nothing but the clause-0 finding, which needs a real
        # commit. A gate that fires on a clean record is noise, and noise gets switched off.
        path.write_text(json.dumps(_sample(root)), encoding="utf-8")
        clean = [f for f in check(root, path) if not f.startswith("clause 0")]
        if clean:
            failures.append(f"a clean record produced findings: {clean}")

    for failure in failures:
        print(f"SELF-TEST: {failure}")

    if failures:
        return 1

    print("self-test: the oracle reddens on every clause it claims to check")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--evidence", default=None, help=f"the exit-evidence record (default {EVIDENCE})")
    parser.add_argument("--self-test", action="store_true", help="prove this gate can fail")
    args = parser.parse_args()

    root = repo_root()

    if args.self_test:
        return self_test(root)

    findings = check(root, Path(args.evidence) if args.evidence else root / EVIDENCE)

    for finding in findings:
        print(finding)

    if findings:
        print(f"\n{len(findings)} finding(s): the front-door exit evidence does not hold.")
        return 1

    print("the front-door exit evidence holds: nine clauses, plus the ordering above them.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
