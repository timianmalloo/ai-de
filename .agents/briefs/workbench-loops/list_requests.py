"""List drive requests (from/to a wl-* session) across the primary and every worktree ledger."""
import json
import pathlib
import subprocess

trees = [line.split()[1] for line in subprocess.run(
    ["git", "-C", r"C:\Projects\ai-de", "worktree", "list", "--porcelain"], capture_output=True, text=True
).stdout.splitlines() if line.startswith("worktree ")]
latest = {}
for tree in trees:
    path = pathlib.Path(tree) / ".agents" / "requests.jsonl"
    if not path.exists():
        continue
    for line in path.read_text(encoding="utf-8").splitlines():
        try:
            d = json.loads(line)
        except ValueError:
            continue
        if not (str(d.get("from", "")).startswith("wl-") or str(d.get("to", "")).startswith("wl-")):
            continue
        latest.setdefault(d.get("id"), {}).update({k: v for k, v in d.items() if v is not None})
        latest[d.get("id")].setdefault("_trees", set()).add(pathlib.Path(tree).name)
for rid, d in latest.items():
    print(rid, "|", d.get("from"), "->", d.get("to"), "|", d.get("status") or d.get("state") or d.get("kind"),
          "|", sorted(d["_trees"]), "|", str(d.get("text", ""))[:150])
