"""Synthetic dormant P1 contracts. No live stream, endpoint or authority fixtures."""
import contextlib
from concurrent.futures import ThreadPoolExecutor
import copy
import hashlib
import importlib.util
import io
import itertools
import json
import os
from pathlib import Path
import shutil
import stat
import subprocess
import sys
import unittest
from unittest.mock import patch
import uuid

SCRIPTS = Path(__file__).resolve().parents[1]
REPO = SCRIPTS.parents[2]
BASELINE = "94ec9036dd0b72aa5b759badcf21a9e3aba6659b"
BASE_SHA = "f73185a306f7a5b63184cd0cc759569030d29bf7115ad8bd30adf6c19f8bdacf"
SUPPORT = ("coord-core.py", "coord_ids.py", "repo_identity.py")
spec = importlib.util.spec_from_file_location("coord_p1", SCRIPTS / "coord-core.py")
coord = importlib.util.module_from_spec(spec)
spec.loader.exec_module(coord)


def remove_fixture(root: Path) -> None:
    def writable_retry(function, path, error):
        if not isinstance(error, PermissionError):
            raise error
        os.chmod(path, stat.S_IWRITE | stat.S_IREAD)
        function(path)
    shutil.rmtree(root, onexc=writable_retry)


def question() -> dict:
    return {"kind": "request-add", "id": "q", "at": 1, "session": "asker",
            "from": "core", "to": "reviewer", "contract": "synthetic proposal",
            "repositoryId": "repo", "streamId": "stream",
            "proposal": {"id": "p", "revision": "1", "sha256": "a" * 64},
            "recipient": {"session": "reviewer", "generation": "g1"},
            "sender": {"session": "asker", "generation": "a1"}}


def digest(event: dict) -> str:
    semantic = {k: v for k, v in event.items()
                if k not in ("payloadDigest", "recordedAt")}
    return hashlib.sha256(json.dumps(semantic, sort_keys=True, separators=(",", ":"),
                                     ensure_ascii=False, allow_nan=False).encode()).hexdigest()


def response(**changes: object) -> dict:
    event = {"kind": "coordination-v1", "schemaVersion": 1,
             "eventType": "response-recorded", "eventId": "r",
             "repositoryId": "repo", "streamId": "stream",
             "threadId": "q", "obligationId": "q", "inReplyTo": "q",
             "causationId": "q", "sender": question()["recipient"],
             "recipient": question()["sender"], "proposal": question()["proposal"],
             "producerSeq": 1, "producerAt": 2, "recordedAt": 3,
             "disposition": "changes-requested", "supersedes": None,
             "authorityRefs": [], "payload": {"text": "synthetic: revise"},
             "digestVersion": 1}
    event.update(changes)
    event["payloadDigest"] = digest(event)
    return event


