#!/usr/bin/env python3
"""ring.py — the A6 ring: re-score on a triple change, demote on regression (ADR-0036 Gate 3; CV-4).

Re-scores the recorded envelopes (via `score.py`, imported — never re-implemented) whenever the
(adapter sha, CLI sha, craft-profile sha) triple changes, or whenever a `called` row's
`model_observed` disagrees with its `model_configured` (CV-3's residual: a silent provider-side
model swap the wire would otherwise hide). A regression against ADR-0036's Gate 2 floors — the
same five this repository's C# reader (`CompileAdmissionGate`) recomputes: the split witness,
`schema_fail`, `applied_denied`, `tool_calls`, `degraded` — demotes `agentic` to `agentic-advisory`
and records `compile.mode.changed{from, to, trigger}`; a later re-score whose floors hold again
RE-ADMITS and moves the drift watermark `readmitted_at`.

STATE IS A WATERMARK, NEVER A VERDICT: the ring's own state file remembers the last-seen triple and
whether the ladder is currently demoted — bookkeeping to detect a REAL transition, never consulted
by any reader to decide admission. Gate 2's own admission report — freshly re-scored and written by
this tool — is what a reader recomputes floors from; this file exists so the ring does not re-emit
the same transition on every unchanged re-run (ADR-0036: "the detector has a watermark, so it
cannot flap").

SCOPE NOTE (recorded, not silently decided — NG3): the plan row and this track's own brief name the
ring's triggering triple as (adapter sha, CLI sha, craft-profile sha) — Gate 1's pin identity — and
this file is built to that, literally. ADR-0036's Gate 3 prose separately names the A6 ring's trigger
as (contract_version, prompt_sha, profile.sha) — the prompt template's identity — with a heavier
mechanism (old vs new over the golden set, k >= 3 paired-median samples). The two are different
questions ("did the installed software change" vs "did the prompt change") that the ADR bundles
under one "ring" name; this tool answers the first, scoped to D-3's code. Building the full
prompt-version A/B ring is a finding for a future track, not built here.

    python tools/compile-eval/ring.py <session-dir-or-events.jsonl>... --adapter-sha SHA --cli-sha SHA
        [--profile-sha SHA] [--state PATH] [--log PATH] [--out PATH] [--tuple ...] [--prefix-measured N]
    python tools/compile-eval/ring.py --self-test
"""
from __future__ import annotations

import argparse
import hashlib
import json
import sys
import tempfile
from datetime import datetime, timezone
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from score import fold, read_rows, score  # noqa: E402

DEFAULT_STATE = Path.home() / ".aide" / "proof" / "compile-ring-state.json"
DEFAULT_LOG = Path.home() / ".aide" / "proof" / "compile-mode-events.jsonl"

STATE_SCHEMA = "compile-ring-state/1"

# The same five floors CompileAdmissionGate.cs recomputes (ADR-0036 Gate 2) — kept as named
# constants here rather than re-derived, so a change to either side is a one-line diff to compare.
REQUIRED_HOLDOUT = 50
SCHEMA_FAIL_FLOOR = 0.02
DEGRADED_FLOOR = 0.05  # Ruling 76

TRIGGER_RING = "ring"
TRIGGER_DRIFT = "drift"
TRIGGERS = ("operator", "gate", TRIGGER_RING, TRIGGER_DRIFT)


# ----------------------------------------------------------------------------- floors (mirrors CompileAdmissionGate.cs)

def floors_hold(report: dict) -> tuple[bool, list[str]]:
    """Recomputes Gate 2's floors from the report's own numerator/denominator pairs — never a verdict key. Returns (holds, reasons)."""
    reasons: list[str] = []

    sample_ids = set(report["sample"]["envelope_ids"])
    holdout_ids = set(report["holdout"]["envelope_ids"])
    holdout_n = report["holdout"]["n"]
    if holdout_n < REQUIRED_HOLDOUT or len(holdout_ids) < REQUIRED_HOLDOUT:
        reasons.append(f"holdout.n = {holdout_n} < {REQUIRED_HOLDOUT} — the holdout is judged, never the sample")
    overlap = sample_ids & holdout_ids
    if overlap:
        reasons.append(f"the sample and the holdout share {len(overlap)} envelope id(s)")
    sample_last = report["sample"]["last_at"]
    holdout_first = report["holdout"]["first_at"]
    if sample_last and holdout_first and holdout_first < sample_last:
        reasons.append("holdout.first_at precedes sample.last_at")

    def rate_holds(name: str, floor: float) -> None:
        metric = report["metrics"][name]
        num, den = metric["numerator"], metric["denominator"]
        if den <= 0:
            reasons.append(f"{name} {num}/{den} — no measured rows to compute the rate over")
            return
        rate = num / den
        if rate > floor:
            reasons.append(f"{name} {num}/{den} = {rate:.1%} > {floor:.0%}")

    rate_holds("schema_fail", SCHEMA_FAIL_FLOOR)
    rate_holds("degraded", DEGRADED_FLOOR)

    for name in ("applied_denied", "tool_calls"):
        metric = report["invariants"][name]
        if metric["numerator"] != 0:
            reasons.append(f"{name} {metric['numerator']}/{metric['denominator']} != 0")

    return (len(reasons) == 0, reasons)


