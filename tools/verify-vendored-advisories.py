#!/usr/bin/env python3
"""A stored advisory scan is a photograph. This is the camera, pointed again.

WHAT THIS GUARDS. `src/AiDe.App/Web/vendor/` ships a half-megabyte of third-party JavaScript built
from a committed lockfile. Security's condition **C19** on the front-door plan requires a recorded
advisory scan over that lockfile — and, in the same sentence, a CI step that re-runs it, with the
honest limit stated in the plan rather than implied:

    "a one-off scan is a point-in-time observation. THE RECURRING STEP IS THE CONTROL, NOT THE
    STORED OUTPUT."

That distinction is the whole reason this file exists. A vendored dependency set does not become
vulnerable when we change it; it becomes vulnerable when somebody else publishes an advisory about a
version we froze months ago. A record of "0 vulnerabilities on 2026-09-11" is true forever and
protects nobody after 2026-09-11.

WHAT IT CHECKS, in order:

  1. THE RECORD EXISTS AND IS NOT STALE. Every `vendor-manifest.json` must carry
     `provenance.advisory_scan`, and its `against_lockfile_sha256` must still equal the hash of the
     lockfile the manifest names. A scan recorded against a lockfile that has since moved is a
     photograph of a different subject.
  2. THE RAW OUTPUT IS PINNED. `advisory_scan.raw` must be a real file whose SHA-256 matches
     `raw_sha256`, so the stored observation cannot be edited into a better one.
  3. THE SCAN IS RE-RUN, NOW. `npm audit --package-lock-only --json` against the same lockfile, and
     any vulnerability at any severity fails the gate.

WHY IT FAILS CLOSED WHEN npm IS ABSENT. "npm was not available, so nothing was checked" is the same
output a clean run gives, which is DC-016 in its purest form. A recurring control that silently
degrades to nothing is worse than no control, because the CI badge keeps saying the same thing. If
this cannot run, it says so and exits 1.

Exit 0 when clean, 1 otherwise. Stdlib only, no third-party imports.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

MANIFEST_NAME = "vendor-manifest.json"
SKIP_DIRS = {".git", "node_modules", "bin", "obj", ".vs", "dist", "_site"}

REQUIRED_SCAN_FIELDS = (
    "command",
    "working_directory",
    "date",
    "result",
    "raw",
    "raw_sha256",
    "against_lockfile_sha256",
    "honest_limit",
)

SEVERITIES = ("info", "low", "moderate", "high", "critical")


def repo_root() -> Path:
    """The repository root, from git — never the caller's cwd (DC-071)."""
    out = subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        capture_output=True, text=True, check=True)
    return Path(out.stdout.strip())


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1 << 20), b""):
            digest.update(chunk)
    return digest.hexdigest()


def find_manifests(root: Path) -> list[Path]:
    found: list[Path] = []
    for dirpath, dirnames, filenames in os.walk(root):
        dirnames[:] = sorted(d for d in dirnames if d not in SKIP_DIRS)
        if MANIFEST_NAME in filenames:
            found.append(Path(dirpath) / MANIFEST_NAME)
    return sorted(found)


def npm() -> str | None:
    """The npm executable, or None. On Windows it is `npm.cmd`, which `shutil.which` finds."""
    return shutil.which("npm")


def rerun_audit(working_directory: Path) -> tuple[dict[str, int] | None, str]:
    """Runs the scan again. Returns (counts, message); counts is None when it could not run."""
    executable = npm()
    if executable is None:
        return None, (
            "npm is not on PATH, so the advisory scan could not be re-run. This is reported as a "
            "FAILURE rather than skipped: 'nothing was checked' and 'nothing was found' must never "
            "produce the same output.")

    try:
        result = subprocess.run(
            [executable, "audit", "--package-lock-only", "--json"],
            cwd=working_directory, capture_output=True, text=True, timeout=300)
    except (OSError, subprocess.TimeoutExpired) as exc:
        return None, f"the advisory scan could not be run: {exc}"

    # `npm audit` exits non-zero WHEN IT FINDS SOMETHING, so the exit code is not the signal — the
    # JSON is. A gate keyed on the exit code would report a registry outage and a critical advisory
    # identically.
    try:
        report = json.loads(result.stdout)
    except json.JSONDecodeError:
        return None, (
            "the advisory scan produced no readable JSON, so nothing was measured. stderr: "
            f"{result.stderr.strip()[:400]}")

    vulnerabilities = report.get("metadata", {}).get("vulnerabilities")
    if not isinstance(vulnerabilities, dict):
        return None, "the advisory scan's JSON carried no metadata.vulnerabilities object"

    counts = {name: int(vulnerabilities.get(name, 0)) for name in SEVERITIES}
    return counts, ""


