#!/bin/sh
# G4: ConsoleSplit CollapsingTheLeft alone, 10 times; one result line per run.
D=docs/proof/workbench-loops-support/focus-oracle-route
for i in 1 2 3 4 5 6 7 8 9 10; do
  dotnet test tests/AiDe.App.Tests -nologo --filter "FullyQualifiedName~CollapsingTheLeftHoldingTheSession" > $D/g4-alone-run$i.txt 2>&1
  echo "run $i exit=$? $(grep -E 'Passed!|Failed!' $D/g4-alone-run$i.txt) $(grep -E 'focus after' $D/g4-alone-run$i.txt)"
done
