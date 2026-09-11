---
id: "note-20260911-on-accent-ink-is-its-own-token"
title: "The ink on the accent ground is its own token, and the leaf never states an ink"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "facelift"
tags: [decision-note, ui, contrast, tokens, wpf, webview2]
links:
  - { to: inv-0008-contrast-floor-passes-while-the-shell-fails, rel: relates-to }
  - { to: proof-contrast-census, rel: relates-to }
review-by: 2027-03-11
review-suggested: []
summary: >-
  Three calls made while implementing INV-0008 phases 1-5: the on-accent ink is AccentContrastBrush
  (DESIGN.md's accent-contrast, #0D1014) rather than a borrowed SurfaceSunkenBrush; the container that
  paints a ground states the ink that goes on it and the leaf text types state none; and the composer
  page draws from CSS custom properties the host pushes on host.init, with the token values as the
  stylesheet's fallbacks for the pre-push frame. Blast radius: every TextBlock/Label in the shell (the
  census is the proof), one additive host.init field, one new WPF token.
---

# The ink on the accent ground is its own token, and the leaf never states an ink

*A decision note (`knowledge-visualization.md` V17): below ADR weight, above chat-scrollback
weight. Written before the session that made it closed.*

- **Kind:** decision (three, one seam)
- **Confidence:** Verified — every value below is re-measured by `ShellContrastCensusTests` on the composed shell
- **Made during:** `/implement` INV-0008 phases 1–5 (session `contrast-fix`, branch `fix/contrast-census`)

## The calls

1. **`AccentContrastBrush`, not `SurfaceSunkenBrush`, is the ink on the accent.** The templates
   painted `SurfaceSunkenBrush` on the checked toggle, the selected row and the selected tab because
   its value (`#0D1014`) happened to be dark enough. `DESIGN.md` already declares
   `{colors.accent-contrast}` at the same value; the code now names the role it means. Two tokens
   share one value by coincidence, not dependency — a future retune of the sunken surface must not
   move the on-accent ink, and the census's ground column now lists every token that shares a value
   (`SurfaceSunkenBrush=AccentContrastBrush`) rather than picking one by dictionary order. 6.60:1 on
   `AccentBrush`, measured. The `DESIGN.md` palette-roles row is D1's to add (the `ui-design` node
   owns the file); this note carries the wording D1 needs: *the only ink on `{colors.accent}` as a
   ground — selected-active tab, checked toggle, selected row, mention chip — 6.6:1 on the accent;
   no other token is legal on the accent ground (text 2.4, muted 1.1, verified 1.2, danger 1.0).*

2. **The container pairs ink with ground; the leaf inherits.** The implicit `TextBlock` and `Label`
   styles keep only `Background={x:Null}`. The `Window` style states the root ink; every template's
   `ContentPresenter` states its state ink; the island card (`SurfaceChrome.WrapAsIsland`) states
   `TextElement.Foreground=TextBrush` beside the raised ground it paints — which is what finally
   pairs the composer footer's "Compiled view" / lease / status lines, the ones the operator
   photographed, that had been inheriting AvalonDock's black. Alternative rejected: keep the leaf
   setter and add a per-trigger `TextBlock` style override in each template — twelve patches over
   one precedence rule, and the next template would forget it.

3. **The composer page's palette is pushed, not copied.** `host.init` carries `theme`, eleven CSS
   custom properties read from `Application.Resources` by `ComposerPageTheme`; the stylesheet reads
   each with **the token's declared value as its fallback** for the frame before the push. Rejected:
   no fallbacks (`var(--text)` alone) — an unset property renders black on a transparent page, which
   is a white flash in a dark shell, and the census would pass it at 21:1 while the palette was
   wrong. Rejected: the old literals as fallbacks — the "second palette" U1's C2 named, and the
   craft gate's 17 Majors. The push is proven separately from the fallbacks by reading the root's
   inline custom properties (the stylesheet never declares one, so a set property is the push's
   footprint).

## What the tab trigger's condition rests on

The AvalonDock VS2013 theme paints the accent ground on `IsActive` and the selected-inactive ground
on `IsActive=False && IsSelected=True` (`Generic.xaml`, `LayoutDocumentPaneControl` item style —
read from the theme source at `master`; the package is 5.0.0). `assume:` the 5.0.0 package's
triggers match `master` for these two conditions — confirmed by the census (the active tab measures
on `AccentBrush`, the inactive on `BorderBrush`); if a later package moved the accent ground to
`IsLastFocusedDocument`, the census reads 2.37:1 on the active tab again and the on-accent ink would
follow that condition instead.
