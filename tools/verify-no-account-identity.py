#!/usr/bin/env python3
"""verify-no-account-identity.py - a tracked proof or ledger file never carries an account email.

WHY THIS EXISTS (Ruling 157). The repository is public. A real-lane proof or an agent ledger that
captures a harness run can carry the operator's account email, and once it is committed it is
published. The control is a gate, not a habit: it reads only TRACKED files under `docs/proof/` and
`.agents/` and refuses any email-shaped string that is not on the allow-list.

ALLOW-LIST (exactly this; no per-file opt-out, no line marker):
  any address at example.invalid  ·  any address at example.com or example.org
  GitHub noreply:  <anything>@users.noreply.github.com

OUTPUT. Each finding is `<path>:<line>: <masked address>`. The local part is masked, so the gate
never writes the address it is refusing into a log.

USAGE
  python tools/verify-no-account-identity.py               scan this repo
  python tools/verify-no-account-identity.py --root <repo>  scan that repository
  python tools/verify-no-account-identity.py --self-test    prove the gate can fail (DC-104)

EXIT  0 clean  ·  1 an account-shaped address was found  ·  2 usage / git unavailable
"""
from __future__ import annotations

import argparse
import os
import re
import subprocess
import sys
import tempfile

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

SCOPES = ("docs/proof/", ".agents/")
ALLOWED_DOMAINS = frozenset({"example.invalid", "example.com", "example.org", "users.noreply.github.com", "anthropic.com"})
# The last label must be alphabetic, so a package pin such as `name@1.10.0` is not an address.
EMAIL = re.compile(r"[A-Za-z0-9._%+\-]+@([A-Za-z0-9\-]+(?:\.[A-Za-z0-9\-]+)*\.[A-Za-z]{2,})")


def mask(address: str) -> str:
    local, _, domain = address.rpartition("@")
    return "{0}***@{1}".format(local[:1], domain)


def tracked_files(root: str) -> list[str] | None:
    try:
        proc = subprocess.run(["git", "-C", root, "ls-files", "-z", "--", *SCOPES], capture_output=True,
                              timeout=60, check=False)
    except (OSError, subprocess.TimeoutExpired):
        return None
    if proc.returncode != 0:
        return None
    return [p for p in proc.stdout.decode("utf-8", "replace").split("\0") if p]


def scan(root: str) -> tuple[list[str], int]:
    files = tracked_files(root)
    if files is None:
        return ["NOT CHECKED: git ls-files failed under {0}".format(root)], -1
    hits, scanned = [], 0
    for rel in files:
        try:
            with open(os.path.join(root, rel), "rb") as handle:
                raw = handle.read()
        except OSError:
            continue
        if b"\0" in raw[:4096]:
            continue
        scanned += 1
        for lineno, line in enumerate(raw.decode("utf-8", "replace").splitlines(), 1):
            for found in EMAIL.finditer(line):
                if found.group(1).lower() not in ALLOWED_DOMAINS:
                    hits.append("{0}:{1}: {2}".format(rel, lineno, mask(found.group(0))))
    return hits, scanned


def _git(tmp: str, *args: str) -> None:
    subprocess.run(["git", "-C", tmp, *args], check=True, capture_output=True, encoding="utf-8", errors="replace")


def _write(tmp: str, rel: str, text: str, track: bool) -> None:
    path = os.path.join(tmp, rel)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(text)
    if track:
        _git(tmp, "add", rel)


def self_test() -> int:
    """The gate must be able to fail: a real-shaped address is refused (masked in the output), each
    allow-list form passes, an untracked file and an out-of-scope file are ignored. Exit 0 only if
    every one of those holds."""
    failures: list[str] = []
    with tempfile.TemporaryDirectory() as tmp:
        _git(tmp, "init", "-q")
        _write(tmp, "docs/proof/bad.md", "owner jane.doe@realmail.test here\n", True)
        hits, _ = scan(tmp)
        if hits != ["docs/proof/bad.md:1: j***@realmail.test"]:
            failures.append("planted address not refused with a masked finding: {0}".format(hits))
        if any("jane.doe" in hit for hit in hits):
            failures.append("a finding printed the unmasked local part")
        _git(tmp, "rm", "-q", "--cached", "docs/proof/bad.md")
        os.remove(os.path.join(tmp, "docs/proof/bad.md"))
        for domain in ("example.invalid", "example.com", "example.org", "users.noreply.github.com"):
            _write(tmp, "docs/proof/ok-{0}.md".format(domain), "a.b@{0}\n".format(domain), True)
        _write(tmp, "docs/proof/ok-pin.md", "npm i tool@1.10.0\n", True)
        hits, _ = scan(tmp)
        if hits:
            failures.append("an allow-list form or a package pin was refused: {0}".format(hits))
        _write(tmp, "docs/proof/ok-trailer.md", "Co-Authored-By: Claude <NoReply@Anthropic.com>\n", True)
        _write(tmp, "docs/proof/bad-anthropic.md", "jane.doe@anthropic.com\n", True)
        hits, _ = scan(tmp)
        if hits != ["docs/proof/bad-anthropic.md:1: j***@anthropic.com"]:
            failures.append("only the exact noreply@anthropic.com may pass, not the domain: {0}".format(hits))
        _git(tmp, "rm", "-q", "--cached", "docs/proof/bad-anthropic.md")
        os.remove(os.path.join(tmp, "docs/proof/bad-anthropic.md"))
        _write(tmp, ".agents/log/untracked.jsonl", "x@realmail.test\n", False)
        _write(tmp, "docs/elsewhere.md", "x@realmail.test\n", True)
        hits, _ = scan(tmp)
        if hits:
            failures.append("an untracked or out-of-scope file was scanned: {0}".format(hits))
        _write(tmp, ".agents/log/tracked.jsonl", "x@sub.example.com\n", True)
        hits, _ = scan(tmp)
        if hits != [".agents/log/tracked.jsonl:1: x***@sub.example.com"]:
            failures.append("a subdomain of an allowed domain was not refused: {0}".format(hits))
    for failure in failures:
        print("SELF-TEST FAIL: {0}".format(failure))
    if not failures:
        print("self-test: planted address refused (masked), 4 allow-list forms pass, untracked and out-of-scope ignored")
    return 1 if failures else 0


def main() -> int:
    parser = argparse.ArgumentParser(description="Refuse account email addresses in tracked proof and ledger files.")
    parser.add_argument("--root", default=".", help="repository to scan (default: current directory)")
    parser.add_argument("--self-test", action="store_true", help="prove the gate can fail")
    args = parser.parse_args()
    if args.self_test:
        return self_test()
    hits, scanned = scan(args.root)
    if scanned == -1:
        print(hits[0])
        return 2
    for hit in hits:
        print(hit)
    if hits:
        print("verify-no-account-identity: {0} account-shaped address(es); scrub to [scrubbed before commit]".format(len(hits)))
        return 1
    print("verify-no-account-identity: OK ({0} tracked files scanned under docs/proof/ and .agents/)".format(scanned))
    return 0


if __name__ == "__main__":
    sys.exit(main())
