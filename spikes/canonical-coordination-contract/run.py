"""Run independent spike gates and retain their actual outputs, never a piped status."""
from __future__ import annotations

import argparse
import json
from pathlib import Path
import subprocess
import sys
import time

ROOT = Path(__file__).resolve().parents[2]
SPIKE = Path(__file__).resolve().parent


def run(label: str, command: list[str]) -> dict:
    start = time.perf_counter()
    result = subprocess.run(command, cwd=ROOT, capture_output=True, timeout=180)
    (SPIKE / "records").mkdir(exist_ok=True)
    (SPIKE / "records" / (label + ".txt")).write_bytes(result.stdout + result.stderr)
    receipt = {"label": label, "command": command, "exit": result.returncode,
               "durationSeconds": time.perf_counter() - start}
    print(json.dumps(receipt), flush=True)
    return receipt


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("stage", choices=("red", "green"))
    args = parser.parse_args()
    build = run(args.stage + "-build", [
        "dotnet", "build", str(SPIKE / "CanonicalProbe.csproj"), "--nologo", "-v:q"])
    if build["exit"]:
        return build["exit"]
    probe = run(args.stage + "-probe", [
        "dotnet", str(SPIKE / "bin/Debug/net10.0/CanonicalProbe.dll"),
        str(ROOT / "tests/AiDe.Core.Tests/Watcher/Fixtures/canonical-coordination-v1.json")])
    tests = run(args.stage + "-tests", [
        "dotnet", "test", str(ROOT / "tests/AiDe.Core.Tests/AiDe.Core.Tests.csproj"),
        "--no-restore", "--nologo", "-v:q", "--filter",
        "FullyQualifiedName~CanonicalCoordinationContractTests"])
    receipts = [build, probe, tests]
    (SPIKE / "records" / (args.stage + ".json")).write_text(
        json.dumps(receipts, indent=2) + "\n", encoding="utf-8")
    expected = 1 if args.stage == "red" else 0
    return 0 if probe["exit"] == expected and tests["exit"] == expected else 1


if __name__ == "__main__":
    sys.exit(main())
