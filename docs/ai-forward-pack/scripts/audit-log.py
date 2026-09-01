#!/usr/bin/env python3
"""audit-log.py — the AI-Forward Pack audit & change log bundle (audit-and-change-log.md).

Durable, committed, history-as-knowledge for a repo: an append-only record of every
meaningful prompt / skill / script / decision, so any future Copilot or Claude Code
session reads the project's own history instead of starting blind. The canonical logs
are append-only JSONL (clean git diffs, like docs/health-history.jsonl); the viewer
reads a derived window.AUDIT_DATA JS (loadable over file://, like docs/docs-index.js).
Python 3.8+, stdlib only — no dependencies.

Two logs, one bundle:
  docs/audit/audit-log.jsonl    every action (shortname, datetime, session, prompt, summary, …)
  docs/audit/change-log.jsonl   the meaningful design changes / decisions (+ git before/after)
  docs/audit/audit-data.js      derived window.AUDIT_DATA = {audit:[…], changes:[…]} (the viewer's data)
  docs/audit/index.html         the interactive viewer (self-bootstrapped from the template)

Subcommands
  append      Add an audit entry.            (Audit Mandate — every skill's last action)
  change      Add a change-log entry.        (Change Mandate — collectknowledge/define-architecture/design/migrate)
  list        Show the last N entries (audit|change). For the CLI skill.
  search      Filter by --session / --since / --until / --keyword. For the CLI skill.
  get         Print one entry by --id (use --field prompt to extract the prompt to re-run).
  render      Regenerate audit-data.js from the JSONL and ensure the viewer exists (repair).
  git-context Print the current git {sha, short, branch, pushed} as JSON (a helper).
  verify      Fail when any log line is unreadable — the system of record must never lose
              an entry silently (FR-052). CI-able.
  suggest     Discern unlogged meaningful changes (recent commits + new ADRs/notes not in the change log).
  import      Ingest a session-export JSON array of turns into the audit log (build on session history).

Conventions
  --root defaults to docs/. The audit dir is <root>/audit. The viewer template is resolved
  relative to this script (pack/templates or docs/ai-forward-pack/templates). git is optional —
  every git call degrades gracefully when git or a repo is absent. This tool never invents a
  prompt or a summary; required fields must be supplied (flags, --*-file, or --from-json -).
"""
import argparse, datetime, html, json, os, re, subprocess, sys, time

# Windows consoles default to cp1252, which cannot encode the box/arrow glyphs this
# tool prints - `prompt-log.py --help` crashed outright with UnicodeEncodeError (FR-047).
# The other scripts survived only because their glyphs happen to exist in cp1252, which is
# luck rather than an invariant, so the guard is applied uniformly.
for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        try:
            _stream.reconfigure(encoding="utf-8", errors="replace")
        except (ValueError, OSError):
            pass


ISO = "%Y-%m-%dT%H:%M:%SZ"
AUDIT_KINDS = ["skill", "command", "script", "prompt", "commit", "manual", "session-import"]
CHANGE_KINDS = ["architecture", "design", "knowledge", "migration", "decision", "spec", "other"]


def now_iso():
    return datetime.datetime.now(datetime.timezone.utc).strftime(ISO)


def parse_iso(s):
    """Parse an ISO-8601 UTC stamp. Returns None on anything unparseable -- an unusable
    --started value must degrade to 'no duration recorded', never to a wrong duration."""
    if not s:
        return None
    t = str(s).strip().replace("Z", "+00:00")
    try:
        d = datetime.datetime.fromisoformat(t)
    except ValueError:
        return None
    return d if d.tzinfo else d.replace(tzinfo=datetime.timezone.utc)


def duration_fields(started, ended_iso):
    """Instrumentation over inference: when a start stamp is supplied, RECORD the elapsed
    seconds rather than leaving a future reader to model it. Returns {} when the start is
    absent or unparseable, and refuses a negative duration (clock skew) rather than
    emitting a number that is precise and wrong."""
    s = parse_iso(started)
    e = parse_iso(ended_iso) or datetime.datetime.now(datetime.timezone.utc)
    if s is None:
        return {}
    secs = (e - s).total_seconds()
    if secs < 0:
        return {"started_at": s.strftime(ISO), "duration_seconds": None,
                "duration_note": "start is after end (clock skew); duration not recorded"}
    return {"started_at": s.strftime(ISO), "duration_seconds": round(secs, 1)}


