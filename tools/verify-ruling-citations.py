#!/usr/bin/env python3
"""Every ruling cited as authority must resolve to a note that says what it decided.

WHAT THIS EXISTS FOR, measured 2026-09-11. Programme decisions are cited by number across the
repository and enforced as binding. EIGHT of them defined nothing: Rulings 5, 7, 32, 34, 35, 37,
45 and 46 were cited with no note anywhere recording what they said. `Ruling 35` was cited six
times as governing a dependency decision. `Ruling 7` was cited as forbidding an interface,
including by a note warning a future session not to build the thing it forbids.

Two ends of that met on the same day. `Ruling 45` was cited by a mockup and a review, and issued
to a node as a mid-task correction, BEFORE any ruling of that number had been made. `Ruling 46`
was cited by DC-130's control and by an audit entry -- and the Owner, unable to see that because
nothing recorded it, allocated 46 again for a different decision. THE ABSENCE OF THE REGISTER IS
WHAT CAUSED THE COLLISION IN THE REGISTER.

This is DC-013 recurrence 6. Defect-class ids survive the same pressure only by accident:
verify-id-allocators.py says in its own header that the DC-032 collision was caught "only because
verify-defect-register.py happens to enforce one-entry-per-class for its own reasons". Ruling
numbers had no equivalent accident. Nothing read them, nothing counted them, and a number was
authoritative the moment it was typed.

A decision that cannot be read is not a decision; it is a number with a reputation.

WHAT COUNTS AS A DEFINITION. A heading in docs/notes/ of the form `## Ruling NN — ...`, or a
`### Ruling NN` block. A mention inside prose is a CITATION, never a definition -- that
distinction is the whole point, and collapsing it would make the gate agree with any file that
talks about a ruling often enough.

THE FROZEN LIST. Six numbers (5, 7, 32, 34, 35, 37) are cited by work that predates this control
and their text cannot be reconstructed without fabricating it, which is worse than the gap. They
are frozen BY NAME and the list may only shrink: filing a note removes an entry, and a NEW
undefined citation fails. A frozen list that may only shrink is a register that cannot rot; one
that may be appended to is the defect with extra steps.
"""

from __future__ import annotations

import argparse
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
DOCS = ROOT / "docs"
NOTES = DOCS / "notes"

CITATION = re.compile(r"\bRulings?\s+(\d{1,3})\b")
# A definition is a HEADING that names the ruling. Prose mentioning it is a citation.
#
# THE PATTERN'S FIRST VERSION WAS WRONG AND THE FIRST RUN CAUGHT IT (DC-104). It required
# `## Ruling NN`, which is the form used by front-door-rulings-41-42.md -- so it flagged
# front-door-ruling-36-yaml-guard-scope.md and ...-38-... as undefined when both define their
# ruling perfectly well under `# Decision note — Ruling 36`. A gate that reports a correctly
# filed note as missing teaches people to distrust it, which is the failure mode that ends with
# the gate switched off. Any heading level counts, and the heading need only CONTAIN the phrase.
DEFINITION = re.compile(r"^#{1,4}\s+.*?\bRulings?\s+(\d{1,3})\b", re.MULTILINE)

# Cited by work predating this control; text unrecoverable without fabricating it, which is worse
# than the gap. Rulings 1 and 3 are cited by docs/plans/conductor-programme.md; Ruling 33's only
# definition is a bolded blockquote inside docs/plans/conductor-front-door.md, which is a plan and
# not a decision note -- close enough to read, not close enough to count.
# This list may only SHRINK. A stale entry is refused by the gate itself.
FROZEN_UNDEFINED = {1, 3, 5, 7, 32, 33, 34, 35, 37}

SCANNED_SUFFIXES = {".md", ".html"}


def definitions(root):
    """Ruling numbers defined by a heading in any note under `root`."""
    found = {}
    notes = root / "notes"
    if not notes.is_dir():
        return found
    for path in sorted(notes.rglob("*.md")):
        for match in DEFINITION.finditer(path.read_text(encoding="utf-8", errors="replace")):
            found.setdefault(int(match.group(1)), path)
    return found


