#!/usr/bin/env python3
"""DC-147's control: a headless sweep over every reviewable mockup's own console.

WHAT THIS GUARDS. Several `docs/mockups/*.html` files carry an in-artifact measurement (a
`#verdict` strip: contrast pairs, target sizes, tier fields) so the hub document can say
"measured, not asserted". `docs/lessons/defect-classes.md` DC-147 recorded the failure mode this
produces: a script error before the audit runs (a stray-quote typo; a bare identifier that never
bound to its element) throws before the strip's update line executes. Nothing errors VISIBLY --
the page still opens, `verify-ui-craft-floor.py` still reports the file clean (it reads markup and
computed style; it never executes the page the way a browser does), and the strip is left reading
its placeholder ("measuring...") forever. The claim survives because the only reader that could
refute it is a browser console nobody opened.

This IS that reader, run in CI. It opens every mockup in a real, headless, Chromium-family browser
and fails on either half of DC-147's signature:

  1. AN UNCAUGHT ERROR IN THE PAGE'S OWN CONSOLE. `ReferenceError`, `SyntaxError` inside an inline
     handler, `TypeError` -- anything the browser itself reports as uncaught.
  2. A `#verdict` STRIP STILL READING ITS PLACEHOLDER after the page has had time to run. Every
     mockup that carries one starts it as `measuring...` and a later script line replaces it; if
     the dumped DOM still shows the placeholder, that line never ran.

HOW IT RENDERS, stdlib only, no third-party imports. This project already treats a real browser
engine as ambient infrastructure it does not vendor (`tools/verify-embedded-scripts.py` shells out
to `node --check`, announcing when Node is absent rather than silently skipping). The same shape
here: shell out to whatever Chromium-family browser is already installed (Microsoft Edge, Google
Chrome or Chromium -- GitHub's own `ubuntu-latest` image ships all three; Windows ships Edge) with
`--headless=new --dump-dom` to read the final DOM and `--enable-logging=stderr --v=1` to capture
the same "Uncaught ..." console line a developer would see in DevTools. No CDP client, no
websocket, no installed package.

WHY IT FAILS CLOSED WHEN NO BROWSER IS FOUND. "No browser was available, so nothing was checked"
is the exact shape DC-016 names: a skip and a clean run print the same thing. If this cannot run,
it says so on stderr and exits 1 -- never a quiet 0.

Usage:
    python tools/verify-mockup-audits.py                # sweep docs/mockups/*.html
    python tools/verify-mockup-audits.py <dir-or-file>   # sweep a specific target
    python tools/verify-mockup-audits.py --self-test     # plants a broken strip; must fail red
"""

from __future__ import annotations

import argparse
import re
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass

ROOT = Path(__file__).resolve().parent.parent
MOCKUPS_DIR = ROOT / "docs" / "mockups"

# Virtual time given to the page to finish its own synchronous setup script. Every mockup's audit
# runs on load with no timers, so this is generous headroom, not a tuned budget.
VIRTUAL_TIME_BUDGET_MS = 4000
RENDER_TIMEOUT_S = 30

# Checked in order; the first one found on PATH wins. GitHub's `ubuntu-latest` image ships all
# three (verified against actions/runner-images' Ubuntu 24.04 software manifest); a Windows
# checkout ships Edge. Named, not guessed -- if the local names ever change, `find_browser`
# below still tries the well-known Windows install paths before giving up.
BROWSER_CANDIDATES = (
    "microsoft-edge-stable",
    "microsoft-edge",
    "google-chrome-stable",
    "google-chrome",
    "chromium-browser",
    "chromium",
    "msedge",
)

WINDOWS_EDGE_PATHS = (
    r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
    r"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
)

CONSOLE_UNCAUGHT = re.compile(r"CONSOLE.*?\"(Uncaught[^\"]*)\"")
VERDICT_PLACEHOLDER = re.compile(r'id="verdict"[^>]*>\s*measuring', re.I)


def find_browser() -> str | None:
    for name in BROWSER_CANDIDATES:
        found = shutil.which(name)
        if found:
            return found

    for candidate in WINDOWS_EDGE_PATHS:
        if Path(candidate).exists():
            return candidate

    return None


def render(browser: str, html_path: Path) -> tuple[str, str]:
    """Boots the page headless once. Returns (dumped DOM, stderr log)."""
    with tempfile.TemporaryDirectory(prefix="verify-mockup-audits-") as profile:
        cmd = [
            browser,
            "--headless=new",
            "--disable-gpu",
            "--no-sandbox",
            "--hide-scrollbars",
            f"--user-data-dir={profile}",
            f"--virtual-time-budget={VIRTUAL_TIME_BUDGET_MS}",
            "--dump-dom",
            "--enable-logging=stderr",
            "--v=1",
            html_path.resolve().as_uri(),
        ]
        proc = subprocess.run(
            cmd, capture_output=True, text=True, timeout=RENDER_TIMEOUT_S, encoding="utf-8", errors="replace"
        )
        return proc.stdout, proc.stderr


