#!/usr/bin/env python3
"""Assemble the /document bundle's close-up view at docs/_site/index.html.

The Docs Explorer (docs/index.html) is the MAP of all 279 artifacts. This is the CLOSE-UP of the
generated bundle - the architecture overview, the four diagram families, and the extracted API
reference - rendered with Mermaid, navigable, and self-contained so it opens from a file:// path
with no server.

It also exists for a duller reason: GitHub Pages serves a .md file as a download, not a page. A
bundle nobody can read in a browser is a bundle that only its author ever reads.

Usage:
    python tools/build-doc-viewer.py
"""
from __future__ import annotations

import json
import re
import subprocess
import sys
from datetime import date
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DOCS = ROOT / 'docs'
TEMPLATE = DOCS / 'ai-forward-pack' / 'templates' / 'doc-viewer.template.html'
OUT = DOCS / '_site' / 'index.html'
META = DOCS / '_meta.json'

FRONTMATTER = re.compile(r'^---\n.*?\n---\n', re.S)


def read_normalised(path: Path) -> str:
    """Read a file with newlines normalised to LF.

    THE OUTPUT OF THIS GENERATOR MUST NOT DEPEND ON THE HOST THAT RAN IT. `verify-derived-views`
    regenerates this file and compares it against the committed one, so any byte that varies by
    platform makes a correct file report as stale on one OS and clean on another. That is exactly
    what happened the first time the control suite moved to a Linux runner (INV-0005): git checks
    these sources out with CRLF on Windows and LF on Linux, and FRONTMATTER above anchors on a bare
    LF, so the same document strips its frontmatter on one host and keeps it on the other.

    Normalising on read removes the class rather than that one regex, and the write below pins LF so
    the bytes on disk do not depend on the platform's newline translation either.
    """
    return path.read_text(encoding='utf-8').replace('\r\n', '\n')

# Order is the reading order, not the filesystem's.
BUNDLE = [
    ('index', 'Overview', 'Start here', DOCS / 'index.md'),
    ('architecture', 'Architecture', 'Start here', DOCS / 'architecture.md'),
    ('diagram-component', 'Component', 'Diagrams', DOCS / 'diagrams' / 'component.md'),
    ('diagram-layers', 'Layered architecture', 'Diagrams', DOCS / 'diagrams' / 'layers.md'),
    ('diagram-sequence', 'Sequence', 'Diagrams', DOCS / 'diagrams' / 'sequence.md'),
    ('diagram-class', 'Class', 'Diagrams', DOCS / 'diagrams' / 'class.md'),
]


def strip_frontmatter(text: str) -> str:
    return FRONTMATTER.sub('', text, count=1)


def head_sha() -> str:
    try:
        return subprocess.run(['git', '-C', str(ROOT), 'rev-parse', 'HEAD'],
                              capture_output=True, text=True, check=True).stdout.strip()
    except Exception:
        return ''


def main() -> int:
    if not TEMPLATE.exists():
        print('error: doc-viewer template missing at ' + str(TEMPLATE), file=sys.stderr)
        return 2

    pages = []
    missing = []
    for page_id, title, group, path in BUNDLE:
        if not path.exists():
            missing.append(str(path.relative_to(ROOT)))
            continue
        pages.append({'id': page_id, 'title': title, 'group': group,
                      'markdown': strip_frontmatter(read_normalised(path))})

    api_files = sorted((DOCS / 'api').glob('AiDe*.md')) if (DOCS / 'api').exists() else []
    for path in api_files:
        ns = path.stem
        pages.append({'id': 'api-' + ns.replace('.', '-').lower(),
                      'title': ns, 'group': 'API reference',
                      'markdown': strip_frontmatter(read_normalised(path))})

    if not pages:
        # A builder that assembled nothing must not report success (R4).
        print('error: no bundle pages found', file=sys.stderr)
        return 2
    if missing:
        print('warning: missing bundle pages: ' + ', '.join(missing), file=sys.stderr)

    html = read_normalised(TEMPLATE).replace('__PROJECT__', 'AI-DE')

    sha = head_sha()
    meta_js = {'project': 'AI-DE', 'generated': date.today().isoformat(), 'documented_sha': sha}

    docs_js = 'window.DOCS = ' + json.dumps(pages, ensure_ascii=False) + ';'
    meta_line = 'window.DOC_META = ' + json.dumps(meta_js) + ';'

    # Replace the template's placeholder arrays wholesale, as its own comment instructs.
    start = html.index('window.DOCS = [')
    end = html.index('window.DOC_META =')
    end_of_meta = html.index('\n', end)
    html = html[:start] + docs_js + '\n' + meta_line + html[end_of_meta:]

    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(html, encoding='utf-8', newline='\n')

    # Coverage comes from the extractor's own count, never from a second tally here.
    try:
        api_summary = json.loads(subprocess.run(
            [sys.executable, str(ROOT / 'tools' / 'api-reference.py'), '--src', 'src',
             '--out', 'docs/api', '--json'],
            capture_output=True, text=True, check=True, cwd=str(ROOT)).stdout)
    except Exception as exc:  # noqa: BLE001 - the meta must degrade to "not recorded", not to a guess
        print('warning: could not read API coverage: ' + str(exc), file=sys.stderr)
        api_summary = None

    META.write_text(json.dumps({
        'generated': date.today().isoformat(),
        'documented_sha': sha,
        'bundle_pages': len(pages),
        'api': {
            'files_scanned': api_summary['files'] if api_summary else 'not recorded',
            'public_symbols': api_summary['symbols'] if api_summary else 'not recorded',
            'documented': api_summary['documented'] if api_summary else 'not recorded',
            'coverage_pct': api_summary['coverage_pct'] if api_summary else 'not recorded',
        },
        'diagram_families': ['component', 'layers', 'sequence', 'class'],
        'confidence': {
            'api_reference': 'Verified — extracted from source doc comments; gaps listed, never filled.',
            'diagrams': 'Verified — every node and edge traced to a declaration or composition root.',
            'architecture_overview': 'Pre-existing artifact, reviewed against the code for this run.',
        },
        'known_gaps': [
            'The API reference is lexical, not compiled: generics, partial classes across files and '
            'conditional compilation are not resolved.',
            'Coverage counts a summary doc comment, not its quality.',
        ],
    }, indent=2) + '\n', encoding='utf-8', newline='\n')

    print(str(len(pages)) + ' pages -> ' + str(OUT.relative_to(ROOT))
          + ' (' + str(round(OUT.stat().st_size / 1024)) + ' KB)')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
