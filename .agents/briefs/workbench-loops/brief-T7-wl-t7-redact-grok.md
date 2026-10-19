# Brief — T7 `wl1a-redaction` (slice WL-1a)

**Goal (plan row):** A pure ingest-side credential-sentinel redaction transform.
**Done when:** a pure `(frame, patterns vN) → (redacted frame, redactions[])`; a sentinel in each frame position is
replaced and recorded; non-sentinel bytes are unchanged; tests red then green.
**Tier** T2 · **fan-out** 0 · **budget** 40 tool calls · 150k · 1 h · **deadline** 1 h.
**Convergence:** the function and its tests are green. T5 wires the call into the mapper — not you.

## Binding line (quoted, DC-189)
ADR-0042 §6: "The credential-sentinel scan is an **ingest-side transform inside each mapper**: the stored raw frame
(`acp.frame` / `native.frame`) is the redacted frame plus `redactions: [{offset, len, pattern_id}]` and
`scanned_with: patterns v<n>`; unredacted bytes never reach the run log, the relay, a Proof Pack or an export."
ADR-0042 §8: "Native stderr and the host's own diagnostics pass through the same sentinel/path redaction before any
retained sink; a redacted span is disclosed".

## Do
`src/AiDe.Core/AgentPlane/FrameRedaction.cs`: a static, allocation-light, pure function over the frame text with a
versioned pattern set (`PatternsVersion`), returning the redacted text and the list of `{offset, len, pattern_id}`
(offsets into the **original** frame). Use T3's envelope types for the redaction record (read
`src/AiDe.Core/AgentPlane/RunEvent.cs` on `main`). Reuse any existing sentinel/secret patterns in the repo first
(grep `sentinel`, `Redact`, `credential` in `src/`) — one definition of a credential pattern, not two; if one exists,
call it. Patterns cover at least: Anthropic/OpenAI/xAI/Google/GitHub token shapes and `Bearer …` headers, and the
test sentinel the repo's existing fixtures use. Redact with a fixed marker that preserves no secret bytes. Tests:
sentinel at the start / middle / end / twice / inside a JSON string / adjacent to another; no sentinel ⇒ identical
output and empty list; overlapping patterns ⇒ one span; the version string is stamped. Red first.

## Owned authored paths
new `src/AiDe.Core/AgentPlane/FrameRedaction.cs`, new `tests/AiDe.Core.Tests/AgentPlane/FrameRedactionTests.cs`,
`docs/proof/workbench-loops-wl-1a/redaction/**`.

## Not in scope
`AcpRunEventMapper.cs` and every other mapper (T5 wires it), the run log, stderr plumbing, any other file.

## Standing rules for this track (CTX-F — every line binds)

You are a **Sub-Agent** in the workbench-loops drive. The Leader is `wl-leader2-20261019`. The
coordination plan is `docs/coordination/coordination-workbench-loops.md` (your row in "Tracks" and
"Track contracts" is your contract). Leader epoch: **2**. Decision requests go to the Owner
`wl-owner-20261018` (`python docs/ai-forward-pack/scripts/coord-decide.py request --to wl-owner-20261018 ...`) — only
for a genuine product trade-off the repo, spec, ADRs or a spike cannot answer, with a recommendation
and a default.

1. **First command, before any reading:** in your tree,
   `cd C:\Projects\ai-de-wl1a-redaction && AGENT_SESSION=wl-t7-redact AGENT_HOST=grok AGENT_WI=workbench-loops python docs/ai-forward-pack/scripts/audit-log.py start --session wl-t7-redact --skill implement`.
   Prefix **every** coord / audit-log / git command with `AGENT_SESSION=wl-t7-redact AGENT_HOST=grok AGENT_WI=workbench-loops`.
2. **Your tree is `C:\Projects\ai-de-wl1a-redaction` (branch `wl1a-redaction`).** Every shell command starts with `cd C:\Projects\ai-de-wl1a-redaction && `.
   Every Read/Edit/Write uses an absolute path under `C:\Projects\ai-de-wl1a-redaction\`. **Never** touch `C:\Projects\ai-de\`
   (the primary checkout) or another track's tree. **Never change into another checkout.**
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
   fail for the right reason, save the output to `docs/proof/workbench-loops-wl-1a/redaction/<check>-red.txt`, commit
   (`test: red — <check>`); then implement, re-run, save `docs/proof/workbench-loops-wl-1a/redaction/<check>-green.txt`, commit. A test
   that never failed is not evidence. Tests: `dotnet test <project> --filter <...> -nologo` (node reuse
   is off at the boundary via Directory.Build.rsp). Before you report done, the **whole** affected test
   projects must be green (`dotnet test tests/AiDe.Core.Tests -nologo`, and `tests/AiDe.App.Tests`
   when you touched App) — save that run as `docs/proof/workbench-loops-wl-1a/redaction/suite-green.txt`.
8. **Context ceiling 400k tokens.** Near ~350k: commit what is green, write
   `docs/proof/workbench-loops-wl-1a/redaction/handoff.md` (what is done, what is red, the next step), and stop with a partial report.
9. **Commit on your branch only; do not push; do not merge.** Commit messages end with
   no Co-Authored-By trailer (the runner records the harness and served model). The Leader joins via `conductor-join.py`.
10. **No dead code survives** (HYG-A): no commented-out code, no unused members.
11. A new defect class you discover → append it at the end of `docs/lessons/defect-classes.md` with id
    `DC-NEW-wl-t7-redact` (class / sweep / derive / prevent), in the same commit as the fix.
12. **Last actions:** `python docs/ai-forward-pack/scripts/audit-log.py append --shortname "wl-t7-redact" --session wl-t7-redact --skill implement --kind skill --prompt "<your goal line>" --summary "<what landed>" --artifact <main file> --goal "<goal>" --done-when "<done when>" --tier T2 --fan-out 0`;
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
