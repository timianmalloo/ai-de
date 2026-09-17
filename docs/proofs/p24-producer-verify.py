"""Bounded P2.P2A experiment: exact source restoration, raw output, TRX counts and SHA-256 pins."""
import hashlib
import json
import os
from pathlib import Path
import subprocess
import time
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
EVIDENCE = ROOT / "docs" / "proofs"
WRITER = ROOT / "src" / "AiDe.Core" / "Watcher" / "CoordinationNativeWrite.cs"
EMITTER = ROOT / "src" / "AiDe.Core" / "Watcher" / "SessionCoordinationEmitter.cs"
PROJECT = ROOT / "tests" / "AiDe.Core.Tests" / "AiDe.Core.Tests.csproj"
PATHS = [
    WRITER, EMITTER, PROJECT,
    ROOT / "src" / "AiDe.Core" / "Watcher" / "CoordinationContractLog.cs",
    ROOT / "tests" / "AiDe.Core.Tests" / "Watcher" / "CoordinationProducerTests.cs",
    ROOT / "tests" / "AiDe.Core.Tests" / "Watcher" / "CoordinationPreparedWriteTests.cs",
    ROOT / "tests" / "AiDe.Core.Tests" / "Watcher" / "CoordinationContractLogTests.cs",
    ROOT / "tests" / "AiDe.Core.Tests" / "Watcher" / "SessionCoordinationEmitterTests.cs",
    ROOT / "tests" / "AiDe.Core.NativeWriterProbe" / "Program.cs",
    ROOT / "tests" / "AiDe.Core.NativeWriterProbe" / "AiDe.Core.NativeWriterProbe.csproj",
    ROOT / "src" / "AiDe.Core" / "bin" / "Debug" / "net10.0" / "AiDe.Core.dll",
    ROOT / "tests" / "AiDe.Core.Tests" / "bin" / "Debug" / "net10.0" / "AiDe.Core.Tests.dll",
    ROOT / "tests" / "AiDe.Core.NativeWriterProbe" / "bin" / "Debug" / "net10.0" / "AiDe.Core.NativeWriterProbe.dll",
    ROOT / "tests" / "AiDe.Core.NativeWriterProbe" / "bin" / "Debug" / "net10.0" / "AiDe.Core.dll",
]
ALL = (
    "FullyQualifiedName~CoordinationProducerTests|FullyQualifiedName~CoordinationPreparedWriteTests|"
    "FullyQualifiedName~SessionCoordinationEmitterTests|FullyQualifiedName~CoordinationContractLogTests|"
    "FullyQualifiedName~CoordinationContractTests"
)
NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
runs = []


def run(name: str, selected: str, failed: int = 0) -> None:
    command = [
        "dotnet", "test", str(PROJECT), "--no-restore", "--filter", selected,
        "--logger", f"trx;LogFileName=p24-producer-{name}.trx",
        "--logger", "console;verbosity=normal",
        "--results-directory", str(EVIDENCE), "--verbosity", "quiet",
    ]
    started = time.monotonic()
    # Capture the actual child streams, not a shell transcript header or a formatted tail.
    child = subprocess.run(command, cwd=ROOT, capture_output=True, text=True, encoding="utf-8",
                           errors="replace", timeout=180, check=False)
    raw = child.stdout + "\nSTDERR:\n" + child.stderr
    (EVIDENCE / f"p24-producer-{name}.stdout.txt").write_text(raw, encoding="utf-8")
    trx = ET.parse(EVIDENCE / f"p24-producer-{name}.trx").getroot()
    counters = trx.find("t:ResultSummary/t:Counters", NS)
    if counters is None:
        raise AssertionError(f"{name}: no test counters")
    counts = {key: int(value) for key, value in counters.attrib.items()}
    print(f"{name}: exit={child.returncode}, counters={counts}", flush=True)
    runs.append({
        "name": name, "command": command, "exit": child.returncode,
        "duration_seconds": time.monotonic() - started, "counts": counts,
        "sha256": {str(path.relative_to(ROOT)): hashlib.sha256(path.read_bytes()).hexdigest()
                   for path in PATHS},
    })
    (EVIDENCE / "p24-producer-writer-pins.json").write_text(
        json.dumps(runs, indent=2) + "\n", encoding="utf-8")
    if counts["failed"] != failed or counts["total"] != counts["passed"] + counts["failed"]:
        raise AssertionError(f"{name}: unexpected logical counts {counts}")
    if (child.returncode == 0) != (failed == 0):
        raise AssertionError(f"{name}: unexpected exit {child.returncode}")


def replace_once(source: str, old: str, new: str) -> str:
    if source.count(old) != 1:
        raise AssertionError(f"Mutation seam not unique: {old}")
    return source.replace(old, new)


def main() -> None:
    original_writer, original_emitter = WRITER.read_bytes(), EMITTER.read_bytes()
    writer = original_writer.decode("utf-8").replace("\r\n", "\n")
    emitter = original_emitter.decode("utf-8").replace("\r\n", "\n")
    try:
        run("final-pre-mutation", ALL)
        mutant = replace_once(writer, "MaximumLineBytes = 65_536", "MaximumLineBytes = 65_537")
        mutant = replace_once(mutant, "MaximumFiles = 128", "MaximumFiles = 129")
        mutant = replace_once(mutant, "MaximumRootBytes = 33_554_432", "MaximumRootBytes = 33_558_528")
        WRITER.write_text(mutant, encoding="utf-8")
        run("bounds-mutant", "FullyQualifiedName~CoordinationProducerTests", 4)
        WRITER.write_bytes(original_writer)

        mutant = replace_once(writer, "var start = checked((int)prepared.Admission.Start);",
                              "prepared.Attempted = true;\n"
                              "        var start = checked((int)prepared.Admission.Start);")
        WRITER.write_text(mutant, encoding="utf-8")
        run("stale-mutant", "FullyQualifiedName~Append_IdenticalNeverAttemptedPreparation", 1)
        WRITER.write_bytes(original_writer)

        mutant = replace_once(emitter,
            "_writer.WriteRegister(externalSessionId, identity.ToAttributes());\n"
            "            lock (_gate) { _live.Add(externalSessionId); }",
            "lock (_gate) { _live.Add(externalSessionId); }\n"
            "            _writer.WriteRegister(externalSessionId, identity.ToAttributes());")
        mutant = replace_once(mutant,
            "_writer.WriteSessionEnd(externalSessionId);\n"
            "            lock (_gate) { _live.Remove(externalSessionId); }",
            "lock (_gate) { _live.Remove(externalSessionId); }\n"
            "            _writer.WriteSessionEnd(externalSessionId);")
        EMITTER.write_text(mutant, encoding="utf-8")
        run("membership-mutant",
            "FullyQualifiedName~Register_AppendDeniedThenRetried|FullyQualifiedName~End_AppendDeniedThenRetried", 2)
        EMITTER.write_bytes(original_emitter)

        mutant = replace_once(emitter, "if (0 == --gate.References)", "if (--gate.References >= 0)")
        EMITTER.write_text(mutant, encoding="utf-8")
        run("three-operation-mutant", "FullyQualifiedName~Register_FormerWaiterOwnsGate", 1)
    finally:
        WRITER.write_bytes(original_writer)
        EMITTER.write_bytes(original_emitter)
    run("final-restored", ALL)
    if WRITER.read_bytes() != original_writer or EMITTER.read_bytes() != original_emitter:
        raise AssertionError("Source was not restored exactly")


if __name__ == "__main__":
    os.chdir(ROOT)
    main()
