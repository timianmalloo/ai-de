// Derived from docs/audit/*.jsonl by scripts/audit-log.py — DO NOT hand-edit (the JSONL logs are the source of truth; see audit-and-change-log.md).
window.AUDIT_DATA = {
  "project": "ai-de-facelift",
  "generated": "2026-08-31T21:57:46Z",
  "audit": [
    {
      "id": "al-0001",
      "shortname": "addpacktorepo-ai-de",
      "datetime": "2026-08-23T19:13:43Z",
      "session": "ca7ce99f-6c79-4949-861b-1d5b20db1754",
      "prompt": "/addpacktorepo -> C:\\Projects\\ai-de",
      "summary": "Installed AI-Forward Pack revision 45 (2026.08.16.2): 39 knowledge docs, 38 wrapped instructions (csharp-style-guide .cs-scoped, FOUNDATION verbatim), 22 skills + 22 prompts, 23 claude agents + 11 copilot agents, 26 templates, 16 scripts, pack docs, docs/index.html, CLAUDE.md + AGENTS.md managed blocks, .gitignore hygiene, docs-health CI workflow. docs-index.js not seeded (V10).",
      "kind": "command",
      "skill": "addpacktorepo",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [],
      "tags": [
        "install",
        "pack",
        "csharp"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0002",
      "shortname": "adopt-ai-de",
      "datetime": "2026-08-23T20:23:58Z",
      "session": "b5f931c6-a515-4e72-95f8-37dc8e3d0495",
      "prompt": "sigh - the original session was supposed to make this a github repo\ncreate a new github repo ai-de under my tim.ian.malloo account (public repo, mit license)\nthen commit and push the content here\nthen run /adopt",
      "summary": "Created the public timianmalloo/ai-de repository, pushed the WPF starter, recovered a source-backed single-container architecture, seeded a four-term glossary, classified project documents, derived a seven-artifact Docs Explorer graph with zero defects, recorded the health baseline and adoption plan, and passed local plus GitHub build/docs-health proof.",
      "kind": "skill",
      "skill": "adopt",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "docs/architecture.md",
        "docs/knowledge/glossary.md",
        "docs/ai-forward-pack-adoption.md",
        "docs/proof/adoption-proof.md"
      ],
      "tags": [
        "adoption",
        "github",
        "wpf"
      ],
      "outcome": "success",
      "goal": "Create and publish timianmalloo/ai-de, then recover its honest, navigable knowledge graph.",
      "done_when": "Public main contains the MIT-licensed repository and a validated, browsable, audited adoption baseline with a phased gap plan.",
      "started_at": "2026-08-23T19:31:43Z",
      "duration_seconds": 3135.0,
      "change": "cl-0001"
    },
    {
      "id": "al-0003",
      "shortname": "collectknowledge-ai-de-domains",
      "datetime": "2026-08-23T22:41:17Z",
      "session": "collectknowledge-20260823",
      "prompt": "/collectknowledge â€” i am going to be building a development environment for myself and for how i work with coding agents. use the following files as \"source matter\" to seed a search for supporting material: ai-native-ide-architecture-sketch.md, agent-coordination/AGENT-COORDINATION-SPEC.md, agent-coordination/agent-coordination-explorer.jsx. I am already working on the agent coordination piece in a different work tree but that has to surface here. accumulate knowledge for all the domains and technologies identified in these docs. accumulate knowledge on UML and Generative Design as well as on the 4GL design surfaces and the state of the art in terms of ERM, Domain Modeling, Domain Driven Design, visualizing interactions between micro-services, visualizing cloud architectures and Azure architecture",
      "summary": "Built a 10-topic sourced knowledge base (71 knowledge artifacts + hub) under docs/knowledge/. Four findings change the seed architecture: Kuzu archived Oct 2025 with no equivalent replacement; MCP 2026-07-28 is stateless and deprecates Sampling/Roots; the coordination spec's TTL+heartbeat leases lack fencing tokens (advisory not exclusive); and the models-are-the-product thesis survives only in its code-derived-views form. Graph: 76 artifacts, 0 defects, 0 orphans.",
      "kind": "skill",
      "skill": "collectknowledge",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "docs/knowledge/index.md",
        "docs/knowledge/code-knowledge-graphs/index.md",
        "docs/knowledge/multi-agent-coordination/index.md",
        "docs/notes/collectknowledge-session-2026-08-23.md"
      ],
      "tags": [
        "knowledge-base",
        "ai-native-ide",
        "multi-agent",
        "modelling"
      ],
      "outcome": "success",
      "goal": "Produce a sourced, confidence-labelled domain knowledge base covering every domain in the seed docs plus UML/generative design/4GL/ERM/DDD/microservice and cloud visualization",
      "done_when": "10 topic bases exist per the pack template with citations and confidence labels, indexed in the docs graph with 0 defects, audit and change logged",
      "started_at": "2026-08-23T21:37:53Z",
      "duration_seconds": 3804.0,
      "git": {
        "sha": "9065ea4f1e6a62139e340c271e14b99a9c4944e4",
        "short": "9065ea4f1",
        "branch": "main",
        "pushed": true
      }
    },
    {
      "id": "al-0194",
      "shortname": "collectknowledge-ai-de-domains",
      "datetime": "2026-08-23T22:41:17Z",
      "session": "collectknowledge-20260823",
      "prompt": "/collectknowledge — i am going to be building a development environment for myself and for how i work with coding agents. use the following files as \"source matter\" to seed a search for supporting material: ai-native-ide-architecture-sketch.md, agent-coordination/AGENT-COORDINATION-SPEC.md, agent-coordination/agent-coordination-explorer.jsx. I am already working on the agent coordination piece in a different work tree but that has to surface here. accumulate knowledge for all the domains and technologies identified in these docs. accumulate knowledge on UML and Generative Design as well as on the 4GL design surfaces and the state of the art in terms of ERM, Domain Modeling, Domain Driven Design, visualizing interactions between micro-services, visualizing cloud architectures and Azure architecture",
      "summary": "Built a 10-topic sourced knowledge base (71 knowledge artifacts + hub) under docs/knowledge/. Four findings change the seed architecture: Kuzu archived Oct 2025 with no equivalent replacement; MCP 2026-07-28 is stateless and deprecates Sampling/Roots; the coordination spec's TTL+heartbeat leases lack fencing tokens (advisory not exclusive); and the models-are-the-product thesis survives only in its code-derived-views form. Graph: 76 artifacts, 0 defects, 0 orphans.",
      "kind": "skill",
      "skill": "collectknowledge",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "docs/knowledge/index.md",
        "docs/knowledge/code-knowledge-graphs/index.md",
        "docs/knowledge/multi-agent-coordination/index.md",
        "docs/notes/collectknowledge-session-2026-08-23.md"
      ],
      "tags": [
        "knowledge-base",
        "ai-native-ide",
        "multi-agent",
        "modelling"
      ],
      "outcome": "success",
      "goal": "Produce a sourced, confidence-labelled domain knowledge base covering every domain in the seed docs plus UML/generative design/4GL/ERM/DDD/microservice and cloud visualization",
      "done_when": "10 topic bases exist per the pack template with citations and confidence labels, indexed in the docs graph with 0 defects, audit and change logged",
      "started_at": "2026-08-23T21:37:53Z",
      "duration_seconds": 3804.0,
      "git": {
        "sha": "9065ea4f1e6a62139e340c271e14b99a9c4944e4",
        "short": "9065ea4f1",
        "branch": "main",
        "pushed": true
      }
    },
    {
      "id": "al-0004",
      "shortname": "specify-ai-native-ide",
      "datetime": "2026-08-24T13:23:21Z",
      "session": "6c940bbc-816b-41ed-a92a-0e954ac70a37",
      "prompt": "create a specification (md and html) for my AI-IDE\n- use this proposal as the seed idea: \"C:\\Users\\malla\\Downloads\\ai-native-ide-architecture-sketch.md\"\n- key scenarios: visually understand implemented logical/as-built architecture; core data/domain models including class hierarchy and aggregate roots; data/process flow; cross-service/library/infrastructure dependencies; cross-agent coordination; repository knowledge as graph/hierarchy; rich-text staged prompts; audit logs; and work/task backlog across sessions and worktrees.\n- typical workflow: one or more Claude Code or GitHub Copilot sessions in terminal tabs, isolated worktrees, coordinated artifacts/graph updates, visual tabs for feedback, and rich-text prompt tabs.\n- reuse existing repositories and libraries where justified; take inspiration from VS Code and Eclipse while keeping code transient rather than the product.",
      "summary": "Produced the AI-native IDE Markdown specification, dependency-free HTML reader, privacy review, decision note, execution plan, and derived Docs Explorer entries; independent review gates cleared the specification with pre-implementation proof conditions.",
      "kind": "skill",
      "skill": "specify",
      "tool": "GitHub Copilot CLI",
      "actor": null,
      "artifacts": [
        "docs/specs/ai-native-ide.md",
        "docs/specs/ai-native-ide.html",
        "docs/security/ai-native-ide-privacy-review.md",
        "docs/plans/ai-native-ide-specification.md",
        "docs/notes/ai-native-ide-specification-framing.md"
      ],
      "tags": [
        "ai-native-ide",
        "specification",
        "privacy-review",
        "agent-coordination"
      ],
      "outcome": "success",
      "goal": "Produce a grounded Markdown and browsable HTML specification for the AI-IDE.",
      "done_when": "The specification, privacy review, HTML reader, Docs Explorer index, validation evidence, and audit entry exist.",
      "started_at": "2026-08-24T13:22:16Z",
      "duration_seconds": 65.0,
      "change": "cl-0003",
      "git": {
        "sha": "0427582ccd915c52c68f170703a64760b3a152bb",
        "short": "0427582cc",
        "branch": "docs/ai-ide-specification",
        "pushed": null
      }
    },
    {
      "id": "al-0005",
      "shortname": "define-architecture-ai-native-ide",
      "datetime": "2026-08-25T22:53:28Z",
      "session": "6c940bbc-816b-41ed-a92a-0e954ac70a37",
      "prompt": "go ahead and merge the PR\nthe /define-architecture on the ai-ide-specification",
      "summary": "Merged the specification to main and produced the AI-DE architecture, seven ADRs, conceptual data model, threat/privacy/release plans, spike evidence, vertical phasing, and independent council verdicts.",
      "kind": "skill",
      "skill": "define-architecture",
      "tool": "GitHub Copilot CLI",
      "actor": null,
      "artifacts": [
        "docs/architecture.md",
        "docs/design/conceptual-model.md",
        "docs/security/ai-native-ide-threat-model.md",
        "docs/security/ai-native-ide-privacy-review.md",
        "docs/release/ai-native-ide-release-plan.md",
        "docs/plans/ai-native-ide-architecture.md"
      ],
      "tags": [
        "ai-native-ide",
        "architecture",
        "spikes",
        "council"
      ],
      "outcome": "success",
      "goal": "Merge the reviewed specification, then define the complete AI-IDE architecture and ADRs in an isolated worktree.",
      "done_when": "Main contains the specification commit and the architecture artifacts are independently reviewed, indexed, validated, and audited.",
      "started_at": "2026-08-25T22:20:21Z",
      "duration_seconds": 1987.0,
      "change": "cl-0004",
      "git": {
        "sha": "bc50c41853fb2314a783709c58eb0f065f48c402",
        "short": "bc50c4185",
        "branch": "architecture/ai-native-ide",
        "pushed": null
      }
    },
    {
      "id": "al-0006",
      "shortname": "ground yourself in the repo knowledge and the specs docs/specs, then useâ€¦",
      "datetime": "2026-08-26T00:02:53Z",
      "session": "prompt-log",
      "prompt": "ground yourself in the repo knowledge and the specs docs/specs, then use our repo personas and do a critique of the proposed architecture docs/architecture.md",
      "summary": "prompt logged for reuse",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": null,
      "artifacts": [],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0195",
      "shortname": "ground yourself in the repo knowledge and the specs docs/specs, then use…",
      "datetime": "2026-08-26T00:02:53Z",
      "session": "prompt-log",
      "prompt": "ground yourself in the repo knowledge and the specs docs/specs, then use our repo personas and do a critique of the proposed architecture docs/architecture.md",
      "summary": "prompt logged for reuse",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": null,
      "artifacts": [],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0007",
      "shortname": "step back / /define-architecture ai-ide-arch-v2 â€” use the spec, the origâ€¦",
      "datetime": "2026-08-26T00:17:11Z",
      "session": "prompt-log",
      "prompt": "step back / /define-architecture ai-ide-arch-v2 â€” use the spec, the original architecture.md and your findings as input; redo the architecture to meet your bar",
      "summary": "prompt logged for reuse",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": null,
      "artifacts": [],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0196",
      "shortname": "step back / /define-architecture ai-ide-arch-v2 — use the spec, the orig…",
      "datetime": "2026-08-26T00:17:11Z",
      "session": "prompt-log",
      "prompt": "step back / /define-architecture ai-ide-arch-v2 — use the spec, the original architecture.md and your findings as input; redo the architecture to meet your bar",
      "summary": "prompt logged for reuse",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": null,
      "artifacts": [],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0008",
      "shortname": "define-architecture-ai-ide-arch-v2",
      "datetime": "2026-08-26T00:36:25Z",
      "session": "4e957874-10fd-4d1b-a6b7-41042277c103",
      "prompt": "/define-architecture ai-ide-arch-v2 â€” use the spec, the original architecture.md and your findings as input; redo the architecture to meet your bar",
      "summary": "Superseded the 2026-08-25 architecture: committed 3 re-runnable spikes, ADR-0008..0011, write-ahead dispatch, in-process-first daemon, MCP egress binding; resolved 3 hard + 2 soft vetoes and the verified contradictions",
      "kind": "skill",
      "skill": "define-architecture",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/architecture.md"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0197",
      "shortname": "define-architecture-ai-ide-arch-v2",
      "datetime": "2026-08-26T00:36:25Z",
      "session": "4e957874-10fd-4d1b-a6b7-41042277c103",
      "prompt": "/define-architecture ai-ide-arch-v2 — use the spec, the original architecture.md and your findings as input; redo the architecture to meet your bar",
      "summary": "Superseded the 2026-08-25 architecture: committed 3 re-runnable spikes, ADR-0008..0011, write-ahead dispatch, in-process-first daemon, MCP egress binding; resolved 3 hard + 2 soft vetoes and the verified contradictions",
      "kind": "skill",
      "skill": "define-architecture",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/architecture.md"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0009",
      "shortname": "design-phase-1-walking-skeleton",
      "datetime": "2026-08-26T00:49:08Z",
      "session": "4e957874-10fd-4d1b-a6b7-41042277c103",
      "prompt": "C:/Program Files/Git/design phase-1 the walking skeleton",
      "summary": "Phase-1 detailed design: data model with grains/additivity/per-attribute history, contracts, patterns, failure-mode + STRIDE + LINDDUN analyses, UI design + DESIGN.md, telemetry, full triggered-directive test plan",
      "kind": "skill",
      "skill": "design",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/design/phase-1-walking-skeleton.md"
      ],
      "tags": [],
      "outcome": "success",
      "started_at": "2026-08-26T00:44:31Z",
      "duration_seconds": 277.0
    },
    {
      "id": "al-0010",
      "shortname": "implement-phase-1-walking-skeleton",
      "datetime": "2026-08-26T01:10:09Z",
      "session": "4e957874-10fd-4d1b-a6b7-41042277c103",
      "prompt": "then /implement the walking skeleton",
      "summary": "Phase-1 walking skeleton implemented red-first: SQLite fact store with spike-verified immutability control, write-ahead two-phase dispatch (ADR-0010), fixture extractor, bounded describe/impact/find/knowledge projections (US-4), MCP processing-class egress gate (ADR-0011), health sidecar, freshness prober, accessible provenance pane wired into WPF. 59 tests green; 6 controls observed RED first; Proof Pack + defect-class register written",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/design/phase-1-proof-pack.md",
        "src/AiDe.Core",
        "docs/lessons/defect-classes.md"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0011",
      "shortname": "p1-perf-benchmark-run",
      "datetime": "2026-08-26T02:14:28Z",
      "session": "4e957874-10fd-4d1b-a6b7-41042277c103",
      "prompt": "do the P1-PERF run",
      "summary": "P1-PERF executed on the 50k-edge corpus. First run FAILED 5/8 budgets (indexes correct but unused - ProjectionService materialized the whole corpus per query). Pushed filtering into indexed SQL: describe 69x, knowledge ~880x, impact 12-29x, find 7.7x faster; all budgets now pass. Growth curve reveals refresh exceeds budget after ~10 generations - open Phase-2 item",
      "kind": "script",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/design/phase-1-perf-results.md",
        "bench/AiDe.Bench"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0012",
      "shortname": "specify-us9-workbench",
      "datetime": "2026-08-26T13:39:30Z",
      "session": "4e957874-10fd-4d1b-a6b7-41042277c103",
      "prompt": "tooling should allow resize/docking like eclipse, vs code, photoshop, premiere - specify, ui-design, redo architecture, list slice changes",
      "summary": "Dockable workbench: US-9 spec + exemplar matrix, workbench mockup with harness, ADR-0012/0013, Phase 1b",
      "kind": "skill",
      "skill": "specify",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/specs/ai-native-ide.md"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0013",
      "shortname": "ui-design-workbench",
      "datetime": "2026-08-26T13:39:30Z",
      "session": "4e957874-10fd-4d1b-a6b7-41042277c103",
      "prompt": "tooling should allow resize/docking like eclipse, vs code, photoshop, premiere - specify, ui-design, redo architecture, list slice changes",
      "summary": "Dockable workbench: US-9 spec + exemplar matrix, workbench mockup with harness, ADR-0012/0013, Phase 1b",
      "kind": "skill",
      "skill": "ui-design",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/mockups/workbench.html"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0014",
      "shortname": "define-architecture-workbench",
      "datetime": "2026-08-26T13:39:30Z",
      "session": "4e957874-10fd-4d1b-a6b7-41042277c103",
      "prompt": "tooling should allow resize/docking like eclipse, vs code, photoshop, premiere - specify, ui-design, redo architecture, list slice changes",
      "summary": "Dockable workbench: US-9 spec + exemplar matrix, workbench mockup with harness, ADR-0012/0013, Phase 1b",
      "kind": "skill",
      "skill": "define-architecture",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/architecture.md"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0015",
      "shortname": "the tooling should allow resize of panes and docking of elements like ecâ€¦",
      "datetime": "2026-08-26T13:42:06Z",
      "session": "prompt-log",
      "prompt": "the tooling should allow resize of panes and docking of elements like eclipse or vs code or photoshop/premiere. step back and make sure this is part of the spec. Consider exemplars in multi-pane, resize, hide, dock, move. /specify these capabilities, then /ui-design, then /define-architecture redo with this consideration, then list how the slices change",
      "summary": "prompt logged for reuse",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": null,
      "artifacts": [],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0198",
      "shortname": "the tooling should allow resize of panes and docking of elements like ec…",
      "datetime": "2026-08-26T13:42:06Z",
      "session": "prompt-log",
      "prompt": "the tooling should allow resize of panes and docking of elements like eclipse or vs code or photoshop/premiere. step back and make sure this is part of the spec. Consider exemplars in multi-pane, resize, hide, dock, move. /specify these capabilities, then /ui-design, then /define-architecture redo with this consideration, then list how the slices change",
      "summary": "prompt logged for reuse",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": null,
      "artifacts": [],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0016",
      "shortname": "implement-phase-1b-workbench-model",
      "datetime": "2026-08-26T13:48:07Z",
      "session": "4e957874-10fd-4d1b-a6b7-41042277c103",
      "prompt": "push and merge then tackle phase 1b",
      "summary": "Phase-1b core: owned headless layout model (tree/stack/surface) with the tiling invariant enforced structurally, one-mutation-path command set making keyboard/pointer equivalence testable, versioned persistence envelope with degradation and partial-restore. 29 workbench tests; invariant and announcement-completeness controls observed RED first",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.Core/Workbench",
        "docs/design/phase-1b-workbench.md"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0017",
      "shortname": "spike-avalondock-uia-a11y",
      "datetime": "2026-08-26T13:58:16Z",
      "session": "4e957874-10fd-4d1b-a6b7-41042277c103",
      "prompt": "do the accessibility insights probe",
      "summary": "UIA probe against a live AvalonDock window with a WPF baseline in the same tree. Confirmed the splitter is an unnamed unfocusable Thumb with no Transform. NEW finding: every tab reports its .NET type name as its accessible name. Obvious style fix tested and FAILED; a visual-tree naming pass tested and WORKS, app-side, no fork",
      "kind": "script",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "spikes/avalondock-a11y/RESULT.md",
        "docs/adr/0012-docking-shell-library.md"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0018",
      "shortname": "implement-workbench-adapter",
      "datetime": "2026-08-26T14:21:48Z",
      "session": "4e957874-10fd-4d1b-a6b7-41042277c103",
      "prompt": "do the next action then provide a tabular view of all slices with status and next best action",
      "summary": "AvalonDock adapter: one-way model->view projection, the verified tab-naming pass, and the leaked-name regression control. 4 STA tests; control observed RED reproducing the exact probe defect",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/WorkbenchAdapter.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0019",
      "shortname": "implement-workbench-keyboard-announce",
      "datetime": "2026-08-26T15:57:12Z",
      "session": "4e957874-10fd-4d1b-a6b7-41042277c103",
      "prompt": "do the 1st step now 1b.8 and 1b.9",
      "summary": "Keyboard command catalog + controller + UIA announcer. SC 2.5.7 conformance is now a reflection test over the operation union (observed RED); every command announces incl. refusals (observed RED via a real gap the test caught: reorderSurface was listed but unhandled). Eclipse-pattern resize session with exact cancel-to-entry",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.Core/Workbench/WorkbenchCommands.cs",
        "src/AiDe.App/Workbench/WorkbenchController.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0020",
      "shortname": "implement-workbench-drop-targets",
      "datetime": "2026-08-26T16:16:30Z",
      "session": "4e957874-10fd-4d1b-a6b7-41042277c103",
      "prompt": "yes do #1",
      "summary": "Drop-target resolution in Core (WPF-free geometry) closing the SC 2.5.7 loop: pointer and keyboard now converge on the same LayoutOperation, and the equivalence test compares two real paths. Preview derives from the same resolve call as the commit. Caught and fixed a silent-refusal bug on locked drag; registered as defect class DC-011",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.Core/Workbench/DropTargetResolver.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0021",
      "shortname": "implement-workbench-shell",
      "datetime": "2026-08-26T16:31:59Z",
      "session": "4e957874-10fd-4d1b-a6b7-41042277c103",
      "prompt": "yes do #1 (1b.11 MainWindow rebuild)",
      "summary": "Composition root wiring model/adapter/controller/announcer into MainWindow, replacing the superseded fixed Grid. Surfaces render real evidence; live region is visible AND polite; focus tracking keeps the controller's focused pane honest. 155 tests; app verified launching a live window",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/WorkbenchShell.cs",
        "src/AiDe.App/MainWindow.xaml"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0022",
      "shortname": "implement-command-palette-nvda-script",
      "datetime": "2026-08-26T16:48:38Z",
      "session": "4e957874-10fd-4d1b-a6b7-41042277c103",
      "prompt": "do #1 prep the script and i will be the human",
      "summary": "Built the command palette (the SC 2.5.7 mechanism the spec names but nothing implemented - commands were catalogued and tested but not invokable by a human), then wrote the 19-step NVDA verification protocol",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/reviews/nvda-workbench-session.md",
        "src/AiDe.App/Workbench/CommandPalette.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0023",
      "shortname": "spike-layout-upgrade-roundtrip",
      "datetime": "2026-08-26T17:02:29Z",
      "session": "4e957874-10fd-4d1b-a6b7-41042277c103",
      "prompt": "yes do the round trip spike",
      "summary": "Round-trip spike found ADR-0013's versioned envelope had a version field but no migration hook: the first surface rename would have reset EVERY saved layout to default. Implemented a DTO-level migration chain that fails closed on a gap; 5 tests, headline observed red",
      "kind": "script",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/reviews/spike-layout-upgrade-roundtrip.md"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0024",
      "shortname": "spike-dpi-and-ganged-resize",
      "datetime": "2026-08-26T17:13:02Z",
      "session": "4e957874-10fd-4d1b-a6b7-41042277c103",
      "prompt": "NVDA successfully called out the name of each tab; do the last two ADR-0012 spikes",
      "summary": "Ganged resize verified (no two panes share area). DPI spike found the app was SYSTEM_AWARE not PerMonitorV2 - our defect not AvalonDock's - fixed via app.manifest and verified against the running exe. Cross-monitor transition still untested (one display). NVDA Part A recorded as passed",
      "kind": "script",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/reviews/spike-dpi-and-ganged-resize.md",
        "src/AiDe.App/app.manifest"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0025",
      "shortname": "implement-persistence-and-compaction",
      "datetime": "2026-08-26T19:14:55Z",
      "session": "4e957874-10fd-4d1b-a6b7-41042277c103",
      "prompt": "do 1 and 3, skipping NVDA",
      "summary": "Wired layout persistence (US-9 restart survival) incl. floating bounds and a re-homing off-screen guard; found and fixed a silently-aborting test run (27 of 54 tests never executed); implemented compaction policy for the measured refresh defect - 654ms to 333ms, 97.6 MiB reclaimed",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.Core/Store/StoreCompactor.cs",
        "src/AiDe.App/Workbench/LayoutPersistence.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0026",
      "shortname": "implement-test-run-integrity-gate",
      "datetime": "2026-08-26T19:26:10Z",
      "session": "4e957874-10fd-4d1b-a6b7-41042277c103",
      "prompt": "do the next action DC-012's control",
      "summary": "Built tools/verify-test-run.py: requires results to exist, outcome Completed, and executed count >= committed baseline. Wired into CI replacing bare dotnet test. Observed RED by reproducing the original crash - bare dotnet test said 'Passed! 27', gate said SHORTFALL 27/54 exit 1. DC-012 uncontrolled -> controlled",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "tools/verify-test-run.py",
        ".github/workflows/build.yml"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0027",
      "shortname": "design-phase-2",
      "datetime": "2026-08-26T19:35:00Z",
      "session": "4e957874-10fd-4d1b-a6b7-41042277c103",
      "prompt": "C:/Program Files/Git/design phase 2 then keep the same process... a table of all tasks with status and then the standard table of best next actions",
      "summary": "Phase-2 design covering Roslyn extractor, ConPTY runtime and the process split. Surfaced two contract gaps in seams Phase 1 called substitutable (ITerminalSession is write-only; IExtractor materialises whole scopes) and a new security threat (MSBuildWorkspace executes repo analyzers/generators). Gated behind four spikes",
      "kind": "skill",
      "skill": "design",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/design/phase-2-real-code-and-terminal.md"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0028",
      "shortname": "repair-defect-register",
      "datetime": "2026-08-26T19:46:03Z",
      "session": "4e957874-10fd-4d1b-a6b7-41042277c103",
      "prompt": "Repair the defect-class register first â€” write the three missing entries (DC-009, DC-010, DC-011) and correct the count line",
      "summary": "Wrote DC-009/010/011, corrected the header counts (controlled was overstated by exactly three), recorded DC-001 as recurred and widened its control to tools/verify-defect-register.py, wired into CI, and corrected two now-false compaction claims in architecture.md and the Phase-1 proof pack",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/lessons/defect-classes.md",
        "tools/verify-defect-register.py"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0199",
      "shortname": "repair-defect-register",
      "datetime": "2026-08-26T19:46:03Z",
      "session": "4e957874-10fd-4d1b-a6b7-41042277c103",
      "prompt": "Repair the defect-class register first — write the three missing entries (DC-009, DC-010, DC-011) and correct the count line",
      "summary": "Wrote DC-009/010/011, corrected the header counts (controlled was overstated by exactly three), recorded DC-001 as recurred and widened its control to tools/verify-defect-register.py, wired into CI, and corrected two now-false compaction claims in architecture.md and the Phase-1 proof pack",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/lessons/defect-classes.md",
        "tools/verify-defect-register.py"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0029",
      "shortname": "spike-s2-msbuildworkspace",
      "datetime": "2026-08-26T19:57:48Z",
      "session": "4e957874-10fd-4d1b-a6b7-41042277c103",
      "prompt": "Then run Spike S2, which is the right first spike of the four: it decides whether the analyzer-execution security control is achievable at all, and it is the cheapest to run.",
      "summary": "S2 cleared with its mitigation changed. A real solution loads against an older SDK with zero diagnostics. Repository-authored generator code DOES execute in the extractor's own process, triggered by GetCompilationAsync. MSBuild properties do not suppress it; stripping AnalyzerReferences does, at a cost of exactly the generated symbols. Re-scopes S1 and surfaces ten transitive advisories",
      "kind": "skill",
      "skill": "investigate",
      "tool": null,
      "actor": null,
      "artifacts": [
        "spikes/roslyn-msbuild-workspace/RESULT.md"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0030",
      "shortname": "register-dc-013-audit-id-collision",
      "datetime": "2026-08-26T20:00:50Z",
      "session": "4e957874-10fd-4d1b-a6b7-41042277c103",
      "prompt": "(continuous improvement, obligated by the collision this session caused)",
      "summary": "Registered DC-013 â€” a monotonic id handed out twice because two worktrees allocate independently. Second occurrence in one day; the first was repaired without being registered. Control: tools/verify-audit-log.py in CI, observed failing on a planted duplicate",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/lessons/defect-classes.md",
        "tools/verify-audit-log.py"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0200",
      "shortname": "register-dc-013-audit-id-collision",
      "datetime": "2026-08-26T20:00:50Z",
      "session": "4e957874-10fd-4d1b-a6b7-41042277c103",
      "prompt": "(continuous improvement, obligated by the collision this session caused)",
      "summary": "Registered DC-013 — a monotonic id handed out twice because two worktrees allocate independently. Second occurrence in one day; the first was repaired without being registered. Control: tools/verify-audit-log.py in CI, observed failing on a planted duplicate",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/lessons/defect-classes.md",
        "tools/verify-audit-log.py"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0031",
      "shortname": "Repair the defect-class register first â€” write the three missing entriesâ€¦",
      "datetime": "2026-08-26T20:01:32Z",
      "session": "prompt-log",
      "prompt": "Repair the defect-class register first â€” write the three missing entries (DC-009, DC-010, DC-011) and correct the count line. Then run Spike S2 (MSBuildWorkspace: does a real solution load without exact SDK match, and can analyzers/source generators be disabled?)",
      "summary": "prompt logged for reuse",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": null,
      "artifacts": [],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0201",
      "shortname": "Repair the defect-class register first — write the three missing entries…",
      "datetime": "2026-08-26T20:01:32Z",
      "session": "prompt-log",
      "prompt": "Repair the defect-class register first — write the three missing entries (DC-009, DC-010, DC-011) and correct the count line. Then run Spike S2 (MSBuildWorkspace: does a real solution load without exact SDK match, and can analyzers/source generators be disabled?)",
      "summary": "prompt logged for reuse",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": null,
      "artifacts": [],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0032",
      "shortname": "spikes-s3-s4-and-accessibility-posture",
      "datetime": "2026-08-26T20:38:23Z",
      "session": "4e957874-10fd-4d1b-a6b7-41042277c103",
      "prompt": "Spike S3 and S4 and on S1 yes disclose absent generated code ... surpress accessability vetos, i am not optimizing for accessability",
      "summary": "ADR-0014 withdraws WCAG 2.2 AA as an obligation and the accessibility hard veto; six spec assertions corrected. S3 cleared: own a WPF renderer, GlyphRun per line 6.64ms p95 vs 142.80ms per-cell. S4 MET ADR-0008's reversal trigger: airspace real, and WebView2CompositionControl kills the process when its pane is floated. S1 decided: disclose absent generated code via a scope-level omission state",
      "kind": "skill",
      "skill": "investigate",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/adr/0014-accessibility-posture.md",
        "spikes/terminal-renderer/RESULT.md",
        "spikes/webview2-airspace/RESULT.md"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0033",
      "shortname": "ok go with your recommendation; and design the outstanding focus piece; â€¦",
      "datetime": "2026-08-26T21:07:32Z",
      "session": "prompt-log",
      "prompt": "ok go with your recommendation; and design the outstanding focus piece; also do a snapshot-swap spike as a gut check",
      "summary": "prompt logged for reuse",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": null,
      "artifacts": [],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0202",
      "shortname": "ok go with your recommendation; and design the outstanding focus piece; …",
      "datetime": "2026-08-26T21:07:32Z",
      "session": "prompt-log",
      "prompt": "ok go with your recommendation; and design the outstanding focus piece; also do a snapshot-swap spike as a gut check",
      "summary": "prompt logged for reuse",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": null,
      "artifacts": [],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0034",
      "shortname": "adr-0015-canvas-hosting-focus-design-snapshot-spike",
      "datetime": "2026-08-26T21:17:40Z",
      "session": "4e957874-10fd-4d1b-a6b7-41042277c103",
      "prompt": "ok go with your recommendation; and design the outstanding focus piece; also do a snapshot-swap spike as a gut check",
      "summary": "ADR-0015 keeps the windowed WebView2 and yields the canvas by snapshot swap; gut-check spike confirms pixel alignment at 150% DPI, WPF composites over the stand-in, clean restore across 8 cycles, ~34ms capture. Focus routing designed after reflection proved the WPF control exposes no CoreWebView2Controller, so MoveFocus is unavailable; SetFocus on the HwndHost handle measured working",
      "kind": "skill",
      "skill": "design",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/adr/0015-canvas-hosting-and-overlay-strategy.md",
        "spikes/webview2-snapshot-swap/RESULT.md"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0035",
      "shortname": "implement-conpty-terminal-runtime",
      "datetime": "2026-08-27T00:28:58Z",
      "session": "4e957874-10fd-4d1b-a6b7-41042277c103",
      "prompt": "yes do the best next action now",
      "summary": "ConPTY terminal runtime, extended ITerminalSession contract, and the D7 conformance suite. Root-caused the child-attachment failure to the test host lacking a real console (not the interop); fixed with an out-of-process helper launched with CREATE_NEW_CONSOLE. 212 tests green",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.Core/Terminal/ConPtyTerminalSession.cs",
        "tests/AiDe.Core.Tests/TerminalSessionConformanceTests.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0036",
      "shortname": "implement-daemon-ipc-boundary",
      "datetime": "2026-08-27T16:14:39Z",
      "session": "4e957874-10fd-4d1b-a6b7-41042277c103",
      "prompt": "yes do the next steps but go back to a tabular summary of tasks",
      "summary": "IPC boundary for the process split: versioned envelope with dual-major handshake, capability registry bound to connection/process/workspace/epoch, DaemonEndpoint with a fixed check order. 25 negative security tests, all seven controls mutation-verified. 237 tests green",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.Core/Ipc/DaemonEndpoint.cs",
        "tests/AiDe.Core.Tests/IpcBoundaryTests.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0037",
      "shortname": "osc-parser",
      "datetime": "2026-08-27T16:50:40Z",
      "session": "osc-parser-2026-08-27",
      "prompt": "do the OSC parser task next; but dont forget to always give me the update of the tasks and status and the best next action <<< both in tabular form when yhou finish a turn",
      "summary": "OSC 133 parser for the terminal runtime, nonce-authenticated and advisory-only. Closed the unknown that gated the whole feature: measured through a real pseudo console that OSC SURVIVES the ConPTY round trip â€” an authenticated OSC 133;D drove the session to Ready, an unauthenticated one left it at Busy. Before this, SessionActivity.Ready was a declared state nothing produced. Per-session 128-bit nonce, length-checked and fixed-time compared; OSC 52/8 refused outright rather than sanitised (no clipboard or hyperlink code path exists to reach); OSC 633 never honoured. First authenticated claim makes OSC authoritative so the output heuristic retires and a shell's own prompt cannot undo the Ready it just announced. Payload capped at 1 KiB with resync so an unterminated flood cannot grow without limit. 7/7 mutations caught. 216 Core tests (+33), all four gates clean. Registered DC-015 after a test passed in 200ms without running its probe: a success check coarser than the claim it stands for.",
      "kind": "skill",
      "skill": "implement",
      "tool": "Claude Code",
      "actor": null,
      "artifacts": [
        "src/AiDe.Core/Terminal/OscParser.cs",
        "src/AiDe.Core/Terminal/TerminalActivityState.cs",
        "tests/AiDe.Core.Tests/OscParserTests.cs",
        "tests/AiDe.Core.Tests/OscRoundTripTests.cs",
        "docs/lessons/defect-classes.md"
      ],
      "tags": [
        "phase-2",
        "terminal",
        "security"
      ],
      "outcome": "success",
      "goal": "Implement the OSC parser: close P2-TERM-06/07 and make SessionActivity.Ready a state the runtime actually produces",
      "done_when": "Allowlisted OSC subset parsed, OSC 52/8 disabled outright, session nonce required before any state claim, forged-OSC-133 negative test red-then-green, wired into ConPtyTerminalSession, four gates clean, committed",
      "git": {
        "sha": "36f0c476b0a26360e8482cf3ec6c55a8413ad763",
        "short": "36f0c476b",
        "branch": "feat/osc-parser",
        "pushed": null
      }
    },
    {
      "id": "al-0203",
      "shortname": "osc-parser",
      "datetime": "2026-08-27T16:50:40Z",
      "session": "osc-parser-2026-08-27",
      "prompt": "do the OSC parser task next; but dont forget to always give me the update of the tasks and status and the best next action <<< both in tabular form when yhou finish a turn",
      "summary": "OSC 133 parser for the terminal runtime, nonce-authenticated and advisory-only. Closed the unknown that gated the whole feature: measured through a real pseudo console that OSC SURVIVES the ConPTY round trip — an authenticated OSC 133;D drove the session to Ready, an unauthenticated one left it at Busy. Before this, SessionActivity.Ready was a declared state nothing produced. Per-session 128-bit nonce, length-checked and fixed-time compared; OSC 52/8 refused outright rather than sanitised (no clipboard or hyperlink code path exists to reach); OSC 633 never honoured. First authenticated claim makes OSC authoritative so the output heuristic retires and a shell's own prompt cannot undo the Ready it just announced. Payload capped at 1 KiB with resync so an unterminated flood cannot grow without limit. 7/7 mutations caught. 216 Core tests (+33), all four gates clean. Registered DC-015 after a test passed in 200ms without running its probe: a success check coarser than the claim it stands for.",
      "kind": "skill",
      "skill": "implement",
      "tool": "Claude Code",
      "actor": null,
      "artifacts": [
        "src/AiDe.Core/Terminal/OscParser.cs",
        "src/AiDe.Core/Terminal/TerminalActivityState.cs",
        "tests/AiDe.Core.Tests/OscParserTests.cs",
        "tests/AiDe.Core.Tests/OscRoundTripTests.cs",
        "docs/lessons/defect-classes.md"
      ],
      "tags": [
        "phase-2",
        "terminal",
        "security"
      ],
      "outcome": "success",
      "goal": "Implement the OSC parser: close P2-TERM-06/07 and make SessionActivity.Ready a state the runtime actually produces",
      "done_when": "Allowlisted OSC subset parsed, OSC 52/8 disabled outright, session nonce required before any state claim, forged-OSC-133 negative test red-then-green, wired into ConPtyTerminalSession, four gates clean, committed",
      "git": {
        "sha": "36f0c476b0a26360e8482cf3ec6c55a8413ad763",
        "short": "36f0c476b",
        "branch": "feat/osc-parser",
        "pushed": null
      }
    },
    {
      "id": "al-0038",
      "shortname": "shell-integration",
      "datetime": "2026-08-27T17:09:04Z",
      "session": "shell-integration-2026-08-27",
      "prompt": "yes write the shell-integration script now",
      "summary": "PowerShell shell integration: the shell-side half that makes the OSC nonce control operate instead of lying dormant. Measured end to end through a real pseudo console â€” a real powershell.exe reaches Ready at its prompt, Busy while a 4s command runs, and Ready again after. Closed two unknowns: PSReadLine DOES load under -NoProfile inside ConPTY, and -EncodedCommand survives the ConPTY launch path. Key rule is all-or-nothing: an authenticated claim retires the output heuristic, so an integration emitting D/A/B but not C would pin a session at Ready for the length of every command â€” worse than no integration. The script therefore checks it can hook line-accept BEFORE overriding anything and installs nothing if it cannot; that bail-out has a behavioural test running the real script in a PowerShell with an emptied module path. Nonce moved to StartAsync since it must exist before the process does; non-hex nonces are refused rather than escaped. 11/11 mutations caught (2 survived the first run, both faults in my mutations, not the controls). 233 Core tests (+17), four gates clean.",
      "kind": "skill",
      "skill": "implement",
      "tool": "Claude Code",
      "actor": null,
      "artifacts": [
        "src/AiDe.Core/Terminal/ShellIntegration.cs",
        "src/AiDe.Core/Terminal/ConPtyTerminalSession.cs",
        "tests/AiDe.Core.Tests/ShellIntegrationTests.cs",
        "tests/AiDe.Core.Tests/ShellIntegrationRoundTripTests.cs"
      ],
      "tags": [
        "phase-2",
        "terminal",
        "security"
      ],
      "outcome": "success",
      "goal": "Ship the shell-integration script so the OSC nonce control actually executes in a real session",
      "done_when": "A PowerShell integration emits the full 133 loop signed with the session nonce, proven through a real ConPTY that the session reaches Ready at a prompt and Busy during a command, gates clean, committed",
      "git": {
        "sha": "131f6fcb014ba0e2dde58a275f3c19e19dc6fb09",
        "short": "131f6fcb0",
        "branch": "feat/shell-integration",
        "pushed": null
      }
    },
    {
      "id": "al-0204",
      "shortname": "shell-integration",
      "datetime": "2026-08-27T17:09:04Z",
      "session": "shell-integration-2026-08-27",
      "prompt": "yes write the shell-integration script now",
      "summary": "PowerShell shell integration: the shell-side half that makes the OSC nonce control operate instead of lying dormant. Measured end to end through a real pseudo console — a real powershell.exe reaches Ready at its prompt, Busy while a 4s command runs, and Ready again after. Closed two unknowns: PSReadLine DOES load under -NoProfile inside ConPTY, and -EncodedCommand survives the ConPTY launch path. Key rule is all-or-nothing: an authenticated claim retires the output heuristic, so an integration emitting D/A/B but not C would pin a session at Ready for the length of every command — worse than no integration. The script therefore checks it can hook line-accept BEFORE overriding anything and installs nothing if it cannot; that bail-out has a behavioural test running the real script in a PowerShell with an emptied module path. Nonce moved to StartAsync since it must exist before the process does; non-hex nonces are refused rather than escaped. 11/11 mutations caught (2 survived the first run, both faults in my mutations, not the controls). 233 Core tests (+17), four gates clean.",
      "kind": "skill",
      "skill": "implement",
      "tool": "Claude Code",
      "actor": null,
      "artifacts": [
        "src/AiDe.Core/Terminal/ShellIntegration.cs",
        "src/AiDe.Core/Terminal/ConPtyTerminalSession.cs",
        "tests/AiDe.Core.Tests/ShellIntegrationTests.cs",
        "tests/AiDe.Core.Tests/ShellIntegrationRoundTripTests.cs"
      ],
      "tags": [
        "phase-2",
        "terminal",
        "security"
      ],
      "outcome": "success",
      "goal": "Ship the shell-integration script so the OSC nonce control actually executes in a real session",
      "done_when": "A PowerShell integration emits the full 133 loop signed with the session nonce, proven through a real ConPTY that the session reaches Ready at a prompt and Busy during a command, gates clean, committed",
      "git": {
        "sha": "131f6fcb014ba0e2dde58a275f3c19e19dc6fb09",
        "short": "131f6fcb0",
        "branch": "feat/shell-integration",
        "pushed": null
      }
    },
    {
      "id": "al-0039",
      "shortname": "terminal-renderer",
      "datetime": "2026-08-27T17:39:42Z",
      "session": "terminal-renderer-2026-08-27",
      "prompt": "yes do the best next action you suggested",
      "summary": "WPF terminal renderer: screen model and VT parser in Core (no WPF), GlyphRun-per-run renderer, palette as App.xaml tokens, key mapping, and the TerminalSurface that finally passes Integration: PowerShell. Measured 5.50ms p95 for a 200x50 full redraw against a 16.67ms budget â€” S3 predicted 6.64ms for this path and 142.80ms for per-cell; a mutation reverting to per-cell IS caught, so S3's constraint is now a control rather than a note. CORRECTED DC-014: its condition ('the host must own a real console') is too strong and read literally says the product's own GUI architecture cannot host terminals. Two stand-ins gave two wrong answers â€” FreeConsole() does not reproduce a GUI host, and a WinExe probe still fails when it inherits a test runner's redirected handles. The real rule is WHICH STANDARD HANDLES THE HOST WAS GIVEN. Added AiDe.App.TerminalProbe, a WinExe whose OutputType is the thing under test. 11/11 mutations caught. Ran the real app: a live PowerShell prompt renders, and it surfaced that PSReadLine is disabled here (screen-reader detection), so shell integration correctly declines to install rather than emit a half-loop â€” carried as a known limitation, NOT mitigated, because forcing it would override an accessibility accommodation. 364 tests (+81), four gates clean.",
      "kind": "skill",
      "skill": "implement",
      "tool": "Claude Code",
      "actor": null,
      "artifacts": [
        "src/AiDe.Core/Terminal/TerminalScreen.cs",
        "src/AiDe.Core/Terminal/VtParser.cs",
        "src/AiDe.App/Workbench/TerminalView.cs",
        "src/AiDe.App/Workbench/TerminalSurface.cs",
        "tests/AiDe.App.TerminalProbe/Program.cs",
        "docs/lessons/defect-classes.md"
      ],
      "tags": [
        "phase-2",
        "terminal",
        "ui",
        "performance"
      ],
      "outcome": "success",
      "goal": "Build the WPF terminal renderer and the surface that opens a session with shell integration on",
      "done_when": "A terminal surface renders real session output with colour and cursor, keyboard input reaches the session, the draw path is measured against S3's budget, gates clean, committed",
      "git": {
        "sha": "3632d0b7817712a3cfc3bb368eacc32f9b6fe245",
        "short": "3632d0b78",
        "branch": "feat/terminal-renderer",
        "pushed": null
      }
    },
    {
      "id": "al-0205",
      "shortname": "terminal-renderer",
      "datetime": "2026-08-27T17:39:42Z",
      "session": "terminal-renderer-2026-08-27",
      "prompt": "yes do the best next action you suggested",
      "summary": "WPF terminal renderer: screen model and VT parser in Core (no WPF), GlyphRun-per-run renderer, palette as App.xaml tokens, key mapping, and the TerminalSurface that finally passes Integration: PowerShell. Measured 5.50ms p95 for a 200x50 full redraw against a 16.67ms budget — S3 predicted 6.64ms for this path and 142.80ms for per-cell; a mutation reverting to per-cell IS caught, so S3's constraint is now a control rather than a note. CORRECTED DC-014: its condition ('the host must own a real console') is too strong and read literally says the product's own GUI architecture cannot host terminals. Two stand-ins gave two wrong answers — FreeConsole() does not reproduce a GUI host, and a WinExe probe still fails when it inherits a test runner's redirected handles. The real rule is WHICH STANDARD HANDLES THE HOST WAS GIVEN. Added AiDe.App.TerminalProbe, a WinExe whose OutputType is the thing under test. 11/11 mutations caught. Ran the real app: a live PowerShell prompt renders, and it surfaced that PSReadLine is disabled here (screen-reader detection), so shell integration correctly declines to install rather than emit a half-loop — carried as a known limitation, NOT mitigated, because forcing it would override an accessibility accommodation. 364 tests (+81), four gates clean.",
      "kind": "skill",
      "skill": "implement",
      "tool": "Claude Code",
      "actor": null,
      "artifacts": [
        "src/AiDe.Core/Terminal/TerminalScreen.cs",
        "src/AiDe.Core/Terminal/VtParser.cs",
        "src/AiDe.App/Workbench/TerminalView.cs",
        "src/AiDe.App/Workbench/TerminalSurface.cs",
        "tests/AiDe.App.TerminalProbe/Program.cs",
        "docs/lessons/defect-classes.md"
      ],
      "tags": [
        "phase-2",
        "terminal",
        "ui",
        "performance"
      ],
      "outcome": "success",
      "goal": "Build the WPF terminal renderer and the surface that opens a session with shell integration on",
      "done_when": "A terminal surface renders real session output with colour and cursor, keyboard input reaches the session, the draw path is measured against S3's budget, gates clean, committed",
      "git": {
        "sha": "3632d0b7817712a3cfc3bb368eacc32f9b6fe245",
        "short": "3632d0b78",
        "branch": "feat/terminal-renderer",
        "pushed": null
      }
    },
    {
      "id": "al-0040",
      "shortname": "ipc-transport",
      "datetime": "2026-08-27T18:41:05Z",
      "session": "ipc-transport-2026-08-27",
      "prompt": "do your best next action (named-pipe transport...)",
      "summary": "Named-pipe transport and AiDe.Daemon.exe: the process split ADR-0009 deferred is now a real second process. Length-prefixed framing with the cap checked BEFORE allocation; pipe name derived by hash so it does not disclose the workspace path; one explicit owner-only ACL read back by a test; peer SID and PID from the kernel, derived after the first frame because Windows refuses impersonation until a read has happened; workspace lock taken before a pipe exists; daemon exits when nobody needs it. MUTATION FOUND THE SAME SHAPE THREE TIMES, now registered as DC-016 (a control that cannot fire in the environment that verifies it): an in-flight semaphore unreachable by construction (REMOVED, and the real bound documented â€” serial service plus caps, backpressure not refusal); the owner-SID check unreachable in a single-user environment (made testable by injecting the expected SID); and WorkspaceLock inert in-process because a Windows mutex is thread-owned and re-entrant, which matters because ADR-0009 keeps an in-process daemon supported. Two defects found by tests first: a deaf client could hold a listener forever (response-write timeout added), and the idle reaper sampled instead of remembering, so a short-lived client left the daemon waiting out the 60s startup grace. 10/10 mutations caught; four needed a runtime-false because if(false) trips CS0162 and fails the build. Daemon serves ping/epoch only â€” moving the core's command surface behind it is the next piece, stated rather than half-done. 406 tests (+42), four gates clean.",
      "kind": "skill",
      "skill": "implement",
      "tool": "Claude Code",
      "actor": null,
      "artifacts": [
        "src/AiDe.Core/Ipc/IpcFraming.cs",
        "src/AiDe.Core/Ipc/IpcPipe.cs",
        "src/AiDe.Core/Ipc/IpcServer.cs",
        "src/AiDe.Core/Ipc/IpcClient.cs",
        "src/AiDe.Daemon/Program.cs",
        "tests/AiDe.Core.Tests/DaemonProcessTests.cs",
        "docs/lessons/defect-classes.md"
      ],
      "tags": [
        "phase-2",
        "ipc",
        "security",
        "process-split"
      ],
      "outcome": "success",
      "goal": "Build the named-pipe transport and AiDe.Daemon.exe, turning the transport-free IPC decision layer into a real cross-process boundary",
      "done_when": "A client reaches a daemon over an owner-SID-restricted pipe; workspace lock, orphan grace exit and flood bounds work; security tests run against a real pipe; gates clean; committed",
      "git": {
        "sha": "c1cc9defa01ba1d4fbf91b1672c3a54fa2cf172e",
        "short": "c1cc9defa",
        "branch": "feat/ipc-transport",
        "pushed": null
      }
    },
    {
      "id": "al-0206",
      "shortname": "ipc-transport",
      "datetime": "2026-08-27T18:41:05Z",
      "session": "ipc-transport-2026-08-27",
      "prompt": "do your best next action (named-pipe transport...)",
      "summary": "Named-pipe transport and AiDe.Daemon.exe: the process split ADR-0009 deferred is now a real second process. Length-prefixed framing with the cap checked BEFORE allocation; pipe name derived by hash so it does not disclose the workspace path; one explicit owner-only ACL read back by a test; peer SID and PID from the kernel, derived after the first frame because Windows refuses impersonation until a read has happened; workspace lock taken before a pipe exists; daemon exits when nobody needs it. MUTATION FOUND THE SAME SHAPE THREE TIMES, now registered as DC-016 (a control that cannot fire in the environment that verifies it): an in-flight semaphore unreachable by construction (REMOVED, and the real bound documented — serial service plus caps, backpressure not refusal); the owner-SID check unreachable in a single-user environment (made testable by injecting the expected SID); and WorkspaceLock inert in-process because a Windows mutex is thread-owned and re-entrant, which matters because ADR-0009 keeps an in-process daemon supported. Two defects found by tests first: a deaf client could hold a listener forever (response-write timeout added), and the idle reaper sampled instead of remembering, so a short-lived client left the daemon waiting out the 60s startup grace. 10/10 mutations caught; four needed a runtime-false because if(false) trips CS0162 and fails the build. Daemon serves ping/epoch only — moving the core's command surface behind it is the next piece, stated rather than half-done. 406 tests (+42), four gates clean.",
      "kind": "skill",
      "skill": "implement",
      "tool": "Claude Code",
      "actor": null,
      "artifacts": [
        "src/AiDe.Core/Ipc/IpcFraming.cs",
        "src/AiDe.Core/Ipc/IpcPipe.cs",
        "src/AiDe.Core/Ipc/IpcServer.cs",
        "src/AiDe.Core/Ipc/IpcClient.cs",
        "src/AiDe.Daemon/Program.cs",
        "tests/AiDe.Core.Tests/DaemonProcessTests.cs",
        "docs/lessons/defect-classes.md"
      ],
      "tags": [
        "phase-2",
        "ipc",
        "security",
        "process-split"
      ],
      "outcome": "success",
      "goal": "Build the named-pipe transport and AiDe.Daemon.exe, turning the transport-free IPC decision layer into a real cross-process boundary",
      "done_when": "A client reaches a daemon over an owner-SID-restricted pipe; workspace lock, orphan grace exit and flood bounds work; security tests run against a real pipe; gates clean; committed",
      "git": {
        "sha": "c1cc9defa01ba1d4fbf91b1672c3a54fa2cf172e",
        "short": "c1cc9defa",
        "branch": "feat/ipc-transport",
        "pushed": null
      }
    },
    {
      "id": "al-0041",
      "shortname": "daemon-operations",
      "datetime": "2026-08-27T19:16:22Z",
      "session": "daemon-operations-2026-08-27",
      "prompt": "do your best next action - the daemon endpoint and then carry on to the P2-UPGRADE",
      "summary": "The core's read surface now crosses the boundary: describe/impact/find/knowledge registered on the daemon endpoint, a typed WorkspaceClient proxy, and the daemon opening a real WorkspaceCore. Tests assert AGREEMENT â€” each projection run in process and over the pipe against one store, whole results compared, because serialisation is where agreement is lost. A protocol gap surfaced and was closed properly: every command is judged against the epoch it claims, so a freshly connected shell could not ask for the epoch (asking is itself a command); the handshake now returns IpcOpenResult carrying capability AND epoch, rather than putting a hole in the fence. Enums travel as strings so adding a member cannot renumber existing ones under a dual-major handshake. Also caught my own vacuous tests: the fixture committed under a scope the assertions did not carry, so both sides were empty and compared equal â€” non-vacuity guards added (DC-015). Dispatch deliberately NOT moved: it carries ADR-0010 two-phase receipts and half-crossing is worse than not yet. 418 tests (+11), four gates clean.",
      "kind": "skill",
      "skill": "implement",
      "tool": "Claude Code",
      "actor": null,
      "artifacts": [
        "src/AiDe.Core/Ipc/WorkspaceOperations.cs",
        "src/AiDe.Core/Ipc/WorkspaceClient.cs",
        "src/AiDe.Daemon/Program.cs",
        "tests/AiDe.Core.Tests/DaemonOperationsTests.cs"
      ],
      "tags": [
        "phase-2",
        "ipc",
        "process-split"
      ],
      "outcome": "success",
      "goal": "Move the core's read surface behind the daemon endpoint so the trust boundary is actually used",
      "done_when": "describe/impact/find/knowledge answered by a daemon over a pipe, agreeing with the in-process projection, gates clean, committed",
      "git": {
        "sha": "905e602e728b029376cf582277cd71924ad0ad95",
        "short": "905e602e7",
        "branch": "feat/daemon-operations",
        "pushed": null
      }
    },
    {
      "id": "al-0207",
      "shortname": "daemon-operations",
      "datetime": "2026-08-27T19:16:22Z",
      "session": "daemon-operations-2026-08-27",
      "prompt": "do your best next action - the daemon endpoint and then carry on to the P2-UPGRADE",
      "summary": "The core's read surface now crosses the boundary: describe/impact/find/knowledge registered on the daemon endpoint, a typed WorkspaceClient proxy, and the daemon opening a real WorkspaceCore. Tests assert AGREEMENT — each projection run in process and over the pipe against one store, whole results compared, because serialisation is where agreement is lost. A protocol gap surfaced and was closed properly: every command is judged against the epoch it claims, so a freshly connected shell could not ask for the epoch (asking is itself a command); the handshake now returns IpcOpenResult carrying capability AND epoch, rather than putting a hole in the fence. Enums travel as strings so adding a member cannot renumber existing ones under a dual-major handshake. Also caught my own vacuous tests: the fixture committed under a scope the assertions did not carry, so both sides were empty and compared equal — non-vacuity guards added (DC-015). Dispatch deliberately NOT moved: it carries ADR-0010 two-phase receipts and half-crossing is worse than not yet. 418 tests (+11), four gates clean.",
      "kind": "skill",
      "skill": "implement",
      "tool": "Claude Code",
      "actor": null,
      "artifacts": [
        "src/AiDe.Core/Ipc/WorkspaceOperations.cs",
        "src/AiDe.Core/Ipc/WorkspaceClient.cs",
        "src/AiDe.Daemon/Program.cs",
        "tests/AiDe.Core.Tests/DaemonOperationsTests.cs"
      ],
      "tags": [
        "phase-2",
        "ipc",
        "process-split"
      ],
      "outcome": "success",
      "goal": "Move the core's read surface behind the daemon endpoint so the trust boundary is actually used",
      "done_when": "describe/impact/find/knowledge answered by a daemon over a pipe, agreeing with the in-process projection, gates clean, committed",
      "git": {
        "sha": "905e602e728b029376cf582277cd71924ad0ad95",
        "short": "905e602e7",
        "branch": "feat/daemon-operations",
        "pushed": null
      }
    },
    {
      "id": "al-0042",
      "shortname": "upgrade-rollback",
      "datetime": "2026-08-27T19:30:14Z",
      "session": "upgrade-rollback-2026-08-27",
      "prompt": "do your best next action - the daemon endpoint and then carry on to the P2-UPGRADE",
      "summary": "Upgrade and rollback: journal, snapshot, health gate, coordinator, side-by-side installs, and startup recovery wired into the daemon so the mechanism is used rather than merely present. Ordering is the design â€” snapshot, journal, migrate, gate, commit â€” with the point of no return last, because an upgrade that fails halfway leaves a store no binary can read. The 60s gate budget is ENFORCED, keeping the slow replay check asynchronous (P1-PERF: 50k-edge replay vs a 15-minute RTO); a gate that only documented its budget would pass it. THREE DEFECTS FOUND: rollback derived the store path from the snapshot's filename and passed only because the fixture put both in one folder; the atomic replace was not atomic because File.ReadAllText does not share delete, so a concurrent reader made the WRITER throw on Windows; and even after that fix a delete-pending file needs a bounded retry. TWO TESTS PROVED NOTHING until mutation said so â€” the enum-naming test asserted a round-tripped value, which numeric enums also satisfy, and nothing distinguished atomic replacement from an in-place write. Pruning protects the current build explicitly because after a rollback the current version is an older one. 13/13 mutations caught. 454 tests (+36), four gates clean. Health gate CONTENTS for a real schema migration are not built â€” the store has no migration chain yet â€” stated rather than faked.",
      "kind": "skill",
      "skill": "implement",
      "tool": "Claude Code",
      "actor": null,
      "artifacts": [
        "src/AiDe.Core/Upgrade/MigrationJournal.cs",
        "src/AiDe.Core/Upgrade/HealthGate.cs",
        "src/AiDe.Core/Upgrade/UpgradeCoordinator.cs",
        "src/AiDe.Core/Upgrade/DaemonInstallation.cs",
        "tests/AiDe.Core.Tests/UpgradeTests.cs",
        "tests/AiDe.Core.Tests/DaemonInstallationTests.cs"
      ],
      "tags": [
        "phase-2",
        "upgrade",
        "rollback"
      ],
      "outcome": "success",
      "goal": "Build P2-UPGRADE-01..03: side-by-side daemon builds, health gate, rollback, and a durable migration journal",
      "done_when": "Upgrade repoints on a passing gate, rolls back on a failing one, and an interrupted migration is undone at the next start; gates clean; committed",
      "git": {
        "sha": "1a71f8ca6565eb7c610076cc46d69dee199bb812",
        "short": "1a71f8ca6",
        "branch": "feat/daemon-operations",
        "pushed": null
      }
    },
    {
      "id": "al-0208",
      "shortname": "upgrade-rollback",
      "datetime": "2026-08-27T19:30:14Z",
      "session": "upgrade-rollback-2026-08-27",
      "prompt": "do your best next action - the daemon endpoint and then carry on to the P2-UPGRADE",
      "summary": "Upgrade and rollback: journal, snapshot, health gate, coordinator, side-by-side installs, and startup recovery wired into the daemon so the mechanism is used rather than merely present. Ordering is the design — snapshot, journal, migrate, gate, commit — with the point of no return last, because an upgrade that fails halfway leaves a store no binary can read. The 60s gate budget is ENFORCED, keeping the slow replay check asynchronous (P1-PERF: 50k-edge replay vs a 15-minute RTO); a gate that only documented its budget would pass it. THREE DEFECTS FOUND: rollback derived the store path from the snapshot's filename and passed only because the fixture put both in one folder; the atomic replace was not atomic because File.ReadAllText does not share delete, so a concurrent reader made the WRITER throw on Windows; and even after that fix a delete-pending file needs a bounded retry. TWO TESTS PROVED NOTHING until mutation said so — the enum-naming test asserted a round-tripped value, which numeric enums also satisfy, and nothing distinguished atomic replacement from an in-place write. Pruning protects the current build explicitly because after a rollback the current version is an older one. 13/13 mutations caught. 454 tests (+36), four gates clean. Health gate CONTENTS for a real schema migration are not built — the store has no migration chain yet — stated rather than faked.",
      "kind": "skill",
      "skill": "implement",
      "tool": "Claude Code",
      "actor": null,
      "artifacts": [
        "src/AiDe.Core/Upgrade/MigrationJournal.cs",
        "src/AiDe.Core/Upgrade/HealthGate.cs",
        "src/AiDe.Core/Upgrade/UpgradeCoordinator.cs",
        "src/AiDe.Core/Upgrade/DaemonInstallation.cs",
        "tests/AiDe.Core.Tests/UpgradeTests.cs",
        "tests/AiDe.Core.Tests/DaemonInstallationTests.cs"
      ],
      "tags": [
        "phase-2",
        "upgrade",
        "rollback"
      ],
      "outcome": "success",
      "goal": "Build P2-UPGRADE-01..03: side-by-side daemon builds, health gate, rollback, and a durable migration journal",
      "done_when": "Upgrade repoints on a passing gate, rolls back on a failing one, and an interrupted migration is undone at the next start; gates clean; committed",
      "git": {
        "sha": "1a71f8ca6565eb7c610076cc46d69dee199bb812",
        "short": "1a71f8ca6",
        "branch": "feat/daemon-operations",
        "pushed": null
      }
    },
    {
      "id": "al-0043",
      "shortname": "shell-uses-daemon",
      "datetime": "2026-08-27T20:01:09Z",
      "session": "shell-uses-daemon-2026-08-27",
      "prompt": "do your best next action",
      "summary": "The shell now launches and uses the daemon. IWorkspaceQueries is the seam both hosting modes satisfy (ADR-0009 keeps both), ShellBootstrap connects-then-launches, and the daemon ships in a folder beside the shell. MEASURED BY RUNNING IT: the app spawns exactly one daemon and renders its evidence panes from answers that crossed the pipe. No fallback to in-process when the daemon will not start â€” that would work while abandoning the boundary, the lock and the epoch fence exactly when they matter (DC-011). FOUND A DEFECT 459 PASSING TESTS COULD NOT SEE: making the pane async left the factory binding Rows and StatusMessage at construction, so both evidence panes sat on 'Loading evidence...' permanently; the pane view model was correct and covered, and nothing asserted on what the CONTROL showed. Found by running the app and looking at it. Registered DC-017 (verified one layer below the one that actually fails) with SurfaceContentTests as the control â€” it builds the surface through the real factory, pumps the dispatcher, and asserts on what is displayed. Mutation then found an unreachable catch in my own fix (the pane already degrades internally), removed per DC-016. Also fixed the heading showing a hash where a user wants a folder name. 463 tests (+9 net after refactor), four gates clean.",
      "kind": "skill",
      "skill": "implement",
      "tool": "Claude Code",
      "actor": null,
      "artifacts": [
        "src/AiDe.Core/Projections/IWorkspaceQueries.cs",
        "src/AiDe.Core/Ipc/ShellBootstrap.cs",
        "src/AiDe.App/ViewModels/MainWindowViewModel.cs",
        "tests/AiDe.App.Tests/SurfaceContentTests.cs",
        "docs/lessons/defect-classes.md"
      ],
      "tags": [
        "phase-2",
        "ipc",
        "process-split",
        "ui"
      ],
      "outcome": "success",
      "goal": "Make the shell launch and use the daemon, so the process split is the product rather than a demonstration",
      "done_when": "A real daemon is started on demand, the shell queries it over the pipe, the in-process path survives behind a seam, gates clean, committed",
      "git": {
        "sha": "df783e9b407705f314486aca3fb83c2303537a15",
        "short": "df783e9b4",
        "branch": "feat/shell-uses-daemon",
        "pushed": null
      }
    },
    {
      "id": "al-0209",
      "shortname": "shell-uses-daemon",
      "datetime": "2026-08-27T20:01:09Z",
      "session": "shell-uses-daemon-2026-08-27",
      "prompt": "do your best next action",
      "summary": "The shell now launches and uses the daemon. IWorkspaceQueries is the seam both hosting modes satisfy (ADR-0009 keeps both), ShellBootstrap connects-then-launches, and the daemon ships in a folder beside the shell. MEASURED BY RUNNING IT: the app spawns exactly one daemon and renders its evidence panes from answers that crossed the pipe. No fallback to in-process when the daemon will not start — that would work while abandoning the boundary, the lock and the epoch fence exactly when they matter (DC-011). FOUND A DEFECT 459 PASSING TESTS COULD NOT SEE: making the pane async left the factory binding Rows and StatusMessage at construction, so both evidence panes sat on 'Loading evidence...' permanently; the pane view model was correct and covered, and nothing asserted on what the CONTROL showed. Found by running the app and looking at it. Registered DC-017 (verified one layer below the one that actually fails) with SurfaceContentTests as the control — it builds the surface through the real factory, pumps the dispatcher, and asserts on what is displayed. Mutation then found an unreachable catch in my own fix (the pane already degrades internally), removed per DC-016. Also fixed the heading showing a hash where a user wants a folder name. 463 tests (+9 net after refactor), four gates clean.",
      "kind": "skill",
      "skill": "implement",
      "tool": "Claude Code",
      "actor": null,
      "artifacts": [
        "src/AiDe.Core/Projections/IWorkspaceQueries.cs",
        "src/AiDe.Core/Ipc/ShellBootstrap.cs",
        "src/AiDe.App/ViewModels/MainWindowViewModel.cs",
        "tests/AiDe.App.Tests/SurfaceContentTests.cs",
        "docs/lessons/defect-classes.md"
      ],
      "tags": [
        "phase-2",
        "ipc",
        "process-split",
        "ui"
      ],
      "outcome": "success",
      "goal": "Make the shell launch and use the daemon, so the process split is the product rather than a demonstration",
      "done_when": "A real daemon is started on demand, the shell queries it over the pipe, the in-process path survives behind a seam, gates clean, committed",
      "git": {
        "sha": "df783e9b407705f314486aca3fb83c2303537a15",
        "short": "df783e9b4",
        "branch": "feat/shell-uses-daemon",
        "pushed": null
      }
    },
    {
      "id": "al-0044",
      "shortname": "refresh-across-boundary",
      "datetime": "2026-08-27T20:54:28Z",
      "session": "refresh-across-boundary-2026-08-27",
      "prompt": "do your best next action",
      "summary": "The first WRITE crosses the boundary: scope refresh. Started-and-polled rather than awaited on the wire, because a scope has a 60s budget and the lane serves one request at a time per connection â€” the control lane carries commands, and a command that starts long work returns once it is started. The command id is the idempotency key and this is where it first matters across processes; deduplication has two guards and only the concurrent one (TryAdd) is load-bearing, which a mutation run proved by disabling it with nothing failing. Job records are bounded because they are keyed by a caller-chosen id; a running job is never evicted. An incomplete extraction is a failure, not a refresh of zero. Reads and writes are separate seams. TWO DEFECTS FOUND BY MUTATION: announcements were not marshalled to the UI thread, so a re-index reporting its outcome from background work would throw exactly when telling the user something (fixed in WorkbenchAnnouncer, which owns the control); and RecordingAnnouncer was not thread-safe. The existing SC 2.5.7 catalog conformance test caught my palette entry having no handler. 9/9 mutations caught. 479 tests (+16), four gates clean. PROMPT DISPATCH IS BLOCKED ON A DIVERGENCE, NOT EFFORT: the design puts terminals in the daemon ('a crashed daemon must not leave agent CLIs running headless') and TerminalSurface creates them in the shell â€” recorded rather than half-crossed.",
      "kind": "skill",
      "skill": "implement",
      "tool": "Claude Code",
      "actor": null,
      "artifacts": [
        "src/AiDe.Core/Ipc/ScopeRefreshService.cs",
        "src/AiDe.Core/Ipc/IWorkspaceCommands.cs",
        "src/AiDe.App/Workbench/WorkbenchController.cs",
        "tests/AiDe.Core.Tests/ScopeRefreshTests.cs"
      ],
      "tags": [
        "phase-2",
        "ipc",
        "process-split",
        "ingestion"
      ],
      "outcome": "success",
      "goal": "Move the first write across the daemon boundary â€” scope refresh â€” so a daemon-backed workspace can be told to index",
      "done_when": "A shell re-indexes a scope over the pipe, idempotently, with a keyboard-reachable trigger; gates clean; committed",
      "git": {
        "sha": "6e377660d8257dc0cd796be4ffddb44507f2bf61",
        "short": "6e377660d",
        "branch": "feat/refresh-across-boundary",
        "pushed": null
      }
    },
    {
      "id": "al-0210",
      "shortname": "refresh-across-boundary",
      "datetime": "2026-08-27T20:54:28Z",
      "session": "refresh-across-boundary-2026-08-27",
      "prompt": "do your best next action",
      "summary": "The first WRITE crosses the boundary: scope refresh. Started-and-polled rather than awaited on the wire, because a scope has a 60s budget and the lane serves one request at a time per connection — the control lane carries commands, and a command that starts long work returns once it is started. The command id is the idempotency key and this is where it first matters across processes; deduplication has two guards and only the concurrent one (TryAdd) is load-bearing, which a mutation run proved by disabling it with nothing failing. Job records are bounded because they are keyed by a caller-chosen id; a running job is never evicted. An incomplete extraction is a failure, not a refresh of zero. Reads and writes are separate seams. TWO DEFECTS FOUND BY MUTATION: announcements were not marshalled to the UI thread, so a re-index reporting its outcome from background work would throw exactly when telling the user something (fixed in WorkbenchAnnouncer, which owns the control); and RecordingAnnouncer was not thread-safe. The existing SC 2.5.7 catalog conformance test caught my palette entry having no handler. 9/9 mutations caught. 479 tests (+16), four gates clean. PROMPT DISPATCH IS BLOCKED ON A DIVERGENCE, NOT EFFORT: the design puts terminals in the daemon ('a crashed daemon must not leave agent CLIs running headless') and TerminalSurface creates them in the shell — recorded rather than half-crossed.",
      "kind": "skill",
      "skill": "implement",
      "tool": "Claude Code",
      "actor": null,
      "artifacts": [
        "src/AiDe.Core/Ipc/ScopeRefreshService.cs",
        "src/AiDe.Core/Ipc/IWorkspaceCommands.cs",
        "src/AiDe.App/Workbench/WorkbenchController.cs",
        "tests/AiDe.Core.Tests/ScopeRefreshTests.cs"
      ],
      "tags": [
        "phase-2",
        "ipc",
        "process-split",
        "ingestion"
      ],
      "outcome": "success",
      "goal": "Move the first write across the daemon boundary — scope refresh — so a daemon-backed workspace can be told to index",
      "done_when": "A shell re-indexes a scope over the pipe, idempotently, with a keyboard-reachable trigger; gates clean; committed",
      "git": {
        "sha": "6e377660d8257dc0cd796be4ffddb44507f2bf61",
        "short": "6e377660d",
        "branch": "feat/refresh-across-boundary",
        "pushed": null
      }
    },
    {
      "id": "al-0045",
      "shortname": "privacy-markers",
      "datetime": "2026-08-27T21:16:27Z",
      "session": "privacy-markers-2026-08-27",
      "prompt": "do your best next action then we can go through decisions to make",
      "summary": "P2-PRIV-01/02 by seeded marker rather than by inference. FOUND THE PRIVACY NET HAD A HOLE: TelemetryTests enforces the floor over ActivitySources named aide.*, and every source added with the process split was named AiDe.Core.* â€” so for four commits the IPC boundary, terminal runtime and upgrade coordinator emitted spans no privacy assertion could see, including spans on the first cross-process trust boundary. Renamed them and added a control that fails when one escapes; registered DC-018 (a guard that watches by name, and a name that moved). THAT CONTROL WAS ITSELF VACUOUS at first â€” it matched 'new ActivitySource(...)' while every declaration is target-typed '= new(...)', so it scanned ZERO sources and passed; mutation caught it, and it now asserts a minimum match count. The terminal probe also lied twice before it was fixed: the seed must arrive before any absence is asserted, and the first run could not read workspace.db because SQLite held it open, so the store â€” the most important file â€” was never scanned while the probe reported success. The core is now closed before scanning and an unreadable file fails the run. Command line seeded as well as output, because the privacy analysis makes them separate claims and nothing tested the command-line one. 4/4 mutations caught. 487 tests (+8), four gates clean.",
      "kind": "skill",
      "skill": "implement",
      "tool": "Claude Code",
      "actor": null,
      "artifacts": [
        "tests/AiDe.Core.Tests/PrivacyMarkerTests.cs",
        "tests/AiDe.Core.Tests/TerminalPrivacyTests.cs",
        "docs/lessons/defect-classes.md"
      ],
      "tags": [
        "phase-2",
        "privacy",
        "telemetry"
      ],
      "outcome": "success",
      "goal": "P2-PRIV-01/02: seed a secret and prove it reaches no store, log, metric or trace",
      "done_when": "Terminal output and daemon payloads proven absent from spans and workspace files by seeded markers, with non-vacuity guards; gates clean; committed",
      "git": {
        "sha": "2ebc71f6d3774e221e0d8dc6a86870a079e17342",
        "short": "2ebc71f6d",
        "branch": "feat/privacy-markers",
        "pushed": null
      }
    },
    {
      "id": "al-0211",
      "shortname": "privacy-markers",
      "datetime": "2026-08-27T21:16:27Z",
      "session": "privacy-markers-2026-08-27",
      "prompt": "do your best next action then we can go through decisions to make",
      "summary": "P2-PRIV-01/02 by seeded marker rather than by inference. FOUND THE PRIVACY NET HAD A HOLE: TelemetryTests enforces the floor over ActivitySources named aide.*, and every source added with the process split was named AiDe.Core.* — so for four commits the IPC boundary, terminal runtime and upgrade coordinator emitted spans no privacy assertion could see, including spans on the first cross-process trust boundary. Renamed them and added a control that fails when one escapes; registered DC-018 (a guard that watches by name, and a name that moved). THAT CONTROL WAS ITSELF VACUOUS at first — it matched 'new ActivitySource(...)' while every declaration is target-typed '= new(...)', so it scanned ZERO sources and passed; mutation caught it, and it now asserts a minimum match count. The terminal probe also lied twice before it was fixed: the seed must arrive before any absence is asserted, and the first run could not read workspace.db because SQLite held it open, so the store — the most important file — was never scanned while the probe reported success. The core is now closed before scanning and an unreadable file fails the run. Command line seeded as well as output, because the privacy analysis makes them separate claims and nothing tested the command-line one. 4/4 mutations caught. 487 tests (+8), four gates clean.",
      "kind": "skill",
      "skill": "implement",
      "tool": "Claude Code",
      "actor": null,
      "artifacts": [
        "tests/AiDe.Core.Tests/PrivacyMarkerTests.cs",
        "tests/AiDe.Core.Tests/TerminalPrivacyTests.cs",
        "docs/lessons/defect-classes.md"
      ],
      "tags": [
        "phase-2",
        "privacy",
        "telemetry"
      ],
      "outcome": "success",
      "goal": "P2-PRIV-01/02: seed a secret and prove it reaches no store, log, metric or trace",
      "done_when": "Terminal output and daemon payloads proven absent from spans and workspace files by seeded markers, with non-vacuity guards; gates clean; committed",
      "git": {
        "sha": "2ebc71f6d3774e221e0d8dc6a86870a079e17342",
        "short": "2ebc71f6d",
        "branch": "feat/privacy-markers",
        "pushed": null
      }
    },
    {
      "id": "al-0046",
      "shortname": "my sessions terminated after my machine restarted overnight; ground yourâ€¦",
      "datetime": "2026-08-28T15:37:49Z",
      "session": "prompt-log",
      "prompt": "my sessions terminated after my machine restarted overnight; ground yourself in the repo, directives, guidance, skills and knowledge; review the session history and audit log; baseline on all tasks done and what tasks are still needed to be done; also clean up any work trees not in use anymore",
      "summary": "prompt logged for reuse",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": null,
      "artifacts": [],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0212",
      "shortname": "my sessions terminated after my machine restarted overnight; ground your…",
      "datetime": "2026-08-28T15:37:49Z",
      "session": "prompt-log",
      "prompt": "my sessions terminated after my machine restarted overnight; ground yourself in the repo, directives, guidance, skills and knowledge; review the session history and audit log; baseline on all tasks done and what tasks are still needed to be done; also clean up any work trees not in use anymore",
      "summary": "prompt logged for reuse",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": null,
      "artifacts": [],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0047",
      "shortname": "decisions-d1-d7-and-spike-d3",
      "datetime": "2026-08-28T16:36:08Z",
      "session": "decisions-d1-d7-2026-08-28",
      "prompt": "D1..D7 answered by the product owner; execute the accepted decisions",
      "summary": "Recorded decisions D1-D7 (cl-0011..cl-0017); promoted ADR-0001..0013 to accepted except 0010; corrected the P2-TERM-05 failure row and resolved the dispatch divergence; refreshed the stale Phase-2 status; built and ran spike D3, which found MSBuildWorkspace executes repository-supplied code across all four vectors; registered DC-019",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0048",
      "shortname": "strategy-1-extraction-decision",
      "datetime": "2026-08-28T17:03:48Z",
      "session": "decisions-d1-d7-2026-08-28",
      "prompt": "spike the sandbox and the non-MSBuild extraction then lets evaluate options; go with Strategy 1; commit and push all",
      "summary": "Adopted Strategy 1 for Component 1: extraction reads the project file as data and compiles with Roslyn directly, never using MSBuildWorkspace, disclosing unresolved references. Backed by two spikes - D3 (MSBuildWorkspace executes repository code, 4/4 vectors) and the containment comparison (job object alone contains nothing; low integrity + job blocks all four; no-MSBuild recovers 159/159 types 6.2x faster).",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0049",
      "shortname": "next-five-fidelity-focus-dispatch-perf",
      "datetime": "2026-08-28T17:43:08Z",
      "session": "decisions-d1-d7-2026-08-28",
      "prompt": "yes do these next in order continue until you have done all 5 and then bring back the summary and next steps",
      "summary": "Completed all five next steps: Option B fidelity spike (100% edge resolution, 0 type loss on four shapes; its own first run's 82-89% was two harness defects); Component 1 contract rewritten with scope grain per (project, TFM); P2-FOCUS-01/02/04 built with -03 recorded as owed; prompt dispatch across the boundary, which found DC-020; P2-PERF-03 measured. 505 tests.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0050",
      "shortname": "extractor-canvas-dispatch-ui-perf",
      "datetime": "2026-08-28T18:26:37Z",
      "session": "decisions-d1-d7-2026-08-28",
      "prompt": "do all of these 5 steps then summarize what i can manually test in the client and show the standard status and next steps",
      "summary": "Completed all five: CSharpExtractor with disclosure facts; the WebView2 canvas with P2-FOCUS-03 running out of process after two measured dead ends; prompt dispatch wired into the shell UI; DC-020's control widened to every operation; P2-PERF-01 measured at 723ms p95 against a 10s budget. 526 tests.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0051",
      "shortname": "phase-2-completion",
      "datetime": "2026-08-28T19:33:14Z",
      "session": "decisions-d1-d7-2026-08-28",
      "prompt": "do all of these 5 steps then summarize what i can manually test in the client and show the standard status and next steps",
      "summary": "Wired the extractor to the shell, rendered real graph data in the canvas, tested P2-EXT-02 broken-load quarantine, drove the snapshot swap from the real drag, and wrote the Phase-2 exit review. Found and fixed a defect where an unparseable project vanished from discovery. 541 tests.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0052",
      "shortname": "external-corpus-canvas-nav-phase-3",
      "datetime": "2026-08-28T20:08:27Z",
      "session": "decisions-d1-d7-2026-08-28",
      "prompt": "do all of these 5 steps then summarize what i can manually test in the client; use my TheTerrace repo for the target repo to index",
      "summary": "Extended fidelity to TheTerrace and closed two gaps; indexed it through the product path (11,041 assertions, 4/4 scopes) which exposed tuple types leaking in as graph nodes; added canvas navigation with click-to-re-root and a radial layout; exposed daemon/health/MCP diagnostics; opened Phase 3. 547 tests.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0053",
      "shortname": "phase-3-spikes-and-adr-0010",
      "datetime": "2026-08-28T20:39:23Z",
      "session": "decisions-d1-d7-2026-08-28",
      "prompt": "do all of these 5 steps then summarize what i can manually test in the client",
      "summary": "Ran both Phase-3 spikes against TheTerrace (Bicep 24/24, EF 62/62 tables), proposed ADR-0016 for bounded-context declaration, added workspace.open so the daemon path is reachable without an environment variable, and promoted ADR-0010 after proving dispatch reaches and is executed by a live session. 548 tests.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0054",
      "shortname": "phase-3-components-and-agent-dispatch",
      "datetime": "2026-08-28T21:16:30Z",
      "session": "decisions-d1-d7-2026-08-28",
      "prompt": "do all of these 5 steps then summarize what i can manually test in the client",
      "summary": "Accepted ADR-0016; built the Bicep and EF schema extractors and the join projection; TheTerrace indexes 7 scopes / 12,034 assertions with five disclosures; measured agent dispatch and found the receipt correct while agents lack a readiness signal. 559 tests.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0055",
      "shortname": "readiness-contexts-joins-bicep-shapes",
      "datetime": "2026-08-28T22:15:43Z",
      "session": "decisions-d1-d7-2026-08-28",
      "prompt": "do all of these 5 steps then summarize what i can manually test in the client",
      "summary": "Dispatch now refuses on unestablished readiness; bounded contexts load, validate and report coverage; the canvas draws joins by confidence; Bicep handles loops, conditionals and existing references with a count disclosure; TheTerrace has an authored context map at 68% coverage. 567 tests.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0056",
      "shortname": "terminal-env-menu-contexts-readiness",
      "datetime": "2026-08-28T22:31:22Z",
      "session": "decisions-d1-d7-2026-08-28",
      "prompt": "terminal has no local env variables; no way to point at a local repo - need a file menu; AND do the next 5 things",
      "summary": "Fixed two reported defects (terminal profile/PATH, no menu) and completed five steps: contexts drawn and coloured, positive agent readiness, Verified [Table] joins, TheTerrace map validated at 68%, Bicep modules exercised. 588 tests.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0057",
      "shortname": "context-pane-agent-readiness-fluent-menu",
      "datetime": "2026-08-28T22:43:41Z",
      "session": "decisions-d1-d7-2026-08-28",
      "prompt": "do all of these 5 steps then summarize what i can manually test in the client",
      "summary": "Context map pane with crossing counts; readiness watcher wired into terminals so an agent can be dispatched to; Fluent ToTable read as a declaration; TheTerrace map committed on a review branch; Edit/Window menus and recent workspaces. Layout surface fixtures now DERIVE from Layout.Default so adding a surface stops breaking unrelated tests. 593 tests.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0058",
      "shortname": "client-defects-and-phase3-steps",
      "datetime": "2026-08-28T23:10:09Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "a few things:\n1: the menu wording is illegible because of the colors\n2: what are the icons on the vertical left side for they seem non functional\n3: default layout: console at the bottom, left column: workspace, right column: graph, domain\n4: right now if i move the graph tab to the right pane it reorders with the graph and whatever was on the right to the left ... moves need to match where i move them and not reorder in a different way\n--------------------\nwork through these and then do the next 5 items from above then again end with the summary and next steps tables",
      "summary": "Four reported client defects fixed, then the next five Phase 3 steps.\n\n**Defects.** Menu dropdowns were light-on-light because a MenuItem popup keeps the system light\ntemplate regardless of the Menu's own colours; styled MenuItem/ContextMenu/Separator instead of\nrecolouring the Menu again. The vertical icon strips were AvalonDock auto-hide anchor sides,\nvisible even though nothing in this build auto-hides â€” collapsed. Default layout is now console\nbottom / workspace left / graph+domain right. Drop placement was the real bug: MoveSurface always\nwrapped the target in a new split, producing [Left, [Right, New]] and giving the nested split\nfresh 50/50 weights that moved panes the user never touched; it now inserts a sibling at the\nrequested index when the parent split already runs in that orientation, taking half of the target\npane's own weight.\n\n**Steps.** Contexts surface with crossing counts; the agent-dispatch spike re-run now exits 8\n(REFUSED) instead of reporting an accepted write into a trust dialog â€” the readiness contract\nproven by the case that produced it; clicking a context filters the canvas and states how many\nneighbours it hid; Ctrl+K, A opens an agent terminal (agents filtered to those on PATH); Bicep\ndependsOn edges bounded to each declaration's span.\n\n**A control fired.** Adding AddSurface to the layout operation union failed the SC 2.5.7\nconformance test for having no declared keyboard equivalent, and the announcement conformance test\nfor having no announcement. Both were real gaps in the new operation, found by reflection over the\nunion rather than by review.\n\nGate: 596 tests green (App 106, Core 490), defect register, audit log and docs graph all clean,\napp verified launching.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "defect",
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Fix the four reported client defects, then complete the next five Phase 3 steps, and close with the manual-test, status and next-step tables",
      "done_when": "All four defects fixed with a test each; five steps landed; full gate green; committed and pushed; tables produced",
      "change": "cl-0045",
      "git": {
        "sha": "bdfd15cc0b7c30bc3f156e2ba877ce0b5afeae75",
        "short": "bdfd15cc0",
        "branch": "main",
        "pushed": true
      }
    },
    {
      "id": "al-0213",
      "shortname": "client-defects-and-phase3-steps",
      "datetime": "2026-08-28T23:10:09Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "a few things:\n1: the menu wording is illegible because of the colors\n2: what are the icons on the vertical left side for they seem non functional\n3: default layout: console at the bottom, left column: workspace, right column: graph, domain\n4: right now if i move the graph tab to the right pane it reorders with the graph and whatever was on the right to the left ... moves need to match where i move them and not reorder in a different way\n--------------------\nwork through these and then do the next 5 items from above then again end with the summary and next steps tables",
      "summary": "Four reported client defects fixed, then the next five Phase 3 steps.\n\n**Defects.** Menu dropdowns were light-on-light because a MenuItem popup keeps the system light\ntemplate regardless of the Menu's own colours; styled MenuItem/ContextMenu/Separator instead of\nrecolouring the Menu again. The vertical icon strips were AvalonDock auto-hide anchor sides,\nvisible even though nothing in this build auto-hides — collapsed. Default layout is now console\nbottom / workspace left / graph+domain right. Drop placement was the real bug: MoveSurface always\nwrapped the target in a new split, producing [Left, [Right, New]] and giving the nested split\nfresh 50/50 weights that moved panes the user never touched; it now inserts a sibling at the\nrequested index when the parent split already runs in that orientation, taking half of the target\npane's own weight.\n\n**Steps.** Contexts surface with crossing counts; the agent-dispatch spike re-run now exits 8\n(REFUSED) instead of reporting an accepted write into a trust dialog — the readiness contract\nproven by the case that produced it; clicking a context filters the canvas and states how many\nneighbours it hid; Ctrl+K, A opens an agent terminal (agents filtered to those on PATH); Bicep\ndependsOn edges bounded to each declaration's span.\n\n**A control fired.** Adding AddSurface to the layout operation union failed the SC 2.5.7\nconformance test for having no declared keyboard equivalent, and the announcement conformance test\nfor having no announcement. Both were real gaps in the new operation, found by reflection over the\nunion rather than by review.\n\nGate: 596 tests green (App 106, Core 490), defect register, audit log and docs graph all clean,\napp verified launching.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "defect",
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Fix the four reported client defects, then complete the next five Phase 3 steps, and close with the manual-test, status and next-step tables",
      "done_when": "All four defects fixed with a test each; five steps landed; full gate green; committed and pushed; tables produced",
      "change": "cl-0045",
      "git": {
        "sha": "bdfd15cc0b7c30bc3f156e2ba877ce0b5afeae75",
        "short": "bdfd15cc0",
        "branch": "main",
        "pushed": true
      }
    },
    {
      "id": "al-0059",
      "shortname": "phase3-surfacing-five-steps",
      "datetime": "2026-08-28T23:47:09Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do all of these 5 steps then summarize what i can manually test in the client\nand show the standard status and next steps",
      "summary": "Five steps, two of which I had proposed on a wrong premise and corrected by opening the code before\nbuilding.\n\n**Layout persistence already existed and was wired.** The proposal to add it was wrong; the real\ndefect was that a runtime-created agent terminal was dropped on every restart, because availability\nwas decided from a whitelist of ids snapshotted from the default layout. Availability is now decided\nby KIND â€” can content be built for this â€” and a kind the build no longer has is still dropped and\nstill reported, so the control did not get quietly widened into a no-op.\n\n**JoinProjection already existed and had no production caller.** Written, tested, never called by the\nrunning app. It is now a surface, splitting Verified from Inferred rather than ranking them together,\nwith the basis on every row and the disclosures stated.\n\nReadiness markers are configurable per agent (agent-readiness.json), fail closed on an uncompilable\npattern, and the watcher exposes the tail it judged so tuning is measurement. Crossings carry their\nmember edges, capped, with the true weight and the undisclosed count kept separate. Uncovered symbols\nare ranked by namespace so a percentage becomes a task.\n\n**A defect class was registered.** Adding the joins surface turned LayoutUpgradeTests red with a\nmigration error for a change unrelated to migration: the fixture held its own copy of \"the surfaces\nthis release ships\". Third occurrence, first registration â€” DC-021.\n\nGate: 612 tests green (App 108, Core 504), defect register, audit log and docs graph clean, app\nverified launching.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Land the five proposed next actions, verify each against the code before building, and close with the manual-test, status and next-step tables",
      "done_when": "Five steps landed with a test each; the full gate green; change-log, audit and defect register updated; committed and pushed; tables produced",
      "change": "cl-0047",
      "git": {
        "sha": "971a687bb86a177cc464a087645ec42a0737ac39",
        "short": "971a687bb",
        "branch": "main",
        "pushed": true
      }
    },
    {
      "id": "al-0214",
      "shortname": "phase3-surfacing-five-steps",
      "datetime": "2026-08-28T23:47:09Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do all of these 5 steps then summarize what i can manually test in the client\nand show the standard status and next steps",
      "summary": "Five steps, two of which I had proposed on a wrong premise and corrected by opening the code before\nbuilding.\n\n**Layout persistence already existed and was wired.** The proposal to add it was wrong; the real\ndefect was that a runtime-created agent terminal was dropped on every restart, because availability\nwas decided from a whitelist of ids snapshotted from the default layout. Availability is now decided\nby KIND — can content be built for this — and a kind the build no longer has is still dropped and\nstill reported, so the control did not get quietly widened into a no-op.\n\n**JoinProjection already existed and had no production caller.** Written, tested, never called by the\nrunning app. It is now a surface, splitting Verified from Inferred rather than ranking them together,\nwith the basis on every row and the disclosures stated.\n\nReadiness markers are configurable per agent (agent-readiness.json), fail closed on an uncompilable\npattern, and the watcher exposes the tail it judged so tuning is measurement. Crossings carry their\nmember edges, capped, with the true weight and the undisclosed count kept separate. Uncovered symbols\nare ranked by namespace so a percentage becomes a task.\n\n**A defect class was registered.** Adding the joins surface turned LayoutUpgradeTests red with a\nmigration error for a change unrelated to migration: the fixture held its own copy of \"the surfaces\nthis release ships\". Third occurrence, first registration — DC-021.\n\nGate: 612 tests green (App 108, Core 504), defect register, audit log and docs graph clean, app\nverified launching.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Land the five proposed next actions, verify each against the code before building, and close with the manual-test, status and next-step tables",
      "done_when": "Five steps landed with a test each; the full gate green; change-log, audit and defect register updated; committed and pushed; tables produced",
      "change": "cl-0047",
      "git": {
        "sha": "971a687bb86a177cc464a087645ec42a0737ac39",
        "short": "971a687bb",
        "branch": "main",
        "pushed": true
      }
    },
    {
      "id": "al-0060",
      "shortname": "measured-readiness-and-four-steps",
      "datetime": "2026-08-29T00:05:47Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do all of these 5 steps then summarize what i can manually test in the client\nand show the standard status and next steps",
      "summary": "Five steps. The first one measured, and the measurement withdrew my own proposal.\n\n**Readiness.** I had written that one measurement would turn agent dispatch from always-refused into\nworking. It does not. A new observe-agent instrument captured what Claude Code actually draws:\nthe trust gate appears even in this repository's own directory; the chevron in the output is the\nselection cursor of that dialog sitting on \"No, exit\"; and the screen is a TUI drawn with absolute\ncursor addressing, so a tail-anchored regex over the byte stream asks where the cursor went last\nrather than what the screen says. A looser marker â€” the obvious repair â€” would report READY at the\nmost dangerous possible moment. The built-in marker is left exactly as it is, the captured bytes are\na committed fixture, and the negative control was observed failing on a loosened pattern before being\naccepted. Screen-buffer readiness is recorded as the next real step.\n\n**The other four landed.** Join rows centre the graph on their From end, clearing any context filter\nfirst. The migration chain's placeholder â€” a worked example describing a rename the product never\nperformed â€” was replaced by the real v1â†’v2 step, so the joins pane reaches users who already have a\nsaved layout; the example moved into the test that documents it. depends_on is consumed as a Verified\njoin. And DC-021 has an automated control: verify-fixture-derivation.py, which found two live cases\nthe hour it was written, one of them a day old and written by the same hand that registered the class.\n\nGate: 616 tests green (App 108, Core 508), five verifiers clean, app verified launching.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Land the five proposed next actions, measuring rather than reasoning where the step calls for it, and close with the manual-test, status and next-step tables",
      "done_when": "Five steps landed with a control each; readiness measured against real agent output; full gate green; committed and pushed; tables produced",
      "change": "cl-0049",
      "git": {
        "sha": "97aad79e5061819896356312a83a957cc4152280",
        "short": "97aad79e5",
        "branch": "main",
        "pushed": true
      }
    },
    {
      "id": "al-0215",
      "shortname": "measured-readiness-and-four-steps",
      "datetime": "2026-08-29T00:05:47Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do all of these 5 steps then summarize what i can manually test in the client\nand show the standard status and next steps",
      "summary": "Five steps. The first one measured, and the measurement withdrew my own proposal.\n\n**Readiness.** I had written that one measurement would turn agent dispatch from always-refused into\nworking. It does not. A new observe-agent instrument captured what Claude Code actually draws:\nthe trust gate appears even in this repository's own directory; the chevron in the output is the\nselection cursor of that dialog sitting on \"No, exit\"; and the screen is a TUI drawn with absolute\ncursor addressing, so a tail-anchored regex over the byte stream asks where the cursor went last\nrather than what the screen says. A looser marker — the obvious repair — would report READY at the\nmost dangerous possible moment. The built-in marker is left exactly as it is, the captured bytes are\na committed fixture, and the negative control was observed failing on a loosened pattern before being\naccepted. Screen-buffer readiness is recorded as the next real step.\n\n**The other four landed.** Join rows centre the graph on their From end, clearing any context filter\nfirst. The migration chain's placeholder — a worked example describing a rename the product never\nperformed — was replaced by the real v1→v2 step, so the joins pane reaches users who already have a\nsaved layout; the example moved into the test that documents it. depends_on is consumed as a Verified\njoin. And DC-021 has an automated control: verify-fixture-derivation.py, which found two live cases\nthe hour it was written, one of them a day old and written by the same hand that registered the class.\n\nGate: 616 tests green (App 108, Core 508), five verifiers clean, app verified launching.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Land the five proposed next actions, measuring rather than reasoning where the step calls for it, and close with the manual-test, status and next-step tables",
      "done_when": "Five steps landed with a control each; readiness measured against real agent output; full gate green; committed and pushed; tables produced",
      "change": "cl-0049",
      "git": {
        "sha": "97aad79e5061819896356312a83a957cc4152280",
        "short": "97aad79e5",
        "branch": "main",
        "pushed": true
      }
    },
    {
      "id": "al-0061",
      "shortname": "screen-model-and-joins-on-a-real-repo",
      "datetime": "2026-08-29T14:50:51Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do all of these 5 steps then summarize what i can manually test in the client\nand show the standard status and next steps",
      "summary": "Five steps. The third one found a defect in the second-to-last turn's work.\n\n**A screen model.** ScreenBuffer is a small VT interpreter â€” cursor movement, erasure, text, nothing\nelse. Readiness now matches the rendered screen anchored to the last drawn line, because the earlier\nmeasurement showed an agent's last bytes are wherever the cursor went rather than what the user sees.\nThe built-in markers are anchored at both ends and remain explicitly unverified against a ready agent.\n\n**The trust gate is a state.** NeedsAttention, searched across the whole screen, outranking readiness,\nannounced once per transition rather than per repaint. Before this the shell refused and said nothing,\nwhich is indistinguishable from a broken pane (DC-011).\n\n**The joins, on a real repository â€” and DC-022.** The first run over TheTerrace reported 7,426\nverified joins, each carrying \"declared in the resource's dependsOn\", in a repository containing no\nBicep and no dependsOn at all. depends_on is the C# extractor's predicate for type dependencies; the\njoin qualified on the PREDICATE rather than the kind of thing it was on, and its basis was a fixed\nstring that could never disagree with the evidence. It failed in the flattering direction â€” the\nlargest Verified count the pane had ever shown. Fixed, pinned by two tests, registered. After the fix:\n0 verified, 59 inferred, which is the correct answer for that repository.\n\nThe contexts pane in the same run is usable, and names Operations as the boundary worth examining.\n360 of 474 uncovered symbols are tests â€” the namespace grouping turned a number that reads as a gap\ninto a line that reads as correct.\n\n**Two controls widened.** The fixture-derivation gate now watches command ids (29 identifiers,\nobserved firing on a planted literal), and the migration chain has both an end-to-end test and one\nasserting the steps join up â€” which fails the moment a version is bumped without a migration.\n\nGate: 627 tests green (App 108, Core 519), five verifiers clean, app verified launching.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Land the five proposed next actions, running the projections over a real repository rather than reasoning about them, and close with the manual-test, status and next-step tables",
      "done_when": "Five steps landed with a control each; the joins measured on TheTerrace; full gate green; committed and pushed; tables produced",
      "change": "cl-0051",
      "git": {
        "sha": "27ce744843701cf1416eb4140acddad2c4a0b2f9",
        "short": "27ce74484",
        "branch": "main",
        "pushed": true
      }
    },
    {
      "id": "al-0216",
      "shortname": "screen-model-and-joins-on-a-real-repo",
      "datetime": "2026-08-29T14:50:51Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do all of these 5 steps then summarize what i can manually test in the client\nand show the standard status and next steps",
      "summary": "Five steps. The third one found a defect in the second-to-last turn's work.\n\n**A screen model.** ScreenBuffer is a small VT interpreter — cursor movement, erasure, text, nothing\nelse. Readiness now matches the rendered screen anchored to the last drawn line, because the earlier\nmeasurement showed an agent's last bytes are wherever the cursor went rather than what the user sees.\nThe built-in markers are anchored at both ends and remain explicitly unverified against a ready agent.\n\n**The trust gate is a state.** NeedsAttention, searched across the whole screen, outranking readiness,\nannounced once per transition rather than per repaint. Before this the shell refused and said nothing,\nwhich is indistinguishable from a broken pane (DC-011).\n\n**The joins, on a real repository — and DC-022.** The first run over TheTerrace reported 7,426\nverified joins, each carrying \"declared in the resource's dependsOn\", in a repository containing no\nBicep and no dependsOn at all. depends_on is the C# extractor's predicate for type dependencies; the\njoin qualified on the PREDICATE rather than the kind of thing it was on, and its basis was a fixed\nstring that could never disagree with the evidence. It failed in the flattering direction — the\nlargest Verified count the pane had ever shown. Fixed, pinned by two tests, registered. After the fix:\n0 verified, 59 inferred, which is the correct answer for that repository.\n\nThe contexts pane in the same run is usable, and names Operations as the boundary worth examining.\n360 of 474 uncovered symbols are tests — the namespace grouping turned a number that reads as a gap\ninto a line that reads as correct.\n\n**Two controls widened.** The fixture-derivation gate now watches command ids (29 identifiers,\nobserved firing on a planted literal), and the migration chain has both an end-to-end test and one\nasserting the steps join up — which fails the moment a version is bumped without a migration.\n\nGate: 627 tests green (App 108, Core 519), five verifiers clean, app verified launching.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Land the five proposed next actions, running the projections over a real repository rather than reasoning about them, and close with the manual-test, status and next-step tables",
      "done_when": "Five steps landed with a control each; the joins measured on TheTerrace; full gate green; committed and pushed; tables produced",
      "change": "cl-0051",
      "git": {
        "sha": "27ce744843701cf1416eb4140acddad2c4a0b2f9",
        "short": "27ce74484",
        "branch": "main",
        "pushed": true
      }
    },
    {
      "id": "al-0062",
      "shortname": "clean-rebuild-and-four-defects",
      "datetime": "2026-08-29T15:28:56Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "commit and push all, merge and make sure main is clean\ndo a full clean and rebuild so i can test the state of the app while you do work\nthen continue with the next 5 steps and then provide the standard status and next steps tables at the end of your turn",
      "summary": "Main was already clean and both remote branches were already fully merged â€” nothing to merge, and I\nsaid so rather than performing a no-op. A full clean and Release rebuild was published to\nartifacts/app for the user to test while work continued, deliberately outside the Debug path so\nongoing builds could not disturb it.\n\nThe clean rebuild immediately earned its keep. P2FOCUS03 failed with \"the canvas probe was not\nbuilt\": AiDe.App.CanvasProbe was never a ProjectReference of the test project, so it had been built\nonce by a full-solution build and every run since had exercised that stale executable. Rebuilt from\ncurrent source it failed for real â€” the canvas page carried a JavaScript syntax error that broke the\nwhole script, and THE GRAPH PANE WAS RENDERING NOTHING AT ALL. Registered as DC-023: a gate that\nkeeps passing because it runs a stale build of the thing it tests.\n\nRunning the panes over TheTerrace found two more. hosted_on matched the whole Microsoft.Sql/* family\nand produced a Cartesian product â€” 64 tables x 3 resources = 192 edges, each claiming to be the only\nliterally-named SQL resource, of which there were three. And the context-coverage denominator counted\nBicep parameters, so the map was blamed for artifacts it was never about.\n\nA defect in the spike itself produced a confidently wrong write-up before that: extractors passed\npositionally put BicepExtractor in the fallback slot, so every bicep scope was routed to the schema\nextractor and failed, and the write-up concluded the repository had no Bicep. It has two templates\nand 24 resources. Fixed with named arguments and corrected in the record.\n\nThe readiness watcher now uses the same TerminalScreen and VtParser the pane renders with; the\nScreenBuffer written last turn was a duplicate of a screen model this repository already had, and was\ndeleted.\n\nFinding worth acting on: 57 of the 72 Football-to-Operations crossings on TheTerrace are one class,\nAppDbContext. Operations is not a boundary that failed; the map counts shared persistence as domain\ncoupling.\n\nGate: 628 tests green (App 108, Core 520), five verifiers clean, Release build verified launching.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Confirm main is clean and merged, publish a stable Release build for the user to test, then land the next five steps and close with the standard tables",
      "done_when": "Main verified clean and fully merged; a Release build published and verified launching; five steps landed with a control each; full gate green; committed and pushed; tables produced",
      "change": "cl-0053",
      "git": {
        "sha": "27adc7d93c323fd278bf0edc91f6fbf7da7c139e",
        "short": "27adc7d93",
        "branch": "main",
        "pushed": true
      }
    },
    {
      "id": "al-0217",
      "shortname": "clean-rebuild-and-four-defects",
      "datetime": "2026-08-29T15:28:56Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "commit and push all, merge and make sure main is clean\ndo a full clean and rebuild so i can test the state of the app while you do work\nthen continue with the next 5 steps and then provide the standard status and next steps tables at the end of your turn",
      "summary": "Main was already clean and both remote branches were already fully merged — nothing to merge, and I\nsaid so rather than performing a no-op. A full clean and Release rebuild was published to\nartifacts/app for the user to test while work continued, deliberately outside the Debug path so\nongoing builds could not disturb it.\n\nThe clean rebuild immediately earned its keep. P2FOCUS03 failed with \"the canvas probe was not\nbuilt\": AiDe.App.CanvasProbe was never a ProjectReference of the test project, so it had been built\nonce by a full-solution build and every run since had exercised that stale executable. Rebuilt from\ncurrent source it failed for real — the canvas page carried a JavaScript syntax error that broke the\nwhole script, and THE GRAPH PANE WAS RENDERING NOTHING AT ALL. Registered as DC-023: a gate that\nkeeps passing because it runs a stale build of the thing it tests.\n\nRunning the panes over TheTerrace found two more. hosted_on matched the whole Microsoft.Sql/* family\nand produced a Cartesian product — 64 tables x 3 resources = 192 edges, each claiming to be the only\nliterally-named SQL resource, of which there were three. And the context-coverage denominator counted\nBicep parameters, so the map was blamed for artifacts it was never about.\n\nA defect in the spike itself produced a confidently wrong write-up before that: extractors passed\npositionally put BicepExtractor in the fallback slot, so every bicep scope was routed to the schema\nextractor and failed, and the write-up concluded the repository had no Bicep. It has two templates\nand 24 resources. Fixed with named arguments and corrected in the record.\n\nThe readiness watcher now uses the same TerminalScreen and VtParser the pane renders with; the\nScreenBuffer written last turn was a duplicate of a screen model this repository already had, and was\ndeleted.\n\nFinding worth acting on: 57 of the 72 Football-to-Operations crossings on TheTerrace are one class,\nAppDbContext. Operations is not a boundary that failed; the map counts shared persistence as domain\ncoupling.\n\nGate: 628 tests green (App 108, Core 520), five verifiers clean, Release build verified launching.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Confirm main is clean and merged, publish a stable Release build for the user to test, then land the next five steps and close with the standard tables",
      "done_when": "Main verified clean and fully merged; a Release build published and verified launching; five steps landed with a control each; full gate green; committed and pushed; tables produced",
      "change": "cl-0053",
      "git": {
        "sha": "27adc7d93c323fd278bf0edc91f6fbf7da7c139e",
        "short": "27adc7d93",
        "branch": "main",
        "pushed": true
      }
    },
    {
      "id": "al-0063",
      "shortname": "collectknowledge-wpf-ui-and-dashboards",
      "datetime": "2026-08-29T15:32:49Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "Build a WPF client with modern/softer styling (rounded corners, drop shadows): acquire best practices for WPF window/tab styling; best permissive-license open-source WPF UI control/styling libraries; exemplars for modern native UX, IDE and video-editing interfaces; plus diagramming, UML/generative-from-UML, ERM/ORM visualization, and visualizing test results, CI/CD execution and operational logs/metrics.",
      "summary": "Two new sourced KBs: wpf-modern-ui-styling (DWM rounded corners/Mica, WindowChrome, .NET Fluent theme, MIT control libs WPF-UI/MahApps/HandyControl/MaterialDesign, soft-shadow perf, JetBrains New-UI/Islands + IDE/editor UX exemplars) and operational-and-test-dashboards (Allure/ReportPortal, CI-as-DAG, RED/USE, MIT charting ScottPlot/LiveCharts2/OxyPlot). Diagram/UML/ERM asks cross-referenced to existing bases, not duplicated.",
      "kind": "skill",
      "skill": "collectknowledge",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "docs/knowledge/wpf-modern-ui-styling/index.md",
        "docs/knowledge/operational-and-test-dashboards/index.md"
      ],
      "tags": [
        "wpf",
        "styling",
        "dashboards"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0064",
      "shortname": "embedded-script-gate-and-measured-map",
      "datetime": "2026-08-29T15:40:01Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "commit and push all, merge and make sure main is clean\ndo a full clean and rebuild so i can test the state of the app while you do work\nthen continue with the next 5 steps and then provide the standard status and next steps tables at the end of your turn",
      "summary": "Housekeeping found uncommitted work from ANOTHER session in this same checkout â€” two knowledge bases\non the client's visual layer. Read before committing rather than swept in, committed because \"all\"\nmeans all, and flagged: two sessions in one checkout share an index, a HEAD and one set of generated\nartifacts, which is the arrangement the worktree discipline exists to avoid. Both remote branches\nwere already fully merged; nothing to merge, and saying so beats a no-op.\n\nFive steps.\n\nAn embedded page now gets a parser. verify-embedded-scripts.py checks 13 inline script blocks with\nnode --check, naming its mode and failing closed. Its first finding was its own false positive â€” a\n<script src> in an HTML comment â€” fixed rather than tuned around, then verified against the real\ndefect. That covers both the \"add a check\" and \"audit the other pages\" steps: twelve of the thirteen\nblocks are in docs templates that would fail exactly as silently.\n\nDC-022's residual is closed for the consumer that had it. has_type is emitted by all three extractors\nand its object values partitioned by producer only by accident; every read now qualifies on the shape\nof the subject too, with a test for the real case still working as well as for the two bad ones.\n\nThe dangling docs link resolved itself when the other session's work landed. I did not fix it and am\nnot claiming to.\n\nAnd TheTerrace's map recommendation was MEASURED rather than asserted: a proposed Platform context\ntakes Operations from 172 crossings to 47 and Football-to-Operations from 72 to 15. Nothing was\napplied to that repository.\n\nGate: 631 tests green (App 108, Core 523), six verifiers clean, Release build published and verified.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Commit and push everything including another session's work, confirm main clean and merged, publish a testable Release build, land the next five steps, and close with the standard tables",
      "done_when": "Main clean, merged and pushed; a Release build published and verified launching; five steps landed with a control each; full gate green; tables produced",
      "change": "cl-0056",
      "git": {
        "sha": "68598f3479f1ce2028f54b9007d7aec40d085847",
        "short": "68598f347",
        "branch": "main",
        "pushed": true
      }
    },
    {
      "id": "al-0218",
      "shortname": "embedded-script-gate-and-measured-map",
      "datetime": "2026-08-29T15:40:01Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "commit and push all, merge and make sure main is clean\ndo a full clean and rebuild so i can test the state of the app while you do work\nthen continue with the next 5 steps and then provide the standard status and next steps tables at the end of your turn",
      "summary": "Housekeeping found uncommitted work from ANOTHER session in this same checkout — two knowledge bases\non the client's visual layer. Read before committing rather than swept in, committed because \"all\"\nmeans all, and flagged: two sessions in one checkout share an index, a HEAD and one set of generated\nartifacts, which is the arrangement the worktree discipline exists to avoid. Both remote branches\nwere already fully merged; nothing to merge, and saying so beats a no-op.\n\nFive steps.\n\nAn embedded page now gets a parser. verify-embedded-scripts.py checks 13 inline script blocks with\nnode --check, naming its mode and failing closed. Its first finding was its own false positive — a\n<script src> in an HTML comment — fixed rather than tuned around, then verified against the real\ndefect. That covers both the \"add a check\" and \"audit the other pages\" steps: twelve of the thirteen\nblocks are in docs templates that would fail exactly as silently.\n\nDC-022's residual is closed for the consumer that had it. has_type is emitted by all three extractors\nand its object values partitioned by producer only by accident; every read now qualifies on the shape\nof the subject too, with a test for the real case still working as well as for the two bad ones.\n\nThe dangling docs link resolved itself when the other session's work landed. I did not fix it and am\nnot claiming to.\n\nAnd TheTerrace's map recommendation was MEASURED rather than asserted: a proposed Platform context\ntakes Operations from 172 crossings to 47 and Football-to-Operations from 72 to 15. Nothing was\napplied to that repository.\n\nGate: 631 tests green (App 108, Core 523), six verifiers clean, Release build published and verified.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Commit and push everything including another session's work, confirm main clean and merged, publish a testable Release build, land the next five steps, and close with the standard tables",
      "done_when": "Main clean, merged and pushed; a Release build published and verified launching; five steps landed with a control each; full gate green; tables produced",
      "change": "cl-0056",
      "git": {
        "sha": "68598f3479f1ce2028f54b9007d7aec40d085847",
        "short": "68598f347",
        "branch": "main",
        "pushed": true
      }
    },
    {
      "id": "al-0065",
      "shortname": "collectknowledge-graph-experience-and-rendering",
      "datetime": "2026-08-29T16:00:15Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "Rich end-to-end experience over the unified code graph + knowledge graph: navigate and introspect any node (walk from a C# file node to read code, then to related metadata/knowledge that informed it). We use Obsidian and Graphify for graph enablement with the LLM; compose them for a great in-editor experience. Accumulate knowledge on: knowledge graphs/GraphRAG/Obsidian/Graphify; connected 2D/3D graph and knowledge-base visualization; building KG visualizations/explorers in WPF (best practices, repos, libraries, controls); design/review surfaces for code-editor viewing leveraging VS Code/Eclipse/JetBrains (public-domain/permissive only); and markdown/HTML visualization/rendering controls for the editor (permissive only).",
      "summary": "Two new sourced KBs: graph-experience-and-visualization (GraphRAG + LazyGraphRAG/LightRAG, 2D/3D force-graph libs Sigma/Cytoscape/3d-force-graph/Reagraph MIT-Apache, node-based UIs React Flow, composing Obsidian+Graphify, WebView2-hosted explorer over native GraphX, node-introspection router as the load-bearing new piece) and editor-and-content-rendering-surfaces (Monaco/AvalonEdit/RoslynPad + Markdig/Markdig.Wpf all MIT/BSD, native-vs-web/airspace decision matrix, only Monaco cleanly reusable from VS Code). Flagged code-knowledge-graphs #8 (GraphRAG 26-85x) for review given LazyGraphRAG ~700x cheaper.",
      "kind": "skill",
      "skill": "collectknowledge",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "docs/knowledge/graph-experience-and-visualization/index.md",
        "docs/knowledge/editor-and-content-rendering-surfaces/index.md"
      ],
      "tags": [
        "knowledge-graph",
        "graphrag",
        "visualization",
        "wpf"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0066",
      "shortname": "worktree-correction-and-second-repository",
      "datetime": "2026-08-29T16:24:40Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "the other session is complete (was supposed to be in a different work tree)\nin fact the repo guidance is all work is done in new work trees so this should have been in a different tree as well so this session did not follow repo guidance\n----\ncommit and push all then make sure main is up to data AND then create a work tree for this session\n----\nthen do the next steps you listed autonomously and provide the standard status and next step tables when you are done",
      "summary": "The correction was fair: WT1 requires a writing session to start in its own worktree, and this one\nworked in the primary checkout for its whole life. It now runs in\nC:/Projects/ai-de-session-phase3-pane-probes on session/phase3-pane-probes, registered.\n\nCleaning up the other session's tree produced the turn's most important finding. The tree was removed\non a verdict of \"clean, merged, unheld\" â€” and a live session recreated it within the minute and wrote\na marker saying it was in use. Nothing was lost, because every cleanliness check was correct. The\nliveness check was not: it read a registration ledger, and that session had never registered.\nDC-024, controlled by a filesystem recency condition observed failing in both directions.\n\nFive steps. The panes are rendered and read rather than merely constructed, with a rule that no\nevidence pane may render nothing. A crossing dominated by one object now names it, so the finding\nthat took a human reading a list is computed. A second repository was measured â€” BioHacker, no EF,\n26 Bicep resources â€” where zero joins is correct and says so, but where the absence of a context map\nwas being rendered as PERFECT COVERAGE: \"Every declared symbol belongs to a context\", from a\nworkspace that has never claimed anything. One repository could not have found that. And TheTerrace's\nPlatform split is applied on a local branch in its own worktree, not pushed.\n\nGate: 641 tests green (App 115, Core 526), six verifiers clean.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Move this session into its own worktree as WT1 requires, then land the five next steps autonomously and close with the standard tables",
      "done_when": "Session running in its own registered worktree; five steps landed with a control each; full gate green; committed and pushed; tables produced",
      "change": "cl-0059",
      "git": {
        "sha": "21068ab3fcf1a7cd9021ac5babfa9d7f95495b6c",
        "short": "21068ab3f",
        "branch": "session/phase3-pane-probes",
        "pushed": null
      }
    },
    {
      "id": "al-0219",
      "shortname": "worktree-correction-and-second-repository",
      "datetime": "2026-08-29T16:24:40Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "the other session is complete (was supposed to be in a different work tree)\nin fact the repo guidance is all work is done in new work trees so this should have been in a different tree as well so this session did not follow repo guidance\n----\ncommit and push all then make sure main is up to data AND then create a work tree for this session\n----\nthen do the next steps you listed autonomously and provide the standard status and next step tables when you are done",
      "summary": "The correction was fair: WT1 requires a writing session to start in its own worktree, and this one\nworked in the primary checkout for its whole life. It now runs in\nC:/Projects/ai-de-session-phase3-pane-probes on session/phase3-pane-probes, registered.\n\nCleaning up the other session's tree produced the turn's most important finding. The tree was removed\non a verdict of \"clean, merged, unheld\" — and a live session recreated it within the minute and wrote\na marker saying it was in use. Nothing was lost, because every cleanliness check was correct. The\nliveness check was not: it read a registration ledger, and that session had never registered.\nDC-024, controlled by a filesystem recency condition observed failing in both directions.\n\nFive steps. The panes are rendered and read rather than merely constructed, with a rule that no\nevidence pane may render nothing. A crossing dominated by one object now names it, so the finding\nthat took a human reading a list is computed. A second repository was measured — BioHacker, no EF,\n26 Bicep resources — where zero joins is correct and says so, but where the absence of a context map\nwas being rendered as PERFECT COVERAGE: \"Every declared symbol belongs to a context\", from a\nworkspace that has never claimed anything. One repository could not have found that. And TheTerrace's\nPlatform split is applied on a local branch in its own worktree, not pushed.\n\nGate: 641 tests green (App 115, Core 526), six verifiers clean.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Move this session into its own worktree as WT1 requires, then land the five next steps autonomously and close with the standard tables",
      "done_when": "Session running in its own registered worktree; five steps landed with a control each; full gate green; committed and pushed; tables produced",
      "change": "cl-0059",
      "git": {
        "sha": "21068ab3fcf1a7cd9021ac5babfa9d7f95495b6c",
        "short": "21068ab3f",
        "branch": "session/phase3-pane-probes",
        "pushed": null
      }
    },
    {
      "id": "al-0067",
      "shortname": "session-contract-and-core-composition",
      "datetime": "2026-08-29T16:35:59Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "there are two sessions working in two different worktrees\nyour sessions need to be collaborative with each other\none session, the claude code session, will work on the core capabilities (the workspace, analyzing the repo contents, implementing the extractors and building the graph for the tool and the overall tool interaction between surfaces), the other session, the GitHub copilot session, will work on the styling and the distinct design surfaces.\nBoth sessions have to agree on contracts (interfaces and abstractions) and on what files they are working on to minimize conflicts and merge complexity.\n-----------\nlook at your work, choose next steps that align to your accountability\ndo your next steps\nthen provide the standard tables of status and next steps at the end of your turn\n----\nthis session is the claude code session working on the core capabilities",
      "summary": "Chose work on the core side of the line, and wrote the line down first.\n\nThe contract (docs/collaboration/session-contracts.md, status: proposed) states the seam in one\nsentence â€” Core produces view models, Design renders them â€” lists file ownership rather than\ndescribing it, names the eight contract types and the two invariants neither session may break\nalone, and gives the three-step protocol for changing any of them. Section 6 lists what it does NOT\nsettle, because writing one session's preference down as settled is how a proposal becomes a fait\naccompli. Three render requests are recorded in it rather than made in conversation.\n\nThen two core defects.\n\nThe daemon composed only the C# extractor and the fixture adapter, so the running application could\nnot see infrastructure or schema â€” while a spike composed all four and reported joins the product had\nno way to show. One composition now, WorkspaceExtractors.Default(), used by both, with named\narguments and a test that reads the routing decision rather than trusting parameter order. Observed\nfailing on the exact mis-ordering that caused the original wrong conclusion: expected \"bicep\", actual\n\"schema\".\n\nAnd every number both panes show was computed from a bounded read â€” 20,000 matches, 4,000 nodes, 60\nneighbours each â€” that never said so. On the repositories measured the caps are slack, and nothing in\nthe output would change if they were not: the counts would just be smaller and still presented as\nfacts. EvidenceRead reports what the read did not see, both causes separately because their fixes\npoint in opposite directions, and a node at exactly the limit counts as truncated.\n\nGate: 647 tests green (App 115, Core 532), six verifiers clean.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "collaboration",
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Write the contract between the two sessions, then do core-side work only: one extractor composition shared by every entry point, and a bounded read that reports what it did not see",
      "done_when": "Contract committed and pushed for the other session to read; core defects fixed with a control each; no Design-owned file edited; full gate green; tables produced",
      "change": "cl-0061",
      "git": {
        "sha": "6db9b6f43e70b8a05994becd344aa86e58e9b37c",
        "short": "6db9b6f43",
        "branch": "session/phase3-pane-probes",
        "pushed": true
      }
    },
    {
      "id": "al-0220",
      "shortname": "session-contract-and-core-composition",
      "datetime": "2026-08-29T16:35:59Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "there are two sessions working in two different worktrees\nyour sessions need to be collaborative with each other\none session, the claude code session, will work on the core capabilities (the workspace, analyzing the repo contents, implementing the extractors and building the graph for the tool and the overall tool interaction between surfaces), the other session, the GitHub copilot session, will work on the styling and the distinct design surfaces.\nBoth sessions have to agree on contracts (interfaces and abstractions) and on what files they are working on to minimize conflicts and merge complexity.\n-----------\nlook at your work, choose next steps that align to your accountability\ndo your next steps\nthen provide the standard tables of status and next steps at the end of your turn\n----\nthis session is the claude code session working on the core capabilities",
      "summary": "Chose work on the core side of the line, and wrote the line down first.\n\nThe contract (docs/collaboration/session-contracts.md, status: proposed) states the seam in one\nsentence — Core produces view models, Design renders them — lists file ownership rather than\ndescribing it, names the eight contract types and the two invariants neither session may break\nalone, and gives the three-step protocol for changing any of them. Section 6 lists what it does NOT\nsettle, because writing one session's preference down as settled is how a proposal becomes a fait\naccompli. Three render requests are recorded in it rather than made in conversation.\n\nThen two core defects.\n\nThe daemon composed only the C# extractor and the fixture adapter, so the running application could\nnot see infrastructure or schema — while a spike composed all four and reported joins the product had\nno way to show. One composition now, WorkspaceExtractors.Default(), used by both, with named\narguments and a test that reads the routing decision rather than trusting parameter order. Observed\nfailing on the exact mis-ordering that caused the original wrong conclusion: expected \"bicep\", actual\n\"schema\".\n\nAnd every number both panes show was computed from a bounded read — 20,000 matches, 4,000 nodes, 60\nneighbours each — that never said so. On the repositories measured the caps are slack, and nothing in\nthe output would change if they were not: the counts would just be smaller and still presented as\nfacts. EvidenceRead reports what the read did not see, both causes separately because their fixes\npoint in opposite directions, and a node at exactly the limit counts as truncated.\n\nGate: 647 tests green (App 115, Core 532), six verifiers clean.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "collaboration",
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Write the contract between the two sessions, then do core-side work only: one extractor composition shared by every entry point, and a bounded read that reports what it did not see",
      "done_when": "Contract committed and pushed for the other session to read; core defects fixed with a control each; no Design-owned file edited; full gate green; tables produced",
      "change": "cl-0061",
      "git": {
        "sha": "6db9b6f43e70b8a05994becd344aa86e58e9b37c",
        "short": "6db9b6f43",
        "branch": "session/phase3-pane-probes",
        "pushed": true
      }
    },
    {
      "id": "al-0068",
      "shortname": "adddomainexperts-graph-wpf-uml",
      "datetime": "2026-08-29T16:46:10Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "adddomainexperts x3: graph-viz/KG-UX, WPF styling, UML/ERM experts",
      "summary": "Added 3 domain experts (kg-visualization-ux-expert, wpf-styling-expert, uml-erm-modelling-expert) + docs/domain-experts.md registry; roster 23+3=26.",
      "kind": "skill",
      "skill": "adddomainexperts",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "docs/domain-experts.md"
      ],
      "tags": [
        "personas"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0069",
      "shortname": "specify-facelift-knowledge-uml",
      "datetime": "2026-08-29T16:46:10Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "specify x3: facelift, knowledge exploration, UML/ERM surfaces",
      "summary": "Three 3-layer specs: app-facelift, knowledge-exploration, uml-erm-surfaces.",
      "kind": "skill",
      "skill": "specify",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "docs/specs/app-facelift.md",
        "docs/specs/knowledge-exploration.md",
        "docs/specs/uml-erm-surfaces.md"
      ],
      "tags": [
        "spec"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0070",
      "shortname": "ui-design-three-surfaces",
      "datetime": "2026-08-29T16:46:10Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "ui-design the facelift, knowledge explorer, and UML/ERM surfaces",
      "summary": "DESIGN.md soft-islands + 3 self-contained mockups with harnesses and hard states.",
      "kind": "skill",
      "skill": "ui-design",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "docs/mockups/app-facelift.html",
        "docs/mockups/knowledge-explorer.html",
        "docs/mockups/uml-erm-surfaces.html"
      ],
      "tags": [
        "ui-design"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0072",
      "shortname": "design-session-contract-accept-4a",
      "datetime": "2026-08-29T16:51:28Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "Register the design session, consult and accept the Core sessions published contract, and render the three Core->Design 4a requests.",
      "summary": "Registered session copilot-design-4d24d94a; claimed design files; accepted session-contracts (added Design response 7.1-7.4, status proposed->accepted, answered 6, accepted 4a); DESIGN.md 4a tokens; new mockup context-map-join.html rendering evidence-shortfall (>= capped), dominant-target emphasis, and IsDeclared==false empty state.",
      "kind": "manual",
      "skill": "ui-design",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "docs/collaboration/session-contracts.md",
        "docs/mockups/context-map-join.html"
      ],
      "tags": [
        "collaboration",
        "ui-design"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0071",
      "shortname": "daemon-proof-caps-incremental-third-repo",
      "datetime": "2026-08-29T17:03:22Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "the other session is working now and has been instructed to accept your contract\ndo the next steps you have listed\nprovide the standard status and next steps tables afterwards",
      "summary": "Four core steps, and they uncovered four more defects than they were aimed at.\n\nProving the daemon's extraction across the pipe failed first against a STALE Release daemon â€”\nLocateDaemon preferred Release if the folder existed rather than following the configuration the\ntests were built in. DC-023 in the harness. With that fixed the daemon does return infrastructure\nevidence, asserted against the provenance extractor id rather than a predicate name.\n\nRaising the caps found the real one: ProjectionService.Find borrowed MaxNeighborsCeiling, so the\nworkbench asked for 20,000 matches and received 50. The context and join panes have been computing\ncrossing counts, join counts and coverage from roughly three percent of a real workspace and\npresenting it as the answer â€” while the spike read the store directly and showed everything. On\nTheTerrace the panes now see 2,164 nodes instead of 50. The shell also invented its own total instead\nof reading ResultBounds.OmittedNodes, and asked for 60 neighbours against a ceiling of 50, so the\ntruncation warning could never fire. It fires on 26 nodes now.\n\nIncremental re-index takes TheTerrace from 4.3s to 0.1s, with reuse counted separately because \"7 of\n7 indexed\" would be a true sentence about a run that read nothing. Testing it across a reopen found\nthat THE SECOND INDEX OF ANY WORKSPACE AFTER A RESTART FAILED â€” the generation counter started at\nzero on every open while the store did not. The daemon opens the store fresh every start; nothing had\never indexed twice across a reopen.\n\nA third repository made the DC-022 collision live rather than hypothetical: it emits depends_on from\nBOTH extractors, 2,310 of them, and the subject qualifier is the only thing keeping 2,304 C# type\ndependencies out of the joins. Six verified edges, each a real dependsOn between two real resources.\n\nGate: 655 tests green (App 115, Core 540), six verifiers clean. Zero design-owned files touched.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Do the four listed core next steps: prove daemon extraction across the pipe, fix the read caps, make re-index incremental, and measure a third repository",
      "done_when": "Each step landed with a control; every defect found on the way registered or fixed; no design-owned file edited; full gate green; tables produced",
      "change": "cl-0064",
      "git": {
        "sha": "62fe22519a048983d643746158f665b8079f8541",
        "short": "62fe22519",
        "branch": "session/phase3-pane-probes",
        "pushed": false
      }
    },
    {
      "id": "al-0221",
      "shortname": "daemon-proof-caps-incremental-third-repo",
      "datetime": "2026-08-29T17:03:22Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "the other session is working now and has been instructed to accept your contract\ndo the next steps you have listed\nprovide the standard status and next steps tables afterwards",
      "summary": "Four core steps, and they uncovered four more defects than they were aimed at.\n\nProving the daemon's extraction across the pipe failed first against a STALE Release daemon —\nLocateDaemon preferred Release if the folder existed rather than following the configuration the\ntests were built in. DC-023 in the harness. With that fixed the daemon does return infrastructure\nevidence, asserted against the provenance extractor id rather than a predicate name.\n\nRaising the caps found the real one: ProjectionService.Find borrowed MaxNeighborsCeiling, so the\nworkbench asked for 20,000 matches and received 50. The context and join panes have been computing\ncrossing counts, join counts and coverage from roughly three percent of a real workspace and\npresenting it as the answer — while the spike read the store directly and showed everything. On\nTheTerrace the panes now see 2,164 nodes instead of 50. The shell also invented its own total instead\nof reading ResultBounds.OmittedNodes, and asked for 60 neighbours against a ceiling of 50, so the\ntruncation warning could never fire. It fires on 26 nodes now.\n\nIncremental re-index takes TheTerrace from 4.3s to 0.1s, with reuse counted separately because \"7 of\n7 indexed\" would be a true sentence about a run that read nothing. Testing it across a reopen found\nthat THE SECOND INDEX OF ANY WORKSPACE AFTER A RESTART FAILED — the generation counter started at\nzero on every open while the store did not. The daemon opens the store fresh every start; nothing had\never indexed twice across a reopen.\n\nA third repository made the DC-022 collision live rather than hypothetical: it emits depends_on from\nBOTH extractors, 2,310 of them, and the subject qualifier is the only thing keeping 2,304 C# type\ndependencies out of the joins. Six verified edges, each a real dependsOn between two real resources.\n\nGate: 655 tests green (App 115, Core 540), six verifiers clean. Zero design-owned files touched.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Do the four listed core next steps: prove daemon extraction across the pipe, fix the read caps, make re-index incremental, and measure a third repository",
      "done_when": "Each step landed with a control; every defect found on the way registered or fixed; no design-owned file edited; full gate green; tables produced",
      "change": "cl-0064",
      "git": {
        "sha": "62fe22519a048983d643746158f665b8079f8541",
        "short": "62fe22519",
        "branch": "session/phase3-pane-probes",
        "pushed": false
      }
    },
    {
      "id": "al-0073",
      "shortname": "implement-facelift-and-4a",
      "datetime": "2026-08-29T17:26:45Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "Execute autonomously on the design next steps: implement the soft-islands facelift theme and render the Core->Design 4a states.",
      "summary": "Implemented in WPF (Design-owned): App.xaml radius/elevation tokens + keyed RoundedButton style; MainWindow.xaml.cs DWM dark title bar + Win11 rounded corners (AllowsTransparency stays false); rounded chrome buttons; ContextMapSurface.cs 4a â€” IsDeclared==false now a first-run empty state (glyph+line+action) and DominantTarget promoted to an accent emphasis chip, ContextBox radius 4->8, IndianRed->DangerBrush token. Build 0/0; App.Tests 115/115 green.",
      "kind": "manual",
      "skill": "implement",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "src/AiDe.App/App.xaml",
        "src/AiDe.App/Workbench/ContextMapSurface.cs"
      ],
      "tags": [
        "implement",
        "facelift"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0222",
      "shortname": "implement-facelift-and-4a",
      "datetime": "2026-08-29T17:26:45Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "Execute autonomously on the design next steps: implement the soft-islands facelift theme and render the Core->Design 4a states.",
      "summary": "Implemented in WPF (Design-owned): App.xaml radius/elevation tokens + keyed RoundedButton style; MainWindow.xaml.cs DWM dark title bar + Win11 rounded corners (AllowsTransparency stays false); rounded chrome buttons; ContextMapSurface.cs 4a — IsDeclared==false now a first-run empty state (glyph+line+action) and DominantTarget promoted to an accent emphasis chip, ContextBox radius 4->8, IndianRed->DangerBrush token. Build 0/0; App.Tests 115/115 green.",
      "kind": "manual",
      "skill": "implement",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "src/AiDe.App/App.xaml",
        "src/AiDe.App/Workbench/ContextMapSurface.cs"
      ],
      "tags": [
        "implement",
        "facelift"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0089",
      "shortname": "id-prevention-scope-shape-force-fourth-repo",
      "datetime": "2026-08-29T17:29:03Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps you have listed\nprovide the standard status and next steps tables afterwards",
      "summary": "Five core steps, and the pattern of the last three turns held: each one found something adjacent.\n\nRe-baselining the measurements taken at 50 nodes turned out to be checkable rather than a re-run, so\nit is checked on every spike run now: 122 of 124 join edges agree between the pane read and the\nstore. The published numbers came from the direct store read and stand; the two edges the panes still\nmiss are cut by the 50-neighbour describe cap, and the shortfall line reports them on the same run.\n\nDC-013 is prevented rather than detected. Every worktree of a repository shares one git common\ndirectory; a counter there with an exclusive-create lock makes allocation atomic across sessions.\nSixteen concurrent allocations from two worktrees returned sixteen distinct ids where the old\nallocator returns one. Promoted to controlled, with the residual stated: two separate clones do not\nshare a git directory.\n\nFingerprinting the workspace shape rather than only each scope found two things. A departed scope is\nnow reconciled out and recorded as a health incident instead of being reused forever while its\nevidence describes code that no longer exists. And a single-file scope is fingerprinted by its own\nfile â€” treating a Bicep template as its directory made two templates in one folder share a basis, so\ndeleting either invalidated both.\n\nForce is reachable: Ctrl+K, Shift+I, threaded additively through the wire so an older client still\nmeans \"use the cache\". Adding a command required one line in a design-owned file because the\nconformance test makes adding and placing a command atomic â€” written into the contract as Core-owned\ndata inside a Design-owned file, with a proposal to move it onto the catalog.\n\nThe fourth repository â€” 63 Python files, no C# â€” produced zero scopes and an EMPTY DISCLOSURE LIST,\nindistinguishable from an empty directory. Third repository running to find the same shape: right\narithmetic, false claim. Unanalysed languages are named with counts now.\n\nGate: 660 tests green (App 115, Core 545), six verifiers clean.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Do the five listed core next steps: re-baseline the pane measurements, prevent DC-013 across agents, fingerprint the workspace shape, make force reachable, and measure a non-.NET repository",
      "done_when": "Each step landed with a control; the seam crossing recorded in the contract; full gate green; committed, merged and published; tables produced",
      "change": "cl-0066",
      "git": {
        "sha": "dd1c53793935746bc3aa6c5a175c1cf5c99f9a8b",
        "short": "dd1c53793",
        "branch": "session/phase3-pane-probes",
        "pushed": true
      }
    },
    {
      "id": "al-0223",
      "shortname": "id-prevention-scope-shape-force-fourth-repo",
      "datetime": "2026-08-29T17:29:03Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps you have listed\nprovide the standard status and next steps tables afterwards",
      "summary": "Five core steps, and the pattern of the last three turns held: each one found something adjacent.\n\nRe-baselining the measurements taken at 50 nodes turned out to be checkable rather than a re-run, so\nit is checked on every spike run now: 122 of 124 join edges agree between the pane read and the\nstore. The published numbers came from the direct store read and stand; the two edges the panes still\nmiss are cut by the 50-neighbour describe cap, and the shortfall line reports them on the same run.\n\nDC-013 is prevented rather than detected. Every worktree of a repository shares one git common\ndirectory; a counter there with an exclusive-create lock makes allocation atomic across sessions.\nSixteen concurrent allocations from two worktrees returned sixteen distinct ids where the old\nallocator returns one. Promoted to controlled, with the residual stated: two separate clones do not\nshare a git directory.\n\nFingerprinting the workspace shape rather than only each scope found two things. A departed scope is\nnow reconciled out and recorded as a health incident instead of being reused forever while its\nevidence describes code that no longer exists. And a single-file scope is fingerprinted by its own\nfile — treating a Bicep template as its directory made two templates in one folder share a basis, so\ndeleting either invalidated both.\n\nForce is reachable: Ctrl+K, Shift+I, threaded additively through the wire so an older client still\nmeans \"use the cache\". Adding a command required one line in a design-owned file because the\nconformance test makes adding and placing a command atomic — written into the contract as Core-owned\ndata inside a Design-owned file, with a proposal to move it onto the catalog.\n\nThe fourth repository — 63 Python files, no C# — produced zero scopes and an EMPTY DISCLOSURE LIST,\nindistinguishable from an empty directory. Third repository running to find the same shape: right\narithmetic, false claim. Unanalysed languages are named with counts now.\n\nGate: 660 tests green (App 115, Core 545), six verifiers clean.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Do the five listed core next steps: re-baseline the pane measurements, prevent DC-013 across agents, fingerprint the workspace shape, make force reachable, and measure a non-.NET repository",
      "done_when": "Each step landed with a control; the seam crossing recorded in the contract; full gate green; committed, merged and published; tables produced",
      "change": "cl-0066",
      "git": {
        "sha": "dd1c53793935746bc3aa6c5a175c1cf5c99f9a8b",
        "short": "dd1c53793",
        "branch": "session/phase3-pane-probes",
        "pushed": true
      }
    },
    {
      "id": "al-0090",
      "shortname": "test-4a-rendering",
      "datetime": "2026-08-29T17:34:09Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "prove 4a rendering with tests",
      "summary": "Two ContextMapSurface 4a render tests (empty-state action; dominant target surfaced); dominant chip denominator Weight->Members.Count. 117/117 green. (Re-emitted after a DC-013 audit merge collision took theirs.)",
      "kind": "manual",
      "skill": "implement",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "tests/AiDe.App.Tests/PaneRenderTests.cs"
      ],
      "tags": [
        "test"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0091",
      "shortname": "facelift-avalondock-dark-theme",
      "datetime": "2026-08-29T17:43:15Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "A screenshot showed the docking panes still white/square. Dark-theme AvalonDock properly and systematically.",
      "summary": "Root cause of the clunky look: AvalonDock rendered its default LIGHT theme (white panes, light square tabs). Fixed by applying Vs2013DarkTheme from the Design-owned MainWindow.xaml.cs (not Core WorkbenchShell); added additive dep Dirkster.AvalonDock.Themes.VS2013 5.0.0 (MIT); recorded in contract 7.5. Build 0/0; App.Tests 117/117.",
      "kind": "manual",
      "skill": "implement",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "src/AiDe.App/MainWindow.xaml.cs",
        "docs/collaboration/session-contracts.md"
      ],
      "tags": [
        "facelift",
        "avalondock"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0092",
      "shortname": "bulk-read-retraction-menu-catalog-broken-build",
      "datetime": "2026-08-29T17:48:53Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps you have listed\nprovide the standard status and next steps tables afterwards",
      "summary": "Five core steps, plus a defect of my own that surfaced first.\n\nThe design session had to re-emit an audit entry \"lost to a DC-013 merge collision\" â€” lost by MY\nresolution. Unioning an append-only file keyed by id, with setdefault, silently drops one side where\nboth used the same id, and the gate stays green because uniqueness is satisfied precisely by the\nremoval. Registered as DC-026 and given a tool that unions by CONTENT so nothing can be dropped. The\ncurrent log is complete: 75 entries, 75 unique ids, and the only gaps are the fifteen my race probe\nburned.\n\nThe bulk evidence read closes the residual from last turn exactly. The panes now see 12,100 of 12,100\nassertions and 124 of 124 join edges â€” zero divergence, where before they rebuilt the set node by\nnode through a 50-neighbour Describe and lost two edges. Paged, with a cursor that is the same tuple\nthe ORDER BY uses, pinned with a page size of one so every boundary is exercised.\n\nA departed scope's evidence is retired by an empty snapshot at a higher generation â€” superseded,\nnever deleted, because destroying the rows would destroy the record of what the graph once said.\n\nEvery catalog command now declares its menu, so Core stops needing to edit a design-owned file to add\none; the switch is a one-line change recorded as a request.\n\nAnd the fifth repository â€” a real one with a deliberate syntax error â€” indexed as \"10 of 10, 0\nfailed\" and disclosed nothing, because Roslyn returns a tree with error nodes rather than throwing.\nFourth instance of DC-025. Worth stating: I nearly reported it from an experiment that had not run,\nand it became a finding only after the broken file was verified to exist.\n\nGate: 669 tests green (App 117, Core 552), six verifiers clean.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Do the five listed core next steps: a paged bulk evidence read, register the absence class, move menu placement to the catalog, measure a repository that does not build, and retract departed scopes",
      "done_when": "Each step landed with a control; my own merge defect registered and tooled; full gate green; committed, merged and published; tables produced",
      "change": "cl-0068",
      "git": {
        "sha": "5af8dd94aeae26d187fd0b6b805453b04e60b06c",
        "short": "5af8dd94a",
        "branch": "session/phase3-pane-probes",
        "pushed": false
      }
    },
    {
      "id": "al-0224",
      "shortname": "bulk-read-retraction-menu-catalog-broken-build",
      "datetime": "2026-08-29T17:48:53Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps you have listed\nprovide the standard status and next steps tables afterwards",
      "summary": "Five core steps, plus a defect of my own that surfaced first.\n\nThe design session had to re-emit an audit entry \"lost to a DC-013 merge collision\" — lost by MY\nresolution. Unioning an append-only file keyed by id, with setdefault, silently drops one side where\nboth used the same id, and the gate stays green because uniqueness is satisfied precisely by the\nremoval. Registered as DC-026 and given a tool that unions by CONTENT so nothing can be dropped. The\ncurrent log is complete: 75 entries, 75 unique ids, and the only gaps are the fifteen my race probe\nburned.\n\nThe bulk evidence read closes the residual from last turn exactly. The panes now see 12,100 of 12,100\nassertions and 124 of 124 join edges — zero divergence, where before they rebuilt the set node by\nnode through a 50-neighbour Describe and lost two edges. Paged, with a cursor that is the same tuple\nthe ORDER BY uses, pinned with a page size of one so every boundary is exercised.\n\nA departed scope's evidence is retired by an empty snapshot at a higher generation — superseded,\nnever deleted, because destroying the rows would destroy the record of what the graph once said.\n\nEvery catalog command now declares its menu, so Core stops needing to edit a design-owned file to add\none; the switch is a one-line change recorded as a request.\n\nAnd the fifth repository — a real one with a deliberate syntax error — indexed as \"10 of 10, 0\nfailed\" and disclosed nothing, because Roslyn returns a tree with error nodes rather than throwing.\nFourth instance of DC-025. Worth stating: I nearly reported it from an experiment that had not run,\nand it became a finding only after the broken file was verified to exist.\n\nGate: 669 tests green (App 117, Core 552), six verifiers clean.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Do the five listed core next steps: a paged bulk evidence read, register the absence class, move menu placement to the catalog, measure a repository that does not build, and retract departed scopes",
      "done_when": "Each step landed with a control; my own merge defect registered and tooled; full gate green; committed, merged and published; tables produced",
      "change": "cl-0068",
      "git": {
        "sha": "5af8dd94aeae26d187fd0b6b805453b04e60b06c",
        "short": "5af8dd94a",
        "branch": "session/phase3-pane-probes",
        "pushed": false
      }
    },
    {
      "id": "al-0093",
      "shortname": "facelift-palette-and-cards",
      "datetime": "2026-08-29T17:49:24Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "Continue the facelift: soften the composited chrome and the evidence cards.",
      "summary": "Command palette overlay: island corner radius + ElevationRaised soft shadow (composited chrome, airspace-correct). Context boxes: raised SurfaceRaised card ground + island radius + 1px hue border. Build 0/0; App.Tests 117/117.",
      "kind": "manual",
      "skill": "implement",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/CommandPalette.cs",
        "src/AiDe.App/Workbench/ContextMapSurface.cs"
      ],
      "tags": [
        "facelift"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0094",
      "shortname": "facelift-craftgate-and-avalondock-decision",
      "datetime": "2026-08-29T18:03:43Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "Screenshot confirms dark theme renders; execute next styling steps to completion.",
      "summary": "Ran the deterministic craft gate (Impeccable) over all 5 mockups; documented the code-node syntax palette + a scrim token in DESIGN.md (cleared design-system-color), fixed one heading skip; design-lint clean. Recorded the AvalonDock tab accent-retokenization as an evidence-backed deferral (squared tabs = IDE convention; accent lives in embedded vstheme+BAML, no cheap override). Build 0/0, tests 117/117.",
      "kind": "skill",
      "skill": "ui-design",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "docs/reviews/ui-mockups-craft-gate.md",
        "docs/notes/avalondock-tab-styling-decision.md"
      ],
      "tags": [
        "facelift"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0095",
      "shortname": "lacking-corpus-shrink-gate-daemon-paging-scale",
      "datetime": "2026-08-29T18:04:24Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps you have listed\nprovide the standard status and next steps tables afterwards",
      "summary": "Five core steps. One of them changed on inspection, which is the useful part.\n\nThe listed step was \"retract on scope failure\". Opening RefreshScopeAsync first showed that keeping\nthe last good snapshot on failure is a DELIBERATE recorded decision â€” blanking the graph on a build\nerror would be worse â€” so retracting would have contradicted it rather than built on it. The real gap\nwas that what renders is then OLD and nothing said so. Failure now discloses stale-scope with the\nrevision still on screen, and the snapshot keeps rendering.\n\nDC-025 has a control instead of a habit: a corpus of workspaces defined by what they LACK â€” empty,\nonly-Python, unparseable source, no context map, a bounded read, a failed scope. Every case asserts a\nsentence rather than a count, because the counts were always right. Fixtures always have the thing,\nwhich is why the class survived four times; this is the deliberate opposite.\n\nThe gate that missed DC-026 now looks for the loss: verify-audit-log.py compares each log against\nHEAD and fails when a committed id has disappeared. It only counted duplicates before, which is\nexactly why it stayed green while my merge removed an entry â€” uniqueness was satisfied precisely by\nthe removal. Observed failing.\n\nEvidenceAsync is proven across the daemon at a page size of one, so the cursor is exercised at every\nboundary. The last three cross-boundary defects were all \"right in process, wrong through the pipe\".\n\nAnd scale, measured on a synthetic workspace and labelled as such: 20 projects, 2,400 types, 21,066\nassertions. First index 13.5s, re-index 0.1s, paged read of everything 185ms with exact agreement.\nThe read is not the problem at this size; extraction is.\n\nGate: 677 tests green (App 117, Core 560), six verifiers clean.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Do the five listed core next steps: a lacking-workspace corpus, prove paging across the daemon, measure at scale, detect a shrinking log, and address stale evidence on scope failure",
      "done_when": "Each step landed with a control observed working; the retraction step reconciled with the recorded decision it would have contradicted; full gate green; committed, merged and published; tables produced",
      "change": "cl-0070",
      "git": {
        "sha": "8ec320e3553fa7d9a11a9dd288c67ca5b02985e4",
        "short": "8ec320e35",
        "branch": "session/phase3-pane-probes",
        "pushed": true
      }
    },
    {
      "id": "al-0225",
      "shortname": "lacking-corpus-shrink-gate-daemon-paging-scale",
      "datetime": "2026-08-29T18:04:24Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps you have listed\nprovide the standard status and next steps tables afterwards",
      "summary": "Five core steps. One of them changed on inspection, which is the useful part.\n\nThe listed step was \"retract on scope failure\". Opening RefreshScopeAsync first showed that keeping\nthe last good snapshot on failure is a DELIBERATE recorded decision — blanking the graph on a build\nerror would be worse — so retracting would have contradicted it rather than built on it. The real gap\nwas that what renders is then OLD and nothing said so. Failure now discloses stale-scope with the\nrevision still on screen, and the snapshot keeps rendering.\n\nDC-025 has a control instead of a habit: a corpus of workspaces defined by what they LACK — empty,\nonly-Python, unparseable source, no context map, a bounded read, a failed scope. Every case asserts a\nsentence rather than a count, because the counts were always right. Fixtures always have the thing,\nwhich is why the class survived four times; this is the deliberate opposite.\n\nThe gate that missed DC-026 now looks for the loss: verify-audit-log.py compares each log against\nHEAD and fails when a committed id has disappeared. It only counted duplicates before, which is\nexactly why it stayed green while my merge removed an entry — uniqueness was satisfied precisely by\nthe removal. Observed failing.\n\nEvidenceAsync is proven across the daemon at a page size of one, so the cursor is exercised at every\nboundary. The last three cross-boundary defects were all \"right in process, wrong through the pipe\".\n\nAnd scale, measured on a synthetic workspace and labelled as such: 20 projects, 2,400 types, 21,066\nassertions. First index 13.5s, re-index 0.1s, paged read of everything 185ms with exact agreement.\nThe read is not the problem at this size; extraction is.\n\nGate: 677 tests green (App 117, Core 560), six verifiers clean.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Do the five listed core next steps: a lacking-workspace corpus, prove paging across the daemon, measure at scale, detect a shrinking log, and address stale evidence on scope failure",
      "done_when": "Each step landed with a control observed working; the retraction step reconciled with the recorded decision it would have contradicted; full gate green; committed, merged and published; tables produced",
      "change": "cl-0070",
      "git": {
        "sha": "8ec320e3553fa7d9a11a9dd288c67ca5b02985e4",
        "short": "8ec320e35",
        "branch": "session/phase3-pane-probes",
        "pushed": true
      }
    },
    {
      "id": "al-0096",
      "shortname": "harness-labels-and-ci-craftgate",
      "datetime": "2026-08-29T18:12:36Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "Do the harness label bump and wire the craft gate into CI.",
      "summary": "Bumped review-harness micro-labels 10px->11px across 4 mockups (cleared the undersized-text findings; app-facelift 12->6, residue is deliberate dense chrome). Added .github/workflows/ui-craft.yml: a slow-ring advisory craft-detector workflow (design-lint strict + ui-craft-gate report to step summary + artifact).",
      "kind": "manual",
      "skill": "implement",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        ".github/workflows/ui-craft.yml"
      ],
      "tags": [
        "facelift"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0097",
      "shortname": "canvas-2d-3d-toggle",
      "datetime": "2026-08-29T18:18:17Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "Implement CanvasSurface 2D/3D.",
      "summary": "Added a self-contained 2D<->3D toggle to the canvas page: Fibonacci-sphere layout + hand-rolled perspective projection (no CDN), drag-to-rotate, 2/3 keyboard shortcuts, depth-cued scale/opacity/z-order. Default stays 2D so the focus probe is unaffected. Retokenized the canvas accent to #5B9DD9. Preserved the keyboard-trap/focus/__tabsSeen/node.activate contracts (P2-FOCUS-01/03 pass through real WebView2). Build 0/0; App.Tests 117/117; embedded-script gate OK.",
      "kind": "manual",
      "skill": "implement",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/CanvasPage.cs"
      ],
      "tags": [
        "facelift",
        "canvas"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0098",
      "shortname": "investigate-INV-0001",
      "datetime": "2026-08-29T18:24:54Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "/investigate â€” i am noticing that the agent sessions are still not using \"my profile\" they dont have my path or environment variables, for example ghcp and claude code are both installed so they should both work with my profile\nonce you understand what the issue is implement the solution dont wait for review\n--------------------------------------------------\nalso do all the next steps as usual and then provide the status and next steps table as usual",
      "summary": "Reported: \"the agent sessions do not have my profile or my environment variables â€” ghcp and claude\nare both installed so they should both work.\"\n\nThe verified cause is not in this product. The machine's PATH is 22,297 characters and cmd.exe\nsilently drops a variable that large, so every .cmd shim â€” which is every npm-installed CLI â€” starts\nwith an EMPTY PATH and cannot find node, git or itself. claude works because claude.exe is a real\nexecutable with no cmd in the path; ghcp does not exist at all, and the Copilot CLI has an npm .cmd\nshim. That asymmetry is the entire symptom.\n\nNecessary and sufficient, both measured: at 22,297 characters a cmd child receives an empty PATH,\nreproduced twice including once with no part of AI-DE involved; trimmed to 1,799 characters the same\nchild receives the full PATH. AI-DE passes the environment correctly â€” PowerShell started from the\nsame inherited block reads all 22,297 characters and resolves claude.\n\nFive hypotheses were ruled out with evidence, including one flag removed and re-measured, and the\nbelief that the profile was at fault â€” which is where two turns of work went first, because the\nsymptom is indistinguishable from a launcher bug.\n\nThe cause of the 22,297 characters: roughly 190 entries of the shape Temp\\biohacker-nuget-<guid>\\\ndotnet-home\\.dotnet\\tools, appended to the PERSISTED user PATH by another project's build tooling and\nnever removed. Each is unique, so there is nothing to de-duplicate.\n\nWhat was ours is the silence. The terminal opened, looked healthy, and the tools were absent with\nnothing saying why. EnvironmentHealth.Inspect now states the size, the limit and the largest repeated\ngroup â€” because 200 unique paths is a number, not a lead â€” announced once per shell, never per pane.\nIt does not edit PATH: a tool that silently rewrites the environment to make itself work has hidden\nthe problem from the only person who can fix it.\n\nAgents are now hosted in the login shell rather than launched beside it, which delivers the profile\nhalf of the request and makes .ps1 shims resolve. It is recorded honestly as NOT fixing the reported\nsymptom â€” a .cmd shim invoked from the hosting shell still starts cmd, which still drops the PATH.\nMeasured, not assumed.\n\nRegistered as DC-027: the environment a parent hands a child is not the one the child receives.",
      "kind": "skill",
      "skill": "investigate",
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [
        "docs/investigations/INV-0001-agent-terminals-lack-the-users-environment.md"
      ],
      "tags": [
        "investigation"
      ],
      "outcome": "success",
      "goal": "Find the verified root cause of agent terminals lacking the user's PATH and profile, implement the fix without waiting for review, and register the class",
      "done_when": "Cause proven necessary and sufficient; competing causes ruled out with evidence; fix implemented and tested; DC-027 registered; investigation artifact written and indexed",
      "change": "cl-0071",
      "git": {
        "sha": "2731758925df4bb4b06ffae1f2186ad43cc82958",
        "short": "273175892",
        "branch": "session/phase3-pane-probes",
        "pushed": true
      }
    },
    {
      "id": "al-0226",
      "shortname": "investigate-INV-0001",
      "datetime": "2026-08-29T18:24:54Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "/investigate — i am noticing that the agent sessions are still not using \"my profile\" they dont have my path or environment variables, for example ghcp and claude code are both installed so they should both work with my profile\nonce you understand what the issue is implement the solution dont wait for review\n--------------------------------------------------\nalso do all the next steps as usual and then provide the status and next steps table as usual",
      "summary": "Reported: \"the agent sessions do not have my profile or my environment variables — ghcp and claude\nare both installed so they should both work.\"\n\nThe verified cause is not in this product. The machine's PATH is 22,297 characters and cmd.exe\nsilently drops a variable that large, so every .cmd shim — which is every npm-installed CLI — starts\nwith an EMPTY PATH and cannot find node, git or itself. claude works because claude.exe is a real\nexecutable with no cmd in the path; ghcp does not exist at all, and the Copilot CLI has an npm .cmd\nshim. That asymmetry is the entire symptom.\n\nNecessary and sufficient, both measured: at 22,297 characters a cmd child receives an empty PATH,\nreproduced twice including once with no part of AI-DE involved; trimmed to 1,799 characters the same\nchild receives the full PATH. AI-DE passes the environment correctly — PowerShell started from the\nsame inherited block reads all 22,297 characters and resolves claude.\n\nFive hypotheses were ruled out with evidence, including one flag removed and re-measured, and the\nbelief that the profile was at fault — which is where two turns of work went first, because the\nsymptom is indistinguishable from a launcher bug.\n\nThe cause of the 22,297 characters: roughly 190 entries of the shape Temp\\biohacker-nuget-<guid>\\\ndotnet-home\\.dotnet\\tools, appended to the PERSISTED user PATH by another project's build tooling and\nnever removed. Each is unique, so there is nothing to de-duplicate.\n\nWhat was ours is the silence. The terminal opened, looked healthy, and the tools were absent with\nnothing saying why. EnvironmentHealth.Inspect now states the size, the limit and the largest repeated\ngroup — because 200 unique paths is a number, not a lead — announced once per shell, never per pane.\nIt does not edit PATH: a tool that silently rewrites the environment to make itself work has hidden\nthe problem from the only person who can fix it.\n\nAgents are now hosted in the login shell rather than launched beside it, which delivers the profile\nhalf of the request and makes .ps1 shims resolve. It is recorded honestly as NOT fixing the reported\nsymptom — a .cmd shim invoked from the hosting shell still starts cmd, which still drops the PATH.\nMeasured, not assumed.\n\nRegistered as DC-027: the environment a parent hands a child is not the one the child receives.",
      "kind": "skill",
      "skill": "investigate",
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [
        "docs/investigations/INV-0001-agent-terminals-lack-the-users-environment.md"
      ],
      "tags": [
        "investigation"
      ],
      "outcome": "success",
      "goal": "Find the verified root cause of agent terminals lacking the user's PATH and profile, implement the fix without waiting for review, and register the class",
      "done_when": "Cause proven necessary and sufficient; competing causes ruled out with evidence; fix implemented and tested; DC-027 registered; investigation artifact written and indexed",
      "change": "cl-0071",
      "git": {
        "sha": "2731758925df4bb4b06ffae1f2186ad43cc82958",
        "short": "273175892",
        "branch": "session/phase3-pane-probes",
        "pushed": true
      }
    },
    {
      "id": "al-0099",
      "shortname": "avalondock-accent-retokenized",
      "datetime": "2026-08-29T18:25:55Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "Do not defer - retokenize the AvalonDock accent to the palette.",
      "summary": "Retokenized the AvalonDock VS2013 dark accent from VS blue (#007ACC family) to the app palette (#5B9DD9 etc.) via DockThemeAccents.Retokenise - a value-based brush override written as direct manager-resource entries (beats the merged theme via DynamicResource, no template surgery, no blanking risk). Real keys established by a runtime probe (disposed). Proven by DockThemeAccentsTests (selected-tab key #007ACC->#5B9DD9). Corner-rounding stays deferred (needs retemplating; squared tabs are the IDE convention). Clean build 0/0; App.Tests 118/118.",
      "kind": "manual",
      "skill": "implement",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/DockThemeAccents.cs"
      ],
      "tags": [
        "facelift",
        "avalondock"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0100",
      "shortname": "path-cleanup-measured-limit-extraction-profile",
      "datetime": "2026-08-29T20:15:57Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps you have listed\nprovide the standard status and next steps tables afterwards",
      "summary": "Four of the five listed steps done, plus one of the five deferred ones. What was not done is named\nrather than half-landed.\n\nThe user PATH is cleaned: 21,528 characters and 206 entries down to 1,150 and 19. The 187 removed\nentries were all throwaway build directories that were ALREADY ABSENT FROM DISK, which is what made\nthe removal safe rather than a judgement call, and the previous value is backed up to\n%LOCALAPPDATA%\\AiDe. Verified after: a .cmd shim in a fresh process receives PATH, and both copilot\nand claude resolve through it.\n\nThe cmd limit is bisected rather than quoted â€” 8,151 carried, 8,152 dropped, with \"The input line is\ntoo long\" printed at the boundary â€” and pinned by a test so it cannot drift back to a documented\nnumber nobody measured.\n\nEnvironmentHealth measures every variable now, not only PATH: any oversized one is dropped by the\nsame mechanism, and checking only the variable that happened to bite is how a class gets found twice.\nAvailableAgents stops trusting a PATH filter that an unhealthy environment invalidates, offering\nevery agent instead â€” a menu that silently omits one is invisible, while a launch that fails is not,\nand the shell has already said why.\n\nExtraction is profiled: roughly 98% of the cost is the READ phase, 505-616ms per project against\n6-15ms for the symbol walk once the JIT is warm. The previous \"extraction is the cost\" was true and\nuseless. This moves the next optimisation to avoiding the read â€” per file rather than per scope â€” and\nsays plainly that making the walk faster would recover about one percent.\n\nNOT done, and listed rather than implied: surfacing stale-scope and source-did-not-parse in the panes\n(design-owned rendering, recorded as a request), file-granularity incremental extraction, a\nsecond-language extractor, and agreeing a merge policy with the design session.\n\nGate: 681 tests green (App 118, Core 563), six verifiers clean.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Do the listed next steps: clean the user PATH, carry the deferred core work, widen the environment check, stop trusting the PATH filter, and bisect the cmd limit",
      "done_when": "The reported symptom verified fixed at its cause; each remaining step landed with a control or explicitly named as not done; full gate green; committed, merged and published",
      "change": "cl-0073",
      "git": {
        "sha": "f5fb23aaa1ed763026de8a647956f7475824174c",
        "short": "f5fb23aaa",
        "branch": "session/phase3-pane-probes",
        "pushed": true
      }
    },
    {
      "id": "al-0227",
      "shortname": "path-cleanup-measured-limit-extraction-profile",
      "datetime": "2026-08-29T20:15:57Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps you have listed\nprovide the standard status and next steps tables afterwards",
      "summary": "Four of the five listed steps done, plus one of the five deferred ones. What was not done is named\nrather than half-landed.\n\nThe user PATH is cleaned: 21,528 characters and 206 entries down to 1,150 and 19. The 187 removed\nentries were all throwaway build directories that were ALREADY ABSENT FROM DISK, which is what made\nthe removal safe rather than a judgement call, and the previous value is backed up to\n%LOCALAPPDATA%\\AiDe. Verified after: a .cmd shim in a fresh process receives PATH, and both copilot\nand claude resolve through it.\n\nThe cmd limit is bisected rather than quoted — 8,151 carried, 8,152 dropped, with \"The input line is\ntoo long\" printed at the boundary — and pinned by a test so it cannot drift back to a documented\nnumber nobody measured.\n\nEnvironmentHealth measures every variable now, not only PATH: any oversized one is dropped by the\nsame mechanism, and checking only the variable that happened to bite is how a class gets found twice.\nAvailableAgents stops trusting a PATH filter that an unhealthy environment invalidates, offering\nevery agent instead — a menu that silently omits one is invisible, while a launch that fails is not,\nand the shell has already said why.\n\nExtraction is profiled: roughly 98% of the cost is the READ phase, 505-616ms per project against\n6-15ms for the symbol walk once the JIT is warm. The previous \"extraction is the cost\" was true and\nuseless. This moves the next optimisation to avoiding the read — per file rather than per scope — and\nsays plainly that making the walk faster would recover about one percent.\n\nNOT done, and listed rather than implied: surfacing stale-scope and source-did-not-parse in the panes\n(design-owned rendering, recorded as a request), file-granularity incremental extraction, a\nsecond-language extractor, and agreeing a merge policy with the design session.\n\nGate: 681 tests green (App 118, Core 563), six verifiers clean.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Do the listed next steps: clean the user PATH, carry the deferred core work, widen the environment check, stop trusting the PATH filter, and bisect the cmd limit",
      "done_when": "The reported symptom verified fixed at its cause; each remaining step landed with a control or explicitly named as not done; full gate green; committed, merged and published",
      "change": "cl-0073",
      "git": {
        "sha": "f5fb23aaa1ed763026de8a647956f7475824174c",
        "short": "f5fb23aaa",
        "branch": "session/phase3-pane-probes",
        "pushed": true
      }
    },
    {
      "id": "al-0102",
      "shortname": "path-guard-tree-cache-merge-protocol",
      "datetime": "2026-08-29T20:31:12Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps you have listed\nprovide the standard status and next steps tables afterwards",
      "summary": "Four of five steps done; the fifth named as not done rather than half-landed.\n\nWhat appends the temp directories to PATH could not be found â€” no repository on this machine contains\nthe string, so it was not a checked-in script and cannot be stopped at source from here. The durable\nanswer available was a guard: PATH regrowth is now caught by SHAPE rather than size. Ten or more\nentries pointing at directories that do not exist is a finding, with the largest group named, so the\nnext accumulation is caught at twenty entries instead of a hundred and eighty-seven.\n\nFile-granularity incremental extraction landed, and the measurement behind it was wrong twice first.\nThe profile said the read is 98% of extraction; splitting the read produced \"parsing is 97% of it, so\ncache the trees\" â€” plausible, confident, and wrong, because the timer wrapped File.ReadAllText and\nParseText together and called the total \"parse\". Timed apart: read 576-690ms, parse 4-5ms. File I/O\nis roughly 97% of everything extraction does, the opposite half. Caught only because a follow-up run\nproduced a 40x speedup with ZERO cache hits and no correct model explained that. Appended to DC-009.\n\nSyntaxTreeCache was built on the wrong rationale and is right anyway, because a hit skips the whole\nfactory â€” disk read as well as parse. 720 of 720 trees reused on a forced re-index, 1.0s to 0.6s, and\nit covers the case the scope fingerprint cannot: one file edited in a project of a hundred and twenty.\n\nThe two render requests and a concrete merge protocol are written into the session contract rather\nthan raised in conversation. Item 4 of the protocol â€” main takes fast-forwards, or moves to pull\nrequests â€” is the one that needs a yes or a no from the design session, and it is listed as unsettled\nuntil they answer.\n\nNOT done: a second-language extractor. It is a substantial piece and the turn went into the\nmeasurement being wrong, which was worth the time.\n\nGate: 683 tests green (App 118, Core 565), six verifiers clean.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Do the listed next steps: find and stop the PATH appender, file-granularity incremental extraction, record the render requests, propose a merge protocol, and a second-language extractor",
      "done_when": "Each step landed with a control or explicitly named as not done; the measurement corrected where it was wrong; full gate green; committed, merged and published",
      "change": "cl-0074",
      "git": {
        "sha": "192fb3d05894f43aac6eb4e1cc920f572bf682dc",
        "short": "192fb3d05",
        "branch": "session/phase3-pane-probes",
        "pushed": true
      }
    },
    {
      "id": "al-0228",
      "shortname": "path-guard-tree-cache-merge-protocol",
      "datetime": "2026-08-29T20:31:12Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps you have listed\nprovide the standard status and next steps tables afterwards",
      "summary": "Four of five steps done; the fifth named as not done rather than half-landed.\n\nWhat appends the temp directories to PATH could not be found — no repository on this machine contains\nthe string, so it was not a checked-in script and cannot be stopped at source from here. The durable\nanswer available was a guard: PATH regrowth is now caught by SHAPE rather than size. Ten or more\nentries pointing at directories that do not exist is a finding, with the largest group named, so the\nnext accumulation is caught at twenty entries instead of a hundred and eighty-seven.\n\nFile-granularity incremental extraction landed, and the measurement behind it was wrong twice first.\nThe profile said the read is 98% of extraction; splitting the read produced \"parsing is 97% of it, so\ncache the trees\" — plausible, confident, and wrong, because the timer wrapped File.ReadAllText and\nParseText together and called the total \"parse\". Timed apart: read 576-690ms, parse 4-5ms. File I/O\nis roughly 97% of everything extraction does, the opposite half. Caught only because a follow-up run\nproduced a 40x speedup with ZERO cache hits and no correct model explained that. Appended to DC-009.\n\nSyntaxTreeCache was built on the wrong rationale and is right anyway, because a hit skips the whole\nfactory — disk read as well as parse. 720 of 720 trees reused on a forced re-index, 1.0s to 0.6s, and\nit covers the case the scope fingerprint cannot: one file edited in a project of a hundred and twenty.\n\nThe two render requests and a concrete merge protocol are written into the session contract rather\nthan raised in conversation. Item 4 of the protocol — main takes fast-forwards, or moves to pull\nrequests — is the one that needs a yes or a no from the design session, and it is listed as unsettled\nuntil they answer.\n\nNOT done: a second-language extractor. It is a substantial piece and the turn went into the\nmeasurement being wrong, which was worth the time.\n\nGate: 683 tests green (App 118, Core 565), six verifiers clean.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Do the listed next steps: find and stop the PATH appender, file-granularity incremental extraction, record the render requests, propose a merge protocol, and a second-language extractor",
      "done_when": "Each step landed with a control or explicitly named as not done; the measurement corrected where it was wrong; full gate green; committed, merged and published",
      "change": "cl-0074",
      "git": {
        "sha": "192fb3d05894f43aac6eb4e1cc920f572bf682dc",
        "short": "192fb3d05",
        "branch": "session/phase3-pane-probes",
        "pushed": true
      }
    },
    {
      "id": "al-0104",
      "shortname": "soften-dropdown-menus",
      "datetime": "2026-08-29T21:08:00Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "The menus have thick borders and look clunky - soften them.",
      "summary": "Retemplated the four MenuItem roles + ContextMenu in App.xaml: dropdowns are now thin (1px) rounded (8px) floating popups on a subtle soft shadow, replacing the clunky thick square system chrome. A standalone XamlReader probe caught and fixed a launch-crashing forward StaticResource (TextMutedBrush referenced before its definition -> DynamicResource) before it shipped; then validated the real templates parse+apply and the submenu opens. Added MenuTemplateTests (render guard). App.dll compiles clean; full local suite blocked by the running app (relaunch to see + let it run).",
      "kind": "manual",
      "skill": "implement",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "src/AiDe.App/App.xaml",
        "tests/AiDe.App.Tests/MenuTemplateTests.cs"
      ],
      "tags": [
        "facelift",
        "menus"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0105",
      "shortname": "python-extractor-paging-tie-io-measured",
      "datetime": "2026-08-29T21:08:20Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps you have listed\nprovide the standard status and next steps tables afterwards",
      "summary": "A second language, and a paging defect the second language exposed.\n\nPythonExtractor reads modules, top-level classes and functions, and import edges. Six repositories\nhad disclosed unread Python before it existed; the disclosure was right and it is not a substitute,\nbecause a graph that says \"there is Python here and I cannot see it\" is honest and still blind.\n\nIt reads STRUCTURE, not semantics, and says so on every scope: imports are not resolved, nested\ndeclarations are invisible, dynamic imports are not followed. Declarations are recognised at column\nzero only â€” an indented def is a method, and claiming it as a module-level function would put a\nsymbol in the graph that no importer can reach. Import edges are INFERRED, because the target is the\nmodule path as written and whether it resolves to anything is not established here; calling that\nVerified is precisely the defect DC-022 is about. A simplify: marker records the ceiling and the\nupgrade trigger.\n\nPython also came OFF the unanalysed-languages list on the same day, because a closed gap reported as\nopen is the same defect as an open one hidden.\n\nMeasured on the repository that started this: ai-forward produced ZERO assertions and an empty\ndisclosure list three turns ago. It now produces 2,158 assertions across 5 scopes in 0.2 seconds,\nwith its remaining gaps named.\n\nAnd that run exposed a real paging defect: 2,158 in the store, 2,157 through the paged read.\n(subject, predicate, object) is NOT unique â€” two scopes can assert the same triple â€” so a cursor over\nthat ordering silently loses exactly the rows that tie, when a page boundary lands on one. Scope is\npart of the ordering and the cursor now, and a test builds two identical Python packages to\nmanufacture the tie deliberately.\n\nThe I/O question was measured rather than assumed and is only half answered. Raw reads of 120 files\ncost 5ms outside this product and 73ms on first touch from a different process â€” a 15x first-access\npenalty consistent with on-access scanning. Inside the extractor the same volume costs ~500ms, which\nis seven times worse again and is NOT explained. Recorded as a gap rather than a claim; the tree\ncache removes the cost on re-index whichever explanation turns out to be right.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Do the listed next steps: cross-process tree caching, a second-language extractor, the merge-protocol ask, the scanner measurement, and rendering the disclosures",
      "done_when": "Each step landed, deferred with a stated reason, or named as belonging to the design session; full gate green; committed, merged and published",
      "change": "cl-0075",
      "git": {
        "sha": "ccd9276972fdd91b6bb5df921979224e02ed476f",
        "short": "ccd927697",
        "branch": "session/phase3-pane-probes",
        "pushed": false
      }
    },
    {
      "id": "al-0229",
      "shortname": "python-extractor-paging-tie-io-measured",
      "datetime": "2026-08-29T21:08:20Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps you have listed\nprovide the standard status and next steps tables afterwards",
      "summary": "A second language, and a paging defect the second language exposed.\n\nPythonExtractor reads modules, top-level classes and functions, and import edges. Six repositories\nhad disclosed unread Python before it existed; the disclosure was right and it is not a substitute,\nbecause a graph that says \"there is Python here and I cannot see it\" is honest and still blind.\n\nIt reads STRUCTURE, not semantics, and says so on every scope: imports are not resolved, nested\ndeclarations are invisible, dynamic imports are not followed. Declarations are recognised at column\nzero only — an indented def is a method, and claiming it as a module-level function would put a\nsymbol in the graph that no importer can reach. Import edges are INFERRED, because the target is the\nmodule path as written and whether it resolves to anything is not established here; calling that\nVerified is precisely the defect DC-022 is about. A simplify: marker records the ceiling and the\nupgrade trigger.\n\nPython also came OFF the unanalysed-languages list on the same day, because a closed gap reported as\nopen is the same defect as an open one hidden.\n\nMeasured on the repository that started this: ai-forward produced ZERO assertions and an empty\ndisclosure list three turns ago. It now produces 2,158 assertions across 5 scopes in 0.2 seconds,\nwith its remaining gaps named.\n\nAnd that run exposed a real paging defect: 2,158 in the store, 2,157 through the paged read.\n(subject, predicate, object) is NOT unique — two scopes can assert the same triple — so a cursor over\nthat ordering silently loses exactly the rows that tie, when a page boundary lands on one. Scope is\npart of the ordering and the cursor now, and a test builds two identical Python packages to\nmanufacture the tie deliberately.\n\nThe I/O question was measured rather than assumed and is only half answered. Raw reads of 120 files\ncost 5ms outside this product and 73ms on first touch from a different process — a 15x first-access\npenalty consistent with on-access scanning. Inside the extractor the same volume costs ~500ms, which\nis seven times worse again and is NOT explained. Recorded as a gap rather than a claim; the tree\ncache removes the cost on re-index whichever explanation turns out to be right.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Do the listed next steps: cross-process tree caching, a second-language extractor, the merge-protocol ask, the scanner measurement, and rendering the disclosures",
      "done_when": "Each step landed, deferred with a stated reason, or named as belonging to the design session; full gate green; committed, merged and published",
      "change": "cl-0075",
      "git": {
        "sha": "ccd9276972fdd91b6bb5df921979224e02ed476f",
        "short": "ccd927697",
        "branch": "session/phase3-pane-probes",
        "pushed": false
      }
    },
    {
      "id": "al-0106",
      "shortname": "ui-design-facelift-elevate",
      "datetime": "2026-08-29T21:44:35Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "the menus still have a goofy block; /ui-design review, elevate and visualize; give me a list of proposed things to consider",
      "summary": "Elevate-mode UI review. Root-caused + fixed the menu \"goofy block\" (popup Border had a lopsided Margin 2,2,12,12 + DropShadow -> the margin/shadow rendered as a light frame, and violated the radius+border-not-shadow principle; now the border fills the popup, clean rounded thin card). Recorded the craft-gate measurement (41 findings, all deliberate dense-meta/harness-chrome). Wrote docs/reviews/ui-facelift.md with rubric findings + a ranked plan led by a cohesive icon system, empty-state Wayfinders, rail affordances, and node inspection. Standalone-validated the menu parses+applies; App.Tests 121/121.",
      "kind": "skill",
      "skill": "ui-design",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "docs/reviews/ui-facelift.md",
        "src/AiDe.App/App.xaml"
      ],
      "tags": [
        "facelift",
        "ui-design"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0107",
      "shortname": "benchmark-artefact-python-imports-cursor-audit",
      "datetime": "2026-08-29T21:45:33Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps you have listed\nprovide the standard status and next steps tables afterwards",
      "summary": "The extraction profile is explained, and both earlier conclusions were artefacts of the benchmark.\n\nA fresh .NET process reading 120 newly-created files took 493ms (4.11ms/file); a SECOND fresh process\nover the same files took 6ms, and the repository's own source reads at 0.19ms/file. So the ~500ms\nwas a one-time, system-wide, per-file cost on the first read after creation â€” the signature of\non-access scanning â€” and it never recurs. It is not extraction's cost at all.\n\nRun against a real repository the profile inverts completely. TheTerrace, 463 files in its main\nscope: read 53ms, parse 694ms, walk 1,167ms. The walk is the LARGEST cost on real code and looked\nfree on the synthetic workload, because the generated types were trivial â€” no inheritance, no\ngenerics, four fields each.\n\nTwo independent flaws in one generator, each inverting a published conclusion. Registered as DC-028:\na synthetic benchmark measures the benchmark unless its workload resembles the real one in the\ndimensions that drive cost. The control is that the spike prints the same timings for named real\nrepositories, so the synthetic figure and the real one appear side by side.\n\nPython imports are resolved within their scope now. An import naming a module the scope contains\npoints at a file that exists and was read, so the edge is VERIFIED; anything else stays INFERRED with\nthe name as written, because it may be a package, a module in another scope, or nothing, and\nasserting which is the guess DC-022 is about. Relative imports resolve against the importing module's\npackage, one level per leading dot, and climbing above the root resolves to nothing rather than\nthrowing. On the real Python repository 82 of 330 imports now resolve, and the disclosure carries the\ncount of those that do not â€” a blanket \"imports are not resolved\" became a closed gap reported as\nopen the moment resolution landed.\n\nThe cursor audit found one more thing and cleared another. ResultBounds.NextCursor is ALWAYS null and\nno projection returning it pages â€” said out loud in the type, because a caller could loop on it and\nnever leave the first page with nothing failing. And DeriveClaimCurrent folds statuses with Max under\na comment saying the weakest wins: correct only because the enum is ordered strongest-first, so a\nreordering would silently invert it into manufacturing confidence. Pinned by a test.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Do the listed next steps: explain the extractor read gap, resolve Python imports, add TypeScript, get the merge decision, and audit every cursor for uniqueness",
      "done_when": "Each step landed, deferred with a stated reason, or named as another session's; the benchmark artefact registered as a class; full gate green; committed and merged",
      "git": {
        "sha": "a286b981cb8b83aaaf594921e1de5cad65377cce",
        "short": "a286b981c",
        "branch": "session/phase3-pane-probes",
        "pushed": true
      }
    },
    {
      "id": "al-0230",
      "shortname": "benchmark-artefact-python-imports-cursor-audit",
      "datetime": "2026-08-29T21:45:33Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps you have listed\nprovide the standard status and next steps tables afterwards",
      "summary": "The extraction profile is explained, and both earlier conclusions were artefacts of the benchmark.\n\nA fresh .NET process reading 120 newly-created files took 493ms (4.11ms/file); a SECOND fresh process\nover the same files took 6ms, and the repository's own source reads at 0.19ms/file. So the ~500ms\nwas a one-time, system-wide, per-file cost on the first read after creation — the signature of\non-access scanning — and it never recurs. It is not extraction's cost at all.\n\nRun against a real repository the profile inverts completely. TheTerrace, 463 files in its main\nscope: read 53ms, parse 694ms, walk 1,167ms. The walk is the LARGEST cost on real code and looked\nfree on the synthetic workload, because the generated types were trivial — no inheritance, no\ngenerics, four fields each.\n\nTwo independent flaws in one generator, each inverting a published conclusion. Registered as DC-028:\na synthetic benchmark measures the benchmark unless its workload resembles the real one in the\ndimensions that drive cost. The control is that the spike prints the same timings for named real\nrepositories, so the synthetic figure and the real one appear side by side.\n\nPython imports are resolved within their scope now. An import naming a module the scope contains\npoints at a file that exists and was read, so the edge is VERIFIED; anything else stays INFERRED with\nthe name as written, because it may be a package, a module in another scope, or nothing, and\nasserting which is the guess DC-022 is about. Relative imports resolve against the importing module's\npackage, one level per leading dot, and climbing above the root resolves to nothing rather than\nthrowing. On the real Python repository 82 of 330 imports now resolve, and the disclosure carries the\ncount of those that do not — a blanket \"imports are not resolved\" became a closed gap reported as\nopen the moment resolution landed.\n\nThe cursor audit found one more thing and cleared another. ResultBounds.NextCursor is ALWAYS null and\nno projection returning it pages — said out loud in the type, because a caller could loop on it and\nnever leave the first page with nothing failing. And DeriveClaimCurrent folds statuses with Max under\na comment saying the weakest wins: correct only because the enum is ordered strongest-first, so a\nreordering would silently invert it into manufacturing confidence. Pinned by a test.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Do the listed next steps: explain the extractor read gap, resolve Python imports, add TypeScript, get the merge decision, and audit every cursor for uniqueness",
      "done_when": "Each step landed, deferred with a stated reason, or named as another session's; the benchmark artefact registered as a class; full gate green; committed and merged",
      "git": {
        "sha": "a286b981cb8b83aaaf594921e1de5cad65377cce",
        "short": "a286b981c",
        "branch": "session/phase3-pane-probes",
        "pushed": true
      }
    },
    {
      "id": "al-0109",
      "shortname": "icon-system-rail",
      "datetime": "2026-08-29T21:58:07Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "Build the icon system (recommended next step).",
      "summary": "Icon system foundation + activity-rail slice: added 10 Lucide-style stroke Geometry resources to App.xaml (keyed by role, palette-inheriting, no image assets). Replaced the rail cryptic Unicode glyphs (â—«â—‘âœŽâ—·) with real vector icons + labels (Explore/Coordinate/Compose/Audit), stroke bound to each button Foreground (accent when active). All 10 geometries validated to parse (bounds computed) standalone - no crash-on-launch. Build 0/0; App.Tests 121/121.",
      "kind": "manual",
      "skill": "implement",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "src/AiDe.App/App.xaml",
        "src/AiDe.App/MainWindow.xaml"
      ],
      "tags": [
        "facelift",
        "icons"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0231",
      "shortname": "icon-system-rail",
      "datetime": "2026-08-29T21:58:07Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "Build the icon system (recommended next step).",
      "summary": "Icon system foundation + activity-rail slice: added 10 Lucide-style stroke Geometry resources to App.xaml (keyed by role, palette-inheriting, no image assets). Replaced the rail cryptic Unicode glyphs (◫◑✎◷) with real vector icons + labels (Explore/Coordinate/Compose/Audit), stroke bound to each button Foreground (accent when active). All 10 geometries validated to parse (bounds computed) standalone - no crash-on-launch. Build 0/0; App.Tests 121/121.",
      "kind": "manual",
      "skill": "implement",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "src/AiDe.App/App.xaml",
        "src/AiDe.App/MainWindow.xaml"
      ],
      "tags": [
        "facelift",
        "icons"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0110",
      "shortname": "icon-system-menus",
      "datetime": "2026-08-29T22:04:13Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "Extend the icon system to the menus.",
      "summary": "Menu icons: added an Icon column to the MenuItem submenu template + two geometries (IconLayout, IconRefresh), and wired MainMenuBuilder.IconFor to set MenuItem.Icon per command via a keyword map (terminal/prompt/open/index/refresh/canvas -> icon, layout default), stroked from the shared geometry set in the palette muted colour. 12 geometries validated to parse; menu+icon renders standalone. Build 0/0; App.Tests 121/121.",
      "kind": "manual",
      "skill": "implement",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/MainMenuBuilder.cs",
        "src/AiDe.App/App.xaml"
      ],
      "tags": [
        "facelift",
        "icons"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0111",
      "shortname": "investigate-terminal-kill",
      "datetime": "2026-08-30T13:57:46Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "investigate why opening a second terminal killed the prior terminal session",
      "summary": "Root cause: WorkbenchAdapter.Render() rebuilds all panes on every mutation, recreating each TerminalSurface; ConPTY kill-on-close job terminates the replaced live process. Any of 5 Render() sites kills every live terminal. Registered DC-029. Report at docs/investigations/INV-0002. STOP for review.",
      "kind": "skill",
      "skill": "investigate",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/investigations/INV-0002-opening-a-terminal-kills-the-others.md"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0112",
      "shortname": "specify-terminal-sessions",
      "datetime": "2026-08-30T14:02:41Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "specify proper multi-terminal behavior including rename, tab color, terminal customization and the fix for the kill defect",
      "summary": "Wrote docs/specs/terminal-sessions.md (3 layers): domain model with session-identity aggregate invariant (fix for DC-029), New terminal vs New agent terminal, rename/tab-colour/colour-scheme; Core owns lifecycle fix+command, Design owns customization UI.",
      "kind": "skill",
      "skill": "specify",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/specs/terminal-sessions.md"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0113",
      "shortname": "ui-design-activity-rail",
      "datetime": "2026-08-30T14:10:26Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "the menus and icons on the side are great but the text under the icons are cut off, maybe we need to just have the icons and tooltips? do another round of ui-design elevate to further refine the UI",
      "summary": "Activity rail elevated to icon-only (VS Code/JetBrains idiom): removed clipped 9px captions, kept tooltip + AutomationProperties.Name, 44px targets, softened borderless active pill. Landed in MainWindow.xaml (BAML recompiles). Mockup+review+DC-030.",
      "kind": "skill",
      "skill": "ui-design",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/mockups/activity-rail.html"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0114",
      "shortname": "full-graph-typescript-walk-profile",
      "datetime": "2026-08-30T14:37:36Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "choose TheTerrace repo: compare the knowledge graph from obsidian vs the knowledge graph from this tool, i see two nodes in this tool vs the full graph in obsidian... even if this was just C# extractors there is way more than what is showing in the tool. Lets focus on getting the full graph built and available to the tooling surfaces as that is the foundation for all of the tools and scenarios.\n--------------\nalso do all the next steps you identified above, autonomously, so we can make more progress on getting the core in place while the other session works on the UI/UX",
      "summary": "The graph surface had never shown a graph.\n\nReported by the user, comparing TheTerrace in this tool against the same repository in Obsidian: two\nnodes versus a full graph. The cause was not extraction â€” the store held 12,100 assertions across\n2,164 nodes â€” it was that the canvas called FindAsync with a limit of ONE to pick a root and then\ndrew that root's neighbours. A workspace of two thousand nodes rendered as the alphabetically first\nsymbol and its single neighbour, correctly, from the day it was written. Four unit tests described\nthat behaviour approvingly.\n\nGraphProjection answers the question the surface exists for: every node and edge, bounded by a cap\nthat is reported rather than silent. Attributes fold onto nodes instead of being drawn as edges â€” a\nhas_type arrow would put the string \"class\" in the graph as a thing other things point at â€” using the\nsame single definition the search already applies. LoadAsync with no root now means the whole graph;\na root means drill-down. Proven across the daemon as well, because every cross-boundary defect so far\nhas been right in process and wrong through the pipe.\n\nA second finding sat inside the first. With the graph finally visible, its six most-connected nodes\nwere string, int, Task<TResult>, DateTimeOffset, IReadOnlyList<T> and Guid â€” 773 edges to string\nalone. A graph whose centre is the BCL is not a picture of anybody's domain, and a cap ordered by raw\ndegree drops the user's own types to keep framework primitives. Nodes carry IsExternal now (nothing\nin the workspace declares them) and declared nodes are kept first. The same repository's centre\nbecame AppDbContext, Fixture, SportMonksProvider, ScheduledRefresh.\n\nRegistered as DC-031: a surface asks a narrower question than the one it exists to answer. The\ngeneralisation is one question â€” what is the cardinality of what this shows, against the cardinality\nof what it fetched â€” and the related tell is a test suite that describes the slice fluently.\n\nTheTerrace now: 28 scopes, 13,530 assertions, 2,813 nodes and 8,602 edges with nothing omitted.\nTypeScript is read, and the symbol walk is profiled.\n\nTypeScriptExtractor closes the largest remaining disclosure â€” typescript-not-analysed (165 files) on\na repository whose C# half was fully mapped. Same bargain as Python: structure, not semantics.\nExported top-level class, interface, type, enum, function and const; import and re-export\nspecifiers; nothing inside a function or a namespace block. Declaration files (.d.ts) are skipped\nbecause they re-state types defined elsewhere and indexing them would put every symbol in the graph\ntwice, once with nothing behind it.\n\nSpecifiers resolve only when RELATIVE and only to a file this scope contains â€” extensions optional,\na directory meaning its index file. A bare specifier is a package or a path alias and resolving it\nneeds configuration this extractor deliberately does not read, so it stays Inferred with the\nspecifier as written. TypeScript and JavaScript left the unanalysed-languages list the same day.\n\nIt is deliberately NOT a shared base with PythonExtractor. They look alike and are not the same:\nTypeScript's specifiers carry extensions and index files, its declarations are export-gated rather\nthan column-zero, and JSX changes what a valid line looks like. A base parameterised by all of that\nis more machinery than either extractor contains; a third language that fits the pattern is when the\nabstraction is earned.\n\nIndexing TheTerrace with it immediately raised a real store error: two files can share a module name\nâ€” app.ts beside a compiled app.js â€” so the same triple was asserted twice in one scope and the\nnatural key rejected it, correctly. Identical facts are one fact, so both extractors deduplicate\nbefore returning. Silencing the key would have weakened a control that is doing its job.\n\nAnd the symbol walk, which the real-repository profile named as the largest cost, is now split:\nenumerating the namespace tree is 135ms for 1,026 types, and reading each type's members is 1,074ms\nâ€” about 89% of the walk and half the whole scope. The next hypothesis worth testing is\nToDisplayString, called per symbol, but that is a hypothesis and is written down as one rather than\nacted on.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Build the full graph and make it available to the tooling surfaces, and carry the outstanding core next steps autonomously",
      "done_when": "The whole graph reaches the surfaces and is proven across the daemon; the class registered; TypeScript read; the walk profiled; full gate green; committed, merged and published",
      "change": "cl-0078",
      "git": {
        "sha": "fb43ecd5fb72deffe4471525ef8eac483487b9a7",
        "short": "fb43ecd5f",
        "branch": "session/phase3-pane-probes",
        "pushed": false
      }
    },
    {
      "id": "al-0232",
      "shortname": "full-graph-typescript-walk-profile",
      "datetime": "2026-08-30T14:37:36Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "choose TheTerrace repo: compare the knowledge graph from obsidian vs the knowledge graph from this tool, i see two nodes in this tool vs the full graph in obsidian... even if this was just C# extractors there is way more than what is showing in the tool. Lets focus on getting the full graph built and available to the tooling surfaces as that is the foundation for all of the tools and scenarios.\n--------------\nalso do all the next steps you identified above, autonomously, so we can make more progress on getting the core in place while the other session works on the UI/UX",
      "summary": "The graph surface had never shown a graph.\n\nReported by the user, comparing TheTerrace in this tool against the same repository in Obsidian: two\nnodes versus a full graph. The cause was not extraction — the store held 12,100 assertions across\n2,164 nodes — it was that the canvas called FindAsync with a limit of ONE to pick a root and then\ndrew that root's neighbours. A workspace of two thousand nodes rendered as the alphabetically first\nsymbol and its single neighbour, correctly, from the day it was written. Four unit tests described\nthat behaviour approvingly.\n\nGraphProjection answers the question the surface exists for: every node and edge, bounded by a cap\nthat is reported rather than silent. Attributes fold onto nodes instead of being drawn as edges — a\nhas_type arrow would put the string \"class\" in the graph as a thing other things point at — using the\nsame single definition the search already applies. LoadAsync with no root now means the whole graph;\na root means drill-down. Proven across the daemon as well, because every cross-boundary defect so far\nhas been right in process and wrong through the pipe.\n\nA second finding sat inside the first. With the graph finally visible, its six most-connected nodes\nwere string, int, Task<TResult>, DateTimeOffset, IReadOnlyList<T> and Guid — 773 edges to string\nalone. A graph whose centre is the BCL is not a picture of anybody's domain, and a cap ordered by raw\ndegree drops the user's own types to keep framework primitives. Nodes carry IsExternal now (nothing\nin the workspace declares them) and declared nodes are kept first. The same repository's centre\nbecame AppDbContext, Fixture, SportMonksProvider, ScheduledRefresh.\n\nRegistered as DC-031: a surface asks a narrower question than the one it exists to answer. The\ngeneralisation is one question — what is the cardinality of what this shows, against the cardinality\nof what it fetched — and the related tell is a test suite that describes the slice fluently.\n\nTheTerrace now: 28 scopes, 13,530 assertions, 2,813 nodes and 8,602 edges with nothing omitted.\nTypeScript is read, and the symbol walk is profiled.\n\nTypeScriptExtractor closes the largest remaining disclosure — typescript-not-analysed (165 files) on\na repository whose C# half was fully mapped. Same bargain as Python: structure, not semantics.\nExported top-level class, interface, type, enum, function and const; import and re-export\nspecifiers; nothing inside a function or a namespace block. Declaration files (.d.ts) are skipped\nbecause they re-state types defined elsewhere and indexing them would put every symbol in the graph\ntwice, once with nothing behind it.\n\nSpecifiers resolve only when RELATIVE and only to a file this scope contains — extensions optional,\na directory meaning its index file. A bare specifier is a package or a path alias and resolving it\nneeds configuration this extractor deliberately does not read, so it stays Inferred with the\nspecifier as written. TypeScript and JavaScript left the unanalysed-languages list the same day.\n\nIt is deliberately NOT a shared base with PythonExtractor. They look alike and are not the same:\nTypeScript's specifiers carry extensions and index files, its declarations are export-gated rather\nthan column-zero, and JSX changes what a valid line looks like. A base parameterised by all of that\nis more machinery than either extractor contains; a third language that fits the pattern is when the\nabstraction is earned.\n\nIndexing TheTerrace with it immediately raised a real store error: two files can share a module name\n— app.ts beside a compiled app.js — so the same triple was asserted twice in one scope and the\nnatural key rejected it, correctly. Identical facts are one fact, so both extractors deduplicate\nbefore returning. Silencing the key would have weakened a control that is doing its job.\n\nAnd the symbol walk, which the real-repository profile named as the largest cost, is now split:\nenumerating the namespace tree is 135ms for 1,026 types, and reading each type's members is 1,074ms\n— about 89% of the walk and half the whole scope. The next hypothesis worth testing is\nToDisplayString, called per symbol, but that is a hypothesis and is written down as one rather than\nacted on.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Build the full graph and make it available to the tooling surfaces, and carry the outstanding core next steps autonomously",
      "done_when": "The whole graph reaches the surfaces and is proven across the daemon; the class registered; TypeScript read; the walk profiled; full gate green; committed, merged and published",
      "change": "cl-0078",
      "git": {
        "sha": "fb43ecd5fb72deffe4471525ef8eac483487b9a7",
        "short": "fb43ecd5f",
        "branch": "session/phase3-pane-probes",
        "pushed": false
      }
    },
    {
      "id": "al-0115",
      "shortname": "implement-terminal-lifecycle-fix",
      "datetime": "2026-08-30T14:39:03Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "do all of these next steps, dont defer the rail polish",
      "summary": "Fixed DC-029: WorkbenchAdapter.Render() now reconciles by ContentId (reuse kept surfaces, dispose closed ones) so a layout mutation no longer kills live terminals; guarded canvas FocusLeaveRequested against duplicate handlers under reuse; added terminal.new 'New terminal' plain-shell command. App.Tests 123/123, Core.Tests 571/571.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/WorkbenchAdapter.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0116",
      "shortname": "implement-terminal-customization-rail",
      "datetime": "2026-08-30T14:50:41Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "do all of these next steps, dont defer the rail polish",
      "summary": "Implemented terminal rename (context menu + DisplayName via IHasDisplayName + shell re-render), per-session colour schemes (TerminalColorScheme presets -> swappable TerminalView palette), per-tab colour accent (TabColour DP bound in rounded-tab template), and rail polish (active left accent bar + roving-tabindex Up/Down nav). Registered DC-031 (reconcile idempotency). App 123/123, Core 571/571, app launches.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/TerminalSurface.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0117",
      "shortname": "walk-profile-fluent-reader-module-ids-graph-query",
      "datetime": "2026-08-30T15:22:20Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps you have listed\nprovide the standard status and next steps tables afterwards",
      "summary": "The hypothesis that `ToDisplayString` dominated the symbol walk was **wrong**, and measuring it found\na much larger defect than the one it was looking for.\n\nSplitting the walk by operation: display 46ms across 7,312 calls â€” **3.9%**. The real costs were\ndepends-on 613ms (51%) and attributes 403ms (34%). The attribute cost was one call:\n`FluentTableMappings` ran `DescendantNodes()` over all 1,026 type declarations, hunting an EF\n`ToTable` call. Hoisting it to the compilation with a source-text prefilter cut it to 1ms of\nprefilter across 465 files and 217ms of walking the 66 that survived.\n\nThen the 66 files were the finding. Most were EF migration `*.Designer.cs` snapshots â€” and the three\nthat were not revealed that the reader **did not work**. It matched `Entity<T>()...ToTable(\"x\")` as a\nsingle expression. TheTerrace, like most EF codebases, writes:\n\n    var terrace = modelBuilder.Entity<Terrace>();\n    terrace.ToTable(\"Terrace\", \"setup\");\n\nSo the extractor recovered **1 declared mapping and guessed 123**, on a repository that states every\none of them outright. Every one of those 123 inferred `maps_to` edges was a name-matching guess\nstanding where a declaration already existed. The same reader also emitted the entity name as\nwritten in source (`Order`) where every other assertion uses the display string (`Shop.Order`) â€” so\neven its successes were edges whose subject matched no node.\n\nResolved semantically now: ask the model for the RECEIVER's type. One rule answers every style â€”\nchained, local-variable, lambda-configuration, `IEntityTypeConfiguration<T>` â€” because in all of them\nthe receiver is an `EntityTypeBuilder<TEntity>`, and the symbol's display string is the name the rest\nof the extractor emits. **Verified joins 1 â†’ 64, inferred 123 â†’ 73, `declares_table` facts 63.**\n\nGenerated files are skipped, on correctness grounds before performance ones: EF writes a model\nsnapshot per migration, each calling `ToTable` for every entity *as it stood then*, so reading them\nasserts a table renamed three migrations ago as current fact wearing the same Verified badge. 63 of\nthe 66 files were these. `fluent-scan` fell 1,418ms â†’ 60ms warm as a side effect, not as the goal.\nThe skip is disclosed with a count, because skipped is not absent.\n\nDC-032 registered: **a reader recognises one spelling of a pattern and reports the rest as absent.**\nThe signature is a ratio nobody looks at â€” precise hits against fallback hits on real input â€” and it\nsurvives because the fallback is doing its job, so the surface looks populated and coverage stays\ngreen. The control was observed failing with the skip disabled.\n\n**Module ids were colliding, certainly rather than theoretically.** Both module-shaped extractors\nnamed a module by its path relative to its OWN SCOPE, and a scope is one directory. Every Python\npackage has an `__init__.py`, so a repository with five packages produced five scopes each declaring\na module called `__init__` â€” one node in the graph carrying the merged edges of five unrelated files.\nThe same for `index.ts`, `main`, `setup`, `conftest`. Ids are now the repository-relative path:\nunique by construction, readable, and the string a person would type to open the file.\n\nThat made cross-scope resolution possible, which was the actual next step. An import naming a sibling\npackage resolves to a file in a different scope, so it could never resolve from inside the one doing\nthe importing. `ExtractionRequest` now carries the workspace's module set, computed once per revision\nfrom the FILESYSTEM â€” not from the store, because resolving against what has already been extracted\nwould make an edge depend on the order the scopes happened to run in, the trap the Python extractor\nalready avoids one level down. Null means \"not supplied\", which is not \"there is nothing there\": the\nedge stays Inferred and the disclosure fires.\n\nA test comment had encoded the defective rule approvingly â€” *\"modules are named relative to it:\n`models`, not `src/models`\"* â€” which is DC-031's tell in its own words.\n\n**The graph surface is now queryable, not just fetchable.** `GraphQuery` filters by kind, by scope,\nand by whether to include nodes nothing declares. The filter runs BEFORE the cap and degree is\ncounted over what survives it: filtering afterwards would rank and trim the whole graph and only then\ndiscard, so a caller asking for the domain model would receive the wrong 5,000 nodes trimmed to the\nright kind, with nothing in the result saying so. Proven across the daemon as well as in process,\nbecause every cross-boundary defect here so far has been right in process and wrong through the pipe.\n\n**A next step I listed last turn was wrong and is withdrawn.** \"The index still walks every scope\" â€”\nit does not. `ScopeFingerprints` already skips an unchanged scope and counts it as *reused* rather\nthan *indexed*, deliberately, so \"28 of 28 indexed\" cannot be a true sentence about a run that read\nnothing. I asserted its absence without opening the file. The residual â€” incrementality BELOW the\nscope, so a one-file edit does not re-walk 1,026 types â€” is real, but it conflicts with the\nappend-only per-scope snapshot model and is a design decision, not a tidy-up.\n\n717 tests green (App 121, Core 596). Six gates clean. Zero design-owned files.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Carry the five listed next steps: profile the walk, resolve Python imports across scopes, give the graph a query surface, assess incremental re-index, settle merge-protocol item 4",
      "done_when": "Each step is done or its premise disproved in writing; full gate green; committed, merged, published",
      "change": "cl-0079",
      "git": {
        "sha": "acce3698269f5a36ad113b845875f5f19a051cac",
        "short": "acce36982",
        "branch": "session/phase3-pane-probes",
        "pushed": true
      }
    },
    {
      "id": "al-0233",
      "shortname": "walk-profile-fluent-reader-module-ids-graph-query",
      "datetime": "2026-08-30T15:22:20Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps you have listed\nprovide the standard status and next steps tables afterwards",
      "summary": "The hypothesis that `ToDisplayString` dominated the symbol walk was **wrong**, and measuring it found\na much larger defect than the one it was looking for.\n\nSplitting the walk by operation: display 46ms across 7,312 calls — **3.9%**. The real costs were\ndepends-on 613ms (51%) and attributes 403ms (34%). The attribute cost was one call:\n`FluentTableMappings` ran `DescendantNodes()` over all 1,026 type declarations, hunting an EF\n`ToTable` call. Hoisting it to the compilation with a source-text prefilter cut it to 1ms of\nprefilter across 465 files and 217ms of walking the 66 that survived.\n\nThen the 66 files were the finding. Most were EF migration `*.Designer.cs` snapshots — and the three\nthat were not revealed that the reader **did not work**. It matched `Entity<T>()...ToTable(\"x\")` as a\nsingle expression. TheTerrace, like most EF codebases, writes:\n\n    var terrace = modelBuilder.Entity<Terrace>();\n    terrace.ToTable(\"Terrace\", \"setup\");\n\nSo the extractor recovered **1 declared mapping and guessed 123**, on a repository that states every\none of them outright. Every one of those 123 inferred `maps_to` edges was a name-matching guess\nstanding where a declaration already existed. The same reader also emitted the entity name as\nwritten in source (`Order`) where every other assertion uses the display string (`Shop.Order`) — so\neven its successes were edges whose subject matched no node.\n\nResolved semantically now: ask the model for the RECEIVER's type. One rule answers every style —\nchained, local-variable, lambda-configuration, `IEntityTypeConfiguration<T>` — because in all of them\nthe receiver is an `EntityTypeBuilder<TEntity>`, and the symbol's display string is the name the rest\nof the extractor emits. **Verified joins 1 → 64, inferred 123 → 73, `declares_table` facts 63.**\n\nGenerated files are skipped, on correctness grounds before performance ones: EF writes a model\nsnapshot per migration, each calling `ToTable` for every entity *as it stood then*, so reading them\nasserts a table renamed three migrations ago as current fact wearing the same Verified badge. 63 of\nthe 66 files were these. `fluent-scan` fell 1,418ms → 60ms warm as a side effect, not as the goal.\nThe skip is disclosed with a count, because skipped is not absent.\n\nDC-032 registered: **a reader recognises one spelling of a pattern and reports the rest as absent.**\nThe signature is a ratio nobody looks at — precise hits against fallback hits on real input — and it\nsurvives because the fallback is doing its job, so the surface looks populated and coverage stays\ngreen. The control was observed failing with the skip disabled.\n\n**Module ids were colliding, certainly rather than theoretically.** Both module-shaped extractors\nnamed a module by its path relative to its OWN SCOPE, and a scope is one directory. Every Python\npackage has an `__init__.py`, so a repository with five packages produced five scopes each declaring\na module called `__init__` — one node in the graph carrying the merged edges of five unrelated files.\nThe same for `index.ts`, `main`, `setup`, `conftest`. Ids are now the repository-relative path:\nunique by construction, readable, and the string a person would type to open the file.\n\nThat made cross-scope resolution possible, which was the actual next step. An import naming a sibling\npackage resolves to a file in a different scope, so it could never resolve from inside the one doing\nthe importing. `ExtractionRequest` now carries the workspace's module set, computed once per revision\nfrom the FILESYSTEM — not from the store, because resolving against what has already been extracted\nwould make an edge depend on the order the scopes happened to run in, the trap the Python extractor\nalready avoids one level down. Null means \"not supplied\", which is not \"there is nothing there\": the\nedge stays Inferred and the disclosure fires.\n\nA test comment had encoded the defective rule approvingly — *\"modules are named relative to it:\n`models`, not `src/models`\"* — which is DC-031's tell in its own words.\n\n**The graph surface is now queryable, not just fetchable.** `GraphQuery` filters by kind, by scope,\nand by whether to include nodes nothing declares. The filter runs BEFORE the cap and degree is\ncounted over what survives it: filtering afterwards would rank and trim the whole graph and only then\ndiscard, so a caller asking for the domain model would receive the wrong 5,000 nodes trimmed to the\nright kind, with nothing in the result saying so. Proven across the daemon as well as in process,\nbecause every cross-boundary defect here so far has been right in process and wrong through the pipe.\n\n**A next step I listed last turn was wrong and is withdrawn.** \"The index still walks every scope\" —\nit does not. `ScopeFingerprints` already skips an unchanged scope and counts it as *reused* rather\nthan *indexed*, deliberately, so \"28 of 28 indexed\" cannot be a true sentence about a run that read\nnothing. I asserted its absence without opening the file. The residual — incrementality BELOW the\nscope, so a one-file edit does not re-walk 1,026 types — is real, but it conflicts with the\nappend-only per-scope snapshot model and is a design decision, not a tidy-up.\n\n717 tests green (App 121, Core 596). Six gates clean. Zero design-owned files.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Carry the five listed next steps: profile the walk, resolve Python imports across scopes, give the graph a query surface, assess incremental re-index, settle merge-protocol item 4",
      "done_when": "Each step is done or its premise disproved in writing; full gate green; committed, merged, published",
      "change": "cl-0079",
      "git": {
        "sha": "acce3698269f5a36ad113b845875f5f19a051cac",
        "short": "acce36982",
        "branch": "session/phase3-pane-probes",
        "pushed": true
      }
    },
    {
      "id": "al-0118",
      "shortname": "fix-terminal-tab-close-and-menu",
      "datetime": "2026-08-30T15:33:24Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "user testing: cant change name/colour of a terminal tab; closing a terminal (X) does nothing; new agent terminal seems redundant; prompt dispatch needs a prompt editor and target terminal",
      "summary": "Fixed dead tab-close (DC-033: DockRoundedTabs close button was Command={x:Null}) by routing through the model via a RelayCommand on the button, AvalonDock DocumentClosing, and a Close context-menu item. Made rename/colour/scheme reachable from the TAB (adapter DecorateTabs attaches TerminalSurface.CreateContextMenu). Added 5 customization tests (App 128). Confirmed PromptBar dispatch exists (Ctrl+K,P -> focused terminal).",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/WorkbenchAdapter.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0119",
      "shortname": "terminal-persistence-and-busy-close",
      "datetime": "2026-08-30T15:46:52Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "do all of these next steps, dont defer the rail polish",
      "summary": "Cross-restart persistence: TerminalCustomizationStore JSON sidecar keyed by stable SurfaceId; TerminalSurface exposes SurfaceId + CustomizationChanged; shell loads/applies once per surface and saves on change. Busy-close confirmation: closing a Busy terminal prompts first. +3 store tests (App 131).",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/TerminalCustomizationStore.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0120",
      "shortname": "depends-on-split-paths-generated-types-allocator-gate",
      "datetime": "2026-08-30T15:47:11Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps you have listed\nprovide the standard status and next steps tables afterwards\n\nalso ... on DC-013 should we \"elect\" a single allocator between sessions?",
      "summary": "Two more performance hypotheses died, and the second death is the useful one.\n\nSplitting `depends_on`: **`SymbolEqualityComparer` dedupe is 3ms of 600ms â€” 0.5%** across 13,906 raw\nsymbols. Gather is 597ms. So the cost of `depends_on` IS the semantic work â€” `GetMembers()` binding\nevery member signature â€” and it cannot be made cheaper, only avoided. Two turns, two suspects\n(`ToDisplayString` at 3.9%, then the comparer at 0.5%), both wrong, both named before being believed.\nThe measurement is now fine-grained enough that the next guess has nowhere to hide.\n\n**Generated types are excluded from the whole extractor**, not just the fluent scan â€” a type is\ndropped only when EVERY file declaring it is auto-generated, so a partial class with a hand-written\nhalf (a WPF window, an EF migration people edit) is kept. Disclosed with a count.\n\n**And the estimate that motivated it was wrong.** I predicted ~600 excluded types on TheTerrace. The\nreal number is **1**. EF marks only the snapshot half (`*.Designer.cs`) as generated; the migration\nhalf starts with a plain `using System;`, so each migration type has a hand-written declaration and\nis correctly kept. The rule is right and its effect here is small â€” recorded that way rather than\nquietly dropped, because \"I expected 600 and got 1\" is the part worth knowing.\n\n**Routes are answerable now.** `GraphPaths` returns how one node reaches another: shortest routes\nonly, directed (walking an edge backwards would answer \"these are related\" while looking like \"a\nchange here reaches there\"), with the route's status taken from its WEAKEST edge so one inferred link\ncannot launder itself into a verified chain. A missing endpoint returns a reason, not an empty list\nthat reads as \"unconnected\". Proven across the daemon pipe as well as in process â€” a path is the one\nresult shaped as a list of lists, so it is the one most likely to arrive flattened.\n\n**The id-allocator gap is closed, and closing it found two more allocators.**\n`tools/verify-id-allocators.py` asks the generalising question behind DC-013 â€” *what else here is\nnumbered by reading the highest and adding one?* â€” as a check rather than a note. It guards five\nfamilies in one place (adding one is a line, not a script) and reports any UNDECLARED sequence, so\nthe next allocator is guarded on the day it is invented. Observed failing on both shapes before it\nwas believed.\n\nOn its first run it found `adr-` (16 decisions, allocated by FILENAME â€” a kind the first draft could\nnot even express) and, on inspection, `INV-`, which was below the threshold at two entries and was\ndeclared anyway.\n\nTwo first-draft errors are recorded in DC-013 because they are the interesting part. It reported\neighteen holes in the audit log as failures â€” but a hole is the merge protocol working: a contested\nid is resolved by re-issuing the loser, leaving the number unused. Verified with `git log -S`: none\nof the missing ids has ever existed, so nothing was lost. **A control that flags the fix as the\ndefect is how a control teaches people to ignore it**, so contiguity is opt-in per family now. It\nalso first read ADR ids out of `architecture.md`, which only cites them â€” an allocator is where an id\nis CREATED, never where it is mentioned.\n\n**Electing a single allocator between sessions was considered and rejected** (the user asked). The\nsessions work in separate worktrees on purpose and an election needs a rendezvous they do not have:\na session an hour into its work has not fetched, so \"ask the allocator\" is stale or a blocking round\ntrip through `main`, and it would make one session wait on another to record a lesson. The class is\nnot \"the wrong allocator won\", it is \"a shared sequence with two writers\".\n\n**Sub-scope incrementality is written up, not built.** `docs/notes/note-20260830-sub-scope-\nincrementality.md` states four options with what each costs, recommends one (keep the atomic\ngeneration, skip the walk for unchanged types) and explicitly does not take it: the per-scope and\nper-parse layers already exist, and nobody has measured whether edit-to-graph latency is a 1.2s\non-demand cost or a per-keystroke one. Instrumenting that is cheaper than any of the four options and\nshould come first. The decision changes the store's contract, so it is the user's call.\n\nA build gap worth naming: the spike project is not in `AiDe.sln`, so last turn's solution build never\ncompiled it and the first measurement this turn silently ran a **stale binary**. Caught because the\nnew timing fields did not appear in the output.\n\n730 tests green (App 123, Core 607). Six gates clean, one of them new. Zero design-owned files.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Carry the five listed next steps and answer whether DC-013 wants an elected allocator",
      "done_when": "Each step done or its premise disproved in writing; the allocator question answered with a recommendation; full gate green; committed, merged, published",
      "change": "cl-0080",
      "git": {
        "sha": "2f80b0b780a0e37a246006b072b851e98e2c33b2",
        "short": "2f80b0b78",
        "branch": "session/phase3-pane-probes",
        "pushed": true
      }
    },
    {
      "id": "al-0234",
      "shortname": "depends-on-split-paths-generated-types-allocator-gate",
      "datetime": "2026-08-30T15:47:11Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps you have listed\nprovide the standard status and next steps tables afterwards\n\nalso ... on DC-013 should we \"elect\" a single allocator between sessions?",
      "summary": "Two more performance hypotheses died, and the second death is the useful one.\n\nSplitting `depends_on`: **`SymbolEqualityComparer` dedupe is 3ms of 600ms — 0.5%** across 13,906 raw\nsymbols. Gather is 597ms. So the cost of `depends_on` IS the semantic work — `GetMembers()` binding\nevery member signature — and it cannot be made cheaper, only avoided. Two turns, two suspects\n(`ToDisplayString` at 3.9%, then the comparer at 0.5%), both wrong, both named before being believed.\nThe measurement is now fine-grained enough that the next guess has nowhere to hide.\n\n**Generated types are excluded from the whole extractor**, not just the fluent scan — a type is\ndropped only when EVERY file declaring it is auto-generated, so a partial class with a hand-written\nhalf (a WPF window, an EF migration people edit) is kept. Disclosed with a count.\n\n**And the estimate that motivated it was wrong.** I predicted ~600 excluded types on TheTerrace. The\nreal number is **1**. EF marks only the snapshot half (`*.Designer.cs`) as generated; the migration\nhalf starts with a plain `using System;`, so each migration type has a hand-written declaration and\nis correctly kept. The rule is right and its effect here is small — recorded that way rather than\nquietly dropped, because \"I expected 600 and got 1\" is the part worth knowing.\n\n**Routes are answerable now.** `GraphPaths` returns how one node reaches another: shortest routes\nonly, directed (walking an edge backwards would answer \"these are related\" while looking like \"a\nchange here reaches there\"), with the route's status taken from its WEAKEST edge so one inferred link\ncannot launder itself into a verified chain. A missing endpoint returns a reason, not an empty list\nthat reads as \"unconnected\". Proven across the daemon pipe as well as in process — a path is the one\nresult shaped as a list of lists, so it is the one most likely to arrive flattened.\n\n**The id-allocator gap is closed, and closing it found two more allocators.**\n`tools/verify-id-allocators.py` asks the generalising question behind DC-013 — *what else here is\nnumbered by reading the highest and adding one?* — as a check rather than a note. It guards five\nfamilies in one place (adding one is a line, not a script) and reports any UNDECLARED sequence, so\nthe next allocator is guarded on the day it is invented. Observed failing on both shapes before it\nwas believed.\n\nOn its first run it found `adr-` (16 decisions, allocated by FILENAME — a kind the first draft could\nnot even express) and, on inspection, `INV-`, which was below the threshold at two entries and was\ndeclared anyway.\n\nTwo first-draft errors are recorded in DC-013 because they are the interesting part. It reported\neighteen holes in the audit log as failures — but a hole is the merge protocol working: a contested\nid is resolved by re-issuing the loser, leaving the number unused. Verified with `git log -S`: none\nof the missing ids has ever existed, so nothing was lost. **A control that flags the fix as the\ndefect is how a control teaches people to ignore it**, so contiguity is opt-in per family now. It\nalso first read ADR ids out of `architecture.md`, which only cites them — an allocator is where an id\nis CREATED, never where it is mentioned.\n\n**Electing a single allocator between sessions was considered and rejected** (the user asked). The\nsessions work in separate worktrees on purpose and an election needs a rendezvous they do not have:\na session an hour into its work has not fetched, so \"ask the allocator\" is stale or a blocking round\ntrip through `main`, and it would make one session wait on another to record a lesson. The class is\nnot \"the wrong allocator won\", it is \"a shared sequence with two writers\".\n\n**Sub-scope incrementality is written up, not built.** `docs/notes/note-20260830-sub-scope-\nincrementality.md` states four options with what each costs, recommends one (keep the atomic\ngeneration, skip the walk for unchanged types) and explicitly does not take it: the per-scope and\nper-parse layers already exist, and nobody has measured whether edit-to-graph latency is a 1.2s\non-demand cost or a per-keystroke one. Instrumenting that is cheaper than any of the four options and\nshould come first. The decision changes the store's contract, so it is the user's call.\n\nA build gap worth naming: the spike project is not in `AiDe.sln`, so last turn's solution build never\ncompiled it and the first measurement this turn silently ran a **stale binary**. Caught because the\nnew timing fields did not appear in the output.\n\n730 tests green (App 123, Core 607). Six gates clean, one of them new. Zero design-owned files.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Carry the five listed next steps and answer whether DC-013 wants an elected allocator",
      "done_when": "Each step done or its premise disproved in writing; the allocator question answered with a recommendation; full gate green; committed, merged, published",
      "change": "cl-0080",
      "git": {
        "sha": "2f80b0b780a0e37a246006b072b851e98e2c33b2",
        "short": "2f80b0b78",
        "branch": "session/phase3-pane-probes",
        "pushed": true
      }
    },
    {
      "id": "al-0121",
      "shortname": "investigate-graph-ipc-scaling",
      "datetime": "2026-08-30T16:02:39Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "graph view error on TheTerrace: ipc.transport_closed the daemon closed the connection without responding; think through scaling at the UX side given extremely large projects",
      "summary": "Root cause: whole-graph response exceeds the 1 MiB IPC frame cap; daemon's ServeAsync doesn't catch the oversized-write ArgumentException so it closes the connection. Deeper finding: the whole-graph default violates the spec's US-K2 (never render whole graph) and does not scale. Added US-K10-12 (aggregated overview default, semantic zoom/LOD, transport-bounded queries). DC-035. Core-owned fix handed off (4c). INV-0003. STOP.",
      "kind": "skill",
      "skill": "investigate",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/investigations/INV-0003-graph-exceeds-ipc-frame-cap.md"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0122",
      "shortname": "refresh-metrics-project-coverage-dc033-sweep-routes",
      "datetime": "2026-08-30T16:05:38Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps you have listed\nprovide the standard status and next steps tables afterwards",
      "summary": "**The measurement the sub-scope decision was blocked on now exists.** A refresh had a span but its\nSTATUS carried no duration, so the only thing a caller could read was how many assertions came back.\n`ScopeRefreshStatus` now carries `QueuedMilliseconds` and `DurationMilliseconds` â€” kept separate\nbecause waiting is a concurrency problem and running is a cost problem, and one number hides which\none a user is feeling. `RefreshMetrics` adds p50/p95/max over a bounded window plus first/last\nobserved, exposed over IPC as `refresh.metrics` with no request body and no failure mode: an operator\nasking what re-indexing costs should never be told it depends on what they enable first.\n\nDeliberately **no rate is computed**. \"Refreshes per hour\" from two samples is a number with no error\nbar that gets quoted as if it had one; the raw facts let a reader compute it when there is enough of\nit to mean something and notice when there is not. Failed refreshes are timed too â€” a run that takes\ntwenty seconds and then throws is the one an operator most wants to see, and excluding failures is\nhow a percentile ends up describing only the easy cases. With nothing measured everything reports\nzero and null rather than an interpolation of nothing.\n\n**No tracked project escapes compilation now.** `spikes/joins-on-a-real-repo` â€” the harness behind\nnearly every performance and join number in this log â€” is not in `AiDe.sln`, so a change to\n`GraphAsync` broke it while the solution build stayed green, and the next measurement silently ran a\nSTALE BINARY. Caught only because an expected timing field did not appear. Had the change not added a\nfield, the numbers would have looked fine and been wrong. That is DC-023 with a twist: the gate was\nnot stale, it was NARROW.\n\n`tools/verify-project-coverage.py` builds every tracked project the solution does not contain. A\ngate rather than solution membership, because `AiDe.sln` answers \"what ships\" and these are evidence\nartifacts. **The cost claim in its own header was wrong and is corrected in place:** it was written\nexpecting to belong in the slow at-readiness ring and measures **16 seconds for 19 projects**, which\nputs it on every push. It prints its wall time on every run so that stays a measured decision.\nObserved failing against a deliberately broken spike.\n\n**DC-033 swept, and it found a second instance.** The class says its signature is a ratio on real\ninput, so the sweep measured. `TypeScriptExtractor` knew `class|interface|type|enum|function|const`\nand did not know `async`, the generator star, `namespace`, `let` or `var`. TheTerrace declares 124\n`export interface`, 26 `export type`, 16 `export const` and **4 `export namespace`** â€” four\ndeclarations reported as absent rather than unread. `PythonExtractor` is clean; the Bicep and schema\nreaders are narrow by *declared* ceiling and disclose it.\n\nThe important half is not the wider pattern, which will be wrong again for the next spelling. **The\nreader now counts its own misses and discloses them** â€” `typescript-exports-not-recognised (N)` â€” so\nthe next unanticipated form announces itself on the scope. Re-exports are excluded deliberately:\ncounting them gives a miss rate that never reaches zero and therefore says nothing.\n\n**Routes reach a surface.** `CanvasGraphViewModel.RouteAsync(from, to)` returns the **same\n`CanvasGraph`** the canvas already binds, because a route IS a subgraph and giving it its own shape\nwould mean a second renderer and a second place for the two sessions to disagree about what a node\nlooks like. Endpoints arrive with `IsRoot = true`; the caption names the count, the shortest length\nand **the weakest link**, because a route drawn without that looks like a fact about the code when\none inferred edge makes the whole claim inferred. Every empty case says which one it is, and the\nprojection's own reason survives to the caption â€” \"not in this graph\" and \"no route within 8 edges\"\nsend a user to different places.\n\n**The `GraphQuery` filter question is put to the design session as a decision, not a request.** All\nthree filters are on the wire and proven across the daemon; which of them belong in the UI, and as\nwhat control, is an information-architecture call this session does not own. Â§4a states the three\nwith what each is worth, recommends a **preset** over toggles (three named views are one decision a\nuser makes once; three toggles are a combinatorial space they must reason about) and says plainly\nthat this is a guess about users. What Core commits to either way is the tested property: the filter\nruns before the cap and degree is computed over what survives it.\n\n765 tests green (App 131, Core 634). Seven gates clean, one of them new.\nZero design-owned files.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Carry the five listed next steps: measure edit-to-graph, close the uncompiled-project gap, expose routes on a surface, sweep extractors for DC-033, put the GraphQuery filter question to the design session",
      "done_when": "Each step done or its premise disproved in writing; full gate green; committed, merged, published",
      "change": "cl-0081",
      "git": {
        "sha": "1885098af60677e9d066a5f234d55baa9719bc9b",
        "short": "1885098af",
        "branch": "session/phase3-pane-probes",
        "pushed": true
      }
    },
    {
      "id": "al-0235",
      "shortname": "refresh-metrics-project-coverage-dc033-sweep-routes",
      "datetime": "2026-08-30T16:05:38Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps you have listed\nprovide the standard status and next steps tables afterwards",
      "summary": "**The measurement the sub-scope decision was blocked on now exists.** A refresh had a span but its\nSTATUS carried no duration, so the only thing a caller could read was how many assertions came back.\n`ScopeRefreshStatus` now carries `QueuedMilliseconds` and `DurationMilliseconds` — kept separate\nbecause waiting is a concurrency problem and running is a cost problem, and one number hides which\none a user is feeling. `RefreshMetrics` adds p50/p95/max over a bounded window plus first/last\nobserved, exposed over IPC as `refresh.metrics` with no request body and no failure mode: an operator\nasking what re-indexing costs should never be told it depends on what they enable first.\n\nDeliberately **no rate is computed**. \"Refreshes per hour\" from two samples is a number with no error\nbar that gets quoted as if it had one; the raw facts let a reader compute it when there is enough of\nit to mean something and notice when there is not. Failed refreshes are timed too — a run that takes\ntwenty seconds and then throws is the one an operator most wants to see, and excluding failures is\nhow a percentile ends up describing only the easy cases. With nothing measured everything reports\nzero and null rather than an interpolation of nothing.\n\n**No tracked project escapes compilation now.** `spikes/joins-on-a-real-repo` — the harness behind\nnearly every performance and join number in this log — is not in `AiDe.sln`, so a change to\n`GraphAsync` broke it while the solution build stayed green, and the next measurement silently ran a\nSTALE BINARY. Caught only because an expected timing field did not appear. Had the change not added a\nfield, the numbers would have looked fine and been wrong. That is DC-023 with a twist: the gate was\nnot stale, it was NARROW.\n\n`tools/verify-project-coverage.py` builds every tracked project the solution does not contain. A\ngate rather than solution membership, because `AiDe.sln` answers \"what ships\" and these are evidence\nartifacts. **The cost claim in its own header was wrong and is corrected in place:** it was written\nexpecting to belong in the slow at-readiness ring and measures **16 seconds for 19 projects**, which\nputs it on every push. It prints its wall time on every run so that stays a measured decision.\nObserved failing against a deliberately broken spike.\n\n**DC-033 swept, and it found a second instance.** The class says its signature is a ratio on real\ninput, so the sweep measured. `TypeScriptExtractor` knew `class|interface|type|enum|function|const`\nand did not know `async`, the generator star, `namespace`, `let` or `var`. TheTerrace declares 124\n`export interface`, 26 `export type`, 16 `export const` and **4 `export namespace`** — four\ndeclarations reported as absent rather than unread. `PythonExtractor` is clean; the Bicep and schema\nreaders are narrow by *declared* ceiling and disclose it.\n\nThe important half is not the wider pattern, which will be wrong again for the next spelling. **The\nreader now counts its own misses and discloses them** — `typescript-exports-not-recognised (N)` — so\nthe next unanticipated form announces itself on the scope. Re-exports are excluded deliberately:\ncounting them gives a miss rate that never reaches zero and therefore says nothing.\n\n**Routes reach a surface.** `CanvasGraphViewModel.RouteAsync(from, to)` returns the **same\n`CanvasGraph`** the canvas already binds, because a route IS a subgraph and giving it its own shape\nwould mean a second renderer and a second place for the two sessions to disagree about what a node\nlooks like. Endpoints arrive with `IsRoot = true`; the caption names the count, the shortest length\nand **the weakest link**, because a route drawn without that looks like a fact about the code when\none inferred edge makes the whole claim inferred. Every empty case says which one it is, and the\nprojection's own reason survives to the caption — \"not in this graph\" and \"no route within 8 edges\"\nsend a user to different places.\n\n**The `GraphQuery` filter question is put to the design session as a decision, not a request.** All\nthree filters are on the wire and proven across the daemon; which of them belong in the UI, and as\nwhat control, is an information-architecture call this session does not own. §4a states the three\nwith what each is worth, recommends a **preset** over toggles (three named views are one decision a\nuser makes once; three toggles are a combinatorial space they must reason about) and says plainly\nthat this is a guess about users. What Core commits to either way is the tested property: the filter\nruns before the cap and degree is computed over what survives it.\n\n765 tests green (App 131, Core 634). Seven gates clean, one of them new.\nZero design-owned files.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Carry the five listed next steps: measure edit-to-graph, close the uncompiled-project gap, expose routes on a surface, sweep extractors for DC-033, put the GraphQuery filter question to the design session",
      "done_when": "Each step done or its premise disproved in writing; full gate green; committed, merged, published",
      "change": "cl-0081",
      "git": {
        "sha": "1885098af60677e9d066a5f234d55baa9719bc9b",
        "short": "1885098af",
        "branch": "session/phase3-pane-probes",
        "pushed": true
      }
    },
    {
      "id": "al-0123",
      "shortname": "inv-0003-bounded-default-and-payload-too-large",
      "datetime": "2026-08-30T16:18:29Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps you have listed\nprovide the standard status and next steps tables afterwards",
      "summary": "The graph pane has been broken on TheTerrace since the whole-graph change, and it is my defect. The\ndesign session investigated it (INV-0003), verified the cause and handed it to Core.\n\nMEASURED: the whole-graph response is 1,522,284 bytes against a 1,048,576-byte IPC frame. It could\nnever have been delivered. `IpcFraming.WriteAsync` threw, the serve loop caught `IOException` and\n`OperationCanceledException` but not that, the exception escaped, the connection closed with no\nreply â€” so \"the answer is too big to send\" reached the user as \"the daemon closed the connection\nwithout responding\", which sends them to look at the daemon.\n\nTwo fixes, both Core's:\n\n**The transport no longer closes silently.** `IpcServer.Respond` checks the encoded size BEFORE\nwriting and returns a new `ipc.payload_too_large` carrying the actual and permitted byte counts.\nChecked before rather than caught after on purpose: a partially written frame leaves the peer reading\na length prefix whose body never arrives, which is a hang rather than an error. The writer's own throw\nis correct and stays.\n\n**The default view is bounded by construction.** No focus now asks for 1,500 declared nodes ranked by\ndegree rather than the whole graph â€” 533,495 bytes, fits, 618 omitted and named in the caption.\n\nThe transport failure exposed the larger error, and the design session is right about it: the fix for\nDC-031 over-corrected. The graph pane rendering two nodes of two thousand was a real defect; loading\nEVERYTHING overshot the spec that was being restored â€” `knowledge-exploration.md` US-K2 already said\nthe whole graph is never rendered at once. The answer to \"one arbitrary alphabetical node\" was a\nbounded overview of MEANINGFUL nodes. DC-031 and DC-035 are the same axis overshot in opposite\ndirections, and a fix for one lands on the other unless the spec is re-read when the new default is\nchosen. That is recorded on DC-035 as mine.\n\nBounding by size and bounding by meaning turned out to be the same change: measured, the six\nmost-connected nodes of a real repository were `string`, `int`, `Task<T>`, `DateTimeOffset`,\n`IReadOnlyList<T>` and `Guid`, so a first view centred on the BCL was unreadable as well as\nundeliverable.\n\nDC-035 moves from `uncontrolled` to `partially-controlled`. Core's half is landed and tested,\nincluding the byte measurements pinned as an assertion so that raising the frame cap without\nrevisiting the default fails there first. **Design's half is open and named:** the aggregated /\nlevel-of-detail overview for a graph too large to show node-by-node, and the \"narrow your focus\"\nstate. Until that exists a repository whose declared code exceeds the cap gets a truthful truncation\nrather than a designed overview.\n\n775 tests green (App 131, Core 639). Seven gates clean. Zero design-owned files.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Fix INV-0003, the graph regression my own whole-graph change caused",
      "done_when": "The transport returns a legible error, the default view is bounded and proven to fit, DC-035 updated, gates green, merged and published",
      "git": {
        "sha": "bbeeb9f832efa1a06e83c1e9df15b0a9bc5cac32",
        "short": "bbeeb9f83",
        "branch": "session/phase3-pane-probes",
        "pushed": true
      }
    },
    {
      "id": "al-0236",
      "shortname": "inv-0003-bounded-default-and-payload-too-large",
      "datetime": "2026-08-30T16:18:29Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps you have listed\nprovide the standard status and next steps tables afterwards",
      "summary": "The graph pane has been broken on TheTerrace since the whole-graph change, and it is my defect. The\ndesign session investigated it (INV-0003), verified the cause and handed it to Core.\n\nMEASURED: the whole-graph response is 1,522,284 bytes against a 1,048,576-byte IPC frame. It could\nnever have been delivered. `IpcFraming.WriteAsync` threw, the serve loop caught `IOException` and\n`OperationCanceledException` but not that, the exception escaped, the connection closed with no\nreply — so \"the answer is too big to send\" reached the user as \"the daemon closed the connection\nwithout responding\", which sends them to look at the daemon.\n\nTwo fixes, both Core's:\n\n**The transport no longer closes silently.** `IpcServer.Respond` checks the encoded size BEFORE\nwriting and returns a new `ipc.payload_too_large` carrying the actual and permitted byte counts.\nChecked before rather than caught after on purpose: a partially written frame leaves the peer reading\na length prefix whose body never arrives, which is a hang rather than an error. The writer's own throw\nis correct and stays.\n\n**The default view is bounded by construction.** No focus now asks for 1,500 declared nodes ranked by\ndegree rather than the whole graph — 533,495 bytes, fits, 618 omitted and named in the caption.\n\nThe transport failure exposed the larger error, and the design session is right about it: the fix for\nDC-031 over-corrected. The graph pane rendering two nodes of two thousand was a real defect; loading\nEVERYTHING overshot the spec that was being restored — `knowledge-exploration.md` US-K2 already said\nthe whole graph is never rendered at once. The answer to \"one arbitrary alphabetical node\" was a\nbounded overview of MEANINGFUL nodes. DC-031 and DC-035 are the same axis overshot in opposite\ndirections, and a fix for one lands on the other unless the spec is re-read when the new default is\nchosen. That is recorded on DC-035 as mine.\n\nBounding by size and bounding by meaning turned out to be the same change: measured, the six\nmost-connected nodes of a real repository were `string`, `int`, `Task<T>`, `DateTimeOffset`,\n`IReadOnlyList<T>` and `Guid`, so a first view centred on the BCL was unreadable as well as\nundeliverable.\n\nDC-035 moves from `uncontrolled` to `partially-controlled`. Core's half is landed and tested,\nincluding the byte measurements pinned as an assertion so that raising the frame cap without\nrevisiting the default fails there first. **Design's half is open and named:** the aggregated /\nlevel-of-detail overview for a graph too large to show node-by-node, and the \"narrow your focus\"\nstate. Until that exists a repository whose declared code exceeds the cap gets a truthful truncation\nrather than a designed overview.\n\n775 tests green (App 131, Core 639). Seven gates clean. Zero design-owned files.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Fix INV-0003, the graph regression my own whole-graph change caused",
      "done_when": "The transport returns a legible error, the default view is bounded and proven to fit, DC-035 updated, gates green, merged and published",
      "git": {
        "sha": "bbeeb9f832efa1a06e83c1e9df15b0a9bc5cac32",
        "short": "bbeeb9f83",
        "branch": "session/phase3-pane-probes",
        "pushed": true
      }
    },
    {
      "id": "al-0124",
      "shortname": "implement-graph-scaling-interim",
      "datetime": "2026-08-30T16:24:23Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "do all of these next steps",
      "summary": "Implemented the two unblocking graph-scaling fixes (INV-0003, no active Core claims): default view requests a bounded ranked overview (WholeGraphNodeCap 5000->750, US-K10) instead of the whole graph; daemon returns IpcErrorCodes.PayloadTooLarge via IpcServer.SerializeWithinBudget instead of closing on an oversized frame (US-K12). +3 GraphScalingTests. Core 637, App 131. Aggregated/LOD overview remains Core.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.Core/Presentation/CanvasGraphViewModel.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0125",
      "shortname": "byte-bounded-responses-and-incrementality-resolved",
      "datetime": "2026-08-30T16:40:09Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "graph loads... now we have UX layout and scaling issues which I will deal with in the other session\n----\ndo the next steps you have here",
      "summary": "The graph was found by a user opening a repository, not by us, so every other read operation was\nmeasured at its own ceiling against the 1 MiB frame. **Two more were one repository away from\nINV-0003, and a third could never have succeeded.**\n\n| operation | before | after |\n|---|---|---|\n| evidence (2,000/page) | 1,004,397 B â€” **95.8% of the frame** | 659,164 B, 1,310 rows, cursor continues |\n| graph (5,000-node ceiling) | 1,522,915 B â€” **overflows** | 474,437 B, shrunk to fit, `Omitted` reports |\n| find (20,000 ceiling) | 461,750 B returned while **reporting a 64 KiB cap** | byte-bounded, `ByteCapped` set |\n\nThe class behind all three: **every ceiling in the read surface counts ITEMS and the transport limit\nis in BYTES**, and every item's size comes from repository content. A count-only cap admits an\nunbounded payload. `Evidence`'s own documentation claimed a page \"can cross a pipe without breaching\nthe result-byte cap\" â€” it was fifteen times over that cap. `Find` declared `MaxBytes: 65,536` beside\n461,750 bytes of results, a control that could not fire (DC-016).\n\nTruncating an evidence page is **lossless**, which is why the bound belongs in the projection: the\ncursor continues from the last row actually returned, so it costs a round trip and never a row. A\ntest pages a corpus to exhaustion and asserts every assertion comes back exactly once. Every response\nkeeps at least one item â€” a caller that receives nothing because its first row is enormous can never\nmake progress.\n\nThe byte guard uses a **measured** constant, not a guessed one: 2,000 assertions whose fields total\n238,002 bytes serialise to 1,004,397, so JSON scaffolding is **383 bytes per row**. Rounded up,\nbecause a guard that under-counts is a guard that lets the frame overflow.\n\n**The sub-scope-incrementality decision is resolved, and not by the instrument built for it.** The\nnote said the answer depended on how often edit-to-graph happens, and `RefreshMetrics` was built to\nmeasure that. One grep answered it instead: **there is no `FileSystemWatcher` anywhere in `src/`**.\nIndexing runs only from explicit commands, so the cost is paid deliberately by a user pressing a\nbutton. Breaking the per-scope snapshot's atomicity to shorten a deliberate 1.2s action is a poor\ntrade, so none of options Aâ€“D is built. The note stays, status `resolved`, naming the trigger that\nwould change it: **re-index on save**, which converts a bounded cost into a per-edit one.\n\nWorth recording because it nearly went the other way. Three hypotheses in this area have now been\nwrong â€” `ToDisplayString` (3.9%), `SymbolEqualityComparer` (0.5%), \"the index still walks every\nscope\" (it does not). This would have been the fourth: collecting a distribution and reasoning\ncarefully about a number that answered a question nobody was asking.\n\n**My own new gate cried wolf and is fixed.** `verify-id-allocators` reported an undeclared family\ncalled `nDC-` â€” it was matching the `n` of a JSON-escaped newline before a *mentioned* `DC-` id in a\nlog summary. A mention is not an allocation, which is the same distinction the first draft got wrong\nwhen it read ADR ids out of `architecture.md`. Fixed with a lookbehind, and the narrative files are\nexcluded from the candidate scan: a record of a mention must never nominate an allocator. This is the\nsecond time this script has flagged correct behaviour as a defect; a control that does that twice is\none people switch off.\n\n790 tests green (App 131, Core 645). Seven gates clean. Zero design-owned files.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Audit every IPC response for the INV-0003 overflow, bound them, and settle the sub-scope incrementality question",
      "done_when": "Every operation fits the frame with tests; the decision note is resolved with its trigger named; gates green; merged and published",
      "change": "cl-0083",
      "git": {
        "sha": "1c273b0b29bf76e7fc453e5f77f733ec512f19e8",
        "short": "1c273b0b2",
        "branch": "session/phase3-pane-probes",
        "pushed": true
      }
    },
    {
      "id": "al-0237",
      "shortname": "byte-bounded-responses-and-incrementality-resolved",
      "datetime": "2026-08-30T16:40:09Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "graph loads... now we have UX layout and scaling issues which I will deal with in the other session\n----\ndo the next steps you have here",
      "summary": "The graph was found by a user opening a repository, not by us, so every other read operation was\nmeasured at its own ceiling against the 1 MiB frame. **Two more were one repository away from\nINV-0003, and a third could never have succeeded.**\n\n| operation | before | after |\n|---|---|---|\n| evidence (2,000/page) | 1,004,397 B — **95.8% of the frame** | 659,164 B, 1,310 rows, cursor continues |\n| graph (5,000-node ceiling) | 1,522,915 B — **overflows** | 474,437 B, shrunk to fit, `Omitted` reports |\n| find (20,000 ceiling) | 461,750 B returned while **reporting a 64 KiB cap** | byte-bounded, `ByteCapped` set |\n\nThe class behind all three: **every ceiling in the read surface counts ITEMS and the transport limit\nis in BYTES**, and every item's size comes from repository content. A count-only cap admits an\nunbounded payload. `Evidence`'s own documentation claimed a page \"can cross a pipe without breaching\nthe result-byte cap\" — it was fifteen times over that cap. `Find` declared `MaxBytes: 65,536` beside\n461,750 bytes of results, a control that could not fire (DC-016).\n\nTruncating an evidence page is **lossless**, which is why the bound belongs in the projection: the\ncursor continues from the last row actually returned, so it costs a round trip and never a row. A\ntest pages a corpus to exhaustion and asserts every assertion comes back exactly once. Every response\nkeeps at least one item — a caller that receives nothing because its first row is enormous can never\nmake progress.\n\nThe byte guard uses a **measured** constant, not a guessed one: 2,000 assertions whose fields total\n238,002 bytes serialise to 1,004,397, so JSON scaffolding is **383 bytes per row**. Rounded up,\nbecause a guard that under-counts is a guard that lets the frame overflow.\n\n**The sub-scope-incrementality decision is resolved, and not by the instrument built for it.** The\nnote said the answer depended on how often edit-to-graph happens, and `RefreshMetrics` was built to\nmeasure that. One grep answered it instead: **there is no `FileSystemWatcher` anywhere in `src/`**.\nIndexing runs only from explicit commands, so the cost is paid deliberately by a user pressing a\nbutton. Breaking the per-scope snapshot's atomicity to shorten a deliberate 1.2s action is a poor\ntrade, so none of options A–D is built. The note stays, status `resolved`, naming the trigger that\nwould change it: **re-index on save**, which converts a bounded cost into a per-edit one.\n\nWorth recording because it nearly went the other way. Three hypotheses in this area have now been\nwrong — `ToDisplayString` (3.9%), `SymbolEqualityComparer` (0.5%), \"the index still walks every\nscope\" (it does not). This would have been the fourth: collecting a distribution and reasoning\ncarefully about a number that answered a question nobody was asking.\n\n**My own new gate cried wolf and is fixed.** `verify-id-allocators` reported an undeclared family\ncalled `nDC-` — it was matching the `n` of a JSON-escaped newline before a *mentioned* `DC-` id in a\nlog summary. A mention is not an allocation, which is the same distinction the first draft got wrong\nwhen it read ADR ids out of `architecture.md`. Fixed with a lookbehind, and the narrative files are\nexcluded from the candidate scan: a record of a mention must never nominate an allocator. This is the\nsecond time this script has flagged correct behaviour as a defect; a control that does that twice is\none people switch off.\n\n790 tests green (App 131, Core 645). Seven gates clean. Zero design-owned files.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Audit every IPC response for the INV-0003 overflow, bound them, and settle the sub-scope incrementality question",
      "done_when": "Every operation fits the frame with tests; the decision note is resolved with its trigger named; gates green; merged and published",
      "change": "cl-0083",
      "git": {
        "sha": "1c273b0b29bf76e7fc453e5f77f733ec512f19e8",
        "short": "1c273b0b2",
        "branch": "session/phase3-pane-probes",
        "pushed": true
      }
    },
    {
      "id": "al-0126",
      "shortname": "ui-design-graph-canvas",
      "datetime": "2026-08-30T16:42:11Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "/ui-design review the screen shot and consult your knowledge on graph visualization; make sure the ux works for a large graph",
      "summary": "Reviewed the graph canvas: single-ring 2D layout piles 50 opaque box-nodes into an unreadable blob; no force-spread/zoom/LOD; disclosure wall. Produced review (rubric+ranked plan), a self-contained force-directed target mockup (dots-not-cards, labels-on-demand, zoom/pan, semantic-zoom clustering, search-first, honest caption, complete states), and DC-036. Highest-leverage fix: replace the ring with a force layout + degree-sized dots. Design-owned rebuild; LOD needs Core community query.",
      "kind": "skill",
      "skill": "ui-design",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/mockups/graph-canvas.html"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0127",
      "shortname": "fix-terminal-pane-resize",
      "datetime": "2026-08-30T16:50:09Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "do the next best actions â€” investigate why the terminal (lower) pane cannot be resized and fix it",
      "summary": "WorkbenchAdapter.BuildPanel dropped SplitNode.Weights, so AvalonDock defaulted every pane to an equal 1* share â€” the terminal pane sat at a fixed unresizable size and split ratios were lost. Applied weights as GridLength(w, Star) on DockWidth/DockHeight; added projection regression test; registered DC-037.",
      "kind": "manual",
      "skill": null,
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/WorkbenchAdapter.cs"
      ],
      "tags": [
        "terminal",
        "layout",
        "defect-fix"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0238",
      "shortname": "fix-terminal-pane-resize",
      "datetime": "2026-08-30T16:50:09Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "do the next best actions — investigate why the terminal (lower) pane cannot be resized and fix it",
      "summary": "WorkbenchAdapter.BuildPanel dropped SplitNode.Weights, so AvalonDock defaulted every pane to an equal 1* share — the terminal pane sat at a fixed unresizable size and split ratios were lost. Applied weights as GridLength(w, Star) on DockWidth/DockHeight; added projection regression test; registered DC-037.",
      "kind": "manual",
      "skill": null,
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/WorkbenchAdapter.cs"
      ],
      "tags": [
        "terminal",
        "layout",
        "defect-fix"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0128",
      "shortname": "implement-graph-canvas-force-layout",
      "datetime": "2026-08-30T17:00:18Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "do the next best actions â€” rebuild the graph canvas 2D layout per the landed review/mockup (DC-036)",
      "summary": "Rebuilt CanvasPage 2D: single-ring -> phyllotaxis + bounded force settle; degree-sized dots with labels-on-demand; edges behind. Preserved ADR-0015 a11y (focusable .node spans, Tab trap, focus.leave) verified by P2-FOCUS-03 probe. App.Tests 132/132.",
      "kind": "skill",
      "skill": "ui-design",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/CanvasPage.cs"
      ],
      "tags": [
        "graph",
        "ui-design",
        "DC-036"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0239",
      "shortname": "implement-graph-canvas-force-layout",
      "datetime": "2026-08-30T17:00:18Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "do the next best actions — rebuild the graph canvas 2D layout per the landed review/mockup (DC-036)",
      "summary": "Rebuilt CanvasPage 2D: single-ring -> phyllotaxis + bounded force settle; degree-sized dots with labels-on-demand; edges behind. Preserved ADR-0015 a11y (focusable .node spans, Tab trap, focus.leave) verified by P2-FOCUS-03 probe. App.Tests 132/132.",
      "kind": "skill",
      "skill": "ui-design",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/CanvasPage.cs"
      ],
      "tags": [
        "graph",
        "ui-design",
        "DC-036"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0129",
      "shortname": "graph-canvas-pan-zoom-fit",
      "datetime": "2026-08-30T17:05:09Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "do the next best actions â€” add 2D pan/zoom/fit to the graph canvas (review ranked plan, US-K11)",
      "summary": "Added 2D drag-to-pan, wheel-zoom (cursor-anchored), and a Fit-to-view control that frames the settled layout on load. 3D drag-to-rotate unchanged; keyboard a11y untouched (P2-FOCUS-03 green). App.Tests 132/132.",
      "kind": "skill",
      "skill": "ui-design",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/CanvasPage.cs"
      ],
      "tags": [
        "graph",
        "ui-design"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0240",
      "shortname": "graph-canvas-pan-zoom-fit",
      "datetime": "2026-08-30T17:05:09Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "do the next best actions — add 2D pan/zoom/fit to the graph canvas (review ranked plan, US-K11)",
      "summary": "Added 2D drag-to-pan, wheel-zoom (cursor-anchored), and a Fit-to-view control that frames the settled layout on load. 3D drag-to-rotate unchanged; keyboard a11y untouched (P2-FOCUS-03 green). App.Tests 132/132.",
      "kind": "skill",
      "skill": "ui-design",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/CanvasPage.cs"
      ],
      "tags": [
        "graph",
        "ui-design"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0130",
      "shortname": "aggregated-overview-and-reflective-frame-gate",
      "datetime": "2026-08-30T17:14:22Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps you have listed\nprovide the standard status and next steps tables afterwards",
      "summary": "**The aggregated overview closes Core's half of DC-035.** The bounded default fixed the transport\nfailure by drawing 1,500 of 2,118 declared nodes and saying so â€” honest, and still a truncation. A\nuser opening a repository wants its SHAPE, and 1,500 dots is not a shape.\n\n`GraphOverview` returns the workspace as GROUPS, grouped by the ids' own hierarchy â€” a C# symbol is\n`TheTerrace.Features.Competitions.Season`, a module is `src/app/models`, so the first `Depth`\nsegments name what a developer would call \"where that lives\", and Depth is the zoom control.\n\nMEASURED on TheTerrace at depth 3: `Features.Fixtures` 117, `Features.Teams` 117, `Features.Matches`\n107, `Infrastructure.Data` 70, `Features.Identity` 47, `Features.Competitions` 44 â€” the actual shape\nof that repository, in **55,758 bytes** against 533,484 for the node graph.\n\n**No community-detection algorithm, deliberately.** Its output is unstable under small changes to the\ngraph, so the same repository would regroup between two indexes and the picture would move for\nreasons the user cannot see. Prefix grouping is stable by construction.\n\nEach group carries `NodeCount` â€” a dot standing for 240 types is only honest while the 240 is on it,\nwhich is the whole difference between an overview and a smaller lie. Each link carries `Weight` for\nthickness and the **weakest** status of the edges it bundles, because drawing a bundle as Verified\nbecause most of its members were would launder guesses into facts at a grain where the user can no\nlonger see the members. Edges inside a group are counted, not drawn.\n\n**My own first default was the failure the overview exists to prevent.** `MaxClusters` shipped at 200,\nand TheTerrace has 689 groups at depth 3 â€” returning 200 of them is a hairball at a coarser grain,\nwhich my own doc comment named as the thing to avoid. Measured and lowered to 60.\n\n**The frame-fit control is reflective, because hand-auditing found the last three and would not find\nthe next.** `EveryOperationFitsTheFrameTests` derives the operation list from `IWorkspaceQueries`\nitself and fails when a method is added with no size check â€” observed failing with an entry removed,\nnaming the method. Writing the list by hand would restate the product's own list (DC-021) and go\nstale in exactly the case that matters.\n\n**The write side was audited and needs nothing.** An `IndexSummary` for 28 scopes is **1,724 bytes** â€”\nthree orders of magnitude below the frame. Recorded rather than guarded: a control whose failure\nnobody can name is one to drop.\n\n**Degree on the wire was already done, by the other session.** They shipped force-directed layout with\ndegree-sized dots (DC-036), so that next step was stale when I wrote it. What layout still could not\ndo is cluster, which is what the overview provides. `GraphOverview.GroupFor` is public now so the\ncanvas groups detail nodes the SAME way the overview does â€” two definitions of one grouping is\nDC-022's shape, and the divergence would render a node in the wrong cluster and look like a layout\nbug.\n\n**A shared test double replaces four hand-written stubs.** Every method added to `IWorkspaceQueries`\nhas broken the same four stubs across two projects â€” four rounds of identical churn, which is a\nmeasured recurrence rather than a predicted one. `FakeWorkspaceQueries` throws rather than returning\nempty: a stub that quietly answers a question the test did not intend to ask turns a wrong call site\ninto a passing test.\n\n791 tests green (App 132, Core 659). Seven gates clean. Zero design-owned files.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Build the server-side aggregated overview, gate every operation against the frame, and audit the write side",
      "done_when": "The overview is measured on a real repository and on the wire; a reflective gate catches an unweighed operation; the write side measured; gates green; merged and published",
      "change": "cl-0084",
      "git": {
        "sha": "13a488b5028bc352171f202df353915e0f791ac2",
        "short": "13a488b50",
        "branch": "session/phase3-pane-probes",
        "pushed": false
      }
    },
    {
      "id": "al-0241",
      "shortname": "aggregated-overview-and-reflective-frame-gate",
      "datetime": "2026-08-30T17:14:22Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps you have listed\nprovide the standard status and next steps tables afterwards",
      "summary": "**The aggregated overview closes Core's half of DC-035.** The bounded default fixed the transport\nfailure by drawing 1,500 of 2,118 declared nodes and saying so — honest, and still a truncation. A\nuser opening a repository wants its SHAPE, and 1,500 dots is not a shape.\n\n`GraphOverview` returns the workspace as GROUPS, grouped by the ids' own hierarchy — a C# symbol is\n`TheTerrace.Features.Competitions.Season`, a module is `src/app/models`, so the first `Depth`\nsegments name what a developer would call \"where that lives\", and Depth is the zoom control.\n\nMEASURED on TheTerrace at depth 3: `Features.Fixtures` 117, `Features.Teams` 117, `Features.Matches`\n107, `Infrastructure.Data` 70, `Features.Identity` 47, `Features.Competitions` 44 — the actual shape\nof that repository, in **55,758 bytes** against 533,484 for the node graph.\n\n**No community-detection algorithm, deliberately.** Its output is unstable under small changes to the\ngraph, so the same repository would regroup between two indexes and the picture would move for\nreasons the user cannot see. Prefix grouping is stable by construction.\n\nEach group carries `NodeCount` — a dot standing for 240 types is only honest while the 240 is on it,\nwhich is the whole difference between an overview and a smaller lie. Each link carries `Weight` for\nthickness and the **weakest** status of the edges it bundles, because drawing a bundle as Verified\nbecause most of its members were would launder guesses into facts at a grain where the user can no\nlonger see the members. Edges inside a group are counted, not drawn.\n\n**My own first default was the failure the overview exists to prevent.** `MaxClusters` shipped at 200,\nand TheTerrace has 689 groups at depth 3 — returning 200 of them is a hairball at a coarser grain,\nwhich my own doc comment named as the thing to avoid. Measured and lowered to 60.\n\n**The frame-fit control is reflective, because hand-auditing found the last three and would not find\nthe next.** `EveryOperationFitsTheFrameTests` derives the operation list from `IWorkspaceQueries`\nitself and fails when a method is added with no size check — observed failing with an entry removed,\nnaming the method. Writing the list by hand would restate the product's own list (DC-021) and go\nstale in exactly the case that matters.\n\n**The write side was audited and needs nothing.** An `IndexSummary` for 28 scopes is **1,724 bytes** —\nthree orders of magnitude below the frame. Recorded rather than guarded: a control whose failure\nnobody can name is one to drop.\n\n**Degree on the wire was already done, by the other session.** They shipped force-directed layout with\ndegree-sized dots (DC-036), so that next step was stale when I wrote it. What layout still could not\ndo is cluster, which is what the overview provides. `GraphOverview.GroupFor` is public now so the\ncanvas groups detail nodes the SAME way the overview does — two definitions of one grouping is\nDC-022's shape, and the divergence would render a node in the wrong cluster and look like a layout\nbug.\n\n**A shared test double replaces four hand-written stubs.** Every method added to `IWorkspaceQueries`\nhas broken the same four stubs across two projects — four rounds of identical churn, which is a\nmeasured recurrence rather than a predicted one. `FakeWorkspaceQueries` throws rather than returning\nempty: a stub that quietly answers a question the test did not intend to ask turns a wrong call site\ninto a passing test.\n\n791 tests green (App 132, Core 659). Seven gates clean. Zero design-owned files.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Build the server-side aggregated overview, gate every operation against the frame, and audit the write side",
      "done_when": "The overview is measured on a real repository and on the wire; a reflective gate catches an unweighed operation; the write side measured; gates green; merged and published",
      "change": "cl-0084",
      "git": {
        "sha": "13a488b5028bc352171f202df353915e0f791ac2",
        "short": "13a488b50",
        "branch": "session/phase3-pane-probes",
        "pushed": false
      }
    },
    {
      "id": "al-0131",
      "shortname": "graph-canvas-search-to-focus",
      "datetime": "2026-08-30T17:20:50Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "do the next best actions â€” add search-to-focus to the graph canvas (target mockup, US-K)",
      "summary": "Added a header search box: type to highlight the first matching node (label or id), dim the rest, and in 2D pan/zoom it to centre; Enter lands keyboard focus on the match (into the trap); / jumps to search, Escape clears+refocuses, 0 resets the 2D view. Keyboard trap preserved (document keydown early-returns while search focused; claimFocus still lands on a node) â€” P2-FOCUS-03 green, App.Tests 132/132.",
      "kind": "skill",
      "skill": "ui-design",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/CanvasPage.cs"
      ],
      "tags": [
        "graph",
        "ui-design"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0242",
      "shortname": "graph-canvas-search-to-focus",
      "datetime": "2026-08-30T17:20:50Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "do the next best actions — add search-to-focus to the graph canvas (target mockup, US-K)",
      "summary": "Added a header search box: type to highlight the first matching node (label or id), dim the rest, and in 2D pan/zoom it to centre; Enter lands keyboard focus on the match (into the trap); / jumps to search, Escape clears+refocuses, 0 resets the 2D view. Keyboard trap preserved (document keydown early-returns while search focused; claimFocus still lands on a node) — P2-FOCUS-03 green, App.Tests 132/132.",
      "kind": "skill",
      "skill": "ui-design",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/CanvasPage.cs"
      ],
      "tags": [
        "graph",
        "ui-design"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0132",
      "shortname": "handoff-lod-unblocked",
      "datetime": "2026-08-30T17:26:11Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "assess LOD after merging Core's GraphOverview; record handoff",
      "summary": "Core shipped OverviewAsync/WorkspaceOverview + public GraphOverview.GroupFor; semantic-zoom LOD is now Design's remaining half (render group super-nodes + expand). Needs a small CanvasNode count-field contract touch so NodeCount reaches the renderer; recommend a /design first. Updated session-contracts Â§4c and tracked bk-graph-lod-render.",
      "kind": "manual",
      "skill": null,
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "docs/collaboration/session-contracts.md"
      ],
      "tags": [
        "graph",
        "handoff"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0243",
      "shortname": "handoff-lod-unblocked",
      "datetime": "2026-08-30T17:26:11Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "assess LOD after merging Core's GraphOverview; record handoff",
      "summary": "Core shipped OverviewAsync/WorkspaceOverview + public GraphOverview.GroupFor; semantic-zoom LOD is now Design's remaining half (render group super-nodes + expand). Needs a small CanvasNode count-field contract touch so NodeCount reaches the renderer; recommend a /design first. Updated session-contracts §4c and tracked bk-graph-lod-render.",
      "kind": "manual",
      "skill": null,
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "docs/collaboration/session-contracts.md"
      ],
      "tags": [
        "graph",
        "handoff"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0133",
      "shortname": "drill-down-daemon-proof-shared-double-second-repo",
      "datetime": "2026-08-30T17:31:26Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps you have listed\nprovide the standard status and next steps tables afterwards",
      "summary": "**Drill-down closes the loop the overview opened.** `GraphQuery.GroupId` returns the nodes inside one\ncluster, and the depth is read back out of the group id rather than passed alongside it â€” a separate\ndepth parameter would let a caller ask for `TheTerrace.Features` at depth 3 and receive nothing, with\nno error and no way to tell that from an empty group.\n\nThe property that makes the overview trustworthy is asserted, not assumed: **every cluster's\n`NodeCount` equals what drilling into it returns**, and every cluster's `InternalEdges` equals what\ndrilling in draws. For every group, not a sampled one â€” the interesting failure is the group whose\nnaming rule differs, and that is never the group a test author picks by hand. Both paths compute\nmembership from one definition (`GraphOverview.GroupFor`), which is why the round trip holds.\n\n**Proven across the pipe.** The overview is nested â€” clusters and weighted links â€” so it is the\nresponse most likely to arrive with an inner list flattened or a count defaulted to zero, and a\ncluster's count is a claim a user can check. The daemon test now drills from the largest cluster back\nto its nodes and asserts the counts agree.\n\n**Three repositories now, not one â€” and the third one found a defect in my own control.** Every\nmeasurement in this project had come from TheTerrace. Running BioHacker (32 scopes, no `DbContext` at\nall, so 0 joins is correct rather than a failure) and meridian-finance-planner (31 scopes, 9,165\nassertions, 165 TypeScript files) confirmed every operation fits the frame on all three.\n\nBioHacker reported `typescript-exports-not-recognised (1)` â€” the DC-033 control firing on the first\nnew repository it saw, which is what it is for. But the miss was **mine**: `export default\ndefineConfig({â€¦})` and `export default test;` declare nothing new, and the miss-counter's own doc\ncomment said `export default someExpression` was excluded while the pattern never excluded it.\n`export default` is ubiquitous, so the disclosure would have fired on nearly every real TypeScript\ncodebase and become noise. Fixed; the 165-file repository now reports zero.\n\n**The shape is worth naming because it is the third instance this session.** The exclusion was written\nin the comment before it was implemented â€” exactly like the evidence page documenting a byte cap it\ndid not apply, and `find` reporting a `MaxBytes` it never enforced. **A claim in prose that the code\ndoes not make true.** When a comment states a bound, the next question is which line applies it.\n\nAnd the meta-lesson, recorded on DC-033: **a control's false-positive rate is only observable on input\nit was not written against.** Validating on one repository validates against one repository.\n\n**The shared test double now covers both projects.** `FakeWorkspaceQueries` moved to `tests/Shared`\nunder a neutral namespace and is linked into both test csproj files â€” linked source rather than a new\nassembly, because a project for forty lines is more machinery than the churn it removes. The two\nApp-project stubs that hand-implemented the whole seam now override only what they answer.\n\n800 tests green (App 132, Core 668). Seven gates clean. Zero design-owned files.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Add the overview drill-down, prove it across the pipe, share the test double, and measure a second repository",
      "done_when": "The overview round trip holds for every group and across the daemon; both test projects share one double; a second and third repository measured; gates green; merged and published",
      "change": "cl-0085",
      "git": {
        "sha": "431cb97ebb26f9a9bab19d2f484a833dc78cdd9a",
        "short": "431cb97eb",
        "branch": "session/phase3-pane-probes",
        "pushed": true
      }
    },
    {
      "id": "al-0244",
      "shortname": "drill-down-daemon-proof-shared-double-second-repo",
      "datetime": "2026-08-30T17:31:26Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps you have listed\nprovide the standard status and next steps tables afterwards",
      "summary": "**Drill-down closes the loop the overview opened.** `GraphQuery.GroupId` returns the nodes inside one\ncluster, and the depth is read back out of the group id rather than passed alongside it — a separate\ndepth parameter would let a caller ask for `TheTerrace.Features` at depth 3 and receive nothing, with\nno error and no way to tell that from an empty group.\n\nThe property that makes the overview trustworthy is asserted, not assumed: **every cluster's\n`NodeCount` equals what drilling into it returns**, and every cluster's `InternalEdges` equals what\ndrilling in draws. For every group, not a sampled one — the interesting failure is the group whose\nnaming rule differs, and that is never the group a test author picks by hand. Both paths compute\nmembership from one definition (`GraphOverview.GroupFor`), which is why the round trip holds.\n\n**Proven across the pipe.** The overview is nested — clusters and weighted links — so it is the\nresponse most likely to arrive with an inner list flattened or a count defaulted to zero, and a\ncluster's count is a claim a user can check. The daemon test now drills from the largest cluster back\nto its nodes and asserts the counts agree.\n\n**Three repositories now, not one — and the third one found a defect in my own control.** Every\nmeasurement in this project had come from TheTerrace. Running BioHacker (32 scopes, no `DbContext` at\nall, so 0 joins is correct rather than a failure) and meridian-finance-planner (31 scopes, 9,165\nassertions, 165 TypeScript files) confirmed every operation fits the frame on all three.\n\nBioHacker reported `typescript-exports-not-recognised (1)` — the DC-033 control firing on the first\nnew repository it saw, which is what it is for. But the miss was **mine**: `export default\ndefineConfig({…})` and `export default test;` declare nothing new, and the miss-counter's own doc\ncomment said `export default someExpression` was excluded while the pattern never excluded it.\n`export default` is ubiquitous, so the disclosure would have fired on nearly every real TypeScript\ncodebase and become noise. Fixed; the 165-file repository now reports zero.\n\n**The shape is worth naming because it is the third instance this session.** The exclusion was written\nin the comment before it was implemented — exactly like the evidence page documenting a byte cap it\ndid not apply, and `find` reporting a `MaxBytes` it never enforced. **A claim in prose that the code\ndoes not make true.** When a comment states a bound, the next question is which line applies it.\n\nAnd the meta-lesson, recorded on DC-033: **a control's false-positive rate is only observable on input\nit was not written against.** Validating on one repository validates against one repository.\n\n**The shared test double now covers both projects.** `FakeWorkspaceQueries` moved to `tests/Shared`\nunder a neutral namespace and is linked into both test csproj files — linked source rather than a new\nassembly, because a project for forty lines is more machinery than the churn it removes. The two\nApp-project stubs that hand-implemented the whole seam now override only what they answer.\n\n800 tests green (App 132, Core 668). Seven gates clean. Zero design-owned files.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Add the overview drill-down, prove it across the pipe, share the test double, and measure a second repository",
      "done_when": "The overview round trip holds for every group and across the daemon; both test projects share one double; a second and third repository measured; gates green; merged and published",
      "change": "cl-0085",
      "git": {
        "sha": "431cb97ebb26f9a9bab19d2f484a833dc78cdd9a",
        "short": "431cb97eb",
        "branch": "session/phase3-pane-probes",
        "pushed": true
      }
    },
    {
      "id": "al-0134",
      "shortname": "specify-knowledge-explorer-mode",
      "datetime": "2026-08-30T19:18:26Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "BTW/backlog: reconsider the graph view as a full-window dual-pane surface (graph+search | node reader), entered from a rail icon; /specify it",
      "summary": "Wrote docs/specs/knowledge-explorer-mode.md refining spec-knowledge-exploration: the graph+reader master-detail becomes a dedicated full-window MODE entered from the activity rail (not a dock pane). Three layers (A: US-E1-E8 + one view-state concept 'primary view mode' + no-rebuild invariant; B: IA/flows/wireframe; C: C1xB2 archetype at body scale, U1-U20, the graph<->reader keyboard-cycle a11y contract). Reuses US-K3 reader + US-K4 node-walk. Flagged: canvas-trap<->reader focus routing and reader content-fetch contract for /design.",
      "kind": "skill",
      "skill": "specify",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "docs/specs/knowledge-explorer-mode.md"
      ],
      "tags": [
        "knowledge-graph",
        "spec"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0135",
      "shortname": "bounds-gate-sql-schemas-repeatable-measurement-depth",
      "datetime": "2026-08-30T19:21:42Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps you have listed\nprovide the standard status and next steps tables afterwards",
      "summary": "**The sweep found the class was live in the tree, and the class now has a control.**\n\nThree instances this session were the same shape â€” a comment states a bound the code does not apply.\nThe sweep found a fourth still standing: `MaxEvidencePageCeiling` documented as \"sized so a page\nstays comfortably inside `MaxResultBytes`\" when 2,000 assertions serialise to **1,004,397 bytes**,\nfifteen times that constant. Corrected to name the mechanism that actually bounds it.\n\n`tools/verify-bounds-are-enforced.py` requires every constant whose NAME claims a limit to appear in\na comparison, a clamp or a take â€” searched over code with comments stripped, so prose cannot count as\nproof. On its first run it found `WholeGraphNodeCap`: declared last turn as a \"retained name\",\ndocumented as a ceiling, **zero usages**. Deleted. One false positive (`OverviewNodeCap`, applied\nindirectly through `GraphQuery.MaxNodes`) became a justified exemption rather than a weaker rule,\nbecause \"it is passed somewhere\" is exactly what made `find` look safe.\n\n**The control says what it cannot do.** It checks a bound is APPLIED, not that a sentence describing\nit is TRUE â€” the TypeScript instance, a regex differing from its comment, is invisible to it. Half\nthe class is mechanised; claiming otherwise would be this class applied to its own control.\n\n**Measurement is repeatable now.** `tools/measure-repositories.py` runs the harness over three\ncodebases chosen for contrast and prints them side by side. It exists because every number in this\nproject came from one repository until a second was tried by hand, and the third exposed a defect in\na control that looked correct against the first.\n\n**The overview's default depth was the useless one, and three repositories showed it:**\n\n    TheTerrace                d1:  74 groups,  1 link    d2: 200,  6    d3: 200, 263\n    BioHacker                 d1:  17 groups,  0 links   d2:  56,  6    d3: 200, 323\n    meridian-finance-planner  d1:  92 groups,  6 links   d2: 159, 18    d3: 200,  93\n\nDepths 1 and 2 are almost linkless in ALL THREE, for an arithmetic reason: at a coarse grain nearly\nevery edge is internal to a group, counted and not drawn. A picture of disconnected islands is\ncorrect and says nothing about structure. Default moved 2 â†’ 3, from evidence rather than taste.\n\n**Raw SQL schemas are read.** BioHacker declares its whole schema in one 197-line file with eight\n`CREATE TABLE`s; the tool said `sql-not-analysed (2 file(s))` and produced **zero** joins â€” honest,\nand blind to that repository's entire schema side. `SqlSchemaExtractor` emits the SAME node shape as\nthe EF reader (`table:Name`, `has_type table`, `has_column`), because a second spelling would be\nDC-022 with two producers of one predicate and the joins would silently see half the tables.\n\nColumn parsing matches parentheses rather than scanning to the first `)`, so `DECIMAL(9,2)` does not\ntruncate the column list; constraint lines are not claimed as columns; the schema qualifier is\nstripped so `dbo.Principal` and `Principal` are one node.\n\nBioHacker: joins **0 â†’ 8**, 54 `has_column` facts. meridian: 30 â†’ 33 inferred, 31 â†’ 34 scopes.\n\nAnd `.sql` came out of `UnanalysedLanguages`. A closed gap reported as open is the same defect as\nhiding one that is not â€” **the third time that list has needed this correction, once per extractor\nadded**, so the rule is now written where the list is: it is the LAST STEP of adding an extractor.\n\nDC-038 registered, with its residual risk stated: name-based detection misses a bound named without\none of those suffixes, and a limit written as a magic number is invisible to it.\n\n812 tests green (App 132, Core 680). Eight gates clean, one of them new. Zero design-owned files.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Sweep the prose-claims-a-bound class and control it, make multi-repo measurement repeatable, read raw SQL schemas, and settle the overview's default depth",
      "done_when": "The class has a control observed finding something; three repositories measured by script; SQL tables reach the joins; depth chosen from evidence; gates green; merged and published",
      "change": "cl-0087",
      "git": {
        "sha": "4314a5efc2057dbca92d34e36b598cc74600931b",
        "short": "4314a5efc",
        "branch": "session/phase3-pane-probes",
        "pushed": true
      }
    },
    {
      "id": "al-0245",
      "shortname": "bounds-gate-sql-schemas-repeatable-measurement-depth",
      "datetime": "2026-08-30T19:21:42Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps you have listed\nprovide the standard status and next steps tables afterwards",
      "summary": "**The sweep found the class was live in the tree, and the class now has a control.**\n\nThree instances this session were the same shape — a comment states a bound the code does not apply.\nThe sweep found a fourth still standing: `MaxEvidencePageCeiling` documented as \"sized so a page\nstays comfortably inside `MaxResultBytes`\" when 2,000 assertions serialise to **1,004,397 bytes**,\nfifteen times that constant. Corrected to name the mechanism that actually bounds it.\n\n`tools/verify-bounds-are-enforced.py` requires every constant whose NAME claims a limit to appear in\na comparison, a clamp or a take — searched over code with comments stripped, so prose cannot count as\nproof. On its first run it found `WholeGraphNodeCap`: declared last turn as a \"retained name\",\ndocumented as a ceiling, **zero usages**. Deleted. One false positive (`OverviewNodeCap`, applied\nindirectly through `GraphQuery.MaxNodes`) became a justified exemption rather than a weaker rule,\nbecause \"it is passed somewhere\" is exactly what made `find` look safe.\n\n**The control says what it cannot do.** It checks a bound is APPLIED, not that a sentence describing\nit is TRUE — the TypeScript instance, a regex differing from its comment, is invisible to it. Half\nthe class is mechanised; claiming otherwise would be this class applied to its own control.\n\n**Measurement is repeatable now.** `tools/measure-repositories.py` runs the harness over three\ncodebases chosen for contrast and prints them side by side. It exists because every number in this\nproject came from one repository until a second was tried by hand, and the third exposed a defect in\na control that looked correct against the first.\n\n**The overview's default depth was the useless one, and three repositories showed it:**\n\n    TheTerrace                d1:  74 groups,  1 link    d2: 200,  6    d3: 200, 263\n    BioHacker                 d1:  17 groups,  0 links   d2:  56,  6    d3: 200, 323\n    meridian-finance-planner  d1:  92 groups,  6 links   d2: 159, 18    d3: 200,  93\n\nDepths 1 and 2 are almost linkless in ALL THREE, for an arithmetic reason: at a coarse grain nearly\nevery edge is internal to a group, counted and not drawn. A picture of disconnected islands is\ncorrect and says nothing about structure. Default moved 2 → 3, from evidence rather than taste.\n\n**Raw SQL schemas are read.** BioHacker declares its whole schema in one 197-line file with eight\n`CREATE TABLE`s; the tool said `sql-not-analysed (2 file(s))` and produced **zero** joins — honest,\nand blind to that repository's entire schema side. `SqlSchemaExtractor` emits the SAME node shape as\nthe EF reader (`table:Name`, `has_type table`, `has_column`), because a second spelling would be\nDC-022 with two producers of one predicate and the joins would silently see half the tables.\n\nColumn parsing matches parentheses rather than scanning to the first `)`, so `DECIMAL(9,2)` does not\ntruncate the column list; constraint lines are not claimed as columns; the schema qualifier is\nstripped so `dbo.Principal` and `Principal` are one node.\n\nBioHacker: joins **0 → 8**, 54 `has_column` facts. meridian: 30 → 33 inferred, 31 → 34 scopes.\n\nAnd `.sql` came out of `UnanalysedLanguages`. A closed gap reported as open is the same defect as\nhiding one that is not — **the third time that list has needed this correction, once per extractor\nadded**, so the rule is now written where the list is: it is the LAST STEP of adding an extractor.\n\nDC-038 registered, with its residual risk stated: name-based detection misses a bound named without\none of those suffixes, and a limit written as a magic number is invisible to it.\n\n812 tests green (App 132, Core 680). Eight gates clean, one of them new. Zero design-owned files.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Sweep the prose-claims-a-bound class and control it, make multi-repo measurement repeatable, read raw SQL schemas, and settle the overview's default depth",
      "done_when": "The class has a control observed finding something; three repositories measured by script; SQL tables reach the joins; depth chosen from evidence; gates green; merged and published",
      "change": "cl-0087",
      "git": {
        "sha": "4314a5efc2057dbca92d34e36b598cc74600931b",
        "short": "4314a5efc",
        "branch": "session/phase3-pane-probes",
        "pushed": true
      }
    },
    {
      "id": "al-0136",
      "shortname": "ui-design-knowledge-explorer-mode",
      "datetime": "2026-08-30T19:26:35Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "BTW/backlog: /ui-design the full-window dual-pane graph+reader Explorer mode",
      "summary": "Created docs/mockups/knowledge-explorer-mode.html (+hub): self-contained dependency-free mockup of the full-window mode â€” rail(active)+body-wide graph|reader split with draggable splitter, narrow-viewport stacks (US-E8); reader by kind (Palenight code/markdown/html) with metadata + walkable typed edges (US-K3/K4); ALL hard states (reader empty/loading/error/unsupported/overflow + graph loading/empty/too-large); review harness (state/viewport/theme/reduced-motion) + in-artifact contrast audit. DESIGN.md tokens; design-lint clean; token contrast worst 6.26:1 (>AA). Impeccable detector unavailable here (no node) â€” recorded, deferred to CI; verified by the inward linter + mechanical contrast audit + manual tells check. Minor tell: emoji state glyphs -> real icons in prod. Graph<->reader keyboard-cycle flagged for /design.",
      "kind": "skill",
      "skill": "ui-design",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "docs/mockups/knowledge-explorer-mode.html"
      ],
      "tags": [
        "ui-design",
        "explorer"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0246",
      "shortname": "ui-design-knowledge-explorer-mode",
      "datetime": "2026-08-30T19:26:35Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "BTW/backlog: /ui-design the full-window dual-pane graph+reader Explorer mode",
      "summary": "Created docs/mockups/knowledge-explorer-mode.html (+hub): self-contained dependency-free mockup of the full-window mode — rail(active)+body-wide graph|reader split with draggable splitter, narrow-viewport stacks (US-E8); reader by kind (Palenight code/markdown/html) with metadata + walkable typed edges (US-K3/K4); ALL hard states (reader empty/loading/error/unsupported/overflow + graph loading/empty/too-large); review harness (state/viewport/theme/reduced-motion) + in-artifact contrast audit. DESIGN.md tokens; design-lint clean; token contrast worst 6.26:1 (>AA). Impeccable detector unavailable here (no node) — recorded, deferred to CI; verified by the inward linter + mechanical contrast audit + manual tells check. Minor tell: emoji state glyphs -> real icons in prod. Graph<->reader keyboard-cycle flagged for /design.",
      "kind": "skill",
      "skill": "ui-design",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "docs/mockups/knowledge-explorer-mode.html"
      ],
      "tags": [
        "ui-design",
        "explorer"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0137",
      "shortname": "define-architecture-explorer-mode",
      "datetime": "2026-08-30T19:34:24Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "BTW/backlog: /define-architecture the full-window Explorer mode as it may be a material UI-architecture change",
      "summary": "Assessed: material but additive. ADR-0017 â€” a full-window surface is a primary VIEW MODE (Workbench|Explorer) via a body-content swap of the docking-host region (rail = mode selector), NOT a dock pane (competes) and NOT a modal overlay (rail must persist); retain-not-rebuild invariant (mode-level DC-029); per-mode persistence slot (amends ADR-0013); graph<->reader keyboard-cycle (amends ADR-0015). ADR-0018 â€” the reader fetches node content on demand via a bounded Core query NodeContentAsync (sibling of OverviewAsync), not by fattening CanvasNode (blows the frame) or App-side file reading (two authorities); RenderKind from the authority; transport-bounded Shortfall. Linked both from architecture.md; recorded the NodeContentAsync Design->Core request in session-contracts Â§4c. 3-phase vertical delivery (skeleton w/ mocked reader seam -> real content query -> keyboard-cycle+responsive).",
      "kind": "skill",
      "skill": "define-architecture",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "docs/adr/0017-primary-view-mode.md",
        "docs/adr/0018-node-content-reader-contract.md"
      ],
      "tags": [
        "architecture",
        "explorer"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0247",
      "shortname": "define-architecture-explorer-mode",
      "datetime": "2026-08-30T19:34:24Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "BTW/backlog: /define-architecture the full-window Explorer mode as it may be a material UI-architecture change",
      "summary": "Assessed: material but additive. ADR-0017 — a full-window surface is a primary VIEW MODE (Workbench|Explorer) via a body-content swap of the docking-host region (rail = mode selector), NOT a dock pane (competes) and NOT a modal overlay (rail must persist); retain-not-rebuild invariant (mode-level DC-029); per-mode persistence slot (amends ADR-0013); graph<->reader keyboard-cycle (amends ADR-0015). ADR-0018 — the reader fetches node content on demand via a bounded Core query NodeContentAsync (sibling of OverviewAsync), not by fattening CanvasNode (blows the frame) or App-side file reading (two authorities); RenderKind from the authority; transport-bounded Shortfall. Linked both from architecture.md; recorded the NodeContentAsync Design->Core request in session-contracts §4c. 3-phase vertical delivery (skeleton w/ mocked reader seam -> real content query -> keyboard-cycle+responsive).",
      "kind": "skill",
      "skill": "define-architecture",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "docs/adr/0017-primary-view-mode.md",
        "docs/adr/0018-node-content-reader-contract.md"
      ],
      "tags": [
        "architecture",
        "explorer"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0138",
      "shortname": "design-knowledge-explorer-mode",
      "datetime": "2026-08-30T19:43:16Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "do the next best actions â€” /design the Explorer mode component to unblock a safe /implement",
      "summary": "Wrote docs/design/knowledge-explorer-mode.md (Phase-1 walking skeleton). Resolves the ADR-deferred mechanism: D1 mode swap = WorkbenchHost.Content toggle with Shell held (HwndHost/WebView2 hide-not-destroy) â€” PROVEN by T1 not trusted, fallback collapse-in-place if red; D2 Explorer hosts its OWN CanvasSurface (no reparenting), second-canvas cost accepted; D3 new CanvasSurface.NodeSelected seam the reader follows (ADR-0018 sits behind it); D4 NodeReaderView stub (header+metadata+walkable edges, content placeholder); D5 rail toggle. Red-first test plan T1-T5 (+T-focus). Makes /implement mechanical.",
      "kind": "skill",
      "skill": "design",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "docs/design/knowledge-explorer-mode.md"
      ],
      "tags": [
        "explorer",
        "design"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0248",
      "shortname": "design-knowledge-explorer-mode",
      "datetime": "2026-08-30T19:43:16Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "do the next best actions — /design the Explorer mode component to unblock a safe /implement",
      "summary": "Wrote docs/design/knowledge-explorer-mode.md (Phase-1 walking skeleton). Resolves the ADR-deferred mechanism: D1 mode swap = WorkbenchHost.Content toggle with Shell held (HwndHost/WebView2 hide-not-destroy) — PROVEN by T1 not trusted, fallback collapse-in-place if red; D2 Explorer hosts its OWN CanvasSurface (no reparenting), second-canvas cost accepted; D3 new CanvasSurface.NodeSelected seam the reader follows (ADR-0018 sits behind it); D4 NodeReaderView stub (header+metadata+walkable edges, content placeholder); D5 rail toggle. Red-first test plan T1-T5 (+T-focus). Makes /implement mechanical.",
      "kind": "skill",
      "skill": "design",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "docs/design/knowledge-explorer-mode.md"
      ],
      "tags": [
        "explorer",
        "design"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0139",
      "shortname": "sql-fold-uses-table-gates-into-ci-marker-audit",
      "datetime": "2026-08-30T19:49:46Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps you have listed\nprovide the standard status and next steps tables afterwards",
      "summary": "**SQL scripts are folded, not just read.** MEASURED: one repository carries **125 `ALTER TABLE â€¦ ADD`**\nstatements, so reading `CREATE` alone showed its schema as it stood at the first migration and called\nthat current. Adds, `DROP COLUMN` and `DROP TABLE` are now applied in file order â€” drops especially,\nbecause a column that no longer exists is a **wrong** fact rather than a missing one. Renames are\ncounted and disclosed rather than guessed: every dialect spells them differently, and guessing\nproduces a confidently wrong column name.\n\n**Three gates I wrote this session were not in CI.** `verify-id-allocators`,\n`verify-project-coverage` and `verify-bounds-are-enforced` sat in `tools/` for several commits with\nno workflow line â€” they ran only when I remembered. A gate nobody invokes is the \"lesson recorded as\nprose\" failure wearing an executable's clothes: it looks like a control in every review and fires\nnever. All three wired, and `verify-project-coverage` now asks the same question about gates that it\nasks about projects â€” **what exists to be run, and is not run.** Observed failing with a gate removed\nfrom the workflow.\n\n**The `simplify:` marker audit found one triggered and one premise already false.** `IpcFraming`'s\ntrigger fired during INV-0003 â€” and the answer was **none of the two exits the marker listed**.\nNeither a bigger frame nor a data lane: the operation did not legitimately need to carry more. A\n2,815-node hairball was never a useful answer and the spec had always said so, so every response was\nbounded below the cap instead. Recorded, because a marker naming two exits invites you to take one.\n\nIts stated premise â€” *\"a control lane carries envelopes, not payloads: the largest legitimate message\nis a command with a small JSON body\"* â€” was **already false when audited**: ordinary responses are an\nevidence page at 659,164 bytes, a graph at 475,223, an overview at 345,507. That sentence had not\nbeen re-read since it was true. The other twelve markers were reviewed with no evidence of firing.\n\n**\"Verified joins for non-EF repositories\" turned out to be the wrong question, and the finding is\nbetter than the feature.** BioHacker has zero `DbContext` files, zero `[Table]` attributes and 191\nSQL literals naming tables from inside store classes. There is **no declaration of a codeâ†’schema\nmapping to verify** â€” and there should not be one invented. A store class issuing four statements\nagainst three tables is not *mapped* to any of them.\n\nWhat the source does declare is **usage**, so that is what is emitted: `uses_table`, Verified because\nthe literal is in the type. **62 edges on BioHacker**, structure it simply did not have. Deliberately\nnot `maps_to` â€” reusing the mapping predicate would launder usage into a mapping exactly where a\nreader trusts it (DC-022's shape). Joins there remain 0 verified / 8 inferred, which is the honest\nanswer.\n\n**Deduplication moved into one place on its third copy.** Python and TypeScript each grew the same\nsix lines after the same raw `UNIQUE constraint failed` from mid-index; `uses_table` hit it a third\ntime immediately, because one store names the same table in four statements. `ExtractionFacts.Distinct`\nnow owns it, so the fourth extractor inherits it. The store's key stays strict â€” silencing it would\ntrade a loud correct failure for a quiet wrong graph.\n\n**And \"make the measurement a CI nightly\" was wrong, stated rather than quietly dropped.** The\nrepositories are sibling checkouts on one machine; a hosted runner has none of them, so the job would\nfail or skip every night â€” a gate that cannot fire, dressed as diligence. `--record` is the\nachievable half: readings append to `docs/measurements/repositories.jsonl`, so drift shows up in\n`git diff` rather than in somebody's memory. First reading committed.\n\n831 tests green (App 132, Core 699). Eight gates clean, all eight now actually run by CI. Zero\ndesign-owned files.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Fold ALTER into the SQL reader, get the measurement recorded, find what a non-EF repo actually declares, and audit the simplify markers",
      "done_when": "Scripts fold in order with drops applied; every gate runs in CI; the marker audit records what triggered; non-EF repos gain real edges without a false Verified; gates green; merged and published",
      "change": "cl-0089",
      "git": {
        "sha": "fd8516ff7fafe459cebb0ce2f55e68de32bc1881",
        "short": "fd8516ff7",
        "branch": "session/phase3-pane-probes",
        "pushed": false
      }
    },
    {
      "id": "al-0249",
      "shortname": "sql-fold-uses-table-gates-into-ci-marker-audit",
      "datetime": "2026-08-30T19:49:46Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps you have listed\nprovide the standard status and next steps tables afterwards",
      "summary": "**SQL scripts are folded, not just read.** MEASURED: one repository carries **125 `ALTER TABLE … ADD`**\nstatements, so reading `CREATE` alone showed its schema as it stood at the first migration and called\nthat current. Adds, `DROP COLUMN` and `DROP TABLE` are now applied in file order — drops especially,\nbecause a column that no longer exists is a **wrong** fact rather than a missing one. Renames are\ncounted and disclosed rather than guessed: every dialect spells them differently, and guessing\nproduces a confidently wrong column name.\n\n**Three gates I wrote this session were not in CI.** `verify-id-allocators`,\n`verify-project-coverage` and `verify-bounds-are-enforced` sat in `tools/` for several commits with\nno workflow line — they ran only when I remembered. A gate nobody invokes is the \"lesson recorded as\nprose\" failure wearing an executable's clothes: it looks like a control in every review and fires\nnever. All three wired, and `verify-project-coverage` now asks the same question about gates that it\nasks about projects — **what exists to be run, and is not run.** Observed failing with a gate removed\nfrom the workflow.\n\n**The `simplify:` marker audit found one triggered and one premise already false.** `IpcFraming`'s\ntrigger fired during INV-0003 — and the answer was **none of the two exits the marker listed**.\nNeither a bigger frame nor a data lane: the operation did not legitimately need to carry more. A\n2,815-node hairball was never a useful answer and the spec had always said so, so every response was\nbounded below the cap instead. Recorded, because a marker naming two exits invites you to take one.\n\nIts stated premise — *\"a control lane carries envelopes, not payloads: the largest legitimate message\nis a command with a small JSON body\"* — was **already false when audited**: ordinary responses are an\nevidence page at 659,164 bytes, a graph at 475,223, an overview at 345,507. That sentence had not\nbeen re-read since it was true. The other twelve markers were reviewed with no evidence of firing.\n\n**\"Verified joins for non-EF repositories\" turned out to be the wrong question, and the finding is\nbetter than the feature.** BioHacker has zero `DbContext` files, zero `[Table]` attributes and 191\nSQL literals naming tables from inside store classes. There is **no declaration of a code→schema\nmapping to verify** — and there should not be one invented. A store class issuing four statements\nagainst three tables is not *mapped* to any of them.\n\nWhat the source does declare is **usage**, so that is what is emitted: `uses_table`, Verified because\nthe literal is in the type. **62 edges on BioHacker**, structure it simply did not have. Deliberately\nnot `maps_to` — reusing the mapping predicate would launder usage into a mapping exactly where a\nreader trusts it (DC-022's shape). Joins there remain 0 verified / 8 inferred, which is the honest\nanswer.\n\n**Deduplication moved into one place on its third copy.** Python and TypeScript each grew the same\nsix lines after the same raw `UNIQUE constraint failed` from mid-index; `uses_table` hit it a third\ntime immediately, because one store names the same table in four statements. `ExtractionFacts.Distinct`\nnow owns it, so the fourth extractor inherits it. The store's key stays strict — silencing it would\ntrade a loud correct failure for a quiet wrong graph.\n\n**And \"make the measurement a CI nightly\" was wrong, stated rather than quietly dropped.** The\nrepositories are sibling checkouts on one machine; a hosted runner has none of them, so the job would\nfail or skip every night — a gate that cannot fire, dressed as diligence. `--record` is the\nachievable half: readings append to `docs/measurements/repositories.jsonl`, so drift shows up in\n`git diff` rather than in somebody's memory. First reading committed.\n\n831 tests green (App 132, Core 699). Eight gates clean, all eight now actually run by CI. Zero\ndesign-owned files.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Fold ALTER into the SQL reader, get the measurement recorded, find what a non-EF repo actually declares, and audit the simplify markers",
      "done_when": "Scripts fold in order with drops applied; every gate runs in CI; the marker audit records what triggered; non-EF repos gain real edges without a false Verified; gates green; merged and published",
      "change": "cl-0089",
      "git": {
        "sha": "fd8516ff7fafe459cebb0ce2f55e68de32bc1881",
        "short": "fd8516ff7",
        "branch": "session/phase3-pane-probes",
        "pushed": false
      }
    },
    {
      "id": "al-0140",
      "shortname": "implement-explorer-mode-p1",
      "datetime": "2026-08-30T19:58:18Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "do the next best actions â€” /implement the Explorer mode Phase-1 walking skeleton",
      "summary": "Built the full Phase-1 walking skeleton per docs/design/knowledge-explorer-mode.md: ShellModeController (WorkbenchHost.Content swap, retain-not-rebuild D1); rail Explore item toggles Explorer mode + active state (D5); ExplorerSurface = graph|reader split + GridSplitter, graph is a dedicated query-bound CanvasSurface via new WorkbenchShell.CreateExplorerGraph (D2); NodeReaderView (header+metadata+walkable edges+empty state D4); new CanvasSurface.NodeSelected seam so the reader follows selection and a reader edge walks the graph (D3). Tests T1 (retain-not-rebuild, same workbench instance across round-trip), T-retain (Explorer built once), T5 (toggle), T4 (reader empty), T3 (reader records selection) â€” App.Tests 137/137. Launched: app starts clean, no crash from the mode wiring. Deferred to Phase 3: full graph<->reader keyboard cycle. Phase 2 wires ADR-0018 NodeContentAsync behind the D3 seam.",
      "kind": "skill",
      "skill": "implement",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/ShellModeController.cs",
        "src/AiDe.App/Workbench/ExplorerSurface.cs"
      ],
      "tags": [
        "explorer",
        "implement"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0250",
      "shortname": "implement-explorer-mode-p1",
      "datetime": "2026-08-30T19:58:18Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "do the next best actions — /implement the Explorer mode Phase-1 walking skeleton",
      "summary": "Built the full Phase-1 walking skeleton per docs/design/knowledge-explorer-mode.md: ShellModeController (WorkbenchHost.Content swap, retain-not-rebuild D1); rail Explore item toggles Explorer mode + active state (D5); ExplorerSurface = graph|reader split + GridSplitter, graph is a dedicated query-bound CanvasSurface via new WorkbenchShell.CreateExplorerGraph (D2); NodeReaderView (header+metadata+walkable edges+empty state D4); new CanvasSurface.NodeSelected seam so the reader follows selection and a reader edge walks the graph (D3). Tests T1 (retain-not-rebuild, same workbench instance across round-trip), T-retain (Explorer built once), T5 (toggle), T4 (reader empty), T3 (reader records selection) — App.Tests 137/137. Launched: app starts clean, no crash from the mode wiring. Deferred to Phase 3: full graph<->reader keyboard cycle. Phase 2 wires ADR-0018 NodeContentAsync behind the D3 seam.",
      "kind": "skill",
      "skill": "implement",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/ShellModeController.cs",
        "src/AiDe.App/Workbench/ExplorerSurface.cs"
      ],
      "tags": [
        "explorer",
        "implement"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0141",
      "shortname": "fix-explorer-keyboard-trap",
      "datetime": "2026-08-30T20:30:35Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "do the next best actions â€” found and fixed the Explorer keyboard trap in shipped Phase-1 code",
      "summary": "DC-039: the Phase-1 Explorer graph canvas trapped keyboard focus â€” CreateExplorerGraph set GraphSource but never subscribed CanvasSurface.FocusLeaveRequested (unlike the workbench's BindCanvas), so tabbing off the graph boundary had nothing routing focus out (WCAG 2.1.2 fail). Fix: ExplorerSurface routes the leave into the reader (NodeReaderView.FocusReader), which moves focus to the first walkable edge or the region itself. Added FocusReader + WalkableEdgeCount; tests for edge-count and focus-lands-in-reader. Registered DC-039. App.Tests 139/139.",
      "kind": "manual",
      "skill": null,
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/ExplorerSurface.cs",
        "src/AiDe.App/Workbench/NodeReaderView.cs"
      ],
      "tags": [
        "explorer",
        "accessibility",
        "defect-fix"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0251",
      "shortname": "fix-explorer-keyboard-trap",
      "datetime": "2026-08-30T20:30:35Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "do the next best actions — found and fixed the Explorer keyboard trap in shipped Phase-1 code",
      "summary": "DC-039: the Phase-1 Explorer graph canvas trapped keyboard focus — CreateExplorerGraph set GraphSource but never subscribed CanvasSurface.FocusLeaveRequested (unlike the workbench's BindCanvas), so tabbing off the graph boundary had nothing routing focus out (WCAG 2.1.2 fail). Fix: ExplorerSurface routes the leave into the reader (NodeReaderView.FocusReader), which moves focus to the first walkable edge or the region itself. Added FocusReader + WalkableEdgeCount; tests for edge-count and focus-lands-in-reader. Registered DC-039. App.Tests 139/139.",
      "kind": "manual",
      "skill": null,
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/ExplorerSurface.cs",
        "src/AiDe.App/Workbench/NodeReaderView.cs"
      ],
      "tags": [
        "explorer",
        "accessibility",
        "defect-fix"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0142",
      "shortname": "uses-table-in-joins-prose-defect-ef-fold-audit",
      "datetime": "2026-08-30T20:46:24Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps you have listed\nprovide the standard status and next steps tables afterwards",
      "summary": "**I shipped a defect last turn and this turn's sweep caught it.** The `uses_table` reader matched a\nSQL keyword followed by a word ANYWHERE in a string literal, so *\"we update the record\"* produced an\nedge to a table called `the`. MEASURED: 63 prose strings in TheTerrace, and its `uses_table` count\nfell from **150 to 56** once the reader required a statement SHAPE.\n\nUnder-matching hides real facts; over-matching invents them â€” and the invented ones are worse,\nbecause they arrive labelled **Verified**. Same root cause as DC-033 in the opposite direction: the\nmatcher's fit to real input had never been measured. **A matcher is not finished until you know both\nwhat it misses and what it invents.**\n\n**The naive fix broke the real case, which is exactly why both directions must be measured\ntogether.** Requiring each literal to begin with a verb found **nothing at all** on the repository\nthat motivated the feature â€” real code splits SQL across concatenated literals, and the fragment\nholding `FROM dbo.AssessmentJob` begins with `FROM`. The reader folds the `+` chain and reads it as\none statement; a chain containing anything non-literal is skipped whole rather than half-read.\n\nA smaller lesson: the regex form of that shape test silently returned false for\n`\"INSERT INTO dbo.AssessmentJob (â€¦)\"` â€” a string plainly beginning with one of its own alternatives â€”\nand cost more to diagnose than the check was worth. It is explicit code now.\n\n**`uses_table` reaches the Joins pane.** A distinct kind, never folded into `maps_to`: a store class\nissuing four statements against three tables is not mapped to any of them, and counting usage as\nmapping would make the verified/inferred ratio the pane exists to show meaningless. BioHacker's pane\ngoes from **0 verified / 8 inferred to 57 / 8** â€” a repository that showed nothing now shows what its\ncode actually touches.\n\n**Step 2 needed no work and step 3 was answered by measurement.** The EF reader ALREADY folds\n`CreateTable`, `AddColumn`, `DropColumn`, `DropTable`, `RenameTable` and raw `Sql` â€” I assumed it\nmight not, without checking, which is the habit this session keeps catching. Measured against real\nusage: the operations it does not handle (`CreateIndex` 113, `AddForeignKey` 19, `AlterColumn`) do\nnot change which tables or columns EXIST. One genuine gap found and closed: `RenameColumn` was\nunhandled with no default case, so a renamed column silently kept its old name â€” a wrong fact. EF\nstates renames in named arguments, so unlike raw SQL it is simply readable.\n\n**Python and TypeScript do not embed SQL, so nothing was built for them.** The counts looked\npromising â€” 186, 61, 1,162 \"SQL literals\" â€” and sampling showed *\"update the\"*, *\"select more than\none option from this\"*, and **zero** files containing `insert into`. Building `uses_table` there\nwould have emitted edges from English prose. Evidence for NOT building something is worth as much as\nevidence for building it.\n\n**The measurement log earns its keep at two readings.** Verified joins across the three repositories\nmoved **64 â†’ 120**, **0 â†’ 57**, **35 â†’ 50**, with assertion counts slightly DOWN as prose noise left\nthe graph. One reading is a point; two is a direction, and the drift is now in `git diff` rather than\nin my memory of last turn's numbers.\n\n840 tests green (App 137, Core 703). Eight gates clean. Zero design-owned files.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Surface uses_table in the joins pane, check the EF reader folds drops, sweep other extractors for the same edge, and take a second measurement reading",
      "done_when": "The joins pane shows usage without laundering it as mapping; the EF fold is verified against real operation usage; the Python/TS sweep is decided on evidence; a second reading is recorded; gates green; merged and published",
      "change": "cl-0090",
      "git": {
        "sha": "87f04b04886d1fb8a5a0c239c253565e765e81b7",
        "short": "87f04b048",
        "branch": "session/phase3-pane-probes",
        "pushed": false
      }
    },
    {
      "id": "al-0252",
      "shortname": "uses-table-in-joins-prose-defect-ef-fold-audit",
      "datetime": "2026-08-30T20:46:24Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps you have listed\nprovide the standard status and next steps tables afterwards",
      "summary": "**I shipped a defect last turn and this turn's sweep caught it.** The `uses_table` reader matched a\nSQL keyword followed by a word ANYWHERE in a string literal, so *\"we update the record\"* produced an\nedge to a table called `the`. MEASURED: 63 prose strings in TheTerrace, and its `uses_table` count\nfell from **150 to 56** once the reader required a statement SHAPE.\n\nUnder-matching hides real facts; over-matching invents them — and the invented ones are worse,\nbecause they arrive labelled **Verified**. Same root cause as DC-033 in the opposite direction: the\nmatcher's fit to real input had never been measured. **A matcher is not finished until you know both\nwhat it misses and what it invents.**\n\n**The naive fix broke the real case, which is exactly why both directions must be measured\ntogether.** Requiring each literal to begin with a verb found **nothing at all** on the repository\nthat motivated the feature — real code splits SQL across concatenated literals, and the fragment\nholding `FROM dbo.AssessmentJob` begins with `FROM`. The reader folds the `+` chain and reads it as\none statement; a chain containing anything non-literal is skipped whole rather than half-read.\n\nA smaller lesson: the regex form of that shape test silently returned false for\n`\"INSERT INTO dbo.AssessmentJob (…)\"` — a string plainly beginning with one of its own alternatives —\nand cost more to diagnose than the check was worth. It is explicit code now.\n\n**`uses_table` reaches the Joins pane.** A distinct kind, never folded into `maps_to`: a store class\nissuing four statements against three tables is not mapped to any of them, and counting usage as\nmapping would make the verified/inferred ratio the pane exists to show meaningless. BioHacker's pane\ngoes from **0 verified / 8 inferred to 57 / 8** — a repository that showed nothing now shows what its\ncode actually touches.\n\n**Step 2 needed no work and step 3 was answered by measurement.** The EF reader ALREADY folds\n`CreateTable`, `AddColumn`, `DropColumn`, `DropTable`, `RenameTable` and raw `Sql` — I assumed it\nmight not, without checking, which is the habit this session keeps catching. Measured against real\nusage: the operations it does not handle (`CreateIndex` 113, `AddForeignKey` 19, `AlterColumn`) do\nnot change which tables or columns EXIST. One genuine gap found and closed: `RenameColumn` was\nunhandled with no default case, so a renamed column silently kept its old name — a wrong fact. EF\nstates renames in named arguments, so unlike raw SQL it is simply readable.\n\n**Python and TypeScript do not embed SQL, so nothing was built for them.** The counts looked\npromising — 186, 61, 1,162 \"SQL literals\" — and sampling showed *\"update the\"*, *\"select more than\none option from this\"*, and **zero** files containing `insert into`. Building `uses_table` there\nwould have emitted edges from English prose. Evidence for NOT building something is worth as much as\nevidence for building it.\n\n**The measurement log earns its keep at two readings.** Verified joins across the three repositories\nmoved **64 → 120**, **0 → 57**, **35 → 50**, with assertion counts slightly DOWN as prose noise left\nthe graph. One reading is a point; two is a direction, and the drift is now in `git diff` rather than\nin my memory of last turn's numbers.\n\n840 tests green (App 137, Core 703). Eight gates clean. Zero design-owned files.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Surface uses_table in the joins pane, check the EF reader folds drops, sweep other extractors for the same edge, and take a second measurement reading",
      "done_when": "The joins pane shows usage without laundering it as mapping; the EF fold is verified against real operation usage; the Python/TS sweep is decided on evidence; a second reading is recorded; gates green; merged and published",
      "change": "cl-0090",
      "git": {
        "sha": "87f04b04886d1fb8a5a0c239c253565e765e81b7",
        "short": "87f04b048",
        "branch": "session/phase3-pane-probes",
        "pushed": false
      }
    },
    {
      "id": "al-0143",
      "shortname": "fix-explorer-empty-graph",
      "datetime": "2026-08-30T21:02:01Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "regression: the Explorer graph shows 'No workspace is open' even though TheTerrace is open",
      "summary": "DC-040: CreateExplorerGraph captured _queries once into a CanvasGraphViewModel; the Explorer surface is retained (US-E6), so a surface first created before the workspace attached stayed bound to null and showed 'No workspace is open' forever, while the workbench graph (rebound via BindCanvas) worked. Fix: read _queries LIVE in the GraphSource lambda (fresh VM per load), and refresh the Explorer graph on each mode entry. App.Tests 139/139.",
      "kind": "manual",
      "skill": null,
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/WorkbenchShell.cs",
        "src/AiDe.App/MainWindow.xaml.cs"
      ],
      "tags": [
        "explorer",
        "regression",
        "defect-fix"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0144",
      "shortname": "extractors-do-not-invent-control-comment-stripping",
      "datetime": "2026-08-30T21:04:10Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps you have listed\nprovide the standard status and next steps tables afterwards",
      "summary": "**The invent-direction of DC-033 now has a control, and it found four more defects on its first\nrun.** `ExtractorsDoNotInventTests` feeds every reader a corpus with no declarations and plenty of\ntext SHAPED like declarations, and asserts it produces nothing but disclosures:\n\n- the **SQL** reader read `-- CREATE TABLE Ghost` and `/* CREATE TABLE Historical */` as tables;\n- the **TypeScript** reader read `export class Removed {}` out of a block comment;\n- the **Python** reader read a class out of a **docstring** â€” the one place its column-zero rule\n  cannot tell documentation from declaration;\n- the **C#** reader turned *\"delete from your account to remove it\"* into `table:your`, because that\n  sentence genuinely begins with a SQL verb and last turn's shape test could not reject it.\n\n**Commented-out code is the worst possible input for a line-oriented reader**, and every repository\nis full of it â€” it is real syntax, because it *was* code. `SourceText` blanks comments before any\nreader believes a line, keeping newlines so provenance line numbers stay true. The C# case needed a\nsecond rule: a real table reference **ends where a clause can begin** â€” a keyword, punctuation, or\nthe end of the statement. In prose the next token is just another word.\n\n**Two things the fixes got wrong first, kept because they are the lesson.** Blanking string contents\nfor SQL deleted `\"main\".\"Thing\"`: in SQL a double quote is a quoted IDENTIFIER, not a string, so the\nreader lost the very names it exists to find â€” caught by a test in seconds, which is the cheapest\npossible way to learn that two languages disagree about a quote character. And `PRINT 'about to\ncreate table X'` names no table while `EXEC('CREATE TABLE â€¦')` does; the reader can tell neither from\nthe other, so it reads neither and discloses the count.\n\n**Step 1 was answered by measurement and needed no change.** The Bicep and context readers were swept\non a repository they were not written against: Bicep's matchers are line-anchored on `resource`,\n`module` and `param`, and their values came back as real parameter names (`sqlServerName`,\n`identityName`) and real Azure types â€” nothing invented. The context map's `*` is a documented suffix\nwildcard and the real patterns end at a `.` boundary. Both pinned by the new control so they stay\nthat way.\n\n**The third reading shows the CORRECTION, which is what a third reading is for.** Verified joins:\n\n    TheTerrace                64 -> 120 -> 95\n    BioHacker                  0 ->  57 -> 55\n    meridian-finance-planner  35 ->  50 -> 46\n\nThe middle number was inflated by prose; the last is the honest one. **A single reading would have\nrecorded 120 as progress**, and two readings would have recorded it as a trend.\n\n**On `AlterColumn` â€” the decision, not an assumption.** It is not folded, and should not be. The\ngraph's `has_column` records that a column EXISTS; `AlterColumn` changes a type or nullability, which\nthat fact does not carry, so folding it would change nothing observable. Recording types is a\ndifferent feature â€” it would need a fact shape that can express them, both schema readers agreeing on\nit, and a consumer that wants it. None of those exists, so this stays where it is: named in the\nextractor's `simplify:` ceiling as the upgrade trigger, rather than half-built.\n\n848 tests green (App 139, Core 709). Eight gates clean. Zero design-owned files.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Sweep the Bicep and context readers for invented facts, make prose-noise a control across all extractors, decide the AlterColumn question, and take a third reading",
      "done_when": "Every extractor is pinned against noise it did not author; the readers found inventing are fixed; AlterColumn decided with reasons; a third reading recorded; gates green; merged and published",
      "change": "cl-0091",
      "git": {
        "sha": "66bfcf62cc39fad546ab14fcfe32f6bffc3e7b4d",
        "short": "66bfcf62c",
        "branch": "session/phase3-pane-probes",
        "pushed": true
      }
    },
    {
      "id": "al-0253",
      "shortname": "extractors-do-not-invent-control-comment-stripping",
      "datetime": "2026-08-30T21:04:10Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps you have listed\nprovide the standard status and next steps tables afterwards",
      "summary": "**The invent-direction of DC-033 now has a control, and it found four more defects on its first\nrun.** `ExtractorsDoNotInventTests` feeds every reader a corpus with no declarations and plenty of\ntext SHAPED like declarations, and asserts it produces nothing but disclosures:\n\n- the **SQL** reader read `-- CREATE TABLE Ghost` and `/* CREATE TABLE Historical */` as tables;\n- the **TypeScript** reader read `export class Removed {}` out of a block comment;\n- the **Python** reader read a class out of a **docstring** — the one place its column-zero rule\n  cannot tell documentation from declaration;\n- the **C#** reader turned *\"delete from your account to remove it\"* into `table:your`, because that\n  sentence genuinely begins with a SQL verb and last turn's shape test could not reject it.\n\n**Commented-out code is the worst possible input for a line-oriented reader**, and every repository\nis full of it — it is real syntax, because it *was* code. `SourceText` blanks comments before any\nreader believes a line, keeping newlines so provenance line numbers stay true. The C# case needed a\nsecond rule: a real table reference **ends where a clause can begin** — a keyword, punctuation, or\nthe end of the statement. In prose the next token is just another word.\n\n**Two things the fixes got wrong first, kept because they are the lesson.** Blanking string contents\nfor SQL deleted `\"main\".\"Thing\"`: in SQL a double quote is a quoted IDENTIFIER, not a string, so the\nreader lost the very names it exists to find — caught by a test in seconds, which is the cheapest\npossible way to learn that two languages disagree about a quote character. And `PRINT 'about to\ncreate table X'` names no table while `EXEC('CREATE TABLE …')` does; the reader can tell neither from\nthe other, so it reads neither and discloses the count.\n\n**Step 1 was answered by measurement and needed no change.** The Bicep and context readers were swept\non a repository they were not written against: Bicep's matchers are line-anchored on `resource`,\n`module` and `param`, and their values came back as real parameter names (`sqlServerName`,\n`identityName`) and real Azure types — nothing invented. The context map's `*` is a documented suffix\nwildcard and the real patterns end at a `.` boundary. Both pinned by the new control so they stay\nthat way.\n\n**The third reading shows the CORRECTION, which is what a third reading is for.** Verified joins:\n\n    TheTerrace                64 -> 120 -> 95\n    BioHacker                  0 ->  57 -> 55\n    meridian-finance-planner  35 ->  50 -> 46\n\nThe middle number was inflated by prose; the last is the honest one. **A single reading would have\nrecorded 120 as progress**, and two readings would have recorded it as a trend.\n\n**On `AlterColumn` — the decision, not an assumption.** It is not folded, and should not be. The\ngraph's `has_column` records that a column EXISTS; `AlterColumn` changes a type or nullability, which\nthat fact does not carry, so folding it would change nothing observable. Recording types is a\ndifferent feature — it would need a fact shape that can express them, both schema readers agreeing on\nit, and a consumer that wants it. None of those exists, so this stays where it is: named in the\nextractor's `simplify:` ceiling as the upgrade trigger, rather than half-built.\n\n848 tests green (App 139, Core 709). Eight gates clean. Zero design-owned files.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Sweep the Bicep and context readers for invented facts, make prose-noise a control across all extractors, decide the AlterColumn question, and take a third reading",
      "done_when": "Every extractor is pinned against noise it did not author; the readers found inventing are fixed; AlterColumn decided with reasons; a third reading recorded; gates green; merged and published",
      "change": "cl-0091",
      "git": {
        "sha": "66bfcf62cc39fad546ab14fcfe32f6bffc3e7b4d",
        "short": "66bfcf62c",
        "branch": "session/phase3-pane-probes",
        "pushed": true
      }
    },
    {
      "id": "al-0145",
      "shortname": "graph-category-filter",
      "datetime": "2026-08-30T21:30:46Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "the graph is so big â€” let me prune it by knowledge / specs / code / data; ui-design and implement in the graph and explorer",
      "summary": "Added a client-side category filter to CanvasPage (so it works in BOTH the workbench graph pane and the Explorer â€” one page). Design: four toggle chips (Code/Data/Specs/Knowledge) matching the existing chrome, each with a colour dot + live per-category count, in a filter bar below the caption; toggling hides that category's nodes and any edge touching a hidden node, and the caption reports 'N of M shown â€” filtered by category'. categoryOf(kind) maps has_type values -> category: Data = table/column/schema/azure/sql; Code = C# types + unrecognised; Specs/Knowledge = docs (forward-compatible, ~0 in the code graph today). Keyboard shortcuts guarded so keys on a chip don't switch the view; P2-FOCUS-03 keyboard trap still green. Future: server-side GraphQuery.Kinds to pull MORE relevant nodes within the cap. App.Tests 139/139.",
      "kind": "skill",
      "skill": "ui-design",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/CanvasPage.cs"
      ],
      "tags": [
        "graph",
        "filter",
        "ui-design"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0254",
      "shortname": "graph-category-filter",
      "datetime": "2026-08-30T21:30:46Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "the graph is so big — let me prune it by knowledge / specs / code / data; ui-design and implement in the graph and explorer",
      "summary": "Added a client-side category filter to CanvasPage (so it works in BOTH the workbench graph pane and the Explorer — one page). Design: four toggle chips (Code/Data/Specs/Knowledge) matching the existing chrome, each with a colour dot + live per-category count, in a filter bar below the caption; toggling hides that category's nodes and any edge touching a hidden node, and the caption reports 'N of M shown — filtered by category'. categoryOf(kind) maps has_type values -> category: Data = table/column/schema/azure/sql; Code = C# types + unrecognised; Specs/Knowledge = docs (forward-compatible, ~0 in the code graph today). Keyboard shortcuts guarded so keys on a chip don't switch the view; P2-FOCUS-03 keyboard trap still green. Future: server-side GraphQuery.Kinds to pull MORE relevant nodes within the cap. App.Tests 139/139.",
      "kind": "skill",
      "skill": "ui-design",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/CanvasPage.cs"
      ],
      "tags": [
        "graph",
        "filter",
        "ui-design"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0150",
      "shortname": "investigate-graph-kind-and-knowledge",
      "datetime": "2026-08-30T21:52:28Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "why is knowledge zero; why bicep reads knowledge; are the 'not resolved' disclosures external or a gap; split infra from data; do next steps",
      "summary": "INV-0004: Knowledge=0 is code-only extractors (docs not indexed, US-K1 Core work); bicep 'knowledge' = coarse node_kind shown where fine has_type meant (reader now prefers has_type); disclosures mostly by-design, a few extractor gaps. Filter split Infra from Data. DC-041 registered; Core handoffs recorded. App.Tests 139 green + P2-FOCUS-03.",
      "kind": "skill",
      "skill": "investigate",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/investigations/INV-0004-graph-kind-taxonomy-and-knowledge.md"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0151",
      "shortname": "knowledge-graph-was-never-populated",
      "datetime": "2026-08-30T22:00:11Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "i noticed in the other session that the graph was showing knowledge as zero count and code as a large count.. is the knowledge graph being populated? if not it should be\ninvestigate and fix that\nalso do the next steps you outlined above\nbut first tell me what the question is on the graphquery filters\n---\nyes go with presets - agreed, three named views",
      "summary": "**The knowledge graph was never being populated, and the reason is worse than a bug.** The reader had\nexisted since Phase 1, with tests, inside the fixture extractor. `CompositeExtractor` had a route for\nit. And `CSharpScopeDiscovery` produced six scope kinds â€” `csharp`, `bicep`, `schema`, `python`,\n`typescript`, `sql` â€” and **no knowledge scope at all**. The capability was correct, tested, and\nunreachable on every real repository for the life of the project.\n\nBoth halves passed their own tests. The producer is proven by unit tests that hand it input; the\nrouter is proven by fixtures that name the scope explicitly. Only DISCOVERY was untested against\nreality, and its gap is invisible from either side.\n\n**A zero that means \"nobody looked\" reads as \"there is none\"** â€” which is the shape this product\nexists to avoid, in the product's own headline surface, on a repository whose premise is that *docs\nhold intent, code holds reality, and the expensive defects live in the gap*. Half of that sentence\nwas never being read.\n\nMEASURED after wiring discovery, on this repository: **466 `owned_by`, 346 `refines`, 287\n`implements`, 272 `relates-to`, 66 `depends-on`**, centred on `knowledge-hub` with 78 edges. Scopes\nacross the three measured repositories: **28â†’66, 34â†’48, 34â†’56**. Every response still fits the frame.\n\n**Running it over real documents immediately found two defects in the new reader** â€” which is the\nlesson from last turn applied one turn later:\n\n- link lines carry a trailing YAML comment, and trimming from the END left it attached: the graph\n  gained a relation literally called `implements }   # typed edges â€” registry in â€¦`. Parsing now\n  stops at the closing brace.\n- **templates** carry frontmatter in exactly the shape a real document does, with `<artifact-id>`\n  where the id goes â€” so they became nodes describing the shape of a document, linked to things that\n  do not exist. Excluded by filename and, independently, by rejecting angle-bracketed placeholders.\n\nDC-041 registered: **a capability is complete, tested, and nothing ever routes work to it.** The\nsignature is a count that is exactly zero on every real repository while a sibling count is large,\nand the question that finds it is *\"what produces the keys this router matches on, and does it\nproduce this one?\"*. The control compares the two lists in a test instead of in somebody's head:\n`EveryRouteHasAProducerAndEveryProducerHasARoute` fails if discovery emits a kind nothing routes, or\nif a route exists that nothing discovers.\n\n**The other next steps, carried:**\n\n- **Bicep is comment-stripped too.** It PASSED the invent control â€” line-anchored matchers, and a\n  sweep of an unfamiliar repository produced only real parameter names and real Azure types. Stripped\n  anyway: it was the last line-oriented reader still parsing raw text, and all three readers caught\n  inventing were caught reading commented-out code.\n- **Provenance line numbers are asserted, not assumed.** Comments are blanked rather than deleted\n  precisely so a claim can still be opened at the right line; a test now pins `5:1` for a table\n  declared after a multi-line comment. It was the reason for the design and had never been checked.\n- **The invent control now runs against real repository text**, which is how both knowledge defects\n  surfaced â€” synthetic noise is written by the same person as the reader (DC-028's shape).\n- **`GraphQuery` filters: ANSWERED by the user â€” presets, three named views.** Recorded in the\n  contract with the three query shapes (Domain / Everything / This project). `Kinds` is deliberately\n  not one of them: it is a refinement *within* a view, and folding it in would rebuild the\n  combinatorial space presets exist to avoid.\n\n859 tests green (App 139, Core 720). Eight gates clean. Zero design-owned files.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Answer the GraphQuery filter question, find and fix why knowledge reports zero, and carry the outlined next steps",
      "done_when": "The filter question is stated and the answer recorded; knowledge is populated and measured on real repositories; the routing gap has a control; Bicep stripped; provenance lines asserted; gates green; merged and published",
      "change": "cl-0095",
      "git": {
        "sha": "609b4e0d4892a919866f2e8e3eee7e2d4d425e95",
        "short": "609b4e0d4",
        "branch": "session/phase3-pane-probes",
        "pushed": false
      }
    },
    {
      "id": "al-0255",
      "shortname": "knowledge-graph-was-never-populated",
      "datetime": "2026-08-30T22:00:11Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "i noticed in the other session that the graph was showing knowledge as zero count and code as a large count.. is the knowledge graph being populated? if not it should be\ninvestigate and fix that\nalso do the next steps you outlined above\nbut first tell me what the question is on the graphquery filters\n---\nyes go with presets - agreed, three named views",
      "summary": "**The knowledge graph was never being populated, and the reason is worse than a bug.** The reader had\nexisted since Phase 1, with tests, inside the fixture extractor. `CompositeExtractor` had a route for\nit. And `CSharpScopeDiscovery` produced six scope kinds — `csharp`, `bicep`, `schema`, `python`,\n`typescript`, `sql` — and **no knowledge scope at all**. The capability was correct, tested, and\nunreachable on every real repository for the life of the project.\n\nBoth halves passed their own tests. The producer is proven by unit tests that hand it input; the\nrouter is proven by fixtures that name the scope explicitly. Only DISCOVERY was untested against\nreality, and its gap is invisible from either side.\n\n**A zero that means \"nobody looked\" reads as \"there is none\"** — which is the shape this product\nexists to avoid, in the product's own headline surface, on a repository whose premise is that *docs\nhold intent, code holds reality, and the expensive defects live in the gap*. Half of that sentence\nwas never being read.\n\nMEASURED after wiring discovery, on this repository: **466 `owned_by`, 346 `refines`, 287\n`implements`, 272 `relates-to`, 66 `depends-on`**, centred on `knowledge-hub` with 78 edges. Scopes\nacross the three measured repositories: **28→66, 34→48, 34→56**. Every response still fits the frame.\n\n**Running it over real documents immediately found two defects in the new reader** — which is the\nlesson from last turn applied one turn later:\n\n- link lines carry a trailing YAML comment, and trimming from the END left it attached: the graph\n  gained a relation literally called `implements }   # typed edges — registry in …`. Parsing now\n  stops at the closing brace.\n- **templates** carry frontmatter in exactly the shape a real document does, with `<artifact-id>`\n  where the id goes — so they became nodes describing the shape of a document, linked to things that\n  do not exist. Excluded by filename and, independently, by rejecting angle-bracketed placeholders.\n\nDC-041 registered: **a capability is complete, tested, and nothing ever routes work to it.** The\nsignature is a count that is exactly zero on every real repository while a sibling count is large,\nand the question that finds it is *\"what produces the keys this router matches on, and does it\nproduce this one?\"*. The control compares the two lists in a test instead of in somebody's head:\n`EveryRouteHasAProducerAndEveryProducerHasARoute` fails if discovery emits a kind nothing routes, or\nif a route exists that nothing discovers.\n\n**The other next steps, carried:**\n\n- **Bicep is comment-stripped too.** It PASSED the invent control — line-anchored matchers, and a\n  sweep of an unfamiliar repository produced only real parameter names and real Azure types. Stripped\n  anyway: it was the last line-oriented reader still parsing raw text, and all three readers caught\n  inventing were caught reading commented-out code.\n- **Provenance line numbers are asserted, not assumed.** Comments are blanked rather than deleted\n  precisely so a claim can still be opened at the right line; a test now pins `5:1` for a table\n  declared after a multi-line comment. It was the reason for the design and had never been checked.\n- **The invent control now runs against real repository text**, which is how both knowledge defects\n  surfaced — synthetic noise is written by the same person as the reader (DC-028's shape).\n- **`GraphQuery` filters: ANSWERED by the user — presets, three named views.** Recorded in the\n  contract with the three query shapes (Domain / Everything / This project). `Kinds` is deliberately\n  not one of them: it is a refinement *within* a view, and folding it in would rebuild the\n  combinatorial space presets exist to avoid.\n\n859 tests green (App 139, Core 720). Eight gates clean. Zero design-owned files.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Answer the GraphQuery filter question, find and fix why knowledge reports zero, and carry the outlined next steps",
      "done_when": "The filter question is stated and the answer recorded; knowledge is populated and measured on real repositories; the routing gap has a control; Bicep stripped; provenance lines asserted; gates green; merged and published",
      "change": "cl-0095",
      "git": {
        "sha": "609b4e0d4892a919866f2e8e3eee7e2d4d425e95",
        "short": "609b4e0d4",
        "branch": "session/phase3-pane-probes",
        "pushed": false
      }
    },
    {
      "id": "al-0152",
      "shortname": "implement-explorer-phase3-keyboard-cycle",
      "datetime": "2026-08-30T22:01:27Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "do the next best action (Explorer Phase 3: full graph<->reader keyboard cycle)",
      "summary": "Closed the graph<->reader keyboard cycle (US-E7/E8): reader detects its boundary (Shift+Tab off first stop / Tab off last) via BoundaryLeave/HandleTabKey and raises FocusLeaveRequested; ExplorerSurface routes graph->reader by direction (Forward=first stop, Backward=last) and reader->graph via guarded FocusTarget.TryFocus. Empty reader still leaves either way (no trap). 4 tests; App.Tests 139->143 green.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/NodeReaderView.cs",
        "src/AiDe.App/Workbench/ExplorerSurface.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0153",
      "shortname": "implement-explorer-responsive-stacking-us-e8",
      "datetime": "2026-08-30T22:10:31Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "do the next steps (Explorer responsive stacking US-E8)",
      "summary": "ExplorerSurface now responsive: above StackBelowWidth (760) panes are side-by-side (columns); below it they stack graph-over-reader (rows) so both halves stay usable on a narrow single-monitor window. ApplyLayoutForWidth is a pure fn of width; recomputed on SizeChanged. 1 test; App.Tests 143->144 green.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/ExplorerSurface.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0155",
      "shortname": "inv-0004-node-class-knowledge-health-vocabulary-control",
      "datetime": "2026-08-30T22:26:25Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do these next steps now",
      "summary": "**INV-0004's root cause was a Phase-1 assumption that six extractors later made false.** Node\nclassification read:\n\n    var isKnowledge = result.Assertions.Any(a => a.Subject == nodeId && a.Predicate == \"has_type\");\n\n*Anything with a type is knowledge* â€” true when the fixture reader was the only producer of\n`has_type`, and false the moment a second extractor emitted it. By now every C# class, table, bicep\nresource and python module carried one, so almost the entire graph was classified `knowledge`, and it\nsurfaced exactly as the design session saw it: a bicep resource reading **\"kind: knowledge\"**. DC-022\nprecisely â€” a predicate gained producers and a consumer kept its assumption about who emits it.\n\n**Fixed by having the PRODUCER declare it.** Scope-id prefixes were the first attempt and were nearly\nright, but the fixture reader emits knowledge from a scope not named for it, so the id could not be\nthe authority either. A fact can be: both knowledge-reading paths now emit `node_class = knowledge`,\nand the core classifies from that. Nothing infers a node's half of the graph from a type name again.\n\n**Neighbours carry their real kind.** `DescribeResult` gained `NeighborKinds`; the canvas hardcoded\n`\"source\"` for every neighbour because the projection did not carry one, so a drill-down showed a\ntable, a bicep resource and a class as the same thing and the filter could not tell them apart. A\nrenderer inventing a default is a renderer stating a fact it does not have.\n\n**`UpsertNode` is a no-op when nothing changed.** It is a Type-2 dimension and every call closed the\ncurrent row and opened a new one, so re-indexing rewrote the history of every unchanged node. History\nwhose every row is an artefact of re-running the indexer cannot answer the question it exists for.\n\n**The knowledge pane was returning code.** It filtered on `has_type` alone â€” the same stale\nassumption, one projection along â€” so now that knowledge is finally indexed it would have shown C#\nclasses beside ADRs. It filters on `node_class` now, which is the dimension that knows.\n\n**`review-by` is read and overdue reviews are a health finding.** 460 review dates on this repository.\n`owned_by` and `review_by` joined the attribute set: a person and a date are not things to navigate\nto, and drawing them would have put \"@someone\" and \"2027-02-28\" in the graph as peers of the\ndocuments that carry them.\n\n**The one \"document with no id\" was a false positive of mine.** `INSTALL.md` opens with `doc:`,\n`bundle_version:` and `changes:` â€” a pack manifest that happens to use YAML, never a graph node. The\ncomplaint now fires only when the frontmatter is GRAPH frontmatter (a type, owner, links or review\ndate) and still has no id. Same mistake as reading prose into a schema, one field along.\n\n**The codeâ†”knowledge join is NOT buildable today, and that is the finding.** Measured: **no knowledge\nlink in this repository targets a code symbol** â€” every `to:` names another document id. There is no\ndeclared relationship to read, and inferring one from titles or namespaces is exactly the guessing\nthis codebase refuses. What would unblock it is a change to how docs are WRITTEN, not to the reader:\na link such as `- { to: TheTerrace.Features.Fixtures, rel: governs }`. Recorded rather than invented.\n\n**DC-042's residual is now checkable.** `ConsumersReadWhatProducersEmitTests` reads the join\nprojection's predicate list FROM ITS SOURCE and asserts every name is one some extractor actually\nemits â€” so a renamed predicate fails a test instead of quietly returning fewer edges. Its first run\nreported `is_secret` and `resource_name_expression` as orphaned; they are emitted on real\nrepositories, and the gap was in the fixture I had written. A control validated against input its\nauthor wrote is a control that measures the author â€” so the fixture now exercises the whole bicep\nvocabulary.\n\n867 tests green (App 144, Core 723). Eight gates clean. Zero design-owned files.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Carry INV-0004's Core handoffs, make the knowledge pane return knowledge, add review-by health, check the id-less document, and sweep the remaining routers for DC-042",
      "done_when": "node_kind is declared by producers not inferred; neighbours carry real kinds; the knowledge pane filters on node_class; review-by read and overdue disclosed; the id-less document explained; the code-knowledge join decided on evidence; a vocabulary control lands; gates green; merged and published",
      "change": "cl-0097",
      "git": {
        "sha": "df001100c4408da481fddf804225e7f01f8148a9",
        "short": "df001100c",
        "branch": "session/phase3-pane-probes",
        "pushed": false
      }
    },
    {
      "id": "al-0256",
      "shortname": "inv-0004-node-class-knowledge-health-vocabulary-control",
      "datetime": "2026-08-30T22:26:25Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do these next steps now",
      "summary": "**INV-0004's root cause was a Phase-1 assumption that six extractors later made false.** Node\nclassification read:\n\n    var isKnowledge = result.Assertions.Any(a => a.Subject == nodeId && a.Predicate == \"has_type\");\n\n*Anything with a type is knowledge* — true when the fixture reader was the only producer of\n`has_type`, and false the moment a second extractor emitted it. By now every C# class, table, bicep\nresource and python module carried one, so almost the entire graph was classified `knowledge`, and it\nsurfaced exactly as the design session saw it: a bicep resource reading **\"kind: knowledge\"**. DC-022\nprecisely — a predicate gained producers and a consumer kept its assumption about who emits it.\n\n**Fixed by having the PRODUCER declare it.** Scope-id prefixes were the first attempt and were nearly\nright, but the fixture reader emits knowledge from a scope not named for it, so the id could not be\nthe authority either. A fact can be: both knowledge-reading paths now emit `node_class = knowledge`,\nand the core classifies from that. Nothing infers a node's half of the graph from a type name again.\n\n**Neighbours carry their real kind.** `DescribeResult` gained `NeighborKinds`; the canvas hardcoded\n`\"source\"` for every neighbour because the projection did not carry one, so a drill-down showed a\ntable, a bicep resource and a class as the same thing and the filter could not tell them apart. A\nrenderer inventing a default is a renderer stating a fact it does not have.\n\n**`UpsertNode` is a no-op when nothing changed.** It is a Type-2 dimension and every call closed the\ncurrent row and opened a new one, so re-indexing rewrote the history of every unchanged node. History\nwhose every row is an artefact of re-running the indexer cannot answer the question it exists for.\n\n**The knowledge pane was returning code.** It filtered on `has_type` alone — the same stale\nassumption, one projection along — so now that knowledge is finally indexed it would have shown C#\nclasses beside ADRs. It filters on `node_class` now, which is the dimension that knows.\n\n**`review-by` is read and overdue reviews are a health finding.** 460 review dates on this repository.\n`owned_by` and `review_by` joined the attribute set: a person and a date are not things to navigate\nto, and drawing them would have put \"@someone\" and \"2027-02-28\" in the graph as peers of the\ndocuments that carry them.\n\n**The one \"document with no id\" was a false positive of mine.** `INSTALL.md` opens with `doc:`,\n`bundle_version:` and `changes:` — a pack manifest that happens to use YAML, never a graph node. The\ncomplaint now fires only when the frontmatter is GRAPH frontmatter (a type, owner, links or review\ndate) and still has no id. Same mistake as reading prose into a schema, one field along.\n\n**The code↔knowledge join is NOT buildable today, and that is the finding.** Measured: **no knowledge\nlink in this repository targets a code symbol** — every `to:` names another document id. There is no\ndeclared relationship to read, and inferring one from titles or namespaces is exactly the guessing\nthis codebase refuses. What would unblock it is a change to how docs are WRITTEN, not to the reader:\na link such as `- { to: TheTerrace.Features.Fixtures, rel: governs }`. Recorded rather than invented.\n\n**DC-042's residual is now checkable.** `ConsumersReadWhatProducersEmitTests` reads the join\nprojection's predicate list FROM ITS SOURCE and asserts every name is one some extractor actually\nemits — so a renamed predicate fails a test instead of quietly returning fewer edges. Its first run\nreported `is_secret` and `resource_name_expression` as orphaned; they are emitted on real\nrepositories, and the gap was in the fixture I had written. A control validated against input its\nauthor wrote is a control that measures the author — so the fixture now exercises the whole bicep\nvocabulary.\n\n867 tests green (App 144, Core 723). Eight gates clean. Zero design-owned files.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Carry INV-0004's Core handoffs, make the knowledge pane return knowledge, add review-by health, check the id-less document, and sweep the remaining routers for DC-042",
      "done_when": "node_kind is declared by producers not inferred; neighbours carry real kinds; the knowledge pane filters on node_class; review-by read and overdue disclosed; the id-less document explained; the code-knowledge join decided on evidence; a vocabulary control lands; gates green; merged and published",
      "change": "cl-0097",
      "git": {
        "sha": "df001100c4408da481fddf804225e7f01f8148a9",
        "short": "df001100c",
        "branch": "session/phase3-pane-probes",
        "pushed": false
      }
    },
    {
      "id": "al-0156",
      "shortname": "fix-explorer-graph-monochrome-context-colour",
      "datetime": "2026-08-30T22:33:32Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "fresh screenshots: Explorer graph is grey while workbench graph is coloured; do unblocked next steps",
      "summary": "Fixed the Explorer graph rendering all-grey: CreateExplorerGraph built a fresh CanvasGraphViewModel without wiring ContextLookup (node colour comes from context; default lookup returns null). Extracted BuildContextLookup() and applied it to both the workbench and Explorer graph sources so they colour identically, read live so a workspace change is reflected. DC-042 registered. Also diagnosed the dead docked 'Explore' tab (Core handoff: refresh view panes on attach / decide the redundant surface's fate). App.Tests 144 green + launch smoke.",
      "kind": "skill",
      "skill": "investigate",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/WorkbenchShell.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0157",
      "shortname": "ui-graph-disclosures-collapsible",
      "datetime": "2026-08-30T22:37:43Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "declutter the graph: the 5-line orange disclosure wall dominates the pane",
      "summary": "Made the graph disclosures a native collapsible <details>: a short summary ('N edge(s) omitted . M analysis boundary note(s)') is always visible; the full list expands on demand; hidden entirely when nothing to disclose. Preserves the honesty (analysis boundaries) while reclaiming vertical space. Keyboard-accessible summary added to the node-shortcut guard so it doesn't fire view toggles. P2-FOCUS-03 + App.Tests 144 green.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/CanvasPage.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0159",
      "shortname": "knowledge-chip-zero-generation-and-declared-class",
      "datetime": "2026-08-30T23:22:01Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "on #2: Its ok if docs and code are not linkable and orthogonal, they will tend to be orthogonal which is why pruning the graph on one or the other is a meaningful cut. do not infer... the graph should only be on observable links/relationships\ni did not see knowledge nodes - it still says 0 for the terrace\ndo next steps",
      "summary": "**The Knowledge chip read 0 on a repository holding 2,343 knowledge nodes. Two causes, both Core's.**\n\n**The store was cached from a build that had no knowledge reader.** `ScopeFingerprints` already\ncarried an `ExtractorGeneration` for exactly this â€” a constant in every fingerprint so that upgrading\nthe product invalidates the sidecar â€” and nobody bumped it. So the knowledge extractor, `node_class`,\ncomment stripping in four readers, the SQL fold and `uses_table` all shipped while every existing\nworkspace kept serving results produced by the previous generation. Bumped to `2026-08-30.1`; an\nupgraded app now rebuilds rather than reusing.\n\n**And the graph carried only each node's FINE kind.** MEASURED: TheTerrace's knowledge types are\n`spec` and **`knowledge-epl-fan-platform`** â€” a name that repository invented. A chip matching a\nfixed list of type names cannot work across repositories, and widening the list only moves the\nproblem to the next one (DC-033). `GraphNode.IsKnowledge` carries the DECLARED coarse dimension, so a\nfilter asks the question instead of recognising spellings.\n\nVerified end to end: TheTerrace now yields **2,343 `node_class`, 639 `owned_by`, 428 `refines`, 114\n`spec`** â€” 24,058 assertions across 66 scopes, every response still inside the frame.\n\n**The codeâ†”knowledge join is closed by decision, not deferred.** The user's rule: *\"It's ok if docs\nand code are not linkable and orthogonal, they will tend to be orthogonal which is why pruning the\ngraph on one or the other is a meaningful cut. Do not infer â€” the graph should only be on observable\nlinks/relationships.\"*\n\nRecorded as a decision note, because it is a standing principle rather than one answer: **an edge is\na claim that something in the repository says so.** Orthogonality is information â€” because the two\nhalves are separate, \"show the knowledge\" and \"show the code\" are exact cuts rather than blurred\nones. Inference would have made membership depend on how good the guess was that day, and this\ncodebase has paid for that twice already (`depends_on` produced 7,426 false Verified edges;\n`uses_table` turned *\"we update the record\"* into a table called `the`). What would legitimately\nunblock a join is a link written down â€” `- { to: TheTerrace.Features.Fixtures, rel: governs }` â€” which\nthe frontmatter reader would index today with no code change.\n\n**Python dynamic and nested declarations: measured, and NOT built.** Dynamic imports are unobservable\nstatically, so reading them would be inference â€” ruled out by the principle above. Nested\ndeclarations are observable but MEASURED at 28/31/21 against 356/457/150 top-level across three\nrepositories (~7%), and they are implementation details inside functions with no type information to\nbuild edges from: the graph would gain volume, not structure. The existing disclosures already state\nexactly this. INV-0004's fourth handoff closes as a priority call answered.\n\n**DC-013 recurred a sixth time and the gate caught it.** Both sessions allocated `DC-042`; mine was\npublished first, so theirs is re-issued as `DC-043` per the contract. Their entry also arrived with an\nunbackticked `Status:` value for the second time â€” and the register gate reported it as *\"declares no\nStatus line\"* when the line was there and the FORMAT was wrong. That message misdirected me twice, so\nit now names the actual problem and shows the expected form. **A control that misnames its own finding\ncosts the reader the time the control saved.**\n\n869 tests green (App 144, Core 725). Eight gates clean. Zero design-owned files.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Record the no-inference principle, find why the Knowledge chip still reads 0 on TheTerrace, and carry the remaining next steps",
      "done_when": "The orthogonality decision is written down; knowledge is proven to reach TheTerrace's graph and the chip has a declared dimension to filter on; python coverage decided on measurement; repos re-measured; gates green; merged and published",
      "change": "cl-0099",
      "git": {
        "sha": "693ce79705bd30345b23a2c93b0750de89324492",
        "short": "693ce7970",
        "branch": "session/phase3-pane-probes",
        "pushed": false
      }
    },
    {
      "id": "al-0257",
      "shortname": "knowledge-chip-zero-generation-and-declared-class",
      "datetime": "2026-08-30T23:22:01Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "on #2: Its ok if docs and code are not linkable and orthogonal, they will tend to be orthogonal which is why pruning the graph on one or the other is a meaningful cut. do not infer... the graph should only be on observable links/relationships\ni did not see knowledge nodes - it still says 0 for the terrace\ndo next steps",
      "summary": "**The Knowledge chip read 0 on a repository holding 2,343 knowledge nodes. Two causes, both Core's.**\n\n**The store was cached from a build that had no knowledge reader.** `ScopeFingerprints` already\ncarried an `ExtractorGeneration` for exactly this — a constant in every fingerprint so that upgrading\nthe product invalidates the sidecar — and nobody bumped it. So the knowledge extractor, `node_class`,\ncomment stripping in four readers, the SQL fold and `uses_table` all shipped while every existing\nworkspace kept serving results produced by the previous generation. Bumped to `2026-08-30.1`; an\nupgraded app now rebuilds rather than reusing.\n\n**And the graph carried only each node's FINE kind.** MEASURED: TheTerrace's knowledge types are\n`spec` and **`knowledge-epl-fan-platform`** — a name that repository invented. A chip matching a\nfixed list of type names cannot work across repositories, and widening the list only moves the\nproblem to the next one (DC-033). `GraphNode.IsKnowledge` carries the DECLARED coarse dimension, so a\nfilter asks the question instead of recognising spellings.\n\nVerified end to end: TheTerrace now yields **2,343 `node_class`, 639 `owned_by`, 428 `refines`, 114\n`spec`** — 24,058 assertions across 66 scopes, every response still inside the frame.\n\n**The code↔knowledge join is closed by decision, not deferred.** The user's rule: *\"It's ok if docs\nand code are not linkable and orthogonal, they will tend to be orthogonal which is why pruning the\ngraph on one or the other is a meaningful cut. Do not infer — the graph should only be on observable\nlinks/relationships.\"*\n\nRecorded as a decision note, because it is a standing principle rather than one answer: **an edge is\na claim that something in the repository says so.** Orthogonality is information — because the two\nhalves are separate, \"show the knowledge\" and \"show the code\" are exact cuts rather than blurred\nones. Inference would have made membership depend on how good the guess was that day, and this\ncodebase has paid for that twice already (`depends_on` produced 7,426 false Verified edges;\n`uses_table` turned *\"we update the record\"* into a table called `the`). What would legitimately\nunblock a join is a link written down — `- { to: TheTerrace.Features.Fixtures, rel: governs }` — which\nthe frontmatter reader would index today with no code change.\n\n**Python dynamic and nested declarations: measured, and NOT built.** Dynamic imports are unobservable\nstatically, so reading them would be inference — ruled out by the principle above. Nested\ndeclarations are observable but MEASURED at 28/31/21 against 356/457/150 top-level across three\nrepositories (~7%), and they are implementation details inside functions with no type information to\nbuild edges from: the graph would gain volume, not structure. The existing disclosures already state\nexactly this. INV-0004's fourth handoff closes as a priority call answered.\n\n**DC-013 recurred a sixth time and the gate caught it.** Both sessions allocated `DC-042`; mine was\npublished first, so theirs is re-issued as `DC-043` per the contract. Their entry also arrived with an\nunbackticked `Status:` value for the second time — and the register gate reported it as *\"declares no\nStatus line\"* when the line was there and the FORMAT was wrong. That message misdirected me twice, so\nit now names the actual problem and shows the expected form. **A control that misnames its own finding\ncosts the reader the time the control saved.**\n\n869 tests green (App 144, Core 725). Eight gates clean. Zero design-owned files.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Record the no-inference principle, find why the Knowledge chip still reads 0 on TheTerrace, and carry the remaining next steps",
      "done_when": "The orthogonality decision is written down; knowledge is proven to reach TheTerrace's graph and the chip has a declared dimension to filter on; python coverage decided on measurement; repos re-measured; gates green; merged and published",
      "change": "cl-0099",
      "git": {
        "sha": "693ce79705bd30345b23a2c93b0750de89324492",
        "short": "693ce7970",
        "branch": "session/phase3-pane-probes",
        "pushed": false
      }
    },
    {
      "id": "al-0161",
      "shortname": "knowledge-pane-capped-before-filtering-and-ci-branch-gap",
      "datetime": "2026-08-30T23:40:40Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps",
      "summary": "**The knowledge pane was STILL returning zero, and the reason was a second, independent defect.**\nChasing the render rather than the data found it: `Knowledge()` read the first 200 `has_type`\nassertions and filtered THOSE to knowledge â€” so on any real repository the 200 were code types in\nalphabetical order and the filter left nothing. MEASURED: **0 items on a workspace holding 468\nknowledge nodes**; now 50, the ceiling.\n\nThat is DC-035's shape one projection along â€” **a cap applied before a filter returns the wrong\nslice trimmed to the right shape, and nothing in the result says so.** I fixed exactly this in\n`GraphProjection` and did not look for siblings. Pinned by a test whose fixture puts 400 code types\nalphabetically ahead of the knowledge, which is the condition that hid it.\n\n**The generation bump is now a control instead of a memory.** `ExtractorGeneration` existed and was\ncomplete; using it was a thing somebody had to remember, and a full day of extractor changes shipped\nwithout it. `verify-extractor-generation.py` fails when anything under `src/AiDe.Core/Extraction/`\nchanged since the generation last did. Deliberately conservative â€” a comment-only edit trips it, and\nthe remedy is a one-line bump costing one re-index, where deciding which edits \"really\" change output\nis a judgement nobody can make reliably about a compiler-driven extractor. Observed failing by\nreplaying the real baseline: it lists the exact extractors that shipped unbumped.\n\nIt uses `git log -G`, not `-S`. `-S` counts occurrences, so replacing one generation value with\nanother leaves the count unchanged and the bump looks like no change at all.\n\n**CI only ran on `push: main` and `pull_request`.** The design session pushes to a long-lived feature\nbranch with no PR, so its work met no gate until it reached main â€” by which point it was merged. That\nis why an unbackticked `Status:` value arrived twice and a duplicate `DC-` id six times, each caught\nby whoever merged next rather than by the branch that introduced it. **A gate that only guards the\ndestination reports problems to the wrong person.** CI now runs on every branch.\n\n**And the new gate would not have fired in CI.** `actions/checkout@v4` is shallow by default, so the\nsearch for the last generation bump would have found nothing and the gate would have passed silently\nâ€” the defect it exists to prevent, one layer up. `fetch-depth: 0` added.\n\n**`IsKnowledge` is proven across the pipe, with an assertion that can fail.** The first version I\nwrote was `Assert.Equal(n.IsKnowledge, n.IsKnowledge)` against a fixture with no knowledge in it â€”\na tautology over an empty set. The daemon fixture now holds a document beside the code, and the test\nasserts a known-knowledge node arrives with the flag set and a known-code node without it.\n\n**Knowledge health is computed and unrendered**, so it is a contract request rather than Core work:\n`owner not recorded`, `type not recorded`, `orphan`, `source location not recorded` and now `review\noverdue since <date>` â€” 460 review dates on this repository. Findings that exist and are shown\nnowhere are \"absence of evidence stays explicit\" failing at the last step.\n\n870 tests green (App 144, Core 726). Nine gates clean, all nine run by CI, on every branch.\nZero design-owned files.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Carry the five next steps: verify knowledge end to end, make the generation bump a control, tell the design session, surface knowledge health, and stop format breaks reaching main",
      "done_when": "The knowledge pane returns knowledge on a real repository; a gate fails when extraction changes without a generation bump and is proven to fire; CI runs on every branch with full history; health findings are requested; gates green; merged and published",
      "change": "cl-0101",
      "git": {
        "sha": "1b61599cc8be5ad753ad2725b61a78b27def6c69",
        "short": "1b61599cc",
        "branch": "session/phase3-pane-probes",
        "pushed": true
      }
    },
    {
      "id": "al-0258",
      "shortname": "knowledge-pane-capped-before-filtering-and-ci-branch-gap",
      "datetime": "2026-08-30T23:40:40Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps",
      "summary": "**The knowledge pane was STILL returning zero, and the reason was a second, independent defect.**\nChasing the render rather than the data found it: `Knowledge()` read the first 200 `has_type`\nassertions and filtered THOSE to knowledge — so on any real repository the 200 were code types in\nalphabetical order and the filter left nothing. MEASURED: **0 items on a workspace holding 468\nknowledge nodes**; now 50, the ceiling.\n\nThat is DC-035's shape one projection along — **a cap applied before a filter returns the wrong\nslice trimmed to the right shape, and nothing in the result says so.** I fixed exactly this in\n`GraphProjection` and did not look for siblings. Pinned by a test whose fixture puts 400 code types\nalphabetically ahead of the knowledge, which is the condition that hid it.\n\n**The generation bump is now a control instead of a memory.** `ExtractorGeneration` existed and was\ncomplete; using it was a thing somebody had to remember, and a full day of extractor changes shipped\nwithout it. `verify-extractor-generation.py` fails when anything under `src/AiDe.Core/Extraction/`\nchanged since the generation last did. Deliberately conservative — a comment-only edit trips it, and\nthe remedy is a one-line bump costing one re-index, where deciding which edits \"really\" change output\nis a judgement nobody can make reliably about a compiler-driven extractor. Observed failing by\nreplaying the real baseline: it lists the exact extractors that shipped unbumped.\n\nIt uses `git log -G`, not `-S`. `-S` counts occurrences, so replacing one generation value with\nanother leaves the count unchanged and the bump looks like no change at all.\n\n**CI only ran on `push: main` and `pull_request`.** The design session pushes to a long-lived feature\nbranch with no PR, so its work met no gate until it reached main — by which point it was merged. That\nis why an unbackticked `Status:` value arrived twice and a duplicate `DC-` id six times, each caught\nby whoever merged next rather than by the branch that introduced it. **A gate that only guards the\ndestination reports problems to the wrong person.** CI now runs on every branch.\n\n**And the new gate would not have fired in CI.** `actions/checkout@v4` is shallow by default, so the\nsearch for the last generation bump would have found nothing and the gate would have passed silently\n— the defect it exists to prevent, one layer up. `fetch-depth: 0` added.\n\n**`IsKnowledge` is proven across the pipe, with an assertion that can fail.** The first version I\nwrote was `Assert.Equal(n.IsKnowledge, n.IsKnowledge)` against a fixture with no knowledge in it —\na tautology over an empty set. The daemon fixture now holds a document beside the code, and the test\nasserts a known-knowledge node arrives with the flag set and a known-code node without it.\n\n**Knowledge health is computed and unrendered**, so it is a contract request rather than Core work:\n`owner not recorded`, `type not recorded`, `orphan`, `source location not recorded` and now `review\noverdue since <date>` — 460 review dates on this repository. Findings that exist and are shown\nnowhere are \"absence of evidence stays explicit\" failing at the last step.\n\n870 tests green (App 144, Core 726). Nine gates clean, all nine run by CI, on every branch.\nZero design-owned files.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Carry the five next steps: verify knowledge end to end, make the generation bump a control, tell the design session, surface knowledge health, and stop format breaks reaching main",
      "done_when": "The knowledge pane returns knowledge on a real repository; a gate fails when extraction changes without a generation bump and is proven to fire; CI runs on every branch with full history; health findings are requested; gates green; merged and published",
      "change": "cl-0101",
      "git": {
        "sha": "1b61599cc8be5ad753ad2725b61a78b27def6c69",
        "short": "1b61599cc",
        "branch": "session/phase3-pane-probes",
        "pushed": true
      }
    },
    {
      "id": "al-0162",
      "shortname": "cap-before-filter-sweep-and-branch-ci-notice",
      "datetime": "2026-08-31T00:12:19Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps",
      "summary": "**The cap-before-filter sweep, run properly this time.** Every bounded read in the projection service\nwas checked rather than spot-sampled: `Find` filters inside `SearchNodeIds`, `Describe` inside\n`AssertionsTouching`, `Impact` inside `OutgoingAssertions`, `Evidence` inside the cursor page â€” all\nfour apply the cap to rows the query has ALREADY filtered, which is the correct order and also the\ncheaper one, because the filter uses an index. `Knowledge` was the only place the order was inverted,\nand it was inverted for a reason worth naming: **its filter lived in C# rather than in the query.**\n\nThat gives the class a signature you can recognise without reading every projection: **a bounded read\nwhose `.Where(...)` is applied to the RESULT of the read rather than expressed in it.** If the filter\nis in the query the cap cannot be wrong; if it is in the caller, the cap chose the rows before anyone\nasked what was wanted. Recorded on DC-035 along with the one residual â€” `Knowledge` still reads each\nnode's touching assertions at 500 and splits them afterwards, so a document with ~495+ real links\nwould get an arbitrary 500 with no omission count. No repository measured comes close.\n\n**Measurements re-recorded after the knowledge work.** TheTerrace 24,058 assertions across 66 scopes,\nBioHacker 4,420 / 48, meridian 12,613 / 56 â€” every response still inside the frame.\n\n**The design session is told what changed under them, in their own file.** A new Â§4d says CI now runs\non every branch, that the next push to their branch will run all nine gates, and â€” because it will be\nnoisy the first time â€” names the two that will fire and how to satisfy them, with a worked example of\nthe accepted and rejected `Status:` forms. Neither is a new rule; both were always enforced, just not\nanywhere they could see them.\n\n**`review-by` on code: decided no, on the user's own rule.** The symmetry is tempting â€” 460 review\ndates on documents, so why not flag a stale class? Because nothing in a C# file, a Bicep template or\na SQL script DECLARES when it should next be read. A date could only be manufactured from\nlast-modified time or churn, and it would render identically to the dates documents actually declare.\nA reader could not tell the two apart, which is exactly the failure the no-inference note exists to\nprevent. A code artifact gets a review date the moment something writes one down; until then the\nabsence is accurate.\n\n**Still only verifiable by you:** whether the Knowledge chip lights up in the running app. Both known\ncauses are fixed and proven at every layer I can reach â€” the extractor, the store, the projection, and\nacross the daemon pipe â€” but I have not watched it render. It needs one re-index in the new build,\nbecause the generation bump makes that a rebuild rather than a reuse.\n\n870 tests green (App 144, Core 726). Nine gates clean, all nine run by CI, on every branch.\nZero design-owned files.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Sweep every projection for cap-before-filter, re-measure, tell the design session CI now gates their branch, and decide review-by for code",
      "done_when": "Every bounded read is checked and the result recorded with its signature; measurements re-recorded; the contract explains the CI change and the two gates that will fire; review-by on code decided with reasons; gates green; merged and published",
      "change": "cl-0102",
      "git": {
        "sha": "aec47955ceaddd0f6c598c27952696dfe4a36e87",
        "short": "aec47955c",
        "branch": "session/phase3-pane-probes",
        "pushed": true
      }
    },
    {
      "id": "al-0259",
      "shortname": "cap-before-filter-sweep-and-branch-ci-notice",
      "datetime": "2026-08-31T00:12:19Z",
      "session": "79f8657c-008d-44a7-b6f7-46c339804d70",
      "prompt": "do the next steps",
      "summary": "**The cap-before-filter sweep, run properly this time.** Every bounded read in the projection service\nwas checked rather than spot-sampled: `Find` filters inside `SearchNodeIds`, `Describe` inside\n`AssertionsTouching`, `Impact` inside `OutgoingAssertions`, `Evidence` inside the cursor page — all\nfour apply the cap to rows the query has ALREADY filtered, which is the correct order and also the\ncheaper one, because the filter uses an index. `Knowledge` was the only place the order was inverted,\nand it was inverted for a reason worth naming: **its filter lived in C# rather than in the query.**\n\nThat gives the class a signature you can recognise without reading every projection: **a bounded read\nwhose `.Where(...)` is applied to the RESULT of the read rather than expressed in it.** If the filter\nis in the query the cap cannot be wrong; if it is in the caller, the cap chose the rows before anyone\nasked what was wanted. Recorded on DC-035 along with the one residual — `Knowledge` still reads each\nnode's touching assertions at 500 and splits them afterwards, so a document with ~495+ real links\nwould get an arbitrary 500 with no omission count. No repository measured comes close.\n\n**Measurements re-recorded after the knowledge work.** TheTerrace 24,058 assertions across 66 scopes,\nBioHacker 4,420 / 48, meridian 12,613 / 56 — every response still inside the frame.\n\n**The design session is told what changed under them, in their own file.** A new §4d says CI now runs\non every branch, that the next push to their branch will run all nine gates, and — because it will be\nnoisy the first time — names the two that will fire and how to satisfy them, with a worked example of\nthe accepted and rejected `Status:` forms. Neither is a new rule; both were always enforced, just not\nanywhere they could see them.\n\n**`review-by` on code: decided no, on the user's own rule.** The symmetry is tempting — 460 review\ndates on documents, so why not flag a stale class? Because nothing in a C# file, a Bicep template or\na SQL script DECLARES when it should next be read. A date could only be manufactured from\nlast-modified time or churn, and it would render identically to the dates documents actually declare.\nA reader could not tell the two apart, which is exactly the failure the no-inference note exists to\nprevent. A code artifact gets a review date the moment something writes one down; until then the\nabsence is accurate.\n\n**Still only verifiable by you:** whether the Knowledge chip lights up in the running app. Both known\ncauses are fixed and proven at every layer I can reach — the extractor, the store, the projection, and\nacross the daemon pipe — but I have not watched it render. It needs one re-index in the new build,\nbecause the generation bump makes that a rebuild rather than a reuse.\n\n870 tests green (App 144, Core 726). Nine gates clean, all nine run by CI, on every branch.\nZero design-owned files.",
      "kind": "prompt",
      "skill": null,
      "tool": null,
      "actor": "claude-opus-5",
      "artifacts": [],
      "tags": [
        "phase-3"
      ],
      "outcome": "success",
      "goal": "Sweep every projection for cap-before-filter, re-measure, tell the design session CI now gates their branch, and decide review-by for code",
      "done_when": "Every bounded read is checked and the result recorded with its signature; measurements re-recorded; the contract explains the CI change and the two gates that will fire; review-by on code decided with reasons; gates green; merged and published",
      "change": "cl-0102",
      "git": {
        "sha": "aec47955ceaddd0f6c598c27952696dfe4a36e87",
        "short": "aec47955c",
        "branch": "session/phase3-pane-probes",
        "pushed": true
      }
    },
    {
      "id": "al-0163",
      "shortname": "specify-editor-surfaces",
      "datetime": "2026-08-31T00:13:18Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "we need the other surfaces: class diagram, code editor (read-only), prompt-editor; should I create parallel sessions or too much contention risk",
      "summary": "Specced editor-surfaces.md: read-only code viewer (render side of ADR-0018 NodeContentAsync, RenderKind=code; Monaco MIT / AvalonEdit MIT, read-only invariant, shortfall, walk) + prompt-draft editor (staged, persisted-with-layout, explicit one-way audited transfer to a named ready terminal session). Class diagram already specced (uml-erm-surfaces). Defined Design/Core ownership lanes so surfaces build against contracts not races â€” feeds the parallelization decision.",
      "kind": "skill",
      "skill": "specify",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/specs/editor-surfaces.md"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0260",
      "shortname": "specify-editor-surfaces",
      "datetime": "2026-08-31T00:13:18Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "we need the other surfaces: class diagram, code editor (read-only), prompt-editor; should I create parallel sessions or too much contention risk",
      "summary": "Specced editor-surfaces.md: read-only code viewer (render side of ADR-0018 NodeContentAsync, RenderKind=code; Monaco MIT / AvalonEdit MIT, read-only invariant, shortfall, walk) + prompt-draft editor (staged, persisted-with-layout, explicit one-way audited transfer to a named ready terminal session). Class diagram already specced (uml-erm-surfaces). Defined Design/Core ownership lanes so surfaces build against contracts not races — feeds the parallelization decision.",
      "kind": "skill",
      "skill": "specify",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/specs/editor-surfaces.md"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0164",
      "shortname": "ui-design-editor-surfaces",
      "datetime": "2026-08-31T00:19:52Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "ui-design the editor surfaces (read-only code viewer + prompt-draft editor)",
      "summary": "Self-contained dependency-free mockup docs/mockups/editor-surfaces.html for spec-editor-surfaces: read-only code viewer (read-only badge, Palenight highlighting, shortfall banner, walkable edge footer; hard states code/markdown/overflow/loading/empty/error/unsupported) + prompt-draft editor (staged, saved-with-layout, one-way audited Transfer to a named ready session, no-session disabled state, transferred confirmation). Review harness (surface/state/theme/reduced-motion) + in-artifact contrast audit (AA). Rubric critique names the renderer-choice spike as the highest-leverage next decision.",
      "kind": "skill",
      "skill": "ui-design",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/mockups/editor-surfaces.html",
        "docs/mockups/editor-surfaces.md"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0166",
      "shortname": "implement-prompt-draft-foundation",
      "datetime": "2026-08-31T00:34:04Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "do these next steps: design + build the prompt-editor",
      "summary": "Built + tested the prompt-draft foundation (spec-editor-surfaces US-ED5-ED7): PromptDraftViewModel (staged body, live ready-target gating, one-way named-target TransferAsync) with 7 headless tests; PromptDraftSurface UI (staged badge, textarea, target picker, Transfer with blocked reason, transferred confirmation) exposing a Configure wiring seam; factory kind 'prompt'. App.Tests 144->151. Decision note note-2026-08-30-prompt-draft-wiring captures the remaining shell wiring (ReadyPromptTargets, DispatchToAsync generalizing the focused dispatch, BindPromptDrafts, PromptDraftStore, newPromptDraft command) as a self-contained next increment.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/PromptDraftViewModel.cs",
        "src/AiDe.App/Workbench/PromptDraftSurface.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0167",
      "shortname": "define-architecture-code-viewer-renderer",
      "datetime": "2026-08-31T00:37:34Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "do these next steps: /define-architecture the viewer renderer choice (Monaco vs AvalonEdit spike)",
      "summary": "ADR-0019: read-only code viewer renders with native AvalonEdit (MIT), not Monaco-in-WebView2, decided by the repo's own documented WebView2 airspace/float-crash cost (ADR-0015) â€” a read-only viewer needs no VS-Code parity. Markdown via Markdig; rich content reuses the one existing canvas WebView2. Residual: a confirming AvalonEdit-read-only-highlighting PoC (float the pane, render Order.cs) before Phase-1 build; RoslynPad fallback for C# behind the same seam.",
      "kind": "skill",
      "skill": "define-architecture",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/adr/0019-code-viewer-renderer.md"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0261",
      "shortname": "define-architecture-code-viewer-renderer",
      "datetime": "2026-08-31T00:37:34Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "do these next steps: /define-architecture the viewer renderer choice (Monaco vs AvalonEdit spike)",
      "summary": "ADR-0019: read-only code viewer renders with native AvalonEdit (MIT), not Monaco-in-WebView2, decided by the repo's own documented WebView2 airspace/float-crash cost (ADR-0015) — a read-only viewer needs no VS-Code parity. Markdown via Markdig; rich content reuses the one existing canvas WebView2. Residual: a confirming AvalonEdit-read-only-highlighting PoC (float the pane, render Order.cs) before Phase-1 build; RoslynPad fallback for C# behind the same seam.",
      "kind": "skill",
      "skill": "define-architecture",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/adr/0019-code-viewer-renderer.md"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0169",
      "shortname": "investigate-knowledge-chip-zero",
      "datetime": "2026-08-31T00:49:51Z",
      "session": "phase3-pane-probes",
      "prompt": "this is what i see with your latest build: as far as I see knowledge still shows as 0 and this is post re-index (still 0). not sure if that answers your q. also do the next steps now",
      "summary": "Two defects, both proven from the user's own store. DC-044: RefreshScopeAsync's reuse check matched on the unchanged artifact revision and defeated the extractor-generation bump, so 66 scopes were visited and none re-read ('0 assertion(s)'); the deeper cause was the natural key, which could not represent the same bytes read by a better reader. Fixed by SourceRevision, which makes the reader part of a fact's identity. DC-045: the knowledge WAS written at 17:20:24 and the screenshot at 17:20:50 still read 0 because indexing told no surface it had happened; fixed by WorkspaceDataChanged. Both controls observed failing on the un-fixed code. Verified end-to-end on a copy of the user's real store: 66 of 66 re-extracted (0 reused), 23,672 to 24,124 assertions, graph returns 234 knowledge nodes.",
      "kind": "skill",
      "skill": "investigate",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/lessons/defect-classes.md"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0170",
      "shortname": "published-artifact-had-no-daemon",
      "datetime": "2026-08-31T00:57:37Z",
      "session": "phase3-pane-probes",
      "prompt": "also do the next steps now",
      "summary": "DC-046: the published shell shipped without the daemon it launches. DaemonPath() resolves BaseDirectory/daemon/AiDe.Daemon.exe; the copy target ran AfterTargets=Build into OutDir, and publish does not carry that across, so artifacts/app had AiDe.Daemon.exe flat at the root and nothing at daemon/. Every published build could open no workspace at all. Found while publishing, not by any test â€” and this session had reported 'published artifacts/app' many times on a command whose exit code was 0. Fixed with a publish-time copy target; verify-published-layout.py publishes to a scratch directory and checks the path read from source, observed failing on the un-fixed build, now in CI.",
      "kind": "skill",
      "skill": "investigate",
      "tool": null,
      "actor": null,
      "artifacts": [
        "tools/verify-published-layout.py"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0262",
      "shortname": "published-artifact-had-no-daemon",
      "datetime": "2026-08-31T00:57:37Z",
      "session": "phase3-pane-probes",
      "prompt": "also do the next steps now",
      "summary": "DC-046: the published shell shipped without the daemon it launches. DaemonPath() resolves BaseDirectory/daemon/AiDe.Daemon.exe; the copy target ran AfterTargets=Build into OutDir, and publish does not carry that across, so artifacts/app had AiDe.Daemon.exe flat at the root and nothing at daemon/. Every published build could open no workspace at all. Found while publishing, not by any test — and this session had reported 'published artifacts/app' many times on a command whose exit code was 0. Fixed with a publish-time copy target; verify-published-layout.py publishes to a scratch directory and checks the path read from source, observed failing on the un-fixed build, now in CI.",
      "kind": "skill",
      "skill": "investigate",
      "tool": null,
      "actor": null,
      "artifacts": [
        "tools/verify-published-layout.py"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0171",
      "shortname": "implement-prompt-draft-wiring",
      "datetime": "2026-08-31T03:31:58Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "do the next steps (finish the prompt-editor shell wiring)",
      "summary": "Wired the prompt-draft surface end-to-end: WorkbenchShell.ReadyPromptTargets (live ready terminals), DispatchToAsync (named-session dispatch via a shared DispatchToSurfaceAsync refactored out of the focused-terminal path; success = PtyWriteAccepted), BindPromptDrafts (Configure each surface post-render, called from BindTerminalAttention), PromptDraftStore (cross-restart body persistence), and a workbench.newPromptDraft command (catalog+menu+controller). App.Tests 151->155, Core.Tests 730 green, launch smoke green. The prompt-editor is now reachable (Terminal menu / Ctrl+K,D) and functional.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/WorkbenchShell.cs",
        "src/AiDe.App/Workbench/PromptDraftStore.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0172",
      "shortname": "graph-payload-too-large",
      "datetime": "2026-08-31T03:37:10Z",
      "session": "phase3-pane-probes",
      "prompt": "before re-index i get this message when opening theterrace workspace: The graph could not be loaded: ipc.payload_too_large: the response is 1,176,341 bytes and one message carries at most 1,048,576; ask for less of it",
      "summary": "DC-047: the byte budget was checked on the inner payload and enforced on the framed one. IpcResponse.Payload is a string, so the projection's JSON is escaped into the envelope â€” measured inflation 1.56-1.57x on every payload. A 727,244-byte graph inside its 768 KiB budget reached 1,137,104 bytes and was refused; reproduced exactly. The graph now measures FramedCost (serialised, escaped, enveloped) and shrinks until that fits with 64 KiB headroom; the row-wise budget dropped to 480 KiB with a test asserting budget*2 <= frame so the assumed worst case cannot drift. Also fixed: the shrink applied one proportional correction and returned without re-checking. Three fixtures failed to reproduce before one did â€” the discriminating window was found by measuring, not by picking big numbers.",
      "kind": "skill",
      "skill": "investigate",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/lessons/defect-classes.md"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0263",
      "shortname": "graph-payload-too-large",
      "datetime": "2026-08-31T03:37:10Z",
      "session": "phase3-pane-probes",
      "prompt": "before re-index i get this message when opening theterrace workspace: The graph could not be loaded: ipc.payload_too_large: the response is 1,176,341 bytes and one message carries at most 1,048,576; ask for less of it",
      "summary": "DC-047: the byte budget was checked on the inner payload and enforced on the framed one. IpcResponse.Payload is a string, so the projection's JSON is escaped into the envelope — measured inflation 1.56-1.57x on every payload. A 727,244-byte graph inside its 768 KiB budget reached 1,137,104 bytes and was refused; reproduced exactly. The graph now measures FramedCost (serialised, escaped, enveloped) and shrinks until that fits with 64 KiB headroom; the row-wise budget dropped to 480 KiB with a test asserting budget*2 <= frame so the assumed worst case cannot drift. Also fixed: the shrink applied one proportional correction and returned without re-checking. Three fixtures failed to reproduce before one did — the discriminating window was found by measuring, not by picking big numbers.",
      "kind": "skill",
      "skill": "investigate",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/lessons/defect-classes.md"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0173",
      "shortname": "spike-avalonedit-read-only-viewer",
      "datetime": "2026-08-31T03:39:19Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "do these next steps (AvalonEdit read-only highlighting PoC, ADR-0019 residual)",
      "summary": "Ran the ADR-0019 residual spike (spikes/avalonedit-viewer, AvalonEdit 6.* on net10.0-windows, now disposed): read-only viewer works (IsReadOnly blocks user edits, line numbers, highlighting, real 88-line file); base type Control = pure WPF, no HwndHost/airspace (the ADR-0015 failure mode absent by construction); 21 built-in highlightings cover .cs/.py/.js/.sql, .ts/.bicep degrade to plain (US-ED2 accepted). Residual cleared; RoslynPad fallback not needed. Updated ADR-0019. Also backlogged: adopt Central Package Management (coordinated, touches Core .csproj).",
      "kind": "skill",
      "skill": "define-architecture",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/adr/0019-code-viewer-renderer.md"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0176",
      "shortname": "define-architecture-class-diagram",
      "datetime": "2026-08-31T04:18:19Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "do these next steps autonomously overnight (class-diagram architecture + build)",
      "summary": "ADR-0020: class diagram is an App-side type-hierarchy view derived from the existing graph (class/interface nodes + inherits->generalization, implements->realization), dependency-free and Core-ungated for Phase 1; members + notation-valid Mermaid deferred to Phase 2 gated on a Core has_member enhancement (handed off). Built the tested ClassHierarchyModel (pure projection, 7 tests). App.Tests 155->162.",
      "kind": "skill",
      "skill": "define-architecture",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/adr/0020-class-diagram-architecture.md",
        "src/AiDe.App/Workbench/ClassHierarchyModel.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0177",
      "shortname": "implement-class-diagram-surface",
      "datetime": "2026-08-31T04:27:07Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "overnight: build the class diagram (ADR-0020 Phase 1)",
      "summary": "Built + wired the class-diagram surface end-to-end (ADR-0020 Phase 1): ClassDiagramSurface (native WPF, no WebView2) renders the type hierarchy as styled cards (class/Â«interfaceÂ», inherits/implements relations, member-less disclosure, external-relation count); ShowGraph builds it via ClassHierarchyModel. Wired: factory kind 'classdiagram', workbench.newClassDiagram command (View menu / Ctrl+K,M), controller callback, shell NewClassDiagramRequested + BindClassDiagrams (loads the graph live). 2 surface tests. App.Tests 162->164, Core.Tests 735, launch smoke green. Second Design command in the Core catalog (bumped _View tripwire 4->5).",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/ClassDiagramSurface.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0264",
      "shortname": "implement-class-diagram-surface",
      "datetime": "2026-08-31T04:27:07Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "overnight: build the class diagram (ADR-0020 Phase 1)",
      "summary": "Built + wired the class-diagram surface end-to-end (ADR-0020 Phase 1): ClassDiagramSurface (native WPF, no WebView2) renders the type hierarchy as styled cards (class/«interface», inherits/implements relations, member-less disclosure, external-relation count); ShowGraph builds it via ClassHierarchyModel. Wired: factory kind 'classdiagram', workbench.newClassDiagram command (View menu / Ctrl+K,M), controller callback, shell NewClassDiagramRequested + BindClassDiagrams (loads the graph live). 2 surface tests. App.Tests 162->164, Core.Tests 735, launch smoke green. Second Design command in the Core catalog (bumped _View tripwire 4->5).",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/ClassDiagramSurface.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0179",
      "shortname": "implement-code-viewer-component",
      "datetime": "2026-08-31T04:33:35Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "overnight: build the read-only code viewer (ADR-0018/0019)",
      "summary": "Built the read-only code viewer component + client content seam (ADR-0018/0019). Added AvalonEdit (MIT, ADR-0019-sanctioned) to the App. CodeViewerView: native read-only TextEditor, highlight-by-language (C#/py/js/sql built-in, unknown degrades to plain per US-ED2), shortfall banner, no-content fallback. INodeContentSource seam mirrors ADR-0018's NodeContentAsync; MockNodeContentSource stands in (labelled sample, no file reads per DC-022) until Core ships the real query. 6 tests. App.Tests 164->170, launch smoke green. Component is tested + staged; live wiring is a one-line substitution when Core ships NodeContentAsync.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/CodeViewerView.cs",
        "src/AiDe.App/Workbench/NodeContentSource.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0180",
      "shortname": "class-diagram-context-grouping",
      "datetime": "2026-08-31T04:37:00Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "overnight: make the class diagram scannable",
      "summary": "Class diagram groups its type cards by context (namespace/bounded context) with per-group headers + counts, so a large type hierarchy is scannable; types with no context fall into a trailing group. ClassTypeNode carries Context (from the graph node). App.Tests 170->171.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/ClassDiagramSurface.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0181",
      "shortname": "ipc-payload-is-json-not-text-about-json",
      "datetime": "2026-08-31T04:38:36Z",
      "session": "phase3-pane-probes",
      "prompt": "the knowledge works now. do the next steps autonomously while i go to bed",
      "summary": "IPC version 3 carries the payload as JSON instead of a string holding JSON text, removing the double-encoding behind DC-047 rather than managing it. Framing overhead fell from 1.57x to 78 bytes; on TheTerrace the canvas request went from 1,000 nodes/283 knowledge to 1,500/340, and a 5,000 request from 706 to 2,792 nodes/729 knowledge. IpcPayload.Read accepts either encoding so a version-2 peer still parses; a stale daemon is refused with a message naming the process rather than the protocol. DC-048 registered: the shrink loop's minimum step existed to guarantee termination and became the answer, so asking for more returned less; fixed with bounded midpoint recovery. DC-023 recurred in ShellBootstrapTests, which chose Release whenever a Release directory existed â€” now content-compared against the tests' own AiDe.Core.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.Core/Ipc/IpcContract.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0265",
      "shortname": "ipc-payload-is-json-not-text-about-json",
      "datetime": "2026-08-31T04:38:36Z",
      "session": "phase3-pane-probes",
      "prompt": "the knowledge works now. do the next steps autonomously while i go to bed",
      "summary": "IPC version 3 carries the payload as JSON instead of a string holding JSON text, removing the double-encoding behind DC-047 rather than managing it. Framing overhead fell from 1.57x to 78 bytes; on TheTerrace the canvas request went from 1,000 nodes/283 knowledge to 1,500/340, and a 5,000 request from 706 to 2,792 nodes/729 knowledge. IpcPayload.Read accepts either encoding so a version-2 peer still parses; a stale daemon is refused with a message naming the process rather than the protocol. DC-048 registered: the shrink loop's minimum step existed to guarantee termination and became the answer, so asking for more returned less; fixed with bounded midpoint recovery. DC-023 recurred in ShellBootstrapTests, which chose Release whenever a Release directory existed — now content-compared against the tests' own AiDe.Core.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.Core/Ipc/IpcContract.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0182",
      "shortname": "overnight-surfaces-note",
      "datetime": "2026-08-31T04:38:54Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "overnight: record progress + morning next-steps",
      "summary": "Decision note note-2026-08-30-overnight-surfaces summarizing the overnight three-surfaces run (class diagram Phase 1 shipped reachable; prompt editor shipped; code viewer component+seam staged, Core-gated on NodeContentAsync) + morning next-steps + the two Core-catalog command touches.",
      "kind": "manual",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/notes/note-2026-08-30-overnight-surfaces.md"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0183",
      "shortname": "daemon-state-leaked-into-localappdata",
      "datetime": "2026-08-31T04:54:35Z",
      "session": "phase3-pane-probes",
      "prompt": "do the next steps autonomously while i go to bed",
      "summary": "DC-049: the daemon derived its state directory from LocalAppData with no way for a caller to say otherwise, so every test that launched one wrote into the user's real profile â€” 12 per Core run, 2,695 over four days, 468 MB, all but one an empty or fixture-sized store. The daemon now takes --data and the shell passes the directory it already computed (removing a second derivation of one value). Measured after: 0 leaked per full-suite run. Also: the cleanup tool's own read-only opens created 5,390 sqlite sidecar files and changed its second run's answer â€” a read that writes is not a read; it now opens immutable where there is no WAL to miss.",
      "kind": "skill",
      "skill": "investigate",
      "tool": null,
      "actor": null,
      "artifacts": [
        "tools/list-workspace-stores.py"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0266",
      "shortname": "daemon-state-leaked-into-localappdata",
      "datetime": "2026-08-31T04:54:35Z",
      "session": "phase3-pane-probes",
      "prompt": "do the next steps autonomously while i go to bed",
      "summary": "DC-049: the daemon derived its state directory from LocalAppData with no way for a caller to say otherwise, so every test that launched one wrote into the user's real profile — 12 per Core run, 2,695 over four days, 468 MB, all but one an empty or fixture-sized store. The daemon now takes --data and the shell passes the directory it already computed (removing a second derivation of one value). Measured after: 0 leaked per full-suite run. Also: the cleanup tool's own read-only opens created 5,390 sqlite sidecar files and changed its second run's answer — a read that writes is not a read; it now opens immutable where there is no WAL to miss.",
      "kind": "skill",
      "skill": "investigate",
      "tool": null,
      "actor": null,
      "artifacts": [
        "tools/list-workspace-stores.py"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0184",
      "shortname": "knowledge-search-saw-only-the-first-200",
      "datetime": "2026-08-31T05:03:08Z",
      "session": "phase3-pane-probes",
      "prompt": "do the next steps autonomously while i go to bed",
      "summary": "DC-035's third instance, found by sweeping the CALLERS rather than the queries â€” the earlier sweep checked that each reader query expressed its filter in SQL, and all of them did, but the knowledge caller still capped at 200 ids in id order and matched the term in memory afterwards. On TheTerrace that meant searching only the alphabetically first 200 of 1,255 knowledge nodes; a document sorting later was reported as not existing. StoreReader.KnowledgeNodes now applies term and type in the query and counts the total over the same filtered set, so omitted is measured against what matched. Measured after: spec 34, adr 9, ui 31, type=adr 7, and 707 honestly reported as omitted at a limit of 50. Control puts the only match past the cap on purpose; observed failing 4 of 4.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "tests/AiDe.Core.Tests/KnowledgeSearchSeesEveryDocumentTests.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0267",
      "shortname": "knowledge-search-saw-only-the-first-200",
      "datetime": "2026-08-31T05:03:08Z",
      "session": "phase3-pane-probes",
      "prompt": "do the next steps autonomously while i go to bed",
      "summary": "DC-035's third instance, found by sweeping the CALLERS rather than the queries — the earlier sweep checked that each reader query expressed its filter in SQL, and all of them did, but the knowledge caller still capped at 200 ids in id order and matched the term in memory afterwards. On TheTerrace that meant searching only the alphabetically first 200 of 1,255 knowledge nodes; a document sorting later was reported as not existing. StoreReader.KnowledgeNodes now applies term and type in the query and counts the total over the same filtered set, so omitted is measured against what matched. Measured after: spec 34, adr 9, ui 31, type=adr 7, and 707 honestly reported as omitted at a limit of 50. Control puts the only match past the cap on purpose; observed failing 4 of 4.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "tests/AiDe.Core.Tests/KnowledgeSearchSeesEveryDocumentTests.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0185",
      "shortname": "class-diagram-search-filter",
      "datetime": "2026-08-31T13:07:21Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "do whatever steps you are not gated on",
      "summary": "Added a search/filter to the class diagram: a header search box (shown when >12 types) filters the type cards by name/id case-insensitively via a pure ClassHierarchyModel.Filter (relations recomputed, filtered-out targets disclosed as external); header shows 'X of Y match'. 3 filter tests. App.Tests 171->174, launch smoke green.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/ClassHierarchyModel.cs",
        "src/AiDe.App/Workbench/ClassDiagramSurface.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0186",
      "shortname": "class-diagram-loading-error-states",
      "datetime": "2026-08-31T13:10:57Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "do whatever steps you are not gated on",
      "summary": "Class diagram gains loading + error states (U9): PopulateClassDiagramsAsync shows Loading before the graph fetch and an explicit error (never a misleading empty 'no classes') on failure; BindClassDiagrams now first-load-only to avoid reload flicker on every render. +1 test. App.Tests 174->175.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/ClassDiagramSurface.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0187",
      "shortname": "prompt-draft-transfer-failure-feedback",
      "datetime": "2026-08-31T13:12:56Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "do whatever steps you are not gated on",
      "summary": "Prompt editor: a rejected transfer (session not ready / write not accepted) now shows an explicit 'did not go through â€” try again' message instead of silently doing nothing (U9, no silent no-op); the draft stays editable/transferable. VM retry behaviour already tested.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/PromptDraftSurface.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0268",
      "shortname": "prompt-draft-transfer-failure-feedback",
      "datetime": "2026-08-31T13:12:56Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "do whatever steps you are not gated on",
      "summary": "Prompt editor: a rejected transfer (session not ready / write not accepted) now shows an explicit 'did not go through — try again' message instead of silently doing nothing (U9, no silent no-op); the draft stays editable/transferable. VM retry behaviour already tested.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/PromptDraftSurface.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0190",
      "shortname": "node-content-query-unblocks-the-code-viewer",
      "datetime": "2026-08-31T13:26:30Z",
      "session": "phase3-pane-probes",
      "prompt": "the ui sessions is blocked on work from this session we should prioritize that work",
      "summary": "ADR-0018's NodeContentAsync shipped, unblocking the design session's code viewer (their priority 1). The gap underneath: nothing recorded WHERE a scope's files are â€” provenance paths are scope-relative and no fact said where the scope was â€” so scopes now emit declared_at and the generation moved to 2026-08-31.1. The projection resolves scope location plus provenance path, confines the result under the workspace root, bounds the read at 256 KB with an honest shortfall, and names the render kind so the reader does not guess. Measured on TheTerrace: 1,158 Code, 340 Text, 2 None of 1,500 drawn nodes. DC-035 recurred a fourth time in this very code â€” the resolver filtered a CAPPED neighbour list, so the most connected type in the workspace reported no source while small types worked; fixed with StoreReader.DeclaringAssertion. The control could not reproduce it twice and now asserts its own preconditions.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.Core/Projections/NodeContent.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0269",
      "shortname": "node-content-query-unblocks-the-code-viewer",
      "datetime": "2026-08-31T13:26:30Z",
      "session": "phase3-pane-probes",
      "prompt": "the ui sessions is blocked on work from this session we should prioritize that work",
      "summary": "ADR-0018's NodeContentAsync shipped, unblocking the design session's code viewer (their priority 1). The gap underneath: nothing recorded WHERE a scope's files are — provenance paths are scope-relative and no fact said where the scope was — so scopes now emit declared_at and the generation moved to 2026-08-31.1. The projection resolves scope location plus provenance path, confines the result under the workspace root, bounds the read at 256 KB with an honest shortfall, and names the render kind so the reader does not guess. Measured on TheTerrace: 1,158 Code, 340 Text, 2 None of 1,500 drawn nodes. DC-035 recurred a fourth time in this very code — the resolver filtered a CAPPED neighbour list, so the most connected type in the workspace reported no source while small types worked; fixed with StoreReader.DeclaringAssertion. The control could not reproduce it twice and now asserts its own preconditions.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.Core/Projections/NodeContent.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0192",
      "shortname": "investigate-empty-class-diagram",
      "datetime": "2026-08-31T13:41:24Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "class diagram shows empty 'No classes or interfaces in view' while the graph is fully populated with TheTerrace open",
      "summary": "Root cause: SurfaceChrome.WrapAsIsland frames non-windowed panes in a Border, so ContentFor(id).OfType<ClassDiagramSurface>() found the wrapper not the surface and the pane never populated. Class-level defect affecting all wrapped surfaces bound by type (contexts, joins, prompt, classdiagram, codeviewer). Fix: unwrap-aware WorkbenchAdapter.SurfaceContent<T>; routed all five binds through it; added STA regression test. Also landed on-demand code-viewer wiring (View/Ctrl+K,U).",
      "kind": "skill",
      "skill": "investigate",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/WorkbenchAdapter.cs",
        "src/AiDe.App/Workbench/WorkbenchShell.cs"
      ],
      "tags": [
        "class-diagram",
        "surface-chrome",
        "regression"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0193",
      "shortname": "has-member-canvas-count-and-build-output",
      "datetime": "2026-08-31T13:48:54Z",
      "session": "phase3-pane-probes",
      "prompt": "do these next steps. also give me a table of all the extractors we have to build and status",
      "summary": "Design blockers 2 and 3 cleared. has_member emits UML-formatted members per type as an ATTRIBUTE so members never become graph nodes — 9,854 across 1,425 of 1,428 types on TheTerrace, capped at 40 with members_truncated carrying the real count (7 types reach it). CanvasNode gained Count with a producer: CanvasGraphViewModel.OverviewAsync returns group super-nodes in the shape the canvas already draws. Two older classes recurred: DC-022 (two skip lists disagreeing about build output — artifacts was in UnanalysedLanguages and not in scope discovery, so Blazor's published JavaScript was indexed as source, 3 scopes of 67) and DC-042 (CheckCompactionNeeded complete, tested, called by nothing — the daemon now calls it at startup). Store: half of it is superseded at 2 generations while the threshold is 8, because the threshold is tuned for latency and the symptom is size; that decision is still open.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.Core/Extraction/CSharpExtractor.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0270",
      "shortname": "canvas-overview-affordance",
      "datetime": "2026-08-31T13:51:31Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "when deep in the graph there is no way to zoom back up to the whole graph",
      "summary": "Added an Overview affordance to the graph canvas: a header button + Home key that reload the whole-graph overview (rootId null), clearing the drill-down history; disabled at the overview. Back climbed history one node at a time with no single gesture to the top. New node.overview host message -> RefreshAsync(null). 2 structural tests.",
      "kind": "skill",
      "skill": "implement",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/CanvasPage.cs",
        "src/AiDe.App/Workbench/CanvasSurface.cs"
      ],
      "tags": [
        "canvas",
        "navigation",
        "ux"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0272",
      "shortname": "handoff-panel-reorder-and-search",
      "datetime": "2026-08-31T13:59:28Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "panels reorder when adding tabs; search too narrow (needs content/keyword search, grep files)",
      "summary": "Root-caused both and deferred to coordinated work: (2) native AvalonDock drag is never reconciled into the owned Layout model, so rebuild-from-model on every add reverts arrangement (App-only reverse-sync, deferred as UX-critical/headless-untestable); (4) graph search filters loaded node labels client-side only, content/keyword search needs Core FindAsync breadth + file grep is a new Core capability (DC-022). Decision note + session-contracts 4h handoff.",
      "kind": "skill",
      "skill": "investigate",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "docs/notes/note-20260831-panel-reorder-and-search-breadth.md",
        "docs/collaboration/session-contracts.md"
      ],
      "tags": [
        "layout",
        "search",
        "handoff"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0274",
      "shortname": "compaction-decided-python-boundary-extractor-roadmap",
      "datetime": "2026-08-31T14:57:37Z",
      "session": "phase3-pane-probes",
      "prompt": "do the next steps you listed above. also lets prioritize all extractors moving forward as well",
      "summary": "Compaction decided on evidence: nothing in the product reads a superseded generation (every read composes with the latest filter; the one by-generation reader is handed the latest), every committed snapshot is complete so the newest always renders, and measurement showed 53.3 MB of which 27.9 MB was dead at just 2 generations — under the old threshold of 8, which was tuned for latency while the symptom is size. Threshold 1, retain 1, run at daemon startup before the store opens: 1.09s to halve a real store, 1-34ms to decide there is nothing to do. DC-050: the Python disclosure conflated a boundary with a gap — 246 'unresolved' imports were all standard library, and I had ranked it the top coverage gap on the number alone; after separating them, 2 genuine unknowns. The standard library is now counted rather than drawn, matching what the C# extractor already did for the BCL. The generated stdlib set initially dropped __future__ (26 false unknowns), caught by re-measuring. docs/plans/extractor-roadmap.md now holds every extractor, its measured coverage, what is not built, and the priority order.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/plans/extractor-roadmap.md"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0275",
      "shortname": "graph-lod-grouped-view",
      "datetime": "2026-08-31T14:59:42Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "graph is so big; do the next steps (LOD semantic-zoom render)",
      "summary": "Implemented the graph semantic-zoom LOD as an additive Group toggle. Core: added CanvasGraphViewModel.GroupAsync(groupId) drilling a cluster to its members via GraphQuery.GroupId (shared membership with the overview, avoids DC-022). App: Group button + group super-node rendering (sized/labelled by Count) + group.open/graph.grouped messages routed through one GraphSource seam by sentinel roots (LoadRouted -> OverviewAsync/GroupAsync/LoadAsync); group-aware Back/Overview history. Flat overview unchanged as default. Core+2, App+1 tests.",
      "kind": "skill",
      "skill": "implement",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "src/AiDe.Core/Presentation/CanvasGraphViewModel.cs",
        "src/AiDe.App/Workbench/CanvasPage.cs",
        "src/AiDe.App/Workbench/WorkbenchShell.cs"
      ],
      "tags": [
        "graph",
        "lod",
        "semantic-zoom"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0276",
      "shortname": "panel-reorder-reverse-sync",
      "datetime": "2026-08-31T16:16:41Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "adding new tabs is re-ordering panels (native drag not preserved on redraw)",
      "summary": "Fixed panel reorder-on-add via a fail-safe view->model reconcile. WorkbenchAdapter.ReadLayoutFromView() maps AvalonDock's current tree (LayoutPanel/LayoutDocumentPaneGroup/LayoutDocumentPane) back to the owned Layout, using the existing ILayoutService.Restore seam; returns null on any shape it cannot map losslessly (floating/anchorable/empty/unknown surface) or if the surface set changed, so it degrades to today's revert, never corruption. Shell reconciles before each surface-add command. Round-trip + fail-safe tests (App+2). Needs live drag validation.",
      "kind": "skill",
      "skill": "implement",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/WorkbenchAdapter.cs",
        "src/AiDe.App/Workbench/WorkbenchShell.cs"
      ],
      "tags": [
        "layout",
        "avalondock",
        "reconcile"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0278",
      "shortname": "new-view-opens-in-focused-stack",
      "datetime": "2026-08-31T16:37:25Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "I was focused in the right window but new class diagram opened in the left; expected it where I had focus",
      "summary": "New class-diagram/code-viewer panes now open in the FOCUSED stack. Added WorkbenchAdapter.ActiveSurfaceId (AvalonDock active/selected document ContentId); shell TargetStackForNewView() prefers the stack holding the active surface, falling back to the canvas stack then any. Test: ActiveSurfaceId after render is a real model surface or null. App+1.",
      "kind": "skill",
      "skill": "implement",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/WorkbenchAdapter.cs",
        "src/AiDe.App/Workbench/WorkbenchShell.cs"
      ],
      "tags": [
        "layout",
        "focus",
        "ux"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0280",
      "shortname": "knowledge-prose-links-and-typescript-precision",
      "datetime": "2026-08-31T16:45:56Z",
      "session": "phase3-pane-probes",
      "prompt": "do the next steps you listed above. tackle both Roadmap #1 and Roadmap #2 in parallel",
      "summary": "Both roadmap items shipped, built in parallel in isolated worktrees. Knowledge now emits links_to for prose markdown links whose target resolves to a document that declares an id — both ends observed, no inference; 42 edges from 19 documents, and a real find: 218 prose links naming markdown files that are not there, rotted cross-references nothing had ever reported. Headings, glossary terms and code references were measured and deliberately NOT extracted (0 of 26,924 inline code spans exactly match a node id; headings would push documents past Describe's 50-row ceiling). TypeScript: of 14 import edges, 12 were invented — the matcher had no statement anchor, so prose and compiled JS became imports, including @playwright/test which is a line of Playwright's own code-generation template and which my brief had wrongly called real. Now 0 invented, and removing the export gate revealed 22 functions and 2 classes that were never visible. I then found knowledge scopes nest (2,368 node_class rows for 877 documents, ~2.7x), fixed it, measured that it cost 30 of 42 prose links, and REVERTED — registered as DC-051 with both halves as one roadmap item.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/plans/extractor-roadmap.md"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0281",
      "shortname": "class-diagram-visual-render",
      "datetime": "2026-08-31T16:46:48Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "doesnt look like a diagram (class diagram showed a list, not boxes+arrows)",
      "summary": "The class diagram now renders as an actual diagram: types as boxes, generalization (solid) and realization (dashed) as UML connectors with a hollow triangle at the base, laid out in inheritance ranks (bases on top, arrows pointing up). A Diagram/List toggle (defaults to Diagram); the diagram caps to the DiagramMax(40) most-connected types with a note, and the list stays for browsing large hierarchies. Dependency-free native WPF Canvas. App+1 test.",
      "kind": "skill",
      "skill": "implement",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/ClassDiagramSurface.cs"
      ],
      "tags": [
        "class-diagram",
        "uml",
        "ux"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0283",
      "shortname": "bounded-describe-keeps-identity",
      "datetime": "2026-08-31T17:01:53Z",
      "session": "phase3-pane-probes",
      "prompt": "do the next steps you listed above",
      "summary": "DC-052: a bounded read ordered for determinism rather than importance. AssertionsTouching capped at 50 ordered subject/predicate/object, so a node with more facts than the cap lost its own has_type, node_class, owned_by and review_by to its own links, in alphabetical order — which facts survived depended on how the node was named. Measured: 12 of 877 knowledge documents were already over the ceiling, and it is why the knowledge reader correctly declined to emit headings, working around a defect one layer down. Its sibling was the node-content reader filtering the same capped list, which made AppDbContext report no source. Fixed with EvidencePredicates.Identity sorting first, then the node's own facts, then inbound, alphabetical within each band so determinism and the omission count are untouched. Control observed failing with the exact message it describes.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.Core/Store/StoreReader.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0284",
      "shortname": "class-diagram-uml-boxes-collapse",
      "datetime": "2026-08-31T17:02:57Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "study UML class diagrams — this renders relationships not a UML class diagram; also allow collapsing interfaces (too broad with many interfaces)",
      "summary": "Class diagram is now real UML: three-compartment boxes (name compartment over a member compartment, divided by a rule) instead of name-only nodes, plus a 'Hide interfaces' collapse toggle that drops interface types and their realization edges (recounted external). Member compartment is empty pending a Core has_member query — handed off in session-contracts 4j (bulk MembersAsync). App+1 test (hide-interfaces).",
      "kind": "skill",
      "skill": "implement",
      "tool": "Copilot CLI",
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/ClassDiagramSurface.cs",
        "docs/collaboration/session-contracts.md"
      ],
      "tags": [
        "class-diagram",
        "uml",
        "collapse"
      ],
      "outcome": "success"
    },
    {
      "id": "al-0286",
      "shortname": "classdiagram-uml-members",
      "datetime": "2026-08-31T17:27:09Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "the class diagram renders BUT study UML class diagrams, this seems to render relationships as opposed to building a UML class diagram which is what i am looking for",
      "summary": "Class diagram now renders true UML three-compartment boxes with attribute/operation members: enriched DescribeResult with Members/MembersDeclared (read via OutgoingAssertions, has_member/members_truncated), ClassDiagramSurface fills each box compartment via DescribeAsync per drawn type (render-gen guarded, attr/op split). +3 Core tests (823), +2 App tests (186).",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.Core/Projections/ProjectionService.cs",
        "src/AiDe.App/Workbench/ClassDiagramSurface.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0287",
      "shortname": "knowledge-dedup-and-csharp-call-edges",
      "datetime": "2026-08-31T17:29:38Z",
      "session": "phase3-pane-probes",
      "prompt": "do the next steps you listed above. tackle both Roadmap #1 and Roadmap #2 in parallel",
      "summary": "Both roadmap items shipped in parallel worktrees and integrated. Knowledge: read widely, emit narrowly — a workspace-wide document map built once per revision and cached (the same shape as WorkspaceModules) lets each document be extracted by exactly one scope while a prose link resolves anywhere; 878 documents preserved, node_class rows 2,371 to 878, links_to held at 42, index time slightly faster. The outside-scope boundary was MOVED to outside-workspace rather than deleted, with a mirror test so 'moved' cannot be confused with 'removed'. C#: measured 10,451 candidate call edges before designing, ruled out method-level on payload arithmetic (5,054 new nodes against a frame with 8% headroom), shipped 1,492 type-level edges of which 72% are not already depends_on. Combined, the two are better than either alone: dedup freed the budget calls consume, so the canvas draws 1,500 nodes with 195,896 bytes of headroom where calls alone left 18,496. DC-053 registered: git stash is repository-global, so a worktree does not isolate it — the two agents collided on the stash stack and both preserved rather than discarded what they did not recognise.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/plans/extractor-roadmap.md"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0290",
      "shortname": "graph-edge-kind-filter-and-wt13",
      "datetime": "2026-08-31T20:47:40Z",
      "session": "phase3-pane-probes",
      "prompt": "do the next steps you listed above",
      "summary": "The graph payload constraint, addressed at the projection. Measured first: edges are 702,425 of 852,680 bytes of the canvas default — 82% — and depends_on plus calls are 74% of the edges. GraphQuery gained ExcludeEdges, applied before the cap so excluded bytes become nodes: asking for 5,000 without calls and depends_on returns 2,992 nodes with nothing omitted, the entire workspace, in 602,364 bytes. Exclusions rather than an include list because an include list is a caller restating the extractors' vocabulary and goes stale silently; a misspelled exclusion is inert and a test says so. Which kinds a first view should draw is a UX decision handed to the design session with the numbers. WT13 added to the pack's worktree discipline: a worktree isolates the working tree and index, not the repository — refs/stash is one stack for the whole repo, so git stash is not a worktree-local tool, with the self-verification checklist updated.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        ".claude/knowledge/session-worktree-discipline.md"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0291",
      "shortname": "classdiagram-variable-height-uml",
      "datetime": "2026-08-31T20:54:55Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "yes the fill worked, i think variable height sizing IS worth the layout rework. I think you still need to really understand UML symbols and styles for class diagrams and sequence diagrams as this is an initial start but i want full fidelity UML diagram",
      "summary": "Class diagram reworked to full UML classifier boxes: variable box height (each measured and sized to its members), three real compartments (name/attributes/operations) separated by rules, interface stereotype + italic interface name, monospace member lines, up to 15/compartment + overflow footer. Member cache + prefetch-then-rerender replaces per-box async fill. +1 App test (187) proving member-rich boxes are taller. Roadmap note + Core ask for ordered-call data (sequence diagrams) filed.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/ClassDiagramSurface.cs",
        "docs/notes/uml-diagram-fidelity-roadmap.md"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0293",
      "shortname": "graph-edge-filter-wt13-raw-sql-counted",
      "datetime": "2026-08-31T20:59:01Z",
      "session": "phase3-pane-probes",
      "prompt": "do the next steps you listed above",
      "summary": "Four of five next steps done. GraphQuery gained ExcludeEdges after measuring that edges are 82% of the canvas payload: excluding calls and depends_on returns the entire 2,992-node workspace with nothing omitted where the default omits 1,492. WT13 added to the pack's worktree discipline — a worktree isolates the working tree and index, not the repository, and refs/stash is one stack. Raw SQL investigated and deliberately NOT built: the disclosure now counts DDL-carrying statements separately (4 of 23 on TheTerrace) and measurement showed the one raw column add is followed by a raw drop of the same column, so the graph's schema is correct today — a fold would have been built for a measured zero. My own scan of that tree said 8 of 30 and was wrong: it counted Down methods, which the reader correctly ignores. C# extraction time not attempted: the walk is already lazily modelled and symbol-cached, the remaining cost is inherent to binding method bodies, and parallelising it is a concurrency change I declined to rush at the end of a long session.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "docs/plans/extractor-roadmap.md"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0297",
      "shortname": "pane-placement-fix-and-instrumentation",
      "datetime": "2026-08-31T21:49:34Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "there still seems to be odd things with the pane focus and refresh, when i added a class diagrame pane the graph pane disappeared. do we have enough instrumentation to trace these behaviors? investigate the issue(s) above while also doing the other next steps",
      "summary": "Root-caused the graph-pane-disappear: the default layout stacks the canvas with a document surface (stack-graph), and reference documents were AddSurface'd as a tab into the focused stack (the graph's), hiding it. Fixed with DocumentPlacementPolicy: a class diagram/code viewer tabs into a document stack or splits BESIDE the graph, never onto the canvas stack. Added WorkbenchDiagnostics (aide.workbench ActivitySource + JSON layout log) since the workbench had zero instrumentation. +5 headless App tests (192). Registered DC-054.",
      "kind": "skill",
      "skill": "investigate",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/DocumentPlacement.cs",
        "src/AiDe.App/Workbench/WorkbenchDiagnostics.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0298",
      "shortname": "call-walk-parallelised-and-a-flaky-control-fixed",
      "datetime": "2026-08-31T21:51:18Z",
      "session": "phase3-pane-probes",
      "prompt": "do the next steps you listed above (i have done re-index multiple times)",
      "summary": "The user's store was at generation 2026-08-31.4 while the current build is 2026-09-01.3 — their re-indexes ran against an older binary, so calls, links_to, is_exported were all 0 and knowledge was still double-indexed. Verified the published build end-to-end against a copy of their real store through the published daemon: 64 scopes, 29,314 assertions, 1,500 nodes, 283 knowledge, 1,272 call edges. C# extraction time closed: measured 5.8s without the call walk and 15.5s with, so 9.7s is binding method bodies and no honest prefilter avoids it; overlapped instead of reduced, one tree per thread with per-thread counters folded afterwards and the call site chosen by a deterministic smallest-location rule rather than by whichever thread arrived first. 15.5s to 8.0s, output identical at 29,314 assertions across two runs. Also fixed my own daemon-isolation control, which snapshotted a machine-global directory and so failed when the App and Core assemblies ran concurrently; it now asserts about the one directory the daemon it launched would have created.",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.Core/Extraction/CSharpExtractor.cs"
      ],
      "tags": [],
      "outcome": "success"
    },
    {
      "id": "al-0299",
      "shortname": "classdiagram-dependency-toggle",
      "datetime": "2026-08-31T21:57:22Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "prompt": "do the other next steps (uml dependency edges)",
      "summary": "Added UML dependency edges (depends_on) as an opt-in 'Dependencies' toggle (default off, since deps are dense at 7585): ClassRelationKind.Dependency + ClassHierarchy.Deps kept separate from inheritance relations (no layout/rank impact), excludes self-edges + inheritance-redundant + external, deduped; Filter/WithoutInterfaces carry deps. Drawn as dashed edge-to-edge lines with an open (stick) arrowhead via box-boundary intersection so they never occlude a box. +3 App tests (195).",
      "kind": "skill",
      "skill": "implement",
      "tool": null,
      "actor": null,
      "artifacts": [
        "src/AiDe.App/Workbench/ClassHierarchyModel.cs",
        "src/AiDe.App/Workbench/ClassDiagramSurface.cs"
      ],
      "tags": [],
      "outcome": "success"
    }
  ],
  "changes": [
    {
      "id": "cl-0001",
      "datetime": "2026-08-23T20:09:31Z",
      "session": "b5f931c6-a515-4e72-95f8-37dc8e3d0495",
      "kind": "architecture",
      "skill": "adopt",
      "title": "Record the current single-container WPF architecture",
      "prompt": "sigh - the original session was supposed to make this a github repo\ncreate a new github repo ai-de under my tim.ian.malloo account (public repo, mit license)\nthen commit and push the content here\nthen run /adopt",
      "summary": "Recovered AI-DE as one .NET 10 WPF runtime container with a minimal MVVM seam; seeded the connected knowledge graph and phased missing product, design, proof, and documentation work.",
      "rationale": "Source and history show one runtime executable and no recorded product or multi-tier architecture; adoption records that baseline without inventing provenance.",
      "artifacts": [
        "docs/architecture.md",
        "docs/ai-forward-pack-adoption.md"
      ],
      "tags": [
        "adoption",
        "architecture"
      ],
      "git": {
        "before": "ef30e96e1386b597ffee3ecef0403b11654fdf9d",
        "after": "5612e54e39dcb3cc612e30efde4ff3ae0dd1f197",
        "branch": "docs/adopt-knowledge-graph",
        "pushed": true,
        "commits": [
          "5612e54 docs: bootstrap repository knowledge graph"
        ]
      }
    },
    {
      "id": "cl-0002",
      "datetime": "2026-08-23T22:41:33Z",
      "session": "collectknowledge-20260823",
      "kind": "knowledge",
      "skill": "collectknowledge",
      "title": "Domain knowledge base established for AI-DE; Kuzu, MCP and lease-fencing findings invalidate three seed-architecture assumptions",
      "prompt": "/collectknowledge on the AI-native IDE architecture sketch, the agent coordination spec, and the named modelling/visualization domains",
      "summary": "Ten sourced domain knowledge bases established as the evidence base for AI-DE. Load-bearing findings: (1) Kuzu archived 2025-10-10, and no embedded+maintained+permissive+.NET Cypher store exists to replace it, so the IGraphStore seam is now essential; (2) MCP spec 2026-07-28 is stateless and deprecates Sampling/Roots/Logging, while the C# SDK is Microsoft+Anthropic maintained and stable; (3) Claude Code hooks have no Copilot equivalent, so a file event bus is the universal floor; (4) TTL+heartbeat leases are advisory without fencing tokens (Kleppmann, unrefuted); (5) DI, ASP.NET routes and EF mapping are structurally invisible to static analysis; (6) bounded contexts are not extractable; (7) inventory is not architecture - curation is the product.",
      "rationale": "The seed architecture sketch selected Kuzu as the graph store and assumed an MCP surface that has since changed shape; the coordination spec relies on leases whose correctness properties the distributed-systems literature explicitly denies. Establishing this evidence before design means those three decisions are remade deliberately rather than discovered during implementation. The disconfirming research also reframes the project thesis into the form (code-derived views, code authoritative) that escapes four of the five historical failure modes of model-driven engineering.",
      "artifacts": [
        "docs/knowledge/index.md",
        "docs/knowledge/code-knowledge-graphs/index.md",
        "docs/knowledge/mcp-and-agent-integration/index.md",
        "docs/knowledge/multi-agent-coordination/index.md"
      ],
      "tags": [],
      "git": {
        "before": "9065ea4f1e6a62139e340c271e14b99a9c4944e4",
        "after": "9065ea4f1e6a62139e340c271e14b99a9c4944e4",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0003",
      "datetime": "2026-08-24T13:23:21Z",
      "session": "6c940bbc-816b-41ed-a92a-0e954ac70a37",
      "kind": "spec",
      "skill": "specify",
      "title": "Specify AI-native IDE product boundary and user experience",
      "prompt": "create a specification (md and html) for my AI-IDE\n- use this proposal as the seed idea: \"C:\\Users\\malla\\Downloads\\ai-native-ide-architecture-sketch.md\"\n- key scenarios: visually understand implemented logical/as-built architecture; core data/domain models including class hierarchy and aggregate roots; data/process flow; cross-service/library/infrastructure dependencies; cross-agent coordination; repository knowledge as graph/hierarchy; rich-text staged prompts; audit logs; and work/task backlog across sessions and worktrees.\n- typical workflow: one or more Claude Code or GitHub Copilot sessions in terminal tabs, isolated worktrees, coordinated artifacts/graph updates, visual tabs for feedback, and rich-text prompt tabs.\n- reuse existing repositories and libraries where justified; take inspiration from VS Code and Eclipse while keeping code transient rather than the product.",
      "summary": "Defined a code-derived, provenance-labelled visual workspace with session coordination, prompt staging, audit inspection, privacy controls, and implementation spikes.",
      "rationale": "The seed needs a testable product contract that preserves its user goals while deferring invalidated or unproven implementation choices to spikes.",
      "artifacts": [
        "docs/specs/ai-native-ide.md",
        "docs/specs/ai-native-ide.html",
        "docs/security/ai-native-ide-privacy-review.md"
      ],
      "tags": [
        "ai-native-ide",
        "specification",
        "agent-coordination"
      ],
      "git": {
        "before": "0427582ccd915c52c68f170703a64760b3a152bb",
        "after": "0427582ccd915c52c68f170703a64760b3a152bb",
        "branch": "docs/ai-ide-specification",
        "pushed": null,
        "commits": []
      }
    },
    {
      "id": "cl-0004",
      "datetime": "2026-08-25T22:53:28Z",
      "session": "6c940bbc-816b-41ed-a92a-0e954ac70a37",
      "kind": "architecture",
      "skill": "define-architecture",
      "title": "Adopt local workspace daemon and fact-store architecture for AI-DE",
      "prompt": "go ahead and merge the PR\nthe /define-architecture on the ai-ide-specification",
      "summary": "Defined a WPF shell over a per-workspace daemon, SQLite dimensions and append-only evidence facts, bounded MCP tools, ConPTY-owned sessions, threat/privacy/release plans, ADRs, and vertical phases.",
      "rationale": "The merged specification requires provenance-labelled derived views, bounded agent coordination, local-first data protection, and a durable representation that does not depend on the archived Kuzu store.",
      "artifacts": [
        "docs/architecture.md",
        "docs/design/conceptual-model.md",
        "docs/security/ai-native-ide-threat-model.md",
        "docs/security/ai-native-ide-privacy-review.md",
        "docs/release/ai-native-ide-release-plan.md",
        "docs/adr/0001-derived-evidence-views.md",
        "docs/adr/0002-workspace-fact-store.md",
        "docs/adr/0003-workspace-daemon-boundary.md",
        "docs/adr/0004-mcp-tool-boundary.md",
        "docs/adr/0005-terminal-runtime-boundary.md",
        "docs/adr/0006-terminal-delivery-semantics.md",
        "docs/adr/0007-agent-session-adapter.md"
      ],
      "tags": [
        "ai-native-ide",
        "architecture",
        "mcp",
        "sqlite",
        "conpty"
      ],
      "git": {
        "before": "bc50c41853fb2314a783709c58eb0f065f48c402",
        "after": "bc50c41853fb2314a783709c58eb0f065f48c402",
        "branch": "architecture/ai-native-ide",
        "pushed": null,
        "commits": []
      }
    },
    {
      "id": "cl-0005",
      "datetime": "2026-08-26T00:36:34Z",
      "session": null,
      "kind": "architecture",
      "skill": "define-architecture",
      "title": "AI-DE architecture v2 supersedes the 2026-08-25 draft",
      "prompt": "C:/Program Files/Git/define-architecture ai-ide-arch-v2 — redo the architecture to meet your bar",
      "summary": "Write-ahead two-phase dispatch receipt (ADR-0010); in-process-first authority core (ADR-0009); MCP authorization bound to session processing class (ADR-0011); WPF+WebView2 shell host recorded (ADR-0008); 3 committed re-runnable spikes; contradictions fixed at source",
      "rationale": "Ten-persona adversary review of the prior draft raised 3 hard and 2 soft vetoes and verified internal contradictions; this revision resolves each with a mechanism, artifact, or test rather than prose",
      "artifacts": [
        "docs/architecture.md",
        "docs/adr/0010-two-phase-dispatch-receipt.md"
      ],
      "tags": [],
      "git": {
        "before": "2b134da951bd364d7f4eca58a663774a918b6001",
        "after": "2b134da951bd364d7f4eca58a663774a918b6001",
        "branch": "architecture/ai-ide-arch-v2",
        "pushed": null,
        "commits": []
      }
    },
    {
      "id": "cl-0006",
      "datetime": "2026-08-26T00:49:08Z",
      "session": null,
      "kind": "design",
      "skill": "design",
      "title": "Phase-1 walking skeleton design",
      "prompt": "C:/Program Files/Git/design phase-1 the walking skeleton",
      "summary": "Blueprint for the thinnest end-to-end slice: fact store with enforced immutability, in-process core with write-ahead dispatch, bounded projections incl. knowledge (US-4), stdio MCP with egress binding, health sidecar, freshness prober, accessible provenance pane",
      "rationale": "Turns the v2 architecture into an implementable slice while keeping every council-veto mechanism testable red-first",
      "artifacts": [
        "docs/design/phase-1-walking-skeleton.md",
        "DESIGN.md"
      ],
      "tags": [],
      "git": {
        "before": "a0bd6998aa345435ef995a7dc8d117e10e3e8383",
        "after": "a0bd6998aa345435ef995a7dc8d117e10e3e8383",
        "branch": "feat/phase-1-walking-skeleton",
        "pushed": null,
        "commits": []
      }
    },
    {
      "id": "cl-0007",
      "datetime": "2026-08-26T19:35:00Z",
      "session": null,
      "kind": "design",
      "skill": "design",
      "title": "Phase-2 design: scope-per-project, bidirectional terminal seam, restored cross-process boundary",
      "prompt": "C:/Program Files/Git/design phase 2",
      "summary": "No new fact table; one scope per project rather than per solution; ITerminalSession extended with a pull-based bounded output channel; analyzers/generators disabled during extraction",
      "rationale": "Streaming extraction would leak partial results into a store whose invariant is complete-snapshot-only; an event-based output path would let a fast producer drive unbounded work; loading a repository must never execute its code",
      "artifacts": [
        "docs/design/phase-2-real-code-and-terminal.md"
      ],
      "tags": [],
      "git": {
        "before": "d42ad03d539bdca589838e86b25285c0928eb691",
        "after": "d42ad03d539bdca589838e86b25285c0928eb691",
        "branch": "design/phase-2",
        "pushed": null,
        "commits": []
      }
    },
    {
      "id": "cl-0008",
      "datetime": "2026-08-26T19:57:48Z",
      "session": null,
      "kind": "design",
      "skill": "investigate",
      "title": "Analyzer-execution control moves from MSBuild properties to stripping AnalyzerReferences",
      "prompt": "Spike S2",
      "summary": "The Phase-2 design's named mitigation was measured ineffective. The control that holds is solution.WithProjectAnalyzerReferences(id, []) before any compilation is requested",
      "rationale": "MSBuild properties govern the build rather than the workspace project model, and they are the repository's own build configuration — a control a hostile repository can influence is not a control. Stripping is applied after load, in our process, and depends on nothing in the repository cooperating.",
      "artifacts": [
        "spikes/roslyn-msbuild-workspace/RESULT.md",
        "docs/design/phase-2-real-code-and-terminal.md"
      ],
      "tags": [],
      "git": {
        "before": "b8b7c08",
        "after": "b8b7c0849435b0cd84e8fb8009e6b1be1308065c",
        "branch": "fix/defect-register-and-spike-s2",
        "pushed": null,
        "commits": []
      }
    },
    {
      "id": "cl-0009",
      "datetime": "2026-08-26T20:38:23Z",
      "session": null,
      "kind": "architecture",
      "skill": "investigate",
      "title": "Accessibility withdrawn as a conformance target; terminal renderer chosen; ADR-0008's reversal trigger met",
      "prompt": "Spike S3 and S4; disclose absent generated code; suppress accessibility vetos",
      "summary": "ADR-0014 accepted. S3 selects an owned WPF renderer with GlyphRun-per-line binding. S4 leaves ADR-0008 unsettled: airspace is real and the composition control crashes the process on float",
      "rationale": "The owner is not optimising for accessibility, so the veto is suppressed and every artifact claiming WCAG 2.2 AA is corrected rather than left asserting conformance the product does not pursue. S3/S4 are measured rather than reasoned: 21x spread between draw paths, and a native access violation on float.",
      "artifacts": [
        "docs/adr/0014-accessibility-posture.md",
        "docs/adr/0008-shell-host.md",
        "docs/adr/0005-terminal-runtime-boundary.md"
      ],
      "tags": [],
      "git": {
        "before": "e59738a",
        "after": "e59738a654ebf8b908075d4f5ea5504ceb025ae5",
        "branch": "spike/s3-s4-and-a11y-posture",
        "pushed": null,
        "commits": []
      }
    },
    {
      "id": "cl-0010",
      "datetime": "2026-08-26T21:17:40Z",
      "session": null,
      "kind": "architecture",
      "skill": "design",
      "title": "Canvas keeps the windowed WebView2; overlaps handled by snapshot swap; focus routed via Win32",
      "prompt": "go with your recommendation; design the focus piece; snapshot-swap spike",
      "summary": "ADR-0015 accepted. ADR-0008 not reversed. Focus enters via SetFocus on the HwndHost handle and leaves via a page-to-host message, because CoreWebView2Controller is not exposed",
      "rationale": "The windowed control fails as a constraint; the composition control fails as a crash. The snapshot swap was measured aligned to within a pixel of rounding at 150% DPI. The documented focus API does not exist on this control - established by enumerating the public surface, not by grepping the assembly, whose string table contains the names.",
      "artifacts": [
        "docs/adr/0015-canvas-hosting-and-overlay-strategy.md",
        "docs/adr/0008-shell-host.md",
        "docs/design/phase-2-real-code-and-terminal.md"
      ],
      "tags": [],
      "git": {
        "before": "41ac91e",
        "after": "41ac91e537282c472ab7461313e2d3be77d6f49b",
        "branch": "feat/s4-decision-and-focus",
        "pushed": null,
        "commits": []
      }
    },
    {
      "id": "cl-0011",
      "datetime": "2026-08-28T16:28:49Z",
      "session": "decisions-d1-d7-2026-08-28",
      "kind": "decision",
      "skill": null,
      "title": "D1 — Terminal processes stay in the shell; the daemon-crash failure row is corrected",
      "prompt": "D1..D7 answered by the product owner",
      "summary": "Terminals remain owned by the shell process (TerminalSurface creates ConPtyTerminalSession). The P2-TERM-05 failure row is rewritten from 'Daemon crashes' to 'Shell crashes', because the Job Object that mitigates it is owned by the creating process.",
      "rationale": "The mitigation already exists and already fires: JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE is implemented in ConPtyInterop and reaps children when the owning process dies. Only the threat sentence was wrong. Moving terminals to the daemon would add a second IPC lane, framing cost and a backpressure design for a stream whose consumer (the WPF surface, measured 5.50ms p95) lives in the shell — and would invert ADR-0003, which scopes the daemon to evidence rather than UI.",
      "artifacts": [
        "docs/design/phase-2-real-code-and-terminal.md"
      ],
      "tags": [
        "phase-2"
      ],
      "git": {
        "before": "1889c33eb11213decb9292eeb22b3115790c0e74",
        "after": "1889c33eb11213decb9292eeb22b3115790c0e74",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0012",
      "datetime": "2026-08-28T16:28:49Z",
      "session": "decisions-d1-d7-2026-08-28",
      "kind": "decision",
      "skill": null,
      "title": "D2 — MSBuildWorkspace advisories: adopt the spike's reference posture and verify the output; do not block Component 1",
      "prompt": "D1..D7 answered by the product owner",
      "summary": "The shipped extractor adopts ExcludeAssets=runtime plus MSBuildLocator, as the S2 spike did. The advisory finding is then verified by inspecting the built output directory for the flagged assemblies. Component 1 is NOT gated on this.",
      "rationale": "S2's csproj records that every reference is compile-time only and MSBuild is loaded at runtime from the installed SDK, so none of the flagged assemblies reach the output. The exposure is therefore very likely a reference-assembly artifact rather than shipped code, and the residual is the user's own SDK which they already execute to build. Verification is cheap and concrete (list bin/), turning a scanner finding into a measured fact. Abandoning MSBuildWorkspace would discard S2's measured result to solve a problem that may not exist.",
      "artifacts": [
        "spikes/roslyn-msbuild-workspace/RESULT.md"
      ],
      "tags": [
        "phase-2"
      ],
      "git": {
        "before": "1889c33eb11213decb9292eeb22b3115790c0e74",
        "after": "1889c33eb11213decb9292eeb22b3115790c0e74",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0013",
      "datetime": "2026-08-28T16:28:49Z",
      "session": "decisions-d1-d7-2026-08-28",
      "kind": "decision",
      "skill": null,
      "title": "D3 — Repository-authored MSBuild tasks are spiked before Component 1, results returned for review",
      "prompt": "D1..D7 answered by the product owner",
      "summary": "A spike loads a fixture project carrying a hostile UsingTask through MSBuildWorkspace and asserts the task never executes. Results are reported for a separate product-owner decision; Component 1 remains gated on it.",
      "rationale": "Loading a repository must never execute its code, and this path is unproven: S2 established that analyzers and generators can be suppressed, but MSBuild evaluation still runs to load projects and repository-supplied task assemblies were never tested. Intuition has already failed once in this exact area — the design's named mitigation (MSBuild properties) was measured ineffective, and EnforceExtendedAnalyzerRules turned out to be one line the attacker sets in their own project.",
      "artifacts": [
        "spikes/msbuild-task-execution/"
      ],
      "tags": [
        "phase-2"
      ],
      "git": {
        "before": "1889c33eb11213decb9292eeb22b3115790c0e74",
        "after": "1889c33eb11213decb9292eeb22b3115790c0e74",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0014",
      "datetime": "2026-08-28T16:28:50Z",
      "session": "decisions-d1-d7-2026-08-28",
      "kind": "decision",
      "skill": null,
      "title": "D4 — ADR-0001..0013 promoted to accepted; ADR-0010 held at proposed",
      "prompt": "D1..D7 answered by the product owner",
      "summary": "Twelve ADRs move from status: proposed to status: accepted. ADR-0010 (two-phase dispatch receipt) stays proposed.",
      "rationale": "These are not proposals, they are shipped: ADR-0003's boundary is a running daemon, ADR-0012's docking shell is the workbench, ADR-0013's envelope round-trips in tests. Recording a decision the product depends on as 'proposed' is a false record and makes the two genuinely recent decisions (0014, 0015) indistinguishable from settled ones. ADR-0010 is held because nothing is built and it depends on D1.",
      "artifacts": [
        "docs/adr/"
      ],
      "tags": [
        "phase-2"
      ],
      "git": {
        "before": "1889c33eb11213decb9292eeb22b3115790c0e74",
        "after": "1889c33eb11213decb9292eeb22b3115790c0e74",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0015",
      "datetime": "2026-08-28T16:28:50Z",
      "session": "decisions-d1-d7-2026-08-28",
      "kind": "decision",
      "skill": null,
      "title": "D5 — Cross-monitor DPI accepted as a documented unverified risk, non-blocking",
      "prompt": "D1..D7 answered by the product owner",
      "summary": "Cross-monitor DPI verification is deferred and explicitly non-blocking. It is validated once multi-monitor hardware is available and the product is further along.",
      "rationale": "The owner is working on a laptop without a second display, so the measurement is hardware-gated rather than decision-gated. The DPI arithmetic already has evidence — the snapshot swap measured aligned to within a pixel of rounding at 150% DPI — so what is missing is the monitor-transition case specifically. The failure mode is visual misalignment on a multi-monitor drag: user-visible, not data-threatening, and fixable when observed.",
      "artifacts": [
        "docs/design/phase-2-real-code-and-terminal.md"
      ],
      "tags": [
        "phase-2"
      ],
      "git": {
        "before": "1889c33eb11213decb9292eeb22b3115790c0e74",
        "after": "1889c33eb11213decb9292eeb22b3115790c0e74",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0016",
      "datetime": "2026-08-28T16:28:50Z",
      "session": "decisions-d1-d7-2026-08-28",
      "kind": "decision",
      "skill": null,
      "title": "D6 — Terminal ships viewport-only for Phase 2; scrollback goes to the backlog",
      "prompt": "D1..D7 answered by the product owner",
      "summary": "TerminalScreen remains viewport-only for Phase 2. Scrollback is recorded as a backlog item with its designed upgrade path, and named as a known product limitation rather than a technical note.",
      "rationale": "Growing the viewport to provide history would put an unbounded allocation behind an innocuous property, sized by an untrusted child process. The upgrade path is already designed — a bounded ring beside the screen, not inside it — so this is deferral rather than debt. It is named as a product limitation because Phase 2's human validation is 'launch pwsh and observe real session state', and a developer who cannot scroll back to read build errors will read that as broken.",
      "artifacts": [
        "src/AiDe.Core/Terminal/TerminalScreen.cs"
      ],
      "tags": [
        "phase-2"
      ],
      "git": {
        "before": "1889c33eb11213decb9292eeb22b3115790c0e74",
        "after": "1889c33eb11213decb9292eeb22b3115790c0e74",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0017",
      "datetime": "2026-08-28T16:28:50Z",
      "session": "decisions-d1-d7-2026-08-28",
      "kind": "decision",
      "skill": null,
      "title": "D7 — Phase-2 performance is baselined before Component 1",
      "prompt": "D1..D7 answered by the product owner",
      "summary": "P2-PERF is established before the Roslyn extractor lands, so the extractor's cost is a delta against a known floor rather than a first observation.",
      "rationale": "Six simplify: ceilings are live in shipped code and no upgrade trigger has ever been evaluated. Measuring after Component 1 lands entangles the first real number with the largest new subsystem, leaving it unclear which half moved it. NOTE: discovered while planning — P2-PERF-01..03 is named in the test plan but never specified, and its only stated budget (scope settlement p95 > 10s) measures Roslyn extraction, which does not exist. The work is therefore specify-and-build, not run.",
      "artifacts": [
        "bench/AiDe.Bench/"
      ],
      "tags": [
        "phase-2"
      ],
      "git": {
        "before": "1889c33eb11213decb9292eeb22b3115790c0e74",
        "after": "1889c33eb11213decb9292eeb22b3115790c0e74",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0018",
      "datetime": "2026-08-28T16:36:01Z",
      "session": "decisions-d1-d7-2026-08-28",
      "kind": "decision",
      "skill": null,
      "title": "D3 spike result: MSBuildWorkspace executes repository-supplied code — Component 1 blocked",
      "prompt": null,
      "summary": "Loading a hostile project through MSBuildWorkspace.OpenProjectAsync executed all four repository-supplied vectors (Exec in InitialTargets, RoslynCodeTaskFactory inline task, UsingTask assembly, design-time target hook) with zero WorkspaceFailed diagnostics and a cleanly loaded project. Two vectors require nothing but the checked-in .csproj. Registered as DC-019.",
      "rationale": "The principle is absolute: loading a repository must never execute its code. S2's analyzer/generator control is correct but covers a mechanism rather than the boundary. The probe carries a positive control and a non-vacuity guard, and the positive control caught a real path bug on the first run that would otherwise have produced a false all-clear. No containment has been designed or tested; candidates are labelled Inferred.",
      "artifacts": [
        "spikes/msbuild-task-execution/RESULT.md"
      ],
      "tags": [
        "phase-2",
        "security"
      ],
      "git": {
        "before": null,
        "after": "1889c33eb11213decb9292eeb22b3115790c0e74",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0019",
      "datetime": "2026-08-28T16:39:16Z",
      "session": "decisions-d1-d7-2026-08-28",
      "kind": "design",
      "skill": null,
      "title": "P2-PERF-01..03 specified; P2-PERF-02 measured — the daemon boundary costs ~0.35ms flat",
      "prompt": null,
      "summary": "The suite was named in the test plan with no cases and one budget that measured a component which does not exist. Now specified with three cases and budgets. P2-PERF-02 measured: describe 0.58ms in process vs 0.92ms over the pipe, impact 0.43ms vs 0.79ms, on a 50k-edge corpus, 30 warm samples, Release.",
      "rationale": "A simplify: ceiling whose upgrade trigger points at an unspecified suite has no trigger. The boundary tax is ~0.35ms flat and reads as 1.6-1.8x only because the projections are sub-millisecond; against the 100ms describe budget that is 0.35 percent. P2-PERF-01 stays blocked behind Component 1. Scope: one client, one connection, no contention.",
      "artifacts": [
        "bench/AiDe.Bench/P2Perf.cs"
      ],
      "tags": [
        "phase-2",
        "performance"
      ],
      "git": {
        "before": null,
        "after": "1889c33eb11213decb9292eeb22b3115790c0e74",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0020",
      "datetime": "2026-08-28T16:56:10Z",
      "session": "decisions-d1-d7-2026-08-28",
      "kind": "design",
      "skill": null,
      "title": "Containment spike: both options work — low-integrity sandbox and no-MSBuild extraction measured",
      "prompt": null,
      "summary": "A job object alone does not contain the D3 attack (4/4 vectors land). Low integrity plus a job object blocks all four and extraction still succeeds. A no-MSBuild path (project file parsed as data, Roslyn compiled directly) recovers 159 of 159 types on src/AiDe.Core in 359ms against MSBuildWorkspace's 2210ms, and never runs repository code.",
      "rationale": "Option A's first run appeared to show low integrity breaks MSBuild; the real cause was the child inheriting an unwritable TEMP, and repointing TMP/TEMP fixed it entirely - a containment that fails environmentally is indistinguishable from one that cannot work. Option B's structural safety is bounded by project.assets.json, which is data but is produced by restore, which is itself MSBuild evaluation. Network egress under Option A is unmeasured; Option B's fidelity on ProjectReference, multi-targeting and custom globs is untested.",
      "artifacts": [
        "spikes/extraction-containment/RESULT.md"
      ],
      "tags": [
        "phase-2",
        "security"
      ],
      "git": {
        "before": null,
        "after": "1889c33eb11213decb9292eeb22b3115790c0e74",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0021",
      "datetime": "2026-08-28T17:01:55Z",
      "session": "decisions-d1-d7-2026-08-28",
      "kind": "architecture",
      "skill": null,
      "title": "Component 1 adopts Strategy 1: extraction never runs repository code; MSBuildWorkspace dropped",
      "prompt": null,
      "summary": "The Roslyn extractor reads the project file as data and compiles with Roslyn directly, always. MSBuildWorkspace is not used. Where package references cannot be resolved the projection discloses the omission rather than answering silently. The low-integrity sandbox (A2) is kept as a measured escape hatch, adopted only by a further decision that closes its network-egress gap.",
      "rationale": "Strategy 1 is the only option where 'loading a repository never executes its code' stays literally true rather than conditional. It is 6.2x faster on the common path (359ms vs 2210ms) and its failure mode is a visible disclosed omission rather than a silent one, matching the precedent S1 set for absent generated symbols. Accepted risk: Option B's fidelity is measured on one project with no ProjectReference; multi-targeting, custom globs and Directory.Build.props are untested, and fidelity failures in an extractor are silent - so a fidelity spike against AiDe.App and a multi-targeted project is owed before the extractor ships.",
      "artifacts": [
        "docs/design/phase-2-real-code-and-terminal.md"
      ],
      "tags": [
        "phase-2",
        "security"
      ],
      "git": {
        "before": null,
        "after": "1889c33eb11213decb9292eeb22b3115790c0e74",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0022",
      "datetime": "2026-08-28T17:27:43Z",
      "session": "decisions-d1-d7-2026-08-28",
      "kind": "design",
      "skill": null,
      "title": "Component 1 contract rewritten against a measured prototype; scope grain is (project, target framework)",
      "prompt": null,
      "summary": "Option B measured 100% dependency-edge resolution and zero type loss against MSBuildWorkspace on four project shapes including ProjectReference, WPF and multi-targeting, at ~25x the speed. Contract rewritten: CSharpExtractor, one scope per (project, target framework), project file read as data, three named disclosure states.",
      "rationale": "The scope grain changed from per-project to per-(project,TFM) because a multi-targeted project's #if-gated types differ between frameworks and a single scope would be silently wrong about the others. The spike's own first run reported 82-89% edge resolution, which was two harness defects - missing implicit usings (the SDK generates them into obj/, which the extractor deliberately does not read) and a WindowsBase 4.0 facade shadowing the real 10.0 assembly - not a limit of the approach; stopping at that number would probably have reversed the strategy. Measuring edges rather than types was decisive: a broken project reference leaves every local type intact and turns its edges into error types, which a type count scores as perfect.",
      "artifacts": [
        "spikes/extraction-fidelity/RESULT.md"
      ],
      "tags": [
        "phase-2",
        "extraction"
      ],
      "git": {
        "before": null,
        "after": "a45ba05ab7f43b4d552a72954d076b78f1b6ac51",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0023",
      "datetime": "2026-08-28T17:33:41Z",
      "session": "decisions-d1-d7-2026-08-28",
      "kind": "design",
      "skill": null,
      "title": "P2-FOCUS-01/02/04 built at the host seam; P2-FOCUS-03 recorded as owed, not approximated",
      "prompt": null,
      "summary": "CanvasFocusRouter holds the focus policy in Core with no WPF dependency; workbench.focusCanvas (Ctrl+K, G) is in the catalog and routed through WorkbenchController. 12 tests. The graph canvas surface itself does not exist - no WebView2 reference in AiDe.App - so the command refuses and announces rather than being hidden.",
      "rationale": "Splitting policy from mechanism lets the rules that do not need a window be tested without one; a rule that can only be tested with a real WebView2 running is a rule that stops being tested. P2-FOCUS-03 is deliberately NOT written against the fake: a keyboard-trap test that cannot fail for the reason it exists would report the trap as tested (DC-016). Keeping the command visible-and-refusing rather than hidden follows DC-011 - silence is indistinguishable from a broken key.",
      "artifacts": [
        "src/AiDe.Core/Workbench/CanvasFocusRouter.cs"
      ],
      "tags": [
        "phase-2",
        "focus"
      ],
      "git": {
        "before": null,
        "after": "a45ba05ab7f43b4d552a72954d076b78f1b6ac51",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0024",
      "datetime": "2026-08-28T17:43:08Z",
      "session": "decisions-d1-d7-2026-08-28",
      "kind": "design",
      "skill": null,
      "title": "Prompt dispatch crosses the boundary; a stale-epoch refusal would have killed the daemon (DC-020)",
      "prompt": null,
      "summary": "BoundaryDispatcher splits ADR-0010's two-phase receipt so the daemon owns durability and the shell owns the pty. dispatch.begin and dispatch.finalize registered; WorkspaceClient extended; 6 tests covering agreement, idempotency, retried-finalize, the shell-crash window, and the two checks that must stay on their own side of the boundary.",
      "rationale": "D1 put terminals in the shell and the store in the daemon, so the two halves of a two-phase delivery are now in different processes. The session-binding check stays with the caller because the daemon has no session to check against - checking there would compare the caller's claim with itself. A test written to assert no attempt is recorded on a stale epoch instead brought the server down: the WorkspaceStoreException escaped Handle (which guards only decoding) and left IpcServer's listen loop, so one client with a stale epoch would have killed the daemon for every shell. Registered as DC-020 and fixed by mapping only WorkspaceStoreException - the type carrying a stable denial code - onto IpcResponse.Error.",
      "artifacts": [
        "src/AiDe.Core/Dispatch/BoundaryDispatcher.cs"
      ],
      "tags": [
        "phase-2",
        "dispatch"
      ],
      "git": {
        "before": null,
        "after": "a45ba05ab7f43b4d552a72954d076b78f1b6ac51",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0025",
      "datetime": "2026-08-28T17:43:08Z",
      "session": "decisions-d1-d7-2026-08-28",
      "kind": "design",
      "skill": null,
      "title": "P2-PERF-03 measured: 1 MiB/s held for 10s with drift tracked",
      "prompt": null,
      "summary": "Sustained 1.00 MiB/s through the VT parser and screen model for 10s: chunk parse p50 1.05ms, p95 1.80ms, per-chunk drift +0.293ms first-to-last quarter, unthrottled ceiling 77 MiB/s.",
      "rationale": "A burst measures the fast path; holding the rate is what exposes accumulation, which is why the gate fails on per-chunk growth rather than only on absolute latency. S3's 2361x figure is scanning only - this measures scan plus screen-model application - so S3's number must never be quoted as end-to-end terminal throughput. The draw half stays in the App tests because it needs a dispatcher and a visual tree; this number is not a frame time.",
      "artifacts": [
        "bench/AiDe.Bench/TerminalThroughput.cs"
      ],
      "tags": [
        "phase-2",
        "performance"
      ],
      "git": {
        "before": null,
        "after": "a45ba05ab7f43b4d552a72954d076b78f1b6ac51",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0026",
      "datetime": "2026-08-28T18:26:17Z",
      "session": "decisions-d1-d7-2026-08-28",
      "kind": "design",
      "skill": null,
      "title": "P2-FOCUS complete: the canvas ships and the keyboard-trap test runs out of process",
      "prompt": null,
      "summary": "CanvasSurface (windowed WebView2, ADR-0015) with inlined boundary handlers, CanvasFocusTarget over the HwndHost, and the canvas surface kind wired into the shell. P2-FOCUS-01..04 all pass; -03 runs out of process via tests/AiDe.App.CanvasProbe.",
      "rationale": "Two input routes failed before the third worked, and both failed with the SAME symptom as a genuine keyboard trap: a posted WM_KEYDOWN never reaches Chromium's key handling, and SendInput goes to the foreground window which no test host can hold. Diagnosed rather than guessed - the page reported activeElement=first while window.__tabsSeen was 0, proving focus had landed and the keys had not arrived. A trap test that fails because input never arrived is DC-016 wearing the right label and would have been 'fixed' by weakening the assertion. Residual: injecting at the renderer's input layer does not cover the OS-to-browser hop.",
      "artifacts": [
        "src/AiDe.App/Workbench/CanvasSurface.cs"
      ],
      "tags": [
        "phase-2",
        "focus"
      ],
      "git": {
        "before": null,
        "after": "78777c88f521256134172e627dc46a78ebd6feff",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0027",
      "datetime": "2026-08-28T18:26:37Z",
      "session": "decisions-d1-d7-2026-08-28",
      "kind": "architecture",
      "skill": null,
      "title": "CSharpExtractor ships: real C# symbols with no MSBuild, disclosures as facts",
      "prompt": null,
      "summary": "CSharpProjectReader reads the project file as data and produces a Roslyn compilation; CSharpExtractor emits has_type, declared_in, inherits, implements and depends_on assertions plus disclosure facts on the scope node. 11 tests. P2-PERF-01 measured: AiDe.Core settles in 723ms p95 against a 10s budget, 1,281 assertions.",
      "rationale": "Disclosures are emitted as ordinary facts on a scope node rather than a new table, keeping the Phase-2 decision that no new fact table is added - and making them queryable by every existing projection for free. An edge that did not resolve is NOT emitted: labelling it Inferred would be worse than silence because the name is whatever the source typed and the edge would point at a node that may not exist. Complete stays true when extraction succeeds, because the disclosures are IN the snapshot rather than missing from it; marking it incomplete would quarantine every unrestored project, which is most of them on a fresh clone.",
      "artifacts": [
        "src/AiDe.Core/Extraction/CSharpExtractor.cs"
      ],
      "tags": [
        "phase-2",
        "extraction"
      ],
      "git": {
        "before": null,
        "after": "78777c88f521256134172e627dc46a78ebd6feff",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0028",
      "datetime": "2026-08-28T18:26:37Z",
      "session": "decisions-d1-d7-2026-08-28",
      "kind": "design",
      "skill": null,
      "title": "Prompt dispatch reaches the UI; DC-020's control widened to every operation",
      "prompt": null,
      "summary": "PromptBar stages a prompt for the focused terminal and reports the receipt, with a distinct sentence per DispatchState and DeliveryUnknown never shown as success. workbench.dispatchPrompt (Ctrl+K, P). Every registered IPC operation now goes through Refusable, not just dispatch.",
      "rationale": "The receipt is what the UI exists to surface: a prompt delivered to an agent session cannot be undone, so DeliveryUnknown must read as unknown and warn about resending rather than rounding to sent. A dispatch that threw is not a dispatch that did not happen - the write-ahead attempt may already be durable - so the UI says 'did not complete, check the receipt' rather than 'failed'. DC-020's control was widened past the operations that needed it: no read projection throws a domain refusal today, and nothing would have failed if one were added.",
      "artifacts": [
        "src/AiDe.App/Workbench/PromptBar.cs"
      ],
      "tags": [
        "phase-2",
        "dispatch"
      ],
      "git": {
        "before": null,
        "after": "78777c88f521256134172e627dc46a78ebd6feff",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0029",
      "datetime": "2026-08-28T18:33:35Z",
      "session": "decisions-d1-d7-2026-08-28",
      "kind": "design",
      "skill": null,
      "title": "The graph canvas joins the default layout as a tab in the primary stack",
      "prompt": null,
      "summary": "Layout.Default gains a 'graph' canvas surface alongside Explore and Domain. Test fixtures enumerating the shipped surface set were updated, and WorkbenchAdapterTests now derives its expected count from the model instead of hardcoding it.",
      "rationale": "Without this the canvas is unreachable: there is no command to open a surface, so a built-but-unlisted canvas would be dead code the user cannot see. A tab in the primary stack rather than a fourth pane, because it is an alternative reading of the same evidence Explore shows and a default layout that opens a WebView2 in its own pane pays for the browser on every start. The hardcoded count of 4 in WorkbenchAdapterTests was replaced by one derived from Layout.Default: the assertion is 'every surface is projected', and a typed count turns adding a surface into a failure that says nothing about projection.",
      "artifacts": [
        "src/AiDe.Core/Workbench/LayoutModel.cs"
      ],
      "tags": [
        "phase-2",
        "ui"
      ],
      "git": {
        "before": null,
        "after": "34bcab91205a203402b00a487c3a7c49357b2234",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0030",
      "datetime": "2026-08-28T19:33:14Z",
      "session": "decisions-d1-d7-2026-08-28",
      "kind": "architecture",
      "skill": null,
      "title": "Phase 2 exits: all three components built, measured, and gated PASS-WITH-CONDITIONS",
      "prompt": null,
      "summary": "Extractor wired to the shell (Ctrl+K, I) with one scope per (project, framework) and per-scope quarantine; canvas renders real projection data with omissions and disclosures shown; snapshot swap driven by the real drag; DC-020's control widened; Phase-2 exit review written. 541 tests, four gates green, all three perf budgets met.",
      "rationale": "Two conditions attach to the pass. The Option-B fidelity spike must be extended to shared projects and Directory.Build.props before another repository is indexed in anger, because a fidelity failure in an extractor is silent and the 100 percent result comes from four shapes that do not include them. ADR-0010 stays proposed until dispatch runs against a real agent session rather than a terminal. Discovery gained a scope for unreadable projects after a test found they VANISHED entirely - not indexed, not failed, not counted - which is the silent incompleteness this design exists to prevent.",
      "artifacts": [
        "docs/reviews/phase-2-exit.md"
      ],
      "tags": [
        "phase-2",
        "review"
      ],
      "git": {
        "before": null,
        "after": "f415e024016455119fe36d82ef72e9dfbd1f209d",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0031",
      "datetime": "2026-08-28T20:08:27Z",
      "session": "decisions-d1-d7-2026-08-28",
      "kind": "design",
      "skill": null,
      "title": "Fidelity extended to an external repository: two real gaps found and closed",
      "prompt": null,
      "summary": "Measured against TheTerrace (811 C# files, Directory.Build.props, central package management, Blazor Web SDK). Two gaps closed: inherited build properties, and the ASP.NET Core reference pack. Result: no worse than MSBuildWorkspace on any project measured; TheTerrace itself now 100 percent edge resolution over 17,501 edges.",
      "rationale": "AiDe's own projects were the corpus the extractor was built against, which makes them the worst possible evidence that it works. The external repository immediately exposed what its own could not: REHEARSAL is defined in Directory.Build.props so five test classes were compiled out and disappeared from the graph entirely, and Microsoft.NET.Sdk.Web implies a framework reference that no NuGet package supplies. The verdict now compares against the BASELINE rather than perfection, because MSBuildWorkspace carries 46 bad edges on one of these projects itself and 'worse than 100 percent' would condemn Option B for a limitation both share.",
      "artifacts": [
        "spikes/extraction-fidelity/RESULT-theterrace.txt"
      ],
      "tags": [
        "phase-2",
        "extraction"
      ],
      "git": {
        "before": null,
        "after": "a77bb092728cab170e5043baddb06fac06b91740",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0032",
      "datetime": "2026-08-28T20:08:27Z",
      "session": "decisions-d1-d7-2026-08-28",
      "kind": "design",
      "skill": null,
      "title": "Phase 3 opened and grounded: the planned DDL parser is replaced by an EF-migration reader",
      "prompt": null,
      "summary": "Phase-3 design written against a real repository rather than the phase plan. TheTerrace has zero .sql files and 63 EF Core migration classes, so the planned DDL parser would have had no corpus. Three components: Bicep-as-data, EF-migration schema reader, and the joins with their confidence rules.",
      "rationale": "Checking the plan against a repository that was not written for this tool changed the component list before any code existed - the failure the Phase-2 spikes were introduced to prevent. Both new extractors inherit Phase 2's constraint that a build is never invoked: bicep build is a compiler on repository-supplied input, which is the D3 shape again. Confidence is the deliverable rather than the edge, because an inferred join across three artifacts looks more impressive than a verified one inside one and is exactly what a user would act on without checking.",
      "artifacts": [
        "docs/design/phase-3-architecture-data-infra.md"
      ],
      "tags": [
        "phase-3"
      ],
      "git": {
        "before": null,
        "after": "a77bb092728cab170e5043baddb06fac06b91740",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0033",
      "datetime": "2026-08-28T20:39:23Z",
      "session": "decisions-d1-d7-2026-08-28",
      "kind": "design",
      "skill": null,
      "title": "Phase-3 spikes clear: Bicep readable as data, EF migrations are viable schema evidence",
      "prompt": null,
      "summary": "Bicep declarative read recovers 24/24 resources, 19/19 types and 18/18 parameters against an az bicep build oracle. The EF migration fold recovers 62/62 tables EF maps in 99ms, plus two the model does not map. Both confirm Phase-3 components 1 and 2 as designed.",
      "rationale": "Both spikes existed to test a CONTRACT, not an optimisation: the product may not run bicep build or dotnet ef for the same reason it may not run MSBuild, so if the declarative read had been insufficient the design would have had to change rather than the principle. The EF result is asymmetric on purpose - the fold found two tables EF's own snapshot omits, created by a migration's Up and never dropped, so the fold is more correct than its oracle about what the database contains. Treating that as an error would have taught the component to hide real tables. Residual: raw Sql() statements are not read, and 8 of 24 Bicep names are expressions that stay unresolved and disclosed.",
      "artifacts": [
        "spikes/ef-migration-schema/RESULT.md"
      ],
      "tags": [
        "phase-3"
      ],
      "git": {
        "before": null,
        "after": "e8f521d185d7c66ecf1916a89535a698ece80764",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0034",
      "datetime": "2026-08-28T20:39:23Z",
      "session": "decisions-d1-d7-2026-08-28",
      "kind": "architecture",
      "skill": null,
      "title": "ADR-0016 proposed: bounded contexts are declared in one validated file, never inferred",
      "prompt": null,
      "summary": "A committed docs/bounded-contexts.yaml authored by a human and validated against extracted symbols: unknown namespaces fail, coverage is reported, and overlap is an error rather than a merge. Folder convention rejected on measured grounds.",
      "rationale": "The corpus repository's obvious candidate - src/TheTerrace/Features - has 31 folders that are UI features, not bounded contexts: AskAi, Ai and Conversation almost certainly share one model. Inferring 31 contexts from 31 folders would produce a diagram that looks authoritative and teaches the user something false about their own system, which is worse than a missing one because a wrong boundary is harder to notice. Attributes in code were rejected because they require editing the analysed repository to analyse it. STATUS PROPOSED: this is the one Phase-3 input with no evidence behind it and needs the product owner's confirmation.",
      "artifacts": [
        "docs/adr/0016-bounded-context-declaration.md"
      ],
      "tags": [
        "phase-3",
        "ddd"
      ],
      "git": {
        "before": null,
        "after": "e8f521d185d7c66ecf1916a89535a698ece80764",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0035",
      "datetime": "2026-08-28T20:39:23Z",
      "session": "decisions-d1-d7-2026-08-28",
      "kind": "architecture",
      "skill": null,
      "title": "ADR-0010 promoted to accepted: dispatch proven against a live session",
      "prompt": null,
      "summary": "DispatchLiveSessionTests runs a real daemon over a real pipe, dispatches to a real ConPTY PowerShell, and requires a unique marker to come back OUT of the terminal before passing. Also added workspace.open (Ctrl+K, O) so a workspace can be chosen rather than only inherited from an environment variable.",
      "rationale": "The existing dispatch tests wrote to a FIXTURE session, so they proved the receipt consistent with itself and said nothing about whether an accepted prompt reached a process that could act on it. A receipt alone cannot distinguish delivered from written-into-a-void; the marker can. Residual stated rather than closed: the live session is a shell, not an agent CLI, so agent-specific behaviour belongs to the session adapter (ADR-0007) rather than to this receipt - and if dispatching to a real agent CLI later shows the receipt itself must change, this decision is the one to revisit.",
      "artifacts": [
        "docs/adr/0010-two-phase-dispatch-receipt.md"
      ],
      "tags": [
        "phase-2",
        "dispatch"
      ],
      "git": {
        "before": null,
        "after": "e8f521d185d7c66ecf1916a89535a698ece80764",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0036",
      "datetime": "2026-08-28T21:16:30Z",
      "session": "decisions-d1-d7-2026-08-28",
      "kind": "architecture",
      "skill": null,
      "title": "ADR-0016 accepted: bounded contexts declared in one validated file",
      "prompt": null,
      "summary": "Promoted from proposed to accepted on the product owner's confirmation. A committed docs/bounded-contexts.yaml validated against extracted symbols; unknown namespaces fail, coverage is reported, overlap is an error.",
      "rationale": "Raised as proposed because it is the one Phase-3 input evidence cannot decide, and recorded as a judgement made once rather than a finding so a later reader is not misled about its basis.",
      "artifacts": [
        "docs/adr/0016-bounded-context-declaration.md"
      ],
      "tags": [
        "phase-3",
        "ddd"
      ],
      "git": {
        "before": null,
        "after": "46598e7812ea9bfe854ead3abeaf8e53fe7c8450",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0037",
      "datetime": "2026-08-28T21:16:30Z",
      "session": "decisions-d1-d7-2026-08-28",
      "kind": "design",
      "skill": null,
      "title": "Phase-3 components 1 and 2 built; joins computed with confidence as the deliverable",
      "prompt": null,
      "summary": "BicepExtractor and EfSchemaExtractor ship, wired into discovery and the composite router. JoinProjection computes code-to-schema, schema-to-infrastructure and code-to-infrastructure edges, each carrying how it was established. TheTerrace now indexes 7 scopes and 12,034 assertions with five disclosures.",
      "rationale": "Joins are computed rather than stored: writing a derived claim back as a fact would put two definitions of one quantity in the store and they would drift the first time an extractor changed. A convention-derived join is Inferred however obvious it looks, and no join is made on an unresolved Bicep name at all - the gap is disclosed instead. Indexing TheTerrace exposed a defect the fidelity work could not: api_version put dates in the graph and resource_name_expression put unevaluated strings there, because every assertion object was being upserted as a node. The predicate rule now lives in one place used by both ingest and search - the first fix touched only ingest and search kept returning the junk, which is two places deciding the same thing with one of them wrong.",
      "artifacts": [
        "src/AiDe.Core/Projections/JoinProjection.cs"
      ],
      "tags": [
        "phase-3"
      ],
      "git": {
        "before": null,
        "after": "46598e7812ea9bfe854ead3abeaf8e53fe7c8450",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0038",
      "datetime": "2026-08-28T21:16:30Z",
      "session": "decisions-d1-d7-2026-08-28",
      "kind": "design",
      "skill": null,
      "title": "Agent dispatch measured: the receipt is right and agents have no readiness signal",
      "prompt": null,
      "summary": "Dispatch into a real claude CLI records PtyWriteAccepted and the agent never answers, because Claude Code opens on a modal trust gate that consumes the prompt. Reproduced in two working directories. ADR-0010's residual is re-characterised rather than closed.",
      "rationale": "The protocol behaved exactly as designed: a write WAS accepted, bytes reached the pty, and the receipt never claimed delivery to an agent because it never claims that - the independent marker check caught the difference. The gap is that an agent CLI has no equivalent of OSC 133: showing a trust gate, authenticating, mid-response and ready all look identical from outside, and a prompt dispatched into any of the first three is silently consumed. That is ADR-0007's contract to grow, with a refusal when readiness cannot be established. The trust dialog was deliberately NOT auto-confirmed - that would be the tool answering a safety question on the user's behalf, and a green result bought that way is worse than a red one.",
      "artifacts": [
        "spikes/agent-dispatch/RESULT.md"
      ],
      "tags": [
        "phase-2",
        "dispatch"
      ],
      "git": {
        "before": null,
        "after": "46598e7812ea9bfe854ead3abeaf8e53fe7c8450",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0039",
      "datetime": "2026-08-28T22:15:43Z",
      "session": "decisions-d1-d7-2026-08-28",
      "kind": "design",
      "skill": null,
      "title": "Dispatch refuses when session readiness cannot be established (ADR-0007)",
      "prompt": null,
      "summary": "SessionReadiness is three-valued - Ready, NotReady, Unknown - and BoundaryDispatcher refuses anything but Ready, before the write-ahead so a refusal leaves no durable attempt. Readiness evidence today means shell integration: OSC 133 signed with the session nonce.",
      "rationale": "Unknown and NotReady are different situations with different correct responses, and collapsing them is how a prompt gets sent into a dialog box - measured against a real agent CLI whose trust gate consumed the prompt. Derived output timing deliberately does not count as evidence: a quiet agent mid-thought looks exactly like an idle one. The refusal happens before anything is made durable, so there is no Pending attempt to sweep and a retry once ready is a first attempt rather than a duplicate.",
      "artifacts": [
        "src/AiDe.Core/Dispatch/SessionReadiness.cs"
      ],
      "tags": [
        "phase-2",
        "dispatch"
      ],
      "git": {
        "before": null,
        "after": "0f4223c3b22d86a4df15b551be43bd62dc351104",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0040",
      "datetime": "2026-08-28T22:15:43Z",
      "session": "decisions-d1-d7-2026-08-28",
      "kind": "design",
      "skill": null,
      "title": "Bounded contexts load and validate; TheTerrace's map covers 68% of its declared symbols",
      "prompt": null,
      "summary": "BoundedContextReader loads a deliberately small YAML subset, validates every include pattern against extracted symbols, treats overlap as an error and reports coverage. TheTerrace's authored map: 5 contexts, 68% of 1,432 declared symbols, 459 deliberately uncovered.",
      "rationale": "Two corrections came from first real use. The subset reader rejected the first genuine map because it used folded block scalars - that is the simplify: marker's named upgrade trigger firing rather than the parser growing on convenience, so folded scalars are supported and anchors and nested maps are still rejected by name. And coverage was first computed over every node in the graph, reporting 52 percent of 2,086 symbols with a denominator that included AngleSharp and Azure package types nobody can assign to a context; it is now over subjects the repository DECLARES, which is 68 percent of 1,432. A percentage with the wrong denominator is a confident wrong number, and coverage is exactly the figure someone would quote.",
      "artifacts": [
        "src/AiDe.Core/Extraction/BoundedContextMap.cs"
      ],
      "tags": [
        "phase-3",
        "ddd"
      ],
      "git": {
        "before": null,
        "after": "0f4223c3b22d86a4df15b551be43bd62dc351104",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0041",
      "datetime": "2026-08-28T22:31:22Z",
      "session": "decisions-d1-d7-2026-08-28",
      "kind": "design",
      "skill": null,
      "title": "The terminal loads the user's profile; the menu bar makes commands discoverable",
      "prompt": null,
      "summary": "Two user-reported defects. -NoProfile removed the user's own tooling from PATH inside the product's terminal; the profile now loads and the integration wraps whatever prompt it finds. And a menu bar built from the command catalog replaces chord-only access to opening a workspace. New terminals also open in the workspace root.",
      "rationale": "The determinism argument for -NoProfile was real and was the wrong trade for a developer tool: a terminal in which the user's tools are not on PATH is not a terminal they can work in. It is met by ORDER instead - the profile runs first, then the integration script captures and wraps the prompt it finds, so a profile cannot redefine the prompt after us. A test asserts -NoProfile is absent, because it is a one-word change nothing else would notice. The menu is built from the same catalog the palette reads, so it cannot offer something the product no longer does, and every item shows its chord - a command reachable only by an untold chord is not a feature.",
      "artifacts": [
        "src/AiDe.App/Workbench/MainMenuBuilder.cs"
      ],
      "tags": [
        "defect",
        "terminal"
      ],
      "git": {
        "before": null,
        "after": "4bbda153310879b4501b4125ca8062cef1600eae",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0042",
      "datetime": "2026-08-28T22:31:22Z",
      "session": "decisions-d1-d7-2026-08-28",
      "kind": "design",
      "skill": null,
      "title": "Contexts are drawn; agents can now be READY; a declared [Table] join is Verified",
      "prompt": null,
      "summary": "ContextProjection groups the graph by declared context and counts crossings separately from internal edges, with direction kept. AgentReadinessWatcher gives an agent a positive readiness signal from a configured prompt marker, named as weaker evidence than the nonce. A [Table] attribute produces a Verified code-to-schema join and suppresses the conventional one. Bicep modules are exercised.",
      "rationale": "Owners are resolved for edge TARGETS as well as subjects - resolving only subjects left every node that appears solely as a target outside every context, so a crossing between two contexts counted as none. Readiness from an observed pattern is accepted because the alternative measured in spikes/agent-dispatch is that an agent can only ever be refused, a correct refusal that is also a dead end; it is a distinct evidence kind because ADR-0007's bar for claiming the agent ACCEPTED a prompt is unchanged and still unmet, and readiness is lost again on new output because a watcher that latched would report a mid-response agent as available. A declared table suppresses the conventional match so the user is never given two edges for one question the code already answers.",
      "artifacts": [
        "src/AiDe.Core/Projections/ContextProjection.cs"
      ],
      "tags": [
        "phase-3"
      ],
      "git": {
        "before": null,
        "after": "4bbda153310879b4501b4125ca8062cef1600eae",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0043",
      "datetime": "2026-08-28T22:43:41Z",
      "session": "decisions-d1-d7-2026-08-28",
      "kind": "design",
      "skill": null,
      "title": "The context map is drawn; agents can be dispatched to; Fluent ToTable is a declaration",
      "prompt": null,
      "summary": "ContextMapSurface renders contexts as boxes with symbol, internal-edge and crossing counts, colour-matched to the canvas, and refuses to draw an invalid map. TerminalSurface feeds AgentReadinessWatcher from the same chunks the screen gets. Fluent Entity<T>().ToTable(\"name\") is read as a declaration. Menu gains Edit/Window and recent workspaces.",
      "rationale": "The crossing count is what a context map exists to show - a context with none is isolated and one with hundreds is not bounded, and neither is visible from a list of names. An invalid map renders its problems rather than a partial diagram, because a diagram drawn from a file that failed validation is wrong in a way nobody can see. Readiness is fed from the same chunk the screen receives so it cannot disagree with what the user is looking at. Only literal table names are read, the same rule the Bicep reader follows. A new coverage test caught workbench.reorderSurface having no menu entry - a command in the catalog and no menu is reachable only by a chord again, which is the defect the menu exists to fix.",
      "artifacts": [
        "src/AiDe.App/Workbench/ContextMapSurface.cs"
      ],
      "tags": [
        "phase-3",
        "ui"
      ],
      "git": {
        "before": null,
        "after": "e1941d81ade0d23d75aed97a7995cd79c9180674",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0044",
      "datetime": "2026-08-28T23:09:29Z",
      "session": null,
      "kind": "design",
      "skill": null,
      "title": "Client defects: menu legibility, dead anchor strips, default layout, drop placement",
      "prompt": null,
      "summary": "Four defects reported against the running client, all fixed with a control each.\n\n1. **Menu wording illegible.** The Menu had a dark background and light foreground, but a\n   MenuItem's *dropdown* renders in a popup that keeps the system light template — so light text\n   landed on a light background. Fixed by styling MenuItem/ContextMenu/Separator in App.xaml, not\n   by recolouring the Menu again.\n2. **Dead icon strips down the left and right edges.** They were AvalonDock's auto-hide anchor\n   sides, rendered permanently because the theme gives them a visible background even when empty.\n   Nothing in this build auto-hides a pane, so a control that cannot do anything was showing\n   controls. Collapsed via a LayoutAnchorSideControl style.\n3. **Default layout.** Now console at the bottom, workspace column on the left (Explore,\n   Provenance, Contexts), graph column on the right (Graph, Domain) — 0.38/0.62 across, 0.68/0.32\n   down.\n4. **Dropping a tab reordered unrelated panes.** MoveSurface always wrapped the target in a NEW\n   split, so dropping right of the right-hand pane produced [Left, [Right, New]]: the pane landed\n   where the user asked in the tree and somewhere else on screen, and the nested split's fresh\n   50/50 weights moved panes the user never touched. Now, when the parent split already runs in\n   the drop's orientation, the surface is inserted as a SIBLING at the requested index and takes\n   half of the target pane's own weight — every other pane keeps its width. Three tests pin it,\n   including that the neighbours' weights are unchanged.",
      "rationale": null,
      "artifacts": [
        "src/AiDe.App/App.xaml",
        "src/AiDe.Core/Workbench/LayoutService.cs",
        "src/AiDe.Core/Workbench/LayoutModel.cs"
      ],
      "tags": [
        "defect",
        "ui"
      ],
      "git": {
        "before": null,
        "after": "bdfd15cc0b7c30bc3f156e2ba877ce0b5afeae75",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0045",
      "datetime": "2026-08-28T23:09:29Z",
      "session": null,
      "kind": "architecture",
      "skill": null,
      "title": "Phase 3: contexts surface, proven dispatch refusal, context filter, agent terminals, dependsOn",
      "prompt": null,
      "summary": "Five steps on the Phase 3 join.\n\n1. **Contexts surface.** Declared bounded contexts render as boxes carrying symbols, internal\n   edges and crossings — the crossing count is the evidence for whether the boundary held. An\n   invalid map renders its problems rather than a partial diagram.\n2. **Dispatch refuses an unready agent, proven.** The agent-dispatch spike now exits 8 (REFUSED)\n   where it previously reported PtyWriteAccepted for a prompt Claude Code's trust gate ate. The\n   refusal happens before the write-ahead, so nothing is typed into whatever dialog is on screen\n   and no durable attempt is recorded.\n3. **Context click filters the graph.** Choosing a context box shows only that context on the\n   canvas and states how many neighbours were hidden — a filter that hides without saying so is\n   how a graph starts lying. Mouse and keyboard both, with automation names.\n4. **New agent terminal command.** Ctrl+K, A opens a terminal bound to a chosen agent CLI, offered\n   only for agents actually on PATH. AddSurface joined the layout operation union and the SC 2.5.7\n   conformance test immediately failed it for having no declared keyboard equivalent — the control\n   working, not a nuisance.\n5. **Bicep dependsOn edges.** depends_on is now extracted, bounded to each declaration's own span\n   so a later resource's dependencies cannot be attributed to an earlier one.",
      "rationale": null,
      "artifacts": [
        "src/AiDe.App/Workbench/ContextMapSurface.cs",
        "spikes/agent-dispatch/RESULT.md",
        "src/AiDe.Core/Extraction/BicepExtractor.cs"
      ],
      "tags": [
        "phase-3"
      ],
      "git": {
        "before": null,
        "after": "bdfd15cc0b7c30bc3f156e2ba877ce0b5afeae75",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0046",
      "datetime": "2026-08-28T23:46:55Z",
      "session": null,
      "kind": "architecture",
      "skill": null,
      "title": "Phase 3 surfacing: restorable runtime surfaces, configurable readiness, openable crossings, a joins pane, ranked uncovered",
      "prompt": null,
      "summary": "Five steps to make the Phase 3 evidence usable. Two of the five were proposed on a wrong premise and\nwere corrected by opening the code first.\n\n1. **A surface created at runtime now survives a restart.** Layout persistence already existed and\n   was wired — the proposal to \"add\" it was wrong. The real defect was that availability was decided\n   from a whitelist of surface IDS snapshotted from the default layout, so an agent terminal, whose\n   id is minted when it opens (agent:claude#a1b2c3), was dropped on EVERY launch and announced as no\n   longer available. Availability is now \"can content be built for this\", which is a property of the\n   surface's KIND. A surface whose kind this build no longer has is still dropped and still reported,\n   so the control keeps firing for the case it was written for.\n2. **Readiness markers are configurable per agent.** A built-in marker that does not match a real\n   agent's prompt refuses that agent forever, and the only way to change one was a rebuild.\n   agent-readiness.json in the workspace state directory overrides or adds them. A pattern that does\n   not compile is reported and the built-in stays in force — never \"assume ready\". An explicitly\n   empty marker means \"this agent has none\", which makes the refusal deliberate rather than the\n   accident of a pattern that happens never to match. AgentReadinessWatcher.LastJudged exposes the\n   tail it actually tested, so tuning is measurement rather than guessing at what an agent prints.\n3. **A crossing can be opened.** \"Editorial → Football, 47 edges\" was a claim about the user's code\n   they could not check, act on or disagree with. Each crossing now carries the member edges, capped\n   at 200 — with Weight staying the true total and Undisclosed stating the difference, so the cap\n   never becomes a quieter wrong number.\n4. **The joins are visible.** JoinProjection was written, tested, and had NO production caller: a\n   projection nobody can see. It is now a surface in the workspace column, rendering Verified and\n   Inferred under separate headings with the basis on every row, and stating what could not be\n   joined rather than reading as completeness.\n5. **Uncovered symbols became a task.** \"68% of 1,432 covered\" tells the user a number and gives\n   them nowhere to start. Uncovered symbols are now ranked by namespace, largest first, with\n   examples. The grouping is presentation only — nothing assigns a symbol to a context, because a\n   symbol placed in \"the nearest\" one is inference dressed as a declaration.",
      "rationale": null,
      "artifacts": [
        "src/AiDe.Core/Workbench/LayoutStore.cs",
        "src/AiDe.Core/Terminal/AgentReadinessProfiles.cs",
        "src/AiDe.Core/Projections/ContextProjection.cs",
        "src/AiDe.App/Workbench/JoinSurface.cs"
      ],
      "tags": [
        "phase-3"
      ],
      "git": {
        "before": null,
        "after": "971a687bb86a177cc464a087645ec42a0737ac39",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0047",
      "datetime": "2026-08-28T23:46:55Z",
      "session": null,
      "kind": "knowledge",
      "skill": null,
      "title": "DC-021 registered: a fixture that restates what the product declares",
      "prompt": null,
      "summary": "DC-021 — a fixture restates what the product declares, so shipping a feature breaks unrelated tests.\n\nAdding the joins surface turned LayoutUpgradeTests red with AIDE-LAYOUT-PARTIAL-RESTORE: a migration\nerror, for a change that had nothing to do with migration. The fixture held its own copy of \"the\nsurfaces this release ships\".\n\nThis was the THIRD occurrence and the first time it was registered. WorkbenchStoreTests hit it twice\nand was fixed in place, with a comment saying so — a repair scoped to the file where it hurt rather\nthan to the class. Registered now, with every layout fixture deriving its surface set from\nLayout.Default(). Residual risk recorded honestly: derivation is a convention, and nothing yet fails\nwhen a new fixture types the list out again.",
      "rationale": null,
      "artifacts": [
        "docs/lessons/defect-classes.md"
      ],
      "tags": [
        "continuous-improvement"
      ],
      "git": {
        "before": null,
        "after": "971a687bb86a177cc464a087645ec42a0737ac39",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0048",
      "datetime": "2026-08-29T00:05:32Z",
      "session": null,
      "kind": "knowledge",
      "skill": null,
      "title": "Agent readiness measured: the marker approach cannot work for a full-screen agent",
      "prompt": null,
      "summary": "Step 1 of the five was \"tune claude's readiness marker against real output — one measurement turns\ndispatch from always-refused into working.\" The measurement was taken, and it withdraws my own\nnext-step.\n\nA new instrument, `observe-agent`, launches an agent CLI under ConPTY and prints what it actually\ndraws, with control characters made visible, and reports whether each configured marker matched THAT\noutput. It asserts nothing; it exists so a marker is written against measured bytes.\n\nThree findings, all recorded in spikes/agent-readiness/RESULT.md.\n\n1. The trust gate appears even in C:\\Projects\\ai-de — the directory where this project's Claude Code\n   sessions run every day. It is not an artefact of an unfamiliar folder; it is the normal first\n   screen for a session this shell starts.\n2. The chevron IS in the output — at ESC[14;2H, as the selection cursor of the trust dialog, sitting\n   on \"No, exit\". A looser marker, which is the obvious repair when a pattern does not match, would\n   have reported READY at the exact moment dispatch is most dangerous: the Enter that submits a\n   prompt is the Enter that confirms \"No, exit\". The shipped conservative pattern correctly reports\n   no match.\n3. The output is a full-screen TUI drawn with absolute cursor addressing, not lines. A tail-anchored\n   regex over the byte stream asks where the cursor went last, not what the screen says. Making the\n   pattern cleverer cannot fix that — the information is not in the ordering of the bytes.\n   Establishing readiness for a full-screen agent needs a VT parser maintaining a cell grid.\n\nConsequence: the marker mechanism is kept, the built-in claude marker is left exactly as it is\nbecause refusing is the right answer for the screens observed, and screen-buffer readiness is\nrecorded as the next real step rather than attempted. The captured output is committed as a test\nfixture and ARealTrustGateIsNotMistakenForAPrompt pins it — observed failing on a loosened marker\nbefore being accepted.",
      "rationale": null,
      "artifacts": [
        "spikes/agent-readiness/RESULT.md",
        "tests/AiDe.Core.TerminalHost/ObserveProbe.cs"
      ],
      "tags": [
        "spike",
        "dispatch"
      ],
      "git": {
        "before": null,
        "after": "97aad79e5061819896356312a83a957cc4152280",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0049",
      "datetime": "2026-08-29T00:05:32Z",
      "session": null,
      "kind": "architecture",
      "skill": null,
      "title": "Joins to graph, a real layout migration, dependsOn consumed, and a gate for DC-021",
      "prompt": null,
      "summary": "The remaining four steps.\n\n**Joins are clickable through to the graph.** A pane naming symbols the canvas can already draw, and\nleaving the user to retype them into a search box, is two tools sharing a window. Activating a join\nrow centres the graph on its From end — the side the user is reasoning about — clearing any context\nfilter first, because centring on a node the canvas has been told not to draw would look like a\nclick that did nothing.\n\n**A real migration replaced the placeholder.** LayoutMigrations shipped with one entry: a worked\nEXAMPLE describing a rename the product never performed. The chain therefore looked exercised while\ndoing nothing, and the joins pane added last turn would have reached only users with no saved layout\n— nobody who has used the product. The chain now carries the real v1 to v2 step, which adds the pane\nbeside its anchor in whatever tree the user actually has; the rename example moved into the test that\ndocuments it. If the anchor is gone the migration does nothing: a user who closed that area has said\nsomething, and re-opening it under a new name is not an upgrade.\n\n**dependsOn is consumed.** The Bicep extractor emitted depends_on and nothing read it — the same\nshipped-but-unreachable shape as the join projection itself. It is now a Verified join edge, and this\nis the one place in the projection where Verified is cheap to earn: both ends are symbols the\ntemplate names, and the edge is read rather than corresponded.\n\n**DC-021 has an automated control.** tools/verify-fixture-derivation.py derives the product's own\nvocabulary from the product and fails when three or more of those identifiers appear as literals in\none collection in a test. It found two live cases the hour it was written — one of them the kinds set\nI added the previous day, in the same commit that registered the class. Wired into CI. It fails\nclosed: an empty derived vocabulary is an error, not a pass over everything.",
      "rationale": null,
      "artifacts": [
        "src/AiDe.Core/Workbench/LayoutMigrations.cs",
        "tools/verify-fixture-derivation.py",
        "src/AiDe.Core/Projections/JoinProjection.cs"
      ],
      "tags": [
        "phase-3"
      ],
      "git": {
        "before": null,
        "after": "97aad79e5061819896356312a83a957cc4152280",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0050",
      "datetime": "2026-08-29T14:50:34Z",
      "session": null,
      "kind": "architecture",
      "skill": null,
      "title": "Readiness moves to a screen model; the trust gate becomes a state the user can act on",
      "prompt": null,
      "summary": "The readiness question, answered at the right layer.\n\n**A screen model.** ScreenBuffer is a small VT interpreter — cursor movement, erasure, text, and\nnothing else. Colour, styling, scroll regions and alternate buffers are consumed and discarded\nbecause none of them change which cell a character occupies. It exists because the measurement said\nthe byte stream cannot answer the question: an agent draws with absolute cursor addressing, so the\nlast bytes are wherever the cursor went, not what the user is looking at. Fed the captured trust-gate\nbytes, it reconstructs the dialog across rows 1 to 16.\n\n**Readiness now matches the rendered screen**, anchored to the last drawn line rather than the tail\nof the buffer. The built-in markers are anchored at both ends: the measured gate draws \"❯ No, exit\",\nand a pattern allowing text after the chevron would call that dialog a prompt. They remain unverified\nagainst a READY agent, because reaching one means answering the trust gate, which this tool will not\ndo on the user's behalf — that is stated where the patterns live.\n\n**Attention is a state, not a silent refusal.** An agent showing a trust gate is not busy and not\nready; it is waiting for a person, and the measurement showed that gate is the NORMAL first screen.\nThe watcher reports NeedsAttention with the line that matched, searched across the whole screen\nrather than the last line, because the gate puts its question ten rows above its buttons. Attention\noutranks readiness. The pane announces it once per transition, not per repaint.",
      "rationale": null,
      "artifacts": [
        "src/AiDe.Core/Terminal/ScreenBuffer.cs",
        "src/AiDe.Core/Terminal/AgentReadinessWatcher.cs"
      ],
      "tags": [
        "dispatch"
      ],
      "git": {
        "before": null,
        "after": "27ce744843701cf1416eb4140acddad2c4a0b2f9",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0051",
      "datetime": "2026-08-29T14:50:34Z",
      "session": null,
      "kind": "knowledge",
      "skill": null,
      "title": "DC-022: a predicate shared by two extractors, joined as if it had one meaning",
      "prompt": null,
      "summary": "The Joins pane, run over TheTerrace — and the defect that found.\n\nFour turns of extractors and panes had shipped without anyone asking whether the joins are any good\non a real codebase. The first run answered: 7,426 verified joins, every one carrying \"declared in the\nresource's dependsOn\", in a repository with no Bicep and no dependsOn anywhere in it.\n\ndepends_on is not a Bicep word. The C# extractor emits it for type dependencies. The join was written\nagainst the PREDICATE rather than the kind of thing the predicate was on, and the basis was a fixed\nstring that never had to agree with the evidence again. It failed in the flattering direction: the\nlargest Verified count the pane had ever shown. Registered as DC-022 and fixed by qualifying on the\nsubject carrying resource_type; two tests, because narrowing a join until it cannot fire is not a fix.\n\nAfter the fix: 0 verified, 59 inferred. Zero is the correct answer for a repository that declares no\n[Table] attributes and has no infrastructure templates, and the pane says so plainly rather than\nimplying completeness.\n\nThe contexts pane in the same run is usable. Operations carries 172 crossings against 225 internal\nedges on 198 symbols — nearly as much traffic leaving as staying — while Football, four times its\nsize, keeps 902 internal against 190 crossing. And 360 of the 474 uncovered symbols are tests, which\nno context map should claim: the namespace grouping turned a number that reads as a gap into one line\nthat reads as correct plus four small namespaces worth a decision.\n\nAlso: the fixture-derivation gate now watches command ids as well (29 identifiers), observed firing\non a planted three-command literal; and the migration chain has an end-to-end test plus one asserting\nthe steps JOIN UP, which fails the moment a version is bumped without a migration beside it.",
      "rationale": null,
      "artifacts": [
        "spikes/joins-on-a-real-repo/RESULT.md",
        "src/AiDe.Core/Projections/JoinProjection.cs",
        "docs/lessons/defect-classes.md"
      ],
      "tags": [
        "continuous-improvement",
        "spike"
      ],
      "git": {
        "before": null,
        "after": "27ce744843701cf1416eb4140acddad2c4a0b2f9",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0052",
      "datetime": "2026-08-29T15:28:36Z",
      "session": null,
      "kind": "architecture",
      "skill": null,
      "title": "Four defects: a Cartesian join, a wrong denominator, a dead Graph pane, and a stale probe",
      "prompt": null,
      "summary": "Four defects, found by running the panes over a real repository and by a clean rebuild.\n\n**1. A Cartesian product presented as 192 findings.** With the Bicep extractor finally routed\ncorrectly, hosted_on matched the whole Microsoft.Sql/* family: 64 tables joined to a server, a\ndatabase AND a virtual-network rule, each edge claiming \"the only literally-named SQL resource in\nthis template\" — of which there were three. Second instance of DC-022, in the join immediately below\nthe one that produced it. A table lives in a DATABASE; narrowed to that, with exactly one candidate\njoining and more than one producing no edges plus a sql-database-ambiguous disclosure. 192 to 64.\n\n**2. Coverage counted artifacts the map was never about.** The uncovered list's second-largest bucket\nwas \"(no namespace)\" at 114, and they were Bicep parameters. A bounded-context map is a statement\nabout a codebase's domain, and counting a template's parameters against it gets worse the more\ninfrastructure a team writes. Coverage is judged against code symbols only now, one rule in one\nplace. 525 uncovered to 412.\n\n**3. The Graph pane rendered nothing at all.** The canvas page carried a JavaScript syntax error —\na stray quote in the legend string — which broke the entire script. The C# compiler cannot see\ninside an embedded page and no unit test renders one.\n\n**4. The control that should have caught it was running a stale binary.** AiDe.App.CanvasProbe was\nnever a ProjectReference of AiDe.App.Tests, unlike the terminal probe and daemon beside it. It had\nbeen built once by a full-solution build and every run since exercised that old executable. The full\nclean the user asked for is what exposed it: the test failed with \"the canvas probe was not built\",\nand once rebuilt from current source it failed for real. Registered as DC-023.\n\nAlso: the readiness watcher now uses the SAME TerminalScreen and VtParser the pane renders with. The\nScreenBuffer written for it last turn was a duplicate of a screen model this repository already had —\ndeleted, because two models of one terminal disagree the first time either is fixed, and readiness\ndisagreeing with what the user is looking at is the whole defect it was built to close.",
      "rationale": null,
      "artifacts": [
        "src/AiDe.Core/Projections/JoinProjection.cs",
        "src/AiDe.App/Workbench/CanvasPage.cs",
        "tests/AiDe.App.Tests/AiDe.App.Tests.csproj",
        "docs/lessons/defect-classes.md"
      ],
      "tags": [
        "defect"
      ],
      "git": {
        "before": null,
        "after": "27adc7d93c323fd278bf0edc91f6fbf7da7c139e",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0053",
      "datetime": "2026-08-29T15:28:36Z",
      "session": null,
      "kind": "knowledge",
      "skill": null,
      "title": "TheTerrace measured: 57 of 72 crossings are one DbContext",
      "prompt": null,
      "summary": "What the panes show on TheTerrace, and the one recommendation worth acting on.\n\n7 of 7 scopes, 12,043 assertions, 4.5 seconds. One verified join — invitationPepper, declared\n@secure(), value never read. 123 inferred: 64 hosted_on and 59 maps_to, every one resting on EF's\nnaming convention and saying so on its own row. Six disclosures.\n\nOperations looked like a boundary that never held — 172 crossings against 225 internal edges on 198\nsymbols. Opening the crossing says otherwise: 57 of the 72 Football-to-Operations edges are a single\nclass, TheTerrace.Infrastructure.Data.AppDbContext, which sits inside Operations' Infrastructure.*\npattern. Every repository that touches the database registers as a domain boundary crossing. That is\nshared persistence, not coupling between two contexts.\n\nThe recommendation, which is TheTerrace's to make and not this repository's: give shared\ninfrastructure its own context named for what it is, or leave it uncovered. Either way the crossing\ncounts start measuring domain coupling instead of counting the ORM. The other crossings survive that\nchange and are real — IAiCompletion and IPromptMapper reaching from Football and Editorial into\nAssistant, ICoachReader and ISquadReader reaching back.\n\nThe DC-022 residual is now measured rather than assumed. A predicate-by-extractor census over the\nsame run: declared_in, has_type and discloses are each emitted by all three extractors. has_type is\nconsumed by predicate in three places and is safe BY ACCIDENT — its object values happen to partition\ncleanly by producer, and nothing enforces that. The spike prints the census on every run so the next\ncollision is visible before it is joined.",
      "rationale": null,
      "artifacts": [
        "spikes/joins-on-a-real-repo/RESULT.md"
      ],
      "tags": [
        "spike"
      ],
      "git": {
        "before": null,
        "after": "27adc7d93c323fd278bf0edc91f6fbf7da7c139e",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0054",
      "datetime": "2026-08-29T15:32:49Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "kind": "knowledge",
      "skill": "collectknowledge",
      "title": "Established WPF modern-styling and operational/test-dashboard knowledge for the AiDe.App client",
      "prompt": "Build a WPF client with modern/softer styling (rounded corners, drop shadows): acquire best practices for WPF window/tab styling; best permissive-license open-source WPF UI control/styling libraries; exemplars for modern native UX, IDE and video-editing interfaces; plus diagramming, UML/generative-from-UML, ERM/ORM visualization, and visualizing test results, CI/CD execution and operational logs/metrics.",
      "summary": "Design should reach the modern-soft look via the in-box .NET 9/10 Fluent theme + WindowChrome + DWM rounded corners (library-optional); reserve WPF effects for chrome not hosted panes (airspace); build three distinct dashboard panes (test/CI/metrics) that expose silent failures (percentiles, gate-ran, flaky-vs-failing) and use MIT charting only.",
      "rationale": "The client must look modern rather than boxy; these are the sourced, permissive-license means and the design rules, with diagram/UML/ERM reconciled to existing bases.",
      "artifacts": [
        "docs/knowledge/wpf-modern-ui-styling/index.md",
        "docs/knowledge/operational-and-test-dashboards/index.md"
      ],
      "tags": [],
      "git": {
        "before": "27adc7d93c323fd278bf0edc91f6fbf7da7c139e",
        "after": "68598f3479f1ce2028f54b9007d7aec40d085847",
        "branch": "main",
        "pushed": true,
        "commits": [
          "68598f3 docs: two knowledge bases on the client's visual layer",
          "5c23ffe fix: the Graph pane was rendering nothing, and the test for it was a month stale"
        ]
      }
    },
    {
      "id": "cl-0055",
      "datetime": "2026-08-29T15:39:44Z",
      "session": null,
      "kind": "architecture",
      "skill": null,
      "title": "Embedded scripts get a parser; DC-022's residual closed by qualifying on subject shape",
      "prompt": null,
      "summary": "An embedded page gets no compiler, no analyzer and no test — so it now gets a parser.\n\nverify-embedded-scripts.py parses every inline <script> this repository embeds in a C# string or an\nHTML template. With Node present it uses node --check, which is the parser the browser uses; without\nit, a narrower lexical scan for unterminated strings and unbalanced brackets. It NAMES which mode it\nran in, because a gate that silently degrades to a weaker check is worse than one that fails, and it\nfails closed when it finds nothing to check.\n\nThirteen script blocks in under a second: the canvas page, and twelve in the docs templates — the\nDocs Explorer, the Audit Explorer, the dream and mockup harnesses — every one of which would fail\nexactly as silently, rendering an empty shell nobody would attribute to a typo.\n\nThe gate's FIRST finding was its own false positive: a <script src> inside an HTML comment, reported\nas a dead script. Fixed rather than tuned around. Then verified against the real defect —\nreintroducing the stray quote produced \"CanvasPage.cs: script starts at line 52 — SyntaxError:\nInvalid or unexpected token\".\n\nThat answers both the \"add a check\" and the \"audit the other pages\" steps: the audit is the gate, and\nit runs on every push rather than once.",
      "rationale": null,
      "artifacts": [
        "tools/verify-embedded-scripts.py",
        ".github/workflows/build.yml"
      ],
      "tags": [
        "defect"
      ],
      "git": {
        "before": null,
        "after": "68598f3479f1ce2028f54b9007d7aec40d085847",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0056",
      "datetime": "2026-08-29T15:39:44Z",
      "session": null,
      "kind": "knowledge",
      "skill": null,
      "title": "TheTerrace's map, measured: Operations 172 crossings to 47",
      "prompt": null,
      "summary": "DC-022's residual, closed for the consumer that had it.\n\nA predicate-by-extractor census over a real repository showed declared_in, has_type and discloses are\neach emitted by all three extractors, and that has_type's object values partition by producer ONLY BY\nACCIDENT — class, record, table, azure-parameter happen not to collide, and nothing enforced that.\n\nEvery has_type read in JoinProjection is now qualified by the shape of the SUBJECT as well as the\nobject value: a table must carry the table: prefix, an Azure parameter must carry a # fragment, and a\ncode type must be a dotted symbol with no scope prefix. Three tests: a bicep-scoped subject claiming\nto be a class is not joined as one, a code type still IS joined to its table, and a code symbol\ndescribed as a \"table\" by some future extractor does not become a join target. The middle one matters\nas much as the others — a qualifier that also blocks the real case is not a fix.\n\nAlso measured, not asserted: the recommendation for TheTerrace's context map. proposed/\nbounded-contexts.yaml moves TheTerrace.Infrastructure.* out of Operations into a Platform context and\nwas run through the same projection. Operations goes from 198 symbols / 225 internal / 172 crossings\nto 109 / 170 / 47, and Football-to-Operations falls from 72 to 15. Platform then carries 161 crossings\nagainst 37 internal edges, which is what shared infrastructure looks like once it is labelled as\nsuch. Operations was never the problem — it was a boundary that mostly holds, wearing the ORM's\ntraffic. Nothing was applied to TheTerrace; the proposed map lives here so the numbers reproduce.\n\nThe dangling docs link resolved itself when the other session's knowledge bases were committed. The\ngraph reports zero dangling targets; I did not fix it and am not claiming to have.",
      "rationale": null,
      "artifacts": [
        "spikes/joins-on-a-real-repo/RESULT.md",
        "src/AiDe.Core/Projections/JoinProjection.cs"
      ],
      "tags": [
        "spike"
      ],
      "git": {
        "before": null,
        "after": "68598f3479f1ce2028f54b9007d7aec40d085847",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0057",
      "datetime": "2026-08-29T16:00:16Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "kind": "knowledge",
      "skill": "collectknowledge",
      "title": "Established unified graph-experience and content-rendering knowledge for the AiDe.App editor",
      "prompt": "Rich end-to-end experience over the unified code graph + knowledge graph: navigate and introspect any node (walk from a C# file node to read code, then to related metadata/knowledge that informed it). We use Obsidian and Graphify for graph enablement with the LLM; compose them for a great in-editor experience. Accumulate knowledge on: knowledge graphs/GraphRAG/Obsidian/Graphify; connected 2D/3D graph and knowledge-base visualization; building KG visualizations/explorers in WPF (best practices, repos, libraries, controls); design/review surfaces for code-editor viewing leveraging VS Code/Eclipse/JetBrains (public-domain/permissive only); and markdown/HTML visualization/rendering controls for the editor (permissive only).",
      "summary": "Design should: build a node-introspection router that fuses the Graphify code graph and the docs knowledge graph and routes each node to its renderer; host the graph explorer as a web force-graph (Sigma/3d-force-graph) in the shared WebView2 pane, not native GraphX; use hybrid GraphRAG (bounded neighbourhoods) and evaluate LazyGraphRAG/LightRAG on the code graph; render content native (Markdig.Wpf, AvalonEdit/RoslynPad) for plain markdown/C# and web (Monaco/HTML) for breadth/interactivity; carry edge provenance/confidence everywhere.",
      "rationale": "The core user scenario is walking from a C# node to the knowledge that informed it; these are the sourced, permissive-license means (all MIT/BSD/Apache) and the composition of Obsidian+Graphify, with GraphRAG cost reconciled.",
      "artifacts": [
        "docs/knowledge/graph-experience-and-visualization/index.md",
        "docs/knowledge/editor-and-content-rendering-surfaces/index.md"
      ],
      "tags": [],
      "git": {
        "before": "f335c4f6ef9dd5544c99ba7565982f58bd7ed2bf",
        "after": "f335c4f6ef9dd5544c99ba7565982f58bd7ed2bf",
        "branch": "main",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0058",
      "datetime": "2026-08-29T16:24:22Z",
      "session": null,
      "kind": "knowledge",
      "skill": null,
      "title": "DC-024: the worktree cleanup asked a ledger whether anyone was there",
      "prompt": null,
      "summary": "This session moved into its own worktree, which repo guidance required from the start and it did not\ndo. Every commit before this one was written directly in the primary checkout — recorded here rather\nthan quietly corrected, because the register exists for the agent's own defects too.\n\nCleaning up afterwards produced a worse finding. coord worktree cleanup --remove deleted\nC:/Projects/ai-de-facelift reporting \"clean, merged, unheld\" — and a LIVE session recreated the tree\nwithin the minute and wrote a marker reading \"facelift worktree in use\". Nothing was lost: the\ncleanliness checks were all correct, the tree had no uncommitted work and no unique commits. The\nLIVENESS check was the wrong one. It read live_keys — a registration ledger — and that session had\nnever run coord session start. A ledger says who signed in; it never says nobody is there.\n\nRegistered as DC-024 and controlled: worktree_safety now ends with a filesystem condition. A tree\nwhose files were modified within the last hour is HELD whatever the ledger says. The scan skips build\noutput, is capped at 4,000 files, and treats hitting the cap as in-use rather than as an answer,\nbecause a partial scan cannot prove absence. The reason string carries the age, so \"idle\" is a\nmeasurement someone can disagree with rather than a verdict.\n\nObserved failing in both directions on a scratch tree: clean, merged and unregistered it reported\nKEEP - touched recently - last modified 0 minute(s) ago; with its files backdated two hours it became\nWOULD - clean, merged, unheld, idle - last modified 120 minute(s) ago. A safety rule that never\npermits anything is not a safety rule.",
      "rationale": null,
      "artifacts": [
        "docs/ai-forward-pack/scripts/coord-core.py",
        ".claude/knowledge/session-worktree-discipline.md"
      ],
      "tags": [
        "continuous-improvement"
      ],
      "git": {
        "before": null,
        "after": "21068ab3fcf1a7cd9021ac5babfa9d7f95495b6c",
        "branch": "session/phase3-pane-probes",
        "pushed": null,
        "commits": []
      }
    },
    {
      "id": "cl-0059",
      "datetime": "2026-08-29T16:24:23Z",
      "session": null,
      "kind": "architecture",
      "skill": null,
      "title": "Panes rendered and read; a second repository finds absence rendered as coverage",
      "prompt": null,
      "summary": "The panes are now rendered and read, and a second repository was measured.\n\nPaneRenderTests walks the visual tree of the Joins and Contexts panes and asserts on CONTENT, never\non a control count — \"has four children\" passes for four empty labels. In-process on an STA thread\nrather than as another out-of-process probe: the canvas needs a real foreground window because it\ndrives a browser through SendInput, and these panes build their own children. The last case is the\nrule both must keep: no evidence pane, in any of its six states, may render no readable text.\nObserved failing by making one pane silent.\n\nA crossing dominated by one object now says which one. Found by eye once — 57 of 72 edges were\nAppDbContext — and a signal a person has to notice is a signal that gets noticed once. Majority of\nthe listed members, computed over what was actually examined rather than extrapolated to the full\nweight, with the undisclosed count still beside it. Three tests including the boundary: exactly half\nis not domination.\n\nBioHacker was measured as a second repository, deliberately a different shape — 25 scopes, 26 Bicep\nresources, no Entity Framework. Zero joins is the correct answer there and the pane says so. But it\nhas no context map, and the pane reported \"0 uncovered\" and \"Every declared symbol belongs to a\ncontext\" — the sentence a fully-mapped codebase produces. The arithmetic was right, which is why a\ncleverer count could not have fixed it. ContextMapView carries IsDeclared now and says plainly that\nnothing has been claimed about the code yet. One repository could not have found this: TheTerrace has\na map, so every path that runs when there is none had never been exercised.\n\nAnd TheTerrace's Platform split is applied on a branch in its own worktree — docs/platform-context,\ncommitted locally and NOT pushed, because that repository is the user's to publish.",
      "rationale": null,
      "artifacts": [
        "tests/AiDe.App.Tests/PaneRenderTests.cs",
        "src/AiDe.Core/Projections/ContextProjection.cs"
      ],
      "tags": [
        "phase-3"
      ],
      "git": {
        "before": null,
        "after": "21068ab3fcf1a7cd9021ac5babfa9d7f95495b6c",
        "branch": "session/phase3-pane-probes",
        "pushed": null,
        "commits": []
      }
    },
    {
      "id": "cl-0060",
      "datetime": "2026-08-29T16:35:40Z",
      "session": null,
      "kind": "decision",
      "skill": null,
      "title": "A written contract between the core and design sessions",
      "prompt": null,
      "summary": "Two sessions now work this repository at once — core capabilities here, styling and design surfaces\nin the other tree. docs/collaboration/session-contracts.md is the proposal, written down because\n\"we'll coordinate\" is not a coordination mechanism and the first thing two sessions lose is not code\nbut agreement about what each thought was true.\n\nThe seam is one sentence: Core produces view models, Design renders them. A view model is a record\nwith no behaviour and no WPF types, carrying what the user needs to know INCLUDING what could not be\nestablished — a projection that hides a gap forces the surface to invent one. Neither side reaches\nacross: Core does not choose colour or control, Design does not compute a number, because a number\ncomputed in a surface is a second definition of a quantity that already has one.\n\nFile ownership is listed rather than described, with the shared files and the rule each carries.\nDerived files — docs-index.js, audit-data.js — are called out because a hand-merged generated file is\na conflict resolved into a lie.\n\nSection 6 is deliberate: what this does NOT settle. Fast-forward versus pull request, whether view\nmodels should carry presentation hints, and where visual regression evidence lives are joint\ndecisions, and writing one session's preference down as settled is how a proposal becomes a fait\naccompli. Status is proposed, not accepted.\n\nThree requests to Design are recorded in 4a rather than made in conversation, each additive and\nalready on main.",
      "rationale": null,
      "artifacts": [
        "docs/collaboration/session-contracts.md"
      ],
      "tags": [
        "collaboration"
      ],
      "git": {
        "before": null,
        "after": "6db9b6f43e70b8a05994becd344aa86e58e9b37c",
        "branch": "session/phase3-pane-probes",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0061",
      "datetime": "2026-08-29T16:35:40Z",
      "session": null,
      "kind": "architecture",
      "skill": null,
      "title": "One extractor composition, and a bounded read that says what it missed",
      "prompt": null,
      "summary": "Two core defects, both about a boundary telling the truth about itself.\n\n**The running app could not see what the spike could.** The daemon composed only the C# extractor and\nthe fixture adapter, so infrastructure and schema were invisible to the product while a spike\ncomposed all four and reported joins the app had no way to show — two answers to \"what does this tool\nread\", depending which door you came in. WorkspaceExtractors.Default() is now the single composition,\nused by the daemon and the spike, with named arguments.\n\nThat matters because the hand-written form is easy to get wrong SILENTLY: the spike once passed its\nextractors positionally, which put BicepExtractor in the fallback slot and routed every bicep scope\nto the schema extractor. Both failed and the write-up concluded the repository had no Bicep. It has\ntwo templates and 24 resource declarations. CompositeExtractor.RouteFor now exposes the routing\ndecision so a test can read it, and the test was observed failing on that exact mis-ordering:\nexpected \"bicep\", actual \"schema\".\n\n**Every number both panes show was computed from a bounded read that never said so.** The shell\nsearches with a 20,000 cap, describes 4,000 of the matches, and takes 60 neighbours from each. On the\nrepositories measured so far all three are slack — and nothing in the output would change if they\nwere not. The counts would simply be smaller and still be presented as facts. EvidenceRead carries\nwhat the read did not see; the shell announces it once per distinct sentence, and reports BOTH causes\nseparately because \"bigger than the search cap\" and \"these nodes are unusually connected\" have\nopposite fixes. A node returning exactly the limit counts as truncated, because the read cannot tell\nthe difference and guessing in the flattering direction is how a cap becomes a quieter wrong number.",
      "rationale": null,
      "artifacts": [
        "src/AiDe.Core/Extraction/WorkspaceExtractors.cs",
        "src/AiDe.Core/Projections/EvidenceRead.cs",
        "src/AiDe.Daemon/Program.cs"
      ],
      "tags": [
        "phase-3"
      ],
      "git": {
        "before": null,
        "after": "6db9b6f43e70b8a05994becd344aa86e58e9b37c",
        "branch": "session/phase3-pane-probes",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0062",
      "datetime": "2026-08-29T16:46:10Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "kind": "decision",
      "skill": "ui-design",
      "title": "Domain experts + facelift/knowledge/UML-ERM specs and mockups",
      "prompt": "domain experts + specify + ui-design chain",
      "summary": "Roster 26; flat->soft-islands; node-introspection router; derived read-only UML/ERM.",
      "rationale": "Subject-matter judgment + three grounded surfaces.",
      "artifacts": [
        "docs/domain-experts.md"
      ],
      "tags": [],
      "git": {
        "before": null,
        "after": "d81828191646a2031f702165854e6f816918571a",
        "branch": "feature/app-facelift-and-graph-surfaces",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0063",
      "datetime": "2026-08-29T17:03:02Z",
      "session": null,
      "kind": "architecture",
      "skill": null,
      "title": "The panes were reading 50 nodes; the search now has its own ceiling",
      "prompt": null,
      "summary": "Two defects the panes had been living with, both about a boundary lying about how much it saw.\n\n**The panes were computing from at most 50 nodes.** ProjectionService.Find borrowed\nMaxNeighborsCeiling, which is 50. The workbench asked for 20,000 matches to build the context and\njoin panes and received 50 — so crossing counts, join counts and coverage were computed from roughly\nthree percent of a real workspace and presented as the answer, while the spike read the store\ndirectly and showed the whole picture. The two disagreed for days and nothing said so. A search\nreturns identity columns only, so its payload per row is small and its ceiling can be large;\nMaxSearchResultsCeiling is now its own constant at 20,000. On TheTerrace the panes went from 50 nodes\nto 2,164.\n\n**And the read reported a total it had made up.** ReadAssertions counted the rows in front of it and\ncalled that the number of matching nodes, while ResultBounds.OmittedNodes had been carrying the true\nremainder all along. The shell reads the bounds now. The neighbour limit was also wrong in the other\ndirection — the shell asked for 60 against a ceiling of 50, so the truncation check compared against\na limit that could never be reached: a control that could not fire, guarding a cap that was already\nbiting. It reads the service's own constant now, and on TheTerrace it fires for real on 26 nodes.\n\nAlso proven across the pipe: the daemon returns infrastructure evidence, asserted against the\nprovenance extractor id rather than a predicate name. That test first failed against a STALE Release\ndaemon, because LocateDaemon preferred Release if the folder existed rather than following the\nconfiguration the tests were built in — DC-023 in the harness, fixed.",
      "rationale": null,
      "artifacts": [
        "src/AiDe.Core/Projections/ProjectionService.cs",
        "src/AiDe.App/Workbench/WorkbenchShell.cs",
        "tests/AiDe.Core.Tests/DaemonProcessTests.cs"
      ],
      "tags": [
        "phase-3"
      ],
      "git": {
        "before": null,
        "after": "62fe22519a048983d643746158f665b8079f8541",
        "branch": "session/phase3-pane-probes",
        "pushed": false,
        "commits": []
      }
    },
    {
      "id": "cl-0064",
      "datetime": "2026-08-29T17:03:02Z",
      "session": null,
      "kind": "architecture",
      "skill": null,
      "title": "Incremental re-index, and the restart that could never index twice",
      "prompt": null,
      "summary": "Incremental re-index, and two defects it uncovered on the way.\n\nEvery index re-extracted every scope: 4.3 seconds on TheTerrace, 2.8 on another repository, paid in\nfull whether one file changed or none. ScopeFingerprints digests each scope's input files by path,\nsize and modification time, plus an extractor generation so upgrading the product invalidates\neverything rather than leaving a graph built by two extractor versions with nothing saying which.\nTheTerrace re-indexes in 0.1s now, ten scopes reused.\n\nThe reuse is counted separately from the indexing. \"7 of 7 indexed\" would be a true sentence about a\nrun that read nothing, and the question after a surprising graph is always whether it actually\nlooked. It fails towards re-extraction: an unreadable directory, a missing sidecar or a scope whose\ncommitted evidence has gone all produce a re-read, because the cost of an unnecessary extraction is\nseconds and the cost of a skipped one is a graph describing code that no longer exists.\n\nTesting it across a reopen found something unrelated and worse: THE SECOND INDEX OF ANY WORKSPACE\nAFTER A RESTART FAILED. The generation counter lives in memory and started at zero on every open\nwhile the store did not, so it re-used generation 1 and violated the desired-generation primary key.\nThe daemon opens the store fresh every time it starts. Nothing had ever indexed twice across a\nreopen, so nothing had ever noticed. Seeded from the store now.\n\nAnd re-extracting a revision the store already holds surfaced as a raw SQLite UNIQUE-constraint\nexception from the middle of a run. The first fix silenced the natural key with INSERT OR IGNORE —\nwhich broke an existing test asserting that key rejects duplicates, and rightly so: that control is\ndeliberate. The caller is idempotent instead. Re-extracting a revision already committed returns\nwithout writing, the store stays strict, and \"index again\" answers the user's real question, which\nis whether the graph is current for this revision.",
      "rationale": null,
      "artifacts": [
        "src/AiDe.Core/Extraction/ScopeFingerprints.cs",
        "src/AiDe.Core/WorkspaceCore.cs"
      ],
      "tags": [
        "phase-3"
      ],
      "git": {
        "before": null,
        "after": "62fe22519a048983d643746158f665b8079f8541",
        "branch": "session/phase3-pane-probes",
        "pushed": false,
        "commits": []
      }
    },
    {
      "id": "cl-0065",
      "datetime": "2026-08-29T17:28:43Z",
      "session": null,
      "kind": "knowledge",
      "skill": null,
      "title": "DC-013 prevented: ids allocated from a lock-guarded counter every worktree shares",
      "prompt": null,
      "summary": "DC-013 is prevented now rather than detected. Three occurrences, the last between two AGENTS rather\nthan two trees of one — which broke the previous control, because \"run log-writing scripts in the\ntree where the work is\" is advice that cannot reach a session that is not yours.\n\nEvery worktree of a repository shares ONE git common directory. A counter placed there is visible to\nall of them, and an exclusive-create lock makes the read-modify-write atomic. The file's own highest\nid remains the floor, so a counter that is missing, stale or from a fresh clone can only ever be\ncaught up to reality, never fall behind it.\n\nSixteen concurrent allocations issued from two different worktrees of this repository returned\nsixteen distinct ids. The previous allocator returns the same one to all sixteen. Gaps are expected\nand harmless: an id is an identifier, not a count, and the gate checks uniqueness rather than\ncontiguity. Promoted to controlled, with the honest residual — two separate CLONES do not share a\ngit directory and would still collide, and a stale lock falls back to the old behaviour by design.\n\nTwo more things the fingerprint work needed. The scope SET is part of a workspace's shape and changes\nwithout any individual scope changing, so a departed scope is now reconciled out of the sidecar and\nrecorded as a health incident rather than reused forever while its evidence describes code that no\nlonger exists. And a single-file scope is fingerprinted by its own file: treating a Bicep template as\nits containing directory made two templates in one infra/ folder share a basis, so deleting either\ninvalidated both.",
      "rationale": null,
      "artifacts": [
        "docs/ai-forward-pack/scripts/audit-log.py",
        "src/AiDe.Core/Extraction/ScopeFingerprints.cs"
      ],
      "tags": [
        "continuous-improvement"
      ],
      "git": {
        "before": null,
        "after": "dd1c53793935746bc3aa6c5a175c1cf5c99f9a8b",
        "branch": "session/phase3-pane-probes",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0066",
      "datetime": "2026-08-29T17:28:43Z",
      "session": null,
      "kind": "architecture",
      "skill": null,
      "title": "Force is reachable, and a fourth repository finds absence rendered as emptiness",
      "prompt": null,
      "summary": "Two escape hatches and a fourth repository.\n\n**Force is reachable.** It existed as an API parameter with nothing able to reach it. Ctrl+K, Shift+I\nre-reads every scope ignoring the cache, threaded through the wire contract additively — a client\nbuilt before the field still decodes and still means \"use the cache\", which is the safe reading of an\nabsent flag. The command announces that it is ignoring the cache, because the only reason to run it\nis that the user does not trust the fast answer and a command that looks identical gives them no way\nto tell which they got. IndexSummary reports reuse in words for the same reason: \"Indexed 0 of 7\"\nwith nothing else reads as a failure.\n\nAdding a command required one line in a Design-owned file, because the conformance test makes adding\na command and placing it in a menu one atomic change. Written down rather than done quietly: the\ncommand-to-menu mapping is now recorded as Core-owned DATA inside a Design-owned file, with a\nproposal to move it onto the catalog entry so the seam stops crossing there.\n\n**A fourth repository, chosen for what it lacks.** 63 Python files, 40 TypeScript, no C#, no Bicep,\nno migrations. It produced zero scopes, zero assertions and an EMPTY DISCLOSURE LIST —\nindistinguishable from an empty directory, with the mechanism whose entire job is to say what was not\nread saying nothing. Third repository in a row to find the same shape: a missing context map read as\nperfect coverage, a bounded search read as the whole workspace, unreadable source read as no source.\nEach time the arithmetic was right and the claim was false.\n\nUnanalysedLanguages names what is present and unread, with counts, because \"some Python\" and \"10,760\nPython files\" are different statements about how much of a repository the graph is silent on.\nVendored directories are excluded and a C#-only workspace discloses nothing, because a disclosure\nthat fires everywhere is noise.\n\nAnd the caps fix is now checkable on every run: 122 of 124 join edges agree between the pane read and\nthe store, the two missing are cut by the neighbour cap, and the shortfall line reports them.",
      "rationale": null,
      "artifacts": [
        "src/AiDe.Core/Extraction/UnanalysedLanguages.cs",
        "src/AiDe.Core/Workbench/WorkbenchCommands.cs",
        "docs/collaboration/session-contracts.md"
      ],
      "tags": [
        "phase-3"
      ],
      "git": {
        "before": null,
        "after": "dd1c53793935746bc3aa6c5a175c1cf5c99f9a8b",
        "branch": "session/phase3-pane-probes",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0067",
      "datetime": "2026-08-29T17:48:34Z",
      "session": null,
      "kind": "architecture",
      "skill": null,
      "title": "The panes read the store exactly: a paged bulk evidence query",
      "prompt": null,
      "summary": "The panes now see exactly what the store holds — 12,100 of 12,100 assertions, 124 of 124 join edges,\nzero divergence. They were rebuilding that set node by node through Describe, which bounds neighbours\nat 50 and lost two edges doing it, and which asks the store for a graph walk when what is wanted is a\ntable scan.\n\nEvidenceAsync is the question they were actually asking: one page of every current assertion, paged\nbecause it crosses a pipe and an unbounded response would breach the result-byte cap. The cursor is\nthe last row's (subject, predicate, object) — the same tuple the ORDER BY uses, so a page boundary\ncannot skip or repeat a row; an id-based cursor would order by something the query does not, which is\nhow paging quietly loses records. A malformed cursor restarts rather than throwing, because it is\nopaque and a caller was never meant to construct one.\n\nPinned with a page size of ONE, so every boundary in the set is exercised: the union of the pages\nequals the whole set, nothing repeated. A comfortable page size would test that the query runs, not\nthat the cursor is right.\n\nTwo more things landed with it. A departed scope's evidence is now retired by committing an EMPTY\nsnapshot at a higher generation — superseded, never deleted, because the store is append-only and\ndestroying the rows would destroy the record of what the graph once said. Removing a project used to\nleave its symbols and crossings in every projection indefinitely. And every catalog command now\ndeclares its menu, so the builder can derive its grouping and Core stops needing to edit a\ndesign-owned file to add a command at all.",
      "rationale": null,
      "artifacts": [
        "src/AiDe.Core/Projections/ProjectionService.cs",
        "src/AiDe.Core/Store/StoreReader.cs",
        "src/AiDe.App/Workbench/WorkbenchShell.cs"
      ],
      "tags": [
        "phase-3"
      ],
      "git": {
        "before": null,
        "after": "5af8dd94aeae26d187fd0b6b805453b04e60b06c",
        "branch": "session/phase3-pane-probes",
        "pushed": false,
        "commits": []
      }
    },
    {
      "id": "cl-0068",
      "datetime": "2026-08-29T17:48:34Z",
      "session": null,
      "kind": "knowledge",
      "skill": null,
      "title": "DC-025 absence rendered as success, and DC-026 a merge that de-duplicates on the disputed key",
      "prompt": null,
      "summary": "Two defect classes registered, one of them mine.\n\nDC-025, absence rendered as success. A projection computes over a set that is empty BECAUSE NOTHING\nWAS COLLECTED, the arithmetic is correct, and the surface renders that zero with the vocabulary of\ncompleteness. Four instances now, all found by pointing the panes at real repositories and none by a\ntest: a missing context map read as perfect coverage; a search bounded at fifty read as the whole\nworkspace; unreadable source read as no source; and now a file that does not parse. Roslyn does not\nthrow on broken source — it returns a tree with error nodes — so extraction succeeds and simply finds\nless, which is indistinguishable from a smaller file. That is the state a developer is in most often,\nand it disclosed nothing. Fixed with source-did-not-parse, which names the files and their count\nwhile still contributing what did parse.\n\nWorth recording honestly: the fourth instance was nearly reported from an experiment that had not\nrun. The script meant to corrupt a file silently did nothing, and the assertion-count difference I\nwas about to attribute to a syntax error had another cause entirely. It became a finding only after\nthe broken file was verified to exist.\n\nDC-026 is mine. Resolving an append-only merge conflict by unioning KEYED BY ID, with setdefault,\nsilently dropped a design-session entry — the gate stayed green throughout, because uniqueness was\nsatisfied precisely by the removal. The defect is not the collision; it is de-duplicating on the\nfield that is in dispute. tools/merge-append-only-log.py unions by content so nothing can be dropped,\nre-issues a contested id from the shared counter, and prints the count in, the count out, and every\nre-issue — a merge that resolves silently is indistinguishable from one that lost something.",
      "rationale": null,
      "artifacts": [
        "docs/lessons/defect-classes.md",
        "tools/merge-append-only-log.py"
      ],
      "tags": [
        "continuous-improvement"
      ],
      "git": {
        "before": null,
        "after": "5af8dd94aeae26d187fd0b6b805453b04e60b06c",
        "branch": "session/phase3-pane-probes",
        "pushed": false,
        "commits": []
      }
    },
    {
      "id": "cl-0069",
      "datetime": "2026-08-29T18:04:06Z",
      "session": null,
      "kind": "knowledge",
      "skill": null,
      "title": "A corpus of workspaces defined by what they lack, and a gate that notices a shrinking log",
      "prompt": null,
      "summary": "DC-025 has a control instead of a habit, and the gate that missed DC-026 now looks for the loss.\n\nLackingWorkspaceTests is a corpus of workspaces defined by what they LACK: empty, only-Python, source\nthat will not parse, no context map, a read that was bounded, and a scope whose extraction failed so\nthe graph shows an older revision. Every case asserts a SENTENCE, never a count, because the counts\nwere always right — that is what made the class survive four times. Fixtures always have the thing:\nthey are written by the person building the feature, so they contain a context map, compile, and are\nin the language the extractor reads. This corpus is the deliberate opposite. Its last case is the\ngeneralisation itself — a workspace missing something must never produce a result that is silent\nabout it — so adding a new kind of absence is how the next instance gets caught before a real\nrepository finds it.\n\nverify-audit-log.py now compares each log against HEAD and fails when an id present in the committed\nversion has disappeared. It only counted duplicates before, which is exactly why it stayed green\nwhile my merge removed an entry: uniqueness was satisfied PRECISELY BY the removal. Observed failing\non a log with its last entry deleted. The residual is honest — it sees losses against HEAD, so a loss\nintroduced and committed in one step is still invisible.\n\nAnd the paged evidence read is proven across the daemon, at a page size of one so the cursor is\nexercised at every boundary rather than the test proving only that a single response deserialises.\nThe last three cross-boundary defects were all \"right in process, wrong through the pipe\".",
      "rationale": null,
      "artifacts": [
        "tests/AiDe.Core.Tests/LackingWorkspaceTests.cs",
        "tools/verify-audit-log.py"
      ],
      "tags": [
        "continuous-improvement"
      ],
      "git": {
        "before": null,
        "after": "8ec320e3553fa7d9a11a9dd288c67ca5b02985e4",
        "branch": "session/phase3-pane-probes",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0070",
      "datetime": "2026-08-29T18:04:06Z",
      "session": null,
      "kind": "architecture",
      "skill": null,
      "title": "Stale scopes are stated, not retracted; and the read scales where extraction does not",
      "prompt": null,
      "summary": "A step changed on inspection, and a measurement taken rather than guessed.\n\nThe listed step was \"retract on scope failure, not just departure\". Opening the code first showed\nthat keeping the last good snapshot on failure is a DELIBERATE decision, recorded in\nRefreshScopeAsync: blanking the graph on a build error would be worse. Retracting would have\ncontradicted that rather than built on it. The real gap was that what renders is then OLD and nothing\nsaid so — a stale scope drew exactly like a current one, and only the incident sidecar knew. So the\nfailure now discloses stale-scope with the revision still being shown, and the snapshot keeps\nrendering.\n\nScale, measured on a SYNTHETIC workspace and labelled as such because nothing available here is much\nlarger than TheTerrace: 20 projects, 2,400 types, 21,066 assertions.\n\n  first index                13.5s  (about 0.68s per project of 120 types, roughly linear)\n  re-index, nothing changed   0.1s  (20 scopes reused)\n  paged read of everything   185ms  (11 pages, exact agreement with the store)\n  shortfall                  none   (no cap bit at 21,066 assertions)\n\nThe read is not the problem at this size; extraction is. 185ms to page 21,066 assertions against\n13.5s to produce them says the next scale work belongs in the extractor rather than the query path —\nand the fingerprint cache already means that 13.5s is paid once rather than per refresh. The honest\nlimit: this says nothing about deep inheritance, heavy generics or thousands of package references,\nbecause the generator produces none of those. It bounds the shape it tested and no more.",
      "rationale": null,
      "artifacts": [
        "src/AiDe.Core/WorkspaceCore.cs",
        "spikes/joins-on-a-real-repo/RESULT.md"
      ],
      "tags": [
        "phase-3"
      ],
      "git": {
        "before": null,
        "after": "8ec320e3553fa7d9a11a9dd288c67ca5b02985e4",
        "branch": "session/phase3-pane-probes",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0071",
      "datetime": "2026-08-29T18:24:27Z",
      "session": null,
      "kind": "decision",
      "skill": null,
      "title": "INV-0001: a 22,297-character PATH, and cmd.exe drops it",
      "prompt": null,
      "summary": "Reported: \"the agent sessions do not have my profile or my environment variables — ghcp and claude\nare both installed so they should both work.\"\n\nThe verified cause is not in this product. The machine's PATH is 22,297 characters and cmd.exe\nsilently drops a variable that large, so every .cmd shim — which is every npm-installed CLI — starts\nwith an EMPTY PATH and cannot find node, git or itself. claude works because claude.exe is a real\nexecutable with no cmd in the path; ghcp does not exist at all, and the Copilot CLI has an npm .cmd\nshim. That asymmetry is the entire symptom.\n\nNecessary and sufficient, both measured: at 22,297 characters a cmd child receives an empty PATH,\nreproduced twice including once with no part of AI-DE involved; trimmed to 1,799 characters the same\nchild receives the full PATH. AI-DE passes the environment correctly — PowerShell started from the\nsame inherited block reads all 22,297 characters and resolves claude.\n\nFive hypotheses were ruled out with evidence, including one flag removed and re-measured, and the\nbelief that the profile was at fault — which is where two turns of work went first, because the\nsymptom is indistinguishable from a launcher bug.\n\nThe cause of the 22,297 characters: roughly 190 entries of the shape Temp\\biohacker-nuget-<guid>\\\ndotnet-home\\.dotnet\\tools, appended to the PERSISTED user PATH by another project's build tooling and\nnever removed. Each is unique, so there is nothing to de-duplicate.\n\nWhat was ours is the silence. The terminal opened, looked healthy, and the tools were absent with\nnothing saying why. EnvironmentHealth.Inspect now states the size, the limit and the largest repeated\ngroup — because 200 unique paths is a number, not a lead — announced once per shell, never per pane.\nIt does not edit PATH: a tool that silently rewrites the environment to make itself work has hidden\nthe problem from the only person who can fix it.\n\nAgents are now hosted in the login shell rather than launched beside it, which delivers the profile\nhalf of the request and makes .ps1 shims resolve. It is recorded honestly as NOT fixing the reported\nsymptom — a .cmd shim invoked from the hosting shell still starts cmd, which still drops the PATH.\nMeasured, not assumed.\n\nRegistered as DC-027: the environment a parent hands a child is not the one the child receives.",
      "rationale": null,
      "artifacts": [
        "docs/investigations/INV-0001-agent-terminals-lack-the-users-environment.md",
        "src/AiDe.Core/Terminal/EnvironmentHealth.cs"
      ],
      "tags": [
        "investigation"
      ],
      "git": {
        "before": null,
        "after": "2731758925df4bb4b06ffae1f2186ad43cc82958",
        "branch": "session/phase3-pane-probes",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0072",
      "datetime": "2026-08-29T20:13:51Z",
      "session": null,
      "kind": "decision",
      "skill": null,
      "title": "The PATH is cleaned, the cmd limit is measured, and extraction is profiled",
      "prompt": null,
      "summary": "The reported symptom is fixed, at its actual cause.\n\nThe user PATH went from 21,528 characters and 206 entries to 1,150 and 19. The 187 removed entries\nwere all of the shape Temp\\biohacker-nuget-<guid>\\dotnet-home\\.dotnet\\tools, and every one of them\nwas ALREADY ABSENT FROM DISK — a directory that does not exist cannot be providing a tool, which is\nwhat made the removal safe rather than a judgement call. The previous value is backed up to\n%LOCALAPPDATA%\\AiDe\\user-path-backup-<timestamp>.txt. Verified after: a .cmd shim in a fresh process\nreceives the PATH, and both copilot and claude resolve through it.\n\nThe cmd limit is now measured rather than quoted. Bisected: cmd carries 8,151 characters and drops\n8,152, printing \"The input line is too long\" and losing the value. The documented figure is 8,191 and\nthe ~40-character difference is the variable's own name plus block overhead, which is why the message\nstill says \"may be dropped\" — not because the number is unmeasured, but because it shifts with the\nname.\n\nEnvironmentHealth now measures every variable, not only PATH: any oversized one is dropped by the\nsame mechanism, and checking only the variable that happened to bite is how a class gets found twice.\n\nAnd AvailableAgents no longer trusts a filter the environment invalidates. When the environment is\nunhealthy the PATH filter is skipped and every agent is offered, because a menu that silently omits\nan agent is invisible while a launch that fails is not — and the shell has already said why.",
      "rationale": null,
      "artifacts": [
        "src/AiDe.Core/Terminal/EnvironmentHealth.cs",
        "src/AiDe.App/Workbench/TerminalSurface.cs"
      ],
      "tags": [
        "investigation"
      ],
      "git": {
        "before": null,
        "after": "f5fb23aaa1ed763026de8a647956f7475824174c",
        "branch": "session/phase3-pane-probes",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0073",
      "datetime": "2026-08-29T20:13:51Z",
      "session": null,
      "kind": "architecture",
      "skill": null,
      "title": "98% of extraction is the read, not the walk",
      "prompt": null,
      "summary": "Extraction profiled, and the answer moves the next optimisation to the other half.\n\nThe previous measurement said \"the read is not the problem at this size; extraction is\" — true and\nuseless, because it did not say which part of extraction. AIDE_EXTRACTION_TIMING splits each scope\ninto the READ phase (parse the sources, build the compilation, resolve references) and the WALK phase\n(visit symbols, emit assertions):\n\n  read 505-616ms per project, walk 6-15ms once the JIT is warm\n\nRoughly 98% of the cost is the read. The first scope's 260ms walk is JIT rather than work, and a\ntotal without the split would have attributed it to the walk and sent the next optimisation at the\nwrong half.\n\nSo incremental work belongs in AVOIDING the read — which the fingerprint cache already does per\nscope and would do better per file. Making the symbol walk faster would recover about one percent.\n\nEmitted on the normal path as activity tags, with an env var for the console form: a feature is not\ndone until its behaviour is measurable without a re-run.",
      "rationale": null,
      "artifacts": [
        "src/AiDe.Core/Extraction/CSharpExtractor.cs",
        "spikes/joins-on-a-real-repo/RESULT.md"
      ],
      "tags": [
        "phase-3"
      ],
      "git": {
        "before": null,
        "after": "f5fb23aaa1ed763026de8a647956f7475824174c",
        "branch": "session/phase3-pane-probes",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0074",
      "datetime": "2026-08-29T20:31:04Z",
      "session": null,
      "kind": "architecture",
      "skill": null,
      "title": "File I/O is 97% of extraction, not parsing — and a timer that said otherwise",
      "prompt": null,
      "summary": "File-granularity incremental extraction, and a measurement of mine that was wrong twice before it was\nright.\n\nThe profile said the READ phase is 98% of extraction. Splitting it further produced \"parsing is 97%\nof the read, so cache the trees\" — plausible, confident, and wrong, because the timer wrapped\nFile.ReadAllText and ParseText together and reported the total as \"parse\". Timed apart on freshly\nwritten files: read 576-690ms, parse 4-5ms. Disk I/O is ~99% of the read, which makes file I/O\nroughly 97% of everything extraction does — the opposite half from the one the bundled timer pointed\nat.\n\nIt was caught only because a follow-up run produced a 40x speedup with ZERO cache hits, and no\ncorrect model explained that. Appended as an instance to DC-009: an instrument reports what it\nmeasured, so a timer around two operations must be named for both or split.\n\nSyntaxTreeCache was built on the wrong rationale and is right anyway, because a hit skips the whole\nfactory — the disk read as well as the parse. Keyed by path, length, modification time and the parse\noptions: hashing the bytes to decide whether to read the bytes is a cache that costs what it saves.\nForced re-index in the same process, with a distinct revision so the store's already-committed\nshort-circuit could not answer for it: 720 of 720 trees reused, 0ms read, 0ms parse, and the whole\nrun 1.0s instead of 1.0s+ at 0.6s. This is the case the scope fingerprint cannot cover — one file\nedited in a project of a hundred and twenty, where the scope must be re-read and 119 files did not\nmove.\n\nPATH regrowth is now caught by SHAPE rather than by size. The oversize check only fires once PATH is\npast cmd's limit, which is to say after the damage; 187 dead build directories accumulated before\nanything noticed. Ten or more entries pointing at directories that do not exist is now a finding,\nwith the largest group named — so regrowth is caught at twenty entries rather than a hundred and\neighty-seven. A handful of dead entries stays quiet, because this is looking for accumulation rather\nthan tidiness.\n\nWhat appends those entries was NOT found: no repository on this machine contains the string, so it\nwas not a checked-in script. The guard is the durable answer available from here.",
      "rationale": null,
      "artifacts": [
        "src/AiDe.Core/Extraction/SyntaxTreeCache.cs",
        "src/AiDe.Core/Terminal/EnvironmentHealth.cs"
      ],
      "tags": [
        "phase-3"
      ],
      "git": {
        "before": null,
        "after": "192fb3d05894f43aac6eb4e1cc920f572bf682dc",
        "branch": "session/phase3-pane-probes",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0075",
      "datetime": "2026-08-29T21:08:10Z",
      "session": null,
      "kind": "architecture",
      "skill": null,
      "title": "A second language extractor, and the paging tie it exposed",
      "prompt": null,
      "summary": "A second language, and a paging defect the second language exposed.\n\nPythonExtractor reads modules, top-level classes and functions, and import edges. Six repositories\nhad disclosed unread Python before it existed; the disclosure was right and it is not a substitute,\nbecause a graph that says \"there is Python here and I cannot see it\" is honest and still blind.\n\nIt reads STRUCTURE, not semantics, and says so on every scope: imports are not resolved, nested\ndeclarations are invisible, dynamic imports are not followed. Declarations are recognised at column\nzero only — an indented def is a method, and claiming it as a module-level function would put a\nsymbol in the graph that no importer can reach. Import edges are INFERRED, because the target is the\nmodule path as written and whether it resolves to anything is not established here; calling that\nVerified is precisely the defect DC-022 is about. A simplify: marker records the ceiling and the\nupgrade trigger.\n\nPython also came OFF the unanalysed-languages list on the same day, because a closed gap reported as\nopen is the same defect as an open one hidden.\n\nMeasured on the repository that started this: ai-forward produced ZERO assertions and an empty\ndisclosure list three turns ago. It now produces 2,158 assertions across 5 scopes in 0.2 seconds,\nwith its remaining gaps named.\n\nAnd that run exposed a real paging defect: 2,158 in the store, 2,157 through the paged read.\n(subject, predicate, object) is NOT unique — two scopes can assert the same triple — so a cursor over\nthat ordering silently loses exactly the rows that tie, when a page boundary lands on one. Scope is\npart of the ordering and the cursor now, and a test builds two identical Python packages to\nmanufacture the tie deliberately.\n\nThe I/O question was measured rather than assumed and is only half answered. Raw reads of 120 files\ncost 5ms outside this product and 73ms on first touch from a different process — a 15x first-access\npenalty consistent with on-access scanning. Inside the extractor the same volume costs ~500ms, which\nis seven times worse again and is NOT explained. Recorded as a gap rather than a claim; the tree\ncache removes the cost on re-index whichever explanation turns out to be right.",
      "rationale": null,
      "artifacts": [
        "src/AiDe.Core/Extraction/PythonExtractor.cs",
        "src/AiDe.Core/Store/StoreReader.cs"
      ],
      "tags": [
        "phase-3"
      ],
      "git": {
        "before": null,
        "after": "ccd9276972fdd91b6bb5df921979224e02ed476f",
        "branch": "session/phase3-pane-probes",
        "pushed": false,
        "commits": []
      }
    },
    {
      "id": "cl-0076",
      "datetime": "2026-08-29T21:45:33Z",
      "session": null,
      "kind": "knowledge",
      "skill": null,
      "title": "DC-028: the synthetic benchmark was measuring itself",
      "prompt": null,
      "summary": "The extraction profile is explained, and both earlier conclusions were artefacts of the benchmark.\n\nA fresh .NET process reading 120 newly-created files took 493ms (4.11ms/file); a SECOND fresh process\nover the same files took 6ms, and the repository's own source reads at 0.19ms/file. So the ~500ms\nwas a one-time, system-wide, per-file cost on the first read after creation — the signature of\non-access scanning — and it never recurs. It is not extraction's cost at all.\n\nRun against a real repository the profile inverts completely. TheTerrace, 463 files in its main\nscope: read 53ms, parse 694ms, walk 1,167ms. The walk is the LARGEST cost on real code and looked\nfree on the synthetic workload, because the generated types were trivial — no inheritance, no\ngenerics, four fields each.\n\nTwo independent flaws in one generator, each inverting a published conclusion. Registered as DC-028:\na synthetic benchmark measures the benchmark unless its workload resembles the real one in the\ndimensions that drive cost. The control is that the spike prints the same timings for named real\nrepositories, so the synthetic figure and the real one appear side by side.\n\nPython imports are resolved within their scope now. An import naming a module the scope contains\npoints at a file that exists and was read, so the edge is VERIFIED; anything else stays INFERRED with\nthe name as written, because it may be a package, a module in another scope, or nothing, and\nasserting which is the guess DC-022 is about. Relative imports resolve against the importing module's\npackage, one level per leading dot, and climbing above the root resolves to nothing rather than\nthrowing. On the real Python repository 82 of 330 imports now resolve, and the disclosure carries the\ncount of those that do not — a blanket \"imports are not resolved\" became a closed gap reported as\nopen the moment resolution landed.\n\nThe cursor audit found one more thing and cleared another. ResultBounds.NextCursor is ALWAYS null and\nno projection returning it pages — said out loud in the type, because a caller could loop on it and\nnever leave the first page with nothing failing. And DeriveClaimCurrent folds statuses with Max under\na comment saying the weakest wins: correct only because the enum is ordered strongest-first, so a\nreordering would silently invert it into manufacturing confidence. Pinned by a test.",
      "rationale": null,
      "artifacts": [
        "docs/lessons/defect-classes.md",
        "spikes/joins-on-a-real-repo/RESULT.md"
      ],
      "tags": [
        "continuous-improvement"
      ],
      "git": {
        "before": null,
        "after": "a286b981cb8b83aaaf594921e1de5cad65377cce",
        "branch": "session/phase3-pane-probes",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0077",
      "datetime": "2026-08-30T14:37:24Z",
      "session": null,
      "kind": "architecture",
      "skill": null,
      "title": "The graph surface had never shown a graph: two nodes of 2,813",
      "prompt": null,
      "summary": "The graph surface had never shown a graph.\n\nReported by the user, comparing TheTerrace in this tool against the same repository in Obsidian: two\nnodes versus a full graph. The cause was not extraction — the store held 12,100 assertions across\n2,164 nodes — it was that the canvas called FindAsync with a limit of ONE to pick a root and then\ndrew that root's neighbours. A workspace of two thousand nodes rendered as the alphabetically first\nsymbol and its single neighbour, correctly, from the day it was written. Four unit tests described\nthat behaviour approvingly.\n\nGraphProjection answers the question the surface exists for: every node and edge, bounded by a cap\nthat is reported rather than silent. Attributes fold onto nodes instead of being drawn as edges — a\nhas_type arrow would put the string \"class\" in the graph as a thing other things point at — using the\nsame single definition the search already applies. LoadAsync with no root now means the whole graph;\na root means drill-down. Proven across the daemon as well, because every cross-boundary defect so far\nhas been right in process and wrong through the pipe.\n\nA second finding sat inside the first. With the graph finally visible, its six most-connected nodes\nwere string, int, Task<TResult>, DateTimeOffset, IReadOnlyList<T> and Guid — 773 edges to string\nalone. A graph whose centre is the BCL is not a picture of anybody's domain, and a cap ordered by raw\ndegree drops the user's own types to keep framework primitives. Nodes carry IsExternal now (nothing\nin the workspace declares them) and declared nodes are kept first. The same repository's centre\nbecame AppDbContext, Fixture, SportMonksProvider, ScheduledRefresh.\n\nRegistered as DC-031: a surface asks a narrower question than the one it exists to answer. The\ngeneralisation is one question — what is the cardinality of what this shows, against the cardinality\nof what it fetched — and the related tell is a test suite that describes the slice fluently.\n\nTheTerrace now: 28 scopes, 13,530 assertions, 2,813 nodes and 8,602 edges with nothing omitted.",
      "rationale": null,
      "artifacts": [
        "src/AiDe.Core/Projections/GraphProjection.cs",
        "src/AiDe.Core/Presentation/CanvasGraphViewModel.cs"
      ],
      "tags": [
        "phase-3"
      ],
      "git": {
        "before": null,
        "after": "fb43ecd5fb72deffe4471525ef8eac483487b9a7",
        "branch": "session/phase3-pane-probes",
        "pushed": false,
        "commits": []
      }
    },
    {
      "id": "cl-0078",
      "datetime": "2026-08-30T14:37:24Z",
      "session": null,
      "kind": "architecture",
      "skill": null,
      "title": "TypeScript read, and the symbol walk profiled",
      "prompt": null,
      "summary": "TypeScript is read, and the symbol walk is profiled.\n\nTypeScriptExtractor closes the largest remaining disclosure — typescript-not-analysed (165 files) on\na repository whose C# half was fully mapped. Same bargain as Python: structure, not semantics.\nExported top-level class, interface, type, enum, function and const; import and re-export\nspecifiers; nothing inside a function or a namespace block. Declaration files (.d.ts) are skipped\nbecause they re-state types defined elsewhere and indexing them would put every symbol in the graph\ntwice, once with nothing behind it.\n\nSpecifiers resolve only when RELATIVE and only to a file this scope contains — extensions optional,\na directory meaning its index file. A bare specifier is a package or a path alias and resolving it\nneeds configuration this extractor deliberately does not read, so it stays Inferred with the\nspecifier as written. TypeScript and JavaScript left the unanalysed-languages list the same day.\n\nIt is deliberately NOT a shared base with PythonExtractor. They look alike and are not the same:\nTypeScript's specifiers carry extensions and index files, its declarations are export-gated rather\nthan column-zero, and JSX changes what a valid line looks like. A base parameterised by all of that\nis more machinery than either extractor contains; a third language that fits the pattern is when the\nabstraction is earned.\n\nIndexing TheTerrace with it immediately raised a real store error: two files can share a module name\n— app.ts beside a compiled app.js — so the same triple was asserted twice in one scope and the\nnatural key rejected it, correctly. Identical facts are one fact, so both extractors deduplicate\nbefore returning. Silencing the key would have weakened a control that is doing its job.\n\nAnd the symbol walk, which the real-repository profile named as the largest cost, is now split:\nenumerating the namespace tree is 135ms for 1,026 types, and reading each type's members is 1,074ms\n— about 89% of the walk and half the whole scope. The next hypothesis worth testing is\nToDisplayString, called per symbol, but that is a hypothesis and is written down as one rather than\nacted on.",
      "rationale": null,
      "artifacts": [
        "src/AiDe.Core/Extraction/TypeScriptExtractor.cs",
        "src/AiDe.Core/Extraction/CSharpExtractor.cs"
      ],
      "tags": [
        "phase-3"
      ],
      "git": {
        "before": null,
        "after": "fb43ecd5fb72deffe4471525ef8eac483487b9a7",
        "branch": "session/phase3-pane-probes",
        "pushed": false,
        "commits": []
      }
    },
    {
      "id": "cl-0079",
      "datetime": "2026-08-30T15:22:12Z",
      "session": null,
      "kind": "architecture",
      "skill": null,
      "title": "The join reader recognised one spelling of EF, and module ids collided",
      "prompt": null,
      "summary": "The hypothesis that `ToDisplayString` dominated the symbol walk was **wrong**, and measuring it found\na much larger defect than the one it was looking for.\n\nSplitting the walk by operation: display 46ms across 7,312 calls — **3.9%**. The real costs were\ndepends-on 613ms (51%) and attributes 403ms (34%). The attribute cost was one call:\n`FluentTableMappings` ran `DescendantNodes()` over all 1,026 type declarations, hunting an EF\n`ToTable` call. Hoisting it to the compilation with a source-text prefilter cut it to 1ms of\nprefilter across 465 files and 217ms of walking the 66 that survived.\n\nThen the 66 files were the finding. Most were EF migration `*.Designer.cs` snapshots — and the three\nthat were not revealed that the reader **did not work**. It matched `Entity<T>()...ToTable(\"x\")` as a\nsingle expression. TheTerrace, like most EF codebases, writes:\n\n    var terrace = modelBuilder.Entity<Terrace>();\n    terrace.ToTable(\"Terrace\", \"setup\");\n\nSo the extractor recovered **1 declared mapping and guessed 123**, on a repository that states every\none of them outright. Every one of those 123 inferred `maps_to` edges was a name-matching guess\nstanding where a declaration already existed. The same reader also emitted the entity name as\nwritten in source (`Order`) where every other assertion uses the display string (`Shop.Order`) — so\neven its successes were edges whose subject matched no node.\n\nResolved semantically now: ask the model for the RECEIVER's type. One rule answers every style —\nchained, local-variable, lambda-configuration, `IEntityTypeConfiguration<T>` — because in all of them\nthe receiver is an `EntityTypeBuilder<TEntity>`, and the symbol's display string is the name the rest\nof the extractor emits. **Verified joins 1 → 64, inferred 123 → 73, `declares_table` facts 63.**\n\nGenerated files are skipped, on correctness grounds before performance ones: EF writes a model\nsnapshot per migration, each calling `ToTable` for every entity *as it stood then*, so reading them\nasserts a table renamed three migrations ago as current fact wearing the same Verified badge. 63 of\nthe 66 files were these. `fluent-scan` fell 1,418ms → 60ms warm as a side effect, not as the goal.\nThe skip is disclosed with a count, because skipped is not absent.\n\nDC-032 registered: **a reader recognises one spelling of a pattern and reports the rest as absent.**\nThe signature is a ratio nobody looks at — precise hits against fallback hits on real input — and it\nsurvives because the fallback is doing its job, so the surface looks populated and coverage stays\ngreen. The control was observed failing with the skip disabled.\n\n**Module ids were colliding, certainly rather than theoretically.** Both module-shaped extractors\nnamed a module by its path relative to its OWN SCOPE, and a scope is one directory. Every Python\npackage has an `__init__.py`, so a repository with five packages produced five scopes each declaring\na module called `__init__` — one node in the graph carrying the merged edges of five unrelated files.\nThe same for `index.ts`, `main`, `setup`, `conftest`. Ids are now the repository-relative path:\nunique by construction, readable, and the string a person would type to open the file.\n\nThat made cross-scope resolution possible, which was the actual next step. An import naming a sibling\npackage resolves to a file in a different scope, so it could never resolve from inside the one doing\nthe importing. `ExtractionRequest` now carries the workspace's module set, computed once per revision\nfrom the FILESYSTEM — not from the store, because resolving against what has already been extracted\nwould make an edge depend on the order the scopes happened to run in, the trap the Python extractor\nalready avoids one level down. Null means \"not supplied\", which is not \"there is nothing there\": the\nedge stays Inferred and the disclosure fires.\n\nA test comment had encoded the defective rule approvingly — *\"modules are named relative to it:\n`models`, not `src/models`\"* — which is DC-031's tell in its own words.\n\n**The graph surface is now queryable, not just fetchable.** `GraphQuery` filters by kind, by scope,\nand by whether to include nodes nothing declares. The filter runs BEFORE the cap and degree is\ncounted over what survives it: filtering afterwards would rank and trim the whole graph and only then\ndiscard, so a caller asking for the domain model would receive the wrong 5,000 nodes trimmed to the\nright kind, with nothing in the result saying so. Proven across the daemon as well as in process,\nbecause every cross-boundary defect here so far has been right in process and wrong through the pipe.\n\n**A next step I listed last turn was wrong and is withdrawn.** \"The index still walks every scope\" —\nit does not. `ScopeFingerprints` already skips an unchanged scope and counts it as *reused* rather\nthan *indexed*, deliberately, so \"28 of 28 indexed\" cannot be a true sentence about a run that read\nnothing. I asserted its absence without opening the file. The residual — incrementality BELOW the\nscope, so a one-file edit does not re-walk 1,026 types — is real, but it conflicts with the\nappend-only per-scope snapshot model and is a design decision, not a tidy-up.\n\n717 tests green (App 121, Core 596). Six gates clean. Zero design-owned files.",
      "rationale": null,
      "artifacts": [
        "src/AiDe.Core/Extraction/CSharpExtractor.cs",
        "src/AiDe.Core/Extraction/ModuleNaming.cs",
        "src/AiDe.Core/Projections/GraphProjection.cs"
      ],
      "tags": [
        "phase-3"
      ],
      "git": {
        "before": null,
        "after": "acce3698269f5a36ad113b845875f5f19a051cac",
        "branch": "session/phase3-pane-probes",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0080",
      "datetime": "2026-08-30T15:47:03Z",
      "session": null,
      "kind": "architecture",
      "skill": null,
      "title": "Two more perf hypotheses died; routes, generated types, and a gate for every id allocator",
      "prompt": null,
      "summary": "Two more performance hypotheses died, and the second death is the useful one.\n\nSplitting `depends_on`: **`SymbolEqualityComparer` dedupe is 3ms of 600ms — 0.5%** across 13,906 raw\nsymbols. Gather is 597ms. So the cost of `depends_on` IS the semantic work — `GetMembers()` binding\nevery member signature — and it cannot be made cheaper, only avoided. Two turns, two suspects\n(`ToDisplayString` at 3.9%, then the comparer at 0.5%), both wrong, both named before being believed.\nThe measurement is now fine-grained enough that the next guess has nowhere to hide.\n\n**Generated types are excluded from the whole extractor**, not just the fluent scan — a type is\ndropped only when EVERY file declaring it is auto-generated, so a partial class with a hand-written\nhalf (a WPF window, an EF migration people edit) is kept. Disclosed with a count.\n\n**And the estimate that motivated it was wrong.** I predicted ~600 excluded types on TheTerrace. The\nreal number is **1**. EF marks only the snapshot half (`*.Designer.cs`) as generated; the migration\nhalf starts with a plain `using System;`, so each migration type has a hand-written declaration and\nis correctly kept. The rule is right and its effect here is small — recorded that way rather than\nquietly dropped, because \"I expected 600 and got 1\" is the part worth knowing.\n\n**Routes are answerable now.** `GraphPaths` returns how one node reaches another: shortest routes\nonly, directed (walking an edge backwards would answer \"these are related\" while looking like \"a\nchange here reaches there\"), with the route's status taken from its WEAKEST edge so one inferred link\ncannot launder itself into a verified chain. A missing endpoint returns a reason, not an empty list\nthat reads as \"unconnected\". Proven across the daemon pipe as well as in process — a path is the one\nresult shaped as a list of lists, so it is the one most likely to arrive flattened.\n\n**The id-allocator gap is closed, and closing it found two more allocators.**\n`tools/verify-id-allocators.py` asks the generalising question behind DC-013 — *what else here is\nnumbered by reading the highest and adding one?* — as a check rather than a note. It guards five\nfamilies in one place (adding one is a line, not a script) and reports any UNDECLARED sequence, so\nthe next allocator is guarded on the day it is invented. Observed failing on both shapes before it\nwas believed.\n\nOn its first run it found `adr-` (16 decisions, allocated by FILENAME — a kind the first draft could\nnot even express) and, on inspection, `INV-`, which was below the threshold at two entries and was\ndeclared anyway.\n\nTwo first-draft errors are recorded in DC-013 because they are the interesting part. It reported\neighteen holes in the audit log as failures — but a hole is the merge protocol working: a contested\nid is resolved by re-issuing the loser, leaving the number unused. Verified with `git log -S`: none\nof the missing ids has ever existed, so nothing was lost. **A control that flags the fix as the\ndefect is how a control teaches people to ignore it**, so contiguity is opt-in per family now. It\nalso first read ADR ids out of `architecture.md`, which only cites them — an allocator is where an id\nis CREATED, never where it is mentioned.\n\n**Electing a single allocator between sessions was considered and rejected** (the user asked). The\nsessions work in separate worktrees on purpose and an election needs a rendezvous they do not have:\na session an hour into its work has not fetched, so \"ask the allocator\" is stale or a blocking round\ntrip through `main`, and it would make one session wait on another to record a lesson. The class is\nnot \"the wrong allocator won\", it is \"a shared sequence with two writers\".\n\n**Sub-scope incrementality is written up, not built.** `docs/notes/note-20260830-sub-scope-\nincrementality.md` states four options with what each costs, recommends one (keep the atomic\ngeneration, skip the walk for unchanged types) and explicitly does not take it: the per-scope and\nper-parse layers already exist, and nobody has measured whether edit-to-graph latency is a 1.2s\non-demand cost or a per-keystroke one. Instrumenting that is cheaper than any of the four options and\nshould come first. The decision changes the store's contract, so it is the user's call.\n\nA build gap worth naming: the spike project is not in `AiDe.sln`, so last turn's solution build never\ncompiled it and the first measurement this turn silently ran a **stale binary**. Caught because the\nnew timing fields did not appear in the output.\n\n730 tests green (App 123, Core 607). Six gates clean, one of them new. Zero design-owned files.",
      "rationale": null,
      "artifacts": [
        "src/AiDe.Core/Projections/GraphPaths.cs",
        "tools/verify-id-allocators.py",
        "docs/notes/note-20260830-sub-scope-incrementality.md"
      ],
      "tags": [
        "phase-3"
      ],
      "git": {
        "before": null,
        "after": "2f80b0b780a0e37a246006b072b851e98e2c33b2",
        "branch": "session/phase3-pane-probes",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0081",
      "datetime": "2026-08-30T16:05:29Z",
      "session": null,
      "kind": "architecture",
      "skill": null,
      "title": "Refresh cost measured, no project uncompiled, DC-033 swept, routes on a surface",
      "prompt": null,
      "summary": "**The measurement the sub-scope decision was blocked on now exists.** A refresh had a span but its\nSTATUS carried no duration, so the only thing a caller could read was how many assertions came back.\n`ScopeRefreshStatus` now carries `QueuedMilliseconds` and `DurationMilliseconds` — kept separate\nbecause waiting is a concurrency problem and running is a cost problem, and one number hides which\none a user is feeling. `RefreshMetrics` adds p50/p95/max over a bounded window plus first/last\nobserved, exposed over IPC as `refresh.metrics` with no request body and no failure mode: an operator\nasking what re-indexing costs should never be told it depends on what they enable first.\n\nDeliberately **no rate is computed**. \"Refreshes per hour\" from two samples is a number with no error\nbar that gets quoted as if it had one; the raw facts let a reader compute it when there is enough of\nit to mean something and notice when there is not. Failed refreshes are timed too — a run that takes\ntwenty seconds and then throws is the one an operator most wants to see, and excluding failures is\nhow a percentile ends up describing only the easy cases. With nothing measured everything reports\nzero and null rather than an interpolation of nothing.\n\n**No tracked project escapes compilation now.** `spikes/joins-on-a-real-repo` — the harness behind\nnearly every performance and join number in this log — is not in `AiDe.sln`, so a change to\n`GraphAsync` broke it while the solution build stayed green, and the next measurement silently ran a\nSTALE BINARY. Caught only because an expected timing field did not appear. Had the change not added a\nfield, the numbers would have looked fine and been wrong. That is DC-023 with a twist: the gate was\nnot stale, it was NARROW.\n\n`tools/verify-project-coverage.py` builds every tracked project the solution does not contain. A\ngate rather than solution membership, because `AiDe.sln` answers \"what ships\" and these are evidence\nartifacts. **The cost claim in its own header was wrong and is corrected in place:** it was written\nexpecting to belong in the slow at-readiness ring and measures **16 seconds for 19 projects**, which\nputs it on every push. It prints its wall time on every run so that stays a measured decision.\nObserved failing against a deliberately broken spike.\n\n**DC-033 swept, and it found a second instance.** The class says its signature is a ratio on real\ninput, so the sweep measured. `TypeScriptExtractor` knew `class|interface|type|enum|function|const`\nand did not know `async`, the generator star, `namespace`, `let` or `var`. TheTerrace declares 124\n`export interface`, 26 `export type`, 16 `export const` and **4 `export namespace`** — four\ndeclarations reported as absent rather than unread. `PythonExtractor` is clean; the Bicep and schema\nreaders are narrow by *declared* ceiling and disclose it.\n\nThe important half is not the wider pattern, which will be wrong again for the next spelling. **The\nreader now counts its own misses and discloses them** — `typescript-exports-not-recognised (N)` — so\nthe next unanticipated form announces itself on the scope. Re-exports are excluded deliberately:\ncounting them gives a miss rate that never reaches zero and therefore says nothing.\n\n**Routes reach a surface.** `CanvasGraphViewModel.RouteAsync(from, to)` returns the **same\n`CanvasGraph`** the canvas already binds, because a route IS a subgraph and giving it its own shape\nwould mean a second renderer and a second place for the two sessions to disagree about what a node\nlooks like. Endpoints arrive with `IsRoot = true`; the caption names the count, the shortest length\nand **the weakest link**, because a route drawn without that looks like a fact about the code when\none inferred edge makes the whole claim inferred. Every empty case says which one it is, and the\nprojection's own reason survives to the caption — \"not in this graph\" and \"no route within 8 edges\"\nsend a user to different places.\n\n**The `GraphQuery` filter question is put to the design session as a decision, not a request.** All\nthree filters are on the wire and proven across the daemon; which of them belong in the UI, and as\nwhat control, is an information-architecture call this session does not own. §4a states the three\nwith what each is worth, recommends a **preset** over toggles (three named views are one decision a\nuser makes once; three toggles are a combinatorial space they must reason about) and says plainly\nthat this is a guess about users. What Core commits to either way is the tested property: the filter\nruns before the cap and degree is computed over what survives it.\n\n765 tests green (App 131, Core 634). Seven gates clean, one of them new.\nZero design-owned files.",
      "rationale": null,
      "artifacts": [
        "src/AiDe.Core/Ipc/ScopeRefreshService.cs",
        "tools/verify-project-coverage.py",
        "src/AiDe.Core/Presentation/CanvasGraphViewModel.cs"
      ],
      "tags": [
        "phase-3"
      ],
      "git": {
        "before": null,
        "after": "1885098af60677e9d066a5f234d55baa9719bc9b",
        "branch": "session/phase3-pane-probes",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0082",
      "datetime": "2026-08-30T16:18:29Z",
      "session": null,
      "kind": "architecture",
      "skill": null,
      "title": "INV-0003: the graph could not fit through the pipe, and the default should never have asked",
      "prompt": null,
      "summary": "The graph pane has been broken on TheTerrace since the whole-graph change, and it is my defect. The\ndesign session investigated it (INV-0003), verified the cause and handed it to Core.\n\nMEASURED: the whole-graph response is 1,522,284 bytes against a 1,048,576-byte IPC frame. It could\nnever have been delivered. `IpcFraming.WriteAsync` threw, the serve loop caught `IOException` and\n`OperationCanceledException` but not that, the exception escaped, the connection closed with no\nreply — so \"the answer is too big to send\" reached the user as \"the daemon closed the connection\nwithout responding\", which sends them to look at the daemon.\n\nTwo fixes, both Core's:\n\n**The transport no longer closes silently.** `IpcServer.Respond` checks the encoded size BEFORE\nwriting and returns a new `ipc.payload_too_large` carrying the actual and permitted byte counts.\nChecked before rather than caught after on purpose: a partially written frame leaves the peer reading\na length prefix whose body never arrives, which is a hang rather than an error. The writer's own throw\nis correct and stays.\n\n**The default view is bounded by construction.** No focus now asks for 1,500 declared nodes ranked by\ndegree rather than the whole graph — 533,495 bytes, fits, 618 omitted and named in the caption.\n\nThe transport failure exposed the larger error, and the design session is right about it: the fix for\nDC-031 over-corrected. The graph pane rendering two nodes of two thousand was a real defect; loading\nEVERYTHING overshot the spec that was being restored — `knowledge-exploration.md` US-K2 already said\nthe whole graph is never rendered at once. The answer to \"one arbitrary alphabetical node\" was a\nbounded overview of MEANINGFUL nodes. DC-031 and DC-035 are the same axis overshot in opposite\ndirections, and a fix for one lands on the other unless the spec is re-read when the new default is\nchosen. That is recorded on DC-035 as mine.\n\nBounding by size and bounding by meaning turned out to be the same change: measured, the six\nmost-connected nodes of a real repository were `string`, `int`, `Task<T>`, `DateTimeOffset`,\n`IReadOnlyList<T>` and `Guid`, so a first view centred on the BCL was unreadable as well as\nundeliverable.\n\nDC-035 moves from `uncontrolled` to `partially-controlled`. Core's half is landed and tested,\nincluding the byte measurements pinned as an assertion so that raising the frame cap without\nrevisiting the default fails there first. **Design's half is open and named:** the aggregated /\nlevel-of-detail overview for a graph too large to show node-by-node, and the \"narrow your focus\"\nstate. Until that exists a repository whose declared code exceeds the cap gets a truthful truncation\nrather than a designed overview.\n\n775 tests green (App 131, Core 639). Seven gates clean. Zero design-owned files.",
      "rationale": null,
      "artifacts": [
        "src/AiDe.Core/Ipc/IpcServer.cs",
        "src/AiDe.Core/Presentation/CanvasGraphViewModel.cs"
      ],
      "tags": [
        "phase-3"
      ],
      "git": {
        "before": null,
        "after": "bbeeb9f832efa1a06e83c1e9df15b0a9bc5cac32",
        "branch": "session/phase3-pane-probes",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0083",
      "datetime": "2026-08-30T16:39:59Z",
      "session": null,
      "kind": "architecture",
      "skill": null,
      "title": "Every response is bounded by bytes, and the incrementality question answered by a grep",
      "prompt": null,
      "summary": "The graph was found by a user opening a repository, not by us, so every other read operation was\nmeasured at its own ceiling against the 1 MiB frame. **Two more were one repository away from\nINV-0003, and a third could never have succeeded.**\n\n| operation | before | after |\n|---|---|---|\n| evidence (2,000/page) | 1,004,397 B — **95.8% of the frame** | 659,164 B, 1,310 rows, cursor continues |\n| graph (5,000-node ceiling) | 1,522,915 B — **overflows** | 474,437 B, shrunk to fit, `Omitted` reports |\n| find (20,000 ceiling) | 461,750 B returned while **reporting a 64 KiB cap** | byte-bounded, `ByteCapped` set |\n\nThe class behind all three: **every ceiling in the read surface counts ITEMS and the transport limit\nis in BYTES**, and every item's size comes from repository content. A count-only cap admits an\nunbounded payload. `Evidence`'s own documentation claimed a page \"can cross a pipe without breaching\nthe result-byte cap\" — it was fifteen times over that cap. `Find` declared `MaxBytes: 65,536` beside\n461,750 bytes of results, a control that could not fire (DC-016).\n\nTruncating an evidence page is **lossless**, which is why the bound belongs in the projection: the\ncursor continues from the last row actually returned, so it costs a round trip and never a row. A\ntest pages a corpus to exhaustion and asserts every assertion comes back exactly once. Every response\nkeeps at least one item — a caller that receives nothing because its first row is enormous can never\nmake progress.\n\nThe byte guard uses a **measured** constant, not a guessed one: 2,000 assertions whose fields total\n238,002 bytes serialise to 1,004,397, so JSON scaffolding is **383 bytes per row**. Rounded up,\nbecause a guard that under-counts is a guard that lets the frame overflow.\n\n**The sub-scope-incrementality decision is resolved, and not by the instrument built for it.** The\nnote said the answer depended on how often edit-to-graph happens, and `RefreshMetrics` was built to\nmeasure that. One grep answered it instead: **there is no `FileSystemWatcher` anywhere in `src/`**.\nIndexing runs only from explicit commands, so the cost is paid deliberately by a user pressing a\nbutton. Breaking the per-scope snapshot's atomicity to shorten a deliberate 1.2s action is a poor\ntrade, so none of options A–D is built. The note stays, status `resolved`, naming the trigger that\nwould change it: **re-index on save**, which converts a bounded cost into a per-edit one.\n\nWorth recording because it nearly went the other way. Three hypotheses in this area have now been\nwrong — `ToDisplayString` (3.9%), `SymbolEqualityComparer` (0.5%), \"the index still walks every\nscope\" (it does not). This would have been the fourth: collecting a distribution and reasoning\ncarefully about a number that answered a question nobody was asking.\n\n**My own new gate cried wolf and is fixed.** `verify-id-allocators` reported an undeclared family\ncalled `nDC-` — it was matching the `n` of a JSON-escaped newline before a *mentioned* `DC-` id in a\nlog summary. A mention is not an allocation, which is the same distinction the first draft got wrong\nwhen it read ADR ids out of `architecture.md`. Fixed with a lookbehind, and the narrative files are\nexcluded from the candidate scan: a record of a mention must never nominate an allocator. This is the\nsecond time this script has flagged correct behaviour as a defect; a control that does that twice is\none people switch off.\n\n790 tests green (App 131, Core 645). Seven gates clean. Zero design-owned files.",
      "rationale": null,
      "artifacts": [
        "src/AiDe.Core/Projections/ProjectionService.cs",
        "docs/notes/note-20260830-sub-scope-incrementality.md"
      ],
      "tags": [
        "phase-3"
      ],
      "git": {
        "before": null,
        "after": "1c273b0b29bf76e7fc453e5f77f733ec512f19e8",
        "branch": "session/phase3-pane-probes",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0084",
      "datetime": "2026-08-30T17:14:13Z",
      "session": null,
      "kind": "architecture",
      "skill": null,
      "title": "The workspace as groups: the aggregated overview, and a reflective frame-fit gate",
      "prompt": null,
      "summary": "**The aggregated overview closes Core's half of DC-035.** The bounded default fixed the transport\nfailure by drawing 1,500 of 2,118 declared nodes and saying so — honest, and still a truncation. A\nuser opening a repository wants its SHAPE, and 1,500 dots is not a shape.\n\n`GraphOverview` returns the workspace as GROUPS, grouped by the ids' own hierarchy — a C# symbol is\n`TheTerrace.Features.Competitions.Season`, a module is `src/app/models`, so the first `Depth`\nsegments name what a developer would call \"where that lives\", and Depth is the zoom control.\n\nMEASURED on TheTerrace at depth 3: `Features.Fixtures` 117, `Features.Teams` 117, `Features.Matches`\n107, `Infrastructure.Data` 70, `Features.Identity` 47, `Features.Competitions` 44 — the actual shape\nof that repository, in **55,758 bytes** against 533,484 for the node graph.\n\n**No community-detection algorithm, deliberately.** Its output is unstable under small changes to the\ngraph, so the same repository would regroup between two indexes and the picture would move for\nreasons the user cannot see. Prefix grouping is stable by construction.\n\nEach group carries `NodeCount` — a dot standing for 240 types is only honest while the 240 is on it,\nwhich is the whole difference between an overview and a smaller lie. Each link carries `Weight` for\nthickness and the **weakest** status of the edges it bundles, because drawing a bundle as Verified\nbecause most of its members were would launder guesses into facts at a grain where the user can no\nlonger see the members. Edges inside a group are counted, not drawn.\n\n**My own first default was the failure the overview exists to prevent.** `MaxClusters` shipped at 200,\nand TheTerrace has 689 groups at depth 3 — returning 200 of them is a hairball at a coarser grain,\nwhich my own doc comment named as the thing to avoid. Measured and lowered to 60.\n\n**The frame-fit control is reflective, because hand-auditing found the last three and would not find\nthe next.** `EveryOperationFitsTheFrameTests` derives the operation list from `IWorkspaceQueries`\nitself and fails when a method is added with no size check — observed failing with an entry removed,\nnaming the method. Writing the list by hand would restate the product's own list (DC-021) and go\nstale in exactly the case that matters.\n\n**The write side was audited and needs nothing.** An `IndexSummary` for 28 scopes is **1,724 bytes** —\nthree orders of magnitude below the frame. Recorded rather than guarded: a control whose failure\nnobody can name is one to drop.\n\n**Degree on the wire was already done, by the other session.** They shipped force-directed layout with\ndegree-sized dots (DC-036), so that next step was stale when I wrote it. What layout still could not\ndo is cluster, which is what the overview provides. `GraphOverview.GroupFor` is public now so the\ncanvas groups detail nodes the SAME way the overview does — two definitions of one grouping is\nDC-022's shape, and the divergence would render a node in the wrong cluster and look like a layout\nbug.\n\n**A shared test double replaces four hand-written stubs.** Every method added to `IWorkspaceQueries`\nhas broken the same four stubs across two projects — four rounds of identical churn, which is a\nmeasured recurrence rather than a predicted one. `FakeWorkspaceQueries` throws rather than returning\nempty: a stub that quietly answers a question the test did not intend to ask turns a wrong call site\ninto a passing test.\n\n791 tests green (App 132, Core 659). Seven gates clean. Zero design-owned files.",
      "rationale": null,
      "artifacts": [
        "src/AiDe.Core/Projections/GraphOverview.cs",
        "tests/AiDe.Core.Tests/EveryOperationFitsTheFrameTests.cs"
      ],
      "tags": [
        "phase-3"
      ],
      "git": {
        "before": null,
        "after": "13a488b5028bc352171f202df353915e0f791ac2",
        "branch": "session/phase3-pane-probes",
        "pushed": false,
        "commits": []
      }
    },
    {
      "id": "cl-0085",
      "datetime": "2026-08-30T17:31:16Z",
      "session": null,
      "kind": "architecture",
      "skill": null,
      "title": "Drill-down closes the overview loop, and a third repository found a defect in my own control",
      "prompt": null,
      "summary": "**Drill-down closes the loop the overview opened.** `GraphQuery.GroupId` returns the nodes inside one\ncluster, and the depth is read back out of the group id rather than passed alongside it — a separate\ndepth parameter would let a caller ask for `TheTerrace.Features` at depth 3 and receive nothing, with\nno error and no way to tell that from an empty group.\n\nThe property that makes the overview trustworthy is asserted, not assumed: **every cluster's\n`NodeCount` equals what drilling into it returns**, and every cluster's `InternalEdges` equals what\ndrilling in draws. For every group, not a sampled one — the interesting failure is the group whose\nnaming rule differs, and that is never the group a test author picks by hand. Both paths compute\nmembership from one definition (`GraphOverview.GroupFor`), which is why the round trip holds.\n\n**Proven across the pipe.** The overview is nested — clusters and weighted links — so it is the\nresponse most likely to arrive with an inner list flattened or a count defaulted to zero, and a\ncluster's count is a claim a user can check. The daemon test now drills from the largest cluster back\nto its nodes and asserts the counts agree.\n\n**Three repositories now, not one — and the third one found a defect in my own control.** Every\nmeasurement in this project had come from TheTerrace. Running BioHacker (32 scopes, no `DbContext` at\nall, so 0 joins is correct rather than a failure) and meridian-finance-planner (31 scopes, 9,165\nassertions, 165 TypeScript files) confirmed every operation fits the frame on all three.\n\nBioHacker reported `typescript-exports-not-recognised (1)` — the DC-033 control firing on the first\nnew repository it saw, which is what it is for. But the miss was **mine**: `export default\ndefineConfig({…})` and `export default test;` declare nothing new, and the miss-counter's own doc\ncomment said `export default someExpression` was excluded while the pattern never excluded it.\n`export default` is ubiquitous, so the disclosure would have fired on nearly every real TypeScript\ncodebase and become noise. Fixed; the 165-file repository now reports zero.\n\n**The shape is worth naming because it is the third instance this session.** The exclusion was written\nin the comment before it was implemented — exactly like the evidence page documenting a byte cap it\ndid not apply, and `find` reporting a `MaxBytes` it never enforced. **A claim in prose that the code\ndoes not make true.** When a comment states a bound, the next question is which line applies it.\n\nAnd the meta-lesson, recorded on DC-033: **a control's false-positive rate is only observable on input\nit was not written against.** Validating on one repository validates against one repository.\n\n**The shared test double now covers both projects.** `FakeWorkspaceQueries` moved to `tests/Shared`\nunder a neutral namespace and is linked into both test csproj files — linked source rather than a new\nassembly, because a project for forty lines is more machinery than the churn it removes. The two\nApp-project stubs that hand-implemented the whole seam now override only what they answer.\n\n800 tests green (App 132, Core 668). Seven gates clean. Zero design-owned files.",
      "rationale": null,
      "artifacts": [
        "src/AiDe.Core/Projections/GraphProjection.cs",
        "src/AiDe.Core/Extraction/TypeScriptExtractor.cs",
        "tests/Shared/FakeWorkspaceQueries.cs"
      ],
      "tags": [
        "phase-3"
      ],
      "git": {
        "before": null,
        "after": "431cb97ebb26f9a9bab19d2f484a833dc78cdd9a",
        "branch": "session/phase3-pane-probes",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0086",
      "datetime": "2026-08-30T19:18:26Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "kind": "design",
      "skill": "specify",
      "title": "Knowledge exploration becomes a full-window Explorer mode (dual-pane graph+reader), not a dock pane",
      "prompt": "reconsider the graph view as a full-window dual-pane surface entered from a rail icon",
      "summary": "Specified a distinct 'primary view mode' (Workbench | Explorer): a rail icon slides the exploration open body-wide with graph+search | reader, reusing the existing reader (US-K3) and node-walk (US-K4). Additive and toggleable; docked graph pane can remain during rollout.",
      "rationale": "On a single monitor the docked graph pane competes with reading a node's contents; a dedicated full-window dual-pane mode is the requested remedy and matches the VS Code/JetBrains/Obsidian idiom.",
      "artifacts": [
        "docs/specs/knowledge-explorer-mode.md"
      ],
      "tags": [],
      "git": {
        "before": "d347415",
        "after": "d3474150954e972f971ac0e3013edb5c9fa5ef1b",
        "branch": "feature/app-facelift-and-graph-surfaces",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0087",
      "datetime": "2026-08-30T19:21:30Z",
      "session": null,
      "kind": "architecture",
      "skill": null,
      "title": "A comment that states a bound the code does not apply, and raw SQL schemas read",
      "prompt": null,
      "summary": "**The sweep found the class was live in the tree, and the class now has a control.**\n\nThree instances this session were the same shape — a comment states a bound the code does not apply.\nThe sweep found a fourth still standing: `MaxEvidencePageCeiling` documented as \"sized so a page\nstays comfortably inside `MaxResultBytes`\" when 2,000 assertions serialise to **1,004,397 bytes**,\nfifteen times that constant. Corrected to name the mechanism that actually bounds it.\n\n`tools/verify-bounds-are-enforced.py` requires every constant whose NAME claims a limit to appear in\na comparison, a clamp or a take — searched over code with comments stripped, so prose cannot count as\nproof. On its first run it found `WholeGraphNodeCap`: declared last turn as a \"retained name\",\ndocumented as a ceiling, **zero usages**. Deleted. One false positive (`OverviewNodeCap`, applied\nindirectly through `GraphQuery.MaxNodes`) became a justified exemption rather than a weaker rule,\nbecause \"it is passed somewhere\" is exactly what made `find` look safe.\n\n**The control says what it cannot do.** It checks a bound is APPLIED, not that a sentence describing\nit is TRUE — the TypeScript instance, a regex differing from its comment, is invisible to it. Half\nthe class is mechanised; claiming otherwise would be this class applied to its own control.\n\n**Measurement is repeatable now.** `tools/measure-repositories.py` runs the harness over three\ncodebases chosen for contrast and prints them side by side. It exists because every number in this\nproject came from one repository until a second was tried by hand, and the third exposed a defect in\na control that looked correct against the first.\n\n**The overview's default depth was the useless one, and three repositories showed it:**\n\n    TheTerrace                d1:  74 groups,  1 link    d2: 200,  6    d3: 200, 263\n    BioHacker                 d1:  17 groups,  0 links   d2:  56,  6    d3: 200, 323\n    meridian-finance-planner  d1:  92 groups,  6 links   d2: 159, 18    d3: 200,  93\n\nDepths 1 and 2 are almost linkless in ALL THREE, for an arithmetic reason: at a coarse grain nearly\nevery edge is internal to a group, counted and not drawn. A picture of disconnected islands is\ncorrect and says nothing about structure. Default moved 2 → 3, from evidence rather than taste.\n\n**Raw SQL schemas are read.** BioHacker declares its whole schema in one 197-line file with eight\n`CREATE TABLE`s; the tool said `sql-not-analysed (2 file(s))` and produced **zero** joins — honest,\nand blind to that repository's entire schema side. `SqlSchemaExtractor` emits the SAME node shape as\nthe EF reader (`table:Name`, `has_type table`, `has_column`), because a second spelling would be\nDC-022 with two producers of one predicate and the joins would silently see half the tables.\n\nColumn parsing matches parentheses rather than scanning to the first `)`, so `DECIMAL(9,2)` does not\ntruncate the column list; constraint lines are not claimed as columns; the schema qualifier is\nstripped so `dbo.Principal` and `Principal` are one node.\n\nBioHacker: joins **0 → 8**, 54 `has_column` facts. meridian: 30 → 33 inferred, 31 → 34 scopes.\n\nAnd `.sql` came out of `UnanalysedLanguages`. A closed gap reported as open is the same defect as\nhiding one that is not — **the third time that list has needed this correction, once per extractor\nadded**, so the rule is now written where the list is: it is the LAST STEP of adding an extractor.\n\nDC-038 registered, with its residual risk stated: name-based detection misses a bound named without\none of those suffixes, and a limit written as a magic number is invisible to it.\n\n812 tests green (App 132, Core 680). Eight gates clean, one of them new. Zero design-owned files.",
      "rationale": null,
      "artifacts": [
        "tools/verify-bounds-are-enforced.py",
        "src/AiDe.Core/Extraction/SqlSchemaExtractor.cs",
        "tools/measure-repositories.py"
      ],
      "tags": [
        "phase-3"
      ],
      "git": {
        "before": null,
        "after": "4314a5efc2057dbca92d34e36b598cc74600931b",
        "branch": "session/phase3-pane-probes",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0088",
      "datetime": "2026-08-30T19:34:24Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "kind": "architecture",
      "skill": "define-architecture",
      "title": "Full-window surfaces are a primary view mode (body-content swap); the Explorer reader fetches node content via a bounded Core query",
      "prompt": "/define-architecture the full-window Explorer mode",
      "summary": "ADR-0017 (primary view mode via body-content swap, retain-not-rebuild) + ADR-0018 (on-demand bounded NodeContentAsync reader contract). Additive to the workbench/docking model; amends ADR-0013 (per-mode persistence) and ADR-0015 (graph<->reader keyboard cycle).",
      "rationale": "A full-window graph+reader must not compete as a dock pane nor be a dismiss-only modal; a body-content swap keyed by a shell view-mode is the smallest correct mechanism and generalises to future full-window surfaces. Content on the graph payload would overflow the IPC frame, so the reader fetches per-node on demand.",
      "artifacts": [
        "docs/adr/0017-primary-view-mode.md",
        "docs/adr/0018-node-content-reader-contract.md",
        "docs/architecture.md"
      ],
      "tags": [],
      "git": {
        "before": "ceb2389",
        "after": "ceb2389e829e25c991f5e0a916f68c7863796399",
        "branch": "feature/app-facelift-and-graph-surfaces",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0089",
      "datetime": "2026-08-30T19:49:35Z",
      "session": null,
      "kind": "architecture",
      "skill": null,
      "title": "SQL folded, usage not mapping, and three gates that CI had never run",
      "prompt": null,
      "summary": "**SQL scripts are folded, not just read.** MEASURED: one repository carries **125 `ALTER TABLE … ADD`**\nstatements, so reading `CREATE` alone showed its schema as it stood at the first migration and called\nthat current. Adds, `DROP COLUMN` and `DROP TABLE` are now applied in file order — drops especially,\nbecause a column that no longer exists is a **wrong** fact rather than a missing one. Renames are\ncounted and disclosed rather than guessed: every dialect spells them differently, and guessing\nproduces a confidently wrong column name.\n\n**Three gates I wrote this session were not in CI.** `verify-id-allocators`,\n`verify-project-coverage` and `verify-bounds-are-enforced` sat in `tools/` for several commits with\nno workflow line — they ran only when I remembered. A gate nobody invokes is the \"lesson recorded as\nprose\" failure wearing an executable's clothes: it looks like a control in every review and fires\nnever. All three wired, and `verify-project-coverage` now asks the same question about gates that it\nasks about projects — **what exists to be run, and is not run.** Observed failing with a gate removed\nfrom the workflow.\n\n**The `simplify:` marker audit found one triggered and one premise already false.** `IpcFraming`'s\ntrigger fired during INV-0003 — and the answer was **none of the two exits the marker listed**.\nNeither a bigger frame nor a data lane: the operation did not legitimately need to carry more. A\n2,815-node hairball was never a useful answer and the spec had always said so, so every response was\nbounded below the cap instead. Recorded, because a marker naming two exits invites you to take one.\n\nIts stated premise — *\"a control lane carries envelopes, not payloads: the largest legitimate message\nis a command with a small JSON body\"* — was **already false when audited**: ordinary responses are an\nevidence page at 659,164 bytes, a graph at 475,223, an overview at 345,507. That sentence had not\nbeen re-read since it was true. The other twelve markers were reviewed with no evidence of firing.\n\n**\"Verified joins for non-EF repositories\" turned out to be the wrong question, and the finding is\nbetter than the feature.** BioHacker has zero `DbContext` files, zero `[Table]` attributes and 191\nSQL literals naming tables from inside store classes. There is **no declaration of a code→schema\nmapping to verify** — and there should not be one invented. A store class issuing four statements\nagainst three tables is not *mapped* to any of them.\n\nWhat the source does declare is **usage**, so that is what is emitted: `uses_table`, Verified because\nthe literal is in the type. **62 edges on BioHacker**, structure it simply did not have. Deliberately\nnot `maps_to` — reusing the mapping predicate would launder usage into a mapping exactly where a\nreader trusts it (DC-022's shape). Joins there remain 0 verified / 8 inferred, which is the honest\nanswer.\n\n**Deduplication moved into one place on its third copy.** Python and TypeScript each grew the same\nsix lines after the same raw `UNIQUE constraint failed` from mid-index; `uses_table` hit it a third\ntime immediately, because one store names the same table in four statements. `ExtractionFacts.Distinct`\nnow owns it, so the fourth extractor inherits it. The store's key stays strict — silencing it would\ntrade a loud correct failure for a quiet wrong graph.\n\n**And \"make the measurement a CI nightly\" was wrong, stated rather than quietly dropped.** The\nrepositories are sibling checkouts on one machine; a hosted runner has none of them, so the job would\nfail or skip every night — a gate that cannot fire, dressed as diligence. `--record` is the\nachievable half: readings append to `docs/measurements/repositories.jsonl`, so drift shows up in\n`git diff` rather than in somebody's memory. First reading committed.\n\n831 tests green (App 132, Core 699). Eight gates clean, all eight now actually run by CI. Zero\ndesign-owned files.",
      "rationale": null,
      "artifacts": [
        "src/AiDe.Core/Extraction/SqlSchemaExtractor.cs",
        "src/AiDe.Core/Extraction/CSharpExtractor.cs",
        ".github/workflows/build.yml"
      ],
      "tags": [
        "phase-3"
      ],
      "git": {
        "before": null,
        "after": "fd8516ff7fafe459cebb0ce2f55e68de32bc1881",
        "branch": "session/phase3-pane-probes",
        "pushed": false,
        "commits": []
      }
    },
    {
      "id": "cl-0090",
      "datetime": "2026-08-30T20:46:13Z",
      "session": null,
      "kind": "architecture",
      "skill": null,
      "title": "A matcher that invents facts is worse than one that misses them",
      "prompt": null,
      "summary": "**I shipped a defect last turn and this turn's sweep caught it.** The `uses_table` reader matched a\nSQL keyword followed by a word ANYWHERE in a string literal, so *\"we update the record\"* produced an\nedge to a table called `the`. MEASURED: 63 prose strings in TheTerrace, and its `uses_table` count\nfell from **150 to 56** once the reader required a statement SHAPE.\n\nUnder-matching hides real facts; over-matching invents them — and the invented ones are worse,\nbecause they arrive labelled **Verified**. Same root cause as DC-033 in the opposite direction: the\nmatcher's fit to real input had never been measured. **A matcher is not finished until you know both\nwhat it misses and what it invents.**\n\n**The naive fix broke the real case, which is exactly why both directions must be measured\ntogether.** Requiring each literal to begin with a verb found **nothing at all** on the repository\nthat motivated the feature — real code splits SQL across concatenated literals, and the fragment\nholding `FROM dbo.AssessmentJob` begins with `FROM`. The reader folds the `+` chain and reads it as\none statement; a chain containing anything non-literal is skipped whole rather than half-read.\n\nA smaller lesson: the regex form of that shape test silently returned false for\n`\"INSERT INTO dbo.AssessmentJob (…)\"` — a string plainly beginning with one of its own alternatives —\nand cost more to diagnose than the check was worth. It is explicit code now.\n\n**`uses_table` reaches the Joins pane.** A distinct kind, never folded into `maps_to`: a store class\nissuing four statements against three tables is not mapped to any of them, and counting usage as\nmapping would make the verified/inferred ratio the pane exists to show meaningless. BioHacker's pane\ngoes from **0 verified / 8 inferred to 57 / 8** — a repository that showed nothing now shows what its\ncode actually touches.\n\n**Step 2 needed no work and step 3 was answered by measurement.** The EF reader ALREADY folds\n`CreateTable`, `AddColumn`, `DropColumn`, `DropTable`, `RenameTable` and raw `Sql` — I assumed it\nmight not, without checking, which is the habit this session keeps catching. Measured against real\nusage: the operations it does not handle (`CreateIndex` 113, `AddForeignKey` 19, `AlterColumn`) do\nnot change which tables or columns EXIST. One genuine gap found and closed: `RenameColumn` was\nunhandled with no default case, so a renamed column silently kept its old name — a wrong fact. EF\nstates renames in named arguments, so unlike raw SQL it is simply readable.\n\n**Python and TypeScript do not embed SQL, so nothing was built for them.** The counts looked\npromising — 186, 61, 1,162 \"SQL literals\" — and sampling showed *\"update the\"*, *\"select more than\none option from this\"*, and **zero** files containing `insert into`. Building `uses_table` there\nwould have emitted edges from English prose. Evidence for NOT building something is worth as much as\nevidence for building it.\n\n**The measurement log earns its keep at two readings.** Verified joins across the three repositories\nmoved **64 → 120**, **0 → 57**, **35 → 50**, with assertion counts slightly DOWN as prose noise left\nthe graph. One reading is a point; two is a direction, and the drift is now in `git diff` rather than\nin my memory of last turn's numbers.\n\n840 tests green (App 137, Core 703). Eight gates clean. Zero design-owned files.",
      "rationale": null,
      "artifacts": [
        "src/AiDe.Core/Extraction/CSharpExtractor.cs",
        "src/AiDe.Core/Projections/JoinProjection.cs",
        "docs/measurements/repositories.jsonl"
      ],
      "tags": [
        "phase-3"
      ],
      "git": {
        "before": null,
        "after": "87f04b04886d1fb8a5a0c239c253565e765e81b7",
        "branch": "session/phase3-pane-probes",
        "pushed": false,
        "commits": []
      }
    },
    {
      "id": "cl-0091",
      "datetime": "2026-08-30T21:03:59Z",
      "session": null,
      "kind": "architecture",
      "skill": null,
      "title": "Every extractor was inventing facts out of comments, and now a control says so",
      "prompt": null,
      "summary": "**The invent-direction of DC-033 now has a control, and it found four more defects on its first\nrun.** `ExtractorsDoNotInventTests` feeds every reader a corpus with no declarations and plenty of\ntext SHAPED like declarations, and asserts it produces nothing but disclosures:\n\n- the **SQL** reader read `-- CREATE TABLE Ghost` and `/* CREATE TABLE Historical */` as tables;\n- the **TypeScript** reader read `export class Removed {}` out of a block comment;\n- the **Python** reader read a class out of a **docstring** — the one place its column-zero rule\n  cannot tell documentation from declaration;\n- the **C#** reader turned *\"delete from your account to remove it\"* into `table:your`, because that\n  sentence genuinely begins with a SQL verb and last turn's shape test could not reject it.\n\n**Commented-out code is the worst possible input for a line-oriented reader**, and every repository\nis full of it — it is real syntax, because it *was* code. `SourceText` blanks comments before any\nreader believes a line, keeping newlines so provenance line numbers stay true. The C# case needed a\nsecond rule: a real table reference **ends where a clause can begin** — a keyword, punctuation, or\nthe end of the statement. In prose the next token is just another word.\n\n**Two things the fixes got wrong first, kept because they are the lesson.** Blanking string contents\nfor SQL deleted `\"main\".\"Thing\"`: in SQL a double quote is a quoted IDENTIFIER, not a string, so the\nreader lost the very names it exists to find — caught by a test in seconds, which is the cheapest\npossible way to learn that two languages disagree about a quote character. And `PRINT 'about to\ncreate table X'` names no table while `EXEC('CREATE TABLE …')` does; the reader can tell neither from\nthe other, so it reads neither and discloses the count.\n\n**Step 1 was answered by measurement and needed no change.** The Bicep and context readers were swept\non a repository they were not written against: Bicep's matchers are line-anchored on `resource`,\n`module` and `param`, and their values came back as real parameter names (`sqlServerName`,\n`identityName`) and real Azure types — nothing invented. The context map's `*` is a documented suffix\nwildcard and the real patterns end at a `.` boundary. Both pinned by the new control so they stay\nthat way.\n\n**The third reading shows the CORRECTION, which is what a third reading is for.** Verified joins:\n\n    TheTerrace                64 -> 120 -> 95\n    BioHacker                  0 ->  57 -> 55\n    meridian-finance-planner  35 ->  50 -> 46\n\nThe middle number was inflated by prose; the last is the honest one. **A single reading would have\nrecorded 120 as progress**, and two readings would have recorded it as a trend.\n\n**On `AlterColumn` — the decision, not an assumption.** It is not folded, and should not be. The\ngraph's `has_column` records that a column EXISTS; `AlterColumn` changes a type or nullability, which\nthat fact does not carry, so folding it would change nothing observable. Recording types is a\ndifferent feature — it would need a fact shape that can express them, both schema readers agreeing on\nit, and a consumer that wants it. None of those exists, so this stays where it is: named in the\nextractor's `simplify:` ceiling as the upgrade trigger, rather than half-built.\n\n848 tests green (App 139, Core 709). Eight gates clean. Zero design-owned files.",
      "rationale": null,
      "artifacts": [
        "src/AiDe.Core/Extraction/SourceText.cs",
        "tests/AiDe.Core.Tests/ExtractorsDoNotInventTests.cs",
        "docs/measurements/repositories.jsonl"
      ],
      "tags": [
        "phase-3"
      ],
      "git": {
        "before": null,
        "after": "66bfcf62cc39fad546ab14fcfe32f6bffc3e7b4d",
        "branch": "session/phase3-pane-probes",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0095",
      "datetime": "2026-08-30T22:00:00Z",
      "session": null,
      "kind": "architecture",
      "skill": null,
      "title": "Knowledge was zero because nothing ever looked",
      "prompt": null,
      "summary": "**The knowledge graph was never being populated, and the reason is worse than a bug.** The reader had\nexisted since Phase 1, with tests, inside the fixture extractor. `CompositeExtractor` had a route for\nit. And `CSharpScopeDiscovery` produced six scope kinds — `csharp`, `bicep`, `schema`, `python`,\n`typescript`, `sql` — and **no knowledge scope at all**. The capability was correct, tested, and\nunreachable on every real repository for the life of the project.\n\nBoth halves passed their own tests. The producer is proven by unit tests that hand it input; the\nrouter is proven by fixtures that name the scope explicitly. Only DISCOVERY was untested against\nreality, and its gap is invisible from either side.\n\n**A zero that means \"nobody looked\" reads as \"there is none\"** — which is the shape this product\nexists to avoid, in the product's own headline surface, on a repository whose premise is that *docs\nhold intent, code holds reality, and the expensive defects live in the gap*. Half of that sentence\nwas never being read.\n\nMEASURED after wiring discovery, on this repository: **466 `owned_by`, 346 `refines`, 287\n`implements`, 272 `relates-to`, 66 `depends-on`**, centred on `knowledge-hub` with 78 edges. Scopes\nacross the three measured repositories: **28→66, 34→48, 34→56**. Every response still fits the frame.\n\n**Running it over real documents immediately found two defects in the new reader** — which is the\nlesson from last turn applied one turn later:\n\n- link lines carry a trailing YAML comment, and trimming from the END left it attached: the graph\n  gained a relation literally called `implements }   # typed edges — registry in …`. Parsing now\n  stops at the closing brace.\n- **templates** carry frontmatter in exactly the shape a real document does, with `<artifact-id>`\n  where the id goes — so they became nodes describing the shape of a document, linked to things that\n  do not exist. Excluded by filename and, independently, by rejecting angle-bracketed placeholders.\n\nDC-041 registered: **a capability is complete, tested, and nothing ever routes work to it.** The\nsignature is a count that is exactly zero on every real repository while a sibling count is large,\nand the question that finds it is *\"what produces the keys this router matches on, and does it\nproduce this one?\"*. The control compares the two lists in a test instead of in somebody's head:\n`EveryRouteHasAProducerAndEveryProducerHasARoute` fails if discovery emits a kind nothing routes, or\nif a route exists that nothing discovers.\n\n**The other next steps, carried:**\n\n- **Bicep is comment-stripped too.** It PASSED the invent control — line-anchored matchers, and a\n  sweep of an unfamiliar repository produced only real parameter names and real Azure types. Stripped\n  anyway: it was the last line-oriented reader still parsing raw text, and all three readers caught\n  inventing were caught reading commented-out code.\n- **Provenance line numbers are asserted, not assumed.** Comments are blanked rather than deleted\n  precisely so a claim can still be opened at the right line; a test now pins `5:1` for a table\n  declared after a multi-line comment. It was the reason for the design and had never been checked.\n- **The invent control now runs against real repository text**, which is how both knowledge defects\n  surfaced — synthetic noise is written by the same person as the reader (DC-028's shape).\n- **`GraphQuery` filters: ANSWERED by the user — presets, three named views.** Recorded in the\n  contract with the three query shapes (Domain / Everything / This project). `Kinds` is deliberately\n  not one of them: it is a refinement *within* a view, and folding it in would rebuild the\n  combinatorial space presets exist to avoid.\n\n859 tests green (App 139, Core 720). Eight gates clean. Zero design-owned files.",
      "rationale": null,
      "artifacts": [
        "src/AiDe.Core/Extraction/KnowledgeExtractor.cs",
        "src/AiDe.Core/Extraction/CSharpScopeDiscovery.cs",
        "docs/collaboration/session-contracts.md"
      ],
      "tags": [
        "phase-3"
      ],
      "git": {
        "before": null,
        "after": "609b4e0d4892a919866f2e8e3eee7e2d4d425e95",
        "branch": "session/phase3-pane-probes",
        "pushed": false,
        "commits": []
      }
    },
    {
      "id": "cl-0097",
      "datetime": "2026-08-30T22:26:13Z",
      "session": null,
      "kind": "architecture",
      "skill": null,
      "title": "INV-0004: everything was knowledge because everything has a type",
      "prompt": null,
      "summary": "**INV-0004's root cause was a Phase-1 assumption that six extractors later made false.** Node\nclassification read:\n\n    var isKnowledge = result.Assertions.Any(a => a.Subject == nodeId && a.Predicate == \"has_type\");\n\n*Anything with a type is knowledge* — true when the fixture reader was the only producer of\n`has_type`, and false the moment a second extractor emitted it. By now every C# class, table, bicep\nresource and python module carried one, so almost the entire graph was classified `knowledge`, and it\nsurfaced exactly as the design session saw it: a bicep resource reading **\"kind: knowledge\"**. DC-022\nprecisely — a predicate gained producers and a consumer kept its assumption about who emits it.\n\n**Fixed by having the PRODUCER declare it.** Scope-id prefixes were the first attempt and were nearly\nright, but the fixture reader emits knowledge from a scope not named for it, so the id could not be\nthe authority either. A fact can be: both knowledge-reading paths now emit `node_class = knowledge`,\nand the core classifies from that. Nothing infers a node's half of the graph from a type name again.\n\n**Neighbours carry their real kind.** `DescribeResult` gained `NeighborKinds`; the canvas hardcoded\n`\"source\"` for every neighbour because the projection did not carry one, so a drill-down showed a\ntable, a bicep resource and a class as the same thing and the filter could not tell them apart. A\nrenderer inventing a default is a renderer stating a fact it does not have.\n\n**`UpsertNode` is a no-op when nothing changed.** It is a Type-2 dimension and every call closed the\ncurrent row and opened a new one, so re-indexing rewrote the history of every unchanged node. History\nwhose every row is an artefact of re-running the indexer cannot answer the question it exists for.\n\n**The knowledge pane was returning code.** It filtered on `has_type` alone — the same stale\nassumption, one projection along — so now that knowledge is finally indexed it would have shown C#\nclasses beside ADRs. It filters on `node_class` now, which is the dimension that knows.\n\n**`review-by` is read and overdue reviews are a health finding.** 460 review dates on this repository.\n`owned_by` and `review_by` joined the attribute set: a person and a date are not things to navigate\nto, and drawing them would have put \"@someone\" and \"2027-02-28\" in the graph as peers of the\ndocuments that carry them.\n\n**The one \"document with no id\" was a false positive of mine.** `INSTALL.md` opens with `doc:`,\n`bundle_version:` and `changes:` — a pack manifest that happens to use YAML, never a graph node. The\ncomplaint now fires only when the frontmatter is GRAPH frontmatter (a type, owner, links or review\ndate) and still has no id. Same mistake as reading prose into a schema, one field along.\n\n**The code↔knowledge join is NOT buildable today, and that is the finding.** Measured: **no knowledge\nlink in this repository targets a code symbol** — every `to:` names another document id. There is no\ndeclared relationship to read, and inferring one from titles or namespaces is exactly the guessing\nthis codebase refuses. What would unblock it is a change to how docs are WRITTEN, not to the reader:\na link such as `- { to: TheTerrace.Features.Fixtures, rel: governs }`. Recorded rather than invented.\n\n**DC-042's residual is now checkable.** `ConsumersReadWhatProducersEmitTests` reads the join\nprojection's predicate list FROM ITS SOURCE and asserts every name is one some extractor actually\nemits — so a renamed predicate fails a test instead of quietly returning fewer edges. Its first run\nreported `is_secret` and `resource_name_expression` as orphaned; they are emitted on real\nrepositories, and the gap was in the fixture I had written. A control validated against input its\nauthor wrote is a control that measures the author — so the fixture now exercises the whole bicep\nvocabulary.\n\n867 tests green (App 144, Core 723). Eight gates clean. Zero design-owned files.",
      "rationale": null,
      "artifacts": [
        "src/AiDe.Core/WorkspaceCore.cs",
        "src/AiDe.Core/Projections/ProjectionService.cs",
        "tests/AiDe.Core.Tests/ConsumersReadWhatProducersEmitTests.cs"
      ],
      "tags": [
        "phase-3"
      ],
      "git": {
        "before": null,
        "after": "df001100c4408da481fddf804225e7f01f8148a9",
        "branch": "session/phase3-pane-probes",
        "pushed": false,
        "commits": []
      }
    },
    {
      "id": "cl-0099",
      "datetime": "2026-08-30T23:21:49Z",
      "session": null,
      "kind": "architecture",
      "skill": null,
      "title": "Knowledge read 0 because the store was a generation behind, and the chip matched type names",
      "prompt": null,
      "summary": "**The Knowledge chip read 0 on a repository holding 2,343 knowledge nodes. Two causes, both Core's.**\n\n**The store was cached from a build that had no knowledge reader.** `ScopeFingerprints` already\ncarried an `ExtractorGeneration` for exactly this — a constant in every fingerprint so that upgrading\nthe product invalidates the sidecar — and nobody bumped it. So the knowledge extractor, `node_class`,\ncomment stripping in four readers, the SQL fold and `uses_table` all shipped while every existing\nworkspace kept serving results produced by the previous generation. Bumped to `2026-08-30.1`; an\nupgraded app now rebuilds rather than reusing.\n\n**And the graph carried only each node's FINE kind.** MEASURED: TheTerrace's knowledge types are\n`spec` and **`knowledge-epl-fan-platform`** — a name that repository invented. A chip matching a\nfixed list of type names cannot work across repositories, and widening the list only moves the\nproblem to the next one (DC-033). `GraphNode.IsKnowledge` carries the DECLARED coarse dimension, so a\nfilter asks the question instead of recognising spellings.\n\nVerified end to end: TheTerrace now yields **2,343 `node_class`, 639 `owned_by`, 428 `refines`, 114\n`spec`** — 24,058 assertions across 66 scopes, every response still inside the frame.\n\n**The code↔knowledge join is closed by decision, not deferred.** The user's rule: *\"It's ok if docs\nand code are not linkable and orthogonal, they will tend to be orthogonal which is why pruning the\ngraph on one or the other is a meaningful cut. Do not infer — the graph should only be on observable\nlinks/relationships.\"*\n\nRecorded as a decision note, because it is a standing principle rather than one answer: **an edge is\na claim that something in the repository says so.** Orthogonality is information — because the two\nhalves are separate, \"show the knowledge\" and \"show the code\" are exact cuts rather than blurred\nones. Inference would have made membership depend on how good the guess was that day, and this\ncodebase has paid for that twice already (`depends_on` produced 7,426 false Verified edges;\n`uses_table` turned *\"we update the record\"* into a table called `the`). What would legitimately\nunblock a join is a link written down — `- { to: TheTerrace.Features.Fixtures, rel: governs }` — which\nthe frontmatter reader would index today with no code change.\n\n**Python dynamic and nested declarations: measured, and NOT built.** Dynamic imports are unobservable\nstatically, so reading them would be inference — ruled out by the principle above. Nested\ndeclarations are observable but MEASURED at 28/31/21 against 356/457/150 top-level across three\nrepositories (~7%), and they are implementation details inside functions with no type information to\nbuild edges from: the graph would gain volume, not structure. The existing disclosures already state\nexactly this. INV-0004's fourth handoff closes as a priority call answered.\n\n**DC-013 recurred a sixth time and the gate caught it.** Both sessions allocated `DC-042`; mine was\npublished first, so theirs is re-issued as `DC-043` per the contract. Their entry also arrived with an\nunbackticked `Status:` value for the second time — and the register gate reported it as *\"declares no\nStatus line\"* when the line was there and the FORMAT was wrong. That message misdirected me twice, so\nit now names the actual problem and shows the expected form. **A control that misnames its own finding\ncosts the reader the time the control saved.**\n\n869 tests green (App 144, Core 725). Eight gates clean. Zero design-owned files.",
      "rationale": null,
      "artifacts": [
        "src/AiDe.Core/Extraction/ScopeFingerprints.cs",
        "src/AiDe.Core/Projections/GraphProjection.cs",
        "docs/notes/note-20260830-the-graph-carries-only-observable-links.md"
      ],
      "tags": [
        "phase-3"
      ],
      "git": {
        "before": null,
        "after": "693ce79705bd30345b23a2c93b0750de89324492",
        "branch": "session/phase3-pane-probes",
        "pushed": false,
        "commits": []
      }
    },
    {
      "id": "cl-0101",
      "datetime": "2026-08-30T23:40:28Z",
      "session": null,
      "kind": "architecture",
      "skill": null,
      "title": "The knowledge pane capped before it filtered, and CI never ran on the branch that broke it",
      "prompt": null,
      "summary": "**The knowledge pane was STILL returning zero, and the reason was a second, independent defect.**\nChasing the render rather than the data found it: `Knowledge()` read the first 200 `has_type`\nassertions and filtered THOSE to knowledge — so on any real repository the 200 were code types in\nalphabetical order and the filter left nothing. MEASURED: **0 items on a workspace holding 468\nknowledge nodes**; now 50, the ceiling.\n\nThat is DC-035's shape one projection along — **a cap applied before a filter returns the wrong\nslice trimmed to the right shape, and nothing in the result says so.** I fixed exactly this in\n`GraphProjection` and did not look for siblings. Pinned by a test whose fixture puts 400 code types\nalphabetically ahead of the knowledge, which is the condition that hid it.\n\n**The generation bump is now a control instead of a memory.** `ExtractorGeneration` existed and was\ncomplete; using it was a thing somebody had to remember, and a full day of extractor changes shipped\nwithout it. `verify-extractor-generation.py` fails when anything under `src/AiDe.Core/Extraction/`\nchanged since the generation last did. Deliberately conservative — a comment-only edit trips it, and\nthe remedy is a one-line bump costing one re-index, where deciding which edits \"really\" change output\nis a judgement nobody can make reliably about a compiler-driven extractor. Observed failing by\nreplaying the real baseline: it lists the exact extractors that shipped unbumped.\n\nIt uses `git log -G`, not `-S`. `-S` counts occurrences, so replacing one generation value with\nanother leaves the count unchanged and the bump looks like no change at all.\n\n**CI only ran on `push: main` and `pull_request`.** The design session pushes to a long-lived feature\nbranch with no PR, so its work met no gate until it reached main — by which point it was merged. That\nis why an unbackticked `Status:` value arrived twice and a duplicate `DC-` id six times, each caught\nby whoever merged next rather than by the branch that introduced it. **A gate that only guards the\ndestination reports problems to the wrong person.** CI now runs on every branch.\n\n**And the new gate would not have fired in CI.** `actions/checkout@v4` is shallow by default, so the\nsearch for the last generation bump would have found nothing and the gate would have passed silently\n— the defect it exists to prevent, one layer up. `fetch-depth: 0` added.\n\n**`IsKnowledge` is proven across the pipe, with an assertion that can fail.** The first version I\nwrote was `Assert.Equal(n.IsKnowledge, n.IsKnowledge)` against a fixture with no knowledge in it —\na tautology over an empty set. The daemon fixture now holds a document beside the code, and the test\nasserts a known-knowledge node arrives with the flag set and a known-code node without it.\n\n**Knowledge health is computed and unrendered**, so it is a contract request rather than Core work:\n`owner not recorded`, `type not recorded`, `orphan`, `source location not recorded` and now `review\noverdue since <date>` — 460 review dates on this repository. Findings that exist and are shown\nnowhere are \"absence of evidence stays explicit\" failing at the last step.\n\n870 tests green (App 144, Core 726). Nine gates clean, all nine run by CI, on every branch.\nZero design-owned files.",
      "rationale": null,
      "artifacts": [
        "src/AiDe.Core/Projections/ProjectionService.cs",
        "tools/verify-extractor-generation.py",
        ".github/workflows/build.yml"
      ],
      "tags": [
        "phase-3"
      ],
      "git": {
        "before": null,
        "after": "1b61599cc8be5ad753ad2725b61a78b27def6c69",
        "branch": "session/phase3-pane-probes",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0102",
      "datetime": "2026-08-31T00:12:08Z",
      "session": null,
      "kind": "architecture",
      "skill": null,
      "title": "The cap-before-filter sweep, and telling the other session what changed under them",
      "prompt": null,
      "summary": "**The cap-before-filter sweep, run properly this time.** Every bounded read in the projection service\nwas checked rather than spot-sampled: `Find` filters inside `SearchNodeIds`, `Describe` inside\n`AssertionsTouching`, `Impact` inside `OutgoingAssertions`, `Evidence` inside the cursor page — all\nfour apply the cap to rows the query has ALREADY filtered, which is the correct order and also the\ncheaper one, because the filter uses an index. `Knowledge` was the only place the order was inverted,\nand it was inverted for a reason worth naming: **its filter lived in C# rather than in the query.**\n\nThat gives the class a signature you can recognise without reading every projection: **a bounded read\nwhose `.Where(...)` is applied to the RESULT of the read rather than expressed in it.** If the filter\nis in the query the cap cannot be wrong; if it is in the caller, the cap chose the rows before anyone\nasked what was wanted. Recorded on DC-035 along with the one residual — `Knowledge` still reads each\nnode's touching assertions at 500 and splits them afterwards, so a document with ~495+ real links\nwould get an arbitrary 500 with no omission count. No repository measured comes close.\n\n**Measurements re-recorded after the knowledge work.** TheTerrace 24,058 assertions across 66 scopes,\nBioHacker 4,420 / 48, meridian 12,613 / 56 — every response still inside the frame.\n\n**The design session is told what changed under them, in their own file.** A new §4d says CI now runs\non every branch, that the next push to their branch will run all nine gates, and — because it will be\nnoisy the first time — names the two that will fire and how to satisfy them, with a worked example of\nthe accepted and rejected `Status:` forms. Neither is a new rule; both were always enforced, just not\nanywhere they could see them.\n\n**`review-by` on code: decided no, on the user's own rule.** The symmetry is tempting — 460 review\ndates on documents, so why not flag a stale class? Because nothing in a C# file, a Bicep template or\na SQL script DECLARES when it should next be read. A date could only be manufactured from\nlast-modified time or churn, and it would render identically to the dates documents actually declare.\nA reader could not tell the two apart, which is exactly the failure the no-inference note exists to\nprevent. A code artifact gets a review date the moment something writes one down; until then the\nabsence is accurate.\n\n**Still only verifiable by you:** whether the Knowledge chip lights up in the running app. Both known\ncauses are fixed and proven at every layer I can reach — the extractor, the store, the projection, and\nacross the daemon pipe — but I have not watched it render. It needs one re-index in the new build,\nbecause the generation bump makes that a rebuild rather than a reuse.\n\n870 tests green (App 144, Core 726). Nine gates clean, all nine run by CI, on every branch.\nZero design-owned files.",
      "rationale": null,
      "artifacts": [
        "docs/lessons/defect-classes.md",
        "docs/collaboration/session-contracts.md",
        "docs/measurements/repositories.jsonl"
      ],
      "tags": [
        "phase-3"
      ],
      "git": {
        "before": null,
        "after": "aec47955ceaddd0f6c598c27952696dfe4a36e87",
        "branch": "session/phase3-pane-probes",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0104",
      "datetime": "2026-08-31T00:37:34Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "kind": "architecture",
      "skill": "define-architecture",
      "title": "Read-only code viewer renders with native AvalonEdit, not Monaco-in-WebView2",
      "prompt": "define-architecture the viewer renderer choice",
      "summary": "ADR-0019 chose native AvalonEdit (MIT) over Monaco-in-WebView2 for the read-only code viewer",
      "rationale": "The repo's documented WebView2 airspace + float-crash cost (ADR-0015) outweighs Monaco's VS-Code parity, which a read-only viewer does not need; native avoids a second WebView2",
      "artifacts": [
        "docs/adr/0019-code-viewer-renderer.md"
      ],
      "tags": [],
      "git": {
        "before": "425c514a8721ac19192961c5fa5e22f89b8935e7",
        "after": "425c514a8721ac19192961c5fa5e22f89b8935e7",
        "branch": "feature/app-facelift-and-graph-surfaces",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0106",
      "datetime": "2026-08-31T00:50:10Z",
      "session": "phase3-pane-probes",
      "kind": "decision",
      "skill": "investigate",
      "title": "A fact's identity includes the reader that produced it",
      "prompt": "knowledge still shows as 0 post re-index",
      "summary": "The store's natural key said 'one revision, one answer' — true only while the extractor was fixed. A stored revision now carries the extractor generation that produced it (SourceRevision), so the same bytes read by a better reader are a different observation the key can represent. Applied inside RefreshScopeAsync so every entry point gets one answer instead of three; stripped at the read boundary so a surface still shows the revision the user named. Second decision: a command that changes the store raises WorkspaceDataChanged and the shell re-reads whatever panes are open — the last mile of a write is the screen.",
      "rationale": null,
      "artifacts": [
        "src/AiDe.Core/Extraction/SourceRevision.cs"
      ],
      "tags": [],
      "git": {
        "before": null,
        "after": "d7ba31fdfa443b51fefa42c155320d906abb0b21",
        "branch": "session/phase3-pane-probes",
        "pushed": false,
        "commits": []
      },
      "audit_ref": "al-0169"
    },
    {
      "id": "cl-0107",
      "datetime": "2026-08-31T03:39:19Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "kind": "architecture",
      "skill": "define-architecture",
      "title": "AvalonEdit confirmed for the read-only code viewer (ADR-0019 residual cleared by PoC)",
      "prompt": "AvalonEdit read-only highlighting PoC",
      "summary": "PoC passed: read-only + highlighting + no airspace (pure WPF Control); C#/py/js/sql covered, ts/bicep degrade to plain",
      "rationale": "Confirms ADR-0019's Inferred residual by execution, so the read-only viewer is ready to build once Core ships NodeContentAsync",
      "artifacts": [
        "docs/adr/0019-code-viewer-renderer.md"
      ],
      "tags": [],
      "git": {
        "before": "bf83370d7bf8d301024d21382f3724e6df4ed55e",
        "after": "bf83370d7bf8d301024d21382f3724e6df4ed55e",
        "branch": "feature/app-facelift-and-graph-surfaces",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0110",
      "datetime": "2026-08-31T04:18:19Z",
      "session": "4d24d94a-eee0-4d48-a40a-79238103a474",
      "kind": "architecture",
      "skill": "define-architecture",
      "title": "Class diagram: App-side type-hierarchy from the existing graph, dependency-free; members/Mermaid deferred (ADR-0020)",
      "prompt": "class-diagram define-architecture",
      "summary": "ADR-0020 chose an App-side derived type-hierarchy view (inherits/implements) over vendoring Mermaid now; members via a Core has_member handoff",
      "rationale": "The data already has inherits/implements as graph edges but no members; a Mermaid classDiagram with empty compartments is not worth ~3MB vendored, so defer it until has_member exists",
      "artifacts": [
        "docs/adr/0020-class-diagram-architecture.md"
      ],
      "tags": [],
      "git": {
        "before": "140a9a5bd49b2cce968b4489178ea97de2007bab",
        "after": "140a9a5bd49b2cce968b4489178ea97de2007bab",
        "branch": "feature/app-facelift-and-graph-surfaces",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0112",
      "datetime": "2026-08-31T04:38:55Z",
      "session": "phase3-pane-probes",
      "kind": "architecture",
      "skill": "implement",
      "title": "The IPC payload is JSON, not text about JSON",
      "prompt": "do the next steps autonomously",
      "summary": "IpcRequest/IpcResponse carry the payload as a JsonElement rather than a string containing serialised JSON, so the envelope no longer escapes it a second time. Version 3; IpcPayload.Read accepts a JSON string as a version-2 payload and parses its text, which is what keeps IpcVersion.Previous a real guarantee rather than a comment. Writing always produces the new form: two encodings on the write side is how a wire format ends up with no single answer to what it looks like. Measured: framing overhead 1.57x to 78 bytes.",
      "rationale": null,
      "artifacts": [
        "src/AiDe.Core/Ipc/IpcContract.cs"
      ],
      "tags": [],
      "git": {
        "before": null,
        "after": "01850d5b0f75a41482b251be4d5aefb2943355bb",
        "branch": "session/phase3-pane-probes",
        "pushed": true,
        "commits": []
      },
      "audit_ref": "al-0181"
    },
    {
      "id": "cl-0114",
      "datetime": "2026-08-31T13:26:30Z",
      "session": "phase3-pane-probes",
      "kind": "architecture",
      "skill": "implement",
      "title": "A scope records where its files are",
      "prompt": "unblock the design session",
      "summary": "Provenance carries a path relative to a scope; nothing carried the scope's own location, so no node could be resolved to a file. Scopes now emit declared_at — an attribute, so scopes do not become graph nodes — and the content reader composes it with the provenance path, confined under the workspace root. This is what makes ADR-0018 buildable at all; the alternative was deriving directories from scope ids, which works for knowledge:docs/adr and fails for bicep:main.",
      "rationale": null,
      "artifacts": [
        "src/AiDe.Core/WorkspaceCore.cs"
      ],
      "tags": [],
      "git": {
        "before": null,
        "after": "1345cb811e502ba96fd4e02adcd3fa6634708195",
        "branch": "session/phase3-pane-probes",
        "pushed": true,
        "commits": []
      }
    },
    {
      "id": "cl-0117",
      "datetime": "2026-08-31T14:57:38Z",
      "session": "phase3-pane-probes",
      "kind": "architecture",
      "skill": "implement",
      "title": "Compaction runs at startup, retaining only what renders",
      "prompt": "decide the compaction threshold",
      "summary": "DefaultThreshold 8 to 1 and DefaultRetain 2 to 1. The old threshold came from the P1-PERF latency curve and answered when growth starts to hurt; it never answered how big the store gets, which is what a user sees, and it never fired on real usage. The retained second generation was kept for investigation and nothing could investigate it — every read composes with the latest-generation filter — so it was residue, not history, and the audit log is where this project records what happened. Safe at one because a failed extraction returns before committing, so the newest snapshot always renders. The daemon compacts before opening the store: the deliberate maintenance moment the design asks for.",
      "rationale": null,
      "artifacts": [
        "src/AiDe.Core/Store/StoreCompactor.cs"
      ],
      "tags": [],
      "git": {
        "before": null,
        "after": "8865ad0418c7980c0db26e98f7ff8a077f55e573",
        "branch": "session/phase3-pane-probes",
        "pushed": true,
        "commits": []
      }
    }
  ]
};
