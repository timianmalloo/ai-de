"""Synthetic real-CLI lease controls; never invoke a live metadata root."""
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import unittest
import uuid

from test_coord_responses import REPO, SCRIPTS, SUPPORT, coord, remove_fixture


class ClaimClassTests(unittest.TestCase):
    def setUp(self) -> None:
        self.fixture = REPO / ".agents" / "xh-claim-fixtures" / uuid.uuid4().hex
        self.primary = self.fixture / "primary"
        self.primary.mkdir(parents=True)
        self.addCleanup(remove_fixture, self.fixture)
        self.env = {k: v for k, v in os.environ.items()
                    if not k.startswith(("GIT_", "AIDE_", "AGENT_", "COORD_"))}
        self.env.update(AGENT_SESSION="synthetic-claim-worker",
                        AGENT_NAME="synthetic-display", PYTHONIOENCODING="utf8")
        self.git(self.primary, "init", "--quiet")
        self.git(self.primary, "-c", "user.name=Synthetic",
                 "-c", "user.email=synthetic@example.invalid",
                 "commit", "--allow-empty", "--quiet", "-m", "fixture")
        self.metadata = self.primary / ".agents"
        self.metadata.mkdir()
        self.registry = self.metadata / "artifacts.yml"
        self.registry.write_text(
            "docs/audit/audit-data.js: derived python regenerate.py\n"
            "docs/api/*.md: derived python regenerate.py\n"
            "docs/audit/audit-log.jsonl: register\n"
            "site/*.html: authored\n", encoding="utf-8")
        scripts = self.primary / "scripts"
        scripts.mkdir()
        for name in SUPPORT:
            shutil.copy2(SCRIPTS / name, scripts / name)
        self.cli = scripts / "coord-core.py"

    def git(self, cwd: Path, *args: str) -> str:
        result = subprocess.run(["git", *args], cwd=cwd, env=self.env,
                                capture_output=True, text=True, timeout=30)
        self.assertEqual(0, result.returncode, result.stderr)
        return result.stdout.strip()

    def invoke(self, *args: str, cwd: Path | None = None) -> subprocess.CompletedProcess:
        working = cwd or self.primary
        root, error = coord.resolve_root(working, None)
        self.assertIsNone(error)
        self.assertEqual(self.metadata.resolve(), Path(root).resolve())
        self.assertTrue(Path(root).resolve().is_relative_to(self.fixture.resolve()))
        self.assertTrue(self.cli.resolve().is_relative_to(self.fixture.resolve()))
        return subprocess.run([sys.executable, str(self.cli.resolve()), *args],
                              cwd=working, env=self.env, capture_output=True,
                              text=True, encoding="utf-8", timeout=30)

    def claims(self) -> list[dict]:
        return [event for path in (self.metadata / "log").glob("*.jsonl")
                for line in path.read_text(encoding="utf-8").splitlines()
                if line.strip() for event in [json.loads(line)]
                if event["kind"] == "claim"]

    def claim(self, path: str, *args: str, cwd: Path | None = None):
        return self.invoke("claim", "--path", path, "--wi", "synthetic",
                           *args, cwd=cwd)

    def assert_denied(self, path: str, code: str, status: int = 3, **kwargs) -> None:
        before = self.claims()
        result = self.claim(path, **kwargs)
        self.assertEqual(status, result.returncode, result.stdout + result.stderr)
        self.assertIn(code, result.stderr)
        self.assertEqual(before, self.claims())

    def test_claim_derived_spellings_refused_without_lease(self) -> None:
        for path in ("docs/audit/audit-data.js", r"docs\audit\audit-data.js",
                     "./docs/audit/audit-data.js", r".\docs\audit\audit-data.js",
                     "././docs/audit/audit-data.js", "docs/./audit/audit-data.js",
                     "docs/api/service.md", r".\docs\api\service.md"):
            with self.subTest(path=path):
                self.assert_denied(path, "COORD-CLAIM-DERIVED-CLASS")

    def test_claim_register_refuses_with_existing_code(self) -> None:
        self.assert_denied("docs/audit/audit-log.jsonl", "COORD-CLAIM-REGISTER-CLASS")

    def test_claim_authored_html_and_markdown_preserve_identity_and_ttl(self) -> None:
        for path in ("site/index.html", "authored.md"):
            with self.subTest(path=path):
                result = self.claim(path, "--ttl", "300")
                self.assertEqual(0, result.returncode, result.stderr)
                event = self.claims()[-1]
                self.assertEqual((path, "synthetic-claim-worker", "synthetic-display", 300),
                                 (event["path"], event["session"], event["agent"], event["ttl"]))

    def test_claim_ttl_cap_and_override_preserved(self) -> None:
        result = self.claim("capped.md", "--ttl", "901")
        self.assertEqual(3, result.returncode, result.stderr)
        self.assertIn("COORD-CLAIM-TTL-CAP", result.stderr)
        self.assertEqual([], self.claims())
        result = self.claim("maximum.md", "--ttl", "900")
        self.assertEqual(0, result.returncode, result.stderr)
        result = self.claim("override.md", "--ttl", "901", "--long-edit", "synthetic")
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual([900, 901], [e["ttl"] for e in self.claims()])

    def test_claim_missing_identity_refused_without_lease(self) -> None:
        self.env.pop("AGENT_SESSION")
        self.assert_denied("authored.md", "NOT CHECKED", 4)

    def test_claim_empty_identity_refused_without_lease(self) -> None:
        self.env["AGENT_SESSION"] = ""
        self.assert_denied("authored.md", "NOT CHECKED", 4)

    def test_claim_malformed_registry_fails_closed(self) -> None:
        self.registry.write_text("not a registry entry\n", encoding="utf-8")
        self.assert_denied("docs/audit/audit-data.js", "COORD-CLASS-CONFLICT", 2)

    def test_claim_missing_registry_preserves_advisory_authored_behavior(self) -> None:
        self.registry.unlink()
        classification = self.invoke("class", "authored.md", "--json")
        self.assertEqual("COORD-CLASS-UNREGISTERED", json.loads(classification.stdout)["code"])
        result = self.claim("authored.md")
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual(1, len(self.claims()))
        self.assertEqual(300, self.claims()[0]["ttl"])

    def test_claim_absolute_updated_cli_from_linked_tree_uses_shared_registry(self) -> None:
        linked = self.fixture / "linked"
        self.git(self.primary, "worktree", "add", "--quiet", "-b", "linked", str(linked))
        self.env["AGENT_SESSION"] = "synthetic-linked-worker"
        self.assertFalse((linked / ".agents" / "artifacts.yml").exists())
        self.assert_denied("docs/audit/audit-data.js", "COORD-CLAIM-DERIVED-CLASS", cwd=linked)
        result = self.claim("linked-authored.md", cwd=linked)
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual("synthetic-linked-worker", self.claims()[-1]["session"])

    def test_claim_removed_derived_guard_grants_and_breaks_denial_oracle(self) -> None:
        source = self.cli.read_text(encoding="utf-8")
        guard = 'if klass == "derived":'
        self.assertEqual(1, source.count(guard), "mutation requires the actual guard")
        self.cli.write_text(source.replace(guard, "if False:", 1), encoding="utf-8")
        result = self.claim("docs/audit/audit-data.js")
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertIn("granted", result.stdout)
        self.assertEqual(1, len(self.claims()))
        with self.assertRaises(AssertionError):
            self.assert_denied("docs/audit/audit-data.js", "COORD-CLAIM-DERIVED-CLASS")


if __name__ == "__main__":
    unittest.main()
