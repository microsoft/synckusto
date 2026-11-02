$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\Release.ps1"

$passed = 0
function Assert-Equal($Actual, $Expected) {
    if ($Actual -cne $Expected) {
        throw "Expected '$Expected', got '$Actual'."
    }
}
function Assert-Fails([scriptblock] $Action, [string] $Message) {
    $failure = $null
    try { & $Action }
    catch { $failure = $_ }
    if ($null -eq $failure -or $failure.Exception.Message -notlike "*$Message*") {
        throw "Expected failure containing '$Message', got '$failure'."
    }
}
function Test-Case([string] $Name, [scriptblock] $Action) {
    & $Action
    $script:passed++
    Write-Host "PASS $Name"
}

$fixture = Join-Path ([IO.Path]::GetTempPath()) "synckusto-release-$([guid]::NewGuid().ToString('N'))"
$null = [IO.Directory]::CreateDirectory($fixture)
$publish = Join-Path $fixture 'publish'
$null = [IO.Directory]::CreateDirectory($publish)
$tested = Join-Path $fixture 'tested.dll'
$zip = Join-Path $fixture 'synckusto.zip'
$commit = 'a' * 40
$scenario = 'success'
$calls = [System.Collections.Generic.List[object]]::new()

# Shadow gh: these tests must never contact GitHub or create a real tag/release.
function gh {
    param([Parameter(ValueFromRemainingArguments)][string[]] $Arguments)

    $script:calls.Add($Arguments)
    $global:LASTEXITCODE = 0
    if ($Arguments[0] -eq 'api' -and $Arguments[1].EndsWith('/releases?per_page=100')) {
        if ($script:scenario -eq 'enumeration-failure') {
            $global:LASTEXITCODE = 1
            return
        }
        if ($script:scenario -eq 'malformed-response') { return 'not json' }
        if ($script:scenario -eq 'empty-response') { return '' }
        if ($script:scenario -eq 'null-response') { return 'null' }
        if ($script:scenario -eq 'object-response') { return '{}' }
        if ($script:scenario -eq 'invalid-page') { return '[{}]' }
        if ($script:scenario -eq 'pagination') {
            return '[[{"tag_name":"v20261029"}],[{"tag_name":"v20261029.2"}]]'
        }
        if ($script:scenario -eq 'duplicate') { return '[[{"tag_name":"v20261029"}]]' }
        return '[[]]'
    }
    if ($Arguments[0] -eq 'api' -and $Arguments[1].EndsWith('/tags?per_page=100')) {
        if ($script:scenario -eq 'tag-enumeration-failure') {
            $global:LASTEXITCODE = 1
            return
        }
        if ($script:scenario -eq 'pagination') {
            return '[[{"name":"v20261029.1"}],[{"name":"v20261029.10"}]]'
        }
        return '[[]]'
    }
    if ($Arguments[0] -eq 'api' -and $Arguments[1] -eq '--method') {
        if ($script:scenario -eq 'tag-race') { $global:LASTEXITCODE = 1 }
        return '{}'
    }
    if ($Arguments[0] -eq 'release' -and $Arguments[1] -eq 'create') {
        if ($script:scenario -eq 'upload-failure') { $global:LASTEXITCODE = 1 }
        return 'https://github.com/example/repo/releases/1'
    }
    if ($Arguments[0] -eq 'release' -and $Arguments[1] -eq 'view') {
        $asset = @{
            name = 'synckusto.zip'
            state = 'uploaded'
            size = (Get-Item -LiteralPath $script:zip).Length
            digest = 'sha256:' + (Get-FileHash -LiteralPath $script:zip).Hash.ToLowerInvariant()
        }
        $release = @{
            isDraft = $true
            tagName = 'v20261029'
            targetCommitish = $script:commit
            assets = @($asset)
            url = 'https://github.com/example/repo/releases/1'
        }
        switch ($script:scenario) {
            'published' { $release.isDraft = $false }
            'wrong-target' { $release.targetCommitish = 'master' }
            'missing-asset' { $release.assets = @() }
            'wrong-size' { $asset.size++ }
            'wrong-digest' { $asset.digest = 'sha256:incorrect' }
            'incomplete-asset' { $asset.state = 'new' }
        }
        return $release | ConvertTo-Json -Depth 10 -Compress
    }
    if ($Arguments[0] -eq 'api' -and $Arguments[1] -like '*/git/ref/tags/*') {
        $sha = $script:commit
        if ($script:scenario -eq 'wrong-tag') { $sha = 'b' * 40 }
        return @{ object = @{ type = 'commit'; sha = $sha } } | ConvertTo-Json -Compress
    }
    throw "Unexpected GitHub CLI call: $($Arguments -join ' ')"
}

