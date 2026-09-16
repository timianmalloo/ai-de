"""Dormant coordination facts: bounded inert data, local integrity, deterministic fold.

Only trusted composition may supply TrustedContext. No production composition currently
does so. Nothing in this module writes a ledger, grants rights or launches a process
other than read-only local Git plumbing. Synthetic policy belongs exclusively in tests.
"""
from __future__ import annotations

import copy
from dataclasses import dataclass
import hashlib
import json
import math
import os
from pathlib import Path
import re
import subprocess
from typing import Callable

MAX_RECORD = 65536
MAX_DEPTH = 16
MAX_REFS = 16
MAX_ARTIFACT = 1048576
FACT_TYPES = frozenset(("obligation-created", "proposal-superseded",
                        "proposal-accepted", "recipient-consumed"))
FIELDS = frozenset(("kind", "schemaVersion", "eventType", "eventId", "repositoryId",
                    "streamId", "threadId", "obligationId", "inReplyTo", "causationId",
                    "sender", "recipient", "proposal", "producerSeq", "producerAt",
                    "recordedAt", "disposition", "supersedes", "authorityRefs", "payload",
                    "digestVersion", "payloadDigest"))
INTEGRITY_FIELDS = frozenset(("repositoryIdentity", "fullCommitId", "repositoryRelativePath",
                              "gitObjectFormat", "fullBlobId", "sha256", "sectionOrDecisionId"))
PROPOSAL_FIELDS = INTEGRITY_FIELDS | {"id", "revision"}
AUTHORITY_FIELDS = INTEGRITY_FIELDS | {"scope", "issuerEvidenceRef", "verifierReceiptRef"}
CODES = frozenset(("XH.SCHEMA_INVALID", "XH.RECORD_TOO_LARGE", "XH.DEPTH_EXCEEDED",
                   "XH.NONFINITE_JSON", "XH.DIGEST_MISMATCH", "XH.INTEGRITY_INVALID",
                   "XH.AUTHORITY_UNVERIFIED", "XH.PROPOSAL_CONTRACT_MISMATCH",
                   "XH.CORRELATION_MISMATCH", "XH.GENERATION_UNKNOWN",
                   "XH.GENERATION_MISMATCH", "XH.REVISION_STALE",
                   "XH.REVISION_AMBIGUOUS", "XH.CONSUMPTION_MISMATCH"))
Endpoint = tuple[str, str]


class ProtocolError(ValueError):
    def __init__(self, code: str):
        if code not in CODES:
            raise ValueError("unregistered protocol error")
        self.code = code
        super().__init__(code)


@dataclass(frozen=True)
class Verification:
    """External, current policy result bound to this exact immutable event.

    The provider must authenticate its channel and human decision references, check §2,
    subject/action/scope/proposal and current revocation. Boolean labels in data do none
    of that. required_peers comes from the independently verified proposal contract.
    """
    event_digest: str
    action: str
    subject: Endpoint
    repository_id: str
    issuer_verified: bool
    authorized: bool
    current: bool
    required_peers: tuple[Endpoint, ...]


@dataclass(frozen=True)
class TrustedContext:
    repository_id: str
    repository_path: Path
    generations: tuple[Endpoint, ...]
    verify: Callable[[dict, str], Verification]


def text(value: object) -> bool:
    return isinstance(value, str) and bool(value.strip()) and len(value) <= 512


def require(condition: bool, code: str = "XH.SCHEMA_INVALID") -> None:
    if not condition:
        raise ProtocolError(code)


def check_tree(value: object) -> None:
    """Bound container depth (root=1), strings via byte cap, and finite JSON scalars."""
    pending = [(value, 1)]
    while pending:
        node, depth = pending.pop()
        if isinstance(node, (dict, list)):
            require(depth <= MAX_DEPTH, "XH.DEPTH_EXCEEDED")
            children = node.values() if isinstance(node, dict) else node
            if isinstance(node, dict):
                require(all(isinstance(k, str) for k in node))
            pending.extend((child, depth + 1) for child in children)
        elif isinstance(node, float):
            require(math.isfinite(node), "XH.NONFINITE_JSON")
        else:
            require(node is None or type(node) in (str, bool, int))


def canonical_bytes(event: dict) -> bytes:
    """Python-only dormant v1: ordinal keys, UTF-8, absent != null, no normalization."""
    semantic = {k: v for k, v in event.items() if k not in ("payloadDigest", "recordedAt")}
    return json.dumps(semantic, sort_keys=True, separators=(",", ":"),
                      ensure_ascii=False, allow_nan=False).encode("utf-8")


