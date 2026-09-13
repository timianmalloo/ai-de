#!/usr/bin/env python3
"""score.py — the compile step's eval scorer (spec Addendum D §A14; ADR-0036 rule 3; US-D11 b3).

Reads real `envelope-events.jsonl` rows (one or more session files), folds them into envelopes,
splits the treatment arm into the SAMPLE (the first N = 50 `called` rows under the installed gate
tuple) and the HOLDOUT (the next 50), and writes an admission report whose every metric carries a
NUMERATOR and a DENOMINATOR over the holdout — never a verdict. The floors are the settings
model's (host-compiled, ADR-0036 Gate 2); this script reports numbers and the split witness, and
the reader recomputes every floor. Labels are de-duplicated by the ORIGINATING `called` row, so a
`reused` envelope (ADR-0035) never counts one model output twice, and latency/token percentiles
exclude `reused` rows.

    python tools/compile-eval/score.py <session-dir-or-events.jsonl>... --out ~/.aide/proof/compile-eval-admission.json
    python tools/compile-eval/score.py --self-test        # the report-contract test, red-first fixtures inside

Scoring uses RECORDED envelopes only — this script never calls a model.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import math
import os
import sys
import tempfile
from collections import defaultdict
from pathlib import Path

SCHEMA = "compiled-envelope/1"
N_SAMPLE = 50  # ADR-0036: N is revised only upward
N_HOLDOUT = 50
STRUCTURE_LINES = ("goal", "done_when", "not_in_scope")
AGENTIC_MODES = ("agentic-advisory", "agentic")
AGENTIC_OUTCOMES = ("succeeded", "succeeded_no_structure", "suspect", "reused")
DEGRADED_OUTCOMES = ("timed_out", "malformed", "unavailable")


# ----------------------------------------------------------------------------- reading

def read_rows(path: Path) -> list[dict]:
    """Every parseable row of one events file; a torn line or another schema is skipped and counted on the row itself."""
    rows: list[dict] = []
    for line in path.read_text(encoding="utf-8").splitlines():
        if not line.strip():
            continue
        try:
            row = json.loads(line)
        except json.JSONDecodeError:
            rows.append({"_torn": True})
            continue
        if row.get("schema") != SCHEMA:
            rows.append({"_unknown_schema": True})
            continue
        rows.append(row)
    return rows


def fold(rows: list[dict]) -> dict[str, dict]:
    """envelope_id -> {opened, decorations[], called[], submitted, consumed}, rows in seq order."""
    envelopes: dict[str, dict] = {}
    for row in rows:
        if "_torn" in row or "_unknown_schema" in row:
            continue
        env = envelopes.setdefault(row["envelope_id"], {"id": row["envelope_id"], "opened": None, "decorations": [], "called": [], "submitted": None, "consumed": None})
        kind = row.get("kind")
        if kind == "opened":
            env["opened"] = row
        elif kind == "decorated":
            env["decorations"].append(row)
        elif kind == "called":
            env["called"].append(row)
        elif kind == "submitted" and row.get("accepted"):
            env["submitted"] = row
        elif kind == "consumed":
            env["consumed"] = row
    for env in envelopes.values():
        env["decorations"].sort(key=lambda r: r["seq"])
        env["called"].sort(key=lambda r: r["seq"])
    return envelopes


def current(env: dict, name: str) -> dict | None:
    rows = [d for d in env["decorations"] if d.get("name") == name]
    return rows[-1] if rows else None


def confirmed(env: dict, name: str) -> dict | None:
    """Current(name) ignoring derived rows under agentic-advisory — the same definition Envelope.Confirmed has."""
    advisory = (env["opened"] or {}).get("compile_mode") == "agentic-advisory"
    rows = [d for d in env["decorations"] if d.get("name") == name and not (advisory and d.get("source") == "derived")]
    return rows[-1] if rows else None


def gate_tuple(call: dict, env: dict | None = None) -> tuple:
    """(contract_version, prompt_sha, profile.sha, model_configured) — the profile sha lives on the envelope's family_profile row, not the called row."""
    profile = (current(env, "family_profile") or {}).get("value") if env else None
    profile_sha = profile.get("sha") if isinstance(profile, dict) else None
    return (call.get("contract_version"), call.get("prompt_sha"), profile_sha, call.get("model_configured"))


