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

    python tools/merge-append-only-log.py --self-test

proves the collision path on a real conflict in a throwaway repository (DC-104: a control's first
run is evidence about the control). It broke silently once: pack revision 59 replaced
`audit-log.py`'s `_reserve` with `next_id`, and this tool kept calling the old name — nothing
noticed for two weeks because the collision branch only runs when two sides claim one id.
"""

from __future__ import annotations

import json
import os
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
# The repository whose index holds the conflict. Only the self-test points it elsewhere.
REPO = Path(os.environ.get("MERGE_LOG_REPO") or ROOT)


def stage(number: int, path: str) -> list[str]:
    """One side of the conflict, as lines. Empty when that stage does not exist."""
    result = subprocess.run(["git", "show", f":{number}:{path}"],
                            cwd=REPO, capture_output=True, check=False)

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


def allocator():
    """`audit-log.py`'s own `next_id`, so the two tools cannot disagree on how an id is minted.

    Collision-proof (ULID-form) when the installed pack carries `coord_ids.py`; the legacy
    max-plus-one over the entries it is handed otherwise, or under `COORD_LEGACY_IDS=1`.
    """
    import importlib.util

    script = ROOT / "docs" / "ai-forward-pack" / "scripts" / "audit-log.py"
    spec = importlib.util.spec_from_file_location("auditlog", script)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module.next_id


def self_test() -> int:
    """A real conflict in a throwaway repository: both sides claim `cl-0002` with different content.

    Passes only when nothing is dropped, upstream keeps the id, the incoming entry is re-issued an id
    neither side holds, and the re-issue is printed — under both allocators.
    """
    failures: list[str] = []

    for legacy in (False, True):
        with tempfile.TemporaryDirectory() as tmp:
            repo = Path(tmp)
            log = repo / "docs" / "audit" / "change-log.jsonl"
            log.parent.mkdir(parents=True)

            def git(*args: str) -> None:
                subprocess.run(["git", *args], cwd=repo, check=True, capture_output=True)

            base = '{"id": "cl-0001", "what": "base"}\n'
            git("init", "-q", "-b", "main")
            git("config", "user.email", "self-test@merge-append-only-log")
            git("config", "user.name", "self-test")
            log.write_text(base, encoding="utf-8")
            git("add", ".")
            git("commit", "-qm", "base")
            git("checkout", "-qb", "incoming")
            log.write_text(base + '{"id": "cl-0002", "what": "incoming"}\n', encoding="utf-8")
            git("commit", "-qam", "incoming")
            git("checkout", "-q", "main")
            log.write_text(base + '{"id": "cl-0002", "what": "upstream"}\n', encoding="utf-8")
            git("commit", "-qam", "upstream")
            merge = subprocess.run(["git", "merge", "incoming"], cwd=repo, capture_output=True)
            if merge.returncode == 0:
                failures.append("the fixture did not conflict — the self-test proves nothing")
                continue

            env = dict(os.environ, MERGE_LOG_REPO=str(repo))
            env.pop("COORD_LEGACY_IDS", None)
            if legacy:
                env["COORD_LEGACY_IDS"] = "1"
            run = subprocess.run([sys.executable, __file__, "docs/audit/change-log.jsonl"],
                                 env=env, capture_output=True, text=True, encoding="utf-8")
            # Lenient on purpose: a failed run leaves conflict markers behind, and that must read as
            # a failed check, not as a crashed self-test.
            entries = [e for _, _, e in parse(log.read_text(encoding="utf-8").splitlines()) if "what" in e]
            ids = [e["id"] for e in entries]
            incoming_ids = [e["id"] for e in entries if e["what"] == "incoming"]
            mode = "legacy" if legacy else "collision-proof"
            checks = [
                ("exit 0", run.returncode == 0),
                ("nothing dropped", {e["what"] for e in entries} == {"base", "upstream", "incoming"}),
                ("upstream keeps cl-0002", any(e["id"] == "cl-0002" and e["what"] == "upstream" for e in entries)),
                ("incoming re-issued", incoming_ids and incoming_ids != ["cl-0002"]),
                ("ids unique", len(ids) == len(set(ids))),
                ("re-issue printed", "re-issued cl-0002 -> " in run.stdout),
                ("re-issued id carries the prefix", all(i.startswith("cl-") for i in incoming_ids)),
            ]
            if legacy:
                checks.append(("legacy re-issue is max+1", incoming_ids == ["cl-0003"]))
            for name, ok in checks:
                if not ok:
                    failures.append(f"[{mode}] {name}\n    stdout: {run.stdout.strip()[:300]}\n    stderr: {run.stderr.strip()[-300:]}")

    if failures:
        print("merge-append-only-log --self-test: FAILED")
        for f in failures:
            print("  - " + f)
        return 1
    print("merge-append-only-log --self-test: OK — a real collision is re-issued, nothing dropped, "
          "under both the collision-proof and the legacy allocator")
    return 0


def main() -> int:
    if len(sys.argv) < 2:
        print(__doc__)
        return 2
    if sys.argv[1] == "--self-test":
        return self_test()

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

    # Every entry either side holds: the legacy allocator mints max-plus-one over exactly these, and
    # each re-issued entry joins them so two collisions in one merge cannot share the new id.
    known = [entry for _, _, entry in upstream + incoming]
    next_id = allocator()

    for identifier, line, entry in incoming:
        if line in by_content:
            continue                       # the same entry on both sides: already present

        if identifier and identifier in resolved:
            # A real id collision. Nothing is dropped — the incoming entry is re-issued.
            entry["id"] = next_id(known, prefix)
            known.append(entry)
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

    (REPO / path).write_text("\n".join(merged) + "\n", encoding="utf-8", newline="\n")

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
