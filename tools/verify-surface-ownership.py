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
import os
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

OWNER_TABLE = re.compile(r"^### (.+?) owns\s*$")
MARKDOWN_HEADING = re.compile(r"^#{1,6}\s+")
CODE_TOKEN = re.compile(r"`([^`]+)`")
SURFACE_NAME = re.compile(r"[^/\s`|]*(?:Surface|View)\.cs")
SURFACE_PATTERN = re.compile(re.escape(SURFACES) + r"/[^\s`|]*\*[^\s`|]*")
TABLE_SEPARATOR = re.compile(r"^:?-{3,}:?$")
PROVENANCE = re.compile(r"(?:\breq-[A-Za-z0-9]+\b|\bRuling\s+\d+\b)", re.IGNORECASE)
RETIREMENT = re.compile(r"\b(?:remove|retire)\s+when\b|\buntil\b", re.IGNORECASE)


def repo_root() -> Path:
    return Path(subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        capture_output=True, text=True, encoding="utf-8", errors="replace", check=True).stdout.strip())


def surfaces(root: Path) -> list[str]:
    """Discover recursive Workbench Surface.cs/View.cs files as root-relative POSIX paths.

    Scan contract (DC-118 control half (b)): root ``src/AiDe.App/Workbench``; recursive;
    token set exactly ``Surface.cs`` and ``View.cs`` suffixes; allowlist empty.
    """
    directory = root / SURFACES

    if not directory.is_dir():
        return []

    return sorted(
        f.relative_to(root).as_posix() for f in directory.rglob("*")
        if f.is_file() and (f.name.endswith("Surface.cs") or f.name.endswith("View.cs")))


def _surface_relevant(value: str) -> bool:
    return (SURFACE_NAME.search(value) is not None or
            SURFACE_PATTERN.search(value) is not None)


def _table_cells(line: str) -> list[str] | None:
    stripped = line.strip()
    if not stripped.startswith("|") or not stripped.endswith("|"):
        return None
    return [cell.strip() for cell in stripped[1:-1].split("|")]


def _path_problem(token: str) -> str | None:
    if "\\" in token:
        return "backslashes are not repository-relative POSIX separators"
    if token.startswith("/") or re.match(r"^[A-Za-z]:", token):
        return "absolute paths are forbidden"
    parts = token.split("/")
    if any(part in ("", ".", "..") for part in parts):
        return "empty, dot, and traversal segments are forbidden"
    if any(character in token for character in "?[]{}"):
        return "only segment-local '*' and a trailing '/**' are supported"
    if "**" in token and (not token.endswith("/**") or token.count("**") != 1):
        return "recursive '/**' is supported only at the end"
    if not token.startswith(SURFACES + "/"):
        return f"surface declarations must be under {SURFACES}"
    return None


def _pattern_matches(pattern: str, path: str) -> bool:
    if pattern.endswith("/**"):
        prefix = pattern[:-3]
        return path.startswith(prefix + "/")
    expression = "^" + re.escape(pattern).replace(r"\*", "[^/]*") + "$"
    return re.fullmatch(expression, path) is not None


def _resolve_token(
        token: str,
        context: str | None,
        present: list[str],
) -> tuple[str | None, list[str], str | None]:
    """Resolve one Path-cell token and return next same-cell directory context."""
    token = token.strip()
    qualified = "/" in token or "\\" in token

    if qualified:
        problem = _path_problem(token)
        if problem:
            return context, [], problem
        resolved = token
        next_context = token.rsplit("/", 1)[0]
    elif context is not None:
        resolved = f"{context}/{token}"
        next_context = context
        problem = _path_problem(resolved)
        if problem:
            return context, [], problem
    else:
        if "*" in token:
            return context, [], "a bare pattern has no directory context"
        candidates = [path for path in present if path.rsplit("/", 1)[-1] == token]
        if not candidates:
            return context, [], "no discovered candidate has this bare filename"
        if len(candidates) > 1:
            return context, [], "ambiguous bare filename; candidates: " + ", ".join(candidates)
        return context, candidates, None

    matches = [path for path in present if _pattern_matches(resolved, path)]
    if not matches and not qualified:
        return next_context, [], "same-cell shorthand matches no discovered surface"
    return next_context, matches, None


