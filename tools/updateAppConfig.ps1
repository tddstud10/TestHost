param (
    [Parameter(Mandatory=$true)]
    [string]
    $BuildDir,

    [Parameter(Mandatory=$true)]
    [string]
    $Version
)

$ErrorActionPreference = "Stop"

Write-Output "Parameters: BuildDir = $BuildDir, Version = $Version"

Write-Output "Updating appconfig... "
$appConfig = Join-path $BuildDir "TestData2\CSXUnit1xNUnit3x.dll.config"
$xml = [xml](Get-Content $appConfig)
$xml.configuration.runtime.assemblyBinding.dependentAssembly | `
    Where-Object { 
        $_.assemblyIdentity.name -ieq "R4nd0mApps.TddStud10.TestRuntime" } | `
    ForEach-Object { 
        $_.bindingRedirect.newVersion = $Version }
$xml.Save($appConfig)
Write-Output "Done"
