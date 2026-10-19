# Brief — T15 `account-identity-gate` (support, Ruling 157) · Claude Code · Sonnet

**Goal (Ruling 157 (3)):** the public repository can never receive an account email through a committed proof or
ledger file. A gate refuses email-shaped strings in tracked `docs/proof/**` and `.agents/**`, and runs on every
gate line.
**Done when:**
- `tools/verify-no-account-identity.py` exists. It reads only **tracked** files (`git ls-files`) under `docs/proof/` and
  `.agents/`, and finds email-shaped strings. It exits 1 listing each as `<path>:<line>: <masked address>`; never print
  the full address, mask the local part. It exits 0 when it finds none.
- The allow-list is exactly: any address at `example.invalid`; any address at `example.com` or `example.org`;
  GitHub's `*@users.noreply.github.com`. Nothing else, and no per-file opt-out.
- `--self-test` plants a real-shaped address (built at run time from parts, so no literal address is ever committed) in a temporary file of a temporary git repo,
  and proves the gate refuses it. It also proves each allow-list form passes and that an untracked file is ignored.
  It exits 0 only when all of that holds.
- The gate is green on main as it stands. The Owner verified that the only email-shaped string under `docs/proof/` is
  `redacted@example.invalid`; check `.agents/**` yourself. If `.agents/**` holds a real address, do not edit that file.
  Report it to the Leader with its path and a masked value, because it is evidence, not yours to change.
- The pack runner enumerates `tools/verify-*.py` (see `tools/run-verify-gates.py:23`), so the gate runs in
  `python tools/run-verify-gates.py`. Confirm that by running the line and quoting the gate's row.
- If the repo's gate self-test registry (`tools/verify-gate-self-tests.py`) lists gates that must carry `--self-test`,
  register the new gate there in the same change.
- Red first: commit the self-test's planted case failing against a stub that always exits 0, then the real gate.
**Tier** T1 · **fan-out** 0 · **budget** 40 tool calls · 120k · 45 min · **deadline** 45 min.
**Not in scope:** ingest redaction (Ruling 157 (4) is the later upgrade); any file under `docs/ai-forward-pack/`; any
edit to an existing proof or ledger file.

## Owned authored paths
New `tools/verify-no-account-identity.py`, `tools/verify-gate-self-tests.py` (registration line only, if it has a
registry), `{PROOF}/**`.
Read `tools/verify-no-machine-paths.py`'s pack copy (`docs/ai-forward-pack/scripts/verify-no-machine-paths.py`) and any
existing `tools/verify-*.py` first, and match their shape: argument parsing, `--self-test`, message format, exit codes,
and `encoding="utf-8", errors="replace"` on every subprocess call (the subprocess-utf8 gate checks it).

## Proof
`{PROOF}/gate-red.txt`, `{PROOF}/gate-green.txt`, `{PROOF}/self-test.txt`, `{PROOF}/gate-line.txt` (the gate's row from
`run-verify-gates`), and `{PROOF}/README.md` (V2 frontmatter, `type: proof-pack`, links `implements` → `architecture`).
