---
id: inv-0003-graph-exceeds-ipc-frame-cap
title: "INV-0003 — Graph load fails with ipc.transport_closed on a large repo"
type: doc
status: accepted
owner: "@timianmalloo"
tags: [investigation, graph, ipc, daemon, transport, rca, core]
links:
  - { to: architecture, rel: relates-to }
  - { to: session-contracts, rel: relates-to }
review-by: 2026-12-13
summary: >-
  Opening TheTerrace as a workspace shows "The graph could not be loaded: ipc.transport_closed: the
  daemon closed the connection without responding." Verified cause: the whole-graph response for a
  real repo (~2,813 nodes / 8,602 edges) exceeds the IPC frame cap (IpcFraming.MaxFrameBytes = 1 MiB),
  so IpcFraming.WriteAsync throws ArgumentException in the daemon's response path; the serve loop
  catches IOException and OperationCanceledException but NOT that ArgumentException, so it propagates,
  the connection closes without a response, and the client reports transport_closed. This is the
  IpcFraming `simplify:` marker's upgrade trigger firing. Core-owned (IPC / daemon / graph
  projection). Report only — handed off to the Core session.
---

# INV-0003 — Graph load fails with ipc.transport_closed on a large repo

## Symptom, as reported

> "when I open theterrace repo as a workstation I see this error in the graph view: The graph could
> not be loaded: ipc.transport_closed: the daemon closed the connection without responding. The other
> session has been working on improving the graph fidelity, not sure if that is related."

It is related.

## Verified root cause

**The whole-graph response for a real repository exceeds the 1 MiB IPC frame cap, and the daemon's
serve loop does not catch the exception that overflow throws — so it closes the connection instead of
answering.**

The chain, read from the code:

1. **The client asks for the whole graph.** `CanvasGraphViewModel.WholeGraphAsync`
   (`src/AiDe.Core/Presentation/CanvasGraphViewModel.cs:74`) calls
   `queries.GraphAsync(new GraphQuery(WholeGraphNodeCap), …)`, where
   `WholeGraphNodeCap = GraphProjection.DefaultMaxNodes = 5_000` (`GraphProjection.cs`).
2. **The graph is large but within the node cap.** TheTerrace indexes to **~2,813 nodes and ~8,602
   edges** (Core's recent graph-fidelity work — GraphProjection now returns *every* node and edge,
   attributes folded onto nodes). 2,813 < 5,000, so the node cap does **not** trim it; the full graph
   is serialized.
3. **The serialized response exceeds the frame cap.** `IpcFraming.MaxFrameBytes = 1024 * 1024`
   (1 MiB), carrying a `simplify:` marker: *"one flat cap rather than per-operation limits; ceiling
   1 MiB; upgrade trigger = an operation [whose payload exceeds it]."* A graph of ~2,813 nodes +
   ~8,602 edges serialises to **roughly 1.5 MiB** (order-of-magnitude: ~250 B/node + ~100 B/edge ≈
   0.7 MiB + 0.86 MiB), **over** the 1 MiB ceiling.
4. **The write throws, in the daemon.** `IpcFraming.WriteAsync` does
   `if (body.Length > MaxFrameBytes) throw new ArgumentException(...)` (`IpcFraming.cs`). This runs
   inside the daemon's `Respond` (`IpcServer.cs:332-333`).
5. **The serve loop does not catch it.** `IpcServer.ServeAsync` wraps the response write in
   `try { await RespondWithinTimeout(...) } catch (IOException) { return; } catch
   (OperationCanceledException) … { return; }` (`IpcServer.cs:285-300`). It catches **IOException** and
   **OperationCanceledException** — **not** `ArgumentException`. So the ArgumentException propagates
   out of `ServeAsync`, the connection handler unwinds, and the pipe is closed **without a response
   written**.
6. **The client sees a silent close.** `IpcClient.ExchangeAsync` reads the frame; `IpcFraming.ReadAsync`
   returns `null` (EOF), and the client maps that to
   `IpcResponse.Error(TransportClosed, "the daemon closed the connection without responding")`
   (`IpcClient.cs:126-134`). `CanvasGraphViewModel` catches it and renders
   `"The graph could not be loaded: {ex.Message}"` (`CanvasGraphViewModel.cs:192`).

**Necessary.** A response ≤ 1 MiB is written and returned normally — small repos (this repo's own
graph) load fine, which is why it only appears on a large one like TheTerrace.

**Sufficient.** Any whole-graph response over 1 MiB throws the uncaught ArgumentException and closes
the connection. TheTerrace's node/edge counts put it over the line.

> **Confirmation step for Core (definitive):** serialize the `GraphAsync(new GraphQuery(5000))`
> response for TheTerrace and measure its byte length — the root cause predicts **> 1,048,576 bytes**.
> Reducing the node cap until the response fits will make the graph load, which is the necessary-and-
> sufficient check.

## Two defects, both Core-owned

| # | Defect | Location |
|---|---|---|
| A | **The whole-graph response can exceed the 1 MiB IPC frame cap.** The `simplify:` marker's upgrade trigger has fired: the graph is now big enough (real repo, 5,000-node cap) to overflow a single frame. | `IpcFraming.MaxFrameBytes`; `GraphProjection.DefaultMaxNodes`; the whole-graph query in `CanvasGraphViewModel` |
| B | **The daemon crashes the connection on an oversized write instead of returning a clean error.** The serve loop catches `IOException`/`OperationCanceledException` but not the `ArgumentException` that overflow throws, and there is **no `PayloadTooLarge` error code** to return. So an overflow reads to the user as an opaque "transport closed", not "the graph is too large to send in one message." | `IpcServer.ServeAsync` (`:285-300`); `IpcContract.IpcErrorCodes` |

