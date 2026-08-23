// Derived from docs/audit/*.jsonl by scripts/audit-log.py — DO NOT hand-edit (the JSONL logs are the source of truth; see audit-and-change-log.md).
window.AUDIT_DATA = {
  "project": "AI-DE",
  "generated": "2026-08-23T20:23:58Z",
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
    }
  ]
};
