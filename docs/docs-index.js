// Derived from artifact frontmatter by scripts/docs-graph.py — DO NOT hand-edit (frontmatter wins; see knowledge-visualization.md V2/V18).
window.DOCS_INDEX = {
  "schemaVersion": "docs-index/v2",
  "project": "AI-DE",
  "generated": "2026-08-23T22:42:03Z",
  "generator": "docs-graph.py derive",
  "rootId": "architecture",
  "artifactTypes": [
    "knowledge",
    "glossary",
    "spec",
    "architecture",
    "adr",
    "design",
    "design-language",
    "investigation",
    "proof-pack",
    "decision-note",
    "threat-model",
    "privacy-review",
    "api",
    "source",
    "doc",
    "index"
  ],
  "relationRegistry": [
    "implements",
    "refines",
    "depends-on",
    "supersedes",
    "tested-by",
    "documents",
    "uses-term",
    "relates-to"
  ],
  "policyVersion": "traversal-policy/v1",
  "policySha256": "968b035a9618e6f997592e4f7ae91fd412b1c059c0ee89d6d8ff3025c26279fd",
  "traversalPolicies": {
    "grounding": [
      {
        "rel": "implements",
        "direction": "outbound",
        "priority": 0
      },
      {
        "rel": "refines",
        "direction": "outbound",
        "priority": 1
      },
      {
        "rel": "depends-on",
        "direction": "outbound",
        "priority": 2
      },
      {
        "rel": "uses-term",
        "direction": "outbound",
        "priority": 3
      },
      {
        "rel": "tested-by",
        "direction": "outbound",
        "priority": 4
      },
      {
        "rel": "documents",
        "direction": "outbound",
        "priority": 5
      }
    ],
    "impact": [
      {
        "rel": "implements",
        "direction": "inbound",
        "priority": 0
      },
      {
        "rel": "refines",
        "direction": "inbound",
        "priority": 1
      },
      {
        "rel": "depends-on",
        "direction": "inbound",
        "priority": 2
      },
      {
        "rel": "tested-by",
        "direction": "inbound",
        "priority": 3
      },
      {
        "rel": "uses-term",
        "direction": "inbound",
        "priority": 4
      }
    ],
    "proof": [
      {
        "rel": "tested-by",
        "direction": "outbound",
        "priority": 0
      }
    ],
    "explore-neighborhood": [
      {
        "rel": "depends-on",
        "direction": "outbound",
        "priority": 0
      },
      {
        "rel": "depends-on",
        "direction": "inbound",
        "priority": 0
      },
      {
        "rel": "documents",
        "direction": "outbound",
        "priority": 1
      },
      {
        "rel": "documents",
        "direction": "inbound",
        "priority": 1
      },
      {
        "rel": "implements",
        "direction": "outbound",
        "priority": 2
      },
      {
        "rel": "implements",
        "direction": "inbound",
        "priority": 2
      },
      {
        "rel": "refines",
        "direction": "outbound",
        "priority": 3
      },
      {
        "rel": "refines",
        "direction": "inbound",
        "priority": 3
      },
      {
        "rel": "relates-to",
        "direction": "outbound",
        "priority": 4
      },
      {
        "rel": "relates-to",
        "direction": "inbound",
        "priority": 4
      },
      {
        "rel": "supersedes",
        "direction": "outbound",
        "priority": 5
      },
      {
        "rel": "supersedes",
        "direction": "inbound",
        "priority": 5
      },
      {
        "rel": "tested-by",
        "direction": "outbound",
        "priority": 6
      },
      {
        "rel": "tested-by",
        "direction": "inbound",
        "priority": 6
      },
      {
        "rel": "uses-term",
        "direction": "outbound",
        "priority": 7
      },
      {
        "rel": "uses-term",
        "direction": "inbound",
        "priority": 7
      }
    ]
  },
  "limits": {
    "indexBytes": 5242880,
    "artifacts": 1000,
    "relationships": 5000,
    "spatialNodes": 500,
    "spatialEdges": 1000,
    "visibleLabels": 150,
    "surfaces": 100
  },
  "artifacts": [
    {
      "id": "architecture",
      "path": "docs/architecture.md",
      "title": "AI-DE Architecture",
      "type": "architecture",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-02-19",
      "reviewSuggested": [],
      "summary": "How AI-DE is put together today: one .NET 10 WPF executable with a small MVVM seam, one xUnit test project, and two GitHub Actions workflows.",
      "tags": [
        "architecture",
        "wpf"
      ],
      "links": [
        {
          "to": "audit-log",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "be6e45fbc8c0996b6187a5629cb19b515383d34e0e93ca3895c06b5e555176bc"
    },
    {
      "id": "note-collectknowledge-session-2026-08-23",
      "path": "docs/notes/collectknowledge-session-2026-08-23.md",
      "title": "Decision note — /collectknowledge run, 2026-08-23",
      "type": "decision-note",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-02-19",
      "reviewSuggested": [],
      "summary": "Session judgements from the /collectknowledge run that built the ten-topic domain knowledge base: the worktree exception, the seven-file template variant, the V16 over-propagation and its correction.",
      "tags": [
        "decision-note",
        "collectknowledge",
        "session-exhaust"
      ],
      "links": [
        {
          "to": "knowledge-hub",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "7512d0c42ae281d521239e2d341d6a2c1af30a4551b7963e559a931dcfa76675"
    },
    {
      "id": "audit-log",
      "path": "docs/audit/audit-log.md",
      "title": "Audit & Change Log",
      "type": "doc",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "The durable, committed history of what was prompted, done, and decided in this repository, so work compounds across sessions. The two JSONL files are the source of truth; audit-data.js and index.html are derived projections.",
      "tags": [
        "audit",
        "history",
        "change-log",
        "project-memory"
      ],
      "links": [],
      "diagrams": [],
      "sourceSha256": "71819e58949cf27b8efd509901f9d19ca6e787cb2fbec6d274a59b8b8fd5e003"
    },
    {
      "id": "seed-agent-coordination-spec",
      "path": "docs/knowledge/seed-material/agent-coordination-spec.md",
      "title": "Seed — Agent Coordination Layer Specification v0.1",
      "type": "doc",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [
        {
          "by": "knowledge-hub",
          "on": "2026-08-23",
          "reason": "New domain knowledge base established; four findings change prior architecture assumptions (Kuzu archived, MCP stateless, lease fencing gap, thesis framing)"
        }
      ],
      "summary": "The originating specification for the agent coordination layer developed in a separate worktree: claims-not-commits, an append-only per-session JSONL log folded into a SQLite read model of leases, work items and decisions, surfaced via agentctl, MCP and hooks.",
      "tags": [
        "seed-material",
        "multi-agent",
        "coordination",
        "event-log"
      ],
      "links": [
        {
          "to": "knowledge-hub",
          "rel": "relates-to"
        },
        {
          "to": "kb-multi-agent-coordination",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "674c586a4fc83fd222a799832f8e33e39cf4cec4ff71a2c6bd3ec718f5304c90"
    },
    {
      "id": "seed-ai-native-ide-sketch",
      "path": "docs/knowledge/seed-material/ai-native-ide-architecture-sketch.md",
      "title": "Seed — AI-Native IDE Architecture Sketch (v0.1)",
      "type": "doc",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [
        {
          "by": "knowledge-hub",
          "on": "2026-08-23",
          "reason": "New domain knowledge base established; four findings change prior architecture assumptions (Kuzu archived, MCP stateless, lease fencing gap, thesis framing)"
        }
      ],
      "summary": "The originating design sketch for an AI-native IDE (\"Atlas\"): a thin WPF shell hosting agent terminals and WebView2 panes over a local daemon whose Kuzu graph, built by artifact-only extractors, is served to agents via MCP and rendered as derived diagrams.",
      "tags": [
        "seed-material",
        "ai-native-ide",
        "graph",
        "mcp"
      ],
      "links": [
        {
          "to": "knowledge-hub",
          "rel": "relates-to"
        },
        {
          "to": "architecture",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "e2b96b600588a8bd4df6608b151691bb631fffdf9b5251695f2c6033cd7fd1aa"
    },
    {
      "id": "kb-ai-native-ide-shell",
      "path": "docs/knowledge/ai-native-ide-shell/index.md",
      "title": "AI-Native IDE Shell Hosting — domain knowledge",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "Evidence base for a Windows shell hosting agent terminals beside web-rendered visual panes: WPF on .NET 10 versus WinUI 3, the ConPTY foundation, the unsupported terminal control, the WebView2 process model, and OSC 133 as the agent signalling channel.",
      "tags": [
        "wpf",
        "dotnet-10",
        "conpty",
        "webview2",
        "avalondock",
        "terminal",
        "osc-133"
      ],
      "links": [
        {
          "to": "knowledge-hub",
          "rel": "refines"
        },
        {
          "to": "seed-ai-native-ide-sketch",
          "rel": "relates-to"
        },
        {
          "to": "architecture",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "d0ba41e999adc6e1b9bef87b4223e250a811f76846d6e1192ce7adf445f35dbd"
    },
    {
      "id": "kb-azure-cloud-architecture",
      "path": "docs/knowledge/azure-cloud-architecture/index.md",
      "title": "Azure & Cloud Architecture Visualization — domain knowledge",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "Evidence base for deriving and rendering Azure architecture views from Bicep/ARM and live resource state: what static IaC analysis can and cannot recover, the exact icon-licence and tag constraints, and the inventory-is-not-architecture problem that decides whether the view is useful.",
      "tags": [
        "azure",
        "bicep",
        "arm",
        "c4",
        "architecture-visualization",
        "iac"
      ],
      "links": [
        {
          "to": "knowledge-hub",
          "rel": "refines"
        },
        {
          "to": "seed-ai-native-ide-sketch",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "ae398c88d4a5e0dca43cf2ef72638f66eadcbdd1f8a745e016a2ee9d4612dfc3"
    },
    {
      "id": "kb-azure-comparables",
      "path": "docs/knowledge/azure-cloud-architecture/comparables.md",
      "title": "Azure Architecture Visualization — comparable tools",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "Named cloud-architecture visualization tools compared by what they read (live subscription, IaC files, or state), what they emit, and the specific thing each gets wrong — the evidence behind the inventory-versus-architecture distinction.",
      "tags": [
        "azure",
        "comparables",
        "iac-visualization"
      ],
      "links": [
        {
          "to": "kb-azure-cloud-architecture",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "579f8a7ee1e912ac3e17c3ff16152566f4d732b39bf8c2dd1f9c00770663b4c0"
    },
    {
      "id": "kb-azure-glossary",
      "path": "docs/knowledge/azure-cloud-architecture/glossary.md",
      "title": "Azure Architecture Visualization — glossary",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "The ubiquitous language for Azure architecture extraction — ARM, Bicep, symbolic name versus resource name, deployment node versus infrastructure node — defined so code, specs and diagrams use one word per concept.",
      "tags": [
        "azure",
        "glossary",
        "ubiquitous-language"
      ],
      "links": [
        {
          "to": "kb-azure-cloud-architecture",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "b99de4f1ab8db86123c199de93ef44afe91f8617940d90367b20ee993f7a6f57"
    },
    {
      "id": "kb-azure-open-questions",
      "path": "docs/knowledge/azure-cloud-architecture/open-questions.md",
      "title": "Azure Architecture Visualization — open questions & failure modes",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "What the research could not settle about Azure architecture extraction, how this domain reliably fails, and the strongest counter-argument actively sought — that auto-generated cloud diagrams are inventory rather than architecture.",
      "tags": [
        "azure",
        "open-questions",
        "failure-modes"
      ],
      "links": [
        {
          "to": "kb-azure-cloud-architecture",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "054f004a07c85cf34bf381013497ac7036e735deb6a07f93556a1f005a16bfba"
    },
    {
      "id": "kb-azure-references",
      "path": "docs/knowledge/azure-cloud-architecture/references.md",
      "title": "Azure Architecture Visualization — references, constants and limits",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "The authoritative documents plus the exact constants this domain turns on: the Azure icon licence quoted verbatim, tag limits, ARM resource-ID formats, Resource Graph throttling, and the Bicep static limits.",
      "tags": [
        "azure",
        "reference",
        "limits",
        "licensing"
      ],
      "links": [
        {
          "to": "kb-azure-cloud-architecture",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "822e323eab5c05f251ad1b054f8c00c8014287fd5eb22109f6bfaaf88d3e3361"
    },
    {
      "id": "kb-azure-sota",
      "path": "docs/knowledge/azure-cloud-architecture/state-of-the-art.md",
      "title": "Azure Architecture Visualization — state of the art",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "Current practice for deriving Azure architecture views: Microsoft's own guidance and visual conventions, what Resource Graph and the Bicep Visualizer actually provide, and the precise boundary of what Bicep static analysis can determine.",
      "tags": [
        "azure",
        "bicep",
        "arm",
        "resource-graph",
        "c4"
      ],
      "links": [
        {
          "to": "kb-azure-cloud-architecture",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "84cee58c7cef4b74daa2aedd9e187d5673757ab8875ca6d21a31bd4ce22ff42a"
    },
    {
      "id": "kb-azure-sources",
      "path": "docs/knowledge/azure-cloud-architecture/sources.md",
      "title": "Azure Architecture Visualization — sources",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "The full access-dated source list behind the Azure architecture-visualization knowledge base, keyed [S1]..[S33] as cited throughout the topic.",
      "tags": [
        "azure",
        "sources",
        "citations"
      ],
      "links": [
        {
          "to": "kb-azure-cloud-architecture",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "82645167371d742e5f4257d669f628dc65e72d6e8e9fc57df3aa289331b6b267"
    },
    {
      "id": "kb-code-and-infra-extraction",
      "path": "docs/knowledge/code-and-infra-extraction/index.md",
      "title": "Code & Infrastructure Extraction — domain knowledge",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "Evidence base for artifact-only extractors: what Roslyn, ScriptDOM and Bicep can recover statically, the three C# patterns (DI, routes, ORM mapping) that static analysis structurally cannot see, and the supported-versus-unsupported programmatic APIs.",
      "tags": [
        "roslyn",
        "scriptdom",
        "bicep",
        "ts-morph",
        "tree-sitter",
        "scip",
        "extraction"
      ],
      "links": [
        {
          "to": "knowledge-hub",
          "rel": "refines"
        },
        {
          "to": "seed-ai-native-ide-sketch",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "d2c676b81baf14148690d4ea17e534aa641d386ce1f0fe744562efd2517d4f20"
    },
    {
      "id": "kb-code-knowledge-graphs",
      "path": "docs/knowledge/code-knowledge-graphs/index.md",
      "title": "Code Knowledge Graphs & Graph Stores — domain knowledge",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "Evidence base for storing and querying a code knowledge graph. Headline: Kuzu — the store the seed architecture selected — was archived in October 2025, and no embedded, actively maintained, permissively licensed Cypher store with a first-class .NET API exists to replace it.",
      "tags": [
        "knowledge-graph",
        "graph-database",
        "scip",
        "glean",
        "kythe",
        "codeql",
        "gql",
        "kuzu"
      ],
      "links": [
        {
          "to": "knowledge-hub",
          "rel": "refines"
        },
        {
          "to": "seed-ai-native-ide-sketch",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "bc051fece64a5cd8ecb1de710cd67ce2b51a9a3e3dae9bf1727596103d5f9ed2"
    },
    {
      "id": "kb-codegraph-comparables",
      "path": "docs/knowledge/code-knowledge-graphs/comparables.md",
      "title": "Code Knowledge Graphs — comparable systems",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "Code-graph systems and graph stores compared, with project liveness treated as a first-class column — four of the systems surveyed died in the last five years, which is itself the domain's most important pattern.",
      "tags": [
        "comparables",
        "glean",
        "scip",
        "kythe",
        "codeql",
        "graph-stores",
        "liveness"
      ],
      "links": [
        {
          "to": "kb-code-knowledge-graphs",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "5762953cb58810b5f293a944458a6ac14b6ba805f0ef432fed10ec7e7e772d96"
    },
    {
      "id": "kb-codegraph-glossary",
      "path": "docs/knowledge/code-knowledge-graphs/glossary.md",
      "title": "Code Knowledge Graphs — glossary",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "Precise definitions for code-graph vocabulary — SCIP, VName, Angle, overlay database, scope graphs, BSL — so design documents name one concept one way.",
      "tags": [
        "glossary",
        "scip",
        "kythe",
        "gql",
        "ubiquitous-language"
      ],
      "links": [
        {
          "to": "kb-code-knowledge-graphs",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "9c3d8ecc8a58344837c098bfe13b1fd5efc5a4b40b73fdf6e72dfb928911e175"
    },
    {
      "id": "kb-codegraph-open-questions",
      "path": "docs/knowledge/code-knowledge-graphs/open-questions.md",
      "title": "Code Knowledge Graphs — open questions & failure modes",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "The unresolved storage question left by Kuzu's death, the missing performance numbers nobody has published, this domain's high project-mortality pattern, and the strongest counter-argument — that an in-memory symbol table would do.",
      "tags": [
        "open-questions",
        "failure-modes",
        "project-mortality"
      ],
      "links": [
        {
          "to": "kb-code-knowledge-graphs",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "6363ace5a56e742519ff11816b297541e09a54cd6ed7874ca35b92c7144bca0f"
    },
    {
      "id": "kb-codegraph-references",
      "path": "docs/knowledge/code-knowledge-graphs/references.md",
      "title": "Code Knowledge Graphs — references, versions, licences and identifier schemes",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "Standards and papers plus the exact constants — SCIP symbol grammar, Kythe VName fields, store versions and licences, CodeQL incremental thresholds, and the GraphRAG cost multiplier.",
      "tags": [
        "reference",
        "scip",
        "kythe",
        "gql",
        "licences",
        "versions"
      ],
      "links": [
        {
          "to": "kb-code-knowledge-graphs",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "52bda35ea7f5ef2b96d35a68b8f4afbd3e6696e093a8585ba69e95ffcb741da3"
    },
    {
      "id": "kb-codegraph-sota",
      "path": "docs/knowledge/code-knowledge-graphs/state-of-the-art.md",
      "title": "Code Knowledge Graphs — state of the art",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "The current state of embedded graph stores, graph query languages, the surviving code-graph systems, symbol-identity schemes, incremental indexing and graph-served retrieval for LLM agents.",
      "tags": [
        "graph-stores",
        "opencypher",
        "gql",
        "scip",
        "glean",
        "kythe",
        "codeql",
        "graphrag"
      ],
      "links": [
        {
          "to": "kb-code-knowledge-graphs",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "20cc9650014881b79e51ab5bc79d9d222d92e4e90abcedd049b7d07fed72a822"
    },
    {
      "id": "kb-codegraph-sources",
      "path": "docs/knowledge/code-knowledge-graphs/sources.md",
      "title": "Code Knowledge Graphs — sources",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "The full access-dated source list behind the code-knowledge-graph base, keyed [S1]..[S32], including the liveness citations that carry the domain's most consequential findings.",
      "tags": [
        "sources",
        "citations"
      ],
      "links": [
        {
          "to": "kb-code-knowledge-graphs",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "b4c64638fb83e9b5c66cc1bd944b180f939926bcf1f12a7ce6e7c22b233e7d9e"
    },
    {
      "id": "kb-coord-comparables",
      "path": "docs/knowledge/multi-agent-coordination/comparables.md",
      "title": "Multi-Agent Coordination — comparable systems",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "How every surveyed multi-agent coding system avoids conflict — by isolation, by turn-taking, by hierarchy, or by refusing parallelism — and the gap that makes the claims-log approach novel.",
      "tags": [
        "comparables",
        "multi-agent",
        "merge-queue",
        "frameworks"
      ],
      "links": [
        {
          "to": "kb-multi-agent-coordination",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "4f5a701f4374d2650366865a79f80740ac17d93e9b47caa5691c812bf613e0a3"
    },
    {
      "id": "kb-coord-glossary",
      "path": "docs/knowledge/multi-agent-coordination/glossary.md",
      "title": "Multi-Agent Coordination — glossary",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "Precise definitions for coordination vocabulary — lease, fencing token, fold, projection, union merge driver, ULID — so the protocol, the read model and the docs use one word per concept.",
      "tags": [
        "glossary",
        "event-sourcing",
        "leases",
        "ubiquitous-language"
      ],
      "links": [
        {
          "to": "kb-multi-agent-coordination",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "87eb597fe11333a5cc3f5d664777267356c26e33daf876d31e4dbe14cad1969a"
    },
    {
      "id": "kb-coord-open-questions",
      "path": "docs/knowledge/multi-agent-coordination/open-questions.md",
      "title": "Multi-Agent Coordination — open questions & failure modes",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "The correctness gaps the literature identifies in a lease-and-log coordination design, and the four strongest disconfirming views — Cognition, Anthropic's own caution, METR, and Kleppmann — assessed rather than dismissed.",
      "tags": [
        "open-questions",
        "failure-modes",
        "fencing",
        "coherence"
      ],
      "links": [
        {
          "to": "kb-multi-agent-coordination",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "ef73b735cd863815662fa234957b7ef394e560a043e639394cfecc80094a4697"
    },
    {
      "id": "kb-coord-references",
      "path": "docs/knowledge/multi-agent-coordination/references.md",
      "title": "Multi-Agent Coordination — references and measured numbers",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "The papers and specifications behind this domain, plus every measured number with its source — token multipliers, MAST failure frequencies, METR's productivity result, and the ULID and UUIDv7 constants.",
      "tags": [
        "reference",
        "mast",
        "metr",
        "rfc-9562",
        "ulid",
        "chubby",
        "event-sourcing"
      ],
      "links": [
        {
          "to": "kb-multi-agent-coordination",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "1631ecfe4eeb4cea17b4430f4ce80a036f37f96cdc0d819e9e99fb557a1f33ec"
    },
    {
      "id": "kb-coord-sota",
      "path": "docs/knowledge/multi-agent-coordination/state-of-the-art.md",
      "title": "Multi-Agent Coordination — state of the art",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "What the deployed multi-agent systems actually do, what MAST measured about how they fail, the distributed-systems primitives behind leases and logs, and git's real behaviour as a coordination substrate.",
      "tags": [
        "multi-agent",
        "anthropic",
        "mast",
        "event-sourcing",
        "leases",
        "git-worktree"
      ],
      "links": [
        {
          "to": "kb-multi-agent-coordination",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "1fad8d8a9eb7bcd711b02f8ffc6b43e0db63e365922d6c77560b87ac267e53ea"
    },
    {
      "id": "kb-coord-sources",
      "path": "docs/knowledge/multi-agent-coordination/sources.md",
      "title": "Multi-Agent Coordination — sources",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "The full access-dated source list behind the multi-agent coordination knowledge base, keyed [S1]..[S22], distinguishing first-party measurement from vendor comparison.",
      "tags": [
        "sources",
        "citations"
      ],
      "links": [
        {
          "to": "kb-multi-agent-coordination",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "1c65f3654c86052f3131f11eb495cae96fe2cfe4925391d6828cccb1b1819ed0"
    },
    {
      "id": "kb-diagram-generation",
      "path": "docs/knowledge/diagram-generation/index.md",
      "title": "Diagram Generation & Rendering — domain knowledge",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "Evidence base for generating every view from a graph query rather than authoring it: the DSL and renderer landscape with verified versions and licences, the layout-stability problem that decides whether regenerated diagrams are usable, and the case against generated diagrams.",
      "tags": [
        "diagrams-as-code",
        "mermaid",
        "d2",
        "plantuml",
        "structurizr",
        "graph-layout",
        "c4"
      ],
      "links": [
        {
          "to": "knowledge-hub",
          "rel": "refines"
        },
        {
          "to": "seed-ai-native-ide-sketch",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "babc1d816a178d454ce18f6007f5c55054449046ecbffa776cb375986e8c5eaf"
    },
    {
      "id": "kb-diagrams-comparables",
      "path": "docs/knowledge/diagram-generation/comparables.md",
      "title": "Diagram Generation — comparable tools",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "Side-by-side comparison of diagram DSLs and interactive graph renderers by diagram type, licence, rendering model and failure mode — the table that decides which renderer serves which view.",
      "tags": [
        "comparables",
        "mermaid",
        "d2",
        "plantuml",
        "structurizr",
        "cytoscape"
      ],
      "links": [
        {
          "to": "kb-diagram-generation",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "0bd6de5bcb6626c9fac2b828ace680fd87431a0920b8f962c6534af4b105a6c6"
    },
    {
      "id": "kb-diagrams-glossary",
      "path": "docs/knowledge/diagram-generation/glossary.md",
      "title": "Diagram Generation — glossary",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "Precise definitions for the diagram-generation vocabulary — Sugiyama method, orthogonal routing, layout stability, headless rendering, workspace — so the design uses one word per concept.",
      "tags": [
        "glossary",
        "layout",
        "rendering",
        "ubiquitous-language"
      ],
      "links": [
        {
          "to": "kb-diagram-generation",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "d21d203d5f0977048c607cf3ee2ced70351ee0dca3d878337ac881e21822fc6e"
    },
    {
      "id": "kb-diagrams-open-questions",
      "path": "docs/knowledge/diagram-generation/open-questions.md",
      "title": "Diagram Generation — open questions & failure modes",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "The unsettled questions (SVG determinism, Mermaid's published version, TALA's licence), the ways generated-diagram pipelines reliably fail, and the sought counter-argument that generated diagrams communicate worse than hand-drawn ones.",
      "tags": [
        "open-questions",
        "failure-modes",
        "determinism",
        "layout-stability"
      ],
      "links": [
        {
          "to": "kb-diagram-generation",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "8680263a7392f701dd7c986e88c2d25ba75cdeeb3ce7ec5d558fb30ee06d254c"
    },
    {
      "id": "kb-diagrams-references",
      "path": "docs/knowledge/diagram-generation/references.md",
      "title": "Diagram Generation — references, versions, licences and constants",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "The version numbers, licence terms, documented scale figures and security defaults for every diagram tool in scope, each read from a primary source and dated — the facts to quote rather than recall.",
      "tags": [
        "reference",
        "versions",
        "licences",
        "layout-algorithms"
      ],
      "links": [
        {
          "to": "kb-diagram-generation",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "37d82e29f90ec3f40ba508201e443c0142a911d1fa738b184d1f152a274d27f5"
    },
    {
      "id": "kb-diagrams-sota",
      "path": "docs/knowledge/diagram-generation/state-of-the-art.md",
      "title": "Diagram Generation — state of the art",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "What each diagram DSL and renderer actually is today — versions, diagram types, rendering model and security posture — plus the layout algorithms underneath and the unsolved layout-stability problem.",
      "tags": [
        "mermaid",
        "d2",
        "plantuml",
        "structurizr",
        "graphviz",
        "elk",
        "layout"
      ],
      "links": [
        {
          "to": "kb-diagram-generation",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "f87f6e636a2bab8d5a072bb02b64d92b808a87797337f83b16c0a9ecb0ea1d42"
    },
    {
      "id": "kb-diagrams-sources",
      "path": "docs/knowledge/diagram-generation/sources.md",
      "title": "Diagram Generation — sources",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "The full access-dated source list behind the diagram-generation knowledge base, keyed [S1]..[S29] as cited throughout the topic.",
      "tags": [
        "sources",
        "citations"
      ],
      "links": [
        {
          "to": "kb-diagram-generation",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "41ec65a8fa4bd58de9942111356eefcfa0a35f472a7ef7aa3593535a4faf90c5"
    },
    {
      "id": "kb-domain-comparables",
      "path": "docs/knowledge/domain-modeling-and-erm/comparables.md",
      "title": "Domain Modelling & ERM — comparable methods and tools",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "Modelling methods and tools compared by the question that matters for us — is what this produces extractable from artifacts, or does it require human judgement?",
      "tags": [
        "comparables",
        "contextmapper",
        "eventstorming",
        "tbls",
        "archunit"
      ],
      "links": [
        {
          "to": "kb-domain-modeling-and-erm",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "c86149ddd6113d52abdc5be6004c311f0d0a1cc380f78d376bffd8af6de6273c"
    },
    {
      "id": "kb-domain-glossary",
      "path": "docs/knowledge/domain-modeling-and-erm/glossary.md",
      "title": "Domain Modelling & ERM — glossary",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "The ubiquitous language of domain and ER modelling defined precisely — aggregate, bounded context, owned entity, shadow property, bitemporal — so the graph and the specs use one word per concept.",
      "tags": [
        "glossary",
        "ddd",
        "erm",
        "ubiquitous-language"
      ],
      "links": [
        {
          "to": "kb-domain-modeling-and-erm",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "87f6f514cf9566c2c9ff799420b48bbb22ef1121339bd43b0267041282ce89ba"
    },
    {
      "id": "kb-domain-modeling-and-erm",
      "path": "docs/knowledge/domain-modeling-and-erm/index.md",
      "title": "Domain Modelling, DDD & ERM — domain knowledge",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "Evidence base for extracting a domain and ER model from artifacts: what DDD stereotypes are reliably machine-detectable, why bounded contexts are not, and the anemic-domain-model problem that no structural heuristic can see through.",
      "tags": [
        "ddd",
        "aggregates",
        "erm",
        "ef-core",
        "eventstorming",
        "context-mapping"
      ],
      "links": [
        {
          "to": "knowledge-hub",
          "rel": "refines"
        },
        {
          "to": "seed-ai-native-ide-sketch",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "c9074571d25994fecd154f4a20c403a9e6fff744b07219c1359fa553f0ddffe3"
    },
    {
      "id": "kb-domain-open-questions",
      "path": "docs/knowledge/domain-modeling-and-erm/open-questions.md",
      "title": "Domain Modelling & ERM — open questions & failure modes",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "What research could not settle, how domain extraction fails silently, and the two disconfirming cases — against tactical DDD and against ER diagrams of large schemas — both of which survive.",
      "tags": [
        "open-questions",
        "failure-modes",
        "anemic-model",
        "disconfirming"
      ],
      "links": [
        {
          "to": "kb-domain-modeling-and-erm",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "605383928330ba9f319cc03a3b481392b540c159a852d6d98806ff32497921ef"
    },
    {
      "id": "kb-domain-references",
      "path": "docs/knowledge/domain-modeling-and-erm/references.md",
      "title": "Domain Modelling & ERM — references, definitions and constants",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "The load-bearing definitions stated precisely — Vernon's four rules, the context-map patterns, the SCD types, the normal forms, Crow's Foot semantics, the temporal syntax and the EF Core IModel API.",
      "tags": [
        "reference",
        "ddd",
        "scd",
        "normal-forms",
        "crows-foot",
        "temporal",
        "ef-core"
      ],
      "links": [
        {
          "to": "kb-domain-modeling-and-erm",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "a608cfb2f1297fe6a181f3fc1e36fb43a7508ff7d03a56b44cb9a34ef5ce4e82"
    },
    {
      "id": "kb-domain-sota",
      "path": "docs/knowledge/domain-modeling-and-erm/state-of-the-art.md",
      "title": "Domain Modelling & ERM — state of the art",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "The DDD canon and its strategic/tactical split, how domain stereotypes appear in .NET code, collaborative modelling methods, the ERM notation families, and what a DDL parser can and cannot recover.",
      "tags": [
        "ddd",
        "evans",
        "vernon",
        "eventstorming",
        "erm",
        "ef-core",
        "temporal"
      ],
      "links": [
        {
          "to": "kb-domain-modeling-and-erm",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "e1e3b0fdb03c94da96aa8cb79e915a90675b1263724f7dcc3acbfa34dcc0761e"
    },
    {
      "id": "kb-domain-sources",
      "path": "docs/knowledge/domain-modeling-and-erm/sources.md",
      "title": "Domain Modelling & ERM — sources",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "The access-dated source list behind the domain-modelling and ERM knowledge base, keyed [S1]..[S19], separating established books from fetched documentation.",
      "tags": [
        "sources",
        "citations"
      ],
      "links": [
        {
          "to": "kb-domain-modeling-and-erm",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "1962d80f744c31fcb92896eeff72b9b40afc5ee7717c4a4d5a3fecb89266e883"
    },
    {
      "id": "kb-extraction-comparables",
      "path": "docs/knowledge/code-and-infra-extraction/comparables.md",
      "title": "Code & Infrastructure Extraction — comparable tools",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "Extractors compared by the question that decides whether they can be used at all — does this work offline from repository artifacts, or does it need a live database, a build, or a network call?",
      "tags": [
        "comparables",
        "extraction",
        "offline",
        "roslyn",
        "sql",
        "bicep"
      ],
      "links": [
        {
          "to": "kb-code-and-infra-extraction",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "d48b95b40c3280a09a8011d98c43fc45039736cb1d47af8408c0628b1ead73c0"
    },
    {
      "id": "kb-extraction-glossary",
      "path": "docs/knowledge/code-and-infra-extraction/glossary.md",
      "title": "Code & Infrastructure Extraction — glossary",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "Precise definitions for extraction vocabulary — design-time build, DocumentationCommentId, CST versus AST, dacpac, scope — so extractors, the graph and the docs use one word per concept.",
      "tags": [
        "glossary",
        "roslyn",
        "sql",
        "bicep",
        "ubiquitous-language"
      ],
      "links": [
        {
          "to": "kb-code-and-infra-extraction",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "604d0f697d9fc584eab755f4b6d8b50a91feddeb9961ef56d5ced31ef3bba9db"
    },
    {
      "id": "kb-extraction-open-questions",
      "path": "docs/knowledge/code-and-infra-extraction/open-questions.md",
      "title": "Code & Infrastructure Extraction — open questions & failure modes",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "The spikes this domain needs — scip-dotnet, source generators, multi-targeting — the ways extraction fails silently, and the disconfirming views on tree-sitter, LSIF and DacFx.",
      "tags": [
        "open-questions",
        "failure-modes",
        "source-generators",
        "scip-dotnet"
      ],
      "links": [
        {
          "to": "kb-code-and-infra-extraction",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "06adc262b97bd8327c7f355e4badb06fd747103254c5ab51855f611737bb5a8d"
    },
    {
      "id": "kb-extraction-references",
      "path": "docs/knowledge/code-and-infra-extraction/references.md",
      "title": "Code & Infrastructure Extraction — references, packages, versions and APIs",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "Every package ID, API name, version, licence and measured timing in this domain, read from NuGet, npm, Maven or official documentation — including the two verbatim disclaimers that decide which APIs may be used.",
      "tags": [
        "reference",
        "nuget",
        "api-names",
        "versions",
        "licences"
      ],
      "links": [
        {
          "to": "kb-code-and-infra-extraction",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "293a26ca66d2282099bf4837e232c5b953d57c42ac8adde1e896c5cf1de6ba36"
    },
    {
      "id": "kb-extraction-sota",
      "path": "docs/knowledge/code-and-infra-extraction/state-of-the-art.md",
      "title": "Code & Infrastructure Extraction — state of the art",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "What each extractor actually provides — Roslyn's three layers and its blind spots, the SQL parsing options and which need a live database, the supported Bicep API, and the cross-language tools with their real granularity.",
      "tags": [
        "roslyn",
        "scriptdom",
        "dacfx",
        "bicep",
        "ts-morph",
        "javaparser",
        "tree-sitter"
      ],
      "links": [
        {
          "to": "kb-code-and-infra-extraction",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "1ddd89fef0e6ce902658ecb788c03a6402e1016c41fdcec87a929365d68be2bd"
    },
    {
      "id": "kb-extraction-sources",
      "path": "docs/knowledge/code-and-infra-extraction/sources.md",
      "title": "Code & Infrastructure Extraction — sources",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "The access-dated source list behind the extraction knowledge base, keyed [S1]..[S19], with package IDs and API names read from registries rather than recalled.",
      "tags": [
        "sources",
        "citations"
      ],
      "links": [
        {
          "to": "kb-code-and-infra-extraction",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "60b057ac873a207d260e689da3b421f27295567dd54e2382fd20a2852b809819"
    },
    {
      "id": "kb-mcp-agent-integration",
      "path": "docs/knowledge/mcp-and-agent-integration/index.md",
      "title": "MCP & Agent Integration — domain knowledge",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "Evidence base for exposing a knowledge graph to coding agents over MCP: the stateless 2026-07-28 spec and what it deprecated, the C#/.NET SDK, the per-client config and hook matrix, and the security surface a write-capable server inherits.",
      "tags": [
        "mcp",
        "claude-code",
        "copilot",
        "hooks",
        "agent-integration",
        "tool-design"
      ],
      "links": [
        {
          "to": "knowledge-hub",
          "rel": "refines"
        },
        {
          "to": "seed-ai-native-ide-sketch",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "e7f9866caa85b69be8de01d200171fa07098f85172f7232dad07c599834b8414"
    },
    {
      "id": "kb-mcp-comparables",
      "path": "docs/knowledge/mcp-and-agent-integration/comparables.md",
      "title": "MCP & Agent Integration — client and SDK comparison",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "Which MCP features each client supports, where its config lives, and what interception surface it offers — the matrix that decides which integration path is universal and which is Claude-Code-only.",
      "tags": [
        "comparables",
        "claude-code",
        "copilot",
        "cursor",
        "sdks"
      ],
      "links": [
        {
          "to": "kb-mcp-agent-integration",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "0e80f6d180e5c4f61697d642e05a6fe47e575187c6df3ad1c0dd6c076b378c0e"
    },
    {
      "id": "kb-mcp-glossary",
      "path": "docs/knowledge/mcp-and-agent-integration/glossary.md",
      "title": "MCP & Agent Integration — glossary",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "Precise definitions for MCP vocabulary — host, client, server, the primitives, MRTR, modern versus legacy era — plus the named threat classes, so design documents use one word per concept.",
      "tags": [
        "glossary",
        "mcp",
        "ubiquitous-language"
      ],
      "links": [
        {
          "to": "kb-mcp-agent-integration",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "5a3950801967e55f3064f8dbed70ca2543316344a8687c76a94bf154bd0457d4"
    },
    {
      "id": "kb-mcp-open-questions",
      "path": "docs/knowledge/mcp-and-agent-integration/open-questions.md",
      "title": "MCP & Agent Integration — open questions & failure modes",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "What research could not settle about MCP integration, how this domain fails — including the graph-as-injection-channel risk we create ourselves — and the strongest published case that MCP is the wrong integration layer.",
      "tags": [
        "open-questions",
        "failure-modes",
        "mcp-security",
        "prompt-injection"
      ],
      "links": [
        {
          "to": "kb-mcp-agent-integration",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "55c08663f8b7314177633a292093c1b03db002156c54e19a99a9e30f245fd844"
    },
    {
      "id": "kb-mcp-references",
      "path": "docs/knowledge/mcp-and-agent-integration/references.md",
      "title": "MCP & Agent Integration — references, versions, packages and exact names",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "The spec revisions, deprecation dates, error codes, package IDs, config filenames and the complete Claude Code hook event list — every load-bearing exact name in this domain, read from its primary source.",
      "tags": [
        "reference",
        "mcp-spec",
        "package-ids",
        "hook-events",
        "config-files"
      ],
      "links": [
        {
          "to": "kb-mcp-agent-integration",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "fef8b83f352181906b202d05a5651ba69cd672dbdc4c250c10a82677db1d72e1"
    },
    {
      "id": "kb-mcp-sota",
      "path": "docs/knowledge/mcp-and-agent-integration/state-of-the-art.md",
      "title": "MCP & Agent Integration — state of the art",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "What MCP is as of the 2026-07-28 revision — stateless base protocol, two transports, the live and deprecated primitives — plus the SDKs, the hook surfaces per client, the documented security model, and the prior art for agent-written knowledge.",
      "tags": [
        "mcp",
        "spec",
        "transports",
        "hooks",
        "security",
        "agent-memory"
      ],
      "links": [
        {
          "to": "kb-mcp-agent-integration",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "251da9d0715d23239eaa9777cdd631d170625f16e076cb9bcba6306a3daac190"
    },
    {
      "id": "kb-mcp-sources",
      "path": "docs/knowledge/mcp-and-agent-integration/sources.md",
      "title": "MCP & Agent Integration — sources",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "The full access-dated source list behind the MCP knowledge base, keyed [S1]..[S29], with a freshness warning — this is the fastest-moving topic in the base.",
      "tags": [
        "sources",
        "citations"
      ],
      "links": [
        {
          "to": "kb-mcp-agent-integration",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "a7a721ff19bb1af9c13ea330d4300215e8386a77abc484722081494406f2a539"
    },
    {
      "id": "kb-micro-comparables",
      "path": "docs/knowledge/microservice-interaction-visualization/comparables.md",
      "title": "Microservice Interaction Visualization — comparable tools",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "Service-graph and trace-diagram tools compared by acquisition method, what each requires, what it can see and what it is structurally blind to — the table that shows why no single tool answers the architecture question.",
      "tags": [
        "comparables",
        "jaeger",
        "tempo",
        "kiali",
        "hubble",
        "appmap"
      ],
      "links": [
        {
          "to": "kb-microservice-interaction",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "a9664c44f3b52ba0daa5f082cd610ef927deae7bf5d228eef216b797c77e39df"
    },
    {
      "id": "kb-micro-glossary",
      "path": "docs/knowledge/microservice-interaction-visualization/glossary.md",
      "title": "Microservice Interaction Visualization — glossary",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "Precise definitions for tracing and conformance vocabulary — SpanKind, creation context, virtual node, convergence/divergence/absence — so the graph, the MCP tools and the UI all use the same words.",
      "tags": [
        "glossary",
        "tracing",
        "reflexion-model",
        "ubiquitous-language"
      ],
      "links": [
        {
          "to": "kb-microservice-interaction",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "a1aeeb3758d9c7a586d2bd57c3255e55b270d255cb8f7482fad5851278bff55d"
    },
    {
      "id": "kb-micro-open-questions",
      "path": "docs/knowledge/microservice-interaction-visualization/open-questions.md",
      "title": "Microservice Interaction Visualization — open questions & failure modes",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "The unsettled questions about trace-derived architecture, the ways this domain fails silently, and the sought counter-argument that trace-derived diagrams actively mislead.",
      "tags": [
        "open-questions",
        "failure-modes",
        "sampling-bias",
        "tracing"
      ],
      "links": [
        {
          "to": "kb-microservice-interaction",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "ab4753bb8225102a15686c29460a4a7fd7f69c9554e164c0f79d3a0d55c05bfa"
    },
    {
      "id": "kb-micro-references",
      "path": "docs/knowledge/microservice-interaction-visualization/references.md",
      "title": "Microservice Interaction Visualization — references, versions and constants",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "The specifications and the exact constants: semconv v1.44.0 stability by domain, OTLP ports and paths, traceparent format, SpanKind values, and the Tempo peer-attribute fallback order.",
      "tags": [
        "reference",
        "opentelemetry",
        "semconv",
        "otlp",
        "w3c-trace-context"
      ],
      "links": [
        {
          "to": "kb-microservice-interaction",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "83487808529e7bd019bee8b7685c85dead0df44d45a20770e92ada82fe1626e6"
    },
    {
      "id": "kb-micro-sota",
      "path": "docs/knowledge/microservice-interaction-visualization/state-of-the-art.md",
      "title": "Microservice Interaction Visualization — state of the art",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "How service graphs are actually derived from telemetry today, what the OpenTelemetry trace model guarantees, how async flows are modelled, and the modelling problems that make trace-to-sequence-diagram harder than it looks.",
      "tags": [
        "opentelemetry",
        "otlp",
        "service-graph",
        "sampling",
        "sequence-diagram"
      ],
      "links": [
        {
          "to": "kb-microservice-interaction",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "12de1655d69cd12861e0cdd5ae4133f57de9c40f47083e7a70bee776203cc6db"
    },
    {
      "id": "kb-micro-sources",
      "path": "docs/knowledge/microservice-interaction-visualization/sources.md",
      "title": "Microservice Interaction Visualization — sources",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "The full access-dated source list behind the microservice-interaction knowledge base, keyed [S1]..[S25] as cited throughout the topic.",
      "tags": [
        "sources",
        "citations"
      ],
      "links": [
        {
          "to": "kb-microservice-interaction",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "5cdeaf9f758213f511deb7c5eb85f21c9aecd286b28bc3cc5516ce52c455cde1"
    },
    {
      "id": "kb-microservice-interaction",
      "path": "docs/knowledge/microservice-interaction-visualization/index.md",
      "title": "Microservice Interaction Visualization — domain knowledge",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "Evidence base for turning runtime traces into service graphs and sequence diagrams: which OpenTelemetry semantic conventions are actually stable, why pub/sub breaks parent-child tracing, and the reflexion-model vocabulary for declared-versus-observed dependencies.",
      "tags": [
        "opentelemetry",
        "tracing",
        "service-graph",
        "sequence-diagram",
        "observability"
      ],
      "links": [
        {
          "to": "knowledge-hub",
          "rel": "refines"
        },
        {
          "to": "seed-ai-native-ide-sketch",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "5d6bd244c55555d5c81211446835f7b34e686440139812bc9f420c070830158a"
    },
    {
      "id": "kb-multi-agent-coordination",
      "path": "docs/knowledge/multi-agent-coordination/index.md",
      "title": "Multi-Agent Coordination — domain knowledge",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "Evidence base for the agent coordination layer built in a separate worktree: what the published multi-agent systems measure, the fencing-token hazard in lease-based claims, why per-session JSONL merges cleanly in git, and the strongest published case against the design.",
      "tags": [
        "multi-agent",
        "coordination",
        "event-sourcing",
        "leases",
        "git-worktree",
        "mast"
      ],
      "links": [
        {
          "to": "knowledge-hub",
          "rel": "refines"
        },
        {
          "to": "seed-agent-coordination-spec",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "528921d798a95942fc68bb2e663d80faf61960e5f271498463a514701b2f9cca"
    },
    {
      "id": "kb-shell-comparables",
      "path": "docs/knowledge/ai-native-ide-shell/comparables.md",
      "title": "AI-Native IDE Shell — comparable hosts",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "Frameworks, docking libraries, terminal-embedding options and existing agent hosts compared against the specific job of running many agent terminals beside derived visual panes.",
      "tags": [
        "comparables",
        "vscode",
        "warp",
        "windows-terminal",
        "frameworks"
      ],
      "links": [
        {
          "to": "kb-ai-native-ide-shell",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "fae874157121e6aef24d91abfcacc2fab46c99d8c80a44e645a7a0b114180e38"
    },
    {
      "id": "kb-shell-glossary",
      "path": "docs/knowledge/ai-native-ide-shell/glossary.md",
      "title": "AI-Native IDE Shell — glossary",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "Precise definitions for shell-hosting vocabulary — ConPTY, HwndHost, airspace, OSC 133, Evergreen runtime, virtual host mapping — so the shell code and its docs agree.",
      "tags": [
        "glossary",
        "conpty",
        "webview2",
        "wpf",
        "ubiquitous-language"
      ],
      "links": [
        {
          "to": "kb-ai-native-ide-shell",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "1ab7690aa841388e39473a751febe5e224e9295f3a41076429bde66fde9ed5e7"
    },
    {
      "id": "kb-shell-open-questions",
      "path": "docs/knowledge/ai-native-ide-shell/open-questions.md",
      "title": "AI-Native IDE Shell — open questions & failure modes",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "What could not be settled about shell hosting, the documented ways this class of project fails, and the three counter-arguments — WinUI 3, xterm.js and FileSystemWatcher — assessed on evidence.",
      "tags": [
        "open-questions",
        "failure-modes",
        "conpty",
        "webview2",
        "wpf"
      ],
      "links": [
        {
          "to": "kb-ai-native-ide-shell",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "8e5e104da4054db8c133a20237da6e3f8a065a0ca7a4f18138e676614147f1d4"
    },
    {
      "id": "kb-shell-references",
      "path": "docs/knowledge/ai-native-ide-shell/references.md",
      "title": "AI-Native IDE Shell — references, APIs, versions and constants",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "The exact API names, sequences, buffer sizes, versions and licences for shell hosting — the facts to quote rather than recall.",
      "tags": [
        "reference",
        "conpty",
        "webview2",
        "osc-133",
        "api-names"
      ],
      "links": [
        {
          "to": "kb-ai-native-ide-shell",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "fb28e3b63e1ed6b5c001bc2f74bc2d290f546a0a7c637da354010d18a6cb0fdb"
    },
    {
      "id": "kb-shell-sota",
      "path": "docs/knowledge/ai-native-ide-shell/state-of-the-art.md",
      "title": "AI-Native IDE Shell — state of the art",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "The framework, docking, terminal-embedding, WebView2 and signalling landscape as it actually stands — including which pieces are supported APIs and which are CI artefacts.",
      "tags": [
        "wpf",
        "winui3",
        "avalonia",
        "conpty",
        "webview2",
        "osc-133",
        "filesystemwatcher"
      ],
      "links": [
        {
          "to": "kb-ai-native-ide-shell",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "326847907117343a50d9ac07c193df8b1500716bed59143f49fa06eb77f17fa1"
    },
    {
      "id": "kb-shell-sources",
      "path": "docs/knowledge/ai-native-ide-shell/sources.md",
      "title": "AI-Native IDE Shell — sources",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "The access-dated source list behind the shell-hosting knowledge base, keyed [S1]..[S16], separating Microsoft primary documentation from the secondary claims about other products.",
      "tags": [
        "sources",
        "citations"
      ],
      "links": [
        {
          "to": "kb-ai-native-ide-shell",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "ecce5e0f66fd6cf2bb44d42c22a2f8ca5cae9337fb34bcd3622c8f24636f15ef"
    },
    {
      "id": "kb-uml-comparables",
      "path": "docs/knowledge/uml-mde-and-4gl/comparables.md",
      "title": "UML, MDE & 4GL — comparable approaches across five decades",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "Every generation of the models-are-the-product idea, what its model-to-artifact mechanism was, where it succeeded, and why it failed — plus the narrow cases that worked and what they have in common.",
      "tags": [
        "comparables",
        "case-tools",
        "mda",
        "low-code",
        "dsl",
        "spec-driven"
      ],
      "links": [
        {
          "to": "kb-uml-mde-and-4gl",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "4beb3f598c80a6a02662f03193395077926d59dde6bb3b43bb763afe613c106d"
    },
    {
      "id": "kb-uml-glossary",
      "path": "docs/knowledge/uml-mde-and-4gl/glossary.md",
      "title": "UML, MDE & 4GL — glossary",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "Precise definitions for the modelling vocabulary — CIM/PIM/PSM, MOF, XMI, OCL, fUML, Alf, 4GL, round-trip, language workbench — so the history is discussed in its own terms.",
      "tags": [
        "glossary",
        "mda",
        "uml",
        "dsl",
        "ubiquitous-language"
      ],
      "links": [
        {
          "to": "kb-uml-mde-and-4gl",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "1c772b237b1180b9e3d13aa7523925a39b1c84ff84b9c91cb2f6f6581575cb34"
    },
    {
      "id": "kb-uml-mde-and-4gl",
      "path": "docs/knowledge/uml-mde-and-4gl/index.md",
      "title": "UML, Model-Driven Engineering & 4GL Design Surfaces — domain knowledge",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "Evidence base for \"the models are the product\" — the fifty-year graveyard of that idea from CASE to MDA to low-code, the narrow conditions under which it has actually worked, and why the code-derived-views variant escapes four of the five historical failure modes.",
      "tags": [
        "uml",
        "mda",
        "mde",
        "sysml",
        "4gl",
        "low-code",
        "generative-design",
        "dsl"
      ],
      "links": [
        {
          "to": "knowledge-hub",
          "rel": "refines"
        },
        {
          "to": "seed-ai-native-ide-sketch",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "9a7af97109d0d7cef77e396f68b7d1eca0ca5a41b4fac0e7fbeb6df577de0d36"
    },
    {
      "id": "kb-uml-open-questions",
      "path": "docs/knowledge/uml-mde-and-4gl/open-questions.md",
      "title": "UML, MDE & 4GL — open questions & the case against the thesis",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "The five structural forces that have defeated every models-are-the-product generation since the 1970s, assessed one by one against the code-derived-views variant — the most important disconfirming analysis in this knowledge base.",
      "tags": [
        "open-questions",
        "failure-modes",
        "disconfirming",
        "mda-critique"
      ],
      "links": [
        {
          "to": "kb-uml-mde-and-4gl",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "2e91e8b598bfbe5ddf4448828271e42eafd3ce3e3a30be08e916f084d895e92f"
    },
    {
      "id": "kb-uml-references",
      "path": "docs/knowledge/uml-mde-and-4gl/references.md",
      "title": "UML, MDE & 4GL — references, spec versions and survey data",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "OMG specification versions and dates read from omg.org, the empirical adoption studies with their sample sizes, the market figures, and the critiques with their exact quotations.",
      "tags": [
        "reference",
        "omg-specs",
        "versions",
        "surveys"
      ],
      "links": [
        {
          "to": "kb-uml-mde-and-4gl",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "d0766df860a86881a4836422edf8a47ab87b5c2c9b657c25e76a3f45da09e5b7"
    },
    {
      "id": "kb-uml-sota",
      "path": "docs/knowledge/uml-mde-and-4gl/state-of-the-art.md",
      "title": "UML, MDE & 4GL — state of the art",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "Where UML, the MDA stack, SysML v2, the Eclipse MDE tooling, generative design and the low-code successors of the 4GL actually stand today — with the spec dates that show the investment curve.",
      "tags": [
        "uml",
        "mda",
        "sysml",
        "emf",
        "xtext",
        "low-code",
        "dsl"
      ],
      "links": [
        {
          "to": "kb-uml-mde-and-4gl",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "4b363a183a5fb0b788891727a9ab9bb4dc42890e462f734532ec5054e6d81fe4"
    },
    {
      "id": "kb-uml-sources",
      "path": "docs/knowledge/uml-mde-and-4gl/sources.md",
      "title": "UML, MDE & 4GL — sources",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "The access-dated source list behind the UML/MDE/4GL knowledge base, keyed [S1]..[S21], distinguishing OMG primary specs from commentary and noting where evidence is absent.",
      "tags": [
        "sources",
        "citations",
        "omg"
      ],
      "links": [
        {
          "to": "kb-uml-mde-and-4gl",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "1fa02af1bdd314c93269bab55213440634a2cedd04077553c96c9aa663c95d49"
    },
    {
      "id": "knowledge-hub",
      "path": "docs/knowledge/index.md",
      "title": "AI-DE Domain Knowledge — index",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "The synthesis over ten sourced domain knowledge bases for AI-DE — an AI-native IDE built on a code knowledge graph — including the four findings that change the seed architecture and the ranked list of spikes that would settle the rest.",
      "tags": [
        "knowledge-base",
        "index",
        "ai-native-ide",
        "multi-agent",
        "modelling"
      ],
      "links": [
        {
          "to": "architecture",
          "rel": "relates-to"
        },
        {
          "to": "seed-ai-native-ide-sketch",
          "rel": "documents"
        },
        {
          "to": "seed-agent-coordination-spec",
          "rel": "documents"
        }
      ],
      "diagrams": [],
      "sourceSha256": "17b157095cd8b11b9e03d19a0a94d42b70b50edbe5f7916698574b3cd1a362db"
    }
  ],
  "surfaces": [
    {
      "id": "surface-audit-index",
      "path": "docs/audit/index.html",
      "title": "ai-de — Audit & Change Log",
      "kind": "audit",
      "description": "Browse the committed audit and change timeline.",
      "artifactId": "audit-log"
    }
  ],
  "graphSha256": "9434c672ec52b4c8ec893ca29b9b1cb09f3a237d175de3803d04be547a5a5c18"
};
