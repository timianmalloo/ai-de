#!/usr/bin/env python3
"""Every control a source comment CLAIMS must resolve to something that exists.

The control for defect class DC-095. A doc comment that says a property is enforced and names the
test enforcing it reads as the strongest kind of evidence — a named, locatable control. Nothing
checks it: C# compilers do not resolve identifiers inside `<c>` tags, and a `<see cref>` pointing at
a test class in another assembly does not resolve either. So the citation can name a class that has
never existed, and the only reader who finds out is one who goes looking — the reader who needed the
guarantee least.

Two instances in one evening, the second committed in the same change that registered the class:

  SqliteWatcherObservationStore  "SchemaMatchesAfterMigrationTests asserts exactly that"
                                 — never existed; the real control was
                                   DaydreamPersistenceTests.AFreshDatabaseAndAMigratedOneHaveTheSameSchema
  WatcherIdentity                "TheWorkspaceKeyIsTheRepositoryNotTheCheckout is the control"
                                 — the class is TheWorkspaceKeyIsTheRepositoryTests

The second is why this exists rather than the reading rule the register first proposed. A rule
violated within the hour by the person who wrote it is not a rule, it is a memoir (CI6).

WHY THE TRIGGER IS THE SENTENCE AND NOT THE NAME. The first version of this gate keyed on the
identifier — anything ending in `Tests`, `Gate` or `.py`. It could not have caught either instance,
because in one of them the missing `Tests` suffix WAS the defect: the cited name resolves to nothing
precisely because it is not spelled like the class it meant. A detector keyed on the shape of a
correct name is blind to a name whose shape is what went wrong. So the trigger is claim language —
asserts, pins, proves, guards, enforces, is the control — which is the thing that makes a comment a
checkable promise rather than prose.

WHAT COUNTS AS RESOLVED: a type or method declared anywhere under src/ or tests/, or a file on disk
for a tool path. Deliberately generous — the failure being caught is "names nothing at all", not
"names the wrong kind of thing".
"""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
from pathlib import Path

SEARCHED = ("src", "tests")

# `<c>Name</c>`, `<c>Type.Member</c>`, and `<see cref="Name"/>` — the three forms used in this repo.
CITATION = re.compile(
    r'<c>([A-Za-z_][\w.]*)</c>'
    r'|<see(?:also)?\s+cref\s*=\s*"(?:[A-Za-z]:)?([^"]+)"\s*/?>')

# A comment that says something is enforced, and then names it, is making a checkable promise.
CLAIMS_ENFORCEMENT = re.compile(
    r'\b(?:is the control|the control is|asserts?\b|asserted by|pins?\b|pinned by'
    r'|proves?\b|proven by|guards?\b|guarded by|enforces?\b|enforced by'
    r'|verifies\b|verified by|is tested by|exercised by)',
    re.IGNORECASE)

# Doc comments wrap, so the verb and the name are routinely on different lines.
CONTEXT_LINES = 2

# A citation must at least look like a declared thing: PascalCase, or a tool path.
PLAUSIBLE = re.compile(r'^(?:[A-Z]\w*(?:\.\w+)*|[\w./-]+\.py)$')

# Identifiers that appear in CODE, as opposed to only in prose. Deliberately not a declaration
# parser: the failure being caught is "this name exists nowhere but in the comment claiming it", and
# a token scan answers exactly that with no false positives from a return type a regex did not model.
# The first version parsed declarations and reported `SchemaSql` and `WorkbenchShell.ResolveGitFacts`
# as fabricated, because it only recognised `void` and `Task` methods — a gate whose failures are
# mostly its own blind spots gets switched off, taking the real check with it.
COMMENT = re.compile(r'^\s*(?:///|//)')
TOKEN = re.compile(r'\b[A-Za-z_]\w*\b')

# Framework names that appear inside enforcement sentences without being controls.
#
# THIS LIST IS THE GATE'S WEAK POINT, said plainly. A framework type named descriptively near an
# enforcement verb - "guards the retemplate ... with the XamlWriter artifacts removed" - is not a
# claim that XamlWriter is the control, but nothing in the text distinguishes the two, and the token
# scan cannot see a type this repository never calls. Each entry is a false positive that was
# checked by hand and found benign.
#
# The failure mode to watch is a list that grows until someone silences a REAL finding by appending
# to it. So: add a name here only after confirming it is a framework type this repository does not
# declare, and never to make a run pass.
IGNORE = {
    "Assert", "Directory", "File", "Path", "SearchOption", "StringComparer", "StringComparison",
    "TimeProvider", "Task", "Exception", "InvalidOperationException", "ArgumentException",
    "Guid", "DateTimeOffset", "SQLite", "NTFS", "JSON", "UTF", "CI", "OK",
    "XamlWriter",
}


