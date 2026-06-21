#Requires -Version 5.1
<#
    Thin wrapper. The real pack/push logic lives ONLY in the shared tooling repo at
    C:\Development\NuGet (Pack-Library.ps1). This file holds no canonical body: it
    loads this repo's pack.config.ps1, reads the version from this repo's
    Directory.Build.props, and forwards everything to the shared script.

    Assumes the shared tooling repo is at C:\Development\NuGet. Override with
    -ToolingRoot if it lives elsewhere.
#>
param(
    [string]$ApiKey,
    [switch]$PackOnly,
    [string]$ToolingRoot = 'C:\Development\NuGet'
)

$ErrorActionPreference = 'Stop'
$RepoRoot = $PSScriptRoot

# --- Locate the shared pack script ---
$SharedScript = Join-Path $ToolingRoot 'Pack-Library.ps1'
if (-not (Test-Path $SharedScript)) {
    throw "Shared pack script not found at '$SharedScript'. Expected the Cerebellum tooling repo at '$ToolingRoot' (override with -ToolingRoot)."
}

# --- Load this repo's configuration ---
$ConfigPath = Join-Path $RepoRoot 'pack.config.ps1'
if (-not (Test-Path $ConfigPath)) {
    throw "pack.config.ps1 not found at '$ConfigPath'. Each repo must supply one."
}
$Config = & $ConfigPath
foreach ($key in 'Projects', 'PushSymbols') {
    if (-not $Config.ContainsKey($key)) { throw "pack.config.ps1 must define '$key'." }
}

# --- Read this repo's single version source ---
$PropsPath = Join-Path $RepoRoot 'Directory.Build.props'
if (-not (Test-Path $PropsPath)) {
    throw "Directory.Build.props not found at '$PropsPath'. It is the single version source for this repo."
}
$props = [xml](Get-Content $PropsPath)
$Version = $props.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
if (-not $Version) { throw "No <Version> found in Directory.Build.props." }

# --- Forward to the shared script ---
$forward = @{
    RepoRoot    = $RepoRoot
    Projects    = $Config.Projects
    Version     = $Version
    PushSymbols = [bool]$Config.PushSymbols
    PackOnly    = $PackOnly
}
if ($ApiKey) { $forward.ApiKey = $ApiKey }

& $SharedScript @forward