def _parse_owners(root: Path, present: list[str]) -> tuple[dict[str, list[str]], list[str]]:
    """Read ownership declarations only from Path cells in §2 owner tables."""
    path = root / CONTRACT

    if not path.exists():
        return {}, [f"ownership contract {CONTRACT} does not exist"]

    text = path.read_text(encoding="utf-8", errors="replace")
    lines = text.splitlines()
    start = next((index for index, line in enumerate(lines)
                  if line.strip() == "## 2. File ownership"), None)
    if start is None:
        return {}, [f"ownership contract {CONTRACT} has no '## 2. File ownership' section"]
    end = next((index for index in range(start + 1, len(lines))
                if lines[index].lstrip().startswith("## ")), len(lines))
    found: dict[str, list[str]] = {}
    problems: list[str] = []
    owner: str | None = None
    path_column: int | None = None
    table_started = False
    non_path_table = False

    for offset, line in enumerate(lines[start + 1:end], start=start + 2):
        heading = OWNER_TABLE.fullmatch(line.strip())
        if heading:
            owner = heading.group(1).strip()
            path_column = None
            table_started = False
            non_path_table = False
            continue
        if MARKDOWN_HEADING.match(line.strip()):
            owner = None
            path_column = None
            table_started = False
            non_path_table = False
            continue

        cells = _table_cells(line)
        if cells is not None and "Path" in cells:
            path_column = cells.index("Path")
            table_started = True
            non_path_table = False
            continue
        if cells is None:
            non_path_table = False
            stripped = line.strip()
            row_without_pipes = stripped.startswith("`") and _surface_relevant(stripped)
            if table_started and stripped and _surface_relevant(line) and (
                    "|" in line or row_without_pipes):
                problems.append(
                    f"line {offset} has a malformed Path-table row containing a surface path: "
                    f"{stripped}")
            if stripped:
                table_started = False
            continue
        if path_column is None:
            # Historical allocation tables are not declarations. Require their actual
            # header/separator shape outside owner context; a mangled owner header
            # must still expose its surface rows instead of becoming an exemption.
            separator = _table_cells(lines[offset]) if offset < end else None
            if (owner is None and len(cells) >= 2 and all(cells) and
                    not _surface_relevant(line) and separator is not None and
                    len(separator) == len(cells) and
                    all(TABLE_SEPARATOR.fullmatch(cell) for cell in separator)):
                non_path_table = True
            if non_path_table:
                continue
            if _surface_relevant(line):
                problems.append(
                    f"line {offset} has a malformed surface Path-table row before a Path header: "
                    f"{line.strip()}")
            continue
        table_started = True
        if all(TABLE_SEPARATOR.fullmatch(cell) for cell in cells):
            continue
        if path_column >= len(cells):
            if _surface_relevant(line):
                problems.append(f"line {offset} has a malformed Path-table row with no Path cell")
            continue

        path_cell = cells[path_column]
        code_tokens = CODE_TOKEN.findall(path_cell)
        unquoted = CODE_TOKEN.sub("", path_cell)
        if _surface_relevant(unquoted):
            problems.append(
                f"line {offset} has a malformed surface declaration in the Path cell; "
                f"paths must be code tokens: {unquoted.strip()}")
        relevant_tokens = [token for token in code_tokens if _surface_relevant(token)]
        if not relevant_tokens:
            continue
        if owner is None:
            problems.append(
                f"line {offset} has a malformed surface declaration outside a '### <owner> owns' "
                f"table: {', '.join(f'`{token}`' for token in relevant_tokens)}")
            continue

        context: str | None = None
        for token in code_tokens:
            if not _surface_relevant(token):
                if "/" in token and "\\" not in token:
                    context = token.rsplit("/", 1)[0]
                continue
            context, matches, problem = _resolve_token(token, context, present)
            if problem:
                problems.append(f"line {offset} has malformed Path token `{token}`: {problem}")
                continue
            for matched in matches:
                owners = found.setdefault(matched, [])
                if owner not in owners:
                    owners.append(owner)

    return ({path: sorted(owner_names) for path, owner_names in sorted(found.items())},
            sorted(set(problems)))


def owners(root: Path) -> dict[str, list[str]]:
    """Which §2 owner tables name each discovered root-relative surface path."""
    present = surfaces(root)
    return _parse_owners(root, present)[0]


