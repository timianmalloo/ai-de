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

TWO CHECKS RUN HERE, NOT THROUGH THE PACK RUNNER. The runner enumerates `verify-*.py` files and runs
each argument-free. Neither of these is one, and neither is worth a new gate file: they are this
repository's knowledge about its own gate line, which is exactly what this file is for.

  control currency — Ruling 131 (c). Does `origin/main` carry commits touching `tools/` or
      `docs/ai-forward-pack/scripts/` since this tree's merge-base? If so the line REFUSES before
      running anything: every gate below would then be the version this tree has, not the version
      main enforces, and a green run would be evidence about a control set that no longer exists.
      It never fetches — a gate does not touch the network or move a ref — so it reads `origin/main`
      as this tree already has it, and when that ref does not resolve it prints
      `control currency: not recorded` and goes on (IO12: degrade, and say so, never to a plausible
      wrong number). The same computation is going into `coord claim`; it is written out here rather
      than imported so that neither file has to be edited to keep the other true.

  docs-graph validate — Ruling 125 (ii). A join passed 38/38 over a BROKEN documentation link graph,
      twice, because nothing in the line ever ran `validate`: `tools/regenerate-derived.py` runs
      `derive`, which REGENERATES the index and therefore silences index drift while leaving a
      dangling link target exactly as dangling as it was. The two are not the same check and the
      first was standing in for the second. Its findings are the graph's defects — dangling link
      target, unregistered rel, duplicate id, orphan, index drift; `review-suggested` and `review-by`
      decay stay warnings inside docs-graph.py itself (V16), so this cannot redden main for doing the
      propagation the mandate requires.

