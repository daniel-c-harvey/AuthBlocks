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
dotnet nuget push "nupkgs/*.nupkg" --api-key $ApiKey --source https://api.nuget.org/v3/index.json --skip-duplicate

Write-Host "Done. Cerebellum.AuthBlocks 1.0.0 published successfully."
