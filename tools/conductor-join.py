#!/usr/bin/env python3
"""conductor-join.py — the join, as a script: every step gated by its exit code, none by a shell line.

The control for DC-113's fourth recurrence (2026-09-13, the CV-5.4 join): the conductor resolved a
register conflict, piped the conflict-marker gate through `| tail -1`, read the gate's remedy text as
a pass, and committed a merge carrying `<<<<<<<` (DC-136's shape) — caught only because the runner
was re-run bare before the push. Three earlier recurrences had the same cause: a hand-typed line at
the join whose status was the formatter's, not the gate's. This script is the join line. It has no
pipes; each step is a subprocess whose return code decides whether the next runs.

Usage (from the PRIMARY checkout, on main):
  python tools/conductor-join.py <branch> --title "<merge title>" --audit-shortname <name> \
      --audit-summary "<text>" --audit-goal "<text>" --audit-done-when "<text>" \
      [--artifact <path> ...] [--docs-only] [--no-push] [--no-release]

Steps, in order, stop on the first red:
  1. merge --no-ff <branch>            a conflict stops here with the file list (resolve by hand,
                                       then re-run with --continue)
  2. verify-no-conflict-markers        always, before anything else reads the tree
  3. verify-defect-register --fix-counts (counts only; ids are the conductor's, allocated by hand)
  4. verify-test-run --update          whole, then both Core halves — skipped with --docs-only
  5. audit-log append                  the join's entry - tier T1, fan-out 0, its own marker's duration, recount_seconds (F-22)
  6. regenerate-derived                the derived views after the audit entry
  7. git add -A && git commit          the join commit (the pre-commit boundary runs)
  8. run-verify-gates                  every gate, one status, on the committed tree
  9. git push origin main              only if 8 passed; --no-push skips
 10. dotnet build App -c Release       the operator's build; --no-release skips; prints ProductVersion

Exit 0 on a complete join; the failing step's number otherwise. Stdlib only.
"""
from __future__ import annotations

import argparse
import os
import subprocess
import time
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PY = sys.executable
TRAILER = ("\n\nCo-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>\n"
           "Claude-Session: https://claude.ai/code/session_01Pc51aWLB1FKK2b8AqUPAun\n")


