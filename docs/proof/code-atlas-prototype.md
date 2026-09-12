---
id: proof-code-atlas-prototype
title: "Code Atlas proposal - prototype evidence and limits"
type: doc
status: draft
owner: "@timianmalloo"
phase: proposal
tags: [code-atlas, prototype, browser, accessibility, provenance]
links:
  - { to: proposal-code-atlas, rel: documents }
  - { to: spec-addendum-c-perspectives, rel: relates-to }
review-by: 2026-10-12
summary: >-
  Browser-level evidence for the exploratory Code Atlas proposal and synthetic interaction
  prototype. Separates working local HTML interactions from unimplemented native, extraction
  and model-analysis capabilities.
---

# Code Atlas proposal: evidence, not native product certification

Scope: `docs/proposals/code-atlas/`, on `proposal/code-atlas`, based on `8d54aadc`.
No product source, accepted specification, CI or `DESIGN.md` changes.
The user authorized exploration and an HTML proposal/mockup, not implementation.

## Change reach

| Surface | Disposition |
|---|---|
| New proposal HTML | Authored in this branch; catalogue, provenance model, scaling and rollout decisions. |
| New mockup HTML | Synthetic fixture; no live extractor, provider, agent or Azure integration. |
| Direction / graph hub | New README with conceptual model, UX flow, direction and acceptance contract. |
| Browser evidence | Local generated JSON beside the proposal; no customer/repository code egress. |
| App / Core / Daemon / MCP | Not edited. |
| Accepted Architecture rail / allow-list | Not edited. The prototype illustrates the existing Architecture destination. |
| Shared audit / derived discoverability | Only this worktree's audit/index are updated; no merge or push into the primary session. |

## Evidence ledger

| Claim | Evidence / oracle | Red observed | Confidence / residual |
|---|---|---|---|
| Proposal catalogue filters work | Chromium: 15 rows total; first-slice filter yields 5; searching the exact Azure operation while first-slice is selected yields no result; changing to all yields that one operation. | Empty-result boundary exercised. An earlier test wrongly expected the broad word `Bicep` to match one row; the corpus legitimately contains it in more than one row. Corrected the oracle to the exact operation, not the product search. | Verified browser interaction; not a repository query. |
| Proposal theme pairings are legible | `proposal-checks.json`: computed foreground/background pairs from actual DOM in both themes. Accent button 6.60:1 dark, 6.22:1 light; minimum of the sampled text pairs 5.34:1. Fail below 4.5:1. | The static detector reported a contrast pair combining dark ink with a light accent. Runtime readback disproved that pairing; the actual light button uses white ink. | Verified for sampled pairs, not an exhaustive WCAG certification. |
| Proposal heading hierarchy does not skip levels | Browser walks h1/h2/h3 and asserts no upward jump greater than one level. Tables carry explicit header scopes. | Static detector caught h1 followed by h3 in the opening columns. Changed those column headings to h2; browser hierarchy check then passed. | Verified semantic outline. |
| Proposal fits review widths | Chromium document width checks at 1440, 1024, 768 and 390 CSS pixels. Table scrolling stays inside its container. | Width assertions would fail on document overflow; no deliberate overflow mutation performed. | Verified on those widths only. |
| Seven distinct question lenses and three trace presentations work | `mockup-checks.json`: seven nonempty, differently titled views; sequence, activity and data-flow content differ. An unresolved message entry point does not substitute the HTTP example. | The unresolved-entry and excluded-trace-context states refuse the misleading alternative. | Verified fixture interaction; no real code analysis. |
| Graph and tree share selection | Select the Order SVG node with Enter; focus stays on the replacement node; switch to tree and assert the same selected ID. Search excludes it without changing the selection; Reveal restores visibility. | A no-match filter exercises the negative path. | Verified browser identity and focus behavior, not native UIA. |
| Class detail is bounded and adjustable | Types, members and package overview are operable. Zoom 150% produces a 1230px diagram inside the scrollable canvas. | Explicit width assertion distinguishes a functioning zoom from an inert selector. | Verified on six fixture types; not a 5,000-type performance result. |
| AI annotation is scoped by node and snapshot | Accept a handler annotation; select Order and observe none; return to handler and recover the accepted text; refresh to a new snapshot and observe no carried acceptance. | Original prototype reproduced `candidateStatus == accepted` on a different node. Fixed by snapshot+node-keyed records. The same selection test now observes none. | Verified; simulated AI only. |
| Accept/reject/correct preserves source evidence | Context preview is explicit; candidate text is editable; acceptance stays labelled annotation; rejection removes only the candidate. Cancelling context preview does not erase an accepted annotation. | Rejected state and unavailable-provider path exercised. | Verified on the fixture. No provider data egress. |
| Hard-state recovery and export are honest | No index / indexing / extraction failure hide evidence and disable export. Partial/stale/overflow/unavailable states render their limits. Export contains fixture=true, snapshot, selected ID and edge origins. | Invalid extraction states refuse export; stale snapshot invalidates current interpretation. | Verified browser states, not backend failure handling. |
| Primary-button hover remains readable | `hover-checks.json`: dark ink on dark accent; white ink on light accent; black ink on yellow HC preview. These equal the 6.60 / 6.22 / 19.56 contrast pairings measured by the token audit. | Static detector found black-on-black hover; browser reproduced equal foreground/background. Added the explicit primary hover pairing and checked all three themes. | Verified browser hover states. |
| Browser package runs locally | No page errors or HTTP(S) requests in the interaction run; section navigation and local export exercised. | A remote-request assertion would fail on a CDN/font/model request. | Verified local file loading. No native/security certification implied. |

