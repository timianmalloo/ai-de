"""Render the canonical AI-DE evolution spec with an offline contents map and journey SVGs."""
from __future__ import annotations

import html
import os
from pathlib import Path
import re
import subprocess
import sys
import tempfile
import textwrap

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'docs/specs/workbench-loops-ux.md'
OUT=SOURCE.with_suffix('.html')


def journey_svg(source: str, number: int) -> str:
    """Render the deliberately bounded flowchart grammar used in this spec; refuse unknown syntax."""
    nodes,positions,edges={},{},[]
    for raw in source.splitlines():
        line=raw.strip()
        if not line or line=='flowchart TD':
            continue
        position=re.fullmatch(r'%% pos (\w+) (\d+) (\d+)',line)
        node=re.fullmatch(r'(\w+)\["([^"]+)"\]',line)
        edge=re.fullmatch(r'(\w+)\s*-->\s*(?:\|([^|]+)\|\s*)?(\w+)',line)
        if position:
            positions[position[1]]=(int(position[2]),int(position[3]))
        elif node:
            if node[1] in nodes:raise ValueError('duplicate journey node: '+node[1])
            nodes[node[1]]=node[2]
        elif edge:
            edges.append((edge[1],edge[3],edge[2] or ''))
        else:
            raise ValueError('unsupported journey diagram syntax: '+line)
    if not nodes or len(nodes)>60 or len(edges)>120:
        raise ValueError('journey diagram empty or outside the declared bound')
    if set(nodes)!=set(positions) or any(a not in nodes or b not in nodes for a,b,_ in edges):
        raise ValueError('journey positions/edges do not match declared nodes')
    width=max(x for x,_ in positions.values())+390
    height=max(y for _,y in positions.values())+85
    marker=f'journey-arrow-{number}'
    parts=[f'<svg class="journey-map" viewBox="0 0 {width} {height}" role="img" aria-labelledby="journey-title-{number} journey-desc-{number}" xmlns="http://www.w3.org/2000/svg">',
           f'<title id="journey-title-{number}">{"Work journey" if number==1 else "System journey"}: main path and recovery branches</title>',
           f'<desc id="journey-desc-{number}">Numbered arrows are explained in the route legend immediately below. Numbers identify routes, not step order. The Mermaid source retains the canonical relationships.</desc>',
           f'<defs><marker id="{marker}" markerWidth="7" markerHeight="7" refX="6" refY="3" orient="auto"><path d="M0 0L6 3L0 6" fill="none" stroke="currentColor"/></marker></defs>']
    labels=[];badge_positions=[];outer_lanes={};legend=[]
    for route,(a,b,label) in enumerate(edges,1):
        ax,ay=positions[a];bx,by=positions[b]
        if ax==bx and by-ay==90:
            x1=x2=ax+140;y1=ay+60 if by>ay else ay;y2=by if by>ay else by+60
            path=f'M{x1} {y1} L{x2} {y2}';lx=x1+20;ly=(y1+y2)/2
        elif ax==bx:
            lane=outer_lanes.get(ax,0);outer_lanes[ax]=lane+1
            left=ax==min(x for x,_ in positions.values())
            x1=x2=ax if left else ax+280;y1=ay+30;y2=by+30
            side=ax-25-12*lane if left else ax+305+12*lane
            path=f'M{x1} {y1} H{side} V{y2} H{x2}';lx=side;ly=y1+18
        else:
            x1=ax+280 if bx>ax else ax;x2=bx if bx>ax else bx+280
            y1=ay+30;y2=by+30
            bend=20 if bx>ax else -20
            middle=(x1+x2)/2
            path=f'M{x1} {y1} C{middle} {y1+bend} {middle} {y2+bend} {x2} {y2}'
            lx=middle;ly=(y1+y2)/2+bend
        parts.append(f'<path class="journey-edge" d="{path}" marker-end="url(#{marker})"/>')
        original_lx,original_ly=lx,ly
        offset=0
        while any((lx-x)**2+(ly-y)**2<28**2 for x,y in badge_positions):
            offset+=1;ly=original_ly+(1 if offset%2 else -1)*((offset+1)//2)*29
            if offset>30:raise ValueError('route badges cannot be placed without overlap')
        badge_positions.append((lx,ly))
        if ly!=original_ly:labels.append(f'<path class="journey-guide" d="M{original_lx} {original_ly}L{lx} {ly}"/>')
        labels.append(f'<g class="journey-route"><circle cx="{lx}" cy="{ly}" r="12"/><text x="{lx}" y="{ly+4}" text-anchor="middle">{route}</text></g>')
        legend.append(f'<tr><td>{route}</td><td>{html.escape(nodes[a])}</td><td>{html.escape(nodes[b])}</td><td>{html.escape(label or "Continue the main flow")}</td></tr>')
    parts.extend(labels)
    for key,label in nodes.items():
        x,y=positions[key];lines=textwrap.wrap(label,34)
        if len(lines)>3:raise ValueError('journey label exceeds its readable box: '+key)
        parts.append(f'<g class="journey-node"><rect x="{x}" y="{y}" width="280" height="60" rx="4"/><text x="{x+140}" y="{y+31-(len(lines)-1)*8}" text-anchor="middle">'+''.join(f'<tspan x="{x+140}" dy="{0 if i==0 else 16}">{html.escape(t)}</tspan>' for i,t in enumerate(lines))+'</text></g>')
    parts.append('</svg>')
    return ''.join(parts)+'<details class="route-legend" open><summary>Route legend — numbers are references, not step order</summary><table><thead><tr><th>Route</th><th>From</th><th>To</th><th>Condition or action</th></tr></thead><tbody>'+''.join(legend)+'</tbody></table></details>'


def main() -> int:
    with tempfile.NamedTemporaryFile(prefix='.evolution-spec-',suffix='.tmp',dir=OUT.parent,delete=False) as f:
        temporary=Path(f.name)
    try:
        subprocess.run([sys.executable,str(ROOT/'docs/ai-forward-pack/scripts/render-markdown.py'),str(SOURCE),'--output',str(temporary),'--title','AI-DE evolution — functional specification and user journeys'],cwd=ROOT,check=True,capture_output=True)
        rendered=temporary.read_text(encoding='utf-8')
        contents=[];seen=set()
        def heading(match):
            level=int(match[1]);body=match[2];plain=html.unescape(re.sub('<[^>]+>','',body))
            slug=re.sub(r'[^a-z0-9]+','-',plain.lower()).strip('-')
            if slug in seen:raise ValueError('duplicate heading anchor: '+slug)
            seen.add(slug)
            if level<=3:contents.append((level,slug,plain))
            return f'<h{level} id="{slug}">{body}</h{level}>'
        rendered=re.sub(r'<h([1-4])>(.*?)</h\1>',heading,rendered,flags=re.S)
        diagram_count=0
        def diagram(match):
            nonlocal diagram_count
            diagram_count+=1
            source=html.unescape(match[1])
            return journey_svg(source,diagram_count)+'<details class="diagram-source"><summary>Mermaid source and complete recovery relationships</summary>'+match[0]+'</details>'
        rendered=re.sub(r'<pre><code class="language-mermaid">(.*?)</code></pre>',diagram,rendered,flags=re.S)
        if diagram_count!=2:raise ValueError('the two distinct journey diagrams are required')
        toc='<details class="spec-toc" open><summary>Specification contents</summary><nav aria-label="Specification contents">'+''.join(f'<a class="level-{level}" href="#{slug}">{html.escape(label)}</a>' for level,slug,label in contents)+'</nav></details>'
        rendered=rendered.replace('<body>','<body>'+toc,1)
        style='''<style>
.spec-toc{position:fixed;left:16px;top:16px;width:245px;max-height:calc(100vh - 32px);overflow:auto;padding:12px;background:var(--panel);border:1px solid var(--line);font-size:13px;line-height:1.4}
.spec-toc summary{font-size:15px;font-weight:600;cursor:pointer;padding:4px}.spec-toc nav a{display:block;padding:5px 4px;text-decoration:none}.spec-toc nav a:hover{text-decoration:underline}.spec-toc .level-1{font-weight:600;margin-top:8px}.spec-toc .level-3{padding-left:16px;font-size:12px}main{margin:24px 24px 24px 284px;width:min(1120px,calc(100% - 308px));padding:32px}h1,h2,h3{scroll-margin-top:20px}h1{font-size:28px}h2{font-size:22px}h3{font-size:18px}a:focus-visible,summary:focus-visible{outline:2px solid var(--accent);outline-offset:2px}.journey-map{width:100%;height:auto;background:#F4F6F8;color:#1A1F26;border:1px solid #C7CFD8;display:block;margin:16px 0}.journey-node rect{fill:#FFFFFF;stroke:#5F6977}.journey-node text{font:15px 'Segoe UI',system-ui,sans-serif;fill:#1A1F26}.journey-edge{fill:none;stroke:#55606E;stroke-width:1.4}.journey-route circle{fill:#FFFFFF;stroke:#5F6977}.journey-route text{font:600 14px 'Segoe UI',system-ui,sans-serif;fill:#1A1F26}.journey-guide{stroke:#55606E;stroke-dasharray:2 2;fill:none}.route-legend{font-size:14px}.route-legend summary{font-weight:600;cursor:pointer;padding:8px 0}.diagram-source{font-size:13px;margin-bottom:24px}.diagram-source summary{cursor:pointer;min-height:28px}.source{overflow-wrap:anywhere}
@media(max-width:1100px){.spec-toc{position:static;width:calc(100% - 32px);max-height:300px;margin:16px}main{width:calc(100% - 32px);margin:16px;padding:24px}}
@media print{.spec-toc,.diagram-source{display:none}main{width:100%;margin:0;padding:0;border:0}h1,h2,h3{break-after:avoid}.journey-map{max-height:240mm;break-inside:avoid}}
</style>'''
        rendered=rendered.replace('</head>',style+'</head>',1)
        rendered=rendered.replace('</body>','<script>if(matchMedia("(max-width:1100px)").matches)document.querySelector(".spec-toc").open=false;</script></body>',1)
        temporary.write_text(rendered,encoding='utf-8',newline='\n')
        os.replace(temporary,OUT)
        print(f'evolution specification: {diagram_count} journey diagrams; {len(contents)} navigation anchors; {OUT.relative_to(ROOT)}')
    finally:
        temporary.unlink(missing_ok=True)
    return 0


for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass


if __name__=='__main__':raise SystemExit(main())
