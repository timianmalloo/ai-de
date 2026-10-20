"""T40 F3-F5: join the sampler to the trx files. Writes F3-events.md, distribution.md and rates.md. Stdlib only."""
import csv
import glob
import os
import re
import xml.etree.ElementTree as ET
from collections import Counter
from datetime import datetime, timedelta, timezone

ROOT = os.path.dirname(os.path.abspath(__file__))
RUNS = os.path.normpath(os.path.join(ROOT, "..", "..", "..", "..", "focus-runs"))
T36 = {
    "CtrlDigit_WithFocusInsideTheComposerOrTheCanvasPage_SwitchesThePerspective": "10/10",
    "CtrlEnter_OnAFileArtifact_RequestsRevealInGraph": "10/10",
    "ARestoredRowsEvents_AreReachedFromTheRowByKeyboard_AndExposedToUiAutomation": "0/10",
    "CollapsingTheLeftHoldingTheSession_LeavesItsConsoleOpen": "1/10 (run 4)",
    "TheFactoryLanding_IsTheNotebookInTheCenter_ElseTheCentersEmptyCopy_EachWithAFocusTarget": "1/10 (run 6)",
}


def ts(s):
    s = re.sub(r"(\.\d{6})\d+", r"\1", s)
    return datetime.fromisoformat(s).astimezone(timezone.utc)


def local(t):
    return t.rsplit("}", 1)[-1]


def parse(path):
    names, res = {}, []
    for el in ET.parse(path).iter():
        t = local(el.tag)
        if t == "UnitTest":
            for c in el.iter():
                if local(c.tag) == "TestMethod":
                    names[el.get("id")] = c.get("name", "")
        elif t == "UnitTestResult":
            msg = ""
            for c in el.iter():
                if local(c.tag) == "Message":
                    msg = (c.text or "").strip()
                    break
            res.append((names.get(el.get("testId"), el.get("testName")), el.get("outcome"), ts(el.get("startTime")),
                        ts(el.get("endTime")), msg))
    return res


