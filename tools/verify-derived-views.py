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
    {
        # THE PUBLISHED DOCUMENTATION, added 2026-09-02. These two had NO gate at all, which is why
        # a session could go a whole day never running build-doc-viewer.py and nothing said so —
        # found when a single entry point regenerated them and produced a diff its author did not
        # expect.
        #
        # They are ONE view because one generator writes both: checking them separately would
        # regenerate the pair, restore one, and leave the other rewritten in the working tree.
        #
        # And the reason they were ungated is worth keeping: `documented_sha` differs on every run,
        # so a naive comparison always fails, so nobody added them — and the absence then looked
        # like a scope decision rather than a gap. The volatile-field mechanism below is exactly the
        # answer; it simply had not been pointed at this.
        "paths": ["docs/_meta.json", "docs/_site/index.html"],
        "command": ["tools/build-doc-viewer.py"],
        "from": "the doc bundle: the API reference, the diagrams and the docs graph",
    },
    {
        # A DIRECTORY of 17 files, added 2026-09-02 after it went stale unnoticed. The API reference
        # is derived from the `///` comments in src/, so ANY change to a doc comment - or adding a
        # public type - makes every committed page here a claim about a source that has moved.
        #
        # It was already stale when this entry was written: the committed pages said 76 types where
        # the source had 79, from a merge minutes earlier. Nothing reported it, because the two
        # views this gate knew about were both single files and this one was neither of them.
        "glob": "docs/api/*.md",
        "command": ["tools/api-reference.py"],
        "from": "the /// comments on every public type and member in src/",
    },
]

# A timestamp stamped at generation time. It differs on every run by construction — comparing it
# would make this control fail always, which is how a control gets switched off.
# Stamped at generation time and different on every run BY CONSTRUCTION — comparing them would make
# this control fail always, which is how a control gets switched off.
#
# `documented_sha` names the commit the docs were generated FROM, so it can only ever be the parent
# of the commit that carries it: it is never equal to its own commit and never will be. That is a
# correct record and a permanently-changing field, which is precisely what this pattern is for.
VOLATILE = re.compile(rb'"(?:generated|documented_sha)":\s*"[^"]*"')


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
        # A view is one file or a directory of them. The set is captured BEFORE regenerating, so a
        # generator that stops emitting a page is reported as a difference rather than ignored.
        if "glob" in view:
            paths = sorted(root.glob(view["glob"]))
            label = view["glob"]
        elif "paths" in view:
            # One generator, several files. They travel together or the restore leaves a sibling
            # rewritten in the working tree.
            paths = [root / p for p in view["paths"]]
            label = ", ".join(view["paths"])
        else:
            paths = [root / view["path"]]
            label = view["path"]

        if not paths or not all(p.exists() for p in paths):
            findings.append(f"{label} is missing — it is derived from {view['from']}")
            continue

        committed = {p: p.read_bytes() for p in paths}

        error = regenerate(root, view)

        if error:
            findings.append(error)
            continue

        stale = sorted(
            p.relative_to(root).as_posix() for p in paths
            if not p.exists() or comparable(committed[p]) != comparable(p.read_bytes()))

        # Restore before reporting, and before returning clean. A control that leaves the working
        # tree rewritten makes the next command's output a lie about what is committed.
        for path, blob in committed.items():
            path.write_bytes(blob)

        if not stale:
            continue

        fix = " ".join(view["command"])
        findings.append(
            f"{len(stale)} of {len(paths)} file(s) under {label} are not what the generator "
            f"produces — derived from {view['from']}, so they are stale or were merged by hand. "
            f"This is the shape a rebase leaves behind: no conflict marker, and wrong (DC-060). "
            f"Stale: {', '.join(stale[:4])}{' …' if len(stale) > 4 else ''}. "
            f"Fix: run `python {fix}` and commit the result.")

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
        path.write_bytes(original)

        # documented_sha is the second volatile field, and the reason the published docs were
        # UNGATED until 2026-09-02: it names the commit generated FROM, so it can only ever be the
        # parent of the commit carrying it. A naive comparison fails on every clean run, so nobody
        # added those views — and the absence then read as a scope decision rather than a gap.
        # Pinned here so the pattern cannot narrow back and re-create the reason.
        meta = root / "docs" / "_meta.json"
        sha_only: list[str] = []

        if meta.exists():
            before = meta.read_bytes()
            try:
                meta.write_bytes(re.sub(
                    rb'"documented_sha": "[0-9a-f]+"',
                    b'"documented_sha": "' + b"0" * 40 + b'"',
                    before))
                sha_only = check(root)
            finally:
                meta.write_bytes(before)
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

    if sha_only:
        print("verify-derived-views: SELF-TEST FAILED — a difference in documented_sha alone was "
              "reported. That field is the parent commit by construction, so this gate would be red "
              "on every clean run — which is exactly why the published docs went ungated.")
        return 1

    print("verify-derived-views: self-test OK — a stale view fails; a changed timestamp and a "
          "changed documented_sha do not.")
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
