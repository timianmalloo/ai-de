# Provenance — the Phase-1 exit run

**These are the real artifacts of one real governed run, not authored ones.** They are the run
file that was fed to the shell, the result it wrote, the transcript of everything that was not
protocol, and the diff the governed lane produced.

| | |
| --- | --- |
| Date | 2026-09-09 |
| Host | `TIMMALLSTRIX` (Windows 11 Pro 10.0.26200) |
| Launcher | `AiDe.App.exe --conduct run.json --out result.json` — the shell's own executable, Debug build |
| Composition root | `src/AiDe.App/Conductor/GovernedRunHost.RunAsync` |
| Engine | `claude-code` via `@agentclientprotocol/claude-agent-acp` **0.75.1**, `dist/index.js` |
| Node | v24.18.0 |
| Account | an Anthropic **Max subscription**, authorised by the operator (`conductor-subscription-use-authorised`) |
| Observed auth | `kind=account`, `label="Claude Max"`, `plan="max"` |
| Wall clock | 102 s, measured by the shell that launched it |
| Exit code | **0** — the launcher's own four-point check held |

## What the lane ran against, and why it is a clone

`repositoryRoot` is a **local clone** of `feature/conductor-agent-plane`, not the phase worktree.
That is not cosmetic and it is not a convenience: a governed lane rooted in a *linked worktree* of
this repository has its session rebound to the parent checkout by `RepositoryCorrection`, and
`ProofPackVerifier` then looks for the lane's declared evidence in a working tree that is on `main`
and does not have it — so the episode scores **Not Scored**. Both halves of that were measured
during this node and the class is registered as **DC-115**. The clone is the shape in which the
four-point floor can be met today; the worktree shape is owed a fix.

## Redaction

Every occurrence of the operator's home directory is replaced by `<REDACTED_HOME>`, verbatim and
in place. **Structure is preserved exactly** — key names, nesting, types, array shapes and path
separators are untouched, which is the same rule the ACP frame corpus was redacted under. Nothing
else is altered: no reordering, no re-serialization, no trimming.

## Files

| File | What it is |
| --- | --- |
| `run.json` | The run file, exactly as fed to `--conduct`. |
| `result.json` | `GovernedRunResult`, exactly as the launcher serialized it. |
| `result.log` | The run's diagnostics — every line that was not protocol, in order. |
| `lane.patch` | `git diff` of the governed lane's own worktree, which is the refactor it produced. `AcpJson.cs` is untracked there and so appears only in the applied commit, not in this patch. |

## Re-running it

`run.json` carries absolute paths from this machine (redacted). To re-run: clone the branch,
`npm install` the pinned adapter under `spikes/acp-subscription-lane/`, rewrite the four path fields
and the `coordCommand` shim path, then launch the shell with `--conduct`. The *behavioural* evidence
is the test suite, which is re-runnable with no setup; this directory is the record of the live run,
in the same sense the frame corpus is.
