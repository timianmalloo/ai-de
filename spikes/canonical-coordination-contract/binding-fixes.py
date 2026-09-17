"""Finite actual-C# binding regressions; immutable 17-pin receipts, no live sources."""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import time
import xml.etree.ElementTree as ET

import validate

ROOT = validate.ROOT
RECORDS = validate.RECORDS
SOURCE = ROOT / "src/AiDe.Core/Watcher/CanonicalCoordinationSourceBinding.cs"
PREFIX = "binding-fixes-"
TESTS = "FullyQualifiedName~CanonicalCoordination"
B1 = "Bind_CopiedLinkedPointer_RefusesImpostorWithoutSource"
B2 = "Bind_GitValidRelativeForwardPointer_BindsPhysicalPrimaryAndSameScope"
RED = f"FullyQualifiedName~{B1}|FullyQualifiedName~{B2}"


def write(path: Path, value: object) -> None:
    with path.open("x", encoding="utf-8") as stream:
        json.dump(value, stream, indent=2)
        stream.write("\n")


def snapshot(name: str) -> dict:
    assert len(validate.TRACKED) == 17
    value = {
        "head": validate.generate.git(ROOT, "rev-parse", "HEAD").decode().strip(),
        "files": {p: hashlib.sha256((ROOT / p).read_bytes()).hexdigest()
                  if (ROOT / p).exists() else None for p in validate.TRACKED},
        "runnerSha256": hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
    }
    write(RECORDS / (PREFIX + name + "-pins.json"), value)
    return value


def run(name: str, selection: str, expected: int, focal: dict | None = None) -> dict:
    before = snapshot(name + "-before")
    stem = RECORDS / (PREFIX + name)
    trx = stem.with_suffix(".trx")
    assert not trx.exists(), trx
    command = ["dotnet", "test", str(ROOT / "tests/AiDe.Core.Tests/AiDe.Core.Tests.csproj"),
               "--no-restore", "--nologo", "-v:minimal", "--filter", selection,
               "--logger", "console;verbosity=normal", "--logger", "trx;LogFileName=" + str(trx)]
    started = time.perf_counter()
    with stem.with_suffix(".stdout.txt").open("xb") as stdout, stem.with_suffix(".stderr.txt").open("xb") as stderr:
        result = subprocess.run(command, cwd=ROOT, stdout=stdout, stderr=stderr, timeout=180)
    after = snapshot(name + "-after")
    evidence = validate.mutation_evidence(trx, focal or {})
    counters = evidence.get("counters", {})
    qualified = result.returncode == expected and evidence["qualified"]
    if expected == 0:
        qualified &= int(counters.get("executed", 0)) > 0 and counters.get("failed") == "0"
    else:
        qualified &= int(counters.get("failed", 0)) == len(focal or {})
    unchanged = {p: before["files"][p] == after["files"][p]
                 for p in validate.TRACKED if not p.endswith(".dll")}
    qualified &= all(unchanged.values())
    receipt = {"command": command, "exit": result.returncode, "qualified": qualified,
               "durationSeconds": time.perf_counter() - started, "evidence": evidence,
               "nonBinaryPinsUnchanged": unchanged}
    write(stem.with_suffix(".json"), receipt)
    print(json.dumps({"run": name, "exit": result.returncode, "qualified": qualified,
                      "counters": counters, "failures": evidence.get("failures")}), flush=True)
    if not qualified:
        raise RuntimeError("BINDING_FIXES.UNQUALIFIED: " + name)
    return receipt


