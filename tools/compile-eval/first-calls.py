#!/usr/bin/env python3
"""first-calls.py — the first 50 advisory compile calls' cost and latency (Ruling 180 (4), condition C4).

Reads real `envelope-events.jsonl` rows with `score.py`'s own reader and fold (imported, never re-implemented), takes the
treatment arm exactly as `score.py` defines it (an envelope opened under an agentic rung with at least one `called` row,
ordered by `opened.at` then id), and reports the FIRST 50 `called` rows that are model calls (`reused` rows excluded):
n, n with cost, n with latency, tokens in+cache and tokens out (sum, p50, p95), latency p50/p95, outcomes by count, and
the envelope ids. A value the wire did not report reads "not recorded", never 0. This script never calls a model and
never touches the network, and it writes nowhere unless --out names a file.

    python tools/compile-eval/first-calls.py <session-dir-or-events.jsonl>... [--out <file>]
    python tools/compile-eval/first-calls.py --self-test
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from score import AGENTIC_MODES, fold, percentile, read_rows  # noqa: E402

LIMIT = 50
NOT_RECORDED = "not recorded"
CONTRACT = "compile-first-calls/1"


def first_calls(envelopes: dict[str, dict], limit: int = LIMIT) -> list[tuple[str, dict]]:
    """(envelope id, called row) for the first `limit` called rows of the treatment arm."""
    treatment = [e for e in envelopes.values() if e["opened"] and e["opened"].get("compile_mode") in AGENTIC_MODES and e["called"]]
    treatment.sort(key=lambda e: (e["opened"]["at"], e["id"]))
    calls = [(e["id"], c) for e in treatment for c in e["called"]]
    return calls[:limit]


def measure(values: list[float]) -> dict:
    return {"n_measured": len(values), "sum": sum(values), "p50": percentile(values, 0.5), "p95": percentile(values, 0.95)}


def report(paths: list[Path]) -> dict:
    rows: list[dict] = []
    for p in paths:
        rows.extend(read_rows(p))
    calls = first_calls(fold(rows))
    costs = [c.get("cost") or {} for _, c in calls]
    tokens_in_cache = [float((cost.get("tokens_in") or 0) + (cost.get("cache_read") or 0)) for cost in costs]
    tokens_out = [float(cost.get("tokens_out") or 0) for cost in costs]
    latencies = [float(c.get("latency_ms") or 0) for _, c in calls]
    by_outcome: dict[str, int] = {}
    for _, c in calls:
        by_outcome[str(c.get("outcome"))] = by_outcome.get(str(c.get("outcome")), 0) + 1
    return {
        "contract": CONTRACT,
        "sources": [str(p) for p in paths],
        "limit": LIMIT,
        "n": len(calls),
        "complete": len(calls) == LIMIT,
        "n_with_cost": len(costs),
        "n_with_latency": len(latencies),
        "tokens_in_plus_cache": measure(tokens_in_cache),
        "tokens_out": measure(tokens_out),
        "latency_ms": measure(latencies),
        "by_outcome": by_outcome,
        "envelope_ids": [eid for eid, _ in calls],
    }


def events_files(arg: str) -> list[Path]:
    p = Path(arg)
    return sorted(p.rglob("envelope-events.jsonl")) if p.is_dir() else [p]


# ----------------------------------------------------------------------------- self-test

def _row(env: str, seq: int, kind: str, at: str, **fields) -> str:
    return json.dumps({"schema": "compiled-envelope/1", "envelope_id": env, "seq": seq, "kind": kind, "at": at, "prev_sha": "0" * 64, **fields})


def _fixture() -> tuple[list[str], list[str]]:
    """54 advisory called rows (51 model calls, 3 reused), 2 model calls with no cost, one mechanical-only control; and the
    envelope ids of the first 50 model calls, in order."""
    lines: list[str] = []
    expected: list[str] = []
    control = "20260913180000000Z-control0"
    lines.append(_row(control, 1, "opened", "2026-09-13T18:00:00.000+00:00", compile_mode="mechanical-only"))
    lines.append(_row(control, 2, "called", "2026-09-13T18:00:00.000+00:00", outcome="succeeded", latency_ms=1, cost={"tokens_in": 9, "tokens_out": 9, "cache_read": 9, "requests": 1}))
    reused_at = {5, 20, 35}
    no_cost_at = {7, 9}
    model_calls = 0
    for i in range(54):
        env = f"2026091319{i:04d}Z-{i:08x}"
        at = f"2026-09-13T19:{i // 60:02d}:{i % 60:02d}.000+00:00"
        lines.append(_row(env, 1, "opened", at, compile_mode="agentic-advisory"))
        if i in reused_at:
            lines.append(_row(env, 2, "called", at, outcome="reused", reason=f"reused_from:2026091319{i - 1:04d}Z-{i - 1:08x}#2", latency_ms=None, cost={"tokens_in": 0, "tokens_out": 0, "cache_read": 0, "requests": 0}))
            continue
        model_calls += 1
        cost = None if i in no_cost_at else {"tokens_in": 2, "tokens_out": 400, "cache_read": 4800, "requests": 1}
        lines.append(_row(env, 2, "called", at, outcome="timed_out" if i == 3 else "succeeded", latency_ms=800 + i, cost=cost))
        if model_calls <= LIMIT:
            expected.append(env)
    return lines, expected


def self_test() -> int:
    lines, expected = _fixture()
    got = report_of(lines)
    problems: list[str] = []
    if got["n"] != LIMIT or not got["complete"]:
        problems.append(f"n {got['n']} (complete {got['complete']}), expected the first {LIMIT} model calls")
    if got["envelope_ids"] != expected:
        problems.append(f"envelope ids are not the first {LIMIT} non-reused calls in order: first difference at "
                        f"{next((k for k, (a, b) in enumerate(zip(got['envelope_ids'], expected)) if a != b), min(len(got['envelope_ids']), len(expected)))}")
    if got["by_outcome"].get("reused"):
        problems.append(f"{got['by_outcome']['reused']} reused rows were counted")
    if "20260913180000000Z-control0" in got["envelope_ids"]:
        problems.append("the mechanical-only control was counted")
    if got["n_with_cost"] != LIMIT - 2:
        problems.append(f"n_with_cost {got['n_with_cost']}, expected {LIMIT - 2} (two calls reported no cost)")
    if got["n_with_latency"] != LIMIT:
        problems.append(f"n_with_latency {got['n_with_latency']}, expected {LIMIT}")
    tic = got["tokens_in_plus_cache"]
    if tic["n_measured"] != LIMIT - 2 or tic["sum"] != (LIMIT - 2) * 4802:
        problems.append(f"tokens in+cache {tic}, expected n {LIMIT - 2} and sum {(LIMIT - 2) * 4802}")
    if got["tokens_out"]["sum"] != (LIMIT - 2) * 400:
        problems.append(f"tokens out sum {got['tokens_out']['sum']}, expected {(LIMIT - 2) * 400}")
    if got["by_outcome"] != {"succeeded": LIMIT - 1, "timed_out": 1}:
        problems.append(f"by_outcome {got['by_outcome']}")

    empty = report_of([])
    if empty["n"] != 0 or empty["latency_ms"]["p50"] != NOT_RECORDED or empty["tokens_out"]["sum"] != NOT_RECORDED:
        problems.append(f"an empty corpus must read '{NOT_RECORDED}', never 0: {empty['latency_ms']}, {empty['tokens_out']}")

    if problems:
        print("SELF-TEST FAILED:\n  " + "\n  ".join(problems))
        return 1
    print(f"SELF-TEST PASS: the first {LIMIT} non-reused advisory calls of 51 (3 reused, 1 control excluded); "
          f"n_with_cost {got['n_with_cost']}; tokens in+cache sum {tic['sum']:.0f}; latency p50 {got['latency_ms']['p50']:.0f} ms; "
          f"an empty corpus reads '{NOT_RECORDED}'")
    return 0


def report_of(lines: list[str]) -> dict:
    """The report over `lines` written to a throwaway events file, so the self-test reads through `read_rows` as a run does."""
    import tempfile

    with tempfile.TemporaryDirectory() as tmp:
        events = Path(tmp) / "envelope-events.jsonl"
        events.write_text("".join(line + "\n" for line in lines), encoding="utf-8", newline="\n")
        return report([events])


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("paths", nargs="*", help="session directories or envelope-events.jsonl files")
    parser.add_argument("--out", help="write the report here as well as to stdout")
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args(argv)
    if args.self_test:
        return self_test()
    if not args.paths:
        parser.error("name at least one session directory or events file")
    paths = [f for a in args.paths for f in events_files(a)]
    result = json.dumps(report(paths), indent=2)
    print(result)
    if args.out:
        Path(args.out).write_text(result + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
