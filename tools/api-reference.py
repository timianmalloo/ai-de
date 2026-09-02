#!/usr/bin/env python3
"""Extract the C# public surface and its XML doc comments into a JavaDoc-style reference.

WHY A SCRIPT AND NOT PROSE. The public surface spans 184 files. Hand-writing it guarantees two
failures the pack names directly: fabricated prose where a doc comment does not exist (the
/document Stage-0 rule), and a coverage claim nobody can check (DC-025). Extraction cannot
fabricate - a member with no `///` block is reported as a GAP, and the coverage percentage is a
count of what was observed, not an estimate.

WHAT IT DOES NOT DO. This is a lexical reader, not a C# parser. It tracks brace depth and
file-scoped namespaces; it does not resolve generics, partial classes across files, or conditional
compilation. That is stated in the emitted header so a reader never mistakes its output for a
compiler's view. Where the lexer cannot decide, it omits rather than guesses.

Usage:
    python tools/api-reference.py --src src --out docs/api [--json]
"""
from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

DECL = re.compile(
    r'^\s*(?P<mods>(?:public|protected internal|protected)'
    r'(?:\s+(?:static|sealed|abstract|virtual|override|readonly|partial|async|new|required'
    r'|extern|unsafe|ref|const|implicit|explicit))*)\s+(?P<rest>.+?)\s*$'
)
TYPE_DECL = re.compile(r'^(?:(?P<kind>class|record|struct|interface|enum|delegate)\s+)(?P<name>[A-Za-z_]\w*)')
RECORD_DECL = re.compile(r'^record\s+(?:class\s+|struct\s+)?(?P<name>[A-Za-z_]\w*)')
NS_FILE = re.compile(r'^\s*namespace\s+(?P<ns>[\w.]+)\s*;')
NS_BLOCK = re.compile(r'^\s*namespace\s+(?P<ns>[\w.]+)\s*\{?\s*$')

TAG = re.compile(
    r'<(?P<tag>summary|remarks|returns|value|param|typeparam|exception)(?P<attrs>[^>]*)>'
    r'(?P<body>.*?)</(?P=tag)>', re.S)
NAME_ATTR = re.compile(r'(?:name|cref)\s*=\s*"([^"]+)"')
# The XML-doc prefix (T:, P:, M:, F:, E:) is optional AS A UNIT. Written as `[A-Za-z]:?` the LETTER
# was mandatory and only the colon optional, so a cref with no prefix — `cref="Profiles"` — had its
# first character eaten and rendered as `rofiles`. Silent: the output is still a plausible code span,
# and nothing compares a generated doc against the identifiers it names.
SEE = re.compile(r'<see(?:also)?\s+cref\s*=\s*"(?:[A-Za-z]:)?([^"]+)"\s*/?>')
PARA = re.compile(r'</?para>')
XML_ANY = re.compile(r'<[^>]+>')


def strip_xml(text):
    """XML doc body -> markdown-ish prose. `see cref` becomes code, everything else is dropped."""
    text = SEE.sub(lambda m: '`' + m.group(1).split('.')[-1] + '`', text)
    text = re.sub(r'<c>(.*?)</c>', r'`\1`', text, flags=re.S)
    text = re.sub(r'<(?:b|strong)>(.*?)</(?:b|strong)>', r'**\1**', text, flags=re.S)
    text = re.sub(r'<(?:i|em)>(.*?)</(?:i|em)>', r'*\1*', text, flags=re.S)
    text = PARA.sub('\n\n', text)
    text = XML_ANY.sub('', text)
    text = text.replace('&lt;', '<').replace('&gt;', '>').replace('&amp;', '&')
    return re.sub(r'[ \t]*\n[ \t]*', '\n', text).strip()


