#!/usr/bin/env python3
"""The Workbench prompt-draft sidecar must stay outside git (Privacy & Data Governance finding).

WHAT THIS GUARDS. `WorkbenchShell.BindPromptDrafts` (src/AiDe.App/Workbench/WorkbenchShell.cs)
writes `PromptDraftStore` (src/AiDe.App/Workbench/PromptDraftStore.cs) to
`<workspace-root>/.aide/prompt-drafts.json` — plaintext JSON, no expiry, no size bound, no
deletion path. Those properties are real and are deferred to the Data & Persistence Architect;
this gate only stops the file becoming reachable by `git add -A` in the first place. Before this
gate and its `.gitignore` line existed, no rule anywhere in the tree matched `.aide/` at all.

WHY A DEDICATED GATE AND NOT JUST THE LINE. A `.gitignore` line with no assertion is a belief, not
a control — `verify-gate-self-tests.py` already enforces (DC-104) that a check this repo relies on
must be able to prove it can fail. `.gitignore` has bitten this repo on exactly this shape before:
`.agents/` as a bare directory rule excluded the directory itself, so git never descended into it
and a later file-level negation could never re-fire (recorded at `.gitignore:553-558`). `.aide/`
carries no negation — nothing under it needs to travel with the repo — but "no negation needed" is
itself a claim this gate checks, not assumes.

WHAT IS CHECKED. `git check-ignore --quiet .aide/prompt-drafts.json` from the repo root, and the
same for a nested path, to confirm the ignore reaches into the directory and not just its own name.
Deliberately `--quiet`, never `-v`: this repo measured that `-v` prints the matching pattern (with
any negation) and still exits 0, which affirms an ignore fired without ever confirming an ignore
was NOT overridden further down the file — the inverted-answer trap `.gitignore:551` records.

Exit 0 when `.aide/` is ignored, 1 otherwise.
"""

from __future__ import annotations

import argparse
import subprocess
import sys
import tempfile
from pathlib import Path

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

# The paths the real repo must ignore. A bare file at the top of the sidecar, and one a directory
# deep, so a directory-only rule that fails to reach into subpaths is caught too.
TARGETS = (".aide/prompt-drafts.json", ".aide/nested/whatever.json")


def repo_root() -> Path:
    return Path(subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        capture_output=True, text=True, check=True).stdout.strip())


def is_ignored(cwd: Path, relative: str) -> bool:
    """True when git reports `relative` ignored under `cwd`. Never raises on a plain "not
    ignored" (exit 1) — only a git failure (anything else) is a genuine error."""
    result = subprocess.run(
        ["git", "check-ignore", "--quiet", relative],
        cwd=cwd, capture_output=True, text=True)

    if result.returncode not in (0, 1):
        raise RuntimeError(
            f"git check-ignore errored on {relative!r} (exit {result.returncode}): "
            f"{result.stderr.strip()}")

    return result.returncode == 0


def self_test() -> int:
    """Prove both directions in a throwaway repo, never the real one: a `.gitignore` naming
    `.aide/` ignores it, and a `.gitignore` that omits the line does not — so this gate is shown
    capable of failing before it is trusted to pass (DC-104)."""
    with tempfile.TemporaryDirectory(prefix="verify-aide-gitignore-selftest-") as raw:
        scratch = Path(raw)
        subprocess.run(["git", "init", "-q"], cwd=scratch, check=True)

        # DIRECTION 1: no rule at all — the state this repo was actually in before the fix.
        (scratch / ".gitignore").write_text("*.log\n", encoding="utf-8")
        if is_ignored(scratch, TARGETS[0]):
            print("self-test FAILED: .aide/ was reported ignored with no matching rule present",
                  file=sys.stderr)
            return 1

        # DIRECTION 2: the rule this fix adds.
        (scratch / ".gitignore").write_text("*.log\n.aide/\n", encoding="utf-8")
        for target in TARGETS:
            if not is_ignored(scratch, target):
                print(f"self-test FAILED: {target} was NOT reported ignored with `.aide/` present",
                      file=sys.stderr)
                return 1

    print("self-test OK — no rule leaves .aide/ unignored, the `.aide/` rule ignores it and "
          "everything nested under it")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--self-test", action="store_true",
                        help="prove this gate fires on a missing rule and passes on the real one, "
                             "in a throwaway repo")
    args = parser.parse_args()

    if args.self_test:
        return self_test()

    root = repo_root()
    missing = [t for t in TARGETS if not is_ignored(root, t)]

    if missing:
        print("verify-aide-gitignore: FAILED")
        for target in missing:
            print(f"  - {target} is NOT ignored by .gitignore")
        print()
        print("  The Workbench prompt-draft sidecar (.aide/) holds plaintext prompt text with no "
              "expiry. Add a `.aide/` rule to .gitignore before this file can reach a commit.")
        return 1

    print(f"verify-aide-gitignore: OK — {', '.join(TARGETS)} ignored.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
