#!/usr/bin/env python3
r"""A separator-terminated containment check must use the platform's own case rule, named once.

WHAT HAPPENED, THREE TIMES. `x.StartsWith(root + Path.DirectorySeparatorChar, ...)` is this
repository's containment boundary: "is this path inside that root". The separator is appended so
that `C:\repo-other` is not admitted as being inside `C:\repo`. The bug is always the SECOND
argument: a hardcoded `StringComparison.OrdinalIgnoreCase`, which is right on Windows and wrong
everywhere else. On POSIX, `/repo/Secrets` and `/repo/secrets` are two different directories, and
folding the case admits a path the rest of the system would never write.

It was fixed at INV-0005, again in `RepositoryIdentity.ToFileSystemPath`, and again in
`RepositoryCorrection` / `ProofPackVerifier` -- each time at one call site. `WatcherIdentity.cs`
states the rule in prose ("THIS IS NOT A FILESYSTEM PATH") and nothing enforced it, so the next
site was written the same way. This is the executable form: a lesson recorded as prose is a
memoir (CI6).

A `FileSystemPath` TYPE distinction was considered and rejected: both defects it was proposed for
operate on strings that genuinely ARE filesystem paths, so the type would not have caught either.
The bug is the comparison rule, not the type -- so the control is aimed at the comparison.

=====================================================================================
THE SCAN'S SCOPE, STATED SO IT CANNOT BE NARROWER THAN ITS OWN SENTENCE
=====================================================================================

ROOT        `src/`, resolved from `git rev-parse --show-toplevel` so the gate works from any
            subdirectory and in any worktree. `tests/` is NOT scanned (see WHAT THIS DOES NOT
            SEE), and neither is anything else in the repository.

RECURSION   Every `*.cs` file beneath `src/`, at any depth, except paths containing a `bin` or
            `obj` segment (build output, not source).

TOKENS      A finding needs BOTH halves:

            (1) A call spelled `.StartsWith(<first>, <second>)` -- exactly two arguments -- where
                <first> is SEPARATOR-TERMINATED, meaning either
                  * <first> itself appends a separator: `+` followed by one of
                    `Path.DirectorySeparatorChar`, `Path.AltDirectorySeparatorChar`,
                    `'\\'`, `'/'`, `"\\"`, `"/"`; or
                  * <first> is a bare identifier declared EARLIER IN THE SAME FILE by a
                    `var`/`string` declaration whose initializer appends one of those tokens.
                    (`ProjectionService` and `KnowledgeExtractor` both build the terminated root
                    into a local one statement above the comparison, so an inline-only matcher
                    would have seen one of the four real sites and reported the other three clean
                    -- DC-006 in the shape that matters here.)

            (2) <second> is a HARDCODED literal: `StringComparison.<Anything>` or
                `StringComparer.<Anything>`. Anything else -- an identifier, a member access -- is
                accepted, PROVIDED its name is in APPROVED_COMPARISONS below.

            Comments and string literals are blanked before matching (line, block, regular,
            verbatim @"...", and raw triple-quoted forms), so prose ABOUT `OrdinalIgnoreCase`
            never trips the gate and a `//` inside a string never starts a comment.

ALLOWLIST   Two named constants, and no other way to be exempt:

            APPROVED_COMPARISONS -- the comparison expressions sanctioned at a containment
            boundary. Each entry must RESOLVE: the gate reads `src/` for its declaration and
            fails if the declaration is missing or does not consult `OperatingSystem.IsWindows()`.
            A name that resolves to nothing reads as the strongest evidence available and checks
            nothing (DC-095), so the approval is verified rather than declared.

            ALLOWED_SITES -- per-site exemptions, keyed `<path>:<receiver identifier>`, each
            carrying the reason it is right. Empty today. It exists so that a future legitimate
            case is added by an entry that states its reason, rather than by widening the matcher
            or deleting the gate.

CORPUS      The scan asserts it read a non-empty corpus: if `src/` yields no `.StartsWith(` call
            at all, the matcher is broken and the gate fails instead of reporting "clean"
            (DC-006).

=====================================================================================
WHAT THIS DELIBERATELY DOES NOT SEE
=====================================================================================

* `tests/`. Deliberate: a test that hardcodes `OrdinalIgnoreCase` is often ASSERTING the Windows
  answer on purpose, which is the established idiom here (`[Trait("Platform","Windows")]`), and a
  gate whose false positives are the correct code is a gate that gets switched off (DC-104).
* Containment expressed any other way -- `IndexOf`, `Contains`, a regex, `Path.GetRelativePath`
  followed by a `..` test. Only the `StartsWith` shape, which is the one that recurred.
* The separator built across a method call or another file. Indirection is followed exactly one
  hop, within one file, through a `var`/`string` declaration.
* WHETHER the approved helper is used CORRECTLY -- the gate checks the name at the call site and
  the definition's use of `OperatingSystem.IsWindows()`. That the rule itself is right is pinned
  by a test, not by this gate.

Widening to any path-named receiver (`gitDir`, `relative`, `full`, `marker`) was measured at 23
sites, some of whose `Ordinal` comparisons are correct -- that needs a waiver mechanism and is not
this gate. The narrow version ships first and ratchets.

Exit 0 when clean, 1 otherwise. Stdlib only.
"""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
import tempfile
from pathlib import Path

