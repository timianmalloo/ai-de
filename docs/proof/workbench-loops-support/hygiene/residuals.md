---
id: proof-wl-hygiene-residuals
title: "T13 named residuals - gates no workflow invokes, and the gates that stay red"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "workbench-loops"
tags: [proof-pack, gate-hygiene, residuals, ruling-151, workbench-loops]
links:
  - { to: architecture, rel: implements }
review-by: 2027-01-19
review-suggested: []
summary: >-
  Residual red gates after T13, each with its cause and next step. The Leader declined the CI change
  (req-01M58W131E19CB9H3810ZAN1PG) for this track, so the workflow steps are recorded here, not added.
---

# T13 residuals

## RES-1 Two gates no workflow invokes (verify-project-coverage stays red on these two lines)

`tools/verify-project-coverage.py` reads `tools/(verify-[\w-]+)\.py` in `.github/workflows/*.yml`. Neither gate below appears there.
The Leader declined the workflow edit for T13; this is the next step for the Owner. `.github/workflows` was not touched.

Steps I would add to the `ubuntu-latest` gates job in `.github/workflows/build.yml`, beside "Mockup audits gate" (line 531):

```yaml
      - name: Preservation workflow policy gate - self-test
        if: ${{ !cancelled() }}
        run: python tools/verify-preservation-workflow-policy.py --self-test

      - name: Preservation workflow policy matches main
        if: ${{ !cancelled() }}
        run: python tools/verify-preservation-workflow-policy.py

      - name: Workbench v2 gate - self-test
        if: ${{ !cancelled() }}
        run: python tools/verify-workbench-v2.py --self-test
```

- `verify-preservation-workflow-policy.py` compares `.github/workflows` on `HEAD` with `main` through git. It needs `fetch-depth: 0` or a fetched `main`
  on a shallow checkout, else it exits 2 (cannot resolve). Not measured on a runner. Local cost: well under 1 s; `--self-test` builds a temp repo.
- `verify-workbench-v2.py` imports `playwright.sync_api` and launches Chromium. A runner needs `pip install playwright` and
  `playwright install chromium` (not in the job today: `verify-mockup-audits.py` uses the runner's own browser and no playwright). Runner cost: not measured on CI.
  Locally the full journey takes under a minute (36 checks); the `--self-test` takes a few seconds. The bare journey step
  (`python tools/verify-workbench-v2.py`) needs the same install and is the stronger control; the self-test alone only proves the gate can fail.
  Either step satisfies the coverage regex.
- Decision for the Owner: add the playwright install (extra minutes per push, maybe ~1 to 2 min, Inferred) or run only the self-test and a local-only journey.

## RES-2 Gates still red, with cause

| Gate | Cause | State |
|---|---|---|
| verify-ruling-citations (pack copy) | pack reads only docs/notes/rulings.md | pack defect PD-1 |
| verify-portable-text-io, verify-subprocess-utf8 | one byte-frozen oracle, no pack exemption | pack defect PD-2 |
| verify-audit-capture | 5 entries from drive tracks (wl-owner x2, wl-leader, wl-t1, wl-t2); each needs a superseding entry with goal, done-when and evidence from its owner | returned to those sessions |
| verify-project-coverage | RES-1 above | residual |

## RES-3 — The local run log keeps the account email and organization (Ruling 157)

- **What:** the claude-code adapter (`@agentclientprotocol/claude-agent-acp` 0.75.1) puts the operator's account `email`
  and `organization` into three `_auth/status_update` frames of every run. `FrameRedaction` patterns v1 (ADR-0042 §6)
  cover credentials only, so `RunLogStore` writes these values to `runs/<dispatch_key>.jsonl` on the operator's machine.
- **Why it is accepted:** this is a single-user local tool, and the values are the operator's own identity on the
  operator's own disk (Ruling 157). The exposure is the commit boundary, because the repository is public.
- **Controls at that boundary:** brief rule 15 (real-lane proof copies are scrubbed, with the original's sha256 and the
  field names recorded; T5 did this for `docs/proof/workbench-loops-wl-1a/host/real-lane-stop/`) and the gate
  `tools/verify-no-account-identity.py` (T15). The gate refuses email-shaped strings in tracked `docs/proof/**` and
  `.agents/**`.
- **Upgrade trigger (Ruling 157 (4)):** ingest redaction (patterns v2 removing account identity in the mapper) the
  moment the tool goes multi-user or uploads, shares or exports run logs.
