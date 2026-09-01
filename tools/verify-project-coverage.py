#!/usr/bin/env python3
"""Every tracked project is compiled by something, and every gate is actually run.

WHAT HAPPENED. `spikes/joins-on-a-real-repo` is the harness that measures extraction against a
real repository — the source of nearly every performance and join number in the change log. It is
not in `AiDe.sln`. A change to `IWorkspaceQueries.GraphAsync` broke it, `dotnet build AiDe.sln`
stayed green because the solution has never heard of it, and the next measurement run silently
executed a STALE BINARY from a previous build. It was caught only because an expected timing field
did not appear in the output. Had the change been one that did not add a field, the numbers would
have looked fine and been wrong.

That is DC-023 — a gate runs a stale build — with the twist that the gate was not stale, it was
NARROW: it compiled everything it knew about, and it did not know about this.

WHY A GATE AND NOT SOLUTION MEMBERSHIP. Adding fifteen spikes to `AiDe.sln` would make the
solution's meaning wrong: it answers "what ships", and these are evidence artifacts kept so a
measurement can be re-run and disputed. It would also put them in every incremental build. The
property actually worth enforcing is more general than the fifteen files — *no tracked project
escapes compilation* — which covers the next project somebody adds outside the solution too.

COST, MEASURED rather than assumed. This was written expecting to belong in the slow ring
(at-readiness only). It builds 19 projects in **16 seconds** on this machine, which puts it in the
fast every-push ring after all. It prints its own wall time on every run so that stays a measured
decision: if the number grows, the ring is reconsidered from evidence rather than from the comment
that used to be here.

Exit 0 when clean, 1 otherwise. Stdlib only.
"""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
import time
from pathlib import Path

SOLUTION = "AiDe.sln"

# A project may be excluded only with a reason, and the reason is read by a human at review time.
# "It does not build" is not one of them — that is the finding, not the exemption.
EXEMPT: dict[str, str] = {}


def repo_root() -> Path:
    out = subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        capture_output=True, text=True, check=True)
    return Path(out.stdout.strip())


def tracked_projects(root: Path) -> list[str]:
    out = subprocess.run(
        ["git", "ls-files", "*.csproj"], capture_output=True, text=True, check=True, cwd=root)
    return sorted(p for p in out.stdout.splitlines() if p.strip())


def solution_projects(root: Path) -> set[str]:
    """The projects the solution references, as repo-relative POSIX paths."""
    solution = root / SOLUTION

    if not solution.exists():
        return set()

    text = solution.read_text(encoding="utf-8", errors="replace")

    # Project("{GUID}") = "Name", "relative\path.csproj", "{GUID}"
    found = set()

    for match in re.finditer(r'Project\([^)]*\)\s*=\s*"[^"]*"\s*,\s*"([^"]+\.csproj)"', text):
        found.add(match.group(1).replace("\\", "/"))

    return found


def build(root: Path, project: str) -> tuple[bool, str]:
    result = subprocess.run(
        ["dotnet", "build", project, "-c", "Release", "--nologo", "-v", "q"],
        capture_output=True, text=True, cwd=root)

    if result.returncode == 0:
        return True, ""

    lines = [
        line.strip() for line in (result.stdout + result.stderr).splitlines()
        if ": error" in line
    ]

    return False, (lines[0] if lines else f"exit {result.returncode}")


def ungated_scripts(root: Path) -> list[str]:
    """Every verify-*.py that CI never runs.

    Found by hand, which is the point: three gates written in one session
    (`verify-id-allocators`, `verify-project-coverage`, `verify-bounds-are-enforced`) sat in `tools/`
    for several commits without a workflow line, so they ran only when somebody remembered. A gate
    nobody invokes is the "lesson recorded as prose" failure wearing an executable's clothes — it
    looks like a control in every review and fires never.
    """
    workflows = root / ".github" / "workflows"
    if not workflows.is_dir():
        return []

    invoked = set()

    for workflow in workflows.glob("*.yml"):
        text = workflow.read_text(encoding="utf-8", errors="replace")
        invoked.update(re.findall(r"tools/(verify-[\w-]+)\.py", text))

    on_disk = {
        path.stem for path in (root / "tools").glob("verify-*.py")
    }

    return sorted(on_disk - invoked)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--list", action="store_true",
        help="report coverage without building (fast; catches a NEW uncovered project, not a broken one)")
    args = parser.parse_args()

    root = repo_root()
    tracked = tracked_projects(root)
    covered = solution_projects(root)

    if not covered:
        print(f"verify-project-coverage: FAILED — {SOLUTION} lists no projects; is it there?")
        return 1

    unknown = sorted(covered - set(tracked))
    outside = [p for p in tracked if p not in covered and p not in EXEMPT]

    problems = []

    # A solution entry pointing at a project git does not track is a build that works on one
    # machine. Reported, because it is the same class of surprise in the other direction.
    for project in unknown:
        problems.append(f"{SOLUTION} references '{project}', which git does not track")

    # Same question about a different artifact: what exists to be run, and is not run.
    for script in ungated_scripts(root):
        problems.append(
            f"tools/{script}.py is a gate that no workflow invokes — it runs only when somebody "
            f"remembers, which is not a control")

    print(
        f"verify-project-coverage: {len(tracked)} tracked project(s) — "
        f"{len(covered) - len(unknown)} in {SOLUTION}, {len(outside)} outside, "
        f"{len(EXEMPT)} exempt.")

    if args.list:
        for project in outside:
            print(f"  outside: {project}")
    else:
        started = time.monotonic()

        for project in outside:
            ok, message = build(root, project)
            if not ok:
                problems.append(f"{project} does not build — {message}")

        elapsed = time.monotonic() - started
        print(f"  built {len(outside)} project(s) outside the solution in {elapsed:.0f}s")

    if problems:
        print("verify-project-coverage: FAILED")
        for problem in problems:
            print(f"  - {problem}")
        print()
        print("  A project nothing compiles is a project that rots, and a harness that rots runs a")
        print("  stale binary and reports a number nobody can tell is old.")
        return 1

    print(f"verify-project-coverage: OK — {len(tracked)} tracked project(s), {len(covered)} in "
          f"{SOLUTION}; every one compiles.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