def parse_doc(lines):
    """Turn a run of `///` lines into {summary, remarks, returns, params[], exceptions[]}."""
    raw = '\n'.join(re.sub(r'^\s*///\s?', '', ln) for ln in lines)
    doc = {'summary': '', 'remarks': '', 'returns': '', 'params': [], 'exceptions': []}
    found = False
    for m in TAG.finditer(raw):
        found = True
        tag, body = m.group('tag'), strip_xml(m.group('body'))
        if tag in ('summary', 'remarks', 'returns', 'value'):
            key = 'returns' if tag == 'value' else tag
            doc[key] = (doc[key] + '\n\n' + body).strip() if doc[key] else body
        elif tag in ('param', 'typeparam'):
            name = NAME_ATTR.search(m.group('attrs'))
            doc['params'].append((name.group(1) if name else '?', body))
        elif tag == 'exception':
            name = NAME_ATTR.search(m.group('attrs'))
            doc['exceptions'].append(((name.group(1) if name else '?').split('.')[-1], body))
    if not found:
        doc['summary'] = strip_xml(raw)
    return doc


def member_name(rest):
    """(kind, signature) for a member declaration line, or ('', '') when the lexer cannot tell."""
    sig = rest.rstrip('{').rstrip()
    sig = re.sub(r'\s*=>.*$', '', sig).rstrip(';').rstrip()
    if not sig:
        return '', ''
    if '(' in sig:
        return 'method', sig
    if sig.endswith('}') or ' get;' in rest or ' set;' in rest or '{ get' in rest:
        return 'property', sig
    return 'member', sig


def scan(path):
    ns, out = '', []
    doc_lines = []
    depth = 0
    type_stack = []
    for raw in path.read_text(encoding='utf-8', errors='replace').splitlines():
        line = raw.rstrip()
        stripped = line.strip()

        if stripped.startswith('///'):
            doc_lines.append(line)
            continue

        if not ns:
            m = NS_FILE.match(line) or NS_BLOCK.match(line)
            if m:
                ns = m.group('ns')
                doc_lines = []
                continue

        if stripped.startswith('[') or not stripped:
            continue

        decl = DECL.match(line)
        if decl:
            rest = decl.group('rest')
            tm = TYPE_DECL.match(rest) or RECORD_DECL.match(rest)
            doc = parse_doc(doc_lines) if doc_lines else None
            if tm:
                kind = tm.groupdict().get('kind') or 'record'
                out.append({'level': 'type', 'kind': kind, 'name': tm.group('name'),
                            'signature': re.sub(r'\s*\{.*$', '', rest).rstrip(),
                            'doc': doc, 'file': path.name})
                # `entered` guards the pop below. This repo puts the opening brace on the NEXT
                # line, so at the declaration line depth is still the type's own depth - popping
                # on `depth <= recorded` there would close the type before its body began, and
                # every member would be missed while the run still reported success.
                type_stack.append([tm.group('name'), depth, False])
            elif type_stack and depth == type_stack[-1][1] + 1:
                kind, sig = member_name(rest)
                if kind:
                    out.append({'level': 'member', 'kind': kind, 'owner': type_stack[-1][0],
                                'name': sig, 'signature': decl.group('mods') + ' ' + sig,
                                'doc': doc, 'file': path.name})
        doc_lines = []

        depth += line.count('{') - line.count('}')
        for frame in type_stack:
            if depth > frame[1]:
                frame[2] = True
        while type_stack and type_stack[-1][2] and depth <= type_stack[-1][1]:
            type_stack.pop()
    return ns, out