def repo_root() -> Path:
    return Path(subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        capture_output=True, text=True, check=True).stdout.strip())


def code_identifiers(root: Path) -> set[str]:
    """Every identifier appearing in non-comment code under src/ and tests/."""
    names: set[str] = set()

    for area in SEARCHED:
        for path in (root / area).rglob("*.cs"):
            if "/obj/" in path.as_posix() or "/bin/" in path.as_posix():
                continue

            for line in path.read_text(encoding="utf-8", errors="replace").splitlines():
                if COMMENT.match(line):
                    continue
                names.update(TOKEN.findall(line))

    return names


def resolves(citation: str, names: set[str], root: Path) -> bool:
    if citation.endswith(".py"):
        name = Path(citation).name
        return (root / citation).is_file() or any(
            p.name == name for p in (root / "tools").rglob("*.py"))

    # Every segment of a dotted citation must exist: Class.Method needs both, so a real class
    # carrying a method name nobody wrote is still caught.
    return all(segment in names for segment in citation.split(".") if segment)


def check(root: Path, files: list[Path] | None = None) -> tuple[list[str], int, int]:
    problems: list[str] = []
    names = code_identifiers(root)

    if not names:
        return (["no code identifiers were found — this gate examined nothing"], 0, 0)

    if files is None:
        files = [
            p for area in SEARCHED for p in (root / area).rglob("*.cs")
            if "/obj/" not in p.as_posix() and "/bin/" not in p.as_posix()
        ]

    cited = 0

    for path in sorted(files):
        text = path.read_text(encoding="utf-8", errors="replace")
        lines = text.splitlines()

        for line_number, line in enumerate(lines, start=1):
            index = line_number - 1
            context = "\n".join(lines[max(0, index - CONTEXT_LINES):index + CONTEXT_LINES + 1])

            if not CLAIMS_ENFORCEMENT.search(context):
                continue

            for a, b in CITATION.findall(line):
                citation = (a or b).strip()

                if citation in IGNORE or not PLAUSIBLE.match(citation):
                    continue

                cited += 1

                if not resolves(citation, names, root):
                    try:
                        shown = path.relative_to(root).as_posix()
                    except ValueError:
                        shown = path.as_posix()

                    problems.append(
                        f"{shown}:{line_number} claims enforcement and cites `{citation}`, which "
                        "resolves to nothing. A comment naming a control by a name that does not "
                        "exist reads as a guarantee and is an unverified assertion (DC-095).")

    # DC-016: if the patterns stopped matching, this would report clean having compared nothing.
    # This repository has cited controls; zero means the gate broke, not that the code did.
    if cited == 0:
        problems.append(
            "no cited controls were found at all — the citation or claim pattern has stopped "
            "matching and this gate is examining nothing")

    return (problems, len(names), cited)


def self_test(root: Path) -> int:
    """Prove the control fires — on a citation shaped like the real defect, not a tidier one."""
    bad = root / "docs" / "ai-forward-pack" / "_selftest_cited_controls.cs"
    bad.parent.mkdir(parents=True, exist_ok=True)

    # Deliberately NOT spelled like a test class. The instance this gate exists for was a citation
    # whose wrong shape was the defect, so a self-test using a well-formed name would prove the
    # weaker gate that has already been shown not to work.
    bad.write_text(
        "/// <summary><c>ANameNothingResolves</c> is the control.</summary>\n"
        "public sealed class SelfTestSubject { }\n",
        encoding="utf-8")

    try:
        problems, _, cited = check(root, files=[bad])
    finally:
        bad.unlink(missing_ok=True)

    if cited != 1:
        print(f"self-test FAILED: expected 1 cited control, found {cited}", file=sys.stderr)
        return 1

    if not any("ANameNothingResolves" in problem for problem in problems):
        print("self-test FAILED: the gate did not report the unresolvable citation", file=sys.stderr)
        return 1

    print("self-test OK — a citation claiming enforcement that resolves to nothing is reported")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--self-test", action="store_true",
                        help="prove the control fires on a claim that resolves to nothing")
    parser.add_argument("--files", nargs="*", default=None,
                        help="check only these files (used to replay a historical defect)")
    args = parser.parse_args()

    root = repo_root()

    if args.self_test:
        return self_test(root)

    files = [Path(f) for f in args.files] if args.files else None
    problems, names, cited = check(root, files=files)

    if problems:
        print("verify-cited-controls: FAILED")
        for problem in problems:
            print(f"  - {problem}")
        return 1

    print(f"verify-cited-controls: OK — {cited} claimed control(s) across {names} code identifiers; "
          "every one resolves.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
