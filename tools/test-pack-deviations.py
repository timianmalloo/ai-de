#!/usr/bin/env python3
"""Check ai-de's retained pack extension and the one-dispatcher hook migration.

Run after updatepack. Tests use temporary audit roots and never alter project history.
"""
from __future__ import annotations

import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parent.parent
AUDIT = ROOT / 'docs/ai-forward-pack/scripts/audit-log.py'
HOOKS = ROOT / 'docs/ai-forward-pack/hooks'


def run_audit(cwd: Path, *args: str) -> subprocess.CompletedProcess[str]:
    return subprocess.run([sys.executable, str(AUDIT), *args], cwd=cwd,
                          capture_output=True, text=True, encoding='utf-8', timeout=30)


class PackDeviations(unittest.TestCase):
    def test_append_preserves_supersedes_without_rewriting_original(self):
        with tempfile.TemporaryDirectory() as directory:
            cwd = Path(directory)
            common = ('--root', str(cwd / 'docs'), 'append', '--session', 'pack-deviation-test',
                      '--kind', 'command', '--skill', 'updatepack', '--prompt', 'fixture',
                      '--summary', 'fixture', '--shortname', 'fixture')
            first = run_audit(cwd, *common)
            self.assertEqual(first.returncode, 0, first.stderr)
            log = cwd / 'docs/audit/audit-log.jsonl'
            before = log.read_bytes()
            first_id = json.loads(before.splitlines()[0])['id']
            second = run_audit(cwd, *common, '--supersedes', first_id)
            self.assertEqual(second.returncode, 0, second.stderr)
            self.assertTrue(log.read_bytes().startswith(before))
            rows = [json.loads(line) for line in log.read_text(encoding='utf-8').splitlines()]
            self.assertEqual(len(rows), 2)
            self.assertEqual(rows[1]['supersedes'], first_id)

    def test_default_fallback_contains_only_jsonl(self):
        with tempfile.TemporaryDirectory() as directory:
            cwd = Path(directory)
            result = run_audit(cwd, 'append', '--session', 'pack-deviation-test',
                               '--prompt', 'fixture', '--summary', 'fixture', '--shortname', 'fixture')
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertFalse((cwd / 'docs').exists())
            files = {path.name for path in (cwd / '.agents/log/audit').iterdir()}
            self.assertEqual(files, {'audit-log.jsonl'})

    def test_native_hooks_have_no_absorbed_identity_guard(self):
        paths = (ROOT / '.github/hooks/ai-forward.json', ROOT / '.grok/hooks/ai-forward.json',
                 HOOKS / 'copilot.ai-forward-hooks.json')
        for path in paths:
            with self.subTest(path=path.relative_to(ROOT)):
                text = path.read_text(encoding='utf-8')
                json.loads(text)
                self.assertIn('dispatch.py', text)
                self.assertFalse('run-hook.sh git-identity-guard.py' in text,
                                 f'{path.relative_to(ROOT)} still runs a guard absorbed by the dispatcher.')

    def test_dispatcher_retains_identity_refusal(self):
        for host in ('claude', 'grok', 'copilot'):
            with self.subTest(host=host):
                event = 'preToolUse' if host == 'copilot' else 'PreToolUse'
                payload = {'tool_input': {'command': 'git config user.name fixture'},
                           'toolArgs': {'command': 'git config user.name fixture'}}
                env = dict(os.environ)
                for key in ('AGENT_SESSION', 'AIDE_SESSION', 'COORD_ROOT'):
                    env.pop(key, None)
                result = subprocess.run([sys.executable, str(HOOKS / 'dispatch.py'), '--host', host,
                                         '--event', event], input=json.dumps(payload), env=env,
                                        capture_output=True, text=True, encoding='utf-8', timeout=30)
                self.assertEqual(result.returncode, 2, result.stdout + result.stderr)
                self.assertIn('PK-07', result.stderr)


if __name__ == '__main__':
    unittest.main(verbosity=2)
