param([string]$Output = (Join-Path $PSScriptRoot '..\publish'))
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '..\MedicalLab\MedicalLab.csproj'
& dotnet publish $project -c Release -r win-x64 --self-contained true -o $Output --nologo
if ($LASTEXITCODE -ne 0) { throw 'Publishing failed; do not build the installer.' }
& (Join-Path $PSScriptRoot 'Verify-Publish.ps1') -Source $Output
