---
id: kb-wpf-styling-open-questions
title: "Modern WPF Styling — open questions & failure modes"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [open-questions, failure-modes, wpf, disconfirming]
links:
  - { to: kb-wpf-modern-ui-styling, rel: refines }
review-by: 2026-11-27
review-suggested: []
summary: >-
  What the WPF-styling research could not settle, the domain's silent failure modes, and the
  disconfirming views deliberately sought against "adopt a modern WPF UI library".
---

# Open questions & domain failure modes

## Unresolved by research

- **Did the .NET 10 Mica/backdrop work ship as stable?** The single load-bearing moving fact. Verify against
  `dotnet/wpf` Discussion #10387 ([W5]) and the installed SDK before depending on built-in Mica; otherwise
  fall back to hand-rolled `DwmSetWindowAttribute(38, 2)` or WPF UI's `FluentWindow`. *(Flagged)*
- **Exact current library versions** — WPF UI, MahApps, HandyControl, Material Design In XAML all release
  frequently; the version table in `data-and-constants.md` is approximate. Read NuGet before pinning. *(Flagged)*
- **Can Mica render *behind* WebView2/terminal panes?** The backdrop is a window-level material; whether a
  hosted opaque pane occludes it (almost certainly yes) was not tested. If the shell is mostly hosted panes,
  Mica is only visible in the chrome gutters — which may make it not worth the cost. *(Flagged — cheap spike.)*

## Known failure modes of this domain

- **`AllowsTransparency="True"` kills the modern look.** The most common WPF "custom window" tutorial uses it,
  and it silently disables the DWM shadow and rounded corners. Every "why are my corners square / shadow gone"
  bug traces here. *(Verified, [W1])*
- **Drop-shadow sprawl.** A `DropShadowEffect` on every card/list-item/button, especially animated, tanks GPU
  and battery. The effect is seductive and cheap to add and expensive at scale. *(Verified, [W14])*
- **Effects over hosted content vanish.** A shadow or Mica applied expecting it to fall over a terminal or
  WebView2 pane simply does not render (airspace). Designing chrome that assumes it will is a defect that only
  shows on the real composited window, not in a XAML preview. *(Verified, cross-ref `ai-native-ide-shell`.)*
- **Library lock-in and theme collisions.** Merging two styling libraries (e.g. MahApps + a second Fluent set)
  produces resource-key clashes and inconsistent control appearance. Pick **one** styling authority. *(Inferred.)*
- **Contrast regressions in dark mode.** "Softer" often means lower-contrast greys that fail WCAG AA for body
  text. Softness must not cost legibility — measure, don't eyeball. *(Verified, U16 gate.)*
- **Rounded corners hurting density.** Over-rounding and over-spacing a dense IDE reduces information density;
  the classic-UI backlash against JetBrains' New UI is the cautionary data point. *(Verified, [W17].)*

## Disconfirming views we deliberately sought

- **"You don't need a UI library at all — pure XAML does it."** True, and it is the *smaller correct* option
  (Solution-Selection Ladder): the DWM + `WindowChrome` + built-in Fluent stack delivers the modern-soft look
  with zero third-party dependencies. The strongest case *against* a library is that it is an aesthetic and
  supply-chain commitment for capability the platform now largely ships. **Verdict:** start library-free;
  adopt WPF UI or Material Design In XAML only for a *named* gap (Mica wrapper, Material elevation, a control
  the framework lacks). This strengthens rather than refutes the base — it just reorders the ladder. *(Reddit
  practitioner framing [W-reddit]; corroborated by the .NET 9 Fluent finding [W3].)*
- **"Most of the app is web-rendered, so WPF styling barely matters."** Partly true and important: the pane
  interiors are HTML/CSS. But the *frame* — window, title bar, docking tabs, side panels, dialogs, command
  palette, empty/error states — is substantial WPF surface and is exactly where "boxy vs modern" is judged
  first. The finding survives, narrowed: **style the WPF frame well; style pane interiors in their runtime.**
- **"Just use WinUI 3 for a native-modern look."** Refuted for this project in `ai-native-ide-shell`: WinUI 3
  has no `HwndHost` (needed for terminals) and no first-party docking. The modern look must be reached *in WPF*.
