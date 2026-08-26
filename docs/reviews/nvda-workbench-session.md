---
id: review-nvda-workbench-session
title: "NVDA verification session — the workbench accessibility claims"
type: doc
status: draft
owner: "@timianmalloo"
phase: "1b"
tags: [accessibility, nvda, wcag, verification, workbench]
links:
  - { to: spec-ai-native-ide, rel: documents }
  - { to: mockup-workbench, rel: relates-to }
  - { to: adr-0012-docking-shell-library, rel: relates-to }
review-by: 2027-02-26
review-suggested: []
summary: >-
  The human-run protocol that verifies whether a screen reader actually SPEAKS the workbench's
  announcements and pane names. Automated tests prove they are emitted and present in the UIA tree;
  only this session proves they are heard.
---

# NVDA verification session — the workbench accessibility claims

**Why this exists.** Every accessibility claim in Phase 1b is currently proven *one step short*:

| Claim | Automated proof we have | What is still unproven |
|---|---|---|
| Pane names reach AT | Present in the live UIA tree (UIA probe) | That NVDA **speaks** them |
| Layout changes announce (SC 4.1.3) | The announcer emits; the live region is Polite and named | That NVDA **speaks** them, and **without moving focus** |
| Every operation is keyboard-reachable (SC 2.5.7) | Catalog covers every operation; the palette lists and runs each | That a human can actually **do it blind** |
| Focus is never stranded (SC 2.4.11) | Palette restores prior focus | Real behaviour after float/collapse |

A test can prove a message was *sent*. Only a screen reader proves it was *received*. **This is the
last gap between "WCAG 2.2 AA by construction" and "verified".**

---

## Setup (about 10 minutes)

1. **Install NVDA** (free): <https://www.nvaccess.org/download/>. Take the default options.
2. **Learn the two keys you need to stop it.**
   - `Insert` + `Q` → quit NVDA.
   - `Ctrl` → **silences speech instantly**. Press this any time it will not stop talking.
   - NVDA's "speech viewer" shows everything it says as text — strongly recommended:
     **NVDA menu (`Insert`+`N`) → Tools → Speech Viewer**. Leave it open; it makes recording results
     trivial and lets you copy exact wording into this document.
3. **Build and launch AI-DE:**
   ```
   cd C:\projects\ai-de
   dotnet run --project src/AiDe.App -c Release
   ```
   The window is titled **AI-DE**. It opens in first-run state (no workspace), which is fine — the
   workbench and all layout commands work regardless.
4. **Turn on speech viewer logging** before step 1 below, so you can paste exact strings.

> **If NVDA says nothing at all at any step**, that is a result — record it. A silent step is a
> failure, not an inconclusive one.

---

## The session

Fill in **Heard** with what NVDA actually said (paste from the speech viewer), and mark **Verdict**.
Where the expectation says *contains*, exact wording does not matter — meaning does.

### Part A — Are the panes identifiable? (the UIA probe's finding)

| # | Do this | Expected | Heard | Verdict |
|---|---|---|---|---|
| A1 | Click once in the AI-DE window, then press `Tab` repeatedly until you land on a pane tab. | Tab names are spoken as **"Explore"**, **"Domain"**, **"Provenance"**, **"Terminal — pwsh"** | | ☐ pass ☐ fail |
| A2 | Listen very carefully for the phrase **"AvalonDock"** or **"Layout Document"** anywhere. | You should **never** hear either. Hearing them means the naming fix regressed. | | ☐ pass ☐ fail |
| A3 | With focus on a tab, press `Ctrl`+`PageDown`. | The **next** surface's name is spoken. | | ☐ pass ☐ fail |

*A2 is the single most important check in Part A. Before the fix, all four tabs announced as
"AvalonDock.Layout.LayoutDocument" — identical and useless.*

### Part B — Do layout changes announce, without stealing focus? (SC 4.1.3, SC 2.4.3)