# --- run-start markers: make duration DEFAULT-ON (IO1) -------------------------------
# A flag someone has to remember is not "measurable by default". The grounding step calls
# `start --session <id>`, which persists the stamp; `append` then picks it up automatically,
# so no caller threads a variable through and no skill can silently forget the measurement.
# The store is ephemeral per-run state, not project history -- it is git-ignored, and its
# absence degrades to "no duration recorded", never to a wrong one (IO8).
STARTS_FILE = ".run-starts.json"
STARTS_MAX_AGE_DAYS = 7


def _starts_path(root):
    return os.path.join(audit_dir(root), STARTS_FILE)


def _read_starts(root):
    try:
        with open(_starts_path(root), "r", encoding="utf-8") as f:
            d = json.load(f)
        return d if isinstance(d, dict) else {}
    except (OSError, ValueError):
        return {}  # unreadable/corrupt -> no duration, never a wrong duration (IO8)


def _write_starts(root, data):
    p = _starts_path(root)
    try:
        os.makedirs(os.path.dirname(p), exist_ok=True)
        tmp = p + ".tmp"
        with open(tmp, "w", encoding="utf-8") as f:
            json.dump(data, f, indent=2, sort_keys=True)
        os.replace(tmp, p)
    except OSError as exc:
        # FR-052. Never let bookkeeping fail the audit write itself — but never lose the
        # measurement silently either (IO8: degrade to "not recorded", AND say so).
        print(f"warning: could not persist the run-start marker at {p} ({exc}). "
              f"This run will have no measured duration.", file=sys.stderr)


def _prune_starts(data):
    """Drop markers older than the max age so an abandoned run cannot later attach an absurd
    duration to an unrelated entry."""
    cutoff = datetime.datetime.now(datetime.timezone.utc) - datetime.timedelta(days=STARTS_MAX_AGE_DAYS)
    out = {}
    for k, v in data.items():
        t = parse_iso(v)
        if t is not None and t >= cutoff:
            out[k] = v
    return out


def record_start(root, session, stamp=None):
    stamp = stamp or now_iso()
    data = _prune_starts(_read_starts(root))
    if session:
        data[str(session)] = stamp
    _write_starts(root, data)
    return stamp


def consume_start(root, session):
    """Return the recorded start for this session and clear it, so one marker measures one
    run. Returns None when there is none -- which degrades to no duration (IO8)."""
    if not session:
        return None
    data = _prune_starts(_read_starts(root))
    stamp = data.pop(str(session), None)
    if stamp is not None:
        _write_starts(root, data)
    return stamp



def audit_dir(root):
    return os.path.join(root, "audit")


def log_path(root, which):
    return os.path.join(audit_dir(root), "audit-log.jsonl" if which == "audit" else "change-log.jsonl")


# ---------- JSONL read / append ----------
# FR-052. A malformed line must not be fatal (the log has to keep working) but it must not
# be INVISIBLE either: this file is the system of record and the corpus /dream mines, so a
# silently-dropped entry means a consolidation pass reasons over an incomplete corpus while
# reporting success — a success-shaped failure in the system built to prevent them. So the
# read still succeeds, and every skip is counted, warned about, and assertable by `verify`.
LOG_READ_SKIPS = []


def read_log(root, which, warn=True):
    p = log_path(root, which)
    if not os.path.exists(p):
        return []
    out = []
    with open(p, encoding="utf-8") as handle:
        for lineno, ln in enumerate(handle, 1):
            ln = ln.strip()
            if not ln:
                continue
            try:
                out.append(json.loads(ln))
            except json.JSONDecodeError as exc:
                LOG_READ_SKIPS.append({"file": p, "line": lineno, "error": str(exc)})
                if warn:
                    print(f"warning: {p}:{lineno} is not valid JSON and was SKIPPED ({exc}). "
                          f"This log is the system of record — repair the line; "
                          f"`audit-log.py verify` fails while it is unreadable.",
                          file=sys.stderr)
    return out


def append_log(root, which, entry):
    os.makedirs(audit_dir(root), exist_ok=True)
    with open(log_path(root, which), "a", encoding="utf-8") as f:
        f.write(json.dumps(entry, ensure_ascii=False) + "\n")


def _git_common_dir(root):
    """The directory every worktree of this repository shares, or None."""
    try:
        cwd = os.path.dirname(os.path.abspath(root)) or "."
        r = subprocess.run(["git", "rev-parse", "--git-common-dir"],
                           cwd=cwd, capture_output=True, text=True, timeout=15)
        if r.returncode != 0:
            return None
        path = r.stdout.strip()
        return path if os.path.isabs(path) else os.path.abspath(os.path.join(cwd, path))
    except (OSError, subprocess.SubprocessError):
        return None


