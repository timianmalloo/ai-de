// Derived from artifact frontmatter by scripts/docs-graph.py — DO NOT hand-edit (frontmatter wins; see knowledge-visualization.md V2/V18).
window.DOCS_INDEX = {
  "schemaVersion": "docs-index/v2",
  "project": "AI-DE",
  "generated": "2026-08-23T20:07:30Z",
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
      "status": "in-review",
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
      "sourceSha256": "5786d2e05ec13a6d61c834f5a4177d16a579f2913c682982bdd0721804c279d7"
    },
    {
      "id": "decision-adoption-boundary",
      "path": "docs/notes/adoption-boundary.md",
      "title": "Adoption records current evidence without inventing product history",
      "type": "decision-note",
      "status": "draft",
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
      "sourceSha256": "80cfa5145f57415eac971c557b54a83915ad8c36ec74bc0cf38000e53e6e2f70"
    },
    {
      "id": "ai-forward-pack-adoption",
      "path": "docs/ai-forward-pack-adoption.md",
      "title": "AI-Forward Pack Adoption Plan",
      "type": "doc",
      "status": "draft",
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
      "sourceSha256": "2a57bd8378c2004fb122629239660549a66be81e6480812c7ec3ddcdc8ce056c"
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
      "status": "draft",
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
      "sourceSha256": "a12bf3ea643c5663bdf66418e61daa8f5c5bd28e9464054a3ba4307ed69f3555"
    },
    {
      "id": "project-documents",
      "path": "docs/project-documents.md",
      "title": "Project Documents",
      "type": "index",
      "status": "draft",
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
      "sourceSha256": "5d29df48bde88802d48d08fcd26684d4ff7220488aba5d41b40fd1b7b357f222"
    },
    {
      "id": "proof-adoption",
      "path": "docs/proof/adoption-proof.md",
      "title": "AI-DE Adoption Proof Pack",
      "type": "proof-pack",
      "status": "draft",
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
      "sourceSha256": "c09fe00673e94310a9cbe35d57acf4f42eefba2dca1cca79ed9c44540abcdbe0"
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
  "graphSha256": "bbc97ac131dc0f4c80071db52aff7c55b0d18311c991161709fdf697aa656706"
};
