import hashlib
import json
from pathlib import Path
import subprocess
import sys
import time

ROOT = Path(__file__).resolve().parents[4]
EVIDENCE = ROOT / "docs" / "proof" / "records" / "p25-portability"
EVIDENCE.mkdir(parents=True, exist_ok=True)
WSL = ["wsl", "--distribution", "Ubuntu-24.04", "--exec"]
HOME = "/home/timmall/.aide-p25-b0d0"
TREE = subprocess.check_output(WSL + ["wslpath", "-a", str(ROOT)], text=True).strip()
CACHE = subprocess.check_output(WSL + ["wslpath", "-a", str(Path.home() / ".nuget" / "packages")], text=True).strip()
ENV = ["env", f"HOME={HOME}/home", f"DOTNET_ROOT={HOME}/sdk", f"DOTNET_CLI_HOME={HOME}/cli",
       f"NUGET_PACKAGES={HOME}/packages", f"TMPDIR={HOME}/fixtures",
       "DOTNET_CLI_TELEMETRY_OPTOUT=1", "DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1",
       "DOTNET_GENERATE_ASPNET_CERTIFICATE=false"]
PROJECT = "tests/AiDe.Core.Tests/AiDe.Core.Tests.csproj"
ORDINARY = "FullyQualifiedName~TheAgentIsToldWhatChanged_AndWhy|FullyQualifiedName~TheContractPumpCannotSeeTheNotice"


def run(label, command):
    started = time.monotonic()
    result = subprocess.run(command, cwd=ROOT, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    (EVIDENCE / f"{label}.log").write_bytes(result.stdout)
    record = {"command": command, "exit": result.returncode, "seconds": time.monotonic() - started,
              "head": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip(),
              "source": {str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest()
                         for p in [ROOT / "src/AiDe.Core/Watcher/RegistrationPublisher.cs",
                                   ROOT / "src/AiDe.Core/Watcher/RegistrationPublicationUnix.cs",
                                   ROOT / "tests/AiDe.Core.Tests/Watcher/PortableRegistrationPublicationTests.cs"] if p.exists()}}
    (EVIDENCE / f"{label}.json").write_text(json.dumps(record, indent=2), encoding="utf-8")
    print(f"{label}: exit={result.returncode} seconds={record['seconds']:.2f}")
    print(result.stdout.decode("utf-8", errors="replace")[-6500:])
    return result.returncode


mode = sys.argv[1]
if mode == "restore":
    code = run("linux-restore", WSL + ENV + [f"{HOME}/sdk/dotnet", "restore", f"{TREE}/{PROJECT}",
               "--source", CACHE, "--packages", f"{HOME}/packages", "--artifacts-path", f"{HOME}/build",
               "-p:NuGetAudit=false"])
elif mode == "spike":
    code = run("linux-api-spike", WSL + ["python3", f"{TREE}/docs/proof/records/p25-portability/spike.py", f"{HOME}/fixtures"])
elif mode.startswith("linux-"):
    selector = ORDINARY if mode == "linux-baseline" else ORDINARY + "|FullyQualifiedName~PortableRegistrationPublicationTests"
    if len(sys.argv) > 2:
        selector = sys.argv[2]
    code = run(mode, WSL + ENV + [f"{HOME}/sdk/dotnet", "test", f"{TREE}/{PROJECT}", "--no-restore",
               "--artifacts-path", f"{HOME}/build", "--filter", selector,
               "--logger", f"trx;LogFileName={mode}.trx", "--results-directory", f"{TREE}/docs/proof/records/p25-portability",
               "-p:NuGetAudit=false"])
else:
    selector = sys.argv[2]
    code = run(mode, ["dotnet", "test", PROJECT, "--no-restore", "--filter", selector,
               "--logger", f"trx;LogFileName={mode}.trx", "--results-directory", str(EVIDENCE)])
sys.exit(code)
