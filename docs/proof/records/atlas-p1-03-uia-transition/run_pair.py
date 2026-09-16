"""Task-local Atlas pair preparation and separately granted execution. Windows only."""
from __future__ import annotations
import argparse
import ctypes as C
from ctypes import wintypes as W
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import re
import queue
import shutil
import subprocess
import sys
import tempfile
import threading
import time
import unittest
import uuid
import xml.etree.ElementTree as ET

ROOT = Path('C:/Projects/ai-de-test-atlas-p1-03-uia-pair')
SOURCE = 'tests/AiDe.App.Tests/Workbench/Understanding/AtlasDaemonMainWindowProofTests.cs'
SOURCE_SHA = '914a755d84f82d381fc0c516ac4e0b2257c61cb256545a904e32da35052099bc'
BASE = '1d46d651cd5b6356e05545115757ba7eb7ffbf45'
HERE = Path(__file__).resolve()
ARTIFACTS = ROOT / 'artifacts/atlas-pair-preparation'
FACTS = ('AiDe.App.Tests.AtlasDaemonMainWindowProofTests.NativeTransition_A_NoLoadingTraversal',
         'AiDe.App.Tests.AtlasDaemonMainWindowProofTests.NativeTransition_B_LoadingTraversal')

class Refused(RuntimeError):
    pass

def digest(path):
    with Path(path).open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()

def inventory(roots):
    result = {}
    for root in roots:
        path = Path(root)
        if not path.exists():
            raise Refused('PIN-MISSING-ROOT:' + str(path))
        for item in ([path] if path.is_file() else sorted(path.rglob('*'))):
            if item.is_file():
                result[str(item.resolve())] = digest(item)
    return result

def verify_inventory(expected, actual):
    if expected.keys() != actual.keys():
        raise Refused('PIN-POPULATION:' + json.dumps({'added': sorted(actual.keys()-expected.keys()), 'missing': sorted(expected.keys()-actual.keys())}))
    for path, sha in expected.items():
        if actual.get(path) != sha:
            raise Refused('PIN-CHANGED:' + path)

def loading_branch(nodes):
    by_id = {node['Ordinal']: node for node in nodes}
    if len(by_id) != len(nodes):
        return False
    for node in nodes:
        if node.get('Name') != 'Loading Code Atlas.':
            continue
        seen = set()
        while node.get('Parent') in by_id and node['Parent'] not in seen:
            seen.add(node['Parent']); node = by_id[node['Parent']]
            if node.get('Name') == 'Code Atlas' and node.get('ControlType') == 'ControlType.TabItem':
                return True
    return False

def write_json(path, value):
    path = Path(path); path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_suffix(path.suffix + '.tmp')
    temporary.write_text(json.dumps(value, indent=2), encoding='utf-8')
    temporary.replace(path)

def stamp():
    return datetime.now(timezone.utc).isoformat()

def git(*args):
    return subprocess.check_output(['git', '-C', str(ROOT), *args], text=True).strip()

def check_root():
    if os.name != 'nt' or Path.cwd().resolve() != ROOT.resolve() or Path(git('rev-parse', '--show-toplevel')).resolve() != ROOT.resolve():
        raise Refused('FIXED-TREE-REQUIRED')
    if digest(ROOT / SOURCE) != SOURCE_SHA:
        raise Refused('REVIEWED-SOURCE-CHANGED')

# Adapted from the repository bounded_process.py gate/Job Object pattern.
# Only KILL_ON_JOB_CLOSE is configured; no memory/process quota is introduced.
class BasicLimits(C.Structure):
    _fields_ = [('PerProcessUserTimeLimit', C.c_longlong), ('PerJobUserTimeLimit', C.c_longlong),
        ('LimitFlags', W.DWORD), ('MinimumWorkingSetSize', C.c_size_t), ('MaximumWorkingSetSize', C.c_size_t),
        ('ActiveProcessLimit', W.DWORD), ('Affinity', C.c_size_t), ('PriorityClass', W.DWORD), ('SchedulingClass', W.DWORD)]

class IO(C.Structure):
    _fields_ = [(name, C.c_ulonglong) for name in ('ReadOperationCount','WriteOperationCount','OtherOperationCount','ReadTransferCount','WriteTransferCount','OtherTransferCount')]

class ExtendedLimits(C.Structure):
    _fields_ = [('BasicLimitInformation', BasicLimits), ('IoInfo', IO), ('ProcessMemoryLimit', C.c_size_t),
        ('JobMemoryLimit', C.c_size_t), ('PeakProcessMemoryUsed', C.c_size_t), ('PeakJobMemoryUsed', C.c_size_t)]

class Accounting(C.Structure):
    _fields_ = [(name, C.c_longlong) for name in ('TotalUserTime','TotalKernelTime','ThisPeriodTotalUserTime','ThisPeriodTotalKernelTime')] + [(name,W.DWORD) for name in ('TotalPageFaultCount','TotalProcesses','ActiveProcesses','TotalTerminatedProcesses')]

class ProcessList(C.Structure):
    _fields_ = [('Assigned', W.DWORD), ('Count', W.DWORD), ('Pids', C.c_size_t * 4096)]

