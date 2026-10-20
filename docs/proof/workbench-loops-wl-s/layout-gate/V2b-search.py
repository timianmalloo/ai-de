"""V2b recall search for the layout-command gate. It shares no regex with the gate.

Lens: a test file is a candidate when it names a docking or layout type; a test in it is a candidate when its body
calls anything that can change the layout (Execute, Apply, HandleKey, Run, Float/Dock/Move/Close/Collapse verbs)
AND reads anything the user sees of the layout (the manager, floating windows, tab/selection state, visibility,
hosted content). Prints one line per candidate test with the tokens that made it one.
"""
import re
import sys
from pathlib import Path

ROOT = Path(sys.argv[1])
LAYOUT_TYPES = re.compile(r"DockHost|WorkbenchAdapter|LayoutOperation|ZoneBackedLayoutService|WorkbenchController|WorkbenchShell|CommandPalette|DockingManager|LayoutService")
CHANGES = re.compile(r"\b(Execute|Apply|HandleKey|FloatPane|MoveSurface|Redock|Collapse\w*|Maximize\w*|Reset\w*|Close\w*Surface|ActivateSurface|SetStackState)\b")
READS = re.compile(r"\b(Manager|FloatingWindows|HostedBy|Drawn|ContentFor|IsSelected|SelectedContent|Descendents|Visuals|IsVisible|ActualWidth|ActualHeight|LayoutRoot|RootPanel)\b")
TEST = re.compile(r"\[(?:Fact|Theory)[^\]]*\]\s*(?:\[[^\]]*\]\s*)*public\s+(?:async\s+)?[\w<>]+\s+(\w+)\s*\(")

for f in sorted(ROOT.rglob("*.cs")):
    if "obj" in f.parts or "bin" in f.parts:
        continue
    text = f.read_text(encoding="utf-8")
    if not LAYOUT_TYPES.search(text):
        continue
    spans = [(m.start(), m.group(1)) for m in TEST.finditer(text)]
    for i, (start, name) in enumerate(spans):
        end = spans[i + 1][0] if i + 1 < len(spans) else len(text)
        body = text[start:end]
        c = sorted(set(CHANGES.findall(body)))
        r = sorted(set(READS.findall(body)))
        if c and r:
            print(f"{f.relative_to(ROOT).as_posix()}::{name} | changes={','.join(c)} | reads={','.join(r)}")

# Lens B (added after the first run found a hole in lens A: it listed no test that reads only a drawn rail).
# A test is a candidate when it renders the adapter itself AND applies a non-AddSurface layout operation or a
# workbench.* command, whatever it reads afterwards. Lens B has no read filter, so it cannot miss an oracle kind.
print("--- lens B: by-hand render + a non-AddSurface layout change, any read ---")
RENDER_B = re.compile(r"[Aa]dapter2?\.Render\(\)")
CHANGE_B = re.compile(r"LayoutOperation\.(?!AddSurface)\w+|Execute\(\"workbench\.")
for f in sorted(ROOT.rglob("*.cs")):
    if "obj" in f.parts or "bin" in f.parts:
        continue
    text = f.read_text(encoding="utf-8")
    spans = [(m.start(), m.group(1)) for m in TEST.finditer(text)]
    for i, (start, name) in enumerate(spans):
        end = spans[i + 1][0] if i + 1 < len(spans) else len(text)
        body = text[start:end]
        if RENDER_B.search(body) and CHANGE_B.search(body):
            print(f"{f.relative_to(ROOT).as_posix()}::{name}")
