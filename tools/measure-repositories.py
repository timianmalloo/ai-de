#!/usr/bin/env python3
"""Run the real-repository harness over several codebases and compare them side by side.

WHY THIS EXISTS. Every measurement in this project came from one repository (TheTerrace) until a
second and third were tried by hand — and the third immediately exposed a defect in a control that
had looked correct against the first. A control's false-positive rate is only observable on input it
was not written against, and "I ran it by hand once" is not a property anybody can re-check.

WHAT IT IS NOT. Not a test and not a gate: these are real repositories on one machine, they change
under us, and the numbers are evidence rather than assertions. It exists so the evidence can be
REPRODUCED and disputed — the same reason the spike it drives exists.

Usage:
  python tools/measure-repositories.py                    # every repository configured below
  python tools/measure-repositories.py C:/path/to/repo    # ad hoc, one or more
  python tools/measure-repositories.py --json             # machine-readable, for a change log
"""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from pathlib import Path

# Sibling checkouts, chosen for CONTRAST rather than convenience: a large C#+EF codebase, a C#
# codebase with no ORM at all (where "0 joins" is the correct answer and looked like a failure), and
# a TypeScript-heavy one. A repository that is absent is reported as absent, never silently skipped.
DEFAULT_REPOSITORIES = [
    r"C:\Projects\TheTerrace",
    r"C:\Projects\BioHacker",
    r"C:\Projects\meridian-finance-planner",
]

SPIKE = "spikes/joins-on-a-real-repo/spike.csproj"

FIELDS = [
    ("scopes",       re.compile(r"^scopes\s*:\s*(\d+) of (\d+) indexed \((\d[\d,]*) reused\) in ([\d.]+)s", re.M)),
]

PATTERNS = {
    "scopes":        re.compile(r"^scopes\s*:\s*(\d+) of \d+ indexed", re.M),
    "assertions":    re.compile(r"^assertions\s*:\s*([\d,]+)", re.M),
    "nodes":         re.compile(r"^nodes\s*:\s*([\d,]+) drawn", re.M),
    "edges":         re.compile(r"^edges\s*:\s*([\d,]+)", re.M),
    "verified":      re.compile(r"^\s*verified\s+store\s+([\d,]+)", re.M),
    "inferred":      re.compile(r"^\s*inferred\s+store\s+([\d,]+)", re.M),
    "graph_bytes":   re.compile(r"^wire\s*:\s*([\d,]+) bytes", re.M),
    "default_bytes": re.compile(r"^overview\s*:\s*([\d,]+) bytes", re.M),
}

OVERFLOW = re.compile(r"^\s*OVERFLOWS:\s*(.+)$", re.M)
FITS = re.compile(r"every operation fits", re.M)
DEPTH = re.compile(r"^\s*depth (\d+): (\d+) group\(s\), (\d+) link\(s\), (\d+) omitted", re.M)


def repo_root() -> Path:
    out = subprocess.run(
        ["git", "rev-parse", "--show-toplevel"], capture_output=True, text=True, check=True)
    return Path(out.stdout.strip())


def number(text: str | None) -> int | None:
    return int(text.replace(",", "")) if text else None


def measure(root: Path, repository: str) -> dict:
    if not Path(repository).is_dir():
        return {"repository": repository, "error": "not on this machine"}

    result = subprocess.run(
        ["dotnet", "run", "--project", SPIKE, "-c", "Release", "--no-build", "--", repository],
        capture_output=True, text=True, cwd=root, timeout=1800)

    out = result.stdout

    if result.returncode != 0 and not out.strip():
        return {"repository": repository, "error": f"the harness exited {result.returncode}"}

    reading = {"repository": Path(repository).name}

    for name, pattern in PATTERNS.items():
        match = pattern.search(out)
        reading[name] = number(match.group(1)) if match else None

    overflow = OVERFLOW.search(out)
    reading["overflows"] = overflow.group(1).strip() if overflow else None
    reading["all_fit"] = bool(FITS.search(out)) and overflow is None

    # Depth is the overview's zoom control, and which depth is worth OPENING at is a question the
    # canvas has to answer. A depth whose groups have almost no links between them is arithmetically
    # correct and useless as a first view, so the link count is recorded per depth.
    reading["depths"] = [
        {"depth": int(d), "groups": int(g), "links": int(l), "omitted": int(o)}
        for d, g, l, o in DEPTH.findall(out)
    ]

    return reading


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("repositories", nargs="*", help="paths to measure (default: the configured set)")
    parser.add_argument("--json", action="store_true", help="emit readings as JSON")
    args = parser.parse_args()

    root = repo_root()
    targets = args.repositories or DEFAULT_REPOSITORIES

    build = subprocess.run(
        ["dotnet", "build", SPIKE, "-c", "Release", "--nologo", "-v", "q"],
        capture_output=True, text=True, cwd=root)

    if build.returncode != 0:
        print("measure-repositories: the harness does not build; measuring nothing.")
        print(build.stdout[-2000:])
        return 1

    readings = [measure(root, r) for r in targets]

    if args.json:
        print(json.dumps(readings, indent=2))
        return 0

    print(f"{'repository':<28} {'scopes':>7} {'assertions':>11} {'nodes':>7} {'edges':>7} "
          f"{'joins v/i':>11} {'graph B':>10} {'frame':>10}")
    print("-" * 100)

    for r in readings:
        if "error" in r:
            print(f"{Path(r['repository']).name:<28} {r['error']}")
            continue

        joins = f"{r['verified'] or 0}/{r['inferred'] or 0}"
        frame = "all fit" if r["all_fit"] else f"OVERFLOW: {r['overflows']}"

        print(f"{r['repository']:<28} {r['scopes'] or 0:>7} {r['assertions'] or 0:>11,} "
              f"{r['nodes'] or 0:>7,} {r['edges'] or 0:>7,} {joins:>11} "
              f"{r['graph_bytes'] or 0:>10,} {frame:>10}")

    print()
    print("overview depth — groups / links between them / omitted at the cap:")
    print()

    for r in readings:
        if "error" in r or not r.get("depths"):
            continue

        row = "  ".join(
            f"d{d['depth']}: {d['groups']}g {d['links']}l" for d in r["depths"])
        print(f"  {r['repository']:<28} {row}")

    failed = [r for r in readings if "error" not in r and not r["all_fit"]]

    if failed:
        print()
        print("  A response that cannot cross the frame is INV-0003 on that repository.")
        return 1

    return 0


if __name__ == "__main__":
    sys.exit(main())
