import collections, glob, json
c = collections.Counter(); ex = None; sess = collections.Counter()
for f in glob.glob(".agents/decisions/*.jsonl"):
    for line in open(f, encoding="utf-8"):
        if '"refused"' in line:
            e = json.loads(line)
            if e.get("kind") == "refused":
                c[(e.get("code"), "hook" if "tool" in e or "hook_event" in e else "plain")] += 1
                sess[e.get("session", "")[:14]] += 1
                ex = ex or e
print(dict(c)); print(ex); print(dict(sess))
