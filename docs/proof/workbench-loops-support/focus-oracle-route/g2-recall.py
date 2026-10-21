"""G2 recall search, independent of the gate: App tests that read keyboard focus or live-region text after
a layout command or a Render(). Raw hits to stdout: file:line  test  [cmd kinds before]  [read]."""
import re, sys, pathlib
root = pathlib.Path("tests/AiDe.App.Tests")
read = re.compile(r"Keyboard\.FocusedElement|FocusManager\.GetFocusedElement|IsKeyboardFocusWithin|\.IsFocused\b|LiveRegion\.Text|\.Announcer\b.*\.Text|LiveRegion\b")
cmd = re.compile(r"\.Render\(\)|\.Apply\(\s*new\s+LayoutOperation\.(?!AddSurface)\w+|\.Execute\(\"workbench\.\w+\"|CloseByTheRoute|CollapseByTheRoute|StepBackByTheRoute|ExpandByTheRail")
member = re.compile(r"^    (?:public|private|internal)\b[^\n]*?(\w+)\s*\(", re.M)
n = 0
for f in sorted(root.rglob("*.cs")):
    if "obj" in f.parts or "bin" in f.parts:
        continue
    t = f.read_text(encoding="utf-8")
    ms = list(member.finditer(t))
    for i, m in enumerate(ms):
        body = t[m.start(): ms[i+1].start() if i+1 < len(ms) else len(t)]
        for r in read.finditer(body):
            before = [c.group(0) for c in cmd.finditer(body[:r.start()])]
            if before:
                line = t[:m.start()+r.start()].count("\n") + 1
                print(f"{f.as_posix()}:{line}\t{m.group(1)}\t{sorted(set(before))}\t{r.group(0)}")
                n += 1
                break
print(f"-- {n} tests", file=sys.stderr)
