#!/usr/bin/env python3
"""Resolve a conflict in an append-only JSONL log without losing an entry.

**Why this exists.** Two sessions append to `audit-log.jsonl` and `change-log.jsonl`, so every merge
between them conflicts on those files. Resolving by hand is a small script each time, and a small
script written each time gets one of them wrong: a union keyed by id, resolved with `setdefault`,
silently keeps whichever side was read first and **drops the other entry entirely**. That happened —
a design-session entry was lost in a merge this session performed and had to be re-emitted by hand.

The defect is not the collision. It is de-duplicating by a key when the key is the thing in dispute.

**What it does instead.** Entries are unioned by CONTENT, so nothing is ever dropped. An id claimed
by two different entries is a real collision (DC-013): the side already published on the upstream
branch keeps the id, and the other is re-issued from the shared counter that `audit-log.py` uses.
Every action is printed — a merge that resolves silently is indistinguishable from one that lost
something.

Usage, mid-conflict:
    python tools/merge-append-only-log.py docs/audit/audit-log.jsonl
    python tools/merge-append-only-log.py docs/audit/change-log.jsonl --prefix cl

It reads the two sides from the git index, writes the resolved file, and leaves staging to you.
"""

from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

ROOT = Path(__file__).resolve().parent.parent


def stage(number: int, path: str) -> list[str]:
    """One side of the conflict, as lines. Empty when that stage does not exist."""
    result = subprocess.run(["git", "show", f":{number}:{path}"],
                            cwd=ROOT, capture_output=True, check=False)

    if result.returncode != 0:
        return []

    return [line for line in result.stdout.decode("utf-8", "replace").splitlines() if line.strip()]


def parse(lines: list[str]) -> list[tuple[str, str, dict]]:
    """(id, raw line, entry) for each parseable line. Unparseable lines are kept verbatim."""
    out: list[tuple[str, str, dict]] = []

    for line in lines:
        try:
            entry = json.loads(line)
            out.append((str(entry.get("id", "")), line, entry))
        except json.JSONDecodeError:
            out.append(("", line, {}))

    return out


def reserve(prefix: str, floor: int) -> int:
    """Next id from the same shared counter audit-log.py uses, so the two cannot disagree."""
    import importlib.util

    script = ROOT / "docs" / "ai-forward-pack" / "scripts" / "audit-log.py"
    spec = importlib.util.spec_from_file_location("auditlog", script)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)

    allocated = module._reserve(str(ROOT / "docs"), prefix, floor)  # noqa: SLF001 - one owner, on purpose
    return allocated if allocated is not None else floor + 1


def main() -> int:
    if len(sys.argv) < 2:
        print(__doc__)
        return 2

    path = sys.argv[1].replace("\\", "/")
    prefix = "cl" if "--prefix" in sys.argv and sys.argv[sys.argv.index("--prefix") + 1] == "cl" \
        else ("cl" if "change-log" in path else "al")

    # Stage 2 is the branch being merged ONTO; stage 3 is the commit being applied. During a rebase
    # that means stage 2 is upstream — the side already published — which is the one that keeps a
    # contested id.
    upstream, incoming = parse(stage(2, path)), parse(stage(3, path))

    if not upstream and not incoming:
        print(f"merge-append-only-log: no conflict staged for {path}")
        return 1

    resolved: dict[str, str] = {}
    by_content: set[str] = set()
    reissued: list[tuple[str, str]] = []

    for identifier, line, _ in upstream:
        resolved[identifier or line] = line
        by_content.add(line)

    highest = 0
    for identifier, _, _ in upstream + incoming:
        if identifier.startswith(prefix + "-"):
            try:
                highest = max(highest, int(identifier.split("-")[1]))
            except (IndexError, ValueError):
                pass

    for identifier, line, entry in incoming:
        if line in by_content:
            continue                       # the same entry on both sides: already present

        if identifier and identifier in resolved:
            # A real id collision. Nothing is dropped — the incoming entry is re-issued.
            allocated = reserve(prefix, highest)
            highest = max(highest, allocated)
            entry["id"] = f"{prefix}-{allocated:04d}"
            line = json.dumps(entry, ensure_ascii=False)
            reissued.append((identifier, entry["id"]))
            resolved[entry["id"]] = line
        else:
            resolved[identifier or line] = line

        by_content.add(line)

    def order(key: str) -> tuple[int, str]:
        try:
            return (int(key.split("-")[1]), "")
        except (IndexError, ValueError):
            return (10**9, key)

    merged = [resolved[k] for k in sorted(resolved, key=order)]

    (ROOT / path).write_text("\n".join(merged) + "\n", encoding="utf-8", newline="\n")

    print(f"merge-append-only-log: {path}")
    print(f"  upstream {len(upstream)}, incoming {len(incoming)} -> {len(merged)} entr(ies), "
          f"0 dropped")

    for old, new in reissued:
        print(f"  re-issued {old} -> {new} (both sides claimed it; upstream keeps the id)")

    if not reissued:
        print("  no id collisions")

    print(f"  regenerate the view, then: git add {path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
