param([ValidateSet('red', 'green')][string]$Phase = 'red')
$ErrorActionPreference = 'Stop'
$env:AGENT_SESSION = 'xh-p2-projection-b0d0'
$env:AGENT_NAME = 'copilot-p2-projection'
$env:PYTHONIOENCODING = 'utf-8'
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new()
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
Set-Location $root
$folder = $PSScriptRoot
$project = 'tests\AiDe.Core.Tests\AiDe.Core.Tests.csproj'
$ns = @{ t = 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010' }

function Invoke-Recorded([string]$name, [string[]]$arguments, [int]$expected) {
    $start = [Diagnostics.ProcessStartInfo]::new('dotnet')
    $start.WorkingDirectory = $root
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    foreach ($argument in $arguments) { $start.ArgumentList.Add($argument) }
    $process = [Diagnostics.Process]::Start($start)
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    $process.WaitForExit()
    $exit = $process.ExitCode
    [IO.File]::WriteAllText("$folder\$name.stdout.txt", $stdout.GetAwaiter().GetResult())
    [IO.File]::WriteAllText("$folder\$name.stderr.txt", $stderr.GetAwaiter().GetResult())
    $pins = @{}
    $pinPaths = @(
        'src\AiDe.Core\Watcher\CoordinationRecovery.cs',
        'src\AiDe.Core\Watcher\CoordinationProjection.cs',
        'src\AiDe.Core\Watcher\CoordinationContractLog.cs',
        'tests\AiDe.Core.Tests\Watcher\CoordinationRecoveryTests.cs',
        'src\AiDe.Core\AiDe.Core.csproj', $project,
        'tests\AiDe.Core.Tests\bin\Debug\net10.0\AiDe.Core.dll',
        'tests\AiDe.Core.Tests\bin\Debug\net10.0\AiDe.Core.Tests.dll',
        '.qualification-lifecycle\src\AiDe.Core\Watcher\CoordinationRecovery.cs',
        '.qualification-lifecycle\test\AiDe.Core.dll',
        '.qualification-lifecycle\test\AiDe.Core.Tests.dll',
        "docs\proofs\p24-recovery-evidence\$name.trx",
        "docs\proofs\p24-recovery-evidence\$name.stdout.txt",
        "docs\proofs\p24-recovery-evidence\$name.stderr.txt"
    )
    foreach ($path in $pinPaths) {
        if (Test-Path $path) { $pins[$path] = (Get-FileHash $path -Algorithm SHA256).Hash }
    }
    $receipt = @{ base = 'eda167380704af1fbba62efbfea402fd0fa97104'; arguments = $arguments
        logicalExit = $exit; expectedExit = $expected; pins = $pins }
    [IO.File]::WriteAllText("$folder\$name.receipt.json", ($receipt | ConvertTo-Json -Depth 6))
    Write-Host "$name logicalExit=$exit expected=$expected"
    if ($exit -ne $expected) { throw "Unexpected runner exit: $name" }
}

function Assert-Results([string]$name, [int]$total, [int]$failed) {
    [xml]$trx = Get-Content "$folder\$name.trx"
    $counters = (Select-Xml -Xml $trx -Namespace $ns -XPath '//t:ResultSummary/t:Counters').Node
    if (!$counters -or [int]$counters.total -ne $total -or [int]$counters.executed -ne $total -or
        [int]$counters.failed -ne $failed) { throw "Unexpected result counters: $name" }
    Write-Host "$name total=$total failed=$failed"
}

$focal = 'Pump_FailsBeforeRecovery_ReportsNotRecordedRatherThanCompletedZero'
$lifecycle = 'Pump_PendingUpdate_OriginalPayloadAppliesOnceAfterObservationRegistration|FullyQualifiedName~Pump_RegisteredUpdate_PreservesNativeIdentity'
if ($Phase -eq 'red') {
    Invoke-Recorded 'lifecycle-b5-baseline' @('test', $project, '--no-restore', '--filter', "FullyQualifiedName~$focal",
        '--logger', 'trx;LogFileName=lifecycle-b5-baseline.trx', '--results-directory', $folder) 0
    Assert-Results 'lifecycle-b5-baseline' 1 0
    $scratch = Join-Path $root '.qualification-lifecycle'
    if (Test-Path $scratch) { throw 'Qualification scratch already exists; refusing overwrite' }
    try {
        [void][IO.Directory]::CreateDirectory("$scratch\src")
        Copy-Item src\AiDe.Core "$scratch\src\AiDe.Core" -Recurse
        Copy-Item Directory.Build.props,Directory.Packages.props $scratch
        Copy-Item tests\AiDe.Core.Tests\bin\Debug\net10.0 "$scratch\test" -Recurse
        $source = "$scratch\src\AiDe.Core\Watcher\CoordinationRecovery.cs"
        $text = [IO.File]::ReadAllText($source)
        $anchor = 'CoordinationRecoveryStatus Status = CoordinationRecoveryStatus.NotRecorded'
        if (($text.Split($anchor)).Count -ne 2) { throw 'Mutation anchor not unique' }
        [IO.File]::WriteAllText($source, $text.Replace($anchor,
            'CoordinationRecoveryStatus Status = CoordinationRecoveryStatus.Completed'))
        Invoke-Recorded 'lifecycle-b5-mutant-build' @('build', "$scratch\src\AiDe.Core\AiDe.Core.csproj",
            '--no-restore', '--verbosity', 'quiet') 0
        Copy-Item "$scratch\src\AiDe.Core\bin\Debug\net10.0\AiDe.Core.dll" "$scratch\test\AiDe.Core.dll"
        Invoke-Recorded 'lifecycle-b5-mutant' @('vstest', "$scratch\test\AiDe.Core.Tests.dll",
            "/TestCaseFilter:FullyQualifiedName~$focal", '/Logger:trx;LogFileName=lifecycle-b5-mutant.trx',
            "/ResultsDirectory:$folder") 1
        Assert-Results 'lifecycle-b5-mutant' 1 1
        $red = [IO.File]::ReadAllText("$folder\lifecycle-b5-mutant.trx")
        if (!$red.Contains('Expected: NotRecorded') -or !$red.Contains('Actual:   Completed')) {
            throw 'Mutation did not fail the focal status assertion'
        }
        Copy-Item tests\AiDe.Core.Tests\bin\Debug\net10.0\AiDe.Core.dll "$scratch\test\AiDe.Core.dll"
        Invoke-Recorded 'lifecycle-b5-restored' @('vstest', "$scratch\test\AiDe.Core.Tests.dll",
            "/TestCaseFilter:FullyQualifiedName~$focal", '/Logger:trx;LogFileName=lifecycle-b5-restored.trx',
            "/ResultsDirectory:$folder") 0
        Assert-Results 'lifecycle-b5-restored' 1 0
    }
    finally { if (Test-Path $scratch) { [IO.Directory]::Delete($scratch, $true) } }
    Invoke-Recorded 'lifecycle-update-red' @('test', $project, '--no-build', '--no-restore', '--filter',
        "FullyQualifiedName~$lifecycle", '--logger', 'trx;LogFileName=lifecycle-update-red.trx',
        '--results-directory', $folder) 1
    Assert-Results 'lifecycle-update-red' 2 1
}
else {
    Invoke-Recorded 'lifecycle-update-green' @('test', $project, '--no-restore', '--filter',
        "FullyQualifiedName~$lifecycle", '--logger', 'trx;LogFileName=lifecycle-update-green.trx',
        '--results-directory', $folder) 0
    Assert-Results 'lifecycle-update-green' 2 0
    [xml]$prior = Get-Content "$folder\recoveryB-finite-restored.trx"
    $classes = $prior.TestRun.TestDefinitions.UnitTest.TestMethod.className | Sort-Object -Unique
    if (!$classes.Count) { throw 'Empty prior selection' }
    $filter = ($classes | ForEach-Object { 'FullyQualifiedName~' + ($_ -split ',')[0] }) -join '|'
    Invoke-Recorded 'lifecycle-union-green' @('test', $project, '--no-build', '--no-restore', '--filter',
        $filter, '--logger', 'trx;LogFileName=lifecycle-union-green.trx', '--results-directory', $folder) 0
    Assert-Results 'lifecycle-union-green' 504 0
    [xml]$current = Get-Content "$folder\lifecycle-union-green.trx"
    $names = [Collections.Generic.Dictionary[string,int]]::new([StringComparer]::Ordinal)
    foreach ($result in $current.TestRun.Results.UnitTestResult) {
        if (!$names.ContainsKey($result.testName)) { $names[$result.testName] = 0 }
        $names[$result.testName]++
    }
    foreach ($result in $prior.TestRun.Results.UnitTestResult) {
        if (!$names.ContainsKey($result.testName) -or $names[$result.testName] -eq 0) {
            throw "Missing prior occurrence: $($result.testName)"
        }
        $names[$result.testName]--
    }
    Write-Host 'All 502 prior result occurrences retained (Ordinal multiset)'
}
