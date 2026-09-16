param([Parameter(Mandatory)][string]$Run)
$ErrorActionPreference = 'Stop'
$env:AGENT_SESSION = 'xh-p2-projection-b0d0'
$env:AGENT_NAME = 'copilot-p22-evidence'
$env:PYTHONIOENCODING = 'utf-8'
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
Set-Location (Resolve-Path (Join-Path $PSScriptRoot '..\..\..'))
$directory = Join-Path (Get-Location) "docs\proofs\p22-cache-evidence\$Run"
if (Test-Path $directory) { throw 'Evidence runs are append-only. Choose a new Run.' }
New-Item -ItemType Directory -Path $directory | Out-Null
$project = 'tests\AiDe.Core.Tests\AiDe.Core.Tests.csproj'
$selected = 'FullyQualifiedName~Diagnostic_InvalidStorageOrSemanticShape_Rejects|FullyQualifiedName~Diagnostic_OneHundredfoldHistory_IndexesActualInsertionLookups|FullyQualifiedName~Receipt_NullDiagnosticsAndPositiveGeneration_AreLegalOnlyOnSemanticRows'
$runs = [System.Collections.Generic.List[object]]::new()
$valid = $true
$oldMode = $env:P22_BOUNDARY_EVIDENCE

function Invoke-Receipt([string]$Name, [string]$Filter, [bool]$Build) {
    $arguments = @('test', $project, '--no-restore', '--filter', $Filter,
        '--logger', "trx;LogFileName=$Name.trx", '--results-directory', $directory, '--verbosity', 'quiet')
    if (!$Build) { $arguments += '--no-build' }
    $clock = [Diagnostics.Stopwatch]::StartNew()
    & dotnet @arguments
    $exit = $LASTEXITCODE
    $clock.Stop()
    $path = Join-Path $directory "$Name.trx"
    if (!(Test-Path $path)) { throw "$Name produced no TRX (exit=$exit)" }
    [xml]$trx = Get-Content -Raw $path
    $results = @($trx.TestRun.Results.UnitTestResult)
    $runs.Add([ordered]@{
        name = $Name; arguments = $arguments; logicalExit = $exit
        durationSeconds = $clock.Elapsed.TotalSeconds
        counters = $trx.TestRun.ResultSummary.Counters.OuterXml
        tests = @($results | ForEach-Object {
            [ordered]@{ name = $_.testName; outcome = $_.outcome
                message = [string]$_.Output.ErrorInfo.Message
                stack = [string]$_.Output.ErrorInfo.StackTrace
                stdout = [string]$_.Output.StdOut }
        })
    })
    return [pscustomobject]@{ Exit = $exit; Results = $results }
}

try {
    $env:P22_BOUNDARY_EVIDENCE = 'controls'
    $control = Invoke-Receipt 'positive-control' $selected $true
    $valid = $valid -and $control.Exit -eq 0 -and $control.Results.Count -eq 23
    $env:P22_BOUNDARY_EVIDENCE = 'faults'
    $faults = Invoke-Receipt 'targeted-faults' $selected $false
    $valid = $valid -and $faults.Exit -eq 1 -and $faults.Results.Count -eq 23
    foreach ($test in $faults.Results) {
        $message = [string]$test.Output.ErrorInfo.Message
        $stdout = [string]$test.Output.StdOut
        $focal = if ($test.testName -like '*InvalidStorageOrSemanticShape*') {
            $message -match 'Assert.Throws' -and $message -match 'No exception was thrown'
        } elseif ($test.testName -like '*IndexesActualInsertionLookups*') {
            $message -match 'Assert.Contains' -and $message -match 'SEARCH' -and
            $stdout -match 'diagnostics=10; predicate-visits=1' -and
            $stdout -match 'diagnostics=1000; predicate-visits=1' -and $stdout -match 'SCAN '
        } else {
            $message -match "SQLite Error 19" -and $message -match 'CHECK constraint failed: session_generation'
        }
        if ($test.outcome -ne 'Failed' -or !$focal -or $stdout -notmatch 'P22_MUTATION ') {
            $valid = $false
            Write-Warning "Not a focal kill: $($test.testName): $message"
        }
    }
    $env:P22_BOUNDARY_EVIDENCE = 'controls'
    $restored = Invoke-Receipt 'restored-green' $selected $false
    $valid = $valid -and $restored.Exit -eq 0 -and $restored.Results.Count -eq 23
    [xml]$previous = Get-Content -Raw 'docs\proofs\p22-cache-evidence\p22-s1s6-first-green.trx'
    $methods = @($previous.TestRun.TestDefinitions.UnitTest.TestMethod | ForEach-Object {
        "FullyQualifiedName=$($_.className.Split(',')[0]).$($_.name)"
    } | Sort-Object -Unique)
    $candidate = Invoke-Receipt 'candidate-202' ($methods -join '|') $false
    $valid = $valid -and $candidate.Exit -eq 0 -and $candidate.Results.Count -eq 202
    $expectedNames = @($control.Results.testName | Sort-Object)
    foreach ($result in @($faults, $restored)) {
        if (Compare-Object $expectedNames @($result.Results.testName | Sort-Object)) {
            $valid = $false
        }
    }
} catch {
    $valid = $false
    throw
} finally {
    $env:P22_BOUNDARY_EVIDENCE = $oldMode
    $paths = @(
        'src\AiDe.Core\Watcher\SqliteWatcherObservationStore.Coordination.cs',
        'src\AiDe.Core\Watcher\SqliteWatcherObservationStore.cs',
        'src\AiDe.Core\AiDe.Core.csproj',
        $project, 'Directory.Packages.props',
        'tests\AiDe.Core.Tests\Watcher\CoordinationProjectionBoundaryTests.cs',
        'tests\AiDe.Core.Tests\Watcher\CoordinationProjectionTests.cs',
        'tests\AiDe.Core.Tests\Watcher\CoordinationReliabilityTests.cs',
        'tests\AiDe.Core.Tests\Watcher\CoordinationProjectionEvidence.cs',
        'tests\AiDe.Core.Tests\Watcher\Run-P22Evidence.ps1',
        'tests\AiDe.Core.Tests\bin\Debug\net10.0\AiDe.Core.Tests.dll',
        'tests\AiDe.Core.Tests\bin\Debug\net10.0\AiDe.Core.dll',
        'tests\AiDe.Core.Tests\bin\Debug\net10.0\Microsoft.Data.Sqlite.dll'
    )
    $receipt = [ordered]@{
        baseCommit = (& git rev-parse HEAD)
        chronologicalRedClaimed = $false
        allFocalOraclesSatisfied = $valid
        pins = @($paths | ForEach-Object { @{ path = $_; sha256 = (Get-FileHash $_ -Algorithm SHA256).Hash } })
        runs = $runs
    }
    $receipt | ConvertTo-Json -Depth 12 | Set-Content (Join-Path $directory 'receipt.json') -Encoding utf8
}
if (!$valid) { throw 'P22 evidence incomplete: inspect per-case TRX and receipt.json.' }
Write-Host "P22 evidence: 23/23 focal kills; positive 23/23; restored 23/23; candidate 202/202."
