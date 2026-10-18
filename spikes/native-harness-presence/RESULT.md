# RESULT — native harness presence spike (2026-10-18)

The machine-generated table is `RESULT.generated.md` (written by `probe.py`; re-running the probe
overwrites only that file). This file is the hand-written record and survives re-runs.

## First run (2026-10-18, original substring-match probe)

| Family | Version observed | Documented tokens in `--help` | Not found at top level |
|---|---|---|---|
| Anthropic / Claude Code | 2.1.302 | `-p`, `--output-format`, `stream-json`, `setup-token` | — |
| OpenAI / Codex | 0.162.1 | `exec`, `app-server`, `login` | — |
| GitHub Copilot CLI | 1.0.98 | `--acp`, `-p`, `--allow-tool`, `login` | — |
| xAI / Grok Build | 1.0.41 | `agent`, `login`, `--prompt` | `stdio` |
| Google / Antigravity | 1.3.5 | `--output-format`, `stream-json` | `login`, `headless` |
| Meta / Muse Code | 1.4.7 → **1.4.10** on the second probe | `exec`, `login`, `serve` | `--json` |

## Subcommand follow-up (same session, read-only)

| Family | Token missing from top-level help | Where it was found | Disposition |
|---|---|---|---|
| Grok Build | `stdio` | `grok agent --help` lists `stdio — Run the agent over stdio`; top-level help names `streaming-json` ("one ACP session update per line, the agent's native format") | **Verified** present at 1.0.41 |
| Antigravity | `login`, `headless` | No `login`/`auth` subcommand; print mode is `--print`/`--prompt` with `--output-format stream-json` and `--input-format stream-json` (NDJSON per turn). Sign-in is the interactive first run | **Verified** headless entry at 1.3.5; sign-in is not a CLI subcommand — the binding launches the interactive flow (ADR-0042 §2) |
| Muse Code | `--json` | `muse exec --help` lists `--json — Emit machine-readable JSONL events on stdout`; `muse login` and `muse serve` (MSP over stdio) exist; `--api-key-stdin` exists and is unreachable from the workbench by ADR-0042 §5's catalog row test | **Verified** present at 1.4.10 |

**Finding (recorded, not inferred):** Muse reported `1.4.7` on the first probe and `1.4.10` minutes
later — the installed CLI self-updated between two read-only invocations. A native route's
qualification fingerprint therefore pins the *observed* version at each attempt (ADR-0042 §7), and
probes never kill a process tree mid-update (ADR-0042 §9).

## Hardened re-run (same day, `RESULT.generated.md`)

All six resolve with exit 0 / 0. **Antigravity reported `1.4.2`** (it was `1.3.5` an hour earlier) —
a second self-update observed in one session, on a second family. Grok's `--prompt` no longer
matches under word-boundary matching at the top level (it is a subcommand option); `agent` and
`login` still do, and `agent stdio` was verified above. Two of six CLIs changed version during a
single architecture session: the per-attempt fingerprint is not optional.

## What this proves and does not prove

- Proves: each binary resolves on the invoking shell's PATH, reports a version, and its own help
  text names the documented headless/login entry. **Verified** on this machine at these versions.
- Does not prove: sign-in state, model access, quota, permission-protocol semantics, or that a
  scoped read-only request succeeds. Those are the per-family qualification records ADR-0042 §7
  requires and remain **Flagged**.
- Oracle limits (SRE review): the probe reads the shell's PATH, not the WPF app's; a missing token
  at a version is a finding against the research note's documented route for that version only.
  The second-run probe uses word-boundary matching, records exit codes and parses the version from
  stdout with a `\d+.\d+.\d+` regex (see `RESULT.generated.md` for the hardened run).
