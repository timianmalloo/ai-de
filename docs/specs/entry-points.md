---
id: spec-entry-points
title: "Understanding views — D-1 Entry-points listing (spec)"
type: spec
status: draft
owner: "@timianmalloo"
phase: "understanding-views-d1"
tags: [understanding-views, D-1, entry-points, architecture, addendum-c]
links:
  - { to: spec-addendum-c-perspectives, rel: refines }
  - { to: spec-understanding-views, rel: relates-to }
  - { to: note-understanding-views-owner-d1-admission, rel: depends-on }
  - { to: note-d1-codex-entry-point-handshake-r3, rel: depends-on }
  - { to: note-d1-r3-producer-ack, rel: depends-on }
  - { to: note-d1-n1-inventory, rel: relates-to }
  - { to: adr-0030-perspective-registry-and-allow-lists, rel: depends-on }
  - { to: adr-0018-node-content-reader-contract, rel: depends-on }
  - { to: conceptual-model-ai-native-ide, rel: relates-to }
review-by: 2027-03-16
review-suggested:
  - { by: note-understanding-views-n4-entry-points, on: 2026-09-17, reason: "N4 BLOCK; grain/F-EP/candidate-set repair in this file; authors do not self-clear" }
summary: >-
  Admits D-1 Entry-points as an Architecture listing of API, UX, and CLI entry points
  plus unclassified. Select scopes the existing Architecture graph. Open Sequence is
  out: mapper UNASSIGNED (handshake r3 frozen). Status draft; N4 not self-cleared.
---

# Spec: D-1 Entry-points listing

- **Status:** Draft (authors do not self-clear N4)
- **Tier:** T2
- **Author / date:** grok-understanding-views-conductor / 2026-09-16
- **Related:** §A5 D-1; Owner admission `note-understanding-views-owner-d1-admission`; handshake r3 blob `e448383a` frozen; N1 inventory

## Live Open Sequence — who must resolve `mapping-unavailable`

This is **not** a D-1 listing blocker. Frozen r3:

| | |
|---|---|
| What is blocked | A **live** “Open Sequence” from a D-1 row into Codex E1 |
| Why | Mapper status **UNASSIGNED / unavailable**. `node_id` and classification are **never** E1 input by themselves |
| Who does **not** resolve it alone | Grok (must keep Open Sequence disabled). Codex E1 (must not consume D-1 rows). The watcher (no hold, but not the producer) |
| Who must resolve it | **Owner/user admits a separate mapping contract.** Then Grok and Codex five-gates freeze that contract (same handshake track: notice → ACK → freeze → implement). Core-authorized E1 service remains the minting authority for method observations |
| Until then | D-1 listing proceeds. E1 independently selects Core method observations under its own contract |

---

## Part A — Functional specification

### Problem

An indexed workspace contains API, UX, and CLI **entry points**, but Architecture has no listing that shows them with kind, and silent omission of unclassifiable ones. The operator cannot start from “what can the outside world invoke?” and scope the graph. This is independent of drawing a sequence diagram.

### Target users & personas

Primary: **operator / reviewer** who already uses Architecture (D-0 tree, graph, source). Job: see every indexed entry point, know its kind, jump to neighbourhood or source, and never miss an unclassified one.

### Core scenario

Workspace is indexed. Operator opens Entry-points. Every API, UX, and CLI entry point the index can name is a row with kind. One unclassified bucket lists the rest that were found but not classified. Selecting a classified row scopes the Architecture graph to that node’s neighbourhood. View source opens `NodeContentAsync` for that `node_id` when one exists. Open Sequence is **disabled** (`mapping-unavailable`).

### Conceptual domain model (before UX)

**Bounded context:** Architecture understanding views (same as D-0). Not Atlas E1 observations.

**Ubiquitous language**

| Term | Meaning |
|---|---|
| Entry point | A place the outside world can invoke: API surface, UX surface, or CLI |
| Kind | `api` \| `ux` \| `cli` \| `unclassified` — **not** type-kind (`class`/`interface`) |
| Listing row | One occurrence in the current index snapshot |
| Unclassified | Found in the index as a candidate, kind not assigned; **never silent-drop** |
| Graph id | `node_dim.node_id` of a **type** (or the declaring type for a member row). Not a minted member node. |
| Row identity | `(occurrence-kind, display)` — **not** `NodeId`. Two members of one type share a graph id and remain two rows. |
| Mapping | r6 proposed; Sequence stays `mapping-unavailable` until r6 ACK **and** Core observation API |

