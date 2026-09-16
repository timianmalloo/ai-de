"""C02 synthetic policy controls; no production verifier or endpoint qualification."""
import copy
import hashlib
from dataclasses import replace
import io
import itertools
import json
import os
import subprocess
import sys
import types
import unittest
import uuid
from unittest.mock import patch

from test_coord_responses import coord, digest, question, response, remove_fixture, REPO, SCRIPTS


class ProtocolTests(unittest.TestCase):
    def setUp(self) -> None:
        self.root = REPO / ".agents" / "xh-p1-protocol" / uuid.uuid4().hex
        self.root.mkdir(parents=True)
        self.addCleanup(remove_fixture, self.root)
        self.env = {k: v for k, v in os.environ.items() if not k.startswith("GIT_")}
        self.git("init", "--quiet")
        (self.root / "proposal.md").write_bytes(b"# Decision\nsynthetic one\n")
        self.git("add", "proposal.md")
        self.git("-c", "user.name=Synthetic", "-c", "user.email=synthetic@example.invalid",
                 "commit", "--quiet", "-m", "synthetic")
        self.ref = self.reference()
        self.peers = (("peer-a", "g1"), ("peer-b", "g1"))
        self.q = question()
        self.q["proposal"] = self.ref
        self.generations = {"asker": "a1", "reviewer": "g1", **dict(self.peers)}
        self.denied = set()
        self.issuer = True
        self.revoked = False
        self.calls = []

    def git(self, *args: str) -> str:
        proc = subprocess.run(["git", *args], cwd=self.root, env=self.env,
                              capture_output=True, timeout=30)
        self.assertEqual(0, proc.returncode, proc.stderr.decode(errors="replace"))
        return proc.stdout.decode().strip()

    def reference(self) -> dict:
        return {"id": "p", "revision": self.git("rev-parse", "HEAD"),
                "repositoryIdentity": "repo", "fullCommitId": self.git("rev-parse", "HEAD"),
                "repositoryRelativePath": "proposal.md", "gitObjectFormat": "sha1",
                "fullBlobId": self.git("rev-parse", "HEAD:proposal.md"),
                "sha256": hashlib.sha256((self.root / "proposal.md").read_bytes()).hexdigest(),
                "sectionOrDecisionId": "# Decision"}

    def event(self, kind="obligation-created", eid="published", proposal=None, **changes) -> dict:
        reference = copy.deepcopy(proposal or self.ref)
        authority = {k: v for k, v in reference.items() if k not in ("id", "revision")}
        authority.update(scope="proposal:p", issuerEvidenceRef="synthetic-decision",
                         verifierReceiptRef="synthetic-policy-result")
        e = {"kind": "coordination-v1", "schemaVersion": 1, "digestVersion": 1,
             "eventType": kind, "eventId": eid, "repositoryId": "repo", "streamId": "stream",
             "threadId": "q", "obligationId": "q", "inReplyTo": "q", "causationId": "q",
             "sender": {"session": "asker", "generation": "a1"},
             "recipient": {"session": "reviewer", "generation": "g1"},
             "proposal": reference, "authorityRefs": [authority], "producerSeq": 1,
             "producerAt": 2, "recordedAt": 3, "disposition": None, "supersedes": None,
             "payload": {"requiredPeers": [dict(session=s, generation=g) for s, g in self.peers]}}
        e.update(changes)
        e["payloadDigest"] = digest(e)
        return e

    def acceptance(self, peer: str, proposal=None, eid=None) -> dict:
        return self.event("proposal-accepted", eid or peer, proposal,
                          sender={"session": peer, "generation": "g1"}, payload={})

    def context(self):
        if not hasattr(coord, "_protocol"):
            return None
        p = coord._protocol()

        def verify(event, action):
            self.calls.append(event["eventId"])
            subject = (event["sender"]["session"], event["sender"]["generation"])
            allowed = (subject == ("asker", "a1") if action in
                       ("obligation-created", "proposal-superseded") else
                       subject in self.peers or subject == ("reviewer", "g1") or
                       (action == "recipient-consumed" and subject == ("asker", "a1")))
            return p.Verification(event["payloadDigest"], action, subject, "repo",
                                  self.issuer, allowed and event["eventId"] not in self.denied,
                                  not self.revoked, self.peers)
        return p.TrustedContext("repo", self.root, tuple(self.generations.items()), verify)

    def folded(self, facts, trusted=True):
        kwargs = {"enhanced": True, "generations": self.generations}
        context = self.context() if trusted else None
        if context is not None:
            kwargs["trusted_context"] = context
        try:
            return coord.fold_requests([self.q, *facts], **kwargs)[0]
        except (coord.CoordError, ValueError, TypeError) as exc:
            self.fail("official fold rejected contract: " + str(exc))

    def read(self, raw: bytes):
        (self.root / "requests.jsonl").write_bytes(raw)
        return coord.read_request_events(self.root)

    def test_Fold_AllRequiredPeers_AcceptsWithoutRightsOrResponse(self):
        facts = [self.event(), self.acceptance("peer-a"), self.acceptance("peer-b")]
        for order in itertools.permutations(facts):
            with self.subTest(order=[e["eventId"] for e in order]):
                row = self.folded([*order, order[-1]])
                self.assertTrue(row["accepted"])
                self.assertTrue(row["unanswered"])
                self.assertEqual(self.ref, row["current_proposal"])
                self.assertEqual(2, len(row["acceptances"]))
                self.assertEqual(3, len(row["protocol_facts"]))
                self.assertFalse(any(row[k] for k in
                                     ("execution_eligible", "ownership_granted", "run_granted",
                                      "transfer_granted", "start_granted")))
        self.assertEqual(facts, [self.event(), self.acceptance("peer-a"), self.acceptance("peer-b")])

    def test_Fold_OnePeerDuplicateAndAck_NotAccepted(self):
        row = self.folded([self.event(), self.acceptance("peer-a"),
                           self.acceptance("peer-a", eid="again"),
                           {"kind": "request-resolve", "id": "q", "resolution": "ACK approved"}])
        self.assertFalse(row["accepted"])
        self.assertEqual(1, len(row["acceptances"]))

    def test_Fold_ProductionComposition_DeniesPerfectSyntheticClaims(self):
        facts = [self.event(), self.acceptance("peer-a"), self.acceptance("peer-b")]
        row = self.folded(facts, trusted=False)
        self.assertFalse(row["accepted"])
        self.assertEqual([], row["acceptances"])
        self.assertEqual({"XH.AUTHORITY_UNVERIFIED"}, {e["code"] for e in row["protocol_errors"]})
        self.assertEqual([], self.calls)

    def test_Fold_UnknownIssuerRevocationAndAuthorization_Deny(self):
        for option in ("issuer", "revoked", "denied"):
            with self.subTest(option=option):
                self.issuer, self.revoked, self.denied = True, False, set()
                if option == "issuer":
                    self.issuer = False
                elif option == "revoked":
                    self.revoked = True
                else:
                    self.denied = {"peer-b"}
                row = self.folded([self.event(), self.acceptance("peer-a"),
                                   self.acceptance("peer-b")])
                self.assertFalse(row["accepted"])
                self.assertTrue(row["protocol_errors"])

    def test_Fold_RequiredPeersNotSenderSelected_DenySubstitution(self):
        pub = self.event(payload={"requiredPeers": [{"session": "peer-a", "generation": "g1"}]})
        row = self.folded([pub, self.acceptance("peer-a")])
        self.assertFalse(row["accepted"])
        self.assertIn("XH.PROPOSAL_CONTRACT_MISMATCH", str(row["protocol_errors"]))

    def test_Fold_GenerationAndCorrelationMismatches_Deny(self):
        for field in ("repositoryId", "streamId", "threadId", "obligationId", "causationId",
                      "generation", "recipientGeneration"):
            with self.subTest(field=field):
                a = self.acceptance("peer-b")
                if field == "generation":
                    a["sender"]["generation"] = "g2"
                elif field == "recipientGeneration":
                    a["recipient"]["generation"] = "g2"
                else:
                    a[field] = "wrong"
                a["payloadDigest"] = digest(a)
                row = self.folded([self.event(), self.acceptance("peer-a"), a])
                self.assertFalse(row["accepted"])
                self.assertTrue(row["protocol_errors"])

    def test_Fold_IntegrityFullGitObjects_RejectsSubstitutions(self):
        for key, value in (("repositoryIdentity", "other"), ("fullCommitId", "a" * 40),
                           ("fullBlobId", self.ref["fullCommitId"]), ("sha256", "b" * 64),
                           ("sectionOrDecisionId", "# Absent"), ("gitObjectFormat", "sha256"),
                           ("repositoryRelativePath", "../proposal.md"),
                           ("repositoryRelativePath", "C:\\escape"),
                           ("repositoryRelativePath", ".git/config")):
            with self.subTest(key=key, value=value):
                wrong = dict(self.ref, **{key: value})
                row = self.folded([self.event(proposal=wrong), self.acceptance("peer-a", wrong),
                                   self.acceptance("peer-b", wrong)])
                self.assertFalse(row["accepted"])
                self.assertIn("XH.INTEGRITY_INVALID", str(row["protocol_errors"]))

    def revision(self):
        (self.root / "proposal.md").write_bytes(b"# Decision\nsynthetic two\n")
        self.git("add", "proposal.md")
        self.git("-c", "user.name=Synthetic", "-c", "user.email=synthetic@example.invalid",
                 "commit", "--quiet", "-m", "revision")
        return self.reference()

    def test_Fold_Supersession_OldAcceptanceHistoricalNotCurrent(self):
        new = self.revision()
        facts = [self.event(), self.event(eid="new", proposal=new),
                 self.event("proposal-superseded", "sup", new, supersedes=self.ref, payload={}),
                 self.acceptance("peer-a"), self.acceptance("peer-b")]
        row = self.folded(list(reversed(facts)))
        self.assertFalse(row["accepted"])
        self.assertEqual(new, row["current_proposal"])
        self.assertEqual(2, len(row["acceptances"]))
        row = self.folded(facts + [self.acceptance("peer-a", new, "new-a"),
                                  self.acceptance("peer-b", new, "new-b")])
        self.assertTrue(row["accepted"])

    def test_Fold_CycleCompetingSuccessorUnauthorizedReviser_NoClockWinner(self):
        new = self.revision()
        third = dict(new, revision="third")
        base = [self.event(), self.event(eid="new", proposal=new),
                self.event("proposal-superseded", "s1", new, supersedes=self.ref, payload={})]
        cases = [
            base + [self.event("proposal-superseded", "cycle", self.ref, supersedes=new, payload={})],
            base + [self.event(eid="third", proposal=third),
                    self.event("proposal-superseded", "fork", third, supersedes=self.ref, payload={})]]
        for facts in cases:
            with self.subTest(ids=[e["eventId"] for e in facts]):
                row = self.folded(facts)
                self.assertIsNone(row["current_proposal"])
                self.assertIn("XH.REVISION_AMBIGUOUS", str(row["protocol_errors"]))
        self.denied = {"s1"}
        row = self.folded(base)
        self.assertIsNone(row["current_proposal"])
        self.assertIn("XH.AUTHORITY_UNVERIFIED", str(row["protocol_errors"]))

    def consumed(self, target, **changes):
        return self.event("recipient-consumed", "consumed", sender=target["recipient"],
                          payload={"eventId": target["eventId"], "eventDigest": target["payloadDigest"],
                                   "checkpoint": "after-review"}, **changes)

    def test_Fold_ExactRecipientConsumption_NotAcceptanceOrUnderstanding(self):
        target = self.event()
        row = self.folded([self.consumed(target), target])
        self.assertEqual(1, len(row["consumptions"]))
        self.assertEqual("after-review", row["consumptions"][0]["payload"]["checkpoint"])
        self.assertFalse(row["accepted"])
        self.assertNotIn("understood", row)

    def test_Fold_ConsumptionWrongDigestAttesterRestart_Denied(self):
        for field in ("eventId", "eventDigest", "sender", "restart"):
            with self.subTest(field=field):
                target = self.event()
                c = self.consumed(target)
                if field == "sender":
                    c["sender"] = {"session": "peer-a", "generation": "g1"}
                elif field == "restart":
                    self.generations["reviewer"] = "g2"
                else:
                    c["payload"][field] = "wrong"
                c["payloadDigest"] = digest(c)
                row = self.folded([target, c])
                self.assertEqual([], row["consumptions"])
                self.assertTrue(row["protocol_errors"])

    def test_Read_DuplicateNonfiniteDepthAndReferenceBounds_Refuse(self):
        e = self.event()
        bad = [b'{"x":1,"x":2}', b'{"x":NaN}', b'{"x":Infinity}']
        deep = {}
        for _ in range(16):
            deep = {"x": deep}
        bad.append(json.dumps(deep).encode())
        e["authorityRefs"] *= 17
        e["payloadDigest"] = digest(e)
        bad.append(json.dumps(e).encode())
        for raw in bad:
            with self.subTest(raw=raw[:80]):
                events, errors = self.read(raw + b"\n")
                self.assertEqual([], events)
                self.assertEqual(1, len(errors))

    def test_Read_RecordLimitAndLimitPlusOne_PreservesNextRecord(self):
        exact = b'{"legacy":"' + b"x" * (65536 - len(b'{"legacy":""}')) + b'"}'
        events, errors = self.read(exact + b"\n")
        self.assertEqual(1, len(events))
        self.assertEqual([], errors)
        events, errors = self.read(exact[:-1] + b' }\n' + json.dumps(self.q).encode() + b"\n")
        self.assertEqual([self.q], events)
        self.assertEqual(1, len(errors))
        self.assertIn("XH.RECORD_TOO_LARGE", errors[0])

    def test_Fold_ArtifactLimitAndLimitPlusOne_BoundedGitRead(self):
        for size in (1048576, 1048577):
            with self.subTest(size=size):
                (self.root / "proposal.md").write_bytes(b"# Decision\n" + b"x" * (size - 11))
                self.git("add", "proposal.md")
                self.git("-c", "user.name=Synthetic", "-c", "user.email=synthetic@example.invalid",
                         "commit", "--quiet", "-m", str(size))
                ref = self.reference()
                row = self.folded([self.event(proposal=ref), self.acceptance("peer-a", ref),
                                   self.acceptance("peer-b", ref)])
                self.assertEqual(size == 1048576, row["accepted"])

    def test_Cli_ProductionClaimsEnvAndReceipt_CannotSelectSyntheticProvider(self):
        facts = [self.q, self.event(), self.acceptance("peer-a"), self.acceptance("peer-b")]
        raw = "".join(json.dumps(e) + "\n" for e in facts).encode()
        self.read(raw)
        env = dict(self.env, COORD_ROOT=str(self.root), AGENT_SESSION="synthetic",
                   AGENT_NAME="synthetic", PYTHONIOENCODING="utf-8",
                   XH_TRUSTED_VERIFIER="synthetic", XH_ACCEPTED="true")
        proc = subprocess.run([sys.executable, str(SCRIPTS / "coord-core.py"),
                               "request", "list", "--status", "all", "--actionable", "--json"],
                              cwd=self.root, env=env, capture_output=True, text=True, timeout=30)
        self.assertEqual(0, proc.returncode, proc.stdout + proc.stderr)
        row = json.loads(proc.stdout)["requests"][0]
        self.assertFalse(row["accepted"])
        self.assertEqual(3, len(row["protocol_facts"]))
        self.assertEqual(raw, (self.root / "requests.jsonl").read_bytes())
        self.assertEqual("disabled", json.loads(proc.stdout)["enhanced_writer"])
        self.assertFalse(any(row[k] for k in ("ownership_granted", "run_granted",
                                            "transfer_granted", "start_granted")))

    def test_Append_AllNewFacts_RefusedBeforeEffects(self):
        for kind in ("obligation-created", "proposal-superseded", "proposal-accepted",
                     "recipient-consumed"):
            with self.subTest(kind=kind):
                with self.assertRaises(coord.CoordError) as raised:
                    coord.append_record(self.root / "absent" / "ledger", self.event(kind))
                self.assertEqual("XH.ENHANCED_DISABLED", raised.exception.code)
                self.assertFalse((self.root / "absent").exists())

    def test_Fold_StandalonePublication_OfficialThreadExists(self):
        facts = [self.event(), self.acceptance("peer-a"), self.acceptance("peer-b")]
        try:
            rows = coord.fold_requests(facts, enhanced=True, trusted_context=self.context())
        except (TypeError, coord.CoordError) as exc:
            self.fail(str(exc))
        self.assertEqual(1, len(rows))
        self.assertTrue(rows[0]["accepted"])
        self.assertEqual([], coord.fold_requests(facts))

    def test_Fold_ResponseConsumption_DoesNotBecomeAcceptance(self):
        target = response(proposal=self.ref, disposition="answer")
        row = self.folded([self.event(), target, self.consumed(target)])
        self.assertFalse(row["unanswered"])
        self.assertEqual(1, len(row["consumptions"]))
        self.assertEqual([], row["acceptances"])
        self.assertFalse(row["accepted"])

    def test_Read_MalformedDiscriminatorFiniteOverflow_ExplicitRefusal(self):
        for value in ([], {}, 1, None):
            e = self.event(eventType=value)
            with self.subTest(value=value):
                try:
                    events, errors = self.read(json.dumps(e).encode() + b"\n")
                except Exception as exc:
                    self.fail("unstructured parser exception: " + repr(exc))
                self.assertEqual([], events)
                self.assertEqual(1, len(errors))
        events, errors = self.read(b'{"number":1e309}\n')
        self.assertEqual([], events)
        self.assertEqual(1, len(errors))

    def test_Read_DepthAndReferenceExactLimits_Accept(self):
        tree = {}
        for _ in range(15):
            tree = {"x": tree}
        events, errors = self.read(json.dumps(tree).encode() + b"\n")
        self.assertEqual([tree], events)
        self.assertEqual([], errors)
        e = self.event()
        e["authorityRefs"] *= 16
        e["payloadDigest"] = digest(e)
        events, errors = self.read(json.dumps(e).encode() + b"\n")
        self.assertEqual([e], events)
        self.assertEqual([], errors)

    def test_Fold_VerifierResultBinding_SubstitutionDenied(self):
        context = self.context()
        self.assertIsNotNone(context)
        for field, value in (("event_digest", "wrong"), ("action", "wrong"),
                             ("subject", ("other", "g1")), ("repository_id", "other"),
                             ("issuer_verified", 1), ("authorized", 1), ("current", 1)):
            with self.subTest(field=field):
                changed = replace(context, verify=lambda e, a: replace(context.verify(e, a),
                                                                       **{field: value}))
                row = coord.fold_requests([self.q, self.event(), self.acceptance("peer-a"),
                                           self.acceptance("peer-b")],
                                          enhanced=True, trusted_context=changed)[0]
                self.assertFalse(row["accepted"])
                self.assertIn("XH.AUTHORITY_UNVERIFIED", str(row["protocol_errors"]))

    def test_Fold_SourceKeyCollisions_AllFactConsumersDenied(self):
        new = dict(self.ref, revision="next")
        for field, kind, reverse in itertools.product(
                ("repositoryId", "streamId"),
                ("obligation-created", "proposal-superseded",
                 "proposal-accepted", "recipient-consumed"), (False, True)):
            with self.subTest(field=field, kind=kind, reverse=reverse):
                published = self.event()
                base = [published]
                if kind == "obligation-created":
                    valid = self.event(eid="shared")
                    forged = self.event(eid="shared", proposal=new)
                elif kind == "proposal-superseded":
                    base.append(self.event(eid="next", proposal=new))
                    valid = self.event(kind, "shared", new, supersedes=self.ref, payload={})
                    forged = self.event(kind, "shared", self.ref, supersedes=new, payload={})
                elif kind == "proposal-accepted":
                    valid = self.acceptance("peer-a", eid="shared")
                    forged = self.acceptance("peer-b", eid="shared")
                else:
                    target = response(proposal=self.ref)
                    foreign_target = response(proposal=self.ref, **{field: "foreign"})
                    base.extend((target, foreign_target))
                    valid = self.consumed(target, eventId="shared")
                    forged = self.consumed(foreign_target, eventId="shared")
                forged[field] = "foreign"
                forged["payloadDigest"] = digest(forged)
                facts = [*base, valid, forged]
                original = copy.deepcopy(facts)
                row = self.folded(list(reversed(facts)) if reverse else facts)
                self.assertEqual(new if kind == "proposal-superseded" else self.ref,
                                 row["current_proposal"])
                self.assertEqual([valid] if kind == "proposal-accepted" else [],
                                 row["acceptances"])
                self.assertEqual([valid] if kind == "recipient-consumed" else [],
                                 row["consumptions"])
                self.assertFalse(row["accepted"])
                self.assertEqual(original, facts)
                matching = [v for v in row["verification"] if v["eventId"] == "shared"
                            and v.get(field) == "foreign"]
                self.assertEqual(1, len(matching))
                self.assertEqual("denied", matching[0]["authorization"])

    def test_Fold_StandalonePoison_IsInertAndCannotReplaceTrustedAnchor(self):
        facts = [self.event(), self.acceptance("peer-a"), self.acceptance("peer-b")]
        for change, reverse in itertools.product(
                ({"repositoryId": "foreign"}, {"streamId": "foreign"},
                 {"sender": {"session": "peer-a", "generation": "g1"}},
                 {"payload": {"requiredPeers": [{"session": "peer-a", "generation": "g1"}]}}),
                (False, True)):
            with self.subTest(change=change, reverse=reverse):
                self.denied = {"000-foreign"} if "streamId" in change else set()
                poison = self.event(eid="000-foreign", proposal=dict(self.ref, revision="poison"),
                                    **change)
                events = [poison, *facts]
                rows = coord.fold_requests(list(reversed(events)) if reverse else events,
                                           enhanced=True, trusted_context=self.context())
                self.assertTrue(rows[0]["accepted"])
                self.assertEqual(self.ref, rows[0]["current_proposal"])
                self.assertEqual(self.ref, rows[0]["proposal"])
                self.assertEqual(self.q["sender"], rows[0]["sender"])
                self.assertEqual(4, sum(len(r["protocol_facts"]) for r in rows))
                self.assertTrue(any(r["protocol_errors"] for r in rows))
        rows = coord.fold_requests([self.event()], enhanced=True)
        self.assertEqual(1, len(rows[0]["protocol_facts"]))
        for field in ("sender", "recipient", "proposal", "at"):
            self.assertNotIn(field, rows[0])

    def test_Fold_StandaloneNamespaces_DoNotPoolPeersOrProposals(self):
        other = [self.event(), self.acceptance("peer-b")]
        for e in other:
            e["streamId"] = "second"
            e["payloadDigest"] = digest(e)
        facts = [self.event(), self.acceptance("peer-a"), *other]
        for order in (facts, list(reversed(facts))):
            rows = coord.fold_requests(order, enhanced=True, trusted_context=self.context())
            self.assertEqual(2, len(rows))
            self.assertFalse(any(r["accepted"] for r in rows))
            self.assertEqual({("repo", "stream", "q"), ("repo", "second", "q")},
                             {(r["repositoryId"], r["streamId"], r["id"]) for r in rows})
            self.assertEqual([2, 2], sorted(len(r["protocol_facts"]) for r in rows))
        other_vote = self.acceptance("peer-a")
        other_vote["streamId"] = "second"
        other_vote["payloadDigest"] = digest(other_vote)
        rows = coord.fold_requests([*facts, self.acceptance("peer-b"), other_vote],
                                   enhanced=True, trusted_context=self.context())
        self.assertTrue(all(r["accepted"] for r in rows))

    def test_Fold_StandaloneTrustedAnchor_PreservesResponseConsumption(self):
        target = response(proposal=self.ref, disposition="answer")
        rows = coord.fold_requests([self.event(), target, self.consumed(target)],
                                   enhanced=True, generations=self.generations,
                                   trusted_context=self.context())
        self.assertFalse(rows[0]["unanswered"])
        self.assertEqual(1, len(rows[0]["consumptions"]))
        self.assertFalse(rows[0]["accepted"])

    def test_Cli_MismatchedPublication_VisibleNegativeWithOrWithoutLegacy(self):
        for field, legacy in itertools.product(
                ("threadId", "obligationId", "causationId"), (False, True)):
            with self.subTest(field=field, legacy=legacy):
                fact = self.event(**{field: "other"})
                facts = [self.q, fact] if legacy else [fact]
                raw = b"".join(json.dumps(e).encode() + b"\n" for e in facts)
                parsed, errors = self.read(raw)
                self.assertEqual(facts, parsed)
                self.assertEqual([], errors)
                env = dict(self.env, COORD_ROOT=str(self.root), AGENT_SESSION="synthetic",
                           AGENT_NAME="synthetic", PYTHONIOENCODING="utf-8",
                           PYTHONDONTWRITEBYTECODE="1")
                proc = subprocess.run(
                    [sys.executable, "-B", str(SCRIPTS / "coord-core.py"),
                     "request", "list", "--status", "all", "--actionable", "--json"],
                    cwd=self.root, env=env, capture_output=True, text=True, timeout=30)
                self.assertEqual(0, proc.returncode, proc.stderr)
                payload = json.loads(proc.stdout)
                self.assertEqual("disabled", payload["enhanced_writer"])
                self.assertEqual(1, sum(len(r["protocol_facts"]) for r in payload["requests"]))
                row = payload["requests"][0]
                self.assertIn("XH.CORRELATION_MISMATCH", str(row["protocol_errors"]))
                self.assertEqual(1, len(row["verification"]))
                self.assertFalse(row["accepted"])
                self.assertIsNone(row["current_proposal"])
                self.assertEqual(raw, (self.root / "requests.jsonl").read_bytes())

    def test_Digest_GoldenBytes_PreservesTypesNullAndUnicode(self):
        value = {"z": None, "a": [True, 1, 1.0, "é"], "recordedAt": 42, "payloadDigest": "inert"}
        self.assertEqual(b'{"a":[true,1,1.0,"\xc3\xa9"],"z":null}', coord._response_bytes(value))
        self.assertNotEqual(coord._response_bytes({"value": None}), coord._response_bytes({}))


