param([string]$Source = (Join-Path $PSScriptRoot '..\publish'))
$ErrorActionPreference = 'Stop'
$project = [xml](Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot '..\MedicalLab\MedicalLab.csproj'))
$minimumRuntime = [version]$project.SelectSingleNode('//RuntimeFrameworkVersion').InnerText
$expectedVersion = [version]$project.Project.PropertyGroup.Version
$config = Get-Content -Raw -LiteralPath (Join-Path $Source 'MedicalLab.runtimeconfig.json') | ConvertFrom-Json
$frameworks = @($config.runtimeOptions.includedFrameworks)
foreach ($name in @('Microsoft.NETCore.App', 'Microsoft.WindowsDesktop.App')) {
    $framework = @($frameworks | Where-Object name -eq $name)
    if ($framework.Count -ne 1 -or [version]$framework[0].version -lt $minimumRuntime) {
        throw "Published $name is missing or older than $minimumRuntime. Republish before packaging."
    }
}
$pkcs = Get-Item -LiteralPath (Join-Path $Source 'System.Security.Cryptography.Pkcs.dll')
$actualPkcs = [version]($pkcs.VersionInfo.ProductVersion.Split('+')[0])
if ($actualPkcs -lt $minimumRuntime) { throw "Published Pkcs is $actualPkcs; expected at least $minimumRuntime." }
$assembly = [Reflection.AssemblyName]::GetAssemblyName((Join-Path $Source 'MedicalLab.dll'))
if ($assembly.Version.ToString(3) -ne $expectedVersion.ToString(3)) {
    throw 'Published application version does not match the project. Republish before packaging.'
}
Write-Output "Verified application $expectedVersion, bundled runtimes >= $minimumRuntime, Pkcs $actualPkcs."
