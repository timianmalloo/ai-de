# Spike result — session-thread (the WPF facts behind `design-session-thread-itemscontrol`)

- **Run:** 2026-09-11 · Windows 11 Pro 10.0.26200 · .NET 10.0.11 · pure WPF (no packages) · window 1440 × 900
- **Command:** `dotnet run --project spikes/session-thread` (the probe launches `-- host` as a child process; `SPIKE_TABNAV=Local|Once|Continue` selects the feed's `KeyboardNavigation.TabNavigation` for the keyboard legs; default `Once`) · `dotnet run --project spikes/session-thread -- composer` (Q14: the real composer, headless)
- **Raw output:** [`RESULT-raw.txt`](RESULT-raw.txt) · exit code `0` · 158 `MEASURE`/`PROBE` lines (Q10–Q15 added after the Stage-4 reviews asked for them; Q14 is a separate `-- composer` leg that references `AiDe.App`; Q15 runs with `SPIKE_VIRT=off`)

## The question

`docs/design/session-thread-itemscontrol.md` (DS-1) designs the session thread as a `ListBox`-derived
feed over a recycling `VirtualizingStackPanel`, variable-height turns, an `Expander` per turn for the
folded reply, a pinned composer beneath, APG-feed keys, and UIA exposure as `List` / `ListItem` with
`ItemStatus`. Every one of those is a platform behaviour. Nothing in `src/AiDe.App` virtualizes, binds
F6, or walks a feed with the keyboard today (the digest of `SessionDocumentSurface.cs`,
`ConsoleSurface.cs`, `ComposerSurface.cs`), so each claim below is measured against a host that
resembles the design's tree, in-process and from a separate UIA client process.

A spike result is evidence for the **stated cases only** — a floor, not a verdict. The tree measured
is a stand-in (a `TextBox` for the WebView2 composer; `TextBlock`s for the lines); CV-1 re-measures
the real tree with the oracles the design names.

## Findings

| # | Claim the design makes | Observed | Verdict |
|---|---|---|---|
| Q1 | A `ListBox` with `VirtualizingPanel.IsVirtualizing=True`, `VirtualizationMode=Recycling`, `ScrollUnit=Pixel` virtualizes variable-height turns | realized containers **1 / 4 / 8 / 9** at 1 / 5 / 40 / 400 turns; viewport 574.7 px; panel `VirtualizingStackPanel` | **Verified** — the realized count tracks the viewport, not the item count |
| Q2 | The composer's top edge is equal at 1 / 5 / 40 turns (the thread scrolls, the editor does not) | `composer_top` = **604.67** at 1, 5, 40 **and 400** turns (a `Grid` with the feed in a `*` row and the composer in an `Auto` row) | **Verified** |
| Q3 | 40 turns without a layout cliff | `layout_ms` 17.6 (40) vs 20.6 (400) → **ratio 1.17–1.19**; recorded as a ratio only (DC-107: an absolute budget measures the machine) | **Verified** for the ratio; the absolute numbers are this machine's |
| Q4a | The platform does **not** follow an appended turn by itself | `offset_after_append == scrollable_after_append` **but** the extent is an *estimate* that moved 5528.8 → 5495.6 across the append (variable heights); the offset was clamped to the new maximum | **Not evidence of following.** The design implements the follow rule explicitly (pinned-before → `ScrollToEnd()` after layout); a test must not read "still at max" as "followed" |
| Q4b | A reader mid-thread is never moved by an append | offset 2610.5 → 2610.5 after an append; `mid_thread_reader_moved=False` | **Verified** |
| Q5a | The `ListBox`'s own PageDown moves by a **page** (the reason the feed overrides it) | plain `ListBox`, PageDown from 0 → **17** | **Verified** — the override is needed |
| Q5b | The feed's PageDown / PageUp move by one turn | from 39: PageUp → **38**, PageDown → **39**; focus lands on the `ListBoxItem` | **Verified** |
| Q5c | The `ListBox`'s own Up / Down / Home / End are enough inside a virtualized, variable-height feed | Up from 39 → **39 (no move)**; End from 0 → **38 (not 39)**; Home → 0 | **Falsified** — the feed owns all six keys through one index-based path (`MoveBy` / `MoveTo`), never the platform's geometry navigation |
| Q5d | Roving tab stop: the fold is a tab stop only in the current turn | `other_turn_fold_is_tab_stop=False`, `current_turn_fold_is_tab_stop=True` (`IsTabStop` bound to the ancestor `ListBoxItem.IsSelected`) | **Verified** |
| Q5e | Tab from the current turn walks its controls and then **leaves** the feed; Shift+Tab returns | `TabNavigation=Once`: Tab → `Expander#fold` → `ToggleButton#HeaderSite` → **`TextBox[Message]` (left the feed)**; Shift+Tab from the composer → the header toggle. `Local` and `Continue`: Tab **stays on the header toggle** (trapped) | **Verified for `Once` only** — the design fixes `Once`; an `Expander` contributes **two** stops (itself and its header) unless one is `IsTabStop=false` |
| Q5f | Ctrl+End reaches the editor, Ctrl+Home the header | `HandleFeedKey(End, Control)` → `TextBox[Message]`; `(Home, Control)` → the header button; plain End is not handled by the feed | **Verified for the wiring**; the modifier read (`e.KeyboardDevice.Modifiers`) is not injectable by `RaiseEvent` and stays **Inferred** until the attended run |
| Q5g | F6 / Tab entry lands on the current turn even when it is scrolled away | `FocusCurrentTurn()` (scroll into view, then focus) → the current turn. Default Tab from the header also landed on it, but the focused container stays realized, so the *unrealized* case was **not isolated** | **Verified** for `FocusCurrentTurn()`; the default-traversal case is **Inferred** — the design never relies on it |
| Q6a | The feed is a UIA `List`, a turn a `ListItem`, named `"b<n>, <words>"`, with the decoration line as `ItemStatus` | in-process: `ListBoxAutomationPeer` / `List` / `Conversation`; `ListBoxItemWrapperAutomationPeer` / `ListItem` / `b40, Refactor …` / `class defect · tier T2 · lease src/** · goal block`. Out of process: `List` "Conversation", items named and `ItemStatus` carried | **Verified** |
| Q6b | Position in set / size of set are real | **-1 / -1** on the wrapper peer `CreatePeerForElement(container)`; **36 / 40, 37 / 40, 38 / 40** over UIA (the data-item peers the `ListBox` peer's children expose); items carry `SelectionItem, ScrollItem, SynchronizedInput` | **Verified over UIA**; a headless oracle must read the item peers through `feedPeer.GetChildren()`, not the container's wrapper peer |
| Q6c | A bare `ItemsControl` would need custom peers | `ItemsControlWrapperAutomationPeer`, control type `List`; its containers are `ContentPresenter`s (no `ListItem`) | **Verified** — the rejected alternative's cost is real |
| Q7 | An `Expander` gives the fold's `ExpandCollapse` pattern; Enter and Space both toggle it | `ExpanderAutomationPeer` / `Group` / pattern present; over UIA `Collapsed → Expanded → Collapsed` by the pattern; header `ToggleButton` `AcceptsReturn=True`; Enter → expanded; Space → expanded. A plain `ToggleButton` also has `AcceptsReturn=True` | **Verified** (the author expected `ToggleButton` to refuse Enter — wrong; measured) |
| Q8a | `AutomationProperties.LiveSetting` reads back Polite / Assertive in-process | `Polite`, `Assertive` | **Verified** |
| Q8b | `RaiseAutomationEvent(LiveRegionChanged)` and `RaiseNotificationEvent(…, ImportantAll, …)` do not throw on this platform | `live.raise_notification_threw=False` | **Verified** for emission only |
| Q8c | A screen reader **hears** the polite status once and the assertive error | the managed `System.Windows.Automation` client reports `LiveSettingProperty` **Unsupported** (a UIA2 client); audibility is not measurable here | **Not measured** — `docs/reviews/nvda-workbench-session.md` Part B/D is the instrument (unrun as of 2026-08-26) |
| Q10 | An event line arriving on the running (current) turn keeps the container, the focus **and the selection** | (a) `rows[39] = new Turn{…}` (replace by index): same container instance, focus still on the fold's header — but **`SelectedIndex = −1`** (the selection was the old record); (b) a row that raises `PropertyChanged` (`MutableTurn.Update`): same container, focus on the header, header text updated to *5 events*, selection kept | **Verified** — the `ItemsSource` element is a per-ordinal **`INotifyPropertyChanged` row** (`TurnItem`), never a collection `Replace`: a replace drops the caret and with it every `IsSelected`-bound state |
| Q11 | One stop per fold: the `Expander` is not focusable (`Focusable=false`, `IsTabStop=false`) and its `HeaderSite` toggle is the stop; Enter and Space still toggle | Tab from the turn → **`ToggleButton#HeaderSite`** → **left the feed**; Shift+Tab back → the header; Enter → expanded; Space → expanded. The implicit `ToggleButton` style placed in the feed's resources did **not** reach the other turn's `HeaderSite` (`IsTabStop=True` there) — and it did not need to: under `Once` Tab never visits another item's subtree | **Verified** for the fold shape; the implicit-style roving mechanism is **falsified as unnecessary** — the design drops the inner-control roving binding |
| Q12 | Which of P2's settings are `ListBox` defaults | `TabNavigation = Once`, `DirectionalNavigation = Contained`, `IsVirtualizing = True`, `ListBoxItem.IsTabStop = True` are defaults; `VirtualizationMode = Standard` (Recycling is the design's), `ScrollUnit = Item` (Pixel is the design's); `CanContentScroll` read `False` on an unstyled instance (the default style sets it — read before the style applied, so **Inferred**; the design sets it explicitly) | **Verified** — P2 sets nothing the platform already sets; P1 sets Recycling, Pixel and `CanContentScroll` explicitly |
| Q13 | A recycled container leaks unbound visual state to another turn, and a turn scrolled away loses it | b3's fold expanded; after `ScrollToEnd` b3 was not realized and **its container was reused for b38, which showed expanded**; scrolling back to b3 gave a different container, **collapsed** | **Verified** — every per-turn visual state (`IsExpanded` ×3, the compiled-prompt scroller's offset) is bound two-way to the row (`TurnItem`), never left on the container |
| Q14 | The real `ComposerSurface` in the design's `Auto` Grid row (header `Auto` · thread `*` · composer `Auto`, 1440 × 900, no browser — the `TheWriterKeepsItsRoomTests` idiom; `dotnet run -- composer`, [`RESULT-raw-composer.txt`](RESULT-raw-composer.txt)) | **Auto row:** composer 945.8 px (taller than the window), editor host **0 px**, compiled view 831.9 px with `MaxHeight = ∞` (the DC-137 cap is guarded by `!IsPositiveInfinity(constraint.Height)`, `ComposerSurface.cs:662`, and a Grid measures an `Auto` row with infinite height), **thread row 0 px**. **With the document's belt** `composer.MaxHeight = ⌊0.45 × 900⌋`: composer 254.8, compiled capped at 141, thread row 617.2 — but the editor host is **still 0 px**: the `DockPanel`'s fill child (the WebView2) has no desired height without a browser, and an `Auto` row arranges the composer at its desired height, so the editor's slot is whatever the composer *declares*, which today is nothing | **Verified** — the first draft's "the composer's `MeasureOverride` prevents DC-137" was **false in an `Auto` row**. The composer must declare its natural height (the editor floor of 130 px, `DESIGN.md:1092`, + chrome + the compiled cap computed against a declared budget) under an infinite constraint, and the document belts with `MaxHeight`; L1's rows assert both |
| Q15 | The layout cost of the **wrong** shape (virtualization off), to calibrate L2's p95 ceiling (`SPIKE_VIRT=off`, [`RESULT-raw-virt-off.txt`](RESULT-raw-virt-off.txt)) | virtualization **on**: realized 8 / 9, first-layout ratio 1.18, p95 of 10 re-layouts 0.03 ms (40) / 0.05 ms (400); **off**: realized **40 / 400**, first-layout ratio **12.87**, p95 re-layout 0.09 ms (40) / **199.02 ms (400)** | **Verified** — a p95 ratio ceiling of 5 sits far inside the gap (≥ 12× on the first layout, ~4,000× on re-layout); the count rows (`realized(400) == 400`) are the sharper falsifier |
| Q9 | What a UIA client sees at 40 turns | **5** `ListItem`s (the realized ones), each `KeyboardFocusable`, positions 36–40 of 40; focus reported by name | **Verified** — a screen reader's item list is the realized window; `SizeOfSet` carries the true count |

## What this establishes for the design

1. `ListBox` + recycling `VirtualizingStackPanel` + pixel scrolling is the right base: virtualization,
   the pinned composer and List/ListItem/ItemStatus/PositionInSet all come from the platform.
2. The feed **owns its keys**: PageDown/PageUp/Up/Down/Home/End through one index path, Ctrl+Home /
   Ctrl+End raised as requests, the `ListBox` defaults `Once` + `Contained` left as they are (Q12),
   one stop per fold (Q11), and `FocusCurrentTurn()` for F6 entry. The platform's Up/End were
   measured wrong for a variable-height virtualized feed, `Local` / `Continue` trap Tab, and a
   roving `IsTabStop` binding on inner controls is unnecessary under `Once` (Q11).
3. The follow rule is the design's, not the platform's: "pinned before the change" is read before the
   items change and `ScrollToEnd()` runs after layout; the extent is an estimate and a test that reads
   the offset alone will pass vacuously.
4. `Expander` is the fold and the disclosures (`ExpandCollapse` for free, Enter and Space native), with
   exactly one tab stop per fold (`Focusable=false` on the `Expander`; the header toggle is the stop — Q11).
6. The row the panel binds is a **mutable `INotifyPropertyChanged` item per ordinal** (Q10): a
   collection `Replace` keeps the container and the focus but drops the selection, and the selection
   is the caret.
7. Per-turn visual state lives on the row, not the container (Q13): recycling hands one turn's
   expanded fold to another turn and forgets it on the way back.
8. The pinned composer needs two mechanisms, not one (Q14): the composer declares its own natural
   height under an infinite constraint (the editor floor inside its desired size) **and** the
   document caps it with `MaxHeight`; either alone leaves the editor at 0 px or the thread at 0 px.
5. Audibility is the one claim no spike or headless test reaches: it is an attended NVDA run.

## Boundary

- The composer is a `TextBox`; the real one is a WebView2 `HwndHost` whose focus-in path the design
  names separately (`ICanvasFocusTarget.TryFocus` idiom). Tab across that boundary is P-13's, not this
  spike's.
- Modifier keys were not injected (no real input); Ctrl legs prove the wiring through `HandleFeedKey`.
- One machine, one run per mode; the timings are a ratio only.
