param()
$ErrorActionPreference = 'Stop'
$captureRoot = Split-Path $PSScriptRoot -Parent
$captureProject = Join-Path $PSScriptRoot 'MarketingCapture/MarketingCapture.csproj'
$captureOutput = Join-Path $captureRoot 'artifacts/marketing-capture/'

# Separate output leaves an already-running MyTools instance alone.
dotnet build $captureProject --verbosity minimal "-p:BaseOutputPath=$captureOutput"
if ($LASTEXITCODE -ne 0) { throw 'The screenshot fixture did not build.' }
$captureAssembly = Join-Path $captureOutput 'Debug/net10.0-windows10.0.19041.0/MarketingCapture.dll'
dotnet $captureAssembly $captureRoot
if ($LASTEXITCODE -ne 0) { throw 'Screenshot capture failed.' }
