param([switch]$ReceiptOnly, [string]$RunName = 'recoveryB-finite-restored')
$ErrorActionPreference = 'Stop'
Set-Location 'C:\Projects\ai-de-feature-xh-p2-projection'
$folder = 'docs\proofs\p24-recovery-evidence'
function Read-Results([string]$path) {
    [xml]$document = Get-Content $path
    if (!$document.TestRun.ResultSummary.Counters) { throw "Missing counters: $path" }
    return $document
}
function Names($document) {
    $names = [System.Collections.Generic.Dictionary[string,int]]::new([StringComparer]::Ordinal)
    foreach ($item in $document.TestRun.Results.UnitTestResult) {
        if (!$names.ContainsKey($item.testName)) { $names[$item.testName] = 0 }
        $names[$item.testName]++
    }
    return ,$names
}
$prior = Read-Results "$folder\recoveryB-union.trx"
if (!$ReceiptOnly) {
    Start-Transcript "$folder\$RunName-full-output.txt"
    $classes = $prior.TestRun.TestDefinitions.UnitTest.TestMethod.className | Sort-Object -Unique
    if (!$classes.Count) { throw 'No prior test classes' }
    $filter = ($classes | ForEach-Object { 'FullyQualifiedName~' + ($_ -split ',')[0] }) -join '|'
    dotnet test tests\AiDe.Core.Tests\AiDe.Core.Tests.csproj --no-restore --filter $filter --logger "trx;LogFileName=$RunName.trx" --results-directory $folder --verbosity normal
    $code = $LASTEXITCODE
    Stop-Transcript
    if ($code -ne 0) { throw "Union failed: $code" }
}
$current = Read-Results "$folder\$RunName.trx"
$oldNames = Names $prior
$newNames = Names $current
$missing = @($oldNames.Keys | Where-Object { !$newNames.ContainsKey($_) -or $newNames[$_] -lt $oldNames[$_] })
if ($missing.Count) { throw "Prior result occurrences missing: $($missing -join ';')" }
if ([int]$current.TestRun.ResultSummary.Counters.failed -ne 0) { throw 'Failed union results' }
$historical = @(
    "$folder\p24-prior-304-green.trx",
    'docs\proofs\p24-feed-evidence\p24-two-blockers-restored-union-green.trx',
    'docs\proofs\p24-feed-evidence\p24-two-blockers-recovery-still-red.trx',
    'docs\proofs\p24-feed-evidence\p24-mcp-green.trx',
    "$folder\recoveryB-union.trx",
    "$folder\$RunName.trx"
)
$counts = foreach ($path in $historical) {
    $doc = Read-Results $path
    $ordinal = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $ignoreCase = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($result in $doc.TestRun.Results.UnitTestResult) {
        [void]$ordinal.Add($result.testName)
        [void]$ignoreCase.Add($result.testName)
    }
    [ordered]@{ path=$path; counters=$doc.TestRun.ResultSummary.Counters.OuterXml
        occurrences=@($doc.TestRun.Results.UnitTestResult).Count
        ordinalDistinct=$ordinal.Count; ordinalIgnoreCaseDistinct=$ignoreCase.Count }
}
$paths = @(
    'src\AiDe.Core\Watcher\CoordinationRecovery.cs',
    'src\AiDe.Core\Watcher\CoordinationProjection.cs',
    'src\AiDe.Core\Watcher\CoordinationContractLog.cs',
    'src\AiDe.Core\Watcher\SqliteWatcherObservationStore.Coordination.cs',
    'tests\AiDe.Core.Tests\Watcher\CoordinationRecoveryTests.cs',
    'tests\AiDe.Core.Tests\Watcher\CoordinationRecoveryBoundaryTests.cs',
    'src\AiDe.Core\AiDe.Core.csproj',
    'tests\AiDe.Core.Tests\AiDe.Core.Tests.csproj',
    'src\AiDe.Core\bin\Debug\net10.0\AiDe.Core.dll',
    'tests\AiDe.Core.Tests\bin\Debug\net10.0\AiDe.Core.Tests.dll'
)
$paths += Get-ChildItem $folder -File -Filter '*.trx' | ForEach-Object { "$folder\$($_.Name)" }
$pins = foreach ($path in $paths) { [ordered]@{ path=$path; sha256=(Get-FileHash $path -Algorithm SHA256).Hash } }
$receipt = [ordered]@{
    base='e5b7949324c449999d1e96f290d28f188a128af8'
    status='partial-author-evidence-independent-review-blocked'
    priorOccurrencesMissing=$missing.Count; counts=$counts; pins=$pins
    comparatorCorrection='Occurrence multisets use Ordinal; unique name counts explicitly name their comparer. Old TRX and JSON receipts are unchanged.'
}
$receipt | ConvertTo-Json -Depth 8 | Set-Content "$folder\$RunName-receipt.json" -Encoding utf8
$counts | ForEach-Object { "$($_.path): occurrences=$($_.occurrences), ordinal=$($_.ordinalDistinct), ignoreCase=$($_.ordinalIgnoreCaseDistinct)" }
"Prior occurrence multiset missing: $($missing.Count)"
