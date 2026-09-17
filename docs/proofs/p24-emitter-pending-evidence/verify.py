"""Run bounded selected tests, preserving raw streams, TRX counters and exact pins."""
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import time
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
OUT = Path(__file__).resolve().parent
name = sys.argv[1]
selected = sys.argv[2] if len(sys.argv) > 2 else (
    "FullyQualifiedName~CoordinationProducerTests|FullyQualifiedName~CoordinationPreparedWriteTests|"
    "FullyQualifiedName~SessionCoordinationEmitterTests|FullyQualifiedName~CoordinationContractLogTests|"
    "FullyQualifiedName~CoordinationContractTests|FullyQualifiedName~CoordinationEmitterPendingTests")
command = ["dotnet", "test", "tests\\AiDe.Core.Tests\\AiDe.Core.Tests.csproj", "--no-restore",
           "--filter", selected, "--logger", f"trx;LogFileName={name}.trx",
           "--logger", "console;verbosity=normal", "--results-directory", str(OUT), "--verbosity", "quiet"]
started = time.monotonic()
run = subprocess.run(command, cwd=ROOT, capture_output=True, text=True, encoding="utf-8",
                     errors="replace", timeout=240, check=False)
(OUT / f"{name}.stdout.txt").write_text(run.stdout + "\nSTDERR:\n" + run.stderr, encoding="utf-8")
trx = OUT / f"{name}.trx"
counts = None
if trx.exists():
    node = ET.parse(trx).find(".//{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}Counters")
    counts = node.attrib if node is not None else None
paths = list((ROOT / "src" / "AiDe.Core" / "Watcher").glob("*Coordination*.cs"))
paths += list((ROOT / "tests" / "AiDe.Core.Tests" / "Watcher").glob("*Coordination*.cs"))
paths += [ROOT / "tests" / "AiDe.Core.Tests" / "AiDe.Core.Tests.csproj",
          ROOT / "src" / "AiDe.Core" / "bin" / "Debug" / "net10.0" / "AiDe.Core.dll",
          ROOT / "tests" / "AiDe.Core.Tests" / "bin" / "Debug" / "net10.0" / "AiDe.Core.Tests.dll"]
record = {"command": command, "exit": run.returncode, "duration_seconds": time.monotonic() - started,
          "counts": counts, "pins": {str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest()
                                    for p in paths if p.exists()}}
(OUT / f"{name}.pins.json").write_text(json.dumps(record, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"exit": run.returncode, "counts": counts, "duration_seconds": record["duration_seconds"]}))
if run.returncode:
    print((run.stdout + run.stderr)[-14000:])
sys.exit(run.returncode)
