param(
    [string]$NuGetPackageRoot,
    [string]$ProjectDir
)

$installerPath = Join-Path $ProjectDir "scripts\installer.nsi"

if ($Env:OS -and $Env:OS.ToLower().Contains("windows")) {
    $makensis = Join-Path $NuGetPackageRoot "nsis-tool\3.0.8\tools\makensis.exe"
    & $makensis $installerPath
}
else {
    & makensis $installerPath
}