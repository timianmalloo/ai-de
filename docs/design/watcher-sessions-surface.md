---
id: design-watcher-sessions-surface
title: "Loomkeeper Sessions Surface - WPF Treegrid Row"
type: design
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [loomkeeper, watcher, design, ui, sessions, wpf, liveness, phase-1]
links:
  - { to: architecture-loomkeeper, rel: implements }
  - { to: design-watcher-phase1-skeleton, rel: refines }
  - { to: spec-agentic-watcher-substrate, rel: implements }
  - { to: mockup-watcher-observatory, rel: refines }
  - { to: adr-0017-watcher-observation-projection, rel: depends-on }
review-by: 2027-02-26
review-suggested: []
summary: >-
  Design for the Loomkeeper Sessions surface (slice 3): the compute reader that closes the Phase-1
  change-surface. A synchronous, deterministic projection folds the observation store + liveness into
  honest session rows (Not Recorded for an unproven harness/model, a no-colour-alone liveness badge),
  exposed by a testable WatcherSessionsPaneViewModel (in AiDe.Core/Presentation, mirroring
  EvidencePaneViewModel) with the full state set, and rendered by a "sessions" surface kind in the WPF
  workbench (G6 Multi-Panel Data Terminal), in the default layout so it is actually visible.
---

# Design: Loomkeeper Sessions Surface

- **Status:** Accepted · **Tier:** T2 · **Phase:** 1, slice 3 · **Refines:** [`design-watcher-phase1-skeleton`](watcher-phase1-skeleton.md) (its final change-surface, "UI treegrid row") and the [Observatory mockup](../mockups/watcher-observatory.md) (G6, Sessions is the default surface).
- **Grounding:** `EvidencePaneViewModel` (`AiDe.Core/Presentation/`) is the exact idiom — a testable pane VM with the full state set (Loading/Empty/Ready/Stale/Error), honest "not recorded", and a no-colour-alone badge; `SurfaceContentFactory` builds a `ListBox` + status `TextBlock` from it; `SurfaceContentTests` prove the *rendered* surface on an STA thread (E11). `KnownKinds` and `Layout.Default()` are load-bearing (a kind not in the default layout is "a control nobody can see", the JoinSurface lesson).

## 1. Responsibility and boundary

One responsibility: **render the observed sessions honestly** — who is running (identity dimensions), whether they are Alive/Stale/Ended (liveness), and **Not Recorded** for anything unproven (an absent harness or model) — completing the Phase-1 walking skeleton's last change-surface (the compute reader). It owns the **sessions read projection** and the **surface that shows it**; it borrows the store and the liveness projection; it does **not** own scoring, the board, episodes, or the daemon→app live wiring.

**Decision (ladder):** the sessions projection is a **synchronous** local-store fold, not an async daemon query — so it avoids the async construction-time binding defect that stranded the evidence pane on "Loading forever" (DC-011): the pane loads its rows synchronously before the control binds. The pane VM lives in `AiDe.Core/Presentation` (rung 2 reuse of the `EvidencePaneViewModel` idiom) so its correctness is fully unit-tested without WPF.

## 2. Data model

No new persisted shape. A new **read** accessor `IWatcherObservationStore.AllSessions()` (expand-only; both the in-memory and SQLite stores already hold the session records) lets the projection enumerate. The projection is the **compute reader** the Phase-1 design's change-surface list named.

**Grain of a row:** one row is exactly one observed session, ordered deterministically (repository display, then worktree branch, then session id) so the render is stable (GO11 determinism). **Not Recorded is honest:** a null `Harness`/`Model` renders the literal "Not Recorded", never blank and never a guess (spec US-13). **Liveness is derived** (never stored) from the liveness projection.

**Change-surface (E7), ticked in implementation:** `store.AllSessions()` → `IWatcherSessionsQuery` (fold store + liveness + span count) → `WatcherSessionsPaneViewModel` (map to honest rows + state) → `SurfaceContentFactory "sessions"` (ListBox + status) → `Layout.Default()` (visible). Every row field has a **writer** (registration/liveness) and a **compute reader** (the row formatter).

## 3. Contracts

```csharp
namespace AiDe.Core.Watcher;

// New read accessor on the store seam (both impls).
IReadOnlyList<SessionRecord> AllSessions();

namespace AiDe.Core.Presentation;

public sealed record LivenessBadge(string Glyph, string Text, string TokenName)  // no colour alone
{
    public static LivenessBadge For(LivenessState state);   // Alive ✓ / Stale ~ / Ended ×
    public string AccessibleName => Text;
}

public sealed record WatcherSessionRow(
    string SessionId, string Repository, string Worktree, string Agent,
    string Harness, string Model,            // "Not Recorded" when the binding's value is null
    LivenessBadge Liveness, string Trust, int SpanCount)
{
    public string DisplayLabel { get; }      // dense, scannable one-line row
    public string AccessibleName { get; }    // full row read for a screen reader
}

public sealed record WatcherSessionSnapshot(
    string SessionId, SessionBinding Binding, LivenessState Liveness, int SpanCount);

public interface IWatcherSessionsQuery { IReadOnlyList<WatcherSessionSnapshot> GetSessions(); }

public sealed class WatcherSessionsPaneViewModel(IWatcherSessionsQuery? query)
{
    PaneState State { get; }                 // Loading -> Empty | Ready | Error (reuses PaneState)
    IReadOnlyList<WatcherSessionRow> Rows { get; }
    string StatusMessage { get; }            // evidence, never reassurance
    string LiveAnnouncement { get; }
    void Load();                             // synchronous; query is a local fold, no I/O
}
```