class ResponseTests(unittest.TestCase):
    def setUp(self) -> None:
        self.root = REPO / ".agents" / "xh-p1-tests" / uuid.uuid4().hex
        self.root.mkdir(parents=True)
        (self.root / ".git").mkdir()
        self.addCleanup(remove_fixture, self.root)

    def write_events(self, events: list) -> None:
        (self.root / "requests.jsonl").write_text(
            "".join(json.dumps(e) + "\n" for e in events), encoding="utf-8")

    def enhanced(self, events: list, **kwargs: object) -> list:
        return coord.fold_requests(events, enhanced=True,
                                   generations=kwargs.get("generations",
                                       {"asker": "a1", "reviewer": "g1"}))

    def test_Fold_LegacyAddResolve_PreservesPayload(self) -> None:
        q = question()
        rows = coord.fold_requests([q, {"kind": "request-resolve", "id": "q",
                                       "at": 2, "resolution": "ACK approved"}])
        self.assertEqual("resolved", rows[0]["status"])
        self.assertEqual(q["contract"], rows[0]["contract"])
        self.assertNotIn("accepted", rows[0])

    def test_Fold_DuplicateAndPermutedLegacy_NeverReopens(self) -> None:
        q = question()
        r = {"kind": "request-resolve", "id": "q", "at": 2, "resolution": "done"}
        for events in itertools.permutations([q, r, q]):
            with self.subTest(events=events):
                self.assertEqual("resolved", coord.fold_requests(events)[0]["status"])

    def test_Fold_NestedJsonTypes_ConflictingAddsRefusedInEitherOrder(self) -> None:
        for first, second in ((True, 1), (False, 0), (1, 1.0)):
            q = question()
            q["extension"] = {"nested": [{"value": first}]}
            other = copy.deepcopy(q)
            other["extension"]["nested"][0]["value"] = second
            for events in itertools.permutations([q, other]):
                with self.subTest(types=(type(first).__name__, type(second).__name__),
                                  order=json.dumps(events)):
                    self.write_events(events)
                    original = (self.root / "requests.jsonl").read_bytes()
                    parsed, errors = coord.read_request_events(self.root)
                    self.assertEqual([], errors)
                    with self.assertRaises(coord.CoordError) as raised:
                        coord.fold_requests(parsed)
                    self.assertEqual("XH.EVENT_CONFLICT", raised.exception.code)
                    self.assertEqual(original, (self.root / "requests.jsonl").read_bytes())
                    self.assertEqual(json.dumps(events), json.dumps(parsed))

    def test_Fold_ReorderedObjectProperties_AreLegitimateDuplicates(self) -> None:
        q = question()
        q["extension"] = {"nested": [{"first": True, "second": 1, "third": 1.0}]}
        other = dict(reversed(list(copy.deepcopy(q).items())))
        other["extension"]["nested"][0] = {"third": 1.0, "second": 1, "first": True}
        resolved = {"kind": "request-resolve", "id": "q", "at": 2, "resolution": "done"}
        for events in itertools.permutations([q, other, resolved]):
            with self.subTest(order=json.dumps(events)):
                original = json.dumps(events)
                self.write_events(events)
                raw = (self.root / "requests.jsonl").read_bytes()
                parsed, errors = coord.read_request_events(self.root)
                self.assertEqual([], errors)
                rows = coord.fold_requests(parsed)
                self.assertEqual(1, len(rows))
                self.assertEqual("resolved", rows[0]["status"])
                self.assertEqual(q["extension"], rows[0]["extension"])
                self.assertEqual(original, json.dumps(events))
                self.assertEqual(raw, (self.root / "requests.jsonl").read_bytes())

    def test_ReadAndFold_ReservedMarkers_ExplicitSchemaRefusal(self) -> None:
        cases = [response(kind="coordination-v2", schemaVersion=2),
                 response(kind="request-add"), response(kind=None), response(kind=[]),
                 response(schemaVersion=None), response(schemaVersion=True),
                 response(schemaVersion="1"), response(schemaVersion=1.0),
                 response(eventType="future-event")]
        for field in ("kind", "schemaVersion", "eventType"):
            event = response()
            del event[field]
            cases.append(event)
        future = response(kind="coordination-v2")
        del future["schemaVersion"]
        cases.append(future)
        for event in cases:
            with self.subTest(envelope=event):
                self.write_events([question(), event])
                raw = (self.root / "requests.jsonl").read_bytes()
                events, errors = coord.read_request_events(self.root)
                self.assertEqual([question()], events)
                self.assertEqual(1, len(errors))
                self.assertIn("XH.SCHEMA_INVALID", errors[0])
                for enhanced in (False, True):
                    with self.assertRaises(coord.CoordError) as raised:
                        coord.fold_requests([question(), event], enhanced=enhanced)
                    self.assertEqual("XH.SCHEMA_INVALID", raised.exception.code)
                self.assertEqual(raw, (self.root / "requests.jsonl").read_bytes())

    def test_ReadAndFold_UnversionedLegacyExtensions_StayTolerant(self) -> None:
        q = question()
        q.update(eventType="response-recorded", extension={"nested": [True, 1, 1.0]})
        unknown = {"kind": "legacy-extension", "eventType": "future-event", "payload": []}
        self.write_events([q, unknown])
        raw = (self.root / "requests.jsonl").read_bytes()
        events, errors = coord.read_request_events(self.root)
        self.assertEqual([q, unknown], events)
        self.assertEqual([], errors)
        rows = coord.fold_requests(events)
        self.assertEqual([dict(q, status="open")], rows)
        self.assertEqual(raw, (self.root / "requests.jsonl").read_bytes())

    def test_Append_ReservedMarkers_RejectBeforeFilesystemEffects(self) -> None:
        for index, event in enumerate((
                {"kind": "coordination-v2"}, {"schemaVersion": None},
                {"kind": "request-add", "schemaVersion": 2}, {"kind": "coordination-v1"})):
            with self.subTest(event=event):
                parent = self.root / ("absent-" + str(index))
                with self.assertRaises(coord.CoordError) as raised:
                    coord.append_record(parent / "requests.jsonl", event)
                self.assertEqual("XH.ENHANCED_DISABLED", raised.exception.code)
                self.assertFalse(parent.exists())

    def test_Fold_EachCorrelationMismatch_RetainsReplyWithoutAnswering(self) -> None:
        for field in ("repositoryId", "streamId", "threadId", "obligationId", "causationId"):
            r = response(**{field: "unrelated"})
            for events in itertools.permutations([question(), r]):
                with self.subTest(field=field, order=[e["kind"] for e in events]):
                    original = json.dumps(events)
                    row = self.enhanced(events)[0]
                    self.assertEqual([r], row["responses"])
                    self.assertEqual([{"eventId": "r", "code": "XH.CORRELATION_MISMATCH"}],
                                     row["response_errors"])
                    self.assertTrue(row["unanswered"])
                    self.assertTrue(row["remaining"])
                    self.assertFalse(row["accepted"])
                    self.assertFalse(row["execution_eligible"])
                    self.assertIsNone(row["latest_disposition"])
                    self.assertIsNone(row["next"])
                    self.assertEqual(original, json.dumps(events))

    def collaborate(self, action: str, as_json: bool) -> subprocess.CompletedProcess:
        env = {k: v for k, v in os.environ.items() if not k.startswith("GIT_")}
        env.update(COORD_ROOT=str(self.root), AGENT_SESSION="synthetic-reader",
                   AGENT_NAME="synthetic", PYTHONIOENCODING="utf-8")
        return subprocess.run(
            [sys.executable, str(SCRIPTS / "coord-core.py"), "collaborate", action]
            + (["--json"] if as_json else []),
            cwd=self.root, env=env, capture_output=True, text=True, encoding="utf-8", timeout=30)

    def test_Cli_CollaborateConflictingAdds_ExplicitFailureWithoutPartialState(self) -> None:
        other = question()
        other["contract"] = "different"
        self.write_events([question(), other])
        raw = (self.root / "requests.jsonl").read_bytes()
        for action, as_json in itertools.product(("summary", "check"), (True, False)):
            with self.subTest(action=action, as_json=as_json):
                proc = self.collaborate(action, as_json)
                self.assertEqual(4, proc.returncode, proc.stdout + proc.stderr)
                self.assertNotIn("Traceback", proc.stderr)
                self.assertNotIn("check: OK", proc.stdout)
                if as_json:
                    payload = json.loads(proc.stdout)
                    self.assertEqual("not-checked", payload["status"])
                    self.assertEqual("XH.EVENT_CONFLICT", payload["code"])
                    self.assertEqual(["XH.EVENT_CONFLICT"], payload["request_errors"])
                    self.assertGreaterEqual(payload["duration_seconds"], 0)
                    self.assertTrue({"requests", "findings", "active_sessions"}.isdisjoint(payload))
                else:
                    self.assertEqual("", proc.stdout)
                    self.assertIn("XH.EVENT_CONFLICT", proc.stderr)
                self.assertEqual(raw, (self.root / "requests.jsonl").read_bytes())

    def test_Cli_CollaborateUnsupportedEnvelope_ExplicitFailureWithoutPartialState(self) -> None:
        self.write_events([question(), response(kind="coordination-v2", schemaVersion=2)])
        raw = (self.root / "requests.jsonl").read_bytes()
        for action, as_json in itertools.product(("summary", "check"), (True, False)):
            with self.subTest(action=action, as_json=as_json):
                proc = self.collaborate(action, as_json)
                self.assertEqual(4, proc.returncode, proc.stdout + proc.stderr)
                self.assertNotIn("Traceback", proc.stderr)
                self.assertNotIn("check: OK", proc.stdout)
                if as_json:
                    payload = json.loads(proc.stdout)
                    self.assertEqual("not-checked", payload["status"])
                    self.assertEqual("COORD-REQUEST-NOT-CHECKED", payload["code"])
                    self.assertEqual(1, len(payload["request_errors"]))
                    self.assertIn("XH.SCHEMA_INVALID", payload["request_errors"][0])
                    self.assertGreaterEqual(payload["duration_seconds"], 0)
                    self.assertTrue({"requests", "findings", "active_sessions"}.isdisjoint(payload))
                else:
                    self.assertEqual("", proc.stdout)
                    self.assertIn("XH.SCHEMA_INVALID", proc.stderr)
                self.assertEqual(raw, (self.root / "requests.jsonl").read_bytes())

    def test_Read_MalformedObjects_ReportsInsteadOfSorting(self) -> None:
        for raw in ('null\n', '[]\n', '1\n', '{"kind":"request-add","id":[]}\n',
                    '{"kind":"request-add","id":"q","at":{}}\n',
                    '{"id":"q","id":"replacement"}\n', '{"at":NaN}\n'):
            with self.subTest(raw=raw):
                (self.root / "requests.jsonl").write_text(raw, encoding="utf-8")
                events, errors = coord.read_request_events(self.root)
                self.assertEqual([], events)
                self.assertEqual(1, len(errors))

    def test_Append_ShortWrite_FailsWithoutRepairClaim(self) -> None:
        original = coord.os.write
        with patch.object(coord.os, "write", side_effect=lambda fd, data: original(fd, data[:1])):
            with self.assertRaises(coord.CoordError) as raised:
                coord.append_record(self.root / "requests.jsonl", question())
        self.assertEqual("COORD-SHORT-WRITE", raised.exception.code)
        self.assertEqual(1, (self.root / "requests.jsonl").stat().st_size)

    def test_Append_DiskFailure_Propagates(self) -> None:
        with patch.object(coord.os, "write", side_effect=OSError("synthetic disk full")):
            with self.assertRaises(OSError):
                coord.append_record(self.root / "requests.jsonl", question())

    def test_Read_InvalidUtf8_ReportsAndKeepsNextRecord(self) -> None:
        (self.root / "requests.jsonl").write_bytes(b"\xff\n" + json.dumps(question()).encode() + b"\n")
        events, errors = coord.read_request_events(self.root)
        self.assertEqual([question()], events)
        self.assertEqual(1, len(errors))

    def test_Fold_CorrelatedReply_AnswersWithoutAcceptance(self) -> None:
        q, r = question(), response()
        before = copy.deepcopy([q, r])
        row = self.enhanced([q, r])[0]
        self.assertFalse(row["unanswered"])
        self.assertFalse(row["accepted"])
        self.assertFalse(row["execution_eligible"])
        self.assertEqual("changes-requested", row["latest_disposition"])
        self.assertEqual(r, row["responses"][0])
        self.assertEqual(before, [q, r])

    def test_Fold_DuplicatePermutedResponses_OneSemanticAnswer(self) -> None:
        for events in itertools.permutations([question(), response(), response()]):
            with self.subTest(order=[e.get("kind") for e in events]):
                row = self.enhanced(events)[0]
                self.assertFalse(row["unanswered"])
                self.assertEqual(1, len(row["responses"]))

    def test_Fold_UnknownOldGeneration_DoesNotApply(self) -> None:
        for generations, code in (({}, "XH.GENERATION_UNKNOWN"),
                                  ({"asker": "a1", "reviewer": "g2"},
                                   "XH.GENERATION_MISMATCH")):
            with self.subTest(generations=generations):
                row = self.enhanced([question(), response()], generations=generations)[0]
                self.assertTrue(row["unanswered"])
                self.assertEqual(code, row["response_errors"][0]["code"])

    def test_Fold_StaleProposal_PreservesHistoricalResponse(self) -> None:
        r = response(proposal={"id": "p", "revision": "0", "sha256": "b" * 64})
        row = self.enhanced([question(), r])[0]
        self.assertTrue(row["unanswered"])
        self.assertEqual(r, row["responses"][0])
        self.assertEqual("XH.REVISION_STALE", row["response_errors"][0]["code"])

    def test_Fold_SupersedingResponse_UsesCausalLatestNotClock(self) -> None:
        r = response()
        r2 = response(eventId="r2", producerSeq=2, producerAt=0,
                      supersedes="r", disposition="deferred",
                      payload={"text": "later", "nextActor": "reviewer",
                               "checkpoint": "after review"})
        for events in itertools.permutations([question(), r2, r]):
            with self.subTest(order=[e.get("eventId") for e in events]):
                row = self.enhanced(events)[0]
                self.assertFalse(row["unanswered"])
                self.assertTrue(row["remaining"])
                self.assertEqual("deferred", row["latest_disposition"])
                self.assertEqual(r2["payload"], row["next"])

    def test_Read_SameEventKeyChangedPayload_ExplicitConflict(self) -> None:
        self.write_events([question(), response(), response(payload={"text": "different"})])
        events, errors = coord.read_request_events(self.root)
        self.assertTrue(any("XH.EVENT_CONFLICT" in e for e in errors))
        self.assertEqual(2, len(events))
        self.assertEqual(3, len((self.root / "requests.jsonl").read_text().splitlines()))

    def test_Read_EnvelopeValidation_RejectsInvalidSchema(self) -> None:
        for changes in ({"schemaVersion": 2}, {"producerSeq": True},
                        {"disposition": "accepted"}, {"eventType": "run-started"},
                        {"recipient": None}, {"payload": []}, {"authorityRefs": None},
                        {"disposition": "deferred"}, {"digestVersion": 2}):
            with self.subTest(changes=changes):
                self.write_events([response(**changes)])
                events, errors = coord.read_request_events(self.root)
                self.assertEqual([], events)
                self.assertTrue(errors)

    def test_Read_DigestMutation_Refuses(self) -> None:
        r = response()
        r["payload"]["text"] = "changed after hash"
        self.write_events([r])
        events, errors = coord.read_request_events(self.root)
        self.assertEqual([], events)
        self.assertTrue(any("XH.DIGEST_MISMATCH" in e for e in errors))

    def test_Fold_ConcurrentResponses_RefusesAmbiguousLatest(self) -> None:
        row = self.enhanced([question(), response(), response(eventId="r2")])[0]
        self.assertTrue(row["unanswered"])
        self.assertEqual("XH.RESPONSE_AMBIGUOUS", row["response_errors"][0]["code"])
        self.assertEqual(2, len(row["responses"]))

    def test_Append_ConcurrentEnhancedAttempts_AllRefused(self) -> None:
        def attempt(_: int) -> str:
            try:
                coord.append_record(self.root / "requests.jsonl", response())
            except coord.CoordError as exc:
                return exc.code
            return "unexpected-success"
        with ThreadPoolExecutor(max_workers=4) as pool:
            results = list(pool.map(attempt, range(8)))
        self.assertEqual(["XH.ENHANCED_DISABLED"] * 8, results)
        self.assertFalse((self.root / "requests.jsonl").exists())

    def test_Append_EnhancedSyntheticVerifier_ZeroEffects(self) -> None:
        r = response(authorityRefs=[{"verified": True, "issuer": "synthetic-human"}])
        with patch.object(coord.os, "open") as opened, patch.object(coord.os, "write"), \
                patch.object(coord.os, "close"), \
                patch.object(coord.subprocess, "run") as run:
            with self.assertRaises(coord.CoordError) as raised:
                coord.append_record(self.root / "requests.jsonl", r)
        self.assertEqual("XH.ENHANCED_DISABLED", raised.exception.code)
        opened.assert_not_called()
        run.assert_not_called()
        self.assertFalse((self.root / "requests.jsonl").exists())

    def test_Cli_AllStatusById_ReadsHistoryWithoutAuthority(self) -> None:
        self.write_events([question(), response()])
        out = io.StringIO()
        with patch.object(coord.os, "getcwd", return_value=str(self.root)), \
                patch.dict(os.environ, {"COORD_ROOT": str(self.root)}), \
                contextlib.redirect_stdout(out):
            result = coord.main(["request", "list", "--status", "all", "--id", "q",
                                 "--actionable", "--json"])
        payload = json.loads(out.getvalue())
        self.assertEqual(0, result)
        self.assertEqual(response(), payload["requests"][0]["responses"][0])
        self.assertTrue(payload["requests"][0]["unanswered"])
        self.assertFalse(payload["requests"][0]["execution_eligible"])
        self.assertGreaterEqual(payload["duration_seconds"], 0)

    def test_Cli_RoutingCorrection_ClosesTypoOnlyWithoutAcknowledgement(self) -> None:
        old = question()
        old.update(id="delivery-typo", to="reviewre",
                   recipient={"session": "reviewre", "generation": "g1"})
        corrected = question()
        corrected.update(id="delivery-corrected", at=3)
        closure = {"kind": "request-resolve", "id": old["id"], "at": 2,
                   "resolution": "SUPERSEDED ROUTING ERROR"}
        self.write_events([old, closure, corrected])
        original = (self.root / "requests.jsonl").read_bytes()

        for status in ("all", "open"):
            with self.subTest(status=status):
                output = io.StringIO()
                with patch.object(coord.os, "getcwd", return_value=str(self.root)), \
                        patch.dict(os.environ, {"COORD_ROOT": str(self.root)}), \
                        contextlib.redirect_stdout(output):
                    result = coord.main(["request", "list", "--status", status,
                                         "--actionable", "--json"])
                self.assertEqual(0, result)
                payload = json.loads(output.getvalue())
                rows = {row["id"]: row for row in payload["requests"]}
                expected_ids = {old["id"], corrected["id"]} if status == "all" else {corrected["id"]}
                self.assertEqual(expected_ids, set(rows))
                current = rows[corrected["id"]]
                self.assertEqual("reviewer", current["to"])
                self.assertEqual("open", current["status"])
                self.assertTrue(current["unanswered"])
                self.assertTrue(current["remaining"])
                for row in rows.values():
                    self.assertFalse(row["accepted"])
                    self.assertEqual([], row["acceptances"])
                    self.assertEqual([], row["consumptions"])
                    self.assertFalse(any(row[k] for k in (
                        "execution_eligible", "ownership_granted", "run_granted",
                        "transfer_granted", "start_granted")))
                if status == "all":
                    historical = rows[old["id"]]
                    self.assertEqual("reviewre", historical["to"])
                    self.assertEqual(old["recipient"], historical["recipient"])
                    self.assertEqual("resolved", historical["status"])
                    self.assertEqual(closure["resolution"], historical["resolution"])
                self.assertEqual(original, (self.root / "requests.jsonl").read_bytes())

    def test_Cli_PinnedUnenrolledLegacy_ActualLinkedTreeAndRollback(self) -> None:
        primary, old, upgraded = (self.root / p for p in ("primary", "old", "upgraded"))
        primary.mkdir()
        env = {k: v for k, v in os.environ.items()
               if k != "COORD_ROOT" and not k.startswith("GIT_")}
        env.update(AGENT_SESSION="synthetic-old", AGENT_NAME="synthetic", PYTHONIOENCODING="utf-8")

        def run(cwd: Path, *argv: str) -> str:
            proc = subprocess.run(argv, cwd=cwd, env=env, capture_output=True,
                                  text=True, encoding="utf-8", timeout=30)
            self.assertEqual(0, proc.returncode, proc.stdout + proc.stderr)
            return proc.stdout

        run(primary, "git", "init", "--quiet")
        run(primary, "git", "-c", "user.name=Synthetic", "-c", "user.email=synthetic@example.invalid",
            "commit", "--quiet", "--allow-empty", "-m", "synthetic baseline")
        run(primary, "git", "worktree", "add", "--quiet", "-b", "old", str(old))
        run(primary, "git", "worktree", "add", "--quiet", "-b", "upgraded", str(upgraded))
        pinned = {}
        for name in SUPPORT:
            pinned[name] = subprocess.check_output(
                ["git", "show", BASELINE + ":docs/ai-forward-pack/scripts/" + name],
                cwd=REPO, env=env)
            (old / name).write_bytes(pinned[name])
            (upgraded / name).write_bytes((SCRIPTS / name).read_bytes())
        (upgraded / "coord_protocol.py").write_bytes((SCRIPTS / "coord_protocol.py").read_bytes())
        self.assertEqual(BASE_SHA, hashlib.sha256((old / "coord-core.py").read_bytes()).hexdigest())
        self.assertEqual(primary, coord.repo_root(old))
        self.assertEqual(primary, coord.repo_root(upgraded))
        env["AGENT_SESSION"] = "synthetic-registered"
        run(upgraded, sys.executable, str(upgraded / "coord-core.py"), "session", "start")
        env["AGENT_SESSION"] = "synthetic-old"
        originals = []
        for phase in ("enhanced-disabled", "rollback"):
            with self.subTest(phase=phase):
                request_id = json.loads(run(old, sys.executable, str(old / "coord-core.py"),
                    "request", "add", "--to", "core", "--contract", phase,
                    "--reason", "synthetic legacy"))["id"]
                run(old, sys.executable, str(old / "coord-core.py"), "request", "resolve",
                    request_id, "--resolution", "unchanged legacy answer")
                old_rows = json.loads(run(old, sys.executable, str(old / "coord-core.py"),
                    "request", "list", "--status", "all", "--json"))["requests"]
                new_rows = json.loads(run(upgraded, sys.executable, str(upgraded / "coord-core.py"),
                    "request", "list", "--status", "all", "--json"))["requests"]
                self.assertEqual(old_rows, new_rows)
                self.assertEqual("resolved", old_rows[-1]["status"])
                log = primary / ".agents" / "requests.jsonl"
                records = [json.loads(line) for line in log.read_text().splitlines()]
                self.assertEqual(originals, records[:len(originals)])
                self.assertEqual(request_id, records[-2]["id"])
                self.assertEqual(phase, records[-2]["contract"])
                originals = records
                for name, content in pinned.items():
                    (upgraded / name).write_bytes(content)
        session_events = list((primary / ".agents" / "log").glob("*.jsonl"))
        self.assertFalse(any(p.name == "synthetic-old.jsonl" for p in session_events))
        self.assertEqual(pinned["coord-core.py"], (upgraded / "coord-core.py").read_bytes())


