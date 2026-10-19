---
id: proof-wl-prefill-assessment-handoff
title: "Hand-off — T29 wl-prefill-assessment node 1 (AC-W01a assessment, docs only)"
type: proof-pack
status: complete
owner: "@timianmalloo"
tags: [workbench-loops, proof, handoff, ac-w01a, compile]
links:
  - { to: architecture, rel: implements }
  - { to: note-wl-dr-prefill-goal-done, rel: relates-to }
review-by: "2026-11-19"
summary: >-
  T29 node 1 is done. It wrote the decision note docs/notes/wl-dr-prefill-goal-done.md, which answers the brief's five
  items with file:line citations. No code, no tests and no App runs. Next: the Leader raises the decision request to the
  Owner, and nothing is built until the Owner rules.
---

# Hand-off — T29 `wl-prefill-assessment`, node 1

**Done.** The decision note `docs/notes/wl-dr-prefill-goal-done.md` answers the five items. Every code claim cites the
file and line on `origin/main` `624b46c4` that this node opened. Claims not opened carry an `assume:` line.

**The finding in one line.** The model prefill exists (Addendum D's compile call, the *? derived* and Keep marks, and the
ADR-0036 eval gates). It is off, because the default is `mechanical-only` (Ruling 68) and main has no control that
changes it. The structure lines start from `StructureDeriver.Fake`, which returns `""`.

**Red.** Nothing. This node is docs only, so rule 7's red-first and suite clauses do not apply.

**Not done (out of scope).** No build. No spec change. T28's compiled-field defect is untouched.

**Brief versus code (rule 14).** Brief item 4 names ADR-0042 for the eval. ADR-0042 defines the route fingerprint. The
eval gate is ADR-0036. The note reads both, and finds that no prompt text is in the fingerprint.

**Findings for the next node (reported only, no defect class appended, because no fix landed).**
1. A kept derived line is not written to the draft, so it does not survive a re-prepare or a restart
   (`ComposerSurface.cs:1543-1546`, `PreCompile.cs:116-125`).
2. The session settings' compile-mode help text is the same under every mode (`SessionDocumentSurface.cs:1413`).
3. Ruling 178's currentness delegation must read `projection_sha` from stored rows (Inferred risk, bound to T24).

**Next step.** The Leader raises the note's section 5 to Owner `wl-owner-20261018`. Once the Owner rules, a build node
takes option A's conditions C1–C4 (or the ruled option) red-first.
