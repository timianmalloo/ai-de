---
id: threat-model-ai-native-ide
title: "AI-DE threat model"
type: threat-model
status: in-review
owner: "@timianmalloo"
phase: "0"
tags: [stride, security, mcp, terminal, workspace]
links:
  - { to: architecture, rel: documents }
  - { to: spec-ai-native-ide, rel: implements }
  - { to: privacy-review-ai-native-ide, rel: relates-to }
review-by: 2027-02-21
review-suggested:
  - { by: architecture, on: 2026-08-25, reason: "Defined the AI-DE workspace daemon, fact-store, MCP, terminal, and vertical delivery architecture" }
summary: >-
  Disposes STRIDE threats across workspace IPC, filesystem identity, terminal and rendering
  content, prompt delivery, MCP, audit evidence, and dependency acquisition with required
  negative controls.
---

# AI-DE threat model

## Trust boundaries

1. WPF shell/user → workspace daemon local IPC.
2. Filesystem/worktree → registry, extractors, and artifact readers.
3. Terminal process output → terminal renderer and session-state parser.
4. Repository/audit/diagram/rich-text content → visual surface and MCP context.
5. Prompt draft → terminal/agent session; agent tool request → MCP gateway.
6. Optional loopback MCP HTTP → daemon.
7. Daemon → SQLite facts, backups, and telemetry sink.

## STRIDE dispositions

| Boundary | Threat | Control | Negative proof |
|---|---|---|---|
| Shell → daemon | Spoofing, elevation, replay | Named-pipe ACL limited to current user; daemon-issued workspace capability bound to `{workspace, daemon epoch, shell process}`; versioned command envelope has command ID, deadline, cancellation, and epoch. A same-user fully compromised process is an explicit residual desktop trust-boundary risk. | Unauthorized SID, wrong capability, stale epoch, replayed command, revoked shell capability. |
| Filesystem → registry/extractor | Tampering, elevation, disclosure | Open trusted directory/file handles, retain volume/file identity, reject reparse-point escape, validate containment by handle at every privileged use; parsers run without executing project code or hooks. | Alias, symlink/junction swap, TOCTOU replacement, oversized/malformed artifact, path outside workspace. |
| Terminal output → UI | Tampering, spoofing, denial, disclosure | Terminal renderer interprets only an allowlisted display subset; OSC state requires a session nonce and is advisory; disable automatic OSC clipboard/hyperlink/host actions; bounded buffer/rate, no output-to-graph/audit/context path. | Forged OSC 133/633, OSC 52 clipboard, hyperlink/URI payload, ANSI flood, terminal text that resembles an instruction. |
| Content → visual surface | Tampering, disclosure, denial | Source labels/text are encoded as inert text; local bundles use a restrictive CSP, no remote fetch, no active SVG/markup, safe URI allowlist, and bounded renderer inputs. | Script/SVG payload, `file:`/javascript URI, remote image, oversized graph/label, hostile Mermaid/Markdown. |
| Prompt → session | Spoofing, repudiation, replay | Immutable draft revision plus `{workspace epoch, session ID/generation, dispatch key}`; **a `Pending` receipt is written before the PTY write and core recovery sweeps it to `DeliveryUnknown`** (write-ahead two-phase, ADR-0010), so a crash in the write window cannot make a retry re-deliver; the generation check is atomic with the write under the session-owner lock; unknown delivery needs a user-confirmed new command; receipt series has daemon ingress sequence and a protected HMAC digest. | Retarget, generation change, duplicate key, **crash after PTY write before finalize**, **crash after Pending before PTY write**, lost acknowledgement, reordered/mutated receipt. |
| MCP request/result | Spoofing, confused deputy, elevation, **indirect egress** | Per-tool action/resource scope independently authorized from authenticated caller **and bound to the target session's processing class (ADR-0011)**; output is tainted data returned in typed data fields only; **repo-derived strings emitted to agents are inert typed data, never blended free-text (AI-DE is an outbound-injection conduit too)**; writes require schema, idempotency key, attribution, deterministic policy validation, and confirmation for decision/consequential classes. | Cross-workspace request, forged caller field, tool-result instruction injection, **hostile symbol label emitted to an agent**, **non-`LocalOnly` caller reading rich facts**, oversized filter/result/byte, unapproved write. |
| MCP HTTP | Spoofing, DNS rebinding | Disabled by default; if enabled, loopback binding, endpoint enrollment, host/origin allowlist, authenticated capability, and explicit 403 behavior. | Hostile/missing Origin, hostile Host, unauthenticated caller, non-loopback bind. |
| Store/audit/telemetry | Tampering, repudiation, disclosure | Daemon-only data directory ACL; append-only fact triggers, monotonic ingress sequence, HMAC receipt chain protected with DPAPI, integrity/redaction state before audit use; telemetry allowlist. | Fact update/delete, forged/reordered receipt, unknown/redacted audit full-text access, PII/secret telemetry fixture. |
| Dependencies | Tampering, supply chain | Exact versions, lockfiles/locked restore, SBOM, licence/provenance review, vulnerability policy, SHA-pinned CI actions. | Typosquat, unpinned package/action, known vulnerable transitive package, licence-policy failure. |

## Acceptance gate

Every negative proof runs red before its control exists and green after. HTTP MCP remains unavailable
until its Origin/caller suite passes. Any control that cannot run on a fresh Windows runner is not
accepted as a security control.
