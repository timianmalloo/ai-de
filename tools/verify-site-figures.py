#!/usr/bin/env python3
"""Bind every number on the public site to the thing it counts.

WHY THIS EXISTS. The site's strongest device is that its figures are counted rather than estimated.
That only holds while they are true, and hand-maintained counts do not stay true: in a single
session the artifact count, the ledger count, the executed-test floor and the public-symbol count
each went stale within one turn, twice after a rebase brought in another session's work. Three
occurrences is a class, not an incident - and the class is "a claim with no path back to its
source", which the site's own craft review had already named as a gap while it was still being
maintained by hand.

The defect is not that a number was wrong. It is that nothing could tell anyone it was wrong.

HOW IT WORKS. Each figure in the HTML carries `data-figure="<name>"`. This script computes every
name from the source of record and compares. `--update` rewrites them; CI runs it without
`--update` so a stale page fails the build instead of publishing.

Usage:
    python tools/verify-site-figures.py            # check; non-zero when a figure is stale
    python tools/verify-site-figures.py --update   # rewrite the figures in place
    python tools/verify-site-figures.py --json     # print what it computed
"""
from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SITE = ROOT / 'site'

FIGURE = re.compile(r'(<(?P<tag>\w+)(?P<attrs>[^>]*\bdata-figure="(?P<name>[\w-]+)"[^>]*)>)(?P<body>.*?)(</(?P=tag)>)')


def _text(path: Path) -> str:
    return path.read_text(encoding='utf-8', errors='replace')


def count_extractors() -> int:
    """Language/artifact families in the composite extractor, excluding the fixture fallback.

    Read from the named arguments of `WorkspaceExtractors.Default()` rather than by counting files
    in the folder: an extractor that exists but is not composed in is not a family the product has.
    """
    src = _text(ROOT / 'src' / 'AiDe.Core' / 'Extraction' / 'WorkspaceExtractors.cs')
    body = src[src.index('Default()'):]
    body = body[:body.index(';')]
    names = re.findall(r'(\w+)\s*:\s*new\s+\w+Extractor\(\)', body)
    return len([n for n in names if n != 'fallback'])


def count_surfaces() -> int:
    src = _text(ROOT / 'src' / 'AiDe.App' / 'Workbench' / 'SurfaceContentFactory.cs')
    m = re.search(r'KnownKinds\s*\{\s*get;\s*\}\s*=\s*\[(?P<list>.*?)\]', src, re.S)
    if not m:
        raise SystemExit('verify-site-figures: could not find KnownKinds')
    return len(re.findall(r'"[^"]+"', m.group('list')))


def count_defect_classes() -> int:
    return len(re.findall(r'(?m)^### DC-', _text(ROOT / 'docs' / 'lessons' / 'defect-classes.md')))


def count_lines(path: Path) -> int:
    return sum(1 for line in _text(path).splitlines() if line.strip())


def test_floor() -> int:
    data = json.loads(_text(ROOT / 'tools' / 'expected-test-counts.json'))
    return sum(data['minimumExecuted'].values())


def run_json(args: list[str]) -> dict:
    out = subprocess.run([sys.executable] + args, cwd=str(ROOT), capture_output=True,
                         text=True, encoding='utf-8', errors='replace', check=True).stdout
    start = out.index('{')
    return json.loads(out[start:])


def measure() -> dict:
    """Every figure the site is allowed to state, with the source it came from."""
    inventory = run_json([str(ROOT / 'docs' / 'ai-forward-pack' / 'scripts' / 'docs-graph.py'), 'inventory'])
    api = run_json([str(ROOT / 'tools' / 'api-reference.py'), '--src', 'src', '--out', 'docs/api', '--json'])
    audit = count_lines(ROOT / 'docs' / 'audit' / 'audit-log.jsonl')
    change = count_lines(ROOT / 'docs' / 'audit' / 'change-log.jsonl')
    return {
        'extractors': count_extractors(),
        'surfaces': count_surfaces(),
        'artifacts': inventory['artifacts'],
        'test-floor': test_floor(),
        'defect-classes': count_defect_classes(),
        'ledger': audit + change,
        'audit-entries': audit,
        'change-entries': change,
        'public-symbols': api['symbols'],
    }


def render(name: str, value: int) -> str:
    # Thousands separators on the large counts only - "7" and "15" read as labels, not quantities.
    return f'{value:,}' if value >= 1000 else str(value)


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument('--update', action='store_true', help='rewrite stale figures in place')
    ap.add_argument('--json', action='store_true', help='print the measured figures')
    args = ap.parse_args()

    truth = measure()
    if args.json:
        print(json.dumps(truth, indent=2))

    pages = sorted(SITE.glob('*.html'))
    if not pages:
        # A verifier that checked nothing must not report success (R4).
        print('error: no site pages found under ' + str(SITE), file=sys.stderr)
        return 2

    stale, checked, unknown, rewritten = [], 0, [], 0

    for page in pages:
        text = original = _text(page)

        def replace(m: 're.Match[str]') -> str:
            nonlocal checked, rewritten
            name, body = m.group('name'), m.group('body')
            if name not in truth:
                unknown.append((page.name, name))
                return m.group(0)
            checked += 1
            want = render(name, truth[name])
            if body.strip() == want:
                return m.group(0)
            if args.update:
                rewritten += 1
                return m.group(1) + want + m.group(6)
            stale.append((page.name, name, body.strip(), want))
            return m.group(0)

        text = FIGURE.sub(replace, text)
        if args.update and text != original:
            page.write_text(text, encoding='utf-8')

    if checked == 0:
        # The figures are only bound while they are ANNOTATED. A page whose data-figure attributes
        # were dropped would pass silently, which is the same shape as the defect this prevents.
        print('error: no data-figure elements found - the site figures are not bound to anything',
              file=sys.stderr)
        return 2

    for page, name in unknown:
        print(f'error: {page} claims an unknown figure "{name}"', file=sys.stderr)

    if args.update:
        print(f'{checked} figure(s) checked, {rewritten} rewritten')
        return 1 if unknown else 0

    for page, name, was, want in stale:
        print(f'STALE {page}: {name} says {was}, source says {want}', file=sys.stderr)

    if stale or unknown:
        print(f'{len(stale)} stale, {len(unknown)} unknown, out of {checked} checked',
              file=sys.stderr)
        return 1

    print(f'{checked} figure(s) verified against source')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