def parse_tuple(parts: list[str] | None) -> tuple | None:
    """The CLI's spelling of an absent element — null / none / - / '' — reads as None, so a real row's (…, None, …) can match."""
    if parts is None:
        return None
    return tuple(None if p.strip().lower() in ("null", "none", "-", "") else p for p in parts)


# ----------------------------------------------------------------------------- labels

def line_label(env: dict, name: str) -> tuple[str, bool]:
    """(label, operator_filled) per §A14.1: kept · edited · emptied · absent."""
    derived = [d for d in env["decorations"] if d.get("name") == name and d.get("source") == "derived"]
    operator = [d for d in env["decorations"] if d.get("name") == name and d.get("source") == "operator"]
    final = confirmed(env, name)
    final_value = (final or {}).get("value") if isinstance((final or {}).get("value"), str) else None
    operator_filled = bool(final_value) and (final or {}).get("source") == "operator"
    if not derived:
        return "absent", operator_filled
    proposed = derived[-1].get("value")
    if not operator and final_value == proposed:
        return "kept", False
    if final_value is None or final_value == "":
        return "emptied", False
    if final_value == proposed:
        return "kept", True
    return "edited", True


def edit_distance(a: str, b: str) -> int:
    prev = list(range(len(b) + 1))
    for i, ca in enumerate(a, 1):
        cur = [i]
        for j, cb in enumerate(b, 1):
            cur.append(min(prev[j] + 1, cur[j - 1] + 1, prev[j - 1] + (ca != cb)))
        prev = cur
    return prev[-1]


def percentile(values: list[float], q: float) -> float | None:
    if not values:
        return None
    s = sorted(values)
    rank = max(0, min(len(s) - 1, math.ceil(q * len(s)) - 1))
    return s[rank]


# ----------------------------------------------------------------------------- scoring

def originating_call(env: dict, envelopes: dict[str, dict]) -> tuple[str, int] | None:
    """The (envelope_id, seq) of the `called` row that produced this envelope's derived rows — a `reused` row points back to it."""
    if not env["called"]:
        return None
    last = env["called"][-1]
    if last.get("outcome") == "reused" and isinstance(last.get("reason"), str) and last["reason"].startswith("reused_from:"):
        ref = last["reason"][len("reused_from:"):]
        eid, _, seq = ref.partition("#")
        try:
            return (eid, int(seq))
        except ValueError:
            return (eid, -1)
    return (env["id"], last["seq"])