def check_manifest(root: Path, manifest_path: Path) -> list[str]:
    rel = manifest_path.relative_to(root).as_posix()
    problems: list[str] = []

    try:
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        return [f"{rel}: could not be read as JSON — {exc}"]

    provenance = manifest.get("provenance")
    if not isinstance(provenance, dict):
        return [f"{rel}: has no 'provenance' object"]

    scan = provenance.get("advisory_scan")
    if not isinstance(scan, dict):
        return [
            f"{rel}: carries no 'provenance.advisory_scan'. C19 requires a RECORDED scan over the "
            "committed lockfile as well as a recurring one; without the record there is nothing to "
            "compare a later finding against."]

    for field in REQUIRED_SCAN_FIELDS:
        value = scan.get(field)
        if value is None or (isinstance(value, (str, list, dict)) and len(value) == 0):
            problems.append(
                f"{rel}: advisory_scan is missing '{field}'. A blank field records nothing while "
                "making the record look complete.")

    lockfile = provenance.get("lockfile")
    if not lockfile:
        return problems + [f"{rel}: provenance names no lockfile, so there is nothing to scan"]

    lock_path = root / lockfile
    if not lock_path.is_file():
        return problems + [f"{rel}: provenance names lockfile '{lockfile}', which is not in the repository"]

    actual_lock = sha256(lock_path)
    if scan.get("against_lockfile_sha256") != actual_lock:
        problems.append(
            f"{rel}: the recorded scan was taken against lockfile SHA-256 "
            f"{scan.get('against_lockfile_sha256')}, and '{lockfile}' is now {actual_lock}. The "
            "record describes a different input set — re-run the scan and re-record it.")

    raw = scan.get("raw")
    if raw:
        raw_path = root / raw
        if not raw_path.is_file():
            problems.append(f"{rel}: advisory_scan names raw output '{raw}', which is not on disk")
        elif sha256(raw_path) != scan.get("raw_sha256"):
            problems.append(
                f"{raw}: SHA-256 does not match the manifest's 'raw_sha256'. The stored observation "
                "is pinned precisely so it cannot be edited into a better one.")

    working_directory = root / (provenance.get("build_working_directory") or lock_path.parent)
    if not working_directory.is_dir():
        return problems + [
            f"{rel}: the scan's working directory '{working_directory}' does not exist"]

    counts, message = rerun_audit(working_directory)
    if counts is None:
        problems.append(f"{rel}: {message}")
        return problems

    total = sum(counts.values())
    summary = ", ".join(f"{name}={counts[name]}" for name in SEVERITIES)
    if total > 0:
        problems.append(
            f"{rel}: the advisory scan re-run over '{lockfile}' reports {total} vulnerability(ies) "
            f"({summary}). The vendored bytes are frozen; the advisories about them are not.")
    else:
        print(f"  {lockfile}: re-scanned now - {summary}")

    return problems


def check(root: Path) -> tuple[list[str], int]:
    manifests = find_manifests(root)
    problems: list[str] = []
    for manifest_path in manifests:
        problems.extend(check_manifest(root, manifest_path))
    return problems, len(manifests)


# --------------------------------------------------------------------------------------------
# SELF-TEST (DC-104). Proves the gate fires on each way the RECORD can be wrong, and stays quiet on
# a clean one. It runs this script as a subprocess from a SUBDIRECTORY of a throwaway git repository,
# because "resolves the repo root from git rather than the caller's cwd" is part of what is proven
# (DC-071).
#
# The re-run half is deliberately exercised against a real, empty lockfile: `npm audit
# --package-lock-only` on a lockfile with no packages reports zero of everything without touching
# the network, so the clean case stays fast and offline while still going through the real command.
# --------------------------------------------------------------------------------------------

CLEAN_LOCK = b'{"name":"selftest","lockfileVersion":3,"requires":true,"packages":{"":{"name":"selftest"}}}\n'
CLEAN_RAW = b'{"metadata":{"vulnerabilities":{"info":0,"low":0,"moderate":0,"high":0,"critical":0}}}\n'


def _write_fixture(root: Path) -> None:
    (root / "vendor").mkdir(parents=True, exist_ok=True)
    (root / "src").mkdir(parents=True, exist_ok=True)
    (root / "deep" / "dir").mkdir(parents=True, exist_ok=True)

    (root / "src" / "package-lock.json").write_bytes(CLEAN_LOCK)
    (root / "src" / "package.json").write_bytes(b'{"name":"selftest","version":"1.0.0"}\n')
    (root / "src" / "advisory-scan.json").write_bytes(CLEAN_RAW)

    manifest = {
        "schema": "vendored-assets/1",
        "directory": "vendor",
        "files": [],
        "provenance": {
            "lockfile": "src/package-lock.json",
            "build_working_directory": "src",
            "advisory_scan": {
                "command": "npm audit --package-lock-only --json",
                "working_directory": "src",
                "date": "2026-09-11",
                "result": "0 vulnerabilities",
                "raw": "src/advisory-scan.json",
                "raw_sha256": hashlib.sha256(CLEAN_RAW).hexdigest(),
                "against_lockfile_sha256": hashlib.sha256(CLEAN_LOCK).hexdigest(),
                "honest_limit": "the recurring step is the control, not this stored output",
            },
        },
    }
    (root / "vendor" / MANIFEST_NAME).write_text(
        json.dumps(manifest, indent=2), encoding="utf-8", newline="\n")


