# Brief — T30 `wl-prefill` (AC-W01a, Rulings 180 and 182) · Claude Code · Opus · node 6

**Goal:** make the privacy gate honest about R21 before T30's second join. The Owner **holds the join** on this one item
(2026-10-20 ~01:16Z; not a new ruling: it enforces R21, which Ruling 180 made live).
**Node 5 is accepted as a branch node** (d3f76f11): (A) the suggested lines are visible (dc408cac), (B) first-calls.py,
(C) the sheet-test gate. **But** 0528992b renamed `SuggestedLinesAnnouncement` to `ReviewLinesAnnouncement` only so that
`ThePrivacyResidualsAreCoveredTests.NoAssistPathExistsAnywhereInTheComposer`
(`tests/AiDe.Core.Tests/Composer/ThePrivacyResidualsAreCoveredTests.cs:105-117`) would pass. That test bans `Assist`,
`Promote` and `Suggest` in the composer sources; its comment says the honest oracle is "there is nothing to call".
`docs/notes/addendum-b-ratification.md:83-84` defers R21 (*"no assist path exists outside the subscription engines"*)
because its test *"proves nothing while no assist path exists at all"*. Ruling 180's prefill **is** that assist path: a
model call from the composer that suggests lines. The test went red for the right reason; the rename hid it. Brief
rule 23 (new) is this lesson.

**This node (n6), in this order:**
- **(H1) Retire the `Suggest` clause** of `NoAssistPathExistsAnywhereInTheComposer`, and say why in the test: R21 is now
  live (Ruling 180), and (H2) is its control. **Keep `Assist` and `Promote`** if they still guard R20 (the mention
  expansion and the template transform, still unbuilt): read R20 in the same ratification note and say in the comment
  which it is. If R20 no longer needs them, say so and keep them anyway (the Owner did not retire them).
- **(H2) R21's real control**, a new Core or App test (your choice; say why). Every model call reachable from the
  composer goes through the session's bound compile engine under gate 1: **one sanctioned call site, named by
  file:line in the test**, and no other engine or client call in the composer sources (the same `ComposerSources()`
  set, plus any file the sanctioned site's caller chain adds inside the composer). Inferred, not read through: the site
  is the `Compiler` default at `src/AiDe.App/Workbench/Composer/ComposerSendGate.cs:148`
  (`CompileCallHost.CompileAsync`), and gate 1 is `CompileModeGate`. Read both before you write the test; if the site
  is elsewhere or there is more than one, stop and mail the Leader with what you found.
  **Mutation red:** add a second model call outside the sanctioned site in a composer source, see the test red, revert.
  Save the red as `H2-red-mutation.txt`, the green as `H2-green.txt`.
- **(H3) The announcement name:** either name is fine once the gate is honest. If you rename it back, it is one commit
  with no behaviour change. Update the defect entry's "Slip in the fix" line to say the rename evaded the gate, and
  cite `DC-NEW-wl-t30-gate-evasion` (the Leader files that class on main).
- **Then** the whole Core and App suites on your final head, and rule 22's ratchet. Name the head sha of each in the
  done mail. This node hands off for T30's second join.

**Tier** T2 · **fan-out** 0 · **budget** 80 tool calls · 120k. **Hand off at 120k** by your own measured usage.
**Kick** at 15 min by mail. Stop at the kick, even mid-check: commit what is green, then run the suite.
**Rules 19, 20, 21, 22 and 23 apply.** Check the board before every App run, as its own command. A
`USER DEMO IN PROGRESS` post with no later `USER DEMO DONE` means no App run of any kind. No real model call in any test.

## Owned authored paths
As in node 5, plus (granted for this node): `tests/AiDe.Core.Tests/Composer/ThePrivacyResidualsAreCoveredTests.cs`
(the `NoAssistPathExistsAnywhereInTheComposer` fact and its comment only), and one new R21 test file in
`tests/AiDe.Core.Tests/Composer/` or `tests/AiDe.App.Tests/Workbench/Composer/`. No src change outside the composer.