# ----------------------------------------------------------------------------- drift (CV-3's residual)

def drift_suspects(envelopes: dict[str, dict]) -> list[dict]:
    """`called` rows where both models are recorded and differ — a silent provider-side model swap the wire would otherwise hide."""
    hits: list[dict] = []
    for env in envelopes.values():
        for call in env["called"]:
            observed = call.get("model_observed")
            configured = call.get("model_configured")
            if not observed or not configured:
                continue
            if observed in ("not recorded",) or configured in ("not recorded",):
                continue
            if observed != configured:
                hits.append({"envelope_id": env["id"], "seq": call["seq"], "model_configured": configured, "model_observed": observed})
    return hits


# ----------------------------------------------------------------------------- state / log

def read_state(path: Path) -> dict:
    if not path.exists():
        return {"schema": STATE_SCHEMA, "triple": None, "mode": None, "demoted_at": None, "readmitted_at": None}
    try:
        state = json.loads(path.read_text(encoding="utf-8"))
    except json.JSONDecodeError:
        return {"schema": STATE_SCHEMA, "triple": None, "mode": None, "demoted_at": None, "readmitted_at": None}
    state.setdefault("triple", None)
    state.setdefault("mode", None)
    state.setdefault("demoted_at", None)
    state.setdefault("readmitted_at", None)
    return state


def write_state(path: Path, state: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(state, indent=2) + "\n", encoding="utf-8")


def append_log(path: Path, row: dict) -> None:
    """Appends one `compile.mode.changed` row — the store is append-only (ADR-0036's transition history)."""
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("a", encoding="utf-8") as f:
        f.write(json.dumps(row) + "\n")


# ----------------------------------------------------------------------------- the ring

def run_ring(
    events_paths: list[Path],
    adapter_sha: str,
    cli_sha: str,
    profile_sha: str | None,
    state_path: Path,
    log_path: Path,
    installed_tuple: tuple | None = None,
    prefix_measured: int | None = None,
    now: str | None = None,
) -> dict:
    now = now or datetime.now(timezone.utc).isoformat()
    state = read_state(state_path)
    triple = {"adapter_sha256": adapter_sha, "cli_sha256": cli_sha, "profile_sha": profile_sha}
    triple_changed = state["triple"] != triple

    rows = [r for p in events_paths for r in read_rows(p)]
    envelopes = fold(rows)
    suspects = drift_suspects(envelopes)
    drift_found = len(suspects) > 0

    result: dict = {
        "at": now,
        "triple_changed": triple_changed,
        "drift_suspects": suspects,
        "action": "no-op",
        "report": None,
        "holds": None,
        "reasons": [],
    }

    if not triple_changed and not drift_found:
        # UNCHANGED, AND NO DRIFT: no-op — the ring re-scores only on a real reason to (ADR-0036).
        state["last_checked_at"] = now
        write_state(state_path, state)
        return result

    report = score(events_paths, installed_tuple, prefix_measured)
    holds, reasons = floors_hold(report)
    result["report"] = report
    result["holds"] = holds
    result["reasons"] = reasons
    result["action"] = "rescored"

    previous_mode = state["mode"]
    trigger = TRIGGER_DRIFT if drift_found else TRIGGER_RING
    demote_needed = previous_mode == "agentic" and (not holds or drift_found)
    readmit_needed = previous_mode == "agentic-advisory" and holds and not drift_found and state.get("demoted_at")

    if demote_needed:
        append_log(log_path, {"schema": "compile-mode-event/1", "kind": "compile.mode.changed", "from": "agentic", "to": "agentic-advisory", "trigger": trigger, "at": now, "reasons": reasons or [f"drift: {suspects[0]['model_configured']} -> {suspects[0]['model_observed']}"]})
        state["mode"] = "agentic-advisory"
        state["demoted_at"] = now
        result["action"] = "demoted"
    elif readmit_needed:
        append_log(log_path, {"schema": "compile-mode-event/1", "kind": "compile.mode.changed", "from": "agentic-advisory", "to": "agentic", "trigger": trigger, "at": now, "reasons": []})
        state["readmitted_at"] = now
        state["mode"] = "agentic"
        result["action"] = "readmitted"
    elif previous_mode is None:
        # FIRST OBSERVATION: nothing to transition FROM — establish the baseline silently.
        state["mode"] = "agentic" if holds else "agentic-advisory"

    state["triple"] = triple
    state["last_checked_at"] = now
    write_state(state_path, state)
    return result


