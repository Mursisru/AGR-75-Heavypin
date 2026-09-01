# Sync Blender FBX/textures → UnityBake → build AGR75Heavypin.nobp → deploy
param(
    [string]$BlenderRoot = "C:\Users\at747_loyuw9y\OneDrive\Документы\Blender\AGR-75-Heavypin",
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$UnityExe = "${env:ProgramFiles}\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe"
)

$ErrorActionPreference = "Stop"
$missile = Join-Path $RepoRoot "UnityBake\Assets\MissilePack"

Copy-Item -Force (Join-Path $BlenderRoot "AGR-75-Heavypin-MainRocket.fbx") $missile
Copy-Item -Force (Join-Path $BlenderRoot "LaunchStandAGR-75-4X.fbx") $missile
Copy-Item -Force (Join-Path $BlenderRoot "LaunchStandAGR-75-6X.fbx") $missile

robocopy (Join-Path $BlenderRoot "AGR-Textures\AGR-Rocket-Textures") (Join-Path $missile "Textures\Rocket") "* Color.png" "* Normal.png" /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
robocopy (Join-Path $BlenderRoot "AGR-Textures\Launch-Stand-textures") (Join-Path $missile "Textures\Launcher") "* Color.png" "* Normal.png" /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
Copy-Item -Force (Join-Path $BlenderRoot "AGR-Textures\AGR-Rocket-Textures\AGR-Preview.png") (Join-Path $RepoRoot "AGR75Heavypin\Resources\AGR-Preview.png")

$proj = Join-Path $RepoRoot "UnityBake"
$log = Join-Path $proj "batchbuild-gui.log"
$env:HEAVYPIN_UNITY_EXIT = "1"

$p = Start-Process -FilePath $UnityExe -ArgumentList @("-projectPath", $proj, "-executeMethod", "BatchBuild.Build", "-logFile", $log) -PassThru -Wait
if ($p.ExitCode -ne 0) {
    Write-Error "Unity rebake failed exit=$($p.ExitCode). See $log"
}

dotnet build (Join-Path $RepoRoot "AGR75Heavypin\AGR75Heavypin.csproj") -c Release
Write-Host "Rebake OK. nobp + DLL deployed."
