param(
    [Parameter(Mandatory)][ValidateSet('baseline', 'diagnostic', 'qualified-baseline', 'candidate', 'final')][string]$Phase
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
Set-Location $root
$env:AGENT_SESSION = 'xh-p2-projection-b0d0'
$env:AGENT_NAME = 'copilot-p2-schema-repair'
$destination = Join-Path $PSScriptRoot "s123-$Phase"
if (Test-Path $destination) { throw 'Never overwrite evidence.' }
[System.IO.Directory]::CreateDirectory($destination) | Out-Null
$inputs = @(
    'src\AiDe.Core\Watcher\SqliteWatcherObservationStore.RegistrationNotices.cs',
    'tests\AiDe.Core.Tests\Watcher\RegistrationAdmissionTests.cs',
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
$filter = 'FullyQualifiedName~RegistrationAdmissionTests|FullyQualifiedName~RegistrationNoticeReliabilityTests|FullyQualifiedName~AWorktreeRegistrationIsCorrectedAndSaidSoTests|FullyQualifiedName~TrustedRegistrarTests|FullyQualifiedName~SqliteWatcherObservationStoreTests'
$arguments = @('test','tests\AiDe.Core.Tests\AiDe.Core.Tests.csproj','--no-restore',
    '-p:UseSharedCompilation=false','-m:1','-nodeReuse:false','--filter',$filter,
    '--logger','trx;LogFileName=notice.trx','--results-directory',$destination,'--verbosity','minimal')
if ('final' -eq $Phase) { $arguments += '--no-build' }
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
$trxPath = Join-Path $destination 'notice.trx'
if (!(Test-Path $trxPath)) { throw "No executed tests; exit=$exitCode. Read $destination\stdout.txt" }
[xml]$trx = Get-Content $trxPath -Raw
$results = @($trx.TestRun.Results.UnitTestResult | ForEach-Object {
    [ordered]@{ name=$_.testName; outcome=$_.outcome; message=$_.Output.ErrorInfo.Message }
})
$failed = @($results | Where-Object outcome -eq 'Failed')
$receipt = [ordered]@{
    phase=$Phase; head=(& git rev-parse HEAD); command=@('dotnet')+$arguments;
    startedUtc=$started.ToString('o'); endedUtc=$ended.ToString('o');
    durationSeconds=($ended-$started).TotalSeconds; actualExit=$exitCode;
    total=$results.Count; passed=@($results | Where-Object outcome -eq 'Passed').Count;
    failures=$failed; beforePins=$before; afterPins=(Get-Pins);
    evidencePins=@(Get-ChildItem $destination -File | ForEach-Object {
        [ordered]@{ file=$_.Name; sha256=(Get-FileHash $_.FullName -Algorithm SHA256).Hash }
    })
}
[System.IO.File]::WriteAllText((Join-Path $destination 'receipt.json'), ($receipt | ConvertTo-Json -Depth 8))
Write-Host "ACTUAL $Phase exit=$exitCode total=$($results.Count) passed=$($receipt.passed) failed=$($failed.Count)"
foreach ($failure in $failed) { Write-Host "$($failure.name): $($failure.message)" }
if ($exitCode -ne 1 -or $results.Count -lt 105) { throw 'Unexpected exit or missing selection.' }
foreach ($name in @('DrainRegistrationNotices_PublicationFails','Register_TwoHostsHave128Outstanding')) {
    if (@($failed | Where-Object name -Match $name).Count -ne 1) { throw "Missing original RED $name" }
}
$nulFailures = @($failed | Where-Object name -Match 'HexWithNulAndOversizedSuffix')
if ($Phase -in @('baseline', 'diagnostic', 'qualified-baseline')) {
    if ($nulFailures.Count -ne 5) { throw 'Expected five NUL assertion REDs.' }
    if ('qualified-baseline' -eq $Phase -and $failed.Count -ne 7) { throw 'Unexpected qualified baseline failure.' }
    foreach ($failure in $nulFailures) {
        if ($failure.message -notmatch 'No exception was thrown') { throw 'Setup failure is not NUL evidence.' }
    }
} elseif ($failed.Count -ne 2) { throw 'Candidate schema has an unexpected failure.' }
Write-Host 'Receipt verified; native admission/delivery remains RED.'
