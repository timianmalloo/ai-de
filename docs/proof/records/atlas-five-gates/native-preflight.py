"""Task-specific native proof setup; it never starts a GUI or grants a desktop slot.

Run in the same environment inherited by the unchanged canonical join. The native test
owns artifacts/atlas-real-daemon-window-proof/<ATLAS_PROOF_RUN>; do not create it here.
"""
from __future__ import annotations

import argparse
import datetime as dt
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[4]
ADMITTED_ROOT = Path("C:/Projects/ai-de-integration-atlas-five-gates")
SESSION = "codex-atlas-five-gates-integration"
AGENT = "codex-astra-gate-conductor"
CONFIGURATION = "Debug"
INPUT_PATHS = ("src", "tests", "Directory.Build.props", "Directory.Build.targets",
               "Directory.Packages.props", "global.json", "NuGet.Config")


def validate_snapshot(expected_head: str, head: str, before: str, after: str) -> None:
    if head != expected_head:
        raise ValueError("Current HEAD differs from the reviewed combined commit.")
    if before or after or before != after:
        raise ValueError("Source/test/build inputs must remain clean before and after the build.")


def validate_registration(root: Path, sessions: dict) -> None:
    matching = [s for s in sessions["sessions"] if s["session"] == SESSION
                and Path(s["worktree"]).resolve() == root.resolve()]
    if sessions["errors"] or len(matching) != 1 or matching[0]["agent"] != AGENT:
        raise ValueError("Registered session/worktree/agent identity did not match exactly.")


def validate_setup(root: Path, environment: dict[str, str]) -> str:
    if environment.get("AGENT_SESSION") != SESSION or environment.get("AGENT_NAME") != AGENT:
        raise ValueError("The exact admitted session and agent identity are required.")
    label = environment.get("ATLAS_PROOF_RUN", "")
    if not re.fullmatch(r"[A-Za-z0-9-]+", label):
        raise ValueError("ATLAS_PROOF_RUN requires a nonempty ASCII alphanumeric/hyphen label.")
    if (root / "artifacts/atlas-real-daemon-window-proof" / label).exists():
        raise ValueError("The native proof receipt already exists; never reuse it.")
    return label


def binary_hashes(root: Path) -> dict[str, str]:
    binary_dir = root / "src/AiDe.Daemon/bin" / CONFIGURATION / "net10.0-windows"
    return {
        str((binary_dir / name).relative_to(root)): hashlib.sha256((binary_dir / name).read_bytes()).hexdigest()
        for name in ("AiDe.Daemon.exe", "AiDe.Daemon.dll", "AiDe.Core.dll")
    }


def output(*args: str) -> str:
    return subprocess.check_output(args, cwd=ROOT, text=True, encoding="utf-8").strip()


