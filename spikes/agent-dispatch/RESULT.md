# Spike — prompt dispatch into a real agent CLI

**Run 2026-08-28** · `claude` (Claude Code) 2.1.251 · dispatched across a real daemon into a real ConPTY session
**Re-run:** `dotnet run --project spikes/agent-dispatch [-- <agent> <trusted-dir>]`

## Why this exists

ADR-0010 was promoted on evidence from a live **shell** session. The residual recorded at the time:

> the live session is a shell, not an agent CLI. What is proven is the *receipt protocol* against a
> real long-running interactive process. What is not proven is agent-specific behaviour.

This dispatches into a real agent to close that.

## Result: the residual is NOT closed, and now has a precise cause

```
receipt state  : PtyWriteAccepted
receipt error  : (none)
marker acted on: False
```

**The receipt is correct and the prompt never reached a conversation.** The session output shows why:

```
Accessing workspace: C:\Projects\ai-de
Quick safety check: Is this a project you created or one you trust?
  ❯ No, exit
    Yes, I trust this folder
Enter to confirm · Esc to cancel
```

Claude Code opens on a **modal trust gate**. The dispatched prompt was written into that dialog, not
into a conversation — and the `\r\n` that submits a prompt is also what confirms the highlighted
option, which was *"No, exit"*.

Reproduced in two working directories (a temp folder and this repository), so it is not a
directory-trust artefact of one location.

## What this proves, which is more than it looks

**The two-phase receipt behaved exactly as designed.** It recorded an accepted write, because a write
*was* accepted — bytes reached the pty. It did not claim delivery to an agent, because it never
claims that. A protocol that reported success here would have been wrong; this one reported what it
actually knows, and the marker check caught the rest.

That is the whole argument for ADR-0010's shape, demonstrated by the case it was built for.

## The real finding: agents have a readiness state, and nothing exposes it

A shell tells us when it is ready for input — that is what OSC 133 and the shell-integration nonce
are for, and why `SessionActivity.Ready` exists. **An agent CLI has no equivalent.** It can be:

- showing a first-run trust gate (measured here),
- authenticating,
- mid-response and not accepting input,
- ready.

From the outside all four look identical: a running process attached to a pty. **A prompt dispatched
into any of the first three is silently consumed by whatever UI is on screen.**

This is **ADR-0007's** territory (the agent session adapter), not ADR-0010's. The receipt cannot fix
it; only a readiness signal can — and the honest interim is that dispatch to an agent must be
**refused** unless the adapter can establish readiness, rather than attempted and reported as
accepted.

## What was deliberately NOT done

**The trust dialog was not auto-confirmed.** Sending `Enter` to select *"Yes, I trust this folder"*
would have made the spike pass, and would have meant the tool answering a safety question on the
user's behalf — the exact thing that gate exists to prevent. A green result bought that way would
have been worse than this red one.

## Consequence

| | |
|---|---|
| ADR-0010 (receipt) | **Stands.** It behaved correctly; the residual is not a defect in it |
| ADR-0010's residual | **Not closed. Re-characterised**: the gap is session readiness, not the receipt |
| ADR-0007 (session adapter) | **Now owes an agent-readiness contract**, and a refusal path when readiness cannot be established |
