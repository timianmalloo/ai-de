[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $OutputRoot,
    [ValidateSet('Discover', 'Qualify', 'Approved')]
    [string] $Mode = 'Approved',
    [string] $ExecutionRoot
)

$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath('..', $PSScriptRoot)
$fixture = Join-Path $repo 'tests\AiDe.Core.Tests\Fixtures\AtlasLegacyV1'
$manifestPath = Join-Path $fixture 'manifest.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($Mode -ne 'Discover') {
    $requiredStatus = if ($Mode -eq 'Approved') { 'Approved' } else { 'QualificationOnly' }
    if ($manifest.canonicalCandidate.status -ne $requiredStatus -or
        $manifest.canonicalCandidate.coreSha256 -notmatch '^[A-F0-9]{64}$' -or
        $manifest.canonicalCandidate.peerSha256 -notmatch '^[A-F0-9]{64}$') {
        throw "No fixed $requiredStatus canonical pair is authorized."
    }
}
$output = [IO.Path]::GetFullPath($OutputRoot, $repo).TrimEnd('\')
$allowed = (Join-Path $repo '.artifacts') + '\'
if (-not $IsWindows -or -not $output.StartsWith($allowed, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Output must be a new directory under this repository .artifacts on Windows.'
}
if (Test-Path -LiteralPath $output) {
    throw 'Output already exists; existing preparations are never overwritten.'
}

function Assert-NoReparseAncestor([string] $Path) {
    $current = $Path
    while ($current) {
        if (Test-Path -LiteralPath $current) {
            if ((Get-Item -LiteralPath $current -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
                throw "Reparse ancestor refused: $current"
            }
        }
        $current = [IO.Path]::GetDirectoryName($current)
    }
}

Assert-NoReparseAncestor $fixture
Assert-NoReparseAncestor $output
$execution = $null
if ($ExecutionRoot) {
    if ($Mode -eq 'Discover') { throw 'Execution staging requires a pre-pinned pair.' }
    $execution = [IO.Path]::GetFullPath($ExecutionRoot, $repo).TrimEnd('\')
    if ($output.StartsWith($execution + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Build staging must not be inside execution staging.'
    }
    if (-not $execution.StartsWith($allowed, [StringComparison]::OrdinalIgnoreCase) -or
        $execution.Equals($output, [StringComparison]::OrdinalIgnoreCase) -or
        $execution.StartsWith($output + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Execution staging must be a separate owned .artifacts directory.'
    }
    if (Test-Path -LiteralPath $execution) { throw 'Execution output already exists; overwrite refused.' }
    Assert-NoReparseAncestor $execution
    # Frozen fixture suffix plus the longest loose SHA-1 object name created by its default-format commit.
    $legacySuffix = '.artifacts\atlas-runtime\' + ('0' * 32) + '\repository'
    $gitSuffix = '.git\objects\00\' + ('0' * 38)
    $maxExecutionLength = 259 - 1 - $legacySuffix.Length - 1 - $gitSuffix.Length
    if ($execution.Length -gt $maxExecutionLength) {
        throw "Legacy Git path budget exceeded: execution root $($execution.Length), maximum $maxExecutionLength."
    }
}
$archivePath = Join-Path $fixture 'baseline-source.zip'
$patchPath = Join-Path $fixture 'peer.patch'
if ((Get-FileHash -LiteralPath $archivePath).Hash -ne $manifest.archiveSha256 -or
    (Get-FileHash -LiteralPath $patchPath).Hash -ne $manifest.patchSha256) {
    throw 'Committed input hash mismatch.'
}
$sdk = (& dotnet --version).Trim()
if ($LASTEXITCODE -or $sdk -ne $manifest.sdk) { throw 'Required dotnet SDK is unavailable.' }
$gitVersion = (& git --version).Trim()
if ($LASTEXITCODE -or $gitVersion -ne $manifest.git) { throw 'Required git version is unavailable.' }
$headers = @(Select-String -LiteralPath $patchPath -Pattern '^diff --git ' | ForEach-Object { $_.Line })
if ($headers.Count -ne 1 -or $headers[0] -ne
    'diff --git a/tests/AiDe.Core.Tests/Understanding/AtlasStaticReaderContractTests.cs b/tests/AiDe.Core.Tests/Understanding/AtlasStaticReaderContractTests.cs') {
    throw 'Peer patch must touch only its recorded test file.'
}

$archive = [IO.Compression.ZipFile]::OpenRead($archivePath)
$expected = @{}
foreach ($entry in $manifest.entries) {
    if ($expected.ContainsKey($entry.path)) { throw 'Duplicate manifest path.' }
    $expected[$entry.path] = $entry
}
$destinations = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$validated = [Collections.Generic.List[object]]::new()
$total = 0L
try {
    if ($archive.Entries.Count -gt 10000) { throw 'Archive entry budget exceeded.' }
    foreach ($entry in $archive.Entries) {
        $name = $entry.FullName
        $parts = $name.Split('/')
        if ([string]::IsNullOrEmpty($name) -or $name.Contains('\') -or $name.Contains(':') -or
            [IO.Path]::IsPathRooted($name) -or -not $expected.ContainsKey($name)) {
            throw "Unmanifested or rooted archive entry: $name"
        }
        foreach ($part in $parts) {
            if ($part -in @('', '.', '..') -or $part.EndsWith('.') -or $part.EndsWith(' ') -or
                $part.IndexOfAny([IO.Path]::GetInvalidFileNameChars()) -ge 0 -or
                $part -match '^(?i:CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\.|$)') {
                throw "Unsafe archive component: $name"
            }
        }
        $entryMode = ($entry.ExternalAttributes -shr 16) -band 61440
        if ($entryMode -notin @(0, 32768) -or ($entry.ExternalAttributes -band 1024)) {
            throw "Link or nonregular archive entry: $name"
        }
        $destination = [IO.Path]::GetFullPath($name.Replace('/', '\'), $output)
        if (-not $destination.StartsWith($output + '\', [StringComparison]::OrdinalIgnoreCase) -or
            -not $destinations.Add($destination)) {
            throw "Escaping or colliding archive entry: $name"
        }
        $total += $entry.Length
        if ($entry.Length -gt 64MB -or $total -gt 256MB -or $entry.Length -ne $expected[$name].length) {
            throw 'Archive size or manifest length mismatch.'
        }
        $source = $entry.Open()
        try { $hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($source)) }
        finally { $source.Dispose() }
        if ($hash -ne $expected[$name].sha256) { throw "Source input hash mismatch: $name" }
        $validated.Add(@{ Entry = $entry; Destination = $destination })
    }
    if ($validated.Count -ne $expected.Count) { throw 'Archive is missing recorded inputs.' }
    [IO.Directory]::CreateDirectory($output) | Out-Null
    foreach ($item in $validated) {
        $parent = [IO.Path]::GetDirectoryName($item.Destination)
        Assert-NoReparseAncestor $parent
        [IO.Directory]::CreateDirectory($parent) | Out-Null
        Assert-NoReparseAncestor $parent
        $source = $item.Entry.Open()
        $target = [IO.File]::Open($item.Destination, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
        try { $source.CopyTo($target) }
        finally { $target.Dispose(); $source.Dispose() }
    }
}
finally { $archive.Dispose() }

$receipt = [ordered]@{
    sourcePin = $manifest.sourcePin
    manifestSha256 = (Get-FileHash -LiteralPath $manifestPath).Hash
    archiveSha256 = $manifest.archiveSha256
    patchSha256 = $manifest.patchSha256
    inputEntries = $manifest.entries
    outputRoot = $output
    sdk = $sdk
    git = $gitVersion
    powershell = $PSVersionTable.PSVersion.ToString()
    mode = $Mode
    historicalCoreSha256 = $manifest.expectedCoreSha256
    historicalPeerSha256 = $manifest.expectedPeerSha256
    settings = 'Debug; deterministic; portable PDB; canonical /_/atlas-v1 paths; explicit archive SourceRoot and per-file baseline SourceLink; patched peer excluded from links and embedded; no ambient SCM queries'
    commands = [Collections.Generic.List[object]]::new()
    outcome = 'Preparing'
}

function Invoke-Bounded([string] $Name, [string] $Executable, [string[]] $Arguments) {
    $start = [Diagnostics.ProcessStartInfo]::new($Executable)
    $start.WorkingDirectory = $output
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.Environment['GIT_CEILING_DIRECTORIES'] = [IO.Path]::GetDirectoryName($output)
    foreach ($argument in $Arguments) { $start.ArgumentList.Add($argument) }
    $process = [Diagnostics.Process]::Start($start)
    if ($null -eq $process) { throw "Could not start $Executable" }
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    $watch = [Diagnostics.Stopwatch]::StartNew()
    try {
        if (-not $process.WaitForExit(180000)) {
            $process.Kill($true)
            $process.WaitForExit()
            throw "Preparation command timed out: $Name"
        }
        $code = $process.ExitCode
        return $code
    }
    finally {
        if (-not $process.HasExited) {
            $process.Kill($true)
            $process.WaitForExit()
        }
        [IO.File]::WriteAllText((Join-Path $output "$Name.stdout.log"), $stdout.GetAwaiter().GetResult())
        [IO.File]::WriteAllText((Join-Path $output "$Name.stderr.log"), $stderr.GetAwaiter().GetResult())
        $receipt.commands.Add(@{ name = $Name; executable = $Executable; arguments = $Arguments;
            exit = $process.ExitCode; elapsedMilliseconds = $watch.ElapsedMilliseconds })
        $process.Dispose()
    }
}

try {
    $normalizedPatch = Join-Path $output 'peer-normalized.patch'
    $utf8 = [Text.UTF8Encoding]::new($false, $true)
    [IO.File]::WriteAllText($normalizedPatch, [IO.File]::ReadAllText($patchPath, $utf8).Replace("`r`n", "`n"), $utf8)
    $receipt.normalizedPatchSha256 = (Get-FileHash -LiteralPath $normalizedPatch).Hash
    $gitSettings = @('-c', 'core.autocrlf=false', '-c', 'core.eol=lf')
    if ((Invoke-Bounded 'patch-check' 'git' ($gitSettings + @('apply', '--no-index', '--check', $normalizedPatch))) -ne 0) {
        throw 'Peer patch check failed.'
    }
    if ((Invoke-Bounded 'patch-apply' 'git' ($gitSettings + @('apply', '--no-index', $normalizedPatch))) -ne 0) {
        throw 'Peer patch application failed.'
    }
    $patched = Join-Path $output 'tests\AiDe.Core.Tests\Understanding\AtlasStaticReaderContractTests.cs'
    $receipt.patchedSourceSha256 = (Get-FileHash -LiteralPath $patched).Hash
    if ($receipt.patchedSourceSha256 -ne $manifest.qualifiedPeerSourceSha256) {
        throw 'Patched source differs from the executed genuine peer.'
    }
    $patchedRelative = 'tests/AiDe.Core.Tests/Understanding/AtlasStaticReaderContractTests.cs'
    $documents = [ordered]@{}
    foreach ($entry in $manifest.entries | Sort-Object path) {
        if ($entry.path.EndsWith('.cs') -and $entry.path -ne $patchedRelative) {
            $documents['/_/atlas-v1/' + $entry.path] =
                'https://raw.githubusercontent.com/timianmalloo/ai-de/' + $manifest.sourcePin + '/' + $entry.path
        }
    }
    $sourceLinkPath = Join-Path $output 'atlas-canonical.sourcelink.json'
    [IO.File]::WriteAllText($sourceLinkPath, (@{ documents = $documents } | ConvertTo-Json -Depth 4 -Compress), $utf8)
    $overridesPath = Join-Path $output 'atlas-canonical.targets'
    $overrides = @'
<Project>
  <PropertyGroup>
    <EnableSourceControlManagerQueries>false</EnableSourceControlManagerQueries>
    <EmbedAllSources>true</EmbedAllSources>
    <Deterministic>true</Deterministic>
    <DebugType>portable</DebugType>
    <SourceRevisionId>__BASELINE__</SourceRevisionId>
    <RepositoryUrl>https://github.com/timianmalloo/ai-de</RepositoryUrl>
  </PropertyGroup>
  <ItemGroup>
    <SourceRoot Remove="@(SourceRoot)" />
    <SourceRoot Include="$(MSBuildThisFileDirectory)" MappedPath="/_/atlas-v1/" SourceControl="archive" RevisionId="__BASELINE__" />
    <AssemblyMetadata Include="AtlasLegacyBaseline" Value="__BASELINE__" />
    <AssemblyMetadata Include="AtlasLegacyArchiveSha256" Value="__ARCHIVE__" />
    <AssemblyMetadata Include="AtlasLegacyPeerSourceSha256" Value="__PEER__" Condition="'$(MSBuildProjectName)' == 'AiDe.Core.Tests'" />
  </ItemGroup>
  <Target Name="AtlasCanonicalCompilerProvenance" BeforeTargets="CoreCompile" AfterTargets="GenerateSourceLinkFile">
    <PropertyGroup>
      <PathMap>$(MSBuildThisFileDirectory)=/_/atlas-v1/</PathMap>
      <SourceLink>$(MSBuildThisFileDirectory)atlas-canonical.sourcelink.json</SourceLink>
    </PropertyGroup>
    <WriteLinesToFile File="$(IntermediateOutputPath)atlas-effective-build.txt"
      Lines="SDK=$(NETCoreSdkVersion);Deterministic=$(Deterministic);DebugType=$(DebugType);EmbedAllSources=$(EmbedAllSources);PathMap=$(PathMap);SourceLink=$(SourceLink);SourceRevisionId=$(SourceRevisionId);EnableSourceControlManagerQueries=$(EnableSourceControlManagerQueries);@(SourceRoot->'SourceRoot=%(Identity)|%(MappedPath)|%(SourceControl)|%(RevisionId)')"
      Overwrite="true" />
  </Target>
</Project>
'@
    $overrides = $overrides.Replace('__BASELINE__', $manifest.sourcePin).
        Replace('__ARCHIVE__', $manifest.archiveSha256).Replace('__PEER__', $manifest.qualifiedPeerSourceSha256)
    [IO.File]::WriteAllText($overridesPath, $overrides.Replace("`r`n", "`n"), $utf8)
    $receipt.generatedInputs = @(
        @{ path = 'atlas-canonical.targets'; sha256 = (Get-FileHash -LiteralPath $overridesPath).Hash },
        @{ path = 'atlas-canonical.sourcelink.json'; sha256 = (Get-FileHash -LiteralPath $sourceLinkPath).Hash }
    )
    $receipt.patchedSourceProvenance = 'Exact qualified source SHA256; embedded in portable PDB, deliberately absent from baseline SourceLink map'
    $build = @('build', 'tests\AiDe.Core.Tests\AiDe.Core.Tests.csproj', '--verbosity', 'quiet',
        "-p:CustomAfterMicrosoftCommonTargets=$overridesPath")
    $code = Invoke-Bounded 'build-no-restore' 'dotnet' ($build + '--no-restore')
    if ($code -ne 0 -and (Select-String -LiteralPath (Join-Path $output 'build-no-restore.stdout.log') -Pattern 'NETSDK1004' -Quiet)) {
        $code = Invoke-Bounded 'build-restored' 'dotnet' $build
    }
    if ($code -ne 0) { throw 'Legacy build failed; inspect recorded stdout/stderr.' }
    $bin = Join-Path $output 'tests\AiDe.Core.Tests\bin\Debug\net10.0'
    $receipt.coreSha256 = (Get-FileHash -LiteralPath (Join-Path $bin 'AiDe.Core.dll')).Hash
    $receipt.peerSha256 = (Get-FileHash -LiteralPath (Join-Path $bin 'AiDe.Core.Tests.dll')).Hash
    $receipt.coreProductVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $bin 'AiDe.Core.dll')).ProductVersion
    if ($Mode -eq 'Discover') {
        $receipt.outcome = 'CandidateBuiltNotQualified'
    }
    elseif ($receipt.coreSha256 -ne $manifest.canonicalCandidate.coreSha256 -or
        $receipt.peerSha256 -ne $manifest.canonicalCandidate.peerSha256) {
        $receipt.outcome = 'BlockedOutputHashMismatch'
        throw 'Output differs from the pre-pinned canonical pair. No runtime replacement hash is accepted.'
    }
    else {
        $receipt.outcome = if ($Mode -eq 'Approved') { 'ApprovedReproduction' } else { 'QualifiedPairReproduction' }
    }
    if ($execution) {
        $fixtureSource = [IO.File]::ReadAllText((Join-Path $output 'tests\AiDe.Core.Tests\Understanding\AtlasProductionAdmissionTests.cs'))
        if (-not $fixtureSource.Contains('Path.Combine(AppContext.BaseDirectory, ".artifacts", "atlas-runtime", Guid.NewGuid().ToString("N"))') -or
            -not $fixtureSource.Contains('Repository => Path.Combine(_root, "repository")')) {
            throw 'Frozen legacy fixture path contract changed.'
        }
        $closure = [Collections.Generic.List[object]]::new()
        foreach ($file in Get-ChildItem -LiteralPath $bin -File -Recurse | Sort-Object FullName) {
            $relative = [IO.Path]::GetRelativePath($bin, $file.FullName)
            if ($relative.Split('\') -contains '.artifacts') { continue }
            Assert-NoReparseAncestor $file.FullName
            $destination = [IO.Path]::GetFullPath($relative, $execution)
            if (-not $destination.StartsWith($execution + '\', [StringComparison]::OrdinalIgnoreCase) -or
                $destination.Length -gt 259) {
                throw 'Runtime closure escapes execution staging or exceeds the file path budget.'
            }
            $closure.Add(@{ path = $relative; length = $file.Length; sha256 = (Get-FileHash -LiteralPath $file.FullName).Hash })
        }
        $receipt.runtimeClosure = $closure
        if (Test-Path -LiteralPath $execution) { throw 'Execution output appeared during preparation; overwrite refused.' }
        Assert-NoReparseAncestor $execution
        [IO.Directory]::CreateDirectory($execution) | Out-Null
        foreach ($file in $closure) {
            $destination = Join-Path $execution $file.path
            Assert-NoReparseAncestor $destination
            [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null
            $source = [IO.File]::OpenRead((Join-Path $bin $file.path))
            $target = [IO.File]::Open($destination, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
            try { $source.CopyTo($target) }
            finally { $target.Dispose(); $source.Dispose() }
            if ((Get-FileHash -LiteralPath $destination).Hash -ne $file.sha256) {
                throw 'Copied runtime closure hash mismatch.'
            }
        }
        if ((Get-FileHash -LiteralPath (Join-Path $execution 'AiDe.Core.dll')).Hash -ne $manifest.canonicalCandidate.coreSha256 -or
            (Get-FileHash -LiteralPath (Join-Path $execution 'AiDe.Core.Tests.dll')).Hash -ne $manifest.canonicalCandidate.peerSha256) {
            throw 'Execution pair differs from its pre-pinned qualification.'
        }
        $receipt.execution = @{
            root = $execution; rootLength = $execution.Length; maximumRootLength = $maxExecutionLength
            legacySuffix = $legacySuffix; gitObjectSuffix = $gitSuffix; maximumFilePath = 259
            files = $closure.Count; excluded = 'Any .artifacts path segment'
        }
    }
}
catch {
    if ($receipt.outcome -ne 'BlockedOutputHashMismatch') { $receipt.outcome = 'BlockedPreparation' }
    $receipt.failure = $_.Exception.Message
    throw
}
finally {
    $receipt | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $output 'preparation.json') -Encoding utf8
}
