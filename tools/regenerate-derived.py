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
import re
import shlex
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
REGISTRY = ROOT / ".agents" / "artifacts.yml"

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
#   audit log render  rewrites docs/audit/audit-data.js + index.html from the JSONL logs — not a
#                     frontmatter source (docs-graph.py only walks *.md for frontmatter and treats
#                     *.html purely as a titled surface; verified by grep against its own file
#                     discovery, tools/regenerate-derived.py FD1), so it is placed here rather than
#                     forced strictly before docs-graph derive for a real dependency — only kept out
#                     of the LAST slot, which is reserved below
#   docs-graph derive rebuilds the index from every artifact's frontmatter — so it must be LAST,
#                     after anything that can change a frontmatter block or add an artifact
STEPS = [
    ("API reference", [sys.executable, "tools/api-reference.py", "--src", "src", "--out", "docs/api"]),
    ("documentation bundle", [sys.executable, "tools/build-doc-viewer.py"]),
    ("site figures", [sys.executable, "tools/verify-site-figures.py", "--update"]),
    # FD1: this is the one registry-declared `derived` generator that had no STEPS entry, so this
    # file's own completion message ("every derived view is current") was never true of it — the
    # verify phase below can FAIL on it (tools/verify-derived-views.py already covers
    # docs/audit/audit-data.js), but nothing here ever RE-RUN the generator to fix what it found.
    # A gate that only detects drift, on a tool whose stated job is to resolve it, leaves the
    # operator to go find the fix command themselves.
    ("audit log render", [sys.executable, "docs/ai-forward-pack/scripts/audit-log.py",
                           "--root", "docs", "--project", "ai-de", "render"]),
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

# The registry entry pattern is `path: class command...` (.agents/artifacts.yml:2). Only lines in
# exactly that shape, outside comments, count — the file's own header and its narrative asides
# ("tools/regenerate-derived.py runs api-reference BEFORE docs-graph derive...", artifacts.yml:38)
# echo the word `derived` in prose and must not be mistaken for a declaration.
_REGISTRY_LINE = re.compile(r"^(\S+):\s+derived\s+(.+)$")


def registry_derived_commands(path: Path) -> list[tuple[str, tuple[str, ...], int]]:
    """(artifact, argv[1:], line number) for every `pattern: derived <command>` line.

    argv[0] (the interpreter) is dropped before comparison: the registry stores the literal
    token `python` (artifacts.yml:23-30 explains why — a merge-time driver's contingency, not a
    generator identity), while STEPS builds every command with sys.executable, this machine's
    absolute interpreter path. Comparing full argv would never match on any machine; comparing
    argv[1:] (script path + arguments) compares what actually identifies the generator.
    """
    out = []
    for lineno, raw in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        m = _REGISTRY_LINE.match(line)
        if not m:
            continue
        artifact, command = m.group(1), m.group(2)
        try:
            argv = shlex.split(command)
        except ValueError:
            continue
        if len(argv) < 2:
            continue
        out.append((artifact, tuple(argv[1:]), lineno))
    return out


def check_registry_coverage() -> tuple[bool, str]:
    """STEPS must regenerate every artifact the registry declares `derived` — never the reverse.

    One-directional on purpose (.agents/artifacts.yml:38-40: regenerate-derived.py is the
    orchestrator; the registry can't encode the ORDER, only the generator, so this file stays
    authoritative over STEPS and the registry is only ever read, never driven from). Reversing the
    check — demanding a registry line for every STEPS command — would fail on site/*.html, which
    artifacts.yml:58-62 keeps deliberately OUT of the registry so a merge never lets a regenerate
    silently discard hand-authored prose alongside the figures.
    """
    if not REGISTRY.exists():
        return False, f"registry coverage: {REGISTRY} not found"
    declared = registry_derived_commands(REGISTRY)
    covered = {tuple(cmd[1:]) for _label, cmd in STEPS}
    missing = [(artifact, lineno) for artifact, argv, lineno in declared if argv not in covered]
    if missing:
        named = "; ".join(f"{artifact} ({REGISTRY.name}:{lineno})" for artifact, lineno in missing)
        return False, f"registry coverage: STEPS has no generator for {named}"
    return True, f"registry coverage: {len(declared)} derived declaration(s) in {REGISTRY.name}, all in STEPS"


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
        # In-process, not a subprocess: it reads STEPS from this same file, so there is nothing to
        # shell out to. Runs even if earlier CHECKS failed — a missing generator is worth reporting
        # alongside whatever it left stale, not hidden behind an unrelated failure.
        ok, line = check_registry_coverage()
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
