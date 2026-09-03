#!/usr/bin/env python3
"""Regenerate every derived view, in dependency order, and verify the result.

WHY THIS EXISTS. The order is not obvious and getting it wrong produces a stale artifact with no
conflict marker and no error — DC-060's shape, reached by sequence rather than by merge. Five
instances landed in one day across two sessions, and none was caught by its author: the gate catches
staleness on the NEXT run, which is usually somebody else's push, so nobody sees the whole shape.

DC-082 names the rule: **derived views regenerate LAST, after the append-only logs are written.**
An audit entry changes the very counts the figures report, so regenerating before appending produces
figures that were correct when written and stale by the time the commit closed — by construction, on
every commit carrying an audit entry.

A rule is a procedure with no failure mode. This is the shape: one command, the order encoded once,
and the verifiers run at the end so "did I do that in the right order" is answered here rather than
on the next person's push.

    RUN THIS AFTER appending audit entries, capturing mitigations, or editing any docs/ artifact —
    never before.

Usage:
    python tools/regenerate-derived.py           # regenerate, then verify
    python tools/regenerate-derived.py --check    # verify only; changes nothing
"""
from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

# Encoding pinned on the way OUT as well as the way in. Every step below is read with
# errors="replace", which yields U+FFFD for a byte the tool emitted in another encoding — and
# printing that to a Windows cp1252 console raises UnicodeEncodeError and kills the run. Third
# instance of this class today (DC-078 was the same thing on the input side of the craft gate), and
# the first two both presented as the tool being broken rather than the console being narrow.
for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

# Order is the whole point of this file.
#
#   api-reference     rewrites docs/api/*.md, whose FRONTMATTER the graph index reads
#   build-doc-viewer  embeds docs/api + the diagrams into docs/_site, and writes _meta.json
#   site-figures      counts artifacts, ledger entries, defect classes and public symbols
#   docs-graph derive rebuilds the index from every artifact's frontmatter — so it must be LAST,
#                     after anything that can change a frontmatter block or add an artifact
STEPS = [
    ("API reference", [sys.executable, "tools/api-reference.py", "--src", "src", "--out", "docs/api"]),
    ("documentation bundle", [sys.executable, "tools/build-doc-viewer.py"]),
    ("site figures", [sys.executable, "tools/verify-site-figures.py", "--update"]),
    ("docs graph index", [sys.executable, "docs/ai-forward-pack/scripts/docs-graph.py", "derive"]),
]

# Run after, never instead. A regeneration that produced a stale artifact reports success on its own
# terms; only the verifiers can say whether the result is current (R4).
CHECKS = [
    ("derived views", [sys.executable, "tools/verify-derived-views.py"]),
    ("site figures", [sys.executable, "tools/verify-site-figures.py"]),
    ("defect register", [sys.executable, "tools/verify-defect-register.py"]),
    ("audit log", [sys.executable, "docs/ai-forward-pack/scripts/audit-log.py", "verify"]),
]


def run(label: str, cmd: list[str]) -> tuple[bool, str]:
    try:
        proc = subprocess.run(
            cmd, cwd=str(ROOT), capture_output=True, text=True,
            encoding="utf-8", errors="replace", timeout=900)
    except (OSError, subprocess.SubprocessError) as exc:
        return False, f"{label}: could not run — {exc}"

    tail = (proc.stdout or proc.stderr or "").strip().splitlines()
    detail = tail[-1] if tail else "(no output)"
    return proc.returncode == 0, f"{label}: {detail}"


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true", help="verify only; regenerate nothing")
    args = ap.parse_args()

    failures = []

    if not args.check:
        print("regenerating, in dependency order:")
        for label, cmd in STEPS:
            ok, line = run(label, cmd)
            print(("  " if ok else "  FAILED ") + line)
            if not ok:
                # Stop rather than continue: a later step reads what an earlier one writes, so
                # carrying on past a failure produces a result derived from a half-written source.
                failures.append(line)
                break

    if not failures:
        print("verifying:")
        for label, cmd in CHECKS:
            ok, line = run(label, cmd)
            print(("  " if ok else "  FAILED ") + line)
            if not ok:
                failures.append(line)

    if failures:
        print()
        print(f"{len(failures)} step(s) failed. If a figure or index is stale after a full run, the")
        print("likely cause is an append AFTER regeneration — re-run this, and append first next time.")
        return 1

    print()
    print("every derived view is current and every gate is green.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