Every other argument (`--skip`, `--dir`, `--budget`, `--self-test`, ...) is passed through.
Exit status is the WORST of the three: 0 when the two checks and every gate passed, 1 when any
failed, 2 on a usage error. `--help` and `--self-test` are the pack runner's alone — they are
asking about the runner, not about this repository — so the two checks stay out of their way.
"""
from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RUNNER = ROOT / "docs" / "ai-forward-pack" / "scripts" / "run-verify-gates.py"
DOCS_GRAPH = ROOT / "docs" / "ai-forward-pack" / "scripts" / "docs-graph.py"
REPO_ARGS = [
    "--args", "verify-test-run.py=--no-run",
    "--args", "verify-front-door-exit-evidence.py=--self-test",
]

# Where this repository's controls live. A commit on main touching either of these changes what the
# gate line MEANS, which is why currency is measured against these two paths and not the whole tree.
CONTROL_PATHS = ("tools", "docs/ai-forward-pack/scripts")

# Enough of a list to act on, never the whole log: a refusal nobody finishes reading is a refusal
# nobody acts on.
SHOWN = 10


def _git(*args: str) -> tuple[int, str]:
    """(exit status, stdout). Read-only calls only: this runs inside a gate line."""
    try:
        done = subprocess.run(["git", *args], cwd=ROOT, capture_output=True, text=True,
                              encoding="utf-8", errors="replace", timeout=60, check=False)
    except (OSError, subprocess.SubprocessError):
        return 1, ""

    return done.returncode, done.stdout.strip()


def control_currency() -> tuple[bool, list[str]]:
    """(current, lines to print). Ruling 131 (c).

    The question is narrow on purpose: not "is this tree behind main" (it always is, and saying so
    every run is how a line gets ignored) but "is it behind main IN ITS CONTROLS". A lane that is
    fifty commits behind on product code still proves what it claims; a lane running last week's
    gates does not, and nothing in its output would say so.
    """
    code, remote = _git("rev-parse", "--verify", "--quiet", "origin/main")

    if code != 0 or not remote:
        return True, ["control currency: not recorded — origin/main does not resolve in this tree, "
                      "and a gate never fetches. The line continues (IO12)."]

    code, base = _git("merge-base", "HEAD", "origin/main")

    if code != 0 or not base:
        return True, ["control currency: not recorded — HEAD and origin/main have no merge-base "
                      "this tree can compute. The line continues (IO12)."]

    code, out = _git("log", "--no-merges", "--format=%h %ad %s", "--date=short",
                     f"{base}..origin/main", "--", *CONTROL_PATHS)

    if code != 0:
        return True, ["control currency: not recorded — `git log` failed here. The line continues "
                      "(IO12)."]

    commits = [line for line in out.splitlines() if line.strip()]

    if not commits:
        return True, [f"control currency: OK — origin/main ({remote[:8]}) carries no commit touching "
                      f"{' or '.join(CONTROL_PATHS)} since this tree's merge-base {base[:8]}."]

    lines = [f"control currency: REFUSED — origin/main ({remote[:8]}) carries {len(commits)} "
             f"commit(s) touching this repository's controls since merge-base {base[:8]}:"]
    lines += [f"    {commit}" for commit in commits[:SHOWN]]

    if len(commits) > SHOWN:
        lines.append(f"    … and {len(commits) - SHOWN} more")

    lines.append(
        "  Running the line now would prove this tree's gates, not the ones main enforces — a green "
        "result about a control set that no longer exists is worse than no result, because it is "
        "quoted as if it were one.")
    lines.append(
        "  Remedy: bring the controls forward in THIS tree — `git merge origin/main` (or rebase onto "
        "it) — and run the line again. If that merge is not yours to make, take it to the conductor: "
        "the answer is never to skip the line.")

    return False, lines


def docs_graph_validate() -> tuple[bool, list[str]]:
    """(ok, lines to print). Ruling 125 (ii).

    The JSON report is parsed rather than relayed: `validate` prints every artifact's tally, which in
    this repository is hundreds of lines, and a finding buried in a data dump is a finding nobody
    reads. The cardinality is kept on the OK line (IO: a gate that prints only "OK" gives the next
    run nothing to disagree with), and every defect is printed on the failing one.
    """
    if not DOCS_GRAPH.is_file():
        return False, [f"docs-graph validate: MISSING at {DOCS_GRAPH} — re-run /updatepack. A gate "
                       "that cannot be started is a failure, never a skip."]

    try:
        done = subprocess.run([sys.executable, str(DOCS_GRAPH), "validate"], cwd=ROOT,
                              capture_output=True, text=True, encoding="utf-8", errors="replace",
                              timeout=300, check=False)
    except (OSError, subprocess.SubprocessError) as error:
        return False, [f"docs-graph validate: could not run — {error}. A gate that cannot be started "
                       "is a failure, never a skip."]

    try:
        report = json.loads(done.stdout)
    except (json.JSONDecodeError, ValueError):
        report = None

    if done.returncode == 0 and isinstance(report, dict):
        return True, [f"docs-graph validate: OK — {report.get('artifacts')} artifact(s), "
                      f"{report.get('defects')} defect(s), {report.get('suggestions')} suggestion(s) "
                      "(review-suggested/review-by decay warn inside docs-graph.py, V16)."]

    lines = ["docs-graph validate: FAILED — the documentation link graph is broken."]

    if isinstance(report, dict):
        findings = [f"{p.get('file')}: {p.get('problem')}" for p in report.get("problems", [])]
        findings += [f"orphan (V10): {orphan}" for orphan in report.get("orphans", [])]
        findings += [f"index drift (V11): {drift}" for drift in report.get("index_drift", [])]
        lines += [f"    {finding}" for finding in findings[:SHOWN]]

        if len(findings) > SHOWN:
            lines.append(f"    … and {len(findings) - SHOWN} more")
    else:
        tail = [line for line in (done.stdout + done.stderr).splitlines() if line.strip()]
        lines.append(f"    {tail[-1] if tail else '(no output)'} (exit {done.returncode})")

    lines.append(
        "  Remedy: a dangling link target names an id no artifact carries — fix the link, land the "
        "artifact it names, or remove the link. Index drift is `python "
        "docs/ai-forward-pack/scripts/docs-graph.py derive`, which tools/regenerate-derived.py also "
        "runs; note that derive CANNOT clear a dangling target, which is why that line passing was "
        "never evidence this one would.")
    lines.append(
        "  If the artefact named is another session's, do not edit it: take the finding to the "
        "conductor with this line, and let its owner repair the link.")

    return False, lines


def main(argv: list[str]) -> int:
    if not RUNNER.is_file():
        print(f"run-verify-gates: the pack runner is missing at {RUNNER} — re-run /updatepack", file=sys.stderr)
        return 2

    # `--help` and `--self-test` are questions about the RUNNER. Answering them with a currency
    # refusal, or making them wait on a graph walk, would be this file getting in the way of the
    # thing it delegates to.
    if any(argument in ("-h", "--help", "--self-test") for argument in argv):
        sys.stdout.flush()
        return subprocess.call([sys.executable, str(RUNNER), *REPO_ARGS, *argv], cwd=ROOT)

    current, lines = control_currency()

    for line in lines:
        print(line)

    if not current:
        print()
        print("run-verify-gates: REFUSED before the first gate — this tree's controls are behind "
              "origin/main.")
        return 1

    print()

    # FLUSH BEFORE THE CHILD. Redirected to a file, Python block-buffers this process's stdout while
    # the pack runner writes to the same descriptor directly — so everything printed above arrived
    # AFTER the whole gate table, at the one place an operator actually reads. Measured, not reasoned:
    # the first redirected run of this file put `control currency: OK` below the runner's verdict.
    sys.stdout.flush()
    status = subprocess.call([sys.executable, str(RUNNER), *REPO_ARGS, *argv], cwd=ROOT)
    sys.stdout.flush()

    if status == 2:
        # A usage error means the pack runner enumerated nothing. There is no gate line to add to.
        return 2

    ok, lines = docs_graph_validate()
    print()

    for line in lines:
        print(line)

    print()

    if status == 0 and ok:
        print("run-verify-gates: OK — the pack runner's gates, and docs-graph validate.")
        return 0

    failed = [name for name, bad in (("the pack runner's gates", status != 0),
                                     ("docs-graph validate", not ok)) if bad]
    print(f"run-verify-gates: FAILED — {' and '.join(failed)}. The line's exit status is the worst "
          "of them; nothing above is a skip.")
    return 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