def kernel():
    dll = C.WinDLL('kernel32', use_last_error=True)
    signatures = {
        'CreateJobObjectW': ([C.c_void_p, W.LPCWSTR], W.HANDLE),
        'SetInformationJobObject': ([W.HANDLE,C.c_int,C.c_void_p,W.DWORD],W.BOOL),
        'AssignProcessToJobObject': ([W.HANDLE,W.HANDLE],W.BOOL),
        'QueryInformationJobObject': ([W.HANDLE,C.c_int,C.c_void_p,W.DWORD,C.c_void_p],W.BOOL),
        'TerminateJobObject': ([W.HANDLE,W.UINT],W.BOOL),
        'CloseHandle': ([W.HANDLE],W.BOOL),
        'OpenProcess': ([W.DWORD,W.BOOL,W.DWORD],W.HANDLE),
        'GetProcessTimes': ([W.HANDLE,C.POINTER(W.FILETIME),C.POINTER(W.FILETIME),C.POINTER(W.FILETIME),C.POINTER(W.FILETIME)],W.BOOL),
        'IsProcessInJob': ([W.HANDLE,W.HANDLE,C.POINTER(W.BOOL)],W.BOOL),
        'WaitForSingleObject': ([W.HANDLE,W.DWORD],W.DWORD),
        'QueryFullProcessImageNameW': ([W.HANDLE,W.DWORD,W.LPWSTR,C.POINTER(W.DWORD)],W.BOOL),
    }
    for name,(args,result) in signatures.items():
        getattr(dll,name).argtypes=args; getattr(dll,name).restype=result
    return dll

def creation(handle):
    values = [W.FILETIME() for _ in range(4)]
    if not kernel().GetProcessTimes(handle, *(C.byref(x) for x in values)):
        raise Refused('PROCESS-TIMES:' + str(C.get_last_error()))
    return (values[0].dwHighDateTime << 32) | values[0].dwLowDateTime

class Job:
    def __init__(self, process):
        self.api=kernel(); self.handle=self.api.CreateJobObjectW(None,None); self.handles={}; self.identities={};self.on_process=None
        if not self.handle:
            raise Refused('JOB-CREATE:' + str(C.get_last_error()))
        limits=ExtendedLimits(); limits.BasicLimitInformation.LimitFlags=0x2000
        if not self.api.SetInformationJobObject(self.handle,9,C.byref(limits),C.sizeof(limits)) or not self.api.AssignProcessToJobObject(self.handle,int(process._handle)):
            self.close(); raise Refused('JOB-ASSIGN:' + str(C.get_last_error()))
        self.sample()

    def sample(self):
        listing=ProcessList()
        if not self.api.QueryInformationJobObject(self.handle,3,C.byref(listing),C.sizeof(listing),None) or listing.Assigned != listing.Count:
            raise Refused('JOB-PROCESS-LIST-INCOMPLETE')
        for pid in listing.Pids[:listing.Count]:
            handle=self.api.OpenProcess(0x100000|0x1000,False,pid)
            if not handle:
                raise Refused('PROCESS-IDENTITY-MISSING:' + str(pid))
            try:
                member=W.BOOL()
                if not self.api.IsProcessInJob(handle,self.handle,C.byref(member)) or not member.value:
                    raise Refused('PROCESS-NOT-OWNED:' + str(pid))
                birth=creation(handle); key=(pid,birth)
                if key not in self.handles:
                    image=C.create_unicode_buffer(32768);length=W.DWORD(len(image))
                    if not self.api.QueryFullProcessImageNameW(handle,0,image,C.byref(length)):raise Refused('PROCESS-IMAGE-MISSING')
                    self.handles[key]=handle; handle=None
                    self.identities[key]={'pid':pid,'creation_filetime':birth,'image':image.value,'observed_at':stamp(),'authority':'owned Job Object membership'}
                    if self.on_process:self.on_process(self.identities[key])
            finally:
                if handle:self.api.CloseHandle(handle)
        accounting=Accounting()
        if not self.api.QueryInformationJobObject(self.handle,1,C.byref(accounting),C.sizeof(accounting),None):
            raise Refused('JOB-ACCOUNTING-UNAVAILABLE')
        return {'total':accounting.TotalProcesses,'active':accounting.ActiveProcesses}

    def terminate(self):
        if not self.api.TerminateJobObject(self.handle,124):
            raise Refused('JOB-TERMINATE:' + str(C.get_last_error()))

    def close(self):
        for handle in self.handles.values():self.api.CloseHandle(handle)
        self.handles.clear()
        if self.handle:self.api.CloseHandle(self.handle);self.handle=None