def _reserve(root, prefix, floor):
    """
    Reserve the next number for `prefix`, atomically across every worktree of this repository.

    DC-013, three times: an id allocated as "highest present, plus one" is correct inside one
    checkout and wrong the moment there are two. The third occurrence was between two AGENTS rather
    than two trees of one — which broke the previous control, because "do not run a log-writing
    script in the primary checkout while a worktree is open" is advice that cannot reach a session
    that is not yours.

    Every worktree of a repository shares ONE git common directory. A counter there is visible to
    both, and an exclusive-create lock around it makes the read-modify-write atomic — so two
    sessions cannot be handed the same number, whatever order they run in.

    The file's own highest id is still the floor, so a counter that is missing, stale, or from a
    fresh clone can only ever be caught up to reality, never behind it. Without git this returns
    None and the caller falls back to the old behaviour, which is exactly as safe as it ever was.
    """
    shared = _git_common_dir(root)
    if not shared or not os.path.isdir(shared):
        return None

    counter = os.path.join(shared, "aide-id-counter-{}.txt".format(prefix))
    lock = counter + ".lock"

    for _ in range(200):
        try:
            handle = os.open(lock, os.O_CREAT | os.O_EXCL | os.O_WRONLY)
        except FileExistsError:
            time.sleep(0.01)
            continue
        except OSError:
            return None

        try:
            stored = 0
            try:
                with open(counter, "r", encoding="utf-8") as f:
                    stored = int((f.read() or "0").strip() or 0)
            except (OSError, ValueError):
                stored = 0

            allocated = max(stored, floor) + 1

            with open(counter, "w", encoding="utf-8") as f:
                f.write(str(allocated))

            return allocated
        finally:
            os.close(handle)
            try:
                os.unlink(lock)
            except OSError:
                pass

    # A lock held for two seconds is a stale lock or a very unlucky day. Falling back is safe: the
    # file's own highest id still applies, and verify-audit-log.py is the backstop either way.
    return None


def next_id(entries, prefix, root=None):
    n = 0
    for e in entries:
        m = re.match(prefix + r"-(\d+)$", str(e.get("id", "")))
        if m:
            n = max(n, int(m.group(1)))

    reserved = _reserve(root, prefix, n) if root is not None else None
    return f"{prefix}-{reserved if reserved is not None else n + 1:04d}"


# ---------- git helpers (always graceful) ----------
def git(args, root):
    try:
        cwd = os.path.dirname(os.path.abspath(root)) or "."
        r = subprocess.run(["git", *args], cwd=cwd, capture_output=True, text=True, timeout=15)
        return r.stdout.strip() if r.returncode == 0 else None
    except (OSError, subprocess.SubprocessError):
        return None


def git_context(root):
    sha = git(["rev-parse", "HEAD"], root)
    if not sha:
        return {"sha": None, "short": None, "branch": None, "pushed": None}
    branch = git(["rev-parse", "--abbrev-ref", "HEAD"], root)
    # pushed == no commits ahead of the upstream (None when there is no upstream)
    ahead = git(["rev-list", "--count", "@{upstream}..HEAD"], root)
    pushed = (ahead == "0") if ahead is not None else None
    return {"sha": sha, "short": sha[:9], "branch": branch, "pushed": pushed}


def commits_between(before, after, root):
    if not before or not after:
        return []
    out = git(["log", "--pretty=%h %s", f"{before}..{after}"], root)
    return [ln for ln in (out or "").split("\n") if ln.strip()]


# ---------- graph hub node (AL7: the bundle must BE a graph node) ----------
HUB = """---
id: audit-log
title: "Audit & Change Log"
type: doc
status: accepted
owner: "@maintainers"
tags: [audit, history, change-log, project-memory]
links: []
review-by: %s
review-suggested: []
summary: >-
  The durable, committed history of what was prompted, done, and decided in this
  repository, so work compounds across sessions. The two JSONL files are the source
  of truth; audit-data.js and index.html are derived projections.
---

# Audit & Change Log

`audit-log.jsonl` records every meaningful prompt, skill run and script; `change-log.jsonl`
records the design decisions. Browse them at [`index.html`](index.html) or via `/auditlog`.
All writes go through `audit-log.py` - never hand-append the JSONL.
"""


