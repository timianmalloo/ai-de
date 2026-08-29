#!/usr/bin/env python3
"""Parse every JavaScript block this repository embeds in another file (DC-023).

A page written inside a C# string, or inside an HTML template a script fills in, gets no compiler,
no analyzer and no test. On 2026-08-29 a stray quote in `CanvasPage.cs` broke the whole `<script>`
and **the Graph pane rendered nothing at all**. It was invisible to the build, invisible to 628
tests, and the one control that would have caught it was running a stale binary.

This is the missing check, and it runs in under a second.

**How it decides.** With Node present it uses `node --check`, which is the same parser the browser
uses — a real answer, with a line number. Without Node it falls back to a lexical scan that finds
unterminated string literals and unbalanced braces/parens: the class that actually bit us, and
honestly less than a parser. The fallback **says which mode it ran in**, because a gate that
silently degrades to a weaker check is worse than one that fails.

It reports syntax only. A script that parses can still be wrong, and no static check replaces the
canvas probe actually rendering the page.

Usage:
    python tools/verify-embedded-scripts.py            # fail on a syntax error
    python tools/verify-embedded-scripts.py --list     # show what it found and would check
"""

from __future__ import annotations

import re
import shutil
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

ROOT = Path(__file__).resolve().parent.parent

# Where pages live. C# holds the product's canvas; the templates are the browsable explorers, which
# fail exactly as silently — a dead script there means a docs page that renders an empty shell.
SEARCH = [
    (ROOT / "src", "*.cs"),
    (ROOT / "docs" / "ai-forward-pack" / "templates", "*.html"),
]

SCRIPT = re.compile(r"<script\b[^>]*>(.*?)</script>", re.S | re.I)
SRC_ONLY = re.compile(r"<script\b[^>]*\bsrc\s*=", re.I)

# HTML comments are stripped first. This gate's own first finding was a comment DESCRIBING a
# <script src> tag, reported as a dead script — a control whose first failure is its own false
# positive is worth having only if it is fixed rather than tuned around.
HTML_COMMENT = re.compile(r"<!--.*?-->", re.S)

# A C# verbatim string doubles its quotes: "" means a literal ". Left as-is the doubling reads as an
# empty string followed by a string, which is a syntax error the C# compiler would never have
# allowed — so it must be undone before the JavaScript is parsed, or every file fails.
CSHARP_DOUBLED_QUOTE = '""'


def blocks() -> list[tuple[Path, int, str]]:
    """Every inline script block, with the line its file starts it on."""
    found: list[tuple[Path, int, str]] = []

    for directory, pattern in SEARCH:
        if not directory.exists():
            continue

        for path in sorted(directory.rglob(pattern)):
            text = path.read_text(encoding="utf-8", errors="replace")

            # Blanked, not deleted, so reported line numbers still point at the real file.
            text = HTML_COMMENT.sub(
                lambda m: re.sub(r"[^\n]", " ", m.group(0)), text)
            if "<script" not in text.lower():
                continue

            for match in SCRIPT.finditer(text):
                opening = text[match.start():match.start() + match.group(0).index(">") + 1]
                if SRC_ONLY.search(opening):
                    # An external script is not embedded; there is nothing here to parse.
                    continue

                body = match.group(1)
                if path.suffix == ".cs":
                    body = body.replace(CSHARP_DOUBLED_QUOTE, '"')

                if body.strip():
                    found.append((path, text[:match.start(1)].count("\n") + 1, body))

    return found


def check_with_node(body: str) -> str | None:
    """None when it parses; the parser's message otherwise."""
    with tempfile.NamedTemporaryFile("w", suffix=".js", delete=False, encoding="utf-8") as handle:
        handle.write(body)
        temporary = handle.name

    try:
        result = subprocess.run(
            ["node", "--check", temporary],
            capture_output=True, text=True, timeout=30, check=False)

        if result.returncode == 0:
            return None

        # Node prints the offending line, a caret, then the error. The error line is what a reader
        # needs; the temporary path in it is noise.
        for line in (result.stderr or "").splitlines():
            if "SyntaxError" in line:
                return line.strip()

        return (result.stderr or "failed to parse").strip().splitlines()[-1]
    finally:
        Path(temporary).unlink(missing_ok=True)


def check_lexically(body: str) -> str | None:
    """
    The no-Node fallback: unterminated strings and unbalanced brackets.

    Deliberately narrow. It walks the source tracking string, template and comment state, which is
    enough to find the defect that prompted this and nothing like enough to be called a parser.
    """
    depth = {"{": 0, "(": 0, "[": 0}
    closers = {"}": "{", ")": "(", "]": "["}
    quote: str | None = None
    line = 1
    index = 0

    while index < len(body):
        c = body[index]

        if c == "\n":
            line += 1
            if quote in ("'", '"'):
                return f"line {line - 1}: unterminated string literal"

        if quote:
            if c == "\\":
                index += 2
                continue
            if c == quote:
                quote = None
            index += 1
            continue

        if c in "'\"`":
            quote = c
        elif c == "/" and index + 1 < len(body) and body[index + 1] == "/":
            while index < len(body) and body[index] != "\n":
                index += 1
            continue
        elif c == "/" and index + 1 < len(body) and body[index + 1] == "*":
            end = body.find("*/", index + 2)
            if end < 0:
                return f"line {line}: unterminated block comment"
            line += body.count("\n", index, end)
            index = end + 2
            continue
        elif c in depth:
            depth[c] += 1
        elif c in closers:
            depth[closers[c]] -= 1
            if depth[closers[c]] < 0:
                return f"line {line}: unbalanced '{c}'"

        index += 1

    if quote:
        return "unterminated string literal at end of script"

    for opener, count in depth.items():
        if count != 0:
            return f"unbalanced '{opener}': {count} left open"

    return None


def main() -> int:
    found = blocks()
    have_node = shutil.which("node") is not None
    mode = "node --check" if have_node else "lexical fallback (Node not found)"

    if not found:
        # Failing closed. Finding nothing to check means the search is wrong — there is at least one
        # embedded page in this repository, and a gate that passes over an empty set is DC-016.
        print("verify-embedded-scripts: FAILED — found no embedded script to check.")
        for directory, pattern in SEARCH:
            print(f"  searched: {directory.relative_to(ROOT)} ({pattern})")
        return 1

    if "--list" in sys.argv:
        print(f"verify-embedded-scripts: {len(found)} block(s), checked with {mode}:")
        for path, line, body in found:
            print(f"  {path.relative_to(ROOT)}:{line}  ({len(body.splitlines())} lines)")
        return 0

    failures: list[tuple[Path, int, str]] = []

    for path, line, body in found:
        problem = check_with_node(body) if have_node else check_lexically(body)
        if problem:
            failures.append((path, line, problem))

    if not failures:
        print(f"verify-embedded-scripts: OK — {len(found)} embedded script block(s) parse "
              f"({mode}).")
        return 0

    print(f"verify-embedded-scripts: FAILED — {len(failures)} embedded script(s) do not parse "
          f"({mode}).")
    print()

    for path, line, problem in failures:
        print(f"  {path.relative_to(ROOT)}: script starts at line {line}")
        print(f"    {problem}")
        print()

    print("  An embedded page gets no compiler and no analyzer. A script that does not parse does")
    print("  not run AT ALL — the pane renders nothing, and every test around it still passes.")
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
