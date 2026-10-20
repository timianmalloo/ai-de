#!/bin/sh
# M3: each test alone x10, bracketed by read-only left-button samples.
T=C:/Projects/ai-de-wl-flake-remeasure
D=$T/docs/proof/workbench-loops-support/flake-remeasure
M=$T/docs/proof/workbench-loops-support/deadholder-repro/mouse-state.ps1
cd $T || exit 9
echo "M3 start  $(powershell.exe -NoProfile -ExecutionPolicy Bypass -File $M)" >> $D/mouse.txt
: > $D/alone.txt
while read proj m; do
  f=0; seen=0
  for i in 1 2 3 4 5 6 7 8 9 10; do
    o=$T/flake-runs/alone-$m-$i.txt
    dotnet test tests/$proj --no-build --filter "FullyQualifiedName~$m" > $o 2>&1
    if grep -qE "^Failed!" $o; then f=$((f+1)); fi
    if grep -qE "^(Passed|Failed)!" $o; then seen=$((seen+1)); fi
  done
  echo "$m alone failures $f/10 (summary lines seen $seen/10)" >> $D/alone.txt
done <<LIST
AiDe.App.Tests ARestoredRowsEvents_AreReachedFromTheRowByKeyboard_AndExposedToUiAutomation
AiDe.App.Tests CtrlDigit_WithFocusInsideTheComposerOrTheCanvasPage_SwitchesThePerspective
AiDe.App.Tests CtrlEnter_OnAFileArtifact_RequestsRevealInGraph
AiDe.App.Tests CollapsingTheLeftHoldingTheSession_LeavesItsConsoleOpen
AiDe.App.Tests TheFloatingNotebook_RedocksToTheCenter_WithItsThreadDraftAndAttemptIntact_SayingNoHide_AndFocusInIt
AiDe.App.Tests AfterStop_TheTurnIsStopRequested_AndTheQueuedTurnStaysQueuedBehindIt
AiDe.App.Tests ADeadHolder_NeverLocksTheUserOut_TheNextOpenSucceeds
AiDe.Core.Tests ASessionWhoseChildExited_ReleasesItsHeadlessHostWhileTheOwnerLives
AiDe.App.Tests TheFactoryLanding_IsTheNotebookInTheCenter_ElseTheCentersEmptyCopy_EachWithAFocusTarget
LIST
echo "M3 end    $(powershell.exe -NoProfile -ExecutionPolicy Bypass -File $M)" >> $D/mouse.txt
