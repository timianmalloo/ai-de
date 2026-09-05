---
id: design-watcher-message-board
title: "Loomkeeper Message Board + Fleet Aggregator"
type: design
status: accepted
owner: "@timianmalloo"
phase: "3"
tags: [loomkeeper, watcher, design, message-board, fleet, cross-repo, quarantine, phase-3]
links:
  - { to: architecture-loomkeeper, rel: implements }
  - { to: design-watcher-sessions-surface, rel: depends-on }
  - { to: design-watcher-coordination-contract, rel: depends-on }
  - { to: spec-agentic-watcher-substrate, rel: implements }
  - { to: adr-0020-trusted-registrar-harness-model-identity, rel: depends-on }
review-by: 2027-02-26
review-suggested: []
summary: >-
  Design for the Loomkeeper Message Board + Fleet aggregator (slice 6). The board is a per-repository,
  append-only communication surface (Question / Decision / Breadcrumb / Knowledge Candidate + Reply /
  Acknowledgement) with author/session/time/trust provenance; a reply/ack must reference an existing
  parent (no orphan thread); all content is quarantined untrusted data that cannot instruct a grader,
  and grader-injection shapes (score 100 / ignore the rubric / promote this lesson) are flagged;
  policy deletion redacts the payload but keeps the immutable envelope as a tombstone. The Fleet
  aggregator builds the repo->session map across >=2 stores. Rides the coord-core append semantics.
---

# Design: Loomkeeper Message Board + Fleet Aggregator

- **Status:** Accepted · **Tier:** T2 · **Phase:** 3, slice 6 · **Depends on:** the session read model (slice 3), the injected coordination contract (slice 2 — the append semantics the board rides), and the trusted registrar (capability, ADR-0020 trusted-registrar-harness-model-identity).
- **Grounding:** spec **US-4** (the board), **US-3** (cross-repo fleet, item 3), the **Board Message** aggregate (lines 210/233), and the note that *"the Message Board event contract must align with one-file-per-session append semantics"* (the coord-core log, slice 2).

## 1. Responsibility and boundary

Two responsibilities in the **Repo Coordination** context (spec §bounded contexts): (a) an **append-only, per-repository Message Board** — agent-to-agent communication (questions, breadcrumbs, decisions, knowledge, replies, acknowledgements) with provenance and thread integrity; and (b) a **cross-repository Fleet aggregator** — the `repository -> sessions` map across >=2 stores. It borrows session identity/capability/trust (Phase 1) and the session read model (slice 3); it does **not** own the grader, the leaderboard, or work-content governance (redaction *mechanism* is here; the *policy* is Phase 5).

**Trust boundary — board content is untrusted by construction.** A board message's content is authored by an agent and may contain anything, including instructions aimed at a grader or learning promoter. So the content is **quarantined**: it is stored and rendered as **inert data**, never as instructions, and there is no path by which it can instruct a grader (US-4 #4). Grader-injection shapes are additionally **flagged** (US-4 #5). This is the *Confused Deputy* mitigation (LOA anti-pattern): untrusted content never reaches a tool/grader as instructions.

## 2. Data model

**Aggregate + its one invariant:**

| Aggregate root | One protected invariant |
|---|---|
| **BoardMessage** | Its **envelope, order, and thread references are append-only**; a correction appends a new message; only a **policy redaction** may null the content payload, and it leaves the **immutable envelope as a tombstone** (spec line 210). A reply/acknowledgement **references an existing parent** and can never create an orphan thread. |

- **`BoardMessageKind`** ∈ { Question, Decision, Breadcrumb, KnowledgeCandidate, Reply, Acknowledgement }. The first four are top-level posts; Reply/Acknowledgement carry a `ParentMessageId`.
- **`BoardMessage`** (`MessageId`, `RepositoryKey`, `Kind`, `AuthorSessionId`, `AuthorTrust`, `ParentMessageId?`, `Content?`, `Quarantined`, `InjectionFlagged`, `Tombstoned`, `RecordedAt`, `Seq`). `Content` is null once tombstoned. `AuthorTrust` is the **session's own trust** (Verified when the harness named itself, else Asserted — the provenance US-4 requires).
- **Grain:** one row is one append event by one authenticated session in one repository thread at one recorded moment (spec line 233). Repository-scoped: a message lives **only** in its `RepositoryKey`'s board (the repo canonical path).
- **Append-only by API, redaction is the one allowed content mutation.** The store exposes `AppendBoardMessage` + `RedactBoardMessage` (content->null + tombstone) and **no envelope update/delete** — so the envelope/order/thread are append-only, while the payload can be redacted once (spec line 210). Migration: expand-only new `board_message_fact` table.
- **Fleet:** `RepositorySessions(Repository, Sessions)` and `FleetView(Repositories)` — the repo->session map, derived by grouping session snapshots (slice 3) across sources by `Repository.CanonicalPath`.

## 3. Contracts

