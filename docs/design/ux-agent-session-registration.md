---
id: ux-agent-session-registration
title: "Agent session registration — the environment contract and harness-scripted launch"
type: design
status: draft
owner: "@timianmalloo"
phase: "3"
tags: [ux, terminal, loomkeeper, watcher, agent, environment-contract, session-3]
links:
  - { to: session-contracts, rel: relates-to }
  - { to: design-watcher-coordination-contract, rel: refines }
  - { to: spec-agentic-watcher-substrate, rel: implements }
review-by: 2026-12-01
review-suggested: []
summary: >-
  Spec for making an agent terminal self-registering with Loomkeeper without the agent's cooperation,
  the AI-Forward pack, or any repo directive. Registration already works and is harness-agnostic; the
  gap is that four identity fields are never filled, two more are placeholders, and nothing is passed
  to the child process. Adds a documented environment contract that any harness can read, and
  harness-scripted launch entries (New Claude Code session / New Copilot session) that supply the
  harness identity AI-DE cannot otherwise know.
---

# Agent session registration

**Session 3 · spec, not an implementation.** The files this touches are Core's and Design's under
[`session-contracts.md`](../collaboration/session-contracts.md) §2.

---

## 1. The thing that already works, and the reason it matters here

**AI-DE already auto-registers every terminal with Loomkeeper, and it needs nothing from the agent.**

`WorkbenchShell.WatcherLoopAsync` runs a 2-second loop calling
`emitter.Reconcile(ids, id => IdentityFor(id, terminals))` (`:1794`). `SessionCoordinationEmitter`
writes `register` / `heartbeat` / `session-end` in the `loomkeeper/1` contract to the watcher's
coordination-log directory, which the host pumps back in.

This satisfies all three of the stated constraints **today**, and the spec must not break any of
them:

| Constraint | Why registration already meets it |
|---|---|
| works with **any harness** | AI-DE registers the *terminal*, not the agent. What runs inside is irrelevant to the register event |
| works with **any model** | nothing about registration reads a model |
| needs **no repo directive and no AI-Forward pack** | the agent is never asked to do anything; `CLAUDE.md`, `AGENTS.md` and `.agents/` are not consulted |

**The design rule that follows, and it governs everything below:**

> **Registration is AI-DE's job and must never depend on the agent.** Anything the agent supplies is
> *enrichment* — additive, optional, and absent by default. A harness that ignores every convention
> in this document must still appear in Loomkeeper, correctly, with fewer attributes.

That inverts the obvious design (tell the agent to register itself), and it is the only shape that
survives "no pack, no directives, any harness".

---

## 2. What is actually missing

`SessionCoordinationIdentity` has **ten** fields. `IdentityFor` (`WorkbenchShell.cs:1840`) fills
**six**, two of them with placeholders:

| Field | Today | Problem |
|---|---|---|
| `RepoPath`, `RepoDisplay` | real | ✓ |
| `TerminalId` | real | ✓ |
| `AgentName` | the executable, or the literal `"terminal"` | ✓ adequate |
| `WorktreeBranch` | **the literal string `"workspace"`** | a placeholder rendered as fact — every session reports the same branch |
| `WorktreePath` | **`repoPath`** | the repo root, not the worktree. Two sessions in two worktrees of one repo are indistinguishable |
| `Harness`, `HarnessVersion` | **never set** | AI-DE cannot know it — §4 |
| `Model`, `ModelVersion` | **never set** | AI-DE cannot know it — §4 |

And **no environment is passed to the child**: `TerminalSessionRequest` has no environment field
(`ConPtyTerminalSession.cs:11`), and the session sets none, so the agent inherits AI-DE's environment
unchanged and has no way to know it is inside AI-DE at all.

**`WorktreeBranch: "workspace"` is the sharpest of these**, and it is this repository's own §8.3a
family: a value that is published, rendered, and false. A watcher pane showing branch `workspace`
for every session is worse than showing nothing, because it invites a reader to compare two sessions
by a field that cannot distinguish them.

---

## 3. The environment contract

One documented set of variables, exported into every agent terminal. **Plain environment variables,
no file format, no repo dependency** — readable by any harness, any language, any shell.

