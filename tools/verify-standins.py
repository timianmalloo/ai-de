#!/usr/bin/env python3
"""A stand-in wired into production code has to say why it is still there.

WHAT HAPPENED (DC-073). `MockNodeContentSource` was written to stand in "until Core ships
NodeContentAsync", behind a seam whose stated purpose was that swapping it would be one line. Core
shipped the query. Nobody swapped it. The code viewer went on showing a labelled `// SAMPLE` against
a fully indexed workspace, and the App contained **zero** calls to the real query.

Every signal was green, and green for the right reasons: the seam existed, the surface rendered, the
tests passed — against the placeholder, which is what they were written to do. The only evidence was
a comparison nobody makes: *is the thing this is waiting for still missing?*

WHAT THIS CHECKS. Every construction of a `Mock*` / `Stub*` / `Sample*` / `Fake*` type outside the
test projects must appear in ALLOWED below, with the condition that makes it legitimate. It is not a
ban — a stand-in is the right answer for a state where the real thing genuinely cannot be asked (no
workspace is open, no credential is configured). It is a forcing function: adding one means writing
down why, and the next person reading the list is asked the question that went unasked for a day.

WHAT IT DELIBERATELY DOES NOT DO. It does not try to decide whether the real implementation exists
yet — that needs semantic analysis, and a wrong answer here would be a false alarm on a control this
repository has already taught people to distrust twice (see verify-id-allocators' history). The list
is short, a human maintains it, and the review it forces is the point.

Exit 0 when clean, 1 otherwise. Stdlib only.
"""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
from pathlib import Path

# Stand-ins that are legitimately constructed in production, and the condition that makes each one
# legitimate. Read this as a question, not a permission: "is that condition still true?"
ALLOWED = {
    "src/AiDe.App/Workbench/WorkbenchShell.cs::MockNodeContentSource":
        "The no-workspace state. With no workspace attached there is no authority to ask, so a "
        "labelled sample is the honest answer; the moment `_queries` is set the shell derives a "
        "CoreNodeContentSource instead. Asserted by "
        "WorkbenchShellTests.AttachWorkspace_SwapsTheSampleContentSourceForTheRealOne.",
}

STANDIN = re.compile(r"\bnew\s+((?:Mock|Stub|Sample|Fake)[A-Za-z0-9_]*)\s*[\(<]")

# Where production code lives. A stand-in inside a test IS the point of a test.
# WIDENED 2026-09-01. It scanned only `src/`, which was an assumption I never tested — the same
# shape of blind spot as a guard reporting clean over a namespace it was not looking at. `spikes/`
# is measurement code that legitimately fakes things, and `tests/` is where a stand-in belongs, so
# both stay out; `tools/` ships behaviour and is now in scope.
PRODUCTION = ("src/", "tools/")


def repo_root() -> Path:
    return Path(subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        capture_output=True, text=True, check=True).stdout.strip())


def tracked(root: Path) -> list[str]:
    out = subprocess.run(
        ["git", "ls-files", "*.cs"], capture_output=True, text=True, check=True, cwd=root)

    return [p for p in out.stdout.splitlines() if p.startswith(PRODUCTION)]


def findings(root: Path) -> tuple[list[str], list[str]]:
    problems: list[str] = []
    seen: list[str] = []

    for relative in tracked(root):
        try:
            text = (root / relative).read_text(encoding="utf-8", errors="replace")
        except OSError:
            continue

        for number, line in enumerate(text.splitlines(), start=1):
            stripped = line.lstrip()

            # A comment ABOUT a stand-in is not a stand-in. DC-073's own explanation quotes the
            # construction it replaced, and a checker that could not tell the difference would fail
            # on the very entry that records the lesson.
            if stripped.startswith("//") or stripped.startswith("///") or stripped.startswith("*"):
                continue

            for match in STANDIN.finditer(line):
                key = f"{relative}::{match.group(1)}"
                seen.append(key)

                if key not in ALLOWED:
                    problems.append(
                        f"{relative}:{number} constructs {match.group(1)} in production and no "
                        f"reason is recorded. Add it to ALLOWED in tools/verify-standins.py with "
                        f"the condition that makes it right — or wire the real implementation. A "
                        f"stand-in whose replacement already exists is invisible precisely because "
                        f"it was planned (DC-073).")

    # A stale allowance is the same defect one level up: it keeps a question alive that nobody has
    # to answer any more, and it makes the list longer than the thing it describes.
    for key in sorted(set(ALLOWED) - set(seen)):
        problems.append(
            f"ALLOWED lists {key}, which is no longer constructed anywhere in production — remove "
            "the entry so the list keeps describing the code.")

    return problems, seen


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--self-test", action="store_true",
        help="prove the control fires: an unlisted stand-in must fail")
    args = parser.parse_args()

    root = repo_root()

    if args.self_test:
        return self_test(root)

    problems, seen = findings(root)

    if problems:
        print("verify-standins: FAILED")
        for problem in problems:
            print(f"  - {problem}")
        return 1

    print(f"verify-standins: OK — {len(seen)} stand-in(s) in production, each with a recorded reason.")
    return 0


def self_test(root: Path) -> int:
    """The control must be observed FAILING, or it is not a control (CI6)."""
    import tempfile

    with tempfile.TemporaryDirectory() as directory:
        place = Path(directory)
        (place / "src").mkdir()

        subprocess.run(["git", "init", "-q"], cwd=place, capture_output=True, check=True)

        (place / "src" / "Thing.cs").write_text(
            "class Thing\n{\n"
            "    // A mock until something ships — this LINE is a comment and must not fire.\n"
            "    private readonly IThing _real = new MockThing();\n"
            "}\n",
            encoding="utf-8")

        subprocess.run(["git", "add", "-A"], cwd=place, capture_output=True, check=True)

        problems, seen = findings(place)

    for problem in problems:
        print(f"  planted -> {problem}")

    if not any("MockThing" in p and "no reason is recorded" in p for p in problems):
        print("verify-standins: SELF-TEST FAILED — an unlisted stand-in was not reported.")
        return 1

    if len(seen) != 1:
        print(f"verify-standins: SELF-TEST FAILED — found {len(seen)} construction(s) where the "
              "fixture has exactly one; the comment line was counted as code.")
        return 1

    print("verify-standins: self-test OK — an unlisted stand-in fails, and a comment about one "
          "does not.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