def mutation_receipt() -> int:
    """Finite guard mutations in memory; no source edits, no production composition."""
    source = (SCRIPTS / "coord_protocol.py").read_text(encoding="utf-8")
    cases = (
        ("issuer", "v.issuer_verified is True and", "True and",
         "test_Fold_UnknownIssuerRevocationAndAuthorization_Deny"),
        ("authorization", "v.authorized is True and", "True and",
         "test_Fold_UnknownIssuerRevocationAndAuthorization_Deny"),
        ("revocation", "v.current is True,", "True,",
         "test_Fold_UnknownIssuerRevocationAndAuthorization_Deny"),
        ("artifact-hash", "hashlib.sha256(data).hexdigest() == ref[\"sha256\"]", "True",
         "test_Fold_IntegrityFullGitObjects_RejectsSubstitutions"),
        ("required-peer-contract", "supplied != set(v.required_peers)", "False",
         "test_Fold_RequiredPeersNotSenderSelected_DenySubstitution"),
        ("generation", "generations[session] == generation", "True",
         "test_Fold_GenerationAndCorrelationMismatches_Deny"),
        ("consumption-digest", 'target["payloadDigest"] != payload["eventDigest"]', "False",
         "test_Fold_ConsumptionWrongDigestAttesterRestart_Denied"),
        ("all-peers", "all((current, peer) in accepted for peer in peers[current])",
         "any((current, peer) in accepted for peer in peers[current])",
         "test_Fold_OnePeerDuplicateAndAck_NotAccepted"),
        ("ambiguity", "if ambiguous:", "if False:",
         "test_Fold_CycleCompetingSuccessorUnauthorizedReviser_NoClockWinner"),
    )
    results = []
    for name, old, new, test in cases:
        if source.count(old) != 1:
            raise AssertionError("mutation anchor not unique: " + name)
        module = types.ModuleType("coord_protocol")
        with patch.dict(sys.modules, {"coord_protocol": module}):
            exec(compile(source.replace(old, new, 1), str(SCRIPTS / "coord_protocol.py"),
                         "exec"), module.__dict__)
            result = unittest.TextTestRunner(stream=io.StringIO()).run(ProtocolTests(test))
        results.append({"mutant": name, "tests": result.testsRun, "failures": len(result.failures),
                        "errors": len(result.errors),
                        "killed": bool(result.failures) and not result.errors})
    repairs = (
        ("checked-source-key", "protocol",
         (("checked[source_key(event)]", 'checked[event["eventId"]]'),
          ("checked.get(source_key(event))", 'checked.get(event["eventId"])'),
          ("source_key(event) not in checked", 'event["eventId"] not in checked')),
         "test_Fold_SourceKeyCollisions_AllFactConsumersDenied"),
        ("standalone-namespace", "core",
         (('key = (event["repositoryId"], event["streamId"], rid)', 'key = rid'),),
         "test_Fold_StandaloneNamespaces_DoNotPoolPeersOrProposals"),
        ("untrusted-anchor", "core",
         (('"contract": "proposal revision"})',
           '"contract": "proposal revision", "sender": event["sender"], '
           '"recipient": event["recipient"], "proposal": event["proposal"], '
           '"at": event["producerAt"]})'),),
         "test_Fold_StandalonePoison_IsInertAndCannotReplaceTrustedAnchor"),
        ("publication-bucket", "core",
         (('rid = event["inReplyTo"]', 'rid = event["threadId"]'),),
         "test_Cli_MismatchedPublication_VisibleNegativeWithOrWithoutLegacy"),
    )
    for name, owner, replacements, test in repairs:
        path = SCRIPTS / ("coord_protocol.py" if owner == "protocol" else "coord-core.py")
        mutated = path.read_text(encoding="utf-8")
        for old, new in replacements:
            if old not in mutated:
                raise AssertionError("missing repair mutation anchor: " + name)
            mutated = mutated.replace(old, new)
        if owner == "protocol":
            module = types.ModuleType("coord_protocol")
            with patch.dict(sys.modules, {"coord_protocol": module}):
                exec(compile(mutated, str(path), "exec"), module.__dict__)
                result = unittest.TextTestRunner(stream=io.StringIO()).run(ProtocolTests(test))
        else:
            module = types.ModuleType("coord_repair_mutant")
            module.__file__ = str(path)
            exec(compile(mutated, str(path), "exec"), module.__dict__)
            # CLI subprocesses must run the mutated official script in an isolated fixture.
            fixture = ProtocolTests()
            fixture.setUp()
            try:
                scripts = fixture.root / "scripts"
                scripts.mkdir()
                for helper in ("coord_ids.py", "repo_identity.py", "coord_protocol.py"):
                    (scripts / helper).write_bytes((SCRIPTS / helper).read_bytes())
                (scripts / "coord-core.py").write_text(mutated, encoding="utf-8")
                with patch.dict(globals(), coord=module, SCRIPTS=scripts):
                    result = unittest.TextTestRunner(stream=io.StringIO()).run(ProtocolTests(test))
            finally:
                fixture.doCleanups()
        results.append({"mutant": name, "tests": result.testsRun, "failures": len(result.failures),
                        "errors": len(result.errors),
                        "killed": bool(result.failures) and not result.errors})
    print(json.dumps(results))
    return 0 if all(r["killed"] for r in results) else 1


if __name__ == "__main__":
    if sys.argv[1:] == ["--mutations"]:
        sys.exit(mutation_receipt())
    unittest.main()