`WatcherSessionsQuery(IWatcherObservationStore store, LivenessProjection liveness)` folds `AllSessions()` into snapshots; a **null** query on the pane means no watcher store is wired (walking-skeleton default) and renders an honest **Unavailable/Empty** state, not a blank success.

## 4. Failure-mode analysis

| # | Failure mode | Disposition |
|---|---|---|
| Input | no sessions observed yet | `PaneState.Empty` + "No sessions observed yet." (honest empty, not a fake row) |
| Input | no watcher store wired (null query) | `PaneState.Empty` + "Session observation is not available." (Unavailable, never blank success) |
| Input | harness/model absent on a session | render "Not Recorded" (US-13), never blank/guess |
| State | a session is Ended | badge × Ended; row still shown (honest history) |
| State | a session Stale (heartbeat lapsed) | badge ~ Stale |
| Resource | the query throws (future live store) | `PaneState.Error` + "Sessions unavailable — the observation store could not be read." (DC-011: never Loading-forever, never blank) |
| Determinism | unordered store enumeration | projection sorts (repo, worktree, session) so the render is stable across platforms (GO11 / PACK-I) |

## 5. Security / privacy

No new trust boundary — a read projection over local, non-secret facts. **No capability, no work content, no personal data** on this surface (the identities are repo/worktree/terminal/agent/harness/model — tools, not persons; LINDDUN nothing-to-bind, as Phase-1 skeleton §8). The `SessionCapability` is never read by the projection (the store holds no capability).

## 6. UI (UX & Accessibility, U9/U16; G6 archetype)

- **Complete state set (U9):** Empty, Unavailable, Ready, Error — each with real, in-voice copy; no Loading-forever (the load is synchronous).
- **Not colour alone (U16):** the liveness badge carries a **glyph + text**, colour is the third signal (mirrors `ConfidenceBadge`), so it reads in high-contrast and for a colour-blind operator.
- **Accessible name (U16):** each row exposes a full `AccessibleName` ("Agent agent-1 in ai-de/main, Claude Code, Opus 4.8, Alive, 3 spans") so a screen reader announces the whole row, and the surface carries its title into the automation tree (the factory already sets `AutomationProperties`).
- **Archetype (G6 Multi-Panel Data Terminal):** dense, evidence-led, monospace-friendly one-line rows; a peer surface in the docking workbench, in the default layout.
- **Token discipline (U3):** the surface uses the existing workbench brushes/tokens (no raw hex), island-wrapped by `SurfaceChrome` like every non-windowed pane.

## 7. Test plan (Testing Strategy triggers D1; E11)

- **D1 (pane VM, in AiDe.Core.Tests):** Load with no query → Empty/Unavailable; Load with a query of zero sessions → Empty; Load with one session (harness+model present) → Ready, one row, badge Alive, trust Verified; a session with null harness/model → row shows "Not Recorded" twice; a Stale session → badge Stale; an Ended session → badge Ended; a throwing query → Error (never Loading, never blank). Ordering: three sessions across two repos → sorted (repo, worktree, session).
- **D1 (query fold, in AiDe.Core.Tests):** `WatcherSessionsQuery` folds `AllSessions()` + liveness + span count into snapshots (register→heartbeat→one span → snapshot Alive, SpanCount 1).
- **E11 (rendered surface, in AiDe.App.Tests, STA):** the `"sessions"` kind builds a control whose ListBox shows the row text and whose status is not "Loading"; `KnownKinds` includes "sessions" and it is not the Unavailable pane; the default layout contains a "sessions" surface (the JoinSurface visibility lesson).
- **Mutation:** one load-bearing oracle (the Not-Recorded formatting, or the Empty-vs-Ready state) red-then-revert.

## 8. Residual (out of slice 3)

- The **daemon→app live wiring** (a running app reading the daemon's live observation store over IPC) — the surface renders honestly with a null/empty query until then; live wiring is a later Phase-1/Phase-2 concern.
- **Repo→worktree→session grouping as a real TreeView** — the walking-skeleton row is a flat, sorted, grouped-by-label list; a nested treegrid control is a craft follow-on (the projection already orders for it).
- Scoring/evidence columns (Weave) — slices 4–7.
