#!/usr/bin/env python3
"""verify-no-account-identity.py - STUB for the red commit: scan() finds nothing."""
from __future__ import annotations

import argparse
import sys
import tempfile
import subprocess
import os


def scan(root: str) -> tuple[list[str], int]:
    return [], 0


def self_test() -> int:
    with tempfile.TemporaryDirectory() as tmp:
        subprocess.run(["git", "init", "-q", tmp], check=True, encoding="utf-8", errors="replace")
        os.makedirs(os.path.join(tmp, "docs", "proof"))
        rel = "docs/proof/x.md"
        with open(os.path.join(tmp, rel), "w", encoding="utf-8") as handle:
            handle.write("owner jane.doe@realmail.test\n")
        subprocess.run(["git", "-C", tmp, "add", rel], check=True, encoding="utf-8", errors="replace")
        hits, _ = scan(tmp)
        if not hits:
            print("SELF-TEST FAIL: the planted real-shaped address was not refused")
            return 1
    return 0


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", default=".")
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args()
    if args.self_test:
        return self_test()
    hits, _ = scan(args.root)
    return 1 if hits else 0


if __name__ == "__main__":
    sys.exit(main())