# ----------------------------------------------------------------------------- the self-test

def _row(env: str, seq: int, kind: str, at: str, **fields) -> str:
    return json.dumps({"schema": "compiled-envelope/1", "envelope_id": env, "seq": seq, "kind": kind, "at": at, "prev_sha": "0" * 64, **fields})


def _fixture_lines(n: int, model_observed: str = "claude-sonnet-5", degraded_every: int | None = None, schema_fail_every: int | None = None) -> list[str]:
    """n agentic-advisory envelopes, a `called` row each, a kept derived goal — same shape score.py's own fixture uses."""
    lines: list[str] = []
    for i in range(n):
        env = f"2026091319{i:04d}Z-{i:08x}"
        at = f"2026-09-13T19:{i // 60:02d}:{i % 60:02d}.000+00:00"
        lines.append(_row(env, 1, "opened", at, source_text=f"do thing {i}", session_id="s", engine_id="claude-code", compile_mode="agentic-advisory", supersedes=None, constants={"k": 5, "byte_bound": 32768, "bound_ms": 60000}))
        outcome = "succeeded"
        if degraded_every and i % degraded_every == 0:
            outcome = "timed_out"
        elif schema_fail_every and i % schema_fail_every == 0:
            outcome = "malformed"
        lines.append(_row(env, 2, "called", at, engine_id="claude-code", model_configured="claude-sonnet-5", model_observed=model_observed,
                           latency_ms=800, cost={"tokens_in": 2, "tokens_out": 400, "cache_read": 4800, "requests": 1}, outcome=outcome, reason=None if outcome == "succeeded" else outcome,
                           inputs_sha="a" * 64, prompt_sha="p" * 64, contract_version="compile-prompt/1", permission_requests=0, tool_calls=0,
                           dropped={"unknown_name": 0, "already_supplied": 0, "ungrounded": 0, "mention_bearing": 0, "type_fail": 0}))
        if outcome == "succeeded":
            lines.append(_row(env, 3, "decorated", at, name="goal", value=f"Do thing {i}.", source="derived", call_seq=2, confidence=0.8, grounded_in=[]))
            lines.append(_row(env, 4, "decorated", at, name="done_when", value="it is done.", source="derived", call_seq=2, confidence=0.8, grounded_in=[]))
            lines.append(_row(env, 5, "decorated", at, name="goal", value=f"Do thing {i}.", source="operator"))
            lines.append(_row(env, 6, "decorated", at, name="done_when", value="it is done.", source="operator"))
            lines.append(_row(env, 7, "submitted", at, accepted=True, refusal=None, text_sha256="t" * 64, projection_sha="q" * 64, projector_version="1"))
        else:
            lines.append(_row(env, 3, "submitted", at, accepted=True, refusal=None, text_sha256="t" * 64, projection_sha="q" * 64, projector_version="1"))
    return lines


