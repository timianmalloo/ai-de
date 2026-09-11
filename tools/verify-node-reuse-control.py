#!/usr/bin/env python3
"""Pin the MSBuild node-reuse control at the boundary, not at one call site.

WHAT THIS EXISTS FOR, measured 2026-09-11. `dotnet build AiDe.sln` leaves SIXTEEN
`/nodeReuse:true` worker processes standing, each holding a conhost. They outlive the
build by design. When the driver is killed rather than allowed to exit -- an agent
session that ends, a closed terminal, a cancelled job -- they are orphaned, and their
command line names no project, no repository and no worktree, so nothing afterwards can
attribute them. The operator sees a growing list of console hosts in Task Manager and
has no way to tell whose they are. They were reported three times.

`tools/verify-test-run.py` already sets MSBUILDDISABLENODEREUSE in its subprocess
environment. That covers ONE call site. It does not cover `dotnet build` or `dotnet test`
typed by hand or by an agent, which is where the sixteen came from -- DC-123, containment
one ring in and absent one ring out.

`Directory.Build.rsp` at the repository root covers every MSBuild invocation rooted
anywhere in the tree. FALSIFIER RUN, both directions, from the root and from a
subdirectory: without the file, 16 workers; with it, 0.

THE FRAGILITY THIS GATE GUARDS: MSBuild takes the FIRST `Directory.Build.rsp` it finds
walking up from the current directory and stops. A nested one anywhere in the tree
SHADOWS the root file for every project beneath it, silently and with no diagnostic. So
the invariant is not "the root file exists" -- it is "every Directory.Build.rsp in the
tree carries the switch".

Run with --behaviour to execute the falsifier itself (~20s, two solution rebuilds).
"""
import argparse
import os
import pathlib
import re
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
SWITCH = re.compile(r"^\s*[/-]nodeReuse:false\s*$", re.IGNORECASE)


def carries_switch(path):
    for line in path.read_text(encoding="utf-8").splitlines():
        if line.lstrip().startswith("#"):
            continue
        if SWITCH.match(line):
            return True
    return False


def count_workers():
    """Live `/nodeReuse:true` MSBuild workers. Windows-only; returns None elsewhere."""
    if os.name != "nt":
        return None
    out = subprocess.run(
        ["powershell", "-NoProfile", "-Command",
         "@(Get-CimInstance Win32_Process -Filter \"Name='dotnet.exe'\" | "
         "Where-Object { $_.CommandLine -like '*nodeReuse:true*' }).Count"],
        capture_output=True, text=True, check=False)
    try:
        return int(out.stdout.strip())
    except ValueError:
        return None


def check(root):
    """Every Directory.Build.rsp under `root` must carry the switch. Returns a defect list."""
    defects = []
    root_rsp = root / "Directory.Build.rsp"
    if not root_rsp.exists():
        defects.append("Directory.Build.rsp is missing from the repository root. "
                       "Without it every bare `dotnet build` leaves 16 orphanable workers.")
    elif not carries_switch(root_rsp):
        defects.append("Directory.Build.rsp exists but carries no uncommented "
                       "`/nodeReuse:false` line.")

    # A nested response file shadows the root one for everything beneath it.
    for nested in sorted(root.rglob("Directory.Build.rsp")):
        if nested == root_rsp or ".git" in nested.parts:
            continue
        rel = nested.relative_to(root).as_posix()
        if not carries_switch(nested):
            defects.append(
                f"{rel} shadows the root response file for every project beneath it and "
                f"does not carry `/nodeReuse:false`. MSBuild stops at the first file it "
                f"finds walking up, so the root control does not reach those projects.")
    return defects


def self_test():
    """Make the tree wrong three ways and require the gate to notice each one.

    DC-104: a control's first green is evidence about the control as much as about the code, and the
    control is the part nobody re-examines because it is what they just reasoned about. So this does
    not assert that the gate is right -- it breaks the tree and watches whether the gate stays quiet.
    """
    import tempfile
    NL = chr(10)
    cases = [
        ("no response file at all", {}, "is missing from the repository root"),
        ("switch present but commented out",
         {"Directory.Build.rsp": "# /nodeReuse:false" + NL}, "carries no uncommented"),
        ("a nested file shadowing a correct root",
         {"Directory.Build.rsp": "/nodeReuse:false" + NL,
          "tests/Inner/Directory.Build.rsp": "/m:4" + NL},
         "shadows the root response file"),
        ("a nested file that also carries the switch",
         {"Directory.Build.rsp": "/nodeReuse:false" + NL,
          "tests/Inner/Directory.Build.rsp": "/nodeReuse:false" + NL + "/m:4" + NL},
         None),
    ]
    failures = []
    for name, files, expected in cases:
        with tempfile.TemporaryDirectory() as tmp:
            root = pathlib.Path(tmp)
            for rel, body in files.items():
                target = root / rel
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_text(body, encoding="utf-8")
            found = check(root)
            if expected is None:
                if found:
                    failures.append(f"{name}: expected clean, got {found}")
            elif not any(expected in d for d in found):
                failures.append(f"{name}: expected a defect containing {expected!r}, got {found}")
    if failures:
        print(f"self-test FAILED: {len(failures)} case(s).")
        for f in failures:
            print(f"  - {f}")
        return 1
    print(f"self-test: {len(cases)} cases, the gate fires on each break and stays quiet when clean.")
    return 0


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--behaviour", action="store_true",
                        help="run the falsifier: rebuild with and without the control")
    parser.add_argument("--self-test", action="store_true",
                        help="prove this gate can fail, against a synthetic tree")
    args = parser.parse_args()
    if args.self_test:
        return self_test()

    root_rsp = ROOT / "Directory.Build.rsp"
    defects = check(ROOT)

    if args.behaviour:
        if os.name != "nt":
            print("behaviour: skipped - the worker census is Windows-only.")
        else:
            subprocess.run(["dotnet", "build-server", "shutdown"],
                           capture_output=True, check=False)
            backup = root_rsp.read_text(encoding="utf-8") if root_rsp.exists() else None
            try:
                if backup is not None:
                    root_rsp.unlink()
                subprocess.run(["dotnet", "build", str(ROOT / "AiDe.sln"), "--nologo",
                                "-v", "q", "-t:Rebuild"], capture_output=True, check=False)
                without = count_workers()
            finally:
                if backup is not None:
                    root_rsp.write_text(backup, encoding="utf-8")
            subprocess.run(["dotnet", "build-server", "shutdown"],
                           capture_output=True, check=False)
            subprocess.run(["dotnet", "build", str(ROOT / "AiDe.sln"), "--nologo",
                            "-v", "q", "-t:Rebuild"], capture_output=True, check=False)
            with_control = count_workers()
            print(f"behaviour: without the control {without} worker(s); "
                  f"with it {with_control}.")
            if not without:
                defects.append(
                    "the falsifier did not fire - removing the control left 0 workers, so "
                    "this run proves nothing about the control. A check that cannot fail "
                    "is not a check.")
            elif with_control:
                defects.append(
                    f"the control is present and {with_control} worker(s) survived the "
                    f"build anyway.")

    if defects:
        print(f"FAIL: {len(defects)} defect(s).")
        for d in defects:
            print(f"  - {d}")
        return 1
    print("node-reuse control: every Directory.Build.rsp in the tree carries "
          "/nodeReuse:false.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