def run_owned(command, directory, environment, *, seconds=180, cleanup_seconds=30, expires=None):
    """A gated job owns every descendant; accounting gaps fail closed, never PID-kill."""
    if expires is not None and time.time()>=expires:raise Refused('SLOT-EXPIRED-BEFORE-LAUNCH')
    directory=Path(directory);directory.mkdir(parents=True,exist_ok=False)
    result={'command':command,'start':stamp(),'runner':{'pid':os.getpid()},'timeout_seconds':seconds,'cleanup_seconds':cleanup_seconds,
        'timed_out':False,'forced':False,'contained':False,'identities_complete':False,'errors':[]}
    runner_handle=kernel().OpenProcess(0x1000,False,os.getpid())
    try:result['runner']['creation_filetime']=creation(runner_handle)
    finally:kernel().CloseHandle(runner_handle)
    process=None;job=None;started=time.monotonic();accounting=None;browser=BrowserObserver(directory)
    with (directory/'stdout.log').open('wb') as stdout,(directory/'stderr.log').open('wb') as stderr:
        try:
            process=subprocess.Popen([sys.executable,'-B',str(HERE),'_gate',str(directory/'direct-child.json'),*command],
                cwd=ROOT,env=environment,stdin=subprocess.PIPE,stdout=stdout,stderr=stderr)
            result['gate']={'pid':process.pid,'creation_filetime':creation(int(process._handle))}
            job=Job(process)
            job.on_process=browser.observe
            process.stdin.write(b'1');process.stdin.close()
            while True:
                accounting=job.sample()
                if process.poll() is not None and accounting['active']==0:break
                if time.monotonic()-started>=seconds or (expires is not None and time.time()>=expires):
                    result['timed_out']=True;raise Refused('ARM-DEADLINE-OR-SLOT-EXPIRY')
                time.sleep(.01) # Watchdog sampling, never WPF readiness or an oracle retry.
        except Exception as error:
            result['errors'].append(str(error));result['forced']=True
            if job is not None:
                try:job.terminate()
                except Exception as secondary:result['errors'].append(str(secondary))
            elif process is not None:
                # Gate has not been released. Popen retains this exact process handle.
                process.kill()
        finally:
            deadline=time.monotonic()+cleanup_seconds
            if job is not None:
                while time.monotonic()<deadline:
                    try:
                        accounting=job.sample()
                        if accounting['active']==0:break
                    except Exception as error:
                        result['errors'].append(str(error));break
                    time.sleep(.01)
                result['processes']=list(job.identities.values())
                result['job_accounting']=accounting
                # Direct-child handle identity is captured by the trusted gated launcher,
                # including children too brief for sampling. Other missed descendants refuse.
                direct=directory/'direct-child.json'
                if direct.exists():
                    child=json.loads(direct.read_text());result['direct_child']=child
                    if not any((row['pid'],row['creation_filetime'])==(child['pid'],child['creation_filetime']) for row in result['processes']):
                        result['processes'].append(child)
                result['identities_complete']=bool(accounting and accounting['total']==len(result['processes']))
                result['contained']=bool(accounting and accounting['active']==0)
                result['sampled_handles_exited']=all(job.api.WaitForSingleObject(handle,max(0,int((deadline-time.monotonic())*1000)))==0 for handle in job.handles.values())
                result['contained'] &= result['sampled_handles_exited']
                job.close()
            if process is not None:
                try:result['exit_code']=process.wait(timeout=max(.001,deadline-time.monotonic()))
                except subprocess.TimeoutExpired:result['errors'].append('GATE-NOT-REAPED')
            if not browser.close(deadline-time.monotonic()):result['errors'].append('BROWSER-OBSERVER-NOT-DRAINED')
            result['browser_observations']=browser.rows
            result['end']=stamp();result['elapsed_seconds']=time.monotonic()-started
            write_json(directory/'process.json',result)
    return result

class BrowserObserver:
    """Read-only CIM samples, correlated to exact job identities; never an oracle."""
    def __init__(self,directory):
        self.directory=Path(directory);self.pending=queue.Queue();self.rows=[]
        self.thread=threading.Thread(target=self.run,daemon=True);self.thread.start()
    def observe(self,row):
        if Path(row.get('image','')).name.lower()=='msedgewebview2.exe':self.pending.put(row)
    def run(self):
        while (row:=self.pending.get()) is not None:
            command=['powershell','-NoProfile','-NonInteractive','-Command',
                f"Get-CimInstance Win32_Process -Filter 'ProcessId={row['pid']}' | Select-Object ProcessId,ExecutablePath,CommandLine,@{{n='CreationFileTime';e={{$_.CreationDate.ToUniversalTime().ToFileTimeUtc()}}}} | ConvertTo-Json -Compress"]
            try:
                result=subprocess.run(command,capture_output=True,text=True,timeout=2)
                item={'identity':row,'exit_code':result.returncode,'stdout':result.stdout,'stderr':result.stderr}
                if result.returncode==0 and result.stdout.strip():item['cim']=json.loads(result.stdout)
            except Exception as error:item={'identity':row,'error':str(error)}
            self.rows.append(item);write_json(self.directory/'browser-observations.json',self.rows)
    def close(self,seconds):
        self.pending.put(None);self.thread.join(timeout=max(0,seconds))
        return not self.thread.is_alive()

def browser_identity():
    loader=Path(os.environ['USERPROFILE'])/'.nuget/packages/microsoft.web.webview2/1.0.3485.44/runtimes/win-x64/native/WebView2Loader.dll'
    library=C.WinDLL(str(loader));function=library.GetAvailableCoreWebView2BrowserVersionString
    function.argtypes=[W.LPCWSTR,C.POINTER(W.LPWSTR)];function.restype=C.c_long
    text=W.LPWSTR();status=function(None,C.byref(text))
    if status!=0:raise Refused('WEBVIEW-RUNTIME-UNAVAILABLE:'+str(status))
    try:version=text.value
    finally:
        ole=C.OleDLL('ole32');ole.CoTaskMemFree.argtypes=[C.c_void_p];ole.CoTaskMemFree(C.cast(text,C.c_void_p))
    if not version or not re.fullmatch(r'\d+\.\d+\.\d+\.\d+',version):raise Refused('WEBVIEW-CHANNEL-UNSUPPORTED')
    folder=Path(os.environ['ProgramFiles(x86)'])/'Microsoft/EdgeWebView/Application'/version
    binary=folder/'msedgewebview2.exe'
    if not binary.is_file():raise Refused('WEBVIEW-SELECTED-PATH-MISSING')
    return {'version':version,'folder':str(folder),'binary':str(binary),'sha256':digest(binary),'loader':str(loader),'loader_sha256':digest(loader)}