def endpoint(value: object) -> Endpoint:
    require(isinstance(value, dict) and set(value) == {"session", "generation"})
    require(all(text(v) for v in value.values()))
    return value["session"], value["generation"]


def reference_shape(value: object, fields: frozenset) -> None:
    require(isinstance(value, dict) and set(value) == fields)
    require(all(text(v) for v in value.values()))


def validate_fact(event: dict) -> bytes:
    check_tree(event)
    require(isinstance(event, dict) and set(event) == FIELDS)
    require(event["kind"] == "coordination-v1")
    require(isinstance(event["eventType"], str) and event["eventType"] in FACT_TYPES)
    require(all(type(event[k]) is int and event[k] == 1
                for k in ("schemaVersion", "digestVersion")))
    require(all(text(event[k]) for k in ("eventId", "repositoryId", "streamId", "threadId",
                                         "obligationId", "inReplyTo", "causationId")))
    require(type(event["producerSeq"]) is int and 0 < event["producerSeq"] < 2 ** 53)
    require(all(type(event[k]) in (int, float) and math.isfinite(event[k])
                for k in ("producerAt", "recordedAt")))
    endpoint(event["sender"])
    endpoint(event["recipient"])
    reference_shape(event["proposal"], PROPOSAL_FIELDS)
    require(event["disposition"] is None)
    refs = event["authorityRefs"]
    require(isinstance(refs, list) and 1 <= len(refs) <= MAX_REFS)
    for ref in refs:
        reference_shape(ref, AUTHORITY_FIELDS)
    payload = event["payload"]
    require(isinstance(payload, dict))
    kind = event["eventType"]
    if kind == "obligation-created":
        require(set(payload) == {"requiredPeers"})
        peers = payload["requiredPeers"]
        require(isinstance(peers, list) and 1 <= len(peers) <= MAX_REFS)
        keys = [endpoint(p) for p in peers]
        require(len(set(keys)) == len(keys))
    elif kind == "recipient-consumed":
        require(set(payload) == {"eventId", "eventDigest", "checkpoint"})
        require(all(text(v) for v in payload.values()))
    else:
        require(not payload)
    if kind == "proposal-superseded":
        reference_shape(event["supersedes"], PROPOSAL_FIELDS)
        require(event["supersedes"]["id"] == event["proposal"]["id"])
    else:
        require(event["supersedes"] is None)
    try:
        canonical = canonical_bytes(event)
        size = len(json.dumps(event, ensure_ascii=False, allow_nan=False).encode("utf-8"))
    except (ValueError, TypeError, UnicodeError, OverflowError) as exc:
        raise ProtocolError("XH.SCHEMA_INVALID") from exc
    require(size <= MAX_RECORD, "XH.RECORD_TOO_LARGE")
    require(event["payloadDigest"] == hashlib.sha256(canonical).hexdigest(),
            "XH.DIGEST_MISMATCH")
    return canonical


def _git(context: TrustedContext, *args: str) -> bytes:
    env = {k: v for k, v in os.environ.items() if not k.startswith("GIT_")}
    env.update(GIT_NO_LAZY_FETCH="1", GIT_NO_REPLACE_OBJECTS="1", GIT_TERMINAL_PROMPT="0")
    try:
        proc = subprocess.run(["git", "--no-replace-objects", *args],
                              cwd=context.repository_path, env=env, capture_output=True,
                              timeout=10)
    except (OSError, subprocess.SubprocessError) as exc:
        raise ProtocolError("XH.INTEGRITY_INVALID") from exc
    require(proc.returncode == 0, "XH.INTEGRITY_INVALID")
    return proc.stdout


