#!/bin/sh
# One full Core+App run k, bracketed by read-only left-button samples. Usage: run-one.sh <k>
k=$1
T=C:/Projects/ai-de-wl-flake-remeasure
D=$T/docs/proof/workbench-loops-support/flake-remeasure
M=$T/docs/proof/workbench-loops-support/deadholder-repro/mouse-state.ps1
cd $T || exit 9
s=$(powershell.exe -NoProfile -ExecutionPolicy Bypass -File $M)
echo "$k start  $s" >> $D/mouse.txt
case "$s" in *0x8000*) echo "BUTTON DOWN at start"; exit 3;; esac
t0=$(date -u +%H:%M:%SZ)
dotnet test tests/AiDe.Core.Tests --no-build --logger "trx;LogFileName=core-$k.trx" --results-directory $T/flake-runs > $T/flake-runs/core-$k.txt 2>&1
dotnet test tests/AiDe.App.Tests --no-build --logger "trx;LogFileName=app-$k.trx" --results-directory $T/flake-runs > $T/flake-runs/app-$k.txt 2>&1
t1=$(date -u +%H:%M:%SZ)
e=$(powershell.exe -NoProfile -ExecutionPolicy Bypass -File $M)
echo "$k end    $e" >> $D/mouse.txt
c=$(grep -E "^(Passed|Failed)!" $T/flake-runs/core-$k.txt | tail -1)
a=$(grep -E "^(Passed|Failed)!" $T/flake-runs/app-$k.txt | tail -1)
echo "$k  $t0  $t1  core: $c  app: $a" >> $D/runs.txt
echo "$k done: $c | $a"
