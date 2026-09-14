---
id: adr-0018-node-content-reader-contract
title: "ADR-0018 — The reader fetches node content on demand via a bounded Core query, not on the graph payload"
type: adr
status: accepted
owner: "@timianmalloo"
phase: ""
tags: [architecture, reader, explorer, ipc, contract, transport-bound]
links:
  - { to: architecture, rel: implements }
  - { to: spec-knowledge-explorer-mode, rel: refines }
  - { to: adr-0017-primary-view-mode, rel: relates-to }
  - { to: spec-knowledge-exploration, rel: refines }
  - { to: adr-0025-code-viewer-renderer, rel: relates-to }
  - { to: proof-explore-view-source, rel: relates-to }
review-by: 2027-02-28
summary: >-
  The Explorer's reader needs a selected node's CONTENT (source/markdown/html) and metadata, which the
  graph payload deliberately does not carry. It is fetched on demand for the one selected node via a
  new bounded Core query (a sibling of GraphOverview), not by fattening CanvasNode — because content on
  every node would blow the IPC transport bound (US-K12) for a value only the selected node needs.
review-suggested:
  - { by: adr-0017-primary-view-mode, on: 2026-09-11, reason: "ADR-0017 accepted as amended (Ruling 52): the closed set is the Perspective set; a body may be a docking host; second-host clause discharged by spikes/second-dock-host-unparent" }
---

# ADR-0018 node-content-reader-contract: The reader fetches node content on demand via a bounded Core query

- **Status:** **Accepted 2026-09-14** (Ruling 93, by the slice that built the consumer —
  `docs/proof/explore-view-source.md`). Proposed 2026-08-30. Raised for the Explorer reader
  (`spec-knowledge-explorer-mode` US-E4). Core-owned contract; the Design session consumes it.
  Coordinated per `docs/collaboration/session-contracts.md`.
- **Phase:** UI-shell / graph.

## Erratum (Ruling 93, 2026-09-14)

- **The consumer is Explore's reader, on a gesture.** The reader is `NodeReaderView` in the
  Explore perspective; it calls `NodeContentAsync` on the operator's **View source** gesture
  (Explore's node context menu, first item) for the node it is showing — not on every selection
  change as the Decision's *"the reader calls it when the selection changes"* read. A selection
  renders metadata and edges from the graph payload it already has; the content is a reading act
  the operator asks for, and a reply whose `NodeId` is not the shown node's is discarded (the
  echo the Decision specified is what makes that discard possible).