def ensure_hub(adir):
    """AL7 requires the bundle be registered in the knowledge graph through a hub artifact.
    Nothing created it: a fresh install produced audit-data.js, audit-log.jsonl and
    index.html but no .md, so the bundle was invisible to the graph. Bootstrapped here
    alongside the viewer (AL11) because the same trigger applies - if it is missing, make it.

    Links are deliberately EMPTY: a fresh install has no other artifact to point at, and a
    dangling link fails `docs-graph.py validate` outright. An INBOUND link (from the UI guide
    hub, or the first skill-authored artifact) clears the orphan check - verified by execution.
    """
    path = os.path.join(adir, "audit-log.md")
    if os.path.exists(path):
        return False
    stamp = now_iso()
    review = "%d%s" % (int(stamp[:4]) + 1, stamp[4:10])
    with open(path, "w", encoding="utf-8") as handle:
        handle.write(HUB % review)
    return True


# ---------- viewer (self-bootstrap from the template) ----------
def find_template():
    here = os.path.dirname(os.path.abspath(__file__))
    for cand in (os.path.join(here, "..", "templates", "audit-explorer.template.html"),
                 os.path.join(here, "..", "..", "templates", "audit-explorer.template.html")):
        if os.path.exists(cand):
            return os.path.abspath(cand)
    return None


def _remote_project(root):
    """The repository's name, taken from its origin URL. Stable across every worktree."""
    try:
        url = subprocess.run(
            ["git", "-C", root, "config", "--get", "remote.origin.url"],
            capture_output=True, encoding="utf-8", errors="replace").stdout.strip()
    except OSError:
        return None

    if not url:
        return None

    name = url.rstrip("/").rsplit("/", 1)[-1]
    return name[:-4] if name.endswith(".git") else name or None


def project_name(root):
    """What this repository is called, decided by something that does not move.

    It used to be the name of the directory two levels up — which in a worktree is the WORKTREE's
    name, not the repository's. Every session therefore rendered a different `project` into the
    derived view: `ai-de-facelift` when the design session regenerated, `ai-de-session-phase3-pane-probes`
    when this one did. The value flipped back and forth on `main`, produced a spurious diff in a
    generated artifact on every cross-session rebase, and would have made `verify-derived-views`
    fail for a reason that has nothing to do with staleness — a control that cries wolf is one
    people switch off.

    Order: the value already recorded in the derived view (so an established name is never
    renamed by a tool), then the origin URL, then the directory as a last resort.
    """
    # The remote FIRST, not the previous value. "Keep whatever is already there" is stable only
    # until two sessions disagree, and then it is first-writer-wins with a diff every time the
    # other one regenerates. The origin URL is the same string in every worktree, on every machine,
    # forever — which is the only property that makes a generated artifact comparable.
    return _remote_project(root) or os.path.basename(
        os.path.abspath(os.path.join(root, "..")))


def render(root, project=None):
    """Regenerate audit-data.js and the managed viewer from canonical sources."""
    ensure_hub(audit_dir(root))
    os.makedirs(audit_dir(root), exist_ok=True)
    data = {"project": project or project_name(root), "generated": now_iso(),
            "audit": read_log(root, "audit"), "changes": read_log(root, "change")}
    # </ is escaped so a prompt containing </script> can never break the <script> host.
    payload = json.dumps(data, ensure_ascii=False, indent=2).replace("</", "<\\/")
    body = ("// Derived from docs/audit/*.jsonl by scripts/audit-log.py — DO NOT hand-edit"
            " (the JSONL logs are the source of truth; see audit-and-change-log.md).\n"
            "window.AUDIT_DATA = " + payload + ";\n")
    with open(os.path.join(audit_dir(root), "audit-data.js"), "w", encoding="utf-8") as out:
        out.write(body)
    idx = os.path.join(audit_dir(root), "index.html")
    tpl = find_template()
    if tpl:
        with open(tpl, encoding="utf-8") as src:
            viewer = src.read().replace(
                "__PROJECT__", html.escape(data["project"], quote=True)
            )
        with open(idx, "w", encoding="utf-8") as out:
            out.write(viewer)
    return data


# ---------- field collection (flags / files / stdin JSON) ----------
def _read_field(value, value_file):
    if value_file:
        if value_file == "-":
            return sys.stdin.read().rstrip("\n")
        with open(value_file, encoding="utf-8") as handle:
            return handle.read().rstrip("\n")
    return value

def _from_json(arg):
    if not arg:
        return {}
    if arg == "-":
        text = sys.stdin.read()
    else:
        with open(arg, encoding="utf-8") as handle:
            text = handle.read()
    obj = json.loads(text)
    return obj if isinstance(obj, dict) else {}


