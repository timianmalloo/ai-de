# Brief — T29 `wl-prefill-assessment` (AC-W01a) · Claude Code · Opus · node 1 (assessment, docs only)

**Goal (Owner, 2026-10-19 ~15:50Z):** the user said: *"I should not be the one filling in goal and done criteria — that
should come from the LLM analysis of my prompt."* The spec already requires the prefill. AC-W01a
(`docs/specs/workbench-loops-ux.md:146`): goal, completion conditions, exclusions and scope are "derived/prefilled where
possible, editable inline and visible before submission". The Owner treats the user's reading, that the prefill comes from
an LLM analysis of the prompt, as the user's requirement. **Assess it and draft a decision request. Build nothing.**
**Done when:** a committed decision note `docs/notes/wl-dr-prefill-goal-done.md` (V2 frontmatter, `type: decision-note`,
an `implements` link to `spec-workbench-loops-ux` or `architecture`). It answers each item below with file:line that you
opened:
1. **What fills those fields today.** The goal, done criteria, exclusions and scope fields in the composer or notebook:
   where they are declared, who writes them, and whether anything prefills them now (file:line for each).
2. **Which call should derive them.** The existing compile call (`src/AiDe.App/Conductor/CompileCallHost.cs`, and how
   `ComposerSendGate.cs` uses it), or a new model call. Cost, latency, and where its output would land in the store.
   Which spec story and which `RunEvent` / record carries it.
3. **Which slice owns AC-W01a** in `docs/architecture.md` §WL.11's table, quoted. Whether doing it now moves work between
   slices.
4. **The determinism and eval question.** Use ADR-0042 and the AI-systems lens
   (`.claude/agents/ai-systems-engineer.md`; run it inline). A model-derived field needs an eval harness, a visible
   "suggested" state until the user accepts or edits it, and no non-determinism leaking into a deterministic path, such
   as the request fingerprint or `projection_sha`. Say what the eval is, what it measures, and the threshold.
5. **The decision request draft:** the question in one sentence, 2–3 options, a recommendation, reversibility, blast
   radius, and a fallback. The Leader raises it to the Owner. The Owner rules before anything is built.

**Tier** T2 · **fan-out** 0 (personas inline) · **budget** 80 tool calls · 160k · **kick** at 12 min by mail.

## Owned authored paths
New `docs/notes/wl-dr-prefill-goal-done.md`; `{PROOF}/handoff.md` only. **No code, no tests, no App runs.**

## Not in scope
Building the prefill. The compiled-field defect (T28). Changing the spec. If the spec and the user's reading conflict,
state the conflict in the note as a decision.
