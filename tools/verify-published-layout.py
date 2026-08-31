#!/usr/bin/env python3
"""The shell finds its daemon in the layout that SHIPS, not only the one developers run.

WHAT HAPPENED. `MainWindowViewModel.DaemonPath()` resolves `<BaseDirectory>/daemon/AiDe.Daemon.exe`.
A build-time target put it there, so every developer run and every test worked. `dotnet publish`
writes to a different directory and does not carry that target's output across, so the PUBLISHED
shell shipped without the daemon it launches — and opening any workspace failed with "This workspace
could not be opened", which reads like a problem with the workspace.

It survived because the layout that was tested was never the layout that shipped. That is the shape:
a control validated against the developer's arrangement of files says nothing about the user's.

WHAT THIS CHECKS. Publishes the shell to a scratch directory and asserts the daemon is at the path
the shell will actually compute — the relative path read from the SOURCE, so renaming the folder in
one place fails here rather than at a user's first click.

Exit 0 when clean, 1 otherwise. Stdlib only.
"""

from __future__ import annotations

import re
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

SHELL = "src/AiDe.App/AiDe.App.csproj"
SOURCE = "src/AiDe.App/ViewModels/MainWindowViewModel.cs"

# Path.Combine(AppContext.BaseDirectory, "daemon", "AiDe.Daemon.exe") — read, never restated (DC-021).
DAEMON_PATH = re.compile(
    r'DaemonPath\(\)\s*=>\s*Path\.Combine\(\s*AppContext\.BaseDirectory\s*,\s*(.+?)\)\s*;',
    re.DOTALL)


def repo_root() -> Path:
    return Path(subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        capture_output=True, text=True, check=True).stdout.strip())


def expected_segments(root: Path) -> list[str] | None:
    match = DAEMON_PATH.search((root / SOURCE).read_text(encoding="utf-8", errors="replace"))

    if match is None:
        return None

    return [part.strip().strip('"') for part in match.group(1).split(",") if part.strip()]


def main() -> int:
    root = repo_root()
    segments = expected_segments(root)

    if not segments:
        print("verify-published-layout: FAILED")
        print(f"  - could not read DaemonPath() from {SOURCE};")
        print("    it has been renamed or reshaped, and this gate can no longer name what to look for.")
        return 1

    out = Path(tempfile.mkdtemp(prefix="aide-publish-"))

    try:
        published = subprocess.run(
            ["dotnet", "publish", SHELL, "-c", "Release", "-o", str(out), "--nologo", "-v", "q"],
            capture_output=True, text=True, cwd=root)

        if published.returncode != 0:
            print("verify-published-layout: FAILED")
            print("  - the shell would not publish:")
            for line in (published.stdout + published.stderr).splitlines()[-8:]:
                print(f"      {line}")
            return 1

        target = out.joinpath(*segments)

        if not target.exists():
            print("verify-published-layout: FAILED")
            print(f"  - the published shell has no {'/'.join(segments)}.")
            print("    Opening any workspace fails with 'This workspace could not be opened',")
            print("    which reads like a problem with the workspace rather than with the build.")
            flat = out / segments[-1]
            if flat.exists():
                print(f"    ({segments[-1]} IS published, at the root — it is in the wrong place, not missing.)")
            return 1

        print(f"verify-published-layout: OK — the published shell finds {'/'.join(segments)}.")
        return 0
    finally:
        shutil.rmtree(out, ignore_errors=True)


if __name__ == "__main__":
    sys.exit(main())
