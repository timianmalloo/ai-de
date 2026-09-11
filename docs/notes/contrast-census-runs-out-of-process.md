---
id: "note-20260911-contrast-census-runs-out-of-process"
title: "The contrast census boots the real App out of process, not a themed window in the test host"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "facelift"
tags: [decision-note, ui, contrast, wpf, testing, probe]
links:
  - { to: inv-0007-contrast-floor-passes-while-the-shell-fails, rel: relates-to }
review-by: 2027-03-10
review-suggested: []
summary: >-
  The census measures the product's composed visual tree by booting AiDe.App.App in its own process
  (AiDe.App.ContrastProbe) and reading a JSON report; an in-process Application was measured to break
  twelve later tests by unregistering the pack: URI scheme on shutdown. Blast radius: one more probe
  project on the build-order edge, one InternalsVisibleTo, ~5 s per App test run.
---

# The contrast census boots the real App out of process, not a themed window in the test host

*A decision note (`knowledge-visualization.md` V17): below ADR weight, above chat-scrollback
weight. One note per call; written before the session that made it closes.*

- **Kind:** decision
- **Confidence:** Verified — both alternatives were run, not reasoned about
- **Made during:** `/investigate` INV-0007 (session `contrast-census`)

## The call

The contrast census (`ShellContrastCensusTests`) launches `tests/AiDe.App.ContrastProbe`, which
constructs the real `AiDe.App.App`, calls its generated `InitializeComponent` (the compiled App.xaml)
and `Run`, reads the `MainWindow` WPF opens from `StartupUri`, opens every surface kind and the
session document through the shell (internal — `InternalsVisibleTo AiDe.App.ContrastProbe`), and writes
a JSON report the test asserts on. The exit code says only whether the census was taken; the verdict
and the failing rows are the test's.

**Why.** The population under test is *what the product composes*: tokens in `Application.Resources`
(where `StaticResource` in MainWindow.xaml, implicit styles reaching into templates, and every
`Application.Current?.TryFindResource(...)` fallback look), the real frame, the dock theme, the
states. A themed `Window` in the test host — `ContrastFloorTests`' host — differs on all three axes
and is the population that went green while the shell failed. A real `Application` inside the test
host was tried first and measured: the census ran, `Application.Current` was null again after
`Run` returned, and **twelve unrelated tests then failed with "The URI prefix is not recognized"** —
shutting the Application down unregisters the `pack:` scheme for the rest of the process (DC-008).
Out of process is the control this repository already uses for ConPTY, canvas focus, the web host
and the composer page (DC-014), and it is the only host in which the product's own `App` can be the
subject.

## Alternatives dismissed

- **In-process `Application`, kept alive** — leaves a `DispatcherObject` owned by a dead thread as
  `Application.Current` for every later test; `Window.Show` and `MainWindow` assignment verify access
  against it. Not measured to fail, and not worth measuring: a dead Application is not a product state.
- **Theme in `Window.Resources`, no Application** — the floor's population; the `?? Brushes.Gray`
  fallbacks fire and implicit leaf styles do not reach templates. Measured to differ from the product
  (167 vs 180 sites; the Graph tab's selection state differs).
- **A hand-composed frame parsed from MainWindow.xaml as loose XAML** — the first in-process cut. A
  mirror of the frame; the day MainWindow.xaml changes the census measures the copy.

## Assumption carried, and its validation

`Application.ResourceAssembly` is already pinned to the probe's own exe by the time `Main` runs
(measured: the public setter throws "cannot be changed after it has been set"), so the probe writes
the private static `_resourceAssembly` field and `BaseUriHelper.ResourceAssembly` by reflection. If a
WPF servicing build renames either, the probe throws with a sentence naming it and the census fails
**loudly** — the test reports "the census was not taken", never a clean pass over nothing (DC-016).
