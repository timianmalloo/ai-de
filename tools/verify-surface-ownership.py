#!/usr/bin/env python3
"""Every surface has a declared owner, or is named as one nobody has assigned yet.

WHAT HAPPENED (§8.11). `docs/collaboration/session-contracts.md` §2 is the authority on file
ownership. It assigns four of the thirteen surface and renderer files in `src/AiDe.App/Workbench/`.
Nine have no entry — including `SearchSurface.cs`, which was half of one day's work.

THE FAILURE MODE IS OMISSION, WHICH IS WHY NOBODY NOTICED. Every line in §2 is still correct. It
covers what existed when it was written and nothing built since, so it fails only by not saying
anything — and nothing checks what a document does not say. A stale allowance describes a state that
no longer exists and can be caught by re-reading it; this cannot.

AND IT CAUSED A REAL MISROUTE. With no entry to look up, an owner gets inferred from what the
symptom looks like: a rendering defect in `SurfaceContentFactory.cs` — a **Core**-owned registry —
was filed to the design session, who could not have fixed it. A map with holes is worse than no map,
because it is consulted with confidence.

WHAT THIS CHECKS.
  * Every `*Surface.cs` / `*View.cs` under Workbench appears in exactly ONE of §2's owner tables,
    or is listed in UNASSIGNED below.
  * No file appears in TWO tables — that is the §8.2 contradiction, mechanised.
  * No UNASSIGNED entry names a file that §2 has since assigned, or that no longer exists.

WHAT IT DELIBERATELY DOES NOT DO. It cannot decide who *should* own a new surface. That is a
judgement between the sessions and stays human. It refuses to let a surface exist with no answer,
which is the forcing-function shape `verify-standins.py` uses.

Exit 0 when clean, 1 otherwise. Stdlib only.
"""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
from pathlib import Path

CONTRACT = "docs/collaboration/session-contracts.md"
SURFACES = "src/AiDe.App/Workbench"

# Surfaces §2 has never assigned. Listed so the gap is a recorded decision-to-make rather than an
# omission nobody can see. REMOVING an entry is the point: it happens when §2 gains a row.
#
# Assigning these is a joint call between the core and design sessions and is NOT made here — a gate
# that picked owners would be one session deciding another's scope by writing a script.
UNASSIGNED: dict[str, str] = {
    # EMPTY, and that is the goal state rather than a missing list.
    #
    # It held nine surfaces built after §2 was written — including SearchSurface, half of one day's
    # work. §2 was reconciled on 2026-09-01 and every one of them now has an owner, so each entry
    # became a description of a state that no longer exists. The gate's stale-allowance half caught
    # all nine the moment the rows landed, which is the half of a forcing function that keeps the
    # list from outliving its subject.
    #
    # A new surface with no owner fails the check. Adding it here is the escape hatch when the
    # assignment needs a decision nobody has made yet — with the reason, and not for long.
}

OWNER_TABLE = re.compile(r"^### (.+?) owns\s*$", re.MULTILINE)
FILE_NAME = re.compile(r"([A-Za-z][A-Za-z0-9_]*(?:Surface|View|Page|Builder)\.cs)")


def repo_root() -> Path:
    return Path(subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        capture_output=True, text=True, check=True).stdout.strip())


def surfaces(root: Path) -> list[str]:
    directory = root / SURFACES

    if not directory.is_dir():
        return []

    return sorted(
        f.name for f in directory.iterdir()
        if f.is_file() and (f.name.endswith("Surface.cs") or f.name.endswith("View.cs")))


def owners(root: Path) -> dict[str, list[str]]:
    """Which owner table names each file, from §2 only."""
    path = root / CONTRACT

    if not path.exists():
        return {}

    text = path.read_text(encoding="utf-8", errors="replace")

    # §2 only. A file named in a later section is prose about it, not an assignment — the same
    # distinction that made verify-id-allocators read ADR ids out of the wrong file once.
    start = text.find("## 2. File ownership")
    end = text.find("## 3.", start + 1)

    if start < 0:
        return {}

    section = text[start:end if end > 0 else len(text)]

    found: dict[str, list[str]] = {}
    table = "(unnamed)"

    for line in section.splitlines():
        heading = OWNER_TABLE.match(line)

        if heading:
            table = heading.group(1)
            continue

        for name in FILE_NAME.findall(line):
            found.setdefault(name, [])
            if table not in found[name]:
                found[name].append(table)

    return found


