"""Pinned, synthetic P1 oracle. No live coordination roots or production writers.

Run from the P2 tree: python spikes\\canonical-coordination-contract\\generate.py
--check regenerates in memory and compares. Fixed worklist: 10,000 finite bit
patterns plus decimal families; at most 15,000 vectors / 2 MiB golden bytes.
"""
from __future__ import annotations

import argparse
import copy
import hashlib
import importlib.util
import json
import math
import os
from pathlib import Path
import random
import struct
import subprocess
import sys
import tempfile
import time

PIN = "ebd4f1c8473b70934ec29d778419289719ef5481"
ROOT = Path(__file__).resolve().parents[2]
DEST = ROOT / "tests/AiDe.Core.Tests/Watcher/Fixtures/canonical-coordination-v1.json"
SUPPORT = ("coord-core.py", "coord_protocol.py", "coord_ids.py", "repo_identity.py",
           "tests/test_coord_responses.py", "tests/test_coord_protocol.py")
MAX_CASES = 15000
MAX_BYTES = 2 * 1024 * 1024
SEED = 20260917


def git(repo: Path, *args: str, data: bytes | None = None) -> bytes:
    env = {k: v for k, v in os.environ.items() if not k.startswith("GIT_")}
    env.update(GIT_TERMINAL_PROMPT="0", GIT_NO_LAZY_FETCH="1",
               GIT_AUTHOR_NAME="Synthetic", GIT_COMMITTER_NAME="Synthetic",
               GIT_AUTHOR_EMAIL="synthetic@example.invalid",
               GIT_COMMITTER_EMAIL="synthetic@example.invalid",
               GIT_AUTHOR_DATE="2000-01-01T00:00:00+0000",
               GIT_COMMITTER_DATE="2000-01-01T00:00:00+0000")
    return subprocess.run(["git", *args], cwd=repo, env=env, input=data,
                          capture_output=True, check=True, timeout=30).stdout


