#Requires -Version 7.0
param(
    [string]$Feed = $(Join-Path (Split-Path $PSScriptRoot -Parent) "../artifacts/nuget-local"),
    [string]$MathRoot = $(Join-Path (Split-Path $PSScriptRoot -Parent) "novolis-math")
)

$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force -Path $Feed | Out-Null

if (-not (Test-Path $MathRoot)) {
    $sibling = Join-Path (Split-Path $PSScriptRoot -Parent) "../novolis-math"
    if (Test-Path $sibling) {
        $MathRoot = (Resolve-Path $sibling).Path
    } else {
        throw "novolis-math not found at $MathRoot"
    }
}

$pack = Join-Path $MathRoot "scripts/pack-local.ps1"
& $pack -Feed $Feed
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "Packed Math from $MathRoot -> $Feed"