## Reproduction and artifacts

- `docs/proposals/code-atlas/mockup-checks.json`: complete interaction run.
- `docs/proposals/code-atlas/proposal-checks.json`: proposal contrast, headings, table headers, widths.
- `docs/proposals/code-atlas/hover-checks.json`: actual hovered computed colors.
- `docs/proposals/code-atlas/mockup-preview.png`, `domain-preview.png`, `azure-preview.png`:
  Chromium captures for visual review, not native application screenshots.
- Local run driver: `C:\Users\malla\.copilot\session-state\45bbc625-37e9-40a8-a2d0-8b39402c8e90\files\verify_code_atlas_interactions.py`.
  It uses the already-installed Python Playwright and launches Chromium. The preview's in-page
  Audit button also exposes the sampled contrast and target-size checks without tooling.

The first full run reached screenshot setup after all interaction assertions passed, then refused
to select a trace option while the tree representation correctly hid it. The capture driver was
corrected to switch to Graph before selecting the trace. The second complete run passed and wrote
the evidence above. This was a test-driver ordering mistake, not a product behavior change.

## Detector interpretation

`design-lint.py DESIGN.md --strict`: clean, zero warnings. The shared design file was read,
not edited.

The first static `ui-craft-gate.py` pass over the proposal returned:

- One real skipped-heading finding, fixed and checked through the rendered DOM.
- One cross-theme contrast pairing that cannot occur in the rendered page. The browser measured
  each theme independently; retain the detector report as a lead, not a rendering verdict.
- Table-wrapper padding findings: table cells have 12px padding; an outer wrapper's lack of
  additional padding does not place the cell text against its border.
- Minor copy-cadence and type-hierarchy findings. The proposal uses the existing 12/13/15/18/22px
  scale rather than modifying the Shell lane's shared design system.

No rule is disabled to turn this into a claimed clean scan.

The mockup's final detector pass covered both its source file and a generated, populated DOM
snapshot. No contrast or other accessibility findings remained in that pass. Remaining findings
were two flat-type-hierarchy observations (source and rendered snapshot) and one table/container
padding observation. They are retained as minor craft considerations; the existing token scale
and bounded table-cell padding are not rewritten to satisfy a heuristic.

## Review accounting and execution change

The source research agent completed 22/22 calls and returned source contracts plus six primary
reference families. No tests or spikes from that report are represented as executed.
The graph persona was mistakenly dispatched to implement a file even though its available tools
were read-only. It changed no files. Implementation returned to the main thread rather than
relaunching another agent for the same task; the existing persona was reused for read-only review.
This is an execution-plan correction, not extra product scope. The initial main-line estimate
underestimated this transfer and the browser-state corrections; no measured token-cost or speedup
claim is made.

`GATE graph-proposal · 2026-09-12 · kg-visualization-ux-expert · exit: proposal notation,
provenance, identity and progressive disclosure reviewed · PASS-WITH-CONDITIONS for proposal
only · no production clearance claimed.`

The review's four design conditions are incorporated in the proposal: position/camera retention
for all unchanged visible IDs; 6-10 visible class cards separated from the 24-type/48-relation query
ceiling; configuration evidence strengthening Inferred relations without claiming observed
traffic; and independent cumulative context ceilings before AI admission. The reviewer found no
proposal-blocking graph/UML defect and explicitly did not inspect or execute the mockup.

The final browser re-run also checked the UML realization endpoint against the interface's card
boundary and the data-flow view's explicitly added contextual database node. That sink is present
in the accessible selection list, and the caption distinguishes six in-scope nodes from one
contextual node instead of subtracting unlike counts.

Discoverability check: the new evidence hub initially used an unsupported `proof` type. The graph
tool reported it, and it was corrected to the registered `doc` type. The existing dangling
`proof-conductor-front-door` link belongs to the frozen F5 work, not this proposal; it is not
silently repaired or claimed as a new defect.

## UX review: observed defects and exact-journey repair