SCAN_ROOT = "src"
EXCLUDED_SEGMENTS = ("bin", "obj")

# The comparison expressions sanctioned at a containment boundary. Every entry is RESOLVED against
# `src/` -- see resolve_approved(). Add a name here only together with its declaration.
APPROVED_COMPARISONS = {
    "PathComparison.ForThisFileSystem":
        "The one platform-conditional path rule (src/AiDe.Core/PathComparison.cs): "
        "OrdinalIgnoreCase on Windows, Ordinal everywhere else.",
}

# Per-site exemptions. Empty today, and that is the point: a legitimate future case is added here
# with its reason, rather than by widening the matcher or switching the gate off.
#
# Key: "<repo-relative path>:<the receiver identifier at the call site>".
ALLOWED_SITES: dict[str, str] = {}

SEPARATOR_TOKENS = (
    "Path.DirectorySeparatorChar",
    "Path.AltDirectorySeparatorChar",
    r"'\\'",
    "'/'",
    r'"\\"',
    '"/"',
)

# `+` followed by a separator token: the append that makes a prefix test a containment test.
SEPARATOR_APPEND = re.compile(
    r"\+\s*(?:" + "|".join(re.escape(token) for token in SEPARATOR_TOKENS) + r")")

# A hardcoded comparison. `StringComparer` is matched too: it is the same mistake reached through
# the other type, and refusing it here costs nothing.
HARDCODED_COMPARISON = re.compile(r"^(?:StringComparison|StringComparer)\.[A-Za-z0-9_]+$")

# `var name =` / `string name =` -- the one-hop indirection two of the real sites use.
LOCAL_DECLARATION = re.compile(r"\b(?:var|string)\s+([A-Za-z_][A-Za-z0-9_]*)\s*=")

STARTS_WITH = re.compile(r"\.StartsWith\s*\(")

IDENTIFIER = re.compile(r"^[A-Za-z_][A-Za-z0-9_]*$")

# The identifier immediately before `.StartsWith(`, used to key an ALLOWED_SITES entry.
RECEIVER = re.compile(r"([A-Za-z_][A-Za-z0-9_]*)\s*$")

WINDOWS_PROBE = "OperatingSystem.IsWindows()"

NEWLINE = chr(10)


def repo_root() -> Path:
    """Resolved from git, so the gate works from any subdirectory and in any worktree."""
    return Path(subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        capture_output=True, text=True, check=True).stdout.strip())