# ---------- subcommands ----------
def cmd_start(args):
    """Instrumentation over inference (IO1): called as a skill's FIRST action (grounding). It
    persists the run's start stamp keyed by session, so the closing `append` records
    duration_seconds automatically -- no flag to remember, no variable to thread through.
    That is what makes the measurement default-on rather than opt-in."""
    stamp = record_start(args.root, args.session)
    print(stamp)
    return 0


def cmd_append(args):
    base = _from_json(getattr(args, "from_json", None))
    prompt = _read_field(args.prompt, args.prompt_file) or base.get("prompt")
    summary = _read_field(args.summary, args.summary_file) or base.get("summary")
    shortname = args.shortname or base.get("shortname")
    session = args.session or base.get("session")
    missing = [k for k, v in [("shortname", shortname), ("session", session),
                              ("prompt", prompt), ("summary", summary)] if not v]
    if missing:
        sys.stderr.write(f"audit-log append: missing required field(s): {', '.join(missing)}\n")
        return 2
    entries = read_log(args.root, "audit")
    entry = {
        "id": next_id(entries, "al", args.root),
        "shortname": shortname,
        "datetime": args.datetime or base.get("datetime") or now_iso(),
        "session": session,
        "prompt": prompt,
        "summary": summary,
        "kind": args.kind or base.get("kind") or "manual",
        "skill": args.skill or base.get("skill"),
        "tool": args.tool or base.get("tool"),
        "actor": args.actor or base.get("actor"),
        "artifacts": (args.artifact or []) or base.get("artifacts") or [],
        "tags": (args.tag or []) or base.get("tags") or [],
        "outcome": args.outcome or base.get("outcome") or "success",
    }
    # Front-matter goal-state (CT19): done_when is the terminal condition, and is the PACK-O
    # PRESENCE signal /dream mines (a substantive turn without it skipped the front matter, AL5b).
    for _opt in ("goal", "done_when"):
        _v = getattr(args, _opt, None) or base.get(_opt)
        if _v:
            entry[_opt] = _v
    # Watcher telemetry convention (AL2a): an OPTIONAL `signals` object carrying the deterministic
    # signals a turn actually OBSERVED at close, read by the watcher's DeterministicSignalsDeriver to
    # lift an imported episode above its conservative default. Honest by construction - only supplied
    # fields are emitted, so anything absent stays a conservative default rather than a fabricated
    # value (spec L127 / NG1). The flags cover the safe, close-observable booleans; a richer caller
    # may supply the full object (incl. the judgement-laden guidance_*/coordination_* counts, which
    # get no flag precisely so a harness cannot fabricate them) via --from-json.
    _signals = dict(base.get("signals") or {})
    for _flag, _key in (("signal_verification_path", "verification_path"),
                        ("signal_verification_executed", "verification_executed"),
                        ("signal_acceptance_met", "acceptance_met"),
                        ("signal_regression", "regression")):
        _sv = getattr(args, _flag, None)
        if _sv is not None:
            _signals[_key] = (_sv == "true")
    if _signals:
        entry["signals"] = _signals
    # Instrumentation over inference (IO1): duration is DEFAULT-ON. An explicit --started wins;
    # otherwise the stamp recorded by `start --session` at grounding is picked up automatically,
    # so no caller has to remember a flag. Absent/unparseable/skewed -> no duration, never a
    # wrong one (IO8). This closes the gap that forced the optimize-graph back-test to model
    # elapsed time instead of measuring it.
    _started = (args.started or base.get("started_at")
                or consume_start(args.root, session))
    entry.update(duration_fields(_started, entry["datetime"]))
    if args.change or base.get("change"):
        entry["change"] = args.change or base.get("change")
    if args.git:
        entry["git"] = git_context(args.root)
    append_log(args.root, "audit", entry)
    render(args.root, args.project)
    print(entry["id"])
    return 0


def cmd_change(args):
    base = _from_json(getattr(args, "from_json", None))
    title = args.title or base.get("title")
    summary = _read_field(args.summary, args.summary_file) or base.get("summary")
    missing = [k for k, v in [("title", title), ("summary", summary)] if not v]
    if missing:
        sys.stderr.write(f"audit-log change: missing required field(s): {', '.join(missing)}\n")
        return 2
    after = git_context(args.root)
    before = args.git_before or base.get("git_before")
    entries = read_log(args.root, "change")
    entry = {
        "id": next_id(entries, "cl", args.root),
        "datetime": args.datetime or now_iso(),
        "session": args.session or base.get("session"),
        "kind": args.kind or base.get("kind") or "decision",
        "skill": args.skill or base.get("skill"),
        "title": title,
        "prompt": _read_field(args.prompt, args.prompt_file) or base.get("prompt"),
        "summary": summary,
        "rationale": args.rationale or base.get("rationale"),
        "artifacts": (args.artifact or []) or base.get("artifacts") or [],
        "tags": (args.tag or []) or base.get("tags") or [],
        "git": {"before": before, "after": after.get("sha"), "branch": after.get("branch"),
                "pushed": after.get("pushed"), "commits": commits_between(before, after.get("sha"), args.root)},
    }
    if args.supersedes or base.get("supersedes"):
        entry["supersedes"] = args.supersedes or base.get("supersedes")
    if args.audit_ref or base.get("audit_ref"):
        entry["audit_ref"] = args.audit_ref or base.get("audit_ref")
    append_log(args.root, "change", entry)
    render(args.root, args.project)
    print(entry["id"])
    return 0