- **`RenderKind` gains `Html`.** The Decision's parenthesis names *markdown / html / code+language
  / text / none*; the enum shipped with `Code · Text · None` and `.html` classified as `Code`.
  `Html` is added (Owner extension; `.html`/`.htm` are the one producer's, DM7) and the reader
  renders it in a **sandbox** — script disabled, no navigation, no network, asserted by reading
  the runtime's settings back before any document, falling back to highlighted source when it
  cannot be asserted. Rendering repository HTML with script enabled would be executing workspace
  code in the product process (Security floor). The code viewer (ADR-0025) still highlights it
  as source: the structural view is unchanged.
- **The code renderer is ADR-0025's.** Ruling 93 named *"the CodeMirror host already in
  composer.html (one WebView2, read-only mode)"* for the `Code` kind, labelled Inferred and left
  to measurement. Measured: the composer's WebView2 lives in a session document's visual tree
  (Coding), not Explore's, and a WebView2 is never re-parented across trees (ADR-0015); the
  vendored bundle carries no standalone-code mount (Security C7 narrowed it to the composer's
  fences). ADR-0025 — accepted — chose native AvalonEdit for exactly this viewer. The reader
  reuses `CodeViewerView` (read-only, highlighted by `Language`), so the reader's only WebView2 is
  the HTML sandbox, created on the first HTML node. The private-bytes measurement is in the Proof
  Pack (P-4's form).
- **Phase 2 is delivered.** The *Delivery phasing* below described a stub-then-substitute plan;
  the query has shipped on the seam since the code viewer landed, and this slice is the reader's
  consumption of it.

## Context

The reader half of the Explorer renders a selected node in its natural form — rendered markdown,
rendered html, syntax-highlighted read-only code, plain text — plus the node's metadata and its typed
edges (`spec-knowledge-exploration` US-K3/K4). The graph the canvas binds carries
`CanvasNode(Id, Label, Kind, IsRoot, Context)` and `CanvasEdge(...)` — deliberately **no content**:
the overview/neighbourhood payloads are bounded to one IPC frame by construction (US-K12, INV-0003),
and a node's *content* (a whole source file, a rendered document) is one to three orders of magnitude
larger than its graph-node record.

The reader needs content and richer metadata for **one** node at a time — the selected one.

## The options

### A — Fatten `CanvasNode` with content

Add a `Content` (and `RenderKind`, `Metadata`) field to `CanvasNode`, so the graph payload already
carries what the reader needs. **Rejected on the same measured grounds as INV-0003:** the graph draws
up to 1,500 nodes; carrying each node's source content would multiply the payload by the average
artifact size and overflow the 1 MiB frame that already forced the overview cap. It pays the cost of
content for **every** node to serve the **one** the user selected. It also couples the graph query to
the reader's needs, so the two surfaces can no longer evolve independently.

### B — The reader reads the file directly from the App

Have the reader open the artifact from disk itself (the App knows the workspace path). **Rejected:**
it puts artifact reading, kind detection, and (for a code node) the symbol→file→span resolution on the
*presentation* side, duplicating logic the Core already owns (the projection knows a node's source and
span). Two readers of "what is this node's content" is DC-022's shape, and the App-side one would drift
from the authority. It also breaks the one-authority model (ADR-0008): the Core is the workspace
authority; the App renders what it is given.

### C — A bounded, on-demand node-content query on the Core (chosen)

Add a query — a sibling of `GraphOverview`/`OverviewAsync` — that, given a node id, returns that one
node's **content**, its **render kind** (markdown / html / code+language / text / *none — metadata
only*), and its **metadata** (id, kind, context, source path/span, provenance/status), **bounded to
the transport**: content that would exceed a frame is **streamed or truncated with an honest "showing
first N — open the source" marker** (the US-K12 discipline applied to content, not just the graph).
The reader calls it when the selection changes; it fetches content for exactly the selected node.

## Decision

**Adopt C.** A new `IWorkspaceQueries` method — provisionally `NodeContentAsync(nodeId, ct)` returning
a `NodeContent(Id, RenderKind, Language?, Content, Metadata, Edges, Shortfall?)` — is the reader's
source. The graph payload (`CanvasNode`) is **not** fattened; content is fetched on demand for the
selected node only; the response is transport-bounded and honest about truncation, exactly as the
graph queries are.

## Consequences

- **Positive**
  - The graph payload stays small and bounded (US-K12 preserved); the cost of content is paid only for
    the node actually read.
  - The Core stays the single authority for "what is this node and what is in it"; the reader renders
    what it is given (ADR-0008 upheld); no duplicate file-reading/kind-detection on the presentation
    side.
  - Graph and reader evolve independently behind their own queries.
  - The `RenderKind` field makes the reader's per-kind branch (US-K3) a **data** decision from the
    authority, not a guess on the client — a diagram or proof node returns `RenderKind = none` and the
    reader shows its metadata+edges fallback (US-E7) rather than mis-rendering.
- **The transport bound applies to content too.** A very large file returns a `Shortfall` (first N
  bytes/lines + "open the source"), never an opaque failure and never an oversized frame — the reader's
  overflow state (US-E7) is driven by this field.
- **Coordination (cross-session).** This is a **Core-owned** contract (it lives on `IWorkspaceQueries`
  and the daemon/IPC surface, alongside `OverviewAsync`). The Design session consumes it in the reader.
  It is registered as a Design→Core request in `session-contracts.md` so the next session can read it;
  Phase 1 of ADR-0017 primary-view-mode mocks this seam (metadata+edges only) so the shell can land before the query
  exists, and Phase 2 substitutes the real query — a substitution, not a redesign.
- **Negative / cost**
  - One more query on the seam and one more DTO. Accepted: it is small, bounded, and strictly additive;
    it does not touch the existing graph contract.

## Delivery phasing

Tracks ADR-0017 primary-view-mode's phases: the reader is a **stub over a defined seam** in Phase 1 (the `NodeContent`
shape is defined; the reader renders metadata+edges from the graph node it already has, content
mocked), and the real `NodeContentAsync` query lands in Phase 2 as a drop-in behind that seam.