def verify_integrity(ref: dict, context: TrustedContext) -> None:
    """Resolve inert objects in the bound local repo; never fetch or execute content."""
    code = "XH.INTEGRITY_INVALID"
    require(ref["repositoryIdentity"] == context.repository_id, code)
    path = ref["repositoryRelativePath"]
    parts = path.split("/")
    require(not any(p in ("", ".", "..") or p.lower() == ".git" for p in parts), code)
    require(not any(c in path for c in ("\\", ":", "\x00", "\n", "\r")), code)
    fmt = ref["gitObjectFormat"]
    require(fmt in ("sha1", "sha256"), code)
    length = 40 if fmt == "sha1" else 64
    require(all(re.fullmatch("[0-9a-f]{" + str(length) + "}", ref[k]) is not None
                for k in ("fullCommitId", "fullBlobId")), code)
    require(re.fullmatch("[0-9a-f]{64}", ref["sha256"]) is not None, code)
    root = _git(context, "rev-parse", "--show-toplevel").decode("utf-8").strip()
    require(Path(root).resolve() == context.repository_path.resolve(), code)
    require(_git(context, "rev-parse", "--show-object-format").decode().strip() == fmt, code)
    commit, blob = ref["fullCommitId"], ref["fullBlobId"]
    require(_git(context, "cat-file", "-t", commit).strip() == b"commit", code)
    require(_git(context, "rev-parse", "--verify", commit + ":" + path).decode().strip() == blob, code)
    require(_git(context, "cat-file", "-t", blob).strip() == b"blob", code)
    size = int(_git(context, "cat-file", "-s", blob).strip())
    require(0 <= size <= MAX_ARTIFACT, code)
    data = _git(context, "cat-file", "blob", blob)
    require(len(data) == size and hashlib.sha256(data).hexdigest() == ref["sha256"], code)
    section = ref["sectionOrDecisionId"]
    require(section.startswith("#") and section.encode("utf-8") in data.splitlines(), code)


def _key(proposal: dict) -> str:
    return json.dumps(proposal, sort_keys=True, separators=(",", ":"), ensure_ascii=False)


def source_key(event: dict) -> tuple[str, str, str]:
    return event["repositoryId"], event["streamId"], event["eventId"]