def load(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    if spec is None or spec.loader is None:
        raise RuntimeError("SPIKE.IMPORT_FAILED")
    module = importlib.util.module_from_spec(spec)
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


def wire(event: dict) -> bytes:
    return json.dumps(event, ensure_ascii=True, separators=(",", ":"),
                      allow_nan=False).encode("utf-8")


def generate(work: Path) -> dict:
    pins = {}
    for name in SUPPORT:
        source = git(ROOT, "show", f"{PIN}:docs/ai-forward-pack/scripts/{name}")
        target = work / name
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(source)
        pins[name] = hashlib.sha256(source).hexdigest()
    sys.path.insert(0, str(work / "tests"))
    fixtures = load("test_coord_responses", work / "tests/test_coord_responses.py")
    facts = load("test_coord_protocol", work / "tests/test_coord_protocol.py")
    coord = fixtures.coord
    protocol = coord._protocol()
    base = fixtures.response()

    def seal(event: dict) -> dict:
        result = copy.deepcopy(event)
        result["payloadDigest"] = hashlib.sha256(protocol.canonical_bytes(result)).hexdigest()
        return result

    def valid(name: str, event: dict) -> dict:
        event = seal(event)
        canonical = coord.validate_response(event)
        return {"name": name, "source": wire(event).decode(),
                "key": list(protocol.source_key(event)),
                "canonical": canonical.decode(), "sha256": hashlib.sha256(canonical).hexdigest()}

    events = [valid("response", base)]
    for name, value in (
        ("integer", 1), ("float", 1.0), ("negative-zero", -0.0),
        ("null", None), ("bool", True), ("large-integer", 10 ** 1000),
        ("unicode", {"\U00010000": "supplementary", "\ue000": "BMP",
                     "e\u0301": "combining", "\u00e9": "precomposed",
                     "controls": "\0\b\t\n\f\r\x1f\"\\/\u2028\U0001f600"}),
    ):
        event = copy.deepcopy(base)
        event["payload"]["extension"] = value
        events.append(valid(name, event))
    events.append(valid("absent", base))
    for value in (-0.0, 1.0, 1e-5, 1e16, 1.7976931348623157e308):
        event = copy.deepcopy(base)
        event.update(producerAt=value, recordedAt=value)
        events.append(valid("timestamp-" + repr(value), event))

    synthetic = work / "synthetic"
    synthetic.mkdir()
    git(synthetic, "init", "--quiet")
    blob = git(synthetic, "hash-object", "-w", "--stdin", data=b"synthetic proposal\n").decode().strip()
    tree = git(synthetic, "mktree", data=f"100644 blob {blob}\tproposal.md\n".encode()).decode().strip()
    commit = git(synthetic, "commit-tree", tree, data=b"synthetic\n").decode().strip()
    git(synthetic, "update-ref", "HEAD", commit)
    fact_fixture = facts.ProtocolTests()
    fact_fixture.peers = (("peer-a", "g1"), ("peer-b", "g1"))
    fact_fixture.ref = {
        "id": "p", "revision": commit, "repositoryIdentity": "repo",
        "fullCommitId": commit, "repositoryRelativePath": "proposal.md",
        "gitObjectFormat": "sha1", "fullBlobId": blob,
        "sha256": hashlib.sha256(b"synthetic proposal\n").hexdigest(),
        "sectionOrDecisionId": "synthetic",
    }
    for kind in sorted(protocol.FACT_TYPES):
        changes = {}
        if kind != "obligation-created":
            changes["payload"] = {}
        if kind == "proposal-superseded":
            changes["supersedes"] = fact_fixture.ref
        if kind == "recipient-consumed":
            changes["payload"] = {"eventId": "r", "eventDigest": base["payloadDigest"],
                                  "checkpoint": "synthetic-checkpoint"}
        events.append(valid(kind, fact_fixture.event(kind=kind, **changes)))

    numbers = []
    rng = random.Random(SEED)
    bits_worklist = [0, 1, 0x8000000000000000, 0x000fffffffffffff,
                     0x0010000000000000, 0x7fefffffffffffff]
    for _ in range(10000):
        bits = rng.getrandbits(64)
        # Fixed count, not rejection sampling: map exponent 2047 to exponent 0.
        bits_worklist.append((bits & 0x800fffffffffffff) | (((bits >> 52) % 2047) << 52))
    for exponent in range(-324, 309):
        for mantissa in ("1", "-1", "1.2345678901234567", "-1.2345678901234567"):
            value = float(f"{mantissa}e{exponent}")
            if math.isfinite(value):
                bits_worklist.append(struct.unpack(">Q", struct.pack(">d", value))[0])
    for bits in bits_worklist:
        value = struct.unpack(">d", bits.to_bytes(8, "big"))[0]
        event = copy.deepcopy(base)
        event["payload"]["number"] = value
        coord.validate_response(seal(event))
        expected = protocol.canonical_bytes({"v": value}).decode()[5:-1]
        numbers.append([f"{bits:016x}", expected])

    read_root = work / "reader"
    read_root.mkdir()

    def read(raw: bytes) -> tuple[list, list]:
        (read_root / "requests.jsonl").write_bytes(raw)
        return coord.read_request_events(read_root)

    invalid = []
    bad_sources = {
        "duplicate-key": b'{"x":1,"x":2}',
        "nonfinite-nan": b'{"x":NaN}',
        "nonfinite-infinity": b'{"x":Infinity}',
        "overflow-float": b'{"x":1e309}',
        "invalid-utf8": b'{"x":"\xff"}',
        "depth": b'{"x":' + b"[" * 17 + b"0" + b"]" * 17 + b"}",
    }
    for name, raw in bad_sources.items():
        accepted, errors = read(raw + b"\n")
        assert not accepted and errors, name
        invalid.append({"name": name, "hex": raw.hex(), "oracleErrors": errors})
    for name, mutate in (
        ("integer-generation", lambda e: e["sender"].update(generation=1)),
        ("boolean-sequence", lambda e: e.update(producerSeq=True)),
        ("schema-float", lambda e: e.update(schemaVersion=1.0)),
        ("timestamp-bool", lambda e: e.update(producerAt=True)),
        ("huge-timestamp", lambda e: e.update(producerAt=10 ** 1000)),
        ("unpaired-surrogate", lambda e: e["payload"].update(text="\ud800")),
    ):
        event = copy.deepcopy(base)
        mutate(event)
        raw = wire(event)
        accepted, errors = read(raw + b"\n")
        assert not accepted and errors, name
        invalid.append({"name": name, "hex": raw.hex(), "oracleErrors": errors,
                        "candidateScope": "schema-validation-not-implemented"})
        try:
            coord.validate_response(event)
        except (coord.CoordError, ValueError, OverflowError, UnicodeError) as error:
            invalid[-1]["directException"] = type(error).__name__
            invalid[-1]["directCode"] = getattr(error, "code", None)
        else:
            raise AssertionError("SPIKE.INVALID_ACCEPTED")

    limits = []
    for ending in (b"\n", b"\r\n"):
        for content_bytes in (65535, 65536, 65537):
            raw = wire(base)
            raw += b" " * (content_bytes - len(raw)) + ending
            accepted, errors = read(raw)
            limits.append({"name": f"whitespace-{content_bytes}-{ending.hex()}",
                           "sourceBytes": len(raw), "contentBytes": content_bytes,
                           "accepted": len(accepted), "errors": errors,
                           "canonicalBytes": len(coord.validate_response(base))})
    expansion = copy.deepcopy(base)
    expansion["payload"]["numbers"] = [1.0] * 11000
    expansion = seal(expansion)
    # Every 1e0 occupies three source bytes but canonicalizes to 1.0 (same size);
    # 1e2 is three source bytes and canonicalizes to 100.0 (five bytes).
    expansion["payload"]["numbers"] = [100.0] * 11000
    expansion = seal(expansion)
    raw = wire(expansion).replace(b"100.0", b"1e2")
    accepted, errors = read(raw + b"\n")
    limits.append({"name": "float-expansion", "sourceBytes": len(raw) + 1,
                   "uncheckedCanonicalBytes": len(protocol.canonical_bytes(expansion)),
                   "accepted": len(accepted), "errors": errors})
    padded_fact = fact_fixture.event()
    padded_fact["authorityRefs"] *= 16
    for reference in padded_fact["authorityRefs"]:
        for key in reference:
            reference[key] = "x" * 512
    padded_fact = seal(padded_fact)
    limits.append({"name": "oversized-fact", "sourceBytes": len(wire(padded_fact)),
                   "uncheckedCanonicalBytes": len(protocol.canonical_bytes(padded_fact))})
    try:
        coord.validate_response(padded_fact)
    except coord.CoordError as error:
        limits[-1]["validationError"] = error.code
    else:
        raise AssertionError("SPIKE.FACT_BOUND_MISSING")

    return {"fixtureVersion": 1, "normalizationVersion": "python-json-v1-dotnet-r-v1",
            "oracleCommit": PIN, "sourceSha256": pins, "python": sys.version.split()[0],
            "seed": SEED, "syntheticGit": {"commit": commit, "blob": blob},
            "events": events, "numbers": numbers, "invalid": invalid, "limits": limits}


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true")
    parser.add_argument("--binding", action="store_true")
    args = parser.parse_args()
    started = time.perf_counter()
    with tempfile.TemporaryDirectory(prefix="canonical-oracle-") as temp:
        corpus = generate(Path(temp))
        if args.binding:
            measure_binding(Path(temp))
    count = len(corpus["numbers"]) + len(corpus["events"]) + len(corpus["invalid"])
    assert count <= MAX_CASES, "SPIKE.CASE_BOUND"
    encoded = (json.dumps(corpus, ensure_ascii=True, separators=(",", ":")) + "\n").encode()
    assert len(encoded) <= MAX_BYTES, "SPIKE.BYTE_BOUND"
    if args.check:
        assert DEST.read_bytes() == encoded, "SPIKE.ORACLE_DRIFT"
    else:
        DEST.parent.mkdir(parents=True, exist_ok=True)
        DEST.write_bytes(encoded)
    print(json.dumps({"code": "SPIKE.ORACLE_OK", "cases": count, "bytes": len(encoded),
                      "sha256": hashlib.sha256(encoded).hexdigest(),
                      "durationSeconds": time.perf_counter() - started}))


def measure_binding(work: Path) -> None:
    coord = sys.modules["test_coord_responses"].coord
    project = sys.modules["repo_identity"].canonical_project
    primary = work / "synthetic"
    git(primary, "config", "remote.origin.url", "https://example.invalid/team/same-name.git")
    linked = work / "different-worktree-name"
    git(primary, "worktree", "add", "--quiet", "--detach", str(linked), "HEAD")
    other = work / "other" / "synthetic"
    other.mkdir(parents=True)
    git(other, "init", "--quiet")
    git(other, "config", "remote.origin.url", "https://example.invalid/other/same-name.git")
    rows = []
    probe = ROOT / "spikes/canonical-coordination-contract/bin/Debug/net10.0/CanonicalProbe.dll"
    for path in (primary, linked, other):
        selected_root = coord.repo_root(path)
        native = subprocess.run(["dotnet", str(probe), "--identity", str(selected_root)],
                                capture_output=True, check=True, timeout=30)
        rows.append({"physicalCheckout": str(path), "officialPrimary": str(selected_root),
                     "officialProject": project(str(path)), "native": json.loads(native.stdout)})
    assert rows[0]["officialPrimary"] == rows[1]["officialPrimary"]
    assert rows[0]["native"]["CanonicalPath"] == rows[1]["native"]["CanonicalPath"]
    assert rows[0]["officialProject"] == rows[2]["officialProject"]
    assert rows[0]["native"]["CanonicalPath"] != rows[2]["native"]["CanonicalPath"]
    result = {"code": "SPIKE.BINDING_OK", "rows": rows,
              "eventRepositoryAuthority": "not established by project names or event payload",
              "rule": "Trusted physical primary selection precedes native identity construction"}
    destination = ROOT / "spikes/canonical-coordination-contract/records/binding.json"
    destination.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(result))


if __name__ == "__main__":
    main()
