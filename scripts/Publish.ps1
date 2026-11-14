<#
.SYNOPSIS
    Increases the app's version, publishes it as one self-contained exe and zips it for a release.

.DESCRIPTION
    Changes <Version> in the Ui.Win csproj, then runs dotnet publish into artifacts\MyTools-<version>. If the publish
    fails, the csproj goes back to the old version. The exe, its licenses and the user guide (docs\user-guide) then go in
    artifacts\MyTools-<version>-win-x64.zip, and the zip's SHA-256 hash is shown, for the release notes. The script
    commits nothing.

    Files already in the output folder are kept, so a config folder from trying the app there isn't lost.

.PARAMETER Bump
    Which part of the version to increase: Major (1.2.3 to 2.0.0), Minor (1.3.0), Patch (1.2.4, the default), or None
    to publish the current version again.

.EXAMPLE
    .\scripts\Publish.ps1

.EXAMPLE
    .\scripts\Publish.ps1 -Bump Minor
#>
[CmdletBinding()]
param(
    [ValidateSet('Major', 'Minor', 'Patch', 'None')]
    [string] $Bump = 'Patch'
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

# The exe, the licenses, which Roboto's requires to go along with the font inside it, and the guide. Not the whole
# folder: it can also have a config folder from trying the app there
$zip = Join-Path $repoRoot "artifacts\MyTools-$version-win-x64.zip"

Compress-Archive -Path (Join-Path $outputFolder '*.exe'), (Join-Path $outputFolder 'licenses'), (Join-Path $outputFolder 'docs') `
    -DestinationPath $zip -Force

Write-Host "Zipped to $zip" -ForegroundColor Green
Write-Host "SHA-256: $((Get-FileHash $zip -Algorithm SHA256).Hash)" -ForegroundColor Green