def check(root: Path, unassigned: dict[str, str] | None = None) -> list[str]:
    # The list is a PARAMETER so the self-test can plant its own. Checking the real one against a
    # fixture repository would report every real surface as missing and bury the planted finding.
    unassigned = UNASSIGNED if unassigned is None else unassigned

    problems: list[str] = []
    present = surfaces(root)

    if not present:
        return [f"no surface files found under {SURFACES} — this check is looking at nothing"]

    assigned, parse_problems = _parse_owners(root, present)
    problems.extend(parse_problems)

    for path in present:
        tables = assigned.get(path, [])

        if len(tables) > 1:
            problems.append(
                f"{path} is assigned to more than one owner in §2 ({', '.join(tables)}) — two "
                "owners is the contradiction §8.2 was about, and both will assume the other has it")
            continue

        if tables:
            continue

        if path in unassigned:
            continue

        problems.append(
            f"{path} has no owner in §2 and is not listed as unassigned. With no entry to look up, "
            "an owner gets inferred from what the symptom looks like — which has already sent a "
            "Core-owned registry defect to the design session. Add a row to §2, or add it to "
            "UNASSIGNED in tools/verify-surface-ownership.py with the reason.")

    for declared_path, why in sorted(unassigned.items()):
        canonical_problem = _path_problem(declared_path)
        if (canonical_problem or "*" in declared_path or
                not (declared_path.endswith("Surface.cs") or declared_path.endswith("View.cs"))):
            problems.append(
                f"UNASSIGNED path {declared_path!r} is not a canonical full surface path under "
                f"{SURFACES}")
            continue
        if not why.strip():
            problems.append(
                f"UNASSIGNED lists {declared_path} without a nonblank reason")
        else:
            if not PROVENANCE.search(why):
                problems.append(
                    f"UNASSIGNED reason for {declared_path} must cite a request or ruling")
            if not RETIREMENT.search(why):
                problems.append(
                    f"UNASSIGNED reason for {declared_path} must state a retirement condition")
        if declared_path not in present:
            problems.append(
                f"UNASSIGNED lists {declared_path} ({why}), which no longer exists — remove the entry so the "
                "list keeps describing the code")
        elif assigned.get(declared_path):
            problems.append(
                f"UNASSIGNED lists {declared_path}, but §2 now assigns it to "
                f"{', '.join(assigned[declared_path])} — remove the entry")

    return sorted(set(problems))


def run(root: Path, unassigned: dict[str, str] | None = None) -> int:
    """Render the CLI result for a repository root and return its process exit code."""
    effective_unassigned = UNASSIGNED if unassigned is None else unassigned
    problems = check(root, effective_unassigned)

    if problems:
        print("verify-surface-ownership: FAILED")
        for problem in problems:
            print(f"  - {problem}")
        return 1

    present = surfaces(root)
    assigned = owners(root)
    owned = sum(1 for path in present if len(assigned.get(path, [])) == 1)
    print(
        f"verify-surface-ownership: OK — {len(present)} surface(s), {owned} assigned in §2, "
        f"{len(effective_unassigned)} recorded as awaiting a joint decision.")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--self-test", action="store_true",
        help="prove the control fires: an unowned, unlisted surface must fail")
    args = parser.parse_args()

    root = repo_root()

    if args.self_test:
        return self_test()

    return run(root)


