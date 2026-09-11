#!/usr/bin/env python3
"""The UI craft floor: a real threshold, over committed source, for the artifact under review.

RULING 48. AGENTS.md requires the craft floor be gated in CI on the grounds that "a lesson
recorded as prose is a memoir" (CI6). It was not gated, and two separate defects kept it from
being gateable:

  DC-133 - the threshold could not fire. `ui-craft-gate.py --gate` exits 1 only on a
  Blocker-mapped finding, and NO rule in the detector's set emits a Blocker unless
  --a11y-obligation is passed. Measured: `--gate` over docs/mockups exits 0 with 66 Majors and
  38 Minors. The step was advisory by ARITHMETIC rather than by decision, which is the part that
  matters -- nobody chose it, so nobody could be asked to defend it.

  DC-006 recurrence - the corpus included generated output. The pack script hands its targets
  straight to the detector with no bin/obj exclusion, so scanning `src/AiDe.App` walks
  `Web/composer.html` AND the git-ignored copies of it under bin/Debug and bin/Release. MEASURED,
  and the arithmetic is exact: `src/AiDe.App/Web` alone reports 17 Majors, all from committed
  source; `src/AiDe.App` reports 51. 51 = 17 x 3 -- source, Debug copy, Release copy. Every
  finding was reported three times and two thirds of them described whatever was last compiled.

WHY THIS IS A REPO-OWNED WRAPPER AND NOT AN EDIT TO THE PACK SCRIPT. Ruling 48 admitted "a
--fail-on <severity> option in ui-craft-gate.py". That script lives in docs/ai-forward-pack/,
whose entire history is pack-revision commits; pack-apply.py three-way-merges repo-local
deviations and, on conflict, PARKS the incoming text under docs/ai-forward-pack/conflicts/ for
manual reconciliation. A threshold this repository's CI depends on should not live somewhere a
future pack update can send it to a conflicts directory. tools/regenerate-derived.py and
tools/verify-derived-views.py already wrap pack scripts, so the shape is established. The
ruling's SUBSTANCE is unchanged: corpus pinned first, threshold scoped to the artifact under
review. Recorded as a deviation from its stated mechanism in front-door-rulings-45-48.md.

THE TWO THINGS THIS ASSERTS THAT THE PACK SCRIPT CANNOT:

  1. A gated target must be COMMITTED SOURCE. A target that resolves under a build-output
     directory is refused outright -- not filtered, refused, because a gate that silently drops
     part of its corpus is the defect one level down. The explicit path is the fix; Ruling 48
     cuts any new exclusion machinery.
  2. The corpus must be NON-EMPTY. A detector that scanned nothing reports exactly like a clean
     one (DC-006's original instance, CD9).

WHAT IS GATED AND WHAT IS NOT, deliberately and per Ruling 48:

  GATED at Major   DESIGN.md and EVERY docs/mockups/*.html not named in LEGACY_ADVISORY --
                   today session-front-door.html (0 Majors / 5 Minors), perspective-shell.html,
                   conversation-composer.html, new-session-sheet.html (0 findings each). A new
                   mockup is gated by default; the exemption is the thing that must be written.
  GATED at Major   src/AiDe.App/Web -- was ADVISORY at 17 Majors, every one a colour literal in
                   the composer HTML; promoted the day the composer page was tokenized (INV-0008
                   Fix C: CSS custom properties pushed on host.init, DESIGN.md values as the
                   fallbacks; the host-probe page carries the token values by hand). Measured
                   0 findings at promotion. A gate that is red on day one gets muted, which is
                   why it waited; a gate that is green on day one and stays wired is the control.
  NOT SCANNED      the twelve legacy mockups in LEGACY_ADVISORY -- 60 Majors (2026-09-11) the
                   ui-craft.yml header records as deliberate DX17 dense-meta text. They gate when
                   docs/reviews/ui-mockups-craft-gate.md dispositions them.
"""

from __future__ import annotations

import argparse
import json
import pathlib
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
PACK_GATE = ROOT / "docs" / "ai-forward-pack" / "scripts" / "ui-craft-gate.py"

SEVERITY_ORDER = ["Nit", "Minor", "Major", "Blocker"]

# THE DEFAULT IS GATED (Addendum C, D1). The first shape of this list named the gated files
# one by one, which meant every mockup authored after it entered the corpus ungated and
# drifted silently until someone remembered the list -- the allow-list was the defect.
# Now every docs/mockups/*.html is gated at Major unless it is named in LEGACY_ADVISORY
# with the reason it is exempt; a new mockup is gated on the day it is committed.
LEGACY_ADVISORY = {
    # The legacy IDE mockups carry deliberate dense-meta text (DX17) -- 60 Majors in the
    # 2026-09-11 measurement. They promote to gated when
    # docs/reviews/ui-mockups-craft-gate.md dispositions them (ui-craft.yml header).
    "activity-rail.html", "app-facelift.html", "context-map-join.html", "editor-surfaces.html",
    "facelift-elevate.html", "graph-canvas.html", "knowledge-explorer-mode.html",
    "knowledge-explorer.html", "named-dock-zones.html", "uml-erm-surfaces.html",
    "watcher-observatory.html", "workbench.html",
}

