#!/usr/bin/env python3
"""The shell finds its SIDECARS in the layout that ships, not only the one developers run.

WHAT HAPPENED. `MainWindowViewModel.DaemonPath()` resolves `<BaseDirectory>/daemon/AiDe.Daemon.exe`.
A build-time target put it there, so every developer run and every test worked. `dotnet publish`
writes to a different directory and does not carry that target's output across, so the PUBLISHED
shell shipped without the daemon it launches — and opening any workspace failed with "This workspace
could not be opened", which reads like a problem with the workspace.

It survived because the layout that was tested was never the layout that shipped. That is the shape:
a control validated against the developer's arrangement of files says nothing about the user's.

WHY IT IS A LIST. The MCP server arrived in exactly that shape — its own `AfterTargets` copy, its own
`<BaseDirectory>/<folder>/<exe>` resolution, its own way of being absent from a published build with
nothing failing. A gate written for one instance of a class does not cover the second, so this reads
EVERY sidecar the shell computes a path to. A new one is a new row here, and the row is the control.

The MCP server's absence is quieter than the daemon's: `McpConfigWriter` refuses to name a binary
that is not there, so a published shell simply never writes `.mcp.json` and every agent silently
falls back to the JSONL floor. Nothing errors. The enlightened path is just gone.

WHAT THIS CHECKS. Publishes the shell ONCE to a scratch directory and asserts each sidecar is at the
path the shell will actually compute — the relative path read from the SOURCE, so renaming a folder
in one place fails here rather than at a user's first click.

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

# Every sidecar the shell resolves under its own BaseDirectory: the method that computes it, the file
# that declares it, and what a user sees when it is not there. The path itself is READ from the
# source and never restated here (DC-021) — a gate that repeats the layout is a second authority on
# it, free to agree with the code while both are wrong.
SIDECARS = [
    ("DaemonPath",
     "src/AiDe.App/ViewModels/MainWindowViewModel.cs",
     "Opening any workspace fails with 'This workspace could not be opened',\n"
     "    which reads like a problem with the workspace rather than with the build."),
    ("McpServerPath",
     "src/AiDe.App/Workbench/WorkbenchShell.cs",
     "No .mcp.json is ever written, so every agent silently falls back to the JSONL\n"
     "    floor. Nothing errors and nothing is logged — the enlightened path is just gone."),
]


# Any method at all that resolves something under BaseDirectory. The registered list above says
# which ones this gate KNOWS about; this finds the ones it does not, which is the hole the daemon
# instance left open ("nothing detects a new one automatically") and the MCP server then fell into.
ANY_SIDECAR = re.compile(
    r'(\w+)\(\)\s*=>\s*Path\.Combine\(\s*AppContext\.BaseDirectory\s*,\s*([^;]+?)\)\s*;',
    re.DOTALL)

SHELL_SOURCES = "src/AiDe.App"


def path_expression(method: str) -> re.Pattern[str]:
    """`<method>() => Path.Combine(AppContext.BaseDirectory, "a", "b.exe");`"""
    return re.compile(
        method + r'\(\)\s*=>\s*Path\.Combine\(\s*AppContext\.BaseDirectory\s*,\s*(.+?)\)\s*;',
        re.DOTALL)


def repo_root() -> Path:
    return Path(subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        capture_output=True, text=True, check=True).stdout.strip())


def unregistered(root: Path) -> list[tuple[str, str]]:
    """Sidecars the shell resolves that this gate has never been told about."""
    known = {method for method, _, _ in SIDECARS}
    found = []

    for source in sorted((root / SHELL_SOURCES).rglob("*.cs")):
        if "\\obj\\" in str(source) or "\\bin\\" in str(source):
            continue
        text = source.read_text(encoding="utf-8", errors="replace")
        for match in ANY_SIDECAR.finditer(text):
            method = match.group(1)
            if method not in known:
                found.append((method, str(source.relative_to(root)).replace("\\", "/")))

    return found


def expected_segments(root: Path, method: str, source: str) -> list[str] | None:
    match = path_expression(method).search(
        (root / source).read_text(encoding="utf-8", errors="replace"))

    if match is None:
        return None

    return [part.strip().strip('"') for part in match.group(1).split(",") if part.strip()]


def main() -> int:
    root = repo_root()

    # Read every path BEFORE publishing. A method that has been renamed or reshaped is a gate that
    # can no longer name what to look for, and finding that out after a Release publish wastes the
    # expensive half of the run.
    # The hole the first instance of this class left open, closed: a sidecar this gate does not
    # know about fails HERE rather than reaching a user, so adding one is a decision rather than an
    # omission. It is the difference between a control that covers the class and one that covers
    # the instance somebody remembered.
    strangers = unregistered(root)
    if strangers:
        print("verify-published-layout: FAILED")
        for method, source in strangers:
            print(f"  - {method}() in {source} resolves a file under BaseDirectory and is not in SIDECARS.")
        print("    Add it there (with what a user sees when it is missing) so the published layout is")
        print("    checked for it too. This gate exists because that arrangement is never the tested one.")
        return 1

    wanted: list[tuple[str, list[str], str]] = []
    for method, source, consequence in SIDECARS:
        segments = expected_segments(root, method, source)
        if not segments:
            print("verify-published-layout: FAILED")
            print(f"  - could not read {method}() from {source};")
            print("    it has been renamed or reshaped, and this gate can no longer name what to look for.")
            return 1
        wanted.append((method, segments, consequence))

    out = Path(tempfile.mkdtemp(prefix="aide-publish-"))

    try:
        # Published once for all of them: the publish is the slow half, and every sidecar is a
        # question about the same output directory.
        published = subprocess.run(
            ["dotnet", "publish", SHELL, "-c", "Release", "-o", str(out), "--nologo", "-v", "q"],
            capture_output=True, text=True, cwd=root)

        if published.returncode != 0:
            print("verify-published-layout: FAILED")
            print("  - the shell would not publish:")
            for line in (published.stdout + published.stderr).splitlines()[-8:]:
                print(f"      {line}")
            return 1

        missing = []
        for method, segments, consequence in wanted:
            target = out.joinpath(*segments)
            if not target.exists():
                missing.append((method, segments, consequence))

        if missing:
            print("verify-published-layout: FAILED")
            for method, segments, consequence in missing:
                print(f"  - the published shell has no {'/'.join(segments)} ({method}).")
                print(f"    {consequence}")
                flat = out / segments[-1]
                if flat.exists():
                    print(f"    ({segments[-1]} IS published, at the root — it is in the wrong place, not missing.)")
            return 1

        found = ", ".join("/".join(segments) for _, segments, _ in wanted)
        print(f"verify-published-layout: OK — the published shell finds {found}.")
        return 0
    finally:
        shutil.rmtree(out, ignore_errors=True)


if __name__ == "__main__":
    sys.exit(main())
