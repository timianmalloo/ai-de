param(
    [Parameter(Mandatory)][string]$Attempt,
    [switch]$ExpectSchemaGreen,
    [switch]$NoBuild
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
Set-Location $root
$env:AGENT_SESSION = 'xh-p2-projection-b0d0'
$env:AGENT_NAME = 'copilot-p2-native-notice'
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new()
if ($Attempt -notmatch '^[a-z0-9-]+$') { throw 'Invalid evidence attempt name' }
$evidence = Join-Path $PSScriptRoot $Attempt
if (Test-Path $evidence) { throw 'Evidence already exists; never overwrite receipts' }
[System.IO.Directory]::CreateDirectory($evidence) | Out-Null
$env:TEMP = Join-Path $root 'tests\AiDe.Core.Tests\obj\notice-run-temp'
$env:TMP = $env:TEMP
[System.IO.Directory]::CreateDirectory($env:TEMP) | Out-Null
$filter = 'FullyQualifiedName~RegistrationAdmissionTests|FullyQualifiedName~RegistrationNoticeReliabilityTests|FullyQualifiedName~AWorktreeRegistrationIsCorrectedAndSaidSoTests|FullyQualifiedName~TrustedRegistrarTests|FullyQualifiedName~SqliteWatcherObservationStoreTests'
$arguments = @('test','tests\AiDe.Core.Tests\AiDe.Core.Tests.csproj','--no-restore',
    '--filter',$filter,'--logger','trx;LogFileName=native-notice.trx',
    '--results-directory',$evidence,'--verbosity','minimal')
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
[System.IO.File]::WriteAllText((Join-Path $evidence 'stdout.txt'), $stdout.GetAwaiter().GetResult())
[System.IO.File]::WriteAllText((Join-Path $evidence 'stderr.txt'), $stderr.GetAwaiter().GetResult())
$process.Dispose()
$trxPath = Join-Path $evidence 'native-notice.trx'
if (!(Test-Path $trxPath)) { throw "No TRX; actual exit $exitCode, streams retained in $evidence" }
[xml]$trx = Get-Content $trxPath -Raw
$results = @($trx.TestRun.Results.UnitTestResult | ForEach-Object {
    [ordered]@{ name=$_.testName; outcome=$_.outcome; message=$_.Output.ErrorInfo.Message }
})
$inputs = @('tests\AiDe.Core.Tests\Watcher\RegistrationAdmissionTests.cs',
    'tests\AiDe.Core.Tests\Watcher\RegistrationNoticeReliabilityTests.cs',
    'tests\AiDe.Core.Tests\AiDe.Core.Tests.csproj','src\AiDe.Core\AiDe.Core.csproj',
    'Directory.Build.props','Directory.Packages.props',
    'tests\AiDe.Core.Tests\bin\Debug\net10.0\AiDe.Core.Tests.dll',
    'tests\AiDe.Core.Tests\bin\Debug\net10.0\AiDe.Core.dll')
$inputs += @(Get-ChildItem src\AiDe.Core\Watcher -Filter '*.cs' | ForEach-Object FullName)
$inputs += @($trxPath, (Join-Path $evidence 'stdout.txt'), (Join-Path $evidence 'stderr.txt'), $PSCommandPath)
$hashes = @($inputs | ForEach-Object {
    [ordered]@{ path=[System.IO.Path]::GetRelativePath($root,(Resolve-Path $_).Path);
        sha256=(Get-FileHash $_ -Algorithm SHA256).Hash }
})
$failures = @($results | Where-Object outcome -eq 'Failed')
$schema = @($results | Where-Object name -like '*RegistrationAdmissionTests*')
$receipt = [ordered]@{
    classification='PARTIAL DDL evidence; native N1/N2 remain RED'
    head=(& git rev-parse HEAD); command=@('dotnet')+$arguments
    started=$started.ToString('o'); ended=$ended.ToString('o')
    durationSeconds=($ended-$started).TotalSeconds; actualExit=$exitCode
    total=$results.Count; passed=@($results | Where-Object outcome -eq 'Passed').Count
    failed=$failures.Count; schemaTotal=$schema.Count
    schemaPassed=@($schema | Where-Object outcome -eq 'Passed').Count
    results=$results; hashes=$hashes
}
[System.IO.File]::WriteAllText((Join-Path $evidence 'receipt.json'),($receipt | ConvertTo-Json -Depth 8))
Write-Host "ACTUAL: exit=$exitCode total=$($receipt.total) passed=$($receipt.passed) failed=$($receipt.failed) schema=$($receipt.schemaPassed)/$($schema.Count)"
if ($schema.Count -lt 1 -or $exitCode -ne 1) { throw 'Missing schema cases or expected native RED exit' }
foreach ($name in @('DrainRegistrationNotices_PublicationFails','Register_TwoHostsHave128Outstanding')) {
    if (@($failures | Where-Object name -match $name).Count -ne 1) { throw "Missing native RED: $name" }
}
if ($ExpectSchemaGreen -and ($failures.Count -ne 2 -or $receipt.schemaPassed -ne $schema.Count)) {
    $failures | ForEach-Object { Write-Host "$($_.name): $($_.message)" }
    throw 'Structural GREEN not achieved or unrelated regression'
}
if (!$ExpectSchemaGreen -and $receipt.schemaPassed -eq $schema.Count) { throw 'Structural RED not observed' }
Write-Host 'Receipt verified; this is NOT a native admission/delivery GREEN.'
