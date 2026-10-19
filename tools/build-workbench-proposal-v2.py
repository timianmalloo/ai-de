"""Build local review companions from the canonical proposal, mockup and design language."""
from pathlib import Path
import json
import re

ROOT=Path(__file__).resolve().parents[1]
def js_string(text):return json.dumps(text,ensure_ascii=False).replace('</','<\\/')

def main():
 proposal=(ROOT/'docs/proposals/local-software-factory-v2.html').read_text(encoding='utf-8')
 mockup=(ROOT/'docs/mockups/factory-workbench-v2.html').read_text(encoding='utf-8')
 page='''<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>AI-DE — Work and system, one workbench</title>
<style>:root{color-scheme:dark;--surface:#12151A;--raised:#1A1F26;--ink:#E4E9EF;--muted:#98A3B2;--border:#2A313B;--strong:#7C8896;--accent:#5B9DD9;--on-accent:#0D1014;--focus:#8FC0EA}*{box-sizing:border-box}body{margin:0;background:var(--surface);color:var(--ink);font:13px/1.5 'Segoe UI Variable Text','Segoe UI',system-ui,sans-serif;height:100vh;display:flex;flex-direction:column}header{padding:12px 20px;border-bottom:1px solid var(--border);display:flex;align-items:center;gap:20px;flex-wrap:wrap}h1{font-size:22px;line-height:1.3;font-weight:600;margin:0}.meta{font-size:12px;color:var(--muted);margin-top:3px}nav{display:flex;gap:6px;flex-wrap:wrap;margin-left:auto}button{font:inherit;min-height:32px;padding:5px 12px;border:1px solid var(--strong);border-radius:4px;background:transparent;color:var(--ink);cursor:pointer}button[aria-pressed=true]{background:var(--accent);color:var(--on-accent);border-color:var(--accent);font-weight:600}button:hover{border-color:var(--focus)}button:focus-visible,a:focus-visible{outline:2px solid var(--focus);outline-offset:2px}a{color:var(--accent)}.context{padding:7px 20px;font-size:12px;border-bottom:1px solid var(--border);display:flex;gap:16px;align-items:center;flex-wrap:wrap}.context span{color:var(--muted);flex:1}.context a{white-space:nowrap}iframe{width:100%;border:0;flex:1;min-height:0;background:var(--surface)}[hidden]{display:none!important}@media(max-width:850px){header{gap:8px;padding:10px 12px}h1{font-size:18px}nav{margin-left:0}.context{padding:6px 12px}}</style></head><body>
<header><div><h1>Start with work. Start with the system.</h1><div class="meta">AI-DE · local desktop workbench · exploratory proposal · 18 October 2026</div></div><nav aria-label="Proposal and end-to-end mockups"><button data-view="proposal" aria-pressed="true">Read the proposal</button><button data-view="work" aria-pressed="false">Start with work</button><button data-view="system" aria-pressed="false">Start with the system</button><button data-view="review" aria-pressed="false">Review a PR</button></nav></header>
<div class="context"><span id="context">Two legitimate entry paths; one intelligent editor, execution record and dockable evidence model.</span><a href="../mockups/factory-workbench-v2.html" target="_blank" rel="noopener">Open full-window mockup</a><a href="local-software-factory-v2.html" target="_blank" rel="noopener">Reading copy</a></div>
<iframe id="reader" title="Full AI-DE proposal"></iframe><iframe id="prototype" title="Interactive AI-DE desktop workbench mockup" hidden></iframe>
<script>
const proposal=__PROPOSAL__,mockup=__MOCKUP__;
const reader=document.getElementById('reader'),prototype=document.getElementById('prototype');let loaded=false,pending='work';
reader.srcdoc=proposal;
function enter(kind){const api=prototype.contentWindow.__workbench;if(!api)return;if(kind==='system')api.enter('system');else if(kind==='review')api.enter('review');else api.enter('work')}
prototype.addEventListener('load',()=>{loaded=true;enter(pending)});
document.querySelectorAll('[data-view]').forEach(button=>button.addEventListener('click',()=>{const view=button.dataset.view;document.querySelectorAll('[data-view]').forEach(b=>b.setAttribute('aria-pressed',b===button));reader.hidden=view!=='proposal';prototype.hidden=view==='proposal';document.getElementById('context').textContent=view==='proposal'?'Two legitimate entry paths; one intelligent editor, execution record and dockable evidence model.':view==='system'?'Explore first. Ask the repo or PR, inspect the visualizer and keep a checklist. Create work only when you choose.':view==='review'?'Read the behavioral change, ask questions, inspect evidence and record a deliberate disposition.':'Compose intent in the notebook, use IntelliSense and scoped context, then inspect execution and evaluation beside it.';if(view!=='proposal'){pending=view;if(!prototype.srcdoc)prototype.srcdoc=mockup;else if(loaded)enter(view)}}));
</script></body></html>'''
 page=page.replace('__PROPOSAL__',js_string(proposal)).replace('__MOCKUP__',js_string(mockup))
 target=ROOT/'docs/proposals/local-software-factory-v2-interactive.html';target.write_text(page,encoding='utf-8', newline="\n")
 design=(ROOT/'DESIGN.md').read_text(encoding='utf-8')
 colors=dict(re.findall(r'^  ([\w-]+): "(#[0-9A-Fa-f]{6})"',design,re.M))
 pairs=[]
 for prefix in ('','light-'):
  for ink in ('text','text-muted','accent','verified','inferred','danger'):
   for ground in ('surface','surface-raised','surface-sunken'):
    pairs.append([prefix+ink,prefix+ground,4.5])
  pairs.append([prefix+'accent-contrast',prefix+'accent',4.5])
 tokens={'colors':colors,'pairs':pairs,'type':[[str(n)+'px',f"400 {n}px/1.5 'Segoe UI', sans-serif",'Notebook and workbench typography'] for n in (11,12,13,15,18,22)]}
 template=(ROOT/'docs/ai-forward-pack/templates/design-language-preview.template.html').read_text(encoding='utf-8')
 template=template.replace('__PROJECT__','AI-DE')
 template=template.replace("lum(hex)>.4?'#000':'#fff'","ratio(hex,'#000000')>=ratio(hex,'#ffffff')?'#000000':'#ffffff'")
 template=template.replace('font-size:.72rem','font-size:12px').replace('font-size:.68rem','font-size:12px')
 template,count=re.subn(r'const TOKENS = \{.*?\n\};','const TOKENS = '+json.dumps(tokens,ensure_ascii=False)+';',template,count=1,flags=re.S)
 assert count==1,'Design template token seam not found'
 (ROOT/'docs/mockups/workbench-design-language.html').write_text(template,encoding='utf-8', newline="\n")
 print(target.relative_to(ROOT))
 print('Design preview:',len(colors),'colors;',len(pairs),'audited token pairs')

import sys
for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass


if __name__=='__main__':main()
