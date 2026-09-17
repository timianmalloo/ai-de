param(
    [Parameter(Mandatory)][string]$Phase,
    [string]$Filter = 'FullyQualifiedName~RegistrationAdmissionTests|FullyQualifiedName~RegistrationNoticeReliabilityTests|FullyQualifiedName~AWorktreeRegistrationIsCorrectedAndSaidSoTests|FullyQualifiedName~TrustedRegistrarTests|FullyQualifiedName~SqliteWatcherObservationStoreTests|FullyQualifiedName~NativeAdmissionRuntimeTests',
    [int]$ExpectedFailures = 2,
    [switch]$NoBuild
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
Set-Location $root
$destination = Join-Path $PSScriptRoot "runtime-$Phase"
if (Test-Path $destination) { throw 'Never overwrite evidence.' }
[System.IO.Directory]::CreateDirectory($destination) | Out-Null
$inputs = @(
    'tests\AiDe.Core.Tests\AiDe.Core.Tests.csproj', 'src\AiDe.Core\AiDe.Core.csproj',
    'src\AiDe.Mcp\AiDe.Mcp.csproj', 'Directory.Build.props', 'Directory.Packages.props',
    'tests\AiDe.Core.Tests\bin\Debug\net10.0\AiDe.Core.Tests.dll',
    'tests\AiDe.Core.Tests\bin\Debug\net10.0\AiDe.Core.dll'
)
$inputs += @(Get-ChildItem src\AiDe.Core\Watcher -Filter '*.cs' | ForEach-Object FullName)
$inputs += @(Get-ChildItem tests\AiDe.Core.Tests\Watcher -Filter '*.cs' | ForEach-Object FullName)
$inputs += $PSCommandPath
function Get-Pins {
    @($inputs | Sort-Object -Unique | ForEach-Object {
        [ordered]@{ path=[System.IO.Path]::GetRelativePath($root,(Resolve-Path $_).Path);
            sha256=(Get-FileHash $_ -Algorithm SHA256).Hash }
    })
}
$before = Get-Pins
[System.IO.File]::WriteAllText((Join-Path $destination 'before-pins.json'), ($before | ConvertTo-Json -Depth 5))
$arguments = @('test','tests\AiDe.Core.Tests\AiDe.Core.Tests.csproj','--no-restore',
    '-p:UseSharedCompilation=false','-m:1','-nodeReuse:false','--filter',$Filter,
    '--logger','trx;LogFileName=notice.trx','--results-directory',$destination,'--verbosity','minimal')
if ($NoBuild) { $arguments += '--no-build' }
$start = [System.Diagnostics.ProcessStartInfo]::new('dotnet')
$start.WorkingDirectory = $root
$start.UseShellExecute = $false
$start.RedirectStandardOutput = $true
$start.RedirectStandardError = $true
foreach ($argument in $arguments) { $start.ArgumentList.Add($argument) }
$started = [DateTimeOffset]::UtcNow
$process = [System.Diagnostics.Process]::Start($start)
$stdout = $process.StandardOutput.ReadToEndAsync()
$stderr = $process.StandardError.ReadToEndAsync()
$process.WaitForExit()
$exitCode = $process.ExitCode
$ended = [DateTimeOffset]::UtcNow
[System.IO.File]::WriteAllText((Join-Path $destination 'stdout.txt'), $stdout.GetAwaiter().GetResult())
[System.IO.File]::WriteAllText((Join-Path $destination 'stderr.txt'), $stderr.GetAwaiter().GetResult())
$process.Dispose()
[System.IO.File]::WriteAllText((Join-Path $destination 'after-pins.json'), ((Get-Pins) | ConvertTo-Json -Depth 5))
$trxPath = Join-Path $destination 'notice.trx'
if (!(Test-Path $trxPath)) { throw "No executed tests; exit=$exitCode. Read $destination\stdout.txt" }
[xml]$trx = Get-Content $trxPath -Raw
$results = @($trx.TestRun.Results.UnitTestResult)
$failed = @($results | Where-Object outcome -eq 'Failed')
$receipt = [ordered]@{
    phase=$Phase; head=(& git rev-parse HEAD); command=@('dotnet')+$arguments;
    startedUtc=$started.ToString('o'); endedUtc=$ended.ToString('o');
    durationSeconds=($ended-$started).TotalSeconds; actualExit=$exitCode;
    total=$results.Count; passed=@($results | Where-Object outcome -eq 'Passed').Count;
    failures=@($failed | ForEach-Object { @{name=$_.testName;message=$_.Output.ErrorInfo.Message} });
    evidencePins=@(Get-ChildItem $destination -File | ForEach-Object {
        @{file=$_.Name;sha256=(Get-FileHash $_.FullName -Algorithm SHA256).Hash}
    })
}
[System.IO.File]::WriteAllText((Join-Path $destination 'receipt.json'), ($receipt | ConvertTo-Json -Depth 8))
Write-Host "ACTUAL $Phase exit=$exitCode total=$($results.Count) passed=$($receipt.passed) failed=$($failed.Count)"
foreach ($failure in $failed) { Write-Host "$($failure.testName): $($failure.Output.ErrorInfo.Message)" }
if (0 -eq $results.Count -or $failed.Count -ne $ExpectedFailures) { throw 'Unexpected executed result shape.' }
if ((0 -eq $ExpectedFailures -and 0 -ne $exitCode) -or (0 -lt $ExpectedFailures -and 1 -ne $exitCode)) {
    throw 'Unexpected logical test exit.'
}
