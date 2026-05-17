param(
    [Parameter(Mandatory)]
    [string]$ApiKey
)

$ErrorActionPreference = 'Stop'

# Wipe and recreate the output directory
Write-Host "Preparing output directory..."
if (Test-Path ./nupkgs) {
    Remove-Item ./nupkgs -Recurse -Force
}
New-Item ./nupkgs -ItemType Directory | Out-Null

# Pack
Write-Host "Packing AuthBlocksLib..."
dotnet pack AuthBlocksLib/AuthBlocksLib.csproj -c Release -o ./nupkgs

# Push
Write-Host "Pushing to nuget.org..."
dotnet nuget push "nupkgs/*.nupkg" --api-key $ApiKey --source https://api.nuget.org/v3/index.json

$csproj = [xml](Get-Content 'AuthBlocksLib/AuthBlocksLib.csproj')
$Version = $csproj.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -ExpandProperty Version
Write-Host "Done. Cerebellum.AuthBlocks $Version published successfully."
