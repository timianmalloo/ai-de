---
id: note-understanding-views-ruling-115-desktop-hold
title: "Ruling 115 — desktop-serialization hold (as used on understanding-views)"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "understanding-views"
tags: [decision-note, ruling-115, desktop-hold, understanding-views]
links:
  - { to: coordination-understanding-views, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: relates-to }
review-by: 2027-03-15
summary: >-
  Records the desktop-serialization clause of Owner Ruling 115 so citations on this
  branch resolve. One shown-window/UIA run at a time, announced with PID. Atlas A–E
  carve-outs in the same ruling are out of scope here.
---

# Ruling 115 — desktop-serialization hold (as used on understanding-views)

The Owner filed Ruling 115 on 2026-09-15 on `main` (`docs/notes/addendum-c-council-rulings.md`). This branch does not yet carry that file's later sections. The clause this programme is bound by is recorded here so a citation is checkable (DC-013).

## Ruling 115 — the hold

**One shown-window/UIA run at a time across all programmes.** Start and end are announced with **PID** in the primary checkout's `.agents/requests.jsonl`. A `desktop START` without a later `desktop END` occupies the desktop. SendInput, shown-window probes, and live App.Tests that launch GUI probes are that class.

Atlas A–E carve-outs in the same numbered ruling are **not** restated here and are **not** a grant on this branch.

**This programme:** `tests/AiDe.App.Tests/DesktopHold.cs` folds START/END and refuses `CtrlEnter_OnAFileArtifact_RequestsRevealInGraph` while occupied; it announces `ActualPID` around the probe.