def check(root: Path, unassigned: dict[str, str] | None = None) -> list[str]:
    # The list is a PARAMETER so the self-test can plant its own. Checking the real one against a
    # fixture repository would report every real surface as missing and bury the planted finding.
    unassigned = UNASSIGNED if unassigned is None else unassigned

    problems: list[str] = []
    present = surfaces(root)
    assigned = owners(root)

    if not present:
        return [f"no surface files found under {SURFACES} — this check is looking at nothing"]

    for name in present:
        tables = assigned.get(name, [])

        if len(tables) > 1:
            problems.append(
                f"{name} is assigned to more than one owner in §2 ({', '.join(tables)}) — two "
                "owners is the contradiction §8.2 was about, and both will assume the other has it")
            continue

        if tables:
            continue

        if name in unassigned:
            continue

        problems.append(
            f"{name} has no owner in §2 and is not listed as unassigned. With no entry to look up, "
            "an owner gets inferred from what the symptom looks like — which has already sent a "
            "Core-owned registry defect to the design session. Add a row to §2, or add it to "
            "UNASSIGNED in tools/verify-surface-ownership.py with the reason.")

    for name, why in sorted(unassigned.items()):
        if name not in present:
            problems.append(
                f"UNASSIGNED lists {name} ({why}), which no longer exists — remove the entry so the "
                "list keeps describing the code")
        elif assigned.get(name):
            problems.append(
                f"UNASSIGNED lists {name}, but §2 now assigns it to "
                f"{', '.join(assigned[name])} — remove the entry")

    return problems


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--self-test", action="store_true",
        help="prove the control fires: an unowned, unlisted surface must fail")
    args = parser.parse_args()

    root = repo_root()

    if args.self_test:
        return self_test()

    problems = check(root)

    if problems:
        print("verify-surface-ownership: FAILED")
        for problem in problems:
            print(f"  - {problem}")
        return 1

    present = surfaces(root)
    owned = sum(1 for n in present if n not in UNASSIGNED)

    print(
        f"verify-surface-ownership: OK — {len(present)} surface(s), {owned} assigned in §2, "
        f"{len(UNASSIGNED)} recorded as awaiting a joint decision.")
    return 0


def self_test() -> int:
    """The control must be observed FAILING, or it is not a control (CI6)."""
    import tempfile

    with tempfile.TemporaryDirectory() as directory:
        place = Path(directory)
        (place / SURFACES).mkdir(parents=True)
        (place / "docs" / "collaboration").mkdir(parents=True)

        (place / CONTRACT).write_text(
            "## 2. File ownership\n\n"
            "### Core owns\n\n"
            "| Path | Why |\n|---|---|\n"
            "| `src/AiDe.App/Workbench/KnownSurface.cs` | assigned |\n\n"
            "## 3. Next\n", encoding="utf-8")

        for name in ("KnownSurface.cs", "OrphanSurface.cs"):
            (place / SURFACES / name).write_text("// probe\n", encoding="utf-8")

        problems = check(place, unassigned={})

    for problem in problems:
        print(f"  planted -> {problem.splitlines()[0]}")

    if not any("OrphanSurface.cs has no owner" in p for p in problems):
        print("verify-surface-ownership: SELF-TEST FAILED — an unowned surface was not reported.")
        return 1

    if any("KnownSurface.cs has no owner" in p for p in problems):
        print("verify-surface-ownership: SELF-TEST FAILED — a surface §2 DOES assign was reported "
              "as unowned; the gate would be red on a correct contract.")
        return 1

    print("verify-surface-ownership: self-test OK — an unowned surface fails, an owned one does not.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