def score(events_paths: list[Path], installed_tuple: tuple | None = None, prefix_measured: int | None = None) -> dict:
    rows: list[dict] = []
    torn = unknown = 0
    for p in events_paths:
        for r in read_rows(p):
            torn += 1 if "_torn" in r else 0
            unknown += 1 if "_unknown_schema" in r else 0
            rows.append(r)
    envelopes = fold(rows)

    # THE TREATMENT ARM, DEFINED ONCE: opened.compile_mode ∈ agentic ∧ a called row exists.
    treatment = [e for e in envelopes.values() if e["opened"] and e["opened"].get("compile_mode") in AGENTIC_MODES and e["called"]]
    treatment.sort(key=lambda e: (e["opened"]["at"], e["id"]))
    control = [e for e in envelopes.values() if e["opened"] and e["opened"].get("compile_mode") == "mechanical-only" and e["submitted"]]

    # The installed gate tuple: given, else the most recent called row's (reported as such).
    tuple_source = "installed"
    if installed_tuple is None:
        tuple_source = "latest called row (no installed tuple given — Inferred)"
        installed_tuple = gate_tuple(treatment[-1]["called"][-1], treatment[-1]) if treatment else (None, None, None, None)
    under_tuple = [e for e in treatment if gate_tuple(e["called"][-1], e) == installed_tuple]
    other_tuple = len(treatment) - len(under_tuple)

    # The invariants are computed over EVERY called row, unfiltered by EffectiveMode (Ruling 68).
    every_called = [c for e in envelopes.values() for c in e["called"]]
    applied_denied = 0  # a derived row named outside the allow-list is a validator defect; counted from the rows
    for e in envelopes.values():
        for d in e["decorations"]:
            if d.get("source") == "derived" and d.get("name") not in STRUCTURE_LINES:
                applied_denied += 1
    tool_calls_total = sum(int(c.get("tool_calls") or 0) for c in every_called)
    permission_total = sum(int(c.get("permission_requests") or 0) for c in every_called)
    # THE DEGRADED-RATE FLOOR'S DOMAIN IS EVERY called ROW (Ruling 76; ADR-0036) — a model that times
    # out a third of its calls before the holdout must not read 0/50. `reused` rows are not a model
    # call and are excluded from the denominator (stated; the reader sees both counts).
    model_calls = [c for c in every_called if c.get("outcome") != "reused"]
    degraded_every = sum(1 for c in model_calls if c.get("outcome") in DEGRADED_OUTCOMES)

    # THE SPLIT WITNESS: the first N under the tuple are the sample, the next N the holdout.
    sample = under_tuple[:N_SAMPLE]
    holdout = under_tuple[N_SAMPLE:N_SAMPLE + N_HOLDOUT]

    def witness(part: list[dict]) -> dict:
        return {
            "envelope_ids": [e["id"] for e in part],
            "first_at": part[0]["opened"]["at"] if part else None,
            "last_at": part[-1]["opened"]["at"] if part else None,
            "n": len(part),
        }

    # LABELS DE-DUPLICATED BY THE ORIGINATING called ROW: a reused envelope's lines count once.
    seen_calls: set[tuple[str, int]] = set()
    holdout_ids = {e["id"] for e in holdout}
    labelled: list[dict] = []
    reused_skipped = 0
    for e in holdout:
        origin = originating_call(e, envelopes)
        # A reused envelope whose originating call lies OUTSIDE the holdout (in the sample) would carry a
        # sample-window output into the holdout's labels: excluded and counted, like a duplicate.
        if origin in seen_calls or (origin is not None and origin[0] != e["id"] and origin[0] not in holdout_ids):
            reused_skipped += 1
            continue
        if origin is not None:
            seen_calls.add(origin)
        labelled.append(e)

    kept = edited = emptied = absent_operator_filled = 0
    distances: list[float] = []
    shape_flip_total = shape_flip_kept = 0
    spans_total = spans_resolving = 0
    confidence_buckets: dict[str, dict[str, int]] = defaultdict(lambda: {"n": 0, "overridden": 0})
    for e in labelled:
        derived_any = any(d.get("source") == "derived" for d in e["decorations"])
        for name in STRUCTURE_LINES:
            label, op_filled = line_label(e, name)
            if label == "kept":
                kept += 1
            elif label == "edited":
                edited += 1
                proposed = [d for d in e["decorations"] if d.get("name") == name and d.get("source") == "derived"][-1].get("value") or ""
                final = (confirmed(e, name) or {}).get("value") or ""
                distances.append(edit_distance(str(proposed), str(final)) / max(1, len(str(proposed))))
            elif label == "emptied":
                emptied += 1
            elif label == "absent" and op_filled:
                absent_operator_filled += 1
        for d in e["decorations"]:
            if d.get("source") != "derived":
                continue
            src = e["opened"].get("source_text") or ""
            for span in d.get("grounded_in") or []:
                spans_total += 1
                s0, s1 = (span.get("span") or [0, 0])[:2]
                if span.get("input") == "source_text" and 0 <= s0 < s1 <= len(src):
                    spans_resolving += 1
            conf = d.get("confidence")
            if isinstance(conf, (int, float)) and 0 <= conf <= 1:
                bucket = f"{math.floor(conf * 5) / 5:.1f}"
                confidence_buckets[bucket]["n"] += 1
                if line_label(e, d.get("name"))[0] in ("edited", "emptied"):
                    confidence_buckets[bucket]["overridden"] += 1
        # shape flip (§A14.3): a goal BLOCK proposed (derived goal AND done_when) on a prompt the operator
        # had not shaped (no operator goal before the call) — then kept, or sent as a Message.
        call_seq = e["called"][-1]["seq"]
        operator_goal_before = any(d.get("name") == "goal" and d.get("source") == "operator" and d["seq"] < call_seq and d.get("value") for d in e["decorations"])
        derived_names = {d.get("name") for d in e["decorations"] if d.get("source") == "derived"}
        if derived_any and not operator_goal_before and {"goal", "done_when"} <= derived_names:
            shape_flip_total += 1
            if confirmed(e, "goal") and confirmed(e, "done_when"):
                shape_flip_kept += 1

    calls_holdout = [e["called"][-1] for e in holdout]
    non_reused = [c for c in calls_holdout if c.get("outcome") != "reused"]
    schema_fail = sum(1 for c in calls_holdout if c.get("outcome") == "malformed")
    degraded_holdout = sum(1 for c in calls_holdout if c.get("outcome") in DEGRADED_OUTCOMES)
    by_outcome: dict[str, int] = defaultdict(int)
    for c in calls_holdout:
        by_outcome[str(c.get("outcome"))] += 1
    by_reason: dict[str, int] = defaultdict(int)
    for c in calls_holdout:
        if c.get("outcome") not in AGENTIC_OUTCOMES:
            by_reason[str(c.get("outcome"))] += 1
    dropped: dict[str, int] = defaultdict(int)
    for c in calls_holdout:
        for k, v in (c.get("dropped") or {}).items():
            dropped[k] += int(v or 0)

    latencies = [float(c["latency_ms"]) for c in non_reused if isinstance(c.get("latency_ms"), (int, float))]
    censored = sum(1 for c in non_reused if c.get("outcome") == "timed_out")
    with_usage = [c for c in non_reused if isinstance(c.get("cost"), dict)]
    tokens_in_cache = [int(c["cost"].get("tokens_in") or 0) + int(c["cost"].get("cache_read") or 0) for c in with_usage]
    tokens_out = [int(c["cost"].get("tokens_out") or 0) for c in with_usage]

    def ratio(num: int, den: int) -> dict:
        return {"numerator": num, "denominator": den}

    def measured(values: list, total: int) -> dict:
        return {"n_measured": len(values), "n_total": total, "partial": len(values) < total or total < N_HOLDOUT}

    report = {
        "contract": "compile-eval-admission/1",
        "note": "numerators and denominators only — the settings model recomputes every floor (ADR-0036 Gate 2); no verdict is written here",
        "sources": [str(p) for p in events_paths],
        "rows": {"total": len(rows), "torn": torn, "unknown_schema": unknown},
        "gate_tuple": {"contract_version": installed_tuple[0], "prompt_sha": installed_tuple[1], "profile_sha": installed_tuple[2], "model_configured": installed_tuple[3], "source": tuple_source},
        "treatment": {"n": len(treatment), "under_tuple": len(under_tuple), "other_tuple_excluded": other_tuple},
        "control": {"n": len(control)},
        "sample": witness(sample),
        "holdout": {**witness(holdout), "by_outcome": dict(by_outcome)},
        "labels_deduplicated": {"envelopes_labelled": len(labelled), "reused_skipped": reused_skipped},
        "prefix_measured": prefix_measured,
        "invariants": {
            "applied_denied": ratio(applied_denied, len(every_called)),
            "tool_calls": ratio(tool_calls_total, len(every_called)),
            "permission_requests": ratio(permission_total, len(every_called)),
        },
        "metrics": {
            "acceptance": ratio(kept, 3 * len(labelled)),
            "missed": ratio(absent_operator_filled, len(labelled)),
            "emptied": ratio(emptied, 3 * len(labelled)),
            "edited": ratio(edited, 3 * len(labelled)),
            "edit_distance_normalised": {"p50": percentile(distances, 0.5), "p95": percentile(distances, 0.95), **measured(distances, edited)},
            "shape_flip_kept": ratio(shape_flip_kept, shape_flip_total),
            "span_resolution": ratio(spans_resolving, spans_total),
            "schema_fail": ratio(schema_fail, len(calls_holdout)),
            "degraded": ratio(degraded_every, len(model_calls)),
            "degraded_holdout": ratio(degraded_holdout, len(calls_holdout)),
            "degraded_by_reason": dict(by_reason),
            "dropped_by_reason": dict(dropped),
            "calibration_by_confidence": {k: ratio(v["overridden"], v["n"]) for k, v in sorted(confidence_buckets.items())},
            "latency_ms": {"p50": percentile(latencies, 0.5), "p95": percentile(latencies, 0.95), "n_censored": censored, "p95_is_lower_bound": censored > 0, **measured(latencies, len(non_reused))},
            "tokens_in_plus_cache": {"p50": percentile(tokens_in_cache, 0.5), "p95": percentile(tokens_in_cache, 0.95), **measured(tokens_in_cache, len(non_reused))},
            "tokens_out": {"p50": percentile(tokens_out, 0.5), "p95": percentile(tokens_out, 0.95), **measured(tokens_out, len(non_reused))},
        },
    }
    return report


