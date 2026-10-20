"""Assemble one track brief: the specific brief + brief-common.md, placeholders filled, refused on a literal seat id.

usage: python .agents/briefs/workbench-loops/assemble_brief.py <specific> <out> TRACK TREE BRANCH PROOF MODEL
exit: 0 written, 1 refused (an unfilled placeholder, or a literal Leader id or epoch: the brief is not kept), 2 usage.
The seat check is tools/verify-no-literal-seat-ids.py --brief (DC-NEW-wl-leader5): a brief names no seat, it says how
to resolve one when sending.
"""
import pathlib
import subprocess
import sys

if len(sys.argv) != 8:
    print(__doc__)
    sys.exit(2)
specific, out, track, tree, branch, proof, model = sys.argv[1:8]
values = {"{TRACK}": track, "{TREE}": tree, "{BRANCH}": branch, "{PROOF}": proof, "{MODEL}": model}
common = pathlib.Path(specific).with_name("brief-common.md").read_text(encoding="utf-8")
text = pathlib.Path(specific).read_text(encoding="utf-8").rstrip("\n") + "\n\n" + common
for key, value in values.items():
    text = text.replace(key, value)
left = [line for line in text.splitlines() if any(key in line for key in values)]
if left:
    sys.exit("unfilled placeholder: " + left[0])
target = pathlib.Path(out)
target.write_text(text, encoding="utf-8", newline="\n")
root = pathlib.Path(__file__).resolve().parents[3]
seat = subprocess.run([sys.executable, str(root / "tools" / "verify-no-literal-seat-ids.py"), "--brief", str(target)],
                      capture_output=True, text=True, encoding="utf-8", errors="replace")
if seat.returncode != 0:
    target.unlink()
    sys.exit(f"refused, {out} not kept:\n{seat.stdout}{seat.stderr}")
print(f"wrote {out}")
