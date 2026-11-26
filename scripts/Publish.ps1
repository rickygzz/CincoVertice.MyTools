<#
.SYNOPSIS
    Increases the app's version, publishes it as one self-contained exe and zips it for a release.

.DESCRIPTION
    Changes <Version> in the Ui.Win csproj, then runs dotnet publish into artifacts\MyTools-<version>. If the publish
    fails, the csproj goes back to the old version. The exe, its licenses and the user guide (docs\user-guide) then go in
    artifacts\MyTools-<version>-win-x64.zip, and the zip's SHA-256 hash is shown, for the release notes.

    Then it asks, one step at a time, whether to commit the csproj as "Version <version>.", tag that commit
    v<version>, push the common submodule when it has commits not pushed, push the branch and the tag, and open the
    GitHub page to create the release. Each step is done only on a yes; a step that can't be done yet is skipped, with
    why. -NoGit publishes without asking.

    Files already in the output folder are kept, so a config folder from trying the app there isn't lost.

.PARAMETER Bump
    Which part of the version to increase: Major (1.2.3 to 2.0.0), Minor (1.3.0), Patch (1.2.4, the default), or None
    to publish the current version again.

.PARAMETER NoGit
    Publishes and zips only: asks nothing, and commits, tags and pushes nothing.

.EXAMPLE
    .\scripts\Publish.ps1

.EXAMPLE
    .\scripts\Publish.ps1 -Bump Minor

.EXAMPLE
    .\scripts\Publish.ps1 -Bump None -NoGit