def self_test() -> None:
    # Every negative uses a disposable synthetic tree; live receipts/binaries are untouched.
    with tempfile.TemporaryDirectory(prefix="atlas-preflight-") as temp:
        root = Path(temp)
        valid = {"AGENT_SESSION": SESSION, "AGENT_NAME": AGENT, "ATLAS_PROOF_RUN": "fresh-123"}
        failures = 0
        invalid = [
            ("missing label", {k: v for k, v in valid.items() if k != "ATLAS_PROOF_RUN"}),
            ("empty label", {**valid, "ATLAS_PROOF_RUN": ""}),
            ("absolute path", {**valid, "ATLAS_PROOF_RUN": "C:/receipts"}),
            ("non-ASCII label", {**valid, "ATLAS_PROOF_RUN": "caf\u00e9"}),
            ("missing identity", {**valid, "AGENT_SESSION": ""}),
            ("wrong agent", {**valid, "AGENT_NAME": "another-agent"}),
        ]
        for name, environment in invalid:
            try:
                validate_setup(root, environment)
            except ValueError:
                failures += 1
                print(f"PASS refusal: {name}")
            else:
                raise AssertionError(f"Expected refusal: {name}")
        assert validate_setup(root, valid) == "fresh-123"
        receipt = root / "artifacts/atlas-real-daemon-window-proof/fresh-123"
        receipt.mkdir(parents=True)
        try:
            validate_setup(root, valid)
        except ValueError:
            failures += 1
            print("PASS refusal: existing native receipt")
        else:
            raise AssertionError("Expected existing-receipt refusal")
        try:
            binary_hashes(root)
        except FileNotFoundError:
            failures += 1
            print("PASS refusal: missing binary")
        else:
            raise AssertionError("Expected missing-binary refusal")
        registration = {"errors": [], "sessions": [{"session": SESSION, "agent": AGENT, "worktree": str(root)}]}
        validate_registration(root, registration)
        validate_snapshot("expected", "expected", "", "")
        checks = [
            ("wrong HEAD", lambda: validate_snapshot("expected", "different", "", "")),
            ("dirty before build", lambda: validate_snapshot("expected", "expected", " M src/file.cs", "")),
            ("dirty after build", lambda: validate_snapshot("expected", "expected", "", " M src/file.cs")),
            ("missing registration", lambda: validate_registration(root, {"errors": [], "sessions": []})),
            ("duplicate registration", lambda: validate_registration(root, {"errors": [], "sessions": registration["sessions"] * 2})),
            ("wrong worktree registration", lambda: validate_registration(root / "different", registration)),
        ]
        for name, check in checks:
            try:
                check()
            except ValueError:
                failures += 1
                print(f"PASS refusal: {name}")
            else:
                raise AssertionError(f"Expected refusal: {name}")
        print(json.dumps({"positive_cases": 3, "rejected_negative_cases": failures, "gui_started": False}))


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--expected-head", help="Exact reviewed combined commit; required for a real build.")
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args()
    if args.self_test:
        self_test()
        return 0
    if not args.expected_head or not re.fullmatch(r"[0-9a-f]{40}", args.expected_head):
        parser.error("--expected-head must be the exact full reviewed commit")
    if ROOT != ADMITTED_ROOT.resolve() or Path(output("git", "rev-parse", "--show-toplevel")).resolve() != ROOT:
        raise ValueError("Preflight must run from the admitted integration worktree.")
    label = validate_setup(ROOT, dict(os.environ))
    head = output("git", "rev-parse", "HEAD")
    before = output("git", "status", "--porcelain", "--", *INPUT_PATHS)
    validate_snapshot(args.expected_head, head, before, before)
    sessions = json.loads(output(sys.executable, "docs/ai-forward-pack/scripts/coord-core.py", "session", "list", "--json"))
    validate_registration(ROOT, sessions)
    target = ROOT / "artifacts/atlas-five-gates/preflight" / f"{label}.json"
    target.parent.mkdir(parents=True, exist_ok=True)
    command = ["dotnet", "build", "src/AiDe.Daemon/AiDe.Daemon.csproj", "-c", CONFIGURATION,
               "--no-incremental", "-nologo", "-v", "q"]
    state = {"status": "building", "started_utc": dt.datetime.now(dt.timezone.utc).isoformat(),
             "head": head, "root": str(ROOT), "session": SESSION, "agent": AGENT,
             "configuration": CONFIGURATION, "run_label": label, "command": command,
             "input_paths": INPUT_PATHS, "inputs_before": before,
             "gui_started": False, "qualification": "not run"}
    # Exclusive creation prevents a build from erasing previous setup evidence.
    with target.open("x", encoding="utf-8") as receipt:
        json.dump(state, receipt, indent=2)
    try:
        result = subprocess.run(command, cwd=ROOT, check=False)
        state["build_exit"] = result.returncode
        if result.returncode:
            raise RuntimeError(f"Explicit daemon build failed: {result.returncode}")
        state["inputs_after"] = output("git", "status", "--porcelain", "--", *INPUT_PATHS)
        validate_snapshot(head, output("git", "rev-parse", "HEAD"), before, state["inputs_after"])
        if validate_setup(ROOT, dict(os.environ)) != label:
            raise ValueError("Native setup changed during the build.")
        state["binary_sha256"] = binary_hashes(ROOT)
        state["status"] = "setup ready; qualification not run"
    except Exception as error:
        state["status"] = "failed"
        state["error"] = str(error)
        raise
    finally:
        state["ended_utc"] = dt.datetime.now(dt.timezone.utc).isoformat()
        target.write_text(json.dumps(state, indent=2) + "\n", encoding="utf-8")
        print(json.dumps({"receipt": str(target.relative_to(ROOT)), **state}), flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
