#!/bin/sh
# runs each listed test alone 10 times, serially; prints pass/fail counts per test
R=/c/Projects/ai-de-wl-loadflake
O=$R/loadflake-runs
cd $R || exit 1
for m in CtrlDigit_WithFocusInsideTheComposerOrTheCanvasPage_SwitchesThePerspective \
 TheFloatingNotebook_RedocksToTheCenter_WithItsThreadDraftAndAttemptIntact_SayingNoHide_AndFocusInIt \
 ARestoredRowsEvents_AreReachedFromTheRowByKeyboard_AndExposedToUiAutomation \
 AfterStop_TheTurnIsStopRequested_AndTheQueuedTurnStaysQueuedBehindIt \
 CtrlEnter_OnAFileArtifact_RequestsRevealInGraph; do
  f=0
  for i in 1 2 3 4 5 6 7 8 9 10; do
    dotnet test tests/AiDe.App.Tests --no-build --filter "FullyQualifiedName~$m" > $O/alone-$m-$i.txt 2>&1
    rc=$?
    [ $rc -ne 0 ] && f=$((f+1))
  done
  echo "$m alone failures $f/10" >> docs/proof/workbench-loops-support/loadflake/alone.txt
done