def render(ns, items):
    types = [i for i in items if i['level'] == 'type']
    members = [i for i in items if i['level'] == 'member']
    by_owner = {}
    for m in members:
        by_owner.setdefault(m['owner'], []).append(m)

    documented = sum(1 for i in items if i['doc'] and i['doc']['summary'])
    pct = round(100 * documented / len(items)) if items else 0
    slug = ns.replace('.', '-').lower()

    lines = [
        '---',
        'id: api-' + slug,
        'title: "API: ' + ns + '"',
        'type: api',
        'status: current',
        'owner: "@timianmalloo"',
        'phase: "0"',
        'tags: [api, reference, generated]',
        'links:',
        '  - { to: architecture, rel: documents }',
        'review-by: 2027-09-02',
        'summary: >-',
        '  Extracted public surface of ' + ns + ': ' + str(len(types)) + ' types, '
        + str(len(members)) + ' members, ' + str(pct) + '% carrying a summary doc comment.',
        '---',
        '',
        '# API: `' + ns + '`',
        '',
        '**' + str(len(types)) + ' public types · ' + str(len(members)) + ' public members · '
        + str(pct) + '% documented.**',
        '',
        '> Extracted from the source by `tools/api-reference.py`. Prose here is the code\'s own',
        '> `///` comment, never written for the reference; a member with no comment is listed as a',
        '> gap rather than given invented text. The extractor is a lexical reader, not a compiler:',
        '> it does not resolve generics, partial classes across files, or conditional compilation.',
        '',
    ]

    for t in types:
        lines += ['## `' + t['name'] + '`', '', '*' + t['kind'] + '* — `' + t['file'] + '`', '']
        if t['doc'] and t['doc']['summary']:
            lines += [t['doc']['summary'], '']
        else:
            lines += ['*No doc comment on this type.* **(gap)**', '']
        if t['doc'] and t['doc']['remarks']:
            lines += ['**Remarks.** ' + t['doc']['remarks'], '']

        own = by_owner.get(t['name'], [])
        if own:
            lines += ['| Member | Summary |', '|---|---|']
            for m in own:
                summary = (m['doc']['summary'] if m['doc'] and m['doc']['summary'] else '**(gap)**')
                summary = summary.replace('\n', ' ').replace('|', '\\|')
                if len(summary) > 220:
                    summary = summary[:217] + '…'
                lines.append('| `' + m['name'].replace('|', '') + '` | ' + summary + ' |')
            lines.append('')
        for m in own:
            if not m['doc']:
                continue
            if m['doc']['params'] or m['doc']['returns'] or m['doc']['exceptions'] or m['doc']['remarks']:
                lines += ['### `' + m['name'] + '`', '']
                if m['doc']['summary']:
                    lines += [m['doc']['summary'], '']
                for p, body in m['doc']['params']:
                    lines.append('- **`' + p + '`** — ' + body.replace('\n', ' '))
                if m['doc']['params']:
                    lines.append('')
                if m['doc']['returns']:
                    lines += ['**Returns.** ' + m['doc']['returns'].replace('\n', ' '), '']
                for exc, body in m['doc']['exceptions']:
                    lines += ['**Throws `' + exc + '`.** ' + body.replace('\n', ' '), '']
                if m['doc']['remarks']:
                    lines += ['**Remarks.** ' + m['doc']['remarks'], '']
    return '\n'.join(lines).rstrip() + '\n'


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--src', default='src')
    ap.add_argument('--out', default='docs/api')
    ap.add_argument('--json', action='store_true')
    args = ap.parse_args()

    src, out = Path(args.src), Path(args.out)
    files = [p for p in src.rglob('*.cs')
             if 'obj' not in p.parts and 'bin' not in p.parts and not p.name.endswith('.g.cs')]
    if not files:
        # A generator that scanned nothing must not report success (R4/CD9).
        print('error: no C# sources under ' + str(src), file=sys.stderr)
        return 2

    namespaces = {}
    for f in files:
        ns, items = scan(f)
        if ns and items:
            namespaces.setdefault(ns, []).extend(items)

    out.mkdir(parents=True, exist_ok=True)
    for existing in out.glob('AiDe.*.md'):
        existing.unlink()  # an entry for a deleted namespace must not survive (V10)

    total = documented = 0
    report = {}
    for ns, items in sorted(namespaces.items()):
        (out / (ns + '.md')).write_text(render(ns, items), encoding='utf-8')
        d = sum(1 for i in items if i['doc'] and i['doc']['summary'])
        total += len(items)
        documented += d
        report[ns] = {'types': sum(1 for i in items if i['level'] == 'type'),
                      'members': sum(1 for i in items if i['level'] == 'member'),
                      'documented': d, 'total': len(items)}

    summary = {'files': len(files), 'namespaces': len(namespaces), 'symbols': total,
               'documented': documented,
               'coverage_pct': round(100 * documented / total, 1) if total else 0.0,
               'by_namespace': report}
    if args.json:
        print(json.dumps(summary, indent=2))
    else:
        print(str(len(files)) + ' files · ' + str(len(namespaces)) + ' namespaces · '
              + str(total) + ' public symbols · ' + str(summary['coverage_pct']) + '% documented')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
