#!/bin/sh
# usage: run_one.sh <k>  - one full Core+App run, appends one line to runs.txt
k=$1
R=/c/Projects/ai-de-wl-loadflake
O=$R/loadflake-runs
cd $R || exit 1
s=$(date -u +%H:%M:%SZ)
dotnet test tests/AiDe.Core.Tests --no-build --logger "trx;LogFileName=core-$k.trx" --results-directory C:/Projects/ai-de-wl-loadflake/loadflake-runs > $O/core-$k.txt 2>&1
dotnet test tests/AiDe.App.Tests --no-build --logger "trx;LogFileName=app-$k.trx" --results-directory C:/Projects/ai-de-wl-loadflake/loadflake-runs > $O/app-$k.txt 2>&1
e=$(date -u +%H:%M:%SZ)
c=$(tail -5 $O/core-$k.txt | grep -E "Passed:|Failed:" | tr -s ' ' | tr '\n' ' ')
a=$(tail -5 $O/app-$k.txt | grep -E "Passed:|Failed:" | tr -s ' ' | tr '\n' ' ')
echo "$k  $s  $e  core: $c  app: $a" >> $R/docs/proof/workbench-loops-support/loadflake/runs.txt
tail -1 $R/docs/proof/workbench-loops-support/loadflake/runs.txt
