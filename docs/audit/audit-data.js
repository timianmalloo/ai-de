// Derived from docs/audit/*.jsonl by scripts/audit-log.py — DO NOT hand-edit (the JSONL logs are the source of truth; see audit-and-change-log.md).
window.AUDIT_DATA = {
  "project": "ai-de",
  "generated": "2026-08-28T17:03:48Z",
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
    }
  ]
};