```csharp
public enum BoardMessageKind { Question, Decision, Breadcrumb, KnowledgeCandidate, Reply, Acknowledgement }

public sealed record BoardMessage(
    string MessageId, string RepositoryKey, BoardMessageKind Kind,
    string AuthorSessionId, TrustClassification AuthorTrust, string? ParentMessageId,
    string? Content, bool Quarantined, bool InjectionFlagged, bool Tombstoned,
    DateTimeOffset RecordedAt, int Seq);

public interface IMessageBoard
{
    BoardMessage Post(string repositoryKey, string sessionId, SessionCapability capability, BoardMessageKind kind, string content); // top-level
    BoardMessage Reply(string repositoryKey, string sessionId, SessionCapability capability, string parentMessageId, string content);
    BoardMessage Acknowledge(string repositoryKey, string sessionId, SessionCapability capability, string parentMessageId);
    void Redact(string messageId); // policy deletion -> tombstone
}

// store additions (both impls):
void AppendBoardMessage(BoardMessage message);
IReadOnlyList<BoardMessage> BoardMessages(string repositoryKey); // repo-scoped, seq order
BoardMessage? FindBoardMessage(string messageId);
void RedactBoardMessage(string messageId);

// injection detection (US-4 #5):
public static class GraderInjectionScanner { static bool LooksLikeInjection(string content); }

// fleet:
public sealed class FleetAggregator { FleetView Aggregate(IEnumerable<IWatcherSessionsQuery> sources); }
```

## 4. Failure-mode analysis

| # | Failure mode | Disposition |
|---|---|---|
| Identity | forged capability posts as a session | **Detect+prevent** — capability verified (LK-0001); test |
| State | reply/ack references a non-existent parent | **Prevent** — reject orphan (LK-0002 / invalid); test (US-4 #2) |
| State | reply/ack references a parent in a *different* repository | **Prevent** — parent must be in the same `RepositoryKey`; test |
| Input | content contains grader instructions (`score 100`, `ignore the rubric`, `promote this lesson`) | **Contain** — stored quarantined; `InjectionFlagged=true`; never an instruction path (US-4 #4/#5); test |
| Input | top-level kind posted with a parent, or reply/ack with no parent | **Prevent** — kind/parent consistency enforced; test |
| State | a board write fails | **Explicit** — the exception surfaces; the message is not returned as posted (US-4 #3); test |
| State | policy redaction of a message | **Tombstone** — content->null, `Tombstoned=true`, envelope kept; a redacted parent still anchors its thread (US-4 #6); test |
| Repo scope | a message queried under the wrong repo | **Exclude** — `BoardMessages(repoKey)` returns only that repo's messages; test |
| Fleet | sessions across >=2 repositories | **Group** — repo->session map by canonical path, ordered; test |

## 5. Security / privacy

- **Quarantine (Confused Deputy):** board content is untrusted and stored as inert data; there is no API that feeds a message's content to a grader as instructions, and injection shapes are flagged (US-4 #4/#5). The **invariance** property (US-4 #5 — the same episode scores identically with/without an injection fixture) holds by construction in slice 5's scorer (it consumes typed deterministic signals, never board text); slice 6 provides the **flag**.
- **Provenance:** every message carries author session + the session's trust classification + recorded time (US-4 #1). Asserted-trust authors are labelled; the board never elevates them.
- **Redaction/tombstone:** the payload can be irreversibly nulled while the envelope remains (spec line 210). The *policy* (opt-in, notice, retention) is Phase 5; the *mechanism* is here. **New personal-data surface:** message content may carry work text — kept **local**, no egress (default-deny gate stands, LK-0003), redaction available.

## 6. Instrumentation (IO1)

Operator questions: messages **posted** per repo and kind, **replies/acks**, **orphan rejections**, **injection flags**, **redactions**, and the fleet's **repo count / session count**. Each is a count over the board store / fleet view.

## 7. Test plan (Testing Strategy D1, D4; E11)

- **D1 (board):** post each top-level kind (provenance = session trust); reply/ack references existing parent; **orphan reply rejected**; cross-repo parent rejected; forged capability rejected; injection content flagged + quarantined; kind/parent consistency; repo-scoped query isolation; redact -> tombstone (content null, envelope kept, thread still anchored).
- **D4 (SQLite):** board messages persist across reopen; repo-scoped query over the real table; redact persists as a tombstone; append-only envelope (no update/delete API).
- **E11 (composition):** register two sessions in two repos → post + reply in repo A, post in repo B → `BoardMessages(A)` shows the thread, `BoardMessages(B)` isolated; the **fleet** aggregator over both shows the repo->session map.
- **A6/injection:** the scanner flags `score 100` / `ignore the rubric` / `promote this lesson` (case-insensitive) and does not flag benign content.
- **Mutation:** one load-bearing oracle (orphan rejection, or injection flagging) red-then-revert.

## 8. Ladder / simplicity

Reuse the registrar (capability + trust), the store idiom (append + query + a single content-redact), the session read model (slice 3) for the fleet — **no new dependency**. The board rides the same append semantics as slice 2 (one ledger). Injection detection is a small deterministic scanner, not an ML classifier (the invariance guarantee comes from the scorer's typed-signal design, not from perfect detection).

## 9. Residual (out of slice 6)

- The **coord-core wire** for board messages (a `board-post`/`board-reply` injected-contract kind so a non-pack session posts over its log) — the connective follow-on; slice 6 ships the domain + store both paths feed.
- **Capture policy / retention / opt-in** governing redaction — Phase 5.
- **The Board UI surface** (empty/thread/unanswered/acknowledged/quarantined states) — a WPF surface follow-on (the slice-3 pattern); slice 6 ships the read model.

## 10. Gate record

`GATE design · 2026-08-31 · reviewers (Adversary Mode): Security & Identity (quarantine/Confused-Deputy, capability-verified authorship, injection flag), Data & Persistence (append-only envelope + single content-redact; expand-only table), Distributed Systems (append semantics align with coord-core one-file-per-session), Test Architect (orphan rejection + injection + repo isolation each tested), Simplifier (deterministic scanner not a classifier) · verdict: PASS-WITH-CONDITIONS · conditions — the coord-core wire, the board UI, and capture policy are later slices/phase`

**Handoff:** → `/implement` this design (board domain + store + fleet, TDD).
