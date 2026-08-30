// Derived from artifact frontmatter by scripts/docs-graph.py — DO NOT hand-edit (frontmatter wins; see knowledge-visualization.md V2/V18).
window.DOCS_INDEX = {
  "schemaVersion": "docs-index/v2",
  "project": "ai-de-facelift",
  "generated": "2026-08-30T19:43:16Z",
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
      "id": "adr-0001-derived-evidence-views",
      "path": "docs/adr/0001-derived-evidence-views.md",
      "title": "ADR-0001 — Use derived evidence views, not editable models",
      "type": "adr",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "0",
      "reviewBy": "2027-02-21",
      "reviewSuggested": [
        {
          "by": "architecture",
          "on": "2026-08-25",
          "reason": "Defined the AI-DE workspace daemon, fact-store, MCP, terminal, and vertical delivery architecture"
        },
        {
          "by": "spec-ai-native-ide",
          "on": "2026-08-26",
          "reason": "US-9 dockable workbench added; archetype corrected to Layout:MultiPanelWorkstation + Persistence:LocalDevice"
        }
      ],
      "summary": "AI-DE stores attributable evidence and generates architecture/model/flow views from it. Users may save view preferences but cannot edit a rendered view into source truth.",
      "tags": [
        "architecture",
        "provenance",
        "diagrams",
        "source-of-truth"
      ],
      "links": [
        {
          "to": "architecture",
          "rel": "implements"
        },
        {
          "to": "spec-ai-native-ide",
          "rel": "implements"
        },
        {
          "to": "knowledge-hub",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "5ec1ed24b419ad311f8d5d33935da93ddef1a0fdf503e9af7c198d0f580e9472"
    },
    {
      "id": "adr-0002-workspace-fact-store",
      "path": "docs/adr/0002-workspace-fact-store.md",
      "title": "ADR-0002 — Use SQLite dimensions and append-only facts per workspace",
      "type": "adr",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "0",
      "reviewBy": "2027-02-21",
      "reviewSuggested": [
        {
          "by": "architecture",
          "on": "2026-08-25",
          "reason": "Defined the AI-DE workspace daemon, fact-store, MCP, terminal, and vertical delivery architecture"
        },
        {
          "by": "spec-ai-native-ide",
          "on": "2026-08-26",
          "reason": "US-9 dockable workbench added; archetype corrected to Layout:MultiPanelWorkstation + Persistence:LocalDevice"
        }
      ],
      "summary": "Each workspace uses an embedded SQLite operational store with stable dimensions, append-only evidence/coordination/audit facts, and rebuildable current-state caches rather than an archived graph database dependency.",
      "tags": [
        "architecture",
        "sqlite",
        "facts",
        "dimensions",
        "provenance"
      ],
      "links": [
        {
          "to": "architecture",
          "rel": "implements"
        },
        {
          "to": "spec-ai-native-ide",
          "rel": "implements"
        },
        {
          "to": "kb-code-knowledge-graphs",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "84b2fcf0514bba14b9bed6d6015f7f33e275ba8c884422048006408f81cc2f72"
    },
    {
      "id": "adr-0003-workspace-daemon-boundary",
      "path": "docs/adr/0003-workspace-daemon-boundary.md",
      "title": "ADR-0003 — Run one local daemon per workspace",
      "type": "adr",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "0",
      "reviewBy": "2027-02-21",
      "reviewSuggested": [
        {
          "by": "architecture",
          "on": "2026-08-25",
          "reason": "Defined the AI-DE workspace daemon, fact-store, MCP, terminal, and vertical delivery architecture"
        },
        {
          "by": "spec-ai-native-ide",
          "on": "2026-08-26",
          "reason": "US-9 dockable workbench added; archetype corrected to Layout:MultiPanelWorkstation + Persistence:LocalDevice"
        }
      ],
      "summary": "A workspace-scoped local daemon owns durable facts, scheduling, query/projection, policy, and tool authorization so the WPF shell and agent sessions do not share an unbounded global state.",
      "tags": [
        "architecture",
        "daemon",
        "workspace",
        "isolation"
      ],
      "links": [
        {
          "to": "architecture",
          "rel": "implements"
        },
        {
          "to": "spec-ai-native-ide",
          "rel": "implements"
        }
      ],
      "diagrams": [],
      "sourceSha256": "ff95dc0733ac146e111dbc579135f54300a50cfb39123c2749e3c8c5fec02405"
    },
    {
      "id": "adr-0004-mcp-tool-boundary",
      "path": "docs/adr/0004-mcp-tool-boundary.md",
      "title": "ADR-0004 — Expose bounded, typed MCP tools with deterministic authorization",
      "type": "adr",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "0",
      "reviewBy": "2027-02-21",
      "reviewSuggested": [
        {
          "by": "architecture",
          "on": "2026-08-25",
          "reason": "Defined the AI-DE workspace daemon, fact-store, MCP, terminal, and vertical delivery architecture"
        },
        {
          "by": "spec-ai-native-ide",
          "on": "2026-08-26",
          "reason": "US-9 dockable workbench added; archetype corrected to Layout:MultiPanelWorkstation + Persistence:LocalDevice"
        }
      ],
      "summary": "The workspace daemon exposes bounded read and narrowly-authorized annotation tools over MCP; every request is self-contained, context-bound, audited, and protected from untrusted tool output and default HTTP-origin weaknesses.",
      "tags": [
        "architecture",
        "mcp",
        "tools",
        "security",
        "agents"
      ],
      "links": [
        {
          "to": "architecture",
          "rel": "implements"
        },
        {
          "to": "spec-ai-native-ide",
          "rel": "implements"
        },
        {
          "to": "kb-mcp-agent-integration",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "553372c817edd06466dc1e35419366b6d8069186181e87ebac5be9469bd52714"
    },
    {
      "id": "adr-0005-terminal-runtime-boundary",
      "path": "docs/adr/0005-terminal-runtime-boundary.md",
      "title": "ADR-0005 — Own ConPTY lifecycle behind a renderer-independent runtime",
      "type": "adr",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "0",
      "reviewBy": "2027-02-21",
      "reviewSuggested": [
        {
          "by": "architecture",
          "on": "2026-08-25",
          "reason": "Defined the AI-DE workspace daemon, fact-store, MCP, terminal, and vertical delivery architecture"
        },
        {
          "by": "spec-ai-native-ide",
          "on": "2026-08-26",
          "reason": "US-9 dockable workbench added; archetype corrected to Layout:MultiPanelWorkstation + Persistence:LocalDevice"
        }
      ],
      "summary": "Terminal process and ConPTY lifecycle belong to a stable runtime contract; WPF terminal controls and web renderers are replaceable views that may not own agent session state.",
      "tags": [
        "architecture",
        "terminal",
        "conpty",
        "sessions",
        "wpf"
      ],
      "links": [
        {
          "to": "architecture",
          "rel": "implements"
        },
        {
          "to": "spec-ai-native-ide",
          "rel": "implements"
        },
        {
          "to": "kb-ai-native-ide-shell",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "6859822872b83d6539c6848bffee2aff8abeb942afe761c4b27aecfc1c11b594"
    },
    {
      "id": "adr-0006-terminal-delivery-semantics",
      "path": "docs/adr/0006-terminal-delivery-semantics.md",
      "title": "ADR-0006 — Treat terminal prompt delivery as an at-most-once attempt",
      "type": "adr",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "0",
      "reviewBy": "2027-02-21",
      "reviewSuggested": [
        {
          "by": "architecture",
          "on": "2026-08-25",
          "reason": "Defined the AI-DE workspace daemon, fact-store, MCP, terminal, and vertical delivery architecture"
        },
        {
          "by": "spec-ai-native-ide",
          "on": "2026-08-26",
          "reason": "US-9 dockable workbench added; archetype corrected to Layout:MultiPanelWorkstation + Persistence:LocalDevice"
        }
      ],
      "summary": "Prompt transfer makes one at-most-once terminal-stream attempt because a terminal write and daemon receipt cannot share a transaction. Unknown delivery blocks automatic resend and requires explicit user confirmation.",
      "tags": [
        "architecture",
        "terminal",
        "prompts",
        "idempotency",
        "delivery"
      ],
      "links": [
        {
          "to": "architecture",
          "rel": "implements"
        },
        {
          "to": "spec-ai-native-ide",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "f50e9cfa1bb1fb03f32a5bc2b3056a2c1040213765e3ddf2297a71d477f882f1"
    },
    {
      "id": "adr-0007-agent-session-adapter",
      "path": "docs/adr/0007-agent-session-adapter.md",
      "title": "ADR-0007 — Separate terminal readiness from agent acceptance",
      "type": "adr",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "0",
      "reviewBy": "2027-02-21",
      "reviewSuggested": [
        {
          "by": "architecture",
          "on": "2026-08-25",
          "reason": "Defined the AI-DE workspace daemon, fact-store, MCP, terminal, and vertical delivery architecture"
        },
        {
          "by": "spec-ai-native-ide",
          "on": "2026-08-26",
          "reason": "US-9 dockable workbench added; archetype corrected to Layout:MultiPanelWorkstation + Persistence:LocalDevice"
        }
      ],
      "summary": "V1 reports only PTY/terminal readiness and paste acceptance. It does not claim an external coding agent accepted a prompt until a supported agent-side adapter provides an authenticated, versioned acknowledgement.",
      "tags": [
        "architecture",
        "agents",
        "terminal",
        "prompts",
        "contracts"
      ],
      "links": [
        {
          "to": "architecture",
          "rel": "implements"
        },
        {
          "to": "spec-ai-native-ide",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "cbb8970fd7460bda29361be8c62399ac6c8abd7e5f013028cd28d3e665f36c59"
    },
    {
      "id": "adr-0008-shell-host",
      "path": "docs/adr/0008-shell-host.md",
      "title": "ADR-0008 — WPF frame with embedded WebView2 as the shell host",
      "type": "adr",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "0",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [
        {
          "by": "architecture",
          "on": "2026-08-25",
          "reason": "Architecture v2 supersedes the 2026-08-25 draft: write-ahead dispatch, in-process-first daemon, MCP egress binding, committed spikes"
        },
        {
          "by": "spec-ai-native-ide",
          "on": "2026-08-26",
          "reason": "US-9 dockable workbench added; archetype corrected to Layout:MultiPanelWorkstation + Persistence:LocalDevice"
        }
      ],
      "summary": "Records the desktop shell-host decision the earlier draft left implicit: a WPF window frame with an embedded WebView2 for visual surfaces and a renderer-independent terminal runtime, with the Phase-2 renderer/airspace spike as the explicit reversal trigger.",
      "tags": [
        "architecture",
        "shell",
        "wpf",
        "webview2",
        "desktop"
      ],
      "links": [
        {
          "to": "architecture",
          "rel": "implements"
        },
        {
          "to": "spec-ai-native-ide",
          "rel": "implements"
        },
        {
          "to": "adr-0005-terminal-runtime-boundary",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "f1cf3a2bb6bcc4a130038db1405f1c3864196e2ca04911b6b4adfff7c2765421"
    },
    {
      "id": "adr-0009-in-process-first-daemon",
      "path": "docs/adr/0009-in-process-first-daemon.md",
      "title": "ADR-0009 — Run the authority core in-process in Phase 1; split to a daemon at Phase 2",
      "type": "adr",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "0",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [
        {
          "by": "architecture",
          "on": "2026-08-25",
          "reason": "Architecture v2 supersedes the 2026-08-25 draft: write-ahead dispatch, in-process-first daemon, MCP egress binding, committed spikes"
        }
      ],
      "summary": "Refines ADR-0003: the Workspace Authority Core is one logical boundary but runs in-process inside the shell in Phase 1, splitting to a separate per-workspace daemon process only at Phase 2 when the terminal runtime first needs process isolation. The Shell Bootstrap owns the process and upgrade lifecycle.",
      "tags": [
        "architecture",
        "daemon",
        "phasing",
        "simplicity",
        "lifecycle"
      ],
      "links": [
        {
          "to": "architecture",
          "rel": "implements"
        },
        {
          "to": "adr-0003-workspace-daemon-boundary",
          "rel": "relates-to"
        },
        {
          "to": "adr-0005-terminal-runtime-boundary",
          "rel": "relates-to"
        },
        {
          "to": "release-plan-ai-native-ide",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "b93e12696e706df378d3910127156fe982b5bc9cd7290022463b371984151806"
    },
    {
      "id": "adr-0010-two-phase-dispatch-receipt",
      "path": "docs/adr/0010-two-phase-dispatch-receipt.md",
      "title": "ADR-0010 — Write-ahead two-phase dispatch receipt for prompt delivery",
      "type": "adr",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "0",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [
        {
          "by": "architecture",
          "on": "2026-08-25",
          "reason": "Architecture v2 supersedes the 2026-08-25 draft: write-ahead dispatch, in-process-first daemon, MCP egress binding, committed spikes"
        }
      ],
      "summary": "Refines ADR-0006 with the mechanism that makes at-most-once terminal delivery true: a Pending delivery receipt is committed before the PTY write, the outcome is appended after, and core recovery sweeps any Pending receipt to DeliveryUnknown — so a crash in the write window cannot make a protocol-conformant retry re-deliver a prompt.",
      "tags": [
        "architecture",
        "prompts",
        "delivery",
        "idempotency",
        "crash-safety"
      ],
      "links": [
        {
          "to": "architecture",
          "rel": "implements"
        },
        {
          "to": "adr-0006-terminal-delivery-semantics",
          "rel": "relates-to"
        },
        {
          "to": "conceptual-model-ai-native-ide",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "820b6083655fc4d021500d7fcf17f37bd9ccbc19c8a62e17694146241fbee867"
    },
    {
      "id": "adr-0011-session-processing-class-egress",
      "path": "docs/adr/0011-session-processing-class-egress.md",
      "title": "ADR-0011 — Bind MCP tool authorization to the session processing class",
      "type": "adr",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "0",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [
        {
          "by": "architecture",
          "on": "2026-08-25",
          "reason": "Architecture v2 supersedes the 2026-08-25 draft: write-ahead dispatch, in-process-first daemon, MCP egress binding, committed spikes"
        }
      ],
      "summary": "Refines ADR-0004: MCP read and write authorization is bound to the target session's declared data-processing class from Phase 1, so an externally-processing agent cannot pull workspace facts via describe/find/impact and forward them to its provider. Closes the unanalyzed indirect-egress path the privacy review had not modelled.",
      "tags": [
        "architecture",
        "mcp",
        "privacy",
        "egress",
        "authorization"
      ],
      "links": [
        {
          "to": "architecture",
          "rel": "implements"
        },
        {
          "to": "adr-0004-mcp-tool-boundary",
          "rel": "relates-to"
        },
        {
          "to": "privacy-review-ai-native-ide",
          "rel": "implements"
        }
      ],
      "diagrams": [],
      "sourceSha256": "03bd4d120b4460bf57546de1aafb9dc42b47b2608af5cbca66cdd219190d272a"
    },
    {
      "id": "adr-0012-docking-shell-library",
      "path": "docs/adr/0012-docking-shell-library.md",
      "title": "ADR-0012 — Adopt AvalonDock for the workbench shell, with an owned accessibility layer",
      "type": "adr",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "0",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [
        {
          "by": "spec-ai-native-ide",
          "on": "2026-08-26",
          "reason": "US-9 dockable workbench added; archetype corrected to Layout:MultiPanelWorkstation + Persistence:LocalDevice"
        }
      ],
      "summary": "The dockable workbench is built on AvalonDock 5.0.0 (MS-PL, net10.0-windows), whose layout serialization is best-in-class — but which ships zero UI Automation peers and a mouse-only splitter. Adoption is conditional on an owned accessibility layer (command-driven layout operations including resize) and a versioned layout envelope, both specified here.",
      "tags": [
        "architecture",
        "ui",
        "docking",
        "accessibility",
        "wpf",
        "licence"
      ],
      "links": [
        {
          "to": "architecture",
          "rel": "implements"
        },
        {
          "to": "adr-0008-shell-host",
          "rel": "relates-to"
        },
        {
          "to": "spec-ai-native-ide",
          "rel": "implements"
        }
      ],
      "diagrams": [],
      "sourceSha256": "52761971b212e0d6feaa59f3d3290061fd324ea5d6e15b2c44b1563d286b8a09"
    },
    {
      "id": "adr-0013-layout-persistence-envelope",
      "path": "docs/adr/0013-layout-persistence-envelope.md",
      "title": "ADR-0013 — Persist the workbench layout in an owned versioned envelope, outside the fact store",
      "type": "adr",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "0",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "Workbench layouts are user preference, not evidence. They are stored per workspace beside the fact store — never inside it — wrapped in an owned {schemaVersion, appVersion, payload} envelope, and a layout that cannot be read degrades to the default arrangement while the original file is kept.",
      "tags": [
        "architecture",
        "ui",
        "layout",
        "persistence",
        "migration"
      ],
      "links": [
        {
          "to": "architecture",
          "rel": "implements"
        },
        {
          "to": "adr-0012-docking-shell-library",
          "rel": "relates-to"
        },
        {
          "to": "adr-0002-workspace-fact-store",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "c8e1bdbea455700c2b6f9ab34fc7dbef4fb2baa8ceca7ee3d0dff23f72498993"
    },
    {
      "id": "adr-0014-accessibility-posture",
      "path": "docs/adr/0014-accessibility-posture.md",
      "title": "ADR-0014 — Accessibility is best-effort, not a conformance target, and holds no veto",
      "type": "adr",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "0",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "The product owner has decided AI-DE is not optimising for accessibility. WCAG 2.2 AA is withdrawn as a conformance obligation and the UX & Accessibility lens no longer holds a hard veto. Existing accessibility work is retained because it is built and passing; it stops being a gate, and every artifact that asserted the obligation is corrected so the repository does not claim conformance it is not pursuing.",
      "tags": [
        "architecture",
        "accessibility",
        "scope",
        "governance",
        "wcag"
      ],
      "links": [
        {
          "to": "architecture",
          "rel": "implements"
        },
        {
          "to": "spec-ai-native-ide",
          "rel": "refines"
        },
        {
          "to": "adr-0012-docking-shell-library",
          "rel": "relates-to"
        },
        {
          "to": "adr-0005-terminal-runtime-boundary",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "c42124967dc05136d8ccee32a1d6a142fe9e8c73ad3343fac28aa30ed9ba6fbc"
    },
    {
      "id": "adr-0015-canvas-hosting-and-overlay-strategy",
      "path": "docs/adr/0015-canvas-hosting-and-overlay-strategy.md",
      "title": "ADR-0015 — Host the graph canvas in the windowed WebView2 and yield it to WPF by snapshot swap",
      "type": "adr",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "2",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "Spike S4 met ADR-0008's reversal trigger: the windowed WebView2 cannot be drawn over, and the composition control that can kills the process when its pane is floated. ADR-0008 is not reversed. The canvas keeps the windowed control, moves its own chrome into the web content, and hides behind a pixel-aligned still frame for the moments shell chrome must cross it.",
      "tags": [
        "architecture",
        "webview2",
        "wpf",
        "airspace",
        "canvas",
        "docking",
        "focus"
      ],
      "links": [
        {
          "to": "architecture",
          "rel": "implements"
        },
        {
          "to": "adr-0008-shell-host",
          "rel": "refines"
        },
        {
          "to": "adr-0012-docking-shell-library",
          "rel": "relates-to"
        },
        {
          "to": "design-phase-2-real-code-and-terminal",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "885e741f87ce2d8e0e7838669d2a4d259932b306a7154119187eff60f2de56d5"
    },
    {
      "id": "adr-0016-bounded-context-declaration",
      "path": "docs/adr/0016-bounded-context-declaration.md",
      "title": "ADR-0016 — Bounded contexts are declared in one reviewable file, never inferred",
      "type": "adr",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "3",
      "reviewBy": "2027-02-28",
      "reviewSuggested": [],
      "summary": "A bounded context is a modelling decision with no evidence in a repository, so it is declared in a committed file and validated against extracted symbols. Folder convention is rejected on measured grounds: the obvious candidate in a real repository has 31 folders that are UI features, not contexts.",
      "tags": [
        "architecture",
        "ddd",
        "bounded-context",
        "phase-3",
        "curation"
      ],
      "links": [
        {
          "to": "architecture",
          "rel": "implements"
        },
        {
          "to": "design-phase-3-architecture-data-infra",
          "rel": "refines"
        },
        {
          "to": "adr-0001-derived-evidence-views",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "1981ce647e3374d9e9985742d0b503122434c27855aff21b480b4a4bc0f188cc"
    },
    {
      "id": "adr-0017-primary-view-mode",
      "path": "docs/adr/0017-primary-view-mode.md",
      "title": "ADR-0017 — Full-window surfaces are a primary view mode (body-content swap), not a dock pane or a modal overlay",
      "type": "adr",
      "status": "proposed",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-02-28",
      "reviewSuggested": [],
      "summary": "A surface that needs the whole body (the Knowledge Explorer's graph+reader) is presented as a primary VIEW MODE the shell holds — Workbench | Explorer — realised as a body-content swap of the region the docking host occupies, with the activity rail as the mode selector. Rejects making it a dock pane (it would compete for space — the defect being fixed) and a modal overlay (the rail must persist and it is not dismiss-only). The non-active mode's state is retained, never rebuilt.",
      "tags": [
        "architecture",
        "ui-shell",
        "view-mode",
        "explorer",
        "docking",
        "accessibility"
      ],
      "links": [
        {
          "to": "architecture",
          "rel": "implements"
        },
        {
          "to": "spec-knowledge-explorer-mode",
          "rel": "refines"
        },
        {
          "to": "adr-0008-shell-host",
          "rel": "relates-to"
        },
        {
          "to": "adr-0012-docking-shell-library",
          "rel": "relates-to"
        },
        {
          "to": "adr-0013-layout-persistence-envelope",
          "rel": "relates-to"
        },
        {
          "to": "adr-0015-canvas-hosting-and-overlay-strategy",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "33fa8d47c7ff1d0044252c2961c4a9d81b97eabdb6728dfe1e4f3c23104b20dc"
    },
    {
      "id": "adr-0018-node-content-reader-contract",
      "path": "docs/adr/0018-node-content-reader-contract.md",
      "title": "ADR-0018 — The reader fetches node content on demand via a bounded Core query, not on the graph payload",
      "type": "adr",
      "status": "proposed",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-02-28",
      "reviewSuggested": [],
      "summary": "The Explorer's reader needs a selected node's CONTENT (source/markdown/html) and metadata, which the graph payload deliberately does not carry. It is fetched on demand for the one selected node via a new bounded Core query (a sibling of GraphOverview), not by fattening CanvasNode — because content on every node would blow the IPC transport bound (US-K12) for a value only the selected node needs.",
      "tags": [
        "architecture",
        "reader",
        "explorer",
        "ipc",
        "contract",
        "transport-bound"
      ],
      "links": [
        {
          "to": "architecture",
          "rel": "implements"
        },
        {
          "to": "spec-knowledge-explorer-mode",
          "rel": "refines"
        },
        {
          "to": "adr-0017-primary-view-mode",
          "rel": "relates-to"
        },
        {
          "to": "spec-knowledge-exploration",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "4717e8bf67ad30e341ea64f1e512187f2db334114e5f265f75b422b664f69fc2"
    },
    {
      "id": "architecture",
      "path": "docs/architecture.md",
      "title": "AI-DE Architecture",
      "type": "architecture",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "0",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [
        {
          "by": "spec-ai-native-ide",
          "on": "2026-08-26",
          "reason": "US-9 dockable workbench added; archetype corrected to Layout:MultiPanelWorkstation + Persistence:LocalDevice"
        }
      ],
      "summary": "Defines AI-DE as a WPF+WebView2 workspace shell over a per-workspace local authority core that builds provenance-labelled facts from repository artifacts, serves derived visual projections and session-class-governed MCP tools, delivers prompts under a write-ahead two-phase receipt, and keeps agent/model capability outside deterministic source truth. Supersedes the 2026-08-25 draft; resolves the council review's three hard and two soft vetoes.",
      "tags": [
        "architecture",
        "ai-native-ide",
        "wpf",
        "workspace-daemon",
        "code-knowledge-graph",
        "mcp"
      ],
      "links": [
        {
          "to": "spec-ai-native-ide",
          "rel": "implements"
        },
        {
          "to": "knowledge-hub",
          "rel": "depends-on"
        },
        {
          "to": "audit-log",
          "rel": "relates-to"
        },
        {
          "to": "privacy-review-ai-native-ide",
          "rel": "depends-on"
        },
        {
          "to": "conceptual-model-ai-native-ide",
          "rel": "depends-on"
        },
        {
          "to": "threat-model-ai-native-ide",
          "rel": "depends-on"
        },
        {
          "to": "release-plan-ai-native-ide",
          "rel": "depends-on"
        },
        {
          "to": "adr-0001-derived-evidence-views",
          "rel": "depends-on"
        },
        {
          "to": "adr-0002-workspace-fact-store",
          "rel": "depends-on"
        },
        {
          "to": "adr-0003-workspace-daemon-boundary",
          "rel": "depends-on"
        },
        {
          "to": "adr-0004-mcp-tool-boundary",
          "rel": "depends-on"
        },
        {
          "to": "adr-0005-terminal-runtime-boundary",
          "rel": "depends-on"
        },
        {
          "to": "adr-0006-terminal-delivery-semantics",
          "rel": "depends-on"
        },
        {
          "to": "adr-0007-agent-session-adapter",
          "rel": "depends-on"
        },
        {
          "to": "adr-0008-shell-host",
          "rel": "depends-on"
        },
        {
          "to": "adr-0009-in-process-first-daemon",
          "rel": "depends-on"
        },
        {
          "to": "adr-0010-two-phase-dispatch-receipt",
          "rel": "depends-on"
        },
        {
          "to": "adr-0011-session-processing-class-egress",
          "rel": "depends-on"
        },
        {
          "to": "adr-0012-docking-shell-library",
          "rel": "depends-on"
        },
        {
          "to": "adr-0013-layout-persistence-envelope",
          "rel": "depends-on"
        }
      ],
      "diagrams": [
        {
          "kind": "flowchart",
          "title": "System shape",
          "mermaid": "flowchart LR\n  User[Workspace operator]\n  Shell[WPF Shell + WebView2 host]\n  Boot[Shell Bootstrap / Updater]\n  Session[Terminal Session Runtime]\n  View[Visual Surface Host]\n  Core[Workspace Authority Core]\n  Registry[Workspace Registry]\n  Ingest[Ingestion Scheduler]\n  Freshness[Freshness Prober]\n  Extractors[Extractor Adapters]\n  Store[(SQLite Fact Store)]\n  Incidents[(Health Incident Sidecar)]\n  Projection[Query and Projection Service]\n  Audit[Audit Reader]\n  Coordination[Coordination Reader]\n  Mcp[MCP Tool Gateway]\n  Repos[Repositories and Worktrees]\n  Agents[Claude Code / Copilot CLI sessions]\n\n  User --> Shell\n  Boot -. supervises/upgrades .-> Core\n  Shell --> Session\n  Shell --> View\n  Shell <--> Core\n  Session <--> Agents\n  Session --> Core\n  View <--> Core\n  Repos --> Ingest\n  Repos --> Freshness\n  Freshness --> Ingest\n  Ingest --> Extractors\n  Extractors --> Core\n  Core --> Registry\n  Core --> Store\n  Core --> Incidents\n  Core --> Projection\n  Core --> Audit\n  Core --> Coordination\n  Mcp <--> Core\n  Agents <--> Mcp"
        }
      ],
      "sourceSha256": "931ebb681e1562e431f42ad350174d29618c3341a7e60293e87361e244196c80"
    },
    {
      "id": "note-20260826-council-review-ai-ide-arch",
      "path": "docs/notes/council-review-ai-ide-arch.md",
      "title": "Ten-persona adversary review of the 2026-08-25 AI-DE architecture found three hard and two soft vetoes",
      "type": "decision-note",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "0",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [
        {
          "by": "architecture",
          "on": "2026-08-25",
          "reason": "Architecture v2 supersedes the 2026-08-25 draft: write-ahead dispatch, in-process-first daemon, MCP egress binding, committed spikes"
        }
      ],
      "summary": "Records the council review that drove the v2 architecture: which persona raised what, the three hard vetoes and two soft vetoes, and where each is resolved. Blast radius: it is the input of record for every change the v2 architecture makes.",
      "tags": [
        "decision-note",
        "architecture",
        "review",
        "personas"
      ],
      "links": [
        {
          "to": "architecture",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "7e30522e21a7914ce2dee4a426e655482aa5b29adc9b1fbb80514a5e3d051c56"
    },
    {
      "id": "note-20260829-facelift-flat-to-soft-islands",
      "path": "docs/notes/note-20260829-facelift-flat-to-soft-islands.md",
      "title": "Facelift direction: evolve the workbench from strict-flat to soft islands, not a redesign",
      "type": "decision-note",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-02-27",
      "reviewSuggested": [],
      "summary": "The facelift evolves the existing DESIGN.md rather than replacing it — three facet moves (Depth Flat→SoftShadow, rounded.lg 8→10 + island 12, Nav +MenuBar) toward the JetBrains Islands register, with density and the WCAG/confidence floors held constant.",
      "tags": [
        "decision-note",
        "ui-design",
        "facelift",
        "design-language"
      ],
      "links": [
        {
          "to": "spec-app-facelift",
          "rel": "relates-to"
        },
        {
          "to": "mockup-app-facelift",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "746291198f9e8528308389186ed236cd4106249c3ee9e403a5c868e0977a0381"
    },
    {
      "id": "note-20260829-graph-experience-knowledge-scope",
      "path": "docs/notes/note-20260829-graph-experience-knowledge-scope.md",
      "title": "Graph-experience request split into two new bases; GraphRAG cost finding flagged for update",
      "type": "decision-note",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-02-27",
      "reviewSuggested": [],
      "summary": "The /collectknowledge run for the unified code+knowledge graph experience produced two new bases (graph-experience-and-visualization, editor-and-content-rendering-surfaces); GraphRAG/Obsidian/ Graphify overlap with existing code-knowledge-graphs and pack standards was reconciled, and finding #8 (GraphRAG 26-85x cost) was flagged for update by LazyGraphRAG.",
      "tags": [
        "decision-note",
        "collectknowledge",
        "scope",
        "graph",
        "graphrag",
        "rendering"
      ],
      "links": [
        {
          "to": "kb-graph-experience-and-visualization",
          "rel": "relates-to"
        },
        {
          "to": "kb-editor-and-content-rendering-surfaces",
          "rel": "relates-to"
        },
        {
          "to": "kb-code-knowledge-graphs",
          "rel": "relates-to"
        },
        {
          "to": "knowledge-hub",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "892e2274dcebfc123ff9f21bb8459ca475fb2e99f6ca2e6607b8e7b5cd5abf3f"
    },
    {
      "id": "note-20260829-wpf-styling-knowledge-scope",
      "path": "docs/notes/note-20260829-wpf-styling-knowledge-scope.md",
      "title": "WPF-styling knowledge request split into two new bases; diagram/UML/ERM cross-referenced, not duplicated",
      "type": "decision-note",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-02-27",
      "reviewSuggested": [],
      "summary": "The /collectknowledge run for \"modern soft WPF styling + widget libraries + diagram/UML/ERM/test dashboards\" produced two new bases (wpf-modern-ui-styling, operational-and-test-dashboards) and reconciled the already-covered diagram/UML/ERM asks by cross-reference rather than duplication.",
      "tags": [
        "decision-note",
        "collectknowledge",
        "scope",
        "wpf",
        "dashboards"
      ],
      "links": [
        {
          "to": "kb-wpf-modern-ui-styling",
          "rel": "relates-to"
        },
        {
          "to": "kb-operational-and-test-dashboards",
          "rel": "relates-to"
        },
        {
          "to": "knowledge-hub",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "29fd3d867b020629533f39b66ac24a4f2cab17f15302933a007d557bcb777817"
    },
    {
      "id": "note-20260830-sub-scope-incrementality",
      "path": "docs/notes/note-20260830-sub-scope-incrementality.md",
      "title": "Sub-scope incrementality: a call to make before any code, with the measurement that motivates it",
      "type": "decision-note",
      "status": "resolved",
      "owner": "@timianmalloo",
      "phase": "phase-3",
      "reviewBy": "2027-02-28",
      "reviewSuggested": [],
      "summary": "Re-indexing a changed C# scope re-walks every type in it, measured at 590ms of an 809ms walk on a real repository. RESOLVED 2026-08-30, and the answer is not to build it: there is no automatic re-index — no FileSystemWatcher exists — so the cost is paid deliberately by a user pressing a button, and breaking the per-scope snapshot's atomicity to shorten it is a poor trade at that trigger. The note stays for the trigger it names: re-index on save.",
      "tags": [
        "decision-note",
        "extraction",
        "performance",
        "store",
        "incremental",
        "measurement"
      ],
      "links": [
        {
          "to": "adr-0002-workspace-fact-store",
          "rel": "relates-to"
        },
        {
          "to": "adr-0001-derived-evidence-views",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "2c540d97f9b2ab94ec8a6d1f9cda36b7c05813282ad30cfe75f7b3e391e48ffd"
    },
    {
      "id": "note-ai-native-ide-architecture-review-depth",
      "path": "docs/notes/ai-native-ide-architecture-review-depth.md",
      "title": "Decision note — AI-native IDE architecture review depth",
      "type": "decision-note",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "0",
      "reviewBy": "2027-02-21",
      "reviewSuggested": [
        {
          "by": "architecture",
          "on": "2026-08-25",
          "reason": "Defined the AI-DE workspace daemon, fact-store, MCP, terminal, and vertical delivery architecture"
        }
      ],
      "summary": "The architecture review exceeded its two-pass plan cap because independent hard-veto findings exposed missing storage, delivery, trust, privacy, and release contracts. The cap was treated as a defect signal; the contracts were completed rather than the gate being reduced.",
      "tags": [
        "architecture",
        "review",
        "execution-graph",
        "phase-1"
      ],
      "links": [
        {
          "to": "architecture",
          "rel": "relates-to"
        },
        {
          "to": "plan-ai-native-ide-architecture",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "db0a5c6dd8132f0ee57a016ff459be8a1365c4f61c9288c4c82bebbe951c2b99"
    },
    {
      "id": "note-ai-native-ide-specification-framing",
      "path": "docs/notes/ai-native-ide-specification-framing.md",
      "title": "Decision note — AI-native IDE specification framing",
      "type": "decision-note",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "0",
      "reviewBy": "2027-02-20",
      "reviewSuggested": [
        {
          "by": "spec-ai-native-ide",
          "on": "2026-08-26",
          "reason": "US-9 dockable workbench added; archetype corrected to Layout:MultiPanelWorkstation + Persistence:LocalDevice"
        }
      ],
      "summary": "Keeps the AI-native IDE specification technology-neutral while adopting code-derived views as its source-of-truth boundary and B1 Keyboard-Velocity as the workspace-shell interaction archetype.",
      "tags": [
        "ai-native-ide",
        "derived-views",
        "ui-archetype",
        "specification"
      ],
      "links": [
        {
          "to": "spec-ai-native-ide",
          "rel": "relates-to"
        },
        {
          "to": "knowledge-hub",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "7ea5df0924025831925b7064e9769adbb912fc28e69dc6a09ed91540e8cfdd8a"
    },
    {
      "id": "note-avalondock-tab-styling",
      "path": "docs/notes/avalondock-tab-styling-decision.md",
      "title": "Decision — AvalonDock document-tab accent & corner styling",
      "type": "decision-note",
      "status": "accepted",
      "owner": "@copilot-design",
      "phase": "facelift",
      "reviewBy": "2026-11-27",
      "reviewSuggested": [],
      "summary": "Records the deliberate decision NOT to retokenize the AvalonDock VS2013 dark theme's document-tab accent hue or round its tab corners, with the runtime evidence that made that a high-risk/low-value change, and the IDE-convention rationale for squared tabs.",
      "tags": [
        "wpf",
        "avalondock",
        "theming",
        "facelift",
        "deviation"
      ],
      "links": [
        {
          "to": "spec-app-facelift",
          "rel": "relates-to"
        },
        {
          "to": "session-contracts",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "a6b72bd93885391d4e1602dc1e4df36a57d76d3c0f9d90815446ebd1581e4cea"
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
      "id": "note-terminal-customization-persistence",
      "path": "docs/notes/terminal-customization-persistence.md",
      "title": "Decision — terminal customization persistence & busy-close",
      "type": "decision-note",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-02-27",
      "reviewSuggested": [],
      "summary": "Resolves the flagged unknowns from spec-terminal-sessions. Persistence-while-open is achieved for free by the DC-029 reconcile fix (the surface instance survives re-renders, so its display name, colour scheme and tab colour persist). Cross-restart persistence and busy-close confirmation are deferred as documented follow-ups.",
      "tags": [
        "terminal",
        "customization",
        "persistence",
        "decision"
      ],
      "links": [
        {
          "to": "spec-terminal-sessions",
          "rel": "relates-to"
        },
        {
          "to": "inv-0002-terminal-rebuild-kills-sessions",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "450714ae4b68f3c3370e2d7df000cd7003531c5765e78a40388826127d03872e"
    },
    {
      "id": "conceptual-model-ai-native-ide",
      "path": "docs/design/conceptual-model.md",
      "title": "AI-DE conceptual domain model",
      "type": "design",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "0",
      "reviewBy": "2027-02-21",
      "reviewSuggested": [
        {
          "by": "architecture",
          "on": "2026-08-25",
          "reason": "Defined the AI-DE workspace daemon, fact-store, MCP, terminal, and vertical delivery architecture"
        },
        {
          "by": "spec-ai-native-ide",
          "on": "2026-08-26",
          "reason": "US-9 dockable workbench added; archetype corrected to Layout:MultiPanelWorkstation + Persistence:LocalDevice"
        }
      ],
      "summary": "Defines the bounded contexts, aggregate invariants, fact grains, history rules, and identity-only relationships used by the AI-DE workspace fact store.",
      "tags": [
        "ddd",
        "domain-model",
        "facts",
        "workspace",
        "agent-coordination"
      ],
      "links": [
        {
          "to": "architecture",
          "rel": "refines"
        },
        {
          "to": "spec-ai-native-ide",
          "rel": "implements"
        },
        {
          "to": "adr-0002-workspace-fact-store",
          "rel": "relates-to"
        }
      ],
      "diagrams": [
        {
          "kind": "class",
          "title": "Aggregates and invariants",
          "mermaid": "classDiagram\n  class WorkspaceRegistry {\n    +WorkspaceId\n    +WorkspaceEpoch\n    +RepositoryMembership\n    +WorktreeMembership\n    invariant one canonical membership per opened identity\n  }\n  class ScopeSnapshot {\n    +ScopeId\n    +DesiredGeneration\n    +CommittedGeneration\n    invariant commit only current desired generation\n  }\n  class RelationshipClaim {\n    +ClaimId\n    invariant one or more attributable assertions\n  }\n  class PromptDraft {\n    +DraftId\n    invariant immutable revision and command binding\n  }\n  class AgentSession {\n    +SessionId\n    +Generation\n    invariant one active worktree reference\n  }\n  class WorkItem {\n    +WorkItemId\n    invariant intent differs from assessment\n  }\n  WorkspaceRegistry --> ScopeSnapshot : contains by identity\n  ScopeSnapshot --> RelationshipClaim : selects assertions\n  PromptDraft --> AgentSession : targets by identity\n  WorkItem --> AgentSession : associates by identity"
        }
      ],
      "sourceSha256": "97d517658252e65e6cf51a7392d73986c9eb96aa4d78d91569aaeeead093e21e"
    },
    {
      "id": "design-knowledge-explorer-mode",
      "path": "docs/design/knowledge-explorer-mode.md",
      "title": "Knowledge Explorer mode — component design (Phase 1 walking skeleton)",
      "type": "design",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-02-28",
      "reviewSuggested": [],
      "summary": "Component design for the Phase-1 walking skeleton of the full-window Explorer mode: the ShellViewMode swap (WorkbenchHost.Content toggles Manager↔ExplorerSurface, Shell held so the workbench and its live ConPTY/WebView2 children hide-not-destroy), a dedicated CanvasSurface in Explorer (not reparented), a new CanvasSurface.NodeSelected seam the reader follows, and a NodeReaderView stub (metadata + walkable edges; content deferred to ADR-0018 Phase 2). Resolves the mechanism the ADRs deferred, with a red-first test plan whose key control is \"a live terminal survives an Explorer round-trip\".",
      "tags": [
        "explorer",
        "view-mode",
        "reader",
        "wpf",
        "design",
        "phase-1"
      ],
      "links": [
        {
          "to": "spec-knowledge-explorer-mode",
          "rel": "implements"
        },
        {
          "to": "adr-0017-primary-view-mode",
          "rel": "refines"
        },
        {
          "to": "adr-0018-node-content-reader-contract",
          "rel": "refines"
        },
        {
          "to": "mockup-knowledge-explorer-mode",
          "rel": "relates-to"
        }
      ],
      "diagrams": [
        {
          "kind": "flowchart",
          "title": "Component & data flow",
          "mermaid": "flowchart LR\n  Rail[Explore rail item] -->|Toggle| SMC[ShellModeController]\n  SMC -->|Workbench| WH[WorkbenchHost.Content = Shell.Manager]\n  SMC -->|Explorer| EX[WorkbenchHost.Content = ExplorerSurface]\n  EX --> G[CanvasSurface 'explorer-graph']\n  EX --> R[NodeReaderView]\n  G -->|NodeSelected CanvasNodeRef| R\n  R -->|activate edge -> RefreshAsync target| G\n  G -. GraphSource .-> VM[CanvasGraphViewModel over IWorkspaceQueries]"
        }
      ],
      "sourceSha256": "3062e5e5fb2e9cf22694fb55e8a48acad69099b888ea7de04b6800cc45f82359"
    },
    {
      "id": "design-phase-1-walking-skeleton",
      "path": "docs/design/phase-1-walking-skeleton.md",
      "title": "Phase 1 walking skeleton — detailed design",
      "type": "design",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "1",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [
        {
          "by": "spec-ai-native-ide",
          "on": "2026-08-26",
          "reason": "US-9 dockable workbench added; archetype corrected to Layout:MultiPanelWorkstation + Persistence:LocalDevice"
        }
      ],
      "summary": "The implementable blueprint for AI-DE's Phase-1 walking skeleton: the SQLite fact schema and its enforced immutability control, the in-process authority core with command receipts and the write-ahead two-phase dispatch, the bounded describe/impact/knowledge projections, the stdio MCP gateway with processing-class egress binding, the health sidecar and freshness prober, and the accessible evidence/provenance pane.",
      "tags": [
        "design",
        "phase-1",
        "walking-skeleton",
        "fact-store",
        "mcp",
        "dispatch",
        "projections"
      ],
      "links": [
        {
          "to": "architecture",
          "rel": "implements"
        },
        {
          "to": "spec-ai-native-ide",
          "rel": "implements"
        },
        {
          "to": "conceptual-model-ai-native-ide",
          "rel": "refines"
        },
        {
          "to": "adr-0002-workspace-fact-store",
          "rel": "depends-on"
        },
        {
          "to": "adr-0009-in-process-first-daemon",
          "rel": "depends-on"
        },
        {
          "to": "adr-0010-two-phase-dispatch-receipt",
          "rel": "depends-on"
        },
        {
          "to": "adr-0011-session-processing-class-egress",
          "rel": "depends-on"
        },
        {
          "to": "threat-model-ai-native-ide",
          "rel": "depends-on"
        },
        {
          "to": "privacy-review-ai-native-ide",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "86cc87c1bf8f24df9729ad0af66bd0a0351ffb39fdccb777319e7683eed5cdd5"
    },
    {
      "id": "design-phase-1b-workbench",
      "path": "docs/design/phase-1b-workbench.md",
      "title": "Phase 1b workbench shell — detailed design",
      "type": "design",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "1b",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "The implementable blueprint for the dockable workbench: an owned, headless layout model (tree → stack → surface) that both the pointer and the keyboard mutate through one command set, an AvalonDock adapter that renders it, and a versioned envelope that persists it.",
      "tags": [
        "design",
        "phase-1b",
        "workbench",
        "docking",
        "layout",
        "accessibility"
      ],
      "links": [
        {
          "to": "architecture",
          "rel": "implements"
        },
        {
          "to": "spec-ai-native-ide",
          "rel": "implements"
        },
        {
          "to": "adr-0012-docking-shell-library",
          "rel": "depends-on"
        },
        {
          "to": "adr-0013-layout-persistence-envelope",
          "rel": "depends-on"
        },
        {
          "to": "mockup-workbench",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "0bf355c353ee96606afda54ba48530644f023c64758729f70f53da3ad7f3e7f7"
    },
    {
      "id": "design-phase-2-real-code-and-terminal",
      "path": "docs/design/phase-2-real-code-and-terminal.md",
      "title": "Phase 2 — real code, terminal, and process split: detailed design",
      "type": "design",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "2",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "The blueprint for Phase 2's three interlocking components — a real Roslyn extractor, a ConPTY terminal runtime, and the in-process-to-daemon split with its IPC auth and upgrade path. Surfaces two contract gaps in seams Phase 1 declared substitutable, and gates implementation behind four named spikes.",
      "tags": [
        "design",
        "phase-2",
        "roslyn",
        "conpty",
        "process-split",
        "ipc",
        "upgrade"
      ],
      "links": [
        {
          "to": "architecture",
          "rel": "implements"
        },
        {
          "to": "spec-ai-native-ide",
          "rel": "implements"
        },
        {
          "to": "adr-0005-terminal-runtime-boundary",
          "rel": "depends-on"
        },
        {
          "to": "adr-0009-in-process-first-daemon",
          "rel": "depends-on"
        },
        {
          "to": "adr-0007-agent-session-adapter",
          "rel": "depends-on"
        },
        {
          "to": "threat-model-ai-native-ide",
          "rel": "depends-on"
        },
        {
          "to": "design-phase-1-walking-skeleton",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "7bc863996e870a105ef018e1946ded1589b22b4b50702c83b94a29477296bbef"
    },
    {
      "id": "design-phase-3-architecture-data-infra",
      "path": "docs/design/phase-3-architecture-data-infra.md",
      "title": "Design: Phase 3 — architecture, data and infrastructure joins",
      "type": "design",
      "status": "proposed",
      "owner": "@timianmalloo",
      "phase": "3",
      "reviewBy": "2027-02-28",
      "reviewSuggested": [],
      "summary": "Phase 3 joins C# evidence to infrastructure and schema evidence. Grounded in a real repository rather than the phase plan: its schema is EF Core migrations, not DDL files, so the planned \"DDL parser\" would have found nothing. Three components, one of which the phase plan did not have.",
      "tags": [
        "design",
        "phase-3",
        "bicep",
        "ddl",
        "domain",
        "joins"
      ],
      "links": [
        {
          "to": "architecture",
          "rel": "implements"
        },
        {
          "to": "design-phase-2-real-code-and-terminal",
          "rel": "refines"
        },
        {
          "to": "review-phase-2-exit",
          "rel": "relates-to"
        },
        {
          "to": "adr-0001-derived-evidence-views",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "4fc0b16537275ee9743dcadefb508ce94a027e92d70521b041d15616aa6cdeb4"
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
      "id": "defect-classes",
      "path": "docs/lessons/defect-classes.md",
      "title": "Defect-class register",
      "type": "doc",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-24",
      "reviewSuggested": [],
      "summary": "The project's register of defect classes — the recurring shapes of things that go wrong here, what each one survives, and the control that now fails when the shape recurs. Seeded from the ten-persona architecture review and the Phase-1 build.",
      "tags": [
        "lessons",
        "defect-classes",
        "continuous-improvement"
      ],
      "links": [
        {
          "to": "architecture",
          "rel": "relates-to"
        },
        {
          "to": "design-phase-1-walking-skeleton",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "c600df455034c22a8659638dc438fdcca6ab3ce707242dab785d5ef5b06f5cac"
    },
    {
      "id": "domain-experts",
      "path": "docs/domain-experts.md",
      "title": "AI-DE Domain Experts — the project's subject-matter lenses",
      "type": "doc",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-02-27",
      "reviewSuggested": [],
      "summary": "The three subject-matter expert lenses added to AI-DE's persona swarm — knowledge-graph visualization/UX, modern WPF styling, and UML/ERM modelling — each with its lens, seam against the general personas, veto, and grounding bases, plus the candidates the gate rejected.",
      "tags": [
        "personas",
        "domain-experts",
        "roster",
        "graph-visualization",
        "wpf",
        "uml",
        "erm"
      ],
      "links": [
        {
          "to": "knowledge-hub",
          "rel": "relates-to"
        },
        {
          "to": "kb-graph-experience-and-visualization",
          "rel": "documents"
        },
        {
          "to": "kb-wpf-modern-ui-styling",
          "rel": "documents"
        },
        {
          "to": "kb-uml-mde-and-4gl",
          "rel": "documents"
        },
        {
          "to": "kb-domain-modeling-and-erm",
          "rel": "documents"
        }
      ],
      "diagrams": [],
      "sourceSha256": "6168931fd44d94873de6f714eef265ce58b2f58cb01a70458db388ec1475e0b5"
    },
    {
      "id": "inv-0001-agent-terminal-environment",
      "path": "docs/investigations/INV-0001-agent-terminals-lack-the-users-environment.md",
      "title": "INV-0001 — Agent terminals lack the user's PATH and profile",
      "type": "doc",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-29",
      "reviewSuggested": [],
      "summary": "A reported loss of the user's PATH and profile in agent terminals. The verified cause is not in AI-DE: the machine's PATH is 22,297 characters and cmd.exe silently drops a variable that large, so every .cmd shim starts with an empty PATH — inside and outside the product. What was ours is that the terminal rendered the broken environment as a healthy one.",
      "tags": [
        "investigation",
        "terminal",
        "environment",
        "rca"
      ],
      "links": [
        {
          "to": "architecture",
          "rel": "relates-to"
        },
        {
          "to": "session-contracts",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "5ce843ed61cd48e6f0fa4875bbf9c221c2dd38e799ff2e96b773424f91be118c"
    },
    {
      "id": "inv-0002-terminal-rebuild-kills-sessions",
      "path": "docs/investigations/INV-0002-opening-a-terminal-kills-the-others.md",
      "title": "INV-0002 — Opening a second terminal kills the running one",
      "type": "doc",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-12-13",
      "reviewSuggested": [],
      "summary": "Opening a second terminal terminated the first one — the running Copilot session was lost. The verified cause is that WorkbenchAdapter.Render() rebuilds the entire AvalonDock layout on every mutation, constructing a brand-new TerminalSurface (and a fresh ConPTY child) for surfaces that did not change. Because each ConPTY child runs inside a kill-on-close job, disposing the replaced surface terminates the live process. Any layout mutation — not just opening a terminal — destroys every live terminal. Secondary: the only create command is \"New agent terminal\", which names the tab after the first agent on PATH (\"claude\"), so a plain terminal is mislabelled and no plain-terminal action exists. Report only — no code changed.",
      "tags": [
        "investigation",
        "terminal",
        "workbench",
        "adapter",
        "render",
        "rca"
      ],
      "links": [
        {
          "to": "architecture",
          "rel": "relates-to"
        },
        {
          "to": "session-contracts",
          "rel": "relates-to"
        },
        {
          "to": "inv-0001-agent-terminal-environment",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "9e22e926fdabc542ccafb5f87d2f99f870e512bc2516d4f2656a7293a90b2033"
    },
    {
      "id": "inv-0003-graph-exceeds-ipc-frame-cap",
      "path": "docs/investigations/INV-0003-graph-exceeds-ipc-frame-cap.md",
      "title": "INV-0003 — Graph load fails with ipc.transport_closed on a large repo",
      "type": "doc",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-12-13",
      "reviewSuggested": [],
      "summary": "Opening TheTerrace as a workspace shows \"The graph could not be loaded: ipc.transport_closed: the daemon closed the connection without responding.\" Verified cause: the whole-graph response for a real repo (~2,813 nodes / 8,602 edges) exceeds the IPC frame cap (IpcFraming.MaxFrameBytes = 1 MiB), so IpcFraming.WriteAsync throws ArgumentException in the daemon's response path; the serve loop catches IOException and OperationCanceledException but NOT that ArgumentException, so it propagates, the connection closes without a response, and the client reports transport_closed. This is the IpcFraming `simplify:` marker's upgrade trigger firing. Core-owned (IPC / daemon / graph projection). Report only — handed off to the Core session.",
      "tags": [
        "investigation",
        "graph",
        "ipc",
        "daemon",
        "transport",
        "rca",
        "core"
      ],
      "links": [
        {
          "to": "architecture",
          "rel": "relates-to"
        },
        {
          "to": "session-contracts",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "9639568f02c25923e0a708cf46869fa2d1a2a68b844949f035afa761f5f79498"
    },
    {
      "id": "lens-code-doc-join",
      "path": "docs/lenses/code-doc-join.md",
      "title": "Lens - code/doc join",
      "type": "doc",
      "status": "accepted",
      "owner": "@maintainers",
      "phase": "",
      "reviewBy": "",
      "reviewSuggested": [],
      "summary": "Derived join between the documentation graph (intent) and the Graphify code graph (reality): documentation referencing code that does not exist, and the most connected code symbols no artifact governs. A prompt, never a gate.",
      "tags": [
        "lens",
        "graphify",
        "code-graph",
        "traceability"
      ],
      "links": [
        {
          "to": "lens-graph-structure",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "c153dca141e06d0710ac8055ea23f9d01193fa4016cd673e613ae9c0f100e27e"
    },
    {
      "id": "lens-graph-health",
      "path": "docs/lenses/graph-health.md",
      "title": "Lens - graph health",
      "type": "doc",
      "status": "accepted",
      "owner": "@maintainers",
      "phase": "",
      "reviewBy": "",
      "reviewSuggested": [],
      "summary": "A read-time Dataview lens over the knowledge graph's health - stale artifacts, missing owners, missing freshness SLAs, and review-suggested flags. Derived, never authoritative.",
      "tags": [
        "lens",
        "obsidian",
        "dataview",
        "graph-health"
      ],
      "links": [
        {
          "to": "lens-graph-structure",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "fa610a3c05e3c4e58a6c6a0eb456ab484d2c85081d59c4a48b665035c8100a99"
    },
    {
      "id": "lens-graph-insight",
      "path": "docs/lenses/graph-insight.md",
      "title": "Lens - graph insight (computed)",
      "type": "doc",
      "status": "accepted",
      "owner": "@maintainers",
      "phase": "",
      "reviewBy": "",
      "reviewSuggested": [],
      "summary": "Computed structural analysis of the knowledge graph - hubs, bridges, components, orphans and structural gaps. Regenerate with obsidian-setup.py --analyze --write. Derived, never authoritative.",
      "tags": [
        "lens",
        "graph-analysis",
        "computed"
      ],
      "links": [
        {
          "to": "lens-graph-structure",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "14837edcddf2975dbc09f6151f3daaaa3e01a5ddef15b00d7be8a8281dcdd746"
    },
    {
      "id": "lens-graph-structure",
      "path": "docs/lenses/graph-structure.md",
      "title": "Lens - graph structure",
      "type": "doc",
      "status": "accepted",
      "owner": "@maintainers",
      "phase": "",
      "reviewBy": "",
      "reviewSuggested": [],
      "summary": "A read-time lens over the shape of the knowledge graph - artifacts by type and status, and the traceability chains (spec to design to proof). Derived, never authoritative.",
      "tags": [
        "lens",
        "obsidian",
        "dataview",
        "structure"
      ],
      "links": [
        {
          "to": "lens-graph-health",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "5bf8471b858442580beb4f227c56ae44fb370b0ff6185b66ba14d4d0da90ae07"
    },
    {
      "id": "mockup-activity-rail",
      "path": "docs/mockups/activity-rail.md",
      "title": "Activity rail — icon-only elevate (mockup)",
      "type": "doc",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-02-27",
      "reviewSuggested": [],
      "summary": "Self-contained mockup for the icon-only activity rail that replaces the clipped icon+label rail (\"Coordinate\" -> \"ordina\" at 56px). Renders rest/hover/focus/active states, tooltip, before/after, theme and reduced-motion via the review harness, with a token-layer contrast readout. The change is landed in src/AiDe.App/MainWindow.xaml.",
      "tags": [
        "mockup",
        "ui-design",
        "activity-rail",
        "navigation",
        "icons",
        "tooltip",
        "wcag"
      ],
      "links": [
        {
          "to": "spec-app-facelift",
          "rel": "relates-to"
        },
        {
          "to": "review-ui-activity-rail",
          "rel": "documents"
        }
      ],
      "diagrams": [],
      "sourceSha256": "c582c368ceb947e1c9a7582e2eafa5fbfdd58f21bb4b41a7f588701b3e18a4a9"
    },
    {
      "id": "mockup-app-facelift",
      "path": "docs/mockups/app-facelift.md",
      "title": "App Facelift — soft-islands shell (mockup)",
      "type": "doc",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-02-27",
      "reviewSuggested": [],
      "summary": "Self-contained, dependency-free mockup of the facelift shell — soft rounded island panes with resting elevation, a discoverable menu bar + icon system, a focused-pane indicator, and a review harness (theme · motion · density · state · focus). The .html is data; this .md is its graph node.",
      "tags": [
        "mockup",
        "facelift",
        "soft-islands",
        "menu",
        "icons"
      ],
      "links": [
        {
          "to": "spec-app-facelift",
          "rel": "documents"
        }
      ],
      "diagrams": [],
      "sourceSha256": "98851314dd309675045d8853c60fd958a9af98fc61c65aafa880abd30f7e5d7e"
    },
    {
      "id": "mockup-context-map-join",
      "path": "docs/mockups/context-map-join.md",
      "title": "Context Map & Join surfaces — Core→Design §4a rendering (mockup)",
      "type": "doc",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-02-27",
      "reviewSuggested": [],
      "summary": "Self-contained mockup rendering the Core session's ContextMapView and JoinResult view models, demonstrating the three accepted §4a requests — bounded-read \"≥ N (capped)\" counts, the dominant crossing class promoted out of the grey suffix, and the IsDeclared==false first-run empty state.",
      "tags": [
        "mockup",
        "context-map",
        "join",
        "evidence-shortfall",
        "collaboration"
      ],
      "links": [
        {
          "to": "session-contracts",
          "rel": "documents"
        },
        {
          "to": "spec-knowledge-exploration",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "019b6dfead9d172ec258bf9ada3019edcd311fc3601c556dc544132abe0ca999"
    },
    {
      "id": "mockup-facelift-elevate",
      "path": "docs/mockups/facelift-elevate.md",
      "title": "Facelift elevate proposals — visualization",
      "type": "doc",
      "status": "draft",
      "owner": "@copilot-design",
      "phase": "facelift",
      "reviewBy": "2026-11-29",
      "reviewSuggested": [],
      "summary": "A self-contained visualization of the four highest-leverage /ui-design elevate proposals — an icon'd clean menu (no goofy block), an icon'd activity rail with labels + active state, tabs with a type-glyph, and Wayfinder empty/loading/error states — with the standard review harness.",
      "tags": [
        "ui-design",
        "mockup",
        "facelift",
        "icons",
        "wayfinder",
        "elevate"
      ],
      "links": [
        {
          "to": "review-ui-facelift",
          "rel": "documents"
        },
        {
          "to": "spec-app-facelift",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "972566ded69ae803c0c7d0db809c589dc7e8c8dca6ce4594ccd2bbe0b3ae500b"
    },
    {
      "id": "mockup-graph-canvas",
      "path": "docs/mockups/graph-canvas.md",
      "title": "Graph canvas — large-graph UX (mockup)",
      "type": "doc",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-02-27",
      "reviewSuggested": [],
      "summary": "Self-contained, dependency-free mockup of the fixed graph-canvas UX: a force-directed node-link layout (degree-sized dots coloured by kind, thin edges behind), labels-on-demand, zoom/pan/fit, search-first focus+context, semantic-zoom clustering (LOD), an honest \"showing N of M\" caption, and disclosures as a chip. Replaces the current single-ring pile of opaque cards.",
      "tags": [
        "mockup",
        "ui-design",
        "graph",
        "canvas",
        "force-layout",
        "lod",
        "semantic-zoom"
      ],
      "links": [
        {
          "to": "review-ui-graph-canvas",
          "rel": "documents"
        },
        {
          "to": "spec-knowledge-exploration",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "70bb5a228bae7a84d5dff744cbba88ab24fb71765630436c763893aad00d1418"
    },
    {
      "id": "mockup-knowledge-explorer",
      "path": "docs/mockups/knowledge-explorer.md",
      "title": "Knowledge Explorer — graph + node introspection (mockup)",
      "type": "doc",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-02-27",
      "reviewSuggested": [],
      "summary": "Self-contained mockup of the knowledge exploration surface — a bounded 2D neighbourhood graph with a 2D/3D toggle, a node-introspection panel that routes each node to its natural renderer (code editor, rendered markdown, rendered html, proof), a provenance legend, and empty / loading / too-large states. The .html is data; this .md is its graph node.",
      "tags": [
        "mockup",
        "knowledge-graph",
        "2d-3d",
        "node-introspection",
        "provenance"
      ],
      "links": [
        {
          "to": "spec-knowledge-exploration",
          "rel": "documents"
        }
      ],
      "diagrams": [],
      "sourceSha256": "2ba356a6b77d2ed153dcf4f491c25b1bf8595ddfc2f0bcd2cba0459438ad9e36"
    },
    {
      "id": "mockup-knowledge-explorer-mode",
      "path": "docs/mockups/knowledge-explorer-mode.md",
      "title": "Knowledge Explorer mode — mockup",
      "type": "doc",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-02-28",
      "reviewSuggested": [],
      "summary": "Self-contained, dependency-free review mockup of the full-window dual-pane Knowledge Explorer mode (spec-knowledge-explorer-mode): the activity rail + a body-wide graph|reader split, with the reader's hard states (code/markdown/html/empty/loading/error/unsupported-kind/overflow) and the graph's loading/empty/too-large states, a review harness (state · viewport · theme · reduced-motion) and an in-artifact contrast/target audit. Tokens are the project DESIGN.md (chrome + the separate syntax palette + provenance). Open `knowledge-explorer-mode.html` over file://.",
      "tags": [
        "knowledge-graph",
        "explorer",
        "reader",
        "dual-pane",
        "mockup",
        "wpf"
      ],
      "links": [
        {
          "to": "spec-knowledge-explorer-mode",
          "rel": "documents"
        },
        {
          "to": "mockup-graph-canvas",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "51f511c81ba5ea794b33af699ec2ddb303aa746dd40e8ddcae87a16d92668420"
    },
    {
      "id": "mockup-uml-erm-surfaces",
      "path": "docs/mockups/uml-erm-surfaces.md",
      "title": "UML & ERM Surfaces — derived views (mockup)",
      "type": "doc",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-02-27",
      "reviewSuggested": [],
      "summary": "Self-contained mockup of the first-class UML & ERM surfaces — a model catalog master-detail with a crow's-foot ER diagram, a UML class diagram (composition/aggregation/dependency), and a C4 context view, all read-only with a permanent derived-view banner, inferred relationships dashed, and generation-error / too-large-curated / attempt-edit states. The .html is data; this .md is its node.",
      "tags": [
        "mockup",
        "uml",
        "erm",
        "c4",
        "derived-views",
        "read-only"
      ],
      "links": [
        {
          "to": "spec-uml-erm-surfaces",
          "rel": "documents"
        }
      ],
      "diagrams": [],
      "sourceSha256": "9207edfaa5dc6f86fc08966edc223d6689c8107ad9edbb9e7c10882d5b187550"
    },
    {
      "id": "mockup-workbench",
      "path": "docs/mockups/workbench.md",
      "title": "AI-DE workbench — reviewable mockup",
      "type": "doc",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "0",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [
        {
          "by": "spec-ai-native-ide",
          "on": "2026-08-26",
          "reason": "US-9 dockable workbench added; archetype corrected to Layout:MultiPanelWorkstation + Persistence:LocalDevice"
        }
      ],
      "summary": "Self-contained, dependency-free mockup of the dockable workbench (US-9) with a review harness covering state, named layout, theme, viewport, reduced motion and the layout lock. Renders the hard states — drop target, keyboard move, keyboard resize, at-minimum, floating, collapsed, maximized, loading, empty, error, partial restore, unreadable layout and overflow.",
      "tags": [
        "mockup",
        "ui",
        "workbench",
        "docking",
        "accessibility"
      ],
      "links": [
        {
          "to": "spec-ai-native-ide",
          "rel": "implements"
        },
        {
          "to": "adr-0012-docking-shell-library",
          "rel": "relates-to"
        },
        {
          "to": "architecture",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "bf5ead48ffaf206188af85737793da995de0409692d2133165110d2891a55a73"
    },
    {
      "id": "perf-results-phase-1",
      "path": "docs/design/phase-1-perf-results.md",
      "title": "P1-PERF — Phase-1 performance gate results",
      "type": "doc",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "1",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "The measured Phase-1 performance run that promotes the architecture's Inferred targets to Verified: refresh, describe, impact, find, knowledge, query plans, and restore RTO on the 50,000-edge corpus — plus the append-only growth curve, which shows the refresh budget failing after ~10 generations.",
      "tags": [
        "performance",
        "benchmark",
        "phase-1",
        "evidence"
      ],
      "links": [
        {
          "to": "design-phase-1-walking-skeleton",
          "rel": "documents"
        },
        {
          "to": "architecture",
          "rel": "documents"
        },
        {
          "to": "proof-pack-phase-1-walking-skeleton",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "db224bf7106c094b4c7a3e4f54182436c18f5f3f712a52021fde24f07fc3dcef"
    },
    {
      "id": "plan-ai-native-ide-architecture",
      "path": "docs/plans/ai-native-ide-architecture.md",
      "title": "Execution plan — AI-native IDE architecture",
      "type": "doc",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "0",
      "reviewBy": "2027-02-21",
      "reviewSuggested": [
        {
          "by": "spec-ai-native-ide",
          "on": "2026-08-26",
          "reason": "US-9 dockable workbench added; archetype corrected to Layout:MultiPanelWorkstation + Persistence:LocalDevice"
        }
      ],
      "summary": "A bounded execution graph for resolving the AI-native IDE’s storage and MCP contracts, then producing an architecture, ADRs, adversarial review, and discoverable evidence.",
      "tags": [
        "plan",
        "architecture",
        "ai-native-ide",
        "spikes"
      ],
      "links": [
        {
          "to": "spec-ai-native-ide",
          "rel": "relates-to"
        },
        {
          "to": "knowledge-hub",
          "rel": "depends-on"
        }
      ],
      "diagrams": [
        {
          "kind": "flowchart",
          "title": "Execution plan — AI-native IDE architecture",
          "mermaid": "flowchart LR\n  M[Merged specification] --> G[Ground constraints]\n  G --> S1[SQLite fact-store spike]\n  G --> S2[MCP SDK spike]\n  S1 --> D[Set data and boundary decisions]\n  S2 --> D\n  D --> A[Write architecture and ADRs]\n  A --> R[Architect council]\n  R -->|findings resolved| V[Derive, validate, audit]"
        }
      ],
      "sourceSha256": "b0e0dddad72fd5e6cb5fb9952449987946d6a6aacc4c2ea8787350069ccaed92"
    },
    {
      "id": "plan-ai-native-ide-specification",
      "path": "docs/plans/ai-native-ide-specification.md",
      "title": "Execution plan — AI-native IDE specification",
      "type": "doc",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "0",
      "reviewBy": "2027-02-20",
      "reviewSuggested": [],
      "summary": "A bounded execution graph for producing the AI-native IDE specification. It keeps grounding, ecosystem research, adversarial review, graph derivation, and audit evidence as explicit gates.",
      "tags": [
        "plan",
        "specification",
        "ai-native-ide"
      ],
      "links": [
        {
          "to": "knowledge-hub",
          "rel": "depends-on"
        },
        {
          "to": "seed-ai-native-ide-sketch",
          "rel": "relates-to"
        }
      ],
      "diagrams": [
        {
          "kind": "flowchart",
          "title": "Graph",
          "mermaid": "flowchart LR\n  G[Ground seed and knowledge] --> S[Write three-layer specification]\n  R[Research reusable candidates] --> S\n  S --> A[Adversarial specification review]\n  A -->|findings resolved| H[Render HTML and derive Docs Explorer]\n  H --> V[Validate graph, HTML, and audit record]"
        }
      ],
      "sourceSha256": "1e69075fe508c573e0487e1a1ae1973dbedc4331d09eecd65d4981e8cac3a590"
    },
    {
      "id": "proof-pack-phase-1-walking-skeleton",
      "path": "docs/design/phase-1-proof-pack.md",
      "title": "Phase 1 walking skeleton — Proof Pack",
      "type": "doc",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "1",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "One row per correctness claim for the Phase-1 walking skeleton, each with its test, its oracle, and whether the test was observed failing before its control existed. Records the three council-veto mechanisms as red-observed, and states the residual risks Phase 1 does not close.",
      "tags": [
        "proof-pack",
        "phase-1",
        "evidence",
        "tdd"
      ],
      "links": [
        {
          "to": "design-phase-1-walking-skeleton",
          "rel": "documents"
        },
        {
          "to": "architecture",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "53589db37edd1ae7891d2712c10bf66202b5d612e7e29379183b50c60ee823ec"
    },
    {
      "id": "release-plan-ai-native-ide",
      "path": "docs/release/ai-native-ide-release-plan.md",
      "title": "AI-DE release, compatibility, and rollback plan",
      "type": "doc",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "0",
      "reviewBy": "2027-02-21",
      "reviewSuggested": [
        {
          "by": "architecture",
          "on": "2026-08-25",
          "reason": "Defined the AI-DE workspace daemon, fact-store, MCP, terminal, and vertical delivery architecture"
        }
      ],
      "summary": "Defines progressive desktop release rings, default-off migration-dependent features, version compatibility, Windows/DPAPI parity, rollback handling, and Phase-1 supply-chain/CI gates for AI-DE.",
      "tags": [
        "release",
        "rollback",
        "migration",
        "compatibility",
        "supply-chain"
      ],
      "links": [
        {
          "to": "architecture",
          "rel": "documents"
        },
        {
          "to": "conceptual-model-ai-native-ide",
          "rel": "depends-on"
        },
        {
          "to": "threat-model-ai-native-ide",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "c6d8f6ca4e8896ef7c8169785f3c2e9f45a7012ec9bb580df466e7cf1d204da2"
    },
    {
      "id": "review-nvda-workbench-session",
      "path": "docs/reviews/nvda-workbench-session.md",
      "title": "NVDA verification session — the workbench accessibility claims",
      "type": "doc",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "1b",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "The human-run protocol that verifies whether a screen reader actually SPEAKS the workbench's announcements and pane names. Automated tests prove they are emitted and present in the UIA tree; only this session proves they are heard.",
      "tags": [
        "accessibility",
        "nvda",
        "wcag",
        "verification",
        "workbench"
      ],
      "links": [
        {
          "to": "spec-ai-native-ide",
          "rel": "documents"
        },
        {
          "to": "mockup-workbench",
          "rel": "relates-to"
        },
        {
          "to": "adr-0012-docking-shell-library",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "28af60f3c5209e889a6dbf5fb1c61876baf2f1a0f96cb6b08a7216fbc7481785"
    },
    {
      "id": "review-phase-2-exit",
      "path": "docs/reviews/phase-2-exit.md",
      "title": "Phase-2 exit review — real code, terminal, and process split",
      "type": "doc",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "2",
      "reviewBy": "2027-02-28",
      "reviewSuggested": [],
      "summary": "Phase 2's three components are built and measured. Every capability the phase promised is demonstrable, four gates are green over 541 tests, and the three performance budgets are met with large headroom. Five residual risks are carried into Phase 3, three of them by explicit decision.",
      "tags": [
        "review",
        "phase-gate",
        "phase-2"
      ],
      "links": [
        {
          "to": "design-phase-2-real-code-and-terminal",
          "rel": "relates-to"
        },
        {
          "to": "architecture",
          "rel": "relates-to"
        },
        {
          "to": "defect-classes",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "93530824cad8aa85f88148cf8dbd63ddf813e1358e5b9417e81170e2eb95a71f"
    },
    {
      "id": "review-ui-activity-rail",
      "path": "docs/reviews/ui-activity-rail.md",
      "title": "UI review — activity rail (elevate)",
      "type": "doc",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-02-27",
      "reviewSuggested": [],
      "summary": "Review/elevate of the workbench activity rail. Measured defect: at a 56px column, 9px captions under the glyphs clipped (\"Coordinate\" -> \"ordina\"). Fix (landed): icon-only rail with tooltip + accessible name — the VS Code / JetBrains idiom — 44px targets, and a softened borderless active pill. Registered UX-F (a caption clipped by its own container).",
      "tags": [
        "ui-review",
        "ui-design",
        "activity-rail",
        "navigation",
        "wcag",
        "elevate"
      ],
      "links": [
        {
          "to": "spec-app-facelift",
          "rel": "relates-to"
        },
        {
          "to": "mockup-activity-rail",
          "rel": "relates-to"
        },
        {
          "to": "architecture",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "fd6b4c07e2846f67caa6bda77ac88bb1c17067770990b372966d73c5abb417c4"
    },
    {
      "id": "review-ui-facelift",
      "path": "docs/reviews/ui-facelift.md",
      "title": "UI review — the facelift, elevate pass",
      "type": "doc",
      "status": "accepted",
      "owner": "@copilot-design",
      "phase": "facelift",
      "reviewBy": "2026-11-29",
      "reviewSuggested": [],
      "summary": "An elevate-mode /ui-design review of the shipped WPF facelift from live screenshots: the menu \"goofy block\" root-caused and fixed, the craft-gate measurement recorded, and a ranked plan of what else to do to reach best-in-class — led by a cohesive icon system.",
      "tags": [
        "ui-design",
        "review",
        "facelift",
        "elevate"
      ],
      "links": [
        {
          "to": "spec-app-facelift",
          "rel": "relates-to"
        },
        {
          "to": "mockup-workbench",
          "rel": "documents"
        },
        {
          "to": "mockup-app-facelift",
          "rel": "documents"
        }
      ],
      "diagrams": [],
      "sourceSha256": "e6fc17eb78821401cdff091a21bb0bdef37626774fcb439d93c8fb0b6b0e5249"
    },
    {
      "id": "review-ui-graph-canvas",
      "path": "docs/reviews/ui-graph-canvas.md",
      "title": "UI review — graph canvas (large-graph UX)",
      "type": "doc",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-02-27",
      "reviewSuggested": [
        {
          "by": "mockup-graph-canvas",
          "on": "2026-08-30",
          "reason": "Graph canvas implemented to the target: 2D force layout + degree-sized dots + pan/zoom/fit landed (DC-036); realizes part of US-K11 — re-check spec/implementation alignment and the still-open semantic-zoom LOD item."
        }
      ],
      "summary": "Review of the graph canvas on TheTerrace after the scaling fix let it load. The graph renders as an unreadable pile of overlapping opaque cards: the 2D layout is a single ring (fine for ~15 neighbours, catastrophic for 50), nodes are heavy boxes that occlude each other and the edges, there is no force-spread, no zoom/pan, no level-of-detail, a fixed 440px stage, and a disclosure wall on top. Target UX (mockup): a force-directed node-link layout with dots-not-cards, labels-on-demand, zoom/pan, semantic-zoom clustering, search-first focus+context, and disclosures as a chip.",
      "tags": [
        "ui-review",
        "ui-design",
        "graph",
        "canvas",
        "force-layout",
        "lod",
        "wcag",
        "elevate"
      ],
      "links": [
        {
          "to": "spec-knowledge-exploration",
          "rel": "relates-to"
        },
        {
          "to": "mockup-graph-canvas",
          "rel": "relates-to"
        },
        {
          "to": "inv-0003-graph-exceeds-ipc-frame-cap",
          "rel": "relates-to"
        },
        {
          "to": "architecture",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "21b5b5f58001a9d1721a76e7382333cf457ba63f7374a618e08ad92e46ee3d23"
    },
    {
      "id": "review-ui-mockups-craft-gate",
      "path": "docs/reviews/ui-mockups-craft-gate.md",
      "title": "Craft-gate review — facelift mockups",
      "type": "doc",
      "status": "accepted",
      "owner": "@copilot-design",
      "phase": "facelift",
      "reviewBy": "2026-11-27",
      "reviewSuggested": [],
      "summary": "The deterministic UI craft detector (ui-craft-gate.py / Impeccable) run over the five facelift mockups: measurement, translated findings, and the ranked plan. Material token-discipline and a11y findings were fixed this run; the residue is review-harness chrome and deliberate dense-IDE meta.",
      "tags": [
        "ui-design",
        "craft-gate",
        "review",
        "facelift"
      ],
      "links": [
        {
          "to": "mockup-app-facelift",
          "rel": "documents"
        },
        {
          "to": "mockup-knowledge-explorer",
          "rel": "documents"
        },
        {
          "to": "mockup-uml-erm-surfaces",
          "rel": "documents"
        }
      ],
      "diagrams": [],
      "sourceSha256": "24d9164538e3176c623194f377abb8f8b3a3d5c5c4a9397ef41c1da2c3de6ddf"
    },
    {
      "id": "review-ui-workbench",
      "path": "docs/reviews/ui-workbench.md",
      "title": "UI review — AI-DE dockable workbench",
      "type": "doc",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "0",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [
        {
          "by": "spec-ai-native-ide",
          "on": "2026-08-26",
          "reason": "US-9 dockable workbench added; archetype corrected to Layout:MultiPanelWorkstation + Persistence:LocalDevice"
        }
      ],
      "summary": "Rubric critique of the workbench mockup, structure before surface, with the deterministic craft gate folded in. Records one justified detector suppression and the accessibility regression the gate caught mid-review.",
      "tags": [
        "ui-review",
        "workbench",
        "accessibility",
        "craft-gate"
      ],
      "links": [
        {
          "to": "mockup-workbench",
          "rel": "documents"
        },
        {
          "to": "spec-ai-native-ide",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "e406470cb08fa582327a7446d316518989bc005e8edefdf38168fc902cf0ff75"
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
        },
        {
          "by": "architecture",
          "on": "2026-08-25",
          "reason": "Defined the AI-DE workspace daemon, fact-store, MCP, terminal, and vertical delivery architecture"
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
      "sourceSha256": "c04d25afbeb930496cf97e78fe6b92e05aab38e5e5d8523933e8709ee16e19d7"
    },
    {
      "id": "session-contracts",
      "path": "docs/collaboration/session-contracts.md",
      "title": "Two-session contract — core capabilities and design surfaces",
      "type": "doc",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-29",
      "reviewSuggested": [],
      "summary": "Who owns which files, which interfaces are the seam between them, and how a change to that seam is agreed. Written by the core session so the design session can disagree with something concrete.",
      "tags": [
        "collaboration",
        "contracts",
        "ownership",
        "worktrees"
      ],
      "links": [
        {
          "to": "architecture",
          "rel": "relates-to"
        },
        {
          "to": "knowledge-hub",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "577cefc93906f5c60488c7bc8548a75a50234bc1f10b4eb54eb4a62a192065a4"
    },
    {
      "id": "spike-dpi-and-ganged-resize",
      "path": "docs/reviews/spike-dpi-and-ganged-resize.md",
      "title": "Spike — per-monitor DPI and ganged resize",
      "type": "doc",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "1b",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "The last two ADR-0012 spikes. Found the app was System DPI aware rather than Per-Monitor V2 — a prerequisite defect for US-9's floating panes, in our code rather than the docking library's. Fixed and verified against the running executable. Ganged resize holds: no two docked panes share area. The cross-monitor transition itself remains untested for want of a second display.",
      "tags": [
        "spike",
        "dpi",
        "multi-monitor",
        "resize",
        "adr-0012"
      ],
      "links": [
        {
          "to": "adr-0012-docking-shell-library",
          "rel": "documents"
        },
        {
          "to": "design-phase-1b-workbench",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "de30aa3e7abb60419e8e66713516a64814a1bce7272df2cc78f7e226ef5cffb4"
    },
    {
      "id": "spike-layout-upgrade-roundtrip",
      "path": "docs/reviews/spike-layout-upgrade-roundtrip.md",
      "title": "Spike — layout round-trip across an app upgrade",
      "type": "doc",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "1b",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "The ADR-0012 round-trip spike, run. It found that the versioned envelope had a version field but no migration hook, so the first release to rename a surface would have degraded every saved layout to the default. The hook is now implemented and pinned by tests.",
      "tags": [
        "spike",
        "layout",
        "migration",
        "persistence",
        "adr-0012"
      ],
      "links": [
        {
          "to": "adr-0013-layout-persistence-envelope",
          "rel": "documents"
        },
        {
          "to": "adr-0012-docking-shell-library",
          "rel": "relates-to"
        },
        {
          "to": "design-phase-1b-workbench",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "87360ac96ef97fa830bbcee369a8ec3a8f5545eae3d45d82dbb9433a460e93ef"
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
      "reviewSuggested": [
        {
          "by": "architecture",
          "on": "2026-08-25",
          "reason": "Defined the AI-DE workspace daemon, fact-store, MCP, terminal, and vertical delivery architecture"
        }
      ],
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
      "sourceSha256": "41e5f38d21389905b10b413b210d587496aba0d15d63bf6dffec044d12bca343"
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
      "reviewSuggested": [
        {
          "by": "kb-graph-experience-and-visualization",
          "on": "2026-08-29",
          "reason": "LazyGraphRAG (~700x cheaper global queries) and LightRAG materially update finding #8 (GraphRAG = 26-85x cost); revisit the cost conclusion"
        }
      ],
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
      "sourceSha256": "963274b7b88aa76c07383e53a7ca386289ac37d7009d6098c8f299b814f336f4"
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
      "id": "kb-content-rendering-comparables",
      "path": "docs/knowledge/editor-and-content-rendering-surfaces/comparables.md",
      "title": "Editor & Content Rendering Surfaces — comparables & libraries",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-27",
      "reviewSuggested": [],
      "summary": "Named code-viewing and markdown/HTML-rendering libraries for WPF, with licence, role and fit for the node-introspection panes.",
      "tags": [
        "monaco",
        "avalonedit",
        "roslynpad",
        "markdig",
        "libraries",
        "licences"
      ],
      "links": [
        {
          "to": "kb-editor-and-content-rendering-surfaces",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "c85926fe9aba23bea65c6c559361adfebce1f43b8c052d9c2cd1cbf04b6363c8"
    },
    {
      "id": "kb-content-rendering-data",
      "path": "docs/knowledge/editor-and-content-rendering-surfaces/data-and-constants.md",
      "title": "Editor & Content Rendering Surfaces — data, constants & decision matrix",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-27",
      "reviewSuggested": [],
      "summary": "The licence facts and the per-node-type renderer decision matrix for the introspection panes.",
      "tags": [
        "decision-matrix",
        "licences",
        "monaco",
        "avalonedit",
        "markdig"
      ],
      "links": [
        {
          "to": "kb-editor-and-content-rendering-surfaces",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "ac53465338c492158679f5a1e56df9b448a5cd0f1419ea5778a354f8da915f70"
    },
    {
      "id": "kb-content-rendering-glossary",
      "path": "docs/knowledge/editor-and-content-rendering-surfaces/glossary.md",
      "title": "Editor & Content Rendering Surfaces — glossary",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-27",
      "reviewSuggested": [],
      "summary": "Precise definitions for the code-viewing and content-rendering vocabulary so the panes and their docs agree.",
      "tags": [
        "glossary",
        "monaco",
        "avalonedit",
        "markdig",
        "webview2"
      ],
      "links": [
        {
          "to": "kb-editor-and-content-rendering-surfaces",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "d7ce0dcdf8abb3e26b7606c2760f4793ef9312bc6dc388c0d2e3445ba21eb8c9"
    },
    {
      "id": "kb-content-rendering-open-questions",
      "path": "docs/knowledge/editor-and-content-rendering-surfaces/open-questions.md",
      "title": "Editor & Content Rendering Surfaces — open questions & failure modes",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-27",
      "reviewSuggested": [],
      "summary": "What the rendering-surface research could not settle, the domain's failure modes, and the disconfirming views sought against each renderer choice.",
      "tags": [
        "open-questions",
        "failure-modes",
        "disconfirming",
        "monaco",
        "avalonedit"
      ],
      "links": [
        {
          "to": "kb-editor-and-content-rendering-surfaces",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "246aeca1997e3b36fbf01a89b4e3c7a59cd76e8ec9119c9006aeb96d6e5b51da"
    },
    {
      "id": "kb-content-rendering-references",
      "path": "docs/knowledge/editor-and-content-rendering-surfaces/references.md",
      "title": "Editor & Content Rendering Surfaces — references",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-27",
      "reviewSuggested": [],
      "summary": "The authoritative repos, docs and licence facts behind the code-viewing and markdown/HTML rendering options — the ones to quote rather than recall.",
      "tags": [
        "monaco",
        "avalonedit",
        "roslynpad",
        "markdig",
        "references"
      ],
      "links": [
        {
          "to": "kb-editor-and-content-rendering-surfaces",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "d8dfa216c6dcbba7ec8301934642a036b2df79223bc09fb8bba8ace6df5fad69"
    },
    {
      "id": "kb-content-rendering-sota",
      "path": "docs/knowledge/editor-and-content-rendering-surfaces/state-of-the-art.md",
      "title": "Editor & Content Rendering Surfaces — state of the art",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-27",
      "reviewSuggested": [],
      "summary": "Current best practice for viewing code and rendering markdown/HTML in a WPF desktop app — the native (AvalonEdit/RoslynPad/Markdig.Wpf) vs web (Monaco/WebView2) options and when each wins.",
      "tags": [
        "monaco",
        "avalonedit",
        "roslynpad",
        "markdig",
        "webview2"
      ],
      "links": [
        {
          "to": "kb-editor-and-content-rendering-surfaces",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "dd5626d2f11aa7ded2e39a5c669845784b18672fdad20f77e1a2ae33ca24992f"
    },
    {
      "id": "kb-content-rendering-sources",
      "path": "docs/knowledge/editor-and-content-rendering-surfaces/sources.md",
      "title": "Editor & Content Rendering Surfaces — sources",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-27",
      "reviewSuggested": [],
      "summary": "The full access-dated source list behind the editor-and-content-rendering-surfaces base, keyed [ED1]..[ED8] as cited throughout the topic.",
      "tags": [
        "sources",
        "citations"
      ],
      "links": [
        {
          "to": "kb-editor-and-content-rendering-surfaces",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "3bf7cf79a0aff9b926ca04669baba36609b9c7ae7d558e1866405e3c801ad2bf"
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
      "id": "kb-dashboards-comparables",
      "path": "docs/knowledge/operational-and-test-dashboards/comparables.md",
      "title": "Operational & Test Dashboards — comparables & tools",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-27",
      "reviewSuggested": [],
      "summary": "Named tools and libraries for test-result, CI/CD and metrics visualisation, with licence, role and where each fits an embedded WPF pane.",
      "tags": [
        "dashboards",
        "tools",
        "charting-libraries",
        "licences"
      ],
      "links": [
        {
          "to": "kb-operational-and-test-dashboards",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "0e1fa237687b9b4bd8cfc1c88ea5f9ffd652d90d4bd8acebb7257c98a78cffc2"
    },
    {
      "id": "kb-dashboards-data",
      "path": "docs/knowledge/operational-and-test-dashboards/data-and-constants.md",
      "title": "Operational & Test Dashboards — data, constants & layout rules",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-27",
      "reviewSuggested": [],
      "summary": "Concrete method definitions, layout rules, charting-library licences and the metrics/percentiles a trustworthy operational or test pane must show.",
      "tags": [
        "dashboards",
        "constants",
        "layout",
        "red",
        "use",
        "licences"
      ],
      "links": [
        {
          "to": "kb-operational-and-test-dashboards",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "337e4c728d1405586dfdc318e5ba9a9d56a70ab55fac537df05f7bcf678fbd7d"
    },
    {
      "id": "kb-dashboards-glossary",
      "path": "docs/knowledge/operational-and-test-dashboards/glossary.md",
      "title": "Operational & Test Dashboards — glossary",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-27",
      "reviewSuggested": [],
      "summary": "Precise definitions for the dashboard/observability/test-reporting vocabulary so the panes and their docs agree.",
      "tags": [
        "glossary",
        "dashboards",
        "red",
        "use",
        "ci-cd",
        "test"
      ],
      "links": [
        {
          "to": "kb-operational-and-test-dashboards",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "257c14947ac974ff50218804ee4276d3370d6387b8b5d235b625049e5b048258"
    },
    {
      "id": "kb-dashboards-open-questions",
      "path": "docs/knowledge/operational-and-test-dashboards/open-questions.md",
      "title": "Operational & Test Dashboards — open questions & failure modes",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-27",
      "reviewSuggested": [],
      "summary": "What the dashboards research could not settle, the domain's silent failure modes, and the disconfirming views deliberately sought against building bespoke visualisation panes.",
      "tags": [
        "open-questions",
        "failure-modes",
        "dashboards",
        "disconfirming"
      ],
      "links": [
        {
          "to": "kb-operational-and-test-dashboards",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "75b67bc90d7977f3ed9aeba931199be6deb7c668ead5b9376b257134ea1fde71"
    },
    {
      "id": "kb-dashboards-references",
      "path": "docs/knowledge/operational-and-test-dashboards/references.md",
      "title": "Operational & Test Dashboards — references",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-27",
      "reviewSuggested": [],
      "summary": "The authoritative methods, docs and specs behind operational/test dashboards — RED, USE, Grafana best practices, the reporting tools' docs, and the charting-library licences.",
      "tags": [
        "dashboards",
        "references",
        "red",
        "use",
        "standards"
      ],
      "links": [
        {
          "to": "kb-operational-and-test-dashboards",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "365e245b2b9ef65861078de061c358aaab03bfdeff429d4c8daa076aa1956b1a"
    },
    {
      "id": "kb-dashboards-sota",
      "path": "docs/knowledge/operational-and-test-dashboards/state-of-the-art.md",
      "title": "Operational & Test Dashboards — state of the art",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-27",
      "reviewSuggested": [],
      "summary": "Current best practice for test-result, CI/CD and operational-metrics visualisation — the reporting tools, the pipeline-as-diagram path, and the RED/USE dashboard methods.",
      "tags": [
        "dashboards",
        "red",
        "use",
        "allure",
        "reportportal",
        "grafana"
      ],
      "links": [
        {
          "to": "kb-operational-and-test-dashboards",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "f06a8d9175207e63a62009c25e9f8db4d1d0c8db81c93727737aaa1932d0ffe8"
    },
    {
      "id": "kb-dashboards-sources",
      "path": "docs/knowledge/operational-and-test-dashboards/sources.md",
      "title": "Operational & Test Dashboards — sources",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-27",
      "reviewSuggested": [],
      "summary": "The full access-dated source list behind the operational-and-test-dashboards base, keyed [D1]..[D14] as cited throughout the topic.",
      "tags": [
        "sources",
        "citations"
      ],
      "links": [
        {
          "to": "kb-operational-and-test-dashboards",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "edab3da6a68e7fa4d9d623fac19b106f5f0192b6a6b1d8229f59f3f3358163d0"
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
      "id": "kb-editor-and-content-rendering-surfaces",
      "path": "docs/knowledge/editor-and-content-rendering-surfaces/index.md",
      "title": "Editor & Content Rendering Surfaces — domain knowledge",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-27",
      "reviewSuggested": [],
      "summary": "Evidence base for the AI-DE content-rendering panes: viewing/reviewing code (Monaco via WebView2, AvalonEdit, RoslynPad — all MIT) and rendering markdown/HTML (Markdig + Markdig.Wpf, or WebView2), with the permissive-licence facts and the native-vs-web trade-off for each surface.",
      "tags": [
        "monaco",
        "avalonedit",
        "roslynpad",
        "markdig",
        "webview2",
        "code-viewing",
        "markdown",
        "html",
        "wpf"
      ],
      "links": [
        {
          "to": "knowledge-hub",
          "rel": "refines"
        },
        {
          "to": "kb-ai-native-ide-shell",
          "rel": "relates-to"
        },
        {
          "to": "kb-graph-experience-and-visualization",
          "rel": "relates-to"
        },
        {
          "to": "kb-wpf-modern-ui-styling",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "2cfbae8301d899891cdc49373446df9aeb3d323bb0c76d3b276467a62cf8190c"
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
      "id": "kb-graph-experience-and-visualization",
      "path": "docs/knowledge/graph-experience-and-visualization/index.md",
      "title": "Unified Graph Experience & Visualization — domain knowledge",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-27",
      "reviewSuggested": [],
      "summary": "Evidence base for the AI-DE end-to-end graph experience — a unified code-graph + knowledge-graph a user navigates and introspects node by node (walk from a C# file to the knowledge that informed it). Covers GraphRAG and its cheaper variants, composing Obsidian + Graphify, 2D/3D graph visualization libraries, node-based UIs, and how to host a graph explorer in a WPF/WebView2 shell.",
      "tags": [
        "knowledge-graph",
        "graphrag",
        "obsidian",
        "graphify",
        "graph-visualization",
        "3d",
        "force-graph",
        "node-introspection"
      ],
      "links": [
        {
          "to": "knowledge-hub",
          "rel": "refines"
        },
        {
          "to": "kb-code-knowledge-graphs",
          "rel": "relates-to"
        },
        {
          "to": "kb-diagram-generation",
          "rel": "relates-to"
        },
        {
          "to": "kb-ai-native-ide-shell",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "70638c2fbc0d66de962b867bd2523f54626eb6e0e466864fe08b02f1326a34d4"
    },
    {
      "id": "kb-graph-experience-comparables",
      "path": "docs/knowledge/graph-experience-and-visualization/comparables.md",
      "title": "Unified Graph Experience — comparables & libraries",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-27",
      "reviewSuggested": [],
      "summary": "Named graph-visualization libraries, node-based UI frameworks, Obsidian graph plugins, and desktop knowledge-graph apps — with licence, role and fit for an embedded WPF/WebView2 explorer.",
      "tags": [
        "graph-visualization",
        "libraries",
        "licences",
        "obsidian-plugins",
        "node-editors"
      ],
      "links": [
        {
          "to": "kb-graph-experience-and-visualization",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "a79e7129068d782f36fa2f61cfeb77716ecc110c2148442315844d5aae539248"
    },
    {
      "id": "kb-graph-experience-data",
      "path": "docs/knowledge/graph-experience-and-visualization/data-and-constants.md",
      "title": "Unified Graph Experience — data, constants & thresholds",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-27",
      "reviewSuggested": [],
      "summary": "The cost figures, scale thresholds, licences and provenance mappings to quote for the graph experience — GraphRAG costs, renderer scale limits, and the code↔knowledge edge model.",
      "tags": [
        "graphrag",
        "constants",
        "thresholds",
        "licences",
        "graph-visualization"
      ],
      "links": [
        {
          "to": "kb-graph-experience-and-visualization",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "cd881438c822a7f2869e646e05d45c05d7210e7f9e5a99b6ddd57ec957554083"
    },
    {
      "id": "kb-graph-experience-glossary",
      "path": "docs/knowledge/graph-experience-and-visualization/glossary.md",
      "title": "Unified Graph Experience — glossary",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-27",
      "reviewSuggested": [],
      "summary": "Precise definitions for the graph-experience vocabulary — GraphRAG, community detection, force- directed layout, node introspection, the code↔docs join — so the code and its docs agree.",
      "tags": [
        "glossary",
        "graphrag",
        "graph-visualization",
        "obsidian",
        "graphify"
      ],
      "links": [
        {
          "to": "kb-graph-experience-and-visualization",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "1586d965392fc2e884f191c55fcbeb7b6d771a20d9616c8b5aefc0a768dae28e"
    },
    {
      "id": "kb-graph-experience-open-questions",
      "path": "docs/knowledge/graph-experience-and-visualization/open-questions.md",
      "title": "Unified Graph Experience — open questions & failure modes",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-27",
      "reviewSuggested": [],
      "summary": "What the graph-experience research could not settle, the domain's silent failure modes, and the disconfirming views deliberately sought against building a custom in-editor graph explorer.",
      "tags": [
        "open-questions",
        "failure-modes",
        "graphrag",
        "graph-visualization",
        "disconfirming"
      ],
      "links": [
        {
          "to": "kb-graph-experience-and-visualization",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "9296a203b2a814ee6d26951fe5a2932cc473770914ac7f3bda6d3b11fb1e3ef0"
    },
    {
      "id": "kb-graph-experience-references",
      "path": "docs/knowledge/graph-experience-and-visualization/references.md",
      "title": "Unified Graph Experience — references",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-27",
      "reviewSuggested": [],
      "summary": "The authoritative sources behind GraphRAG, the graph-viz libraries, and the Obsidian/Graphify composition — the ones to quote rather than recall.",
      "tags": [
        "graphrag",
        "references",
        "graph-visualization",
        "standards"
      ],
      "links": [
        {
          "to": "kb-graph-experience-and-visualization",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "f6371f685e7853f90580ea11b98df574c17d39e19e0ef31adddc6e6fd25f4b4b"
    },
    {
      "id": "kb-graph-experience-sota",
      "path": "docs/knowledge/graph-experience-and-visualization/state-of-the-art.md",
      "title": "Unified Graph Experience — state of the art",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-27",
      "reviewSuggested": [],
      "summary": "Current best practice for GraphRAG retrieval, 2D/3D graph visualization, and composing Obsidian + Graphify into a navigable code+knowledge graph experience.",
      "tags": [
        "graphrag",
        "graph-visualization",
        "3d",
        "obsidian",
        "force-graph"
      ],
      "links": [
        {
          "to": "kb-graph-experience-and-visualization",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "3f364082dd16155eef255addfc36d97d51a0a800f3c0c25ea6e439cbd79095b7"
    },
    {
      "id": "kb-graph-experience-sources",
      "path": "docs/knowledge/graph-experience-and-visualization/sources.md",
      "title": "Unified Graph Experience — sources",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-27",
      "reviewSuggested": [],
      "summary": "The full access-dated source list behind the graph-experience-and-visualization base, keyed [GX1]..[GX22] as cited throughout the topic.",
      "tags": [
        "sources",
        "citations"
      ],
      "links": [
        {
          "to": "kb-graph-experience-and-visualization",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "15882694b21d15a8b7c5abcad9848e31ce1c2a34fddc66f32d4f4c5ca2c1b02f"
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
      "id": "kb-operational-and-test-dashboards",
      "path": "docs/knowledge/operational-and-test-dashboards/index.md",
      "title": "Operational & Test Result Dashboards — domain knowledge",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-27",
      "reviewSuggested": [],
      "summary": "Evidence base for the AI-DE panes that visualise test results, CI/CD pipeline execution, and operational logs/metrics: the reporting tools (Allure, ReportPortal), pipeline-as-diagram tools, the RED/USE dashboard methods, the permissive charting libraries (LiveCharts2, ScottPlot, OxyPlot), and the design rules that keep a dashboard actionable rather than a wall of numbers.",
      "tags": [
        "dashboards",
        "test-results",
        "ci-cd",
        "observability",
        "metrics",
        "charting",
        "grafana",
        "allure"
      ],
      "links": [
        {
          "to": "knowledge-hub",
          "rel": "refines"
        },
        {
          "to": "kb-microservice-interaction",
          "rel": "relates-to"
        },
        {
          "to": "kb-diagram-generation",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "1a5d8c571ce92bf0e7075ebebd3132fdb2fb0ceea9df6ce3133f5e0ff6d6ec71"
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
      "id": "kb-wpf-modern-ui-styling",
      "path": "docs/knowledge/wpf-modern-ui-styling/index.md",
      "title": "Modern & Soft WPF UI Styling — domain knowledge",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-27",
      "reviewSuggested": [],
      "summary": "Evidence base for giving the AiDe.App WPF shell a modern, soft, rounded look — Windows 11 DWM rounded corners and Mica, WindowChrome custom title bars, the built-in .NET Fluent theme, the permissive (MIT) control-library landscape, soft-shadow performance, and the IDE/editor UX exemplars (JetBrains New UI / Islands, VS Code, Zed, DaVinci Resolve) that inform it.",
      "tags": [
        "wpf",
        "styling",
        "fluent",
        "windowchrome",
        "dwm",
        "mica",
        "control-libraries",
        "ide-ux",
        "dark-theme"
      ],
      "links": [
        {
          "to": "knowledge-hub",
          "rel": "refines"
        },
        {
          "to": "kb-ai-native-ide-shell",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "fd8551f9b15d5fb4f602a55fa7f5db5820394a4415eb9f410121bd28916c4996"
    },
    {
      "id": "kb-wpf-styling-comparables",
      "path": "docs/knowledge/wpf-modern-ui-styling/comparables.md",
      "title": "Modern WPF Styling — comparables & libraries",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-27",
      "reviewSuggested": [],
      "summary": "The permissively-licensed WPF control/styling library landscape (named, with licence, look and maintenance), plus the IDE and creative-tool UX exemplars that define the modern-soft target.",
      "tags": [
        "wpf",
        "control-libraries",
        "ide-ux",
        "exemplars",
        "licences"
      ],
      "links": [
        {
          "to": "kb-wpf-modern-ui-styling",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "4403e77044dd3a52fcec0f327ce1c52d094f51eb8f17a1536d67cf8abd8975da"
    },
    {
      "id": "kb-wpf-styling-data",
      "path": "docs/knowledge/wpf-modern-ui-styling/data-and-constants.md",
      "title": "Modern WPF Styling — data, constants & recipes",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-27",
      "reviewSuggested": [],
      "summary": "Concrete attribute values, licence/version facts, soft-shadow/rounded recipes and a starter token scale for a modern-soft WPF look.",
      "tags": [
        "wpf",
        "constants",
        "recipes",
        "tokens"
      ],
      "links": [
        {
          "to": "kb-wpf-modern-ui-styling",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "7a9be5f0d48034753c328f46d84e6f1773de154c066fb7fc9c820184964a5de0"
    },
    {
      "id": "kb-wpf-styling-glossary",
      "path": "docs/knowledge/wpf-modern-ui-styling/glossary.md",
      "title": "Modern WPF Styling — glossary",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-27",
      "reviewSuggested": [],
      "summary": "Precise definitions for the WPF-styling vocabulary — WindowChrome, DWM corner preference, Mica, Fluent theme, ThemeMode, elevation — so the styling code and its docs agree.",
      "tags": [
        "glossary",
        "wpf",
        "dwm",
        "fluent"
      ],
      "links": [
        {
          "to": "kb-wpf-modern-ui-styling",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "5959b3b76b63a28304c248b13bd86ff8a1086d67a3929025dde35c3062f2ec8e"
    },
    {
      "id": "kb-wpf-styling-open-questions",
      "path": "docs/knowledge/wpf-modern-ui-styling/open-questions.md",
      "title": "Modern WPF Styling — open questions & failure modes",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-27",
      "reviewSuggested": [],
      "summary": "What the WPF-styling research could not settle, the domain's silent failure modes, and the disconfirming views deliberately sought against \"adopt a modern WPF UI library\".",
      "tags": [
        "open-questions",
        "failure-modes",
        "wpf",
        "disconfirming"
      ],
      "links": [
        {
          "to": "kb-wpf-modern-ui-styling",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "d9b14167c4f8e077385675ad5b7cbccfd5889dbe832dee4880df17a292b3c8e6"
    },
    {
      "id": "kb-wpf-styling-references",
      "path": "docs/knowledge/wpf-modern-ui-styling/references.md",
      "title": "Modern WPF Styling — references",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-27",
      "reviewSuggested": [],
      "summary": "The authoritative API surfaces, standards and reference kits behind modern WPF styling — the ones to quote rather than recall.",
      "tags": [
        "wpf",
        "references",
        "apis",
        "standards"
      ],
      "links": [
        {
          "to": "kb-wpf-modern-ui-styling",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "3422d88cf23f3d5ababc54941c4c5f617a7e4d1da6defb2ab8cd3a43205a67cb"
    },
    {
      "id": "kb-wpf-styling-sota",
      "path": "docs/knowledge/wpf-modern-ui-styling/state-of-the-art.md",
      "title": "Modern WPF Styling — state of the art",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-27",
      "reviewSuggested": [],
      "summary": "Current best practice for a modern, soft WPF look: the DWM + WindowChrome + Fluent-theme stack, the Mica situation, soft-shadow technique, and rounded-control re-templating.",
      "tags": [
        "wpf",
        "fluent",
        "dwm",
        "windowchrome",
        "mica"
      ],
      "links": [
        {
          "to": "kb-wpf-modern-ui-styling",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "a960243c9034f212ea6524e29316ee24d6daa301550df6760c15f91671b8f273"
    },
    {
      "id": "kb-wpf-styling-sources",
      "path": "docs/knowledge/wpf-modern-ui-styling/sources.md",
      "title": "Modern WPF Styling — sources",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2026-11-27",
      "reviewSuggested": [],
      "summary": "The full access-dated source list behind the WPF-modern-ui-styling base, keyed [W1]..[W25] as cited throughout the topic.",
      "tags": [
        "sources",
        "citations"
      ],
      "links": [
        {
          "to": "kb-wpf-modern-ui-styling",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "3a5dbfa74db1a03b721985696d85edda617bf9acc4ccad90292d98e23560e595"
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
      "reviewSuggested": [
        {
          "by": "architecture",
          "on": "2026-08-25",
          "reason": "Defined the AI-DE workspace daemon, fact-store, MCP, terminal, and vertical delivery architecture"
        }
      ],
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
      "sourceSha256": "4ca22685db704bea072312d2a683d8e771a5c5ce2757822e8527e25b9087ab78"
    },
    {
      "id": "privacy-review-ai-native-ide",
      "path": "docs/security/ai-native-ide-privacy-review.md",
      "title": "AI-native IDE — Privacy and data-governance review",
      "type": "privacy-review",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "0",
      "reviewBy": "2027-02-20",
      "reviewSuggested": [
        {
          "by": "spec-ai-native-ide",
          "on": "2026-08-26",
          "reason": "US-9 dockable workbench added; archetype corrected to Layout:MultiPanelWorkstation + Persistence:LocalDevice"
        }
      ],
      "summary": "Defines the privacy posture for local AI-IDE workspace data: data inventory, purpose, retention, deletion, indirect model egress, and LINDDUN-lite dispositions. It is a pre-implementation gate for the AI-native IDE specification.",
      "tags": [
        "privacy",
        "linddun",
        "work-data",
        "prompts",
        "audit"
      ],
      "links": [
        {
          "to": "spec-ai-native-ide",
          "rel": "documents"
        },
        {
          "to": "knowledge-hub",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "32b4814e4d2bd7463961ca3f0822adde409eddbe18b8fa45b47ff68df81968ba"
    },
    {
      "id": "spec-ai-native-ide",
      "path": "docs/specs/ai-native-ide.md",
      "title": "AI-native IDE — Product specification",
      "type": "spec",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "0",
      "reviewBy": "2027-02-20",
      "reviewSuggested": [
        {
          "by": "architecture",
          "on": "2026-08-25",
          "reason": "Defined the AI-DE workspace daemon, fact-store, MCP, terminal, and vertical delivery architecture"
        }
      ],
      "summary": "Specifies a local-first AI-native IDE for working across isolated coding-agent sessions while understanding the code-derived architecture, domain, data, process, dependencies, knowledge, audit history, and coordinated work as linked visual views.",
      "tags": [
        "ai-native-ide",
        "architecture-visualization",
        "code-knowledge-graph",
        "agent-coordination",
        "prompts"
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
        },
        {
          "to": "privacy-review-ai-native-ide",
          "rel": "depends-on"
        }
      ],
      "diagrams": [
        {
          "kind": "flowchart",
          "title": "Inspect an architectural relationship and provide feedback",
          "mermaid": "flowchart TD\n  A[Open workspace] --> B{Workspace evidence healthy?}\n  B -->|yes| C[Choose Explore view and select node]\n  B -->|partial or stale| BS[Show source-specific stale status] --> C\n  C --> D[Inspect provenance, confidence and dependencies]\n  D --> E{Evidence sufficient?}\n  E -->|yes| F[Open related worktree/session and audit context]\n  E -->|no| G[Label unknown or request bounded refresh]\n  G --> H{Refresh succeeds?}\n  H -->|yes| D\n  H -->|no| I[Keep last known view and show recovery choices]\n  I --> K{Retry, inspect source, or cancel?}\n  K -->|retry| G\n  K -->|inspect source| L[Open source/provenance inspector] --> D\n  K -->|cancel| M([Return to Explore with context preserved])\n  F --> J[Create or open prompt draft]"
        },
        {
          "kind": "flowchart",
          "title": "Stage, review, and dispatch a prompt",
          "mermaid": "flowchart TD\n  A[Create prompt draft] --> B[Compose rich-text prompt and attach bounded context references]\n  B --> C[Review target session]\n  C --> D{Target ready and in workspace?}\n  D -->|yes| E[Show exact revision and confirm transfer]\n  D -->|busy, disconnected, wrong workspace| F[Keep draft and show reason]\n  F --> C\n  E --> G[Dispatch]\n  G --> H{Delivery acknowledged?}\n  H -->|yes| I[Record audit receipt and open session]\n  H -->|no| J[Keep draft and record failed outcome]\n  J --> K{Retry, retarget, or cancel?}\n  K -->|retry| C\n  K -->|retarget| C\n  K -->|cancel| X([Preserve draft and exit])"
        },
        {
          "kind": "flowchart",
          "title": "Coordinate work across worktrees",
          "mermaid": "flowchart TD\n  A[Open work board] --> B[Filter by repository, worktree, session, or agent]\n  B --> BN{Items match?}\n  BN -->|no| BX[Show no-match state and clear or change filter] --> B\n  BN -->|yes| C[Inspect work item evidence and dependencies]\n  C --> D{State current and authoritative?}\n  D -->|yes| E[Use state to plan feedback or next work slice]\n  D -->|advisory, stale, unknown| F[Show label and inspect source/audit evidence]\n  F --> G{Record new advisory claim?}\n  G -->|no| H[Return to board]\n  G -->|yes| GI{Write succeeds?}\n  GI -->|yes| H\n  GI -->|no| GF[Keep prior state and show write failure/retry] --> G\n  E --> H[Return to board]"
        },
        {
          "kind": "flowchart",
          "title": "Open a workspace, explore evidence, or recover",
          "mermaid": "flowchart TD\n  A[Open workspace] --> B{Membership and local service available?}\n  B -->|yes| C{Evidence available?}\n  B -->|no| BR[Show contained path/session failure and recovery choices]\n  BR -->|retry after user repair| A\n  BR -->|cancel| X([Preserve context and exit])\n  C -->|yes| D[Search or browse Explore]\n  C -->|empty or unsupported| CE[Explain empty/unsupported source and link to source configuration]\n  CE --> D\n  D --> E[Select node, diagram, source, or knowledge item]\n  E --> F{Bounded data available?}\n  F -->|yes| G[Inspect source/provenance and navigate related item]\n  F -->|over limit or refresh failed| H[Show limits or stale snapshot with retry/cancel]\n  H -->|retry succeeds| G\n  H -->|cancel| D"
        },
        {
          "kind": "flowchart",
          "title": "Arrange the workbench (drag path and keyboard path converge)",
          "mermaid": "flowchart TD\n  A[Start a layout change] --> B{Pointer or keyboard?}\n  B -->|drag| C[Pick up a surface or a splitter]\n  B -->|keyboard| K[Command palette or pane menu:<br/>Move · Split · Float · Collapse · Resize]\n  C --> D{Layout locked?}\n  K --> D\n  D -->|yes| DL[Refuse and explain: layout is locked] --> Z([Layout unchanged])\n  D -->|no| E{Valid destination?}\n  E -->|drag| F[Show the destination BEFORE release:<br/>split edge · join stack · dock region · float]\n  E -->|keyboard| G[Show the selected edge or target,<br/>arrow keys adjust in declared increments]\n  F --> H{Commit or cancel?}\n  G --> H\n  H -->|Escape / cancel| Z\n  H -->|commit| I{Would it break the tiling<br/>or a minimum size?}\n  I -->|yes| J[Stop at the minimum; refuse the illegal drop] --> F\n  I -->|no| L[Apply: redistribute space, collapse empty stacks]\n  L --> M[Announce the change to AT without moving focus]\n  M --> N[Place focus on a defined, unobscured element]\n  N --> O([Layout persisted for this workspace])"
        },
        {
          "kind": "flowchart",
          "title": "Restore a layout that cannot be fully honoured",
          "mermaid": "flowchart TD\n  A[Open workspace] --> B{Stored layout readable?}\n  B -->|no / incompatible version| C[Start from the default arrangement,<br/>say so, PRESERVE the unreadable file] --> Z([Usable window])\n  B -->|yes| D{Every surface still exists?}\n  D -->|no| E[Place the rest validly;<br/>report exactly which surfaces were dropped]\n  D -->|yes| F{Every floating pane's display connected?}\n  E --> F\n  F -->|no| G[Re-home off-screen panes onto a connected display;<br/>report what moved]\n  F -->|yes| H[Restore as saved]\n  G --> H\n  H --> Z"
        },
        {
          "kind": "flowchart",
          "title": "Search audit history and recover a tab/session",
          "mermaid": "flowchart TD\n  A[Open Audit or a saved tab] --> B{Underlying source/session available?}\n  B -->|audit available| C[Filter timeline and select entry]\n  B -->|session/view ended or unavailable| R[Show preserved identity, last state, and recovery options]\n  R -->|reconnect or reopen succeeds| A\n  R -->|cancel| X([Keep layout; do not retarget])\n  C --> D{Entry integrity and redaction approved?}\n  D -->|yes| E[Show permitted detail and source links]\n  D -->|no, redacted, malformed, or inaccessible| F[Show state and safe recovery/filter action]\n  F --> C"
        }
      ],
      "sourceSha256": "f06ab6e2e7d234cbcfa463c33cf2f6864943ac430b970335379bceaa9e03a6db"
    },
    {
      "id": "spec-app-facelift",
      "path": "docs/specs/app-facelift.md",
      "title": "Application Facelift — styling, icons & menu system (spec)",
      "type": "spec",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-02-27",
      "reviewSuggested": [],
      "summary": "Specifies the visual facelift of the AI-DE workbench — an evolution from strict-flat to soft rounded \"islands\" (subtle elevation, larger radii, Fluent theme + DWM), a consistent icon system, and a discoverable menu + command system — without weakening the evidence-first density or the WCAG 2.2 AA / confidence-not-colour-alone floors.",
      "tags": [
        "facelift",
        "styling",
        "soft-islands",
        "icons",
        "menu",
        "wpf",
        "fluent"
      ],
      "links": [
        {
          "to": "spec-ai-native-ide",
          "rel": "refines"
        },
        {
          "to": "kb-wpf-modern-ui-styling",
          "rel": "implements"
        },
        {
          "to": "kb-ai-native-ide-shell",
          "rel": "relates-to"
        }
      ],
      "diagrams": [
        {
          "kind": "flowchart",
          "title": "Part B — UX specification (how it works)",
          "mermaid": "flowchart TD\n  A[Operator opens app] --> B{Wants an action}\n  B -->|knows the shortcut| C[Command palette / shortcut] --> Z[Action runs]\n  B -->|browsing| D[Menu bar] --> E{Action enabled?}\n  E -->|yes| Z\n  E -->|no, disabled| F[Hover shows reason] --> D\n  B -->|change look| G[View menu → Theme] --> H[Dark/Light/High-contrast]\n  H --> I{Applied?} -->|yes| J[Instant re-theme, announced] \n  I -->|token fails AA| K[Blocked at build by contrast audit — never ships]"
        }
      ],
      "sourceSha256": "b9c49e5aaf87e7290f150837b4cdad32506f2b3d7ae9846832d74fd67e7b11a4"
    },
    {
      "id": "spec-knowledge-exploration",
      "path": "docs/specs/knowledge-exploration.md",
      "title": "Knowledge Exploration Surface (spec)",
      "type": "spec",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-02-27",
      "reviewSuggested": [],
      "summary": "Specifies the knowledge exploration surface — one traversable graph over every repo artifact (code, knowledge, specs, architecture, generated artifacts) with a 2D/3D toggle, node introspection that renders each node in its natural form (md/html rendered, code in a syntax-highlighted editor), and visualizations grounded in standard UML/ERM notation.",
      "tags": [
        "knowledge-graph",
        "exploration",
        "traversal",
        "2d-3d",
        "node-introspection",
        "uml",
        "erm"
      ],
      "links": [
        {
          "to": "spec-ai-native-ide",
          "rel": "refines"
        },
        {
          "to": "kb-graph-experience-and-visualization",
          "rel": "implements"
        },
        {
          "to": "kb-editor-and-content-rendering-surfaces",
          "rel": "implements"
        },
        {
          "to": "conceptual-model-ai-native-ide",
          "rel": "relates-to"
        }
      ],
      "diagrams": [
        {
          "kind": "flowchart",
          "title": "Part B — UX specification (how it works)",
          "mermaid": "flowchart TD\n  A[Open explorer] --> B[Search or pick a start node]\n  B --> C[Bounded neighbourhood renders in 2D]\n  C --> D{Select a node}\n  D --> E[Introspection panel routes by type]\n  E -->|code| F[Syntax-highlighted read-only editor]\n  E -->|knowledge/md| G[Rendered markdown]\n  E -->|html| H[Rendered html]\n  E -->|diagram| I[Diagram pane]\n  E --> J[List typed edges with provenance]\n  J -->|select edge| K[Focus moves to target - the node-walk] --> C\n  C --> L{Toggle 3D?}\n  L -->|yes| M[3D force layout, selection preserved] --> C\n  C --> N{Structural view?}\n  N -->|UML/ERM| O[Standard-notation view over the neighbourhood]\n  D -->|no neighbours| P[Explicit empty neighbourhood state]\n  C -->|too large| Q[Bounded 'showing N of M' + expand]\n  M -->|occlusion/lost| R[Return to 2D preserves node] --> C"
        }
      ],
      "sourceSha256": "bf499178f876e20f5778e96d3f18fdd04c36cb873b31c79fb41bd5d84f4dbabc"
    },
    {
      "id": "spec-knowledge-explorer-mode",
      "path": "docs/specs/knowledge-explorer-mode.md",
      "title": "Knowledge Explorer — full-window dual-pane mode (spec)",
      "type": "spec",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-02-28",
      "reviewSuggested": [],
      "summary": "Refines the Knowledge Exploration Surface into a distinct full-window \"mode\": a rail icon slides the exploration open as a body-wide dual-pane surface (graph + search on one side, a per-kind node reader on the other) instead of a docked pane, so exploration and reading a node's contents fit comfortably on one screen. The graph, reader, per-kind rendering and node-walk are inherited from spec-knowledge-exploration; this spec adds only the presentation mode and the two-pane composition.",
      "tags": [
        "knowledge-graph",
        "exploration",
        "reader",
        "dual-pane",
        "view-mode",
        "node-walk",
        "wpf"
      ],
      "links": [
        {
          "to": "spec-knowledge-exploration",
          "rel": "refines"
        },
        {
          "to": "mockup-graph-canvas",
          "rel": "relates-to"
        },
        {
          "to": "kb-editor-and-content-rendering-surfaces",
          "rel": "implements"
        }
      ],
      "diagrams": [
        {
          "kind": "flowchart",
          "title": "Part B — UX specification (how it works)",
          "mermaid": "flowchart TD\n  W[Workbench mode] -->|activate Graph rail item| EOpen{Graph already loaded?}\n  EOpen -->|yes, live| E[Explorer: restore split + last node]\n  EOpen -->|no| EL[Explorer: graph loading state + reader empty state]\n  EL -->|overview arrives| E\n  E -->|select node in graph| R{Render node by kind}\n  R -->|md/html/code/text ok| RR[Reader shows content + metadata + typed edges]\n  R -->|missing / unknown kind / oversized| RE[Reader error/fallback + recovery: pick another node / open source]\n  RR -->|activate a typed edge| WALK[Graph re-focuses target within transport bound]\n  WALK --> R\n  RE -->|select a different node| R\n  RR -->|drag split| RS[Split ratio persists]\n  E -->|activate Graph rail item OR Escape unclaimed| W\n  E -->|narrow window| NAR[Stacked split / reader drawer US-E8]"
        }
      ],
      "sourceSha256": "99617318b85e83c23b47fba0c33a5162f08f3fdfe91172827972242fe1740025"
    },
    {
      "id": "spec-terminal-sessions",
      "path": "docs/specs/terminal-sessions.md",
      "title": "Multiple terminal sessions — lifecycle, rename, tab colour & colour schemes (spec)",
      "type": "spec",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-02-27",
      "reviewSuggested": [],
      "summary": "Specifies correct behaviour for multiple concurrent terminal sessions: a first-class \"New terminal\" (plain shell) action distinct from \"New agent terminal\"; sessions that are never destroyed except by explicit user intent (the fix for INV-0002 / DC-029, where any layout mutation killed every live terminal); and per-session identity, rename, tab colour, and ANSI colour-scheme customization. The domain model makes session-identity-preservation an aggregate invariant. Core owns the lifecycle fix and the \"New terminal\" command; Design owns the rename/colour/scheme UI.",
      "tags": [
        "terminal",
        "sessions",
        "lifecycle",
        "customization",
        "tab-colour",
        "colour-scheme",
        "wpf"
      ],
      "links": [
        {
          "to": "spec-ai-native-ide",
          "rel": "refines"
        },
        {
          "to": "spec-app-facelift",
          "rel": "relates-to"
        },
        {
          "to": "inv-0002-terminal-rebuild-kills-sessions",
          "rel": "depends-on"
        },
        {
          "to": "architecture",
          "rel": "relates-to"
        }
      ],
      "diagrams": [
        {
          "kind": "flowchart",
          "title": "User flows",
          "mermaid": "flowchart TD\n  subgraph Create\n    A[User wants a terminal] --> B{Plain or agent?}\n    B -->|plain| C[New terminal] --> D[Shell session opens, titled 'Terminal']\n    B -->|agent| E[New agent terminal…] --> F{Any agent on PATH?}\n    F -->|no| G[Message: no agent CLI found + how to install]\n    F -->|yes| H[Agent session opens, titled after the agent]\n  end\n  D --> I[Both/all sessions run independently]\n  H --> I"
        },
        {
          "kind": "flowchart",
          "title": "User flows",
          "mermaid": "flowchart TD\n  J[User right-clicks a terminal tab] --> K{Action}\n  K -->|Rename| L[Inline edit] --> M{Non-empty?}\n  M -->|yes| N[Tab shows new name; identity unchanged]\n  M -->|no| O[Rejected; prior name kept + message]\n  K -->|Tab colour| P[Pick swatch or None] --> Q[Accent applied; text still AA]\n  K -->|Colour scheme| R[Pick named preset] --> S[This session re-renders with scheme; others unaffected]\n  K -->|Close| T[Confirm if session is busy] --> U[Only this session ends]"
        },
        {
          "kind": "flowchart",
          "title": "User flows",
          "mermaid": "flowchart TD\n  V[Any layout mutation: open/close pane, split, restore, resize] --> W[Adapter reconciles by ContentId]\n  W --> X[Unchanged surfaces reuse their live content]\n  X --> Y[Every running session survives, identity + process intact]"
        }
      ],
      "sourceSha256": "5c4a5d26f7afc3febf9e99a480650a6aadbbd2f836665ae40a3bc4941f617ce7"
    },
    {
      "id": "spec-uml-erm-surfaces",
      "path": "docs/specs/uml-erm-surfaces.md",
      "title": "UML & ERM Surfaces (spec)",
      "type": "spec",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "",
      "reviewBy": "2027-02-27",
      "reviewSuggested": [],
      "summary": "Specifies first-class UML and ERM surfaces generated as read-only views of the repo graph — C4/class/component/sequence UML and crow's-foot ER diagrams — with notation validity enforced, the derived-view (never-editable) rule preserved, and polished visualization that composes with the knowledge exploration surface.",
      "tags": [
        "uml",
        "erm",
        "c4",
        "class-diagram",
        "er-diagram",
        "derived-views",
        "structurizr",
        "mermaid"
      ],
      "links": [
        {
          "to": "spec-ai-native-ide",
          "rel": "refines"
        },
        {
          "to": "spec-knowledge-exploration",
          "rel": "relates-to"
        },
        {
          "to": "kb-uml-mde-and-4gl",
          "rel": "implements"
        },
        {
          "to": "kb-domain-modeling-and-erm",
          "rel": "implements"
        },
        {
          "to": "kb-diagram-generation",
          "rel": "implements"
        },
        {
          "to": "conceptual-model-ai-native-ide",
          "rel": "relates-to"
        }
      ],
      "diagrams": [
        {
          "kind": "flowchart",
          "title": "Part B — UX specification (how it works)",
          "mermaid": "flowchart TD\n  A[Open Model surface] --> B[Pick a view: C4 / UML class / UML component / ER]\n  B --> C{Scope}\n  C --> D[Select system / bounded context]\n  D --> E[Generate view from graph]\n  E --> F{Generation ok?}\n  F -->|yes| G[Notation-valid diagram renders]\n  F -->|no| H[Bounded error: 'could not generate' + reason; last-known marked stale]\n  G --> I{C4 level switch?}\n  I -->|context/container/component| E\n  G --> J{Drill?}\n  J -->|select element| K[Jump to node in knowledge explorer - node-walk]\n  G --> L{Attempt edit?}\n  L -->|yes| M[Read-only: 'this is a derived view; edit the source' + link to code]\n  G -->|too large at this level| N[Curation applied: 'showing curated view; N elements folded']"
        }
      ],
      "sourceSha256": "6c93704c427bfade09fe56d333323118227d06b282a0fb2d6f9a841119e947cb"
    },
    {
      "id": "threat-model-ai-native-ide",
      "path": "docs/security/ai-native-ide-threat-model.md",
      "title": "AI-DE threat model",
      "type": "threat-model",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "0",
      "reviewBy": "2027-02-21",
      "reviewSuggested": [
        {
          "by": "architecture",
          "on": "2026-08-25",
          "reason": "Defined the AI-DE workspace daemon, fact-store, MCP, terminal, and vertical delivery architecture"
        },
        {
          "by": "spec-ai-native-ide",
          "on": "2026-08-26",
          "reason": "US-9 dockable workbench added; archetype corrected to Layout:MultiPanelWorkstation + Persistence:LocalDevice"
        }
      ],
      "summary": "Disposes STRIDE threats across workspace IPC, filesystem identity, terminal and rendering content, prompt delivery, MCP, audit evidence, and dependency acquisition with required negative controls.",
      "tags": [
        "stride",
        "security",
        "mcp",
        "terminal",
        "workspace"
      ],
      "links": [
        {
          "to": "architecture",
          "rel": "documents"
        },
        {
          "to": "spec-ai-native-ide",
          "rel": "implements"
        },
        {
          "to": "privacy-review-ai-native-ide",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "b8bd5ee4e4b5516c7b3bf7bb0994e6016d7bea411c248a0302d8f077639b5cb0"
    }
  ],
  "surfaces": [
    {
      "id": "surface-audit-index",
      "path": "docs/audit/index.html",
      "title": "ai-de-facelift — Audit & Change Log",
      "kind": "audit",
      "description": "Browse the committed audit and change timeline.",
      "artifactId": "audit-log"
    },
    {
      "id": "surface-mockups-activity-rail",
      "path": "docs/mockups/activity-rail.html",
      "title": "Activity rail — elevate mockup",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "mockup-activity-rail"
    },
    {
      "id": "surface-mockups-facelift-elevate",
      "path": "docs/mockups/facelift-elevate.html",
      "title": "AI-DE facelift — elevate proposals (visualization)",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "mockup-facelift-elevate"
    },
    {
      "id": "surface-mockups-app-facelift",
      "path": "docs/mockups/app-facelift.html",
      "title": "AI-DE Facelift — soft-islands shell (mockup)",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "mockup-app-facelift"
    },
    {
      "id": "surface-mockups-workbench",
      "path": "docs/mockups/workbench.html",
      "title": "AI-DE Workbench — mockup",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "mockup-workbench"
    },
    {
      "id": "surface-specs-ai-native-ide",
      "path": "docs/specs/ai-native-ide.html",
      "title": "AI-native IDE — Product specification",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "spec-ai-native-ide"
    },
    {
      "id": "surface-mockups-context-map-join",
      "path": "docs/mockups/context-map-join.html",
      "title": "Context Map & Join surfaces — Core→Design §4a (mockup)",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "mockup-context-map-join"
    },
    {
      "id": "surface-mockups-graph-canvas",
      "path": "docs/mockups/graph-canvas.html",
      "title": "Graph canvas — target UX",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "mockup-graph-canvas"
    },
    {
      "id": "surface-mockups-knowledge-explorer-mode",
      "path": "docs/mockups/knowledge-explorer-mode.html",
      "title": "Knowledge Explorer mode — mockup",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "mockup-knowledge-explorer-mode"
    },
    {
      "id": "surface-mockups-knowledge-explorer",
      "path": "docs/mockups/knowledge-explorer.html",
      "title": "Knowledge Explorer — graph + node introspection (mockup)",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "mockup-knowledge-explorer"
    },
    {
      "id": "surface-mockups-uml-erm-surfaces",
      "path": "docs/mockups/uml-erm-surfaces.html",
      "title": "UML & ERM Surfaces — derived views (mockup)",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "mockup-uml-erm-surfaces"
    }
  ],
  "graphSha256": "4c8d977099a265e970b13ea8c284af4f80b05ea7cad4cd24996d757d8ceff439"
};
