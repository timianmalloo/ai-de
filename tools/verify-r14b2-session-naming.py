#!/usr/bin/env python3
"""R14 b2: new code in this slice uses "session" only for the user-facing container.

WHAT HAPPENED. Ruling 15/15a already found and fixed the collision this guards against:
`GovernedSessionSource` -> `GovernedLaneSource` and `GovernedSession` -> `GovernedEpisode`, because
`AgentPlane`'s pre-existing "session" vocabulary meant something else entirely (one open governed
episode) from the front door's "session" (Addendum A3's user-facing container: named, workspace-bound,
holding an ordered series of blocks and runs). Two things spelled the same way, in the same
vocabulary, meaning different things, is the confusion A3 exists to repair — and it is exactly the
shape a new symbol can reintroduce by accident the day after the rename lands.

The Watcher's OWN "session" vocabulary (`SessionReadiness`, `ITerminalSession`, `WatcherIdentity`'s
`SessionRecord`, ...) is real, predates A3, and migrates opportunistically per A3's own rule — it is
NOT this check's business and is out of scope for this node (`Watcher/`, `Dispatch/`, `Terminal/` are
frozen).

WHAT THIS CHECKS. Every newly ADDED type declaration (class/record/struct/interface/enum) whose name
contains "session", comparing the working tree against a base ref (the diff, not the whole repository
— existing "session" types predate this rule and are grandfathered by construction, exactly as
`verify-surface-ownership.py` grandfathers §2's existing gaps). A new "session"-named type must be
EITHER:
  * under a `Sessions/` directory (the container's own namespace — this node built
    `src/AiDe.Core/Sessions/`), OR
  * under one of the frozen directories (`Watcher/`, `Dispatch/`, `Terminal/` — not this node's to
    rename, and not this check's to police).

Anything else is a NEW "session" symbol outside the container that this check cannot tell apart from
the exact collision Ruling 15/15a fixed, so it fails.

WHAT THIS DELIBERATELY DOES NOT DO. It does not touch member names, local variables, or comments —
only type declarations, because those are the identifiers that get searched, referenced across files,
and confused. It does not re-litigate the pre-existing tree; a static whole-repo scan would fail on
`SessionRowPresenter`, `WatcherSessionsPaneViewModel`, `SessionIdentity` and others that exist today
for reasons this node did not judge and is not re-opening.

SCOPED TO `src/`, NOT `tests/`. Both Ruling 15/15a renames were production types
(`GovernedSessionSource`, `GovernedSession`) — the risk is a consumer elsewhere in the source tree
resolving the wrong "session". This repository's test classes are named in BDD prose
(`TheAcpSessionRunsInTheProvisionedWorktreeTests`, found live on this branch, added 2026-09-09,
outside this node and long before this check existed) describing a scenario, not exposing an API
surface anything resolves against — a different, established convention this check is not the place
to relitigate. Checked directly against this repository rather than assumed: run without a `tests/`
exclusion, this check's own first run flagged that file, which is how the scope decision above was
made (DC-116 — checked in this turn).

Exit 0 when clean, 1 otherwise. Stdlib only.
"""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
import tempfile
from pathlib import Path

FROZEN_DIRS = ("Watcher/", "Dispatch/", "Terminal/")
CONTAINER_MARKER = "Sessions/"

# Matches an added line (git diff -U0, a line starting with a single '+') declaring a type whose
# name contains "session", case-insensitive — "Session", "SESSION" and "session" all collide the
# same way in a codebase that reads identifiers, not casing.
ADDED_TYPE_DECL = re.compile(
    r'^\+(?!\+\+)\s*(?:public|internal|private|protected|sealed|abstract|static|partial|readonly|'
    r'record|\s)*\b(class|record|struct|interface|enum)\s+([A-Za-z_][A-Za-z0-9_]*)\b')

FILE_HEADER = re.compile(r'^\+\+\+ b/(.+)$')

EMPTY_TREE = "4b825dc642cb6eb9a060e54bf8d69288fbee4904"  # git's magic empty-tree hash


def repo_root() -> Path:
    return Path(subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        capture_output=True, text=True, check=True).stdout.strip())


def resolve_base(root: Path, base: str | None) -> str:
    if base:
        return base

    verify = subprocess.run(
        ["git", "rev-parse", "--verify", "-q", "origin/main"],
        capture_output=True, text=True, cwd=root)

    if verify.returncode == 0:
        origin_sha = verify.stdout.strip()
        head_sha = subprocess.run(
            ["git", "rev-parse", "HEAD"], capture_output=True, text=True, cwd=root).stdout.strip()
        return "HEAD~1" if origin_sha == head_sha else "origin/main"

    parent = subprocess.run(
        ["git", "rev-parse", "--verify", "-q", "HEAD~1"],
        capture_output=True, text=True, cwd=root)

    return "HEAD~1" if parent.returncode == 0 else EMPTY_TREE


def added_session_types(root: Path, base: str) -> list[tuple[str, str]]:
    """(path, type name) for every newly ADDED "session"-named type declaration since base."""
    diff = subprocess.run(
        ["git", "diff", "--no-color", "-U0", base, "HEAD", "--", "*.cs"],
        capture_output=True, cwd=root, encoding="utf-8", errors="replace")

    if diff.returncode != 0 or not diff.stdout:
        return []

    found: list[tuple[str, str]] = []
    current_path = ""

    for line in diff.stdout.splitlines():
        header = FILE_HEADER.match(line)
        if header:
            current_path = header.group(1)
            continue

        match = ADDED_TYPE_DECL.match(line)
        if match and "session" in match.group(2).lower():
            found.append((current_path, match.group(2)))

    return found