def blank_comments_and_strings(text: str) -> str:
    r"""Replace comment and string-literal bodies with spaces, PRESERVING every offset.

    Offsets are preserved so a finding still reports the right line, and newlines are kept so line
    counting works. Handles line, block, regular, verbatim @"" and raw triple-quoted forms,
    because `src/` contains raw-string literals and a scanner that mis-handles one runs off the
    rails for the remainder of the file.
    """
    out = list(text)
    i, n = 0, len(text)

    def blank(start: int, stop: int) -> None:
        for k in range(start, min(stop, n)):
            if out[k] != NEWLINE:
                out[k] = " "

    while i < n:
        two = text[i:i + 2]

        if two == "//":
            end = text.find(NEWLINE, i)
            end = n if end < 0 else end
            blank(i, end)
            i = end
            continue

        if two == "/*":
            end = text.find("*/", i + 2)
            end = n if end < 0 else end + 2
            blank(i, end)
            i = end
            continue

        if text.startswith('"""', i):
            fence = 0
            while i + fence < n and text[i + fence] == '"':
                fence += 1
            quotes = '"' * fence
            end = text.find(quotes, i + fence)
            end = n if end < 0 else end + fence
            blank(i, end)
            i = end
            continue

        if two == '@"':
            j = i + 2
            while j < n:
                if text[j] == '"':
                    if text[j:j + 2] == '""':
                        j += 2
                        continue
                    j += 1
                    break
                j += 1
            blank(i, j)
            i = j
            continue

        if text[i] in ('"', "'"):
            quote = text[i]
            j = i + 1
            while j < n:
                if text[j] == "\\":
                    j += 2
                    continue
                if text[j] == quote:
                    j += 1
                    break
                if text[j] == NEWLINE:
                    break
                j += 1
            blank(i, j)
            i = j
            continue

        i += 1

    return "".join(out)


def split_arguments(text: str, open_paren: int) -> list[str] | None:
    """Top-level arguments of the call whose `(` is at open_paren."""
    depth = 0
    args: list[str] = []
    start = open_paren + 1

    for i in range(open_paren, len(text)):
        char = text[i]

        if char in "([{":
            depth += 1
        elif char in ")]}":
            depth -= 1
            if depth == 0:
                args.append(text[start:i])
                return [a.strip() for a in args]
        elif char == "," and depth == 1:
            args.append(text[start:i])
            start = i + 1

    return None


def separator_terminated_locals(text: str) -> set[str]:
    """Identifiers declared in this file from an initializer that appends a separator."""
    found: set[str] = set()

    for match in LOCAL_DECLARATION.finditer(text):
        end = text.find(";", match.end())
        if end < 0:
            continue
        if SEPARATOR_APPEND.search(text[match.end():end]):
            found.add(match.group(1))

    return found


def source_files(root: Path) -> list[Path]:
    return sorted(
        path for path in (root / SCAN_ROOT).rglob("*.cs")
        if not any(part in EXCLUDED_SEGMENTS for part in path.relative_to(root).parts))


def scan_file(root: Path, path: Path) -> tuple[list[str], int]:
    """(problems, number of StartsWith calls seen) for one file."""
    raw = path.read_text(encoding="utf-8", errors="replace")
    text = blank_comments_and_strings(raw)
    terminated = separator_terminated_locals(text)
    relative = path.relative_to(root).as_posix()

    problems: list[str] = []
    seen = 0

    for call in STARTS_WITH.finditer(text):
        seen += 1
        arguments = split_arguments(text, call.end() - 1)

        if arguments is None or len(arguments) != 2:
            continue

        first, second = arguments
        line = text.count(NEWLINE, 0, call.start()) + 1

        is_containment = bool(SEPARATOR_APPEND.search(first)) or (
            IDENTIFIER.match(first) is not None and first in terminated)

        if not is_containment:
            continue

        if not HARDCODED_COMPARISON.match(second):
            if second in APPROVED_COMPARISONS:
                continue
            problems.append(
                f"{relative}:{line}: containment check passes `{second}`, which is not in "
                f"APPROVED_COMPARISONS. Use one of {sorted(APPROVED_COMPARISONS)}, or add "
                f"`{second}` there with the reason it is the platform's own case rule.")
            continue

        receiver = RECEIVER.search(text[:call.start()])
        key = f"{relative}:{receiver.group(1) if receiver else '?'}"

        if key in ALLOWED_SITES:
            continue

        problems.append(
            f"{relative}:{line}: separator-terminated containment check hardcodes `{second}`. On "
            "POSIX this admits a genuinely different path -- `/repo/Secrets` and `/repo/secrets` "
            f"are two directories. Use {sorted(APPROVED_COMPARISONS)[0]}, or add `{key}` to "
            "ALLOWED_SITES with the reason a fixed comparison is correct here.")

    return (problems, seen)