def gated_mockups():
    """Every committed mockup not explicitly exempted, at Major."""
    folder = ROOT / "docs" / "mockups"
    return [("docs/mockups/" + p.name, "Major")
            for p in sorted(folder.glob("*.html")) if p.name not in LEGACY_ADVISORY]

# Targets whose findings FAIL this gate, with the severity each fails at.
GATED = [("DESIGN.md", "Major"),
         # Promoted from ADVISORY when the composer HTML was tokenized (ranked-plan item 13, INV-0008).
         ("src/AiDe.App/Web", "Major")] + gated_mockups()

# Scanned and reported, never failed. Each carries the condition that would promote it.
ADVISORY: list[tuple[str, str]] = []

BUILD_OUTPUT = {"bin", "obj", "node_modules", "artifacts"}


def at_or_above(severity, threshold):
    try:
        return SEVERITY_ORDER.index(severity) >= SEVERITY_ORDER.index(threshold)
    except ValueError:
        # An unknown severity is never silently dropped: treat it as failing.
        return True


def is_build_output(path):
    return any(part in BUILD_OUTPUT for part in pathlib.PurePath(path).parts)


def scan(target):
    """Findings for one target, or None when the detector could not be run."""
    result = subprocess.run(
        [sys.executable, str(PACK_GATE), str(ROOT / target), "--json"],
        capture_output=True, text=True, check=False, cwd=str(ROOT))
    if not result.stdout.strip():
        return None
    try:
        return json.loads(result.stdout)
    except json.JSONDecodeError:
        return None


def check(gated=None, advisory=None):
    gated = GATED if gated is None else gated
    advisory = ADVISORY if advisory is None else advisory
    defects = []
    report = []

    for target, threshold in gated:
        if is_build_output(target):
            defects.append(
                f"{target} is a gated target that resolves under a build-output directory. A "
                f"craft finding there describes whatever was last compiled, not the repository. "
                f"Pin the target to committed source (Ruling 48).")
            continue
        if not (ROOT / target).exists():
            defects.append(f"{target} is gated and does not exist. A gate whose subject is "
                           f"absent reports exactly like a gate whose subject is clean.")
            continue

        findings = scan(target)
        if findings is None:
            defects.append(f"{target}: the detector produced no JSON, so nothing was scanned. "
                           f"A detector that scanned nothing reports like a clean one (CD9).")
            continue

        failing = [f for f in findings
                   if at_or_above(str(f.get("severity", "")), threshold)]
        report.append(f"  gated   {target}: {len(findings)} finding(s), "
                      f"{len(failing)} at or above {threshold}")
        for f in failing:
            defects.append(
                f"{target}: [{f.get('severity')}] {f.get('rule')} ({f.get('directive')}) - "
                f"{f.get('evidence')}")

    for target, condition in advisory:
        findings = scan(target)
        if findings is None:
            report.append(f"  advisory {target}: nothing scanned")
            continue
        majors = [f for f in findings if at_or_above(str(f.get("severity", "")), "Major")]
        from_build = [f for f in findings
                      if is_build_output(str(f.get("location", "")).replace("\\", "/"))]
        report.append(f"  advisory {target}: {len(findings)} finding(s), {len(majors)} Major, "
                      f"{len(from_build)} from build output - {condition}")

    return defects, report


def self_test():
    """Break the configuration and require the gate to notice. DC-104."""
    cases = []
    failures = []

    # A gated target under build output must be refused, not filtered.
    defects, _ = check(gated=[("src/AiDe.App/bin/Debug", "Major")], advisory=[])
    cases.append("build-output target refused")
    if not any("build-output directory" in d for d in defects):
        failures.append(f"build-output target: expected a refusal, got {defects}")

    # A gated target that does not exist must fail, not pass quietly.
    defects, _ = check(gated=[("docs/mockups/no-such-file.html", "Major")], advisory=[])
    cases.append("absent target refused")
    if not any("does not exist" in d for d in defects):
        failures.append(f"absent target: expected a defect, got {defects}")

    # The threshold must actually discriminate: Nit over a real artifact must fail where
    # Major passes, or the gate is DC-133 wearing a different number.
    nit, _ = check(gated=[("docs/mockups/session-front-door.html", "Nit")], advisory=[])
    major, _ = check(gated=[("docs/mockups/session-front-door.html", "Major")], advisory=[])
    cases.append("threshold discriminates")
    if not nit:
        failures.append("threshold: a Nit threshold found nothing on an artifact measured to "
                        "carry 5 Minors - the threshold does not discriminate, which is the "
                        "defect this gate exists to prevent")
    if major:
        failures.append(f"threshold: a Major threshold should pass on this artifact today, "
                        f"got {len(major)} defect(s)")

    if failures:
        print(f"self-test FAILED: {len(failures)} case(s).")
        for f in failures:
            print(f"  - {f}")
        return 1
    print(f"self-test: {len(cases)} cases - {', '.join(cases)}.")
    return 0


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--self-test", action="store_true",
                        help="prove this gate can fail")
    args = parser.parse_args()
    if args.self_test:
        return self_test()

    defects, report = check()
    for line in report:
        print(line)
    if defects:
        print(f"verify-ui-craft-floor: FAILED - {len(defects)} finding(s) at or above the "
              f"gated severity.")
        for d in defects:
            print(f"  - {d}")
        return 1
    print("verify-ui-craft-floor: OK - every gated artifact is clean at its threshold, over "
          "committed source.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
