"""Ruling 151 step-8 comparator: run every verify gate in a parent tree and a head tree back to back, and
compare at finding level.

Usage: python -I gate_ratchet.py <parent_tree> <head_tree> <out_dir>
Exit 0 = the ratchet passes (every head finding exists at the parent; no gate green at the parent is red at the
head); 1 = it refuses; 2 = usage. The three gates Ruling 151 (3) never ratchets are reported but excluded: their
evidence is the conductor recount.
"""
import json
import pathlib
import re
import subprocess
import sys

NEVER_RATCHETED = {"verify-test-run.py", "verify-terminal-host-exit-paths.py", "verify-project-coverage.py"}
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


CITED = re.compile(r"^(?:Ruling (\d+)(?: / R-\d+)?|R-(\d+)) is cited as authority .* no heading in docs/notes/rulings\.md defines it")


def ruling_filter(tree, findings):
    """Ruling 153: drop "Ruling N cited, no heading in rulings.md" when addendum-c has exactly one Ruling N heading."""
    register = tree / "docs" / "notes" / "addendum-c-council-rulings.md"
    text = register.read_text(encoding="utf-8") if register.exists() else ""
    kept, dropped = [], []
    for f in findings:
        m = CITED.match(f)
        n = (m.group(1) or m.group(2)) if m else None
        if n and len(re.findall(rf"^#{{2,3}}[ \t]+Ruling[ \t]+{n}\b", text, re.M)) == 1:
            dropped.append(f)
        else:
            kept.append(f)
    return kept, dropped


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
    dropped = []
    if name == "verify-ruling-citations.py":
        findings, dropped = ruling_filter(tree, findings)
    return {"exit": code, "findings": findings, "output": out, "filtered": dropped}


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
        verdict = "excluded (recount)" if name in NEVER_RATCHETED else (
            "REFUSE green->red" if green_to_red else ("REFUSE new findings" if new else "ok"))
        if verdict.startswith("REFUSE"):
            refusals.append(gate)
        result["gates"][gate] = {"parent_exit": p["exit"], "head_exit": h["exit"], "new_findings": new,
                                 "verdict": verdict, "filtered_parent": len(p.get("filtered", [])),
                                 "filtered_head": len(h.get("filtered", []))}
        if p.get("filtered") or h.get("filtered"):
            print(f"  Ruling 153 filter on {gate}: dropped parent={len(p.get('filtered', []))} head={len(h.get('filtered', []))}",
                  flush=True)
        (out_dir / (name + ".parent.txt")).write_text(p["output"], encoding="utf-8")
        (out_dir / (name + ".head.txt")).write_text(h["output"], encoding="utf-8")
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
