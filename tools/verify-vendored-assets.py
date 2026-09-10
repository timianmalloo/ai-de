#!/usr/bin/env python3
"""A vendored binary blob is only pinned if something re-reads the pin. This re-reads it.

WHAT THIS GUARDS. `src/AiDe.App/Web/vendor/` holds a half-megabyte ES-module bundle of CodeMirror 6
that no compiler type-checks, no test imports by name, and no reviewer reads. It was produced once,
by a toolchain that is not in the build, from packages that are not in the repository. Everything
that makes it trustworthy is a claim in a JSON file beside it — and a claim nothing re-reads is
documentation, not a control (CI6).

Security's condition C1-C4 on the front-door plan, made executable. FIVE ways the pin can be wrong,
and the gate exits 1 on each:

  1. HASH MISMATCH          the file's bytes are not the bytes that were recorded
  2. ENTRY WITH NO FILE     the manifest promises a file that is not there
  3. FILE WITH NO ENTRY     a file sits in the vendored directory that nothing recorded
  4. LOCKFILE MISMATCH      the lockfile the bundle claims to be built from has changed
  5. MISSING PROVENANCE     a field that says where this came from is absent or blank

THE THIRD ONE IS THE POINT, and it is the one that gets left out. The first two feel like the whole
job — "everything I listed is present and correct" — and they are satisfied completely by a directory
that ALSO contains a second script somebody dropped in beside the bundle. The page loads what the
directory serves, not what the manifest lists. So the check walks the FILESYSTEM rather than the
manifest, and rather than `git ls-files`: an untracked file in a served directory is served exactly
as hard as a tracked one.

THE FOURTH IS THE SECOND-COMMIT PROBLEM. Security's own words: "vendoring usually fails at the second
commit, not the first". A re-fetch cannot separate a version bump from a pipeline change, so the
lockfile is hashed too — the input set and the output bytes are pinned together or neither is.

Exit 0 when clean, 1 otherwise. Stdlib only, no third-party imports, no network.
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

# Directories never walked when looking for manifests: build output holds COPIES of the vendored
# directory, and checking a copy against repo-relative paths would report the same files twice.
SKIP_DIRS = {".git", "node_modules", "bin", "obj", ".vs", "dist", "_site"}

# Every field C2 requires. Present AND non-blank: a provenance key whose value is "" or [] records
# nothing while making the manifest look complete, which is worse than its absence because it passes
# a shape check.
REQUIRED_PROVENANCE = (
    "packages",
    "lockfile",
    "lockfile_sha256",
    "entry",
    "install_command",
    "build_command",
    "build_working_directory",
    "builder",
    "builder_version",
    "node_version",
    "npm_version",
    "build_date",
    "licence",
    "licence_copy",
)


def repo_root() -> Path:
    """The repository root, from git — never the caller's cwd.

    DC-071's shape: a tool that resolves its inputs relative to wherever it was invoked passes when
    run from the root and reports nothing when run from anywhere else, which is the same output a
    clean repository gives. `--self-test` runs this script from a subdirectory for exactly that
    reason.
    """
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


def check_manifest(root: Path, manifest_path: Path) -> list[str]:
    """Every problem this manifest has, rather than the first one.

    All five modes are reported in one run because a vendored-asset failure is usually investigated
    once: reporting "hash mismatch" and stopping hides that the lockfile moved too, which is the
    difference between "somebody edited a file" and "somebody rebuilt from a different input set".
    """
    rel_manifest = manifest_path.relative_to(root).as_posix()
    problems: list[str] = []

    try:
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        return [f"{rel_manifest}: could not be read as JSON — {exc}"]

    directory = manifest.get("directory")
    if not directory:
        return [f"{rel_manifest}: has no 'directory' field, so nothing knows which files it covers"]

    vendor_dir = root / directory
    if not vendor_dir.is_dir():
        return [f"{rel_manifest}: names directory '{directory}', which does not exist"]

    entries = manifest.get("files")
    if not isinstance(entries, list) or not entries:
        problems.append(f"{rel_manifest}: 'files' is missing or empty — a manifest that lists nothing pins nothing")
        entries = []

    listed: set[str] = set()

    for entry in entries:
        path_value = entry.get("path") if isinstance(entry, dict) else None
        if not path_value:
            problems.append(f"{rel_manifest}: a 'files' entry has no 'path'")
            continue

        listed.add(path_value)
        target = root / path_value

        # MODE 2 — an entry with no file.
        if not target.is_file():
            problems.append(
                f"{rel_manifest}: lists '{path_value}', which is not on disk. The manifest is the "
                "only record of what this directory is supposed to contain; a promise with nothing "
                "behind it means the pin covers a file the shell will never load.")
            continue

        # MODE 1 — the bytes are not the recorded bytes.
        actual = sha256(target)
        expected = entry.get("sha256")
        if actual != expected:
            problems.append(
                f"{path_value}: SHA-256 is {actual}, the manifest records {expected}. Either the "
                "file was rebuilt without re-recording it, or something changed it. Both are the "
                "same failure from here: the committed bytes are not the reviewed bytes.")

        actual_bytes = target.stat().st_size
        if actual_bytes != entry.get("bytes"):
            problems.append(
                f"{path_value}: is {actual_bytes} bytes, the manifest records {entry.get('bytes')}.")

    # MODE 3 — a file with no entry. THE ONE THAT GETS OMITTED. Walked from the filesystem, so an
    # untracked file counts: the page is served the directory, and a browser does not consult git.
    for dirpath, dirnames, filenames in os.walk(vendor_dir):
        dirnames[:] = sorted(d for d in dirnames if d not in SKIP_DIRS)
        for name in sorted(filenames):
            found = Path(dirpath) / name

            # The manifest cannot contain its own hash. It is excluded by exact identity rather than
            # by name, so a SECOND file called vendor-manifest.json in a subdirectory is still caught.
            if found == manifest_path:
                continue

            rel = found.relative_to(root).as_posix()
            if rel not in listed:
                problems.append(
                    f"{rel}: is in the vendored directory and NOT in {rel_manifest}. This is the "
                    "mode a hash check alone cannot see: every listed file can be perfect while an "
                    "unlisted script sits beside the pinned bundle, served by the same virtual host "
                    "and loaded by the same page.")

    provenance = manifest.get("provenance")
    if not isinstance(provenance, dict):
        problems.append(f"{rel_manifest}: has no 'provenance' object")
        return problems

    # MODE 5 — a provenance field that is absent, blank, or empty.
    for field in REQUIRED_PROVENANCE:
        if field not in provenance:
            problems.append(
                f"{rel_manifest}: provenance is missing '{field}'. Every one of these answers a "
                "question a reader would otherwise have to guess at, and a guess about where half a "
                "megabyte of executable came from is the whole risk.")
            continue

        value = provenance[field]
        if value is None or (isinstance(value, (str, list, dict)) and len(value) == 0):
            problems.append(
                f"{rel_manifest}: provenance field '{field}' is empty. A blank field records "
                "nothing while making the manifest look complete — which passes a shape check and "
                "answers no question.")

    # MODE 4 — the lockfile the bundle claims to be built from has changed.
    lockfile = provenance.get("lockfile")
    if lockfile:
        lock_path = root / lockfile
        if not lock_path.is_file():
            problems.append(
                f"{rel_manifest}: provenance names lockfile '{lockfile}', which is not in the "
                "repository. The build is then unrepeatable and the package set is an assertion.")
        else:
            actual_lock = sha256(lock_path)
            if actual_lock != provenance.get("lockfile_sha256"):
                problems.append(
                    f"{lockfile}: SHA-256 is {actual_lock}, the manifest records "
                    f"{provenance.get('lockfile_sha256')}. Vendoring fails at the SECOND commit, not "
                    "the first: without this, a version bump and a pipeline change look identical.")

    return problems


def check(root: Path) -> tuple[list[str], int, int]:
    manifests = find_manifests(root)
    problems: list[str] = []
    files = 0

    for manifest_path in manifests:
        problems.extend(check_manifest(root, manifest_path))
        try:
            files += len(json.loads(manifest_path.read_text(encoding="utf-8")).get("files") or [])
        except (OSError, json.JSONDecodeError):
            pass

    return problems, len(manifests), files


# --------------------------------------------------------------------------------------------
# SELF-TEST
#
# DC-104: a new control's first run is evidence about two things, and the likelier defect is in the
# control. So this proves the gate FIRES on each of the five modes AND stays quiet on a clean tree —
# a self-test that only proves the happy path proves nothing at all.
#
# It runs THIS SCRIPT as a subprocess, from a SUBDIRECTORY of a throwaway git repository, because the
# property being proven includes "resolves the repo root from git, not from the caller's cwd"
# (DC-071). Calling check() in-process with a root argument would prove the logic and skip exactly
# the bug that shape produces.
# --------------------------------------------------------------------------------------------

CLEAN_BUNDLE = b"export function makeComposer() { return 1; }\n"
CLEAN_LOCK = b'{"lockfileVersion": 3, "packages": {}}\n'


def _write_fixture(root: Path) -> None:
    (root / "vendor").mkdir(parents=True, exist_ok=True)
    (root / "lock").mkdir(parents=True, exist_ok=True)
    (root / "deep" / "dir").mkdir(parents=True, exist_ok=True)

    bundle = root / "vendor" / "bundle.mjs"
    bundle.write_bytes(CLEAN_BUNDLE)
    lock = root / "lock" / "package-lock.json"
    lock.write_bytes(CLEAN_LOCK)

    manifest = {
        "schema": "vendored-assets/1",
        "directory": "vendor",
        "files": [{
            "path": "vendor/bundle.mjs",
            "sha256": hashlib.sha256(CLEAN_BUNDLE).hexdigest(),
            "bytes": len(CLEAN_BUNDLE),
        }],
        "provenance": {field: "recorded" for field in REQUIRED_PROVENANCE} | {
            "packages": [{"name": "example", "version": "1.0.0"}],
            "lockfile": "lock/package-lock.json",
            "lockfile_sha256": hashlib.sha256(CLEAN_LOCK).hexdigest(),
        },
    }
    (root / "vendor" / MANIFEST_NAME).write_text(
        json.dumps(manifest, indent=2), encoding="utf-8", newline="\n")


def _run_from_subdirectory(root: Path) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        [sys.executable, str(Path(__file__).resolve())],
        cwd=root / "deep" / "dir", capture_output=True, text=True)


def _break_hash(root: Path) -> None:
    (root / "vendor" / "bundle.mjs").write_bytes(CLEAN_BUNDLE + b"// tampered\n")


def _break_entry_with_no_file(root: Path) -> None:
    (root / "vendor" / "bundle.mjs").unlink()


def _break_file_with_no_entry(root: Path) -> None:
    (root / "vendor" / "helper.mjs").write_bytes(b"// dropped in beside the bundle\n")


def _break_lockfile(root: Path) -> None:
    (root / "lock" / "package-lock.json").write_bytes(CLEAN_LOCK + b"\n")


def _break_provenance(root: Path) -> None:
    path = root / "vendor" / MANIFEST_NAME
    manifest = json.loads(path.read_text(encoding="utf-8"))
    manifest["provenance"]["build_command"] = ""
    path.write_text(json.dumps(manifest, indent=2), encoding="utf-8", newline="\n")


MODES = (
    ("hash mismatch", _break_hash, "SHA-256 is"),
    ("manifest entry with no file", _break_entry_with_no_file, "which is not on disk"),
    ("a file with no manifest entry", _break_file_with_no_entry, "NOT in"),
    ("lockfile-hash mismatch", _break_lockfile, "SECOND commit"),
    ("missing or empty provenance field", _break_provenance, "is empty"),
)


def self_test() -> int:
    failures: list[str] = []
    workspace = Path(tempfile.mkdtemp(prefix="verify-vendored-assets-selftest-"))

    try:
        root = workspace / "repo"
        root.mkdir()
        subprocess.run(["git", "init", "-q"], cwd=root, check=True, capture_output=True)

        # THE CLEAN CASE FIRST. A self-test that proves only the failures cannot tell a working gate
        # from one that refuses everything, which is the same defect wearing the opposite sign.
        _write_fixture(root)
        clean = _run_from_subdirectory(root)
        if clean.returncode != 0:
            failures.append(
                f"the CLEAN case exited {clean.returncode}, expected 0 — this gate refuses a "
                f"correct tree.\n{clean.stdout}{clean.stderr}")
        else:
            print("  clean case                        -> exit 0 (as required)")

        for label, break_it, expected_phrase in MODES:
            shutil.rmtree(root / "vendor", ignore_errors=True)
            shutil.rmtree(root / "lock", ignore_errors=True)
            _write_fixture(root)
            break_it(root)

            result = _run_from_subdirectory(root)
            if result.returncode != 1:
                failures.append(
                    f"'{label}' exited {result.returncode}, expected 1. A mode the gate does not "
                    f"catch is a mode that will ship.\n{result.stdout}{result.stderr}")
                continue

            if expected_phrase not in result.stdout:
                failures.append(
                    f"'{label}' exited 1 but reported something else — the gate may be failing for "
                    f"the wrong reason, which passes a count and proves nothing.\n{result.stdout}")
                continue

            print(f"  {label:<34}-> exit 1 (as required)")

    finally:
        shutil.rmtree(workspace, ignore_errors=True)

    if failures:
        print("\nverify-vendored-assets --self-test: FAILED")
        for failure in failures:
            print(f"  - {failure}")
        return 1

    print(
        f"\nverify-vendored-assets --self-test: all {len(MODES)} failure modes fire and the clean "
        "case passes.")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--self-test", action="store_true",
        help="prove this gate exits 1 on each of the five modes and 0 on a clean tree")
    args = parser.parse_args()

    if args.self_test:
        return self_test()

    root = repo_root()
    problems, manifests, files = check(root)

    if not manifests:
        # Not a pass. A gate that examined nothing and printed OK is the failure it exists to
        # prevent, one layer up.
        print("verify-vendored-assets: FAILED — no vendor-manifest.json was found anywhere in the "
              "repository, so this check examined nothing.")
        return 1

    if problems:
        print(f"verify-vendored-assets: FAILED — {len(problems)} problem(s)")
        for problem in problems:
            print(f"  - {problem}")
        return 1

    print(f"verify-vendored-assets: {files} vendored file(s) across {manifests} manifest(s) match "
          "their recorded hashes, sizes, lockfile and provenance.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
