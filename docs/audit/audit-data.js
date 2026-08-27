// Derived from docs/audit/*.jsonl by scripts/audit-log.py — DO NOT hand-edit (the JSONL logs are the source of truth; see audit-and-change-log.md).
window.AUDIT_DATA = {
  "project": "ai-de-ipc",
  "generated": "2026-08-27T16:14:39Z",
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
    }
  ]
};
