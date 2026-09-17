"""Seal the P25 author receipts; no test exit is inferred from a shell pipeline."""
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import subprocess
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[4]
evidence = Path(__file__).parent
wsl = ["wsl", "--distribution", "Ubuntu-24.04", "--exec"]
home = "/home/timmall/.aide-p25-b0d0"
namespace = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
runs = {}
for file in sorted(evidence.glob("*.trx")):
    tree = ET.parse(file)
    counters = tree.find(".//t:Counters", namespace).attrib
    failures = []
    for result in tree.findall(".//t:UnitTestResult", namespace):
        if result.attrib["outcome"] == "Failed":
            message = result.find(".//t:Message", namespace)
            failures.append({"name": result.attrib["testName"], "message": message.text if message is not None else None})
    runs[file.stem] = {"counters": counters, "failures": failures}
assert runs["linux-baseline"]["counters"]["failed"] == "2"
assert runs["linux-verified"]["counters"]["passed"] == "19"
assert runs["windows-focus"]["counters"]["passed"] == "79"
assert runs["windows-floor"]["counters"]["passed"] == "252"
assert runs["windows-floor"]["counters"]["failed"] == "2"
expected = {"DrainRegistrationNotices_PublicationFails_RetryRetainsOriginalCorrection",
            "Register_TwoHostsHave128OutstandingCorrections_RefusesBeforeNativeMutation"}
assert {f["name"].split(".")[-1] for f in runs["windows-floor"]["failures"]} == expected
assert all("COORD_NATIVE_UNAVAILABLE" in f["message"] for f in runs["linux-baseline"]["failures"])
linux_pins = subprocess.check_output(wsl + ["sha256sum",
    f"{home}/build/bin/AiDe.Core/debug/AiDe.Core.dll",
    f"{home}/build/bin/AiDe.Core.Tests/debug/AiDe.Core.Tests.dll"], text=True)
windows_files = [root / "src/AiDe.Core/bin/Debug/net10.0/AiDe.Core.dll",
                 root / "tests/AiDe.Core.Tests/bin/Debug/net10.0/AiDe.Core.Tests.dll"]
files = [root / "global.json", root / "Directory.Packages.props",
         root / "src/AiDe.Core/Watcher/RegistrationPublisher.cs",
         root / "src/AiDe.Core/Watcher/RegistrationPublicationUnix.cs",
         root / "tests/AiDe.Core.Tests/Watcher/PortableRegistrationPublicationTests.cs",
         root / "tests/AiDe.Core.Tests/Watcher/AWorktreeRegistrationIsCorrectedAndSaidSoTests.cs"]
sdk = json.loads((root / ".p25-portability/sdk-source.json").read_text(encoding="utf-8-sig"))
(evidence / "sdk-source.json").write_text(json.dumps(sdk, indent=2), encoding="utf-8")
manifest = {
    "at": datetime.now(timezone.utc).isoformat(),
    "head": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=root, text=True).strip(),
    "windows_sdk": subprocess.check_output(["dotnet", "--info"], cwd=root, text=True),
    "linux_sdk": subprocess.check_output(wsl + [f"{home}/sdk/dotnet", "--info"], text=True),
    "linux_filesystem": subprocess.check_output(wsl + ["stat", "-f", "-c", "%T", f"{home}/fixtures"], text=True).strip(),
    "linux_binary_sha256": linux_pins,
    "sha256": {str(p.relative_to(root)): hashlib.sha256(p.read_bytes()).hexdigest() for p in files + windows_files},
    "runs": runs,
    "isolation": {"linux": home, "windows": str(root), "fixture_files_remaining":
        subprocess.check_output(wsl + ["find", f"{home}/fixtures", "-mindepth", "1", "-maxdepth", "1"], text=True).splitlines()},
}
(evidence / "final-manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
print(json.dumps({"linux": runs["linux-verified"]["counters"],
                  "windows": runs["windows-floor"]["counters"],
                  "filesystem": manifest["linux_filesystem"],
                  "fixture_files_remaining": manifest["isolation"]["fixture_files_remaining"],
                  "binary_sha256": linux_pins}, indent=2))
