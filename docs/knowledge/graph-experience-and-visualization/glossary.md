---
id: kb-graph-experience-glossary
title: "Unified Graph Experience — glossary"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [glossary, graphrag, graph-visualization, obsidian, graphify]
links:
  - { to: kb-graph-experience-and-visualization, rel: refines }
review-by: 2026-11-27
review-suggested: []
summary: >-
  Precise definitions for the graph-experience vocabulary — GraphRAG, community detection, force-
  directed layout, node introspection, the code↔docs join — so the code and its docs agree.
---

# Glossary — ubiquitous language

| Term | Definition |
|---|---|
| **GraphRAG** | Retrieval-augmented generation where retrieval walks a knowledge graph (entities+relationships) rather than flat vector chunks; strong on multi-hop, costly for global context. *(Verified, [GX1])* |
| **Local search** (GraphRAG) | Entity-centric query using a node's immediate neighbourhood — vector-like precision, graph-aware. *(Verified, [GX1])* |
| **Global search** (GraphRAG) | Dataset-wide query answered via hierarchical **community summaries**; expensive without LazyGraphRAG. *(Verified, [GX1])* |
| **Community detection** | Clustering related nodes into hierarchical groups (modularity); the mechanism that makes global summaries affordable. *(Verified, [GX1])* |
| **LazyGraphRAG** | GraphRAG variant deferring LLM summarisation to query time; ~0.1% index cost, ~700× cheaper global queries. *(Verified, [GX3])* |
| **Force-directed layout** | A physics-based graph layout (nodes repel, edges pull) producing organic 2D/3D positions; the basis of force-graph renderers. *(Verified, [GX6])* |
| **Node introspection** | Selecting a graph node and inspecting its content + typed edges in a detail panel that routes by node type (code→code renderer, knowledge→markdown, diagram→diagram pane). *(Our term for the core scenario.)* |
| **The node-walk** | The core interaction: step from node to node along typed edges — e.g. C# file → design that specified it → tests that prove it. *(Our term.)* |
| **The code↔docs join** | Graphify's `--join` lens: edges linking code reality to documentation intent, surfacing *documentation-without-implementation* and *risk-without-governance*. *(Verified, GK)* |
| **Provenance / confidence** | Every edge carries how it was known: Graphify `EXTRACTED/INFERRED/AMBIGUOUS` → pack Verified/Inferred/Flagged. A citation is not a promotion. *(Verified, GK6–GK7)* |
| **Betweenness centrality** | A network-science metric: how often a node lies on shortest paths — the graph's bridges; error there propagates furthest. *(Verified, OB14)* |
| **Gap / structural hole** | A missing link between two clusters — an innovation/insight opportunity (InfraNodus). *(Verified, [GX12])* |
| **God-node / hub** | A node with disproportionate degree — the riskiest change surface (Graphify `god-nodes`). *(Verified, GK10)* |
| **Node-based UI** | A graph *editor* (React Flow, litegraph) offering selection→detail→edge-following — a workbench for a neighbourhood, distinct from a force-graph *layout of the whole*. *(Verified, [GX15])* |
| **Graphify** | On-device code knowledge graph (Apache-2.0, PyPI `graphifyy`, canonical graphify.com). **`graphify.net` is unaffiliated.** *(Verified, GK15)* |
