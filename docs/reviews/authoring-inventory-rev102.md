---
id: authoring-inventory-rev102
title: "Authoring mechanisms and integration gaps — revision 102"
type: doc
status: reviewed
owner: "@timianmalloo"
phase: "assessment"
tags: [authoring, prompts, inventory, editor]
links:
  - { to: built-inventory-rev102, rel: refines }
  - { to: proposal-local-software-factory, rel: relates-to }
review-by: 2027-01-17
summary: "Seventeen detailed authoring rows distinguish the current CodeMirror and compiler mechanisms from unconnected catalogs/context/persistence and the proposed prompt-stack experience."
---

# Authoring: built mechanisms versus delivered experience

This is a detailed expansion of the authoring rows in the 49-row whole-solution inventory, not
17 additional disjoint features to add to a completion total. Source inspected at baseline9f13ca7a;
no native authoring runtime qualification was performed in this review.

| Capability | Status | Evidence | Limit |
|---|---|---|---|
| CodeMirror6 Markdown, syntax fences | Built | vendor-src/composer-bundle/composer-entry.mjs:13,28,127–138 | Current C#/JSON/Markdown configuration, not every language |
| Editor undo/redo | Built | composer-entry.mjs:127–128 | Text edit history, not prompt navigation |
| True multi-cursor / rectangular selection | New or unqualified | Inspected baseExtensions; no explicit configuration found | Spike pinned editor behavior and accessibility before claiming delivery |
| Find/replace and multi-edit transactions | New integration | No search/replace extension configured in inspected entry | Prototype demonstrates previewed exact replacement, not full editor search |
| Mention chips | Built presentation | composer-entry.mjs:61–99 | Literal characters remain source; chip does not prove attachment resolution |
| File/graph autocomplete data | Mechanism built; not wired | ComposerSurface.cs:1672–1673 | host.init sends empty candidate arrays |
| Multiline paste fencing | Built | src/AiDe.App/Web/composer.mjs:55,96 | Fence size handles embedded backticks; no invented filename provenance |
| FreeForm / GoalBlock / Template model | Built shapes | ComposerDraft.cs:6,96,136; SessionComposerBinder.cs:119 | Forms/decorations, not arbitrary reorderable rich blocks |
| Template UI/catalog mechanism | Mechanism built; session binding incomplete | SessionComposerBinder.cs:151–173; ComposerSurface.cs:560 | Sole Configure call omits optional catalog |
| Exact compiled preview | Built | ComposerSurface.cs:147,471; ComposerCompiler.cs:242 | Reuse this canonical what-will-be-sent boundary |
| Prepare/stale/send distinction | Built | ComposerSurface.cs:586,1168 | Richer edits must invalidate prepared output |
| Per-turn Reuse | Built | SessionDocumentSurface.cs:611,843–846 | Use as next draft, not reversible Back/Forward |
| Jump to prior turn | Built | SessionDocumentSurface.cs:930,1162 | Transcript navigation, not draft revision restoration |
| Compilation envelopes/history | Built | SessionDocumentSurface.cs:132–133; Compilation/Envelope.cs:25 | Durable source/submission provenance, not prompt-stack UX |
| Session unsent-draft restart recovery | Core primitive; production wiring not found | ComposerDraftStore.cs:31,51,80; full src consumer search | Do not advertise recovery from primitive tests alone |
| Standalone Prompt draft persistence | Wired | WorkbenchShell.cs:1670–1686; PromptDraftStore.cs:12 | Separate WPF draft path using .aide/prompt-drafts.json |
| Desktop prompt stack / rich structured authoring | Proposed new experience | Factory proposal and authoring mockup | Preserve scratch edits, immutable submissions, canonical serialization and permissions |

Unqualified abbreviated filenames above resolve under `src/AiDe.App/Workbench/Composer`,
`src/AiDe.App/Workbench/Sessions`, or `src/AiDe.Core/Presentation/Composer` as appropriate;
the named production types and bundle entry were inspected. This is source evidence, not a claim
that all native gestures passed. See the proposal's explicit migration stages.

Existing boundaries to preserve: renderer messages cannot Send (`ComposerMessageRouter.cs:10–31`);
host owns Ctrl+Enter (`ComposerSurface.cs:789`); field text above256KiB is rejected, not truncated
(`ComposerMessageRouter.cs:71`); deterministic compiler does not expand files/graph/network
(`ComposerCompiler.cs:22–40`); attachment bytes are excluded from draft persistence
(`ComposerDraftStore.cs:22–28`). host.init rebuilds editor instances, so preserving selection/undo
across mode changes requires explicit implementation and proof (`composer.mjs:184,263–281`).