| Variable | Value | Purpose |
|---|---|---|
| `AIDE_SESSION` | the terminal's surface id | **the presence signal.** Set ⇒ you are inside an AI-DE agent terminal |
| `AIDE_TERMINAL_ID` | same id | explicit name for the contract's `terminal.id` |
| `AIDE_WORKSPACE` | absolute workspace root | what AI-DE has open, which may differ from `cwd` |
| `AIDE_WORKTREE` | absolute worktree path | distinguishes sibling worktrees of one repo |
| `AIDE_BRANCH` | current branch | so the agent need not shell out for it |
| `AIDE_AGENT` | the executable AI-DE launched | `claude`, `copilot`, … |
| `AIDE_HARNESS` | the harness id from the launch entry (§5), else unset | `claude-code`, `github-copilot` |
| `AIDE_CONTRACT_LOG` | absolute path to the coordination-log **directory** | where enrichment is appended |
| `AIDE_CONTRACT_VERSION` | `loomkeeper/1` | pinned, so a future schema is a different value rather than a silent re-parse |

### 3.1 Rules

1. **`AIDE_SESSION` is the only variable anything should branch on.** Unset ⇒ not an AI-DE terminal
   ⇒ do nothing. An agent launched from an ordinary shell must never register, and this is the check
   that guarantees it.
2. **Every variable is advisory.** Nothing in AI-DE reads them back to decide anything. They exist so
   an agent *can* participate; the session is already registered whether it does or not.
3. **Absent stays absent.** If AI-DE cannot determine a value (no git, detached HEAD, no harness
   chosen) the variable is **not set**, never set to a guess or to a placeholder like `"workspace"`.
   An agent seeing no `AIDE_BRANCH` knows the branch is unknown; an agent seeing `AIDE_BRANCH=workspace`
   would believe a false one.
4. **The child's environment is otherwise untouched.** These are additions; nothing existing is
   overwritten, because the agent's own configuration (`ANTHROPIC_*`, `PATH`, proxy settings) is the
   user's and AI-DE has no business editing it.

### 3.2 Enrichment, for a harness that chooses to

An agent that wants its harness and model on the record appends **one line** to a file in
`$AIDE_CONTRACT_LOG`:

```json
{"kind":"register","contract":"loomkeeper/1","session":"$AIDE_SESSION","at":<epoch>,"seq":<n>,
 "attrs":{"service.name":"claude-code","gen_ai.request.model":"claude-opus-5"}}
```

Deliberately the **same** `session` id AI-DE used, so this is a re-register that merges rather than a
second session. The ingest already counts `DuplicateRegister`, so the behaviour on a repeat is an
existing, tested path rather than a new one — **but which of merge-or-reject it currently does must
be confirmed before this is built** (§7).

**No repo file is needed to learn this.** The instruction can travel in the environment itself:
`AIDE_CONTRACT_README` may point at a short file AI-DE writes into the session's own directory
containing exactly this snippet. A harness that reads one env var can participate with no directive,
no pack, and nothing committed to the repository.

---

## 4. Why the harness and model cannot be inferred, and what to do instead

AI-DE launches an executable. It cannot know:

- **which harness** — `claude` could be a shim, a wrapper, or a different tool with the same name;
- **which model** — chosen inside the session, changeable mid-session, and invisible from outside.

Two honest options, and the spec takes both:

1. **The launch entry declares the harness** (§5). If the user picked *"New Claude Code session"*,
   `Harness` is known by construction. This is the user's own proposal and it is the right one.
2. **The model is only ever the agent's to report.** It stays `null` until enrichment arrives.

**`null` must render as unknown, never as a default.** A watcher row showing a model nobody reported
is the `WorktreeBranch: "workspace"` defect again, and this document exists partly because that one
already shipped.

---

## 5. Harness-scripted launch entries

Today the Terminal menu offers **New agent terminal**, which picks
`TerminalSurface.AvailableAgents.FirstOrDefault()` — the first agent on `PATH`, with no choice and no
declared harness. That is where the missing identity comes from.

**Proposed:** one entry per configured harness.