| # | Do this | Expected | Heard | Verdict |
|---|---|---|---|---|
| B1 | Press `Ctrl`+`Shift`+`P`. | Something like **"Command palette. 11 layout commands. Type to filter."** | | ☐ pass ☐ fail |
| B2 | Press `Down` a few times. | **Each** command is announced with its name, gesture and hint — e.g. *"Resize pane…, Ctrl+K R, Select an edge…"* | | ☐ pass ☐ fail |
| B3 | Type `lock`, then press `Enter`. | **"Layout is locked. Unlock to rearrange panes."** | | ☐ pass ☐ fail |
| B4 | **Immediately** press `Ctrl`+`Shift`+`P` again — do not click anything first. | The palette opens again. *If it does not, focus was stranded by B3 — that is a **fail**, note it.* | | ☐ pass ☐ fail |
| B5 | Type `lock`, `Enter` (unlocks). Then `Ctrl`+`Shift`+`P`, type `maximize`, `Enter`. | Something like **"Explore maximized."** | | ☐ pass ☐ fail |
| B6 | `Ctrl`+`Shift`+`P`, `maximize`, `Enter` again. | Something like **"Explore restored."** | | ☐ pass ☐ fail |

*B4 is the focus test. An announcement that moves focus fails SC 4.1.3 even if it is spoken
perfectly — the user must be left where they were.*

### Part C — Can you do it blind? (SC 2.5.7 — the real one)

**Close your eyes, or turn the monitor off.** This part is the criterion the whole design rests on,
and it cannot be judged with your eyes open.

| # | Do this (eyes closed) | Expected | Heard | Verdict |
|---|---|---|---|---|
| C1 | `Ctrl`+`Shift`+`P`, type `float`, `Enter`. | A pane floats; you hear something like **"Provenance floating."** | | ☐ pass ☐ fail |
| C2 | `Ctrl`+`Shift`+`P`, type `collapse`, `Enter`. | **"…collapsed. Its surfaces remain available by name."** | | ☐ pass ☐ fail |
| C3 | `Ctrl`+`Shift`+`P`, type `resize`, `Enter`. | **"Resize: vertical divider. Arrow keys adjust. Enter commits, Escape cancels."** | | ☐ pass ☐ fail |
| C4 | Press `Right` three times. | Each press announces the new size, e.g. **"Divider moved. 78 percent."** | | ☐ pass ☐ fail |
| C5 | Press `Right` repeatedly (10+) until it stops moving. | **"Minimum size reached."** — and it must **refuse**, not collapse the pane. | | ☐ pass ☐ fail |
| C6 | Press `Escape`. | **"Resize cancelled."** — and the layout returns to where C3 started. | | ☐ pass ☐ fail |
| C7 | `Ctrl`+`Shift`+`P`, type `reset`, `Enter`. | **"Workbench layout reset to the default."** | | ☐ pass ☐ fail |
| C8 | **Open your eyes.** Does the window match what you believed you had done? | The spoken narrative and the visible layout agree. | | ☐ pass ☐ fail |

*C8 is the honesty check. If what you heard and what you see disagree, the announcements are
**wrong** rather than merely absent — a worse failure, because a user would have trusted them.*

### Part D — Refusals (the "is this key broken?" test)

| # | Do this | Expected | Heard | Verdict |
|---|---|---|---|---|
| D1 | `Ctrl`+`Shift`+`P`, `lock`, `Enter` (locks). Then `Ctrl`+`Shift`+`P`, `float`, `Enter`. | **"Layout is locked. Unlock to rearrange panes."** — not silence. | | ☐ pass ☐ fail |
| D2 | Unlock again. | **"Layout unlocked."** | | ☐ pass ☐ fail |

*A refused operation that says nothing is indistinguishable from a broken key. This is defect class
DC-011.*

---

## Results

- **Date / NVDA version / Windows build:**
- **Passed:** ___ / 19  **Failed:** ___
- **Any step where NVDA said nothing at all:**
- **Any step where speech and screen disagreed (C8):**

### Failures — one line each

| Step | What was expected | What actually happened |
|---|---|---|
| | | |

---

## What to do with the results

- **All pass:** the accessibility claims are verified rather than constructed. Update
  `docs/adr/0012-docking-shell-library.md` and the Phase-1b design to say *verified with NVDA
  \<version\> on \<date\>*, and mark 1b.13 done.
- **Any Part A failure:** the tab-naming pass regressed. `WorkbenchAdapterTests` and
  `WorkbenchShellTests` should have caught it — if they are green and NVDA disagrees, the automated
  control is testing the wrong thing and that is the more important bug.
- **Any Part B/C failure:** the announcement path does not reach NVDA. Most likely cause is the
  notification API, not the live region — try again after checking which of the two mechanisms fires
  (the announcer deliberately uses both).
- **Any Part D failure:** a silent refusal. That is DC-011 recurring; the control needs widening.

**Whatever the outcome, record it here and commit.** A verification session whose results live only
in someone's memory is the same as not having run it.
