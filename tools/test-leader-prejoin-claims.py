#!/usr/bin/env python3
"""Self-test for the claim check in .agents/briefs/workbench-loops/leader_prejoin.py (T38, DC-NEW-wl-t36-claim).

Builds a throwaway git repo with a branch that changes three paths and a session log that claims some of them,
then runs `leader_prejoin.py <repo> --branch <b> --session <s>` and reads the exit code and output.
Exit 0 = every case behaved as asserted; 1 = a case failed.
"""
import json
import pathlib
import subprocess
import sys
import tempfile

TOOL = pathlib.Path(__file__).resolve().parent.parent / ".agents" / "briefs" / "workbench-loops" / "leader_prejoin.py"
SESSION = "wl-test-node"


def git(repo, *args):
    subprocess.run(["git", *args], cwd=repo, check=True, capture_output=True, text=True)


def build(repo):
    git(repo, "init", "-q", "-b", "main")
    git(repo, "config", "user.email", "t@example.invalid")
    git(repo, "config", "user.name", "t")
    (repo / "README.md").write_text("base\n")
    git(repo, "add", "-A")
    git(repo, "commit", "-q", "-m", "base")
    git(repo, "checkout", "-q", "-b", "node")
    for rel in ("src/a.txt", "src/b.txt", "docs/c.md", ".agents/log/x.jsonl"):
        p = repo / rel
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text("changed\n")
    git(repo, "add", "-A")
    git(repo, "commit", "-q", "-m", "node work")
    git(repo, "checkout", "-q", "main")


def write_log(repo, claims, extra=()):
    log = repo / ".agents" / "log"
    log.mkdir(parents=True, exist_ok=True)
    rows = [{"kind": "claim", "session": SESSION, "path": p, "at": 1.0, "ttl": 300.0, "wi": "w"} for p in claims]
    rows += list(extra)
    (log / f"{SESSION}.jsonl").write_text("".join(json.dumps(r) + "\n" for r in rows))


def run(repo, session=SESSION):
    r = subprocess.run([sys.executable, "-I", str(TOOL), str(repo), "--branch", "node", "--session", session],
                       capture_output=True, text=True)
    return r.returncode, r.stdout + r.stderr


def main():
    failures = []

    def expect(name, cond, out):
        if not cond:
            failures.append(f"{name}: unexpected\n{out}")

    with tempfile.TemporaryDirectory() as tmp:
        repo = pathlib.Path(tmp)
        build(repo)

        write_log(repo, ["src/a.txt", "src/b.txt"])
        rc, out = run(repo)
        expect("two of three claimed -> exit 1", rc == 1, out)
        expect("names the unclaimed path", "docs/c.md" in out and "src/a.txt" not in out.split("UNCLAIMED", 1)[-1], out)

        write_log(repo, ["src/a.txt", "src/b.txt", "docs/c.md"])
        rc, out = run(repo)
        expect("all claimed, .agents exempt -> exit 0", rc == 0, out)
        expect("reports session, branch, counts and the exempt class",
               all(s in out for s in (SESSION, "node", "checked 4", "exempt 1")) and "coordination-record" in out, out)

        write_log(repo, ["src", "docs"], extra=[])
        rc, out = run(repo)
        expect("directory leases cover files -> exit 0", rc == 0, out)

        log = repo / ".agents" / "log" / f"{SESSION}.jsonl"
        rows = [json.loads(line) for line in log.read_text().splitlines()]
        rows[0]["except"] = ["src/b.txt"]
        log.write_text("".join(json.dumps(r) + "\n" for r in rows))
        rc, out = run(repo)
        expect("--except carve-out is unclaimed -> exit 1", rc == 1 and "src/b.txt" in out, out)

        rc, out = run(repo, session="wl-no-such-session")
        expect("missing log -> exit 1 with reason", rc == 1 and "no session log" in out, out)

        log.write_text("not json\n")
        rc, out = run(repo)
        expect("unreadable log -> exit 1", rc == 1 and "unreadable" in out, out)

    for f in failures:
        print("FAIL " + f)
    print(f"test-leader-prejoin-claims: {'FAILED' if failures else 'ok'} ({len(failures)} failures)")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
