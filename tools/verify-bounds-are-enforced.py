#!/usr/bin/env python3
"""A constant that names a bound is compared against something, not just reported.

THE CLASS, found three times in one session:

  * `Evidence` documented that a page "stays comfortably inside MaxResultBytes once serialised".
    MEASURED: 2,000 assertions = 1,004,397 bytes, fifteen times that constant.
  * `Find` built a `ResultBounds` reporting `MaxBytes: 65,536` and returned 461,750 bytes. The cap
    was passed to a struct and never compared to anything (DC-016: a control that cannot fire).
  * The TypeScript miss-counter's comment said `export default someExpression` was excluded; the
    pattern never excluded it, so the disclosure would have fired on nearly every real codebase.

All three are the same shape: **a claim in prose that the code does not make true**. The prose is
where the reviewer looks, so the claim is what gets believed — and each of these survived review.

WHAT THIS CHECKS, and what it deliberately does not. Deciding whether a comment is true needs a
reader. But the mechanical half is checkable and it is where two of the three lived: a constant whose
name says it BOUNDS something must appear in a comparison, a clamp, or a take. A bound that is only
ever assigned, passed as an argument, or named in a comment cannot fire, whatever its documentation
says.

It cannot see the third instance (a regex that did not match what its comment claimed), and says so
rather than implying the class is covered. What it removes is the "declared and never applied" half.

Exit 0 when clean, 1 otherwise. Stdlib only.
"""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
from pathlib import Path

# A constant whose NAME claims it limits something.
BOUND_NAME = re.compile(r"^(Max[A-Za-z]*(Bytes|Ceiling|Nodes|Edges|Results|Length|Paths|Clusters)|[A-Za-z]*Cap|[A-Za-z]*Budget)$")

DECLARATION = re.compile(
    r"^\s*(?:public|private|internal|protected)\s+(?:static\s+)?(?:readonly\s+)?const\s+\w+\s+(\w+)\s*=",
    re.MULTILINE)

# What it means for a bound to actually do something.
ENFORCEMENT = [
    re.compile(r"[<>]=?\s*[\w.]*\b{name}\b"),          # x > Cap
    re.compile(r"\b{name}\b\s*[<>]=?"),                 # Cap < x
    re.compile(r"Clamp\([^)]*\b{name}\b"),              # Clamp(v, 1, Cap)
    re.compile(r"Math\.(Min|Max)\([^)]*\b{name}\b"),
    re.compile(r"\.Take\(\s*[^)]*\b{name}\b"),
    re.compile(r"\bTake\w*\([^)]*\b{name}\b"),
]

# A bound may be declared in one file and enforced in another, so enforcement is searched repo-wide.
SOURCE_GLOB = "*.cs"

# Bounds applied INDIRECTLY, by being handed to a parameter that is itself clamped. Each needs a
# reason naming where it fires, because "it is passed somewhere" is exactly what made `find` look
# safe: MaxResultBytes was passed to a ResultBounds struct and compared to nothing. An entry here is
# a claim a reviewer can check, not a way to quiet the gate.
APPLIED_ELSEWHERE = {
    "OverviewNodeCap":
        "handed to GraphQuery.MaxNodes, which ProjectionService.Graph clamps and GraphProjection "
        "takes against; OversizedResponseTests grows a corpus past it and asserts the response "
        "stops there",
}


def repo_root() -> Path:
    out = subprocess.run(
        ["git", "rev-parse", "--show-toplevel"], capture_output=True, text=True, check=True)
    return Path(out.stdout.strip())


def tracked_sources(root: Path) -> list[Path]:
    out = subprocess.run(
        ["git", "ls-files", SOURCE_GLOB], capture_output=True, text=True, check=True, cwd=root)

    return [
        root / line for line in out.stdout.splitlines()
        if line.startswith("src/") and line.strip()
    ]


def strip_comments(text: str) -> str:
    """Enforcement must be CODE. A bound mentioned only in prose is the defect, not the proof."""
    text = re.sub(r"/\*.*?\*/", "", text, flags=re.DOTALL)
    return re.sub(r"^[ \t]*//.*$", "", text, flags=re.MULTILINE)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--list", action="store_true", help="show every bound and where it is enforced")
    args = parser.parse_args()

    root = repo_root()
    sources = tracked_sources(root)

    bodies = {path: strip_comments(path.read_text(encoding="utf-8", errors="replace")) for path in sources}
    everything = "\n".join(bodies.values())

    bounds: dict[str, Path] = {}

    for path, body in bodies.items():
        for name in DECLARATION.findall(body):
            if BOUND_NAME.match(name):
                bounds.setdefault(name, path)

    if not bounds:
        print("verify-bounds-are-enforced: FAILED — no bound constants found; has the naming changed?")
        return 1

    unenforced = []

    for name, declared_in in sorted(bounds.items()):
        applied = name in APPLIED_ELSEWHERE or any(
            re.search(rule.pattern.format(name=re.escape(name)), everything)
            for rule in ENFORCEMENT)

        if args.list:
            how = "indirect" if name in APPLIED_ELSEWHERE else ("applied " if applied else "DECLARED")
            print(f"  {how:8} {name}  ({declared_in.relative_to(root)})")

        if not applied:
            unenforced.append(
                f"{name} (declared in {declared_in.relative_to(root)}) is never compared, clamped or "
                f"taken against — it can be reported but it cannot fire")

    if unenforced:
        print("verify-bounds-are-enforced: FAILED")
        for problem in unenforced:
            print(f"  - {problem}")
        print()
        print("  A bound that is only assigned, passed or documented is a promise the code does not")
        print("  keep. Apply it, or rename it so it stops claiming to be a limit.")
        return 1

    print(
        f"verify-bounds-are-enforced: OK — {len(bounds)} bound constant(s), every one compared, "
        "clamped or taken against in code.")
    print(
        "  Note: this checks that a bound is APPLIED, not that a comment describing it is true. "
        "The prose half of the class still needs a reader.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
