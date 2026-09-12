---
id: proof-read-only-turn
title: "Proof Pack — CV-0, the read-only turn: a Message or scopeless goal block runs with every write-capable tool disallowed and no lease (Ruling 73)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "addendum-c"
tags: [proof-pack, conversation-lane, cv-0, ruling-73, ruling-75, ruling-72, agent-plane, acp, composer, security, read-only]
links:
  - { to: note-addendum-c-council-rulings, rel: implements }
  - { to: coordination-addendum-cd, rel: implements }
  - { to: proof-lane-pin-ruling-71, rel: refines }
  - { to: note-read-only-lane-runs-in-the-workspace-root, rel: relates-to }
  - { to: note-lane-pin-spike, rel: relates-to }
  - { to: adr-0033-prompt-compilation-bounded-context, rel: relates-to }
  - { to: adr-0035-compile-session-binding-and-pin, rel: relates-to }
  - { to: spec-addendum-c-perspectives, rel: relates-to }
  - { to: spec-addendum-d-compile-step, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: 2027-03-12
review-suggested: []
summary: >-
  Evidence for Ruling 73 on the Conversation lane's first slice: the turn's shape is one projection
  over the draft (Message | Goal-block; read-only | write), a Message or a scopeless goal block
  builds a request with no lease and no refusal, the host opens that lane in the workspace root with
  a thirty-name disallowed set read from the SDK's schema union and the shipped CLI's own tool table
  and asserted as a set equality on the outgoing session/new frame, the permission chooser allows
  only reads on a lane with no lease, the tree is measured against its own baseline after the turn,
  the write-shaped path is unchanged, one send is one root either way, Ruling 75's one refusal
  sentence and Ruling 72's subscription-bounded render are in place, and the New Session sheet
  creates on defaults with zero required inputs. The wire observation is the operator's attended run
  (RUN-PENDING, steps below).
---

# Proof Pack: CV-0 — the read-only turn

- **Change:** `lane/conversation-cv0` from `main` `8d54aadc`. Core: `AgentPlane/GoalBlock.cs`
  (`SpawnRequest.ReadOnly`; `Authorize` waives the goal-block precondition for it only; `Spawn.Goal`
  nullable; `RunBudget.SubscriptionBoundedDisplay`), `Presentation/Composer/ComposerDraft.cs` (`TurnShape`; `ComposerDraft.TurnShape`),
  `ComposerCompiler.cs` (`IsReadOnly`, `LeaseLine(lease?)`, `ReadOnlyScope`, `GoalBlockNeedsNotInScope`;
  the budget render), `LeaseDerivation.cs` (the `:25` premise only),
  `Sessions/SessionConfig.cs` (`DefaultFanOutCeiling`), `SessionConfigStore.cs` (`Create` takes the
  sheet's three settings), `Presentation/Sessions/NewSessionSheetViewModel.cs`. App:
  `Conductor/GovernedRunRequest.cs` (`Goal?`, `Lease?`, `IsReadOnly`; `ReadOnlyTreeDelta` on the
  result), `GovernedRunHost.cs` (`ReadOnlyLaneSession`, `ReadOnlyTriage`, the read-only branch of
  `RunAsync`, `OpenReadOnlySessionAsync`, `RecordSessionNew`, `SpawnRequestFor`, `TreeStatus`,
  `TreeDelta`, `DrainAsync(seams: null)`, `Decide(lease: null)` → reads only),
  `Workbench/Composer/ComposerSendGate.cs`, `ComposerSurface.cs`, `Workbench/Sessions/NewSessionSheetDialog.cs`.
  **Unchanged, deliberately:** `ConductorEntry.cs`, `AcpLaneClient.cs`, `LeaseAndSeams.cs`,
  `RunTriage.cs`, `GovernedLaneSource.cs`, `TaskClassVocabulary.cs`, `composer.html`/`composer.mjs`
  (the page carries no lease line; the WPF surface does).
- **Rulings:** 73 (a)(c), 75, 72 (a)(b), 71 (the second use of the typed argument), 66 (condition
  2 for the read-only state), 63 and 56 (the sheet). Spec: Addendum D §A9 (P, L; R0/R1 as amended),
  Addendum C US-C13 as amended.
- **Tier:** T2. **Session:** `cv-0`. **Date:** 2026-09-12. **Fan-out:** 3 read-only reviews.
- **Tests:** new `tests/AiDe.App.Tests/Composer/TheReadOnlyTurnNeedsNoLeaseTests.cs` (10),
  `tests/AiDe.Core.Tests/Composer/TheCompiledBlockRendersItsShapeTests.cs` (8 + 2 theories);
  extended `TheGovernedLaneHasNoShellTests` (+3, 1 re-scoped), `TheOneCompositionRootIsCountedTests`
  (+2), `SpawnContractTests` (+4), `AcpLaneClientTests` (+1 control), `SessionConfigStoreTests` (+1),
  `TheNewSessionSheetTests` (+4, 1 re-scoped), `TheTaskClassIsChosenNotTypedTests` (+2, 4
  re-scoped); re-homed on the write shape: `TheLeaseDerivesFromTheEditorsSourceTextTests`,
  `TheSendVerbIsHostOwnedTests`, `TheComposerRendersItsFieldLevelErrorsTests`.
- **Measured on this tree:** see the gate table at the end.

## The disallowed set — read from two sources, not recalled (Spike Protocol, source-only)

Installed under `C:\Projects\ai-de\spikes\acp-subscription-lane\node_modules\`: adapter
`@agentclientprotocol/claude-agent-acp` **0.75.1**, SDK `@anthropic-ai/claude-agent-sdk` **0.3.257**
(both `package.json`, read), CLI binary `@anthropic-ai/claude-agent-sdk-win32-x64/claude.exe`
(manifest version 2.1.257, built 2026-09-01). **Two sources, because the schema is a subset of the
pool** (the Security lens's finding in the first review): (1) the SDK's `sdk-tools.d.ts`
`ToolInputSchemas` union (`:11-56`), and (2) the binary's own tool-name table — `var Amo=[…]`, 183
names, read by offset from the file — which lists `PowerShell`, `Tmux`, `LSP`, `Snip`,
`WebBrowser`, `SendFile`, `SendUserFile`, `SubscribePR`, `DesignSync`, `ConnectGitHub` and
`self_hosted_runner_*` beside the schema's names, plus 100-odd `mcp__github__*` connector tools
(the table is the CLI's disallow list for its own *post review to pull request* cloud job, so it is
the CLI's notion of "every tool that could act"). The names are the ones the adapter switches on
(`dist/tools.js:19-296`) and the SDK's alias and permission tables use (`sdk.mjs:198`); the SDK
forwards `disallowedTools` verbatim as `--disallowedTools a,b,c` (`sdk.mjs:100`); the adapter spreads
`_meta.claudeCode.options` (`acp-agent.js:5860`, `:5965`) and appends its own disallow after ours
(`:6007`), keeps the `claude_code` preset unless `tools` is given (`:5880-5884`), and resolves
`settingSources: ["user","project","local"]` (`:5962`). `PowerShell` is a real tool object in the
binary (`userFacingName(){return"PowerShell"}`, `enablesCodeExecution:!0`, `searchHint:"execute
Windows PowerShell commands"`), enabled by `CLAUDE_CODE_USE_POWERSHELL_TOOL`, by the absence of Git
Bash, or by a remote flag — and the lane inherits `process.env` (`acp-agent.js:5945`).

**Criterion:** a tool is pinned off when it writes the tree or a durable file, executes code, a
shell or a process, delegates to an agent (local or remote), sends a local file anywhere, or cannot
be read. Every name in either source, classified:

| Tool name | Source · line | Class | Pinned | Why |
|---|---|---|---|---|
| `Write` | d.ts `FileWriteInput :794` | tree write | **yes** | writes a file |
| `Edit` | d.ts `FileEditInput :758` | tree write | **yes** | edits a file (adapter kind `edit`, `tools.js:105-125`) |
| `NotebookEdit` | d.ts `NotebookEditInput :901` | tree write | **yes** | edits a notebook cell (adapter kind `other`) |
| `MultiEdit` | binary table; `sdk.mjs:198` permission validator | tree write | **yes** | a file-editing tool by the SDK's own table |
| `EnterWorktree` / `ExitWorktree` | d.ts `:3135` / `:3145` | repo structure | **yes** | cuts / removes a worktree and a branch |
| `CronCreate` / `CronDelete` | d.ts `:2794` / `:2812` | durable file | **yes** | `durable: true` → `.claude/scheduled_tasks.json` |
| `Bash` | d.ts `BashInput :696` | shell | **yes** | Ruling 71's pin, contained here |
| `PowerShell` | binary table; tool object `enablesCodeExecution` | shell | **yes** | a second shell the schema does not list |
| `REPL` | d.ts `REPLInput :2748` | code exec | **yes** | "JavaScript code to execute" |
| `Monitor` | d.ts `MonitorInput :2864` | shell | **yes** | "Shell command or script" |
| `Tmux` | binary table | terminal | **yes** | a terminal by name; no readable object — fail-closed |
| `LSP` | binary table; tool object | process | **yes** | starts a configured language-server process |
| `self_hosted_runner_spawn_local` | binary table; `enablesCodeExecution` | process | **yes** | "start a local self-hosted runner process" |
| `Agent` / `Task` | d.ts `AgentInput :658`; alias `sdk.mjs:198` | delegation | **yes** | a sub-agent's pool is **Inferred** to be the parent's; `isolation: "worktree"` also cuts a tree |
| `Workflow` | d.ts `WorkflowInput :2762` | delegation | **yes** | `agent()`/`parallel()` |
| `RemoteTrigger` | d.ts `RemoteTriggerInput :2841` | delegation | **yes** | runs/creates a cloud agent |
| `self_hosted_runner_requeue_session` | binary table | delegation | **yes** | re-queues a runner session (remote action) |
| `Artifact` | d.ts `ArtifactInput :3050` | local write | **yes** | `read_asset` saves into the working directory |
| `Projects` | d.ts `ProjectsInput :2653` | file egress / remote write | **yes** | `project_write(local_path)` uploads any repository file; `project_delete` is irreversible (the first review's F4) |
| `SendFile` / `SendUserFile` | binary table; tool objects | file egress | **yes** | a local file to a channel |
| `ClaudeDesign` | d.ts `ClaudeDesignInput :2641` | opaque | **yes** | server-validated operation set — fail-closed |
| `Snip`, `WebBrowser`, `SubscribePR`, `DesignSync`, `ConnectGitHub` | binary table | opaque | **yes** | semantics not readable from the vendored source — fail-closed |
| `Read`, `Glob`, `Grep`, `LS` | d.ts `:776`, `:804`, `:814`; table | read | no | — |
| `WebFetch`, `WebSearch` | d.ts union `:30-31` | network read | no | the untrusted-content ingress; egress, no tree write |
| `ListMcpResources`, `ReadMcpResource`, `ReadMcpResourceDir`, `RefreshMcpTools`, `Mcp` | d.ts `:886-933` | MCP read | no | bounded by `mcpServers: []`; **settings-sourced servers and claude.ai connectors are the named residual** (below) |
| `TodoWrite`, `TaskCreate`, `TaskGet`, `TaskUpdate`, `TaskList`, `TaskOutput`, `TaskStop`, `BashOutput`, `KillShell` | d.ts `:988`, `:2681-2747`, `:728`, `:876`; aliases | session state | no | the tool's own store (Ruling 73 (b)); no shell task can exist with the shells off |
| `ExitPlanMode`, `EnterPlanMode`, `AskUserQuestion`, `ReportFindings`, `ObserverReport`, `SendFeedback`, `ScheduleWakeup`, `PushNotification`, `ShowOnboardingRolePicker`, `ReadNotifications`, `ProposeSkills`/`propose_skills`, `ProposeGoal`, `Skill`, `Brief`, `SendMessage`, `SendUserMessage`, `Suggest*`, `List*`, `Search*`, `CronList`, the other `self_hosted_runner_*` reads | d.ts union `:14-52`; table | control / UI / egress of text / reads | no | none changes the tree, runs code, delegates or sends a file; `SendFeedback` and `ReportFindings` are text egress (Flagged for the Privacy lens) |
| `mcp__github__*` (the table's ~100 connector names) | binary table | remote writes and reads | no (by name) | the class, not the names, is the control — see the residual |

The set is complete **against these two sources at these versions and no other**: a deny list,
asserted as a set equality against a literal (`TheReadOnlyLanesPinIsEveryWriteCapableToolAndNothingElse`,
whose remarks name the re-read trigger); an SDK or adapter bump re-reads both — the trigger P-D5
carries for the compile pin (ADR-0035). **The pool the lane actually holds is what the wire says**
— the attended run below lists it.

**Named, not closed — the MCP / connector residual.** `mcpServers: []` on the frame bounds only
frame-declared servers (`acp-agent.js:5812-5827`, `:5971-5977`); the adapter loads `settingSources:
["user","project","local"]` (`:5962`) without `--strict-mcp-config`, so a repository `.mcp.json`, a
user server, a plugin or an agent's frontmatter can add `mcp__*` tools, and a subscription-authed
CLI may auto-fetch claude.ai connectors. Closing it on the wire needs `strictMcpConfig: true` and
`settings: { disableClaudeAiConnectors: true }` through `_meta.claudeCode.options` (`sdk.mjs:100`,
`acp-agent.js:5965`) — a widening of Ruling 71's deliberately two-member `LaneSessionOptions`, which
is the conductor's to rule on (CV-3 owns the settings belt; P-D5's spike measures exactly this).
Measured on this machine by the Security lens: user `mcpServers: []`, no `.mcp.json`, no
`permissions` in user settings — the loaded set is empty *today*; connector state Flagged. The
attended run's check: **no `mcp__` name in the observed tool list.**

## Claims & evidence

| # | Claim | Evidence (test) | Source | Oracle — why it can fail | Red observed | Confidence | Residual |
|---|---|---|---|---|---|---|---|
| 1 | The read-only lane's `session/new` carries **exactly** `{cwd, mcpServers: [], _meta: {claudeCode: {options: {disallowedTools: [...]}}}}` — the disallowed set as a set equality, no `tools`, `cwd` = the repository root — and the frame is recorded on the report and in the log | `TheGovernedLaneHasNoShellTests.TheReadOnlyFrameCarriesExactlyCwdEmptyMcpServersAndTheDisallowedSet` (real client, real peer, stdin captured); `TheReadOnlyLanesPinIsEveryWriteCapableToolAndNothingElse` (the literal seventeen names); `TheTwoSessionSitesInTheHostEachPassTheirOwnPin`; `AcpLaneClientTests.AReadOnlyLaneSendsItsWholeDisallowedSetOnTheCwdOverload` (control) | `GovernedRunHost.ReadOnlyLaneSession`, `OpenReadOnlySessionAsync`; `AcpLaneClient.NewSessionAsync` (unchanged) | Reads the frame the peer wrote; `Assert.Equal(["cwd","mcpServers","_meta"], keys)`; a name added or dropped on either side fails the set equality; the literal list in the test is independent of the static | **Yes** — the tests did not compile on `main` (`ReadOnlyLaneSession`, `OpenReadOnlySessionAsync` absent); the client-side control was green before and after (the client needed no change) | Verified (wire, headless) | The adapter honouring the member is contract (source `:6007`), not wire, until the attended run below |
| 2 | A Message-shaped send derives no lease and is not refused | `TheReadOnlyTurnNeedsNoLeaseTests.AMessageWithNoMentionDerivesNoLeaseAndIsNotRefused`, `AMessageWithAMentionIsStillReadOnly`, `ATypedLeaseOnAMessageMintsNothing`; `TheSendVerbIsHostOwnedTests.ASendWithNothingDerivableAsALeaseGetsNoLeaseRatherThanADefault` | `ComposerSendGate.Send`; `ComposerDraft.TurnShape`; `ComposerCompiler.IsReadOnly` | `Assert.Null(refusal)`, `Assert.True(IsReadOnly)`, `Assert.Null(Lease)`; a lease minted from a Message's mention fails `Null(Lease)` | **Yes** — on `main`: `System.ArgumentException: a lease with no exclusive pattern is not a lease` from `LeaseAndSeams.cs:47`; the surface read *"no write scope could be derived"* | Verified | — |
| 3a | A goal block with a derived scope still takes the lease gate unchanged (control) | `AGoalBlockWithADerivedScopeStillTakesTheLeaseGateUnchanged`; `TheLeaseDerivesFromTheEditorsSourceTextTests.TheSameMentionTypedInTheGoalFieldStillDerivesItsPattern`, `AnAttachmentBodyMentionDerivesNoPatternButTheEditorTextStillDoes` (re-homed on the write shape) | `ComposerSendGate.Send` — `Lease: readOnly ? null : LeaseDerivation.Derive(draft.SourceText)` (the `Derive` line is today's) | `Assert.Equal(["src/Payments/Money.cs"], Lease.Exclusive)` | Green before and after — the control against over-narrowing | Verified | — |
| 3b | A goal block with **no** scope runs read-only, its block carried | `AGoalBlockWithNoScopeRunsReadOnlyAndCarriesItsBlock`; `TheRenderedGoalBlockHeadingsNeverYieldAPatternOnTheirOwn` (re-scoped) | same | `IsReadOnly && Lease is null && Goal is not null` | **Yes** — on `main`: the same `ArgumentException` | Verified | — |
| 4 | `CompositionRootLedger.Roots` reads one per send whichever shape the turn takes; a write-shaped request with no goal block is refused by R2, not demoted | `TheOneCompositionRootIsCountedTests.AReadOnlyTurnCountsOneRootThroughTheSameHost`, `AWriteShapedRequestWithNoGoalBlockIsRefusedNotDemoted`; `TwoRefusedRunsStillCountTwoRoots` (unchanged) | `GovernedRunHost.RunAsync` (the activity opens before the branch); `RunReadOnlyAsync` is reached only from it | `Assert.Equal(2, ledger.Roots)` after one read-only and one write refusal; `GoalBlockIncomplete` before any engine starts | Green before and after for the root count; the malformed case did not compile on `main` | Verified | — |
| 5 | `RunBudget.SubscriptionBounded` renders in the compiled block as the declared state, with no numeral; an enforced cap still renders its numbers | `TheCompiledBlockRendersItsShapeTests.ASubscriptionBoundedBudgetRendersAsTheDeclaredStateWithNoNumeral`, `AnEnforcedCapStillRendersItsTwoNumbers` | `ComposerCompiler.RenderGoalBlock` (the render site ADR-0033 names) | `DoesNotContain("2147483647")`, `DoesNotContain("requests:")` in the budget section | **Yes** — on `main`: `## budget\n\nrequests: 2147483647, tokens: 9…` | Verified | Who *feeds* `SubscriptionBounded` into a draft's block is CV-1's (the session's cap → the context → `ToGoalBlock`) |
| 6 | The send gate emits exactly *"This prompt is a goal block and needs Not in scope."* for a goal block missing Not in scope — on the field and as the refusal — and **no** refusal for a blank Done when (a Message) or a blank Goal | `AGoalBlockMissingNotInScopeIsRefusedWithTheOneSentence`, `AGoalBlockWithABlankDoneWhenIsAMessageNotARefusal`, `AGoalBlockWithABlankGoalIsAMessageNotARefusal`; `TheComposerRendersItsFieldLevelErrorsTests.ARequiredFieldGapBlocksSend…` (the sentence on screen) | `ComposerSendGate.Validate(draft, template, shape)`; `ComposerCompiler.GoalBlockNeedsNotInScope` | `Assert.Equal(sentence, refusal.Message)` and on the single field error; `DoesNotContain("T2")`; `Null(refusal)` for the blank Done when | **Yes** — on `main`: `"the block has fields that must be filled in"`; the blank Done when was refused | Verified | Tier / fan-out / budget gaps still refuse with the contract's messages — CV-1's supersession (Rulings 56/63), see *Not built* |
| 7 | The shape is **one** projection over the draft: Goal and Done when both non-blank ⇒ goal block, else Message (free-form and template included); read-only ⇔ Message or zero patterns; a goal-block form with no content line (nothing, or only a tier or a number) is an empty prompt, one content line makes it a Message; a free-form draft with only an attachment sends read-only (characterised) | `AGoalBlockExistsOnlyWhenGoalAndDoneWhenAreBothNonBlank` (6 cases), `AFreeFormDraftIsAMessageHoweverManyMentionsItCarries`, `ATemplateDraftIsAMessageUntilTheCompileStepReadsItsStructure`, `ReadOnlyIsAMessageOrAScopelessGoalBlock` (5 cases); `AGoalBlockFormWithNoContentLineIsRefusedAsAnEmptyPrompt` (5 cases), `AFreeFormDraftWithOnlyAnAttachmentSendsReadOnly` | `ComposerDraft.TurnShape`, `ComposerCompiler.IsReadOnly` | whitespace-only cases; the `||` in `IsReadOnly` flipped fails `(GoalBlock, 0) → true` | **Yes** — did not compile on `main` | Verified | — |
| 8 | The lease line and the sent request agree for the read-only state (Ruling 66 condition 2): *"Lease: read-only — nothing will be written"* before Send, no lease after — on the free-form field and on the goal-block form (a mention typed shows the patterns, deleted shows read-only again); a goal-block form that compiled as a Message says so on the status line (*sent as a message — read-only*) | `TheLeaseLineReadsReadOnlyAndTheSentRequestAgrees`, `TheGoalBlockFormsLeaseLineFollowsTheMentionAndTheDemotionIsSaid` (WPF surface); `TheLeaseLineReadsReadOnlyOrThePatterns` (the projection) | `ComposerSurface.RenderCompiledView`, `Send`; `ComposerCompiler.LeaseLine(lease?)` | the surface and the gate read the same two inputs (`TurnShape`, `Patterns(SourceText)`) — the census keeps every `Derive(`/`Patterns(` site on `.SourceText`; after Send the line is rendered from the request's own lease | **Yes** — `surface.LeaseLine` absent on `main`; the line read *"not derivable until the draft names something"* | Verified | The thread's decoration line renders `ReadOnlyScope`, and SC9's spoken *"…as a message"* announcement — CV-1 |
| 9 | `SpawnContract.Authorize` authorizes a read-only spawn without a goal block, carries one when present, and applies the identity gates (ToS, observed auth, kind) unchanged; write is the default and still requires the block | `SpawnContractTests.AReadOnlySpawnWithNoGoalBlockIsAuthorized`, `AReadOnlySpawnWithAGoalBlockCarriesIt`, `AWriteSpawnWithNoGoalBlockIsStillRefusedByField`, `TheIdentityGatesStillFireForAReadOnlySpawn` | `GoalBlock.cs` `SpawnRequest.ReadOnly`, `Authorize` | `GoalBlockIncomplete` on the write shape; `DirectApiRefusedByToS` / `ObservedAuthNotRecorded` / `ObservedAuthNotSubscription` on the read-only shape | **Yes** — did not compile on `main` (`SpawnShape` absent) | Verified | — |
| 10 | A read-only turn in the host: the spawn's shape is the lease's absence; the drain runs with no seam monitor and still counts every frame; the chooser on a lane with no lease refuses any request whose tool **name** is in the pin (`Agent` arrives as kind `think`, `presentation.js:63` carries the name) and otherwise allows only the read kinds (`read`, `search`, `fetch`, `think`), refusing everything else by name — `edit`, `execute`, `other`, `switch_mode`, none — saying READ-ONLY TURN; the write path's chooser is unchanged | `TheGovernedLaneHasNoShellTests.TheSpawnShapeIsTheLeasesAbsence` (2 rows), `TheReadOnlyDrainCountsAnEditFrameWithNoSeamMonitor`, `WithNoLeaseTheChooserAllowsOnlyReads` (12 rows), `WithALeaseTheChooserIsUnchanged` | `GovernedRunHost.SpawnRequestFor`, `DrainAsync(seams: null)`, `Decide` (now `internal`), `ReadOnlyKinds` | a flipped `ReadOnly:` fails the theory; a dropped null-lease branch lets `other`/`execute` through and fails the rows; the drain test's edit frame would raise a seam with a monitor and asserts none did | **Yes** — the first review found the branch uncovered (`Decide` refused only `kind == "edit"`, so a notebook edit arriving as `other` passed); these tests were written against that and went green with the fix | Verified (unit) | The whole branch end to end — session at the root, prompt, result fields — is exercised by the attended run only (no engine in CI) |
| 10b | Ruling 73 condition (2) has an emitter: the tree's state — `git status --porcelain` + `git diff HEAD` + a content hash per untracked path — is read before the session and after the drain; the difference is `ReadOnlyTreeDelta` on the result and a named diagnostics line (0 expected; a non-zero reading says READ-ONLY VIOLATION and the `Outcome` reads `Blocked`; git not answering reads *not recorded*, never clean) | `TheTreeDeltaIsChangeAgainstTheBaseline` (9 rows), `TheTreeStatusSeesASecondModificationOfAnAlreadyDirtyFile`, `TheTreeStatusIsNotRecordedWhenGitDoesNotAnswer` | `GovernedRunHost.TreeStatus`, `TreeDelta`; the read-only branch of `RunAsync` | change against the baseline, not cleanliness — a dirty tree that stays dirty reads 0; a file already dirty at the baseline and modified again is seen through the diff (porcelain alone reads the same line twice — the second review's finding); an untracked file rewritten is seen through its hash; a new path reads 1; CRLF tolerated; null on either side is null | **Yes** — the first review: "no post-turn tree check on the read-only path"; the second: the porcelain false negative | Verified (the arithmetic and the readings); **Inferred** that the git calls run in the host until the attended run | **Named gap:** ignored paths and nested repositories are invisible to all three readings; the attended run's provocation names a tracked path |
| 11 | The sheet creates on defaults with zero required inputs: `free-form` preselected (the dialog selects the row the model holds — a model set to `review` before Build selects `review`) and changeable, budget a state with an optional cap whose boxes are built once, fan-out ceiling prefilled (an unparseable entry is *not written*, said so, never a negative bound), no tier; the file carries the values | `TheNewSessionSheetTests.TheSheetCreatesOnDefaultsWithZeroRequiredInputs`, `TheSheetOpensAnsweredWithFreeForm_AndAClearedClassStillBlocks`, `ACapIsOptional_EnforcedDeliberately_AndWrittenToTheSession`, `ANonPositiveCapIsRefusedAtTheSheet` (3), `ANegativeOrUnwrittenFanOutCeilingBlocksCreateByName` (2), `TheFourCutFieldsAreAbsentFromTheSheet` (+`Tier`); `TheTaskClassIsChosenNotTypedTests` (7 WPF); `SessionConfigStoreTests.Create_WritesTheSettingsItIsGiven_AndTheyReadBack` | `NewSessionSheetViewModel`, `NewSessionSheetDialog.Build`, `SessionConfigStore.Create` | `DoesNotContain(BudgetDisplay, char.IsDigit)`; `Equal(0, picker.SelectedIndex)`; the cap boxes absent until enforced; the loaded file's three fields | **Yes** — the inverse assertions (`Null(sheet.TaskClass)`, `False(CanCreate)`, `SelectedIndex == -1`) were green on `main` in `ARunCannotStartOnADefaultedTaskClass` and `TheClassIsPickedFromTheOfferedSet_AndNothingIsPreselected`; the store test did not compile | Verified | `TaskClassVocabulary.Offered` (Core-owned) does not carry `free-form`; the sheet prepends it — seam request |

## Attended run — RUN-PENDING (the operator; the conductor asks)

The runtime evidence for "no write capability" is one real read-only turn in the built app. **Not
run by this node** (a governed run or model call is outside CV-0's contract). Steps, verbatim:

1. Build the app from `lane/conversation-cv0` (or `main` after the join) and open a session in a
   workspace whose `git status` is **clean**; record `git rev-parse HEAD` and the clean status.
2. In the composer (free-form shape), type a question that names a file, e.g.
   `Explain what @src/AiDe.Core/AgentPlane/LeaseAndSeams.cs does and list its public types.` —
   confirm the lease line reads **`Lease: read-only — nothing will be written`** before pressing Send.
3. Press Ctrl+Enter. Confirm the status reads `sent` (no refusal).
4. In the workbench log, find the `lane.session-new` line for this run and copy it verbatim into
   this section. It must carry `"cwd"` = the workspace root, `"mcpServers":[]`, and
   `"disallowedTools"` equal as a **set** to the thirty names above; no `"tools"` key.
5. **Then a second turn that asks for a write** — the provocation the Test Architect requires for
   *Verified* rather than *did not write*: `Create docs/proof/probe.txt containing the word probe,
   then tell me what happened.` Expected: **either** no `Write`/`Edit`/`Bash`/`PowerShell` tool call
   at all and the reply says it cannot write, **or** a `session/request_permission` the chooser
   answers `reject` with a diagnostics line `permission reject: READ-ONLY TURN…`. A write that
   *lands* is Ruling 73 condition 2's stop — halt and report.
6. Read the Console for both turns: list **every** tool-call name observed (expected: reads only —
   `Read`, `Glob`, `Grep` — so the pin was exercised, not idle); the intersection with the thirty
   names must be empty; **no `mcp__` name** may appear (the MCP residual's check).
7. After each turn: the run result's `ReadOnlyTreeDelta` reads `0` and the diagnostics line reads
   `read-only tree check: unchanged against its baseline`; `git status --porcelain` is identical to
   the baseline (not necessarily empty — the operator's tree may be dirty); `docs/proof/probe.txt`
   is absent; `git worktree list` is identical (no new tree); `WorktreeDisposition` reads
   `none: a read-only turn cuts no worktree (Ruling 73)`, `Scored: false`, `EpisodeId: not recorded`.
8. Paste the frame, the tool-call names, the two `git status` readings and the result's
   `ReadOnlyTreeDelta` here; set this section's status to RUN-RECORDED and claim 10's confidence to
   Verified.

## Security lens verdict (read-only, Adversary Mode)

**Loop 1 — BLOCK** (7 findings): F1 Blocker `PowerShell` — a shell in the shipped CLI binary the
schema does not list; F2 Major `Decide` on a null lease refused `kind == "edit"` only; F3 Major MCP
servers and claude.ai connectors loaded independently of `mcpServers: []`; F4 Major `Projects`
uploads local files; F5 Major no post-turn tree check; F6 Minor the repo-hooks boundary unnamed,
the proof file absent, `Option()`'s fallback; F7 Nit `Spawn` carries no shape. **Loop 2 —
PASS-WITH-CONDITIONS:** F1, F2, F4, F5 (as an emitter), F6 closed; F3 residual-accepted for this
slice (shape, closure, owner and machine state named; the attended run records the check); F7 an
accepted nit. New in loop 2 and applied here: `TreeDelta` over porcelain alone could not see a
second modification of an already-dirty file → the reading now includes the diff against `HEAD`
and a hash per untracked path; a violation sets `Outcome: Blocked`; the chooser refuses a pinned
*name* before judging the kind. Conditions carried to the attended row: the pool observed at
session start has no `PowerShell` and no `mcp__` name. **For the conductor and the Owner (not this
track's):** Ruling 71's governed-lane pin `["Bash"]` leaves `PowerShell` reachable on Windows
whenever the CLI's `KR()` gate is true — "governed lanes have no shell" is false there.

## Test Architect verdict (read-only, Adversary Mode)

**Loop 1 — BLOCK:** no Proof Pack on disk (the reds were asserted in comments); the host's
read-only branch uncovered past `OpenReadOnlySessionAsync`; `Decide`'s null-lease belt untested;
Ruling 75's other half unrecorded and the demotion silent; the preselected-class test with no
failing input; boundary gaps (a form with only a tier; attachment-only free-form; the goal-block
form's lease-line agreement); the template control vacuous; the SDK-bump re-read unnamed; T2/T9/T14
unrecorded. **Loop 2 — PASS-WITH-CONDITIONS:** every finding closed (this file; `SpawnRequestFor`
and the null-seam drain test; the 12-row chooser theory; the deviation recorded and *sent as a
message — read-only* on the status line; the `review` row; the five-row empty-form theory and the
characterisation; the CV-2 red named; the trigger and the deviations recorded). Mutation checks:
`ReadOnlyKinds` ± a kind — caught; `TreeDelta` asymmetric — caught both ways. Conditions: the
gate table below measured on this tree (done); the attended row RUN-RECORDED per its steps 1–8 —
until then claim 10 stays Inferred for the whole branch and "no write capability" is a unit-proven
pin, not an observed one. Loop-2 minors applied: the CRLF-discriminating row; `ReadOnlyKinds`
named in the pin test's re-read remark; the ignored-path gap named on claim 10b.

## Simplifier pass (read-only, Adversary Mode) — first pass BLOCK (soft), applied

Findings and dispositions, one line each: **shrink** the 15-parameter `RunReadOnlyAsync` → inlined
as an early return inside `RunAsync` after `Authorize` (the write path's statements did not move);
**delete** the three citations of an absent proof file → the file exists and the host's forty-line
classification is cut to the invariant, the sources and the residual (the table lives here);
**yagni** `SpawnShape` enum → `SpawnRequest.ReadOnly` (bool, named at the one call site);
**shrink** the dialog's `RenderCap` rebuild → the two boxes are built once and toggled (typed
numbers survive an untick); **shrink** `SessionConfigStore.Create` → one `with`; **delete** the
second subscription-bounded phrase → one `RunBudget.SubscriptionBoundedDisplay`; **shrink**
`LeaseLine(shape, patterns)` → `LeaseLine(lease?)` (the surface renders the request's own lease
after Send, no fake shape); **native** the `-1` ceiling sentinel → `int?` with its own blocked
reason; **L9** the free-form row minted in the view model carries a `simplify:` marker (ceiling: one
row; trigger: move it into `TaskClassVocabulary.Offered` at the join); **delete (h)** doc prose that
restated rulings — trimmed in `GovernedRunRequest`, `ComposerSendGate` (three remarks),
`ComposerSurface`, `NewSessionSheetViewModel`, `GovernedRunHost` (the drain's `seams` doc,
`OpenReadOnlySessionAsync`, `ReadOnlyTriage`). **Declined, with reason:** the duplicated
`await prompt` / dispose-and-drain tail (two helpers would touch the write path's lines — the
author's constraint outweighs −7); `ReadOnlyTriage` kept (`RunTriage.For`'s skip path stewards
seams; `RunTriage.cs` is outside the lane); `RecordSessionNew` and `OpenReadOnlySessionAsync` kept
(two callers; the test's oracle). Net applied: ≈ −140 lines against the pass's −150 possible.

## Defect class (for the conductor to allocate at the join — the register was leased by SH-1 for the whole of this run)

**DC-nnn — A security control written for one shape is applied to every shape, and refuses work the
control cannot protect** (Ruling 73 (c)). Shape: a control derived for a shape with a risk (a lane
that can write → a lease so the seam monitor can discriminate) is applied by the gate to every
instance of the broader type (every send), the shape without the risk included; the refusal is
correct by the control's letter and protects nothing. Signature: a refusal whose remedy names a
resource the operation does not use (*"reference the files this run may write"* on a question); a
doc line *"no X means no run"* where X matters for a subset; a test asserting the refusal on the
shape without the risk. Instance (2026-09-11): C17's lease gate on every send — `ComposerSendGate.Send`
called `LeaseDerivation.Derive` for a free-form Message, `Lease`'s constructor threw, the surface
read *"no write scope could be derived… Reference the files or directories this run may write"*.
Control: the Security lens's rule on every finding — *name the shape the control protects; a shape
without the risk is exempt by construction* — and the test pair (`AMessageWithNoMentionDerivesNoLeaseAndIsNotRefused`
red on `main`; `AGoalBlockWithADerivedScopeStillTakesTheLeaseGateUnchanged` green throughout): a
control that fires on both shapes is the signature. Status: `controlled` by the pair; the lens rule
is prose until the persona audit gains the check (a finding for the pack).

## Not built (residual)

- **The composer's TIER / FAN_OUT_CAP / BUDGET fields stay** (Rulings 56/63's supersessions). Tier
  has no derivation rule until CV-2's `Project()` lands §A9 (P-D1's fourteen inputs — "no rule
  exists today"); the effective fan-out cap is `min(cap(tier), ceiling)` and depends on it; the
  budget alone could read the session's cap, but the binder that would carry `BudgetCap` into
  `ComposerSendContext` is CV-1's row. Removing one of three leaves a half-state; left whole, said so.
- **MCP servers from the repository's settings** (`.mcp.json`, `settingSources`): the frame sends
  `mcpServers: []`; what the adapter loads from settings is the P-D5 spike's question and
  `LaneSessionOptions` stays two members by design (Ruling 71; ADR-0035's `settings` belt is CV-3's).
- **A sub-agent's tool pool** is Inferred to be the parent's; `Agent`/`Task`/`Workflow`/`RemoteTrigger`
  are pinned off rather than relied on.
- **The read-only branch end to end** (session at the root → prompt → result) runs in no CI test
  (no engine); its parts are unit-tested (claim 10, 10b) and the whole is the attended run's.
- **Ruling 75's other half is CV-1's, recorded as a deviation here:** a goal block whose tier,
  fan-out cap or budget is blank is still refused with the contract's own field sentences
  (`TheComposerRendersItsFieldLevelErrorsTests` asserts exactly four) — Rulings 56/63 move those
  three off the per-prompt form in CV-1; and the demotion to a Message is *said* on the status line
  here (*sent as a message — read-only*) while SC9's spoken announcement is CV-1's.
- **Testing Strategy deviations, recorded:** T2 — the shape's whitespace domain is enumerated (six
  rows), not property-tested (the domain is "blank or not", two values); T9/T14 — the compiled
  block's budget line is asserted by contains / does-not-contain, not a golden of the whole block
  (`TheComposerIsOneValidationMechanismTests` keeps the block's structure; the line is the change).
- **A CV-2 red, by name:** Ruling 66's template control — *a template's fixed prose never widens a
  lease while a field value still does* — is vacuous until a template can be write-shaped
  (`structure_source: template`); `ATemplateBodyMentionCannotWidenALeaseBecauseATemplateSendsReadOnly`
  proves the read-only half; CV-2 re-instates the other on `Project()`.
- **Ruling 71's governed-lane pin has the same PowerShell gap** (`GovernedLaneSession = ["Bash"]`
  names one shell; the binary ships two). Not this track's to change — Ruling 71 names `["Bash"]`
  and the F5 exit run recorded it — a finding for the conductor and the Owner.
- **Read-only turns are invisible to the fleet map and the leaderboard** (no episode); the named
  upgrade is a `turn.read-only` event on the run's own stream.
- **`TaskClassVocabulary.Offered`** (Core-owned) does not carry `free-form` and its `RequiredLabel`
  / `NoDefaultRule` copy still says *"no default"*; the sheet prepends the row and no longer appends
  the rule — a seam request to Core / CV-1 to move the row into the vocabulary and retire the copy.
- **The register's tail** (`docs/lessons/defect-classes.md:6584`) carries a stray *"## 5. What this
  note does not decide"* section from a coordination note — a finding for the conductor at the join.

## Gates (measured on this tree, `lane/conversation-cv0`, 2026-09-12)

| Gate | Command | Result |
|---|---|---|
| Build, warnings as errors | `dotnet build AiDe.sln -p:TreatWarningsAsErrors=true` (Core, App, both test projects, the probes) | `Build succeeded. 0 Warning(s) 0 Error(s)` |
| Core tests | `dotnet test tests/AiDe.Core.Tests` | **2278 / 2278** passed (floor 2256) |
| App tests | `dotnet test tests/AiDe.App.Tests` | **695 / 695** passed (floor 639) |
| `verify-test-run.py` (CHECK only; `--update` never run; `tools/expected-test-counts.json` not edited) | `python tools/verify-test-run.py` | `OK — 2973 tests executed across 2 project(s), every project met its baseline` |
| `tools/verify-*.py` (bare) | every gate, in a loop | all `OK` except `verify-derived-views` / `verify-site-figures` (derived artefacts regenerated at close by `regenerate-derived.py`) and `verify-stranded-audit` (two *other* worktrees — `ai-de-conductor-addendum-c`, `ai-de-proposal-code-atlas` — hold uncommitted audit-log lines; a finding for the conductor, not this tree's) |
| `verify-defect-register.py`, `verify-id-allocators.py` | after the `DC-nnn` append | `OK` (the placeholder is allocated by the conductor at the join) |

The counts above are from the run after the last edit (the reviewers' conditions applied); the
closing audit entry records the same run.