def profile_environment(parent,profile,immutable_roots):
    if any(key.upper().startswith('WEBVIEW2_') for key in parent):raise Refused('INHERITED-WEBVIEW-CONFIG')
    profile=Path(profile).resolve();owned=(ROOT/'artifacts/atlas-uia-profiles').resolve()
    if not profile.is_relative_to(owned) or profile==owned:raise Refused('PROFILE-NOT-OWNED')
    for root in immutable_roots:
        frozen=Path(root).resolve()
        if profile==frozen or profile.is_relative_to(frozen) or frozen.is_relative_to(profile):raise Refused('PROFILE-PIN-OVERLAP')
    if profile.exists():raise Refused('PROFILE-REUSED')
    profile.mkdir(parents=True,exist_ok=False)
    result=parent.copy();result['WEBVIEW2_USER_DATA_FOLDER']=str(profile)
    return result

def verify_browser_use(rows,profile,browser):
    for row in rows:
        cim=row.get('cim',{});identity=row['identity']
        if cim.get('ProcessId')!=identity['pid'] or cim.get('CreationFileTime')!=identity['creation_filetime']:continue
        if Path(cim.get('ExecutablePath','')).resolve()!=Path(browser['binary']).resolve():continue
        command=cim.get('CommandLine') or ''
        match=re.search(r'--user-data-dir=(?:"([^"]+)"|([^\s]+))',command)
        if match and Path(match.group(1) or match.group(2)).resolve()==Path(profile).resolve():return True
    raise Refused('ACTUAL-WEBVIEW-RUNTIME-OR-PROFILE-UNPROVED')

def gate(arguments):
    identity=Path(arguments[0]);command=arguments[1:]
    if sys.stdin.buffer.read(1)!=b'1':return 125
    process=subprocess.Popen(command,stdin=subprocess.DEVNULL)
    write_json(identity,{'pid':process.pid,'creation_filetime':creation(int(process._handle)),
        'parent_pid':os.getpid(),'authority':'direct Popen handle in assigned non-breakaway gate','observed_at':stamp()})
    return process.wait()

def environment():
    result=os.environ.copy()
    # Fresh CLI/MSBuild nodes are necessary for job custody. No service/node reuse.
    result.update({'DOTNET_CLI_USE_MSBUILD_SERVER':'0','MSBUILDDISABLENODEREUSE':'1','DOTNET_CLI_TELEMETRY_OPTOUT':'1'})
    return result

def tool_identity(dotnet):
    values={'webview':browser_identity(),'python':sys.version,'python_path':sys.executable,
        'powershell':shutil.which('powershell'),'git':shutil.which('git')}
    for label,args in [('sdk',['--version']),('runtimes',['--list-runtimes']),('sdks',['--list-sdks']),('info',['--info'])]:
        values[label]=subprocess.check_output([str(dotnet),*args],cwd=ROOT,env=environment(),text=True)
    return values

def freeze_roots(dotnet, tools):
    dotnet=Path(dotnet);roots=[dotnet,dotnet.parent/'host',dotnet.parent/'sdk'/tools['sdk'].strip(),
        dotnet.parent/'shared',Path(sys.executable),HERE,Path(tools['webview']['folder']),Path(tools['webview']['loader'])]
    # Include all current installed runtime versions: adding a higher roll-forward
    # candidate changes the population and must refuse, not silently re-resolve.
    for parent in ('src','tests'):
        for directory in (ROOT/parent).rglob('bin/Debug'):
            roots.append(directory)
        for assets in (ROOT/parent).rglob('obj/project.assets.json'):
            roots.append(assets.parent)
            data=json.loads(assets.read_text(encoding='utf-8-sig'))
            for library in data['libraries'].values():
                if library.get('type')!='package':continue
                candidates=[Path(folder)/library['path'] for folder in data['packageFolders']]
                package=next((p for p in candidates if p.is_dir()),None)
                if package is None:raise Refused('PACKAGE-MISSING:'+library['path'])
                roots.append(package)
    files=git('ls-files','src','tests','global.json','Directory.*','NuGet.Config','nuget.config').splitlines()
    roots.extend(ROOT/file for file in files)
    # Pin actual runner stdlib/native modules already loaded and Python's adjacent DLLs.
    roots.extend(Path(module.__file__) for module in tuple(sys.modules.values()) if getattr(module,'__file__',None) and Path(module.__file__).is_file())
    roots.extend(Path(sys.executable).parent.glob('*.dll'))
    roots.append(Path(shutil.which('git')))
    roots.append(Path(shutil.which('powershell')).parent)
    return sorted({str(path.resolve()) for path in roots})

