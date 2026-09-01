#!/usr/bin/env python3
"""Extraction cannot change without the generation that invalidates cached results changing too.

WHAT HAPPENED. `ScopeFingerprints.ExtractorGeneration` is a constant in every scope fingerprint,
and it exists for exactly one reason: upgrading the product must invalidate the cached sidecar, so
an extractor improvement reaches every file rather than only the ones a user happens to touch
afterwards.

It was last bumped on 2026-08-29. Over the following day the knowledge extractor, `node_class`
classification, comment stripping in four readers, the SQL fold and `uses_table` all shipped —
every one of them changing extraction OUTPUT for input that did not change. Nobody bumped it, so
every existing workspace kept serving results produced by the previous generation, and the user
reported the Knowledge chip reading **0** on a repository holding 2,343 knowledge nodes.

The mechanism was right and complete. The step that used it was a thing somebody had to remember.

WHAT THIS CHECKS. If anything under `src/AiDe.Core/Extraction/` changed since the generation last
changed, the generation must change too. That is deliberately conservative: a comment-only edit
trips it, and the remedy is a one-line bump whose cost is one re-index. The alternative — deciding
which edits "really" change output — is a judgement nobody can make reliably about a compiler-driven
extractor, and getting it wrong is silent.

Exit 0 when clean, 1 otherwise. Stdlib only.
"""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
from pathlib import Path

GENERATION_FILE = "src/AiDe.Core/Extraction/ScopeFingerprints.cs"
GENERATION_PATTERN = r'ExtractorGeneration\s*=\s*"'
WATCHED = "src/AiDe.Core/Extraction/"

# Files whose changes cannot alter what an extractor produces.
IGNORED = {GENERATION_FILE}


def git(*args: str, cwd: Path) -> str:
    return subprocess.run(
        ["git", *args], capture_output=True, text=True, cwd=cwd).stdout.strip()


def repo_root() -> Path:
    return Path(subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        capture_output=True, text=True, check=True).stdout.strip())


def current_generation(root: Path) -> str | None:
    text = (root / GENERATION_FILE).read_text(encoding="utf-8", errors="replace")
    match = re.search(GENERATION_PATTERN + r'([^"]+)"', text)
    return match.group(1) if match else None


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--since", default=None,
        help="compare against this ref instead of the commit that last changed the generation")
    args = parser.parse_args()

    root = repo_root()
    generation = current_generation(root)

    if generation is None:
        print("verify-extractor-generation: FAILED")
        print(f"  - no ExtractorGeneration constant found in {GENERATION_FILE};")
        print("    the mechanism that invalidates cached extraction has been removed or renamed.")
        return 1

    # -G matches commits whose DIFF contains the pattern, so replacing one value with another counts.
    # -S would not: it counts occurrences, and a value swap leaves the count unchanged — which is
    # how a bump can look like no change at all.
    baseline = args.since or git(
        "log", "-1", "--format=%H", "-G", GENERATION_PATTERN, "--", GENERATION_FILE, cwd=root)

    if not baseline:
        print(f"verify-extractor-generation: OK — generation {generation}, "
              "no prior change to compare against (shallow clone or first commit).")
        return 0

    changed = [
        line for line in git(
            "diff", "--name-only", f"{baseline}..HEAD", "--", WATCHED, cwd=root).splitlines()
        if line.strip() and line not in IGNORED
    ]

    if changed:
        print("verify-extractor-generation: FAILED")
        print(f"  - extraction changed since the generation last did ({baseline[:9]}), "
              f"and {GENERATION_FILE.split('/')[-1]} still says {generation}:")

        for path in changed[:10]:
            print(f"      {path}")

        if len(changed) > 10:
            print(f"      (+{len(changed) - 10} more)")

        print()
        print("  Bump ExtractorGeneration. Without it, every existing workspace keeps serving")
        print("  results from the previous generation — which is how the Knowledge chip read 0")
        print("  on a repository holding 2,343 knowledge nodes.")
        return 1

    print(f"verify-extractor-generation: OK — generation {generation}, "
          f"no extraction change since it was set ({baseline[:9]}).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