**Entities vs values:** Listing is a **query result** (value), not a stored census. Kind is a value. Unclassified reason is a disclosure (same family as D-0 not-recorded).

**Aggregate:** **Entry-point listing** for one workspace snapshot. Root: the listing. **Invariant:** every candidate the index produced is either `api`/`ux`/`cli` or present under unclassified; the query must not drop a candidate. Caps, if hit, disclose omitted counts (known/unknown denominator) rather than a complete-looking short list.

**Grain (closed):** one row is exactly one **candidate occurrence** in the current snapshot.

**Candidate set (named):** latest-generation `has_type` facts on non-knowledge subjects **union** latest-generation `has_member` facts. Order: types by `node_id`, then members by `(type, member)`. Cap takes that prefix; `Omitted (N)` is disclosed (not a complete-looking short list). Extractor `members_truncated` (cap 40 per type) is a **separate** disclosure when present — listing those 40 is not pretending the type's member set is complete.

**Identity:**
- **Type row:** `NodeId` = that type's `node_dim.node_id` (`ToDisplayString()`). Display = that id. Graph/source use that id.
- **Member row:** `Display` = `{typeId}.{extractor has_member object}` (e.g. `App.Program.+ Main()`). `NodeId` = **declaring type** `node_id` so graph/source open the **type neighbourhood**, not a minted member node. Row identity is **not** `NodeId`.

**Not:** D-0 census grain. **Not:** `has_member` object as a Core method-observation id (r5).

### In scope / Out of scope

**In:** Listing query; classification `api`/`ux`/`cli`/`unclassified`; select → existing `DescribeAsync`/`GraphAsync`; source via `NodeContentAsync` when `node_id` exists; Architecture kind + derived menu **only after** UV-0 is red-green (AR3); disclosures for not-recorded / cap.

**Out:** Live Open Sequence; D-1 as E1 input; mapper; E2; D-2 data-flow; D-0 census grain; App disk walk (DC-022); Atlas `Understanding/` namespace; thinning §A5; scaffolding the kind before the query.

### User stories

**US-L1 — Open listing.** As an operator, I want every indexed API, UX, and CLI entry point listed with kind so I can start from invocable surfaces.
- **Given** fixture F-EP (five rows: `Orders.OrdersController` has_type class → api, NodeId=self; `Shell.MainWindow` has_type class → ux, NodeId=self; `App.Program` has_type class → cli, NodeId=self; `App.Program` has_member `+ Main()` → cli, NodeId=`App.Program`, Display contains `+ Main()`; `Domain.Order` has_type class → unclassified with reason) **When** the listing query runs **Then** those five Displays/Kinds/NodeIds match
- **Given** an occurrence the classifier cannot assign **When** the listing query runs **Then** it appears under `unclassified` with a reason, never omitted (unless cap-omitted and disclosed)

**US-L2 — Select scopes graph.** As an operator, I want selecting a classified row to scope the Architecture graph to that neighbourhood.
- **Given** a type row **When** I select it **Then** `DescribeAsync`/`GraphAsync` uses that type `node_id`
- **Given** a member row **When** I select it **Then** graph uses the **declaring type** `node_id` (type neighbourhood, not a member node)

**US-L3 — View source.** As an operator, I want source for a graph node.
- **Given** a type or member row **When** View source **Then** `NodeContentAsync` of the **type** `node_id` (ADR-0018), never E1 occurrence Source

**US-L0 — Empty / error / no-workspace.**
- **Given** an indexed workspace with no `has_type`/`has_member` **When** the query runs **Then** rows are empty and omit count is 0 (not a fake complete list)
- **Given** the query throws **When** the surface shows **Then** error copy + Retry (US-T8 family)
- **Given** no workspace **When** the surface binds **Then** "Open a workspace to see entry points."

**US-L4 — Open Sequence stays dark.** As an operator, I must not be offered a live Sequence jump that guesses a method.
- **Given** any listing row **When** the surface renders **Then** Open Sequence is disabled with reason `mapping-unavailable`
- **Given** mapper still UNASSIGNED **When** any test or UI tries to pass classification into E1 **Then** that path does not exist

**US-L5 — Caps disclose.** As an operator, I want truncation to look like truncation.
- **Given** candidates exceed the listing cap **When** the query returns **Then** omitted count / denominator class is disclosed (D-0 skip-count family)
- **Given** a truncated listing **When** E1 has a valid partial receipt of its own **Then** that receipt remains valid (r3 §4) — D-1 must not claim E1 completeness
- **Given** a `members_truncated` fact **When** the listing query runs **Then** chrome discloses that some types list at most 40 members

