#!/usr/bin/env python3
"""Every harness that reads this repository must be told to capture its evidence.

THE MEASUREMENT THAT MADE THIS NECESSARY. Over this repository's entire recorded history: 111
episodes scored, 1 observation written. The recurrence threshold is two distinct episodes, so the
learning corpus can only be empty — an engine that is correct, verified end to end, and producing
nothing.

The cause was not the engine and not laziness. The instruction that makes scoring possible
(`episode.artifacts` on `episode-close`) lived in exactly ONE place:
`.github/instructions/session-collaboration.instructions.md`, which is GitHub Copilot's convention.
`CLAUDE.md` and `AGENTS.md` did not mention it at all — so the harness running most of the work was
never told, and could not have known. A practice nobody was asked to follow is not a practice
problem.

WHY A GATE AND NOT A NOTE. The channel is harness-NEUTRAL by construction — an environment variable
and a JSONL line, so anything that can write a file participates. What is not automatic is that each
harness's own instruction root says so, and the failure is silent: adding Gemini CLI means adding
GEMINI.md, and nothing would notice that the new harness was never told. That is DC-103 — a scope
named rather than derived — pointed at agent instructions instead of at tests.

WHAT IS CHECKED. For each harness root that EXISTS, the file must reach the capture contract: name
the attribute and the channel. A declared harness with no root yet is reported as PENDING rather than
failed — inventing GEMINI.md before Gemini CLI is used would be a file nobody maintains, and the gate
catches it the moment the file appears without the instruction, which is when it matters.
"""

from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path

# The harnesses this repository supports, and the file each one reads first.
#
# Adding a harness means adding it HERE, which is the point: the list is the declaration, and the
# gate turns it into an obligation. A harness whose root does not exist yet is pending, not a failure.
HARNESS_ROOTS = {
    "Claude Code": "CLAUDE.md",
    "GitHub Copilot": ".github/instructions/session-collaboration.instructions.md",
    "Generic (AGENTS.md convention)": "AGENTS.md",
    "Gemini CLI": "GEMINI.md",
}

# A root "reaches the capture contract" when it names both the attribute and the channel. Two
# markers rather than one, because a file can mention the channel while saying nothing about
# evidence — the channel predates capture and every harness root could plausibly cite it.
REQUIRED_MARKERS = ("episode.artifacts", "AIDE_CONTRACT_LOG")


def repo_root() -> Path:
    return Path(subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        capture_output=True, text=True, check=True).stdout.strip())


def check(root: Path, roots: dict[str, str] | None = None) -> tuple[list[str], int, int]:
    """Returns (problems, roots checked, roots pending)."""
    problems: list[str] = []
    roots = HARNESS_ROOTS if roots is None else roots

    if not roots:
        return (["no harness roots are declared — this gate examined nothing"], 0, 0)

    checked = 0
    pending = 0

    for harness, relative in sorted(roots.items()):
        path = root / relative

        if not path.is_file():
            pending += 1
            continue

        checked += 1
        text = path.read_text(encoding="utf-8", errors="replace")
        missing = [m for m in REQUIRED_MARKERS if m not in text]

        if missing:
            problems.append(
                f"{relative} is {harness}'s instruction root and does not carry the Proof Pack "
                f"capture instruction (missing: {', '.join(missing)}). An agent reading only this "
                "file will close every episode without naming its evidence, and score Not Scored "
                "forever — which is what 111 episodes and 1 observation looked like.")

    if checked == 0:
        problems.append(
            "no harness root exists at all — every declared root is pending, so this gate is "
            "examining nothing")

    return (problems, checked, pending)


def self_test(root: Path) -> int:
    """Prove both directions fire: a root missing the instruction, and an examined-nothing run."""
    silent = root / "docs" / "ai-forward-pack" / "_selftest_harness_root.md"
    silent.parent.mkdir(parents=True, exist_ok=True)
    silent.write_text("# A harness root that never mentions capture\n", encoding="utf-8")

    try:
        relative = silent.relative_to(root).as_posix()
        problems, _, _ = check(root, roots={"Self-test harness": relative})

        if not any(relative in problem for problem in problems):
            print("self-test FAILED: a root missing the instruction was not reported", file=sys.stderr)
            return 1

        # The DC-016 direction: a run where nothing exists must say so rather than pass.
        problems, _, _ = check(root, roots={"Absent harness": "NO_SUCH_ROOT.md"})

        if not any("examining nothing" in problem for problem in problems):
            print("self-test FAILED: a run with no existing root reported success", file=sys.stderr)
            return 1
    finally:
        silent.unlink(missing_ok=True)

    print("self-test OK — a root missing the instruction is reported, and an empty run refuses")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--self-test", action="store_true",
                        help="prove the gate fires on a harness root that omits the instruction")
    args = parser.parse_args()

    root = repo_root()

    if args.self_test:
        return self_test(root)

    problems, checked, pending = check(root)

    if problems:
        print("verify-capture-instruction: FAILED")
        for problem in problems:
            print(f"  - {problem}")
        return 1

    print(f"verify-capture-instruction: OK — {checked} harness root(s) carry the capture "
          f"instruction, {pending} declared root(s) not yet present.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