## Ownership

Both defects are in **Core**'s domain (`src/AiDe.Core/Ipc/**`, `src/AiDe.Daemon/**`,
`GraphProjection`), and the trigger is the **Core session's** graph-fidelity change (the graph grew
from a 2-node slice to the full ~2,813-node graph). The Design side already degrades gracefully — it
catches the error and renders it rather than crashing — so there is **no Design-side fix**; the graph
*view* cannot do better until the daemon returns a distinguishable error or a response that fits.

## Recommended fix (for the Core session)

Layered, cheapest first:

1. **Make the failure legible (robustness, do regardless).** Catch the oversized-frame case in
   `IpcServer.ServeAsync` (or check `body.Length` before writing) and return a new
   `IpcErrorCodes.PayloadTooLarge` response instead of letting the ArgumentException close the
   connection. The user then sees "the graph is too large to send" and the Design view can show a
   real message.
2. **Make the whole-graph fit the transport.** Options (Core's call):
   - **Chunk/stream** the graph response across frames (the marker's "per-operation limits"
     upgrade) — the correct long-term fix, keeps full fidelity.
   - **Raise `MaxFrameBytes`** to a bounded larger ceiling (e.g. 8–16 MiB) — the marker's literal
     "upgrade trigger", simplest, but still a hard wall.
   - **Trim the whole-graph payload** — a smaller default node cap for the *whole-graph* view, a
     lighter node wire shape, or degree-ranked truncation that is *reported* (the projection already
     counts what it drops), so the graph loads bounded and says what it omitted.

## Stop

This is a report. Per `/investigate` discipline it ends here for human/Core review; no code was
changed. Handed off to the Core session (the owning domain and the change that triggered it).

## Deeper finding — the 1 MiB cap is a symptom; the load-everything model is the disease

Raising `MaxFrameBytes` to 8 or 16 MiB would make TheTerrace load and **move the wall to the next
project**. TheTerrace (~2,813 nodes) is *small*; real targets reach 10⁴–10⁶ nodes. A whole-graph
transfer does not scale, and — decisively — **it was never what the surface is supposed to do.**

**The transport failure exposed a spec violation.** `docs/specs/knowledge-exploration.md` already
requires the opposite of a whole-graph load:

- **US-K2 — Bounded neighbourhood (no hairball):** *"only its bounded N-hop neighbourhood is shown
  (default N≤2) … the whole graph is never rendered at once."*
- Design principle in the same spec: *"Not a whole-graph dump — always a bounded neighbourhood."*
- **US-K9:** a graph too large to lay out renders a bounded *"showing N of M"* state.

But the implementation does the reverse: `CanvasGraphViewModel.LoadAsync` with **no root** calls
`WholeGraphAsync` (`:120-126`), whose own comment says *"NO ROOT means the WHOLE GRAPH."* That default
was introduced to fix an earlier "only 2 nodes render" bug — but it **over-corrected**: the fix for
"one arbitrary alphabetical node" is a **bounded overview of meaningful nodes**, not *everything*.
A raw whole-graph is also not *useful*: this repo's own graph, once whole, centred on `string`,
`int`, `Task<T>` — the BCL, not the domain. A hairball of 2,813 nodes is unreadable even if it fit.

So there are **three** problems, not one, and only the first is the transport:

| | Problem | Owner |
|---|---|---|
| 1 | The daemon closes the connection on an oversized frame instead of a legible error (defect B above). | Core (robustness backstop) |
| 2 | The **default view loads the whole raw graph**, violating US-K2 and not scaling. | **Design (the default-view UX) + Core (the query it calls)** |
| 3 | There is **no aggregated/level-of-detail overview** for a graph too big to show node-by-node. | **Design (UX) + Core (server-side aggregation)** |

### The scaling architecture (the real fix)

The graph is a **navigation surface, not a dump**. Detail must be bounded by *viewport, zoom and
focus* — never by *project size*. The client must **never request the whole raw graph**. Concretely
(now specified as US-K10–US-K12 in `knowledge-exploration.md`):

1. **Aggregated overview as the default (no whole-graph load).** With no focus, show a *bounded*
   entry: the most important nodes (degree/betweenness, domain nodes preferred over framework
   primitives — the projection already ranks and can drop-with-count), **or** an aggregated view
   (communities / packages / namespaces as super-nodes). Report "showing N of M." Small by
   construction.
2. **Semantic zoom / level-of-detail.** Zoomed out → clusters as super-nodes with bundled edges;
   zoom in or expand a cluster → its members fetched on demand. Detail scales with zoom, not with
   project size — the standard large-graph technique (Gephi, Graphia, Cytoscape, Obsidian's own
   graph all do this; a raw 10⁴-node force layout is neither renderable nor legible).
3. **Search-first entry + focus+context.** Enter by searching for a known node → focal node → its
   bounded neighbourhood → expand along typed edges (the node-walk US-K4 already specifies). This is
   the primary path for a large project.
4. **Every query bounded to the transport by construction (US-K12).** A request is either sized to
   fit one frame or streamed across frames, so no request can overflow and close the connection. A
   request that *would* exceed the bound returns a labelled "narrow your focus" state, not an opaque
   transport failure. This makes the 1 MiB cap (or any cap) a non-event rather than a wall.

Under this model the frame cap barely matters: a bounded neighbourhood or an aggregated overview is
kilobytes, not megabytes, for a project of any size. Raising the cap is at most a convenience for a
generous streaming chunk — never the load-bearing fix.

