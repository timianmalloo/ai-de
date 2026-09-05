#!/usr/bin/env python3
"""A conflict marker must never reach a commit.

WHAT HAPPENED. `site/collaboration.html` and `site/index.html` — the PUBLISHED pages — carried
committed `<<<<<<< HEAD` / `=======` / `>>>>>>> origin/main` markers on `main`. The markers had
duplicated four `data-figure` cells, so the audit-entry count appeared four times in one table row
and the ledger count twice. It was found only because a later merge produced NESTED markers, which
made the file impossible to resolve without reading it.

WHY EVERY EXISTING GATE PASSED. `verify-site-figures` reads each `data-figure` cell and compares it
against the source. Every duplicated cell held the SAME, CORRECT number — so the gate verified four
copies of a right answer and reported success. It counts figures; it has no opinion about whether the
document around them is intact. `verify-derived-views` did not look either: `site/*.html` are not
among its four derived views, because they are only PARTIALLY derived — `regenerate-derived.py`
patches their figures in place rather than rebuilding them. The file therefore belonged to no gate's
structural check, and a content check cannot see structural damage by construction.

WHY A DEDICATED GATE. A conflict marker is a uniquely bad defect to leave to human attention: it is
syntactically legal in almost every text format we commit (HTML renders it as stray text, Markdown as
a paragraph, JSONL as an unparseable line only if it lands mid-record), it survives review because
diffs of a large generated file are skimmed, and it is unambiguous evidence that a file which should
have been REGENERATED was resolved by hand instead (§4b item 3). There is no legitimate reason for one
to exist in a tracked file, which makes this the rare check with no judgement in it at all.

WHAT IS CHECKED. Every tracked text file, for `<<<<<<<`, `>>>>>>>` and `|||||||` at the start of a
line. `=======` is deliberately NOT flagged on its own: it is a Markdown setext heading underline and
a reStructuredText rule, so alone it is ambiguous, and a gate that fires on valid prose is a gate
someone switches off.

Exit 0 when clean, 1 on any finding.
"""

from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path

# Only the unambiguous ones. `=======` alone is legitimate Markdown/RST and is never flagged.
MARKERS = ("<<<<<<<", ">>>>>>>", "|||||||")

# This file must describe the markers it hunts, so it necessarily contains them in prose.
SELF = "tools/verify-no-conflict-markers.py"


def repo_root() -> Path:
    return Path(subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        capture_output=True, text=True, check=True).stdout.strip())


def tracked_files(root: Path) -> list[str]:
    out = subprocess.run(["git", "ls-files"], capture_output=True, text=True, cwd=root).stdout
    return out.split()


def scan(root: Path, files: list[str]) -> tuple[list[str], int]:
    """Returns (findings, files actually read)."""
    findings: list[str] = []
    read = 0

    for relative in files:
        if relative == SELF:
            continue

        path = root / relative

        try:
            text = path.read_text(encoding="utf-8")
        except (OSError, UnicodeDecodeError):
            # Binary, or gone from the worktree. Neither can carry a text marker we could act on.
            continue

        read += 1

        for number, line in enumerate(text.splitlines(), 1):
            if line.startswith(MARKERS):
                findings.append(
                    f"{relative}:{number} carries a conflict marker: {line.strip()[:60]!r}")

    return (findings, read)


def self_test(root: Path) -> int:
    """Prove all three directions: it fires, it does NOT fire on valid prose, and it refuses an
    empty corpus."""
    scratch = root / "docs" / "ai-forward-pack"
    scratch.mkdir(parents=True, exist_ok=True)

    dirty = scratch / "_selftest_conflicted.md"
    clean = scratch / "_selftest_setext.md"

    dirty.write_text("before\n" + "<" * 7 + " HEAD\nmine\n=======\ntheirs\n"
                     + ">" * 7 + " origin/main\nafter\n", encoding="utf-8")

    # The false-positive direction. A setext heading underline is `=======` at the start of a line,
    # and it is valid Markdown that appears in real documents.
    clean.write_text("A heading\n=========\n\nbody text\n", encoding="utf-8")

    try:
        rel_dirty = dirty.relative_to(root).as_posix()
        rel_clean = clean.relative_to(root).as_posix()

        findings, _ = scan(root, [rel_dirty])
        if not any(rel_dirty in f for f in findings):
            print("self-test FAILED: a committed conflict marker was not reported", file=sys.stderr)
            return 1

        findings, _ = scan(root, [rel_clean])
        if findings:
            print(f"self-test FAILED: a Markdown setext underline was reported: {findings}",
                  file=sys.stderr)
            return 1

        # PACK-P: a check must establish its corpus is non-empty before reporting a verdict over it.
        _, read = scan(root, ["NO_SUCH_FILE_AT_ALL.md"])
        if read != 0:
            print("self-test FAILED: a missing file was counted as read", file=sys.stderr)
            return 1
    finally:
        dirty.unlink(missing_ok=True)
        clean.unlink(missing_ok=True)

    print("self-test OK — a marker is reported, a setext underline is not, and an unreadable "
          "corpus counts as nothing read")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--self-test", action="store_true",
                        help="prove the gate fires, and that it does not fire on valid Markdown")
    args = parser.parse_args()

    root = repo_root()

    if args.self_test:
        return self_test(root)

    files = tracked_files(root)
    findings, read = scan(root, files)

    # A verdict over a corpus nobody established was non-empty is not a verdict (PACK-P). This gate
    # is cheap and repository-wide, so "nothing was read" means the scan itself is broken.
    if read == 0:
        print("verify-no-conflict-markers: FAILED")
        print("  - no tracked text file could be read at all — this gate examined nothing, which is "
              "not the same as finding nothing")
        return 1

    if findings:
        print("verify-no-conflict-markers: FAILED")
        for finding in findings:
            print(f"  - {finding}")
        print()
        print("  A marker in a tracked file means a conflict was resolved by hand and left "
              "unfinished.")
        print("  If the file is DERIVED, do not edit it — regenerate it (§4b item 3):")
        print("      python tools/regenerate-derived.py")
        return 1

    print(f"verify-no-conflict-markers: OK — {read} tracked text file(s) read, no conflict markers.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
