#!/usr/bin/env python3
"""derive-fixtures.py — the golden set, derived mechanically from real envelope rows (§A14.2; ADR-0036 rule 3).

Reads `envelope-events.jsonl` rows, keeps submitted envelopes under an agentic rung that carry a
`called` row, and writes one golden case per envelope: the source text, the open lines, the
`called` outcome, the derived proposals and the operator's confirmed lines. Hand-authored cases
are tagged `authored: true` and EXCLUDED from admission (DC-127 — no fixture is authored by the
deriver's author). Cases are de-duplicated by the originating `called` row.

WORK DATA NEVER ENTERS CHANNEL A BY ACCIDENT: a golden set written under a git-TRACKED path is
refused unless `--affirm <envelope_id>...` names every envelope it contains — the operator's
per-envelope affirmation, recorded on the case as `affirmed: {envelope_id, at}`. Without it the
harness reads the store in place and writes only to an untracked path (the default is
`~/.aide/proof/compile-eval-golden.json`).

    python tools/compile-eval/derive-fixtures.py <session-dir-or-events.jsonl>... [--out PATH] [--affirm ID...]
    python tools/compile-eval/derive-fixtures.py --self-test
"""
from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
import tempfile
from datetime import datetime, timezone
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from score import AGENTIC_MODES, STRUCTURE_LINES, confirmed, fold, originating_call, read_rows  # noqa: E402

DEFAULT_OUT = Path.home() / ".aide" / "proof" / "compile-eval-golden.json"


def is_tracked(path: Path) -> bool:
    """Whether git tracks (or would track) this path: inside a work tree and not ignored. A path outside any repository is untracked."""
    directory = path.parent if path.parent.exists() else path.parent.parent
    try:
        inside = subprocess.run(["git", "rev-parse", "--is-inside-work-tree"], cwd=directory, capture_output=True, text=True)
    except OSError:
        return False
    if inside.returncode != 0 or inside.stdout.strip() != "true":
        return False
    ignored = subprocess.run(["git", "check-ignore", "-q", str(path)], cwd=directory, capture_output=True, text=True)
    return ignored.returncode != 0


def derive(events_paths: list[Path], affirmed: dict[str, str] | None = None) -> dict:
    rows = [r for p in events_paths for r in read_rows(p)]
    envelopes = fold(rows)
    cases: list[dict] = []
    seen: set[tuple[str, int]] = set()
    for env in sorted(envelopes.values(), key=lambda e: ((e["opened"] or {}).get("at") or "", e["id"])):
        if not env["opened"] or env["opened"].get("compile_mode") not in AGENTIC_MODES or not env["called"] or not env["submitted"]:
            continue
        origin = originating_call(env, envelopes)
        if origin in seen:
            continue
        if origin is not None:
            seen.add(origin)
        call = env["called"][-1]
        derived = {d["name"]: d.get("value") for d in env["decorations"] if d.get("source") == "derived"}
        case = {
            "envelope_id": env["id"],
            "authored": False,
            "category": "common",
            "source_text": env["opened"].get("source_text"),
            "open_lines": [n for n in STRUCTURE_LINES if n not in {d["name"] for d in env["decorations"] if d.get("source") in ("operator", "mechanical") and d.get("seq", 0) < call["seq"] and isinstance(d.get("value"), str) and d["value"]}],
            "called": {k: call.get(k) for k in ("outcome", "reason", "model_configured", "model_observed", "contract_version", "prompt_sha", "inputs_sha", "tool_calls", "permission_requests", "dropped")},
            "derived": derived,
            "confirmed": {n: (confirmed(env, n) or {}).get("value") for n in STRUCTURE_LINES},
            "originating_call": {"envelope_id": origin[0], "seq": origin[1]} if origin else None,
        }
        if affirmed and env["id"] in affirmed:
            case["affirmed"] = {"envelope_id": env["id"], "at": affirmed[env["id"]]}
        cases.append(case)
    return {"contract": "compile-eval-golden/1", "derived_at": datetime.now(timezone.utc).isoformat(), "cases": cases, "counts": {"cases": len(cases), "authored": 0}}


