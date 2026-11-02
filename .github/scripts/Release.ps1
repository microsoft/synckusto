Set-StrictMode -Version Latest

function Get-ReleaseVersion {
    param(
        [Parameter(Mandatory)][string] $Date,
        [AllowEmptyCollection()][string[]] $ExistingTags = @()
    )

    $parsedDate = [datetime]::MinValue
    if ($Date -cnotmatch '^[0-9]{8}$' -or
        -not [datetime]::TryParseExact($Date, 'yyyyMMdd', [cultureinfo]::InvariantCulture,
            [System.Globalization.DateTimeStyles]::None, [ref] $parsedDate)) {
        throw "Release date must be a real calendar date in YYYYMMDD format: $Date"
    }

    $highest = -1
    foreach ($tag in $ExistingTags) {
        if ($tag -cmatch "^v$Date(?:\.([1-9][0-9]*))?$") {
            $suffix = 0
            if ($Matches[1] -and -not [int]::TryParse($Matches[1], [ref] $suffix)) {
                throw "Release suffix exceeds the supported integer range: $tag"
            }
            $highest = [Math]::Max($highest, $suffix)
        }
    }
    if ($highest -eq [int]::MaxValue) {
        throw "Release suffixes are exhausted for $Date"
    }
    if ($highest -lt 0) {
        return $Date
    }
    return "$Date.$($highest + 1)"
}

function Invoke-ReleaseGitHub {
    param([Parameter(Mandatory)][string[]] $Arguments)

    $output = & gh @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "GitHub CLI '$($Arguments[0])' failed with exit code $LASTEXITCODE."
    }
    if ([string]::IsNullOrWhiteSpace(($output -join "`n"))) {
        throw "GitHub CLI '$($Arguments[0])' returned an empty response."
    }
    return ($output -join "`n")
}

function Get-ReservedReleaseTags {
    param([Parameter(Mandatory)][string] $Repository)

    foreach ($collection in @('releases', 'tags')) {
        $pages = Invoke-ReleaseGitHub @('api', "repos/$Repository/${collection}?per_page=100", '--paginate', '--slurp') |
            ConvertFrom-Json -Depth 100 -NoEnumerate -ErrorAction Stop
        if ($pages -isnot [array]) {
            throw "GitHub API returned an invalid $collection response: expected an array of pages."
        }
        foreach ($page in $pages) {
            if ($page -isnot [array]) {
                throw "GitHub API returned an invalid $collection page: expected an array."
            }
            foreach ($entry in $page) {
                $name = if ($collection -eq 'releases') { $entry.tag_name } else { $entry.name }
                if ([string]::IsNullOrWhiteSpace($name)) {
                    throw "GitHub API returned an invalid $collection entry with no tag name."
                }
                $name
            }
        }
    }
}

function New-ReleaseArchive {
    param(
        [Parameter(Mandatory)][string] $PublishDirectory,
        [Parameter(Mandatory)][string] $TestedAssembly,
        [Parameter(Mandatory)][string] $Destination
    )

    $requiredFiles = @(
        'SyncKusto.exe', 'SyncKusto.dll', 'SyncKusto.deps.json', 'SyncKusto.runtimeconfig.json',
        'SyncKusto.Core.dll', 'SyncKusto.Kusto.dll', 'SyncKusto.FileSystem.dll'
    )
    foreach ($name in $requiredFiles) {
        if (-not (Test-Path -LiteralPath (Join-Path $PublishDirectory $name) -PathType Leaf)) {
            throw "Published application is missing $name"
        }
    }
    if (@(Get-ChildItem -LiteralPath $PublishDirectory -Recurse -Filter '*.Tests.dll').Count -ne 0) {
        throw 'Published application must not contain test assemblies.'
    }
    $expectedHash = (Get-FileHash -LiteralPath $TestedAssembly -Algorithm SHA256).Hash
    if ((Get-FileHash -LiteralPath (Join-Path $PublishDirectory 'SyncKusto.dll') -Algorithm SHA256).Hash -cne $expectedHash) {
        throw 'Published application differs from the version-stamped assembly that passed the tests.'
    }
    $destinationPath = [IO.Path]::GetFullPath($Destination)
    $null = [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destinationPath))
    [IO.Compression.ZipFile]::CreateFromDirectory($PublishDirectory, $destinationPath)

    $archive = [IO.Compression.ZipFile]::OpenRead($destinationPath)
    try {
        foreach ($name in $requiredFiles) {
            if ($null -eq $archive.GetEntry($name)) {
                throw "Release ZIP is missing $name at its root."
            }
        }
        $stream = $archive.GetEntry('SyncKusto.dll').Open()
        try {
            $packagedHash = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($stream))
            if ($packagedHash -cne $expectedHash) {
                throw 'Release ZIP does not contain the tested application assembly.'
            }
        }
        finally {
            $stream.Dispose()
        }
    }
    finally {
        $archive.Dispose()
    }
}

function New-DraftRelease {
    param(
        [Parameter(Mandatory)][string] $Repository,
        [Parameter(Mandatory)][ValidatePattern('^[0-9]{8}(\.[1-9][0-9]*)?$')][string] $Version,
        [Parameter(Mandatory)][ValidatePattern('^[0-9a-f]{40}$')][string] $Commit,
        [Parameter(Mandatory)][string] $Archive
    )

    $null = Get-ReleaseVersion -Date $Version.Substring(0, 8)
    $file = Get-Item -LiteralPath $Archive -ErrorAction Stop
    if ($file.Name -cne 'synckusto.zip' -or $file.Length -eq 0) {
        throw 'The release asset must be a nonempty synckusto.zip.'
    }
    $tag = "v$Version"
    if ($tag -cin @(Get-ReservedReleaseTags -Repository $Repository)) {
        throw "Release number is already reserved: $tag. Nothing will be overwritten."
    }

    # Atomically reserve the tag so even a concurrent manual release cannot reuse it.
    $null = Invoke-ReleaseGitHub @('api', '--method', 'POST', "repos/$Repository/git/refs",
        '-f', "ref=refs/tags/$tag", '-f', "sha=$Commit")
    $null = Invoke-ReleaseGitHub @('release', 'create', $tag, $file.FullName, '--repo', $Repository,
        '--verify-tag', '--target', $Commit, '--draft', '--title', "SyncKusto $tag",
        '--generate-notes', '--notes', "Source commit: $Commit")

    $release = Invoke-ReleaseGitHub @('release', 'view', $tag, '--repo', $Repository,
        '--json', 'isDraft,tagName,targetCommitish,assets,url') | ConvertFrom-Json -ErrorAction Stop
    $reference = Invoke-ReleaseGitHub @('api', "repos/$Repository/git/ref/tags/$tag") |
        ConvertFrom-Json -ErrorAction Stop
    $assets = @($release.assets | Where-Object { $_.name -ceq $file.Name })
    $digest = 'sha256:' + (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    if (-not $release.isDraft -or $release.tagName -cne $tag -or $release.targetCommitish -cne $Commit -or
        $reference.object.type -cne 'commit' -or $reference.object.sha -cne $Commit) {
        throw 'Created release is not a draft targeting the tested source commit.'
    }
    if ($assets.Count -ne 1 -or $assets[0].state -cne 'uploaded' -or
        $assets[0].size -ne $file.Length -or $assets[0].digest -cne $digest) {
        throw 'Draft release asset verification failed. Inspect the incomplete draft before publishing.'
    }
    return $release
}