def _fmt_row(e, which):
    when = (e.get("datetime") or "")[:16].replace("T", " ")
    if which == "audit":
        label = e.get("shortname") or e.get("skill") or e.get("kind") or "—"
    else:
        label = e.get("title") or e.get("kind") or "—"
    summ = (e.get("summary") or "").replace("\n", " ")
    if len(summ) > 80:
        summ = summ[:77] + "…"
    sess = (e.get("session") or "")[:8]
    return f"{e.get('id','?'):<8} {when:<16} {sess:<8} {label[:24]:<24} {summ}"


def cmd_list(args):
    entries = read_log(args.root, args.kind)
    if args.json:
        print(json.dumps(entries[-args.n:], ensure_ascii=False, indent=2))
        return 0
    sel = entries[-args.n:]
    if not sel:
        print(f"(no {args.kind} entries yet)")
        return 0
    print(f"{'id':<8} {'datetime':<16} {'session':<8} {'label':<24} summary")
    for e in sel:
        print(_fmt_row(e, args.kind))
    return 0


def _matches(e, kw):
    if not kw:
        return True
    hay = " ".join(str(e.get(k, "")) for k in
                   ("id", "shortname", "title", "prompt", "summary", "skill", "kind", "actor"))
    hay += " " + " ".join(e.get("tags") or []) + " " + " ".join(e.get("artifacts") or [])
    return kw.lower() in hay.lower()


def cmd_search(args):
    entries = read_log(args.root, args.kind)
    res = []
    for e in entries:
        if args.session and args.session not in str(e.get("session", "")):
            continue
        dt = str(e.get("datetime", ""))
        if args.since and dt < args.since:
            continue
        if args.until and dt > args.until:
            continue
        if not _matches(e, args.keyword):
            continue
        res.append(e)
    if args.json:
        print(json.dumps(res, ensure_ascii=False, indent=2))
        return 0
    if not res:
        print("(no matches)")
        return 0
    print(f"{'id':<8} {'datetime':<16} {'session':<8} {'label':<24} summary")
    for e in res:
        print(_fmt_row(e, args.kind))
    print(f"\n{len(res)} match(es)")
    return 0


def cmd_get(args):
    for which in ("audit", "change"):
        for e in read_log(args.root, which):
            if str(e.get("id")) == args.id:
                if args.field:
                    print(e.get(args.field, ""))
                else:
                    print(json.dumps(e, ensure_ascii=False, indent=2))
                return 0
    sys.stderr.write(f"audit-log get: id not found: {args.id}\n")
    return 1


def cmd_render(args):
    data = render(args.root, args.project)
    print(f"rendered {len(data['audit'])} audit + {len(data['changes'])} change entries "
          f"-> {os.path.join(audit_dir(args.root), 'audit-data.js')}")
    return 0


def cmd_git_context(args):
    print(json.dumps(git_context(args.root), indent=2))
    return 0


def cmd_verify(args):
    """FR-052. Assert the system of record is fully readable.

    `read_log` deliberately survives a malformed line so the tooling keeps working — but a
    line that is silently dropped is a line /dream will never see, and the consolidation
    would report success over an incomplete corpus. This makes the skip COUNTABLE and
    therefore gateable: exit 1 while any line is unreadable, naming file and line number.
    """
    del LOG_READ_SKIPS[:]
    counts = {}
    for which in ("audit", "change"):
        counts[which] = len(read_log(args.root, which, warn=False))
    if not LOG_READ_SKIPS:
        print(f"audit log verified: {counts['audit']} audit + {counts['change']} change "
              f"entries, 0 unreadable lines")
        return 0
    for skip in LOG_READ_SKIPS:
        print(f"UNREADABLE {skip['file']}:{skip['line']}: {skip['error']}", file=sys.stderr)
    print(f"audit log verify: {len(LOG_READ_SKIPS)} unreadable line(s) — these entries are "
          f"invisible to every reader, including /dream. Repair them.", file=sys.stderr)
    return 1


