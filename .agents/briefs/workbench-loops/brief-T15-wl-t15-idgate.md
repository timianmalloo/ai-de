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
- `--self-test` plants a real-shaped address (e.g. `jane.doe@realmail.test`) in a temporary file of a temporary git repo,
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
registry), `docs/proof/workbench-loops-support/identity-gate/**`.
Read `tools/verify-no-machine-paths.py`'s pack copy (`docs/ai-forward-pack/scripts/verify-no-machine-paths.py`) and any
existing `tools/verify-*.py` first, and match their shape: argument parsing, `--self-test`, message format, exit codes,
and `encoding="utf-8", errors="replace"` on every subprocess call (the subprocess-utf8 gate checks it).

## Proof
`docs/proof/workbench-loops-support/identity-gate/gate-red.txt`, `docs/proof/workbench-loops-support/identity-gate/gate-green.txt`, `docs/proof/workbench-loops-support/identity-gate/self-test.txt`, `docs/proof/workbench-loops-support/identity-gate/gate-line.txt` (the gate's row from
`run-verify-gates`), and `docs/proof/workbench-loops-support/identity-gate/README.md` (V2 frontmatter, `type: proof-pack`, links `implements` → `architecture`).

## Standing rules for this track (CTX-F — every line binds)

You are a **Sub-Agent** in the workbench-loops drive. The Leader is `wl-leader2-20261019`. The
coordination plan is `docs/coordination/coordination-workbench-loops.md` (your row in "Tracks" and
"Track contracts" is your contract). Leader epoch: **2**. Decision requests go to the Owner
`wl-owner-20261018` (`python docs/ai-forward-pack/scripts/coord-decide.py request --to wl-owner-20261018 ...`) — only
for a genuine product trade-off the repo, spec, ADRs or a spike cannot answer, with a recommendation
and a default.

1. **First command, before any reading:** in your tree,
   `cd C:\Projects\ai-de-account-identity-gate && AGENT_SESSION=wl-t15-idgate AGENT_HOST=claude AGENT_WI=workbench-loops python docs/ai-forward-pack/scripts/audit-log.py start --session wl-t15-idgate --skill implement`.
   Prefix **every** coord / audit-log / git command with `AGENT_SESSION=wl-t15-idgate AGENT_HOST=claude AGENT_WI=workbench-loops`.
