#!/usr/bin/env python3
"""Validate this episode's artifact identities without touching production files."""
from __future__ import annotations
import argparse
import hashlib
import json
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[3]
SOURCE_REF = re.compile(r'\bsrc/[A-Za-z0-9_./-]+\.cs(?:proj)?')

def citation_errors(text: str) -> list[str]:
    return [path for path in sorted(set(SOURCE_REF.findall(text))) if not (ROOT / path).is_file()]

def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--self-test', action='store_true')
    args = parser.parse_args()
    if args.self_test:
        assert citation_errors('src/NoSuchProject/Missing.cs') == ['src/NoSuchProject/Missing.cs']
        assert not citation_errors('src/AiDe.Core/WorkspaceCore.cs')
        print('Citation control rejects the injected missing source and accepts a real source.')
        return 0
    files = list((ROOT / 'docs/knowledge/spec-first-software-factory').glob('*.md'))
    files += [ROOT / p for p in (
        'docs/reviews/forensic-review-rev102.md', 'docs/reviews/built-inventory-rev102.md',
        'docs/reviews/authoring-inventory-rev102.md', 'docs/backlog/forensic-review-rev102.md',
        'docs/proposals/local-software-factory.md', 'docs/mockups/factory-workbench.md',
        'docs/proof/factory-evolution-20261017.md')]
    assert len(files) == 15
    findings=[]
    for path in files:
        if not path.is_file():
            findings.append('Missing artifact: '+str(path.relative_to(ROOT)))
            continue
        for bad in citation_errors(path.read_text(encoding='utf-8')):
            findings.append(str(path.relative_to(ROOT))+': invalid source '+bad)
        companion = path.with_suffix('.html')
        if path == ROOT / 'docs/mockups/factory-workbench.md':
            companion = ROOT / 'docs/mockups/factory-workbench-notes.html'
        if not companion.is_file():
            findings.append('Missing HTML companion: '+str(path.relative_to(ROOT)))
    mock=(ROOT/'docs/mockups/factory-workbench.html').read_text(encoding='utf-8')
    wrapper=(ROOT/'docs/proposals/local-software-factory-interactive.html').read_text(encoding='utf-8')
    match=re.search(r'const prototype=(.*?);const notes=',wrapper)
    if not match or json.loads(match.group(1)) != mock:
        findings.append('Embedded mockup differs from standalone HTML')
    for prefix in ('src/','tests/','.github/workflows/'):
        import subprocess
        changed=subprocess.run(['git','diff','--name-only','--',prefix],cwd=ROOT,capture_output=True,text=True,check=True).stdout.strip()
        if changed: findings.append('Out-of-scope production/test/CI edits: '+changed)
    if findings:
        print('\n'.join(findings))
        return 1
    result={'artifacts_checked':len(files),'invalid_source_references':0,'html_companions':True,
            'embedded_mockup_matches':True,'production_test_ci_edits':False,
            'mockup_sha256':hashlib.sha256(mock.encode()).hexdigest()}
    (Path(__file__).parent/'artifact-check-results.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
    print(json.dumps(result,indent=2))
    return 0

if __name__ == '__main__':
    raise SystemExit(main())