def cmd_suggest(args):
    """Advisory: surface meaningful changes that may not be in the change log yet."""
    changes = read_log(args.root, "change")
    last_after = None
    for e in changes:
        a = (e.get("git") or {}).get("after")
        if a:
            last_after = a
    head = git(["rev-parse", "HEAD"], args.root)
    findings = []
    if last_after and head:
        for ln in commits_between(last_after, head, args.root):
            findings.append(("commit", ln))
    elif head:
        for ln in (git(["log", "-n", str(args.n), "--pretty=%h %s"], args.root) or "").split("\n"):
            if ln.strip():
                findings.append(("commit", ln))
    # New decision artifacts (ADRs / decision notes) not referenced by any change entry.
    referenced = " ".join(json.dumps(e) for e in changes)
    for sub in ("adr", "notes"):
        d = os.path.join(args.root, sub)
        if os.path.isdir(d):
            for f in sorted(os.listdir(d)):
                if f.endswith(".md") and f[:-3] not in referenced and f not in referenced:
                    findings.append(("artifact", f"docs/{sub}/{f}"))
    if not findings:
        print("no unlogged meaningful changes detected")
        return 0
    print("Possible meaningful changes not yet in the change log "
          "(promote with `audit-log.py change`):")
    for kind, what in findings:
        print(f"  [{kind}] {what}")
    return 0


def cmd_import(args):
    """Ingest a session-export JSON array of turns into the audit log (build on session history)."""
    if args.file == "-":
        text = sys.stdin.read()
    else:
        with open(args.file, encoding="utf-8") as handle:
            text = handle.read()
    rows = json.loads(text)
    if isinstance(rows, dict):
        rows = rows.get("turns") or rows.get("entries") or [rows]
    entries = read_log(args.root, "audit")
    existing = {e.get("id") for e in entries}
    added = 0
    for r in rows:
        prompt = r.get("prompt") or r.get("user_message") or ""
        summary = r.get("summary") or r.get("assistant_response") or ""
        if not (prompt or summary):
            continue
        if len(summary) > 280:
            summary = summary[:277] + "…"
        eid = next_id(entries, "al", args.root)
        entry = {
            "id": eid,
            "shortname": r.get("shortname") or (r.get("session", "") or "session")[:8] + f"-t{r.get('turn_index', added)}",
            "datetime": r.get("datetime") or r.get("timestamp") or now_iso(),
            "session": r.get("session") or r.get("session_id") or args.session,
            "prompt": prompt,
            "summary": summary or "(imported turn)",
            "kind": "session-import",
            "skill": r.get("skill"),
            "tool": r.get("tool") or args.tool,
            "actor": r.get("actor"),
            "artifacts": r.get("artifacts") or [],
            "tags": r.get("tags") or ["imported"],
            "outcome": r.get("outcome") or "success",
        }
        if eid in existing:
            continue
        entries.append(entry)
        append_log(args.root, "audit", entry)
        existing.add(eid)
        added += 1
    render(args.root, args.project)
    print(f"imported {added} session turn(s) into the audit log")
    return 0


