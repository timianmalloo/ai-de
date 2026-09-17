param([string]$Attempt = 'semantic-red')
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
Set-Location $root
$env:AGENT_SESSION = 'xh-p2-projection-b0d0'
$env:AGENT_NAME = 'copilot-native-notice-p2'
$evidence = Join-Path $PSScriptRoot $Attempt
[System.IO.Directory]::CreateDirectory($evidence) | Out-Null
$env:TEMP = Join-Path $root 'tests\AiDe.Core.Tests\obj\notice-run-temp'
$env:TMP = $env:TEMP
[System.IO.Directory]::CreateDirectory($env:TEMP) | Out-Null
$filter = 'FullyQualifiedName~RegistrationNoticeReliabilityTests|FullyQualifiedName~AWorktreeRegistrationIsCorrectedAndSaidSoTests|FullyQualifiedName~TrustedRegistrarTests'
$arguments = @('test', 'tests\AiDe.Core.Tests\AiDe.Core.Tests.csproj', '--no-restore',
    '--filter', $filter, '--logger', 'trx;LogFileName=native-notice.trx',
    '--results-directory', $evidence, '--verbosity', 'minimal')
$started = [DateTimeOffset]::UtcNow
Start-Transcript -Path (Join-Path $evidence 'console.txt')
Write-Host ('COMMAND: dotnet ' + ($arguments -join ' '))
& dotnet @arguments
$logicalExit = $LASTEXITCODE
Write-Host "LOGICAL_EXIT: $logicalExit"
Stop-Transcript
$ended = [DateTimeOffset]::UtcNow
[xml]$trx = Get-Content (Join-Path $evidence 'native-notice.trx') -Raw
$results = @($trx.TestRun.Results.UnitTestResult | ForEach-Object {
    [ordered]@{ name = $_.testName; outcome = $_.outcome; duration = $_.duration
        message = $_.Output.ErrorInfo.Message; output = $_.Output.StdOut }
})
$inputs = @('tests\AiDe.Core.Tests\Watcher\RegistrationNoticeReliabilityTests.cs',
    'tests\AiDe.Core.Tests\AiDe.Core.Tests.csproj', 'src\AiDe.Core\AiDe.Core.csproj',
    'Directory.Build.props', 'Directory.Packages.props')
$inputs += @(Get-ChildItem 'src\AiDe.Core\Watcher' -Filter '*.cs' -Recurse | ForEach-Object {
    [System.IO.Path]::GetRelativePath($root, $_.FullName)
})
$inputs += @('tests\AiDe.Core.Tests\bin\Debug\net10.0\AiDe.Core.Tests.dll',
    'tests\AiDe.Core.Tests\bin\Debug\net10.0\AiDe.Core.dll')
$hashes = @($inputs | Sort-Object -Unique | ForEach-Object {
    [ordered]@{ path = $_; sha256 = (Get-FileHash $_ -Algorithm SHA256).Hash }
})
$receipt = [ordered]@{
    classification = 'RED-UNSHIPPABLE legacy native-notice diagnostics'
    head = (& git rev-parse HEAD); command = @('dotnet') + $arguments
    started = $started.ToString('o'); ended = $ended.ToString('o')
    durationSeconds = ($ended - $started).TotalSeconds; logicalExit = $logicalExit
    total = $results.Count; failed = @($results | Where-Object outcome -eq 'Failed').Count
    passed = @($results | Where-Object outcome -eq 'Passed').Count
    results = $results; hashes = $hashes
}
[System.IO.File]::WriteAllText((Join-Path $evidence 'receipt.json'),
    ($receipt | ConvertTo-Json -Depth 12))
$results | ForEach-Object { Write-Host ($_.outcome + ' ' + $_.name) }
$failed = @($results | Where-Object outcome -eq 'Failed')
if (1 -ne $logicalExit -or 2 -ne $failed.Count -or 5 -ne @($results | Where-Object name -like '*RegistrationNoticeReliabilityTests*').Count) {
    throw 'Receipt does not contain exactly the two intended REDs and all five new cases.'
}
if (@($failed | Where-Object name -notmatch 'DrainRegistrationNotices_PublicationFails|Register_TwoHostsHave128Outstanding').Count) {
    throw 'An unrelated case failed.'
}
foreach ($failure in $failed) {
    $diagnostic = $failure.output | ConvertFrom-Json
    if ($failure.name -match 'DrainRegistrationNotices_PublicationFails' -and
        ($failure.message -notmatch 'Assert.Single' -or $diagnostic.scenario -ne 'N1' -or $diagnostic.retryCount -ne 0)) {
        throw 'N1 did not reach the semantic lost-notice assertion.'
    }
    if ($failure.name -match 'Register_TwoHostsHave128Outstanding' -and
        ($failure.message -notmatch 'Assert.Equal' -or $diagnostic.scenario -ne 'N2' -or $diagnostic.outstanding -ne 129)) {
        throw 'N2 did not reach the semantic overflow/native-mutation assertion.'
    }
}
Write-Host "EXPECTED_RED_RECEIPT: $($results.Count) executed, $($failed.Count) intended failures."