def resolve_approved(root: Path) -> list[str]:
    """Every approved name must resolve to a declaration that consults the platform (DC-095)."""
    problems: list[str] = []
    bodies = [path.read_text(encoding="utf-8", errors="replace") for path in source_files(root)]

    for name in sorted(APPROVED_COMPARISONS):
        member = name.rsplit(".", 1)[-1]
        declaration = re.compile(
            r"\b(?:StringComparison|StringComparer)\s+" + re.escape(member) + r"\b")
        hits = [text for text in bodies if declaration.search(text)]

        if not hits:
            problems.append(
                f"APPROVED_COMPARISONS names `{name}`, but no declaration of `{member}` exists "
                f"under {SCAN_ROOT}/. An approval that resolves to nothing approves nothing.")
            continue

        if not any(WINDOWS_PROBE in text for text in hits):
            problems.append(
                f"APPROVED_COMPARISONS names `{name}`, but its declaration never consults "
                f"`{WINDOWS_PROBE}`. It is approved for being platform-conditional; it is not.")

    return problems


def check(root: Path) -> list[str]:
    problems: list[str] = []
    total_calls = 0

    for path in source_files(root):
        found, seen = scan_file(root, path)
        problems.extend(found)
        total_calls += seen

    if total_calls == 0:
        return [f"no `.StartsWith(` call was found anywhere under {SCAN_ROOT}/ -- this scan read "
                "nothing, and a clean report over an empty corpus is not a pass (DC-006)."]

    return problems + resolve_approved(root)


def main() -> int:
    parser = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--self-test", action="store_true", help="prove the control fires")
    args = parser.parse_args()

    if args.self_test:
        return self_test()

    problems = check(repo_root())

    if problems:
        print("verify-containment-comparisons: FAILED")
        for problem in problems:
            print(f"  - {problem}")
        return 1

    print("verify-containment-comparisons: OK -- every separator-terminated containment check "
          f"uses {sorted(APPROVED_COMPARISONS)[0]}.")
    return 0


HELPER = """namespace AiDe.Core;

internal static class PathComparison
{
    internal static StringComparison ForThisFileSystem => OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;
}
"""

NOT_CONDITIONAL = """namespace AiDe.Core;

internal static class PathComparison
{
    internal static StringComparison ForThisFileSystem => StringComparison.OrdinalIgnoreCase;
}
"""

CLEAN = '''namespace AiDe.Core.Extraction;

internal static class Clean
{
    // Correct: the approved helper at a containment boundary.
    internal static bool Inline(string root, string candidate) =>
        candidate.StartsWith(root + Path.DirectorySeparatorChar, PathComparison.ForThisFileSystem);

    // Correct: a hardcoded comparison where there is NO separator - not a containment check.
    internal static bool NotContainment(string id) =>
        id.StartsWith("table:", StringComparison.Ordinal);

    // The false positive that matters most: prose ABOUT the wrong answer, and a string holding it.
    // resolved.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
    internal static string Prose =>
        "resolved.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)";
}
'''

PLANTED_INLINE = """namespace AiDe.Core.Extraction;

internal static class PlantedInline
{
    internal static bool Escapes(string root, string resolved) =>
        resolved.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
}
"""

PLANTED_INDIRECT = """namespace AiDe.Core.Projections;

internal static class PlantedIndirect
{
    internal static bool Inside(string root, string candidate)
    {
        var rooted = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;

        return candidate.StartsWith(rooted, StringComparison.OrdinalIgnoreCase);
    }
}
"""


def _write(root: Path, relative: str, body: str) -> Path:
    path = root / relative
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(body, encoding="utf-8")
    return path


def _run_from(cwd: Path) -> tuple[int, str]:
    result = subprocess.run(
        [sys.executable, str(Path(__file__).resolve())],
        cwd=cwd, capture_output=True, text=True)
    return (result.returncode, result.stdout + result.stderr)


def _first(output: str, needle: str) -> str:
    return next((line.strip() for line in output.splitlines() if needle in line), "")