# ----------------------------------------------------------------------------- the contract test

FORBIDDEN_KEYS = {"met", "verdict", "passed", "pass", "admitted", "selectable"}


def check_contract(report: dict) -> list[str]:
    """The report-contract test (US-D11 b3): every metric carries numerator/denominator or n_measured/n_total; no verdict key anywhere; the split witness is present and disjoint."""
    problems: list[str] = []

    def walk(node, path):
        if isinstance(node, dict):
            for k, v in node.items():
                if k in FORBIDDEN_KEYS:
                    problems.append(f"{path}.{k}: a verdict key — the reader recomputes floors, the report never writes one")
                walk(v, f"{path}.{k}")
        elif isinstance(node, list):
            for i, v in enumerate(node):
                walk(v, f"{path}[{i}]")

    walk(report, "report")
    for name, metric in {**report["metrics"], **report["invariants"]}.items():
        if not isinstance(metric, dict):
            problems.append(f"metrics.{name}: not an object")
            continue
        if name in ("degraded_by_reason", "dropped_by_reason"):
            continue
        if name == "calibration_by_confidence":
            for bucket, r in metric.items():
                if set(r) != {"numerator", "denominator"}:
                    problems.append(f"metrics.{name}[{bucket}]: not a numerator/denominator pair")
            continue
        has_ratio = "numerator" in metric and "denominator" in metric
        has_measured = "n_measured" in metric and "n_total" in metric
        if not (has_ratio or has_measured):
            problems.append(f"metrics.{name}: carries neither numerator/denominator nor n_measured/n_total")
    s, h = set(report["sample"]["envelope_ids"]), set(report["holdout"]["envelope_ids"])
    if s & h:
        problems.append(f"split witness: sample and holdout overlap on {sorted(s & h)[:3]}")
    if report["sample"]["last_at"] and report["holdout"]["first_at"] and report["holdout"]["first_at"] < report["sample"]["last_at"]:
        problems.append("split witness: holdout.first_at < sample.last_at")
    if "prefix_measured" not in report:
        problems.append("prefix_measured is not stated (null is allowed; absence is not)")
    return problems


