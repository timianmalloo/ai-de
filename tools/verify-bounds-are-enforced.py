#!/usr/bin/env python3
"""A constant that names a bound is compared against something, not just reported.

THE CLASS, found three times in one session:

  * `Evidence` documented that a page "stays comfortably inside MaxResultBytes once serialised".
    MEASURED: 2,000 assertions = 1,004,397 bytes, fifteen times that constant.
  * `Find` built a `ResultBounds` reporting `MaxBytes: 65,536` and returned 461,750 bytes. The cap
    was passed to a struct and never compared to anything (DC-016: a control that cannot fire).
  * The TypeScript miss-counter's comment said `export default someExpression` was excluded; the
    pattern never excluded it, so the disclosure would have fired on nearly every real codebase.

All three are the same shape: **a claim in prose that the code does not make true**. The prose is
where the reviewer looks, so the claim is what gets believed — and each of these survived review.

WHAT THIS CHECKS, and what it deliberately does not. Deciding whether a comment is true needs a
reader. But the mechanical half is checkable and it is where two of the three lived: a constant whose
name says it BOUNDS something must appear in a comparison, a clamp, or a take. A bound that is only
ever assigned, passed as an argument, or named in a comment cannot fire, whatever its documentation
says.

It cannot see the third instance (a regex that did not match what its comment claimed), and says so
rather than implying the class is covered. What it removes is the "declared and never applied" half.

Exit 0 when clean, 1 otherwise. Stdlib only.
"""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
from pathlib import Path

# A constant whose NAME claims it limits something.
BOUND_NAME = re.compile(r"^(Max[A-Za-z]*(Bytes|Ceiling|Nodes|Edges|Results|Length|Paths|Clusters)|[A-Za-z]*Cap|[A-Za-z]*Budget)$")

DECLARATION = re.compile(
    r"^\s*(?:public|private|internal|protected)\s+(?:static\s+)?(?:readonly\s+)?const\s+\w+\s+(\w+)\s*=",
    re.MULTILINE)

# What it means for a bound to actually do something.
ENFORCEMENT = [
    re.compile(r"[<>]=?\s*[\w.]*\b{name}\b"),          # x > Cap
    re.compile(r"\b{name}\b\s*[<>]=?"),                 # Cap < x
    re.compile(r"Clamp\([^)]*\b{name}\b"),              # Clamp(v, 1, Cap)
    re.compile(r"Math\.(Min|Max)\([^)]*\b{name}\b"),
    re.compile(r"\.Take\(\s*[^)]*\b{name}\b"),
    re.compile(r"\bTake\w*\([^)]*\b{name}\b"),
]

# A bound may be declared in one file and enforced in another, so enforcement is searched repo-wide.
SOURCE_GLOB = "*.cs"

# Bounds applied INDIRECTLY, by being handed to a parameter that is itself clamped. Each needs a
# reason naming where it fires, because "it is passed somewhere" is exactly what made `find` look
# safe: MaxResultBytes was passed to a ResultBounds struct and compared to nothing. An entry here is
# a claim a reviewer can check, not a way to quiet the gate.
APPLIED_ELSEWHERE = {
    "OverviewNodeCap":
        "handed to GraphQuery.MaxNodes, which ProjectionService.Graph clamps and GraphProjection "
        "takes against; OversizedResponseTests grows a corpus past it and asserts the response "
        "stops there",
}

# One checked indirect seam, not a reason-only exemption or a general C# call-graph analyzer.
# Exact class/member scopes and adjacent statements connect the pinned index to its bound and the
# NativePin digest guard to hashing. CaptureAsync boundary tests remain the behavioral oracle.
INDEX_SOURCE = "src/AiDe.Core/Understanding/AtlasGitMembership.cs"
INDEX_CALL = '''var index = pins.Add(association.Index, trackChanges: true, role: "worktree-index");
var indexDigest = index.Digest(MaxIndexBytes, deadline.Token);'''
INDEX_GUARD = '''internal string Digest(long maximumBytes, CancellationToken cancellationToken)
{
var length = RandomAccess.GetLength(_handle);
Require(length <= maximumBytes, AtlasMembershipCaptureState.BudgetExceeded, "index-byte-budget");
using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);'''


