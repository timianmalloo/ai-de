# Brief — T7 `wl1a-redaction` (slice WL-1a)

**Goal (plan row):** A pure ingest-side credential-sentinel redaction transform.
**Done when:** a pure `(frame, patterns vN) → (redacted frame, redactions[])`; a sentinel in each frame position is
replaced and recorded; non-sentinel bytes are unchanged; tests red then green.
**Tier** T2 · **fan-out** 0 · **budget** 40 tool calls · 150k · 1 h · **deadline** 1 h.
**Convergence:** the function and its tests are green. T5 wires the call into the mapper — not you.

## Binding line (quoted, DC-189)
ADR-0042 §6: "The credential-sentinel scan is an **ingest-side transform inside each mapper**: the stored raw frame
(`acp.frame` / `native.frame`) is the redacted frame plus `redactions: [{offset, len, pattern_id}]` and
`scanned_with: patterns v<n>`; unredacted bytes never reach the run log, the relay, a Proof Pack or an export."
ADR-0042 §8: "Native stderr and the host's own diagnostics pass through the same sentinel/path redaction before any
retained sink; a redacted span is disclosed".

## Do
`src/AiDe.Core/AgentPlane/FrameRedaction.cs`: a static, allocation-light, pure function over the frame text with a
versioned pattern set (`PatternsVersion`), returning the redacted text and the list of `{offset, len, pattern_id}`
(offsets into the **original** frame). Use T3's envelope types for the redaction record (read
`src/AiDe.Core/AgentPlane/RunEvent.cs` on `main`). Reuse any existing sentinel/secret patterns in the repo first
(grep `sentinel`, `Redact`, `credential` in `src/`) — one definition of a credential pattern, not two; if one exists,
call it. Patterns cover at least: Anthropic/OpenAI/xAI/Google/GitHub token shapes and `Bearer …` headers, and the
test sentinel the repo's existing fixtures use. Redact with a fixed marker that preserves no secret bytes. Tests:
sentinel at the start / middle / end / twice / inside a JSON string / adjacent to another; no sentinel ⇒ identical
output and empty list; overlapping patterns ⇒ one span; the version string is stamped. Red first.

## Owned authored paths
new `src/AiDe.Core/AgentPlane/FrameRedaction.cs`, new `tests/AiDe.Core.Tests/AgentPlane/FrameRedactionTests.cs`,
`{PROOF}/**`.

## Not in scope
`AcpRunEventMapper.cs` and every other mapper (T5 wires it), the run log, stderr plumbing, any other file.