def self_test() -> int:
    """Exercise the recursive identity, parser boundary, exception, and CLI contracts."""
    import tempfile

    failures: list[str] = []

    def fixture(place: Path, files: list[str], section: str) -> None:
        for relative in files:
            target = place / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_text("// probe\n", encoding="utf-8")
        contract = place / CONTRACT
        contract.parent.mkdir(parents=True, exist_ok=True)
        contract.write_text(
            "## 2. File ownership\n\n" + section + "\n## 3. Later\n",
            encoding="utf-8")

    def require(name: str, condition: bool, detail: str) -> None:
        if not condition:
            failures.append(f"{name}: {detail}")

    def has(problems: list[str], *parts: str) -> bool:
        return any(all(part in problem for part in parts) for problem in problems)

    with tempfile.TemporaryDirectory() as directory:
        base = Path(directory)

        recursion = base / "recursion"
        fixture(
            recursion,
            [
                f"{SURFACES}/RootSurface.cs",
                f"{SURFACES}/Nested/KnownView.cs",
                f"{SURFACES}/Other/OrphanView.cs",
            ],
            "### Core owns\n\n"
            "| Path | Why |\n|---|---|\n"
            f"| `{SURFACES}/RootSurface.cs` | exact |\n"
            f"| `{SURFACES}/Nested/KnownView.cs` | nested |\n")
        expected_paths = [
            f"{SURFACES}/Nested/KnownView.cs",
            f"{SURFACES}/Other/OrphanView.cs",
            f"{SURFACES}/RootSurface.cs",
        ]
        require("recursive repository-relative identities", surfaces(recursion) == expected_paths,
                f"got {surfaces(recursion)!r}")
        recursion_problems = check(recursion, unassigned={})
        require("nested unowned surface", has(
            recursion_problems, f"{SURFACES}/Other/OrphanView.cs", "has no owner"),
            f"got {recursion_problems!r}")
        require("nested exact ownership", not has(
            recursion_problems, f"{SURFACES}/Nested/KnownView.cs", "has no owner"),
            f"got {recursion_problems!r}")

        exact = base / "exact"
        fixture(
            exact,
            [f"{SURFACES}/AnchorSurface.cs", f"{SURFACES}/A/DuplicateView.cs",
             f"{SURFACES}/B/DuplicateView.cs"],
            "### Core owns\n\n| Path | Why |\n|---|---|\n"
            f"| `{SURFACES}/AnchorSurface.cs` | anchor |\n"
            f"| `{SURFACES}/A/DuplicateView.cs` | A |\n\n"
            "### Design owns\n\n| Path | Why |\n|---|---|\n"
            f"| `{SURFACES}/B/DuplicateView.cs` | B |\n")
        require("duplicate basenames keep exact identities", check(exact, unassigned={}) == [],
                f"got {check(exact, unassigned={})!r}")

        ambiguous = base / "ambiguous"
        fixture(
            ambiguous,
            [f"{SURFACES}/AnchorSurface.cs", f"{SURFACES}/A/DuplicateView.cs",
             f"{SURFACES}/B/DuplicateView.cs"],
            "### Core owns\n\n| Path | Why |\n|---|---|\n"
            f"| `{SURFACES}/AnchorSurface.cs` | anchor |\n"
            f"| `{SURFACES}/A/DuplicateView.cs` | exact does not remove ambiguity |\n"
            "| `DuplicateView.cs` | ambiguous shorthand |\n")
        ambiguous_problems = check(ambiguous, unassigned={})
        require("ambiguous bare filename names every candidate", has(
            ambiguous_problems, "DuplicateView.cs", f"{SURFACES}/A/DuplicateView.cs",
            f"{SURFACES}/B/DuplicateView.cs"), f"got {ambiguous_problems!r}")

        grouped = base / "grouped"
        fixture(
            grouped,
            [f"{SURFACES}/Group/FirstSurface.cs", f"{SURFACES}/Group/SecondView.cs",
             f"{SURFACES}/Other/ThirdSurface.cs"],
            "### Core owns\n\n| Path | Why |\n|---|---|\n"
            f"| `{SURFACES}/Group/FirstSurface.cs`, `SecondView.cs`, "
            f"`{SURFACES}/Other/ThirdSurface.cs` | grouped |\n")
        require("grouped shorthand inherits only same-cell directory",
                check(grouped, unassigned={}) == [], f"got {check(grouped, unassigned={})!r}")

        grouped_reset = base / "grouped-reset"
        fixture(
            grouped_reset,
            [f"{SURFACES}/A/FirstSurface.cs", f"{SURFACES}/A/SecondView.cs"],
            "### Core owns\n\n| Path | Why |\n|---|---|\n"
            f"| `{SURFACES}/A/FirstSurface.cs`, `src/Other/Marker.cs`, `SecondView.cs` | reset |\n")
        require("a new qualified token resets grouped directory context", has(
            check(grouped_reset, unassigned={}), "SecondView.cs", "malformed", "under"),
            f"got {check(grouped_reset, unassigned={})!r}")

        overlap = base / "overlap"
        fixture(
            overlap,
            [f"{SURFACES}/OneSurface.cs", f"{SURFACES}/Nested/TwoView.cs"],
            "### Core owns\n\n| Path | Why |\n|---|---|\n"
            f"| `{SURFACES}/*Surface.cs` | pattern |\n"
            f"| `{SURFACES}/OneSurface.cs` | same-owner duplicate |\n"
            f"| `{SURFACES}/Nested/**` | recursive pattern |\n")
        require("same-owner overlap deduplicates", check(overlap, unassigned={}) == [],
                f"got {check(overlap, unassigned={})!r}")

        segment_star = base / "segment-star"
        fixture(
            segment_star,
            [f"{SURFACES}/RootView.cs", f"{SURFACES}/Nested/DeepView.cs"],
            "### Core owns\n\n| Path | Why |\n|---|---|\n"
            f"| `{SURFACES}/*View.cs` | one segment only |\n")
        require("ordinary star never crosses a slash", has(
            check(segment_star, unassigned={}), f"{SURFACES}/Nested/DeepView.cs", "has no owner"),
            f"got {check(segment_star, unassigned={})!r}")

        broad_star = base / "broad-star"
        fixture(
            broad_star,
            [f"{SURFACES}/HiddenView.cs"],
            "### Core owns\n\n| Path | Why |\n|---|---|\n"
            f"| `{SURFACES}/**` | core broad |\n\n"
            "### Design owns\n\n| Path | Why |\n|---|---|\n"
            f"| `{SURFACES}/*.cs` | supported segment star |\n")
        require("segment-local broad pattern remains declaration-relevant", has(
            check(broad_star, unassigned={}), f"{SURFACES}/HiddenView.cs",
            "more than one owner", "Core", "Design"),
            f"got {check(broad_star, unassigned={})!r}")

        suffix_population = base / "suffix-population"
        suffix_paths = [
            f"{SURFACES}/Surface.cs",
            f"{SURFACES}/View.cs",
            f"{SURFACES}/_OddView.cs",
            f"{SURFACES}/ÉcranView.cs",
        ]
        fixture(
            suffix_population,
            suffix_paths,
            "### Core owns\n\n| Path | Why |\n|---|---|\n" +
            "".join(f"| `{path}` | exact suffix population |\n" for path in suffix_paths))
        require("every suffix-admitted filename remains declaration-relevant",
                check(suffix_population, unassigned={}) == [],
                f"got {check(suffix_population, unassigned={})!r}")

        overlap_contract = overlap / CONTRACT
        overlap_contract.write_text(
            overlap_contract.read_text(encoding="utf-8").replace(
                "## 3. Later", "### Design owns\n\n| Path | Why |\n|---|---|\n"
                f"| `{SURFACES}/OneSurface.cs` | conflict |\n\n## 3. Later"),
            encoding="utf-8")
        require("cross-owner overlap always conflicts", has(
            check(overlap, unassigned={}), f"{SURFACES}/OneSurface.cs",
            "more than one owner", "Core", "Design"),
            f"got {check(overlap, unassigned={})!r}")

        contamination = base / "contamination"
        fixture(
            contamination,
            [f"{SURFACES}/AnchorSurface.cs", f"{SURFACES}/ProseView.cs",
             f"{SURFACES}/WhyView.cs", f"{SURFACES}/SharedView.cs",
             f"{SURFACES}/LaterView.cs"],
            "### Core owns\n\n"
            f"Prose about `{SURFACES}/ProseView.cs` is not an assignment.\n\n"
            "| Path | Why |\n|---|---|\n"
            f"| `{SURFACES}/AnchorSurface.cs` | mentions `{SURFACES}/WhyView.cs` |\n\n"
            "### Shared, and therefore rule-bound\n\n| Path | Rule |\n|---|---|\n"
            f"| `{SURFACES}/SharedView.cs` | shared is not an owner |\n")
        (contamination / CONTRACT).write_text(
            (contamination / CONTRACT).read_text(encoding="utf-8") +
            f"\n| `{SURFACES}/LaterView.cs` | outside section 2 |\n",
            encoding="utf-8")
        contamination_problems = check(contamination, unassigned={})
        for filename in ("ProseView.cs", "WhyView.cs", "SharedView.cs", "LaterView.cs"):
            require(f"Path-cell-only scope excludes {filename}", has(
                contamination_problems, f"{SURFACES}/{filename}", "has no owner"),
                f"got {contamination_problems!r}")
        require("ownerless Path table is diagnosed", has(
            contamination_problems, "SharedView.cs", "malformed", "outside"),
            f"got {contamination_problems!r}")

        nested_heading = base / "nested-heading"
        fixture(
            nested_heading,
            [f"{SURFACES}/HiddenView.cs"],
            "### Core owns\n\n| Path | Why |\n|---|---|\n"
            f"| `{SURFACES}/**` | broad |\n\n"
            "#### Notes, not an owner\n\n| Path | Why |\n|---|---|\n"
            f"| `{SURFACES}/HiddenView.cs` | outside owner context |\n")
        require("any non-owner heading resets owner context", has(
            check(nested_heading, unassigned={}), f"{SURFACES}/HiddenView.cs", "outside", "owner"),
            f"got {check(nested_heading, unassigned={})!r}")

        historical_tables = {
            "branch allocation": (
                "| Writer / branch | Exact new authored files | Limit |\n|---|---|---|\n"
                f"| `atlas-writer` / `atlas/branch` | `{SURFACES}/AtlasReaderView.cs`; "
                "`tests/AiDe.App.Tests/AtlasReaderViewTests.cs` | bounded history |\n"),
            "track assignment": (
                "| Existing/new | Exact authored path |\n|---|---|\n"
                f"| Existing | `{SURFACES}/AtlasReaderView.cs` |\n"),
        }
        for index, (name, table) in enumerate(historical_tables.items()):
            historical = base / f"historical-{index}"
            fixture(
                historical,
                [f"{SURFACES}/AtlasReaderView.cs"],
                "### Core owns\n\n| Path | Why |\n|---|---|\n"
                f"| `{SURFACES}/AtlasReaderView.cs` | canonical owner |\n\n"
                "### Historical allocation, not an owner\n\n" + table)
            require(f"historical non-Path {name} is not a malformed declaration",
                    check(historical, unassigned={}) == [],
                    f"got {check(historical, unassigned={})!r}")
            require(f"historical non-Path {name} contributes no owner",
                    owners(historical) == {f"{SURFACES}/AtlasReaderView.cs": ["Core"]},
                    f"got {owners(historical)!r}")
            contract = historical / CONTRACT
            contract.write_text(contract.read_text(encoding="utf-8").replace(
                f"| `{SURFACES}/AtlasReaderView.cs` | canonical owner |\n", ""),
                encoding="utf-8")
            require(f"historical non-Path {name} cannot fill an ownership gap", has(
                check(historical, unassigned={}), "AtlasReaderView.cs", "has no owner"),
                f"got {check(historical, unassigned={})!r}")

        header_boundaries = {
            "owner mangled Path": "### Design owns\n\n| Pth | Why |\n|---|---|\n",
            "owner renamed Path": "### Design owns\n\n| Exact authored path | Why |\n|---|---|\n",
            "owner missing Path": "### Design owns\n\n",
            "ownerless missing Path": "### Notes\n\n",
            "non-Path header without separator": "### Notes\n\n| Existing/new | Exact authored path |\n",
            "non-Path header mismatched separator": "### Notes\n\n| Existing/new | Exact authored path |\n|---|\n",
            "non-Path table ended by blank": "### Notes\n\n" + historical_tables["track assignment"] + "\n",
            "non-Path table ended by prose": "### Notes\n\n" + historical_tables["track assignment"] + "End of history.\n",
            "non-Path table ended by owner heading": "### Notes\n\n" + historical_tables["track assignment"] + "### Design owns\n",
            "non-Path table followed by ownerless Path table": "### Notes\n\n" + historical_tables["track assignment"] + "| Path | Why |\n|---|---|\n",
        }
        for index, (name, prefix) in enumerate(header_boundaries.items()):
            boundary = base / f"header-boundary-{index}"
            fixture(
                boundary,
                [f"{SURFACES}/HiddenView.cs", f"{SURFACES}/AtlasReaderView.cs"],
                "### Core owns\n\n| Path | Why |\n|---|---|\n"
                f"| `{SURFACES}/**` | broad owner must not mask malformed rows |\n\n" +
                prefix + f"| `{SURFACES}/HiddenView.cs` | malformed declaration |\n")
            require(f"{name} retains malformed surface evidence", has(
                check(boundary, unassigned={}), "HiddenView.cs", "malformed"),
                f"got {check(boundary, unassigned={})!r}")

        missing_path_header = base / "missing-path-header"
        fixture(
            missing_path_header,
            [f"{SURFACES}/HiddenView.cs"],
            "### Core owns\n\n| Path | Why |\n|---|---|\n"
            f"| `{SURFACES}/**` | broad |\n\n"
            "### Design owns\n\n"
            f"| `{SURFACES}/HiddenView.cs` | missing Path header |\n")
        require("surface row without Path header is visible", has(
            check(missing_path_header, unassigned={}), f"{SURFACES}/HiddenView.cs", "malformed"),
            f"got {check(missing_path_header, unassigned={})!r}")

        no_delimiters = base / "no-delimiters"
        fixture(
            no_delimiters,
            [f"{SURFACES}/HiddenView.cs"],
            "### Core owns\n\n| Path | Why |\n|---|---|\n"
            f"| `{SURFACES}/**` | broad |\n\n"
            "### Design owns\n\n| Path | Why |\n|---|---|\n"
            f"`{SURFACES}/HiddenView.cs` missing every table delimiter\n")
        require("surface row without table delimiters is visible", has(
            check(no_delimiters, unassigned={}), f"{SURFACES}/HiddenView.cs", "malformed"),
            f"got {check(no_delimiters, unassigned={})!r}")

        unrelated_workbench = base / "unrelated-workbench"
        fixture(
            unrelated_workbench,
            [f"{SURFACES}/AnchorSurface.cs"],
            "### Core owns\n\n| Path | Why |\n|---|---|\n"
            f"| `{SURFACES}/AnchorSurface.cs` | surface |\n"
            f"| {SURFACES}/WorkbenchShell.cs | unrelated non-surface row |\n")
        require("unrelated Workbench filename stays outside jurisdiction",
                check(unrelated_workbench, unassigned={}) == [],
                f"got {check(unrelated_workbench, unassigned={})!r}")

        indented_end = base / "indented-section-end"
        target = indented_end / SURFACES / "HiddenView.cs"
        target.parent.mkdir(parents=True)
        target.write_text("// probe\n", encoding="utf-8")
        contract = indented_end / CONTRACT
        contract.parent.mkdir(parents=True)
        contract.write_text(
            "## 2. File ownership\n\n"
            "### Core owns\n\n| Path | Why |\n|---|---|\n"
            f"| `{SURFACES}/HiddenView.cs` | owner |\n\n"
            "  ## 3. Later\n\n"
            "### Design owns\n\n| Path | Why |\n|---|---|\n"
            f"| `{SURFACES}/HiddenView.cs` | outside section 2 |\n",
            encoding="utf-8")
        require("indented level-two heading ends section 2",
                check(indented_end, unassigned={}) == [],
                f"got {check(indented_end, unassigned={})!r}")

        malformed_cases = {
            "unquoted path": f"| {SURFACES}/BrokenView.cs | no code token |",
            "missing table delimiter": f"| `{SURFACES}/BrokenView.cs` | missing end",
            "traversal": f"| `{SURFACES}/../BrokenView.cs` | escape |",
            "backslash": r"| `src\AiDe.App\Workbench\BrokenView.cs` | not POSIX |",
            "unsupported recursive glob": f"| `{SURFACES}/**/BrokenView.cs` | unsupported |",
        }
        for index, (name, row) in enumerate(malformed_cases.items()):
            place = base / f"malformed-{index}"
            fixture(
                place,
                [f"{SURFACES}/AnchorSurface.cs", f"{SURFACES}/BrokenView.cs"],
                "### Core owns\n\n| Path | Why |\n|---|---|\n"
                f"| `{SURFACES}/AnchorSurface.cs` | anchor |\n{row}\n")
            require(f"malformed {name} is visible", has(
                check(place, unassigned={}), "BrokenView.cs", "malformed"),
                f"got {check(place, unassigned={})!r}")

        zero_bare = base / "zero-bare"
        fixture(
            zero_bare,
            [f"{SURFACES}/AnchorSurface.cs"],
            "### Core owns\n\n| Path | Why |\n|---|---|\n"
            f"| `{SURFACES}/AnchorSurface.cs` | anchor |\n"
            "| `MissingView.cs` | no candidate |\n")
        require("zero-match bare filename fails", has(
            check(zero_bare, unassigned={}), "MissingView.cs", "no discovered candidate"),
            f"got {check(zero_bare, unassigned={})!r}")

        exception = base / "exception"
        fixture(
            exception,
            [f"{SURFACES}/AnchorSurface.cs", f"{SURFACES}/PendingView.cs"],
            "### Core owns\n\n| Path | Why |\n|---|---|\n"
            f"| `{SURFACES}/AnchorSurface.cs` | anchor |\n")
        pending = f"{SURFACES}/PendingView.cs"
        valid_reason = "pending req-01TEST; remove when Ruling 999 assigns an owner"
        require("live canonical exception", check(exception, {pending: valid_reason}) == [],
                f"got {check(exception, {pending: valid_reason})!r}")
        for name, entries, expected in (
            ("blank exception reason", {pending: ""}, "nonblank reason"),
            ("exception without provenance", {pending: "remove when assigned"}, "request or ruling"),
            ("exception without retirement", {pending: "pending req-01TEST"}, "retirement"),
            ("noncanonical exception path", {"PendingView.cs": valid_reason}, "canonical"),
            ("stale missing exception", {f"{SURFACES}/GoneView.cs": valid_reason}, "no longer exists"),
        ):
            require(name, has(check(exception, entries), expected),
                    f"got {check(exception, entries)!r}")
        exception_contract = exception / CONTRACT
        exception_contract.write_text(
            exception_contract.read_text(encoding="utf-8").replace(
                "## 3. Later", f"| `{pending}` | now assigned |\n\n## 3. Later"),
            encoding="utf-8")
        require("newly assigned exception is stale", has(
            check(exception, {pending: valid_reason}), pending, "now assigns"),
            f"got {check(exception, {pending: valid_reason})!r}")

        deterministic = base / "deterministic"
        fixture(
            deterministic,
            [f"{SURFACES}/ZedView.cs", f"{SURFACES}/AlphaSurface.cs",
             f"{SURFACES}/Middle/ItemView.cs"],
            "### Core owns\n\n| Path | Why |\n|---|---|\n")
        first = check(deterministic, unassigned={})
        second = check(deterministic, unassigned={})
        require("generated diagnostics are deterministic", first == second == sorted(first),
                f"first={first!r}, second={second!r}")
        before = set(first)
        owned = deterministic / SURFACES / "OwnedSurface.cs"
        owned.write_text("// probe\n", encoding="utf-8")
        contract_text = (deterministic / CONTRACT).read_text(encoding="utf-8")
        (deterministic / CONTRACT).write_text(
            contract_text.replace("## 3. Later", f"| `{SURFACES}/OwnedSurface.cs` | owned |\n"
                                  "\n## 3. Later"), encoding="utf-8")
        require("adding an owned file does not erase existing findings",
                before.issubset(set(check(deterministic, unassigned={}))),
                f"before={before!r}, after={check(deterministic, unassigned={})!r}")

        missing_contract = base / "missing-contract"
        target = missing_contract / SURFACES / "OnlySurface.cs"
        target.parent.mkdir(parents=True)
        target.write_text("// probe\n", encoding="utf-8")
        require("missing ownership contract fails closed", has(
            check(missing_contract, unassigned={}), CONTRACT, "does not exist"),
            f"got {check(missing_contract, unassigned={})!r}")

        missing_section = base / "missing-section"
        fixture(missing_section, [f"{SURFACES}/OnlySurface.cs"], "### Core owns\n")
        (missing_section / CONTRACT).write_text("## 1. Other\n", encoding="utf-8")
        require("missing section 2 fails closed", has(
            check(missing_section, unassigned={}), "no '## 2. File ownership'"),
            f"got {check(missing_section, unassigned={})!r}")

        zero_population = base / "zero-population"
        fixture(zero_population, [], "### Core owns\n")
        require("zero population fails closed", has(
            check(zero_population, unassigned={}), "no surface files found"),
            f"got {check(zero_population, unassigned={})!r}")

        runner = globals().get("run")
        if runner is None:
            failures.append("temporary-filesystem CLI exit behavior: run() is missing")
        else:
            require("temporary-filesystem CLI failure exit", runner(recursion, {}) == 1,
                    "unowned fixture did not exit 1")
            require("temporary-filesystem CLI success exit", runner(grouped, {}) == 0,
                    "fully owned fixture did not exit 0")

        if not os.environ.get("SURFACE_GATE_MUTANT"):
            source = Path(__file__).read_text(encoding="utf-8")
            mutations = {
                "nonrecursive discovery": ('directory.rglob("*")', 'directory.glob("*")'),
                "basename identity alias": (
                    "f.relative_to(root).as_posix()", "f.name"),
                "non-Path cell contamination": (
                    "code_tokens = CODE_TOKEN.findall(path_cell)",
                    "code_tokens = CODE_TOKEN.findall(line)"),
                "ambiguous bare-name bypass": (
                    "if len(candidates) > 1:", "if False and len(candidates) > 1:"),
                "cross-owner conflict suppression": (
                    "if len(tables) > 1:", "if False and len(tables) > 1:"),
                "stale assigned exception retention": (
                    "elif assigned.get(declared_path):",
                    "elif False and assigned.get(declared_path):"),
                "heading-context reset suppression": (
                    "if MARKDOWN_HEADING.match(line.strip()):",
                    "if False and MARKDOWN_HEADING.match(line.strip()):"),
                "delimiter-free row suppression": (
                    'row_without_pipes = stripped.startswith("`") and _surface_relevant(stripped)',
                    "row_without_pipes = False"),
            }
            mutant_environment = os.environ.copy()
            mutant_environment["SURFACE_GATE_MUTANT"] = "1"
            for index, (name, (old, new)) in enumerate(mutations.items()):
                if old not in source:
                    failures.append(f"mutation oracle {name}: source target is absent")
                    continue
                mutant = base / f"mutant-{index}.py"
                mutant.write_text(source.replace(old, new, 1), encoding="utf-8")
                result = subprocess.run(
                    [sys.executable, str(mutant), "--self-test"],
                    cwd=root_for_self_test(),
                    capture_output=True,
                    text=True,
                    encoding="utf-8",
                    errors="replace",
                    env=mutant_environment,
                    check=False)
                require(f"mutation oracle {name}", result.returncode != 0,
                        "mutated gate still passed its self-test")

    if failures:
        print("verify-surface-ownership: SELF-TEST FAILED")
        for failure in failures:
            print(f"  - {failure}")
        return 1

    print("verify-surface-ownership: self-test OK — eight injected mutants plus recursive "
          "identities, §2 Path cells, historical non-Path tables, malformed headers, "
          "patterns, exceptions, deterministic diagnostics, and "
          "CLI exits are proven.")
    return 0


def root_for_self_test() -> Path:
    """Keep mutation subprocesses in the repository so repo_root() has a Git context."""
    return repo_root()


if __name__ == "__main__":
    sys.exit(main())