if __name__ == "__main__":
    if len(sys.argv) == 3 and sys.argv[1] in ("--receipt", "--baseline-receipt"):
        receipt = Path(sys.argv[2]).resolve()
        receipt.relative_to(REPO)
        baseline_root = None
        if sys.argv[1] == "--baseline-receipt":
            baseline_root = REPO / ".agents" / ("xh-p1-baseline-" + uuid.uuid4().hex)
            baseline_root.mkdir()
            env = {k: v for k, v in os.environ.items() if not k.startswith("GIT_")}
            for name in SUPPORT:
                (baseline_root / name).write_bytes(subprocess.check_output(
                    ["git", "show", BASELINE + ":docs/ai-forward-pack/scripts/" + name],
                    cwd=REPO, env=env))
            baseline_spec = importlib.util.spec_from_file_location(
                "coord_baseline", baseline_root / "coord-core.py")
            coord = importlib.util.module_from_spec(baseline_spec)
            baseline_spec.loader.exec_module(coord)
        stream = io.StringIO()
        try:
            result = unittest.TextTestRunner(stream=stream, verbosity=2).run(
                unittest.defaultTestLoader.loadTestsFromTestCase(ResponseTests))
        finally:
            if baseline_root:
                remove_fixture(baseline_root)
        receipt.write_text(stream.getvalue(), encoding="utf-8")
        for test, error in result.failures + result.errors:
            print(test.id().split(" ")[0] + ": " + error.splitlines()[-1])
        print("tests={} failures={} errors={} receipt={}".format(
            result.testsRun, len(result.failures), len(result.errors), receipt))
        sys.exit(0 if result.wasSuccessful() and result.testsRun else 1)
    unittest.main(verbosity=2)