def _run_from_subdirectory(root: Path) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        [sys.executable, str(Path(__file__).resolve())],
        cwd=root / "deep" / "dir", capture_output=True, text=True)


def _break_no_record(root: Path) -> None:
    path = root / "vendor" / MANIFEST_NAME
    manifest = json.loads(path.read_text(encoding="utf-8"))
    del manifest["provenance"]["advisory_scan"]
    path.write_text(json.dumps(manifest, indent=2), encoding="utf-8", newline="\n")


def _break_stale_record(root: Path) -> None:
    (root / "src" / "package-lock.json").write_bytes(CLEAN_LOCK + b"\n")


def _break_missing_raw(root: Path) -> None:
    (root / "src" / "advisory-scan.json").unlink()


def _break_edited_raw(root: Path) -> None:
    (root / "src" / "advisory-scan.json").write_bytes(CLEAN_RAW + b"\n")


def _break_blank_field(root: Path) -> None:
    path = root / "vendor" / MANIFEST_NAME
    manifest = json.loads(path.read_text(encoding="utf-8"))
    manifest["provenance"]["advisory_scan"]["honest_limit"] = ""
    path.write_text(json.dumps(manifest, indent=2), encoding="utf-8", newline="\n")


MODES = (
    ("no recorded scan at all", _break_no_record, "carries no 'provenance.advisory_scan'"),
    ("the record is against a moved lockfile", _break_stale_record, "describes a different input set"),
    ("the raw output is missing", _break_missing_raw, "which is not on disk"),
    ("the raw output was edited", _break_edited_raw, "pinned precisely"),
    ("a blank record field", _break_blank_field, "is missing 'honest_limit'"),
)


def self_test() -> int:
    if npm() is None:
        print("verify-vendored-advisories --self-test: FAILED — npm is not on PATH, so the re-run "
              "half of this gate cannot be proven here. It fails closed rather than reporting a "
              "pass it did not measure.")
        return 1

    failures: list[str] = []
    workspace = Path(tempfile.mkdtemp(prefix="verify-vendored-advisories-selftest-"))

    try:
        root = workspace / "repo"
        root.mkdir()
        subprocess.run(["git", "init", "-q"], cwd=root, check=True, capture_output=True)

        _write_fixture(root)
        clean = _run_from_subdirectory(root)
        if clean.returncode != 0:
            failures.append(
                f"the CLEAN case exited {clean.returncode}, expected 0 — this gate refuses a correct "
                f"record.\n{clean.stdout}{clean.stderr}")
        else:
            print("  clean case                            -> exit 0 (as required)")

        for label, break_it, phrase in MODES:
            shutil.rmtree(root / "vendor", ignore_errors=True)
            shutil.rmtree(root / "src", ignore_errors=True)
            _write_fixture(root)
            break_it(root)

            result = _run_from_subdirectory(root)
            if result.returncode != 1:
                failures.append(
                    f"'{label}' exited {result.returncode}, expected 1. A mode the gate does not "
                    f"catch is a mode that will ship.\n{result.stdout}{result.stderr}")
                continue

            if phrase not in result.stdout:
                failures.append(
                    f"'{label}' exited 1 but reported something else — the gate may be failing for "
                    f"the wrong reason.\n{result.stdout}")
                continue

            print(f"  {label:<38}-> exit 1 (as required)")

    finally:
        shutil.rmtree(workspace, ignore_errors=True)

    if failures:
        print("\nverify-vendored-advisories --self-test: FAILED")
        for failure in failures:
            print(f"  - {failure}")
        return 1

    print(f"\nverify-vendored-advisories --self-test: all {len(MODES)} failure modes fire and the "
          "clean case passes.")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--self-test", action="store_true",
        help="prove this gate exits 1 on each way the record can be wrong and 0 on a clean one")
    args = parser.parse_args()

    if args.self_test:
        return self_test()

    root = repo_root()
    problems, manifests = check(root)

    if not manifests:
        print("verify-vendored-advisories: FAILED — no vendor-manifest.json was found anywhere, so "
              "this check examined nothing.")
        return 1

    if problems:
        print(f"verify-vendored-advisories: FAILED — {len(problems)} problem(s)")
        for problem in problems:
            print(f"  - {problem}")
        return 1

    print(f"verify-vendored-advisories: {manifests} manifest(s) carry a current advisory record, and "
          "the scan was re-run against every lockfile they name.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
