#!/usr/bin/env python3
"""Every colour role in DESIGN.md has a value in every declared mode, and the on-accent ink exists.

THE CLASS (Addendum C, D1, 2026-09-11). DESIGN.md's Modes table said, for the light theme,
"inverted roles, same semantics" -- a rule with no values. The runtime contrast census
(INV-0007) therefore had nothing to measure for the light run and could only ever report
"not measured": a mode with a rule but no values is a mode that cannot fail, and a mode that
cannot fail is indistinguishable from one that was never checked. The values were declared as
flat `light-<role>` keys under `colors:` (the only form both `design-lint.py` and the impeccable
detector read). This script is the control that keeps them declared: the NEXT colour role added
to the dark set without a light twin fails here, on the day it is added, not when the census
finally runs light.

A second shape from the same investigation: 12 of 14 failing pairings were a light ink painted
on the accent ground because no state named an on-accent ink. DESIGN.md now declares
`accent-contrast` as the only ink any state may paint on `accent` (the code's AccentContrastBrush)
and `border-strong` as the control boundary; this script refuses a DESIGN.md that drops either, in
either mode.

Stdlib only. Exit 0 when clean, 1 otherwise. `--self-test` proves it can fail (DC-104).
"""

from __future__ import annotations

import argparse
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
DESIGN = ROOT / "DESIGN.md"
MODE_PREFIXES = ("light-",)
REQUIRED_ROLES = ("accent-contrast", "border-strong", "accent", "text", "text-muted", "text-disabled", "focus")


def colour_keys(text):
    """The keys declared directly under `colors:` in the frontmatter, in order."""
    lines = text.splitlines()
    if not lines or lines[0].strip() != "---":
        return []
    keys, inside = [], False
    for ln in lines[1:]:
        if ln.strip() == "---":
            break
        if re.match(r"^colors:\s*$", ln):
            inside = True
            continue
        if inside:
            if ln and not ln.startswith(" "):
                break
            m = re.match(r"^\s{2}([\w-]+):\s*\S", ln)
            if m:
                keys.append(m.group(1))
    return keys


def check(text):
    keys = colour_keys(text)
    defects = []
    if not keys:
        return ["DESIGN.md declares no colors: block (or the frontmatter could not be read)."]
    base = [k for k in keys if not k.startswith(MODE_PREFIXES)]
    for prefix in MODE_PREFIXES:
        declared = {k[len(prefix):] for k in keys if k.startswith(prefix)}
        missing = [k for k in base if k not in declared]
        for k in missing:
            defects.append(f"colour role `{k}` has no `{prefix}{k}` value: the {prefix[:-1]} mode "
                           f"cannot be measured for it (a rule with no value cannot fail).")
        extra = [k for k in declared if k not in base]
        for k in extra:
            defects.append(f"`{prefix}{k}` names a role `{k}` that the dark set does not declare.")
    for role in REQUIRED_ROLES:
        if role not in base:
            defects.append(f"required colour role `{role}` is not declared.")
    return defects


def self_test():
    text = DESIGN.read_text(encoding="utf-8")
    cases, failures = [], []
    # The real file must be clean.
    cases.append("real DESIGN.md clean")
    if check(text):
        failures.append(f"real DESIGN.md: expected clean, got {check(text)}")
    # Drop one light twin: must fail on that role.
    broken = re.sub(r"^\s{2}light-accent:.*\n", "", text, count=1, flags=re.M)
    cases.append("missing light twin refused")
    if not any("`accent` has no `light-accent`" in d for d in check(broken)):
        failures.append("missing light twin: not refused")
    # Drop the on-accent ink in both modes: must fail.
    broken = re.sub(r"^\s{2}(light-)?accent-contrast:.*\n", "", text, flags=re.M)
    cases.append("missing on-accent ink refused")
    if not any("accent-contrast" in d for d in check(broken)):
        failures.append("missing accent-contrast: not refused")
    # An orphan light key must fail.
    broken = text.replace("  light-surface:", "  light-ghost: \"#000000\"\n  light-surface:", 1)
    cases.append("orphan mode key refused")
    if not any("light-ghost" in d for d in check(broken)):
        failures.append("orphan light key: not refused")
    if failures:
        print(f"self-test FAILED: {len(failures)} case(s).")
        for f in failures:
            print(f"  - {f}")
        return 1
    print(f"self-test: {len(cases)} cases - {', '.join(cases)}.")
    return 0


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--self-test", action="store_true", help="prove this check can fail")
    args = parser.parse_args()
    if args.self_test:
        return self_test()
    defects = check(DESIGN.read_text(encoding="utf-8"))
    if defects:
        print(f"verify-design-modes: FAILED - {len(defects)} defect(s).")
        for d in defects:
            print(f"  - {d}")
        return 1
    keys = colour_keys(DESIGN.read_text(encoding="utf-8"))
    base = [k for k in keys if not k.startswith(MODE_PREFIXES)]
    print(f"verify-design-modes: OK - {len(base)} colour roles, each with a light value; "
          f"accent-contrast and border-strong declared.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