def prepare():
    check_root();ARTIFACTS.mkdir(parents=True,exist_ok=True)
    if (ARTIFACTS/'build.json').exists() or (ARTIFACTS/'manifest.json').exists():raise Refused('PREPARATION-ALREADY-ATTEMPTED')
    dotnet=Path(shutil.which('dotnet')).resolve();build={'started':stamp(),'build_commit':git('rev-parse','HEAD'),'commands':[]}
    write_json(ARTIFACTS/'build.json',build)
    for index,project in enumerate(('tests/AiDe.App.Tests/AiDe.App.Tests.csproj','src/AiDe.Daemon/AiDe.Daemon.csproj')):
        command=[str(dotnet),'build',project,'--configuration','Debug']
        with (ARTIFACTS/f'build-{index}.stdout.log').open('wb') as output,(ARTIFACTS/f'build-{index}.stderr.log').open('wb') as error:
            started=time.monotonic();result=subprocess.run(command,cwd=ROOT,env=environment(),stdout=output,stderr=error)
        build['commands'].append({'command':command,'exit_code':result.returncode,'elapsed_seconds':time.monotonic()-started})
        write_json(ARTIFACTS/'build.json',build)
        if result.returncode:raise Refused('BUILD-FAILED')
    build['finished']=stamp();write_json(ARTIFACTS/'build.json',build)
    if any(key.upper().startswith('WEBVIEW2_') for key in os.environ):raise Refused('INHERITED-WEBVIEW-CONFIG')
    tools=tool_identity(dotnet);roots=freeze_roots(dotnet,tools)
    frozen=inventory(roots)
    required=[ROOT/'tests/AiDe.App.Tests/bin/Debug/net10.0-windows/testhost.dll',ROOT/'tests/AiDe.App.Tests/bin/Debug/net10.0-windows/AiDe.App.Tests.runtimeconfig.json',ROOT/'src/AiDe.Daemon/bin/Debug/net10.0-windows/AiDe.Daemon.runtimeconfig.json']
    if not all(str(path.resolve()) in frozen for path in required):raise Refused('REQUIRED-OUTPUT-MISSING')
    manifest={'schema':1,'tree':str(ROOT),'build_commit':build['build_commit'],'source_sha256':SOURCE_SHA,'runner_sha256':digest(HERE),
        'python':sys.executable,'dotnet':str(dotnet),'tools':tools,'roots':roots,'files':frozen,'frozen_at':stamp(),
        'execution_session':'codex-atlas-p1-03-pair-execution','execution_agent':'codex-astra-pair-executor'}
    write_json(ARTIFACTS/'manifest.json',manifest)
    print(json.dumps({'manifest':str(ARTIFACTS/'manifest.json'),'sha256':digest(ARTIFACTS/'manifest.json'),'files':len(frozen),'roots':len(roots),'tools':tools},indent=2))

def check_manifest(path, expected_hash):
    if digest(path)!=expected_hash:raise Refused('MANIFEST-HASH')
    manifest=json.loads(Path(path).read_text())
    if manifest['tree']!=str(ROOT) or manifest['runner_sha256']!=digest(HERE) or manifest['source_sha256']!=digest(ROOT/SOURCE):raise Refused('MANIFEST-IDENTITY')
    if tool_identity(manifest['dotnet'])!=manifest['tools']:raise Refused('TOOL-RESOLUTION-CHANGED')
    verify_inventory(manifest['files'],inventory(manifest['roots']))
    return manifest

def verify_process_result(process):
    if process['forced'] or not process['contained'] or not process['identities_complete'] or process['errors']:
        raise Refused('PROCESS-CONTAINMENT-OR-IDENTITY')

def one(events,stage):
    values=[event['Attributes'] for event in events if event['Stage']==stage]
    if len(values)!=1:raise Refused('EVENT-CARDINALITY:'+stage)
    return values[0]