def check(root: Path, base: str) -> list[str]:
    problems: list[str] = []

    for path, name in added_session_types(root, base):
        normalized = path.replace("\\", "/")

        if not normalized.startswith("src/"):
            continue  # production API surface only — tests use an established BDD-prose naming
            # convention this check is not the place to relitigate; see the docstring.

        if any(f"/{frozen}" in f"/{normalized}" for frozen in FROZEN_DIRS):
            continue  # not this node's vocabulary to police (opportunistic migration, A3)

        if f"/{CONTAINER_MARKER}" in f"/{normalized}":
            continue  # the container's own namespace — exactly where "session" belongs

        problems.append(
            f'{path}: new type "{name}" uses "session" outside the container namespace '
            f'({CONTAINER_MARKER}) and outside the frozen Watcher/Dispatch/Terminal directories. '
            'R14 b2: new code uses "session" only for the user-facing container (Ruling 15/15a). '
            f'Either move it under a {CONTAINER_MARKER} directory or rename it.')

    return problems


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--base", default=None, help="ref to diff against (default: auto-detected)")
    parser.add_argument("--self-test", action="store_true", help="prove the control fires")
    args = parser.parse_args()

    if args.self_test:
        return self_test()

    root = repo_root()
    base = resolve_base(root, args.base)
    problems = check(root, base)

    if problems:
        print(f"verify-r14b2-session-naming: FAILED (base {base})")
        for problem in problems:
            print(f"  - {problem}")
        return 1

    print(f"verify-r14b2-session-naming: OK — no new out-of-container \"session\" symbol since {base}.")
    return 0


def _run_git(place: Path, *args: str) -> None:
    subprocess.run(["git", *args], cwd=place, capture_output=True, check=True)


def self_test() -> int:
    """The control must be observed FAILING, or it is not a control (CI6)."""
    with tempfile.TemporaryDirectory() as directory:
        place = Path(directory)

        _run_git(place, "init", "-q")
        _run_git(place, "config", "user.email", "selftest@example.invalid")
        _run_git(place, "config", "user.name", "R14b2 self-test")

        (place / "src" / "AiDe.Core" / "Watcher").mkdir(parents=True)
        (place / "src" / "AiDe.Core" / "Watcher" / "Placeholder.cs").write_text(
            "namespace AiDe.Core.Watcher;\npublic sealed class Placeholder { }\n", encoding="utf-8")
        _run_git(place, "add", "-A")
        _run_git(place, "commit", "-q", "-m", "base")

        (place / "src" / "AiDe.Core" / "Sessions").mkdir(parents=True)
        (place / "src" / "AiDe.Core" / "Sessions" / "SessionConfig.cs").write_text(
            "namespace AiDe.Core.Sessions;\n"
            "public sealed record SessionConfig(string SessionId);\n",
            encoding="utf-8")

        (place / "src" / "AiDe.Core" / "Watcher" / "MoreWatcherThing.cs").write_text(
            "namespace AiDe.Core.Watcher;\n"
            "internal sealed class WatcherSessionHelper { }\n",
            encoding="utf-8")

        (place / "src" / "AiDe.Core" / "AgentPlane").mkdir(parents=True)
        (place / "src" / "AiDe.Core" / "AgentPlane" / "RogueSessionThing.cs").write_text(
            "namespace AiDe.Core.AgentPlane;\n"
            "public sealed class RogueSessionThing { }\n",
            encoding="utf-8")

        (place / "tests" / "AiDe.Core.Tests" / "AgentPlane").mkdir(parents=True)
        (place / "tests" / "AiDe.Core.Tests" / "AgentPlane" / "TheSessionScenarioTests.cs").write_text(
            "namespace AiDe.Core.Tests.AgentPlane;\n"
            "public sealed class TheSessionScenarioTests { }\n",
            encoding="utf-8")

        _run_git(place, "add", "-A")
        _run_git(place, "commit", "-q", "-m", "slice")

        problems = check(place, "HEAD~1")

    for problem in problems:
        print(f"  planted -> {problem.splitlines()[0]}")

    if not any("RogueSessionThing" in p for p in problems):
        print("verify-r14b2-session-naming: SELF-TEST FAILED — an out-of-container new "
              "\"session\" type (RogueSessionThing, in AgentPlane/) was not reported.")
        return 1

    if any("SessionConfig" in p for p in problems):
        print("verify-r14b2-session-naming: SELF-TEST FAILED — a type under Sessions/ (the "
              "container's own namespace) was reported; the gate would be red on correct code.")
        return 1

    if any("WatcherSessionHelper" in p for p in problems):
        print("verify-r14b2-session-naming: SELF-TEST FAILED — a new type under the frozen "
              "Watcher/ directory was reported; that vocabulary migrates opportunistically and "
              "is not this node's to police.")
        return 1

    if any("TheSessionScenarioTests" in p for p in problems):
        print("verify-r14b2-session-naming: SELF-TEST FAILED — a test class was reported; "
              "tests/ is out of scope (BDD-prose naming, not production API surface).")
        return 1

    print("verify-r14b2-session-naming: self-test OK — an out-of-container new \"session\" type "
          "fails, a container type and a frozen-directory type do not.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