```
Terminal
  New terminal                      Ctrl+`
  New agent terminal                (unchanged — first available, harness unknown)
  ─────────────────
  New Claude Code session
  New GitHub Copilot session
  … one per harness profile with a launch template
```

### 5.1 Built on the profile mechanism that already exists

`AgentReadinessProfile(Agent, Pattern, Origin, AttentionPattern)` is already the per-harness
extension point, already user-configurable through `agent-readiness.json`, and already fails loudly
on a bad pattern rather than degrading to "assume ready". **Extend it rather than adding a second
list** — a parallel harness registry would be the same defect this repository has hit five times in
one day (a hand-maintained list kept in step by memory).

Add to the profile:

| Field | Purpose |
|---|---|
| `HarnessId` | `claude-code` — becomes `service.name`, and `AIDE_HARNESS` |
| `DisplayName` | `Claude Code` — the menu label, so a new profile adds its own menu entry |
| `LaunchArgs` | optional arguments appended to the executable |

**A profile with a `DisplayName` gets a menu entry; one without does not.** The menu is then
*derived from the profile set*, so adding a harness is one config edit and no code change — and no
second list to forget.

### 5.2 Honouring repo knowledge is free, and worth stating

The launch already sets `WorkingDirectory` to the workspace rather than wherever AI-DE was started
from. Claude Code and Copilot both discover `CLAUDE.md`, `AGENTS.md` and `.github/instructions/`
**from the working directory**, so a scripted launch honours the repo's own directives with no extra
work — and **still works in a repo that has none**, because discovery finding nothing is the normal
case, not an error.

This is the part of the request that needs no design: it already holds, and the spec's job is to not
break it. In particular, **`LaunchArgs` must not inject a system prompt** that would compete with the
repo's own instructions. AI-DE's job is to start the harness in the right directory with the right
environment; what the agent then reads is the repository's business.

---

## 6. What this changes, by owner

| # | Change | File | Owner |
|---|---|---|---|
| 1 | `TerminalSessionRequest` gains `IReadOnlyDictionary<string,string>? Environment` | `AiDe.Core/Terminal/ConPtyTerminalSession.cs` | Core |
| 2 | The ConPTY start applies it to the child | same | Core |
| 3 | `IdentityFor` fills the **real** branch and worktree; unknown ⇒ omitted, never `"workspace"` | `WorkbenchShell.cs:1840` | Core |
| 4 | `IdentityFor` passes `Harness` from the launch entry when there was one | same | Core |
| 5 | `TerminalSurface` populates the environment per §3 at spawn | `TerminalSurface.cs` | Design |
| 6 | `AgentReadinessProfile` gains `HarnessId`, `DisplayName`, `LaunchArgs` | `AgentReadinessProfiles.cs` | Core |
| 7 | Menu entries derived from profiles with a `DisplayName` | `MainMenuBuilder.cs` + catalog | Design + Core |
| 8 | Watcher panes render `null` harness/model as unknown, not as a default | watcher surfaces | Design |

**Order matters for one pair only:** 3 is worth doing alone and immediately. It removes a false value
from a rendered surface, needs none of the rest, and is the only item here that fixes something
currently wrong rather than adding something absent.

---

## 7. Open questions — to be answered by observation, not assumption

1. **Does a second `register` for a known session merge its attributes or get rejected as
   `DuplicateRegister`?** §3.2 depends on the answer. The counter exists; the behaviour must be
   *run*, not read — this repository spent a day establishing that reading the code answers a
   different question than running it.
2. **Should `New agent terminal` remain** once per-harness entries exist? It is the entry with no
   declared harness, so it is the one that produces the incomplete identity. Keeping it is
   defensible (it works when no profile matches); the argument for removing it is that a menu should
   not offer the degraded path beside the good one.
3. **Where does `AIDE_CONTRACT_README` live?** It must be outside the repository, or a repo with no
   pack acquires an untracked file it did not ask for.

## 8. What this spec does not do

It does not make an agent *use* Loomkeeper — post to the board, raise a dispute, read other sessions.
It makes the agent **visible** and gives it the address of the channel. Participation beyond
registration is a separate design, and it is the one that genuinely needs the agent's cooperation and
therefore genuinely needs directives.