2. **Your tree is `C:\Projects\ai-de-account-identity-gate` (branch `account-identity-gate`).** Every Bash command starts with `cd C:\Projects\ai-de-account-identity-gate && `.
   Every Read/Edit/Write uses an absolute path under `C:\Projects\ai-de-account-identity-gate\`. **Never** touch `C:\Projects\ai-de\`
   (the primary checkout) or another track's tree. **Never call `EnterWorktree` or `ExitWorktree`.**
3. **Windows:** run `python`, never `python3`. `coord <verb>` = `python docs/ai-forward-pack/scripts/coord-core.py <verb>`.
4. **Owned authored paths** are listed below. You edit nothing else. Need a change outside them →
   `coord request add --to wl-leader2-20261019 --deadline 1800 --fallback "<what you do meanwhile>" "<the request>"`
   and carry on with what you own. `derived` files (docs/docs-index.js, docs/audit/audit-data.js,
   docs/audit/index.html, docs/api/*.md, docs/_site/*, docs/_meta.json) are regenerated at the join —
   do not edit or claim them. `register` files (docs/audit/*.jsonl, .agents/log/*.jsonl) are
   append-only and **never claimed**. Do not edit the test-count baseline; the join recounts.
5. **Claim for the minutes of the edit:** `coord claim --path <file>` (default TTL) just before
   editing a file, `coord release` after the commit. Never a lifetime lease.
6. **A multi-line program is a file, then a run** — never a heredoc program. **A gate's exit status is
   never behind a pipe**: run the test/build on its own line and read `$?`, or redirect output to a
   file and then read the file.
7. **Red first on every check named in your exit evidence.** For each: write the test, run it, see it
   fail for the right reason, save the output to `docs/proof/workbench-loops-support/identity-gate/<check>-red.txt`, commit
   (`test: red — <check>`); then implement, re-run, save `docs/proof/workbench-loops-support/identity-gate/<check>-green.txt`, commit. A test
   that never failed is not evidence. Tests: `dotnet test <project> --filter <...> -nologo` (node reuse
   is off at the boundary via Directory.Build.rsp). Before you report done, the **whole** affected test
   projects must be green (`dotnet test tests/AiDe.Core.Tests -nologo`, and `tests/AiDe.App.Tests`
   when you touched App) — save that run as `docs/proof/workbench-loops-support/identity-gate/suite-green.txt`.
8. **Context ceiling 400k tokens.** Near ~350k: commit what is green, write
   `docs/proof/workbench-loops-support/identity-gate/handoff.md` (what is done, what is red, the next step), and stop with a partial report.
9. **Commit on your branch only; do not push; do not merge.** Commit messages end with
   `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`. The Leader joins via `conductor-join.py`.
10. **No dead code survives** (HYG-A): no commented-out code, no unused members.
11. A new defect class you discover → append it at the end of `docs/lessons/defect-classes.md` with id
    `DC-NEW-wl-t15-idgate` (class / sweep / derive / prevent), in the same commit as the fix.
12. **Last actions:** `python docs/ai-forward-pack/scripts/audit-log.py append --shortname "wl-t15-idgate" --session wl-t15-idgate --skill implement --kind skill --prompt "<your goal line>" --summary "<what landed>" --artifact <main file> --goal "<goal>" --done-when "<done when>" --tier T2 --fan-out 0`;
    commit it; then `coord mail send --to wl-leader2-20261019 --kind done --body "<exit evidence: commits, test names, proof files>"`.
    Your final message to the Leader is the same exit-evidence report: every commit sha (red and green),
    every test name added, every proof file, the suite result with pass/fail counts, any seam request
    raised, anything not done. **Your "done" is evidence, not authority** — the Leader re-runs and reads back.
13. **Every Markdown file you add under `docs/` starts with V2 frontmatter** (`id` unique, `title`, `type` — `proof-pack`
    for evidence READMEs, `design` for design docs, `decision-note` for notes — `status`, `owner: "@timianmalloo"`, `links`
    with at least one `implements` edge to its ADR or `architecture`, `review-by`, `summary`). Before your done report run
    `python docs/ai-forward-pack/scripts/docs-graph.py validate` and fix every **problem** it lists. Index drift alone is
    fine, because the join regenerates the index. A file with no frontmatter turned this gate from green to red at a join
    (2026-10-19).
14. **The code on main outranks your brief on what a field means.** Before you implement a field, record or kind that
    your brief describes, open its declaration and doc comment on main. If they disagree with the brief, follow the code.
    Send a seam request naming both texts, and do not build to the brief's wording. A brief that described an offset
    differently from `RunEventRedaction`'s doc comment sent T7 the wrong way (DC-NEW-wl-leader2, 2026-10-19).
15. **A real-lane proof copy never carries account identity (Ruling 157; the repo is public).** Before you commit any file
    captured from a real harness (run logs, frames, transcripts), replace every account email, organization or user-identity
    value with `[scrubbed before commit]`. Record in that folder's README the original's sha256 and the scrubbed field
    names. The gate `tools/verify-no-account-identity.py` refuses email-shaped strings in tracked `docs/proof/**` and
    `.agents/**`. Only `*@example.invalid`, `example.com`/`example.org` and GitHub `users.noreply` addresses pass.
16. **Node size is set by scope, not only by a cap (DC-NEW-wl-leader2-b).** A node carries at most three red-then-green
    checks, or one check plus its real-lane run. At 120 tool calls or 200k context, commit what is green and write
    `handoff.md`, even if a check is half done. The Leader starts the next node fresh. Three Opus nodes in a row
    overran caps sized for twice their real scope (T5 n1 346k, T8 n1 285k, T5 n2 330k).
