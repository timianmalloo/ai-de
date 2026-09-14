#!/usr/bin/env python3
"""run-verify-gates.py — this repository's gate line, delegating to the pack's runner.

The runner itself is `docs/ai-forward-pack/scripts/run-verify-gates.py` (pack revision 70 lifted
the local copy that measured DC-113's third recurrence, and generalised it); this file is the ONE
place that carries what only this repository knows — the per-gate arguments below — so the
familiar line `python tools/run-verify-gates.py` keeps its meaning and no operator has to
remember a flag (the pack runner also runs its own two gates under docs/ai-forward-pack/scripts/,
which replaced the tools/ copies of the conflict-marker and console-launch gates at revision 70).
One definition of the runner (DM7): if the runner has a defect, the fix lands in the pack copy.

Per-gate arguments this repository needs (the pack runner runs every gate argument-free):
  verify-test-run.py                  --no-run     it reads the last trx files; bare, it runs the suite
  verify-front-door-exit-evidence.py  --self-test  F5's nine-clause oracle, byte-identical to its
                                                   commit 1374401d (its clause 0 demands that), so it
                                                   keeps proving it can fail and is never edited. The
                                                   record landed 2026-09-14 on the operator's word
                                                   (Ruling 103); the BARE reading of it is the
                                                   sibling gate verify-front-door-exit-attended.py,
                                                   which runs argument-free here and reports each
                                                   clause MET / NOT MET from the untouched oracle.

Every other argument (`--skip`, `--dir`, `--budget`, `--self-test`, ...) is passed through.
Exit status is the pack runner's: 0 when every gate passed, 1 when any failed, 2 on a usage error.
"""
from __future__ import annotations

import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RUNNER = ROOT / "docs" / "ai-forward-pack" / "scripts" / "run-verify-gates.py"
REPO_ARGS = [
    "--args", "verify-test-run.py=--no-run",
    "--args", "verify-front-door-exit-evidence.py=--self-test",
]


def main(argv: list[str]) -> int:
    if not RUNNER.is_file():
        print(f"run-verify-gates: the pack runner is missing at {RUNNER} — re-run /updatepack", file=sys.stderr)
        return 2
    return subprocess.call([sys.executable, str(RUNNER), *REPO_ARGS, *argv], cwd=ROOT)


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