def self_test() -> int:
    """The control must be observed FAILING, or it is not a control (CI6/DC-104).

    Run as a SUBPROCESS from a NON-ROOT directory, because `git rev-parse --show-toplevel` is part
    of what is being tested: a gate that only works from the repository root is a gate that breaks
    in every worktree, and calling check() directly would never exercise it.
    """
    with tempfile.TemporaryDirectory() as directory:
        place = Path(directory).resolve()

        subprocess.run(["git", "init", "-q"], cwd=place, capture_output=True, check=True)

        _write(place, "src/AiDe.Core/PathComparison.cs", HELPER)
        _write(place, "src/AiDe.Core/Extraction/Clean.cs", CLEAN)
        elsewhere = place / "src" / "AiDe.Core" / "Extraction"

        # 1. A clean tree passes, judged from a NON-ROOT directory.
        code, output = _run_from(elsewhere)
        if code != 0:
            print("verify-containment-comparisons: SELF-TEST FAILED -- a clean tree was reported "
                  "dirty, so the gate would be red on correct code." + NEWLINE + output)
            return 1

        # 2. The inline shape (FixtureExtractor's) fails.
        planted = _write(place, "src/AiDe.Core/Extraction/PlantedInline.cs", PLANTED_INLINE)
        code, output = _run_from(elsewhere)
        if code != 1 or "PlantedInline.cs" not in output:
            print("verify-containment-comparisons: SELF-TEST FAILED -- an INLINE "
                  "separator-terminated containment check with a hardcoded comparison was not "
                  "reported." + NEWLINE + output)
            return 1
        print(f"  planted -> {_first(output, 'PlantedInline')}")
        planted.unlink()

        # 3. The one-hop indirect shape (ProjectionService's, KnowledgeExtractor's) fails.
        planted = _write(place, "src/AiDe.Core/Projections/PlantedIndirect.cs", PLANTED_INDIRECT)
        code, output = _run_from(elsewhere)
        if code != 1 or "PlantedIndirect.cs" not in output:
            print("verify-containment-comparisons: SELF-TEST FAILED -- a containment check whose "
                  "terminated root is built into a LOCAL one statement above was not reported. "
                  "Two of the three real sites have exactly this shape, so an inline-only matcher "
                  "reports them clean." + NEWLINE + output)
            return 1
        print(f"  planted -> {_first(output, 'PlantedIndirect')}")
        planted.unlink()

        # 4. An approval that resolves to nothing fails (DC-095).
        (place / "src" / "AiDe.Core" / "PathComparison.cs").unlink()
        code, output = _run_from(elsewhere)
        if code != 1 or "approves nothing" not in output:
            print("verify-containment-comparisons: SELF-TEST FAILED -- an APPROVED_COMPARISONS "
                  "entry with no declaration under src/ was accepted." + NEWLINE + output)
            return 1
        print(f"  planted -> {_first(output, 'approves nothing')}")

        # 5. An approval whose declaration is NOT platform-conditional fails.
        _write(place, "src/AiDe.Core/PathComparison.cs", NOT_CONDITIONAL)
        code, output = _run_from(elsewhere)
        if code != 1 or "it is not" not in output:
            print("verify-containment-comparisons: SELF-TEST FAILED -- an approved comparison "
                  f"whose declaration never consults {WINDOWS_PROBE} was accepted."
                  + NEWLINE + output)
            return 1
        print(f"  planted -> {_first(output, 'it is not')}")

        # 6. An empty corpus is not a pass (DC-006).
        for stale in (place / "src").rglob("*.cs"):
            stale.unlink()
        code, output = _run_from(elsewhere)
        if code != 1 or "read nothing" not in output:
            print("verify-containment-comparisons: SELF-TEST FAILED -- a scan that read no source "
                  "at all reported success." + NEWLINE + output)
            return 1
        print(f"  planted -> {_first(output, 'read nothing')}")

    print("verify-containment-comparisons: self-test OK -- the inline shape, the one-hop indirect "
          "shape, an unresolvable approval, a non-conditional approval and an empty corpus each "
          "fail; a clean tree passes, judged from a non-root directory.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