def fold_protocol(row: dict, facts: list[dict], all_events: list[dict],
                  context: TrustedContext | None) -> None:
    """Append-only facts -> fail-closed view. Order and receipt timestamps grant nothing."""
    related = sorted((e for e in facts if e["inReplyTo"] == row["id"]),
                     key=source_key)
    row.update(protocol_facts=copy.deepcopy(related), protocol_errors=[], verification=[],
               current_proposal=None, acceptances=[], consumptions=[], accepted=False,
               execution_eligible=False, ownership_granted=False, run_granted=False,
               transfer_granted=False, start_granted=False)
    checked: dict[tuple[str, str, str], Verification] = {}
    integrity_cache: set[str] = set()

    def error(event: dict, code: str) -> None:
        row["protocol_errors"].append({k: event[k] for k in
                                       ("repositoryId", "streamId", "eventId")} | {"code": code})

    for event in related:
        result = {"eventId": event["eventId"], "repositoryId": event["repositoryId"],
                  "streamId": event["streamId"], "integrity": "not-checked",
                  "issuer": "unverified", "authorization": "denied"}
        row["verification"].append(result)
        try:
            require(all(event[k] == row["id"] for k in
                        ("inReplyTo", "threadId", "obligationId", "causationId")),
                    "XH.CORRELATION_MISMATCH")
            require(context is not None, "XH.AUTHORITY_UNVERIFIED")
            require(event["repositoryId"] == row.get("repositoryId") == context.repository_id
                    and event["streamId"] == row.get("streamId"),
                    "XH.CORRELATION_MISMATCH")
            generations = dict(context.generations)
            require(len(generations) == len(context.generations), "XH.GENERATION_UNKNOWN")
            for ep in (event["sender"], event["recipient"]):
                session, generation = endpoint(ep)
                require(session in generations, "XH.GENERATION_UNKNOWN")
                require(generations[session] == generation, "XH.GENERATION_MISMATCH")
            refs = [event["proposal"], *event["authorityRefs"]]
            if event["supersedes"] is not None:
                refs.append(event["supersedes"])
            for ref in refs:
                key = _key({k: ref[k] for k in INTEGRITY_FIELDS})
                if key not in integrity_cache:
                    verify_integrity(ref, context)
                    integrity_cache.add(key)
            result["integrity"] = "verified"
            v = context.verify(copy.deepcopy(event), event["eventType"])
            require(isinstance(v, Verification) and v.event_digest == event["payloadDigest"]
                    and v.action == event["eventType"] and v.subject == endpoint(event["sender"])
                    and v.repository_id == context.repository_id, "XH.AUTHORITY_UNVERIFIED")
            result["issuer"] = "verified" if v.issuer_verified is True else "unverified"
            require(v.issuer_verified is True and v.authorized is True and v.current is True,
                    "XH.AUTHORITY_UNVERIFIED")
            require(type(v.required_peers) is tuple and 1 <= len(v.required_peers) <= MAX_REFS,
                    "XH.PROPOSAL_CONTRACT_MISMATCH")
            require(all(type(p) is tuple and len(p) == 2 and all(text(x) for x in p)
                        for p in v.required_peers), "XH.PROPOSAL_CONTRACT_MISMATCH")
            require(len(set(v.required_peers)) == len(v.required_peers),
                    "XH.PROPOSAL_CONTRACT_MISMATCH")
            result["authorization"] = "verified"
            checked[source_key(event)] = v
        except (ProtocolError, OSError, ValueError, TypeError, UnicodeError) as exc:
            error(event, exc.code if isinstance(exc, ProtocolError) else "XH.AUTHORITY_UNVERIFIED")

    proposals: dict[str, dict] = {}
    publications: dict[str, dict] = {}
    peers: dict[str, tuple[Endpoint, ...]] = {}
    ambiguous = False
    for event in related:
        v = checked.get(source_key(event))
        if v is None or event["eventType"] != "obligation-created":
            continue
        key = _key(event["proposal"])
        supplied = {endpoint(p) for p in event["payload"]["requiredPeers"]}
        if supplied != set(v.required_peers):
            error(event, "XH.PROPOSAL_CONTRACT_MISMATCH")
            continue
        if key in peers and set(peers[key]) != set(v.required_peers):
            ambiguous = True
        proposals[key], peers[key] = event["proposal"], v.required_peers
        publications.setdefault(key, event)
    successors: dict[str, set[str]] = {}
    predecessors: dict[str, set[str]] = {}
    for event in related:
        if source_key(event) not in checked or event["eventType"] != "proposal-superseded":
            continue
        old, new = _key(event["supersedes"]), _key(event["proposal"])
        if old not in proposals or new not in proposals:
            error(event, "XH.REVISION_STALE")
            ambiguous = True
            continue
        successors.setdefault(old, set()).add(new)
        predecessors.setdefault(new, set()).add(old)
    ambiguous |= any(len(s) != 1 for s in (*successors.values(), *predecessors.values()))
    roots = set(proposals) - set(predecessors)
    if proposals and len(roots) != 1:
        ambiguous = True
    visited: set[str] = set()
    current = next(iter(roots)) if len(roots) == 1 else None
    while current is not None and current not in visited:
        visited.add(current)
        following = successors.get(current, set())
        if not following:
            break
        current = next(iter(following)) if len(following) == 1 else None
    if proposals and (visited != set(proposals) or current in successors):
        ambiguous = True
    if ambiguous:
        row["protocol_errors"].append({"code": "XH.REVISION_AMBIGUOUS"})
        current = None
    row["current_proposal"] = copy.deepcopy(proposals.get(current))
    if current is not None and "sender" not in row:
        publication = publications[current]
        row.update(sender=copy.deepcopy(publication["sender"]),
                   recipient=copy.deepcopy(publication["recipient"]),
                   proposal=copy.deepcopy(publication["proposal"]),
                   at=publication["producerAt"], to=publication["recipient"]["session"])

    accepted: set[tuple[str, Endpoint]] = set()
    targets = {source_key(e): e for e in all_events}
    consumed: set[tuple[tuple[str, str, str], str, Endpoint, str]] = set()
    for event in related:
        v = checked.get(source_key(event))
        if v is None:
            continue
        kind, key = event["eventType"], _key(event["proposal"])
        if kind == "proposal-accepted":
            actor = endpoint(event["sender"])
            if key not in peers or actor not in peers[key] or set(v.required_peers) != set(peers[key]):
                error(event, "XH.PROPOSAL_CONTRACT_MISMATCH")
                continue
            if (key, actor) not in accepted:
                accepted.add((key, actor))
                row["acceptances"].append(copy.deepcopy(event))
            if key != current:
                error(event, "XH.REVISION_STALE")
        elif kind == "recipient-consumed":
            payload = event["payload"]
            target = targets.get((event["repositoryId"], event["streamId"], payload["eventId"]))
            if (target is None or source_key(target) == source_key(event)
                    or target["payloadDigest"] != payload["eventDigest"]
                    or target["threadId"] != event["threadId"]
                    or target["obligationId"] != event["obligationId"]
                    or target["recipient"] != event["sender"]
                    or target["proposal"] != event["proposal"]):
                error(event, "XH.CONSUMPTION_MISMATCH")
                continue
            stamp = (source_key(target), target["payloadDigest"], endpoint(event["sender"]),
                     payload["checkpoint"])
            if stamp not in consumed:
                consumed.add(stamp)
                row["consumptions"].append(copy.deepcopy(event))
    if current is not None:
        row["accepted"] = all((current, peer) in accepted for peer in peers[current])