def main():
    ap = argparse.ArgumentParser(prog="audit-log.py", description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--root", default="docs", help="docs root (default: docs); audit dir is <root>/audit")
    ap.add_argument("--project", default=None, help="project name for the viewer (default: repo dir name)")
    sub = ap.add_subparsers(dest="cmd", required=True)

    ap_a = sub.add_parser("append", help="add an audit entry")
    ap_a.add_argument("--shortname"); ap_a.add_argument("--session"); ap_a.add_argument("--prompt")
    ap_a.add_argument("--prompt-file", dest="prompt_file"); ap_a.add_argument("--summary")
    ap_a.add_argument("--summary-file", dest="summary_file"); ap_a.add_argument("--datetime")
    ap_a.add_argument("--kind", choices=AUDIT_KINDS); ap_a.add_argument("--skill")
    ap_a.add_argument("--tool"); ap_a.add_argument("--actor")
    ap_a.add_argument("--artifact", action="append"); ap_a.add_argument("--tag", action="append")
    ap_a.add_argument("--outcome", choices=["success", "partial", "failed", "blocked"])
    ap_a.add_argument("--change", help="link to a change-log id (cl-NNNN)")
    ap_a.add_argument("--goal", help="the turn's goal (front matter CT19)")
    ap_a.add_argument("--done-when", dest="done_when", help="the terminal condition (front matter CT19); the PACK-O presence signal /dream mines (AL5b)")
    # Watcher telemetry convention (AL2a): the safe, close-observable deterministic signals a turn
    # may record. Tri-state (absent -> omitted -> the watcher reader's conservative default; NG1).
    ap_a.add_argument("--signal-verification-path", dest="signal_verification_path",
                      choices=["true", "false"],
                      help="signal: a committed Proof Pack / verification path exists (watcher AL2a)")
    ap_a.add_argument("--signal-verification-executed", dest="signal_verification_executed",
                      choices=["true", "false"],
                      help="signal: the verification (red-observed) was actually executed")
    ap_a.add_argument("--signal-acceptance-met", dest="signal_acceptance_met",
                      choices=["true", "false"],
                      help="signal: the done_when acceptance criterion was met")
    ap_a.add_argument("--signal-regression", dest="signal_regression",
                      choices=["true", "false"],
                      help="signal: a known regression was introduced (a claim, so emit only when checked)")
    ap_a.add_argument("--git", action="store_true", help="capture current git context")
    ap_a.add_argument("--started", help="ISO-8601 UTC start stamp captured at grounding; records "
                                        "started_at + duration_seconds so elapsed time is MEASURED, "
                                        "not modeled (instrumentation over inference, IO1)")
    ap_a.add_argument("--from-json", dest="from_json", help="read fields from a JSON object (path or - for stdin)")

    ap_c = sub.add_parser("change", help="add a change-log entry")
    ap_c.add_argument("--title"); ap_c.add_argument("--summary"); ap_c.add_argument("--summary-file", dest="summary_file")
    ap_c.add_argument("--session"); ap_c.add_argument("--kind", choices=CHANGE_KINDS); ap_c.add_argument("--skill")
    ap_c.add_argument("--prompt"); ap_c.add_argument("--prompt-file", dest="prompt_file"); ap_c.add_argument("--rationale")
    ap_c.add_argument("--artifact", action="append"); ap_c.add_argument("--tag", action="append")
    ap_c.add_argument("--supersedes"); ap_c.add_argument("--audit-ref", dest="audit_ref")
    ap_c.add_argument("--git-before", dest="git_before", help="HEAD sha captured before the work began")
    ap_c.add_argument("--datetime")
    ap_c.add_argument("--from-json", dest="from_json", help="read fields from a JSON object (path or - for stdin)")

    ap_l = sub.add_parser("list", help="show the last N entries")
    ap_l.add_argument("--n", type=int, default=10); ap_l.add_argument("--kind", choices=["audit", "change"], default="audit")
    ap_l.add_argument("--json", action="store_true")

    ap_s = sub.add_parser("search", help="filter by session/datetime/keyword")
    ap_s.add_argument("--kind", choices=["audit", "change"], default="audit")
    ap_s.add_argument("--session"); ap_s.add_argument("--since"); ap_s.add_argument("--until")
    ap_s.add_argument("--keyword"); ap_s.add_argument("--json", action="store_true")

    ap_g = sub.add_parser("get", help="print one entry by id")
    ap_g.add_argument("--id", required=True); ap_g.add_argument("--field", help="print just this field (e.g. prompt)")

    sub.add_parser("render", help="regenerate audit-data.js and ensure the viewer exists")
    sub.add_parser("git-context", help="print current git context as JSON")
    sub.add_parser("verify", help="fail if any log line is unreadable (FR-052; CI-able)")

    ap_sug = sub.add_parser("suggest", help="surface meaningful changes not yet in the change log")
    ap_sug.add_argument("--n", type=int, default=15)

    ap_imp = sub.add_parser("import", help="ingest a session-export JSON array into the audit log")
    ap_imp.add_argument("--file", default="-", help="JSON file (or - for stdin)")
    ap_imp.add_argument("--session"); ap_imp.add_argument("--tool")

    ap_st = sub.add_parser("start", help="record this run's start stamp (call at grounding) so the "
                                         "closing `append` records duration automatically (IO1)")
    ap_st.add_argument("--session", help="session id the closing append will use")

    args = ap.parse_args()
    dispatch = {
        "append": cmd_append, "change": cmd_change, "list": cmd_list, "search": cmd_search,
        "get": cmd_get, "render": cmd_render, "git-context": cmd_git_context,
        "suggest": cmd_suggest, "import": cmd_import, "start": cmd_start,
        "verify": cmd_verify,
    }
    sys.exit(dispatch[args.cmd](args))


if __name__ == "__main__":
    main()
