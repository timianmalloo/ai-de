// Derived from artifact frontmatter by scripts/docs-graph.py — DO NOT hand-edit (frontmatter wins; see knowledge-visualization.md V2/V18).
window.DOCS_INDEX = {
  "schemaVersion": "docs-index/v2",
  "project": "ai-de-session-phase3-pane-probes",
  "generated": "2026-08-31T23:07:40Z",
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
      "id": "adr-0017-watcher-observation-projection",
      "path": "docs/adr/0017-watcher-observation-projection.md",
      "title": "ADR-0017 — Loomkeeper observes as a projection over the shared fact store, not a second database",
      "type": "adr",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "discovery",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "Loomkeeper adds harness/model dimensions and watcher facts (span, board message, work episode, evidence, scorecard, daydream observation) to the existing ADR-0002 SQLite fact store and computes liveness, Weave, and the leaderboard as ADR-0001 derived views, rather than owning a second store.",
      "tags": [
        "architecture",
        "loomkeeper",
        "facts",
        "dimensions",
        "projection",
        "observability"
      ],
      "links": [
        {
          "to": "architecture-loomkeeper",
          "rel": "implements"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "implements"
        },
        {
          "to": "adr-0002-workspace-fact-store",
          "rel": "refines"
        },
        {
          "to": "adr-0001-derived-evidence-views",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "324e75068c16ec719d8a2d162d0c687cd567c925c4d88c0d8031f662af2ac4c0"
    },
    {
      "id": "adr-0018-credential-backed-grading-egress",
      "path": "docs/adr/0018-credential-backed-grading-egress.md",
      "title": "ADR-0018 — Credentials are DPAPI local secrets and off-device grading is an opt-in egress path",
      "type": "adr",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "discovery",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "Loomkeeper credentials are sealed with DPAPI CurrentUser and never logged or emitted; outbound network is denied by default; credential-backed off-device grading is an ADR-0011 ExternalProcessing egress path that stays blocked until an explicit, revocable, per-path opt-in reclassifies it.",
      "tags": [
        "architecture",
        "loomkeeper",
        "security",
        "privacy",
        "egress",
        "credentials",
        "dpapi"
      ],
      "links": [
        {
          "to": "architecture-loomkeeper",
          "rel": "implements"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "implements"
        },
        {
          "to": "adr-0011-session-processing-class-egress",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "967858353b15a2c3f5c665c8b42043f852ad8d856de56ade35105dc23d80cad7"
    },
    {
      "id": "adr-0019-advisory-evaluator-calibration",
      "path": "docs/adr/0019-advisory-evaluator-calibration.md",
      "title": "ADR-0019 — Advisory dimensions and the leaderboard require calibrated, held-out-validated evaluators",
      "type": "adr",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "discovery",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "A model-graded dimension contributes score points only after its evaluator version passes stability (>=95% same 0-4 band over 20 runs) and human agreement (quadratic weighted kappa >=0.75) on separate versioned corpora; leaderboard ranks are scoped to one calibrated task class and score schema, and anti-Goodhart counter-metrics gate whether a score rise counts as improvement.",
      "tags": [
        "architecture",
        "loomkeeper",
        "scoring",
        "evaluation",
        "calibration",
        "leaderboard",
        "ai-systems"
      ],
      "links": [
        {
          "to": "architecture-loomkeeper",
          "rel": "implements"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "implements"
        },
        {
          "to": "kb-agentic-session-observability",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "5abb6588cbfeb19126e3e8cc9b885fc86599a0bd076d572827cf92a11abae1a3"
    },
    {
      "id": "adr-0020-trusted-registrar-harness-model-identity",
      "path": "docs/adr/0020-trusted-registrar-harness-model-identity.md",
      "title": "ADR-0020 — A trusted registrar binds harness/model identity and issues a per-session capability",
      "type": "adr",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "discovery",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "Registration binds repository/worktree/terminal/agent/harness/model/session-generation and issues a per-session capability verified on every event; asserted identity is labelled and cannot clear a floor; non-AI-Forward sessions get an injected coordination contract while AI-Forward sessions reuse the coord-core records rather than a second ledger.",
      "tags": [
        "architecture",
        "loomkeeper",
        "identity",
        "registration",
        "capability",
        "harness",
        "model"
      ],
      "links": [
        {
          "to": "architecture-loomkeeper",
          "rel": "implements"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "implements"
        },
        {
          "to": "adr-0007-agent-session-adapter",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "fc184c3983c4eacb54336a4b7e13680da7ca551c7664768f7866a48a3f52df97"
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
      "sourceSha256": "1bcbc703fc47da2f24c24d07ae93b0c3c3709704678f996ac51610c5db0a8dda"
    },
    {
      "id": "architecture-loomkeeper",
      "path": "docs/architecture/loomkeeper.md",
      "title": "Loomkeeper Watcher Substrate - Architecture",
      "type": "architecture",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "discovery",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "Top-level architecture for Loomkeeper, the local agentic watcher subsystem. It observes many terminal-agent sessions across repositories by composing the existing AI-DE fact store, derived views, delivery semantics, session adapter, and egress governance, and adds a trusted registrar, harness/model attribution, a calibrated advisory evaluator, a leaderboard, per-turn standing feedback, and a human-gated Daydream learning loop - local-only by default.",
      "tags": [
        "loomkeeper",
        "agent-observability",
        "architecture",
        "scoring",
        "leaderboard",
        "daydream",
        "watcher"
      ],
      "links": [
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "implements"
        },
        {
          "to": "architecture",
          "rel": "refines"
        },
        {
          "to": "kb-agentic-session-observability",
          "rel": "depends-on"
        },
        {
          "to": "adr-0002-workspace-fact-store",
          "rel": "depends-on"
        },
        {
          "to": "adr-0001-derived-evidence-views",
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
          "to": "adr-0011-session-processing-class-egress",
          "rel": "depends-on"
        },
        {
          "to": "adr-0016-bounded-context-declaration",
          "rel": "depends-on"
        },
        {
          "to": "adr-0017-watcher-observation-projection",
          "rel": "depends-on"
        },
        {
          "to": "adr-0018-credential-backed-grading-egress",
          "rel": "depends-on"
        },
        {
          "to": "adr-0019-advisory-evaluator-calibration",
          "rel": "depends-on"
        },
        {
          "to": "adr-0020-trusted-registrar-harness-model-identity",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "6c61ff31a5e56c22b9165b075b48c2658ed6a74adcfe00105d6cb3c860d8bfce"
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
      "id": "note-conn-10-11-episode-source-blocker",
      "path": "docs/notes/conn-10-11-episode-source-blocker.md",
      "title": "conn-10/conn-11 are blocked on an episode-lifecycle source + verification telemetry",
      "type": "decision-note",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "2",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "conn-10 (auto-score-on-close) and conn-11 (raise-dispute + cloud judge) cannot ship honestly yet: no terminal session opens a goal/done-when Work Episode, and there is no telemetry convention for observing a verification path - so a deterministic signals deriver could only ever return HasVerificationPath=false, which the scorer correctly renders Not-Scored, and disputes have no scored episode to target. Deferring both behind an episode-lifecycle capture slice rather than fabricating signals (spec L127; no-guessing).",
      "tags": [
        "loomkeeper",
        "watcher",
        "scoring",
        "dispute",
        "blocker",
        "conn-10",
        "conn-11",
        "decision-note"
      ],
      "links": [
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "relates-to"
        },
        {
          "to": "design-watcher-weave-score",
          "rel": "relates-to"
        },
        {
          "to": "design-watcher-session-emitter",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "298403503f40f940c45953096e643b6c7e4ed02fb37be09cdaf441b107d7a1d1"
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
      "id": "note-watcher-substrate-framing",
      "path": "docs/notes/watcher-substrate-framing.md",
      "title": "Loomkeeper framing, score authority, and Observatory archetype",
      "type": "decision-note",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "discovery",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "Records the decision to name the watcher Loomkeeper, keep deterministic facts authoritative over advisory model judgments, human-gate Daydream promotion, and use a G6 evidence-led Observatory inside the existing AI-DE workbench.",
      "tags": [
        "decision-note",
        "loomkeeper",
        "scoring",
        "ui-archetype",
        "privacy"
      ],
      "links": [
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "relates-to"
        },
        {
          "to": "kb-agentic-session-observability",
          "rel": "depends-on"
        },
        {
          "to": "mockup-watcher-observatory",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "c20b395a9957338b90358af9b8af08d35c4cb32863c8b29e530c097d41878c78"
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
      "id": "design-watcher-advisory-evaluator",
      "path": "docs/design/watcher-advisory-evaluator.md",
      "title": "Loomkeeper - Local Advisory Evaluator & Egress Guard (connective 3)",
      "type": "design",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "4",
      "reviewBy": "2027-02-28",
      "reviewSuggested": [],
      "summary": "Implement the IAdvisoryEvaluator seam two ways: a deterministic LOCAL heuristic evaluator that scores the two advisory dimensions from a quarantined evidence token list with a conservative default (needs no model, credential, or egress - the safe smoke-test default), and an EgressGuardedAdvisoryEvaluator that enforces default-deny egress (LK-0003) THEN a present credential (LK-0002) before any egressing cloud judge can run (ADR-0018), never calling the inner evaluator when either check fails. The real cloud model call stays a seam behind the guard - a local smoke test uses the local evaluator.",
      "tags": [
        "loomkeeper",
        "watcher",
        "advisory",
        "evaluator",
        "egress",
        "credential",
        "adr-0018",
        "phase-4"
      ],
      "links": [
        {
          "to": "design-watcher-advisory-grader",
          "rel": "depends-on"
        },
        {
          "to": "adr-0018-credential-backed-grading-egress",
          "rel": "implements"
        },
        {
          "to": "architecture-loomkeeper",
          "rel": "implements"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "implements"
        }
      ],
      "diagrams": [],
      "sourceSha256": "be3bcdad905ba7bf7fea9404ea36391fb9630f26237435f961121ff6e6c829dd"
    },
    {
      "id": "design-watcher-advisory-grader",
      "path": "docs/design/watcher-advisory-grader.md",
      "title": "Loomkeeper Advisory Grader - Calibration Gates, Leaderboard, Standing",
      "type": "design",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "4",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "Design for the Loomkeeper advisory grader (slice 7, final). The deterministic cores: the ADR-0019 calibration gates (stability >=95% band consistency with spread <=1, quadratic weighted kappa >=0.75 vs human labels, and anti-Goodhart counter-metrics that must not worsen) that decide whether an advisory evaluator version may contribute points; the gated fold of a qualified advisory dimension into the Weave (never overriding a deterministic dimension); the leaderboard (cohort >=5 or Not Comparable, segmented by task class + score schema version, per harness/model/harness-model, non-identifying); and per-turn agent standing (rank + trend + one evidence reason per dimension, no single optimizable scalar). The model judge itself sits behind an IAdvisoryEvaluator seam.",
      "tags": [
        "loomkeeper",
        "watcher",
        "design",
        "advisory",
        "calibration",
        "kappa",
        "leaderboard",
        "standing",
        "phase-4"
      ],
      "links": [
        {
          "to": "architecture-loomkeeper",
          "rel": "implements"
        },
        {
          "to": "design-watcher-weave-score",
          "rel": "refines"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "implements"
        },
        {
          "to": "adr-0019-advisory-evaluator-calibration",
          "rel": "depends-on"
        },
        {
          "to": "adr-0018-credential-backed-grading-egress",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "9b7c531d475e24574be847f70c0c5b79ec52c466c83d7afed07118ffb1669318"
    },
    {
      "id": "design-watcher-board-leaderboard-surfaces",
      "path": "docs/design/watcher-board-leaderboard-surfaces.md",
      "title": "Loomkeeper - Board & Leaderboard WPF Surfaces (connective 2)",
      "type": "design",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "4",
      "reviewBy": "2027-02-28",
      "reviewSuggested": [],
      "summary": "Two new WPF read surfaces - Message Board (US-4) and Leaderboard (US-14) - built exactly like the slice-3 Sessions pane: a synchronous store-fold view model in AiDe.Core.Presentation behind a null-safe query seam, rendered by SurfaceContentFactory, seeded into the default layout and added to existing layouts by a v2->v3 migration so they are reachable (E10). WorkbenchShell now opens the per-workspace watcher SQLite store and wires all three read queries, so the panes render live when the ingest host has written data and degrade to an honest \"not available\" when the store is absent.",
      "tags": [
        "loomkeeper",
        "watcher",
        "wpf",
        "surface",
        "board",
        "leaderboard",
        "standing",
        "ui",
        "phase-4"
      ],
      "links": [
        {
          "to": "design-watcher-sessions-surface",
          "rel": "refines"
        },
        {
          "to": "design-watcher-score-persistence",
          "rel": "depends-on"
        },
        {
          "to": "design-watcher-message-board",
          "rel": "depends-on"
        },
        {
          "to": "design-watcher-advisory-grader",
          "rel": "depends-on"
        },
        {
          "to": "architecture-loomkeeper",
          "rel": "implements"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "implements"
        }
      ],
      "diagrams": [],
      "sourceSha256": "2ea59168999401ccd1495966b00711bce1cfc874ed38b5d8b326a7000ad05feb"
    },
    {
      "id": "design-watcher-coordination-contract",
      "path": "docs/design/watcher-coordination-contract.md",
      "title": "Loomkeeper Injected Coordination Contract - Non-Pack Ingest Adapter",
      "type": "design",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "1",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "Design for the Loomkeeper injected coordination contract (slice 2): a versioned, coord-core-append schema that lets a session from a repository WITHOUT the AI-Forward pack register and heartbeat over the same append-only ledger (one ledger, projected, not duplicated). A pure CoordContractParser reads the JSONL tolerantly (LOG-A leading newline, CRLF, blank/malformed skip, version pin, sort by at/seq), and an InjectedContractIngest adapter mints the capability at register, holds external-id->capability, and feeds the same TrustedRegistrar/IngestHost as the OTLP path. Contract established by spike S4.",
      "tags": [
        "loomkeeper",
        "watcher",
        "design",
        "coordination",
        "injected-contract",
        "coord-core",
        "phase-1"
      ],
      "links": [
        {
          "to": "architecture-loomkeeper",
          "rel": "implements"
        },
        {
          "to": "design-watcher-ingest-host",
          "rel": "refines"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "implements"
        },
        {
          "to": "adr-0020-trusted-registrar-harness-model-identity",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "c59220a166cbe3df078f1f9b9e55c38180b6ba522703271153e0ad47e00643ce"
    },
    {
      "id": "design-watcher-dispute-service",
      "path": "docs/design/watcher-dispute-service.md",
      "title": "Loomkeeper - Raise-Dispute API, Sessions Badge & Cloud-Judge Scaffold (connective 7)",
      "type": "design",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "4",
      "reviewBy": "2027-02-28",
      "reviewSuggested": [],
      "summary": "Close the US-16 fairness loop and make the model-judge seam concrete. DisputeService.RaiseDispute is the operator API that mints the dispute id + timestamp and appends the append-only fact (requiring a reason). A session is Disputed iff any of its episodes carries a dispute (DM7), surfaced as a no-colour-alone badge on the Sessions row and computed by the sessions query. DelegatingAdvisoryEvaluator is the cloud-judge scaffold: an IAdvisoryEvaluator that delegates the 0-4 rubric to an injected model call and is placed inside the EgressGuardedAdvisoryEvaluator, so the network call only happens after the ADR-0018 egress opt-in + credential check pass.",
      "tags": [
        "loomkeeper",
        "watcher",
        "dispute",
        "sessions",
        "badge",
        "cloud-judge",
        "adr-0018",
        "phase-4"
      ],
      "links": [
        {
          "to": "design-watcher-score-dispute",
          "rel": "refines"
        },
        {
          "to": "design-watcher-sessions-surface",
          "rel": "refines"
        },
        {
          "to": "design-watcher-advisory-evaluator",
          "rel": "depends-on"
        },
        {
          "to": "architecture-loomkeeper",
          "rel": "implements"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "implements"
        }
      ],
      "diagrams": [],
      "sourceSha256": "fbb52b9329b1f4c843ce6366b425da52a31da18240774100a941b2f500709d41"
    },
    {
      "id": "design-watcher-host",
      "path": "docs/design/watcher-host.md",
      "title": "Loomkeeper - In-Process Watcher Host (connective 5)",
      "type": "design",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "4",
      "reviewBy": "2027-02-28",
      "reviewSuggested": [],
      "summary": "Compose the observation store, trusted registrar, ingest host, injected coordination-contract ingest + log pump, and (best-effort) the OTLP receiver into one WatcherHost, and run it IN THE WPF APP PROCESS. Running the ingest beside the read surfaces makes liveness exact (the registrar and liveness projection share one process-global monotonic clock), which is the cross-process caveat conn-2 recorded, now removed. The host drains the coordination-contract log on a 2s background loop so a session that writes a register/heartbeat log appears live without a restart. This is the next-step that turns the panes from live-capable into live.",
      "tags": [
        "loomkeeper",
        "watcher",
        "host",
        "ingest",
        "coordination",
        "liveness",
        "in-process",
        "phase-4"
      ],
      "links": [
        {
          "to": "design-watcher-otlp-receiver",
          "rel": "depends-on"
        },
        {
          "to": "design-watcher-coordination-contract",
          "rel": "depends-on"
        },
        {
          "to": "design-watcher-board-leaderboard-surfaces",
          "rel": "refines"
        },
        {
          "to": "architecture-loomkeeper",
          "rel": "implements"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "implements"
        }
      ],
      "diagrams": [],
      "sourceSha256": "9f3a01beb28528a18ae915f142fe03bcc2b2f90c4138ef60860d6427a552d27c"
    },
    {
      "id": "design-watcher-ingest-host",
      "path": "docs/design/watcher-ingest-host.md",
      "title": "Loomkeeper Ingest Host - Bounded Queue and Drain Loop",
      "type": "design",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "1",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "Design for the Loomkeeper ingest host (slice 1): synchronous registration/heartbeat plus an async, bounded span queue (Channel.CreateBounded + DropOldest backpressure) drained into OtelSpanMapper -> TrustedRegistrar/SpanIngest, with forged spans rejected, malformed events quarantined, and counters exposing the operator questions. Transport is a substitutable IHarnessEventSource port; the OTLP network receiver is a follow-on adapter (slice 1b).",
      "tags": [
        "loomkeeper",
        "watcher",
        "design",
        "ingest",
        "host",
        "backpressure",
        "phase-1"
      ],
      "links": [
        {
          "to": "architecture-loomkeeper",
          "rel": "implements"
        },
        {
          "to": "design-watcher-ingest-wire",
          "rel": "refines"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "implements"
        },
        {
          "to": "adr-0020-trusted-registrar-harness-model-identity",
          "rel": "depends-on"
        },
        {
          "to": "adr-0002-workspace-fact-store",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "2a6e6fed3056569095f9808007d5dfc1c55e747a4a5a8c7df6e5ddeed6a2b62d"
    },
    {
      "id": "design-watcher-ingest-wire",
      "path": "docs/design/watcher-ingest-wire.md",
      "title": "Loomkeeper Ingest Wire - Harness Telemetry to Observation",
      "type": "design",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "1",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "Design for the Loomkeeper ingest wire: a dual-path adapter that turns harness telemetry - native OTel spans and a registration/session-start event - into TrustedRegistrar registrations and capability-verified SpanIngest calls. Its deterministic core is a pure OtelSpanMapper (built now); the OTLP transport receiver and daemon host remain. Contract established by spike S1.",
      "tags": [
        "loomkeeper",
        "watcher",
        "design",
        "ingest",
        "otlp",
        "adapter",
        "phase-1"
      ],
      "links": [
        {
          "to": "architecture-loomkeeper",
          "rel": "implements"
        },
        {
          "to": "design-watcher-phase1-skeleton",
          "rel": "refines"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "implements"
        },
        {
          "to": "adr-0020-trusted-registrar-harness-model-identity",
          "rel": "depends-on"
        },
        {
          "to": "adr-0017-watcher-observation-projection",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "019d9cfedee12d87e29eb6c749715ff3d1dbf27ff6789aeb74fa4e909597e989"
    },
    {
      "id": "design-watcher-live-refresh",
      "path": "docs/design/watcher-live-refresh.md",
      "title": "Loomkeeper Live Pane Auto-Refresh (conn-9)",
      "type": "design",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "2",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "The watcher read panes (sessions/board/leaderboard) re-render live when the observation store changes - a session registering/ending, a board post, or a new score shows up without a manual reopen - gated by a cheap store fingerprint so an idle watcher never gratuitously rebuilds a pane (no scroll reset/flicker).",
      "tags": [
        "loomkeeper",
        "watcher",
        "design",
        "refresh",
        "liveness",
        "ux",
        "conn-9",
        "phase-2"
      ],
      "links": [
        {
          "to": "architecture-loomkeeper",
          "rel": "implements"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "implements"
        },
        {
          "to": "design-watcher-session-emitter",
          "rel": "depends-on"
        },
        {
          "to": "design-watcher-sessions-surface",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "9db103e3332bf32e809ad63d5a14aa319ca20ec17a09203226e14a54e801f9c6"
    },
    {
      "id": "design-watcher-message-board",
      "path": "docs/design/watcher-message-board.md",
      "title": "Loomkeeper Message Board + Fleet Aggregator",
      "type": "design",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "3",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "Design for the Loomkeeper Message Board + Fleet aggregator (slice 6). The board is a per-repository, append-only communication surface (Question / Decision / Breadcrumb / Knowledge Candidate + Reply / Acknowledgement) with author/session/time/trust provenance; a reply/ack must reference an existing parent (no orphan thread); all content is quarantined untrusted data that cannot instruct a grader, and grader-injection shapes (score 100 / ignore the rubric / promote this lesson) are flagged; policy deletion redacts the payload but keeps the immutable envelope as a tombstone. The Fleet aggregator builds the repo->session map across >=2 stores. Rides the coord-core append semantics.",
      "tags": [
        "loomkeeper",
        "watcher",
        "design",
        "message-board",
        "fleet",
        "cross-repo",
        "quarantine",
        "phase-3"
      ],
      "links": [
        {
          "to": "architecture-loomkeeper",
          "rel": "implements"
        },
        {
          "to": "design-watcher-sessions-surface",
          "rel": "depends-on"
        },
        {
          "to": "design-watcher-coordination-contract",
          "rel": "depends-on"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "implements"
        },
        {
          "to": "adr-0020-trusted-registrar-harness-model-identity",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "6fdf4b28bfea48ee8af4d2e88e47db4c5f38ef4a01921e529e1254f6155e3a58"
    },
    {
      "id": "design-watcher-otlp-receiver",
      "path": "docs/design/watcher-otlp-receiver.md",
      "title": "Loomkeeper OTLP Receiver - Transport Adapter",
      "type": "design",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "1",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "Design for the Loomkeeper OTLP receiver (slice 1b): a loopback HttpListener that accepts OTLP/JSON trace exports at /v1/traces, resolves a per-session bearer token to the session's capability, parses spans with stdlib System.Text.Json (no protobuf dependency), and enqueues them into the ingest host. Split into a pure OtlpJsonParser and thin OtlpHttpReceiver glue. Contract established by the slice-1b spike.",
      "tags": [
        "loomkeeper",
        "watcher",
        "design",
        "otlp",
        "receiver",
        "transport",
        "phase-1"
      ],
      "links": [
        {
          "to": "architecture-loomkeeper",
          "rel": "implements"
        },
        {
          "to": "design-watcher-ingest-host",
          "rel": "refines"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "implements"
        },
        {
          "to": "adr-0018-credential-backed-grading-egress",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "cd1fd8022e65e8c70e7d0d628e243ad8cc0ddd47b4fb34a54c964319deda8f19"
    },
    {
      "id": "design-watcher-phase1-skeleton",
      "path": "docs/design/watcher-phase1-skeleton.md",
      "title": "Loomkeeper Phase-1 Walking Skeleton - Deterministic Observation Core",
      "type": "design",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "1",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "Detailed design for the Loomkeeper Phase-1 walking skeleton: the deterministic T0 observation core - identity value objects with harness/model, a Trusted Registrar issuing per-session capabilities, content-addressed idempotent span ingest, monotonic liveness projection, and a default-deny egress gate - over an IWatcherObservationStore seam with an in-memory implementation. No personal data, no model, no network.",
      "tags": [
        "loomkeeper",
        "watcher",
        "design",
        "identity",
        "ingest",
        "liveness",
        "egress",
        "walking-skeleton"
      ],
      "links": [
        {
          "to": "architecture-loomkeeper",
          "rel": "implements"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "implements"
        },
        {
          "to": "adr-0020-trusted-registrar-harness-model-identity",
          "rel": "depends-on"
        },
        {
          "to": "adr-0017-watcher-observation-projection",
          "rel": "depends-on"
        },
        {
          "to": "adr-0018-credential-backed-grading-egress",
          "rel": "depends-on"
        },
        {
          "to": "adr-0002-workspace-fact-store",
          "rel": "depends-on"
        },
        {
          "to": "adr-0006-terminal-delivery-semantics",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "807705a3a65c94b27ae9d0b1385fec92878071c1eb7a3a6d8d0e4bde10249762"
    },
    {
      "id": "design-watcher-score-dispute",
      "path": "docs/design/watcher-score-dispute.md",
      "title": "Loomkeeper - Operator Dispute Path (connective 4)",
      "type": "design",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "4",
      "reviewBy": "2027-02-28",
      "reviewSuggested": [],
      "summary": "The operator dispute path (US-16 / spec rule 12): an operator records a ScoreDispute against a scored episode - an append-only fact that NEVER overwrites the Scorecard - and the episode's Disputed state is DERIVED from the presence of dispute facts (DM7), never a stored flag. Persisted in both stores (append-only trigger + idempotent id), read by a DisputeProjection, and surfaced as a disputed-episode count on the Leaderboard so a disputed score is discoverable from the surface (US-16).",
      "tags": [
        "loomkeeper",
        "watcher",
        "dispute",
        "fairness",
        "append-only",
        "us-16",
        "phase-4"
      ],
      "links": [
        {
          "to": "design-watcher-score-persistence",
          "rel": "depends-on"
        },
        {
          "to": "design-watcher-board-leaderboard-surfaces",
          "rel": "refines"
        },
        {
          "to": "architecture-loomkeeper",
          "rel": "implements"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "implements"
        },
        {
          "to": "adr-0002-workspace-fact-store",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "b48c6eb4f0bab2ac1e528f39a4bb57a209f33e4cdd98f7d22c85587902adbf59"
    },
    {
      "id": "design-watcher-score-persistence",
      "path": "docs/design/watcher-score-persistence.md",
      "title": "Loomkeeper - Scorecard & Leaderboard Persistence (connective 1)",
      "type": "design",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "4",
      "reviewBy": "2027-02-28",
      "reviewSuggested": [],
      "summary": "Persist a scored episode (ScoredEpisode + its Scorecard) as a MATERIALIZED DERIVED CACHE behind the existing IWatcherObservationStore seam, so the WPF Leaderboard/Standing surfaces read scored data without recomputing. The cache is a current-state cell (upsert, not append-only) because a recomputation must replace the prior card; it is rebuildable from (episode + signals) via WeaveScorer (DM7), and a round-trip test asserts persisted == in-memory == the value the scorer produced.",
      "tags": [
        "loomkeeper",
        "watcher",
        "persistence",
        "scorecard",
        "leaderboard",
        "materialized-cache",
        "sqlite",
        "phase-4"
      ],
      "links": [
        {
          "to": "design-watcher-weave-score",
          "rel": "depends-on"
        },
        {
          "to": "design-watcher-advisory-grader",
          "rel": "depends-on"
        },
        {
          "to": "architecture-loomkeeper",
          "rel": "implements"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "implements"
        },
        {
          "to": "adr-0002-workspace-fact-store",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "3285197bb91bd7151d61d1d1e0e0323be851d728de01776acbaaeda1ba6982a9"
    },
    {
      "id": "design-watcher-scoring-service",
      "path": "docs/design/watcher-scoring-service.md",
      "title": "Loomkeeper - Evidence Composer & Scoring Service (connective 6)",
      "type": "design",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "4",
      "reviewBy": "2027-02-28",
      "reviewSuggested": [],
      "summary": "Compose a closed episode's DeterministicEpisodeSignals into the local evaluator's evidence token string (EvidenceComposer), and turn (episode + signals + classification) into a persisted ScoredEpisode (ScoringService) so scored episodes reach the Leaderboard/Standing surfaces. The four deterministic dimensions are always scored; the two advisory dimensions fold only when the evaluator's (version, taskClass, schemaVersion) is qualified in the calibration registry (ADR-0019, rule 8); with no evaluator, only the deterministic Weave is recorded (the safe default).",
      "tags": [
        "loomkeeper",
        "watcher",
        "scoring",
        "evidence",
        "calibration",
        "advisory",
        "phase-4"
      ],
      "links": [
        {
          "to": "design-watcher-weave-score",
          "rel": "depends-on"
        },
        {
          "to": "design-watcher-advisory-grader",
          "rel": "depends-on"
        },
        {
          "to": "design-watcher-advisory-evaluator",
          "rel": "depends-on"
        },
        {
          "to": "design-watcher-score-persistence",
          "rel": "depends-on"
        },
        {
          "to": "architecture-loomkeeper",
          "rel": "implements"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "implements"
        }
      ],
      "diagrams": [],
      "sourceSha256": "001a6ae7f964e4116e8f95fe6e29656ca37ff25f7ff1ac113808deb52eeb9da3"
    },
    {
      "id": "design-watcher-session-emitter",
      "path": "docs/design/watcher-session-emitter.md",
      "title": "Loomkeeper Session Coordination Emitter - Auto-Emitting Session Wrapper (conn-8)",
      "type": "design",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "2",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "The app-side writer that makes a terminal/agent session appear in the watcher: a pure, testable SessionCoordinationEmitter (Register/Heartbeat/HeartbeatAll/End/Reconcile) over coordination-contract logs, plus the WorkbenchShell loop that reconciles the live terminal panes into coordination sessions and pumps them into the store. Also closes the session-end-that-never-ended liveness gap (DC-043).",
      "tags": [
        "loomkeeper",
        "watcher",
        "design",
        "coordination",
        "emitter",
        "session",
        "liveness",
        "conn-8",
        "phase-2"
      ],
      "links": [
        {
          "to": "architecture-loomkeeper",
          "rel": "implements"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "implements"
        },
        {
          "to": "design-watcher-coordination-contract",
          "rel": "depends-on"
        },
        {
          "to": "design-watcher-phase1-skeleton",
          "rel": "depends-on"
        },
        {
          "to": "adr-0020-trusted-registrar-harness-model-identity",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "4d7fa426a329999f8f649d2f37f09f737950a2ef7eb066148a47b783b856e177"
    },
    {
      "id": "design-watcher-sessions-surface",
      "path": "docs/design/watcher-sessions-surface.md",
      "title": "Loomkeeper Sessions Surface - WPF Treegrid Row",
      "type": "design",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "1",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "Design for the Loomkeeper Sessions surface (slice 3): the compute reader that closes the Phase-1 change-surface. A synchronous, deterministic projection folds the observation store + liveness into honest session rows (Not Recorded for an unproven harness/model, a no-colour-alone liveness badge), exposed by a testable WatcherSessionsPaneViewModel (in AiDe.Core/Presentation, mirroring EvidencePaneViewModel) with the full state set, and rendered by a \"sessions\" surface kind in the WPF workbench (G6 Multi-Panel Data Terminal), in the default layout so it is actually visible.",
      "tags": [
        "loomkeeper",
        "watcher",
        "design",
        "ui",
        "sessions",
        "wpf",
        "liveness",
        "phase-1"
      ],
      "links": [
        {
          "to": "architecture-loomkeeper",
          "rel": "implements"
        },
        {
          "to": "design-watcher-phase1-skeleton",
          "rel": "refines"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "implements"
        },
        {
          "to": "mockup-watcher-observatory",
          "rel": "refines"
        },
        {
          "to": "adr-0017-watcher-observation-projection",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "9e2f4a197ea31e0fbbc0cfd7e9d19602e8e53a9132b267a21c5ffad894676d57"
    },
    {
      "id": "design-watcher-weave-score",
      "path": "docs/design/watcher-weave-score.md",
      "title": "Loomkeeper Deterministic Weave - Score, Floors, Coverage",
      "type": "design",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "2",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "Design for the Loomkeeper deterministic Weave (slice 5): a pure scoring engine that evaluates a CLOSED Work Episode on the four deterministic dimensions (Outcome integrity 30, Focus & termination 15, Guidance adherence 15, Coordination & learning 10 = observed weight 70), leaving the two advisory dimensions (Evidence discipline, Solution economy = 30) excluded until the grader passes its calibration gates (slice 7). Hard floors (correctness, security, privacy, data integrity, evaluator integrity) trip a Blocked verdict and suppress the numeric headline; a missing goal/done/verification path is Not Scored; the headline is honest \"Partial: earned / observed weight\" with no rescale to 0-100. Evidence Coverage is separate from points. This is where done_when becomes measured.",
      "tags": [
        "loomkeeper",
        "watcher",
        "design",
        "weave",
        "scoring",
        "floors",
        "coverage",
        "phase-2"
      ],
      "links": [
        {
          "to": "architecture-loomkeeper",
          "rel": "implements"
        },
        {
          "to": "design-watcher-work-episode",
          "rel": "refines"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "implements"
        },
        {
          "to": "adr-0019-advisory-evaluator-calibration",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "1313ec1c4e7eb40c7a48ff546eb8d35d9191c0a749e2634e71464531e735bfce"
    },
    {
      "id": "design-watcher-work-episode",
      "path": "docs/design/watcher-work-episode.md",
      "title": "Loomkeeper Work Episode - Goal/Done-When Lifecycle",
      "type": "design",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "2",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "Design for the Loomkeeper Work Episode (slice 4): the unit scoring attaches to. An episode binds one immutable goal + done-condition (mirroring the AI-Forward CT19 goal-state triple Goal / Done when / Not in scope) to one bounded interval of one authenticated session, with observable activity (spans in the interval) bound to it. Changing the goal starts a NEW episode generation (the aggregate invariant); a capability-verified Open/Reframe/Close lifecycle records a DECLARED outcome. The quality judgment (was the goal actually met, did it drift) is the Weave's job (slice 5), not here.",
      "tags": [
        "loomkeeper",
        "watcher",
        "design",
        "work-episode",
        "goal",
        "done-when",
        "scoring",
        "phase-2"
      ],
      "links": [
        {
          "to": "architecture-loomkeeper",
          "rel": "implements"
        },
        {
          "to": "design-watcher-phase1-skeleton",
          "rel": "depends-on"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "implements"
        },
        {
          "to": "adr-0020-trusted-registrar-harness-model-identity",
          "rel": "depends-on"
        },
        {
          "to": "adr-0017-watcher-observation-projection",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "971343e1f38cd837fcc7a080ca59011f70d9a5b74f5a762b132f360badf22317"
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
      "sourceSha256": "4481ec6d45753f957b19019773745c18e8e7cb7c54e978a1631dc8b0f67774df"
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
      "id": "mockup-watcher-observatory",
      "path": "docs/mockups/watcher-observatory.md",
      "title": "Loomkeeper Observatory - Interactive UI Mockup",
      "type": "doc",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "discovery",
      "reviewBy": "2026-11-28",
      "reviewSuggested": [],
      "summary": "Self-contained interactive mockup for watching cross-repository agent sessions, score evidence, repository messages, Daydream learning, privacy controls, and Loomkeeper's own health through a review harness covering personas, viewports, hard states, themes, density, and reduced motion.",
      "tags": [
        "loomkeeper",
        "ui-mockup",
        "observability",
        "agent-scoring",
        "daydream"
      ],
      "links": [
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "implements"
        },
        {
          "to": "ui-review-watcher-observatory",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "59c7e099db8b64f212ed8e42a2bac1efce7588f295e66dacf9389e240c602b64"
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
      "id": "plan-agentic-watcher-substrate",
      "path": "docs/plans/agentic-watcher-substrate.md",
      "title": "Execution Graph - Loomkeeper Knowledge, Specification, and UI",
      "type": "doc",
      "status": "resolved",
      "owner": "@timianmalloo",
      "phase": "discovery",
      "reviewBy": "2026-11-28",
      "reviewSuggested": [],
      "summary": "The bounded execution graph used to ground the repository, coordinate with active worktrees, research the domain, specify Loomkeeper, create the Observatory, and pass independent evidence, model, security, privacy, UX, accessibility, AI, and simplification gates.",
      "tags": [
        "execution-graph",
        "loomkeeper",
        "collectknowledge",
        "specify",
        "ui-design"
      ],
      "links": [
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "relates-to"
        },
        {
          "to": "kb-agentic-session-observability",
          "rel": "depends-on"
        },
        {
          "to": "mockup-watcher-observatory",
          "rel": "relates-to"
        }
      ],
      "diagrams": [
        {
          "kind": "flowchart",
          "title": "Optimized graph",
          "mermaid": "flowchart LR\n  G[Ground repository] --> W[Worktree + coordination]\n  W --> K[Knowledge base]\n  K --> S[Specification]\n  S --> D[Design language]\n  D --> M[Observatory mockup]\n  M --> R[Mechanical + adversarial review]\n  K --> C[Discoverability + audit]\n  S --> C\n  R --> C"
        }
      ],
      "sourceSha256": "0208d50d6a1acd8e31f9ffc5b1c2ca4a34b7f67411b522222f1e5db56d859000"
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
      "sourceSha256": "acbb087ebecc581379758360b80c2f52be402c92e72f72bfb5fa32793a045d68"
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
      "id": "ui-review-watcher-observatory",
      "path": "docs/reviews/ui-watcher-observatory.md",
      "title": "UI Review - Loomkeeper Observatory",
      "type": "doc",
      "status": "in-review",
      "owner": "@timianmalloo",
      "phase": "discovery",
      "reviewBy": "2026-11-28",
      "reviewSuggested": [],
      "summary": "Create-mode review of the Loomkeeper Observatory mockup. The evidence-first G6 structure, hard states, score honesty, keyboard treegrid, token discipline, and model-governance controls pass; native WPF UI Automation, platform keyboard conventions, system contrast, and multi-monitor DPI remain implementation conditions.",
      "tags": [
        "ui-review",
        "loomkeeper",
        "accessibility",
        "agent-observability"
      ],
      "links": [
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "documents"
        },
        {
          "to": "mockup-watcher-observatory",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "4cd3f297ac37a5b81be47a3404f3e2ba027a5321ccc6ae58ea9d287e24fbe6f8"
    },
    {
      "id": "kb-agentic-session-observability-glossary",
      "path": "docs/knowledge/agentic-session-observability/glossary.md",
      "title": "Agentic Session Observability - Glossary",
      "type": "glossary",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "discovery",
      "reviewBy": "2026-11-28",
      "reviewSuggested": [],
      "summary": "Ubiquitous language for the watcher domain, separating identities, observations, evaluations, coordination records, and promoted learning.",
      "tags": [
        "glossary",
        "ubiquitous-language",
        "watcher"
      ],
      "links": [
        {
          "to": "kb-agentic-session-observability",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "855af815674245851df9fc51855e1117fe59ddff3127449ee9da7583c0c75f07"
    },
    {
      "id": "investigation-terminal-cursor-render-crash",
      "path": "docs/investigations/terminal-cursor-render-crash.md",
      "title": "Investigation - AiDe.App crash: terminal cursor render IndexOutOfRange (DC-041)",
      "type": "investigation",
      "status": "resolved",
      "owner": "@timianmalloo",
      "phase": "4",
      "reviewBy": "2027-02-28",
      "reviewSuggested": [],
      "summary": "AiDe.App terminated with an unhandled IndexOutOfRangeException while two agent CLIs (copilot + claude) were grounding in a repo. Verified root cause (from the Windows Application event log, reproduced deterministically): TerminalView.DrawCursor read the character under the cursor through the raw TerminalScreen indexer, but the cursor legitimately sits at the pending-wrap column (CursorColumn == Columns) after writing the last column; at the bottom row that indexes one past the end of the cell array, and the exception is unhandled on the WPF UI thread inside OnRender, which terminates the process. Fixed with a bounds-safe CellUnderCursor() the renderer uses. Registered as DC-041. (A separate finding: the watcher UX is not wired into the running app - see below.)",
      "tags": [
        "loomkeeper",
        "terminal",
        "crash",
        "render",
        "cursor",
        "defect",
        "dc-041",
        "phase-4"
      ],
      "links": [
        {
          "to": "design-watcher-host",
          "rel": "relates-to"
        },
        {
          "to": "architecture-loomkeeper",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "24bcafb9ba39c42119263437bfa8fb3fde7c73f21cdee5bac15317d9400194d6"
    },
    {
      "id": "kb-agentic-session-observability",
      "path": "docs/knowledge/agentic-session-observability/index.md",
      "title": "Agentic Session Observability, Coordination, Learning, and Scoring",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "discovery",
      "reviewBy": "2026-11-28",
      "reviewSuggested": [],
      "summary": "Evidence base for a local watcher that registers terminal-agent sessions across repositories, observes their traces and coordination, supports shared knowledge, evaluates agent effectiveness, and turns repeated failure patterns into reviewable daydream learnings.",
      "tags": [
        "agent-observability",
        "coordination",
        "evaluation",
        "continuous-learning",
        "terminal-sessions"
      ],
      "links": [
        {
          "to": "kb-multi-agent-coordination",
          "rel": "refines"
        },
        {
          "to": "spec-ai-native-ide",
          "rel": "relates-to"
        },
        {
          "to": "session-contracts",
          "rel": "relates-to"
        }
      ],
      "diagrams": [],
      "sourceSha256": "28b8980906af6e24c2800a47de876b2bc1a027f7acedcaeefefe91156cf50ef7"
    },
    {
      "id": "kb-agentic-session-observability-comparables",
      "path": "docs/knowledge/agentic-session-observability/comparables.md",
      "title": "Agentic Session Observability - Comparables",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "discovery",
      "reviewBy": "2026-11-28",
      "reviewSuggested": [],
      "summary": "Comparable observability platforms, agent runtimes, coordination services, benchmarks, and learning systems, with the specific capability each contributes and the gap it leaves.",
      "tags": [
        "comparables",
        "observability",
        "coordination",
        "evaluation"
      ],
      "links": [
        {
          "to": "kb-agentic-session-observability",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "d5c1b090e869d383c7ac0b65f84673f1665b5cf6565a3df44d1622b1dbfca889"
    },
    {
      "id": "kb-agentic-session-observability-data",
      "path": "docs/knowledge/agentic-session-observability/data-and-constants.md",
      "title": "Agentic Session Observability - Data and Constants",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "discovery",
      "reviewBy": "2026-11-28",
      "reviewSuggested": [],
      "summary": "Measured findings, system invariants, candidate score dimensions, and boundary conditions that should constrain later specification and architecture work.",
      "tags": [
        "metrics",
        "benchmarks",
        "invariants",
        "scoring"
      ],
      "links": [
        {
          "to": "kb-agentic-session-observability",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "a94476b3054b28929a5520607f1cb9ba79c8a06f06c98139d690949457c9e1bf"
    },
    {
      "id": "kb-agentic-session-observability-open",
      "path": "docs/knowledge/agentic-session-observability/open-questions.md",
      "title": "Agentic Session Observability - Open Questions",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "discovery",
      "reviewBy": "2026-11-28",
      "reviewSuggested": [],
      "summary": "Unsettled contracts, known domain failure modes, and the strongest arguments against a watcher that scores and continuously teaches active coding agents.",
      "tags": [
        "open-questions",
        "risks",
        "disconfirmation"
      ],
      "links": [
        {
          "to": "kb-agentic-session-observability",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "9204f159ffef151fcf799756c7a23b66070adba876f86a0d60ce6cb51b726a70"
    },
    {
      "id": "kb-agentic-session-observability-references",
      "path": "docs/knowledge/agentic-session-observability/references.md",
      "title": "Agentic Session Observability - References",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "discovery",
      "reviewBy": "2026-11-28",
      "reviewSuggested": [],
      "summary": "Standards, official documentation, benchmark papers, and learning-safety research that establish the watcher domain's contracts and known limitations.",
      "tags": [
        "references",
        "standards",
        "papers"
      ],
      "links": [
        {
          "to": "kb-agentic-session-observability",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "b5336422ebacdb56e46d9da074cef0ecf380dcba836dad11baf71fe1c19fbcb7"
    },
    {
      "id": "kb-agentic-session-observability-sota",
      "path": "docs/knowledge/agentic-session-observability/state-of-the-art.md",
      "title": "Agentic Session Observability - State of the Art",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "discovery",
      "reviewBy": "2026-11-28",
      "reviewSuggested": [],
      "summary": "Current techniques for observing agent sessions, evaluating trajectories, coordinating live processes, and evolving agent context, including the limitations that prevent any one technique from serving as the whole watcher.",
      "tags": [
        "state-of-the-art",
        "opentelemetry",
        "agent-evaluation",
        "memory"
      ],
      "links": [
        {
          "to": "kb-agentic-session-observability",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "452d920b9c06d806e7b1f685a7d5c87433e45be5ee62982ec87200c24113aee0"
    },
    {
      "id": "kb-agentic-session-observability-sources",
      "path": "docs/knowledge/agentic-session-observability/sources.md",
      "title": "Agentic Session Observability - Sources",
      "type": "knowledge",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "discovery",
      "reviewBy": "2026-11-28",
      "reviewSuggested": [],
      "summary": "Full external and repository source list for the agentic watcher evidence base, with access dates and the claims each source supports.",
      "tags": [
        "sources",
        "citations",
        "provenance"
      ],
      "links": [
        {
          "to": "kb-agentic-session-observability",
          "rel": "refines"
        }
      ],
      "diagrams": [],
      "sourceSha256": "4ef09f61c84e341e3fcdf547fa1da6d95c60de55fca16cab435125167948359f"
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
      "id": "proof-terminal-cursor-render-crash",
      "path": "docs/proof/terminal-cursor-render-crash.md",
      "title": "Proof Pack - Terminal cursor render crash fix (DC-041)",
      "type": "proof-pack",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "4",
      "reviewBy": "2027-02-28",
      "reviewSuggested": [],
      "summary": "Evidence that the terminal cursor render crash (DC-041) is fixed: CellUnderCursor() returns null at the pending-wrap cursor position (the exact index that was one past the end of the cell array), the renderer reads through it instead of the raw indexer, and the guard is mutation-verified to reproduce the original IndexOutOfRangeException when removed. 3 new tests, full Core 970/0, App 138/0.",
      "tags": [
        "loomkeeper",
        "terminal",
        "crash",
        "render",
        "cursor",
        "proof-pack",
        "dc-041",
        "phase-4"
      ],
      "links": [
        {
          "to": "investigation-terminal-cursor-render-crash",
          "rel": "tested-by"
        }
      ],
      "diagrams": [],
      "sourceSha256": "889f7a7e998fa13c59b9b8b7cfbed2d2d06d070f8c444d969a4a3ec5fd9b55a4"
    },
    {
      "id": "proof-watcher-advisory-evaluator",
      "path": "docs/proof/watcher-advisory-evaluator.md",
      "title": "Proof Pack - Loomkeeper Local Advisory Evaluator & Egress Guard (connective 3)",
      "type": "proof-pack",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "4",
      "reviewBy": "2027-02-28",
      "reviewSuggested": [],
      "summary": "Evidence that the advisory seam has a safe local implementation and an enforced egress boundary: the local heuristic scores the two advisory dimensions deterministically from a quarantined evidence token list, defaults conservatively for absent tokens (a missing signal can only lower a score), refuses a deterministic dimension (rule 8), and is stable over 20 repeats; and the egress guard denies a non-opted-in path (LK-0003) and a missing credential (LK-0002) - egress checked first - never calling the inner cloud evaluator when either fails, and delegating only when both hold. 15 tests, full suite 928/0, the egress-first ordering mutation-verified.",
      "tags": [
        "loomkeeper",
        "watcher",
        "proof-pack",
        "advisory",
        "evaluator",
        "egress",
        "credential",
        "adr-0018",
        "phase-4"
      ],
      "links": [
        {
          "to": "design-watcher-advisory-evaluator",
          "rel": "tested-by"
        },
        {
          "to": "adr-0018-credential-backed-grading-egress",
          "rel": "depends-on"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "tested-by"
        }
      ],
      "diagrams": [],
      "sourceSha256": "22650db2ba58b4eb65f125c289eb910dffb703e6fd9bdafb5fbad0d3aa578891"
    },
    {
      "id": "proof-watcher-advisory-grader",
      "path": "docs/proof/watcher-advisory-grader.md",
      "title": "Proof Pack - Loomkeeper Advisory Grader, Calibration, Leaderboard & Standing (slice 7)",
      "type": "proof-pack",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "4",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "Evidence that the Loomkeeper advisory grader meets its design: the two advisory dimensions (Evidence discipline, Solution economy) enter Weave points ONLY after the ADR-0019 calibration gates pass - evaluator stability (>=95% modal band, spread <=1 over 20 repeats), quadratic-weighted-kappa >=0.75 against human labels, and an anti-Goodhart held-out counter-metric check; the advisory fold never raises a Blocked or Not Scored verdict (rule 8) and only folds a dimension whose (evaluatorVersion, taskClass, schemaVersion) triple is qualified in the registry; the leaderboard is Not Comparable below a cohort of 5 (rule 10) or with a single operator (US-10 privacy suppression), and is segmented by (task class, schema version) (rule 11); and the AgentStanding exposes rank, trend and one reason per dimension but NO single optimizable scalar (US-16 anti-Goodhart) - proven by 27 tests incl. a reflection guard on the no-scalar contract and a mutation-verified cohort-minimum oracle. Full suite 889/0.",
      "tags": [
        "loomkeeper",
        "watcher",
        "proof-pack",
        "advisory",
        "calibration",
        "qwk",
        "leaderboard",
        "standing",
        "anti-goodhart",
        "phase-4"
      ],
      "links": [
        {
          "to": "design-watcher-advisory-grader",
          "rel": "tested-by"
        },
        {
          "to": "design-watcher-weave-score",
          "rel": "depends-on"
        },
        {
          "to": "adr-0019-advisory-evaluator-calibration",
          "rel": "depends-on"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "tested-by"
        }
      ],
      "diagrams": [],
      "sourceSha256": "ddfd8d51c3ee1bc76913c7228b1062ab4686f4327cbae48b015e0a5d65e24a46"
    },
    {
      "id": "proof-watcher-board-leaderboard-surfaces",
      "path": "docs/proof/watcher-board-leaderboard-surfaces.md",
      "title": "Proof Pack - Loomkeeper Board & Leaderboard WPF Surfaces (connective 2)",
      "type": "proof-pack",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "4",
      "reviewBy": "2027-02-28",
      "reviewSuggested": [],
      "summary": "Evidence that the Board (US-4) and Leaderboard (US-14) WPF surfaces render honestly and are reachable: the pane view models fold the store synchronously and degrade to explicit states (never Loading-forever, DC-011); untrusted board content is shown-but-flagged and a redaction is a tombstone; the leaderboard segments by (task class, schema) and shows Not Comparable for a below-cohort or single-operator cell (US-10); both surfaces render a populated ListBox through SurfaceContentFactory and are in the default layout; a v2->v3 migration adds them to existing layouts (E10); and WorkbenchShell opens the per-workspace watcher store and wires all three queries. 15 Core + 3 App render + 1 migration test; Core suite 913/0, App suite 138/0; the migration oracle mutation-verified.",
      "tags": [
        "loomkeeper",
        "watcher",
        "proof-pack",
        "wpf",
        "surface",
        "board",
        "leaderboard",
        "ui",
        "phase-4"
      ],
      "links": [
        {
          "to": "design-watcher-board-leaderboard-surfaces",
          "rel": "tested-by"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "tested-by"
        }
      ],
      "diagrams": [],
      "sourceSha256": "fae984158d8e5013b350f258459eef3bfaa02972e7709bfba5465c56804601b4"
    },
    {
      "id": "proof-watcher-coordination-contract",
      "path": "docs/proof/watcher-coordination-contract.md",
      "title": "Proof Pack - Loomkeeper Injected Coordination Contract (slice 2)",
      "type": "proof-pack",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "1",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "Evidence that the Loomkeeper injected coordination contract meets its design: a non-AI-Forward session registers and heartbeats over the coord-core append log and appears identically in the fact store (one ledger, projected); the parser tolerantly reads the real writer shape (LOG-A leading newline, CRLF, blank/malformed skip, version pin, sort by at/seq); and the capability lives in the adapter, so a heartbeat for a session never registered here is dropped - proven by 16 tests incl. an end-to-end parse->adapter->real-registrar->liveness composition, with the version-pin oracle mutation-verified.",
      "tags": [
        "loomkeeper",
        "watcher",
        "proof-pack",
        "coordination",
        "injected-contract",
        "coord-core",
        "phase-1"
      ],
      "links": [
        {
          "to": "design-watcher-coordination-contract",
          "rel": "tested-by"
        },
        {
          "to": "design-watcher-ingest-host",
          "rel": "depends-on"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "tested-by"
        }
      ],
      "diagrams": [],
      "sourceSha256": "2405d978d3a1793648d5a0958730254c159ccbde6fee4612a0b07bc7d902b664"
    },
    {
      "id": "proof-watcher-dispute-service",
      "path": "docs/proof/watcher-dispute-service.md",
      "title": "Proof Pack - Loomkeeper Raise-Dispute API, Sessions Badge & Cloud-Judge Scaffold (connective 7)",
      "type": "proof-pack",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "4",
      "reviewBy": "2027-02-28",
      "reviewSuggested": [],
      "summary": "Evidence that the US-16 fairness loop closes and the model-judge seam is concrete: RaiseDispute mints the id + timestamp and appends the fact (requiring a trimmed reason); a session is Disputed iff any of its episodes carries a dispute (DM7), shown as a no-colour-alone Sessions badge and computed by the query; and the DelegatingAdvisoryEvaluator clamps + delegates the rubric and, behind the ADR-0018 egress guard, does not judge until opted-in and credentialed. 12 tests, Core 967/0, App 138/0; the per-session derivation mutation-verified.",
      "tags": [
        "loomkeeper",
        "watcher",
        "proof-pack",
        "dispute",
        "sessions",
        "cloud-judge",
        "phase-4"
      ],
      "links": [
        {
          "to": "design-watcher-dispute-service",
          "rel": "tested-by"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "tested-by"
        }
      ],
      "diagrams": [],
      "sourceSha256": "f87ced28267f64ac14f8f93bb64033a1f1c6f631de453d1ebb0f3545e5f147a3"
    },
    {
      "id": "proof-watcher-host",
      "path": "docs/proof/watcher-host.md",
      "title": "Proof Pack - Loomkeeper In-Process Watcher Host (connective 5)",
      "type": "proof-pack",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "4",
      "reviewBy": "2027-02-28",
      "reviewSuggested": [],
      "summary": "Evidence that the in-process WatcherHost composes and runs the ingest: a coordination-contract log registers a session into the shared store through the host; re-pumping is idempotent; liveness is exact because the registrar and the liveness projection share one monotonic clock in-process; an enqueued span is drained by PumpOnce; and the shared store feeds the same Sessions read query the WPF surface folds (E11). Wired into WorkbenchShell with a 2s background pump. 7 tests, Core 946/0, App 138/0; the drain wiring mutation-verified.",
      "tags": [
        "loomkeeper",
        "watcher",
        "proof-pack",
        "host",
        "ingest",
        "coordination",
        "liveness",
        "phase-4"
      ],
      "links": [
        {
          "to": "design-watcher-host",
          "rel": "tested-by"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "tested-by"
        }
      ],
      "diagrams": [],
      "sourceSha256": "d041ee55375063cec46a8efd417707c78f308821808906290a3d74461157f6fc"
    },
    {
      "id": "proof-watcher-ingest-host",
      "path": "docs/proof/watcher-ingest-host.md",
      "title": "Proof Pack - Loomkeeper Ingest Host (slice 1a)",
      "type": "proof-pack",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "1",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "Evidence that the Loomkeeper ingest host meets its design: registration/heartbeat are synchronous, the bounded span queue absorbs a flood with drop-oldest (every drop counted), forged spans are rejected, malformed ones are quarantined without killing the drain, and the counters reconcile - proven by 9 tests with the backpressure counter compile-enforced.",
      "tags": [
        "loomkeeper",
        "watcher",
        "proof-pack",
        "ingest",
        "host",
        "phase-1"
      ],
      "links": [
        {
          "to": "design-watcher-ingest-host",
          "rel": "tested-by"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "tested-by"
        }
      ],
      "diagrams": [],
      "sourceSha256": "7f2257bbec97ef54f25287ab10a6d4693272478071d5ba10838f50ae4bda75b1"
    },
    {
      "id": "proof-watcher-ingest-wire",
      "path": "docs/proof/watcher-ingest-wire.md",
      "title": "Proof Pack - Loomkeeper Ingest Wire (OtelSpanMapper)",
      "type": "proof-pack",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "1",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "Evidence that the Loomkeeper ingest wire's deterministic core (OtelSpanMapper) meets the contract spike S1 established: OTel span and registration events map to ObservedSpan/SessionBinding, unknown harness/model degrade to Not Recorded, malformed events raise LK-0004, and the Development-status GenAI schema is pinned behind a mutation-verified regression gate.",
      "tags": [
        "loomkeeper",
        "watcher",
        "proof-pack",
        "ingest",
        "otlp",
        "phase-1"
      ],
      "links": [
        {
          "to": "design-watcher-ingest-wire",
          "rel": "tested-by"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "tested-by"
        }
      ],
      "diagrams": [],
      "sourceSha256": "07dc272b05d9b7465d19962793780c6cbf508420fa0528abbaa6b26635178edc"
    },
    {
      "id": "proof-watcher-live-refresh",
      "path": "docs/proof/watcher-live-refresh.md",
      "title": "Proof Pack - Live Pane Auto-Refresh (conn-9)",
      "type": "proof-pack",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "2",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "Proof Pack for conn-9: the watcher panes re-render on a store change, gated by a fingerprint whose liveness-state term catches an Ended transition with an unchanged session count. App 140/0.",
      "tags": [
        "loomkeeper",
        "watcher",
        "proof-pack",
        "refresh",
        "conn-9",
        "phase-2"
      ],
      "links": [
        {
          "to": "design-watcher-live-refresh",
          "rel": "tested-by"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "implements"
        }
      ],
      "diagrams": [],
      "sourceSha256": "8f52e88382a3c4eb755bef88d2cb1432aa8b6539bbce88d1f7f36be78f6c7563"
    },
    {
      "id": "proof-watcher-message-board",
      "path": "docs/proof/watcher-message-board.md",
      "title": "Proof Pack - Loomkeeper Message Board + Fleet (slice 6)",
      "type": "proof-pack",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "3",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "Evidence that the Loomkeeper Message Board + Fleet aggregator meet their design: a per-repository, append-only board with author/session/time/trust provenance; a reply/ack must reference an existing parent in the same repo (no orphan, no cross-repo thread); a forged capability is rejected; content is quarantined untrusted data and grader-injection shapes are flagged; a policy redaction tombstones the payload while the envelope remains and the thread stays anchored; and the fleet builds the repo->session map across >=2 sources - proven by 28 tests incl. D4 SQLite + an E11 composition, with the orphan-rejection oracle mutation-verified. Full suite 862/0.",
      "tags": [
        "loomkeeper",
        "watcher",
        "proof-pack",
        "message-board",
        "fleet",
        "cross-repo",
        "quarantine",
        "phase-3"
      ],
      "links": [
        {
          "to": "design-watcher-message-board",
          "rel": "tested-by"
        },
        {
          "to": "design-watcher-sessions-surface",
          "rel": "depends-on"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "tested-by"
        }
      ],
      "diagrams": [],
      "sourceSha256": "82001dba2186495be6196426e3f4fe2300e8b9ebde8bca51fae637cf6c7119d5"
    },
    {
      "id": "proof-watcher-otlp-receiver",
      "path": "docs/proof/watcher-otlp-receiver.md",
      "title": "Proof Pack - Loomkeeper OTLP Receiver (slice 1b)",
      "type": "proof-pack",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "1",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "Evidence that the Loomkeeper OTLP/HTTP receiver meets its design: it accepts OTLP/JSON exports at /v1/traces with stdlib System.Text.Json (no protobuf dependency), resolves a per-session bearer token to a capability (the capability never travels the wire), parses and enqueues spans onto the ingest host, caps the body, and answers 200 even when it drops a bad/unauthenticated export - proven by 13 tests including two real-loopback HTTP integration tests, with the auth oracle compile-enforced.",
      "tags": [
        "loomkeeper",
        "watcher",
        "proof-pack",
        "ingest",
        "otlp",
        "receiver",
        "phase-1"
      ],
      "links": [
        {
          "to": "design-watcher-otlp-receiver",
          "rel": "tested-by"
        },
        {
          "to": "design-watcher-ingest-host",
          "rel": "depends-on"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "tested-by"
        }
      ],
      "diagrams": [],
      "sourceSha256": "e80bf48108977ae23e7440abfcd75d65651e94b5be26cbbaffe4aa2a46fd9bfb"
    },
    {
      "id": "proof-watcher-phase1-skeleton",
      "path": "docs/proof/watcher-phase1-skeleton.md",
      "title": "Proof Pack - Loomkeeper Phase-1 Walking Skeleton",
      "type": "proof-pack",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "1",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "Evidence that the Loomkeeper Phase-1 deterministic core (identity + Trusted Registrar, idempotent span ingest, monotonic liveness, default-deny egress) and its durable SQLite store meet their design contracts: 41 xUnit tests green (30 core + 11 SQLite), with red observed on the forgery, dedup, and append-only oracles by mutation.",
      "tags": [
        "loomkeeper",
        "watcher",
        "proof-pack",
        "phase-1"
      ],
      "links": [
        {
          "to": "design-watcher-phase1-skeleton",
          "rel": "tested-by"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "tested-by"
        }
      ],
      "diagrams": [],
      "sourceSha256": "88db95fbfe34817d03c84ba141127c2ef29ac097928899e06b8d4f455a376a88"
    },
    {
      "id": "proof-watcher-runtime-wiring",
      "path": "docs/proof/watcher-runtime-wiring.md",
      "title": "Proof Pack - Loomkeeper watcher wired into the running app (DC-042)",
      "type": "proof-pack",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "4",
      "reviewBy": "2027-02-28",
      "reviewSuggested": [],
      "summary": "Evidence that the Loomkeeper watcher read surfaces are now wired into the running app. The wiring moved from the shell constructor (which the app builds with a null workspace) into AttachWorkspace (the real runtime path, which previously rebuilt the factory without the watcher queries and never opened the host), and the already-realized watcher panes are invalidated so they rebuild against the wired factory (never a terminal, DC-029). Proven by an E11 test through the real composition root: after attach the Sessions pane shows its live empty state, not \"not available\". App 139/0.",
      "tags": [
        "loomkeeper",
        "watcher",
        "wiring",
        "composition-root",
        "e2e-c",
        "dc-042",
        "phase-4"
      ],
      "links": [
        {
          "to": "investigation-terminal-cursor-render-crash",
          "rel": "relates-to"
        },
        {
          "to": "design-watcher-host",
          "rel": "depends-on"
        },
        {
          "to": "design-watcher-board-leaderboard-surfaces",
          "rel": "depends-on"
        }
      ],
      "diagrams": [],
      "sourceSha256": "1c2452d14bbe52fe4b78cf8d8041e46d4b32f413a0609056e007cd0c12f44cfe"
    },
    {
      "id": "proof-watcher-score-dispute",
      "path": "docs/proof/watcher-score-dispute.md",
      "title": "Proof Pack - Loomkeeper Operator Dispute Path (connective 4)",
      "type": "proof-pack",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "4",
      "reviewBy": "2027-02-28",
      "reviewSuggested": [],
      "summary": "Evidence that the operator dispute path meets US-16 / rule 12: a ScoreDispute is an append-only fact that never overwrites the Scorecard (prior score preserved); it round-trips whole-score and per-dimension on both stores and persists across a reopen; the SQLite fact rejects UPDATE/DELETE (DM11) and ignores a duplicate id idempotently; the Disputed state is derived from the facts (DM7); and the Leaderboard surfaces the disputed-episode count so a disputed score is discoverable (US-16). 11 tests, full suite 939/0, App 138/0; the append-only/idempotent oracle mutation-verified.",
      "tags": [
        "loomkeeper",
        "watcher",
        "proof-pack",
        "dispute",
        "fairness",
        "append-only",
        "us-16",
        "phase-4"
      ],
      "links": [
        {
          "to": "design-watcher-score-dispute",
          "rel": "tested-by"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "tested-by"
        }
      ],
      "diagrams": [],
      "sourceSha256": "c8869ba6128799e705fde59370dfb21277e7aec8b568b5846b7cc02aa46a390c"
    },
    {
      "id": "proof-watcher-score-persistence",
      "path": "docs/proof/watcher-score-persistence.md",
      "title": "Proof Pack - Loomkeeper Scorecard & Leaderboard Persistence (connective 1)",
      "type": "proof-pack",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "4",
      "reviewBy": "2027-02-28",
      "reviewSuggested": [],
      "summary": "Evidence that a scored episode persists as a materialized derived cache (DM7) behind IWatcherObservationStore: the in-memory and real SQLite stores return an equal card; the persisted card equals the value WeaveScorer produced (persisted == in-memory == derived); a recompute upserts and leaves no stale dimension/floor child rows; null Coverage round-trips as null not zero; and AllScoredEpisodes() feeds LeaderboardComposer through to a comparable cell. 9 tests, full suite 897/0, the child-cleanup oracle mutation-verified.",
      "tags": [
        "loomkeeper",
        "watcher",
        "proof-pack",
        "persistence",
        "scorecard",
        "leaderboard",
        "materialized-cache",
        "phase-4"
      ],
      "links": [
        {
          "to": "design-watcher-score-persistence",
          "rel": "tested-by"
        },
        {
          "to": "design-watcher-weave-score",
          "rel": "depends-on"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "tested-by"
        }
      ],
      "diagrams": [],
      "sourceSha256": "cd337f7f5d2c37cacf7b72808676c74bbe24fbf8a9282d2cad9d0f2fbc0f98d3"
    },
    {
      "id": "proof-watcher-scoring-service",
      "path": "docs/proof/watcher-scoring-service.md",
      "title": "Proof Pack - Loomkeeper Evidence Composer & Scoring Service (connective 6)",
      "type": "proof-pack",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "4",
      "reviewBy": "2027-02-28",
      "reviewSuggested": [],
      "summary": "Evidence that the scoring path is wired: EvidenceComposer maps deterministic signals to the local evaluator's token vocabulary (omitting unobserved tokens so they default conservatively) and round-trips through the evaluator; ScoringService scores an episode and persists a ScoredEpisode that feeds the Leaderboard; the two advisory dimensions fold only when the evaluator is qualified in the registry (ADR-0019, rule 8) and stay excluded otherwise; and a recompute replaces the prior card. 9 tests, full suite 955/0, the composer->evaluator mapping mutation-verified.",
      "tags": [
        "loomkeeper",
        "watcher",
        "proof-pack",
        "scoring",
        "evidence",
        "calibration",
        "phase-4"
      ],
      "links": [
        {
          "to": "design-watcher-scoring-service",
          "rel": "tested-by"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "tested-by"
        }
      ],
      "diagrams": [],
      "sourceSha256": "fc94abce1c6bc7c116b719101dc8f7efca6de7c2c375304ee89692043aec4afe"
    },
    {
      "id": "proof-watcher-session-emitter",
      "path": "docs/proof/watcher-session-emitter.md",
      "title": "Proof Pack - Session Coordination Emitter (conn-8)",
      "type": "proof-pack",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "2",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "Proof Pack for conn-8: the auto-emitting session wrapper (SessionCoordinationEmitter + Reconcile) and its shell wiring, including the DC-043 session-end-to-Ended fix. 9 emitter tests, 2 mutation-verified; Core 979/0, App 139/0.",
      "tags": [
        "loomkeeper",
        "watcher",
        "proof-pack",
        "emitter",
        "coordination",
        "conn-8",
        "phase-2"
      ],
      "links": [
        {
          "to": "design-watcher-session-emitter",
          "rel": "tested-by"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "implements"
        }
      ],
      "diagrams": [],
      "sourceSha256": "11e70f24e83ac4f4d00b2755bef2aab5ed5901b9702054bee9f3e63089d571e8"
    },
    {
      "id": "proof-watcher-sessions-surface",
      "path": "docs/proof/watcher-sessions-surface.md",
      "title": "Proof Pack - Loomkeeper Sessions Surface (slice 3)",
      "type": "proof-pack",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "1",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "Evidence that the Loomkeeper Sessions surface meets its design: a synchronous, deterministic projection folds the observation store + liveness into honest session rows (Not Recorded for an unproven harness/model, a no-colour-alone liveness badge), the pane VM carries the full state set and never strands on Loading nor renders an unreadable store as blank success (DC-011), and the WPF \"sessions\" surface shows an observed row and is in the default layout - proven by 10 Core tests + 3 STA render tests, with the Not-Recorded honesty oracle mutation-verified. Core 780/0, App 135/0.",
      "tags": [
        "loomkeeper",
        "watcher",
        "proof-pack",
        "ui",
        "sessions",
        "wpf",
        "phase-1"
      ],
      "links": [
        {
          "to": "design-watcher-sessions-surface",
          "rel": "tested-by"
        },
        {
          "to": "design-watcher-phase1-skeleton",
          "rel": "depends-on"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "tested-by"
        }
      ],
      "diagrams": [],
      "sourceSha256": "f972a904bfe3c6f5f256263ee6bb296c6cbd106cddfd078b6ca72553a663d04b"
    },
    {
      "id": "proof-watcher-weave-score",
      "path": "docs/proof/watcher-weave-score.md",
      "title": "Proof Pack - Loomkeeper Deterministic Weave (slice 5)",
      "type": "proof-pack",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "2",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "Evidence that the Loomkeeper deterministic Weave meets its design: a closed Work Episode is scored on the four deterministic dimensions (observed weight 70) with the two advisory dimensions excluded (not faked); a hard floor (correctness / security / privacy / data integrity / evaluator integrity) trips a Blocked verdict and suppresses the numeric headline; a missing goal / done-condition / verification path or an open episode is Not Scored; the Partial headline uses the observed-weight denominator and never rescales to 0-100; and Evidence Coverage is separate from points - proven by 27 tests incl. an E11 composition, with the no-rescale oracle mutation-verified. Full suite 834/0.",
      "tags": [
        "loomkeeper",
        "watcher",
        "proof-pack",
        "weave",
        "scoring",
        "floors",
        "coverage",
        "phase-2"
      ],
      "links": [
        {
          "to": "design-watcher-weave-score",
          "rel": "tested-by"
        },
        {
          "to": "design-watcher-work-episode",
          "rel": "depends-on"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "tested-by"
        }
      ],
      "diagrams": [],
      "sourceSha256": "69ffea8b77a21e25742f2fc4f309e2fd185c26445fcde439dc228cc92a335345"
    },
    {
      "id": "proof-watcher-work-episode",
      "path": "docs/proof/watcher-work-episode.md",
      "title": "Proof Pack - Loomkeeper Work Episode (slice 4)",
      "type": "proof-pack",
      "status": "accepted",
      "owner": "@timianmalloo",
      "phase": "2",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "Evidence that the Loomkeeper Work Episode meets its design: an episode binds one immutable goal + done-condition (the CT19 goal-state triple) to one bounded interval of one authenticated session; the lifecycle is capability-verified (forgery rejected on open/reframe/close); changing the goal starts a NEW episode (the old is Superseded, the next generation opens with the new goal, never a mutation); the projection binds only spans inside the interval (endpoints inclusive, open episode uses now); and it persists across a SQLite reopen - proven by 20 tests incl. D4 SQLite + an E11 composition, with the interval-endpoint oracle mutation-verified. Full suite 807/0.",
      "tags": [
        "loomkeeper",
        "watcher",
        "proof-pack",
        "work-episode",
        "goal",
        "done-when",
        "phase-2"
      ],
      "links": [
        {
          "to": "design-watcher-work-episode",
          "rel": "tested-by"
        },
        {
          "to": "design-watcher-phase1-skeleton",
          "rel": "depends-on"
        },
        {
          "to": "spec-agentic-watcher-substrate",
          "rel": "tested-by"
        }
      ],
      "diagrams": [],
      "sourceSha256": "fa128429155af92c08c52f48033ebd5764a806c77b75aa71aa0a2a0d2a716b96"
    },
    {
      "id": "spec-agentic-watcher-substrate",
      "path": "docs/specs/agentic-watcher-substrate.md",
      "title": "Loomkeeper - Agentic Watcher Substrate and Observatory",
      "type": "spec",
      "status": "draft",
      "owner": "@timianmalloo",
      "phase": "discovery",
      "reviewBy": "2027-02-26",
      "reviewSuggested": [],
      "summary": "Specifies Loomkeeper, a local agentic watcher that registers terminal-agent sessions across repositories, exposes repo-scoped collaboration, produces evidence-backed agent scorecards attributed by harness and model, ranks harness/model performance on a leaderboard, is user-configured with local credentials, and turns repeated patterns into reviewable daydream learning through the Observatory UI.",
      "tags": [
        "loomkeeper",
        "agent-observability",
        "coordination",
        "scoring",
        "leaderboard",
        "daydream",
        "watcher"
      ],
      "links": [
        {
          "to": "kb-agentic-session-observability",
          "rel": "implements"
        },
        {
          "to": "spec-ai-native-ide",
          "rel": "refines"
        },
        {
          "to": "session-contracts",
          "rel": "relates-to"
        }
      ],
      "diagrams": [
        {
          "kind": "flowchart",
          "title": "Registration and blind spots",
          "mermaid": "flowchart TD\n  A([Open Watch]) --> B{Any watched repository?}\n  B -->|no| C[First run: local-only notice + Watch a repository]\n  C --> D{Repository identity valid?}\n  D -->|no| E[Show invalid/duplicate repository and retry] --> C\n  D -->|yes| F\n  B -->|yes| F([Terminal or agent session starts])\n  F --> G{Registration available?}\n  G -->|native or injected contract| H[Bind repository, worktree, terminal, agent, harness, model, generation]\n  G -->|unsupported| I[Blind Spot: Partially Observed or Not Watched]\n  H --> J{Identity authority valid?}\n  J -->|verified capability| K[Registered: heartbeat and observation begin]\n  J -->|asserted only| L[Registered with Asserted trust label]\n  J -->|duplicate or forged| M[Reject, record attempt, open identity investigation]\n  M --> N{Disposition}\n  N -->|new process generation| H\n  N -->|dismiss false detection| I\n  I --> O{Operator action}\n  O -->|install adapter or register| H\n  O -->|accept gap| P([Remain Not Recorded and unscored])\n  K --> Q{Heartbeat fresh?}\n  L --> Q\n  Q -->|yes| R([Alive])\n  Q -->|expired| S[Stale; scores marked stale]\n  S --> T{Process resumes?}\n  T -->|yes| U[New registration/generation; old authority rejected]\n  T -->|no| V([Ended or unknown])"
        },
        {
          "kind": "flowchart",
          "title": "Work Episode lifecycle",
          "mermaid": "flowchart TD\n  A([Registered Agent Session]) --> B{Goal and done condition declared?}\n  B -->|no| C[No Work Episode; Not Scored]\n  B -->|yes| D[Open immutable Work Episode]\n  D --> E[Observe actions, evidence, messages, and outputs]\n  E --> F{Goal changes?}\n  F -->|yes| G[Close prior episode as superseded goal] --> H[Open new Work Episode] --> E\n  F -->|no| I{Done condition reached or session ends?}\n  I -->|no| E\n  I -->|yes| J[Close episode]\n  J --> K{Minimum verification present?}\n  K -->|yes| L([Scoreable])\n  K -->|no| M([Not Scored with missing verification])"
        },
        {
          "kind": "flowchart",
          "title": "Watching and evidence adjudication",
          "mermaid": "flowchart TD\n  A([Open Watch / Sessions]) --> B[Choose repository, worktree, terminal, or session]\n  B --> C[Open Session Detail]\n  C --> D{Goal and verification available?}\n  D -->|no| E[Not Scored with named missing evidence]\n  D -->|yes| F[Open Weave Scorecard]\n  F --> G{Hard floor failed?}\n  G -->|yes| H[Blocked verdict; failing floor pinned]\n  G -->|no| I[Show score + Evidence Coverage + dimensions]\n  H --> J[Open dimension evidence]\n  I --> J\n  J --> K{Accept judgment?}\n  K -->|yes| L([Return to Sessions])\n  K -->|dispute| M[Append dispute with reason and evidence]\n  M --> N[Deterministic/human disposition wins; prior version retained]\n  N --> F\n  K -->|send feedback| O[One behavior + one consequence + one next-turn correction]\n  O --> O1{Trustworthy evidence exists?}\n  O1 -->|no| O2[Decline feedback with reason]\n  O1 -->|yes| P{Leaks held-out grader?}\n  P -->|yes| Q[Block or redact]\n  P -->|no| R([Feedback delivered])"
        },
        {
          "kind": "flowchart",
          "title": "Repo Message Board",
          "mermaid": "flowchart TD\n  A([Open Message Board]) --> A1{Repository context selected?}\n  A1 -->|no / All repositories| A2[Require repository picker] --> A1\n  A1 -->|yes| B{Post, reply, acknowledge, search}\n  B -->|post| C[Choose Question, Decision, Breadcrumb, or Knowledge Candidate]\n  C --> D[Attempt append with provenance and trust]\n  D --> D1{Append succeeds?}\n  D1 -->|no| D2[Show failed write; preserve draft; retry]\n  D1 -->|yes| I\n  B -->|reply| E{Parent exists?}\n  E -->|yes| F[Append reply linked to parent]\n  E -->|no| G[Reject orphan reply with reason]\n  B -->|acknowledge| H[Append acknowledgement; unanswered state clears]\n  B -->|search| H1[Show repository-scoped results and return to thread]\n  H --> K\n  F --> I{Instruction-like or poisoned content?}\n  I -->|yes| J[Quarantine as untrusted; no grader/promotion authority]\n  I -->|no| K([Visible in thread])\n  B -->|read failure or stale| L[Show failed/stale state and retry]"
        },
        {
          "kind": "flowchart",
          "title": "Daydream review and learning",
          "mermaid": "flowchart TD\n  A([Observe behavior or outcome]) --> B[Daydream Observation with evidence and confidence]\n  B --> C{Repeated or deterministically reproduced?}\n  C -->|no| D([Remain Observation])\n  C -->|yes| E[Propose Candidate Lesson]\n  E --> F[Show sources, counter-evidence, expected effect, and disconfirming check]\n  F --> G{Disconfirming check complete?}\n  G -->|no| H[Promotion disabled]\n  H --> H1[Run or attach disconfirming check] --> G\n  G -->|yes, candidate refuted| H2[Mark Disconfirmed; promotion blocked]\n  G -->|yes, survives| I{Human decision}\n  I -->|promote| J[Versioned Promoted Learning aligned to Dream/defect class]\n  I -->|defer| K([Remain Candidate])\n  I -->|reject| L([Archive with reason])\n  J --> M{Source corrected/deleted or later contradiction?}\n  M -->|yes| N[Retract or supersede learning and projections]\n  M -->|no| O[Measure recurrence/effect]\n  J --> P{Operator retracts or supersedes?}\n  P -->|yes, with reason| N\n  P -->|no| O"
        },
        {
          "kind": "flowchart",
          "title": "Privacy, retention, and deletion",
          "mermaid": "flowchart TD\n  A([First repository capture]) --> B[Notice: purpose, data classes, retention, deletion, non-personnel boundary]\n  B --> C{Operator acknowledges?}\n  C -->|no| D[Capture registration/health only; no work content]\n  C -->|yes| E[Set per-repo opt-in content capture and redaction]\n  E --> F([Local-only capture])\n  F --> G{Request}\n  G -->|external export or hosted judge| H[Export blocked in v1; hosted judge only via explicit egress opt-in]\n  G -->|rank a person| I[Refused]\n  G -->|delete| J[Preview source + derived scores/learning affected]\n  J --> K{Confirm deletion?}\n  K -->|no| F\n  K -->|yes| L[Run resumable deletion/retraction process]\n  L --> M{All required effects complete?}\n  M -->|yes| N[Issue Complete receipt]\n  M -->|partial or failed| O[Issue Partial receipt with failed effects]\n  O --> P[Retry incomplete effects] --> L"
        },
        {
          "kind": "flowchart",
          "title": "Configuration and credentials",
          "mermaid": "flowchart TD\n  A([Open Configuration]) --> B[Choose watched harnesses, models, and repositories]\n  B --> C{Credential needed for a watched harness?}\n  C -->|no| D([Watch selected scope, local-only])\n  C -->|yes| E[Enter credential]\n  E --> F[Store as local secret; never logged or emitted]\n  F --> G{Grader/Daydream must call a model off-device?}\n  G -->|no| D\n  G -->|yes| H[Egress opt-in notice: purpose, endpoint, data classes]\n  H --> I{Operator opts in?}\n  I -->|no| J([Stay local-only; that path disabled; Egress blocked])\n  I -->|yes| K[Enable that egress path only]\n  K --> L{Later revoke or credential removed?}\n  L -->|yes| M[Revoke: disable path, drop secret, keep no derived copy]\n  L -->|no| N([Watching with opted-in egress])\n  D --> O{Harness or model unreported?}\n  O -->|yes| P[Attribution Not Recorded; episode still scored]\n  O -->|no| Q([Attributed to harness and model])"
        },
        {
          "kind": "flowchart",
          "title": "Leaderboard",
          "mermaid": "flowchart TD\n  A([Open Leaderboard]) --> B{Task class and score schema selected?}\n  B -->|no| C[Require task class + score schema version] --> B\n  B -->|yes| D[Gather comparable episodes in that class + version]\n  D --> E{Cohort >= minimum and not a single-human proxy?}\n  E -->|no| F[Show Not Comparable with reason; no rank]\n  E -->|yes| G[Rank by harness, model, and harness-model]\n  G --> H[Show rank, cohort size, Evidence Coverage, and trend per cell]\n  H --> I{Open a cell?}\n  I -->|yes| J[Open the episodes and Scorecards behind the rank]\n  I -->|no| K([Return to Leaderboard])\n  H --> L{Rubric/schema/model version changed?}\n  L -->|yes| M[Segment versions; do not trend incompatible results into one rank]\n  L -->|no| K"
        }
      ],
      "sourceSha256": "835df96d89a3ed0442dd6cb4df83b9c1dfeac8c5aa182aa62eebb0715b243c2f"
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
      "title": "ai-de-feature-agent-watcher-substrate — Audit & Change Log",
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
      "id": "surface-mockups-knowledge-explorer",
      "path": "docs/mockups/knowledge-explorer.html",
      "title": "Knowledge Explorer — graph + node introspection (mockup)",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "mockup-knowledge-explorer"
    },
    {
      "id": "surface-specs-agentic-watcher-substrate",
      "path": "docs/specs/agentic-watcher-substrate.html",
      "title": "Loomkeeper - Agentic watcher proposal",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "spec-agentic-watcher-substrate"
    },
    {
      "id": "surface-mockups-watcher-observatory",
      "path": "docs/mockups/watcher-observatory.html",
      "title": "Loomkeeper Observatory - review mockup",
      "kind": "knowledge-tool",
      "description": "Open an interactive knowledge artifact.",
      "artifactId": "mockup-watcher-observatory"
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
  "graphSha256": "a048bc74a73b490395fce4f0e658a183f2df4646af587560421b501c454ed9f9"
};
