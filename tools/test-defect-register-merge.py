#!/usr/bin/env python3
"""test-defect-register-merge.py - two branches each append one defect-class entry; the merge keeps both.

The register (docs/lessons/defect-classes.md) is append-only Markdown. With no merge rule in
.gitattributes it is resolved as `authored`, so every second join collides at end-of-file. This test
builds a throwaway repository carrying the real .gitattributes and the real register, appends one
entry on each of two branches, merges, and requires: exit 0, no conflict markers, both entries kept,
and verify-defect-register.py --fix-counts then the plain check both pass.

Exit 0 on pass, 1 on any failure.
"""
from __future__ import annotations

import re
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
REGISTER = Path("docs/lessons/defect-classes.md")
VERIFIER = Path("tools/verify-defect-register.py")


def git(cwd: Path, *args: str, check: bool = True) -> subprocess.CompletedProcess:
    return subprocess.run(
        ["git", "-c", "user.name=t", "-c", "user.email=t@t", "-c", "core.autocrlf=false", *args],
        cwd=cwd, capture_output=True, text=True, encoding="utf-8", errors="replace", check=check)


def entry(tag: str) -> str:
    return (f"\n### DC-NEW-{tag} - A test entry for the merge rule\n\n"
            f"- **Shape:** appended by branch {tag}.\n"
            "- **Status:** `uncontrolled` - a fixture entry\n")


def main() -> int:
    with tempfile.TemporaryDirectory() as tmp:
        work = Path(tmp)
        for rel in (REGISTER, VERIFIER, Path(".gitattributes")):
            (work / rel).parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(REPO / rel, work / rel)
        git(work, "init", "-q", "-b", "main")
        git(work, "add", "-A")
        git(work, "commit", "-q", "-m", "base")
        for tag in ("a", "b"):
            git(work, "checkout", "-q", "-b", tag, "main")
            with (work / REGISTER).open("a", encoding="utf-8", newline="\n") as fh:
                fh.write(entry(tag))
            git(work, "commit", "-q", "-am", f"append {tag}")
        git(work, "checkout", "-q", "a")
        merge = git(work, "merge", "--no-edit", "b", check=False)
        text = (work / REGISTER).read_text(encoding="utf-8")
        failures = []
        if merge.returncode != 0:
            failures.append("merge exited {}: {}".format(merge.returncode, merge.stdout.strip()[:200]))
        if re.search(r"^(<<<<<<< |>>>>>>> |=======$)", text, re.MULTILINE):
            failures.append("conflict markers in the merged register")
        for tag in ("a", "b"):
            if text.count(f"### DC-NEW-{tag} - ") != 1:
                failures.append(f"entry {tag} occurs {text.count(f'### DC-NEW-{tag} - ')} times, expected 1")
        if not failures:
            for args in (["--fix-counts"], []):
                run = subprocess.run([sys.executable, str(work / VERIFIER), *args], cwd=work,
                                     capture_output=True, text=True, encoding="utf-8", errors="replace")
                if run.returncode != 0:
                    failures.append("verify-defect-register {} exited {}: {}".format(
                        " ".join(args), run.returncode, (run.stdout + run.stderr).strip()[-300:]))
        if failures:
            print("test-defect-register-merge: FAIL")
            for f in failures:
                print("  - " + f)
            return 1
        print("test-defect-register-merge: OK - two appended entries merged, both kept, no markers, "
              "counts fix and check pass.")
        return 0


if __name__ == "__main__":
    sys.exit(main())