The first independent UX review blocked prototype sign-off. Its findings were reproduced in
Chromium, not dismissed as future native work. `ux-red-checks.json` records five true pre-fix
predicates. `ux-green-checks.json` records the corresponding corrected journeys:

| Finding | Change | Executed evidence |
|---|---|---|
| Preview dialog had no accessible name | Associated the dialog with its existing heading. | Browser-computed role/name and accessibility snapshot for Coding, Explore and New session; close returns focus to the opener. Not a screen-reader speech recording. |
| Tree → Trace announced a sequence but stayed in tree | The trace entry verb explicitly selects Graph. | Invoke from a tree-selected endpoint; actual sequence text and heading focus are asserted. |
| Edge inspection opened the previously selected node's source | Relationship inspector has an explicit supporting-source ID; both CTA and Source tab use it. Retained node evidence has a separate return action. | Inspect SQL repository → interface while handler is selected; both source paths open the SQL repository excerpt, not the handler. |
| Source/context/recovery lost focus | Focus goes to the surviving inspector container or canvas heading, not to the removed initiating button. | Check both the focused element and the next Tab target on the exact transitions. |
| Navigator snapshot remained hard-coded | Navigator, canvas badge and inspector read the same snapshot state. | After refresh all three visibly contain `demo-b28d0`. |
| Width cap was described as a viewport | Label changed to Preview width. Actual browser viewport checks remain separate. | Browser-computed combobox name is Preview width; actual-width tests remain in the main run. |

Hover repair was also rechecked in the affected themes. The full interaction run was repeated
after the fixes and regenerated the screenshots. The important class is not merely a broken
button: an entry verb, evidence link or status label can report the new state while still
rendering the old selection/body. Tests must enter through that exact verb and compare the
surfaces with each other. These review-discovered classes are included in the shared-register
handoff already deferred because SH-1 holds that file.

`GATE ux-prototype-closure · 2026-09-12 · ux-accessibility · exit: six reported findings and
the additional hover concern dispositioned against repaired source and exact-journey browser
evidence · PASS · veto cleared for this standalone iteration artifact only.`

The independent reviewer inspected the repaired source, red/green evidence and proof mapping in
three closure calls. All six findings were closed; no unresolved concern remains from that list.
The reviewer did not rerun Chromium or record screen-reader speech. This is not exhaustive WCAG
or native certification. The proposal and mockup are ready for user iteration; implementation,
native proof, model eval admission and the shared-register reconciliation remain separate work.

## Captured prototype defect classes and contention

**Selection-scoped state stored globally.** A single accepted-candidate flag can label a different
node as accepted after navigation. Sweep: inspector, AI preview, selection, snapshot refresh and
export. Derive: one interpretation lookup keyed by snapshot and node ID. Prevent: accept A,
select B, assert B has no acceptance; return to A and preserve its text; change snapshot and do
not reuse A's approval. Original red and corrected green were observed.

**Base tokens correct, interactive override invalid.** A generic button hover rule outranked a
primary-button base style and paired dark ink with a dark ground. Sweep: dark/light/HC primary
states, selected toggles and proposal buttons. Derive: the explicit primary hover uses the same
accent/ink pair as the primary rest state. Prevent: inspect actual hovered computed styles in
all themes, not only the root token matrix. Original red and corrected green were observed.

**Shared-register append deferred, explicitly:** claiming `docs/lessons/defect-classes.md` was
refused because `claude-sh-1` held the lease for SH-1. The register was not edited and the lease
was not waited out. These entries remain here for the conductor's later append/reconciliation.
No DC number was guessed or allocated over another session's work.

## Scope deviations, deliberate and bounded

- The mockup is colocated with its proposal under `docs/proposals/code-atlas/`, rather than
  modifying Design-owned `docs/mockups/`. The README is its graph hub and links both local HTML
  files. This keeps the exploratory package isolated and easy to review together.
- `DESIGN.md` is reused unchanged; no alternative product palette or design system is introduced.
- Existing specs and architecture remain normative. This is a draft proposal, not a parallel
  accepted specification. The D-4 resource-versus-C4-container wording is flagged for approval.
- No AIDE episode event is fabricated: `AIDE_SESSION` and `AIDE_CONTRACT_LOG` are absent in this
  CLI environment. Evidence is recorded durably here and in the worktree's audit.

## Native and analysis limits

Not established by this work: WPF UI Automation, WebView2/native keyboard handoff, mixed-DPI,
native performance, framework-wide entry-point recall, model domain/layer accuracy, evaluated
Bicep/compiler semantics or live Azure state. No real model request ran; token spend and
analysis latency are not measured by a simulated result.

The proposal names future admission criteria for these capabilities. They remain prerequisites
to production implementation, not blockers to reviewing an honestly labelled HTML prototype.
