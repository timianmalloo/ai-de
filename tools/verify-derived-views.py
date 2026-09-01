#!/usr/bin/env python3
"""The committed derived views are what their generators would produce right now.

WHAT HAPPENED. `docs/docs-index.js` and `docs/audit/audit-data.js` are generated from
`docs/**` frontmatter and from the two append-only JSONL logs. A rebase merges them like any
other text, and they are line-oriented JSON — so git resolves them into a file that is
syntactically valid, semantically wrong, and carries no conflict marker to give it away. Every
test passes, because nothing in the test suite reads a derived view.

That is DC-060, and it has happened three times in two days:

  * once leaving conflict MARKERS committed inside the register (caught by a grep, eventually);
  * once with no markers at all — git merged two generated files line-wise into something that
    parsed, and it surfaced only because somebody regenerated and saw a diff;
  * once as a stale `sourceSha256` after a three-commit rebase, where the regeneration ran
    between commit two and commit three and the third commit changed a document.

The response after the second was "always regenerate and confirm no diff after a rebase" — which
is a habit, not a control. A lesson recorded as prose is a memoir (CI6). This is the executable
form.

WHAT THIS CHECKS. Runs each generator, compares its output against what is committed, and
restores the committed bytes if they differ so a failing check never leaves the tree dirtier
than it found it.

The `generated` timestamp is EXCLUDED from the comparison. It changes on every run by
construction, so comparing it would make this fail always — and a control that fails always is
one people pass `--no-verify` to. Everything else, including every content hash, is compared
byte for byte.

Exit 0 when clean, 1 otherwise. Stdlib only.
"""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
from pathlib import Path

# Each derived view, and the command that produces it. Adding one is a line here.
VIEWS = [
    {
        "path": "docs/docs-index.js",
        "command": ["docs/ai-forward-pack/scripts/docs-graph.py", "derive"],
        "from": "the frontmatter of every artifact under docs/",
    },
    {
        "path": "docs/audit/audit-data.js",
        "command": ["docs/ai-forward-pack/scripts/audit-log.py", "render"],
        "from": "docs/audit/audit-log.jsonl and docs/audit/change-log.jsonl",
    },
]

# A timestamp stamped at generation time. It differs on every run by construction — comparing it
# would make this control fail always, which is how a control gets switched off.
VOLATILE = re.compile(rb'"generated":\s*"[^"]*"')


def repo_root() -> Path:
    return Path(subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        capture_output=True, text=True, check=True).stdout.strip())


def comparable(blob: bytes) -> bytes:
    """The view with its volatile fields flattened, and line endings normalised."""
    return VOLATILE.sub(b'"generated":"-"', blob).replace(b"\r\n", b"\n")


def regenerate(root: Path, view: dict) -> str | None:
    """Run the generator. Returns an error string, or None."""
    out = subprocess.run(
        [sys.executable, *view["command"]],
        capture_output=True, cwd=root, encoding="utf-8", errors="replace")

    if out.returncode != 0:
        return (f"{' '.join(view['command'])} exited {out.returncode} — the view cannot be "
                f"verified because its generator does not run: {out.stderr.strip()[:200]}")

    return None


def check(root: Path) -> list[str]:
    findings: list[str] = []

    for view in VIEWS:
        path = root / view["path"]

        if not path.exists():
            findings.append(f"{view['path']} is missing — it is derived from {view['from']}")
            continue

        committed = path.read_bytes()

        error = regenerate(root, view)

        if error:
            findings.append(error)
            continue

        produced = path.read_bytes()

        if comparable(committed) == comparable(produced):
            # Put the committed bytes back so a clean run leaves the tree exactly as it was —
            # only the volatile timestamp would differ, and a gate must not create a diff.
            path.write_bytes(committed)
            continue

        # Restore before reporting. A failing control that leaves the working tree rewritten
        # makes the next command's output a lie about what is committed.
        path.write_bytes(committed)

        findings.append(
            f"{view['path']} is not what its generator produces — it is derived from "
            f"{view['from']} and is stale or was merged by hand. This is the shape a rebase "
            f"leaves behind: valid JSON, no conflict marker, and wrong (DC-060). "
            f"Fix: run `python {view['command'][0]} {view['command'][1]}` and commit the result.")

    return findings


def self_test(root: Path) -> int:
    """The control must be observed FAILING, or it is not a control (CI6)."""
    view = VIEWS[0]
    path = root / view["path"]

    if not path.exists():
        print(f"verify-derived-views: SELF-TEST FAILED — {view['path']} is not here to plant in.")
        return 1

    original = path.read_bytes()

    try:
        # The shape a line-wise merge leaves: still valid, still unmarked, one value wrong.
        planted = original.replace(b'"rootId"', b'"rootIdX"', 1)

        if planted == original:
            print("verify-derived-views: SELF-TEST FAILED — nothing to plant; the fixture "
                  "no longer matches the file and would pass by changing nothing (DC-016).")
            return 1

        path.write_bytes(planted)
        findings = check(root)

        # And the volatile field alone must NOT fail, or the gate is red on every clean run.
        path.write_bytes(VOLATILE.sub(b'"generated": "1999-01-01T00:00:00Z"', original))
        timestamp_only = check(root)
    finally:
        path.write_bytes(original)

    for finding in findings:
        print(f"  planted -> {finding}")

    if not findings:
        print("verify-derived-views: SELF-TEST FAILED — a stale view was not detected.")
        return 1

    if timestamp_only:
        print("verify-derived-views: SELF-TEST FAILED — a difference in the generation timestamp "
              "alone was reported; this gate would be red on every clean run.")
        return 1

    print("verify-derived-views: self-test OK — a stale view fails, a changed timestamp does not.")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--self-test", action="store_true",
        help="prove the control fires: plant a stale view, expect failure")
    args = parser.parse_args()

    root = repo_root()

    if args.self_test:
        return self_test(root)

    findings = check(root)

    if findings:
        print("verify-derived-views: FAILED")
        for finding in findings:
            print(f"  - {finding}")
        return 1

    print(f"verify-derived-views: OK — {len(VIEWS)} derived view(s) match their generators.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
