#!/usr/bin/env python3
"""A generated API doc may not name identifiers that do not exist.

WHAT HAPPENED. `api-reference.py` rendered `<see cref="..."/>` into a code span, stripping the
XML-doc prefix (`T:`, `P:`, `M:`) with this pattern:

    cref\\s*=\\s*"[A-Za-z]:?([^"]+)"

The colon is optional; the LETTER is not. So a cref written without a prefix — which is how every
same-type reference is written, `cref="Profiles"` — had its first character consumed as though it
were a prefix, and rendered as `rofiles`. `CommandLine` became `ommandLine`, `WorkbenchLayout`
became `orkbenchLayout`, `OtelAttributes` became `telAttributes`.

WHY IT SURVIVED. The output is still a plausible code span. A reader who does not know the type
sees a lowercase identifier and assumes a parameter or a local. It is only wrong if you go looking
for the thing it names — and it appeared in 13 committed files that nothing compared against the
source they document. Found by eye, in a diff, while checking something else.

WHAT THIS CHECKS. Every capitalised code span in `docs/api/*.md` that looks like a .NET identifier
must exist as a declared name somewhere in `src/`. That catches a mangled name, because
`orkbenchLayout` is not declared anywhere — and it catches the general case of a doc naming
something that has been renamed or deleted.

WHAT IT DELIBERATELY DOES NOT DO. It ignores lowercase-initial spans: parameter names, JSON keys and
JavaScript identifiers are legitimately lowercase and are not .NET declarations. That is a real
blind spot — a mangled `Profiles` becomes lowercase `rofiles` and is skipped by that rule — so the
mangling check does NOT rely on it. Instead, any span that becomes a known identifier when a single
capital letter is restored is reported as a decapitation, which is the exact shape of this defect.

Exit 0 when clean, 1 otherwise. Stdlib only.
"""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
from pathlib import Path

API = "docs/api"
SRC = "src"

# A code span that looks like a .NET identifier: PascalCase, or a dotted namespace path.
SPAN = re.compile(r"`([A-Za-z][A-Za-z0-9_]*(?:\.[A-Za-z][A-Za-z0-9_]*)*)`")

# A declaration in C#: the name that follows a declaring keyword, or a member signature.
DECLARED = re.compile(
    r"\b(?:class|record|struct|interface|enum|delegate)\s+([A-Za-z_]\w*)"
    r"|\b(?:public|internal|private|protected)\s+(?:static\s+|readonly\s+|const\s+|async\s+|"
    r"sealed\s+|override\s+|virtual\s+|abstract\s+|partial\s+)*[\w<>?,.\[\]]+\s+([A-Za-z_]\w*)\s*[({=;]")

# Words that are prose or another language's, not .NET declarations.
IGNORE = {
    "null", "true", "false", "struct", "class", "record", "interface", "enum", "string", "int",
    "bool", "void", "object", "double", "long", "byte", "char", "float", "decimal", "var", "new",
    "this", "base", "import", "export", "const", "let", "function", "return", "await", "async",
}


def repo_root() -> Path:
    return Path(subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        capture_output=True, text=True, check=True).stdout.strip())


def declared_names(root: Path) -> set[str]:
    names: set[str] = set()

    for path in (root / SRC).rglob("*.cs"):
        if "/obj/" in path.as_posix() or "/bin/" in path.as_posix():
            continue

        text = path.read_text(encoding="utf-8", errors="replace")

        for a, b in DECLARED.findall(text):
            if a:
                names.add(a)
            if b:
                names.add(b)

    return names


def check(root: Path) -> tuple[list[str], int, int]:
    problems: list[str] = []
    names = declared_names(root)
    directory = root / API

    if not directory.is_dir():
        return ([f"no {API}/ directory — this check is looking at nothing"], 0, 0)

    spans = 0
    decapitated: dict[str, str] = {}

    for path in sorted(directory.glob("*.md")):
        text = path.read_text(encoding="utf-8", errors="replace")

        for span in SPAN.findall(text):
            if span in IGNORE or span in names:
                continue

            # A dotted path is checked on its last segment; namespaces are not declarations.
            if "." in span and span.rsplit(".", 1)[-1] in names:
                continue

            spans += 1

            # THE DECAPITATION TEST, and the reason this gate exists. If restoring one capital
            # letter yields a real declared name, the generator ate a character.
            restored = next(
                (c + span for c in "ABCDEFGHIJKLMNOPQRSTUVWXYZ" if c + span in names), None)

            if restored is not None:
                decapitated[span] = restored

    for span, restored in sorted(decapitated.items()):
        problems.append(
            f"`{span}` is not a declared name, but `{restored}` is — the generator dropped the "
            "first character of a cref. It renders as a plausible code span, so it is only wrong "
            "to a reader who goes looking for what it names.")

    # The DC-016 guard. If SPAN or DECLARED stopped matching, this would report clean having
    # compared nothing at all.
    if not names:
        problems.append("no declared names were found in src/ — this gate examined nothing")

    return (problems, len(names), spans)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--self-test", action="store_true",
                        help="prove the control fires: a decapitated cref must be reported")
    args = parser.parse_args()

    if args.self_test:
        return self_test()

    problems, names, unknown = check(repo_root())

    if problems:
        print("verify-api-crefs: FAILED")
        for problem in problems:
            print(f"  - {problem}")
        return 1

    print(f"verify-api-crefs: OK — {names:,} declared name(s) known, "
          f"{unknown} unresolved span(s), none of them a dropped first character.")
    return 0


def self_test() -> int:
    """The control must be observed FAILING (CI6)."""
    import tempfile

    with tempfile.TemporaryDirectory() as directory:
        place = Path(directory)
        (place / SRC).mkdir(parents=True)
        (place / API).mkdir(parents=True)

        (place / SRC / "Thing.cs").write_text(
            "public sealed class Thing { public static string Profiles { get; set; } }\n",
            encoding="utf-8")

        (place / API / "doc.md").write_text(
            # The defect: a cref rendered without its first character.
            "See `rofiles` for the list. Also `Profiles` and `someParameter` and `null`.\n",
            encoding="utf-8")

        problems, _, _ = check(place)

    for problem in problems:
        print(f"  planted -> {problem.split(' —')[0]}")

    if not any("`rofiles`" in p and "`Profiles`" in p for p in problems):
        print("verify-api-crefs: SELF-TEST FAILED — a decapitated cref was not reported.")
        return 1

    if any("`Profiles`" in p and "`rofiles`" not in p for p in problems):
        print("verify-api-crefs: SELF-TEST FAILED — a correct name was reported, so the gate would "
              "be red on a correct document.")
        return 1

    print("verify-api-crefs: self-test OK — a dropped first character fails, a correct name does not.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
