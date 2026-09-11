#!/usr/bin/env python3
"""Spike (source read, no model call): the adapter's `session/new` `_meta` contract the compile
session's tool pin depends on — ADR-0035 / Addendum D §A13.4 C1.

Reads the INSTALLED `@agentclientprotocol/claude-agent-acp` adapter and asserts, line by line, the
eight ADAPTER-SIDE facts the pin rests on. The pin's ENFORCEMENT lives in the Claude Code CLI binary
the SDK ships (the adapter forwards `tools`; ADR-0035 pins that binary's sha too) — a source read of
the adapter cannot show that half; P-D5 does. A version bump changes the sha and this check re-runs;
a bump that moves any of the eight is a red result, which is the "re-run the spike" trigger Ruling 68
names.

    python spikes/compile-session-tool-pin/verify-adapter-contract.py [path-to-acp-agent.js]

Exit 0 = every fact holds; 1 = a fact moved; 2 = the adapter is not installed at the path given.
"""
from __future__ import annotations

import hashlib
import json
import re
import sys
from pathlib import Path

DEFAULT = Path("spikes/acp-subscription-lane/node_modules/@agentclientprotocol/claude-agent-acp/dist/acp-agent.js")

# (name, regex the SOURCE must contain, what the fact carries for the architecture)
FACTS = [
    ("tools-from-meta",
     r"const tools = userProvidedOptions\?\.tools \?\?\s*\(params\._meta\?\.disableBuiltInTools === true \? \[\] : \{ type: \"preset\", preset: \"claude_code\" \}\);",
     "`_meta.claudeCode.options.tools` is the primary pin; `_meta.disableBuiltInTools: true` is the legacy shorthand for `[]`; absent both, the SDK gets the `claude_code` preset"),
    ("tools-reach-sdk",
     r"disallowedTools: \[\.\.\.\(userProvidedOptions\?\.disallowedTools \|\| \[\]\), \.\.\.disallowedTools\],\s*\n\s*tools,\s*$",
     "the resolved `tools` value is passed to the SDK query options verbatim (anchored to the options block, beside disallowedTools)"),
    ("disallowed-concat",
     r"disallowedTools: \[\.\.\.\(userProvidedOptions\?\.disallowedTools \|\| \[\]\), \.\.\.disallowedTools\],",
     "`_meta.claudeCode.options.disallowedTools` is concatenated with the adapter's own — the braces pin reaches the SDK"),
    ("setting-sources",
     r"settingSources: \[\"user\", \"project\", \"local\"\],",
     "the compile session loads the repository's CLAUDE.md, settings and hooks from the cwd — the constitution by harness load, and the hooks residual"),
    ("meta-spread-order",
     r"settingSources: \[\"user\", \"project\", \"local\"\],\s*\n(?:.*\n){0,2}\s*\.\.\.userProvidedOptions,",
     "`_meta.claudeCode.options` is spread AFTER settingSources — a caller could override it; the architecture rejects doing so (dropping `project` drops CLAUDE.md)"),
    ("permission-mode-from-settings",
     r"const permissionMode = resolvePermissionMode\(settingsManager\.getSettings\(\)\.permissions\?\.defaultMode, this\.logger\);",
     "the permission mode comes from settings, not from the host — the reject-all handler is not the control; the pin is"),
    ("bypass-before-canusetool",
     r"Code applies bypassPermissions before invoking canUseTool",
     "auto-allowed/bypassed tools never reach the host's canUseTool — the adapter's own COMMENT (Inferred: CLI-enforced; Flagged until P-D5)"),
    ("leading-slash-is-a-command",
     r"a `/model <name>` command\s*\n\s*// typed as a prompt — which the CLI executes as a local command",
     "a prompt whose first bytes are `/…` is executed by the CLI as a local command — why the compile prompt begins with a fixed host header"),
]


def main(argv: list[str]) -> int:
    path = Path(argv[1]) if len(argv) > 1 else DEFAULT
    if not path.is_file():
        print(f"NOT INSTALLED: {path}")
        return 2
    raw = path.read_bytes()
    text = raw.decode("utf-8")
    sha = hashlib.sha256(raw).hexdigest()  # raw bytes: the same domain the product's pin check uses
    pkg = path.parent.parent / "package.json"
    version = json.loads(pkg.read_text(encoding="utf-8")).get("version", "?") if pkg.is_file() else "?"
    print(f"adapter: {path}")
    print(f"version: {version}")
    print(f"sha256 : {sha}")
    failed = 0
    for name, pattern, carries in FACTS:
        m = re.search(pattern, text, re.MULTILINE)
        line = text.count("\n", 0, m.start()) + 1 if m else None
        status = "PASS" if m else "FAIL"
        if not m:
            failed += 1
        print(f"  [{status}] {name:<30} line {line if line else '-':<6} {carries}")
    print()
    print("RESULT: " + ("PASS — every adapter-side fact the pin rests on holds in this adapter build" if failed == 0
                        else f"FAIL — {failed} fact(s) moved; re-read the adapter and re-run the P-D5 wire spike"))
    return 0 if failed == 0 else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv))
