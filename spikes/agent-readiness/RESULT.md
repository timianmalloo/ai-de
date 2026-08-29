# Spike — what an agent CLI actually puts on the screen

**Run 2026-08-28** · `claude` (Claude Code) launched under ConPTY in `C:\Projects\ai-de`
**Re-run:** `AiDe.Core.TerminalHost.exe <report> observe-agent claude <dir> 25`

## Why this exists

The readiness markers shipped with this build are a **guess** at what an agent's prompt looks like.
A guess that does not match refuses that agent forever, silently — an unmatched pattern and a busy
agent are the same observation from outside. The stated next action was "tune the marker against real
output, one measurement turns dispatch from always-refused into working."

The measurement says otherwise, twice.

## Finding 1 — the trust gate appears even in a directory already trusted interactively

`C:\Projects\ai-de` is where this project's Claude Code sessions run every day. Launched through
ConPTY it still opens on:

```
Accessing workspace: C:\Projects\ai-de
Quick safety check: Is this a project you created or one you trust?
  ❯ No, exit
    Yes, I trust this folder
```

So the trust gate measured in `spikes/agent-dispatch` is not an artefact of an unfamiliar folder. It
is the **normal first screen** for a session this shell starts. Whatever readiness means, it has to
survive that.

## Finding 2 — the chevron is on screen, and it means "No, exit"

The captured bytes contain `❯`, at `ESC[14;2H❯`. It is not a prompt. It is the **selection cursor of
the trust dialog**, sitting on *"No, exit"*.

A looser marker — the obvious repair when a pattern does not match, and the one this next-step was
heading for — would have matched it and reported **READY at the exact moment dispatch is most
dangerous**: the Enter that submits a prompt is the Enter that confirms *No, exit*.

The shipped pattern `(^|\n)\s*[>❯]\s*$` correctly reports **no match** here, because it requires the
chevron to be the last thing in the buffer. It is right for a reason that is easy to erode.

`ARealTrustGateIsNotMistakenForAPrompt` pins this against the captured bytes.

## Finding 3 — the approach cannot work for a full-screen agent, and this is why

The output is not lines. It is a TUI drawn with absolute cursor addressing:

```
ESC[3;2H Accessing workspace:   ESC[5;2H C:\Projects\ai-de
ESC[14;2H ❯ No, exit            ESC[15;4H Yes, I trust this folder
```

**A tail-anchored regex over the byte stream is asking where the cursor went last, not what the
screen says.** For a line-oriented shell those coincide; for an agent that repaints regions in
whatever order it likes, they do not. Making the pattern cleverer cannot fix that — the information
the pattern needs is not in the ordering of the bytes.

Establishing readiness for a full-screen agent needs the **rendered screen**: a VT parser maintaining
a cell grid, matched against the row the prompt occupies. That is a real piece of work and it is not
this step.

## Consequence

| | |
|---|---|
| The marker mechanism | **Kept, and now configurable + measurable.** It is correct for line-oriented agents and it fails closed |
| The built-in `claude` marker | **Left as it is.** It refuses, and this measurement shows refusing is the right answer for the screens observed |
| "Tune the marker and dispatch works" | **Withdrawn.** It was my own next-step, and it was wrong. No pattern over this byte stream distinguishes a prompt from a dialog |
| Screen-buffer readiness | **The next real step**, recorded rather than attempted |
| Auto-confirming the gate | **Still refused.** Sending Enter to pick "Yes, I trust this folder" would make the spike pass by answering a safety question on the user's behalf |

## Evidence

`claude-trust-gate.escaped.txt` — the captured tail, control characters made visible, used directly
as a test fixture. It is the escaped form deliberately: a raw dump is unreadable in exactly the
whitespace a tail-anchored pattern turns on.