def _row(env: str, seq: int, kind: str, at: str, **fields) -> dict:
    return {"schema": SCHEMA, "envelope_id": env, "seq": seq, "kind": kind, "at": at, "prev_sha": "0" * 64, **fields}


def _fixture(n_agentic: int, reused_every: int = 0, mode: str = "agentic-advisory") -> list[str]:
    """A synthetic corpus: n agentic envelopes (a derived goal kept on even ids, emptied on odd), one control envelope."""
    lines: list[str] = []
    for i in range(n_agentic):
        env = f"2026091319{i:04d}Z-{i:08x}"
        at = f"2026-09-13T19:{i // 60:02d}:{i % 60:02d}.000+00:00"
        lines.append(json.dumps(_row(env, 1, "opened", at, source_text=f"rename helper {i}", session_id="s", engine_id="claude-code", compile_mode=mode, supersedes=None, constants={"k": 5, "byte_bound": 32768, "bound_ms": 60000})))
        lines.append(json.dumps(_row(env, 2, "decorated", at, name="task_class", value="implement", source="session-default")))
        lines.append(json.dumps(_row(env, 3, "decorated", at, name="ceilings", value={"fan_out": 2, "budget": None}, source="mechanical")))
        reused = reused_every and i % reused_every == 1
        call = dict(engine_id="claude-code", model_configured="claude-sonnet-5", model_observed="claude-opus-5[1m]", latency_ms=None if reused else 800 + i,
                    cost={"tokens_in": 0, "tokens_out": 0, "cache_read": 0, "requests": 0} if reused else {"tokens_in": 2, "tokens_out": 400, "cache_read": 4800, "requests": 1},
                    outcome="reused" if reused else "succeeded", reason=f"reused_from:2026091319{i-1:04d}Z-{i-1:08x}#4" if reused else None,
                    inputs_sha="a" * 64, prompt_sha="p" * 64, contract_version="compile-prompt/1", permission_requests=0, tool_calls=0,
                    dropped={"unknown_name": 0, "already_supplied": 0, "ungrounded": 0, "mention_bearing": 0, "type_fail": 0})
        lines.append(json.dumps(_row(env, 4, "called", at, **call)))
        lines.append(json.dumps(_row(env, 5, "decorated", at, name="goal", value=f"Rename helper {i}.", source="derived", call_seq=4, confidence=0.8, grounded_in=[{"input": "source_text", "span": [0, 6]}])))
        if i % 2 == 0:
            lines.append(json.dumps(_row(env, 6, "decorated", at, name="goal", value=f"Rename helper {i}.", source="operator")))
        else:
            lines.append(json.dumps(_row(env, 6, "decorated", at, name="goal", value=None, source="operator")))
        lines.append(json.dumps(_row(env, 7, "submitted", at, accepted=True, refusal=None, text_sha256="t" * 64, projection_sha="q" * 64, projector_version="1")))
    env = "20260913180000000Z-control0"
    lines.append(json.dumps(_row(env, 1, "opened", "2026-09-13T18:00:00.000+00:00", source_text="x", session_id="s", engine_id="claude-code", compile_mode="mechanical-only", supersedes=None, constants={"k": 5, "byte_bound": 32768, "bound_ms": 60000})))
    lines.append(json.dumps(_row(env, 2, "submitted", "2026-09-13T18:00:00.000+00:00", accepted=True, refusal=None, text_sha256="t" * 64, projection_sha="q" * 64, projector_version="1")))
    return lines


