# Spike result — second-dock-host-unparent (Ruling 52's Inferred clause)

- **Run:** 2026-09-11 · Windows 11 Pro 10.0.26200 · .NET 10.0.11 · `Dirkster.AvalonDock` 5.0.0 ·
  `Microsoft.Web.WebView2` 1.0.3485.44 (wrapper) · WebView2 Runtime 152.0.4191.66
- **Command:** `dotnet run --project spikes/second-dock-host-unparent`
- **Raw output:** [`RESULT-raw.txt`](RESULT-raw.txt) · exit code `0`

## The question

Ruling 52 (`docs/notes/addendum-c-council-rulings.md`) retains and amends ADR-0017 so that a
perspective's body may be a **second** AvalonDock host, and labels exactly one clause **Inferred**:
*"that a second AvalonDock host survives unparenting as the first does — the ADR's T1 no-rebuild
test must be re-run against the second host"*. Its CONDITIONS: *"Falsified if the no-rebuild test
fails for a second live docking host (a hidden `HwndHost` in host 2 restarts); then Architecture's
body becomes a non-docking composite and the ruling is re-issued."*

`ExplorerModeTests` T1 proves the presenter swap with a `Border` stand-in, which says nothing about
an `HwndHost`. This spike puts two `DockingManager`s under one `ContentControl` (the ADR-0017
presenter's shape), gives host B a real `WebView2` — the only `HwndHost` kind the shell hosts today
(the terminal is WPF-drawn: `TerminalSurface : ContentControl` over `TerminalView`) — **and** a raw
`HwndHost` (a Win32 `STATIC` child) beside it, then cycles the presenter's content
A → B → A → B (three times) and B → Explore → B, reading live state after every cycle.

## Findings

| # | Check | Observed | Verdict |
|---|---|---|---|
| Q1 | First entry into host B initialises the WebView2 once | `Loaded` fired **2×** on the first attach (AvalonDock attaches a document pane's content twice while it builds its layout — the DC-138 shape), `EnsureCoreWebView2Async` **1×**, `CoreWebView2InitializationCompleted` **1×** | PASS |
| Q2.1–Q2.3 | `CoreWebView2` is the **same object** after each B → A → B cycle | `ReferenceEquals` true on all three cycles; the object is alive *while hidden* too | PASS |
| Q2.1–Q2.3 | Page state survives (a JS variable `n = 41` and `scrollY = 500`) | `n=41`, `scrollY=500` after every cycle | PASS |
| Q2.1–Q2.3 | No re-initialisation | `Ensure=1`, `InitCompleted=1` throughout; `Loaded` climbed 3 → 4 → 5 (each cycle is one real re-attach) | PASS |
| Q3 | B → full-window surface (`Border`) → B | same `CoreWebView2`, `n=41` | PASS |
| Q4 | **The raw `HwndHost`** (the literal "hidden `HwndHost` restarts" case) | `BuildWindowCore` **1×**, `DestroyWindowCore` **0×**, HWND `0x32081C` identical before and after every cycle | not a Ruling-52 gate; **reported: no restart** |
| Q5 | The re-parents were real, initialisation stayed once | `Loaded` total 6, `Ensure` 1 | PASS |

**RESULT: PASS — a second AvalonDock host with a live WebView2 survives unparenting; Ruling 52's
condition holds.** [Verified — this run]

## What this establishes, and its boundary

1. **The presenter swap (`ContentControl.Content` = host A | host B | full-window surface) does not
   destroy an `HwndHost`'s native window when the top-level window is unchanged.** WPF's `HwndHost`
   keeps its child HWND while its `PresentationSource` (the window) is the same; unparenting from the
   visual tree hides it and re-parenting shows it. Observed for both the WebView2 wrapper *and* a raw
   `HwndHost`. The retain-never-rebuild invariant therefore **extends to a second docking host by the
   same mechanism** — it is a property of the presenter swap, not of host A.
2. **`Loaded` is per attach; initialisation must be once-guarded** (DC-138). The first entry raised
   `Loaded` twice before any presenter cycle, so any web surface in host B must go through
   `WebSurfaceHost` (the once-gate keyed to the surface's lifetime), exactly as in host A.
3. **Boundary — not measured here:** a *floating* pane creates a new top-level window (a new
   `HwndSource`); `spikes/webview2-airspace` already established the default control survives a
   float and the composition control does not. A perspective switch never floats a pane, so it is
   outside Ruling 52's clause; it stays the airspace spike's finding.
4. **Boundary — not measured here:** the Explore stand-in is a `Border`, while the product's
   `ExplorerSurface` hosts its own WebView2 — two live WebView2s in two bodies were not cycled; and
   host B's WebView2 sat in the *selected* tab — a WebView2 in a non-selected tab across a cycle was
   not measured. Both are P-4's in the product. The "initialised once" numbers (`Ensure=1`,
   `InitCompleted=1`) are enforced by this spike's own once-guard (`Program.cs:136-141`) and are not
   evidence about the platform; the load-bearing oracles are `CoreWebView2` identity, page state,
   scroll, and the raw HWND's identity with zero `DestroyWindowCore`.
5. **Boundary — not measured here:** a hidden host's *resource* cost (a hidden WebView2 keeps its
   process) — accepted by ADR-0017's negative consequence and unchanged by a second host.

## Consequence for the architecture

ADR-0017's Inferred clause is **discharged as Verified for the shell's real HwndHost kind and for the
general HwndHost case under the presenter swap**. The T1 control in the slice (P-4 of Addendum C:
a class diagram in host B keeps selection and scroll offset across a cycle; no `CoreWebView2`
re-initialisation) has a green path. Ruling 52 is not re-issued; Architecture's body stays a docking
host.

---

# Extension — A↔B↔C (WL-S, ADR-0045 §3)

- **Run:** 2026-10-19 · Intel Core Ultra 9 275HX, 127 GB · Windows 11 Pro 10.0.26300 · .NET 10.0.11 ·
  `Dirkster.AvalonDock` 5.0.0 · WebView2 wrapper 1.0.3485.44 · Runtime 155.0.4283.62
- **Command:** `dotnet run --project spikes/second-dock-host-unparent` · exit code `0` · 54 checks PASS, 0 FAIL
- **Raw output:** appended to [`RESULT-raw.txt`](RESULT-raw.txt); red/green copies in
  `docs/proof/workbench-loops-wl-s/spike/spike-red.txt` (exit 1, only the five host-C presenter checks failed) and `spike-green.txt`.

## What was run

Three `DockingManager`s (A Factory, B Review, C Understand stand-ins) under one `ContentControl` presenter.
The live WebView2 and a raw `HwndHost` sit in B. Cycles: A→B→C ×3, then C→A→C (Q6). The old two-host
assertions (Q1–Q5) are kept and pass. After every step: same `CoreWebView2` object, JS state `n == 41`,
`scrollY == 500`, one initialisation, and the raw `HwndHost` has the same HWND, 1 build, 0 destroys, and
answers `WM_GETTEXTLENGTH` within 1 s.

**Verdict: PASS.** Three hosts is the same mechanism as two; ADR-0045 §3's Inferred clause on the cycle is Verified here.

## Private bytes (forced GC; `Process.PrivateMemorySize64`)

Host shown | idle, this process | idle, WebView2 browser process | 100k doc, this process | 100k doc, browser
---|---|---|---|---
A | 161.5 MiB | 32.9 MiB | 156.7 MiB | 37.3 MiB
B | 148.3 MiB | 32.9 MiB | 152.3 MiB | 37.3 MiB
C | 153.2 MiB | 32.9 MiB | 152.5 MiB | 37.2 MiB

- The 100k-character document (`textContent.length == 100000`, loaded by `NavigateToString`) adds about **4.4 MiB to the
  WebView2 browser process** (the only measurable effect) and nothing attributable in this process.
- **Reading caveat:** all three hosts are alive in one process, so which host the presenter shows does not change
  what is allocated. The spread across rows (about 13 MiB, also negative deltas after the document) is GC/working-set
  noise of one run, not a per-host cost. A single run; no repeat variance measured. The honest budget figure is the
  process total of roughly **150 MiB with three hosts + a live WebView2 + the document, plus about 37 MiB in the
  WebView2 browser process** (renderer and GPU child processes are not counted).
- Not measured: marginal cost of a host (build the hosts one at a time and diff) and the non-selected-tab WebView2 case.
