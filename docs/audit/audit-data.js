// Derived from docs/audit/*.jsonl by scripts/audit-log.py — DO NOT hand-edit (the JSONL logs are the source of truth; see audit-and-change-log.md).
window.AUDIT_DATA = {
  "project": "ai-de-session-phase3-pane-probes",
  "generated": "2026-08-29T16:24:40Z",
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
    }
  ]
};