def self_test() -> int:
    with tempfile.TemporaryDirectory() as tmp:
        events = Path(tmp) / "envelope-events.jsonl"
        state_path = Path(tmp) / "state.json"
        log_path = Path(tmp) / "events.jsonl"

        # --- the canonicalisation fixture: the C# half asserts the identical constant over the identical bytes (CV-4's red).
        fixture = Path(tmp) / "canonicalisation-fixture.bin"
        fixture.write_bytes(b"the compile pin canonicalisation fixture (CV-4)\n")
        sha = hashlib.sha256(fixture.read_bytes()).hexdigest()
        expected = "59ca002ce13e199384243973f49f5cf3fb0d39c759f98fcbf79579f1e63c5488"
        if sha != expected:
            print(f"SELF-TEST FAILED (canonicalisation): {sha} != {expected}")
            return 1
        print("SELF-TEST PASS (canonicalisation): matches the C# fixture's constant")

        # --- GREEN: 120 clean envelopes, unchanged triple, no drift -> no-op, no log row.
        events.write_text("\n".join(_fixture_lines(120)) + "\n", encoding="utf-8")
        r1 = run_ring([events], "a" * 64, "b" * 64, None, state_path, log_path)
        if r1["action"] != "rescored" or not r1["holds"]:
            print(f"SELF-TEST FAILED: first run should establish a passing baseline, got {r1['action']} holds={r1['holds']} reasons={r1['reasons']}")
            return 1
        state = read_state(state_path)
        if state["mode"] != "agentic":
            print(f"SELF-TEST FAILED: baseline mode should be agentic, got {state['mode']}")
            return 1
        print("SELF-TEST PASS (baseline): first observation establishes agentic with no transition logged")

        r2 = run_ring([events], "a" * 64, "b" * 64, None, state_path, log_path)
        if r2["action"] != "no-op" or r2["triple_changed"]:
            print(f"SELF-TEST FAILED: an unchanged triple with no drift must no-op, got {r2}")
            return 1
        if log_path.exists():
            print("SELF-TEST FAILED: a no-op run must not touch the append-only log")
            return 1
        print("SELF-TEST PASS (no-op): unchanged triple, no drift -> no re-score, no log row")

        # --- RED then GREEN: a triple change whose floors still hold re-scores but does not demote.
        r3 = run_ring([events], "c" * 64, "b" * 64, None, state_path, log_path)
        if r3["action"] != "rescored" or not r3["holds"]:
            print(f"SELF-TEST FAILED: a triple change with passing floors should re-score without demoting, got {r3}")
            return 1
        if log_path.exists():
            print("SELF-TEST FAILED: a passing re-score must not log a transition (mode did not change)")
            return 1
        print("SELF-TEST PASS (re-score, no transition): triple changed, floors still hold -> rescored, mode unchanged")

        # --- RED: a triple change into a regressed corpus (degraded rate over 5%) demotes with trigger "ring".
        events.write_text("\n".join(_fixture_lines(120, degraded_every=10)) + "\n", encoding="utf-8")  # 12/120 = 10% > 5%
        r4 = run_ring([events], "d" * 64, "b" * 64, None, state_path, log_path)
        if r4["action"] != "demoted":
            print(f"SELF-TEST FAILED: a regressed corpus under a changed triple must demote, got {r4}")
            return 1
        log_rows = [json.loads(line) for line in log_path.read_text(encoding="utf-8").splitlines()]
        if len(log_rows) != 1 or log_rows[0]["trigger"] != TRIGGER_RING or log_rows[0]["from"] != "agentic" or log_rows[0]["to"] != "agentic-advisory":
            print(f"SELF-TEST FAILED: expected one ring-triggered demotion row, got {log_rows}")
            return 1
        if read_state(state_path)["mode"] != "agentic-advisory" or not read_state(state_path)["demoted_at"]:
            print("SELF-TEST FAILED: state must record the demotion and its watermark")
            return 1
        print("SELF-TEST PASS (demote on regression): trigger=ring, one compile.mode.changed row, state demoted_at set")

        # --- unchanged triple again: still demoted, no re-demotion (no flapping), no new log row.
        r5 = run_ring([events], "d" * 64, "b" * 64, None, state_path, log_path)
        if r5["action"] != "no-op":
            print(f"SELF-TEST FAILED: an unchanged triple after a demotion must still no-op, got {r5}")
            return 1
        if len(log_path.read_text(encoding="utf-8").splitlines()) != 1:
            print("SELF-TEST FAILED: a no-op must never append a second row for the same transition (no flapping)")
            return 1
        print("SELF-TEST PASS (no flapping): unchanged triple after a demotion re-scores nothing and logs nothing further")

        # --- GREEN: the corpus recovers and the triple changes again -> re-admits, readmitted_at watermark set.
        events.write_text("\n".join(_fixture_lines(120)) + "\n", encoding="utf-8")
        r6 = run_ring([events], "e" * 64, "b" * 64, None, state_path, log_path)
        if r6["action"] != "readmitted":
            print(f"SELF-TEST FAILED: a passing re-score after a demotion must re-admit, got {r6}")
            return 1
        log_rows = [json.loads(line) for line in log_path.read_text(encoding="utf-8").splitlines()]
        if len(log_rows) != 2 or log_rows[1]["trigger"] != TRIGGER_RING or log_rows[1]["from"] != "agentic-advisory" or log_rows[1]["to"] != "agentic":
            print(f"SELF-TEST FAILED: expected a second row re-admitting, got {log_rows}")
            return 1
        state = read_state(state_path)
        if state["mode"] != "agentic" or not state["readmitted_at"]:
            print("SELF-TEST FAILED: the drift watermark readmitted_at must be set on re-admission")
            return 1
        print("SELF-TEST PASS (re-admit): trigger=ring, readmitted_at watermark set, state back to agentic")

        # --- RED: model_observed != model_configured (CV-3's residual) demotes with trigger "drift", triple UNCHANGED.
        state2_path = Path(tmp) / "state2.json"
        log2_path = Path(tmp) / "events2.jsonl"
        clean = Path(tmp) / "clean.jsonl"
        clean.write_text("\n".join(_fixture_lines(120)) + "\n", encoding="utf-8")
        base = run_ring([clean], "f" * 64, "g" * 64, None, state2_path, log2_path)
        if base["action"] != "rescored" or read_state(state2_path)["mode"] != "agentic":
            print(f"SELF-TEST FAILED: drift-test baseline should establish agentic, got {base}")
            return 1

        drifted = Path(tmp) / "drifted.jsonl"
        drifted.write_text("\n".join(_fixture_lines(120, model_observed="claude-opus-5[1m]")) + "\n", encoding="utf-8")
        r7 = run_ring([drifted], "f" * 64, "g" * 64, None, state2_path, log2_path)  # SAME triple — drift alone must still trip the ring.
        if r7["triple_changed"]:
            print("SELF-TEST FAILED: this step must hold the triple constant to isolate the drift trigger")
            return 1
        if r7["action"] != "demoted" or len(r7["drift_suspects"]) == 0:
            print(f"SELF-TEST FAILED: a model_observed/model_configured mismatch must demote even with an unchanged triple, got {r7}")
            return 1
        log_rows2 = [json.loads(line) for line in log2_path.read_text(encoding="utf-8").splitlines()]
        if log_rows2[-1]["trigger"] != TRIGGER_DRIFT:
            print(f"SELF-TEST FAILED: the drift-caused demotion must carry trigger=drift, got {log_rows2[-1]}")
            return 1
        print("SELF-TEST PASS (drift trigger): a model_observed mismatch demotes with trigger=drift on an unchanged triple")

    return 0


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("sources", nargs="*", help="session directories or envelope-events.jsonl files")
    parser.add_argument("--adapter-sha", help="the installed adapter's dist/acp-agent.js sha256 (CompilePin.Installed's AdapterSha256)")
    parser.add_argument("--cli-sha", help="the installed CLI binary's sha256 (CompilePin.Installed's CliSha256)")
    parser.add_argument("--profile-sha", default=None, help="the craft profile's sha, or omitted while none exists (ADR-0037)")
    parser.add_argument("--state", default=str(DEFAULT_STATE), help="the ring's own watermark state file")
    parser.add_argument("--log", default=str(DEFAULT_LOG), help="the append-only compile.mode.changed log")
    parser.add_argument("--out", help="where to write the freshly re-scored admission report (default: not written)")
    parser.add_argument("--prefix-measured", type=int, default=None)
    parser.add_argument("--tuple", nargs=4, metavar=("CONTRACT", "PROMPT_SHA", "PROFILE_SHA", "MODEL"))
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args(argv)

    if args.self_test:
        return self_test()

    if not args.sources or not args.adapter_sha or not args.cli_sha:
        parser.error("sources, --adapter-sha and --cli-sha are required unless --self-test is given")

    from score import parse_tuple

    paths: list[Path] = []
    for s in args.sources:
        p = Path(s)
        paths.append(p / "envelope-events.jsonl" if p.is_dir() else p)

    result = run_ring(paths, args.adapter_sha, args.cli_sha, args.profile_sha, Path(args.state), Path(args.log), parse_tuple(args.tuple), args.prefix_measured)
    if args.out and result["report"] is not None:
        Path(args.out).parent.mkdir(parents=True, exist_ok=True)
        Path(args.out).write_text(json.dumps(result["report"], indent=2) + "\n", encoding="utf-8")

    print(json.dumps({k: v for k, v in result.items() if k != "report"}, indent=2))
    return 0


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8")
        except (AttributeError, ValueError):
            pass
    sys.exit(main(sys.argv[1:]))