#>
[CmdletBinding()]
param(
    [ValidateSet('Major', 'Minor', 'Patch', 'None')]
    [string] $Bump = 'Patch',

    [switch] $NoGit
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'src\CincoVertice.MyTools.Ui.Win\CincoVertice.MyTools.Ui.Win.csproj'

function Write-Project([string] $text, [bool] $hasBom) {
    [System.IO.File]::WriteAllText($project, $text, [System.Text.UTF8Encoding]::new($hasBom))
}

# Written back with the same BOM and line endings, so the only change in the file is the number
$bytes = [System.IO.File]::ReadAllBytes($project)
$hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
$original = [System.IO.File]::ReadAllText($project)

$versionPattern = '<Version>(\d+)\.(\d+)\.(\d+)</Version>'
$found = [regex]::Match($original, $versionPattern)

if (-not $found.Success) {
    throw "No <Version>major.minor.patch</Version> in $project"
}

$major = [int]$found.Groups[1].Value
$minor = [int]$found.Groups[2].Value
$patch = [int]$found.Groups[3].Value
$oldVersion = "$major.$minor.$patch"

$version = switch ($Bump) {
    'Major' { "$($major + 1).0.0" }
    'Minor' { "$major.$($minor + 1).0" }
    'Patch' { "$major.$minor.$($patch + 1)" }
    'None' { $oldVersion }
}

$outputFolder = Join-Path $repoRoot "artifacts\MyTools-$version"

Write-Host "Version: $oldVersion -> $version"
Write-Project ([regex]::new($versionPattern).Replace($original, "<Version>$version</Version>", 1)) $hasBom

$published = $false

try {
    & dotnet publish $project `
        --configuration Release `
        --runtime win-x64 `
        --self-contained true `
        --output $outputFolder `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
		-p:DebugType=none

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE"
    }

    $published = $true
}
finally {
    if (-not $published) {
        Write-Project $original $hasBom
        Write-Host "Publish failed: the version stays $oldVersion" -ForegroundColor Red
    }
}

Write-Host "Published $version to $outputFolder" -ForegroundColor Green

# The user guide goes next to the exe as docs\user-guide, the path new scripts' comments give, where the help window
# reads it. The publish copies it there too, but adds to what's there: copied fresh here, so a page removed from the
# guide isn't left over from an earlier publish
$guideFolder = Join-Path $outputFolder 'docs\user-guide'

if (Test-Path $guideFolder) {
    Remove-Item $guideFolder -Recurse -Force
}

New-Item -ItemType Directory -Path $guideFolder -Force | Out-Null
Copy-Item (Join-Path $repoRoot 'docs\user-guide\*') $guideFolder -Recurse

# The diagram editor's shape libraries go next to the exe as library, a folder per library, which its images panel
# reads. Copied fresh too, so a shape removed from a library isn't left over from an earlier publish
$libraryFolder = Join-Path $outputFolder 'library'

if (Test-Path $libraryFolder) {
    Remove-Item $libraryFolder -Recurse -Force
}

New-Item -ItemType Directory -Path $libraryFolder -Force | Out-Null
Copy-Item (Join-Path $repoRoot 'src\CincoVertice.MyTools.Ui.Win\Diagramming\Library\*') $libraryFolder -Recurse

# The exe, the licenses, which Roboto's requires to go along with the font inside it, the guide and the shape
# libraries. Not the whole folder: it can also have a config folder from trying the app there
$zip = Join-Path $repoRoot "artifacts\MyTools-$version-win-x64.zip"
$zipped = @(
    (Join-Path $outputFolder '*.exe'),
    (Join-Path $outputFolder 'licenses'),
    (Join-Path $outputFolder 'docs'),
    $libraryFolder
)

Compress-Archive -Path $zipped -DestinationPath $zip -Force

Write-Host "Zipped to $zip" -ForegroundColor Green
Write-Host "SHA-256: $((Get-FileHash $zip -Algorithm SHA256).Hash)" -ForegroundColor Green

if ($NoGit) {
    return
}

# Releasing: each step only when the user says yes, in the order the tag needs, since it must include the commit
# that changes the version
function Confirm-Step([string] $question) {
    return (Read-Host "$question [y/N]") -match '^\s*(y|yes)\s*$'
}

function Invoke-Git([string] $folder) {
    & git -C $folder @args

    if ($LASTEXITCODE -ne 0) {
        throw "git $($args -join ' ') failed with exit code $LASTEXITCODE"
    }
}

function Skip([string] $why) {
    Write-Host "Skipped: $why" -ForegroundColor Yellow
}

Write-Host ''

$tag = "v$version"
$commonFolder = Join-Path $repoRoot 'common'
$projectPath = 'src/CincoVertice.MyTools.Ui.Win/CincoVertice.MyTools.Ui.Win.csproj'
$branch = (& git -C $repoRoot branch --show-current).Trim()

if ($branch -ne 'dev') {
    Write-Warning "This is branch '$branch': releases come from dev."
}

# Changes other than the version: the zip has them, the tag wouldn't
$others = @(& git -C $repoRoot status --porcelain | Where-Object { -not $_.EndsWith($projectPath) })

if ($others.Count -gt 0) {
    Write-Warning "Changes not committed, which the zip has but the tag won't:"
    $others | ForEach-Object { Write-Host "    $_" }
}

$isVersionCommitted = -not (& git -C $repoRoot status --porcelain '--' $projectPath)

if (-not $isVersionCommitted -and (Confirm-Step "Commit the csproj as 'Version $version.'?")) {
    # Only that file, even when others are staged
    Invoke-Git $repoRoot commit -m "Version $version." '--' $projectPath
    $isVersionCommitted = $true
}

if (& git -C $repoRoot tag --list $tag) {
    Write-Host "Tag $tag is there already." -ForegroundColor Yellow
}
elseif (-not $isVersionCommitted) {
    Skip "tag $tag, since the csproj's new version isn't committed: commit it, then git tag $tag"
}
elseif (Confirm-Step "Tag this commit $tag ($((& git -C $repoRoot log -1 --format=%s).Trim()))?") {
    Invoke-Git $repoRoot tag $tag
}

# The release's submodule commit must be on GitHub too, or nobody else can build the tag
# Windows PowerShell turns a native command's error output into an error, which stops the script here
$ErrorActionPreference = 'Continue'
$commonUpstream = & git -C $commonFolder rev-parse --abbrev-ref '@{upstream}' 2>$null
$commonFailed = $LASTEXITCODE -ne 0
$ErrorActionPreference = 'Stop'

if ($commonFailed) {
    Skip "checking common, whose branch follows none on GitHub: push it yourself if it has commits"
}
else {
    $commonAhead = [int](& git -C $commonFolder rev-list --count '@{upstream}..HEAD')
    $commits = if ($commonAhead -eq 1) { '1 commit' } else { "$commonAhead commits" }

    if ($commonAhead -gt 0 -and (Confirm-Step "common has $commits not pushed to $commonUpstream. Push common?")) {
        # To the branch it follows, by name, whatever its own is called: origin/dev is origin, HEAD:dev
        $remote, $remoteBranch = $commonUpstream.Trim() -split '/', 2
        Invoke-Git $commonFolder push $remote "HEAD:$remoteBranch"
    }
}

$hasTag = [bool](& git -C $repoRoot tag --list $tag)
$pushed = if ($hasTag) { "$branch and tag $tag" } else { $branch }

if (Confirm-Step "Push $pushed to origin?") {
    Invoke-Git $repoRoot push origin $branch

    if ($hasTag) {
        Invoke-Git $repoRoot push origin $tag
    }
}

# Where the release is made, from the csproj's repository, as the app's update check reads it
$repository = [regex]::Match($original, '<RepositoryUrl>([^<]+)</RepositoryUrl>').Groups[1].Value.TrimEnd('/')

if ($repository -and $hasTag) {
    $releasePage = "$repository/releases/new?tag=$tag"

    Write-Host "Create the release at $releasePage and attach $zip" -ForegroundColor Green

    if (Confirm-Step 'Open that page?') {
        Start-Process $releasePage
    }
}