def findings_for(browser: str, html_path: Path) -> list[str]:
    """DC-147's two failure signatures, read from one headless render. Never asserts what it did
    not observe: a page with no #verdict element is simply not checked for the placeholder half."""
    dom, log = render(browser, html_path)

    problems = [f"console: {m.group(1)}" for m in CONSOLE_UNCAUGHT.finditer(log)]

    if 'id="verdict"' in dom and VERDICT_PLACEHOLDER.search(dom):
        problems.append("the #verdict strip still reads its placeholder (\"measuring...\") after render — its update line never ran")

    return problems


def sweep(browser: str, targets: list[Path]) -> int:
    failed: dict[Path, list[str]] = {}

    for page in targets:
        problems = findings_for(browser, page)
        if problems:
            failed[page] = problems

    if failed:
        print(f"{len(failed)} of {len(targets)} mockup(s) failed the headless sweep (DC-147):")
        for page, problems in failed.items():
            print(f"- {page.relative_to(ROOT) if page.is_relative_to(ROOT) else page}")
            for problem in problems:
                print(f"    {problem}")
        return 1

    print(f"{len(targets)} mockup(s) swept headless, 0 findings (DC-147's control)")
    return 0


# ─────────────────────────────────────────────────────────────────────── self-test ──

BROKEN_FIXTURE = """<!doctype html><html><body>
<span class="checks" id="verdict">measuring&hellip;</span>
<script>
  // Planted DC-147 instance: a bare identifier that never bound to an element (the exact shape
  // of the four legacy mockups' h_theme bug) throws before the line below ever runs.
  h_theme.onchange = () => {};
  document.getElementById('verdict').innerHTML = 'audit: 0 contrast fail';
</script>
</body></html>
"""

HEALTHY_FIXTURE = """<!doctype html><html><body>
<span class="checks" id="verdict">measuring&hellip;</span>
<script>
  document.getElementById('verdict').innerHTML = 'audit: 0 contrast fail';
</script>
</body></html>
"""


def run_self_test(browser: str) -> int:
    with tempfile.TemporaryDirectory(prefix="verify-mockup-audits-selftest-") as tmp:
        tmp_path = Path(tmp)
        broken = tmp_path / "broken.html"
        healthy = tmp_path / "healthy.html"
        broken.write_text(BROKEN_FIXTURE, encoding="utf-8")
        healthy.write_text(HEALTHY_FIXTURE, encoding="utf-8")

        broken_problems = findings_for(browser, broken)
        healthy_problems = findings_for(browser, healthy)

    ok = True

    if not broken_problems:
        print(
            "SELF-TEST FAILED: the planted breakage (an undefined bare identifier, a verdict "
            "strip stuck on its placeholder) was NOT caught.",
            file=sys.stderr,
        )
        ok = False
    else:
        print(f"planted breakage caught, as it must be (red observed): {broken_problems}")

    if healthy_problems:
        print(
            f"SELF-TEST FAILED: the healthy fixture was flagged with no bug planted: {healthy_problems}",
            file=sys.stderr,
        )
        ok = False
    else:
        print("healthy fixture measured clean, as it must be")

    if ok:
        print("self-test passed: the gate fires on the planted DC-147 shape and stays quiet when clean.")
        return 0

    return 1


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--self-test", action="store_true", help="plant a broken verdict strip; must fail (red first)")
    parser.add_argument("path", nargs="?", default=str(MOCKUPS_DIR), help="a mockup file or a directory of them")
    args = parser.parse_args(argv)

    browser = find_browser()
    if browser is None:
        print(
            "verify-mockup-audits: no Chromium-family browser found on PATH or at the well-known "
            "Windows Edge install paths (looked for: " + ", ".join(BROWSER_CANDIDATES) + "). "
            "DC-147's control cannot run here — failing closed rather than reporting a skip as a "
            "pass (DC-016).",
            file=sys.stderr,
        )
        return 1

    if args.self_test:
        return run_self_test(browser)

    target = Path(args.path)
    if target.is_dir():
        mockups = sorted(target.glob("*.html"))
    elif target.is_file():
        mockups = [target]
    else:
        print(f"verify-mockup-audits: no such file or directory: {target}", file=sys.stderr)
        return 1

    if not mockups:
        print(f"verify-mockup-audits: no .html files found under {target}", file=sys.stderr)
        return 1

    return sweep(browser, mockups)


if __name__ == "__main__":
    sys.exit(main())