**US-T13 — Explore unchanged.** Switching to Explore leaves ADR-0017 graph+reader. Entry-points is Architecture-only.

### Non-functional (ISO 25010)

| Attribute | Requirement |
|---|---|
| Performance efficiency | Query-time listing; p95 budget named at design-slice (Inferred until measured) |
| Reliability | Cancel mid-walk; superseded result must not overwrite (D-0 generation/CTS class) |
| Security | App does not walk disk (DC-022); daemon-side query |
| Compatibility | Reuse `node_id` domain; do not fork a second graph store (Ruling 53) |
| Usability | Kind word in UIA Name; unclassified visible |
| Accessibility | WCAG 2.2 AA on the Architecture pane (same as D-0) |
| Maintainability | Derived menu (ADR-0030); no hand-written Perspectives list |
| Portability | N/A (Windows workbench) |

### Comparables [Inferred unless noted]

| Comparable | Behaviour | Source |
|---|---|---|
| VS / Rider “Go to Symbol” | Lists types/members; does not classify API vs UX vs CLI | Industry IDE [Inferred] |
| ASP.NET endpoint explorer / Swagger | Lists HTTP routes as API entry points | ASP.NET [Inferred] |
| `System.CommandLine` / `Main` | CLI entry is a method, not a type | .NET [Inferred] |
| WPF `ICommand` / routed commands | UX entry often a member | WPF [Inferred] |
| D-0 Solution tree [Verified] | Census path×kind; unindexed never silent; select/source | This repo |

Conflict to surface: §A5 names API/UX/CLI **surfaces** (often members); N1 shows members are not `node_id`s today. Spec does not pick the extractor algorithm; architecture must.

---

## Part B — UX specification

Personas: operator as in D-0 (Architecture pane).

**IA:** Architecture → derived Show **Entry-points** (ADR-0030). Groups: API, UX, CLI, Unclassified. Not a second graph.

**Findability:** ≤ 2 steps from Architecture (View menu / Show), same UX-1 family as D-0.

**Flows**

```mermaid
flowchart TD
  open[Open Entry-points] --> list{Listing}
  list -->|ok| groups[API / UX / CLI / Unclassified]
  list -->|error| err[Could not read entry points + Retry]
  list -->|empty index| empty[Specified empty]
  groups --> sel[Select classified row]
  sel -->|has node_id| graph[Scope Architecture graph]
  sel -->|no node_id| nog[Reason: no graph id]
  groups --> src[View source]
  src -->|has node_id| reader[NodeContentAsync]
  src -->|no node_id| nos[Unavailable]
  groups --> seq[Open Sequence]
  seq --> dark[Disabled mapping-unavailable]
```

Recovery: Retry re-runs listing query; Back is Architecture navigation (not E1 Restore).

**Wireframe structure:** pane with grouped list + chrome (skip/cap/unclassified counts) + disabled Sequence affordance with visible reason. Empty / loading / error / no-workspace / stale-while-refresh (D-0 state set).

---

## Part C — UI specification

**Archetype (auto-selected):** **B-series operational indexed list / navigator** (same family as D-0 Solution tree). JTBD is record-management over an index, not a temporal wizard (A) or canvas (C). Reading is parallel; activating a row is serial.

**Signature:** Architecture One Derived("_View"); `Zone` null (View-menu-only unless Owner later freezes a default). Tokens: `DESIGN.md`. States: empty, loading, error, no-workspace, stale, unclassified group, cap disclosure. Contrast AA. Sequence control: present but **disabled**, reason in accessible name `mapping-unavailable`.

N8-level glyphs: architecture/design-slice (kind word required even if glyph deferred).

---

## Gate record

- **Simplifier:** Open Sequence and mapper out of this spec — recorded, not gold-plated into UV-0.
- **Test Architect:** N4 re-review PASS-WITH-CONDITIONS (`note-understanding-views-n4-entry-points-rereview`). Grain closed (declaring-type borrow, not mint). **Authors do not self-clear** spec `status`.
- **Data:** listing aggregate + invariant stated; durable representation is architecture.
- **UX/IA:** flows cover disabled Sequence and missing `node_id`. N4 required.
- **Security:** DC-022 named.

**Residual risk:** name-heuristic classifier is not extractor-precise (Flagged). Caps Inferred. N4 BLOCK repair in this file; **authors do not self-clear** — different Test Architect re-reviews. Grain is closed as declaring-type `node_id` for members.

**Handoff:** N5 implements **declaring-type borrow**, not member-node mint. UV-1 kind already on the branch. Open Sequence stays dark until r6 ACK **and** Core observation API.