def run(step: int, what: str, command: list[str], env: dict | None = None, allow: tuple[int, ...] = (0,)) -> subprocess.CompletedProcess:
    print(f"\n== step {step}: {what}\n   $ {' '.join(command)}", flush=True)
    completed = subprocess.run(command, cwd=ROOT, env=env, text=True, encoding="utf-8", errors="replace",
                               capture_output=True)
    tail = (completed.stdout + completed.stderr).strip().splitlines()
    for line in tail[-8:]:
        print(f"   | {line[:200]}")
    if completed.returncode not in allow:
        print(f"\nconductor-join: step {step} ({what}) exited {completed.returncode} — the join stops here.")
        sys.exit(step)
    return completed


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("branch", nargs="?", help="the branch to merge (omit with --continue)")
    parser.add_argument("--title", required=True, help="the merge commit's title (the trailer is appended)")
    parser.add_argument("--audit-shortname", required=True)
    parser.add_argument("--audit-summary", required=True)
    parser.add_argument("--audit-goal", required=True)
    parser.add_argument("--audit-done-when", required=True)
    parser.add_argument("--artifact", action="append", default=[], help="docs/proof paths for the audit entry")
    parser.add_argument("--docs-only", action="store_true", help="skip the recount (no test or product change)")
    parser.add_argument("--continue", dest="cont", action="store_true", help="the merge was resolved by hand; start at step 2")
    parser.add_argument("--no-push", action="store_true")
    parser.add_argument("--no-release", action="store_true")
    args = parser.parse_args(argv)

    env = dict(os.environ)
    env.setdefault("PYTHONIOENCODING", "utf-8")
    env.setdefault("MSBUILDDISABLENODEREUSE", "1")
    env.setdefault("AGENT_SESSION", "claude-conductor-addendum-c")
    env.setdefault("AGENT_NAME", "claude-conductor")

    branch_now = subprocess.run(["git", "branch", "--show-current"], cwd=ROOT, capture_output=True, text=True).stdout.strip()
    if branch_now != "main":
        print(f"conductor-join: run from the primary checkout on main (this is '{branch_now}')")
        return 2

    if not args.cont:
        if not args.branch:
            parser.error("a branch is required unless --continue")
        merge = subprocess.run(["git", "merge", "--no-ff", args.branch, "-m", args.title + TRAILER],
                               cwd=ROOT, capture_output=True, text=True, encoding="utf-8", errors="replace")
        print(f"\n== step 1: merge --no-ff {args.branch}")
        for line in (merge.stdout + merge.stderr).strip().splitlines()[-6:]:
            print(f"   | {line[:200]}")
        if merge.returncode != 0:
            conflicts = subprocess.run(["git", "diff", "--name-only", "--diff-filter=U"], cwd=ROOT,
                                       capture_output=True, text=True).stdout.strip().splitlines()
            print("\nconductor-join: the merge has conflicts — resolve by hand, `git add` them, `git commit --no-edit`, "
                  "then re-run with --continue:")
            for c in conflicts:
                print(f"   - {c}")
            return 1

    # The join's own marker (F-22 of the session profile sp-0002: fourteen join entries carried no
    # duration, tier or fan-out, so the profiler could not cost a join). Set here so the closing
    # entry's duration_seconds is measured from this instant; one marker measures one join (AL4a).
    run(2, "audit marker", [PY, "docs/ai-forward-pack/scripts/audit-log.py", "start", "--session", env["AGENT_SESSION"]], env)
    recount_started = time.monotonic()

    run(2, "no conflict markers", [PY, "tools/verify-no-conflict-markers.py"], env)
    run(3, "register counts", [PY, "tools/verify-defect-register.py", "--fix-counts"], env)
    run(3, "register sequence", [PY, "tools/verify-defect-register.py"], env)

    if not args.docs_only:
        run(4, "recount — whole", [PY, "tools/verify-test-run.py", "--update"], env)
        run(4, "recount — Core portable", [PY, "tools/verify-test-run.py", "--update", "--only", "AiDe.Core.Tests",
                                            "--filter", "Platform!=Windows", "--key", "AiDe.Core.Tests.portable"], env)
        run(4, "recount — Core Windows", [PY, "tools/verify-test-run.py", "--update", "--only", "AiDe.Core.Tests",
                                           "--filter", "Platform=Windows", "--key", "AiDe.Core.Tests.nonportable"], env)
        # `--update` re-baselines the counts even when a test FAILED (its job is the count); the
        # check form exits non-zero on a failed test — so a red suite stops the join here, before
        # the audit entry and the commit (the F5 join, 2026-09-14: one order-dependent red reached
        # step 8 and left a join commit unpushed).
        run(4, "recount — outcome", [PY, "tools/verify-test-run.py", "--no-run"], env)
    else:
        print("\n== step 4: recount skipped (--docs-only)")
    recount_seconds = int(time.monotonic() - recount_started)

    audit = [PY, "docs/ai-forward-pack/scripts/audit-log.py", "append",
             "--shortname", args.audit_shortname, "--session", env["AGENT_SESSION"],
             "--skill", "execute-with-coordination", "--kind", "skill", "--tier", "T1", "--fan-out", "0",
             "--prompt", f"keep going (the join of {args.branch or 'the resolved merge'})",
             "--summary", f"{args.audit_summary} recount_seconds={recount_seconds} (docs_only={args.docs_only}).",
             "--goal", args.audit_goal, "--done-when", args.audit_done_when,
             "--signal-verification-path", "true", "--signal-verification-executed", "true",
             "--signal-acceptance-met", "true"]
    for a in args.artifact:
        audit += ["--artifact", a]
    run(5, "audit entry", audit, env)

    run(6, "regenerate derived views", [PY, "tools/regenerate-derived.py"], env)

    run(7, "stage", ["git", "add", "-A"], env)
    run(7, "commit", ["git", "commit", "-q", "-m", f"chore(join): {args.audit_shortname} — the join's audit entry, derived views, floors" + TRAILER], env)

    run(8, "every verify gate", [PY, "tools/run-verify-gates.py"], env)

    if args.no_push:
        print("\n== step 9: push skipped (--no-push)")
    else:
        run(9, "push main", ["git", "push", "origin", "main"], env)

    sha = subprocess.run(["git", "rev-parse", "--short", "HEAD"], cwd=ROOT, capture_output=True, text=True).stdout.strip()
    if args.no_release:
        print(f"\n== step 10: Release build skipped (--no-release); main at {sha}")
    else:
        run(10, "Release build", ["dotnet", "build", "src/AiDe.App/AiDe.App.csproj", "-c", "Release", "-nologo", "-v", "q"], env)
        dll = ROOT / "src/AiDe.App/bin/Release/net10.0-windows/AiDe.App.dll"
        version = subprocess.run(["powershell", "-NoProfile", "-Command", f"(Get-Item '{dll}').VersionInfo.ProductVersion"],
                                 capture_output=True, text=True).stdout.strip()
        print(f"\nconductor-join: complete — main {sha}; Release ProductVersion {version}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
