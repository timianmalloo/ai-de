"""Resolve conflicts in docs/lessons/defect-classes.md by keeping both sides (ours first, then theirs).
The register is append-only in practice (plan: Artifact classes). Refuses any other file and any conflict that is
not pure appended entries (each side must start with '### ' after optional blank lines).
Usage: python -I keep_both.py <tree>"""
import pathlib
import re
import sys

tree = pathlib.Path(sys.argv[1])
path = tree / "docs" / "lessons" / "defect-classes.md"
text = path.read_text(encoding="utf-8")
pattern = re.compile(r"^<<<<<<< [^\n]*\n(.*?)^=======\n(.*?)^>>>>>>> [^\n]*\n", re.S | re.M)
count = 0


def merge(m):
    global count
    ours, theirs = m.group(1), m.group(2)
    for side in (ours, theirs):
        if side.strip() and not side.lstrip().startswith("### "):
            sys.exit(f"refused: a conflict hunk is not a pure appended entry:\n{side[:300]}")
    count += 1
    return ours.rstrip("\n") + "\n\n" + theirs.lstrip("\n")


new = pattern.sub(merge, text)
if re.search(r"^(<<<<<<<|=======$|>>>>>>>)", new, re.M):
    sys.exit("refused: markers remain")
path.write_text(new, encoding="utf-8", newline="\n")
print(f"resolved {count} hunk(s) keeping both sides")