CS_NONCODE = re.compile(
    r'//[^\n]*|/\*[\s\S]*?\*/|(?P<raw>"{3,})[\s\S]*?(?P=raw)'
    r'|@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'')


def mask_noncode(body: str) -> str:
    """Keep offsets but hide comments/literals from declaration and brace recognition."""
    return CS_NONCODE.sub(lambda match: " " * len(match.group()), body)


def member_body(body: str, declaration: str) -> str:
    """Extract one direct member, rejecting missing/ambiguous/unbalanced declarations."""
    masked = mask_noncode(body)
    matches = [match for match in re.finditer(declaration + r"\s*\{", masked)
               if masked[:match.start()].count("{") == masked[:match.start()].count("}")]
    if len(matches) != 1:
        return ""
    start = matches[0].end()
    depth = 1
    for offset in range(start, len(masked)):
        depth += (masked[offset] == "{") - (masked[offset] == "}")
        if depth == 0:
            return body[start:offset]
    return ""


def index_bound_enforced(body: str) -> bool:
    """Recognize only the real Atlas capture and its nested NativePin digest, not decoys."""
    owner = member_body(body, r"\bclass\s+AtlasGitMembership\b[^;{}]*")
    caller = member_body(owner, r"\binternal\s+async\s+ValueTask<AtlasMembershipSnapshot>\s+"
                        r"CaptureForQualificationAsync\s*\([^)]*\)")
    native_pin = member_body(owner, r"\bclass\s+NativePin\s*:\s*IDisposable")
    digest = member_body(native_pin, r"\binternal\s+string\s+Digest\s*\(\s*long\s+maximumBytes\s*,"
                         r"\s*CancellationToken\s+cancellationToken\s*\)")
    # Exactly one real index.Digest call prevents a live unlimited call being paired with a decoy.
    caller_code = mask_noncode(caller)
    call = re.sub(r"\s+", "", mask_noncode(INDEX_CALL))
    guard = INDEX_GUARD.split("{", 1)[1]
    return (len(re.findall(r"\bindex\s*\.\s*Digest\s*\(", caller_code)) == 1
            and call in re.sub(r"\s+", "", caller_code)
            and re.sub(r"\s+", "", strip_comments(digest)).startswith(re.sub(r"\s+", "", guard)))


def self_test() -> int:
    def fixture(call: str = INDEX_CALL, guard: str = INDEX_GUARD) -> str:
        return ('internal sealed class AtlasGitMembership { '
                'internal async ValueTask<AtlasMembershipSnapshot> CaptureForQualificationAsync('
                'string sourceRoot, AtlasObjectIdentity expectedRootIdentity, CancellationToken cancellationToken) {'
                + call + '} internal sealed class NativePin : IDisposable {' + guard + ' } } }')

    body = fixture()
    cases = {
        "wrong class": (body.replace("class AtlasGitMembership", "class DecoyMembership"), False),
        "live unbounded plus decoy": (body.replace("Digest(MaxIndexBytes,", "Digest(long.MaxValue,")
                                      + body.replace("class AtlasGitMembership", "class DecoyMembership"), False),
        "wrong caller method": (body.replace("CaptureForQualificationAsync", "DecoyCapture"), False),
        "wrong nested class": (body.replace("class NativePin", "class DecoyPin"), False),
        "wrong digest method": (body.replace("string Digest(", "string DecoyDigest("), False),
        "live unbounded plus literal decoy": (fixture(INDEX_CALL.replace("Digest(MaxIndexBytes,", "Digest(long.MaxValue,")
                                                     + '\nvar decoy = """' + INDEX_CALL + '""";'), False),
        "checked caller and guard": (body, True),
        "forwarded unlimited": (body.replace("Digest(MaxIndexBytes,", "Digest(long.MaxValue,"), False),
        "deleted clamp": (body.replace('Require(length <= maximumBytes, AtlasMembershipCaptureState.BudgetExceeded, "index-byte-budget");', ""), False),
        "strict boundary": (body.replace("length <= maximumBytes", "length < maximumBytes"), False),
        "unrelated pin": (body.replace("pins.Add(association.Index,", "pins.Add(executable,"), False),
        "guard after hashing": (fixture(guard=INDEX_GUARD.replace(
            'Require(length <= maximumBytes, AtlasMembershipCaptureState.BudgetExceeded, "index-byte-budget");', "")
            + 'Require(length <= maximumBytes, AtlasMembershipCaptureState.BudgetExceeded, "index-byte-budget");'), False),
        "comment only": ("/*" + body + "*/", False),
        "caller only": (INDEX_CALL, False),
        "helper only": (INDEX_GUARD, False),
    }
    failed = [name for name, (source, expected) in cases.items() if index_bound_enforced(source) != expected]
    for name in failed:
        print(f"FAIL: {name}")
    print(f"verify-bounds-are-enforced self-test: {len(cases) - len(failed)}/{len(cases)} passed")
    return int(bool(failed))


def repo_root() -> Path:
    out = subprocess.run(
        ["git", "rev-parse", "--show-toplevel"], capture_output=True, text=True, encoding="utf-8", errors="replace", check=True)
    return Path(out.stdout.strip())


def tracked_sources(root: Path) -> list[Path]:
    out = subprocess.run(
        ["git", "ls-files", SOURCE_GLOB], capture_output=True, text=True, encoding="utf-8", errors="replace", check=True, cwd=root)

    return [
        root / line for line in out.stdout.splitlines()
        if line.startswith("src/") and line.strip()
    ]


def strip_comments(text: str) -> str:
    """Enforcement must be CODE. A bound mentioned only in prose is the defect, not the proof."""
    text = re.sub(r"/\*.*?\*/", "", text, flags=re.DOTALL)
    return re.sub(r"^[ \t]*//.*$", "", text, flags=re.MULTILINE)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--list", action="store_true", help="show every bound and where it is enforced")
    parser.add_argument("--self-test", action="store_true", help="exercise checked indirect enforcement and its mutants")
    args = parser.parse_args()
    if args.self_test:
        return self_test()

    root = repo_root()
    sources = tracked_sources(root)

    bodies = {path: strip_comments(path.read_text(encoding="utf-8", errors="replace")) for path in sources}
    everything = "\n".join(bodies.values())

    bounds: dict[str, Path] = {}

    for path, body in bodies.items():
        for name in DECLARATION.findall(body):
            if BOUND_NAME.match(name):
                bounds.setdefault(name, path)

    if not bounds:
        print("verify-bounds-are-enforced: FAILED — no bound constants found; has the naming changed?")
        return 1

    unenforced = []

    for name, declared_in in sorted(bounds.items()):
        indirect = name in APPLIED_ELSEWHERE or (
            name == "MaxIndexBytes" and declared_in == root / INDEX_SOURCE
            and index_bound_enforced(bodies[declared_in]))
        applied = indirect or any(
            re.search(rule.pattern.format(name=re.escape(name)), everything)
            for rule in ENFORCEMENT)

        if args.list:
            how = "indirect" if indirect else ("applied " if applied else "DECLARED")
            print(f"  {how:8} {name}  ({declared_in.relative_to(root)})")

        if not applied:
            unenforced.append(
                f"{name} (declared in {declared_in.relative_to(root)}) is never compared, clamped or "
                f"taken against — it can be reported but it cannot fire")

    if unenforced:
        print("verify-bounds-are-enforced: FAILED")
        for problem in unenforced:
            print(f"  - {problem}")
        print()
        print("  A bound that is only assigned, passed or documented is a promise the code does not")
        print("  keep. Apply it, or rename it so it stops claiming to be a limit.")
        return 1

    print(
        f"verify-bounds-are-enforced: OK — {len(bounds)} bound constant(s), every one compared, "
        "clamped or taken against in code.")
    print(
        "  Note: this checks that a bound is APPLIED, not that a comment describing it is true. "
        "The prose half of the class still needs a reader.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