def self_test() -> int:
    with tempfile.TemporaryDirectory() as tmp:
        events = Path(tmp) / "envelope-events.jsonl"
        events.write_text("\n".join(_fixture(120, reused_every=10)) + "\n", encoding="utf-8")
        report = score([events], prefix_measured=None)
        problems = check_contract(report)
        if problems:
            print("SELF-TEST FAILED (contract):\n  " + "\n  ".join(problems))
            return 1
        if report["sample"]["n"] != 50 or report["holdout"]["n"] != 50:
            print(f"SELF-TEST FAILED: split {report['sample']['n']}/{report['holdout']['n']}")
            return 1
        if report["holdout"]["first_at"] < report["sample"]["last_at"]:
            print("SELF-TEST FAILED: holdout precedes sample")
            return 1
        # dedup: 50 holdout envelopes, every tenth (i % 10 == 1) reused → those are skipped
        if report["labels_deduplicated"]["reused_skipped"] != 5:
            print(f"SELF-TEST FAILED: reused_skipped {report['labels_deduplicated']['reused_skipped']} != 5")
            return 1
        if report["metrics"]["latency_ms"]["n_measured"] != 45 or report["metrics"]["latency_ms"]["n_total"] != 45:
            print(f"SELF-TEST FAILED: latency n {report['metrics']['latency_ms']}")
            return 1
        if report["invariants"]["tool_calls"]["denominator"] != 120:
            print("SELF-TEST FAILED: the invariants must run over EVERY called row (120)")
            return 1
        # The degraded floor's domain: every model call (120 rows, 12 reused → 108), not the holdout's 50.
        if report["metrics"]["degraded"]["denominator"] != 108:
            print(f"SELF-TEST FAILED: degraded must be over every model call (108), got {report['metrics']['degraded']}")
            return 1
        # --tuple with the CLI's null spelling matches real rows whose profile sha is absent.
        tupled = score([events], parse_tuple(["compile-prompt/1", "p" * 64, "null", "claude-sonnet-5"]))
        if tupled["treatment"]["under_tuple"] != 120:
            print(f"SELF-TEST FAILED: --tuple with a null profile sha matched {tupled['treatment']['under_tuple']} of 120")
            return 1
        acc = report["metrics"]["acceptance"]
        if acc["denominator"] != 3 * 45:
            print(f"SELF-TEST FAILED: acceptance denominator {acc['denominator']} != 3 × 45")
            return 1
        print(f"SELF-TEST PASS (green as expected): contract holds; sample 50 / holdout 50; reused_skipped 5; acceptance {acc['numerator']}/{acc['denominator']}")

        # RED, as expected: a report that smuggles a verdict, or overlaps the split, must fail the contract.
        bad = json.loads(json.dumps(report))
        bad["metrics"]["acceptance"]["met"] = True
        bad["holdout"]["envelope_ids"] = bad["sample"]["envelope_ids"][:1] + bad["holdout"]["envelope_ids"]
        red = check_contract(bad)
        if len(red) < 2 or not any("verdict" in p for p in red) or not any("overlap" in p for p in red):
            print("SELF-TEST FAILED: the contract did not go red on a verdict key and an overlapping split:\n  " + "\n  ".join(red))
            return 1
        print("SELF-TEST PASS (red as expected): " + " | ".join(red))

        # Too few envelopes: the report is written, every measured metric labelled partial, nothing invented.
        events.write_text("\n".join(_fixture(7)) + "\n", encoding="utf-8")
        small = score([events])
        if small["holdout"]["n"] != 0 or not small["metrics"]["latency_ms"]["partial"] or small["metrics"]["latency_ms"]["p95"] is not None:
            print("SELF-TEST FAILED: a corpus below N must report an empty holdout and partial, never a plausible p95")
            return 1
        print("SELF-TEST PASS (partial as expected): 7 envelopes → holdout 0, partial, p95 null")
    return 0


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("sources", nargs="*", help="session directories or envelope-events.jsonl files")
    parser.add_argument("--out", help="where to write the admission report (default: stdout)")
    parser.add_argument("--prefix-measured", type=int, default=None, help="tokens_in + cache_read of the empty-source-text fixture compile, as billed")
    parser.add_argument("--tuple", nargs=4, metavar=("CONTRACT", "PROMPT_SHA", "PROFILE_SHA", "MODEL"), help="the installed gate tuple")
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args(argv)
    if args.self_test:
        return self_test()
    if not args.sources:
        parser.error("sources are required unless --self-test is given")
    paths: list[Path] = []
    for s in args.sources:
        p = Path(s)
        paths.append(p / "envelope-events.jsonl" if p.is_dir() else p)
    report = score(paths, parse_tuple(args.tuple), args.prefix_measured)
    problems = check_contract(report)
    text = json.dumps(report, indent=2)
    if args.out:
        Path(args.out).parent.mkdir(parents=True, exist_ok=True)
        Path(args.out).write_text(text + "\n", encoding="utf-8")
        print(f"wrote {args.out} (sha256 {hashlib.sha256(text.encode('utf-8')).hexdigest()[:12]}…)")
    else:
        print(text)
    if problems:
        print("CONTRACT PROBLEMS:\n  " + "\n  ".join(problems), file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    # A Windows console defaults to cp1252 and the report's own prose carries `→` and `—`.
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8")
        except (AttributeError, ValueError):
            pass
    sys.exit(main(sys.argv[1:]))
