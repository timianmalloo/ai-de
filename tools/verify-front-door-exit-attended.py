#!/usr/bin/env python3
"""verify-front-door-exit-attended.py — F5's exit record read bare, on the operator's word (Ruling 103).

WHY A SECOND FILE AND NOT A BRANCH IN THE ORACLE. `tools/verify-front-door-exit-evidence.py` is the
nine-clause oracle committed before the run (`1374401d`, 2026-09-11), and its clause 0 reddens when
that file's bytes differ from the bytes at `oracleCommit` — an oracle widened after the run is a
description. Ruling 103 asks the gate to accept a record that carries the operator's word and no
frames; putting that acceptance INSIDE the oracle would change its bytes after the run and trip the
very clause that keeps it honest. So the oracle stays byte-identical and this file wraps it: it reads
the record, requires the operator's word, runs the untouched oracle bare (a UTF-8 subprocess — see
`oracle_findings`), and reports each
clause as MET or NOT MET with the oracle's own findings. The exit status is the operator's decision
(0), except where the record is not the operator's, is malformed, or clause 0 fails — an oracle whose
integrity is broken cannot say which clauses held.

WHAT "MET" MEANS HERE. The record is assembled from the run's own artefacts by
`spikes/conductor-front-door-exit-run/assemble-exit-evidence.py` (each field names its source, or
reads "not recorded"). A clause reads NOT MET when the oracle has a finding against it — including
"not recorded" where the oracle wanted a number. That is the honest headline: the front door was
walked through by the operator on build 51e806f8 (clause 1's origin is the product's own stamp), the
run completed, no terminal was hosted; what the product did not record (a scored cell, the prompt's
own digest, latencies) reads NOT MET, not "passed".

Exit 0: the record is attended by the operator and the oracle's integrity clause holds (per-clause
status printed). Exit 1: the record is missing, not the operator's, malformed, or clause 0 failed.
`--self-test` proves the red paths: a record attended by "conductor" fails; a broken clause 0 fails.
Stdlib only.
"""
from __future__ import annotations

import argparse
import json
import os
import re
import subprocess
import sys
import tempfile
from pathlib import Path

EVIDENCE = "spikes/conductor-front-door-exit-run/exit-evidence.json"
ORACLE = "tools/verify-front-door-exit-evidence.py"
CLAUSE = re.compile(r"^clause (\d+):")


def repo_root() -> Path:
    return Path(subprocess.run(["git", "rev-parse", "--show-toplevel"],
                               capture_output=True, text=True, encoding="utf-8", errors="replace", check=True).stdout.strip())


def oracle_findings(root: Path, evidence_path: Path) -> list[str]:
    """The untouched oracle's findings, one per line of its stdout.

    RUN AS A SUBPROCESS UNDER UTF-8, NOT IMPORTED. The oracle's `_git` decodes `git show` with the
    interpreter's locale (`text=True`, no encoding): on this machine that is cp1252, so the `§` in
    its own docstring decodes to two characters on one side of clause 0's byte comparison and one on
    the other, and the committed oracle reads as "changed" against itself. `PYTHONUTF8=1` makes the
    child decode both sides the same way; setting it in-process after start would not. The oracle
    cannot be repaired in place: a repair post-dates the run, which clause 0 forbids by design.
    """
    env = dict(os.environ, PYTHONUTF8="1", PYTHONIOENCODING="utf-8")
    completed = subprocess.run([sys.executable, str(root / ORACLE), "--evidence", str(evidence_path)],
                               cwd=root, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
    findings = [line.rstrip() for line in completed.stdout.splitlines()
                if line.strip()
                and not line.startswith("the front-door exit evidence")
                and not re.match(r"^\d+ finding\(s\)", line)]
    if completed.returncode == 0:
        return []
    if not findings:
        return [f"the oracle exited {completed.returncode} with no findings: {completed.stderr.strip()[:300]}"]
    return findings


def attended(root: Path, evidence_path: Path) -> tuple[int, list[str]]:
    """Returns (exit status, lines to print)."""
    lines: list[str] = []
    if not evidence_path.exists():
        return 1, [f"the exit-evidence record was not found at {evidence_path}"]
    try:
        record = json.loads(evidence_path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as error:
        return 1, [f"the exit-evidence record is not JSON: {error}"]

    who = (record.get("attended") or {}).get("by")
    words = ((record.get("attended") or {}).get("words") or "").strip()
    if who != "operator" or not words:
        return 1, [f"the record is attended by {who!r} with words {words!r}: a frameless record is accepted on the "
                   "operator's word only (Ruling 103); anyone else's record needs the frames"]

    findings = oracle_findings(root, evidence_path)
    by_clause: dict[int, list[str]] = {n: [] for n in range(10)}
    other: list[str] = []
    for f in findings:
        m = CLAUSE.match(f)
        (by_clause[int(m.group(1))] if m else other).append(f)

    if other:
        return 1, [*other, "the record is malformed for the oracle; nothing below it can be read"]
    if by_clause[0]:
        return 1, [*by_clause[0], "clause 0 failed: the oracle that ran is not the oracle that was committed before the "
                                  "run, so no clause status is readable"]

    a = record["attended"]
    lines.append(f"F5 exit evidence, attended by the operator: \"{words}\" — {a.get('observed')}")
    lines.append(f"  gesture: {a.get('gesture')}; build {a.get('build')}; session {a.get('session')}; run {a.get('run')} {a.get('outcome')}")
    met = 0
    for n in range(10):
        if by_clause[n]:
            lines.append(f"  clause {n}: NOT MET")
            for f in by_clause[n]:
                lines.append(f"      {f}")
        else:
            met += 1
            lines.append(f"  clause {n}: MET")
    lines.append(f"clauses met {met}/10; F5 is closed on the operator's word (Ruling 103) — the NOT MET rows are what the "
                 "product did not record, carried as residuals, never as a pass.")
    return 0, lines


def self_test(root: Path) -> int:
    """Two red paths, observed red: a conductor-attended record; a record whose oracle commit is not the oracle."""
    record = json.loads((root / EVIDENCE).read_text(encoding="utf-8"))
    failures = []
    with tempfile.TemporaryDirectory() as tmp:
        conductor = dict(record, attended=dict(record["attended"], by="conductor"))
        p = Path(tmp) / "conductor.json"
        p.write_text(json.dumps(conductor), encoding="utf-8")
        status, out = attended(root, p)
        if status != 1 or not any("operator's word only" in line for line in out):
            failures.append("a record attended by the conductor was accepted")

        widened = dict(record, oracleCommit="0" * 40)
        p2 = Path(tmp) / "widened.json"
        p2.write_text(json.dumps(widened), encoding="utf-8")
        status, out = attended(root, p2)
        if status != 1 or not any("clause 0" in line for line in out):
            failures.append("a record whose oracleCommit is not a commit was accepted")

    for f in failures:
        print(f"self-test: {f}")
    if failures:
        return 1
    print("verify-front-door-exit-attended: self-test OK — a conductor-attended record and a broken clause 0 both fail.")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--evidence", default=None)
    parser.add_argument("--self-test", action="store_true", help="prove this gate can fail")
    args = parser.parse_args()
    root = repo_root()
    if args.self_test:
        return self_test(root)
    status, lines = attended(root, Path(args.evidence) if args.evidence else root / EVIDENCE)
    for line in lines:
        print(line)
    return status


if __name__ == "__main__":
    sys.exit(main())
