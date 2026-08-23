// Derived from artifact frontmatter by scripts/docs-graph.py — DO NOT hand-edit (frontmatter wins; see knowledge-visualization.md V2/V18).
window.DOCS_INDEX = {
  "schemaVersion": "docs-index/v2",
  "project": "AI-DE",
  "generated": "2026-08-23T20:23:58Z",
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
      "title": "AI-DE Current Architecture",
      "type": "architecture",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-02-19",
      "reviewSuggested": [],
      "summary": "Recovered current-state architecture for the AI-DE .NET 10 WPF starter, including its single runtime container, startup and binding path, build/test surface, and explicitly absent tiers.",
      "tags": [
        "architecture",
        "wpf"
      ],
      "links": [
        {
          "to": "glossary",
          "rel": "uses-term"
        },
        {
          "to": "decision-adoption-boundary",
          "rel": "depends-on"
        }
      ],
      "diagrams": [
        {
          "kind": "c4",
          "title": "C4 context",
          "mermaid": "C4Context\n  title AI-DE current system context\n  Person(localDeveloper, \"Local developer\", \"Provisional actor; product users are not yet specified\")\n  System(aiDe, \"AI-DE\", \"Local Windows desktop starter\")\n  System_Ext(windowsPlatform, \"Windows and .NET 10 WPF\", \"Runtime platform\")\n\n  Rel(localDeveloper, aiDe, \"Runs and inspects\")\n  Rel(aiDe, windowsPlatform, \"Uses\")"
        },
        {
          "kind": "c4",
          "title": "C4 container",
          "mermaid": "C4Container\n  title AI-DE current container view\n  Person(localDeveloper, \"Local developer\", \"Provisional actor\")\n  System_Boundary(aiDeBoundary, \"AI-DE\") {\n    Container(wpfApp, \"AiDe.App\", \"C#, .NET 10, WPF\", \"Renders the starter desktop window and immutable presentation data\")\n  }\n  System_Ext(windowsPlatform, \"Windows and .NET 10 WPF\", \"Runtime platform\")\n\n  Rel(localDeveloper, wpfApp, \"Runs\")\n  Rel(wpfApp, windowsPlatform, \"Uses\")"
        },
        {
          "kind": "sequence",
          "title": "Startup and rendering path",
          "mermaid": "sequenceDiagram\n  participant Runtime as .NET/WPF runtime\n  participant App as App.xaml\n  participant Window as MainWindow\n  participant ViewModel as MainWindowViewModel\n\n  Runtime->>App: Start application\n  App->>Window: Resolve StartupUri\n  Window->>Window: InitializeComponent()\n  Window->>ViewModel: Construct from XAML resources\n  ViewModel-->>Window: Provide immutable display values\n  Window-->>Runtime: Render bound title, heading, steps, and status"
        }
      ],
      "sourceSha256": "056bf6df254983e828ffd558d68387cedbbd77f2d0ea0713f3f33e31eacce071"
    },
    {
      "id": "decision-adoption-boundary",
      "path": "docs/notes/adoption-boundary.md",
      "title": "Adoption records current evidence without inventing product history",
      "type": "decision-note",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-02-19",
      "reviewSuggested": [],
      "summary": "Records the adoption boundary: document the current WPF starter and its known gaps, while deferring unrecorded product intent, designs, and proofs to future owning workflows.",
      "tags": [
        "adoption",
        "decision-note"
      ],
      "links": [],
      "diagrams": [],
      "sourceSha256": "65dd5b1b6ba38713ae58a8d87466391e2bc4237c0e2a9c5d7cb2418aeb910437"
    },
    {
      "id": "ai-forward-pack-adoption",
      "path": "docs/ai-forward-pack-adoption.md",
      "title": "AI-Forward Pack Adoption Plan",
      "type": "doc",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "Phased plan for turning the recovered AI-DE baseline into complete, evidence-linked product, design, proof, and documentation artifacts without fabricating history.",
      "tags": [
        "adoption",
        "roadmap"
      ],
      "links": [
        {
          "to": "architecture",
          "rel": "depends-on"
        },
        {
          "to": "proof-adoption",
          "rel": "tested-by"
        }
      ],
      "diagrams": [],
      "sourceSha256": "12ec242d689a8ccf75062de203eea144d05056e28c1b5fe9b8031fa44ccfd25c"
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
      "links": [
        {
          "to": "ai-forward-pack-adoption",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "2965f5ab6c345d66cbe20b8534a20fe1764e2b6e4964a34a79d4e055baf4e624"
    },
    {
      "id": "glossary",
      "path": "docs/knowledge/glossary.md",
      "title": "AI-DE Glossary",
      "type": "glossary",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "Governed definitions for the small set of terms shared across the AI-DE starter code and project documentation.",
      "tags": [
        "glossary",
        "wpf"
      ],
      "links": [],
      "diagrams": [],
      "sourceSha256": "f08290123e74bd2e39a96995e916718065a4cafb530ad7833f8c3eb7bb57ab94"
    },
    {
      "id": "project-documents",
      "path": "docs/project-documents.md",
      "title": "Project Documents",
      "type": "index",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "Map of content for AI-DE project-facing documentation, including root onboarding and legal files that remain outside the docs directory.",
      "tags": [
        "documentation",
        "navigation"
      ],
      "links": [
        {
          "to": "architecture",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "5e2e8306d6f24a9d1ea252d00c62ff68b3d044c90ca474f845a540c193bfa6ae"
    },
    {
      "id": "proof-adoption",
      "path": "docs/proof/adoption-proof.md",
      "title": "AI-DE Adoption Proof Pack",
      "type": "proof-pack",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-21",
      "reviewSuggested": [],
      "summary": "Claim-by-claim evidence that repository publication and the AI-Forward adoption baseline are persistent, graph-valid, buildable, browsable, and honestly scoped.",
      "tags": [
        "adoption",
        "proof"
      ],
      "links": [],
      "diagrams": [],
      "sourceSha256": "12b028c8e8d0bfaa75808c1256a07ff3e47056b386e81652540a57b536072137"
    }
  ],
  "surfaces": [
    {
      "id": "surface-audit-index",
      "path": "docs/audit/index.html",
      "title": "AI-DE — Audit & Change Log",
      "kind": "audit",
      "description": "Browse the committed audit and change timeline.",
      "artifactId": "audit-log"
    }
  ],
  "graphSha256": "49579ecfcb92e52ce53e8761b7097352ddabdd18974baa96830201a970b73426"
};
