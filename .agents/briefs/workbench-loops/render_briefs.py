"""Render one self-contained brief per track: the track's specific brief + the substituted common rules."""
import pathlib
import sys

HERE = pathlib.Path(__file__).parent
TRACKS = {
    "T1": ("wl-t1-wl0", "wl0-evidence", "docs/proof/workbench-loops-wl-0", "Sonnet 5.5"),
    "T2": ("wl-t2-sp1", "sp1-schema-v2", "docs/proof/workbench-loops-wl-1a/sp1", "Sonnet 5.5"),
    "T3": ("wl-t3-sp2", "sp2-runevent-vocab", "docs/proof/workbench-loops-wl-1a/sp2", "Sonnet 5.5"),
    "T4": ("wl-t4-sp3", "sp3-perspective-rerow", "docs/proof/workbench-loops-wl-s/sp3", "Opus 5.5"),
    "T5": ("wl-t5-host", "wl1a-attempt-host", "docs/proof/workbench-loops-wl-1a/host", "Opus 5.5"),
    "T6": ("wl-t6-folds", "wl1a-folds", "docs/proof/workbench-loops-wl-1a/folds", "Sonnet 5.5"),
    "T7": ("wl-t7-redact", "wl1a-redaction", "docs/proof/workbench-loops-wl-1a/redaction", "Sonnet 5.5"),
    "T8": ("wl-t8-bind", "wl1a-notebook-binding", "docs/proof/workbench-loops-wl-1a/binding", "Opus 5.5"),
    "T9": ("wl-t9-spike", "wls-unparent-spike", "docs/proof/workbench-loops-wl-s/spike", "Sonnet 5.5"),
    "T10": ("wl-t10-e1", "e1-notebook-spike", "docs/proof/workbench-loops-wl-2/e1", "Opus 5.5"),
    "T11": ("wl-t11-kinds", "wls-kinds-migration", "docs/proof/workbench-loops-wl-s/kinds", "Opus 5.5"),
    "T12": ("wl-t12-census", "census-flake", "docs/proof/workbench-loops-support/census", "Sonnet 5.5"),
    "T2b": ("wl-t2-sp1", "sp1-schema-v2", "docs/proof/workbench-loops-wl-1a/sp1", "Sonnet 5.5"),
    "T1b": ("wl-t1-wl0", "wl0-evidence", "docs/proof/workbench-loops-wl-0", "Sonnet 5.5"),
    "T2c": ("wl-t2-sp1", "sp1-schema-v2", "docs/proof/workbench-loops-wl-1a/sp1", "Sonnet 5.5"),
    "T4b": ("wl-t4-sp3", "sp3-perspective-rerow", "docs/proof/workbench-loops-wl-s/sp3", "Sonnet 5.5"),
    "T13": ("wl-t13-hygiene", "gate-hygiene", "docs/proof/workbench-loops-support/hygiene", "Sonnet 5.5"),
    "T12b": ("wl-t12-census", "census-flake", "docs/proof/workbench-loops-support/census", "Sonnet 5.5"),
    "T13b": ("wl-t13-hygiene", "gate-hygiene", "docs/proof/workbench-loops-support/hygiene", "Sonnet 5.5"),
    "T12c": ("wl-t12-census", "census-flake", "docs/proof/workbench-loops-support/census", "Sonnet 5.5"),
    "T14": ("wl-t14-nbops", "wl1a-notebook-ops", "docs/proof/workbench-loops-wl-1a/notebook-ops", "Sonnet 5.5"),
    "T15": ("wl-t15-idgate", "account-identity-gate", "docs/proof/workbench-loops-support/identity-gate", "Sonnet 5.5"),
    "T16": ("wl-t16-bindrow", "wl1a-attempt-binding-row", "docs/proof/workbench-loops-wl-1a/binding-row", "Sonnet 5.5"),
    "T17": ("wl-t17-restore", "wl1a-restored-turns", "docs/proof/workbench-loops-wl-1a/restored-turns", "Opus 5.5"),
    "T18": ("wl-t18-knowledge", "wls-knowledge-mode", "docs/proof/workbench-loops-wl-s/knowledge-mode", "Opus 5.5"),
    "T21": ("wl-t21-float", "wls-float", "docs/proof/workbench-loops-wl-s/float", "Opus 5.5"),
    "T22": ("wl-t22-wl1b-design", "wl1b-design", "docs/proof/workbench-loops-wl-1b/design", "Opus 5.5"),
    "T23": ("wl-t23-wl1b-folds", "wl1b-folds", "docs/proof/workbench-loops-wl-1b/folds", "Sonnet 5.5"),
}

common = (HERE / "brief-common.md").read_text(encoding="utf-8")
for key in sys.argv[1:] or TRACKS:
    track, branch, proof, model = TRACKS[key]
    tree = "C:\\Projects\\ai-de-" + branch
    rules = (common.replace("{TRACK}", track).replace("{BRANCH}", branch)
             .replace("{TREE}", tree).replace("{PROOF}", proof).replace("{MODEL}", model))
    specific = (HERE / f"specific-{key}.md").read_text(encoding="utf-8").replace("{PROOF}", proof)
    out = HERE / f"brief-{key}-{track}.md"
    out.write_text(specific + "\n" + rules, encoding="utf-8", newline="\n")
    print(out)