try {
    Test-Case 'first release and leap day' {
        Assert-Equal (Get-ReleaseVersion -Date '20261029') '20261029'
        Assert-Equal (Get-ReleaseVersion -Date '20240229') '20240229'
    }
    Test-Case 'second and subsequent releases' {
        Assert-Equal (Get-ReleaseVersion '20261029' @('v20261029')) '20261029.1'
        Assert-Equal (Get-ReleaseVersion '20261029' @('v20261029', 'v20261029.1')) '20261029.2'
    }
    Test-Case 'numeric ordering, gaps, and missing base tag' {
        Assert-Equal (Get-ReleaseVersion '20261029' @('v20261029.2', 'v20261029.10')) '20261029.11'
        Assert-Equal (Get-ReleaseVersion '20261029' @('v20261029.3')) '20261029.4'
    }
    Test-Case 'ignore unrelated or noncanonical names' {
        Assert-Equal (Get-ReleaseVersion '20261029' @(
            'v20261028', 'v202610290', 'v20261029-beta', 'v20261029.01', 'v20261029.0', 'V20261029'
        )) '20261029'
    }
    Test-Case 'invalid dates and suffix overflow fail explicitly' {
        foreach ($date in @('20260229', '20261301', '2026102', '2026-10-29', ' 20261029')) {
            Assert-Fails { Get-ReleaseVersion -Date $date } 'real calendar date'
        }
        Assert-Fails { Get-ReleaseVersion '20261029' @('v20261029.2147483648') } 'integer range'
        Assert-Fails { Get-ReleaseVersion '20261029' @('v20261029.2147483647') } 'exhausted'
    }
    Test-Case 'paginate drafts and standalone tags, not only published releases' {
        $script:scenario = 'pagination'
        $script:calls.Clear()
        $reserved = @(Get-ReservedReleaseTags 'example/repo')
        Assert-Equal $reserved.Count 4
        Assert-Equal (Get-ReleaseVersion '20261029' $reserved) '20261029.11'
        foreach ($call in $script:calls) {
            Assert-Equal ($call -contains '--paginate') $true
            Assert-Equal ($call -contains '--slurp') $true
        }
    }
    Test-Case 'API and JSON errors are not treated as an empty release history' {
        foreach ($failure in @('enumeration-failure', 'tag-enumeration-failure')) {
            $script:scenario = $failure
            Assert-Fails { Get-ReservedReleaseTags 'example/repo' } 'failed with exit code'
        }
        $script:scenario = 'malformed-response'
        Assert-Fails { Get-ReservedReleaseTags 'example/repo' } 'JSON'
        $script:scenario = 'empty-response'
        Assert-Fails { Get-ReservedReleaseTags 'example/repo' } 'empty response'
        foreach ($invalid in @('null-response', 'object-response', 'invalid-page')) {
            $script:scenario = $invalid
            Assert-Fails { Get-ReservedReleaseTags 'example/repo' } 'invalid'
        }
    }

    [IO.File]::WriteAllText($tested, 'tested stamped assembly fixture')
    foreach ($name in @(
        'SyncKusto.exe', 'SyncKusto.dll', 'SyncKusto.deps.json', 'SyncKusto.runtimeconfig.json',
        'SyncKusto.Core.dll', 'SyncKusto.Kusto.dll', 'SyncKusto.FileSystem.dll'
    )) {
        [IO.File]::WriteAllText((Join-Path $publish $name), 'tested stamped assembly fixture')
    }
    Test-Case 'package tested binaries at ZIP root' {
        New-ReleaseArchive -PublishDirectory $publish -TestedAssembly $tested -Destination $zip
        $archive = [IO.Compression.ZipFile]::OpenRead($zip)
        try {
            Assert-Equal $archive.Entries.Count 7
            Assert-Equal ($null -ne $archive.GetEntry('SyncKusto.exe')) $true
            Assert-Equal ($null -ne $archive.GetEntry('SyncKusto.dll')) $true
        }
        finally { $archive.Dispose() }
    }
    Test-Case 'never overwrite an existing archive' {
        Assert-Fails { New-ReleaseArchive $publish $tested $zip } 'already exists'
    }
    Test-Case 'reject stale publish output, missing runtime files, and tests' {
        $otherZip = Join-Path $fixture 'invalid.zip'
        [IO.File]::WriteAllText($tested, 'different build')
        Assert-Fails { New-ReleaseArchive $publish $tested $otherZip } 'differs from'
        [IO.File]::WriteAllText($tested, 'tested stamped assembly fixture')
        $runtimeConfig = Join-Path $publish 'SyncKusto.runtimeconfig.json'
        [IO.File]::Delete($runtimeConfig)
        Assert-Fails { New-ReleaseArchive $publish $tested $otherZip } 'missing SyncKusto.runtimeconfig.json'
        [IO.File]::WriteAllText($runtimeConfig, 'fixture')
        $testAssembly = Join-Path $publish 'SyncKusto.Tests.dll'
        [IO.File]::WriteAllText($testAssembly, 'fixture')
        Assert-Fails { New-ReleaseArchive $publish $tested $otherZip } 'test assemblies'
        [IO.File]::Delete($testAssembly)
    }
    Test-Case 'create draft with asset and exact source SHA, never publish it' {
        $script:scenario = 'success'
        $script:calls.Clear()
        $result = New-DraftRelease 'example/repo' '20261029' $commit $zip
        Assert-Equal $result.isDraft $true
        $tagCall = @($script:calls | Where-Object { $_[0] -eq 'api' -and $_[1] -eq '--method' })
        Assert-Equal $tagCall.Count 1
        Assert-Equal ($tagCall[0] -contains "sha=$commit") $true
        $create = @($script:calls | Where-Object { $_[0] -eq 'release' -and $_[1] -eq 'create' })
        Assert-Equal $create.Count 1
        foreach ($argument in @('--draft', '--verify-tag', '--generate-notes', '--target', $commit, $zip)) {
            Assert-Equal ($create[0] -contains $argument) $true
        }
    }
    Test-Case 'collisions and enumeration errors cannot cause remote writes' {
        foreach ($failure in @('duplicate', 'enumeration-failure', 'tag-enumeration-failure')) {
            $script:scenario = $failure
            $script:calls.Clear()
            $message = if ($failure -eq 'duplicate') { 'already reserved' } else { 'failed with exit code' }
            Assert-Fails { New-DraftRelease 'example/repo' '20261029' $commit $zip } $message
            Assert-Equal @($script:calls | Where-Object { $_ -contains 'POST' -or $_[0] -eq 'release' }).Count 0
        }
    }
    Test-Case 'tag race aborts without creating a release' {
        $script:scenario = 'tag-race'
        $script:calls.Clear()
        Assert-Fails { New-DraftRelease 'example/repo' '20261029' $commit $zip } 'failed with exit code'
        Assert-Equal @($script:calls | Where-Object { $_[0] -eq 'release' }).Count 0
    }
    Test-Case 'attachment failure fails the workflow, leaving the draft for inspection' {
        $script:scenario = 'upload-failure'
        Assert-Fails { New-DraftRelease 'example/repo' '20261029' $commit $zip } 'failed with exit code'
    }
    Test-Case 'verify draft status, target SHA, and tag SHA' {
        foreach ($failure in @('published', 'wrong-target', 'wrong-tag')) {
            $script:scenario = $failure
            Assert-Fails { New-DraftRelease 'example/repo' '20261029' $commit $zip } 'not a draft targeting'
        }
    }
    Test-Case 'verify uploaded asset presence, state, size, and digest' {
        foreach ($failure in @('missing-asset', 'wrong-size', 'wrong-digest', 'incomplete-asset')) {
            $script:scenario = $failure
            Assert-Fails { New-DraftRelease 'example/repo' '20261029' $commit $zip } 'asset verification failed'
        }
    }
    Write-Host "$passed release automation test groups passed."
}
finally {
    [IO.Directory]::Delete($fixture, $true)
}
