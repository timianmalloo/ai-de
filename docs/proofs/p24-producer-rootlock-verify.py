"""Root OS-open cleanup qualification; reuse the existing native command/TRX/SHA harness."""
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location(
    "producer_evidence", ROOT / "docs" / "proofs" / "p24-producer-verify.py")
assert spec is not None and spec.loader is not None
harness = importlib.util.module_from_spec(spec)
spec.loader.exec_module(harness)
harness.EVIDENCE = ROOT / "docs" / "proofs" / "p24-producer-rootlock-runs"
TEST = "PrepareAndAppend_ExternalOsRootLockBusy_ReclaimsLocalEntryAndPreservesHolder"
SOURCE = "src/AiDe.Core/Watcher/CoordinationNativeWrite.cs"


def coord(operation: str) -> None:
    subprocess.run(
        ["python", "docs/ai-forward-pack/scripts/coord-core.py", operation,
         "--path", SOURCE, *(
             ["--wi", "p24-rootlock-evidence", "--ttl", "300"] if operation == "claim" else [])],
        cwd=ROOT, check=True)


def write_source(contents: bytes) -> None:
    coord("claim")
    try:
        harness.WRITER.write_bytes(contents)
        assert harness.WRITER.read_bytes() == contents
    finally:
        coord("release")


def main() -> None:
    os.chdir(ROOT)
    harness.EVIDENCE.mkdir(exist_ok=False)
    original = harness.WRITER.read_bytes()
    production_before = subprocess.check_output(
        ["git", "ls-files", "-s", "src"], cwd=ROOT, text=True)
    harness.PATHS.append(Path(__file__).resolve())
    selected = f"FullyQualifiedName~{TEST}"
    harness.run("rootlock-original", selected)
    assert harness.runs[-1]["counts"]["total"] == 1
    writer = original.decode("utf-8").replace("\r\n", "\n")
    mutant = harness.replace_once(writer,
        "var lease = new RootLease(key, gate);",
        "var lease = new RootLease(key, gate);\n        var failedOsOpen = false;")
    mutant = harness.replace_once(mutant,
        "catch (IOException error) when ((error.HResult & 0xffff) is 32 or 33 or 11)\n"
        "            {\n                throw",
        "catch (IOException error) when ((error.HResult & 0xffff) is 32 or 33 or 11)\n"
        "            {\n                failedOsOpen = true;\n                throw")
    mutant = harness.replace_once(mutant,
        "catch { lease.Dispose(); throw; }",
        "catch\n        {\n"
        "            if (failedOsOpen) Monitor.Exit(gate.Sync);\n"
        "            else lease.Dispose();\n            throw;\n        }")
    try:
        write_source(mutant.encode("utf-8"))
        harness.run("rootlock-mutant", selected, failed=1)
        trx = ET.parse(harness.EVIDENCE / "p24-producer-rootlock-mutant.trx").getroot()
        failures = trx.findall(".//t:UnitTestResult[@outcome='Failed']", harness.NS)
        assert len(failures) == 1 and failures[0].attrib["testName"].endswith(TEST)
        assert "retained local root entry after OS-open failure" in "".join(failures[0].itertext())
        assert "originalOsHolderValid=true; competingOsOpenDenied=true" in "".join(failures[0].itertext())
    finally:
        write_source(original)
    harness.run("rootlock-restored", selected)
    assert harness.runs[-1]["counts"]["total"] == 1
    harness.run("rootlock-full", harness.ALL)
    assert harness.runs[-1]["counts"]["passed"] == 77
    assert harness.WRITER.read_bytes() == original
    assert subprocess.check_output(["git", "ls-files", "-s", "src"], cwd=ROOT, text=True) == production_before
    subprocess.run(["git", "diff", "--exit-code", "--", "src"], cwd=ROOT, check=True)
    receipt = {
        "test": TEST,
        "qualification": "Same-process independently owned FileStream; real OS FileShare.None denial, not a second-process claim. Target root registry absent before the holder and after each failed Prepare/Append.",
        "mutation": "Only the recognized OS-open sharing-error catch sets failedOsOpen. Its outer catch releases Monitor but skips RootLease.Dispose/reference retirement. All other cleanup remains unchanged.",
        "oracle": "Same test fails at retained target-key RootGates entry after asserting Unavailable/COORD_WRITER_BUSY, unchanged accepted bytes, valid original holder and continued OS exclusion.",
        "green": "3 Prepare + 3 Append denials; original handle remains valid; after disposing it, pending Append and fresh Prepare+Append succeed with exact native bytes and 3 parsed events.",
        "source_restored_byte_exact": True,
        "production_index_blobs_unchanged": True,
        "production_worktree_diff_empty": True,
        "existing_root_holder": "Append_HeldThroughFlush_NormalizedAliasWriterFailsFastAndGateReclaims remains unchanged; it qualifies the local monitor branch, not this OS-open branch.",
        "historical_evidence": "Untouched; earlier AccessDenied failed probe and abort/zero-test qualifications are not RED evidence here.",
        "remaining": ["Independent Test/DS re-gate and canonical parent Proof Pack attachment",
                      "Global-128 emitter Prepared/notices", "Old-binary qualification",
                      "P2 bridge/recovery", "P3-P5"],
        "execution_graph": "Read/author -> original -> isolated mutant -> exact restoration -> full suite -> receipt. Serial builds; no agents; bounded 3 repeats per operation.",
    }
    (harness.EVIDENCE / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(receipt, indent=2), flush=True)


if __name__ == "__main__":
    main()
