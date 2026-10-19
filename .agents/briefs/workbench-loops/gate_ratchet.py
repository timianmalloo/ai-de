"""Ruling 151 step-8 comparator: run every verify gate in a parent tree and a head tree back to back, and
compare at finding level.

Usage: python -I gate_ratchet.py <parent_tree> <head_tree> <out_dir>
Exit 0 = the ratchet passes (every head finding exists at the parent; no gate green at the parent is red at the
head); 1 = it refuses; 2 = usage. The three gates Ruling 151 (3) never ratchets are reported but excluded: their
evidence is the conductor recount. Ruling 160: the pack copy of verify-ruling-citations is recorded but excluded
until PD-1 is fixed in the pack, and the repo copy is strict (it must exit 0 at the head).
"""
import json
import pathlib
import re
import subprocess
import sys

NEVER_RATCHETED = {"verify-test-run.py", "verify-terminal-host-exit-paths.py", "verify-project-coverage.py"}
# Ruling 160: recorded, never ratcheted, until PD-1 lands in the pack (it reads docs/notes/rulings.md only).
EXCLUDED_PD1 = {"docs/ai-forward-pack/scripts/verify-ruling-citations.py"}
# Ruling 160: must exit 0 at every join head, with no ratchet allowance.
STRICT = {"tools/verify-ruling-citations.py"}
GATE_ARGS = {"verify-test-run.py": ["--no-run"], "verify-front-door-exit-evidence.py": ["--self-test"], "docs-graph.py": ["validate"]}
GATE_DIRS = ["tools", "docs/ai-forward-pack/scripts"]
TIMEOUT = 600
FINDING = re.compile(r"^\s*[-•✗]\s+(.*\S)\s*$")


def gates(tree):
    found = []
    for d in GATE_DIRS:
        for p in sorted((tree / d).glob("verify-*.py")):
            found.append(p.relative_to(tree).as_posix())
    found.append("docs/ai-forward-pack/scripts/docs-graph.py")
    return found


def normalise(text, tree):
    for form in sorted({str(tree).replace("\\", "\\\\"), str(tree), str(tree).replace("\\", "/"), tree.as_posix()}, key=len, reverse=True):
        text = text.replace(form, "<ROOT>")
    return text


def run(tree, gate):
    name = pathlib.PurePosixPath(gate).name
    cmd = [sys.executable, gate, *GATE_ARGS.get(name, [])]
    try:
        p = subprocess.run(cmd, cwd=tree, capture_output=True, text=True, encoding="utf-8", errors="replace",
                           timeout=TIMEOUT)
        code, out = p.returncode, (p.stdout or "") + (p.stderr or "")
    except subprocess.TimeoutExpired:
        code, out = 124, f"TIMEOUT after {TIMEOUT}s"
    out = normalise(out, tree)
    findings = sorted({m.group(1) for line in out.splitlines() if (m := FINDING.match(line))})
    if code != 0 and not findings:
        findings = [f"exit {code}: " + (out.strip().splitlines() or ["no output"])[-1][:300]]
    return {"exit": code, "findings": findings, "output": out}


def main():
    if len(sys.argv) != 4:
        print(__doc__)
        return 2
    parent, head, out_dir = (pathlib.Path(a).resolve() for a in sys.argv[1:])
    out_dir.mkdir(parents=True, exist_ok=True)
    names = sorted(set(gates(parent)) | set(gates(head)))
    result = {"parent": str(parent), "head": str(head), "gates": {}}
    refusals = []
    for gate in names:
        p = run(parent, gate) if (parent / gate).exists() else {"exit": None, "findings": [], "output": "absent"}
        h = run(head, gate) if (head / gate).exists() else {"exit": None, "findings": [], "output": "absent"}
        name = pathlib.PurePosixPath(gate).name
        new = [f for f in h["findings"] if f not in p["findings"]] if h["exit"] not in (0, None) else []
        green_to_red = p["exit"] == 0 and h["exit"] not in (0, None)
        if name in NEVER_RATCHETED:
            verdict = "excluded (recount)"
        elif gate in EXCLUDED_PD1:
            verdict = "excluded (PD-1)"
        elif gate in STRICT:
            verdict = "ok (strict)" if h["exit"] == 0 else "REFUSE strict, head red"
        else:
            verdict = "REFUSE green->red" if green_to_red else ("REFUSE new findings" if new else "ok")
        if verdict.startswith("REFUSE"):
            refusals.append(gate)
        result["gates"][gate] = {"parent_exit": p["exit"], "head_exit": h["exit"], "new_findings": new,
                                 "verdict": verdict}
        # Keyed by the gate's path: two gates may share a basename (the pack and repo ruling-citations copies).
        stem = gate.replace("/", "__")
        (out_dir / (stem + ".parent.txt")).write_text(p["output"], encoding="utf-8")
        (out_dir / (stem + ".head.txt")).write_text(h["output"], encoding="utf-8")
        print(f"{verdict:22} {gate}  parent={p['exit']} head={h['exit']}" + (f"  new={len(new)}" if new else ""),
              flush=True)
        for f in new[:5]:
            print(f"    + {f[:240]}", flush=True)
    result["refusals"] = refusals
    (out_dir / "ratchet.json").write_text(json.dumps(result, indent=1), encoding="utf-8")
    print(f"ratchet: {'PASS' if not refusals else 'REFUSE'} — {len(refusals)} refusal(s) over {len(names)} gate(s)")
    return 0 if not refusals else 1


if __name__ == "__main__":
    sys.exit(main())
