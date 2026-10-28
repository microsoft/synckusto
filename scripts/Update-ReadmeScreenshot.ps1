#Requires -Version 7.2
<#
.SYNOPSIS
Builds SyncKusto and refreshes the README screenshot using fictional, offline data.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if (-not $IsWindows) {
    throw 'Screenshot capture requires Windows and a desktop session.'
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$dotnet = (Get-Command dotnet -CommandType Application -ErrorAction Stop | Select-Object -First 1).Source
$temporaryDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("synckusto-screenshot-" + [guid]::NewGuid().ToString('N'))
$stagingPath = Join-Path $repoRoot (".screenshot-" + [guid]::NewGuid().ToString('N') + '.png')

Push-Location $repoRoot
try {
    $requiredSdk = (Get-Content -LiteralPath 'global.json' -Raw | ConvertFrom-Json).sdk.version
    $actualSdk = & $dotnet --version
    if ($LASTEXITCODE -ne 0) {
        throw "Install a .NET SDK compatible with global.json ($requiredSdk or a newer servicing patch in the same feature band) and ensure dotnet can find it."
    }
    Write-Verbose "Using .NET SDK $actualSdk."

    & $dotnet restore SyncKusto.sln --locked-mode --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw 'Locked dependency restore failed. The existing screenshot was not changed.' }
    & $dotnet build SyncKusto.Tests\SyncKusto.Tests.csproj --configuration Release --no-restore --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw 'Build failed. The existing screenshot was not changed.' }

    [System.IO.Directory]::CreateDirectory($temporaryDirectory) | Out-Null
    $capturePath = Join-Path $temporaryDirectory 'screenshot.png'
    $startInfo = [System.Diagnostics.ProcessStartInfo]::new($dotnet)
    $startInfo.UseShellExecute = $false
    $startInfo.WorkingDirectory = $repoRoot
    foreach ($argument in @(
        'test', 'SyncKusto.Tests\SyncKusto.Tests.csproj',
        '--configuration', 'Release', '--no-build', '--no-restore',
        '--filter', 'FullyQualifiedName=SyncKusto.Tests.Screenshots.ReadmeScreenshotTests.Capture',
        '--results-directory', $temporaryDirectory,
        '--', 'RunConfiguration.TreatNoTestsAsError=true', 'NUnit.ExplicitMode=Strict'
    )) {
        $startInfo.ArgumentList.Add($argument)
    }
    $startInfo.Environment['SYNCKUSTO_SCREENSHOT_PATH'] = $capturePath
    $process = [System.Diagnostics.Process]::Start($startInfo)
    try {
        if (-not $process.WaitForExit(120000)) {
            $process.Kill($true)
            $process.WaitForExit()
            throw 'Screenshot capture timed out. The existing screenshot was not changed.'
        }
        if ($process.ExitCode -ne 0) {
            throw 'Screenshot capture failed. The existing screenshot was not changed.'
        }
    }
    finally {
        $process.Dispose()
    }

    $png = [System.IO.File]::ReadAllBytes($capturePath)
    if ($png.Length -lt 24 -or
        [Convert]::ToHexString($png[0..7]) -ne '89504E470D0A1A0A' -or
        [System.Text.Encoding]::ASCII.GetString($png, 12, 4) -ne 'IHDR') {
        throw 'Capture did not produce a valid PNG. The existing screenshot was not changed.'
    }
    $width = [Convert]::ToInt32([Convert]::ToHexString([byte[]]$png[16..19]), 16)
    $height = [Convert]::ToInt32([Convert]::ToHexString([byte[]]$png[20..23]), 16)
    if ($width -ne 655 -or $height -ne 854) {
        throw "Unexpected screenshot dimensions: ${width}x${height}. The existing screenshot was not changed."
    }

    [System.IO.File]::Copy($capturePath, $stagingPath)
    $destination = Join-Path $repoRoot 'screenshot.png'
    if (Test-Path -LiteralPath $destination) {
        [System.IO.File]::Replace($stagingPath, $destination, [NullString]::Value)
    }
    else {
        [System.IO.File]::Move($stagingPath, $destination)
    }
    Write-Host "Updated $destination"
}
finally {
    if (Test-Path -LiteralPath $stagingPath) {
        Remove-Item -LiteralPath $stagingPath -Force
    }
    if (Test-Path -LiteralPath $temporaryDirectory) {
        Remove-Item -LiteralPath $temporaryDirectory -Recurse -Force
    }
    Pop-Location
}