def validate_receipt(receipt, arm, label, process, trx, reviewed_commit):
    events=receipt['Events'];identity=one(events,'transition.identity');held=one(events,'transition.held')
    pre=one(events,'transition.pre-oracle');cleanup=one(events,'transition.cleanup')
    if identity['Arm']!=arm or identity['Run']!=label or identity['SourceCommit']!=reviewed_commit or Path(identity['RepositoryRoot']).resolve()!=ROOT.resolve():raise Refused('RECEIPT-IDENTITY')
    if identity['UiaWpfOwner']!='not-recorded':raise Refused('OWNER-CLAIM')
    known={row['pid']:row for row in process['processes']}
    if identity['ProcessId'] not in known:raise Refused('TESTHOST-IDENTITY-MISSING')
    if any(pre[key]!=1 for key in ('Count','Admissions','Handoffs','Replacements','LoadedEvents','TailCount','Replies')):raise Refused('PUBLICATION-CARDINALITY')
    if held['Count']!=1 or held['Admissions']!=1 or held['Handoffs']!=0 or held['ContentType']!='TextBlock' or held['Text']!='Loading Code Atlas.':raise Refused('HELD-FIXTURE')
    publications=[event['Attributes'] for event in events if event['Stage']=='transition.publication']
    ready=[item for item in publications if item['Phase']=='publication-ready']
    if len(ready)!=1 or not ready[0]['ContentIsReader'] or any(item['Phase'].startswith('TRANSITION-') or item['Phase']=='publication-rejected' for item in publications):raise Refused('PUBLICATION-FAILED')
    packets=[event['Attributes'] for event in events if event['Stage']=='observer.wpf']
    for boundary in ('before-query-batch','after-query-batch'):
        packet=next((row for row in packets if row['Boundary']==boundary),None)
        if not packet or packet['Truncated'] or packet['Unavailable']:raise Refused('WPF-OBSERVATION-INCOMPLETE')
        hosts=[row for row in packet['Hosts'] if row['ContentIsReaderView'] and row['ContentKind']=='AtlasReaderView' and row['Attachment']=='owned-window']
        if len(hosts)!=1:raise Refused('WPF-CURRENT-CHILD')
        host=hosts[0];view=next((row for row in packet['Views'] if row['Id']==host['ContentId']),None)
        if host['ReaderViewId']!=host['ContentId'] or not host['IsLoaded'] or not host['IsVisible'] or not view or not view['IsLoaded'] or not view['IsVisible']:raise Refused('WPF-VISIBILITY')
    queries=[event['Attributes'] for event in events if event['Stage']=='uia.find-first.original']
    if not queries or queries[0]['ExpectedName']!='Atlas files':raise Refused('ORACLE-NOT-REACHED')
    if any(item['OwnHwnd']!=held['OwnHwnd'] or item['ExpectedProcessId']!=identity['ProcessId'] for item in queries):raise Refused('ORACLE-OWNERSHIP')
    release=one(events,'transition.release')
    if release['LoadingTraversal']!=(arm=='B'):raise Refused('TREATMENT-FLAG')
    treatments=[event['Attributes'] for event in events if event['Stage']=='transition.uia-census' and event['Attributes']['Phase']=='loading-treatment']
    if arm=='B':
        if len(treatments)!=1:raise Refused('TREATMENT-MISSING')
        treatment=treatments[0]
        if treatment['Truncated'] or treatment['OwnHwnd']!=held['OwnHwnd'] or treatment['ExpectedProcessId']!=identity['ProcessId'] or not held['Tick']<=treatment['StartTick']<=treatment['EndTick']<=release['Tick'] or not loading_branch(treatment['Nodes']):raise Refused('TREATMENT-ANCESTRY-INCOMPLETE')
    elif treatments:raise Refused('CONTROL-ARM-TRAVERSED')
    if cleanup['RegistryCount']!=0 or cleanup['Handoffs']!=1 or 'reader-disposed' not in cleanup['Custody']:raise Refused('CLEANUP-NOT-PROVED')
    start=one(events,'transition.daemon-start');reaped=one(events,'transition.daemon-reaped')
    if start['Id'] not in known or reaped['Id']!=start['Id'] or reaped['Forced'] or reaped['ExitCode']!=0:raise Refused('DAEMON-CLEANUP-INVALID')
    one(events,'transition.reader-disposed');lease=one(events,'transition.lease-release-returned')
    if not lease['HealthyAtRelease'] or not lease['NormalReturn']:raise Refused('LEASE-CLEANUP-INVALID')
    ns={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'};xml=ET.parse(trx)
    tests=xml.findall('.//t:UnitTestResult',ns)
    if len(tests)!=1 or tests[0].get('testName')!=FACTS[ord(arm)-ord('A')]:raise Refused('TRX-POPULATION')
    outcome=tests[0].get('outcome');primary=[event['Attributes'] for event in events if event['Stage']=='transition.primary']
    if outcome=='Passed':
        expected=['Atlas files','Atlas member outline','Atlas source page read-only','Atlas pagination and bounds','Back to restored Atlas receipt']
        if [item['ExpectedName'] for item in queries]!=expected or not all(item['OriginalFound'] for item in queries) or not receipt['Completed'] or receipt['FailureCount']!=0:raise Refused('PASS-RECEIPT-CONFLICT')
        oracle='pass'
    elif outcome=='Failed' and len(primary)==1 and receipt['FailureCount']==1 and not receipt['Completed'] and 'ObserveAutomationAsync' in (primary[0].get('StackTrace') or ''):
        oracle='negative'
    else:raise Refused('NON-ORACLE-FAILURE')
    expected_exit=0 if oracle=='pass' else 1
    if process['exit_code']!=expected_exit:raise Refused('TRX-PROCESS-EXIT-CONFLICT')
    return {'fixture_valid':True,'original_oracle':oracle,'cleanup_valid':True,'uia_wpf_owner':'not-recorded','daemon_identity':known[start['Id']]}

def execute(args):
    check_root()
    if os.environ.get('AGENT_SESSION')!='codex-atlas-p1-03-pair-execution' or os.environ.get('AGENT_NAME')!='codex-astra-pair-executor':raise Refused('EXECUTION-IDENTITY')
    if not args.slot or git('rev-parse','HEAD')!=args.reviewed_commit or git('status','--porcelain','--untracked-files=no'):raise Refused('REVIEWED-COMMIT-OR-SLOT')
    expiry=datetime.fromisoformat(args.expires_utc.replace('Z','+00:00')).timestamp()
    if time.time()>=expiry or not re.fullmatch('[A-Za-z0-9-]+',args.label):raise Refused('EXPIRY-OR-LABEL')
    destination=ROOT/'artifacts/atlas-uia-pairs'/args.label
    if destination.exists():raise Refused('PAIR-ALREADY-EXISTS')
    for arm in 'AB':
        if (ROOT/'artifacts/atlas-uia-transition'/f'{args.label}-{arm}').exists():raise Refused('ARM-ALREADY-EXISTS')
    manifest=check_manifest(args.manifest,args.manifest_sha256)
    destination.mkdir(parents=True);state={'slot':args.slot,'reviewed_commit':args.reviewed_commit,'manifest_sha256':args.manifest_sha256,'events':[{'event':'BEGIN','at':stamp()}],'arms':[],'completed':False}
    write_json(destination/'state.json',state)
    try:
        for arm,fact in zip('AB',FACTS):
            if time.time()>=expiry:raise Refused('SLOT-EXPIRED')
            check_manifest(args.manifest,args.manifest_sha256);label=f'{args.label}-{arm}'
            arm_dir=destination/arm;results=arm_dir/'trx'
            profile=ROOT/'artifacts/atlas-uia-profiles'/label
            env=profile_environment(environment(),profile,manifest['roots']);env['ATLAS_PROOF_RUN']=label
            state['events'].append({'event':'PINS-BEFORE','arm':arm,'at':stamp(),'sha256':args.manifest_sha256});write_json(destination/'state.json',state)
            command=[manifest['dotnet'],'test','tests/AiDe.App.Tests/AiDe.App.Tests.csproj','--configuration','Debug','--no-build','--no-restore','--filter',f'FullyQualifiedName={fact}','--logger','trx;LogFileName=arm.trx','--results-directory',str(results)]
            process=run_owned(command,arm_dir,env,expires=expiry)
            check_manifest(args.manifest,args.manifest_sha256)
            state['events'].append({'event':'PINS-AFTER','arm':arm,'at':stamp(),'sha256':args.manifest_sha256})
            verify_process_result(process)
            verify_browser_use(process.get('browser_observations',[]),profile,manifest['tools']['webview'])
            receipt=json.loads((ROOT/'artifacts/atlas-uia-transition'/label/'receipt.json').read_text())
            result=validate_receipt(receipt,arm,label,process,results/'arm.trx',args.reviewed_commit)
            state['arms'].append({'arm':arm,'process':process,'result':result});write_json(destination/'state.json',state)
        state['completed']=True
    except Exception as error:
        state['failure']=str(error);raise
    finally:
        state['events'].append({'event':'END','at':stamp(),'completed':state['completed']})
        state['events'].append({'event':'RELEASE','at':stamp(),'meaning':'runner ended; watcher must independently release its slot'})
        write_json(destination/'state.json',state)

class Controls(unittest.TestCase):
    def test_added_input_refused(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory); (path / 'one').write_text('one')
            frozen = inventory([path]); (path / 'two').write_text('two')
            with self.assertRaises(Refused):
                verify_inventory(frozen, inventory([path]))

    def test_unrelated_loading_text_is_not_treatment(self):
        nodes = [{'Ordinal': 0, 'Parent': -1, 'Name': 'owned'},
                 {'Ordinal': 1, 'Parent': 0, 'Name': 'Code Atlas'},
                 {'Ordinal': 2, 'Parent': 0, 'Name': 'Loading Code Atlas.'}]
        self.assertFalse(loading_branch(nodes))

    def test_changed_and_missing_input_refused(self):
        for actual in ({'a':'changed'},{}):
            with self.assertRaises(Refused):verify_inventory({'a':'frozen'},actual)

    def test_actual_ancestor_accepted_cycle_refused(self):
        nodes=[{'Ordinal':0,'Parent':-1,'Name':'Code Atlas','ControlType':'ControlType.TabItem'},
               {'Ordinal':1,'Parent':0,'Name':'Loading Code Atlas.'}]
        self.assertTrue(loading_branch(nodes));nodes[1]['Parent']=1;self.assertFalse(loading_branch(nodes))

    def test_profile_distinct_fresh_and_contained(self):
        with tempfile.TemporaryDirectory(dir=ROOT/'artifacts/atlas-uia-profiles') as folder:
            a=profile_environment({},Path(folder)/'A',[]);b=profile_environment({},Path(folder)/'B',[])
            self.assertNotEqual(a['WEBVIEW2_USER_DATA_FOLDER'],b['WEBVIEW2_USER_DATA_FOLDER'])
            with self.assertRaises(Refused):profile_environment({},Path(folder)/'A',[])
            with self.assertRaises(Refused):profile_environment({'WEBVIEW2_BROWSER_EXECUTABLE_FOLDER':'x'},Path(folder)/'C',[])
            with self.assertRaises(Refused):profile_environment({},Path(folder)/'C',[folder])
            with self.assertRaises(Refused):profile_environment({},ROOT/'outside-profile',[])

    def test_env_delivery_is_not_browser_consumption(self):
        with retained_control('environment') as folder:
            profile=ROOT/'artifacts/atlas-uia-profiles'/Path(folder).name
            env=profile_environment(environment(),profile,[])
            result=run_owned([sys.executable,'-B',str(HERE),'_harmless','env'],Path(folder)/'env',env,seconds=5)
            self.assertTrue(result['contained']);self.assertEqual(result['exit_code'],0)
            delivered=json.loads((Path(folder)/'env/stdout.log').read_text())
            self.assertEqual(delivered['WEBVIEW2_USER_DATA_FOLDER'],str(profile.resolve()))
            with self.assertRaises(Refused):verify_browser_use([],profile,{'binary':'unobserved'})

    def test_timeout_contains_descendants_and_spares_sentinel(self):
        sentinel=subprocess.Popen([sys.executable,'-B',str(HERE),'_harmless','block'],stdin=subprocess.DEVNULL,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
        birth=creation(int(sentinel._handle))
        try:
            with retained_control('containment') as folder:
                result=run_owned([sys.executable,'-B',str(HERE),'_harmless','tree'],Path(folder)/'timeout',environment(),seconds=1,cleanup_seconds=3)
                write_json(ARTIFACTS/'containment-control.json',{'result':result,'sentinel':{'pid':sentinel.pid,'creation_filetime':birth,'survived':sentinel.poll() is None}})
                self.assertTrue(result['timed_out']);self.assertTrue(result['forced']);self.assertTrue(result['contained'])
                self.assertTrue(result['identities_complete']);self.assertGreaterEqual(len(result['processes']),3)
                self.assertIsNone(sentinel.poll());self.assertEqual(birth,creation(int(sentinel._handle)))
                write_json(ARTIFACTS/'containment-control.json',{'result':result,'sentinel':{'pid':sentinel.pid,'creation_filetime':birth,'survived':True}})
        finally:sentinel.terminate();sentinel.wait(timeout=3)

    def test_normal_streams_retained(self):
        with retained_control('normal') as folder:
            result=run_owned([sys.executable,'-B',str(HERE),'_harmless','normal'],Path(folder)/'normal',environment(),seconds=5)
            self.assertFalse(result['forced']);self.assertTrue(result['contained']);self.assertTrue(result['identities_complete'])
            self.assertEqual(result['exit_code'],0)
            self.assertIn('raw-out',(Path(folder)/'normal/stdout.log').read_text())
            self.assertIn('raw-error',(Path(folder)/'normal/stderr.log').read_text())

    def test_expired_slot_never_launches(self):
        with retained_control('expiry') as folder:
            output=Path(folder)/'refused'
            with self.assertRaises(Refused):run_owned([sys.executable,'-V'],output,environment(),expires=time.time()-1)
            self.assertFalse(output.exists())

    def test_missing_identity_and_forced_cleanup_refused(self):
        valid={'forced':False,'contained':True,'identities_complete':True,'errors':[]}
        verify_process_result(valid)
        for key,value in [('forced',True),('contained',False),('identities_complete',False),('errors',['missing'])]:
            with self.assertRaises(Refused):verify_process_result(valid|{key:value})

class retained_control:
    def __init__(self,name):self.path=ARTIFACTS/'controls'/(name+'-'+uuid.uuid4().hex)
    def __enter__(self):self.path.mkdir(parents=True);return self.path
    def __exit__(self,*args):return False

def selftest(output):
    ARTIFACTS.mkdir(parents=True,exist_ok=True);(ROOT/'artifacts/atlas-uia-profiles').mkdir(parents=True,exist_ok=True)
    result = unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(Controls))
    Path(output).parent.mkdir(parents=True, exist_ok=True)
    Path(output).write_text(json.dumps({'run': result.testsRun, 'failures': len(result.failures), 'cases':unittest.defaultTestLoader.getTestCaseNames(Controls),
        'errors': len(result.errors), 'details': [(str(test), detail) for test, detail in result.failures + result.errors]}, indent=2))
    return 0 if result.wasSuccessful() else 1

def main():
    if len(sys.argv)>1 and sys.argv[1]=='_gate':return gate(sys.argv[2:])
    if len(sys.argv)>1 and sys.argv[1]=='_harmless':
        action=sys.argv[2]
        if action=='env':print(json.dumps({'WEBVIEW2_USER_DATA_FOLDER':os.environ.get('WEBVIEW2_USER_DATA_FOLDER')}));return 0
        if action=='normal':print('raw-out',flush=True);print('raw-error',file=sys.stderr,flush=True);return 0
        if action=='tree':subprocess.Popen([sys.executable,'-B',str(HERE),'_harmless','block'])
        threading.Event().wait();return 0
    parser=argparse.ArgumentParser(description=__doc__);commands=parser.add_subparsers(dest='command',required=True)
    control=commands.add_parser('selftest');control.add_argument('--output',required=True)
    commands.add_parser('prepare')
    verify=commands.add_parser('verify');verify.add_argument('--manifest',required=True);verify.add_argument('--manifest-sha256',required=True)
    run=commands.add_parser('execute')
    for name in ('manifest','manifest-sha256','reviewed-commit','slot','expires-utc','label'):run.add_argument('--'+name,required=True)
    args=parser.parse_args()
    if args.command=='selftest':return selftest(args.output)
    if args.command=='prepare':prepare();return 0
    if args.command=='verify':check_root();check_manifest(args.manifest,args.manifest_sha256);print('PINS-MATCH');return 0
    execute(args);return 0

if __name__=='__main__':
    try:raise SystemExit(main())
    except Refused as error:print('REFUSED: '+str(error),file=sys.stderr);raise SystemExit(2)
