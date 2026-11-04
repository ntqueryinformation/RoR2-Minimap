# Launch RoR2 modded with the given mod-manager profile without the mod manager UI. Temporarily
# points doorstop at the profile's BepInEx, restores the original file when the game closes.
param(
    [string]$Profile = "$env:APPDATA\r2modmanPlus-local\RiskOfRain2\profiles\lookingglass",
    [string]$GameDir = "D:\SteamLibrary\steamapps\common\Risk of Rain 2"
)
$ErrorActionPreference = "Stop"
$ini = Join-Path $GameDir "doorstop_config.ini"
$preloader = Join-Path $Profile "BepInEx\core\BepInEx.Preloader.dll"
if (!(Test-Path $preloader)) { throw "BepInEx preloader not found in profile: $preloader" }
Copy-Item $ini "$ini.openminimap.bak" -Force
$normalizedProfile = ($Profile -replace '\\', '/')
(Get-Content $ini) -replace 'target_assembly=.*', "target_assembly=$normalizedProfile/BepInEx/core/BepInEx.Preloader.dll" |
    Set-Content $ini
Write-Host "Launching modded with profile: $Profile (doorstop restored automatically on exit)"
try {
    $process = Start-Process -FilePath (Join-Path $GameDir "Risk of Rain 2.exe") -WorkingDirectory $GameDir -PassThru
    $process.WaitForExit()
}
finally {
    Copy-Item "$ini.openminimap.bak" $ini -Force
    Remove-Item "$ini.openminimap.bak" -Force -ErrorAction SilentlyContinue
    Write-Host "doorstop_config.ini restored."
}