def check_committed(golden: dict) -> list[str]:
    """ADR-0036 test 7: a committed case carries `authored: true` or `affirmed: {envelope_id, at}`."""
    problems = []
    for case in golden.get("cases", []):
        if case.get("authored") is True:
            continue
        aff = case.get("affirmed")
        if not isinstance(aff, dict) or aff.get("envelope_id") != case.get("envelope_id") or not aff.get("at"):
            problems.append(f"case {case.get('envelope_id')}: neither authored: true nor affirmed {{envelope_id, at}}")
    return problems


def write(golden: dict, out: Path, affirm: list[str] | None) -> int:
    if is_tracked(out):
        missing = [c["envelope_id"] for c in golden["cases"] if "affirmed" not in c]
        if not affirm or missing:
            print(f"REFUSED: {out} is a git-tracked path and the golden set carries work data (source_text). "
                  f"Pass --affirm with every envelope id it contains ({len(missing)} unaffirmed: {missing[:3]}{'…' if len(missing) > 3 else ''}) or write to an untracked path.", file=sys.stderr)
            return 2
        problems = check_committed(golden)
        if problems:
            print("REFUSED:\n  " + "\n  ".join(problems), file=sys.stderr)
            return 2
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(golden, indent=2) + "\n", encoding="utf-8")
    print(f"wrote {out}: {golden['counts']['cases']} case(s)")
    return 0


def self_test() -> int:
    from score import _fixture  # the same synthetic corpus the scorer's self-test uses

    with tempfile.TemporaryDirectory() as tmp:
        repo = Path(tmp) / "repo"
        repo.mkdir()
        subprocess.run(["git", "init", "-q"], cwd=repo, check=True)
        events = Path(tmp) / "envelope-events.jsonl"
        events.write_text("\n".join(_fixture(12, reused_every=4)) + "\n", encoding="utf-8")
        golden = derive([events])
        # 12 agentic envelopes, i % 4 == 1 reused (i = 1, 5, 9) → 9 cases
        if golden["counts"]["cases"] != 9:
            print(f"SELF-TEST FAILED: expected 9 de-duplicated cases, got {golden['counts']['cases']}")
            return 1
        tracked = repo / "docs" / "golden.json"
        tracked.parent.mkdir()
        if write(golden, tracked, affirm=None) != 2:
            print("SELF-TEST FAILED: a tracked path without --affirm was not refused")
            return 1
        if tracked.exists():
            print("SELF-TEST FAILED: the refusal wrote the file anyway")
            return 1
        print("SELF-TEST PASS (red as expected): a tracked path without --affirm is refused before any write")

        untracked = Path(tmp) / "outside" / "golden.json"
        if write(golden, untracked, affirm=None) != 0 or not untracked.exists():
            print("SELF-TEST FAILED: an untracked path was not written")
            return 1
        print("SELF-TEST PASS (green as expected): an untracked path is written without affirmation")

        ids = [c["envelope_id"] for c in golden["cases"]]
        affirmed = derive([events], {i: "2026-09-13T20:00:00+00:00" for i in ids})
        if write(affirmed, tracked, affirm=ids) != 0 or check_committed(json.loads(tracked.read_text(encoding="utf-8"))):
            print("SELF-TEST FAILED: an affirmed golden set was not accepted at a tracked path")
            return 1
        print("SELF-TEST PASS (green as expected): every case affirmed → the tracked path is written and passes the committed-case check")

        bad = json.loads(tracked.read_text(encoding="utf-8"))
        del bad["cases"][0]["affirmed"]
        if not check_committed(bad):
            print("SELF-TEST FAILED: a committed case without authored/affirmed was not flagged")
            return 1
        print("SELF-TEST PASS (red as expected): a committed case with neither authored nor affirmed is flagged")
    return 0


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("sources", nargs="*")
    parser.add_argument("--out", default=str(DEFAULT_OUT))
    parser.add_argument("--affirm", nargs="*", metavar="ENVELOPE_ID", help="the operator's per-envelope affirmation for a tracked output path")
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args(argv)
    if args.self_test:
        return self_test()
    if not args.sources:
        parser.error("sources are required unless --self-test is given")
    paths = [(Path(s) / "envelope-events.jsonl" if Path(s).is_dir() else Path(s)) for s in args.sources]
    now = datetime.now(timezone.utc).isoformat()
    golden = derive(paths, {i: now for i in (args.affirm or [])})
    return write(golden, Path(args.out), args.affirm)


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
