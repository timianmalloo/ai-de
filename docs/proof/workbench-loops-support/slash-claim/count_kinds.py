import collections, glob, json
for pat in (".agents/log/*.jsonl", ".agents/decisions/*.jsonl"):
    c = collections.Counter(); bad = 0
    for f in glob.glob(pat):
        for line in open(f, encoding="utf-8", errors="replace"):
            if line.strip():
                try: c[json.loads(line).get("kind")] += 1
                except ValueError: bad += 1
    print(pat, dict(sorted(c.items(), key=lambda kv: str(kv[0]))), "unparsed", bad)