def main():
    rows = []
    with open(os.path.join(RUNS, "sampler.csv"), newline="") as f:
        for r in csv.DictReader(f):
            r["t"] = datetime.fromtimestamp(int(r["utc_ms"]) / 1000, timezone.utc)
            rows.append(r)
    runs = {}
    for p in glob.glob(os.path.join(RUNS, "app-*.trx")):
        runs[int(re.search(r"app-(\d+)\.trx", p).group(1))] = parse(p)
    ks = sorted(runs)
    events, bad = [], {}
    for k in ks:
        for name, out, st, en, msg in runs[k]:
            if out != "Passed":
                bad.setdefault(name, {})[k] = (out, msg.splitlines()[0] if msg else "")
            if out == "Failed" and "(none)" in msg:
                events.append((k, name, st, en, msg.splitlines()[0]))
    selfpid = {r["self_pid"] for r in rows}
    sampler_fg = [r for r in rows if r["fg_pid"] in selfpid]
    gaps = [(b["t"] - a["t"]).total_seconds() for a, b in zip(rows, rows[1:])]
    lines = ["---", "id: proof-wl-focus-none-events", 'title: "Evidence: T40 events joined to the sampler"',
             "type: proof-pack", "status: draft", 'owner: "@timianmalloo"', 'phase: "workbench-loops"',
             "tags: [workbench-loops, evidence, measurement, focus]", "links:",
             "  - { to: architecture, rel: implements }", 'review-by: "2026-11-19"',
             "summary: >-", "  Each focus (none) failure with the sampler rows inside its test span plus 2 s each side.",
             "---", "", "# T40 F3 - events", "",
             "Rows: %d. Gaps between rows: max %.2f s, median %.2f s. Sampler PID as foreground holder: %d rows." % (
                 len(rows), max(gaps) if gaps else 0, sorted(gaps)[len(gaps) // 2] if gaps else 0, len(sampler_fg)), ""]
    if not events:
        lines.append("No test failed with focus on (none) in %d runs: NOT REPRODUCED." % len(ks))
    for k, name, st, en, msg in events:
        w = [r for r in rows if st - timedelta(seconds=2) <= r["t"] <= en + timedelta(seconds=2)]
        inside = [r for r in w if st <= r["t"] <= en]
        holders = Counter(r["fg_proc"] for r in inside)
        lines += ["## Run %d - `%s`" % (k, name), "", "- message: %s" % msg,
                  "- test span: %s to %s (%.2f s)" % (st.strftime("%H:%M:%S.%f")[:-3], en.strftime("%H:%M:%S.%f")[:-3],
                                                    (en - st).total_seconds()),
                  "- rows inside the span: %d; holders inside: %s" % (len(inside), dict(holders)), "",
                  "| utc | in span | fg proc | fg pid | fg class | hwndActive | hwndFocus | focus class | lbutton |",
                  "|---|---|---|---|---|---|---|---|---|"]
        for r in w:
            lines.append("| %s | %s | %s | %s | %s | %s | %s | %s | %s |" % (
                r["t"].strftime("%H:%M:%S.%f")[:-3], "y" if st <= r["t"] <= en else "", r["fg_proc"], r["fg_pid"],
                r["fg_class"], r["hwndActive"], r["hwndFocus"], r["focus_class"], r["lbutton"]))
        lines.append("")
    with open(os.path.join(ROOT, "F3-events.md"), "w") as f:
        f.write("\n".join(lines) + "\n")
    allc = Counter(r["fg_proc"] for r in rows)
    inrun = Counter()
    wins = [(runs[k][0][2], max(x[3] for x in runs[k])) for k in ks if runs[k]]
    for r in rows:
        if any(a <= r["t"] <= b for a, b in wins):
            inrun[r["fg_proc"]] += 1
    d = ["---", "id: proof-wl-focus-none-distribution", 'title: "Evidence: T40 foreground-holder distribution"',
         "type: proof-pack", "status: draft", 'owner: "@timianmalloo"', 'phase: "workbench-loops"',
         "tags: [workbench-loops, evidence, measurement, focus]", "links:", "  - { to: architecture, rel: implements }",
         'review-by: "2026-11-19"', "summary: >-", "  Each process's share of the sampler rows, over all rows and over the runs only.",
         "---", "", "# T40 - who held the foreground", "",
         "| process | all rows | share | rows inside a run window | share |", "|---|---|---|---|---|"]
    for p, n in allc.most_common():
        d.append("| %s | %d | %.1f%% | %d | %.1f%% |" % (p or "(none)", n, 100 * n / len(rows), inrun[p],
                                                        100 * inrun[p] / max(1, sum(inrun.values()))))
    with open(os.path.join(ROOT, "distribution.md"), "w") as f:
        f.write("\n".join(d) + "\n")
    r = ["---", "id: proof-wl-focus-none-rates", 'title: "Evidence: T40 per-test non-pass rates over N full App runs"',
         "type: proof-pack", "status: draft", 'owner: "@timianmalloo"', 'phase: "workbench-loops"',
         "tags: [workbench-loops, evidence, measurement, focus]", "links:", "  - { to: architecture, rel: implements }",
         'review-by: "2026-11-19"', "summary: >-", "  Tests that failed or ended NOT CHECKED in any run, with T36's in-suite column beside them.",
         "---", "", "# T40 F5 - rates (N = %d runs)" % len(ks), "",
         "| Test | Failed | Not passed other (skipped / NOT CHECKED) | runs | T36 in suite | first message line |",
         "|---|---|---|---|---|---|"]
    for name, d_ in sorted(bad.items()):
        nf = sum(1 for o, _ in d_.values() if o == "Failed")
        r.append("| `%s` | %d/%d | %d/%d | %s | %s | %s |" % (name, nf, len(ks), len(d_) - nf, len(ks),
                                                             ",".join(map(str, sorted(d_))), T36.get(name, "not in T36's list"),
                                                             next(iter(d_.values()))[1].replace("|", "/")[:120]))
    for name in T36:
        if name not in bad:
            r.append("| `%s` | 0/%d | 0/%d | - | %s | passed in every run |" % (name, len(ks), len(ks), T36[name]))
    with open(os.path.join(ROOT, "rates.md"), "w") as f:
        f.write("\n".join(r) + "\n")
    print("runs", ks, "events", [(e[0], e[1]) for e in events], "sampler-as-holder", len(sampler_fg))


main()