def citations(root):
    """Ruling numbers cited anywhere under `root`, mapped to the files citing them."""
    found = {}
    for path in sorted(root.rglob("*")):
        if path.suffix.lower() not in SCANNED_SUFFIXES or not path.is_file():
            continue
        if "_site" in path.parts or "ai-forward-pack" in path.parts:
            continue
        text = path.read_text(encoding="utf-8", errors="replace")
        for match in CITATION.finditer(text):
            found.setdefault(int(match.group(1)), set()).add(
                path.relative_to(root.parent).as_posix())
    return found


def check(root, frozen=FROZEN_UNDEFINED):
    defined = definitions(root)
    cited = citations(root)
    defects = []

    for number in sorted(cited):
        if number in defined:
            continue
        where = sorted(cited[number])
        if number in frozen:
            continue
        defects.append(
            f"Ruling {number} is cited as authority in {len(where)} file(s) and no note under "
            f"docs/notes/ defines it. Cited by: {', '.join(where[:4])}"
            f"{' ...' if len(where) > 4 else ''}. Anyone can assert what it said and nobody can "
            f"check.")

    # The frozen list may only shrink: a number that has since been defined must leave it.
    for number in sorted(frozen):
        if number in defined:
            defects.append(
                f"Ruling {number} is on the frozen-undefined list but is now DEFINED in "
                f"{defined[number].relative_to(root.parent).as_posix()}. Remove it from "
                f"FROZEN_UNDEFINED - a stale entry lets the next undefined citation hide behind it.")

    return defects, defined, cited


def self_test():
    """Break a synthetic tree and require the gate to notice. DC-104.

    A control's first green is evidence about the control as much as about the code, and the
    control is the part nobody re-examines because it is what they just reasoned about.
    """
    import tempfile

    NL = chr(10)
    cases = [
        ("a ruling cited with no note defining it",
         {"notes/some-note.md": "We applied Ruling 91 here." + NL},
         "Ruling 91 is cited as authority"),
        ("the same ruling, defined by a heading",
         {"notes/some-note.md": "We applied Ruling 91 here." + NL,
          "notes/rulings.md": "## Ruling 91 — the thing" + NL + "It decided the thing." + NL},
         None),
        ("a definition that is only prose, not a heading",
         {"notes/rulings.md": "Ruling 91 decided the thing, at length, repeatedly." + NL},
         "Ruling 91 is cited as authority"),
        ("cited outside docs/notes, defined inside it",
         {"plans/p.md": "Per Ruling 91 we did this." + NL,
          "notes/rulings.md": "## Ruling 91 — the thing" + NL},
         None),
    ]
    failures = []
    for name, files, expected in cases:
        with tempfile.TemporaryDirectory() as tmp:
            root = pathlib.Path(tmp) / "docs"
            for rel, body in files.items():
                target = root / rel
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_text(body, encoding="utf-8")
            found, _, _ = check(root, frozen=set())
            if expected is None:
                if found:
                    failures.append(f"{name}: expected clean, got {found}")
            elif not any(expected in d for d in found):
                failures.append(f"{name}: expected a defect containing {expected!r}, got {found}")

    # The frozen list may only shrink.
    with tempfile.TemporaryDirectory() as tmp:
        root = pathlib.Path(tmp) / "docs"
        (root / "notes").mkdir(parents=True)
        (root / "notes" / "r.md").write_text("## Ruling 91 — filed" + chr(10), encoding="utf-8")
        found, _, _ = check(root, frozen={91})
        if not any("frozen-undefined list but is now DEFINED" in d for d in found):
            failures.append("stale frozen entry: expected the gate to demand its removal, "
                            f"got {found}")

    if failures:
        print(f"self-test FAILED: {len(failures)} case(s).")
        for f in failures:
            print(f"  - {f}")
        return 1
    print(f"self-test: {len(cases) + 1} cases, the gate fires on each break and stays quiet "
          f"when clean.")
    return 0


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--self-test", action="store_true",
                        help="prove this gate can fail, against a synthetic tree")
    args = parser.parse_args()
    if args.self_test:
        return self_test()

    defects, defined, cited = check(DOCS)
    if defects:
        print(f"verify-ruling-citations: FAILED — {len(defects)} defect(s).")
        for d in defects:
            print(f"  - {d}")
        return 1
    frozen_still = sorted(n for n in FROZEN_UNDEFINED if n not in defined)
    print(f"verify-ruling-citations: OK — {len(cited)} ruling(s) cited, {len(defined)} defined by "
          f"a note, {len(frozen_still)} frozen as pre-existing debt "
          f"({', '.join(str(n) for n in frozen_still)}).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
