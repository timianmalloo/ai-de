param([string]$Only)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
Set-Location $root
$env:PYTHONUTF8 = '1'
$env:TEMP = Join-Path $root 'tests\AiDe.Core.Tests\obj\repair-process-files'
$env:TMP = $env:TEMP
[IO.Directory]::CreateDirectory($env:TEMP) | Out-Null
$work = Join-Path $root 'tests\AiDe.Core.Tests\obj\repair-mutants'
$source = Join-Path $root 'src\AiDe.Core'
$tests = Join-Path $root 'tests\AiDe.Core.Tests\bin\Debug\net10.0'
$variants = @(
    @{name='identity'; file='Watcher\RegistrationPublisher.cs'; filter='Publish_InvalidId'; count=2;
      edits=@(
        @('notice.NoticeId.Length != 32 || notice.NoticeId.Any(character => !char.IsAsciiHexDigitLower(character))', 'false'),
        @('$"n-{notice.NoticeId}.json"', '$"n-{NativeAdmissionCodec.Digest(System.Text.Encoding.UTF8.GetBytes(notice.NoticeId))[..32]}.json"'))},
    @{name='reserved'; file='Watcher\RegistrationPublisher.cs'; filter='Publish_ReservedLegacyFilename'; count=2;
      edits=@(
        @('NativeAdmissionCodec.ValidateCompatibilitySession(notice.SessionId);', '// isolated mutant: admit reserved compatibility IDs into a safe synthetic target'),
        @('var latestName = StandingPublisher.FileNameFor(notice.SessionId);', 'var latestName = "synthetic-reserved.json";'))},
    @{name='version'; file='Watcher\SqliteWatcherObservationStore.RegistrationNotices.cs'; filter='Complete_WrongVersionSameOwnerAndAttempt'; count=1;
      edits=@(,@('AND attempt=$attempt AND ownership_version=$version;', 'AND attempt=$attempt;'))},
    @{name='reader'; file='Watcher\NoticeAdmissionCoordinator.cs'; filter='Admit_RetainedAccountingReaderFails'; count=1;
      edits=@(,@('catch (InvalidOperationException) { throw NativeAdmissionErrors.Error(NativeAdmissionErrors.Uncertain); }',
               'catch (InvalidOperationException) { return 0; }'))},
    @{name='latest-bytes'; file='Watcher\SqliteWatcherObservationStore.RegistrationNotices.cs'; filter='Publish_NewAcceptedPendingNotice'; count=1;
      edits=@(,@('var bytes = ReadNativeDelivery(id, transaction).Bytes;', 'var bytes = notice.Bytes;'))},
    @{name='unavailable'; file='Watcher\IngestHost.cs'; filter='Publish_DefaultComposition'; count=1;
      edits=@(,@('public NativeNoticeBatch PublishNativeNotices(int maximum = 16, CancellationToken cancellationToken = default) =>
        throw NativeAdmissionErrors.Error(NativeAdmissionErrors.Unavailable);',
               'public NativeNoticeBatch PublishNativeNotices(int maximum = 16, CancellationToken cancellationToken = default) =>
        new(0, 0, 0, null, 0);'))},
    @{name='worker-read'; file='Watcher\RegistrationPublisher.cs'; filter='Publish_StoreReadFails'; count=1;
      edits=@(,@('catch (InvalidOperationException) { error = NativeAdmissionErrors.Uncertain; }',
               'catch (InvalidOperationException) { error = null; }'))}
)
function Invoke-Captured([string]$exe, [string[]]$arguments, [string]$destination, [string]$name) {
    $start = [Diagnostics.ProcessStartInfo]::new($exe)
    $start.WorkingDirectory = $root
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    foreach ($argument in $arguments) { $start.ArgumentList.Add($argument) }
    $timer = [Diagnostics.Stopwatch]::StartNew()
    $process = [Diagnostics.Process]::Start($start)
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    $process.WaitForExit()
    $exit = $process.ExitCode
    [IO.File]::WriteAllText((Join-Path $destination "$name.stdout.txt"), $stdout.GetAwaiter().GetResult())
    [IO.File]::WriteAllText((Join-Path $destination "$name.stderr.txt"), $stderr.GetAwaiter().GetResult())
    @{command=@($exe)+$arguments;exit=$exit;durationSeconds=$timer.Elapsed.TotalSeconds} |
        ConvertTo-Json -Depth 6 | Set-Content (Join-Path $destination "$name.exit.json") -Encoding utf8
    $process.Dispose()
    return $exit
}
foreach ($variant in ($variants | Where-Object { !$Only -or $_.name -eq $Only })) {
    $destination = Join-Path $PSScriptRoot "repair-mutant-$($variant.name)"
    if (Test-Path $destination) { throw "Evidence already exists: $($variant.name)" }
    [IO.Directory]::CreateDirectory($destination) | Out-Null
    $fixture = Join-Path $work $variant.name
    $core = Join-Path $fixture 'core'
    $testBin = Join-Path $fixture 'tests'
    [IO.Directory]::CreateDirectory($core) | Out-Null
    Get-ChildItem $source -File -Recurse | Where-Object {
        $_.FullName -notmatch '\\(obj|bin)\\'
    } | ForEach-Object {
        $target = Join-Path $core ([IO.Path]::GetRelativePath($source, $_.FullName))
        [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target)) | Out-Null
        Copy-Item $_.FullName $target
    }
    [IO.Directory]::CreateDirectory((Join-Path $core 'obj')) | Out-Null
    Get-ChildItem (Join-Path $source 'obj') -File | Copy-Item -Destination (Join-Path $core 'obj')
    $originalPath = Join-Path $source $variant.file
    $original = [IO.File]::ReadAllText($originalPath).Replace("`r`n", "`n")
    $mutant = $original
    foreach ($edit in $variant.edits) {
        $old = $edit[0].Replace("`r`n", "`n")
        if (!$mutant.Contains($old)) { throw "Mutation seam absent: $($variant.name)" }
        $mutant = $mutant.Replace($old, $edit[1].Replace("`r`n", "`n"))
    }
    [IO.File]::WriteAllText((Join-Path $core $variant.file), $mutant)
    [IO.File]::WriteAllText((Join-Path $destination 'original.cs.txt'), $original)
    [IO.File]::WriteAllText((Join-Path $destination 'mutant.cs.txt'), $mutant)
    $before = (Get-FileHash $originalPath -Algorithm SHA256).Hash
    $build = Invoke-Captured 'dotnet' @('build',(Join-Path $core 'AiDe.Core.csproj'),'--no-restore',
        '-p:UseSharedCompilation=false','-m:1','-nodeReuse:false','--verbosity','minimal') $destination 'build'
    if ($build -ne 0) { throw "Build failure is not semantic RED: $($variant.name)" }
    Copy-Item $tests $testBin -Recurse
    $mutantDll = Join-Path $core 'bin\Debug\net10.0\AiDe.Core.dll'
    Copy-Item $mutantDll (Join-Path $testBin 'AiDe.Core.dll')
    $exit = Invoke-Captured 'dotnet' @('vstest',(Join-Path $testBin 'AiDe.Core.Tests.dll'),
        "/TestCaseFilter:FullyQualifiedName~$($variant.filter)",
        '/Logger:trx;LogFileName=mutant.trx',"/ResultsDirectory:$destination") $destination 'test'
    [xml]$trx = Get-Content (Join-Path $destination 'mutant.trx') -Raw
    $results = @($trx.TestRun.Results.UnitTestResult)
    $failed = @($results | Where-Object outcome -eq 'Failed')
    $receipt = @{
        parent=(& git rev-parse HEAD); mutant=$variant.name; scope=$variant.file; sourceBefore=$before
        sourceAfter=(Get-FileHash $originalPath -Algorithm SHA256).Hash
        mutantDll=(Get-FileHash $mutantDll -Algorithm SHA256).Hash
        testsDll=(Get-FileHash (Join-Path $testBin 'AiDe.Core.Tests.dll') -Algorithm SHA256).Hash
        total=$results.Count; failed=$failed.Count; actualExit=$exit
        results=@($results | ForEach-Object { @{name=$_.testName;outcome=$_.outcome;message=$_.Output.ErrorInfo.Message} })
    }
    $receipt | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $destination 'receipt.json') -Encoding utf8
    Write-Host "MUTANT $($variant.name) exit=$exit total=$($results.Count) failed=$($failed.Count)"
    if ($before -ne $receipt.sourceAfter -or $results.Count -ne $variant.count -or
        $failed.Count -ne $variant.count -or $exit -ne 1) { throw 'Mutant oracle/result mismatch' }
    Remove-Item $fixture -Recurse -Force
}