def mutate() -> None:
    guard = "if (!string.Equals(backlink, dotGit, PathComparison.ForThisFileSystem))"
    legacy = """var located = new FileSystemRepositoryLocator().RepositoryFor(checkout);
                if (located is null || RepositoryIdentity.Canonicalise(located) != RepositoryIdentity.Canonicalise(primary))
                    return Result(CanonicalBindingStatus.Unavailable, CoordinationBindingErrors.Invalid);
                """
    mutations = (
        ("guard-removed-executable", guard,
         "if (!string.Equals(dotGit, dotGit, PathComparison.ForThisFileSystem))",
         {B1: ("Assert.Equal() Failure", "Expected: Unbound", "Actual:   Bound")}),
        ("guard-reversed", guard, guard.replace("!string.Equals", "string.Equals"),
         {B1: ("Assert.Equal() Failure", "Expected: Bound", "Actual:   Unbound"),
          B2: ("Assert.Equal() Failure", "Expected: Bound", "Actual:   Unbound")}),
        ("legacy-root", "var backlinkPath =", legacy + "var backlinkPath =",
         {B2: ("Assert.Equal() Failure", "Expected: Bound", "Actual:   Unavailable")}),
    )
    for name, original, replacement, focal in mutations:
        before = snapshot(name + "-premutation")
        saved = SOURCE.read_bytes()
        text = saved.decode("utf-8")
        assert text.count(original) == 1, name
        try:
            SOURCE.write_bytes(text.replace(original, replacement).encode("utf-8"))
            run(name, RED, 1, focal)
        finally:
            SOURCE.write_bytes(saved)
            assert SOURCE.read_bytes() == saved
            restored = snapshot(name + "-source-restored")
            assert restored["files"][str(SOURCE.relative_to(ROOT)).replace("\\", "/")] == hashlib.sha256(saved).hexdigest()
        run(name + "-restored", TESTS, 0)
        after = snapshot(name + "-rebuilt")
        stable = {p: before["files"][p] == after["files"][p] for p in validate.TRACKED}
        write(RECORDS / (PREFIX + name + "-restoration.json"), stable)
        assert all(stable.values()), stable


def probe() -> None:
    before = snapshot("probe-before")
    commands = [
        ["dotnet", "build", str(validate.HERE / "CanonicalProbe.csproj"), "--no-restore", "--nologo", "-v:minimal"],
        ["dotnet", str(validate.HERE / "bin/Debug/net10.0/CanonicalProbe.dll"),
         str(ROOT / "tests/AiDe.Core.Tests/Watcher/Fixtures/canonical-coordination-v1.json")],
    ]
    for i, command in enumerate(commands):
        stem = RECORDS / (PREFIX + f"probe-{i}")
        with stem.with_suffix(".stdout.txt").open("xb") as stdout, stem.with_suffix(".stderr.txt").open("xb") as stderr:
            result = subprocess.run(command, cwd=ROOT, stdout=stdout, stderr=stderr, timeout=180)
        write(stem.with_suffix(".json"), {"command": command, "exit": result.returncode})
        assert result.returncode == 0
    result = json.loads((RECORDS / (PREFIX + "probe-1.stdout.txt")).read_text(encoding="utf-8"))
    assert result["mismatchCount"] == 0 and result["fullContractQualified"] is False
    assert result["numberCount"] + result["eventCount"] + len(result["invalidResults"]) == 12568
    after = snapshot("probe-after")
    for p in validate.TRACKED:
        if not p.endswith(".dll"):
            assert before["files"][p] == after["files"][p], p
    oracle = json.loads((RECORDS / "validator-oracle.json").read_text(encoding="utf-8"))
    assert len(oracle["cases"]) == 115 and oracle["pin"] == validate.generate.PIN
    print(json.dumps(result), flush=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("stage", choices=("red", "green", "mutate", "probe"))
    stage = parser.parse_args().stage
    if stage == "red":
        assert validate.generate.git(ROOT, "rev-parse", "HEAD").decode().strip() == "f5e2fc17edbb758943b7e263e7fe31ea63fbfa8a"
        assert SOURCE.read_bytes() == validate.generate.git(ROOT, "show", "HEAD:src/AiDe.Core/Watcher/CanonicalCoordinationSourceBinding.cs").replace(b"\n", b"\r\n") or SOURCE.read_bytes() == validate.generate.git(ROOT, "show", "HEAD:src/AiDe.Core/Watcher/CanonicalCoordinationSourceBinding.cs")
        run("red-exact", RED, 1, {
            B1: ("Assert.Equal() Failure", "Expected: Unbound", "Actual:   Bound"),
            B2: ("Assert.Equal() Failure", "Expected: Bound", "Actual:   Unavailable")})
    elif stage == "green":
        run("green-final", TESTS, 0)
    elif stage == "mutate":
        mutate()
    else:
        probe()
