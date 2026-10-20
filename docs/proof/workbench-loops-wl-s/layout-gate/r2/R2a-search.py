"""R2a: line-by-line listing, per App test file, of layout commands, by-hand renders and the two reads.
Independent of the gate: a different tokenisation (line scan per test method, no helper inlining)."""
import re, sys, pathlib
cmd = re.compile(r'LayoutOperation\.(?!AddSurface)\w+|Execute\("workbench\.|ZoneLayoutService\.(?:Collapse|Close|Move|Float|Set)\w*|\.Controller\.(?:Move|Float|Close|Collapse)\w*\(')
rend = re.compile(r'\.Render\(\)')
read = re.compile(r'ContentFor\(|ActiveSurfaceId')
test = re.compile(r'^\s*public\s+(?:async\s+)?(?:void|Task)\s+(\w+)')
for p in sorted(pathlib.Path('tests/AiDe.App.Tests').rglob('*.cs')):
    if 'LayoutOraclesDrive' in p.name or '/obj/' in p.as_posix(): continue
    lines = p.read_text(encoding='utf-8').split('\n')
    cur = None; ev = []
    def flush():
        if cur and any(k == 'R' for k, *_ in ev) and any(k == 'C' for k, *_ in ev):
            print(f'{p.as_posix()}::{cur}')
            for k, n, t in ev: print(f'   {k} {n}: {t.strip()[:110]}')
    for i, l in enumerate(lines, 1):
        m = test.match(l)
        if m and not l.strip().startswith('//'):
            flush(); cur = m.group(1); ev = []
        if l.strip().startswith('//'): continue
        if cmd.search(l): ev.append(('C', i, l))
        if rend.search(l): ev.append(('M', i, l))
        if read.search(l): ev.append(('R', i, l))
    flush()
