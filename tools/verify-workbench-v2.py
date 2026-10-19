"""Exercise the local HTML workbench; never call a provider or launch a repo command."""
from pathlib import Path
import json
import argparse
import hashlib
from datetime import datetime, timezone
from playwright.sync_api import sync_playwright

ROOT=Path(__file__).resolve().parents[1]
TARGET=ROOT/'docs/mockups/factory-workbench-v2.html'

def main(argv=None):
 parser=argparse.ArgumentParser(description=__doc__)
 parser.add_argument('--out',default='artifacts/workbench-loops-20261018/interactions.json')
 parser.add_argument('--target',default=str(TARGET))
 parser.add_argument('--self-test',action='store_true',help='prove the gate can fail: a page with no workbench must fail the journey')
 args=parser.parse_args(argv)
 if args.self_test:return self_test()
 target=Path(args.target);out=ROOT/args.out;results=[];errors=[];requests=[];journey_completed=False
 with sync_playwright() as pw:
  browser=pw.chromium.launch(headless=True)
  page=browser.new_page(viewport={'width':1440,'height':1000})
  page.on('pageerror',lambda e:errors.append(str(e)))
  page.on('request',lambda r:requests.append(r.url))
  page.goto(target.as_uri())
  page.evaluate('localStorage.clear()');page.reload()
  def click(action):page.locator('[data-action="'+action+'"]').filter(visible=True).first.click()
  def state():return page.evaluate('window.__workbench.getState()')
  def check(name,fn):
   try:fn();results.append({'check':name,'passed':True})
   except Exception as e:results.append({'check':name,'passed':False,'error':str(e)[:900]});raise
  def require(condition,message):
   if not condition:raise AssertionError(message)
  try:
   check('Nonzero rendered frame and no initial layout failures',lambda:require(page.evaluate('window.__workbench.audit().findings.length')==0,'Initial frame failed'))
   check('Feature destinations live in left navigation',lambda:require(page.locator('#navigation .children .child').count()==4 and page.locator('#runPane [role=tab]').count()==0,'Feature map mismatch'))
   click('start');click('start-system')
   check('System-first entry creates investigation without a work item',lambda:require(state()['entry']=='system' and state()['scope']=='repo' and 'no work item' in page.locator('#statusScope').inner_text(),'System entry forced work'))
   page.locator('#editor').fill('Explain @Ru')
   page.locator('#editor').press('End');page.locator('#editor').press('Control+Space')
   check('Rich completion exposes indexed suggestions',lambda:require(page.locator('#completion [role=option]').count()>0,'No completion options'))
   page.locator('#editor').press('Enter')
   check('Rich completion inserts the selected reference',lambda:require('@RunEvent' in state()['docs']['understand']['md'],'Reference missing'))
   page.locator('#editor').press('Control+Space');page.locator('#editor').press('Escape')
   check('Completion dismissal resets accessibility state',lambda:require(page.locator('#editor').get_attribute('aria-expanded')=='false' and page.locator('#editor').get_attribute('aria-activedescendant') is None,'Stale completion ARIA'))
   page.locator('#editor').fill('@zzzz_unavailable');page.locator('#editor').press('End');page.locator('#editor').press('Control+Space')
   check('Empty completion has no invented active option',lambda:require(page.locator('#completion [role=option]').count()==0 and page.locator('#editor').get_attribute('aria-activedescendant') is None,'Empty completion names an absent option'))
   page.locator('#editor').press('Escape')
   click('projection');page.locator('#sourceEditor').fill('Inspect @Dock');page.locator('#sourceEditor').press('End');page.locator('#sourceEditor').press('Control+Space');page.locator('#sourceEditor').press('Enter')
   check('Source completion preserves projection and inserts symbol',lambda:require(state()['projection']=='source' and '@DockHost' in page.locator('#sourceEditor').input_value(),'Source IntelliSense failed'))
   page.locator('#sourceEditor').fill('@unrecorded_symbol');click('projection');page.locator('.mention[data-ref="@unrecorded_symbol"]').click()
   check('Unknown reference never substitutes another symbol',lambda:require('Unindexed reference' in page.locator('#tip').inner_text() and 'RunEvent' not in page.locator('#tip').inner_text(),'Unknown metadata fabricated'))
   click('hide-tip');click('projection')
   page.locator('#sourceEditor').fill('```json\n{\"a\":1}\n\n\n\n{\"b\":2}\n```\n\n## Notes\n\nKeep this content.')
   original=page.locator('#sourceEditor').input_value();click('projection');click('projection')
   check('Rich/source preserves code language and blank lines',lambda:require(page.locator('#sourceEditor').input_value()==original,'Source changed without edit'))
   page.locator('#sourceEditor').fill('cancellation remains cancellation');click('multi-edit');click('preview-replace')
   check('Multi-edit previews the real match count',lambda:require('2 exact matches' in page.locator('#replacePreview').inner_text(),'Match count mismatch'))
   click('apply-replace');check('Multi-edit applies one explicit transaction',lambda:require(state()['docs']['understand']['md']=='run cancellation remains run cancellation','Replacement failed'))
   click('undo');check('Multi-edit undo restores exact content',lambda:require(state()['docs']['understand']['md']=='cancellation remains cancellation','Undo mismatch'))
   click('save');click('branch');check('Branch retains an immutable prior prompt',lambda:require(len(state()['history'])>=2 and state()['docs']['understand']['title'].endswith('branch'),'Branch history absent'))
   page.locator('[data-mode=factory]').click();click('run');page.wait_for_function('window.__workbench.getState().run.status === "waiting"')
   before=state()['run'];page.locator('[data-mode=understand]').click()
   check('Active run survives loop navigation',lambda:require(state()['run']['id']==before['id'] and state()['run']['status']=='waiting' and state()['run']['snapshot']==before['snapshot'],'Navigation lost run'))
   click('allow-once');page.wait_for_function('window.__workbench.getState().run.status === "complete"')
   check('Completed response retains original route and scope',lambda:require(state()['run']['provider']=='codex' and state()['run']['scope']=='work' and state()['run']['mode']=='factory','Run provenance drift'))
   click('connections');page.locator('[data-provider=claude]').click()
   check('Selected provider does not relabel previous run',lambda:require('Codex' in page.locator('#runSummary').inner_text(),'Historical provider drift'))
   click('native-login');check('Authentication requires explicit native sign-in',lambda:require(state()['auth']['claude']=='pending' and page.locator('[data-action=confirm-login]').is_visible(),'Login was implicit'))
   click('confirm-login');click('check-auth');check('Profile qualification is separate from sign-in',lambda:require(state()['auth']['claude']=='ready','Fixture profile not checked'))
   click('close-dialog')
   page.locator('[data-action=pane-menu][data-pane=run]').click();page.locator('[data-action=move-pane][data-place=bottom]').click()
   check('Execution docks below with stable run identity',lambda:require(page.locator('#bottomSlot>#runPane').count()==1 and state()['run']['id']==before['id'],'Dock lost state'))
   check('Dock move restores visible pane focus',lambda:require(page.evaluate('document.activeElement.dataset.pane')=='run' and page.evaluate('document.activeElement.closest("#menu")===null'),'Dock focus stayed hidden'))
   page.locator('[data-action=pane-menu][data-pane=run]').click();page.locator('[data-action=move-pane][data-place=float]').click()
   check('Floating approximation retains execution content',lambda:require(page.locator('#floatLayer>#runPane').count()==1 and 'Ready for your review' in page.locator('#runSummary').inner_text(),'Float lost content'))
   page.locator('[data-action=pane-menu][data-pane=run]').click();click('restore-layout')
   old_width=state()['right'];page.locator('#splitter').focus();page.locator('#splitter').press('ArrowLeft')
   check('Keyboard resizing changes the split',lambda:require(state()['right']==old_width+2,'Keyboard resize failed'))
   click('focus-editor');page.keyboard.press('Escape')
   check('Focus mode restores the peer run pane',lambda:require(page.locator('#runPane').is_visible(),'Focus restore failed'))
   check('Focus mode restores a visible editing target',lambda:require(page.evaluate('document.activeElement.id') in ['editor','sourceEditor'],'Focus returned to body'))
   click('show-sequence');page.locator('[data-node=Harness]').press('Enter');click('ask-selection')
   check('Diagram question retains selected-object context',lambda:require('Explain Harness' in state()['docs']['understand']['md'],'Selected object missing'))
   click('concern');n=len(state()['checks']);page.locator('[data-check="'+str(n-1)+'"]').check()
   check('Checklist acknowledgment does not become executed proof',lambda:require('Execution evidence is still missing' in page.locator('#checklist').inner_text(),'Acknowledgment promoted evidence'))
   check('Checklist keyboard focus survives update',lambda:require(page.evaluate('document.activeElement.dataset.check')==str(n-1),'Checklist focus lost'))
   click('promote-work');click('confirm-promotion')
   check('Investigation becomes work only after explicit promotion',lambda:require(len(state()['workItemsCreated'])==1 and state()['scope']=='work' and state()['workItemsCreated'][0]['origin']['scope']=='repo','Promotion lost origin'))
   page.locator('[data-mode=review]').click();page.locator('[data-view=pulls]').click();click('open-pr')
   check('PR review opens evidence before asking for disposition',lambda:require(not page.locator('#dialog').is_visible() and page.locator('#visualKind').input_value()=='diff','Review skipped to disposition'))
   click('review-decision');page.locator('#decisionReason').fill('Need explicit evidence for the disconnect path.');click('record-decision')
   check('Reasoned review disposition is explicit and local',lambda:require('No PR approval or merge occurred' in page.locator('#toast').inner_text(),'Disposition implied merge'))
   for provider in ['agy','muse']:
    page.locator('[data-mode=factory]').click();click('connections');page.locator('[data-provider='+provider+']').click();click('check-existing');click('check-auth');click('close-dialog');click('run')
    page.wait_for_function('window.__workbench.getState().run.status === "waiting-policy"')
    require(page.locator('#runPane [data-action=allow-once]').count()==0,'Invented approval channel for '+provider)
    click('continue-readonly');page.wait_for_function('window.__workbench.getState().run.status === "complete"')
    require(state()['run']['readOnly'],'Read-only continuation not recorded')
   results.append({'check':'Antigravity and Muse use native-policy/read-only recovery without invented approval controls','passed':True})
   click('harness')
   for theme in ['dark','light','hc']:
    page.locator('#theme').select_option(theme)
    for width in [960,1440]:
     page.set_viewport_size({'width':width,'height':1000})
     for mode in ['factory','review','understand']:
      page.locator('[data-mode='+mode+']').click()
      audit=page.evaluate('window.__workbench.audit()')
      require(not audit['findings'],str((theme,width,mode,audit)))
   results.append({'check':'18 theme/viewport/loop combinations render without measured failures','passed':True})
   page.set_viewport_size({'width':1440,'height':1000});page.locator('#theme').select_option('dark')
   for hard_state in ['empty','loading','error','partial','stale','wrong','quota','signedout','overflow','ready']:
    page.locator('#viewState').select_option(hard_state)
    audit=page.evaluate('window.__workbench.audit()');require(not audit['findings'],str((hard_state,audit)))
    if hard_state in ['loading','stale','quota','signedout']:require(page.locator('#runButton').is_disabled(),'State did not block run: '+hard_state)
   results.append({'check':'10 hard states retain visible usable frame and required run blocks','passed':True})
   page.locator('#persona').select_option('observer')
   require(page.locator('#editor').get_attribute('contenteditable')=='false' and page.locator('#runButton').is_disabled(),'Observer controls are inconsistent')
   page.locator('#persona').select_option('developer');page.locator('#density').select_option('comfortable')
   require(page.locator('body').get_attribute('data-density')=='comfortable','Density harness did not apply')
   page.locator('#viewport').select_option('compact');require(not page.evaluate('window.__workbench.audit().findings'),'Container viewport failed')
   page.locator('#viewport').select_option('wide');page.locator('#density').select_option('compact')
   page.locator('#reduced').uncheck();require(page.locator('body').get_attribute('data-motion')=='normal','Motion state missing');page.locator('#reduced').check()
   page.locator('#capability').select_option('offline');require(page.locator('#runButton').is_disabled(),'Offline run enabled');page.locator('#capability').select_option('no-reasoning');page.locator('#capability').select_option('full')
   results.append({'check':'Persona, container viewport, density, capability and reduced-motion controls apply real state','passed':True})
   require(not errors,'Browser errors: '+str(errors))
   require(not [r for r in requests if r.startswith(('http:','https:'))],'Unexpected network requests')
   results.append({'check':'No browser errors or network/provider requests','passed':True})
   journey_completed=True
  except Exception as error:
   if not results or results[-1]['passed']:
    results.append({'check':'Journey execution reached its endpoint','passed':False,'error':str(error)[:900]})
   out.parent.mkdir(parents=True,exist_ok=True);page.screenshot(path=str(out.with_name('interaction-failure.png')))
  output={'artifact':target.relative_to(ROOT).as_posix() if target.is_relative_to(ROOT) else target.name,'artifact_sha256':hashlib.sha256(target.read_bytes()).hexdigest(),'checked_at':datetime.now(timezone.utc).isoformat(),'journey_completed':journey_completed,'checks':results,'passed':sum(x['passed'] for x in results),'failed':sum(not x['passed'] for x in results),'browser_errors':errors,'network_requests':[r for r in requests if r.startswith(('http:','https:'))]}
  out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps(output,indent=2),encoding='utf-8', newline="\n")
  browser.close()
 print(json.dumps(output,indent=2))
 return 1 if not journey_completed or output['failed'] or output['browser_errors'] else 0

def self_test():
 import tempfile
 with tempfile.TemporaryDirectory() as tmp:
  page=Path(tmp)/'empty.html';page.write_text('<!doctype html><title>x</title><p>no workbench</p>',encoding='utf-8',newline='\n')
  if main(['--target',str(page),'--out',str(Path(tmp)/'interactions.json')])!=1:
   print('self-test FAILED: a page with no workbench passed the journey');return 1
 print('self-test OK - a page with no workbench fails the journey');return 0

import sys
for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass


if __name__=='__main__':raise SystemExit(main())
